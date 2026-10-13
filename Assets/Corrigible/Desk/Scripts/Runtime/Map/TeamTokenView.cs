using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Saha ekibi jetonu (C1 §7.1 madde 6): siyah elmas + krem harf; varsa varış etiketi (“B. Avrupa'ya · 2 gün”).</summary>
    public sealed class TeamTokenView : MonoBehaviour
    {
        [SerializeField] TMP_Text letter;
        [SerializeField] RectTransform tag;
        [SerializeField] DeskRow tagRow;
        [SerializeField] TMP_Text tagText;

        public void Bind(TeamTokenVM t)
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(t.X, -t.Y);
            DeskUi.SetText(letter, t.Id);
            bool hasTag = !string.IsNullOrEmpty(t.Tag);
            DeskUi.SetActive(tag, hasTag);
            if (!hasTag) return;
            DeskUi.SetText(tagText, DeskLoc.Instance.T(t.Tag));
            tagRow.Layout();
            // Etiket konumu jetona göre değil haritaya göre (TSV)
            tag.anchoredPosition = new Vector2(t.TagX - t.X, -(t.TagY - t.Y));
        }
    }
}
