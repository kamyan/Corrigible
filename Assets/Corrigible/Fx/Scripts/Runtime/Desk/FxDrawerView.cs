using System;
using Corrigible.Core;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Önceden yazılmış çekmece slotları. JSON yalnız görünür veri ve komut kimliği taşır.</summary>
    public sealed class FxDrawerView : MonoBehaviour
    {
        [SerializeField] public string drawerId;
        [SerializeField] public FxPanelBindings ui;
        [SerializeField] public FxTheme theme;
        [SerializeField] public ScrollRect scroll;
        [SerializeField] public RectTransform content;
        [SerializeField] public FxGuideAiStatesPage aiPage;
        [SerializeField] public FxTypewriter typewriter;
        [SerializeField] public int nodeCapacity=3;
        [SerializeField] public int newsCapacity=12;
        [SerializeField] public bool liveLayout;
        public string GuideTopic{get;set;}="aiStates";
        public void Bind(JObject data)
        {
            if(data==null)return;
            ui.Text("windowTitle",Loc.T(drawerId=="research"?Loc.N("ARAŞTIRMA"):drawerId=="news"?Loc.N("HABERLER"):drawerId=="teams"?Loc.N("EKİPLER"):drawerId=="aiLog"?Loc.N("YZ GÜNLÜĞÜ"):Loc.N("REHBER")));
            var title=ui["windowTitle"].Text;ui["headerKey"].Rect.anchoredPosition=new Vector2(43+title.GetPreferredValues(title.text).x+14,-17);
            if(drawerId=="research")Research(data);else if(drawerId=="news")News(data);else if(drawerId=="teams")Teams(data);else if(drawerId=="aiLog")Log(data);else Guide(data);
        }
        void Research(JObject data)
        {
            ui.Text("summary",Loc.F("<b>{0} proje</b> sürüyor",(int?)data["activeCount"]??0));ui.Text("dailyCost",Loc.F("{0} fon/gün",FxDeskText.Num(FxDeskText.N(data,"costPerDay"),1)));
            bool reserve=data["reserve"]!=null;ui.Text("speedTitle",reserve?Loc.T("FON"):Loc.T("HIZ"));
            var options=data[reserve?"reserve":"speed"]?["options"] as JArray;
            for(int n=0;n<3;n++){var o=options!=null&&n<options.Count?options[n]:null;ui.Active("speed."+n,o!=null);if(o==null)continue;float value=o.Type==JTokenType.Object?FxDeskText.N(o,reserve?"value":"mult"):(float)o;ui.Text("speedValue."+n,reserve?Loc.F("{0} fon",FxDeskText.Num(value)):Loc.F("×{0}",FxDeskText.Num(value,value%1==0?0:1)));ui.Text("speedSub."+n,reserve?Loc.T("acil durum fonu"):value==1?Loc.T("normal"):Loc.F("+{0} fon/gün",FxDeskText.Num(FxDeskText.N(o,"extraPerDay"),1)));var action=ui["speed."+n].Action;action.Bind(reserve?"reserve":"researchSpeed",value.ToString(System.Globalization.CultureInfo.InvariantCulture));action.SetPreview(Mathf.Approximately(value,FxDeskText.N(data[reserve?"reserve":"speed"],"selected",reserve?15:1)));}
            var branches=data["branches"] as JArray;int count=branches?.Count??0;
            for(int b=0;b<6;b++)
            {
                bool active=b<count;ui.Active("branch."+b,active);if(!active)continue;var branch=branches[b];ui.Text("branchTitle."+b,FxDeskText.T((string)branch["nameKey"]));ui.Text("branchDone."+b,Loc.F("{0}/{1} bitti",(int?)branch["done"]??0,(int?)branch["total"]??0));ui.Text("branchGoal."+b,branch["goalPercent"]?.Type==JTokenType.Null?"":DeskFormat.Percent((float?)branch["goalPercent"]??0,0));
                var nodes=branch["nodes"] as JArray;for(int n=0;n<nodeCapacity;n++)
                {
                    string k=b+"."+n;var node=nodes!=null&&n<nodes.Count?nodes[n]:null;ui.Active("node."+k,node!=null);if(node==null)continue;
                    string state=(string)node["state"],id=(string)node["id"];bool running=state=="running",done=state=="done",locked=state=="locked";var color=theme.Color(locked?"tx3":done?"mt":"cy");
                    ui.Tint("nodeEdge."+k,color);ui.Text("nodeTitle."+k,FxDeskText.T((string)node["name"]));ui.Tint("nodeTitle."+k,theme.Color(locked?"tx3":"tx"));
                    ui.Active("nodeCheck."+k,done);ui.Text("nodeValue."+k,done?"":locked?"":running?FxDeskText.Num(FxDeskText.N(node,"progress")*100):"+");ui.Active("nodeLock."+k,locked);ui.Active("nodeProgress."+k,running);ui["nodeProgress."+k].Image.fillAmount=FxDeskText.N(node,"progress");ui.Active("nodeSelected."+k,FxDeskText.B(node,"selected"));
                    ui.Text("nodeDays."+k,done?Loc.T("bitti"):locked?SadeResearchDrawerView.RequirementText(node["requirement"]):(string)node["statusText"]??Loc.F("{0} gün",(int?)node["daysLeft"]??(int?)node["days"]??0));ui["node."+k].Action.Bind("researchSelect",id);
                }
            }
            var detail=data["detail"];if(detail!=null){string state=FxDeskText.S(detail,"state",FxDeskText.B(detail,"canPause")?"running":"available");ui.Text("detailName",FxDeskText.T((string)detail["name"]));ui.Text("detailBranch",Loc.F("{0} · {1}",FxDeskText.T((string)detail["branchKey"]),(string)detail["step"]));ui.Text("detailPercent",DeskFormat.Percent(FxDeskText.N(detail,"progress")*100));ui["detailProgress"].Image.fillAmount=FxDeskText.N(detail,"progress");ui.Text("detailDays",state=="locked"?SadeResearchDrawerView.RequirementText(detail["requirement"]):state=="done"?Loc.T("bitti"):(string)detail["dayText"]??Loc.F("{0} gün kaldı",(int?)detail["daysLeft"]??0));ui.Text("detailCost",detail["costText"]!=null?FxDeskText.T((string)detail["costText"]):Loc.F("{0} fon/gün",FxDeskText.Num(FxDeskText.N(detail,"costPerDay"),1)));ui.Text("detailUnlock",FxDeskText.T((string)detail["unlocks"]));ui.Text("detailButton",(string)detail["buttonLabel"]??Loc.T(state=="running"?Loc.N("Duraklat"):state=="done"?Loc.N("Tamamlandı"):Loc.N("Başlat")));ui["detailAction"].Action.Bind(state=="running"?"researchPause":"researchStart",(string)detail["nodeId"],state!="done"&&state!="locked"&&string.IsNullOrEmpty((string)detail["disabledReason"]));}
            float extra=Mathf.Max(0,count-4)*96;ui["detail"].Rect.anchoredPosition=new Vector2(0,-480-extra);content.sizeDelta=new Vector2(content.sizeDelta.x,480+extra+ui["detail"].Rect.rect.height);
        }
        void News(JObject data)
        {
            string filter=FxDeskText.S(data,"filter","all");var items=data["items"] as JArray;ui.Text("filterAll",Loc.F("Tümü {0}",(int?)data["counts"]?["all"]??items?.Count??0));ui.Text("filterUnverified",Loc.F("Doğrulanmamış {0}",(int?)data["counts"]?["unverified"]??0));ui.Text("stamp",(string)data["stamp"]);ui["all"].Action.SetPreview(filter=="all");ui["unverified"].Action.SetPreview(filter!="all");
            float y=42;string lastDay=null;for(int n=0;n<newsCapacity;n++)
            {
                var row=items!=null&&n<items.Count?items[n]:null;string trust=(string)row?["trust"];bool active=row!=null&&(filter=="all"||trust=="report"||trust=="suspicious"||trust=="unknown");ui.Active("row."+n,active);if(!active)continue;
                string day=(string)row["day"];bool newDay=day!=lastDay;ui.Text("day."+n,newDay?day:"");if(newDay)y+=24;lastDay=day;ui["row."+n].Rect.anchoredPosition=new Vector2(0,-y);ui.Text("source."+n,Loc.F("{0} · {1}",FxDeskText.T((string)row["source"]),(string)row["time"]));ui.Text("headline."+n,FxDeskText.T((string)row["headline"]));ui["headline."+n].Text.fontStyle=trust=="fake"?FontStyles.Strikethrough:FontStyles.Normal;
                if(liveLayout)ui["headline."+n].Text.ForceMeshUpdate(true);
                float headlineHeight=liveLayout?Mathf.Max(29,Mathf.Ceil(ui["headline."+n].Text.GetPreferredValues(ui["headline."+n].Text.text,438,float.PositiveInfinity).y)):29;
                if(liveLayout){ui["headline."+n].Rect.sizeDelta=new Vector2(438,headlineHeight);float extra=headlineHeight-29;void Shift(string prefix,float top){var rect=ui[prefix+n].Rect;rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-top-extra);}Shift("trust.",65);Shift("audit.",64);Shift("fake.",70);ui["row."+n].Rect.sizeDelta=new Vector2(ui["row."+n].Rect.sizeDelta.x,103+extra);}ui.Text("trust."+n,FxDeskText.Trust(trust));theme.Apply(ui["trustMark."+n].Image,trust=="verified"?"fx_tr_ok":trust=="suspicious"?"fx_tr_sus":trust=="unknown"?"fx_tr_unk":"fx_tr_rap",theme.Color(trust=="verified"?"mt":trust=="fake"?"rd":trust=="report"?"vi":"am"));ui.Tint("trust."+n,theme.Color(trust=="fake"?"rd":trust=="verified"?"mt":trust=="report"?"vi":"am"));ui.Text("regionName."+n,FxDeskText.T("region.short."+(string)row["region"]));ui["row."+n].Action.Bind("newsSelect",(string)row["id"]);ui["row."+n].Action.region=(string)row["region"];ui["region."+n].Action.Bind("select",(string)row["region"]);ui["region."+n].Action.region=(string)row["region"];ui.Active("audit."+n,FxDeskText.B(row,"canAudit"));ui["audit."+n].Action.Bind("newsAudit",(string)row["id"]);ui.Active("fake."+n,trust=="fake");y+=111+(liveLayout?headlineHeight-29:0);
            }content.sizeDelta=new Vector2(content.sizeDelta.x,y+8);
        }
        void Teams(JObject data)
        {
            var summary=data["summary"];ui.Text("summary",Loc.F("{0} ekip · {1} hazır · {2} yolda",(int?)summary?["total"]??0,(int?)summary?["ready"]??0,(int?)summary?["travelling"]??0));ui.Text("hireName",(string)data["newTeamText"]??Loc.F("yeni ekip: {0} fon",(int?)data["newTeamCost"]??0));ui["hire"].Action.Bind("teamHire",null,data["canHire"]==null||(bool?)data["canHire"]==true);var teams=data["teams"] as JArray;float y=liveLayout?76:38;
            for(int n=0;n<8;n++){var t=teams!=null&&n<teams.Count?teams[n]:null;ui.Active("row."+n,t!=null);if(t==null)continue;ui["row."+n].Rect.anchoredPosition=new Vector2(0,-y);string id=(string)t["id"];bool travelling=(string)t["status"]=="travelling";ui.Text("name."+n,SadeCardText.TeamName(id));ui.Text("location."+n,FxDeskText.T((string)t["location"]));ui.Text("status."+n,travelling?Loc.T("YOLDA"):Loc.T("HAZIR"));ui["row."+n].Action.team=id;
                var members=t["members"] as JArray;for(int m=0;m<3;m++){var a=members!=null&&m<members.Count?members[m]:null;ui.Text("member."+n+"."+m,(string)a?["letter"]);int hp=(int?)a?["hp"]??0,max=(int?)a?["max"]??0;for(int h=0;h<3;h++){var pip=ui["pip."+n+"."+m+"."+h].Image;pip.enabled=h<max;pip.color=theme.Color(h<hp?"mt":"tx3");}}
                ui.Active("route."+n,travelling);ui.Active("action."+n,!travelling);if(travelling){var route=t["route"];ui["routeProgress."+n].Image.fillAmount=FxDeskText.N(route,"progress");ui.Text("routeName."+n,Loc.F("{0} · {1} gün · {2}",FxDeskText.T((string)route["from"]),(int?)route["daysLeft"]??0,FxDeskText.T((string)route["to"])));}
                else{var action=t["action"];bool raid=(string)action?["kind"]=="raid";ui.Text("actionName."+n,raid?Loc.F("BASKIN {0} [B]",DeskFormat.Percent((float?)action?["success"]??0)):Loc.T("Görevlendir ›"));ui["action."+n].Action.Bind(raid?"raid":"assignTeam",id);ui.Text("actionSub."+n,raid?FxDeskText.T((string)action["site"]):Loc.T("Haritada hedef seç"));}y+=122;
            }content.sizeDelta=new Vector2(content.sizeDelta.x,y);
        }
        void Log(JObject data)
        {
            ui.Text("source",FxDeskText.T((string)data["source"]));var entries=data["entries"] as JArray;float y=30;
            for(int n=0;n<12;n++){var row=entries!=null&&n<entries.Count?entries[n]:null;ui.Active("row."+n,row!=null);if(row==null)continue;bool isNew=FxDeskText.B(row,"new");ui["row."+n].Rect.anchoredPosition=new Vector2(0,-y);ui.Text("stamp."+n,(string)row["stamp"]);ui.Active("new."+n,isNew);var text=FxDeskText.T((string)row["text"]);bool redact=text.Contains("[[KARARTI]]");ui.Text("body."+n,text.Replace("[[KARARTI]]","             "));ui.Active("redact."+n,redact);ui.Text("note."+n,FxDeskText.T((string)row["analystNote"]));string reg=(string)row["region"];ui.Text("regionName."+n,reg!=null?FxDeskText.T("region.short."+reg):(string)row["link"]?["kind"]=="news"?Loc.T("HABER · SAHTE"):Loc.T("ÇEKİRDEK"));var action=ui["region."+n].Action;action.region=reg;action.Bind(reg!=null?"select":(string)row["link"]?["kind"]=="news"?"logNews":"none",reg??(string)row["link"]?["id"]);y+=isNew?96:72;}
            ui.Active("warning",FxDeskText.B(data,"deceptionWarning"));ui["warning"].Rect.anchoredPosition=new Vector2(0,-y-10);content.sizeDelta=new Vector2(content.sizeDelta.x,y+62);
        }
        void Guide(JObject data)
        {
            int page=Array.IndexOf(FxGuideCopy.Ids,GuideTopic);if(page<0)page=0;bool states=page==0;
            for(int n=0;n<7;n++){ui.Text("topicName."+n,Loc.T(FxGuideCopy.Names[n]));ui["topic."+n].Action.SetPreview(n==page);}
            ui.Text("heading",DeskText.Upper(Loc.T(FxGuideCopy.Names[page])));ui.Text("page",Loc.F("{0} / {1}",page+1,7));ui["previous"].Action.Bind("guidePage",(page-1).ToString(),page>0);ui["next"].Action.Bind("guidePage",(page+1).ToString(),page<6);ui.Active("aiStates",states);ui.Active("help",!states);
            if(states){ui.Text("subtitle",Loc.T("Haritadaki küre, o bölgedeki YZ sızmasını gösterir. Değer arttıkça küre akıcı biçimde hâl değiştirir."));for(int n=0;n<3;n++){ui.Text("stateTitle."+n,FxDeskText.State(n==0?12:n==1?50:92));ui.Text("stateBody."+n,Loc.T(FxGuideCopy.StateDescriptions[n]));ui["stateRange."+n].Rect.anchoredPosition=new Vector2(280,-8);}return;}
            ui.Text("subtitle",page==1?Loc.T("Her sayının yanındaki işaret, o sayıya ne kadar güvenebileceğini söyler."):"");var rows=FxGuideCopy.Rows(GuideTopic);string[] trusts={"verified","report","suspicious","unknown","fake"};
            for(int n=0;n<5;n++){bool active=page==1||n<rows.Length;ui.Active("helpRow."+n,active);if(!active)continue;ui.Text("helpTitle."+n,page==1?FxDeskText.Trust(trusts[n]):Loc.T(rows[n].Title));ui.Text("helpBody."+n,page==1?Loc.T(SadeGuideDrawerView.Body(trusts[n])):Loc.T(rows[n].Body));}
        }
        public void FocusNews(int index){if(scroll!=null&&index>=0&&index<newsCapacity)scroll.verticalNormalizedPosition=Mathf.Clamp01(1+ui["row."+index].Rect.anchoredPosition.y/Mathf.Max(1,content.rect.height-scroll.viewport.rect.height));}
    }
}
