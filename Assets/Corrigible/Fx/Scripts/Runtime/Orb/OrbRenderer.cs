using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Corrigible.Fx.Orb;
using UnityEngine;
using UnityEngine.Rendering;
namespace Corrigible.Fx
{
    /// <summary>Tek GPU ilkel tamponu ve komut tamponuyla gama uzayındaki eklemeli küre tuvalini çizer.</summary>
    public sealed class OrbRenderer : MonoBehaviour
    {
        [SerializeField] public OrbRenderSettings settings;
        [StructLayout(LayoutKind.Sequential)] struct Primitive {public Vector4 d0,d1,d2,d3,d4,d5,d6;}
        Primitive[] primitives;
        ComputeBuffer buffer;
        CommandBuffer commands;
        MaterialPropertyBlock properties;
        static readonly int SizeId=Shader.PropertyToID("_OrbSize"),BufferId=Shader.PropertyToID("_OrbPrimitives");
        public RenderTexture Target {get;private set;}
        public int PrimitiveCount {get;private set;}
        public long TextureBytes=>Target!=null?(long)Target.width*Target.height*4:0;
        public double CssSize {get;private set;}
        public void Ensure(double cssSize,int pixels)
        {
            if(settings==null)throw new InvalidOperationException("Küre çizim ayarları bağlanmadı.");
            CssSize=cssSize;if(buffer==null){primitives=new Primitive[OrbDrawList.Capacity];buffer=new ComputeBuffer(primitives.Length,112,ComputeBufferType.Structured);commands=new CommandBuffer{name="FX Orb Canvas"};properties=new MaterialPropertyBlock();properties.SetBuffer(BufferId,buffer);}
            if(Target!=null&&Target.width==pixels)return;
            ReleaseTarget();Target=new RenderTexture(pixels,pixels,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){name="FX Orb Canvas",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,useMipMap=false,autoGenerateMips=false};Target.Create();
        }
        public void Render(OrbSimulation simulation,int pixels)
        {
            Ensure(simulation.Size,pixels);var list=simulation.DrawList;PrimitiveCount=list.Count;float scale=(float)(pixels/CssSize);
            for(int i=0;i<list.Count;i++)
            {
                ref var p=ref list.Items[i];ref var v=ref primitives[i];
                v.d0=new Vector4((float)p.A.X,(float)p.A.Y,(float)p.B.X,(float)p.B.Y);
                v.d1=new Vector4((float)p.C.X,(float)p.C.Y,(float)p.Radius,(float)p.Width);
                v.d2=new Vector4((float)p.Shape,(float)p.Stop,(float)p.GlobalAlpha,p.Gradient?1:p.Round?2:0);
                v.d3=Color(p.Color0);v.d4=Color(p.Color1);v.d5=Color(p.Color2);v.d6=new Vector4(scale,0,0,0);
            }
            buffer.SetData(primitives,0,0,list.Count);properties.SetFloat(SizeId,(float)CssSize);
            commands.Clear();commands.SetRenderTarget(Target);commands.ClearRenderTarget(false,true,UnityEngine.Color.clear);commands.SetViewport(new Rect(0,0,pixels,pixels));commands.DrawProcedural(Matrix4x4.identity,settings.draw,0,MeshTopology.Triangles,list.Count*6,1,properties);Graphics.ExecuteCommandBuffer(commands);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Color(OrbColor c)=>new Vector4((float)c.R/255,(float)c.G/255,(float)c.B/255,(float)c.A);
        void ReleaseTarget(){if(Target==null)return;Target.Release();DestroyResource(Target);Target=null;}
        static void DestroyResource(UnityEngine.Object item){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        void OnDestroy(){ReleaseTarget();buffer?.Release();commands?.Release();}
    }
}
