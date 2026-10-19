using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FxLineGraphic : MaskableGraphic
    {
        [SerializeField] public Vector2[] points={new Vector2(0,0),new Vector2(200,0)};
        [SerializeField] public bool quadratic,roundCaps=true;
        [SerializeField,Min(2)] public int segments=32;
        [SerializeField,Min(.1f)] public float width=2,softness=.7f;
        [SerializeField] public Vector2 dash=new Vector2(6,4);
        [SerializeField] public float speed=20;
        float revealStart,revealDuration,revealDelay;
        public int MeshBuilds {get;private set;}
        public float Length {get;private set;}
        public void BeginReveal(float start,float duration,float delay){revealStart=start;revealDuration=duration;revealDelay=delay;SetVerticesDirty();}
        public static Vector2 Quadratic(Vector2 a,Vector2 b,Vector2 c,float t)=>a*(1-t)*(1-t)+2*b*t*(1-t)+c*t*t;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            MeshBuilds++;vh.Clear();if(points==null||points.Length<2)return;
            int n=quadratic&&points.Length>=3?Mathf.Max(2,segments):points.Length-1;
            float length=0;Vector2 prev=Point(0,n);for(int i=1;i<=n;i++){var p=Point(i,n);length+=Vector2.Distance(prev,p);prev=p;}Length=length;
            float u=0;prev=Point(0,n);float half=width*.5f,pad=half+softness;
            for(int i=1;i<=n;i++)
            {
                var p=Point(i,n);var dir=(p-prev).normalized;float dist=Vector2.Distance(prev,p);if(dist<.001f)continue;var normal=new Vector2(-dir.y,dir.x)*pad;
                float begin=i==1&&roundCaps?-pad:0,end=i==n&&roundCaps?pad:0;
                var a=prev+dir*begin;var b=p+dir*end;int idx=vh.currentVertCount;
                Add(vh,a-normal,u+begin,-pad,half,length);Add(vh,a+normal,u+begin,pad,half,length);Add(vh,b+normal,u+dist+end,pad,half,length);Add(vh,b-normal,u+dist+end,-pad,half,length);
                vh.AddTriangle(idx,idx+1,idx+2);vh.AddTriangle(idx+2,idx+3,idx);u+=dist;prev=p;
            }
        }
        Vector2 Point(int i,int n)=>quadratic&&points.Length>=3?Quadratic(points[0],points[1],points[2],(float)i/n):points[i];
        void Add(VertexHelper vh,Vector2 p,float u,float side,float half,float total)
        {
            var v=UIVertex.simpleVert;v.position=p;v.color=color;v.uv1=new Vector4(u,side,half,softness);
            v.uv2=new Vector4(dash.x,dash.y,speed,roundCaps?1:0);v.uv3=new Vector4(total,revealStart,revealDuration,revealDelay);vh.AddVert(v);
        }
    }
}
