using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Baskın yan panelinde bir ekip üyesi kartı (RaidUnitCard prefab'ı).</summary>
    public sealed class RaidUnitCard : MonoBehaviour
    {
        [SerializeField] StyledButton button;
        [SerializeField] TMP_Text letter;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text status;
        [SerializeField] TMP_Text ability;
        [SerializeField] Image cardBackground;
        [SerializeField] Image letterBackground;
        [SerializeField] Image selectedRule;
        [SerializeField] TMP_Text stateStamp;
        [SerializeField] TMP_Text hpPips;
        [SerializeField] TMP_Text movement;

        RectTransform rect;
        Vector2 restPosition;

        public StyledButton Button => button;

        void Awake()
        {
            rect = transform as RectTransform;
            if (rect != null) restPosition = rect.anchoredPosition;
        }

        public void Set(string l, string name, string st, string ab, bool selected, bool dead, UiTheme theme,
            bool spent = false, bool interactive = true, int hp = -1, int maxHp = -1, int move = 0, string state = null)
        {
            letter.text = l; title.text = name; status.text = st; ability.text = ab;
            var paper = selected && !dead ? theme.selectedCardBg : spent || dead ? theme.panel : theme.raise;
            var ink = dead ? theme.threat : theme.fg;
            button.SetColors(paper, selected && !dead ? theme.verify : theme.line2, ink);
            button.SetEnabled(!dead && interactive);
            if (cardBackground != null) cardBackground.color = paper;
            if (letterBackground != null) letterBackground.color = spent ? theme.line2 : theme.raise;
            if (selectedRule != null) { selectedRule.color = theme.verify; selectedRule.gameObject.SetActive(selected && !dead); }
            title.color = ink; letter.color = ink; status.color = dead ? theme.threat : theme.muted;
            ability.color = theme.muted;
            if (stateStamp != null)
            {
                stateStamp.text = UiUtil.Up(state ?? st);
                stateStamp.color = dead ? theme.threat : spent ? theme.muted : theme.verify;
            }
            if (hpPips != null && hp >= 0)
            {
                hpPips.text = Loc.F("{0} {1}/{2}", new string('■', Mathf.Max(0, hp)) + new string('□', Mathf.Max(0, maxHp - hp)), hp, maxHp);
                hpPips.color = ink;
            }
            if (movement != null) movement.text = Loc.F("Hareket {0} kare", move);
            if (rect != null) rect.anchoredPosition = restPosition + (selected && !dead ? Vector2.up * 5f : Vector2.zero);
        }
    }
}
