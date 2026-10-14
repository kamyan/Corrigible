using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.EditorTools;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>Yeni ekonominin para, erişim, RNG ve komutla tekrar oynatma sınırları.</summary>
    public class EconomyTests
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static readonly string[] Nodes = { "anomali_1", "anomali_2", "dogrulama", "guclu_1", "guclu_2", "kuantum", "bilinc_1", "bilinc_2", "analog" };

        static Simulation Ready(bool teams = false, double scale = 1.5, Balance balance = null)
        {
            var sim = Simulation.NewGame(null, "manipulator", 73, balance ?? new Balance { growth = 0, spread = 0, coreTek = 0, sabotage = 0 },
                new ExpOptions { Teams = teams, TimeScale = scale });
            sim.S.Aware = true;
            sim.S.NextGift = int.MaxValue; sim.S.NextNews = int.MaxValue;
            foreach (var r in sim.S.Regions) { r.Inf = 0; r.Trust = 70; r.Monitored = true; r.Detected = true; }
            return sim;
        }

        [Test]
        public void NewGame_UsesModernReserveAndEmptyUpgradeState()
        {
            var sim = Simulation.NewGame(null, "manipulator", 7);
            Assert.AreEqual(3, sim.S.EconomyVersion);
            Assert.AreEqual(3, sim.Record.version); Assert.AreEqual(3, sim.Record.ruleVersion);
            Assert.AreEqual(60, sim.S.Fon); Assert.AreEqual(5, sim.S.Sg);
            Assert.AreEqual(15, sim.S.ResearchReserve);
            Assert.IsEmpty(sim.S.Operations); Assert.IsEmpty(sim.S.RecoveryJobs);
            Assert.AreEqual(0, sim.S.BudgetLevel);
            Assert.AreEqual(255, Nodes.Sum(sim.OperationCost));
        }

        [Test]
        public void Income_CentralRegionalAndBudgetUseSeparateMultipliers()
        {
            var sim = Ready(); var s = sim.S;
            Assert.AreEqual(2.404, sim.FundRate, 1e-12);
            s.Aware = false; Assert.AreEqual(1.2616, sim.FundRate, 1e-12);
            Assert.IsFalse(sim.BuyBudgetUpgrade().Ok);
            s.Aware = true; s.Fon = 100;
            uint rng = s.Rng.State;
            Assert.IsTrue(sim.BuyBudgetUpgrade().Ok);
            Assert.AreEqual(2.6896, sim.FundRate, 1e-12);
            Assert.IsTrue(sim.BuyBudgetUpgrade().Ok);
            Assert.AreEqual(3.0704, sim.FundRate, 1e-12);
            Assert.AreEqual(35, s.Fon, 1e-12);
            Assert.AreEqual(rng, s.Rng.State, "Bütçe yatırımı haber/RNG üretmemeli");
            Assert.AreEqual(0.5, sim.IncomeBreakdown.Central, 1e-12);
            Assert.AreEqual(1.904, sim.IncomeBreakdown.RegionalBase, 1e-12);
            Assert.AreEqual(0.6664, sim.IncomeBreakdown.BudgetBonus, 1e-12);
            Assert.IsFalse(sim.BuyBudgetUpgrade().Ok);
            foreach (var r in s.Regions) { r.Left = true; r.Off = 3; }
            Assert.AreEqual(0.5, sim.FundRate, 1e-12, "Merkez ödeneği bölgesel ekonomik katkıdan bağımsızdır");
        }

        [Test]
        public void Operations_AreAtomicInstantAndDoNotReplaceAlignment()
        {
            var sim = Ready(); var s = sim.S; s.Fon = 1000;
            Assert.IsTrue(sim.StartResearch("yorum").Ok);
            sim.Step(); double progress = s.Research.Prog;
            int count = sim.Record.commands.Count, news = s.News.Count;
            Assert.IsTrue(sim.BuyOperation("guclu_1").Ok);
            Assert.AreEqual(1, sim.OperationLevel("guclu"));
            Assert.IsFalse(s.Done("guclu"));
            Assert.IsTrue(sim.BuyOperation("guclu_2").Ok);
            Assert.AreEqual(2, sim.OperationLevel("guclu")); Assert.IsTrue(s.Done("guclu"));
            Assert.AreEqual("yorum", s.Research.Cur); Assert.AreEqual(progress, s.Research.Prog);
            Assert.AreEqual(news + 2, s.News.Count);
            Assert.AreEqual(count + 2, sim.Record.commands.Count);
            double fon = s.Fon; uint rng = s.Rng.State;
            Assert.IsFalse(sim.BuyOperation("guclu_2").Ok);
            Assert.AreEqual(fon, s.Fon); Assert.AreEqual(rng, s.Rng.State);
            Assert.AreEqual(count + 2, sim.Record.commands.Count);
            Assert.IsFalse(sim.StartResearch("anomali").Ok);
        }

        [TestCase("anomali_2")]
        [TestCase("dogrulama")]
        [TestCase("guclu_2")]
        [TestCase("kuantum")]
        [TestCase("bilinc_2")]
        [TestCase("analog")]
        [TestCase("butce_2")]
        public void OperationPrerequisiteFailure_DoesNotSpendOrRecord(string id)
        {
            var sim = Ready(); sim.S.Fon = 1000;
            uint rng = sim.S.Rng.State;
            Assert.IsFalse(sim.BuyOperation(id).Ok);
            Assert.AreEqual(1000, sim.S.Fon); Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.IsEmpty(sim.Record.commands); Assert.IsEmpty(sim.TransactionLedger);
        }

        [TestCase("operation")]
        [TestCase("recovery")]
        [TestCase("diplomacy")]
        public void UnaffordableCommand_DoesNotCreateDebtJobsOrHistory(string kind)
        {
            var sim = Ready(); sim.S.Fon = 1; sim.S.Sg = 0;
            sim.S.Region("weu").Econ = 45; sim.S.Region("weu").Trust = 50;
            uint rng = sim.S.Rng.State;
            var result = kind == "operation" ? sim.BuyOperation("anomali_1")
                : kind == "recovery" ? sim.StartRecovery("weu") : sim.DiplomaticTalk("weu");
            Assert.IsFalse(result.Ok);
            Assert.AreEqual(1, sim.S.Fon); Assert.AreEqual(0, sim.S.Sg); Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.IsEmpty(sim.S.RecoveryJobs); Assert.IsEmpty(sim.S.Operations);
            Assert.IsEmpty(sim.Record.commands); Assert.IsEmpty(sim.TransactionLedger);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Verification_IsUnavailableWhenMediaRulesAreOff(int stage)
        {
            var sim = Simulation.NewGame(null, "manipulator", 7, rules: new RuleOptions { Stage = stage });
            sim.S.Fon = 100;
            Assert.IsTrue(sim.BuyOperation("anomali_1").Ok); Assert.IsTrue(sim.BuyOperation("anomali_2").Ok);
            double fon = sim.S.Fon; uint rng = sim.S.Rng.State; int commands = sim.Record.commands.Count;
            Assert.IsFalse(sim.BuyOperation("dogrulama").Ok);
            Assert.AreEqual(fon, sim.S.Fon); Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(commands, sim.Record.commands.Count);
        }

        [Test]
        public void NetworkTiers_HalveSabotageAndReduceOnlyCrossRegionSpread()
        {
            var sim = Ready(); var s = sim.S; s.Fon = 100;
            sim.B.sabotage = 0.1;
            foreach (var r in s.Regions) r.Inf = 50;
            Assert.AreEqual(0.05, sim.SabotageChance, 1e-12);
            Assert.IsTrue(sim.BuyOperation("guclu_1").Ok);
            Assert.AreEqual(0.0375, sim.SabotageChance, 1e-12);
            Assert.IsTrue(sim.BuyOperation("guclu_2").Ok);
            Assert.AreEqual(0.025, sim.SabotageChance, 1e-12);
            sim.B.spread = 0.3;
            foreach (var r in s.Regions) r.Inf = 0;
            s.Region("weu").Inf = 50;
            sim.Step();
            Assert.AreEqual(50, s.Region("weu").Inf, 1e-12);
            Assert.AreEqual(0.07, s.Region("na").Inf, 1e-12, "Son kademe toplam %30 keser, iki indirim üst üste çarpılmaz");
        }

        [Test]
        public void Quantum_BlocksSatelliteLeaksButDoesNotBuyQuarantine()
        {
            var sim = Ready(); var s = sim.S; s.Fon = 200; sim.B.spread = 0.3;
            Assert.IsTrue(sim.BuyOperation("guclu_1").Ok); Assert.IsTrue(sim.BuyOperation("guclu_2").Ok);
            Assert.IsTrue(sim.BuyOperation("kuantum").Ok, "Uydu henüz yokken hazırlık alınabilir");
            Assert.IsFalse(s.Regions.Any(r => r.Q));
            s.Traits.Add("uydu"); s.Region("weu").Inf = 50; s.Region("weu").Q = true;
            sim.Step();
            Assert.AreEqual(0, s.Region("in").Inf, 1e-12);
            Assert.AreEqual(0.007, s.Region("na").Inf, 1e-12);
        }

        [Test]
        public void CommunicationTiers_ReplaceEarlierLossFactor()
        {
            var sim = Ready(); var s = sim.S; s.Fon = 500; s.Sg = 100;
            Assert.IsTrue(sim.BuyOperation("bilinc_1").Ok);
            s.Region("weu").Trust = 100;
            Assert.IsTrue(sim.Act(Actions.KillSwitch, "weu").Ok);
            Assert.AreEqual(77.5, s.Region("weu").Trust);
            Assert.IsTrue(sim.BuyOperation("bilinc_2").Ok);
            s.Region("na").Trust = 100;
            Assert.IsTrue(sim.Act(Actions.KillSwitch, "na").Ok);
            Assert.AreEqual(85, s.Region("na").Trust);
        }

        [Test]
        public void Analog_CanBeBoughtDuringIllusionAndDoesNotTearItAutomatically()
        {
            var sim = Ready(); var s = sim.S; s.Fon = 300; s.Sg = 10;
            Assert.IsTrue(sim.StartResearch("yorum").Ok); Assert.IsTrue(sim.SetResearchPaused(true).Ok);
            foreach (var r in s.Regions) r.Dep = 50;
            s.Illusion = new IllusionState { Start = s.Day, Tek = 40, Trust = 70 };
            Assert.IsTrue(sim.BuyOperation("bilinc_1").Ok); Assert.IsTrue(sim.BuyOperation("bilinc_2").Ok);
            Assert.IsTrue(sim.BuyOperation("analog").Ok);
            Assert.IsNotNull(s.Illusion); Assert.IsTrue(s.Regions.All(r => r.Dep == 30));
            Assert.AreEqual(20, sim.AuditCost); Assert.AreEqual(85, sim.VetoThreshold);
            Assert.IsFalse(sim.BuyOperation("analog").Ok); Assert.IsTrue(s.Regions.All(r => r.Dep == 30));
            s.Region("weu").Inf = 40;
            Assert.IsTrue(sim.Act(Actions.Audit, "weu").Ok);
            Assert.IsNull(s.Illusion); Assert.IsTrue(s.Research.Paused);
        }

        [Test]
        public void Alignment_RequiresObservedRegionAndConsumesOnlyAvailableReserveExcess()
        {
            var sim = Ready(); var s = sim.S;
            foreach (var r in s.Regions) r.Monitored = false;
            Assert.IsFalse(sim.StartResearch("yorum").Ok); Assert.IsEmpty(sim.Record.commands);
            foreach (var r in s.Regions) r.Monitored = true;
            s.Fon = 14;
            Assert.IsTrue(sim.StartResearch("yorum").Ok);
            sim.Step();
            double need = sim.ResearchNeed;
            Assert.Greater(sim.LastResearchPayment, 0); Assert.Less(sim.LastResearchPayment, need);
            Assert.AreEqual(Math.Min(need, Math.Max(0, 14 + sim.LastIncome.Total - 15)), sim.LastResearchPayment, 1e-12);
            Assert.AreEqual(15, s.Fon, 1e-12);
            Assert.AreEqual(sim.LastResearchPayment, s.Research.TotalPaid, 1e-12);
            Assert.AreEqual(sim.LastIncome.Total - sim.LastResearchPayment, sim.LastNetCashFlow, 1e-12);
            Assert.AreEqual(s.Fon - 14, sim.TransactionLedger.Sum(x => x.Fon), 1e-12);
        }

        [Test]
        public void PausedResearch_PreservesProgressAndSkipsSabotageRngButAiRemembersProject()
        {
            var a = Ready(scale: 1); var b = Ready(scale: 1);
            foreach (var r in a.S.Regions) r.Inf = 50;
            foreach (var r in b.S.Regions) r.Inf = 50;
            a.B.sabotage = 100; b.B.sabotage = 100;
            Assert.IsTrue(a.StartResearch("yorum").Ok); Assert.IsTrue(a.SetResearchPaused(true).Ok);
            a.S.Research.Prog = 3; a.S.Research.TotalPaid = 7;
            a.Step(); b.Step();
            Assert.AreEqual(b.S.Rng.State, a.S.Rng.State, "Duraklatılmış projede sabotaj RNG'si çalışmamalı");
            Assert.AreEqual("yorum", a.S.Research.Cur); Assert.AreEqual(3, a.S.Research.Prog); Assert.AreEqual(7, a.S.Research.TotalPaid);
            Assert.AreEqual(0, a.LastResearchPayment);
            a.S.Cp = 20; a.Step(); Assert.IsTrue(a.S.Has("hizlanma"));
            Assert.IsTrue(a.SetResearchPaused(false).Ok); a.Step(); Assert.Greater(a.LastResearchPayment, 0);
        }

        [Test]
        public void PendingDilemma_PreventsChangingResearchPauseState()
        {
            var sim = Ready(); Assert.IsTrue(sim.StartResearch("yorum").Ok);
            sim.S.PendingEvent = new PendingDilemma { Id = "kostebek", Region = "weu" };
            Assert.IsFalse(sim.SetResearchPaused(true).Ok); Assert.IsFalse(sim.S.Research.Paused);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Recovery_CompletesAfterFourDaysAndIncomeUsesItOnlyOnFifth(bool teams)
        {
            var sim = Ready(teams); var s = sim.S; var r = s.Region("weu"); r.Econ = 45;
            Assert.IsTrue(sim.StartRecovery(r.Id).Ok); Assert.IsFalse(sim.StartRecovery(r.Id).Ok);
            Assert.AreEqual(58, s.Fon); Assert.AreEqual(4, s.RecoveryJobs.Single().FinishDay);
            if (teams)
            {
                string team = s.RecoveryJobs.Single().Team;
                Assert.AreEqual(4, sim.Team(team).Busy);
                Assert.IsFalse(sim.MoveTeam(team, "na").Ok);
            }
            for (int i = 0; i < 3; i++) sim.Step();
            Assert.AreEqual(1, s.RecoveryJobs.Count);
            double before = r.Econ; sim.Step();
            Assert.AreEqual(before + (85 - before) * (0.02 / 1.5) + 15, r.Econ, 1e-12);
            Assert.IsEmpty(s.RecoveryJobs);
            Assert.Greater(sim.FundRate, sim.LastIncome.Total, "Dördüncü günün geliri onarım tamamlanmadan ödenir");
            sim.Step(); Assert.Greater(sim.LastIncome.Total, 2.30);
        }

        [TestCase("offline")]
        [TestCase("left")]
        [TestCase("quarantine")]
        [TestCase("team_lost")]
        public void Recovery_InvalidCompletionDoesNotRefundOrRaiseEconomy(string reason)
        {
            var sim = Ready(teams: true); var s = sim.S; var r = s.Region("weu"); r.Econ = 45;
            Assert.IsTrue(sim.StartRecovery(r.Id).Ok);
            for (int i = 0; i < 3; i++) sim.Step();
            if (reason == "offline") r.Off = 5;
            if (reason == "left") { r.Left = true; r.Trust = 10; }
            if (reason == "quarantine") r.Q = true;
            if (reason == "team_lost") s.Teams.RemoveAll(t => t.Id == s.RecoveryJobs.Single().Team);
            double before = r.Econ;
            sim.Step();
            Assert.Less(r.Econ - before, 1); Assert.IsEmpty(s.RecoveryJobs);
            Assert.AreEqual(-2, sim.TransactionLedger.Where(x => x.Kind == "recovery").Sum(x => x.Fon));
        }

        [Test]
        public void Recovery_RespectsGapAndDoesNotReduceGiftEconomyAboveBaseline()
        {
            var sim = Ready(); var r = sim.S.Region("weu"); r.Econ = 81;
            Assert.IsFalse(sim.StartRecovery(r.Id).Ok);
            r.Econ = 80; Assert.IsTrue(sim.StartRecovery(r.Id).Ok);
            r.Econ = 95;
            for (int i = 0; i < 4; i++) sim.Step();
            Assert.AreEqual(85 + 10 * Math.Pow(1 - 0.02 / 1.5, 4), r.Econ, 1e-12);
        }

        [Test]
        public void LostTeamReplacement_UsesVacantIdentityAfterFailedRecovery()
        {
            var sim = Ready(teams: true); sim.S.Fon = 100;
            sim.S.Region("weu").Econ = 45;
            Assert.IsTrue(sim.StartRecovery("weu").Ok);
            string lost = sim.S.RecoveryJobs.Single().Team;
            sim.S.Teams.RemoveAll(t => t.Id == lost);
            // Kayıp ekibin görevi sonuç vermez; ikame yeni bir kurulum süresi ister.
            for (int i = 0; i < 4; i++) sim.Step();
            Assert.IsEmpty(sim.S.RecoveryJobs);
            Assert.Less(sim.S.Region("weu").Econ, 60);
            Assert.IsTrue(sim.HireTeam().Ok);
            Assert.AreEqual(3, sim.S.Teams.Select(t => t.Id).Distinct().Count());
            Assert.IsNotNull(sim.Team(lost)); Assert.AreEqual(4, sim.Team(lost).Down);
        }

        [TestCase(0)]
        [TestCase(18)]
        public void Diplomacy_ReturnsFormerMemberImmediatelyAndEnforcesRegionalCooldown(double trust)
        {
            var sim = Ready(); var s = sim.S; s.Sg = 10;
            var r = s.Region("weu"); r.Trust = trust; r.Left = true;
            Assert.IsTrue(sim.DiplomaticTalk(r.Id).Ok);
            Assert.AreEqual(35, r.Trust); Assert.IsFalse(r.Left); Assert.AreEqual(30, r.DiplomacyUntil);
            Assert.AreEqual(52, s.Fon); Assert.AreEqual(8, s.Sg);
            Assert.IsFalse(sim.DiplomaticTalk(r.Id).Ok);
            Assert.AreEqual(52, s.Fon); Assert.AreEqual(8, s.Sg);
            s.Region("na").Trust = 50; Assert.IsTrue(sim.DiplomaticTalk("na").Ok);
            s.Day = 30; Assert.IsTrue(sim.DiplomaticTalk(r.Id).Ok);
        }

        [Test]
        public void Diplomacy_DoesNotReduceHighTrustOrOpenOfflineIncome()
        {
            var sim = Ready(); var r = sim.S.Region("weu"); r.Trust = 90;
            Assert.IsFalse(sim.DiplomaticTalk(r.Id).Ok); Assert.AreEqual(90, r.Trust);
            r.Trust = 0; r.Left = true; r.Off = 10;
            double income = sim.FundRate; Assert.IsTrue(sim.DiplomaticTalk(r.Id).Ok);
            Assert.IsFalse(r.Left); Assert.AreEqual(income, sim.FundRate, 1e-12);
        }

        [Test]
        public void BlockedOrEndedWorld_DoesNotPayAnotherDay()
        {
            var sim = Ready(); sim.Step();
            double fon = sim.S.Fon; int day = sim.S.Day, ledger = sim.TransactionLedger.Count;
            sim.S.PendingGift = new PendingGift { Region = "weu", Gift = GameData.Gifts[0] };
            sim.Step(); Assert.AreEqual(fon, sim.S.Fon); Assert.AreEqual(day, sim.S.Day); Assert.AreEqual(ledger, sim.TransactionLedger.Count);
            sim.S.PendingGift = null; sim.S.Ending = "acik";
            sim.Step(); Assert.AreEqual(fon, sim.S.Fon); Assert.AreEqual(day, sim.S.Day);
        }

        [TestCase(7u)]
        [TestCase(99u)]
        public void NewEconomyCommands_RestoreExactlyIncludingLedgerAndRunningRecovery(uint seed)
        {
            var sim = PlayRecorded(seed, 80);
            sim.Record.day = sim.S.Day;
            var saved = UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record));
            var restored = Replay.Restore(saved);
            Assert.AreEqual(Snapshot(sim), Snapshot(restored));
            Assert.IsTrue(sim.Record.commands.Any(c => c.kind == "operation"));
            Assert.IsTrue(sim.Record.commands.Any(c => c.kind == "recovery"));
            Assert.IsTrue(sim.Record.commands.Any(c => c.kind == "diplomacy"));
            Assert.IsTrue(sim.Record.commands.Any(c => c.kind == "research_pause"));
            Assert.AreEqual(sim.B.startFon + sim.TransactionLedger.Sum(x => x.Fon), sim.S.Fon, 1e-9);
            Assert.AreEqual(sim.B.startSg + sim.TransactionLedger.Sum(x => x.Sg), sim.S.Sg, 1e-9);
            var pending = PlayRecorded(seed, 12); pending.Record.day = pending.S.Day;
            Assert.AreEqual(1, pending.S.RecoveryJobs.Count);
            Assert.AreEqual(Snapshot(pending), Snapshot(Replay.Restore(pending.Record)));
        }

        [Test]
        public void NewEconomyCommands_AreLanguageIndependent()
        {
            Loc.SetLanguage(Loc.Turkish);
            string tr = Snapshot(PlayRecorded(7, 80));
            try
            {
                Loc.SetLanguage(Loc.English, Loc.ParseTable(File.ReadAllText(LocalizationTool.TableFile))[Loc.English]);
                Assert.AreEqual(tr, Snapshot(PlayRecorded(7, 80)));
            }
            finally { Loc.SetLanguage(Loc.Turkish); }
        }

        [TestCase(7u, 12, "FA3F1EC8")]
        [TestCase(7u, 80, "3FF4F88B")]
        [TestCase(99u, 12, "A4629295")]
        [TestCase(99u, 80, "28A5313E")]
        public void EconomyCommands_MatchBrowserPrototype(uint seed, int days, string expected)
        {
            // Beklenen hash'ler Prototype/parite.js tarafından bağımsız JS çekirdeğinden üretilir.
            var s = PlayRecorded(seed, days).S;
            string F(double n) => n.ToString("F6", Inv);
            int Bit(bool value) => value ? 1 : 0;
            string snapshot = string.Join("|", new object[] { s.Day, s.Ending, F(s.Fon), F(s.Sg), F(s.Tek), F(s.Cp), s.Rng.State,
                s.EconomyVersion, s.BudgetLevel, string.Join(",", s.Operations), s.Research.Cur, F(s.Research.Prog), Bit(s.Research.Paused),
                F(s.Research.TotalPaid), F(s.LastIncome.Total), F(s.LastResearchPayment), s.Core, s.CoreKnown, string.Join(",", s.Traits) })
                + "|" + string.Join(";", s.Regions.Select(r => string.Join("/", new object[] { r.Id, F(r.Inf), F(r.Econ), F(r.Trust), F(r.Dep), Bit(r.Left), Bit(r.Q), r.Off, r.DiplomacyUntil })))
                + "|" + string.Join(";", s.RecoveryJobs.Select(j => $"{j.Region}/{j.Team}/{j.FinishDay}"))
                + "|" + string.Join(";", s.TransactionLedger.Select(t => $"{t.Day}/{t.Kind}/{t.Id}/{F(t.Fon)}/{F(t.Sg)}"));
            uint hash = 2166136261;
            foreach (char c in snapshot) hash = unchecked((hash ^ c) * 16777619);
            Assert.AreEqual(expected, hash.ToString("X8", Inv));
        }

        static Simulation PlayRecorded(uint seed, int until)
        {
            var sim = Simulation.NewGame(null, "manipulator", seed, new Balance { startFon = 2000, startSg = 200 }, new ExpOptions { Teams = false, TimeScale = 1.5 });
            var s = sim.S;
            foreach (var r in s.Regions) sim.Act(Actions.Monitor, r.Id);
            foreach (var node in Nodes) sim.BuyOperation(node);
            sim.Act(Actions.Quarantine, "weu");
            int pausedAt = -1; bool pauseDone = false;
            while (s.Ending == null && s.Day < until)
            {
                if (s.PendingGift != null) sim.ResolveGift(Simulation.GiftReject);
                if (s.Day == 10)
                {
                    sim.Act(Actions.Quarantine, "weu"); sim.StartRecovery("weu");
                    sim.DiplomaticTalk(s.Regions.First(r => r.Trust < 70).Id);
                }
                if (s.Aware)
                {
                    if (s.BudgetLevel < 2) sim.BuyBudgetUpgrade();
                    if (s.Research.Cur == null && !s.Done("yorum")) sim.StartResearch("yorum");
                }
                if (!pauseDone && s.Research.Cur != null && s.Research.Prog >= 2)
                {
                    sim.SetResearchPaused(true); pausedAt = s.Day; pauseDone = true;
                }
                if (s.Research.Paused && s.Day >= pausedAt + 8) sim.SetResearchPaused(false);
                sim.Step(); s.Events.Clear();
            }
            return sim;
        }

        static string Snapshot(Simulation sim)
        {
            string F(double n) => n.ToString("R", Inv);
            var s = sim.S;
            return string.Join("|", new object[] { s.Day, s.Ending, F(s.Fon), F(s.Sg), F(s.Tek), F(s.Cp), s.Rng.State,
                s.EconomyVersion, s.BudgetLevel, string.Join(",", s.Operations), s.Research.Cur, F(s.Research.Prog), s.Research.Paused,
                F(s.Research.TotalPaid), F(s.LastIncome.Total), F(s.LastResearchPayment), s.Core, s.CoreKnown, string.Join(",", s.Traits) })
                + "|" + string.Join(";", s.Regions.Select(r => string.Join("/", new object[] { r.Id, F(r.Inf), F(r.Econ), F(r.Trust), F(r.Dep), r.Left, r.Q, r.Off, r.DiplomacyUntil })))
                + "|" + string.Join(";", s.RecoveryJobs.Select(j => $"{j.Region}/{j.Team}/{j.FinishDay}"))
                + "|" + string.Join(";", s.TransactionLedger.Select(t => $"{t.Day}/{t.Kind}/{t.Id}/{F(t.Fon)}/{F(t.Sg)}"));
        }
    }
}
