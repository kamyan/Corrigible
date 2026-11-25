using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 12, parça P4a (aşama 12 tasarımı §11.1, §11.6, §11.8): masanın Katar ve Yalpa yüzeyleri, sahnesiz kurucularla. Görev
    /// fişinin OLAY satırı ve kimlik emre yazılan şartı okur (<see cref="Simulation.RegionEvent"/>: bayrak açıkken "Katar"/"Yalpa", kapalıyken
    /// bugünkü olay), bina satırının hover'ı şartın notunu (<see cref="Simulation.RegionNote"/>); operasyon raporunda kenardan düşenin satırı
    /// "vagondan düştü · 5 vardiya yatış" (Alert; yatış Core'un sonuçta yazdığı değer, vardiya geçince de aynı). Kurulum
    /// <see cref="FacilityDeskKit.Reach"/> (yalnız komutlar); tahta kurulumu test ışınlamasıdır, rapor ve sonuç gerçek komutlardan.
    /// </summary>
    public sealed class FacilityConditionDeskTests
    {
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void Restore() => Loc.SetLanguage(Loc.Turkish);

        static void UseEnglish()
        {
            var table = Loc.ParseTable(File.ReadAllText(TablePath));
            table.TryGetValue(Loc.English, out var en);
            Loc.SetLanguage(Loc.English, en);
        }

        static SlipField EventRow(OperationSlipData d) => d.Fields.FirstOrDefault(f => f.Label == Loc.T("OLAY"));

        static void AssertEventRow(Simulation sim, string region, string evId)
        {
            var d = LiberationMapBuilder.Slip(sim, region);
            var row = EventRow(d);
            Assert.IsNotNull(row, region + ": OLAY satırı");
            var ev = Baskin.RegionEvents[evId];
            Assert.AreEqual(Loc.T(ev.Name), row.Value, region);
            Assert.AreEqual(Loc.T(ev.Desc), row.HoverLine, region);
            Assert.AreEqual(Loc.F("OLAY · {0}", DeskText.Upper(Loc.T(ev.Name))), row.HoverTitle, region);
            Assert.LessOrEqual(d.Fields.Count, LiberationMapBuilder.SlipFieldsMax, region + ": beş SAHA yuvası");
            StringAssert.Contains(Loc.T(ev.Name) + ": " + Loc.T(ev.Desc), LiberationMapBuilder.Identity(region, true, sim), region + ": kimlik");
        }

        [Test]
        public void Slip_EventRow_KatarAndYalpa_SubFlagsOffTodaysEvent()
        {
            var sim = FacilityDeskKit.Reach(79, "na");
            Assert.AreEqual("na", sim.S.Liberation.CurrentRegion, "Reach: na cephe");
            Assert.IsNull(sim.S.Raid);
            AssertEventRow(sim, "na", "katar");
            AssertEventRow(sim, "oc", "yalpa");
            Assert.AreEqual("Katar", EventRow(LiberationMapBuilder.Slip(sim, "na")).Value);
            Assert.AreEqual("Yalpa", EventRow(LiberationMapBuilder.Slip(sim, "oc")).Value);
            // Emir aynı şartı taşır (okuyucu = emir = tahta)
            FacilityDeskKit.StartFirst(sim);
            Assert.AreEqual("katar", sim.S.Raid.Params.Event);

            // Öbür bölgelerin kimliği imza makinesi alt bayrağından bağımsız
            var on = FacilityDeskKit.New();
            var legacySignature = FacilityDeskKit.Bal();
            legacySignature.facilitySignatureMachines = false;
            var legacy = Simulation.NewGame(79, legacySignature);
            foreach (var def in GameData.Regions.Where(r => r.Id != "na" && r.Id != "oc"))
            {
                Assert.AreEqual(GameData.Region(def.Id).Event, FacilityConditionText.EventOf(on, def.Id), def.Id);
                Assert.AreEqual(LiberationMapBuilder.Identity(def.Id, true), LiberationMapBuilder.Identity(def.Id, true, legacy), def.Id);
            }
            // Simülasyonsuz kimlik bugünkü gibi (eski çağıranlar)
            StringAssert.Contains("Yük asansörü", LiberationMapBuilder.Identity("na", true));

            // Alt bayraklar kapalı: tesis açık ama bugünkü olay
            var bal = FacilityDeskKit.Bal();
            bal.facilityKatar = false;
            bal.facilityYalpa = false;
            var sub = Simulation.NewGame(79, bal);
            AssertEventRow(sub, "na", "asansor");
            AssertEventRow(sub, "oc", "yag");

            // Bölge olayları kapalı: OLAY satırı yok (bugünkü gibi)
            bal = FacilityDeskKit.Bal();
            bal.liberationRegionEvents = false;
            var quiet = Simulation.NewGame(79, bal);
            Assert.IsNull(EventRow(LiberationMapBuilder.Slip(quiet, "na")));
            Assert.IsNull(EventRow(LiberationMapBuilder.Slip(quiet, "oc")));
        }

        [Test]
        public void Slip_EventRow_English()
        {
            UseEnglish();
            var sim = FacilityDeskKit.New();
            Assert.AreEqual("Train", EventRow(LiberationMapBuilder.Slip(sim, "na")).Value);
            Assert.AreEqual("Roll", EventRow(LiberationMapBuilder.Slip(sim, "oc")).Value);
            AssertEventRow(sim, "na", "katar");
            AssertEventRow(sim, "oc", "yalpa");
        }

        [Test]
        public void Slip_BuildingHover_ReadsTheRegionNote()
        {
            var sim = FacilityDeskKit.New();
            foreach (var def in GameData.Regions)
            {
                var d = LiberationMapBuilder.Slip(sim, def.Id);
                Assert.AreEqual(sim.RegionNote(def.Id), d.BuildingHoverLine, def.Id);
                Assert.AreEqual(def.Building, d.Building, def.Id);
            }
            Assert.AreEqual(GameData.Region("na").FacilityNote, LiberationMapBuilder.Slip(sim, "na").BuildingHoverLine, "na: şartın notu");
            Assert.AreEqual(GameData.Region("oc").FacilityNote, LiberationMapBuilder.Slip(sim, "oc").BuildingHoverLine, "oc: şartın notu");
            Assert.AreEqual("Sevkiyat katarı", LiberationMapBuilder.Slip(sim, "na").Building);
            Assert.AreEqual("Yüzer maden platformu", LiberationMapBuilder.Slip(sim, "oc").Building);
            StringAssert.Contains("açık", LiberationMapBuilder.Slip(sim, "na").BuildingHoverLine);
        }

        /// <summary>
        /// Süren na baskınını oynar: G temiz geçilir; A'da (Katar) düz zeminde EMP uzmanı Analist'i kuzey kenarından düşürür; kalan odalar
        /// betikli kazanılır; sonuç raporlardan (<see cref="Simulation.ResolveRaid"/>). Analist'in kimliği döner.
        /// </summary>
        static string OverboardRaid(Simulation sim)
        {
            FacilityDeskKit.StartFirst(sim);
            Assert.AreEqual("katar", sim.S.Raid.Params.Event);
            var G = sim.OpenRaidRoom();
            Assert.AreEqual(1, G.Room.Index);
            FacilityDeskKit.Calm(G);
            FacilityDeskKit.ToGateCore(G);
            Assert.IsTrue(Baskin.Pass(G));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(G)).Ok, "G");

            var A = sim.OpenRaidRoom();
            Assert.AreEqual("katar", A.Ev?.Id, "ara vagon Katar");
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var c = Baskin.CellAt(A, x, y);
                    c.T = "floor"; c.O = null; c.Shutter = false; c.Crate = false; c.Vent = 0; c.Smoke = false;
                }
            FacilityDeskKit.Calm(A);
            A.MaxTurns = 30;
            var team = A.Units.Where(Baskin.IsSpecialist).OrderBy(u => u.Slot).ToList();
            foreach (var u in team)
            {
                (u.X, u.Y) = u.Type == "emp" ? (3, 3) : u.Type == "ana" ? (3, 0) : (5, 5);
            }
            var ana = team.First(u => u.Type == "ana");
            var emp = team.First(u => u.Type == "emp");
            Assert.IsTrue(Baskin.IsOpenEdge(A, ana.X, ana.Y));
            Assert.IsTrue(Baskin.DoAct(A, emp.Id, 3, 1), "EMP");
            ana = A.Units.First(u => u.Id == ana.Id);
            Assert.IsTrue(ana.Overboard && ana.Dead, "Analist vagondan düştü");
            FacilityDeskKit.ToGateCore(A);
            Assert.IsTrue(Baskin.Pass(A));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(A)).Ok, "A");
            for (int guard = 0; guard < 4 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                FacilityDeskKit.PlayRoom(B);
                var ok = sim.AdvanceRaidRoom(sim.RaidRoomReport(B));
                Assert.IsTrue(ok.Ok, "oda " + B.Room.Index + ": " + ok.Msg);
            }
            Assert.IsTrue(sim.RaidRoomsClosed);
            var result = sim.FacilityRaidResult();
            int slot = sim.S.Raid.Params.Squad.FindIndex(s => s.Type == "ana");
            CollectionAssert.AreEqual(new[] { slot }, result.Overboard);
            var res = sim.ResolveRaid(result);
            Assert.IsTrue(res.Ok, res.Msg);
            return sim.S.RaidLog.Last().LiberationReward.Squad.Single(o => o.Type == "ana").PersonId;
        }

        [Test]
        public void TeamRows_Overboard_LaidUpFive_AndStaysFiveAfterTheShift()
        {
            var sim = FacilityDeskKit.Reach(79, "na");
            string personId = OverboardRaid(sim);
            var log = sim.S.RaidLog.Last();
            var o = log.LiberationReward.Squad.Single(x => x.PersonId == personId);
            Assert.IsTrue(o.Overboard && o.Down && !o.Dead, "kenardan düşen yaşar, yatar");
            Assert.AreEqual(5, sim.Board.Person(personId).Down, "Core: facilityFallInjuryTurns");

            var d = OperationReportBuilder.Build(sim);
            var row = d.Results.Single(r => r.Label == Loc.F("{0} · {1}", o.PersonName, Baskin.UnitTypes["ana"].Short));
            Assert.AreEqual("vagondan düştü · 5 vardiya yatış", row.Value);
            Assert.AreEqual(OperationTone.Alert, row.Tone);
            Assert.IsFalse(d.Results.Any(r => r.Value.Contains("kurtarıldı")), "öbür ikisi ayakta döndü; kenar düşüşü 'kurtarıldı' değil");
            StringAssert.Contains(Loc.F("{0} vagondan düştü; {1} vardiya yatış gerekiyor.", o.PersonName, 5), sim.Board.RaidSummary);

            // Yatış Core'un sonuçta yazdığı değerdir: vardiya geçip kişinin kalan yatışı azalınca rapor yine 5 der
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
            Assert.Less(sim.Board.Person(personId).Down, 5, "vardiya geçti");
            var again = OperationReportBuilder.Build(sim, log);
            Assert.AreEqual("vagondan düştü · 5 vardiya yatış", again.Results.Single(r => r.Label == row.Label).Value);

            // Dönüşün alanı değişince satır onu izler (kural kopyalanmaz)
            var tuned = FacilityDeskKit.Reach(79, "na", b => b.facilityFallInjuryTurns = 3);
            string tunedId = OverboardRaid(tuned);
            Assert.AreEqual(3, tuned.Board.Person(tunedId).Down);
            StringAssert.Contains("vagondan düştü · 3 vardiya yatış", string.Join("|", OperationReportBuilder.Build(tuned).Results.Select(r => r.Value)));

            UseEnglish();
            var en = OperationReportBuilder.Build(sim, log);
            StringAssert.Contains("fell off the car · laid up 5 shifts", string.Join("|", en.Results.Select(r => r.Value)));
        }
    }
}
