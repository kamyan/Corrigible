using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Kayan dokuların UV hareketi sabit mesh ve ortak saatle shader'da hesaplanır.</summary>
    public sealed class FxUvGraphic : RawImage
    {
        public float period=1.6f,phase;
        public Vector2 range=new Vector2(0,1),direction=Vector2.right;
        public FxWave wave=FxWave.Linear;
        public FxPhase phaseKind=FxPhase.Redact;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);var v=new UIVertex();for(int i=0;i<vh.currentVertCount;i++){vh.PopulateUIVertex(ref v,i);v.uv1=new Vector4(5,period,range.x,range.y);v.uv2=new Vector4(direction.x,direction.y,(float)phaseKind,(float)wave);v.uv3=new Vector4(phase,0,0,0);vh.SetUIVertex(v,i);}
        }
    }
}
