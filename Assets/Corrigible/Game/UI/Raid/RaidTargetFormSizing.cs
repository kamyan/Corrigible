using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    // Uzun çeviriler ve birden fazla rakip yeteneği, formun altındaki günlüğü aşağı iter.
    public sealed class RaidTargetFormSizing : MonoBehaviour
    {
        [SerializeField] RectTransform form;
        [SerializeField] TMP_Text modifiers;
        [SerializeField] RectTransform log;
        string previousText;
        float previousWidth = -1;

        void LateUpdate()
        {
            if (form == null || modifiers == null || log == null) return;
            float width = modifiers.rectTransform.rect.width;
            if (previousText == modifiers.text && Mathf.Approximately(previousWidth, width)) return;
            previousText = modifiers.text;
            previousWidth = width;
            float contentHeight = modifiers.GetPreferredValues(modifiers.text, width, 1000).y;
            float height = Mathf.Max(262, 217 + contentHeight);
            form.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            modifiers.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(47, contentHeight));
            log.offsetMax = new Vector2(log.offsetMax.x, -(64 + height + 18));
        }
    }
}
