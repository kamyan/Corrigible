using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using Corrigible.Game.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 5 (R18) üretim akışı, sunucu tarafı (PLAN §3 W5, D1 = B): pano seslerinin ses olaylarına eşlenmesi, defter
    /// kipi (Esc zinciri, K4'ten çıkış, pano tıkı; kilitli planda da), bırakmaların Core komutlarına çevrilmesi ve sunum durumunun
    /// komuttan önce yazılıp retde geri alınması (fiş ve tarif kartı yerleri), değiştirmenin iki kaydı, eski konmuş fişlerin bir kez
    /// serbest bırakılması (D2) ve geçici uGUI defterine bağın kalmadığı. Sahneye bağlı değildir; kayıt ve yerleşim dosyaları geçici
    /// klasöre yazılır. Defterin durumları <see cref="RecipeBookView"/>'in (W1) sözleşmesine göre okunur.
    /// </summary>
    public sealed class Rev3ProductionFlowTests
    {
        const string StorageKey = "Corrigible.TestStorage";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        string originalStorage, directory;
        readonly List<Object> created = new List<Object>();
        readonly List<PlanningBoardPresenter> enabled = new List<PlanningBoardPresenter>();

        [SetUp]
        public void SetUp()
        {
            Loc.SetLanguage(Loc.Turkish);
            originalStorage = SessionState.GetString(StorageKey, "");
            directory = Path.Combine(Path.GetTempPath(), "Corrigible-Production-" + Guid.NewGuid().ToString("N"));
            SessionState.SetString(StorageKey, directory);
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var p in enabled) if (p != null) Call(p, "OnDisable");
            enabled.Clear();
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
            Loc.SetLanguage(Loc.Turkish);
            if (string.IsNullOrEmpty(originalStorage)) SessionState.EraseString(StorageKey);
            else SessionState.SetString(StorageKey, originalStorage);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        // ---------------------------------------------------------------- kurulum

        T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            created.Add(go);
            return go.AddComponent<T>();
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            Assert.IsNotNull(p, target.GetType().Name + "." + field);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static object Call(object target, string method, params object[] args)
        {
            var m = target.GetType().GetMethod(method, Private | BindingFlags.Public);
            Assert.IsNotNull(m, target.GetType().Name + "." + method);
            return m.Invoke(target, args);
        }

        /// <summary>Alan biçimli bir olayı (derleyicinin ürettiği gizli alan) dışarıdan yayar: bağın gerçekten kurulduğunu sınar.</summary>
        static void Raise(object source, string eventName)
        {
            var field = source.GetType().GetField(eventName, Private);
            Assert.IsNotNull(field, source.GetType().Name + "." + eventName);
            var handler = field.GetValue(source) as Action;
            Assert.IsNotNull(handler, eventName + " olayına kimse bağlanmamış");
            handler();
        }

        /// <summary>K1 (masa) ve K4 (pano) pozlu kamera düzeni; masada başlar.</summary>
        SiginakCameraRig Rig()
        {
            var root = new GameObject("Rig root");
            created.Add(root);
            Transform Anchor(string name, Vector3 pos, Quaternion rot)
            {
                var t = new GameObject(name).transform;
                t.SetParent(root.transform);
                t.SetPositionAndRotation(pos, rot);
                return t;
            }
            var camGo = new GameObject("Camera", typeof(Camera));
            camGo.transform.SetParent(root.transform);
            var rig = camGo.AddComponent<SiginakCameraRig>();
            rig.Configure(camGo.GetComponent<Camera>(), null, new[]
            {
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Desk, anchor = Anchor("K1", new Vector3(0, 1.52f, -0.15f), Quaternion.identity), fieldOfView = 56.7f, lensShiftY = -0.324f },
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Shelf, anchor = Anchor("K4", new Vector3(-1.5f, 1.15f, 0.8f), Quaternion.Euler(0, -35, 0)), fieldOfView = 48 },
            }, new Vector2(36, 20.25f), 0.45f, 0.15f);
            rig.Snap(SiginakPose.Desk);
            return rig;
        }

        /// <summary>Gerçek kartlı oyun (tohum 79, V.001: metal 3 · devre 2 · kumaş 2 · kimyasal 1).</summary>
        (GameController game, LiberationDeskView surface) Game()
        {
            var game = Make<GameController>("Game");
            var surface = Make<LiberationDeskView>("Surface");
            Set(surface, "game", game);
            game.NewGame(79);
            Assert.IsNotNull(game.Sim?.Board, "kartlı oyun");
            Assert.IsFalse(game.StoryHold);
            return (game, surface);
        }

        /// <summary>Yalın pano: görünüm (yalnız defter ve sürükleme denetçisi bağlı), defter, denetçi.</summary>
        (BoardView3D board, RecipeBookView book, BoardDragController drag) Board()
        {
            var board = Make<BoardView3D>("Planning board");
            var book = Make<RecipeBookView>("Tarif defteri");
            var drag = Make<BoardDragController>("Board drag");
            Set(board, "book", book);
            Set(board, "drag", drag);
            return (board, book, drag);
        }

        /// <summary>Sunucu; bağlar <c>OnEnable</c>'dan önce kurulur, <c>OnEnable</c> elle çağrılır (Edit modda kendiliğinden çalışmaz).</summary>
        PlanningBoardPresenter Presenter(GameController game, LiberationDeskView surface, SiginakCameraRig rig, BoardView3D board, BoardDragController drag)
        {
            var p = Make<PlanningBoardPresenter>("Planning board presenter");
            p.Configure(game, surface, rig, null, null);
            p.ConfigureBoard(board, drag);
            Call(p, "OnEnable");
            enabled.Add(p);
            return p;
        }

        /// <summary>Defteri açar ve açılışı bitirir (W1: <c>Open</c> → Opening, <c>Advance</c> süresi geçince Open).</summary>
        static void OpenBook(RecipeBookView book)
        {
            book.Open();
            Assert.IsTrue(PlanningBoardPresenter.BookHoldsBoard(book.State), "Open() defteri açılışa alır: " + book.State);
            book.Advance(1f);
            Assert.AreEqual(BookState.Open, book.State, "0,35 s'lik öne gelme 1 s'de biter");
        }

        static SavedCommand Last(Simulation sim, int back = 0) => sim.Record.commands[sim.Record.commands.Count - 1 - back];

        static void AssertCommand(SavedCommand c, string kind, string a, string b, int day)
        {
            Assert.AreEqual(kind, c.kind);
            Assert.AreEqual(a, c.a, kind + " a");
            Assert.AreEqual(b, c.b, kind + " b");
            Assert.AreEqual(day, c.day, kind + " günü");
        }

        static Vector3 P(float x, float y, float angle) => new Vector3(x, y, angle);

        // ---------------------------------------------------------------- ses (E29)

        [Test]
        public void CueOf_MapsEveryBoardSound()
        {
            var expected = new Dictionary<BoardSound, (Cue cue, int number)>
            {
                { BoardSound.Lift, (Cue.BoardLift, 97) }, { BoardSound.Settle, (Cue.BoardMagnet, 98) },
                { BoardSound.Return, (Cue.BoardReturn, 99) }, { BoardSound.Deny, (Cue.BoardDeny, 100) },
                { BoardSound.DeckSlide, (Cue.BoardDeckSlide, 101) }, { BoardSound.DeckCollect, (Cue.BoardDeckCollect, 102) },
                { BoardSound.Tuck, (Cue.BoardItemTuck, 103) }, { BoardSound.Hurda, (Cue.BoardHurda, 104) },
                { BoardSound.Padlock, (Cue.BoardPadlock, 105) }, { BoardSound.Flap, (Cue.BoardFlap, 106) },
                { BoardSound.BookOpen, (Cue.BoardBookOpen, 115) }, { BoardSound.PageTurn, (Cue.BoardPageTurn, 116) },
                { BoardSound.BookClose, (Cue.BoardBookClose, 117) }, { BoardSound.RecipeLift, (Cue.BoardRecipeLift, 118) },
                { BoardSound.ProductSlide, (Cue.BoardProductSlide, 119) },
            };
            var all = (BoardSound[])Enum.GetValues(typeof(BoardSound));
            Assert.AreEqual(all.Length, expected.Count, "her BoardSound eşlenir");
            foreach (var sound in all)
            {
                Assert.IsTrue(expected.ContainsKey(sound), sound.ToString());
                var cue = PlanningBoardPresenter.CueOf(sound);
                Assert.AreEqual(expected[sound].cue, cue, sound.ToString());
                Assert.AreEqual(expected[sound].number, (int)cue, sound + " numarası (SoundBank girişleri int saklar)");
            }
            var fresh = all.Where(s => s >= BoardSound.BookOpen).Select(PlanningBoardPresenter.CueOf).ToList();
            Assert.AreEqual(5, fresh.Distinct().Count(), "defter ve üretim sesleri ayrı");
            CollectionAssert.DoesNotContain(fresh, Cue.BoardFlap, "yeni ses default'a (kapak sesine) düşmez");
        }

        // ---------------------------------------------------------------- defter kipi (§1.12)

        [Test]
        public void BookStates_HoldAndSuspension_Table()
        {
            var hold = new Dictionary<BookState, (bool holds, bool suspends)>
            {
                { BookState.Closed, (false, false) }, { BookState.Opening, (true, true) }, { BookState.Open, (true, true) },
                { BookState.Turning, (true, true) }, { BookState.Lowered, (true, false) }, { BookState.Closing, (false, false) },
            };
            foreach (BookState state in Enum.GetValues(typeof(BookState)))
            {
                Assert.IsTrue(hold.ContainsKey(state), state.ToString());
                Assert.AreEqual(hold[state].holds, PlanningBoardPresenter.BookHoldsBoard(state), state + " defter kipi");
                Assert.AreEqual(hold[state].suspends, PlanningBoardPresenter.BookSuspendsDrag(state),
                    state + " kart girdisi askıda (alçakta denetçi kopyayı sürer)");
            }
        }

        [Test]
        public void HandleBack_ClosesBookBeforeDesk()
        {
            var rig = Rig();
            var (board, book, drag) = Board();
            var p = Presenter(null, null, rig, board, drag);
            rig.Snap(SiginakPose.Shelf);
            Assert.IsTrue(p.IsOpen);

            OpenBook(book);
            Assert.IsTrue(p.HandleBack(), "Esc tüketilir");
            Assert.IsFalse(PlanningBoardPresenter.BookHoldsBoard(book.State), "önce defter kancaya: " + book.State);
            Assert.AreEqual(SiginakPose.Shelf, rig.Current, "kamera K4'te kalır");
            Assert.IsFalse(board.BookMode, "defter kipi kapandı");
            Assert.IsFalse(drag.Suspended, "kart girdisi geri geldi");

            book.Advance(1f);
            Assert.AreEqual(BookState.Closed, book.State);
            Assert.IsTrue(p.HandleBack(), "defter kapalıyken masaya");
            Assert.AreEqual(SiginakPose.Desk, rig.Current);
            Assert.IsFalse(p.HandleBack(), "K1'de panonun işi yok");

            // Açılırken (henüz Open değil) de Esc defteri kapatır
            rig.Snap(SiginakPose.Shelf);
            book.Open();
            Assert.IsTrue(p.HandleBack());
            Assert.IsFalse(PlanningBoardPresenter.BookHoldsBoard(book.State), "açılış yarıda kesilir");
            Assert.AreEqual(SiginakPose.Shelf, rig.Current);
        }

        [Test]
        public void Interactivity_LeavingK4_ClosesBook()
        {
            // Sunucu K4 etkileşimini yalnız kartlı oyun varken açar (UpdateInteractivity: Sim?.Board != null); bu yüzden gerçek oyun
            var (game, surface) = Game();
            var rig = Rig();
            var (board, book, drag) = Board();
            var p = Presenter(game, surface, rig, board, drag);
            rig.Snap(SiginakPose.Shelf);
            Assert.IsTrue(board.Interactive, "K4'e varış etkileşimi açar");

            OpenBook(book);
            Call(p, "SyncBookMode");
            Assert.IsTrue(board.BookMode, "defter açık: defter kipi");
            Assert.IsTrue(drag.Suspended, "defter açık: kart girdisi yok");

            rig.Focus(SiginakPose.Desk);   // K1'e dönüş (animasyonlu)
            Assert.IsFalse(board.Interactive);
            Assert.IsFalse(PlanningBoardPresenter.BookHoldsBoard(book.State), "K4'ten çıkışta defter kapanır (B6): " + book.State);
            Assert.AreEqual(BookState.Closing, book.State, "kapanış animasyonlu (kamera giderken)");
            Assert.IsFalse(board.BookMode);
            Assert.IsFalse(drag.Suspended);
            book.Advance(1f);
            Assert.AreEqual(BookState.Closed, book.State);
        }

        [Test]
        public void BackgroundClick_ClosesBook()
        {
            var rig = Rig();
            var (board, book, drag) = Board();
            var p = Presenter(null, null, rig, board, drag);
            rig.Snap(SiginakPose.Shelf);
            p.Ui.SelectedId = "sakin-1";

            OpenBook(book);
            Call(p, "LateUpdate");
            Assert.IsTrue(drag.Suspended);

            Raise(drag, "BackgroundClicked");
            Assert.IsFalse(PlanningBoardPresenter.BookHoldsBoard(book.State), "pano tıkı defteri kapatır: " + book.State);
            Assert.AreEqual("sakin-1", p.Ui.SelectedId, "tık tüketilir: seçim değişmez");
            Assert.IsFalse(board.BookMode);
            Assert.IsFalse(drag.Suspended);
            Assert.AreEqual(SiginakPose.Shelf, rig.Current, "kamera K4'te");

            // Defter kapalıyken aynı olay bir şey yapmaz
            Assert.DoesNotThrow(() => Raise(drag, "BackgroundClicked"));
            Assert.AreNotEqual(BookState.Opening, book.State);
        }

        [Test]
        public void LockedPlan_BoardClick_ClosesBook()
        {
            var (game, surface) = Game();
            var sim = game.Sim;
            var print = game.ExecuteBoardCommand(s => s.PrintShift());
            Assert.IsTrue(print.Ok, print.Msg);
            Assert.IsTrue(sim.Board.Locked, "basılı fiş planı kilitler");

            var rig = Rig();
            var (board, book, drag) = Board();
            var p = Presenter(game, surface, rig, board, drag);
            rig.Snap(SiginakPose.Shelf);
            Assert.IsFalse(drag.Editable, "kilitli plan düzenlenemez");
            Assert.IsTrue(book.Locked, "defter okunur ama tarif kalkmaz (B10)");

            int commands = sim.Record.commands.Count;
            uint rng = sim.S.Rng.State;
            OpenBook(book);   // kilitli planda da açılır (R18-not "defter okunur")
            Call(p, "SyncBookMode");
            Assert.IsTrue(drag.Suspended);

            Raise(drag, "BackgroundClicked");
            Assert.IsFalse(PlanningBoardPresenter.BookHoldsBoard(book.State), "kilitli planda da pano tıkı defteri kapatır: " + book.State);
            Assert.AreEqual(commands, sim.Record.commands.Count, "komut yok");
            Assert.AreEqual(rng, sim.S.Rng.State, "RNG çekilmez");
        }

        // ---------------------------------------------------------------- bırakma → komut (§1.3) [B]

        [Test]
        public void Drop_Reserve_WritesAndRollsBackChipPose()
        {
            var (game, surface) = Game();
            var sim = game.Sim;
            var p = Presenter(game, surface, null, null, null);
            int day = sim.S.Day;
            var armor = sim.BoardRecipes.First(r => r.Id == "armor");
            int metalNeed = armor.Inputs.Where(i => i.Kind == "metal").Sum(i => i.Quantity);
            Assume.That(metalNeed >= 2, "V.001 zırh plakası 2 metal ister");

            // Tarif iş emrine: tek board_queue; kartın yeri yeni emrin kimliğinde
            string orderId = PlanningBoardPresenter.NextOrderId(sim.Board);
            var queued = p.ApplyDrop(BoardCardKind.Recipe, "armor", BoardTarget.Order(), P(520, 330, -2));
            Assert.IsTrue(queued.Accepted, queued.Reason);
            AssertCommand(Last(sim), "board_queue", "armor", null, day);
            Assert.AreEqual(orderId, sim.Board.Orders.Single().Id);
            Assert.AreEqual(P(520, 330, -2), p.Ui.RecipePose(orderId), "kart bırakıldığı yerde (R18b)");

            // Hammadde iş emrine: mutlak board_reserve +1; fiş bırakıldığı yerde
            int commands = sim.Record.commands.Count;
            var first = p.ApplyDrop(BoardCardKind.Material, "metal", BoardTarget.Order(), P(612, 320, 4));
            Assert.IsTrue(first.Accepted, first.Reason);
            Assert.AreEqual(commands + 1, sim.Record.commands.Count, "tek kayıt");
            AssertCommand(Last(sim), "board_reserve", "metal", "1", day);
            var second = p.ApplyDrop(BoardCardKind.Material, "metal", BoardTarget.Order(), P(668, 330, -6));
            Assert.IsTrue(second.Accepted, second.Reason);
            AssertCommand(Last(sim), "board_reserve", "metal", "2", day);
            Assert.AreEqual(2, BoardDropRules.ChipCount(sim.Board, "metal"));
            Assert.AreEqual(P(612, 320, 4), p.Ui.ChipPoses[BoardUiState.ChipPoseKey("metal", 0)]);
            Assert.AreEqual(P(668, 330, -6), p.Ui.ChipPoses[BoardUiState.ChipPoseKey("metal", 1)]);

            // Ret (ihtiyaçtan fazla; tarifin istemediği tür): komut yok, ön yazılan fiş yeri geri alınır, ileti Core'un
            var before = new Dictionary<string, Vector3>(p.Ui.ChipPoses);
            commands = sim.Record.commands.Count;
            var over = p.ApplyPlan(new BoardDropPlan(BoardDropAction.Reserve, "metal", null, null, metalNeed + 1), BoardCardKind.Material, "metal", P(742, 340, 0));
            Assert.IsFalse(over.Accepted);
            Assert.AreEqual(Loc.T("Bu tarif için uygun tür ve miktarı ayır."), over.Reason);
            var foreign = p.ApplyPlan(new BoardDropPlan(BoardDropAction.Reserve, "devre", null, null, 1), BoardCardKind.Material, "devre", P(800, 340, 0));
            Assert.IsFalse(foreign.Accepted);
            Assert.AreEqual(Loc.T("Bu tarif için uygun tür ve miktarı ayır."), foreign.Reason);
            Assert.AreEqual(commands, sim.Record.commands.Count, "ret komut yazmaz");
            CollectionAssert.AreEquivalent(before, p.Ui.ChipPoses, "fiş yerleri retten önceki gibi");

            // Fiş depoya: board_reserve −1; o fişin yeri silinir, sonraki fiş bir sıra kayar (yerinde kalır)
            var back = p.ApplyDrop(BoardCardKind.Chip, BoardUiState.ChipId("metal", 0), BoardTarget.Store(), P(0, 0, 0));
            Assert.IsTrue(back.Accepted, back.Reason);
            Assert.IsTrue(back.Rerender, "kalan fişler bırakma bitince bir kez daha dizilir");
            AssertCommand(Last(sim), "board_reserve", "metal", "1", day);
            Assert.AreEqual(1, BoardDropRules.ChipCount(sim.Board, "metal"));
            Assert.AreEqual(P(668, 330, -6), p.Ui.ChipPoses[BoardUiState.ChipPoseKey("metal", 0)], "kalan fiş yerinde");
            Assert.IsFalse(p.Ui.ChipPoses.ContainsKey(BoardUiState.ChipPoseKey("metal", 1)));

            // Form içinde fişi taşımak yalnız yerleşimdir
            commands = sim.Record.commands.Count;
            var moved = p.ApplyDrop(BoardCardKind.Chip, BoardUiState.ChipId("metal", 0), BoardTarget.Order(), P(700, 310, 9));
            Assert.IsTrue(moved.Accepted);
            Assert.AreEqual(commands, sim.Record.commands.Count, "yerleşim komut yazmaz");
            Assert.AreEqual(P(700, 310, 9), p.Ui.ChipPoses[BoardUiState.ChipPoseKey("metal", 0)]);

            // Yerleşim dosyası sunucunun yazdığını taşır (kayda girmez)
            var layout = PlanningBoardLayoutStore.Load(sim.Record);
            Assert.IsNotNull(layout, "yerleşim dosyası yazıldı");
            var ui = new BoardUiState();
            PlanningBoardLayoutStore.Apply(layout, ui);
            Assert.AreEqual(P(700, 310, 9), ui.ChipPoses[BoardUiState.ChipPoseKey("metal", 0)]);
            Assert.AreEqual(P(520, 330, -2), ui.RecipePose(orderId));
        }

        [Test]
        public void Drop_Replace_MovesRecipePose()
        {
            var (game, surface) = Game();
            var sim = game.Sim;
            var p = Presenter(game, surface, null, null, null);
            int day = sim.S.Day;

            Assert.IsTrue(p.ApplyDrop(BoardCardKind.Recipe, "armor", BoardTarget.Order(), P(510, 326, 1)).Accepted);
            string oldOrder = sim.Board.Orders.Single().Id;
            Assert.IsTrue(p.ApplyDrop(BoardCardKind.Material, "metal", BoardTarget.Order(), P(612, 320, 0)).Accepted);
            Assert.IsTrue(p.Ui.ChipPoses.ContainsKey(BoardUiState.ChipPoseKey("metal", 0)));

            // Dolu emre başka tarif: board_cancel + board_queue, aynı gün, tek bırakma
            int commands = sim.Record.commands.Count;
            string newOrder = PlanningBoardPresenter.NextOrderId(sim.Board);
            var replaced = p.ApplyDrop(BoardCardKind.Recipe, "medkit", BoardTarget.Order(), P(530, 334, -3));
            Assert.IsTrue(replaced.Accepted, replaced.Reason);
            Assert.AreEqual(commands + 2, sim.Record.commands.Count, "değiştirme iki kayıt (D3)");
            AssertCommand(Last(sim, 1), "board_cancel", oldOrder, null, day);
            AssertCommand(Last(sim), "board_queue", "medkit", null, day);
            Assert.AreEqual(newOrder, sim.Board.Orders.Single().Id);
            Assert.IsNull(p.Ui.RecipePose(oldOrder), "eski emrin kart yeri düşer");
            Assert.AreEqual(P(530, 334, -3), p.Ui.RecipePose(newOrder), "yeni kart bırakıldığı yerde");
            Assert.IsFalse(p.Ui.ChipPoses.ContainsKey(BoardUiState.ChipPoseKey("metal", 0)), "iptalde serbest kalan fişin yeri düşer");

            // Aynı tarifi yeniden bırakmak komutsuz (başlamış işin girdisi boşa gitmesin)
            commands = sim.Record.commands.Count;
            Assert.IsFalse(p.ApplyDrop(BoardCardKind.Recipe, "medkit", BoardTarget.Order(), P(530, 334, 0)).Accepted);
            Assert.AreEqual(commands, sim.Record.commands.Count);

            // Formdaki kart (kimlik = iş emri) form içinde: yalnız yerleşim
            var moved = p.ApplyDrop(BoardCardKind.Recipe, newOrder, BoardTarget.Order(), P(540, 340, 5));
            Assert.IsTrue(moved.Accepted);
            Assert.AreEqual(commands, sim.Record.commands.Count, "yerleşim komut yazmaz");
            Assert.AreEqual(P(540, 340, 5), p.Ui.RecipePose(newOrder));

            // Form dışına (deftere): board_cancel; kartın yeri düşer
            var cancelled = p.ApplyDrop(BoardCardKind.Recipe, newOrder, BoardTarget.Book(), P(70, 452, 0));
            Assert.IsTrue(cancelled.Accepted, cancelled.Reason);
            Assert.AreEqual(commands + 1, sim.Record.commands.Count);
            AssertCommand(Last(sim), "board_cancel", newOrder, null, day);
            Assert.AreEqual(0, sim.Board.Orders.Count);
            Assert.AreEqual(0, p.Ui.RecipePoses.Count);

            // Ret: kuyruk reddedilirse ön yazılan kart yeri geri alınır
            Assert.IsTrue(game.ExecuteBoardCommand(s => s.QueueProduction("armor")).Ok);
            string blocked = PlanningBoardPresenter.NextOrderId(sim.Board);
            commands = sim.Record.commands.Count;
            var refused = p.ApplyPlan(new BoardDropPlan(BoardDropAction.Queue, "medkit"), BoardCardKind.Recipe, "medkit", P(500, 320, 0));
            Assert.IsFalse(refused.Accepted);
            Assert.AreEqual(Loc.T("Önce mevcut üretim emrini tamamla veya iptal et."), refused.Reason);
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.IsNull(p.Ui.RecipePose(blocked), "ön yazım geri alındı");
        }

        // ---------------------------------------------------------------- tık yolunun reddi (§1.9)

        [Test]
        public void TapDeny_FlashesOnlyForInsufficientStock()
        {
            Assert.IsTrue(PlanningBoardPresenter.DeniesStock(BoardCardKind.Material, Loc.T("Serbest hammadde yetersiz.")));
            Assert.IsFalse(PlanningBoardPresenter.DeniesStock(BoardCardKind.Material, Loc.T("Bu tarif için uygun tür ve miktarı ayır.")));
            Assert.IsFalse(PlanningBoardPresenter.DeniesStock(BoardCardKind.Material, Loc.T("Girdisi ayrılabilen bir taslak iş yok.")));
            Assert.IsFalse(PlanningBoardPresenter.DeniesStock(BoardCardKind.Material, null), "kilitli plan (neden yok)");
            Assert.IsFalse(PlanningBoardPresenter.DeniesStock(BoardCardKind.Chip, Loc.T("Serbest hammadde yetersiz.")), "yalnız stok kartı");
            Assert.IsFalse(PlanningBoardPresenter.DeniesStock(BoardCardKind.Person, Loc.T("Serbest hammadde yetersiz.")));
        }

        // ---------------------------------------------------------------- eski konmuş fişler (D2) [B]

        [Test]
        public void ReleaseLegacyStaging_OnceThenSilent()
        {
            var (game, surface) = Game();
            var sim = game.Sim;
            var p = Presenter(game, surface, null, null, null);
            if (!sim.Board.AutomaticProduction) Assert.IsTrue(game.ExecuteBoardCommand(s => s.EnableAutomaticProduction()).Ok);
            Assert.IsTrue(game.ExecuteBoardCommand(s => s.StageProductionMaterial("metal", 2)).Ok, "A dönemi fişleri (geliştirici kaydı)");
            Assert.IsTrue(game.ExecuteBoardCommand(s => s.StageProductionMaterial("kumas", 1)).Ok);
            int day = sim.S.Day, commands = sim.Record.commands.Count;
            uint rng = sim.S.Rng.State;

            // Hikâye açıkken (RetireJobs'un koşulu) dokunulmaz
            game.StoryHold = true;
            Assert.AreEqual(0, p.ReleaseLegacyStaging());
            Assert.AreEqual(commands, sim.Record.commands.Count);
            game.StoryHold = false;

            Assert.AreEqual(2, p.ReleaseLegacyStaging(), "her tür için bir board_stage(tür, 0)");
            Assert.AreEqual(commands + 2, sim.Record.commands.Count);
            var released = new[] { Last(sim, 1), Last(sim) };
            CollectionAssert.AreEquivalent(new[] { "metal", "kumas" }, released.Select(c => c.a));
            foreach (var c in released) AssertCommand(c, "board_stage", c.a, "0", day);
            Assert.IsFalse(sim.Board.ProductionInputs.Any(m => m.Quantity > 0), "fişler stoka döndü");
            Assert.AreEqual(0, BoardDropRules.ChipCount(sim.Board, "metal"));
            Assert.AreEqual(rng, sim.S.Rng.State, "RNG çekilmez");

            Assert.AreEqual(0, p.ReleaseLegacyStaging(), "sonra sessiz");
            Assert.AreEqual(0, p.ReleaseLegacyStaging());
            Assert.AreEqual(commands + 2, sim.Record.commands.Count);

            // Kayıttan devam: aynı oyun yeniden kurulur, konmuş fiş yok → komut yok
            var restored = Replay.Restore(sim.Record);
            Assert.IsFalse(restored.Board.ProductionInputs.Any(m => m.Quantity > 0), "Replay serbest bırakmayı yeniden oynatır");
        }

        [Test]
        public void ReleaseLegacyStaging_NothingStaged_NoCommand()
        {
            var (game, surface) = Game();
            var sim = game.Sim;
            var p = Presenter(game, surface, null, null, null);
            if (!sim.Board.AutomaticProduction) Assert.IsTrue(game.ExecuteBoardCommand(s => s.EnableAutomaticProduction()).Ok);
            Assert.IsTrue(game.ExecuteBoardCommand(s => s.QueueProduction("armor")).Ok);
            Assert.IsTrue(game.ExecuteBoardCommand(s => s.ReserveProductionMaterial("metal", 1)).Ok);
            int commands = sim.Record.commands.Count;
            Assert.AreEqual(0, p.ReleaseLegacyStaging(), "B'nin ayrılan girdisi eski fiş değildir");
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.AreEqual(1, sim.Board.Orders.Single().Reserves.Sum(r => r.Quantity));
        }

        // ---------------------------------------------------------------- yardımcılar ve kaynak taraması

        [Test]
        public void ProjectToPlane_HitsBoardPlaneAlongEyeRay()
        {
            var eye = new Vector3(0, 0, -1);
            var hit = PlanningBoardPresenter.ProjectToPlane(eye, new Vector3(0.1f, 0.2f, -0.5f), 0f);
            Assert.AreEqual(0.2f, hit.x, 1e-5f);
            Assert.AreEqual(0.4f, hit.y, 1e-5f);
            Assert.AreEqual(0f, hit.z, 1e-6f);
            var flat = PlanningBoardPresenter.ProjectToPlane(new Vector3(0, 0, -0.3f), new Vector3(0.3f, 0.1f, -0.3f), -0.01f);
            Assert.AreEqual(new Vector3(0.3f, 0.1f, -0.01f), flat, "düzleme paralel ışın: dik izdüşüm");
        }

        [Test]
        public void NoTemporaryBookCoupling()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "Assets/Corrigible/Desk/Scripts/Bridge/PlanningBoardPresenter.cs");
            Assert.IsTrue(File.Exists(path), path);
            string code = File.ReadAllText(path);
            StringAssert.DoesNotContain("view.Motion.", code, "geçici uGUI defteri (D6) sunucudan ayrıldı");
            StringAssert.DoesNotContain("Motion.BookOpen", code);
            StringAssert.DoesNotContain("Motion.CloseBook", code);
            StringAssert.DoesNotContain("Find(\"", code, "ad araması yok (AGENTS 5)");
            StringAssert.Contains("board3D.Book", code, "defter pano görünümünün bağından");
        }

        [Test]
        public void Unwired_CallsAreSafe()
        {
            var p = Make<PlanningBoardPresenter>("Bare presenter");
            Assert.DoesNotThrow(() =>
            {
                Call(p, "OnEnable");
                Call(p, "LateUpdate");
                Call(p, "Update");
                Assert.IsFalse(p.HandleBack());
                Assert.AreEqual(0, p.ReleaseLegacyStaging());
                Assert.IsFalse(p.ApplyDrop(BoardCardKind.Recipe, "armor", BoardTarget.Order(), Vector3.zero).Accepted);
                Call(p, "OnDisable");
            });
        }
    }
}
