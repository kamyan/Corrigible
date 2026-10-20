using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind;
using Corrigible.Ayna.Mind.Bots;
using Corrigible.Ayna.EditorTools;
using Corrigible.Core;
using WorldState = Corrigible.Ayna.Core.WorldState;
using NUnit.Framework;

namespace Corrigible.Ayna.Tests
{
    public sealed class AynaDeterminismTests
    {
        [TestCase(1u)] [TestCase(42u)] [TestCase(154312u)]
        public void SameSeedAndBots_ProduceExactStateEveryCycle(uint seed)
        {
            var a = AynaRulesTests.New(seed: seed); var b = AynaRulesTests.New(seed: seed);
            var ap = new RandomBot(seed); var bp = new RandomBot(seed); var aa = new AggroBot(); var ba = new AggroBot();
            while (!a.Outcome.Finished)
            {
                a.ResolveCycle(ap.MakePlan(a.View(Side.Player)), aa.MakePlan(a.View(Side.Ayna)));
                b.ResolveCycle(bp.MakePlan(b.View(Side.Player)), ba.MakePlan(b.View(Side.Ayna)));
                Assert.That(a.StateHash, Is.EqualTo(b.StateHash)); AynaRulesTests.Invariants(a);
            }
        }
        [Test] public void SaveJsonReplay_ReproducesFullStateAndDetachesInputs()
        {
            var sim = AynaRulesTests.New(); var pbot = new AggroBot(); var abot = new StealthBot();
            while (!sim.Outcome.Finished)
            {
                var p = pbot.MakePlan(sim.View(Side.Player)); var a = abot.MakePlan(sim.View(Side.Ayna)); sim.ResolveCycle(p, a); p.Orders.Clear(); a.Orders.Clear();
            }
            var rec = AynaJson.ReadRecord(AynaJson.Write(sim.Record)); var restored = AynaReplay.Restore(rec);
            Assert.That(restored.StateHash, Is.EqualTo(sim.StateHash)); rec.Cycles.Clear(); Assert.That(sim.Record.Cycles.Count, Is.GreaterThan(0));
        }
        [Test] public void MidGameReplay_ContinuesExactly()
        {
            var a = AynaRulesTests.New(); var bot = new GreedyBot();
            for (int i = 0; i < 4; i++) a.ResolveCycle(bot.MakePlan(a.View(Side.Player)), bot.MakePlan(a.View(Side.Ayna)));
            var b = AynaReplay.Restore(a.Record); Assert.That(a.StateHash, Is.EqualTo(b.StateHash));
            while (!a.Outcome.Finished)
            {
                var p = bot.MakePlan(a.View(Side.Player)); var q = bot.MakePlan(a.View(Side.Ayna)); a.ResolveCycle(p, q); b.ResolveCycle(p, q); Assert.That(a.StateHash, Is.EqualTo(b.StateHash));
            }
        }
        [Test] public void PreviewAndView_DoNotAdvanceRandomnessOrMutateWorld()
        {
            var s = AynaRulesTests.New(); string hash = s.StateHash;
            s.PreviewPlan(Side.Player, new Plan(new[] { Order.Expand("na", 2) })); var v = s.View(Side.Player);
            v.BalanceSnapshot().StartCompute = 1000; v.Outcome.Winner = Side.Player;
            Assert.That(s.StateHash, Is.EqualTo(hash)); Assert.That(v.Compute, Is.EqualTo(6));
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<int>)v.Tiers).Add(4));
        }
        [Test] public void HiddenWorldDifferences_DoNotChangeViewPredictionOrPlan()
        {
            var b = new AynaBalance { PlannerSamples = 2, PlannerCandidates = 4 };
            var a = AynaRulesTests.New(b: b); var c = AynaRulesTests.New(b: b);
            AynaRulesTests.Give(c, "oc", 1, 2); c.State.Sides[1].Compute = 700; c.State.Sides[1].Tiers[3] = 3;
            Assert.That(c.StateHash, Is.Not.EqualTo(a.StateHash));
            var av = a.View(Side.Player); var cv = c.View(Side.Player);
            Assert.That(AynaJson.Write(av), Is.EqualTo(AynaJson.Write(cv)));
            Assert.That(AynaJson.Write(PriorModel.Predict(av)), Is.EqualTo(AynaJson.Write(PriorModel.Predict(cv))));
            Assert.That(AynaJson.Write(MacroPlanner.Choose(av, new MindSession(28))), Is.EqualTo(AynaJson.Write(MacroPlanner.Choose(cv, new MindSession(28)))));
        }
        [Test] public void PlayerPendingPlan_IsNotAnInputToMind()
        {
            var v = AynaRulesTests.New(b: new AynaBalance { PlannerSamples = 1, PlannerCandidates = 4 }).View(Side.Ayna);
            var pending = new Plan(new[] { Order.Expand("na", 2) });
            var before = MacroPlanner.Choose(v, new MindSession(17)); pending.Orders.Add(new Order { Kind = OrderKind.Hide, Region = "na" });
            Assert.That(AynaJson.Write(MacroPlanner.Choose(v, new MindSession(17))), Is.EqualTo(AynaJson.Write(before)));
        }
        [Test] public void HiddenEnemyProduction_DoesNotLeakThroughOwnUnitIds()
        {
            var a = AynaRulesTests.New(); var b = AynaRulesTests.New();
            foreach (var s in new[] { a, b })
            {
                AynaRulesTests.Rich(s); AynaRulesTests.Rich(s, 1); AynaRulesTests.Give(s, "sea", 0, 1); AynaRulesTests.Give(s, "oc", 1, 1);
            }
            var player = AynaRulesTests.P(new Order { Kind = OrderKind.Produce, ArmyId = 1, Unit = UnitKind.Scout });
            a.ResolveCycle(player, AynaRulesTests.P(new Order { Kind = OrderKind.Produce, ArmyId = 2, Unit = UnitKind.Scout }));
            b.ResolveCycle(player, new Plan());
            a.ResolveCycle(player, new Plan()); b.ResolveCycle(player, new Plan());
            Assert.That(a.State.Sides[1].Armies[0].Units.Count, Is.Not.EqualTo(b.State.Sides[1].Armies[0].Units.Count));
            Assert.That(AynaJson.Write(a.View(Side.Player)), Is.EqualTo(AynaJson.Write(b.View(Side.Player))));
        }
        [Test] public void OtherSidesObservationHistory_DoesNotLeakThroughSequenceIds()
        {
            var a = AynaRulesTests.New(); var b = AynaRulesTests.New();
            a.State.Knowledge[1].NextObservationId = 31;
            a.State.Knowledge[1].Observations.Add(new Observation(30, 1, "oc", "activity"));
            var signal = AynaRulesTests.P(new Order { Kind = OrderKind.Feint, Region = "na" });
            a.ResolveCycle(new Plan(), signal); b.ResolveCycle(new Plan(), signal);
            Assert.That(AynaJson.Write(a.View(Side.Player)), Is.EqualTo(AynaJson.Write(b.View(Side.Player))));
        }
        [TestCase("weu", "in", 2)] [TestCase("na", "cn", 2)] [TestCase("weu", "oc", 3)] [TestCase("sa", "ru", 4)]
        public void CachedDistances_PreservePhysicalShortestPaths(string from, string to, int expected)
        {
            Assert.That(AynaWorldData.Distance(from, to), Is.EqualTo(expected)); Assert.That(AynaWorldData.Distance(to, from), Is.EqualTo(expected));
            Assert.That(AynaWorldData.Distance(from, from), Is.Zero); Assert.That(AynaWorldData.Distance(null, to), Is.EqualTo(int.MaxValue));
        }
        [Test] public void TrueWorld_IsInternalAndOnlyTestsAreFriends()
        {
            Assert.That(typeof(WorldState).IsNotPublic); Assert.That(typeof(WorldState).GetFields(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
            var friends = typeof(WorldState).Assembly.GetCustomAttributes<InternalsVisibleToAttribute>().Select(x => x.AssemblyName).ToArray();
            Assert.That(friends, Is.EqualTo(new[] { "Corrigible.Ayna.Tests" }));
            foreach (var method in typeof(MacroPlanner).GetMethods(BindingFlags.Public | BindingFlags.Static))
                Assert.That(method.GetParameters().Any(x => x.ParameterType == typeof(WorldState) || x.ParameterType == typeof(AynaSimulation)), Is.False);
        }
        [Test] public void RevealAll_IsForbiddenBeforeGameOver()
        {
            var s = AynaRulesTests.New(); Assert.Throws<InvalidOperationException>(() => s.View(Side.Player, true));
            for (int i = 0; i < 40; i++) AynaRulesTests.Step(s);
            Assert.That(s.View(Side.Player, true).Regions.All(x => x.Knowledge == Knowledge.Exact));
        }
        [Test] public void Fog_DoesNotUseTruthAsEstimate_OrExposeHiddenRoster()
        {
            var s = AynaRulesTests.New(); var v = s.View(Side.Player);
            Assert.That(v.Region("cn").Knowledge, Is.EqualTo(Knowledge.Unknown)); Assert.That(v.Region("cn").EnemyServers, Is.Null);
            Assert.That(v.Region("cn").EnemyMin, Is.Zero); Assert.That(v.Region("cn").EnemyMax, Is.EqualTo(8)); Assert.That(v.EnemyCoreRegion, Is.Null);
            AynaRulesTests.Give(s, "cn", 0, 1); v = s.View(Side.Player);
            Assert.That(v.Region("cn").EnemyServers, Is.EqualTo(3)); Assert.That(v.Region("cn").EnemyArmies[0].RosterKnown, Is.False);
            Assert.That(v.EnemyCoreRegion, Is.Null);
        }
        [Test] public void BeliefsRespectCapacityAndView_AndNeverMutateSource()
        {
            var s = AynaRulesTests.New(); var v = s.View(Side.Player); string hash = s.StateHash;
            for (uint n = 1; n < 10; n++)
            {
                var g = BeliefSampler.Sample(v, new Rng(n)); var sampled = AynaSimulation.CreateBelief(v, g); AynaRulesTests.Invariants(sampled);
                foreach (var r in v.Regions) Assert.That(g.EnemyServers[r.Id], Is.InRange(r.EnemyMin, r.EnemyMax));
                sampled.ResolveCycle(new Plan(), new Plan());
            }
            Assert.That(s.StateHash, Is.EqualTo(hash));
            var bad = BeliefSampler.Sample(v, new Rng(1)); bad.EnemyServers["cn"] = 9; Assert.Throws<ArgumentException>(() => AynaSimulation.CreateBelief(v, bad));
        }
        [Test] public void WorldRngIsIndependentFromMindAndPreviewWork()
        {
            var s = AynaRulesTests.New(); var session = new MindSession(42); string hash = s.StateHash;
            for (int i = 0; i < 10; i++) { var g = BeliefSampler.Sample(s.View(Side.Ayna), session.Rng); new BeliefSimulator(s.View(Side.Ayna), g).Evaluate(Side.Ayna, new Plan(), new Plan()); }
            Assert.That(s.StateHash, Is.EqualTo(hash));
        }
        [Test] public void LanguageDoesNotChangeGame()
        {
            string previous = Loc.Lang;
            var previousTable = (System.Collections.Generic.Dictionary<string, string>)typeof(Loc).GetField("table", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            try
            {
                var s = AynaRulesTests.New(); var t = AynaRulesTests.New(); var bot = new AggroBot();
                while (!s.Outcome.Finished)
                {
                    Loc.SetLanguage("tr"); s.ResolveCycle(bot.MakePlan(s.View(Side.Player)), bot.MakePlan(s.View(Side.Ayna)));
                    Loc.SetLanguage("en"); t.ResolveCycle(bot.MakePlan(t.View(Side.Player)), bot.MakePlan(t.View(Side.Ayna)));
                    Assert.That(s.StateHash, Is.EqualTo(t.StateHash));
                }
            }
            finally { Loc.SetLanguage(previous, previousTable); }
        }
        [Test] public void RecordAndProfile_RejectFutureVersionsAndBadSequence()
        {
            var s = AynaRulesTests.New(); var rec = s.Record; rec.FormatVersion = 9; Assert.Throws<ArgumentException>(() => AynaJson.ReadRecord(AynaJson.Write(rec)));
            var p = new AynaProfileData { Version = 99 }; Assert.Throws<ArgumentException>(() => AynaJson.ReadProfile(AynaJson.Write(p)));
            rec = s.Record; rec.Cycles.Add(new CycleRecord { Cycle = 2, Player = new Plan(), Ayna = new Plan() }); Assert.Throws<ArgumentException>(() => AynaReplay.Restore(rec));
        }
        [Test] public void PriorDistribution_Normalizes_ConditionalTargetIsDefined()
        {
            var d = PriorModel.Predict(AynaRulesTests.New().View(Side.Ayna));
            Assert.That(d.Sum(x => x.Probability), Is.EqualTo(1).Within(1e-12)); Assert.That(d.All(x => x.Probability > 0));
            Assert.That(d.Where(x => x.Kind == OrderKind.Expand).Sum(x => PriorModel.ConditionalTargetProbability(d, x.Kind, x.Region)), Is.EqualTo(1).Within(1e-12));
        }
        [TestCase("Habit")] [TestCase("Random")] [TestCase("Greedy")] [TestCase("Aggro")] [TestCase("Stealth")] [TestCase("Surprise")] [TestCase("Mind")]
        public void EveryBotCanFinishAndKeepsInvariants(string botId)
        {
            var s = AynaRulesTests.New(b: new AynaBalance { PlannerSamples = 1, PlannerCandidates = 3 });
            var p = AynaSelfPlay.Bot(botId, 27); var a = AynaSelfPlay.Bot("Aggro", 42);
            while (!s.Outcome.Finished) { s.ResolveCycle(p.MakePlan(s.View(Side.Player)), a.MakePlan(s.View(Side.Ayna))); AynaRulesTests.Invariants(s); }
            Assert.That(s.Outcome.Cycle, Is.LessThanOrEqualTo(40));
        }
        [Test] public void SameMindSeedAndView_ReproducesConsecutiveDecisions()
        {
            var s = AynaRulesTests.New(b: new AynaBalance { PlannerSamples = 1, PlannerCandidates = 3 }); var a = new MindBot(42); var b = new MindBot(42); var opponent = new AggroBot();
            for (int i = 0; i < 6; i++)
            {
                var v = s.View(Side.Player); var p = a.MakePlan(v); Assert.That(AynaJson.Write(b.MakePlan(v)), Is.EqualTo(AynaJson.Write(p))); s.ResolveCycle(p, opponent.MakePlan(s.View(Side.Ayna)));
            }
        }
        [Test, Explicit("500 oyun ve denge raporu; normal EditMode turunda çalıştırılmaz.")]
        public void SelfPlay500_WritesMeasuredAcceptanceReport() => Assert.That(AynaSelfPlay.RunDefault(500, "Docs/AYNA/denge/F1/explicit"), Is.Not.Empty);

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator SelfPlayJob_YieldsBetweenGamesAndReturnsMeasuredReport()
        {
            string dir = "Temp/AynaAsyncTest";
            try
            {
                AynaSelfPlay.Start(new SelfPlayOptions { Games = 2, PlayerBot = "Aggro", AynaBot = "Stealth", OutputDirectory = dir });
                Assert.That(AynaSelfPlay.Status(), Does.Contain("\"Running\":true"));
                int frames = 0;
                while (AynaSelfPlay.Status().Contains("\"Running\":true") && frames++ < 120) yield return null;
                Assert.That(AynaSelfPlay.Status(), Does.Contain("\"Completed\":2"));
                Assert.That(AynaSelfPlay.Status(), Does.Contain("\"Running\":false"));
                Assert.That(AynaSelfPlay.Status(), Does.Contain("\"Error\":null"));
                Assert.That(System.IO.Directory.GetFiles(dir, "*.json").Length, Is.EqualTo(1));
            }
            finally { AynaSelfPlay.Cancel(); if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); }
        }
    }
}
