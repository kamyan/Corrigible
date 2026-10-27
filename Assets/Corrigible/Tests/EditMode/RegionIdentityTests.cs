using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Bölge kimliği ve zorluk eğrisi: her bölgenin kendine özgü özelliği ve imza düşmanı, kademe eşikleri, özelliklerin tahtaya
    /// etkisi, altı yeni makinenin niyetleri ve sığınak odalarının özgür bölgeyle açılması.
    /// </summary>
    public class RegionIdentityTests
    {
        static RaidParams P(int seed, string trait = null, string signature = null, int tier = 1, string kind = "server") => new RaidParams
        {
            Seed = seed, MissionKind = kind, Tier = tier, Signature = signature,
            Traits = trait == null ? new List<string>() : new List<string> { trait },
        };

        static int Count(Baskin.BState B, string objectKind)
        {
            int n = 0;
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (B.Cells[y, x].O?.K == objectKind) n++;
            return n;
        }

        static int Cells(Baskin.BState B, string type)
        {
            int n = 0;
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (B.Cells[y, x].T == type) n++;
            return n;
        }

        /// <summary>Boş bir tahta: her yer zemin, makineler devre dışı; ekip köşelerde.</summary>
        static Baskin.BState Empty()
        {
            var B = Baskin.Create(new RaidParams { Seed = 11, MissionKind = "server" });
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) B.Cells[y, x] = new Baskin.Cell { T = "floor" };
            B.Cells[0, 7].O = new Baskin.Obj { K = "node", Hp = 4, Max = 4 };
            foreach (var u in B.Units.Where(u => u.Side == 'e')) u.Dead = true;
            var team = B.Units.Where(u => u.Side == 'p').ToArray();
            team[0].X = 0; team[0].Y = 0; team[1].X = 0; team[1].Y = 7; team[2].X = 7; team[2].Y = 7;
            B.Spawns.Clear();
            return B;
        }

        static Baskin.Unit Machine(Baskin.BState B, string type, int x, int y, string kind, int dir)
        {
            var T = Baskin.UnitTypes[type];
            var u = new Baskin.Unit { Id = B.NextId++, Type = type, Side = 'e', X = x, Y = y, Hp = T.Hp, Max = T.Hp, Armor = T.Armor, Order = 1,
                Intent = new Baskin.Intent { Kind = kind, Dir = dir } };
            B.Units.Add(u);
            return u;
        }

        [Test]
        public void EveryRegion_HasOwnTrait_AndEverySignatureMachineGuardsTwoRegions()
        {
            var traits = GameData.Regions.Select(r => r.Trait).ToList();
            CollectionAssert.AllItemsAreUnique(traits, "her bölgenin özelliği kendine");
            CollectionAssert.IsSubsetOf(traits, Baskin.RegionTraits.Keys);
            foreach (var group in GameData.Regions.GroupBy(r => r.Enemy))
            {
                Assert.AreEqual(2, group.Count(), group.Key);
                Assert.AreEqual('e', Baskin.UnitTypes[group.Key].Side);
                Assert.IsNotEmpty(Baskin.UnitTypes[group.Key].Desc);
            }
            Assert.AreEqual(6, GameData.Regions.Select(r => r.Enemy).Distinct().Count());
        }

        [Test]
        public void Tier_RisesWithLiberatedRegions()
        {
            var sim = Simulation.NewGame(3);
            CollectionAssert.AreEqual(new[] { 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4 }, Enumerable.Range(0, 12).Select(sim.RaidTier).ToArray());
        }

        [Test]
        public void RaidOrder_CarriesRegionTraitSignatureAndTier()
        {
            var sim = Simulation.NewGame(3, new Balance { startFon = 100 });
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            var p = sim.S.Raid.Params;
            CollectionAssert.AreEqual(new[] { "guvenli" }, p.Traits, "kademe 1: yalnız bölge özelliği");
            Assert.AreEqual("zdron", p.Signature);
            Assert.AreEqual(1, p.Tier);
        }

        [Test]
        public void Tier_AddsMachines_CopyAndStrength()
        {
            int Enemies(int tier) => Baskin.Create(P(21, null, null, tier)).Units.Count(u => u.Side == 'e');
            int baseCount = Enemies(1);
            Assert.AreEqual(baseCount + 1, Enemies(2));
            Assert.AreEqual(baseCount + 3, Enemies(4));
            var strong = Baskin.Create(new RaidParams { Seed = 21, Tier = 4, Traits = new List<string> { "kopya", "guclu" } });
            Assert.IsTrue(strong.Units.Where(u => u.Side == 'e').All(u => u.Max == Baskin.UnitTypes[u.Type].Hp + 1), "4. kademede makinelerin canı bir fazla");
            Assert.AreEqual(2, strong.SpawnPerTurn);
        }

        [Test]
        public void Signature_ReplacesOneMachine_TwoFromTierThree()
        {
            Assert.AreEqual(1, Baskin.Create(P(31, null, "havan", 1)).Units.Count(u => u.Type == "havan"));
            Assert.AreEqual(2, Baskin.Create(P(31, null, "havan", 3)).Units.Count(u => u.Type == "havan"));
            var armored = Baskin.Create(P(31, null, "zdron", 1)).Units.Single(u => u.Type == "zdron");
            Assert.AreEqual(1, armored.Armor, "zırhlı dron zırhla başlar");
        }

        [Test]
        public void BoardTraits_ChangeTheBoard_WithoutClosingThePath()
        {
            var plain = Baskin.Create(P(41));
            Assert.AreEqual(6, plain.MaxTurns);
            Assert.AreEqual(5, Baskin.Create(P(41, "kisa")).MaxTurns);
            Assert.IsNotEmpty(plain.Spawns);
            Assert.IsEmpty(Baskin.Create(P(41, "guvenli")).Spawns, "ilk tur uyanma yok");
            Assert.IsTrue(Baskin.Create(P(41, "sis")).Fog);
            Assert.AreEqual(Baskin.TargetsLeft(plain)[0].O.Max + 1, Baskin.TargetsLeft(Baskin.Create(P(41, "zirhli")))[0].O.Max);
            Assert.AreEqual(Count(plain, "civ") + 2, Count(Baskin.Create(P(41, "kalabalik")), "civ"));
            Assert.AreEqual(Count(plain, "cool") + 2, Count(Baskin.Create(P(41, "sogutma")), "cool"));
            Assert.AreEqual(Count(plain, "spawner") + 1, Count(Baskin.Create(P(41, "fabrika")), "spawner"));
            Assert.AreEqual(Cells(plain, "cable") + 3, Cells(Baskin.Create(P(41, "sebeke")), "cable"));
            Assert.AreEqual(Cells(plain, "wall") + 3, Cells(Baskin.Create(P(41, "dar")), "wall"));
            Assert.AreEqual(Cells(plain, "wall") - 3, Cells(Baskin.Create(P(41, "acik")), "wall"));
            Assert.AreEqual(plain.Units.Count(u => u.Side == 'e') + 1, Baskin.Create(P(41, "takviye")).Units.Count(u => u.Side == 'e'));
            var patrol = Baskin.Create(P(41, "devriye"));
            var drone = patrol.Units.First(u => u.Side == 'e');
            Assert.AreEqual(1, patrol.EnemyMove);
            Assert.Greater(Baskin.Reachable(patrol, drone).Count, Baskin.Reachable(plain, plain.Units.First(u => u.Id == drone.Id)).Count);
            // Duvar ve nesne eklenince de ekip girişinden hedefe yol kalır
            foreach (var trait in new[] { "dar", "kalabalik", "sogutma", "fabrika" })
                for (int seed = 1; seed <= 20; seed++)
                {
                    var B = Baskin.Create(P(seed, trait));
                    var tek = B.Units.First(u => u.Type == "tek");
                    Assert.IsTrue(Reaches(B, tek), trait + " " + seed);
                }
        }

        /// <summary>Teknisyen hedef dolabın bitişiğine yürüyerek varabilir mi (makineler yok sayılır).</summary>
        static bool Reaches(Baskin.BState B, Baskin.Unit from)
        {
            var seen = new HashSet<(int, int)> { (from.X, from.Y) };
            var q = new Queue<(int X, int Y)>(); q.Enqueue((from.X, from.Y));
            var goals = Baskin.TargetsLeft(B).SelectMany(t => Baskin.Dirs.Select(d => (t.X + d[0], t.Y + d[1]))).ToHashSet();
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                if (goals.Contains(p)) return true;
                foreach (var d in Baskin.Dirs)
                {
                    int x = p.X + d[0], y = p.Y + d[1];
                    if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N || !seen.Add((x, y))) continue;
                    if (B.Cells[y, x].T == "wall" || B.Cells[y, x].O != null) continue;
                    q.Enqueue((x, y));
                }
            }
            return false;
        }

        [Test]
        public void Mortar_LobsThreeTilesOverWalls()
        {
            var B = Empty();
            var tek = B.Units.First(u => u.Type == "tek");
            tek.X = 3; tek.Y = 3;
            B.Cells[3, 2].T = "wall";
            Machine(B, "havan", 0, 3, "lob", 1);
            int hp = tek.Hp;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(hp - 1, tek.Hp, "duvarın üstünden üç kare öteye");
        }

        [Test]
        public void Bomber_Explodes_HittingFourNeighbours_AndDies()
        {
            var B = Empty();
            var tek = B.Units.First(u => u.Type == "tek");
            var emp = B.Units.First(u => u.Type == "emp");
            tek.X = 3; tek.Y = 2; emp.X = 4; emp.Y = 3;
            B.Cells[3, 2].O = new Baskin.Obj { K = "civ", Hp = 1, Max = 1 };
            var bomb = Machine(B, "bomba", 3, 3, "blast", 0);
            int t = tek.Hp, e = emp.Hp;
            var area = Baskin.Intents(B).Single(i => i.Unit == bomb.Id).Area;
            Assert.AreEqual(4, area.Count);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(t - 1, tek.Hp);
            Assert.AreEqual(e - 1, emp.Hp);
            Assert.AreEqual(1, B.Collateral, "patlama sivil sunucuyu da kırar");
            Assert.IsTrue(bomb.Dead);
        }

        [Test]
        public void Locker_StopsTheSpecialistMovingNextTurn_ButNotActing()
        {
            var B = Empty();
            var tek = B.Units.First(u => u.Type == "tek");
            tek.X = 3; tek.Y = 3;
            Machine(B, "kilit", 0, 3, "lock", 1);
            int hp = tek.Hp;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(hp, tek.Hp, "kilit hasar vermez");
            Assert.IsTrue(tek.Locked);
            Assert.IsFalse(Baskin.CanMove(B, tek));
            Assert.IsTrue(Baskin.CanAct(B, tek));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsFalse(tek.Locked, "kilit bir tur sürer");
            Assert.IsTrue(Baskin.CanMove(B, tek));
        }

        [Test]
        public void ShieldDrone_ArmorsTheMachineNextToIt()
        {
            var B = Empty();
            var guard = Machine(B, "bakim", 4, 4, "repair", 0);
            Machine(B, "kalkan", 3, 4, "shield", 1);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(1, guard.Armor);
            Assert.IsTrue(B.FxList.Any(f => f.K == "shield" && f.Unit == guard.Id));
        }

        [Test]
        public void Replicator_WakesADroneNextToIt()
        {
            var B = Empty();
            Machine(B, "cogalt", 4, 4, "clone", 1);
            int drones = B.Units.Count(u => !u.Dead && u.Type == "dron");
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(drones + 1, B.Units.Count(u => !u.Dead && u.Type == "dron"), "yeni dron uyanır (ve hemen hamlesini planlar)");
            Assert.IsTrue(B.FxList.Any(f => f.K == "spawn" && f.X == 5 && f.Y == 4));
        }

        [Test]
        public void MachinePlanning_PicksOwnIntentKinds()
        {
            for (int seed = 1; seed <= 10; seed++)
                foreach (var type in new[] { "havan", "bomba", "kilit", "kalkan", "cogalt", "zdron" })
                {
                    var B = Baskin.Create(P(seed, null, type, 3));
                    foreach (var i in Baskin.Intents(B))
                    {
                        var u = B.Units.First(x => x.Id == i.Unit);
                        CollectionAssert.Contains(Baskin.UnitTypes[u.Type].Kinds, i.Kind, u.Type);
                    }
                }
        }

        [Test]
        public void ShelterLevels_OpenWithLiberatedRegions()
        {
            var sim = Simulation.NewGame(5, new Balance { startFon = 500, shelterStartMaterials = 100 });
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok, "onarım (seviye 1) açık");
            Assert.AreEqual(Loc.F("Seviye {0} için {1} özgür bölge gerekiyor (şimdi {2}).", 2, sim.B.shelterLevel2Regions, 0),
                sim.ShelterUpgradeCheck(ShelterLogic.Workshop));
            Assert.IsFalse(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok);
            foreach (var r in sim.S.Liberation.Regions.Take(sim.B.shelterLevel2Regions)) foreach (var s in r.Sectors) s.Captured = true;
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok, "iki özgür bölgede seviye 2");
            StringAssert.Contains(sim.B.shelterLevel3Regions.ToString(), sim.ShelterUpgradeCheck(ShelterLogic.Workshop));
        }
    }
}
