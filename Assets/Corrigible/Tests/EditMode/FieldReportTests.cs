using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>Saha fişlerinin fon sınırı, erişimi, algı güvenliği ve kayıt/devam davranışı.</summary>
    public class FieldReportTests
    {
        static Balance StableBalance() => new Balance
        {
            growth = 0, spread = 0, tek = 0, coreTek = 0, cpRate = 0, depRate = 0,
            centralFund = 12.5, regionalFundMul = 0,
        };

        static Simulation Ready(bool teams = false, double scale = 1)
        {
            return Simulation.NewGame(null, "solucan", 517u, StableBalance(),
                teams ? new ExpOptions { Teams = true, TimeScale = scale } : null,
                new RuleOptions { Stage = 1, FieldReports = true });
        }

        static void Days(Simulation sim, int count)
        {
            for (int i = 0; i < count; i++) sim.Step();
        }

        static FieldReport FirstReport(Simulation sim)
        {
            sim.S.Region(Simulation.HQ).Monitored = true;
            Days(sim, sim.Dur(sim.B.reportIntervalDays));
            Assert.IsNotEmpty(sim.PendingFieldReports);
            return sim.PendingFieldReports[0];
        }

        [Test]
        public void NoCoverage_AccumulatesNeitherBudgetNorReports()
        {
            var sim = Ready();
            Days(sim, 40);
            Assert.AreEqual(0, sim.S.FieldReportBudget);
            Assert.IsEmpty(sim.PendingFieldReports);
            Assert.IsTrue(sim.LastIncome.Total > 0, "Normal gelir çalışırken saha ödeneği kapsam gerektirir.");
        }

        [Test]
        public void Eligibility_RequiresOnlineMembershipAndNetworkOrStationedHealthyTeam()
        {
            var sim = Ready(true);
            var r = sim.S.Region(Simulation.HQ);
            Assert.IsTrue(sim.EligibleForFieldReport(r.Id));
            foreach (var t in sim.S.Teams) { t.Dest = "na"; t.Eta = 5; }
            Assert.IsFalse(sim.EligibleForFieldReport(r.Id), "Yoldaki ekip eski konumunu kapsamaz.");
            var team = sim.S.Teams[0]; team.Dest = null; team.Down = 2;
            Assert.IsFalse(sim.EligibleForFieldReport(r.Id));
            team.Down = 0; team.Busy = 2;
            Assert.IsTrue(sim.EligibleForFieldReport(r.Id), "Yerleşik ekip görevdeyken de saha varlığı sağlar.");
            r.Off = 1; Assert.IsFalse(sim.EligibleForFieldReport(r.Id));
            r.Off = 0; r.Left = true; Assert.IsFalse(sim.EligibleForFieldReport(r.Id));
            r.Left = false; team.Down = 2; r.Monitored = true;
            Assert.IsTrue(sim.EligibleForFieldReport(r.Id), "Sağlam izleme ağı ekipten bağımsız çalışır.");
            Assert.IsFalse(sim.EligibleForFieldReport("missing"));
        }

        [Test]
        public void Collection_PaysOnceAddsUnverifiedNewsAndRecordsTheCommand()
        {
            var sim = Ready();
            var report = FirstReport(sim);
            double fon = sim.S.Fon;
            int commands = sim.Record.commands.Count;
            Assert.IsFalse(sim.S.News.Any(h => h.Kind == "field"), "Haber fiş toplanmadan yayınlanmaz.");
            Assert.IsTrue(sim.CollectFieldReport(report.Id).Ok);
            Assert.AreEqual(fon + 1, sim.S.Fon);
            Assert.AreEqual(commands + 1, sim.Record.commands.Count);
            Assert.AreEqual("report", sim.Record.commands.Last().kind);
            Assert.IsTrue(sim.TransactionLedger.Any(t => t.Kind == "report" && t.Fon == 1));
            var h = sim.S.News.First(x => x.Kind == "field");
            Assert.AreEqual(report.Headline, h.Text);
            Assert.AreEqual(report.Day, h.Day);
            Assert.IsNull(h.Verified);
            uint rng = sim.S.Rng.State;
            Assert.IsFalse(sim.CollectFieldReport(report.Id).Ok);
            Assert.AreEqual(fon + 1, sim.S.Fon);
            Assert.AreEqual(commands + 1, sim.Record.commands.Count);
            Assert.AreEqual(rng, sim.S.Rng.State);
        }

        [Test]
        public void FieldEstimate_CannotBeDeclaredTrueByNewsVerification()
        {
            var sim = Ready();
            var report = FirstReport(sim);
            Assert.IsTrue(sim.CollectFieldReport(report.Id).Ok);
            var h = sim.S.News.First(x => x.Kind == "field");
            sim.S.Research.Done.Add("dogrulama");
            double sg = sim.S.Sg; uint rng = sim.S.Rng.State; int commands = sim.Record.commands.Count;
            Assert.IsFalse(sim.Verify(h.Id).Ok);
            Assert.IsNull(h.Verified);
            Assert.AreEqual(sg, sim.S.Sg);
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(commands, sim.Record.commands.Count);
        }

        [Test]
        public void PaidAndPendingRewards_NeverExceedEightPercentOfEligibleIncome()
        {
            var sim = Ready();
            sim.B.centralFund = 2;
            sim.B.reportIntervalDays = 1;
            sim.B.reportRegionCooldownDays = 1;
            sim.B.reportLifetimeDays = 3;
            foreach (var r in sim.S.Regions) r.Monitored = true;
            double income = 0, paid = 0;
            for (int i = 0; i < 200; i++)
            {
                sim.Step();
                income += sim.LastIncome.Total;
                if (i % 4 != 0)
                    foreach (var report in sim.PendingFieldReports.ToArray())
                    {
                        Assert.IsTrue(sim.CollectFieldReport(report.Id).Ok);
                        paid += report.Reward;
                    }
                double reserved = sim.PendingFieldReports.Sum(r => r.Reward);
                Assert.LessOrEqual(paid + reserved + sim.S.FieldReportBudget, income * 0.08 + 1e-9);
                Assert.LessOrEqual(sim.S.FieldReportBudget, 2);
            }
            Assert.Greater(paid, 20);
        }

        [Test]
        public void GlobalIntervalAndPendingLimit_ApplyAcrossAllRegions()
        {
            var sim = Ready();
            foreach (var r in sim.S.Regions) r.Monitored = true;
            Days(sim, 5); Assert.IsEmpty(sim.PendingFieldReports);
            sim.Step(); Assert.AreEqual(1, sim.PendingFieldReports.Count);
            Assert.AreEqual(6, sim.PendingFieldReports[0].Day);
            Days(sim, 5); Assert.AreEqual(1, sim.PendingFieldReports.Count);
            sim.Step(); Assert.AreEqual(2, sim.PendingFieldReports.Count);
            Assert.AreEqual(12, sim.PendingFieldReports[1].Day);
            Assert.AreNotEqual(sim.PendingFieldReports[0].Region, sim.PendingFieldReports[1].Region);
            Days(sim, 12); Assert.AreEqual(2, sim.PendingFieldReports.Count);
        }

        [Test]
        public void CollectingOrMovingTeams_DoesNotResetRegionOrGlobalCooldown()
        {
            var sim = Ready(true);
            var report = FirstReport(sim);
            int cooldown = sim.S.FieldReportCooldowns[report.Region];
            int next = sim.S.NextFieldReportDay;
            Assert.IsTrue(sim.CollectFieldReport(report.Id).Ok);
            Assert.IsTrue(sim.MoveTeam(sim.S.Teams[0].Id, "na").Ok);
            Assert.AreEqual(cooldown, sim.S.FieldReportCooldowns[report.Region]);
            Assert.AreEqual(next, sim.S.NextFieldReportDay);
            // Uzak ekip başka bir bölgeyi üretime açmasın; bu test yalnız HQ takvimini ölçer.
            sim.S.Teams.Clear();
            Days(sim, cooldown - sim.S.Day - 1);
            Assert.IsEmpty(sim.PendingFieldReports);
            sim.Step();
            Assert.AreEqual(1, sim.PendingFieldReports.Count);
            Assert.AreEqual(cooldown, sim.PendingFieldReports[0].Day);
        }

        [TestCase(1d, 6, 30, 12)]
        [TestCase(1.5d, 9, 45, 18)]
        [TestCase(2d, 12, 60, 24)]
        public void Timers_UseTheSimulationTimeScale(double scale, int interval, int lifetime, int cooldown)
        {
            var sim = Ready(true, scale);
            var report = FirstReport(sim);
            Assert.AreEqual(interval, report.Day);
            Assert.AreEqual(lifetime, report.ExpiresDay - report.Day);
            Assert.AreEqual(cooldown, sim.S.FieldReportCooldowns[report.Region] - report.Day);
        }

        [Test]
        public void Expiration_ForfeitsReservedRewardAndArchivesTheOriginalDay()
        {
            var sim = Ready();
            sim.B.reportLifetimeDays = 2;
            var report = FirstReport(sim);
            sim.B.reportIncomeShare = 0;
            double budget = sim.S.FieldReportBudget;
            Days(sim, 2);
            Assert.IsFalse(sim.PendingFieldReports.Any(r => r.Id == report.Id));
            Assert.AreEqual(budget, sim.S.FieldReportBudget, "Ödül geri dönüşerek yeni fiş finanse etmemeli.");
            Assert.IsFalse(sim.TransactionLedger.Any(t => t.Kind == "report"));
            Assert.IsTrue(sim.S.News.Any(h => h.Kind == "field" && h.Text == report.Headline && h.Day == report.Day));
        }

        [Test]
        public void LostCoverage_CancelsTheSlipWithoutRefundingItsReward()
        {
            var sim = Ready();
            var report = FirstReport(sim);
            sim.S.Region(report.Region).Monitored = false;
            double budget = sim.S.FieldReportBudget;
            sim.Step();
            Assert.IsEmpty(sim.PendingFieldReports);
            Assert.AreEqual(budget, sim.S.FieldReportBudget);
            Assert.IsFalse(sim.S.News.Any(h => h.Kind == "field"));
        }

        [TestCase("expired")]
        [TestCase("blocked")]
        [TestCase("ended")]
        [TestCase("offline")]
        [TestCase("left")]
        [TestCase("missing")]
        public void InvalidCollection_IsAtomic(string reason)
        {
            var sim = Ready();
            var report = FirstReport(sim);
            string id = report.Id;
            if (reason == "expired") report.ExpiresDay = sim.S.Day;
            if (reason == "blocked") sim.S.PendingEvent = new PendingDilemma();
            if (reason == "ended") sim.S.Ending = "silme";
            if (reason == "offline") sim.S.Region(report.Region).Off = 1;
            if (reason == "left") sim.S.Region(report.Region).Left = true;
            if (reason == "missing") id = "not-a-report";
            double fon = sim.S.Fon; uint rng = sim.S.Rng.State;
            int commands = sim.Record.commands.Count, news = sim.S.News.Count;
            Assert.IsFalse(sim.CollectFieldReport(id).Ok);
            Assert.AreEqual(fon, sim.S.Fon);
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.AreEqual(news, sim.S.News.Count);
            Assert.AreEqual(1, sim.PendingFieldReports.Count);
        }

        [Test]
        public void Illusion_EqualDisplayedValuesProduceEqualWeightsDespiteDifferentReality()
        {
            var sim = Ready();
            sim.S.Day = 10;
            sim.S.Illusion = new IllusionState { Start = 0 };
            var a = sim.S.Region("na"); var b = sim.S.Region("cn");
            a.Monitored = b.Monitored = true;
            a.FakeLow = b.FakeLow = 2;
            a.Inf = 95; b.Inf = 5;
            Assert.AreEqual(sim.Shown(a).V, sim.Shown(b).V);
            Assert.AreEqual(sim.FieldReportWeight(a.Id), sim.FieldReportWeight(b.Id));
        }

        [Test]
        public void HighShownInfiltration_IsCappedAndDuplicateTeamsDoNotMultiplyCoverageBonus()
        {
            var sim = Ready(true);
            var r = sim.S.Region(Simulation.HQ);
            r.Monitored = true; r.Inf = 0;
            Assert.AreEqual(1.25, sim.FieldReportWeight(r.Id), 1e-12);
            var first = sim.S.Teams[0];
            sim.S.Teams.RemoveRange(1, sim.S.Teams.Count - 1);
            Assert.AreEqual(1.25, sim.FieldReportWeight(r.Id), 1e-12);
            r.Inf = 60;
            Assert.AreEqual(1.875, sim.FieldReportWeight(r.Id), 1e-12);
            r.Inf = 100;
            Assert.AreEqual(1.875, sim.FieldReportWeight(r.Id), 1e-12);
            first.Down = 1;
            Assert.AreEqual(1.5, sim.FieldReportWeight(r.Id), 1e-12);
        }

        static GameRecord CopyRecord(GameRecord source) => new GameRecord
        {
            version = source.version, ruleVersion = source.ruleVersion, type = source.type, seed = source.seed,
            hasProfile = source.hasProfile, profileAtStart = source.profileAtStart?.Clone(), balance = source.balance.Clone(),
            hasExp = source.hasExp, exp = source.exp?.Clone(), hasRules = source.hasRules, rules = source.rules?.Clone(),
            day = source.day, commands = source.commands.Select(c => new SavedCommand { day = c.day, kind = c.kind, a = c.a, b = c.b }).ToList(),
        };

        static void AssertReplay(Simulation sim)
        {
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(CopyRecord(sim.Record));
            Assert.AreEqual(sim.S.Day, restored.S.Day);
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(sim.S.Fon, restored.S.Fon);
            Assert.AreEqual(sim.S.FieldReportBudget, restored.S.FieldReportBudget);
            Assert.AreEqual(sim.S.NextFieldReportDay, restored.S.NextFieldReportDay);
            Assert.AreEqual(sim.S.FieldReportSequence, restored.S.FieldReportSequence);
            CollectionAssert.AreEqual(sim.S.FieldReportCooldowns, restored.S.FieldReportCooldowns);
            CollectionAssert.AreEqual(sim.PendingFieldReports.Select(r => r.Id + ":" + r.Region + ":" + r.Day),
                restored.PendingFieldReports.Select(r => r.Id + ":" + r.Region + ":" + r.Day));
            CollectionAssert.AreEqual(sim.S.News.Select(h => h.Id + ":" + h.Text), restored.S.News.Select(h => h.Id + ":" + h.Text));
        }

        [Test]
        public void NewGameReplay_RestoresCollectedAndPendingReportsExactly()
        {
            var sim = Ready();
            Assert.IsTrue(sim.Act(Actions.Monitor, Simulation.HQ).Ok);
            Days(sim, 6);
            Assert.IsTrue(sim.CollectFieldReport(sim.PendingFieldReports[0].Id).Ok);
            Days(sim, 12);
            Assert.AreEqual(1, sim.PendingFieldReports.Count);
            AssertReplay(sim);
        }

        [Test]
        public void EnablingExistingSave_IsRecordedWithoutChangingTheInitialRules()
        {
            var sim = Simulation.NewGame(null, "solucan", 517u, StableBalance());
            Assert.IsTrue(sim.Act(Actions.Monitor, Simulation.HQ).Ok);
            Days(sim, 10);
            Assert.IsTrue(sim.EnableFieldReports().Ok);
            Assert.IsFalse(sim.Record.hasRules);
            Assert.IsNull(sim.Record.rules);
            Assert.AreEqual("reports_enable", sim.Record.commands.Last().kind);
            Assert.AreEqual(16, sim.S.NextFieldReportDay);
            Days(sim, 6);
            Assert.IsTrue(sim.CollectFieldReport(sim.PendingFieldReports[0].Id).Ok);
            Days(sim, 4);
            AssertReplay(sim);
        }

        [Test]
        public void DisabledReports_PreserveBaseRngAndEconomicState()
        {
            var baseline = Simulation.NewGame(null, "solucan", 517u, StableBalance());
            var disabled = Simulation.NewGame(null, "solucan", 517u, StableBalance(), rules: new RuleOptions { Stage = 4 });
            Assert.IsTrue(baseline.Act(Actions.Monitor, Simulation.HQ).Ok);
            Assert.IsTrue(disabled.Act(Actions.Monitor, Simulation.HQ).Ok);
            Days(baseline, 45); Days(disabled, 45);
            Assert.IsFalse(disabled.ReportsOn);
            Assert.IsEmpty(disabled.PendingFieldReports);
            Assert.AreEqual(baseline.S.Rng.State, disabled.S.Rng.State);
            Assert.AreEqual(baseline.S.Fon, disabled.S.Fon);
            CollectionAssert.AreEqual(baseline.S.Regions.Select(r => r.Inf), disabled.S.Regions.Select(r => r.Inf));
        }
    }
}
