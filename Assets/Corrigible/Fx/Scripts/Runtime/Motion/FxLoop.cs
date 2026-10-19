using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public enum FxProperty { Alpha, Scale, Rotation, UV, Position }
    public enum FxWave { Sine, Linear, Step }
    public sealed class FxLoop : FxTimedBehaviour
    {
        [SerializeField] public FxProperty property;
        [SerializeField] public FxWave wave;
        [SerializeField] public FxPhase phaseKind;
        [SerializeField] public float period=1.6f, phase;
        [SerializeField] public Vector2 range=new Vector2(.55f,1), direction=Vector2.right;
        [SerializeField] public RectTransform target;
        [SerializeField] public Graphic graphic;
        [SerializeField] public FxUvGraphic uvTarget;
        Vector3 scale;Vector2 position;Quaternion rotation;float last=float.NaN;
        protected override void OnEnable() { base.OnEnable(); if(target!=null) {scale=target.localScale;position=target.anchoredPosition;rotation=target.localRotation;} if(uvTarget!=null && property==FxProperty.UV){uvTarget.period=period;uvTarget.phase=phase;uvTarget.range=range;uvTarget.direction=direction;uvTarget.wave=wave;uvTarget.phaseKind=phaseKind;uvTarget.SetVerticesDirty();} last=float.NaN; }
        protected override void OnDisable() { base.OnDisable(); if(target!=null){target.localScale=scale;target.anchoredPosition=position;target.localRotation=rotation;} if(graphic!=null)graphic.canvasRenderer.SetAlpha(1); }
        public static float Sample(FxWave wave,float phase) => wave==FxWave.Linear?phase:wave==FxWave.Step?(phase<.5f?1:0):.5f-.5f*Mathf.Cos(phase*Mathf.PI*2);
        public override void Tick(FxClock source)
        {
            if(property==FxProperty.UV)return;
            float p=source.Phase(period,phase,phaseKind),v=Mathf.Lerp(range.x,range.y,Sample(wave,p));
            if(v==last)return;last=v;
            switch(property) {
                case FxProperty.Alpha: if(graphic!=null)graphic.canvasRenderer.SetAlpha(v); break;
                case FxProperty.Scale: if(target!=null)target.localScale=scale*v;break;
                case FxProperty.Position:if(target!=null)target.anchoredPosition=position+direction*v;break;
                case FxProperty.Rotation:if(target!=null)target.localRotation=rotation*Quaternion.Euler(0,0,v);break;
                
            }
        }
    }
}
