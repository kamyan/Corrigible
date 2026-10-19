using System.Collections.Generic;
using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>Tek saatten çağrılan toplu yürütücü. Bileşen başına Update veya coroutine yoktur.</summary>
    public sealed class FxLoopRunner : MonoBehaviour
    {
        readonly List<FxTimedBehaviour> items=new List<FxTimedBehaviour>(256);
        public int Count=>items.Count;
        public void Register(FxTimedBehaviour item) { if(!items.Contains(item))items.Add(item); }
        public void Unregister(FxTimedBehaviour item)=>items.Remove(item);
        public void Tick(FxClock clock) { for(int i=items.Count-1;i>=0;i--) if(i<items.Count && items[i]!=null)items[i].Tick(clock); }
    }
}
