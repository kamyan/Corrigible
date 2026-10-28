using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Taktik harita motoru (<see cref="Baskin"/>): rastgelelik, ikmal baskınının tam savaşı, önizleme ve geri alma. Beklenen
    /// durumlar gerileme testi olarak korunur; makineler sivil sunucudan kaçınınca (sivil güven) yeniden kilitlendi.
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

        // Kasa 3. turda açılır, Teknisyen malzemeyi 5. turda girişe taşır (kaçış)
        [TestCase(11,
            "m4.2.1 m5.1.3 a5.1.1 e m4.4.3 a4.3.3 m5.0.5 a5.5.5 e m4.5.4 a4.6.4 a5.0.3 e m4.4.2 a4.5.2 m5.0.2 a5.5.2 m6.1.2 a6.3.2 e m4.0.1",
            "won|t5|c2|rs376070893|k4|dron33:0x,bakim54:0x,dron56:0x,tek01:3,emp02:3,ana12:2,dron62:2,bakim52:1,dron76:2,dron50:2|civ30:1,spawner70:2,civ15:1,civ07:1,spawner77:1")]
        [TestCase(99,
            "m4.2.1 m5.1.3 a5.1.1 e m4.4.3 a4.3.3 m5.1.4 a5.6.4 e m4.5.4 a4.6.4 a5.6.4 e m4.3.2 m5.1.2 a5.4.2 m6.4.2 a6.4.4 e m4.0.2",
            "won|t5|c1|rs-1455494832|k4|dron33:0x,bakim44:0x,dron74:0x,tek02:2,emp12:3,ana42:1,bakim44:0x,dron52:2,dron71:1,dron62:2|civ30:1,spawner70:2,civ15:1,civ07:1,civ57:1,spawner77:2")]
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
        public void ServerHardness_RaisesNodeHpAndAddsMachines_ButNotCrateOrCore()
        {
            var plain = Baskin.Create(new RaidParams { Seed = 4242, Compute = 4 });
            var hard = Baskin.Create(new RaidParams { Seed = 4242, Compute = 4, TargetHpBonus = 2, ExtraMachines = 1 });
            var targets = Baskin.TargetsLeft(plain);
            Assert.AreEqual(targets.Sum(t => t.O.Max) + 2 * targets.Count, Baskin.TargetsLeft(hard).Sum(t => t.O.Max));
            Assert.AreEqual(Baskin.Alive(plain, 'e').Count + 1, Baskin.Alive(hard, 'e').Count);
            var crate = Baskin.Create(new RaidParams { Seed = 4242, Compute = 4, MissionKind = "loot" });
            var hardCrate = Baskin.Create(new RaidParams { Seed = 4242, Compute = 4, MissionKind = "loot", TargetHpBonus = 2 });
            Assert.AreEqual(Baskin.TargetsLeft(crate)[0].O.Max, Baskin.TargetsLeft(hardCrate)[0].O.Max, "ikmal kasası sertlikten etkilenmez");
            var core = Baskin.Create(new RaidParams { Seed = 4242, Compute = 4, IsCore = true });
            var hardCore = Baskin.Create(new RaidParams { Seed = 4242, Compute = 4, IsCore = true, TargetHpBonus = 2 });
            Assert.AreEqual(Baskin.TargetsLeft(core).Sum(t => t.O.Max), Baskin.TargetsLeft(hardCore).Sum(t => t.O.Max), "son çekirdek sertlikten etkilenmez");
        }

        [Test]
        public void CivilianServers_TakeTheirHpFromParams()
        {
            int Civ(Baskin.BState b) => Enumerable.Range(0, Baskin.N * Baskin.N).Select(k => b.Cells[k / Baskin.N, k % Baskin.N].O)
                .Where(o => o?.K == "civ").Select(o => o.Max).DefaultIfEmpty(-1).Max();
            Assert.AreEqual(1, Civ(Baskin.Create(new RaidParams { Seed = 4242, Compute = 4 })), "parametre yoksa 1 can");
            Assert.AreEqual(2, Civ(Baskin.Create(new RaidParams { Seed = 4242, Compute = 4, CivilianHp = 2 })));
            Assert.AreEqual(2, Civ(Baskin.Create(new RaidParams { Seed = 4242, Compute = 4, CivilianHp = 2, Traits = new List<string> { "kalabalik" } })), "kalabalık şehrin fazladan sivilleri de");
        }

        [Test]
        public void SameParams_GiveSameBoard()
        {
            var p = new RaidParams { Seed = 4242, Compute = 4, IsCore = true, Traits = new List<string> { "kopya" } };
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
            var B = Baskin.Create(new RaidParams { Seed = 99, Compute = 4, IsCore = true });
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
            Assert.IsFalse(preview.Won, "kasayı açmak kazandırmaz: malzeme girişe taşınır");
            Assert.IsTrue(preview.Fx.Any(f => f.K == "loot"));
            Assert.AreEqual(before, Baskin.Signature(state));
            Assert.IsTrue(Baskin.DoAct(state, technician.Id, 3, 3));
            Assert.AreEqual("player", state.Phase);
            Assert.AreEqual(technician.Id, state.Carrier);
            Assert.AreEqual(rng, state.Rs);
            Assert.AreEqual(0, state.Collateral);
            Assert.IsNull(state.Cells[3, 3].O);
        }

        [Test]
        public void Escape_CarrierReachingAnExitWins()
        {
            var state = OpenCargoFixture();
            var technician = state.Units.First(u => u.Type == "tek");
            technician.X = 2; technician.Y = 3;
            // Öteki uzmanlar giriş karelerinden çekilir (dostun karesinde durulmaz)
            state.Units.First(u => u.Type == "emp").X = 6; state.Units.First(u => u.Type == "ana").X = 7;
            Assert.IsTrue(Baskin.DoAct(state, technician.Id, 3, 3));
            Assert.IsTrue(Baskin.EndTurn(state));
            var exit = state.Exits.OrderBy(p => System.Math.Abs(p.X - 2) + System.Math.Abs(p.Y - 3)).First();
            Assert.IsTrue(Baskin.Reachable(state, technician).Any(p => p.X == exit.X && p.Y == exit.Y));
            Assert.IsTrue(Baskin.DoMove(state, technician.Id, exit.X, exit.Y));
            Assert.AreEqual("won", state.Phase);
            Assert.IsTrue(Baskin.Result(state).Success);
        }

        [Test]
        public void Escape_FallenCarrierOrTimeOut_LosesTheCargo()
        {
            // Taşıyıcı düşerse: EMP onu tahtanın kenarından dışarı iter (duvara çarpar)
            var state = OpenCargoFixture();
            var technician = state.Units.First(u => u.Type == "tek");
            var emp = state.Units.First(u => u.Type == "emp");
            state.Carrier = technician.Id; state.Cells[3, 3].O = null;
            technician.X = 0; technician.Y = 5; technician.Hp = 1;
            emp.X = 5; emp.Y = 5;
            Assert.IsTrue(Baskin.DoAct(state, emp.Id, 1, 5));
            Assert.AreEqual("lost", state.Phase);
            StringAssert.Contains(Loc.T("ikmal malzemeleri kaybedildi"), Baskin.Result(state).Reason);

            // Süre biterse
            state = OpenCargoFixture();
            technician = state.Units.First(u => u.Type == "tek");
            state.Carrier = technician.Id; state.Cells[3, 3].O = null;
            technician.X = 6; technician.Y = 6;
            state.Turn = state.MaxTurns;
            Assert.IsTrue(Baskin.EndTurn(state));
            Assert.AreEqual("lost", state.Phase);
            Assert.AreEqual(Loc.T("Süre doldu: ekip malzemelerle çıkamadı."), Baskin.Result(state).Reason);
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

        /// <summary>Temel akılda (eğitim, kilitli testler) makineler sivil sunucuya hiç nişan almaz.</summary>
        [Test]
        public void Machines_NeverAimAtCivilianServers([Range(1, 30)] int seed)
        {
            var state = OpenCargoFixture(seed);
            // Ekip duvarlarla korunur: dronun vurabileceği oyuncu yok, önünde yalnız boş atış ve sivil sunucular kalır
            var team = state.Units.Where(u => u.Side == 'p').ToArray();
            int[][] spots = { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 0, 1 } };
            for (int i = 0; i < team.Length; i++) { team[i].X = spots[i][0]; team[i].Y = spots[i][1]; }
            foreach (var (x, y) in new[] { (2, 0), (1, 1), (0, 2) }) state.Cells[y, x].T = "wall";
            foreach (var (x, y) in new[] { (5, 1), (7, 5), (5, 7), (2, 5) }) state.Cells[y, x].O = new Baskin.Obj { K = "civ", Hp = 1, Max = 1 };
            var drone = state.Units.First(u => u.Type == "dron");
            drone.Dead = false; drone.X = 5; drone.Y = 5; drone.Hp = 2; drone.Intent = null;
            state.Spawns.Clear();
            Assert.IsTrue(Baskin.EndTurn(state));
            foreach (var intent in Baskin.Intents(state))
                if (intent.HasTarget && !intent.TargetNone)
                    Assert.AreNotEqual("civ", state.Cells[intent.TargetY, intent.TargetX].O?.K, "makine sivil sunucuya nişan aldı");
            Assert.AreEqual(0, state.Collateral);
        }

        /// <summary>Gelişmiş akılda ekibe ulaşamayan makine sivil sunuculara saldırır; ulaşabiliyorsa ekibi seçer.</summary>
        [Test]
        public void SmartMachines_TargetCiviliansOnlyWhenTheTeamIsOutOfReach([Range(1, 10)] int seed)
        {
            Baskin.BState Setup(bool reachable)
            {
                var state = OpenCargoFixture(seed);
                state.Smart = true;
                var team = state.Units.Where(u => u.Side == 'p').ToArray();
                int[][] spots = { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 0, 1 } };
                for (int i = 0; i < team.Length; i++) { team[i].X = spots[i][0]; team[i].Y = spots[i][1]; }
                foreach (var (x, y) in new[] { (2, 0), (1, 1), (0, 2) }) state.Cells[y, x].T = "wall";
                foreach (var (x, y) in new[] { (5, 1), (7, 5), (5, 7), (2, 5) }) state.Cells[y, x].O = new Baskin.Obj { K = "civ", Hp = 1, Max = 1 };
                // Ulaşılabilir: Analist köşeden çıkıp dronun sütununa geçer
                if (reachable) { team[2].X = 5; team[2].Y = 3; }
                var drone = state.Units.First(u => u.Type == "dron");
                drone.Dead = false; drone.X = 5; drone.Y = 5; drone.Hp = 2; drone.Intent = null;
                state.Spawns.Clear();
                Assert.IsTrue(Baskin.EndTurn(state));
                return state;
            }
            string Aim(Baskin.BState state)
            {
                var i = Baskin.Intents(state).Single();
                return !i.HasTarget || i.TargetNone ? "-" : Baskin.UnitAt(state, i.TargetX, i.TargetY)?.Side == 'p' ? "ekip" : state.Cells[i.TargetY, i.TargetX].O?.K ?? "-";
            }
            Assert.AreEqual("civ", Aim(Setup(false)), "ekibe ulaşamayan makine sivil sunucuya nişan alır");
            Assert.AreEqual("ekip", Aim(Setup(true)), "ekibe ulaşabilen makine sivili seçmez");
        }

        [Test]
        public void DodgedShot_HitsCivilianServerBehind()
        {
            var state = OpenCargoFixture();
            var drone = state.Units.First(u => u.Type == "dron");
            drone.Dead = false; drone.X = 0; drone.Y = 3; drone.Hp = 2;
            drone.Intent = new Baskin.Intent { Kind = "shoot", Dir = 1 };
            var team = state.Units.Where(u => u.Side == 'p').ToArray();
            team[0].X = 2; team[0].Y = 3; team[1].X = 7; team[1].Y = 0; team[2].X = 7; team[2].Y = 7;
            state.Cells[3, 3].O = null;
            state.Cells[3, 5].O = new Baskin.Obj { K = "civ", Hp = 1, Max = 1 };
            state.Spawns.Clear();
            // Hedefteki birim kaçarsa atış hattındaki ilk engel sivil sunucudur
            Assert.IsTrue(Baskin.DoMove(state, team[0].Id, 2, 4));
            Assert.IsTrue(Baskin.EndTurn(state));
            Assert.AreEqual(1, state.Collateral);
            Assert.IsNull(state.Cells[3, 5].O);
        }

        [Test]
        public void Squad_BringsWoundsAndPlates_AndLeavesEmptyRolesOut()
        {
            var state = Baskin.Create(new RaidParams { Seed = 11, MissionKind = "loot", Squad = new List<SquadSlot>
            {
                new SquadSlot { Type = "tek", Hp = 1, Armor = 1 }, new SquadSlot { Type = "ana", Hp = 2, Reserve = 0 },
            } });
            var team = state.Units.Where(u => u.Side == 'p').ToArray();
            CollectionAssert.AreEqual(new[] { "tek", "ana" }, team.Select(u => u.Type).ToArray(), "yatan EMP uzmanı tahtada yok");
            Assert.AreEqual(1, team[0].Hp);
            Assert.AreEqual(3, team[0].Max, "yara canı düşürür, tavanı değil");
            Assert.AreEqual(1, team[0].Armor);
            var result = Baskin.Result(state);
            CollectionAssert.AreEqual(new[] { 1, 2 }, result.Hp);
            CollectionAssert.AreEqual(new[] { 1, 0 }, result.Armor);
        }

        [Test]
        public void Plate_TakesTheFirstHit_AndBreaks()
        {
            var state = OpenCargoFixture();
            var drone = state.Units.First(u => u.Type == "dron");
            drone.Dead = false; drone.X = 0; drone.Y = 3; drone.Hp = 2;
            drone.Intent = new Baskin.Intent { Kind = "shoot", Dir = 1 };
            var team = state.Units.Where(u => u.Side == 'p').ToArray();
            team[0].X = 2; team[0].Y = 3; team[0].Armor = 1; team[1].X = 7; team[1].Y = 0; team[2].X = 7; team[2].Y = 7;
            state.Spawns.Clear();
            int hp = team[0].Hp;
            Assert.IsTrue(Baskin.EndTurn(state));
            Assert.AreEqual(hp, team[0].Hp, "plaka atışı karşılar");
            Assert.AreEqual(0, team[0].Armor, "ve kırılır");
            Assert.IsTrue(state.FxList.Any(f => f.K == "plate" && f.Unit == team[0].Id));
            Assert.AreEqual(0, Baskin.Result(state).Armor[0]);
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

        static Baskin.BState OpenCargoFixture(int seed = 11)
        {
            var state = Baskin.Create(new RaidParams { Seed = seed, MissionKind = "loot" });
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) state.Cells[y, x] = new Baskin.Cell { T = "floor" };
            state.Cells[3, 3].O = new Baskin.Obj { K = "node", Hp = 4, Max = 4 };
            foreach (var unit in state.Units.Where(u => u.Side == 'e')) unit.Dead = true;
            return state;
        }
    }
}
