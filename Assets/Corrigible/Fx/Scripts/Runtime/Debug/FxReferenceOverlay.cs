using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Bir referansı gerektiğinde yükler. Geliştirme katmanıdır; hit testi almaz, tek dokuyu yeniden kullanır.</summary>
    public sealed class FxReferenceOverlay : FxTimedBehaviour
    {
        [SerializeField] public RawImage image;
        [SerializeField] public RectTransform viewport;
        [SerializeField] public string folder="Docs/UI_Fx/SonSalter_Fx_Paketi/reference/screens";
        Texture2D texture;string loaded;int opacityIndex=1;bool cover;
        static readonly float[] Opacities={.25f,.5f,.75f,1};
        public bool Visible {get;private set;}
        public bool Blink {get;private set;}
        public string ScreenName {get;private set;}
        public float Opacity=>Opacities[opacityIndex];
        public int LoadCount {get;private set;}
        public void Select(string name,bool fill)
        {ScreenName=name;cover=fill;if(Visible)Load();RefreshLayout();}
        public void SetVisible(bool value)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Visible=value;if(value)Load();Tick(clock);
#else
            Visible=false;image.enabled=false;
#endif
        }
        public void CycleOpacity(){opacityIndex=(opacityIndex+1)%Opacities.Length;Tick(clock);}
        public void ToggleBlink(){Blink=!Blink;Tick(clock);}
        bool Load()
        {
            if(loaded==ScreenName&&texture!=null)return true;
            var path=Path.Combine(Application.dataPath,"..",folder,ScreenName+".jpg");
            if(!File.Exists(path)){Debug.LogWarning("[FX] Referans bulunamadı: "+ScreenName);Visible=false;return false;}
            if(texture==null)texture=new Texture2D(2,2,TextureFormat.RGB24,false){name="FX Reference"};
            ImageConversion.LoadImage(texture,File.ReadAllBytes(path));texture.wrapMode=TextureWrapMode.Clamp;image.texture=texture;loaded=ScreenName;LoadCount++;return true;
        }
        public void RefreshLayout()
        {
            if(image==null||viewport==null)return;var size=viewport.rect.size;float ratio=cover?Mathf.Max(size.x/1920,size.y/1080):Mathf.Min(size.x/1920,size.y/1080);
            image.rectTransform.sizeDelta=new Vector2(1920,1080)*ratio;image.rectTransform.anchoredPosition=Vector2.zero;
        }
        public override void Tick(FxClock source)
        {
            if(image==null)return;bool on=Visible&&(!Blink||source==null||source.Reduced||Mathf.Repeat((float)source.Now,1)<.5f);if(image.enabled!=on)image.enabled=on;
            var tint=new Color(1,1,1,Opacity);if(image.color!=tint)image.color=tint;RefreshLayout();
        }
        void OnDestroy(){if(texture!=null)Destroy(texture);}
    }
}
