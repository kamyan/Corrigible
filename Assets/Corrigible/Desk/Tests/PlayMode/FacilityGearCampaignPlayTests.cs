using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using K = Corrigible.Desk.Tests.FacilityPlayKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 9'un bitti ölçütü (aşama 9 tasarımı §11.7; TESIS §12 satır 9): <b>bir harekâtta her ekipman üretilip kullanılır.</b>
    /// Tek harekât (tohum 79, bayrak ve olaylar yalnız bellek dengesinde), gerçek DeskGame ve Baskin sahneleri, Play'de:
    /// <list type="number">
    /// <item><b>Ödül → tarif (7):</b> yedi bölgenin (weu, af, sa, me, sea, in, jk) bekleyen ödülü kayda girmeyen kurulumla yazılır, kâğıt
    /// kapının yanındaki panoya iner ve ödül kâğıdından seçilir: ilki gerçek tık ve tuşlarla (K8'den panodaki kâğıda tık, 1 + Enter),
    /// öbürleri kâğıt kapıdayken sunumun karar yoluyla (<see cref="SiginakServerReward.Decide"/>). Her birinde tek
    /// <c>server_reward … tarif</c> komutu ve defterde yeni sayfa.</item>
    /// <item><b>Üretim (7):</b> her ekipmanın iş emri defterden: ilki gerçek fareyle tarif sayfasından iş emrine sürüklenir, öbürleri
    /// sunucunun bırakma yolundan (<see cref="PlanningBoardPresenter.ApplyDrop"/>: tarif → iş emri, stok → iş emri fişleri). Hammadde ve
    /// erzak yetmezse kurulum stoğu doldurur (kayda girmez; bu test kayıttan devamı sınamaz). İlk kapanış şalter ve devir fişiyle, öbürleri
    /// <c>PrintBoardShift</c> + <c>FileBoardShift</c> ile. Ürün depodaki ikinci sırada kart olarak durur (anlık görüntü + kart nesnesi).</item>
    /// <item><b>Takma ve emir (3 baskın):</b> ekipman kişiye panodan takılır (ilki gerçek sürükleme, öbürleri bırakma yolu; <c>board_equip</c>),
    /// baskın emri görev fişinin yolundan (<see cref="LiberationDeskView.LaunchSector"/>). Baskın 1: örme seti, lokum, taret; baskın 2:
    /// kelepçe, vinç, yem; baskın 3: köpek. Baskın tahtasındaki her uzmanın ekipmanı ve hakkı panodan gelir (<c>GearUp</c> kullanılmaz).</item>
    /// <item><b>Kullanım:</b> her uzman kendi ekipmanını R tuşu + tahta tıkıyla verir (ışınlama ve hedefin hazırlanması kurulumdur), kelepçeden
    /// sonra tutsak tahtada tıkla seçilip E ile komut verir, köpek 4 ile seçilip E ile havlar, sonra düşer (kurulum: uyanma işareti). Her
    /// eylemde <c>Hist</c> bir artar. Kanat sonra oyuncunun yolundan oynanır (<see cref="FacilityPlayKit.PlayWing"/>), sonuç kartı, operasyon
    /// raporu Enter ile dosyalanır.</item>
    /// <item><b>Sonda:</b> yedi türün her biri üretildi ve kullanıldı; harcanan kelepçe panodan silindi; düşen köpek panoda dinleniyor.</item>
    /// </list>
    /// Yalnız editörde (defter, kapı panosu ve EVRAK ışınları; batchmode'da K4 örtüsü bilinen ortam kalanı).
    /// </summary>
    public sealed class FacilityGearCampaignPlayTests
    {
        PointerGesture gesture;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (gesture != null && !gesture.Released) gesture.Release();
            gesture = null;
            yield return K.UnloadRaid();
            yield return ShiftPlayKit.End();
        }

        static Simulation Sim => BoardPlayKit.Sim;
        static Corrigible.Game.GameController Game => BoardPlayKit.Game;
        static BoardView3D Board => BoardPlayKit.Board;
        static RecipeBookView Book => BoardPlayKit.Book;
        static SiginakServerReward Reward => BoardPlayKit.Find<SiginakServerReward>();
        static SiginakOperation Operation => BoardPlayKit.Find<SiginakOperation>();
        static LiveRaidDataSource Src => K.Live;

        static void Ok(ActResult r, string what) => Assert.IsTrue(r != null && r.Ok, what + ": " + r?.Msg);

        /// <summary>Ödül bölgesi → açtığı ekipman tarifi (FacilityGear.Reward tablosu; sıra üretim sırasıdır).</summary>
        static readonly (string Region, string Gear)[] Rewards =
        {
            ("weu", FacilityGear.Wire), ("af", FacilityGear.Charge), ("sa", FacilityGear.Turret), ("me", FacilityGear.Decoy),
            ("sea", FacilityGear.Winch), ("in", FacilityGear.Clamp), ("jk", FacilityGear.Dog),
        };

        /// <summary>Ekipmanın baskındaki eylemi (köpekte havla).</summary>
        static string ActionOf(string gear)
        {
            switch (gear)
            {
                case FacilityGear.Wire: return Baskin.ActWire;
                case FacilityGear.Charge: return Baskin.ActCharge;
                case FacilityGear.Turret: return Baskin.ActTurret;
                case FacilityGear.Decoy: return Baskin.ActDecoy;
                case FacilityGear.Winch: return Baskin.ActWinch;
                case FacilityGear.Clamp: return Baskin.ActClamp;
                default: return Baskin.ActBark;
            }
        }

        readonly HashSet<string> produced = new HashSet<string>();
        readonly HashSet<string> used = new HashSet<string>();

        [UnityTest, Timeout(600000)]
        public IEnumerator EveryGear_ProducedEquippedUsed()
        {
            yield return BoardPlayKit.LoadDesk(tune: FacilityRewardPlayKit.Tune);
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            ShiftPlayKit.RequireWired();
            Assert.IsNotNull(Reward, "SiginakServerReward sahnede");
            foreach (var (region, gear) in Rewards) Assert.AreEqual(gear, FacilityGear.Reward(region).Recipe, region + ": ödül tablosu");
            int start = Sim.Record.commands.Count;

            // ---------- 1. Ödül → tarif (7)
            for (int i = 0; i < Rewards.Length; i++) yield return ChooseRecipe(i);
            Assert.AreEqual(13, Sim.BoardRecipes.Count, "defterde 6 + 7 sayfa");

            // ---------- 2. Üretim (7)
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.Frames(2);
            for (int i = 0; i < Rewards.Length; i++) yield return Produce(Rewards[i].Gear, i == 0);
            CollectionAssert.AreEquivalent(FacilityGear.Kinds, produced, "yedi ekipman üretildi");

            // ---------- 3–4. Takma, emir ve kullanım: üç baskın
            var squad = Sim.PlanningSquad();
            Assert.AreEqual(3, squad.Count, "üç kişilik kadro");
            string tek = Person(squad, "tek"), emp = Person(squad, "emp"), ana = Person(squad, "ana");

            yield return Equip(new[] { (tek, FacilityGear.Wire), (emp, FacilityGear.Charge), (ana, FacilityGear.Turret) }, true);
            yield return Raid("baskın 1", new[] { ("tek", FacilityGear.Wire), ("emp", FacilityGear.Charge), ("ana", FacilityGear.Turret) }, null);
            yield return QuickClose("baskın 1 sonrası");

            yield return Equip(new[] { (tek, FacilityGear.Clamp), (emp, FacilityGear.Winch), (ana, FacilityGear.Decoy) }, false);
            yield return Raid("baskın 2", new[] { ("tek", FacilityGear.Clamp), ("emp", FacilityGear.Winch), ("ana", FacilityGear.Decoy) }, CaptiveCommand);
            Assert.IsFalse(Sim.Board.Equipment.Any(e => e.Kind == FacilityGear.Clamp), "harcanan kelepçe panodan silindi");
            yield return QuickClose("baskın 2 sonrası");
            yield return BoardPlayKit.Frames(2);
            Assert.IsFalse(Board.Last.Gear[Array.IndexOf(BoardLayout.GearKinds, FacilityGear.Clamp)] != null, "depoda kelepçe yığını yok");

            yield return Equip(new[] { (tek, FacilityGear.Dog) }, false);
            yield return Raid("baskın 3", new (string, string)[0], DogBarkThenDown);
            var dog = Sim.Board.Equipment.Single(e => e.Kind == FacilityGear.Dog);
            Assert.AreEqual(Math.Max(0, Sim.B.facilityDogRestTurns), dog.Rest, "düşen köpek panoda dinleniyor");
            Assert.AreEqual(tek, dog.OwnerId, "köpek sahibinde kalır");

            // ---------- 5. Sonda: yedi türün her biri üretildi ve kullanıldı
            CollectionAssert.AreEquivalent(FacilityGear.Kinds.Select(ActionOf).ToArray(), used.Where(k => k != Baskin.ActCommand).ToArray(),
                "yedi ekipmanın her biri baskında kullanıldı");
            CollectionAssert.Contains(used, Baskin.ActCommand, "tutsak komut verdi");
            var cmds = ShiftPlayKit.Since(start);
            Assert.AreEqual(7, cmds.Count(c => c.kind == "server_reward" && c.b == FacilityGear.ChoiceRecipe), "yedi tarif seçimi");
            CollectionAssert.AreEquivalent(FacilityGear.Kinds, cmds.Where(c => c.kind == "board_queue").Select(c => c.a).ToArray(), "yedi iş emri");
            Assert.AreEqual(3, cmds.Count(c => c.kind == "raidres"), "üç baskın sonucu");
            Assert.GreaterOrEqual(cmds.Count(c => c.kind == "board_equip"), 7, "yedi takma");
            Debug.Log("[FacilityGearCampaignPlayTests] komut " + cmds.Count + " · gün " + Sim.S.Day + " · kullanılan " + string.Join(",", used));
        }

        static string Person(List<SquadSlot> squad, string type)
        {
            var slot = squad.FirstOrDefault(s => s.Type == type);
            Assert.IsNotNull(slot, "kadroda " + type);
            return slot.PersonId;
        }

        // ---------------------------------------------------------------- ödül

        /// <summary>Bekleyen ödül (kurulum) → kâğıt çekmeceye iner → tarif seçilir (ilki çekmeceden alınıp 1 + Enter, öbürleri kâğıt
        /// çekmecedeyken Decide(0)) → karar çekmeceye konur ve şalterle gider.</summary>
        IEnumerator ChooseRecipe(int i)
        {
            var (region, gear) = Rewards[i];
            Sim.Facility.PendingReward = region;
            ShiftPlayKit.RaiseChanged();
            yield return ShiftPlayKit.WaitDoor(Reward.View, region + ": ödül kâğıdı", 4);
            Assert.IsTrue(Reward.AtDoor, region + ": ödül kâğıdı kapıda");
            int pages = Sim.BoardRecipes.Count;
            var mark = ShiftPlayKit.Mark();
            if (i == 0)
            {
                yield return ShiftPlayKit.TakeFromDoor(Reward.View, region + ": ödül kâğıdı");
                Assert.IsTrue(Reward.InHand, region + ": çekmecedeki kâğıt ele geldi");
                yield return BoardPlayKit.PressKey(Key.Digit1);
                Assert.IsTrue(Reward.Last.Rows[0].Selected, region + ": 1 tarifi işaretler");
                yield return BoardPlayKit.PressKey(Key.Enter);
            }
            else Assert.IsTrue(Reward.Decide(0), region + ": karar (tarif)");
            var cmds = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, cmds.Count, region + ": tek komut · " + ShiftPlayKit.Describe(cmds));
            Assert.AreEqual(("server_reward", region, FacilityGear.ChoiceRecipe), (cmds[0].kind, cmds[0].a, cmds[0].b), region);
            Assert.AreEqual(pages + 1, Sim.BoardRecipes.Count, region + ": defterde yeni sayfa");
            Assert.IsTrue(Sim.BoardRecipes.Any(r => r.Id == gear), region + ": " + gear + " tarifi açık");
            // Karar çekmeceye konur; şalterle gider (Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md §A.0)
            yield return ShiftPlayKit.SendFromDrawer(Reward.View, region + ": ödül kâğıdı");
            yield return BoardPlayKit.WaitFor(() => !Reward.View.Visible && !Reward.Busy, 4, region + ": kâğıt gönderilip gizlenmedi");
        }

        // ---------------------------------------------------------------- üretim

        static Vector3 OrderPose(float dx = 0) => new Vector3(BoardLayout.Order.center.x + dx, BoardLayout.Order.center.y, 0);

        IEnumerator Produce(string gear, bool real)
        {
            var recipe = Sim.BoardRecipes.Single(r => r.Id == gear);
            // Kurulum: hammadde ve erzak (kayda girmez)
            foreach (var m in recipe.Inputs)
            {
                var have = Sim.Board.Material(m.Kind);
                if (have == null) Sim.Board.Materials.Add(have = new BoardMaterial { Kind = m.Kind });
                have.Quantity = Math.Max(have.Quantity, m.Quantity);
            }
            if (Sim.Shelter.Supplies < recipe.Supplies + 2) Sim.Shelter.Supplies = recipe.Supplies + 6;
            ShiftPlayKit.RaiseChanged();
            yield return BoardPlayKit.Frames(2);

            var mark = ShiftPlayKit.Mark();
            if (real) yield return DragRecipeToOrder(gear);
            else
            {
                var outcome = BoardPlayKit.Presenter.ApplyDrop(BoardCardKind.Recipe, gear, BoardTarget.Order(), OrderPose());
                Assert.IsTrue(outcome.Accepted, gear + ": tarif iş emrine (" + outcome.Reason + ")");
            }
            var cmds = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, cmds.Count, gear + ": " + ShiftPlayKit.Describe(cmds));
            Assert.AreEqual(("board_queue", gear), (cmds[0].kind, cmds[0].a), gear + ": iş emri");

            // Girdi fişleri: stok kartı iş emrine (her bırakış bir fiş)
            int chip = 0;
            foreach (var m in recipe.Inputs)
                for (int q = 0; q < m.Quantity; q++, chip++)
                {
                    var outcome = BoardPlayKit.Presenter.ApplyDrop(BoardCardKind.Material, m.Kind, BoardTarget.Order(), OrderPose(6 * chip));
                    Assert.IsTrue(outcome.Accepted, gear + ": fiş " + m.Kind + " (" + outcome.Reason + ")");
                }
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(OrderState.Full, Board.Last.Order.State, gear + ": " + Board.Last.Order.Status);

            // Vardiyalar: ilki şalter ve devir fişiyle, öbürleri komutla
            int before = Sim.Board.Equipment.Count(e => e.Kind == gear);
            for (int guard = 0; guard < 5 && Sim.Board.Equipment.Count(e => e.Kind == gear) == before; guard++)
            {
                if (real && guard == 0)
                {
                    yield return ShiftPlayKit.PrintToTongue();
                    yield return ShiftPlayKit.FileOnEvrak();
                }
                else yield return QuickClose(gear);
            }
            var item = Sim.Board.Equipment.LastOrDefault(e => e.Kind == gear);
            Assert.IsNotNull(item, gear + " üretilmedi");
            Assert.IsNull(item.OwnerId, gear + ": depoda");
            produced.Add(gear);

            // Depo ikinci sırası: anlık görüntü ve kart nesnesi
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.WaitFor(() => !Board.NewItemSliding && !Board.NewItemWaiting, 4, gear + ": ürün kayışı bitmedi");
            yield return BoardPlayKit.Frames(2);
            var stack = Board.Last.Gear[Array.IndexOf(BoardLayout.GearKinds, gear)];
            Assert.IsNotNull(stack, gear + ": ikinci sırada yığın");
            CollectionAssert.Contains(stack.TopIds, item.Id, gear + ": yığının kartı");
            var card = Board.CardFor(item.Id);
            Assert.IsTrue(card != null && card.gameObject.activeInHierarchy, gear + ": depoda kart görünür");
        }

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

        /// <summary>Gerçek fare: kapalı deftere tık, tarifin açılımına çevir, kutusundan basılı tutup iş emrine bırak.</summary>
        IEnumerator DragRecipeToOrder(string gear)
        {
            int index = Board.Last.Recipes.FindIndex(r => r.Id == gear);
            Assert.GreaterOrEqual(index, 0, gear + ": defterde sayfa");
            var col = Book.ClosedHit;
            var screen = BoardPlayKit.ScreenOfWorld(col.transform.TransformPoint(col.center));
            var hit = BoardPlayKit.FirstHit(screen);
            Assert.AreSame(col.gameObject, hit.gameObject, "kapalı defter örtülmemeli; ışın " + BoardPlayKit.DescribeHits(screen));
            Assert.IsTrue(BoardPlayKit.ClickHit(hit), "deftere tık");
            yield return BoardPlayKit.WaitFor(() => Book.State == BookState.Open && !Book.Moving, 3, "defter açılmadı");
            var next = RecipePageMetrics.Default.Geometry(true).NextHit.center;
            while (Book.Spread < index / 2)
            {
                int from = Book.Spread;
                BoardPlayKit.ClickHit(PageHit(true, next, "sonraki"));
                yield return BoardPlayKit.WaitFor(() => Book.State == BookState.Open && !Book.Moving && Book.Spread == from + 1, 3, "sayfa çevrilmedi");
            }
            yield return BoardPlayKit.Frames(1);
            bool right = index % 2 == 1;
            Assert.AreEqual(gear, (right ? Book.PageRight : Book.PageLeft).Recipe.Id, "tarifin sayfası");
            var boxHit = PageHit(right, RecipePageMetrics.Default.Geometry(right).Box.center, gear + " kutusu");
            gesture = BoardPlayKit.Press(boxHit);
            yield return gesture.Move(boxHit.screenPosition + new Vector2(16, -16), 1);
            Assert.IsNotNull(Board.Drag.Adopted, "tarif kopyası denetçide");
            yield return gesture.Move(BoardPlayKit.BoardScreen(BoardLayout.Order.center), 10);
            yield return gesture.Hold(4);
            Assert.AreEqual(BoardTargetKind.Order, Board.Drag.HoverTarget.Kind, "iş emri üstünde");
            gesture.Release();
            gesture = null;
            yield return BoardPlayKit.WaitFor(() => !Board.Drag.Busy, 3, "bırakma bitmedi");
            yield return BoardPlayKit.WaitFor(() => Book.State == BookState.Closed && !Book.Moving, 3, "defter kapanmadı");
        }

        /// <summary>Vardiya kapanışı komutla (<c>board_print</c> + <c>board_file</c>); gün bir ilerler.</summary>
        static IEnumerator QuickClose(string what)
        {
            int day = Sim.S.Day;
            Ok(Game.PrintBoardShift(), "board_print · " + what);
            Ok(Game.FileBoardShift(Sim.Board.ReceiptId), "board_file · " + what);
            Assert.AreEqual(day + 1, Sim.S.Day, what + ": kapanış günü bir ilerletir");
            yield return BoardPlayKit.WaitSeconds(.6f);
        }

        // ---------------------------------------------------------------- takma ve baskın

        /// <summary>Depodaki ekipman kişiye: ilki (<paramref name="realFirst"/>) gerçek sürükleme, öbürleri bırakma yolu; tek <c>board_equip</c>.</summary>
        IEnumerator Equip((string Person, string Gear)[] pairs, bool realFirst)
        {
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.WaitFor(() => !Board.NewItemSliding && !Board.NewItemWaiting && !Board.Drag.Busy, 4, "pano hazır değil");
            yield return BoardPlayKit.Frames(2);
            for (int i = 0; i < pairs.Length; i++)
            {
                var (person, gear) = pairs[i];
                var item = Sim.Board.Equipment.FirstOrDefault(e => e.Kind == gear && e.OwnerId == null);
                Assert.IsNotNull(item, gear + ": depoda yok");
                var mark = ShiftPlayKit.Mark();
                if (realFirst && i == 0)
                {
                    var card = Board.CardFor(item.Id);
                    Assert.IsTrue(card != null && card.gameObject.activeInHierarchy, gear + ": depo kartı");
                    var personCard = Board.CardFor(person);
                    Assert.IsNotNull(personCard, person + ": kişi kartı");
                    yield return BoardPlayKit.Drag(card.gameObject, personCard.transform.position, 12);
                    yield return BoardPlayKit.WaitFor(() => !Board.Drag.Busy, 3, "takma bitmedi");
                }
                else
                {
                    var outcome = BoardPlayKit.Presenter.ApplyDrop(BoardCardKind.Item, item.Id, BoardTarget.Person(person), Vector3.zero);
                    Assert.IsTrue(outcome.Accepted, gear + " → " + person + " (" + outcome.Reason + ")");
                }
                var cmds = ShiftPlayKit.Since(mark.commands);
                Assert.AreEqual(1, cmds.Count, gear + ": " + ShiftPlayKit.Describe(cmds));
                Assert.AreEqual(("board_equip", item.Id, person), (cmds[0].kind, cmds[0].a, cmds[0].b), gear + ": takma");
                yield return BoardPlayKit.Frames(2);
                Assert.AreEqual(gear, Sim.PlanningSquad().Single(s => s.PersonId == person).Gear, gear + ": baskın emrinde");
            }
        }

        /// <summary>
        /// Görev fişinin yolundan baskın emri, odada her uzman kendi ekipmanını R + tık ile verir, tur biter (aygıtlar çalışır),
        /// <paramref name="extra"/> (tutsak ya da köpek) oynar; kanat oynanır, sonuç kartı, masaya dönüş ve operasyon raporu Enter ile.
        /// </summary>
        IEnumerator Raid(string what, (string Type, string Gear)[] uses, Func<IEnumerator> extra)
        {
            BoardPlayKit.Rig.Snap(SiginakPose.Desk);
            yield return BoardPlayKit.Frames(2);
            var sector = Sim.NextLiberationSector(Sim.S.Liberation.CurrentRegion);
            Assert.IsNotNull(sector, what + ": sektör");
            var squad = Sim.PlanningSquad();
            int logs = Sim.S.RaidLog.Count;
            var launched = BoardPlayKit.Surface.LaunchSector(sector.Id);
            Assert.IsNotNull(launched, what + ": masa kilitli");
            Ok(launched, what + ": baskın emri");
            yield return K.Source();
            var B = Src.State;
            Assert.IsNotNull(B.Room, what + ": tesis odası");

            // Ekipman ve hak panodan: emrin kadro satırı → tahtadaki uzman
            for (int i = 0; i < squad.Count; i++)
            {
                if (string.IsNullOrEmpty(squad[i].Gear) || squad[i].Gear == FacilityGear.Dog) continue;
                var u = B.Units.Single(v => Baskin.IsSpecialist(v) && v.Slot == i);
                Assert.AreEqual(squad[i].Gear, u.Gear, what + ": " + u.Type + " ekipmanı panodan");
                Assert.AreEqual(FacilityGear.RaidCharges(Sim.B, squad[i].Gear, Sim.Facility.Perks), u.GearLeft, what + ": " + u.Type + " hakkı");
            }

            // Kurulum: makineler kalkar, olay yok, süre uzun (testin kurduğu kareler bozulmasın)
            yield return K.Settle(Src, what + " açılış");
            K.Calm(Src);
            Src.State.Ev = null;
            Src.State.MaxTurns = Src.State.Turn + 10;
            Src.ClearHover();
            yield return K.Settle(Src, what + " kurulum");

            foreach (var (type, gear) in uses) yield return UseGear(K.Team(Src, type), ActionOf(gear));
            if (uses.Length > 0) yield return K.EndTurnConfirmed(Src);
            if (extra != null) yield return extra();

            var visits = new List<string>();
            yield return K.PlayWing(Src, visits);
            yield return BoardPlayKit.WaitSeconds(.8f);
            Src.Continue();
            yield return BoardPlayKit.WaitFor(() => !RaidLauncher.IsRaidLoaded, 8, what + ": baskın sahnesi kapanmadı");
            Assert.IsNull(Sim.S.Raid, what);
            Assert.AreEqual(logs + 1, Sim.S.RaidLog.Count, what + ": sonuç");
            Assert.IsTrue(Sim.S.RaidLog.Last().Success, what + ": sektör alındı");
            yield return BoardPlayKit.WaitFor(() => Operation.ReportOpen, 8, what + ": operasyon raporu inmedi");
            yield return BoardPlayKit.WaitSeconds(1.2f);
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.WaitFor(() => !Operation.ReportOpen, 4, what + ": rapor dosyalanmadı");
            Debug.Log("[FacilityGearCampaignPlayTests] " + what + " · paftalar " + string.Join(" ", visits));
        }

        static bool Free(Baskin.BState B, int x, int y)
        {
            if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N) return false;
            var c = Baskin.CellAt(B, x, y);
            return c.T == "floor" && c.O == null && c.Door == 0 && c.Vent == 0 && Baskin.UnitAt(B, x, y) == null && !Baskin.IsGateTile(B, x, y)
                && !(B.CargoX == x && B.CargoY == y) && !B.Units.Any(b => Baskin.LyingBody(b) && b.X == x && b.Y == y)
                && !B.Exits.Any(e => e.X == x && e.Y == y);
        }

        /// <summary>
        /// Uzmanı eylemin hedefi olan bir kareye komşu boş zemine ışınlar (kurulum; kelepçede komşuya makine, lokumda tahtada uygun duvar
        /// yoksa komşu kare duvar olur) ve hedefi döndürür.
        /// </summary>
        static Baskin.Pt Prepare(Baskin.Unit u, string kind)
        {
            var B = Src.State;
            for (int pass = 0; pass < 2; pass++)
                for (int y = 1; y < Baskin.N - 1; y++)
                    for (int x = 1; x < Baskin.N - 1; x++)
                    {
                        if (!Free(B, x, y) && !(u.X == x && u.Y == y)) continue;
                        u.X = x; u.Y = y;
                        if (kind == Baskin.ActClamp || pass == 1)
                        {
                            var q = Baskin.Dirs.Select(d => new Baskin.Pt(x + d[0], y + d[1])).FirstOrDefault(p => Free(B, p.X, p.Y));
                            if (!Free(B, q.X, q.Y)) continue;
                            if (kind == Baskin.ActClamp)
                            {
                                var m = K.AddMachine(Src, "dron", q.X, q.Y);
                                m.Max = m.Hp = 2; m.Armor = 0;
                            }
                            else if (kind == Baskin.ActCharge) Baskin.CellAt(B, q.X, q.Y).T = "wall";
                        }
                        var targets = Baskin.ActionTargets(B, u, kind);
                        if (targets.Count > 0) { Src.ClearHover(); return targets[0]; }
                        if (kind == Baskin.ActClamp) Assert.Fail("kelepçe hedefi kurulamadı");
                    }
            Assert.Fail(kind + ": hedef bulunamadı · oda " + B.Room?.Id);
            return default;
        }

        /// <summary>Ekipman eylemi oyuncunun yolundan: harfle seçim, R (kip ve kart satırı), tahtaya tık; Hist bir artar.</summary>
        IEnumerator UseGear(Baskin.Unit u, string kind)
        {
            yield return K.Settle(Src, kind + " öncesi");
            var at = Prepare(u, kind);
            yield return K.Settle(Src, kind + " kurulum");
            Src.Select(K.Letter(u));
            Assert.AreEqual(K.Letter(u), Src.Selected, kind + ": seçilemedi");
            yield return K.ArmGearByKey(Src, kind);
            var vm = Src.Current;
            Assert.IsTrue(vm.GearArmed && vm.GearRows.Length == 1 && vm.GearRows[0].Armed, kind + ": kartın ekipman satırı kurulu");
            CollectionAssert.Contains(vm.AbilityTargets, new Vector2Int(at.X, at.Y), kind + ": hedef çerçeveli");
            int hist = Src.State.Hist.Count;
            K.ClickTile(at.X, at.Y);
            Assert.AreEqual(hist + 1, Src.State.Hist.Count, kind + ": tahtaya tık eylemi vermedi");
            used.Add(kind);
            yield return K.Settle(Src, kind);
        }

        /// <summary>Baskın 2: kelepçelenen makine sonraki turda tahtada tıkla seçilir, E ile komut verir.</summary>
        IEnumerator CaptiveCommand()
        {
            var cap = Src.State.Units.FirstOrDefault(Baskin.IsCaptive);
            Assert.IsNotNull(cap, "tutsak tahtada");
            var near = K.FreeNeighbor(Src.State, new Baskin.Pt(cap.X, cap.Y), true);
            var victim = K.AddMachine(Src, "dron", near.X, near.Y);
            victim.Hp = 1;
            yield return K.Settle(Src, "hedef");
            K.ClickTile(cap.X, cap.Y);
            Assert.AreEqual(cap.Id, Src.SelectedOther, "tutsak tahtada tıkla seçilir");
            yield return K.ArmByKey(Src, Baskin.ActCommand);
            int hist = Src.State.Hist.Count;
            K.ClickTile(victim.X, victim.Y);
            Assert.AreEqual(hist + 1, Src.State.Hist.Count, "tutsak komut verdi");
            used.Add(Baskin.ActCommand);
            yield return K.Settle(Src, "komut");
        }

        /// <summary>Baskın 3: köpek panodan gelir (dördüncü kart), 4 ile seçilir, E ile havlar; sonra düşer (kurulum: uyanma işareti).</summary>
        IEnumerator DogBarkThenDown()
        {
            var dog = Src.State.Units.FirstOrDefault(Baskin.IsDog);
            Assert.IsNotNull(dog, "köpek emirden tahtaya geldi");
            Assert.AreEqual(4, Src.Current.Team.Length, "dördüncü kart: köpek");
            Assert.IsTrue(Src.Current.Team[3].Dog);
            yield return K.SelectDog(Src, dog.Id);
            yield return K.ArmByKey(Src, Baskin.ActBark);
            int hist = Src.State.Hist.Count;
            K.ClickTile(dog.X, dog.Y);
            Assert.AreEqual(hist + 1, Src.State.Hist.Count, "havla verildi");
            used.Add(Baskin.ActBark);
            yield return K.Settle(Src, "havla");
            dog = Src.State.Units.First(v => v.Id == dog.Id);
            dog.Hp = 1;
            Src.State.Spawns.Add(new Baskin.Pt(dog.X, dog.Y));
            Src.ClearHover();
            yield return K.EndTurnConfirmed(Src);
            Assert.IsTrue(Src.State.Units.First(v => v.Id == dog.Id).Dead, "köpek tahtadan çekildi");
            Assert.IsTrue(Src.Current.Team.First(t => t.Dog).Down, "köpeğin kartı TAHTADAN ÇEKİLDİ");
        }
    }
}
