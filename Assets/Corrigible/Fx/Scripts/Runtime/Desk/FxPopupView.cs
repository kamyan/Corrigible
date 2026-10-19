using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>FX saatiyle açılış/kapanış. Kapanan içerik görünürlük ve girdi ağacından çıkar.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FxPopupView : FxTimedBehaviour
    {
        [SerializeField] public RectTransform rect;
        [SerializeField] public CanvasGroup group;
        [SerializeField] public RectTransform backdrop,viewport;
        [SerializeField] public Vector2 entrance=new Vector2(0,-22);
        [SerializeField] public float duration=.55f,startScaleY=1;
        public bool IsOpen{get;private set;}
        public bool IsAnimating=>animating;
        Vector2 position;double began;bool animating,closing;
        public Vector2 Size=>rect.rect.size;
        public void Show(Vector2 point,bool animate=true)
        {
            position=new Vector2(point.x,-point.y);
            if(IsOpen&&!closing){if(!animating)rect.anchoredPosition=position;return;}
            IsOpen=true;closing=false;began=clock.Now;animating=animate&&!clock.CompleteEntrances;
            gameObject.SetActive(true);group.blocksRaycasts=true;group.interactable=true;
            if(!animating)Complete();else Tick(clock);
        }
        public void MoveTo(Vector2 point){position=new Vector2(point.x,-point.y);if(!animating)rect.anchoredPosition=position;}
        public void Close(bool animate=true)
        {
            if(!IsOpen&&!closing)return;IsOpen=false;closing=true;began=clock.Now;
            group.blocksRaycasts=false;group.interactable=false;animating=animate&&!clock.CompleteEntrances;
            if(!animating)Complete();
        }
        public void Complete()
        {
            animating=false;rect.anchoredPosition=position;rect.localScale=Vector3.one;group.alpha=closing?0:1;
            UpdateBackdrop();if(closing){closing=false;gameObject.SetActive(false);}
        }
        void UpdateBackdrop(){if(backdrop!=null&&viewport!=null){backdrop.anchoredPosition=-rect.anchoredPosition;backdrop.sizeDelta=viewport.rect.size;}}
        public override void Tick(FxClock source)
        {
            if(!animating)return;float p=FxTween.Progress(source.Now,began,closing?.25f:duration,0,source.CompleteEntrances);
            group.alpha=closing?1-p:p;rect.anchoredPosition=position+(closing?new Vector2(0,-12)*p:entrance*(1-p));
            rect.localScale=new Vector3(1,closing?1:Mathf.Lerp(startScaleY,1,p),1);UpdateBackdrop();if(p>=1)Complete();
        }
    }
}
