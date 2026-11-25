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
    /// KOS aşama 4 kaydedilmiş DeskGame: olay denetleyicisi tek ve bütün bağları dolu, tutanak EVRAK tepsisinin altında kayıtlı (soru 8;
    /// dinlenme çapası tepside), kapıdan gelen kâğıdın çapası "SiginakRoom/Kapı panosu" altında (Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md
    /// §A), kapı vuruşu için kabul köprüsü bağlı; sunucu ödülünün kâğıdı da aynı panoya gelir. Çarpıştırıcı kapalı kaydedilmiş, masa ve yüzey
    /// denetleyiciye bağlı. Yeni Runtime dosyaları ad aramaz ve <c>Loc</c> çağırmaz.
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
            "Assets/Corrigible/Desk/Scripts/Runtime/Siginak/PassDrawer.cs",
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

        /// <summary>Belge çekmecesinin kayan tablası (2026-11-23; kapı panosunun yerine): SiginakRoom/Belge çekmecesi/Tabla, EVRAK tepsisinin
        /// dışında; tablanın PartDriver'ı çekmecenin <see cref="PassDrawer"/>'ına bağlı.</summary>
        Transform DoorBoard(EvrakTray tray)
        {
            var room = scene.GetRootGameObjects().Single(g => g.name == "SiginakRoom").transform;
            Assert.IsNull(room.Find("Kapı panosu"), "kapı panosu kalktı");
            var drawer = room.Find("Belge çekmecesi");
            Assert.IsNotNull(drawer, "SiginakRoom/Belge çekmecesi");
            var pass = drawer.GetComponent<PassDrawer>();
            Assert.IsNotNull(pass, "PassDrawer");
            var tabla = drawer.Find("Tabla");
            Assert.IsNotNull(tabla, "Belge çekmecesi/Tabla");
            Assert.AreSame(tabla.GetComponent<PartDriver>(), pass.Drawer, "tabla çekmecenin kayan parçası");
            Assert.IsNotNull(pass.LampPart, "lamba");
            Assert.IsNotNull(pass.Lever, "şalter kolu");
            Assert.IsNotNull(pass.DrawerTarget, "çekmecenin hedefi");
            Assert.IsNotNull(pass.SwitchTarget, "şalterin hedefi");
            Assert.IsFalse(tabla.IsChildOf(tray.transform), "çekmece tepsinin dışında");
            return tabla;
        }

        /// <summary>Çekmecenin köprüsü (<see cref="SiginakPassDrawer"/>) tek ve iki belge köprüsüne bağlı; iki köprü de ona bağlı.</summary>
        [Test]
        public void PassDrawer_IsWired()
        {
            Open();
            var bridge = All<SiginakPassDrawer>().Single();
            var so = new SerializedObject(bridge);
            foreach (string field in new[] { "game", "drawer", "shelterEvent", "serverReward", "desk", "hoverTag" })
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, "SiginakPassDrawer." + field + " bağlı değil");
            Assert.AreSame(All<PassDrawer>().Single(), so.FindProperty("drawer").objectReferenceValue);
            Assert.AreSame(bridge, new SerializedObject(All<SiginakShelterEvent>().Single()).FindProperty("passDrawer").objectReferenceValue, "SiginakShelterEvent.passDrawer");
            Assert.AreSame(bridge, new SerializedObject(All<SiginakServerReward>().Single()).FindProperty("passDrawer").objectReferenceValue, "SiginakServerReward.passDrawer");
        }

        [Test]
        public void Controller_IsWired_PaperComesToTheDoor_AndRestsOnTheTray()
        {
            Open();
            var flows = All<SiginakShelterEvent>();
            Assert.AreEqual(1, flows.Length, "tek olay denetleyicisi");
            var flow = flows[0];
            var so = new SerializedObject(flow);
            foreach (string field in new[] { "game", "surface", "rig", "telex", "desk", "tray", "trayTarget", "view", "hoverTag", "admission" })
            {
                var p = so.FindProperty(field);
                Assert.IsNotNull(p, "alan: " + field);
                Assert.IsNotNull(p.objectReferenceValue, "SiginakShelterEvent." + field + " bağlı değil");
            }
            Assert.AreSame(flow, new SerializedObject(All<SiginakDesk>().Single()).FindProperty("shelterEvent").objectReferenceValue, "SiginakDesk.shelterEvent");
            Assert.AreSame(flow, new SerializedObject(All<LiberationDeskView>().Single()).FindProperty("shelterEvent").objectReferenceValue, "LiberationDeskView.shelterEvent");

            var view = (ShelterEventPaperView)so.FindProperty("view").objectReferenceValue;
            var tray = (EvrakTray)so.FindProperty("tray").objectReferenceValue;
            // Tesis aşama 9: görünümün ikinci örneği sunucu ödülünün kâğıdıdır (SiginakServerReward.view); tutanak tektir
            var reward = (ShelterEventPaperView)new SerializedObject(All<SiginakServerReward>().Single()).FindProperty("view").objectReferenceValue;
            Assert.AreNotSame(view, reward, "ödül kâğıdı tutanaktan ayrı");
            Assert.AreEqual(1, All<ShelterEventPaperView>().Count(v => v != reward), "tek tutanak");
            Assert.IsTrue(view.transform.IsChildOf(tray.transform), "tutanak EVRAK tepsisinin altında");
            Assert.IsNotNull(view.RestAnchor, "dinlenme çapası");
            Assert.IsTrue(view.RestAnchor.IsChildOf(tray.transform), "dinlenme çapası tepside");
            // Kapı belgeleri: tutanak belge çekmecesinin tablasına iner; kapı vuruşu kabul köprüsünden (KnockDelivery)
            var board = DoorBoard(tray);
            Assert.IsNotNull(view.DoorAnchor, "kapı panosu çapası (doorAnchor) bağlı");
            Assert.AreEqual("Olay tutanağı kapıda", view.DoorAnchor.name);
            Assert.AreSame(board, view.DoorAnchor.parent, "kapı çapası çekmecenin tablasının çocuğu");
            Assert.IsFalse(view.DoorAnchor.IsChildOf(tray.transform), "kapı çapası tepside değil");
            Assert.AreSame(All<SiginakAdmission>().Single(), so.FindProperty("admission").objectReferenceValue, "SiginakShelterEvent.admission sahnenin kabul köprüsü");
            Assert.IsNotNull(view.HitArea, "çarpıştırıcı");
            Assert.IsFalse(view.HitArea.enabled, "çarpıştırıcı kapalı kaydedilmiş (yalnız veri varken açılır)");
            Assert.IsNotNull(view.Face, "yüz");
            var trayTarget = (DeskTarget)so.FindProperty("trayTarget").objectReferenceValue;
            Assert.AreSame(trayTarget, new SerializedObject(All<SiginakDesk>().Single()).FindProperty("trayTarget").objectReferenceValue, "EVRAK hedefi masanınkiyle aynı");
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(view.gameObject), "tutanak prefab örneği (ShelterEventPaper.prefab)");
        }

        /// <summary>Tesis aşama 9 ve kapı belgeleri: sunucu ödülünün kâğıdı da kapı panosuna gelir ("Sunucu ödülü kapıda"), okunup
        /// bırakılınca EVRAK tepsisine iner; kayıtlı yeri tepsinin altı, kapı vuruşu kabul köprüsünden.</summary>
        [Test]
        public void ServerReward_IsWired_PaperComesToTheDoor_AndRestsOnTheTray()
        {
            Open();
            var reward = All<SiginakServerReward>().Single();
            var so = new SerializedObject(reward);
            foreach (string field in new[] { "game", "surface", "rig", "telex", "tray", "trayTarget", "view", "shelterEvent", "hoverTag", "admission" })
            {
                var p = so.FindProperty(field);
                Assert.IsNotNull(p, "alan: " + field);
                Assert.IsNotNull(p.objectReferenceValue, "SiginakServerReward." + field + " bağlı değil");
            }
            Assert.AreSame(All<SiginakShelterEvent>().Single(), so.FindProperty("shelterEvent").objectReferenceValue, "SiginakServerReward.shelterEvent");
            Assert.AreSame(All<SiginakAdmission>().Single(), so.FindProperty("admission").objectReferenceValue, "SiginakServerReward.admission sahnenin kabul köprüsü");

            var view = (ShelterEventPaperView)so.FindProperty("view").objectReferenceValue;
            var tray = (EvrakTray)so.FindProperty("tray").objectReferenceValue;
            Assert.IsTrue(view.transform.IsChildOf(tray.transform), "ödül kâğıdı kayıtlı hâlde EVRAK tepsisinin altında");
            Assert.IsNotNull(view.RestAnchor, "dinlenme çapası");
            Assert.IsTrue(view.RestAnchor.IsChildOf(tray.transform), "dinlenme çapası tepside");
            var board = DoorBoard(tray);
            Assert.IsNotNull(view.DoorAnchor, "kapı panosu çapası (doorAnchor) bağlı");
            Assert.AreEqual("Sunucu ödülü kapıda", view.DoorAnchor.name);
            Assert.AreSame(board, view.DoorAnchor.parent, "kapı çapası çekmecenin tablasının çocuğu");
            Assert.IsFalse(view.DoorAnchor.IsChildOf(tray.transform), "kapı çapası tepside değil");
            var eventView = (ShelterEventPaperView)new SerializedObject(All<SiginakShelterEvent>().Single()).FindProperty("view").objectReferenceValue;
            Assert.AreNotSame(eventView.DoorAnchor, view.DoorAnchor, "iki kâğıdın panoda ayrı çapası var");
            Assert.IsNotNull(view.HitArea, "çarpıştırıcı");
            Assert.IsFalse(view.HitArea.enabled, "çarpıştırıcı kapalı kaydedilmiş (yalnız veri varken açılır)");
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
