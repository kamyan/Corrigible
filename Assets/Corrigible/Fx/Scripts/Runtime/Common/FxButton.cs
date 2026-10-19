using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Corrigible.Fx
{
    public enum FxButtonKind { Normal, Primary, Danger, Small, Large, EndTurn, Off, Confirm, Link }
    /// <summary>Standart Button olaylarını korur; hover ve odak aynı kenarı gösterir.</summary>
    public sealed class FxButton : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public Button button;
        [SerializeField] public TMPro.TMP_Text label;
        [SerializeField] public Image fill,edge,glow;
        [SerializeField] public CanvasGroup group;
        [SerializeField] public FxLoop pulse;
        [SerializeField] public FxButtonKind kind;
        [SerializeField] public bool solidPrimary;
        bool hovered,selected;
        public static float Height(FxButtonKind kind)=>kind==FxButtonKind.Small?26:kind==FxButtonKind.Large?48:kind==FxButtonKind.EndTurn?58:32;
        void OnEnable()=>Refresh();
        public void Refresh()
        {
            if(theme==null)return;bool off=kind==FxButtonKind.Off;button.interactable=!off;
            string accent=kind==FxButtonKind.Danger?"rd":kind==FxButtonKind.Confirm?"am":"cy";var c=theme.Color(accent);
            theme.Apply(fill,"fx_chamfer_7",c);fill.enabled=kind!=FxButtonKind.Link;var f=c;f.a=kind==FxButtonKind.EndTurn?.95f:kind==FxButtonKind.Primary?.22f:.06f;if(solidPrimary&&kind==FxButtonKind.Primary)f.a=1;fill.color=f;
            theme.Apply(edge,"fx_chamfer_7_line",(hovered||selected)&&!off?theme.Color("cy2"):c);
            edge.enabled=kind!=FxButtonKind.Link;group.alpha=off?.45f:1;
            if(glow!=null){theme.Apply(glow,"fx_glow_frame",c);glow.material=theme.additive;glow.enabled=(hovered||selected)&&!off;}
            if(label!=null){theme.Apply(label,kind==FxButtonKind.EndTurn?"endTurn":kind==FxButtonKind.Large?"buttonLarge":kind==FxButtonKind.Link?"moreLink":"button");if(kind==FxButtonKind.Confirm)label.color=theme.Color("am2");if(solidPrimary&&kind==FxButtonKind.Primary)label.color=theme.Color("bg");}
            if(pulse!=null)pulse.enabled=kind==FxButtonKind.Confirm;
        }
        public void OnPointerEnter(PointerEventData e){hovered=true;Refresh();}
        public void OnPointerExit(PointerEventData e){hovered=false;Refresh();}
        public void OnSelect(BaseEventData e){selected=true;Refresh();}
        public void OnDeselect(BaseEventData e){selected=false;Refresh();}
    }
}
