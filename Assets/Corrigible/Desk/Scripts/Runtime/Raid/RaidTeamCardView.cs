using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    public sealed class RaidTeamCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] RectTransform visual;
        [SerializeField] CanvasGroup group;
        [SerializeField] Image warnRing,downCross;
        [SerializeField] TMP_Text downStamp;
        [SerializeField] Button button;
        [SerializeField] TMP_Text nameLabel, key;
        [SerializeField] Image thumbnail, nameHighlight;
        [SerializeField] Image[] pips, rightFill, rightRing, rightIcon;
        [Tooltip("Zırh plakası noktası (mavi): son can noktasının sağında.")]
        [SerializeField] Image armorPip;
        RaidTeamModel model; IRaidCommands commands; bool hover; float lift;
        public Button Button => button;
        public void Bind(IRaidCommands value, int index) { commands=value; key.text=(index+1).ToString(); button.onClick.AddListener(Select); }
        void Select() { if(model != null) commands?.SelectUnit(model.Letter); }
        public void Render(RaidTeamModel value, bool locked)
        {
            model=value;if(group!=null)group.alpha=locked ? .6f : 1;if(warnRing!=null)warnRing.enabled=value.Warn;if(downCross!=null)downCross.enabled=value.Down;
            if(downStamp!=null){downStamp.gameObject.SetActive(value.Down);downStamp.text=value.Absent?Loc.T("YARALI"):Loc.T("DÜŞTÜ");}
            nameLabel.text=value.Name;
            // Dar ekip kartında İngilizce uzun adın brifteki kısa karşılığı.
            if(value.Letter=='E' && DeskLoc.Instance.Lang==DeskLang.En) nameLabel.text=Loc.T("EMP UZM.");
            DeskSprites.Apply(thumbnail,value.Thumb); nameHighlight.enabled=value.Selected;
            RaidHudView.Pips(pips,value.Hp,value.Max,value.Down?"ink.red":"ink.ink");RaidHudView.ArmorPip(armorPip,pips,value.Max,value.Down?0:value.Armor);thumbnail.color=new Color(1,1,1,value.Down?.7f:1); button.interactable=!locked&&!value.Down;
            for(int i=0;i<2;i++)
            {
                bool available=i==0?value.Move:value.Ability, armed=i==1&&value.Armed;
                rightRing[i].enabled=rightIcon[i].enabled=!value.Down;rightFill[i].enabled=available&&!value.Down; rightFill[i].color=DeskTheme.Active.Get(armed?"sade.highlighter":"sade.index");
                rightRing[i].color=DeskTheme.Active.Get(available?"sade.status.info":"ink.disabledText");
                rightIcon[i].color=rightRing[i].color;
            }
        }
        public void OnPointerEnter(PointerEventData e) { hover=true; if(model!=null&&!model.Down)Hovered?.Invoke(model.Letter); }
        public void OnPointerExit(PointerEventData e) { hover=false; commands?.ClearHover(); }
        void Update()
        {
            float target=model!=null&&model.Selected?10:hover?2:0;
            lift=Mathf.MoveTowards(lift,target,Time.unscaledDeltaTime*100); visual.anchoredPosition=new Vector2(0,lift);
        }
        public event System.Action<char> Hovered;
    }
}
