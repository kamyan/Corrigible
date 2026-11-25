using System;
using System.Linq;
using Corrigible.Core;
namespace Corrigible.Desk.Bridge
{
    /// <summary>Kartlı başlangıcın okuduğu durum; kuralları ve oyun kaydını değiştirmez.</summary>
    public sealed class PlanningTutorialContext
    {
        public bool Blocked, BoardOpen, PersonSelected, AssignedCook, SlipOpen, RaidOrdered, RaidLoaded, RaidOver;
        public bool RaidSelected, RaidMoved, RaidActed, RaidTurnEnded, ReportOpen, Printed;
        /// <summary>KOS aşama 8: kapıda grup bekliyor; kamera tezgâhta (K8A/K8R); bir grup karara bağlandı ya da gitti (bir kez yeter).</summary>
        public bool GroupWaiting, AtCounter, GroupDone;
        /// <summary>
        /// 2026-11-24 (ilk baskın adım adım): kadro raporu açık; baskının ilerleyişi (0 ilk oda, 1 kapıya giren var, 2 ikinci pafta,
        /// 3 hedef paftası, 4 kaçış, 5 bitti) ve "ilk kez" notlarının koşulları (son tur, düşen uzman, uyanan makine, bölge olayı, yük).
        /// </summary>
        public bool SquadOpen, RaidLastTurn, RaidLastTurnHard, RaidDown, RaidWake, RaidHazard, RaidCarrier;
        public int RaidProgress;
        /// <summary>Karar bekleyen olay tutanağı var; tutanak elde; bir olay karara bağlandı (ya da bu kayıtta olay yok).</summary>
        public bool EventOpen, EventInHand, EventDone;
        public int Raids, Day;
    }
    public sealed class PlanningTutorialStep
    {
        public string Id, Title, Body;
        /// <summary>Bilgi adımı: SONRAKİ ile geçer; <see cref="Done"/> da verilmişse koşul doğru olunca kendiliğinden de geçer (oyuncu
        /// işi rehberden önce yaptıysa ya da baskın ilerlediyse).</summary>
        public bool Info;
        public Func<PlanningTutorialContext,bool> Done;
        /// <summary>Konu adımı (KOS aşama 8): yalnız bu koşul doğruyken gösterilir; sırası değil oyunun hâli seçer.</summary>
        public Func<PlanningTutorialContext,bool> Topic;
    }
    /// <summary>
    /// İlk pano, gerçek baskın ve fiziksel vardiya kapanışı; sonra hedef satırı ve ilk aday grubu ile ilk olay tutanağı. Eylemden
    /// sonra ilerler; sunum dışında etkisi yoktur.
    /// <para>
    /// KOS aşama 8: ilk vardiya dosyalanınca (<c>Day &gt; 0</c>) hedef adımına geçilir. Ardından konu bölümü gelir: kapıda grup ya da
    /// kapı panosunda tutanak belirince onun adımı gösterilir (hangisi önce gelirse); ikisi de yokken "vardiyayı sürdür" adımı canlı hedef
    /// satırıyla bekler. İkisi de bir kez karara bağlanınca (ya da <see cref="WatchLastDay"/>'e kadar konu çıkmazsa) İFŞA, son şalter
    /// ve kapanış bilgileri gelir. Rehber sürerken son şalter mühürlüdür (<c>LastSwitchViewBuilder.Lock</c>).
    /// </para>
    /// </summary>
    public sealed class PlanningTutorialFlow
    {
        static bool PastRaid(PlanningTutorialContext c)=>c.RaidOver||c.Raids>0&&!c.RaidOrdered;
        static bool GroupTopic(PlanningTutorialContext c)=>c.GroupWaiting&&!c.GroupDone;
        static bool EventTopic(PlanningTutorialContext c)=>c.EventOpen&&!c.EventDone;
        /// <summary>Konu çıkmadan bu güne gelinirse bekleme biter: rehber son şalteri sonsuza dek mühürlü tutmaz.</summary>
        public const int WatchLastDay=10;
        public static readonly PlanningTutorialStep[] Steps={
            Info("intro",Loc.N("İLK VARDİYAN"),Loc.N("Sarı çerçeve her adımda nereye basacağını gösterir. Önce ekibini tanıyıp bir görev vereceğiz, sonra ilk baskına çıkacağız. Zaman kendiliğinden akmaz; vardiya, basılı devir fişini EVRAK kutusuna koyduğunda ilerler. Hazır olduğunda SONRAKİ'ye bas.")),
            Info("counters",Loc.N("SAYAÇLAR"),Loc.N("Masadaki sayaçlar sığınağın hâlidir: fon, erzak, malzeme, özgür bölgeler ve GÜVEN. Erzak her vardiya tükenir; GÜVEN sıfıra inerse direniş dağılır. Sağdaki ALARM ve İFŞA YZ'nin seni ne kadar aradığını sayar.")),
            Wait("board",Loc.N("KİŞİ PANOSUNU AÇ"),Loc.N("Masadaki SIĞINAK tuşuna veya soldaki panoya tıkla. Sekiz kişinin kartları burada; aynı kişiler sığınakta çalışır ve baskına çıkar."),c=>c.BoardOpen),
            Wait("person",Loc.N("BİR KİŞİYİ TANI"),Loc.N("Panodaki bir kişi kartına tıkla. CAN, hareket ve güç kişiye aittir. Boşta destesine tek tık kartları açar; sürüklemek üstteki kartı alır."),c=>c.PersonSelected||c.AssignedCook),
            Wait("assign",Loc.N("İLK GÖREVİNİ VER"),Loc.N("Boşta'dan bir kişi kartını MUTFAK alanına sürükleyip bırak. Dolu alana bırakırsan iki kişi yer değiştirir. Mutfak, vardiya sonunda erzak tüketimini azaltır."),c=>c.AssignedCook),
            Info("gear",Loc.N("SAHA KADROSU VE EŞYALAR"),Loc.N("TEKNİSYEN, EMP ve ANALİST baskına giden üç kişidir. Her birinin kendi rol kiti olmalı. Depodaki ekipman kartını kişinin kartının üstüne bırakarak verirsin; zırh plakası ilk hasarı karşılar.")),
            Info("intel",Loc.N("GÖZETİM VE PAFTALAR"),Loc.N("GÖZETİM cebine koyduğun kişi her vardiya kapanışında sıradaki kanadın bir odasının planını çıkarır. Bilinen paftada odanın duvarlarını ve nesnelerini baskından önce görürsün; bilinmeyen pafta PLAN YOK damgalıdır.")),
            Wait("back",Loc.N("MASAYA DÖN"),Loc.N("Panodan çıkmak için MASAYA DÖN levhasına bas (ya da sağ tık / Esc)."),c=>!c.BoardOpen||c.SlipOpen||c.RaidOrdered),
            // İlk kanadın (yeni oyunda sıradaki kanat) hiçbir odası bilinmez: üç pafta da "PLAN YOK" damgalıdır (FacilityPlanBuilder)
            Wait("slip",Loc.N("GÖREVİ SEÇ"),Loc.N("Masadaki haritaya tıkla. Görev fişi hedefi, bedeli ve kazanılacak kaynakları gösterir. 01 kanadının üç paftası henüz PLAN YOK: odaları bilmiyorsun. Ekibin girdiği oda teybe kaydedilir; öbür kaynaklar paftanın üstüne gelince yazar."),c=>c.SlipOpen||c.RaidOrdered),
            Wait("order",Loc.N("KADROYU KONTROL ET"),Loc.N("BASKIN EMRİ VER ile kadroyu aç. İsimleri, kitleri ve canları kontrol et. Ekipman eksikse panoya dön. Hazırsan BASKINA ÇIK."),c=>c.RaidOrdered),
            Info("raidBoard",Loc.N("BASKIN TAHTASI"),Loc.N("Bu tahta kanadın ilk paftası, ilk odası. Sol üstteki şerit odanın tur sayısıdır: süre dolmadan ekibi yeşil geçide götür. Altında görev ve hedef satırları, sağda telsiz masası yazar."),c=>c.RaidProgress>=2||PastRaid(c)),
            Info("raidIntent",Loc.N("MAKİNELERİN NİYETİ"),Loc.N("İşaretli makinenin üstüne gel. Kırmızı ok onun bu tur sonu ne yapacağını, bayraktaki sayı sırasını gösterir. Makineler sen EMRİ İLET'e basınca bu sırayla oynar; niyeti görüp hamleni ona göre yap."),c=>c.RaidProgress>=2||PastRaid(c)),
            Wait("raidSelect",Loc.N("SAHADA BİR KİŞİ SEÇ"),Loc.N("Alt sıradan bir uzman seç (1–3) ya da tahtadaki taşına tıkla. Mavi kareler yürüyebileceği alandır."),c=>c.RaidSelected||c.RaidProgress>=1||PastRaid(c)),
            Wait("raidMove",Loc.N("İLK HAMLE"),Loc.N("Fareyi mavi karelerde gezdir: yürüyeceği yol çizilir. İşaretli kareye tıkla. Her kişi turda bir hareket ve bir yetenek kullanır; Z son hamleyi geri alır."),c=>c.RaidMoved||c.RaidProgress>=1||PastRaid(c)),
            Info("raidAbility",Loc.N("YETENEK"),Loc.N("Q'ya bas, işaretli hedefe tıkla: EMP makineleri iter, Analist çeker, her uzman bitişik kasayı açar; önizleme sonucu gösterir. Hedef yoksa SONRAKİ."),c=>c.RaidActed||c.RaidProgress>=2||PastRaid(c)),
            Wait("raidTurn",Loc.N("EMRİ İLET"),Loc.N("Diğer kişilerin hareketlerini de yap. Hazır olduğunda EMRİ İLET'e veya Enter'a bas. Makineler bayraklarındaki sırayla oynar; Boşluk hızlı yürütmeyi açar."),c=>c.RaidTurnEnded||c.RaidProgress>=1||PastRaid(c)),
            Wait("raidGate",Loc.N("YEŞİL GEÇİT"),Loc.N("İşaretli yeşil kare odanın ileri geçididir. Bir uzmanı oraya yürüt: kapıya giren tahtadan kalkar, makineler onu hedef alamaz."),c=>c.RaidProgress>=1||PastRaid(c)),
            Wait("raidPass",Loc.N("HERKES KAPIDA: GEÇ"),Loc.N("Ayaktaki bütün uzmanlar kapıya girince EMRİ İLET GEÇ olur: bas, sıradaki pafta açılır. Süre dolarken kapıda biri varsa oda onunla geçilir; yetişemeyen geri çekilir."),c=>c.RaidProgress>=2||PastRaid(c)),
            Wait("raidMiddle",Loc.N("İKİNCİ PAFTA"),Loc.N("Tahta geçidin yönünden kayarak geldi; giriş kapın önceki geçidin karşısında. Ara odanın donanımını (anten ya da vana) Teknisyen'le kırarsan hedef odası kolaylaşır. Yine yeşil geçide git ve GEÇ."),c=>c.RaidProgress>=3||PastRaid(c)),
            Wait("raidTarget",Loc.N("HEDEF: KASAYI AÇ"),Loc.N("Son pafta hedef odası. İşaretli kasaya bitişik bir uzmanla Q'ya bas ve kasaya tıkla: kasa açılır, yükü o uzman taşır ve kaçış başlar."),c=>c.RaidProgress>=4||PastRaid(c)),
            Wait("raidEscape",Loc.N("DÖNÜŞ"),Loc.N("Şimdi aynı yoldan dön: işaretli giriş geçidine yürü, herkes kapıdayken GEÇ. Dönüşte süre dolarken tahtada kalan kaybedilir; yükü taşıyan düşerse yük yere düşer, başka biri alabilir. İlk odanın girişinden çıkınca baskın biter."),PastRaid),
            Wait("result",Loc.N("SONUCU MASAYA GETİR"),Loc.N("Sonuç kâğıdını oku, OPERASYON RAPORUNA GEÇ'e bas. Kayıplar, yaralar ve eldeki ekipman sığınağa döner; ölen kişi geri gelmez."),c=>c.ReportOpen),
            Wait("report",Loc.N("RAPORU DOSYALA"),Loc.N("Operasyon raporunda kişi sonuçlarını ve kazançları oku; raporu EVRAK'a kaldırıp masaya dön."),c=>!c.ReportOpen),
            Info("rest",Loc.N("YARALILAR VE YATIŞ"),Loc.N("Yaralı her vardiya kapanışında iyileşir: Boşta'da {0} can, görevdeyken {1} can (yarım tayında iyileşme yok). Yatan yaralı, canı dolsa bile yatış süresi bitene kadar çalışamaz. Gerekiyorsa sağlıklı birini saha rolüne geçir ve rol kitini de ona ver.")),
            Wait("print",Loc.N("KAPAĞI AÇ, FİŞİ BAS"),Loc.N("MASA'ya dön. VARDİYAYI BİTİR şalterinin önce kapağını aç, sonra koluna bas. Plan uygunsa devir fişi teleks daktilosundan basılır; eksik varsa düzeltmen gereken bilgi yazılır. Fişi basmak henüz zamanı ilerletmez."),c=>c.Printed||c.Day>0),
            Wait("file",Loc.N("YENİ VARDİYAYI BAŞLAT"),Loc.N("Daktilodaki basılı fişi alıp EVRAK kutusuna sürükle ve bırak. Yeni vardiya ancak bu dosyalama ile başlar; üretim, dinlenme ve erzak hesabı o anda işlenir."),c=>c.Day>0),
            // KOS aşama 8: hedef satırı (ONERILER Ö4). {0} canlı satırdır (GoalLine).
            Info("goal",Loc.N("HEDEF VE SAYAÇLAR"),Loc.N("Bütün sektörleri geri alınca son sunucu düşer: kazanırsın. Kaybettirenler: GÜVEN 0'da direniş dağılır, İFŞA eşikte konum çözülür, açlıkla sığınak eksik kalırsa terk edilir. ALARM eşikte YZ bir sektörü geri alır, güven düşer.\n{0}")),
            Wait("watch",Loc.N("VARDİYAYI SÜRDÜR"),Loc.N("Her vardiya: panoyu düzenle, görevi seç, fişi bas, EVRAK'a koy. Kapı çalınca rehber sürer: kapıda aday grubu ya da kapıya bırakılmış bir tutanak olur.\n{0}"),c=>c.GroupDone&&c.EventDone||c.Day>=WatchLastDay),
            Topical("event",Loc.N("OLAY TUTANAĞI"),Loc.N("Kapı çaldı, teleks OLAY satırını bastı: tutanak kapının çekmecesine bırakıldı, çekmecenin lambası yanıyor. KAPI tuşuyla kapıya dön, çekmeceyi aç ve tutanağı al. Karar verilmeden devir fişi basılmaz: şalterin plakası “olay bekliyor” der."),EventTopic,c=>c.EventInHand||c.EventDone),
            Topical("decide",Loc.N("SATIR SEÇ, KARARA BAĞLA"),Loc.N("Her satırın bedeli yanında, bilinen riski altında yazar. GÜVENLİ etiketli satır her durumda açıktır ama bedelsiz değildir. Bir satır seç (1–3) ve KARAR kutusuna bas; Esc tutanağı çekmeceye geri koyar. Karar çekmeceye konunca yanındaki şalteri çekip dışarıya bildir."),EventTopic,c=>c.EventDone),
            Topical("door",Loc.N("KAPIDA ADAYLAR"),Loc.N("Teleks KAPI satırını bastı. KAPI tuşuyla kapıya dön, inik tezgâha tıkla. Karar vermezsen grup birkaç vardiya sonra gider."),GroupTopic,c=>c.AtCounter||c.GroupDone),
            Topical("stamp",Loc.N("KARTI OKU, DAMGALA"),Loc.N("Şüphe satırı kim olabileceğini, altındakiler getirdiğini yazar; getirdiği kit ve zırh onunla kalır. En çok birini al: KABUL bir karta, GERİ ÇEVİR fişe. Sığınak doluysa önce fişte yollanacak kişiyi işaretle; {0} erzak yolluk götürür."),GroupTopic,c=>c.GroupDone),
            Info("exposure",Loc.N("İFŞA VE İZLER"),Loc.N("Alarmın altındaki İFŞA şeridi YZ'nin seni ne kadar bildiğini sayar. Bir iz şüphedir, iki iz güçlü kanıttır; görmezden gelinen iz ifşayı artırır, tecrit her zaman bulgu vermez. İfşa {0} olunca teleks KONUM ÇÖZÜLDÜ der: sorgu, son baskın ya da son şalter yetişmezse sığınak düşer.")),
            Info("switch",Loc.N("SON ŞALTER VE ÇEKİLME"),Loc.N("Arka duvardaki kapaklı şalter harekâtı bitirir; rehber sürerken ve {0}. vardiyadan önce mühürlüdür. Plakası sonucu yazar: ÇEKİLME yenilgi sayılmaz; konum çözülmüş, sığınak eksik, açlık sürüyor ya da güven düşükse TAHLİYE olur, yenilgidir.")),
            Info("go",Loc.N("ARTIK KOMUTA SENDE"),Loc.N("Rehber bitince son şalterin rehber mührü kalkar. Sektörleri al; güveni, alarmı ve ifşayı izle. Üretim için panodaki tarif defterini kullan. Baskında E tesis hamlesini, R taşınan ekipmanı kurar; şartlar ve makine niyetleri KURALLAR kartında. Bitirmek için SONRAKİ'ye bas."))
        };
        static PlanningTutorialStep Info(string id,string title,string body,Func<PlanningTutorialContext,bool> done=null)=>new PlanningTutorialStep{Id=id,Title=title,Body=body,Info=true,Done=done};

