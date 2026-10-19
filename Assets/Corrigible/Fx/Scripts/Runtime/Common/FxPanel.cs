using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxPanel : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public Image glass,shadow,topBracket,bottomBracket;
        [SerializeField] public string variant="fx_glass",accent="cy";
        void OnEnable()=>Refresh();
        public void Refresh(){if(theme==null)return;theme.Apply(glass,variant,Color.white);if(shadow!=null)theme.Apply(shadow,"fx_shadow_panel",Color.white);foreach(var b in new[]{topBracket,bottomBracket})if(b!=null)theme.Apply(b,"fx_bracket_10",theme.Color(accent));}
    }
}
