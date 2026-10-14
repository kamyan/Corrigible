using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Araştırma ağacı: okunabilir kilitler, her durumda açılan araç ve gözlemsiz Hizalama tahmini.</summary>
    public class DeskResearchTests
    {
        static Simulation NewGame() => Simulation.NewGame(null, "manipulator", 7u, new Balance(), economyVersion: 2);

        static Simulation ModernGame(Balance balance = null, RuleOptions rules = null) =>
            Simulation.NewGame(null, "manipulator", 7u, balance ?? new Balance(), new ExpOptions { Teams = true, TimeScale = 1.5 }, rules);

        static void Observe(Simulation sim, int count = 4)
        {
            sim.S.Aware = true;
            foreach (var region in sim.S.Regions.Take(count)) { region.Monitored = true; region.Detected = true; }
        }

        static ResearchVM Build(Simulation sim) => new SimDeskBuilder().Build(new DeskBuildInput
        {
            Sim = sim, Speed = 0, SecondsPerDay = 0.9f, State = new DeskBridgeState(),
            MapSpec = JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Mock/map.json").text),
        }).Research;

        static ResearchProjectVM Project(ResearchVM vm, string id) => vm.Branches.SelectMany(b => b.Projects).Single(p => p.Id == id);

        [Test]
        public void ResearchTree_KeepsBranchesAndConnectsEveryPrerequisite()
        {
            var sim = NewGame();
            uint rng = sim.S.Rng.State;
            int commands = sim.Record.commands.Count;
            var vm = Build(sim);
            CollectionAssert.AreEqual(new[] { GameData.BranchAlign, "Tespit", "Savunma", "Politika" }, vm.Branches.Select(b => b.Id));
            var projects = vm.Branches.SelectMany(b => b.Projects).ToDictionary(p => p.Id);
            Assert.AreEqual(9, projects.Count);
            foreach (var project in projects.Values)
            {
                Assert.That(project.Stage, Is.InRange(1, 4), project.Id);
                Assert.IsNotEmpty(project.Unlock, project.Id);
                if (project.PrerequisiteId == null) continue;
                Assert.IsTrue(projects.ContainsKey(project.PrerequisiteId), project.Id + ": ön koşul ağaçta görünmeli");
                Assert.Less(projects[project.PrerequisiteId].Stage, project.Stage, project.Id + ": ok önceki aşamadan gelir");
            }
            Assert.AreEqual(rng, sim.S.Rng.State, "Görünüm ağacı RNG tüketmez");
            Assert.AreEqual(commands, sim.Record.commands.Count, "Görünüm ağacı komut eklemez");
        }

        [TestCase("available")]
        [TestCase("locked")]
        [TestCase("running")]
        [TestCase("done")]
        [TestCase("stopped")]
        public void ProjectEffect_RemainsVisibleInEveryState(string state)
        {
            var sim = NewGame();
            string id = state == "locked" ? "dogrulama" : "anomali";
            if (state == "done") sim.S.Research.Done.Add(id);
            if (state == "running" || state == "stopped") Assert.IsTrue(sim.StartResearch(id).Ok);
            if (state == "stopped") sim.S.Illusion = new IllusionState { Start = sim.S.Day, Tek = sim.S.Tek, Trust = 70 };
            var project = Project(Build(sim), id);
            Assert.AreEqual(state, project.State);
            Assert.IsNotEmpty(project.Unlock, "Durum bilgisi araştırmanın açacağı aracı gizlememeli");
        }

        [Test]
        public void Verification_StatesPrerequisiteAndUnlocksAfterAnomaly()
        {
            var sim = NewGame();
            var locked = Project(Build(sim), "dogrulama");
            Assert.AreEqual("locked", locked.State);
            Assert.AreEqual("anomali", locked.PrerequisiteId);
            Assert.IsFalse(locked.CanStart);
            StringAssert.Contains(GameData.ResearchItem("anomali").Name, locked.LockReason);
            Assert.AreEqual(locked.LockReason, locked.DisabledReason);

            sim.S.Research.Done.Add("anomali");
            var available = Project(Build(sim), "dogrulama");
            Assert.AreEqual("available", available.State);
            Assert.IsTrue(available.CanStart);
            Assert.IsNull(available.LockReason);
            Assert.IsTrue(sim.StartResearch("dogrulama").Ok, "Kartın açılması Core başlatma kuralıyla aynı olmalı");
        }

        [Test]
        public void AlignmentWithoutObservation_CanStartButShowsNoFiniteEstimate()
        {
            var sim = NewGame();
            var beforeDetection = Project(Build(sim), "yorum");
            Assert.AreEqual("locked", beforeDetection.State);
            Assert.IsFalse(beforeDetection.CanStart);

            sim.S.Aware = true;
            var available = Project(Build(sim), "yorum");
            Assert.AreEqual("available", available.State);
            Assert.IsTrue(available.CanStart, "Gözlem eksikliği yeni bir başlatma kilidi değildir");
            Assert.IsTrue(available.NeedsObservation);
            Assert.IsNull(available.LockReason);
            Assert.AreEqual(Loc.T("Gözlem gerekli"), available.Days);
            Assert.AreEqual(Loc.T("İlerlemek için gözlem noktası kur."), available.Condition);
            Assert.AreEqual(Loc.T("Başlat"), available.StartLabel, "Hız sıfırken yanıltıcı 0 fon/gün etiketi kullanılmaz");

            Assert.IsTrue(sim.StartResearch("yorum").Ok);
            var running = Project(Build(sim), "yorum");
            Assert.AreEqual("running", running.State);
            Assert.AreEqual(Loc.T("Gözlem gerekli"), running.Sub);
            Assert.IsTrue(running.NeedsObservation);

            var region = sim.S.Regions[0];
            region.Monitored = true; region.Detected = true;
            var observed = Project(Build(sim), "yorum");
            Assert.IsFalse(observed.NeedsObservation);
            Assert.AreNotEqual(Loc.T("Gözlem gerekli"), observed.Sub, "Gözlem gelince kalan süre tekrar hesaplanır");
        }

        [Test]
        public void SingleProjectLimit_HasAnExplanationWithoutHidingTheNextEffect()
        {
            var sim = NewGame();
            Assert.IsTrue(sim.StartResearch("anomali").Ok);
            var queued = Project(Build(sim), "guclu");
            Assert.AreEqual("available", queued.State, "Ön koşulları açık, yalnız araştırma masası meşgul");
            Assert.IsFalse(queued.CanStart);
            Assert.AreEqual(Loc.T("Aynı anda tek araştırma yürür."), queued.LockReason);
            Assert.IsNotEmpty(queued.Unlock);
            Assert.IsFalse(sim.StartResearch("guclu").Ok);
        }

        [Test]
        public void ModernPanel_HasFourteenCardsAndPreservesSimulationWhileBuilding()
        {
            var sim = ModernGame();
            uint rng = sim.S.Rng.State;
            int commands = sim.Record.commands.Count;
            var vm = Build(sim);
            Assert.IsTrue(vm.ModernEconomy);
            CollectionAssert.AreEqual(new[] { GameData.BranchAlign, "Tespit", "Savunma", "Politika", "Bütçe" }, vm.Branches.Select(b => b.Id));
            var projects = vm.Branches.SelectMany(b => b.Projects).ToDictionary(p => p.Id);
            CollectionAssert.AreEquivalent(GameData.Operations.Concat(GameData.BudgetUpgrades).Select(o => o.Id)
                .Concat(GameData.Research.Where(r => r.Branch == GameData.BranchAlign).Select(r => r.Id)), projects.Keys);
            Assert.AreEqual(14, projects.Count);
            foreach (var project in projects.Values)
            {
                Assert.IsNotEmpty(project.Unlock, project.Id);
                if (project.PrerequisiteId == null) continue;
                Assert.IsTrue(projects.ContainsKey(project.PrerequisiteId), project.Id);
                Assert.Less(projects[project.PrerequisiteId].Stage, project.Stage, project.Id);
            }
            Assert.AreEqual(rng, sim.S.Rng.State, "Önizleme RNG tüketmez");
            Assert.AreEqual(commands, sim.Record.commands.Count, "Önizleme komut üretmez");
            Assert.AreEqual(15, sim.S.ResearchReserve);
        }

        [Test]
        public void ModernOperations_RemainPurchasableWhileAlignmentRuns()
        {
            var sim = ModernGame();
            Observe(sim);
            sim.S.Fon = 200;
            Assert.IsTrue(sim.StartResearch("yorum").Ok);
            sim.S.Research.Prog = 5;
            var operation = Project(Build(sim), "guclu_1");
            Assert.AreEqual("operation", operation.Kind);
            Assert.AreEqual("available", operation.State);
            Assert.IsTrue(operation.CanStart);
            Assert.IsFalse(operation.CanPause);
            Assert.IsNotEmpty(operation.PurchasePreview);
            Assert.IsNotEmpty(operation.Funding);
            Assert.IsTrue(sim.BuyOperation("guclu_1").Ok);
            Assert.AreEqual(5, sim.S.Research.Prog);
            Assert.AreEqual("yorum", sim.S.Research.Cur);
            Assert.AreEqual("done", Project(Build(sim), "guclu_1").State);
        }

        [Test]
        public void ModernPrerequisites_AndMediaGateMatchCoreChecks()
        {
            var sim = ModernGame(rules: new RuleOptions { Stage = 1 });
            sim.S.Fon = 300;
            var second = Project(Build(sim), "anomali_2");
            Assert.AreEqual("locked", second.State);
            Assert.AreEqual("anomali_1", second.PrerequisiteId);
            Assert.AreEqual(sim.OperationCheck("anomali_2"), second.DisabledReason);
            Assert.IsTrue(sim.BuyOperation("anomali_1").Ok);
            Assert.IsTrue(sim.BuyOperation("anomali_2").Ok);
            var verification = Project(Build(sim), "dogrulama");
            Assert.IsFalse(verification.CanStart);
            Assert.AreEqual(sim.OperationCheck("dogrulama"), verification.LockReason);
            var budget = Project(Build(sim), "butce_1");
            Assert.IsFalse(budget.CanStart);
            Assert.AreEqual(sim.OperationCheck("butce_1"), budget.DisabledReason);
        }

        [Test]
        public void ModernAlignment_RequiresObservationAndSeparatesManualPauseFromIllusion()
        {
            var sim = ModernGame();
            sim.S.Aware = true;
            var blocked = Project(Build(sim), "yorum");
            Assert.AreEqual("locked", blocked.State);
            Assert.IsFalse(blocked.CanStart);
            Assert.IsTrue(blocked.NeedsObservation);
            Assert.AreEqual(sim.ResearchCheck("yorum"), blocked.DisabledReason);
            Observe(sim, 1);
            Assert.IsTrue(sim.StartResearch("yorum").Ok);
            Assert.IsTrue(sim.SetResearchPaused(true).Ok);
            var paused = Project(Build(sim), "yorum");
            Assert.AreEqual("paused", paused.State);
            Assert.AreEqual(true, paused.ManuallyPaused);
            Assert.IsTrue(paused.CanPause);
            Assert.IsNull(paused.PauseDisabledReason);
            Assert.IsTrue(sim.SetResearchPaused(false).Ok);
            sim.S.Illusion = new IllusionState { Start = sim.S.Day, Tek = sim.S.Tek, Trust = 70 };
            var stopped = Project(Build(sim), "yorum");
            Assert.AreEqual("stopped", stopped.State);
            Assert.AreEqual(false, stopped.ManuallyPaused, "İllüzyon duruşu sürdür komutu gibi görünmez");
            Assert.AreEqual(Loc.T("Duraklat"), stopped.PauseLabel);
        }

        [Test]
        public void PendingEvent_DisablesPauseAndExplainsTheBlock()
        {
            var sim = ModernGame();
            Observe(sim);
            Assert.IsTrue(sim.StartResearch("yorum").Ok);
            sim.S.PendingEvent = new PendingDilemma { Id = "atilim", Region = "weu", Day = sim.S.Day };
            var project = Project(Build(sim), "yorum");
            Assert.IsTrue(project.CanPause, "Kontrol kaybolmaz; kapalı olma nedeni gösterilir");
            Assert.AreEqual(sim.SetResearchPausedCheck(true), project.PauseDisabledReason);
            Assert.IsNotEmpty(project.PauseDisabledReason);
        }

        [Test]
        public void FundingPreview_UsesRuntimePricesAndActualPaymentWhileReserveLimitsProgress()
        {
            var balance = new Balance { anomalyTier1Cost = 13, interpretationCost = 90, interpretationDays = 30 };
            var sim = ModernGame(balance);
            Observe(sim);
            sim.S.Fon = sim.S.ResearchReserve;
            Assert.IsTrue(sim.StartResearch("yorum").Ok);
            sim.S.LastIncomeDay = 12;
            sim.S.LastResearchPayment = 0.75;
            var vm = Build(sim);
            var operation = Project(vm, "anomali_1");
            StringAssert.Contains("13", operation.StartLabel);
            var alignment = Project(vm, "yorum");
            StringAssert.Contains("90", alignment.Sub);
            StringAssert.Contains("45", alignment.Sub, "30 iş günü / (1 / 1,5) = 45 simülasyon günü");
            Assert.IsNotEmpty(alignment.Funding);
            StringAssert.Contains(DeskFormat.Number(sim.LastResearchPayment, 1), vm.Summary.Text);
            StringAssert.Contains(DeskFormat.Number(sim.NextResearchPayment, 1), vm.Summary.Text);
            StringAssert.Contains("12", vm.Summary.Text, "Son ödeme günü sonraki tahminden ayrıdır");
        }

        [Test]
        public void ModernPanelPrefab_LaysOutAllCardsInsideScrollableContent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Desk/Prefabs/Screens/ResearchFolder.prefab");
            Assert.IsNotNull(prefab);
            var instance = Object.Instantiate(prefab);
            bool previousPreview = DeskUi.EditPreview;
            try
            {
                DeskUi.EditPreview = true;
                var sim = ModernGame();
                Observe(sim);
                sim.S.Fon = 200;
                Assert.IsTrue(sim.StartResearch("yorum").Ok);
                var view = instance.GetComponent<ResearchFolderView>();
                view.Bind(Build(sim));
                Assert.AreEqual(14, view.Rows.Count);
                var content = (RectTransform)new SerializedObject(view).FindProperty("branchesRoot").objectReferenceValue;
                var scroll = instance.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
                Assert.IsNotNull(scroll, "Beş dal panelden taşınca kaydırılabilmeli");
                Assert.IsTrue(scroll.vertical);
                Assert.IsTrue(view.BranchSpan(4, out _, out _, out _), "Bütçe dalının görünüm aralığı var");
                foreach (var row in view.Rows)
                {
                    var rect = (RectTransform)row.transform;
                    Assert.LessOrEqual(-rect.anchoredPosition.y + row.Height, content.rect.height + 1, row.ProjectId);
                    Assert.LessOrEqual(rect.anchoredPosition.x + rect.rect.width, content.rect.width + 1, row.ProjectId);
                    var card = row.GetComponent<ResearchProjectCardView>();
                    Assert.IsNotNull(card, row.ProjectId + ": tablo yerine proje fişi kullanılır");
                    Assert.GreaterOrEqual(row.Height, 144, row.ProjectId);
                }
                var current = view.Rows.Single(row => row.ProjectId == "yorum");
                Assert.IsTrue(current.PauseButton.gameObject.activeSelf, "Yeni projede pause kontrolü gösterilir");
                Assert.IsTrue(current.PauseButton.interactable);
                var locked = view.Rows.Single(row => row.ProjectId == "guclu_2");
                Assert.IsFalse(locked.StartButton.gameObject.activeSelf, "Eksik önkoşulun satın alma düğmesi kapalı kartta gizlidir");
            }
            finally
            {
                DeskUi.EditPreview = previousPreview;
                Object.DestroyImmediate(instance);
            }
        }

        [TestCase("C1_normal")]
        [TestCase("C2_illusion")]
        public void MockResearchFixtures_HaveExplicitMetadataAndKeepIdsInEnglish(string preset)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Mock/" + preset + ".json");
            var vm = JObject.Parse(asset.text)["research"].ToObject<ResearchVM>();
            CollectionAssert.AreEqual(new[] { GameData.BranchAlign, "Tespit", "Savunma", "Politika" }, vm.Branches.Select(b => b.Id));
            var projects = vm.Branches.SelectMany(b => b.Projects).ToList();
            CollectionAssert.AreEquivalent(GameData.Research.Select(r => r.Id), projects.Select(p => p.Id));
            foreach (var project in projects)
            {
                var definition = GameData.ResearchItem(project.Id);
                Assert.AreEqual(definition.Stage, project.Stage, project.Id);
                Assert.AreEqual(definition.Req, project.PrerequisiteId, project.Id);
                Assert.IsNotEmpty(project.Unlock, project.Id);
                if (project.PrerequisiteId != null) Assert.IsNotEmpty(project.Condition, project.Id);
            }
            var ids = projects.Select(p => p.PrerequisiteId).ToArray();
            var english = new MockEnglish(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Mock/mock_en.txt").text);
            english.Apply(vm);
            CollectionAssert.AreEqual(ids, projects.Select(p => p.PrerequisiteId), "Ön koşul kimlikleri çevrilmez");
            foreach (var project in projects)
            {
                Assert.IsFalse(MockEnglish.LooksTurkish(project.Unlock), project.Id + ": etki İngilizce görünmeli");
                Assert.IsFalse(MockEnglish.LooksTurkish(project.Condition), project.Id + ": koşul İngilizce görünmeli");
            }
        }
    }
}
