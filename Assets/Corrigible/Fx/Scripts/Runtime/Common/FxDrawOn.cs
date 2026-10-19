using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>Giriş zamanı bir kez yazılır. Hareket azaltıldığında giriş kalıcı tamamlanır; akış shader'da sürer.</summary>
    public sealed class FxDrawOn : FxTimedBehaviour
    {
        [SerializeField] public FxLineGraphic line;
        [SerializeField] public float duration=1.4f,delay=.3f;
        double start;bool completed;
        protected override void OnEnable(){base.OnEnable();start=clock!=null?clock.Now:0;completed=false;if(line!=null)line.BeginReveal((float)start,duration,delay);}
        public override void Tick(FxClock source)
        {
            if(completed)return;
            if(source.CompleteEntrances){if(line!=null)line.BeginReveal(0,0,0);completed=true;}
            else if(source.Now>=start+duration+delay)completed=true;
        }
    }
}
