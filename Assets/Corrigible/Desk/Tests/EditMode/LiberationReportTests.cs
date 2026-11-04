using System;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Operasyon raporu (Revizyon 2 · S7) gerçek görev/ödül kaydını kullanır; kayıttan devamda aynı raporu kurar.</summary>
    public class LiberationReportTests
    {
        static Simulation New() => Simulation.NewGame(731, new Balance { startFon = 100 });

        static OperationReportData Build(Simulation sim) => OperationReportBuilder.Build(sim);

        static void Finish(Simulation sim, string sector, RaidResult result = null)
        {
            while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step();
            Assert.IsTrue(sim.StartLiberationRaid(sector).Ok);
            Assert.IsTrue(sim.ResolveRaid(result ?? new RaidResult { Success = true, Turns = 4 }).Ok);
        }

        static OperationRow Row(OperationReportData report, string label) => report.Results.Single(r => r.Label == Loc.T(label));
        static OperationGain GainOf(OperationReportData report, string label) => report.Gains.Single(g => g.Label == Loc.T(label));
        static string Gain(double amount) => DeskFormat.Signed(amount, Math.Abs(amount - Math.Round(amount)) < 1e-9 ? 0 : 1);

        [Test]
        public void LootReport_ShowsGainsAndSlips_TitleSaysTheResult()
        {
            var sim = New();
            Finish(sim, "weu_1");
            var report = Build(sim);
            Assert.IsTrue(report.Success);
            Assert.AreEqual(Loc.T("SEKTÖR GERİ ALINDI"), report.Title);
            Assert.AreEqual(Loc.T("BAŞARILI"), report.ResultStamp);
            CollectionAssert.AreEqual(new[] { Loc.T("FON"), Loc.T("MALZEME"), Loc.T("ERZAK"), Loc.T("KURTULAN") }, report.Gains.Select(g => g.Label));
            Assert.AreEqual(Gain(sim.B.liberationLootFunds), GainOf(report, "FON").Value);
            Assert.AreEqual(Gain(sim.B.shelterLootSupplies), GainOf(report, "ERZAK").Value);
            Assert.AreEqual(Gain(sim.B.shelterLootMaterials), GainOf(report, "MALZEME").Value);
            Assert.AreEqual(Gain(sim.B.shelterLootResidents), GainOf(report, "KURTULAN").Value);
            Assert.AreEqual(GainOf(report, "FON").Value, report.Slips[1], "FON sayacına aynı fiş");
            Assert.IsNull(report.Slips[0], "VARDİYA sayacına fiş asılmaz");
            Assert.AreEqual(Loc.T("GERİ ALINDI"), report.Results[0].Value);
            Assert.AreEqual(OperationTone.Retaken, report.Results[0].Tone);
            Assert.AreEqual(Loc.F("{0}: {1}/{2} sektör KYGA'da; bölgenin özgürlüğüne {3} sektör kaldı.", Loc.T("Batı Avrupa"), 1, 3, 2), report.Sentence);
        }

        [Test]
        public void FailedReport_HasNoRewardArea_ShowsRecoveryAndAlarmScale()
        {
            var sim = New();
            Finish(sim, "weu_1", new RaidResult { Down = 3, Retreat = true, Turns = 6 });
            var report = Build(sim);
            Assert.IsFalse(report.Success);
            Assert.AreEqual(Loc.T("SEKTÖR İŞGAL ALTINDA"), report.Title);
            Assert.IsEmpty(report.Gains, "başarısız raporda ödül alanı hiç yok");
            Assert.IsTrue(report.Slips.All(s => s == null), "fiş asılmaz");
            var fallen = string.Join(", ", Simulation.Roles.Take(3).Select(t => Baskin.UnitTypes[t].Name));
            var injury = report.Results.Single(r => r.Label == Loc.F("Düştü: {0}", fallen));
            Assert.AreEqual(Loc.F("{0} gün", sim.B.liberationInjuryDays), injury.Value, "düşen uzman yatar, kalıcı kayıp yok");
            var alarm = Row(report, "YZ alarmı");
            Assert.Greater(alarm.AlarmSteps, 0, "başarısızda alarm ölçekle");
            StringAssert.StartsWith("+", alarm.Value, "başarısız baskın YZ alarmını yükseltir");
            CollectionAssert.AreEqual(new[] { Loc.T("İŞGAL ALTINDA"), alarm.Value, Loc.F("{0}/{1} ayakta", 0, 3), injury.Value }, report.Results.Select(r => r.Value),
                "sektör, alarm, ekip ve düşenler; eski dünya cezaları yok");
            Assert.AreEqual(1, sim.S.Teams.Count);
        }

        [Test]
        public void LeftBehind_HasItsOwnRow_ApartFromTheFallen()
        {
            var sim = New();
            Finish(sim, "weu_1", new RaidResult { Success = true, Down = 2, Turns = 8, Hp = { 0, 0, 3 }, Left = { 1 } });
            var report = Build(sim);
            var types = sim.S.RaidLog.Last().LiberationReward.Squad.Select(o => Baskin.UnitTypes[o.Type].Name).ToList();
            var fallen = report.Results.Single(r => r.Label == Loc.F("Düştü: {0}", types[0]));
            var left = report.Results.Single(r => r.Label == Loc.F("Çıkamadı: {0}", types[1]));
            Assert.AreEqual(Loc.F("{0} gün", sim.B.liberationInjuryDays), left.Value);
            Assert.AreEqual(OperationTone.Alert, left.Tone);
            Assert.AreEqual(fallen.Value, left.Value);
            Assert.IsTrue(report.Results.Any(r => r.Value == Loc.F("{0}/{1} ayakta", 1, 3)));
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
            Assert.AreEqual(Loc.T("SEKTÖR GERİ ALINDI"), report.Title);
            Assert.AreEqual(Gain(sim.B.liberationServerFunds + sim.B.liberationCaptureFunds), GainOf(report, "FON").Value);
            Assert.AreEqual(Gain(sim.B.shelterServerMaterials), GainOf(report, "MALZEME").Value);
            Assert.IsTrue(GainOf(report, "ERZAK").Zero && GainOf(report, "KURTULAN").Zero, "sunucudan erzak ve kurtulan gelmez");
            Assert.AreEqual(Loc.F("{0} özgür; haritadan sınırdaki bir bölgeyi seç ve yeni cepheyi aç.", Loc.T("Batı Avrupa")), report.Sentence);
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
            Assert.AreEqual(Loc.F("{0} · {1}", Loc.T("Batı Avrupa"), Loc.T("Teknik depo")), lost.Value);
            Assert.AreEqual(OperationTone.Alert, lost.Tone);
            StringAssert.DoesNotContain(lost.Value, report.Sentence, "karşı saldırı satırda; cümlede tekrar edilmez");
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
            Assert.AreEqual(Gain(1), GainOf(report, "ERZAK").Value);
            Assert.AreEqual(Gain(1), GainOf(report, "MALZEME").Value);
            Assert.IsTrue(GainOf(report, "KURTULAN").Zero);
            Assert.AreEqual(1, sim.S.RaidLog.Last().LiberationReward.Supplies);
            Assert.AreEqual(0, sim.S.RaidLog.Last().LiberationReward.Residents);
        }

        [Test]
        public void NoSentenceAppearsTwice()
        {
            var sim = New();
            Finish(sim, "weu_1");
            Finish(sim, "weu_2", new RaidResult { Success = false, Collateral = 2, Down = 1, Turns = 6 });
            foreach (var report in new[] { Build(sim), OperationReportBuilder.Build(sim, sim.S.RaidLog[0]) })
            {
                var texts = new[] { report.Title, report.Sentence, report.ResultStamp }
                    .Concat(report.Results.SelectMany(r => new[] { r.Label, r.Value })).Concat(report.Gains.Select(g => g.Label)).ToList();
                Assert.AreEqual(texts.Count, texts.Distinct().Count(), string.Join(" | ", texts));
            }
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
            Assert.AreEqual(original.Title, replayed.Title);
            Assert.AreEqual(original.Sentence, replayed.Sentence);
            CollectionAssert.AreEqual(original.Results.Select(r => r.Value), replayed.Results.Select(r => r.Value));
            CollectionAssert.AreEqual(original.Gains.Select(g => g.Value), replayed.Gains.Select(g => g.Value));
        }
    }
}
