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
        /// <summary>Karar bekleyen olay tutanağı var; tutanak elde; bir olay karara bağlandı (ya da bu kayıtta olay yok).</summary>
        public bool EventOpen, EventInHand, EventDone;
        public int Raids, Day;
    }
    public sealed class PlanningTutorialStep
    {
        public string Id, Title, Body;
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
    /// EVRAK'ta tutanak belirince onun adımı gösterilir (hangisi önce gelirse); ikisi de yokken "vardiyayı sürdür" adımı canlı hedef
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
            Info("intro",Loc.N("İLK VARDİYAN"),Loc.N("Önce ekibini tanıyıp bir görev vereceğiz. Zaman kendiliğinden akmaz; vardiya, basılı devir fişini EVRAK kutusuna koyduğunda ilerler. Hazır olduğunda SONRAKİ'ye bas.")),
            Wait("board",Loc.N("KİŞİ PANOSUNU AÇ"),Loc.N("Masadaki SIĞINAK tuşuna veya soldaki panoya tıkla. Sekiz kişinin kartları burada; aynı kişiler sığınakta çalışır ve baskına çıkar."),c=>c.BoardOpen),
            Wait("person",Loc.N("BİR KİŞİYİ TANI"),Loc.N("Panodaki bir kişi kartına tıkla. CAN, hareket ve güç kişiye aittir. Boşta destesine tek tık kartları açar; sürüklemek üstteki kartı alır."),c=>c.PersonSelected||c.AssignedCook),
            Wait("assign",Loc.N("İLK GÖREVİNİ VER"),Loc.N("Boşta'dan bir kişi kartını MUTFAK alanına sürükleyip bırak. Dolu alana bırakırsan iki kişi yer değiştirir. Mutfak, vardiya sonunda erzak tüketimini azaltır."),c=>c.AssignedCook),
            Info("gear",Loc.N("SAHA KADROSU VE EŞYALAR"),Loc.N("TEKNİSYEN, EMP ve ANALİST baskına giden üç kişidir. Her birinin kendi rol kiti olmalı. Depodaki ekipman kartını kişinin kartının üstüne bırakarak verirsin; zırh plakası ilk hasarı karşılar.")),
            Wait("slip",Loc.N("GÖREVİ SEÇ"),Loc.N("MASAYA DÖN ile uzaklaş ve masadaki haritaya tıkla. Görev fişi hedefi, bedeli ve kazanılacak kaynakları gösterir."),c=>c.SlipOpen||c.RaidOrdered),
            Wait("order",Loc.N("KADROYU KONTROL ET"),Loc.N("BASKIN EMRİ VER'e bas. Açılan kadro raporunda isimleri, canları ve kitleri kontrol et; hazır olduğunda BASKINA ÇIK'a bas. Eksik varsa rapor nedenini yazar."),c=>c.RaidOrdered),
            Wait("raidSelect",Loc.N("SAHADA BİR KİŞİ SEÇ"),Loc.N("Alt sıradaki isimli kartlardan birine veya tahtadaki piyonuna tıkla; 1, 2, 3 de seçer. Mavi kareler yürüyebileceği yerlerdir. Kırmızı oklar makinelerin sıradaki niyetlerini gösterir."),c=>c.RaidSelected||PastRaid(c)),
            Wait("raidMove",Loc.N("İLK HAMLE"),Loc.N("Seçtiğin kişinin ulaşabildiği mavi bir kareye tıkla. Hareket hakkı kullanılır. Her kişi bir turda bir hareket ve bir yetenek kullanabilir; Z son hamleyi geri alır."),c=>c.RaidMoved||PastRaid(c)),
            Wait("raidAbility",Loc.N("YETENEĞİ KULLAN"),Loc.N("Q'ya veya yetenek satırına bas, ardından işaretlenen geçerli bir hedefe tıkla. Teknisyen kasayı açar, EMP makineleri iter, Analist çeker. Hedef üstündeki önizleme sonucu gösterir."),c=>c.RaidActed||PastRaid(c)),
            Wait("raidTurn",Loc.N("EMRİ İLET"),Loc.N("Diğer kişilerin hareket ve yeteneklerini de değerlendir. Hazır olduğunda EMRİ İLET'e veya Enter'a bas. Makineler bayraklarındaki sırayla oynar; Boşluk hızlı yürütmeyi açar."),c=>c.RaidTurnEnded||PastRaid(c)),
            Wait("raid",Loc.N("KASAYI AL, EKİBİ ÇIKAR"),Loc.N("Teknisyen ile ikmal kasasını açtıktan sonra herkesi yeşil giriş karelerinden tahliye et. Tur sınırını izle: süre sonunda geride kalan kişi kalıcı olarak kaybedilir. Zırh ve can fişleri güncel durumu gösterir."),PastRaid),
            Wait("result",Loc.N("SONUCU MASAYA GETİR"),Loc.N("Sonuç kâğıdını oku, OPERASYON RAPORUNA GEÇ'e bas. Kayıplar, yaralar ve eldeki ekipman sığınağa döner; ölen kişi geri gelmez."),c=>c.ReportOpen),
            Wait("report",Loc.N("RAPORU DOSYALA"),Loc.N("Operasyon raporunda kişi sonuçlarını ve kazançları oku; raporu EVRAK'a kaldırıp masaya dön."),c=>!c.ReportOpen),
            Info("rest",Loc.N("YARALIYI DİNLENDİR"),Loc.N("Hafif yaralıyı Boşta'ya bırak: her vardiya kapanışında 1 can iyileşir. Yatan yaralı, canı dolsa bile yatış süresi bitene kadar çalışamaz. Gerekiyorsa sağlıklı birini saha rolüne geçir ve rol kitini de ona ver.")),
            Wait("print",Loc.N("KAPAĞI AÇ, FİŞİ BAS"),Loc.N("MASA'ya dön. VARDİYAYI BİTİR şalterinin önce kapağını aç, sonra koluna bas. Plan uygunsa devir fişi teleks daktilosundan basılır; eksik varsa düzeltmen gereken bilgi yazılır. Fişi basmak henüz zamanı ilerletmez."),c=>c.Printed||c.Day>0),
            Wait("file",Loc.N("YENİ VARDİYAYI BAŞLAT"),Loc.N("Daktilodaki basılı fişi alıp EVRAK kutusuna sürükle ve bırak. Yeni vardiya ancak bu dosyalama ile başlar; üretim, dinlenme ve erzak hesabı o anda işlenir."),c=>c.Day>0),
            // KOS aşama 8: hedef satırı (ONERILER Ö4). {0} canlı satırdır (GoalLine).
            Info("goal",Loc.N("HEDEF VE SAYAÇLAR"),Loc.N("Bütün sektörleri geri alınca son sunucu düşer: kazanırsın. Kaybettirenler: GÜVEN 0'da direniş dağılır, İFŞA eşikte konum çözülür, açlıkla sığınak eksik kalırsa terk edilir. ALARM eşikte YZ bir sektörü geri alır, güven düşer.\n{0}")),
            Wait("watch",Loc.N("VARDİYAYI SÜRDÜR"),Loc.N("Her vardiya: panoyu düzenle, görevi seç, fişi bas, EVRAK'a koy. Kapı çalınca ya da EVRAK'a tutanak düşünce rehber sürer.\n{0}"),c=>c.GroupDone&&c.EventDone||c.Day>=WatchLastDay),
            Topical("event",Loc.N("OLAY TUTANAĞI"),Loc.N("Teleks OLAY satırını bastı, EVRAK tepsisi yanıp sönüyor. Tepsiye tıkla, tutanağı al. Karar verilmeden devir fişi basılmaz: şalterin plakası “olay bekliyor” der."),EventTopic,c=>c.EventInHand||c.EventDone),
            Topical("decide",Loc.N("SATIR SEÇ, KARARA BAĞLA"),Loc.N("Her satırın bedeli yanında, bilinen riski altında yazar. GÜVENLİ etiketli satır her durumda açıktır ama bedelsiz değildir. Bir satır seç (1–3) ve KARAR kutusuna bas; Esc tutanağı tepsiye geri koyar."),EventTopic,c=>c.EventDone),
            Topical("door",Loc.N("KAPIDA ADAYLAR"),Loc.N("Teleks KAPI satırını bastı. KAPI tuşuyla kapıya dön, inik tezgâha tıkla. Karar vermezsen grup birkaç vardiya sonra gider."),GroupTopic,c=>c.AtCounter||c.GroupDone),
            Topical("stamp",Loc.N("KARTI OKU, DAMGALA"),Loc.N("Şüphe satırı kim olabileceğini, altındakiler getirdiğini yazar; getirdiği kit ve zırh onunla kalır. En çok birini al: KABUL bir karta, GERİ ÇEVİR fişe. Sığınak doluysa önce fişte yollanacak kişiyi işaretle; {0} erzak yolluk götürür."),GroupTopic,c=>c.GroupDone),
            Info("exposure",Loc.N("İFŞA VE İZLER"),Loc.N("Alarmın altındaki İFŞA şeridi YZ'nin seni ne kadar bildiğini sayar. Bir iz şüphedir, iki iz güçlü kanıttır; görmezden gelinen iz ifşayı artırır, tecrit her zaman bulgu vermez. İfşa {0} olunca teleks KONUM ÇÖZÜLDÜ der: sorgu, son baskın ya da son şalter yetişmezse sığınak düşer.")),
            Info("switch",Loc.N("SON ŞALTER VE ÇEKİLME"),Loc.N("Arka duvardaki kapaklı şalter harekâtı bitirir; rehber sürerken ve {0}. vardiyadan önce mühürlüdür. Plakası sonucu yazar: ÇEKİLME yenilgi sayılmaz; konum çözülmüş, sığınak eksik, açlık sürüyor ya da güven düşükse TAHLİYE olur, yenilgidir.")),
            Info("go",Loc.N("ARTIK KOMUTA SENDE"),Loc.N("Rehber bitince son şalterin rehber mührü kalkar. Sektörleri al; güveni, alarmı ve ifşayı izle. Üretim için panodaki tarif defterini kullan. Bitirmek için SONRAKİ'ye bas."))
        };
        static PlanningTutorialStep Info(string id,string title,string body)=>new PlanningTutorialStep{Id=id,Title=title,Body=body,Info=true};
        static PlanningTutorialStep Wait(string id,string title,string body,Func<PlanningTutorialContext,bool> done)=>new PlanningTutorialStep{Id=id,Title=title,Body=body,Done=done};
        static PlanningTutorialStep Topical(string id,string title,string body,Func<PlanningTutorialContext,bool> topic,Func<PlanningTutorialContext,bool> done)=>new PlanningTutorialStep{Id=id,Title=title,Body=body,Done=done,Topic=topic};
        public static int IndexOf(string id)=>Array.FindIndex(Steps,s=>s.Id==id);
        static readonly int RaidIndex=IndexOf("raidSelect");
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
                if(Current.Info||!Current.Done(c))break;
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
        /// </summary>
        public static string BodyText(PlanningTutorialStep s,Simulation sim)
        {
            if(s==null)return "";
            var b=sim?.B;
            switch(s.Id)
            {
                case "goal":case "watch":return Loc.F(s.Body,GoalLine(sim));
                case "stamp":return Loc.F(s.Body,Loc.Num(sim?.DismissSuppliesPreview??b?.dismissSupplies??0,"0.##"));
                case "exposure":return Loc.F(s.Body,b?.exposureThreshold??0);
                case "switch":return Loc.F(s.Body,b?.lastSwitchMinShift??0);
                default:return Loc.T(s.Body);
            }
        }

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
