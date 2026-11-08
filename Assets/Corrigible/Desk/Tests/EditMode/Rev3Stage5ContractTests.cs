using System;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using Corrigible.Game.Audio;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 5 (R18) sözleşmesi (PLAN §3 W0): yeni enum değerleri sona eklenir (eski sıralar ve ses numaraları
    /// kaymaz), yeni bırakma eylemleri var olan Core komutlarını doğru bağımsız değişkenlerle yürütür (değiştirme tek bırakışta iki
    /// kayıt), yerel token anahtarları (<c>_local_a5</c>) temaya girer, sunum durumunun yeni alanları sıfırlanır.
    /// </summary>
    public sealed class Rev3Stage5ContractTests
    {
        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        // ---------------------------------------------------------------- enumlar

        [Test]
        public void Cues_115To119_AreTheBookAndProductionSounds()
        {
            Assert.AreEqual(115, (int)Cue.BoardBookOpen);
            Assert.AreEqual(116, (int)Cue.BoardPageTurn);
            Assert.AreEqual(117, (int)Cue.BoardBookClose);
            Assert.AreEqual(118, (int)Cue.BoardRecipeLift);
            Assert.AreEqual(119, (int)Cue.BoardProductSlide);
            Assert.AreEqual(114, (int)Cue.SlipMouthEmpty, "Aşama 4'ün sesleri yerinde");
            var twice = Enum.GetValues(typeof(Cue)).Cast<int>().GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            Assert.IsEmpty(twice, "bir ses numarası iki ada verilmiş");
        }

        [Test]
        public void BoardSound_NewValuesAppendedAfterFlap()
        {
            var expected = new[]
            {
                "Lift", "Settle", "Return", "Deny", "DeckSlide", "DeckCollect", "Tuck", "Hurda", "Padlock", "Flap",
                "BookOpen", "PageTurn", "BookClose", "RecipeLift", "ProductSlide",
            };
            CollectionAssert.AreEqual(expected, Enum.GetNames(typeof(BoardSound)));
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(i, (int)Enum.Parse(typeof(BoardSound), expected[i]), expected[i]);
        }

        [Test]
        public void BoardTargetKind_BookAppended()
        {
            Assert.AreEqual(11, (int)BoardTargetKind.Empty, "eski hedefler kaymaz");
            Assert.AreEqual(12, (int)BoardTargetKind.Book);
            var book = BoardTarget.Book();
            Assert.AreEqual(BoardTargetKind.Book, book.Kind);
            Assert.IsNull(book.Job);
            Assert.IsNull(book.Id);
            Assert.AreEqual(-1, book.Row);
            Assert.AreEqual(new BoardTarget(BoardTargetKind.Book), book);
        }

        [Test]
        public void BookTypes_Order()
        {
            CollectionAssert.AreEqual(new[] { "Closed", "Opening", "Open", "Turning", "Lowered", "Closing" }, Enum.GetNames(typeof(BookState)));
            CollectionAssert.AreEqual(new[] { "None", "Prev", "Next", "RecipeL", "RecipeR", "Page", "Outside" }, Enum.GetNames(typeof(BookZone)));
            CollectionAssert.AreEqual(new[] { "Empty", "Recipe", "Missing", "Full", "Started", "Stopped" }, Enum.GetNames(typeof(OrderState)));
        }

        [Test]
        public void DropAction_NewActionsAppended()
        {
            CollectionAssert.AreEqual(new[] { "None", "Assign", "Train", "Equip", "Scrap", "Stage", "Queue", "Reserve", "Cancel", "Replace" },
                Enum.GetNames(typeof(BoardDropAction)));
        }

        // ---------------------------------------------------------------- bırakma eylemlerinin komutları

        static Simulation Retired(uint seed = 79)
        {
            var sim = Simulation.NewGame(seed, new Balance
            {
                planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100
            });
            Assert.IsTrue(sim.EnableAutomaticProduction().Ok);
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                Assert.IsTrue(sim.AssignResident(p.Id, "rest").Ok);
            return sim;
        }

        /// <summary>Planın komutunu çalıştırır; başarılı olmalı ve tam <paramref name="records"/> kayıt yazmalı (aynı gün). Yeni kayıtları döner.</summary>
        static SavedCommand[] Run(Simulation sim, BoardDropPlan plan, int records)
        {
            var command = plan.Command();
            Assert.IsNotNull(command, plan.Action.ToString());
            int before = sim.Record.commands.Count;
            var result = command(sim);
            Assert.IsTrue(result.Ok, plan.Action + ": " + result.Msg);
            var written = sim.Record.commands.Skip(before).ToArray();
            Assert.AreEqual(records, written.Length, plan.Action + ": kayıt sayısı");
            foreach (var c in written) Assert.AreEqual(sim.S.Day, c.day, plan.Action + ": aynı gün");
            return written;
        }

        static void AssertRecord(SavedCommand c, string kind, string a, string b)
        {
            Assert.AreEqual(kind, c.kind);
            Assert.AreEqual(a, c.a);
            Assert.AreEqual(b, c.b);
        }

        [Test]
        public void DropPlan_Queue_Reserve_Cancel_RunTheirOwnCoreCommand()
        {
            var sim = Retired();
            Assert.AreEqual(0, sim.Board.Orders.Count, "başlangıçta iş emri yok");

            var queued = Run(sim, new BoardDropPlan(BoardDropAction.Queue, "armor"), 1);
            AssertRecord(queued[0], "board_queue", "armor", null);
            var order = sim.Board.Orders.Single();
            Assert.AreEqual("armor", order.RecipeId);

            var reserved = Run(sim, new BoardDropPlan(BoardDropAction.Reserve, "metal", amount: 1), 1);
            AssertRecord(reserved[0], "board_reserve", "metal", "1");
            Assert.AreEqual(1, order.Reserves.Single(r => r.Kind == "metal").Quantity, "mutlak miktar");

            var back = Run(sim, new BoardDropPlan(BoardDropAction.Reserve, "metal", amount: 0), 1);
            AssertRecord(back[0], "board_reserve", "metal", "0");

            var cancelled = Run(sim, new BoardDropPlan(BoardDropAction.Cancel, null, job: order.Id), 1);
            AssertRecord(cancelled[0], "board_cancel", order.Id, null);
            Assert.AreEqual(0, sim.Board.Orders.Count);
        }

        [Test]
        public void DropPlan_Replace_IsCancelThenQueue_TwoRecordsSameDay()
        {
            var sim = Retired();
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            string oldId = sim.Board.Orders.Single().Id;

            var written = Run(sim, new BoardDropPlan(BoardDropAction.Replace, "medkit", job: oldId), 2);
            AssertRecord(written[0], "board_cancel", oldId, null);
            AssertRecord(written[1], "board_queue", "medkit", null);
            var order = sim.Board.Orders.Single();
            Assert.AreEqual("medkit", order.RecipeId);
            Assert.AreNotEqual(oldId, order.Id, "yeni emir yeni kimlikle");
        }

        [Test]
        public void DropPlan_Replace_RefusedCancel_QueuesNothing()
        {
            var sim = Retired();
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            int before = sim.Record.commands.Count;
            var result = new BoardDropPlan(BoardDropAction.Replace, "medkit", job: "is-999").Command()(sim);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual(Loc.T("Böyle bir iş emri yok."), result.Msg, "iptalin iletisi döner");
            Assert.AreEqual(before, sim.Record.commands.Count, "reddedilen değiştirme kayda girmez");
            Assert.AreEqual("armor", sim.Board.Orders.Single().RecipeId, "eski emir yerinde");
        }

        // ---------------------------------------------------------------- sunum durumu

        [Test]
        public void UiState_Reset_ClearsStage5Fields()
        {
            var ui = new BoardUiState { BookSpread = 2, NewItem = new NewItemState { Day = 3, Id = "esya-12", Slid = true, Touched = true } };
            ui.RecipePoses["is-1"] = new Vector3(520, 330, 4);
            ui.Reset();
            Assert.AreEqual(0, ui.BookSpread);
            Assert.IsEmpty(ui.RecipePoses);
            Assert.IsNull(ui.NewItem);
        }

        [Test]
        public void Snapshot_Stage5Defaults()
        {
            var s = new BoardSnapshot();
            Assert.IsEmpty(s.Recipes);
            Assert.IsNotNull(s.Order);
            Assert.AreEqual(OrderState.Empty, s.Order.State);
            Assert.IsNull(s.Order.Card);
            Assert.IsNull(s.NewItem);
            Assert.IsNotNull(s.Book);
            Assert.IsNull(s.Book.Header, "BookWords'ün Türkçe ilkleyicisi yok (AGENTS kural 9)");
            Assert.IsFalse(s.RecipesDraggable);
            Assert.AreEqual(-1, new RecipeCardData().Done, "varsayılan: ilerleme kutusu yok");
        }

        // ---------------------------------------------------------------- yerel tokenlar (_local_a5)

        static SiginakTheme Fresh()
        {
            var theme = ScriptableObject.CreateInstance<SiginakTheme>();
            SiginakThemeSetup.Fill(theme, System.IO.File.ReadAllText(SiginakThemeSetup.TokensPath));
            return theme;
        }

        static bool HasKey(SiginakTheme t, string key) =>
            t.colors.Any(c => Matches(c.key, key)) || t.numbers.Any(n => Matches(n.key, key)) || t.strings.Any(s => Matches(s.key, key));

        static bool Matches(string have, string key) => have == key || have.StartsWith(key + ".") || have.StartsWith(key + "[");

        [Test]
        public void Tokens_LocalA5_EveryNewPathResolves()
        {
            var t = Fresh();
            try
            {
                var paths = ((JArray)JObject.Parse(System.IO.File.ReadAllText(SiginakThemeSetup.TokensPath))["_local_a5"]["new"]).Select(p => (string)p).ToList();
                Assert.AreEqual(13, paths.Count);
                var missing = paths.Where(p => !HasKey(t, p.StartsWith("colors.") ? p.Substring(7) : p)).ToList();
                Assert.IsEmpty(missing, "temada karşılığı olmayan yerel tokenlar: " + string.Join(", ", missing));
            }
            finally { Object.DestroyImmediate(t); }
        }

        [Test]
        public void Tokens_LocalA5_Values()
        {
            var t = Fresh();
            try
            {
                Assert.AreEqual(new Vector3(0f, 1.227f, -0.665f), t.Vector("recipeBook.readPoseLocal.pos"));
                Assert.AreEqual(-9f, t.Number("recipeBook.readPoseLocal.tiltX"), 1e-4);
                Assert.AreEqual(new Vector3(-0.02f, 1.062f, -0.62f), t.Vector("recipeBook.lowPoseLocal.pos"));
                Assert.AreEqual(-14f, t.Number("recipeBook.lowPoseLocal.tiltX"), 1e-4);
                Assert.AreEqual(4f, t.Number("recipeBook.hoverMm"), 1e-4);
                Assert.AreEqual(0.1f, t.Number("recipeBook.coverDelayS"), 1e-4);
                Assert.AreEqual(6f, t.Number("cards.recipeHeldTiltDeg"), 1e-4);
                Assert.AreEqual(0.25f, t.Number("motion.board.bookDropS"), 1e-4);
                Assert.AreEqual("B4291C", ColorUtility.ToHtmlStringRGB(t.ColorOf("board.newTag.color")));
                Assert.AreEqual("F7EFD8", ColorUtility.ToHtmlStringRGB(t.ColorOf("board.newTag.ink")));
                Assert.AreEqual(new Vector2(32, 13), t.Vector2Of("board.newTag.sizeMm"));
                Assert.AreEqual(9.4f, t.Number("board.newTag.capMm"), 1e-4);
                Assert.AreEqual("9A1E14", ColorUtility.ToHtmlStringRGB(t.ColorOf("stamp.stopped")));
                Assert.AreEqual("solid", t.Text("stamp.stopped.edge"));
                Assert.AreEqual("öne gelir, kadrajın altına kayar; ekranda düğme yok (sekmeler ve kapak)", t.Text("recipeBook.readPose"),
                    "paketin metin düğümü yerinde");
                // Paketin değerleri değişmedi
                Assert.AreEqual(0.35f, t.Number("motion.board.bookForwardS"), 1e-4);
                Assert.AreEqual(4f, t.Number("cards.hoverLiftMm"), 1e-4);
                Assert.AreEqual(10f, t.Number("cards.heldLiftMm"), 1e-4);
            }
            finally { Object.DestroyImmediate(t); }
        }
    }
}
