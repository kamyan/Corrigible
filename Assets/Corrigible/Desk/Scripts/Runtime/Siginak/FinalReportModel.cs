using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Son raporun göstergesi: etiket, değer (CP 700 46), açıklama; değer rengi.</summary>
    [Serializable]
    public sealed class FinalIndicator
    {
        public string Label, Value, Sub;
        /// <summary>Değerin rengi: düz mürekkep, mavi (bütün bölgeler özgür) ya da kırmızı (kaybedilen harekâtta kalan nüfus).</summary>
        public OperationTone Tone;
    }

    /// <summary>Kırılma anı: grafikte numaralı yuvarlak (vardiyada), altta aynı numarayla satır (V.### + olay).</summary>
    [Serializable]
    public sealed class FinalMoment
    {
        public int Number, Shift;
        public string Code, Text;
        /// <summary>Kaybedilen harekâtın son anı: yuvarlak kırmızı dolu.</summary>
        public bool Alert;
    }

    /// <summary>
    /// Raporun sonucu (KOS aşama 6): kazanıldı (yeşil KAZANILDI), kaybedildi (kırmızı KAYBEDİLDİ ya da TAHLİYE) ya da düzenli
    /// çekilme (mavi ÇEKİLDİ; yenilgi sayılmaz, hiçbir yerde kırmızı uyarı almaz).
    /// </summary>
    public enum FinalOutcome { Won, Lost, Withdrawn }

    /// <summary>
    /// Ölçüt kutusunun satırı (KOS aşama 6): etiket, değer, sınır ve hüküm. <see cref="Met"/> true “TUTTU” (yeşil), false
    /// “TUTMADI” (kırmızı), null nötr (hüküm sütunu düz mürekkep ya da boş). <see cref="Alert"/>: satır kırmızı (tahliye nedeni).
    /// </summary>
    [Serializable]
    public sealed class FinalCriterionRow
    {
        public string Label, Value, Limit, Verdict;
        public bool? Met;
        public bool Alert;
    }

    /// <summary>SİCİL sekmesinin bir sütunu (KOS aşama 6): başlık ve çok satırlı gövde; <see cref="Alert"/> başlık kırmızı.</summary>
    [Serializable]
    public sealed class FinalSection
    {
        public string Title, Body;
        public bool Alert;
    }

    /// <summary>
    /// Harekât sonu raporu (Revizyon 2 · S8a/S8b): başlık satırı, sonuç damgası (KAZANILDI yeşil / KAYBEDİLDİ kırmızı) ve
    /// kayıpta neden damgası (DAĞILMA, VURULDU, SIZMA), sonucu söyleyen cümle ve altında ikinci satır, dört gösterge, işgal
    /// şeridi (x = vardiya 1…N, tam genişlik) ve kırılma anları (en çok altı), YENİDEN BAŞLA.
    /// </summary>
    [Serializable]
    public sealed class FinalReportData
    {
        public string Head, Number;
        /// <summary>Sonuç (KOS aşama 6); <see cref="Won"/> geriye dönük olarak <c>Outcome == Won</c>.</summary>
        public FinalOutcome Outcome = FinalOutcome.Lost;
        public bool Won
        {
            get => Outcome == FinalOutcome.Won;
            set => Outcome = value ? FinalOutcome.Won : Outcome == FinalOutcome.Won ? FinalOutcome.Lost : Outcome;
        }
        public string Stamp, Reason;
        public string Headline, Sub;
        /// <summary>İkinci satır parola gibi (CP 700 16, kazanınca “AĞA BAĞLANMA · ANALOG KAL”); değilse düz metin (LF 19).</summary>
        public bool SubIsMotto;
        /// <summary>Ton satırı (KOS aşama 6): başlığın altında üçüncü satır, tonun gerekçesi; boşsa gizli.</summary>
        public string ToneLine;
        /// <summary>Ölçüt kutusu (KOS aşama 6): başlık, en çok beş satır ve kural satırı; satır yoksa kutu gizli.</summary>
        public string CriteriaTitle, CriteriaRule;
        public List<FinalCriterionRow> Criteria = new List<FinalCriterionRow>();
        /// <summary>Alt yarının sekmeleri (HAREKÂT / SİCİL) ve SİCİL'in üç sütunu (gizli bilgi yalnız burada; yalnız son varken).</summary>
        public string MainTab, RecordTab;
        public List<FinalSection> Record = new List<FinalSection>();
        public List<FinalIndicator> Indicators = new List<FinalIndicator>();
        public string ChartTitle, ChartRange, OccupiedLabel, RetakenLabel;
        /// <summary>Harekâtın uzunluğu (vardiya); grafiğin x ekseni 1…<see cref="Shifts"/>.</summary>
        public int Shifts;
        /// <summary>İşgal serisi: x vardiya (1…N), y işgal yüzdesi (0–100).</summary>
        public List<Vector2> Points = new List<Vector2>();
        public string MomentsTitle;
        public List<FinalMoment> Moments = new List<FinalMoment>();
        public string Action;
    }
}
