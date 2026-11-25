using System.Collections;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 9, parça 3 (aşama 9 tasarımı §7.1–7.3, §11.6): ödülle açılan ekipman tarifinin sunumun yolundan üretilip takılması,
    /// gerçek DeskGame'de, Play'de. Örme seti tarifi ödülden açılır (gerçek <c>server_reward</c>; bekleyen ödül elle kurulur), defter
    /// gerçek ışınla açılır ve "Örme seti" sayfasına çevrilir, tarif kutusundan basılı tutup iş emrine sürüklenir (<c>board_queue
    /// wire_kit</c>), girdiler ayrılır, vardiya şalter ve devir fişiyle kapanır; ürün depodaki ikinci sırada kart olarak durur ve kişiye
    /// sürüklenir (<c>board_equip</c>). Kapalı sayfa sürüklenmez. Defter testleri gibi editörde koşulur (batchmode'da K4 ışını örtülür).
    /// </summary>
    public sealed class FacilityGearBoardPlayTests
    {
        PointerGesture gesture;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (gesture != null && !gesture.Released) gesture.Release();
            gesture = null;
            yield return ShiftPlayKit.End();
        }

        static Simulation Sim => BoardPlayKit.Sim;
        static BoardView3D Board => BoardPlayKit.Board;
        static RecipeBookView Book => BoardPlayKit.Book;
        static BoardDragController Drag => Board.Drag;

        static void Ok(ActResult r, string what) => Assert.IsTrue(r != null && r.Ok, what + ": " + r?.Msg);

        static RaycastResult PageHit(bool right, Vector2 mm, string context)
        {
            var page = right ? Book.PageRight : Book.PageLeft;
            var col = right ? Book.HitRight : Book.HitLeft;
            Assert.IsTrue(col.enabled, context + ": sayfa çarpıştırıcısı açık");
            var screen = BoardPlayKit.ScreenOfWorld(page.WorldAt(mm));
            var hit = BoardPlayKit.FirstHit(screen);
            Assert.AreSame(col.gameObject, hit.gameObject, context + ": ışın " + BoardPlayKit.DescribeHits(screen));
            return hit;
        }

        IEnumerator OpenBookAt(int spread)
        {
            var col = Book.ClosedHit;
            var screen = BoardPlayKit.ScreenOfWorld(col.transform.TransformPoint(col.center));
            var hit = BoardPlayKit.FirstHit(screen);
            Assert.AreSame(col.gameObject, hit.gameObject, "kapalı defter örtülmemeli; ışın " + BoardPlayKit.DescribeHits(screen));
            Assert.IsTrue(BoardPlayKit.ClickHit(hit), "deftere tık");
            yield return BoardPlayKit.WaitFor(() => Book.State == BookState.Open && !Book.Moving, 3, "defter açılmadı");
            var next = RecipePageMetrics.Default.Geometry(true).NextHit.center;
            while (Book.Spread < spread)
            {
                int from = Book.Spread;
                BoardPlayKit.ClickHit(PageHit(true, next, "sonraki"));
                yield return BoardPlayKit.WaitFor(() => Book.State == BookState.Open && !Book.Moving && Book.Spread == from + 1, 3, "sayfa çevrilmedi");
            }
            yield return BoardPlayKit.Frames(1);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator RecipeDrag_GearPage_ToOrder_Produced_Equipped()
        {
            yield return BoardPlayKit.LoadDesk(tune: FacilityRewardPlayKit.Tune);
            ShiftPlayKit.RequireWired();
            yield return BoardPlayKit.Frames(2);
            var game = BoardPlayKit.Game;
            // Örme seti tarifi (Batı Avrupa'nın ödülü): bekleyen ödül elle kurulur, seçim gerçek komut
            Sim.Facility.PendingReward = "weu";
            Ok(game.ExecuteBoardCommand(s => s.ChooseServerReward("weu", FacilityGear.ChoiceRecipe)), "server_reward weu tarif");
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.Frames(2);
            var s0 = Board.Last;
            int wire = s0.Recipes.FindIndex(r => r.Id == FacilityGear.Wire);
            Assert.AreEqual(6, wire, "örme seti yedinci sayfa (açık sayfaların sonu)");
            Assert.IsFalse(s0.Recipes[wire].Locked);
            Assert.IsTrue(s0.Recipes[wire + 1].Locked, "sonraki sayfa kapalı");
            Assert.AreEqual(7, Book.SpreadCount, "13 sayfa, 7 açılım");

            // ---- kapalı sayfa (sağda, 8. sayfa) sürüklenmez; örme seti sayfası (solda) iş emrine
            yield return OpenBookAt(wire / 2);
            Assert.AreEqual(FacilityGear.Wire, Book.PageLeft.Recipe.Id);
            Assert.IsTrue(Book.PageRight.Recipe.Locked, "sağ sayfa kapalı");
            Assert.AreEqual(BookZone.Page, Book.ZoneOf(PageHit(true, RecipePageMetrics.Default.Geometry(true).Box.center, "kapalı kutu")), "kapalı sayfanın kutusu sayfa sayılır");
            var mark = ShiftPlayKit.Mark();
            var boxHit = PageHit(false, RecipePageMetrics.Default.Geometry(false).Box.center, "örme seti kutusu");
            gesture = BoardPlayKit.Press(boxHit);
            yield return gesture.Move(boxHit.screenPosition + new Vector2(16, -16), 1);
            Assert.IsNotNull(Drag.Adopted, "tarif kopyası denetçide");
            yield return gesture.Move(BoardPlayKit.BoardScreen(BoardLayout.Order.center), 10);
            yield return gesture.Hold(4);
            Assert.AreEqual(BoardTargetKind.Order, Drag.HoverTarget.Kind, "iş emri üstünde");
            gesture.Release();
            gesture = null;
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy, 3, "bırakma bitmedi");
            var cmds = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, cmds.Count, ShiftPlayKit.Describe(cmds));
            Assert.AreEqual("board_queue", cmds[0].kind);
            Assert.AreEqual(FacilityGear.Wire, cmds[0].a);
            yield return BoardPlayKit.WaitFor(() => Book.State == BookState.Closed && !Book.Moving, 3, "defter kapanmadı");

            // ---- girdiler (fişler) ve vardiya: şalter ve devir fişi
            var recipe = Sim.BoardRecipes.Single(r => r.Id == FacilityGear.Wire);
            foreach (var m in recipe.Inputs)
                Ok(game.ExecuteBoardCommand(s => s.ReserveProductionMaterial(m.Kind, m.Quantity)), "board_reserve " + m.Kind);
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(OrderState.Full, Board.Last.Order.State, Board.Last.Order.Status);
            Assert.AreEqual(recipe.Inputs.Sum(m => m.Quantity), Board.Last.Chips.Count, "fişler");
            yield return ShiftPlayKit.PrintToTongue();
            yield return ShiftPlayKit.FileOnEvrak();
            var item = Sim.Board.Equipment.SingleOrDefault(e => e.Kind == FacilityGear.Wire);
            Assert.IsNotNull(item, "örme seti üretildi");
            Assert.IsNull(item.OwnerId, "depoda");

            // ---- depo ikinci sırası: kart, YENİ, kişiye sürükleme
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.WaitFor(() => !Board.NewItemSliding && !Board.NewItemWaiting, 3, "ürün kayışı bitmedi");
            yield return BoardPlayKit.Frames(2);
            var s = Board.Last;
            Assert.IsNotNull(s.Gear[0], "ikinci sırada örme seti yığını");
            Assert.AreEqual(item.Id, s.Gear[0].TopIds.Last());
            Assert.IsTrue(s.NewItem != null && s.NewItem.Gear && s.NewItem.ItemId == item.Id, "YENİ ekipmanda");
            var card = Board.CardFor(item.Id);
            Assert.IsNotNull(card, "eşya kartı");
            Assert.IsTrue(card.gameObject.activeInHierarchy, "kart görünür");
            var at = BoardLayout.LocalToMm(card.Target);
            Assert.Less(Vector2.Distance(at, BoardLayout.Gear(0)), 1.5f, "kart ikinci sıranın çapasında: " + at);
            var person = Sim.Board.People.First(p => p.Alive && p.Job == "tek");
            var personCard = Board.CardFor(person.Id);
            mark = ShiftPlayKit.Mark();
            yield return BoardPlayKit.Drag(card.gameObject, personCard.transform.position, 12);
            yield return BoardPlayKit.WaitFor(() => !Drag.Busy, 3, "takma bitmedi");
            cmds = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, cmds.Count, ShiftPlayKit.Describe(cmds));
            Assert.AreEqual("board_equip", cmds[0].kind);
            Assert.AreEqual(item.Id, cmds[0].a);
            Assert.AreEqual(person.Id, cmds[0].b);
            yield return BoardPlayKit.Frames(2);
            Assert.IsNull(Board.Last.Gear[0], "takılan ekipman sıradan çıkar");
            Assert.AreEqual(FacilityGear.Wire, Sim.PlanningSquad().Single(x => x.PersonId == person.Id).Gear, "baskın emrinde örme seti");
        }
    }
}
