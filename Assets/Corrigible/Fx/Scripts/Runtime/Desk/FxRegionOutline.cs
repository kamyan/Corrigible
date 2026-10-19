using System;
using Corrigible.Desk;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Canlı bölge sınırlarını hazır çokgen halkalarından tek mesh olarak çizer.</summary>
    public sealed class FxRegionOutline : MaskableGraphic
    {
        [SerializeField] public RegionMeshGraphic.Polygon[] polygons=Array.Empty<RegionMeshGraphic.Polygon>();
        [SerializeField] public float thickness=1.2f;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=rectTransform.rect;
            foreach(var polygon in polygons)for(int ring=0;ring<polygon.RingOffsets.Length-1;ring++)
            {
                int start=polygon.RingOffsets[ring],end=polygon.RingOffsets[ring+1];
                for(int n=start;n<end;n++)
                {
                    var a=polygon.Vertices[n];var b=polygon.Vertices[n+1<end?n+1:start];
                    a=new Vector2(rect.xMin+a.x,rect.yMax-a.y);b=new Vector2(rect.xMin+b.x,rect.yMax-b.y);
                    var delta=b-a;if(delta.sqrMagnitude<.00001f)continue;var normal=new Vector2(-delta.y,delta.x).normalized*thickness*.5f;
                    int first=vh.currentVertCount;var v=UIVertex.simpleVert;v.color=color;
                    v.position=a-normal;vh.AddVert(v);v.position=a+normal;vh.AddVert(v);v.position=b+normal;vh.AddVert(v);v.position=b-normal;vh.AddVert(v);
                    vh.AddTriangle(first,first+1,first+2);vh.AddTriangle(first,first+2,first+3);
                }
            }
        }
    }
}
