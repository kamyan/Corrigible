using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FxHistoryFillGraphic : MaskableGraphic
    {
        [SerializeField] public Sprite sprite;
        [SerializeField] public Vector2[] points=System.Array.Empty<Vector2>();
        public override Texture mainTexture=>sprite!=null?sprite.texture:base.mainTexture;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(points.Length<2)return;var uv=Vector2.one*.5f;if(sprite!=null){var outer=UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);uv=new Vector2((outer.x+outer.z)*.5f,(outer.y+outer.w)*.5f);}var top=color;var bottom=color;bottom.a=0;
            for(int i=1;i<points.Length;i++){int n=vh.currentVertCount;vh.AddVert(new Vector3(points[i-1].x,rectTransform.rect.yMin),bottom,uv);vh.AddVert(points[i-1],top,uv);vh.AddVert(points[i],top,uv);vh.AddVert(new Vector3(points[i].x,rectTransform.rect.yMin),bottom,uv);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        }
    }
}
