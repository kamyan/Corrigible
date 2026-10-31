using System;
using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>Operasyon raporunun SONUÇLAR satırının değer biçimi (Revizyon 2 · S7).</summary>
    public enum OperationTone
    {
        /// <summary>Düz mürekkep.</summary>
        Plain,
        /// <summary>Artı: yeşil.</summary>
        Good,
        /// <summary>±0 / değişmedi: gri.</summary>
        Neutral,
        /// <summary>Sektör geri alındı: mavi.</summary>
        Retaken,
        /// <summary>İşgal, eşik, düşen uzman: kırmızı.</summary>
        Alert,
    }

    /// <summary>SONUÇLAR satırı; <see cref="AlarmCells"/> &gt; 0 ise değerin solunda alarm ölçeği.</summary>
    [Serializable]
    public sealed class OperationRow
    {
        public string Label, Value;
        public OperationTone Tone;
        /// <summary>Alarm ölçeği (yalnız başarısız raporda): dolu hücre, toplam hücre, kırmızı bölgenin başladığı hücre (1 tabanlı).</summary>
        public int AlarmCells, AlarmSteps, AlarmHighFrom;
    }

    /// <summary>KAZANÇ gözü: 32 px yeşil sayı ve etiket (yalnız başarılı raporda).</summary>
    [Serializable]
    public sealed class OperationGain
    {
        public string Value, Label;
        public bool Zero;
    }

    /// <summary>
    /// Operasyon raporu (Revizyon 2 · S7c/S7d): başlık satırı ve numara, künye (TARİH, SONUÇ damgası, HEDEF, SÜRE), sonucu
    /// söyleyen başlık ve altında tek cümle, başarılıda KAZANÇ gözleri, SONUÇLAR ve MASAYA DÖN. Aynı cümle iki kez geçmez;
    /// başarısız raporda ödül alanı hiç yoktur.
    /// </summary>
    [Serializable]
    public sealed class OperationReportData
    {
        public string Head, Number;
        public string DateLabel, Date, ResultLabel, ResultStamp, TargetLabel, Target, DurationLabel, Duration;
        public bool Success;
        public string Title, Sentence;
        public string GainsTitle, ResultsTitle;
        public List<OperationGain> Gains = new List<OperationGain>();
        public List<OperationRow> Results = new List<OperationRow>();
        public string Action;
        /// <summary>Sayaç rafının ± fişleri (VARDİYA, FON, ERZAK, MALZEME); boş olan asılmaz. Yalnız başarılı raporda.</summary>
        public string[] Slips = new string[4];
    }
}
