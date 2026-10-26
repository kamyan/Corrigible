using System.IO;
using System.Linq;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using Corrigible.Game.Raid;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>Kurulan odayı sahnedeki sunum anahtarına bağlar; eski görünümün nesnelerini listeler.</summary>
    public static partial class SiginakRoomBuilder
    {
        public const string SettingsPath = DeskSpriteManifest.Root + "/Resources/SiginakSettings.asset";
        const string LegacyRoot = "CommandPost3D";

        /// <summary>Yeni görünümde kapanan eski fiziksel düzen (CommandPost3D altında). Kamera, pozlar ve canvas'lar kalır.</summary>
        static readonly string[] LegacyChildren =
            { "Environment", "Table", "MapMount", "StatusNote", "Radio", "RadioTranscript", "Fax", "Ledger", "Files", "Tabs", "Lamp", "LiberationRoom",
              "LiberationHudCanvas" };

        public static SiginakPresentationSwitch FindSwitch() =>
            Object.FindObjectsByType<SiginakPresentationSwitch>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();

        /// <summary>Kurulumdan önce: yeni görünüm açıksa eskiye döndürür (harita canvas'ı silinecek kökün altında kalmasın).</summary>
        static bool PrepareRebuild()
        {
            var sw = FindSwitch();
            bool wasNew = sw != null && sw.IsNew;
            if (wasNew) sw.Apply(false);
            return wasNew;
        }

        static string Wire(Scene scene, Refs refs, bool wasNew, Hud hud, Controls controls)
        {
            var legacyRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == LegacyRoot);
            if (legacyRoot == null) return "UYARI: " + LegacyRoot + " yok, anahtar bağlanmadı";
            var legacy = LegacyChildren.Select(n => legacyRoot.transform.Find(n)).Where(t => t != null).Select(t => t.gameObject).ToArray();
            var map = Object.FindObjectsByType<SadeMapView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(v => (RectTransform)v.transform).FirstOrDefault(t => t.gameObject.scene == scene);
            var camera = legacyRoot.GetComponentInChildren<Camera>(true);
            var rig = camera != null ? camera.GetComponent<CommandPostCamera>() : null;
            var siginakRig = camera != null ? CameraRig(camera, legacyRoot.GetComponent<CommandPostDesk>(), refs) : null;
            var launcher = Object.FindObjectsByType<RaidLauncher>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(l => l.gameObject.scene == scene);
            LockDuringRaidTransition(launcher, siginakRig);
            var surface = Object.FindObjectsByType<LiberationDeskView>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(v => v.gameObject.scene == scene);
            // Masa kontrollerinin oyun bağlantısı (Aşama 4)
            var deskGo = new GameObject("Desk controller");
            deskGo.transform.SetParent(refs.Root, false);
            var siginakDesk = deskGo.AddComponent<SiginakDesk>();
            var game = Object.FindObjectsByType<GameController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(g => g.gameObject.scene == scene);
            controls.Telex.Configure(controls.Printer, hud.Strip, camera);
            // Kurtuluş haritası, fişler ve ekip figürü (Aşama 5)
            var mapParts = BuildMap(scene, refs, hud, camera, siginakRig, controls.Telex, game, surface);
            // Sığınak: kesit maketi odaları, BOŞTA tepsisi, özet panosu (Aşama 6)
            BuildShelter(refs, hud, camera, siginakRig, game, surface, controls.Shelf);
            // CRT: masadaki ekran ve tam ekran baskın geçişi (Aşama 7)
            var crt = BuildCrt(scene, refs, camera, game);
            // Vardiya geçişi: dizi, rapor, EVRAK tepsisi (Aşama 8)
            var shiftParts = BuildShift(refs, hud, controls);
            var shiftGo = new GameObject("Shift controller");
            shiftGo.transform.SetParent(refs.Root, false);
            var shift = shiftGo.AddComponent<SiginakShift>();
            // Açılış ve emir belgesi (Aşama 9)
            var briefingParts = BuildBriefing(refs, hud, controls);
            var briefingGo = new GameObject("Briefing controller");
            briefingGo.transform.SetParent(refs.Root, false);
            var briefing = briefingGo.AddComponent<SiginakBriefing>();
            briefing.Configure(game, surface, siginakRig, controls.Telex, shiftParts.Tray, shift, briefingParts.View, briefingParts.Opening, camera);
            shift.Configure(game, surface, siginakDesk, siginakRig, mapParts?.Controller, controls.Switch, controls.Telex, controls.Counters,
                shiftParts.Sequence, shiftParts.Report, shiftParts.Tray, camera, briefing);
            siginakDesk.Configure(game, surface, siginakRig, SettingsAsset(), camera, controls.Counters, controls.Rail, controls.Switch, controls.Telex,
                controls.Hope, controls.FreedEntries, controls.Shelf, controls.Crt, controls.Tray, controls.TelexPaper, hud.Edge, hud.Plaque,
                mapParts?.Controller, shift, briefing);
            if (surface != null)
            {
                var so = new SerializedObject(surface);
                so.FindProperty("siginakRig").objectReferenceValue = siginakRig;
                so.FindProperty("telex").objectReferenceValue = controls.Telex;
                so.FindProperty("siginak").objectReferenceValue = siginakDesk;
                so.FindProperty("siginakMap").objectReferenceValue = mapParts?.Controller;
                so.FindProperty("siginakBriefing").objectReferenceValue = briefing;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            // Odadaki hedefler için fizik ışını; yalnız yeni görünümde açık (eski odanın primitive çarpıştırıcıları dünya
            // canvas'larının tıklamasını kapatmasın)
            PhysicsRaycaster physics = null;
            if (camera != null)
            {
                physics = camera.GetComponent<PhysicsRaycaster>();
                if (physics == null) physics = camera.gameObject.AddComponent<PhysicsRaycaster>();
                physics.enabled = false;
            }

            var sw = FindSwitch();
            if (sw == null)
            {
                var go = new GameObject("SiginakPresentation");
                SceneManager.MoveGameObjectToScene(go, scene);
                sw = go.AddComponent<SiginakPresentationSwitch>();
            }
            sw.Configure(SettingsAsset(), refs.Root.gameObject, legacy, map, refs.MapMount, camera, rig, siginakRig, launcher,
                refs.PoseCrtDive, 40, physics, mapParts != null ? new[] { mapParts.Fills, mapParts.Marks } : null, crt);
            EditorUtility.SetDirty(sw);
            sw.Apply(wasNew);
            return $"anahtar: {legacy.Length} eski nesne, harita {(map != null ? "bağlı" : "YOK")}, görünüm {(wasNew ? "yeni" : "eski")}";
        }

        /// <summary>Ana kameradaki sığınak rig'i: pozlar ve görüş açıları tokenlardan; eski görünümde kapalı durur.</summary>
        static SiginakCameraRig CameraRig(Camera camera, CommandPostDesk desk, Refs refs)
        {
            var rig = camera.GetComponent<SiginakCameraRig>();
            if (rig == null) rig = camera.gameObject.AddComponent<SiginakCameraRig>();
            SiginakCameraRig.PoseSlot Slot(SiginakPose pose, Transform anchor, string key, float shift = 0) =>
                new SiginakCameraRig.PoseSlot { pose = pose, anchor = anchor, fieldOfView = T.Number("cameras." + key + ".fovV", 45), lensShiftY = shift };
            rig.Configure(camera, desk, new[]
            {
                Slot(SiginakPose.Desk, refs.PoseK1, "K1", T.Number("cameras.K1.lensShiftY")),
                Slot(SiginakPose.Map, refs.PoseMap, "map"),
                Slot(SiginakPose.Shelf, refs.PoseK4, "K4"),
                Slot(SiginakPose.Crt, refs.PoseK5a, "K5a"),
                Slot(SiginakPose.Shift, refs.PoseK6a, "K6a"),
            }, Settings.sensorSizeMm, T.Number("motion.poseS", 0.45f), T.Number("motion.poseRiseS", 0.15f));
            rig.enabled = false;
            EditorUtility.SetDirty(rig);
            return rig;
        }

        /// <summary>Baskına yaklaşırken RaidLauncher kamerayı kendisi sürer; sığınak rig'i de kilit listesinde olmalı.</summary>
        static void LockDuringRaidTransition(RaidLauncher launcher, SiginakCameraRig rig)
        {
            if (launcher == null || rig == null) return;
            var so = new SerializedObject(launcher);
            var list = so.FindProperty("lockDuringTransition");
            for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == rig) return;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = rig;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SiginakSettings SettingsAsset()
        {
            var s = AssetDatabase.LoadAssetAtPath<SiginakSettings>(SettingsPath);
            if (s != null) return s;
            s = ScriptableObject.CreateInstance<SiginakSettings>();
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            AssetDatabase.CreateAsset(s, SettingsPath);
            return s;
        }
    }
}
