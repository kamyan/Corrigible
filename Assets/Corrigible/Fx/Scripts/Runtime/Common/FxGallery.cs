using Corrigible.Core;
using Corrigible.Desk;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxGallery : MonoBehaviour
    {
        [SerializeField] public FxClock clock;
        [SerializeField] public Toggle reduced,freeze;
        [SerializeField] public Button language;
        [SerializeField] public GameObject[] pages;
        [SerializeField] public Button next,previous;
        [SerializeField] public TMPro.TMP_Text pageLabel;
        int page;
        void OnEnable(){reduced.onValueChanged.AddListener(Reduce);freeze.onValueChanged.AddListener(Freeze);language.onClick.AddListener(Language);next.onClick.AddListener(Next);previous.onClick.AddListener(Previous);DeskLoc.Instance.Changed+=Refresh;Refresh();}
        void OnDisable(){reduced.onValueChanged.RemoveListener(Reduce);freeze.onValueChanged.RemoveListener(Freeze);language.onClick.RemoveListener(Language);next.onClick.RemoveListener(Next);previous.onClick.RemoveListener(Previous);DeskLoc.Instance.Changed-=Refresh;}
        void Reduce(bool value)=>clock.Reduced=value;
        void Freeze(bool value){if(value)clock.Freeze();else clock.Resume();}
        void Language()=>DeskLoc.Instance.Toggle();
        void Next(){page=(page+1)%pages.Length;Refresh();}
        void Previous(){page=(page+pages.Length-1)%pages.Length;Refresh();}
        public void ShowPage(int index){page=Mathf.Clamp(index,0,pages.Length-1);Refresh();}
        void Refresh(){for(int i=0;i<pages.Length;i++)pages[i].SetActive(i==page);pageLabel.text=Loc.F("SAYFA {0} / {1}",page+1,pages.Length);}
    }
}
