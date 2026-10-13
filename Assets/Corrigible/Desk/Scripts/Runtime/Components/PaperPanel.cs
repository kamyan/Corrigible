using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    public enum PaperKind { Page, Carbon, IndexCard, Manila, ManilaBack, Note, Plaque, Slip, LabelChip, Card, Label }

    /// <summary>
    /// Kâğıt yüzey (PROMPT §6.6): dolgu + çerçeve + sert gölge (+ dizin kartında çizgi dokusu ve kırmızı çizgi).
    /// Tür değişince sprite ve tema anahtarları kendiliğinden uygulanır; İllüzyon paleti <see cref="ThemeTint"/> ile gelir.
    /// </summary>
    [ExecuteAlways]
    public sealed class PaperPanel : MonoBehaviour
    {
        [SerializeField] PaperKind kind;
        [SerializeField] Image fill;
        [SerializeField] Image frame;
        [Tooltip("Dizin kartı: mavi çizgi dokusu (tx_index_lines, kart üstünden 35 px aşağıdan başlar).")]
        [SerializeField] RawImage lines;
        [Tooltip("Dizin kartı: 33–35 px arasındaki kırmızı çizgi.")]
        [SerializeField] Image rule;
        [SerializeField] ShadowPreset shadow = ShadowPreset.None;
        [Tooltip("Ön ayar yerine TSV'deki kesin gölge (x, y aşağı pozitif).")]
        [SerializeField] bool customShadow;
        [SerializeField] Vector2 shadowOffset = new Vector2(2, 2);
        [SerializeField] Color shadowColor = new Color(0.235f, 0.176f, 0.078f, 0.35f);
        [Tooltip("Dizin kartı başlık yüksekliği: kırmızı çizgi bunun altında 2 px.")]
        [SerializeField] float headerHeight = 33;

        public PaperKind Kind => kind;

        public void SetKind(PaperKind k)
        {
            if (kind == k) return;
            kind = k;
            Apply();
        }

        void OnEnable() => Apply();
        void OnValidate() => DeskUi.Deferred(this, Apply);
        void OnRectTransformDimensionsChange() => FitLines();

        (string fill, string fillKey, string frame, string frameKey) Spec() => kind switch
        {
            PaperKind.Page => ("sh_fill_r1", "paper.page", "sh_frame_r1_1", "paper.pageEdge"),
            PaperKind.Carbon => ("sh_fill_r2", "paper.onion", "sh_frame_r2_1", "paper.onionEdge"),
            PaperKind.IndexCard => ("sh_fill_r2", "paper.index", "sh_frame_r2_1", "paper.indexEdge"),
            PaperKind.Manila => ("sh_fill_r4_tl0", "paper.manila", "sh_frame_r4_tl0_1", "paper.manilaEdge"),
            PaperKind.ManilaBack => ("sh_fill_r4_tl0", "paper.manilaBack", "sh_frame_r4_tl0_1", "paper.manilaBackEdge"),
            PaperKind.Note => ("sh_fill_r0", "paper.sticky", null, null),
            PaperKind.Plaque => ("sh_fill_r2", "desk.plaque", "sh_frame_r2_1", "desk.plaqueBorder"),
            PaperKind.Slip => ("sh_fill_r2", "trust.report.paper", "sh_dash_r2_1", "ink.carbon"),
            PaperKind.LabelChip => ("sh_fill_r1", "paper.mapLabelChip", null, null),
            PaperKind.Card => ("sh_fill_r2", "paper.index", "sh_frame_r2_1", "paper.indexEdge"),
            PaperKind.Label => ("sh_fill_r2", "paper.page", null, null),
            _ => ("sh_fill_r1", "paper.page", "sh_frame_r1_1", "paper.pageEdge"),
        };

        public void Apply()
        {
            var (f, fk, fr, frk) = Spec();
            if (fill != null)
            {
                DeskSprites.Apply(fill, f);
                DeskUi.Tint(fill, fk);
                ApplyShadow();
            }
            if (frame != null)
            {
                DeskUi.SetActive(frame, fr != null);
                if (fr != null) { DeskSprites.Apply(frame, fr); DeskUi.Tint(frame, frk); }
            }
            bool index = kind == PaperKind.IndexCard && ((RectTransform)transform).rect.height > headerHeight + 2;
            if (lines != null) DeskUi.SetActive(lines, index);
            if (rule != null)
            {
                DeskUi.SetActive(rule, index);
                if (index) { DeskSprites.Apply(rule, "sh_fill_r0"); DeskUi.Tint(rule, "paper.ruleRed"); }
            }
            FitLines();
        }

        void ApplyShadow()
        {
            var sh = fill.GetComponent<Shadow>();
            bool want = customShadow || shadow != ShadowPreset.None;
            if (!want) { if (sh != null) sh.enabled = false; return; }
            if (sh == null) return;   // Shadow bileşeni prefab'da dolgu üstünde durur
            sh.enabled = true;
            sh.useGraphicAlpha = true;
            Vector2 off; Color col;
            if (customShadow) { off = shadowOffset; col = shadowColor; }
            else { var s = DeskTheme.Active != null ? DeskTheme.Active.Shadow(shadow) : default; off = s.offset; col = s.color; }
            sh.effectDistance = new Vector2(off.x, -off.y);
            sh.effectColor = col;
        }

        /// <summary>Çizgi dokusunu tasarım fazına hizalar: doku kart üstünden 35 px'te başlar, 22 px periyot, çizgi döşemenin son satırı.</summary>
        void FitLines()
        {
            if (lines == null || kind != PaperKind.IndexCard || !lines.gameObject.activeSelf) return;
            var rt = (RectTransform)lines.transform;
            var parent = (RectTransform)transform;
            float top = headerHeight + 2;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(1, 1);
            rt.offsetMax = new Vector2(-1, -top);
            float w = Mathf.Max(1, parent.rect.width - 2), h = Mathf.Max(1, parent.rect.height - top - 1);
            const float tileW = 8, tileH = 22;
            float hh = h / tileH;
            // Unity UV'si alttan başlar: üst kenar döşeme üstüne denk gelsin
            lines.uvRect = new Rect(0, Mathf.Ceil(hh) - hh, w / tileW, hh);
        }

        /// <summary>TSV'deki kesin gölgeyi uygula (ör. eylem kartı 2,2 rgba(60,45,20,.35)).</summary>
        public void SetShadow(Vector2 offset, Color color)
        {
            customShadow = true; shadowOffset = offset; shadowColor = color;
            if (fill != null) ApplyShadow();
        }

        public void SetShadow(ShadowPreset preset)
        {
            customShadow = false; shadow = preset;
            if (fill != null) ApplyShadow();
        }
    }
}
