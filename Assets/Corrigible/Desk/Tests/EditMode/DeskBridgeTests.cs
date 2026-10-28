using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.Tests
{
    /// <summary>Oyun köprüsü: DeskGame sahnesinin bağlantıları, baskın raporu ve son raporun simülasyondan kurulması.</summary>
    public class DeskBridgeTests
    {
        const string ScenePath = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";

        static Simulation New() => Simulation.NewGame(731, new Balance { startFon = 100 });

        static DeskViewModel Build(Simulation sim, DeskBridgeState st = null, bool raidScene = false) =>
            new SimDeskBuilder().Build(sim, st ?? new DeskBridgeState(), raidScene);

        static void CaptureNext(Simulation sim)
        {
            string id = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id;
            while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step();
            Assert.IsTrue(sim.StartLiberationRaid(id).Ok, id);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true, Turns = 4 }).Ok, id);
        }

        static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        static Object Ref(Object owner, string field) => new SerializedObject(owner).FindProperty(field).objectReferenceValue;

        static void InScene(System.Action<Scene> check)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try { check(scene); }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void RaidReport_OpensOnceAfterTheRaidAndWaitsWhileTheRaidSceneIsOpen()
        {
            var sim = New();
            var st = new DeskBridgeState();
            Assert.IsNull(Build(sim, st).Event, "baskın yokken rapor yok");
            CaptureNext(sim);
            Assert.IsNull(Build(sim, st, raidScene: true).Event, "taktik harita açıkken rapor bekler");
            var report = Build(sim, st).Event;
            Assert.IsNotNull(report);
            Assert.AreEqual("Doc:Close", report.Doc.Buttons.Single().Command);
            st.RaidLogSeen = sim.S.RaidLog.Count;
            Assert.IsNull(Build(sim, st).Event, "okunan rapor yeniden açılmaz");
        }

        [Test]
        public void GameOver_AfterWholeCampaign_ReportsLiberationAndOffersRestart()
        {
            var sim = New();
            for (int i = 0; i < GameData.Regions.Length; i++)
            {
                while (!sim.LiberationRegion(sim.S.Liberation.CurrentRegion).Completed) CaptureNext(sim);
                if (i < GameData.Regions.Length - 1) Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
            }
            Assert.AreEqual("silme", sim.S.Ending);

            var vm = Build(sim);
            Assert.IsNull(vm.Event, "son rapor açıkken baskın raporu açılmaz");
            var over = vm.GameOver;
            Assert.IsNotNull(over);
            Assert.AreEqual(DeskText.Upper(GameData.Ending("silme").Title), over.Report.Title);
            Assert.AreEqual(4, over.Kpis.Count);
            Assert.AreEqual(GameData.Regions.Length.ToString(), over.Kpis[2].Value, "bütün bölgeler özgür");
            CollectionAssert.AreEqual(new[] { 0f, 100f }, over.Chart.Series.Points[0], "harekât tam işgalle başlar");
            Assert.AreEqual(0f, over.Chart.Series.Points.Last()[1], "ve işgalsiz biter");
            Assert.AreEqual("result", over.Timeline.Cards.Last().Type);
            Assert.AreEqual("Restart", over.Endbar.Actions.Single().Command);
        }

        [Test]
        public void RaidReport_ShowsTrustLoss_AndWarnsWhenLow()
        {
            var sim = New();
            int max = sim.TrustMax, start = sim.S.Trust, per = sim.B.liberationCiviliansPerTrust;
            Assert.Greater(max, start, "Güven tavanı başlangıçtan yüksek.");
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true, Collateral = 3, Turns = 4, CiviliansLeft = per }).Ok);
            var doc = Build(sim).Event.Doc;
            var left = doc.Lines.Single(l => l.Label == Loc.T("Ayakta kalan sivil sunucu"));
            Assert.AreEqual("good", left.Status);
            Assert.AreEqual(per.ToString(), left.Value);
            var line = doc.Lines.Single(l => l.Label == Loc.T("Sivil güven"));
            Assert.AreEqual("threat", line.Status);
            StringAssert.EndsWith($"{start - 2}/{max}", line.Value, "Kırılan 3 sunucu −3, ayakta kalanlar +1.");
            StringAssert.DoesNotContain(Loc.T("Sivil güven"), doc.Risk, "güven yüksekken uyarı yok");

            while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step();
            Assert.IsTrue(sim.StartLiberationRaid("weu_2").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = false, Collateral = sim.S.Trust - 1, Turns = 6 }).Ok);
            Assert.IsFalse(Build(sim).Event.Doc.Lines.Any(l => l.Label == Loc.T("Ayakta kalan sivil sunucu")), "kaybedilen baskında satır yok");
            doc = Build(sim).Event.Doc;
            StringAssert.StartsWith($"Sivil güven 1/{max}.", doc.Risk, "güven azalınca rapor uyarır");
        }

        [Test]
        public void GameOver_WhenTrustRunsOut_ReportsDefeat()
        {
            var sim = New();
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = false, Collateral = sim.B.liberationTrustStart, Turns = 6 }).Ok);
            Assert.AreEqual("dagilma", sim.S.Ending);
            var over = Build(sim).GameOver;
            Assert.IsNotNull(over);
            Assert.AreEqual(DeskText.Upper(GameData.Ending("dagilma").Title), over.Report.Title);
            Assert.AreEqual(Loc.T("KAYBEDİLDİ"), over.Report.BigStamp.Text);
            Assert.AreEqual(Loc.T("YZ kazandı."), over.Report.Who);
            Assert.AreEqual("Restart", over.Endbar.Actions.Single().Command);
        }

        [Test]
        public void GameOver_TimelineMarksCounterattacks()
        {
            var sim = New();
            CaptureNext(sim);
            sim.S.Alarm = sim.B.liberationAlarmThreshold;
            sim.Step();
            sim.S.Trust = 1;
            sim.S.Alarm = sim.B.liberationAlarmThreshold;
            sim.Step();
            Assert.AreEqual("dagilma", sim.S.Ending);
            var cards = Build(sim).GameOver.Timeline.Cards;
            var counter = cards.Single(c => c.Type == "counter");
            Assert.AreEqual(Loc.T("YZ ALDI"), counter.Stamp.Text);
            Assert.AreEqual("red", counter.Stamp.Color);
            Assert.AreEqual(Loc.T("Batı Avrupa"), counter.Title);
        }

        [Test]
        public void DeskGameScene_IsWired()
        {
            InScene(scene =>
            {
                var presenter = All<DeskPresenter>(scene).Single();
                Assert.IsInstanceOf<SimDeskDataSource>(Ref(presenter, "dataSource"));
                Assert.IsInstanceOf<SimDeskCommands>(Ref(presenter, "commands"));
                Assert.IsInstanceOf<BridgeSettings>(Ref(presenter, "settingsSource"));
                foreach (string field in new[] { "eventModal", "gameOver", "settingsView" })
                    Assert.IsNotNull(Ref(presenter, field), field);
                var surface = Ref(presenter, "surface");
                Assert.IsInstanceOf<LiberationDeskView>(surface, "masa sığınak odasına bağlı");
                foreach (string field in new[] { "game", "source", "siginakRig", "desk", "launcher", "telex", "siginak", "siginakMap", "siginakBriefing" })
                    Assert.IsNotNull(Ref(surface, field), field);
                Assert.AreEqual(1, All<Corrigible.Game.GameController>(scene).Length);
            });
        }

        [Test]
        public void DeskGameScene_UsesManualShifts()
        {
            InScene(scene =>
            {
                var desk = All<SiginakDesk>(scene).Single();
                Assert.IsNotNull(Ref(desk, "shiftSwitch"), "vardiya şalterle biter");
            });
        }

        [Test]
        public void DeskGameScene_RetainedScreensRenderAboveTheStationControls()
        {
            InScene(scene =>
            {
                foreach (var tint in All<ThemeTint>(scene))
                    if (!string.IsNullOrEmpty(tint.Key))
                        Assert.IsTrue(DeskTheme.Active.TryGet(tint.Key, out _), tint.name + ": " + tint.Key);
                // Sığınak odasının ekran katmanı (kenar satırı, plaka, teleks şeridi, rapor, emir belgesi)
                var contextCanvas = (Canvas)Ref(All<HoverTag>(scene).Single(), "canvas");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, contextCanvas.rootCanvas.renderMode);
                var presenter = All<DeskPresenter>(scene).Single();
                foreach (string field in new[] { "eventModal", "gameOver", "settingsView" })
                {
                    var canvas = ((Component)Ref(presenter, field)).GetComponentInParent<Canvas>(true);
                    Assert.IsNotNull(canvas, field);
                    Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.rootCanvas.renderMode, field + ": kamera Canvas'ı oda katmanının arkasında kalır.");
                    Assert.Greater(canvas.sortingOrder, contextCanvas.sortingOrder, field + ": rapor ve ayarlar oda katmanının önünde olmalı.");
                }
            });
        }

        [Test]
        public void DeskGameScene_BriefingButtonsStayOutsideTheWorldInputLock()
        {
            InScene(scene =>
            {
                var begin = (UnityEngine.UI.Button)Ref(All<BriefingView>(scene).Single(), "action");
                var canvas = begin.GetComponentInParent<Canvas>(true).rootCanvas;
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "emir belgesi Overlay: oda kilitliyken de fareyle ulaşılır");
                Assert.IsTrue(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled);
                Assert.IsTrue(begin.interactable);
                Assert.IsTrue(begin.targetGraphic.raycastTarget);
            });
        }

        [Test]
        public void DeskGameScene_MissionSlipStaysOnMapAndOnlyRaidUsesMonitor()
        {
            InScene(scene =>
            {
                var slip = All<OperationSlip>(scene).Single();
                var mount = scene.GetRootGameObjects().Single(g => g.name == "SiginakRoom").transform.Find("Desk/MapMount");
                Assert.IsTrue(slip.transform.IsChildOf(mount), "görev fişi masadaki haritanın üstünde");
                Assert.AreEqual(RenderMode.WorldSpace, slip.GetComponentInParent<Canvas>(true).renderMode);
                var launcher = All<Corrigible.Game.Raid.RaidLauncher>(scene).Single();
                Assert.AreSame(launcher, Ref(All<LiberationDeskView>(scene).Single(), "launcher"));
                Assert.IsTrue(new SerializedObject(launcher).FindProperty("enableMonitorTransition").boolValue);
                Assert.AreEqual("CrtDive", launcher.MonitorFocusPose.name, "yalnız baskın CRT'ye yaklaşır");
            });
        }

        [Test]
        public void DeskGameScene_MapRegionsUsePolygonMappingA()
        {
            InScene(scene =>
            {
                var map = All<SadeMapView>(scene).Single();
                CollectionAssert.AreEqual(GameData.Regions.Select(r => r.Id), map.Regions.Select(s => s.Id));
                Assert.AreEqual(90, map.Regions.Single(s => s.Id == "na").Fill.PolygonCount, "na = KA (81) + OA (9)");
                Assert.AreEqual(3, map.Regions.Single(s => s.Id == "cn").Fill.PolygonCount, "cn = DoA [0, 1, 7]");
                Assert.AreEqual(5, map.Regions.Single(s => s.Id == "jk").Fill.PolygonCount, "jk = DoA [2, 3, 4, 5, 6]");
            });
        }
    }
}
