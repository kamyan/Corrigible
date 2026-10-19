var b=UnityEngine.Object.FindFirstObjectByType<Corrigible.Fx.FxRaidBoard>();var c=b.clock;c.enabled=false;c.Freeze();c.Advance(UnityEngine.Time.unscaledTimeAsDouble);var checks=new System.Collections.Generic.List<object>();int failed=0;
void Check(string name,bool ok,object detail=null){checks.Add(new{name,ok,detail});if(!ok)failed++;}
var cam=b.boardCamera;var previous=cam.targetTexture;var rt=new UnityEngine.RenderTexture(1920,1080,24);cam.targetTexture=rt;UnityEngine.Canvas.ForceUpdateCanvases();cam.Render();
try{
Check("Play etkin",UnityEditor.EditorApplication.isPlaying);
Check("Tek görsel saat",UnityEngine.Object.FindObjectsByType<Corrigible.Fx.FxClock>(UnityEngine.FindObjectsSortMode.None).Length==1);
Check("Canlı kontrolcü yok",UnityEngine.Object.FindFirstObjectByType<Corrigible.Game.GameController>()==null);
Check("Işık yok",UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None).Length==0);
Check("Eski parça Update yok",UnityEngine.Object.FindObjectsByType<Corrigible.Game.Raid.RaidPiece>(UnityEngine.FindObjectsSortMode.None).Length==0);
Check("İşaret CanvasRenderer",b.marks.GetComponent<UnityEngine.CanvasRenderer>()!=null);
foreach(var id in new[]{"B1","B2","B3","B4","B5","B6","B7a","B7b","B8"}){
b.LoadPreset(id);c.Advance(c.Now);UnityEngine.Canvas.ForceUpdateCanvases();cam.Render();
int visible=0;foreach(var slot in b.units)if(slot.root.gameObject.activeSelf)visible++;
Check(id+" birim havuzu",visible==b.Snapshot.State.Units.Count,visible);Check(id+" RNG korunur",b.Snapshot.State.Rs==0);Check(id+" gösterilen küre",b.nodeOrb.level==(id=="B5"?64:id=="B6"||id.StartsWith("B7")?86:58),b.nodeOrb.level);
Check(id+" can etiketleri",System.Array.TrueForAll(b.units,s=>s.root.gameObject.activeSelf==s.health.gameObject.activeSelf));
Check(id+" referanslar",System.Array.TrueForAll(b.units,s=>s.healthBorder!=null&&s.flagGroup!=null&&s.body!=null));
if(id=="B4"){Check("Belirsizlik soru fişleri",System.Array.FindAll(b.units,x=>x.root.gameObject.activeSelf&&x.question.activeSelf).Length==2);Check("Aldatma iki belirsiz iki taranmış",b.DisplayIntents.FindAll(x=>x.Uncertain).Count==2&&b.DisplayIntents.FindAll(x=>x.IsScanned).Count==2);}
if(id=="B5"){Check("İşaretsiz niyetler gizli",b.DisplayIntents.Count==3);Check("Biten sıra üstü çizili",System.Array.FindAll(b.units,x=>x.root.gameObject.activeSelf&&x.strike.gameObject.activeSelf).Length==2);Check("Etkin sıra eşlendi",b.Snapshot.IntentStates.ContainsValue("current"));}
}
b.LoadPreset("B1");b.nodeOrb.RenderFixed(.025,100);var previousOrb=b.nodeOrb.Simulation;b.LoadPreset("B5");Check("Küre modeli düzey değişiminde korunur",System.Object.ReferenceEquals(previousOrb,b.nodeOrb.Simulation)&&b.nodeOrb.DisplayedLevel<64);
b.LoadPreset("B2");c.Advance(c.Now);UnityEngine.Canvas.ForceUpdateCanvases();Check("İşaret mesh çiziliyor",b.marks.canvasRenderer.GetMesh()!=null&&b.marks.canvasRenderer.GetMesh().vertexCount>0);
var groundMesh=b.marks.canvasRenderer.GetMesh();float groundError=0;foreach(var vertex in groundMesh.vertices)groundError=System.Math.Max(groundError,System.Math.Abs(b.marks.rectTransform.TransformPoint(vertex).y-.075f));Check("İşaretler dünya zemininde",groundError<.001f,groundError);
foreach(var size in new[]{new[]{1920,1080},new[]{1920,1200},new[]{2560,1440},new[]{3440,1440}}){
var target=new UnityEngine.RenderTexture(size[0],size[1],24);cam.targetTexture=target;UnityEngine.Canvas.ForceUpdateCanvases();cam.Render();b.RefreshProjection(true);b.Tick(c);UnityEngine.Canvas.ForceUpdateCanvases();float scale=System.Math.Min(size[0]/1920f,size[1]/1080f);float maximum=0;
foreach(var uv in new[]{new UnityEngine.Vector2(-.3f,-.3f),new UnityEngine.Vector2(8.3f,-.3f),new UnityEngine.Vector2(8.3f,8.3f),new UnityEngine.Vector2(-.3f,8.3f)}){var p=cam.WorldToScreenPoint(Corrigible.Fx.FxRaidBoard.Corner(uv.x,uv.y));float x=size[0]*.5f+(210+(uv.x+uv.y)*93.75f-960)*scale;float y=size[1]*.5f+(528+(uv.y-uv.x)*53.775f-540)*scale;maximum=System.Math.Max(maximum,System.Math.Max(System.Math.Abs(p.x-x),System.Math.Abs(size[1]-p.y-y)));}
Check("Kamera "+size[0]+"x"+size[1],maximum<4,maximum);
var top=System.Array.Find(b.cells,x=>x.x==6&&x.y==3).anchor-UnityEngine.Vector3.up*.12f;var local=b.Project(top);float offset=b.nodeOrb.image.rectTransform.anchoredPosition.y-local.y;Check("Küre +64px "+size[0],System.Math.Abs(offset-64)<.05,offset);
cam.targetTexture=rt;target.Release();UnityEngine.Object.DestroyImmediate(target);}
b.LoadPreset("B1");b.RefreshProjection(true);int refreshes=b.ProjectionRefreshes,builds=b.marks.MeshBuilds;c.Freeze();for(int i=0;i<20;i++)c.Advance(c.Now);UnityEngine.Canvas.ForceUpdateCanvases();int warmBuild=b.marks.MeshBuilds;for(int i=0;i<100;i++)c.Advance(c.Now);UnityEngine.Canvas.ForceUpdateCanvases();Check("Durağan izdüşüm önbelleği",b.ProjectionRefreshes==refreshes);Check("Durağan işaret mesh önbelleği",b.marks.MeshBuilds==warmBuild);
c.Reduced=true;c.Resume();c.Advance(c.Now+5);Check("Azaltılmış titreme",b.Flicker==1);Check("Azaltılmış giriş tamam",b.labelsGroup.alpha==1);var pos=b.nodeOrb.image.rectTransform.anchoredPosition;c.Advance(c.Now+1);Check("Azaltılmış küre sabit",pos==b.nodeOrb.image.rectTransform.anchoredPosition);c.Reduced=false;c.Freeze();Check("Küre bağı piksel ölçeği",b.tether.rectTransform.localScale==UnityEngine.Vector3.one);
foreach(var name in new[]{"FxEdgeLine","FxHoloFace","FxHoloGlass"}){int errors=0;foreach(var msg in UnityEditor.ShaderUtil.GetShaderMessages(UnityEngine.Shader.Find("Corrigible/"+name)))if(msg.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)errors++;Check(name+" GPU derleme",errors==0);}
}finally{cam.targetTexture=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);c.Reduced=false;b.LoadPreset("B1");c.Freeze();c.Advance(c.Now);}
var result=new{passed=checks.Count-failed,failed,checks};System.IO.Directory.CreateDirectory("Docs/UI_Fx/captures/F5");System.IO.File.WriteAllText("Docs/UI_Fx/captures/F5/play-qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
