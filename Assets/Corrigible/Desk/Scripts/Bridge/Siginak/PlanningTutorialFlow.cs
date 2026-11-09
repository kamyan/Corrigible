using System;
using Corrigible.Core;
namespace Corrigible.Desk.Bridge
{
    /// <summary>Kartlı başlangıcın okuduğu durum; kuralları ve oyun kaydını değiştirmez.</summary>
    public sealed class PlanningTutorialContext
    {
        public bool Blocked, BoardOpen, PersonSelected, AssignedCook, SlipOpen, RaidOrdered, RaidLoaded, RaidOver;
        public bool RaidSelected, RaidMoved, RaidActed, RaidTurnEnded, ReportOpen, Printed;
        public int Raids, Day;
    }
    public sealed class PlanningTutorialStep
    {
        public string Id, Title, Body;
        public bool Info;
        public Func<PlanningTutorialContext,bool> Done;
    }
    /// <summary>İlk pano, gerçek baskın ve fiziksel vardiya kapanışı. Eylemden sonra ilerler; sunum dışında etkisi yoktur.</summary>
    public sealed class PlanningTutorialFlow
    {
        static bool PastRaid(PlanningTutorialContext c)=>c.RaidOver||c.Raids>0&&!c.RaidOrdered;
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
            Wait("print",Loc.N("KAPAĞI AÇ, FİŞİ BAS"),Loc.N("MASA'ya dön. VARDİYAYI BİTİR şalterinin önce kapağını aç, sonra koluna bas. Plan uygunsa devir fişi çıkar; eksik varsa düzeltmen gereken bilgi yazılır. Fişi basmak henüz zamanı ilerletmez."),c=>c.Printed||c.Day>0),
            Wait("file",Loc.N("YENİ VARDİYAYI BAŞLAT"),Loc.N("Şalterin yanındaki basılı fişi alıp EVRAK kutusuna sürükle ve bırak. Yeni vardiya ancak bu dosyalama ile başlar; üretim, dinlenme ve erzak hesabı o anda işlenir."),c=>c.Day>0),
            Info("go",Loc.N("ARTIK KOMUTA SENDE"),Loc.N("Temel döngü: panoda hazırlan, görev seç, baskından dön, devir fişini dosyala. Üretim için panodaki tarif defterini kullan. Kapı çaldığında kabul tezgâhındaki talimatı oku; büyüteç belgeleri karşılaştırır. Öğreticiyi bitirmek için SONRAKİ'ye bas."))
        };
        static PlanningTutorialStep Info(string id,string title,string body)=>new PlanningTutorialStep{Id=id,Title=title,Body=body,Info=true};
        static PlanningTutorialStep Wait(string id,string title,string body,Func<PlanningTutorialContext,bool> done)=>new PlanningTutorialStep{Id=id,Title=title,Body=body,Done=done};
        public int Index{get;private set;}
        public bool Finished{get;private set;}
        public PlanningTutorialStep Current=>Finished?null:Steps[Index];
        public PlanningTutorialFlow(int index=0){Index=Math.Max(0,Math.Min(Steps.Length-1,index));}
        public bool Update(PlanningTutorialContext c)
        {
            if(Finished||c.Blocked)return false;int before=Index;
            // Oyuncu rehberden önce sefere çıktıysa pano bilgisi savaşın üstünde beklemez.
            if(c.RaidOrdered&&Index<7)Index=7;
            while(!Finished&&!Current.Info&&Current.Done(c))Next();
            return before!=Index||Finished;
        }
        public void Acknowledge(){if(!Finished&&Current.Info)Next();}
        public void Skip()=>Finished=true;
        void Next(){if(Index+1>=Steps.Length)Finished=true;else Index++;}
    }
}
