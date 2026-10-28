using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>Sığınak harcamaları, personel sınırları, plaka dökümü, revir, yedekler, telsiz ve kayıt/devam için regresyonlar.</summary>
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
        public void Personnel_CannotExceedPopulationOrRoomSlots()
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
            Assert.IsFalse(sim.ShelterAssign(ShelterLogic.Radio, 1).Ok, "Nüfus birden çok odada kullanılamaz");
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Infirmary, 0).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Radio, 2).Ok);
            // Seviye 2 iki özgür bölge ister; enerji yok, büyütmeyi yalnız bölge, fon ve malzeme sınırlar
            foreach (var r in sim.S.Liberation.Regions.Take(sim.B.shelterLevel2Regions)) foreach (var s in r.Sectors) s.Captured = true;
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok, "Enerji yok: oda büyütmeyi yalnız fon ve malzeme sınırlar");
        }

        [Test]
        public void Starvation_IsRecoverableAndDoesNotKillResidents()
        {
            var sim = Ready(new Balance { shelterLootResidents = 0 });
            var shelter = sim.Shelter;
            shelter.Supplies = 0;
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Living, 0).Ok);
            int population = shelter.Population;
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.IsTrue(shelter.Starving);
            Assert.AreEqual(population, shelter.Population);
            // Erzak kıt: toplayıcılar açlıkta da çalışır ama bütün nüfusu doyurmaz; ikmal baskınının erzağı açlığı bitirir
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Living, 2).Ok);
            Assert.Less(2 * sim.B.shelterSuppliesPerWorker, population * sim.B.shelterSuppliesPerResident);
            ShelterLogic.RewardLoot(sim.S, sim.B);
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.IsFalse(shelter.Starving);
            Assert.Greater(shelter.Supplies, 0);
            Assert.AreEqual(population, shelter.Population);
        }

        [Test]
        public void Workshop_CastsPlatesFromMaterials_UpToStorage()
        {
            var sim = Ready(new Balance { shelterStartMaterials = 100 });
            var s = sim.Shelter;
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Living, 0).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Workshop, 2).Ok);
            double materials = s.Materials;
            // İki görevli × 0,25: iki vardiyada bir plaka
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.AreEqual(0, s.Plates);
            Assert.AreEqual(0.5, s.PlateProgress, 1e-9);
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.AreEqual(1, s.Plates);
            Assert.AreEqual(materials - sim.B.shelterPlateMaterials, s.Materials, 1e-9);
            // Birinci seviye depo bir plaka: döküm bekler, malzeme harcanmaz
            ShelterLogic.Advance(sim.S, sim.B);
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.AreEqual(1, s.Plates);
            Assert.AreEqual(1, s.PlateProgress, 1e-9, "birikim bir plakada bekler");
            Assert.AreEqual(materials - sim.B.shelterPlateMaterials, s.Materials, 1e-9);
            // Malzeme yoksa da bekler
            s.Plates = 0; s.Materials = 1;
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.AreEqual(0, s.Plates);
            Assert.AreEqual(1, s.Materials, 1e-9);
        }

        [Test]
        public void Infirmary_HealsMostWoundedFirst_AndGetsTheDownedUpEarly()
        {
            var sim = Ready(new Balance { shelterHealPerWorker = 0.5 });
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Infirmary).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Living, 0).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Infirmary, 2).Ok);
            var team = sim.S.Teams[0];
            team.Members[0].Wounds = 1; team.Members[1].Wounds = 2; team.Members[2].Down = 3;
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.AreEqual(1, team.Members[1].Wounds, "en yaralı önce sarılır");
            Assert.AreEqual(1, team.Members[0].Wounds);
            Assert.AreEqual(2, team.Members[2].Down, "yatan uzman bir gün erken kalkar");
            sim.Shelter.Supplies = 0;
            ShelterLogic.Advance(sim.S, sim.B);
            Assert.IsTrue(sim.Shelter.Starving);
            Assert.AreEqual(2, team.Members[1].Wounds + team.Members[0].Wounds, "erzak yokken revir durur");
            Assert.AreEqual(2, team.Members[2].Down);
        }

        [Test]
        public void LivingArea_HoldsOneReservePerLevel()
        {
            var sim = Ready(new Balance { shelterStartMaterials = 100, startFon = 200 });
            Assert.AreEqual(1, sim.Shelter.Reserves.Count);
            foreach (var r in sim.S.Liberation.Regions.Take(sim.B.shelterLevel2Regions)) foreach (var s in r.Sectors) s.Captured = true;
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Living).Ok);
            Assert.AreEqual(2, sim.Shelter.Reserves.Count, "yeni seviye bir yedek getirir");
            Assert.IsTrue(sim.Shelter.Reserves.All(r => r.Down == 0 && r.Role == null));
        }

        [Test]
        public void Radio_SlowsTheShiftAlarm()
        {
            var sim = Ready();
            Assert.AreEqual(sim.B.liberationAlarmPerShift, sim.AlarmPerShift, 1e-9);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Living, 0).Ok);
            Assert.IsTrue(sim.ShelterAssign(ShelterLogic.Radio, 2).Ok);
            Assert.AreEqual(sim.B.liberationAlarmPerShift - 2 * sim.B.shelterRadioAlarmPerWorker, sim.AlarmPerShift, 1e-9);
            sim.Step();
            Assert.AreEqual(sim.AlarmPerShift, sim.S.Alarm, 1e-9);
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
            Assert.AreEqual(sim.Shelter.Plates, restored.Shelter.Plates);
            Assert.AreEqual(sim.Shelter.PlateProgress, restored.Shelter.PlateProgress);
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(4, restored.Record.commands.Count);
        }
    }
}
