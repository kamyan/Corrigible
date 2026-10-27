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
        public double liberationLootFunds = 12;
        public double liberationServerFunds = 8;
        public double liberationCaptureFunds = 15;
        /// <summary>Baskında düşen uzmanın yatacağı vardiya (yerine yedek çıkar).</summary>
        public int liberationInjuryDays = 3;
        /// <summary>Baskına çıkmayan vardiyada her uzmanın toparladığı yara (can).</summary>
        public int liberationRestHeal = 1;
        /// <summary>Yedek uzman (acemi) rolün canından bu kadar eksik çıkar.</summary>
        public int liberationRookieHpPenalty = 1;
        /// <summary>
        /// Zorluk kademesi bu kadar özgür bölgeden başlar (kademe 1: 0). Kademe başına baskında bir makine fazla; 3. kademede
        /// makineler kendini kopyalar (turda iki uyanma), 4. kademede makinelerin canı bir fazla; imza düşmanı 3. kademeden iki.
        /// </summary>
        public int liberationTier2At = 2;
        public int liberationTier3At = 6;
        public int liberationTier4At = 10;
        /// <summary>Sivil güven: başlangıç ve tavan. Sıfıra inerse direniş dağılır (harekât kaybedilir).</summary>
        public int liberationTrustStart = 10;
        /// <summary>Baskında kırılan her sivil sunucunun güvene bedeli.</summary>
        public int liberationTrustPerCivilian = 1;
        /// <summary>Özgürleşen her bölgenin geri verdiği güven (tavanı aşmaz).</summary>
        public int liberationTrustPerRegion = 2;
        // YZ alarmı: vardiyalar ve başarısız baskınlar yükseltir, kırılan YZ sunucusu düşürür; eşikte YZ karşı saldırır
        public double liberationAlarmPerShift = 1;
        public double liberationAlarmPerFailure = 3;
        public double liberationAlarmPerServer = 4;
        public double liberationAlarmThreshold = 10;
        /// <summary>Karşı saldırının sivil güvene bedeli (YZ son geri alınan sektörü yeniden işgal eder).</summary>
        public int liberationCounterattackTrust = 1;

        // Sığınak
        public int shelterStartPopulation = 6;
        public double shelterStartSupplies = 24;
        public double shelterStartMaterials = 14;
        public int shelterMaxRoomLevel = 3;
        /// <summary>Oda seviyesi özgür bölgeyle açılır: seviye 2 ve 3 için gereken özgür bölge.</summary>
        public int shelterLevel2Regions = 2;
        public int shelterLevel3Regions = 5;
        public int shelterResidentsPerLevel = 8;
        public int shelterWorkersPerLevel = 2;
        public double shelterSuppliesPerWorker = 1;
        public double shelterSuppliesPerResident = 0.2;
        public double shelterSuppliesStoragePerLevel = 40;
        public double shelterMaterialsStorage = 100;
        public double shelterStarvingWorkMultiplier = 0.5;
        /// <summary>Yaşam alanının her seviyesi bir yedek uzman barındırır.</summary>
        public int shelterReservesPerLevel = 1;
        /// <summary>Atölye: görevli başına vardiyada dökülen zırh plakası, plakanın malzemesi ve seviye başına plaka deposu.</summary>
        public double shelterPlatesPerWorker = 0.25;
        public double shelterPlateMaterials = 4;
        public int shelterPlatesPerLevel = 1;
        /// <summary>Revir: görevli başına vardiyada sarılan yara; en az bu kadar görevliyle yatan uzman bu kadar vardiya erken kalkar.</summary>
        public double shelterHealPerWorker = 0.34;
        public int shelterHealingWorkers = 1;
        public int shelterHealingDays = 1;
        /// <summary>Telsiz odası: görevli başına vardiya alarmından düşülen.</summary>
        public double shelterRadioAlarmPerWorker = 0.05;
        public double shelterLivingFon = 8;
        public double shelterLivingMaterials = 6;
        public double shelterRadioFon = 6;
        public double shelterRadioMaterials = 4;
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
