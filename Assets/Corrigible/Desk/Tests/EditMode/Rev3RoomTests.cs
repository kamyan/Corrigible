using System.Collections.Generic;
using System.Linq;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 Aşama 2 (komuta odası temeli): model parçalarına bağlanan sunum bileşenleri (sayaç tamburu, tebeşir çetelesi,
    /// teleks kâğıdının dört yuvası, telsiz defterinin sayfası) ve poz ışığı tarifi (pozda en çok 4 gölgeli ışık, tarifteki her
    /// ışık sahnede bağlı).
    /// </summary>
    public class Rev3RoomTests
    {
        const string ScenePath = DeskSpriteManifest.Root + "/Scenes/DeskGame.unity";
        const string RecipePath = DeskSpriteManifest.Root + "/Data/Rev3/PoseLightRecipe.asset";
        const string EntrancePath = DeskSpriteManifest.Root + "/Scenes/Giris.unity";

        readonly List<Object> created = new List<Object>();

        [TearDown]
        public void Clean()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        GameObject Make(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }

        static void Wire(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void CounterWheel_TurnsTheModelDrum36DegreesPerDigit()
        {
            var go = Make("Wheel");
            go.AddComponent<MeshRenderer>();   // CounterWheel bir Renderer ister (tamburun kendi ağı)
            var drum = go.AddComponent<PartDriver>();
            drum.Configure("Wheel", PartKind.Rotate, PartAxis.X, -3600, 3600, 0, Vector3.zero);
            var wheel = go.AddComponent<CounterWheel>();
            wheel.ConfigureDrum(drum);
            for (int d = 0; d < 10; d++)
            {
                wheel.Set(d);
                Assert.AreEqual(36f * d, drum.Value, 0.01f, "rakam " + d);
                Assert.AreEqual(d, wheel.Digit);
            }
        }

        [Test]
        public void ChalkTally_EachMarkShowsAGroupOfFiveAndTheLastOneStaysFull()
        {
            Assert.AreEqual(0, ChalkTally.CellFor(0, 0, 16));
            Assert.AreEqual(3, ChalkTally.CellFor(3, 0, 16));
            Assert.AreEqual(5, ChalkTally.CellFor(7, 0, 16));
            Assert.AreEqual(2, ChalkTally.CellFor(7, 1, 16));
            Assert.AreEqual(0, ChalkTally.CellFor(7, 2, 16));
            Assert.AreEqual(5, ChalkTally.CellFor(80, 15, 16), "80 vardiya: son yer dolu");
            Assert.AreEqual(5, ChalkTally.CellFor(95, 15, 16), "80'den sonra son yer dolu kalır");
        }

        static TMP_Text Tmp(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var t = go.AddComponent<TextMeshPro>();
            t.rectTransform.sizeDelta = new Vector2(0.22f, 0.03f);
            t.fontSize = 0.2f;
            return t;
        }

        [Test]
        public void Teleprinter_WritesTheFourPaperSlots()
        {
            var go = Make("Teleprinter");
            var printer = go.AddComponent<Teleprinter>();
            var code = Tmp(go, "Code"); var status = Tmp(go, "Status"); var message = Tmp(go, "Message"); var prev = Tmp(go, "Prev");
            Wire(printer, "frontCode", code); Wire(printer, "frontStatus", status);
            Wire(printer, "frontMessage", message); Wire(printer, "frontPrev", prev);
            var lines = new List<TelexLine>
            {
                new TelexLine { code = "001/00", status = "HAT AÇIK", message = "Analog hat açık." },
                new TelexLine { code = "001/01", status = "KAYIT VAR", message = "Üstüne yazılır." },
            };
            printer.Show(lines, false);
            StringAssert.Contains("001/01", code.text);
            StringAssert.Contains("KAYIT VAR", status.text);
            Assert.AreEqual("Üstüne yazılır.", message.text);
            StringAssert.Contains("001/00  HAT AÇIK", prev.text, "önceki satır kodu ve durumuyla");
            printer.Show(new List<TelexLine>(), false);
            Assert.AreEqual("", status.text);
            Assert.AreEqual("", prev.text);
        }

        [Test]
        public void RadioLogBook_PageListsEntriesWithAHangingIndent()
        {
            var go = Make("Radio log");
            var book = go.AddComponent<RadioLogBook>();
            var page = Tmp(go, "Page");
            Wire(book, "page", page);
            book.Show(new List<(string, string)> { ("014/01", "Alfa hazır"), ("014/02", "Alfa döndü · B. Avrupa 01 alındı") });
            var rows = page.text.Split('\n');
            Assert.AreEqual(2, rows.Length);
            StringAssert.Contains("014/01", rows[0]);
            StringAssert.Contains("<indent=", rows[1]);
            StringAssert.Contains("Alfa döndü", rows[1]);
            book.Show(null);
            Assert.AreEqual("", page.text);
        }

        [Test]
        public void PoseLightRecipe_AtMostFourShadowedLightsPerPose()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<PoseLightRecipe>(RecipePath);
            Assert.IsNotNull(recipe, RecipePath);
            foreach (SiginakPose pose in System.Enum.GetValues(typeof(SiginakPose)))
            {
                Assert.IsTrue(recipe.TryGet(pose, out var e), "tarifte poz yok: " + pose);
                Assert.LessOrEqual(PoseLightRecipe.ShadowedCount(e), PoseLightRecipe.MaxShadowed, pose.ToString());
                Assert.AreEqual(e.lights.Length, e.lights.Select(l => l.id).Distinct().Count(), "aynı ışık iki kez: " + pose);
            }
            recipe.TryGet(SiginakPose.Desk, out var k1);
            Assert.AreEqual(3, PoseLightRecipe.ShadowedCount(k1), "K1: ampul, masa lambası, pano lambası");
            recipe.TryGet(SiginakPose.Shelf, out var k4);
            Assert.AreEqual(1, PoseLightRecipe.ShadowedCount(k4), "K4: yalnız pano lambası");
            Assert.IsTrue(k4.lights.Single(l => l.id == "boardLamp").shadows);
            // Revizyon 3 · Aşama 4: K6 (ShiftClose) girdisi K6a'nın (Shift) kopyası; 2 gölgeli: ampul spotu + masa lambası (§3.13, E18).
            // Tek fark ortam dolgusu 0,4 → 0,3: kamerayı izleyen dolgu eldeki fişi 0,29 m'den aydınlatır; R19b_elde ve R19a_inik'le
            // bölge parlaklığına göre (kâğıt 209 → ~175, şalter gövdesi 99 → ~66'ya doğru)
            recipe.TryGet(SiginakPose.ShiftClose, out var k6);
            recipe.TryGet(SiginakPose.Shift, out var k6a);
            Assert.AreEqual(2, PoseLightRecipe.ShadowedCount(k6), "K6: ampul ve masa lambası");
            CollectionAssert.AreEquivalent(new[] { "bulbSpot", "deskLamp" }, k6.lights.Where(l => l.shadows).Select(l => l.id).ToArray());
            Assert.AreEqual(k6a.ambientScale, k6.ambientScale, 1e-5f, "K6 ortamı K6a'nınki");
            CollectionAssert.AreEquivalent(k6a.lights.Select(l => l.id + ":" + l.on + ":" + (l.id == "envFill" ? 0.3f : l.intensity) + ":" + l.shadows).ToArray(),
                k6.lights.Select(l => l.id + ":" + l.on + ":" + l.intensity + ":" + l.shadows).ToArray(), "K6 ışıkları K6a'nın kopyası (ortam dolgusu 0,3)");
            // Revizyon 3 · Aşama 6 (PLAN §1.5, §2.2 #11): K8A/K8G/K8R 3 gölgeli = ampul spotu + masa lambası + tezgâh lambası (counterSpot);
            // K8 aday varken de tezgâh lambası yanar (en çok 3; kabul dizisinde koridor ışığı tezgâh katlandıktan sonra gelir, DoorProp sürer)
            foreach (var pose in new[] { SiginakPose.Admission, SiginakPose.BreathGlass, SiginakPose.AdmissionRead })
            {
                recipe.TryGet(pose, out var e);
                Assert.AreEqual(3, PoseLightRecipe.ShadowedCount(e), pose + ": ampul, masa lambası, tezgâh lambası");
                CollectionAssert.AreEquivalent(new[] { "bulbSpot", "deskLamp", "counterSpot" }, e.lights.Where(l => l.on && l.shadows).Select(l => l.id).ToArray(), pose.ToString());
            }
            recipe.TryGet(SiginakPose.Door, out var k8);
            Assert.LessOrEqual(PoseLightRecipe.ShadowedCount(k8), 3, "K8 en çok 3 gölgeli (token lightsRecipe.K8)");
        }

        [Test]
        public void DeskGameScene_PoseLightingBindsEveryRecipeLight()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var room = scene.GetRootGameObjects().Single(g => g.name == "SiginakRoom").transform;
                var lighting = room.GetComponentsInChildren<PoseLighting>(true).Single();
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<PoseLightRecipe>(RecipePath), lighting.Recipe);
                var so = new SerializedObject(lighting);
                Assert.IsNotNull(so.FindProperty("rig").objectReferenceValue, "rig");
                Assert.IsNotNull(so.FindProperty("bulb").objectReferenceValue, "ampul kırpışması");
                var bound = Enumerable.Range(0, lighting.BindingCount).Select(lighting.BindingAt).ToList();
                Assert.IsTrue(bound.All(b => b.light != null), "boş bağlama");
                Assert.AreEqual(bound.Count, bound.Select(b => b.light).Distinct().Count(), "bir ışık iki kimlikte");
                var ids = lighting.Recipe.Entries.SelectMany(e => e.lights).Select(l => l.id).Distinct();
                foreach (var id in ids) Assert.IsTrue(bound.Any(b => b.id == id), "tarifteki ışık bağlı değil: " + id);
                // Kaydedilen hâl K1: gölgeli ışıklar en çok 4
                int shadowed = room.GetComponentsInChildren<Light>(true).Count(l => l.enabled && l.gameObject.activeInHierarchy && l.shadows != LightShadows.None);
                Assert.LessOrEqual(shadowed, PoseLightRecipe.MaxShadowed, "sahnede açık gölgeli ışık");
                var boardLamp = bound.Single(b => b.id == "boardLamp").light;
                Assert.AreEqual(LightType.Spot, boardLamp.type);
                Assert.AreEqual(116, boardLamp.spotAngle, 0.01f, "token board.lamp 58° (three.js yarım açısı) → Unity koni 116°");
                var env = bound.Single(b => b.id == "envFill").light;
                Assert.AreEqual(LightShadows.None, env.shadows, "ortam dolgusu gölgesiz");
                Assert.AreEqual(env, new SerializedObject(lighting).FindProperty("cameraFill").objectReferenceValue, "ortam dolgusu kamerayı izler");
                // R19: EVRAK hedef ışığı poz tarifinde yok (şiddetini tarif ezmesin; PLAN §1.7), kapalı ve gölgesiz kaydedilir
                var tray = room.GetComponentsInChildren<EvrakTray>(true).Single();
                var target = (Light)new SerializedObject(tray).FindProperty("targetLight").objectReferenceValue;
                Assert.IsNotNull(target, "EvrakTray.targetLight");
                Assert.IsFalse(bound.Any(b => b.light == target), "EVRAK hedef ışığı PoseLighting'e bağlı olmamalı");
                Assert.IsFalse(target.enabled, "EVRAK hedef ışığı kapalı kaydedilir");
                Assert.AreEqual(LightShadows.None, target.shadows, "EVRAK hedef ışığı gölgesiz");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        // Revizyon 3 · Aşama 10 (PLAN §2.2 F3): tarif testi yalnız asset'i, yukarıdaki sayım yalnız edit modda açık ışıkları görür. Oyunda
        // sonradan yanan bir ışık (kapı açılınca koridor lambası) Point + gölge ise 6 atlas dilimi yer; kural kapalı ışıkları da kapsar.
        [TestCase(ScenePath)]
        [TestCase(EntrancePath)]
        public void Scene_EveryShadowedLightIsASpot_EvenWhenDisabled(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var lights = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).ToList();
                Assert.IsNotEmpty(lights, path);
                var wrong = lights.Where(l => l.shadows != LightShadows.None && l.type != LightType.Spot)
                    .Select(l => Rev3SceneRefAudit.PathOf(l.transform) + " (" + l.type + ")").ToList();
                Assert.IsEmpty(wrong, "gölgeyi yalnız spot taşır (küp gölge 6 dilim)");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        // Aşama 10 (F1, D7 = a): URP "Custom" kademesinde m_CustomResolution'ı değil Light.shadowResolution'ı okur; −1 kalınca 65535 px ister
        // ve atlas yarıya inerek küçülür (tezgâh lambası 32 px'e düşüyordu).
        [TestCase("bulbSpot")]
        [TestCase("deskLamp")]
        [TestCase("boardLamp")]
        public void DeskGameScene_MainShadowLightsAskFor2048(string id)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var lighting = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PoseLighting>(true)).Single();
                var light = Enumerable.Range(0, lighting.BindingCount).Select(lighting.BindingAt).Single(b => b.id == id).light;
                Assert.AreEqual(2048, (int)light.shadowResolution, id);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
