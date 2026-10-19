using System;
using System.IO;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx.Tests
{
    public sealed class FxPanelTests
    {
        [TestCase(1920,1080)] [TestCase(1920,1200)] [TestCase(2560,1440)] [TestCase(3440,1440)]
        public void CardAndTips_StayInsideViewportAndAvoidSelectedNode(int width,int height)
        {
            var viewport=new Vector2(width,height);var spec=JObject.Parse(File.ReadAllText("Assets/Corrigible/Fx/Data/map_fx.json"));
            float scale=Mathf.Max(width/1920f,height/1080f);var crop=(viewport-new Vector2(1920,1080)*scale)*.5f;
            foreach(var region in spec["regions"])
            {
                var node=new Vector2((float)region["node"][0],(float)region["node"][1])*scale+crop;float radius=(float)region["orbRadius"];
                foreach(bool drawer in new[]{false,true})
                {
                    var obstacle=drawer?new Rect(16,height-652-76,500,652):default;
                    var label=new Rect(node.x-95,node.y+radius+22,190,22);
                    var pos=FxPanelPlacement.Card(node,radius,new Vector2(372,510),viewport,obstacle,label);var rect=new Rect(pos,new Vector2(372,510));
                    Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(16));Assert.That(rect.xMax,Is.LessThanOrEqualTo(width-16));Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(96));Assert.That(rect.yMax,Is.LessThanOrEqualTo(height-96));
                    Assert.That(rect.Overlaps(new Rect(node.x-radius*1.15f,node.y-radius*1.15f,radius*2.3f,radius*2.3f)),Is.False,region["id"].ToString());Assert.That(rect.Overlaps(label),Is.False);if(drawer)Assert.That(rect.Overlaps(obstacle),Is.False);
                }
                var tip=FxPanelPlacement.NodeTip(node,radius,new Vector2(232,167),viewport,out float pointer);Assert.That(pointer,Is.InRange(12,220));Assert.That(tip.x,Is.InRange(16,width-248));Assert.That(tip.y,Is.InRange(92,height-247));
            }
            var hud=FxPanelPlacement.Breakdown(new Rect(width-50,14,40,66),new Vector2(300,238),viewport);Assert.That(hud.x,Is.EqualTo(width-316));Assert.That(hud.y,Is.EqualTo(90));
        }
        [Test] public void Batch_MissingOptionalLabelDoesNotBreakOtherWidgets()
        {
            var root=new GameObject("Boş etiket",typeof(RectTransform),typeof(Canvas));
            try
            {
                var go=new GameObject("Metin",typeof(RectTransform));go.transform.SetParent(root.transform,false);
                var text=go.AddComponent<TMPro.TextMeshProUGUI>();text.text=null;
                var combined=new GameObject("Grup",typeof(RectTransform));combined.transform.SetParent(root.transform,false);combined.SetActive(false);
                var batch=combined.AddComponent<FxGraphicBatch>();batch.sources=new Graphic[]{text};combined.SetActive(true);
                Assert.DoesNotThrow(()=>batch.Refresh());
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void Batch_ClipsScrollableGeometryAndOccludesOnlyLowerWindows()
        {
            var root=new GameObject("BatchClipTest",typeof(RectTransform),typeof(Canvas));
            try
            {
                RectTransform Rect(string name,Transform parent,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.sizeDelta=size;return r;}
                var mask=Rect("Viewport",root.transform,new Vector2(80,60));mask.gameObject.AddComponent<RectMask2D>();
                var source=Rect("Source",mask,new Vector2(100,100)).gameObject.AddComponent<Image>();
                var windowRect=Rect("Window",root.transform,new Vector2(40,60));windowRect.gameObject.SetActive(false);var window=windowRect.gameObject.AddComponent<FxPopupView>();window.rect=windowRect;window.group=windowRect.GetComponent<CanvasGroup>();window.group.alpha=1;
                var own=Rect("OwnSource",windowRect,new Vector2(10,10)).gameObject.AddComponent<Image>();
                var combined=Rect("Combined",root.transform,new Vector2(200,200));combined.gameObject.SetActive(false);var batch=combined.gameObject.AddComponent<FxGraphicBatch>();batch.sources=new Graphic[]{source,own};batch.occlusionWindows=new[]{window};combined.gameObject.SetActive(true);
                batch.Refresh();Canvas.ForceUpdateCanvases();Assert.That(batch.canvasRenderer.GetMesh().bounds.size,Is.EqualTo(new Vector3(80,60,0)));
                window.gameObject.SetActive(true);batch.Refresh();Canvas.ForceUpdateCanvases();var mesh=batch.canvasRenderer.GetMesh();var v=mesh.vertices;var triangles=mesh.triangles;float area=0;
                for(int n=0;n<triangles.Length;n+=3)area+=Mathf.Abs(Vector3.Cross(v[triangles[n+1]]-v[triangles[n]],v[triangles[n+2]]-v[triangles[n]]).z)*.5f;
                Assert.That(area,Is.EqualTo(80*60-40*60+10*10).Within(.1f));
                int builds=batch.MeshBuilds;batch.Refresh();long start=GC.GetAllocatedBytesForCurrentThread();for(int n=0;n<100;n++)batch.Refresh();Assert.That(GC.GetAllocatedBytesForCurrentThread()-start,Is.Zero);Canvas.ForceUpdateCanvases();Assert.That(batch.MeshBuilds,Is.EqualTo(builds));
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void PanelPrefabs_HaveUniqueBindingsAndNoMissingScripts()
        {
            var guids=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Corrigible/Fx/Prefabs/Desk/F4"});Assert.That(guids.Length,Is.EqualTo(12));
            foreach(var guid in guids){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));Assert.That(prefab.GetComponent<FxPopupView>(),Is.Not.Null);foreach(var u in prefab.GetComponentsInChildren<FxPanelBindings>(true)){var keys=new System.Collections.Generic.HashSet<string>();foreach(var slot in u.slots){Assert.That(keys.Add(slot.Key),Is.True,prefab.name+":"+slot.Key);Assert.That(slot.Rect,Is.Not.Null);}}foreach(var t in prefab.GetComponentsInChildren<Transform>(true))Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),Is.Zero,prefab.name);}
        }
        [Test] public void OrbAtlas_PreservesPixelsAndUsesOneTexture()
        {
            var root=new GameObject("AtlasTest");root.SetActive(false);var atlas=root.AddComponent<FxOrbAtlas>();var red=new RenderTexture(16,16,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);var green=new RenderTexture(16,16,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);var previous=RenderTexture.active;Texture2D read=null;
            try
            {
                var views=new OrbView[2];for(int n=0;n<2;n++){var go=new GameObject("Orb",typeof(RectTransform));go.SetActive(false);go.transform.SetParent(root.transform);views[n]=go.AddComponent<OrbView>();views[n].image=go.GetComponent<RawImage>();}atlas.views=views;root.SetActive(true);red.Create();green.Create();RenderTexture.active=red;GL.Clear(false,true,Color.red);RenderTexture.active=green;GL.Clear(false,true,Color.green);atlas.Publish(views[0],red);atlas.Publish(views[1],green);
                Assert.That(views[0].image.texture,Is.SameAs(views[1].image.texture));Assert.That(views[0].image.uvRect.Overlaps(views[1].image.uvRect),Is.False);RenderTexture.active=atlas.Target;read=new Texture2D(atlas.Target.width,atlas.Target.height,TextureFormat.RGBA32,false,true);read.ReadPixels(new Rect(0,0,read.width,read.height),0,0);read.Apply();Assert.That(read.GetPixel(4,4).r,Is.EqualTo(1).Within(.01));Assert.That(read.GetPixel(atlas.tileSize+4,4).g,Is.EqualTo(1).Within(.01));
            }finally{RenderTexture.active=previous;if(read!=null)UnityEngine.Object.DestroyImmediate(read);red.Release();green.Release();UnityEngine.Object.DestroyImmediate(red);UnityEngine.Object.DestroyImmediate(green);UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void GameAdapter_MissingEntriesNeverFallBackToMockTruth()
        {
            var data=new SadeDeskData{View=new SadeDeskViewModel()};data.View.Map.Regions.Add(new RegionNodeVM{Id="DoA",Value=0,Trust="verified"});
            var cache=new FxDeskDataCache{CardTemplate=new JObject{["secret"]=52},DossierTemplate=new JObject{["secret"]=52},FundTemplate=new JObject{["secret"]=52},NodeTemplate=new JObject{["secret"]=52}};cache.Load(data);
            Assert.That(cache.CardTemplate,Is.Null);Assert.That(cache.DossierTemplate,Is.Null);Assert.That(cache.FundTemplate,Is.Null);Assert.That(cache.NodeTemplate,Is.Null);var card=cache.Card(data.View,"DoA",true);Assert.That(card["secret"],Is.Null);Assert.That((double)card["infiltration"]["value"],Is.Zero);
        }
        [Test] public void ShownData_PreservesUnknownAndSanitizesIllusionSources()
        {
            var vm=new SadeDeskViewModel();vm.Map=new SadeMapStateVM();vm.Map.Regions.Add(new RegionNodeVM{Id="OA",Value=null,Trust="unknown"});vm.Map.Regions.Add(new RegionNodeVM{Id="DoA",Value=0,Trust="verified"});
            var cache=new FxDeskDataCache{CardTemplate=JObject.Parse(File.ReadAllText("Assets/Corrigible/Desk/Data/Sade/Mock/masa_S2.json"))["regionCard"] as JObject,DossierTemplate=JObject.Parse(File.ReadAllText("Assets/Corrigible/Desk/Data/Sade/Mock/masa_S9.json"))["dossier"] as JObject};
            var unknown=cache.Card(vm,"OA",false);Assert.That(unknown["infiltration"]["value"].Type,Is.EqualTo(JTokenType.Null));Assert.That(FxDeskText.Value(unknown["infiltration"]["value"],"unknown"),Is.EqualTo("?"));
            var illusion=cache.Dossier(vm,"DoA",true);Assert.That((double)illusion["infiltration"]["value"],Is.Zero);Assert.That(((JArray)illusion["sources"]).Count,Is.Zero);Assert.That(((JArray)illusion["links"]).Count,Is.Zero);Assert.That(((JObject)illusion["history"]).Count,Is.Zero);Assert.That((int)cache.CardTemplate["infiltration"]["value"],Is.EqualTo(38));
        }
    }
}
