using Corrigible.Game.Raid;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Kare işaretleri, uyanma halkaları ve ok uçları tek durağan mesh'tedir. Ölçüler FX paletinden pişirilir.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FxRaidMarks : MaskableGraphic
    {
        public FxRaidBoard board;
        public int MeshBuilds {get;private set;}
        float N(string style,string key,float fallback)=>board.Number("raid.marks."+style+"."+key,fallback);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();MeshBuilds++;if(board==null||board.Snapshot==null)return;var m=board.Snapshot.Marks;
            var shadow=board.Ink("raid.shadow");foreach(var unit in board.Snapshot.State.Units)Disc(vh,RaidBoard3D.W(unit.X,unit.Y),.34f,shadow,new Vector2(1.5f,-2));
            foreach(var k in m.Moves)Tile(vh,k,"move",board.Ink("cy"),true);
            foreach(var k in m.Targets)Tile(vh,k,"abilityTarget",board.Ink("mt"));
            foreach(var k in m.Danger)Tile(vh,k.Key,k.Value?"hitCertain":"hitMaybeLie",board.Ink("rd"),true,!k.Value,!k.Value);
            foreach(var k in m.Heal)Tile(vh,k,"repair",board.Ink("rd"),false,true);
            foreach(var k in m.Spawns)Wake(vh,k);
            foreach(var u in board.Snapshot.State.Units)if(u.Id==m.Selected){int k=u.Y*8+u.X;Tile(vh,k,"selected",board.Ink("cy"));Frame(vh,Corners(k,.37f),board.Ink("cy"),N("selected","innerPx",1.2f),false);}
            foreach(var intent in board.DisplayIntents)
            {
                if(!intent.HasTarget||intent.TargetNone)continue;var a=board.Project(RaidBoard3D.W(intent.FromX,intent.FromY)+Vector3.up*.08f);var b=board.Project(RaidBoard3D.W(intent.TargetX,intent.TargetY)+Vector3.up*.08f);
                Vector2 d=(b-a).normalized;if(Vector2.Distance(a,b)<2)continue;var c=board.Ink(intent.Uncertain?"am":"rd");c.a=board.IntentAlpha(intent.Unit);
                Line(vh,a+d*17,b-d*10,c,3.5f);Quad(vh,new[]{b,b-d*12+new Vector2(-d.y,d.x)*6,b-d*12-new Vector2(-d.y,d.x)*6},c);
            }
            var preview=board.AbilityPreview;if(preview!=null)foreach(var fx in preview.Fx)if(fx.K=="push"){
                bool blocked=!preview.Moves.Any(x=>x.Unit==fx.Unit);var color=board.Ink(blocked?"rd":"mt");var a=board.Project(RaidBoard3D.W(fx.X,fx.Y)+Vector3.up*.08f);var b=board.Project(RaidBoard3D.W(fx.X+fx.Dx,fx.Y+fx.Dy)+Vector3.up*.08f);var dir=(b-a).normalized;Line(vh,a,b,color,3);Quad(vh,new[]{b,b-dir*15+new Vector2(-dir.y,dir.x)*7,b-dir*15-new Vector2(-dir.y,dir.x)*7},color);if(blocked){Line(vh,b-new Vector2(7,7),b+new Vector2(7,7),color,3);Line(vh,b+new Vector2(-7,7),b+new Vector2(7,-7),color,3);}else if(fx.X+fx.Dx>=0&&fx.X+fx.Dx<8&&fx.Y+fx.Dy>=0&&fx.Y+fx.Dy<8)Frame(vh,Corners((fx.Y+fx.Dy)*8+fx.X+fx.Dx,.4f),color,2,true);
            }
        }
        Vector2[] Corners(int k,float r=.43f){var c=RaidBoard3D.W(k%8,k/8)+Vector3.up*.075f;return new[]{board.Project(c+new Vector3(-r,0,-r)),board.Project(c+new Vector3(r,0,-r)),board.Project(c+new Vector3(r,0,r)),board.Project(c+new Vector3(-r,0,r))};}
        void Tile(VertexHelper vh,int k,string style,Color color,bool hatch=false,bool dashed=false,bool amber=false)
        {
            float inset=N(style,"insetUnits",.07f);var p=Corners(k,.5f-inset);var fill=color;fill.a=N(style,"fillAlpha",.12f);Quad(vh,p,fill);
            var border=amber?board.Ink("am"):color;border.a=N(style,"edgeAlpha",N(style,"alpha",.85f));Frame(vh,p,border,N(style,"edgePx",N(style,"outerPx",2.4f)),dashed,style=="repair"?2:7,style=="repair"?4:4.5f);
            if(hatch){color.a=N(style,"hatchAlpha",.34f);float pitch=N(style,"hatch.spacingUnits",.11f),width=N(style,"hatch.widthPx",1.6f);int count=Mathf.Max(2,Mathf.FloorToInt((1-2*inset)/pitch));for(int i=1;i<count;i++){float t=(float)i/count;if(style=="move")Line(vh,Vector2.Lerp(p[0],p[3],t),Vector2.Lerp(p[1],p[2],t),color,width);else Line(vh,Vector2.Lerp(p[0],p[1],t),Vector2.Lerp(p[3],p[2],t),color,width);}}
        }
        void Wake(VertexHelper vh,int key)
        {
            var world=RaidBoard3D.W(key%8,key/8)+Vector3.up*.075f;var color=board.Ink("rd");var fill=color;fill.a=N("wake","fillAlpha",.12f);float radius=N("wake","ringRadiusUnits",.33f);Disc(vh,world,radius,fill,Vector2.zero);
            var ring=Circle(world,radius);Frame(vh,ring,color,N("wake","ringPx",2.4f),false);var inner=Circle(world,N("wake","innerDashedRadius",.17f));for(int i=0;i<inner.Length;i+=2)Line(vh,inner[i],inner[(i+1)%inner.Length],color,1);
            int rays=Mathf.RoundToInt(N("wake","rays.count",8));for(int i=0;i<rays;i++){float angle=i*Mathf.PI*2/rays;var d=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));Line(vh,board.Project(world+d*N("wake","rays.fromUnits",.38f)),board.Project(world+d*N("wake","rays.toUnits",.47f)),color,N("wake","rays.px",2.2f));}
            var center=board.Project(world);float height=N("wake","pin.heightPx",17);Line(vh,center,center+Vector2.up*height,color,1.4f);var head=new Vector2[16];for(int i=0;i<head.Length;i++){float a=i*Mathf.PI*2/head.Length;head[i]=center+Vector2.up*height+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*3.5f;}Quad(vh,head,color);
        }
        Vector2[] Circle(Vector3 world,float radius){var p=new Vector2[48];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;p[i]=board.Project(world+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius);}return p;}
        void Disc(VertexHelper vh,Vector3 world,float radius,Color color,Vector2 offset){var p=Circle(world,radius);for(int i=0;i<p.Length;i++)p[i]+=offset;Quad(vh,p,color);}
        static void Frame(VertexHelper vh,Vector2[] p,Color c,float w,bool dash,float on=7,float off=5){for(int i=0;i<p.Length;i++){var a=p[i];var b=p[(i+1)%p.Length];if(!dash)Line(vh,a,b,c,w);else{float len=Vector2.Distance(a,b);for(float d=0;d<len;d+=on+off)Line(vh,Vector2.Lerp(a,b,d/len),Vector2.Lerp(a,b,Mathf.Min(len,d+on)/len),c,w);}}}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,Color c,float w){var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*w*.5f;Quad(vh,new[]{a-n,a+n,b+n,b-n},c);}
        static void Quad(VertexHelper vh,Vector2[] p,Color c){int start=vh.currentVertCount;foreach(var pt in p){var v=UIVertex.simpleVert;v.position=pt;v.color=c;v.uv0=new Vector4(.5f,.5f);vh.AddVert(v);}for(int i=1;i<p.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);}
    }
}
