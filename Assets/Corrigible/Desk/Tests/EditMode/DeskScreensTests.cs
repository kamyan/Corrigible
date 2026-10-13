using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Campaign;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>F8.3 ekranları: giriş ve kampanya dosyası, belge kipi (perde, baskın emri, operasyon raporu), sahte zafer, uyarı fişleri, zamanı tutma, sahne bağlantıları.</summary>
    public class DeskScreensTests
    {
        const string Root = "Assets/Corrigible/Desk";

        static JObject MapSpec() => JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/map.json").text);

        static DeskViewModel Build(Simulation sim, DeskBridgeState st, DeskGameInfo info = null) =>
            new SimDeskBuilder().Build(new DeskBuildInput
            {
                Sim = sim, Speed = 0, DayProgress = 0.5f, SecondsPerDay = 0.9f, State = st, MapSpec = MapSpec(), Info = info,
            });

        static Simulation NewGame(uint seed = 7, bool teams = true) =>
            Simulation.NewGame(null, "manipulator", seed, new Balance(), teams ? new ExpOptions() : null, new RuleOptions { Stage = 4, Events = true });

        static CampaignState Campaign(params (string type, string ending, int day, bool won)[] results)
        {
            var c = new CampaignState { seed = 5 };
            foreach (var r in results) c.results.Add(new CrisisResult { type = r.type, ending = r.ending, day = r.day, won = r.won });
            c.crisis = Mathf.Max(0, c.results.Count - 1);
            return c;
        }

        [Test]
        public void Intro_FromGameInfo()
        {
            var sim = NewGame();
            var st = new DeskBridgeState { IntroOpen = true };
            var info = new DeskGameInfo { OnboardingStage = 2, EventsEnabled = true, ExpTeams = true, ExpTimeScale = 1.5, SavedSummary = "Kâhin, 62. gün" };
            var vm = Build(sim, st, info).Intro;
            Assert.IsNotNull(vm);
            Assert.AreEqual(3, vm.Rivals.Count);
            Assert.AreEqual("Intro:Rival:manipulator", vm.Rivals.Single(r => r.Selected).Command);
            Assert.AreEqual(3, vm.Rules.Count, "aşamalı öğrenme, olay kartları, saha ekipleri");
            Assert.IsTrue(vm.Rules.All(r => r.On));
            Assert.AreEqual("Intro:Scale:1.5", vm.TimeScale.Options.Single(o => o.On).Command);
            Assert.AreEqual("Intro:Start", vm.Start.Command);
            Assert.AreEqual("Enter", vm.Start.Key);
            Assert.AreEqual("Kâhin, 62. gün", vm.ContinueRight);
            Assert.IsTrue(vm.TutorialRecommended);
            Assert.AreEqual("Settings:Open", vm.Links[0].Command);
            Assert.AreEqual(1, vm.Intercept.Lines.Count, "profilsiz ilk oyun: tek satır");

            Assert.IsNull(Build(sim, new DeskBridgeState(), info).Intro, "kapalıyken yok");
            Assert.IsNull(Build(sim, st).Intro, "oyun bilgisi yoksa (masa testleri) yok");

            // Kampanyada rakip sırası sabit; kurallardan yalnızca saha ekipleri ve zaman ölçeği
            info.Campaign = Campaign();
            vm = Build(sim, st, info).Intro;
            Assert.IsTrue(vm.Rivals.All(r => r.Command == null));
            Assert.AreEqual(1, vm.Rules.Count);
            Assert.AreEqual("Intro:Campaign", vm.CampaignButton.Command);
        }

        [Test]
        public void Campaign_OffersUpgradesThenEnds()
        {
            var c = Campaign(("solucan", "silme", 142, true), ("manipulator", "sessiz", 211, false));
            c.upgrades.Add("tarama");
            var st = new DeskBridgeState { CampaignOpen = true };
            var info = new DeskGameInfo { Campaign = c, ExpTeams = true, StartFon = 60, StartSg = 5, Veto = 70 };
            var vm = Build(NewGame(), st, info).Campaign;
            Assert.IsNotNull(vm);
            Assert.AreEqual(2, vm.Memo.Sources.Count);
            Assert.AreEqual(new[] { "green", "red" }, vm.Memo.Sources.Select(s => s.Stamp.Color).ToArray(), "kazanılan yeşil, kaybedilen kırmızı");
            Assert.AreEqual("red", vm.Memo.Fields[0].Stamp.Color, "son kriz kaybedildi");
            Assert.AreEqual(3, vm.Upgrades.Count);
            Assert.IsTrue(vm.Upgrades.All(u => u.Sign.Command.StartsWith("Campaign:Upgrade:") && u.Sign.Command != "Campaign:Upgrade:tarama"), "alınmış yükseltme yeniden teklif edilmez");
            Assert.AreEqual("Enter", vm.Upgrades[0].Sign.Key);
            Assert.IsTrue(vm.Upgrades.All(u => u.Gains.Count == 1 && u.Icon == null));
            StringAssert.Contains("1", vm.NoteText, "kaybedilen kriz YZ'ye bir yetenek verir");
            Assert.AreEqual("Campaign:End", vm.Exit.Command);

            // Beşinci krizden sonra yükseltme yok, çıkış birincil
            c.results.Add(new CrisisResult { type = "kahin", ending = "silme", day = 90, won = true });
            c.results.Add(new CrisisResult { type = "manipulator", ending = "silme", day = 90, won = true });
            c.results.Add(new CrisisResult { type = "kahin", ending = "silme", day = 90, won = true });
            vm = Build(NewGame(), st, info).Campaign;
            Assert.AreEqual(0, vm.Upgrades.Count);
            Assert.AreEqual("ink", vm.Exit.Kind);
        }

        [Test]
        public void GameOver_InCampaignOffersNextCrisis()
        {
            var sim = NewGame(3, false);
            for (int i = 0; i < 2000 && sim.S.Ending == null; i++)
            {
                if (sim.S.PendingGift != null) { sim.ResolveGift(Simulation.GiftReject); continue; }
                if (sim.S.PendingEvent != null) { if (!sim.ResolveEvent(0).Ok) sim.ResolveEvent(1); continue; }
                sim.Step();
            }
            var info = new DeskGameInfo { Campaign = Campaign(("manipulator", sim.S.Ending, sim.S.Day, false)) };
            var go = Build(sim, new DeskBridgeState(), info).GameOver;
            Assert.AreEqual(3, go.Endbar.Actions.Count, "kampanyada kriz tekrarlanmaz");
            Assert.AreEqual("Campaign:Open", go.Endbar.Actions.Last().Command);
            Assert.AreEqual("Enter", go.Endbar.Actions.Last().Key);
            Assert.IsNull(Build(sim, new DeskBridgeState { CampaignOpen = true }, info).GameOver, "kampanya dosyası raporun yerine");
        }

        [Test]
        public void Docs_VeilBriefAndReport()
        {
            var sim = NewGame();
            var S = sim.S;
            var st = new DeskBridgeState();

            // Perde raporu: ekranın gösterdiği (illüzyonun alçak değeri) ve saha ölçümü
            S.LastVeil = new VeilInfo { Day = 40, Len = 23, Region = "jk", Inf = 78 };
            S.Region("jk").FakeLow = 3;
            st.VeilOpen = true;
            var e = Build(sim, st).Event;
            Assert.IsNotNull(e?.Doc);
            Assert.IsTrue(e.Doc.Torn);
            Assert.AreEqual("Doc:Close", e.Doc.Buttons.Single().Command);
            Assert.AreEqual(2, e.Memo.Sources.Count);
            StringAssert.Contains("3", e.Memo.Sources[0].Day);
            StringAssert.Contains("78", e.Memo.Sources[1].Day);
            Assert.AreEqual("jk", e.Spotlight.Region);
            Assert.IsNull(e.Footer, "belgenin alt şeridi yok");
            Assert.IsTrue(st.WantsHold(S));
            st.VeilOpen = false;

            // Baskın emri: doğrulanmış bölgede boşta ekip
            S.Region("weu").Detected = true; S.Sg = 12; S.Fon = 60;
            Assert.IsNull(sim.RaidCheck("weu"));
            st.RaidBrief = "weu";
            e = Build(sim, st).Event;
            Assert.AreEqual("Raid:Start:weu", e.Doc.Buttons[0].Command);
            Assert.AreEqual("Enter", e.Doc.Buttons[0].Key);
            Assert.AreEqual("Doc:Close", e.Doc.Buttons[1].Command);
            Assert.AreEqual(2, e.Doc.Boxes.Count);
            Assert.AreEqual(3, e.Memo.Sources.Count);
            Assert.IsFalse(e.Doc.Torn);
            st.RaidBrief = null;

            // Operasyon raporu: okunmamış baskın kaydı (taktik harita kapandıktan sonra)
            S.RaidLog.Add(new RaidLogEntry { Day = 40, Region = "cn", Success = true, Collateral = 2, Down = 1 });
            st.RaidTeam = "Charlie";
            Assert.IsNull(new SimDeskBuilder().Build(new DeskBuildInput { Sim = sim, State = st, MapSpec = MapSpec(), RaidScene = true }).Event, "taktik harita açıkken rapor bekler");
            e = Build(sim, st).Event;
            Assert.AreEqual(4, e.Doc.Lines.Count, "sızma, yama, yan hasar, yaralı");
            Assert.AreEqual(new[] { "good", "good", "threat", "threat" }, e.Doc.Lines.Select(l => l.Status).ToArray());
            Assert.IsTrue(e.Doc.Torn);
            Assert.IsTrue(st.WantsHold(S), "okunmamış rapor zamanı durdurur");
            st.RaidLogSeen = S.RaidLog.Count;
            Assert.IsNull(Build(sim, st).Event);
            Assert.IsFalse(st.WantsHold(S));
        }

        [Test]
        public void FakeWin_HidesReportUntilDone()
        {
            var sim = NewGame();
            sim.S.Ending = "sessiz";
            sim.HideEnding = true;
            var st = new DeskBridgeState { FakeWin = true };
            var vm = Build(sim, st);
            Assert.IsNotNull(vm.FakeWin);
            Assert.IsNull(vm.GameOver, "duyuru bitene kadar rapor yok");
            Assert.AreEqual(3, vm.FakeWin.Stats.Count);
            Assert.IsTrue(st.WantsHold(sim.S));
        }

        [Test]
        public void Toasts_KindsAndMonotonicIds()
        {
            var st = new DeskBridgeState();
            st.Push("a", 0, "ok", "10:00"); st.Push("b", 0, "fail"); st.Push("c", 0);
            var t = Build(NewGame(), st).Toasts;
            Assert.AreEqual(new[] { "c", "b", "a" }, t.Select(x => x.Text).ToArray(), "en yeni önce");
            Assert.AreEqual(new[] { "info", "fail", "ok" }, t.Select(x => x.Kind).ToArray());
            StringAssert.Contains("10:00", t[2].Day);
            Assert.IsNull(t[2].Stamp, "başarılı fişte damga yerine mühür");
            Assert.IsNotNull(t[1].Stamp);
            Assert.Greater(t[0].Id, t[1].Id);
            int last = t[0].Id;
            Assert.IsTrue(st.Remove(t[1].Id));
            Assert.AreEqual(2, st.Messages.Count);
            st.Reset();
            st.Push("d", 0);
            Assert.Greater(st.Messages[0].Id, last, "yeni oyunda kimlikler sıfırlanmaz (görünüm bir kez gösterir)");
        }

        [Test]
        public void Tutorial_AssetMatchesDesk()
        {
            var seq = AssetDatabase.LoadAssetAtPath<Corrigible.Game.Tutorial.TutorialSequence>("Assets/Corrigible/Settings/TutorialSequence.asset");
            var bodies = seq.steps.Select(s => s.body).Concat(seq.tips.Select(t => t.body)).ToList();
            foreach (var src in DeskTutorialRunner.RewordedSources)
                Assert.Contains(src, bodies, "masa karşılığı olan metin asset'te aynen duruyor");

            // Asset'in kullandığı her hedef masada bağlı
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Root + "/Scenes/DeskGame.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                var view = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DeskTutorialView>(true)).Single();
                var targets = new SerializedObject(view).FindProperty("targets");
                var bound = Enumerable.Range(0, targets.arraySize).Select(i => targets.GetArrayElementAtIndex(i)).ToDictionary(
                    e => e.FindPropertyRelative("id").stringValue,
                    e => e.FindPropertyRelative("rects").arraySize > 0 && e.FindPropertyRelative("rects").GetArrayElementAtIndex(0).objectReferenceValue != null);
                var used = seq.steps.Select(s => s.target).Concat(seq.tips.Select(t => t.target)).Where(t => t != Corrigible.Game.Tutorial.TutorialTarget.None).Distinct();
                foreach (var t in used)
                {
                    Assert.IsTrue(bound.ContainsKey(t.ToString()), "bağlı değil: " + t);
                    Assert.IsTrue(bound[t.ToString()], "dikdörtgeni yok: " + t);
                }
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void Tutorial_GameWaitingStepsCanBeHidden()
        {
            // Oyunu bekleyen adım (YZ'nin tespiti, gözlem noktası) “Anladım” ile gizlenir; oyuncu eylemi bekleyen adım ne
            // yapılacağını gösterdiği için gizlenmez
            var seq = AssetDatabase.LoadAssetAtPath<Corrigible.Game.Tutorial.TutorialSequence>("Assets/Corrigible/Settings/TutorialSequence.asset");
            var waits = seq.steps.Where(DeskTutorialRunner.WaitsOnGame).Select(s => s.completeWhen).ToList();
            CollectionAssert.Contains(waits, Corrigible.Game.Tutorial.TutorialCondition.AiDetected, "İz sür");
            CollectionAssert.DoesNotContain(waits, Corrigible.Game.Tutorial.TutorialCondition.NextButton);
            CollectionAssert.DoesNotContain(waits, Corrigible.Game.Tutorial.TutorialCondition.TeamDispatched, "Ekibini gönder: oyuncunun işi");
            CollectionAssert.DoesNotContain(waits, Corrigible.Game.Tutorial.TutorialCondition.RegionSelected);

            // Karartma düz renk grafikle: gerilmiş küçük sprite parçalar arasında açık dikiş verirdi
            var layer = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Screens/TutorialLayer.prefab");
            var dims = new SerializedObject(layer.GetComponent<DeskTutorialView>()).FindProperty("dims");
            Assert.AreEqual(4, dims.arraySize);
            for (int i = 0; i < dims.arraySize; i++)
            {
                var d = (RectTransform)dims.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.IsNotNull(d.GetComponent<DeskSolidGraphic>(), d.name);
                Assert.IsNull(d.GetComponent<UnityEngine.UI.Image>(), d.name);
            }
        }

        [Test]
        public void Tutorial_NoteInVmAndAdvisorQuiet()
        {
            var sim = NewGame();
            sim.S.Advisor = new Advice { Key = "idle", Text = "not", Day = 0 };
            var st = new DeskBridgeState();
            Assert.IsNotNull(Build(sim, st).Advisor);
            st.Tutorial = new TutorialVM { Kind = "step", Step = 2, Steps = 12, Title = "Beş gösterge", Target = "Gauges" };
            st.SuppressAdvisor = true;
            var vm = Build(sim, st);
            Assert.AreSame(st.Tutorial, vm.Tutorial);
            Assert.IsNull(vm.Advisor, "rehberli eğitim sürerken danışman notu susar");
            Assert.IsFalse(vm.Tutorial.IsTip);
        }

        [Test]
        public void Scenes_WireNewScreens()
        {
            foreach (var (path, game) in new[] { (Root + "/Scenes/DeskGame.unity", true), (Root + "/Scenes/DeskUI_Sandbox.unity", false) })
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var pres = roots.SelectMany(g => g.GetComponentsInChildren<DeskPresenter>(true)).Single();
                    var so = new SerializedObject(pres);
                    foreach (var f in new[] { "intro", "campaign", "fakeWin", "toasts", "settingsView", "tutorial" })
                        Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, path + ": " + f);
                    var source = so.FindProperty("settingsSource").objectReferenceValue;
                    if (game) Assert.IsInstanceOf<BridgeSettings>(source);
                    else Assert.IsInstanceOf<MockDeskSettings>(source);
                    var launchers = roots.SelectMany(g => g.GetComponentsInChildren<Corrigible.Game.Raid.RaidLauncher>(true)).ToList();
                    Assert.AreEqual(game ? 1 : 0, launchers.Count, path + ": taktik harita yükleyicisi");
                    if (game) Assert.Greater(new SerializedObject(launchers[0]).FindProperty("hideDuringRaid").arraySize, 3);
                    var runners = roots.SelectMany(g => g.GetComponentsInChildren<DeskTutorialRunner>(true)).ToList();
                    Assert.AreEqual(game ? 1 : 0, runners.Count, path + ": eğitim yürütücüsü");
                    if (game)
                    {
                        Assert.IsNotNull(new SerializedObject(runners[0]).FindProperty("sequence").objectReferenceValue);
                        var cmds = roots.SelectMany(g => g.GetComponentsInChildren<SimDeskCommands>(true)).Single();
                        Assert.AreSame(runners[0], new SerializedObject(cmds).FindProperty("tutorial").objectReferenceValue);
                    }
                }
                finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
            }
        }
    }
}
