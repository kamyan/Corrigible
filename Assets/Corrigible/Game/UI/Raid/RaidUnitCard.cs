using TMPro;
using UnityEngine;

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

        public StyledButton Button => button;

        public void Set(string l, string name, string st, string ab, bool selected, bool dead, UiTheme theme)
        {
            letter.text = l; title.text = name; status.text = st; ability.text = ab;
            button.SetColors(theme.raise, selected ? theme.fg : theme.line2);
            button.SetEnabled(!dead);
        }
    }
}