        /// <summary>
        /// "İlk kez" notları (geliştirici, 2026-11-24: "ilk defa olan her şeyi tanıtsın"): rehber sürerken baskın tahtasında bir durum ilk
        /// kez görülünce sıradaki adımın notunun yerine bir kez çıkar, SONRAKİ ile kapanır (adım beklemeye devam eder). Hangilerinin
        /// görüldüğünü <c>SiginakTutorial</c> tutar (PlayerPrefs; kayda yazılmaz). Sıra öncelik sırasıdır.
        /// </summary>
        public static readonly PlanningTutorialStep[] FirstNotes={
            Topical("firstDown",Loc.N("UZMAN DÜŞTÜ"),Loc.N("Canı biten uzman yere düşer ve kartı kapanır. GEÇ'ten önce yanındaki bir uzman onu E (sürükle) ile kapıya taşıyabilir; taşınmayan gövde o odada yatar, ekip dönüşte oradan geçerken toplar. Baskın dönüşten önce biterse yerde kalır."),c=>c.RaidDown,null),
            Topical("firstLastTurn",Loc.N("SON TUR"),Loc.N("Tur şeridinin son hücresindesin. Bu tur sonunda süre doluyor: kapıda bekleyen varsa oda onunla geçilir, kapıya varamayan geri çekilir; kapıda kimse yoksa ekip döner ve baskın başarısız biter."),c=>c.RaidLastTurn,null),
            Topical("firstLastTurnHard",Loc.N("SON TUR"),Loc.N("Tur şeridinin son hücresindesin. Hedef odasında süre dolarsa hedef kilitlenir ve baskın kaybedilir; dönüşte süre dolarken tahtada kalan uzman geride kalır ve düşmüş sayılır."),c=>c.RaidLastTurnHard,null),
            Topical("firstWake",Loc.N("MAKİNE UYANIYOR"),Loc.N("Kırmızı halkalı karelerde tur sonunda yeni makine uyanır. Uyanma karesinin üstünde duran uzman uyanmayı engeller ama hasar alır; ya da o kareden uzak dur."),c=>c.RaidWake,null),
            Topical("firstHazard",Loc.N("BÖLGE OLAYI"),Loc.N("Bu odada bölgenin olayı işliyor; kuralı KURALLAR kartında yazar. Tahtadaki işaretli kareler tur sonunda etkilenir; bir kareye yürümeden önce üstüne gelip önizlemedeki uyarıya bak."),c=>c.RaidHazard,null),
            Topical("firstCarrier",Loc.N("YÜKÜ TAŞIYAN"),Loc.N("Kasadan alınan ikmali artık bir uzman taşıyor. Yük çıkışa varmadan baskın kazanılmaz. Taşıyan düşerse yük yerde kalır: o kareye yürüyen başka bir uzman alır."),c=>c.RaidCarrier,null),
        };
        static PlanningTutorialStep Wait(string id,string title,string body,Func<PlanningTutorialContext,bool> done)=>new PlanningTutorialStep{Id=id,Title=title,Body=body,Done=done};
        static PlanningTutorialStep Topical(string id,string title,string body,Func<PlanningTutorialContext,bool> topic,Func<PlanningTutorialContext,bool> done)=>new PlanningTutorialStep{Id=id,Title=title,Body=body,Done=done,Topic=topic};
        public static int IndexOf(string id)=>Array.FindIndex(Steps,s=>s.Id==id);

