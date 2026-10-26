using System.IO;
using System.Linq;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 2: oda kurulumu, koordinat dönüşümü, sunum anahtarı ve oda yazıları.</summary>
    public class SiginakRoomTests
    {
        const string ScenePath = DeskSpriteManifest.Root + "/Scenes/DeskGame.unity";

        static void Near(Quaternion expected, Quaternion actual, string what) =>
            Assert.Less(Quaternion.Angle(expected, actual), 0.01f, what);

        /// <summary>three.js (sağ el, z koltuğa) → Unity (sol el, z duvara): X ve Y açısı işaret değiştirir, Z değiştirmez.</summary>
        [Test]
        public void ThreeEuler_MatchesMirroredThreeRotation()
        {
            const float a = 0.6f;
            Near(Quaternion.Euler(-a * Mathf.Rad2Deg, 0, 0), SiginakRoomBuilder.ThreeEuler(a, 0, 0), "x");
            Near(Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), SiginakRoomBuilder.ThreeEuler(0, a, 0), "y");
            Near(Quaternion.Euler(0, 0, a * Mathf.Rad2Deg), SiginakRoomBuilder.ThreeEuler(0, 0, a), "z (README'nin aksine işaret korunur)");

            // Birleşik açı: three'de (Rx·Ry·Rz) döndürüp z'yi aynalamak = Unity'de döndürmek
            float rx = 0.3f, ry = -0.5f, rz = 0.7f;
            var three = new Quaternion(Mathf.Sin(rx / 2), 0, 0, Mathf.Cos(rx / 2)) * new Quaternion(0, Mathf.Sin(ry / 2), 0, Mathf.Cos(ry / 2)) *
                        new Quaternion(0, 0, Mathf.Sin(rz / 2), Mathf.Cos(rz / 2));
            var v = new Vector3(0.2f, -0.7f, 0.4f);
            var mirrored = three * new Vector3(v.x, v.y, -v.z);
            var expected = new Vector3(mirrored.x, mirrored.y, -mirrored.z);
            Assert.Less(Vector3.Distance(expected, SiginakRoomBuilder.ThreeEuler(rx, ry, rz) * v), 1e-5f);
        }

        [Test]
        public void DeskGameScene_SiginakRoomIsWired_NewByDefault()
        {
            Assert.IsTrue(SiginakSettings.Active.newPresentation, "Aşama 10'dan beri varsayılan yeni görünüm (anahtar açılışta uygular)");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var sw = roots.SelectMany(g => g.GetComponentsInChildren<SiginakPresentationSwitch>(true)).Single();
                var so = new SerializedObject(sw);
                foreach (var field in new[] { "settings", "siginakRoot", "mapCanvas", "legacyMapParent", "siginakMapMount", "mainCamera", "legacyCameraRig", "cameraRig", "launcher", "crtDivePose", "legacyMonitorPose" })
                    Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, field);
                var legacy = so.FindProperty("legacyObjects");
                Assert.AreEqual(13, legacy.arraySize, "eski oda + LiberationHudCanvas (üst HUD, alt çubuk, sonuç notu)");
                for (int i = 0; i < legacy.arraySize; i++) Assert.IsNotNull(legacy.GetArrayElementAtIndex(i).objectReferenceValue, "eski nesne " + i);

                var room = (GameObject)so.FindProperty("siginakRoot").objectReferenceValue;
                Assert.IsFalse(room.activeSelf, "sahne dosyası eski görünümle kaydedilir; yeni görünümü anahtar Awake'te açar");
                foreach (var path in new[] { "Shell", "Desk", "Desk/MapMount", "Desk/Counter bank", "Desk/Key rail", "Desk/Shift switch", "Desk/Teleprinter",
                             "Shelf", "Shelf/Diorama", "CRT desk", "CRT desk/CRT/Screen", "Ceiling bulb", "Poses/K1", "Poses/Map", "Poses/K4", "Poses/K5a", "Poses/K6a" })
                    Assert.IsNotNull(room.transform.Find(path), path);
                var mount = room.transform.Find("Desk/MapMount");
                Assert.Less(Vector3.Distance(new Vector3(0, 0.7612f, 1.3175f), mount.position), 1e-4f);

                var lights = room.GetComponentsInChildren<Light>(true);
                var bulb = lights.Single(l => l.name == "Bulb light");
                Assert.AreEqual(LightShadows.Soft, bulb.shadows);
                Assert.AreEqual(1024, bulb.shadowCustomResolution);
                var lamp = lights.Single(l => l.name == "Desk lamp light");
                Assert.AreEqual(LightType.Spot, lamp.type);
                Assert.AreEqual(2048, lamp.shadowCustomResolution);
                Assert.IsNotNull(room.GetComponentInChildren<BulbFlicker>(true));
                // Primitive çarpıştırıcıları silinir; kalanların hepsi tıklanır hedeflerindir (Aşama 4)
                Assert.IsTrue(room.GetComponentsInChildren<Collider>(true).All(c => c.GetComponent<DeskTarget>() != null), "hedef dışı çarpıştırıcı");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>Anahtar haritayı yeni masaya taşır, eski odayı kapatır, kamerayı K1'e koyar; geri alınca her şey eski yerinde.</summary>
        [Test]
        public void PresentationSwitch_AppliesAndRestores()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var sw = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SiginakPresentationSwitch>(true)).Single();
                var so = new SerializedObject(sw);
                var map = (RectTransform)so.FindProperty("mapCanvas").objectReferenceValue;
                var legacyParent = map.parent;
                var legacyScale = map.localScale;
                var cam = sw.MainCamera;
                var camPos = cam.transform.position;
                var oldRoom = (GameObject)so.FindProperty("legacyObjects").GetArrayElementAtIndex(0).objectReferenceValue;

                sw.Apply(true);
                Assert.IsTrue(sw.IsNew);
                Assert.AreSame(so.FindProperty("siginakMapMount").objectReferenceValue, map.parent);
                Assert.AreEqual(1.2f / 1920f, map.localScale.x, 1e-7f);
                Assert.IsFalse(oldRoom.activeSelf);
                Assert.IsTrue(cam.usePhysicalProperties);
                Assert.AreEqual(-0.324f, cam.lensShift.y, 1e-4f);
                Assert.AreEqual(56.7f, Camera.FocalLengthToFieldOfView(cam.focalLength, cam.sensorSize.y), 0.05f);

                sw.Apply(false);
                Assert.IsFalse(sw.IsNew);
                Assert.AreSame(legacyParent, map.parent);
                Assert.AreEqual(legacyScale, map.localScale);
                Assert.IsTrue(oldRoom.activeSelf);
                Assert.IsFalse(cam.usePhysicalProperties);
                Assert.AreEqual(camPos, cam.transform.position);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>Odadaki her yazı: çevrilebilir sabit metinse kaynak = yazı; fontun statik atlası her glifi içerir (TR + EN).</summary>
        [Test]
        public void RoomTexts_AreLocalizableAndCovered()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var room = scene.GetRootGameObjects().Single(g => g.name == SiginakRoomBuilder.RootName);
                var texts = room.GetComponentsInChildren<TextMeshPro>(true);
                Assert.Greater(texts.Length, 30);
                var table = Corrigible.Core.Loc.ParseTable(File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"));
                var english = table[Corrigible.Core.Loc.English];
                foreach (var t in texts)
                {
                    Assert.AreEqual("", SiginakFontPreview.MissingStatic(t.font, t.text), t.name + " eksik glif (TR)");
                    var loc = t.GetComponent<DeskLocText>();
                    if (loc == null) continue;
                    Assert.AreEqual(t.text, loc.Source, t.name);
                    Assert.IsTrue(english.TryGetValue(loc.Source, out var en) && !string.IsNullOrEmpty(en), "çevirisi yok: " + loc.Source);
                    Assert.AreEqual("", SiginakFontPreview.MissingStatic(t.font, en), t.name + " eksik glif (EN)");
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void RoomMaterials_ComeFromTokens()
        {
            var mats = Directory.GetFiles(SiginakMaterials.Dir, "*.mat", SearchOption.AllDirectories)
                .Select(p => AssetDatabase.LoadAssetAtPath<Material>(p.Replace('\\', '/'))).ToList();
            Assert.Greater(mats.Count, 50);
            foreach (var m in mats)
            {
                Assert.IsTrue(m.shader.name.StartsWith("Universal Render Pipeline/") || m.shader.name.StartsWith("TextMeshPro/"), m.name + ": " + m.shader.name);
                if (m.HasProperty("_BaseColor")) Assert.AreNotEqual(Color.magenta, m.GetColor("_BaseColor"), m.name + ": eksik token");
            }
        }
    }
}
