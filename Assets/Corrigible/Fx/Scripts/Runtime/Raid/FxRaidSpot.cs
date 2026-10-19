using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Sonucun %86–100 yarıçap aralığında yumuşayan spotu; delik de girdiyi engeller.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FxRaidSpot : MaskableGraphic
    {
        Vector2 center;float radius=150;bool hole=true;
        public void SetSpot(Vector2 point,float size,float unused,Color unusedColor,bool showHole=true)
        {if(center==point&&radius==size&&hole==showHole)return;center=point;radius=size;hole=showHole;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var clear=color;clear.a=0;
            if(!hole){Quad(vh,r.min,new Vector2(r.xMin,r.yMax),r.max,new Vector2(r.xMax,r.yMin),color,color);return;}
            var lo=center-Vector2.one*radius;var hi=center+Vector2.one*radius;
            Quad(vh,r.min,new Vector2(r.xMin,r.yMax),new Vector2(lo.x,r.yMax),new Vector2(lo.x,r.yMin),color,color);
            Quad(vh,new Vector2(hi.x,r.yMin),new Vector2(hi.x,r.yMax),r.max,new Vector2(r.xMax,r.yMin),color,color);
            Quad(vh,new Vector2(lo.x,r.yMin),new Vector2(lo.x,lo.y),new Vector2(hi.x,lo.y),new Vector2(hi.x,r.yMin),color,color);
            Quad(vh,new Vector2(lo.x,hi.y),new Vector2(lo.x,r.yMax),new Vector2(hi.x,r.yMax),hi,color,color);
            for(int i=0;i<128;i++){
                float a=i*Mathf.PI*2/128,b=(i+1)*Mathf.PI*2/128;var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                Quad(vh,center+u*radius*.86f,center+v*radius*.86f,center+v*radius,center+u*radius,clear,color);
                Quad(vh,center+u*radius,center+v*radius,center+v*(radius/Mathf.Max(Mathf.Abs(v.x),Mathf.Abs(v.y))),center+u*(radius/Mathf.Max(Mathf.Abs(u.x),Mathf.Abs(u.y))),color,color);
            }
        }
        static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color inner,Color outer)
        {int i=vh.currentVertCount;vh.AddVert(a,inner,Vector2.zero);vh.AddVert(b,inner,Vector2.zero);vh.AddVert(c,outer,Vector2.zero);vh.AddVert(d,outer,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i+2,i+3,i);}
    }
}
