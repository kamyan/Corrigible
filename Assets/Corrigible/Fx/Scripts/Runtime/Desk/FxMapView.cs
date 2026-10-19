using System;
using Corrigible.Desk;
using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>FX map görünümü yalnız mevcut SadeMapStateVM'yi okur; sahne/prefab geometriyi taşır.</summary>
    public sealed class FxMapView : MonoBehaviour, IDeskMapGeometry
    {
        public float WorldScale=>cover.Scale*transform.lossyScale.x;
        public Vector3 MapToWorld(float x,float y){var point=cover.ToCanvas(new Vector2(x,y));var rect=(RectTransform)transform;return rect.TransformPoint(new Vector3(point.x-rect.rect.width*.5f,rect.rect.height*.5f-point.y,0));}
        public Vector3 RegionWorld(string id){var slot=Find(id);return slot!=null?slot.Node.transform.position:transform.position;}
        [Serializable] public sealed class RegionSlot
        {
            public string Id;public Vector2 Point;public RegionMeshGraphic HitArea;public FxRegionNodeView Node;
            public GameObject QuarantineOutline,Selection,Highlight,Alert;
        }
        [SerializeField] public MapCoverFitter cover;
        [SerializeField] public RegionSlot[] regions;
        [SerializeField] public FxMapThreadView[] threads;
        [SerializeField] public FxTeamTokenView[] teams;
        [SerializeField] public bool liveSlots;
        [SerializeField] public FxFieldReportView[] reports=Array.Empty<FxFieldReportView>();
        public event Action<string> ReportClicked;
        void Awake(){foreach(var report in reports)report.Clicked+=OnReport;}
        void OnReport(string command)=>ReportClicked?.Invoke(command);
        public void BindReports(System.Collections.Generic.List<FieldReportVM> values){for(int n=0;n<reports.Length;n++){reports[n].gameObject.SetActive(n<values.Count);if(n<values.Count)reports[n].Bind(values[n]);}}
        readonly System.Collections.Generic.List<string> alerts=new System.Collections.Generic.List<string>();
        public void Bind(SadeMapStateVM vm)
        {
            alerts.Clear();alerts.AddRange(vm.AlertRegions);
            foreach(var slot in regions){RegionNodeVM value=null;foreach(var r in vm.Regions)if(r.Id==slot.Id){value=r;break;}bool visible=value!=null;slot.Node.gameObject.SetActive(visible);slot.HitArea.gameObject.SetActive(visible);if(!visible)continue;slot.Node.Bind(value);slot.QuarantineOutline.SetActive(value.Quarantine);slot.Selection.SetActive(value.Selected);slot.Alert.SetActive(vm.AlertRegions.Contains(slot.Id)&&!value.Selected);}
            for(int index=0;index<threads.Length;index++){var thread=threads[index];if(liveSlots&&index>=vm.SpreadThreads.Count){thread.gameObject.SetActive(false);continue;}if(liveSlots&&index<vm.SpreadThreads.Count){thread.fromId=vm.SpreadThreads[index].From;thread.toId=vm.SpreadThreads[index].To;}bool active=false;foreach(var t in vm.SpreadThreads)if(thread.fromId==t.From&&thread.toId==t.To){active=true;break;}thread.gameObject.SetActive(active);if(active)thread.Refresh();}
            for(int index=0;index<teams.Length;index++){var token=teams[index];if(liveSlots&&index>=vm.Teams.Count){token.gameObject.SetActive(false);continue;}if(liveSlots&&index<vm.Teams.Count)token.teamId=vm.Teams[index].Id;SadeMapTeamVM value=null;foreach(var t in vm.Teams)if(t.Id==token.teamId){value=t;break;}token.gameObject.SetActive(value!=null);if(value!=null)token.Bind(value);}
        }
        public RegionSlot Find(string id){foreach(var s in regions)if(s.Id==id)return s;return null;}
        public void Select(string id){foreach(var s in regions){bool selected=s.Id==id;s.Node.SetSelected(selected);s.Selection.SetActive(selected);s.Alert.SetActive(!selected&&alerts.Contains(s.Id));}}
        public void HighlightTeam(string id){foreach(var team in teams)team.SetHighlighted(team.teamId==id);}
        public void Highlight(string id){foreach(var s in regions)s.Highlight.SetActive(s.Id==id);}
    }
}
