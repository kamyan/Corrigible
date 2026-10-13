using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bölge etiketi (C1 §7.1 madde 8): 160 px kutu içinde ortalı / sola / sağa yaslı çip; Courier 700 11 .05em ink2,
    /// zemin #ECE6D4 %92, iç boşluk 1/4. Seçili bölgede çip fosforlu sarı, yazı mürekkep.
    /// </summary>
    public sealed class RegionLabelView : MonoBehaviour
    {
        [SerializeField] RectTransform chip;
        [SerializeField] Image chipFill;
        [SerializeField] TMP_Text label;
        [Tooltip("Düğümün üstüne gelince çipe mürekkep kenar (açık karar 4).")]
        [SerializeField] Image hoverFrame;

        public void SetHover(bool on) => DeskUi.SetActive(hoverFrame, on);

        public void Bind(RegionNodeVM n)
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(n.LabelX, -n.LabelY);
            rt.sizeDelta = new Vector2(n.LabelW, 15);

            DeskUi.SetText(label, n.Label);
            DeskUi.Tint(label, n.Selected ? "ink.ink" : "ink.ink2");
            DeskUi.Tint(chipFill, n.Selected ? "paper.highlighter" : "paper.mapLabelChip", n.Selected ? 1 : 0.92f);

            float w = Mathf.Round(DeskUi.TextWidth(label, n.Label)) + 8;
            float ax = n.LabelAlign == "left" ? 0 : n.LabelAlign == "right" ? 1 : 0.5f;
            chip.anchorMin = chip.anchorMax = new Vector2(ax, 0.5f);
            chip.pivot = new Vector2(ax, 0.5f);
            chip.anchoredPosition = Vector2.zero;
            chip.sizeDelta = new Vector2(w, 15);
        }
    }
}
