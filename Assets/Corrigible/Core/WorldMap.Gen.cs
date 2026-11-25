using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Üretici: hücre ağı → kara maskesi (elips birleşimi kıtalar, koylar, en az iki hücrelik boğazlar) → 12 bölge (en uzak nokta
    /// tohumları, en küçüğün büyüdüğü taşma, iki tur ortaya çekme) → komşuluk (kara + üçüncü bir bölgenin karasından geçmeyen
    /// kıtalar arası kablo) → karargâh (derece 3–4 ve alan şartı) → paket dağıtımı (kademe sınırında açılış karıştırması) → adlar. Bir deneme kurallardan birini (<see cref="WorldMap.Validate"/>)
    /// tutturamazsa bir sonraki tuzla yeniden denenir; sınır aşılırsa sabit yedek düzen. Bütün kararlar tam sayı ya da
    /// <see cref="Rng"/> çıktısıyla; sıra anlamlıdır (değişirse harita değişir).
    /// </summary>
    public sealed partial class WorldMap
    {
        public const int MaxAttempts = 800;
        /// <summary>Kıta sayısı tohum başına bu kadar denemelik bloklarla çekilir (zor kıta sayısı ilk denemelerde elenmesin).</summary>
        public const int ContinentBlock = 80;
        /// <summary>Yedek düzen: bu tohumun bu denemesi geçerli harita verir (test kilitli).</summary>
        public const uint FallbackSeed = 45;
        public const int FallbackAttempt = 0;
        public const int MinLand = 110, MaxLand = 150, MinContinents = 2, MaxContinents = 4, MinLinks = 15, MaxLinks = 21;
        /// <summary>Kıta sayısı akışının tuzu ("KITA").</summary>
        const uint ContinentSalt = 0x4B495441u;
        /// <summary>Bundan küçük kara parçaları denize döner.</summary>
        const int MinIsland = 8;
        /// <summary>Köşe sapması: yarım hücre genişliğinin bu katı (yalnız çizim).</summary>
        const double Jitter = 0.2;
        /// <summary>
        /// Kıta sayısına göre (dizin k) elipslerin toplam alanı kıta hedefinin bu katı, onda bir (örtüşme, koylar ve boğazlar yer;
        /// iki hücrelik boğaz daha çok kara yer). C2'de ölçüldü: her k'da kara ortancası ~130, deneme başarısı %3–8.
        /// </summary>
        static readonly int[] AreaFactor10 = { 0, 0, 32, 48, 40 };
        /// <summary>
        /// Kıta sayısına göre en dar boğazın deniz hücresi sayısı. Dört kıtada iki hücrelik boğazlar 18×10'luk iç alanda 110 hücre karayı
        /// sığdırmıyor (deneme başarısı %0,1): orada bir hücre.
        /// </summary>
        public static readonly IReadOnlyList<int> StraitWidth = Array.AsReadOnly(new[] { 0, 0, 2, 2, 1 });
        /// <summary>Kıtaya koy açılma olasılığı (%) ve koyun alanı (kıta alanının yüzdesi).</summary>
        const int BayChance = 40, BayMin = 6, BayMax = 12;
        /// <summary>Kara kararının tam sayı koordinatı: köşe ağı birimi × (47, 27) ≈ tasarım px; bir hücrenin alanı ≈ 94 × 81.</summary>
        const int UnitX = 47, UnitY = 27, CellArea = 94 * 81;
        /// <summary>Kablo onarımında aşılabilecek en uzun deniz (hücre).</summary>
        const int RepairReach = 6;
        /// <summary>Ağaca girmeyen kıta çiftine ek kablo yalnız bu uzaklığa kadar (uzun kablo üçüncü kıtanın üstünden geçer).</summary>
        const int ExtraCableReach = 3;
        /// <summary>
        /// Açılış karıştırmasının olasılığı (%, harita başına; yalnız üç komşulu karargâh halkasında): bir erken paket halkadan
        /// hemen arkaya çıkar, bir orta paket halkaya girer (<c>MixOpening</c>). Karıştırmasız (üretici 2) haritaların %66'sında
        /// ilk karar tam {af, jk, sa} idi; %70 ile en sık küme %21 (tohum 1–500). Daha seyrek karıştırma en sık kümeyi %25'in
        /// üstüne çıkarır; erken paketi halkadan uzağa (2–4 halka) atan sürümler bot zaferini 4–7 puan düşürdü (ACILIS ölçümü).
        /// </summary>
        const int MixChance = 70;
        /// <summary>Açılış karıştırmasının akış tuzu ("KARI"): ana üretim akışından ayrı.</summary>
        const uint MixSalt = 0x4B415249u;
        /// <summary>
        /// Karargâhın alan şartı (üretici 4): en az bu kadar kalın hücre (<see cref="WorldMap.Thick"/>, <see cref="MapCell.Depth"/> ≥ 1).
        /// Oyuncunun ilk ekranında karargâhta S-07, ad, ilk cephe bloğu ve figür birlikte durur; tek kalın hücreli karargâhta
        /// (haritaların %69'u) cephe bloğu komşu karaya ya da konturun üstüne taşıyordu (Mono 300 tohum, S-07 serbestken
        /// %7 / %12; iki kalın hücreyle %1,5 / %1,5). Alan şartının ölçümü: HQ_UYGULA.
        /// </summary>
        public const int HqMinThick = 2;
        /// <summary>Karargâhın en az boyutu, ortalama bölge boyutunun onda biri cinsinden (9: %90).</summary>
        public const int HqMinSize10 = 9;
        /// <summary>
        /// <see cref="WorldMap.HqCell"/> harita kenarından en az bu kadar hücre içeride (sütun 2..17, satır 2..9). Dış halka hep deniz
        /// olduğundan kalın bir hücre zaten bu bantta: kural bugün hiç ret vermez, ızgara değişirse korur.
        /// </summary>
        public const int HqEdgeBand = 2;

        static readonly int[] DX = { 0, 1, 1, 0, -1, -1 }, DY = { -2, -1, 1, 2, 1, -1 };

        static readonly object cacheLock = new object();
        static WorldMap cached;

        // Üretim sırasındaki çalışma durumu (bölgeler 0..11 dizinle; paketler sonra yerleşir)
        int[] owner;
        int[] seedCell;
        int landCount;
        List<List<int>> comps;
        bool[,] adj;
        int[,] shared;
        readonly List<(int a, int b, int ca, int cb)> cables = new List<(int, int, int, int)>();
        int hqIndex;
        uint mixStream;
        int[] layer;
        string[] pkg;

        /// <summary>
        /// Harekât tohumunun haritası: ilk geçerli deneme, yoksa yedek düzen. Son üretilen harita önbellekte tutulur (kayıt
        /// süzgeci, yeniden oynatma ve özet aynı tohumu art arda ister); harita salt okunur olduğu için paylaşmak güvenlidir.
        /// </summary>
        public static WorldMap Generate(uint seed)
        {
            lock (cacheLock)
                if (cached != null && cached.Seed == seed) return cached;
            var map = GenerateUncached(seed);
            lock (cacheLock) cached = map;
            return map;
        }

        static WorldMap GenerateUncached(uint seed)
        {
            for (int a = 0; a < MaxAttempts; a++)
            {
                var m = Build(seed, a, seed, a, out _);
                if (m != null) return m;
            }
            return Build(seed, -1, FallbackSeed, FallbackAttempt, out _)
                ?? throw new InvalidOperationException("Yedek harita geçersiz.");
        }

        /// <summary>Tek bir deneme (tanı ve testler için; önbelleğe girmez): geçerliyse harita, değilse null ve bozulan kural.</summary>
        public static WorldMap TryAttempt(uint seed, int attempt, out string why) => Build(seed, attempt, seed, attempt, out why);

        static WorldMap Build(uint seed, int attempt, uint genSeed, int genAttempt, out string why)
        {
            var m = new WorldMap(seed, attempt);
            uint stream = Rng.Mix(genSeed, Salt + (uint)genAttempt);
            var rng = new Rng(stream);
            // Kıta sayısı denemeden değil tohumdan (blok blok): her deneme yeniden çekseydi kolay kıta sayısı (2) baskın çıkardı
            int k = ContinentCount(Rng.Mix(genSeed, ContinentSalt + (uint)(genAttempt / ContinentBlock)));
            m.mixStream = Rng.Mix(stream, MixSalt);
            m.BuildGrid(stream);
            why = m.BuildLand(rng, k) ?? m.BuildRegions(rng) ?? m.BuildLinks(rng);
            if (why != null) return null;
            m.PlacePackages(rng);
            m.Finish();
            why = m.Validate();
            return why == null ? m : null;
        }

        /// <summary>2 / 3 / 4 kıta: %35 / %40 / %25.</summary>
        static int ContinentCount(uint h) { uint v = h % 20; return v < 7 ? 2 : v < 15 ? 3 : 4; }

        static int Int(Rng r, int min, int max) => min + (int)Math.Floor(r.Next() * (max - min + 1));

        /// <summary>Tam sayı karekökü (aşağı yuvarlı, Newton).</summary>
        static long Isqrt(long v)
        {
            if (v <= 0) return 0;
            long x = v, y = (x + 1) / 2;
            while (y < x) { x = y; y = (x + v / x) / 2; }
            return x;
        }

        /// <summary>Karekökün en yakın tam sayısı (yarım yukarı): √v ≥ f + ½ ⇔ v − f² &gt; f.</summary>
        static long RoundSqrt(long v) { long f = Isqrt(v); return v - f * f > f ? f + 1 : f; }

        // ---------------------------------------------------------------- 1. hücre ağı

        void BuildGrid(uint stream)
        {
            double hw = Width / (2 * Cols + 1), hr = hw / Math.Sqrt(3.0);
            double oy = (Height - (3 * Rows + 1) * hr) / 2;
            uint js = Rng.Mix(stream, 0x56455254u);
            cells = new MapCell[Cols * Rows];
            Cells = Array.AsReadOnly(cells);
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    var cell = new MapCell { Index = r * Cols + c, Col = c, Row = r };
                    int xc = 2 * c + (r & 1) + 1, yc = 3 * r + 2;
                    double sx = 0, sy = 0;
                    for (int k = 0; k < 6; k++)
                    {
                        int vx = xc + DX[k], vy = yc + DY[k], id = vy * 64 + vx;
                        uint h = Rng.Mix(js, (uint)id);
                        double jx = ((h & 0xFFFF) / 65535.0 - 0.5) * 2 * Jitter * hw;
                        double jy = (((h >> 16) & 0xFFFF) / 65535.0 - 0.5) * 2 * Jitter * hw;
                        cell.vertex[k] = id;
                        cell.x[k] = Math.Max(0, Math.Min(Width, vx * hw + jx));
                        cell.y[k] = Math.Max(0, Math.Min(Height, oy + vy * hr + jy));
                        sx += cell.x[k]; sy += cell.y[k];
                    }
                    cell.CX = sx / 6; cell.CY = sy / 6;
                    for (int k = 0; k < 6; k++) cell.neighbor[k] = NeighborIndex(c, r, k);
                    cells[cell.Index] = cell;
                }
        }

        static int NeighborIndex(int c, int r, int k)
        {
            bool odd = (r & 1) == 1;
            int nc, nr;
            switch (k)
            {
                case 0: nc = odd ? c + 1 : c; nr = r - 1; break;  // kuzeydoğu
                case 1: nc = c + 1; nr = r; break;                // doğu
                case 2: nc = odd ? c + 1 : c; nr = r + 1; break;  // güneydoğu
                case 3: nc = odd ? c : c - 1; nr = r + 1; break;  // güneybatı
                case 4: nc = c - 1; nr = r; break;                // batı
                default: nc = odd ? c : c - 1; nr = r - 1; break; // kuzeybatı
            }
            return nc < 0 || nr < 0 || nc >= Cols || nr >= Rows ? -1 : nr * Cols + nc;
        }

        static int CX(int i) { int c = i % Cols, r = i / Cols; return (2 * c + (r & 1) + 1) * UnitX; }
        static int CY(int i) => (3 * (i / Cols) + 2) * UnitY;
        static bool Inner(int i) { int c = i % Cols, r = i / Cols; return c > 0 && r > 0 && c < Cols - 1 && r < Rows - 1; }

        /// <summary>Tam sayı koordinatı (x, y) hangi hücrenin içinde: en yakın merkez (eşitlikte küçük dizin); ızgara dışı −1.</summary>
        static int CellAt(long x, long y)
        {
            int r0 = (int)Math.Max(0, Math.Min(Rows - 1, (y - 2 * UnitY) / (3 * UnitY)));
            int best = -1;
            long bestD = long.MaxValue;
            for (int r = Math.Max(0, r0 - 1); r <= Math.Min(Rows - 1, r0 + 2); r++)
            {
                int c0 = (int)((x / UnitX - 1 - (r & 1)) / 2);
                for (int c = Math.Max(0, c0 - 1); c <= Math.Min(Cols - 1, c0 + 2); c++)
                {
                    int i = r * Cols + c;
                    long dx = x - CX(i), dy = y - CY(i), d = dx * dx + dy * dy;
                    if (d < bestD) { bestD = d; best = i; }
                }
            }
            return best;
        }

        // ---------------------------------------------------------------- 2. kara maskesi

        struct Ellipse { public int X, Y, A, B, Shear; }

        string BuildLand(Rng rng, int k)
        {
            int target = Int(rng, 116, 144);
            var weights = new int[k];
            int wsum = 0;
            for (int j = 0; j < k; j++) { weights[j] = k == 4 ? Int(rng, 8, 12) : Int(rng, 6, 15); wsum += weights[j]; }

            // Merkezler iç alanın içinde, birbirinden uzak (k'ya göre en az uzaklık; 40 denemede en iyisi)
            int x0 = 3 * UnitX + 150, x1 = 38 * UnitX - 150, y0 = 5 * UnitY + 110, y1 = 32 * UnitY - 110;
            int dmin = k == 2 ? 640 : k == 3 ? 520 : 430;
            var centers = new List<(int x, int y)>();
            for (int j = 0; j < k; j++)
            {
                (int x, int y) best = (0, 0);
                long bestD = -1;
                for (int t = 0; t < 40; t++)
                {
                    int x = Int(rng, x0, x1), y = Int(rng, y0, y1);
                    long d = long.MaxValue;
                    foreach (var c in centers) d = Math.Min(d, (long)(x - c.x) * (x - c.x) + (long)(y - c.y) * (y - c.y));
                    if (d > bestD) { bestD = d; best = (x, y); }
                    if (d >= (long)dmin * dmin) break;
                }
                centers.Add(best);
            }

            var ellipses = new List<Ellipse>();
            var ofContinent = new List<int>();
            var bays = new List<Ellipse>();
            for (int j = 0; j < k; j++)
            {
                long area = (long)target * weights[j] * CellArea * AreaFactor10[k] / (10L * wsum);
                int n = Int(rng, 2, 4);
                var main = MakeEllipse(rng, centers[j].x, centers[j].y, area * 60 / 100);
                ellipses.Add(main);
                ofContinent.Add(j);
                for (int i = 1; i < n; i++)
                {
                    int ox = Int(rng, -main.A, main.A), oy = Int(rng, -main.B, main.B);
                    ellipses.Add(MakeEllipse(rng, centers[j].x + ox, centers[j].y + oy, area * 55 / (100L * (n - 1))));
                    ofContinent.Add(j);
                }
                // Koy: ana elipsin kenarında bir elips denize döner (dolu dikdörtgen silüeti kırar)
                if (Int(rng, 0, 99) < BayChance)
                {
                    int dir = Int(rng, 0, 5);
                    int bx = new[] { main.A, -main.A, main.A / 2, -main.A / 2, main.A / 2, -main.A / 2 }[dir];
                    int by = new[] { 0, 0, main.B, main.B, -main.B, -main.B }[dir];
                    bays.Add(MakeEllipse(rng, centers[j].x + bx, centers[j].y + by, area * Int(rng, BayMin, BayMax) / 100));
                }
            }

            // Hücre, kendisini içeren elipslerin kıtalarından merkezi en yakın olana (koy elipsindeyse deniz). "İlk elips kazanır"
            // olsaydı küçük sıralı kıta ötekini yutardı ve üç-dört kıta nadiren tutardı.
            var cont = Enumerable.Repeat(-1, cells.Length).ToArray();
            for (int i = 0; i < cells.Length; i++)
            {
                if (!Inner(i) || bays.Any(b => InEllipse(b, CX(i), CY(i)))) continue;
                long bestD = long.MaxValue;
                for (int e = 0; e < ellipses.Count; e++)
                {
                    int j = ofContinent[e];
                    if (j == cont[i] || !InEllipse(ellipses[e], CX(i), CY(i))) continue;
                    long dx = CX(i) - centers[j].x, dy = CY(i) - centers[j].y, d = dx * dx + dy * dy;
                    if (d < bestD) { bestD = d; cont[i] = j; }
                }
            }
            // Boğaz. Bir hücrelik (dört kıta): başka kıtaya değen hücre, küçük sıralı kıtaya değiyorsa denize döner. İki hücrelik:
            // önce başka kıtaya değen hücreler iki yandan denize döner, sonra iki adım ötesinde küçük sıralı bir kıtanın kalan karası
            // olan hücreler; iki kıtanın kalan karası arasında en az iki deniz hücresi kalır.
            bool wide = StraitWidth[k] == 2;
            var land = new bool[cells.Length];
            for (int i = 0; i < cells.Length; i++)
                land[i] = cont[i] >= 0 && !cells[i].neighbor.Any(n => n >= 0 && cont[n] >= 0 && (wide ? cont[n] != cont[i] : cont[n] < cont[i]));
            if (wide)
            {
                var kept = (bool[])land.Clone();
                for (int i = 0; i < cells.Length; i++)
                {
                    if (!land[i]) continue;
                    foreach (int n in cells[i].neighbor)
                        if (n >= 0)
                            foreach (int m in cells[n].neighbor)
                                if (m >= 0 && land[m] && cont[m] < cont[i]) kept[i] = false;
                }
                land = kept;
            }
            FillLakes(land);
            foreach (var comp in Components(land)) if (comp.Count < MinIsland) foreach (int i in comp) land[i] = false;

            comps = Components(land);
            landCount = land.Count(x => x);
            for (int i = 0; i < cells.Length; i++) cells[i].Land = land[i];
            for (int c = 0; c < comps.Count; c++) foreach (int i in comps[c]) cells[i].Continent = c;
            Continents = comps.Count;
            if (landCount < MinLand) return "kara_az";
            if (landCount > MaxLand) return "kara_cok";
            // Çekilen kıta sayısı tutmalı (kıta yutulursa ya da koy ikiye bölerse yeniden dene): dağılım çekilişi izler
            if (comps.Count != k) return "kita";
            // Koyun ya da elips arasının ayırdığı parçalar arasında da boğaz dar olmasın
            if (wide && NarrowStrait()) return "bogaz";
            return null;
        }

        /// <summary>Alanı verilen (birim px²) eğik elips; yarıçaplar tam sayı karekökle (π ≈ 355/113).</summary>
        static Ellipse MakeEllipse(Rng rng, int x, int y, long area)
        {
            int aspect = Int(rng, 55, 185); // en/boy × 100
            int a = (int)Math.Max(60, RoundSqrt(area * aspect * 113 / 35500));
            int b = (int)Math.Max(50, (area * 113 * 2 + 355L * a) / (2 * 355L * a));
            return new Ellipse { X = x, Y = y, A = a, B = b, Shear = Int(rng, -2, 2) };
        }

        /// <summary>Eğik elips testi, tam sayıyla: u = 4dx + eğim·dy, v = 4dy; (u·b)² + (v·a)² ≤ (4ab)².</summary>
        static bool InEllipse(Ellipse e, int x, int y)
        {
            long dx = x - e.X, dy = y - e.Y;
            long u = 4 * dx + e.Shear * dy, w = 4 * dy;
            long ub = u * e.B, wa = w * e.A, ab = 4L * e.A * e.B;
            return ub * ub + wa * wa <= ab * ab;
        }

        /// <summary>Dış denize bağlı olmayan deniz hücreleri (göller) kara olur.</summary>
        void FillLakes(bool[] land)
        {
            var sea = new bool[land.Length];
            var queue = new Queue<int>();
            for (int i = 0; i < land.Length; i++) if (!Inner(i) && !land[i]) { sea[i] = true; queue.Enqueue(i); }
            while (queue.Count > 0)
                foreach (int n in cells[queue.Dequeue()].neighbor)
                    if (n >= 0 && !land[n] && !sea[n]) { sea[n] = true; queue.Enqueue(n); }
            for (int i = 0; i < land.Length; i++) if (!land[i] && !sea[i]) land[i] = true;
        }

        /// <summary>Kara bileşenleri, en küçük dizinli hücresine göre sıralı; her bileşenin hücreleri artan dizin.</summary>
        List<List<int>> Components(bool[] land)
        {
            var seen = new bool[land.Length];
            var list = new List<List<int>>();
            for (int s = 0; s < land.Length; s++)
            {
                if (!land[s] || seen[s]) continue;
                var comp = new List<int>();
                var queue = new Queue<int>();
                queue.Enqueue(s); seen[s] = true;
                while (queue.Count > 0)
                {
                    int i = queue.Dequeue();
                    comp.Add(i);
                    foreach (int n in cells[i].neighbor) if (n >= 0 && land[n] && !seen[n]) { seen[n] = true; queue.Enqueue(n); }
                }
                comp.Sort();
                list.Add(comp);
            }
            return list;
        }

        // ---------------------------------------------------------------- 3. bölgeler

        string BuildRegions(Rng rng)
        {
            int k = comps.Count;
            var quota = new int[k];
            var rem = new long[k];
            int sum = 0;
            for (int c = 0; c < k; c++)
            {
                quota[c] = Math.Max(1, RegionCount * comps[c].Count / landCount);
                rem[c] = (long)RegionCount * comps[c].Count - (long)quota[c] * landCount;
                sum += quota[c];
            }
            while (sum < RegionCount)
            {
                int best = 0;
                for (int c = 1; c < k; c++) if (rem[c] > rem[best]) best = c;
                quota[best]++; rem[best] -= landCount; sum++;
            }
            while (sum > RegionCount)
            {
                int best = -1;
                for (int c = 0; c < k; c++) if (quota[c] > 1 && (best < 0 || rem[c] < rem[best])) best = c;
                quota[best]--; rem[best] += landCount; sum--;
            }
            for (int c = 0; c < k; c++)
                if (10L * RegionCount * comps[c].Count < 7L * landCount * quota[c] || 10L * RegionCount * comps[c].Count > 13L * landCount * quota[c])
                    return "kota";

            // En uzak nokta tohumları (bileşen içinde BFS uzaklığı; eşitlikte rastgele)
            var seeds = new List<int>();
            for (int c = 0; c < k; c++)
            {
                var list = comps[c];
                var mine = new List<int> { list[Int(rng, 0, list.Count - 1)] };
                while (mine.Count < quota[c])
                {
                    var dist = Bfs(mine, i => cells[i].Land);
                    int max = list.Max(i => dist[i]);
                    var ties = list.Where(i => dist[i] == max).ToList();
                    mine.Add(ties[Int(rng, 0, ties.Count - 1)]);
                }
                seeds.AddRange(mine);
            }

            Grow(seeds);
            // İki tur ortaya çekme: her bölgenin tohumu ortanca hücresi (bölge hücrelerine uzaklık toplamı en küçük) olur
            for (int pass = 0; pass < 2; pass++)
            {
                for (int r = 0; r < RegionCount; r++)
                {
                    var mine = RegionCells(r);
                    seeds[r] = mine.OrderBy(i => mine.Sum(j => HexDistance(i, j))).ThenBy(i => i).First();
                }
                Grow(seeds);
            }
            seedCell = seeds.ToArray();
            for (int i = 0; i < cells.Length; i++) if (cells[i].Land && owner[i] < 0) return "sahipsiz_kara";
            for (int r = 0; r < RegionCount; r++)
            {
                var mine = RegionCells(r);
                if (!SizeOk(mine.Count, landCount)) return "boyut";
                if (!mine.Any(i => cells[i].neighbor.All(n => n >= 0 && owner[n] == r))) return "kalin";
            }
            return null;
        }

        List<int> RegionCells(int r)
        {
            var list = new List<int>();
            for (int i = 0; i < owner.Length; i++) if (owner[i] == r) list.Add(i);
            return list;
        }

        int[] Bfs(IEnumerable<int> sources, Func<int, bool> pass)
        {
            var dist = Enumerable.Repeat(int.MaxValue, cells.Length).ToArray();
            var queue = new Queue<int>();
            foreach (int s in sources) { dist[s] = 0; queue.Enqueue(s); }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                foreach (int n in cells[i].neighbor)
                    if (n >= 0 && pass(n) && dist[n] == int.MaxValue) { dist[n] = dist[i] + 1; queue.Enqueue(n); }
            }
            return dist;
        }

        /// <summary>
        /// Çok kaynaklı sıralı taşma: her adımda büyüyebilen en küçük bölge (eşitlikte küçük dizin) bir komşu kara hücresi alır;
        /// aday, bölgeye en çok komşu olan, sonra tohumuna en yakın, sonra en küçük dizinli hücre. Bölgeler bağlı ve dengeli kalır.
        /// </summary>
        void Grow(List<int> seeds)
        {
            owner = Enumerable.Repeat(-1, cells.Length).ToArray();
            var size = new int[RegionCount];
            for (int r = 0; r < RegionCount; r++) { owner[seeds[r]] = r; size[r] = 1; }
            var bestCell = new int[RegionCount];
            var bestTouch = new int[RegionCount];
            var bestDist = new int[RegionCount];
            var touch = new int[RegionCount];
            for (int left = landCount - RegionCount; left > 0; left--)
            {
                for (int r = 0; r < RegionCount; r++) bestCell[r] = -1;
                for (int i = 0; i < cells.Length; i++)
                {
                    if (!cells[i].Land || owner[i] >= 0) continue;
                    Array.Clear(touch, 0, RegionCount);
                    foreach (int n in cells[i].neighbor) if (n >= 0 && owner[n] >= 0) touch[owner[n]]++;
                    for (int r = 0; r < RegionCount; r++)
                    {
                        if (touch[r] == 0) continue;
                        int d = HexDistance(i, seeds[r]);
                        if (bestCell[r] < 0 || touch[r] > bestTouch[r] || touch[r] == bestTouch[r] && d < bestDist[r])
                        { bestCell[r] = i; bestTouch[r] = touch[r]; bestDist[r] = d; }
                    }
                }
                int pick = -1;
                for (int r = 0; r < RegionCount; r++) if (bestCell[r] >= 0 && (pick < 0 || size[r] < size[pick])) pick = r;
                if (pick < 0) return;
                owner[bestCell[pick]] = pick;
                size[pick]++;
            }
        }

        // ---------------------------------------------------------------- 4. komşuluk ve karargâh

        string BuildLinks(Rng rng)
        {
            adj = new bool[RegionCount, RegionCount];
            shared = new int[RegionCount, RegionCount];
            for (int i = 0; i < cells.Length; i++)
            {
                if (!cells[i].Land) continue;
                foreach (int n in cells[i].neighbor)
                    if (n > i && cells[n].Land && owner[n] != owner[i])
                    {
                        shared[owner[i], owner[n]]++; shared[owner[n], owner[i]]++;
                        adj[owner[i], owner[n]] = adj[owner[n], owner[i]] = true;
                    }
            }

            // Kıtalar arası kablolar: kıta çiftlerinin en yakın açık (üçüncü bölgenin karasından ve başka kablodan geçmeyen) kıyı
            // hücreleri; en kısa bağlantılarla kapsayan ağaç. Açık hattı olmayan çift ağaca giremez.
            var pairs = new List<(int d, int p, int q, int a, int b)>();
            for (int p = 0; p < comps.Count; p++)
                for (int q = p + 1; q < comps.Count; q++)
                {
                    var (d, a, b) = ClosestCable(comps[p], comps[q], (x, y) => true);
                    if (a >= 0) pairs.Add((d, p, q, a, b));
                }
            pairs = pairs.OrderBy(x => x.d).ThenBy(x => x.p).ThenBy(x => x.q).ToList();
            var parent = Enumerable.Range(0, comps.Count).ToArray();
            int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
            var unused = new List<(int d, int p, int q, int a, int b)>();
            foreach (var pr in pairs)
            {
                int fp = Find(pr.p), fq = Find(pr.q);
                if (fp == fq) { unused.Add(pr); continue; }
                // Ağaçtaki önceki kablolar hattı kesmiş olabilir: yeniden ara
                var (d, a, b) = ClosestCable(comps[pr.p], comps[pr.q], (x, y) => true);
                if (a < 0) continue;
                parent[fp] = fq;
                AddCable(a, b);
            }
            // Ek kablo (yarı yarıya): ağaca girmeyen en kısa kıta çifti, yalnız kısa ise; tek çiftte aynı iki kıtanın bağlı olmayan
            // ikinci en yakın bölgeleri
            if (rng.Next() < 0.5)
            {
                if (unused.Count > 0)
                {
                    if (unused[0].d <= ExtraCableReach)
                    {
                        var (d, a, b) = ClosestCable(comps[unused[0].p], comps[unused[0].q], (x, y) => true);
                        if (a >= 0 && d <= ExtraCableReach) AddCable(a, b);
                    }
                }
                else if (pairs.Count == 1)
                {
                    var (d2, a2, b2) = ClosestCable(comps[pairs[0].p], comps[pairs[0].q], (x, y) => !Linked(owner[x], owner[y]));
                    if (a2 >= 0 && d2 <= pairs[0].d + 3) AddCable(a2, b2);
                }
            }
            // Onarım: derecesi 2'nin altındaki bölge başka kıtadaki en yakın bağlantısız bölgeye kabloyla bağlanır
            for (int r = 0; r < RegionCount; r++)
            {
                if (Degree(r) >= 2) continue;
                var mine = RegionCells(r);
                var others = Enumerable.Range(0, cells.Length).Where(i => cells[i].Land && cells[i].Continent != cells[mine[0]].Continent).ToList();
                var (d, a, b) = ClosestCable(mine, others, (x, y) => !Linked(r, owner[y]) && Degree(owner[y]) < 5);
                if (a >= 0 && d <= RepairReach) AddCable(a, b);
            }

            for (int r = 0; r < RegionCount; r++) if (Degree(r) < 2 || Degree(r) > 5) return "derece";
            int count = 0;
            for (int a = 0; a < RegionCount; a++) for (int b = a + 1; b < RegionCount; b++) if (adj[a, b]) count++;
            if (count < MinLinks || count > MaxLinks) return "toplam";

            // Karargâh: derecesi 3–4 olan ve alan şartını (HqMinThick, HqMinSize10, HqEdgeBand) tutan bölgelerden biri
            // (ızgaranın dış halkası deniz: hiçbir bölge harita kenarına yapışık değil). Zar yalnız aday varsa, bir kez atılır;
            // adaysız deneme reddedilir (deneme başına tuz ayrı, belirlenimcilik bozulmaz).
            var hq = Enumerable.Range(0, RegionCount).Where(r => Degree(r) >= 3 && Degree(r) <= 4).ToList();
            if (hq.Count == 0) return "karargah";
            hq = hq.Where(HqRoomy).ToList();
            if (hq.Count == 0) return "karargah_alan";
            hqIndex = hq[Int(rng, 0, hq.Count - 1)];
            return null;
        }

        /// <summary>Bölge karargâh olabilecek kadar geniş mi (üretim sırasında, <c>owner</c> üstünden; <see cref="WorldMap.Validate"/> ile aynı kural).</summary>
        bool HqRoomy(int r)
        {
            var mine = RegionCells(r);
            if (10 * RegionCount * mine.Count < HqMinSize10 * landCount) return false;
            if (mine.Count(i => cells[i].neighbor.All(n => n >= 0 && owner[n] == r)) < HqMinThick) return false;
            return InEdgeBand(InnerOrder(mine, i => owner[i] == r, out _)[0]);
        }

        /// <summary>Hücre harita kenarından en az <see cref="HqEdgeBand"/> hücre içeride mi?</summary>
        static bool InEdgeBand(int i)
        {
            int c = i % Cols, r = i / Cols;
            return c >= HqEdgeBand && r >= HqEdgeBand && c < Cols - HqEdgeBand && r < Rows - HqEdgeBand;
        }

        /// <summary>
        /// Bölgenin iç sırası (<see cref="MapRegion.Inner"/>): derinlik (sınıra BFS, <paramref name="dist"/>) büyükten küçüğe, sonra
        /// bölge hücrelerine altıgen uzaklıkları toplamı, sonra dizin.
        /// </summary>
        List<int> InnerOrder(List<int> mine, Func<int, bool> inRegion, out int[] dist)
        {
            var border = mine.Where(i => cells[i].neighbor.Any(n => n < 0 || !inRegion(n))).ToList();
            var d = dist = Bfs(border, inRegion);
            return mine.OrderByDescending(i => d[i]).ThenBy(i => mine.Sum(j => HexDistance(i, j))).ThenBy(i => i).ToList();
        }

        bool Linked(int a, int b) => a == b || adj[a, b];

        int Degree(int r)
        {
            int d = 0;
            for (int x = 0; x < RegionCount; x++) if (adj[r, x]) d++;
            return d;
        }

        void AddCable(int cellA, int cellB)
        {
            int a = owner[cellA], b = owner[cellB];
            if (Linked(a, b)) return;
            adj[a, b] = adj[b, a] = true;
            cables.Add((a, b, cellA, cellB));
        }

        /// <summary>
        /// Kablo için en yakın açık kıyı hücresi çifti: önce henüz kablo ucu olmayan hücreler (iki kablo aynı hücreden çıkmasın),
        /// yoksa hepsi. Yoksa (max, −1, −1).
        /// </summary>
        (int d, int a, int b) ClosestCable(List<int> from, List<int> to, Func<int, int, bool> ok)
        {
            var used = new HashSet<int>();
            foreach (var c in cables) { used.Add(c.ca); used.Add(c.cb); }
            var best = Closest(from, to, (x, y) => !used.Contains(x) && !used.Contains(y) && ok(x, y));
            return best.a >= 0 ? best : Closest(from, to, ok);
        }

        /// <summary>İki hücre kümesi arasındaki en yakın açık çift (altıgen uzaklığı; eşitlikte küçük dizinler); yoksa (max, −1, −1).</summary>
        (int d, int a, int b) Closest(List<int> from, List<int> to, Func<int, int, bool> ok)
        {
            (int d, int a, int b) best = (int.MaxValue, -1, -1);
            foreach (int a in from)
            {
                if (!Coastal(a)) continue;
                foreach (int b in to)
                {
                    if (!Coastal(b)) continue;
                    int d = HexDistance(a, b);
                    if (d < best.d && ok(a, b) && CableClear(a, b)) best = (d, a, b);
                }
            }
            return best;
        }

        /// <summary>
        /// Kablo hattı (iki hücre merkezi arası doğru) açık mı: üzerindeki örnek noktaların hücresi deniz ya da iki uç bölgeden
        /// birinin karası; ayrıca var olan bir kabloyu kesmiyor. Tam sayıyla (hücre başına 6 örnek, çapraz çarpım).
        /// </summary>
        bool CableClear(int ca, int cb)
        {
            int a = owner[ca], b = owner[cb];
            long ax = CX(ca), ay = CY(ca), bx = CX(cb), by = CY(cb);
            int steps = 6 * Math.Max(1, HexDistance(ca, cb));
            for (int t = 1; t < steps; t++)
            {
                int i = CellAt(ax + (bx - ax) * t / steps, ay + (by - ay) * t / steps);
                if (i >= 0 && cells[i].Land && owner[i] != a && owner[i] != b) return false;
            }
            foreach (var c in cables)
                if (c.ca != ca && c.cb != ca && c.ca != cb && c.cb != cb
                    && Crosses(ax, ay, bx, by, CX(c.ca), CY(c.ca), CX(c.cb), CY(c.cb))) return false;
            return true;
        }

        /// <summary>İki doğru parçası birbirini içten kesiyor mu (tam sayı çapraz çarpım).</summary>
        static bool Crosses(long ax, long ay, long bx, long by, long cx, long cy, long dx, long dy)
        {
            long d1 = Math.Sign((bx - ax) * (cy - ay) - (by - ay) * (cx - ax)), d2 = Math.Sign((bx - ax) * (dy - ay) - (by - ay) * (dx - ax));
            long d3 = Math.Sign((dx - cx) * (ay - cy) - (dy - cy) * (ax - cx)), d4 = Math.Sign((dx - cx) * (by - cy) - (dy - cy) * (bx - cx));
            return d1 * d2 < 0 && d3 * d4 < 0;
        }

        bool Coastal(int i) => cells[i].Land && cells[i].neighbor.Any(n => n < 0 || !cells[n].Land);

        // ---------------------------------------------------------------- 5. paketler ve adlar

        void PlacePackages(Rng rng)
        {
            // Karargâhtan uzaklık katmanları
            layer = Enumerable.Repeat(int.MaxValue, RegionCount).ToArray();
            layer[hqIndex] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(hqIndex);
            while (queue.Count > 0)
            {
                int r = queue.Dequeue();
                for (int x = 0; x < RegionCount; x++) if (adj[r, x] && layer[x] == int.MaxValue) { layer[x] = layer[r] + 1; queue.Enqueue(x); }
            }
            var keyR = Enumerable.Range(0, RegionCount).Select(_ => rng.Next()).ToArray();
            var ids = GameData.Regions.Select(d => d.Id).Where(id => id != Simulation.HQ).ToList();
            var keyP = ids.ToDictionary(id => id, _ => rng.Next());
            var regions = Enumerable.Range(0, RegionCount).Where(r => r != hqIndex).OrderBy(r => layer[r]).ThenBy(r => keyR[r]).ThenBy(r => r).ToList();
            var packages = ids.OrderBy(id => PackageStage[id]).ThenBy(id => keyP[id]).ThenBy(id => id, StringComparer.Ordinal).ToList();
            MixOpening(packages, regions);
            pkg = new string[RegionCount];
            pkg[hqIndex] = Simulation.HQ;
            for (int i = 0; i < regions.Count; i++) pkg[regions[i]] = packages[i];
        }

        /// <summary>
        /// Kademe sınırında olasılıklı karıştırma (<see cref="MixChance"/>, ayrı akış). Paket listesi kademe sırasında; liste yeri i
        /// <paramref name="regions"/>[i] bölgesine düşer (ilk yerler karargâh halkası). Yalnız halkası erken paket sayısı kadar
        /// olan haritada (dört komşulu halkada bir orta paket zaten var): zar tutarsa rastgele bir erken paket, halkanın dışındaki
        /// orta paketlerden birinin yerine geçer; o orta paket halkaya girer. Erken paketin gideceği yer halkaya en çok komşu olan
        /// orta paket yeridir (paket bir halka dışarı çıkar ama hemen arkada kalır: halkadaki bir bölge bitince sınıra girer).
        /// Zarlar her zaman atılır (akış sabit). Halkada erken paket kalmazsa
        /// <see cref="WorldMap.Validate"/> denemeyi reddeder.
        /// </summary>
        void MixOpening(List<string> packages, List<int> regions)
        {
            var mix = new Rng(mixStream);
            bool hit = Int(mix, 0, 99) < MixChance;
            int early = packages.Count(id => PackageStage[id] == 1), middle = packages.Count(id => PackageStage[id] == 2);
            int leave = Int(mix, 0, early - 1);
            var tie = regions.Select(_ => mix.Next()).ToArray();
            int ring = regions.Count(r => layer[r] == 1);
            if (!hit || ring != early) return;
            int best = -1, bestTouch = -1;
            for (int i = early; i < early + middle; i++)
            {
                int touch = regions.Count(r => layer[r] == 1 && adj[r, regions[i]]);
                if (touch > bestTouch || touch == bestTouch && tie[i] < tie[best]) { best = i; bestTouch = touch; }
            }
            var t = packages[leave]; packages[leave] = packages[best]; packages[best] = t;
        }

        // ---------------------------------------------------------------- 6. sunumun okuduğu türetilmiş veri

        void Finish()
        {
            var order = GameData.Regions.Select(d => d.Id).ToList();
            byId = new Dictionary<string, MapRegion>();
            var index = new Dictionary<string, int>();
            foreach (var def in GameData.Regions)
            {
                int r = Array.IndexOf(pkg, def.Id);
                index[def.Id] = r;
                var region = new MapRegion { Id = def.Id, SourceName = def.Ai, Stage = PackageStage[def.Id], Distance = layer[r] };
                region.cells.AddRange(RegionCells(r));
                region.Continent = cells[region.cells[0]].Continent;
                regions.Add(region);
                byId[def.Id] = region;
            }
            foreach (var cell in cells) cell.Region = cell.Land ? pkg[owner[cell.Index]] : null;

            // Derinlik (bölge sınırına BFS) ve en iç hücre adayları
            foreach (var r in regions)
            {
                var inner = InnerOrder(r.cells, i => cells[i].Region == r.Id, out var dist);
                foreach (int i in r.cells) cells[i].Depth = dist[i];
                r.inner.AddRange(inner);
            }

            // Bağlantılar (paket sırasıyla) ve komşu listeleri
            for (int a = 0; a < order.Count; a++)
                for (int b = a + 1; b < order.Count; b++)
                {
                    int ra = index[order[a]], rb = index[order[b]];
                    if (!adj[ra, rb]) continue;
                    var link = new MapLink { A = order[a], B = order[b], SharedEdges = shared[ra, rb] };
                    foreach (var c in cables)
                    {
                        if (c.a == ra && c.b == rb) { link.CellA = c.ca; link.CellB = c.cb; }
                        else if (c.a == rb && c.b == ra) { link.CellA = c.cb; link.CellB = c.ca; }
                        else continue;
                        link.Cable = true;
                        link.X1 = cells[link.CellA].CX; link.Y1 = cells[link.CellA].CY;
                        link.X2 = cells[link.CellB].CX; link.Y2 = cells[link.CellB].CY;
                    }
                    links.Add(link);
                }
            foreach (var r in regions)
            {
                r.neighbors.Clear();
                foreach (var id in order) if (id != r.Id && Adjacent(r.Id, id)) r.neighbors.Add(id);
            }
            LinkDefs = Array.AsReadOnly(links.Select(l => new LinkDef { A = l.A, B = l.B }).ToArray());

            // Kıyı ve bölge sınırı kenarları
            foreach (var cell in cells)
            {
                if (!cell.Land) continue;
                for (int k = 0; k < 6; k++)
                {
                    int n = cell.neighbor[k];
                    bool sea = n < 0 || !cells[n].Land;
                    if (!sea && (cells[n].Region == cell.Region || n < cell.Index)) continue;
                    var e = new MapEdge
                    {
                        CellA = cell.Index, CellB = n, RegionA = cell.Region, RegionB = sea ? null : cells[n].Region,
                        V1 = cell.vertex[k], V2 = cell.vertex[(k + 1) % 6],
                        X1 = cell.x[k], Y1 = cell.y[k], X2 = cell.x[(k + 1) % 6], Y2 = cell.y[(k + 1) % 6],
                    };
                    (sea ? coast : borders).Add(e);
                }
            }
            Fingerprint = BuildFingerprint();
        }
    }
}
