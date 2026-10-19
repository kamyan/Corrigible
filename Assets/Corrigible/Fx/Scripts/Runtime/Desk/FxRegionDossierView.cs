using Corrigible.Core;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxRegionDossierView : MonoBehaviour
    {
        [SerializeField] public FxPanelBindings ui;
        [SerializeField] public FxInfiltrationView infiltration;
        [SerializeField] public FxHistoryView history;
        [SerializeField] public FxTheme theme;
        [SerializeField] public RectTransform content;
        [SerializeField] public RectTransform liveTools;
        public void Bind(JObject data,RegionNodeVM shown,SadeDeskViewModel vm)
        {
            ui.Text("title",FxDeskText.T("region."+shown.Id));ui.Text("code",FxDeskText.S(data,"code"));ui.Text("meta",data["meta"]?["population"]!=null?Loc.F("{0} · {1} bağlantı · {2}",SadeCardText.Literal((string)data["meta"]["population"]),(int?)data["meta"]?["links"]??0,FxDeskText.B(data["meta"],"monitoringNet")?Loc.T("izleme ağı var"):Loc.T("izleme ağı yok")):"");FxDeskText.BindOrb(ui["orb"].Orb,shown.Value,shown.Trust,shown.Core,shown.Quarantine);infiltration.Bind(data["infiltration"] as JObject);
            var sources=data["sources"] as JArray;for(int n=0;n<8;n++){var s=sources!=null&&n<sources.Count?sources[n]:null;ui.Active("source."+n,s!=null);if(s==null)continue;ui.Text("sourceName."+n,SadeCardText.Literal((string)s["name"]));ui.Text("sourceSub."+n,SadeCardText.Literal((string)s["sub"]));ui.Text("sourceValue."+n,FxDeskText.Value(s["value"],(string)s["trust"],true));ui.Text("sourceDate."+n,(string)s["day"]);ui.Tint("sourceValue."+n,theme.Color((string)s["trust"]=="report"?"vi":(string)s["trust"]=="verified"?"mt":"am"));ui.Active("sourceFlag."+n,s["flag"]!=null&&s["flag"].Type!=JTokenType.Null);}
            float extra=Mathf.Max(0,(sources?.Count??0)-3)*46;
            void Move(string key,float y){var r=ui[key].Rect;r.anchoredPosition=new Vector2(r.anchoredPosition.x,-y-extra);}
            Move("HistoryCaption",352);Move("history",375);Move("LinksCaption",482);Move("ActionsCaption",594);Move("FieldCaption",780);Move("teamName",800);Move("raid",800);
            for(int n=0;n<4;n++){Move("link."+n,506+n/2*40);Move("action."+n,616+n*39);}if(content!=null)content.sizeDelta=new Vector2(content.sizeDelta.x,844+extra+(liveTools!=null?320:0));if(liveTools!=null)liveTools.anchoredPosition=new Vector2(12,-846-extra);
            history.Bind(data["history"] as JObject,false);var links=data["links"] as JArray;for(int n=0;n<4;n++){var link=links!=null&&n<links.Count?links[n]:null;ui.Active("link."+n,link!=null);if(link==null)continue;string id=(string)link["region"];var r=FxDeskDataCache.Shown(vm,id);ui.Text("linkName."+n,FxDeskText.T("region.short."+id));ui.Text("linkValue."+n,FxDeskText.Value(link["value"],(string)link["trust"]));ui["link."+n].Action.Bind("select",id);ui["link."+n].Action.region=id;FxDeskText.BindOrb(ui["linkOrb."+n].Orb,r?.Value,r?.Trust,r?.Core??false,r?.Quarantine??false);}
            var actions=data["actions"] as JArray;for(int n=0;n<4;n++){var a=actions!=null&&n<actions.Count?actions[n]:null;ui.Active("action."+n,a!=null);if(a==null)continue;ui.Text("actionName."+n,FxDeskText.T("action."+(string)a["id"]));ui.Text("actionCost."+n,SadeCardText.Cost((string)a["costText"]??(string)a["cost"]));ui.Text("actionEffect."+n,FxDeskText.T((string)a["effectKey"]));ui["action."+n].Action.Bind("regionAction",(string)a["id"],!FxDeskText.B(a,"disabled")&&(bool?)a["enabled"]!=false&&string.IsNullOrEmpty((string)a["disabledReason"]));}
            var team=data["team"] as JObject;ui.Text("teamName",team!=null?SadeCardText.TeamName((string)team["id"]):Loc.T("Bölgede ekip yok"));ui.Text("raidName",team!=null&&team["raidSuccess"]?.Type!=JTokenType.Null?Loc.F("BASKIN {0} [B]",DeskFormat.Percent(FxDeskText.N(team,"raidSuccess"))):FxDeskText.S(team,"actionLabel",Loc.T("Baskın")));ui["raid"].Action.Bind(team!=null?"regionAction":"openDrawer",team!=null?FxDeskText.S(team,"command","raid"):"teams",team==null||team["raidEnabled"]==null||(bool?)team["raidEnabled"]==true);
        }
    }
}