        /// <summary>2026-11-24'ten önceki adım dizisi (kayıtlı rehber yalnız dizin taşıyordu): eski dizin bu tablodan kimliğe çevrilir; eski
        /// tek "raid" adımı ilk baskın adımlarının başına düşer (ilerleyiş durumdan geçer).</summary>
        static readonly string[] LegacyIds={"intro","board","person","assign","gear","slip","order","raidSelect","raidMove","raidAbility","raidTurn","raidBoard",
            "result","report","rest","print","file","goal","watch","event","decide","door","stamp","exposure","switch","go"};
        /// <summary>Eski dizinin bugünkü karşılığı (bilinmiyorsa 0).</summary>
        public static int FromLegacyIndex(int i)=>i>=0&&i<LegacyIds.Length?Math.Max(0,IndexOf(LegacyIds[i])):0;
        static readonly int RaidIndex=IndexOf("raidBoard");
        /// <summary>İlk vardiyadan sonraki ilk adım (hedef satırı).</summary>
        public static readonly int GoalIndex=IndexOf("goal");
        static readonly int WatchIndex=IndexOf("watch"),AfterTopics=IndexOf("exposure");
        public int Index{get;private set;}
        public bool Finished{get;private set;}
        public PlanningTutorialStep Current=>Finished?null:Steps[Index];
        /// <summary>Konu bölümünde mi (bekleme ya da kapı/olay adımları).</summary>
        public bool InTopics=>!Finished&&Index>=WatchIndex&&Index<AfterTopics;
        public PlanningTutorialFlow(int index=0){Index=Math.Max(0,Math.Min(Steps.Length-1,index));}
        public bool Update(PlanningTutorialContext c)
        {
            if(Finished||c.Blocked)return false;int before=Index;
            // Oyuncu rehberden önce sefere çıktıysa pano bilgisi savaşın üstünde beklemez.
            if(c.RaidOrdered&&Index<RaidIndex)Index=RaidIndex;
            // İlk vardiya dosyalandıysa ilk vardiyanın adımları geçmiştir (rapor görülmeden kapanan vardiya da).
            if(c.Day>0&&Index<GoalIndex)Index=GoalIndex;
            while(!Finished)
            {
                if(InTopics){int pick=Pick(c);if(pick!=AfterTopics){Index=pick;break;}Index=AfterTopics;continue;}
                if(Current.Info){if(Current.Done!=null&&Current.Done(c)){Next();continue;}break;}
                if(!Current.Done(c))break;
                Next();
            }
            return before!=Index||Finished;
        }
        /// <summary>Konu bölümünde gösterilecek adım: koşulu doğru ve bitmemiş ilk konu, yoksa bekleme; bekleme bittiyse İFŞA.</summary>
        static int Pick(PlanningTutorialContext c)
        {
            for(int i=WatchIndex+1;i<AfterTopics;i++){var s=Steps[i];if(s.Topic(c)&&!s.Done(c))return i;}
            return Steps[WatchIndex].Done(c)?AfterTopics:WatchIndex;
        }
        public void Acknowledge(){if(!Finished&&Current.Info)Next();}
        public void Skip()=>Finished=true;
        void Next(){if(Index+1>=Steps.Length)Finished=true;else Index++;}

