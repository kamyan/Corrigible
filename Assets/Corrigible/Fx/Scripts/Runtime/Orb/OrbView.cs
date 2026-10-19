using System;
using Corrigible.Fx.Orb;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    [RequireComponent(typeof(RawImage),typeof(OrbRenderer))]
    public sealed class OrbView : MonoBehaviour
    {
        [SerializeField] public RawImage image;
        [SerializeField] public OrbRenderer orbRenderer;
        [SerializeField] public OrbClock clock;
        [SerializeField] public FxOrbAtlas atlas;
        [SerializeField] public OrbKind kind;
        [SerializeField] public double radius=48,level=50,filamentScale=1;
        [SerializeField] public int seed=3;
        [SerializeField] public float startDelay;
        [SerializeField] public bool autoSweep,laboratory;
        readonly Vector3[] corners=new Vector3[4];
        double lastTime=-1,appearedAt;
        bool wasVisible;
        public bool? VisibilityOverride {get;set;}
        public OrbSimulation Simulation {get;private set;}
        public double DisplayedLevel=>Simulation!=null?Simulation.DisplayedLevel:level;
        public event Action<double> DisplayedLevelChanged;
        public int FramesRendered {get;private set;}
        void OnEnable(){appearedAt=clock!=null&&clock.clock!=null?clock.clock.Now:0;ResetModel();if(startDelay>0&&image!=null)image.canvasRenderer.SetAlpha(0);if(clock!=null)clock.Register(this);}
        void OnDisable(){if(clock!=null)clock.Unregister(this);wasVisible=false;lastTime=-1;}
        public void ResetModel()
        {
            if(image==null||orbRenderer==null||orbRenderer.settings==null)return;
            Simulation=new OrbSimulation(radius,seed,kind,level,image.rectTransform.rect.width){FilamentScale=filamentScale,AutoSweep=autoSweep};lastTime=-1;wasVisible=false;
            image.raycastTarget=false;image.material=orbRenderer.settings.composite;
        }
        public bool IsVisible()
        {
            if(VisibilityOverride.HasValue)return VisibilityOverride.Value;
            if(image==null||!image.isActiveAndEnabled||image.canvasRenderer.cull||image.color.a<=0||image.canvasRenderer.GetInheritedAlpha()<=.001f||image.canvas==null)return false;
            var canvas=image.canvas.rootCanvas;var cam=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;image.rectTransform.GetWorldCorners(corners);
            float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
            for(int i=0;i<4;i++){var p=RectTransformUtility.WorldToScreenPoint(cam,corners[i]);minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
            var screen=canvas.pixelRect;return maxX>screen.xMin&&minX<screen.xMax&&maxY>screen.yMin&&minY<screen.yMax;
        }
        int Pixels()=>Mathf.Clamp(Mathf.CeilToInt(image.rectTransform.rect.width*(image.canvas!=null?image.canvas.scaleFactor:1)*orbRenderer.settings.supersample),16,laboratory?orbRenderer.settings.labMaxSize:orbRenderer.settings.maxSize);
        void Draw(){orbRenderer.Render(Simulation,Pixels());if(atlas!=null)atlas.Publish(this,orbRenderer.Target);else if(image.texture!=orbRenderer.Target)image.texture=orbRenderer.Target;FramesRendered++;}
        public bool RenderTick(double now,double sweepTime,bool reduced)
        {
            if(!reduced&&now-appearedAt<startDelay){lastTime=now;return false;}
            if(startDelay>0&&image!=null)image.canvasRenderer.SetAlpha(1);
            if(!IsVisible()){lastTime=now;wasVisible=false;return false;}
            if(Simulation==null)ResetModel();if(Simulation==null)return false;
            Simulation.Level=level;Simulation.AutoSweep=autoSweep;Simulation.SweepTime=sweepTime;
            if(reduced)Simulation.CompleteIntro();double elapsed=lastTime<0||!wasVisible?1.0/orbRenderer.settings.maxFps:Math.Max(0,now-lastTime);lastTime=now;wasVisible=true;
            double old=Simulation.DisplayedLevel;Simulation.AdvanceRealtime(elapsed);Draw();if(old!=Simulation.DisplayedLevel)DisplayedLevelChanged?.Invoke(Simulation.DisplayedLevel);return true;
        }
        public void Hold(double now,bool reduced)
        {
            if(Simulation==null)ResetModel();if(Simulation==null)return;
            if(startDelay>0&&image!=null)image.canvasRenderer.SetAlpha(1);
            if(reduced)Simulation.CompleteIntro();
            if(image.texture==null&&IsVisible()){Simulation.CompleteIntro();Simulation.Advance(.025);Draw();}
            lastTime=now;
        }
        public void RenderFixed(double dt,int steps,bool sweep=false)
        {
            ResetModel();if(image!=null)image.canvasRenderer.SetAlpha(1);Simulation.AutoSweep=sweep;Simulation.SweepTime=double.NaN;Simulation.Advance(dt,steps);Draw();lastTime=clock!=null&&clock.clock!=null?clock.clock.Now:0;wasVisible=true;
        }
    }
}
