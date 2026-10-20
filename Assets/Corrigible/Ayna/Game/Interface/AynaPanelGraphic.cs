using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Ayna.Game
{
    // Cam panel, kırpılmış köşeler ve krom çizgileri yalnız sunumdur.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AynaPanelGraphic : MaskableGraphic
    {
        [SerializeField] Color edge = new Color(.36f,.89f,1,.35f);
        [SerializeField] float cut = 8;
        public void Style(Color fill, Color border) { color=fill;edge=border;SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float c=Mathf.Min(cut,Mathf.Min(r.width,r.height)*.2f);
            var p=new[]{new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMin,r.yMax),new Vector2(r.xMin,r.yMin+c)};
            var mid=r.center;for(int i=0;i<p.Length;i++)Tri(vh,mid,p[i],p[(i+1)%p.Length],color);
            for(int i=0;i<p.Length;i++)Line(vh,p[i],p[(i+1)%p.Length],1,edge);
            var light=edge;light.a=Mathf.Min(1,edge.a*2);Line(vh,new Vector2(r.xMin,r.yMax-10),new Vector2(r.xMin,r.yMax),2,light);Line(vh,new Vector2(r.xMin,r.yMax),new Vector2(r.xMin+18,r.yMax),2,light);
            Line(vh,new Vector2(r.xMax-18,r.yMin),new Vector2(r.xMax,r.yMin),2,light);
        }
        static void Tri(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color col){int n=vh.currentVertCount;vh.AddVert(a,col,Vector2.zero);vh.AddVert(b,col,Vector2.zero);vh.AddVert(c,col,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float w,Color col){var v=(b-a).normalized;var q=new Vector2(-v.y,v.x)*w*.5f;int n=vh.currentVertCount;vh.AddVert(a-q,col,Vector2.zero);vh.AddVert(a+q,col,Vector2.zero);vh.AddVert(b+q,col,Vector2.zero);vh.AddVert(b-q,col,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
