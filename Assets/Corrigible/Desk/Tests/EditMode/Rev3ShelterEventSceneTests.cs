using System.IO;
using System.Linq;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// KOS aşama 4 kaydedilmiş DeskGame: olay denetleyicisi tek ve bütün bağları dolu, tutanak EVRAK tepsisinin altında (soru 8),
    /// çarpıştırıcısı kapalı kaydedilmiş, masa ve yüzey denetleyiciye bağlı. Yeni Runtime dosyaları ad aramaz ve <c>Loc</c> çağırmaz.
    /// Sahne gerektiren testler kullanıcının açık DeskGame'ine dokunmaz (<c>Assert.Ignore</c>); boş sahnede koş.
    /// </summary>
    public sealed class Rev3ShelterEventSceneTests
    {
        const string ScenePath = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";
        static readonly string[] RuntimeFiles =
        {
            "Assets/Corrigible/Desk/Scripts/Runtime/Siginak/ShelterEventPaperData.cs",
            "Assets/Corrigible/Desk/Scripts/Runtime/Siginak/ShelterEventPaperFace.cs",
            "Assets/Corrigible/Desk/Scripts/Runtime/Siginak/ShelterEventPaperView.cs",
        };

        Scene scene, previous;
        bool opened;

        void Open()
        {
            if (opened) return;
            var existing = SceneManager.GetSceneByPath(ScenePath);
            if (existing.IsValid() && existing.isLoaded)
                Assert.Ignore("Sahne doğrulaması boş sahne açıkken çalıştırılmalı; kullanıcının açık sahnesine dokunulmadı.");
            previous = SceneManager.GetActiveScene();
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            opened = true;
        }

        [OneTimeTearDown]
        public void CloseWithoutSaving()
        {
            if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            opened = false;
        }

        T[] All<T>() where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void Controller_IsWired_AndPaperRestsOnTheTray()
        {
            Open();
            var flows = All<SiginakShelterEvent>();
            Assert.AreEqual(1, flows.Length, "tek olay denetleyicisi");
            var flow = flows[0];
            var so = new SerializedObject(flow);
            foreach (string field in new[] { "game", "surface", "rig", "telex", "desk", "tray", "trayTarget", "view", "hoverTag" })
            {
                var p = so.FindProperty(field);
                Assert.IsNotNull(p, "alan: " + field);
                Assert.IsNotNull(p.objectReferenceValue, "SiginakShelterEvent." + field + " bağlı değil");
            }
            Assert.AreSame(flow, new SerializedObject(All<SiginakDesk>().Single()).FindProperty("shelterEvent").objectReferenceValue, "SiginakDesk.shelterEvent");
            Assert.AreSame(flow, new SerializedObject(All<LiberationDeskView>().Single()).FindProperty("shelterEvent").objectReferenceValue, "LiberationDeskView.shelterEvent");

            var view = (ShelterEventPaperView)so.FindProperty("view").objectReferenceValue;
            var tray = (EvrakTray)so.FindProperty("tray").objectReferenceValue;
            Assert.AreEqual(1, All<ShelterEventPaperView>().Length, "tek tutanak");
            Assert.IsTrue(view.transform.IsChildOf(tray.transform), "tutanak EVRAK tepsisinin altında");
            Assert.IsNotNull(view.RestAnchor, "dinlenme çapası");
            Assert.IsTrue(view.RestAnchor.IsChildOf(tray.transform), "dinlenme çapası tepside");
            Assert.IsNotNull(view.HitArea, "çarpıştırıcı");
            Assert.IsFalse(view.HitArea.enabled, "çarpıştırıcı kapalı kaydedilmiş (yalnız veri varken açılır)");
            Assert.IsNotNull(view.Face, "yüz");
            var trayTarget = (DeskTarget)so.FindProperty("trayTarget").objectReferenceValue;
            Assert.AreSame(trayTarget, new SerializedObject(All<SiginakDesk>().Single()).FindProperty("trayTarget").objectReferenceValue, "EVRAK hedefi masanınkiyle aynı");
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(view.gameObject), "tutanak prefab örneği (ShelterEventPaper.prefab)");
        }

        [Test]
        public void RuntimeFiles_NoNameLookup_NoLoc()
        {
            foreach (string path in RuntimeFiles)
            {
                Assert.IsTrue(File.Exists(path), path);
                string source = File.ReadAllText(path);
                foreach (string banned in new[] { "Find(\"", "GameObject.Find", "FindObjectOfType", "FindObjectsOfType", "FindFirstObjectByType", "FindAnyObjectByType", "Loc.T(", "Loc.F(" })
                    Assert.IsFalse(source.Contains(banned), path + ": " + banned);
            }
        }
    }
}
