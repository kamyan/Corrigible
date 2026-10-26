using System.Linq;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 3: kamera pozları ve geçişler (0,45 s ease-in-out; K1 → harita önce 0,15 s yükselir).</summary>
    public class SiginakCameraTests
    {
        GameObject root;
        SiginakCameraRig rig;
        Camera cam;
        Transform desk, map, shelf;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("RigTest");
            desk = Anchor("Desk", new Vector3(0, 1.52f, -0.15f), Quaternion.identity);
            map = Anchor("Map", new Vector3(0, 2.75f, 1.30f), Quaternion.LookRotation(new Vector3(0, 0.76f, 1.36f) - new Vector3(0, 2.75f, 1.30f)));
            shelf = Anchor("Shelf", new Vector3(-1.5f, 1.15f, 0.8f), Quaternion.Euler(0, -35, 0));
            var camGo = new GameObject("Camera", typeof(Camera));
            camGo.transform.SetParent(root.transform);
            cam = camGo.GetComponent<Camera>();
            rig = camGo.AddComponent<SiginakCameraRig>();
            rig.Configure(cam, null, new[]
            {
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Desk, anchor = desk, fieldOfView = 56.7f, lensShiftY = -0.324f },
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Map, anchor = map, fieldOfView = 30 },
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Shelf, anchor = shelf, fieldOfView = 48 },
            }, new Vector2(36, 20.25f), 0.45f, 0.15f);
            rig.Snap(SiginakPose.Desk);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        Transform Anchor(string name, Vector3 pos, Quaternion rot)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root.transform);
            t.SetPositionAndRotation(pos, rot);
            return t;
        }

        float Fov => Camera.FocalLengthToFieldOfView(cam.focalLength, cam.sensorSize.y);

        [Test]
        public void Snap_PlacesPhysicalCameraWithLensShift()
        {
            Assert.AreEqual(desk.position, cam.transform.position);
            Assert.IsTrue(cam.usePhysicalProperties);
            Assert.AreEqual(Camera.GateFitMode.Vertical, cam.gateFit);
            Assert.AreEqual(56.7f, Fov, 0.01f);
            Assert.AreEqual(-0.324f, cam.lensShift.y, 1e-5f);
            Assert.IsFalse(rig.IsFocused);
        }

        [Test]
        public void DeskToMap_RisesFirstThenTiltsAndLandsIn045s()
        {
            rig.FocusMap();
            Assert.IsTrue(rig.IsFocused, "hedef poz hemen harita sayılır (görev fişi yerleşimi)");
            rig.Step(0.10f);
            Assert.IsTrue(rig.InTransition);
            Assert.Less(Quaternion.Angle(desk.rotation, cam.transform.rotation), 0.01f, "ilk 0,15 s yalnız yükselir");
            Assert.Greater(cam.transform.position.y, desk.position.y);
            Assert.AreEqual(desk.position.z, cam.transform.position.z, 1e-4f);
            Assert.AreEqual(56.7f, Fov, 0.01f);

            rig.Step(0.15f);
            Assert.IsTrue(rig.InTransition);
            Assert.Greater(Quaternion.Angle(desk.rotation, cam.transform.rotation), 1, "sonra eğilir");
            Assert.That(cam.lensShift.y, Is.InRange(-0.324f, 0f));

            rig.Step(0.21f);
            Assert.IsFalse(rig.InTransition);
            Assert.Less(Vector3.Distance(map.position, cam.transform.position), 1e-4f);
            Assert.Less(Quaternion.Angle(map.rotation, cam.transform.rotation), 0.01f);
            Assert.AreEqual(30, Fov, 0.01f);
            Assert.AreEqual(0, cam.lensShift.y, 1e-5f);
        }

        [Test]
        public void OtherTransitions_AreStraightEaseInOut()
        {
            rig.FocusShelter();
            rig.Step(0.225f);
            // Yarı sürede ease-in-out yolun yarısındadır; yükselme yok
            var mid = Vector3.Lerp(desk.position, shelf.position, 0.5f);
            Assert.Less(Vector3.Distance(mid, cam.transform.position), 1e-3f);
            rig.Step(0.25f);
            Assert.Less(Vector3.Distance(shelf.position, cam.transform.position), 1e-4f);
            Assert.AreEqual(48, Fov, 0.01f);
            Assert.AreEqual(SiginakPose.Shelf, rig.Current);
        }

        [Test]
        public void FocusSamePose_DoesNotRestart_AndInterfaceMapsToPoses()
        {
            IDeskCameraRig face = rig;
            face.FocusMap();
            rig.Step(1);
            face.FocusMap();
            Assert.IsFalse(rig.InTransition, "aynı poza ikinci çağrı geçişi baştan başlatmaz");
            face.ResetView();
            Assert.AreEqual(SiginakPose.Desk, rig.Current);
            face.FocusShelter();
            Assert.AreEqual(SiginakPose.Shelf, rig.Current);
            face.FocusOperations();
            Assert.AreEqual(SiginakPose.Shelf, rig.Current, "pozu olmayan istek yok sayılır");
        }

        /// <summary>Sahnede: rig ana kamerada, eski görünümde kapalı; LiberationDeskView ve RaidLauncher'a bağlı; beş poz tanımlı.</summary>
        [Test]
        public void DeskGameScene_RigIsWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var sceneRig = roots.SelectMany(g => g.GetComponentsInChildren<SiginakCameraRig>(true)).Single();
                Assert.IsFalse(sceneRig.enabled, "eski görünümde kapalı");
                foreach (var pose in new[] { SiginakPose.Desk, SiginakPose.Map, SiginakPose.Shelf, SiginakPose.Crt, SiginakPose.Shift })
                    Assert.IsTrue(sceneRig.TryGetPose(pose, out _), pose.ToString());
                sceneRig.TryGetPose(SiginakPose.Shift, out var shift);
                Assert.AreEqual(0.36f, shift.anchor.position.z, 1e-4f, "K6a render'daki gibi şaltere yakın");

                var surface = roots.SelectMany(g => g.GetComponentsInChildren<LiberationDeskView>(true)).Single();
                Assert.AreSame(sceneRig, new SerializedObject(surface).FindProperty("siginakRig").objectReferenceValue);
                var launcher = roots.SelectMany(g => g.GetComponentsInChildren<Corrigible.Game.Raid.RaidLauncher>(true)).Single();
                var locks = new SerializedObject(launcher).FindProperty("lockDuringTransition");
                Assert.IsTrue(Enumerable.Range(0, locks.arraySize).Any(i => locks.GetArrayElementAtIndex(i).objectReferenceValue == sceneRig),
                    "baskına yaklaşırken rig kilitlenir");

                var sw = roots.SelectMany(g => g.GetComponentsInChildren<SiginakPresentationSwitch>(true)).Single();
                var legacyPose = launcher.MonitorFocusPose;
                sw.Apply(true);
                Assert.IsTrue(sceneRig.enabled);
                Assert.AreEqual("CrtDive", launcher.MonitorFocusPose.name);
                sw.Apply(false);
                Assert.IsFalse(sceneRig.enabled);
                Assert.AreSame(legacyPose, launcher.MonitorFocusPose);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
