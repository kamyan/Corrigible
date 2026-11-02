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
        public void FocusSamePose_DoesNotRestart_AndStationsMapToPoses()
        {
            var face = rig;
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

        (Transform back, Transform dive) DiveAnchors()
        {
            var back = Anchor("CrtBack", new Vector3(0.05f, 1.55f, -0.55f), Quaternion.Euler(14, 0, 0));
            var dive = Anchor("CrtDive", new Vector3(1.219f, 0.95f, 1.493f), Quaternion.Euler(0, 35, 0));
            rig.ConfigureDive(back, 56);
            return (back, dive);
        }

        struct Frame { public Vector3 position; public Quaternion rotation; public float fov; }

        Frame Now => new Frame { position = cam.transform.position, rotation = cam.transform.rotation, fov = Fov };

        [Test]
        public void RaidDive_IsOneSmoothMove_PullBackTurnPushIn()
        {
            var (back, dive) = DiveAnchors();
            rig.Snap(SiginakPose.Map);
            float seconds = rig.DiveSeconds;
            Assert.AreEqual(SiginakTheme.Active.NumberOf("motion.crtDiveS"), seconds, 1e-4f);

            const int steps = 120;
            var frames = new System.Collections.Generic.List<Frame>();
            rig.BeginDive(dive, 40);
            frames.Add(Now);
            for (int i = 0; i < steps; i++) { rig.StepDive(seconds / steps); frames.Add(Now); }
            rig.StepDive(1e-3f); // toplamadaki kayan nokta artığı
            Assert.IsFalse(rig.Diving);
            Assert.Less(Vector3.Distance(map.position, frames[0].position), 1e-4f, "bulunduğu yerden başlar");
            Assert.Less(Vector3.Distance(dive.position, cam.transform.position), 1e-4f);
            Assert.Less(Quaternion.Angle(dive.rotation, cam.transform.rotation), 0.01f);
            Assert.AreEqual(40, Fov, 0.01f);
            Assert.AreEqual(0, cam.lensShift.y, 1e-5f);

            // Sıra okunur: önce geri çekilir (henüz dönmeden), sonra CRT'ye döner, en son ekrana yaklaşır
            var early = frames[steps * 3 / 10];
            Assert.Less(early.position.z, map.position.z - 0.5f, "önce geri çekilir");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0, early.rotation.eulerAngles.y)), 5, "henüz dönmedi");
            var middle = frames[steps * 6 / 10];
            Assert.Greater(middle.rotation.eulerAngles.y, 15, "CRT'ye dönüyor");
            Assert.Greater(Vector3.Distance(dive.position, middle.position), 1, "ekrana daha varmadı");
            Assert.Less(Vector3.Distance(dive.position, frames[steps * 9 / 10].position), 0.2f, "sonda ekranın önünde");

            // Yol ortasında durma yok, ani dönüş yok: her adımda kamera ya yer ya yön değiştirir, açısal hız ve ivme sınırlı
            float dt = seconds / steps, lastTurn = 0;
            for (int i = 1; i <= steps; i++)
            {
                float move = Vector3.Distance(frames[i].position, frames[i - 1].position) / dt;
                float turn = Quaternion.Angle(frames[i].rotation, frames[i - 1].rotation) / dt;
                if (i > steps / 20 && i < steps - steps / 20) Assert.Greater(move + turn / 60, 0.3f, "durma yok (u = " + (float)i / steps + ")");
                Assert.Less(turn, 120, "dönüş sivri değil (u = " + (float)i / steps + ")");
                if (i > 1) Assert.Less(Mathf.Abs(turn - lastTurn) / dt, 600, "açısal hız sıçramaz");
                lastTurn = turn;
            }

            // Dönüş aynı yolun tersi: önce monitörde bekler, sonra dalışın karelerinden geriye geçer ve harita pozuna oturur
            rig.BeginReturn(dive, 40, 0.3f);
            rig.StepDive(0.25f);
            Assert.Less(Vector3.Distance(dive.position, cam.transform.position), 1e-4f, "tüp kapanırken bekler");
            rig.StepDive(0.05f);
            for (int i = 1; i <= steps; i++)
            {
                rig.StepDive(dt);
                var mirror = frames[steps - i];
                Assert.Less(Vector3.Distance(mirror.position, cam.transform.position), 1e-3f, "ayna konum (adım " + i + ")");
                Assert.Less(Quaternion.Angle(mirror.rotation, cam.transform.rotation), 0.1f, "ayna bakış (adım " + i + ")");
            }
            rig.StepDive(1e-3f);
            Assert.IsFalse(rig.Diving);
            Assert.Less(Vector3.Distance(map.position, cam.transform.position), 1e-4f);
            Assert.Less(Quaternion.Angle(map.rotation, cam.transform.rotation), 0.01f);
            Assert.AreEqual(30, Fov, 0.01f);
        }

        [Test]
        public void RaidDive_FromDeskClearsLensShift_AndSkipLandsOnTarget()
        {
            var (back, dive) = DiveAnchors();
            rig.BeginDive(dive, 40);
            rig.StepDive(rig.DiveSeconds * 0.3f);
            Assert.Less(cam.transform.position.z, desk.position.z, "K1'den de geri çekilir");
            Assert.That(cam.lensShift.y, Is.InRange(-0.324f, -0.001f), "lens kaydırması yumuşakça sıfırlanıyor");
            rig.FinishDive();
            Assert.IsFalse(rig.Diving);
            Assert.Less(Vector3.Distance(dive.position, cam.transform.position), 1e-4f);
            Assert.AreEqual(40, Fov, 0.01f);
            Assert.AreEqual(0, cam.lensShift.y, 1e-5f);

            rig.BeginReturn(dive, 40, 0.3f);
            rig.StepDive(0.3f + rig.DiveSeconds);
            Assert.IsFalse(rig.Diving);
            Assert.Less(Vector3.Distance(desk.position, cam.transform.position), 1e-4f, "geçerli poza (K1) döner");
            Assert.AreEqual(56.7f, Fov, 0.01f);
            Assert.AreEqual(-0.324f, cam.lensShift.y, 1e-5f);
        }

        /// <summary>Sahnede: rig ana kamerada ve açık; LiberationDeskView ve RaidLauncher'a bağlı; beş poz tanımlı.</summary>
        [Test]
        public void DeskGameScene_RigIsWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var sceneRig = roots.SelectMany(g => g.GetComponentsInChildren<SiginakCameraRig>(true)).Single();
                Assert.IsTrue(sceneRig.enabled, "DeskGame'in tek kamerası");
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
                Assert.AreEqual("CrtDive", launcher.MonitorFocusPose.name, "baskına yaklaşma CRT'ye");
                var back = new SerializedObject(sceneRig).FindProperty("diveBack").objectReferenceValue as Transform;
                Assert.AreEqual("CrtBack", back != null ? back.name : null, "baskına geçişte önce geri çekilme pozu");
                var crt = roots.SelectMany(g => g.GetComponentsInChildren<SiginakCrt>(true)).Single();
                Assert.AreSame(sceneRig, new SerializedObject(crt).FindProperty("rig").objectReferenceValue, "CRT geçişi kamerayı rig'le sürer");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
