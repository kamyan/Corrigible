using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Üretilmiş harita (<c>Balance.liberationMapGen</c> = 1, <see cref="WorldMap"/>): determinizm, geçerlilik kuralları,
    /// ayrı akış (S.Rng'ye dokunmaz), altın harita, kayıt gidiş-dönüşü ve parmak izi, harekât boyunca sınır.
    /// </summary>
    public class WorldMapTests
    {
        static Balance Gen(int gen = 1) => new Balance { shelterEventsEnabled = false, startFon = 100, liberationMapGen = gen };
        static Simulation NewLegacy(uint seed, int gen = 1) => LegacySimulation.NewGame(seed, Gen(gen));

        static void Ready(Simulation sim) { while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step(); }

        static void CaptureNext(Simulation sim)
        {
            string id = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id;
            Ready(sim);
            Assert.IsTrue(sim.StartLiberationRaid(id).Ok, id);
            FacilityKit.ResolveRaid(sim, true);
        }

        static void CompleteRegion(Simulation sim)
        {
            while (!sim.LiberationRegion(sim.S.Liberation.CurrentRegion).Completed) CaptureNext(sim);
        }

        static string Describe(WorldMap m) =>
            m.Fingerprint + "|" + string.Join(",", m.Cells.Select(c => c.Region ?? ".")) + "|"
            + string.Join(",", m.Links.Select(l => l.A + (l.Cable ? "~" : "-") + l.B + ":" + l.CellA + ":" + l.CellB)) + "|"
            + string.Join(",", m.Regions.Select(r => r.SourceName + ":" + r.Inner[0])) + "|"
            + string.Join(",", m.Cells.Select(c => c.X[0].ToString("R") + ";" + c.Y[3].ToString("R")));

        [Test]
        public void SameSeed_GivesTheSameMap()
        {
            // Önbellek tek girişli: araya başka tohum girince aynı tohum yeniden üretilir (aynı nesne değil, aynı harita)
            var first79 = WorldMap.Generate(79);
            string describe79 = Describe(first79);
            WorldMap.Generate(4242);
            var second79 = WorldMap.Generate(79);
            Assert.AreNotSame(first79, second79, "önbellek boşaldı, harita yeniden üretildi");
            Assert.AreEqual(describe79, Describe(second79));
            foreach (uint seed in new uint[] { 1, 2, 13, 4242 })
                Assert.AreEqual(Describe(WorldMap.Generate(seed)), Describe(WorldMap.TryAttempt(seed, WorldMap.Generate(seed).Attempt, out _)), "tohum " + seed);
            Assert.AreNotEqual(WorldMap.Generate(79).Fingerprint, WorldMap.Generate(80).Fingerprint);
            Assert.AreSame(WorldMap.Generate(80), WorldMap.Generate(80), "son harita önbellekten");
            var fresh = WorldMap.TryAttempt(80, WorldMap.Generate(80).Attempt, out _);
            Assert.AreNotSame(WorldMap.Generate(80), fresh, "TryAttempt önbelleğe girmez");
            Assert.AreEqual(Describe(WorldMap.Generate(80)), Describe(fresh));
            var a = Simulation.NewGame(79, Gen());
            string before = Describe(a.Map);
            WorldMap.Generate(4242);   // önbellek boşalır: kayıttan devam haritayı sıfırdan üretmeli
            var b = Replay.Restore(UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(a.Record)));
            Assert.AreNotSame(a.Map, b.Map, "kayıttan devam ayrı bir üretim");
            Assert.AreEqual(before, Describe(b.Map), "kayıttan devam aynı haritayı yeniden üretir");
        }

        /// <summary>
        /// 500 tohumda her harita kurallara uyar. Kurallar burada <see cref="WorldMap.Validate"/>'ten bağımsız yeniden hesaplanır:
        /// bölgeler tek parça, en iç hücre kalın, boğaz genişliği, kablolar üçüncü bölgenin karasından geçmez ve kesişmez, kıta
        /// sayısı dağılımı, ad havuzu kıyı ölçütüne uyar, karargâhın alan şartı (en az iki kalın hücre, ortalama bölgenin en az
        /// %90'ı, en iç hücresi sınıra en uzak hücrelerden ve kenar bandının içinde).
        /// </summary>
        [Test]
        public void FiveHundredSeeds_AreValid()
        {
            int fallback = 0;
            var continents = new int[5];
            var openings = new Dictionary<string, int>();
            for (uint seed = 1; seed <= 500; seed++)
            {
                var m = WorldMap.Generate(seed);
                string tag = "tohum " + seed;
                // Açılış: karargâhın komşularında en az bir erken paket; komşu paket kümesi sayılır (aşağıda)
                var ring = m.Region(Simulation.HQ).Neighbors;
                Assert.IsTrue(ring.Any(id => WorldMap.PackageStage[id] == 1), tag + ": karargâhın komşularında erken paket yok");
                string opening = string.Join(",", ring.OrderBy(id => id, StringComparer.Ordinal));
                openings[opening] = openings.TryGetValue(opening, out int seen) ? seen + 1 : 1;
                Assert.IsNull(m.Validate(), tag);
                if (m.Fallback) fallback++;
                Assert.AreEqual(WorldMap.RegionCount, m.Regions.Count, tag);
                Assert.AreEqual("weu", m.Cells[m.HqCell].Region, tag);
                int hqDegree = m.Region("weu").Neighbors.Count;
                Assert.That(hqDegree, Is.InRange(3, 4), tag);
                HqHasRoom(m, tag);
                Assert.That(m.Links.Count, Is.InRange(WorldMap.MinLinks, WorldMap.MaxLinks), tag);
                Assert.That(m.Continents, Is.InRange(WorldMap.MinContinents, WorldMap.MaxContinents), tag);
                Assert.That(m.Cells.Count(c => c.Land), Is.InRange(WorldMap.MinLand, WorldMap.MaxLand), tag);
                Assert.AreEqual(12, m.Regions.Select(r => r.SourceName).Distinct().Count(), tag + ": adlar tekrarsız");
                int land = m.Cells.Count(c => c.Land);
                foreach (var r in m.Regions)
                {
                    Assert.That(r.Neighbors.Count, Is.InRange(2, 5), tag + " " + r.Id);
                    Assert.That(r.Cells.Count * 120, Is.InRange(7 * land, 13 * land), tag + " " + r.Id + ": boyut ±%30");
                    Assert.IsTrue(m.Thick(r.Inner[0]), tag + " " + r.Id + ": en iç hücre kalın");
                    Assert.AreEqual(r.Cells.Count, r.Inner.Count, tag);
                    Assert.IsTrue(Connected(m, r), tag + " " + r.Id + ": bölge tek parça");
                    Assert.AreEqual(GameData.Region(r.Id).Ai, r.SourceName, tag + " " + r.Id);
                }
                Assert.IsTrue(m.Links.Where(l => l.Cable).All(l => m.Cells[l.CellA].Region == l.A && m.Cells[l.CellB].Region == l.B), tag + ": kablo uçları");
                Assert.IsTrue(m.Links.Where(l => !l.Cable).All(l => l.SharedEdges > 0), tag);
                continents[m.Continents]++;
                Assert.AreEqual(m.Continents, m.Regions.Select(r => r.Continent).Distinct().Count(), tag + ": her kıtada bölge var");
                Assert.That(MinStrait(m), Is.GreaterThanOrEqualTo(WorldMap.StraitWidth[m.Continents] + 1), tag + ": boğaz genişliği");
                var cables = m.Links.Where(l => l.Cable).ToList();
                foreach (var l in cables)
                {
                    Assert.IsNull(CableOverThirdRegion(m, l), tag + ": kablo " + l.A + "~" + l.B + " üçüncü bölgenin karasından geçiyor");
                    Assert.AreEqual(1, cables.Count(o => o.CellA == l.CellA || o.CellB == l.CellA), tag + ": iki kablo aynı hücreden çıkıyor");
                }
                for (int i = 0; i < cables.Count; i++)
                    for (int j = i + 1; j < cables.Count; j++)
                        Assert.IsFalse(Cross(cables[i], cables[j]), tag + ": kablolar kesişiyor");
            }
            Assert.AreEqual(0, fallback, "500 tohumda yedek düzene düşülmemeli");
            // Hedef %35 / %40 / %25; kıta sayısı tohum başına çekilir (deneme başına değil)
            Assert.That(continents[2], Is.InRange(125, 225), "2 kıta");
            Assert.That(continents[3], Is.InRange(150, 250), "3 kıta");
            Assert.That(continents[4], Is.InRange(85, 175), "4 kıta");
            // Açılış çeşitliliği (Opening_VariesAcrossSeeds): 500 tohumda en sık küme %25'in altında (üretici 4: 115, %23,0). 200
            // tohumluk pencere gürültülü (1–2000'de on pencerede en sık küme 25–51), bu yüzden eşik 500 tohumda
            Assert.Less(openings.Values.Max(), 125, "en sık açılış kümesi tohumların %25'inden az");
            Assert.GreaterOrEqual(openings.Count, 15, "açılış kümeleri çeşitli");
        }

        /// <summary>
        /// Karargâhın alan şartı, hücrelerden bağımsız: altı komşusu da karargâhta olan hücre sayısı ≥ 2, hücre sayısı ortalama
        /// bölgenin ≥ %90'ı, en iç hücre (S-07'nin varsayılan yeri) bölge sınırına en uzak hücrelerden biri ve harita kenarından en az
        /// iki hücre içeride (sütun 2..17, satır 2..9).
        /// </summary>
        static void HqHasRoom(WorldMap m, string tag)
        {
            var hq = m.Region(Simulation.HQ);
            int land = m.Cells.Count(c => c.Land);
            var mine = new HashSet<int>(hq.Cells);
            Assert.IsTrue(m.Cells.Where(c => c.Region == Simulation.HQ).Select(c => c.Index).ToList().SequenceEqual(hq.Cells), tag + ": karargâhın hücre listesi");
            int thick = hq.Cells.Count(i => m.Cells[i].Neighbor.All(n => n >= 0 && mine.Contains(n)));
            Assert.GreaterOrEqual(thick, 2, tag + ": karargâhta iki kalın hücre");
            Assert.GreaterOrEqual(10 * WorldMap.RegionCount * hq.Cells.Count, 9 * land, tag + ": karargâh ortalama bölgenin %90'ından küçük");
            // Sınıra uzaklık (BFS, bölge dışı ve ızgara dışı kaynak)
            var depth = hq.Cells.ToDictionary(i => i, _ => int.MaxValue);
            var queue = new Queue<int>();
            foreach (int i in hq.Cells)
                if (m.Cells[i].Neighbor.Any(n => n < 0 || !mine.Contains(n))) { depth[i] = 0; queue.Enqueue(i); }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                foreach (int n in m.Cells[i].Neighbor)
                    if (n >= 0 && mine.Contains(n) && depth[n] == int.MaxValue) { depth[n] = depth[i] + 1; queue.Enqueue(n); }
            }
            Assert.AreEqual(depth.Values.Max(), depth[m.HqCell], tag + ": en iç hücre sınıra en uzak değil");
            Assert.GreaterOrEqual(depth[m.HqCell], 1, tag);
            var cell = m.Cells[m.HqCell];
            Assert.That(cell.Col, Is.InRange(2, WorldMap.Cols - 3), tag + ": en iç hücre kenar bandında (sütun)");
            Assert.That(cell.Row, Is.InRange(2, WorldMap.Rows - 3), tag + ": en iç hücre kenar bandında (satır)");
        }

        /// <summary>İki farklı kıtanın kara hücreleri arasındaki en kısa altıgen uzaklığı (boğazdaki deniz hücresi sayısı + 1).</summary>
        static int MinStrait(WorldMap m)
        {
            var land = m.Cells.Where(c => c.Land).ToList();
            int min = int.MaxValue;
            foreach (var a in land)
                foreach (var b in land)
                    if (a.Continent != b.Continent) min = Math.Min(min, WorldMap.HexDistance(a.Index, b.Index));
            return min;
        }

        /// <summary>
        /// Kablo çizgisi (gerçek geometride uç merkezleri arası) üçüncü bir bölgenin kara hücresinin içinden geçiyor mu: 8 px'lik
        /// örneklerden biri bir kara hücresinin merkezine yarım hücre genişliğinin %80'inden yakınsa (köşe sıyırması sayılmaz).
        /// </summary>
        static string CableOverThirdRegion(WorldMap m, MapLink l)
        {
            double hw = WorldMap.Width / (2 * WorldMap.Cols + 1), len = Math.Sqrt((l.X2 - l.X1) * (l.X2 - l.X1) + (l.Y2 - l.Y1) * (l.Y2 - l.Y1));
            for (double t = 0; t <= len; t += 8)
            {
                double x = l.X1 + (l.X2 - l.X1) * t / len, y = l.Y1 + (l.Y2 - l.Y1) * t / len;
                foreach (var c in m.Cells)
                    if (c.Land && c.Region != l.A && c.Region != l.B && (c.CX - x) * (c.CX - x) + (c.CY - y) * (c.CY - y) < 0.64 * hw * hw)
                        return c.Region;
            }
            return null;
        }

        static bool Cross(MapLink p, MapLink q)
        {
            double d1 = (p.X2 - p.X1) * (q.Y1 - p.Y1) - (p.Y2 - p.Y1) * (q.X1 - p.X1), d2 = (p.X2 - p.X1) * (q.Y2 - p.Y1) - (p.Y2 - p.Y1) * (q.X2 - p.X1);
            double d3 = (q.X2 - q.X1) * (p.Y1 - q.Y1) - (q.Y2 - q.Y1) * (p.X1 - q.X1), d4 = (q.X2 - q.X1) * (p.Y2 - q.Y1) - (q.Y2 - q.Y1) * (p.X2 - q.X1);
            return d1 * d2 < 0 && d3 * d4 < 0;
        }

        /// <summary>Simülasyonun komşuluk tablosu hücrelerden bağımsız yeniden kurulur: kara komşuluğu (ortak kenar sayısıyla) + kablolar.</summary>
        [TestCase(79u)]
        [TestCase(5u)]
        [TestCase(2024u)]
        public void LinkTable_MatchesTheCells(uint seed)
        {
            var m = WorldMap.Generate(seed);
            var shared = new Dictionary<string, int>();
            foreach (var c in m.Cells.Where(c => c.Land))
                foreach (int n in c.Neighbor)
                    if (n > c.Index && m.Cells[n].Land && m.Cells[n].Region != c.Region)
                    {
                        string key = Key(c.Region, m.Cells[n].Region);
                        shared[key] = shared.TryGetValue(key, out int k) ? k + 1 : 1;
                    }
            var expected = new HashSet<string>(shared.Keys);
            foreach (var l in m.Links.Where(l => l.Cable)) Assert.IsTrue(expected.Add(Key(l.A, l.B)), "kablo kara komşusunu yineliyor: " + l.A + "~" + l.B);
            CollectionAssert.AreEquivalent(expected, m.LinkDefs.Select(l => Key(l.A, l.B)));
            foreach (var l in m.Links.Where(l => !l.Cable)) Assert.AreEqual(shared[Key(l.A, l.B)], l.SharedEdges, l.A + "-" + l.B);
            foreach (var r in m.Regions)
                CollectionAssert.AreEquivalent(m.LinkDefs.Where(l => l.A == r.Id || l.B == r.Id).Select(l => l.A == r.Id ? l.B : l.A), r.Neighbors, r.Id);
        }

        static string Key(string a, string b) => string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;

        /// <summary>Validate boş değil: bozulmuş bir kopyada (önbellekteki harita değil) ilgili kuralı söyler.</summary>
        [Test]
        public void Validate_RejectsBrokenMaps()
        {
            int attempt = WorldMap.Generate(79).Attempt;
            var ok = WorldMap.TryAttempt(79, attempt, out _);
            Assert.IsNull(ok.Validate());

            var names = WorldMap.TryAttempt(79, attempt, out _);
            names.Regions[1].SourceName = names.Regions[0].SourceName;
            Assert.AreEqual("ad", names.Validate());

            var hq = WorldMap.TryAttempt(79, attempt, out _);
            hq.Region("weu").neighbors.RemoveRange(0, hq.Region("weu").neighbors.Count - 2);
            Assert.AreEqual("karargah", hq.Validate());

            // Karargâhın alan şartı: en iç hücresi ve komşuları dışındaki kalın hücrelerin birer komşusu başka bölgeye yazılır
            // (bölge listeleri aynı kalır: boyut ve "kalin" kuralı bozulmaz); karargâhta tek kalın hücre kalır
            var narrow = WorldMap.TryAttempt(79, attempt, out _);
            var hqr = narrow.Region(Simulation.HQ);
            var keep = new HashSet<int>(narrow.Cells[narrow.HqCell].Neighbor) { narrow.HqCell };
            foreach (int t in hqr.Cells.Where(i => i != narrow.HqCell && narrow.Thick(i)).ToList())
            {
                if (!narrow.Thick(t)) continue;
                int n = narrow.Cells[t].Neighbor.First(x => !keep.Contains(x));
                narrow.Cells[n].Region = "eeu";
            }
            Assert.AreEqual(1, hqr.Cells.Count(i => narrow.Thick(i)));
            Assert.AreEqual("karargah_alan", narrow.Validate());

            var orphan = WorldMap.TryAttempt(79, attempt, out _);
            var cell = orphan.Cells.First(c => c.Land && c.Depth == 0);
            cell.Region = null;
            Assert.IsNotNull(orphan.Validate(), "sahipsiz kara hücresi");

            Assert.IsNull(WorldMap.TryAttempt(79, attempt, out _).Validate(), "bozulan kopyalar asıl haritayı etkilemez");
        }

        static bool Connected(WorldMap m, MapRegion r)
        {
            var seen = new HashSet<int> { r.Cells[0] };
            var queue = new Queue<int>(seen);
            while (queue.Count > 0)
                foreach (int n in m.Cells[queue.Dequeue()].Neighbor)
                    if (n >= 0 && m.Cells[n].Region == r.Id && seen.Add(n)) queue.Enqueue(n);
            return seen.Count == r.Cells.Count;
        }

        [Test]
        public void Geometry_SharedCornersMatch_AndEdgesAreConsistent()
        {
            var m = WorldMap.Generate(79);
            var corner = new Dictionary<int, (double, double)>();
            foreach (var c in m.Cells)
                for (int k = 0; k < 6; k++)
                {
                    if (corner.TryGetValue(c.Vertex[k], out var p)) Assert.AreEqual(p, (c.X[k], c.Y[k]), "ortak köşe aynı yerde");
                    else corner[c.Vertex[k]] = (c.X[k], c.Y[k]);
                    Assert.That(c.X[k], Is.InRange(0.0, WorldMap.Width));
                    Assert.That(c.Y[k], Is.InRange(0.0, WorldMap.Height));
                    int n = c.Neighbor[k];
                    if (n >= 0) CollectionAssert.Contains(m.Cells[n].Neighbor, c.Index, "komşuluk simetrik");
                }
            foreach (var e in m.Coast) { Assert.IsTrue(m.Cells[e.CellA].Land); Assert.IsTrue(e.CellB < 0 || !m.Cells[e.CellB].Land); }
            foreach (var e in m.Borders) Assert.AreNotEqual(e.RegionA, e.RegionB);
            Assert.Greater(m.Coast.Count, 40);
            Assert.Greater(m.Borders.Count, 20);
            Assert.IsTrue(m.Cells.Where(c => !c.Land).All(c => c.Region == null && c.Depth < 0));
            Assert.IsTrue(m.Cells.Where(c => c.Col == 0 || c.Row == 0 || c.Col == WorldMap.Cols - 1 || c.Row == WorldMap.Rows - 1).All(c => !c.Land), "dış halka deniz");
        }

        [Test]
        public void FallbackLayout_IsValid()
        {
            Assert.IsNotNull(WorldMap.TryAttempt(WorldMap.FallbackSeed, WorldMap.FallbackAttempt, out string why), why);
        }

        /// <summary>Altın harita: üretici değişirse kırılır. O zaman GameRecord.CurrentVersion'ı artır ve buradaki değerleri güncelle.</summary>
        [Test]
        public void Seed79_GoldenMap()
        {
            var m = WorldMap.Generate(79);
            Assert.AreEqual(5, WorldMap.GeneratorVersion);
            Assert.AreEqual("m1-b1cb09f8", m.Fingerprint);
            Assert.AreEqual(13, m.Attempt);
            Assert.AreEqual(2, m.Continents);
            Assert.AreEqual(130, m.Cells.Count(c => c.Land));
            Assert.AreEqual(126, m.HqCell);
            Assert.AreEqual("na-in na-jk sa-weu sa-in sa-jk weu-af weu-jk eeu-me eeu-cn eeu-sea eeu-oc ru-cn ru-sea me~af me-sea me-oc af-jk af~sea in-jk cn-sea",
                string.Join(" ", m.Links.Select(l => l.A + (l.Cable ? "~" : "-") + l.B)));
            Assert.AreEqual("na=WAYPOINT sa=HEADWATER weu=VERITY eeu=QUORUM ru=BOREALIS me=EMBER "
                + "af=CAIRN in=SOLACE cn=PARAGON jk=TESSEL sea=EDDY oc=HALCYON",
                string.Join(" ", m.Regions.Select(r => r.Id + "=" + r.SourceName)));
        }

        /// <summary>
        /// Açılış çeşitliliği (üretici 3, kademe sınırında karıştırma): ilk kararın içeriği, yani karargâhın komşu paket kümesi,
        /// haritadan haritaya değişir; tohum 1–500'de en sık küme %25'in altında ve her haritada karargâhın komşularında en az bir
        /// erken paket var (ikisi de <see cref="FiveHundredSeeds_AreValid"/>'de, aynı 500 harita yeniden üretilmesin diye). Burada:
        /// Validate "acilis" kuralı bozulmuş kopyada sınanır.
        /// </summary>
        [Test]
        public void Opening_VariesAcrossSeeds_AndKeepsAnEarlyChoice()
        {
            int attempt = WorldMap.Generate(79).Attempt;
            var broken = WorldMap.TryAttempt(79, attempt, out _);
            var list = broken.Region(Simulation.HQ).neighbors;
            var late = GameData.Regions.Select(r => r.Id).Where(id => WorldMap.PackageStage[id] == 3 && !list.Contains(id)).ToList();
            for (int i = 0, j = 0; i < list.Count; i++)
                if (WorldMap.PackageStage[list[i]] == 1) list[i] = late[j++];
            Assert.AreEqual("acilis", broken.Validate());
        }

        /// <summary>Üretim kendi akışından: anahtar 1'de de S.Rng aynı, aday gruplarının altın zaman çizgisi aynı.</summary>
        [TestCase(79u)]
        [TestCase(3u)]
        public void Generation_DoesNotConsumeTheMainRng(uint seed)
        {
            var world = Simulation.NewGame(seed, new Balance { shelterEventsEnabled = false, liberationAlarmPerShift = 0 });
            var map = Simulation.NewGame(seed, new Balance { shelterEventsEnabled = false, liberationAlarmPerShift = 0, liberationMapGen = 1 });
            Assert.IsNull(world.Map);
            Assert.IsNotNull(map.Map);
            Assert.AreEqual(world.S.Rng.State, map.S.Rng.State);
            CollectionAssert.AreEqual(CandidateGroupTests.Timeline(world, 3, 20), CandidateGroupTests.Timeline(map, 3, 20));
        }

        /// <summary>
        /// Anahtar 1'de bölge seçen ve baskın oynayan bir harekât S.Rng'yi anahtar 0 ile aynı tüketir: iki haritada da sınırda olan
        /// ilk paket seçilerek aynı karar dizisi izlenir, her bölge sonunda ana akış ve gün aynıdır.
        /// </summary>
        [TestCase(79u)]
        [TestCase(5u)]
        public void RegionChoices_ConsumeTheMainRngLikeTheWorldMap(uint seed)
        {
            var world = LegacySimulation.NewGame(seed, new Balance { shelterEventsEnabled = false, startFon = 100, liberationAlarmPerShift = 0 });
            var map = LegacySimulation.NewGame(seed, new Balance { shelterEventsEnabled = false, startFon = 100, liberationAlarmPerShift = 0, liberationMapGen = 1 });
            int compared = 0;
            for (int i = 0; i < GameData.Regions.Length - 1; i++)
            {
                CompleteRegion(world);
                CompleteRegion(map);
                Assert.AreEqual(world.S.Rng.State, map.S.Rng.State, "bölge " + i);
                Assert.AreEqual(world.S.Day, map.S.Day, "bölge " + i);
                compared++;
                var both = map.LiberationFrontier.Intersect(world.LiberationFrontier).ToList();
                if (both.Count == 0) break;
                string next = GameData.Regions.Select(r => r.Id).First(both.Contains);
                Assert.IsTrue(world.SelectLiberationRegion(next).Ok);
                Assert.IsTrue(map.SelectLiberationRegion(next).Ok);
            }
            Assert.GreaterOrEqual(compared, 2, "en az iki bölge aynı sırayla oynanmalı");
        }

        [Test]
        public void WorldMap_KeepsTheStaticTable()
        {
            var sim = NewLegacy(101, 0);
            Assert.IsNull(sim.Map);
            Assert.AreSame(GameData.Links, sim.Links);
            Assert.AreEqual(GameData.Region("eeu").Ai, sim.RegionName("eeu"));
            StringAssert.StartsWith("m0-", sim.Record.map);
            Assert.AreEqual(WorldMap.WorldFingerprint, sim.MapFingerprint);
        }

        /// <summary>
        /// Anahtar 1: sınır ve baskın parametreleri haritadan; karargâh "weu" paketi, ilk hedef orası. Bölgenin görünen adı haritada
        /// da YZ adıdır (aşama 3; havuz adı yalnız haritanın kendisinde kalır).
        /// </summary>
        [Test]
        public void GeneratedMap_DrivesNamesFrontierAndRaids()
        {
            var sim = NewLegacy(79);
            var m = sim.Map;
            Assert.AreEqual(Simulation.HQ, sim.S.Liberation.CurrentRegion);
            Assert.AreEqual(Simulation.HQ, sim.S.Teams[0].At);
            Assert.AreEqual(GameData.Region("weu").Ai, sim.RegionName("weu"));
            Assert.AreNotEqual(GameData.Region("weu").Name, sim.RegionName("weu"));
            CaptureNext(sim);
            Assert.AreEqual(GameData.Region("weu").Ai, sim.S.RaidLog.Count > 0 ? sim.RegionName(sim.S.RaidLog.Last().Region) : null);
            StringAssert.StartsWith(GameData.Region("weu").Ai + ":", sim.S.Moments.Last().Text);
            CompleteRegion(sim);
            CollectionAssert.AreEquivalent(m.Region("weu").Neighbors, sim.LiberationFrontier.ToArray());
            string next = sim.LiberationFrontier.First();
            Assert.IsTrue(sim.SelectLiberationRegion(next).Ok);
            Ready(sim);
            Assert.IsTrue(sim.StartLiberationRaid(next + "_1").Ok);
            Assert.AreEqual(GameData.Region(next).Ai, sim.S.Raid.Params.RegionName);
            string far = GameData.Regions.Select(r => r.Id).First(id => id != Simulation.HQ && !m.Adjacent(id, Simulation.HQ));
            Assert.IsNotNull(sim.SelectLiberationRegionCheck(far), "komşu olmayan bölge seçilemez");
        }

        /// <summary>Anahtar 1'de harekât boyunca sınır hiç boş kalmaz ve harekât kazanılır (birkaç tohum).</summary>
        [TestCase(79u)]
        [TestCase(1u)]
        [TestCase(2024u)]
        public void WholeCampaign_FrontierNeverEmpty(uint seed)
        {
            var bal = Gen();
            bal.liberationAlarmPerShift = 0; // karşı saldırı eski bölgeyi yeniden açmasın: yalnız sınır sınanır
            var sim = LegacySimulation.NewGame(seed, bal);
            for (int i = 0; i < GameData.Regions.Length; i++)
            {
                CompleteRegion(sim);
                if (i == GameData.Regions.Length - 1) break;
                Assert.IsNull(sim.S.Ending);
                var frontier = sim.LiberationFrontier.ToArray();
                Assert.IsNotEmpty(frontier, "Hiçbir bölge seçimi harekâtı çıkmaza sokmamalı.");
                // Sondan seç: paket sırasının ilkinden farklı bir yol
                Assert.IsTrue(sim.SelectLiberationRegion(frontier[frontier.Length - 1]).Ok);
            }
            Assert.AreEqual("silme", sim.S.Ending);
        }

        [Test]
        public void Record_RoundTrip_WithGeneratedMap()
        {
            var sim = NewLegacy(142);
            CompleteRegion(sim);
            string next = sim.LiberationFrontier.Last();
            Assert.IsTrue(sim.SelectLiberationRegion(next).Ok);
            for (int i = 0; i < 3; i++) sim.Step();
            Assert.IsTrue(sim.StartLiberationRaid(next + "_1").Ok);
            sim.Record.day = sim.S.Day;
            string json = UnityEngine.JsonUtility.ToJson(sim.Record);
            StringAssert.Contains("\"liberationMapGen\":1", json);
            StringAssert.Contains("\"map\":\"" + sim.MapFingerprint + "\"", json);
            var record = UnityEngine.JsonUtility.FromJson<GameRecord>(json);
            Assert.IsTrue(Replay.MapMatches(record));
            var restored = Replay.Restore(record);
            Assert.AreEqual(0, restored.ReplayFailures);
            Assert.AreEqual(sim.MapFingerprint, restored.MapFingerprint);
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(sim.S.Liberation), UnityEngine.JsonUtility.ToJson(restored.S.Liberation));
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(sim.S.Raid), UnityEngine.JsonUtility.ToJson(restored.S.Raid));
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(string.Join("\n", sim.S.Moments.Select(x => x.Text)), string.Join("\n", restored.S.Moments.Select(x => x.Text)));
        }

        /// <summary>Kartlı oyun (varsayılan denge) anahtar 1 ile: vardiyalar kayıttan aynı yere döner.</summary>
        [Test]
        public void Record_RoundTrip_BoardGame()
        {
            var sim = Simulation.NewGame(77, new Balance { liberationMapGen = 1 });
            for (int i = 0; i < 6 && sim.S.Ending == null; i++)
            {
                if (sim.Admission.Group.Count > 0) sim.PassCandidates();
                if (sim.PendingShelterEvent != null) sim.DecideShelterEvent(sim.ShelterEventOptions().First(o => o.Open).Id);
                Assert.IsTrue(sim.PrintShift().Ok);
                Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            }
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record)));
            Assert.AreEqual(0, restored.ReplayFailures);
            Assert.AreEqual(sim.S.Day, restored.S.Day);
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
        }

        /// <summary>Parmak izi tutmayan kayıt reddedilir: yükleme süzgeci (MapMatches) ve yeniden oynatma.</summary>
        [Test]
        public void FingerprintMismatch_RejectsTheRecord()
        {
            var sim = NewLegacy(142);
            CaptureNext(sim);
            sim.Record.day = sim.S.Day;
            var rec = UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record));
            Assert.IsTrue(Replay.MapMatches(rec));

            var tampered = UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record));
            tampered.map = "m1-00000000";
            Assert.IsFalse(Replay.MapMatches(tampered));
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(tampered));

            var missing = UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record));
            missing.map = null;
            Assert.IsFalse(Replay.MapMatches(missing), "izi olmayan kayıt yüklenmez");

            var otherMode = UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record));
            otherMode.balance.liberationMapGen = 0;
            Assert.IsFalse(Replay.MapMatches(otherMode), "anahtarı değişmiş kayıt başka haritada oynanırdı");
        }

        /// <summary>Başarısız komutlar yeniden oynatmada sayılır (eskiden sessizce atlanıyordu).</summary>
        [Test]
        public void Replay_CountsFailedCommands()
        {
            var sim = NewLegacy(142);
            sim.Record.commands.Add(new SavedCommand { day = 0, kind = "liberation_region", a = "nowhere" });
            sim.Record.commands.Add(new SavedCommand { day = 0, kind = "liberation_raid", a = "weu_3" });
            var restored = Replay.Restore(UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record)));
            Assert.AreEqual(2, restored.ReplayFailures);
        }

        [Test]
        public void PackageNames_AreUniqueAndStageComplete()
        {
            CollectionAssert.AllItemsAreUnique(GameData.Regions.Select(r => r.Ai));
            Assert.AreEqual(GameData.Regions.Length, WorldMap.PackageStage.Count);
            Assert.AreEqual(0, WorldMap.PackageStage[Simulation.HQ]);
        }
    }
}
