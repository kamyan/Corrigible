using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Baskın raporu gerçek görev/ödül kaydını kullanır; kayıttan devamda aynı raporu kurar.</summary>
    public class LiberationReportTests
    {
        static Simulation New() => Simulation.NewGame(731, new Balance { startFon = 100 });

        static EventVM Build(Simulation sim) => new SimDeskBuilder().Build(sim, new DeskBridgeState(), false).Event;

        static void Finish(Simulation sim, string sector, RaidResult result = null)
        {
            while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step();
            Assert.IsTrue(sim.StartLiberationRaid(sector).Ok);
            Assert.IsTrue(sim.ResolveRaid(result ?? new RaidResult { Success = true, Turns = 4 }).Ok);
        }

        static EventLineVM Row(EventVM report, string label) => report.Doc.Lines.Single(r => r.Label == Loc.T(label));
        static string Gain(double amount) => Loc.F("+{0}", DeskFormat.Number(amount));

        [Test]
        public void LootReport_ShowsLootRewards_WithoutLegacyCoreOrPatchClaims()
        {
            var sim = New();
            Finish(sim, "weu_1");
            var report = Build(sim);
            Assert.AreEqual(DeskText.Upper(Loc.T("İKMAL ALINDI")), report.Memo.Title);
            Assert.AreEqual(Gain(sim.B.liberationLootFunds), Row(report, "Fon").Value);
            Assert.AreEqual(Gain(sim.B.shelterLootSupplies), Row(report, "Erzak").Value);
            Assert.AreEqual(Gain(sim.B.shelterLootMaterials), Row(report, "Malzeme").Value);
            Assert.AreEqual(Loc.F("+{0}", sim.B.shelterLootResidents), Row(report, "Kurtulan").Value);
            Assert.IsFalse(report.Doc.Lines.Any(r => r.Label == Loc.T("Yama (büyüme yarıya iner)")));
            Assert.AreNotEqual(Loc.T("<b>Çekirdek yok edildi.</b> Kalan kopyaları temizle."), report.Doc.Risk);
            Assert.AreEqual("Doc:Close", report.Doc.Buttons.Single().Command);
        }

        [Test]
        public void FailedReport_ShowsRecovery_InsteadOfPermanentLossOrWorldPenalties()
        {
            var sim = New();
            Finish(sim, "weu_1", new RaidResult { Down = 3, Retreat = true, Turns = 6 });
            var report = Build(sim);
            var fallen = string.Join(", ", Simulation.Roles.Select(t => Baskin.UnitTypes[t].Name));
            var injury = report.Doc.Lines.Single(r => r.Label == Loc.F("Düştü: {0}", fallen));
            Assert.AreEqual(Loc.F("{0} gün", sim.B.liberationInjuryDays), injury.Value, "düşen uzman yatar, kalıcı kayıp yok");
            var alarm = Row(report, "YZ alarmı");
            Assert.AreEqual("threat", alarm.Status, "başarısız baskın YZ alarmını yükseltir");
            Assert.AreEqual(3, report.Doc.Lines.Count, "Sektör durumu, düşenler ve YZ alarmı; eski dünya cezaları yok.");
            Assert.IsFalse(report.Doc.Lines.Any(r => r.Label == Loc.T("Fon")));
            Assert.AreEqual(Loc.T("İŞGAL ALTINDA"), report.Doc.Lines[0].Value);
            Assert.AreEqual(1, sim.S.Teams.Count);
        }

        [Test]
        public void ServerCompletion_UsesRecordedSectorAndCompletionReward()
        {
            var sim = New();
            Finish(sim, "weu_1"); Finish(sim, "weu_2"); Finish(sim, "weu_3");
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            var report = Build(sim);
            var log = sim.S.RaidLog.Last();
            Assert.AreEqual("weu_3", log.SectorId);
            Assert.AreEqual("server", log.MissionKind);
            Assert.IsTrue(log.LiberationReward.RegionCompleted);
            Assert.AreEqual(DeskText.Upper(Loc.T("SUNUCU YIKILDI")), report.Memo.Title);
            Assert.AreEqual(Gain(sim.B.liberationServerFunds + sim.B.liberationCaptureFunds), Row(report, "Fon").Value);
            Assert.AreEqual(Gain(sim.B.shelterServerMaterials), Row(report, "Malzeme").Value);
            Assert.IsFalse(report.Doc.Lines.Any(r => r.Label == Loc.T("Kurtulan") || r.Label == Loc.T("Erzak")));
            Assert.AreEqual(Loc.T("Bölge özgür. Haritadan sınırdaki bir bölgeyi seç ve yeni cepheyi aç."), report.Doc.Risk);
        }

        [Test]
        public void FailedRaidOverAlarmThreshold_ReportsCounterattack()
        {
            var sim = New();
            Finish(sim, "weu_1");
            while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step();
            sim.S.Alarm = sim.B.liberationAlarmThreshold - 1;
            Finish(sim, "weu_2", new RaidResult { Success = false, Turns = 6 });
            Assert.AreEqual(1, sim.S.Counterattacks);
            var report = Build(sim);
            Assert.AreEqual(Loc.T("EŞİK AŞILDI"), Row(report, "YZ alarmı").Value);
            var lost = Row(report, "YZ karşı saldırısı");
            Assert.AreEqual(Loc.F("{0} · {1}", Loc.T("Batı Avrupa"), Loc.T("Tedarik deposu")), lost.Value);
            Assert.AreEqual(Loc.F("YZ karşı saldırdı: {0} yeniden işgal altında. Alarm sıfırlandı.", lost.Value), report.Doc.Risk);
            Assert.IsFalse(sim.LiberationSector("weu_1").Captured);
        }

        [Test]
        public void LootStorageCaps_ReportOnlyResourcesActuallyReceived()
        {
            var sim = New();
            sim.Shelter.Supplies = ShelterLogic.SuppliesCapacity(sim.Shelter, sim.B) - 1;
            sim.Shelter.Materials = sim.B.shelterMaterialsStorage - 1;
            sim.Shelter.Population = ShelterLogic.Capacity(sim.Shelter, sim.B);
            Finish(sim, "weu_1");
            var report = Build(sim);
            Assert.AreEqual(Gain(1), Row(report, "Erzak").Value);
            Assert.AreEqual(Gain(1), Row(report, "Malzeme").Value);
            Assert.IsFalse(report.Doc.Lines.Any(r => r.Label == Loc.T("Kurtulan")));
            Assert.AreEqual(1, sim.S.RaidLog.Last().LiberationReward.Supplies);
            Assert.AreEqual(0, sim.S.RaidLog.Last().LiberationReward.Residents);
        }

        [Test]
        public void SaveReplay_ReconstructsMissionAndActualRewardMetadata()
        {
            var sim = New();
            Finish(sim, "weu_1"); Finish(sim, "weu_2"); Finish(sim, "weu_3");
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(JsonUtility.FromJson<GameRecord>(JsonUtility.ToJson(sim.Record)));
            Assert.AreEqual(JsonUtility.ToJson(sim.S.RaidLog.Last()), JsonUtility.ToJson(restored.S.RaidLog.Last()));
            var original = Build(sim);
            var replayed = Build(restored);
            Assert.AreEqual(original.Memo.Title, replayed.Memo.Title);
            CollectionAssert.AreEqual(original.Doc.Lines.Select(r => r.Value), replayed.Doc.Lines.Select(r => r.Value));
            Assert.AreEqual(original.Doc.Risk, replayed.Doc.Risk);
        }
    }
}
