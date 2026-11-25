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
        /// Tek kip denge turu (2026-11-22, Docs/TESIS-BASKINLARI-TASARIMI.md §11.6 "Tek kip denge ölçümü"): 3. kademe 7 → 9; tesiste
        /// 3. kademe sektörü 2. kademenin ~1,5 katı vardiya tutuyordu (çıkarınca bot zaferi %55 → %45).
        /// </summary>
        public int liberationTier2At = 2;
        public int liberationTier3At = 9;
        public int liberationTier4At = 12;
        /// <summary>
        /// Harekât haritası: 0 bugünkü dünya haritası (sabit 12 bölge ve 18 bağlantı, <c>GameData.Links</c>), 1 her yeni harekâtta
        /// tohumdan üretilen harita (<c>WorldMap</c>: kıtalar, bölge şekilleri, komşuluk ve adlar üretilir; 12 kimlik paketi
        /// düğümlere dağıtılır, karargâh hep "weu"). Kayıt bu değeri taşır; harita kayda yazılmaz, tohumdan yeniden üretilir.
        /// Sunum üretilmiş haritayı çizene kadar oyunda 0.
        /// </summary>
        public int liberationMapGen = 0;
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
        // YZ alarmı: vardiyalar ve başarısız baskınlar yükseltir, kırılan YZ sunucusu düşürür; eşikte YZ karşı saldırır.
        // Tek kip denge turu (2026-11-22): vardiya payı 1 → 0,5 (kuşatmaların %83'ü vardiya alarmındandı; çıkarınca bot zaferi %55 → %42,5)
        public double liberationAlarmPerShift = 0.5;
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
        /// <summary>Sivil sunucunun canı (kampanyada): gelişmiş akılda ekibe ulaşamayan makine sivillere saldırır, fazla can
        /// oyuncuya bir tur kurtarma fırsatı verir. Tek kip denge turu (2026-11-22): 2 → 3. Bakım robotunun yakın saldırısı 2 vurur;
        /// 2 canda sivili tek turda kırıyor, "bir tur" niyeti işlemiyordu. Tesiste baskın başına kırılan sivil 2,32 → ~1,6, dağılma
        /// (güven 0) sonların %74'ünden ~%15'e.</summary>
        public int liberationCivilianHp = 3;
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
        // ---- Tesis baskınları (Docs/TESIS-BASKINLARI-TASARIMI.md): baskın kanat başına üç odalı tesis (giriş, ara oda, hedef oda)
        // olarak oynanır. Oyunun tek baskın kipidir (kayıt 21; eski tek tahta kaldırıldı). Aşağıdaki facility* değerleri tesisin
        // ayarları ve alt bayraklarıdır.
        /// <summary>Tesisin giriş odasının tur sayısı. Sürüm 10 telafisi (2026-11-24): 5 → 6 (döndürülmüş odalarla simetrik geçit zaferi
        /// %45'ten %31'e düşürdü; G/A/dönüş 6 ve hedef 10 ile %43, Docs/TESIS-BASKINLARI-TASARIMI.md §14.x).</summary>
        public int facilityEntryTurns = 6;
        /// <summary>Tesisin ara odasının tur sayısı. Sürüm 10 telafisi: 5 → 6.</summary>
        public int facilityMiddleTurns = 6;
        /// <summary>Tesisin hedef odasının tur sayısı (kısa nöbet bölgesinde bir tur kısa). Tek kip denge turu (2026-11-22): 8 → 9;
        /// başarısızlığın çoğu süre dolmasıydı ve hedef odada kaçışın süresi dolunca geride kalan ölüyordu (çıkarınca bot zaferi
        /// %55 → %42,5). Sürüm 10 telafisi (2026-11-24): 9 → 10.</summary>
        public int facilityTargetTurns = 10;
        /// <summary>Giriş odasının başlangıç makineleri: bir bakım robotu, kalanı dron. Aşama 4 kararı (2026-11-18): 2 → 1 (yalnız bakım
        /// robotu; Docs/TESIS-BASKINLARI-TASARIMI.md §11.6 "Aşama 4 ölçümü", §14 yedinci tur).</summary>
        public int facilityEntryMachines = 1;
        /// <summary>Ara odanın başlangıç makineleri: bir bakım robotu, kalanı dron. Aşama 4 kararı: 3 → 1.</summary>
        public int facilityMiddleMachines = 1;
        /// <summary>Hedef odanın başlangıç makineleri: bir bakım robotu, kalanı dron; kademe ekleri ve imza dönüşümü bunlara eklenir.</summary>
        public int facilityTargetMachines = 3;
        /// <summary>Sigorta panosunun canı (giriş odasının donanımı).</summary>
        public int facilityPanelHp = 2;
        /// <summary>Çağrı anteninin canı (giriş ya da ara odanın donanımı).</summary>
        public int facilityAntennaHp = 2;
        /// <summary>Soğutma vanasının canı (ara odanın donanımı).</summary>
        public int facilityValveHp = 1;
        /// <summary>Dönüş odasının (kaçışta batıya geri geçilen ara ya da giriş odası) tur sayısı. Aşama 4 kararı: 4 → 5 (geride kalan
        /// ve ekibin tamamının öldüğü baskın azalır). Sürüm 10 telafisi (2026-11-24): 5 → 6.</summary>
        public int facilityReturnTurns = 6;
        /// <summary>Sigorta panosu kırıkken hedef odasında kopya dolaplarının makine uyandırmadığı ilk tur sayısı.</summary>
        public int facilityPanelQuietTurns = 2;
        /// <summary>Çağrı anteni kırıkken hedef odasına eksik gelen makine (önce kademe ekinden, yetmezse başlangıç dronundan).</summary>
        public int facilityAntennaMachines = 1;
        /// <summary>Soğutma vanası kapalıyken sunucu görevinde hedef dolabın eksik canı (en az 1 kalır).</summary>
        public int facilityValveTargetHp = 2;
        /// <summary>Soğutma vanası kapalıyken ikmal görevinde hedef odasının makinelerinin yürümediği ilk tur sayısı.</summary>
        public int facilityValveStillTurns = 1;
        /// <summary>Tesis hamleleri (aşama 6; Docs/TESIS-BASKINLARI-TASARIMI.md §4): pusu hattının boyu (kurulduğu kareden).</summary>
        public int facilityAmbushRange = 4;
        /// <summary>Koşu darbesinin en kısa koşusu (kare; üst sınır uzmanın yürüyüşü).</summary>
        public int facilityRushRun = 3;
        /// <summary>Koşu darbesinin makineyi aynı yönde itmesi (kare).</summary>
        public int facilityRushPush = 2;
        /// <summary>Yükün fırlatılabildiği en uzak kare (düz hat).</summary>
        public int facilityThrowRange = 3;
        /// <summary>Düşen kişiyi sürükleyen uzmanın yürüyüş kaybı (o tur yeteneği de yoktur).</summary>
        public int facilityDragMoveCost = 1;
        /// <summary>İkmal hedef odasının büyük sandığı (iki kişilik iş) açılan kazanılmış baskında ek metal ve devre (kartlı oyun).</summary>
        public int facilityBigCrateMetal = 2, facilityBigCrateCircuits = 1;
        /// <summary>
        /// "Son nefes" kuralını kapatır (yalnız ölçüm için; oyunda false). Kural: tesiste oyuncunun hamle sırası dışında (makine
        /// evresi, tur sonu olay, kablo, gaz) düşen uzman, oda oyuncu bir daha hamle yapamadan biterse (kendiliğinden geçiş, süre ya da
        /// kaçış) ve ekipten biri ayakta çıktıysa ekiple gider (Docs/TESIS-BASKINLARI-TASARIMI.md §4.3). Ad olumsuz: kayıtta ya da
        /// asset'te alan yoksa false gelir ve kural açık kalır (serileştiricinin eksik alanı nasıl doldurduğuna bağlı değil).
        /// </summary>
        public bool facilityLastBreathDies = false;
        /// <summary>Dinleme nöbeti: bir odanın bina planını açan puan (aşama 5; Docs/TESIS-BASKINLARI-TASARIMI.md §7.2). Geliştirici
        /// (2026-11-24): "3 vardiyada bir harita değil, 1 vardiyada bir oda haritası": 3 → 1 (Gözetim'deki kişi her kapanışta bir paftayı
        /// açar; Kulak'la iki). Kayıt dengeyi taşır: eski oyunlar eski bedelle sürer.</summary>
        public int facilityIntelCost = 1;
        /// <summary>Dinleme nöbeti: Gözetim görevindeki kişinin her vardiya kapanışında ürettiği puan.</summary>
        public int facilityWatchIntel = 1;
        // ---- Tesis aşama 8: ekipman, tarifler ve kazanımlar (Docs/TESIS-BASKINLARI-TASARIMI.md §5, §6.1–6.2, §11.1). Kimlikler ve
        // tablolar Core/FacilityGear.cs'te; buradaki sayılar asset'e yazılmaz, C# varsayılanıyla çalışır (aşama 6 emsali).
        /// <summary>Örme setinin ve dinamit lokumunun baskın başına hakkı (Yedek çanta kazanımı ikisine de ekler).</summary>
        public int facilityWireCharges = 2, facilityChargeCharges = 2;
        /// <summary>Tel örgünün ve kalas duvarın canı.</summary>
        public int facilityWireHp = 3, facilityPlankHp = 3;
        /// <summary>Kalas duvarın örme setinden düştüğü hak (tel 1).</summary>
        public int facilityPlankCost = 2;
        /// <summary>Süresi biten ya da kırılan aygıtın karesinde kalan hurdanın canı.</summary>
        public int facilityScrapHp = 1;
        /// <summary>Aygıtın (taret, yem, vinç) çalıştığı makine evresi ve canı.</summary>
        public int facilityDeviceTurns = 3, facilityDeviceHp = 2;
        /// <summary>Taretin ve vincin hattı (kare, pusu emsali).</summary>
        public int facilityTurretRange = 4, facilityWinchRange = 4;
        /// <summary>Vincin bir evrede çektiği kare ve yemin bir evrede yürüdüğü kare.</summary>
        public int facilityWinchPull = 1, facilityDecoyStep = 2;
        /// <summary>Ele geçirilen makinenin oyuncu için oynadığı tur.</summary>
        public int facilityClampTurns = 3;
        /// <summary>Köpeğin canı, yürüyüşü ve düşünce panoda kullanılamadığı vardiya.</summary>
        public int facilityDogHp = 2, facilityDogMove = 5, facilityDogRestTurns = 3;
        /// <summary>Havlamanın erimi (Manhattan; makinenin plan öncesi karesinden).</summary>
        public int facilityBarkRange = 3;
        /// <summary>Örme seti tarifi: metal, kumaş, vardiya.</summary>
        public int facilityWireMetal = 2, facilityWireCloth = 1, facilityWireShifts = 1;
        /// <summary>Dinamit lokumu tarifi: metal, kimyasal, vardiya.</summary>
        public int facilityChargeMetal = 1, facilityChargeChemicals = 2, facilityChargeShifts = 2;
        /// <summary>Yaylı taret tarifi: metal, devre, vardiya.</summary>
        public int facilityTurretMetal = 2, facilityTurretCircuits = 1, facilityTurretShifts = 2;
        /// <summary>Yürüyen yem tarifi: metal, devre, kumaş, vardiya.</summary>
        public int facilityDecoyMetal = 1, facilityDecoyCircuits = 1, facilityDecoyCloth = 1, facilityDecoyShifts = 1;
        /// <summary>Makaralı vinç tarifi: metal, kumaş, vardiya.</summary>
        public int facilityWinchMetal = 2, facilityWinchCloth = 1, facilityWinchShifts = 1;
        /// <summary>Kumanda kelepçesi tarifi: devre, metal, vardiya.</summary>
        public int facilityClampCircuits = 2, facilityClampMetal = 1, facilityClampShifts = 2;
        /// <summary>Köpek eğitiminin erzak bedeli (iş emri başlarken düşer; yetmezse emir bekler).</summary>
        public double facilityDogSupplies = 4;
        /// <summary>Köpek eğitimi tarifi: kumaş, vardiya.</summary>
        public int facilityDogCloth = 1, facilityDogShifts = 3;
        /// <summary>Kulak: Gözetim görevlisinin vardiya başına ek dinleme puanı.</summary>
        public int facilityEarIntel = 1;
        /// <summary>Yedek çanta: örme setinin ve dinamit lokumunun ek baskın hakkı.</summary>
        public int facilitySpareCharges = 1;
        /// <summary>Açık hat: dönüş odasının ek turu. Eşik: gidişteki giriş odasının ek turu.</summary>
        public int facilityOpenLineTurns = 1, facilityThresholdTurns = 1;
        /// <summary>Revir: tesisten çıkarılan düşenin yatışından düşen vardiya (en az 1 kalır). Tezgâh: ödül tariflerinin kısalan
        /// vardiyası (en az 1 kalır).</summary>
        public int facilityInfirmaryShifts = 1, facilityWorkbenchShifts = 1;
        /// <summary>Nişan: pusu hattına eklenen kare. Kürk: köpeğe eklenen can. Makara: vincin çektiği kare (mutlak).</summary>
        public int facilitySightRange = 1, facilityFurHp = 1, facilityPulleyPull = 2;
        /// <summary>
        /// Yalnız ölçüm: Sedye kazanımını bütün harekât boyunca açık sayar (aşama 8, K24; <c>facilityLastBreathDies</c> emsali). Oyunda
        /// false: kazanım yalnız sunucu ödülüyle gelir.
        /// </summary>
        public bool facilityStretcherAlways = false;
        // ---- Tesis aşama 10: önlem defteri (Docs/TESIS-BASKINLARI-TASARIMI.md §6.3, §11.1). Sınıflar ve adlar Core/FacilityCounters.cs'te,
        // tahta kuralları Core/Baskin.Counters.cs'te; buradaki sayılar asset'e yazılmaz, C# varsayılanıyla çalışır (aşama 8 emsali).
        /// <summary>Bir baskının bir sınıfa ekleyebileceği en çok sayaç ve önlemin öğrenildiği eşik (en az 1 okunur).</summary>
        public int facilityTallyCap = 3, facilityCounterThreshold = 6;
        /// <summary>Önlem defterinin alt bayrağı: sayım, öğrenme, önlemler ve
        /// cevap tarifleri. Ölçüm kilidi için false (aşama 9 ile aynı oyun).</summary>
        public bool facilityCountermeasures = true;
        /// <summary>Yalnız ölçüm: pusu isabeti "aygıt" sayacına girer (belge §6.3'ün lafzı; aşama 10 K6). Oyunda false.</summary>
        public bool facilityTallyAmbush = false;
        /// <summary>Çift cidar: odada seçilen duvar sayısı, yıkmanın hak bedeli (tek hamlede) ve Delici şarjla bedeli.</summary>
        public int facilityDoubleWalls = 2, facilityDoubleWallCharges = 2, facilityPiercerCharges = 1;
        /// <summary>Kesici kol: makine niyetinin tele (kademe 2'de kalasa da) ek hasarı.</summary>
        public int facilityCutterDamage = 1;
        /// <summary>Kısa bağ: kelepçenin kısalan oyuncu turu (en az 1 kalır).</summary>
        public int facilityShortClampTurns = 1;
        /// <summary>Koku yayıcı: canı ve köpek komutlarını kestiği erim (Manhattan). Odadaki sayısı önlemin kademesidir.</summary>
        public int facilityEmitterHp = 1, facilityEmitterRange = 3;
        /// <summary>Parazit: aygıtın kısalan çalışma turu (en az 1 kalır).</summary>
        public int facilityJamTurns = 1;
        /// <summary>Makine aklı: pusu ve taret hattındaki varış karesinin konum cezası.</summary>
        public double facilityCounterLinePenalty = 6.0;
        /// <summary>Cevap tarifinin temel tarife eklediği devre.</summary>
        public int facilityAnswerCircuits = 1;
        /// <summary>Cevapların etkisi: Çelik tel setinin tele eklediği can, Yedek rölenin kelepçeye ve Çift zembereğin aygıtlara eklediği tur.</summary>
        public int facilitySteelWireHp = 1, facilityRelayTurns = 1, facilitySpringTurns = 1;
        // ---- Tesis aşama 11: kalıcı tahta (izler), YZ onarımı, bekleyen karşı saldırı ve üç odalı savunma (Docs/TESIS-BASKINLARI-TASARIMI.md
        // §9.1–9.3, §11.1). İzler Core/FacilityMarks.cs'te, savunma tahtası Core/Baskin.Defense.cs'te; buradaki sayılar asset'e yazılmaz,
        // C# varsayılanıyla çalışır (aşama 8 ve 10 emsali).
        /// <summary>İzlerin alt bayrağı: kalıcı tahta, YZ onarımı, bina planındaki izler ve emirdeki iz kopyası. Ölçüm kilidi için false
        /// (aşama 10 tahtaları).</summary>
        public bool facilityMarks = true;
        /// <summary>Onarım: işgaldeki her kanatta vardiya kapanışında listede en önde duran bu kadar iz silinir (en eski önce).</summary>
        public int facilityRepairPerShift = 1;
        /// <summary>Savunmanın alt bayrağı: bekleyen karşı saldırı (kuşatma), savun/bırak komutları, üç odalı savunma ve yeniden alınan
        /// bölgenin fonu ve güveninin bir kez verilmesi (§9.3). false: anlık karşı saldırı ve yeniden verilen bölge ödülü (aşama 10).</summary>
        public bool facilityDefenseEnabled = true;
        /// <summary>Savunmanın toplam süresi (tek tur sayacı; oda değişince sıfırlanmaz, en az 1 okunur).</summary>
        public int facilityDefenseTurns = 10;
        /// <summary>Dalgalar: ilk dalganın çıkış turu, dalga aralığı ve dalga sayısı (2, 5, 8). İç odaya geliş terk edilen oda başına bir
        /// tur gecikir; iç odanın ilk turu (barikat) makinesizdir.</summary>
        public int facilityDefenseFirstWave = 2, facilityDefenseWaveTurns = 3, facilityDefenseWaves = 3;
        /// <summary>Dalga büyüklüğü: bu taban + en çok 3 olmak üzere kademe (kademe 1/2/3/4 → 2/3/4/4).</summary>
        public int facilityDefenseWaveBase = 1;
        /// <summary>Saha rölesinin canı (hedef odasında; belgede facilityRelayHp, Yedek rölenin facilityRelayTurns'üyle karışmasın diye bu
        /// ad). Röle kırılırsa savunma kaybedilir.</summary>
        public int facilityDefenseRelayHp = 4;
        /// <summary>Savunmada terk edilen (içeri çekilinen) oda başına güven kaybı.</summary>
        public int facilityDefenseRoomTrust = 1;
        /// <summary>Siper kazanımı: savunmada saha rölesine eklenen can.</summary>
        public int facilityCoverRelayHp = 1;
        // ---- Tesis aşama 12: Katar (na) ve Yalpa (oc) tesis şartları (Docs/TESIS-BASKINLARI-TASARIMI.md §10, §11.1). Kurallar
        // Core/Baskin.Conditions.cs'te; buradaki sayılar asset'e yazılmaz, C# varsayılanıyla çalışır (aşama 8, 10 ve 11 emsali).
        /// <summary>Katar'da açık kenardan düşen ekip üyesinin yatışı (vardiya; ölmez; en az 1 okunur).</summary>
        public int facilityFallInjuryTurns = 5;
        /// <summary>Katar'ın takibi: dönüşte batı geçidine (bağlantıya) yürüme uzaklığı en çok bu kadar olan makine sonraki vagona geçer.</summary>
        public int facilityFollowRange = 2;

        // Tesis aşama 13: bölgeye özgü imza makineleri.
        public bool facilitySignatureMachines = true;
        public int facilityFlameRange = 2, facilityFlameDamage = 1;
        public int facilityRamRange = 3, facilityRamPush = 2;
        public int facilityAnchorRange = 4, facilityAnchorPull = 2;
        public int facilityFrostRange = 3, facilityFrostTurns = 2;
        public int facilitySealHp = 3, facilityBuildDamage = 1;
        /// <summary>Sahadaki sızmacının alt bayrağı (tesis aşama 17; kayıt 21'den beri varsayılan açık): ekipteki sızmacı baskında döner,
        /// saha kararı (sabotaj olayı) açılır, tesis izi "f10" (sürüm 10; önce "f9"). false: saha kapalı (tesis izi "f8s10", önce "f8"; aşama 13–15 ölçüm kilitleri bunu BAL
        /// ile verir).</summary>
        public bool infiltratorFieldEnabled = true;
        public int infiltratorTellMaxDelay = 4, infiltratorTurn = 2;
        public bool facilityTitlesEnabled = true;
        public int facilityMaxTitles = 2;
        public int facilityMarksmanAt = 3, facilityCarrierAt = 2, facilityRamTitleAt = 3;
        public int facilityHandlerAt = 3, facilityBuilderAt = 3, facilityThrowerAt = 2;
        public int facilityTitleAmbushBonus = 2, facilityTitleRushBonus = 1, facilityTitleThrowBonus = 2;
        public int facilityTitleDeviceBonus = 1, facilityTitleDogCommands = 2;
        /// <summary>Katar'ın alt bayrağı: na tesiste Katar (açık kenar, takip, AYIR; G ve A'da ileri geçit doğu sütununda). false: yük
        /// asansörü (aşama 11 planı ve tahtaları).</summary>
        public bool facilityKatar = true;
        /// <summary>Yalpa'nın alt bayrağı: oc tesiste Yalpa (2., 4., 6. tur sonunda yatış). false: hidrolik yağ (aşama 11).</summary>
        public bool facilityYalpa = true;
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
        /// <summary>Kayıt 22: Boşta olmayan (görevdeki, saha rolündeki) yatmayan kişinin vardiya kapanışındaki iyileşmesi; Boşta
        /// <see cref="boardRestHeal"/> alır. Yarım tayında ikisi de 0.</summary>
        public int boardDutyHeal = 1;
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
        /// <summary>Ayaktaki kişi bunun altındaysa kapıya acil grup gelir (terk sayacı da bu eşiği okur). Tek kip denge turu
        /// (2026-11-22): 3 → 4; tesiste baskınların ~%9'unda üç kişilik ekibin tamamı ölüyor, 3'te bu sığınağın tamamı oluyordu
        /// ("son kişi" sonu; çıkarınca bot zaferi %55 → %32,5).</summary>
        public int candidateEmergencyBelow = 4;
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
