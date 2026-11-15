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
        /// Aşama 8 bot ölçümü (2026-11-14, 40 tohum): 3. kademe 6 → 7, 4. kademe 10 → 12. 12 bölgelik harekâtta 4. kademe hiç
        /// gelmez (başarısızlığın %30, sivil kaybının baskın başına 2,06 olduğu kademeydi); kod yerinde, buradan geri açılır.
        /// Göstergeler paydayı <c>Simulation.MaxTier</c>'den okur ("TEHDİT n/3"); 11 olursa son bölge 4. kademe olur (ölçülmedi).
        /// </summary>
        public int liberationTier2At = 2;
        public int liberationTier3At = 7;
        public int liberationTier4At = 12;
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
        /// <summary>
        /// Özgürleşen her bölgenin geri verdiği güven (tavanı aşmaz). Aşama 8 bot ölçümü (2026-11-14): 3 → 4. dagilma'nın
        /// kaynağı baskında kırılan siviller (kayıpların %94'ü); ödül tarafı karşılar. Sunumun kuralıyla ölçülen (2026-11-15, iyi
        /// politikalar, 80 tohum): uygulanan dengede dagilma %28; aynı kuralla eski yedi değerde (40 tohum) %62. İlk ölçümün
        /// (toplayıcılı bot) sayısı %51 → %18 idi. 5 de çalışır ama harekâtı uzatır.
        /// </summary>
        public int liberationTrustPerRegion = 4;
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
        /// İkmal görevinin ek turu: kasa alındıktan sonra bütün ekip malzemeyle girişe döner (kaçış, dönüş yolu). Harekât ölçümü (2026-10-28, bot,
        /// 10'ar harekât): ek tur 2, başarısızlık alarmı 1, bölge güveni 3 ile orta oyuncu 5/10, dikkatli 7/10 kazanır; eski
        /// değerlerle (1, 3, 2) 0/10 ve 2/10 (başarısız baskınların alarmı ve uzayan harekâtın sivil kayıpları güveni bitiriyordu).
        /// </summary>
        public int liberationLootExtraTurns = 2;
        /// <summary>
        /// Sunucu görevinin ek turu: hedef kırılınca bütün ekip girişten çıkmalı (kaçış), çıkamayan baskında düşmüş sayılır. Bot
        /// ölçümünde (2026-11-01, 12 tohum) 1 ek turla sunucu başarısı kaçıştan önceki kadar (T+E+A %91, eskiden %90; zayıf
        /// ikililer ±3 puan), 2 ek turla 3–13 puan kolay; ek tursuz T+E+A %78. Aşama 8 harekât ölçümü (2026-11-14, 40 tohum): 1 → 2;
        /// ilk ölçümde (toplayıcılı bot; sunumun kuralıyla yeniden ölçülmedi, tek başına) sunucu başarısızlığı %11 → %6, harekât
        /// başına ölü −1,5, zafer uzunluğu −2,7 vardiya.
        /// </summary>
        public int liberationServerExtraTurns = 2;
        /// <summary>Sivil sunucunun canı (kampanyada): gelişmiş akılda ekibe ulaşamayan makine sivillere saldırır, ikinci can
        /// oyuncuya bir tur kurtarma fırsatı verir.</summary>
        public int liberationCivilianHp = 2;
        /// <summary>
        /// Baskın başına hak: Madenci'nin duvar yıkan dinamiti (robota ve nesneye atılan dinamit hak harcamaz) ve Sis uzmanının sis
        /// bombası. Sınırsız dinamit kasa odası ve şerit haritalarının bulmacasını, sınırsız sis taşıyıcı kovalamacasını çökertir.
        /// </summary>
        public int liberationDynamiteCharges = 2;
        public int liberationSmokeCharges = 3;
        /// <summary>
        /// Bölge olayları (Docs/BOLGE-OLAYLARI.md): her bölgenin tahtasında tur tur işleyen bina içi bir sistem (söndürme bulutu,
        /// kayar raf, yük asansörü…). Kapalıyken baskın yalnız bölge özelliği ve imza makinesiyle oynanır.
        /// </summary>
        public bool liberationRegionEvents = true;
        /// <summary>
        /// Madenci ve Sis uzmanı kadroya atölyenin bu seviyesinde katılır (0: baştan). Atölye seviyesi düşmez; katılan ayrılmaz.
        /// Erken baskınlar T + E + A ile ölçüldü; dinamit ve sis haritanın yeni çözümlerini kademeyle açar.
        /// </summary>
        public int liberationMinerWorkshopLevel = 2;
        public int liberationSmokeWorkshopLevel = 3;

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

        // Kartlı vardiya panosu: insanlar, kişisel öğrenme, stok ve tarifler
        public bool planningBoardEnabled = true;
        public int boardStartResidents = 8;
        public int boardResidentCapacity = 10;
        public int boardBaseHp = 3;
        public int boardBaseMove = 3;
        public int boardBasePower = 1;
        public int boardProfileStatStep = 1;
        public int boardFieldSkillMoveTier = 2;
        public int boardFieldSkillMoveBonus = 1;
        public int boardMaxFieldMove = 5;
        public int boardFieldSkillPowerTier = 3;
        public int boardFieldSkillPowerBonus = 1;
        public int boardMaxFieldPower = 2;
        public int boardFieldSkillRecoveryTier = 1;
        public int boardFieldSkillRecoveryBonus = 1;
        public int boardInjuryTurns = 3;
        public int boardStartMetal = 3;
        public int boardStartCircuits = 2;
        public int boardStartCloth = 2;
        public int boardStartChemicals = 1;
        public double boardStartSupplies = 24;
        public double boardSuppliesCapacity = 60;
        public int boardMaterialsCapacity = 60;
        /// <summary>
        /// Kişi başı vardiya tüketimi (aşçı tasarrufu ayrıca düşülür). Aşama 8 (2026-11-15): 0,35 → 0,40; sekiz kişi ve kademe 1
        /// aşçıyla 8 × 0,40 − 0,7 = 2,5 erzak/vardiya. Yayındaki panoda İkmal ve Bakım yuvası yok (sunum bu görevleri Boşta'ya
        /// indirir); bu kuralla bot ölçümünde (karışık+tecrit, karışık+ucuz, kefilli+ucuz; 120 harekât, olay aralığı 2–3) iyi politikada
        /// `erzak_azaliyor` görülen harekât 0,375'te %28, 0,40'ta %47, 0,425'te %62; terk %0 / %6 / %6 (80 tohumda 0,40: %53, terk %5).
        /// Eğim dik: küçük bir değişiklik (obur, onuncu kişi) açlığı belirgin kaydırır. 0,45'te terk %16, 0,575'te %42 (ilk ölçümün
        /// önerisi 0,575 toplayıcılı botla yapılmıştı: 3,9 − 1,4 = 2,5 net, buradakiyle aynı). Bot yarım tayın ve takas kullanmaz.
        /// </summary>
        public double boardSuppliesPerResident = 0.40;
        public double boardCookSaving = 0.6;
        public double boardCookSavingPerTier = 0.1;
        public double boardGatherSupplies = 1.25;
        public double boardGatherSuppliesPerTier = 0.15;
        public int boardGatherSalvageInterval = 3;
        public int boardGatherMetal = 1;
        public int boardGatherCircuits = 1;
        public double boardCookingMaxSavingFraction = 0.5;
        public int boardRestHeal = 1;
        public int boardCareHeal = 1;
        public int boardCareHealPerTier = 1;
        public int boardMedkitHeal = 1;
        public int boardTrainingGain = 2;
        public int boardWorkGain = 1;
        public int boardCraftSpeedTier = 2;
        public int boardCraftExtraProgress = 1;
        public int boardTrainedAt = 2;
        public int boardExperiencedAt = 6;
        public int boardExpertAt = 12;
        public int boardStartArmor = 2;
        public int boardStartMedkits = 1;
        public int boardEquipmentCapacity = 32;
        public int boardScrapMetal = 1;
        public int boardArmorMetal = 2;
        public int boardArmorCloth = 1;
        public int boardArmorTurns = 1;
        public int boardMedkitCloth = 1;
        public int boardMedkitChemicals = 1;
        public int boardMedkitTurns = 1;
        public int boardEmpMetal = 1;
        public int boardEmpCircuits = 1;
        public int boardEmpTurns = 2;
        public int boardKitMetal = 1;
        public int boardKitCircuits = 1;
        public int boardKitTurns = 1;
        public int boardLootMetal = 3;
        public int boardLootCircuits = 2;
        public int boardLootCloth = 2;
        public int boardLootChemicals = 1;
        public int boardServerMetal = 2;
        public int boardServerCircuits = 2;
        public int boardServerCloth = 1;
        public int boardServerChemicals = 1;
        /// <summary>İkmal baskınının erzakı. Aşama 8 (2026-11-14): 6 → 4 (13 Kasım kararı 2); tüketim artışıyla birlikte "iki ikmal
        /// atlanırsa uyarı". Tek başına etkisizdi; 3 ile terk %24.</summary>
        public double boardLootSupplies = 4;
        public int boardEmpCellCharges = 1;
        public int boardEmpBoostCharges = 1;
        public int boardEmpBoostDamage = 1;

        // Kapı aralığı (olağan grup bu kadar vardiyada bir gelir) ve sabotajın erzak ve malzeme kaybı
        public int admissionKnockMin = 3;
        public int admissionKnockMax = 4;
        public double admissionSabotageRations = 3;
        public int admissionSabotageMaterials = 2;

        // Aday grubu: kapıya 2–3 aday birlikte gelir, en çok biri alınır; şüphe kademesi sızmacı olasılığını belirler
        public int candidateGroupMin = 2;
        public int candidateGroupMax = 3;
        public int candidateWaitShifts = 2;
        public int candidateEmergencyBelow = 3;
        public double candidateTier0Share = 0.30;
        public double candidateTier2Share = 0.30;
        public double candidateTier0Robot = 0.05;
        public double candidateTier1Robot = 0.20;
        public double candidateTier2Robot = 0.45;
        public double candidateTier0CostChance = 0.35;
        // Aday özellikleri: taksitli getiriler, bedeller ve yolluk
        public int perkCircuits = 2;
        public int perkCircuitShifts = 2;
        public double perkSupplies = 6;
        public int perkSupplyShifts = 3;
        public int perkWoundedShifts = 2;
        public int perkSturdyHp = 1;
        public int perkSlowMove = 1;
        public double perkDoubleRationFactor = 2;
        public double dismissSupplies = 2;
        // Gizli iz çekimleri: üretimde çekilir ve saklanır; olaylar (sonraki aşama) okur
        public int traceDelayMin = 2;
        public int traceDelayMax = 6;
        public int secondTraceGapMin = 1;
        public int secondTraceGapMax = 3;
        public double humanTraceChance = 0.20;
        public double humanSecondTraceChance = 0.25;
        // Kit kilidi: eksik rol kiti devresiz (ya da bu kadar devreyle) ve aç kapanışta da üretilir
        public int boardMissingKitCircuits = 0;
        public bool boardStarvingKitProduction = true;

        // Sığınak olayları: kapanışta durumdan tetiklenir, karar kâğıtla verilir. Kapalıyken olay, sızma zinciri, tecrit, kısıt,
        // tayın kipi, karartma, açlıktan ayrılma, ifşa, dusus ve terk sonları çalışmaz (HungryStreak yine sayılır); aday grubu
        // kapısı çalışır; sığınağın kendi sonu yalnız topluluk, son şalter (salter) çalışır ve tahliye nedeni yalnız güven olabilir.
        // Kısıtı ya da kapı dışı yollamayı kuran elle yol yoktur (aşama 7).
        // Oyun hep açık oynanır; anahtar testlerin olaysız kapanış dizileri içindir.
        public bool shelterEventsEnabled = true;
        /// <summary>Olağan olaylar açılıştan açılışa bu kadar vardiya bekler (açılışta iki çekim: aralık ve hedef). Aşama 8: 2–3 kaldı.
        /// Üst sınır 2 denendi (ilk ölçümün toplayıcılı botuyla olay/harekât 12,6 → 14,4), ama sunumun kuralıyla (erzak olayları
        /// da açılır; iyi politikalar, 80 tohum) 2'de 16,6, 3'te 14,7 (hedef 12–15); zafer %68 / %64, gürültü içinde.</summary>
        public int eventGapMin = 2;
        public int eventGapMax = 3;
        // Tecrit ve sızma zinciri
        public int isolationShifts = 3;
        public int isolationCapacity = 1;
        /// <summary>İkinci iz kâğıdından sonra sabotajdan önce geçen tam vardiya.</summary>
        public int interventionShifts = 1;
        public double isolationFindChance = 0.60;
        public double isolationFindChanceTwo = 0.85;
        public int eventRestrictShifts = 3;
        public int eventRestrictLongShifts = 5;
        // İfşa ve sonlar
        public int exposureIgnoredTrace = 1;
        public int exposureSabotage = 2;
        public int exposureInterrogation = -1;
        /// <summary>İfşa eşiği (sayaç tavansız): eşiğe ulaşan kapanış "konum çözüldü" der; uyarı süresinin sonundaki kapanışta
        /// ifşa hâlâ eşikteyse sığınak düşer (dusus). Sıfır ya da altı: dusus kapalı.</summary>
        public int exposureThreshold = 6;
        /// <summary>Eşiğe bir kararla (görmezden gelinen iz) varılırsa "konum çözüldü" uyarısının süresi (vardiya; sığınak bu
        /// sayıncı kapanışta düşer). Sabotajla varılan eşikte uyarı hep bir vardiyadır. 1'in altı 1 sayılır.</summary>
        public int exposureDecisionWarnShifts = 2;
        /// <summary>Teleks uyarı eşikleri (sunum okur).</summary>
        public int exposureWarnAt1 = 2;
        public int exposureWarnAt2 = 4;
        /// <summary>İfşa bu değer ve üstündeyken YZ alarmı vardiyada <see cref="exposureAlarmBonus"/> fazla artar; sıfır: ara bedel yok.</summary>
        public int exposureAlarmAt = 3;
        public double exposureAlarmBonus = 0.5;
        /// <summary>terk: ayrılma yaşanmış sığınakta, kapıda acil grup varken canlı sayısı acil eşiğinin altında kapanan üst üste
        /// vardiya. Sıfır: terk kapalı.</summary>
        public int abandonUnderstaffedShifts = 3;
        /// <summary>Son şalterin en erken vardiyası (VARDİYA sayacı, gün + 1).</summary>
        public int lastSwitchMinShift = 5;
        /// <summary>Güven bunun altındayken çekilen şalter tahliye sayılır.</summary>
        public int evacuationTrustBelow = 2;
        /// <summary>Temiz zafer: ölüm sınırının tabanı (harekât uzunluğuyla büyür, bkz. <see cref="victoryCleanDeathShifts"/>), ifşa en çok,
        /// harekât boyunca en düşük güven en az.</summary>
        public int victoryCleanMaxDeaths = 0;
        /// <summary>Temiz zaferin ölüm sınırı her bu kadar vardiyada (VARDİYA sayacı, gün + 1) bir artar; sıfır ya da altı: sınır sabit.
        /// Varsayılan 15 (aşama 8 bot ölçümü): 30–44. vardiyada 2, 45–59'da 3, 60–74'te 4.</summary>
        public int victoryCleanDeathShifts = 15;
        /// <summary>Gölgeli zafer: içeride en az bu kadar izi görmezden gelinmiş, ortaya çıkmamış bir sızmacı kaldıysa (1'in altı 1).</summary>
        public int victoryShadowIgnoredTraces = 1;
        public int victoryCleanMaxExposure = 2;
        public int victoryCleanMinTrust = 5;
        // Açlık ve tayın
        public int desertStreak = 3;
        public int eventDeserterStreak = 2;
        public int rationWarnShifts = 3;
        public int halfRationShifts = 4;
        public int halfRationRaidHp = 1;
        public double halfRationFactor = 0.5;
        // Hırsızlık
        public double theftSupplies = 2;
        public double bigTheftSupplies = 5;
        public double bigTheftChance = 0.35;
        public int bigTheftDelayMin = 3;
        public int bigTheftDelayMax = 5;
        // Ağır yaralı
        public double reviveSupplies = 3;
        public double woundWorsenChance = 0.25;
        public int reviveHp = 2;
        // Kavga
        public int feudShifts = 2;
        public double feudInjuryChance = 0.50;
        // Anma
        public double memorialSupplies = 2;
        public double griefChance = 0.40;
        public int memorialTrust = 1;
        // Karartma
        public double blackoutAlarm = 2;
        public double blackoutAlarmMargin = 2;
        // Takas
        public int tradeMetal = 2;
        public int tradeCircuits = 1;
        public double tradeMetalSupplies = 5;
        public double tradeSupplies = 4;
        public double tradeLowSupplies = 8;
        public double tradeHighSupplies = 12;
        // Çağrı
        public double callSupplies = 4;
        public double callRefuseChance = 0.50;
        public double callMinSupplies = 12;
        public int callTrust = 2;
        // Konvoy
        public double convoySupplies = 5;
        public double convoyAlarm = 2;
        public int convoyTrustAt = 15;
        // Olayların harekât başına tekrar sınırı ve iki açılışı arasındaki bekleme (vardiya)
        public int eventBigTheftMax = 2;
        public int eventBigTheftCooldown = 0;
        public int eventDeserterMax = 3;
        public int eventDeserterCooldown = 3;
        public int eventLowRationMax = 3;
        public int eventLowRationCooldown = 5;
        public int eventTheftMax = 2;
        public int eventTheftCooldown = 6;
        public int eventWoundedMax = 3;
        public int eventWoundedCooldown = 4;
        public int eventFeudMax = 2;
        public int eventFeudCooldown = 6;
        public int eventMemorialMax = 3;
        public int eventMemorialCooldown = 4;
        public int eventBlackoutMax = 3;
        public int eventBlackoutCooldown = 5;
        public int eventTradeMax = 3;
        public int eventTradeCooldown = 5;
        public int eventCallMax = 2;
        public int eventCallCooldown = 8;
        public int eventConvoyMax = 2;
        public int eventConvoyCooldown = 8;

        public Balance Clone() => (Balance)MemberwiseClone();
    }
}
