using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Corrigible.Fx
{
    /// <summary>Hazır parça kenarlarını iki çizime toplar. Geometri yalnız komutla değişince yeniden yüklenir.</summary>
    [ExecuteAlways]
    public sealed class FxRaidEdges : MonoBehaviour
    {
        [SerializeField] public MeshFilter[] sources;
        [SerializeField] public MeshRenderer[] originalRenderers;
        [SerializeField] public MeshFilter edgeTarget,glowTarget;
        sealed class Shape {public Vector3[] vertices,normals;public Vector4[] endpoints,data,tangents;public Color[] colors;public int[] triangles;}
        Shape[] shapes;Matrix4x4[] matrices;bool[] visible;Mesh combined;
        readonly List<Vector3> vertices=new List<Vector3>(8192),normals=new List<Vector3>(8192);
        readonly List<Vector4> endpoints=new List<Vector4>(8192),data=new List<Vector4>(8192),tangents=new List<Vector4>(8192);
        readonly List<Color> colors=new List<Color>(8192);readonly List<int> triangles=new List<int>(12288);
        public int Rebuilds {get;private set;}
        public Mesh Combined=>combined;
        void OnEnable(){foreach(var r in originalRenderers)r.enabled=false;edgeTarget.gameObject.SetActive(true);glowTarget.gameObject.SetActive(true);Refresh();}
        void OnDisable(){foreach(var r in originalRenderers)if(r!=null)r.enabled=true;if(edgeTarget!=null)edgeTarget.gameObject.SetActive(false);if(glowTarget!=null)glowTarget.gameObject.SetActive(false);}
        void Prepare()
        {
            shapes=new Shape[sources.Length];matrices=new Matrix4x4[sources.Length];visible=new bool[sources.Length];var cache=new Dictionary<Mesh,Shape>();
            for(int i=0;i<sources.Length;i++){var mesh=sources[i].sharedMesh;if(!cache.TryGetValue(mesh,out var shape)){var uv=new List<Vector4>();mesh.GetUVs(0,uv);var d=new List<Vector4>();mesh.GetUVs(1,d);shape=new Shape{vertices=mesh.vertices,normals=mesh.normals,endpoints=uv.ToArray(),data=d.ToArray(),tangents=mesh.tangents,colors=mesh.colors,triangles=mesh.triangles};cache.Add(mesh,shape);}shapes[i]=shape;}
            combined=new Mesh{name="FX Raid Combined Edges",hideFlags=HideFlags.DontSave,indexFormat=IndexFormat.UInt32};combined.MarkDynamic();edgeTarget.sharedMesh=glowTarget.sharedMesh=combined;
        }
        public void Refresh()
        {
            bool changed=shapes==null;if(changed)Prepare();var inverse=edgeTarget.transform.worldToLocalMatrix;
            for(int i=0;i<sources.Length;i++){bool active=sources[i].gameObject.activeInHierarchy;var matrix=inverse*sources[i].transform.localToWorldMatrix;changed|=active!=visible[i]||active&&matrix!=matrices[i];visible[i]=active;matrices[i]=matrix;}
            if(!changed)return;vertices.Clear();normals.Clear();endpoints.Clear();data.Clear();tangents.Clear();colors.Clear();triangles.Clear();
            for(int i=0;i<sources.Length;i++)if(visible[i]){
                var shape=shapes[i];var matrix=matrices[i];var normalMatrix=matrix.inverse.transpose;int offset=vertices.Count;
                for(int n=0;n<shape.vertices.Length;n++){vertices.Add(matrix.MultiplyPoint3x4(shape.vertices[n]));normals.Add(normalMatrix.MultiplyVector(shape.normals[n]).normalized);var uv=shape.endpoints[n];var point=matrix.MultiplyPoint3x4(new Vector3(uv.x,uv.y,uv.z));endpoints.Add(new Vector4(point.x,point.y,point.z,uv.w));data.Add(shape.data[n]);var tangent=shape.tangents[n];var normal=normalMatrix.MultiplyVector(new Vector3(tangent.x,tangent.y,tangent.z)).normalized;tangents.Add(new Vector4(normal.x,normal.y,normal.z,tangent.w));colors.Add(shape.colors[n]);}
                foreach(int index in shape.triangles)triangles.Add(offset+index);
            }
            combined.Clear();combined.SetVertices(vertices);combined.SetNormals(normals);combined.SetUVs(0,endpoints);combined.SetUVs(1,data);combined.SetTangents(tangents);combined.SetColors(colors);combined.SetTriangles(triangles,0);combined.RecalculateBounds();Rebuilds++;
        }
        void OnDestroy(){if(combined!=null){if(Application.isPlaying)Destroy(combined);else DestroyImmediate(combined);}}
    }
}
