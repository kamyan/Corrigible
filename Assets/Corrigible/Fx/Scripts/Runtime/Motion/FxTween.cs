using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>CSS cubic-bezier(.2,.8,.2,1); x zamanını ters çözerek y değerini bulur.</summary>
    public struct FxTween
    {
        public static float Ease(float time)
        {
            if(time<=0)return 0; if(time>=1)return 1;
            float lo=0,hi=1,u=time;
            for(int i=0;i<18;i++) { float x=3*(1-u)*(1-u)*u*.2f+3*(1-u)*u*u*.2f+u*u*u; if(x<time)lo=u;else hi=u;u=(lo+hi)*.5f; }
            return 3*(1-u)*(1-u)*u*.8f+3*(1-u)*u*u+u*u*u;
        }
        public static float Progress(double now,double start,float duration,float delay=0,bool skip=false)
            => skip?1:Ease(Mathf.Clamp01((float)(now-start-delay)/Mathf.Max(.001f,duration)));
    }
}
