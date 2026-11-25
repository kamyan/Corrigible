using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Üretilmiş haritanın çizimi (anahtar 1, harita aşama 2 · C3): hücre çokgenleri doğru bölgeye gider ve yıldız biçimli kalır,
    /// kırık kenar iki komşuda aynıdır, kıyı ve sınır zincirleri Core'un kenar listeleriyle birebir tutar; sahnede
    /// <see cref="LiberationMapLayer.RegionAt"/> hücre merkezlerinde doğru bölgeyi verir ve dünya haritasına dönüş sahnedeki
    /// veriyi geri getirir. Harita Core'dan doğrudan (<see cref="WorldMap.Generate"/>) alınır; anahtar asset'te değişmez.
    /// </summary>
    public class GeneratedMapDrawingTests
    {
        const string ScenePath = DeskSpriteManifest.Root + "/Scenes/DeskGame.unity";
        static readonly uint[] Seeds = { 79, 1, 2, 5, 13, 2024 };

        static GeneratedMapGeometry Geometry(uint seed)
        {
            return GeneratedMapRenderer.BuildGeometry(WorldMap.Generate(seed));
        }

        static float EdgeLength(MapCell c) => Vector2.Distance(new Vector2((float)c.X[0], (float)c.Y[0]), new Vector2((float)c.X[1], (float)c.Y[1]));

        [TestCaseSource(nameof(Seeds))]
        public void CellPolygons_GoToTheirRegion_AndStayStarShaped(uint seed)
        {
            var g = Geometry(seed);
            var map = g.Map;
            var probes = new Dictionary<string, RegionMeshGraphic>();
            try
            {
                foreach (var r in map.Regions)
                {
                    var polys = g.RegionPolygons[r.Id];
                    Assert.AreEqual(r.Cells.Count, polys.Length, r.Id + ": hücre başına bir çokgen");
                    for (int i = 0; i < polys.Length; i++)
                    {
                        var cell = map.Cells[r.Cells[i]];
                        var p = polys[i];
                        Assert.AreEqual(GeneratedMapGeometry.RingPoints + 1, p.Vertices.Length);
                        CollectionAssert.AreEqual(new[] { 0, GeneratedMapGeometry.RingPoints }, p.RingOffsets);
                        Assert.AreEqual(GeneratedMapGeometry.Center(cell), p.Vertices[GeneratedMapGeometry.RingPoints], "son köşe hücre merkezi");
                        CollectionAssert.AreEqual(g.CellRing(cell.Index), p.Vertices.Take(GeneratedMapGeometry.RingPoints));
                        // Yelpaze: bütün üçgenler aynı yönde ve alanlı (merkezden yıldız biçimli)
                        for (int t = 0; t + 2 < p.Triangles.Length; t += 3)
                        {
                            Vector2 a = p.Vertices[p.Triangles[t]], b = p.Vertices[p.Triangles[t + 1]], c = p.Vertices[p.Triangles[t + 2]];
                            float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                            Assert.Greater(cross, 1f, seed + " hücre " + cell.Index + " üçgen " + t / 3 + ": yelpaze yönü bozuk");
                        }
                        foreach (var v in p.Vertices)
                        {
                            Assert.GreaterOrEqual(v.x, p.Bounds.x); Assert.LessOrEqual(v.x, p.Bounds.z);
                            Assert.GreaterOrEqual(v.y, p.Bounds.y); Assert.LessOrEqual(v.y, p.Bounds.w);
                        }
                    }
                    var probe = new GameObject("Ölçü " + r.Id, typeof(RectTransform)).AddComponent<RegionMeshGraphic>();
                    probe.SetPolygons(polys);
                    probes[r.Id] = probe;
                }
                // Her kara hücresinin merkezi yalnız kendi bölgesinde; deniz hücresinin merkezi hiçbir bölgede
                foreach (var c in map.Cells)
                {
                    var p = GeneratedMapGeometry.Center(c);
                    var owners = probes.Where(kv => kv.Value.ContainsMapPoint(p)).Select(kv => kv.Key).ToList();
                    if (c.Land) CollectionAssert.AreEqual(new[] { c.Region }, owners, seed + " hücre " + c.Index);
                    else CollectionAssert.IsEmpty(owners, seed + " deniz hücresi " + c.Index);
                }
            }
            finally { foreach (var p in probes.Values) Object.DestroyImmediate(p.gameObject); }
        }

        [TestCaseSource(nameof(Seeds))]
        public void BrokenEdges_AreSharedByBothNeighbours_AndStayWithinTheAmplitude(uint seed)
        {
            var g = Geometry(seed);
            var map = g.Map;
            int shared = 0;
            float widest = 0;
            foreach (var c in map.Cells)
            {
                if (!c.Land) continue;
                for (int k = 0; k < 6; k++)
                {
                    var edge = g.EdgePath(c.Index, k);
                    Assert.AreEqual(GeneratedMapGeometry.EdgeSteps + 2, edge.Count);
                    // Halkada aynı noktalar aynı sırada
                    for (int i = 0; i <= GeneratedMapGeometry.EdgeSteps; i++)
                        Assert.AreEqual(edge[i], g.CellRing(c.Index)[k * (GeneratedMapGeometry.EdgeSteps + 1) + i]);
                    // Sapma: kiriş doğrusuna dik uzaklık ≤ genlik × boy (kıyıda kıyı genliği; + köşe sapması payı)
                    Vector2 a = edge[0], b = edge[edge.Count - 1];
                    float len = (b - a).magnitude;
                    float amplitude = g.IsCoastEdge(c.Vertex[k], c.Vertex[(k + 1) % 6]) ? g.CoastAmplitude : g.EdgeAmplitude;
                    Assert.AreEqual(nb0(c, k) < 0 || !map.Cells[nb0(c, k)].Land, g.IsCoastEdge(c.Vertex[k], c.Vertex[(k + 1) % 6]), "kıyı kenarı");
                    for (int i = 1; i < edge.Count - 1; i++)
                    {
                        float d = Mathf.Abs((b.x - a.x) * (edge[i].y - a.y) - (b.y - a.y) * (edge[i].x - a.x)) / len;
                        widest = Mathf.Max(widest, d / len);
                        Assert.LessOrEqual(d, (amplitude + 0.01f) * len, seed + " hücre " + c.Index + " kenar " + k);
                    }
                    int nb = c.Neighbor[k];
                    if (nb < 0 || !map.Cells[nb].Land) continue;
                    var n = map.Cells[nb];
                    int back = Enumerable.Range(0, 6).Single(j => n.Vertex[j] == c.Vertex[(k + 1) % 6] && n.Vertex[(j + 1) % 6] == c.Vertex[k]);
                    var other = g.EdgePath(nb, back);
                    CollectionAssert.AreEqual(edge.Reverse().ToArray(), other.ToArray(), seed + " " + c.Index + "/" + nb + ": komşular aynı kırık çizgiyi görür");
                    shared++;
                }
            }
            Assert.Greater(shared, 100);
            Assert.Greater(widest, 0.05f, "kenarlar gerçekten kırılıyor (düz altıgen değil)");
            Assert.Greater(g.CoastAmplitude, g.EdgeAmplitude, "kıyı iç sınırdan hırçın (tema)");

            int nb0(MapCell cell, int k) => cell.Neighbor[k];
        }

        [TestCaseSource(nameof(Seeds))]
        public void CoastBorderAndCableLines_MatchTheCoreEdges(uint seed)
        {
            var g = Geometry(seed);
            var map = g.Map;
            int step = GeneratedMapGeometry.EdgeSteps + 1;
            // Kıyı halkaları kapalı: nokta sayısı = kenar × (ara + 1)
            Assert.AreEqual(map.Coast.Count * step, g.CoastLoops.Sum(l => l.Count), "kıyı halkaları Core'un kıyı kenarlarını tam bir kez çizer");
            Assert.AreEqual(map.Borders.Count * step, g.BorderPaths.Sum(l => l.Count - 1), "sınır zincirleri Core'un sınır kenarlarını tam bir kez çizer");
            Assert.AreEqual(map.Links.Count(l => l.Cable), g.CablePaths.Count);
            foreach (var r in map.Regions)
            {
                var outline = g.RegionOutlines[r.Id];
                Assert.IsNotEmpty(outline, r.Id);
                // Dış halka bölgenin bütün sınır kenarlarını taşır (hücrelerin bölge dışına bakan kenarları)
                int boundary = r.Cells.Sum(ci => Enumerable.Range(0, 6).Count(k =>
                {
                    int nb = map.Cells[ci].Neighbor[k];
                    return nb < 0 || !map.Cells[nb].Land || map.Cells[nb].Region != r.Id;
                }));
                Assert.AreEqual(boundary * step, outline.Sum(l => l.Count), r.Id);
                Assert.AreEqual(r.Id, Owner(g, g.TeamAnchors[r.Id]), r.Id + " ekip noktası kendi bölgesinde");
            }
            Assert.AreEqual(Simulation.HQ, Owner(g, g.ShelterPoint), "S-07 karargâhta");
            Assert.AreEqual(GeneratedMapGeometry.Center(map.Cells[map.HqCell]), g.ShelterPoint);
        }

        static string Owner(GeneratedMapGeometry g, Vector2 p)
        {
            var probe = new GameObject("Ölçü", typeof(RectTransform)).AddComponent<RegionMeshGraphic>();
            try
            {
                foreach (var kv in g.RegionPolygons)
                {
                    probe.SetPolygons(kv.Value);
                    if (probe.ContainsMapPoint(p)) return kv.Key;
                }
                return null;
            }
            finally { Object.DestroyImmediate(probe.gameObject); }
        }

        [Test]
        public void DeskGameScene_GeneratedMapIsWired_AndHiddenOnTheWorldMap()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var layer = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<LiberationMapLayer>(true)).Single();
                var lso = new SerializedObject(layer);
                var gen = (GeneratedMapRenderer)lso.FindProperty("generated").objectReferenceValue;
                Assert.IsNotNull(gen, "LiberationMapLayer.generated");
                Assert.AreSame(layer.transform.Find("S-07"), lso.FindProperty("shelterSlip").objectReferenceValue);
                var so = new SerializedObject(gen);
                foreach (var f in new[] { "paper", "borderUnder", "border", "coast", "cable", "cableEnds" })
                    Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
                Assert.AreEqual(4, so.FindProperty("halo").arraySize);
                var root = layer.transform.parent;
                // Sıra (alttan üste): kâğıt, hale, dolgular, sınır/kıyı/kablo, kâğıt dokusu, işaretler
                int Index(string n) => root.Find(n).GetSiblingIndex();
                Assert.Less(Index("MapBase"), Index("GenPaper"));
                Assert.Less(Index("GenPaper"), Index("GenHalo"));
                Assert.Less(Index("GenHalo"), Index("RegionFills"));
                Assert.Less(Index("Liberation fills"), Index("GenLines"));
                Assert.Less(Index("GenLines"), Index("PaperGrain"));
                // Dünya haritasında (sahnenin hâli) üretilmiş katmanlar kapalı, pişmiş dokular açık
                foreach (var n in new[] { "GenPaper", "GenHalo", "GenLines" }) Assert.IsFalse(root.Find(n).gameObject.activeSelf, n);
                foreach (var n in new[] { "MapBase", "MapLines" }) Assert.IsTrue(root.Find(n).gameObject.activeSelf, n);
                // Hiçbir yeni katman tık almaz (bölge dolgusu ve deniz altta kalır)
                foreach (var n in new[] { "GenPaper", "GenHalo", "GenLines" })
                    foreach (var gr in root.Find(n).GetComponentsInChildren<Graphic>(true)) Assert.IsFalse(gr.raycastTarget, gr.name);
                Assert.IsNotNull(((RawImage)so.FindProperty("paper").objectReferenceValue).texture, "kıyısız kâğıt dokusu");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void DeskGameScene_RegionAt_FindsTheCellsRegion_AndWorldMapComesBack([Values(79u, 5u, 2024u)] uint seed)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var layer = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<LiberationMapLayer>(true)).Single();
                var root = layer.transform.parent;
                var shelter = (RectTransform)layer.transform.Find("S-07");
                var sceneShelter = shelter.anchoredPosition;
                var before = layer.Regions.ToDictionary(m => m.RegionId, m => (m.Hit.PolygonCount, m.LabelPoint, m.StampPoint, layer.TeamAnchor(m.RegionId)));
                var map = WorldMap.Generate(seed);

                layer.SetMap(map);
                Assert.AreSame(map, layer.Map);
                Assert.IsNotNull(layer.Geometry);
                foreach (var n in new[] { "GenPaper", "GenHalo", "GenLines" }) Assert.IsTrue(root.Find(n).gameObject.activeSelf, n);
                foreach (var n in new[] { "MapBase", "MapLines" }) Assert.IsFalse(root.Find(n).gameObject.activeSelf, n);
                foreach (var m in layer.Regions)
                {
                    Assert.IsTrue(m.Generated, m.RegionId);
                    Assert.AreEqual(map.Region(m.RegionId).Cells.Count, m.Hit.PolygonCount, m.RegionId);
                    Assert.IsTrue(m.gameObject.activeInHierarchy, m.RegionId + " işaret kökü açık (RegionAt onu sayar)");
                }
                foreach (var c in map.Cells)
                {
                    var world = layer.MapToWorld(GeneratedMapGeometry.Center(c));
                    Assert.AreEqual(c.Land ? c.Region : null, layer.RegionAt(world), seed + " hücre " + c.Index);
                    Assert.AreEqual(GeneratedMapGeometry.Center(c).x, layer.WorldToMap(world).x, 0.01f);
                }
                foreach (var m in layer.Regions)
                    Assert.AreEqual(m.RegionId, layer.RegionAt(layer.MapToWorld(layer.TeamAnchor(m.RegionId))), m.RegionId + " ekip noktası");
                var p = layer.ShelterPoint;
                Assert.AreEqual(new Vector2(p.x, -p.y), shelter.anchoredPosition, "S-07 yerleşimin seçtiği yerde");
                var gen = (GeneratedMapRenderer)new SerializedObject(layer).FindProperty("generated").objectReferenceValue;
                var coast = (UILineGraphic)new SerializedObject(gen).FindProperty("coast").objectReferenceValue;
                Assert.AreEqual(layer.Geometry.CoastLoops.Count, coast.PathCount);

                // Aynı harita ikinci kez kurulmaz
                var g = layer.Geometry;
                layer.SetMap(map);
                Assert.AreSame(g, layer.Geometry);

                // Dünya haritasına dönüş: sahnedeki veri birebir geri gelir
                layer.SetMap(null);
                Assert.IsNull(layer.Geometry);
                foreach (var n in new[] { "GenPaper", "GenHalo", "GenLines" }) Assert.IsFalse(root.Find(n).gameObject.activeSelf, n);
                foreach (var n in new[] { "MapBase", "MapLines" }) Assert.IsTrue(root.Find(n).gameObject.activeSelf, n);
                Assert.AreEqual(sceneShelter, shelter.anchoredPosition);
                foreach (var m in layer.Regions)
                {
                    Assert.IsFalse(m.Generated, m.RegionId);
                    Assert.AreEqual(before[m.RegionId], (m.Hit.PolygonCount, m.LabelPoint, m.StampPoint, layer.TeamAnchor(m.RegionId)), m.RegionId);
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
