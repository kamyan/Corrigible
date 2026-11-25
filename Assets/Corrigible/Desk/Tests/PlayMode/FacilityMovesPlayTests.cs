using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using K = Corrigible.Desk.Tests.FacilityPlayKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis hamleleri oyunda (aşama 7; Docs/TESIS-BASKINLARI-TASARIMI.md §4, §11.5, §12 satır 7 "bütün hamleler oyunda verilir"):
    /// altı hamlenin her biri gerçek giriş yolundan verilir (E tuşu → hamle kipi, tahtaya tık → <c>Baskin.DoAction</c>), canlandırılır,
    /// telsize yazılır ve Z ile geri alınır; pusu makine evresinde makine yürüyüşünden sonra ateşlenir (evre bölünür), taşıyıcı makine
    /// evresinde düşünce yük yere düşer ve yürüyerek alınır, kapıya sürüklenen gövde sıradaki paftada yatar. Masa asset dengesiyle
    /// kurulur (asset değişmez). Işınlamalar ve makinelerin kaldırılması test kurulumudur; makine
    /// evresi senaryoları kurulumu Core'un kopyasında (<c>BState.Clone</c>, kendi RNG'si) önceden sınayarak seçer, gerçek evre aynı
    /// durumdan oynar.
    /// </summary>
    public sealed class FacilityMovesPlayTests
    {
        [UnityTearDown] public IEnumerator Teardown()
        {
            yield return K.UnloadRaid();
            yield return BoardPlayKit.TearDown();
        }

        static LiveRaidDataSource Src => K.Live;
        static Baskin.Unit U(string type) => K.Team(Src, type);
        static string Name(string type) => Baskin.UnitTypes[type].Name;
        static bool Logged(string line) => Src.Current.Log.Any(l => l.Text == line);
        static string At(string what) => what + " · pafta " + Src.State.Room?.Index + " · tur " + Src.State.Turn + " · evre " + Src.State.Phase;

        /// <summary>Son hamlenin tıktan hemen sonraki telsiz fişi.</summary>
        static string lastSlip;

        /// <summary>Makineler kalkar, tahta düz zemin, ekip köşede (EditMode sunum testlerinin kurulumu).</summary>
        static IEnumerator Ready()
        {
            yield return K.Settle(Src, "kurulum öncesi");
            K.Calm(Src); K.Floor(Src); K.Park(Src);
            yield return K.Settle(Src, "kurulum");
        }

        /// <summary>
        /// Hamleyi oyuncunun yolundan verir: uzman seçilir, E'ye hamle kurulana kadar basılır (kartın satırı kurulu, hedef çerçeveli),
        /// tahtadaki hedefe tıklanır; geri alma görüntüsü alındı, fiş yazıldı, canlandırma bitti.
        /// </summary>
        static IEnumerator Give(Baskin.Unit u, string kind, int x, int y, int other = 0)
        {
            yield return K.Settle(Src, kind + " öncesi");
            Src.ClearHover();
            Src.Select(K.Letter(u));
            Assert.AreEqual(K.Letter(u), Src.Selected, At("seçilemedi: " + u.Type));
            yield return K.ArmByKey(Src, kind, other);
            var vm = Src.Current;
            Assert.IsTrue(vm.ActionArmed, At(kind + ": kip açık"));
            Assert.GreaterOrEqual(vm.ActionKey, 0, At(kind + ": E fişinin satırı"));
            // Kartta tek kurulu satır var ve o, bu hamlenin satırı (ad bağımsız beklenenle: elle); E fişi o satırda
            var armed = vm.ActionRows.Where(r => r.Armed).ToArray();
            Assert.AreEqual(1, armed.Length, At(kind + ": kartta tek kurulu satır"));
            Assert.AreEqual(ExpectedName(kind, x, y, other), armed[0].Text, At(kind + ": kurulu satır bu hamlenin"));
            Assert.AreSame(armed[0], vm.ActionRows[vm.ActionKey], At(kind + ": E fişi kurulu satırda"));
            CollectionAssert.Contains(vm.AbilityTargets, new Vector2Int(x, y), At(kind + ": hedef çerçeveli"));
            int hist = Src.State.Hist.Count;
            K.ClickTile(x, y);
            Assert.AreEqual(hist + 1, Src.State.Hist.Count, At(kind + ": tahtaya tık hamleyi vermedi"));
            Assert.IsFalse(Src.ActionArmed, At(kind + ": başarılı hamle kipi kapatır"));
            lastSlip = Src.Current.EventLine;
            Assert.IsFalse(string.IsNullOrEmpty(lastSlip), At(kind + ": telsiz fişi"));
            yield return K.Settle(Src, kind);
        }

        /// <summary>Hamlenin kart satırındaki adı, kurulumdan elle (sürüklemede gövdenin kısa adı; işte hedef karenin türü).</summary>
        static string ExpectedName(string kind, int x, int y, int other)
        {
            switch (kind)
            {
                case Baskin.ActAmbush: return Loc.T("Pusu");
                case Baskin.ActRush: return Loc.T("Koşu darbesi");
                case Baskin.ActDrag:
                    var body = Src.State.Units.First(u => u.Id == other);
                    return Loc.F("Sürükle: {0}", string.IsNullOrEmpty(body.PersonName) ? Name(body.Type) : body.PersonName);
                case Baskin.ActThrow: return Loc.T("Fırlat");
                case Baskin.ActJoint: return Baskin.CellAt(Src.State, x, y).Crate ? Loc.T("Büyük sandığı aç") : Loc.T("Kepengi kaldır");
                case Baskin.ActVent: return Loc.T("Menfez");
                default: Assert.Fail("bilinmeyen hamle: " + kind); return null;
            }
        }

        /// <summary>Veri kaynağının HUD'u (sahnedeki SerializeField başvurusu; pasifken de).</summary>
        static RaidHudView Hud
        {
            get
            {
                var field = typeof(RaidDataSource).GetField("hud", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(field, "RaidDataSource.hud alanı");
                var hud = field.GetValue(Src) as RaidHudView;
                Assert.IsNotNull(hud, "veri kaynağının HUD'u bağlı");
                return hud;
            }
        }

        /// <summary>Z tuşu (gerçek klavye yolu) son hamleyi geri alır.</summary>
        static IEnumerator UndoByKey(string what)
        {
            yield return K.Settle(Src, what + " geri alma öncesi");
            int hist = Src.State.Hist.Count;
            Assert.Greater(hist, 0, At(what + ": geri alınacak hamle yok"));
            yield return BoardPlayKit.PressKey(Key.Z);
            Assert.AreEqual(hist - 1, Src.State.Hist.Count, At(what + ": Z geri almadı"));
            yield return K.Settle(Src, what + " geri alma");
        }

        /// <summary>
        /// EMRİ İLET (onay sorulursa bir kez daha) ve makine evresi biter: evre boyunca her karede <paramref name="sample"/> çağrılır
        /// (olay fişi, görünür telsiz satırları, tahta).
        /// </summary>
        static IEnumerator EndTurnSampling(System.Action sample)
        {
            yield return K.Settle(Src, "tur öncesi");
            int turn = Src.State.Turn;
            Src.EndTurn();
            if (Src.Current.EndConfirm) Src.EndTurn();
            float until = Time.realtimeSinceStartup + 15f;
            while ((Src.Executing || Src.Busy || Src.State.Turn == turn && Src.State.Phase == "player") && Time.realtimeSinceStartup < until)
            {
                if (Src.Executing) sample();
                yield return null;
            }
            Assert.IsFalse(Src.Executing, At("makine evresi bitmedi"));
            yield return K.Settle(Src, "makine evresi");
        }

        [UnityTest]
        public IEnumerator Moves_AllSix_GivenInPlay()
        {
            yield return K.OpenRaid();
            var B = Src.State;
            Assert.AreEqual(1, B.Room.Index, "ilk pafta");
            var board = K.Board;
            var given = new HashSet<string>();
            yield return Ready();

            // ---------- Pafta 1 · pusu: Teknisyen kuzeye ----------
            K.Put(U("tek"), 3, 4); Src.ClearHover();
            yield return Give(U("tek"), Baskin.ActAmbush, 3, 3);
            Assert.AreEqual(0, U("tek").AmbushDir, "pusu kuzeye");
            Assert.IsTrue(Logged(Loc.F("{0} pusu kurdu.", Name("tek"))), "telsizde Core satırı");
            StringAssert.Contains(" · pusu · ", lastSlip);
            Assert.AreEqual(1, board.AmbushLineCount, "pusu hattı tahtada");
            yield return UndoByKey("pusu");
            Assert.AreEqual(-1, U("tek").AmbushDir, "geri alınan pusu");
            Assert.AreEqual(0, board.AmbushLineCount, "geri alınan pusunun hattı yok");
            yield return Give(U("tek"), Baskin.ActAmbush, 3, 3);
            Assert.AreEqual(0, U("tek").AmbushDir);
            given.Add(Baskin.ActAmbush);

            // ---------- Pafta 1 · koşu darbesi: EMP uzmanı düz hatta, makine 3 kare ötede ----------
            K.Park(Src);
            K.Put(U("emp"), 3, 1);
            int machine = K.Machine(B, 3, 5).Id;
            Src.ClearHover();
            yield return Give(U("emp"), Baskin.ActRush, 3, 5);
            Assert.AreEqual((3, 4), (U("emp").X, U("emp").Y), "koşucu makinenin önünde durur");
            Assert.Greater(B.Units.First(u => u.Id == machine).Y, 5, "makine itildi");
            Assert.AreEqual((3, 4), board.PieceTile(U("emp").Id), "koşucunun parçası durduğu karede");
            Assert.IsTrue(Logged(Loc.F("{0} koşu darbesi indirdi.", Name("emp"))));
            StringAssert.Contains(" · koşu darbesi → ", lastSlip);
            yield return UndoByKey("koşu");
            Assert.AreEqual((3, 1), (U("emp").X, U("emp").Y), "geri alınan koşu");
            yield return Give(U("emp"), Baskin.ActRush, 3, 5);
            given.Add(Baskin.ActRush);
            Src.State.Units.First(u => u.Id == machine).Dead = true;

            // ---------- Pafta 1 · sürükleme: Teknisyen yanında yatan EMP uzmanını iki kare çeker ----------
            K.Park(Src);
            var dragger = U("tek"); dragger.Moved = dragger.Acted = false;   // kurulum: Teknisyen'in bu tur hakları yeniden
            K.Put(dragger, 3, 3); K.Put(U("emp"), 2, 3); K.MakeBody(Src, U("emp"));
            int body = U("emp").Id;
            yield return Give(U("tek"), Baskin.ActDrag, 5, 3, body);
            Assert.AreEqual((5, 3), (U("tek").X, U("tek").Y), "sürükleyen varışta");
            Assert.AreEqual((4, 3), (U("emp").X, U("emp").Y), "gövde yolun sondan bir önceki karesinde");
            Assert.AreEqual((4, 3), board.PieceTile(body), "gövdenin parçası arkasından geldi");
            StringAssert.Contains(" sürükleniyor → ", lastSlip, "sürüklemenin fişi (Core satırı yok)");
            yield return UndoByKey("sürükleme");
            Assert.AreEqual((2, 3), (U("emp").X, U("emp").Y), "geri alınan sürükleme");
            yield return Give(U("tek"), Baskin.ActDrag, 5, 3, body);
            given.Add(Baskin.ActDrag);

            // ---------- Pafta 1 · fırlatma: yük Teknisyen'den Analist'e ----------
            var emp = U("emp"); emp.Dead = false; emp.Hp = emp.Max;
            K.Park(Src);
            var tek = U("tek"); tek.Moved = tek.Acted = false;   // kurulum: Teknisyen'in bu tur hakları yeniden
            K.Put(tek, 3, 3); K.Put(U("ana"), 3, 5);
            B.Carrier = tek.Id;
            Src.ClearHover();
            yield return Give(tek, Baskin.ActThrow, 3, 5);
            Assert.AreEqual(U("ana").Id, B.Carrier, "yük Analist'te");
            Assert.IsTrue(Logged(Loc.F("{0} yükü fırlattı; yükü artık {1} taşıyor.", Name("tek"), Name("ana"))));
            StringAssert.Contains(" · yük → ", lastSlip);
            yield return UndoByKey("fırlatma");
            Assert.AreEqual(U("tek").Id, Src.State.Carrier, "geri alınan fırlatma");
            yield return Give(U("tek"), Baskin.ActThrow, 3, 5);
            given.Add(Baskin.ActThrow);

            yield return K.ThroughGate(Src);
            B = Src.State;
            Assert.AreEqual(2, B.Room.Index, "pafta 2");
            Assert.IsTrue(B.Room.Shutter.HasValue && B.Room.Vents.Count >= 2, "pafta 2'de kepenk ve menfez bekleniyor: " + B.Room.Id);
            yield return Ready();

            // ---------- Pafta 2 · iki kişilik iş: kepenk (tut, sonra aç) ----------
            var s = B.Room.Shutter.Value;
            var n1 = K.FreeNeighbor(B, s, false, B.Room.Vents.ToArray()); K.Put(U("tek"), n1.X, n1.Y);
            var n2 = K.FreeNeighbor(B, s, false, B.Room.Vents.Concat(new[] { n1 }).ToArray()); K.Put(U("emp"), n2.X, n2.Y);
            Src.ClearHover();
            yield return Give(U("tek"), Baskin.ActJoint, s.X, s.Y);
            Assert.AreEqual(U("tek").Id, Src.State.HoldId, "Teknisyen kepengi tutuyor");
            Assert.IsTrue(Logged(Loc.F("{0} kepengi tutuyor: ikinci uzman bu tur gelmeli.", Name("tek"))));
            yield return UndoByKey("tutuş");
            Assert.AreEqual(0, Src.State.HoldId, "geri alınan tutuş");
            yield return Give(U("tek"), Baskin.ActJoint, s.X, s.Y);
            yield return Give(U("emp"), Baskin.ActJoint, s.X, s.Y);
            Assert.AreEqual("floor", Baskin.CellAt(Src.State, s.X, s.Y).T, "kepenk kalktı");
            Assert.IsTrue(Logged(Loc.T("Kepenk kalktı.")));
            StringAssert.Contains("kepenk kalktı · ", lastSlip);
            yield return UndoByKey("kepenk");
            Assert.AreEqual("wall", Baskin.CellAt(Src.State, s.X, s.Y).T, "geri alınan açılış");
            yield return Give(U("emp"), Baskin.ActJoint, s.X, s.Y);
            Assert.AreEqual("floor", Baskin.CellAt(Src.State, s.X, s.Y).T);
            given.Add(Baskin.ActJoint);

            // ---------- Pafta 2 · menfez: Analist yakın uca yürür, öbür uçtan çıkar ----------
            var v0 = B.Room.Vents[0]; var v1 = B.Room.Vents[1];
            var near = K.FreeNeighbor(Src.State, v0, true, v1);
            K.Put(U("ana"), near.X, near.Y);
            Src.ClearHover();
            yield return Give(U("ana"), Baskin.ActVent, v1.X, v1.Y);
            Assert.AreEqual((v1.X, v1.Y), (U("ana").X, U("ana").Y), "Analist öbür uçta");
            Assert.AreEqual((v1.X, v1.Y), board.PieceTile(U("ana").Id), "parça öbür uçta");
            Assert.IsTrue(U("ana").Moved && !U("ana").Acted, "menfez yürüyüştür, yetenek kalır");
            Assert.IsTrue(Logged(Loc.F("{0} menfezden geçti.", Name("ana"))));
            yield return UndoByKey("menfez");
            Assert.AreEqual((near.X, near.Y), (U("ana").X, U("ana").Y), "geri alınan menfez");
            yield return Give(U("ana"), Baskin.ActVent, v1.X, v1.Y);
            given.Add(Baskin.ActVent);

            yield return K.ThroughGate(Src);
            B = Src.State;
            Assert.AreEqual(3, B.Room.Index, "pafta 3");
            Assert.IsTrue(B.Room.BigCrate.HasValue, "pafta 3'te büyük sandık bekleniyor: " + B.Room.Id);
            yield return Ready();

            // ---------- Pafta 3 · iki kişilik iş: büyük sandık ----------
            var c = B.Room.BigCrate.Value;
            var c1 = K.FreeNeighbor(B, c, false); K.Put(U("tek"), c1.X, c1.Y);
            var c2 = K.FreeNeighbor(B, c, false, c1); K.Put(U("emp"), c2.X, c2.Y);
            Src.ClearHover();
            yield return Give(U("tek"), Baskin.ActJoint, c.X, c.Y);
            Assert.AreEqual(U("tek").Id, Src.State.HoldId);
            Assert.IsTrue(Logged(Loc.F("{0} büyük sandığı tutuyor: ikinci uzman bu tur gelmeli.", Name("tek"))));
            yield return Give(U("emp"), Baskin.ActJoint, c.X, c.Y);
            Assert.IsTrue(Src.State.CrateOpen, "büyük sandık açıldı");
            Assert.IsTrue(Logged(Loc.T("Büyük sandık açıldı.")));
            StringAssert.Contains("büyük sandık açıldı · ", lastSlip);
            yield return UndoByKey("sandık");
            Assert.IsFalse(Src.State.CrateOpen, "geri alınan açılış");
            yield return Give(U("emp"), Baskin.ActJoint, c.X, c.Y);
            Assert.IsTrue(Src.State.CrateOpen);

            CollectionAssert.AreEquivalent(Baskin.ActionKinds, given, "altı hamlenin hepsi oyunda verildi");
            Assert.AreEqual(3, Src.State.Room.Index, "sahne yeniden yüklenmeden üç pafta");
        }

        [UnityTest]
        public IEnumerator Ambush_FiresAfterMachineWalk_SlipLogLine()
        {
            yield return K.OpenRaid();
            yield return Ready();
            // Tahtada yalnız Teknisyen kalır (öbürleri kapıda): makinenin planı onu hedefler
            yield return K.ToGate(Src, U("emp"));
            yield return K.ToGate(Src, U("ana"));
            var B = Src.State;
            var tek = U("tek");
            K.Put(tek, 3, 4);
            Src.ClearHover();
            yield return K.Settle(Src, "kurulum");

            // Kurulum Core'un kopyasında seçilir: makine yürüyüp yeni karesinde pusuya girmeli (evrenin bölünmesini sınar)
            int machine = 0; Baskin.Pt start = default, aim = default; Baskin.Fx expected = null;
            var aims = Baskin.ActionTargets(B, tek, Baskin.ActAmbush);
            var enemies = B.Units.Where(u => u.Side == 'e').Select(u => u.Id).Take(4).ToList();
            foreach (var t in aims)
            {
                foreach (var id in enemies)
                {
                    for (int y = 0; y < Baskin.N && expected == null; y++)
                        for (int x = 0; x < Baskin.N && expected == null; x++)
                        {
                            if (Baskin.CellAt(B, x, y).T != "floor" || Baskin.CellAt(B, x, y).O != null || Baskin.UnitAt(B, x, y) != null || Baskin.IsGateTile(B, x, y)) continue;
                            if (x == tek.X || y == tek.Y) continue;   // pusu hattında ya da Teknisyen'in hizasında başlamaz
                            var copy = B.Clone();
                            K.Machine(copy, x, y, id);
                            if (!Baskin.DoAction(copy, tek.Id, Baskin.ActAmbush, t.X, t.Y)) continue;
                            if (!Baskin.EndTurn(copy)) continue;
                            var hit = copy.FxList.FirstOrDefault(f => f.K == "ambush-hit" && f.Unit == id);
                            if (hit == null || hit.X == x && hit.Y == y) continue;
                            // Vuruş can götürmeli (zırh ya da buz kabuğu karşılamasın: kayıt sayımı ve can iddiası)
                            var victim = copy.Units.First(u => u.Id == id);
                            if (!victim.Dead && victim.Hp >= victim.Max) continue;
                            machine = id; start = new Baskin.Pt(x, y); aim = t; expected = hit;
                        }
                    if (expected != null) break;
                }
                if (expected != null) break;
            }
            Assert.IsNotNull(expected, "yürüyüp pusuya giren makine kurulumu bulunamadı · " + At("kurulum"));
            Debug.Log("[FacilityMovesPlayTests] pusu " + aim.X + "," + aim.Y + " · makine #" + machine + " " + start.X + "," + start.Y + " → " + expected.X + "," + expected.Y);

            var m = K.Machine(B, start.X, start.Y, machine);
            int hp = m.Hp;
            Src.ClearHover();
            yield return Give(U("tek"), Baskin.ActAmbush, aim.X, aim.Y);
            var board = K.Board;
            Assert.AreEqual(1, board.AmbushLineCount, "pusu hattı tahtada");

            (int x, int y)? hitTile = null, pieceAt = null; Baskin.Fx hitFx = null;
            void OnEffect(Baskin.Fx f, int i, int n)
            {
                if (f.K != "ambush-hit" || hitFx != null) return;
                hitFx = f; hitTile = (f.X, f.Y); pieceAt = board.PieceTile(f.Unit);
            }
            string slip = null;
            string coreLine = Loc.F("Pusu: {0} vuruldu.", Name(m.Type));
            bool slipSeen = false, logSeen = false, lineDuring = false;
            board.EffectPlaying += OnEffect;
            try
            {
                yield return EndTurnSampling(() =>
                {
                    if (hitFx != null && slip == null) slip = FacilityMoveText.EffectSlip(hitFx, Src.State);
                    var line = Src.Current.EventLine;
                    if (slip != null && line != null && line.StartsWith(slip)) slipSeen = true;
                    if (Logged(coreLine)) logSeen = true;
                    if (hitFx == null && board.AmbushLineCount > 0) lineDuring = true;
                });
            }
            finally { board.EffectPlaying -= OnEffect; }

            Assert.IsNotNull(hitFx, "makine evresinde pusu ateşlendi");
            Assert.AreEqual((expected.X, expected.Y), hitTile, "pusu kopyada bulunan karede");
            Assert.AreNotEqual((start.X, start.Y), hitTile, "makine yürüdü");
            Assert.AreEqual(hitTile, pieceAt, "pusu anında makinenin parçası yeni karesinde (evre bölündü: önce yürüyüş, sonra pusu)");
            Assert.IsTrue(lineDuring, "hat ateşlenene kadar tahtada kalır");
            Assert.IsTrue(slipSeen, "evre boyunca olay fişinde pusu satırı: " + slip);
            Assert.IsTrue(logSeen, "evre boyunca telsiz kaydında Core satırı (görünür satır sayımı): " + coreLine);
            Assert.IsTrue(Logged(coreLine));
            var after = Src.State.Units.First(u => u.Id == machine);
            Assert.IsTrue(after.Dead || after.Hp == hp - 1, "makinenin canı −1");
            Assert.AreEqual(0, board.AmbushLineCount, "evre sonunda pusu hattı silinir");
        }

        [UnityTest]
        public IEnumerator CargoDrop_MachinePhase_FloorAndPickup()
        {
            yield return K.OpenRaid();
            yield return Ready();
            yield return K.ToGate(Src, U("ana"));
            var B = Src.State;
            var tek = U("tek");
            K.Put(tek, 3, 3);
            tek.Hp = 1; tek.Armor = 0; tek.Ice = 0;   // kurulum: ilk darbe düşürür (plaka ya da buz karşılamaz)
            B.Carrier = tek.Id;
            Src.ClearHover();
            yield return K.Settle(Src, "kurulum");
            var emp = U("emp");

            // Kurulum Core'un kopyasında seçilir: tek makine Teknisyen'in yanında, en çok 3 turda taşıyıcı düşer, EMP uzmanı ayakta kalır
            int machine = 0, turns = 0; Baskin.Pt start = default;
            foreach (var id in B.Units.Where(u => u.Side == 'e').Select(u => u.Id).Take(4).ToList())
            {
                foreach (var d in Baskin.Dirs)
                {
                    int x = tek.X + d[0], y = tek.Y + d[1];
                    if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N || Baskin.CellAt(B, x, y).T != "floor" || Baskin.UnitAt(B, x, y) != null || Baskin.IsGateTile(B, x, y)) continue;
                    var copy = B.Clone();
                    K.Machine(copy, x, y, id);
                    int n = 0;
                    for (int k = 1; k <= 3; k++)
                    {
                        if (!Baskin.EndTurn(copy)) break;
                        if (copy.Phase != "player") break;
                        if (copy.FxList.Any(f => f.K == "cargo-drop")) { n = k; break; }
                    }
                    var e = copy.Units.First(u => u.Id == emp.Id);
                    if (n == 0 || e.Dead || copy.CargoX < 0 || copy.Phase != "player") continue;
                    machine = id; start = new Baskin.Pt(x, y); turns = n;
                    break;
                }
                if (machine != 0) break;
            }
            Assert.AreNotEqual(0, machine, "taşıyıcıyı düşüren makine kurulumu bulunamadı · " + At("kurulum"));
            Debug.Log("[FacilityMovesPlayTests] yük · makine #" + machine + " " + start.X + "," + start.Y + " · " + turns + " tur");
            K.Machine(B, start.X, start.Y, machine);
            Src.ClearHover();

            var board = K.Board;
            string coreLine = Loc.F("{0} düştü: yük yere düştü. Kareye giren ilk ekip üyesi alır.", Name("tek"));
            string slipHead = Loc.F("yük yere düştü · {0}", "");
            bool slipSeen = false, logSeen = false, shownDuring = false;
            // Amaç satırı evre içinde yükün düşüşünü önceden söylemez: cargo-drop oynayana kadar evreden önceki amaç (sunum kopyası)
            string goalBefore = Src.Current.Goal;
            StringAssert.DoesNotContain(Loc.F("Amaç: yükü yerden al ({0})", ""), goalBefore ?? "", "kurulumda yük taşıyıcıda");
            bool dropped = false; int beforeDrop = 0; string earlyGoal = null;
            void OnDrop(Baskin.Fx f, int i, int n) { if (f.K == "cargo-drop") dropped = true; }
            board.EffectPlaying += OnDrop;
            try
            {
                for (int k = 1; k <= turns; k++)
                    yield return EndTurnSampling(() =>
                    {
                        var line = Src.Current.EventLine;
                        if (line != null && line.StartsWith(slipHead)) slipSeen = true;
                        if (Logged(coreLine)) logSeen = true;
                        if (board.CargoShown) shownDuring = true;
                        if (!dropped) { beforeDrop++; if (Src.Current.Goal != goalBefore) earlyGoal ??= Src.Current.Goal; }
                    });
            }
            finally { board.EffectPlaying -= OnDrop; }
            Assert.IsTrue(dropped, "cargo-drop evrede oynadı");
            Assert.Greater(beforeDrop, 0, "evre içinde cargo-drop'tan önce örnek alındı");
            Assert.IsNull(earlyGoal, "evre içinde cargo-drop'tan önce amaç satırı değişmedi");
            B = Src.State;
            Assert.IsTrue(U("tek").Dead, "taşıyıcı düştü");
            Assert.GreaterOrEqual(B.CargoX, 0, "yük yerde");
            Assert.AreEqual(0, B.Carrier);
            Assert.IsTrue(slipSeen, "evre boyunca olay fişinde yük satırı");
            Assert.IsTrue(logSeen, "evre boyunca telsiz kaydında Core satırı (görünür satır sayımı)");
            Assert.IsTrue(shownDuring, "yük parçası evre içinde (Sync'ten önce) belirdi");
            Assert.IsTrue(board.CargoShown, "yük parçası tahtada");
            Assert.IsTrue(Src.Current.CargoOnFloor, "görev çipi: yük yerde");
            Assert.AreEqual(Loc.F("Amaç: yükü yerden al ({0})", FacilityRaidText.Cell(B.Room.Index, B.CargoX, B.CargoY)), Src.Current.Goal);
            var stampsField = typeof(RaidHudView).GetField("brokenStamps", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(stampsField, "RaidHudView.brokenStamps alanı");
            var stamps = stampsField.GetValue(Hud) as TMP_Text[];
            Assert.IsNotNull(stamps, "görev çipinin damgaları");
            Assert.Greater(stamps.Length, 0, "görev çipinin damgaları");
            Assert.IsTrue(stamps.Any(t => t != null && t.text == Loc.T("YERDE")), "görev çipinin damgası YERDE");

            // EMP uzmanı yükün yanına (kurulum), sonra tahtaya tıkla yürür ve yükü alır
            int cx = B.CargoX, cy = B.CargoY;
            emp = U("emp");
            var beside = K.FreeNeighbor(B, new Baskin.Pt(cx, cy), true);
            K.Put(emp, beside.X, beside.Y);
            Src.ClearHover();
            yield return K.Settle(Src, "alım öncesi");
            Src.Select(K.Letter(emp));
            Assert.AreEqual(K.Letter(emp), Src.Selected, At("EMP seçilemedi"));
            K.ClickTile(cx, cy);
            Assert.AreEqual(emp.Id, Src.State.Carrier, "yürüyen uzman yükü aldı");
            Assert.AreEqual(FacilityMoveText.PickupSlip(Src.State, Src.Current.Team.First(t => t.Letter == K.Letter(emp)).Name, cx, cy), Src.Current.EventLine, "yük alma fişi");
            yield return K.Settle(Src, "yük alımı");
            Assert.IsFalse(board.CargoShown, "alınan yükün parçası kalktı");
            Assert.IsFalse(Src.Current.CargoOnFloor);
        }

        [UnityTest]
        public IEnumerator Drag_ThroughGate_BodyNextSheet()
        {
            yield return K.OpenRaid();
            yield return Ready();
            var B = Src.State;
            var g = Baskin.GateTiles(B)[0];
            int dir = g.X >= 4 ? -1 : 1;
            K.Put(U("tek"), g.X + dir, g.Y); K.Put(U("emp"), g.X + 2 * dir, g.Y); K.MakeBody(Src, U("emp"));
            int body = U("emp").Id;
            yield return Give(U("tek"), Baskin.ActDrag, g.X, g.Y, body);
            Assert.IsTrue(U("tek").AtGate && U("emp").AtGate, "ikisi kapıda");
            Assert.AreEqual(Loc.T("GÖVDE KAPIDA"), Src.Current.Team.First(t => t.Letter == 'E').DownText, "ekip kartı");
            Assert.IsNull(K.Board.PieceTile(body), "kapıdaki gövde tahtadan kalktı");
            var notes = Src.MarksFor(Src.Current).Notes;
            Assert.IsTrue(notes.Contains((g.Y * Baskin.N + g.X, Loc.F("KAPIDA {0}/{1}", 1, 2))), "kapı notu ayaktakileri sayar (gövde sayılmaz): " + string.Join(", ", notes.Select(n => n.text)));

            yield return K.ThroughGate(Src);
            B = Src.State;
            Assert.AreEqual(2, B.Room.Index, "pafta 2");
            var lying = B.Units.Single(u => u.Side == 'p' && u.Type == "emp");
            Assert.IsTrue(Baskin.LyingBody(lying), "gövde sıradaki paftada yatar");
            Assert.AreEqual((lying.X, lying.Y), K.Board.PieceTile(lying.Id), "gövdenin parçası tahtada");
            StringAssert.StartsWith(Loc.F("YERDE · {0}", FacilityRaidText.Cell(2, lying.X, lying.Y)), Src.Current.Team.First(t => t.Letter == 'E').DownText);
        }

        /// <summary>
        /// Son nefes (aşama 6 Core'u, Unit.Breath): Teknisyen ve Analist kapıda, tahtadaki son uzman makine evresinde düşer, oda
        /// kendiliğinden geçilir. Pafta çevrilmeden önce gövdenin parçası ekiple tahtadan kalkar ve ekip kartı yerde demez (EKİPLE
        /// TAŞINIYOR); sıradaki paftada gövde yatar, telsizde Core'un son nefes satırı ve kapanışın "ekiple taşınıyor" satırı var.
        /// Kurulum Core'un kopyasında seçilir.
        /// </summary>
        [UnityTest]
        public IEnumerator LastBreath_AutoPass_BodyLeavesWithTeam()
        {
            yield return K.OpenRaid();
            yield return Ready();
            yield return K.ToGate(Src, U("tek"));
            yield return K.ToGate(Src, U("ana"));
            var B = Src.State;
            Assert.IsTrue(B.Moves.LastBreath, "son nefes kuralı açık (varsayılan denge)");
            var emp = U("emp");
            K.Put(emp, 3, 3);
            emp.Hp = 1; emp.Armor = 0; emp.Ice = 0;   // kurulum: ilk darbe düşürür
            Src.ClearHover();
            yield return K.Settle(Src, "kurulum");
            int empId = emp.Id;

            int machine = 0, turns = 0; Baskin.Pt start = default;
            foreach (var id in B.Units.Where(u => u.Side == 'e').Select(u => u.Id).Take(6).ToList())
            {
                foreach (var d in Baskin.Dirs)
                {
                    int x = emp.X + d[0], y = emp.Y + d[1];
                    if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N || Baskin.CellAt(B, x, y).T != "floor" || Baskin.UnitAt(B, x, y) != null || Baskin.IsGateTile(B, x, y)) continue;
                    var copy = B.Clone();
                    K.Machine(copy, x, y, id);
                    int n = 0;
                    for (int k = 1; k <= 3 && n == 0; k++)
                    {
                        if (!Baskin.EndTurn(copy)) break;
                        if (copy.Phase != "player") n = k;
                    }
                    if (n == 0 || copy.Phase != "pass" || !copy.Units.First(u => u.Id == empId).Breath) continue;
                    machine = id; start = new Baskin.Pt(x, y); turns = n;
                    break;
                }
                if (machine != 0) break;
            }
            Assert.AreNotEqual(0, machine, "son uzmanı düşürüp odayı kendiliğinden geçiren makine kurulumu bulunamadı · " + At("kurulum"));
            Debug.Log("[FacilityMovesPlayTests] son nefes · makine #" + machine + " " + start.X + "," + start.Y + " · " + turns + " tur");
            K.Machine(B, start.X, start.Y, machine);
            Src.ClearHover();
            var board = K.Board;
            for (int k = 1; k < turns; k++) yield return EndTurnSampling(() => { });

            // Son evre: oda kendiliğinden geçilir; pafta çevrilene kadar (evre bittikten sonra) kart ve tahta örneklenir
            yield return K.Settle(Src, "son evre öncesi");
            Src.EndTurn();
            if (Src.Current.EndConfirm) Src.EndTurn();
            string downText = null; bool pieceGone = false, window = false;
            float until = Time.realtimeSinceStartup + 20f;
            while (Src.State.Room.Index == 1 && Time.realtimeSinceStartup < until)
            {
                if (!Src.Executing && Src.State.Phase == "pass")
                {
                    window = true;
                    downText ??= Src.Current.Team.First(t => t.Letter == 'E').DownText;
                    if (board.PieceTile(empId) == null) pieceGone = true;
                }
                yield return null;
            }
            Assert.IsTrue(window, "evre bitti, oda geçildi (pass) ve pafta çevrilmeden önce örneklendi · " + At("son evre"));
            Assert.AreEqual(Loc.T("EKİPLE TAŞINIYOR"), downText, "ekip kartı yerde demez");
            Assert.IsTrue(pieceGone, "gövdenin parçası ekiple tahtadan kalktı");
            yield return K.Settle(Src, "pafta çevirme");

            Assert.AreEqual(2, Src.State.Room.Index, "sıradaki pafta");
            var placed = Src.State.Units.Single(u => u.Side == 'p' && u.Type == "emp");
            Assert.IsTrue(Baskin.LyingBody(placed) && !placed.Breath, "ekiple giden gövde sıradaki paftada yatar");
            Assert.AreEqual((placed.X, placed.Y), K.Board.PieceTile(placed.Id), "gövdenin parçası sıradaki paftada");
            StringAssert.StartsWith(Loc.F("YERDE · {0}", FacilityRaidText.Cell(2, placed.X, placed.Y)), Src.Current.Team.First(t => t.Letter == 'E').DownText);
            Assert.IsTrue(Logged(Loc.F("{0} son nefeste düştü; ekip onu da götürdü.", Name("emp"))), "Core'un son nefes satırı");
            Assert.IsTrue(Logged(Loc.F("{0} ekiple taşınıyor", FacilityRaidText.PersonName(Src.State.Params, placed.Slot))), "kapanışın satırı");
        }
    }
}
