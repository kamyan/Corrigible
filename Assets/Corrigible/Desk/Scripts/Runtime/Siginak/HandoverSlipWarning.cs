using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Devir fişi yüzünün uyarı kutusu (Revizyon 3 · R19c, PLAN W3): 0,31 mm çerçeve, 1,04 mm (5 px) sol çubuk, ⚠ simgesi
    /// (sözcüksüz sprite), önek (“KAPI ·”, “DOSYA ·”; kalın, 2,92 mm, 0,06 em aralık) ve sarabilen metin (3,125 mm, satır
    /// 4,17 mm). Önek metnin ilk satırında, metinle aynı yazıda zengin metin olarak durur (tasarımda satır içi). Yüz
    /// (<see cref="HandoverSlipFace"/>) havuzdan çoğaltır, dikey yerini ve kıvrım ofsetini verir; kutu kendi içini yerleştirir.
    /// </summary>
    public sealed class HandoverSlipWarning : MonoBehaviour
    {
        [Header("Çerçeve (üst, sağ, alt ince; sol kalın çubuk)")]
        [SerializeField] Image frameTop;
        [SerializeField] Image frameRight;
        [SerializeField] Image frameBottom;
        [SerializeField] Image frameLeft;
        [SerializeField] Image icon;
        [Tooltip("Önek + metin (CP-Regular; önek kalın zengin metin).")]
        [SerializeField] TMP_Text text;

        float height;
        int lines;

        public RectTransform Rect => (RectTransform)transform;
        public TMP_Text Text => text;
        public Image Icon => icon;
        public Image FrameLeft => frameLeft;
        /// <summary>Son yerleşimdeki yükseklik (mm) ve metnin satır sayısı.</summary>
        public float HeightMm => height;
        public int LineCount => lines;

        /// <summary>Kurucunun (tek seferlik editör script'i) bağlaması.</summary>
        public void Configure(Image top, Image right, Image bottom, Image left, Image warnIcon, TMP_Text body)
        {
            frameTop = top; frameRight = right; frameBottom = bottom; frameLeft = left; icon = warnIcon; text = body;
        }

        /// <summary>Kutuyu yazar ve <paramref name="width"/> mm genişliğe yerleştirir; yüksekliği (mm) döner.</summary>
        public float Apply(string prefix, string body, HandoverSlipMetrics m, HandoverSlipPalette p, float width)
        {
            float b = m.boxBorderMm, bar = m.boxBarMm;
            float textX = bar + m.boxPadLeftMm + m.boxIconColumnMm + m.boxIconGapMm;
            float textW = Mathf.Max(1f, width - textX - m.boxPadRightMm - b);
            float top = b + m.boxPadTopMm;

            HandoverSlipText.Style(text, m.boxTextEmMm, 0, p.ink, true, m.boxTextLineMm);
            HandoverSlipText.Set(text, Compose(prefix, body, m));
            float baseline = top + HandoverSlipText.Baseline(text, m.boxTextEmMm, m.boxTextLineMm);
            HandoverSlipText.PlaceLine(text, textX, baseline, textW, m.boxTextLineMm, TextAlignmentOptions.BaselineLeft);
            lines = Mathf.Max(1, HandoverSlipText.Lines(text));

            float inner = Mathf.Max(lines * m.boxTextLineMm, m.boxIconTopMm + m.boxIconMm);
            height = top + inner + m.boxPadBottomMm + b;

            if (icon != null)
            {
                HandoverSlipText.Place(icon.rectTransform, bar + m.boxPadLeftMm, top + m.boxIconTopMm, m.boxIconMm, m.boxIconMm, new Vector2(0, 1));
                if (icon.color != p.ink) icon.color = p.ink;
            }
            Bar(frameTop, 0, 0, width, b, p.ink);
            Bar(frameBottom, 0, height - b, width, b, p.ink);
            Bar(frameLeft, 0, 0, bar, height, p.ink);
            Bar(frameRight, width - b, 0, b, height, p.ink);

            var rt = Rect;
            var size = new Vector2(width, height);
            if (rt.sizeDelta != size) rt.sizeDelta = size;
            return height;
        }

        /// <summary>Önek kalın, küçük ve aralıklı; arada bir boşluk, sonra metin (tasarımın satır içi <c>&lt;b&gt;</c>'si).</summary>
        static string Compose(string prefix, string body, HandoverSlipMetrics m)
        {
            if (string.IsNullOrEmpty(prefix)) return body ?? string.Empty;
            string size = m.boxPrefixEmMm.ToString("0.####", CultureInfo.InvariantCulture);
            string spacing = m.boxPrefixSpacingEm.ToString("0.###", CultureInfo.InvariantCulture);
            return string.Concat("<b><size=", size, "><cspace=", spacing, "em>", prefix, "</cspace></size></b> ", body ?? string.Empty);
        }

        static void Bar(Image g, float x, float y, float w, float h, Color colour)
        {
            if (g == null) return;
            HandoverSlipText.Place(g.rectTransform, x, y, w, h, new Vector2(0, 1));
            if (g.color != colour) g.color = colour;
        }
    }
}
