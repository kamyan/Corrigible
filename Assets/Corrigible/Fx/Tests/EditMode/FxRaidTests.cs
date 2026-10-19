using System.IO;
using Corrigible.Fx.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Corrigible.Fx.Tests
{
    public sealed class FxRaidTests
    {
        [TestCase(1920,1080)] [TestCase(1920,1200)] [TestCase(2560,1440)] [TestCase(3440,1440)]
        public void CameraProjectsCanonicalGridAndHeight(int width,int height)
        {
            var go=new GameObject("İzdüşüm testi");var cam=go.AddComponent<Camera>();var rt=new RenderTexture(width,height,24);
            try{cam.targetTexture=rt;float scale=Mathf.Min(width/1920f,height/1080f);cam.orthographic=true;cam.orthographicSize=height/scale/(2*132.5825f);cam.transform.rotation=Quaternion.Euler(35,45,0);cam.transform.position=-cam.transform.forward*20-cam.transform.up*(12/132.5825f);
                foreach(var point in new[]{new Vector3(0,0,0),new Vector3(8,8,0),new Vector3(6.5f,3.5f,1.4f),new Vector3(-.3f,8.3f,-.25f)}){
                    var p=cam.WorldToScreenPoint(FxRaidBoard.Corner(point.x,point.y,point.z));float x=width*.5f+(210+(point.x+point.y)*93.75f-960)*scale;float top=height*.5f+(528+(point.y-point.x)*53.775f-point.z*108.62f-540)*scale;
                    Assert.That(p.x,Is.EqualTo(x).Within(.05f));Assert.That(height-p.y,Is.EqualTo(top).Within(.05f));}
            }finally{cam.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
        }
        [Test] public void BoxHasTwelveEdgesWithoutFaceDiagonals()
        {
            var source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Corrigible/Art/Raid/Analyst/Meshes/UnitBox.asset");var mesh=FxRaidEdgeMesh.Build(source,new[]{Color.cyan});
            try{Assert.That(mesh.vertexCount,Is.EqualTo(12*4));Assert.That(mesh.triangles.Length,Is.EqualTo(12*6));var uv=new System.Collections.Generic.List<Vector4>();mesh.GetUVs(1,uv);foreach(var p in uv){Assert.That(Mathf.Abs(p.x),Is.EqualTo(1));Assert.That(p.y,Is.EqualTo(1));}}
            finally{Object.DestroyImmediate(mesh);}
        }
        [Test] public void EdgeAntialiasCoordinatesKeepTheSameSideAlongBothEndpoints()
        {
            var source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Corrigible/Art/Raid/Analyst/Meshes/UnitBox.asset");var mesh=FxRaidEdgeMesh.Build(source,new[]{Color.cyan});
            try{var endpoints=new System.Collections.Generic.List<Vector4>();var sides=new System.Collections.Generic.List<Vector4>();mesh.GetUVs(0,endpoints);mesh.GetUVs(1,sides);var expected=new[]{-1f,1f,1f,-1f};for(int i=0;i<mesh.vertexCount;i++)Assert.That(sides[i].x*(endpoints[i].w>.5f?-1:1),Is.EqualTo(expected[i%4]));}
            finally{Object.DestroyImmediate(mesh);}
        }
        [Test] public void SoftCreaseIsRetainedOnlyAsSilhouetteCandidate()
        {
            var mesh=new Mesh{vertices=new[]{new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(0,0,1),new Vector3(0,.1f,-1)},triangles=new[]{0,2,1,0,1,3}};var edges=FxRaidEdgeMesh.Build(mesh,new[]{Color.cyan});
            try{var uv=new System.Collections.Generic.List<Vector4>();edges.GetUVs(1,uv);int soft=0;foreach(var p in uv)if(p.y==0)soft++;Assert.That(soft,Is.EqualTo(4));Assert.That(edges.vertexCount,Is.EqualTo(20));}finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(edges);}
        }
        [Test] public void FlickerUsesBriefPhasesAndReducedMotionIsStable()
        {
            Assert.That(FxRaidBoard.BoardFlicker(.5,false),Is.EqualTo(1));Assert.That(FxRaidBoard.BoardFlicker(1+7*.48,false),Is.EqualTo(.86).Within(.0001));Assert.That(FxRaidBoard.BoardFlicker(1+7*.84,false),Is.EqualTo(.90).Within(.0001));Assert.That(FxRaidBoard.BoardFlicker(1+7*.48,true),Is.EqualTo(1));
        }
        [Test] public void FacePaletteAndShaderCompilationAreValid()
        {
            foreach(var name in new[]{"FxHoloFace","FxHoloGlass","FxEdgeLine"}){var shader=Shader.Find("Corrigible/"+name);Assert.That(shader,Is.Not.Null);foreach(var message in ShaderUtil.GetShaderMessages(shader))Assert.That(message.severity,Is.Not.EqualTo(UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),message.message);}
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Corrigible/Fx/Settings/Raid/PlywoodTop.mat");Assert.That(ColorUtility.ToHtmlStringRGB(material.GetColor("_BaseColor")),Is.EqualTo("0C2A42"));var plan=AssetDatabase.LoadAssetAtPath<Material>("Assets/Corrigible/Fx/Settings/Raid/Ozalit.mat");Assert.That(plan.GetTexture("_BaseMap").name,Is.EqualTo("tx_fx_plan_ana_sunucu_salonu_2048"));
        }
        [Test] public void SandboxHasAuthoredReferencesAndNoLiveController()
        {
            var path="Assets/Corrigible/Fx/Scenes/FxRaid_Sandbox.unity";var text=File.ReadAllText(path);Assert.That(text,Does.Contain("FxRaid_Sandbox"));Assert.That(text,Does.Not.Contain("m_EditorClassIdentifier: Assembly-CSharp::"));
            var script=File.ReadAllText("Assets/Corrigible/Fx/Scripts/Runtime/Raid/FxRaidBoard.cs");Assert.That(script,Does.Not.Contain("GameController"));Assert.That(script,Does.Not.Contain("UnityEngine.Random"));Assert.That(script,Does.Not.Contain(".Inf"));
            var board=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Fx/Prefabs/Raid/FxBoardBase.prefab");Assert.That(board,Is.Not.Null);foreach(var t in board.GetComponentsInChildren<Transform>(true))Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),Is.Zero);
        }
    }
}
