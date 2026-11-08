using System.Collections.Generic;
using System.Linq;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Sığınak Aşama 3: kamera pozları ve geçişler (0,45 s ease-in-out; K1 → harita önce 0,15 s yükselir). Revizyon 3
    /// Aşama 3: K4'e 0,6 s, K4'ten 0,5 s (motion.board), varış olayı, sürüklemesiz sağ tık (10 px ölü bölge), K4'te
    /// uzaklaşma sınırı.
    /// </summary>
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

        /// <summary>K4 kendi süresini kullanır (aşağıda); öteki varsayılan geçişler (burada K1 → CRT) 0,45 s ve düz ease-in-out.</summary>
        [Test]
        public void OtherTransitions_AreStraightEaseInOut()
        {
            var crt = Anchor("Crt", new Vector3(1.2f, 1.0f, 1.4f), Quaternion.Euler(0, 35, 0));
            rig.Configure(cam, null, new[]
            {
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Desk, anchor = desk, fieldOfView = 56.7f, lensShiftY = -0.324f },
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Map, anchor = map, fieldOfView = 30 },
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Shelf, anchor = shelf, fieldOfView = 48 },
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Crt, anchor = crt, fieldOfView = 40 },
            }, new Vector2(36, 20.25f), 0.45f, 0.15f);
            rig.Snap(SiginakPose.Desk);

            rig.FocusOperations();
            Assert.AreEqual(0.45f, rig.TransitionRemaining, 1e-4f, "K1 → CRT motion.poseS");
            rig.Step(0.225f);
            // Yarı sürede ease-in-out yolun yarısındadır; yükselme yok
            var mid = Vector3.Lerp(desk.position, crt.position, 0.5f);
            Assert.Less(Vector3.Distance(mid, cam.transform.position), 1e-3f);
            rig.Step(0.25f);
            Assert.IsFalse(rig.InTransition);
            Assert.Less(Vector3.Distance(crt.position, cam.transform.position), 1e-4f);
            Assert.AreEqual(40, Fov, 0.01f);
            Assert.AreEqual(SiginakPose.Crt, rig.Current);
        }

        /// <summary>
        /// E1 (Aşama 3 E §1.3): panoya (K4) gidiş motion.board.toK4S 0,6 s, K4'ten çıkış motion.board.backToDeskS 0,5 s;
        /// K1 → harita motion.poseS 0,45 s. Varış olayı geçişin sonunda bir kez yayınlanır.
        /// </summary>
        [Test]
        public void ShelfTransitions_UseBoardTokens()
        {
            var theme = SiginakTheme.Active;
            float toK4 = theme.NumberOf("motion.board.toK4S", -1), back = theme.NumberOf("motion.board.backToDeskS", -1);
            Assert.AreEqual(0.6f, toK4, 1e-4f, "motion.board.toK4S");
            Assert.AreEqual(0.5f, back, 1e-4f, "motion.board.backToDeskS");
            Assert.AreEqual(toK4, rig.PoseSeconds(SiginakPose.Desk, SiginakPose.Shelf), 1e-6f);
            Assert.AreEqual(toK4, rig.PoseSeconds(SiginakPose.Map, SiginakPose.Shelf), 1e-6f, "her pozdan K4'e aynı süre");
            Assert.AreEqual(back, rig.PoseSeconds(SiginakPose.Shelf, SiginakPose.Map), 1e-6f, "K4'ten her çıkış aynı süre");
            Assert.AreEqual(back, rig.PoseSeconds(SiginakPose.Shelf, SiginakPose.Door), 1e-6f);
            Assert.AreEqual(0.45f, rig.PoseSeconds(SiginakPose.Desk, SiginakPose.Map), 1e-6f);

            var arrived = new List<SiginakPose>();
            rig.Arrived += arrived.Add;

            rig.FocusShelter();
            Assert.AreEqual(toK4, rig.TransitionRemaining, 1e-4f, "K1 → K4 0,6 s");
            rig.Step(toK4 / 2);
            Assert.Less(Vector3.Distance(Vector3.Lerp(desk.position, shelf.position, 0.5f), cam.transform.position), 1e-3f, "0,30 s'de yolun yarısı");
            Assert.IsTrue(rig.InTransition);
            Assert.IsEmpty(arrived, "geçiş bitmeden varış yok");
            rig.Step(toK4 / 2 + 1e-4f);
            Assert.IsFalse(rig.InTransition, "0,60 s'de vardı");
            Assert.Less(Vector3.Distance(shelf.position, cam.transform.position), 1e-4f);
            Assert.AreEqual(48, Fov, 0.01f);
            CollectionAssert.AreEqual(new[] { SiginakPose.Shelf }, arrived);

            rig.ResetView();
            Assert.AreEqual(back, rig.TransitionRemaining, 1e-4f, "K4 → K1 0,5 s");
            rig.Step(back / 2);
            Assert.Less(Vector3.Distance(Vector3.Lerp(shelf.position, desk.position, 0.5f), cam.transform.position), 1e-3f, "0,25 s'de yolun yarısı");
            rig.Step(back / 2 + 1e-4f);
            Assert.IsFalse(rig.InTransition);
            Assert.Less(Vector3.Distance(desk.position, cam.transform.position), 1e-4f);
            CollectionAssert.AreEqual(new[] { SiginakPose.Shelf, SiginakPose.Desk }, arrived);

            rig.FocusMap();
            Assert.AreEqual(0.45f, rig.TransitionRemaining, 1e-4f, "K1 → harita değişmedi (motion.poseS)");
            rig.Focus(SiginakPose.Shelf, 0.2f);
            Assert.AreEqual(0.2f, rig.TransitionRemaining, 1e-4f, "süresini veren çağrı kendi süresini kullanır");
        }

        /// <summary>E2: aynı süre iki token grubunda tutulur (motion.board ve cameras.K4); ayrışırsa hangisinin geçerli olduğu belirsizleşir.</summary>
        [Test]
        public void PoseTimeTokens_AgreeAcrossGroups()
        {
            var theme = SiginakTheme.Active;
            Assert.IsTrue(theme.TryNumber("motion.board.toK4S", out var toK4), "motion.board.toK4S");
            Assert.IsTrue(theme.TryNumber("motion.board.backToDeskS", out var back), "motion.board.backToDeskS");
            Assert.IsTrue(theme.TryNumber("cameras.K4.fromK1S", out var fromK1), "cameras.K4.fromK1S");
            Assert.IsTrue(theme.TryNumber("cameras.K4.backS", out var backS), "cameras.K4.backS");
            Assert.AreEqual(fromK1, toK4, 1e-6f, "K1 → K4 süresi iki grupta aynı");
            Assert.AreEqual(backS, back, 1e-6f, "K4 → K1 süresi iki grupta aynı");
        }

        /// <summary>Varış: geçişin sonunda ve anında oturtmada (Snap); yarıda kesilen geçiş bildirilmez, poz değişimi önce gelir.</summary>
        [Test]
        public void Arrived_FiresOnSnapAndAtTransitionEndOnly()
        {
            var events = new List<string>();
            rig.PoseChanged += p => events.Add("change:" + p);
            rig.Arrived += p => events.Add("arrive:" + p);

            rig.Snap(SiginakPose.Map);
            CollectionAssert.AreEqual(new[] { "change:Map", "arrive:Map" }, events);

            events.Clear();
            rig.FocusShelter();
            rig.Step(0.1f);
            rig.ResetView(); // yarıda başka poza
            rig.Step(0.1f);
            Assert.IsTrue(rig.InTransition);
            rig.Step(1);
            rig.Step(1);
            CollectionAssert.AreEqual(new[] { "change:Shelf", "change:Desk", "arrive:Desk" }, events, "yarıda kalan K4 varmadı; K1 bir kez vardı");
        }

        /// <summary>
        /// E3 (Karar K7): sağ tuş 0 / 9 px oynatılıp bırakılırsa tık (geri), 10 / 40 px bakış; eşik aşılınca bekletilen hareket
        /// kaybolmaz (uygulanan sapma = toplam hareket × duyarlılık).
        /// </summary>
        [TestCase(0f, true)]
        [TestCase(9f, true)]
        [TestCase(10f, false)]
        [TestCase(40f, false)]
        public void RightClick_DeadZone(float movePx, bool click)
        {
            const float slop = 10, sensitivity = 0.09f;
            // Edit modda EventSystem çalışmaz: ölü bölge EventSystem'in varsayılan eşiği (10 px); sahnedeki değeri DeskGameScene_RigIsWired kilitler
            if (EventSystem.current == null) Assert.AreEqual(slop, SiginakCameraRig.RightClickSlop, 1e-6f, "EventSystem yokken ölü bölge 10 px");
            Assert.AreEqual(sensitivity, rig.LookSensitivity, 1e-6f, "bakış duyarlılığı 0,09°/px");
            var gesture = new RightButtonGesture();
            var start = new Vector2(400, 300);
            gesture.Press(start);
            Assert.IsTrue(gesture.Held);
            // 1 px'lik adımlarla sağa, sonra hiç oynamadan bir kare
            int steps = Mathf.CeilToInt(movePx);
            Vector2 applied = Vector2.zero;
            for (int i = 1; i <= steps; i++)
                applied += gesture.Move(start + new Vector2(Mathf.Min(i, movePx), 0), new Vector2(Mathf.Min(i, movePx) - Mathf.Min(i - 1, movePx), 0), slop);
            applied += gesture.Move(start + new Vector2(movePx, 0), Vector2.zero, slop);
            Assert.AreEqual(!click, gesture.Looking, "ölü bölge");
            Assert.AreEqual(click, gesture.Release(), click ? "tık" : "bakış");
            Assert.IsFalse(gesture.Held);
            Assert.AreEqual(click ? 0 : movePx, applied.x, 1e-4f, "bakışa giden hareket");
            Assert.AreEqual(0, applied.y, 1e-6f);
            Assert.AreEqual(click ? 0 : movePx * sensitivity, applied.x * rig.LookSensitivity, 1e-4f, "sapma = hareket × 0,09");
        }

        /// <summary>Kilit, taşıma, poz değişimi ve Overlay basışı tık saymadan bırakır; sonraki basış yeniden sınıflanır.</summary>
        [Test]
        public void RightClick_ResetCancels_AndNextPressIsFresh()
        {
            var gesture = new RightButtonGesture();
            gesture.Press(Vector2.zero);
            gesture.Move(new Vector2(3, 0), new Vector2(3, 0), 10);
            gesture.Reset();
            Assert.IsFalse(gesture.Release(), "sıfırlanan basış tık değil");
            Assert.AreEqual(Vector2.zero, gesture.Move(new Vector2(50, 0), new Vector2(50, 0), 10), "basılı değilken hareket yok sayılır");

            gesture.Press(new Vector2(100, 100));
            Assert.AreEqual(Vector2.zero, gesture.Move(new Vector2(106, 100), new Vector2(6, 0), 10), "önceki basışın hareketi taşınmaz");
            Assert.AreEqual(new Vector2(12, 0), gesture.Move(new Vector2(112, 100), new Vector2(6, 0), 10), "eşikte bekleyen 6 + 6 px");
            Assert.AreEqual(new Vector2(-2, 3), gesture.Move(new Vector2(110, 103), new Vector2(-2, 3), 10), "bakışta delta olduğu gibi");
            Assert.IsFalse(gesture.Release());
        }

        /// <summary>K4'te tekerlekle uzaklaşma 1,0'da durur (1,1'de kart düzlemi 14 px altına düşer); yaklaşma ve öteki pozlar değişmez.</summary>
        [Test]
        public void ZoomOut_IsClampedAtShelf()
        {
            Assert.AreEqual(new Vector2(0.6f, 1.1f), rig.ZoomRange, "K1");
            rig.Snap(SiginakPose.Shelf);
            Assert.AreEqual(0.6f, rig.ZoomRange.x, 1e-6f, "K4'te yaklaşma açık");
            Assert.AreEqual(1.0f, rig.ZoomRange.y, 1e-6f, "K4'te uzaklaşma 1,0");
            rig.Snap(SiginakPose.Map);
            Assert.AreEqual(1.1f, rig.ZoomRange.y, 1e-6f, "harita");
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

        /// <summary>Sahnede: rig ana kamerada ve açık; LiberationDeskView ve RaidLauncher'a bağlı; altı poz tanımlı (K6 dahil).</summary>
        [Test]
        public void DeskGameScene_RigIsWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var sceneRig = roots.SelectMany(g => g.GetComponentsInChildren<SiginakCameraRig>(true)).Single();
                Assert.IsTrue(sceneRig.enabled, "DeskGame'in tek kamerası");
                foreach (var pose in new[] { SiginakPose.Desk, SiginakPose.Map, SiginakPose.Shelf, SiginakPose.Crt, SiginakPose.Shift, SiginakPose.ShiftClose })
                    Assert.IsTrue(sceneRig.TryGetPose(pose, out _), pose.ToString());
                // Revizyon 3 · Aşama 4: kartlı oyunun kapanışı K6'da (cameras.K6, FOV 42, lens kaydırması yok; Karar K8). K6a (Shift)
                // yalnız eski yolda kalır.
                sceneRig.TryGetPose(SiginakPose.ShiftClose, out var k6);
                Assert.AreEqual("K6", k6.anchor.name);
                Assert.AreEqual(SiginakTheme.Active.NumberOf("cameras.K6.fovV", 42), k6.fieldOfView, 1e-4f);
                Assert.AreEqual(0, k6.lensShiftY, 1e-6f);
                AssertPhysicalReceiptAndTrayFitShiftCloseFrame(roots, sceneRig, k6);

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
                // Sağ tıkın ölü bölgesi EventSystem'in sürükleme eşiğidir; kart sürüklemesi aynı eşikle başlar (Aşama 3 E §2.3)
                var events = roots.SelectMany(g => g.GetComponentsInChildren<EventSystem>(true)).ToList();
                Assert.IsNotEmpty(events, "DeskGame'de EventSystem");
                foreach (var e in events) Assert.AreEqual(10, e.pixelDragThreshold, e.name + ": sürükleme eşiği / sağ tık ölü bölgesi 10 px");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>
        /// K6'da (1920 × 1080) dildeki fişin kutusu (100 × 150 mm, <see cref="ShiftReceiptPaper.TongueTarget"/> pozunda) ve EVRAK
        /// bırakma kutusu bütünüyle kadrajda.
        /// </summary>
        static void AssertPhysicalReceiptAndTrayFitShiftCloseFrame(GameObject[] roots, SiginakCameraRig sceneRig, SiginakCameraRig.PoseSlot shift)
        {
            var paper = roots.SelectMany(g => g.GetComponentsInChildren<ShiftReceiptPaper>(true)).Single();
            var paperData = new SerializedObject(paper);
            var output = paperData.FindProperty("output").objectReferenceValue as Transform;
            var paperBox = paperData.FindProperty("hitArea").objectReferenceValue as BoxCollider;
            var tray = roots.SelectMany(g => g.GetComponentsInChildren<EvrakTray>(true)).Single();
            var trayBox = new SerializedObject(tray).FindProperty("receiptDropArea").objectReferenceValue as BoxCollider;
            Assert.IsNotNull(output);
            Assert.IsNotNull(paperBox);
            Assert.IsNotNull(trayBox);

            var previewObject = new GameObject("Vardiya kadrajı testi", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var texture = new RenderTexture(1920, 1080, 0) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var preview = previewObject.GetComponent<Camera>();
                preview.enabled = false;
                preview.targetTexture = texture;
                preview.aspect = 16f / 9f;
                preview.usePhysicalProperties = true;
                preview.sensorSize = new SerializedObject(sceneRig).FindProperty("sensorSizeMm").vector2Value;
                preview.gateFit = Camera.GateFitMode.Vertical;
                preview.focalLength = Camera.FieldOfViewToFocalLength(shift.fieldOfView, preview.sensorSize.y);
                preview.lensShift = new Vector2(0, shift.lensShiftY);
                preview.nearClipPlane = .03f;
                preview.transform.SetPositionAndRotation(shift.anchor.position, shift.anchor.rotation);

                // Başlangıçta gizli fiş çıkınca dil pozunda durur (ağzın dış ucu + ofset, üstü ağızdan uzakta; PLAN §1.5); oradaki
                // gerçek kutusunu izdüşür.
                var tongue = paper.TongueTarget;
                Assert.AreEqual(0.10f, paperBox.size.x, 1e-4f, "fiş kutusu 100 mm");
                Assert.AreEqual(0.15f, paperBox.size.y, 1e-4f, "fiş kutusu 150 mm");
                var paperAtTongue = Matrix4x4.TRS(tongue.position, tongue.rotation, paper.transform.lossyScale) * paper.transform.worldToLocalMatrix;
                AssertBoxInFrame(preview, paperBox, paperAtTongue, "Dildeki devir fişi");
                AssertBoxInFrame(preview, trayBox, Matrix4x4.identity, "Mevcut EVRAK kutusu");
            }
            finally
            {
                previewObject.GetComponent<Camera>().targetTexture = null;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(previewObject);
            }
        }

        static void AssertBoxInFrame(Camera preview, BoxCollider box, Matrix4x4 worldTransform, string description)
        {
            for (int i = 0; i < 8; i++)
            {
                var local = box.center + Vector3.Scale(box.size * .5f,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var point = preview.WorldToViewportPoint(worldTransform.MultiplyPoint3x4(box.transform.TransformPoint(local)));
                Assert.Greater(point.z, preview.nearClipPlane, description + " kameranın önünde olmalı.");
                Assert.That(point.x, Is.InRange(.01f, .99f), description + " yatayda bütünüyle görünmeli.");
                Assert.That(point.y, Is.InRange(.01f, .99f), description + " dikeyde bütünüyle görünmeli.");
            }
        }
    }
}
