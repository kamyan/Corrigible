using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Corrigible.Fx
{
    /// <summary>Yalnız niyet üretir; onay ve gerçek komut F4 penceresinin sorumluluğudur.</summary>
    public sealed class FxKillSwitchView : FxTimedBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        [SerializeField] public Image progress;
        [SerializeField] public float holdSeconds=3;
        public bool Interactable {get;set;}=true;
        public float Progress {get;private set;}
        public event Action Requested;
        bool held;double started;
        public void OnPointerDown(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left||!Interactable)return;held=true;started=clock.Now;}
        public void OnPointerUp(PointerEventData e)=>Cancel();
        public void OnPointerExit(PointerEventData e)=>Cancel();
        public void Cancel(){held=false;Progress=0;if(progress!=null)progress.fillAmount=0;}
        public override void Tick(FxClock source){if(!held||!Interactable)return;Progress=Mathf.Clamp01((float)((source.Now-started)/holdSeconds));progress.fillAmount=Progress;if(Progress>=1){held=false;Requested?.Invoke();}}
        protected override void OnDisable(){Cancel();base.OnDisable();}
    }
}
