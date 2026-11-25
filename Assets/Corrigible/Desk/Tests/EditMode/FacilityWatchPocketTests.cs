using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 5 (Docs/TESIS-BASKINLARI-TASARIMI.md §7.2): kartlı panoda GÖZETİM cebi, dinleme nöbetinin sunumu. Kart cebe bırakılınca tek <c>board_assign(kişi, watch)</c>; şerit, üzerine gelme satırı ve önizleme dinleme puanını
    /// (<c>Facility.Intel</c> / <c>facilityIntelCost</c>) ve açılacak paftayı (<c>FacilityIntelTarget</c>) Core'dan okur; devir fişinde
    /// görevli GÖZETİM satırıyla görünür. Kurucular RNG çekmez, komut yazmaz.
    /// </summary>
    public sealed class FacilityWatchPocketTests
    {
        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        /// <summary>Kartlı harekât, tohum 79; açılış komutları sunucunun <c>RetireJobs</c>'u gibi (watch emekli değil).</summary>
        /// <param name="cost">Plan bedeli (varsayılan 1: her vardiya bir pafta; 3: eski kayıtların birikimli sayacı).</param>
        static Simulation Retired(int cost = 1)
        {
            var sim = Simulation.NewGame(79, new Balance
            {
                shelterEventsEnabled = false, planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100,
                admissionKnockMax = 100, facilityIntelCost = cost,
            });
            Assert.IsTrue(sim.EnableAutomaticProduction().Ok);
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "contact")).ToArray())
                Assert.IsTrue(sim.AssignResident(p.Id, "rest").Ok);
            return sim;
        }

        static ResidentState Idle(Simulation sim) => sim.Board.People.First(p => p.Alive && p.Job == "rest" && p.Down <= 0 && !p.Restricted);

        static BoardSnapshot Snap(Simulation sim, bool english = false) =>
            PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState(), english, false, false);

        static void Close(Simulation sim)
        {
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }

        static string Fingerprint(Simulation sim) =>
            sim.Record.commands.Count + "|" + sim.S.Rng.State + "|" + sim.Facility.Intel + "|" + string.Join(",", sim.Facility.Known) + "|" +
            string.Join(",", sim.Board.People.Select(p => p.Id + ":" + p.Job));

        // ---------------------------------------------------------------- bırakma → komut

        /// <summary>
        /// Uçtan uca (§7.1–7.2, "plan baskından önce görünür"; 2026-11-24'ten beri her vardiya bir oda): hiç baskın yapılmadan
        /// Gözetim'deki kişi bir kapanışta cephe kanadının giriş odasını açar; görev fişinin yanındaki kurucu 1. paftayı çizer (64 kare),
        /// öbür ikisi PLAN YOK kalır; iki kapanış daha kanadın üç paftasını da çizer.
        /// </summary>
        [Test]
        public void WatchClosings_DrawOneSheetPerShift_BeforeAnyRaid()
        {
            var sim = Retired();
            string region = sim.S.Liberation.CurrentRegion;
            var before = FacilityPlanBuilder.Build(sim, region);
            CollectionAssert.AreEqual(new[] { false, false, false }, before.Sheets.Select(s => s.Known));
            Assert.IsTrue(sim.AssignResident(Idle(sim).Id, "watch").Ok);
            Close(sim);
            Assert.IsEmpty(sim.S.RaidLog, "baskın yok");
            var d = FacilityPlanBuilder.Build(sim, region);
            Assert.IsTrue(d.Front);
            CollectionAssert.AreEqual(new[] { true, false, false }, d.Sheets.Select(s => s.Known));
            Assert.AreEqual(64, d.Sheets[0].Tiles.Count);
            Assert.IsNull(d.Sheets[0].Stamp);
            Assert.AreEqual("PLAN YOK", d.Sheets[1].Stamp);
            Close(sim); Close(sim);
            CollectionAssert.AreEqual(new[] { true, true, true }, FacilityPlanBuilder.Build(sim, region).Sheets.Select(s => s.Known), "üç kapanışta üç pafta");
        }

        [Test]
        public void CardDroppedOnWatch_IsOneBoardAssignWatch()
        {
            var sim = Retired();
            var p = Idle(sim);
            var t = BoardTarget.Pocket(BoardLayout.WatchJob);
            Assert.IsTrue(BoardDropRules.Valid(BoardCardKind.Person, p.Id, t, sim), BoardDropRules.Reason(BoardCardKind.Person, p.Id, t, sim));
            int before = sim.Record.commands.Count;
            uint rng = sim.S.Rng.State;
            var result = BoardDropRules.CommandFor(BoardCardKind.Person, p.Id, t, sim)(sim);
            Assert.IsTrue(result.Ok, result.Msg);
            Assert.AreEqual(before + 1, sim.Record.commands.Count, "tek komut");
            var c = sim.Record.commands.Last();
            Assert.AreEqual("board_assign", c.kind);
            Assert.AreEqual(p.Id, c.a);
            Assert.AreEqual("watch", c.b);
            Assert.AreEqual(rng, sim.S.Rng.State, "RNG çekilmez");

            // Kayıttan aynen kurulur (yeni komut yok: board_assign)
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(sim.Record);
            Assert.AreEqual(0, restored.ReplayFailures);
            Assert.AreEqual("watch", restored.Board.Person(p.Id).Job);
        }

        // ---------------------------------------------------------------- puan metni

        /// <summary>Varsayılan (bedel 1, 2026-11-24): şerit doğrudan açılacak paftayı söyler, her kapanışta bir sonraki.</summary>
        [Test]
        public void StripAndHover_EveryShift_NamesTheNextSheet()
        {
            var sim = Retired();
            var p = Idle(sim);
            Assert.IsTrue(sim.AssignResident(p.Id, "watch").Ok);
            var wing = sim.FacilityPlan.Wing(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id);
            for (int sheet = 1; sheet <= 3; sheet++)
            {
                if (sheet > 1) Close(sim);
                var card = Snap(sim).Person(p.Id);
                Assert.AreEqual(Loc.F("Dinleme · PAFTA {0}", sheet), card.Strip, "kapanış " + (sheet - 1));
                Assert.AreEqual(Loc.F("her vardiya bir pafta · {0}", Loc.F("sıradaki: PAFTA {0}", sheet)), card.HoverLine);
                Assert.AreEqual(wing[sheet - 1].Id, sim.FacilityIntelTarget.Id, "Core'un hedefiyle aynı pafta");
                Assert.AreEqual(Loc.F("kapanışta PAFTA {0} çıkar", sheet), FacilityWatchText.CloseLine(sim));
            }
            Close(sim);
            Assert.IsTrue(wing.All(r => sim.FacilityKnown(r.Id)), "üç kapanışta kanadın üç paftası");
            Assert.AreEqual("Dinleme · bitti", Snap(sim).Person(p.Id).Strip);

            Loc.SetLanguage(Loc.English, Loc.ParseTable(System.IO.File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"))[Loc.English]);
            foreach (var room in wing) sim.Facility.Known.Remove(room.Id);   // test kurulumu: paftalar yeniden bilinmez
            Assert.AreEqual("Listening · SHEET 1", Snap(sim, true).Person(p.Id).Strip);
        }

        /// <summary>Kulak kazanımıyla (bedel 1) bir kapanış iki pafta açar: üzerine gelme ve önizleme ikisini söyler (Core'un döngüsüyle aynı).</summary>
        [Test]
        public void Ear_TwoSheetsPerClose_TextsSayTwo()
        {
            var sim = Retired();
            var p = Idle(sim);
            Assert.IsTrue(sim.AssignResident(p.Id, "watch").Ok);
            sim.Facility.Perks.Add(FacilityGear.Ear);   // test kurulumu: kazanım
            Assert.AreEqual(2, FacilityWatchText.SheetsPerClose(sim));
            Assert.AreEqual(Loc.F("her vardiya {0} pafta · {1}", 2, Loc.F("sıradaki: PAFTA {0}", 1)), Snap(sim).Person(p.Id).HoverLine);
            Assert.AreEqual(Loc.F("kapanışta PAFTA {0}–{1} çıkar", 1, 2), FacilityWatchText.CloseLine(sim));
            var wing = sim.FacilityPlan.Wing(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id);
            Close(sim);
            Assert.IsTrue(sim.FacilityKnown(wing[0].Id) && sim.FacilityKnown(wing[1].Id), "Core da iki pafta açtı");
            Assert.AreEqual(1, FacilityWatchText.SheetsPerClose(sim), "kalan tek pafta");
        }

        /// <summary>Eski bedel (3): puan birikir, şerit sayaçtır.</summary>
        [Test]
        public void StripAndHover_Cost3_FollowIntelAndNextSheet()
        {
            var sim = Retired(3);
            var p = Idle(sim);
            Assert.IsTrue(sim.AssignResident(p.Id, "watch").Ok);
            var wing = sim.FacilityPlan.Wing(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id);
            string[] strips = { "Dinleme 0/3", "Dinleme 1/3", "Dinleme 2/3", "Dinleme 0/3", "Dinleme 1/3" };
            int[] sheets = { 1, 1, 1, 2, 2 };
            for (int i = 0; i < strips.Length; i++)
            {
                if (i > 0) Close(sim);
                Assert.AreEqual("watch", sim.Board.Person(p.Id).Job, "görev vardiyalar arasında korunur");
                string before = Fingerprint(sim);
                var card = Snap(sim).Person(p.Id);
                Assert.AreEqual(before, Fingerprint(sim), "kurucu saf");
                Assert.AreEqual(strips[i], card.Strip, "kapanış " + i);
                Assert.AreEqual(Loc.F("dinleme {0}/{1} · {2}", sim.Facility.Intel, sim.B.facilityIntelCost, Loc.F("sıradaki: PAFTA {0}", sheets[i])),
                    card.HoverLine, "kapanış " + i);
                Assert.AreEqual(wing[sheets[i] - 1].Id, sim.FacilityIntelTarget.Id, "Core'un hedefiyle aynı pafta");
            }
            Assert.IsTrue(sim.FacilityKnown(wing[0].Id), "3 puan: giriş odasının planı açıldı");

            // Açılacak oda yok: şerit "plan yok", puan tavanda (Core)
            foreach (var room in wing) if (!sim.Facility.Known.Contains(room.Id)) sim.Facility.Known.Add(room.Id);   // test kurulumu
            var none = Snap(sim).Person(p.Id);
            Assert.AreEqual("Dinleme · bitti", none.Strip);
            StringAssert.EndsWith("açılacak plan yok", none.HoverLine);

            Loc.SetLanguage(Loc.English, Loc.ParseTable(System.IO.File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"))[Loc.English]);
            Assert.AreEqual("Listening · done", Snap(sim, true).Person(p.Id).Strip);
        }

        [Test]
        public void Preview_Cost3_ShowsListeningAndNextSheet_OrWarnsWhenNothingToOpen()
        {
            var sim = Retired(3);
            var p = Idle(sim);
            var preview = BoardPreviewText.For(BoardCardKind.Person, p.Id, BoardTarget.Pocket("watch"), sim);
            Assert.IsTrue(preview.Valid);
            Assert.IsFalse(preview.Warning, preview.Body);
            StringAssert.StartsWith("GÖZETİM ← ", preview.Title);
            StringAssert.Contains("kapanışta dinleme 0/3 → 1/3", preview.Body);
            StringAssert.Contains("sıradaki: PAFTA 1", preview.Body);
            sim.Facility.Intel = 2;   // test kurulumu: bir kapanış sonra plan çıkar
            preview = BoardPreviewText.For(BoardCardKind.Person, p.Id, BoardTarget.Pocket("watch"), sim);
            StringAssert.Contains("kapanışta PAFTA 1 çıkar", preview.Body);
            sim.Facility.Intel = 0;

            var wing = sim.FacilityPlan.Wing(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id);
            foreach (var room in wing) sim.Facility.Known.Add(room.Id);   // test kurulumu
            preview = BoardPreviewText.For(BoardCardKind.Person, p.Id, BoardTarget.Pocket("watch"), sim);
            Assert.IsTrue(preview.Valid);
            Assert.IsTrue(preview.Warning, "açılacak plan yok: uyarı");
            StringAssert.Contains("açılacak plan yok", preview.Body);
            StringAssert.Contains("kapanışta dinleme 0/3 → 1/3", preview.Body);

            // Takas satırı: GÖZETİM'deki kişi öbürünün yerine; geri dönüşte plaka sözcüğü GÖZETİM
            Assert.IsTrue(sim.AssignResident(p.Id, "watch").Ok);
            var cook = BoardDropRules.Occupant(sim.Board, "cook");
            var swap = BoardPreviewText.For(BoardCardKind.Person, cook.Id, BoardTarget.Person(p.Id), sim);
            StringAssert.Contains(Loc.F("takas: {0} → {1}", p.Name, "MUTFAK"), swap.Body);
        }

        // ---------------------------------------------------------------- devir fişi

        [Test]
        public void HandoverSlip_ListsTheWatcherAsGozetim()
        {
            var sim = Retired();
            var p = Idle(sim);
            Assert.IsTrue(sim.AssignResident(p.Id, "watch").Ok);
            string before = Fingerprint(sim);
            var facts = HandoverSlipFactsBuilder.Build(sim);
            Assert.AreEqual(before, Fingerprint(sim));
            var row = facts.shelter.Single(r => r.name == p.Name);
            Assert.AreEqual("watch", row.job);
            int at = facts.shelter.IndexOf(row);
            Assert.IsTrue(facts.shelter.Skip(at + 1).All(r => r.job == "rest"), "GÖZETİM cep satırlarının sonunda, BOŞTA'dan önce");
            var lines = HandoverSlipFormat.Format(facts);
            Assert.AreEqual("GÖZETİM", lines.Shelter.Single(l => l.Name == p.Name).Value);
        }
    }
}
