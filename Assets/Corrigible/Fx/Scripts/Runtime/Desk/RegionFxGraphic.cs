using System;
using Corrigible.Desk;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Sade earcut geometrisini değişmeden çizer. Düzey yalnız ortak shader dizisinde değişir.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RegionFxGraphic : MaskableGraphic
    {
        [SerializeField] public RegionMeshGraphic.Polygon[] polygons=Array.Empty<RegionMeshGraphic.Polygon>();
        [SerializeField] public int regionIndex;
        public int MeshBuilds {get;private set;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            MeshBuilds++;vh.Clear();var rect=rectTransform.rect;var v=UIVertex.simpleVert;v.color=color;
            foreach(var p in polygons){int start=vh.currentVertCount;foreach(var point in p.Vertices){v.position=new Vector3(rect.xMin+point.x,rect.yMax-point.y);v.uv0=point;v.uv1=new Vector4(regionIndex,0,0,0);vh.AddVert(v);}for(int i=0;i<p.Triangles.Length;i+=3)vh.AddTriangle(start+p.Triangles[i],start+p.Triangles[i+1],start+p.Triangles[i+2]);}
        }
    }
}
