using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masadaki teleks kâğıdı (K1 notu): başlık “TELEKS · KYGA-07” ve son satırlar; en yeni satır makineye en yakın olan.
    /// Yeni satır harf harf basılır (motion.telexCharS). Kod CP 700, durum CP 700 (yapılamadıysa kırmızı), ileti CP 400.
    /// Revizyon 2 (S1): son ileti makinenin önüne asılı ön kâğıtta büyük yazılır (K1'den okunur); yeni iletide eski ön kâğıt
    /// koparılır ve masaya düşer.
    /// </summary>
    public sealed class Teleprinter : MonoBehaviour
    {
        [SerializeField] TMP_Text paper;
        [SerializeField] Renderer paperRenderer;
        [SerializeField, Min(1)] int visibleLines = 5;
        [SerializeField] string header = "TELEKS · KYGA-07";
        [Tooltip("Ön kâğıt: son ileti (kod, durum, ileti) ve altında bir önceki satır.")]
        [SerializeField] TMP_Text front;
        [Tooltip("Koparılan ön kâğıdın kopyası (düşüş için; kapalı durur).")]
        [SerializeField] Transform torn;
        [SerializeField] TMP_Text tornText;
        [SerializeField, Min(0.05f)] float tearSeconds = 0.4f;

        readonly List<TelexLine> shown = new List<TelexLine>();
        float typedSince, tornAt = -100;
        int typingLength;
        bool typing;
        Vector3 tornPos;
        Quaternion tornRot;
        string fitStatus;
        int fitPercent = StatusPercent;

        /// <summary>Ön kâğıttaki durum satırının boyu (ileti %100; S1: durum 66 / ileti 50 doku px).</summary>
        const int StatusPercent = 132;

        public void Configure(TMP_Text text, Renderer renderer, string headerSource) { paper = text; paperRenderer = renderer; header = headerSource; }

        public void Show(IReadOnlyList<TelexLine> lines, bool animateNewest)
        {
            // Yeni ileti: eski ön kâğıt koparılır (kopyası düşer)
            if (animateNewest && Application.isPlaying && torn != null && front != null && shown.Count > 0 && lines.Count > 0 &&
                !Same(lines[lines.Count - 1], shown[0]))
            {
                tornText.text = front.text;
                if (tornAt < 0) { tornPos = torn.localPosition; tornRot = torn.localRotation; }
                tornAt = Time.unscaledTime;
                torn.gameObject.SetActive(true);
            }
            shown.Clear();
            for (int i = lines.Count - 1; i >= 0 && shown.Count < visibleLines; i--) shown.Add(lines[i]);
            typing = animateNewest && shown.Count > 0 && Application.isPlaying;
            typedSince = Time.unscaledTime;
            typingLength = shown.Count > 0 ? Plain(shown[0]).Length : 0;
            Render(typing ? 0 : typingLength);
        }

        static bool Same(TelexLine a, TelexLine b) => a.code == b.code && a.status == b.status && a.message == b.message;

        void Update()
        {
            Tear();
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
        /// Ön kâğıt varsa (Revizyon 2) onun durum satırı ölçülür.
        /// </summary>
        public bool IsReadableFrom(Camera view, float minPixels)
        {
            var text = front != null ? front : paper;
            if (text == null || !IsVisibleFrom(view)) return false;
            // TMP 3D: punto 10 = 1 m em; ön kâğıdın durum satırı %132 (uzun durumda biraz küçülür)
            float em = text.fontSize * (front != null ? fitPercent / 100f : 1) * 0.1f * Mathf.Abs(text.transform.lossyScale.y);
            var a = view.WorldToScreenPoint(text.transform.position);
            var b = view.WorldToScreenPoint(text.transform.position + text.transform.up * em);
            return Vector2.Distance(a, b) * 1080f / Mathf.Max(1, view.pixelHeight) >= minPixels;
        }

        static string Plain(TelexLine l) => l.code + "  " + l.status + "  " + l.message;

        /// <summary>Koparılan kâğıt öne doğru devrilip masaya düşer ve kaybolur (S1 notu › hareket: 0,4 s).</summary>
        void Tear()
        {
            if (torn == null || tornAt < 0) return;
            float t = (Time.unscaledTime - tornAt) / tearSeconds;
            if (t >= 1)
            {
                torn.gameObject.SetActive(false);
                torn.localPosition = tornPos; torn.localRotation = tornRot;
                tornAt = -100;
                return;
            }
            torn.localPosition = tornPos + new Vector3(0.02f * t, -0.11f * t * t, -0.06f * t);
            torn.localRotation = tornRot * Quaternion.Euler(-70 * t, 0, 12 * t);
        }

        /// <summary>Ön kâğıt: kod (soluk), durum (büyük, yapılamadıysa kırmızı), ileti; altında önceki satır küçük.</summary>
        void RenderFront(int newestChars)
        {
            if (front == null) return;
            if (shown.Count == 0) { front.text = ""; return; }
            var t = SiginakTheme.Active;
            var l = shown[0];
            string ink3 = ColorUtility.ToHtmlStringRGB(t.ColorOf("ink3")), status = ColorUtility.ToHtmlStringRGB(t.ColorOf(l.kind == TelexKind.Failed ? "status.threat" : "ink"));
            // Basılan satır harf harf: önce kod, sonra durum, sonra ileti dolar
            string code = l.code ?? "", st = l.status ?? "", msg = l.message ?? "";
            int n = newestChars;
            string Take(ref int left, string s) { var r = s.Substring(0, Mathf.Clamp(left, 0, s.Length)); left -= s.Length + 2; return r; }
            var sb = new StringBuilder();
            sb.Append("<b><color=#").Append(ink3).Append(">").Append(Take(ref n, code)).Append("</color></b>\n");
            sb.Append("<size=").Append(StatusFit(st)).Append("%><b><color=#").Append(status).Append(">").Append(Take(ref n, st)).Append("</color></b></size>\n");
            sb.Append(Take(ref n, msg));
            if (shown.Count > 1 && newestChars >= Plain(l).Length)
                sb.Append("\n<size=68%><color=#").Append(ink3).Append("><b>").Append(shown[1].code).Append("  ").Append(shown[1].status).Append("</b></color></size>");
            front.text = sb.ToString();
        }

        /// <summary>
        /// Durum satırı tek satırda kalsın (EN “KNOCK AT THE DOOR”, TR “KYGA-07 HAT AÇIK”): sığmazsa %132'den küçülür, ileti
        /// boyunun altına inmez. Satır başına bir kez ölçülür.
        /// </summary>
        int StatusFit(string status)
        {
            if (status == fitStatus) return fitPercent;
            fitStatus = status;
            fitPercent = StatusPercent;
            var r = front.rectTransform.rect;
            float width = r.width - front.margin.x - front.margin.z;
            float need = front.GetPreferredValues("<size=" + StatusPercent + "%><b>" + status + "</b></size>", 100000, 0).x;
            if (width > 0 && need > width) fitPercent = Mathf.Max(100, Mathf.FloorToInt(StatusPercent * width / need));
            return fitPercent;
        }

        void Render(int newestChars)
        {
            RenderFront(newestChars);
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
