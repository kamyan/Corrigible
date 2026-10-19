using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxLedBar : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public FxShaderImage track,fill,danger,glint;
        [SerializeField] public RectTransform pointer,uncertainty;
        [SerializeField] public FxLineGraphic uncertaintyLine;
        [SerializeField] public Image fillGlow,pointerGlow;
        [SerializeField,Range(0,1)] public float value=.62f,dangerFrom=.8f,low=.5f,high=.7f;
        [SerializeField] public string accent="cy";
        void OnEnable()=>Refresh();
        void OnRectTransformDimensionsChange(){if(theme!=null&&track!=null&&fill!=null&&danger!=null&&pointer!=null&&uncertainty!=null&&glint!=null)Refresh();}
        public void Refresh()
        {
            if(theme==null)return;Setup(track,"cy",.14f);Setup(fill,accent,1);Setup(danger,"rd",.55f);
            Range(fill.rectTransform,0,value);Range(danger.rectTransform,dangerFrom,1);pointer.anchorMin=pointer.anchorMax=new Vector2(value,.5f);pointer.pivot=new Vector2(.5f,.5f);pointer.anchoredPosition=Vector2.zero;pointer.sizeDelta=new Vector2(2,15);
            Range(uncertainty,low,high);uncertainty.offsetMin=new Vector2(-2,-2);uncertainty.offsetMax=new Vector2(2,2);
            if(uncertaintyLine!=null){float w=((RectTransform)transform).rect.width*(high-low)+4;uncertaintyLine.points=new[]{new Vector2(0,0),new Vector2(w,0),new Vector2(w,-11),new Vector2(0,-11),new Vector2(0,0)};uncertaintyLine.color=theme.Color("vi");uncertaintyLine.SetVerticesDirty();}
            if(fillGlow!=null){var c=theme.Color(accent);c.a=.35f;theme.Apply(fillGlow,"fx_glow_frame",c);fillGlow.material=theme.additive;fillGlow.rectTransform.anchorMin=new Vector2(0,0);fillGlow.rectTransform.anchorMax=new Vector2(value,1);fillGlow.rectTransform.offsetMin=Vector2.one*-16;fillGlow.rectTransform.offsetMax=Vector2.one*16;}
            if(pointerGlow!=null){var c=theme.Color("tx");c.a=.35f;theme.Apply(pointerGlow,"fx_glow_dot_32",c);pointerGlow.material=theme.additive;pointerGlow.rectTransform.anchorMin=pointerGlow.rectTransform.anchorMax=new Vector2(value,.5f);pointerGlow.rectTransform.pivot=new Vector2(.5f,.5f);pointerGlow.rectTransform.anchoredPosition=Vector2.zero;pointerGlow.rectTransform.sizeDelta=new Vector2(22,22);}
            glint.effect=FxShaderImage.Effect.Glint;glint.period=2.8f;glint.material=theme.additive;glint.color=theme.Color("cy2");glint.SetVerticesDirty();
        }
        void Setup(FxShaderImage image,string key,float alpha){var c=theme.Color(key);c.a=alpha;image.color=c;image.material=theme.additive;image.effect=FxShaderImage.Effect.Led;image.SetVerticesDirty();}
        internal static void Range(RectTransform rect,float min,float max){rect.anchorMin=new Vector2(min,0);rect.anchorMax=new Vector2(max,1);rect.offsetMin=rect.offsetMax=Vector2.zero;}
    }
}