        // ---------------------------------------------------------------- gösterim (yalnız okur)

        /// <summary>
        /// Adımın gösterilecek metni (o dilde): yer tutucular canlı değerlerle doldurulur (hedef satırı, yolluk, ifşa eşiği, son
        /// şalterin en erken vardiyası). Simülasyonu yalnız okur.
        /// <para>
        /// Tesis aşama 17 ve tek kip: tablodaki slip, order, raidSelect, raid ve go gövdeleri tesis metnidir (eski tek tahtanın
        /// metinleri kalktı). Öbür adımların tablodaki gövdesi "alt bayraksız metin"dir; tesisin hâline göre konuşan adımlar onun
        /// tesiste de geçerli dersini silmez: raidMove ve raidAbility alt bayraksız metnin sonuna kısa bir tesis notu ekler
        /// (<see cref="WithNote"/>); goal, exposure ve report geçerli dersleri koruyan tek metindir, artık doğru olmayan cümleyi (eşikte
        /// anlık karşı saldırı) taşımaz. Henüz anlamı olmayan bilgi koşula bağlıdır: önlem notu yalnız yürürlükte önlem varken
        /// (<see cref="Simulation.Countermeasures"/>) çıkar; ilk baskının sonucunda sunucu ödülü olamayacağı için (ilk kanat ikmal
        /// kanadıdır) result adımı tablodaki metindir. Alt bayrağı (<c>facilityTitlesEnabled</c>, <c>facilityDefenseEnabled</c>,
        /// <c>infiltratorFieldEnabled</c>, <c>facilityCountermeasures</c>) kapalı adım alt bayraksız metne döner; adım dizisi değişmez.
        /// </para>
        /// </summary>
        public static string BodyText(PlanningTutorialStep s,Simulation sim)
        {
            if(s==null)return "";
            var b=sim?.B;
            // Bazı adımlar tesisin hâline göre konuşur; alt bayrağı kapalı adım (ya da simülasyonsuz çağrı) tablodaki alt bayraksız metne döner
            if(b!=null)
            {
                switch(s.Id)
                {
                    // Sürükleme bir tesis hamlesidir (E): uzman düşerse yanındaki onu kapıya taşır
                    case "raidMove": return WithNote(s,Loc.T("Düşen uzmanı GEÇ'ten önce yanındaki biri E ile kapıya sürüklesin; dönüşte temiz odadaki düşeni ekip toplar."));
                    case "raidAbility": return WithNote(s,Loc.T("Teknisyen yol üstündeki donanımı sabote edebilir. Hedefe varmadan odanın tur sayacını kontrol et."));
                    case "raidTurn" when sim.Countermeasures.Count>0: return WithNote(s,Loc.T("YZ sık kullandığın taktiklere önlem öğrendi; plandaki kırmızı işaretlere bak."));
                    case "report" when b.facilityTitlesEnabled: return Loc.T("Operasyon raporunda kişi sonuçlarını ve kazançları oku; sağ dönenler yaptıkları işlerle unvan kazanabilir. Raporu EVRAK'a kaldırıp masaya dön.");
                    case "goal" when b.facilityDefenseEnabled: return Loc.F("Bütün sektörleri geri alınca son sunucu düşer: kazanırsın. Kaybettirenler: GÜVEN 0'da direniş dağılır, İFŞA eşikte konum çözülür, açlıkla sığınak eksik kalırsa terk edilir. ALARM eşikte YZ bir kanadı kuşatır: SAVUN ya da BIRAK; bırakılan kanat düşer, güven azalır.\n{0}",GoalLine(sim));
                    case "exposure" when b.infiltratorFieldEnabled: return Loc.F("Alarmın altındaki İFŞA şeridi YZ'nin seni ne kadar bildiğini sayar. Bir iz şüphedir, iki iz güçlü kanıttır; görmezden gelinen iz ifşayı artırır. Sahada köpeğin hırlaması ve EMP sonrası kilitlenme şüphe uyandırır; insanlarda da görülebilir. İfşa {0} olunca KONUM ÇÖZÜLDÜ: sorgu, son baskın ya da son şalter yetişmezse sığınak düşer.",b.exposureThreshold);
                }
            }
            switch(s.Id)
            {
                case "goal":case "watch":return Loc.F(s.Body,GoalLine(sim));
                // Kayıt 22: iyileşme denge değerlerinden (Boşta boardRestHeal, görev boardDutyHeal)
                case "rest":return Loc.F(s.Body,b?.boardRestHeal??1,b?.boardDutyHeal??1);
                case "stamp":return Loc.F(s.Body,Loc.Num(sim?.DismissSuppliesPreview??b?.dismissSupplies??0,"0.##"));
                case "exposure":return Loc.F(s.Body,b?.exposureThreshold??0);
                case "switch":return Loc.F(s.Body,b?.lastSwitchMinShift??0);
                default:return Loc.T(s.Body);
            }
        }

