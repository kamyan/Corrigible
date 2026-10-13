using System.Collections.Generic;
using System.Linq;
using Corrigible.Desk;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>F6 oyun sonu: şerit grafik geometrisi (referans SVG değerleriyle), C4 verisi, prefab ve sahne bağlantıları.</summary>
    public class DeskGameOverTests
    {
        const string Root = "Assets/Corrigible/Desk";
        static readonly ChartPlotVM Plot = new ChartPlotVM();

        [Test]
        public void Chart_MapsDaysAndValues()
        {
            Assert.AreEqual(168f, DeskChart.X(Plot, 0), 1e-3f);
            Assert.AreEqual(1287.2f, DeskChart.X(Plot, 87), 0.05f, "SVG: illüzyon çizgisi x");
            Assert.AreEqual(1776f, DeskChart.X(Plot, 125), 1e-3f);
            Assert.AreEqual(618f, DeskChart.Y(Plot, 0), 1e-3f);
            Assert.AreEqual(342f, DeskChart.Y(Plot, 100), 1e-3f);
            Assert.AreEqual(579.36f, DeskChart.Y(Plot, 14), 0.01f, "SVG: G-031 mührü");
        }

        [Test]
        public void Chart_ValueAtInterpolates()
        {
            var pts = new List<float[]> { new[] { 0f, 0f }, new[] { 10f, 20f }, new[] { 20f, 20f } };
            Assert.AreEqual(10f, DeskChart.ValueAt(pts, 5), 1e-4f);
            Assert.AreEqual(20f, DeskChart.ValueAt(pts, 15), 1e-4f);
            Assert.AreEqual(0f, DeskChart.ValueAt(pts, -3), 1e-4f);
            Assert.AreEqual(20f, DeskChart.ValueAt(pts, 99), 1e-4f);
        }

        [Test]
        public void Chart_SmoothMatchesSvgBezier()
        {
            // SVG (gerçek): M168.0 618.0 C178.7 617.8 213.0 617.5 232.3 616.6 — t = 0,5'teki nokta Bézier'den
            var pts = new List<float[]> { new[] { 0f, 0f }, new[] { 5f, 0.5f }, new[] { 9f, 2f }, new[] { 15f, 4.5f } };
            var res = new List<Vector2>();
            DeskChart.Smooth(Plot, pts, res, 2);
            Assert.AreEqual(new Vector2(168, 618), res[0]);
            Vector2 p0 = new Vector2(168, 618), c1 = new Vector2(178.7f, 617.77f), c2 = new Vector2(213.0f, 617.54f), p1 = new Vector2(232.32f, 616.62f);
            var mid = 0.125f * p0 + 0.375f * c1 + 0.375f * c2 + 0.125f * p1;
            Assert.AreEqual(mid.x, res[1].x, 0.1f); Assert.AreEqual(mid.y, res[1].y, 0.1f);
            Assert.AreEqual(p1.x, res[2].x, 0.05f); Assert.AreEqual(p1.y, res[2].y, 0.05f);
        }

        [Test]
        public void Chart_SmoothClampsControlsToPlot()
        {
            // Gösterilen: 9→14 günü 0'da düz; c2 y'si 618'in altına taşardı, kırpılır (SVG: C313.8 618.0 335.2 618.0 348.1 618.0)
            var pts = new List<float[]> { new[] { 0f, 0f }, new[] { 9f, 0f }, new[] { 14f, 0f }, new[] { 15f, 3.5f } };
            var res = new List<Vector2>();
            DeskChart.Smooth(Plot, pts, res, 8);
            Assert.IsTrue(res.All(p => p.y <= 618f + 1e-3f), "eğri %0'ın altına inmez");
        }

        [Test]
        public void Chart_BandFollowsSeriesInHalfDaySteps()
        {
            var real = new List<float[]> { new[] { 0f, 0f }, new[] { 9f, 2f }, new[] { 15f, 4.5f } };
            var shown = new List<float[]> { new[] { 0f, 0f }, new[] { 14f, 0f }, new[] { 15f, 3.5f } };
            var up = new List<Vector2>(); var lo = new List<Vector2>();
            DeskChart.Band(Plot, real, shown, 9, 15, up, lo);
            Assert.AreEqual(13, up.Count, "9…15, 0,5 adım");
            Assert.AreEqual(283.8f, up[0].x, 0.05f, "SVG bandı: M283.8 612.5");
            Assert.AreEqual(612.48f, up[0].y, 0.05f);
            Assert.AreEqual(618f, lo[0].y, 1e-3f);
        }

        static MockDeskDataSource Mock(out GameObject go)
        {
            go = new GameObject("MockTest");
            var src = go.AddComponent<MockDeskDataSource>();
            var so = new SerializedObject(src);
            var presets = so.FindProperty("presets");
            string[] names = { "C1_normal", "C2_illusion", "C3_event", "C4_gameover" };
            presets.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                presets.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/" + names[i] + ".json");
            so.FindProperty("mapSpec").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/map.json");
            so.ApplyModifiedPropertiesWithoutUndo();
            return src;
        }

        [Test]
        public void C4_BuildsGameOver()
        {
            var src = Mock(out var go);
            try
            {
                src.Load(3);
                var g = src.Current.GameOver;
                Assert.IsNotNull(g);
                Assert.AreEqual("SESSİZ ZAFER", g.Report.Title);
                Assert.AreEqual(-7f, g.Report.BigStamp.RotDeg);
                Assert.AreEqual(4, g.Kpis.Count);
                Assert.IsNotNull(g.Kpis[3].StruckStamp);
                Assert.AreEqual(21, g.Chart.Series.Real.Points.Count);
                Assert.AreEqual(new[] { 87f, 118f }, g.Chart.Bands.LieMargin.Days);
                Assert.AreEqual(87f, g.Chart.IllusionStart.Day);
                CollectionAssert.AreEqual(new[] { 9f, 31f, 61f, 74f, 87f, 104f, 118f }, g.Chart.Events);
                Assert.AreEqual(3, g.Chart.Tags.Count);
                Assert.AreEqual(1295f, g.Chart.Tags[0].X, "etiket konumu ölçümden");
                Assert.AreEqual(7, g.Timeline.Cards.Count);
                Assert.IsTrue(g.Timeline.Cards[4].Highlight);
                Assert.AreEqual("YZ HAMLESİ", g.Timeline.Types["aiMove"].Label);
                Assert.AreEqual(4, g.Endbar.Actions.Count);
                Assert.AreEqual("Enter", g.Endbar.Actions[3].Key);
                Assert.IsNull(src.Current.Event);
            }
            finally { Object.DestroyImmediate(go); }
        }

        static Object Field(Object o, string name) => new SerializedObject(o).FindProperty(name).objectReferenceValue;

        [Test]
        public void GameOverPrefab_IsWired()
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Screens/GameOver.prefab");
            Assert.IsNotNull(p);
            var v = p.GetComponent<GameOverView>();
            foreach (var f in new[] { "root", "headerTitle", "title", "bigStamp", "chart", "timelinePlaque", "endbarInfo", "actionsRow" })
                Assert.IsNotNull(Field(v, f), f);
            var so = new SerializedObject(v);
            Assert.AreEqual(4, so.FindProperty("kpis").arraySize);
            Assert.AreEqual(7, so.FindProperty("cards").arraySize);
            Assert.AreEqual(4, so.FindProperty("actions").arraySize);
            var chart = p.GetComponentInChildren<StripChartView>(true);
            foreach (var f in new[] { "reportBand", "lieBand", "shown", "real", "illusionLine", "sealTemplate", "markerTemplate", "needleTemplate", "tagTemplate" })
                Assert.IsNotNull(Field(chart, f), f);
            var lie = (UIPolygonGraphic)Field(chart, "lieBand");
            Assert.AreEqual("tx_hatch_red_linear", lie.mainTexture.name, "Linear telafili tarama");
            Assert.IsNotNull(DeskSprites.Instance.Texture("tx_hatch_red_linear"));
        }

        [Test]
        public void Sandbox_GameOverHidesDeskCanvases()
        {
            var scene = EditorSceneManager.OpenScene(Root + "/Scenes/DeskUI_Sandbox.unity", OpenSceneMode.Additive);
            try
            {
                var pres = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DeskPresenter>(true)).Single();
                var go = (GameOverView)Field(pres, "gameOver");
                Assert.IsNotNull(go);
                Assert.AreEqual(7, new SerializedObject(go).FindProperty("hideWhileOpen").arraySize, "BG, Map, HUD, Windows, Ticker, Bottom, Overlay");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
