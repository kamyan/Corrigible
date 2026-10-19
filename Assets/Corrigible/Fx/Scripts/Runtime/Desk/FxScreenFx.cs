using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxScreenFx : FxTimedBehaviour
    {
        [SerializeField] public RawImage beam;
        [SerializeField] public RectTransform viewport;
        public override void Tick(FxClock source)
        {
            bool visible=!source.Static;if(beam.enabled!=visible)beam.enabled=visible;if(!visible)return;
            float x=Mathf.Lerp(-200,viewport.rect.width+180,source.Phase(12,0,FxPhase.Sweep));beam.rectTransform.anchoredPosition=new Vector2(x,0);
        }
    }
}
