using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>F1 saatine kayıtlı tek küre yürütücüsü. Görünmeyen modelin zamanı ilerlemez.</summary>
    public sealed class OrbClock : FxTimedBehaviour
    {
        [SerializeField] public OrbRenderSettings settings;
        [SerializeField] public OrbBudget budget;
        readonly List<OrbView> views=new List<OrbView>(32);
        int cursor,capOverride;
        int FrameCap=>capOverride>0?capOverride:settings.maxFps;
        public void SetFrameCap(int value){capOverride=Mathf.Clamp(value,settings.minFps,settings.maxFps);CurrentFps=System.Math.Min(CurrentFps,FrameCap);ResetSchedule();}
        double nextFrame,averageCost,previousTime,elapsed;
        public int CurrentFps {get;private set;}=40;
        public int RenderedLastFrame {get;private set;}
        public double LastCpuMs {get;private set;}
        public double Elapsed=>elapsed;
        public int Count=>views.Count;
        public long TextureBytes {get{long n=0;for(int i=0;i<views.Count;i++)n+=views[i].orbRenderer.TextureBytes;return n;}}
        protected override void OnEnable(){base.OnEnable();previousTime=clock!=null?clock.Now:0;elapsed=0;nextFrame=previousTime;CurrentFps=settings!=null?FrameCap:40;averageCost=0;}
        public void Register(OrbView view){if(!views.Contains(view))views.Add(view);}
        public void Unregister(OrbView view)=>views.Remove(view);
        public override void Tick(FxClock source)
        {
            double dt=System.Math.Max(0,source.Now-previousTime);previousTime=source.Now;
            if(!source.Frozen)elapsed+=dt;
            RenderedLastFrame=0;if(settings==null)return;
            if(source.Frozen){for(int i=0;i<views.Count;i++)views[i].Hold(source.Now,source.Reduced);return;}
            int fps=source.Reduced?settings.reducedFps:CurrentFps;
            if(source.Now<nextFrame)return;nextFrame=source.Now+1.0/System.Math.Max(1,fps);
            long timer=Stopwatch.GetTimestamp();int total=views.Count,visited=0;
            while(visited<total&&views.Count>0)
            {
                if(budget!=null&&!budget.Allows(RenderedLastFrame,(Stopwatch.GetTimestamp()-timer)*1000.0/Stopwatch.Frequency))break;
                cursor%=views.Count;var view=views[cursor];cursor=(cursor+1)%views.Count;visited++;
                if(view!=null&&view.RenderTick(source.Now,Elapsed,source.Reduced))RenderedLastFrame++;
            }
            LastCpuMs=(Stopwatch.GetTimestamp()-timer)*1000.0/Stopwatch.Frequency;averageCost=averageCost*.9+LastCpuMs*.1;
            if(!source.Reduced){if(averageCost>settings.costDownMs)CurrentFps=System.Math.Max(settings.minFps,CurrentFps-2);else if(averageCost<settings.costUpMs)CurrentFps=System.Math.Min(FrameCap,CurrentFps+1);}
        }
        public void ResetSchedule(){nextFrame=0;}
    }
}
