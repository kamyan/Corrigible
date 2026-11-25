using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    public class FacilitySignatureTests
    {
        static Baskin.BState Board(string signature, string kind, int dir = 1)
        {
            var p = Params(Wing("weu_1"), 13, events: false);
            p.Signature = signature;
            var b = Open(p, System.Array.Empty<RoomReport>());
            foreach (var c in b.Cells)
            {
                c.T = "floor"; c.O = null; c.Door = 0; c.Vent = 0;
            }
            b.Units.Clear();
            b.Spawns.Clear();
            b.Units.Add(new Baskin.Unit { Id = b.NextId++, Type = signature, Side = 'e', X = 1, Y = 3, Hp = 2, Max = 2,
                Intent = new Baskin.Intent { Kind = kind, Dir = dir }, Order = 1 });
            b.Units.Add(new Baskin.Unit { Id = b.NextId++, Type = "tek", Side = 'p', X = 3, Y = 3, Hp = 3, Max = 3 });
            return b;
        }

        static void ResolveOnly(Baskin.BState b)
        {
            int rng = b.Rs;
            b.MaxTurns = b.Turn;
            Assert.IsTrue(Baskin.EndTurn(b));
            Assert.AreEqual(rng, b.Rs, "niyet uygulaması çekim yapmaz");
        }

        [Test]
        public void TwelveRegions_HaveTwelveSignatures_AndOldModeKeepsSix()
        {
            var sim = LegacySimulation.NewGame(79, Bal());
            var old = LegacySimulation.NewGame(79, new Balance { facilitySignatureMachines = false });
            var expected = new[] { "zdron", "havan", "alev", "muhur", "bomba", "kalkan", "cogalt", "tampon", "montaj", "kilit", "kirag", "capa" };
            CollectionAssert.AreEquivalent(expected, GameData.Regions.Select(r => sim.RegionSignature(r.Id)));
            foreach (var r in GameData.Regions)
                Assert.AreEqual(r.Enemy, old.RegionSignature(r.Id), r.Id);
            foreach (var type in expected)
            {
                var wing = Wing("weu_1");
                var p = Params(wing, 31, 2, events: false);
                p.Signature = type;
                var board = Baskin.CreateRoom(p, wing[2], FacilityMaps.Setup(Bal(false), wing[2], p.Seed));
                var signature = board.Units.FirstOrDefault(u => u.Type == type);
                Assert.IsNotNull(signature, type);
                Assert.IsTrue(Baskin.UnitTypes[type].Kinds.Contains(signature.Intent.Kind), type);
            }
        }

        [Test]
        public void Flame_HitsBothTiles_AndRespectsWalls()
        {
            var open = Board("alev", "flame");
            open.Cells[3, 2].O = new Baskin.Obj { K = "civ", Hp = 2, Max = 2 };
            Assert.AreEqual(2, Baskin.Intents(open).Single().Area.Count);
            ResolveOnly(open);
            Assert.AreEqual(2, open.Units[1].Hp);
            Assert.AreEqual(1, open.Cells[3, 2].O.Hp);

            var wall = Board("alev", "flame");
            wall.Cells[3, 2].T = "wall";
            ResolveOnly(wall);
            Assert.AreEqual(3, wall.Units[1].Hp);
        }

        [Test]
        public void Flame_PrimesThePressureCylinder()
        {
            var b = Board("alev", "flame");
            b.Ev = new Baskin.EvState { Id = "tup" };
            b.Ev.Fuses.Add(new Baskin.Fuse { X = 2, Y = 3, Dir = 1 });
            b.Cells[3, 2].O = new Baskin.Obj { K = "tup", Hp = 1, Max = 1 };
            int turn = b.Turn;
            ResolveOnly(b);
            Assert.AreEqual(turn + 1, b.Ev.Fuses.Single().Due);
            Assert.AreEqual("tup", b.Cells[3, 2].O?.K);
        }

        [Test]
        public void RamPushesBothSides_AndPullStopsBeforeTheRobot()
        {
            foreach (char side in new[] { 'p', 'e' })
            {
                var b = Board("tampon", "ram");
                var victim = b.Units[1];
                victim.Side = side;
                if (side == 'e') b.Units.Add(new Baskin.Unit { Id = b.NextId++, Type = "tek", Side = 'p', X = 6, Y = 6, Hp = 3, Max = 3 });
                ResolveOnly(b);
                Assert.AreEqual(5, victim.X);
                Assert.AreEqual(2, b.Units[0].X);
            }

            var pull = Board("capa", "pull");
            ResolveOnly(pull);
            Assert.AreEqual(2, pull.Units[1].X);
            Assert.AreEqual(3, pull.Units[1].Hp);
        }

        [Test]
        public void Seal_CannotCutTheOnlyPassageOrBuildOnTheGate()
        {
            var open = Board("muhur", "seal");
            ResolveOnly(open);
            Assert.AreEqual("kalas", open.Cells[3, 2].O?.K);
            Assert.AreEqual(3, open.Cells[3, 2].O?.Hp);

            var gate = Board("muhur", "seal");
            gate.Cells[3, 2].Door = 1;
            Assert.IsTrue(Baskin.Intents(gate).Single().TargetNone);
            ResolveOnly(gate);
            Assert.IsNull(gate.Cells[3, 2].O);

            var corridor = Board("muhur", "seal");
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    if (y != 3 && !corridor.Room.West.Any(p => p.X == x && p.Y == y) &&
                        !corridor.Room.Forward.Any(p => p.X == x && p.Y == y)) corridor.Cells[y, x].T = "wall";
            int row = corridor.Room.West[0].Y;
            for (int y = System.Math.Min(3, row); y <= System.Math.Max(3, row); y++) corridor.Cells[y, 0].T = "floor";
            ResolveOnly(corridor);
            Assert.IsNull(corridor.Cells[3, 2].O);
        }

        [Test]
        public void Frost_MarksCrossAndStopsWalkForTwoTurns()
        {
            var b = Board("kirag", "frost");
            var u = b.Units[1];
            u.X = 2;
            Assert.AreEqual(5, Baskin.Intents(b).Single().Area.Count);
            int turn = b.Turn;
            ResolveOnly(b);
            foreach (int offset in new[] { 1, 2 })
            {
                b.Turn = turn + offset;
                Assert.IsTrue(Baskin.Frosted(b, 3, 3));
                Assert.IsFalse(Baskin.Reachable(b, u).Any(p => p.X == 4 && p.Y == 3));
                Assert.IsNull(Baskin.WalkPath(b, u, 3, 4, 3));
            }
            b.Turn = turn + 3;
            Assert.IsFalse(Baskin.Frosted(b, 3, 3));
            Assert.IsTrue(Baskin.Reachable(b, u).Any(p => p.X == 4 && p.Y == 3));
        }

        [Test]
        public void Assembler_UsesScrap_AndItsOwnDroneLeavesNoScrap()
        {
            var b = Board("montaj", "build");
            var donor = new Baskin.Unit { Id = b.NextId++, Type = "dron", Side = 'e', X = 2, Y = 3, Hp = 1, Max = 1 };
            b.Units.Add(donor);
            Baskin.TestHurt(b, donor, 1);
            Assert.AreEqual("hurda", b.Cells[3, 2].O?.K);
            ResolveOnly(b);
            var drone = b.Units.Single(u => u.Assembled);
            Assert.AreEqual("dron", drone.Type);
            Assert.IsNull(b.Cells[3, 2].O);
            Baskin.TestHurt(b, drone, 3);
            Assert.IsNull(b.Cells[3, 2].O);
        }

        [Test]
        public void MachineBarriers_SurviveRoomReportButDoNotBecomeTraces()
        {
            var b = Board("muhur", "seal");
            ResolveOnly(b);
            var report = new RoomReport { Room = b.Room.Index };
            var wall = b.Cells[3, 2].O;
            report.Objects.Add(new RoomObject { K = wall.K, Hp = wall.Hp, Max = wall.Max, X = 2, Y = 3, MachineBuilt = wall.MachineBuilt });
            report.Objects.Add(new RoomObject { K = "kalas", Hp = 2, Max = 3, X = 3, Y = 2 });
            var restored = RoomReport.Decode(report.Encode());
            Assert.IsTrue(restored.Objects[0].MachineBuilt);
            var marks = FacilityMarks.Derive(b.Room, new[] { restored }, 0).Where(m => m.Kind == "kalas").ToList();
            Assert.AreEqual(1, marks.Count);
            Assert.AreEqual(3, marks[0].X);
            Assert.AreEqual(2, marks[0].Y);
        }

        [Test]
        public void AssembledBit_RoundTripsWithAndWithoutBolt()
        {
            foreach (bool unbolted in new[] { false, true })
            {
                var r = new RoomReport { Room = 1 };
                r.Machines.Add(new RoomMachine { Type = "dron", X = 2, Y = 3, Hp = 2, Assembled = true, Unbolted = unbolted });
                var decoded = RoomReport.Decode(r.Encode()).Machines.Single();
                Assert.IsTrue(decoded.Assembled);
                Assert.AreEqual(unbolted, decoded.Unbolted);
            }
        }

        [Test]
        public void AssembledCaptive_KeepsItsScrapExceptionAcrossTheGate()
        {
            var p = WithGear(Params(Wing("cn_1"), 13, events: false), 0, FacilityGear.Clamp);
            p.Signature = "montaj";
            var reports = new List<RoomReport>();
            var board = Open(p, reports);
            Flat(board, new Baskin.Pt(3, 3), new Baskin.Pt(3, 4), new Baskin.Pt(4, 4));
            var drone = AddMachine(board, "dron", new Baskin.Pt(4, 3), 1);
            drone.Assembled = true;
            Assert.IsTrue(Baskin.DoAction(board, BySlot(board, 0).Id, Baskin.ActClamp, drone.X, drone.Y));
            var report = PassRoom(p, reports, board);
            reports[0] = RoomReport.Decode(report.Encode());

            var next = Open(p, reports);
            var captive = next.Units.Single(Baskin.IsCaptive);
            Assert.IsTrue(captive.Assembled);
            Baskin.TestHurt(next, captive, captive.Hp + captive.Armor);
            Assert.IsNull(Baskin.CellAt(next, captive.X, captive.Y).O);
        }
    }
}
