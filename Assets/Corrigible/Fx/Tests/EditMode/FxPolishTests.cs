using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx.Tests
{
    public sealed class FxPolishTests
    {
        [Test]
        public void PulseUsesVertexDataAndStopsAtConstantState()
        {
            var go=new GameObject("Nabız testi",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            try{var pulse=go.AddComponent<FxPulseGraphic>();pulse.SetPulse(true);using(var vh=new VertexHelper()){var v=UIVertex.simpleVert;v.position=Vector3.one;vh.AddVert(v);pulse.ModifyMesh(vh);vh.PopulateUIVertex(ref v,0);Assert.That(v.uv1.x,Is.GreaterThan(0));Assert.That(v.uv1.z,Is.EqualTo(31));pulse.SetPulse(false);pulse.ModifyMesh(vh);vh.PopulateUIVertex(ref v,0);Assert.That(v.uv1.x,Is.Zero);}}finally{Object.DestroyImmediate(go);}
        }
        [Test]
        public void BatchKeepsIndependentSourceMeshesAcrossDynamicRebuilds()
        {
            var root=new GameObject("Grup testi",typeof(RectTransform),typeof(Canvas));
            Image Image(string name,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(root.transform,false);var image=go.GetComponent<Image>();image.color=color;return image;}
            var a=Image("A",Color.red);var b=Image("B",Color.green);var go=new GameObject("Grup",typeof(RectTransform));go.transform.SetParent(root.transform,false);go.SetActive(false);var batch=go.AddComponent<FxGraphicBatch>();batch.sources=new Graphic[]{a,b};go.SetActive(true);
            try{batch.Refresh();Canvas.ForceUpdateCanvases();var before=batch.canvasRenderer.GetMesh();Assert.That(System.Array.Exists(before.colors32,c=>c.r==255&&c.g==0),Is.True);Assert.That(System.Array.Exists(before.colors32,c=>c.g==255&&c.r==0),Is.True);Object.DestroyImmediate(before);a.color=Color.blue;batch.Refresh();Canvas.ForceUpdateCanvases();var after=batch.canvasRenderer.GetMesh();Assert.That(System.Array.Exists(after.colors32,c=>c.b==255&&c.r==0),Is.True);Assert.That(System.Array.Exists(after.colors32,c=>c.g==255&&c.r==0),Is.True);Object.DestroyImmediate(after);}finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void TextCheckerFindsReplacementSquareUsingTheOriginalCharacter()
        {
            var root=new GameObject("Glif testi",typeof(RectTransform),typeof(Canvas));var go=new GameObject("GlyphProbe",typeof(RectTransform),typeof(CanvasRenderer),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(root.transform,false);var text=go.GetComponent<TMPro.TextMeshProUGUI>();
            try{text.font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Corrigible/Fx/Art/Fonts/SDF/ChakraPetch-Bold.asset");text.fontSize=13;text.rectTransform.sizeDelta=new Vector2(500,60);text.text="\U0001F9EA";text.ForceMeshUpdate(true,true);var report=new System.Text.StringBuilder();Corrigible.Fx.EditorTools.FxTextChecker.CheckCurrent("Glyph",report,out int count);Assert.That(report.ToString(),Does.Contain("GlyphProbe · Eksik glif"));}finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void EdgesPreserveTransformedEndpointsAndRebuildOnlyWhenChanged()
        {
            var root=new GameObject("Kenar testi");root.SetActive(false);var mesh=new Mesh();mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.up};mesh.normals=new[]{Vector3.forward,Vector3.forward,Vector3.forward};mesh.tangents=new[]{new Vector4(1,0,0,1),new Vector4(1,0,0,1),new Vector4(1,0,0,1)};mesh.colors=new[]{Color.red,Color.green,Color.blue};mesh.SetUVs(0,new[]{new Vector4(1,2,3,1),new Vector4(1,2,3,0),new Vector4(1,2,3,1)});mesh.SetUVs(1,new[]{Vector4.one,Vector4.one,Vector4.one});mesh.triangles=new[]{0,1,2};
            MeshFilter Child(string name){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);return go.GetComponent<MeshFilter>();}
            try{var source=Child("Kaynak");source.sharedMesh=mesh;source.transform.localPosition=new Vector3(4,5,6);source.transform.localScale=new Vector3(2,3,4);var edges=root.AddComponent<FxRaidEdges>();edges.sources=new[]{source};edges.originalRenderers=new[]{source.GetComponent<MeshRenderer>()};edges.edgeTarget=Child("Çizgi");edges.glowTarget=Child("Işıma");root.SetActive(true);Assert.That(edges.Combined.vertexCount,Is.EqualTo(3));Assert.That(edges.Combined.vertices[1],Is.EqualTo(new Vector3(6,5,6)));var uv=new System.Collections.Generic.List<Vector4>();edges.Combined.GetUVs(0,uv);Assert.That(uv[0],Is.EqualTo(new Vector4(6,11,18,1)));int builds=edges.Rebuilds;edges.Refresh();Assert.That(edges.Rebuilds,Is.EqualTo(builds));source.gameObject.SetActive(false);edges.Refresh();Assert.That(edges.Combined.vertexCount,Is.Zero);Assert.That(edges.edgeTarget.sharedMesh,Is.SameAs(edges.glowTarget.sharedMesh));edges.enabled=false;Assert.That(edges.originalRenderers[0].enabled,Is.True);}finally{Object.DestroyImmediate(root);Object.DestroyImmediate(mesh);}
        }
    }
}
