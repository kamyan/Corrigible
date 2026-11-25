using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis baskınının sunum testlerinin ortak kurulumu (aşama 3): olaysız yeni oyun (tohum 79), harekâtın ilk baskın emri
    /// (karargâhın ilk kanadı), makinesiz tahta, birimsiz kapı karesi ve ayaktakilerin kapıya girişi. Yalnız public API kullanır (Desk
    /// test derlemesi Core'un internal'larını görmez). Işınlama test kurulumudur; komutlar gerçek yoldan gider.
    /// </summary>
    public static class FacilityDeskKit
    {
        /// <summary>Sığınak olayları kapalı denge (tesis tek baskın kipi; sahadaki sızmacı C# varsayılanıyla açık).</summary>
        public static Balance Bal() => new Balance { shelterEventsEnabled = false };

        /// <summary>Olaysız yeni oyun.</summary>
        public static Simulation New(uint seed = 79) => Simulation.NewGame(seed, Bal());

        /// <summary>Harekâtın ilk baskın emri (bulunulan bölgenin ilk alınmamış sektörü); Ok iddiası. Sektör kimliğini döndürür.</summary>
        public static string StartFirst(Simulation sim)
        {
            var sector = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.IsNotNull(sector, "alınacak sektör yok");
            var r = sim.StartLiberationRaid(sector.Id);
            Assert.IsTrue(r.Ok, "StartLiberationRaid: " + r.Msg);
            return sector.Id;
        }

        /// <summary>Makineleri devre dışı bırakır, uyanma işaretlerini siler ve uyanmayı kapatır (testin kurduğu kareler bozulmasın).</summary>
        public static void Calm(Baskin.BState B)
        {
            foreach (var e in B.Units.Where(u => u.Side == 'e' && !u.Dead)) { e.Dead = true; e.Intent = null; }
            B.Spawns.Clear();
            B.SpawnPerTurn = 0;
        }

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

        /// <summary>Tahtadaki ayaktaki uzmanlar, kadro sırasıyla.</summary>
        public static Baskin.Unit[] Standing(Baskin.BState B) =>
            B.Units.Where(u => u.Side == 'p' && Baskin.OnBoard(u)).OrderBy(u => u.Slot).ToArray();

        /// <summary>Ayaktaki her uzmanı boş bir kapı karesine ışınlar ve Core'dan kapıya sokar (Baskin.EnterGate; Assert).</summary>
        public static void ToGateCore(Baskin.BState B)
        {
            foreach (var u in Standing(B))
            {
                var g = FreeGate(B, u);
                Assert.IsTrue(g.HasValue, "boş kapı karesi yok: " + B.Room?.Id);
                u.X = g.Value.X;
                u.Y = g.Value.Y;
                Assert.IsTrue(Baskin.EnterGate(B, u.Id), "EnterGate: " + B.Room?.Id);
            }
        }

        /// <summary>Kaçışı başlatır (hedef odası): hedefler kalkar, ikmalde Teknisyen (yoksa ayaktaki ilk uzman) taşıyıcı olur.</summary>
        public static void StartEscape(Baskin.BState B)
        {
            foreach (var t in Baskin.TargetsLeft(B)) Baskin.CellAt(B, t.X, t.Y).O = null;
            if (Baskin.IsLoot(B))
            {
                var team = Standing(B);
                B.Carrier = (team.FirstOrDefault(u => u.Type == "tek") ?? team.First()).Id;
            }
            B.Escape = true;
        }

        /// <summary>
        /// Betikli oda: makineler kalkar (<paramref name="keep"/> makine ayakta kalır: oda temiz sayılmaz), hedef odasında kaçış
        /// başlar, ayaktakiler kapıya girer ve herkes kapıdaysa GEÇ verilir (tesisin girişinde tahliye: baskın biter).
        /// </summary>
        public static void PlayRoom(Baskin.BState B, int keep = 0)
        {
            var foes = B.Units.Where(u => u.Side == 'e' && !u.Dead).ToList();
            foreach (var e in foes.Skip(keep)) { e.Dead = true; e.Intent = null; }
            foreach (var e in foes.Take(keep)) if (Baskin.IsGateTile(B, e.X, e.Y)) { e.Dead = true; e.Intent = null; }
            B.Spawns.Clear();
            B.SpawnPerTurn = 0;
            if (B.Room.Type == RoomType.Target && !B.Escape) StartEscape(B);
            ToGateCore(B);
            if (Baskin.CanPass(B)) Assert.IsTrue(Baskin.Pass(B), "GEÇ: " + B.Room.Id);
        }

        /// <summary>
        /// Aşama 9: süren tesis baskınını komutlarla kesin kazanır (Core'un <c>FacilityKit.WinRaid</c>'inin public karşılığı): her odada
        /// makineler kalkar, hedef odasında kaçış başlar, ekip kapıdan geçer, oda raporu <c>AdvanceRaidRoom</c> ile yazılır; sonuç
        /// <c>ResolveRaid</c>. Kayıttan aynen yeniden oynanır.
        /// </summary>
        public static void WinRaid(Simulation sim)
        {
            for (int guard = 0; guard < 6 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                Calm(B);
                if (B.Room.Type == RoomType.Target && !B.Escape) StartEscape(B);
                ToGateCore(B);
                if (B.Phase == "player") Assert.IsTrue(Baskin.Pass(B), "GEÇ: " + B.Room.Id);
                var ok = sim.AdvanceRaidRoom(sim.RaidRoomReport(B));
                Assert.IsTrue(ok.Ok, ok.Msg);
            }
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            var res = sim.ResolveRaid(sim.FacilityRaidResult());
            Assert.IsTrue(res.Ok, res.Msg);
            Assert.IsTrue(sim.S.RaidLog.Last().Success, "WinRaid kazanmalı");
        }

        /// <summary>Kadro üyesinin kaderi (<see cref="ResolveRaid(Simulation, bool, int[])"/>): ayakta döner (kapıdan çıkar).</summary>
        public const int Returns = 0;
        /// <summary>Düşer ve tesiste kalır, ölür (tesiste "ekip ayakta döndüyse düşen kurtarılır" kuralı yok). Başarıda hedef odasında,
        /// başarısızlıkta geri çekilinen giriş odasında düşer.</summary>
        public const int Dies = 1;
        /// <summary>Geride kalır: kaçışta süre dolar, çıkamaz (ölür). Yalnız başarılı baskında.</summary>
        public const int LeftBehind = 2;
        /// <summary>Giriş odasında düşer, ekip dönüşte toplar: tesisten çıkarılır, yaşar (yatar). Yalnız başarılı baskında.</summary>
        public const int Rescued = 3;

        /// <summary>
        /// Tek kip: süren baskını oda zinciriyle bitirir (<c>ResolveRaid</c> elle verilen sonucu yok sayar, son oda raporlanmadan
        /// reddeder). <paramref name="success"/>: kazanır; değilse giriş odasında geri çekilir. Kadronun kaderi <paramref name="fates"/>'te,
        /// kadro sırasıyla (verilmeyen <see cref="Returns"/>; başarısızlıkta yalnız <see cref="Returns"/> ve <see cref="Dies"/>).
        /// </summary>
        public static void ResolveRaid(Simulation sim, bool success, params int[] fates) => ResolveRaid(sim, success, fates, null);

        /// <summary>
        /// <see cref="ResolveRaid(Simulation, bool, int[])"/> + <paramref name="act"/>: her odanın açılışında, makineler kalktıktan sonra
        /// çağrılır (test düzeni: ör. <see cref="Civilians"/> ile kırılan ve ayakta kalan sivil sayısı).
        /// <para>
        /// Başarı: her odada makineler ve kopya dolapları kalkar (geçilen odalar temiz kalır, dönüş yürüyerek olur); hedef odasında kaçış
        /// başlar (ikmalde taşıyıcı <see cref="Returns"/> kaderli ilk uzman, önce Teknisyen); geride kalan kaçışta tahtada bekler, öbürleri
        /// kapıdayken süre biter. Başarısızlık: giriş odasında geri çekilme. Yalnız public komutlar (<c>OpenRaidRoom</c> → tahta →
        /// <c>AdvanceRaidRoom</c> → <c>ResolveRaid</c>); kayıttan aynen yeniden oynanır. Editör fikstürünün (<c>RaidChainFixture</c>)
        /// test karşılığı; Core testlerinde <c>FacilityKit.ResolveRaid</c>.
        /// </para>
        /// </summary>
        public static void ResolveRaid(Simulation sim, bool success, int[] fates, Action<Baskin.BState> act)
        {
            Assert.IsNotNull(sim?.S.Raid, "süren baskın yok");
            Assert.IsTrue(RaidChain.IsFacility(sim.S.Raid.Params), "baskının odaları yok");
            fates = fates ?? new int[0];
            int count = RaidChain.SquadCount(sim.S.Raid.Params);
            int Fate(int slot) => slot < fates.Length ? fates[slot] : Returns;
            var slots = Enumerable.Range(0, count).ToList();
            Assert.IsTrue(success || slots.All(i => Fate(i) == Returns || Fate(i) == Dies), "geri çekilmede yalnız Returns ve Dies olur");
            Assert.IsTrue(!success || slots.Any(i => Fate(i) == Returns), "başarılı baskında en az bir uzman ayakta dönmeli");
            for (int guard = 0; guard < 8 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                Calm(B);
                Unspawn(B);
                act?.Invoke(B);
                if (!success)
                {
                    foreach (int i in slots.Where(i => Fate(i) == Dies)) DownSlot(B, i);
                    if (B.Phase == "player") Assert.IsTrue(Baskin.Retreat(B), "geri çekilme: " + B.Room.Id);
                }
                else
                {
                    if (B.Room.Type == RoomType.Entry && !B.Escape)
                        foreach (int i in slots.Where(i => Fate(i) == Rescued)) DownSlot(B, i);
                    bool target = B.Room.Type == RoomType.Target && !B.Escape;
                    if (target)
                    {
                        foreach (int i in slots.Where(i => Fate(i) == Dies)) DownSlot(B, i);
                        foreach (var t in Baskin.TargetsLeft(B)) Baskin.CellAt(B, t.X, t.Y).O = null;
                        if (Baskin.IsLoot(B))
                        {
                            var carriers = Specialists(B).Where(u => Fate(u.Slot) == Returns).ToList();
                            Assert.IsNotEmpty(carriers, "yükü taşıyacak uzman yok");
                            B.Carrier = (carriers.FirstOrDefault(u => u.Type == "tek") ?? carriers[0]).Id;
                        }
                        B.Escape = true;
                    }
                    // Kaçışta geride kalan tahtada bekler; öbürleri kapıya (geride kalan kapı karesindeyse önce kapı dışına çekilir)
                    if (target)
                        foreach (var u in Specialists(B).Where(u => Fate(u.Slot) == LeftBehind && Baskin.IsGateTile(B, u.X, u.Y)))
                        {
                            var spot = Enumerable.Range(0, Baskin.N * Baskin.N).Select(k => new Baskin.Pt(k % Baskin.N, k / Baskin.N))
                                .First(t => FreeAt(B, t.X, t.Y) && !Baskin.IsGateTile(B, t.X, t.Y));
                            u.X = spot.X;
                            u.Y = spot.Y;
                        }
                    foreach (var u in Specialists(B).Where(u => !target || Fate(u.Slot) != LeftBehind))
                        if (B.Phase == "player") ToGate(B, u);
                    // Geride kalan: öbürleri kapıda beklerken süre biter; çıkamayan düşmüş sayılır, kapıdakiler geçer
                    for (int t = 0; t < 40 && B.Phase == "player" && Specialists(B).Any(); t++)
                    {
                        Assert.IsTrue(Baskin.EndTurn(B), "tur sonu: tur " + B.Turn);
                        Calm(B);
                    }
                    if (B.Phase == "player") Assert.IsTrue(Baskin.Pass(B), "GEÇ: " + B.Room.Id);
                }
                var ok = sim.AdvanceRaidRoom(sim.RaidRoomReport(B));
                Assert.IsTrue(ok.Ok, "raid_room: " + ok.Msg);
            }
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            var res = sim.ResolveRaid(sim.FacilityRaidResult());
            Assert.IsTrue(res.Ok, "raidres: " + res.Msg);
            Assert.AreEqual(success, sim.S.RaidLog.Last().Success, "ResolveRaid sonucu");
        }

        /// <summary>Bölgenin baskın emrini verir ve oda zinciriyle bitirir (<see cref="ResolveRaid(Simulation, bool, int[], Action{Baskin.BState})"/>).</summary>
        public static void Raid(Simulation sim, string sectorId, bool success, int[] fates = null, Action<Baskin.BState> act = null)
        {
            var r = sim.StartLiberationRaid(sectorId);
            Assert.IsTrue(r.Ok, "StartLiberationRaid " + sectorId + ": " + r.Msg);
            ResolveRaid(sim, success, fates, act);
        }

        /// <summary>
        /// Oda düzeni (<see cref="ResolveRaid(Simulation, bool, int[], Action{Baskin.BState})"/>'in <c>act</c>'i): giriş odasının ilk
        /// ziyaretinde odanın kırılan sivil sayacı <paramref name="collateral"/> olur; ilk ziyaretlerde tahtadaki sivillerden ilk
        /// <paramref name="keep"/> tanesi ayakta kalır, öbürleri kaldırılır (kırılmış sayılmaz). Core'un <c>LiberationCampaignTests.Civilians</c>'ı.
        /// </summary>
        public static Action<Baskin.BState> Civilians(int keep, int collateral = 0)
        {
            int kept = 0;
            return B =>
            {
                if (B.Escape) return;
                if (B.Room.Type == RoomType.Entry) B.Collateral = collateral;
                for (int y = 0; y < Baskin.N; y++)
                    for (int x = 0; x < Baskin.N; x++)
                    {
                        if (Baskin.CellAt(B, x, y).O?.K != "civ") continue;
                        if (kept < keep) kept++;
                        else Baskin.CellAt(B, x, y).O = null;
                    }
            };
        }

        /// <summary>Bekleyen sunucu ödülünü komutla seçer (kartlı oyunda bekleyen ödül sonraki emri kilitler); yoksa false.</summary>
        public static bool ChooseReward(Simulation sim, string choice = FacilityGear.ChoiceRecipe)
        {
            string region = sim?.Facility?.PendingReward;
            if (string.IsNullOrEmpty(region)) return false;
            var r = sim.ChooseServerReward(region, choice);
            Assert.IsTrue(r.Ok, "server_reward: " + r.Msg);
            return true;
        }

        /// <summary>Kadro sırasıyla uzmanı düşürür (tahtada ayaktaysa): plaka ve buz karşılamaz, can 0 (test kurulumu).</summary>
        static void DownSlot(Baskin.BState B, int slot)
        {
            var u = B.Units.FirstOrDefault(v => Baskin.IsSpecialist(v) && v.Slot == slot && Baskin.OnBoard(v));
            if (u == null) return;
            u.Armor = 0; u.Ice = 0; u.Hp = 0; u.Dead = true;
        }

        /// <summary>Tahtadaki ayaktaki uzmanlar, kadro sırasıyla (köpek, aygıt ve tutsak değil).</summary>
        static List<Baskin.Unit> Specialists(Baskin.BState B) =>
            B.Units.Where(u => Baskin.IsSpecialist(u) && Baskin.OnBoard(u)).OrderBy(u => u.Slot).ToList();

        /// <summary>Kopya dolaplarını kaldırır: makinesiz oda temiz sayılır, dönüşte yeniden oynanmaz.</summary>
        static void Unspawn(Baskin.BState B)
        {
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    if (Baskin.CellAt(B, x, y).O?.K == "spawner") Baskin.CellAt(B, x, y).O = null;
        }

        /// <summary>Uzmanı birimsiz bir kapı karesine koyar ve kapıya girdirir (Assert).</summary>
        static void ToGate(Baskin.BState B, Baskin.Unit u)
        {
            var g = FreeGate(B, u);
            Assert.IsTrue(g.HasValue, "boş kapı karesi yok: " + B.Room?.Id);
            u.X = g.Value.X;
            u.Y = g.Value.Y;
            Assert.IsTrue(Baskin.EnterGate(B, u.Id), "EnterGate: " + B.Room?.Id);
        }

        /// <summary>Kare geçilebilir (duvar ve nesne yok) ve üstünde ayakta birim yok.</summary>
        static bool FreeAt(Baskin.BState B, int x, int y) =>
            x >= 0 && y >= 0 && x < Baskin.N && y < Baskin.N && Baskin.CellAt(B, x, y).T != "wall" && Baskin.CellAt(B, x, y).O == null && Baskin.UnitAt(B, x, y) == null;

        /// <summary>
        /// Aşama 9: bulunulan bölgeyi komutlarla tamamlar (bölge bittiyse sınırdan ilk bölge seçilir): baskın emri, kesin kazanılan baskın,
        /// vardiya kapanışı (<c>board_print</c> + <c>board_file</c>). Bölgeyi tamamlayan baskından sonra kapanış yapılmaz (bekleyen ödül
        /// açıkta kalır). Kadro yeni oyunun saha kadrosudur (kitli üç kişi). Bölge kimliği döner.
        /// </summary>
        public static string CompleteRegion(Simulation sim)
        {
            var lib = sim.S.Liberation;
            if (sim.LiberationRegion(lib.CurrentRegion).Completed)
                Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok, "yeni cephe");
            string region = lib.CurrentRegion;
            for (int guard = 0; guard < 6; guard++)
            {
                var sector = sim.NextLiberationSector(region);
                var started = sim.StartLiberationRaid(sector.Id);
                Assert.IsTrue(started.Ok, "baskın emri: " + started.Msg);
                WinRaid(sim);
                if (sim.LiberationRegion(region).Completed) return region;
                var printed = sim.PrintShift();
                Assert.IsTrue(printed.Ok, printed.Msg);
                var filed = sim.FileShift(sim.Board.ReceiptId);
                Assert.IsTrue(filed.Ok, filed.Msg);
            }
            Assert.Fail("bölge tamamlanmadı: " + region);
            return region;
        }

        /// <summary>
        /// Aşama 12: yeni oyunu (tesis açık denge; <paramref name="tune"/>) yalnız komutlarla <paramref name="regionId"/>'ye getirir (Core
        /// <c>FacilityKit.SimAt</c>'ın public karşılığı; test derlemeleri ayrı): bulunulan bölge tamamlanır (<see cref="CompleteRegion"/>),
        /// bekleyen sunucu ödülü seçilir (tarif), vardiya kapanır, sınırdan hedef seçilir; hedef sınırda değilse hedefe en az bağlantıyla
        /// (<see cref="Simulation.Links"/>) yaklaşan sınır bölgesi. Dönen simülasyonda hedef cephe, baskın yok; bütün adımlar kayıtta.
        /// </summary>
        public static Simulation Reach(uint seed, string regionId, Action<Balance> tune = null)
        {
            var b = Bal();
            tune?.Invoke(b);
            var sim = Simulation.NewGame(seed, b);
            for (int guard = 0; guard < 2 * GameData.Regions.Length; guard++)
            {
                var lib = sim.S.Liberation;
                if (lib.CurrentRegion == regionId && !sim.LiberationRegion(regionId).Completed) return sim;
                if (sim.LiberationRegion(lib.CurrentRegion).Completed)
                {
                    var frontier = sim.LiberationFrontier.ToList();
                    Assert.IsNotEmpty(frontier, "sınır boş: " + regionId);
                    string pick = frontier.Contains(regionId) ? regionId : frontier.OrderBy(r => Hops(sim, r, regionId)).First();
                    var sel = sim.SelectLiberationRegion(pick);
                    Assert.IsTrue(sel.Ok, sel.Msg);
                    continue;
                }
                CompleteRegion(sim);
                string pending = sim.Facility?.PendingReward;
                if (!string.IsNullOrEmpty(pending))
                {
                    var r = sim.ChooseServerReward(pending, FacilityGear.ChoiceRecipe);
                    Assert.IsTrue(r.Ok, r.Msg);
                }
                var printed = sim.PrintShift();
                Assert.IsTrue(printed.Ok, printed.Msg);
                var filed = sim.FileShift(sim.Board.ReceiptId);
                Assert.IsTrue(filed.Ok, filed.Msg);
            }
            Assert.Fail("bölgeye varılamadı: " + regionId);
            return sim;
        }

        /// <summary>İki bölge arasındaki en az bağlantı sayısı (<see cref="Simulation.Links"/>; bağlantı yoksa int.MaxValue).</summary>
        static int Hops(Simulation sim, string from, string to)
        {
            var dist = new Dictionary<string, int> { [from] = 0 };
            var queue = new Queue<string>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                if (at == to) return dist[at];
                foreach (var l in sim.Links)
                {
                    string next = l.A == at ? l.B : l.B == at ? l.A : null;
                    if (next == null || dist.ContainsKey(next)) continue;
                    dist[next] = dist[at] + 1;
                    queue.Enqueue(next);
                }
            }
            return int.MaxValue;
        }

        /// <summary>
        /// Aşama 9: kayda girmeyen kurulum: bölgenin bekleyen ödülü elle kurulur (<c>Facility.PendingReward</c>); seçim gerçek
        /// <c>server_reward</c> komutuyla (<paramref name="choice"/> null değilse). Bu yol yeniden oynatılamaz (kurulum kayıtta yok).
        /// </summary>
        public static void Reward(Simulation sim, string region, string choice = FacilityGear.ChoiceRecipe)
        {
            sim.Facility.PendingReward = region;
            if (choice == null) return;
            var r = sim.ChooseServerReward(region, choice);
            Assert.IsTrue(r.Ok, region + " " + choice + ": " + r.Msg);
        }

        /// <summary>Aşama 9: yedi temel tarifin bölgeleri, <c>FacilityGear.Kinds</c> sırasıyla (ödül tablosunun tersi).</summary>
        public static readonly string[] GearRegions = { "weu", "af", "sa", "me", "sea", "in", "jk" };

        /// <summary>Elle oda raporu (Skipped/Closed testleri): oda, yön, bitiş; kadro ayakta.</summary>
        public static RoomReport Rep(int room, bool ret = false, string end = "pass", int squad = 3)
        {
            var r = new RoomReport { Room = room, Return = ret, End = end };
            for (int i = 0; i < squad; i++) r.Team.Add(new RoomMember { State = "a", Hp = 3 });
            return r;
        }
    }
}
