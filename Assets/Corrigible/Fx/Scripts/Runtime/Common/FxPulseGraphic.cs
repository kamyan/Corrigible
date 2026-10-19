using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Nabız bilgisi vertex'te kalır; ortak shader saati alfa üretir, boşta UI mesh'i değişmez.</summary>
    public sealed class FxPulseGraphic : BaseMeshEffect
    {
        [SerializeField] public float period=1.2f,minimumAlpha=.72f;
        [SerializeField] bool pulse;
        public bool Pulse=>pulse;
        public void SetPulse(bool value){if(pulse==value)return;pulse=value;if(graphic!=null)graphic.SetVerticesDirty();}
        public override void ModifyMesh(VertexHelper vh)
        {
            if(!IsActive())return;var vertex=UIVertex.simpleVert;for(int i=0;i<vh.currentVertCount;i++){vh.PopulateUIVertex(ref vertex,i);vertex.uv1=new Vector4(pulse?1/Mathf.Max(.01f,period):0,minimumAlpha,31,0);vh.SetUIVertex(vertex,i);}
        }
    }
}
