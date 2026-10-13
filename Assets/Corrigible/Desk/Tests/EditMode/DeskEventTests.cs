using System.Linq;
using Corrigible.Desk;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>F5 olay bildirimi: C3 verisi (C1 tabanı + olay), sahte imza, spot mesh'i, prefab ve sahne bağlantıları.</summary>
    public class DeskEventTests
    {
        const string Root = "Assets/Corrigible/Desk";

        static MockDeskDataSource Mock(out GameObject go)
        {
            go = new GameObject("MockTest");
            var src = go.AddComponent<MockDeskDataSource>();
            var so = new SerializedObject(src);
            var presets = so.FindProperty("presets");
            string[] names = { "C1_normal", "C2_illusion", "C3_event" };
            presets.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                presets.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/" + names[i] + ".json");
            so.FindProperty("mapSpec").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/map.json");
            so.ApplyModifiedPropertiesWithoutUndo();
            return src;
        }

        [Test]
        public void C3_BuildsEventOverPausedDesk()
        {
            var src = Mock(out var go);
            try
            {
                src.Load(2);
                var vm = src.Current;
                Assert.IsTrue(vm.Clock.Paused, "olay masayı duraklatır");
                Assert.AreEqual("DAS", vm.Dossier.RegionId, "arka plan C1 masası");
                var e = vm.Event;
                Assert.IsNotNull(e);
                Assert.AreEqual("AVR", e.Spotlight.Region);
                Assert.AreEqual(new[] { 1163f, 364f }, e.Spotlight.Center);
                Assert.AreEqual(148f, e.Spotlight.HoleDiameter);
                Assert.AreEqual("KARANLIK KIŞ", e.Memo.Title);
                Assert.AreEqual(-1f, e.Memo.Rect[3], "\"auto\" → −1");
                Assert.AreEqual(4, e.Memo.Fields.Count);
                Assert.AreEqual("ACİL", e.Memo.Fields[1].Stamp.Text);
                Assert.AreEqual(4, e.Memo.Sources.Count);
                Assert.IsTrue(e.Memo.Sources[1].Carbon);
                Assert.AreEqual(3, e.Options.Count);
                Assert.AreEqual("hover", e.Options[0].State);
                Assert.IsTrue(e.Options[0].Gains[1].ValueIsText);
                Assert.IsTrue(e.Options.All(o => o.Preview != null), "her seçeneğin önizlemesi var (sahte: üretilir)");
                Assert.AreEqual(4, e.AiLog.Lines.Count);
                Assert.AreEqual(1.2f, e.AiLog.RotDeg, 1e-4f);
                Assert.AreEqual(-1.6f, e.Advisor.RotDeg, 1e-4f);
                Assert.AreEqual(2, e.Footer.Hints.Count);
                CollectionAssert.AreEqual(new[] { "1", "2", "3" }, e.Footer.Hints[1].Keys);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void C3_SignClosesEventAndResumes()
        {
            var src = Mock(out var go);
            try
            {
                src.Load(2);
                src.CloseEvent();
                Assert.IsNull(src.Current.Event);
                Assert.IsFalse(src.Current.Clock.Paused);
                src.Load(0);
                Assert.IsNull(src.Current.Event, "C1'de olay yok");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Spotlight_HoleIsClearAndOuterCovers()
        {
            var go = new GameObject("Spot", typeof(RectTransform));
            try
            {
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(1000, 600);
                var g = go.AddComponent<SpotlightGraphic>();
                g.color = new Color(0, 0, 0, 0.9f);
                g.SetSpot(new Vector2(100, 50), 74, 12, new Color(0, 0, 0, 0.95f));
                var mesh = new Mesh();
                var vh = new VertexHelper();
                typeof(SpotlightGraphic).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(VertexHelper) }, null)
                    .Invoke(g, new object[] { vh });
                vh.FillMesh(mesh);
                var v = mesh.vertices; var c = mesh.colors32;
                // Delik içinde (merkezden 73 px'ten yakın) hiçbir köşe yok
                Assert.IsFalse(v.Any(p => Vector2.Distance(p, new Vector2(100, 50)) < 73f), "delik saydam");
                // Dikdörtgenin köşeleri kaplanır
                Assert.IsTrue(v.Any(p => Mathf.Approximately(p.x, -500) && Mathf.Approximately(p.y, -300)));
                Assert.IsTrue(v.Any(p => Mathf.Approximately(p.x, 500) && Mathf.Approximately(p.y, 300)));
                // Halka (74–86) dış karartmadan koyu
                int ringIdx = System.Array.FindIndex(v, p => Mathf.Abs(Vector2.Distance(p, new Vector2(100, 50)) - 80) < 6f);
                Assert.GreaterOrEqual(ringIdx, 0);
                Assert.AreEqual(242, c[ringIdx].a, 1);
            }
            finally { Object.DestroyImmediate(go); }
        }

        static GameObject Prefab(string rel) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + rel + ".prefab");
        static Object Field(Object o, string name) => new SerializedObject(o).FindProperty(name).objectReferenceValue;

        [Test]
        public void EventModalPrefab_IsWired()
        {
            var p = Prefab("Screens/EventModal");
            Assert.IsNotNull(p);
            var v = p.GetComponent<EventModalView>();
            foreach (var f in new[] { "root", "peek", "scrimGroup", "scrim", "papers", "design", "memo", "log", "advisor", "advisorLink", "thread", "threadShadow", "markerRing", "footerText", "footerHints" })
                Assert.IsNotNull(Field(v, f), f);
            Assert.AreEqual(3, new SerializedObject(v).FindProperty("options").arraySize);
            Assert.AreEqual(4, new SerializedObject(v).FindProperty("footerKeys").arraySize);
            Assert.IsTrue(p.GetComponentInChildren<SpotlightGraphic>(true).raycastTarget, "arka plan etkileşimsiz");
            foreach (var o in p.GetComponentsInChildren<EventOptionView>(true))
            {
                Assert.IsNotNull(Field(o, "sign")); Assert.IsNotNull(Field(o, "previewBox")); Assert.IsNotNull(Field(o, "slot"));
                Assert.IsFalse(new SerializedObject(Field(o, "sign")).FindProperty("autoWidth").boolValue, "imza düğmesi genişliği karttan");
            }
            var memo = p.GetComponentInChildren<EventMemoView>(true);
            Assert.AreEqual(4, new SerializedObject(memo).FindProperty("fields").arraySize);
            Assert.IsNotNull(Field(memo, "rowTemplate"));
        }

        [Test]
        public void KeyChip_DefaultsUnchanged()
        {
            var k = Prefab("Components/KeyChip").GetComponent<KeyChipView>();
            var so = new SerializedObject(k);
            Assert.AreEqual(18f, so.FindProperty("height").floatValue);
            Assert.AreEqual(11f, so.FindProperty("fontSize").floatValue);
            Assert.AreEqual("paper.index", so.FindProperty("fillKey").stringValue);
        }

        [Test]
        public void Sandbox_HasEventModalUnderConfirm()
        {
            var scene = EditorSceneManager.OpenScene(Root + "/Scenes/DeskUI_Sandbox.unity", OpenSceneMode.Additive);
            try
            {
                var pres = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DeskPresenter>(true)).Single();
                var ev = (EventModalView)Field(pres, "eventModal");
                Assert.IsNotNull(ev);
                var confirm = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ConfirmModalView>(true)).Single();
                Assert.AreSame(ev.transform.parent, confirm.transform.parent, "ikisi de Modal canvas'ında");
                Assert.Less(ev.transform.GetSiblingIndex(), confirm.transform.GetSiblingIndex(), "onay kartı olayın üstünde");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
