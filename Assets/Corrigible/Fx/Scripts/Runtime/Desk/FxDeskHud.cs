using Corrigible.Desk;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxDeskHud : MonoBehaviour
    {
        [SerializeField] public RectTransform viewport;
        [SerializeField] public FxHudChipView[] resources,risks;
        [SerializeField] public FxGoalChipView goal;
        [SerializeField] public FxClockPlaqueView time;
        [SerializeField] public FxDockTabView[] tabs;
        SadeDeskViewModel model;
        public void Bind(SadeDeskViewModel vm,string open){model=vm;for(int i=0;i<resources.Length;i++)resources[i].Bind(vm.Resources[i]);for(int i=0;i<risks.Length;i++)risks[i].Bind(vm.Risks[i]);goal.Bind(vm.Goals);time.Bind(vm.Clock);foreach(var tab in tabs)foreach(var t in vm.Dock.Tabs)if(t.Id==tab.id)tab.Bind(t,open==t.Id);RefreshLayout();}
        void OnRectTransformDimensionsChange()=>RefreshLayout();
        public void RefreshLayout()
        {
            if(model==null)return;float x=16;foreach(var chip in resources){Place((RectTransform)chip.transform,x,14);x+=chip.Width+8;}Place((RectTransform)goal.transform,x,14);float end=x+((RectTransform)goal.transform).rect.width;
            float right=viewport.rect.width-16;for(int i=risks.Length-1;i>=0;i--){right-=risks[i].Width;Place((RectTransform)risks[i].transform,right,14);right+=2;}
            float tw=((RectTransform)time.transform).rect.width;float tx=Mathf.Clamp((viewport.rect.width-tw)*.5f,end+8,Mathf.Max(end+8,right-8-tw));Place((RectTransform)time.transform,tx,14);
            x=16;foreach(var tab in tabs){var rt=(RectTransform)tab.transform;rt.anchoredPosition=new Vector2(x,12);x+=tab.Width+8;}
        }
        static void Place(RectTransform r,float x,float y){r.anchoredPosition=new Vector2(x,-y);}
    }
}
