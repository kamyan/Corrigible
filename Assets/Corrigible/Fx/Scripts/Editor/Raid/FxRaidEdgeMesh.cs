using System;
using System.Collections.Generic;
using UnityEngine;
namespace Corrigible.Fx.EditorTools
{
    /// <summary>Kaynak üçgenleri konuma göre kaynaklar; sert kenarları ve yumuşak yüzlerin siluet adaylarını dörtgenle saklar.</summary>
    public static class FxRaidEdgeMesh
    {
        struct Edge {public Vector3 a,b,n1,n2;public Color color;public bool paired;}
        public static Mesh Build(Mesh source,Color[] colors)
        {
            var vertices=source.vertices;var edges=new Dictionary<string,Edge>();
            string Key(Vector3 p)=>Mathf.RoundToInt(p.x*100000)+":"+Mathf.RoundToInt(p.y*100000)+":"+Mathf.RoundToInt(p.z*100000);
            for(int s=0;s<source.subMeshCount;s++)
            {
                var t=source.GetTriangles(s);for(int i=0;i<t.Length;i+=3)
                {var a=vertices[t[i]];var b=vertices[t[i+1]];var c=vertices[t[i+2]];var n=Vector3.Cross(b-a,c-a).normalized;
                    void Add(Vector3 x,Vector3 y){string kx=Key(x),ky=Key(y);string key=string.CompareOrdinal(kx,ky)<0?kx+"/"+ky:ky+"/"+kx;if(edges.TryGetValue(key,out var e)){e.n2=n;e.paired=true;edges[key]=e;}else edges.Add(key,new Edge{a=x,b=y,n1=n,n2=n,color=colors[Mathf.Min(s,colors.Length-1)]});}
                    Add(a,b);Add(b,c);Add(c,a);
                }
            }
            var v=new List<Vector3>();var uv=new List<Vector4>();var data=new List<Vector4>();var normals=new List<Vector3>();var tangents=new List<Vector4>();var cs=new List<Color>();var tris=new List<int>();
            foreach(var e in edges.Values)
            {
                float angle=Vector3.Angle(e.n1,e.n2);if(e.paired&&angle<.01f)continue;int start=v.Count;bool hard=!e.paired||angle>30;
                for(int i=0;i<4;i++){bool end=i>=2;v.Add(end?e.b:e.a);var other=end?e.a:e.b;uv.Add(new Vector4(other.x,other.y,other.z,end?1:0));data.Add(new Vector4((i==0||i==3?-1:1)*(end?-1:1),hard?1:0,0,0));normals.Add(e.n1);tangents.Add(new Vector4(e.n2.x,e.n2.y,e.n2.z,1));cs.Add(e.color);}
                tris.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            var mesh=new Mesh{name=source.name+"_FxEdges",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetUVs(1,data);mesh.SetNormals(normals);mesh.SetTangents(tangents);mesh.SetColors(cs);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();return mesh;
        }
    }
}
