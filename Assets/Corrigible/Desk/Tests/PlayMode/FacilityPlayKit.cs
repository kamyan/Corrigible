using System.Collections;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis baskınının PlayMode sürücüsü (aşama 3; Docs/TESIS-BASKINLARI-TASARIMI.md §3.3–3.6, §11.5). <see cref="BoardPlayKit"/>
    /// masayı asset dengesiyle kurar (tesis tek baskın kipi; asset değişmez);
    /// bu kit eklemeli yüklenen Baskin sahnesindeki <see cref="LiveRaidDataSource"/>'u bulur ve odaları oyuncunun yolundan sürer:
    /// seçim, kapıya giriş (<c>EnterGate</c>), GEÇ (<c>EndTurn</c>), Teknisyen'in yeteneği (<c>Arm</c>/<c>Use</c>), tesisin
    /// girişinden tahliye. Işınlama (birimi kapı karesine ya da hedefin yanına koymak) ve makinelerin kaldırılması test kurulumudur;
    /// komutlar ve raporlar gerçek yoldan gider (<c>raid_room</c>, <c>raidres</c>). Tohum sabit (79); boş kare bulunamazsa ileti
    /// odayı yazar. EditMode'daki <c>FacilityDeskKit</c>'in PlayMode karşılığı (test derlemeleri birbirini görmez).
    /// </summary>
    public static class FacilityPlayKit
    {
        public const string RaidSceneName = "Baskin";

        /// <summary>Son <see cref="Source"/> çağrısının bulduğu baskın kaynağı.</summary>
        public static LiveRaidDataSource Live { get; private set; }

        /// <summary>Baskin sahnesi yüklenip tahta açılana kadar bekler (<see cref="Live"/>), sonra kısa bir yerleşme.</summary>
        public static IEnumerator Source(float timeout = 8f)
        {
            Live = null;
            yield return BoardPlayKit.WaitFor(() => Object.FindFirstObjectByType<LiveRaidDataSource>()?.State != null, timeout, "baskın sahnesi yüklenmedi");
            Live = Object.FindFirstObjectByType<LiveRaidDataSource>();
            yield return BoardPlayKit.WaitSeconds(.6f);
            yield return Settle(Live, "açılış");
        }

        /// <summary>Baskin sahnesi açıksa boşaltır (testin sökümü; DeskGame'den önce).</summary>
        public static IEnumerator UnloadRaid()
        {
            Live = null;
            if (!RaidLauncher.IsRaidLoaded) yield break;
            var scene = SceneManager.GetSceneByName(RaidSceneName);
            if (!scene.IsValid() || !scene.isLoaded) yield break;
            var unload = SceneManager.UnloadSceneAsync(scene);
            while (unload != null && !unload.isDone) yield return null;
        }

        /// <summary>Baskin sahnesinin tutamağı (sahne yeniden yüklendi mi: aynı oda zincirinde değişmemeli).</summary>
        public static int SceneHandle => SceneManager.GetSceneByName(RaidSceneName).handle;

        public static char Letter(Baskin.Unit u) => Baskin.UnitTypes[u.Type].Short[0];

        /// <summary>Tahtadaki ayaktaki uzmanlar, kadro sırasıyla (aşama 9: köpek, aygıt ve tutsak uzman değil; kapıya sayılmaz).</summary>
        public static Baskin.Unit[] Standing(Baskin.BState B) =>
            B.Units.Where(u => Baskin.IsSpecialist(u) && Baskin.OnBoard(u)).OrderBy(u => u.Slot).ToArray();

        /// <summary>Birimsiz (ya da <paramref name="u"/>'nun durduğu) ilk kapı karesi; yoksa null.</summary>
        public static Baskin.Pt? FreeGate(Baskin.BState B, Baskin.Unit u)
        {
            foreach (var g in Baskin.GateTiles(B))
            {
                var o = Baskin.UnitAt(B, g.X, g.Y);
                if (o == null || o == u) return g;
            }
            return null;
        }

        static string Where(LiveRaidDataSource src) =>
            "tohum " + BoardPlayKit.Sim?.S.Seed + " · oda " + src.State?.Room?.Id + " · pafta " + src.State?.Room?.Index +
            (src.State?.Escape == true ? " · kaçış" : "") + " · tur " + src.State?.Turn + " · evre " + src.State?.Phase;

        /// <summary>Kaynak boşalana kadar bekler: canlandırma, makine evresi ve pafta çevirme bitti.</summary>
        public static IEnumerator Settle(LiveRaidDataSource src, string what, float timeout = 8f)
        {
            yield return BoardPlayKit.WaitFor(() => src != null && !src.Busy && !src.Executing && !src.Passing, timeout, what + ": baskın boşalmadı (" + Where(src) + ")");
            yield return BoardPlayKit.Frames(2);
        }

        /// <summary>Makineleri devre dışı bırakır, uyanmayı kapatır ve tahtayı yeniden çizer (testin kurduğu kareler bozulmasın).</summary>
        public static void Calm(LiveRaidDataSource src)
        {
            var B = src.State;
            foreach (var e in B.Units.Where(u => u.Side == 'e' && !u.Dead)) { e.Dead = true; e.Intent = null; }
            B.Spawns.Clear();
            B.SpawnPerTurn = 0;
            src.ClearHover();
        }

        /// <summary>Ayaktaki uzmanı boş bir kapı karesine ışınlar, sonra gerçek yoldan seçip kapıya sokar (EnterGate).</summary>
        public static IEnumerator ToGate(LiveRaidDataSource src, Baskin.Unit u)
        {
            yield return Settle(src, "kapı öncesi");
            var g = FreeGate(src.State, u);
            Assert.IsTrue(g.HasValue, "boş kapı karesi yok: " + Where(src));
            u.X = g.Value.X; u.Y = g.Value.Y;
            src.ClearHover();
            src.Select(Letter(u));
            Assert.AreEqual(Letter(u), src.Selected, "seçilemedi: " + u.Type + " · " + Where(src));
            src.EnterGate();
            Assert.IsTrue(u.AtGate || u.Out, "kapıya girmedi: " + u.Type + " · " + Where(src));
            yield return Settle(src, "kapıya giriş");
        }

        /// <summary>
        /// Odanın geçidinden geçiş: makineler kalkar, ayaktakiler sırayla kapıya girer, herkes kapıdayken EMRİ İLET (= GEÇ) verilir;
        /// rapor dosyalanır ve sıradaki pafta çevrilir (ya da zincir kapanır, sonuç kartı). Tesisin girişinde kullanılmaz (<see cref="WalkOut"/>).
        /// </summary>
        public static IEnumerator ThroughGate(LiveRaidDataSource src)
        {
            Assert.IsFalse(Baskin.IsFinalExit(src.State), "tesisin girişinde GEÇ yok: " + Where(src));
            Calm(src);
            foreach (var u in Standing(src.State)) yield return ToGate(src, u);
            Assert.IsTrue(src.Current.Team.Where(t => !t.Down && !t.Dog).All(t => t.AtGate), "herkes kapıda (köpek ekiple geçer): " + Where(src));
            Assert.IsTrue(src.Current.CanPass, "GEÇ açık olmalı: " + Where(src));
            int filed = BoardPlayKit.Sim.S.Raid.Reports.Count;
            src.EndTurn();
            Assert.AreEqual(filed + 1, BoardPlayKit.Sim.S.Raid.Reports.Count, "GEÇ odanın raporunu dosyalar (raid_room): " + Where(src));
            yield return Settle(src, "pafta çevirme");
        }

        /// <summary>Tesisin girişi (G dönüşü): ayaktakiler batı karelerinden tahliye edilir; son çıkan zinciri kapatır.</summary>
        public static IEnumerator WalkOut(LiveRaidDataSource src)
        {
            Assert.IsTrue(Baskin.IsFinalExit(src.State), "tesisin girişi değil: " + Where(src));
            Calm(src);
            foreach (var u in Standing(src.State)) yield return ToGate(src, u);
            yield return Settle(src, "tahliye");
        }

        /// <summary>
        /// Hedef odası: Teknisyen hedefin yanındaki boş zemine ışınlanır, seçilip yeteneği hedefe kullanılır (ikmalde kasa açılır,
        /// kaçış başlar). Sunucu görevinde dolap kırılana kadar tur bitirilir (makineler kalkık).
        /// </summary>
        public static IEnumerator TakeTarget(LiveRaidDataSource src)
        {
            Assert.AreEqual(RoomType.Target, src.State.Room.Type, "hedef odası değil: " + Where(src));
            for (int guard = 0; guard < 8 && !src.State.Escape; guard++)
            {
                Calm(src);
                yield return Settle(src, "hedef öncesi");
                var B = src.State;
                var target = Baskin.TargetsLeft(B).FirstOrDefault();
                Assert.IsNotNull(target.O, "hedef kalmadı ama kaçış başlamadı: " + Where(src));
                var tek = Standing(B).FirstOrDefault(u => u.Type == "tek");
                Assert.IsNotNull(tek, "Teknisyen tahtada değil: " + Where(src));
                if (tek.Acted) { yield return EndTurnConfirmed(src); continue; }
                Baskin.Pt? spot = null;
                foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int x = target.X + d.Item1, y = target.Y + d.Item2;
                    if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N) continue;
                    var c = Baskin.CellAt(B, x, y);
                    var other = Baskin.UnitAt(B, x, y);
                    if (c.T != "floor" || c.O != null || other != null && other != tek || Baskin.IsGateTile(B, x, y)) continue;
                    spot = new Baskin.Pt(x, y);
                    break;
                }
                Assert.IsTrue(spot.HasValue, "hedefin yanında boş zemin yok: " + Where(src));
                tek.X = spot.Value.X; tek.Y = spot.Value.Y;
                src.ClearHover();
                src.Select(Letter(tek));
                Assert.AreEqual(Letter(tek), src.Selected, "Teknisyen seçilemedi: " + Where(src));
                src.Arm();
                Assert.IsTrue(src.Armed, "yetenek kurulamadı: " + Where(src));
                src.Use(new Vector2Int(target.X, target.Y));
                Assert.IsTrue(tek.Acted, "yetenek hedefe kullanılmadı: " + Where(src));
                yield return Settle(src, "yetenek");
            }
            Assert.IsTrue(src.State.Escape, "kaçış başlamadı: " + Where(src));
        }

        /// <summary>EMRİ İLET; onay sorulursa (boşta kalan hamle) bir kez daha. Makine evresi bitene kadar bekler.</summary>
        public static IEnumerator EndTurnConfirmed(LiveRaidDataSource src)
        {
            yield return Settle(src, "tur öncesi");
            int turn = src.State.Turn;
            src.EndTurn();
            if (src.Current.EndConfirm) src.EndTurn();
            yield return BoardPlayKit.WaitFor(() => src.State.Turn != turn || src.State.Phase != "player" || src.Current.Finished, 10, "tur bitmedi: " + Where(src));
            yield return Settle(src, "makine evresi");
        }

        // ---------- Tesis hamleleri (aşama 7): gerçek giriş yolu (E tuşu, tahtaya tık) ve kurulum yardımcıları ----------
        /// <summary>
        /// Masayı kurar (asset dengesi), harekâtın ilk sektörüne baskın emri verir, tahtayı bekler (<see cref="Live"/>). Dil Türkçe.
        /// </summary>
        public static IEnumerator OpenRaid()
        {
            yield return BoardPlayKit.LoadDesk();
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            var sim = BoardPlayKit.Sim;
            var next = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.IsNotNull(next, "alınacak sektör yok");
            var started = BoardPlayKit.Game.StartLiberationRaid(next.Id);
            Assert.IsTrue(started.Ok, started.Msg);
            yield return Source();
            Assert.IsTrue(FacilityRaidSession.Applies(sim));
            Assert.IsNotNull(Live.State.Room, "tesis odası");
        }

        /// <summary>Ekibin o türdeki üyesi (geri almadan sonra yeniden okunmalı: <c>Baskin.Undo</c> birim listesini yeniler).</summary>
        public static Baskin.Unit Team(LiveRaidDataSource src, string type) => src.State.Units.First(u => u.Side == 'p' && u.Type == type);

        public static RaidBoard3D Board => Object.FindFirstObjectByType<RaidBoard3D>();

        /// <summary>
        /// E tuşuna (gerçek klavye yolu: <c>RaidHudView.Update</c> → <c>LetterKeys</c> → <c>ArmAction</c>) istenen hamle kurulana kadar
        /// en çok 8 kez basar; kurulamazsa kırılır.
        /// </summary>
        public static IEnumerator ArmByKey(LiveRaidDataSource src, string kind, int other = 0)
        {
            for (int i = 0; i < 8 && !(src.ActionKind == kind && (other == 0 || src.ActionOther == other)); i++)
                yield return BoardPlayKit.PressKey(Key.E);
            Assert.AreEqual(kind, src.ActionKind, "E ile hamle kurulamadı: " + kind + " · " + Where(src));
            if (other != 0) Assert.AreEqual(other, src.ActionOther, "E ile gövde seçilemedi · " + Where(src));
        }

        // ---------- Tesis ekipmanı (aşama 9): R tuşu, köpek kartı, kurulum yardımcıları ----------
        /// <summary>
        /// R tuşuna (gerçek klavye yolu: <c>RaidHudView.Update</c> → <c>LetterKeys</c> "gear" → <c>ArmGear</c>) istenen ekipman eylemi
        /// kurulana kadar en çok 3 kez basar (örme setinde tel → kalas → kapalı); kurulamazsa kırılır.
        /// </summary>
        public static IEnumerator ArmGearByKey(LiveRaidDataSource src, string kind)
        {
            for (int i = 0; i < 3 && src.GearKind != kind; i++) yield return BoardPlayKit.PressKey(Key.R);
            Assert.AreEqual(kind, src.GearKind, "R ile ekipman eylemi kurulamadı: " + kind + " · " + Where(src));
        }

        /// <summary>4 tuşu (gerçek klavye yolu: dördüncü kart, köpek) seçili köpek olana kadar en çok 4 kez basar.</summary>
        public static IEnumerator SelectDog(LiveRaidDataSource src, int dogId)
        {
            for (int i = 0; i < 4 && src.SelectedOther != dogId; i++) yield return BoardPlayKit.PressKey(Key.Digit4);
            Assert.AreEqual(dogId, src.SelectedOther, "4 ile köpek seçilemedi · " + Where(src));
        }

        /// <summary>Uzmana ekipman ve hak (emrin yazdığı gibi; test kurulumu, kayda girmez); bu tur yeteneği yeniden açık.</summary>
        public static Baskin.Unit GearUp(LiveRaidDataSource src, string type, string gear, int left)
        {
            var u = Team(src, type);
            u.Gear = gear; u.GearLeft = left; u.Acted = false;
            src.ClearHover();
            return u;
        }

        /// <summary>Tahtaya yeni ayakta makine (türün canı, niyetsiz; planı makine evresinde kurulur). Test kurulumu.</summary>
        public static Baskin.Unit AddMachine(LiveRaidDataSource src, string type, int x, int y)
        {
            var B = src.State;
            var t = Baskin.UnitTypes[type];
            var e = new Baskin.Unit { Id = B.NextId++, Type = type, Side = 'e', X = x, Y = y, Hp = t.Hp, Max = t.Hp, Armor = t.Armor };
            B.Units.Add(e);
            src.ClearHover();
            return e;
        }

        /// <summary>Tahtaya köpek (sahibin kadro sırası; can ve yürüyüş dengeden). Test kurulumu: emirde köpekli kadronun ilk odası gibi.</summary>
        public static Baskin.Unit AddDog(LiveRaidDataSource src, int owner, int x, int y)
        {
            var B = src.State;
            var d = new Baskin.Unit { Id = B.NextId++, Type = "kopek", Side = 'p', Class = Baskin.ClassDog, Owner = owner, X = x, Y = y };
            d.Max = d.Hp = B.Moves.DogHp; d.MoveRange = B.Moves.DogMove;
            B.Units.Add(d);
            src.ClearHover();
            return d;
        }

        /// <summary>Tahtada kareye tık (gerçek <c>TileClicked</c> yolu: tahta → veri kaynağının Click'i).</summary>
        public static void ClickTile(int x, int y)
        {
            var board = Board;
            Assert.IsNotNull(board, "tahta yok");
            board.Click(x, y);
        }

        /// <summary>Uzman düşer (gövde tahtada yatar; test kurulumu) ve tahta yeniden çizilir.</summary>
        public static void MakeBody(LiveRaidDataSource src, Baskin.Unit u) { u.Dead = true; u.Hp = 0; src.ClearHover(); }

        public static void Put(Baskin.Unit u, int x, int y) { u.X = x; u.Y = y; }

        /// <summary>Tahtayı düz zemine çevirir (EditMode kitinin aynısı): kapalı kepenk ve sandık nişi duvar kalır, hedef dolap ve kasa
        /// kalır, menfez bayrakları kalır; nesne, kanal, sis kalkar. Tahta yeniden çizilir.</summary>
        public static void Floor(LiveRaidDataSource src)
        {
            var B = src.State;
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var c = Baskin.CellAt(B, x, y);
                    if (c.Shutter && c.T == "wall" || c.Crate) continue;
                    if (c.O != null && Baskin.IsTarget(c.O.K)) { if (c.T != "floor") c.T = "floor"; continue; }
                    c.T = "floor"; c.O = null; c.Smoke = false;
                }
            src.ClearHover();
        }

        /// <summary>Ekibi sol alt köşeye toplar (EditMode kitinin aynısı): menfez, kapı ve dolu kareler atlanır.</summary>
        public static void Park(LiveRaidDataSource src)
        {
            var B = src.State;
            var gates = B.Room.Forward.Concat(B.Room.West).ToList();
            var spots = new System.Collections.Generic.List<Baskin.Pt>();
            for (int y = Baskin.N - 1; y >= 0 && spots.Count < 3; y--)
                for (int x = 0; x < Baskin.N && spots.Count < 3; x++)
                {
                    var c = Baskin.CellAt(B, x, y);
                    if (c.T != "floor" || c.O != null || c.Vent > 0 || gates.Any(g => g.X == x && g.Y == y)) continue;
                    spots.Add(new Baskin.Pt(x, y));
                }
            int i = 0;
            foreach (var u in B.Units.Where(u => u.Side == 'p' && !u.AtGate && !u.Out)) { u.X = spots[i].X; u.Y = spots[i].Y; i++; }
            src.ClearHover();
        }

        /// <summary>Kalkmış bir makine ayağa kalkar ve (x, y)'ye konur (niyetsiz; planı makine evresinde kurulur).</summary>
        public static Baskin.Unit Machine(Baskin.BState B, int x, int y, int id = 0)
        {
            var e = id != 0 ? B.Units.First(v => v.Id == id) : B.Units.First(v => v.Side == 'e');
            e.Dead = false; e.Out = false; e.Cab = false; e.Frozen = false; e.Left = false; e.Hp = e.Max; e.Intent = null; e.X = x; e.Y = y;
            return e;
        }

        /// <summary>Karenin tahtadaki komşularından zemin, birimsiz, kapı karesi olmayan ve <paramref name="avoid"/> dışındaki ilki.</summary>
        public static Baskin.Pt FreeNeighbor(Baskin.BState B, Baskin.Pt p, bool orthogonal, params Baskin.Pt[] avoid)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0 || orthogonal && dx != 0 && dy != 0) continue;
                    int x = p.X + dx, y = p.Y + dy;
                    if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N) continue;
                    var c = Baskin.CellAt(B, x, y);
                    if (c.T != "floor" || c.O != null || Baskin.UnitAt(B, x, y) != null || Baskin.IsGateTile(B, x, y) || avoid.Any(a => a.X == x && a.Y == y)) continue;
                    return new Baskin.Pt(x, y);
                }
            Assert.Fail("boş komşu yok: " + p.X + "," + p.Y);
            return default;
        }

        /// <summary>
        /// Kanadı baştan sona oynar: ileri geçitler, hedef, dönüş odaları ve tesisin girişi; sonuç kartında durur. Gezilen paftaları
        /// ("1", "2", "3", "2d"…) döndürür. <paramref name="onRoom"/> her pafta açıldığında çağrılır (iddialar için).
        /// </summary>
        public static IEnumerator PlayWing(LiveRaidDataSource src, System.Collections.Generic.List<string> visits, System.Action<LiveRaidDataSource> onRoom = null)
        {
            for (int guard = 0; guard < 8 && !src.Current.Finished; guard++)
            {
                var B = src.State;
                visits.Add(B.Room.Index + (FacilityRaidText.IsReturnBoard(B) ? "d" : ""));
                onRoom?.Invoke(src);
                if (B.Room.Type == RoomType.Target && !B.Escape) yield return TakeTarget(src);
                if (Baskin.IsFinalExit(src.State)) yield return WalkOut(src);
                else yield return ThroughGate(src);
            }
            Assert.IsTrue(src.Current.Finished, "sonuç kartı gelmedi: " + string.Join(" ", visits) + " · " + Where(src));
        }
    }
}
