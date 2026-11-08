using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Kişi kartının gerçek sürüklemesi (Revizyon 3 · Aşama 10, PLAN §4 "Kişi kartı sürükleme PlayMode"; PROMPT §8.6). DeskGame gerçek
    /// sahnesi, K4; kart <see cref="BoardPlayKit.Drag"/> ile sürüklenir, komut <see cref="BoardDragController"/> →
    /// <c>PlanningBoardPresenter.DropRequested</c> → <c>BoardDropRules</c> → <c>GameController.ExecuteBoardCommand</c> zincirinden
    /// geçer. Takas <c>AssignResident</c>'ın kendisidir: tek <c>board_assign</c> yazar, ikinci kart Core'da yer değiştirir.
    /// </summary>
    public sealed class PersonCardDragPlayTests
    {
        static BoardView3D Board => BoardPlayKit.Board;
        static Simulation Sim => BoardPlayKit.Sim;
        static BoardDragController Drag => Board.Drag;

        readonly struct Drop
        {
            public readonly BoardCardView Card;
            public readonly BoardTarget Target;
            public readonly bool Accepted;
            public readonly float At;

            public Drop(BoardCardView card, BoardTarget target, bool accepted, float at)
            {
                Card = card; Target = target; Accepted = accepted; At = at;
            }
        }

        readonly List<Drop> drops = new List<Drop>();
        BoardSoundLog sounds;
        BoardDragController watched;
        (int commands, uint rng) mark;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return BoardPlayKit.LoadDesk();
            Assert.IsNotNull(Board, "BoardView3D");
            Assert.IsNotNull(Drag, "BoardDragController");
            Assert.IsNotNull(Board.Deck, "BostaDeckView");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (watched != null) watched.Resolved -= OnResolved;
            watched = null;
            sounds?.Dispose();
            sounds = null;
            drops.Clear();
            yield return BoardPlayKit.TearDown();
        }

        void OnResolved(BoardCardView card, BoardTarget target, bool accepted) =>
            drops.Add(new Drop(card, target, accepted, Time.unscaledTime));

        /// <summary>
        /// Atama: kapalı destenin üst kartı boş cebe (MUTFAK) → tek <c>board_assign(kişi, cook)</c>, RNG çekilmez; kart cepte
        /// <c>motion.board.settleS</c> sürede oturur, bir "tık"; desteden çıkar, göreve yazılır. Başlangıçta boş tek cep EĞİTİM'dir
        /// (o <c>board_train</c> yazar); aşçı düzen için eğitime alınır ki destenin üstünde cebi hiç görmemiş biri kalsın.
        /// </summary>
        [UnityTest]
        public IEnumerator DeckTopCard_ToEmptyPocket_Assigns()
        {
            const string job = "cook";
            var cook = BoardDropRules.Occupant(Sim.Board, job);
            Assert.IsNotNull(cook, "V.001'de MUTFAK cebinde biri var");
            string subject = PlanningBoardData.Subjects.First(s => cook.Skill(s) < Sim.B.boardExpertAt);
            var train = BoardPlayKit.Game.ExecuteBoardCommand(s => s.TrainResident(cook.Id, subject));
            Assert.IsTrue(train != null && train.Ok, "düzen: aşçı eğitime (" + train?.Msg + ")");
            yield return Arrive();
            yield return BoardPlayKit.WaitFor(() => !Board.CardFor(cook.Id).Moving, 2, "aşçının kartı eğitim cebine varmadı");

            var before = Board.Last;
            Assert.IsFalse(before.PocketOccupant.TryGetValue(job, out var occupant) && !string.IsNullOrEmpty(occupant), "düzen: MUTFAK cebi boş (" + occupant + ")");
            Assert.IsNotEmpty(before.DeckOrder, "düzen: destede kart var");
            string person = before.DeckOrder[0];
            Assert.AreNotEqual(cook.Id, person, "üst kart aşçı değil");
            Assert.IsNull(Sim.AssignResidentCheck(person, job), "Core bu atamayı kabul eder");
            var card = Board.CardFor(person);
            Assert.IsNotNull(card, "üst kartın görünümü");
            Assert.IsFalse(Board.Deck.IsOpen, "deste kapalı");
            Assert.AreSame(card, Board.Deck.Top, "sürüklenen kart destenin üst kartı");
            int deckCount = before.DeckOrder.Count;

            yield return DropOn(card, job);
            yield return BoardPlayKit.WaitFor(() => !card.Moving, 2, "kart cebe oturmadı");

            float took = Time.unscaledTime - drops[0].At, settle = Token("motion.board.settleS", 0.06f), tolerance = BoardPlayKit.FrameTolerance;
            Assert.That(took, Is.InRange(settle - tolerance, settle + tolerance), "oturma süresi (" + took.ToString("0.000") + " s)");
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy && card.LiftMm <= 1e-3f, 2, "bırakma bitmedi");

            AssertCommand(person, job);
            Assert.AreEqual(job, Sim.Board.Person(person).Job, "kişi göreve yazıldı");
            var after = Board.Last;
            Assert.AreEqual(person, after.PocketOccupant[job], "anlık görüntüde cep dolu");
            CollectionAssert.DoesNotContain(after.DeckOrder, person, "desteden çıktı");
            Assert.AreEqual(deckCount - 1, after.DeckOrder.Count, "deste bir azaldı");
            Assert.IsFalse(Board.Deck.Contains(card), "deste görünümünde yok");
            AssertSeated(card, job);
            Assert.AreEqual(1, DragSounds(BoardSound.Settle), "bir oturma \"tık\"ı (" + sounds + ")");
            AssertNotRefused();
        }

        /// <summary>
        /// Takas: dolu cepteki kart (TEKNİSYEN) başka bir dolu cebe (ÜRETİM) → tek <c>board_assign(kişi, craft)</c>, iki kişi yer
        /// değiştirir, RNG çekilmez; iki kart birlikte <c>motion.board.swapS</c> sürede düz yoldan gider, varışta iki "tık".
        /// </summary>
        [UnityTest]
        public IEnumerator OccupiedPockets_Swap()
        {
            const string from = "tek", to = "craft";
            yield return Arrive();
            var snapshot = Board.Last;
            Assert.IsTrue(snapshot.PocketOccupant.TryGetValue(from, out var moved) && !string.IsNullOrEmpty(moved), "V.001'de " + from + " cebi dolu");
            Assert.IsTrue(snapshot.PocketOccupant.TryGetValue(to, out var partner) && !string.IsNullOrEmpty(partner), "V.001'de " + to + " cebi dolu");
            Assert.AreNotEqual(moved, partner);
            Assert.IsNull(Sim.AssignResidentCheck(moved, to), "Core bu takası kabul eder");
            var card = Board.CardFor(moved);
            var partnerCard = Board.CardFor(partner);
            Assert.IsNotNull(card, "sürüklenen kartın görünümü");
            Assert.IsNotNull(partnerCard, "ortağın görünümü");

            yield return DropOn(card, to);
            Assert.IsTrue(partnerCard.Moving, "ortak, komutun eşzamanlı çiziminde yola çıktı");
            yield return BoardPlayKit.WaitFor(() => !card.Moving && !partnerCard.Moving, 2, "takas bitmedi");

            float took = Time.unscaledTime - drops[0].At, swap = Token("motion.board.swapS", 0.32f), tolerance = BoardPlayKit.FrameTolerance;
            Assert.That(took, Is.InRange(swap - tolerance, swap + tolerance), "takas süresi (" + took.ToString("0.000") + " s)");
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy && DragSounds(BoardSound.Settle) >= 2, 2, "ikinci \"tık\" gelmedi (" + sounds + ")");

            AssertCommand(moved, to);
            Assert.AreEqual(to, Sim.Board.Person(moved).Job, "sürüklenen kişi yeni görevde");
            Assert.AreEqual(from, Sim.Board.Person(partner).Job, "ortak sürüklenenin eski görevinde");
            var after = Board.Last;
            Assert.AreEqual(moved, after.PocketOccupant[to], to + " cebinde sürüklenen");
            Assert.AreEqual(partner, after.PocketOccupant[from], from + " cebinde ortak");
            AssertSeated(card, to);
            AssertSeated(partnerCard, from);
            Assert.AreEqual(2, DragSounds(BoardSound.Settle), "iki oturma \"tık\"ı (" + sounds + ")");
            AssertNotRefused();
        }

        // ------------------------------------------------------------------ yardımcılar

        /// <summary>K4'e gider, süren kart hareketlerinin bitmesini bekler, bırakma ve ses günlüklerini açar.</summary>
        IEnumerator Arrive()
        {
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy, 2, "pano kart hareketinde");
            Assert.IsTrue(Board.Interactive, "K4'te pano girdisi açık");
            Assert.IsTrue(Drag.Editable, "plan açık");
            watched = Drag;
            watched.Resolved += OnResolved;
            sounds = new BoardSoundLog(Board);
        }

        /// <summary>Kartı cebin kart merkezine sürükleyip bırakır; bırakmanın tek ve kabul edilmiş olduğunu doğrular.</summary>
        IEnumerator DropOn(BoardCardView card, string job)
        {
            mark = ShiftPlayKit.Mark();
            BoardPlayKit.ResetFrameStats();
            yield return BoardPlayKit.Drag(card.gameObject, BoardPlayKit.BoardWorld(BoardLayout.CardCentre(job)), 12);
            Assert.AreEqual(1, drops.Count, "tek bırakma sonucu (BoardDragController.Resolved)");
            Assert.AreSame(card, drops[0].Card, "sonuç sürüklenen kartın");
            Assert.AreEqual(BoardTarget.Pocket(job), drops[0].Target, "hedef cep");
            Assert.IsTrue(drops[0].Accepted, "bırakma kabul edildi");
        }

        /// <summary>Bırakmadan beri tam bir <c>board_assign</c> (bugüne) yazıldı; RNG değişmedi.</summary>
        void AssertCommand(string person, string job)
        {
            var got = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, got.Count, "tek komut: " + ShiftPlayKit.Describe(got));
            Assert.AreEqual("board_assign", got[0].kind, ShiftPlayKit.Describe(got));
            Assert.AreEqual(person, got[0].a, "komutun kişisi");
            Assert.AreEqual(job, got[0].b, "komutun görevi");
            Assert.AreEqual(Sim.S.Day, got[0].day, "komut bugüne yazılır");
            Assert.AreEqual(mark.rng, BoardPlayKit.Snapshot().rng, "RNG durumu değişmemeli");
        }

        /// <summary>Kart cebin merkezinde, inik, denetçiden çıkmış ve yeniden tıklanır.</summary>
        void AssertSeated(BoardCardView card, string job)
        {
            var seat = BoardLayout.MmToLocal(BoardLayout.CardCentre(job), BoardLayout.CardZ(BoardLayout.PocketLevel));
            Assert.Less(Vector3.Distance(seat, card.Target), 1e-5f, card.Id + ": hedefi " + job + " cebi");
            Assert.Less(Vector3.Distance(seat, card.transform.localPosition), 1e-4f, card.Id + ": " + job + " cebinde (±0,1 mm)");
            Assert.AreEqual(0, card.TargetAngle, 1e-3f, card.Id + ": düz");
            Assert.AreEqual(0, card.LiftMm, 1e-3f, card.Id + ": inik");
            Assert.IsFalse(Drag.Owns(card), card.Id + ": sahiplik panoya döndü");
            Assert.IsTrue(card.Collider != null && card.Collider.enabled, card.Id + ": girdi geri verildi");
        }

        void AssertNotRefused()
        {
            Assert.AreEqual(0, DragSounds(BoardSound.Deny), "ret sesi yok (" + sounds + ")");
            Assert.AreEqual(0, DragSounds(BoardSound.Return), "geri dönüş sesi yok (" + sounds + ")");
            Assert.IsNull(Drag.Held, "tutulan kart kalmadı");
            Assert.IsFalse(BoardPlayKit.Rig.InputSuspended, "kamera kısayolları geri verildi");
        }

        /// <summary>Denetçinin kendi ses olayları (deste ve depo olayları sayılmaz).</summary>
        int DragSounds(BoardSound sound) => sounds.Entries.Count(e => e.FromDrag && e.Sound == sound);

        static float Token(string key, float plan)
        {
            var theme = SiginakTheme.Active;
            Assert.IsNotNull(theme, "SiginakTheme.Active");
            Assert.IsTrue(theme.TryNumber(key, out var value), key);
            Assert.AreEqual(plan, value, 1e-4f, key);
            return value;
        }
    }
}
