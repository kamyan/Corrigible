using System;
using Corrigible.Core;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Corrigible.Fx
{
    /// <summary>FX masası: ortak SadeInteractionState, gösterim verisi ve tek komut sınırı.</summary>
    public sealed class FxDeskInteraction : FxTimedBehaviour
    {
        [SerializeField] public MockSadeDeskDataSource source;
        [SerializeField] public MockSadeDeskCommands commands;
        [SerializeField] public FxMapView map;
        [SerializeField] public FxDeskHud hud;
        [SerializeField] public FxDeskPanels panels;
        [SerializeField] public FxMapClickTarget sea;
        [SerializeField] public FxMapClickTarget[] alerts;
        [SerializeField] public UnityEngine.UI.Button legendButton;
        [SerializeField] public FxKillSwitchView killSwitch;
        [SerializeField] public TextAsset mockPreviews;
        public SadeInteractionState State{get;private set;}=new SadeInteractionState();
        public FxDeskDataCache Data{get;}=new FxDeskDataCache();
        public SadeDeskViewModel Model{get;private set;}
        public Func<bool> ExternalBlock;
        public bool ExternalBlocked=>ExternalBlock?.Invoke()==true;
        public bool IsLive=>gameMode;
        public bool Blocked=>ExternalBlocked||State.Pause||State.Confirmation;
        public bool Illusion=>!gameMode&&source!=null?source.Illusion:gameIllusion;
        public event Action<string> DrawerRequested,RegionSelected;
        public event Action LegendRequested,GlobalSwitchRequested;
        IDeskCommands target;int revision=-1,lastSpeed=1;bool pausedBeforeMenu,gameIllusion,gameMode,rendering,logRead;
        string pendingHover,tipReason,pendingAction,pendingRegion;bool hoverHud;double hoverAt;
        JObject previews;
        string selectedResearch,newsFilter="all";
        public void ResetLive(){State=new SadeInteractionState();selectedResearch=null;newsFilter="all";logRead=false;pendingAction=pendingRegion=pendingHover=null;killSwitch.Cancel();panels.HideTips();}
        public void SyncSelection(string id){if(id==State.SelectedRegion)return;bool full=State.FullDossier;State.Select(id);if(full)State.OpenDossier();}
        public void ClosePause(){State.Pause=false;Render(false);}
        protected override void OnEnable()
        {
            base.OnEnable();if(source!=null)source.Changed+=Reload;DeskLoc.Instance.Changed+=LanguageChanged;map.cover.Changed+=RefreshLayout;
            foreach(var slot in map.regions){slot.Node.Clicked+=Select;slot.Node.Hovered+=NodeHovered;slot.HitArea.Clicked+=Select;}
            map.ReportClicked+=Report;foreach(var a in alerts)a.Clicked+=Select;sea.Clicked+=Sea;hud.time.SpeedClicked+=SetSpeed;
            foreach(var tab in hud.tabs)tab.Clicked+=ToggleDrawer;legendButton.onClick.AddListener(ToggleLegend);killSwitch.Requested+=GlobalRequest;
            previews=mockPreviews!=null?JObject.Parse(mockPreviews.text):new JObject();Reload();
        }
        protected override void OnDisable()
        {
            base.OnDisable();if(source!=null)source.Changed-=Reload;DeskLoc.Instance.Changed-=LanguageChanged;map.cover.Changed-=RefreshLayout;
            foreach(var slot in map.regions){slot.Node.Clicked-=Select;slot.Node.Hovered-=NodeHovered;slot.HitArea.Clicked-=Select;}
            map.ReportClicked-=Report;foreach(var a in alerts)a.Clicked-=Select;sea.Clicked-=Sea;hud.time.SpeedClicked-=SetSpeed;
            foreach(var tab in hud.tabs)tab.Clicked-=ToggleDrawer;legendButton.onClick.RemoveListener(ToggleLegend);killSwitch.Requested-=GlobalRequest;
        }
        void LanguageChanged(){if(gameMode||source==null)Render(false);}
        void Reload()
        {
            if(gameMode||source==null)return;bool reset=revision!=source.SampleRevision;
            if(reset){revision=source.SampleRevision;State=new SadeInteractionState();pendingAction=pendingRegion=pendingHover=null;Data.Load(source);logRead=false;if(panels!=null)panels.Drawer("guide").GuideTopic="aiStates";var s=source.Snapshot;
                if(s["regionCard"] is JObject card)State.Select((string)card["regionId"]);
                if(s["drawer"] is JObject drawer)State.OpenDrawer((string)drawer["id"]);
                if(s["dossier"] is JObject dossier){State.Select((string)dossier["regionId"]);State.OpenDossier();}
                State.Legend=(bool?)s["legend"]?["open"]==true;
                panels?.HideTips();killSwitch.Cancel();
            }
            Model=source.Current;Render(false);
            if(reset&&panels!=null){if(source.Snapshot["tooltip"] is JObject b)panels.ShowBreakdown((string)b["target"],false);if(source.Snapshot["nodeTooltip"] is JObject n)panels.ShowNode((string)n["regionId"],false);map.Highlight((string)source.Snapshot["drawer"]?["news"]?["mapHighlight"]);map.HighlightTeam((string)source.Snapshot["drawer"]?["teams"]?["tokenHighlight"]);}
        }
        public void Bind(SadeDeskViewModel vm,IDeskCommands gameCommands){Model=vm;target=gameCommands;Render(false);}
        public void BindGame(SadeDeskData data,IDeskCommands gameCommands,bool illusion=false)
        {
            gameMode=true;Model=data.View;target=gameCommands;gameIllusion=illusion;Data.Load(data);
            var research=Data.Drawer("research");if(selectedResearch!=null&&research!=null)ResearchCommand("researchSelect",selectedResearch);
            var news=Data.Drawer("news");if(news!=null)news["filter"]=newsFilter;
            Render(false);
        }
        public void Render(bool animate=true)
        {
            if(Model==null||rendering)return;rendering=true;
            try{foreach(var r in Model.Map.Regions)r.Selected=r.Id==State.SelectedRegion;
                if(logRead)foreach(var tab in Model.Dock.Tabs)if(tab.Id=="aiLog")tab.BadgeType=null;
                map.Bind(Model.Map);map.Select(State.SelectedRegion);hud.Bind(Model,State.Drawer);panels?.Render(animate);
            }finally{rendering=false;}
        }
        public void RefreshLayout()=>panels?.RefreshLayout();
        void HideTips(){pendingHover=null;panels?.HideTips();}
        public void Select(string id)
        {
            if(Blocked||map.Find(id)==null)return;
            if(State.AssigningTeam!=null){Send("AssignTeam",State.AssigningTeam+":"+id);target?.Run("Team:Send:"+ResolveTeam(State.AssigningTeam)+":"+id);State.AssigningTeam=null;}
            HideTips();State.Select(id);target?.SelectRegion(id);Send("SelectRegion",id);RegionSelected?.Invoke(id);Render();
        }
        void Sea(string id)=>ClearSelection();
        void Report(string command){if(!Blocked)target?.Run(command);}
        public void ClearSelection(){if(Blocked)return;HideTips();State.ClearSelection();target?.SelectRegion(null);Send("ClearSelection");Render();}
        public void ToggleDrawer(string id)
        {
            if(Blocked||!Data.Drawers.ContainsKey(id))return;HideTips();State.ToggleDrawer(id);
            if(id=="aiLog"&&State.Drawer!=null)logRead=true;Send("ToggleWindow",id);target?.ToggleWindow(Window(id));DrawerRequested?.Invoke(State.Drawer);Render();
        }
        public void OpenDrawer(string id){if(Blocked||!Data.Drawers.ContainsKey(id))return;HideTips();State.OpenDrawer(id);if(id=="aiLog")logRead=true;target?.ToggleWindow(Window(id));Render();}
        static WindowId Window(string id)=>id=="news"?WindowId.News:id=="teams"?WindowId.Teams:id=="aiLog"?WindowId.AiLog:id=="guide"?WindowId.Guide:WindowId.Research;
        public void ToggleLegend(){if(Blocked)return;State.Legend=!State.Legend;Send("ToggleLegend");LegendRequested?.Invoke();Render();}
        void GlobalRequest(){if(Blocked||Illusion)return;HideTips();pendingAction="global";pendingRegion=null;State.Confirmation=true;panels?.ConfirmText(true);Send("GlobalKillSwitchRequested");GlobalSwitchRequested?.Invoke();Render();}
        public void Confirm()
        {
            if(ExternalBlocked||!State.Confirmation||pendingAction==null)return;string action=pendingAction,region=pendingRegion;pendingAction=pendingRegion=null;State.Confirmation=false;killSwitch.Cancel();
            if(action=="global"){target?.ConfirmGlobalKillSwitch();Send("GlobalKillSwitch");}else{target?.ExecuteRegionAction(region,3);Send("RegionAction",region+":kilit");}Render();
        }
        void CancelConfirmation(){pendingAction=pendingRegion=null;State.Confirmation=false;killSwitch.Cancel();Render();}
        public void Escape()
        {
            if(ExternalBlocked||Model==null)return;bool wasPaused=State.Pause;var layer=State.Escape();
            if(layer==SadeEscapeLayer.Confirmation){pendingAction=pendingRegion=null;killSwitch.Cancel();}
            if(layer==SadeEscapeLayer.Tooltip)HideTips();
            if(layer==SadeEscapeLayer.LargePanel){map.Highlight(null);map.HighlightTeam(null);}
            if(layer==SadeEscapeLayer.Pause){if(!wasPaused)pausedBeforeMenu=Model.Clock.Paused;Model.Clock.Paused=State.Pause||pausedBeforeMenu;if(!pausedBeforeMenu)target?.TogglePause();}
            Send("Escape",layer.ToString());Render();
        }
        public void SetSpeed(int index)
        {
            if(Blocked||Model==null)return;index=Mathf.Clamp(index,0,3);if(index>0)lastSpeed=index;
            Model.Clock.Paused=index==0;Model.Clock.Speed=index==0?"pause":index==3?"4x":index==2?"2x":"1x";
            if(!gameMode&&source!=null){source.Snapshot["clock"]["paused"]=index==0;source.Snapshot["clock"]["speed"]=index<2?1:index==2?2:4;}
            target?.SetSpeed((GameSpeed)index);Send(index==0?"Pause":"SetSpeed",index.ToString());hud.time.Bind(Model.Clock);
        }
        public void SelectNext(int direction){int index=-1;for(int n=0;n<map.regions.Length;n++)if(map.regions[n].Id==State.SelectedRegion)index=n;if(index<0)index=direction<0?0:-1;Select(map.regions[(index+direction+map.regions.Length)%map.regions.Length].Id);}
        public void RegionAction(string id)
        {
            if(Blocked||State.SelectedRegion==null)return;id=SadeCardText.ActionId(id);
            if(id!=null&&id.StartsWith("sendTeam:",StringComparison.Ordinal)){target?.Run("Team:Send:"+id.Substring(9)+":"+State.SelectedRegion);return;}
            if(id=="recover"||id=="diplomacy"){target?.Run((id=="recover"?"Economy:Recover:":"Economy:Diplomacy:")+State.SelectedRegion);return;}var card=Data.Card(Model,State.SelectedRegion,Illusion);JToken selected=null;
            foreach(var action in card["actions"]??new JArray())if((string)action["id"]==id)selected=action;
            if(id!="raid"&&(selected==null||FxDeskText.B(selected,"disabled")||(bool?)selected["enabled"]==false||!string.IsNullOrEmpty((string)selected["disabledReason"])))return;
            if(id=="raid"){target?.LaunchFieldOperation(State.SelectedRegion);Send("LaunchFieldOperation",State.SelectedRegion);return;}
            if(id=="killSwitch"){HideTips();pendingAction=id;pendingRegion=State.SelectedRegion;State.Confirmation=true;panels?.ConfirmText(false);Render();return;}
            int index=id=="monitorNet"?0:id=="audit"?1:2;target?.ExecuteRegionAction(State.SelectedRegion,index);Send("RegionAction",State.SelectedRegion+":"+SadeCardText.Command(id));
        }
        public void PreviewAction(string id)
        {
            if(State.SelectedRegion==null)return;id=SadeCardText.ActionId(id);var card=Data.Card(Model,State.SelectedRegion,Illusion);JObject preview=null;
            foreach(var a in card["actions"]??new JArray())if((string)a["id"]==id)preview=a["preview"] as JObject;
            if(preview==null&&target==null)preview=previews?[id] as JObject;
            panels?.Preview(id,preview);
        }
        void Send(string command,string arg=null)=>commands?.Run(command,arg);
        void NodeHovered(string id,bool on){if(on)Schedule(id,false);else if(pendingHover==id||panels?.TooltipRegion==id)HideTips();}
        void Schedule(string id,bool hud){if(Blocked)return;HideTips();pendingHover=id;hoverHud=hud;hoverAt=clock.Now+.4;}
        public void HoverCommand(FxDeskCommandTarget action,bool on)
        {
            if(action.command=="breakdown"){if(on)Schedule(action.argument,true);else if(pendingHover==action.argument||panels?.TooltipHud==action.argument)HideTips();return;}
            if(!string.IsNullOrEmpty(action.region))map.Highlight(on?action.region:null);
            if(!string.IsNullOrEmpty(action.team))map.HighlightTeam(on?action.team:null);
            if(on&&action.command=="regionAction"){PreviewAction(action.argument);if(!string.IsNullOrEmpty(action.hoverTip))panels?.cardView.DisabledPreview(action.hoverTip);}
        }
        public void Command(string command,string argument=null)
        {
            if(ExternalBlocked)return;
            if(command=="confirm"){Confirm();return;}if(command=="cancel"){CancelConfirmation();return;}
            if(command=="reduced"){clock.Reduced=!clock.Reduced;PlayerPrefs.SetInt(FxDebugMenu.ReducedKey,clock.Reduced?1:0);PlayerPrefs.Save();return;}
            if(command=="settings"){SettingsView.Instance?.Open();return;}
            if(command=="menu"){target?.ReturnToMainMenu();return;}
            if(command=="restart"){target?.Restart();return;}
            if(command=="resume"){if(State.Pause){State.Pause=false;Model.Clock.Paused=pausedBeforeMenu;if(!pausedBeforeMenu)target?.TogglePause();Render();}return;}
            if(Blocked)return;
            if(command=="select")Select(argument);else if(command=="closeCard")ClearSelection();
            else if(command=="closeLarge"){State.CloseLargePanel();map.Highlight(null);map.HighlightTeam(null);Render();}
            else if(command=="legend")ToggleLegend();else if(command=="regionAction")RegionAction(argument);
            else if(command=="fullDossier"){if(State.SelectedRegion!=null){HideTips();State.OpenDossier();Render();}}
            else if(command=="openDrawer")OpenDrawer(argument);
            else if(command=="newsFilter"){newsFilter=argument;var d=Data.Drawer("news");if(d!=null)d["filter"]=argument;Render(false);}
            else if(command=="newsSelect"||command=="newsAudit")NewsCommand(command,argument);
            else if(command=="logNews"){OpenDrawer("news");var items=Data.Drawer("news")?["items"] as JArray;if(items!=null)for(int n=0;n<items.Count;n++)if((string)items[n]["id"]==argument)panels?.Drawer("news")?.FocusNews(n);}
            else if(command=="assignTeam"){State.AssigningTeam=argument;Send(command,argument);}
            else if(command=="raid"){foreach(var t in Model.Map.Teams)if(t.Id==argument&&t.Region!=null){Select(t.Region);RegionAction("raid");break;}}
            else if(command=="sendTeam"){if(State.SelectedRegion!=null)target?.Run("Team:Send:"+argument+":"+State.SelectedRegion);}
            else if(command=="run")target?.Run(argument);
            else if(command=="settings"){SettingsView.Instance?.Open();}
            else if(command=="restart")target?.Restart();
            else if(command=="menu")target?.ReturnToMainMenu();
            else if(command=="teamHire"){target?.Run("Team:Hire");Send(command);}
            else if(command=="researchSelect"||command=="researchStart"||command=="researchPause"){ResearchCommand(command,argument);if(gameMode&&command=="researchSelect")panels.Drawer("research").scroll.verticalNormalizedPosition=0;}
            else if(command=="researchSpeed"||command=="reserve")
            {
                var option=Data.Drawer("research")?[command=="reserve"?"reserve":"speed"] as JObject;
                if(option==null)return;float value=float.Parse(argument,System.Globalization.CultureInfo.InvariantCulture);option["selected"]=value;
                if(command=="reserve"&&option["options"] is JArray choices)for(int n=0;n<choices.Count;n++){float v=choices[n].Type==JTokenType.Object?FxDeskText.N(choices[n],"value"):(float)choices[n];if(Mathf.Approximately(v,value)){target?.SetEmergencyFund(n);break;}}
                Send(command,argument);Render(false);
            }
            else if(command=="guideTopic"||command=="guidePage"){var guide=panels?.Drawer("guide");if(guide!=null){int index=command=="guidePage"?Mathf.Clamp(int.Parse(argument),0,6):Array.IndexOf(FxGuideCopy.Ids,argument);guide.GuideTopic=FxGuideCopy.Ids[Mathf.Clamp(index,0,6)];Render(false);}}
        }
        public void FocusNews(string id){var items=Data.Drawer("news")?["items"] as JArray;if(items!=null)for(int n=0;n<items.Count;n++)if((string)items[n]["id"]==id)panels.Drawer("news").FocusNews(n);}
        void NewsCommand(string command,string id)
        {
            foreach(var row in Data.Drawer("news")?["items"]??new JArray())if((string)row["id"]==id){if(command=="newsAudit"&&!FxDeskText.B(row,"canAudit"))return;Select((string)row["region"]);if(command=="newsAudit"){target?.Run("Verify:"+id);PreviewAction("audit");}Send(command,id);break;}
        }
        void ResearchCommand(string command,string id)
        {
            selectedResearch=id;var data=Data.Drawer("research");if(data==null)return;foreach(var branch in data["branches"]??new JArray())
            {
                var nodes=branch["nodes"] as JArray;if(nodes==null)continue;
                for(int n=0;n<nodes.Count;n++){var node=nodes[n];bool selected=(string)node["id"]==id;node["selected"]=selected;if(!selected)continue;string state=(string)node["state"];
                    if(command=="researchStart"&&(state=="available"||state=="paused")){target?.StartResearch(id);Send(command,id);if(target==null)node["state"]=state="running";}
                    if(command=="researchPause"&&state=="running"){target?.PauseResearch(id);Send(command,id);if(target==null)node["state"]=state="paused";}
                    var detail=node["detail"] as JObject;if(detail==null&&(string)data["detail"]?["nodeId"]==id){detail=(JObject)data["detail"].DeepClone();detail["state"]=state;detail["canPause"]=state=="running";}data["detail"]=detail!=null?detail.DeepClone():new JObject{["nodeId"]=id,["name"]=node["name"],["branchKey"]=branch["nameKey"],["step"]=Loc.F("{0}/{1}",n+1,nodes.Count),["progress"]=node["progress"]??0,["daysLeft"]=node["daysLeft"]??node["days"]??0,["costPerDay"]=node["costPerDay"]??0,["unlocks"]=n+1<nodes.Count?nodes[n+1]["name"]:"—",["state"]=state,["requirement"]=node["requirement"],["canPause"]=state=="running"};
                }
            }
            if(target==null){int active=0;float daily=0;foreach(var branch in data["branches"]??new JArray())foreach(var node in branch["nodes"]??new JArray())if((string)node["state"]=="running"){active++;daily+=FxDeskText.N(node,"costPerDay");}data["activeCount"]=active;data["costPerDay"]=daily;}
            Render(false);
        }
        string ResolveTeam(string id){foreach(var row in Data.Drawer("teams")?["teams"]??new JArray())if((string)row["id"]==id)return (string)row["teamId"]??id;return id;}
        public void LoadSample(int number){if(gameMode)return;gameMode=false;target=null;if(source!=null)source.LoadSample(number);}
        public void SetIllusion(bool value){if(gameMode)return;HideTips();if(!gameMode&&source!=null)source.SetIllusion(value);else{gameIllusion=value;Render(false);}}
        public override void Tick(FxClock visualClock)
        {
            if(pendingHover!=null&&!Blocked&&clock.Now>=hoverAt){string id=pendingHover;pendingHover=null;if(hoverHud)panels?.ShowBreakdown(id);else panels?.ShowNode(id);}
            var k=Keyboard.current;if(k==null||Model==null||ExternalBlocked)return;
            if(gameMode&&DeskTutorialView.Instance!=null&&DeskTutorialView.Instance.HasNext&&(k.enterKey.wasPressedThisFrame||k.escapeKey.wasPressedThisFrame))return;
            if(k.escapeKey.wasPressedThisFrame){Escape();return;}
            if(State.Confirmation){if(k.enterKey.wasPressedThisFrame||k.numpadEnterKey.wasPressedThisFrame)Confirm();return;}if(Blocked)return;
            if(k.rKey.wasPressedThisFrame)ToggleDrawer("research");if(k.nKey.wasPressedThisFrame)ToggleDrawer("news");if(k.tKey.wasPressedThisFrame)ToggleDrawer("teams");if(k.lKey.wasPressedThisFrame)ToggleDrawer("aiLog");if(k.hKey.wasPressedThisFrame)ToggleDrawer("guide");if(k.tabKey.wasPressedThisFrame)SelectNext(k.shiftKey.isPressed?-1:1);
            if(k.digit1Key.wasPressedThisFrame||k.numpad1Key.wasPressedThisFrame)RegionAction("monitorNet");if(k.digit2Key.wasPressedThisFrame||k.numpad2Key.wasPressedThisFrame)RegionAction("audit");if(k.digit3Key.wasPressedThisFrame||k.numpad3Key.wasPressedThisFrame)RegionAction("quarantine");if(k.digit4Key.wasPressedThisFrame||k.numpad4Key.wasPressedThisFrame)RegionAction("killSwitch");if(k.bKey.wasPressedThisFrame)RegionAction("raid");
            if(k.spaceKey.wasPressedThisFrame)SetSpeed(Model.Clock.Paused?lastSpeed:0);int speed=Model.Clock.Paused?0:Model.Clock.Speed=="4x"?3:Model.Clock.Speed=="2x"?2:1;
            if(k.equalsKey.wasPressedThisFrame||k.numpadPlusKey.wasPressedThisFrame)SetSpeed(speed+1);if(k.minusKey.wasPressedThisFrame||k.numpadMinusKey.wasPressedThisFrame)SetSpeed(speed-1);
#if UNITY_EDITOR
            if(!gameMode&&!FxDebugMenu.FunctionKeysOwned)for(int n=0;n<9;n++)if(k[(Key)((int)Key.F1+n)].wasPressedThisFrame)LoadSample(n+1);
            if(!gameMode&&k.iKey.wasPressedThisFrame)SetIllusion(!Illusion);
#endif
        }
    }
}
