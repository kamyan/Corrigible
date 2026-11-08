using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tarif defteri ve üretim (Revizyon 3 · Aşama 5, R18; PLAN §5.2 P1–P17, E §3; Karar D1-B: tarif kartı iş emrine
    /// <c>board_queue</c>, dolu emre <c>board_cancel</c> + <c>board_queue</c>, fiş <c>board_reserve ±1</c>, formdan dışarı
    /// <c>board_cancel</c>). DeskGame gerçek sahnesiyle Play'de, tohum 79 (V.001: metal 3 · devre 2 · kumaş 2 · kimyasal 1, ÜRETİM'de
    /// Suna). Defter tıkları ve tarif sürüklemesi <b>gerçek ışınla</b> (<see cref="BoardPlayKit.FirstHit"/>; örtülme de sınanır) ve
    /// EventSystem'in hiyerarşi kuralıyla (<see cref="PointerGesture"/>) verilir; kart sürüklemeleri Aşama 3'ün
    /// <see cref="BoardPlayKit.Drag"/>'iyle; üzerine gelme sanal fareyle. Süreler token'dan (<c>motion.board.*</c>, yerel
    /// <c>recipeBook.*</c>), tolerans token + iki kare (<see cref="BoardPlayKit.FrameTolerance"/>). Defterin ve kartların hareketi
    /// <c>Update</c>'te yürür: örnekler <c>yield return null</c>'dan sonra (o karenin <c>Update</c>'inden sonra, <c>LateUpdate</c>'ten
    /// önce) alınır; <c>LateUpdate</c>'te yazılanlar (defter kipi, etiket, sayfa yerleşimi) bir kare sonra okunur.
    /// Her sunum adımı komut yazmaz ve RNG çekmez; komut beklenen adımda ad, <c>a</c>, <c>b</c> ve gün denetlenir.
    /// Defter sahneye bağlı değilse (W6 sahne adımından önce) testler nedeniyle düşer.
    /// </summary>
    public sealed class ProductionPlayTests
    {
        PoseLog poses;
        BoardSoundLog sounds;
        PointerGesture gesture;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (gesture != null && !gesture.Released) gesture.Release();
            gesture = null;
            poses?.Dispose();
            poses = null;
            sounds?.Dispose();
            sounds = null;
            yield return ShiftPlayKit.End();
        }

        // ================================================================== ortak

        static float Tol => BoardPlayKit.FrameTolerance;
        static BoardView3D Board => BoardPlayKit.Board;
        static RecipeBookView Book => BoardPlayKit.Book;
        static Simulation Sim => BoardPlayKit.Sim;
        static BoardDragController Drag => Board.Drag;
        static BoardUiState Ui => BoardPlayKit.Presenter.Ui;

        static void AssertDuration(float took, float seconds, string what) =>
            Assert.That(took, Is.InRange(seconds - Tol, seconds + Tol), what + ": " + took.ToString("0.000") + " s (beklenen " + seconds + " ± " + Tol.ToString("0.000") + ")");

        static void AssertColor(Color expected, Color actual, string what)
        {
            const float eps = 1.5f / 255f;
            Assert.That(Mathf.Abs(expected.r - actual.r) <= eps && Mathf.Abs(expected.g - actual.g) <= eps && Mathf.Abs(expected.b - actual.b) <= eps,
                what + ": " + actual + " (beklenen " + expected + ")");
        }

        /// <summary>Görünümün okuduğu token, PLAN §1.14'teki sayıyla (token değişirse plan da güncellenmeli).</summary>
        static float Plan(float value, float plan, string key)
        {
            Assert.AreEqual(plan, value, 1e-4f, key + " (PLAN §1.14)");
            return value;
        }

        static void Ok(ActResult r, string what) => Assert.IsTrue(r != null && r.Ok, what + ": " + r?.Msg);

        /// <summary>Defter ve pano sahneye bağlı (W6 §2.3 #1–#8): görünüm, parçalar, dört sayfa yüzü, üç çarpıştırıcı, denetçi.</summary>
        static void RequireBook()
        {
            var board = Board;
            Assert.IsNotNull(board, "BoardView3D sahnede (SiginakRoom/Planning board)");
            Assert.IsNotNull(board.Drag, "BoardView3D.drag (BoardDragController)");
            var book = board.Book;
            Assert.IsNotNull(book, "BoardView3D.book → Planning board/Tarif defteri (RecipeBookView; W6 §2.3 #1, #8)");
            Assert.IsNotNull(book.Frame, "RecipeBookView.frame → Planning board model kökü (W6)");
            Assert.IsNotNull(book.Cover, "RecipeBookView.cover → Cover PartDriver (W6)");
            Assert.IsNotNull(book.Leaf, "RecipeBookView.leaf → Leaf PartDriver (W6)");
            Assert.IsNotNull(book.PageLeft, "RecipeBookView.pageLeft (Sayfa L; W6 §2.3 #2)");
            Assert.IsNotNull(book.PageRight, "RecipeBookView.pageRight (Sayfa R; W6 §2.3 #3)");
            Assert.IsNotNull(book.LeafFront, "RecipeBookView.leafFront (Yaprak ön; W6 §2.3 #4)");
            Assert.IsNotNull(book.LeafBack, "RecipeBookView.leafBack (Yaprak arka; W6 §2.3 #4)");
            Assert.IsNotNull(book.ClosedHit, "RecipeBookView.closedHit (kökün BoxCollider'ı; W6)");
            Assert.IsNotNull(book.HitLeft, "RecipeBookView.hitLeft (Sayfa L / Çarpıştırıcı; W6)");
            Assert.IsNotNull(book.HitRight, "RecipeBookView.hitRight (Sayfa R / Çarpıştırıcı; W6)");
        }

        /// <summary>DeskGame (tohum 79), defter bağlı, K4'te; ses günlüğü açık.</summary>
        IEnumerator DeskAtBoard()
        {
            yield return BoardPlayKit.LoadDesk();
            RequireBook();
            yield return BoardPlayKit.EnterBoard();
            sounds = new BoardSoundLog(Board);
        }

        /// <summary>Kökün pano çerçevesindeki (model yereli) konumu ve dönüşü: okuma ve alçak pozlar bu çerçevede tanımlı.</summary>
        static Vector3 FramePos(RecipeBookView b) => b.Frame != null ? b.Frame.InverseTransformPoint(b.Root.position) : b.Root.localPosition;
        static Quaternion FrameRot(RecipeBookView b) => b.Frame != null ? Quaternion.Inverse(b.Frame.rotation) * b.Root.rotation : b.Root.localRotation;

        /// <summary>Parçanın uç değeri (aralığın büyük mutlak ucu: Cover −178, Leaf −176).</summary>
        static float End(PartDriver p) => Mathf.Abs(p.RangeTo) > Mathf.Abs(p.RangeFrom) ? p.RangeTo : p.RangeFrom;

        struct BookSample
        {
            public float At;
            public int Frame;
            public BookState State;
            public Vector3 Pos;
            public Quaternion Rot;
            public float Cover, Leaf, Hover;
            public bool LeafShown;
        }

        static BookSample SampleBook()
        {
            var b = Book;
            return new BookSample
            {
                At = Time.unscaledTime, Frame = Time.frameCount, State = b.State, Pos = FramePos(b), Rot = FrameRot(b),
                Cover = b.CoverProgress, Leaf = b.LeafProgress, Hover = b.HoverProgress, LeafShown = b.Leaf != null && b.Leaf.gameObject.activeSelf,
            };
        }

        /// <summary>Koşul doğru olana dek her karede defter örneği (kare sonu: o karenin Update'inden sonra).</summary>
        static IEnumerator WatchBook(List<BookSample> into, Func<bool> until, float timeout, string what)
        {
            float end = Time.unscaledTime + timeout;
            while (true)
            {
                yield return null;
                BoardPlayKit.TrackFrame();
                into?.Add(SampleBook());
                if (until()) yield break;
                if (Time.unscaledTime > end) Assert.Fail("zaman aşımı (" + timeout + " s): " + what + " (defter " + Book.State + ")");
            }
        }

        static bool IsBookCollider(GameObject go)
        {
            var b = Book;
            return go != null && b != null && (b.ClosedHit != null && go == b.ClosedHit.gameObject || b.HitLeft != null && go == b.HitLeft.gameObject
                || b.HitRight != null && go == b.HitRight.gameObject);
        }

        static bool OnScreen(Vector2 p) => p.x > 4 && p.y > 4 && p.x < Screen.width - 4 && p.y < Screen.height - 4;

        /// <summary>Kapalı defterin gövdesinin K4'teki gerçek ışını: ilk sonuç defterin kendi çarpıştırıcısı olmalı (örtülmez).</summary>
        static RaycastResult ClosedBookHit(string context)
        {
            var col = Book.ClosedHit;
            Assert.IsTrue(col.enabled, context + ": kapalı defterin çarpıştırıcısı K4'te açık (RecipeBookView.SetInteractive)");
            var screen = BoardPlayKit.ScreenOfWorld(col.transform.TransformPoint(col.center));
            var hit = BoardPlayKit.FirstHit(screen);
            Assert.AreSame(col.gameObject, hit.gameObject, context + ": K4'te kapalı defterin önü örtülmemeli; ışın " + BoardPlayKit.DescribeHits(screen));
            return hit;
        }

        static Vector2 NextMm => RecipePageMetrics.Default.Geometry(true).NextHit.center;
        static Vector2 PrevMm => RecipePageMetrics.Default.Geometry(false).PrevHit.center;
        static Vector2 BoxMm(bool right) => RecipePageMetrics.Default.Geometry(right).Box.center;

        /// <summary>Açık defterin sayfa noktasının gerçek ışını: ilk sonuç o sayfanın çarpıştırıcısı olmalı.</summary>
        static RaycastResult PageHit(bool right, Vector2 mm, string context)
        {
            var b = Book;
            var page = right ? b.PageRight : b.PageLeft;
            var col = right ? b.HitRight : b.HitLeft;
            Assert.IsTrue(col.enabled, context + ": açık defterin " + (right ? "sağ" : "sol") + " sayfa çarpıştırıcısı açık");
            var screen = BoardPlayKit.ScreenOfWorld(page.WorldAt(mm));
            Assert.IsTrue(OnScreen(screen), context + ": sayfa noktası kadrajda (" + screen + ")");
            var hit = BoardPlayKit.FirstHit(screen);
            Assert.AreSame(col.gameObject, hit.gameObject, context + ": sayfa (" + mm + " mm) örtülmemeli; ışın " + BoardPlayKit.DescribeHits(screen));
            return hit;
        }

        /// <summary>
        /// Defter açıkken pano yüzüne düşen boş bir nokta (dosya, hurda, depo, boşta alanı, arduvaz adaylarından ilki): defter kipinde kart ve
        /// sabit hedef çarpıştırıcıları kapalı olduğundan ilk sonuç <c>BoardView3D.boardFace</c> olmalı.
        /// </summary>
        static RaycastResult BoardSpaceHit(string context)
        {
            var face = ShiftPlayKit.Get<Collider>(Board, "boardFace");
            Assert.IsNotNull(face, context + ": BoardView3D.boardFace → Pano yüzü (W6; defter kipinde açık kalan pano hedefi)");
            Vector2[] candidates =
            {
                BoardLayout.Folder.center, BoardLayout.Hurda.center, BoardLayout.Kit(5), BoardLayout.Material(0), BoardLayout.Bosta.center,
                BoardLayout.Slate.center, BoardLayout.Kit(0),
            };
            var tried = new List<string>();
            foreach (var mm in candidates)
            {
                var screen = BoardPlayKit.BoardScreen(mm);
                if (!OnScreen(screen)) continue;
                var hit = BoardPlayKit.FirstHit(screen);
                tried.Add(mm + " → " + (hit.gameObject != null ? hit.gameObject.name : "—"));
                if (hit.gameObject == face.gameObject) return hit;
            }
            Assert.Fail(context + ": pano yüzüne düşen boş nokta bulunamadı (" + string.Join("; ", tried) + ")");
            return default;
        }

        /// <summary>Açık defter: kapalı deftere gerçek ışınla tık, açılış bitene dek bekler (sayfa yerleşimi bir kare sonra).</summary>
        static IEnumerator OpenBook(string context)
        {
            var b = Book;
            Assert.AreEqual(BookState.Closed, b.State, context + ": defter kapalı başlar");
            var hit = ClosedBookHit(context);
            Assert.IsTrue(BoardPlayKit.ClickHit(hit), context + ": tık");
            Assert.AreEqual(BookState.Opening, b.State, context + ": kapalı deftere tık açar (B1)");
            yield return BoardPlayKit.WaitFor(() => b.State == BookState.Open && !b.Moving, 2, context + ": defter açılmadı (" + b.State + ")");
            yield return BoardPlayKit.Frames(1);
        }

        static IEnumerator WaitClosed(string context)
        {
            var b = Book;
            yield return BoardPlayKit.WaitFor(() => b.State == BookState.Closed && !b.Moving, 2, context + ": defter kapanmadı (" + b.State + ")");
            yield return BoardPlayKit.Frames(1);
        }

        /// <summary>Panonun ortak üzerine gelme etiketinin (denetçinin <c>hoverTag</c>'i) sahibi, başlığı, satırı ve çerçevesi.</summary>
        static (IHoverSource owner, string title, string line, Color? frame) TagState(HoverTag tag)
        {
            if (tag == null) return (null, null, null, null);
            var owner = tag.Owner;
            Color? frame = owner is IHoverFrame f ? f.Frame : null;
            return (owner, owner?.HoverTitle, owner?.Line, frame);
        }

        static HoverTag BoardTag => ShiftPlayKit.Get<HoverTag>(Drag, "hoverTag");

        /// <summary>
        /// [B] Tarif kutusuna (<paramref name="right"/> sayfası) gerçek ışınla basar ve sürükleme eşiğini geçer: defter
        /// <see cref="RecipeBookView.RecipeLifted"/> yayar, sunucu kopyayı denetçiye verir (<c>Adopt</c>), sonraki olaylar kopyaya gider.
        /// Hareket <see cref="gesture"/>'da kalır (bırakılmaz).
        /// </summary>
        IEnumerator LiftRecipe(bool right, string context)
        {
            var hit = PageHit(right, BoxMm(right), context + ": tarif kutusu");
            gesture = BoardPlayKit.Press(hit);
            Assert.AreSame(Book.gameObject, gesture.Dragged, context + ": sürükleme olaylarının alıcısı defter (RecipeBookView, IDragHandler)");
            yield return gesture.Move(hit.screenPosition + new Vector2(16, -16), 1);
        }

        /// <summary>Tutulan kartı iş emrinin ortasına götürür ve bırakmadan <paramref name="hold"/> kare bekler.</summary>
        IEnumerator HoldOverOrder(int hold = 4)
        {
            yield return gesture.Move(BoardPlayKit.BoardScreen(BoardLayout.Order.center), 10);
            yield return gesture.Hold(hold);
        }

        /// <summary>Zırh plakası emri, girdileri tam ayrılmış (gerçek komutlar: <c>board_queue</c> + iki <c>board_reserve</c>).</summary>
        static IEnumerator ArmorOrderFull()
        {
            var game = BoardPlayKit.Game;
            Ok(game.ExecuteBoardCommand(s => s.QueueProduction("armor")), "board_queue armor");
            Ok(game.ExecuteBoardCommand(s => s.ReserveProductionMaterial("metal", s.B.boardArmorMetal)), "board_reserve metal");
            Ok(game.ExecuteBoardCommand(s => s.ReserveProductionMaterial("kumas", s.B.boardArmorCloth)), "board_reserve kumas");
            yield return BoardPlayKit.Frames(2);
        }

        static void AssertCommands(IList<SavedCommand> got, string context, params (string kind, string a, string b)[] want)
        {
            Assert.AreEqual(want.Length, got.Count, context + ": komutlar " + ShiftPlayKit.Describe(got));
            for (int i = 0; i < want.Length; i++)
            {
                Assert.AreEqual(want[i].kind, got[i].kind, context + ": " + (i + 1) + ". komutun türü (" + ShiftPlayKit.Describe(got) + ")");
                if (want[i].a != null) Assert.AreEqual(want[i].a, got[i].a, context + ": " + got[i].kind + " a");
                if (want[i].b != null) Assert.AreEqual(want[i].b, got[i].b, context + ": " + got[i].kind + " b");
                Assert.AreEqual(Sim.S.Day, got[i].day, context + ": " + got[i].kind + " bugüne yazılır");
            }
        }

        static RecipeData RecipeOf(string id)
        {
            var r = Board.Last.Recipes.FirstOrDefault(x => x != null && x.Id == id);
            Assert.IsNotNull(r, "anlık görüntüde tarif " + id);
            return r;
        }

        static BoardEquipment NewestStored(string kind) =>
            Sim.Board.Equipment.Where(e => e.Kind == kind && string.IsNullOrEmpty(e.OwnerId))
                .OrderBy(e => PlanningBoardSnapshotBuilder.EquipmentNumber(e.Id)).LastOrDefault();

        static int Quantity(string kind) => Sim.Board.Material(kind)?.Quantity ?? 0;

        // ================================================================== P1

        /// <summary>
        /// P1 (R18a; PLAN §1.4 B0, B1): kapalı defter kancada (Aşama 3 pozu); sanal fare üzerine gelince kök pano −z'sinde 4 mm öne
        /// (<c>recipeBook.hoverMm</c>, <c>cardLiftS</c> 0,08 s), 0,4 s sonra etiket "TARİF DEFTERİ" · "6 tarif". Gerçek ışınla tık: kök
        /// okuma pozuna <c>bookForwardS</c> 0,35 s ease-out (yarı sürede yolun ≥ %60'ı), kapak 0,10 s'de başlar ve 0,35 s'de −178°'de;
        /// yaprak gizli, açılım 1 (TARİF 01 Zırh plakası / 02 Bakım kiti). Kamera K4'te kalır, arka plan karartılmaz (yeni Overlay
        /// tuval yok), tek <c>BookOpen</c> ses olayı, defter kipi (kart girdisi askıda), komut ve RNG aynı.
        /// </summary>
        [UnityTest]
        public IEnumerator Book_FromHook_ComesForward_Opens()
        {
            yield return DeskAtBoard();
            var book = Book;
            var rig = BoardPlayKit.Rig;
            float forward = Plan(RecipeBookView.ForwardS, 0.35f, "motion.board.bookForwardS");
            float coverS = Plan(RecipeBookView.CoverS, 0.25f, "motion.board.bookCoverS");
            float delay = Plan(RecipeBookView.CoverDelayS, 0.1f, "recipeBook.coverDelayS");
            float hoverMm = Plan(RecipeBookView.HoverMm, 4f, "recipeBook.hoverMm");
            float hoverS = Plan(RecipeBookView.HoverS, 0.08f, "motion.board.cardLiftS");
            float tagDelay = ShiftPlayKit.TokenOr("motion.tooltipDelayS", 0.4f);

            Assert.IsTrue(book.Interactive, "K4'e varınca defter etkileşimli");
            Assert.AreEqual(BookState.Closed, book.State, "açılışta defter kapalı");
            var closed = RecipeBookView.ClosedPoseLocal;
            Assert.Less(Vector3.Distance(closed.position, book.Root.localPosition), 1e-4f, "kapalı defter kancada (Aşama 3 pozu ±0,1 mm; D11)");
            Assert.Less(Quaternion.Angle(closed.rotation, book.Root.localRotation), 0.05f, "kapalı defterin eğimi x −4°");
            Assert.IsTrue(book.Leaf.gameObject.activeSelf, "kapalıyken yaprak görünür");
            Assert.IsFalse(book.PageLeft.gameObject.activeSelf || book.PageRight.gameObject.activeSelf, "kapalıyken sayfa tuvalleri kapalı");
            var closedFrame = FramePos(book);
            var overlays = OverlayCanvases();
            var start = BoardPlayKit.Snapshot();
            poses = new PoseLog(rig);

            // B0: üzerine gelme (sanal fare; UI modülü olayı vermezse doğrudan giriş)
            var hit = ClosedBookHit("üzerine gelme");
            BoardPlayKit.ResetFrameStats();
            yield return BoardPlayKit.MoveMouse(hit.screenPosition);
            float hoverAt = -1;
            bool viaMouse = true;
            for (int i = 0; i < 4 && hoverAt < 0; i++)
            {
                if (book.Hovered) hoverAt = Time.unscaledTime;
                else yield return BoardPlayKit.Frames(1);
            }
            if (hoverAt < 0)
            {
                viaMouse = false;
                TestContext.WriteLine("sanal fare defterin üzerine gelmesini UI modülünden vermedi; giriş olayı doğrudan verildi");
                ExecuteEvents.Execute(book.gameObject, BoardPlayKit.PointerAt(hit), ExecuteEvents.pointerEnterHandler);
                hoverAt = Time.unscaledTime;
            }
            Assert.IsTrue(book.Hovered, "kapalı defterin üzerine gelindi");
            yield return BoardPlayKit.WaitSeconds(hoverS + Tol);
            var lift = FramePos(book) - closedFrame;
            Assert.AreEqual(1f, book.HoverProgress, 1e-4f, "üzerine gelme " + hoverS + " s'de tamam");
            Assert.AreEqual(-hoverMm, lift.z * 1000f, 0.1f, "kök pano −z'sinde 4 mm öne (D10: öteleme)");
            Assert.Less(new Vector2(lift.x, lift.y).magnitude * 1000f, 0.1f, "üzerine gelme yalnız öne öteler");
            var tag = book.Tag;
            Assert.IsNotNull(tag, "RecipeBookView.Tag (BoardView3D'nin HoverTag'i)");
            yield return BoardPlayKit.WaitFor(() => tag.Visible && tag.Owner == (IHoverSource)book, tagDelay + 1, "defterin etiketi açılmadı");
            if (viaMouse) AssertDuration(Time.unscaledTime - hoverAt, tagDelay, "etiket gecikmesi (motion.tooltipDelayS, yedek 0,4)");
            Assert.AreEqual(Loc.T("TARİF DEFTERİ"), ShiftPlayKit.Get<TMP_Text>(tag, "title").text, "etiket başlığı");
            Assert.AreEqual(Loc.F("{0} tarif", 6), ShiftPlayKit.Get<TMP_Text>(tag, "line").text, "etiket satırı (altı tarif, veriden)");
            Assert.AreEqual(Board.Last.BookTitle, book.HoverTitle, "başlık anlık görüntüden");
            Assert.AreEqual(Board.Last.BookLine, book.Line, "satır anlık görüntüden");
            if (viaMouse) yield return BoardPlayKit.MoveMouse(new Vector2(6, 6));
            else ExecuteEvents.Execute(book.gameObject, BoardPlayKit.PointerAt(hit), ExecuteEvents.pointerExitHandler);
            yield return BoardPlayKit.WaitFor(() => book.HoverProgress <= 0f, 1, "üzerine gelme bitmedi");
            Assert.Less(Vector3.Distance(FramePos(book), closedFrame), 1e-4f, "üzerine gelme bitince kök kancada");

            // B1: tık → okuma pozu
            hit = ClosedBookHit("açılış");
            BoardPlayKit.ResetFrameStats();
            float t0 = Time.unscaledTime;
            Assert.IsTrue(BoardPlayKit.ClickHit(hit), "deftere tık");
            Assert.AreEqual(BookState.Opening, book.State, "tık defteri açar (B1)");
            Assert.AreEqual(1, sounds.Count(BoardSound.BookOpen), "tek BookOpen ses olayı (" + sounds + ")");
            var samples = new List<BookSample>();
            yield return WatchBook(samples, () => book.State == BookState.Open && !book.Moving, 2, "defter açılmadı");
            var read = RecipeBookView.ReadPoseLocal;
            float path = Vector3.Distance(read.position, closedFrame);
            Assert.Greater(path, 0.1f, "okuma pozu kancadan uzakta");
            var half = samples.First(s => s.At - t0 >= forward * 0.5f);
            float progress = 1f - Vector3.Distance(half.Pos, read.position) / path;
            Assert.GreaterOrEqual(progress, 0.6f, "ease-out: yarı sürede (" + (half.At - t0).ToString("0.000") + " s) yolun ≥ %60'ı (" + progress.ToString("0.00") + ")");
            foreach (var s in samples.Where(s => s.At - t0 < delay - Tol))
                Assert.AreEqual(0f, s.Cover, 1e-4f, "kapak " + delay + " s'den önce kalkmaz (" + (s.At - t0).ToString("0.000") + " s)");
            var coverDone = samples.First(s => s.Cover >= 0.9999f);
            AssertDuration(coverDone.At - t0, delay + coverS, "kapak −178°'de (coverDelayS + bookCoverS)");
            var opened = samples.First(s => s.State == BookState.Open);
            AssertDuration(opened.At - t0, Mathf.Max(forward, delay + coverS), "defter açık (bookForwardS, örtüşük kapak)");
            Assert.Less(Vector3.Distance(FramePos(book), read.position), 1e-4f, "okuma pozu (recipeBook.readPoseLocal ±0,1 mm)");
            Assert.Less(Quaternion.Angle(FrameRot(book), read.rotation), 0.05f, "okuma pozunun eğimi (x −9°)");
            Assert.AreEqual(-178f, End(book.Cover), 0.01f, "kapağın açık ucu manifestte −178°");
            Assert.AreEqual(End(book.Cover), book.Cover.Value, 0.05f, "kapak açık uca oturdu");
            Assert.IsFalse(book.Leaf.gameObject.activeSelf, "açıkken yaprak gizli");
            Assert.IsTrue(book.PageLeft.gameObject.activeSelf && book.PageRight.gameObject.activeSelf, "açıkken sayfa tuvalleri açık");

            // açılım 1: TARİF 01 / 02
            Assert.AreEqual(0, book.Spread, "ilk açılış açılım 1'de");
            Assert.AreEqual(3, book.SpreadCount, "üç açılım");
            var ids = Board.Last.Recipes.Select(r => r.Id).ToArray();
            CollectionAssert.AreEqual(new[] { "armor", "medkit", "emp_cell", "kit_tek", "kit_emp", "kit_ana" }, ids, "defterin sayfa sırası Core'un sırası");
            Assert.AreEqual("armor", book.PageLeft.Recipe?.Id, "sol sayfa TARİF 01");
            Assert.AreEqual("medkit", book.PageRight.Recipe?.Id, "sağ sayfa TARİF 02");
            Assert.AreEqual(RecipeOf("armor").Title, book.PageLeft.Title.text, "sol sayfanın başlığı (büyük harf)");
            Assert.AreEqual(RecipeOf("medkit").Title, book.PageRight.Title.text, "sağ sayfanın başlığı");
            Assert.IsFalse(book.PageLeft.ShowsPrev, "açılım 1'de önceki sekmesi yok");
            Assert.IsTrue(book.PageRight.ShowsNext, "açılım 1'de sonraki sekmesi var");
            Assert.IsFalse(book.PageLeft.Overflowing || book.PageRight.Overflowing, "sayfalar taşmaz (" + string.Join(", ", book.PageLeft.OverflowFields.Concat(book.PageRight.OverflowFields)) + ")");

            // defter kipi, kamera, ekran
            yield return BoardPlayKit.Frames(1);
            Assert.IsTrue(Board.BookMode, "defter kipi açık (BoardView3D.SetBookMode; PLAN §1.12)");
            Assert.IsTrue(Drag.Suspended, "defter açıkken kart girdisi askıda");
            Assert.IsEmpty(poses.Changes, "kamera K4'te kalır (defter kameraya gelir)");
            Assert.AreEqual(SiginakPose.Shelf, rig.Current);
            BoardPlayKit.AssertAtAnchor(SiginakPose.Shelf, "defter açık");
            CollectionAssert.AreEquivalent(overlays, OverlayCanvases(), "arka plan karartılmaz, ekranda düğme yok (yeni Overlay tuval açılmadı)");
            Assert.AreEqual(1, sounds.Count(BoardSound.BookOpen), "açılışta tek BookOpen (" + sounds + ")");
            Assert.AreEqual(start, BoardPlayKit.Snapshot(), "defteri açmak komut yazmaz, RNG çekmez");
        }

        static List<string> OverlayCanvases() =>
            Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.enabled && c.gameObject.activeInHierarchy)
                .Select(c => c.name).OrderBy(n => n).ToList();

        // ================================================================== P2

        /// <summary>
        /// P2 (R18a; B2–B4, B11): "sonraki ►" köşesine gerçek ışınla tık yaprağı <c>pageTurnS</c> 0,25 s'de çevirir (yaprak görünür,
        /// açısı tekdüze, bitince gizli; tek <c>PageTurn</c>); açılım 2 = 03 EMP hücresi / 04 Teknisyen kiti, 3 = 05 EMP kiti /
        /// 06 Analist kiti. Çevirme sürerken tık yok sayılır. Açılım 3'te sonraki, 1'de önceki sekmesi yok ve köşe bir şey yapmaz.
        /// ← tuşu geri çevirir. Açılım yerleşime yazılır (<c>BoardUiState.BookSpread</c>); komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator Book_TurnPages_ByTabs()
        {
            yield return DeskAtBoard();
            var book = Book;
            float turnS = Plan(RecipeBookView.TurnS, 0.25f, "motion.board.pageTurnS");
            yield return OpenBook("açılış");
            var start = BoardPlayKit.Snapshot();
            sounds.Clear();
            string[] left = { "armor", "emp_cell", "kit_emp" }, right = { "medkit", "kit_tek", "kit_ana" };

            for (int to = 1; to <= 2; to++)
            {
                var hit = PageHit(true, NextMm, "sonraki (açılım " + to + ")");
                BoardPlayKit.ResetFrameStats();
                float t0 = Time.unscaledTime;
                int turns = sounds.Count(BoardSound.PageTurn);
                BoardPlayKit.ClickHit(hit);
                Assert.AreEqual(BookState.Turning, book.State, "sonraki köşesi yaprağı çevirir (B2)");
                Assert.AreEqual(turns + 1, sounds.Count(BoardSound.PageTurn), "çevirmenin başında tek PageTurn");
                // B4: çevirme sürerken tık yok sayılır
                BoardPlayKit.ClickHit(PageHit(true, NextMm, "çevirme sürerken"));
                Assert.AreEqual(turns + 1, sounds.Count(BoardSound.PageTurn), "çevirme sürerken tık sıraya girmez (B4)");
                var samples = new List<BookSample>();
                yield return WatchBook(samples, () => book.State == BookState.Open, 2, "çevirme bitmedi");
                AssertDuration(samples.First(s => s.State == BookState.Open).At - t0, turnS, "çevirme (pageTurnS)");
                var turning = samples.Where(s => s.State == BookState.Turning).ToList();
                Assert.IsNotEmpty(turning, "çevirme kareleri");
                Assert.IsTrue(turning.All(s => s.LeafShown), "çevirirken yaprak görünür");
                for (int i = 1; i < turning.Count; i++)
                    Assert.GreaterOrEqual(turning[i].Leaf, turning[i - 1].Leaf - 1e-4f, "yaprak açısı tekdüze artar");
                Assert.IsFalse(book.Leaf.gameObject.activeSelf, "çevirme bitince yaprak gizli");
                Assert.AreEqual(to, book.Spread, "açılım " + (to + 1));
                Assert.AreEqual(left[to], book.PageLeft.Recipe?.Id, "sol sayfa (açılım " + (to + 1) + ")");
                Assert.AreEqual(right[to], book.PageRight.Recipe?.Id, "sağ sayfa (açılım " + (to + 1) + ")");
                Assert.AreEqual(RecipeOf(left[to]).Title, book.PageLeft.Title.text, "sol sayfanın başlığı (açılım " + (to + 1) + ")");
                Assert.AreEqual(to, Ui.BookSpread, "açılım yerleşime yazıldı (bookSpread)");
            }

            // açılım 3: sonraki yok
            Assert.IsFalse(book.PageRight.ShowsNext, "açılım 3'te sonraki sekmesi çizilmez");
            Assert.IsTrue(book.PageLeft.ShowsPrev, "açılım 3'te önceki sekmesi var");
            int before = sounds.Count(BoardSound.PageTurn);
            BoardPlayKit.ClickHit(PageHit(true, NextMm, "açılım 3'te sonraki köşesi"));
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(BookState.Open, book.State, "açılım 3'te sonraki köşesi bir şey yapmaz");
            Assert.AreEqual(2, book.Spread);
            Assert.AreEqual(before, sounds.Count(BoardSound.PageTurn), "ses yok");

            // ← tuşu (B11) ve önceki köşesi
            yield return BoardPlayKit.PressKey(Key.LeftArrow);
            yield return BoardPlayKit.WaitFor(() => book.State == BookState.Open && book.Spread == 1, turnS + 1, "← tuşu geri çevirmedi");
            Assert.AreEqual(before + 1, sounds.Count(BoardSound.PageTurn), "← tek çevirme");
            BoardPlayKit.ClickHit(PageHit(false, PrevMm, "önceki (açılım 2)"));
            Assert.AreEqual(BookState.Turning, book.State, "önceki köşesi yaprağı geri çevirir (B3)");
            var back = new List<BookSample>();
            yield return WatchBook(back, () => book.State == BookState.Open, 2, "geri çevirme bitmedi");
            var backTurning = back.Where(s => s.State == BookState.Turning).ToList();
            for (int i = 1; i < backTurning.Count; i++)
                Assert.LessOrEqual(backTurning[i].Leaf, backTurning[i - 1].Leaf + 1e-4f, "geri çevirmede yaprak açısı tekdüze azalır");
            Assert.AreEqual(0, book.Spread, "açılım 1");
            Assert.AreEqual("armor", book.PageLeft.Recipe?.Id);
            Assert.AreEqual("medkit", book.PageRight.Recipe?.Id);
            Assert.IsFalse(book.PageLeft.ShowsPrev, "açılım 1'de önceki sekmesi çizilmez");
            before = sounds.Count(BoardSound.PageTurn);
            BoardPlayKit.ClickHit(PageHit(false, PrevMm, "açılım 1'de önceki köşesi"));
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(BookState.Open, book.State, "açılım 1'de önceki köşesi bir şey yapmaz");
            Assert.AreEqual(before, sounds.Count(BoardSound.PageTurn));
            Assert.AreEqual(0, Ui.BookSpread, "açılım yerleşimde 0");
            Assert.AreEqual(start, BoardPlayKit.Snapshot(), "sayfa çevirmek komut yazmaz, RNG çekmez");
        }

        // ================================================================== P3

        /// <summary>Açık defteri kapatan yollar (PLAN §1.4 B5, D10).</summary>
        public enum CloseRoute { CoverEdge, BoardSpace, Esc, RightClick, BackSign }

        static readonly CloseRoute[] CloseRoutes = { CloseRoute.CoverEdge, CloseRoute.BoardSpace, CloseRoute.Esc, CloseRoute.RightClick, CloseRoute.BackSign };

        /// <summary>
        /// P3 (B5): kapak kenarına (sol sayfanın dışı) ya da pano boşluğuna gerçek ışınla tık, Esc, sürüklemesiz sağ tık ve MASAYA DÖN
        /// açık defteri kapatır: kapak ilk 0,25 s'de, kök <c>bookCloseS</c> 0,3 s'de kancaya (±0,1 mm), tek <c>BookClose</c>; kamera K4'te
        /// kalır (Esc zinciri: tutulan kart → dosya → KAYIPLAR → <b>defter</b> → kabul → masa). Esc yolunda ikinci Esc masaya döndürür.
        /// Defter kipi biter; komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator Book_Close_ByCoverBoardSpaceEsc([ValueSource(nameof(CloseRoutes))] CloseRoute route)
        {
            yield return DeskAtBoard();
            var book = Book;
            var rig = BoardPlayKit.Rig;
            float closeS = Plan(RecipeBookView.CloseS, 0.3f, "motion.board.bookCloseS");
            float coverS = Mathf.Min(RecipeBookView.CoverS, closeS);
            var closedLocal = book.Root.localPosition;
            yield return OpenBook(route + ": açılış");
            var start = BoardPlayKit.Snapshot();
            sounds.Clear();
            poses = new PoseLog(rig);
            BoardPlayKit.ResetFrameStats();

            switch (route)
            {
                case CloseRoute.CoverEdge:
                {
                    // kapağın dış kenarı: sol sayfanın 3 mm dışı (kapak bezinde çarpıştırıcı yok; ışın pano yüzüne düşer)
                    var screen = BoardPlayKit.ScreenOfWorld(book.PageLeft.WorldAt(new Vector2(-2.5f, 70f)));
                    var hit = BoardPlayKit.FirstHit(screen);
                    Assert.IsFalse(IsBookCollider(hit.gameObject), "kapak kenarı sayfa değildir (ışın " + BoardPlayKit.DescribeHits(screen) + ")");
                    Assert.IsTrue(BoardPlayKit.ClickHit(hit), "kapak kenarının ışını bir şeye çarpar (pano yüzü): " + BoardPlayKit.DescribeHits(screen));
                    break;
                }
                case CloseRoute.BoardSpace:
                    BoardPlayKit.ClickHit(BoardSpaceHit("pano boşluğu"));
                    break;
                case CloseRoute.Esc:
                    yield return BoardPlayKit.PressKey(Key.Escape);
                    break;
                case CloseRoute.RightClick:
                    yield return BoardPlayKit.RightDrag(BoardPlayKit.ScreenCentre, new Vector2(3, 0), 1);
                    break;
                case CloseRoute.BackSign:
                    var sign = BoardPlayKit.BackSignTarget();
                    Assert.IsNotNull(sign, "MASAYA DÖN levhası");
                    Assert.IsTrue(sign.Interactable, "MASAYA DÖN defter kipinde de tıklanır");
                    BoardPlayKit.Click(sign.gameObject);
                    break;
            }

            var samples = new List<BookSample> { SampleBook() };
            if (book.State != BookState.Closed)
                yield return WatchBook(samples, () => book.State == BookState.Closed && !book.Moving, 2, route + ": defter kapanmadı");
            var closing = samples.Where(s => s.State == BookState.Closing).ToList();
            Assert.IsNotEmpty(closing, route + ": defter kapanıyor (B5)");
            float from = closing.First().At;
            var shut = samples.First(s => s.State == BookState.Closed);
            var coverShut = samples.FirstOrDefault(s => s.At >= from && s.Cover <= 1e-4f);
            // Tık yollarında kapanış tıkın karesinde başlar ve ilk örnek o karededir; Esc ve sağ tıkta kapanış girdi karesinde başlar,
            // ilk örnek yardımcının son karesinden sonra (en çok iki kare geç): alt sınır iki kare gevşer
            bool exact = route == CloseRoute.CoverEdge || route == CloseRoute.BoardSpace || route == CloseRoute.BackSign;
            if (exact)
            {
                AssertDuration(shut.At - from, closeS, route + ": kapanış (bookCloseS)");
                AssertDuration(coverShut.At - from, coverS, route + ": kapak önce kapanır (bookCoverS)");
            }
            else
            {
                float late = 2 * BoardPlayKit.MaxFrameDt;
                Assert.That(shut.At - from, Is.InRange(closeS - Tol - late, closeS + Tol), route + ": kapanış (bookCloseS) " + (shut.At - from).ToString("0.000"));
                Assert.That(coverShut.At - from, Is.InRange(coverS - Tol - late, coverS + Tol), route + ": kapak önce kapanır " + (coverShut.At - from).ToString("0.000"));
            }
            Assert.Less(coverShut.At, shut.At + 1e-4f, route + ": kapak kökten önce (ya da aynı karede) kapanır");
            Assert.Less(Vector3.Distance(closedLocal, book.Root.localPosition), 1e-4f, route + ": kök kancada (±0,1 mm)");
            Assert.AreEqual(0f, book.Cover.Value, 0.05f, route + ": kapak kapalı");
            Assert.IsTrue(book.Leaf.gameObject.activeSelf, route + ": kapalıyken yaprak görünür");
            Assert.AreEqual(1, sounds.Count(BoardSound.BookClose), route + ": tek BookClose (" + sounds + ")");
            yield return BoardPlayKit.Frames(1);
            Assert.IsFalse(Board.BookMode, route + ": defter kipi bitti");
            Assert.IsFalse(Drag.Suspended, route + ": kart girdisi açık");
            Assert.AreEqual(SiginakPose.Shelf, rig.Current, route + ": defteri kapatmak K4'te bırakır");
            Assert.IsFalse(poses.Changes.Any(c => c.Pose == SiginakPose.Desk), route + ": masaya dönülmedi (önce defter)");
            Assert.IsTrue(ClosedBookHit(route + ": kapanınca").gameObject != null, route + ": kapalı defter yeniden tıklanır");
            Assert.AreEqual(start, BoardPlayKit.Snapshot(), route + ": defteri kapatmak komut yazmaz, RNG çekmez");

            if (route == CloseRoute.Esc)
            {
                float back = ShiftPlayKit.Token("motion.board.backToDeskS", 0.5f);
                BoardPlayKit.ResetFrameStats();
                yield return BoardPlayKit.PressKey(Key.Escape);
                yield return BoardPlayKit.WaitFor(() => poses.Arrived(SiginakPose.Desk), back + 1, "ikinci Esc masaya döndürmedi");
                BoardPlayKit.AssertTransition(poses, SiginakPose.Desk, back, "ikinci Esc");
                Assert.AreEqual(start, BoardPlayKit.Snapshot(), "masaya dönüş komut yazmaz");
            }
        }

        // ================================================================== P4

        /// <summary>
        /// P4 (R18-not "kilitli plan — defter okunur"; PLAN §1.10): tam zırh emriyle fiş basılır (plan kilitli, K6), K4'e gidilir:
        /// defter açılır ve çevrilir; [B] tarif kutusunda sürükleme denemesi kart kaldırmaz, kök 2 mm titrer + <c>Deny</c> (B10); pano
        /// boşluğuna tık defteri kilitli planda da kapatır (§1.12, P15 alt durumu); fiş ve stok kartı basışta kalkmaz, 2 mm titrer +
        /// <c>Deny</c>. Hiçbir <c>board_*</c> yazılmaz.
        /// </summary>
        [UnityTest]
        public IEnumerator Book_ReadableWhenLocked_ChipsDoNotMove()
        {
            yield return ShiftPlayKit.Begin();
            RequireBook();
            yield return ArmorOrderFull();
            yield return ShiftPlayKit.PrintToTongue();
            Assert.IsTrue(Sim.Board.Locked, "fiş basılınca plan kilitli");
            yield return BoardPlayKit.EnterBoard();
            sounds = new BoardSoundLog(Board);
            var book = Book;
            Assert.IsTrue(Board.Last.Locked, "anlık görüntü kilitli");
            Assert.IsTrue(book.Locked, "defter kilitli (okunur)");
            Assert.IsFalse(book.RecipesDraggable, "kilitli planda tarif sürüklenmez");
            float denyMm = ShiftPlayKit.Token("motion.board.denyShakeMm", 2f), denyS = ShiftPlayKit.TokenOr("motion.board.denyShakeS", 0.18f);
            var mark = ShiftPlayKit.Mark();

            yield return OpenBook("kilitli plan");
            BoardPlayKit.ClickHit(PageHit(true, NextMm, "kilitli planda sonraki"));
            yield return BoardPlayKit.WaitFor(() => book.State == BookState.Open && book.Spread == 1, 1, "kilitli planda sayfa çevrilmedi");

            // B10: tarif kutusu
            sounds.Clear();
            yield return LiftRecipe(false, "kilitli tarif");
            Assert.IsNull(Drag.Adopted, "kilitli planda tarif kopyası kalkmaz");
            Assert.AreEqual(BookState.Open, book.State, "defter alçalmaz");
            Assert.AreEqual(1, sounds.Count(BoardSound.Deny), "ret sesi (" + sounds + ")");
            float peak = 0;
            float until = Time.unscaledTime + denyS + Tol + 0.05f;
            while (Time.unscaledTime < until)
            {
                peak = Mathf.Max(peak, (book.Root.localPosition - book.BasePose.position).magnitude * 1000f);
                yield return null;
                BoardPlayKit.TrackFrame();
            }
            Assert.LessOrEqual(peak, denyMm + 0.05f, "defterin titremesi ≤ 2 mm");
            Assert.Greater(peak, denyMm * 0.5f, "defter titredi (tepe " + peak.ToString("0.00") + " mm)");
            Assert.Less((book.Root.localPosition - book.BasePose.position).magnitude * 1000f, 0.01f, "titreme bitti, kök okuma pozunda");
            gesture.Release();
            gesture = null;

            // pano boşluğu kilitli planda da kapatır
            BoardPlayKit.ClickHit(BoardSpaceHit("kilitli planda pano boşluğu"));
            Assert.AreEqual(BookState.Closing, book.State, "kilitli planda pano boşluğuna tık defteri kapatır (BackgroundClicked)");
            yield return WaitClosed("kilitli plan");

            // fiş ve stok kartı
            foreach (var id in new[] { "chip:metal:0", "metal" })
            {
                var card = Board.CardFor(id);
                Assert.IsNotNull(card, "kart " + id);
                Assert.IsTrue(card.isActiveAndEnabled, "kart görünür: " + id);
                sounds.Clear();
                var home = card.Target;
                var press = BoardPlayKit.PressOnly(card.gameObject);
                gesture = press;
                yield return press.Move(press.Data.position + new Vector2(0, -40), 2);
                Assert.IsNull(Drag.Held, id + ": kilitli planda kart kalkmaz");
                Assert.AreEqual(1, sounds.Count(BoardSound.Deny), id + ": basışta tek Deny (" + sounds + ")");
                float cardPeak = 0;
                float end = Time.unscaledTime + denyS + Tol + 0.05f;
                while (Time.unscaledTime < end)
                {
                    var p = card.transform.localPosition;
                    cardPeak = Mathf.Max(cardPeak, Mathf.Abs(p.x - home.x) * 1000f);
                    yield return null;
                    BoardPlayKit.TrackFrame();
                }
                press.Release();
                gesture = null;
                Assert.LessOrEqual(cardPeak, denyMm + 0.05f, id + ": yanal sarsıntı ≤ 2 mm");
                Assert.Greater(cardPeak, denyMm * 0.5f, id + ": sarsıntı görüldü (tepe " + cardPeak.ToString("0.00") + " mm)");
                Assert.LessOrEqual(card.LiftMm, 4.05f, id + ": kart kalkmadı");
            }
            ShiftPlayKit.AssertUnchanged(mark, "kilitli planda defter, tarif, fiş ve stok kartı");
            Assert.IsTrue(Sim.Board.Locked, "plan kilitli kalır");
        }

        // ================================================================== P5

        /// <summary>
        /// P5 (R18b–c, B): defterden Zırh plakası tarifi boş iş emrine sürüklenir (önizleme "İŞ EMRİ ← ZIRH PLAKASI", amber çerçeve) → tam
        /// bir <c>board_queue armor</c>; defter kancaya kapanır (B8); form "tarif", TARİF kutusunda kart. Metal ×2 ve kumaş ×1 stoktan iş
        /// emrine: üç mutlak <c>board_reserve</c> (b = 1, 2, 1); serbest/ayrı sayıları, fişler; metal 2'den sonra "eksik: 1 kumaş",
        /// kumaştan sonra form tam.
        /// </summary>
        [UnityTest]
        public IEnumerator Stage_MaterialToOrder_OneChipPerDrop_RecipeCardAppears()
        {
            yield return DeskAtBoard();
            var book = Book;
            yield return OpenBook("tarif");
            var mark = ShiftPlayKit.Mark();
            var armor = RecipeOf("armor");
            Assert.AreEqual(OrderState.Empty, Board.Last.Order.State, "açılışta iş emri boş");

            yield return LiftRecipe(false, "zırh plakası");
            var copy = Drag.Adopted;
            Assert.IsNotNull(copy, "tarif kopyası denetçide (RecipeLifted → AcquireRecipeCopy → Adopt; W6: BoardView3D.recipePrefab)");
            Assert.AreEqual(BoardCardKind.Recipe, copy.Kind);
            Assert.AreEqual("armor", copy.Id, "kopyanın kimliği tarif");
            yield return HoldOverOrder();
            Assert.AreEqual(BoardTargetKind.Order, Drag.HoverTarget.Kind, "iş emri üstünde hedef İŞ EMRİ");
            Assert.IsTrue(Board.Marks != null && Board.Marks.GlowShown, "iş emri amber çerçeveyle aydınlanır (BoardMarks)");
            var expected = BoardPreviewText.For(BoardCardKind.Recipe, "armor", BoardTarget.Order(), Sim, Ui);
            var shown = TagState(BoardTag);
            Assert.AreEqual(expected.Title, shown.title, "önizleme başlığı (İŞ EMRİ ← ZIRH PLAKASI)");
            Assert.AreEqual(expected.Body, shown.line, "önizleme gövdesi (girdi · üretici)");
            StringAssert.Contains(armor.Title, shown.title ?? "", "başlıkta tarifin adı");
            ShiftPlayKit.AssertUnchanged(mark, "bırakmadan önce");
            gesture.Release();
            gesture = null;
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy && book.State == BookState.Closed && !book.Moving, 2, "bırakma ve defterin kapanışı bitmedi");
            yield return BoardPlayKit.Frames(1);
            AssertCommands(ShiftPlayKit.Since(mark.commands), "tarif → boş iş emri", ("board_queue", "armor", null));
            var order = Sim.Board.Orders.Single();
            Assert.AreEqual("armor", order.RecipeId);
            Assert.AreEqual(OrderState.Recipe, Board.Last.Order.State, "form: tarif konmuş");
            Assert.AreEqual(order.Id, Board.Last.Order.OrderId);
            var form = Board.OrderRecipeCard;
            Assert.IsNotNull(form, "TARİF kutusunda tarif kartı");
            Assert.AreEqual(order.Id, form.Id, "formdaki kartın kimliği iş emri");
            Assert.AreEqual(armor.Name, (form.Face as RecipeCardFace)?.NameText.text, "kartın adı");
            StringAssert.StartsWith(Loc.F("eksik: {0}", ""), Board.Last.Order.Status, "form: girdiler eksik");

            // hammaddeler: metal ×2, kumaş ×1
            var inputs = BoardLayout.Inputs.center;
            var drops = new[] { ("metal", 1, inputs + new Vector2(-100, 0)), ("metal", 2, inputs + new Vector2(-40, 4)), ("kumas", 1, inputs + new Vector2(40, -4)) };
            int k = 0;
            foreach (var (kind, amount, mm) in drops)
            {
                int free = Board.Last.Materials[Array.IndexOf(BoardLayout.MaterialKinds, kind)].Free;
                var step = ShiftPlayKit.Mark();
                var card = Board.CardFor(kind);
                Assert.IsNotNull(card, "stok kartı " + kind);
                yield return BoardPlayKit.Drag(card.gameObject, BoardPlayKit.BoardWorld(mm), 10);
                yield return BoardPlayKit.WaitFor(() => !Drag.Busy, 2, kind + " bırakması bitmedi");
                AssertCommands(ShiftPlayKit.Since(step.commands), kind + " → iş emri", ("board_reserve", kind, amount.ToString()));
                var m = Board.Last.Materials[Array.IndexOf(BoardLayout.MaterialKinds, kind)];
                Assert.AreEqual(free - 1, m.Free, kind + ": serbest bir azalır");
                Assert.AreEqual(amount, m.Held, kind + ": ayrı = ayrılan");
                var chip = Board.CardFor(BoardUiState.ChipId(kind, amount - 1));
                Assert.IsTrue(chip != null && chip.isActiveAndEnabled, kind + ": " + amount + ". fiş iş emrinde");
                k++;
                if (k == 2)
                {
                    Assert.AreEqual(OrderState.Missing, Board.Last.Order.State, "metal 2'den sonra form eksik");
                    var lang = DeskLoc.Instance.Lang;
                    string lack = Loc.F("{0} {1}", 1, PlanningBoardSnapshotBuilder.Lowered(PlanningBoardData.MaterialName("kumas"), lang));
                    Assert.AreEqual(Loc.F("eksik: {0}", lack), Board.Last.Order.Status, "mavi not: eksik: 1 kumaş (R18c)");
                }
            }
            Assert.AreEqual(OrderState.Full, Board.Last.Order.State, "kumaştan sonra form tam (" + Board.Last.Order.Status + ")");
            string makerName = ProductionForecast.Of(Sim).Maker;
            Assert.IsFalse(string.IsNullOrEmpty(makerName), "V.001'de üretici var (Suna)");
            Assert.AreEqual(Loc.F("{0} · girdiler ayrıldı · üretici: {1}", PlanningBoardSnapshotBuilder.Lowered(armor.Name, DeskLoc.Instance.Lang), makerName),
                Board.Last.Order.Status, "form: girdiler ayrıldı · üretici (R18c)");
            Assert.IsFalse(Board.Order.Stamped, "hazır emirde damga yok");
            Assert.AreEqual(3, Board.Last.Chips.Count, "üç fiş (2 metal + 1 kumaş)");
            Assert.IsNull(Sim.BoardPlanCheck(), "tam emirde plan geçerli");
            Assert.AreEqual(4, ShiftPlayKit.Since(mark.commands).Count, "bir kuyruk + üç ayırma");
        }

        // ================================================================== P6

        /// <summary>
        /// P6 (B): fiş depoya geri sürüklenir → tam bir <c>board_reserve(kumas, 0)</c> (mutlak, ayrılan − 1); kumaş fişi kalkar, form
        /// yeniden "eksik", tarif kartı kalır. Fişi iş emri içinde taşımak komutsuzdur (yalnız yerleşim, <c>ChipPoses</c>).
        /// </summary>
        [UnityTest]
        public IEnumerator Unstage_ChipToDepot()
        {
            yield return DeskAtBoard();
            yield return ArmorOrderFull();
            Assert.AreEqual(OrderState.Full, Board.Last.Order.State);
            var depot = BoardPlayKit.Anchor("Mat_kumas");
            Assert.IsNotNull(depot, "Mat_kumas çapası");
            var mark = ShiftPlayKit.Mark();
            yield return BoardPlayKit.Drag(Board.CardFor("chip:kumas:0").gameObject, depot.position, 10);
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy, 2, "fiş bırakması bitmedi");
            AssertCommands(ShiftPlayKit.Since(mark.commands), "fiş → depo", ("board_reserve", "kumas", "0"));
            Assert.IsNull(Board.CardFor("chip:kumas:0"), "kumaş fişi kalktı");
            Assert.AreEqual(OrderState.Missing, Board.Last.Order.State, "form yeniden eksik");
            StringAssert.StartsWith(Loc.F("eksik: {0}", ""), Board.Last.Order.Status, "mavi not geri geldi");
            Assert.IsNotNull(Board.OrderRecipeCard, "tarif kartı TARİF kutusunda kalır (emir sürer)");
            Assert.AreEqual(2, Board.Last.Materials[2].Free, "kumaş serbest 2");

            var move = ShiftPlayKit.Mark();
            var target = BoardLayout.Inputs.center + new Vector2(80, 8);
            yield return BoardPlayKit.Drag(Board.CardFor("chip:metal:0").gameObject, BoardPlayKit.BoardWorld(target), 10);
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy, 2, "fişin taşınması bitmedi");
            ShiftPlayKit.AssertUnchanged(move, "fişi iş emri içinde taşımak");
            var pose = Ui.ChipPose("chip:metal:0");
            Assert.IsTrue(pose.HasValue, "taşınan fişin yeri yerleşimde (ChipPoses)");
            Assert.AreEqual(Drag.LastDropPose.x, pose.Value.x, 0.01f, "fişin yeri bırakıldığı yer (x)");
            Assert.AreEqual(Drag.LastDropPose.y, pose.Value.y, 0.01f, "fişin yeri bırakıldığı yer (y)");
        }

        // ================================================================== P7

        /// <summary>
        /// P7 (R18-not "yetersiz kaynak"; PLAN §1.9): Bakım kiti emri + 1 kimyasal ayrılır (serbest 0). Kimyasal stok kartından sürükleme
        /// denemesi: kart kalkmaz (üzerine gelme 4 mm'yi aşmaz, tutulan kart yok), 2 mm yanal sarsıntı <c>denyShakeS</c> içinde, tek
        /// <c>Deny</c>, serbest sayı ilk karede #9A1E14 ve <c>denyFlashS</c> 0,2 s sonra eski renginde; komut ve RNG aynı.
        /// </summary>
        [UnityTest]
        public IEnumerator InsufficientStock_NoLift_Shake_RedFlash()
        {
            yield return DeskAtBoard();
            var game = BoardPlayKit.Game;
            Ok(game.ExecuteBoardCommand(s => s.QueueProduction("medkit")), "board_queue medkit");
            Ok(game.ExecuteBoardCommand(s => s.ReserveProductionMaterial("kimyasal", 1)), "board_reserve kimyasal 1");
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(0, Sim.BoardMaterialFree("kimyasal"), "kimyasal serbest 0");
            Assert.AreEqual(Loc.T("Serbest hammadde yetersiz."), BoardDropRules.CanPick(BoardCardKind.Material, "kimyasal", Sim), "CanPick reddi");
            var card = Board.CardFor("kimyasal");
            var face = card.Face as MaterialCardFace;
            Assert.IsNotNull(face?.FreeText, "kimyasal kartının serbest sayısı");
            var baseColor = face.FreeText.color;
            var red = SiginakTheme.Active.ColorOf("cards.material.reservedRing");
            AssertColor(new Color32(0x9A, 0x1E, 0x14, 0xFF), red, "cards.material.reservedRing");
            float denyMm = ShiftPlayKit.Token("motion.board.denyShakeMm", 2f), denyS = ShiftPlayKit.TokenOr("motion.board.denyShakeS", 0.18f);
            float flashS = ShiftPlayKit.Token("motion.board.denyFlashS", 0.2f);
            var flashed = new List<string>();
            Action<string> onFlash = kind => flashed.Add(kind);
            Drag.StockFlashed += onFlash;
            var mark = ShiftPlayKit.Mark();
            sounds.Clear();
            var home = card.Target;

            BoardPlayKit.ResetFrameStats();
            gesture = BoardPlayKit.PressOnly(card.gameObject);
            float t0 = Time.unscaledTime;
            gesture.Step(gesture.Data.position + new Vector2(0, 40));   // sürükleme denemesi (OnCardBeginDrag → CanPick)
            Assert.IsNull(Drag.Held, "serbest 0: kart kalkmaz");
            Assert.AreEqual(1, sounds.Count(BoardSound.Deny), "tek Deny (" + sounds + ")");
            CollectionAssert.AreEqual(new[] { "kimyasal" }, flashed, "kırmızı yalnız kimyasalda (StockFlashed)");
            AssertColor(red, face.FreeText.color, "serbest sayı ilk karede kırmızı");

            float peak = 0, restoredAt = -1;
            float until = t0 + Mathf.Max(denyS, flashS) + Tol + 0.1f;
            while (Time.unscaledTime < until)
            {
                yield return null;
                BoardPlayKit.TrackFrame();
                gesture.Step(gesture.Data.position + new Vector2(0, 2));
                var p = card.transform.localPosition;
                peak = Mathf.Max(peak, Mathf.Abs(p.x - home.x) * 1000f);
                Assert.IsNull(Drag.Held, "sürükleme sürerken de kart kalkmaz");
                Assert.LessOrEqual(card.LiftMm, 4.05f, "kart üzerine gelme kalkışını aşmaz (" + card.LiftMm.ToString("0.00") + " mm)");
                if (restoredAt < 0 && face.FreeText.color != red) restoredAt = Time.unscaledTime;
            }
            gesture.Release();
            gesture = null;
            Drag.StockFlashed -= onFlash;
            Assert.LessOrEqual(peak, denyMm + 0.05f, "yanal sarsıntı ≤ 2 mm");
            Assert.Greater(peak, 1f, "yanal sarsıntı > 1 mm (tepe " + peak.ToString("0.00") + ")");
            Assert.Less(Mathf.Abs(card.transform.localPosition.x - home.x) * 1000f, 0.01f, "sarsıntıdan sonra yuvasında");
            Assert.Greater(restoredAt, 0, "kırmızı söndü");
            AssertDuration(restoredAt - t0, flashS, "kırmızı süresi (denyFlashS)");
            AssertColor(baseColor, face.FreeText.color, "serbest sayı eski renginde");
            Assert.AreEqual(1, Board.Last.Chips.Count, "iş emrine fiş eklenmedi (yalnız ayrılan kimyasal)");
            ShiftPlayKit.AssertUnchanged(mark, "yetersiz kaynakta sürükleme denemesi");
        }

        // ================================================================== P8

        /// <summary>
        /// P8 (R18d): tam zırh emri → şalter (<c>board_print</c>) → EVRAK (<c>board_file</c>, ikizle aynı) → <c>LastCompleted == [armor]</c>,
        /// metal 3 → 1, kumaş 2 → 1, devre 2, kimyasal 1; depoda yeni zırh; iş emri boş. K4'e gelmeden ürün TARİF kutusunda bekler
        /// (D9-A); ilk K4 varışında yığının üst kart yerine <c>productToStoreS</c> 0,3 s'de kayar (tek <c>ProductSlide</c>), YENİ etiketi
        /// yalnız o kartta. K4'ten çıkıp girince ikinci kayış yok; karta ilk işaretçi girişi etiketi düşürür (D9-A). Komut yazılmaz.
        /// </summary>
        [UnityTest]
        public IEnumerator ProductionAcrossShiftClose_ProductSlidesToStore_Yeni()
        {
            yield return ShiftPlayKit.Begin();
            RequireBook();
            yield return ArmorOrderFull();
            float slideS = ShiftPlayKit.Token("motion.board.productToStoreS", 0.3f);
            int armorStored = Sim.Board.Equipment.Count(e => e.Kind == "armor" && string.IsNullOrEmpty(e.OwnerId));
            int devre = Quantity("devre"), kimyasal = Quantity("kimyasal");
            Assert.AreEqual(3, Quantity("metal"), "V.001 metal 3");
            Assert.AreEqual(2, Quantity("kumas"), "V.001 kumaş 2");

            yield return ShiftPlayKit.PrintToTongue();
            var twin = ShiftPlayKit.Twin();
            string receipt = Sim.Board.ReceiptId;
            var before = ShiftPlayKit.Mark();
            yield return ShiftPlayKit.FileOnEvrak();
            var commands = ShiftPlayKit.Since(before.commands);
            Assert.AreEqual(1, commands.Count(c => c.kind == "board_file"), "tam bir board_file: " + ShiftPlayKit.Describe(commands));
            var others = commands.Where(c => c.kind != "board_file").ToList();
            if (others.Count > 0) TestContext.WriteLine("board_file dışı komutlar (RetireJobs, E §8.13): " + ShiftPlayKit.Describe(others));
            Ok(twin.FileShift(receipt), "ikiz aynı fişi dosyalar");
            Assert.AreEqual(ShiftPlayKit.Fingerprint(twin), ShiftPlayKit.Fingerprint(Sim), "devir ikizle aynı");

            CollectionAssert.AreEqual(new[] { "armor" }, Sim.Board.LastCompleted, "kapanışta zırh plakası üretildi");
            Assert.AreEqual(1, Quantity("metal"), "metal 3 → 1 (R18d)");
            Assert.AreEqual(1, Quantity("kumas"), "kumaş 2 → 1");
            Assert.AreEqual(devre, Quantity("devre"), "devre aynı");
            Assert.AreEqual(kimyasal, Quantity("kimyasal"), "kimyasal aynı");
            Assert.AreEqual(armorStored + 1, Sim.Board.Equipment.Count(e => e.Kind == "armor" && string.IsNullOrEmpty(e.OwnerId)), "depoda bir zırh fazla");
            Assert.IsEmpty(Sim.Board.Orders, "biten emir silindi (B)");
            Assert.AreEqual(0, Sim.Board.ProductionInputs.Sum(m => m.Quantity), "konmuş fiş yok");
            var made = NewestStored("armor");
            Assert.IsNotNull(made, "yeni zırh depoda");

            // K1: ürün TARİF kutusunda bekler
            yield return BoardPlayKit.Frames(2);
            var s = Board.Last;
            Assert.AreEqual(OrderState.Empty, s.Order.State, "iş emri boş");
            Assert.IsNull(Board.OrderRecipeCard, "TARİF kutusunda tarif kartı yok");
            Assert.AreEqual(0, s.Chips.Count, "fiş yok");
            Assert.IsNotNull(s.NewItem, "anlık görüntüde yeni ürün");
            Assert.AreEqual(made.Id, s.NewItem.ItemId, "yeni ürün en yeni depodaki zırh (sayısal sonek)");
            Assert.AreEqual("armor", s.NewItem.Kind);
            Assert.AreEqual(3, s.NewItem.Stack, "zırh yığını (kit_tek, kit_emp, kit_ana, armor …)");
            Assert.IsTrue(Board.NewItemWaiting, "K4'e gelmeden ürün bekler (D9-A)");
            var card = Board.NewItemCard;
            Assert.IsNotNull(card, "yeni ürünün kartı");
            Assert.IsTrue(card.isActiveAndEnabled, "bekleyen ürün görünür");
            var waitMm = BoardLayout.LocalToMm(card.transform.localPosition);
            var homeMm = Board.Order.RecipeHomeMm(card.transform.parent);
            Assert.Less(Vector2.Distance(waitMm, homeMm), 1f, "ürün TARİF kutusunda bekler (±1 mm; " + waitMm + " / " + homeMm + ")");
            int countBefore = s.Kits[3].Count;

            // ilk K4 varışı: kayış
            sounds = new BoardSoundLog(Board);
            float slidAt = -1;
            Action<NewItemData> onSlid = d => { if (slidAt < 0) slidAt = Time.unscaledTime; };
            Board.NewItemSlid += onSlid;
            var mark = ShiftPlayKit.Mark();
            BoardPlayKit.ResetFrameStats();
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.WaitFor(() => slidAt >= 0, slideS + 1, "ürün yığına kaymadı");
            Board.NewItemSlid -= onSlid;
            Assert.AreEqual(1, sounds.Count(BoardSound.ProductSlide), "tek ProductSlide (" + sounds + ")");
            AssertDuration(slidAt - sounds.At(BoardSound.ProductSlide), slideS, "kayış (productToStoreS)");
            Assert.IsTrue(Board.Store.TopSlotOf(made.Id, out var slot), "ürün yığının üstünde");
            Assert.Less(Vector3.Distance(slot, card.transform.localPosition), 1e-4f, "ürün üst kart yerinde (±0,1 mm)");
            Assert.AreEqual(countBefore, Board.Last.Kits[3].Count, "yığın sayısı aynı kuralla (×n)");
            var tag = Board.Store.NewTag;
            Assert.IsNotNull(tag, "StoreView.newTag (YENİ etiketi; W6 §2.3 #6)");
            yield return BoardPlayKit.Frames(1);
            Assert.IsTrue(tag.Shown, "YENİ etiketi asılı");
            Assert.AreSame(card.transform, tag.Target, "YENİ yalnız yeni ürünün kartında");
            Assert.AreEqual(Loc.T("YENİ"), tag.Word.text, "etiketin sözcüğü");
            Assert.IsTrue(Ui.NewItem != null && Ui.NewItem.Slid && Ui.NewItem.Id == made.Id, "kayış yerleşime yazıldı (newItem.slid)");

            // ikinci varış: kayış yok
            BoardPlayKit.Presenter.Close();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.Frames(3);
            Assert.AreEqual(1, sounds.Count(BoardSound.ProductSlide), "ikinci varışta kayış yok");
            Assert.IsFalse(Board.NewItemWaiting || Board.NewItemSliding, "ürün yığında");
            Assert.IsTrue(tag.Shown, "dokunulmadan etiket kalır");

            // ilk dokunuş: işaretçi girişi
            var ped = BoardPlayKit.Pointer(card.gameObject, BoardPlayKit.ScreenPoint(card.transform.position));
            ExecuteEvents.Execute(card.gameObject, ped, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(card.gameObject, ped, ExecuteEvents.pointerExitHandler);
            yield return BoardPlayKit.WaitFor(() => !tag.Shown && !tag.Fading, ShiftPlayKit.TokenOr("motion.board.itemTuckS", 0.15f) + 1, "YENİ etiketi düşmedi");
            Assert.IsTrue(Ui.NewItem.Touched, "dokunuş yerleşime yazıldı (newItem.touched)");
            ShiftPlayKit.AssertUnchanged(mark, "kayış, YENİ ve dokunuş");
        }

        // ================================================================== P9

        /// <summary>
        /// P9 (B): tarif + üç ayırma, fişler serbest açılarla iş emrinde → KAYITTAN DEVAM: kayıt dosyasının komutları canlıyla aynı,
        /// sürüm 14; devam yeni komut yazmaz; fişler aynı mm/açıyla (±0,01), tarif kartının yeri, stok kartlarında serbest/ayrı aynı.
        /// </summary>
        [UnityTest]
        public IEnumerator Staging_SurvivesSaveAndContinue()
        {
            yield return DeskAtBoard();
            Ok(BoardPlayKit.Game.ExecuteBoardCommand(s => s.QueueProduction("armor")), "board_queue armor");
            yield return BoardPlayKit.Frames(2);
            var inputs = BoardLayout.Inputs.center;
            foreach (var (kind, mm) in new[] { ("metal", inputs + new Vector2(-110, 6)), ("metal", inputs + new Vector2(-20, -8)), ("kumas", inputs + new Vector2(70, 10)) })
            {
                yield return BoardPlayKit.Drag(Board.CardFor(kind).gameObject, BoardPlayKit.BoardWorld(mm), 10);
                yield return BoardPlayKit.WaitFor(() => !Drag.Busy, 2, kind + " bırakması bitmedi");
            }
            Assert.AreEqual(OrderState.Full, Board.Last.Order.State, "emir tam");
            var chipPoses = Ui.ChipPoses.ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.AreEqual(3, chipPoses.Count, "üç fişin yeri yerleşimde");
            string orderId = Sim.Board.Orders.Single().Id;
            var recipePose = Ui.RecipePose(orderId);
            var materials = Board.Last.Materials.Select(m => (m.Free, m.Held)).ToArray();
            var lines = ShiftPlayKit.CommandLines(Sim.Record);
            var saved = SaveStore.Load();
            Assert.IsNotNull(saved, "kayıt dosyası yazıldı");
            Assert.AreEqual(GameRecord.CurrentVersion, saved.version, "kayıt biçimi");
            Assert.AreEqual(15, GameRecord.CurrentVersion, "kayıt biçimi 15 (Aşama 6 K4-A gözetmen kuralı)");
            CollectionAssert.AreEqual(lines, ShiftPlayKit.CommandLines(saved), "kayıt dosyasının komutları canlıyla aynı");

            sounds?.Dispose();
            sounds = null;
            yield return BoardPlayKit.ContinueDesk();
            RequireBook();
            CollectionAssert.AreEqual(lines, ShiftPlayKit.CommandLines(Sim.Record), "devamdan sonra komutlar birebir (devam komut yazmaz)");
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.Frames(2);
            BoardPlayKit.AssertNoCommand("kayıttan devam ve K4");
            Assert.AreEqual(BookState.Closed, Book.State, "devamda defter kancada kapalı");
            Assert.AreEqual(OrderState.Full, Board.Last.Order.State, "emir ve ayrılan girdiler geri geldi");
            Assert.AreEqual(orderId, Board.Last.Order.OrderId);
            Assert.IsNotNull(Board.OrderRecipeCard, "tarif kartı TARİF kutusunda");
            CollectionAssert.AreEqual(materials, Board.Last.Materials.Select(m => (m.Free, m.Held)).ToArray(), "stok kartlarında serbest/ayrı aynı");
            Assert.AreEqual(chipPoses.Count, Ui.ChipPoses.Count, "fiş yerleri geri geldi");
            foreach (var kv in chipPoses)
            {
                Assert.IsTrue(Ui.ChipPoses.TryGetValue(kv.Key, out var p), "fiş yeri " + kv.Key);
                Assert.AreEqual(kv.Value.x, p.x, 0.01f, kv.Key + " x");
                Assert.AreEqual(kv.Value.y, p.y, 0.01f, kv.Key + " y");
                Assert.AreEqual(kv.Value.z, p.z, 0.01f, kv.Key + " açı");
            }
            foreach (var chip in Board.Last.Chips)
            {
                var c = Board.CardFor(chip.Id);
                var pose = Ui.ChipPose(chip.Id);
                if (c == null || !pose.HasValue) continue;
                var mm = BoardLayout.LocalToMm(c.Target);
                Assert.Less(Vector2.Distance(mm, new Vector2(pose.Value.x, pose.Value.y)), 0.05f, chip.Id + ": fiş kayıtlı yerinde");
            }
            var after = Ui.RecipePose(orderId);
            Assert.AreEqual(recipePose.HasValue, after.HasValue, "tarif kartının yeri yerleşimde");
            if (recipePose.HasValue) Assert.Less(Vector3.Distance(recipePose.Value, after.Value), 0.01f, "tarif kartının yeri aynı");
        }

        // ================================================================== P10

        /// <summary>
        /// P10 (D9-A, layout v3): üretimli kapanıştan sonra K4'e girmeden KAYITTAN DEVAM: <c>LastCompleted</c> kayıtla geri gelir, ürün
        /// TARİF kutusunda bekler, ilk K4 varışında bir kez kayar. YENİ'ye dokunmadan yeniden devam: kayış oynamaz, etiket asılı kalır.
        /// </summary>
        [UnityTest]
        public IEnumerator Product_PendingAfterContinue()
        {
            yield return ShiftPlayKit.Begin();
            RequireBook();
            yield return ArmorOrderFull();
            yield return ShiftPlayKit.PrintToTongue();
            yield return ShiftPlayKit.FileOnEvrak();
            var made = NewestStored("armor");
            Assert.IsNotNull(made, "yeni zırh");
            yield return BoardPlayKit.Frames(2);
            Assert.IsTrue(Board.NewItemWaiting, "devamdan önce ürün bekliyor");

            yield return ShiftPlayKit.Continue();
            RequireBook();
            CollectionAssert.AreEqual(new[] { "armor" }, Sim.Board.LastCompleted, "LastCompleted kayıtla geri gelir");
            Assert.IsNotNull(Board.Last.NewItem, "devamda yeni ürün");
            Assert.AreEqual(made.Id, Board.Last.NewItem.ItemId);
            Assert.IsTrue(Board.NewItemWaiting, "kayış oynamadı: devamda da TARİF kutusunda bekler");
            sounds = new BoardSoundLog(Board);
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.WaitFor(() => !Board.NewItemSliding && !Board.NewItemWaiting, 2, "kayış bitmedi");
            Assert.AreEqual(1, sounds.Count(BoardSound.ProductSlide), "ilk K4 varışında tek kayış");
            Assert.IsTrue(Ui.NewItem != null && Ui.NewItem.Slid, "kayış yerleşime yazıldı");
            Assert.IsTrue(Board.Store.NewTag.Shown, "YENİ asılı");

            sounds.Dispose();
            sounds = null;
            yield return ShiftPlayKit.Continue();
            RequireBook();
            Assert.IsFalse(Board.NewItemWaiting, "oynamış kayış devamda beklemez");
            sounds = new BoardSoundLog(Board);
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.Frames(3);
            Assert.AreEqual(0, sounds.Count(BoardSound.ProductSlide), "oynamış kayış yeniden oynamaz");
            Assert.IsTrue(Board.Store.NewTag.Shown, "dokunulmayan YENİ devamda da asılı");
            Assert.AreSame(Board.NewItemCard.transform, Board.Store.NewTag.Target, "etiket yeni ürünün kartında");
            BoardPlayKit.AssertNoCommand("devam ve kayış");
        }

        // ================================================================== P11

        /// <summary>
        /// P11 (R18c DURDU): tam zırh emrinde üretici (Suna, ÜRETİM) BOŞTA'ya sürüklenir → tam bir <c>board_assign(Suna, rest)</c>; form
        /// DURDU damgası (sözcük + #9A1E14) ve neden "üretici yok"; şalter kilitlenmez (yalnız DURDU). Kapanışta hiçbir şey harcanmaz,
        /// emir ve fişler kalır.
        /// </summary>
        [UnityTest]
        public IEnumerator Durdu_NoProducer_Real()
        {
            yield return ShiftPlayKit.Begin();
            RequireBook();
            yield return ArmorOrderFull();
            var maker = Sim.Board.People.FirstOrDefault(p => p.Alive && p.Job == "craft");
            Assert.IsNotNull(maker, "V.001'de ÜRETİM cebinde biri var");
            yield return BoardPlayKit.EnterBoard();
            var mark = ShiftPlayKit.Mark();
            var card = Board.CardFor(maker.Id);
            Assert.IsNotNull(card, "üreticinin kartı");
            var to = BoardLayout.Bosta.center + new Vector2(-240, 50);
            Assert.IsTrue(BoardLayout.Bosta.Contains(to), "bırakış noktası BOŞTA alanında");
            yield return BoardPlayKit.Drag(card.gameObject, BoardPlayKit.BoardWorld(to), 12);
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy, 2, "kişi bırakması bitmedi");
            AssertCommands(ShiftPlayKit.Since(mark.commands), "üretici → BOŞTA", ("board_assign", maker.Id, "rest"));

            var o = Board.Last.Order;
            Assert.AreEqual(OrderState.Stopped, o.State, "form DURDU");
            Assert.AreEqual(Loc.T("DURDU"), o.Stamp, "damga sözcüğü");
            Assert.AreEqual(Loc.T("üretici yok"), o.StopReason, "durma nedeni");
            StringAssert.Contains(o.StopReason, o.Status, "neden durum satırında (renkle değil sözcükle)");
            Assert.IsTrue(Board.Order.Stamped, "formda damga");
            Assert.AreEqual(Loc.T("DURDU"), Board.Order.StampText.text);
            AssertColor(SiginakTheme.Active.ColorOf("stamp.stopped"), Board.Order.StampText.color, "damga rengi (colors.stamp.stopped)");
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(ShiftPhase.Ready, ShiftPlayKit.Switch.Phase, "üreticisiz emir şalteri kilitlemez (" + ShiftPlayKit.Switch.LockPlate + ")");

            BoardPlayKit.Presenter.Close();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            yield return ShiftPlayKit.PrintToTongue();
            yield return ShiftPlayKit.FileOnEvrak();
            Assert.IsEmpty(Sim.Board.LastCompleted, "üretici yokken üretim yok");
            Assert.AreEqual(3, Quantity("metal"), "metal harcanmadı");
            Assert.AreEqual(2, Quantity("kumas"), "kumaş harcanmadı");
            var order = Sim.Board.Orders.Single();
            Assert.IsFalse(order.Started, "emir başlamadı");
            Assert.AreEqual(3, order.Reserves.Sum(r => r.Quantity), "ayrılan girdiler kalır");
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(3, Board.Last.Chips.Count, "fişler kalır");
            Assert.AreEqual(OrderState.Stopped, Board.Last.Order.State, "form hâlâ DURDU");
        }

        // ================================================================== P12

        /// <summary>
        /// P12 (R18b, B): tarif kutusuna basılı tutup sürükleme: kopya <c>recipeLiftS</c> 0,1 s'de kalkar (tek <c>RecipeLift</c>, saat
        /// yönünde 6° eğim), defter <c>bookDropS</c> 0,25 s'de alçak poza (B7). İş emri dışına bırakış komutsuzdur: kopya
        /// <c>returnArcS</c> 0,25 s'de sayfaya döner ve gizlenir, defter okuma pozuna (B9). İkinci deneme iş emrine bırakılır: tam bir
        /// <c>board_queue</c>, kart bırakıldığı serbest pozda kalır (yerleşim), defter kancaya kapanır (B8), mıknatıs görünür.
        /// </summary>
        [UnityTest]
        public IEnumerator RecipeDrag_FromPage_BookSlidesDown_DropOnOrder()
        {
            yield return DeskAtBoard();
            var book = Book;
            float liftS = ShiftPlayKit.Token("motion.board.recipeLiftS", 0.1f);
            float dropS = Plan(RecipeBookView.DropS, 0.25f, "motion.board.bookDropS");
            float returnS = ShiftPlayKit.Token("motion.board.returnArcS", 0.25f);
            float forward = RecipeBookView.ForwardS, closeS = RecipeBookView.CloseS;
            float heldMm = ShiftPlayKit.Token("cards.heldLiftMm", 10f);
            float tilt = BoardDragController.HeldTiltFor(BoardCardKind.Recipe, false);
            Assert.AreEqual(-ShiftPlayKit.TokenOr("cards.recipeHeldTiltDeg", 6f), tilt, 1e-4f, "elde eğim: tasarım +6 (saat yönü) → Unity −6");
            yield return OpenBook("tarif");
            var mark = ShiftPlayKit.Mark();

            // 1. deneme: iş emri dışına
            sounds.Clear();
            BoardPlayKit.ResetFrameStats();
            float t0 = Time.unscaledTime;
            yield return LiftRecipe(false, "kalkış");
            var copy = Drag.Adopted;
            Assert.IsNotNull(copy, "kopya denetçide (W6: recipePrefab)");
            Assert.AreEqual(BookState.Lowered, book.State, "defter alçalır (B7)");
            Assert.AreEqual(1, sounds.Count(BoardSound.RecipeLift), "tek RecipeLift (" + sounds + ")");
            Assert.IsTrue(Drag.Suspended == false, "alçak defterde denetçi kopyayı sürer");
            float liftedAt = -1, loweredAt = -1;
            var low = RecipeBookView.LowPoseLocal;
            float end = t0 + Mathf.Max(liftS, dropS) + Tol + 0.1f;
            while (Time.unscaledTime < end)
            {
                if (liftedAt < 0 && copy.LiftMm >= heldMm - 0.01f) liftedAt = Time.unscaledTime;
                if (loweredAt < 0 && !book.Moving && Vector3.Distance(FramePos(book), low.position) < 5e-4f) loweredAt = Time.unscaledTime;
                yield return gesture.Hold(1);
            }
            Assert.Greater(liftedAt, 0, "kopya " + heldMm + " mm kalktı");
            AssertDuration(liftedAt - t0, liftS, "kopyanın kalkışı (recipeLiftS)");
            Assert.Greater(loweredAt, 0, "defter alçak pozda");
            AssertDuration(loweredAt - t0, dropS, "defterin alçalması (bookDropS)");
            // Eğim, defter alçak poza oturduktan sonra (tam süit koşusunda kare uzun olabilir; Aşama 5 kabulünde 0,16° sapma görüldü)
            yield return BoardPlayKit.WaitFor(() => !book.Moving, 1, "defter alçak pozda durmadı");
            Assert.Less(Quaternion.Angle(FrameRot(book), low.rotation), 0.05f, "alçak pozun eğimi (x −14°)");
            Assert.AreEqual(tilt, Drag.HeldTiltDeg, 1e-4f, "kopyanın eğimi");
            Assert.AreEqual(tilt, copy.TiltDeg, 0.01f, "kopya eğik tutulur");
            Assert.IsFalse(copy.MagnetVisible, "elde mıknatıs gizli");

            yield return gesture.Move(BoardPlayKit.BoardScreen(BoardLayout.Bosta.center), 8);
            yield return gesture.Hold(2);
            Assert.AreEqual(BoardTargetKind.Book, Drag.HoverTarget.Kind, "iş emri dışında hedef defter");
            Assert.IsFalse(Board.Marks.GlowShown, "iş emri dışında çerçeve yok");
            BoardPlayKit.ResetFrameStats();
            float r0 = Time.unscaledTime;
            gesture.Release();
            gesture = null;
            float hiddenAt = -1, openAt = -1;
            yield return BoardPlayKit.WaitFor(() =>
            {
                if (hiddenAt < 0 && !copy.gameObject.activeSelf) hiddenAt = Time.unscaledTime;
                if (openAt < 0 && book.State == BookState.Open && !book.Moving) openAt = Time.unscaledTime;
                return hiddenAt > 0 && openAt > 0 && !Drag.Busy;
            }, 2, "kopya sayfaya dönmedi ya da defter öne gelmedi");
            AssertDuration(hiddenAt - r0, returnS, "kopya sayfaya döner ve gizlenir (returnArcS)");
            Assert.LessOrEqual(openAt - r0, forward + Tol, "defter okuma pozuna (bookForwardS)");
            Assert.Less(Vector3.Distance(FramePos(book), RecipeBookView.ReadPoseLocal.position), 1e-4f, "defter okuma pozunda (B9)");
            ShiftPlayKit.AssertUnchanged(mark, "iş emri dışına bırakış");
            Assert.AreEqual(OrderState.Empty, Board.Last.Order.State, "iş emri boş kalır");

            // 2. deneme: iş emrine
            sounds.Clear();
            yield return LiftRecipe(false, "ikinci kalkış");
            copy = Drag.Adopted;
            Assert.IsNotNull(copy, "ikinci kopya");
            yield return HoldOverOrder(6);
            Assert.AreEqual(BoardTargetKind.Order, Drag.HoverTarget.Kind, "iş emri üstünde");
            Assert.IsTrue(Board.Marks.GlowShown, "iş emri çerçevesi (amber)");
            Assert.IsNotNull(TagState(BoardTag).frame, "önizleme çerçeveli");
            gesture.Release();
            gesture = null;
            var dropped = Drag.LastDropPose;
            float c0 = Time.unscaledTime;
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy, 2, "bırakma bitmedi");
            AssertCommands(ShiftPlayKit.Since(mark.commands), "tarif → boş iş emri", ("board_queue", "armor", null));
            Assert.AreEqual(1, sounds.Count(BoardSound.Settle), "oturma \"tık\" (" + sounds + ")");
            yield return BoardPlayKit.WaitFor(() => book.State == BookState.Closed && !book.Moving, closeS + 1, "defter kancaya kapanmadı (B8)");
            Assert.LessOrEqual(Time.unscaledTime - c0, closeS + Tol + 0.1f, "defter bookCloseS içinde kapanır");
            yield return BoardPlayKit.Frames(2);
            string orderId = Sim.Board.Orders.Single().Id;
            var form = Board.OrderRecipeCard;
            Assert.AreSame(copy, form, "kabul edilen kopya formun kartı olur (sıçramaz)");
            Assert.IsTrue(form.MagnetVisible, "panoda mıknatıs görünür");
            var pose = Ui.RecipePose(orderId);
            Assert.IsTrue(pose.HasValue, "tarif kartının serbest pozu yerleşimde (recipe:" + orderId + ")");
            Assert.AreEqual(dropped.x, pose.Value.x, 0.05f, "kart bırakıldığı yerde (x)");
            Assert.AreEqual(dropped.y, pose.Value.y, 0.05f, "kart bırakıldığı yerde (y)");
            var mm = BoardLayout.LocalToMm(form.Target);
            Assert.Less(Vector2.Distance(mm, new Vector2(pose.Value.x, pose.Value.y)), 1f, "kart kayıtlı pozunda (±1 mm)");
            Assert.IsTrue(BoardLayout.RecipeOnOrder.Contains(mm), "kart iş emrinin içinde");
        }

        // ================================================================== P13

        /// <summary>
        /// P13 (R18b "eskisini değiştirir", D3-A): dolu (başlamamış) zırh emrine Bakım kiti tarifi: önizleme amber, gövdede "başlamamış
        /// iş · ücretsiz iptal"; bırakış tek <c>ExecuteBoardCommand</c>'da iki kayıt (<c>board_cancel is-1</c> + <c>board_queue medkit</c>,
        /// aynı gün); eski emrin tarif pozu düşer, serbest kalan fişler kalkar, yeni emrin kartı formda.
        /// </summary>
        [UnityTest]
        public IEnumerator RecipeDrop_OnFilledOrder_Replaces()
        {
            yield return DeskAtBoard();
            yield return ArmorOrderFull();
            string oldId = Sim.Board.Orders.Single().Id;
            Assert.AreEqual(3, Board.Last.Chips.Count, "zırhın üç fişi");
            yield return OpenBook("değiştirme");
            var mark = ShiftPlayKit.Mark();
            yield return LiftRecipe(true, "bakım kiti");
            Assert.AreEqual("medkit", Drag.Adopted?.Id, "sağ sayfanın kopyası Bakım kiti");
            yield return HoldOverOrder(6);
            var expected = BoardPreviewText.For(BoardCardKind.Recipe, "medkit", BoardTarget.Order(), Sim, Ui);
            Assert.IsTrue(expected.Warning, "değiştirme önizlemesi uyarı (amber)");
            var shown = TagState(BoardTag);
            Assert.AreEqual(expected.Title, shown.title, "önizleme başlığı");
            Assert.AreEqual(expected.Body, shown.line, "önizleme gövdesi");
            StringAssert.Contains(Loc.T("başlamamış iş · ücretsiz iptal"), shown.line ?? "", "başlamamış emrin değiştirilmesi ücretsiz");
            Assert.IsNotNull(shown.frame, "önizleme çerçeveli (amber)");
            ShiftPlayKit.AssertUnchanged(mark, "bırakmadan önce");
            gesture.Release();
            gesture = null;
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy, 2, "bırakma bitmedi");
            yield return BoardPlayKit.Frames(2);
            AssertCommands(ShiftPlayKit.Since(mark.commands), "dolu emre yeni tarif", ("board_cancel", oldId, null), ("board_queue", "medkit", null));
            var order = Sim.Board.Orders.Single();
            Assert.AreEqual("medkit", order.RecipeId, "yeni emir Bakım kiti");
            Assert.AreNotEqual(oldId, order.Id, "yeni emir kimliği");
            Assert.IsFalse(Ui.RecipePose(oldId).HasValue, "eski emrin tarif pozu düştü");
            Assert.IsTrue(Ui.RecipePose(order.Id).HasValue, "yeni emrin tarif pozu bırakılan yer");
            Assert.AreEqual(0, Board.Last.Chips.Count, "eski emrin fişleri serbest kaldı");
            Assert.AreEqual(3, Board.Last.Materials[0].Free, "metal serbest 3");
            Assert.AreEqual(order.Id, Board.OrderRecipeCard?.Id, "formdaki kart yeni emrin");
            Assert.AreEqual(OrderState.Recipe, Board.Last.Order.State, "yeni emir girdisiz");
        }

        // ================================================================== P14

        /// <summary>
        /// P14 (R18d "iki vardiya EMP hücresi", D7-A): V.001'de Suna'nın üretim kademesi hız eşiğinin altında (iki kapanış). EMP hücresi
        /// + metal 1 + devre 1 → 1. kapanış: metal ve devre birer düşer, emir başladı (Done 1/2), kartta iki kutudan biri dolu, fiş yok,
        /// ürün yok; başlamış emre başka tarif önizlemesi "harcanan girdi geri gelmez" (Esc ile geri alınır, komut yok). 2. kapanış:
        /// EMP hücresi depoda (YENİ ile), emir silindi.
        /// </summary>
        [UnityTest]
        public IEnumerator EmpCell_TwoShifts_ProgressBoxes()
        {
            yield return ShiftPlayKit.Begin();
            RequireBook();
            var game = BoardPlayKit.Game;
            var maker = Sim.Board.People.FirstOrDefault(p => p.Alive && p.Job == "craft");
            Assert.IsNotNull(maker, "üretici");
            Assert.Less(Sim.BoardSkillTier(maker, "craft"), Sim.B.boardCraftSpeedTier, "V.001'de üretim kademesi hız eşiğinin altında (SPB:82, :102–106): iki kapanış");
            Ok(game.ExecuteBoardCommand(s => s.QueueProduction("emp_cell")), "board_queue emp_cell");
            Ok(game.ExecuteBoardCommand(s => s.ReserveProductionMaterial("metal", s.B.boardEmpMetal)), "board_reserve metal");
            Ok(game.ExecuteBoardCommand(s => s.ReserveProductionMaterial("devre", s.B.boardEmpCircuits)), "board_reserve devre");
            yield return BoardPlayKit.Frames(2);
            int metal = Quantity("metal"), devre = Quantity("devre");

            yield return ShiftPlayKit.PrintToTongue();
            yield return ShiftPlayKit.FileOnEvrak();
            var order = Sim.Board.Orders.Single();
            Assert.IsTrue(order.Started, "1. kapanışta emir başladı");
            Assert.AreEqual(1, order.Remaining, "bir vardiya kaldı");
            Assert.AreEqual(metal - Sim.B.boardEmpMetal, Quantity("metal"), "metal ödendi");
            Assert.AreEqual(devre - Sim.B.boardEmpCircuits, Quantity("devre"), "devre ödendi");
            Assert.IsEmpty(Sim.Board.LastCompleted, "1. kapanışta ürün yok");

            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.Frames(2);
            var o = Board.Last.Order;
            Assert.AreEqual(OrderState.Started, o.State, "form: başladı (" + o.Status + ")");
            Assert.AreEqual(2, o.Turns);
            Assert.AreEqual(1, o.Done, "Done = Turns − Remaining");
            Assert.AreEqual(1, o.Card.Done, "kartın ilerlemesi");
            var face = Board.OrderRecipeCard?.Face as RecipeCardFace;
            Assert.IsNotNull(face, "TARİF kutusunda EMP hücresi kartı");
            Assert.AreEqual(2, face.VisibleBoxes, "iki vardiyalık tarifte iki kutu (D7-A)");
            Assert.AreEqual(1, face.FilledBoxes, "biri dolu");
            Assert.AreEqual(0, Board.Last.Chips.Count, "başlamış emirde fiş çizilmez (girdiler ödendi)");

            // başlamış emre başka tarif: önizleme, Esc ile geri
            sounds = new BoardSoundLog(Board);
            yield return OpenBook("başlamış emir");
            var mark = ShiftPlayKit.Mark();
            yield return LiftRecipe(false, "zırh (başlamış emre)");
            var copy = Drag.Adopted;
            Assert.IsNotNull(copy, "kopya");
            yield return HoldOverOrder(6);
            var shown = TagState(BoardTag);
            StringAssert.Contains(Loc.T("harcanan girdi geri gelmez"), shown.line ?? "", "başlamış emri değiştirmek girdileri yakar (önizleme)");
            Assert.IsTrue(BoardPlayKit.Presenter.HandleBack(), "Esc zinciri tutulan kopyayı geri alır");
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy && !copy.gameObject.activeSelf && Book.State == BookState.Open && !Book.Moving, 2, "kopya sayfaya dönmedi");
            gesture.Release();
            gesture = null;
            ShiftPlayKit.AssertUnchanged(mark, "önizleme ve geri alma");
            Assert.IsTrue(Sim.Board.Orders.Single().Started, "başlamış emir yerinde");

            BoardPlayKit.Presenter.Close();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            Assert.AreEqual(BookState.Closed, Book.State, "K4'ten çıkınca defter kapanır (B6)");
            yield return ShiftPlayKit.PrintToTongue();
            yield return ShiftPlayKit.FileOnEvrak();
            CollectionAssert.AreEqual(new[] { "emp_cell" }, Sim.Board.LastCompleted, "2. kapanışta EMP hücresi");
            Assert.IsEmpty(Sim.Board.Orders, "biten emir silindi");
            var made = NewestStored("emp_cell");
            Assert.IsNotNull(made, "EMP hücresi depoda");
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.WaitFor(() => !Board.NewItemSliding && !Board.NewItemWaiting, 2, "kayış bitmedi");
            Assert.AreEqual(made.Id, Board.Last.NewItem?.ItemId, "yeni ürün EMP hücresi");
            yield return BoardPlayKit.Frames(1);
            Assert.AreSame(Board.NewItemCard.transform, Board.Store.NewTag.Target, "YENİ EMP hücresinin kartında");
        }

        // ================================================================== P15

        /// <summary>
        /// P15 (D10, PLAN §1.12 defter kipi): defter açıkken kişi kartı, konu satırı ve dosya kapağı tıklanmaz: gerçek ışın onların
        /// çarpıştırıcısına değil pano yüzüne düşer ve tık defteri kapatır; doğrudan karta verilen tık da (denetçi askıda) yalnız defteri
        /// kapatır. Seçim, eğitim konusu ve dosya değişmez; komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator Book_BlocksBoardClicks_ClickOutsideCloses()
        {
            yield return DeskAtBoard();
            var book = Book;
            var s = Board.Last;
            string personId = null;
            foreach (var job in new[] { "tek", "emp", "ana", "craft", "cook", "train" })
                if (s.PocketOccupant.TryGetValue(job, out var id) && !string.IsNullOrEmpty(id)) { personId = id; break; }
            Assert.IsNotNull(personId, "cepte bir kişi");
            var person = Board.CardFor(personId);
            var rows = ShiftPlayKit.Get<BoxCollider[]>(Board.Topics, "rowHits");
            var folderHit = Board.Folder != null ? Board.Folder.Hit : null;
            var targets = new List<(string name, Collider col, Vector3 world)>
            {
                ("kişi kartı", person.Collider, person.transform.position),
            };
            if (rows != null && rows.Length > 1 && rows[1] != null) targets.Add(("konu satırı", rows[1], rows[1].transform.TransformPoint(rows[1].center)));
            if (folderHit != null) targets.Add(("dosya kapağı", folderHit, folderHit is BoxCollider fb ? fb.transform.TransformPoint(fb.center) : folderHit.transform.position));
            string selected = Ui.SelectedId, topic = Ui.TrainingTopic;
            var mark = ShiftPlayKit.Mark();
            int clicked = 0;

            foreach (var (name, col, world) in targets)
            {
                yield return OpenBook(name);
                Assert.IsTrue(Board.BookMode, name + ": defter kipi");
                Assert.IsFalse(col.enabled, name + ": defter kipinde çarpıştırıcısı kapalı");
                var screen = BoardPlayKit.ScreenOfWorld(world);
                var hit = BoardPlayKit.FirstHit(screen);
                if (IsBookCollider(hit.gameObject))
                {
                    TestContext.WriteLine(name + ": ekranda açık defterin arkasında kalıyor; gerçek ışın deftere düşer, atlandı");
                    book.Close();
                    yield return WaitClosed(name);
                    continue;
                }
                Assert.AreNotSame(col.gameObject, hit.gameObject, name + ": ışın kapalı çarpıştırıcıya düşmez");
                Assert.IsTrue(BoardPlayKit.ClickHit(hit), name + ": ışın pano yüzüne düşer (" + BoardPlayKit.DescribeHits(screen) + ")");
                Assert.AreEqual(BookState.Closing, book.State, name + ": tık defteri kapatır (tüketilir)");
                yield return WaitClosed(name);
                clicked++;
                Assert.AreEqual(selected, Ui.SelectedId, name + ": seçim değişmez");
                Assert.AreEqual(topic, Ui.TrainingTopic, name + ": eğitim konusu değişmez");
                Assert.IsFalse(Board.Folder != null && Board.Folder.IsOpen, name + ": dosya açılmaz");
            }
            Assert.Greater(clicked, 0, "en az bir hedef defterin dışında kaldı");

            // denetçiye doğrudan tık: askıda yalnız defteri kapatır
            yield return OpenBook("doğrudan kart tıkı");
            BoardPlayKit.Click(person.gameObject);
            Assert.AreEqual(BookState.Closing, book.State, "askıdaki denetçi kart tıkını BackgroundClicked'e çevirir");
            yield return WaitClosed("doğrudan kart tıkı");
            Assert.AreEqual(selected, Ui.SelectedId, "kart seçilmedi");
            ShiftPlayKit.AssertUnchanged(mark, "defter kipinde pano tıkları");
        }

        // ================================================================== P16

        /// <summary>
        /// P16 (B; Aşama 4 D1-A): Zırh plakası + yalnız metal 2 → planın Core denetimi eksik girdiyi söyler, şalter "plan düzeltilmeli"
        /// ile kilitlenir; kumaş eklenince açılır.
        /// </summary>
        [UnityTest]
        public IEnumerator IncompleteOrder_LocksShiftSwitch()
        {
            yield return ShiftPlayKit.Begin();
            var game = BoardPlayKit.Game;
            var sw = ShiftPlayKit.Switch;
            Ok(game.ExecuteBoardCommand(s => s.QueueProduction("armor")), "board_queue armor");
            Ok(game.ExecuteBoardCommand(s => s.ReserveProductionMaterial("metal", s.B.boardArmorMetal)), "board_reserve metal");
            yield return BoardPlayKit.Frames(2);
            string issue = Sim.BoardPlanCheck();
            Assert.IsNotNull(issue, "eksik girdili emir planı geçersiz kılar (BoardPlanCheck)");
            Assert.AreEqual(ShiftPhase.Locked, sw.Phase, "şalter kilitli");
            Assert.AreEqual(Loc.T("plan düzeltilmeli"), sw.LockPlate, "plaka: plan düzeltilmeli");
            Assert.AreEqual(issue, sw.LockLine, "üzerine gelme satırı Core'un iletisi");
            Assert.AreEqual(OrderState.Missing, Board.Last.Order.State, "form eksik (mavi not)");
            Ok(game.ExecuteBoardCommand(s => s.ReserveProductionMaterial("kumas", s.B.boardArmorCloth)), "board_reserve kumas");
            yield return BoardPlayKit.Frames(2);
            Assert.IsNull(Sim.BoardPlanCheck(), "tam emirde plan geçerli");
            yield return BoardPlayKit.WaitFor(() => sw.Phase == ShiftPhase.Ready && !sw.Moving, 2, "şalter açılmadı (" + sw.Phase + " / " + sw.LockPlate + ")");
        }

        // ================================================================== P17

        /// <summary>
        /// P17 (B, D2-A): A döneminden kalmış gibi konmuş fişli kayıt (sunucu kapalıyken doğrudan <c>board_stage(metal, 1)</c> ve
        /// <c>board_stage(kumas, 1)</c>, dosyaya yazılır). KAYITTAN DEVAM: yeniden oynatma konmuş fişleri kurar, sunucu her tür için bir
        /// kez <c>board_stage(tür, 0)</c> yazar (fişler stoka döner), sonra sessizdir.
        /// </summary>
        [UnityTest]
        public IEnumerator LegacyStagedChips_ReleasedOnce()
        {
            yield return BoardPlayKit.LoadDesk();
            // Sunucu kapalı: LateUpdate'i (ReleaseLegacyStaging) konmuş fişleri canlı oyunda hemen bırakmasın
            BoardPlayKit.Presenter.enabled = false;
            var sim = Sim;
            Ok(sim.StageProductionMaterial("metal", 1), "board_stage metal 1 (A dönemi)");
            Ok(sim.StageProductionMaterial("kumas", 1), "board_stage kumas 1 (A dönemi)");
            BoardPlayKit.Game.Save();
            int saved = sim.Record.commands.Count;
            var file = SaveStore.Load();
            Assert.IsNotNull(file, "kayıt yazıldı");
            Assert.AreEqual(saved, file.commands.Count, "konmuş fişler dosyada");
            Assert.AreEqual("board_stage", file.commands[saved - 1].kind);

            yield return BoardPlayKit.ContinueDesk();
            RequireBook();
            var released = BoardPlayKit.Sim.Record.commands.Skip(saved).ToList();
            AssertCommands(released, "devamda eski fişlerin bırakılması", ("board_stage", "metal", "0"), ("board_stage", "kumas", "0"));
            Assert.AreEqual(0, BoardPlayKit.Sim.Board.ProductionInputs.Sum(m => m.Quantity), "konmuş fiş kalmadı");
            Assert.AreEqual(3, BoardPlayKit.Sim.BoardMaterialFree("metal"), "metal stoka döndü");
            yield return BoardPlayKit.WaitSeconds(0.3f);
            BoardPlayKit.AssertNoCommand("bırakmadan sonra sessiz");
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(0, Board.Last.Chips.Count, "eski fiş çizilmez");
            BoardPlayKit.AssertNoCommand("K4'te de sessiz");
        }
    }
}
