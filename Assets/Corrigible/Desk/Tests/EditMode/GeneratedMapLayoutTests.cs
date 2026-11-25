using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Corrigible.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Kind = Corrigible.Desk.GeneratedMapLayout.Kind;
using Box = Corrigible.Desk.GeneratedMapLayout.Box;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Üretilmiş haritanın otomatik yerleşimi (C4; tasarım §6, §11): 100 tohumda, iki dilde ayrı ayrı ölçülen adlarla hiçbir
    /// kutu aynı anda görünebileceği bir kutuyla çakışmaz, her ekip noktası kendi bölgesinin içindedir, bütün kutular harita
    /// alanındadır ve punto alt sınırın altına inmez. Karargâh (oyuncunun ilk ekranı): S-07 karargâhın karasında; cephe bloğu
    /// başka bölgenin karasında, şerit kendi konturunun üstünde ve ad (iki dilde) karadan taşan karargâh oranı sınırlı. Ölçüm
    /// sahnedeki etiketin TMP'siyle yapılır (sahne eklemeli açılır, kaydedilmeden kapanır). Görünürlük kuralı burada yerleşimden
    /// bağımsız, durumlar üzerinden yeniden kurulur.
    /// </summary>
    public sealed class GeneratedMapLayoutTests
    {
        const string ScenePath = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";
        const int Seeds = 100;

        Scene scene;
        LiberationMapLayer layer;
        GeneratedMapLayout.Metrics metrics;
        GeneratedMapLayout.Measure measure;

        [OneTimeSetUp]
        public void OpenScene()
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            layer = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<LiberationMapLayer>(true)).Single();
            // Oyundaki ölçüler: harita uygulanınca lejantın kablo satırı açılır, ölçüler ve lejant kutusu yerleşime girer
            layer.SetMap(WorldMap.Generate(79));
            metrics = layer.Layout.Sizes;
            measure = GeneratedMapRenderer.MeasureWith(layer.Regions);
        }

        [OneTimeTearDown]
        public void CloseScene()
        {
            if (layer != null) layer.SetMap(null);
            if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }

        enum St { Occupied, Candidate, Free, Front }

        /// <summary>Durumda kutu görünür mü (LiberationRegionMarks.Bind ve LiberationMapBuilder.Team'e göre).</summary>
        static bool Visible(Kind k, string region, St st, string team)
        {
            switch (k)
            {
                case Kind.Label: return true;
                case Kind.Percent: return st == St.Occupied || st == St.Candidate;
                case Kind.Candidate: return st == St.Candidate;
                case Kind.Ticks: case Kind.Stamp: return st == St.Free;
                case Kind.FrontStrip: case Kind.FrontFields: case Kind.FrontReoccupied: case Kind.FrontPin: return st == St.Front;
                case Kind.Team: return team == region;
                default: return true;
            }
        }

        /// <summary>Bölgenin olası durumları: cephe açık ya da özgürleşmiş; karargâh ilk cepheden sonra hep özgür.</summary>
        static IEnumerable<St> States(string region, string front, string hq)
        {
            if (region == front) { yield return St.Front; yield return St.Free; yield break; }
            if (region == hq) { yield return St.Free; yield break; }
            yield return St.Occupied; yield return St.Candidate; yield return St.Free;
        }

        /// <summary>
        /// İki kutu bir oyun anında birlikte görünebilir mi? Cephe her bölge olabilir; figür cephede ya da karargâhta durur.
        /// Tasarım gereği yan yana duranlar (ad ile altı, ad ile cephe raptiyesi, yüzde ile ADAY) ve figürün kablo raptiyesinin
        /// üstünde durması sayılmaz.
        /// </summary>
        static bool CanCoexist(in Box a, in Box b, string hq, IEnumerable<string> ids)
        {
            // Karargâh hiç işgal/aday olmaz: yüzdesi ve ADAY'ı hiç görünmez
            if (a.Region == hq && (a.Kind == Kind.Percent || a.Kind == Kind.Candidate) || b.Region == hq && (b.Kind == Kind.Percent || b.Kind == Kind.Candidate)) return false;
            if (a.Region == null || b.Region == null)
            {
                if (a.Region == null && b.Region == null) return false;
                var f = a.Region == null ? a : b;
                var o = a.Region == null ? b : a;
                return !(f.Kind == Kind.CablePin && o.Kind == Kind.Team);
            }
            if (a.Region == b.Region && Stacked(a.Kind, b.Kind)) return false;
            foreach (var front in ids)
                foreach (var team in new[] { front, hq }.Distinct())
                    foreach (var sa in States(a.Region, front, hq))
                        foreach (var sb in a.Region == b.Region ? new[] { sa } : States(b.Region, front, hq))
                            if (Visible(a.Kind, a.Region, sa, team) && Visible(b.Kind, b.Region, sb, team)) return true;
            return false;
        }

        static bool Stacked(Kind x, Kind y)
        {
            bool Under(Kind k) => k == Kind.Percent || k == Kind.Candidate || k == Kind.Ticks;
            return x == Kind.Label && (Under(y) || y == Kind.FrontPin) || y == Kind.Label && (Under(x) || x == Kind.FrontPin)
                || x == Kind.Percent && y == Kind.Candidate || x == Kind.Candidate && y == Kind.Percent;
        }

        /// <summary>Kutunun kendi bölgesinde ve başka bölgenin karasında kalan payı (6 px ızgara, 4 px içe; kesin halkalarla).</summary>
        static (float own, float foreign) Own(GeneratedMapGeometry g, Rect r, string id)
        {
            int own = 0, foreign = 0, n = 0;
            for (float y = r.yMin + 4; y <= r.yMax - 4; y += 6)
                for (float x = r.xMin + 4; x <= r.xMax - 4; x += 6)
                {
                    n++;
                    var q = g.RegionAt(new Vector2(x, y));
                    if (q == id) own++;
                    else if (q != null) foreign++;
                }
            return n == 0 ? (1f, 0f) : (own / (float)n, foreign / (float)n);
        }

        static float EllipseOwn(GeneratedMapGeometry g, Vector2 c, Vector2 radii, string id)
        {
            int own = 0, n = 0;
            for (int iy = -2; iy <= 2; iy++)
                for (int ix = -3; ix <= 3; ix++)
                {
                    float u = ix / 3f, v = iy / 2f;
                    if (u * u + v * v > 1.0001f) continue;
                    n++;
                    if (g.RegionAt(c + new Vector2(u * radii.x, v * radii.y)) == id) own++;
                }
            return own / (float)n;
        }

        /// <summary>Kutu <paramref name="pad"/> kadar büyütülünce dört köşesi ve dört kenar ortası bölgede mi (kesin halkalarla)?</summary>
        static bool PaddedInside(GeneratedMapGeometry g, Rect r, float pad, string id)
        {
            float x0 = r.xMin - pad, x1 = r.xMax + pad, y0 = r.yMin - pad, y1 = r.yMax + pad, xm = (x0 + x1) * 0.5f, ym = (y0 + y1) * 0.5f;
            return new[] { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x0, y1), new Vector2(x1, y1), new Vector2(xm, y0), new Vector2(xm, y1), new Vector2(x0, ym), new Vector2(x1, ym) }
                .All(q => g.RegionAt(q) == id);
        }

        /// <summary>Bölgeden kara komşularına giden aday oklarının çizgileri (gövde ve başın iki kanadı; SetFrontier ile aynı).</summary>
        static IEnumerable<(Vector2 from, Vector2 to)> ArrowsOf(GeneratedMapGeometry g, WorldMap map, string id, GeneratedMapLayout.Metrics m)
        {
            foreach (var l in map.Links)
            {
                if (l.Cable) continue;
                string other = l.A == id ? l.B : l.B == id ? l.A : null;
                if (other == null) continue;
                var edge = g.MiddleEdge(id, other, out int fromCell, out int toCell);
                if (edge == null) continue;
                var mid = g.EdgeMiddle(edge.V1, edge.V2);
                var d = (GeneratedMapGeometry.Center(map.Cells[toCell]) - GeneratedMapGeometry.Center(map.Cells[fromCell])).normalized;
                var normal = new Vector2(-d.y, d.x);
                var tip = mid + d * (m.FrontierLength * 0.5f);
                yield return (mid - d * (m.FrontierLength * 0.5f), tip);
                yield return (tip - d * m.FrontierHead + normal * m.FrontierHead * 0.7f, tip);
                yield return (tip - d * m.FrontierHead - normal * m.FrontierHead * 0.7f, tip);
            }
        }

        /// <summary>Doğru parçası dikdörtgene değiyor mu (örnekleme, 2 px)?</summary>
        static bool SegmentHits(Vector2 a, Vector2 b, Rect r)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(a, b) / 2));
            for (int i = 0; i <= n; i++) if (r.Contains(Vector2.Lerp(a, b, i / (float)n))) return true;
            return false;
        }

        static bool Overlap(Rect a, Rect b) => a.xMin < b.xMax - 0.5f && b.xMin < a.xMax - 0.5f && a.yMin < b.yMax - 0.5f && b.yMin < a.yMax - 0.5f;

        [Test]
        public void HundredSeeds_BothLanguages_NoOverlap_TeamsInTheirRegion_EverythingOnTheMap()
        {
            var failures = new List<string>();
            var stats = new StringBuilder();
            int restarts = 0, twoLines = 0, stampSmall = 0, regions = 0, labelOut = 0, frontForeign = 0, teamOut = 0;
            int hqFrontForeign = 0, hqStraddle = 0, hqLabelOut = 0, shelterMoved = 0;
            int hqLabelContour = 0, hqTeamOut = 0, hqPinOut = 0, hqStrict = 0, shelterContour = 0, shelterArrow = 0;
            var tiers = new Dictionary<float, int>();
            var sides = new Dictionary<LiberationRegionMarks.StripSide, int>();
            double total = 0, worst = 0;
            float minFont = metrics.MinFont;
            Assert.GreaterOrEqual(minFont, 28, "punto alt sınırı (A10: K2'de ≥ 14 ekran pikseli)");
            var area = new Rect(0, 0, (float)WorldMap.Width, (float)WorldMap.Height);
            for (uint seed = 1; seed <= Seeds; seed++)
            {
                var map = WorldMap.Generate(seed);
                var geometry = GeneratedMapRenderer.BuildGeometry(map);
                var names = GeneratedMapRenderer.Names(map);
                var sw = Stopwatch.StartNew();
                var layout = GeneratedMapLayout.Compute(map, geometry, names, measure, metrics);
                double ms = sw.Elapsed.TotalMilliseconds;
                total += ms; worst = System.Math.Max(worst, ms);
                restarts += layout.Restarts;
                if (!layout.Clean) failures.Add($"tohum {seed}: yerleşim temiz değil");
                var ids = map.Regions.Select(r => r.Id).ToList();
                Assert.AreEqual(12, layout.Placements.Count, "tohum " + seed);
                var fixedBoxes = layout.Boxes.Where(b => b.Region == null).ToList();
                Assert.IsTrue(fixedBoxes.Any(b => b.Kind == Kind.Shelter), "S-07 listede");
                // S-07 HqCell'e kilitli değil: karargâhın karasında, kutusu sabit listede
                var shelterBox = fixedBoxes.Single(b => b.Kind == Kind.Shelter).Rect;
                Assert.Less(Vector2.Distance(layout.ShelterPoint, shelterBox.center), 0.01f, "tohum " + seed + ": S-07 kutusu yerleşimin noktasında");
                Assert.AreEqual(map.Hq, geometry.RegionAt(layout.ShelterPoint), "tohum " + seed + ": S-07 karargâhta");
                Assert.GreaterOrEqual(Own(geometry, shelterBox, map.Hq).own, 0.9f, "tohum " + seed + ": S-07 kutusu karargâhın karasında");
                if (geometry.CellAt(layout.ShelterPoint) != map.HqCell) shelterMoved++;
                // S-07 konturdan payla içeride: kutu 6 px büyütülünce köşeleri ve kenar ortaları da karargâhta
                if (!PaddedInside(geometry, shelterBox, 6, map.Hq)) shelterContour++;
                // S-07 karargâhın komşularına giden aday oklarına değmiyor (karargâh özgür, komşu aday)
                if (ArrowsOf(geometry, map, map.Hq, metrics).Any(a => SegmentHits(a.from, a.to, shelterBox))) shelterArrow++;
                {
                    // Karargâh: cephe bloğu başka bölgenin karasında değil, şerit kendi konturunu kesmiyor; figür tabanı ve raptiye
                    // kendi karasında
                    var hp = layout.Of(map.Hq);
                    GeneratedMapLayout.FrontRects(hp.Label, hp.LabelBox, hp.StripSide, hp.StripOffset, hp.ReoccupiedOffset, hp.PinRight, metrics, out var hs, out var hf, out _, out var hpin);
                    var cs = Own(geometry, hs, map.Hq);
                    if (cs.foreign > 0.2f || Own(geometry, hf, map.Hq).foreign > 0.2f) hqFrontForeign++;
                    if (cs.own > 0.06f && cs.own < 0.94f) hqStraddle++;
                    // Figür tabanı koyu konturun içinde: 6 px büyütülmüş elipsin %90'ı karargâhta (kontur sınırın 5 px içinde)
                    if (EllipseOwn(geometry, hp.Team, metrics.FigureBase * 0.5f + Vector2.one * 6, map.Hq) < 0.9f) hqTeamOut++;
                    // Raptiye başı koyu konturun içinde, arada boşlukla: merkezden 18 px'lik çemberin 16 noktası karargâhta (kontur sınırın 5 px içinde)
                    if (!Enumerable.Range(0, 16).All(k => geometry.RegionAt(hpin.center + new Vector2(Mathf.Cos(k * Mathf.PI / 8), Mathf.Sin(k * Mathf.PI / 8)) * 18) == map.Hq)) hqPinOut++;
                    if (hp.HqStrict) hqStrict++;
                    // Ad (iki dilin büyüğü, çip dahil) koyu konturdan uzak: kutu 3 px büyütülünce köşeleri ve kenar ortaları karargâhta
                    if (!PaddedInside(geometry, GeneratedMapLayout.LabelRect(hp.Label, hp.LabelBox, metrics), 3, map.Hq)) hqLabelContour++;
                }
                Assert.AreEqual(map.Links.Count(l => l.Cable) * 2, fixedBoxes.Count(b => b.Kind == Kind.CablePin), "kablo raptiyeleri listede");

                foreach (var p in layout.Placements.Values)
                {
                    tiers[p.FontSize] = tiers.TryGetValue(p.FontSize, out var n) ? n + 1 : 1;
                    sides[p.StripSide] = sides.TryGetValue(p.StripSide, out var s) ? s + 1 : 1;
                    if (p.StampScale < 1) stampSmall++;
                    if (p.FontSize < minFont) failures.Add($"tohum {seed} {p.Id}: punto {p.FontSize} < {minFont}");
                    if (geometry.RegionAt(p.Team) != p.Id) failures.Add($"tohum {seed} {p.Id}: ekip noktası {p.Team} bölgenin dışında ({geometry.RegionAt(p.Team) ?? "deniz"})");
                    if (map.Cells[p.LabelCell].Region != p.Id) failures.Add($"tohum {seed} {p.Id}: etiket hücresi başka bölgenin");
                    // Okunurluk (kesin halkalarla, 6 px ızgara, 4 px içe): ad kendi karasında, cephe bloğu başka bölgenin karasında
                    // değil, figür tabanı kendi karasında
                    regions++;
                    if (Own(geometry, GeneratedMapLayout.LabelRect(p.Label, p.LabelBox, metrics), p.Id).own < 0.9f) labelOut++;
                    GeneratedMapLayout.FrontRects(p.Label, p.LabelBox, p.StripSide, p.StripOffset, p.ReoccupiedOffset, p.PinRight, metrics, out var strip, out var fields, out _, out _);
                    if (Own(geometry, strip, p.Id).foreign > 0.2f || Own(geometry, fields, p.Id).foreign > 0.2f) frontForeign++;
                    if (EllipseOwn(geometry, p.Team, metrics.FigureBase * 0.5f, p.Id) < 0.7f) teamOut++;
                    foreach (var o in metrics.Occluders)
                        foreach (var b in GeneratedMapLayout.BoxesOf(p, metrics))
                            if (b.Kind != Kind.Team && !(p.Id == map.Hq && (b.Kind == Kind.Percent || b.Kind == Kind.Candidate)) && Overlap(b.Rect, o)) failures.Add($"tohum {seed}: {b} örtücünün ({o}) altında");
                }

                for (int lang = 0; lang < 2; lang++)
                {
                    // Bu dilin ad kutuları ayrı ölçülür; öteki kutular dilden bağımsız (yerleşim iki dilin büyüğüyle yapıldı)
                    var list = new List<Box>(fixedBoxes);
                    foreach (var p in layout.Placements.Values)
                    {
                        var size = GeneratedMapLayout.LabelSize(names[p.Id][lang], p.FontSize, p.MaxLineWidth, measure);
                        if (lang == 0 && size.y > p.FontSize * 1.8f) twoLines++;
                        if (p.Id == map.Hq && Own(geometry, GeneratedMapLayout.LabelRect(p.Label, size, metrics), p.Id).own < 0.9f) hqLabelOut++;
                        if (size.x > p.LabelBox.x + 0.01f || size.y > p.LabelBox.y + 0.01f)
                            failures.Add($"tohum {seed} {p.Id} dil {lang}: ad kutusu {size} yerleşimin kutusunu ({p.LabelBox}) aşıyor");
                        foreach (var b in GeneratedMapLayout.BoxesOf(p, metrics))
                            list.Add(b.Kind == Kind.Label ? new Box(p.Id, Kind.Label, GeneratedMapLayout.LabelRect(p.Label, size, metrics)) : b);
                    }
                    foreach (var b in list)
                        if (b.Kind != Kind.Legend && b.Kind != Kind.Occluder && !(b.Region == map.Hq && (b.Kind == Kind.Percent || b.Kind == Kind.Candidate))
                            && !(area.Contains(b.Rect.min) && area.Contains(b.Rect.max)))
                            failures.Add($"tohum {seed} dil {lang}: {b} harita alanının dışında");
                    for (int i = 0; i < list.Count; i++)
                        for (int j = i + 1; j < list.Count; j++)
                            if (Overlap(list[i].Rect, list[j].Rect) && CanCoexist(list[i], list[j], map.Hq, ids))
                                failures.Add($"tohum {seed} dil {lang}: {list[i]} × {list[j]}");
                }
            }
            stats.Append($"yerleşim: ort {total / Seeds:0.0} ms, en kötü {worst:0.0} ms, yeniden başlama {restarts}; punto ");
            foreach (var kv in tiers.OrderByDescending(k => k.Key)) stats.Append($"{kv.Key}:{kv.Value} ");
            stats.Append($"· iki satır (TR) {twoLines} · şerit ");
            foreach (var kv in sides) stats.Append($"{kv.Key}:{kv.Value} ");
            stats.Append($"· küçük damga {stampSmall} · ad karadan taşan {labelOut}/{regions} · başka karada cephe {frontForeign} · taban dışarıda {teamOut}");
            stats.Append($" · karargâh: başka karada cephe {hqFrontForeign}/{Seeds}, konturu kesen şerit {hqStraddle}, taşan ad {hqLabelOut}/{2 * Seeds} (iki dil), S-07 HqCell dışında {shelterMoved}");
            stats.Append($", ad kontura yakın {hqLabelContour}, figür tabanı konturda {hqTeamOut}, raptiye konturda {hqPinOut}, en sıkı kademe {hqStrict}, S-07 kontura yakın {shelterContour}, S-07 oka değen {shelterArrow}");
            UnityEngine.Debug.Log("[GeneratedMapLayoutTests] " + stats);
            // Unity'de 100 tohum %2,9 / %8,9 / %4,2; Mono aynasında 300 tohum %3,3 / %9,5 / %3,9 (eski yerleşim %11 / %31 / %6).
            // Sınırlar gerilemeyi yakalar
            if (labelOut > regions * 0.1f) failures.Add($"adın %10'dan fazlası kendi karasının dışına taşıyor ({labelOut}/{regions})");
            if (frontForeign > regions * 0.15f) failures.Add($"cephe bloğu çok sık başka bölgenin karasında ({frontForeign}/{regions})");
            if (teamOut > regions * 0.08f) failures.Add($"figür tabanı çok sık kendi karasının dışında ({teamOut}/{regions})");
            // Karargâh (üretici 4 alan şartı + S-07 serbest; Mono aynası 600 tohum: %0,3 / %5,0 / %0; eski %27 / %23 / %13)
            if (hqFrontForeign >= Seeds * 0.05f) failures.Add($"karargâhın cephe bloğu %5'ten sık başka bölgenin karasında ({hqFrontForeign}/{Seeds})");
            if (hqStraddle >= Seeds * 0.08f) failures.Add($"karargâhın şeridi %8'den sık kendi konturunu kesiyor ({hqStraddle}/{Seeds})");
            if (hqLabelOut >= 2 * Seeds * 0.05f) failures.Add($"karargâhın adı %5'ten sık karadan taşıyor ({hqLabelOut}/{2 * Seeds})");
            // Karargâhın figürü, raptiyesi ve S-07'si konturdan payla (sıkı kademeler, S-07 kuralları; Mono aynası 600 tohum: figür
            // %1,8, raptiye %3,2, S-07 %2,8, okta %0,7, ad %0,3; önce %50 / %55 / %44 / %12 / %57)
            if (hqLabelContour >= Seeds * 0.05f) failures.Add($"karargâhın adı %5'ten sık konturun üstünde ({hqLabelContour}/{Seeds})");
            if (hqTeamOut >= Seeds * 0.08f) failures.Add($"karargâhın figür tabanı %8'den sık konturun üstünde ({hqTeamOut}/{Seeds})");
            if (hqPinOut >= Seeds * 0.15f) failures.Add($"karargâhın raptiyesi %15'ten sık konturun üstünde ({hqPinOut}/{Seeds})");
            if (shelterContour >= Seeds * 0.05f) failures.Add($"S-07 %5'ten sık karargâh konturuna yakın ({shelterContour}/{Seeds})");
            if (shelterArrow >= Seeds * 0.05f) failures.Add($"S-07 %5'ten sık aday okuna değiyor ({shelterArrow}/{Seeds})");
            Assert.IsEmpty(failures, string.Join("\n", failures.Take(40)) + (failures.Count > 40 ? $"\n… {failures.Count} hata" : ""));
        }

        [Test]
        public void SameMap_SameLayout()
        {
            var map = WorldMap.Generate(13);
            var a = GeneratedMapLayout.Compute(map, GeneratedMapRenderer.BuildGeometry(map), GeneratedMapRenderer.Names(map), measure, metrics);
            var b = GeneratedMapLayout.Compute(map, GeneratedMapRenderer.BuildGeometry(map), GeneratedMapRenderer.Names(map), measure, metrics);
            foreach (var p in a.Placements.Values)
            {
                var q = b.Of(p.Id);
                Assert.AreEqual((p.Label, p.FontSize, p.StripSide, p.StripOffset, p.Stamp, p.StampScale, p.Team), (q.Label, q.FontSize, q.StripSide, q.StripOffset, q.Stamp, q.StampScale, q.Team), p.Id);
            }
        }

        [Test]
        public void ExclusiveRule_MatchesTheVisibilityOfGameStates()
        {
            // Yerleşimin kuralı test kuralından gevşek olmamalı: test kuralının birlikte gösterdiği her çift yerleşimde de dışlanır
            var map = WorldMap.Generate(79);
            var ids = map.Regions.Select(r => r.Id).ToList();
            var kinds = new[] { Kind.Label, Kind.Percent, Kind.Candidate, Kind.Ticks, Kind.Stamp, Kind.FrontStrip, Kind.FrontFields, Kind.FrontReoccupied, Kind.FrontPin, Kind.Team };
            var regions = new[] { map.Hq, ids.First(i => i != map.Hq), ids.Last(i => i != map.Hq) };
            var r = new Rect(0, 0, 10, 10);
            foreach (var ra in regions)
                foreach (var rb in regions)
                    foreach (var ka in kinds)
                        foreach (var kb in kinds)
                        {
                            if (ra == rb && ka == kb) continue;
                            // Cephe bloğunun kendi parçaları kurala girmez: yerleşim onları ayrıca ayrık kurar (100 tohum testi ölçer)
                            if (ra == rb && GeneratedMapLayout.IsFront(ka) && GeneratedMapLayout.IsFront(kb)) continue;
                            var a = new Box(ra, ka, r);
                            var b = new Box(rb, kb, r);
                            if (CanCoexist(a, b, map.Hq, ids))
                                Assert.IsTrue(GeneratedMapLayout.Exclusive(a, b, map.Hq), $"{a} × {b}");
                        }
            Assert.IsFalse(GeneratedMapLayout.Exclusive(new Box("na", Kind.FrontStrip, r), new Box("sa", Kind.FrontStrip, r), "weu"), "tek cephe");
            Assert.IsFalse(GeneratedMapLayout.Exclusive(new Box("na", Kind.Team, r), new Box("sa", Kind.Team, r), "weu"), "tek figür");
            Assert.IsTrue(GeneratedMapLayout.Exclusive(new Box("weu", Kind.Team, r), new Box("sa", Kind.FrontStrip, r), "weu"), "karargâhta bekleyen figür");
        }

        [Test]
        /// <summary>
        /// Yerleşim modelinin kutuları sahnede gerçekten çizilenleri kapsar (İnceleme S1 · O3): her bölge her durumda (işgal, aday,
        /// özgür, cephe + YENİDEN İŞGAL) bağlanır, kökün altındaki her açık yazı ve işaretin harita dikdörtgeni o bölgenin
        /// <see cref="GeneratedMapLayout.BoxesOf"/> kutularından birinin içinde kalır (±3 px). Biri <c>LiberationRegionMarks.Below</c>'da
        /// bir ofseti değiştirip modeli unutursa burada kırılır.
        /// </summary>
        [TestCase(2u)]
        [TestCase(13u)]
        public void DrawnMarks_StayInsideTheirLayoutBoxes(uint seed)
        {
            var sim = Simulation.NewGame(seed, new Balance { shelterEventsEnabled = false, liberationMapGen = 1 });
            layer.SetMap(sim.Map);
            var front = Corrigible.Desk.Bridge.LiberationMapBuilder.Build(sim).Regions.First(r => r.Kind == MapRegionKind.Front);
            var failures = new List<string>();
            // Bağlama kökleri açıp kapatır; öteki testler kökün açıklığına bakar (isabet): sonra geri konur
            var active = layer.Regions.ToDictionary(m => m, m => m.gameObject.activeSelf);
            try
            {
                foreach (var m in layer.Regions)
                {
                    var p = layer.Layout.Of(m.RegionId);
                    var boxes = GeneratedMapLayout.BoxesOf(p, layer.Layout.Sizes).ToList();
                    foreach (var kind in new[] { MapRegionKind.Occupied, MapRegionKind.Candidate, MapRegionKind.Free, MapRegionKind.Front })
                    {
                        var st = new MapRegionState
                        {
                            Id = m.RegionId, Name = sim.RegionName(m.RegionId), Kind = kind, Captured = kind == MapRegionKind.Front ? 1 : kind == MapRegionKind.Free ? 3 : 0,
                            Occupation = kind == MapRegionKind.Front ? 66.7 : kind == MapRegionKind.Free ? 0 : 100, FreeShift = kind == MapRegionKind.Free ? 11 : -1,
                            Trait = front.Trait, Signature = front.Signature, Reoccupied = kind == MapRegionKind.Front ? 0 : -1,
                        };
                        m.Bind(st);
                        foreach (RectTransform child in m.transform)
                        {
                            if (!child.gameObject.activeInHierarchy) continue;
                            var corners = new Vector3[4];
                            child.GetWorldCorners(corners);
                            var pts = corners.Select(c => layer.WorldToMap(c)).ToList();
                            var r = Rect.MinMaxRect(pts.Min(q => q.x), pts.Min(q => q.y), pts.Max(q => q.x), pts.Max(q => q.y));
                            if (r.width < 1 || r.height < 1) continue;
                            bool inside = boxes.Any(b => r.xMin >= b.Rect.xMin - 3 && r.yMin >= b.Rect.yMin - 3 && r.xMax <= b.Rect.xMax + 3 && r.yMax <= b.Rect.yMax + 3);
                            if (!inside) failures.Add($"tohum {seed} {m.RegionId} {kind}: {child.name} {r} hiçbir kutunun içinde değil");
                        }
                    }
                }
            }
            finally
            {
                foreach (var kv in active) kv.Key.gameObject.SetActive(kv.Value);
                layer.SetMap(WorldMap.Generate(79));
            }
            Assert.IsEmpty(failures, string.Join("\n", failures.Take(30)));
        }

        [Test]
        public void SetMap_FeedsThePlacements_ToTheMarks_AndTheWorldMapComesBack()
        {
            var map = WorldMap.Generate(2);
            var sceneValues = new Dictionary<string, (Vector2, float, float, LiberationRegionMarks.StripSide)>();
            layer.SetMap(null);
            foreach (var m in layer.Regions) sceneValues[m.RegionId] = (m.LabelPoint, m.LabelSize, m.MaxLineWidth, m.Strip);
            var sceneLegend = layer.LegendRect;
            layer.SetMap(map);
            var layout = layer.Layout;
            Assert.IsNotNull(layout);
            Assert.IsTrue(layout.Clean);
            // Lejant yerleşimin seçtiği köşede (kablo satırı açık, kutu uzadı); yerleşimin kutusu iki dilin genişidir, sağ
            // köşede lejant sağ kenarından tutulur
            var legendNow = layer.LegendRect;
            Assert.AreEqual(layout.Legend.yMin, legendNow.yMin, 0.5f, "lejant köşesi");
            Assert.IsTrue(Mathf.Abs(layout.Legend.xMin - legendNow.xMin) < 0.5f || Mathf.Abs(layout.Legend.xMax - legendNow.xMax) < 0.5f, $"lejant köşesi {legendNow} / {layout.Legend}");
            Assert.GreaterOrEqual(layout.Legend.width + 0.5f, legendNow.width, "yerleşimin lejant kutusu iki dilin genişi");
            foreach (var m in layer.Regions)
            {
                var p = layout.Of(m.RegionId);
                Assert.AreEqual(p.Label, m.LabelPoint, m.RegionId);
                Assert.AreEqual(p.Stamp, m.StampPoint, m.RegionId);
                Assert.AreEqual(p.FontSize, m.LabelSize, m.RegionId);
                Assert.AreEqual(p.StripSide, m.Strip, m.RegionId);
                Assert.AreEqual(p.Team, layer.TeamAnchor(m.RegionId), m.RegionId);
                Assert.AreEqual(m.RegionId, layer.RegionAt(layer.MapToWorld(layer.TeamAnchor(m.RegionId))), m.RegionId + " ekip noktası (isabet çokgeni)");
            }
            // Lejantta kablo hattı satırı yalnız üretilmiş haritada
            var gen = (GeneratedMapRenderer)new SerializedObject(layer).FindProperty("generated").objectReferenceValue;
            var row = ((UILineGraphic)new SerializedObject(gen).FindProperty("legendCable").objectReferenceValue).transform.parent.parent;
            Assert.IsTrue(row.gameObject.activeSelf, "kablo hattı satırı açık");
            Assert.AreEqual("kablo hattı", row.GetComponentInChildren<DeskLocText>(true).Source);

            layer.SetMap(null);
            Assert.IsFalse(row.gameObject.activeSelf, "dünya haritasında kablo satırı kapalı");
            Assert.AreEqual(sceneLegend.position, layer.LegendRect.position, "lejant sahnedeki yerinde");
            Assert.AreEqual(sceneLegend.height, layer.LegendRect.height, 0.5f, "lejant sahnedeki boyunda");
            foreach (var m in layer.Regions)
                Assert.AreEqual(sceneValues[m.RegionId], (m.LabelPoint, m.LabelSize, m.MaxLineWidth, m.Strip), m.RegionId);
            layer.SetMap(WorldMap.Generate(79));
        }

        [Test]
        public void Frontier_DrawsAnArrowPerFreeToCandidateLink()
        {
            var map = WorldMap.Generate(13);
            layer.SetMap(map);
            var gen = (GeneratedMapRenderer)new SerializedObject(layer).FindProperty("generated").objectReferenceValue;
            var frontier = (UILineGraphic)new SerializedObject(gen).FindProperty("frontier").objectReferenceValue;
            var active = layer.Regions.ToDictionary(m => m, m => m.gameObject.activeSelf);
            // Karargâh özgür, komşuları aday
            var state = new LiberationMapState();
            foreach (var r in map.Regions)
                state.Regions.Add(new MapRegionState
                {
                    Id = r.Id, Name = r.SourceName,
                    Kind = r.Id == map.Hq ? MapRegionKind.Free : map.Adjacent(map.Hq, r.Id) ? MapRegionKind.Candidate : MapRegionKind.Occupied,
                });
            layer.Bind(state);
            int expected = map.Links.Where(l => l.A == map.Hq || l.B == map.Hq).Sum(l => l.Cable ? 1 : 2);
            Assert.AreEqual(expected, frontier.PathCount, "kara bağlantısında gövde + baş, kabloda yalnız baş");
            // Aday yoksa ok yok
            foreach (var r in state.Regions) r.Kind = MapRegionKind.Occupied;
            layer.Bind(state);
            Assert.AreEqual(0, frontier.Points.Count);
            // Bağlama işaretleri gizler/açar: sahnedeki hâle dön (öteki testler RegionAt ile açık işaret bekler)
            layer.Bind(null);
            foreach (var kv in active) kv.Key.gameObject.SetActive(kv.Value);
            layer.SetMap(WorldMap.Generate(79));
        }
    }
}
