var game=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaGameController>();
var ui=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaInterfaceView>();
var drawers=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaDrawerView>();
var type=typeof(Corrigible.Ayna.Game.AynaInterfaceView);var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
System.Func<string,UnityEngine.UI.Button> button=n=>(UnityEngine.UI.Button)type.GetField(n,flags).GetValue(ui);
ui.SelectRegion("weu");ui.QueueRegional(1);string queued=game.ExportSave();button("saveButton").onClick.Invoke();game.ClearQueue();button("loadButton").onClick.Invoke();bool exact=queued==game.ExportSave();
for(int tab=0;tab<5;tab++){drawers.Open(tab);Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A"+(tab==0?"8":tab==1?"9":tab==2?"10":tab==3?"Log":"Help")+".png");}
drawers.Close();
int guard=0;
while(game.Phase!=Corrigible.Ayna.Presentation.AynaPhase.Conflict && game.Phase!=Corrigible.Ayna.Presentation.AynaPhase.End && guard++<150)
{
 if(game.Phase==Corrigible.Ayna.Presentation.AynaPhase.Planning){game.ClearQueue();var plan=Corrigible.Ayna.Mind.Bots.PlanFactory.Build(game.Match.Simulation.View(Corrigible.Ayna.Core.Side.Player),Corrigible.Ayna.Mind.Bots.BotStyle.Balanced,0);foreach(var o in plan.Orders)game.Queue(o);game.LockPlan();Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A3.png");}
 else game.Continue();
}
if(game.Phase==Corrigible.Ayna.Presentation.AynaPhase.Conflict)
{
 Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A4.png");button("rosterButton").onClick.Invoke();
 var toggles=(UnityEngine.UI.Toggle[])type.GetField("rosterToggles",flags).GetValue(ui);
 for(int i=0;i<System.Math.Min(game.Screens.A5.Slots,game.Screens.A5.Available.Length);i++)toggles[i].isOn=true;
 Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A5.png");button("commandButton").onClick.Invoke();
 Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A6.png");
}
var result=new {SaveExact=exact,QueueCommands=game.Screens.A2.Queue.Length,game.Phase,Cycle=game.Screens.A2.Cycle,Board=game.Screens.A6!=null};
System.IO.File.WriteAllText("Docs/AYNA/UI/live_smoke.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
