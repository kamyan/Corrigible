using System;
using System.Linq;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Corrigible.Desk.Tests
{
    /// <summary>R1–R4: model bağlantıları, zımba sırası ve baskı dokularının gerçekten kullanılan UV'leri.</summary>
    public sealed class Rev3Stage8EntranceTests
    {
        static SerializedProperty P(Object o,string f)=>new SerializedObject(o).FindProperty(f);
        static T R<T>(Object o,string f)where T:Object=>(T)P(o,f).objectReferenceValue;
        [TestCase("SettingsCabinet",-95)] [TestCase("SettingsCabinetMenu",-120)]
        public void Cabinet_UsesTheModelPivotsAndReadablePlates(string name,float angle)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Desk/Prefabs/Siginak/"+name+".prefab");var c=root.GetComponent<SettingsCabinet>();
            Assert.AreEqual(angle,P(c,"leafOpenDeg").floatValue);Assert.IsNotNull(R<PartDriver>(c,"bulbPart"));Assert.AreEqual(LightShadows.None,R<Light>(c,"panelFill").shadows);
            foreach(var f in root.GetComponentsInChildren<CabinetFader>(true))foreach(string key in new[]{"handlePart","needlePart","lampPart"})Assert.IsNotNull(R<PartDriver>(f,key),f.name+key);
            foreach(var model in root.GetComponentsInChildren<Rev3Model>(true).Where(m=>m.ModelName=="CabPlate"))foreach(var slot in model.Slots)
            {Assert.IsFalse(slot.Text.enableAutoSizing);Assert.GreaterOrEqual(slot.Text.fontSize*0.1f*slot.Text.font.faceInfo.capLine/slot.Text.font.faceInfo.pointSize,.0067f);}
            Assert.AreEqual(name=="SettingsCabinetMenu",R<ShiftSwitch>(c,"exit")==null);
        }
        [Test]
        public void Clock_Uses80DistinctOrderedAnchors_AndSeparateFreshHole()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Corrigible/Desk/Scenes/Giris.unity",OpenSceneMode.Additive);
            try
            {
                var ctrl=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<EntranceController>(true)).Single();var c=R<TimeClock>(ctrl,"clock");var a=P(c,"punches");Assert.AreEqual(80,a.arraySize);
                var holes=Enumerable.Range(0,80).Select(i=>((GameObject)a.GetArrayElementAtIndex(i).objectReferenceValue).transform).ToArray();
                Assert.AreEqual(80,holes.Distinct().Count());for(int i=0;i<80;i++)Assert.AreEqual("H"+(i+1).ToString("00"),holes[i].parent.name);
                foreach(string f in new[]{"cardRole","cardShelter","cardFree","newCardShift","newCardFree","freshPunch","clipPart","tiltPart","plungerPart","lampPart","hourPart","minutePart"})Assert.IsNotNull(P(c,f).objectReferenceValue,f);
                Assert.IsFalse(R<Transform>(c,"blank").gameObject.activeSelf);
                foreach(string f in new[]{"clockPressPose","switchPose","instruments"})Assert.IsNotNull(P(ctrl,f).objectReferenceValue,f);
                Assert.IsFalse(R<DeskTarget>(ctrl,"doorTarget").GetComponent<Collider>().enabled,"kapı dekor, oyunu saat/kart başlatır");
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
        [TestCase("CabCounter_Wheel0")] [TestCase("PunchClock_Wheel0")] [TestCase("CabGauge_Body")] [TestCase("ShiftCard_Tilt")]
        public void PrintedMeshes_HaveNormalizedUV_NotMetreProjection(string name)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Corrigible/Desk/Art/Rev3/SceneMeshes/Menu/"+name+".asset");Assert.IsNotNull(mesh);
            Assert.Greater(mesh.uv.Max(p=>p.x)-mesh.uv.Min(p=>p.x),.9f);Assert.Greater(mesh.uv.Max(p=>p.y)-mesh.uv.Min(p=>p.y),.9f);
            Assert.IsFalse(mesh.uv.Any(p=>float.IsNaN(p.x)||float.IsNaN(p.y)),"sıfır genişlikli izdüşüm yok");
        }

        [TestCase("İPTAL EDİLİR\nV.014")] [TestCase("TO BE CANCELLED\nS.014")]
        public void CancellationSlip_FitsBothLanguages_WithoutShrinking(string text)
        {
            var scene=EditorSceneManager.OpenScene("Assets/Corrigible/Desk/Scenes/Giris.unity",OpenSceneMode.Additive);
            try
            {
                var clock=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TimeClock>(true)).Single();var t=R<TMPro.TMP_Text>(clock,"voidText");
                var preferred=t.GetPreferredValues(text,float.PositiveInfinity,float.PositiveInfinity);
                Assert.IsFalse(t.enableAutoSizing);Assert.LessOrEqual(preferred.x,t.rectTransform.rect.width);Assert.LessOrEqual(preferred.y,t.rectTransform.rect.height);
                Assert.AreEqual(1,t.transform.lossyScale.x,.001f,"fiş büyürken harf genişliği değişmez");
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
