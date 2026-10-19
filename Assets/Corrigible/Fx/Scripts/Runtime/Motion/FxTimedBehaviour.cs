using UnityEngine;
namespace Corrigible.Fx
{
    public abstract class FxTimedBehaviour : MonoBehaviour
    {
        [SerializeField] public FxClock clock;
        protected virtual void OnEnable() { if(clock!=null)clock.Runner.Register(this); }
        protected virtual void OnDisable() { if(clock!=null)clock.Runner.Unregister(this); }
        public abstract void Tick(FxClock source);
    }
}
