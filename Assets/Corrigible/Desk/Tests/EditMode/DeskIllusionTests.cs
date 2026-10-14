using System.Linq;
using Corrigible.Desk;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>F4 İllüzyon: C2 verisinin kurulması, sunum anahtarının bileşenleri, prefab ve sahne bağlantıları.</summary>
    public class DeskIllusionTests
    {
        const string Root = "Assets/Corrigible/Desk";

        static MockDeskDataSource Mock(string preset, out GameObject go)
        {
            go = new GameObject("MockTest");
            var src = go.AddComponent<MockDeskDataSource>();
            var so = new SerializedObject(src);
            var presets = so.FindProperty("presets");
            presets.arraySize = 1;
            presets.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/" + preset + ".json");
            so.FindProperty("mapSpec").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/map.json");
            so.ApplyModifiedPropertiesWithoutUndo();
            return src;
        }

        [Test]
        public void C2_BuildsIllusionModel()
        {
            var src = Mock("C2_illusion", out var go);
            try
            {
                var vm = src.Current;
                Assert.IsTrue(vm.Illusion);
                Assert.IsFalse(vm.Map.AnalystLayer, "analist katmanı yok");
                Assert.IsTrue(vm.Map.Regions.All(r => r.Trust == "verified"), "tüm düğümler ONAYLANDI");
                var avr = vm.Map.Regions.Single(r => r.Id == "AVR");
                Assert.IsTrue(avr.Quarantine && avr.QuarantineClean, "karantina halkası kusursuz (\"clean\")");
                Assert.AreEqual(2, vm.Map.IllusionTraces.GhostNotes.Count);
                Assert.AreEqual(0.09f, vm.Map.IllusionTraces.GhostNotes[0].Alpha, 1e-4f);
                Assert.AreEqual(700, vm.Map.IllusionTraces.GhostNotes[1].Weight);
                Assert.AreEqual(new[] { 1400f, 550f }, vm.Map.IllusionTraces.EraserSmudge.Center);
                Assert.IsTrue(vm.Map.Analyst.Pins.Count > 0, "raptiye delikleri analist raptiyelerinin yerinde");
                Assert.AreEqual("TEMİZ", vm.Goal.Stamp.Text);
                Assert.AreEqual("Onay bekleniyor", vm.Goal.Next.Right);
                Assert.AreEqual("TEMİZ", vm.Research.HeaderStamp.Text);
                Assert.AreEqual("stopped", vm.Research.Branches.SelectMany(b => b.Projects).Single(p => p.Id == "dogrulama").State);
                Assert.AreEqual("ink-small", vm.Advisor.Actions[0].Kind);
                Assert.AreEqual(2, vm.Dossier.Actions.Count(a => a.State == "dim"));
                Assert.AreEqual("text", vm.Dossier.Preview.Rows[0].Kind);
                Assert.AreEqual("ONAYLANDI", vm.Dossier.Preview.Rows[0].Stamp);
                Assert.AreEqual(10, vm.Clues.Count);
                Assert.AreEqual(new[] { 528f, 398f }, vm.Clues[0].Markers[0], "işaret: 24 px dairenin sol-üstü");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void C1_HasNoIllusionTraces()
        {
            var src = Mock("C1_normal", out var go);
            try
            {
                Assert.IsFalse(src.Current.Illusion);
                Assert.IsNull(src.Current.Map.IllusionTraces);
                Assert.IsFalse(src.Current.Map.Regions.Single(r => r.Id == "AVR").QuarantineClean);
                Assert.AreEqual(0, src.Current.Clues.Count);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Straighten_RestoresAuthoredAngle()
        {
            var go = new GameObject("Tape");
            try
            {
                go.transform.localEulerAngles = new Vector3(0, 0, 12);
                var s = go.AddComponent<IllusionStraighten>();
                s.SetIllusion(true);
                Assert.AreEqual(0f, go.transform.localEulerAngles.z, 1e-3f);
                s.SetIllusion(false);
                Assert.AreEqual(12f, go.transform.localEulerAngles.z, 1e-3f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void StampAngle_IsZeroInIllusion()
        {
            var theme = DeskTheme.Active;
            bool was = DeskTheme.Illusion;
            try
            {
                DeskTheme.Illusion = false;
                Assert.AreEqual(-4f, theme.StampAngle("x", -4f));
                DeskTheme.Illusion = true;
                Assert.AreEqual(0f, theme.StampAngle("x", -4f), "damgalar İllüzyonda 0°");
            }
            finally { DeskTheme.Illusion = was; }
        }

        static GameObject Prefab(string rel) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + rel + ".prefab");
        static Object Field(Object o, string name) => new SerializedObject(o).FindProperty(name).objectReferenceValue;
        static int Count(Object o, string name) => new SerializedObject(o).FindProperty(name).arraySize;

        [Test]
        public void Prefabs_AreWiredForIllusion()
        {
            var map = Prefab("Screens/Map").GetComponent<MapView>();
            Assert.IsNotNull(Field(map, "tracesRoot")); Assert.IsNotNull(Field(map, "pinholeTemplate"));
            Assert.IsNotNull(Field(map, "ghostTemplate")); Assert.IsNotNull(Field(map, "eraser"));
            var adv = Prefab("Screens/AdvisorBand");
            Assert.AreEqual(2, adv.GetComponentsInChildren<IllusionStraighten>(true).Length, "iki bant parçası");
            var goal = Prefab("Screens/GoalCard").GetComponent<GoalCardView>();
            Assert.IsNotNull(Field(goal, "stamp")); Assert.IsNotNull(Field(goal, "nextRight"));
            var res = Prefab("Screens/ResearchFolder").GetComponent<ResearchFolderView>();
            Assert.IsNotNull(Field(res, "headerStamp"));
            Assert.GreaterOrEqual(new SerializedObject(res).FindProperty("headerBaseHeight").floatValue, 19,
                "Yeni panonun başlık satırı illüzyon damgasını kaydırmadan sığdırmalı");
            Assert.AreEqual(4, Count(res, "stageHeaders"));
            Assert.IsNotNull(Field(res, "cardTemplate")); Assert.IsNotNull(Field(res, "connections"));
            var dos = Prefab("Screens/RegionDossier").GetComponent<RegionDossierView>();
            Assert.IsNotNull(Field(dos, "infStamp")); Assert.IsNotNull(Field(dos, "coverPanel")); Assert.IsNotNull(Field(dos, "infPanel"));
            Assert.AreEqual(3, Count(dos, "statsRow")); Assert.AreEqual(3, Count(dos, "actionsHead")); Assert.AreEqual(3, Count(dos, "fieldHead"));
            var tx = Prefab("Screens/Telex").GetComponent<TelexStripView>();
            Assert.IsNotNull(Field(tx, "plate")); Assert.IsNotNull(Field(tx, "paper"));
            var clue = Prefab("Screens/ClueOverlay").GetComponent<ClueOverlay>();
            Assert.IsNotNull(Field(clue, "markerTemplate"));
        }

        [Test]
        public void Sandbox_HasIllusionPresenterAndClues()
        {
            var scene = EditorSceneManager.OpenScene(Root + "/Scenes/DeskUI_Sandbox.unity", OpenSceneMode.Additive);
            try
            {
                var pres = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DeskPresenter>(true)).Single();
                var ip = pres.GetComponent<IllusionPresenter>();
                Assert.IsNotNull(ip);
                Assert.AreEqual(1, Count(ip, "roots"));
                Assert.AreEqual(0f, new SerializedObject(ip).FindProperty("analystFadeSeconds").floatValue, "açık karar 5: varsayılan sessiz");
                Assert.AreSame(ip, Field(pres, "illusion"));
                Assert.IsNotNull(Field(pres, "clues"));
                var menu = pres.GetComponent<DeskDebugMenu>();
                Assert.IsNotNull(Field(menu, "clueOverlay")); Assert.IsNotNull(Field(menu, "illusion"));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
