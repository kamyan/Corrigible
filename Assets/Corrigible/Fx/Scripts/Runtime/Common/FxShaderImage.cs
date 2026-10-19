using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Tarama ve parıltı parametreleri sabit vertex kanallarında; akış shader saatinden gelir.</summary>
    public sealed class FxShaderImage : Image
    {
        public enum Effect { None, Sweep, Led, Glint, Hazard }
        [SerializeField] public Effect effect;
        [SerializeField] public float period=5.2f,tileWidth=6,tileHeight=2;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);var v=new UIVertex(); var r=rectTransform.rect;
            for(int i=0;i<vh.currentVertCount;i++) {vh.PopulateUIVertex(ref v,i);v.uv1=new Vector4((float)effect,period,r.width,r.height);v.uv3=sprite!=null?UnityEngine.Sprites.DataUtility.GetOuterUV(sprite):new Vector4(0,0,1,1);v.uv2=new Vector4(tileWidth,tileHeight,(v.position.x-r.xMin)/Mathf.Max(1,r.width),(v.position.y-r.yMin)/Mathf.Max(1,r.height));vh.SetUIVertex(v,i);}
        }
    }
}
