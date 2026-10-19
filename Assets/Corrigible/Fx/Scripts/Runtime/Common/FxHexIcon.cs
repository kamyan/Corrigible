using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxHexIcon : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public Image frame,icon,glow;
        [SerializeField] public bool small;
        [SerializeField] public string iconKey="ic_fund_l",accent="cy";
        void OnEnable()=>Refresh();
        public void Refresh(){if(theme==null)return;theme.Apply(frame,small?"fx_hex_32":"fx_hex_40",theme.Color(accent));theme.Apply(icon,iconKey,theme.Color(accent));if(glow!=null){var c=theme.Color(accent);c.a=.35f;theme.Apply(glow,"fx_glow_dot_32",c);glow.material=theme.additive;}}
    }
}
