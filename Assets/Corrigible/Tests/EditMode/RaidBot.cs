using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Tests
{
    /// <summary>
    /// Denge ölçümü için baskın botu: <see cref="Baskin"/> tahtasını baştan sona oynayan deterministik açgözlü oyuncu.
    /// Her turda uzmanların hamle + yetenek seçeneklerini durumun kopyalarında dener ve kaba bir puanla sıralar (makinelerin
    /// görünen niyetleri ve uyanma kareleri, <see cref="Baskin.Intents"/>); ilk adımın en iyi birkaç seçeneğini açgözlü
    /// tamamlar, bu tur planlarını kopyada gerçek makine evresiyle (<see cref="Baskin.EndTurn"/>) doğrular ve en iyisini oynar.
    /// Puan bir makine evresi sonrasına bakar ve görevin kalan yolunu ekler: Teknisyen'den hedefin (kasanın) yanına ve oradan
    /// çıkışa en kısa yol; kaçışta (kasa alındı ya da hedef kırıldı) tahtadaki her uzmanın çıkışa yolu, süreye sığmayan uzman
    /// düşmüş sayılır (taşıyıcıysa görev kaybedilir); makinenin
    /// durduğu kare bedelli (gelişmiş akıl hedefin önünü bedeniyle tutar, yolu açmak da ilerlemedir). Tek evre
    /// bakan puan Madenci'nin dinamitini (birkaç tur sonra kısalan yol) görmez ve sisi fazla değerli bulur; yol terimi ve
    /// "yıkılacak duvar" çekimi (<see cref="Weights.Breach"/>) bunu dengeler.
    /// Botun rastgeleliği yok: aynı tahta aynı oyunu verir. Gerçek durum yalnız motorun komutlarıyla (DoMove, DoAct, EnterGate,
    /// Pass, EndTurn) değişir; denemeler kopyada yapılır. Kum fırtınasında (gizli uyanma) doğrulama yapılmaz: gerçek evre gizli
    /// kareyi gösterirdi.
    /// Tesis baskını (aşama 2, <see cref="PlayFacility"/>): oda zincirini simülasyonun komutlarıyla sürer (odayı aç, oyna, raporu
    /// dosyala). Oda içinde kapıyı bilir: kapı karesine yürüyen uzman kapıda bekler (tahliye edilmiş gibi güvende sayılır), kapı
    /// karesinde duran uzman <see cref="Baskin.EnterGate"/> ile girer, herkes kapıdayken GEÇ verilir. Giriş ve ara odada puanın
    /// hedefi ileri geçittir; donanım politikası (<see cref="Devices"/>) odaya girişte bir kez Teknisyen'e donanımı kırma hedefi
    /// verir. Odasız tahtada (Room null) bu dalların hiçbiri çalışmaz: oyun birebir aynıdır. Tek kipten (kayıt 21) beri kampanyada
    /// her baskın tesis odasıdır; Room null yalnız koddan kurulan örnek/test tahtasıdır (<see cref="Baskin.Create"/>). Aşağıdaki
    /// "eski tahta" yorumları ve Room null dalları o tahtayı anlatır; bot kararını ve tesis çıktısını değiştirmemek için bu turda
    /// sadeleştirilmedi (TESIS belgesi §12 "18 · Tek kip").
    /// Aşama 4 (tesis puanı): yol ve süre tahmini kadronun gerçek yürüyüşüyle yapılır (<see cref="Mv"/>; kartlı kadroda 2–5, eski
    /// puan Teknisyen'i 3 sayıyordu), bu tur henüz yürümemiş uzmana bir tur fazla sayılır (<see cref="TurnsFor"/>); giriş ve ara odada
    /// geç kalan her uzman (yalnız Teknisyen değil) odayı kaybettirdiği için bedellidir (<see cref="Weights.GateLate"/>,
    /// <see cref="Weights.GateMiss"/>); "kir" donanım hedefini ekibin geri kalanı sıkışık değilse seçer ve hedef süreye sığmaz olunca
    /// bırakır (<see cref="ReviewDeviceGoal"/>). Bu terimler yalnız oyuncunun gördüğünü okur (kişi kartı, tahta, niyetler). Not: planı
    /// seçerken kopyada oynatılan makine evresi (<see cref="ChooseTurn"/>) RNG tüketir; bot yeni uyanan makinenin türünü ve yerini
    /// bu yolla önceden görür (aşama 4'ten önce de böyleydi, iki tahtada aynı).
    /// Aşama 8 (ekipman, <see cref="Gear"/>): köpek, aygıt ve tutsak uzman değildir (<see cref="Baskin.IsSpecialist"/>: kapı, can,
    /// yol, kilit, sürükleme ve "ekip ayakta" sayımı); köpek ve tutsak kendi hamleleriyle oynanır, aygıt oynanmaz. Ekipman ve birim
    /// eylemleri <see cref="Baskin.AllActionKinds"/>'tan aday olur (süzgeçler <see cref="ExpandActions"/>'ta); aygıtlar makine planından
    /// önce çalıştığı için (K3) doğrulama kopyasında açık kalır, sonraki turları ve havlama kaba terimle sayılır (<see cref="UnitTerm"/>,
    /// <see cref="BarrierValue"/>, <see cref="BarkValue"/>). Ekipmansız tahtada bu terimlerin hiçbiri çalışmaz: oyun aşama 6 ile aynı.
    /// Aşama 10 (önlem defteri): kademeyi emirden (<see cref="Baskin.MoveRules"/>, <c>B.Params.Counters</c>) okur. Önlemlerin çoğu
    /// Core'un aday ve evre kurallarında kalır (bot kendiliğinden uyar); iki terim eklenir: köpeği susturan koku yayıcı bedellidir
    /// (<see cref="Weights.Emitter"/>; sabotaj, EMP ve tutsak komutu onu kırar) ve makine aklı (aygıt 2. kademe) pusu hattından
    /// kaçındığı için kurulu pusunun kaba beklentisi küçülür (<see cref="Weights.AmbushSeen"/>). Önlemsiz emirde ikisi de çalışmaz.
    /// Aşama 11 (üç odalı savunma, <see cref="Baskin.IsDefense"/>): aynı zincir sürücüsüyle oynanır; puan <see cref="DefenseScore"/>
    /// (röle canı <see cref="Weights.Relay"/>, süre sonu kazanç, görev yolu yok), barikat turunda sonraki tur gelecek makineler giriş
    /// karesinde hayalet makine sayılır (<see cref="Weights.WaveGhost"/>), tutarken kapı karesi aday değildir, içeri çekilme
    /// <see cref="Retreat"/> politikasıyla. Saldırı tahtasında bu dalların hiçbiri çalışmaz: puan birebir aynıdır.
    /// Aşama 12 (Katar ve Yalpa): Katar'ın dönüş geçidinde AYIR politikası (<see cref="Detach"/>); açık kenardan düşen ekip üyesi
    /// (<see cref="Baskin.Unit.Overboard"/>) ayrı bedelle puanlanır (<see cref="Edge"/>, <see cref="Weights.EdgeFall"/>); kenardan düşen
    /// makine bugünkü düşürülmüş makine terimini alır. Yalpa'nın yatışı doğrulama kopyasındaki gerçek evrede görülür (politika yok).
    /// Katar ve Yalpa dışında hiçbir dal çalışmaz: puan birebir aynıdır.
    /// </summary>
    public sealed class RaidBot
    {
        const int N = Baskin.N;
        /// <summary>Yolu olmayan hedefin uzaklığı.</summary>
        const int Far = 40;

        /// <summary>Puan ağırlıkları. Ölçek: görevin bir turluk ilerlemesi <see cref="Progress"/> puan.</summary>
        public sealed class Weights
        {
            public double Win = 10000, Loss = -10000;
            /// <summary>Görevin kalan turu başına (sürekli tahmin) ve süreye sığmayan tur başına.</summary>
            public double Progress = 45, Late = 60;
            /// <summary>Teknisyen'in canı, son canı ve düşmesi (Teknisyen'siz görev bitmez); öteki uzmanlar.</summary>
            public double TekHp = 8, TekLow = 10, TekDown = 300;
            public double Hp = 4, Low = 4, Down = 45, Plate = 3;
            /// <summary>Öteki uzmanların çıkışa yolunun Teknisyen'in yoluna göre ağırlığı (kaçışta bütün ekip çıkmalı). Ölçümde
            /// (2026-11-01, 4 tohum) 1 → 0,5 → 0,25 → 0,1 → 0 T+E+A'yı %72 → %79 → %87 → %92 → %94'e çıkardı; düşen uzman 0,50 →
            /// 0,33 → 0,38: uzmanlar Teknisyen'i desteklemeyi bırakıp çıkışa erken yürürse görev zorlaşır, hiç bakmazsa geride kalır.</summary>
            public double Others = 0.1;
            /// <summary>Ayaktaki makine, canı, zırhı; evrede uyanacak makine; ayaktaki kopya dolabı.</summary>
            public double Enemy = 7, EnemyHp = 3, EnemyArmor = 2, NewEnemy = 9, Spawner = 6;
            /// <summary>Kırılan sivil sunucu; kilitlenen Teknisyen (yolu varsa) ve öteki uzman.</summary>
            public double Civilian = 12, TekLocked = 18, Locked = 3;
            /// <summary>Yıkılacak duvarın kazancının, Madenci yanına vardığında puana geçen payı (yıkınca kazancın tamamı).</summary>
            public double Breach = 0.6;
            /// <summary>Yolda makinenin durduğu karenin fazladan bedeli (adım): makine düşürülmeli ya da itilmeli; gelişmiş akıl hedefin
            /// önünü bedeniyle tutar.</summary>
            public int EnemyStep = 3;
            /// <summary>Tesis: donanım hedefi seçilen odada donanım sağlamken Teknisyen'in sabit bedeli (kırınca kazanılır).</summary>
            public double Device = 60;
            /// <summary>Tesis giriş ve ara odası: öteki uzmanların ileri geçide yolunun Teknisyen'in yoluna göre ağırlığı. Aşama 4 ölçümü
            /// (tohum 1–20, "iyi" küme, DEV hic/kir baskın başarısı): 0,1 → %47/%46, 0,25 → %53/%60, 0,35 → %48/%59, 0,5 → %60/%57,
            /// 1 → %29/%28 (bu tarama uyanma karesini yolda bedelli sayan bir denemeyle yapıldı; o terim etkisizdi, 0,5'te onsuz %60/%58,
            /// çıkarıldı; tarama ortam değişkenli geçici bir kopyayla yapıldı, yön için). Büyük pay öbürlerini Teknisyen'den önce koşturur, makineler onları düşürür; harekâtlar ayrıştığı için gürültü
            /// ±7 puan.</summary>
            public double GateShare = 0.5;
            /// <summary>Tesis giriş ve ara odası: ileri geçide süresinde varamayacak uzmanın geç kaldığı tur başına bedeli ve geç kalan
            /// biri varsa odanın sabit bedeli (biri geç kalırsa oda, dolayısıyla baskın kaybedilir). Aşama 4'te 60/0, 150/200 ve 300/500
            /// aynı sonucu verdi (300/500 bayt bayt aynı): kararı çoğunlukla gerçek yürüyüşle yol terimi verir.</summary>
            public double GateLate = 150, GateMiss = 200;
            /// <summary>Tesis hamleleri (aşama 6). Ambush: makinenin bu tur yürüyebildiği bir hatta kurulan pusunun kaba beklentisi
            /// (makine canı 3 puan; isabet kesin değil). Pusu vuruşu doğrulamada görülmez (<see cref="ChooseTurn"/>), bu terimle sayılır.</summary>
            public double Ambush = 6;
            /// <summary>"Hep taşı" politikası (<see cref="Fallen"/>), yalnız kaçışta ve hedef odasında: tahtada yatan düşenin kapıya
            /// kalan yolu (sürükleyecek en yakın uzmanın gövdeye yolu + gövdenin kapıya yolu, sürükleme yürüyüşüyle; tur olarak)
            /// <see cref="Progress"/> × <see cref="BodyShare"/> ağırlıkla; süreye sığmayan gövde <see cref="Abandon"/>. Turun başında
            /// kurtarılabilen gövde yatarken GEÇ kazanç sayılmaz (<see cref="RescueFeasible"/>); kurtarılamayanla geçilirse gövde başına
            /// <see cref="Abandon"/>.</summary>
            public double BodyShare = 1, Abandon = 300;
            /// <summary>Tesis: taşıyıcının bu evrede düşeceği tahmini (yük yere düşer; eski tahtada görev biterdi).</summary>
            public double CargoDrop = 150;
            /// <summary>Tesis: açılmış büyük sandık (ek ödül) ve iki kişilik işin tutuşu (ikinci uzman bu tur gelebiliyorsa; sandıkta
            /// sandığın yarısı, kepenkte <see cref="Hold"/>; kepengin asıl kazancı açılınca kısalan yoldan gelir).</summary>
            public double Crate = 25, Hold = 10;
            /// <summary>Tesis ekipmanı (aşama 8; yalnız tesis odasında, terimler kaçıştan önce ve sonra aynı ağırlıkla). Aygıtın kalan
            /// turu başına (min(kalan tur, odanın kalan turu)): hattı bir makinenin varabileceği kareden geçen taret <see cref="Turret"/>,
            /// yakınındaki (Manhattan ≤ 5) en çok iki makine başına yem <see cref="Decoy"/>, hattının ilk dolu karesi gövde ya da yük olan
            /// vinç <see cref="Winch"/>. Aygıtın bu evredeki çalışması doğrulama kopyasında görülür (K3: makine planından önce çalışır);
            /// terim yalnız sonraki turların kaba beklentisidir.</summary>
            public double Turret = 4, Decoy = 2, Winch = 6;
            /// <summary>Engel (tel, kalas, hurda): her makinenin ekibe gerçek yolunun engel yüzünden uzadığı adım başına (makine başına en
            /// çok 3). Kalasın atışı kesmesi tahminde (niyetler tahtadan) kendiliğinden görülür.</summary>
            public double Barrier = 3;
            /// <summary>Tutsak makine: kalan süresi 1'den büyükken makine bedelinin bu payı (≤ 1 iken tam makine; kelepçe aşırı
            /// değerlenmesin).</summary>
            public double CaptiveShare = 0.3;
            /// <summary>Köpek: canı, düşmesi (3 vardiya dinlenir), havlamanın erimindeki en çok iki makine başına kaba değeri (yeni niyetler
            /// doğrulamada görülmez), koklama (sıradaki oda öğrenilir) ve koklamadan önce ileri geçide uzaklığı (köpek yürüyüşüyle tur).</summary>
            public double DogHp = 2, DogDown = 30, Bark = 4, Sniff = 8, SniffWalk = 2;
            /// <summary>Aşama 10, koku yayıcı (köpek önlemi): tahtada ayaktaki köpeği erimde (<see cref="Baskin.EmitterBlocks"/>) susturan
            /// yayıcının köpek başına bedeli (havla + kokla beklentisi kadar); yayıcıyı kırmak ya da köpeği erimden çıkarmak kazançtır.
            /// Önlemsiz emirde ve "hic"te 0.</summary>
            public double Emitter = 6;
            /// <summary>Aşama 10, makine aklı (aygıt 2. kademe): makineler kurulu pusu hattından kaçınır; pusunun kaba beklentisi
            /// <see cref="Ambush"/> × bu pay (1. kademe ve önlemsiz emirde tam).</summary>
            public double AmbushSeen = 0.25;
            /// <summary>Aşama 11, savunma tahtası: saha rölesinin canı başına (kırık röle kayıp). Yalnız savunmada.</summary>
            public double Relay = 10;
            /// <summary>Aşama 11, barikat: sonraki tur gelecek makine (<see cref="Baskin.DefenseIncoming"/>) giriş karesinde hayalet makine
            /// sayılır; engelin, taretin ve pusunun bu makineye değeri gerçeğinin bu payı (0 kapatır). Yalnız savunmada.</summary>
            public double WaveGhost = 1;
            /// <summary>Aşama 12, Katar: açık kenardan düşen uzman (Teknisyen ayrı). NaN (varsayılan): değer <see cref="Edge"/>
            /// politikasından (hic: düşmek + bırakmak, <see cref="Down"/> + <see cref="Abandon"/> ve <see cref="TekDown"/> +
            /// <see cref="Abandon"/>; kacis: 0,3 × düşmek). Camp8 BOTW ile sayı verilirse politika ezilir (ağırlık taraması).</summary>
            public double EdgeFall = double.NaN, TekEdgeFall = double.NaN;
        }

        public Weights W = new Weights();
        /// <summary>İlk adımda denenen seçenek sayısı: her biri açgözlü tamamlanır, gerçek makine evresiyle karşılaştırılır. Ölçümde
        /// (4 tohum, 5760 baskın) 1 → 3 → 6 → 10 seçenek T+E+A'yı %78 → %88 → %92 → %94'e çıkardı; kadrolar arası sıra ve farklar
        /// hemen hiç değişmedi. Puan ağırlıklarının ±%50 değişimi de sırayı değiştirmedi.</summary>
        public int Alternatives = 8;
        /// <summary>İzleme: her turun başında durum ve seçilen plan (plan uygulanmadan önce).</summary>
        public Action<Baskin.BState, List<Step>> OnTurn;
        /// <summary>Tesis donanım politikası: "kir" (fırsat varsa: süreye sığıyorsa kır), "hic" (dokunma), "hep" (sağlamsa her zaman
        /// kır). Hedef odaya girişte bir kez seçilir (puan oda içinde sıçramasın); "kir" süreye sığmaz olunca turun başında bırakır
        /// (<see cref="ReviewDeviceGoal"/>).</summary>
        public string Devices = "kir";
        /// <summary>Tesis düşen politikası (aşama 6): "hep" (hep taşı: düşeni kapıya sürükler, puanda gövdenin kapıya yolu ve odada
        /// bırakmanın bedeli; kapı karesindeki uzman yanında gövde varken turun başında kendiliğinden kapıya girmez) ya da "hic" (hiç
        /// taşıma: sürükleme adayı yok, gövde yalnız düşme cezasıdır). Temiz odadan dönüşte toplanma politika dışıdır (kural).
        /// Eski tahtada etkisiz.</summary>
        public string Fallen = "hep";
        /// <summary>Tesis ekipman politikası (aşama 8): "kullan" (ekipman eylemleri ve köpeğin/tutsağın komutları aday) ya da "hic"
        /// (hiçbiri aday değil; köpek yine yürür, tutsak olmaz): ekipmanın kendi değeri aynı üretim yolunda eşli ölçülür. Eski tahtada
        /// etkisiz.</summary>
        public string Gear = "kullan";
        /// <summary>Aşama 11, savunmada içeri çekilme (Camp8 CEKIL): "hic" odayı tutar (kapı karesine girmez, GEÇ vermez), "hep" giriş ve
        /// ara odada ilk fırsatta ileri geçide yürür ve GEÇ verir (hedef odasında geçit yok: tutar), "baski" turun başında tehlikede (ayaktaki
        /// uzman ≤ 1 ya da makine ≥ ayaktaki + 2) ve ayaktaki bütün uzmanlar bu tur ileri geçide varabiliyorsa o odada çekilir. Saldırı
        /// baskınında etkisiz.</summary>
        public string Retreat = "hic";
        /// <summary>Aşama 12, Katar'ın dönüş geçidinde AYIR (Camp8 AYIR): "hep" verilebildiğinde (<see cref="Baskin.CanDecouple"/>)
        /// AYIR, değilse GEÇ; "hic" hep GEÇ (takip işler); "takip" yalnız takip edecek makine varken (<see cref="Baskin.FollowerIds"/>).
        /// Başka her yerde <see cref="Baskin.DecoupleBlock"/> "rule" döner: oyun aynı.</summary>
        public string Detach = "hep";
        /// <summary>Aşama 12, kendi üyesini Katar'ın kenarından düşürmenin bedeli (Camp8 KENAR): "hic" düşmekten de pahalı (kasıtlı
        /// düşürmez, EMP'nin yan itmesinden kaçınır), "kacis" ucuz (yetişemeyecek üyeyi EMP ile kenardan atar; belge §13'ün üst sınırı).
        /// <see cref="Weights.EdgeFall"/> sayı verilmişse o geçerli.</summary>
        public string Edge = "hic";
        /// <summary>Tesis: her oda bitince (rapor dosyalanmadan önce) tahta, rapor ve odanın sayaçları.</summary>
        public Action<Baskin.BState, RoomReport, Tally> OnRoom;
        /// <summary>Oynanan odada Teknisyen'in donanımı kırma hedefi (<see cref="Play"/> odaya girişte bir kez seçer; eski tahtada false).</summary>
        bool deviceGoal;
        /// <summary>Turun başında (<see cref="RescueFeasible"/>): yatan gövdeler süresinde kurtarılabilir; GEÇ onları bırakmaktır.</summary>
        bool bodyRescue;
        /// <summary>Aşama 11: savunmada bu odadan içeri çekiliniyor (<see cref="Retreat"/>; odaya girişte ya da "baski"de turun başında).</summary>
        bool retreating;

        /// <summary>Baskının (tesiste odanın) sayaçları: yıkılan duvar, robota/nesneye atılan dinamit, sis bombası, sise kesilen niyet;
        /// tesiste kapıya giriş (yürüyüşle ya da EnterGate), donanım karesine eylem ve odada donanım hedefi seçildi mi.
        /// Tesis hamleleri (aşama 6): eylem türü başına kullanım (pusu, koşu, sürükleme, fırlatma, iki kişilik iş, menfez), pusu
        /// isabeti, açılan iş (kepenk ya da büyük sandık), yerdeki yükün düşmesi ve alınması (efektlerden sayılır).</summary>
        public sealed class Tally
        {
            public int Walls, DynamiteHits, Smokes, SmokedShots, Gates, DeviceHits; public bool DeviceGoal;
            public int Ambush, Rush, Drag, Throw, Joint, Vent, AmbushHits, Opened, CargoDrops, CargoPicks;
            /// <summary>Aşama 8: ekipman ve birim eylemleri (tür başına kullanım) ve efektlerden etkiler (taret isabeti, vinç çekişi, yem
            /// adımı, ele geçirme, bırakılma, köpeğin düşmesi, hurda, kırılan aygıt, tutsağın verdiği plaka).</summary>
            public int Wire, Plank, Charge, Turret, Decoy, Winch, Clamp, Bark, Fetch, Sniff, Command;
            public int TurretHits, WinchPulls, DecoySteps, Captures, Releases, DogDowns, Scraps, Breaks, Plates;
            /// <summary>Aşama 10, önlemlerin efektleri: cıvatalı makinenin direndiği itme/çekiş ("bolted"), Söküm ("unbolt"), çift cidarın
            /// yıkılması ("dwall"), kesici kolun ek hasarı ("cut"), parazitin kısalttığı aygıt ("jam"), kırılan koku yayıcı ("break",
            /// Obj "yayici"); vincin makineyi çektiği çekiş (Core'un itme sayımı: çekiş başına bir, adım başına değil).</summary>
            public int Bolted, Unbolts, DoubleWalls, Cuts, Jams, EmitterBreaks, MachinePulls;
            /// <summary>Aşama 12, Katar: verilen AYIR ("decouple" efekti), Katar'ın dönüş geçişinde verilmeyen AYIR'ın nedeni
            /// (<see cref="Baskin.DecoupleBlock"/>: tek, gate, acted; "pol" politika istemedi, "auto" kendiliğinden geçiş), geçişte takip
            /// eden makine (<see cref="Baskin.FollowerIds"/>), kenardan düşen makine, uzman ve öbürleri (köpek, tutsak, aygıt; "fall"
            /// efekti), kenardan düşen makinenin kaynağı (emp, sab, kosu, evre, diger: efekti doğuran komut).</summary>
            public int Detaches, Followers, EdgeMachines, EdgeTeam, EdgeOther;
            public readonly Dictionary<string, int> DetachMiss = new Dictionary<string, int>();
            public readonly Dictionary<string, int> EdgeBy = new Dictionary<string, int>();
            /// <summary>Aşama 12, Yalpa: yatış ("ev" efekti, Obj "yalpa"), kayan şeyler türe göre ("slide": p uzman, e makine, govde,
            /// yuk, hurda, diger köpek/tutsak/aygıt) ve çarpan birim (slide N 0: uzman, makine).</summary>
            public int Tilts, TiltHurtP, TiltHurtE;
            public readonly Dictionary<string, int> Slides = new Dictionary<string, int>();
            /// <summary>Vincin makine çekişleri (tur, vinç, makine): bir çekişin iki adımı bir kez sayılır.</summary>
            internal readonly HashSet<(int, int, int)> Pulled = new HashSet<(int, int, int)>();
            /// <summary>Sayılmış efektler (nesne kimliği): yürüyüş ve kapıya giriş efekt listesini temizlemez, aynı efekt bir kez sayılır.</summary>
            internal readonly HashSet<Baskin.Fx> Counted = new HashSet<Baskin.Fx>();
        }

        /// <summary>Bir uzmanın tur hamlesi: isteğe bağlı yürüyüş, ardından isteğe bağlı yetenek. Tesis hamlesi (aşama 6,
        /// <see cref="Kind"/> dolu, <see cref="Baskin.ActionKinds"/>): yeteneğin yerine geçenlerde (pusu, fırlatma, iki kişilik iş)
        /// isteğe bağlı yürüyüş + eylem (AX, AY); koşu darbesinde yalnız Act (AX, AY makinenin karesi); sürüklemede yalnız Move (MX, MY
        /// varış, Other gövdenin birim kimliği); menfezde Move (MX, MY öbür uç) + isteğe bağlı yetenek. Kind null: bugünkü yol.</summary>
        public struct Step { public int Unit, MX, MY, AX, AY, Other; public bool Move, Act; public string Kind; }

        sealed class Option { public double Pre; public Step Step; public Baskin.BState State; }

        sealed class Line { public double Pre; public Baskin.BState State; public List<Step> Steps; public List<int> Left; }

        /// <summary>Tur bağlamı: Madenci'nin yıkması en çok yol kısaltan duvar ve o duvarın yanına uzaklık alanı.</summary>
        sealed class Context { public bool HasBreach; public int BX, BY, Gain; public int[,] Field; }

        /// <summary>Kaba makine evresi tahmini (birim sırası <c>C.Units</c>): hasar, kırılan plaka, kilit, nesne hasarı.</summary>
        sealed class Forecast
        {
            public int[] Dmg, Plate; public bool[] Locked;
            public readonly int[] ObjDmg = new int[N * N];
            public int NewEnemies, Collateral, Repairs, SpawnersBroken, NewBarriers; public bool CrateLost;
        }

        struct Route { public int D1, D2; public int Total => D1 + D2; }

        /// <summary>
        /// Tahtayı (tesisin bir odası ya da koddan kurulan örnek tahta) sonuna kadar oynar; durum yerinde değişir. Tesis odasında evre "pass"
        /// (GEÇ ya da kendiliğinden geçiş), "won" ya da "lost" ile biter. Donanım hedefi odaya girişte burada bir kez seçilir.
        /// </summary>
        public Tally Play(Baskin.BState B)
        {
            var tally = new Tally();
            // Aşama 11: savunmada donanım hedefi yok (görev tutmak); içeri çekilme odaya girişte ("hep") ya da turun başında ("baski")
            deviceGoal = !Baskin.IsDefense(B) && DeviceGoal(B);
            tally.DeviceGoal = deviceGoal;
            retreating = Baskin.IsDefense(B) && Retreat == "hep" && B.Room != null && B.Room.Forward.Count > 0;
            passed = false;
            while (B.Phase == "player")
            {
                if (Baskin.IsDefense(B)) ReviewRetreat(B);
                if (GateStep(B, tally, true)) break;
                ReviewDeviceGoal(B);
                var plan = ChooseTurn(B);
                OnTurn?.Invoke(B, plan);
                foreach (var step in plan) Apply(B, step, tally);
                if (B.Phase != "player") break;
                if (GateStep(B, tally, false)) break;
                if (!Baskin.EndTurn(B))
                {
                    // Aşama 11: savunmada bütün ekip kapıdayken tur yalnız GEÇ ile biter (Baskin.CanEndTurn); saldırıda bugünkü istisna
                    if (Baskin.IsDefense(B) && !Baskin.CanEndTurn(B) && Baskin.Pass(B)) break;
                    throw new InvalidOperationException("Bot turu bitiremedi.");
                }
                tally.SmokedShots += B.FxList.Count(f => f.K == "smoked");
                CountFx(B, tally, "evre");
            }
            if (B.Phase == "pass")
            {
                // Aşama 12: geçişte takip eden makineler (raporun Follow'u); Katar'ın dönüşündeki kendiliğinden geçiş AYIR'sız geçer
                tally.Followers = Baskin.FollowerIds(B).Count;
                if (!passed && B.Room != null && B.Ev?.Id == "katar" && B.Escape && !Baskin.IsDefense(B) && !Baskin.IsFinalExit(B))
                    Bump(tally.DetachMiss, "auto");
            }
            deviceGoal = false;
            retreating = false;
            return tally;
        }

        /// <summary>Bu odada GEÇ ya da AYIR bot tarafından verildi (kendiliğinden geçiş değil).</summary>
        bool passed;

        static void Bump(Dictionary<string, int> map, string key)
        {
            map.TryGetValue(key, out int n);
            map[key] = n + 1;
        }

        /// <summary>Aşama 12: kenardan düşen uzmanın bedeli (<see cref="Weights.EdgeFall"/> sayıysa o; değilse <see cref="Edge"/>).</summary>
        double EdgeCost(bool isTek)
        {
            double w = isTek ? W.TekEdgeFall : W.EdgeFall;
            if (!double.IsNaN(w)) return w;
            double down = isTek ? W.TekDown : W.Down;
            // 0,3 × düşmek, tam sayılarla (taramanın EdgeFall=13.5 değeriyle bayt aynı)
            return Edge == "kacis" ? down * 3 / 10 : down + W.Abandon;
        }

        /// <summary>
        /// Aşama 11, "baski": turun başında (giriş ve ara odada, son turdan önce) tehlike varsa (ayaktaki uzman ≤ 1 ya da tahtadaki makine
        /// ≥ ayaktaki + 2) ve ayaktaki her uzman bu tur ileri geçide varabiliyorsa (kapıda, geçit karesinde ya da yürüyüşüyle) o odadan
        /// çekilinir; karar oda bitene kadar sürer. Yalnız oyuncunun gördüğünü okur (tahta, kişi kartı).
        /// </summary>
        void ReviewRetreat(Baskin.BState B)
        {
            if (retreating || Retreat != "baski" || B.Room == null || B.Room.Forward.Count == 0 || B.Turn >= B.MaxTurns) return;
            var up = B.Units.Where(u => Baskin.IsSpecialist(u) && !u.Dead && !u.Out).ToList();
            int machines = Baskin.Alive(B, 'e').Count(e => !Baskin.IsGadget(e));
            if (up.Count == 0 || !(up.Count <= 1 || machines >= up.Count + 2)) return;
            foreach (var u in up)
            {
                if (u.AtGate || Baskin.OnBoard(u) && Baskin.IsGateTile(B, u.X, u.Y)) continue;
                if (!Baskin.OnBoard(u) || !Baskin.CanMove(B, u) || !Baskin.Reachable(B, u).Any(p => Baskin.IsGateTile(B, p.X, p.Y))) return;
            }
            retreating = true;
        }

        /// <summary>
        /// Süren tesis baskınını oda oda oynar (Docs/TESIS-BASKINLARI-TASARIMI.md §11, sürücü sözleşmesi): odayı açar
        /// (<see cref="Simulation.OpenRaidRoom"/>), oynar, raporu dosyalar (<see cref="Simulation.AdvanceRaidRoom"/>); son oda
        /// raporlanınca raporlardan toplanan sonucu döndürür (çağıran <see cref="Simulation.ResolveRaid"/>'e verir). Yalnız komut
        /// kullanır: kayıttan aynen yeniden oynanır.
        /// </summary>
        public RaidResult PlayFacility(Simulation sim)
        {
            for (int guard = 0; guard < 6; guard++)                 // en çok 5 oda
            {
                if (sim.RaidRoomsClosed) return sim.FacilityRaidResult();
                var B = sim.OpenRaidRoom();
                var tally = Play(B);
                var rep = sim.RaidRoomReport(B);
                OnRoom?.Invoke(B, rep, tally);
                var ok = sim.AdvanceRaidRoom(rep);
                if (!ok.Ok) throw new InvalidOperationException("raid_room: " + ok.Msg);
            }
            throw new InvalidOperationException("Oda zinciri bitmedi.");
        }

        /// <summary>Kapı karesinde duran uzmanlar kapıya girer (<see cref="GateIdle"/>); herkes kapıdaysa GEÇ. Evre değiştiyse true.</summary>
        bool GateStep(Baskin.BState B, Tally tally, bool start)
        {
            GateIdle(B, tally, start);
            if (B.Phase != "player") return true;
            if (!Baskin.CanPass(B)) return false;
            // Aşama 12 (Katar'ın dönüş geçidi): AYIR politikası; Katar dönüşü değilse nedeni "rule", oyun aynı
            string why = Baskin.DecoupleBlock(B);
            if (why != "rule")
            {
                bool want = why == null && (Detach == "hep" || Detach == "takip" && Baskin.FollowerIds(B).Count > 0);
                if (want)
                {
                    if (!Baskin.Decouple(B)) throw new InvalidOperationException("Bot AYIR veremedi.");
                    passed = true;
                    CountFx(B, tally);
                    return true;
                }
                Bump(tally.DetachMiss, why ?? "pol");
            }
            if (!Baskin.Pass(B)) throw new InvalidOperationException("Bot GEÇ veremedi.");
            passed = true;
            return true;
        }

        /// <summary>Tahtada kapı karesinde duran (itilmiş, kurulumda konmuş) her uzman adım harcamadan kapıya girer; donanım hedefi
        /// sürerken Teknisyen hariç. Eski tahtada kapı karesi yok. Tesis, "hep taşı": turun başında (<paramref name="start"/>) yanında
        /// tahtada yatan gövde olan ve yürüyebilen uzman girmez (önce sürüklesin; plandan sonra bugünkü kural).</summary>
        void GateIdle(Baskin.BState B, Tally tally, bool start)
        {
            if (B.Room == null) return;
            // Aşama 11: savunmada tutarken kapıya girilmez (kapı sığınak değildir; bütün ekip kapıdaysa yalnız GEÇ)
            if (Baskin.IsDefense(B) && !retreating) return;
            bool keepTek = deviceGoal && DeviceIntact(B);
            // Aşama 8: yalnız uzman kapıya girer (köpek, aygıt ve tutsak girmez; köpek ve tutsak GEÇ'te ekiple geçer)
            foreach (var u in Baskin.Alive(B, 'p').Where(Baskin.IsSpecialist))
            {
                if (B.Phase != "player") return;
                if (!Baskin.OnBoard(u) || !Baskin.IsGateTile(B, u.X, u.Y) || keepTek && u.Type == "tek") continue;
                if (start && Fallen == "hep" && B.Moves != null && BodyRoom(B) && Baskin.CanMove(B, u) && Baskin.DragBodies(B, u).Count > 0) continue;
                if (!Baskin.EnterGate(B, u.Id)) throw new InvalidOperationException("Bot kapıya giremedi.");
                tally.Gates++;
            }
        }

        /// <summary>Tesis odasında son komutun efektlerinden sayaçlar: pusu isabeti, açılan iş, yükün düşmesi ve alınması. Yalnız yeni
        /// efektler sayılır: DoMove ve EnterGate listeyi temizlemez (önceki komutun ya da makine evresinin efektleri listede kalır),
        /// sayılan efekt nesne kimliğiyle bir daha sayılmaz (inceleme bulgusu: pusu isabeti ~2,2 kat şişikti).</summary>
        /// <param name="src">Aşama 12: efekti doğuran komut (kenardan düşen makinenin kaynağı): emp, sab, kosu, evre (makine evresi ve
        /// olay adımı), diger.</param>
        internal static void CountFx(Baskin.BState B, Tally tally, string src = "diger")
        {
            if (B.Room == null) return;
            foreach (var f in B.FxList)
            {
                if (!tally.Counted.Add(f)) continue;
                switch (f.K)
                {
                    case "ambush-hit": tally.AmbushHits++; break;
                    case "open": tally.Opened++; break;
                    case "cargo-drop": tally.CargoDrops++; break;
                    case "cargo-pick": tally.CargoPicks++; break;
                    case "turret-hit": tally.TurretHits++; break;
                    case "winch-pull":
                        tally.WinchPulls++;
                        // Aşama 10: makine çekişi (Core'un itme sayımı; gövde, yük ve ekip çekişi sayılmaz)
                        if (f.Obj == null && UnitById(B, f.Unit) is Baskin.Unit pu && pu.Side == 'e' && tally.Pulled.Add((B.Turn, f.N, f.Unit)))
                            tally.MachinePulls++;
                        break;
                    case "bolted": tally.Bolted++; break;
                    case "unbolt": tally.Unbolts++; break;
                    case "dwall": tally.DoubleWalls++; break;
                    case "cut": tally.Cuts++; break;
                    case "jam": tally.Jams++; break;
                    case "break": if (f.Obj == "yayici") tally.EmitterBreaks++; break;
                    case "decoy-step": tally.DecoySteps++; break;
                    case "capture": tally.Captures++; break;
                    case "release": tally.Releases++; break;
                    case "dog-down": tally.DogDowns++; break;
                    case "scrap": tally.Scraps++; break;
                    case "device-break": tally.Breaks++; break;
                    case "plate-give": tally.Plates++; break;
                    // Aşama 12: Katar'ın kenarı ve AYIR, Yalpa'nın yatışı ve kayması
                    case "fall":
                        {
                            var fu = UnitById(B, f.Unit);
                            if (fu != null && Baskin.IsSpecialist(fu)) tally.EdgeTeam++;
                            else if (fu != null && fu.Side == 'e' && !Baskin.IsGadget(fu)) { tally.EdgeMachines++; Bump(tally.EdgeBy, src); }
                            else tally.EdgeOther++;
                            break;
                        }
                    case "decouple": tally.Detaches++; break;
                    case "ev": if (f.Obj == "yalpa") tally.Tilts++; break;
                    case "slide":
                        {
                            if (f.Obj == "hurda") { Bump(tally.Slides, "hurda"); break; }
                            if (f.Obj == "body") { Bump(tally.Slides, "govde"); break; }
                            if (f.Obj == "cargo") { Bump(tally.Slides, "yuk"); break; }
                            var su = UnitById(B, f.Unit);
                            string kind = su != null && Baskin.IsSpecialist(su) ? "p" : su != null && su.Side == 'e' && !Baskin.IsGadget(su) ? "e" : "diger";
                            Bump(tally.Slides, kind);
                            if (f.N == 0) { if (kind == "p") tally.TiltHurtP++; else if (kind == "e") tally.TiltHurtE++; }
                            break;
                        }
                }
            }
        }

        static void Apply(Baskin.BState B, Step step, Tally tally)
        {
            if (B.Phase != "player") return;
            if (step.Kind != null) { ApplyAction(B, step, tally); return; }
            if (step.Move && !Baskin.DoMove(B, step.Unit, step.MX, step.MY)) throw new InvalidOperationException("Bot hamlesi geçersiz: yürüyüş");
            CountFx(B, tally);
            var u = UnitById(B, step.Unit);
            if (step.Move && u.AtGate) tally.Gates++;
            if (!step.Act || B.Phase != "player") return;
            Act(B, step, tally);
        }

        /// <summary>Uzmanın yeteneği (bugünkü yol) ve sayaçları.</summary>
        static void Act(Baskin.BState B, Step step, Tally tally)
        {
            var u = UnitById(B, step.Unit);
            string ability = Baskin.UnitTypes[u.Type].Ability;
            bool wall = Baskin.CellAt(B, step.AX, step.AY).T == "wall";
            var target = Baskin.CellAt(B, step.AX, step.AY).O;
            if (target != null && Baskin.IsDevice(target.K)) tally.DeviceHits++;
            if (!Baskin.DoAct(B, step.Unit, step.AX, step.AY)) throw new InvalidOperationException("Bot hamlesi geçersiz: yetenek");
            CountFx(B, tally, ability == "emp" ? "emp" : ability == "sabotaj" ? "sab" : "diger");
            if (ability == "dinamit") { if (wall) tally.Walls++; else tally.DynamiteHits++; }
            else if (ability == "duman") tally.Smokes++;
        }

        /// <summary>Tesis hamlesi (<see cref="Step.Kind"/>): bedeline göre yürüyüş/eylem/yetenek sırası (<see cref="Step"/>).</summary>
        static void ApplyAction(Baskin.BState B, Step step, Tally tally)
        {
            var cost = Baskin.CostOf(step.Kind);
            if (cost == Baskin.ActionCost.Ability && step.Move)
            {
                if (!Baskin.DoMove(B, step.Unit, step.MX, step.MY)) throw new InvalidOperationException("Bot hamlesi geçersiz: yürüyüş");
                CountFx(B, tally);
                if (UnitById(B, step.Unit).AtGate) tally.Gates++;
                if (B.Phase != "player") return;
            }
            bool at = cost == Baskin.ActionCost.Ability || step.Kind == Baskin.ActRush;
            int x = at ? step.AX : step.MX, y = at ? step.AY : step.MY;
            if (!Baskin.DoAction(B, step.Unit, step.Kind, x, y, step.Other)) throw new InvalidOperationException("Bot hamlesi geçersiz: " + step.Kind);
            CountFx(B, tally, step.Kind == Baskin.ActRush ? "kosu" : "diger");
            switch (step.Kind)
            {
                case Baskin.ActAmbush: tally.Ambush++; break;
                case Baskin.ActRush: tally.Rush++; break;
                case Baskin.ActDrag: tally.Drag++; break;
                case Baskin.ActThrow: tally.Throw++; break;
                case Baskin.ActJoint: tally.Joint++; break;
                case Baskin.ActVent: tally.Vent++; break;
                case Baskin.ActWire: tally.Wire++; break;
                case Baskin.ActPlank: tally.Plank++; break;
                case Baskin.ActCharge: tally.Charge++; break;
                case Baskin.ActTurret: tally.Turret++; break;
                case Baskin.ActDecoy: tally.Decoy++; break;
                case Baskin.ActWinch: tally.Winch++; break;
                case Baskin.ActClamp: tally.Clamp++; break;
                case Baskin.ActBark: tally.Bark++; break;
                case Baskin.ActFetch: tally.Fetch++; break;
                case Baskin.ActSniff: tally.Sniff++; break;
                case Baskin.ActCommand: tally.Command++; break;
            }
            var u = UnitById(B, step.Unit);
            if (step.Kind == Baskin.ActDrag && u.AtGate) tally.Gates++;
            // Menfezden sonra yetenek (yürüyüşü harcandı, yeteneği durur)
            if (step.Kind == Baskin.ActVent && step.Act && B.Phase == "player") Act(B, step, tally);
        }

        // ---------- Tur planı ----------
        List<Step> ChooseTurn(Baskin.BState B)
        {
            // Aşama 11: savunmada gövde kuralı yok (kazanınca ekibin odasında yatan çıkarılır; holdWin ve BodyCost kapalı)
            bodyRescue = !Baskin.IsDefense(B) && RescueFeasible(B);
            var ctx = MakeContext(B);
            var left = Baskin.Alive(B, 'p').Where(u => Baskin.CanAct(B, u)).Select(u => u.Id).ToList();
            var lines = new List<Line>();
            foreach (var first in Expand(B, left, ctx, Math.Max(1, Alternatives)))
            {
                var line = new Line { Pre = first.Pre, State = first.State, Steps = new List<Step> { first.Step }, Left = left.Where(id => id != first.Step.Unit).ToList() };
                while (line.Left.Count > 0 && line.State.Phase == "player")
                {
                    var next = Expand(line.State, line.Left, ctx, 1);
                    if (next.Count == 0) break;
                    line.State = next[0].State; line.Pre = next[0].Pre; line.Steps.Add(next[0].Step); line.Left.Remove(next[0].Step.Unit);
                }
                lines.Add(line);
            }
            if (lines.Count == 0) return new List<Step>();
            // Doğrulama: planın sonundaki durumda makine evresi kopyada gerçekten oynanır (zincirleme etkiler, ölen makinenin
            // boşa giden niyeti); yeni niyetlere bakılmaz. Kum fırtınasında yalnız kaba puan.
            Line pick = null;
            double pickPost = double.NegativeInfinity;
            foreach (var line in lines)
            {
                double post = line.Pre;
                if (line.State.Phase != "player") post = Score(line.State, ctx, false);
                else if (!B.Fog)
                {
                    var v = line.State.Clone();
                    // Tesis (karar D21): pusu hatları doğrulama kopyasında silinir (bot makine evresinin pusu vuruşunu önceden görmesin);
                    // pusunun değeri kaba beklentiyle eklenir
                    // Aşama 8: havlamanın etkisi yeni niyetlerdedir (doğrulama puanı bakmaz): kaba değeri de evreden önce alınır.
                    // Aygıtlar makine planından önce çalıştığı için (K3) kopyada açık kalır: gizli bilgi göstermez
                    double amb = v.Room != null ? AmbushValue(v) + BarkValue(v) : 0;
                    if (v.Room != null) foreach (var vu in v.Units) vu.AmbushDir = vu.AmbushX = vu.AmbushY = -1;
                    Baskin.ForecastTurn(v);
                    post = Score(v, ctx, false) + amb;
                }
                if (pick == null || post > pickPost + 1e-9 || Math.Abs(post - pickPost) <= 1e-9 && line.Pre > pick.Pre + 1e-9) { pick = line; pickPost = post; }
            }
            return pick.Steps;
        }

        /// <summary>Kalan uzmanların bütün seçenekleri (dur/yürü × yetenek yok/hedef); kaba puana göre en iyi <paramref name="keep"/> tanesi.</summary>
        List<Option> Expand(Baskin.BState S, List<int> left, Context ctx, int keep)
        {
            var top = new List<Option>();
            var smoke = SmokeCells(S);
            HashSet<int> reach = null;
            // Aşama 11: savunmada tutarken kapı karesi yürüyüş (ve sürükleme, menfez) adayı değildir: kapıda bekleyen tur sonunda tahtaya
            // döner, bütün ekip kapıdaysa tur yalnız GEÇ ile biter
            bool holdGate = Baskin.IsDefense(S) && !retreating;
            void Offer(double pre, Step step, Baskin.BState state)
            {
                if (holdGate && state.Units.Any(x => Baskin.IsSpecialist(x) && x.AtGate)) return;
                // Eşitlikte önce bulunan kalır
                int i = top.Count;
                while (i > 0 && pre > top[i - 1].Pre + 1e-9) i--;
                if (i >= keep) return;
                top.Insert(i, new Option { Pre = pre, Step = step, State = state });
                if (top.Count > keep) top.RemoveAt(keep);
            }
            foreach (int id in left)
            {
                var u = UnitById(S, id);
                if (u == null || !Baskin.CanAct(S, u)) continue;
                var spots = new List<Baskin.Pt> { new Baskin.Pt(u.X, u.Y) };
                if (Baskin.CanMove(S, u)) spots.AddRange(Baskin.Reachable(S, u));
                // Tesis: donanım hedefi sürerken Teknisyen kapıya yürümez (kapıya girse tahtadan kalkardı)
                bool keepTek = deviceGoal && u.Type == "tek" && DeviceIntact(S);
                for (int si = 0; si < spots.Count; si++)
                {
                    var p = spots[si];
                    if (si > 0 && keepTek && Baskin.IsGateTile(S, p.X, p.Y)) continue;
                    var M = S;
                    if (si > 0)
                    {
                        M = S.Clone();
                        if (!Baskin.DoMove(M, id, p.X, p.Y)) continue;
                    }
                    var step = new Step { Unit = id, Move = si > 0, MX = p.X, MY = p.Y };
                    Offer(Score(M, ctx, true), step, M);
                    if (M.Phase != "player") continue;
                    var mu = UnitById(M, id);
                    // Yürüyüşle kapıya giren (ya da tahliye edilen) uzman tahtada değil: yeteneği yok
                    if (!Baskin.OnBoard(mu)) continue;
                    foreach (var t in Useful(M, mu, smoke))
                    {
                        var C = M.Clone();
                        if (!Baskin.DoAct(C, id, t.X, t.Y)) continue;
                        var acted = step;
                        acted.Act = true; acted.AX = t.X; acted.AY = t.Y;
                        Offer(Score(C, ctx, true), acted, C);
                    }
                    // Tesis hamleleri (aşama 6): yetenek hedeflerinden sonra (eşitlikte eski aday kalır); eski tahtada hiç üretilmez
                    if (M.Room != null && M.Moves != null) ExpandActions(S, M, id, si, step, ctx, smoke, ref reach, Offer);
                }
            }
            return top;
        }

        /// <summary>
        /// Tesis hamlesi adayları (<see cref="Baskin.ActionKinds"/> sırasıyla): yeteneğin yerine geçenler (pusu, fırlatma, iki kişilik
        /// iş) her yürüyüş seçeneğinin üstünde; yürüyüş isteyenler (koşu darbesi, sürükleme, menfez) yalnız yürümeden. Pusu yalnız
        /// hattın bu tur bir makinenin varabileceği bir kareden geçtiği yönde; sürükleme yalnız "hep taşı"da; menfezden sonra ayrıca
        /// yetenek. <paramref name="reach"/>: makinelerin bu tur varabileceği kareler (ilk pusu adayında bir kez hesaplanır).
        /// </summary>
        void ExpandActions(Baskin.BState S, Baskin.BState M, int id, int si, Step step, Context ctx, HashSet<int> smoke, ref HashSet<int> reach,
            Action<double, Step, Baskin.BState> offer)
        {
            var mu = UnitById(M, id);
            // Aşama 8: "kullan"da ekipman ve birim eylemleri de (Baskin.AllActionKinds; ActionKinds'in altısı önde, eşitlikte eski aday
            // kalır); "hic"te yalnız aşama 6'nın altısı. Eski tahtada ve ekipmansız tahtada CanAction yenilerde hep false.
            foreach (var kind in Gear == "hic" ? Baskin.ActionKinds : Baskin.AllActionKinds)
            {
                var cost = Baskin.CostOf(kind);
                if (cost != Baskin.ActionCost.Ability && si > 0) continue;
                if (kind == Baskin.ActDrag && Fallen != "hep") continue;
                if (!Baskin.CanAction(M, mu, kind)) continue;
                // Havla yalnız erimde makine varken
                // Havla yalnız erimde makine varken ve köpek yük taşımıyorken (makineleri taşıyıcıya çekmesin)
                if (kind == Baskin.ActBark && (mu.Id == M.Carrier || !Baskin.Alive(M, 'e').Any(e => Math.Abs(e.X - mu.X) + Math.Abs(e.Y - mu.Y) <= M.Moves.BarkRange))) continue;
                // Yem yalnız tahtada makine varken
                if (kind == Baskin.ActDecoy && Baskin.Alive(M, 'e').Count == 0) continue;
                var bodies = kind == Baskin.ActDrag ? Baskin.DragBodies(M, mu) : new List<int> { 0 };
                foreach (int body in bodies)
                    foreach (var t in Baskin.ActionTargets(M, mu, kind, body))
                    {
                        // Aşama 8: yük köpeğe fırlatılmaz (2 canlı köpek taşıyıcı olunca düşüp yükü düşürüyordu; köpek yalnız yerdeki yükü
                        // getirir). Eski tahtada ve köpeksiz tahtada hiçbir aday düşmez
                        if (kind == Baskin.ActThrow && Baskin.IsDog(Baskin.UnitAt(M, t.X, t.Y))) continue;
                        if (kind == Baskin.ActAmbush)
                        {
                            reach = reach ?? Reach(S);
                            if (!LineHits(M, mu.X, mu.Y, t.X - mu.X, t.Y - mu.Y, Baskin.AmbushRangeFor(M, mu), reach)) continue;
                        }
                        // Engel: makinenin bu tur varabileceği kare ya da (kalas) bir niyetin hattı
                        if (kind == Baskin.ActWire || kind == Baskin.ActPlank)
                        {
                            reach = reach ?? Reach(S);
                            if (!reach.Contains(t.Y * N + t.X) && !(kind == Baskin.ActPlank && smoke.Contains(t.Y * N + t.X))) continue;
                        }
                        // Taret: hattı bir makinenin varabileceği kareden geçen yön
                        if (kind == Baskin.ActTurret)
                        {
                            reach = reach ?? Reach(S);
                            if (!TurretLineHits(M, t.X, t.Y, t.X - mu.X, t.Y - mu.Y, M.Moves.TurretRange, reach)) continue;
                        }
                        // Vinç: hattın ilk dolu karesi gövde, yük ya da birim (makine ya da ekip; çekişin bu evredeki sonucu doğrulamada görülür)
                        if (kind == Baskin.ActWinch && WinchFirst(M, t.X, t.Y, t.X - mu.X, t.Y - mu.Y, M.Moves.WinchRange) == 0) continue;
                        var C = M.Clone();
                        if (!Baskin.DoAction(C, id, kind, t.X, t.Y, body)) continue;
                        var st = step;
                        st.Kind = kind;
                        st.Other = body;
                        if (cost == Baskin.ActionCost.Ability || kind == Baskin.ActRush) { st.Act = true; st.AX = t.X; st.AY = t.Y; }
                        else { st.Move = true; st.MX = t.X; st.MY = t.Y; }
                        offer(Score(C, ctx, true), st, C);
                        if (kind != Baskin.ActVent || C.Phase != "player") continue;
                        var cu = UnitById(C, id);
                        if (!Baskin.OnBoard(cu)) continue;
                        foreach (var a in Useful(C, cu, smoke))
                        {
                            var D = C.Clone();
                            if (!Baskin.DoAct(D, id, a.X, a.Y)) continue;
                            var sa = st;
                            sa.Act = true; sa.AX = a.X; sa.AY = a.Y;
                            offer(Score(D, ctx, true), sa, D);
                        }
                    }
            }
        }

        /// <summary>Makinelerin bu tur (makine evresinin planında) varabileceği kareler: bulundukları kare ve yürüyüşleri (y*8+x).</summary>
        static HashSet<int> MachineReach(Baskin.BState S)
        {
            var o = new HashSet<int>();
            foreach (var e in Baskin.Alive(S, 'e'))
            {
                o.Add(e.Y * N + e.X);
                foreach (var p in Baskin.Reachable(S, e)) o.Add(p.Y * N + p.X);
            }
            return o;
        }

        /// <summary>Aday süzgeçlerinin erişimi (pusu, engel, taret): makinelerin erişimi; savunmada ayrıca sonraki tur gelecek makinelerin
        /// hayalet erişimi (<see cref="GhostReach"/>, <see cref="Weights.WaveGhost"/> &gt; 0). Saldırıda <see cref="MachineReach"/>'in aynısı.</summary>
        HashSet<int> Reach(Baskin.BState S)
        {
            var o = MachineReach(S);
            if (Baskin.IsDefense(S) && W.WaveGhost > 0) o.UnionWith(GhostReach(S));
            return o;
        }

        /// <summary>Aşama 11: sonraki tur gelecek makineler (<see cref="Baskin.DefenseIncoming"/>; tahmini yerleşim karesi olanlar) ve
        /// türlerinin yürüyüşü.</summary>
        static List<(Baskin.Pt at, int move)> Ghosts(Baskin.BState S)
        {
            var o = new List<(Baskin.Pt, int)>();
            if (!Baskin.IsDefense(S) || S.Phase != "player") return o;
            foreach (var v in Baskin.DefenseIncoming(S))
                if (v.Turn == S.Turn + 1 && v.X >= 0 && Baskin.UnitTypes.TryGetValue(v.Type, out var ut))
                    o.Add((new Baskin.Pt(v.X, v.Y), ut.Move));
            return o;
        }

        /// <summary>Hayalet makinelerin erişimi: karesi ve türünün yürüyüşüyle varabileceği kareler (duvar ve nesne geçilmez, birimler
        /// sayılmaz; y*8+x).</summary>
        static HashSet<int> GhostReach(Baskin.BState S)
        {
            var o = new HashSet<int>();
            foreach (var (at, move) in Ghosts(S))
            {
                var d = new Dictionary<int, int> { [at.Y * N + at.X] = 0 };
                var q = new Queue<int>();
                q.Enqueue(at.Y * N + at.X);
                while (q.Count > 0)
                {
                    int k = q.Dequeue(), x = k % N, y = k / N;
                    o.Add(k);
                    if (d[k] >= move) continue;
                    foreach (var dd in Baskin.Dirs)
                    {
                        int nx = x + dd[0], ny = y + dd[1];
                        if (!Inb(nx, ny) || d.ContainsKey(ny * N + nx) || S.Cells[ny, nx].T == "wall" || S.Cells[ny, nx].O != null) continue;
                        d[ny * N + nx] = d[k] + 1;
                        q.Enqueue(ny * N + nx);
                    }
                }
            }
            return o;
        }

        /// <summary>(x, y)'den (dx, dy) yönünde <paramref name="range"/> karelik hat (duvarda, nesnede, söndürme bulutunda ve tahta
        /// dışında biter: <see cref="Baskin.AmbushLine"/>'ın kuralı) bir makinenin varabileceği bir kareden geçiyor mu.</summary>
        internal static bool LineHits(Baskin.BState S, int x, int y, int dx, int dy, int range, HashSet<int> reach)
        {
            for (int k = 1; k <= range; k++)
            {
                int px = x + dx * k, py = y + dy * k;
                if (!Inb(px, py) || S.Cells[py, px].T == "wall" || S.Cells[py, px].O != null) return false;
                if (S.Ev != null && S.Ev.Id == "bulut" && S.Cells[py, px].Cloud) return false;
                if (reach.Contains(py * N + px)) return true;
            }
            return false;
        }

        /// <summary>Aşama 8, taretin hattı (motorun kuralı: tahta dışı, duvar, tel dışındaki nesne ya da bulutta biter; ilk ayakta birim
        /// ekip tarafındaysa atmaz) bir makinenin varabileceği kareden geçiyor mu. (x, y) taretin karesi.</summary>
        internal static bool TurretLineHits(Baskin.BState S, int x, int y, int dx, int dy, int range, HashSet<int> reach)
        {
            for (int k = 1; k <= range; k++)
            {
                int px = x + dx * k, py = y + dy * k;
                if (!Inb(px, py) || S.Cells[py, px].T == "wall" || S.Cells[py, px].O is Baskin.Obj o && o.K != "tel") return false;
                if (S.Ev != null && S.Ev.Id == "bulut" && S.Cells[py, px].Cloud) return false;
                if (reach.Contains(py * N + px)) return true;
                var t = Baskin.UnitAt(S, px, py);
                if (t != null && t.Side == 'p') return false;
            }
            return false;
        }

        /// <summary>Aşama 8, vincin hattının ilk dolu karesi (motorun kuralı: tahta dışı, duvar ya da her nesnede biter; aygıt hattı keser):
        /// 0 yok (ya da aygıt), 1 yatan gövde ya da yerdeki yük, 2 makine, 3 ekip birimi (vinç arkadaşını da çeker: kaçışta kapıya yaklaştırır).
        /// (x, y) vincin karesi.</summary>
        internal static int WinchFirst(Baskin.BState S, int x, int y, int dx, int dy, int range)
        {
            for (int k = 1; k <= range; k++)
            {
                int px = x + dx * k, py = y + dy * k;
                if (!Inb(px, py) || S.Cells[py, px].T == "wall" || S.Cells[py, px].O != null) return 0;
                var t = Baskin.UnitAt(S, px, py);
                if (t != null) return Baskin.IsGadget(t) ? 0 : t.Side == 'e' ? 2 : 3;
                if (S.CargoX == px && S.CargoY == py || S.Units.Any(b => Baskin.LyingBody(b) && b.X == px && b.Y == py)) return 1;
            }
            return 0;
        }

        /// <summary>Aşama 8, havlayan köpeklerin kaba değeri: erimindeki (Manhattan) en çok iki makine başına <see cref="Weights.Bark"/>
        /// (makineler bu planda köpeği ekip gibi puanlar; yeni niyetler doğrulamada görülmez, havla planın sonunda silinir).</summary>
        double BarkValue(Baskin.BState C)
        {
            if (C.Room == null || C.Phase != "player" || C.Moves == null) return 0;
            double v = 0;
            foreach (var d in C.Units)
            {
                if (!Baskin.IsDog(d) || !d.Bark || !Baskin.OnBoard(d)) continue;
                int n = Baskin.Alive(C, 'e').Count(e => Math.Abs(e.X - d.X) + Math.Abs(e.Y - d.Y) <= C.Moves.BarkRange);
                v += W.Bark * Math.Min(2, n);
            }
            return v;
        }

        /// <summary>
        /// Aşama 8, uzman olmayan ekip birimlerinin puanı (yalnız tesiste; eski tahtada Class hep null). Tutsak: makine bedelinin
        /// <see cref="Weights.CaptiveShare"/> payı (kalan süresi ≤ 1 iken tam: yeniden makine olacak). Köpek: canı, düşmesi (taşıyıcıysa
        /// yük yere düşer), koklamadan önce ileri geçide uzaklığı. Aygıt: sonraki turların kaba değeri (<see cref="GadgetValue"/>).
        /// </summary>
        double UnitTerm(Baskin.BState C, Baskin.Unit u, int hp, int turnsLeft)
        {
            if (Baskin.IsCaptive(u))
                return hp > 0 ? -(W.Enemy + W.EnemyHp * hp + W.EnemyArmor * u.Armor) * (u.Turns <= 1 ? 1 : W.CaptiveShare) : 0;
            if (Baskin.IsDog(u))
            {
                if (hp <= 0) return -W.DogDown - (u.Id == C.Carrier && !u.Left ? W.CargoDrop : 0);
                double v = W.DogHp * hp;
                if (C.Room != null && !C.Escape && C.Sniffed == 0 && C.Room.Forward.Count > 0 && Baskin.OnBoard(u))
                {
                    var gate = Field(C, C.Room.Forward, -1, -1);
                    int d = gate[u.Y, u.X] < 0 ? Far : gate[u.Y, u.X];
                    v -= W.SniffWalk * Math.Max(0, d - 1) / (double)Mv(C, u);
                }
                return v;
            }
            if (Baskin.IsGadget(u)) return hp > 0 && Baskin.OnBoard(u) ? GadgetValue(C, u, turnsLeft) : 0;
            return 0;
        }

        /// <summary>Aygıtın sonraki turlarının kaba değeri (min(kalan tur, odanın kalan turu) başına): taret hattı makine erişiminden
        /// geçiyorsa, yem yakınındaki makine başına (en çok iki), vinç hattının ilk dolu karesi gövde ya da yükse.</summary>
        double GadgetValue(Baskin.BState C, Baskin.Unit g, int turnsLeft)
        {
            int left = Math.Min(g.Turns, Math.Max(0, turnsLeft));
            if (left <= 0 || g.Dir < 0 || C.Moves == null) return 0;
            var d = Baskin.Dirs[g.Dir];
            switch (g.Type)
            {
                case "taret":
                    if (TurretLineHits(C, g.X, g.Y, d[0], d[1], C.Moves.TurretRange, MachineReach(C))) return W.Turret * left;
                    // Aşama 11: savunmada sonraki tur gelecek makineye (hayalet) payıyla; saldırıda boş
                    return Baskin.IsDefense(C) && W.WaveGhost > 0 && TurretLineHits(C, g.X, g.Y, d[0], d[1], C.Moves.TurretRange, GhostReach(C))
                        ? W.Turret * left * W.WaveGhost : 0;
                case "yem": return W.Decoy * left * Math.Min(2, Baskin.Alive(C, 'e').Count(e => Math.Abs(e.X - g.X) + Math.Abs(e.Y - g.Y) <= 5));
                case "vinc": return WinchFirst(C, g.X, g.Y, d[0], d[1], C.Moves.WinchRange) == 1 ? W.Winch * left : 0;
                default: return 0;
            }
        }

        /// <summary>
        /// Aşama 8, engellerin (tel, kalas, hurda) değeri: tahtadaki her makinenin uzmanlara gerçek yürüyüş uzaklığı (duvar ve nesne
        /// geçilmez, birimler sayılmaz) engeller yüzünden ne kadar uzadıysa adım başına <see cref="Weights.Barrier"/> (makine başına en
        /// çok 3; ulaşamayan 3). Engelsiz tahtada 0.
        /// </summary>
        double BarrierValue(Baskin.BState C)
        {
            if (C.Room == null) return 0;
            bool any = false;
            for (int y = 0; y < N && !any; y++) for (int x = 0; x < N; x++) if (C.Cells[y, x].O is Baskin.Obj o && Baskin.IsBarrier(o.K)) { any = true; break; }
            if (!any) return 0;
            var sources = C.Units.Where(u => Baskin.IsSpecialist(u) && Baskin.OnBoard(u)).Select(u => u.Y * N + u.X).ToList();
            if (sources.Count == 0) return 0;
            var shut = TeamSteps(C, sources, false);
            var open = TeamSteps(C, sources, true);
            double v = 0;
            foreach (var e in Baskin.Alive(C, 'e'))
            {
                int k = e.Y * N + e.X;
                if (open[k] < 0) continue;
                v += W.Barrier * (shut[k] < 0 ? 3 : Math.Min(3, shut[k] - open[k]));
            }
            // Aşama 11: savunmada sonraki tur gelecek makine (hayalet) giriş karesinde, payıyla
            if (Baskin.IsDefense(C) && W.WaveGhost > 0)
                foreach (var (at, _) in Ghosts(C))
                {
                    int k = at.Y * N + at.X;
                    if (open[k] < 0) continue;
                    v += W.WaveGhost * W.Barrier * (shut[k] < 0 ? 3 : Math.Min(3, shut[k] - open[k]));
                }
            return v;
        }

        /// <summary>Uzmanlardan adım uzaklığı (BFS; duvar ve nesne geçilmez, <paramref name="ignoreBarriers"/> iken engel geçilir; birimler
        /// sayılmaz; ulaşılmaz −1).</summary>
        static int[] TeamSteps(Baskin.BState C, List<int> sources, bool ignoreBarriers)
        {
            var d = new int[N * N];
            for (int i = 0; i < d.Length; i++) d[i] = -1;
            var q = new Queue<int>();
            foreach (int s in sources) if (d[s] < 0) { d[s] = 0; q.Enqueue(s); }
            while (q.Count > 0)
            {
                int k = q.Dequeue(), x = k % N, y = k / N;
                foreach (var dd in Baskin.Dirs)
                {
                    int nx = x + dd[0], ny = y + dd[1];
                    if (!Inb(nx, ny)) continue;
                    int n = ny * N + nx;
                    if (d[n] >= 0) continue;
                    var c = C.Cells[ny, nx];
                    if (c.T == "wall" || c.O != null && !(ignoreBarriers && Baskin.IsBarrier(c.O.K))) continue;
                    d[n] = d[k] + 1;
                    q.Enqueue(n);
                }
            }
            return d;
        }

        /// <summary>Kurulu pusuların kaba değeri: hattı (<see cref="Baskin.AmbushLine"/>) bu tur bir makinenin varabileceği kareden
        /// geçen her pusu <see cref="Weights.Ambush"/>.</summary>
        double AmbushValue(Baskin.BState C)
        {
            if (C.Room == null || C.Phase != "player") return 0;
            HashSet<int> reach = null, ghost = null;
            double v = 0;
            foreach (var u in C.Units)
            {
                if (!Baskin.IsSpecialist(u) || u.AmbushDir < 0 || !Baskin.OnBoard(u)) continue;
                reach = reach ?? MachineReach(C);
                // Aşama 10: makine aklı (aygıt 2. kademe) hattı görür ve kaçınır: beklenti küçülür (önlemsiz emirde tam)
                double full = C.Moves != null && C.Moves.Jam >= 2 ? W.Ambush * W.AmbushSeen : W.Ambush;
                if (Baskin.AmbushLine(C, u).Any(p => reach.Contains(p.Y * N + p.X))) v += full;
                // Aşama 11: savunmada sonraki tur gelecek makineye (hayalet) payıyla; saldırıda hiç çalışmaz
                else if (Baskin.IsDefense(C) && W.WaveGhost > 0)
                {
                    ghost = ghost ?? GhostReach(C);
                    if (Baskin.AmbushLine(C, u).Any(p => ghost.Contains(p.Y * N + p.X))) v += full * W.WaveGhost;
                }
            }
            return v;
        }

        /// <summary>Aşama 10, koku yayıcının bedeli: tahtada ayakta olup erimdeki yayıcının susturduğu her köpek
        /// <see cref="Weights.Emitter"/> (<see cref="Baskin.EmitterBlocks"/>). Yayıcısız emirde (köpek önlemi yok) ve "hic"te 0.</summary>
        double EmitterCost(Baskin.BState C)
        {
            if (C.Room == null || C.Moves == null || C.Moves.Emitter <= 0 || Gear == "hic") return 0;
            double v = 0;
            foreach (var d in C.Units)
                if (Baskin.IsDog(d) && !d.Dead && d.Hp > 0 && Baskin.OnBoard(d) && Baskin.EmitterBlocks(C, d)) v += W.Emitter;
            return v;
        }

        /// <summary>
        /// Aşama 10, botun sayaçlarından önlem sınıflarının sayımı (<see cref="FacilityCounters.Classes"/> sırası; Core'un tahta sayımıyla
        /// karşılaştırmak için, Camp8 <c>tlb</c>): itme = koşu darbesi + vincin makine çekişi, yıkım = lokum, örme = tel + kalas, ele = ele
        /// geçirme, köpek = havla + getir + kokla, aygıt = taret + yem + vinç (+ <paramref name="ambush"/> iken pusu isabeti). Emirdeki
        /// kademesi 2 olan sınıf 0 (Core saymaz). Tavansız.
        /// </summary>
        public static int[] TallyMap(IEnumerable<Tally> rooms, IList<int> levels, bool ambush)
        {
            var o = new int[FacilityCounters.Classes.Length];
            foreach (var t in rooms)
            {
                o[0] += t.Rush + t.MachinePulls;
                o[1] += t.Charge;
                o[2] += t.Wire + t.Plank;
                o[3] += t.Captures;
                o[4] += t.Bark + t.Fetch + t.Sniff;
                o[5] += t.Turret + t.Decoy + t.Winch + (ambush ? t.AmbushHits : 0);
            }
            for (int i = 0; i < o.Length; i++) if (levels != null && i < levels.Count && levels[i] >= 2) o[i] = 0;
            return o;
        }

        /// <summary>Denemeye değer yetenek hedefleri: EMP yalnız birim ya da nesne olan kareye veya birimin yanına, sis yalnız bir
        /// niyetin geçtiği ya da vurduğu kareye; öteki yeteneklerin hedefleri zaten az.</summary>
        IEnumerable<Baskin.Pt> Useful(Baskin.BState M, Baskin.Unit u, HashSet<int> smoke)
        {
            string ability = Baskin.UnitTypes[u.Type].Ability;
            foreach (var t in Baskin.Targets(M, u))
            {
                // Sürüm 10: bitişik ikmal kasası her uzmanın hedefidir (sis ve EMP süzgeci onu düşürmez)
                bool cabinet = Baskin.IsLoot(M) && Baskin.CellAt(M, t.X, t.Y).O is Baskin.Obj k && Baskin.IsTarget(k.K) && Math.Abs(t.X - u.X) + Math.Abs(t.Y - u.Y) == 1;
                if (cabinet) { yield return t; continue; }
                // Tesis, politika "hic": donanım karesi sabotaj, EMP ve dinamit hedefi değil
                if (Devices == "hic" && M.Room != null && Baskin.CellAt(M, t.X, t.Y).O is Baskin.Obj o && Baskin.IsDevice(o.K)) continue;
                if (ability == "emp" && !EmpMatters(M, t.X, t.Y)) continue;
                if (ability == "duman" && !smoke.Contains(t.Y * N + t.X)) continue;
                yield return t;
            }
        }

        static bool EmpMatters(Baskin.BState M, int x, int y)
        {
            if (Baskin.UnitAt(M, x, y) != null) return true;
            var o = Baskin.CellAt(M, x, y).O;
            if (o != null && !Baskin.IsTarget(o.K)) return true;
            foreach (var d in Baskin.Dirs)
                if (Inb(x + d[0], y + d[1]) && Baskin.UnitAt(M, x + d[0], y + d[1]) != null) return true;
            return false;
        }

        /// <summary>Niyetlerin geçtiği kareler: atış ve kilit hattı ilk duvara ya da nesneye kadar (birimler yer değiştirir, sayılmaz),
        /// yakın vuruşun ve havanın hedefi.</summary>
        static HashSet<int> SmokeCells(Baskin.BState S)
        {
            var cells = new HashSet<int>();
            foreach (var iv in Baskin.Intents(S))
            {
                if (iv.Kind == "shoot" || iv.Kind == "lock")
                {
                    int range = iv.Kind == "lock" ? 4 : N;
                    for (int k = 1; k <= range; k++)
                    {
                        int x = iv.FromX + Baskin.Dirs[iv.Dir][0] * k, y = iv.FromY + Baskin.Dirs[iv.Dir][1] * k;
                        if (!Inb(x, y)) break;
                        cells.Add(y * N + x);
                        if (Baskin.CellAt(S, x, y).T == "wall" || Baskin.CellAt(S, x, y).O != null) break;
                    }
                }
                else if (iv.Kind == "flame" || iv.Kind == "ram" || iv.Kind == "pull" || iv.Kind == "frost")
                    foreach (var a in iv.Area) cells.Add(a.Y * N + a.X);
                else if ((iv.Kind == "melee" || iv.Kind == "lob") && iv.HasTarget) cells.Add(iv.TargetY * N + iv.TargetX);
            }
            return cells;
        }

        // ---------- Puan ----------
        /// <param name="before">Makine evresinden önce (niyetlerden kaba tahmin) ya da sonra (gerçek evre oynandı).</param>
        double Score(Baskin.BState C, Context ctx, bool before)
        {
            var f = before && C.Phase == "player" ? Predict(C) : null;
            bool loot = Baskin.IsLoot(C);
            // Tesis odası (aşama 6 terimleri yalnız burada; eski tahtada puan birebir aynı)
            bool fac = C.Room != null;
            double s = 0;
            Baskin.Unit tek = null, miner = null;
            bool anyAlive = false, carrierDown = false, tekLocked = false, tekOut = false;
            var inside = new List<(Baskin.Unit u, bool locked)>();
            int unitTurns = before ? C.MaxTurns - C.Turn : C.MaxTurns - C.Turn + 1;
            for (int i = 0; i < C.Units.Count; i++)
            {
                var u = C.Units[i];
                int hp = u.Dead ? 0 : u.Hp - (f?.Dmg[i] ?? 0);
                if (u.Side != 'p')
                {
                    if (hp > 0) s -= W.Enemy + W.EnemyHp * hp + W.EnemyArmor * u.Armor;
                    continue;
                }
                // Aşama 8: köpek, aygıt ve tutsak uzman değildir (can, yol, kilit ve "ekip ayakta" sayımına girmez)
                if (u.Class != null) { s += UnitTerm(C, u, hp, unitTurns); continue; }
                bool isTek = u.Type == "tek";
                // Aşama 12 (Katar): açık kenardan düşen uzman gövde değildir, yatar (ölmez); bedeli politikadan. Taşıyıcının yükü kenar
                // karesinde kalır (düşen taşıyıcının dalı)
                if (u.Overboard)
                {
                    s -= EdgeCost(isTek);
                    if (u.Id == C.Carrier) { if (fac && !u.Left) s -= W.CargoDrop; else carrierDown = true; }
                    continue;
                }
                if (hp <= 0)
                {
                    s -= isTek ? W.TekDown : W.Down;
                    // Tesis: düşen taşıyıcının yükü yere düşer (alınabilir); süre dolarken tahtada kalan taşıyıcıyla yük gider
                    if (u.Id == C.Carrier) { if (fac && !u.Left) s -= W.CargoDrop; else carrierDown = true; }
                    continue;
                }
                anyAlive = true;
                int plate = u.Armor - (f?.Plate[i] ?? 0);
                s += (isTek ? W.TekHp : W.Hp) * hp + W.Plate * plate - (hp == 1 ? isTek ? W.TekLow : W.Low : 0);
                // Tahliye edilen (tesiste kapıda bekleyen) uzman güvende: canı sayılır, yolu ve kilidi yok
                if (u.Out || u.AtGate) { if (isTek) tekOut = true; continue; }
                bool locked = f != null ? f.Locked[i] : u.Locked;
                inside.Add((u, locked));
                if (isTek) { tek = u; tekLocked = locked; }
                else if (locked) s -= W.Locked;
                if (u.Type == "maden" && u.Charges != 0) miner = u;
            }
            // Tesis sürüm 10: ikmal kasasını her uzman açar; Teknisyen yoksa kasaya ilk ayaktaki uzman yürür (rota ve süre onun)
            if (loot && tek == null && !tekOut && C.Carrier == 0 && inside.Count > 0) { tek = inside[0].u; tekLocked = inside[0].locked; }
            if (f != null) s += W.Spawner * f.SpawnersBroken - W.NewEnemy * (f.NewEnemies + 0.5 * f.NewBarriers) - W.Civilian * f.Collateral;
            s -= W.Civilian * C.Collateral;
            int targetHp = 0;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var o = C.Cells[y, x].O;
                    if (o == null) continue;
                    if (o.K == "spawner") s -= W.Spawner;
                    else if (Baskin.IsTarget(o.K)) targetHp += o.Hp;
                }
            if (fac) s += JointValue(C) + (before ? AmbushValue(C) + BarkValue(C) : 0) + BarrierValue(C) + (C.Sniffed != 0 ? W.Sniff : 0);
            // Aşama 10: köpeği susturan koku yayıcı (yalnız köpek önlemli emirde; önlemsiz tahtada puan aynı)
            if (fac && C.Moves != null && C.Moves.Emitter > 0) s -= EmitterCost(C);
            // Aşama 11: savunma tahtası (görev yolu, ilerleme ve gövde terimi yok); saldırı tahtasında hiç çalışmaz
            if (fac && Baskin.IsDefense(C)) return DefenseScore(C, f, s, anyAlive, tek, tekLocked, inside, before);
            // "Hep taşı": turun başında kurtarılabilen gövde yatarken GEÇ ve tesisin girişinden son çıkış kazanç değildir (gövde
            // bırakılır); puan aşağıda gövdeyi sürükleyecek kimse yokmuş gibi sürer (BodyCost: Abandon)
            bool holdWin = fac && bodyRescue && BodyRoom(C) && C.Units.Any(Lying);
            if (C.Phase == "won" && !holdWin) return W.Win + s - (fac ? Abandoned(C) : 0);
            // Tesis, ikmal kaçışı: görev yükle biter; yük taşıyıcıyla (ya da yerde, alınabilir) değilse gitti. Fırlatmadan sonra
            // Teknisyen düşse de yük başkasındadır. Kaçıştan önce sunucuda bugünkü kural (Teknisyen gerekir); ikmalde sürüm 10'dan beri
            // kasayı her uzman açar (ayakta uzman varken baskın sürer).
            bool cargoGone = fac && loot && C.Escape && C.Carrier == 0 && C.CargoX < 0;
            bool doomed = !fac ? loot ? carrierDown : tek == null && !tekOut
                : loot && C.Escape ? carrierDown || cargoGone
                : !loot && tek == null && !tekOut;
            if (C.Phase == "lost" || !anyAlive || f != null && f.CrateLost || doomed && !C.Escape) return W.Loss + s;
            // Tesis odası geçildi ya da GEÇ verilebilir (herkes kapıda; EndTurn bu durumda tur bitirmez, kopyadaki evre denemesi
            // tahtayı değiştirmez): oda kazanılmış gibi. Tesis: yerde bırakılan yük ve (hep taşı) odada yatan gövde bedelli.
            if ((C.Phase == "pass" || Baskin.CanPass(C)) && !holdWin)
                return ((doomed || fac && loot && C.Escape && C.Carrier == 0) && loot ? W.Loss : 0) + W.Win + s - (fac ? Abandoned(C) : 0);
            // Kaçışta Teknisyen düştüyse baskın sürer: ikmalde görev gitti, sunucuda zaten kazanıldı; öteki uzmanlar yine çıkmalı
            if (doomed && loot) s += W.Loss;
            int turnsLeft = before ? C.MaxTurns - C.Turn : C.MaxTurns - C.Turn + 1;
            if (fac) s -= BodyCost(C, inside, turnsLeft, before);
            // Tesisin giriş ve ara odası (gidiş): hedef ileri geçit; süre dolarsa ekip geri çekilir (baskın kaybedilir)
            if (C.Room != null && !C.Escape && C.Room.Forward.Count > 0) return GateScore(C, s, tek, tekLocked, inside, turnsLeft, before);
            // Öteki uzmanlar baskın bitmeden çıkışa dönmeli (kaçış): yolları her evrede aynı ağırlıkla sayılır, kasayı açmak ya da
            // hedefi kırmak puanı sıçratmaz; süreye sığmayan düşmüş sayılır
            var exits = Field(C, C.Exits, -1, -1);
            // Tesis, ikmal kaçışı: tam ağırlıklı yol Teknisyen'in değil yükün (taşıyıcının ya da yerdeki yükün) yoludur
            bool cargoKey = fac && loot && C.Escape;
            foreach (var (u, _) in inside)
            {
                if (cargoKey ? u.Id == C.Carrier : u.Type == "tek") continue;
                int d = exits[u.Y, u.X] < 0 ? Far : exits[u.Y, u.X], mv = Mv(C, u);
                s -= W.Progress * W.Others * d / mv + (Ceil(d, mv) > TurnsFor(C, u, turnsLeft, before) ? W.Down : 0);
            }
            if (cargoKey) return s - CargoCost(C, exits, inside, turnsLeft, before);
            if (C.Escape)
            {
                // Kaçışta Teknisyen'in yolu çıkışa (taşıyıcı ya da kırılan hedefin yanından); çıktıysa yolu yok
                if (tek == null) return s;
                int d = exits[tek.Y, tek.X] < 0 ? Far : exits[tek.Y, tek.X], tm = Mv(C, tek);
                s -= W.Progress * d / (double)tm + W.Late * Math.Max(0, Ceil(d, tm) - TurnsFor(C, tek, turnsLeft, before));
                if (tekLocked && d > 0) s -= W.TekLocked;
                return s;
            }
            if (turnsLeft <= 0) return W.Loss + s;

            var route = RouteOf(C, tek, -1, -1);
            bool carrying = loot && C.Carrier != 0;
            double need;
            int needTurns, tk = Mv(C, tek);
            double tkd = tk;
            if (!loot)
            {
                // Hedef kırılınca Teknisyen o tur yürüyemez; ardından çıkışa döner (kaçış)
                int h = targetHp + (f?.Repairs ?? 0);
                need = route.D1 / tkd + h / 2.0 + route.D2 / tkd;
                needTurns = (route.D1 == 0 ? Ceil(h, 2) : Ceil(route.D1, tk) + Ceil(h, 2) - 1) + Ceil(route.D2, tk);
            }
            else if (!carrying)
            {
                // Kasa açılınca Teknisyen o tur yürüyemez
                need = route.D1 / tkd + route.D2 / tkd + 0.5;
                needTurns = (route.D1 == 0 ? 1 : Ceil(route.D1, tk)) + Ceil(route.D2, tk);
            }
            else
            {
                need = route.D2 / tkd;
                needTurns = Ceil(route.D2, tk);
            }
            s -= W.Progress * need + W.Late * Math.Max(0, needTurns - TurnsFor(C, tek, turnsLeft, before));
            if (tekLocked && (carrying ? route.D2 : route.D1) > 0) s -= W.TekLocked;
            if (ctx.HasBreach && miner != null && C.Cells[ctx.BY, ctx.BX].T == "wall")
            {
                int dm = ctx.Field[miner.Y, miner.X];
                if (dm >= 0) s += W.Progress * ctx.Gain / 3.0 * W.Breach * Math.Max(0, 1 - dm / 6.0);
            }
            return s;
        }

        /// <summary>
        /// Aşama 11, savunma tahtasının puanı: bugünkü can, zırh, makine ve tesis terimleri (<paramref name="s"/>) + saha rölesinin canı
        /// (<see cref="Weights.Relay"/>; makine evresinden önceki kaba puanda niyetlerin röleye hasarı düşülür, kırılırsa kayıp). Evre "won"
        /// (süre doldu, ekip ayakta) kazanç, "lost" ya da ayakta uzman yoksa kayıp; GEÇ (yalnız çekilmede olur) oda kazanılmış gibi. İçeri
        /// çekilirken (<see cref="Retreat"/>) giriş ve ara odada ileri geçide yol <see cref="GateScore"/>'un kuralıyla. İlerleme, görev yolu,
        /// <c>holdWin</c> ve <see cref="BodyCost"/> yok.
        /// </summary>
        double DefenseScore(Baskin.BState C, Forecast f, double s, bool anyAlive, Baskin.Unit tek, bool tekLocked,
            List<(Baskin.Unit u, bool locked)> inside, bool before)
        {
            if (C.Phase == "lost" || !anyAlive) return W.Loss + s;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var o = C.Cells[y, x].O;
                    if (o == null || o.K != Baskin.RelayKind) continue;
                    int hp = o.Hp - (f?.ObjDmg[y * N + x] ?? 0);
                    if (hp <= 0) return W.Loss + s;
                    s += W.Relay * hp;
                }
            if (C.Phase == "won" || C.Phase == "pass" || Baskin.CanPass(C)) return W.Win + s;
            if (retreating && C.Room.Forward.Count > 0)
                return GateScore(C, s, tek, tekLocked, inside, before ? C.MaxTurns - C.Turn : C.MaxTurns - C.Turn + 1, before);
            return s;
        }

        // ---------- Tesis hamleleri (aşama 6) puan terimleri; yalnız tesis odasında çağrılır ----------
        /// <summary>Açılmış büyük sandık ve iki kişilik işin tutuşu (ikinci uzman bu tur iş karesinin yanına varabiliyorsa); makine
        /// evresinden önceki kaba puanda kurulu pusuların beklentisi (<see cref="AmbushValue"/>; doğrulamada ayrıca eklenir).</summary>
        double JointValue(Baskin.BState C)
        {
            double v = C.CrateOpen ? W.Crate : 0;
            if (C.Phase != "player") return v;
            if (C.HoldId != 0 && SecondCanCome(C)) v += C.Cells[C.HoldY, C.HoldX].Crate ? W.Crate * 0.5 : W.Hold;
            return v;
        }

        /// <summary>Tutulan işe bu tur ikinci bir uzman gelebilir: tahtada, yeteneği duran ve iş karesinin sekiz komşusunda olan ya da
        /// oraya yürüyebilen.</summary>
        static bool SecondCanCome(Baskin.BState C)
        {
            foreach (var u in C.Units)
            {
                if (u.Id == C.HoldId || !Baskin.IsSpecialist(u) || !Baskin.CanAct(C, u)) continue;
                if (Near(u.X, u.Y, C.HoldX, C.HoldY)) return true;
                if (Baskin.CanMove(C, u) && Baskin.Reachable(C, u).Any(p => Near(p.X, p.Y, C.HoldX, C.HoldY))) return true;
            }
            return false;
        }

        static bool Near(int x, int y, int tx, int ty) => Math.Max(Math.Abs(x - tx), Math.Abs(y - ty)) == 1;

        /// <summary>"Hep taşı" terimlerinin odası: kaçış (hedef odası ve dönüş) ve hedef odası. Gidişin giriş ve ara odasında yatan
        /// gövde dönüşte yeniden tahtaya konur (oda temizse toplanır): ileri taşımak onu hedef odasından geçirip geri getirmek olurdu.</summary>
        static bool BodyRoom(Baskin.BState C) => C.Escape || C.Room.Type == RoomType.Target;

        /// <summary>Gövdenin bu odadan götürüleceği kapı: kaçışta (ve geçidi olmayan hedef odada) batı girişi, gidişte ileri geçit.</summary>
        static IEnumerable<Baskin.Pt> BodyGoal(Baskin.BState C) => C.Escape || C.Room.Forward.Count == 0 ? C.Room.West : C.Room.Forward;

        /// <summary>"Hep taşı": kurtarılamayan (turun başında süreye sığmayan) gövdeyle oda geçilirken tahtada yatan her gövde
        /// <see cref="Weights.Abandon"/>.</summary>
        double Abandoned(Baskin.BState C)
        {
            if (Fallen != "hep" || !BodyRoom(C)) return 0;
            int n = C.Units.Count(Lying);
            return n * W.Abandon;
        }

        /// <summary>Odada bırakılacak gövde: tahtada yatıyor ve son nefeste ekiple gitmiyor (<see cref="Baskin.Unit.Breath"/>; doğrulama
        /// kopyasındaki makine evresi odayı bitirdiyse yazılır).</summary>
        static bool Lying(Baskin.Unit u) => Baskin.LyingBody(u) && !u.Breath;

        /// <summary>
        /// Turun başında: "hep taşı" ve bu odada yatan her gövde süresinde kapıya sürüklenebilir görünüyor (bu tur GEÇ gövdeyi bırakmak
        /// olur; <c>holdWin</c>). Yalnız ayaktakilerin süresi yetiyorsa: her gövde <see cref="BodyCost"/>'un seçtiği sürükleyiciye
        /// verilir ve bir sürükleyicinin gövdeleri sırayla sayılır (ilki: gövdeye varış + kapıya sürükleme; sonrakiler: kapıdan gövdeye
        /// dönüş + sürükleme); toplam, sürükleyicinin kalan turlarını aşarsa GEÇ tutulmaz. Gerekçe (aşama 6 inceleme bulgusu 2): gövde
        /// başına ayrı bakış tek sürükleyiciye iki gövde yüklüyor, bot geçişi erteleyip ayaktakileri süre sonunda tahtada bırakıyordu.
        /// </summary>
        bool RescueFeasible(Baskin.BState B)
        {
            if (B.Room == null || Fallen != "hep" || B.Moves == null || !BodyRoom(B) || !B.Units.Any(Lying)) return false;
            var inside = Baskin.Alive(B, 'p').Where(Baskin.IsSpecialist).Select(u => (u, u.Locked)).ToList();
            int turnsLeft = B.MaxTurns - B.Turn;
            BodyCost(B, inside, turnsLeft, true, out int late);
            if (late > 0) return false;
            var goal = Field(B, BodyGoal(B), -1, -1);
            var load = new Dictionary<int, int>();
            foreach (var b in B.Units)
            {
                if (!Lying(b)) continue;
                int dg = BodyGoalDist(goal, b);
                var fromBody = Field(B, new[] { new Baskin.Pt(b.X, b.Y) }, b.X, b.Y);
                Baskin.Unit pick = null;
                double best = double.MaxValue;
                int first = 0, again = 0;
                foreach (var (u, _) in inside)
                {
                    int du = fromBody[u.Y, u.X];
                    if (du < 0) continue;
                    int reach = Math.Max(0, du - 1), mv = Mv(B, u), md = Math.Max(1, mv - Baskin.DragCostFor(B, u));
                    double cost = reach / (double)mv + dg / (double)md;
                    if (cost < best) { best = cost; pick = u; first = Ceil(reach, mv) + Ceil(dg, md); again = Ceil(Math.Max(0, dg - 1), mv) + Ceil(dg, md); }
                }
                if (pick == null) return false;
                load[pick.Id] = load.TryGetValue(pick.Id, out int had) ? had + again : first;
                if (load[pick.Id] > TurnsFor(B, pick, turnsLeft, true)) return false;
            }
            return true;
        }

        /// <summary>Gövdenin kapıya sürükleme uzaklığı (<see cref="BodyCost"/>'un kuralı): geçilmez karede yatan gövde komşusundan; kapı
        /// karesinde de en az bir hamle.</summary>
        static int BodyGoalDist(int[,] goal, Baskin.Unit b)
        {
            int dg = goal[b.Y, b.X];
            if (dg < 0)
                foreach (var dd in Baskin.Dirs)
                {
                    int nx = b.X + dd[0], ny = b.Y + dd[1];
                    if (Inb(nx, ny) && goal[ny, nx] >= 0 && (dg < 0 || goal[ny, nx] + 1 < dg)) dg = goal[ny, nx] + 1;
                }
            return dg < 0 ? Far : Math.Max(1, dg);
        }

        /// <summary>
        /// "Hep taşı": tahtada yatan her gövdenin kapıya kalan yolu. Sürükleyici, gövdeye (yanına) en kısa sürede varıp onu
        /// sürükleme yürüyüşüyle (yürüyüş − <see cref="MoveRules.DragCost"/>, en az 1) kapıya götürebilecek tahtadaki uzmandır;
        /// bedel <see cref="Weights.Progress"/> × <see cref="Weights.BodyShare"/> × tur, süreye sığmazsa ya da kimse varamazsa
        /// <see cref="Weights.Abandon"/> (payıyla). Kapıdaki gövde bedelsizdir.
        /// </summary>
        double BodyCost(Baskin.BState C, List<(Baskin.Unit u, bool locked)> inside, int turnsLeft, bool before) =>
            BodyCost(C, inside, turnsLeft, before, out _);

        double BodyCost(Baskin.BState C, List<(Baskin.Unit u, bool locked)> inside, int turnsLeft, bool before, out int lateBodies)
        {
            lateBodies = 0;
            if (Fallen != "hep" || C.Moves == null || !BodyRoom(C) || !C.Units.Any(Baskin.LyingBody)) return 0;
            var goal = Field(C, BodyGoal(C), -1, -1);
            double s = 0;
            foreach (var b in C.Units)
            {
                if (!Baskin.LyingBody(b)) continue;
                int dg = goal[b.Y, b.X];
                if (dg < 0)
                    foreach (var dd in Baskin.Dirs)
                    {
                        int nx = b.X + dd[0], ny = b.Y + dd[1];
                        if (Inb(nx, ny) && goal[ny, nx] >= 0 && (dg < 0 || goal[ny, nx] + 1 < dg)) dg = goal[ny, nx] + 1;
                    }
                // Gövde kapı karesinde yatsa da sürükleme bir hamledir (sürükleyen kapıya girerken gövdeyi götürür)
                dg = dg < 0 ? Far : Math.Max(1, dg);
                var fromBody = Field(C, new[] { new Baskin.Pt(b.X, b.Y) }, b.X, b.Y);
                double best = double.MaxValue;
                bool late = true;
                foreach (var (u, _) in inside)
                {
                    int du = fromBody[u.Y, u.X];
                    if (du < 0) continue;
                    int reach = Math.Max(0, du - 1), mv = Mv(C, u), md = Math.Max(1, mv - Baskin.DragCostFor(C, u));
                    double cost = reach / (double)mv + dg / (double)md;
                    if (cost < best) { best = cost; late = Ceil(reach, mv) + Ceil(dg, md) > TurnsFor(C, u, turnsLeft, before); }
                }
                if (best == double.MaxValue) { s += W.Abandon; lateBodies++; continue; }
                s += W.Progress * W.BodyShare * best + (late ? W.Abandon : 0);
                if (late) lateBodies++;
            }
            return s;
        }

        /// <summary>
        /// Tesis, ikmal kaçışı: yükün çıkışa kalan yolu (tam ağırlık, bugünkü Teknisyen terimi gibi). Taşıyıcı tahtadaysa onun yolu;
        /// yük yerdeyse en yakın tahtadaki uzmanın yüke, yükün çıkışa yolu; taşıyıcı kapıda ya da çıktıysa 0.
        /// </summary>
        double CargoCost(Baskin.BState C, int[,] exits, List<(Baskin.Unit u, bool locked)> inside, int turnsLeft, bool before)
        {
            if (C.Carrier != 0)
            {
                foreach (var (u, locked) in inside)
                {
                    if (u.Id != C.Carrier) continue;
                    int d = exits[u.Y, u.X] < 0 ? Far : exits[u.Y, u.X], tm = Mv(C, u);
                    return W.Progress * d / (double)tm + W.Late * Math.Max(0, Ceil(d, tm) - TurnsFor(C, u, turnsLeft, before))
                        + (locked && d > 0 ? W.TekLocked : 0);
                }
                // Aşama 8: yükü köpek taşıyor. Tesisin girişinde köpek batı karesine yürüyüp tahliye olmalı (yol köpeğin yürüyüşüyle);
                // ara odalarda köpek GEÇ'te ekiple geçer: tam ağırlıklı yol en geç kalan uzmanınki
                var dog = UnitById(C, C.Carrier);
                if (Baskin.IsDog(dog) && Baskin.OnBoard(dog))
                {
                    if (Baskin.IsFinalExit(C))
                    {
                        int d = exits[dog.Y, dog.X] < 0 ? Far : exits[dog.Y, dog.X], tm = Mv(C, dog);
                        return W.Progress * d / (double)tm + W.Late * Math.Max(0, Ceil(d, tm) - TurnsFor(C, dog, turnsLeft, before));
                    }
                    double worst = 0;
                    foreach (var (u, _) in inside)
                    {
                        int d = exits[u.Y, u.X] < 0 ? Far : exits[u.Y, u.X], tm = Mv(C, u);
                        worst = Math.Max(worst, W.Progress * d / (double)tm + W.Late * Math.Max(0, Ceil(d, tm) - TurnsFor(C, u, turnsLeft, before)));
                    }
                    return worst;
                }
                return 0;
            }
            if (C.CargoX < 0) return 0;
            int dx = exits[C.CargoY, C.CargoX] < 0 ? Far : exits[C.CargoY, C.CargoX];
            var fromCargo = Field(C, new[] { new Baskin.Pt(C.CargoX, C.CargoY) }, -1, -1);
            double best = W.Progress * Far;
            foreach (var (u, _) in inside)
            {
                int du = fromCargo[u.Y, u.X];
                if (du < 0) continue;
                int mv = Mv(C, u);
                double c = W.Progress * (du + dx) / (double)mv + W.Late * Math.Max(0, Ceil(du, mv) + Ceil(dx, mv) - TurnsFor(C, u, turnsLeft, before));
                if (c < best) best = c;
            }
            return best;
        }

        static int Ceil(int a, int b) => a <= 0 ? 0 : (a + b - 1) / b;

        /// <summary>Uzmanın puandaki yürüyüşü: tesiste kadronun gerçek yürüyüşü (kişi kartından, 2–5; oyuncu kartta görür), eski
        /// tahtada türün değeri (Teknisyen 3; eski puan birebir aynı kalsın).</summary>
        static int Mv(Baskin.BState C, Baskin.Unit u) => C.Room != null && u.MoveRange > 0 ? u.MoveRange : Baskin.UnitTypes[u.Type].Move;

        /// <summary>Uzmanın bu turdan sonra yürüyebileceği tur sayısı: tesiste bu tur henüz yürümemiş uzman (makine evresinden önceki
        /// kaba puanda) bir tur fazla yürür; eski tahtada herkes için <paramref name="turnsLeft"/>.</summary>
        static int TurnsFor(Baskin.BState C, Baskin.Unit u, int turnsLeft, bool before) =>
            turnsLeft + (C.Room != null && before && Baskin.CanMove(C, u) ? 1 : 0);

        /// <summary>Giriş ve ara odanın puanı: uzmanların ileri geçide kalan yolu (Teknisyen tam, öbürleri <see cref="Weights.GateShare"/>
        /// ağırlıkla; gerçek yürüyüşle tur olarak), süreye sığmayan uzmanın geç kaldığı tur (<see cref="Weights.GateLate"/>) ve biri
        /// geç kalıyorsa odanın bedeli (<see cref="Weights.GateMiss"/>): GEÇ için ayaktaki herkes kapıda olmalı. Donanım hedefi sürerken
        /// Teknisyen'in yolu donanımın yanından geçer.</summary>
        double GateScore(Baskin.BState C, double s, Baskin.Unit tek, bool tekLocked, List<(Baskin.Unit u, bool locked)> inside, int turnsLeft, bool before)
        {
            // Son turda makine evresinden önceki kaba puanda turnsLeft 0'dır, ama bu tur yürümemiş uzmanın hâlâ bir turu var
            // (TurnsFor); aksi hâlde son turda her seçenek Loss alır ve geçide uzaklık puana girmez (aşama 4 denetimi: ilk baskın
            // 177 → 202/300)
            if (turnsLeft + (before ? 1 : 0) <= 0) return W.Loss + s;
            var gate = Field(C, C.Room.Forward, -1, -1);
            bool miss = false;
            foreach (var (u, _) in inside)
            {
                if (u.Type == "tek") continue;
                int d = gate[u.Y, u.X] < 0 ? Far : gate[u.Y, u.X], mv = Mv(C, u);
                int late = Math.Max(0, Ceil(d, mv) - TurnsFor(C, u, turnsLeft, before));
                s -= W.Progress * W.GateShare * d / mv + W.GateLate * late;
                if (late > 0) miss = true;
            }
            if (tek != null)
            {
                int tm = Mv(C, tek), tl = TurnsFor(C, tek, turnsLeft, before);
                if (deviceGoal && DeviceIntact(C))
                {
                    var r = DeviceRoute(C, tek, gate);
                    int late = Math.Max(0, r.NeedTurns(tm) - tl);
                    s -= W.Progress * (r.D1 / (double)tm + r.H / 2.0 + r.D2 / (double)tm) + W.GateLate * late + W.Device;
                    if (late > 0) miss = true;
                    if (tekLocked && r.D1 > 0) s -= W.TekLocked;
                }
                else
                {
                    int dt = gate[tek.Y, tek.X] < 0 ? Far : gate[tek.Y, tek.X];
                    int late = Math.Max(0, Ceil(dt, tm) - tl);
                    s -= W.Progress * dt / tm + W.GateLate * late;
                    if (late > 0) miss = true;
                    if (tekLocked && dt > 0) s -= W.TekLocked;
                }
            }
            if (miss) s -= W.GateMiss;
            return s;
        }

        /// <summary>Teknisyen'in donanıma yolu: D1 Teknisyen → donanımın açık komşusu, D2 oradan ileri geçide, H donanımın canı;
        /// tur tahmini hedef dolabınınki gibi (sabotaj 2 hasar, Teknisyen'in gerçek yürüyüşü <paramref name="mv"/>).</summary>
        struct DevRoute
        {
            public int D1, D2, H;
            public int NeedTurns(int mv) => (D1 == 0 ? Ceil(H, 2) : Ceil(D1, mv) + Ceil(H, 2) - 1) + Ceil(D2, mv);
        }

        DevRoute DeviceRoute(Baskin.BState C, Baskin.Unit tek, int[,] gate)
        {
            var at = C.Room.DeviceAt.Value;
            var best = new DevRoute { D1 = Far, D2 = Far, H = Baskin.CellAt(C, at.X, at.Y).O?.Hp ?? 0 };
            var fromTek = Field(C, new[] { new Baskin.Pt(tek.X, tek.Y) }, -1, -1);
            foreach (var dd in Baskin.Dirs)
            {
                int ax = at.X + dd[0], ay = at.Y + dd[1];
                if (!Inb(ax, ay) || fromTek[ay, ax] < 0 || gate[ay, ax] < 0) continue;
                if (fromTek[ay, ax] + gate[ay, ax] < best.D1 + best.D2) { best.D1 = fromTek[ay, ax]; best.D2 = gate[ay, ax]; }
            }
            return best;
        }

        /// <summary>Odanın donanımı yerinde ve sağlam.</summary>
        static bool DeviceIntact(Baskin.BState B) =>
            B.Room?.Device != null && B.Room.DeviceAt is Baskin.Pt d && Baskin.CellAt(B, d.X, d.Y).O?.K == B.Room.Device;

        /// <summary>
        /// Odaya girişte bir kez ("kir"de sonradan <see cref="ReviewDeviceGoal"/> bırakabilir): Teknisyen bu odada donanımı kırmayı hedefler mi. Politika "hic", eski tahta, kaçış (dönüş odası),
        /// donanımsız oda, kırık donanım ya da tahtada Teknisyen yoksa hayır; "hep" her zaman; "kir" donanımı kırıp ileri geçide
        /// varmanın tur tahmini süreye bir tur payla sığıyorsa.
        /// </summary>
        bool DeviceGoal(Baskin.BState B)
        {
            if (Devices == "hic" || B.Room == null || B.Escape || B.Room.Forward.Count == 0 || !DeviceIntact(B)) return false;
            var tek = Baskin.Alive(B, 'p').FirstOrDefault(u => Baskin.IsSpecialist(u) && u.Type == "tek");
            if (tek == null) return false;
            if (Devices == "hep") return true;
            var r = DeviceRoute(B, tek, Field(B, B.Room.Forward, -1, -1));
            return r.D1 < Far && r.NeedTurns(Mv(B, tek)) <= B.MaxTurns - 1 && TeamFits(B, 1);
        }

        /// <summary>Ekibin öteki uzmanları ileri geçide <paramref name="margin"/> tur payla yetişiyor mu (gerçek yürüyüş, makineler
        /// bedelli; turun başında). Teknisyen donanıma saparken öbürleri zaten sıkışıksa hedef seçilmez.</summary>
        bool TeamFits(Baskin.BState B, int margin)
        {
            var gate = Field(B, B.Room.Forward, -1, -1);
            int left = B.MaxTurns - B.Turn + 1;
            foreach (var u in Baskin.Alive(B, 'p').Where(Baskin.IsSpecialist))
            {
                if (u.Type == "tek") continue;
                int d = gate[u.Y, u.X] < 0 ? Far : gate[u.Y, u.X];
                if (Ceil(d, Mv(B, u)) > left - margin) return false;
            }
            return true;
        }

        /// <summary>
        /// Turun başında ("kir"): donanım hedefi artık süreye sığmıyorsa (Teknisyen'in donanım yolu kalan tura sığmaz ama doğrudan
        /// geçide yetişir) hedeften vazgeçilir; insan oyuncu da odayı kaybetmek yerine donanımı bırakır. "hep" vazgeçmez (politika).
        /// </summary>
        void ReviewDeviceGoal(Baskin.BState B)
        {
            if (!deviceGoal || Devices != "kir" || B.Room == null || !DeviceIntact(B)) return;
            var tek = Baskin.Alive(B, 'p').FirstOrDefault(u => Baskin.IsSpecialist(u) && u.Type == "tek");
            if (tek == null) return;
            var gate = Field(B, B.Room.Forward, -1, -1);
            int left = B.MaxTurns - B.Turn + 1, tm = Mv(B, tek);
            int direct = gate[tek.Y, tek.X] < 0 ? Far : gate[tek.Y, tek.X];
            if (DeviceRoute(B, tek, gate).NeedTurns(tm) > left && Ceil(direct, tm) <= left) deviceGoal = false;
        }

        /// <summary>Görünen niyetlerin ve uyanma karelerinin (kum fırtınasında görünmez) bu evredeki sonucu, sırayla ve kabaca: ölen
        /// birimin karesindeki sonraki atış arkaya geçmez, soğutma ünitesi bir kez patlar.</summary>
        static Forecast Predict(Baskin.BState C)
        {
            int n = C.Units.Count;
            var f = new Forecast { Dmg = new int[n], Plate = new int[n], Locked = new bool[n] };
            bool loot = Baskin.IsLoot(C);
            foreach (var iv in Baskin.Intents(C))
            {
                int ei = IndexOf(C, iv.Unit);
                if (ei < 0 || C.Units[ei].Hp - f.Dmg[ei] <= 0) continue;
                if (!iv.HasTarget || iv.TargetNone) continue;
                int tx = iv.TargetX, ty = iv.TargetY;
                switch (iv.Kind)
                {
                    case "flame":
                        foreach (var a in iv.Area) Hit(C, f, a.X, a.Y, C.Moves.FlameDamage, loot, true);
                        break;
                    case "frost":
                        foreach (var a in iv.Area)
                        {
                            int threatened = At(C, f, a.X, a.Y);
                            if (threatened >= 0 && Baskin.IsSpecialist(C.Units[threatened])) f.Locked[threatened] = true;
                        }
                        break;
                    case "build": f.NewEnemies++; break;
                    case "seal": f.NewBarriers++; break;
                    case "ram":
                    case "pull":
                    {
                        int ui = At(C, f, tx, ty);
                        if (ui < 0 || !Baskin.IsSpecialist(C.Units[ui])) break;
                        var d = Baskin.Dirs[iv.Dir];
                        int sign = iv.Kind == "ram" ? 1 : -1;
                        int nx = tx + sign * d[0], ny = ty + sign * d[1];
                        if (iv.Kind == "pull" && nx == iv.FromX && ny == iv.FromY) break;
                        f.Locked[ui] = true;
                        if (!Inb(nx, ny) || C.Cells[ny, nx].T == "wall" || C.Cells[ny, nx].O != null ||
                            At(C, f, nx, ny) >= 0) HitUnit(C, f, ui, 1);
                        break;
                    }
                    case "melee": Hit(C, f, tx, ty, C.Units[ei].Type == "montaj" ? C.Moves.BuildDamage : 2, loot, true); break;
                    case "blast":
                        foreach (var a in iv.Area) Hit(C, f, a.X, a.Y, 1, loot, true);
                        f.Dmg[ei] = C.Units[ei].Hp;
                        break;
                    case "lock":
                    {
                        int ui = At(C, f, tx, ty);
                        if (ui >= 0 && Baskin.IsSpecialist(C.Units[ui])) f.Locked[ui] = true;
                        break;
                    }
                    case "repair":
                    {
                        var o = C.Cells[ty, tx].O;
                        if (o != null && Baskin.IsTarget(o.K) && o.Hp < o.Max) f.Repairs++;
                        break;
                    }
                    case "shield": break;
                    case "clone":
                    {
                        int ui = At(C, f, tx, ty);
                        if (ui >= 0) HitUnit(C, f, ui, 1);
                        else if (C.Cells[ty, tx].T == "floor" && C.Cells[ty, tx].O == null) f.NewEnemies++;
                        break;
                    }
                    default: Hit(C, f, tx, ty, 1, loot, true); break; // atış ve havan
                }
            }
            if (!C.Fog)
                foreach (var p in C.Spawns)
                {
                    int ui = At(C, f, p.X, p.Y);
                    if (ui >= 0) HitUnit(C, f, ui, 1);
                    else if (C.Cells[p.Y, p.X].O == null) f.NewEnemies++;
                }
            // Bölge olayının bu evre sonu: Core'un önizlemesi (niyetlerden sonra, uyanma kareleri dolu sayılarak) birime hasar ve
            // donmayı bildirir; bot olayı oyuncu gibi tahtadaki uyarıdan bilir
            if (C.Ev != null)
            {
                var hz = Baskin.Hazards(C);
                foreach (var w in hz.Warnings)
                {
                    int ui = IndexOf(C, w.Unit);
                    if (ui < 0) continue;
                    if (w.Damage > 0) HitUnit(C, f, ui, w.Damage);
                    // Kriyo: donacak uzman sonraki tur kilitli gibi yürüyemez
                    if (Baskin.IsSpecialist(C.Units[ui]) && hz.Marks.Any(m => m.Kind == "freeze_next" && m.X == C.Units[ui].X && m.Y == C.Units[ui].Y)) f.Locked[ui] = true;
                }
            }
            return f;
        }

        static void Hit(Baskin.BState C, Forecast f, int x, int y, int n, bool loot, bool chain)
        {
            if (!Inb(x, y)) return;
            int ui = At(C, f, x, y);
            if (ui >= 0) { HitUnit(C, f, ui, n); return; }
            var o = C.Cells[y, x].O;
            int k = y * N + x;
            if (o == null || f.ObjDmg[k] >= o.Hp) return;
            f.ObjDmg[k] += n;
            if (f.ObjDmg[k] < o.Hp) return;
            if (o.K == "civ") f.Collateral++;
            else if (o.K == "spawner") f.SpawnersBroken++;
            else if (o.K == "cool" && chain) foreach (var d in Baskin.Dirs) Hit(C, f, x + d[0], y + d[1], 1, loot, false);
            else if (loot && Baskin.IsTarget(o.K)) f.CrateLost = true;
        }

        static void HitUnit(Baskin.BState C, Forecast f, int i, int n)
        {
            var u = C.Units[i];
            int hp = u.Hp - f.Dmg[i];
            if (hp <= 0) return;
            int a = Math.Min(u.Armor - f.Plate[i], n);
            if (a > 0) { f.Plate[i] += a; n -= a; }
            f.Dmg[i] += Math.Min(n, hp);
        }

        /// <summary>Karede ayakta (tahminde de düşmemiş) birimin sırası; yoksa −1.</summary>
        static int At(Baskin.BState C, Forecast f, int x, int y)
        {
            for (int i = 0; i < C.Units.Count; i++)
            {
                var u = C.Units[i];
                if (Baskin.OnBoard(u) && u.X == x && u.Y == y) return u.Hp - f.Dmg[i] > 0 ? i : -1;
            }
            return -1;
        }

        static int IndexOf(Baskin.BState C, int id)
        {
            for (int i = 0; i < C.Units.Count; i++) if (C.Units[i].Id == id) return i;
            return -1;
        }

        static Baskin.Unit UnitById(Baskin.BState S, int id) => S.Units.FirstOrDefault(u => u.Id == id);

        static bool Inb(int x, int y) => x >= 0 && y >= 0 && x < N && y < N;

        // ---------- Yol ----------
        /// <summary>Yürünebilir kare: duvar ve nesne değil (birimlerin bedeli <see cref="Field"/>'da); (ex, ey) duvar olsa da açık sayılır.</summary>
        static bool Open(Baskin.BState S, int x, int y, int ex, int ey) =>
            Inb(x, y) && (x == ex && y == ey || S.Cells[y, x].T != "wall" && S.Cells[y, x].O == null);

        /// <summary>Kaynaklardan uzaklık (adım; ulaşılmaz −1). Makinenin durduğu kare <see cref="Weights.EnemyStep"/> fazla sayılır,
        /// dost birimin içinden geçilir. 64 karede düz Dijkstra.</summary>
        int[,] Field(Baskin.BState S, IEnumerable<Baskin.Pt> sources, int ex, int ey)
        {
            var extra = new int[N * N];
            foreach (var u in S.Units) if (!u.Dead && u.Side == 'e') extra[u.Y * N + u.X] = W.EnemyStep;
            var d = new int[N * N];
            var done = new bool[N * N];
            for (int i = 0; i < d.Length; i++) d[i] = int.MaxValue;
            foreach (var p in sources) d[p.Y * N + p.X] = Math.Min(d[p.Y * N + p.X], extra[p.Y * N + p.X]);
            while (true)
            {
                int k = -1;
                for (int i = 0; i < d.Length; i++) if (!done[i] && d[i] != int.MaxValue && (k < 0 || d[i] < d[k])) k = i;
                if (k < 0) break;
                done[k] = true;
                int x = k % N, y = k / N;
                foreach (var dd in Baskin.Dirs)
                {
                    int nx = x + dd[0], ny = y + dd[1];
                    if (!Open(S, nx, ny, ex, ey)) continue;
                    int n = ny * N + nx, c = d[k] + 1 + extra[n];
                    if (c < d[n]) d[n] = c;
                }
            }
            var field = new int[N, N];
            for (int i = 0; i < d.Length; i++) field[i / N, i % N] = d[i] == int.MaxValue ? -1 : d[i];
            return field;
        }

        /// <summary>Görevin kalan yolu: D1 Teknisyen → hedefin (kasanın) yanı, D2 oradan çıkışa (kaçış, her görevde); kasa
        /// alındıysa D1 0 ve D2 taşıyıcı → çıkış. (ex, ey): yıkılmış sayılan duvar.</summary>
        Route RouteOf(Baskin.BState S, Baskin.Unit tek, int ex, int ey)
        {
            bool loot = Baskin.IsLoot(S);
            var carrier = loot && S.Carrier != 0 ? UnitById(S, S.Carrier) : null;
            var exits = Field(S, S.Exits, ex, ey);
            if (carrier != null)
            {
                int d = !Baskin.OnBoard(carrier) ? -1 : exits[carrier.Y, carrier.X];
                return new Route { D1 = 0, D2 = d < 0 ? Far : d };
            }
            var fromTek = Field(S, new[] { new Baskin.Pt(tek.X, tek.Y) }, ex, ey);
            var best = new Route { D1 = Far, D2 = Far };
            foreach (var t in Baskin.TargetsLeft(S))
                foreach (var dd in Baskin.Dirs)
                {
                    int ax = t.X + dd[0], ay = t.Y + dd[1];
                    if (!Inb(ax, ay) || fromTek[ay, ax] < 0) continue;
                    int d2 = exits[ay, ax];
                    if (d2 < 0) continue;
                    if (fromTek[ay, ax] + d2 < best.Total) best = new Route { D1 = fromTek[ay, ax], D2 = d2 };
                }
            return best;
        }

        /// <summary>Turun başında: hak kalan Madenci varsa, yıkılınca görevin yolunu en çok (en az iki adım) kısaltan duvar.</summary>
        Context MakeContext(Baskin.BState B)
        {
            var ctx = new Context();
            var players = Baskin.Alive(B, 'p').Where(Baskin.IsSpecialist).ToList();
            var tek = players.FirstOrDefault(u => u.Type == "tek");
            if (tek == null || !players.Any(u => u.Type == "maden" && u.Charges != 0)) return ctx;
            int now = RouteOf(B, tek, -1, -1).Total, gain = 1;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    if (B.Cells[y, x].T != "wall" || !Baskin.Dirs.Any(d => Open(B, x + d[0], y + d[1], -1, -1))) continue;
                    int g = now - RouteOf(B, tek, x, y).Total;
                    if (g > gain) { gain = g; ctx.HasBreach = true; ctx.BX = x; ctx.BY = y; ctx.Gain = g; }
                }
            if (!ctx.HasBreach) return ctx;
            var sides = Baskin.Dirs.Select(d => new Baskin.Pt(ctx.BX + d[0], ctx.BY + d[1])).Where(p => Open(B, p.X, p.Y, -1, -1)).ToList();
            ctx.Field = Field(B, sides, -1, -1);
            return ctx;
        }
    }
}
