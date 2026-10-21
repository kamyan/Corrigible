using System;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>Sığınak kuralları RNG tüketmez; yalnız özgürleştirme kampanyası tarafından çağrılır.</summary>
    public static class ShelterLogic
    {
        public const string Living = "living", Generator = "generator", Workshop = "workshop", Infirmary = "infirmary";

        public static ShelterState Create(Balance b) => new ShelterState
        {
            Supplies = b.shelterStartSupplies, Materials = b.shelterStartMaterials, Population = b.shelterStartPopulation,
            Rooms = {
                new ShelterRoomState { Id = Living, Level = 1, Workers = Math.Min(b.shelterWorkersPerLevel, b.shelterStartPopulation) },
                new ShelterRoomState { Id = Generator, Level = 1 },
                new ShelterRoomState { Id = Workshop },
                new ShelterRoomState { Id = Infirmary },
            },
        };

        public static string RoomName(string id) => id switch
        {
            Living => Loc.T("Yaşam alanı"), Generator => Loc.T("Jeneratör"),
            Workshop => Loc.T("Atölye"), Infirmary => Loc.T("Revir"), _ => Loc.T("Bilinmeyen oda"),
        };

        public static string RoomDescription(string id) => id switch
        {
            Living => Loc.T("Kurtulanlara yer açar. Görevlendirilen kişiler çevreden erzak toplar."),
            Generator => Loc.T("Çalışan odalara enerji sağlar. Görevlendirilen kişiler enerji kapasitesini artırır."),
            Workshop => Loc.T("Görevlendirilen kişiler her gün inşaat malzemesi üretir."),
            Infirmary => Loc.T("Görevlendirilen kişiler yaralı saha ekiplerinin iyileşmesini hızlandırır."),
            _ => string.Empty,
        };

        public static int Capacity(ShelterState s, Balance b) => Math.Max(0, s.Room(Living)?.Level ?? 0) * b.shelterResidentsPerLevel;
        public static int AvailableWorkers(ShelterState s) => Math.Max(0, s.Population - s.Rooms.Sum(r => r.Workers));
        public static int WorkerCapacity(ShelterRoomState room, Balance b) => Math.Max(0, room.Level) * b.shelterWorkersPerLevel;
        public static double EnergyCapacity(ShelterState s, Balance b)
        {
            var generator = s.Room(Generator);
            return generator == null ? 0 : generator.Level * b.shelterEnergyPerLevel + generator.Workers * b.shelterWorkerEnergy;
        }
        public static double EnergyDemand(ShelterState s, Balance b) => s.Rooms.Where(r => r.Id == Workshop || r.Id == Infirmary).Sum(r => r.Level * b.shelterRoomEnergy);
        public static double SuppliesCapacity(ShelterState s, Balance b) => Math.Max(1, s.Room(Living)?.Level ?? 0) * b.shelterSuppliesStoragePerLevel;

        public static ShelterUpgradeCost UpgradeCost(ShelterState s, string id, Balance b)
        {
            int next = (s.Room(id)?.Level ?? 0) + 1;
            return new ShelterUpgradeCost
            {
                Fon = next * (id == Infirmary ? b.shelterInfirmaryFon : id == Workshop ? b.shelterWorkshopFon : id == Generator ? b.shelterGeneratorFon : b.shelterLivingFon),
                Materials = next * (id == Infirmary ? b.shelterInfirmaryMaterials : id == Workshop ? b.shelterWorkshopMaterials : id == Generator ? b.shelterGeneratorMaterials : b.shelterLivingMaterials),
            };
        }

        public static void Advance(WorldState world, Balance b)
        {
            var s = world.Liberation?.Shelter;
            if (s == null) return;
            double oldSupplies = s.Supplies, oldMaterials = s.Materials;
            var living = s.Room(Living);
            // Erzak toplama açlıkta da sürer; çalışanları yaşam alanına taşıyarak krizden çıkılabilir.
            double incoming = (living?.Workers ?? 0) * b.shelterSuppliesPerWorker;
            s.Supplies = Math.Max(0, Math.Min(SuppliesCapacity(s, b), s.Supplies + incoming - s.Population * b.shelterSuppliesPerResident));
            s.Starving = s.Supplies <= 0;
            double efficiency = s.Starving ? b.shelterStarvingWorkMultiplier : 1;
            if (EnergyCapacity(s, b) >= EnergyDemand(s, b))
            {
                var workshop = s.Room(Workshop);
                s.Materials = Math.Min(b.shelterMaterialsStorage, s.Materials + (workshop?.Workers ?? 0) * b.shelterMaterialsPerWorker * efficiency);
                var infirmary = s.Room(Infirmary);
                if (!s.Starving && infirmary != null && infirmary.Level > 0 && infirmary.Workers >= b.shelterHealingWorkers)
                {
                    foreach (var team in world.Teams)
                        if (team.Down > 0)
                        {
                            team.Down = Math.Max(0, team.Down - b.shelterHealingDays);
                            if (team.Down == 0) team.DownWhy = null;
                        }
                }
            }
            s.LastSuppliesChange = s.Supplies - oldSupplies;
            s.LastMaterialsChange = s.Materials - oldMaterials;
        }

        public static void RewardLoot(WorldState world, Balance b)
        {
            var s = world.Liberation?.Shelter;
            if (s == null) return;
            s.Materials = Math.Min(b.shelterMaterialsStorage, s.Materials + b.shelterLootMaterials);
            s.Supplies = Math.Min(SuppliesCapacity(s, b), s.Supplies + b.shelterLootSupplies);
            s.Population = Math.Min(Capacity(s, b), s.Population + b.shelterLootResidents);
            s.Starving = s.Supplies <= 0;
        }

        public static void RewardServer(WorldState world, Balance b)
        {
            var s = world.Liberation?.Shelter;
            if (s == null) return;
            s.Materials = Math.Min(b.shelterMaterialsStorage, s.Materials + b.shelterServerMaterials);
        }
    }
}