        /// <summary>Alt bayraksız metin ve arkasından tesis notu (aynı paragraf): metnin tesiste de geçerli dersleri korunur.</summary>
        static string WithNote(PlanningTutorialStep s,string note)=>Loc.F("{0} {1}",Loc.T(s.Body),note);

        /// <summary>
        /// Hedef satırı (ONERILER Ö4): kazanmanın sayacı (alınan sektör / bütün sektörler) ve kaybettiren üç sayaç (güven, alarm/eşik,
        /// ifşa/eşik). Sayaçlar odadaki göstergelerin okuduğu değerlerdir.
        /// </summary>
        public static string GoalLine(Simulation sim)
        {
            if(sim?.S?.Liberation==null)return "";
            var regions=sim.S.Liberation.Regions;
            int total=regions.Sum(r=>r.Sectors.Count),taken=regions.Sum(r=>r.CapturedCount);
            int exposure=sim.Shelter?.Exposure??0;
            return Loc.F("HEDEF · sektör {0}/{1} · güven {2} · alarm {3}/{4} · ifşa {5}/{6}",taken,total,sim.S.Trust,
                Loc.Num(sim.S.Alarm,"0.#"),Loc.Num(sim.B.liberationAlarmThreshold,"0.#"),exposure,sim.B.exposureThreshold);
        }
    }
}
