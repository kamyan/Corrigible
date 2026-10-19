using System.Linq;
using System.Collections.Generic;
using Corrigible.Desk;
using Corrigible.Desk.Bridge;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>S1–S9/B3/B4/B7b örnekleri; ana oyunun kaydını kullanmaz.</summary>
    public sealed class FxRaidSource : FxRaidDataSource, IRaidPreviewSource
    {
        [SerializeField] public TextAsset boardStates;
        [SerializeField] public TextAsset[] screenStates;
        [SerializeField] int initialScreen=1;
        protected override void Start()=>LoadPreset(initialScreen);
        protected override void Finish(){ContinueCount++;LoadPreset(1);}
        public int ContinueCount {get;private set;}
        int screen=1;
        public override int Screen=>screen;
        public JObject Raw {get;private set;}
        public override JObject FixtureRaw=>fixtureHud!=null||Screen==0?Raw:null;
        public void LoadPreset(int screen)
        {
            this.screen=screen;MachineRunning=false;PresentedEffect=null;var data=JObject.Parse(boardStates.text);var raw=JObject.Parse(screenStates[screen-1].text);Raw=raw;
            if(screen==9)raw["outcome"]="win";
            data["states"]["screen"]=raw;var snapshot=MockRaidBoardDataSource.Parse(data,"screen");
            snapshot.State.Params.RegionName=Loc.T("Batı Avrupa");OpenState(snapshot.State);fixtureIntents=snapshot.IntentStates;fixtureHud=raw["hud"] as JObject;
            State.Collateral=(int?)raw["civilianBroken"]??0;civilianCount=(int?)raw["civilianTotal"]??3;
            // Kırılmış hedefler de başlangıç hedef formunda kalır.
            if(raw["targets"] is JArray targets)
            {
                targetForms.Clear();foreach(var t in targets){string kind=(string)t["id"];var pos=kind=="backup"?(6,6):(6,3);targetForms.Add((pos.Item1,pos.Item2,kind,(int)t["max"]));}
            }
            foreach(var team in fixtureHud?["team"] as JArray??new JArray())
            {
                var u=State.Units.FirstOrDefault(x=>x.Side=='p'&&Baskin.UnitTypes[x.Type].Short==(string)team["letter"]);if(u==null)continue;
                u.Moved=(bool?)team["move"]==false||(string)team["status"]=="done";u.Acted=team["ability"]?.Type==JTokenType.Boolean&&!(bool)team["ability"]||(string)team["status"]=="done";
                if((bool?)team["selected"]==true){Selected=((string)team["letter"])[0];Armed=(string)team["ability"]=="armed";}
            }
            if(screen==2){Selected='T';Armed=true;State.Units.First(u=>u.Type=="tek").Moved=true;hover=new Vector2Int(4,6);hoverVisible=true;}
            if(screen==7){Selected='T';Armed=true;hover=new Vector2Int(6,3);hoverVisible=true;}
            confirm=(string)fixtureHud?["end"]=="confirm";retreat=(bool?)fixtureHud?["topButtons"]?["retreatArmed"]==true;panel=(string)fixtureHud?["topButtons"]?["open"];
            unread=(bool?)fixtureHud?["topButtons"]?["logUnread"]??true;Publish();
        }
        public void LoadBoardPreset(string id)
        {
            screen=0;MachineRunning=false;PresentedEffect=null;Raw=JObject.Parse(boardStates.text)["states"][id] as JObject;var data=JObject.Parse(boardStates.text);var snapshot=MockRaidBoardDataSource.Parse(data,id);OpenState(snapshot.State);fixtureIntents=snapshot.IntentStates;
            if(id=="B3"){Selected='E';Armed=true;hover=new Vector2Int(6,3);hoverVisible=true;}
            Publish();
        }
        protected override void ApplyFixture(RaidViewModel vm)
        {
            base.ApplyFixture(vm);if(MachineRunning)return;
            if(fixtureHud==null){vm.Queue=Baskin.Intents(State).Select(x=>new RaidQueueItem{Order=x.Order,State="pending"}).ToArray();return;}
            if(fixtureHud["eventSlip"] is JObject slip)
            {
                vm.EventVisible=true;vm.Queue=slip["queue"].Select(x=>new RaidQueueItem{Order=(int)x["order"],State=(string)x["state"]}).ToArray();
                vm.EventLine=Loc.F("№{0} · {1} → {2} −1",(int)slip["line"]["order"],Loc.T("Güvenlik dronu kuzeye ateş ediyor"),RaidText.Get((string)slip["line"]["targetKey"]));
            }
            if(fixtureHud["logDrawer"]?["lines"] is JArray lines)vm.Log=lines.Select(x=>new RaidLogModel{Kind=(string)x[0],Text=RaidText.MockLine((string)x[1])}).ToArray();
            if(fixtureHud["endConfirm"] is JObject c){vm.Unused=new[]{new RaidUnusedModel{Letter='A',Name=RaidText.Get("raid.unit.A"),Move=true,Ability=true,ThreatOrder=(int?)c["threat"]?["robotOrder"]??0}};vm.Team[2].Warn=true;}
        }

    }
}
