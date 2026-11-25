using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis baskınları aşama 5 (Docs/TESIS-BASKINLARI-TASARIMI.md §7.1–7.2, §11.2): bina planı bilinen odalar. Teyp (girilen oda),
    /// evrak (alınan kanattan sıradaki kanadın giriş odası), dinleme nöbeti (Gözetim görevlisi vardiya başına puan; 3 puan bir oda;
    /// açılacak oda yokken puan bir oda bedelinde tavanlanır), plan tahtası (robotsuz, baskın emrinin kurduğu yapı). Hiçbiri RNG çekmez ve kayıttan aynen yeniden kurulur. Aşama 10: önlem kaplaması; aşama 11: kalıcı tahtanın izleri
    /// (plan = emirdeki tahta, izlerle de).
    /// </summary>
    public class FacilityIntelTests
    {
        static Simulation NewSim(uint seed = 79, int? cost = null)
        {
            var b = Bal();
            if (cost.HasValue) b.facilityIntelCost = cost.Value;
            return ShelterEventKit.New(b, seed);
        }

        static List<string> Known(Simulation sim) => sim.S.Liberation.Facility.Known;

        static Simulation Restore(Simulation sim)
        {
            sim.Record.day = sim.S.Day;
            var r = Replay.Restore(sim.Record);
            Assert.AreEqual(0, r.ReplayFailures);
            return r;
        }

        /// <summary>Boştaki ilk kişiyi Gözetim'e atar.</summary>
        static ResidentState Watch(Simulation sim)
        {
            var p = sim.Board.People.First(x => x.Alive && x.Job == "rest" && x.Down <= 0);
            var r = sim.AssignResident(p.Id, "watch");
            Assert.IsTrue(r.Ok, r.Msg);
            return p;
        }

        // ---------- Teyp ----------
        [Test]
        public void Tape_EveryEnteredRoomIsKnown_OnceEach()
        {
            var sim = Sim();
            var rooms = sim.S.Raid.Params.Rooms.ToList();
            CollectionAssert.IsEmpty(Known(sim), "baskından önce plan yok");
            var reps = PlayRaid(sim);
            var entered = reps.Select(r => rooms[r.Room - 1]).Distinct().ToList();
            CollectionAssert.IsSubsetOf(entered, Known(sim));
            Assert.AreEqual(Known(sim).Count, Known(sim).Distinct().Count(), "tekrarsız");
            Assert.IsTrue(reps.Any(r => r.Return) ? reps.Count > entered.Count : true, "dönüş ziyareti ikinci kez eklenmez");
            foreach (var id in entered) Assert.IsTrue(sim.FacilityKnown(id), id);
        }

        // ---------- Evrak ----------
        [Test]
        public void Papers_CapturedWing_RevealsTheNextWingsEntry()
        {
            // 79'dan başlayarak ilk uygun kampanya (sürüm 10'da 79'un ilk baskını Teknisyen'i kaybediyor ve yedek yok)
            for (uint seed = 79; seed < 99; seed++)
                if (CapturedWingReveals(seed)) return;
            Assert.Fail("12 vardiyada sıradaki kanadı olan başarılı tesis baskını bulunamadı (tohum 79–98)");
        }

        static bool CapturedWingReveals(uint seed)
        {
            var sim = NewSim(seed);
            for (int shift = 0; shift < 12 && sim.S.Ending == null; shift++)
            {
                string sector = null;
                if (TryStartRaid(sim)) { sector = sim.S.Raid.Params.SectorId; PlayRaid(sim); }
                if (sector != null && sim.S.RaidLog.Last().Success)
                {
                    var region = sim.LiberationRegion(sector.Substring(0, sector.IndexOf('_')));
                    var next = sim.NextLiberationSector(region.Id);
                    if (next != null)
                    {
                        Assert.IsTrue(sim.FacilityKnown(FacilityMaps.RoomId(next.Id, 1)), "evrak: " + next.Id);
                        Assert.IsTrue(sim.FacilityPlan.Wing(next.Id).Skip(1).All(r => !sim.FacilityKnown(r.Id)), "yalnız giriş odası");
                        return true;
                    }
                }
                ShelterEventKit.Close(sim);
            }
            return false;
        }

        // ---------- Dinleme nöbeti ----------
        /// <summary>Varsayılan (2026-11-24, geliştirici: "1 vardiyada bir oda haritası"): Gözetim'deki kişi her kapanışta sıradaki
        /// kanadın planı bilinmeyen ilk odasını açar (G, A, H); puan birikmez, kanat bitince bir oda bedelinde bekler.</summary>
        [Test]
        public void Watch_EveryShift_OpensTheNextUnknownRoom_InOrder()
        {
            var sim = NewSim();
            Assert.AreEqual(1, sim.B.facilityIntelCost, "varsayılan bedel");
            Watch(sim);
            var wing = sim.FacilityPlan.Wing(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id);
            for (int i = 0; i < 3; i++)
            {
                ShelterEventKit.Close(sim);
                Assert.AreEqual(0, sim.S.Liberation.Facility.Intel, "kapanış " + (i + 1) + ": puan harcandı");
                CollectionAssert.AreEqual(wing.Take(i + 1).Select(r => r.Id).ToArray(), Known(sim), "kapanış " + (i + 1));
            }
            Assert.IsNull(sim.FacilityIntelTarget, "kanadın üç odası da bilinir");
            ShelterEventKit.Close(sim);
            Assert.AreEqual(1, sim.S.Liberation.Facility.Intel, "hedef yokken puan bir oda bedelinde bekler");
            var plain = NewSim();
            for (int i = 0; i < 4; i++) ShelterEventKit.Close(plain);
            Assert.AreEqual(plain.S.Rng.State, sim.S.Rng.State, "dinleme nöbeti RNG çekmez");
        }

        /// <summary>Eski bedel (3; kayıt dengeyi taşır, eski oyunlar böyle sürer): puan birikir, üç kapanışta bir oda.</summary>
        [Test]
        public void Watch_Cost3_ThreeShifts_OpenTheNextUnknownRoom_InOrder()
        {
            var sim = NewSim(cost: 3);
            Watch(sim);
            var wing = sim.FacilityPlan.Wing(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id);
            uint rng0 = sim.S.Rng.State;
            var expected = new[] { 1, 2, 0, 1, 2, 0 };
            for (int i = 0; i < 6; i++)
            {
                ShelterEventKit.Close(sim);
                Assert.AreEqual(expected[i], sim.S.Liberation.Facility.Intel, "kapanış " + (i + 1));
                Assert.AreEqual(i < 2 ? 0 : i < 5 ? 1 : 2, Known(sim).Count, "kapanış " + (i + 1));
            }
            CollectionAssert.AreEqual(new[] { wing[0].Id, wing[1].Id }, Known(sim), "sıra: G, A");
            Assert.AreEqual(wing[2].Id, sim.FacilityIntelTarget?.Id);
            // Çekimsiz: aynı tohumda Gözetim'siz oyunla aynı ana RNG
            var plain = NewSim();
            for (int i = 0; i < 6; i++) ShelterEventKit.Close(plain);
            Assert.AreEqual(plain.S.Rng.State, sim.S.Rng.State, "dinleme nöbeti RNG çekmez");
            Assert.AreNotEqual(0u, rng0);
        }

        [Test]
        public void Watch_NoWatcher_NoIntel_AndBedriddenWatcherBlocksThePlan()
        {
            var sim = NewSim();
            for (int i = 0; i < 3; i++) ShelterEventKit.Close(sim);
            Assert.AreEqual(0, sim.S.Liberation.Facility.Intel, "Gözetim boşken puan yok");
            CollectionAssert.IsEmpty(Known(sim));
            // Yatan kişi Gözetim'de kalamaz: plan basılmaz (öbür görevlerle aynı kural), puan üretmez
            var p = Watch(sim);
            p.Down = 5; p.Hp = 1;
            Assert.IsNotNull(sim.BoardPlanCheck());
        }

        [Test]
        public void Watch_NoTarget_IntelCapsAtOneRoom()
        {
            var sim = NewSim();
            Watch(sim);
            var wing = sim.FacilityPlan.Wing(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id);
            Known(sim).AddRange(wing.Select(r => r.Id));   // kurulum: kanadın üç odası bilinir, açılacak oda yok
            Assert.IsNull(sim.FacilityIntelTarget);
            for (int i = 0; i < 5; i++) ShelterEventKit.Close(sim);
            Assert.AreEqual(sim.B.facilityIntelCost, sim.S.Liberation.Facility.Intel, "tavan: bir oda bedeli");
            Assert.AreEqual(3, Known(sim).Count);
        }

        // ---------- Kayıt ----------
        [Test]
        public void Replay_RebuildsKnownRoomsAndIntel()
        {
            var sim = NewSim();
            Watch(sim);
            int raids = 0;
            for (int shift = 0; shift < 7 && sim.S.Ending == null; shift++)
            {
                if (TryStartRaid(sim)) { PlayRaid(sim); raids++; }
                ShelterEventKit.Close(sim);
            }
            Assert.Greater(raids, 0);
            Assert.Greater(Known(sim).Count, 0);
            var r = Restore(sim);
            CollectionAssert.AreEqual(Known(sim), Known(r));
            Assert.AreEqual(sim.S.Liberation.Facility.Intel, r.S.Liberation.Facility.Intel);
            Assert.AreEqual(State(sim), State(r));
        }

        // ---------- Plan tahtası ----------
        /// <summary>
        /// Plan = baskın emrinin kuracağı tahta, makinesiz: 36 kanadın üç odası × kademe 1/3 × olaylar açık/kapalı × iki ayrı baskın
        /// tohumu. Yapı, nesneler (hedef dolabın türü baskında core/node olur), geçit, menfez, kepenk ve olay bayrakları bire bir.
        /// Tohumdan bağımsızlık iki tohumun aynı plana denk gelmesiyle sınanır.
        /// </summary>
        [Test]
        public void PlanCells_MatchTheBoardTheOrderBuilds_EveryRoomTierAndEvent()
        {
            int tested = 0;
            foreach (var wing in Wings)
                foreach (int tier in new[] { 1, 3 })
                    foreach (bool events in new[] { true, false })
                        foreach (int seed in new[] { 41, 977 })
                        {
                            var p = Params(wing, seed, tier, events);
                            var reps = new List<RoomReport>();
                            for (int i = 0; i < wing.Count; i++)
                            {
                                var room = wing[i];
                                var board = Open(p, reps, Bal(events));
                                Assert.AreEqual(room.Id, board.Room?.Id);
                                var plan = room.PlanCells(tier, events && room.Event != null);
                                for (int y = 0; y < Baskin.N; y++)
                                    for (int x = 0; x < Baskin.N; x++)
                                    {
                                        var a = plan[y, x]; var c = board.Cells[y, x];
                                        string at = $"{room.Id} k{tier} {(events ? "olay" : "olaysız")} t{seed} {x},{y}";
                                        Assert.AreEqual(c.T, a.T, at);
                                        Assert.AreEqual(c.Door, a.Door, at);
                                        Assert.AreEqual(c.Vent, a.Vent, at);
                                        Assert.AreEqual(c.Shutter, a.Shutter, at);
                                        Assert.AreEqual(c.Cloud, a.Cloud, at); Assert.AreEqual(c.Gas, a.Gas, at);
                                        Assert.AreEqual(c.Crack, a.Crack, at); Assert.AreEqual(c.Oil, a.Oil, at);
                                        Assert.AreEqual(c.Shelf, a.Shelf, at); Assert.AreEqual(c.Phase, a.Phase, at);
                                        if (a.O != null && Baskin.IsTarget(a.O.K)) Assert.IsTrue(c.O != null && Baskin.IsTarget(c.O.K), at);
                                        else Assert.AreEqual(a.O?.K, c.O?.K, at);
                                    }
                                reps.Add(Rep(i + 1));
                                tested++;
                            }
                        }
            Assert.AreEqual(36 * 3 * 2 * 2 * 2, tested);
        }

        /// <summary>Planla tahtanın hücre eşitliği (yapı, kapılar, olay bayrakları, nesne türü; aşama 10: çift cidar ve yayıcının canı).</summary>
        static void AssertSameCells(Baskin.Cell[,] plan, Baskin.Cell[,] board, string where)
        {
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var a = plan[y, x]; var c = board[y, x];
                    string at = where + " " + x + "," + y;
                    Assert.AreEqual(c.T, a.T, at);
                    Assert.AreEqual(c.Double, a.Double, at);
                    Assert.AreEqual(c.Door, a.Door, at); Assert.AreEqual(c.Vent, a.Vent, at); Assert.AreEqual(c.Shutter, a.Shutter, at);
                    Assert.AreEqual(c.Cloud, a.Cloud, at); Assert.AreEqual(c.Gas, a.Gas, at);
                    Assert.AreEqual(c.Crack, a.Crack, at); Assert.AreEqual(c.Oil, a.Oil, at);
                    Assert.AreEqual(c.Shelf, a.Shelf, at); Assert.AreEqual(c.Phase, a.Phase, at);
                    if (a.O != null && Baskin.IsTarget(a.O.K)) Assert.IsTrue(c.O != null && Baskin.IsTarget(c.O.K), at);
                    else Assert.AreEqual(a.O?.K, c.O?.K, at);
                    if (a.O?.K == "yayici") Assert.AreEqual(a.O.Hp, c.O.Hp, at);
                }
        }

        /// <summary>
        /// Aşama 10 (K23, kırmızı kalemin verisi): önlemlerin yapısal kaplaması (çift cidar, koku yayıcı) planda da aynı yerde. 36 kanadın
        /// üç odası × kademe 0/1/2 (yıkım ve köpek birlikte) × olaylar açık/kapalı: plan (<c>PlanCells</c> + <see cref="Baskin.ApplyCounterCells(Baskin.Cell[,], FacilityRoom, bool, IList{int}, Balance)"/>)
        /// = emirdeki kademelerle kurulan tahta. Kademe 1'de giriş ve ara odada iki çift duvar, hedefte yok; kademe 2'de hedefte de; yayıcı
        /// her odada kademe kadar.
        /// </summary>
        [Test]
        public void PlanCells_WithCounters_MatchTheBoard_EveryRoomAndLevel()
        {
            int tested = 0;
            var doubles = new int[3, 3];
            var emitters = new int[3, 3];
            foreach (var wing in Wings)
                foreach (int level in new[] { 0, 1, 2 })
                    foreach (bool events in new[] { true, false })
                    {
                        var p = Params(wing, 41, 1, events);
                        p.Counters = new List<int> { 0, level, 0, 0, level, 0 };
                        var reps = new List<RoomReport>();
                        for (int i = 0; i < wing.Count; i++)
                        {
                            var room = wing[i];
                            var board = Open(p, reps, Bal(events));
                            bool ev = events && room.Event != null;
                            var plan = room.PlanCells(1, ev);
                            Baskin.ApplyCounterCells(plan, room, ev, p.Counters, Bal(events));
                            AssertSameCells(plan, board.Cells, $"{room.Id} k{level} {(events ? "olay" : "olaysız")}");
                            for (int y = 0; y < Baskin.N; y++)
                                for (int x = 0; x < Baskin.N; x++)
                                {
                                    if (plan[y, x].Double) doubles[level, i]++;
                                    if (plan[y, x].O?.K == "yayici") emitters[level, i]++;
                                }
                            reps.Add(Rep(i + 1));
                            tested++;
                        }
                    }
            Assert.AreEqual(36 * 3 * 3 * 2, tested);
            int boards = 36 * 2;
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(0, doubles[0, i], "kademe 0: çift cidar yok");
                Assert.AreEqual(0, emitters[0, i], "kademe 0: yayıcı yok");
                Assert.AreEqual(boards * 1, emitters[1, i], "kademe 1: her odada bir yayıcı");
                Assert.AreEqual(boards * 2, emitters[2, i], "kademe 2: her odada iki yayıcı");
            }
            Assert.AreEqual(0, doubles[1, 2], "kademe 1: hedef odada çift cidar yok");
            Assert.Greater(doubles[1, 0], boards * 2 * 9 / 10, "giriş odalarının çoğunda iki çift duvar");
            Assert.Greater(doubles[1, 1], boards * 2 * 9 / 10, "ara odaların çoğunda iki çift duvar");
            Assert.Greater(doubles[2, 2], boards * 2 * 9 / 10, "kademe 2: hedef odada da");
        }

        /// <summary>
        /// Aşama 10: simülasyonun plan okuyucusu bugünkü kademeleri kaplar ve baskın emrinin kurduğu tahtayla aynıdır (weu_1'in üç odası);
        /// alt bayrak kapalıyken kaplama yok. Okuyucu durum yazmaz, RNG çekmez.
        /// </summary>
        [Test]
        public void PlanCells_Simulation_OverlaysTodaysCounters_EqualsTheOrderBoard()
        {
            var sim = NewSim();
            sim.Facility.Counter.AddRange(new[] { 0, 2, 0, 0, 2, 0 });
            sim.Facility.Tally.AddRange(new[] { 0, 0, 0, 0, 0, 0 });
            var offBalance = Bal();
            offBalance.facilityCountermeasures = false;
            var off = ShelterEventKit.New(offBalance, 79);
            off.Facility.Counter.AddRange(new[] { 0, 2, 0, 0, 2, 0 });
            ShelterEventKit.FillSquad(sim);
            ShelterEventKit.StartRaid(sim);
            var p = sim.S.Raid.Params;
            CollectionAssert.AreEqual(new[] { 0, 2, 0, 0, 2, 0 }, p.Counters);
            var reps = new List<RoomReport>();
            var rooms = sim.FacilityPlan.Wing(p.SectorId);
            uint rng = sim.S.Rng.State;
            for (int i = 0; i < rooms.Count; i++)
            {
                var plan = sim.FacilityPlanCells(rooms[i]);
                var board = RaidChain.Open(sim.B, sim.FacilityPlan, p, reps);
                AssertSameCells(plan, board.Cells, rooms[i].Id);
                int emit = 0, dbl = 0;
                foreach (var c in plan) { if (c.O?.K == "yayici") emit++; if (c.Double) dbl++; }
                Assert.AreEqual(2, emit, rooms[i].Id);
                int offEmit = 0, offDbl = 0;
                foreach (var c in off.FacilityPlanCells(rooms[i])) { if (c.O?.K == "yayici") offEmit++; if (c.Double) offDbl++; }
                Assert.AreEqual(0, offEmit + offDbl, "alt bayrak kapalı: kaplama yok");
                reps.Add(Rep(i + 1));
            }
            Assert.AreEqual(rng, sim.S.Rng.State);
        }

        [Test]
        public void PlanCells_AreACopy_CarryNoRobots_AndDrawNoRng()
        {
            var sim = NewSim();
            foreach (var room in sim.FacilityPlan.Region("weu"))
            {
                // Kopya: okuyan değiştirse de oda bozulmaz
                var plan = sim.FacilityPlanCells(room);
                string t0 = plan[0, 0].T, o0 = plan[0, 0].O?.K;
                plan[0, 0].T = t0 == "wall" ? "floor" : "wall";
                plan[0, 0].O = new Baskin.Obj { K = "dron" };
                Assert.AreEqual(t0, sim.FacilityPlanCells(room)[0, 0].T, room.Id);
                Assert.AreEqual(o0, sim.FacilityPlanCells(room)[0, 0].O?.K, room.Id);
                foreach (var row in room.Rows)
                    Assert.IsFalse(row.Any(ch => ch == 'd' || ch == 'm' || ch == 'Y'), room.Id + ": robot harfi");
            }
            uint rng = sim.S.Rng.State;
            foreach (var room in sim.FacilityPlan.Region("weu")) sim.FacilityPlanCells(room);
            Assert.AreEqual(rng, sim.S.Rng.State, "plan okuyucusu RNG çekmez");
        }

        // ---------- Aşama 11: izli plan ----------
        /// <summary>Odanın plan hücrelerinden rastgele iz kümesi (test kurulumu, saf akış): iki duvar, bir kepenk, donanım, soğutma ve kopya
        /// dolabı (varsa) ve üç engel; bir kısmı uygulanamaz (bağ kuralı, dolu kare) ve iki tarafta da atlanır.</summary>
        static List<FacilityMark> RandomMarks(FacilityRoom room, Baskin.Cell[,] cells, Rng r)
        {
            var o = new List<FacilityMark>();
            var tiles = new List<Baskin.Pt>();
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) tiles.Add(new Baskin.Pt(x, y));
            Baskin.Pt Pick(List<Baskin.Pt> from) => from[(int)(r.Next() * from.Count)];
            void Add(string kind, Baskin.Pt t, int hp = 0) =>
                o.Add(new FacilityMark { Room = room.Id, Kind = kind, X = t.X, Y = t.Y, Hp = hp, Max = hp + 1, Day = 1 });
            var walls = tiles.Where(t => cells[t.Y, t.X].T == "wall" && !cells[t.Y, t.X].Shutter && !cells[t.Y, t.X].Crate).ToList();
            for (int i = 0; i < 2 && walls.Count > 0; i++) Add(FacilityMarks.Wall, Pick(walls));
            var shutters = tiles.Where(t => cells[t.Y, t.X].Shutter).ToList();
            if (shutters.Count > 0) Add(FacilityMarks.Shutter, Pick(shutters));
            if (room.DeviceAt is Baskin.Pt d) Add(FacilityMarks.Device, d);
            foreach (var t in tiles.Where(t => cells[t.Y, t.X].O?.K == "cool" || cells[t.Y, t.X].O?.K == "spawner")) Add(cells[t.Y, t.X].O.K, t);
            var floors = tiles.Where(t => cells[t.Y, t.X].T == "floor").ToList();
            var kinds = new[] { FacilityMarks.Wire, FacilityMarks.Plank, FacilityMarks.Scrap };
            for (int i = 0; i < 3 && floors.Count > 0; i++) Add(kinds[i], Pick(floors), 1 + i);
            return o;
        }

        /// <summary>
        /// Aşama 11 (§9.1, plan = tahta izlerle): 36 kanadın üç odası × kademe 1/3 × olaylar açık/kapalı, rastgele izli emir: plan
        /// (<c>PlanCells</c> + <see cref="FacilityMarks.Apply"/>) = emirdeki izlerle kurulan ilk ziyaret tahtası (yapı, kapılar, nesne türü,
        /// engelin canı). Uygulanan izin sayısı anlamlı.
        /// </summary>
        [Test]
        public void PlanCells_WithMarks_MatchTheBoard_EveryRoomTierAndEvent()
        {
            int tested = 0, applied = 0, total = 0;
            var r = new Rng(11);
            foreach (var wing in Wings)
                foreach (int tier in new[] { 1, 3 })
                    foreach (bool events in new[] { true, false })
                    {
                        var p = Params(wing, 41, tier, events);
                        p.Marks = wing.SelectMany(room => RandomMarks(room, room.PlanCells(tier, events && room.Event != null), r)).ToList();
                        var reps = new List<RoomReport>();
                        for (int i = 0; i < wing.Count; i++)
                        {
                            var room = wing[i];
                            var board = Open(p, reps, Bal(events));
                            bool ev = events && room.Event != null;
                            var plan = room.PlanCells(tier, ev);
                            var skipped = FacilityMarks.Apply(plan, room, p.Marks, null, ev);
                            int mine = FacilityMarks.Of(p.Marks, room.Id).Count;
                            total += mine;
                            applied += mine - skipped.Count;
                            string at = $"{room.Id} k{tier} {(events ? "olay" : "olaysız")}";
                            AssertSameCells(plan, board.Cells, at);
                            for (int y = 0; y < Baskin.N; y++)
                                for (int x = 0; x < Baskin.N; x++)
                                    if (plan[y, x].O != null && FacilityMarks.IsObjectKind(plan[y, x].O.K))
                                        Assert.AreEqual((plan[y, x].O.Hp, plan[y, x].O.Max), (board.Cells[y, x].O.Hp, board.Cells[y, x].O.Max), at);
                            reps.Add(Rep(i + 1));
                            tested++;
                        }
                    }
            Assert.AreEqual(36 * 3 * 2 * 2, tested);
            Assert.Greater(applied, total / 2, "izlerin çoğu uygulanır");
            Assert.Less(applied, total, "bir kısmı uygulanamaz (iki tarafta da atlanır)");
        }

        /// <summary>Aşama 11: simülasyonun plan okuyucusu kanadın bugünkü izlerini uygular ve baskın emrinin kurduğu tahtayla aynıdır (weu_1);
        /// alt bayrak kapalıyken uygulamaz. Durum yazmaz, RNG çekmez.</summary>
        [Test]
        public void PlanCells_Simulation_AppliesTodaysMarks_EqualsTheOrderBoard()
        {
            var sim = NewSim();
            var r = new Rng(5);
            var rooms = sim.FacilityPlan.Wing("weu_1");
            bool events = sim.B.liberationRegionEvents;
            foreach (var room in rooms) sim.Facility.Marks.AddRange(RandomMarks(room, room.PlanCells(sim.Tier, events && room.Event != null), r));
            var offBalance = Bal();
            offBalance.facilityMarks = false;
            var off = ShelterEventKit.New(offBalance, 79);
            off.Facility.Marks.AddRange(sim.Facility.Marks);
            ShelterEventKit.FillSquad(sim);
            ShelterEventKit.StartRaid(sim);
            var p = sim.S.Raid.Params;
            Assert.AreEqual(sim.Facility.Marks.Count, p.Marks.Count);
            var reps = new List<RoomReport>();
            uint rng = sim.S.Rng.State;
            int differs = 0;
            for (int i = 0; i < rooms.Count; i++)
            {
                var plan = sim.FacilityPlanCells(rooms[i]);
                var board = RaidChain.Open(sim.B, sim.FacilityPlan, p, reps);
                AssertSameCells(plan, board.Cells, rooms[i].Id);
                var bare = off.FacilityPlanCells(rooms[i]);
                AssertSameCells(bare, rooms[i].PlanCells(sim.Tier, events && rooms[i].Event != null), rooms[i].Id + " kapalı");
                for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (bare[y, x].T != plan[y, x].T || bare[y, x].O?.K != plan[y, x].O?.K) differs++;
                reps.Add(Rep(i + 1));
            }
            Assert.Greater(differs, 3, "izler planda görünür");
            Assert.AreEqual(rng, sim.S.Rng.State);
        }
    }
}
