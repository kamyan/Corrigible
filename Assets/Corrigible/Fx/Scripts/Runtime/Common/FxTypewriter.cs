using TMPro;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxTypewriter : FxTimedBehaviour
    {
        [SerializeField] public TMP_Text text;
        [SerializeField] public RectTransform cursor;
        [SerializeField] public UnityEngine.UI.Graphic cursorGraphic;
        [SerializeField] public float duration=2.2f,delay=.6f;
        double start;string previous;int count;bool completed;
        public int Visible=>text.maxVisibleCharacters;
        protected override void OnEnable(){base.OnEnable();Restart();}
        public void Restart(){completed=false;start=clock!=null?clock.Now:0;previous=text!=null?text.text:null;if(text!=null){text.ForceMeshUpdate();count=text.textInfo.characterCount;}}
        public override void Tick(FxClock source)
        {
            if(text==null)return;if(previous!=text.text)Restart();
            float p=completed||source.CompleteEntrances?1:Mathf.Clamp01((float)(source.Now-start-delay)/Mathf.Max(.001f,duration));
            if(p>=1)completed=true;
            int steps=Mathf.FloorToInt(p*48);int visible=p>=1?count:Mathf.FloorToInt(count*steps/48f);if(text.maxVisibleCharacters!=visible)text.maxVisibleCharacters=visible;
            if(cursorGraphic!=null)cursorGraphic.canvasRenderer.SetAlpha(source.Static?0:FxLoop.Sample(FxWave.Step,source.Phase(.9f,0,FxPhase.Cursor)));
            if(cursor!=null && count>0){int i=Mathf.Clamp(visible-1,0,count-1);var c=text.textInfo.characterInfo[i];cursor.anchoredPosition=new Vector2(visible==0?0:c.xAdvance,c.baseLine);}
        }
        protected override void OnDisable(){base.OnDisable();if(text!=null)text.maxVisibleCharacters=int.MaxValue;}
    }
}
