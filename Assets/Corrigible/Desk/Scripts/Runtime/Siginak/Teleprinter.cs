using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masadaki teleks kâğıdı (K1 notu): başlık “TELEKS · KYGA-07” ve son satırlar; en yeni satır makineye en yakın olan.
    /// Yeni satır harf harf basılır (motion.telexCharS). Kod CP 700, durum CP 700 (yapılamadıysa kırmızı), ileti CP 400.
    /// </summary>
    public sealed class Teleprinter : MonoBehaviour
    {
        [SerializeField] TMP_Text paper;
        [SerializeField] Renderer paperRenderer;
        [SerializeField, Min(1)] int visibleLines = 5;
        [SerializeField] string header = "TELEKS · KYGA-07";

        readonly List<TelexLine> shown = new List<TelexLine>();
        float typedSince;
        int typingLength;
        bool typing;

        public void Configure(TMP_Text text, Renderer renderer, string headerSource) { paper = text; paperRenderer = renderer; header = headerSource; }

        public void Show(IReadOnlyList<TelexLine> lines, bool animateNewest)
        {
            shown.Clear();
            for (int i = lines.Count - 1; i >= 0 && shown.Count < visibleLines; i--) shown.Add(lines[i]);
            typing = animateNewest && shown.Count > 0 && Application.isPlaying;
            typedSince = Time.unscaledTime;
            typingLength = shown.Count > 0 ? Plain(shown[0]).Length : 0;
            Render(typing ? 0 : typingLength);
        }

        void Update()
        {
            if (!typing) return;
            float perChar = Mathf.Max(0.005f, SiginakTheme.Active.Number("motion.telexCharS", 0.03f));
            int n = Mathf.Min(typingLength, Mathf.FloorToInt((Time.unscaledTime - typedSince) / perChar));
            Render(n);
            if (n >= typingLength) typing = false;
        }

        /// <summary>Kâğıdın orta noktası kameranın görüş alanında mı.</summary>
        public bool IsVisibleFrom(Camera view)
        {
            if (paperRenderer == null) return false;
            var p = view.WorldToViewportPoint(paperRenderer.bounds.center);
            return p.z > 0 && p.x > 0.02f && p.x < 0.98f && p.y > 0.02f && p.y < 0.98f;
        }

        /// <summary>
        /// Kâğıt görüş alanında ve satırları ekranda en az <paramref name="minPixels"/> px (1080p'ye göre) mi. Değilse satır
        /// ekranın altındaki şeritte de basılır (Aşama 10 punto taraması: masadaki kâğıt hiçbir pozda 14 px'e ulaşmıyor).
        /// </summary>
        public bool IsReadableFrom(Camera view, float minPixels)
        {
            if (paper == null || !IsVisibleFrom(view)) return false;
            // TMP 3D: punto 10 = 1 m em
            float em = paper.fontSize * 0.1f * Mathf.Abs(paper.transform.lossyScale.y);
            var a = view.WorldToScreenPoint(paper.transform.position);
            var b = view.WorldToScreenPoint(paper.transform.position + paper.transform.up * em);
            return Vector2.Distance(a, b) * 1080f / Mathf.Max(1, view.pixelHeight) >= minPixels;
        }

        static string Plain(TelexLine l) => l.code + "  " + l.status + "  " + l.message;

        void Render(int newestChars)
        {
            if (paper == null) return;
            var t = SiginakTheme.Active;
            string ink3 = ColorUtility.ToHtmlStringRGB(t.ColorOf("ink3"));
            var sb = new StringBuilder();
            sb.Append("<b>").Append(Corrigible.Core.Loc.T(header)).Append("</b>\n");
            for (int i = 0; i < shown.Count; i++)
            {
                var l = shown[i];
                if (i == 0 && newestChars < Plain(l).Length)
                {
                    sb.Append("\n").Append(Plain(l).Substring(0, newestChars));
                    continue;
                }
                string statusColor = ColorUtility.ToHtmlStringRGB(t.ColorOf(l.kind == TelexKind.Failed ? "status.threat" : "ink"));
                sb.Append("\n<color=#").Append(ink3).Append(">").Append(l.code).Append("</color>  <b><color=#").Append(statusColor).Append(">")
                    .Append(l.status).Append("</color></b>\n").Append(l.message);
            }
            paper.text = sb.ToString();
        }
    }
}
