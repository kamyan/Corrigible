using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>Sığınak harcamaları, personel sınırları ve kayıt/devam için regresyonlar.</summary>
    public class ShelterTests
    {
        static Simulation Ready(Balance b = null) => Simulation.NewGame(79, b);

        [Test]
        public void Repair_PaysOnceAndFailureDoesNotSpendOrRecord()
        {
            var sim = Ready();
            var shelter = sim.Shelter;
            Assert.AreEqual(1, shelter.Room(ShelterLogic.Living).Level);
            Assert.AreEqual(0, shelter.Room(ShelterLogic.Workshop).Level);
            double fon = sim.S.Fon, materials = shelter.Materials;
            uint rng = sim.S.Rng.State;
            var cost = ShelterLogic.UpgradeCost(shelter, ShelterLogic.Workshop, sim.B);
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok);
            Assert.AreEqual(fon - cost.Fon, sim.S.Fon);
            Assert.AreEqual(materials - cost.Materials, shelter.Materials);
            Assert.AreEqual(1, shelter.Room(ShelterLogic.Workshop).Level);
            Assert.AreEqual("shelter_upgrade", sim.Record.commands.Single().kind);
            Assert.AreEqual(rng, sim.S.Rng.State);
            sim.S.Fon = 0;
            materials = shelter.Materials;
            Assert.IsFalse(sim.ShelterUpgrade(ShelterLogic.Infirmary).Ok);
            Assert.AreEqual(0, sim.S.Fon);
            Assert.AreEqual(materials, shelter.Materials);
            Assert.AreEqual(1, sim.Record.commands.Count);
        }

        [Test]
        public void PersonnelAndPower_CannotExceedPopulationSlotsOrGeneratorOutput()
        {
            var sim = Ready(new Balance { shelterStartMaterials = 100 });
            Assert.IsFalse(sim.ShelterAssign(ShelterLogic.Workshop, 1).Ok, "Bozuk oda çalıştırılamaz");
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok);
            Assert.IsFalse(sim.ShelterAssign(ShelterLogic.Workshop, 3).Ok, "Birinci seviye oda iki kişiliktir");
            Assert.IsFalse(sim.ShelterAssign(ShelterLogic.Workshop, -1).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Workshop, 2).Ok);
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Infirmary).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Infirmary, 2).Ok);
            Assert.AreEqual(0, ShelterLogic.AvailableWorkers(sim.Shelter));
            Assert.IsFalse(sim.ShelterAssign(ShelterLogic.Generator, 1).Ok, "Nüfus birden çok odada kullanılamaz");
            Assert.IsFalse(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok, "Enerji yetmeden oda büyütülemez");
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Infirmary, 0).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Generator, 2).Ok);
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok);
            Assert.IsFalse(sim.ShelterAssign(ShelterLogic.Generator, 0).Ok, "Çalışan odaların enerjisi kesilemez");
        }

        [Test]
        public void Starvation_IsRecoverableAndDoesNotKillResidents()
        {
            var sim = Ready();
            var shelter = sim.Shelter;
            shelter.Supplies = 0;
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Living, 0).Ok);
            int population = shelter.Population;
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.IsTrue(shelter.Starving);
            Assert.AreEqual(population, shelter.Population);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Living, 2).Ok);
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.IsFalse(shelter.Starving);
            Assert.Greater(shelter.Supplies, 0);
            Assert.AreEqual(population, shelter.Population);
        }

        [Test]
        public void WorkshopAndInfirmary_ProduceAndShortenTeamRecovery()
        {
            var sim = Ready();
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok);
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Infirmary).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Workshop, 1).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Infirmary, 1).Ok);
            var team = sim.S.Teams[0]; team.Down = 3;
            double materials = sim.Shelter.Materials;
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.AreEqual(materials + sim.B.shelterMaterialsPerWorker, sim.Shelter.Materials);
            Assert.AreEqual(2, team.Down);
            sim.Shelter.Supplies = 0;
            sim.Shelter.Room(ShelterLogic.Living).Workers = 0;
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.AreEqual(2, team.Down, "Erzak yokken revir ek iyileşme vermez");
            Assert.AreEqual(sim.B.shelterMaterialsPerWorker * sim.B.shelterStarvingWorkMultiplier, sim.Shelter.LastMaterialsChange);
        }

        [Test]
        public void Loot_RespectsStorageAndResidentCapacity()
        {
            var sim = Ready();
            var shelter = sim.Shelter;
            shelter.Supplies = ShelterLogic.SuppliesCapacity(shelter, sim.B);
            shelter.Materials = sim.B.shelterMaterialsStorage;
            shelter.Population = ShelterLogic.Capacity(shelter, sim.B);
            uint rng = sim.S.Rng.State;
            ShelterLogic.RewardLoot(sim.S, sim.B);
            Assert.AreEqual(ShelterLogic.SuppliesCapacity(shelter, sim.B), shelter.Supplies);
            Assert.AreEqual(sim.B.shelterMaterialsStorage, shelter.Materials);
            Assert.AreEqual(ShelterLogic.Capacity(shelter, sim.B), shelter.Population);
            Assert.AreEqual(rng, sim.S.Rng.State);
        }

        [Test]
        public void RepairAssignmentAndDailyProduction_RestoreThroughCommandReplay()
        {
            var sim = Ready();
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Workshop, 1).Ok);
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Infirmary).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Infirmary, 1).Ok);
            for (int i = 0; i < 5; i++) sim.Step();
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(sim.Record);
            Assert.AreEqual(sim.S.Day, restored.S.Day);
            Assert.AreEqual(sim.S.Fon, restored.S.Fon);
            Assert.AreEqual(sim.Shelter.Supplies, restored.Shelter.Supplies);
            Assert.AreEqual(sim.Shelter.Materials, restored.Shelter.Materials);
            Assert.AreEqual(sim.Shelter.Population, restored.Shelter.Population);
            foreach (var room in sim.Shelter.Rooms)
            {
                Assert.AreEqual(room.Level, restored.Shelter.Room(room.Id).Level);
                Assert.AreEqual(room.Workers, restored.Shelter.Room(room.Id).Workers);
            }
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(4, restored.Record.commands.Count);
        }
    }
}
