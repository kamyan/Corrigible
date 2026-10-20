using System;
using System.IO;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Game;
using Corrigible.Ayna.Mind;
using Corrigible.Ayna.Mind.Bots;
using NUnit.Framework;

namespace Corrigible.Ayna.Tests
{
    public sealed class LearningTests
    {
        static StartSetup Setup(LearningLevel level = LearningLevel.Full) => new StartSetup { Seed = 1781, Player = new StartChoice { Region = "weu", Architecture = Architecture.Swarm }, Ayna = new StartChoice { Region = "cn", Architecture = Architecture.Fortress }, Settings = new AynaSettings { Learning = level } };
        static Prediction[] Forecast(double p = .05) => new[] { new Prediction { Kind = OrderKind.Expand, Region = "weu", Probability = p }, new Prediction { Kind = OrderKind.Expand, Region = "na", Probability = 1 - p } };
        static AynaProfileData Prior() { var p = new AynaProfileData { Games = 3, Generation = 3 }; p.Architectures[0] = 20; p.StartRegions[2] = 10; p.Units[(int)UnitKind.Scout] = 30; p.Targets[AynaProfileData.TargetIndex(OrderKind.Expand, "weu")] = 20; return p; }
        [Test] public void ProfileMigration_RoundTrip_RejectsUnknownAndNonfinite()
        {
            var old = new AynaProfileData { Version = 1, Generation = 2, Games = 2 }; old.OrderCounts[0] = 8;
            var p = AynaJson.ReadProfile(AynaJson.Write(old)); Assert.That(p.Version, Is.EqualTo(3)); Assert.That(p.OrderCounts[0], Is.EqualTo(8)); Assert.That(p.CycleTargets.Length, Is.EqualTo(6760));
            p.Targets[0] = double.NaN; Assert.Throws<ArgumentException>(() => p.Copy()); p = new AynaProfileData { Version = 4 }; Assert.Throws<ArgumentException>(() => p.Copy());
        }
        [Test] public void ForecastIsNormalized_Positive_AndLearnedTargetsAffectDistribution()
        {
            var sim = AynaSimulation.Create(Setup()); var v = sim.View(Side.Ayna); var a = new MindSession(7); var b = new MindSession(7, Prior());
            var f = a.Predict(v); Assert.That(f.Sum(p => p.Probability), Is.EqualTo(1).Within(1e-12)); Assert.That(f.All(p => p.Probability > 0));
            Assert.That(MindSession.TargetProbability(b.Predict(v), OrderKind.Expand, "weu"), Is.GreaterThan(MindSession.TargetProbability(f, OrderKind.Expand, "weu")));
        }
        [Test] public void OffIgnoresEvenMalformedProfile_AndMatchesPlansAcrossGames()
        {
            var bad = new AynaProfileData { Version = 999, Targets = null };
            var a = new LearningMatch(Setup(LearningLevel.Off), "a", bad); var b = new LearningMatch(Setup(LearningLevel.Off), "b", Prior());
            for (int i = 0; i < 4; i++) { var pa = a.MakeAynaPlan(); var pb = b.MakeAynaPlan(); Assert.That(AynaJson.Write(pa), Is.EqualTo(AynaJson.Write(pb))); a.ResolveAuto(new Plan()); b.ResolveAuto(new Plan()); Assert.That(a.Simulation.StateHash, Is.EqualTo(b.Simulation.StateHash)); }
        }
        [Test] public void OnlyVerifiedVisibleOrdersLearn_ObservationIsNotCountedTwice()
        {
            var sim = AynaSimulation.Create(Setup()); var model = new MindSession(2); sim.ResolveCycle(new Plan(new[] { Order.Expand("weu", 1) }), new Plan()); model.Observe(sim.View(Side.Ayna)); Assert.That(model.Checkpoint().Game.Targets.Sum(), Is.EqualTo(0));
            sim.State.Region("weu").Owned[1] = 1; sim.State.Region("weu").Free--;
            sim.ResolveCycle(new Plan(new[] { Order.Expand("weu", 1) }), new Plan()); model.Observe(sim.View(Side.Ayna)); var snapshot = AynaJson.Write(model.Checkpoint()); model.Observe(sim.View(Side.Ayna)); Assert.That(AynaJson.Write(model.Checkpoint()), Is.EqualTo(snapshot)); Assert.That(model.Checkpoint().Game.Targets.Sum(), Is.GreaterThan(0));
        }
        [Test] public void HiddenOrdersAndFeintsDoNotTrainVerifiedTargets()
        {
            var sim = AynaSimulation.Create(Setup(), new AynaBalance { StartCompute = 100 }); sim.State.Region("weu").Owned[1] = 1; sim.State.Region("weu").Free--;
            sim.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.Hide, Region = "weu" }, Order.Expand("weu", 1), new Order { Kind = OrderKind.Feint, Region = "na" } }), new Plan());
            var model = new MindSession(2); model.Observe(sim.View(Side.Ayna)); Assert.That(model.Checkpoint().Game.Targets.Sum(), Is.EqualTo(0)); Assert.That(model.Checkpoint().Game.Feints[0], Is.EqualTo(1));
        }
        [Test] public void ForecastLocksBeforePlans_AndReplaysAwardOnce()
        {
            var sim = AynaSimulation.Create(Setup()); Assert.That(sim.LockForecast(Side.Ayna, Forecast())); Assert.That(sim.LockForecast(Side.Ayna, Forecast()), Is.False);
            sim.ResolveCycle(new Plan(new[] { Order.Expand("weu", 1) }), new Plan()); Assert.That(sim.View(Side.Player).LastSurprise); Assert.That(sim.View(Side.Player).Compute, Is.EqualTo(new AynaBalance().StartCompute - new AynaBalance().ExpandCost + .5 + 4 * new AynaBalance().IncomePerServer + new AynaBalance().CoreIncome).Within(1e-9));
            Assert.That(AynaReplay.Restore(sim.Record).StateHash, Is.EqualTo(sim.StateHash)); Assert.That(sim.View(Side.Player).Readability, Is.EqualTo(5).Within(1e-9));
        }
        [Test] public void RejectedOrdersDoNotEarnSurprise_AndInvalidForecastIsAtomic()
        {
            var sim = AynaSimulation.Create(Setup()); string hash = sim.StateHash;
            Assert.That(sim.LockForecast(Side.Ayna, new[] { new Prediction { Probability = double.NaN } }), Is.False); Assert.That(sim.StateHash, Is.EqualTo(hash));
            sim.LockForecast(Side.Ayna, Forecast()); sim.ResolveCycle(new Plan(new[] { Order.Expand("invalid", 1) }), new Plan()); Assert.That(sim.View(Side.Player).LastSurprise, Is.False);
        }
        [Test] public void ConditionalThresholdIsStrict_AndOneBonusForMultipleOrders()
        {
            var sim = AynaSimulation.Create(Setup()); sim.LockForecast(Side.Ayna, Forecast(new AynaBalance().SurpriseThreshold)); sim.ResolveCycle(new Plan(new[] { Order.Expand("weu", 1) }), new Plan()); Assert.That(sim.View(Side.Player).LastSurprise, Is.False);
            sim = AynaSimulation.Create(Setup()); sim.LockForecast(Side.Ayna, new[] { new Prediction { Kind = OrderKind.Expand, Region = "weu", Probability = .025 }, new Prediction { Kind = OrderKind.Expand, Region = "eeu", Probability = .025 }, new Prediction { Kind = OrderKind.Expand, Region = "na", Probability = .95 } }); sim.ResolveCycle(new Plan(new[] { Order.Expand("weu", 1), Order.Expand("eeu", 1) }), new Plan()); Assert.That(sim.View(Side.Player).Compute, Is.EqualTo(new AynaBalance().StartCompute - 2 * new AynaBalance().ExpandCost + .5 + 5 * new AynaBalance().IncomePerServer + new AynaBalance().CoreIncome).Within(1e-9));
        }
        [Test] public void LanguageNoiseOnlyChangesDisplay_NotRewardsOrWorldRng()
        {
            var a = AynaSimulation.Create(Setup()); var b = AynaSimulation.Create(Setup()); b.State.Sides[0].Tiers[4] = 3;
            a.LockForecast(Side.Ayna, Forecast()); b.LockForecast(Side.Ayna, Forecast()); var rng = b.State.WorldRng.State;
            for (int i = 0; i < 5; i++) { var f = b.View(Side.Player).IncomingForecast; Assert.That(f.Sum(p => p.Probability), Is.EqualTo(1).Within(1e-9)); }
            Assert.That(b.State.WorldRng.State, Is.EqualTo(rng)); a.ResolveCycle(new Plan(new[] { Order.Expand("weu", 1) }), new Plan()); b.ResolveCycle(new Plan(new[] { Order.Expand("weu", 1) }), new Plan()); Assert.That(a.View(Side.Player).Compute, Is.EqualTo(b.View(Side.Player).Compute)); Assert.That(a.View(Side.Player).Readability, Is.EqualTo(b.View(Side.Player).Readability));
        }
        [Test] public void SurpriseFirstStrikeAppliesToFirstBattleOnly_AndMidBoardReplayWorks()
        {
            var sim = AynaSimulation.Create(Setup());
            sim.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = "eeu" } }), new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 2, Region = "ru" } }));
            sim.LockForecast(Side.Ayna, new[] { new Prediction { Kind = OrderKind.March, Region = "ru", Probability = .05 }, new Prediction { Kind = OrderKind.March, Region = "eeu", Probability = .95 } });
            sim.BeginCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = "ru" } }), new Plan());
            Assert.That(sim.StartBoard()); Assert.That(sim.Board(Side.Player).Phase, Is.EqualTo(BoardPhase.FirstStrike)); Assert.That(sim.State.FirstStrike.All(x => !x));
            Assert.That(AynaReplay.Restore(sim.Record).StateHash, Is.EqualTo(sim.StateHash));
        }
        [Test] public void OpeningPolicyUsesPrior_AndCountersRespectLevels()
        {
            var full = new MindSession(5, Prior()); Assert.That(OpeningPolicy.Choose(full).Architecture, Is.EqualTo(Architecture.Fortress)); Assert.That(OpeningPolicy.Counters(full, new AynaBalance()).Count, Is.EqualTo(2));
            var light = new MindSession(5, Prior(), LearningLevel.Light); Assert.That(OpeningPolicy.Counters(light, new AynaBalance()).Count, Is.EqualTo(1)); Assert.That(OpeningPolicy.Counters(new MindSession(5, Prior(), LearningLevel.Off), new AynaBalance()), Is.Empty);
        }
        [Test] public void MatchSaveRestoresMindRng_ObservationsAndNextPlan()
        {
            var game = new LearningMatch(Setup(), "resume", Prior()); game.ResolveAuto(new Plan()); game.PrepareCycle(); var restored = LearningMatch.Restore(game.Save());
            Assert.That(restored.Simulation.StateHash, Is.EqualTo(game.Simulation.StateHash)); Assert.That(AynaJson.Write(restored.AynaMind.Checkpoint()), Is.EqualTo(AynaJson.Write(game.AynaMind.Checkpoint()))); Assert.That(AynaJson.Write(restored.MakeAynaPlan()), Is.EqualTo(AynaJson.Write(game.MakeAynaPlan())));
        }
        [Test] public void CompleteMergeIsWeighted_Idempotent_AndRequiresFinishedGame()
        {
            var sim = AynaSimulation.Create(Setup(), new AynaBalance { MaxCycles = 1 }); var model = new MindSession(2, Prior()); Assert.Throws<InvalidOperationException>(() => model.Complete(sim.View(Side.Ayna), Setup().Player, "x"));
            sim.ResolveCycle(new Plan(), new Plan()); var p = model.Complete(sim.View(Side.Ayna, true), Setup().Player, "x"); Assert.That(p.Generation, Is.EqualTo(4)); Assert.That(p.Architectures[0], Is.EqualTo(.65 * 20 + .35).Within(1e-12)); Assert.That(AynaJson.Write(model.Complete(sim.View(Side.Ayna, true), Setup().Player, "x")), Is.EqualTo(AynaJson.Write(p)));
            var light = new MindSession(2, Prior(), LearningLevel.Light); Assert.That(light.Complete(sim.View(Side.Ayna, true), Setup().Player, "light").Architectures[0], Is.EqualTo(.825 * 20 + .175).Within(1e-12));
        }
        [Test] public void StoreAtomicCommit_RejectsStaleSave_ResetInvalidatesOldMemory_OffDoesNoIO()
        {
            string dir = Path.Combine("Temp", "AynaProfileTests", Guid.NewGuid().ToString("N")); var store = new AynaProfileStore(dir);
            try
            {
                Assert.That(store.Load(LearningLevel.Off).Games, Is.EqualTo(0)); Assert.That(store.Commit(null, 0, "initial", LearningLevel.Off), Is.False); Assert.That(Directory.Exists(dir), Is.False);
                var p = new AynaProfileData { Generation = 1, Games = 1 }; Assert.That(store.Commit(p, 0, "initial", LearningLevel.Full)); var bytes = File.ReadAllBytes(store.ProfilePath); Assert.That(store.Commit(p, 0, "initial", LearningLevel.Full), Is.False); Assert.That(File.ReadAllBytes(store.ProfilePath), Is.EqualTo(bytes));
                var empty = store.EraseMemory(); Assert.That(empty.Generation, Is.EqualTo(0)); Assert.That(store.Commit(p, 0, "initial", LearningLevel.Full), Is.False); Assert.That(store.Load(LearningLevel.Full).MemoryId, Is.EqualTo(empty.MemoryId));
                File.WriteAllText(store.ProfilePath, "{\"Version\":999}"); Assert.Throws<ArgumentException>(() => store.Load(LearningLevel.Full)); Assert.That(File.ReadAllText(store.ProfilePath), Is.EqualTo("{\"Version\":999}"));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }
        [Test] public void UndoTraceIsNotLearned_OnlyCommittedBoardEntersView()
        {
            var sim = BattleFlowTests.Encounter(); sim.StartBoard(); sim.PlaceBoardTraps(Array.Empty<int>()); sim.SetBoardIntents(new BoardPlan());
            var command = BoardSimulation.CreateBelief(sim.Board(Side.Player)).LegalCommands(Side.Player).First(c => c.Action == BoardAction.Move); sim.ApplyBoardCommand(Side.Player, command); sim.UndoBoard(Side.Player); Assert.That(sim.View(Side.Ayna).Boards, Is.Empty);
            sim.RetreatBoard(Side.Player); sim.CompleteBoard(); Assert.That(sim.View(Side.Ayna).Boards.Single().Cells, Is.Empty); Assert.That(AynaReplay.Restore(sim.Record).StateHash, Is.EqualTo(sim.StateHash));
        }
        [Test] public void TwoModelsAwardSymmetrically_WithoutChangingWorldRng()
        {
            var sim = AynaSimulation.Create(Setup()); var rng = sim.State.WorldRng.State;
            sim.LockForecast(Side.Ayna, Forecast()); sim.LockForecast(Side.Player, new[] { new Prediction { Kind = OrderKind.Expand, Region = "cn", Probability = .05 }, new Prediction { Kind = OrderKind.Expand, Region = "na", Probability = .95 } });
            sim.ResolveCycle(new Plan(new[] { Order.Expand("weu", 1) }), new Plan(new[] { Order.Expand("cn", 1) })); Assert.That(sim.View(Side.Player).LastSurprise); Assert.That(sim.View(Side.Ayna).LastSurprise); Assert.That(sim.State.WorldRng.State, Is.EqualTo(rng)); Assert.That(AynaReplay.Restore(sim.Record).StateHash, Is.EqualTo(sim.StateHash));
        }
        [Test] public void ReadabilityUsesLastFiveCycles_AndStoredForecastIsDefensive()
        {
            var sim = AynaSimulation.Create(Setup());
            for (int i = 1; i <= 6; i++)
            {
                string target = i % 2 == 1 ? "eeu" : "weu"; var frame = new[] { new Prediction { Kind = OrderKind.March, Region = target, Probability = i / 10.0 }, new Prediction { Kind = OrderKind.March, Region = "na", Probability = 1 - i / 10.0 } };
                sim.LockForecast(Side.Ayna, frame); frame[0].Probability = 0;
                var display = sim.View(Side.Player).IncomingForecast; display[0].Probability = 0;
                sim.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = target } }), new Plan());
            }
            Assert.That(sim.View(Side.Player).Readability, Is.EqualTo(40).Within(1e-9)); Assert.That(AynaReplay.Restore(sim.Record).StateHash, Is.EqualTo(sim.StateHash));
        }
        [Test] public void MidBoardLearningSaveRestoresUndoAndMind()
        {
            var match = new LearningMatch(Setup(), "mid-board", chooseOpening: false); var sim = match.Simulation;
            sim.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = "eeu" } }), new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 2, Region = "ru" } }));
            match.PrepareCycle(); sim.BeginCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = "ru" } }), new Plan()); sim.StartBoard(); sim.PlaceBoardTraps(Array.Empty<int>());
            if (sim.Board(Side.Player).Phase == BoardPhase.Intents) sim.SetBoardIntents(new BoardPlan());
            var command = BoardSimulation.CreateBelief(sim.Board(Side.Player)).LegalCommands(Side.Player).First(c => c.Action == BoardAction.Move); sim.ApplyBoardCommand(Side.Player, command);
            var restored = LearningMatch.Restore(match.Save()); Assert.That(restored.Simulation.StateHash, Is.EqualTo(sim.StateHash)); Assert.That(restored.Simulation.UndoBoard(Side.Player)); Assert.That(sim.UndoBoard(Side.Player)); Assert.That(restored.Simulation.StateHash, Is.EqualTo(sim.StateHash));
        }
        [Test] public void ProfileSnapshotsDoNotShareMutableArrays()
        {
            var prior = Prior(); var session = new MindSession(5, prior); prior.Targets[0] = 999;
            var a = session.ProfileAtStart; a.Targets[0] = 555; Assert.That(session.ProfileAtStart.Targets[0], Is.Not.EqualTo(555));
            var checkpoint = session.Checkpoint(); var restored = MindSession.Restore(checkpoint); checkpoint.Prior.Targets[0] = 777; Assert.That(restored.ProfileAtStart.Targets[0], Is.Not.EqualTo(777));
        }
        [Test] public void InvalidOpeningCountersAreRejected()
        {
            var setup = Setup(LearningLevel.Off); setup.AynaCounters.Add(new OpeningCounter { Wall = true }); Assert.Throws<ArgumentException>(() => AynaSimulation.Create(setup));
            setup = Setup(); setup.AynaCounters.AddRange(new[] { new OpeningCounter { Wall = true }, new OpeningCounter { Wall = true } }); Assert.Throws<ArgumentException>(() => AynaSimulation.Create(setup));
        }
        [Test] public void ForecastExcludesOnlyProvablyImpossibleTargets()
        {
            var sim = AynaSimulation.Create(Setup()); var view = sim.View(Side.Ayna); var model = new MindSession(1);
            var forecast = model.Predict(view); Assert.That(forecast.Where(p => p.Kind == OrderKind.Attack).All(p => view.Region(p.Region).OwnServers > 0));
            Assert.That(forecast.Any(p => p.Kind == OrderKind.Expand && view.Region(p.Region).Knowledge == Knowledge.Unknown));
            Assert.That(forecast.Sum(p => p.Probability), Is.EqualTo(1).Within(1e-12));
        }
        [TestCase(OrderKind.Attack, true, 1)] [TestCase(OrderKind.March, true, 2)]
        [TestCase(OrderKind.BuildWall, true, 0)] [TestCase(OrderKind.Attack, false, -1)]
        public void ReactionUsesVerifiedOpponentReply_AfterOwnAttack(OrderKind reply, bool verified, int expected)
        {
            var sim = AynaSimulation.Create(Setup()); var model = new MindSession(1); var known = sim.State.Knowledge[1];
            known.ReceiptCycle = 1; known.Receipts.Add(new OrderReceipt(0, new Order { Kind = OrderKind.Attack, Region = "weu" }, RejectReason.None, 1, 1));
            model.Observe(sim.View(Side.Ayna));
            known.ReceiptCycle = 2; known.Receipts.Clear();
            known.Observations.Add(new Observation(known.NextObservationId++, 2, "weu", "order", reply, verified));
            model.Observe(sim.View(Side.Ayna)); var reactions = model.Checkpoint().Game.Reactions;
            Assert.That(reactions.Sum(), Is.EqualTo(expected < 0 ? 0 : 1));
            if (expected >= 0) Assert.That(reactions[expected], Is.EqualTo(1));
            model.Observe(sim.View(Side.Ayna)); Assert.That(model.Checkpoint().Game.Reactions, Is.EqualTo(reactions));
        }
        [Test] public void FullRegionStillAllowsPaidExpansion_AndMustRemainInForecast()
        {
            var sim = AynaSimulation.Create(Setup(), new AynaBalance { StartCompute = 100 });
            int capacity = AynaWorldData.Region("weu").Capacity;
            AynaRulesTests.Give(sim, "weu", 0, capacity - 1); AynaRulesTests.Give(sim, "weu", 1, 1);
            var forecast = new MindSession(1).Predict(sim.View(Side.Ayna));
            Assert.That(MindSession.TargetProbability(forecast, OrderKind.Expand, "weu"), Is.GreaterThan(0));
            sim.ResolveCycle(new Plan(new[] { Order.Expand("weu", 1) }), new Plan());
            var receipt = sim.View(Side.Player).Receipts.Single();
            Assert.That(receipt.Rejection, Is.EqualTo(RejectReason.None)); Assert.That(receipt.Cost, Is.GreaterThan(0));
            Assert.That(sim.View(Side.Player).Region("weu").OwnServers, Is.EqualTo(capacity - 1));
        }
        [Test] public void HiddenArmyRegionAlsoHidesProductionOrderFromLearning()
        {
            var sim = AynaSimulation.Create(Setup(), new AynaBalance { StartCompute = 100 });
            AynaRulesTests.Give(sim, "weu", 1, 1); AynaRulesTests.Give(sim, "sea", 0, 1);
            sim.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.Hide, Region = "weu" }, new Order { Kind = OrderKind.Produce, ArmyId = 1, Unit = UnitKind.Scout } }), new Plan());
            Assert.That(sim.View(Side.Player).Receipts.Last().Rejection, Is.EqualTo(RejectReason.None));
            var model = new MindSession(8); model.Observe(sim.View(Side.Ayna)); Assert.That(model.Checkpoint().Game.OrderCounts[(int)OrderKind.Produce], Is.EqualTo(0));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void FirstBoardTargetUsesActionTimeFacts(int targetKind)
        {
            int start = targetKind == 2 ? 7 : 27, target = targetKind == 2 ? 6 : 26;
            var board = BoardSimulation.Create(new BoardSetup { Template = 0, Transform = 0, Fighters = new System.Collections.Generic.List<BoardFighter> { new BoardFighter { Id = 1, Side = Side.Player, Kind = UnitKind.Scout, Hp = 2, Cell = start }, new BoardFighter { Id = 2, Side = Side.Ayna, Kind = UnitKind.Scout, Hp = 2, Cell = targetKind == 1 ? target : 8 } } });
            board.SetIntents(new BoardPlan()); Assert.That(board.Apply(Side.Player, new BoardCommand { UnitId = 1, Action = targetKind == 0 ? BoardAction.Move : BoardAction.Attack, To = target }));
            if (targetKind == 2) Assert.That(board.View(Side.Player).Tiles[target], Is.EqualTo(BoardTile.Floor));
            board.Retreat(Side.Player); var observation = new BoardLearningObservation("target", board.State, Side.Ayna); Assert.That(observation.FirstTarget, Is.EqualTo(targetKind));
        }
        [Test] public void MixedRosterIntentPlansRemainValid_WithLearnedHeat()
        {
            for (uint seed = 1; seed <= 40; seed++)
            {
                var setup = new BoardSetup { Seed = seed, Template = (int)(seed % 4), Transform = (int)(seed % 8) };
                for (int side = 0; side < 2; side++) for (int i = 0; i < 4; i++) { var kind = (UnitKind)((seed + i * 3 + side) % 10); setup.Fighters.Add(new BoardFighter { Id = i * 2 + side + 1, Side = (Side)side, Kind = kind, Hp = new AynaBalance().UnitHp[(int)kind] }); }
                var board = BoardSimulation.Create(setup); board.PlaceTraps(Array.Empty<int>()); var rng = new Corrigible.Core.Rng(seed);
                while (!board.Finished)
                {
                    var plan = BoardPlanner.Choose(board.View(Side.Ayna), responseHeat: Enumerable.Repeat(1.0 / 64, 64).ToArray());
                    Assert.That(board.SetIntents(plan), Is.True, "seed=" + seed + " turn=" + board.View(Side.Ayna).Turn);
                    foreach (var c in BoardPlanner.Choose(board.View(Side.Player), BotStyle.Random, rng).Commands) { if (board.Finished) break; board.Apply(Side.Player, c); }
                    if (!board.Finished) board.EndResponse(Side.Player);
                }
            }
        }
        [Test] public void BehaviorProjectionIsViewOnly_AndDoesNotConsumeWorldOrMindRng()
        {
            var a = AynaSimulation.Create(Setup()); var b = AynaSimulation.Create(Setup()); b.State.Sides[0].Compute = 999;
            var left = new MindSession(5); var right = new MindSession(5); string hash = a.StateHash; uint rng = left.Rng.State;
            left.Observe(a.View(Side.Ayna)); right.Observe(b.View(Side.Ayna));
            Assert.That(AynaJson.Write(left.Predict(a.View(Side.Ayna))), Is.EqualTo(AynaJson.Write(right.Predict(b.View(Side.Ayna))))); Assert.That(a.StateHash, Is.EqualTo(hash)); Assert.That(left.Rng.State, Is.EqualTo(rng));
            string snapshot = AynaJson.Write(left.Checkpoint()); for (int i = 0; i < 3; i++) left.Predict(a.View(Side.Ayna)); Assert.That(AynaJson.Write(left.Checkpoint()), Is.EqualTo(snapshot));
        }
        [Test] public void BehaviorCheckpointRestoresFutureProjection_AndLegacyUsesCountsOnly()
        {
            var sim = AynaSimulation.Create(Setup()); var session = new MindSession(9); session.Observe(sim.View(Side.Ayna)); var copy = MindSession.Restore(session.Checkpoint());
            sim.ResolveCycle(new Plan(new[] { Order.Expand("weu", 1) }), new Plan()); session.Observe(sim.View(Side.Ayna)); copy.Observe(sim.View(Side.Ayna)); Assert.That(AynaJson.Write(session.Checkpoint()), Is.EqualTo(AynaJson.Write(copy.Checkpoint())));
            var old = session.Checkpoint(); old.Version = 1; old.Behavior = null; var legacy = MindSession.Restore(old); legacy.Observe(sim.View(Side.Ayna)); Assert.That(legacy.Checkpoint().Version, Is.EqualTo(1)); Assert.That(legacy.Checkpoint().Behavior, Is.Null);
        }
        [Test] public void BehaviorMixtureNormalizes_AndOffDoesNotConstructHypotheses()
        {
            var sim = AynaSimulation.Create(Setup()); var session = new MindSession(1, Prior()); session.Observe(sim.View(Side.Ayna));
            Assert.That(session.Checkpoint().Behavior.Particles.Count, Is.EqualTo(6)); Assert.That(session.Predict(sim.View(Side.Ayna)).Sum(p => p.Probability), Is.EqualTo(1).Within(1e-9));
            var off = new MindSession(1, null, LearningLevel.Off); off.Observe(sim.View(Side.Ayna)); Assert.That(off.Checkpoint().Behavior, Is.Null);
        }
        [Test] public void CompletedBattleExposesOnlyObservedSurvivorHp_AndKeepsEmptyArmyBelief()
        {
            var sim = BattleFlowTests.Encounter(); var model = new MindSession(42); model.Observe(sim.View(Side.Ayna)); sim.StartBoard();
            var board = sim.State.Pending.Board; foreach (var unit in board.State.Units.Where(u => u.Side == Side.Player)) unit.Hp = 0; board.State.Phase = BoardPhase.Finished;
            sim.CompleteBoard(); var view = sim.View(Side.Ayna); var observation = view.Boards.Single(); Assert.That(observation.UnitHp.All(h => h == 0)); Assert.That(observation.Region, Is.EqualTo("ru"));
            model.Observe(view); Assert.That(model.Checkpoint().Behavior.Particles.All(p => p.Guess.Armies.Count > 0));
        }
        [Test] public void PublicStateContradictionReweightsHypotheses_AndCheckpointRestoresEvidence()
        {
            var sim = AynaSimulation.Create(Setup()); var model = new MindSession(3); model.Observe(sim.View(Side.Ayna)); var data = model.Checkpoint();
            foreach (var p in data.Behavior.Particles) p.Guess.EnemyServers["cn"] = p.Policy == 0 ? 3 : 0;
            model = MindSession.Restore(data); sim.ResolveCycle(new Plan(), new Plan()); model.Observe(sim.View(Side.Ayna));
            var after = model.Checkpoint(); Assert.That(after.Game.PolicyTrials.Sum(), Is.GreaterThan(0)); Assert.That(after.Game.PolicyMatches[0], Is.LessThan(after.Game.PolicyMatches[1]));
            var copy = MindSession.Restore(after); Assert.That(AynaJson.Write(copy.Checkpoint()), Is.EqualTo(AynaJson.Write(after)));
        }
    }
}
