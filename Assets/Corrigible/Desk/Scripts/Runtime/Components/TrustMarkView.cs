using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    public enum TrustMarkVariant { Inline, NodeBadge, Legend }

    /// <summary>
    /// Güven işareti (PROMPT §1 tablo, C5 “güven katmanı”). Satır içi biçimler:
    /// ONAYLANDI = mavi mühür (+ “onaylandı”), RAPOR = kesik kenarlı karbon fiş “~ RAPOR”,
    /// ŞÜPHELİ = turuncu kalem halkası + Kalam “şüpheli”, BİLİNMİYOR = kesik daire içinde “?” + “BİLİNMİYOR”.
    /// Düğüm rozeti ve lejant yalnızca ikondur. Durum renginden bağımsızdır.
    /// </summary>
    [ExecuteAlways]
    public sealed class TrustMarkView : MonoBehaviour
    {
        [SerializeField] TrustLevel level;
        [SerializeField] TrustMarkVariant variant;
        [SerializeField] bool showWord = true;
        [Tooltip("Boşsa varsayılan kelime (onaylandı / ~ RAPOR / şüpheli / BİLİNMİYOR).")]
        [SerializeField] string word;
        [Tooltip("0 = varsayılan yazı boyu; tabloda ve teleksde Kalam 14.")]
        [SerializeField] float textSize;
        [Tooltip("ŞÜPHELİ: halka ikonu yerine metin içinde “? şüpheli” (önizleme, teleks).")]
        [SerializeField] bool iconless;
        [SerializeField] Image icon;
        [Tooltip("RAPOR fişi: dolgu + kesik kenar.")]
        [SerializeField] PaperPanel slip;
        [SerializeField] TMP_Text label;

        public TrustLevel Level => level;

        void OnEnable() { DeskTheme.Changed += Apply; DeskLoc.Instance.Changed += Apply; Apply(); }
        void OnDisable() { DeskTheme.Changed -= Apply; DeskLoc.Instance.Changed -= Apply; }
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(TrustLevel t, bool withWord = true, string customWord = null)
        {
            if (level == t && showWord == withWord && word == customWord) return;
            level = t; showWord = withWord; word = customWord;
            Apply();
        }

        static string DefaultWord(TrustLevel t) => t switch
        {
            TrustLevel.Verified => Loc.N("onaylandı"),
            TrustLevel.Report => Loc.N("~ RAPOR"),
            TrustLevel.Suspicious => Loc.N("şüpheli"),
            _ => Loc.N("BİLİNMİYOR"),
        };

        public void Apply()
        {
            var theme = DeskTheme.Active;
            if (theme == null || icon == null) return;
            var rt = (RectTransform)transform;

            if (variant != TrustMarkVariant.Inline)
            {
                DeskUi.SetActive(slip, false);
                DeskUi.SetActive(label, false);
                var s = variant == TrustMarkVariant.NodeBadge
                    ? level switch { TrustLevel.Verified => "node_badge_verified", TrustLevel.Report => "node_badge_report", TrustLevel.Suspicious => "legend_suspicious", _ => "legend_unknown" }
                    : level switch { TrustLevel.Verified => "mark_seal_14", TrustLevel.Report => "legend_report", TrustLevel.Suspicious => "legend_suspicious", _ => "legend_unknown" };
                DeskUi.SetActive(icon, true);
                DeskSprites.Apply(icon, s);
                DeskUi.SetColor(icon, Color.white);
                icon.SetNativeSize();
                Place(icon.rectTransform, 0, icon.rectTransform.sizeDelta.x, icon.rectTransform.sizeDelta.y, icon.rectTransform.sizeDelta.y);
                rt.sizeDelta = icon.rectTransform.sizeDelta;
                return;
            }

            string w = showWord ? DeskLoc.Instance.T(string.IsNullOrEmpty(word) ? DefaultWord(level) : word) : null;
            float h, iconW = 0, iconH = 0, gap = 0;
            string iconSprite = null;
            DeskFont font; float size, spacing = 0; string ink;
            switch (level)
            {
                case TrustLevel.Verified:
                    iconSprite = "mark_seal_16"; iconW = iconH = 16; gap = 5; h = 16;
                    font = DeskFont.LfRegular; size = 12; ink = "ink.ink";
                    break;
                case TrustLevel.Report:
                    h = 18; font = DeskFont.CpBold; size = 11; spacing = 5; ink = "ink.carbon";
                    break;
                case TrustLevel.Suspicious:
                    iconSprite = iconless ? null : "mark_suspicious"; iconW = 20; iconH = 18; gap = 2; h = iconless ? 14 : 18;
                    font = DeskFont.KalamBold; size = 15; ink = "trust.suspicious.ink";
                    if (iconless && w != null) w = "? " + w;
                    break;
                default:
                    iconSprite = "mark_unknown_17"; iconW = iconH = 17; gap = 4; h = 17;
                    font = DeskFont.CpBold; size = 11; spacing = 5; ink = "trust.unknown.ink";
                    break;
            }

            bool isSlip = level == TrustLevel.Report;
            DeskUi.SetActive(slip, isSlip);
            DeskUi.SetActive(icon, iconSprite != null);
            if (iconSprite != null)
            {
                DeskSprites.Apply(icon, iconSprite);
                DeskUi.SetColor(icon, Color.white);
            }

            DeskUi.SetActive(label, !string.IsNullOrEmpty(w) || isSlip);
            float textW = 0;
            if (label.gameObject.activeSelf)
            {
                var fa = theme.Font(font);
                if (label.font != fa) label.font = fa;
                if (textSize > 0 && level == TrustLevel.Suspicious) size = textSize;
                if (!Mathf.Approximately(label.fontSize, size)) label.fontSize = size;
                if (!Mathf.Approximately(label.characterSpacing, spacing)) label.characterSpacing = spacing;
                DeskUi.Tint(label, ink);
                var t = isSlip ? DeskLoc.Instance.T(string.IsNullOrEmpty(word) ? "~ RAPOR" : word) : w;
                DeskUi.SetText(label, t);
                textW = Mathf.Round(DeskUi.TextWidth(label, t));
            }

            float x = 0;
            if (isSlip)
            {
                // Kısa fiş (“~”, istatistik kartı) 3 px iç boşluk; tam fiş 5 px
                float pad = !string.IsNullOrEmpty(word) && word.Length <= 2 ? 3 : 5, border = 1;
                float sw = textW + (pad + border) * 2;
                Place((RectTransform)slip.transform, 0, sw, h, h);
                Place(label.rectTransform, pad + border, textW, h, h);
                x = sw;
            }
            else
            {
                if (iconSprite != null) { Place(icon.rectTransform, 0, iconW, iconH, h); x = iconW; }
                if (label.gameObject.activeSelf) { x += iconSprite != null ? gap : 0; Place(label.rectTransform, x, textW, h, h); x += textW; }
            }
            if (rt.sizeDelta != new Vector2(x, h)) rt.sizeDelta = new Vector2(x, h);
        }

        /// <summary>Çocuğu sol kenardan x'e, dikeyde ortaya yerleştirir.</summary>
        static void Place(RectTransform c, float x, float w, float h, float rowH)
        {
            c.anchorMin = c.anchorMax = new Vector2(0, 0.5f);
            c.pivot = new Vector2(0, 0.5f);
            c.anchoredPosition = new Vector2(x, 0);
            c.sizeDelta = new Vector2(w, h);
        }
    }
}
