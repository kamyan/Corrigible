using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sekme rozeti (C1 .dot): sarı sayı rozeti (doğrulanmamış sayı) ya da kırmızı “yeni kayıt” noktası.
    /// Sayı: 18 px yükseklik, en az 18 genişlik, 1,5 px mürekkep kenar, Courier 700 11. Nokta: 14 px kırmızı + 1,5 px kenar.
    /// </summary>
    [ExecuteAlways]
    public sealed class CountBadgeView : MonoBehaviour
    {
        [SerializeField] bool dot;
        [SerializeField] int count = 3;
        [SerializeField] Image fill;
        [SerializeField] Image ring;
        [SerializeField] TMP_Text label;

        void OnEnable() { DeskTheme.Changed += Apply; Apply(); }
        void OnDisable() => DeskTheme.Changed -= Apply;
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void SetCount(int n) { dot = false; count = n; Apply(); }
        public void SetDot() { dot = true; Apply(); }

        public void Apply()
        {
            var theme = DeskTheme.Active;
            if (theme == null || fill == null) return;
            var rt = (RectTransform)transform;
            DeskUi.SetActive(label, !dot);
            if (dot)
            {
                DeskSprites.Apply(fill, "sh_circle_16"); DeskUi.Tint(fill, "badges.newDot");
                DeskSprites.Apply(ring, "sh_ring_o14_1_5"); DeskUi.Tint(ring, "badges.border");
                if (rt.sizeDelta != new Vector2(16, 16)) rt.sizeDelta = new Vector2(16, 16);
                return;
            }
            DeskSprites.Apply(fill, "sh_pill_h18_fill"); DeskUi.Tint(fill, "badges.countBg");
            DeskSprites.Apply(ring, "sh_pill_h18_ring"); DeskUi.Tint(ring, "badges.border");
            if (label.font != theme.cpBold) label.font = theme.cpBold;
            DeskUi.Tint(label, "ink.ink");
            var t = DeskFormat.Int(count);
            DeskUi.SetText(label, t);
            float w = Mathf.Max(18, Mathf.Round(DeskUi.TextWidth(label, t)) + 8 + 3);
            if (rt.sizeDelta != new Vector2(w, 18)) rt.sizeDelta = new Vector2(w, 18);
        }
    }
}
