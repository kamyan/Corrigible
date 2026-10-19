using System;
using Corrigible.Core;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>Yazılmış bütün masa panelleri ve yerleşimleri. Durumun sahibi FxDeskInteraction'dır.</summary>
    public sealed class FxDeskPanels : MonoBehaviour
    {
        [SerializeField] public FxDeskInteraction owner;
        [SerializeField] public RectTransform viewport;
        [SerializeField] public RectTransform liveCardViewport;
        [SerializeField] public float liveCardHeight=822;
        [SerializeField] public FxPopupView card,dossier,legend,nodeTip,breakdown,confirmation,pause;
        [SerializeField] public FxPopupView[] drawers;
        [SerializeField] public FxRegionCardView cardView;
        [SerializeField] public FxRegionDossierView dossierView;
        [SerializeField] public FxNodeTooltipView nodeView;
        [SerializeField] public FxBreakdownTooltipView breakdownView;
        [SerializeField] public FxDrawerView[] drawerViews;
        [SerializeField] public FxPanelBindings confirmUi,pauseUi;
        [SerializeField] public FxLineGraphic leaderGlow,leader;
        [SerializeField] public TMP_Text killHint;
        [SerializeField] public FxDeskCommandTarget[] hudTargets;
        readonly Vector3[] corners=new Vector3[4];
        string shownTip,hudTip;
        public string TooltipRegion=>shownTip;
        public string TooltipHud=>hudTip;
        public void Render(bool animate)
        {
            if(owner.Model==null)return;var state=owner.State;
            if(liveCardViewport!=null)card.rect.sizeDelta=new Vector2(card.rect.sizeDelta.x,Mathf.Min(liveCardHeight,viewport.rect.height-192));
            for(int n=0;n<drawers.Length;n++){if(state.Drawer==drawerViews[n].drawerId){drawers[n].Show(DrawerPoint(n),animate);drawerViews[n].Bind(owner.Data.Drawer(state.Drawer));}else drawers[n].Close(animate);}
            var shown=FxDeskDataCache.Shown(owner.Model,state.SelectedRegion);
            if(shown!=null&&!state.FullDossier){cardView.Bind(owner.Data.Card(owner.Model,shown.Id,owner.Illusion),shown);card.Show(CardPoint(),animate);}else card.Close(animate);
            if(shown!=null&&state.FullDossier){dossierView.Bind(owner.Data.Dossier(owner.Model,shown.Id,owner.Illusion),shown,owner.Model);dossier.rect.sizeDelta=new Vector2(500,Mathf.Min(918,viewport.rect.height-172));dossier.Show(DossierPoint(),animate);}else dossier.Close(animate);
            if(state.Legend)legend.Show(LegendPoint(),animate);else legend.Close(animate);
            if(state.Confirmation)confirmation.Show(new Vector2((viewport.rect.width-confirmation.Size.x)*.5f,(viewport.rect.height-confirmation.Size.y)*.5f),animate);else confirmation.Close(animate);
            if(state.Pause)pause.Show(new Vector2((viewport.rect.width-pause.Size.x)*.5f,(viewport.rect.height-pause.Size.y)*.5f),animate);else pause.Close(animate);
            killHint.text=Loc.T(owner.Illusion?Loc.N("Gerekli değil"):Loc.N("3 sn basılı tut"));owner.killSwitch.Interactable=!owner.Illusion&&!owner.Blocked;RefreshLayout();
        }
        public void RefreshLayout()
        {
            if(owner==null||owner.Model==null||card==null)return;
            for(int n=0;n<drawers.Length;n++)if(drawers[n].IsOpen)drawers[n].MoveTo(DrawerPoint(n));
            if(card.IsOpen)card.MoveTo(CardPoint());if(dossier.IsOpen)dossier.MoveTo(DossierPoint());if(legend.IsOpen)legend.MoveTo(LegendPoint());
            if(nodeTip.IsOpen&&shownTip!=null)PlaceNodeTip(shownTip);if(breakdown.IsOpen&&hudTip!=null)PlaceBreakdown(hudTip);
            RefreshLeader();
        }
        void OnRectTransformDimensionsChange(){if(owner!=null)RefreshLayout();}
        Vector2 DrawerPoint(int index){var r=drawers[index];float h=Mathf.Min(drawerViews[index].drawerId=="research"?652:drawerViews[index].drawerId=="news"?706:drawerViews[index].drawerId=="teams"?494:drawerViews[index].drawerId=="aiLog"?540:576,viewport.rect.height-168);r.rect.sizeDelta=new Vector2(drawerViews[index].drawerId=="guide"?560:500,h);return new Vector2(16,viewport.rect.height-76-h);}
        Vector2 DossierPoint()=>new Vector2(NodePoint(owner.State.SelectedRegion).x>viewport.rect.width*.5f?16:viewport.rect.width-516,92);
        Vector2 LegendPoint()=>new Vector2(viewport.rect.width-16-legend.Size.x,viewport.rect.height-88-legend.Size.y);
        public Vector2 NodePoint(string id){var s=owner.map.Find(id);return s==null?Vector2.zero:owner.map.cover.ToCanvas(s.Point);}
        Vector2 CardPoint()
        {
            var s=owner.map.Find(owner.State.SelectedRegion);if(s==null)return new Vector2(16,96);Rect obstacle=default;
            for(int n=0;n<drawers.Length;n++)if(owner.State.Drawer==drawerViews[n].drawerId)obstacle=new Rect(DrawerPoint(n),drawers[n].Size);
            return FxPanelPlacement.Card(NodePoint(s.Id),(float)s.Node.Radius,card.Size,viewport.rect.size,obstacle,ContextRect(s.Node.label.rectTransform));
        }
        public Rect ContextRect(RectTransform rect)
        {
            rect.GetWorldCorners(corners);var top=viewport.InverseTransformPoint(corners[1]);var bottom=viewport.InverseTransformPoint(corners[3]);return new Rect(top.x-viewport.rect.xMin,viewport.rect.yMax-top.y,bottom.x-top.x,top.y-bottom.y);
        }
        void RefreshLeader()
        {
            bool show=card.IsOpen&&owner.State.SelectedRegion!=null&&!owner.State.FullDossier;leader.gameObject.SetActive(show);leaderGlow.gameObject.SetActive(show);if(!show)return;
            var node=owner.map.Find(owner.State.SelectedRegion).Node;var p=NodePoint(node.regionId);var r=ContextRect(card.rect);bool right=r.center.x>p.x;float from=p.x+(right?1:-1)*(float)node.Radius*1.15f;float y=p.y-4;float endY=Mathf.Clamp(y,r.yMin+24,r.yMax-24);float endX=right?r.xMin:r.xMax;
            SetLine(leader,from,-y,endX,-endY);SetLine(leaderGlow,from,-y,endX,-endY);
        }
        static void SetLine(FxLineGraphic line,float x,float y,float tx,float ty){var a=new Vector2(x,y);var b=new Vector2(tx,ty);if(line.points!=null&&line.points.Length==2&&line.points[0]==a&&line.points[1]==b)return;line.points=new[]{a,b};line.SetVerticesDirty();}
        public void HideTips(bool animate=false)
        {
            if(shownTip!=null){var s=owner.map.Find(shownTip);if(s!=null)s.Node.label.gameObject.SetActive(true);}shownTip=hudTip=null;owner.State.Tooltip=false;nodeTip.Close(animate);breakdown.Close(animate);
        }
        public void ShowNode(string id,bool animate=true)
        {
            if(owner.Blocked||owner.map.Find(id)==null)return;HideTips();shownTip=id;owner.State.Tooltip=true;nodeView.Bind(owner.Data.Tooltip(owner.Model,id,owner.Illusion));PlaceNodeTip(id);nodeTip.Show(NodeTipPoint(id),animate);owner.map.Find(id).Node.label.gameObject.SetActive(false);
        }
        Vector2 NodeTipPoint(string id){var p=NodePoint(id);var r=owner.map.Find(id).Node.Radius;var point=FxPanelPlacement.NodeTip(p,(float)r,nodeTip.Size,viewport.rect.size,out float pointer);nodeView.ui["pointer"].Rect.anchoredPosition=new Vector2(pointer,0);return point;}
        void PlaceNodeTip(string id)=>nodeTip.MoveTo(NodeTipPoint(id));
        public void ShowBreakdown(string id,bool animate=true,string message=null)
        {
            if(owner.Blocked)return;HideTips();hudTip=id;owner.State.Tooltip=true;var data=owner.Data.Breakdown(owner.Model,id,owner.Illusion);if(message!=null)data["message"]=message;breakdownView.Bind(data);breakdown.rect.sizeDelta=new Vector2(300,data["history"]!=null?238.4f:data["rows"]!=null?Mathf.Max(120,breakdownView.ui["genericBody"].Rect.rect.height+64):120);PlaceBreakdown(id);breakdown.Show(BreakdownPoint(id),animate);
        }
        Vector2 BreakdownPoint(string id)
        {
            Rect rect=new Rect(16,14,182,66);foreach(var target in hudTargets)if(target.argument==id){rect=ContextRect((RectTransform)target.transform);break;}
            if(rect.center.x>viewport.rect.width*.5f)rect.x=rect.xMax-breakdown.Size.x;
            var pos=FxPanelPlacement.Breakdown(rect,breakdown.Size,viewport.rect.size);breakdownView.ui["pointer"].Rect.anchoredPosition=new Vector2(Mathf.Clamp(rect.center.x-pos.x,18,breakdown.Size.x-18),0);return pos;
        }
        void PlaceBreakdown(string id)=>breakdown.MoveTo(BreakdownPoint(id));
        public void ConfirmText(bool global){confirmUi.Text("title",Loc.T(global?Loc.N("Küresel Son Şalter"):Loc.N("Bölgenin Son Şalteri")));confirmUi.Text("body",Loc.T(global?Loc.N("Tüm ağları kapatma kararını onaylıyor musunuz?"):Loc.N("Bölge çevrimdışı kalacak. Bu kararı onaylıyor musunuz?")));}
        public void Preview(string action,JObject data){if(card.IsOpen)cardView.Preview(action,data);}
        public FxDrawerView Drawer(string id){foreach(var d in drawerViews)if(d.drawerId==id)return d;return null;}
        public void CompleteAnimations(){foreach(var p in drawers)if(p.IsAnimating)p.Complete();foreach(var p in new[]{card,dossier,legend,nodeTip,breakdown,confirmation,pause})if(p.IsAnimating)p.Complete();RefreshLayout();}
    }
}
