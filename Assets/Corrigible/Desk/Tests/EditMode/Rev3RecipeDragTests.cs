using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 5 · W3 (PLAN §1.3, §1.6, §1.8–§1.10, §1.12; D1 = B): tarif kartı ve iş emri sürüklemesinin kuralları ve
    /// sürükleme denetçisinin yeni yolları.
    /// <list type="bullet">
    /// <item>Kurallar (<see cref="BoardDropRules"/>, saf): defterden kopya boş emre tek <c>board_queue</c>, dolu emre aynı gün iki kayıt
    /// (<c>board_cancel</c> + <c>board_queue</c>); formdaki kart form içinde yalnız yerleşim, form dışında (<see cref="BoardTargetKind.Book"/>)
    /// <c>board_cancel</c>; fiş sayısı başlamamış emrin ayrılanı; yetersiz stokta kart alınmaz; kilitli planda hiçbir yol komut yazmaz.
    /// Her kabul edilen yol Core'un sonucunu önceden bilir, ret iletisi Core'unkidir.</item>
    /// <item>Denetçi (<see cref="BoardDragController"/>, edit modda kurulan küçük pano): alma reddinde kart kalkmaz, 2 mm titrer, "tok";
    /// kırmızı yalnız yetersiz stokta; defter kipinde (<see cref="BoardDragController.Suspended"/>) her tık
    /// <see cref="BoardDragController.BackgroundClicked"/> (kilitli planda da); tarif kartının hedefi çarpıştırıcıdan değil yüz
    /// düzleminden; benimsenen kopya (<see cref="BoardDragController.Adopt"/>) iş emrine oturur ya da deftere döner.</item>
    /// </list>
    /// Edit modda <c>Update</c> çalışmaz: hareketler hedefleri ve özel adım yöntemleriyle ölçülür; komut ve RNG her adımda denetlenir.
    /// </summary>
    public sealed class Rev3RecipeDragTests
    {
        const string LockedMessage = "Plan kilitli. Fişi devir kutusuna bırak.";
        const string Insufficient = "Serbest hammadde yetersiz.";

        readonly List<Object> created = new List<Object>();
        System.Func<bool> blockedWas;

        [SetUp]
        public void SetUp()
        {
            Loc.SetLanguage(Loc.Turkish);
            blockedWas = DeskTarget.Blocked;
            DeskTarget.Blocked = null;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = created.Count - 1; i >= 0; i--)
            {
                var o = created[i];
                if (o == null) continue;
                if (o is RenderTexture rt) rt.Release();
                Object.DestroyImmediate(o);
            }
            created.Clear();
            DeskTarget.Blocked = blockedWas;
            Loc.SetLanguage(Loc.Turkish);
        }

        // ================================================================ simülasyon kurulumu (Rev3BoardDropRulesTests ile aynı)

        static Simulation Ready(uint seed = 79) => Simulation.NewGame(seed, new Balance
        {
            planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100
        });

        /// <summary>Sunucunun ilk açılıştaki <c>RetireJobs</c>'u: otomatik üretim ve emekli görevlerin BOŞTA'ya alınması.</summary>
        static Simulation Retired(uint seed = 79)
        {
            var sim = Ready(seed);
            Assert.IsTrue(sim.EnableAutomaticProduction().Ok);
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                Assert.IsTrue(sim.AssignResident(p.Id, "rest").Ok);
            return sim;
        }

        /// <summary>Tarifin bütün girdileri ayrılmış emir (plan denetimi geçer).</summary>
        static void QueueFull(Simulation sim, string recipeId)
        {
            Assert.IsTrue(sim.QueueProduction(recipeId).Ok, recipeId);
            var recipe = sim.BoardRecipes.First(r => r.Id == recipeId);
            foreach (var input in recipe.Inputs)
                Assert.IsTrue(sim.ReserveProductionMaterial(input.Kind, input.Quantity).Ok, recipeId + " " + input.Kind);
        }

        /// <summary>Vardiyayı kapatır (devir fişi bas + dosyala).</summary>
        static void Close(Simulation sim)
        {
            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
        }

        static ResidentState Maker(Simulation sim) =>
            sim.Board.People.FirstOrDefault(p => p.Alive && p.Down <= 0 && p.Hp > 0 && p.Job == "craft");

        static string State(Simulation sim)
        {
            var b = sim.Board;
            var sb = new StringBuilder();
            sb.Append(sim.S.Rng.State).Append('|').Append(sim.Record.commands.Count).Append('|').Append(b.Locked).Append(b.Printed).Append('|');
            foreach (var p in b.People) sb.Append(p.Id).Append(p.Job).Append(p.TrainingSubject).Append(p.Restricted).Append(';');
            foreach (var e in b.Equipment) sb.Append(e.Id).Append(e.OwnerId).Append(';');
            foreach (var m in b.Materials) sb.Append(m.Kind).Append(m.Quantity).Append(';');
            foreach (var m in b.ProductionInputs) sb.Append(m.Kind).Append(m.Quantity).Append(';');
            foreach (var o in b.Orders)
            {
                sb.Append(o.Id).Append(o.RecipeId).Append(o.Remaining).Append(o.Started).Append(':');
                foreach (var r in o.Reserves) sb.Append(r.Kind).Append(r.Quantity).Append(',');
                sb.Append(';');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Bırakmayı sunucunun yaptığı gibi yürütür (tek <c>ExecuteBoardCommand</c> = tek komut işlevi): geçerli olmalı, yerleşim
        /// değil, RNG çekmez, tam <paramref name="records"/> kayıt yazar, hepsi bugünün günü. Yazılan kayıtları döner.
        /// </summary>
        static SavedCommand[] Drop(Simulation sim, BoardCardKind kind, string id, BoardTarget t, int records = 1)
        {
            string label = kind + " " + id + " → " + t;
            Assert.IsTrue(BoardDropRules.Valid(kind, id, t, sim), label + ": " + BoardDropRules.Reason(kind, id, t, sim));
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(kind, id, t, sim), label);
            var command = BoardDropRules.CommandFor(kind, id, t, sim);
            Assert.IsNotNull(command, label);
            int before = sim.Record.commands.Count;
            uint rng = sim.S.Rng.State;
            int day = sim.S.Day;
            var result = command(sim);
            Assert.IsTrue(result.Ok, label + ": " + result.Msg);
            Assert.AreEqual(before + records, sim.Record.commands.Count, label + ": kayıt sayısı.");
            Assert.AreEqual(rng, sim.S.Rng.State, label + ": pano komutları RNG çekmez.");
            var written = sim.Record.commands.Skip(before).ToArray();
            foreach (var c in written) Assert.AreEqual(day, c.day, label + ": " + c.kind + " bugünün kaydı.");
            return written;
        }

        static void AssertCommand(SavedCommand c, string kind, string a, string b)
        {
            Assert.AreEqual(kind, c.kind);
            Assert.AreEqual(a, c.a);
            Assert.AreEqual(b, c.b);
        }

        static void AssertNoCommand(Simulation sim, BoardCardKind kind, string id, BoardTarget t)
        {
            string label = kind + " " + id + " → " + t;
            Assert.IsNull(BoardDropRules.CommandFor(kind, id, t, sim), label);
            Assert.IsFalse(BoardDropRules.Valid(kind, id, t, sim), label);
            Assert.IsTrue(BoardDropRules.Plan(kind, id, t, sim).IsNone, label);
            Assert.IsNull(BoardDropRules.Reason(kind, id, t, sim), label);
        }

        static void AssertRefused(Simulation sim, BoardCardKind kind, string id, BoardTarget t, string message)
        {
            string label = kind + " " + id + " → " + t;
            Assert.IsFalse(BoardDropRules.Valid(kind, id, t, sim), label);
            Assert.AreEqual(message, BoardDropRules.Reason(kind, id, t, sim), label);
            var command = BoardDropRules.CommandFor(kind, id, t, sim);
            Assert.IsNotNull(command, label);
            string before = State(sim);
            var result = command(sim);
            Assert.IsFalse(result.Ok, label);
            Assert.AreEqual(message, result.Msg, label);
            Assert.AreEqual(before, State(sim), label + ": reddedilen komut kayda girmez, durumu değiştirmez.");
        }

        // ================================================================ kurallar: yetersiz kaynak ve fiş sayısı

        [Test]
        public void Material_FreeZero_CannotBePicked()
        {
            var sim = Retired();
            Assert.AreEqual(sim.B.boardStartMetal, sim.BoardMaterialFree("metal"));
            Assert.IsNull(BoardDropRules.CanPick(BoardCardKind.Material, "metal", sim), "serbest 3: kart kalkar");

            // Bakım kiti emri + 1 kimyasal: kimyasalın serbesti 0 → kart kalkmaz (Core'un iletisi)
            Assert.IsTrue(sim.QueueProduction("medkit").Ok);
            Assert.IsTrue(sim.ReserveProductionMaterial("kimyasal", 1).Ok);
            Assert.AreEqual(0, sim.BoardMaterialFree("kimyasal"));
            Assert.AreEqual(Insufficient, BoardDropRules.CanPick(BoardCardKind.Material, "kimyasal", sim));
            Assert.IsNull(BoardDropRules.CanPick(BoardCardKind.Material, "kumas", sim), "kumaş serbest 2");
            Assert.IsNull(BoardDropRules.CanPick(BoardCardKind.Chip, BoardUiState.ChipId("kimyasal", 0), sim), "fiş geri alınabilir");
            Assert.IsNull(BoardDropRules.CanPick(BoardCardKind.Recipe, sim.Board.Orders[0].Id, sim), "formdaki kart");
            Assert.IsNull(BoardDropRules.CanPick(BoardCardKind.Material, "altin", sim), "bilinmeyen tür: kart yok, ret yok");
            Assert.IsTrue(BoardDragController.FlashesStock(BoardCardKind.Material, BoardDropRules.CanPick(BoardCardKind.Material, "kimyasal", sim)));

            // Serbest var ama tarif bu türü istemiyor: kart kalkar, bırakış Core iletisiyle geri döner (kırmızı yok)
            Assert.IsNull(BoardDropRules.CanPick(BoardCardKind.Material, "metal", sim));
            Assert.AreEqual("Bu tarif için uygun tür ve miktarı ayır.", BoardDropRules.Reason(BoardCardKind.Material, "metal", BoardTarget.Order(), sim));
            Assert.IsFalse(BoardDragController.FlashesStock(BoardCardKind.Material, BoardDropRules.Reason(BoardCardKind.Material, "metal", BoardTarget.Order(), sim)));

            // Panosuz kayıt, baskın, kilit: Core'un sırasıyla
            Assert.AreEqual("Bu kayıtta vardiya panosu yok.", BoardDropRules.CanPick(BoardCardKind.Material, "metal", null));
            var raid = Ready();
            Assert.IsTrue(raid.StartLiberationRaid("weu_1").Ok);
            foreach (var kind in new[] { BoardCardKind.Material, BoardCardKind.Chip, BoardCardKind.Recipe, BoardCardKind.Person, BoardCardKind.Item })
                Assert.AreEqual("Baskın sürerken plan değiştirilemez.", BoardDropRules.CanPick(kind, "metal", raid), kind.ToString());

            var locked = Retired();
            QueueFull(locked, "armor");
            Assert.IsTrue(locked.PrintShift().Ok, locked.BoardPlanCheck());
            Assert.AreEqual(LockedMessage, BoardDropRules.CanPick(BoardCardKind.Material, "devre", locked));

            // CanPick saf: komut, RNG, durum aynı
            var pure = Retired();
            string pureBefore = State(pure);
            for (int i = 0; i < 3; i++)
                foreach (var kind in PlanningBoardData.MaterialKinds) BoardDropRules.CanPick(BoardCardKind.Material, kind, pure);
            Assert.AreEqual(pureBefore, State(pure));
        }

        [Test]
        public void ChipCount_ReservesOfUnstartedOrder()
        {
            var sim = Retired();
            foreach (var kind in PlanningBoardData.MaterialKinds) Assert.AreEqual(0, BoardDropRules.ChipCount(sim.Board, kind), kind);
            Assert.AreEqual(0, BoardDropRules.ChipCount(null, "metal"));

            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            Assert.AreEqual(0, BoardDropRules.ChipCount(sim.Board, "metal"), "emir var, ayrılan yok: fiş yok");
            Assert.IsTrue(sim.ReserveProductionMaterial("metal", 2).Ok);
            Assert.IsTrue(sim.ReserveProductionMaterial("kumas", 1).Ok);
            Assert.AreEqual(2, BoardDropRules.ChipCount(sim.Board, "metal"));
            Assert.AreEqual(1, BoardDropRules.ChipCount(sim.Board, "kumas"));
            Assert.AreEqual(0, BoardDropRules.ChipCount(sim.Board, "devre"));
            Assert.AreEqual(BoardDropRules.Staged(sim.Board, "metal") + BoardDropRules.Reserved(sim.Board, "metal"), BoardDropRules.ChipCount(sim.Board, "metal"));

            // Fiş sırası: 0 … ayrılan − 1 iş emri üstünde yerleşimdir, fazlası yok
            Assert.IsTrue(BoardDropRules.IsLayoutOnly(BoardCardKind.Chip, BoardUiState.ChipId("metal", 1), BoardTarget.Order(), sim));
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(BoardCardKind.Chip, BoardUiState.ChipId("metal", 2), BoardTarget.Order(), sim));
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(BoardCardKind.Chip, BoardUiState.ChipId("kumas", 1), BoardTarget.Order(), sim));

            // Kapanışta girdiler ödenir: başlamış emirde fiş yok (D7)
            Close(sim);
            Assert.AreEqual(1, sim.S.Day);
            var order = sim.Board.Orders.FirstOrDefault();
            if (order != null) Assert.IsTrue(order.Started);
            foreach (var kind in PlanningBoardData.MaterialKinds) Assert.AreEqual(0, BoardDropRules.ChipCount(sim.Board, kind), "kapanıştan sonra " + kind);

            // Eski konmuş fiş + başlamamış emrin ayrılanı birlikte sayılır
            var legacy = Retired();
            Assert.IsTrue(legacy.StageProductionMaterial("metal", 1).Ok);
            Assert.IsTrue(legacy.QueueProduction("emp_cell").Ok);
            Assert.IsTrue(legacy.ReserveProductionMaterial("metal", 1).Ok);
            Assert.AreEqual(2, BoardDropRules.ChipCount(legacy.Board, "metal"));
        }

        // ================================================================ kurallar: defterden kopya

        [Test]
        public void Recipe_ToEmptyOrder_IsOneQueue()
        {
            var sim = Retired();
            var maker = Maker(sim);
            Assert.IsNotNull(maker, "tohum 79: ÜRETİM cebinde biri var");

            var p = BoardPreviewText.For(BoardCardKind.Recipe, "armor", BoardTarget.Order(), sim);
            Assert.AreEqual("İŞ EMRİ ← ZIRH PLAKASI", p.Title);
            Assert.AreEqual("girdi: 2 metal + 1 kumaş · üretici: " + maker.Name, p.Body);
            Assert.IsFalse(p.Warning);
            Assert.IsTrue(p.Valid);

            var c = Drop(sim, BoardCardKind.Recipe, "armor", BoardTarget.Order());
            AssertCommand(c[0], "board_queue", "armor", null);
            Assert.AreEqual(1, sim.Board.Orders.Count);
            Assert.AreEqual("armor", sim.Board.Orders[0].RecipeId);
            Assert.IsFalse(sim.Board.Orders[0].Started);

            // Aynı tarif yeniden: aynı yer (komutsuz; başlamış işin girdisi boşa gitmesin)
            AssertNoCommand(sim, BoardCardKind.Recipe, "armor", BoardTarget.Order());
            // Deftere dönüş, yanlış hedef ve bilinmeyen tarif komutsuz
            AssertNoCommand(sim, BoardCardKind.Recipe, "medkit", BoardTarget.Book());
            AssertNoCommand(sim, BoardCardKind.Recipe, "medkit", BoardTarget.Store());
            AssertNoCommand(sim, BoardCardKind.Recipe, "yok_tarif", BoardTarget.Order());

            // Otomatik üretimsiz (eski) kayıtta da kuyruk çalışır (Core AutomaticProduction'ı denetlemez)
            var manual = Ready();
            AssertCommand(Drop(manual, BoardCardKind.Recipe, "emp_cell", BoardTarget.Order())[0], "board_queue", "emp_cell", null);

            // Üretici yoksa önizleme uyarır (amber), bırakma yine geçerli
            var idle = Retired();
            Assert.IsTrue(idle.AssignResident(Maker(idle).Id, "rest").Ok);
            var q = BoardPreviewText.For(BoardCardKind.Recipe, "medkit", BoardTarget.Order(), idle);
            Assert.AreEqual("İŞ EMRİ ← BAKIM KİTİ", q.Title);
            Assert.AreEqual("girdi: 1 kumaş + 1 kimyasal · üretici yok", q.Body);
            Assert.IsTrue(q.Warning);
            Assert.IsTrue(BoardDropRules.Valid(BoardCardKind.Recipe, "medkit", BoardTarget.Order(), idle));
        }

        [Test]
        public void Recipe_ToFilledOrder_IsCancelThenQueue_TwoRecords()
        {
            // Başlamamış emir: ücretsiz iptal, ayrılan girdi serbest kalır
            var sim = Retired();
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            Assert.IsTrue(sim.ReserveProductionMaterial("metal", 1).Ok);
            string oldId = sim.Board.Orders[0].Id;
            var plan = BoardDropRules.Plan(BoardCardKind.Recipe, "medkit", BoardTarget.Order(), sim);
            Assert.AreEqual(BoardDropAction.Replace, plan.Action);
            Assert.AreEqual("medkit", plan.Id);
            Assert.AreEqual(oldId, plan.Job);
            var p = BoardPreviewText.For(BoardCardKind.Recipe, "medkit", BoardTarget.Order(), sim);
            Assert.AreEqual("İŞ EMRİ ← BAKIM KİTİ", p.Title);
            StringAssert.EndsWith("başlamamış iş · ücretsiz iptal", p.Body);
            StringAssert.StartsWith("girdi: 1 kumaş + 1 kimyasal", p.Body);
            Assert.IsTrue(p.Warning, "dolu emre bırakma amber");

            var c = Drop(sim, BoardCardKind.Recipe, "medkit", BoardTarget.Order(), 2);
            AssertCommand(c[0], "board_cancel", oldId, null);
            AssertCommand(c[1], "board_queue", "medkit", null);
            Assert.AreEqual(1, sim.Board.Orders.Count, "tek emir kuralı");
            Assert.AreEqual("medkit", sim.Board.Orders[0].RecipeId);
            Assert.AreNotEqual(oldId, sim.Board.Orders[0].Id);
            Assert.AreEqual(0, BoardDropRules.ChipCount(sim.Board, "metal"), "eski emrin ayrılanı serbest");
            Assert.AreEqual(sim.B.boardStartMetal, sim.BoardMaterialFree("metal"));

            // Başlamış emir: harcanan girdi geri gelmez (uyarı); yine iki kayıt
            var started = Retired();
            QueueFull(started, "emp_cell");
            Close(started);
            Assert.IsTrue(started.Board.Orders.Count == 1 && started.Board.Orders[0].Started, "EMP hücresi iki vardiya sürer");
            string startedId = started.Board.Orders[0].Id;
            var s = BoardPreviewText.For(BoardCardKind.Recipe, "armor", BoardTarget.Order(), started);
            StringAssert.EndsWith("harcanan girdi geri gelmez", s.Body);
            Assert.IsTrue(s.Warning);
            int metalAfterPay = started.Board.Material("metal").Quantity;
            var d = Drop(started, BoardCardKind.Recipe, "armor", BoardTarget.Order(), 2);
            AssertCommand(d[0], "board_cancel", startedId, null);
            AssertCommand(d[1], "board_queue", "armor", null);
            Assert.AreEqual(1, d[0].day);
            Assert.AreEqual(metalAfterPay, started.Board.Material("metal").Quantity, "ödenen girdi stoka dönmez");

            // Kuyruğu reddedilen değiştirme hiçbir şey yazmaz (tek komut işlevi: iptal reddedilirse kuyruk denenmez)
            var gone = Retired();
            Assert.IsTrue(gone.QueueProduction("armor").Ok);
            var replace = new BoardDropPlan(BoardDropAction.Replace, "medkit", "is-999");
            Assert.AreEqual("Böyle bir iş emri yok.", BoardDropRules.Reason(replace, gone));
            string before = State(gone);
            var r = replace.Command()(gone);
            Assert.IsFalse(r.Ok);
            Assert.AreEqual("Böyle bir iş emri yok.", r.Msg);
            Assert.AreEqual(before, State(gone));
        }

        // ================================================================ kurallar: formdaki tarif kartı

        [Test]
        public void RecipeCard_MovedOnOrder_IsLayoutOnly()
        {
            var sim = Retired();
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            string card = sim.Board.Orders[0].Id;   // formdaki kartın kimliği = iş emri
            string before = State(sim);
            Assert.IsTrue(BoardDropRules.IsLayoutOnly(BoardCardKind.Recipe, card, BoardTarget.Order(), sim));
            Assert.IsTrue(BoardDropRules.Valid(BoardCardKind.Recipe, card, BoardTarget.Order(), sim));
            Assert.IsNull(BoardDropRules.CommandFor(BoardCardKind.Recipe, card, BoardTarget.Order(), sim));
            var p = BoardPreviewText.For(BoardCardKind.Recipe, card, BoardTarget.Order(), sim);
            Assert.AreEqual("İŞ EMRİ ← ZIRH PLAKASI", p.Title);
            Assert.IsNull(p.Body);
            Assert.IsTrue(p.Valid);
            Assert.AreEqual(before, State(sim), "yerleşim komut yazmaz");

            // Başka kart türleri ve kopya yerleşim değildir; bilinmeyen emir de
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(BoardCardKind.Recipe, "armor", BoardTarget.Order(), sim));
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(BoardCardKind.Recipe, "is-999", BoardTarget.Order(), sim));
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(BoardCardKind.Recipe, card, BoardTarget.Book(), sim));
            Assert.AreEqual("ZIRH PLAKASI", DeskText.Upper(BoardDropRules.RecipeOf(sim, card).Name));
            Assert.AreEqual("armor", BoardDropRules.RecipeOf(sim, card).Id);
            Assert.AreEqual("medkit", BoardDropRules.RecipeOf(sim, "medkit").Id);
            Assert.IsNull(BoardDropRules.RecipeOf(sim, "yok"));

            // Bırakılan poz iş emrinin içine kırpılır: kart iş emri alanından taşmaz
            var outside = new Vector2(BoardLayout.Order.xMax + 40, BoardLayout.Order.yMin - 40);
            var clamped = BoardLayout.ClampRecipe(outside);
            var half = BoardLayout.RecipeSizeMm * 0.5f;
            Assert.LessOrEqual(clamped.x + half.x, BoardLayout.Order.xMax + 1e-3f);
            Assert.GreaterOrEqual(clamped.y - half.y, BoardLayout.Order.yMin - 1e-3f);
        }

        [Test]
        public void RecipeCard_OffOrder_IsCancel()
        {
            var sim = Retired();
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            Assert.IsTrue(sim.ReserveProductionMaterial("metal", 2).Ok);
            string card = sim.Board.Orders[0].Id;
            var plan = BoardDropRules.Plan(BoardCardKind.Recipe, card, BoardTarget.Book(), sim);
            Assert.AreEqual(BoardDropAction.Cancel, plan.Action);
            Assert.AreEqual(card, plan.Job);
            var p = BoardPreviewText.For(BoardCardKind.Recipe, card, BoardTarget.Book(), sim);
            Assert.AreEqual("İŞİ İPTAL ET", p.Title);
            Assert.AreEqual("başlamamış iş · ücretsiz iptal", p.Body);
            Assert.IsFalse(p.Warning);

            AssertCommand(Drop(sim, BoardCardKind.Recipe, card, BoardTarget.Book())[0], "board_cancel", card, null);
            Assert.AreEqual(0, sim.Board.Orders.Count);
            Assert.AreEqual(sim.B.boardStartMetal, sim.BoardMaterialFree("metal"), "ayrılan girdiler serbest");
            // Emir gittikten sonra eski kimlikle hiçbir yol komut yazmaz
            AssertNoCommand(sim, BoardCardKind.Recipe, card, BoardTarget.Book());
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(BoardCardKind.Recipe, card, BoardTarget.Order(), sim));

            // Formdaki kart deftere dışında hiçbir yere komut yazmaz
            var other = Retired();
            Assert.IsTrue(other.QueueProduction("medkit").Ok);
            string id = other.Board.Orders[0].Id;
            foreach (var t in new[] { BoardTarget.Store(), BoardTarget.Hurda(), BoardTarget.Pocket("craft"), BoardTarget.Bosta(), BoardTarget.None })
                AssertNoCommand(other, BoardCardKind.Recipe, id, t);

            // Başlamış emrin iptali: uyarı "harcanan girdi geri gelmez"
            var started = Retired();
            QueueFull(started, "emp_cell");
            Close(started);
            string sid = started.Board.Orders[0].Id;
            var s = BoardPreviewText.For(BoardCardKind.Recipe, sid, BoardTarget.Book(), started);
            Assert.AreEqual("İŞİ İPTAL ET", s.Title);
            Assert.AreEqual("harcanan girdi geri gelmez", s.Body);
            Assert.IsTrue(s.Warning);
            AssertCommand(Drop(started, BoardCardKind.Recipe, sid, BoardTarget.Book())[0], "board_cancel", sid, null);
        }

        [Test]
        public void Locked_EveryRecipeAndChipPath_NoCommand()
        {
            var sim = Retired();
            QueueFull(sim, "armor");
            string card = sim.Board.Orders[0].Id;
            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            Assert.IsTrue(sim.Board.Locked);
            string chip = BoardUiState.ChipId("metal", 0);
            Assert.AreEqual(2, BoardDropRules.ChipCount(sim.Board, "metal"), "fişler kilitli planda da çizilir");

            AssertRefused(sim, BoardCardKind.Recipe, "medkit", BoardTarget.Order(), LockedMessage);   // değiştirme
            AssertRefused(sim, BoardCardKind.Recipe, card, BoardTarget.Book(), LockedMessage);       // iptal
            AssertRefused(sim, BoardCardKind.Chip, chip, BoardTarget.Store(), LockedMessage);
            AssertRefused(sim, BoardCardKind.Material, "metal", BoardTarget.Order(), LockedMessage);
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(BoardCardKind.Recipe, card, BoardTarget.Order(), sim), "kâğıt üstünde de oynamaz");
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(BoardCardKind.Chip, chip, BoardTarget.Order(), sim));
            Assert.IsFalse(BoardDropRules.Valid(BoardCardKind.Recipe, card, BoardTarget.Order(), sim));
            foreach (var kind in new[] { BoardCardKind.Material, BoardCardKind.Chip, BoardCardKind.Recipe })
                Assert.AreEqual(LockedMessage, BoardDropRules.CanPick(kind, kind == BoardCardKind.Chip ? chip : kind == BoardCardKind.Recipe ? card : "metal", sim));

            // Boş emirli kilitli plan: kuyruk da reddedilir
            var empty = Retired();
            Assert.IsTrue(empty.PrintShift().Ok, empty.BoardPlanCheck());
            AssertRefused(empty, BoardCardKind.Recipe, "kit_tek", BoardTarget.Order(), LockedMessage);

            // Baskın sürerken
            var raid = Ready();
            QueueFull(raid, "armor");   // eksiksiz emir: plan denetimi baskın emrini engellemesin
            string rid = raid.Board.Orders[0].Id;
            Assert.IsTrue(raid.StartLiberationRaid("weu_1").Ok);
            AssertRefused(raid, BoardCardKind.Recipe, "medkit", BoardTarget.Order(), "Baskın sürerken plan değiştirilemez.");
            AssertRefused(raid, BoardCardKind.Recipe, rid, BoardTarget.Book(), "Baskın sürerken plan değiştirilemez.");
            AssertRefused(raid, BoardCardKind.Material, "metal", BoardTarget.Order(), "Baskın sürerken plan değiştirilemez.");
        }

        [Test]
        public void Valid_PredictsTheCoreResult_ForEveryRecipeAndMaterialPath()
        {
            // Her durumda (emir yok · başlamamış · başlamış) her kopya, formdaki kart, stok kartı ve fiş için Valid = Core sonucu
            System.Func<Simulation>[] states =
            {
                () => Retired(),
                () => { var s = Retired(); Assert.IsTrue(s.QueueProduction("armor").Ok); Assert.IsTrue(s.ReserveProductionMaterial("metal", 1).Ok); return s; },
                () => { var s = Retired(); QueueFull(s, "emp_cell"); Close(s); return s; },
            };
            var recipeIds = Retired().BoardRecipes.Select(r => r.Id).ToArray();
            var targets = new[] { BoardTarget.Order(), BoardTarget.Book(), BoardTarget.Store(), BoardTarget.Hurda() };
            int accepted = 0, refused = 0;
            foreach (var fresh in states)
            {
                var sim = fresh();
                var gestures = new List<(BoardCardKind kind, string id)>();
                gestures.AddRange(recipeIds.Select(id => (BoardCardKind.Recipe, id)));
                gestures.AddRange(sim.Board.Orders.Select(o => (BoardCardKind.Recipe, o.Id)));
                gestures.AddRange(PlanningBoardData.MaterialKinds.Select(k => (BoardCardKind.Material, k)));
                gestures.AddRange(PlanningBoardData.MaterialKinds.SelectMany(k => new[] { (BoardCardKind.Chip, BoardUiState.ChipId(k, 0)), (BoardCardKind.Chip, BoardUiState.ChipId(k, 1)) }));
                foreach (var g in gestures)
                foreach (var t in targets)
                {
                    string label = g.kind + " " + g.id + " → " + t;
                    bool valid = BoardDropRules.Valid(g.kind, g.id, t, sim);
                    if (BoardDropRules.IsLayoutOnly(g.kind, g.id, t, sim))
                    {
                        Assert.IsTrue(valid, label);
                        Assert.IsNull(BoardDropRules.CommandFor(g.kind, g.id, t, sim), label);
                        continue;
                    }
                    var plan = BoardDropRules.Plan(g.kind, g.id, t, sim);
                    var command = plan.Command();
                    if (command == null) { Assert.IsFalse(valid, label + ": komutsuz bırakma geçerli sayılmaz."); continue; }
                    string reason = BoardDropRules.Reason(plan, sim);
                    int before = sim.Record.commands.Count;
                    uint rng = sim.S.Rng.State;
                    var result = command(sim);
                    Assert.AreEqual(valid, result.Ok, label + ": " + result.Msg);
                    Assert.AreEqual(rng, sim.S.Rng.State, label);
                    if (result.Ok)
                    {
                        Assert.IsNull(reason, label);
                        int records = plan.Action == BoardDropAction.Replace ? 2 : 1;
                        Assert.AreEqual(before + records, sim.Record.commands.Count, label);
                        accepted++;
                        sim = fresh();
                    }
                    else
                    {
                        Assert.AreEqual(reason, result.Msg, label);
                        Assert.AreEqual(before, sim.Record.commands.Count, label);
                        refused++;
                    }
                }
            }
            Assert.Greater(accepted, 0);
            Assert.Greater(refused, 0);
        }

        // ================================================================ denetçi: edit modda küçük pano

        sealed class Rig
        {
            public Transform Model;
            public Camera Cam;
            public BoardDragController Drag;
            public readonly List<BoardSound> Sounds = new List<BoardSound>();
            public readonly List<string> Flashed = new List<string>();
            public readonly List<string> Denied = new List<string>();
            public readonly List<bool> DragChanges = new List<bool>();
            public readonly List<BoardTarget> Requested = new List<BoardTarget>();
            public int Background, Empty, Clicked;

            /// <summary>Pano mm'sinin (yüz düzleminde) ekran noktası.</summary>
            public Vector2 Screen(Vector2 mm)
            {
                var world = Model.TransformPoint(BoardLayout.MmToLocal(mm, BoardLayout.FaceZ));
                var sp = Cam.WorldToScreenPoint(world);
                return new Vector2(sp.x, sp.y);
            }
        }

        T Track<T>(T o) where T : Object { created.Add(o); return o; }

        /// <summary>
        /// Pano kökü (model çerçevesi = kök), 1920 × 1080 hedefli kamera (panonun ortasına 1 m'den bakar, kapalı: çizim yok) ve
        /// denetçi. Oda yok (rig null), pano görünümü yok (anlık görüntü null), etiket yok.
        /// </summary>
        Rig MakeRig(HurdaBin hurda = null)
        {
            var root = Track(new GameObject("Pano (test)"));
            var camGo = Track(new GameObject("Kamera (test)"));
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = 40;
            cam.nearClipPlane = 0.05f;
            cam.targetTexture = Track(new RenderTexture(1920, 1080, 16));
            var centre = BoardLayout.MmToLocal(new Vector2(BoardLayout.InnerWidthMm / 2, BoardLayout.InnerHeightMm / 2), BoardLayout.FaceZ);
            camGo.transform.SetPositionAndRotation(new Vector3(centre.x, centre.y, -1.2f), Quaternion.identity);

            var drag = root.AddComponent<BoardDragController>();
            drag.Configure(cam, root.transform, null, null, null, hurda, null);
            drag.ReadKeys = false;
            var rig = new Rig { Model = root.transform, Cam = cam, Drag = drag };
            drag.Sound += s => rig.Sounds.Add(s);
            drag.StockFlashed += k => rig.Flashed.Add(k);
            drag.PickDenied += (c, why) => rig.Denied.Add(why);
            drag.DragChanged += on => rig.DragChanges.Add(on);
            drag.BackgroundClicked += () => rig.Background++;
            drag.EmptyClicked += () => rig.Empty++;
            drag.CardClicked += c => rig.Clicked++;
            drag.DropRequested += (c, t) => rig.Requested.Add(t);
            return rig;
        }

        /// <summary>Panoda bir kart (kök BoxCollider 0,112 × 0,08 × 0,004, kartın türü ve çarpıştırıcısı seri alandan).</summary>
        BoardCardView MakeCard(Rig rig, BoardCardKind kind, string id, Vector2 mm, float level = 0)
        {
            var go = Track(new GameObject(kind + " " + id));
            go.transform.SetParent(rig.Model, false);
            var box = go.AddComponent<BoxCollider>();
            var size = kind == BoardCardKind.Material ? new Vector2(80, 56) : kind == BoardCardKind.Chip ? new Vector2(56, 40) : new Vector2(112, 80);
            box.size = new Vector3(size.x * 0.001f, size.y * 0.001f, 0.004f);
            var card = go.AddComponent<BoardCardView>();
            var so = new SerializedObject(card);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("hit").objectReferenceValue = box;
            so.ApplyModifiedPropertiesWithoutUndo();
            switch (kind)
            {
                case BoardCardKind.Material: card.Bind(new MaterialCardData { Kind = id, Name = id }); break;
                case BoardCardKind.Chip: card.Bind(new ChipData { Id = id, Kind = "metal", Name = "METAL" }); break;
                case BoardCardKind.Recipe: card.Bind(new RecipeCardData { Id = id, RecipeId = id, Name = id }); break;
                default: Assert.Fail("test kartı: " + kind); break;
            }
            Assert.AreEqual(id, card.Id);
            card.Controller = rig.Drag;
            card.Place(BoardLayout.MmToLocal(mm, BoardLayout.CardZ(level)), 0);
            return card;
        }

        static PointerEventData Pointer(Vector2 press, Vector2 at, PointerEventData.InputButton button = PointerEventData.InputButton.Left) =>
            new PointerEventData(null) { button = button, pressPosition = press, position = at };

        static Vector2 OrderCentre => BoardLayout.Order.center;
        static Vector2 PocketCentre => BoardLayout.Pocket("tek").center;

        // --- BoardCardView'in özel titreşim adımı (edit modda Update çalışmaz)

        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        static bool Shaking(BoardCardView c)
        {
            var f = typeof(BoardCardView).GetField("shaking", Private);
            Assert.IsNotNull(f, "BoardCardView.shaking (titreşim durumu) bulunamadı");
            return (bool)f.GetValue(c);
        }

        static void StepShake(BoardCardView c, float dt)
        {
            var step = typeof(BoardCardView).GetMethod("StepShake", Private);
            var apply = typeof(BoardCardView).GetMethod("Apply", Private);
            Assert.IsNotNull(step, "BoardCardView.StepShake bulunamadı");
            Assert.IsNotNull(apply, "BoardCardView.Apply bulunamadı");
            step.Invoke(c, new object[] { dt });
            apply.Invoke(c, null);
        }

        static void InvokeOnDisable(BoardDragController d)
        {
            var m = typeof(BoardDragController).GetMethod("OnDisable", Private);
            Assert.IsNotNull(m);
            m.Invoke(d, null);
        }

        // ================================================================ denetçi: alma reddi

        [Test]
        public void DenyFeedback_ShakeTiming()
        {
            var rig = MakeRig();
            var card = MakeCard(rig, BoardCardKind.Material, "kimyasal", BoardLayout.Material(3));
            rig.Drag.CanPick = c => c.Kind == BoardCardKind.Material && c.Id == "kimyasal" ? Loc.T(Insufficient) : null;
            var home = card.transform.localPosition;
            var press = rig.Screen(BoardLayout.Material(3));

            rig.Drag.OnCardPointerDown(card, Pointer(press, press));
            Assert.IsFalse(Shaking(card), "basış tık olabilir: titremez");
            Assert.IsEmpty(rig.Sounds);
            rig.Drag.OnCardBeginDrag(card, Pointer(press, press + new Vector2(40, 30)));

            // Kart kalkmaz, tutulmaz, sahiplenilmez; tek "tok", tek ret etiketi, kırmızı yanar
            Assert.IsNull(rig.Drag.Held);
            Assert.IsFalse(rig.Drag.Busy);
            Assert.IsFalse(rig.Drag.Owns(card));
            Assert.AreEqual(0f, card.LiftMm);
            Assert.AreEqual(home, card.Target);
            CollectionAssert.AreEqual(new[] { BoardSound.Deny }, rig.Sounds);
            CollectionAssert.AreEqual(new[] { Insufficient }, rig.Denied);
            CollectionAssert.AreEqual(new[] { "kimyasal" }, rig.Flashed);
            CollectionAssert.IsEmpty(rig.DragChanges, "tutma başlamadı");
            CollectionAssert.IsEmpty(rig.Requested);
            Assert.IsTrue(card.Collider.enabled, "çarpıştırıcı açık kalır");

            // Titreşim: yanal x, tepe ≤ 2 mm ve > 1 mm, denyShakeS içinde yuvaya döner
            Assert.AreEqual(2f, BoardDragController.DenyShakeMm, 1e-4f);
            float seconds = BoardDragController.DenyShakeS;
            Assert.That(seconds, Is.EqualTo(0.18f).Within(0.02f), "temada yok, yedek 0,18 s");
            Assert.IsTrue(Shaking(card));
            const float dt = 1f / 240f;
            float elapsed = 0, peak = 0;
            while (Shaking(card) && elapsed < 1f)
            {
                StepShake(card, dt);
                elapsed += dt;
                var p = card.transform.localPosition;
                peak = Mathf.Max(peak, Mathf.Abs(p.x - home.x) * 1000f);
                Assert.AreEqual(home.y, p.y, 1e-6f, "yalnız yanal");
                Assert.AreEqual(home.z, p.z, 1e-6f, "kalkış yok");
            }
            Assert.That(elapsed, Is.EqualTo(seconds).Within(2 * dt));
            Assert.LessOrEqual(peak, 2f + 1e-3f);
            Assert.Greater(peak, 1f);
            Assert.AreEqual(home, card.transform.localPosition, "sonunda yuvada");

            // Bırakma yoluna ret gelmez: sonraki sürükleme denemesi yine reddedilir (kart hiç alınmadı)
            rig.Drag.OnCardBeginDrag(card, Pointer(press, press + new Vector2(40, 30)));
            Assert.IsNull(rig.Drag.Held);
            Assert.AreEqual(2, rig.Denied.Count);
        }

        [Test]
        public void DenyFlash_OnlyForInsufficientStock()
        {
            string insufficient = Loc.T(Insufficient);
            string wrongKind = Loc.T("Bu tarif için uygun tür ve miktarı ayır.");
            string noOrder = Loc.T("Girdisi ayrılabilen bir taslak iş yok.");

            // Yüklem: yalnız hammadde kartı ve yalnız "Serbest hammadde yetersiz."
            Assert.IsTrue(BoardDragController.FlashesStock(BoardCardKind.Material, insufficient));
            Assert.IsFalse(BoardDragController.FlashesStock(BoardCardKind.Material, wrongKind));
            Assert.IsFalse(BoardDragController.FlashesStock(BoardCardKind.Material, noOrder));
            Assert.IsFalse(BoardDragController.FlashesStock(BoardCardKind.Material, Loc.T(LockedMessage)));
            Assert.IsFalse(BoardDragController.FlashesStock(BoardCardKind.Material, Loc.T("Serbest hammadde yetersiz veya miktar aynı.")));
            Assert.IsFalse(BoardDragController.FlashesStock(BoardCardKind.Material, null));
            Assert.IsFalse(BoardDragController.FlashesStock(BoardCardKind.Chip, insufficient));
            Assert.IsFalse(BoardDragController.FlashesStock(BoardCardKind.Recipe, insufficient));

            var rig = MakeRig();
            var press = rig.Screen(BoardLayout.Material(0));
            var overOrder = rig.Screen(OrderCentre);

            // 1. Alma reddi başka nedenle: titrer ve "tok", kırmızı yok
            var a = MakeCard(rig, BoardCardKind.Material, "metal", BoardLayout.Material(0));
            rig.Drag.CanPick = c => c == a ? wrongKind : null;
            rig.Drag.OnCardBeginDrag(a, Pointer(press, press + new Vector2(40, 30)));
            Assert.IsNull(rig.Drag.Held);
            Assert.IsTrue(Shaking(a));
            CollectionAssert.AreEqual(new[] { BoardSound.Deny }, rig.Sounds);
            CollectionAssert.IsEmpty(rig.Flashed);
            rig.Drag.CanPick = null;

            // 2. Geçersiz bırakış (komutsuz): nedeni yetersiz stoksa kırmızı, değilse yok
            string body = null;
            rig.Drag.CanDrop = (c, t) => false;
            rig.Drag.Preview = (c, t) => new DropPreview("İŞ EMRİ ← METAL", body, true, false);
            foreach (var (reason, flashes, kind) in new[] { (wrongKind, false, "devre"), (insufficient, true, "kumas"), (noOrder, false, "kimyasal") })
            {
                body = reason;
                rig.Flashed.Clear();
                var c = MakeCard(rig, BoardCardKind.Material, kind, BoardLayout.Material(1));
                var from = rig.Screen(BoardLayout.Material(1));
                rig.Drag.OnCardBeginDrag(c, Pointer(from, from + new Vector2(40, 30)));
                Assert.AreEqual(c, rig.Drag.Held, kind + " tutuldu");
                rig.Drag.OnCardDrag(c, Pointer(from, overOrder));
                rig.Drag.OnCardEndDrag(c, Pointer(from, overOrder));
                Assert.IsNull(rig.Drag.Held);
                Assert.AreEqual(flashes ? 1 : 0, rig.Flashed.Count, reason);
                if (flashes) Assert.AreEqual(kind, rig.Flashed[0]);
                CollectionAssert.IsEmpty(rig.Requested, "geçersiz bırakış komut istemez");
            }

            // 3. Core reddi (Resolve(false, neden)): aynı kural
            rig.Drag.CanDrop = null;
            rig.Drag.Preview = null;
            foreach (var (reason, flashes, kind) in new[] { (insufficient, true, "metal"), (wrongKind, false, "devre") })
            {
                rig.Flashed.Clear();
                rig.Requested.Clear();
                var c = MakeCard(rig, BoardCardKind.Material, kind, BoardLayout.Material(2));
                var from = rig.Screen(BoardLayout.Material(2));
                rig.Drag.OnCardBeginDrag(c, Pointer(from, from + new Vector2(40, 30)));
                rig.Drag.OnCardEndDrag(c, Pointer(from, overOrder));
                Assert.AreEqual(1, rig.Requested.Count);
                Assert.AreEqual(BoardTargetKind.Order, rig.Requested[0].Kind);
                rig.Drag.Resolve(false, reason);
                Assert.AreEqual(flashes ? 1 : 0, rig.Flashed.Count, reason);
            }

            // 4. Fiş ve tarif kartı hiçbir nedenle kırmızı yakmaz
            rig.Flashed.Clear();
            var chip = MakeCard(rig, BoardCardKind.Chip, BoardUiState.ChipId("metal", 0), new Vector2(612, 314));
            rig.Drag.CanPick = c => insufficient;
            var chipAt = rig.Screen(new Vector2(612, 314));
            rig.Drag.OnCardBeginDrag(chip, Pointer(chipAt, chipAt + new Vector2(40, 30)));
            Assert.IsNull(rig.Drag.Held);
            Assert.IsTrue(Shaking(chip));
            CollectionAssert.IsEmpty(rig.Flashed);
        }

        // ================================================================ denetçi: defter kipi

        [Test]
        public void Suspended_ClickRaisesBackgroundClicked_EvenWhenLocked()
        {
            var rig = MakeRig();
            var card = MakeCard(rig, BoardCardKind.Material, "metal", BoardLayout.Material(0));
            var at = rig.Screen(new Vector2(700, 300));
            var cardAt = rig.Screen(BoardLayout.Material(0));
            rig.Drag.Editable = false;
            rig.Drag.Suspended = true;
            Assert.IsTrue(rig.Drag.Suspended);

            // Pano yüzü, kart ve sabit hedef tıkı: hepsi defteri kapatır (kilitli planda da), seçim yok
            rig.Drag.OnPointerClick(Pointer(at, at));
            Assert.AreEqual(1, rig.Background);
            rig.Drag.OnCardClick(card, Pointer(cardAt, cardAt));
            Assert.AreEqual(2, rig.Background);
            rig.Drag.ClickTarget(BoardTarget.Store());
            Assert.AreEqual(3, rig.Background);
            Assert.AreEqual(0, rig.Clicked);
            Assert.AreEqual(0, rig.Empty);

            // Sürüklemeli bırakış, sağ tık ve klavye tık sayılmaz
            rig.Drag.OnPointerClick(Pointer(at, at + new Vector2(80, 0)));
            rig.Drag.OnPointerClick(Pointer(at, at, PointerEventData.InputButton.Right));
            rig.Drag.StepKeyboardTarget(1);
            rig.Drag.ActivateKeyboardTarget();
            Assert.AreEqual(3, rig.Background);

            // Kart girdisi yok: basış titretmez, sürükleme tutmaz, üzerine gelme kaldırmaz
            rig.Drag.OnCardPointerDown(card, Pointer(cardAt, cardAt));
            Assert.IsFalse(Shaking(card), "defter kipinde kilitli titreşim yok");
            rig.Drag.Editable = true;
            rig.Drag.OnCardPointerEnter(card, Pointer(cardAt, cardAt));
            rig.Drag.OnCardBeginDrag(card, Pointer(cardAt, cardAt + new Vector2(40, 30)));
            Assert.IsNull(rig.Drag.Held);
            CollectionAssert.IsEmpty(rig.Sounds);
            CollectionAssert.IsEmpty(rig.DragChanges);

            // Kip kalkınca eski davranış: kilitli planda boş tık sayılmaz, açık planda EmptyClicked
            rig.Drag.Suspended = false;
            rig.Drag.Editable = false;
            rig.Drag.OnPointerClick(Pointer(at, at));
            Assert.AreEqual(3, rig.Background);
            Assert.AreEqual(0, rig.Empty);
            rig.Drag.Editable = true;
            rig.Drag.OnPointerClick(Pointer(at, at));
            Assert.AreEqual(3, rig.Background);
            Assert.AreEqual(1, rig.Empty);

            // Tutulan kart kip açılınca yayla yerine döner (komut yok)
            var held = MakeCard(rig, BoardCardKind.Material, "devre", BoardLayout.Material(1));
            var from = rig.Screen(BoardLayout.Material(1));
            rig.Drag.OnCardBeginDrag(held, Pointer(from, from + new Vector2(40, 30)));
            Assert.AreEqual(held, rig.Drag.Held);
            rig.Drag.Suspended = true;
            Assert.IsNull(rig.Drag.Held);
            CollectionAssert.AreEqual(new[] { true, false }, rig.DragChanges);
            CollectionAssert.IsEmpty(rig.Requested);
        }

        // ================================================================ denetçi: tarif kartının hedefi

        [Test]
        public void ColliderTarget_Recipe_IsNone()
        {
            Assert.IsFalse(BoardDragController.UsesColliderTargets(BoardCardKind.Recipe));
            Assert.IsTrue(BoardDragController.UsesColliderTargets(BoardCardKind.Material));
            Assert.IsTrue(BoardDragController.UsesColliderTargets(BoardCardKind.Item));

            // Hurda ön yüzü iş emrinin önünde dursa bile tarif kartı yüz düzleminden çözülür
            var bin = Track(new GameObject("Hurda (test)"));
            var front = bin.AddComponent<BoxCollider>();
            front.size = new Vector3(0.3f, 0.3f, 0.01f);
            var hurda = bin.AddComponent<HurdaBin>();
            var so = new SerializedObject(hurda);
            so.FindProperty("front").objectReferenceValue = front;
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreEqual(front, hurda.Front);

            var rig = MakeRig(hurda);
            bin.transform.SetParent(rig.Model, false);
            bin.transform.localPosition = BoardLayout.MmToLocal(OrderCentre, BoardLayout.FaceZ - 0.05f);

            var recipe = MakeCard(rig, BoardCardKind.Recipe, "armor", BoardLayout.Book);
            var material = MakeCard(rig, BoardCardKind.Material, "metal", BoardLayout.Material(0));
            Physics.SyncTransforms();
            var overOrder = rig.Screen(OrderCentre);
            Assert.AreEqual(BoardTargetKind.Hurda, rig.Drag.DropTargetAt(material, overOrder).Kind, "çarpıştırıcı önce (sahne kurulumu doğru)");
            Assert.AreEqual(BoardTarget.Order(), rig.Drag.DropTargetAt(recipe, overOrder), "tarif: çarpıştırıcı hedefi yok");
            Assert.AreEqual(BoardTarget.Book(), rig.Drag.DropTargetAt(recipe, rig.Screen(PocketCentre)), "iş emri dışı: deftere");
            Assert.AreEqual(BoardTarget.Book(), rig.Drag.DropTargetAt(recipe, rig.Screen(BoardLayout.Depot.center)));
            Assert.AreEqual(BoardTarget.None, rig.Drag.DropTargetAt(null, overOrder));
        }

        // ================================================================ denetçi: defterden kopya (Adopt)

        [Test]
        public void Adopt_DropOnOrder_SettlesWhereReleased_FinishedTrue()
        {
            var rig = MakeRig();
            var copy = MakeCard(rig, BoardCardKind.Recipe, "armor", BoardLayout.Book);
            copy.SetInteractive(false);
            copy.SetVisible(false);
            var results = new List<bool>();
            var ended = new List<(BoardCardView, bool)>();
            rig.Drag.AdoptEnded += (c, ok) => ended.Add((c, ok));
            rig.Drag.CanDrop = (c, t) => t.Kind == BoardTargetKind.Order;
            rig.Drag.DropRequested += (c, t) => rig.Drag.Resolve(true);

            var press = rig.Screen(BoardLayout.Book);
            var e = Pointer(press, press + new Vector2(30, 20));
            rig.Drag.Adopt(copy, e, () => new Pose(rig.Model.TransformPoint(BoardLayout.MmToLocal(BoardLayout.Book, BoardLayout.CardZ(0))), rig.Model.rotation),
                ok => results.Add(ok));

            Assert.AreEqual(copy, rig.Drag.Held);
            Assert.AreEqual(copy, rig.Drag.Adopted);
            Assert.IsTrue(copy.gameObject.activeSelf, "kopya görünür");
            Assert.AreEqual(copy.gameObject, e.pointerDrag, "sonraki sürükleme olayları karta");
            Assert.AreEqual(copy.Controller, rig.Drag);
            Assert.That(rig.Drag.HeldTiltDeg, Is.EqualTo(-6f).Within(1e-4f), "görselde saat yönünde 6°");
            CollectionAssert.AreEqual(new[] { BoardSound.RecipeLift }, rig.Sounds, "şırp");
            CollectionAssert.AreEqual(new[] { true }, rig.DragChanges);
            Assert.IsFalse(copy.MagnetVisible, "elde mıknatıs gizli");
            Assert.AreEqual(BoardLayout.CardZ(BoardLayout.RecipeLevel), copy.Target.z, 1e-6f, "pano düzleminde tutulur");
            CollectionAssert.IsEmpty(results);

            // Benimsenmişken defter kipi kart girdisini kesmez
            rig.Drag.Suspended = true;
            Assert.AreEqual(copy, rig.Drag.Held);
            rig.Drag.Suspended = false;

            var drop = OrderCentre + new Vector2(20, 10);
            var at = rig.Screen(drop);
            rig.Drag.OnCardDrag(copy, Pointer(press, at));
            Assert.AreEqual(BoardTarget.Order(), rig.Drag.HoverTarget);
            rig.Drag.OnCardEndDrag(copy, Pointer(press, at));

            CollectionAssert.AreEqual(new[] { BoardTarget.Order() }, rig.Requested);
            CollectionAssert.AreEqual(new[] { true }, results, "kabul: defter kancaya kapanır (B8)");
            Assert.IsNull(rig.Drag.Adopted);
            Assert.IsNull(rig.Drag.Held);
            CollectionAssert.AreEqual(new[] { true, false }, rig.DragChanges);
            var pose = rig.Drag.LastDropPose;
            Assert.IsTrue(BoardLayout.RecipeOnOrder.Contains(new Vector2(pose.x, pose.y)), "poz iş emrinin içinde");
            Assert.That(Vector2.Distance(new Vector2(pose.x, pose.y), drop), Is.LessThan(3f), "bırakıldığı serbest konum (tutma düzlemi 11 mm önde: küçük paralaks)");
            var target = BoardLayout.MmToLocal(new Vector2(pose.x, pose.y), BoardLayout.CardZ(BoardLayout.RecipeLevel));
            Assert.That(Vector3.Distance(copy.Target, target), Is.LessThan(1e-4f), "oturma hedefi bırakılan poz");
            Assert.IsTrue(rig.Drag.Owns(copy), "oturma bitene dek denetçinin");

            // Hareketin sonu (edit modda Update yok; OnDisable süren hareketleri sonuna götürür): AdoptEnded kabulle, mıknatıs görünür
            InvokeOnDisable(rig.Drag);
            Assert.AreEqual(1, ended.Count);
            Assert.AreEqual(copy, ended[0].Item1);
            Assert.IsTrue(ended[0].Item2);
            Assert.IsTrue(copy.gameObject.activeSelf, "kabul edilen kopya panoda kalır");
            Assert.IsFalse(rig.Drag.Owns(copy));
            CollectionAssert.AreEqual(new[] { true }, results, "finished bir kez");
        }

        [Test]
        public void Adopt_DropOffOrder_ReturnsToThePage_NoCommand()
        {
            var rig = MakeRig();
            var copy = MakeCard(rig, BoardCardKind.Recipe, "medkit", BoardLayout.Book);
            copy.SetInteractive(false);
            copy.SetVisible(false);
            var results = new List<bool>();
            var ended = new List<(BoardCardView, bool)>();
            rig.Drag.AdoptEnded += (c, ok) => ended.Add((c, ok));
            rig.Drag.CanDrop = (c, t) => t.Kind == BoardTargetKind.Order;
            var pageWorld = rig.Model.TransformPoint(BoardLayout.MmToLocal(BoardLayout.Book + new Vector2(5, -3), BoardLayout.CardZ(0)));
            var pageRot = rig.Model.rotation * Quaternion.Euler(0, 0, 4);

            var press = rig.Screen(BoardLayout.Book);
            rig.Drag.Adopt(copy, Pointer(press, press + new Vector2(30, 20)), () => new Pose(pageWorld, pageRot), ok => results.Add(ok));
            var away = rig.Screen(PocketCentre);
            rig.Drag.OnCardDrag(copy, Pointer(press, away));
            Assert.AreEqual(BoardTarget.Book(), rig.Drag.HoverTarget);
            rig.Drag.OnCardEndDrag(copy, Pointer(press, away));

            CollectionAssert.IsEmpty(rig.Requested, "deftere dönüş komutsuz");
            CollectionAssert.AreEqual(new[] { false }, results, "defter hemen öne gelir (B9)");
            Assert.IsNull(rig.Drag.Adopted);
            Assert.IsTrue(copy.Moving, "0,25 s kavis");
            var local = rig.Model.InverseTransformPoint(pageWorld);
            Assert.That(Vector3.Distance(copy.Target, local), Is.LessThan(1e-4f), "sayfadaki kutuya döner");
            Assert.AreEqual(4f, copy.TargetAngle, 1e-3f);

            InvokeOnDisable(rig.Drag);
            Assert.AreEqual(1, ended.Count);
            Assert.IsFalse(ended[0].Item2);
            Assert.IsFalse(copy.gameObject.activeSelf, "dönen kopya gizlenir");
        }

        [Test]
        public void Adopt_Refused_WhenLockedOrNotARecipe_CancelReturns()
        {
            var rig = MakeRig();
            var copy = MakeCard(rig, BoardCardKind.Recipe, "kit_tek", BoardLayout.Book);
            var press = rig.Screen(BoardLayout.Book);
            var results = new List<bool>();

            rig.Drag.Editable = false;
            rig.Drag.Adopt(copy, Pointer(press, press), () => new Pose(), ok => results.Add(ok));
            CollectionAssert.AreEqual(new[] { false }, results, "kilitli plan: benimsenmez");
            Assert.IsNull(rig.Drag.Held);
            rig.Drag.Editable = true;

            var material = MakeCard(rig, BoardCardKind.Material, "metal", BoardLayout.Material(0));
            rig.Drag.Adopt(material, Pointer(press, press), () => new Pose(), ok => results.Add(ok));
            CollectionAssert.AreEqual(new[] { false, false }, results, "yalnız tarif kartı");
            rig.Drag.Adopt(copy, null, () => new Pose(), ok => results.Add(ok));
            CollectionAssert.AreEqual(new[] { false, false, false }, results, "olaysız");
            CollectionAssert.IsEmpty(rig.Sounds);

            // Esc (Cancel) tutulan kopyayı sayfaya döndürür
            var page = rig.Model.TransformPoint(BoardLayout.MmToLocal(BoardLayout.Book, BoardLayout.CardZ(0)));
            rig.Drag.Adopt(copy, Pointer(press, press + new Vector2(30, 20)), () => new Pose(page, rig.Model.rotation), ok => results.Add(ok));
            Assert.AreEqual(copy, rig.Drag.Held);
            rig.Drag.Cancel();
            Assert.IsNull(rig.Drag.Held);
            CollectionAssert.AreEqual(new[] { false, false, false, false }, results);
            CollectionAssert.IsEmpty(rig.Requested);

            // Başka kart tutulurken benimseme olmaz
            InvokeOnDisable(rig.Drag);
            var other = MakeCard(rig, BoardCardKind.Material, "devre", BoardLayout.Material(1));
            var from = rig.Screen(BoardLayout.Material(1));
            rig.Drag.OnCardBeginDrag(other, Pointer(from, from + new Vector2(40, 30)));
            Assert.AreEqual(other, rig.Drag.Held);
            var second = MakeCard(rig, BoardCardKind.Recipe, "kit_ana", BoardLayout.Book);
            rig.Drag.Adopt(second, Pointer(press, press), () => new Pose(), ok => results.Add(ok));
            Assert.AreEqual(5, results.Count);
            Assert.IsFalse(results[4]);
            Assert.AreEqual(other, rig.Drag.Held);
        }
    }
}
