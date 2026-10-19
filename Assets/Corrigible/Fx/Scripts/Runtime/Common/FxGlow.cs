using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxGlow : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public Image image;
        [SerializeField] public string spriteKey="fx_glow_frame",colorKey="cy";
        [SerializeField,Range(0,1)] public float alpha=.6f;
        void OnEnable()=>Refresh();
        public void Refresh(){if(theme==null||image==null)return;var c=theme.Color(colorKey);c.a*=alpha;theme.Apply(image,spriteKey,c);image.material=theme.additive;}
    }
}
