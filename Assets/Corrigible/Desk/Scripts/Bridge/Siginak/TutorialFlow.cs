using System;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Eğitimin okuduğu anlık durum: masa, baskın tahtası, raporlar ve sığınak (kontrolcü her karede doldurur).</summary>
    public sealed class TutorialContext
    {
        /// <summary>Açılış, emir belgesi, vardiya dizisi, ayarlar, son rapor ya da CRT geçişi: not gizlenir, adım beklemede.</summary>
        public bool Blocked;
        public bool SlipOpen, RaidOrdered, RaidLoaded, RaidOver, ReportOpen, ShelterPose, ShiftReportOpen;
        public int WorkshopLevel, WorkshopWorkers, Day, Raids;
    }

    /// <summary>Eğitimin bir adımı: analistin notu ve adımı bitiren koşul (bilgi adımı TAMAM ile geçer).</summary>
    public sealed class TutorialStep
    {
        public string Id, Title, Body;
        public bool Info;
        public Func<TutorialContext, TutorialFlow, bool> Done;
    }

    /// <summary>
    /// Rehberli ilk vardiyalar: ilk harekâtta analistin notları oyuncuyu masadan baskına (adım adım oynanan senaryolu ilk
    /// baskın, <see cref="TutorialRaid"/>), rapordan sığınağa ve vardiya şalterine götürür. Her adım oyuncunun eylemini bekler; koşulu zaten sağlanmış adımlar atlanır. Simülasyona dokunmaz; yalnız okur.
    /// </summary>
    public sealed class TutorialFlow
    {
        public static readonly TutorialStep[] Steps =
        {
            Info("intro", Loc.N("KOMUTA ODASI"),
                Loc.N("Burası Sığınak 07. Üstteki sayaçlar fonu, erzakı, malzemeyi, özgür bölgeleri ve sivil GÜVEN'i sayar; duvardaki YZ ALARMI eşiğe varınca YZ karşı saldırır. Zaman yalnız vardiya şalterini indirince ilerler.")),
            Wait("slip", Loc.N("GÖREV FİŞİ"),
                Loc.N("Masadaki GÖREVLER tuşuna bas: harita açılır, cephedeki Batı Avrupa'nın görev fişi gelir."),
                (c, f) => c.SlipOpen || c.RaidOrdered),
            Wait("order", Loc.N("BASKIN EMRİ"),
                Loc.N("Fiş sıradaki sektörü, bedelini, ödülünü, bölgenin özelliğini ve kadroyu yazar. BASKIN EMRİ VER'e bas."),
                (c, f) => c.RaidOrdered),
            // Senaryolu ilk baskında not, rehberin adımlarını yazar (SiginakTutorial); bu metin rehbersiz baskın içindir
            Wait("raid", Loc.N("TAKTİK TAHTA"),
                Loc.N("Kırmızı oklar gözcünün telsizden bildirdiği makine hamleleri, bayraktaki sayı sırası. Uzmanı seç (1–3), mavi kareye yürü, Q ile yeteneği seç ve hedefe tıkla; Enter emri iletir. Kasayı altıncı turdan önce al."),
                (c, f) => c.RaidOver || !c.RaidLoaded && !c.RaidOrdered && c.Raids > 0),
            Wait("result", Loc.N("SONUÇ"),
                Loc.N("Baskın bitti. Alınan hasar sığınağa yara olarak döner; düşen uzman günlerce yatar. OPERASYON RAPORUNA GEÇ'e bas: masaya dönülür, rapor açılır."),
                (c, f) => f.ReportSeen),
            Wait("report", Loc.N("OPERASYON RAPORU"),
                Loc.N("Rapor ödülü, yaraları, sivil güveni ve YZ alarmını yazar. MASAYA DÖN ile kapat."),
                (c, f) => f.ReportSeen && !c.ReportOpen && !c.RaidLoaded),
            Wait("shelter", Loc.N("SIĞINAK"),
                Loc.N("Masadaki SIĞINAK tuşuna bas: kesit maketi açılır. Odalar uzmanları bir sonraki baskına hazırlar."),
                (c, f) => c.ShelterPose || c.WorkshopLevel > 0),
            Wait("repair", Loc.N("ATÖLYE"),
                Loc.N("Atölyeye tıkla ve ONAR'a bas. Atölye malzemeden zırh plakası döker; plaka baskında ilk darbeyi karşılar."),
                (c, f) => c.WorkshopLevel > 0),
            Wait("staff", Loc.N("PERSONEL"),
                Loc.N("+ PERS. ile atölyeye birini ata; boşta kalan kimse üretmez. Revir yarayı sarar, telsiz alarmı yavaşlatır; üst seviyeler özgür bölgelerle açılır."),
                (c, f) => c.WorkshopWorkers > 0),
            Wait("shift", Loc.N("VARDİYA ŞALTERİ"),
                Loc.N("Vardiyada tek baskın var. MASA tuşuyla masaya dön ve şaltere iki kez tıkla: kapak kalkar, kol iner, zaman bir vardiya ilerler."),
                (c, f) => c.Day > 0),
            Wait("shiftReport", Loc.N("VARDİYA RAPORU"),
                Loc.N("Rapor vardiyanın fonunu, üretimini, uzmanların yarasını ve alarmı yazar. Alttaki düğme onu EVRAK tepsisine kaldırır."),
                (c, f) => f.ShiftReportSeen && !c.ShiftReportOpen),
            Info("go", Loc.N("İYİ ŞANSLAR"),
                Loc.N("Batı Avrupa'nın üç sektörünü al, sonra sınırdaki bir bölgeyi seç. Her bölgenin kendi özelliği ve imza makinesi var; harekât ilerledikçe tehdit artar. Sivil GÜVEN sıfıra inerse direniş dağılır. Harekât emri EVRAK tepsisinde durur.")),
        };

        static TutorialStep Info(string id, string title, string body) => new TutorialStep { Id = id, Title = title, Body = body, Info = true };
        static TutorialStep Wait(string id, string title, string body, Func<TutorialContext, TutorialFlow, bool> done) =>
            new TutorialStep { Id = id, Title = title, Body = body, Done = done };

        public int Index { get; private set; }
        public bool Finished { get; private set; }
        /// <summary>Operasyon raporu ve vardiya raporu bu eğitimde açıldı mı (kapandığını anlamak için önce açılmış olmalı).</summary>
        public bool ReportSeen { get; private set; }
        public bool ShiftReportSeen { get; private set; }

        public TutorialStep Current => Finished ? null : Steps[Index];
        public static int Count => Steps.Length;

        /// <summary>Durumu okur; koşulu sağlanan bekleme adımlarını geçer (art arda birkaçı olabilir). Adım değiştiyse true.</summary>
        public bool Update(TutorialContext c)
        {
            if (Finished) return false;
            if (c.ReportOpen && Index >= IndexOf("result")) ReportSeen = true;
            if (c.ShiftReportOpen && c.Day > 0) ShiftReportSeen = true;
            int before = Index;
            while (!Finished && !Current.Info && Current.Done(c, this)) Next();
            return Index != before || Finished;
        }

        /// <summary>Bilgi adımının TAMAM'ı.</summary>
        public void Acknowledge()
        {
            if (!Finished && Current.Info) Next();
        }

        public void Skip() => Finished = true;

        void Next()
        {
            if (Index + 1 >= Steps.Length) { Finished = true; return; }
            Index++;
        }

        static int IndexOf(string id) => Array.FindIndex(Steps, s => s.Id == id);
    }
}
