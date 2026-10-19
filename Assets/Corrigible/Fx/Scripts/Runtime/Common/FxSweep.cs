using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>RectMask2D içinde sabit şerit; geometriyi oynatmadan tek shader saatiyle akar.</summary>
    public sealed class FxSweep : MonoBehaviour
    {
        [SerializeField] public FxShaderImage strip;
        [SerializeField] public FxTheme theme;
        void OnEnable(){if(strip==null||theme==null)return;strip.sprite=theme.Sprite("fx_sweep_128");strip.effect=FxShaderImage.Effect.Sweep;strip.period=theme.Number("panels.sweep.periodSec",5.2f);strip.material=theme.additive;strip.color=theme.Color("cy2");strip.SetVerticesDirty();}
    }
}
