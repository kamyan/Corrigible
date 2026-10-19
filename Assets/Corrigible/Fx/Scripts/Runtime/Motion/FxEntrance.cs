using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxEntrance : FxTimedBehaviour
    {
        [SerializeField] public RectTransform target;
        [SerializeField] public CanvasGroup group;
        [SerializeField] public Vector2 offset=new Vector2(0,-14);
        [SerializeField] public float duration=.7f,delay,startScale=1;
        Vector2 end;Vector3 scale;double start;float previous=-1;bool completed;
        protected override void OnEnable() { base.OnEnable();if(target!=null){end=target.anchoredPosition;scale=target.localScale;} start=clock!=null?clock.Now:0;previous=-1;completed=false; }
        protected override void OnDisable() {base.OnDisable();if(target!=null){target.anchoredPosition=end;target.localScale=scale;}if(group!=null)group.alpha=1;}
        public override void Tick(FxClock source)
        {
            if(completed)return;
            float p=FxTween.Progress(source.Now,start,duration,delay,source.CompleteEntrances);
            if(p>=1)completed=true;
            if(p==previous)return;previous=p;
            if(target!=null){target.anchoredPosition=end+offset*(1-p);target.localScale=scale*Mathf.Lerp(startScale,1,p);}if(group!=null)group.alpha=p;
        }
    }
}
