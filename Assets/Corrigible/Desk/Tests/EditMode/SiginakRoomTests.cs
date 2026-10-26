using System.IO;
using System.Linq;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Sığınak odası (Aşama 2, Aşama 10'dan beri DeskGame'in tek sunumu): sahnenin kurulu hâli, baskın bağlantısı, eski
    /// komuta odasının kalmadığı, oda yazıları ve malzemeler.
    /// </summary>
    public class SiginakRoomTests
    {
        const string ScenePath = DeskSpriteManifest.Root + "/Scenes/DeskGame.unity";

        [Test]
        public void DeskGameScene_SiginakRoomIsTheOnlyPresentation()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var room = roots.Single(g => g.name == "SiginakRoom");
                Assert.IsTrue(room.activeSelf, "oda sahnede açık kayıtlı");
                foreach (var path in new[] { "Shell", "Desk", "Desk/MapMount", "Desk/Counter bank", "Desk/Key rail", "Desk/Shift switch", "Desk/Teleprinter",
                             "Shelf", "Shelf/Diorama", "CRT desk", "CRT desk/CRT/Screen", "Ceiling bulb", "Poses/K1", "Poses/Map", "Poses/K4", "Poses/K5a", "Poses/K6a" })
                    Assert.IsNotNull(room.transform.Find(path), path);
                var mount = room.transform.Find("Desk/MapMount");
                Assert.Less(Vector3.Distance(new Vector3(0, 0.7612f, 1.3175f), mount.position), 1e-4f);

                // Eski komuta odası, eski HUD / alt çubuk, eski hikâye kâğıdı ve eski görev fişi yok
                var commandPost = roots.Single(g => g.name == "CommandPost3D").transform;
                Assert.AreEqual(new[] { "Main Camera" }, commandPost.Cast<Transform>().Select(t => t.name).ToArray());
                Assert.IsFalse(roots.Any(g => g.name == "LiberationMapCanvas"));

                // Kâğıt harita odanın masasında (1,2 m), kamera fiziksel ve K1'de, odanın fizik ışını açık
                var map = roots.SelectMany(g => g.GetComponentsInChildren<SadeMapView>(true)).Single();
                Assert.IsTrue(map.transform.IsChildOf(mount));
                Assert.AreEqual(1.2f / 1920f, map.transform.parent.localScale.x * map.transform.localScale.x, 1e-7f);
                var cam = commandPost.GetComponentInChildren<Camera>(true);
                var rig = cam.GetComponent<SiginakCameraRig>();
                Assert.IsTrue(rig.enabled);
                Assert.IsTrue(cam.usePhysicalProperties);
                Assert.AreEqual(-0.324f, cam.lensShift.y, 1e-4f);
                Assert.IsTrue(cam.GetComponent<PhysicsRaycaster>().enabled);

                // Baskın bağlantısı: CRT geçişi, monitöre yaklaşma pozu, baskında odanın gizlenmesi
                var link = roots.SelectMany(g => g.GetComponentsInChildren<SiginakRaidLink>(true)).Single();
                Assert.IsNull(link.transform.parent, "odanın kökünün dışında (oda gizliyken de çalışır)");
                var ls = new SerializedObject(link);
                Assert.AreSame(room, ls.FindProperty("room").objectReferenceValue);
                Assert.IsInstanceOf<SiginakCrt>(ls.FindProperty("transition").objectReferenceValue);
                var launcher = (Corrigible.Game.Raid.RaidLauncher)ls.FindProperty("launcher").objectReferenceValue;
                Assert.AreEqual("CrtDive", launcher.MonitorFocusPose.name);

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
                // Hareket etmeyen parçalar statik birleşir (Aşama 10 performans)
                Assert.IsTrue(GameObjectUtility.GetStaticEditorFlags(room.transform.Find("Desk/Top").gameObject).HasFlag(StaticEditorFlags.BatchingStatic));
                Assert.IsFalse(GameObjectUtility.GetStaticEditorFlags(room.GetComponentInChildren<StationKey>(true).gameObject).HasFlag(StaticEditorFlags.BatchingStatic));
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
                var room = scene.GetRootGameObjects().Single(g => g.name == "SiginakRoom");
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
            var mats = Directory.GetFiles(SiginakSpriteManifest.Root + "/Materials", "*.mat", SearchOption.AllDirectories)
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
