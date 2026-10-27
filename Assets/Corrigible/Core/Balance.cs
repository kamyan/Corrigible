using System;

namespace Corrigible.Core
{
    /// <summary>
    /// Tüm denge sabitleri. Oyunda BalanceConfig ScriptableObject'i içinde düzenlenir; tasarımcı kod değiştirmeden ayar yapar.
    /// </summary>
    [Serializable]
    public class Balance
    {
        public double startFon = 60;

        // Geri alma harekâtı: baskınlar, gelir, ödüller
        public double liberationRaidCost = 12;
        public double liberationDailyFunds = 2;
        public double liberationRegionIncome = 0.5;
        public double liberationLootFunds = 22;
        public double liberationServerFunds = 10;
        public double liberationCaptureFunds = 30;
        public int liberationInjuryDays = 2;
        public double liberationRaidThreat = 30;
        public double liberationRaidThreatPerRegion = 5;
        /// <summary>Bu kadar bölge özgürleşince makineler kendini kopyalar (baskında daha çok robot).</summary>
        public int liberationCopyFromRegion = 2;

        // Sığınak
        public int shelterStartPopulation = 6;
        public double shelterStartSupplies = 24;
        public double shelterStartMaterials = 14;
        public int shelterMaxRoomLevel = 3;
        public int shelterResidentsPerLevel = 8;
        public int shelterWorkersPerLevel = 2;
        public double shelterSuppliesPerWorker = 1;
        public double shelterSuppliesPerResident = 0.2;
        public double shelterSuppliesStoragePerLevel = 40;
        public double shelterMaterialsStorage = 100;
        public double shelterMaterialsPerWorker = 0.5;
        public double shelterStarvingWorkMultiplier = 0.5;
        public double shelterEnergyPerLevel = 4;
        public double shelterWorkerEnergy = 1;
        public double shelterRoomEnergy = 2;
        public int shelterHealingWorkers = 1;
        public int shelterHealingDays = 1;
        public double shelterLivingFon = 8;
        public double shelterLivingMaterials = 6;
        public double shelterGeneratorFon = 6;
        public double shelterGeneratorMaterials = 4;
        public double shelterWorkshopFon = 10;
        public double shelterWorkshopMaterials = 6;
        public double shelterInfirmaryFon = 12;
        public double shelterInfirmaryMaterials = 8;
        public double shelterLootMaterials = 10;
        public double shelterLootSupplies = 10;
        public int shelterLootResidents = 1;
        public double shelterServerMaterials = 6;

        public Balance Clone() => (Balance)MemberwiseClone();
    }
}
