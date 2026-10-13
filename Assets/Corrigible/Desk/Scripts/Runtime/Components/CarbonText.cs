using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// RAPOR değeri (karbon kopya): mor-gri mürekkep + (1,2; −0,6) px kaydırılmış %30 ikinci kopya (CSS text-shadow).
    /// Gölge ayrı bir TMP'dir; malzeme değişmediği için aynı batch'te kalır. “≈” öneki VM metninde gelir.
    /// </summary>
    [ExecuteAlways]
    public sealed class CarbonText : MonoBehaviour
    {
        [SerializeField] TMP_Text main;
        [SerializeField] TMP_Text ghost;
        [SerializeField] bool carbon = true;
        [SerializeField] string inkKey = "ink.ink";
        [SerializeField] Vector2 offset = new Vector2(1.2f, -0.6f);
        [Range(0, 1)] [SerializeField] float ghostAlpha = 0.3f;
        [Tooltip("Kök genişliği metnin genişliği olur (satır içinde DeskRow ile dizilirken).")]
        [SerializeField] bool fitWidth = true;

        public TMP_Text Main => main;

        void OnEnable() { DeskTheme.Changed += Apply; Apply(); }
        void OnDisable() => DeskTheme.Changed -= Apply;
        void OnValidate() => DeskUi.Deferred(this, Apply);

        /// <param name="isCarbon">RAPOR ise karbon; değilse düz mürekkep (<paramref name="ink"/>).</param>
        public void Set(string text, bool isCarbon, string ink = "ink.ink")
        {
            carbon = isCarbon; inkKey = ink;
            DeskUi.SetText(main, text);
            Apply();
        }

        public void Apply()
        {
            if (main == null) return;
            if (fitWidth)
            {
                var rt = (RectTransform)transform;
                float w = Mathf.Round(DeskUi.TextWidth(main, main.text));
                if (!Mathf.Approximately(rt.sizeDelta.x, w)) rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
            }
            // İllüzyonda karbon kâğıt krem olur, ≈ kalkar (metni VM verir); burada yalnızca mürekkep ve kopya
            DeskUi.Tint(main, carbon ? "ink.carbon" : inkKey);
            if (ghost == null) return;
            DeskUi.SetActive(ghost, carbon);
            if (!carbon) return;
            if (ghost.font != main.font) ghost.font = main.font;
            if (!Mathf.Approximately(ghost.fontSize, main.fontSize)) ghost.fontSize = main.fontSize;
            if (!Mathf.Approximately(ghost.characterSpacing, main.characterSpacing)) ghost.characterSpacing = main.characterSpacing;
            if (ghost.alignment != main.alignment) ghost.alignment = main.alignment;
            DeskUi.SetText(ghost, main.text);
            DeskUi.Tint(ghost, "ink.carbon", ghostAlpha);
            var g = ghost.rectTransform; var m = main.rectTransform;
            g.anchorMin = m.anchorMin; g.anchorMax = m.anchorMax; g.pivot = m.pivot; g.sizeDelta = m.sizeDelta;
            g.anchoredPosition = m.anchoredPosition + offset;
        }
    }
}
