using System;
using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Üretilmiş haritanın (<see cref="WorldMap"/>, <c>Balance.liberationMapGen</c> = 1) çizim geometrisi, tasarım pikselinde
    /// (1920×1080, sol üst orijin, y aşağı; Core'un koordinatlarıyla aynı uzay).
    /// <list type="bullet">
    /// <item>Core haritası salt okunur ve önbellekte paylaşılır: köşe ve kenarlar burada kopya olarak kurulur, Core'a yazılmaz.</item>
    /// <item>El çizimi kenar: her köşe kendi ortak kimliğinden tohumlanan küçük bir sapma alır; her kenar <see cref="EdgeSteps"/> ara
    /// noktayla kırılır ve ara noktalar kenara dik, iki ucun kimliğinden tohumlanan üç harmonikli bir dalgayla kayar. Komşu iki
    /// hücre aynı kırık çizgiyi görür (dolgu, kıyı ve sınır arasında boşluk kalmaz).</item>
    /// <item>Kıyı daha hırçın, iç sınır daha sakin (dünya haritasında siyasi sınırlar kıyıdan düzdür): kıyı kenarı
    /// <see cref="CoastAmplitude"/>, kıyı köşesi <see cref="CoastJitter"/>; iç kenar <see cref="EdgeAmplitude"/>, iç köşe
    /// <see cref="VertexJitter"/>. Böylece kıyı hex kenarı boyunda eşit tümsekler gibi okunmaz.</item>
    /// <item>Dalga kenar boyunun en çok o kenarın genliği kadarı: hücre merkezinden bakınca yıldız biçimli kalır, çokgen
    /// merkezden yelpaze üçgenlenir.</item>
    /// <item>Bölge halkaları, kıyı halkaları ve bölge sınırı zincirleri aynı kırık kenarlardan, ortak köşe kimlikleriyle zincirlenir.</item>
    /// </list>
    /// Sunumdur: float ve tamsayı karma serbest, simülasyona hiçbir şey dönmez.
    /// </summary>
    public sealed class GeneratedMapGeometry
    {
        /// <summary>Kenar başına ara nokta sayısı.</summary>
        public const int EdgeSteps = 7;
        /// <summary>Hücre halkasının nokta sayısı (6 köşe + 6 × ara nokta).</summary>
        public const int RingPoints = 6 * (1 + EdgeSteps);

        public WorldMap Map { get; }
        /// <summary>İç (bölge sınırı) kenar dalgasının kenar boyuna oranla üst sınırı.</summary>
        public float EdgeAmplitude { get; }
        /// <summary>İç köşe sapmasının kenar boyuna oranla üst sınırı.</summary>
        public float VertexJitter { get; }
        /// <summary>Kıyı kenarı dalgasının kenar boyuna oranla üst sınırı.</summary>
        public float CoastAmplitude { get; }
        /// <summary>Kıyı köşesi (en az bir yanı deniz) sapmasının kenar boyuna oranla üst sınırı.</summary>
        public float CoastJitter { get; }
        /// <summary>İki köşe arasındaki kenar kıyı mı (kara | deniz)?</summary>
        public bool IsCoastEdge(int v1, int v2) => coastEdges.Contains(EdgeKey(v1, v2));

        /// <summary>Bölgenin hücre çokgenleri (kara hücresi başına bir çokgen; son köşe hücre merkezi).</summary>
        public IReadOnlyDictionary<string, RegionMeshGraphic.Polygon[]> RegionPolygons => regionPolygons;
        /// <summary>Bölgenin dış (ve varsa iç) halkaları; kapalı, son nokta başa bağlanır.</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<Vector2>>> RegionOutlines => regionOutlines;
        /// <summary>Kıyı halkaları (kara | deniz); kapalı.</summary>
        public IReadOnlyList<IReadOnlyList<Vector2>> CoastLoops => coastLoops;
        /// <summary>İki bölge arasındaki sınır zincirleri (açık; kapalı bir sınırın son noktası ilkinin tekrarı).</summary>
        public IReadOnlyList<IReadOnlyList<Vector2>> BorderPaths => borderPaths;
        /// <summary>Kablo hatları: iki uç (hücre merkezleri).</summary>
        public IReadOnlyList<IReadOnlyList<Vector2>> CablePaths => cablePaths;
        /// <summary>Bölgenin içe kaydırılmış halkaları (cephe ve aday konturu; koyu sınır çizgisinin hemen içinde).</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<Vector2>>> RegionContours => regionContours;
        /// <summary>Varsayılan ekip noktası (bölgenin <c>Inner[0]</c> hücresinin merkezi); asıl nokta <see cref="Corrigible.Desk.GeneratedMapLayout"/>'tan.</summary>
        public IReadOnlyDictionary<string, Vector2> TeamAnchors => teamAnchors;
        /// <summary>
        /// Sığınak (S-07) için varsayılan nokta: <see cref="WorldMap.HqCell"/>'in merkezi. Oyunda yerleşim
        /// (<see cref="GeneratedMapLayout.ShelterPoint"/>) karargâhın içinde başka bir yer seçebilir.
        /// </summary>
        public Vector2 ShelterPoint { get; }

        readonly Dictionary<int, Vector2> vertices = new Dictionary<int, Vector2>();
        readonly Dictionary<long, Vector2[]> edges = new Dictionary<long, Vector2[]>();
        readonly HashSet<long> coastEdges = new HashSet<long>();
        readonly HashSet<int> coastVertices = new HashSet<int>();
        // Kenarı taşıyan kara hücrelerinin merkezleri (bir ya da iki): kırık kenar ikisinden bakınca da yıldız biçimli kalmalı
        readonly Dictionary<long, List<Vector2>> edgeCenters = new Dictionary<long, List<Vector2>>();
        readonly Dictionary<string, RegionMeshGraphic.Polygon[]> regionPolygons = new Dictionary<string, RegionMeshGraphic.Polygon[]>();
        readonly Dictionary<string, IReadOnlyList<IReadOnlyList<Vector2>>> regionOutlines = new Dictionary<string, IReadOnlyList<IReadOnlyList<Vector2>>>();
        readonly List<IReadOnlyList<Vector2>> coastLoops = new List<IReadOnlyList<Vector2>>();
        readonly List<IReadOnlyList<Vector2>> borderPaths = new List<IReadOnlyList<Vector2>>();
        readonly List<IReadOnlyList<Vector2>> cablePaths = new List<IReadOnlyList<Vector2>>();
        readonly Dictionary<string, Vector2> teamAnchors = new Dictionary<string, Vector2>();
        readonly Dictionary<string, IReadOnlyList<IReadOnlyList<Vector2>>> regionContours = new Dictionary<string, IReadOnlyList<IReadOnlyList<Vector2>>>();
        readonly Vector2[][] rings;
        // Nokta → hücre: kara hücrelerinin halka sınırları, 64 px kovalar
        const float Bucket = 64;
        readonly Dictionary<int, List<int>> buckets = new Dictionary<int, List<int>>();
        readonly Rect[] cellBounds;

        /// <summary>
        /// <paramref name="coastAmplitude"/> / <paramref name="coastJitter"/> verilmezse (&lt; 0) kıyı iç kenarla aynı kırılır.
        /// Oyun ve testler değerleri temadan alır (<c>space.mapGen.*</c>; <see cref="GeneratedMapRenderer.BuildGeometry"/>).
        /// </summary>
        public GeneratedMapGeometry(WorldMap map, float edgeAmplitude = 0.14f, float vertexJitter = 0.06f, float coastAmplitude = -1, float coastJitter = -1)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            EdgeAmplitude = Mathf.Clamp(edgeAmplitude, 0, 0.3f);
            VertexJitter = Mathf.Clamp(vertexJitter, 0, 0.2f);
            CoastAmplitude = coastAmplitude < 0 ? EdgeAmplitude : Mathf.Clamp(coastAmplitude, 0, 0.3f);
            CoastJitter = coastJitter < 0 ? VertexJitter : Mathf.Clamp(coastJitter, 0, 0.2f);
            var cells = map.Cells;

            // Kıyı kenarları ve köşeleri (kara hücresinin deniz ya da ızgara dışı komşusuna bakan kenarı)
            foreach (var c in cells)
            {
                if (!c.Land) continue;
                for (int k = 0; k < 6; k++)
                {
                    int a = c.Vertex[k], b = c.Vertex[(k + 1) % 6];
                    long ek = EdgeKey(a, b);
                    if (!edgeCenters.TryGetValue(ek, out var owners)) edgeCenters[ek] = owners = new List<Vector2>(2);
                    owners.Add(Center(c));
                    int nb = c.Neighbor[k];
                    if (nb >= 0 && cells[nb].Land) continue;
                    coastEdges.Add(EdgeKey(a, b));
                    coastVertices.Add(a);
                    coastVertices.Add(b);
                }
            }

            // Köşeler: ilk gören hücrenin koordinatı (aynı kimlik her hücrede tam aynı nokta), sonra kimlikten sapma
            foreach (var c in cells)
                for (int k = 0; k < 6; k++)
                {
                    int id = c.Vertex[k];
                    if (vertices.ContainsKey(id)) continue;
                    var p = new Vector2((float)c.X[k], (float)c.Y[k]);
                    float len = EdgeLength(c);
                    float jitter = coastVertices.Contains(id) ? CoastJitter : VertexJitter;
                    p += new Vector2(Unit((uint)id, 0x5A17u, 1), Unit((uint)id, 0x5A17u, 2)) * (jitter * len);
                    vertices[id] = p;
                }

            // Hücre halkaları ve bölge çokgenleri
            rings = new Vector2[cells.Count][];
            cellBounds = new Rect[cells.Count];
            foreach (var c in cells)
            {
                if (!c.Land) continue;
                var ring = rings[c.Index] = BuildRing(c);
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                foreach (var p in ring) { x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y); }
                cellBounds[c.Index] = Rect.MinMaxRect(x0, y0, x1, y1);
                for (int by = Mathf.FloorToInt(y0 / Bucket); by <= Mathf.FloorToInt(y1 / Bucket); by++)
                    for (int bx = Mathf.FloorToInt(x0 / Bucket); bx <= Mathf.FloorToInt(x1 / Bucket); bx++)
                    {
                        int key = by * 1024 + bx;
                        if (!buckets.TryGetValue(key, out var l)) buckets[key] = l = new List<int>(4);
                        l.Add(c.Index);
                    }
            }
            foreach (var r in map.Regions)
            {
                var polys = new RegionMeshGraphic.Polygon[r.Cells.Count];
                for (int i = 0; i < polys.Length; i++) polys[i] = Fan(rings[r.Cells[i]], Center(cells[r.Cells[i]]));
                regionPolygons[r.Id] = polys;

                var boundary = new List<(int, int)>();
                foreach (int ci in r.Cells)
                {
                    var c = cells[ci];
                    for (int k = 0; k < 6; k++)
                    {
                        int nb = c.Neighbor[k];
                        bool same = nb >= 0 && cells[nb].Land && cells[nb].Region == r.Id;
                        if (!same) boundary.Add((c.Vertex[k], c.Vertex[(k + 1) % 6]));
                    }
                }
                var loops = new List<IReadOnlyList<Vector2>>();
                foreach (var (path, _) in Chain(boundary)) loops.Add(path);
                regionOutlines[r.Id] = loops;

                teamAnchors[r.Id] = Center(cells[r.Inner[0]]);
            }

            // Kıyı: kara hücresinin deniz (ya da ızgara dışı) komşusuna bakan kenarları
            var coast = new List<(int, int)>();
            foreach (var c in cells)
            {
                if (!c.Land) continue;
                for (int k = 0; k < 6; k++)
                {
                    int nb = c.Neighbor[k];
                    if (nb < 0 || !cells[nb].Land) coast.Add((c.Vertex[k], c.Vertex[(k + 1) % 6]));
                }
            }
            foreach (var (path, _) in Chain(coast)) coastLoops.Add(path);

            // Bölge sınırları (Core'un kenar listesi; kıyıda ve üçlü kavşakta biten zincirler)
            var borders = new List<(int, int)>();
            foreach (var e in map.Borders) borders.Add((e.V1, e.V2));
            foreach (var (path, closed) in Chain(borders))
            {
                if (closed) { var copy = new List<Vector2>(path) { path[0] }; borderPaths.Add(copy); }
                else borderPaths.Add(path);
            }

            foreach (var l in map.Links)
                if (l.Cable) cablePaths.Add(new[] { new Vector2((float)l.X1, (float)l.Y1), new Vector2((float)l.X2, (float)l.Y2) });

            ShelterPoint = Center(cells[map.HqCell]);

            // Cephe/aday konturu: bölge halkaları bölgenin içine kaydırılır (koyu sınırla üst üste binmez)
            foreach (var r in map.Regions)
            {
                var inset = new List<IReadOnlyList<Vector2>>();
                foreach (var loop in regionOutlines[r.Id])
                {
                    float side = SideOf(loop, p => RegionAt(p) == r.Id);
                    inset.Add(Offset(loop, side * ContourInset));
                }
                regionContours[r.Id] = inset;
            }
        }

        /// <summary>Konturun bölge sınırından içeri kayması (px).</summary>
        public const float ContourInset = 5;

        // ---------------------------------------------------------------- nokta → hücre

        /// <summary>Noktanın kara hücresi (kırık halkaların içinde); denizde ya da ızgara dışında −1.</summary>
        public int CellAt(Vector2 p)
        {
            int key = Mathf.FloorToInt(p.y / Bucket) * 1024 + Mathf.FloorToInt(p.x / Bucket);
            if (!buckets.TryGetValue(key, out var list)) return -1;
            foreach (int c in list)
                if (cellBounds[c].Contains(p) && InRing(rings[c], p)) return c;
            return -1;
        }

        /// <summary>Noktanın bölgesi (paket kimliği); denizde null.</summary>
        public string RegionAt(Vector2 p)
        {
            int c = CellAt(p);
            return c >= 0 ? Map.Cells[c].Region : null;
        }

        // Dizi (arayüz değil): isabet ve yerleşim bunu çok sık çağırır
        static bool InRing(Vector2[] ring, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                Vector2 a = ring[i], b = ring[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        // ---------------------------------------------------------------- kaydırma ve el çizimi

        /// <summary>
        /// Kapalı yolun hangi yanı koşulu sağlıyor: +1 sol normal (−dy, dx) yönü, −1 sağ. Yol boyunca 9 örnekte 6 px yandaki
        /// nokta denenir, çoğunluk karar verir.
        /// </summary>
        public static float SideOf(IReadOnlyList<Vector2> loop, Func<Vector2, bool> test)
        {
            int votes = 0, n = loop.Count;
            for (int s = 0; s < 9; s++)
            {
                int i = (int)((s + 0.5f) * n / 9f) % n;
                Vector2 a = loop[i], b = loop[(i + 1) % n], d = b - a;
                if (d.sqrMagnitude < 1e-4f) continue;
                var normal = new Vector2(-d.y, d.x).normalized;
                var mid = (a + b) * 0.5f;
                votes += test(mid + normal * 6) ? 1 : -1;
            }
            return votes >= 0 ? 1 : -1;
        }

        /// <summary>Kapalı yolu sol normal yönünde <paramref name="distance"/> kadar kaydırır (gönye, en çok 2 kat).</summary>
        public static List<Vector2> Offset(IReadOnlyList<Vector2> loop, float distance)
        {
            int n = loop.Count;
            var result = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = loop[(i - 1 + n) % n], p = loop[i], next = loop[(i + 1) % n];
                var n0 = Normal(prev, p);
                var n1 = Normal(p, next);
                var m = n0 + n1;
                if (m.sqrMagnitude < 1e-6f) m = n1;
                m.Normalize();
                float dot = Mathf.Max(0.5f, Vector2.Dot(m, n1));
                result.Add(p + m * (distance / dot));
            }
            return result;
        }

        static Vector2 Normal(Vector2 a, Vector2 b)
        {
            var d = b - a;
            return d.sqrMagnitude < 1e-8f ? Vector2.zero : new Vector2(-d.y, d.x).normalized;
        }

        /// <summary>Kapalı yolu Chaikin köşe kesmesiyle yumuşatır (<paramref name="passes"/> tur).</summary>
        public static List<Vector2> Smooth(IReadOnlyList<Vector2> loop, int passes)
        {
            var cur = new List<Vector2>(loop);
            for (int k = 0; k < passes; k++)
            {
                var next = new List<Vector2>(cur.Count * 2);
                for (int i = 0; i < cur.Count; i++)
                {
                    Vector2 a = cur[i], b = cur[(i + 1) % cur.Count];
                    next.Add(Vector2.Lerp(a, b, 0.25f));
                    next.Add(Vector2.Lerp(a, b, 0.75f));
                }
                cur = next;
            }
            return cur;
        }

        /// <summary>
        /// Kıyı taraması (eski deniz haritalarındaki su çizgileri): yumuşatılmış kıyı halkaları denize doğru
        /// <paramref name="distance"/> kadar kaydırılır.
        /// </summary>
        public List<IReadOnlyList<Vector2>> Waterline(float distance)
        {
            var list = new List<IReadOnlyList<Vector2>>(coastLoops.Count);
            foreach (var loop in coastLoops)
            {
                var smooth = Smooth(loop, 2);
                float side = SideOf(smooth, p => CellAt(p) < 0);
                list.Add(Offset(smooth, side * distance));
            }
            return list;
        }

        /// <summary>
        /// Kurşun kalemin ikinci geçişi: kıyı halkalarının köşeleri kimliksiz ama belirlenimci olarak
        /// <paramref name="jitter"/> px oynatılır (ince, soluk ikinci çizgi).
        /// </summary>
        public List<IReadOnlyList<Vector2>> Sketch(IReadOnlyList<IReadOnlyList<Vector2>> loops, float jitter, uint salt)
        {
            var list = new List<IReadOnlyList<Vector2>>(loops.Count);
            for (int l = 0; l < loops.Count; l++)
            {
                var loop = loops[l];
                var copy = new List<Vector2>(loop.Count);
                for (int i = 0; i < loop.Count; i++)
                {
                    // Yavaş değişen sapma: her 4. noktada yeni değer, arası doğrusal (titrek değil, elle yeniden geçilmiş)
                    int k0 = i / 4, k1 = k0 + 1;
                    float t = (i % 4) / 4f;
                    var a = new Vector2(Unit((uint)l, (uint)k0, salt), Unit((uint)l, (uint)k0, salt + 1));
                    var b = new Vector2(Unit((uint)l, (uint)k1, salt), Unit((uint)l, (uint)k1, salt + 1));
                    copy.Add(loop[i] + Vector2.Lerp(a, b, t) * jitter);
                }
                list.Add(copy);
            }
            return list;
        }

        /// <summary>
        /// Kablo hatları ip gibi: iki uç ve ortanın <paramref name="sag"/> oranında (boya göre, 6–22 px) aşağı sarktığı
        /// denetim noktası (Quadratic eğri için üç nokta).
        /// </summary>
        public List<IReadOnlyList<Vector2>> CableCurves(float sag)
        {
            var list = new List<IReadOnlyList<Vector2>>(cablePaths.Count);
            foreach (var c in cablePaths)
            {
                Vector2 a = c[0], b = c[1];
                float drop = Mathf.Clamp(Vector2.Distance(a, b) * sag, 6, 22);
                list.Add(new[] { a, (a + b) * 0.5f + new Vector2(0, drop), b });
            }
            return list;
        }

        /// <summary>İki köşe arasındaki kırık kenarın orta noktası.</summary>
        public Vector2 EdgeMiddle(int v1, int v2)
        {
            var e = Edge(v1, v2);
            return e[e.Length / 2];
        }

        /// <summary>
        /// İki bölgenin ortak sınır kenarlarından ortadaki (kenar ortalarının ağırlık merkezine en yakın); <paramref name="fromCell"/>
        /// <paramref name="from"/>'un, <paramref name="toCell"/> <paramref name="to"/>'nun hücresi. Ortak kenar yoksa null. Adaya
        /// giden ok (<c>GeneratedMapRenderer.SetFrontier</c>) ve yerleşimin S-07 engeli (<c>GeneratedMapLayout</c>) bunu kullanır.
        /// </summary>
        public MapEdge MiddleEdge(string from, string to, out int fromCell, out int toCell)
        {
            var list = new List<MapEdge>();
            var center = Vector2.zero;
            foreach (var e in Map.Borders)
                if (e.RegionA == from && e.RegionB == to || e.RegionA == to && e.RegionB == from)
                {
                    list.Add(e);
                    center += EdgeMiddle(e.V1, e.V2);
                }
            fromCell = toCell = -1;
            if (list.Count == 0) return null;
            center /= list.Count;
            MapEdge best = null;
            float bestD = float.MaxValue;
            foreach (var e in list)
            {
                float d = (EdgeMiddle(e.V1, e.V2) - center).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            fromCell = best.RegionA == from ? best.CellA : best.CellB;
            toCell = best.RegionA == from ? best.CellB : best.CellA;
            return best;
        }

        // ---------------------------------------------------------------- hücre

        public static Vector2 Center(MapCell c) => new Vector2((float)c.CX, (float)c.CY);

        /// <summary>Hücrenin kırık halkası: köşe 0'dan saat yönünde (ekranda), her köşeden sonra o kenarın ara noktaları.</summary>
        public IReadOnlyList<Vector2> CellRing(int cell) => rings[cell];

        /// <summary>Hücrenin k. kenarı (köşe k → k+1), iki uç dahil; komşu hücrede aynı noktalar ters sırayla gelir.</summary>
        public IReadOnlyList<Vector2> EdgePath(int cell, int k)
        {
            var c = Map.Cells[cell];
            return Edge(c.Vertex[k], c.Vertex[(k + 1) % 6]);
        }

        Vector2[] BuildRing(MapCell c)
        {
            var ring = new Vector2[RingPoints];
            int n = 0;
            for (int k = 0; k < 6; k++)
            {
                var e = Edge(c.Vertex[k], c.Vertex[(k + 1) % 6]);
                for (int i = 0; i < e.Length - 1; i++) ring[n++] = e[i];
            }
            return ring;
        }

        static float EdgeLength(MapCell c)
        {
            double dx = c.X[1] - c.X[0], dy = c.Y[1] - c.Y[0];
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// İki köşe arasındaki kırık kenar (a'dan b'ye, uçlar dahil). Kanonik yön (küçük kimlikten büyüğe) bir kez hesaplanır;
        /// ters yön aynı noktaların ters sırasıdır.
        /// </summary>
        static long EdgeKey(int a, int b)
        {
            int lo = a < b ? a : b, hi = a < b ? b : a;
            return ((long)lo << 32) | (uint)hi;
        }

        Vector2[] Edge(int a, int b)
        {
            bool flip = a > b;
            int lo = flip ? b : a, hi = flip ? a : b;
            long key = ((long)lo << 32) | (uint)hi;
            if (!edges.TryGetValue(key, out var pts))
            {
                Vector2 p = vertices[lo], q = vertices[hi], d = q - p;
                float len = d.magnitude;
                var normal = len > 1e-4f ? new Vector2(-d.y, d.x) / len : Vector2.zero;
                // Dalga: iki uçta sıfır; üç harmonik (1, 2, 3; ağırlık 1 / 0,5 / 0,35), toplamı en çok genlik × boy.
                // Üçüncü harmonik tümseği kenar boyunca böler: kıyı hex periyoduyla tekrar eden “bulut” gibi okunmaz.
                float amplitude = coastEdges.Contains(key) ? CoastAmplitude : EdgeAmplitude;
                float h1 = Unit((uint)lo, (uint)hi, 3), h2 = Unit((uint)lo, (uint)hi, 4), h3 = Unit((uint)lo, (uint)hi, 5);
                pts = new Vector2[EdgeSteps + 2];
                pts[0] = p;
                pts[EdgeSteps + 1] = q;
                edgeCenters.TryGetValue(key, out var owners);
                // Yıldız biçim koruması: kenar, taşıyan hücrelerin merkezinden bakınca geri dönüyorsa dalga yarıya iner (en çok
                // 4 kez, sonra düz); karar kanonik kenarda bir kez verilir, komşular yine aynı çizgiyi görür
                for (int attempt = 0; attempt <= 4; attempt++)
                {
                    float amp = attempt == 4 ? 0 : amplitude / (1 << attempt);
                    for (int i = 1; i <= EdgeSteps; i++)
                    {
                        float t = i / (float)(EdgeSteps + 1);
                        float wave = (h1 * Mathf.Sin(Mathf.PI * t) + 0.5f * h2 * Mathf.Sin(2 * Mathf.PI * t) + 0.35f * h3 * Mathf.Sin(3 * Mathf.PI * t)) / 1.85f;
                        pts[i] = Vector2.Lerp(p, q, t) + normal * (wave * amp * len);
                    }
                    if (StarShaped(pts, owners)) break;
                }
                edges[key] = pts;
            }
            if (!flip) return pts;
            var rev = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i++) rev[i] = pts[pts.Length - 1 - i];
            return rev;
        }

        /// <summary>Kırık kenarın her parçası, taşıyan hücre merkezlerinden bakınca kirişle aynı dönüş yönünde ve alanlı mı?</summary>
        static bool StarShaped(Vector2[] pts, List<Vector2> centers)
        {
            if (centers == null) return true;
            foreach (var c in centers)
            {
                float chord = Cross(pts[0] - c, pts[pts.Length - 1] - c);
                float sign = chord >= 0 ? 1 : -1;
                for (int i = 0; i + 1 < pts.Length; i++)
                    if (sign * Cross(pts[i] - c, pts[i + 1] - c) < 4f) return false;
            }
            return true;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>Halka + merkez: merkezden yelpaze üçgenleme; isabet testi halkayı (0..n) kullanır.</summary>
        static RegionMeshGraphic.Polygon Fan(Vector2[] ring, Vector2 center)
        {
            int n = ring.Length;
            var v = new Vector2[n + 1];
            Array.Copy(ring, v, n);
            v[n] = center;
            var tris = new int[n * 3];
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                tris[i * 3] = n;
                tris[i * 3 + 1] = i;
                tris[i * 3 + 2] = (i + 1) % n;
                var p = ring[i];
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }
            return new RegionMeshGraphic.Polygon
            {
                Vertices = v,
                RingOffsets = new[] { 0, n },
                Triangles = tris,
                Bounds = new Vector4(minX, minY, maxX, maxY),
            };
        }

        // ---------------------------------------------------------------- zincirleme

        /// <summary>
        /// Köşe kimliği çiftlerinden oluşan kenarları yollara bağlar: önce derecesi 2 olmayan köşelerden başlayan açık zincirler,
        /// sonra kalan halkalar (kapalı; son nokta tekrarlanmaz). Yol noktaları kırık kenarlardandır.
        /// </summary>
        List<(List<Vector2> path, bool closed)> Chain(List<(int a, int b)> list)
        {
            var adj = new Dictionary<int, List<int>>();
            for (int i = 0; i < list.Count; i++)
            {
                Add(list[i].a, i);
                Add(list[i].b, i);
            }
            var used = new bool[list.Count];
            var result = new List<(List<Vector2>, bool)>();
            // Belirlenimci sıra: kenar listesi sırasıyla
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < list.Count; i++)
                {
                    if (used[i]) continue;
                    int start = -1;
                    if (pass == 0)
                    {
                        if (adj[list[i].a].Count != 2) start = list[i].a;
                        else if (adj[list[i].b].Count != 2) start = list[i].b;
                        else continue;
                    }
                    else start = list[i].a;
                    var path = new List<Vector2>();
                    int at = start, edge = i;
                    bool closed = false;
                    while (edge >= 0)
                    {
                        used[edge] = true;
                        int next = list[edge].a == at ? list[edge].b : list[edge].a;
                        var e = Edge(at, next);
                        for (int j = path.Count == 0 ? 0 : 1; j < e.Length; j++) path.Add(e[j]);
                        at = next;
                        if (pass == 1 && at == start) { closed = true; break; }
                        if (adj[at].Count != 2) break;
                        edge = -1;
                        foreach (int o in adj[at]) if (!used[o]) { edge = o; break; }
                    }
                    if (closed) path.RemoveAt(path.Count - 1);
                    result.Add((path, closed));
                }
            return result;

            void Add(int v, int e)
            {
                if (!adj.TryGetValue(v, out var l)) adj[v] = l = new List<int>(3);
                l.Add(e);
            }
        }

        // ---------------------------------------------------------------- karma

        /// <summary>Kimliklerden [−1, 1] arası belirlenimci değer (görsel sapma; simülasyonun RNG'sine dokunmaz).</summary>
        static float Unit(uint a, uint b, uint salt)
        {
            uint h = a * 0x9E3779B1u ^ (b + 0x7F4A7C15u) * 0x85EBCA77u ^ salt * 0xC2B2AE3Du;
            h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0xFFFFFF * 2 - 1;
        }
    }
}
