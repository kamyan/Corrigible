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
    /// Harekât sonu raporu (Revizyon 2 · S8a/S8b): başlık satırı, sonuç damgası (KAZANILDI yeşil / KAYBEDİLDİ kırmızı) ve
    /// kayıpta neden damgası (DAĞILMA, VURULDU, SIZMA), sonucu söyleyen cümle ve altında ikinci satır, dört gösterge, işgal
    /// şeridi (x = vardiya 1…N, tam genişlik) ve kırılma anları (en çok altı), YENİDEN BAŞLA.
    /// </summary>
    [Serializable]
    public sealed class FinalReportData
    {
        public string Head, Number;
        public bool Won;
        public string Stamp, Reason;
        public string Headline, Sub;
        /// <summary>İkinci satır parola gibi (CP 700 16, kazanınca “AĞA BAĞLANMA · ANALOG KAL”); değilse düz metin (LF 19).</summary>
        public bool SubIsMotto;
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
