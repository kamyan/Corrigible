using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Corrigible.Fx
{
    public sealed class FxStamp : FxTimedBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public Image frame;
        [SerializeField] public TMP_Text label;
        [SerializeField] public RectTransform target;
        [SerializeField] public string accent="rd";
        Vector2 origin;
        protected override void OnEnable(){base.OnEnable();if(target!=null)origin=target.anchoredPosition;if(theme!=null){theme.Apply(frame,"fx_frame_1_5",theme.Color(accent));label.font=theme.Font("ChakraPetch-Bold");label.fontSize=13;label.characterSpacing=18;label.color=theme.Color(accent);}}
        protected override void OnDisable(){base.OnDisable();if(target!=null)target.anchoredPosition=origin;}
        public override void Tick(FxClock source){if(target==null)return;float p=source.Phase(4,0,FxPhase.Glitch);float dx=accent=="rd"&&p>.86f&&p<.92f?(Mathf.FloorToInt(p*100)%2==0?2:-2):0;target.anchoredPosition=origin+Vector2.right*dx;}
    }
}
