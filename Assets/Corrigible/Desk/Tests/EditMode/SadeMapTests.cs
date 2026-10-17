using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>Haritanın izdüşüm, farklı ekran oranı ve delikli çokgen sözleşmeleri.</summary>
    public class SadeMapTests
    {
        [TestCase(127, 37.5, 1598.951, 392.344)]
        [TestCase(-88, 40.5, 245.508, 370.263)]
        [TestCase(-36, 47, 572.852, 320.277)]
        [TestCase(25.5, 0, 960, 640)]
        public void MillerProjection_MatchesReferenceSamples(double longitude, double latitude, double x, double y)
        {
            Vector2 point = MapProjection.ToMap(longitude, latitude);
            Assert.That(point.x, Is.EqualTo(x).Within(0.002));
            Assert.That(point.y, Is.EqualTo(y).Within(0.002));
            Assert.AreEqual(point, MapProjection.ToMap(longitude + 360, latitude));
        }

        [TestCase(1920, 1080, 1)]
        [TestCase(1920, 1200, 1.111111f)]
        [TestCase(2580, 1080, 1.34375f)]
        public void Cover_KeepsMapCenteredAndRoundTripsMarkers(float width, float height, float scale)
        {
            var size = new Vector2(width, height);
            Assert.That(MapCoverFitter.CoverScale(size), Is.EqualTo(scale).Within(0.00001));
            Assert.AreEqual(size / 2, MapCoverFitter.MapToCanvas(MapProjection.DesignSize / 2, size));
            var designPoint = new Vector2(1473, 410.3f);
            Vector2 screenPoint = MapCoverFitter.MapToCanvas(designPoint, size);
            Assert.That(Vector2.Distance(designPoint, MapCoverFitter.CanvasToMap(screenPoint, size)), Is.LessThan(0.0002));
            Assert.GreaterOrEqual(MapProjection.DesignWidth * scale + 0.01f, width);
            Assert.GreaterOrEqual(MapProjection.DesignHeight * scale + 0.01f, height);
        }

        [Test]
        public void ReferenceData_PreservesTwelveRegionsAndPreparedTriangleIndices()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Sade/map_regions.json");
            Assert.NotNull(asset);
            var data = SadeMapData.Parse(asset.text);
            Assert.AreEqual(12, data.Regions.Length);
            Assert.AreEqual(12, data.Regions.Select(x => x.Id).Distinct().Count());
            Assert.AreEqual(-46, data.FindRegion("DA").Label.Y - data.FindRegion("DA").Node[1], 0.001);
            Assert.AreEqual(33, data.FindRegion("AV").Label.Y - data.FindRegion("AV").Node[1], 0.001);
            foreach (var region in data.Regions)
                foreach (var polygon in region.Polygons)
                {
                    int count = polygon.Rings.Sum(ring => ring.Length);
                    Assert.AreEqual(0, polygon.Triangles.Length % 3, region.Id);
                    foreach (int triangle in polygon.Triangles) Assert.That(triangle, Is.InRange(0, count - 1), region.Id);
                }
            Assert.AreEqual(4, data.Overlay.Threads.Items.Length);
            Assert.AreEqual(3, data.LonLatSamples.Count);
        }

        [Test]
        public void RegionRaycast_ExcludesHoleSeaAndSpaceBetweenIslands()
        {
            var go = new GameObject("Delikli bölge", typeof(RectTransform), typeof(RegionMeshGraphic));
            try
            {
                var graphic = go.GetComponent<RegionMeshGraphic>();
                Assert.NotNull(go.GetComponent<CanvasRenderer>(), "Bölge dolgusu yeni nesnede de çizilebilir olmalı.");
                graphic.Configure(HoledRegion());
                Assert.IsTrue(graphic.ContainsMapPoint(new Vector2(1, 1)));
                Assert.IsTrue(graphic.ContainsMapPoint(new Vector2(0, 5)));
                Assert.IsFalse(graphic.ContainsMapPoint(new Vector2(5, 5)));
                Assert.IsFalse(graphic.ContainsMapPoint(new Vector2(3, 5)));
                Assert.IsFalse(graphic.ContainsMapPoint(new Vector2(-1, 5)));
                Assert.IsFalse(graphic.ContainsMapPoint(new Vector2(15, 5)));
                Assert.IsTrue(graphic.ContainsMapPoint(new Vector2(21, 1)));
                Assert.IsTrue(graphic.ContainsMapPoint(new Vector2(9, 9)));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void PreparedRegion_SurvivesSerializedReloadAndKeepsDesignTexturePhase()
        {
            var original = new GameObject("İlk bölge", typeof(RectTransform), typeof(CanvasRenderer), typeof(RegionMeshGraphic));
            var restored = new GameObject("Yüklenen bölge", typeof(RectTransform), typeof(CanvasRenderer), typeof(RegionMeshGraphic));
            var texture = new Texture2D(4, 4);
            try
            {
                var graphic = original.GetComponent<RegionMeshGraphic>();
                graphic.Configure(HoledRegion(), texture, 5);
                var copy = restored.GetComponent<RegionMeshGraphic>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(graphic), copy);
                Assert.AreEqual("test", copy.RegionId);
                Assert.AreEqual(2, copy.PolygonCount);
                Assert.IsTrue(copy.ContainsMapPoint(new Vector2(1, 1)));
                Assert.IsFalse(copy.ContainsMapPoint(new Vector2(5, 5)));

                ((RectTransform)restored.transform).sizeDelta = MapProjection.DesignSize;
                using (var mesh = new VertexHelper())
                {
                    typeof(RegionMeshGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Invoke(copy, new object[] { mesh });
                    Assert.AreEqual(12, mesh.currentVertCount);
                    Assert.AreEqual(30, mesh.currentIndexCount);
                    var vertex = new UIVertex();
                    mesh.PopulateUIVertex(ref vertex, 2);
                    Assert.AreEqual(new Vector3(-950, 530), vertex.position);
                    Assert.AreEqual(2, vertex.uv0.x, 0.0001);
                    Assert.AreEqual(-2, vertex.uv0.y, 0.0001);
                }
            }
            finally
            {
                Object.DestroyImmediate(original);
                Object.DestroyImmediate(restored);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ThreadVisibility_FollowsControllerAcrossSeparateRenderLayers()
        {
            var root = new GameObject("İplik görünümü", typeof(SadeSpreadThreadView));
            var layer = new GameObject("Ayrı çizim katmanı", typeof(RectTransform));
            try
            {
                var line = new GameObject("Çizgi", typeof(RectTransform), typeof(UILineGraphic)).GetComponent<UILineGraphic>();
                var shadow = new GameObject("Gölge", typeof(RectTransform), typeof(UILineGraphic)).GetComponent<UILineGraphic>();
                var arrow = new GameObject("Ok", typeof(RectTransform)).GetComponent<RectTransform>();
                line.transform.SetParent(layer.transform, false);
                shadow.transform.SetParent(layer.transform, false);
                arrow.SetParent(layer.transform, false);
                var so = new SerializedObject(root.GetComponent<SadeSpreadThreadView>());
                so.FindProperty("line").objectReferenceValue = line;
                so.FindProperty("shadow").objectReferenceValue = shadow;
                so.FindProperty("arrow").objectReferenceValue = arrow;
                so.ApplyModifiedPropertiesWithoutUndo();
                root.SetActive(false);
                Assert.IsFalse(line.gameObject.activeSelf);
                Assert.IsFalse(shadow.gameObject.activeSelf);
                Assert.IsFalse(arrow.gameObject.activeSelf);
                root.SetActive(true);
                Assert.IsTrue(line.gameObject.activeSelf);
                Assert.IsTrue(shadow.gameObject.activeSelf);
                Assert.IsTrue(arrow.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(layer);
            }
        }

        static SadeMapRegion HoledRegion() => new SadeMapRegion
        {
            Id = "test",
            Polygons = new[]
            {
                new SadeMapPolygon
                {
                    Rings = new[]
                    {
                        new[] { new[] { 0f, 0f }, new[] { 10f, 0f }, new[] { 10f, 10f }, new[] { 0f, 10f } },
                        new[] { new[] { 3f, 3f }, new[] { 3f, 7f }, new[] { 7f, 7f }, new[] { 7f, 3f } }
                    },
                    Triangles = new[] { 0, 1, 7, 0, 7, 4, 1, 2, 6, 1, 6, 7, 2, 3, 5, 2, 5, 6, 3, 0, 4, 3, 4, 5 }
                },
                new SadeMapPolygon
                {
                    Rings = new[] { new[] { new[] { 20f, 0f }, new[] { 22f, 0f }, new[] { 22f, 2f }, new[] { 20f, 2f } } },
                    Triangles = new[] { 0, 1, 2, 0, 2, 3 }
                }
            }
        };
    }
}
