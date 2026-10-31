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

        // ---------- Madenci ve Sis uzmanı ----------

        [Test]
        public void Dynamite_BreaksTheWall_DebrisHitsTheNextTile_AndUndoRestoresIt()
        {
            var state = SpecialistFixture(out _, out var miner, out _);
            miner.X = 2; miner.Y = 5;
            state.Cells[5, 3].T = "wall";
            var drone = Machine(state, "dron", 4, 5);
            string before = Baskin.Signature(state);
            Assert.IsTrue(Baskin.Targets(state, miner).Any(p => p.X == 3 && p.Y == 5));
            Assert.IsTrue(Baskin.DoAct(state, miner.Id, 3, 5));
            Assert.AreEqual("floor", state.Cells[5, 3].T, "duvar kalıcı olarak zemine döner");
            Assert.AreEqual(1, drone.Hp, "moloz bir kare öteye sıçrar");
            Assert.AreEqual(1, miner.Charges, "duvar yıkmak bir dinamit harcar");
            Assert.IsTrue(state.FxList.Any(f => f.K == "breach" && f.X == 3 && f.Y == 5));
            Assert.IsTrue(state.FxList.Any(f => f.K == "debris" && f.X == 4 && f.Y == 5));
            StringAssert.Contains("|r35", Baskin.Signature(state));
            Assert.IsTrue(Baskin.Undo(state));
            Assert.AreEqual("wall", state.Cells[5, 3].T, "geri alma duvarı geri getirir");
            Assert.AreEqual(2, state.Units.First(u => u.Type == "maden").Charges);
            Assert.AreEqual(before, Baskin.Signature(state));
        }

        [Test]
        public void Debris_SparesTheCrate_StopsAtWalls_AndSetsOffCoolingUnits()
        {
            // Kasanın önündeki duvar: moloz kasaya işlemez
            var state = SpecialistFixture(out _, out var miner, out _);
            miner.X = 3; miner.Y = 5;
            state.Cells[4, 3].T = "wall";
            Assert.IsTrue(Baskin.DoAct(state, miner.Id, 3, 4));
            Assert.AreEqual(4, state.Cells[3, 3].O.Hp, "ikmal kasası molozdan hasar almaz");
            Assert.AreEqual("player", state.Phase);

            // Arkadaki duvar: moloz durur, ikinci duvar yerinde
            state = SpecialistFixture(out _, out miner, out _);
            miner.X = 2; miner.Y = 5;
            state.Cells[5, 3].T = "wall"; state.Cells[5, 4].T = "wall";
            Assert.IsTrue(Baskin.DoAct(state, miner.Id, 3, 5));
            Assert.AreEqual("wall", state.Cells[5, 4].T);
            Assert.IsFalse(state.FxList.Any(f => f.K == "debris"));

            // Soğutma ünitesi: moloz onu patlatır, patlama yanındaki robotu vurur; önizleme zinciri gösterir
            state = SpecialistFixture(out _, out miner, out _);
            miner.X = 2; miner.Y = 5;
            state.Cells[5, 3].T = "wall";
            state.Cells[5, 4].O = new Baskin.Obj { K = "cool", Hp = 1, Max = 1 };
            var drone = Machine(state, "dron", 5, 5);
            var preview = Baskin.Preview(state, miner.Id, 3, 5);
            Assert.IsTrue(preview.Fx.Any(f => f.K == "break" && f.Obj == "cool"));
            Assert.IsTrue(preview.Fx.Any(f => f.K == "dmg" && f.Unit == drone.Id));
            Assert.IsTrue(Baskin.DoAct(state, miner.Id, 3, 5));
            Assert.IsNull(state.Cells[5, 4].O);
            Assert.AreEqual(1, drone.Hp);
        }

        [Test]
        public void Dynamite_TargetsWallsRobotsAndPlainObjects_NotCabinetsOrAllies()
        {
            var state = SpecialistFixture(out var tek, out var miner, out _);
            miner.X = 3; miner.Y = 2;
            tek.X = 4; tek.Y = 2;
            state.Cells[1, 3].T = "wall";
            state.Cells[2, 2].O = new Baskin.Obj { K = "civ", Hp = 1, Max = 1 };
            var targets = Baskin.Targets(state, miner).Select(p => (p.X, p.Y)).ToList();
            CollectionAssert.AreEquivalent(new[] { (3, 1), (2, 2) }, targets, "kasa (3,3) ve dost uzman (4,2) hedef değil");
            miner.Charges = 0;
            CollectionAssert.AreEquivalent(new[] { (2, 2) }, Baskin.Targets(state, miner).Select(p => (p.X, p.Y)).ToList(), "dinamit bitince duvar yıkılmaz");

            // Robota atılan dinamit 1 hasar verir, itmez ve hak harcamaz
            state = SpecialistFixture(out _, out miner, out _);
            miner.X = 2; miner.Y = 5;
            var drone = Machine(state, "dron", 3, 5);
            Assert.IsTrue(Baskin.DoAct(state, miner.Id, 3, 5));
            Assert.AreEqual(1, drone.Hp);
            Assert.AreEqual(3, drone.X);
            Assert.AreEqual(2, miner.Charges);
        }

        [Test]
        public void Smoke_CutsShotsAtTheSmokeAndBeyond_ButNotTheShooterInIt()
        {
            int ShotAt(int sx, int sy)
            {
                var state = SpecialistFixture(out var tek, out _, out var smoke);
                tek.X = 2; tek.Y = 3;
                smoke.X = sx; smoke.Y = 6;
                Machine(state, "dron", 0, 3, "shoot", 1);
                if (sy >= 0) Assert.IsTrue(Baskin.DoAct(state, smoke.Id, sx, sy));
                Assert.IsTrue(Baskin.EndTurn(state));
                Assert.IsFalse(Enumerable.Range(0, Baskin.N * Baskin.N).Any(k => state.Cells[k / Baskin.N, k % Baskin.N].Smoke), "sis evrenin sonunda dağılır");
                return tek.Hp;
            }
            Assert.AreEqual(2, ShotAt(5, -1), "sis yoksa atış Teknisyen'i vurur");
            Assert.AreEqual(3, ShotAt(1, 3), "aradaki sis atışı keser");
            Assert.AreEqual(3, ShotAt(2, 3), "uzmanın karesindeki sis de keser");
            Assert.AreEqual(2, ShotAt(0, 3), "sisin içindeki makine yine ateş eder");
        }

        [Test]
        public void Smoke_CutsLockMeleeAndMortarOnTheSmokedTile_NotBlasts()
        {
            Baskin.BState Run(string type, int mx, int my, string kind, int dir, int tx, int ty, int sx, int sy, out Baskin.Unit tek)
            {
                var state = SpecialistFixture(out tek, out _, out var smoke);
                tek.X = tx; tek.Y = ty;
                // Sis uzmanı aynı sütunda dört kare öteden atar
                smoke.X = sx; smoke.Y = sy + (sy >= 4 ? -4 : 4);
                Machine(state, type, mx, my, kind, dir);
                Assert.IsTrue(Baskin.DoAct(state, smoke.Id, sx, sy), type);
                Assert.IsTrue(Baskin.EndTurn(state));
                return state;
            }
            Run("kilit", 0, 1, "lock", 1, 3, 1, 2, 1, out var locked);
            Assert.IsFalse(locked.Locked, "kilit sise girince kaybolur");
            var melee = Run("bakim", 4, 4, "melee", 3, 3, 4, 3, 4, out var hit);
            Assert.AreEqual(3, hit.Hp, "bakım robotu sisli kareye vuramaz");
            Assert.IsTrue(melee.FxList.Any(f => f.K == "smoked" && f.X == 3 && f.Y == 4));
            Run("havan", 0, 6, "lob", 1, 3, 6, 1, 6, out var lobbed);
            Assert.AreEqual(2, lobbed.Hp, "havan engellerin üstünden atar: yoldaki sis onu durdurmaz");
            Run("havan", 0, 6, "lob", 1, 3, 6, 3, 6, out lobbed);
            Assert.AreEqual(3, lobbed.Hp, "havan sisli kareye düşerse boşa gider");
            Run("bomba", 5, 1, "blast", 0, 5, 2, 5, 2, out var blasted);
            Assert.AreEqual(2, blasted.Hp, "patlama nişan değildir: sis onu kesmez");
        }

        [Test]
        public void SmokePreview_ShowsTheCutIntent_WithoutChangingTheLiveState()
        {
            var state = SpecialistFixture(out var tek, out _, out var smoke);
            tek.X = 2; tek.Y = 3;
            smoke.X = 2; smoke.Y = 6;
            Machine(state, "dron", 0, 3, "shoot", 1);
            var live = Baskin.Intents(state).Single();
            Assert.IsFalse(live.Smoked);
            Assert.AreEqual(2, live.TargetX);
            var preview = Baskin.Preview(state, smoke.Id, 2, 3);
            var cut = preview.Intents.Single();
            Assert.IsTrue(cut.Smoked && cut.TargetNone);
            Assert.AreEqual((2, 3), (cut.TargetX, cut.TargetY));
            Assert.IsFalse(state.Cells[3, 2].Smoke, "önizleme canlı durumu değiştirmez");
            Assert.IsTrue(Baskin.DoAct(state, smoke.Id, 2, 3));
            Assert.AreEqual(2, smoke.Charges);
            Assert.IsTrue(Baskin.Intents(state).Single().Smoked);
            StringAssert.Contains("|s23", Baskin.Signature(state));
            smoke.Charges = 0; smoke.Acted = false;
            Assert.AreEqual(0, Baskin.Targets(state, smoke).Count, "bomba bitince sis atılmaz");
        }

        [Test]
        public void DefaultSquad_HasNoCharges_AndCampaignChargesComeFromParams()
        {
            var plain = Baskin.Create(new RaidParams { Seed = 11 });
            Assert.IsTrue(plain.Units.Where(u => u.Side == 'p').All(u => u.Charges == -1));
            var picked = Baskin.Create(new RaidParams { Seed = 11, DynamiteCharges = 1, SmokeCharges = 4, Squad = new List<SquadSlot>
            {
                new SquadSlot { Type = "tek", Hp = 3 }, new SquadSlot { Type = "maden", Hp = 3 }, new SquadSlot { Type = "duman", Hp = 2 },
            } });
            Assert.AreEqual(1, picked.Units.First(u => u.Type == "maden").Charges);
            Assert.AreEqual(4, picked.Units.First(u => u.Type == "duman").Charges);
            Assert.AreEqual(-1, picked.Units.First(u => u.Type == "tek").Charges);
        }

        /// <summary>Boş tahta (yalnız ikmal kasası), Teknisyen, Madenci ve Sis uzmanı köşelerde; makineler devre dışı.</summary>
        static Baskin.BState SpecialistFixture(out Baskin.Unit tek, out Baskin.Unit miner, out Baskin.Unit smoke)
        {
            var state = Baskin.Create(new RaidParams { Seed = 11, MissionKind = "loot", Squad = new List<SquadSlot>
            {
                new SquadSlot { Type = "tek", Hp = 3 }, new SquadSlot { Type = "maden", Hp = 3 }, new SquadSlot { Type = "duman", Hp = 2 },
            } });
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) state.Cells[y, x] = new Baskin.Cell { T = "floor" };
            state.Cells[3, 3].O = new Baskin.Obj { K = "node", Hp = 4, Max = 4 };
            foreach (var unit in state.Units.Where(u => u.Side == 'e')) unit.Dead = true;
            state.Spawns.Clear();
            tek = state.Units.First(u => u.Type == "tek"); tek.X = 7; tek.Y = 0;
            miner = state.Units.First(u => u.Type == "maden"); miner.X = 7; miner.Y = 7;
            smoke = state.Units.First(u => u.Type == "duman"); smoke.X = 6; smoke.Y = 7;
            return state;
        }

        /// <summary>Tahtaya niyetiyle bir makine koyar (sırası en sonda).</summary>
        static Baskin.Unit Machine(Baskin.BState state, string type, int x, int y, string kind = null, int dir = 0)
        {
            var t = Baskin.UnitTypes[type];
            var u = new Baskin.Unit
            {
                Id = state.NextId++, Type = type, Side = 'e', X = x, Y = y, Hp = t.Hp, Max = t.Hp, Armor = t.Armor,
                Order = 1 + state.Units.Count(v => v.Side == 'e' && !v.Dead), Intent = kind != null ? new Baskin.Intent { Kind = kind, Dir = dir } : null,
            };
            state.Units.Add(u);
            return u;
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
