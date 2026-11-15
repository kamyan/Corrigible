using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    public sealed partial class PlanningBoardState
    {
        /// <summary>Sığınak olayları; pano varsa her zaman kuruludur (olaylar kapalı olsa da).</summary>
        public ShelterEventState ShelterEvents;
    }

    /// <summary>Açık olay: karar bekleyen tek kâğıt. Yalnız kimlik ve sayı taşır; metin gösterimde kurulur.</summary>
    [Serializable]
    public sealed class ShelterEventInstance
    {
        public string Kind, PersonId, OtherId;
        public int OpenedShift;
        /// <summary>Kâğıdın yazdığı gerçek sayı: çalınan ya da kaybedilen miktar, ağır yaralıda kalan yatış.</summary>
        public double Amount;
        /// <summary>Kaybın türü ("erzak" | "metal") ve izin yeri ("uretim" | "erzak" | "nobet"); yoksa null.</summary>
        public string Resource, Area;
        /// <summary>Hedef giderse geri yüklenen aralık ve bekleme değerleri.</summary>
        public int PrevNextEventShift, PrevLastShift;
    }

    [Serializable]
    public sealed class ShelterEventCount
    {
        public string Kind;
        public int Count, LastShift = -1;
    }

    /// <summary>
    /// Olay günlüğü. <see cref="Kind"/>: olay kimliği (karar), ya da "bulgu", "bulgu_yok", "sokuldu", "ayrilma", "kapandi", "ifsa" (Option: kaynak), "vadeli", "konum" (ifşa eşikte: son vardiya), "konum_kalkti".
    /// </summary>
    [Serializable]
    public sealed class ShelterEventLogEntry
    {
        public int Shift;
        public string Kind, Option, PersonId, OtherId;
        public double Amount;
    }

    /// <summary>Ayrı tutulan çift: <c>S.Day &lt; Until</c> iken aynı baskın kadrosunda olamazlar.</summary>
    [Serializable]
    public sealed class ShelterFeud
    {
        public string A, B;
        public int Until;
    }

    /// <summary>Kararın vadeli sonucu. Çekimleri bir sonraki kapanışta yapılır; alanları gizlidir (sonuç önceden okunamaz).</summary>
    [Serializable]
    public sealed class ShelterDelayed
    {
        /// <summary>"buyuk_hirsizlik" | "yatis_uzar" | "kavga_yara" | "yas" | "cagri_guven".</summary>
        internal string Kind;
        internal string PersonId, OtherId;
        internal List<string> Mates = new List<string>();
        internal int DecidedShift, DueShift;
        internal bool Drawn, Occurs, Ready;
        internal double Pick;
    }

    /// <summary>
    /// Okuyucu çıktısı (durum değildir): açık olayın bir seçeneği, o dildeki metinleriyle. <see cref="KitLock"/>: kısıtlayan ya da
    /// tecrit eden seçenekte hedef bir rolün tek kitini bağlı taşıyorsa ayrı bedel satırı (D3); yoksa null. Sunum alt alta yazar.
    /// </summary>
    public sealed class ShelterEventOption
    {
        public string Id, Name, Cost, Risk, KitLock, ClosedReason;
        public bool Open, Safe;
    }

    [Serializable]
    public sealed class ShelterEventState
    {
        /// <summary>Açık olay; yoksa null.</summary>
        public ShelterEventInstance Pending;
        /// <summary>Olağan olay en erken bu kapanışta (<c>S.Day >=</c>) açılır.</summary>
        public int NextEventShift;
        public List<ShelterEventCount> Counts = new List<ShelterEventCount>();
        public List<ShelterEventLogEntry> Log = new List<ShelterEventLogEntry>();
        public List<ShelterFeud> Feuds = new List<ShelterFeud>();
        internal List<ShelterDelayed> Delayed = new List<ShelterDelayed>();

        public ShelterEventCount Count(string kind) => Counts.Find(c => c.Kind == kind);
    }
}
