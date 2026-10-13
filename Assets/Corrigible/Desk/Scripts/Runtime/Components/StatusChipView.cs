using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Durum çipi (C5): renk + ikon + kelime. 22 px, 1,5 px renkli kenar, dizin kâğıdı, Franklin 800 12 .08em, ikon 14.
    /// Güven kanalından (damga) ayrıdır.
    /// </summary>
    [ExecuteAlways]
    public sealed class StatusChipView : MonoBehaviour
    {
        [SerializeField] StatusKind status = StatusKind.Threat;
        [Tooltip("Boşsa varsayılan kelime (TEHDİT / UYARI / BİLGİ / İYİ SONUÇ).")]
        [SerializeField] string word;
        [SerializeField] Image fill;
        [SerializeField] Image frame;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text label;

        void OnEnable() { DeskTheme.Changed += Apply; DeskLoc.Instance.Changed += Apply; Apply(); }
        void OnDisable() { DeskTheme.Changed -= Apply; DeskLoc.Instance.Changed -= Apply; }
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(StatusKind s, string customWord = null)
        {
            if (status == s && word == customWord) return;
            status = s; word = customWord; Apply();
        }

        public static string DefaultWord(StatusKind s) => s switch
        {
            StatusKind.Threat => Loc.N("TEHDİT"),
            StatusKind.Warning => Loc.N("UYARI"),
            StatusKind.Info => Loc.N("BİLGİ"),
            StatusKind.Good => Loc.N("İYİ SONUÇ"),
            _ => "",
        };

        public void Apply()
        {
            var theme = DeskTheme.Active;
            if (theme == null || label == null) return;
            string ink = DeskUi.StatusKey(status);
            DeskSprites.Apply(fill, "sh_fill_r2"); DeskUi.Tint(fill, "paper.index");
            DeskSprites.Apply(frame, "sh_frame_r2_1"); DeskUi.Tint(frame, ink);   // CSS 1,5 px → referansta 1 px
            DeskSprites.Apply(icon, status switch
            {
                StatusKind.Threat => "ic_octagon_14", StatusKind.Warning => "ic_warning_14",
                StatusKind.Good => "ic_check_circle_14", _ => "ic_info_14",
            });
            DeskUi.Tint(icon, ink);
            if (label.font != theme.lfExtraBold) label.font = theme.lfExtraBold;
            DeskUi.Tint(label, ink);
            var t = DeskLoc.Instance.T(string.IsNullOrEmpty(word) ? DefaultWord(status) : word);
            DeskUi.SetText(label, t);

            // pad 0 8 0 6, kenar 1, ikon 14, aralık 5
            float tw = Mathf.Round(DeskUi.TextWidth(label, t));
            Row(icon.rectTransform, 1 + 6, 14, 14);
            Row(label.rectTransform, 1 + 6 + 14 + 5, tw, 22);
            var rt = (RectTransform)transform;
            var size = new Vector2(1 + 6 + 14 + 5 + tw + 8 + 1, 22);
            if (rt.sizeDelta != size) rt.sizeDelta = size;
        }

        static void Row(RectTransform c, float x, float w, float h)
        {
            c.anchorMin = c.anchorMax = new Vector2(0, 0.5f); c.pivot = new Vector2(0, 0.5f);
            c.anchoredPosition = new Vector2(x, 0); c.sizeDelta = new Vector2(w, h);
        }
    }
}
