using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    public class SadeRaidBoardTests
    {
        const string Root = "Assets/Corrigible/Desk";
        static JObject Data() => JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Sade/Mock/raid_board_states.json").text);
        static T Ref<T>(Object owner, string field) where T : Object => new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;

        [TestCase(1920, 1080)]
        [TestCase(1920, 1200)]
        [TestCase(3440, 1440)]
        public void Camera_ProjectsGridAndHeightToReferencePixels(int width, int height)
        {
            var go = new GameObject("Kamera izdüşüm testi");
            var camera = go.AddComponent<Camera>();
            var target = new RenderTexture(width, height, 16);
            camera.targetTexture = target;
            try
            {
                float scale = Mathf.Min(width / 1920f, height / 1080f);
                float ppu = SadeTheme.Active.Number("raid.camera.pixelsPerUnitAt1080");
                camera.orthographic = true;
                camera.orthographicSize = SadeRaidCameraFraming.Size(height, scale, ppu);
                camera.transform.rotation = Quaternion.Euler(35, 45, 0);
                camera.transform.position = -camera.transform.forward * 20 - camera.transform.up * (12 / ppu);
                foreach (var p in new[] { new Vector3(0, 0, 0), new Vector3(8, 8, 0), new Vector3(3.5f, 2.5f, .9f), new Vector3(4, 4, 0) })
                {
                    Vector3 actual = camera.WorldToScreenPoint(new Vector3(p.x - 4, p.z, 4 - p.y));
                    float expectedX = width / 2f + (p.x + p.y - 8) * 93.75f * scale;
                    float expectedTop = height / 2f - 12 * scale + (p.y - p.x) * 53.775f * scale - p.z * 108.62f * scale;
                    Assert.AreEqual(expectedX, actual.x, 1, "x izdüşümü");
                    Assert.AreEqual(expectedTop, height - actual.y, 1, "üstten y izdüşümü");
                }
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(target); }
        }

        [TestCase("B1")][TestCase("B2")][TestCase("B3")][TestCase("B4")][TestCase("B5")]
        [TestCase("B6")][TestCase("B7a")][TestCase("B7b")][TestCase("B8")]
        public void Presets_PreserveFixturePositionsHealthAndDoNotConsumeRandom(string id)
        {
            var data = Data(); var raw = data["states"][id];
            var snapshot = MockRaidBoardDataSource.Parse(data, id); var state = snapshot.State;
            Assert.AreEqual(64, state.Cells.Length);
            Assert.AreEqual(raw["units"].Count() + raw["robots"].Count(), state.Units.Count);
            foreach (var pair in raw["units"].Concat(raw["robots"]).Zip(state.Units, (fixture, unit) => (fixture, unit)))
            {
                Assert.AreEqual((int)pair.fixture["pos"][0], pair.unit.X);
                Assert.AreEqual((int)pair.fixture["pos"][1], pair.unit.Y);
                Assert.AreEqual((int)pair.fixture["hp"], pair.unit.Hp);
                Assert.AreEqual((int)pair.fixture["max"], pair.unit.Max);
            }
            Assert.AreEqual(0, state.Rs, "Örnek kurulum RNG tüketmez.");
            if (id == "B2") Assert.AreEqual(7, snapshot.Marks.Moves.Count);
            if (id == "B4")
            {
                var intents = Baskin.Intents(state);
                Assert.AreEqual(2, intents.Count(i => i.Uncertain));
                Assert.AreEqual(2, intents.Count(i => i.IsScanned));
            }
            if (id == "B6") { Assert.IsTrue(state.Units.Any(u => u.Type == "emp" && u.Dead)); Assert.Greater(snapshot.Marks.Rubble.Count, 0); }
        }

        [Test]
        public void LabelPrefab_ShowsPipsAndTargetArmorWithoutHealthText()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<RaidScreenLabel>(Root + "/Prefabs/Sade/Raid/RaidScreenLabel.prefab");
            Assert.IsNotNull(prefab);
            var canvasObject = new GameObject("Etiket testi", typeof(RectTransform), typeof(Canvas));
            var cameraObject = new GameObject("Kamera", typeof(Camera));
            try
            {
                var label = Object.Instantiate(prefab, canvasObject.transform);
                label.Configure(cameraObject.GetComponent<Camera>(), canvasObject.GetComponent<Canvas>(), DeskTheme.Active.cpBold);
                label.ShowHealth(3, 4, Color.red, true, true);
                Assert.AreEqual(3, label.Health); Assert.AreEqual(4, label.Maximum);
                Assert.IsTrue(label.HasTarget); Assert.IsTrue(label.HasArmor);
                Assert.IsFalse(Ref<TMPro.TMP_Text>(label, "caption").gameObject.activeSelf);
                var array = new SerializedObject(label).FindProperty("pips");
                Assert.AreEqual(4, Enumerable.Range(0, array.arraySize).Count(i => ((Image)array.GetArrayElementAtIndex(i).objectReferenceValue).gameObject.activeSelf));
                label.ShowHealth(1, 2, Color.black, false, false);
                Assert.IsFalse(label.HasTarget); Assert.IsFalse(label.HasArmor);
            }
            finally { Object.DestroyImmediate(canvasObject); Object.DestroyImmediate(cameraObject); }
        }

        [Test]
        public void Sandbox_IsIsolatedAndUsesCameraBackgroundAndOverlayLabels()
        {
            var scene = EditorSceneManager.OpenScene(Root + "/Scenes/RaidBoard_Sandbox.unity", OpenSceneMode.Additive);
            try
            {
                var components = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
                Assert.IsFalse(components.Any(c => c is Corrigible.Game.GameController || c is Corrigible.Game.UI.RaidView || c is RaidCameraFraming));
                var board = components.OfType<RaidBoard3D>().Single();
                Assert.IsNotNull(Ref<RaidScreenLabel>(board, "screenLabelPrefab"));
                var labels = Ref<Canvas>(board, "screenLabelCanvas");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, labels.renderMode); Assert.AreEqual(10, labels.sortingOrder);
                var background = components.OfType<BoardBackgroundView>().Single();
                var canvas = Ref<Canvas>(background, "background");
                Assert.AreEqual(RenderMode.ScreenSpaceCamera, canvas.renderMode); Assert.AreSame(board.Camera, canvas.worldCamera);
                Assert.IsNotNull(components.OfType<MockRaidBoardDataSource>().Single());
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
