using System.Linq;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using Corrigible.Game.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Ayarlar dolabı ve menü sahnesi (Docs/AYARLAR-DOLABI-VE-MENU.md): sürgünün değer hesabı, prefab'ın parçaları, DeskGame'deki
    /// bağlantılar (K9 pozu, bağlayıcı, eski kartın kalkması), menü sahnesinin bağlantıları ve build sırası.
    /// </summary>
    public class SettingsCabinetTests
    {
        const string DeskScene = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";
        const string EntranceScene = "Assets/Corrigible/Desk/Scenes/Giris.unity";
        const string PrefabPath = "Assets/Corrigible/Desk/Prefabs/Siginak/SettingsCabinet.prefab";

        static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        static SerializedProperty P(Object owner, string field) => new SerializedObject(owner).FindProperty(field);
        static Object Ref(Object owner, string field) => P(owner, field).objectReferenceValue;

        static void InScene(string path, System.Action<Scene> check)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try { check(scene); }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void Fader_QuantizesToFivePercentSteps()
        {
            Assert.AreEqual(0.55f, CabinetFader.Quantize(0.53f), 1e-5f);
            Assert.AreEqual(0.5f, CabinetFader.Quantize(0.52f), 1e-5f);
            Assert.AreEqual(0f, CabinetFader.Quantize(-0.2f), 1e-5f);
            Assert.AreEqual(1f, CabinetFader.Quantize(1.3f), 1e-5f);
            Assert.AreEqual(0f, CabinetFader.Quantize(0.024f), 1e-5f);
        }

        [Test]
        public void Fader_ValueFollowsTheHandleAlongTheSlot()
        {
            Assert.AreEqual(0f, CabinetFader.ValueAt(-0.08f, -0.08f, 0.08f), 1e-5f);
            Assert.AreEqual(1f, CabinetFader.ValueAt(0.08f, -0.08f, 0.08f), 1e-5f);
            Assert.AreEqual(0.5f, CabinetFader.ValueAt(0f, -0.08f, 0.08f), 1e-5f);
            Assert.AreEqual(1f, CabinetFader.ValueAt(0.3f, -0.08f, 0.08f), 1e-5f, "yarığın dışı kıstırılır");
        }

        [Test]
        public void Fader_DetentEveryTenPercent_HalfDetentAtFive()
        {
            Assert.AreEqual(false, CabinetFader.DetentBetween(0.50f, 0.55f), "%5: yarım çentik");
            Assert.AreEqual(true, CabinetFader.DetentBetween(0.55f, 0.60f), "%10: büyük çentik");
            Assert.IsNull(CabinetFader.DetentBetween(0.6f, 0.6f));
        }

        [Test]
        public void Briefing_FromTheEntrance_HasNoDossier()
        {
            Assert.IsFalse(BriefingBuilder.Build(false, null, false).NoDossier);
            Assert.IsTrue(BriefingBuilder.Build(false, null, false, false).NoDossier);
        }

        [Test]
        public void GameLaunch_IntentIsReadOnce()
        {
            GameLaunch.Pending = GameLaunch.Mode.Continue;
            Assert.AreEqual(GameLaunch.Mode.Continue, GameLaunch.Take());
            Assert.AreEqual(GameLaunch.Mode.None, GameLaunch.Take());
        }

        [Test]
        public void CabinetPrefab_HasTheControlsAndTheirParts()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, PrefabPath);
            var cabinet = prefab.GetComponent<SettingsCabinet>();
            foreach (var f in new[] { "closedTarget", "leafTarget", "leafHinge", "latch", "interior", "bulb", "bulbGlass", "bulbOn", "bulbOff", "cameraAnchor", "sound", "language", "exit" })
                Assert.IsNotNull(Ref(cabinet, f), f);
            var channels = P(cabinet, "channels");
            Assert.AreEqual(4, channels.arraySize, "genel, müzik, ortam, efekt");
            for (int i = 0; i < 4; i++)
            {
                var fader = (CabinetFader)channels.GetArrayElementAtIndex(i).objectReferenceValue;
                foreach (var f in new[] { "target", "handle", "needle", "lamp", "lampOn", "lampOff" }) Assert.IsNotNull(Ref(fader, f), fader.name + "." + f);
                var digits = P(fader, "digits");
                Assert.AreEqual(3, digits.arraySize, fader.name);
                for (int k = 0; k < 3; k++) Assert.IsNotNull(digits.GetArrayElementAtIndex(k).objectReferenceValue, fader.name + " teker " + k);
                Assert.Greater(P(fader, "travelMax").floatValue, P(fader, "travelMin").floatValue, fader.name);
            }
            Assert.AreEqual(2, P(Ref(cabinet, "language"), "angles").arraySize, "Türkçe, English");
            // Her tıklanır parçanın üzerine gelme başlığı var (çeviri aracı toplar)
            foreach (var t in prefab.GetComponentsInChildren<DeskTarget>(true))
                Assert.IsNotEmpty(P(t, "title").stringValue, t.name);
            // Dil kademe anahtarının yazıları kendi dillerinde ve sabit
            var labels = prefab.GetComponentsInChildren<TMPro.TMP_Text>(true);
            foreach (var fixedText in new[] { "TÜRKÇE", "ENGLISH", "DİL · LANGUAGE" })
            {
                var t = labels.Single(l => l.text == fixedText);
                Assert.IsNull(t.GetComponent<DeskLocText>(), fixedText + " çevrilmez");
            }
            Assert.IsNotNull(labels.Single(l => l.text == "MÜZİK").GetComponent<DeskLocText>());
        }

        [Test]
        public void DeskGameScene_CabinetIsWired()
        {
            InScene(DeskScene, scene =>
            {
                var cabinet = All<SettingsCabinet>(scene).Single();
                Assert.IsInstanceOf<HoverTag>(Ref(cabinet, "hoverTag"), "sahnenin etiketi dolaba bağlı");
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(cabinet), "dolap prefab'ın örneği");
                var rig = All<SiginakCameraRig>(scene).Single();
                Assert.IsTrue(rig.TryGetPose(SiginakPose.Cabinet, out var slot), "K9 pozu");
                Assert.AreSame(cabinet.CameraAnchor, slot.anchor);
                Assert.AreSame(cabinet, Ref(All<DeskPresenter>(scene).Single(), "settingsCabinet"));
                var bridge = All<SiginakCabinet>(scene).Single();
                foreach (var f in new[] { "cabinet", "rig", "settings", "game", "surface", "telex", "briefing", "opening", "view" })
                    Assert.IsNotNull(Ref(bridge, f), f);
                Assert.AreSame(bridge, Ref(All<LiberationDeskView>(scene).Single(), "siginakCabinet"));
                Assert.IsNotNull(Ref(cabinet, "exit"), "odada MENÜYE DÖN kolu var");
                Assert.IsFalse(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Any(t => t.name == "SettingsCard"),
                    "eski ayarlar kartı kalktı");
                // Ekranda AYARLAR düğmesi yok: kenar satırında çip yok, sağ üstteki plaka yalnız ATLA
                var hud = All<HoverTag>(scene).Single().transform;
                Assert.IsFalse(hud.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t => t.text.Trim() == "AYARLAR"), "ekranda AYARLAR yazısı");
                var plaque = All<SettingsPlaque>(scene).Single();
                Assert.IsFalse(((GameObject)Ref(plaque, "plaque")).activeSelf, "plaka açılışta gizli (yalnız dizide ATLA)");
            });
        }

        [Test]
        public void EntranceScene_IsWired_AndOpensFirst()
        {
            var scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            Assert.AreEqual(EntranceScene, scenes[0], "menü sahnesi build'de ilk");
            CollectionAssert.Contains(scenes, DeskScene);
            CollectionAssert.Contains(scenes, "Assets/Corrigible/Scenes/Baskin.unity");
            Assert.AreEqual(GameLaunch.EntranceScene, System.IO.Path.GetFileNameWithoutExtension(EntranceScene));
            Assert.AreEqual(GameLaunch.DeskScene, System.IO.Path.GetFileNameWithoutExtension(DeskScene));
            InScene(EntranceScene, scene =>
            {
                var ctrl = All<EntranceController>(scene).Single();
                foreach (var f in new[] { "cam", "overview", "clockPose", "threshold", "inside", "cabinet", "settings", "clock", "door", "doorTarget", "mainSwitch", "fade", "glassOn", "glassOff" })
                    Assert.IsNotNull(Ref(ctrl, f), f);
                Assert.AreEqual(3, P(ctrl, "lamps").arraySize, "sahanlığın üç lambası");
                var cabinet = (SettingsCabinet)Ref(ctrl, "cabinet");
                Assert.IsInstanceOf<HoverTag>(Ref(cabinet, "hoverTag"));
                Assert.IsNull(Ref(cabinet, "exit"), "menüde MENÜYE DÖN yok");
                Assert.IsFalse(cabinet.transform.Find("Interior/Exit lever").gameObject.activeSelf);
                var clock = (TimeClock)Ref(ctrl, "clock");
                foreach (var f in new[] { "clockTarget", "cardTarget", "card", "cardSummary", "cardTilt", "voidTag", "voidText", "stackTarget", "blank", "clip", "slotAbove", "slotIn", "plunger" })
                    Assert.IsNotNull(Ref(clock, f), f);
                Assert.AreEqual(80, P(clock, "punches").arraySize);
                var door = (DoorProp)Ref(ctrl, "door");
                Assert.AreEqual("Wheel outside", ((Transform)Ref(door, "wheel")).name, "kapı dışarıdaki çarkla açılır");
                Assert.IsNull(Ref(door, "silhouette"));
                Assert.AreEqual(1, P(door, "stairLights").arraySize);
                Assert.IsNotNull(P(door, "stairLights").GetArrayElementAtIndex(0).objectReferenceValue, "içeriden sızan ışık");
                var sw = (ShiftSwitch)Ref(ctrl, "mainSwitch");
                foreach (var f in new[] { "coverHinge", "lever", "target" }) Assert.IsNotNull(Ref(sw, f), "son şalter " + f);
                var director = All<AudioDirector>(scene).Single();
                Assert.IsNotNull(Ref(director, "bank"));
                Assert.IsNull(Ref(director, "game"), "menüde oyun yok: drone ve uğultu");
                Assert.AreEqual(1, All<UnityEngine.EventSystems.EventSystem>(scene).Length);
                Assert.AreEqual(1, All<Camera>(scene).Length);
                foreach (var t in All<DeskTarget>(scene))
                {
                    Assert.IsNotEmpty(P(t, "title").stringValue, t.name);
                    if (t.GetComponentInParent<SettingsCabinet>() == null) Assert.IsNotNull(Ref(t, "hoverTag"), t.name + " etiketi");
                }
            });
        }
    }
}
