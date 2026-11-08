using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Corrigible.Desk
{
    public enum TelexKind { Info, Done, Failed, Alarm }

    /// <summary>Teleks satırı: kod “VVV/NN” (vardiya / o vardiyanın satır sayacı), durum, ileti. Metinler gösterime hazır.</summary>
    [Serializable]
    public struct TelexLine
    {
        public string code, status, message;
        public TelexKind kind;
    }

    /// <summary>
    /// Bütün geri bildirimin tek kapısı (PROMPT §4 TelexFeed): “Emir uygulandı” / “Yapılamadı” ve dizilerin satırları
    /// buradan geçer. Satır masadaki teleks kâğıdına basılır; teleks kadraj dışındaysa aynı satır ekranın altından kâğıt
    /// şerit olarak kayar. Son 30 satır tutulur (yan kayıt dosyasına yazılır).
    /// </summary>
    public sealed class TelexFeed : MonoBehaviour
    {
        [SerializeField] Teleprinter printer;
        [SerializeField] TelexStrip strip;
        [SerializeField] Camera view;
        [SerializeField, Min(1)] int keep = 30;

        readonly List<TelexLine> lines = new List<TelexLine>();
        int shift = -1, counter;

        public IReadOnlyList<TelexLine> Lines => lines;
        public event Action<TelexLine> Posted;

        public void Configure(Teleprinter teleprinter, TelexStrip overlayStrip, Camera camera) { printer = teleprinter; strip = overlayStrip; view = camera; }

        /// <summary>
        /// Yeni satır; kodu vardiyaya göre sayar. <paramref name="header"/>: hat açılış satırı “VVV/00” (sayaç ilerlemez;
        /// K7 açılışı “001/00 KYGA-07 HAT AÇIK”). <paramref name="typeSeconds"/> &gt; 0: satırın tamamı o sürede basılır
        /// (Revizyon 3 · R19b2 “VARDİYA DEVREDİLDİ” 1,2 s); değilse harf başına <c>motion.telexCharS</c>.
        /// </summary>
        public TelexLine Post(int shiftNumber, string status, string message, TelexKind kind, bool header = false, float typeSeconds = -1)
        {
            if (shiftNumber != shift || header) { shift = shiftNumber; counter = 0; }
            if (!header) counter++;
            var line = new TelexLine
            {
                code = shiftNumber.ToString("000", CultureInfo.InvariantCulture) + "/" + counter.ToString("00", CultureInfo.InvariantCulture),
                status = status, message = message ?? "", kind = kind,
            };
            lines.Add(line);
            if (lines.Count > keep) lines.RemoveRange(0, lines.Count - keep);
            if (printer != null) printer.Show(lines, true, typeSeconds);
            // Kâğıt okunur boyda değilse (ya da uyarı satırıysa, ör. KAYIT VAR) satır şeritte de görünür
            if (strip != null && (!PrinterReadable || kind == TelexKind.Alarm)) strip.Enqueue(line);
            Posted?.Invoke(line);
            return line;
        }

        /// <summary>Kayıttan devam: satırları animasyonsuz geri koyar, sayacı son koddan sürdürür.</summary>
        public void Restore(IEnumerable<TelexLine> saved)
        {
            lines.Clear();
            if (saved != null) lines.AddRange(saved);
            if (lines.Count > keep) lines.RemoveRange(0, lines.Count - keep);
            shift = -1; counter = 0;
            if (strip != null) strip.Clear();
            if (lines.Count > 0)
            {
                var parts = lines[lines.Count - 1].code.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out var s) && int.TryParse(parts[1], out var n)) { shift = s; counter = n; }
            }
            if (printer != null) printer.Show(lines, false);
        }

        public void Clear() => Restore(null);

        /// <summary>Teleks kâğıdı kadrajın içinde mi.</summary>
        public bool PrinterVisible => printer != null && view != null && printer.IsVisibleFrom(view);

        /// <summary>Kâğıttaki satırlar okunur boyda (≥ 14 px) mı; değilse satır şerit olarak da gösterilir.</summary>
        public bool PrinterReadable => printer != null && view != null && printer.IsReadableFrom(view, 14);
    }
}
