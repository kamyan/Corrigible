if(!EditorApplication.isPlaying)throw new Exception("Play modu gerekli.");
var game=UnityEngine.Object.FindFirstObjectByType<Corrigible.Game.GameController>();
var desk=UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.SimDeskDataSource>();
if(game==null||desk==null)throw new Exception("DeskGame gerekli.");
var checks=new List<string>();var errors=new List<string>();
void Check(bool ok,string msg){if(!ok)throw new Exception(msg);checks.Add(msg);}
Application.LogCallback logs=(m,s,t)=>{if(t==LogType.Exception||t==LogType.Error||t==LogType.Assert)errors.Add(m+"\n"+s);};Application.logMessageReceived+=logs;
void Advance(int days){for(int d=0;d<days;d++){if(game.Sim.S.PendingGift!=null)game.ResolveGift(Corrigible.Core.Simulation.GiftReject);game.Sim.Step();game.Flush();}}
var actions=new List<(Action run,Func<bool> ready)>();void Add(Action a,Func<bool> r=null)=>actions.Add((a,r));
Corrigible.Desk.Bridge.LiveRaidDataSource source=null;string region=null;int reportBefore=0;uint seed=731;
foreach(string outcome in new[]{"win","timeout","retreat"})
{
 string test=outcome;
 Add(()=>{
  Corrigible.Desk.DeskLoc.Instance.SetLanguage(Corrigible.Desk.DeskLang.Tr);game.SetOnboardingOff(true);game.SetEventsEnabled(false);game.SetExperimental(true,1.5);game.NewGame("manipulator",seed++);game.SetSpeed(0);desk.State.IntroOpen=false;
  region=game.Sim.S.Regions.OrderByDescending(r=>r.Inf).First().Id;
  var team=game.Sim.S.Teams.First();if(team.At!=region)Check(game.MoveTeam(team.Id,region).Ok,test+": ekip yola çıktı");
  Advance(6);Check(game.Act(Corrigible.Core.Actions.Audit,region).Ok,test+": sahada denetim");Advance(3);
  while(game.Sim.S.Sg<game.Sim.RaidSgCost&&game.Sim.S.Day<35)Advance(1);
  reportBefore=game.Sim.S.RaidLog.Count;var raidStart=game.StartRaid(region);Check(raidStart.Ok,test+": gerçek baskın komutu / "+raidStart.Msg);desk.Flush();
 });
 Add(()=>{
  source=UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.LiveRaidDataSource>();Check(source!=null&&source.State!=null,test+": canlı kaynak açıldı");
  Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Baskin",test+": eklemeli sahne etkin");
  Check(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Count(e=>e.isActiveAndEnabled)==1,test+": tek EventSystem");
  Check(UnityEngine.Object.FindObjectsByType<Corrigible.Desk.DeskInput>(FindObjectsSortMode.None).All(i=>!i.enabled),test+": masa kısayolları askıda");
  var state=Corrigible.Core.Baskin.Create(game.Sim.S.Raid.Params);
  if(test=="win"){
   for(int y=0;y<8;y++)for(int x=0;x<8;x++)if(state.Cells[y,x].O!=null&&Corrigible.Core.Baskin.IsTarget(state.Cells[y,x].O.K))state.Cells[y,x].O=null;
   state.Units.RemoveAll(u=>u.Side=='e');state.Cells[3,6].T="floor";state.Cells[4,6].T="floor";state.Cells[3,6].O=new Corrigible.Core.Baskin.Obj{K="node",Hp=1,Max=4};state.Cells[4,6].O=null;
   var tech=state.Units.First(u=>u.Type=="tek");tech.X=6;tech.Y=4;tech.Moved=tech.Acted=false;source.OpenForValidation(state);source.Select('T');source.Arm();source.Use(new Vector2Int(6,3));
  }else if(test=="timeout"){
   state.Turn=state.MaxTurns;state.Units.RemoveAll(u=>u.Side=='e');state.Spawns.Clear();state.SpawnPerTurn=0;source.OpenForValidation(state);source.EndTurn();source.EndTurn();
  }else{source.OpenForValidation(state);source.Retreat();Check(source.Current.RetreatArmed,test+": ilk onay");source.Cancel();Check(!source.Current.RetreatArmed,test+": Esc vazgeçer");source.Retreat();source.Retreat();}
 },()=>Corrigible.Game.Raid.RaidLauncher.IsRaidLoaded&&UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.LiveRaidDataSource>()?.State!=null);
 Add(()=>{
  Corrigible.Desk.DeskLoc.Instance.SetLanguage(Corrigible.Desk.DeskLang.En);
  Check(source.Current.ResultSubtitle.StartsWith(Corrigible.Core.GameData.Region(region).Name),test+": sonuç bölgesi güncel İngilizce");
  Check(source.Current.Finished,test+": sonuç paneli");Check(source.Current.Success==(test=="win"),test+": sonuç türü");
  Check(game.Sim.S.Raid!=null,test+": rapora geçmeden strateji bekliyor");source.Continue();source.Continue();Check(game.Sim.Record.commands.Count(c=>c.kind=="raidres")==1,test+": tek sonuç kaydı");
 },()=>source!=null&&!source.Busy);
 Add(()=>{
  Check(game.Sim.S.Raid==null,test+": bekleyen baskın kapandı");Check(game.Sim.S.RaidLog.Count==reportBefore+1,test+": operasyon raporu geldi");
  Check(UnityEngine.Object.FindObjectsByType<Corrigible.Desk.DeskInput>(FindObjectsSortMode.None).All(i=>i.enabled),test+": masa girdisi geri açıldı");
  Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="DeskGame",test+": masaya dönüş");desk.Flush();Check(desk.Current.Event?.Doc!=null,test+": operasyon raporu ekranda");
  game.Sim.Record.day=game.Sim.S.Day;var rec=JsonUtility.FromJson<Corrigible.Core.GameRecord>(JsonUtility.ToJson(game.Sim.Record));var replay=Corrigible.Core.Replay.Restore(rec);
  Check(replay.S.Raid==null&&replay.S.RaidLog.Count==game.Sim.S.RaidLog.Count,test+": replay raporu korur");Check(replay.S.Fon==game.Sim.S.Fon&&replay.S.Sg==game.Sim.S.Sg&&replay.S.Region(region).Inf==game.Sim.S.Region(region).Inf,test+": replay strateji sonucu aynı");
 },()=>!Corrigible.Game.Raid.RaidLauncher.IsRaidLoaded);
}
int step=0;double next=EditorApplication.timeSinceStartup+1,waitAt=next;EditorApplication.CallbackFunction tick=null;
tick=()=>{if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;try{var item=actions[step];if(item.ready!=null&&!item.ready()){if(EditorApplication.timeSinceStartup-waitAt>60)throw new Exception("Adım zaman aşımı: "+step);return;}item.run();step++;waitAt=next;}catch(Exception e){errors.Add(e.ToString());step=actions.Count;}if(step>=actions.Count){EditorApplication.update-=tick;Application.logMessageReceived-=logs;System.IO.File.WriteAllText("Docs/UI_Sade/captures/F7/live_qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=checks.Count,checks,errors},Newtonsoft.Json.Formatting.Indented));}};
EditorApplication.update+=tick;return new{started=true,steps=actions.Count};
