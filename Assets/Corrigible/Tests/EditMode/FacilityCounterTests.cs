using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 10, parça B (Docs/TESIS-BASKINLARI-TASARIMI.md §6.3; uygulama tasarımı §1.2–1.3, §3, §6.2): önlem defterinin
    /// simülasyon tarafı. Öğrenmenin saf seçimi (<see cref="FacilityCounters.Learn"/>), baskın sonunda sayımın tavanlı toplanması,
    /// bölge ilk kez tamamlanınca öğrenme (eşik, eşitlik, karşı saldırı, zafer ve dağılma), emir kopyası (<see cref="RaidParams.Counters"/>,
    /// <see cref="RaidParams.Answers"/>), cevap tariflerinin defter sayfası (aynı dizin ve kimlik, +devre, Söküm'ün kit yolu), cevap seçilince
    /// taslak iş emrinin iptali ve kayıt (f4, yeniden oynatma). Tahta kuralları <see cref="FacilityCountermeasureTests"/>'te; plan = tahta
    /// <see cref="FacilityIntelTests"/>'te. Sayaçlar testte tahtaya doğrudan yazılır (rapor tahtadan kurulur; tahta yeniden oynatılmaz).
    /// </summary>
    public class FacilityCounterTests
    {
        static Simulation NewSim(Action<Balance> tune = null, uint seed = 79)
        {
            var b = Bal();
            tune?.Invoke(b);
            return ShelterEventKit.New(b, seed);
        }

        static Simulation Restore(Simulation sim)
        {
            sim.Record.day = sim.S.Day;
            var r = Replay.Restore(sim.Record);
            Assert.AreEqual(0, r.ReplayFailures);
            return r;
        }

        static List<int> L(params int[] v) => v.ToList();

        /// <summary>Tahtanın sınıf sayaçlarına ekler (itme, yıkım, örme, ele, köpek, aygıt sırası).</summary>
        static void AddTally(Baskin.BState B, IList<int> t)
        {
            B.TallyPush += t[0]; B.TallyBreach += t[1]; B.TallyBuild += t[2];
            B.TallyCapture += t[3]; B.TallyDog += t[4]; B.TallyGadget += t[5];
        }

        /// <summary>Yalnız ilk odada (gidiş G) sayaç ekleyen oda eylemi.</summary>
        static Action<Baskin.BState, int> First(params int[] t) => (B, i) => { if (i == 0) AddTally(B, t); };

        /// <summary>
        /// <see cref="FacilityKit.WinRaid"/>'in aynısı; her odada (oda sırası 0'dan) <paramref name="room"/> çağrılır (sayaç yazmak için).
        /// Gidiş G, A, H ve dönüş A, G: beş rapor.
        /// </summary>
        static void Win(Simulation sim, Action<Baskin.BState, int> room = null)
        {
            for (int i = 0; i < 6 && !sim.RaidRoomsClosed; i++)
            {
                var B = sim.OpenRaidRoom();
                Calm(B);
                room?.Invoke(B, i);
                if (B.Room.Type == RoomType.Target && !B.Escape) StartEscape(B);
                ToGate(B);
                if (B.Phase == "player") Assert.IsTrue(Baskin.Pass(B), "GEÇ: " + B.Room.Id);
                var ok = sim.AdvanceRaidRoom(sim.RaidRoomReport(B));
                Assert.IsTrue(ok.Ok, ok.Msg);
            }
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            var res = sim.ResolveRaid(sim.FacilityRaidResult());
            Assert.IsTrue(res.Ok, res.Msg);
        }

        /// <summary>Giriş odasında süre dolar: geri çekilme, başarısız baskın.</summary>
        static void Lose(Simulation sim, params int[] t)
        {
            var B = sim.OpenRaidRoom();
            Calm(B);
            AddTally(B, t);
            for (int guard = 0; guard < 30 && B.Phase == "player"; guard++) Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual("lost", B.Phase);
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(B)).Ok);
            Assert.IsTrue(sim.RaidRoomsClosed);
            Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
            Assert.IsFalse(sim.S.RaidLog.Last().Success);
        }

        static void Start(Simulation sim)
        {
            ShelterEventKit.FillSquad(sim);
            var sector = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            var started = sim.StartLiberationRaid(sector.Id);
            Assert.IsTrue(started.Ok, started.Msg);
        }

        /// <summary><see cref="FacilityKit.CompleteRegion"/>'ın aynısı; her baskında <paramref name="room"/> oda eylemiyle.</summary>
        static string Complete(Simulation sim, Action<Baskin.BState, int> room)
        {
            var lib = sim.S.Liberation;
            if (sim.LiberationRegion(lib.CurrentRegion).Completed)
                Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
            string region = lib.CurrentRegion;
            for (int guard = 0; guard < 6; guard++)
            {
                Start(sim);
                Win(sim, room);
                if (sim.LiberationRegion(region).Completed) return region;
                ShelterEventKit.Close(sim);
            }
            Assert.Fail("bölge tamamlanmadı: " + region);
            return region;
        }

        /// <summary>Bölgenin ödülünü komutla verir: bekleyen ödül elle kurulur (bölgeyi oynamadan), seçim server_reward komutudur.</summary>
        static ActResult Grant(Simulation sim, string recipe)
        {
            string region = GameData.Regions.Select(r => r.Id).First(id => FacilityGear.Reward(id).Recipe == recipe);
            sim.Facility.PendingReward = region;
            var r = sim.ChooseServerReward(region, FacilityGear.ChoiceRecipe);
            Assert.IsTrue(r.Ok, r.Msg);
            return r;
        }

        static void Stock(Simulation sim, int each = 6)
        {
            foreach (string kind in PlanningBoardData.MaterialKinds) sim.AddBoardMaterial(kind, each);
        }

        static void Order(Simulation sim, string recipeId)
        {
            var q = sim.QueueProduction(recipeId);
            Assert.IsTrue(q.Ok, q.Msg);
            foreach (var input in sim.BoardRecipes.Single(r => r.Id == recipeId).Inputs)
            {
                var r = sim.ReserveProductionMaterial(input.Kind, input.Quantity);
                Assert.IsTrue(r.Ok, r.Msg);
            }
        }

        static int Qty(BoardRecipe r, string kind) => r.Inputs.Where(i => i.Kind == kind).Sum(i => i.Quantity);

        // ---------- Öğrenmenin saf seçimi ----------
        [Test]
        public void Learn_PureSelection()
        {
            var none = new List<FacilityCounterEntry>();
            var zero = L(0, 0, 0, 0, 0, 0);
            Assert.AreEqual(-1, FacilityCounters.Learn(zero, zero, none, 6), "sayaç yok");
            Assert.AreEqual(-1, FacilityCounters.Learn(L(5, 0, 0, 0, 0, 0), zero, none, 6), "eşik altı");
            Assert.AreEqual(0, FacilityCounters.Learn(L(6, 0, 0, 0, 0, 0), zero, none, 6), "eşikte");
            Assert.AreEqual(0, FacilityCounters.Learn(L(1, 0, 0, 0, 0, 0), zero, none, 0), "eşik 0 → 1");
            Assert.AreEqual(-1, FacilityCounters.Learn(zero, zero, none, 0), "eşik 0 sıfır kullanımı ödüllendirmez");
            Assert.AreEqual(4, FacilityCounters.Learn(L(6, 0, 7, 0, 9, 0), zero, none, 6), "en büyük sayaç");
            Assert.AreEqual(2, FacilityCounters.Learn(L(6, 0, 7, 0, 9, 0), L(0, 0, 0, 0, 2, 0), none, 6), "2. kademe adaylıktan çıkar");
            Assert.AreEqual(1, FacilityCounters.Learn(L(0, 8, 8, 0, 0, 0), zero, none, 6), "eşitlikte tablo sırası");
            var log = new List<FacilityCounterEntry> { new FacilityCounterEntry { Day = 3, Region = "weu", Class = FacilityCounters.Breach, Level = 1 } };
            Assert.AreEqual(2, FacilityCounters.Learn(L(0, 8, 8, 0, 0, 0), L(0, 1, 0, 0, 0, 0), log, 6), "hiç önlem alınmamış sınıf en eskidir");
            log.Add(new FacilityCounterEntry { Day = 9, Region = "af", Class = FacilityCounters.Build, Level = 1 });
            Assert.AreEqual(1, FacilityCounters.Learn(L(0, 8, 8, 0, 0, 0), L(0, 1, 1, 0, 0, 0), log, 6), "son girdisi en eski olan");
            log.Add(new FacilityCounterEntry { Day = 12, Region = "sa", Class = FacilityCounters.Breach, Level = 2 });
            Assert.AreEqual(2, FacilityCounters.Learn(L(0, 8, 8, 0, 0, 0), L(0, 1, 1, 0, 0, 0), log, 6), "sınıfın son girdisi sayılır");
            Assert.AreEqual(0, FacilityCounters.Learn(L(6), new List<int>(), null, 6), "eksik öğe 0 okunur");
        }

        [Test]
        public void RaidTally_SumsReports_CapsPerClass()
        {
            var a = new RoomReport { Tally = L(5, 1, 0, 0, 2, 0) };
            var b = new RoomReport { Tally = L(0, 1, 0, 0, 2, 4) };
            CollectionAssert.AreEqual(new[] { 3, 2, 0, 0, 3, 3 }, FacilityCounters.RaidTally(new[] { a, b }, 3));
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 0 }, FacilityCounters.RaidTally(new[] { new RoomReport() }, 3), "eski rapor");
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 0 }, FacilityCounters.RaidTally(new[] { a }, -1), "tavan en az 0");
        }

        // ---------- Toplama ----------
        [Test]
        public void Tally_CappedPerRaid_ReturnVisitIsOwnReport()
        {
            var sim = NewSim();
            CollectionAssert.IsEmpty(sim.Facility.Tally, "yeni oyunda boş");
            CollectionAssert.IsEmpty(sim.Facility.Counter);
            Start(sim);
            Assert.IsTrue(sim.OpenRaidRoom().Moves.Tally, "emirde altı kademe: tahta sayar");
            // G'de 5 koşu darbesi ve 2 köpek komutu, A'da 1 aygıt, ilk dönüş ziyaretinde 2 köpek komutu
            bool back = false;
            Win(sim, (B, i) =>
            {
                if (i == 0) AddTally(B, L(5, 0, 0, 0, 2, 0));
                if (i == 1) AddTally(B, L(0, 0, 0, 0, 0, 1));
                if (sim.RaidRoom.Value.Return && !back) { back = true; AddTally(B, L(0, 0, 0, 0, 2, 0)); }
            });
            Assert.IsTrue(back, "dönüş ziyareti oldu");
            var rooms = sim.S.RaidLog.Last().Rooms.Select(RoomReport.Decode).ToList();
            var ret = rooms.First(r => r.Return);
            Assert.AreEqual(5, rooms[0].TallyAt(0), "rapor tavansız");
            Assert.AreEqual(2, ret.TallyAt(4), "dönüş ziyareti kendi sayacıyla");
            Assert.AreEqual(0, ret.TallyAt(0), "dönüş tahtası sıfırdan sayar (çift sayım yok)");
            CollectionAssert.AreEqual(new[] { 3, 0, 0, 0, 3, 1 }, sim.Facility.Tally, "baskın başına tavan 3 (5 → 3; 2 + 2 → 3)");
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 0 }, sim.Facility.Counter);
            ShelterEventKit.Close(sim);
            Start(sim);
            Win(sim, First(0, 0, 0, 0, 1, 0));
            CollectionAssert.AreEqual(new[] { 3, 0, 0, 0, 4, 1 }, sim.Facility.Tally, "baskınlar birikir");
        }

        [Test]
        public void Tally_FailedRaidCounts()
        {
            var sim = NewSim();
            Start(sim);
            Lose(sim, 2, 0, 1, 0, 0, 0);
            CollectionAssert.AreEqual(new[] { 2, 0, 1, 0, 0, 0 }, sim.Facility.Tally, "araç kullanıldı: başarısız baskın da sayar");
            CollectionAssert.IsEmpty(sim.Facility.Learned);
        }

        [Test]
        public void Tally_LevelTwoClass_NotAdded()
        {
            var sim = NewSim();
            sim.Facility.Counter.AddRange(L(2, 0, 0, 0, 1, 0));
            sim.Facility.Tally.AddRange(L(0, 0, 0, 0, 0, 0));
            Start(sim);
            Assert.AreEqual(2, sim.OpenRaidRoom().Moves.Bolt);
            Win(sim, First(3, 0, 0, 0, 3, 0));
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 3, 0 }, sim.Facility.Tally, "2. kademedeki sınıf sayılmaz; 1. kademe sayar");
        }

        // ---------- Öğrenme ----------
        [Test]
        public void Learn_OnFirstCompletion_ResetsOnlyTheChosen()
        {
            var sim = NewSim();
            string region = Complete(sim, (B, i) =>
            {
                if (i == 0) AddTally(B, L(3, 0, 0, 0, 2, 0));
            });
            Assert.AreEqual("weu", region);
            Assert.IsTrue(sim.LiberationRegion("weu").Rewarded);
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 0, 0 }, sim.Facility.Counter, "en büyük sayaç: itme (9 > 6)");
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 6, 0 }, sim.Facility.Tally, "yalnız seçilen sıfırlanır");
            Assert.AreEqual(1, sim.Facility.Learned.Count);
            var e = sim.Facility.Learned[0];
            Assert.AreEqual(sim.S.Day, e.Day);
            Assert.AreEqual("weu", e.Region);
            Assert.AreEqual(FacilityCounters.Push, e.Class);
            Assert.AreEqual(1, e.Level);
            Assert.AreEqual("itme:1", sim.S.RaidLog.Last().LiberationReward.Countermeasure, "sunum alanı");
            Assert.IsTrue(sim.S.RaidLog.Take(sim.S.RaidLog.Count - 1).All(l => l.LiberationReward.Countermeasure == null), "önceki baskınlarda yok");
            Assert.AreEqual(1, sim.CounterLevel(FacilityCounters.Push));
            Assert.AreEqual(6, sim.CounterTally(FacilityCounters.Dog));
            Assert.AreEqual(0, sim.CounterLevel("yok"));
            CollectionAssert.AreEqual(new[] { (FacilityCounters.Push, 1) }, sim.Countermeasures.Select(x => (x.Class, x.Level)));
            // Sonraki emir yeni kademeyi taşır
            Assert.IsTrue(ChooseReward(sim));
            ShelterEventKit.Close(sim);
            Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
            Start(sim);
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 0, 0 }, sim.S.Raid.Params.Counters);
            Assert.AreEqual(1, sim.OpenRaidRoom().Moves.Bolt, "cıvatalı ayak tahtada");
        }

        [Test]
        public void Learn_BelowThreshold_NoCounter_RegionSpent()
        {
            var sim = NewSim();
            Complete(sim, First(1, 1, 0, 0, 0, 0));
            Assert.IsTrue(sim.LiberationRegion("weu").Rewarded);
            CollectionAssert.AreEqual(new[] { 3, 3, 0, 0, 0, 0 }, sim.Facility.Tally);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 0 }, sim.Facility.Counter);
            CollectionAssert.IsEmpty(sim.Facility.Learned);
            Assert.IsNull(sim.S.RaidLog.Last().LiberationReward.Countermeasure);
            CollectionAssert.IsEmpty(sim.Countermeasures);
            // Bölgenin hakkı harcandı: karşı saldırıdan sonra yeniden alınan bölge eşiği geçse de öğretmez
            Assert.IsTrue(ChooseReward(sim));
            ShelterEventKit.Close(sim);
            sim.LiberationSector("weu_3").Captured = false;
            Start(sim);
            Win(sim, First(3, 3, 0, 0, 0, 0));
            Assert.IsTrue(sim.LiberationRegion("weu").Completed);
            CollectionAssert.AreEqual(new[] { 6, 6, 0, 0, 0, 0 }, sim.Facility.Tally, "baskının sayımı yine eklenir");
            CollectionAssert.IsEmpty(sim.Facility.Learned, "yeniden tamamlanan bölge öğretmez");
        }

        [Test]
        public void Learn_AfterCounterattack_RecapturedRegion_Unchanged()
        {
            var sim = NewSim();
            Complete(sim, First(3, 0, 0, 0, 0, 0));
            Assert.AreEqual(1, sim.Facility.Learned.Count);
            Assert.IsTrue(ChooseReward(sim));
            ShelterEventKit.Close(sim);
            sim.LiberationSector("weu_3").Captured = false;
            Start(sim);
            Win(sim, First(3, 0, 0, 0, 0, 0));
            Assert.IsTrue(sim.LiberationRegion("weu").Completed);
            Assert.AreEqual(1, sim.Facility.Learned.Count, "ikinci öğrenme yok");
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 0, 0 }, sim.Facility.Counter);
            CollectionAssert.AreEqual(new[] { 3, 0, 0, 0, 0, 0 }, sim.Facility.Tally, "sayım sürer");
            Assert.IsNull(sim.S.RaidLog.Last().LiberationReward.Countermeasure);
        }

        [Test]
        public void Learn_NotOnVictory()
        {
            var sim = NewSim();
            foreach (var area in sim.S.Liberation.Regions.Where(r => r.Id != "weu"))
                foreach (var s in area.Sectors) s.Captured = true;
            Complete(sim, First(3, 0, 0, 0, 0, 0));
            Assert.AreEqual("silme", sim.S.Ending);
            CollectionAssert.AreEqual(new[] { 9, 0, 0, 0, 0, 0 }, sim.Facility.Tally, "sayım yapılır");
            CollectionAssert.IsEmpty(sim.Facility.Learned, "zaferde önlem yok");
            Assert.IsNull(sim.S.RaidLog.Last().LiberationReward.Countermeasure);
        }

        [Test]
        public void Learn_NotOnDissolution()
        {
            var sim = NewSim();
            for (int i = 0; i < 2; i++)
            {
                Start(sim);
                Win(sim, First(3, 0, 0, 0, 0, 0));
                ShelterEventKit.Close(sim);
            }
            sim.S.Trust = 1;
            Start(sim);
            Win(sim, (B, i) => { if (i == 0) { AddTally(B, L(3, 0, 0, 0, 0, 0)); B.Collateral = 30; } });
            Assert.IsTrue(sim.LiberationRegion("weu").Completed);
            Assert.AreEqual("dagilma", sim.S.Ending);
            CollectionAssert.IsEmpty(sim.Facility.Learned, "dağılmada önlem yok");
        }

        [Test]
        public void Learn_DrawsNoRng_SameGameAsSubFlagOff()
        {
            uint Run(bool on, out Simulation sim)
            {
                sim = NewSim(b => b.facilityCountermeasures = on);
                Complete(sim, First(3, 0, 0, 0, 2, 0));
                return sim.S.Rng.State;
            }
            uint a = Run(true, out var on);
            uint b = Run(false, out var off);
            Assert.AreEqual(b, a, "sayım ve öğrenme RNG çekmez");
            Assert.AreEqual(off.S.Fon, on.S.Fon);
            Assert.AreEqual(off.S.Trust, on.S.Trust);
            Assert.AreEqual(1, on.Facility.Learned.Count);
            CollectionAssert.IsEmpty(off.Facility.Tally, "alt bayrak kapalı: liste dolmaz");
            CollectionAssert.IsEmpty(off.Facility.Counter);
            CollectionAssert.IsEmpty(off.Facility.Learned);
            Assert.IsNull(off.S.RaidLog.Last().LiberationReward.Countermeasure);
            Assert.IsFalse(off.CountermeasuresOn);
            Assert.AreEqual(0, off.CounterTally(FacilityCounters.Push));
        }

        // ---------- Bayraklar ve emir kopyası ----------
        [Test]
        public void Flags_Off_NoListsNoCopies()
        {
            // Alt bayrak (önlem defteri) kapalı: liste ve emir kopyası yok (ana bayrak tek kipte yok)
            var b = Bal();
            b.facilityCountermeasures = false;
            var sim = ShelterEventKit.New(b, 79);
            Assert.IsFalse(sim.CountermeasuresOn);
            ShelterEventKit.FillSquad(sim);
            CollectionAssert.IsEmpty(sim.LiberationSquadPreview().Counters);
            Start(sim);
            CollectionAssert.IsEmpty(sim.S.Raid.Params.Counters);
            CollectionAssert.IsEmpty(sim.S.Raid.Params.Answers);
            Assert.IsFalse(sim.OpenRaidRoom().Moves.Tally, "tahta saymaz");
            CollectionAssert.IsEmpty(sim.Facility.Tally);
            CollectionAssert.IsEmpty(sim.Facility.Counter);
            CollectionAssert.IsEmpty(sim.Countermeasures);
        }

        [Test]
        public void Order_CopiesCountersAndAnswers_LaterChangesDoNotReachIt()
        {
            var sim = NewSim();
            Grant(sim, FacilityGear.AnsSokum);
            Grant(sim, FacilityGear.AnsRelay);
            sim.Facility.Counter.AddRange(L(1, 0, 0, 2, 0, 1));
            sim.Facility.Tally.AddRange(L(0, 0, 0, 0, 0, 0));
            ShelterEventKit.FillSquad(sim);
            var preview = sim.LiberationSquadPreview();
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 2, 0, 1 }, preview.Counters, "görev fişi önizlemesi");
            CollectionAssert.AreEqual(new[] { FacilityGear.AnsSokum, FacilityGear.AnsRelay }, preview.Answers);
            Start(sim);
            var p = sim.S.Raid.Params;
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 2, 0, 1 }, p.Counters);
            CollectionAssert.AreEqual(new[] { FacilityGear.AnsSokum, FacilityGear.AnsRelay }, p.Answers);
            var B = sim.OpenRaidRoom();
            Assert.AreEqual(1, B.Moves.Bolt);
            Assert.AreEqual(2, B.Moves.ShortClamp);
            Assert.AreEqual(1, B.Moves.Jam);
            Assert.IsTrue(B.Moves.Unbolt);
            Assert.AreEqual(sim.B.facilityRelayTurns, B.Moves.RelayTurns);
            Assert.AreEqual(0, B.Moves.SpringTurns, "açılmamış cevap işlemez");
            // Süren baskın sonradan değişen defterden etkilenmez (kopya)
            sim.Facility.Counter[0] = 2;
            sim.Facility.Recipes.Add(FacilityGear.AnsSpring);
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 2, 0, 1 }, sim.S.Raid.Params.Counters);
            Assert.AreEqual(1, sim.OpenRaidRoom().Moves.Bolt);
            Assert.AreEqual(0, sim.OpenRaidRoom().Moves.SpringTurns);
        }

        // ---------- Cevap tarifleri: defter ----------
        [Test]
        public void AnswerPage_ReplacesBase_SameIndexAndId()
        {
            var sim = NewSim();
            Grant(sim, FacilityGear.Wire);
            Grant(sim, FacilityGear.Clamp);
            var before = sim.BoardRecipes;
            Grant(sim, FacilityGear.AnsSteelWire);
            var after = sim.BoardRecipes;
            Assert.AreEqual(before.Count, after.Count, "cevap yeni sayfa açmaz");
            CollectionAssert.AreEqual(before.Select(r => r.Id), after.Select(r => r.Id), "kimlikler ve dizinler aynı");
            int i = after.FindIndex(r => r.Id == FacilityGear.Wire);
            var page = after[i];
            Assert.AreEqual(FacilityGear.AnsSteelWire, page.Answer);
            Assert.AreEqual(FacilityGear.RecipeName(FacilityGear.AnsSteelWire), page.Name);
            Assert.AreEqual(FacilityGear.Wire, page.Output);
            Assert.AreEqual(before[i].Turns, page.Turns);
            Assert.IsTrue(page.Reward);
            Assert.AreEqual(Qty(before[i], "metal"), Qty(page, "metal"));
            Assert.AreEqual(Qty(before[i], "kumas"), Qty(page, "kumas"));
            Assert.AreEqual(Qty(before[i], "devre") + sim.B.facilityAnswerCircuits, Qty(page, "devre"), "+1 devre (yeni satır)");
            Assert.LessOrEqual(page.Inputs.Count, 3);
            // Öbür sayfalar aynı
            for (int k = 0; k < after.Count; k++)
                if (k != i)
                {
                    Assert.IsNull(after[k].Answer, after[k].Id);
                    Assert.AreEqual(before[k].Name, after[k].Name);
                }
            // Devre satırı olan temel: adedi artar
            Grant(sim, FacilityGear.AnsRelay);
            var clamp = sim.BoardRecipes.Single(r => r.Id == FacilityGear.Clamp);
            Assert.AreEqual(FacilityGear.AnsRelay, clamp.Answer);
            Assert.AreEqual(sim.B.facilityClampCircuits + sim.B.facilityAnswerCircuits, Qty(clamp, "devre"));
            Assert.AreEqual(2, clamp.Inputs.Count);
            Assert.IsTrue(sim.QueueProduction(FacilityGear.Wire).Ok, "iş emri aynı kimlikle");
        }

        [Test]
        public void AnswerPage_BaseNotOpen_NoPage_SpringConvertsLaterBase()
        {
            var sim = NewSim();
            Grant(sim, FacilityGear.AnsPiercer);
            Grant(sim, FacilityGear.AnsSpring);
            Assert.AreEqual(6, sim.BoardRecipes.Count, "temel açık değilse sayfa yok");
            Assert.IsTrue(sim.BoardRecipes.All(r => r.Answer == null));
            Grant(sim, FacilityGear.Winch);
            var winch = sim.BoardRecipes.Single(r => r.Id == FacilityGear.Winch);
            Assert.AreEqual(FacilityGear.AnsSpring, winch.Answer, "sonradan açılan temel de dönüşür (okuma anı)");
            Assert.AreEqual(FacilityGear.RecipeName(FacilityGear.AnsSpring), winch.Name);
            Grant(sim, FacilityGear.Decoy);
            var decoy = sim.BoardRecipes.Single(r => r.Id == FacilityGear.Decoy);
            Assert.AreEqual(FacilityGear.AnsSpring, decoy.Answer);
            Assert.AreEqual(sim.B.facilityDecoyCircuits + 1, Qty(decoy, "devre"));
            Assert.AreEqual(3, decoy.Inputs.Count, "üç girdi türü korunur");
            Assert.AreEqual(7, sim.BoardRecipes.FindIndex(r => r.Id == FacilityGear.Decoy), "açılış sırasıyla");
        }

        [Test]
        public void AnswerPage_SubFlagOff_BookAsStage9()
        {
            var sim = NewSim(b => b.facilityCountermeasures = false);
            Grant(sim, FacilityGear.Wire);
            var before = sim.BoardRecipes;
            Grant(sim, FacilityGear.AnsSteelWire);
            Grant(sim, FacilityGear.AnsSokum);
            var after = sim.BoardRecipes;
            Assert.AreEqual(before.Count, after.Count);
            for (int k = 0; k < after.Count; k++)
            {
                Assert.IsNull(after[k].Answer);
                Assert.AreEqual(before[k].Name, after[k].Name);
                CollectionAssert.AreEqual(before[k].Inputs.Select(m => m.Kind + m.Quantity), after[k].Inputs.Select(m => m.Kind + m.Quantity));
            }
        }

        [Test]
        public void AnswerPage_Sokum_ReplacesTechnicianKit_KitPaths()
        {
            var sim = NewSim(b => b.boardMissingKitCircuits = 0);
            var plain = sim.BoardRecipes.Single(r => r.Id == "kit_tek");
            Grant(sim, FacilityGear.AnsSokum);
            var kit = sim.BoardRecipes.Single(r => r.Id == "kit_tek");
            Assert.AreEqual(3, sim.BoardRecipes.FindIndex(r => r.Id == "kit_tek"), "Teknisyen kitinin sayfası");
            Assert.AreEqual(FacilityGear.AnsSokum, kit.Answer);
            Assert.AreEqual(FacilityGear.RecipeName(FacilityGear.AnsSokum), kit.Name);
            Assert.IsFalse(kit.Reward, "kit gibi: otomatik yol ve eksik kit yolu tanır");
            Assert.AreEqual("kit_tek", kit.Output);
            Assert.AreEqual(Qty(plain, "devre") + 1, Qty(kit, "devre"));
            Assert.IsNull(sim.BoardRecipes.Single(r => r.Id == "kit_emp").Answer, "öbür kitler değişmez");
            // Eksik kit: indirimin üstüne +1 devre
            Stock(sim);
            foreach (var e in sim.Board.Equipment.Where(e => e.Kind == "kit_tek").ToList()) Assert.IsTrue(sim.ScrapEquipment(e.Id).Ok);
            Assert.IsTrue(sim.BoardKitMissing("kit_tek"));
            kit = sim.BoardRecipes.Single(r => r.Id == "kit_tek");
            Assert.AreEqual(sim.B.boardMissingKitCircuits + 1, Qty(kit, "devre"));
            // Otomatik yol: üretim alanı cevap tarifinin girdisini ister
            sim.Board.ProductionInputs.Clear();
            sim.Board.ProductionInputs.Add(new BoardMaterial { Kind = "metal", Quantity = Qty(kit, "metal") });
            Assert.AreNotEqual("kit_tek", sim.AutomaticProductionRecipe?.Id, "devre eksik");
            sim.Board.ProductionInputs.Add(new BoardMaterial { Kind = "devre", Quantity = Qty(kit, "devre") });
            Assert.AreEqual("kit_tek", sim.AutomaticProductionRecipe?.Id);
            Assert.AreEqual(FacilityGear.AnsSokum, sim.AutomaticProductionRecipe.Answer);
            sim.Board.ProductionInputs.Clear();
            // Emir açıkken eksik kit (aşama 8, K19): cevap sürümünün girdisiyle üretilir
            Order(sim, "emp_cell");
            int metal = sim.Board.Material("metal").Quantity, devre = sim.Board.Material("devre").Quantity;
            ShelterEventKit.Close(sim);
            Assert.AreEqual(1, sim.Board.Equipment.Count(e => e.Kind == "kit_tek"), "kit üretildi");
            Assert.AreEqual(metal - Qty(kit, "metal"), sim.Board.Material("metal").Quantity);
            Assert.AreEqual(devre - Qty(kit, "devre"), sim.Board.Material("devre").Quantity, "cevap kitinin devresi ödendi");
        }

        // ---------- Cevap tarifleri: taslak iptali (K22) ----------
        [Test]
        public void AnswerChosen_CancelsDraftOrderOfItsBase()
        {
            var sim = NewSim();
            Stock(sim);
            Grant(sim, FacilityGear.Clamp);
            Order(sim, FacilityGear.Clamp);
            int free = sim.BoardMaterialFree("devre");
            var r = Grant(sim, FacilityGear.AnsRelay);
            Assert.AreEqual(Loc.F("Tarif açıldı: {0}. Taslak iş emri iptal edildi: {1} tarifi değişti.",
                FacilityGear.RecipeName(FacilityGear.AnsRelay), PlanningBoardData.EquipmentName(FacilityGear.Clamp)), r.Msg);
            CollectionAssert.IsEmpty(sim.Board.Orders, "taslak silindi");
            Assert.AreEqual(free + sim.B.facilityClampCircuits, sim.BoardMaterialFree("devre"), "ayrılan hammadde serbest");
            Assert.IsNull(sim.BoardPlanCheck(), "fiş kilitlenmez");
            Order(sim, FacilityGear.Clamp);
            Assert.IsNull(sim.BoardPlanCheck(), "yeniden açılan emir yeni tarifle geçer");
            Assert.AreEqual(sim.B.facilityClampCircuits + 1, sim.Board.Orders[0].Reserves.Single(m => m.Kind == "devre").Quantity);
            ShelterEventKit.Close(sim);

            // Söküm: Teknisyen kitinin taslağı
            var other = NewSim();
            Stock(other);
            Order(other, "kit_tek");
            var s = Grant(other, FacilityGear.AnsSokum);
            Assert.AreEqual(Loc.F("Tarif açıldı: {0}. Taslak iş emri iptal edildi: {1} tarifi değişti.",
                FacilityGear.RecipeName(FacilityGear.AnsSokum), PlanningBoardData.EquipmentName("kit_tek")), s.Msg);
            CollectionAssert.IsEmpty(other.Board.Orders);
        }

        [Test]
        public void AnswerChosen_StartedOrderContinues_OtherDraftsStay()
        {
            var sim = NewSim();
            Stock(sim);
            Grant(sim, FacilityGear.Clamp);
            Order(sim, FacilityGear.Clamp);
            ShelterEventKit.Close(sim);
            var order = sim.Board.Orders.Single();
            Assert.IsTrue(order.Started);
            var r = Grant(sim, FacilityGear.AnsRelay);
            Assert.AreEqual(Loc.F("Tarif açıldı: {0}.", FacilityGear.RecipeName(FacilityGear.AnsRelay)), r.Msg);
            Assert.AreSame(order, sim.Board.Orders.Single(), "başlamış emir sürer");
            Assert.IsNull(sim.BoardPlanCheck());
            ShelterEventKit.Close(sim);
            CollectionAssert.IsEmpty(sim.Board.Orders);
            Assert.AreEqual(1, sim.Board.Equipment.Count(e => e.Kind == FacilityGear.Clamp), "çıktı aynı eşya");

            // Başka temelin taslağı ve alt bayrak kapalı: dokunulmaz
            var other = NewSim();
            Stock(other);
            Order(other, "armor");
            Grant(other, FacilityGear.AnsRelay);
            Assert.AreEqual(1, other.Board.Orders.Count);
            var off = NewSim(b => b.facilityCountermeasures = false);
            Stock(off);
            Grant(off, FacilityGear.Clamp);
            Order(off, FacilityGear.Clamp);
            Assert.AreEqual(Loc.F("Tarif açıldı: {0}.", FacilityGear.RecipeName(FacilityGear.AnsRelay)), Grant(off, FacilityGear.AnsRelay).Msg);
            Assert.AreEqual(1, off.Board.Orders.Count);
            Assert.IsNull(off.BoardPlanCheck(), "sayfa değişmediği için taslak geçerli");
        }

        [Test]
        public void AnswerDesc_AndBases()
        {
            var b = new Balance();
            foreach (string a in FacilityGear.Answers)
            {
                Assert.IsNotEmpty(FacilityGear.AnswerDesc(b, a), a);
                Assert.IsNotEmpty(FacilityGear.AnswerBases(a), a);
                Assert.AreEqual(a, FacilityCounters.AnswerOf(FacilityCounters.ClassOfAnswer(a)), a);
            }
            CollectionAssert.AreEqual(new[] { "kit_tek" }, FacilityGear.AnswerBases(FacilityGear.AnsSokum));
            CollectionAssert.AreEqual(new[] { FacilityGear.Turret, FacilityGear.Decoy, FacilityGear.Winch }, FacilityGear.AnswerBases(FacilityGear.AnsSpring));
            CollectionAssert.IsEmpty(FacilityGear.AnswerBases(FacilityGear.Wire));
            Assert.AreEqual(FacilityGear.AnsSpring, FacilityGear.AnswerFor(FacilityGear.Decoy, new[] { FacilityGear.Wire, FacilityGear.AnsSpring }));
            Assert.IsNull(FacilityGear.AnswerFor(FacilityGear.Wire, new[] { FacilityGear.Wire, FacilityGear.AnsSpring }));
            Assert.IsNull(FacilityGear.AnswerFor(FacilityGear.Dog, FacilityGear.Answers), "köpeğin cevabı yok");
            Assert.AreEqual(Loc.F("Tel +{0} can.", 1), FacilityGear.AnswerDesc(b, FacilityGear.AnsSteelWire));
            Assert.AreEqual("", FacilityGear.AnswerDesc(b, "yok"));
        }

        // ---------- Kayıt ----------
        [Test]
        public void Record_F5_LearningCampaignReplays()
        {
            var sim = NewSim();
            Assert.AreEqual("f10", sim.Record.facility, "güncel tesis izi (saha varsayılan açık)");
            Complete(sim, (B, i) => { if (i == 0) AddTally(B, L(3, 0, 0, 0, 2, 0)); if (i == 3) AddTally(B, L(0, 2, 0, 0, 0, 0)); });
            Assert.AreEqual(1, sim.Facility.Learned.Count);
            Assert.IsTrue(ChooseReward(sim));
            ShelterEventKit.Close(sim);
            Assert.IsTrue(TryStartRaid(sim));
            Win(sim, First(0, 0, 3, 0, 0, 0));
            Assert.IsTrue(sim.S.RaidLog.Last().Rooms[0].EndsWith("|0:0:3:0:0:0"), "raid_room dizgisinde sayaç kuyruğu");
            var r = Restore(sim);
            Assert.AreEqual(State(sim), State(r));
            Assert.AreEqual(CounterLine(sim.Facility), CounterLine(r.Facility));
            Assert.AreEqual(sim.Facility.Learned.Count, r.Facility.Learned.Count);
            // Kayıttan devam: sonraki emir aynı kademeleri taşır
            ShelterEventKit.Close(sim);
            ShelterEventKit.Close(r);
            Assert.IsTrue(TryStartRaid(sim));
            Assert.IsTrue(TryStartRaid(r));
            CollectionAssert.AreEqual(sim.S.Raid.Params.Counters, r.S.Raid.Params.Counters);
            Assert.AreEqual(Baskin.Signature(sim.OpenRaidRoom()), Baskin.Signature(r.OpenRaidRoom()));
        }
    }
}
