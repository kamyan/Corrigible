if(!EditorApplication.isPlaying)throw new Exception("Play gerekli.");
const string dir="Docs/UI_Sade/captures/F7";System.IO.Directory.CreateDirectory(dir);
var errors=new List<string>();var report=new System.Text.StringBuilder();int checkedTexts=0,cases=0;var captures=new List<string>();
void Inspect(string name){cases++;errors.AddRange(new string[0]);int count;int n=Corrigible.Desk.EditorTools.SadeTextChecker.CheckCurrent(name,report,out count);checkedTexts+=count;if(n>0)errors.Add(name+": "+n+" metin hatası");}
string DeskShot(string name,int width,int height,bool save){string path=dir+"/"+name+".png";
            System.IO.Directory.CreateDirectory(dir);
            path ??= System.IO.Path.Combine(dir, $"{name}_{DateTime.Now:yyyy-MM-dd_HHmmss}.png");

            var cam = Camera.main;
            GameObject temp = null;
            if (cam == null)
            {
                temp = new GameObject("_DeskCaptureCam") { hideFlags = HideFlags.HideAndDontSave };
                cam = temp.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.orthographic = true;
            }

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var saved = new List<(Canvas, RenderMode, Camera, float)>();
            foreach (var c in canvases)
            {
                if (!c.isRootCanvas || !c.isActiveAndEnabled) continue;
                saved.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                // Sıralama: yüksek sortingOrder kameraya yakın
                c.planeDistance = 100 - Mathf.Clamp(c.sortingOrder, 0, 1000) * 0.09f;
            }
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            foreach (var c in canvases) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
            // Kamera canvas'ının yeni piksel dikdörtgeni ilk çizimde kesinleşir. Konuma bağlı
            // katmanları bundan sonra yenile; aksi hâlde ilk en-boy yakalaması eski ölçeği kullanır.
            cam.Render();
            // Sade harita katmanları ve sabit boylu işaretler, geçici yakalama boyunu aynı karede izlesin.
            foreach (var fitter in UnityEngine.Object.FindObjectsByType<Corrigible.Desk.MapCoverFitter>(FindObjectsSortMode.None)) fitter.Refresh();
            foreach (var interaction in UnityEngine.Object.FindObjectsByType<Corrigible.Desk.SadeDeskInteraction>(FindObjectsSortMode.None)) interaction.RefreshLayout();
            foreach (var popup in UnityEngine.Object.FindObjectsByType<Corrigible.Desk.PopupCardBase>(FindObjectsSortMode.None))
                if (popup.IsOpen || popup.IsAnimating) popup.CompleteAnimation();
            foreach (var interaction in UnityEngine.Object.FindObjectsByType<Corrigible.Desk.SadeDeskInteraction>(FindObjectsSortMode.None)) interaction.RefreshLayout();
            // Geçişler bitmiş hâliyle yakalansın (olay modalının açılışı)
            foreach (var m in UnityEngine.Object.FindObjectsByType<Corrigible.Desk.EventModalView>(FindObjectsSortMode.None)) m.CompleteAnimation();
            foreach(var layer in UnityEngine.Object.FindObjectsByType<Corrigible.Desk.SadeMapTextLayer>(FindObjectsSortMode.None))layer.Refresh();
            // Konuma bağlı görünümler (seçim ipliği) yeni çözünürlükte yeniden hesaplansın
            foreach (var t in UnityEngine.Object.FindObjectsByType<Corrigible.Desk.SelectionThreadView>(FindObjectsSortMode.None)) t.Refresh();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            Inspect(name);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            if(save)System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());

            cam.targetTexture = prevTarget;
            foreach (var (c, mode, wc, pd) in saved) { c.renderMode = mode; c.worldCamera = wc; c.planeDistance = pd; }
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            if (temp != null) UnityEngine.Object.DestroyImmediate(temp);
            Canvas.ForceUpdateCanvases();
            return System.IO.Path.GetFullPath(path);
}
void RaidShot(string name,int width,int height,bool save)
{
 var board=UnityEngine.Object.FindFirstObjectByType<Corrigible.Game.Raid.RaidBoard3D>();var cam=board.Camera;var framing=UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.SadeRaidCameraFraming>();var background=UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.BoardBackgroundView>();
 var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var previous=cam.targetTexture;
 var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas).ToArray();
 var settings=canvases.Select(c=>(c,mode:c.renderMode,c.worldCamera,c.planeDistance,c.scaleFactor,scaler:c.GetComponent<UnityEngine.UI.CanvasScaler>(),enabled:c.GetComponent<UnityEngine.UI.CanvasScaler>()?.enabled??false)).ToArray();
 Texture2D texture=null;
 try
 {
  cam.targetTexture=rt;
  foreach(var item in settings){if(item.scaler!=null)item.scaler.enabled=false;item.c.scaleFactor=Mathf.Min(width/1920f,height/1080f);if(item.mode==RenderMode.ScreenSpaceOverlay){item.c.renderMode=RenderMode.ScreenSpaceCamera;item.c.worldCamera=cam;item.c.planeDistance=.5f;}}
  Canvas.ForceUpdateCanvases();framing.Refresh();cam.Render();
  typeof(Corrigible.Game.Raid.RaidBoard3D).GetMethod("LayoutLabels",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(board,null);
  UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.RaidPreviewOverlay>()?.Refresh();UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.RaidOutcomeSpot>()?.Refresh();background.Refresh(true);Canvas.ForceUpdateCanvases();cam.Render();
  var active=RenderTexture.active;RenderTexture.active=rt;texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();RenderTexture.active=active;
  Inspect(name);
  string path=dir+"/"+name+".png";if(save){System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());captures.Add(path);}

 }
 finally
 {
  cam.targetTexture=previous;foreach(var item in settings){item.c.renderMode=item.mode;item.c.worldCamera=item.worldCamera;item.c.planeDistance=item.planeDistance;item.c.scaleFactor=item.scaleFactor;if(item.scaler!=null)item.scaler.enabled=item.enabled;}
  if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);Canvas.ForceUpdateCanvases();framing.Refresh();background.Refresh(true);
 }
}
var steps=new List<Action>();
foreach(bool isRaid in new[]{false,true})
{
 bool raid=isRaid;
 steps.Add(()=>UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(raid?"Assets/Corrigible/Desk/Scenes/RaidBoard_Sandbox.unity":"Assets/Corrigible/Desk/Scenes/SadeDesk_Sandbox.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)));
 steps.Add(()=>{});steps.Add(()=>{});
 foreach(var language in new[]{Corrigible.Desk.DeskLang.Tr,Corrigible.Desk.DeskLang.En})
 for(int i=1;i<=9;i++)
 {
  int screen=i;var lang=language;
  steps.Add(()=>{Corrigible.Desk.DeskLoc.Instance.SetLanguage(lang);if(raid)UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.MockRaidDataSource>().LoadPreset(screen);else UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.MockSadeDeskDataSource>().LoadSample(screen);});
  foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1920,1200),new Vector2Int(2560,1440),new Vector2Int(3440,1440)})
  {var dimensions=size;steps.Add(()=>{string name=(raid?"Baskin":"Masa")+screen+"_"+lang+"_"+dimensions.x+"x"+dimensions.y;bool save=dimensions.y==1080||lang==Corrigible.Desk.DeskLang.En&&(screen==2||screen==6||screen==9);if(raid)RaidShot(name,dimensions.x,dimensions.y,save);else{DeskShot(name,dimensions.x,dimensions.y,save);if(save)captures.Add(dir+"/"+name+".png");}});}
 }
}
int step=0;double next=EditorApplication.timeSinceStartup+.4;EditorApplication.CallbackFunction tick=null;
tick=()=>{if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.2;try{steps[step++]();}catch(Exception e){errors.Add("Adım "+step+": "+e);step=steps.Count;}if(step>=steps.Count){EditorApplication.update-=tick;System.IO.File.WriteAllText(dir+"/matrix_saved_done.txt","Tamam");System.IO.File.WriteAllText(dir+"/text_report.txt",report.ToString());System.IO.File.WriteAllText(dir+"/matrix_qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{cases,checkedTexts,errors,captures},Newtonsoft.Json.Formatting.Indented));}};
EditorApplication.update+=tick;return new{started=true,steps=steps.Count};
