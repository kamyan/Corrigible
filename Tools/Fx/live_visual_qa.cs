var surf=UnityEngine.Object.FindFirstObjectByType<Corrigible.Fx.FxLiveDeskSurface>();var src=surf.source;var game=src.Game;var fx=surf.interaction;game.ContinueSaved();game.SetSpeed(0);src.State.VeilOpen=false;src.State.RaidLogSeen=game.Sim.S.RaidLog.Count;src.Flush();
string folder="Docs/UI_Fx/captures/Live";var report=new System.Text.StringBuilder();var checks=new System.Collections.Generic.List<object>();int errors=0,texts=0,captures=0,failed=0;
void Check(string name,bool ok,object detail=null){checks.Add(new{name,ok,detail});if(!ok)failed++;}
var camera=UnityEngine.Camera.main;var previous=camera.targetTexture;
try{
foreach(var lang in new[]{Corrigible.Desk.DeskLang.Tr,Corrigible.Desk.DeskLang.En}){
Corrigible.Desk.DeskLoc.Instance.SetLanguage(lang);src.Flush();
foreach(var state in new[]{"card","dossier","research","news","teams","aiLog","guide","pause"}){
fx.Command("resume");fx.Command("closeLarge");fx.ClearSelection();fx.Select(game.Sim.S.Origin);src.Flush();
if(state=="dossier")fx.Command("fullDossier");else if(state!="card"&&state!="pause")fx.OpenDrawer(state);if(state=="research")fx.Command("researchSelect","anomali_1");
if(state=="pause"){fx.ClearSelection();fx.Escape();}
foreach(var size in new[]{new[]{1920,1080},new[]{1920,1200},new[]{3440,1440}}){var rt=new UnityEngine.RenderTexture(size[0],size[1],24);camera.targetTexture=rt;string context=state+"-"+lang+"-"+size[0]+"x"+size[1];try{Corrigible.Fx.EditorTools.FxCaptureTools.Capture(folder+"/"+context+".png",size[0],size[1]);errors+=Corrigible.Fx.EditorTools.FxTextChecker.CheckCurrent(context,report,out int count);texts+=count;captures++;if(state=="card"){var rect=fx.panels.ContextRect(fx.panels.card.rect);Check("kart-ekranda-"+context,rect.yMax<=fx.panels.viewport.rect.height-95&&rect.yMin>=95);}}finally{camera.targetTexture=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
}}
fx.Command("resume");fx.Command("closeLarge");fx.ClearSelection();Corrigible.Desk.DeskLoc.Instance.SetLanguage(Corrigible.Desk.DeskLang.Tr);src.Flush();
// Overlay olay katmanı ile kamera canvas'ının aynı düğümü ekranda işaretlemesi.
var view=Corrigible.Desk.EventModalView.Instance;var method=typeof(Corrigible.Desk.EventModalView).GetMethod("SpotWorld",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);var destination=view.GetComponentInParent<UnityEngine.Canvas>();
foreach(var region in fx.map.regions){var actual=(UnityEngine.Vector3)method.Invoke(view,new object[]{region.Id,new float[]{0,0}});var screen=UnityEngine.RectTransformUtility.WorldToScreenPoint(camera,region.Node.transform.position);var projected=UnityEngine.RectTransformUtility.WorldToScreenPoint(destination.renderMode==UnityEngine.RenderMode.ScreenSpaceOverlay?null:destination.worldCamera,actual);Check("olay-işareti-"+region.Id,UnityEngine.Vector2.Distance(screen,projected)<1,new{x=projected.x,y=projected.y,expectedX=screen.x,expectedY=screen.y});}
}catch(System.Exception e){Check("exception",false,e.ToString());}finally{camera.targetTexture=previous;}
System.IO.File.WriteAllText(folder+"/text-check.txt",report.ToString());var result=new{captures,texts,errors,total=checks.Count,failed,checks};System.IO.File.WriteAllText(folder+"/visual-qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{captures,texts,errors,total=checks.Count,failed};
