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
        public double liberationLootFunds = 10;
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
        /// <summary>Sivil güven: başlangıç. Sıfıra inerse direniş dağılır (harekât kaybedilir).</summary>
        public int liberationTrustStart = 10;
        /// <summary>
        /// Sivil güvenin tavanı (başlangıçtan küçükse başlangıç tavandır). Bot ölçümünde (2026-10-28, 30'ar harekât) 40 iken
        /// kazanılan 27/30 (kapısız), 30/30 (kapıya en iyi cevap); 20 iken 20/30 ve 21/30: birikim son bölgelere yetmez.
        /// </summary>
        public int liberationTrustMax = 20;
        /// <summary>Baskında kırılan her sivil sunucunun güvene bedeli.</summary>
        public int liberationTrustPerCivilian = 1;
        /// <summary>
        /// Kazanılan baskında ayakta kalan her bu kadar sivil sunucu +1 güven verir (0 = ödül yok). Bot ölçümünde (2026-10-28)
        /// baskın başına ~3,6 sivil ayakta kalıyor: 1 olunca güven hiç düşmüyor, 2 olunca harekâtların ~%20'si dağılıyor.
        /// </summary>
        public int liberationCiviliansPerTrust = 2;
        /// <summary>Özgürleşen her bölgenin geri verdiği güven (tavanı aşmaz).</summary>
        public int liberationTrustPerRegion = 3;
        // YZ alarmı: vardiyalar ve başarısız baskınlar yükseltir, kırılan YZ sunucusu düşürür; eşikte YZ karşı saldırır
        public double liberationAlarmPerShift = 1;
        public double liberationAlarmPerFailure = 1;
        public double liberationAlarmPerServer = 4;
        public double liberationAlarmThreshold = 10;
        /// <summary>Karşı saldırının sivil güvene bedeli (YZ son geri alınan sektörü yeniden işgal eder).</summary>
        public int liberationCounterattackTrust = 1;
        /// <summary>
        /// Sunucu görevinin sertliği: düğüm dolabının ek canı (son bölgenin çekirdeği hariç; 6 turda yetişilmez olur) ve tahtada
        /// baştan olan ek makine. Bot ölçümünde (2026-10-28) ek can belirleyici (+2: %89 → %76), ek makine etkisiz.
        /// </summary>
        public int liberationServerTargetHp = 2;
        public int liberationServerMachines = 0;
        /// <summary>
        /// İkmal görevinin ek turu: kasa alındıktan sonra malzeme girişe taşınır (dönüş yolu). Harekât ölçümü (2026-10-28, bot,
        /// 10'ar harekât): ek tur 2, başarısızlık alarmı 1, bölge güveni 3 ile orta oyuncu 5/10, dikkatli 7/10 kazanır; eski
        /// değerlerle (1, 3, 2) 0/10 ve 2/10 (başarısız baskınların alarmı ve uzayan harekâtın sivil kayıpları güveni bitiriyordu).
        /// </summary>
        public int liberationLootExtraTurns = 2;
        /// <summary>Sivil sunucunun canı (kampanyada): gelişmiş akılda ekibe ulaşamayan makine sivillere saldırır, ikinci can
        /// oyuncuya bir tur kurtarma fırsatı verir.</summary>
        public int liberationCivilianHp = 2;
        /// <summary>
        /// Baskın başına hak: Madenci'nin duvar yıkan dinamiti (robota ve nesneye atılan dinamit hak harcamaz) ve Sis uzmanının sis
        /// bombası. Sınırsız dinamit kasa odası ve şerit haritalarının bulmacasını, sınırsız sis taşıyıcı kovalamacasını çökertir.
        /// </summary>
        public int liberationDynamiteCharges = 2;
        public int liberationSmokeCharges = 3;

        // Kapı: birkaç vardiyada bir biri sığınağın kapısını çalar (ziyaretçiler ve etkileri Core/DoorCards.cs)
        /// <summary>İlk vuruşun günü (eğitimden sonra; senaryolu ve dostça).</summary>
        public int doorFirstKnock = 2;
        /// <summary>İki vuruş arası gün (en az–en çok; kapının kendi tohumuyla).</summary>
        public int doorKnockMin = 2;
        public int doorKnockMax = 4;
        /// <summary>Bu günden önce ölümcül ziyaretçi (silahlı sakin, kopya) gelmez.</summary>
        public int doorSafeShifts = 8;
        /// <summary>Aynı ziyaretçi son bu kadar ziyarette yeniden gelmez.</summary>
        public int doorRepeatGap = 4;
        /// <summary>Erzak nüfusun bu katının altındaysa vardiya aç geçer; iki aç vardiyadan sonra sakinlerin heyeti gelir.</summary>
        public double doorHungerSupplies = 1;
        /// <summary>Kopya (kopyalar konusunda uyarıldıysan): alarm eşiğin bu payına varınca ya da karşı saldırıdan sonra bu kadar gün içinde.</summary>
        public double doorCopyAlarm = 0.6;
        public int doorCopyAfterCounterattack = 4;

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
        /// <summary>
        /// Erzak kıttır: tam nüfusta (24) yaşam alanının toplayıcıları tüketimin ancak yarısını karşılar, gerisi ikmal
        /// baskınlarından gelir. Bot ölçümünde (2026-10-28, 30 harekât) erzak ortalama deponun %52'si, vardiyaların ~%20'si aç
        /// (kapının açlık eşiğinin altında), harekâtların yarısında erzak birkaç vardiya tükeniyor; aç sakinlerin heyeti 0–2 kez
        /// geliyor. Önceki değerlerle (toplama 1, tüketim 0,2, ikmal 10, depo 40) erzak hep %95 doluydu, hiç aç vardiya yoktu.
        /// </summary>
        public double shelterSuppliesPerWorker = 0.7;
        public double shelterSuppliesPerResident = 0.35;
        public double shelterSuppliesStoragePerLevel = 30;
        /// <summary>Malzeme deposu: odalar ve plakalar aynı malzemeyi paylaşır; depo dolmaz (ölçümde ortalama 15, eskiden 44).</summary>
        public double shelterMaterialsStorage = 60;
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
        public double shelterLootMaterials = 7;
        public double shelterLootSupplies = 6;
        public int shelterLootResidents = 1;
        public double shelterServerMaterials = 5;

        public Balance Clone() => (Balance)MemberwiseClone();
    }
}
