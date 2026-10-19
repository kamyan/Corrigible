using Corrigible.Core;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxRegionCardView : MonoBehaviour
    {
        [SerializeField] public FxPanelBindings ui;
        [SerializeField] public FxInfiltrationView infiltration;
        [SerializeField] public FxTheme theme;
        JObject model;
        public void Bind(JObject data,RegionNodeVM shown)
        {
            model=data;ui.Text("title",FxDeskText.T(FxDeskText.S(data,"nameKey","region."+shown.Id)));ui.Text("code",FxDeskText.S(data,"code"));
            ui.Text("meta",shown.Id=="DoA"?SadeCardText.Literal("1,64 mlr"):"");
            FxDeskText.BindOrb(ui["orb"].Orb,shown.Value,shown.Trust,shown.Core,shown.Quarantine);
            ui.Text("status",FxDeskText.T(FxDeskText.S(data["status"],"textKey")));bool conflict=FxDeskText.S(data["status"],"trust")=="suspicious";
            ui.Active("statusRow",conflict||FxDeskText.B(data,"illusion"));if(FxDeskText.B(data,"illusion"))ui.Text("status",Loc.T("Kapsam %0 · doğrulandı · 0 ms"));ui.Text("gap",conflict?Loc.F("{0} puan fark",SourceGap(data["infiltration"])):"");
            infiltration.Bind(data["infiltration"] as JObject);ui.Text("state",FxDeskText.State(shown.Value));ui.Tint("state",FxDeskText.StateColor(theme,shown.Value));
            ui.Text("stateNote",conflict?Loc.T("iki kaynakta da"):"");
            var stats=data["stats"] as JArray;for(int n=0;n<3;n++){var s=stats!=null&&n<stats.Count?stats[n]:null;ui.Active("stat."+n,s!=null);if(s==null)continue;ui.Text("statValue."+n,FxDeskText.Value(s["value"],(string)s["trust"],false,(bool?)s["approx"]==true));ui.Tint("statValue."+n,theme.Color((string)s["trust"]=="report"?"vi":"tx"));ui.Text("statName."+n,FxDeskText.T("card."+(string)s["id"]));}
            var actions=data["actions"] as JArray;for(int n=0;n<4;n++){var a=actions!=null&&n<actions.Count?actions[n]:null;ui.Active("action."+n,a!=null);if(a==null)continue;var target=ui["action."+n].Action;target.Bind("regionAction",(string)a["id"],!FxDeskText.B(a,"disabled")&&(bool?)a["enabled"]!=false&&string.IsNullOrEmpty((string)a["disabledReason"]));target.hoverTip=FxDeskText.T((string)a["disabledReason"]);ui.Text("actionName."+n,FxDeskText.T("action."+(string)a["id"]));}
            BindTeam(data["team"] as JObject);Preview((string)data["preview"]?["actionId"]??"monitorNet",data["preview"] as JObject);
        }
        static int SourceGap(JToken data)
        {
            float low=float.MaxValue,high=float.MinValue;foreach(var s in data?["sources"]??new JArray()){float v=FxDeskText.N(s,"value");low=Mathf.Min(low,v);high=Mathf.Max(high,v);}return high>=low?Mathf.RoundToInt(high-low):0;
        }
        public void DisabledPreview(string reason){ui.Text("previewResult",reason);}
        void BindTeam(JObject team)
        {
            bool present=team!=null;string custom=FxDeskText.T((string)team?["name"]);ui.Text("teamName",present?(!string.IsNullOrEmpty(custom)?custom:SadeCardText.TeamName((string)team["id"])):Loc.T("Bölgede ekip yok"));ui.Text("teamSub",present?FxDeskText.S(team,"sub",SadeCardText.TeamSub((string)team["city"],FxDeskText.T((string)team["statusKey"]))):"");
            ui.Text("teamLetter",present?(string)team["id"]:"");ui.Text("raidName",present&&team["raidSuccess"]?.Type!=JTokenType.Null?Loc.F("BASKIN {0} [B]",DeskFormat.Percent(FxDeskText.N(team,"raidSuccess"),0)):FxDeskText.S(team,"actionLabel",Loc.T("Baskın")));ui["raid"].Action.Bind(present?"regionAction":"openDrawer",present?FxDeskText.S(team,"command","raid"):"teams",!present||team["raidEnabled"]==null||(bool?)team["raidEnabled"]==true);
        }
        public void Preview(string action,JObject preview)
        {
            for(int n=0;n<4;n++)ui["action."+n].Action.SetPreview(ui["action."+n].Action.argument==action);
            if(preview==null){ui.Active("previewCheck",false);ui.Text("previewCost",Loc.T("Etki ve maliyet için kaynak verisi gerekli."));ui.Text("previewResult","");return;}
            int days=(int?)preview["days"]??0;float cost=FxDeskText.N(preview,"costFund"),power=FxDeskText.N(preview,"costPower");
            ui.Text("previewCost",days>0?Loc.F("{0} gün · {1} fon",days,FxDeskText.Num(cost)):power>0?Loc.F("hemen · {0} SG",FxDeskText.Num(power)):Loc.T("hemen"));
            ui.Active("previewCheck",days>0);ui.Text("previewResult",days>0?Loc.T("ONAYLANIR"):Loc.T("HAZIR"));
        }
    }
}
