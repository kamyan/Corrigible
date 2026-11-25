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
    /// Tesis aşama 5 (§7.2), gerçek DeskGame K4: GÖZETİM cebi (tek kip: asset dengesiyle hep açık). Plaka, bant ve şeritler görünür;
    /// destenin üst kartı cebe sürüklenince tek <c>board_assign(kişi, watch)</c> yazılır, kart cebe oturur ve sunucunun her karedeki
    /// <c>RetireJobs</c>'u onu BOŞTA'ya indirmez; Tab sırası cebe uğrar.
    /// </summary>
    public sealed class FacilityWatchPlayTests
    {
        static BoardView3D Board => BoardPlayKit.Board;
        static Simulation Sim => BoardPlayKit.Sim;
        static BoardDragController Drag => Board.Drag;

        readonly List<(BoardCardView card, BoardTarget target, bool accepted)> drops = new List<(BoardCardView, BoardTarget, bool)>();
        BoardDragController watched;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (watched != null) watched.Resolved -= OnResolved;
            watched = null;
            drops.Clear();
            yield return BoardPlayKit.TearDown();
        }

        void OnResolved(BoardCardView card, BoardTarget target, bool accepted) => drops.Add((card, target, accepted));

        IEnumerator Arrive()
        {
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy, 2, "pano kart hareketinde");
            Assert.IsTrue(Board.Interactive, "K4'te pano girdisi açık");
            watched = Drag;
            watched.Resolved += OnResolved;
        }

        /// <summary>Tab ile bütün hedefleri bir kez dolaşır (sıradaki hedefler).</summary>
        static List<BoardTarget> TabAround()
        {
            Drag.ClearKeyboardTarget();
            var seen = new List<BoardTarget>();
            for (int i = 0; i < BoardDragController.KeyboardTargets.Length; i++)
            {
                Drag.StepKeyboardTarget(1);
                seen.Add(Drag.KeyboardTarget);
            }
            Drag.ClearKeyboardTarget();
            return seen;
        }

        /// <summary>
        /// Deste ve tepsisi GÖZETİM'e göre: açıkken 980 mm, kapalıyken bugünkü 1098 mm (§14 sekizinci tur); üst kart ve tepsinin
        /// çarpıştırıcısı aynı x'te, açık elin aralığı da aynı kurala göre.
        /// </summary>
        static void AssertDeckAt(bool watch)
        {
            var model = BoardPlayKit.BoardModel.transform;
            float want = BoardLayout.Deck(watch).x;
            var tray = Board.DeckTray;
            Assert.IsNotNull(tray, "BoardView3D.deckTray");
            Assert.AreEqual(want, BoardLayout.LocalToMm(model.InverseTransformPoint(tray.transform.TransformPoint(tray.center))).x, 0.5f, "tepsi");
            Assert.AreEqual(watch, Board.Deck.WatchOpen);
            int n = Board.Deck.Cards.Count;
            Assert.Greater(n, 0);
            var top = Board.Deck.Top.transform;
            Assert.AreEqual(want, BoardLayout.LocalToMm(model.InverseTransformPoint(top.position)).x, 0.5f, "destenin üst kartı");
        }

        [UnityTest]
        public IEnumerator DeckCardToWatch_IsOneAssign_AndStays()
        {
            yield return BoardPlayKit.LoadDesk();
            yield return Arrive();
            Assert.IsTrue(Board.Last.WatchOpen);
            foreach (var part in Board.WatchParts) Assert.IsTrue(part.activeInHierarchy, part.name + " görünür");
            yield return BoardPlayKit.WaitFor(() => Board.Deck.Top != null && !Board.Deck.Top.Moving, 2, "deste oturmadı");
            AssertDeckAt(true);
            CollectionAssert.Contains(TabAround(), BoardTarget.Pocket("watch"), "Tab GÖZETİM'e uğrar");

            string person = Board.Last.DeckOrder[0];
            var card = Board.CardFor(person);
            Assert.AreSame(card, Board.Deck.Top);
            var mark = ShiftPlayKit.Mark();
            yield return BoardPlayKit.Drag(card.gameObject, BoardPlayKit.BoardWorld(BoardLayout.CardCentre("watch")), 12);
            Assert.AreEqual(1, drops.Count);
            Assert.AreEqual(BoardTarget.Pocket("watch"), drops[0].target);
            Assert.IsTrue(drops[0].accepted);
            yield return BoardPlayKit.WaitFor(() => !card.Moving && !Drag.Busy, 2, "kart cebe oturmadı");
            var got = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, got.Count, ShiftPlayKit.Describe(got));
            Assert.AreEqual("board_assign", got[0].kind);
            Assert.AreEqual(person, got[0].a);
            Assert.AreEqual("watch", got[0].b);
            Assert.AreEqual(mark.rng, Sim.S.Rng.State, "RNG çekilmez");

            yield return BoardPlayKit.Frames(5);
            Assert.AreEqual("watch", Sim.Board.Person(person).Job, "RetireJobs GÖZETİM'i emekli saymaz");
            Assert.AreEqual(1, ShiftPlayKit.Since(mark.commands).Count, "ek komut yok");
            Assert.AreEqual(person, Board.Last.PocketOccupant["watch"]);
            var seat = BoardLayout.MmToLocal(BoardLayout.CardCentre("watch"), BoardLayout.CardZ(BoardLayout.PocketLevel));
            Assert.Less(Vector3.Distance(seat, card.transform.localPosition), 1e-4f, "kart GÖZETİM cebinde");
            Assert.AreEqual(Loc.F("Dinleme · PAFTA {0}", Sim.FacilityIntelTarget.Index), Board.Last.Person(person).Strip, "her vardiya bir pafta");
        }
    }
}
