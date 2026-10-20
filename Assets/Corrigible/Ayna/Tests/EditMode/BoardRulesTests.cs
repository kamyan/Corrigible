using System;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind;
using Corrigible.Ayna.Mind.Bots;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Ayna.Tests
{
    public sealed class BoardRulesTests
    {
        static BoardSetup Setup(UnitKind p = UnitKind.Scout, UnitKind a = UnitKind.Scout) => new BoardSetup { Seed = 42, Template = 0, Transform = 0,
            Fighters = new System.Collections.Generic.List<BoardFighter> { new BoardFighter { Id = 1, Side = Side.Player, Kind = p, Hp = new AynaBalance().UnitHp[(int)p], Cell = 27 },
                new BoardFighter { Id = 2, Side = Side.Ayna, Kind = a, Hp = new AynaBalance().UnitHp[(int)a], Cell = 28 } } };
        internal static BoardSimulation Flat(UnitKind p = UnitKind.Scout, UnitKind a = UnitKind.Scout)
        {
            var s = BoardSimulation.Create(Setup(p, a)); s.State.Tiles = new BoardTile[64];
            Assert.That(s.SetIntents(new BoardPlan())); return s;
        }
        static BoardCommand C(BoardAction action, int cell, int id = 1, int target = 0) => new BoardCommand { Action = action, To = cell, UnitId = id, TargetUnitId = target };
        [Test] public void SerializedCommandIdsStayContiguous_ForFastValidation()
        {
            foreach (var type in new[] { typeof(OrderKind), typeof(DataFamily), typeof(UnitKind), typeof(BoardAction) })
            {
                int expected = 0; foreach (var value in Enum.GetValues(type)) Assert.That(Convert.ToInt32(value), Is.EqualTo(expected++), type.Name);
            }
        }
        [Test] public void BasicAttack_ClassBonus_AndGuardianArmorAreAppliedOnce()
        {
            var s = Flat(UnitKind.Guardian, UnitKind.Emp); Assert.That(s.Apply(Side.Player, C(BoardAction.Attack, 28)));
            Assert.That(s.State.Units[1].Hp, Is.EqualTo(0));
            s = Flat(UnitKind.Intrusion, UnitKind.Guardian); s.Apply(Side.Player, C(BoardAction.Attack, 28)); Assert.That(s.State.Units[1].Hp, Is.EqualTo(3));
        }
        [Test] public void ScoutFliesAcrossRacks_ButCannotLandOnThem()
        {
            var s = Flat(); s.State.Units[1].Cell = 63; s.State.Tiles[28] = BoardTile.Rack;
            Assert.That(s.Apply(Side.Player, C(BoardAction.Move, 28)), Is.False);
            Assert.That(s.Apply(Side.Player, C(BoardAction.Move, 29))); Assert.That(s.State.Units[0].Cell, Is.EqualTo(29));
        }
        [Test] public void SniperRange_Damage_FirstObstacle()
        {
            var s = Flat(UnitKind.Sniper); s.State.Units[1].Cell = 30; s.State.Tiles[29] = BoardTile.Rack;
            Assert.That(s.Apply(Side.Player, C(BoardAction.Ability, 30)), Is.False); s.State.Tiles[29] = BoardTile.Floor;
            Assert.That(s.Apply(Side.Player, C(BoardAction.Ability, 28)), Is.False);
            Assert.That(s.Apply(Side.Player, C(BoardAction.Ability, 30))); Assert.That(s.State.Units[1].Hp, Is.EqualTo(1));
        }
        [Test] public void SpiderCrossesRack_PushesOneCell_ButCannotCrossCivilian()
        {
            var s = Flat(UnitKind.Spider, UnitKind.Guardian); s.State.Tiles[26] = BoardTile.Rack; s.State.Tiles[25] = BoardTile.Civilian;
            Assert.That(s.Apply(Side.Player, C(BoardAction.Move, 25)), Is.False);
            Assert.That(s.Apply(Side.Player, C(BoardAction.Ability, 28))); Assert.That(s.State.Units[1].Cell, Is.EqualTo(29));
            s.State.Tiles[25] = BoardTile.Floor; Assert.That(s.Apply(Side.Player, C(BoardAction.Move, 25)));
        }
        [Test] public void EmpStunsAirForTwoTurns_GroundForOne_NoDamage()
        {
            var s = Flat(UnitKind.Emp); s.State.Units[1].Cell = 30;
            s.State.Units.Add(new BoardUnit { Id = 4, Side = Side.Ayna, Kind = UnitKind.Guardian, Hp = 4, Cell = 22 });
            s.State.Units.Add(new BoardUnit { Id = 6, Side = Side.Ayna, Kind = UnitKind.Decoy, Hp = 1, Cell = 31 });
            Assert.That(s.Apply(Side.Player, C(BoardAction.Ability, 30)));
            Assert.That(s.State.Units[1].StunnedUntil, Is.EqualTo(2)); Assert.That(s.State.Units[2].StunnedUntil, Is.EqualTo(1));
            Assert.That(s.State.Units[3].StunnedUntil, Is.EqualTo(0)); Assert.That(s.State.Units[1].Hp, Is.EqualTo(2));
        }
        [Test] public void IntentEmpStunsResponseOnItsNextTurn()
        {
            var s = Flat(UnitKind.Guardian, UnitKind.Emp); s.State.Phase = BoardPhase.Intents; s.State.Units[0].Cell = 25;
            Assert.That(s.SetIntents(new BoardPlan { Commands = { C(BoardAction.Ability, 25, 2) } })); Assert.That(s.EndResponse(Side.Player));
            Assert.That(s.State.Turn, Is.EqualTo(2)); Assert.That(s.State.Units[0].StunnedUntil, Is.EqualTo(2));
        }
        [Test] public void OverloadDamagesArea_CivilianNoiseAndSelfDestruction()
        {
            var s = Flat(UnitKind.Overload, UnitKind.Guardian); s.State.Tiles[19] = BoardTile.Civilian;
            Assert.That(s.Apply(Side.Player, C(BoardAction.Ability, 27))); Assert.That(s.State.Units[0].Hp, Is.EqualTo(0));
            Assert.That(s.State.Units[1].Hp, Is.EqualTo(3)); Assert.That(s.State.Tiles[19], Is.EqualTo(BoardTile.Floor)); Assert.That(s.State.Noise[0], Is.EqualTo(2));
        }
        [Test] public void IntrusionControlsRobotForCurrentTurn_CancelsItsIntent()
        {
            var s = Flat(UnitKind.Intrusion, UnitKind.Guardian); s.State.Phase = BoardPhase.Intents;
            Assert.That(s.SetIntents(new BoardPlan { Commands = { C(BoardAction.Attack, 27, 2) } }));
            Assert.That(s.Apply(Side.Player, C(BoardAction.Ability, 28)));
            Assert.That(s.View(Side.Player).Units.Single(u => u.Id == 2).Controller, Is.EqualTo(Side.Player));
            Assert.That(s.Apply(Side.Player, C(BoardAction.Move, 29, 2))); s.EndResponse(Side.Player);
            Assert.That(s.State.Units[0].Hp, Is.EqualTo(3)); Assert.That(s.View(Side.Player).Units.Single(u => u.Id == 2).Controller, Is.EqualTo(Side.Ayna));
        }
        [Test] public void RelayCapturesImmediately_AndGivesNearbyMovementBonus()
        {
            var s = Flat(UnitKind.Relay); s.State.Units[1].Cell = 63; s.State.Nodes[0].Cell = 29; s.State.Nodes[0].Owner = Side.None;
            s.State.Units.Add(new BoardUnit { Id = 3, Side = Side.Player, ControlledBy = Side.Player, Hp = 4, Kind = UnitKind.Guardian, Cell = 19 });
            Assert.That(s.LegalCommands(Side.Player).Any(c => c.UnitId == 3 && c.Action == BoardAction.Move && c.To == 23));
            Assert.That(s.Apply(Side.Player, C(BoardAction.Move, 29))); Assert.That(s.State.Nodes[0].Owner, Is.EqualTo(Side.Player));
        }
        [Test] public void DecoyRedirectsIntentTarget_WhenItIsInRange()
        {
            var s = Flat(UnitKind.Scout, UnitKind.Guardian); s.State.Phase = BoardPhase.Intents;
            s.State.Units.Add(new BoardUnit { Id = 3, Side = Side.Player, ControlledBy = Side.Player, Kind = UnitKind.Decoy, Hp = 1, Cell = 20 });
            Assert.That(s.SetIntents(new BoardPlan { Commands = { C(BoardAction.Attack, 27, 2) } }));
            Assert.That(s.View(Side.Player).Intents.Single().To, Is.EqualTo(20)); s.EndResponse(Side.Player);
            Assert.That(s.State.Units[0].Hp, Is.EqualTo(2)); Assert.That(s.State.Units[2].Hp, Is.LessThanOrEqualTo(0));
        }
        [Test] public void PhantomRedirectsToAnotherLegalTarget_IllegalRedirectIsAtomic()
        {
            var s = Flat(UnitKind.PhantomOrder); s.State.Phase = BoardPhase.Intents;
            s.State.Units.Add(new BoardUnit { Id = 3, Side = Side.Player, ControlledBy = Side.Player, Kind = UnitKind.Guardian, Hp = 4, Cell = 20 });
            Assert.That(s.SetIntents(new BoardPlan { Commands = { C(BoardAction.Attack, 27, 2) } }));
            string hash = s.StateHash; Assert.That(s.Apply(Side.Player, C(BoardAction.Ability, 63, target: 2)), Is.False); Assert.That(s.StateHash, Is.EqualTo(hash));
            Assert.That(s.Apply(Side.Player, C(BoardAction.Ability, 20, target: 2))); Assert.That(s.View(Side.Player).Intents.Single().To, Is.EqualTo(20));
        }
        [Test] public void TrapIsHidden_RevealedByScoutAndVisionThree_StopsMovementAndUndoRestores()
        {
            var s = Flat(UnitKind.Guardian); s.State.Units[1].Cell = 63; s.State.Traps.Add(new BoardTrap { Cell = 28, Owner = Side.Ayna });
            Assert.That(s.View(Side.Player).Traps, Is.Empty); string hash = s.StateHash;
            var preview = s.Preview(Side.Player, C(BoardAction.Move, 29)); Assert.That(preview.Units.Single(u => u.Id == 1).Hp, Is.EqualTo(4)); Assert.That(s.StateHash, Is.EqualTo(hash));
            Assert.That(s.Apply(Side.Player, C(BoardAction.Move, 29))); Assert.That(s.State.Units[0].Cell, Is.EqualTo(28)); Assert.That(s.State.Units[0].Acted);
            Assert.That(s.Undo(Side.Player)); Assert.That(s.StateHash, Is.EqualTo(hash));
            s.State.Units[0].Kind = UnitKind.Scout; Assert.That(s.View(Side.Player).Traps.Count, Is.EqualTo(1));
            s.State.Units[0].Kind = UnitKind.Guardian; s.State.VisionThree[0] = true; Assert.That(s.View(Side.Player).Traps.Count, Is.EqualTo(1));
            Assert.That(s.Preview(Side.Player, C(BoardAction.Move, 29)).Units.Single(u => u.Id == 1).Hp, Is.EqualTo(3));
        }
        [Test] public void FirstStrike_PrecedesIntents_AndHasUndo()
        {
            var setup = Setup(); setup.FirstStrikeSide = Side.Player; var s = BoardSimulation.Create(setup); s.State.Tiles = new BoardTile[64];
            Assert.That(s.View(Side.Player).Phase, Is.EqualTo(BoardPhase.FirstStrike)); Assert.That(s.SetIntents(new BoardPlan()), Is.False);
            Assert.That(s.Apply(Side.Player, C(BoardAction.Move, 19))); Assert.That(s.Undo(Side.Player));
            Assert.That(s.EndResponse(Side.Player)); Assert.That(s.State.Turn, Is.EqualTo(1)); Assert.That(s.View(Side.Player).Phase, Is.EqualTo(BoardPhase.Intents));
        }
        [Test] public void SixTurnsEnd_RetreatAwardsAllNodes_AndMutualDestructionDraws()
        {
            var s = Flat(); s.State.Units[1].Cell = 63;
            for (int i = 0; i < 6; i++) { if (s.State.Phase == BoardPhase.Intents) s.SetIntents(new BoardPlan()); s.EndResponse(Side.Player); }
            Assert.That(s.Finished); Assert.That(s.State.Turn, Is.EqualTo(6));
            s = Flat(); Assert.That(s.Retreat(Side.Player)); Assert.That(s.Result.Nodes, Is.EqualTo(new[] { 0, 3 })); Assert.That(s.Retreat(Side.Ayna), Is.False);
            s = Flat(UnitKind.Overload); s.Apply(Side.Player, C(BoardAction.Ability, 27)); Assert.That(s.Finished); Assert.That(s.Result.Winner, Is.EqualTo(Side.None));
        }
        [Test] public void NodeOccupationWaitsUntilTurnEnd_ContestedAdjacencyKeepsOwner()
        {
            var s = Flat(); s.State.Units[1].Cell = 63; s.State.Nodes[0].Cell = 26; s.State.Nodes[0].Owner = Side.None;
            s.Apply(Side.Player, C(BoardAction.Move, 26)); Assert.That(s.State.Nodes[0].Owner, Is.EqualTo(Side.None)); s.EndResponse(Side.Player); Assert.That(s.State.Nodes[0].Owner, Is.EqualTo(Side.Player));
            s = Flat(); s.State.Nodes[0].Cell = 27; s.State.Nodes[0].Owner = Side.Ayna;
            s.State.Units.Add(new BoardUnit { Id = 3, Side = Side.Player, Hp = 2, Kind = UnitKind.Scout, Cell = 26 }); s.EndResponse(Side.Player);
            Assert.That(s.State.Nodes[0].Owner, Is.EqualTo(Side.Ayna));
        }
        [Test] public void CoreCabinet_HasFiveHp_ArmorAndBreaksOnlyOnBoard()
        {
            var s = Flat(UnitKind.Sniper); s.State.Units[1].Cell = 63; s.State.CoreCells[1] = 30; s.State.CoreHp[1] = 5;
            for (int i = 0; i < 5; i++)
            { if (s.State.Phase == BoardPhase.Intents) s.SetIntents(new BoardPlan()); Assert.That(s.Apply(Side.Player, C(BoardAction.Ability, 30))); if (!s.Finished) s.EndResponse(Side.Player); }
            Assert.That(s.Result.CoreBroken[1]); Assert.That(s.Result.Winner, Is.EqualTo(Side.Player));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void TemplatesAndEightTransforms_AreDeterministicAndPlayable(int template)
        {
            for (int transform = 0; transform < 8; transform++)
            {
                var setup = Setup(); foreach (var f in setup.Fighters) f.Cell = -1; setup.Template = template; setup.Transform = transform;
                var a = BoardSimulation.Create(setup); var b = BoardSimulation.Create(setup);
                Assert.That(a.StateHash, Is.EqualTo(b.StateHash)); Assert.That(a.View(Side.Player).Nodes.Count, Is.EqualTo(3));
                Assert.That(a.LegalCommands(Side.Player).Any(c => c.Action == BoardAction.Move));
            }
        }
        [Test] public void BeliefAndPlannerNeverSeeHiddenTraps_AndCannotMutateSource()
        {
            var a = Flat(UnitKind.Guardian); a.State.Traps.Add(new BoardTrap { Cell = 35, Owner = Side.Ayna }); var b = Flat(UnitKind.Guardian);
            string hash = a.StateHash; var av = a.View(Side.Player); Assert.That(AynaJson.Write(av), Is.EqualTo(AynaJson.Write(b.View(Side.Player))));
            Assert.That(AynaJson.Write(BoardPlanner.Choose(av)), Is.EqualTo(AynaJson.Write(BoardPlanner.Choose(b.View(Side.Player)))));
            Assert.That(AynaJson.Write(a.Preview(Side.Player, C(BoardAction.Move, 35))), Is.EqualTo(AynaJson.Write(b.Preview(Side.Player, C(BoardAction.Move, 35)))));
            Assert.That(a.StateHash, Is.EqualTo(hash)); Assert.That(BoardSimulation.CreateBelief(av).State.Traps, Is.Empty);
        }
        [Test] public void InvalidIntentPlanIsAtomic_AndTrapsCanOnlyBePlacedOnce()
        {
            var s = BoardSimulation.Create(Setup()); string hash = s.StateHash;
            Assert.That(s.SetIntents(new BoardPlan { Commands = { C(BoardAction.Move, 19, 2), C(BoardAction.Move, 18, 2) } }), Is.False); Assert.That(s.StateHash, Is.EqualTo(hash));
            Assert.That(s.PlaceTraps(new[] { 23, 24 })); Assert.That(s.PlaceTraps(new[] { 23 }), Is.False);
        }
    }
}
