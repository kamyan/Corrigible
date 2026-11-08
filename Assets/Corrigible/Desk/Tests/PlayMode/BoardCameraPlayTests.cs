using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Kartlı pano, kamera ve geri yolu (Revizyon 3 Aşama 3, PLAN §5.2 P1–P4; E §5.3 "Camera and routing"). DeskGame gerçek
    /// sahnesiyle Play'de: SIĞINAK tuşu K4'e <c>motion.board.toK4S</c> (0,6 s) sürede götürür ve kenar satırı ilk kareden
    /// gizlenir; MASAYA DÖN levhası, Esc ve sürüklemesiz sağ tık K4'ten <c>motion.board.backToDeskS</c> (0,5 s) sürede masaya
    /// döndürür; ölü bölgeyi aşan sağ sürükleme bakıştır (geri sayılmaz); açık dosya Esc'i ilk önce kendisi tüketir. Hiçbiri komut
    /// yazmaz ya da RNG çekmez. Süre toleransı token + iki kare (<see cref="BoardPlayKit.FrameTolerance"/>).
    /// </summary>
    public sealed class BoardCameraPlayTests
    {
        /// <summary>K4'ten masaya dönüşün üç yolu (PLAN §1.5 "Back").</summary>
        public enum BackRoute { BackSign, Esc, RightClick }

        static readonly BackRoute[] BackRoutes = { BackRoute.BackSign, BackRoute.Esc, BackRoute.RightClick };

        /// <summary>Cep sırası (BoardSnapshot.PocketOccupant anahtarları); dosyası açılacak kişi ilk dolu cepten.</summary>
        static readonly string[] PocketOrder = { "tek", "emp", "ana", "craft", "cook", "train" };

        PoseLog log;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return BoardPlayKit.LoadDesk();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            log?.Dispose();
            log = null;
            yield return BoardPlayKit.TearDown();
        }

        /// <summary>
        /// P1: SIĞINAK tuşunun DeskTarget'ına sol tık → hemen <c>Current == Shelf</c>, kalan süre 0,6 s; geçişin her karesinde
        /// kenar satırı gizli; 0,6 s + iki karede varış, kamera <c>Cam_K4</c> çapasında ±0,1 mm. Pano girdisi geçiş sırasında
        /// kapalı, varınca açık (PLAN §1.5, eleştiri #8).
        /// </summary>
        [UnityTest]
        public IEnumerator SiginakKey_GoesToK4_In06s_EdgeLineHidden()
        {
            var rig = BoardPlayKit.Rig;
            float toK4 = Token("motion.board.toK4S", 0.6f);
            Assert.AreEqual(toK4, rig.PoseSeconds(rig.Current, SiginakPose.Shelf), 1e-6f, "PoseSeconds(K1 → K4) = motion.board.toK4S");
            var edge = BoardPlayKit.Find<EdgeLine>();
            Assert.IsNotNull(edge, "EdgeLine");
            var key = ShelterKey();
            var board = BoardPlayKit.Board;
            var start = BoardPlayKit.Snapshot();

            log = new PoseLog(rig);
            BoardPlayKit.ResetFrameStats();
            BoardPlayKit.Click(key.gameObject);
            Assert.AreEqual(SiginakPose.Shelf, rig.Current, "SIĞINAK tuşu K4'e götürür (SiginakDesk.Station → LiberationDeskView.ShowShelter → PlanningBoardPresenter.Show)");
            Assert.AreEqual(toK4, rig.TransitionRemaining, 1e-3f, "ilk karede kalan süre = motion.board.toK4S");
            Assert.IsTrue(rig.InTransition, "geçiş başladı");

            int frame = 0;
            float until = Time.unscaledTime + toK4 + 1f;
            while (rig.InTransition)
            {
                yield return null;
                BoardPlayKit.TrackFrame();
                frame++;
                Assert.IsFalse(edge.Visible, "K4 geçişinin " + frame + ". karesinde kenar satırı gizli olmalı (SiginakDesk → EdgeLine.SetVisible, hiddenByPose)");
                if (board != null && rig.InTransition)
                    Assert.IsFalse(board.Interactive, "geçiş sürerken pano girdisi kapalı (" + frame + ". kare)");
                if (Time.unscaledTime > until) Assert.Fail("K4 geçişi " + (toK4 + 1f) + " s içinde bitmedi");
            }
            yield return BoardPlayKit.Frames(2);

            Assert.AreEqual(SiginakPose.Shelf, rig.Current);
            Assert.IsFalse(rig.InTransition, "0,6 s + iki karede vardı");
            Assert.IsFalse(edge.Visible, "K4'te kenar satırı gizli");
            BoardPlayKit.AssertTransition(log, SiginakPose.Shelf, toK4, "SIĞINAK tuşu");
            Assert.AreEqual(1, log.Arrivals.Count(a => a.Pose == SiginakPose.Shelf), "K4'e tek varış (SiginakCameraRig.Arrived)");
            BoardPlayKit.AssertAtAnchor(SiginakPose.Shelf, "K4");
            if (board != null) Assert.IsTrue(board.Interactive, "K4'e varınca pano girdisi açık (BoardView3D.SetInteractive)");
            Assert.AreEqual(start, BoardPlayKit.Snapshot(), "K4'e gidiş komut yazmaz, RNG çekmez");
        }

        /// <summary>
        /// P2: K4'ten üç geri yolu (MASAYA DÖN levhası, Esc, 3 px'lik sağ tık) → <c>Current == Desk</c>, geçiş 0,5 s ± iki kare,
        /// kamera K1 çapasında; tek poz değişimi, komut ve RNG değişmez.
        /// </summary>
        [UnityTest]
        public IEnumerator Back_FromK4_In05s([ValueSource(nameof(BackRoutes))] BackRoute route)
        {
            yield return BoardPlayKit.EnterBoard();
            var rig = BoardPlayKit.Rig;
            float back = Token("motion.board.backToDeskS", 0.5f);
            Assert.AreEqual(back, rig.PoseSeconds(SiginakPose.Shelf, SiginakPose.Desk), 1e-6f, "PoseSeconds(K4 → K1) = motion.board.backToDeskS");
            log = new PoseLog(rig);
            var before = BoardPlayKit.Snapshot();

            switch (route)
            {
                case BackRoute.BackSign:
                    var sign = BoardPlayKit.BackSignTarget();
                    Assert.IsNotNull(sign, "MASAYA DÖN levhasının DeskTarget'ı (BoardView3D.backTarget)");
                    Assert.IsTrue(sign.Interactable, "MASAYA DÖN K4'te tıklanır");
                    BoardPlayKit.Click(sign.gameObject);
                    break;
                case BackRoute.Esc:
                    yield return BoardPlayKit.PressKey(Key.Escape);
                    break;
                case BackRoute.RightClick:
                    // Ölü bölgenin (10 px) içinde 3 px oynayan sağ tık: geri (Karar K7)
                    yield return BoardPlayKit.RightDrag(BoardPlayKit.ScreenCentre, new Vector2(3, 0), 1);
                    Assert.AreEqual(1, log.Backs.Count, "3 px'lik sağ tık bir kez BackClicked yayınlar");
                    Assert.AreEqual(SiginakPose.Shelf, log.Backs[0].Pose, "BackClicked bırakıldığı pozla (K4)");
                    break;
            }

            yield return BoardPlayKit.WaitFor(() => log.Changes.Any(c => c.Pose == SiginakPose.Desk), 1, route + ": masaya dönüş başlamadı");
            yield return BoardPlayKit.WaitFor(() => log.Arrived(SiginakPose.Desk), back + 1, route + ": masaya varılmadı");
            yield return BoardPlayKit.Frames(2);

            Assert.AreEqual(SiginakPose.Desk, rig.Current, route.ToString());
            Assert.IsFalse(rig.InTransition, route.ToString());
            BoardPlayKit.AssertTransition(log, SiginakPose.Desk, back, route.ToString());
            BoardPlayKit.AssertAtAnchor(SiginakPose.Desk, route.ToString());
            Assert.AreEqual(1, log.Changes.Count, route + ": tek poz değişimi (K4 → K1)");
            if (route != BackRoute.RightClick) Assert.IsEmpty(log.Backs, route + ": sağ tık olayı yok");
            Assert.AreEqual(before, BoardPlayKit.Snapshot(), route + ": geri dönüş komut yazmaz, RNG çekmez");
        }

        /// <summary>
        /// P3: K4'te sağ tuşla 40 px sürükleme (5 kare) bakıştır: poz K4'te kalır, <see cref="SiginakCameraRig.BackClicked"/>
        /// yayınlanmaz, sapma ≈ 40 × 0,09 = 3,6° (yumuşatma oturduktan sonra; ölü bölgede bekleyen hareket kaybolmaz).
        /// </summary>
        [UnityTest]
        public IEnumerator RightDrag_Looks_StaysOnK4()
        {
            yield return BoardPlayKit.EnterBoard();
            var rig = BoardPlayKit.Rig;
            Assert.IsTrue(rig.TryGetPose(SiginakPose.Shelf, out var slot), "K4 pozu");
            log = new PoseLog(rig);
            var before = BoardPlayKit.Snapshot();
            const float move = 40;
            float expected = move * rig.LookSensitivity;
            Assert.AreEqual(3.6f, expected, 1e-4f, "bakış duyarlılığı 0,09°/px (E §2.3)");
            Assert.AreEqual(0, Yaw(slot.anchor, rig.transform), 0.05f, "başlangıçta bakış sıfır");

            yield return BoardPlayKit.RightDrag(BoardPlayKit.ScreenCentre, new Vector2(move, 0), 5);
            // Bakış yumuşatılır (lookSmoothing 18/s): 0,4 s sonra hedefe kalan pay e^-7,2 ≈ %0,08
            yield return BoardPlayKit.WaitSeconds(0.4f);

            Assert.AreEqual(SiginakPose.Shelf, rig.Current, "sağ sürükleme K4'te bırakır");
            Assert.IsFalse(rig.InTransition, "geçiş başlamadı");
            Assert.IsEmpty(log.Backs, "ölü bölgeyi aşan sağ basış BackClicked değildir");
            Assert.IsEmpty(log.Changes, "poz değişmedi");
            float yaw = Yaw(slot.anchor, rig.transform);
            Assert.AreNotEqual(0f, yaw, "bakış uygulandı");
            Assert.AreEqual(expected, yaw, 0.3f, "sapma = 40 px × 0,09 (eşikte bekleyen 16 px dahil)");
            Assert.Less(Vector3.Distance(slot.anchor.position, rig.transform.position), 1e-4f, "bakış kamerayı yerinden oynatmaz");
            Assert.AreEqual(before, BoardPlayKit.Snapshot(), "bakış komut yazmaz, RNG çekmez");
        }

        /// <summary>
        /// P4: K4'te kişi kartına çift tık dosyayı açar; ilk Esc yalnız dosyayı kapatır (poz K4'te kalır), ikinci Esc 0,5 s'de
        /// masaya döndürür (PLAN §1.5 geri zinciri: tutulan kart → dosya → defter → kapı → masa; seçim ve deste Esc tüketmez).
        /// </summary>
        [UnityTest]
        public IEnumerator Esc_ClosesFolderFirst_ThenLeaves()
        {
            yield return BoardPlayKit.EnterBoard();
            var rig = BoardPlayKit.Rig;
            var board = BoardPlayKit.Board;
            Assert.IsNotNull(board, "BoardView3D sahnede (SiginakRoom/Planning board)");
            Assert.IsNotNull(board.Folder, "BoardView3D.Folder (PersonFolderView)");
            Assert.IsTrue(board.Interactive, "K4'e varınca pano girdisi açık");
            float back = Token("motion.board.backToDeskS", 0.5f);
            var start = BoardPlayKit.Snapshot();

            var card = PocketPerson(board);
            if (card == null) Assert.Inconclusive("V.001'de cepte görünür kişi kartı yok; dosya çift tıkla açılamadı");
            BoardPlayKit.Click(card.gameObject);
            yield return BoardPlayKit.Frames(1);
            BoardPlayKit.Click(card.gameObject, 2);
            yield return BoardPlayKit.WaitFor(() => board.Folder.IsOpen, 1,
                "kişi kartına çift tık dosyayı açmadı (BoardDragController.CardDoubleClicked → PlanningBoardPresenter)");
            yield return BoardPlayKit.WaitFor(() => !board.Folder.Moving, 2, "dosyanın açılışı bitmedi");
            Assert.AreEqual(SiginakPose.Shelf, rig.Current, "dosya açmak K4'te bırakır");

            log = new PoseLog(rig);
            yield return BoardPlayKit.PressKey(Key.Escape);
            Assert.IsFalse(board.Folder.IsOpen, "ilk Esc dosyayı kapatır");
            // Yanlışlıkla masaya dönüş başlamış olsaydı bu sürede görünürdü
            yield return BoardPlayKit.WaitSeconds(back + 0.1f);
            Assert.AreEqual(SiginakPose.Shelf, rig.Current, "ilk Esc K4'te bırakır");
            Assert.IsEmpty(log.Changes, "ilk Esc poz değiştirmez");
            yield return BoardPlayKit.WaitFor(() => !board.Folder.Moving, 2, "dosyanın kapanışı bitmedi");

            BoardPlayKit.ResetFrameStats();
            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return BoardPlayKit.WaitFor(() => log.Arrived(SiginakPose.Desk), back + 1, "ikinci Esc masaya döndürmedi");
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(SiginakPose.Desk, rig.Current, "ikinci Esc masaya döndürür");
            BoardPlayKit.AssertTransition(log, SiginakPose.Desk, back, "ikinci Esc");
            Assert.IsFalse(board.Folder.IsOpen, "dosya kapalı kalır");
            Assert.AreEqual(start, BoardPlayKit.Snapshot(), "seçim, dosya ve Esc komut yazmaz, RNG çekmez");
        }

        // ---------- Yardımcılar ----------

        /// <summary>Tema token'ı; PLAN §3.9'daki sayıyla da karşılaştırılır (token değişirse plan da güncellenmeli).</summary>
        static float Token(string key, float plan)
        {
            var theme = SiginakTheme.Active;
            Assert.IsNotNull(theme, "SiginakTheme.Active");
            Assert.IsTrue(theme.TryNumber(key, out var value), key);
            Assert.AreEqual(plan, value, 1e-4f, key + " (PLAN §3.9)");
            return value;
        }

        /// <summary>Masadaki SIĞINAK istasyon tuşunun tıklanır hedefi (StationKeyRail, 0 numaralı tuş).</summary>
        static DeskTarget ShelterKey()
        {
            var rail = BoardPlayKit.Find<StationKeyRail>();
            Assert.IsNotNull(rail, "StationKeyRail");
            Assert.Greater(rail.Count, StationKeyRail.Shelter, "istasyon tuşları");
            var key = rail[StationKeyRail.Shelter];
            Assert.IsNotNull(key, "SIĞINAK tuşu");
            Assert.IsNotNull(key.Target, "SIĞINAK tuşunun DeskTarget'ı");
            Assert.IsTrue(key.Target.Interactable, "SIĞINAK tuşu K1'de tıklanır");
            return key.Target;
        }

        /// <summary>İlk dolu cepteki kişinin görünür kartı (çift tıkla dosyası açılır); yoksa null.</summary>
        static BoardCardView PocketPerson(BoardView3D board)
        {
            var s = board.Last;
            Assert.IsNotNull(s, "BoardView3D.Last (pano K4'te çizilmedi)");
            foreach (var job in PocketOrder)
            {
                if (!s.PocketOccupant.TryGetValue(job, out var id) || string.IsNullOrEmpty(id)) continue;
                var card = board.CardFor(id);
                if (card != null && card.isActiveAndEnabled && card.Kind == BoardCardKind.Person) return card;
            }
            return null;
        }

        /// <summary>Kameranın çapaya göre sapması (derece, dünya yukarısı çevresinde; sağa artı).</summary>
        static float Yaw(Transform anchor, Transform camera)
        {
            Vector3 a = Vector3.ProjectOnPlane(anchor.forward, Vector3.up), c = Vector3.ProjectOnPlane(camera.forward, Vector3.up);
            return Vector3.SignedAngle(a, c, Vector3.up);
        }
    }
}
