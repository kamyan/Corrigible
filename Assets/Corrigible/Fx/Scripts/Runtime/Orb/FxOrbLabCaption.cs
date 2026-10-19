using Corrigible.Core;
using Corrigible.Desk;
using TMPro;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxOrbLabCaption : MonoBehaviour
    {
        public enum CaptionKind { StateNumber, Range, Zone }
        [SerializeField] public CaptionKind kind;
        [SerializeField] public TMP_Text text;
        [SerializeField] public RectTransform followingTitle;
        [SerializeField] public int number=1,low,high=30;
        [SerializeField] public string stateName="UYUMLU";
        void OnEnable(){DeskLoc.Instance.Changed+=Refresh;Refresh();}
        void OnDisable()=>DeskLoc.Instance.Changed-=Refresh;
        public void Refresh(){if(text==null)return;text.text=kind==CaptionKind.StateNumber?Loc.F("HÂL {0}",number):kind==CaptionKind.Range?Loc.F("sızma {0}–{1}",low,high):Loc.F("HÂL {0} · {1}",number,Loc.T(stateName));if(followingTitle!=null){var p=followingTitle.anchoredPosition;p.x=text.rectTransform.anchoredPosition.x+text.preferredWidth+12;followingTitle.anchoredPosition=p;}}
    }
}
