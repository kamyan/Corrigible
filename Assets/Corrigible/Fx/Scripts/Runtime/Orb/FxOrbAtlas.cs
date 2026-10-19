using System;
using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>Panel kürelerinin gerçek zamanlı RT'lerini tek dokuda toplar; model ve çizim sırasını değiştirmez.</summary>
    public sealed class FxOrbAtlas : MonoBehaviour
    {
        [SerializeField] public OrbView[] views=Array.Empty<OrbView>();
        [SerializeField,Min(128)] public int tileSize=128;
        public RenderTexture Target{get;private set;}
        public long TextureBytes=>Target!=null?(long)Target.width*Target.height*4:0;
        int columns;
        void OnEnable()=>Ensure();
        void Ensure()
        {
            if(Target!=null||views.Length==0)return;columns=Mathf.CeilToInt(Mathf.Sqrt(views.Length));int rows=Mathf.CeilToInt((float)views.Length/columns);
            Target=new RenderTexture(columns*tileSize,rows*tileSize,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){name="FX Panel Orb Atlas",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,useMipMap=false,autoGenerateMips=false};Target.Create();var previous=RenderTexture.active;RenderTexture.active=Target;GL.Clear(false,true,Color.clear);RenderTexture.active=previous;
        }
        public void Publish(OrbView view,RenderTexture source)
        {
            Ensure();int index=Array.IndexOf(views,view);if(index<0||Target==null||source==null)return;
            if(source.width>tileSize||source.height>tileSize)throw new InvalidOperationException("Panel küresi atlas hücresine sığmıyor.");
            int x=index%columns*tileSize,y=index/columns*tileSize;Graphics.CopyTexture(source,0,0,0,0,source.width,source.height,Target,0,0,x,y);
            if(view.image.texture!=Target)view.image.texture=Target;var uv=new Rect((float)x/Target.width,(float)y/Target.height,(float)source.width/Target.width,(float)source.height/Target.height);if(view.image.uvRect!=uv)view.image.uvRect=uv;
        }
        void OnDisable(){if(Target==null)return;Target.Release();if(Application.isPlaying)Destroy(Target);else DestroyImmediate(Target);Target=null;}
    }
}
