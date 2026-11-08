using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Satır şablonunun türü: saha (disk + ad + nokta çizgisi + eşyalar), sığınak (ad + nokta çizgisi + görev · durum),
    /// KAPANIŞTA BEKLENEN (etiket sütunu + değer + kalın fark).</summary>
    public enum HandoverSlipRowKind { Field, Shelter, Expected }

    /// <summary>
    /// Devir fişi yüzünün satır şablonu (Revizyon 3 · R19c, PLAN W3): takım diski + harf (yalnız saha satırı), ad, nokta çizgisi,
    /// sağa yaslı değer ve ek (KAPANIŞTA'da ERZAK farkı). Saha, sığınak ve KAPANIŞTA BEKLENEN satırları aynı bileşeni farklı
    /// şablonlarla kullanır; yüz (<see cref="HandoverSlipFace"/>) havuzdan çoğaltır, dikey yerini ve kıvrım ofsetini verir, satır
    /// kendi içini yerleştirir (mm, sol üst orijin, y aşağı).
    /// <list type="bullet">
    /// <item>Saha ve sığınak: kutular satırın altına yaslı (CSS <c>align-items: flex-end</c>); her yazının taban çizgisi kendi
    /// 5 mm'lik satır kutusunda. Değer sığmazsa sağa yaslı sarar, satır uzar (punto küçülmez).</item>
    /// <item>KAPANIŞTA: etiket 21,67 mm sütunda, değer kalan genişlikte sarar, fark en sağda; üçü ortak taban çizgisinde
    /// (CSS <c>align-items: baseline</c>).</item>
    /// <item>Sığınak değeri tek yazıdır: görev kalın ve 0,04 em aralıklı, durum eki ince (CP-Regular'ın kalın ağırlığı CP-Bold).</item>
    /// </list>
    /// </summary>
    public sealed class HandoverSlipRow : MonoBehaviour
    {
        [SerializeField] HandoverSlipRowKind kind;
        [Header("Takım diski (yalnız saha)")]
        [SerializeField] Image discFill;
        [SerializeField] Image discRing;
        [SerializeField] TMP_Text discLetter;
        [Header("Yazılar")]
        [Tooltip("Saha ve sığınakta ad (CP-Bold), KAPANIŞTA'da etiket (CP-Bold).")]
        [SerializeField] TMP_Text nameText;
        [Tooltip("Ad ile değer arasındaki nokta çizgisi (döşenen sprite; KAPANIŞTA'da yok).")]
        [SerializeField] Image leader;
        [Tooltip("Sağdaki değer (CP-Regular; kalın kısımlar zengin metinle).")]
        [SerializeField] TMP_Text valueText;
        [Tooltip("KAPANIŞTA'da ERZAK farkı (CP-Bold); öteki şablonlarda yok.")]
        [SerializeField] TMP_Text suffixText;

        float height;

        public HandoverSlipRowKind Kind => kind;
        public RectTransform Rect => (RectTransform)transform;
        public Image DiscFill => discFill;
        public Image DiscRing => discRing;
        public TMP_Text DiscLetter => discLetter;
        public TMP_Text NameText => nameText;
        public Image Leader => leader;
        public TMP_Text ValueText => valueText;
        public TMP_Text SuffixText => suffixText;
        /// <summary>Son yerleşimdeki yükseklik (mm).</summary>
        public float HeightMm => height;

        /// <summary>Kurucunun (tek seferlik editör script'i) bağlaması.</summary>
        public void Configure(HandoverSlipRowKind rowKind, Image fill, Image ring, TMP_Text letter, TMP_Text name, Image dots, TMP_Text value, TMP_Text suffix)
        {
            kind = rowKind; discFill = fill; discRing = ring; discLetter = letter;
            nameText = name; leader = dots; valueText = value; suffixText = suffix;
        }

        /// <summary>
        /// Satırı yazar ve içini <paramref name="width"/> mm genişliğe yerleştirir; yüksekliği (mm) döner. Satırın kendi konumuna
        /// dokunmaz (yüz verir).
        /// </summary>
        public float Apply(HandoverSlipLines.Row row, HandoverSlipMetrics m, HandoverSlipPalette p, float width)
        {
            row ??= new HandoverSlipLines.Row();
            height = kind == HandoverSlipRowKind.Expected ? ApplyExpected(row, m, p, width) : ApplyListed(row, m, p, width);
            var rt = Rect;
            var size = new Vector2(width, height);
            if (rt.sizeDelta != size) rt.sizeDelta = size;
            return height;
        }

        // ---------------------------------------------------------------- saha ve sığınak

        float ApplyListed(HandoverSlipLines.Row row, HandoverSlipMetrics m, HandoverSlipPalette p, float width)
        {
            float line = m.rowMm;
            float x = 0;
            bool field = kind == HandoverSlipRowKind.Field;
            SetActive(discFill, field); SetActive(discRing, field); SetActive(discLetter, field);
            SetActive(suffixText, false);
            if (field)
            {
                float d = m.discMm, top = line - m.discBottomMm - d;
                PlaceBox(discFill, 0, top, d, d, p.disc);
                PlaceBox(discRing, 0, top, d, d, p.discRim);
                HandoverSlipText.Style(discLetter, m.discEmMm, 0, p.ink, false);
                HandoverSlipText.Set(discLetter, row.Disc);
                // harf: diskin içinde ortada (satır kutusu "normal" yükseklikte, disk ortasında)
                float letterLine = HandoverSlipText.NormalLine(discLetter, m.discEmMm);
                float baseline = top + (d - letterLine) / 2 + HandoverSlipText.Ascent(discLetter, m.discEmMm);
                HandoverSlipText.PlaceLine(discLetter, d / 2, baseline, d, letterLine, TextAlignmentOptions.Baseline);
                x = d + m.discGapMm;
            }

            HandoverSlipText.Style(nameText, m.nameEmMm, 0, p.ink, false);
            HandoverSlipText.Set(nameText, row.Name);
            float nameWidth = HandoverSlipText.Width(nameText, row.Name);
            HandoverSlipText.PlaceLine(nameText, x, HandoverSlipText.Baseline(nameText, m.nameEmMm, line), nameWidth, line, TextAlignmentOptions.BaselineLeft);

            string value = field ? row.Value : ShelterValue(row, m);
            float valueEm = field ? m.valueEmMm : m.jobEmMm;
            HandoverSlipText.Style(valueText, valueEm, 0, p.ink, false);
            float valueLeftLimit = x + nameWidth + 2 * m.leaderMarginMm + m.leaderMinMm;
            float available = Mathf.Max(1f, width - valueLeftLimit);
            float valueWidth = HandoverSlipText.Width(valueText, value);
            int lines = 1;
            if (valueWidth > available + 0.01f)
            {
                // Sığmıyor: sağa yaslı sarar (tasarım "satır sarar, punto küçülmez"); satır uzar
                HandoverSlipText.Style(valueText, valueEm, 0, p.ink, true, line);
                valueWidth = available;
                HandoverSlipText.Set(valueText, value);
                HandoverSlipText.PlaceLine(valueText, width, HandoverSlipText.Baseline(valueText, valueEm, line), valueWidth, line, TextAlignmentOptions.BaselineRight);
                lines = HandoverSlipText.Lines(valueText);
            }
            else
            {
                HandoverSlipText.Set(valueText, value);
                HandoverSlipText.PlaceLine(valueText, width, HandoverSlipText.Baseline(valueText, valueEm, line), valueWidth, line, TextAlignmentOptions.BaselineRight);
            }

            // nokta çizgisi: adın sonundan değerin başına (her iki yanda 1,25 mm), satırın altından 1,25 mm yukarıda
            float from = x + nameWidth + m.leaderMarginMm, to = width - valueWidth - m.leaderMarginMm;
            bool dots = !string.IsNullOrEmpty(value) && to - from >= m.leaderMinMm - 0.01f;
            SetActive(leader, dots);
            if (dots) PlaceBox(leader, from, line - m.leaderBottomMm - m.leaderHeightMm, to - from, m.leaderHeightMm, p.leader);
            return Mathf.Max(m.rowMm, lines * line);
        }

        /// <summary>Sığınak değeri: kalın, aralıklı görev ve ince durum eki (zengin metin; sözcükler biçimleyiciden gelir).</summary>
        static string ShelterValue(HandoverSlipLines.Row row, HandoverSlipMetrics m)
        {
            string job = string.IsNullOrEmpty(row.Value) ? string.Empty
                : string.Concat("<b><cspace=", Em(m.jobSpacingEm), ">", row.Value, "</cspace></b>");
            if (string.IsNullOrEmpty(row.Suffix)) return job;
            return string.Concat(job, HandoverSlipFormat.Separator, row.Suffix);
        }

        // ---------------------------------------------------------------- KAPANIŞTA BEKLENEN

        float ApplyExpected(HandoverSlipLines.Row row, HandoverSlipMetrics m, HandoverSlipPalette p, float width)
        {
            float line = m.rowMm;
            SetActive(discFill, false); SetActive(discRing, false); SetActive(discLetter, false); SetActive(leader, false);
            bool delta = !string.IsNullOrEmpty(row.Suffix);
            SetActive(suffixText, delta);

            HandoverSlipText.Style(nameText, m.labelEmMm, m.labelSpacingEm * 100f, p.label, false);
            HandoverSlipText.Style(valueText, m.valueEmMm, 0, p.ink, true, line);
            if (delta) HandoverSlipText.Style(suffixText, m.deltaEmMm, 0, p.ink, false);

            // ortak taban çizgisi: satırdaki yazıların en aşağıdaki taban çizgisi
            float bLabel = HandoverSlipText.Baseline(nameText, m.labelEmMm, line);
            float bValue = HandoverSlipText.Baseline(valueText, m.valueEmMm, line);
            float baseline = Mathf.Max(bLabel, bValue);
            if (delta) baseline = Mathf.Max(baseline, HandoverSlipText.Baseline(suffixText, m.deltaEmMm, line));

            HandoverSlipText.Set(nameText, row.Name);
            HandoverSlipText.PlaceLine(nameText, 0, baseline, m.labelColumnMm, line, TextAlignmentOptions.BaselineLeft);

            float right = width;
            if (delta)
            {
                HandoverSlipText.Set(suffixText, row.Suffix);
                float w = HandoverSlipText.Width(suffixText, row.Suffix);
                HandoverSlipText.PlaceLine(suffixText, width, baseline, w, line, TextAlignmentOptions.BaselineRight);
                right = width - w - m.deltaGapMm;
            }

            string value = row.Value ?? string.Empty;
            if (!string.IsNullOrEmpty(row.Strong) && value.EndsWith(row.Strong, System.StringComparison.Ordinal))
                value = string.Concat(value.Substring(0, value.Length - row.Strong.Length), "<b>", row.Strong, "</b>");
            HandoverSlipText.Set(valueText, value);
            HandoverSlipText.PlaceLine(valueText, m.labelColumnMm, baseline, Mathf.Max(1f, right - m.labelColumnMm), line, TextAlignmentOptions.BaselineLeft);
            int lines = HandoverSlipText.Lines(valueText);
            return Mathf.Max(m.rowMm, lines * line);
        }

        // ---------------------------------------------------------------- yardımcılar

        static string Em(float em) => em.ToString("0.###", CultureInfo.InvariantCulture) + "em";

        static void SetActive(Component c, bool on)
        {
            if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
        }

        static void PlaceBox(Graphic g, float x, float y, float w, float h, Color colour)
        {
            if (g == null) return;
            HandoverSlipText.Place(g.rectTransform, x, y, w, h, new Vector2(0, 1));
            if (g.color != colour) g.color = colour;
        }
    }
}
