using Corrigible.Core;
using Corrigible.Fx.Orb;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxGuideAiStatesPage : FxTimedBehaviour
    {
        [SerializeField] public OrbClock orbClock;
        [SerializeField] public OrbView live;
        [SerializeField] public RectTransform marker;
        public override void Tick(FxClock source)
        {
            double level=OrbMath.Sweep(orbClock.Elapsed);live.autoSweep=true;
            var point=new Vector2((float)(level/100)*((RectTransform)marker.parent).rect.width,0);if(marker.anchoredPosition!=point)marker.anchoredPosition=point;
        }
    }
}
