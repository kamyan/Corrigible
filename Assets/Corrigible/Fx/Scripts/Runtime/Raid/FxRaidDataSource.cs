using System.Linq;
using System.Collections.Generic;
using Corrigible.Core;
using Corrigible.Desk;
using Corrigible.Desk.Bridge;
using Newtonsoft.Json.Linq;
namespace Corrigible.Fx
{
    /// <summary>Canlı ve örnek baskının ortak FX sunumu. Makineler gerçek durumu bir kez çözer, görüntü kopyasını canlandırır.</summary>
    public abstract class FxRaidDataSource : RaidDataSource
    {
        public virtual int Screen => 0;
        public virtual JObject FixtureRaw => null;
        public IReadOnlyDictionary<int,string> IntentStates => fixtureIntents;
        public Baskin.PreviewResult Preview => preview;
        public void RefreshPresentation()=>Publish();
        public void ConfigureTheme(SadeTheme value){theme=value;animateActions=false;}
        protected override void ApplyFixture(RaidViewModel vm)
        {
            if(MachineRunning){vm.EventVisible=true;vm.EventLine=machineLine;vm.Queue=machineRoster.Select(x=>new RaidQueueItem{Order=x.Order,State=fixtureIntents.TryGetValue(x.Unit,out var v)?v:"pending"}).ToArray();}
            else vm.Queue=Baskin.Intents(State).Select(x=>new RaidQueueItem{Order=x.Order,State="pending"}).ToArray();
        }
        public bool MachineRunning {get;protected set;}
        public Baskin.Fx PresentedEffect {get;protected set;}
        Baskin.BState machineFinal;
        protected List<Baskin.IntentView> machineRoster;
        List<Baskin.Fx> machineEffects;
        int effectIndex;double nextEffect;protected string machineLine;
        public void EndWithFx(double now)
        {
            if(State==null||MachineRunning)return;EndTurn();if(State.Phase!="enemy"||MachineRunning)return;
            State.Phase="player";var display=State.Clone();machineRoster=Baskin.Intents(State);
            if(!Baskin.EndTurn(State)){Publish();return;}
            machineFinal=State;machineEffects=new List<Baskin.Fx>(State.FxList);State=display;State.Phase="enemy";
            Selected='\0';Armed=false;confirm=retreat=false;hover=null;preview=null;fixtureHud=null;fixtureIntents.Clear();
            foreach(var item in machineRoster)fixtureIntents[item.Unit]="pending";
            effectIndex=0;nextEffect=now;MachineRunning=true;machineLine=Loc.T("niyetler sırayla uygulanıyor");Publish();
        }
        public void TickMachines(double now)
        {
            if(!MachineRunning||now<nextEffect)return;AdvanceMachines(now);
        }
        void AdvanceMachines(double now)
        {
            nextEffect=now+(fast?.08:.25);
            if(effectIndex>=machineEffects.Count){State=machineFinal;MachineRunning=false;PresentedEffect=null;fixtureIntents.Clear();Publish();return;}
            var fx=machineEffects[effectIndex++];PresentedEffect=fx;var unit=State.Units.FirstOrDefault(x=>x.Id==fx.Unit);
            if(fx.K=="attack"){
                foreach(var id in fixtureIntents.Keys.ToArray())if(fixtureIntents[id]=="now")fixtureIntents[id]="done";
                fixtureIntents[fx.Unit]="now";var intent=machineRoster.FirstOrDefault(x=>x.Unit==fx.Unit);
                machineLine=Loc.F("№{0} · saldırı → {1}",intent?.Order??0,Loc.T("hedef kare"));
            }
            if(fx.K=="dmg"){if(unit!=null)unit.Hp=System.Math.Max(0,unit.Hp-fx.N);else if(State.Cells[fx.Y,fx.X].O is Baskin.Obj o)o.Hp=System.Math.Max(0,o.Hp-fx.N);machineLine=Loc.F("{0} · −{1}",machineLine,fx.N);}
            if(fx.K=="die"&&unit!=null){unit.Dead=true;if(fixtureIntents.ContainsKey(unit.Id))fixtureIntents[unit.Id]="cancel";}
            if(fx.K=="break"){if(State.Cells[fx.Y,fx.X].O?.K=="civ")State.Collateral++;State.Cells[fx.Y,fx.X].O=null;}
            if(fx.K=="heal"&&State.Cells[fx.Y,fx.X].O is Baskin.Obj healed){healed.Hp=System.Math.Min(healed.Max,healed.Hp+1);machineLine=Loc.T("Hedef dolabı onarıldı.");}
            if(fx.K=="push"&&unit!=null){var final=machineFinal.Units.FirstOrDefault(x=>x.Id==unit.Id);if(final!=null){unit.X=final.X;unit.Y=final.Y;}}
            if(fx.K=="spawn"){var born=machineFinal.Units.FirstOrDefault(x=>x.X==fx.X&&x.Y==fx.Y&&!State.Units.Any(u=>u.Id==x.Id));if(born!=null)State.Units.Add(born.Clone());machineLine=Loc.T("Yeni bir robot uyandı.");}
            Publish();
        }
    }
}
