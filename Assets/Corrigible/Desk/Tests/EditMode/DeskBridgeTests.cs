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
    /// <summary>Oyun köprüsü: DeskGame sahnesinin bağlantıları, operasyon raporu ve son raporun simülasyondan kurulması.</summary>
    public class DeskBridgeTests
    {
        const string ScenePath = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";

        static Simulation New() => Simulation.NewGame(731, new Balance { startFon = 100 });

        static DeskViewModel Build(Simulation sim, DeskBridgeState st = null) =>
            new SimDeskBuilder().Build(sim, st ?? new DeskBridgeState());

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
        public void FinalReport_AfterWholeCampaign_WonStamp_StripFromFullToFree_TurningPoints()
        {
            var sim = New();
            for (int i = 0; i < GameData.Regions.Length; i++)
            {
                while (!sim.LiberationRegion(sim.S.Liberation.CurrentRegion).Completed) CaptureNext(sim);
                if (i < GameData.Regions.Length - 1) Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
            }
            Assert.AreEqual("silme", sim.S.Ending);

            var over = Build(sim).FinalReport;
            Assert.IsNotNull(over);
            Assert.IsTrue(over.Won);
            Assert.AreEqual(Loc.T("KAZANILDI"), over.Stamp);
            Assert.IsNull(over.Reason, "kazanınca neden damgası yok");
            Assert.IsTrue(over.SubIsMotto);
            Assert.AreEqual(4, over.Indicators.Count);
            Assert.AreEqual(Loc.F("{0}/{1}", GameData.Regions.Length, GameData.Regions.Length), over.Indicators[2].Value, "bütün bölgeler özgür");
            Assert.AreEqual(OperationTone.Retaken, over.Indicators[2].Tone);
            Assert.AreEqual(sim.S.Day + 1, over.Shifts);
            Assert.AreEqual(new Vector2(1, 100), over.Points[0], "harekât tam işgalle başlar");
            Assert.AreEqual(new Vector2(over.Shifts, 0), over.Points.Last(), "ve son vardiyada işgalsiz biter");
            Assert.LessOrEqual(over.Moments.Count, FinalReportBuilder.MaxMoments);
            var first = sim.S.RaidLog.First(l => l.Success && l.LiberationReward?.RegionCompleted == true);
            Assert.AreEqual(Loc.F("{0} özgür, ilk bölge", Loc.T(GameData.Region(first.Region).Name)), over.Moments[0].Text, "ilk an ilk özgür bölge");
            Assert.AreEqual(Loc.F("{0} özgür, son sunucu düştü", Loc.T(GameData.Region(sim.S.RaidLog.Last().Region).Name)), over.Moments.Last().Text);
            Assert.IsFalse(over.Moments.Any(m => m.Alert), "kazanınca kırmızı an yok");
            CollectionAssert.AreEqual(Enumerable.Range(1, over.Moments.Count), over.Moments.Select(m => m.Number));
            Assert.AreEqual(Loc.T("YENİDEN BAŞLA"), over.Action);
        }

        /// <summary>Harekâtı <paramref name="shifts"/> vardiyaya uzatır (karşı saldırı ve güven kaybı olmadan), sonra Dağılma ile bitirir.</summary>
        static Simulation Lasting(int shifts)
        {
            var sim = New();
            CaptureNext(sim);
            while (sim.S.Day < shifts - 1 && sim.S.Ending == null) { sim.S.Alarm = 0; sim.S.Trust = sim.TrustMax; sim.Step(); }
            Assert.AreEqual(shifts - 1, sim.S.Day, "harekât erken bitmedi");
            sim.S.Ending ??= "dagilma";
            return sim;
        }

        [TestCase(29)]
        [TestCase(48)]
        public void FinalReport_StripSpansShiftOneToN_WhateverTheLength(int shifts)
        {
            var report = FinalReportBuilder.Build(Lasting(shifts));
            Assert.AreEqual(shifts, report.Shifts);
            Assert.AreEqual(1, report.Points[0].x);
            Assert.AreEqual(shifts, report.Points.Last().x, "x ekseni 1…N: kısa harekât sola sıkışmaz");
            for (int i = 1; i < report.Points.Count; i++) Assert.Greater(report.Points[i].x, report.Points[i - 1].x, "vardiyalar artan sırada");
            Assert.AreEqual(Loc.F("VARDİYA 1–{0}", shifts), report.ChartRange);
            Assert.IsTrue(report.Moments.All(m => m.Shift >= 1 && m.Shift <= shifts));
        }

        [Test]
        public void OperationReport_ShowsTrustLoss_AndWarnsWhenLow()
        {
            var sim = New();
            int max = sim.TrustMax, start = sim.S.Trust, per = sim.B.liberationCiviliansPerTrust;
            Assert.Greater(max, start, "Güven tavanı başlangıçtan yüksek.");
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true, Collateral = 3, Turns = 4, CiviliansLeft = per }).Ok);
            var report = OperationReportBuilder.Build(sim);
            var line = report.Results.Single(l => l.Label == Loc.T("Sivil güven"));
            Assert.AreEqual(OperationTone.Plain, line.Tone, "eksi mürekkep (S5 kuralı)");
            StringAssert.EndsWith($"{start - 2}/{max}", line.Value, "Kırılan 3 sunucu −3, ayakta kalanlar +1.");
            Assert.AreEqual("3", report.Results.Single(l => l.Label == Loc.T("Kırılan sivil sunucu")).Value);
            StringAssert.DoesNotContain(Loc.T("Sivil güven"), report.Sentence, "güven yüksekken uyarı yok");

            while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step();
            Assert.IsTrue(sim.StartLiberationRaid("weu_2").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = false, Collateral = sim.S.Trust - 1, Turns = 6 }).Ok);
            StringAssert.StartsWith($"Sivil güven 1/{max}.", OperationReportBuilder.Build(sim).Sentence, "güven azalınca rapor uyarır");
        }

        [Test]
        public void FinalReport_WhenTrustRunsOut_LostStampWithDisbandingReason()
        {
            var sim = New();
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = false, Collateral = sim.B.liberationTrustStart, Turns = 6 }).Ok);
            Assert.AreEqual("dagilma", sim.S.Ending);
            var over = Build(sim).FinalReport;
            Assert.IsNotNull(over);
            Assert.IsFalse(over.Won);
            Assert.AreEqual(Loc.T("KAYBEDİLDİ"), over.Stamp);
            Assert.AreEqual(DeskText.Upper(Loc.T(GameData.Ending("dagilma").Title)), over.Reason, "neden damgası");
            Assert.AreEqual(Loc.T("Sivil güven sıfıra indi; Sığınak 07 dağıldı."), over.Headline);
            Assert.AreEqual(OperationTone.Alert, over.Indicators[3].Tone, "kayıpta kalan nüfus kırmızı");
            Assert.AreEqual(Loc.T("kişi kaldı"), over.Indicators[3].Sub);
            var last = over.Moments.Last();
            Assert.AreEqual(Loc.T("Güven 0 · sığınak dağıldı"), last.Text);
            Assert.IsTrue(last.Alert, "son an kırmızı");
            StringAssert.StartsWith(Loc.F("Baskında {0} sivil sunucu kırıldı", sim.B.liberationTrustStart), over.Moments[over.Moments.Count - 2].Text, "kaybın nedeni");
        }

        [Test]
        public void FinalReport_TurningPointsMarkCounterattacks()
        {
            var sim = New();
            CaptureNext(sim);
            sim.S.Alarm = sim.B.liberationAlarmThreshold;
            sim.Step();
            sim.S.Trust = 1;
            sim.S.Alarm = sim.B.liberationAlarmThreshold;
            sim.Step();
            Assert.AreEqual("dagilma", sim.S.Ending);
            var moments = Build(sim).FinalReport.Moments;
            Assert.AreEqual(1, moments.Count(m => m.Text == Loc.F("YZ karşı saldırısı: {0} {1:00} yeniden işgal", Loc.T("Batı Avrupa"), 1)),
                string.Join(" | ", moments.Select(m => m.Text)));
            // An Türkçe yazıldı; dil değişince de sektör tanınır
            var lang = DeskLoc.Instance.Lang;
            try
            {
                DeskLoc.Instance.SetLanguage(DeskLang.En);
                moments = Build(sim).FinalReport.Moments;
                Assert.AreEqual(1, moments.Count(m => m.Text == Loc.F("YZ karşı saldırısı: {0} {1:00} yeniden işgal", Loc.T("Batı Avrupa"), 1)),
                    string.Join(" | ", moments.Select(m => m.Text)));
            }
            finally { DeskLoc.Instance.SetLanguage(lang); }
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
                foreach (string field in new[] { "finalReport", "settingsView" })
                    Assert.IsNotNull(Ref(presenter, field), field);
                var surface = Ref(presenter, "surface");
                Assert.IsInstanceOf<LiberationDeskView>(surface, "masa sığınak odasına bağlı");
                foreach (string field in new[] { "game", "source", "siginakRig", "desk", "launcher", "telex", "siginak", "siginakMap", "siginakBriefing", "siginakOperation" })
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
                foreach (string field in new[] { "finalReport", "settingsView" })
                {
                    var canvas = ((Component)Ref(presenter, field)).GetComponentInParent<Canvas>(true);
                    Assert.IsNotNull(canvas, field);
                    Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.rootCanvas.renderMode, field + ": kamera Canvas'ı oda katmanının arkasında kalır.");
                    Assert.Greater(canvas.sortingOrder, contextCanvas.sortingOrder, field + ": rapor ve ayarlar oda katmanının önünde olmalı.");
                }
            });
        }

        [Test]
        public void DeskGameScene_OperationReport_SitsLeftOfTheMap_AndIsWired()
        {
            InScene(scene =>
            {
                // Revizyon 2 · S7c/S7d: Overlay, sol 24 · üst 274, 560 px; harita pozunda açılır, odanın ekran katmanında
                var view = All<OperationReportView>(scene).Single();
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, view.GetComponentInParent<Canvas>(true).rootCanvas.renderMode);
                var sheet = (RectTransform)Ref(view, "sheet");
                Assert.AreEqual(new Vector2(0, 1), sheet.anchorMin);
                Assert.AreEqual(new Vector2(24 + 280, -274), sheet.anchoredPosition);
                Assert.AreEqual(560, sheet.sizeDelta.x, 0.5f);
                Assert.IsFalse(view.gameObject.activeSelf, "rapor yalnız baskından dönünce açılır");
                var op = All<SiginakOperation>(scene).Single();
                foreach (string field in new[] { "game", "surface", "desk", "rig", "map", "counters", "report", "tray", "launcher" })
                    Assert.IsNotNull(Ref(op, field), field);
                Assert.AreSame(op, Ref(All<SiginakDesk>(scene).Single(), "operation"), "sayaçlar rapor inene kadar bekler");
                Assert.AreSame(op, Ref(All<SiginakTutorial>(scene).Single(), "operation"));
            });
        }

        [Test]
        public void DeskGameScene_FinalReport_FullPaperOverTheMap_StripFillsItsWidth()
        {
            InScene(scene =>
            {
                // Revizyon 2 · S8: Overlay, 1620 × 952 (sol 150 · üst 64); harita karartılır, kamera harita pozuna geçer
                var view = All<FinalReportView>(scene).Single();
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, view.GetComponentInParent<Canvas>(true).rootCanvas.renderMode);
                var sheet = (RectTransform)Ref(view, "sheet");
                Assert.AreEqual(new Vector2(150, -64), sheet.anchoredPosition);
                Assert.AreEqual(new Vector2(1620, 952), sheet.sizeDelta);
                Assert.IsFalse(((GameObject)Ref(view, "root")).activeSelf, "rapor yalnız harekât bitince açılır");
                var final = All<SiginakFinal>(scene).Single();
                Assert.AreSame(view, Ref(final, "report"));
                Assert.IsNotNull(Ref(final, "rig"));
                // x = vardiya 1…N her zaman tam genişlik (29 ve 48 vardiya)
                var chart = (OccupationChart)Ref(view, "chart");
                var r = chart.rectTransform.rect;
                Assert.AreEqual(1480, r.width, 1);
                foreach (int n in new[] { 29, 48 })
                {
                    chart.SetSeries(new[] { new Vector2(1, 100), new Vector2(n, 40) }, n);
                    Assert.AreEqual(r.xMin, chart.Local(1, 0).x, 0.01f, n + ": ilk vardiya sol kenarda");
                    Assert.AreEqual(r.xMax, chart.Local(n, 0).x, 0.01f, n + ": son vardiya sağ kenarda");
                    Assert.AreEqual(70, chart.ValueAt((1 + n) / 2f), 0.01f);
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
        public void DeskGameScene_MissionSlipOnScreenLayerAndOnlyRaidUsesMonitor()
        {
            InScene(scene =>
            {
                var slip = All<OperationSlip>(scene).Single();
                var hud = scene.GetRootGameObjects().Single(g => g.name == "SiginakRoom").transform.Find("SiginakHud");
                // Revizyon 2 · S2: fiş ekran katmanında sol masa kenarında (teleksin üstünde; dünyada teleks örterdi)
                Assert.IsTrue(slip.transform.IsChildOf(hud), "görev fişi ekran katmanında");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, slip.GetComponentInParent<Canvas>(true).rootCanvas.renderMode);
                Assert.IsNotNull(slip.GetComponent<UnityEngine.UI.GraphicRaycaster>(), "iç içe canvas kendi ışınıyla tıklanır");
                Assert.Less(slip.transform.GetSiblingIndex(), hud.Find("Hover tag").GetSiblingIndex(), "üzerine gelme etiketi fişin üstünde");
                var so = new SerializedObject(slip);
                var it = so.GetIterator();
                for (bool enter = true; it.NextVisible(enter); enter = false)
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.name != "m_Script")
                        Assert.IsNotNull(it.objectReferenceValue, "fiş alanı bağlı değil: " + it.propertyPath);
                Assert.AreEqual(3, so.FindProperty("fields").arraySize);
                Assert.AreEqual(4, so.FindProperty("rewards").arraySize);
                Assert.AreEqual(4, so.FindProperty("tierGlyphs").arraySize);
                foreach (var h in slip.GetComponentsInChildren<UiHoverTarget>(true))
                    Assert.IsNotNull(new SerializedObject(h).FindProperty("hoverTag").objectReferenceValue, h.name);
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
