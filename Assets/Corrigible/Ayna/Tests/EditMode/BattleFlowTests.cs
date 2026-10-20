using System;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind;
using NUnit.Framework;

namespace Corrigible.Ayna.Tests
{
    public sealed class BattleFlowTests
    {
        internal static AynaSimulation Encounter()
        {
            var s = AynaRulesTests.New();
            s.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = "eeu" } }), new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 2, Region = "ru" } }));
            s.BeginCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = "ru" } }), new Plan()); return s;
        }
        static void ReplayEquals(AynaSimulation s)
        {
            var restored = AynaReplay.Restore(AynaJson.ReadRecord(AynaJson.Write(s.Record)));
            Assert.That(restored.StateHash, Is.EqualTo(s.StateHash));
        }
        [Test] public void PendingBattlePausesIncome_RejectsNewCycle_AndReplays()
        {
            var s = Encounter(); Assert.That(s.IsResolving); Assert.That(s.PendingBattle(Side.Player).Region, Is.EqualTo("ru"));
            var b = new AynaBalance();
            Assert.That(s.View(Side.Player).Cycle, Is.EqualTo(2)); Assert.That(s.View(Side.Player).Compute, Is.EqualTo(b.StartCompute - 2 * b.MarchCost + b.StartServers * b.IncomePerServer + b.CoreIncome).Within(1e-9));
            Assert.Throws<InvalidOperationException>(() => s.ResolveCycle(new Plan(), new Plan())); ReplayEquals(s);
            Assert.That(s.ResolvePendingAuto()); Assert.That(s.IsResolving, Is.False); Assert.That(s.View(Side.Player).Cycle, Is.EqualTo(3)); ReplayEquals(s);
        }
        [Test] public void MidBoardReplay_PreservesUndoAndHiddenTraps_AndCommitOnlyOnce()
        {
            var s = Encounter(); Assert.That(s.StartBoard()); Assert.That(s.PlaceBoardTraps(Array.Empty<int>())); ReplayEquals(s);
            var plan = BoardPlanner.Choose(s.Board(Side.Ayna)); Assert.That(s.SetBoardIntents(plan)); ReplayEquals(s);
            string hash = s.StateHash; var command = BoardSimulation.CreateBelief(s.Board(Side.Player)).LegalCommands(Side.Player).First(c => c.Action == BoardAction.Move);
            Assert.That(s.PreviewBoardCommand(Side.Player, command), Is.Not.Null); Assert.That(s.StateHash, Is.EqualTo(hash));
            Assert.That(s.ApplyBoardCommand(Side.Player, command)); ReplayEquals(s);
            var restored = AynaReplay.Restore(s.Record); Assert.That(restored.UndoBoard(Side.Player)); Assert.That(restored.StateHash, Is.EqualTo(hash));
            Assert.That(s.UndoBoard(Side.Player)); Assert.That(s.StateHash, Is.EqualTo(hash)); ReplayEquals(s);
            Assert.That(s.RetreatBoard(Side.Player)); ReplayEquals(s); Assert.That(s.CompleteBoard());
            hash = s.StateHash; Assert.That(s.CompleteBoard(), Is.False); Assert.That(s.StateHash, Is.EqualTo(hash)); ReplayEquals(s); AynaRulesTests.Invariants(s);
        }
        [Test] public void InvalidRosterAndCommandsAreAtomicAndUnrecorded()
        {
            var s = Encounter(); string hash = s.StateHash, record = AynaJson.Write(s.Record);
            Assert.That(s.StartBoard(new[] { 1, 1 }), Is.False); Assert.That(s.StateHash, Is.EqualTo(hash)); Assert.That(AynaJson.Write(s.Record), Is.EqualTo(record));
            s.StartBoard(); hash = s.StateHash; record = AynaJson.Write(s.Record);
            Assert.That(s.ApplyBoardCommand(Side.Ayna, new BoardCommand()), Is.False); Assert.That(s.StateHash, Is.EqualTo(hash)); Assert.That(AynaJson.Write(s.Record), Is.EqualTo(record));
        }
        [Test] public void TacticalNodeClaimsUseSameSnapshot_ThreeTwoBecomesFourOne()
        {
            var s = Encounter(); AynaRulesTests.Give(s, "ru", 0, 3); AynaRulesTests.Give(s, "ru", 1, 2); s.StartBoard();
            var board = s.State.Pending.Board; board.State.Nodes[0].Owner = board.State.Nodes[1].Owner = Side.Player; board.State.Nodes[2].Owner = Side.Ayna;
            board.State.Phase = BoardPhase.Finished; s.CompleteBoard(); Assert.That(s.State.Region("ru").Owned, Is.EqualTo(new[] { 4, 1 })); AynaRulesTests.Invariants(s);
        }
        [Test] public void CoreDestructionErasesWithoutEscape_ReservesAreUntouched()
        {
            var s = Encounter(); s.State.Sides[1].CoreRegion = "ru";
            s.State.Sides[0].Armies[0].Units.Add(new UnitState { Id = 101, Kind = UnitKind.Scout, Hp = .5 });
            Assert.That(s.StartBoard(new[] { 1, 3, 5 })); var b = s.State.Pending.Board; b.State.CoreHp[1] = 0; b.State.Phase = BoardPhase.Finished;
            Assert.That(s.CompleteBoard()); Assert.That(s.Outcome.Reason, Is.EqualTo(EndReason.Erasure)); Assert.That(s.Outcome.Winner, Is.EqualTo(Side.Player));
            Assert.That(s.State.Sides[0].Armies[0].Units.Single(u => u.Id == 101).Hp, Is.EqualTo(.5));
        }
        [Test] public void BoardRngAndPreviewDoNotConsumeWorldRng()
        {
            var a = Encounter(); var b = Encounter(); uint rng = a.State.WorldRng.State;
            a.StartBoard(); a.PlaceBoardTraps(Array.Empty<int>()); a.SetBoardIntents(new BoardPlan());
            for (int i = 0; i < 3; i++) BoardPlanner.Choose(a.Board(Side.Player));
            Assert.That(a.State.WorldRng.State, Is.EqualTo(rng)); Assert.That(b.State.WorldRng.State, Is.EqualTo(rng));
        }
        [Test] public void UnprotectedCoreCanBeCommanded_MidBattleReplays()
        {
            var s = AynaRulesTests.New();
            s.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = "eeu" } }), new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 2, Region = "sea" } }));
            s.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = "ru" } }), new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 2, Region = "oc" } }));
            s.BeginCycle(new Plan(new[] { Order.Attack("cn", 1) }), new Plan());
            Assert.That(s.IsResolving); Assert.That(s.PendingBattle(Side.Player).AynaRoster, Is.Empty);
            Assert.That(s.StartBoard()); Assert.That(s.Board(Side.Player).Phase, Is.EqualTo(BoardPhase.Intents));
            Assert.That(s.Board(Side.Player).CoreCells[1], Is.GreaterThanOrEqualTo(0)); ReplayEquals(s);
            s.RetreatBoard(Side.Player); s.CompleteBoard(); Assert.That(s.Outcome.Reason, Is.Not.EqualTo(EndReason.Erasure)); ReplayEquals(s);
        }
        [Test] public void MultipleBattlesResumeInRegionOrder_IncomeOnlyAfterLast()
        {
            var s = AynaRulesTests.New(b: new AynaBalance { StartCompute = 100 });
            s.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.NewArmy, Region = "weu" }, Order.Expand("me", 1) }), new Plan(new[] { new Order { Kind = OrderKind.NewArmy, Region = "cn" }, Order.Expand("ru", 1) }));
            s.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.Produce, ArmyId = 3, Unit = UnitKind.Emp }, new Order { Kind = OrderKind.March, ArmyId = 1, Region = "eeu" } }),
                new Plan(new[] { new Order { Kind = OrderKind.Produce, ArmyId = 4, Unit = UnitKind.Emp }, new Order { Kind = OrderKind.March, ArmyId = 2, Region = "ru" } }));
            s.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 3, Region = "me" } }), new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 4, Region = "in" } }));
            s.BeginCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = "ru" }, new Order { Kind = OrderKind.March, ArmyId = 3, Region = "in" } }), new Plan());
            Assert.That(s.PendingBattle(Side.Player).Region, Is.EqualTo("in")); double cash = s.View(Side.Player).Compute;
            s.StartBoard(); s.RetreatBoard(Side.Player); s.CompleteBoard();
            Assert.That(s.IsResolving); Assert.That(s.PendingBattle(Side.Player).Region, Is.EqualTo("ru")); Assert.That(s.View(Side.Player).Compute, Is.EqualTo(cash)); ReplayEquals(s);
            s.ResolvePendingAuto(); Assert.That(s.IsResolving, Is.False); Assert.That(s.View(Side.Player).Compute, Is.GreaterThan(cash)); ReplayEquals(s);
        }
        [Test] public void LegacyRecordUsesSavedBalance_AndMissingV2InputsAreRejected()
        {
            var b = new AynaBalance { UnitHp = new double[] { 2, 2, 4, 3, 2, 1, 2, 2, 1, 2 }, UnitMove = new[] { 4, 3, 2, 3, 3, 4, 3, 3, 3, 3 }, SniperDamage = 2 };
            var s = AynaRulesTests.New(b: b);
            s.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = "eeu" } }), new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 2, Region = "ru" } }));
            s.ResolveCycle(new Plan(new[] { new Order { Kind = OrderKind.March, ArmyId = 1, Region = "ru" } }), new Plan());
            var rec = s.Record; rec.FormatVersion = rec.RulesVersion = 1; rec.Inputs = null;
            Assert.That(AynaReplay.Restore(rec).StateHash, Is.EqualTo(s.StateHash));
            rec = s.Record; rec.Inputs.Clear(); Assert.Throws<ArgumentException>(() => AynaJson.ReadRecord(AynaJson.Write(rec)));
        }
        [Test] public void TacticalRaidHeuristicUsesVisibleBattle_AndDoesNotChangeWorld()
        {
            var s = AynaRulesTests.New(); s.State.Sides[0].Armies[0].Region = "cn"; s.State.Sides[1].Armies[0].Region = "oc";
            s.ResolveCycle(new Plan(new[] { Order.Attack("cn", 1) }), new Plan());
            var v = s.View(Side.Player); var a = v.BalanceSnapshot(); var b = a.Copy(); b.TacticalPlanning = true;
            string hash = s.StateHash;
            Assert.That(MacroPlanner.Evaluate(v, b) - MacroPlanner.Evaluate(v, a), Is.EqualTo(b.ScoreCoreRaid).Within(1e-9));
            Assert.That(s.StateHash, Is.EqualTo(hash));
        }
    }
}
