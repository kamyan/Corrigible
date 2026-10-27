using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Taktik harita motoru (<see cref="Baskin"/>): rastgelelik, ikmal baskınının tam savaşı, önizleme ve geri alma. Beklenen
    /// durumlar eski tarayıcı prototipinin parite çıktısıdır; artık gerileme testi olarak korunur.
    /// </summary>
    public sealed class RaidEngineTests
    {
        [Test]
        public void Rng_IsMulberry32()
        {
            var r = new Rng(42);
            Assert.AreEqual(0.601103751920164, r.Next(), 1e-15);
            Assert.AreEqual(0.448290558997542, r.Next(), 1e-15);
            Assert.AreEqual(0.852465793490410, r.Next(), 1e-15);
            Assert.AreEqual(0.669734041439369, r.Next(), 1e-15);
        }

        [TestCase(11,
            "m4.2.1 m5.3.1 m6.1.4 e m4.4.2 m5.5.2 a5.5.6 m6.1.3 e m4.5.4 a4.6.4",
            "won|t3|c2|rs1071847760|dron22:2,bakim56:0x,dron72:1,tek54:2,emp52:3,ana13:2,dron37:2,bakim62:2|civ30:1,spawner70:2,civ15:1,civ07:1,spawner77:2")]
        [TestCase(99,
            "m4.2.1 m5.3.1 m6.1.4 e m4.4.2 m5.5.2 a5.5.6 m6.1.3 e m4.5.4 a4.6.4",
            "won|t3|c2|rs1071847848|dron22:2,bakim56:0x,dron37:1,tek54:2,emp52:3,ana13:2,bakim51:2,dron72:2|civ30:1,spawner70:2,civ15:1,civ07:1,spawner77:2")]
        public void LootRaid_SameMovesGiveSameBattle(int seed, string moves, string expected)
        {
            var state = Baskin.Create(new RaidParams { Seed = seed, MissionKind = "loot" });
            foreach (string move in moves.Split(' '))
            {
                if (move == "e") { Assert.IsTrue(Baskin.EndTurn(state), move); continue; }
                int[] p = move.Substring(1).Split('.').Select(int.Parse).ToArray();
                Assert.IsTrue(move[0] == 'm' ? Baskin.DoMove(state, p[0], p[1], p[2]) : Baskin.DoAct(state, p[0], p[1], p[2]), move);
            }
            Assert.AreEqual(expected, Baskin.Signature(state));
            Assert.IsTrue(Baskin.Result(state).Success);
        }

        [Test]
        public void SameParams_GiveSameBoard()
        {
            var p = new RaidParams { Seed = 4242, Compute = 4, Inf = 40, IsCore = true, Traits = new List<string> { "kopya" } };
            Assert.AreEqual(Baskin.Signature(Baskin.Create(p)), Baskin.Signature(Baskin.Create(p)));
        }

        [Test]
        public void CopyTrait_WakesTwoMachinesPerTurn()
        {
            Assert.AreEqual(1, Baskin.Create(new RaidParams { Seed = 5 }).SpawnPerTurn);
            Assert.AreEqual(2, Baskin.Create(new RaidParams { Seed = 5, Traits = new List<string> { "kopya" } }).SpawnPerTurn);
        }

        [Test]
        public void Undo_RestoresExactState()
        {
            var B = Baskin.Create(new RaidParams { Seed = 99, Compute = 4, Inf = 40, IsCore = true });
            var u = B.Units.First(x => x.Type == "tek");
            string before = Baskin.Signature(B);
            var to = Baskin.Reachable(B, u)[0];
            Assert.IsTrue(Baskin.DoMove(B, u.Id, to.X, to.Y));
            Assert.AreNotEqual(before, Baskin.Signature(B));
            Assert.IsTrue(Baskin.Undo(B));
            Assert.AreEqual(before, Baskin.Signature(B));
        }

        [Test]
        public void LootPreview_CollectsCargoWithoutChangingLiveStateOrRng()
        {
            var state = OpenCargoFixture();
            var technician = state.Units.First(u => u.Type == "tek");
            technician.X = 2; technician.Y = 3;
            string before = Baskin.Signature(state);
            int rng = state.Rs;
            var preview = Baskin.Preview(state, technician.Id, 3, 3);
            Assert.IsTrue(preview.Won);
            Assert.IsTrue(preview.Fx.Any(f => f.K == "loot"));
            Assert.AreEqual(before, Baskin.Signature(state));
            Assert.IsTrue(Baskin.DoAct(state, technician.Id, 3, 3));
            Assert.AreEqual("won", state.Phase);
            Assert.AreEqual(rng, state.Rs);
            Assert.AreEqual(0, state.Collateral);
            Assert.IsNull(state.Cells[3, 3].O);
        }

        [Test]
        public void DestroyedCargo_IsFailure()
        {
            var state = OpenCargoFixture();
            var emp = state.Units.First(u => u.Type == "emp");
            emp.X = 5; emp.Y = 0;
            var drone = state.Units.First(u => u.Side == 'e');
            drone.Dead = false; drone.X = 4; drone.Y = 3; drone.Hp = 2;
            state.Cells[3, 3].O.Hp = 1;
            Assert.IsTrue(Baskin.DoAct(state, emp.Id, 5, 3));
            Assert.AreEqual("lost", state.Phase);
            Assert.IsFalse(Baskin.Result(state).Success);
            Assert.IsNull(state.Cells[3, 3].O);
        }

        [Test]
        public void MissingMissionKind_IsServerRaid()
        {
            var unset = Baskin.Create(new RaidParams { Seed = 777, IsCore = true, MissionKind = null });
            var server = Baskin.Create(new RaidParams { Seed = 777, IsCore = true, MissionKind = "server" });
            Assert.AreEqual(Baskin.Signature(unset), Baskin.Signature(server));
            Assert.IsFalse(Baskin.IsLoot(server));
        }

        [Test]
        public void LootMission_HasSingleCargoEvenInCoreRegion()
        {
            var state = Baskin.Create(new RaidParams { Seed = 777, IsCore = true, MissionKind = "loot" });
            Assert.IsFalse(state.IsCore);
            Assert.AreEqual(1, Baskin.TargetsLeft(state).Count);
        }

        static Baskin.BState OpenCargoFixture()
        {
            var state = Baskin.Create(new RaidParams { Seed = 11, MissionKind = "loot" });
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) state.Cells[y, x] = new Baskin.Cell { T = "floor" };
            state.Cells[3, 3].O = new Baskin.Obj { K = "node", Hp = 4, Max = 4 };
            foreach (var unit in state.Units.Where(u => u.Side == 'e')) unit.Dead = true;
            return state;
        }
    }
}
