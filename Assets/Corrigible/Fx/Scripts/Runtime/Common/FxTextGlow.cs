using TMPro;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxTextGlow : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public TMP_Text text;
        [SerializeField] public string preset="GlowCyan35";
        void OnEnable()=>Refresh();
        public void Refresh(){if(theme!=null&&text!=null&&text.font!=null)text.fontSharedMaterial=theme.Glow(preset,text.font);}
    }
}
