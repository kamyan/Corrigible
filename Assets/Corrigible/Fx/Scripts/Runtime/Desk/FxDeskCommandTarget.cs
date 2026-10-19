using Corrigible.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Corrigible.Fx
{
    /// <summary>Yazılmış FX hit alanı. Düğme, satır, kaynak veya harita bağı aynı komut sınırını kullanır.</summary>
    public sealed class FxDeskCommandTarget : Selectable,IPointerClickHandler,ISubmitHandler
    {
        [SerializeField] public FxDeskInteraction owner;
        [SerializeField] public string command,argument,region,team,accent="cy",hoverTip;
        [SerializeField] public Image fill,edge;
        [SerializeField] public bool solid;
        [SerializeField] public FxTheme theme;
        public bool PreviewSelected{get;private set;}
        protected override void OnEnable(){base.OnEnable();transition=Transition.None;Refresh(false);}
        public void Bind(string cmd,string arg,bool available=true){command=cmd;argument=arg;interactable=available;Refresh(false);}
        public void SetPreview(bool on){PreviewSelected=on;Refresh(on);}
        void Refresh(bool hover)
        {
            if(theme==null)return;var c=theme.Color(interactable?accent:"tx3");var f=c;f.a=solid?1:(hover||PreviewSelected?.14f:.04f);if(fill!=null)fill.color=f;c.a=hover||PreviewSelected?.8f:.25f;if(edge!=null)edge.color=c;
        }
        public override void OnPointerEnter(PointerEventData e){base.OnPointerEnter(e);Refresh(true);owner?.HoverCommand(this,true);}
        public override void OnPointerExit(PointerEventData e){base.OnPointerExit(e);Refresh(false);owner?.HoverCommand(this,false);}
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&IsInteractable())owner?.Command(command,argument);}
        public void OnSubmit(BaseEventData e){if(IsInteractable())owner?.Command(command,argument);}
        protected override void OnDisable(){if(owner!=null)owner.HoverCommand(this,false);base.OnDisable();}
    }
}
