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
    public sealed class FxMapTests
    {
        static JObject Spec=>JObject.Parse(File.ReadAllText("Assets/Corrigible/Fx/Data/map_fx.json"));
        [Test] public void AiColors_MatchAllCanonicalSamples(){foreach(var p in (JObject)Spec["aiColor"]["samples"]){ColorUtility.TryParseHtmlString((string)p.Value,out var expected);var actual=FxRegionRules.AiColor(double.Parse(p.Key));Assert.That(actual.r,Is.EqualTo(expected.r).Within(1.0/255));Assert.That(actual.g,Is.EqualTo(expected.g).Within(1.0/255));Assert.That(actual.b,Is.EqualTo(expected.b).Within(1.0/255));}}
        [Test] public void NodeOffsets_MatchAllTwelveReferencePoints(){foreach(var r in Spec["regions"]){var p=new Vector2((float)r["node"][0],(float)r["node"][1]);double radius=(double)r["orbRadius"];bool east=(string)r["id"]=="DA";var chip=p+FxRegionRules.ChipOffset(radius,east);var label=p+FxRegionRules.LabelOffset(radius,east);Assert.That(chip.y,Is.EqualTo((float)r["valueChipTopCenter"][1]).Within(.15));Assert.That(label.y,Is.EqualTo((float)r["labelTopCenter"][1]).Within(.15));}}
        [Test] public void SpreadEndpoints_MatchReferenceAndStayOutsideOrbs(){var specs=Spec;foreach(var thread in specs["overlay"]["spreadThreads"]["items"]){JToken from=null,to=null;foreach(var r in specs["regions"]){if((string)r["id"]==(string)thread["from"])from=r;if((string)r["id"]==(string)thread["to"])to=r;}Vector2 A(JToken r)=>new Vector2((float)r["node"][0],(float)r["node"][1]);var points=FxRegionRules.Spread(A(from),A(to),(float)from["orbRadius"],(float)to["orbRadius"]);for(int i=0;i<3;i++){Assert.That(points[i].x,Is.EqualTo((float)thread["quadBezier"][i][0]).Within(.2));Assert.That(points[i].y,Is.EqualTo((float)thread["quadBezier"][i][1]).Within(.2));}}}
        [Test] public void RegionParameterChange_DoesNotRebuildUiGeometry()
        {
            var go=new GameObject("RegionTest",typeof(RectTransform));var layer=go.AddComponent<FxRegionLayer>();layer.enabled=false;
            try{layer.sourceMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Corrigible/Fx/Settings/FxRegion.mat");var c=new GameObject("Fill",typeof(RectTransform),typeof(CanvasRenderer),typeof(RegionFxGraphic));c.transform.SetParent(go.transform);var r=c.GetComponent<RegionFxGraphic>();layer.regions=new[]{r};layer.enabled=true;int before=r.MeshBuilds;layer.Set(0,100,false,false);long gc=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)layer.Set(0,i,false,false);Assert.That(GC.GetAllocatedBytesForCurrentThread()-gc,Is.Zero);Assert.That(r.MeshBuilds,Is.EqualTo(before));Assert.That(layer.Value(0).x,Is.EqualTo(.99f));}finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void GraphicBatch_PreservesSourceMeshesAndVisibilityChanges()
        {
            var root=new GameObject("BatchTest",typeof(RectTransform),typeof(Canvas));
            try
            {
                var images=new Graphic[2];
                for(int i=0;i<images.Length;i++){var go=new GameObject("Source",typeof(RectTransform),typeof(Image));go.transform.SetParent(root.transform,false);images[i]=go.GetComponent<Image>();}
                var combined=new GameObject("Combined",typeof(RectTransform));combined.transform.SetParent(root.transform,false);combined.SetActive(false);
                var batch=combined.AddComponent<FxGraphicBatch>();batch.sources=images;combined.SetActive(true);
                batch.Refresh();Canvas.ForceUpdateCanvases();
                Assert.That(batch.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(8));
                Assert.That(images[0].canvasRenderer.cull,Is.True);
                images[1].gameObject.SetActive(false);batch.Refresh();Canvas.ForceUpdateCanvases();
                Assert.That(batch.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(4));
                var image=(Image)images[0];image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Corrigible/Fx/Art/Sprites/fx_white@2x.png");image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.fillOrigin=0;image.fillAmount=.5f;
                batch.Refresh();Canvas.ForceUpdateCanvases();
                Assert.That(batch.canvasRenderer.GetMesh().bounds.size.x,Is.EqualTo(image.rectTransform.rect.width*.5f).Within(.01f));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void SharedRegionShader_SupportsStencilAndNoCompileErrors(){var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Corrigible/Fx/Settings/FxRegion.mat");Assert.That(m,Is.Not.Null);Assert.That(m.HasProperty("_Stencil"),Is.True);Assert.That(ShaderUtil.ShaderHasError(m.shader),Is.False);}
    }
}
