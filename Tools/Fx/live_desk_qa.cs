if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play gerekli.");
var surf=UnityEngine.Object.FindFirstObjectByType<Corrigible.Fx.FxLiveDeskSurface>();var src=surf.source;var game=src.Game;var fx=surf.interaction;var cmd=UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.SimDeskCommands>();
var checks=new System.Collections.Generic.List<object>();int failed=0;void Check(string name,bool ok,object detail=null){checks.Add(new{name,ok,detail});if(!ok)failed++;}
string folder="Docs/UI_Fx/captures/Live";System.IO.Directory.CreateDirectory(folder);
void Flush(){game.Flush();src.Flush();fx.panels.CompleteAnimations();UnityEngine.Canvas.ForceUpdateCanvases();}
string State()=>Newtonsoft.Json.JsonConvert.SerializeObject(game.Sim.S);
bool ReplayEquals(){game.Save();var record=UnityEngine.JsonUtility.FromJson<Corrigible.Core.GameRecord>(UnityEngine.JsonUtility.ToJson(game.Sim.Record));var restored=Corrigible.Core.Replay.Restore(record);return State()==Newtonsoft.Json.JsonConvert.SerializeObject(restored.S);}
void Capture(string name)=>Corrigible.Fx.EditorTools.FxCaptureTools.Capture(folder+"/"+name+".png");
try{
game.SetSpeed(0);game.NewGame("manipulator",731u);Flush();Check("giriş-zorluk-menüsü",Corrigible.Desk.IntroView.Instance.IsOpen&&src.State.IntroOpen);
fx.Select("cn");fx.Command("researchStart","anomali_1");Check("giriş-arkası-komut-kilitli",fx.State.SelectedRegion==null&&game.Sim.Record.commands.Count==0);Capture("intro-tr");
string[] rivals={"solucan","manipulator","kahin"};for(int n=0;n<3;n++){Corrigible.Desk.IntroView.Instance.Choose(n);Flush();Check("zorluk-"+n,game.TypeId==rivals[n]&&Corrigible.Desk.IntroView.Instance.IsOpen);}
game.SetOnboardingOff(true);game.SetEventsEnabled(false);game.SetExperimental(true,1.5);game.NewGame("solucan",731u);Flush();Corrigible.Desk.IntroView.Instance.Begin();Flush();Check("göreve-başla-1x",!Corrigible.Desk.IntroView.Instance.IsOpen&&game.Speed==1);fx.SetSpeed(3);Flush();Check("4x",game.Speed==4);fx.SetSpeed(1);Flush();Check("1x-geri",game.Speed==1&&fx.Model.Clock.Speed=="1x");fx.SetSpeed(0);Flush();
var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
foreach(var region in fx.map.regions){fx.ClearSelection();region.Node.OnPointerClick(pointer);Flush();Check("bölge-tıklama-"+region.Id,fx.State.SelectedRegion==region.Id&&fx.panels.card.IsOpen&&fx.Data.GameData.Cards.ContainsKey(region.Id));var shown=game.Sim.Shown(game.Sim.S.Region(region.Id));Check("algı-sınırı-"+region.Id,fx.Model.Map.Regions.First(r=>r.Id==region.Id).Value==(shown.V.HasValue?(double?)UnityEngine.Mathf.RoundToInt((float)shown.V.Value):null));}
fx.Select("weu");Flush();Check("bölgesel-fon-katkısı",surf.regionTools[0].finance.text==src.Current.Dossier.Finance.Contribution+"\n"+src.Current.Dossier.Finance.Note);Capture("region-tr");
string before=State();fx.LoadSample(8);fx.SetIllusion(!fx.Illusion);Check("canlı-örnek-kısayolları-kapalı",fx.IsLive&&before==State());
fx.OpenDrawer("research");fx.Command("researchSelect","anomali_1");Flush();var action=fx.panels.Drawer("research").ui["detailAction"].Action;bool available=action.interactable;fx.Command(action.command,action.argument);Flush();Check("peşin-geliştirme-anında-ve-kayıtlı",available&&game.Sim.Record.commands.Last().kind=="operation"&&game.Sim.Record.commands.Last().a=="anomali_1");
fx.Command("reserve","30");Flush();Check("rezerv-gerçek-komut",game.Sim.S.ResearchReserve==30&&game.Sim.Record.commands.Last().kind=="reserve");Check("operasyon-replay",ReplayEquals());Capture("research-tr");
string languageBefore=State();Corrigible.Desk.DeskLoc.Instance.SetLanguage(Corrigible.Desk.DeskLang.En);Flush();Check("dil-değişimi-oyunu-değiştirmez",State()==languageBefore);Capture("research-en");Corrigible.Desk.DeskLoc.Instance.SetLanguage(Corrigible.Desk.DeskLang.Tr);Flush();
fx.Command("closeLarge");fx.ClearSelection();fx.Escape();Check("duraklatma-menüsü",fx.State.Pause);fx.Command("settings");Check("ayarlar-açılır",Corrigible.Desk.SettingsView.Instance.IsOpen);fx.Select("cn");Check("ayarlar-arkası-kilit",fx.State.SelectedRegion==null);Corrigible.Desk.SettingsView.Instance.Close();fx.Command("resume");Flush();
// Baskın senaryosu temiz bir gerçek komut kaydıyla başlar; test oyun durumu elle değiştirilmez.
game.NewGame("solucan",731u);Flush();Corrigible.Desk.IntroView.Instance.Begin();game.SetSpeed(0);Flush();
string regionId=game.Sim.S.Origin;fx.Select(regionId);Flush();var dispatch=surf.regionTools[0].teams.FirstOrDefault(t=>t.gameObject.activeSelf&&t.interactable);if(dispatch!=null)fx.Command(dispatch.command,dispatch.argument);Flush();
for(int n=0;n<8;n++){game.Sim.Step();Flush();}fx.RegionAction("monitorNet");Flush();Check("izleme-komutu-kayıtta",game.Sim.Record.commands.Any(c=>c.kind=="act"&&c.a=="izleme"));
int collected=0;for(int n=0;n<100&&game.Sim.RaidCheck(regionId)!=null&&game.Sim.S.Ending==null;n++){
if(game.Sim.S.PendingGift!=null)game.ResolveGift(Corrigible.Core.Simulation.GiftReject);if(game.Sim.S.PendingEvent!=null)game.ResolveEvent(0);game.Sim.Step();Flush();src.State.VeilOpen=false;src.Flush();
foreach(var report in fx.map.reports)if(report.gameObject.activeSelf&&report.collect.interactable){int count=game.Sim.Record.commands.Count;report.collect.onClick.Invoke();Flush();if(game.Sim.Record.commands.Count>count)collected++;}
}
Check("rapor-fonu-gerçek-komut",collected>0&&game.Sim.Record.commands.Any(c=>c.kind=="report"),collected);Check("baskın-kuralları-sağlandı",game.Sim.RaidCheck(regionId)==null,game.Sim.RaidCheck(regionId));Check("strateji-replay",ReplayEquals());
fx.Select(regionId);Flush();fx.RegionAction("raid");Flush();Check("baskın-emri-modal",src.State.RaidBrief==regionId&&Corrigible.Desk.EventModalView.Instance.IsDoc&&fx.Blocked);Capture("raid-brief-tr");Corrigible.Desk.EventModalView.Instance.PressDoc(true);Flush();Check("baskın-parametreleri-kayıtta",game.Sim.S.Raid!=null&&game.Sim.Record.commands.Last().kind=="raid");game.Save();System.IO.File.WriteAllText("Temp/fx-live-pending.json",UnityEngine.JsonUtility.ToJson(game.Sim.Record));Check("bekleyen-baskın-replay",ReplayEquals());
}catch(System.Exception e){Check("exception",false,e.ToString());}
var result=new{total=checks.Count,failed,checks};System.IO.File.WriteAllText(folder+"/desk-qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{total=checks.Count,failed};
