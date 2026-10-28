using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Harekâtın taktik harita havuzu: yeni oyunun tohumundan deterministik üretilen 8×8 yerleşimler (<see cref="Baskin.Layouts"/>
    /// ile aynı biçim). Her harekâtın kendi havuzu olur; baskınlar havuzu karıştırılmış torbalardan sırayla çeker (<see cref="Slot"/>). Kendi
    /// üretecini kullanır: <c>S.Rng</c>'den tüketmez, kaydı ve baskın tohumlarını değiştirmez.
    /// Havuz altı türden beşer haritadır (<see cref="Kinds"/>); her tür ayrı bir taktik sorun kurar: koridor (dağınık kısa duvarlar),
    /// kasa odası (hedef iki kapılı odada), iki hat (uzun bir duvar tahtayı ikiye böler), açık salon (tek karelik sütunlar,
    /// açık atış hatları), şebeke (ortada kablo kuşağı: itilen yanar), rehine (siviller yolun ortasında). Bütün türlerde sol
    /// kenarda üç giriş, tek hedef dolap ve yanında duvar, 4–5 sivil, 2 soğutma, 2 kopya dolabı, 2 dron ve 1 bakım robotu; hiçbir
    /// dronun girişe açık atış hattı yok. Girişten hedefe uzaklık türün zorluğunu ayarlar (bot ölçümüyle, <see cref="PathRange"/>).
    /// </summary>
    public static class RaidMaps
    {
        public const int PoolSize = 30;
        const int N = Baskin.N;

        public const string Corridor = "koridor", Vault = "oda", Lanes = "serit", Hall = "salon", Grid = "sebeke", Hostage = "rehine";
        /// <summary>Harita türleri; havuzda her türden eşit sayıda harita.</summary>
        public static readonly string[] Kinds = { Corridor, Vault, Lanes, Hall, Grid, Hostage };

        /// <summary>Her türün oda adları (türün haritaları bu adları alır).</summary>
        static readonly Dictionary<string, string[]> Names = new Dictionary<string, string[]>
        {
            [Corridor] = new[] { Loc.N("Kablo galerisi"), Loc.N("Bakım tüneli"), Loc.N("Güvenlik koridoru"), Loc.N("Yük asansörü holü"), Loc.N("Havalandırma şaftı") },
            [Vault] = new[] { Loc.N("Arşiv katı"), Loc.N("Kontrol odası"), Loc.N("Yedekleme merkezi"), Loc.N("Batarya odası"), Loc.N("Sinyal odası") },
            [Lanes] = new[] { Loc.N("Depolama dizisi"), Loc.N("Montaj hattı"), Loc.N("Yükleme rampası"), Loc.N("Metro peronu"), Loc.N("Atık su kanalı") },
            [Hall] = new[] { Loc.N("Sunucu salonu"), Loc.N("Ağ geçidi"), Loc.N("Veri ambarı"), Loc.N("Otopark katı"), Loc.N("Soğuk depo") },
            [Grid] = new[] { Loc.N("Fiber santrali"), Loc.N("Trafo merkezi"), Loc.N("Yedek güç odası"), Loc.N("Röle istasyonu"), Loc.N("Telefon santrali") },
            [Hostage] = new[] { Loc.N("Uydu bağlantı odası"), Loc.N("Laboratuvar katı"), Loc.N("Pompa dairesi"), Loc.N("Kazan dairesi"), Loc.N("Soğutma kulesi") },
        };

        /// <summary>Türün girişten hedefe adım aralığı (bot ölçümüyle ayarlı: türler arası baskın başarısı yakın kalsın).</summary>
        public static (int Min, int Max) PathRange(string kind) => kind switch
        {
            Vault => (6, 9),
            Lanes => (8, 11),
            Hall => (8, 10),
            Hostage => (7, 10),
            _ => (7, 10),
        };

        /// <summary>Haritanın türü (adından); havuz dışı ad için null.</summary>
        public static string KindOf(string mapName) => Names.FirstOrDefault(kv => kv.Value.Contains(mapName)).Key;

        static uint cachedSeed;
        static List<(string Name, string[] Rows)> cached;

        /// <summary>Harekâtın havuzu: <see cref="PoolSize"/> farklı harita, türler sırayla ve her biri ayrı adla. Aynı tohum aynı havuzu verir.</summary>
        public static List<(string Name, string[] Rows)> Pool(uint campaignSeed)
        {
            if (cached != null && cachedSeed == campaignSeed) return cached;
            var pool = new List<(string Name, string[] Rows)>();
            int per = PoolSize / Kinds.Length;
            for (int k = 0; k < Kinds.Length; k++)
            {
                var names = Names[Kinds[k]].ToList();
                var order = new Rng(Mix(campaignSeed, 500u + (uint)k));
                for (int i = names.Count - 1; i > 0; i--) { int j = (int)Math.Floor(order.Next() * (i + 1)); (names[i], names[j]) = (names[j], names[i]); }
                int made = 0;
                for (int i = 1; made < per && i <= per * 40; i++)
                {
                    var rows = Generate(Kinds[k], new Rng(Mix(campaignSeed, (uint)(k * 1000 + i))));
                    if (rows == null || pool.Any(m => m.Rows.SequenceEqual(rows))) continue;
                    pool.Add((names[made++], rows));
                }
            }
            // Üretilemezse (olmaması gerekir) elle çizilmiş haritalarla tamamlanır
            for (int i = 0; pool.Count < PoolSize; i++) pool.Add(Baskin.Layouts[i % Baskin.Layouts.Length]);
            cachedSeed = campaignSeed; cached = pool;
            return pool;
        }

        /// <summary>
        /// Harekâtın <paramref name="raid"/>. baskınının (1'den) havuzdaki haritası: havuz karıştırılmış torbalardan sırayla
        /// çekilir. Her harita bir kez gelmeden hiçbiri tekrar etmez; torba değişirken aynı harita art arda gelmez.
        /// </summary>
        public static int Slot(uint campaignSeed, int raid)
        {
            int k = Math.Max(0, raid - 1), bag = k / PoolSize;
            var order = Bag(campaignSeed, bag);
            if (bag > 0 && order[0] == Bag(campaignSeed, bag - 1)[PoolSize - 1]) (order[0], order[1]) = (order[1], order[0]);
            return order[k % PoolSize];
        }

        static int[] Bag(uint campaignSeed, int bag)
        {
            var order = Enumerable.Range(0, PoolSize).ToArray();
            var r = new Rng(Mix(campaignSeed, 1000u + (uint)bag));
            for (int i = order.Length - 1; i > 0; i--) { int j = (int)Math.Floor(r.Next() * (i + 1)); (order[i], order[j]) = (order[j], order[i]); }
            return order;
        }

        /// <summary>Verilen türden tek bir harita; türün ve genel kuralların uyduğu bir yerleşim bulunamazsa null.</summary>
        public static string[] Generate(string kind, Rng r)
        {
            for (int attempt = 0; attempt < 80; attempt++)
            {
                var g = new char[N, N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) g[y, x] = '.';
                int ey = Int(r, 0, N - 3);
                for (int i = 0; i < 3; i++) g[ey + i, 0] = 'E';
                // Girişin önü boş kalır: ilk iki sütunda girişin bir kare çevresine bir şey konmaz
                bool NearEntry(int x, int y) => x <= 1 && y >= ey - 1 && y <= ey + 3;
                // Kasa odasının içi: dolaplar ve dronlar dışarıda kalır, bakım robotu içeride
                var room = new HashSet<(int x, int y)>();
                int cx, cy;
                bool built = kind switch
                {
                    Vault => BuildVault(g, r, room, out cx, out cy),
                    Lanes => BuildLanes(g, r, out cx, out cy),
                    Hall => BuildHall(g, r, NearEntry, out cx, out cy),
                    Grid => BuildGrid(g, r, NearEntry, out cx, out cy),
                    Hostage => BuildCorridor(g, r, NearEntry, 5, 7, out cx, out cy),
                    _ => BuildCorridor(g, r, NearEntry, 7, 9, out cx, out cy),
                };
                if (!built) continue;
                bool Out(int x, int y) => !room.Contains((x, y));

                // Dolaplar: soğutma ortada, kopya dolapları uzak yarının üst ve alt kenarında, siviller çoğunlukla kenarlarda
                Put(g, r, 'K', 2, (x, y) => x >= 2 && x <= 5 && Out(x, y));
                // Kenar dolu (kasa odası kenara dayalı) ise bir iç satır
                foreach (var (edge, inner) in new[] { (0, 1), (N - 1, N - 2) })
                {
                    bool free = Enumerable.Range(4, N - 4).Any(x => g[edge, x] == '.' && Out(x, edge));
                    int row = free ? edge : inner;
                    Put(g, r, 'U', 1, (x, y) => x >= 4 && y == row && Out(x, y));
                }
                if (kind == Hostage)
                {
                    // Rehine: üç sivil yolun ortasında (ekip ile makineler arasında), biri ya da ikisi kenarda
                    Put(g, r, 'S', 3, (x, y) => x >= 2 && x <= 5 && y >= 1 && y <= N - 2 && !NearEntry(x, y));
                    Put(g, r, 'S', Int(r, 1, 2), (x, y) => (x == N - 1 || y == 0 || y == N - 1) && !NearEntry(x, y) && Out(x, y));
                }
                else
                {
                    int civ = Int(r, 4, 5);
                    Put(g, r, 'S', civ - 1, (x, y) => (x == 0 || x == N - 1 || y == 0 || y == N - 1) && !NearEntry(x, y) && Out(x, y));
                    Put(g, r, 'S', 1, (x, y) => x >= 1 && x <= N - 2 && !NearEntry(x, y) && Out(x, y));
                }
                // Makineler uzak yarıda; bakım robotu hedef dolabın yakınında (kasa odasında içeride)
                if (room.Count > 0) Put(g, r, 'm', 1, (x, y) => room.Contains((x, y)) && Math.Abs(x - cx) + Math.Abs(y - cy) <= 2);
                else Put(g, r, 'm', 1, (x, y) => x >= 4 && Math.Abs(x - cx) + Math.Abs(y - cy) <= 2);
                Put(g, r, 'd', 2, (x, y) => x >= 4 && Out(x, y));

                var rows = Enumerable.Range(0, N).Select(y => new string(Enumerable.Range(0, N).Select(x => g[y, x]).ToArray())).ToArray();
                if (Valid(rows, kind)) return rows;
            }
            return null;
        }

        /// <summary>Koridor: hedef dolap 6. sütunda, yanında korunak; dağınık 2–3 karelik duvarlar ve bir kablo kanalı.</summary>
        static bool BuildCorridor(char[,] g, Rng r, Func<int, int, bool> nearEntry, int wallsMin, int wallsMax, out int cx, out int cy)
        {
            cx = 6; cy = Int(r, 1, N - 2);
            g[cy, cx] = 'C';
            if (!Guard(g, r, cx, cy)) return false;
            Walls(g, r, Int(r, wallsMin, wallsMax) - 2, 2, 6);
            Cables(g, r, Int(r, 2, 4), 2, 6);
            if (r.Next() < 0.35) Put(g, r, '~', 1, (x, y) => x >= 1 && !nearEntry(x, y));
            return true;
        }

        /// <summary>
        /// Kasa odası: sağ kenara dayalı 2–3 × 2–3'lük oda, duvarlarında iki kapı; hedef dolap içeride, kapıya bitişik değil.
        /// Dışarıda kısa bir korunak duvarı ve bir kablo kanalı.
        /// </summary>
        static bool BuildVault(char[,] g, Rng r, HashSet<(int x, int y)> room, out int cx, out int cy)
        {
            cx = cy = -1;
            int w = Int(r, 2, 3), h = Int(r, 2, 3);
            int ix1 = N - 1, ix0 = ix1 - w + 1, iy0 = Int(r, 0, N - h), iy1 = iy0 + h - 1;
            var doors = new List<(int x, int y)>();
            for (int y = iy0 - 1; y <= iy1 + 1; y++)
                for (int x = ix0 - 1; x <= ix1; x++)
                {
                    if (!In(x, y)) continue;
                    bool across = y >= iy0 && y <= iy1;
                    if (x >= ix0 && across) { room.Add((x, y)); continue; }
                    g[y, x] = '#';
                    // Köşe olmayan duvar, dışı tahtada: kapı olabilir (sol yan ya da üst/alt yan)
                    if (x == ix0 - 1 ? across : In(x, y < iy0 ? y - 1 : y + 1)) doors.Add((x, y));
                }
            if (doors.Count == 0) return false;
            int open = Math.Min(doors.Count, 2);
            var opened = new List<(int x, int y)>();
            for (int i = 0; i < open; i++)
            {
                int k = (int)Math.Floor(r.Next() * doors.Count);
                opened.Add(doors[k]); g[doors[k].y, doors[k].x] = '.'; doors.RemoveAt(k);
            }
            // Hedef: kapıya bitişik olmayan, bir duvara dayalı iç kare
            var spots = room.Where(t => opened.All(d => Math.Abs(d.x - t.x) + Math.Abs(d.y - t.y) > 1)
                && Baskin.Dirs.Any(d => In(t.x + d[0], t.y + d[1]) && g[t.y + d[1], t.x + d[0]] == '#')).ToList();
            if (spots.Count == 0) return false;
            (cx, cy) = spots[(int)Math.Floor(r.Next() * spots.Count)];
            g[cy, cx] = 'C';
            // Dışarıda: oda duvarları zaten çok; bir korunak parçası ve bir kablo kanalı yeter
            Walls(g, r, 2, 2, Math.Max(2, ix0 - 3));
            Cables(g, r, Int(r, 2, 3), 2, Math.Max(2, ix0 - 2));
            return true;
        }

        /// <summary>
        /// İki hat: 2.–5. satırlardan birinde 2. sütundan başlayan uzun bir bölme duvarı (yarısında bir geçit); hedef dolap
        /// bölmeye dayalı, iki şeritten birinde. Şeritlere birer kısa duvar ve bir kablo kanalı.
        /// </summary>
        static bool BuildLanes(char[,] g, Rng r, out int cx, out int cy)
        {
            int wy = Int(r, 2, N - 3), x1 = Int(r, 5, 7);
            for (int x = 2; x <= x1; x++) g[wy, x] = '#';
            int gap = r.Next() < 0.5 ? Int(r, 3, x1 - 1) : -1;
            if (gap > 0) g[wy, gap] = '.';
            cx = Int(r, 5, x1); cy = r.Next() < 0.5 ? wy - 1 : wy + 1;
            if (cx == gap || g[cy, cx] != '.') return false;
            g[cy, cx] = 'C';
            Walls(g, r, Int(r, 2, 3), 2, 6, verticalChance: 1);
            Cables(g, r, Int(r, 2, 3), 2, 6);
            return true;
        }

        /// <summary>Açık salon: hedef dolap 5.–6. sütunda korunağıyla; 5–7 tek karelik sütun (birbirine değmez); iki üç tek kablo.</summary>
        static bool BuildHall(char[,] g, Rng r, Func<int, int, bool> nearEntry, out int cx, out int cy)
        {
            cx = Int(r, 5, 6); cy = Int(r, 1, N - 2);
            g[cy, cx] = 'C';
            if (!Guard(g, r, cx, cy)) return false;
            int pillars = Int(r, 5, 7), tx = cx, ty = cy;
            Put(g, r, '#', pillars, (x, y) => x >= 2 && x <= 6 && !nearEntry(x, y) && Math.Abs(x - tx) + Math.Abs(y - ty) > 1 && Clear(g, x, y));
            Put(g, r, '~', Int(r, 2, 3), (x, y) => x >= 2 && x <= 6 && !nearEntry(x, y));
            return true;
        }

        /// <summary>Şebeke: hedef dolap 6. sütunda korunağıyla; 3.–4. sütunda boydan boya kablo kuşağı (birkaç boşlukla), 3–5 duvar.</summary>
        static bool BuildGrid(char[,] g, Rng r, Func<int, int, bool> nearEntry, out int cx, out int cy)
        {
            cx = 6; cy = Int(r, 1, N - 2);
            g[cy, cx] = 'C';
            if (!Guard(g, r, cx, cy)) return false;
            int bx = Int(r, 3, 4);
            for (int y = 0; y < N; y++) if (g[y, bx] == '.' && r.Next() < 0.7) g[y, bx] = '~';
            Cables(g, r, 2, 5, 6);
            int band = bx;
            Walls(g, r, Int(r, 3, 5), 2, 6, avoidColumn: band);
            return true;
        }

        /// <summary>Hedef dolabın yanında 2 karelik korunak duvarı (girişe bakan solda ya da üstünde/altında).</summary>
        static bool Guard(char[,] g, Rng r, int cx, int cy)
        {
            var guards = new List<(int x, int y)[]>
            {
                new[] { (cx - 1, cy - 1), (cx - 1, cy) }, new[] { (cx - 1, cy), (cx - 1, cy + 1) },
                new[] { (cx - 2, cy), (cx - 1, cy) },
                new[] { (cx - 1, cy - 1), (cx, cy - 1) }, new[] { (cx, cy - 1), (cx + 1, cy - 1) },
                new[] { (cx - 1, cy + 1), (cx, cy + 1) }, new[] { (cx, cy + 1), (cx + 1, cy + 1) },
            };
            var guard = guards[(int)Math.Floor(r.Next() * guards.Count)];
            if (guard.Any(c => !In(c.x, c.y) || g[c.y, c.x] != '.')) return false;
            foreach (var c in guard) g[c.y, c.x] = '#';
            return true;
        }

        /// <summary>2–3 karelik dikey (çoğunlukla) ya da yatay duvar parçaları, toplam <paramref name="count"/> kare.</summary>
        static void Walls(char[,] g, Rng r, int count, int xMin, int xMax, double verticalChance = 0.65, int avoidColumn = -1)
        {
            for (int placed = 0, guard = 0; placed < count && guard < 80; guard++)
            {
                bool vertical = r.Next() < verticalChance;
                int len = Math.Min(r.Next() < 0.7 ? 2 : 3, count - placed);
                int x = Int(r, xMin, xMax), y = Int(r, 0, N - 1);
                var cells = Enumerable.Range(0, len).Select(i => vertical ? (x, y + i) : (x + i, y)).ToList();
                if (cells.Any(c => !In(c.Item1, c.Item2) || g[c.Item2, c.Item1] != '.' || c.Item1 == avoidColumn)) continue;
                foreach (var c in cells) g[c.Item2, c.Item1] = '#';
                placed += len;
            }
        }

        /// <summary>Elektrikli kablo kanalı: <paramref name="len"/> karelik tek parça.</summary>
        static void Cables(char[,] g, Rng r, int len, int xMin, int xMax)
        {
            bool vertical = r.Next() < 0.5;
            for (int guard = 0; guard < 30; guard++)
            {
                int x = Int(r, xMin, xMax), y = Int(r, 0, N - 1);
                var cells = Enumerable.Range(0, len).Select(i => vertical ? (x, y + i) : (x + i, y)).ToList();
                if (cells.Any(c => !In(c.Item1, c.Item2) || g[c.Item2, c.Item1] != '.')) continue;
                foreach (var c in cells) g[c.Item2, c.Item1] = '~';
                return;
            }
        }

        /// <summary>Çevresindeki sekiz karede duvar yok (açık salonun sütunları birbirine değmez).</summary>
        static bool Clear(char[,] g, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                if (In(x + dx, y + dy) && g[y + dy, x + dx] == '#') return false;
            return true;
        }

        /// <summary>
        /// Bütün türlerin kuralları: sol kenarda üç bitişik giriş, tek hedef dolap; girişlerden hedefin en az iki komşusuna ve
        /// bütün makinelere varılır, hedefe 6–11 adım; en çok iki kapalı köşe kare; her kopya dolabının boş bir komşusu var; hiçbir
        /// dronun girişe açık atış hattı yok; hedef dolabın yanında duvar var; sayılar aralıkta.
        /// </summary>
        public static bool Valid(string[] rows) => Path(rows) is int p && p >= 6 && p <= 11;

        /// <summary>Genel kurallar ve türün kendi kuralları (adım aralığı ve türün imzası).</summary>
        public static bool Valid(string[] rows, string kind)
        {
            if (!(Path(rows) is int p)) return false;
            var (min, max) = PathRange(kind);
            if (p < min || p > max) return false;
            int Count(char c) => rows.Sum(row => row.Count(ch => ch == c));
            switch (kind)
            {
                case Corridor: return Count('#') >= 7 && Count('#') <= 9 && Count('~') <= 5;
                // Hedef dolap bir odada: en az iki yanı duvar ya da tahta kenarı, çok duvar
                case Vault: return Closed(rows) >= 2 && Count('#') >= 8;
                // Bir satırda en az dört bitişik duvar (bölme)
                case Lanes: return rows.Any(row => row.Contains("####"));
                // Korunak dışında duvarlar tek kare (sütun), en az dördü
                case Hall: return Pillars(rows) >= 4;
                case Grid: return Count('~') >= 5;
                case Hostage: return Enumerable.Range(1, N - 2).Sum(y => Enumerable.Range(2, 4).Count(x => rows[y][x] == 'S')) >= 3;
                default: return true;
            }
        }

        /// <summary>Genel kurallara uyan yerleşimde girişten hedefin bitişiğine en kısa yol; uymuyorsa null.</summary>
        static int? Path(string[] rows)
        {
            if (rows == null || rows.Length != N || rows.Any(row => row.Length != N)) return null;
            char At(int x, int y) => rows[y][x];
            bool Walk(int x, int y) => In(x, y) && ".~Edm".IndexOf(At(x, y)) >= 0;
            int Count(char c) => rows.Sum(row => row.Count(ch => ch == c));
            var entries = Enumerable.Range(0, N).Where(y => At(0, y) == 'E').ToList();
            if (entries.Count != 3 || entries[2] - entries[0] != 2 || Count('E') != 3 || Count('C') != 1) return null;
            if (Count('#') < 5 || Count('#') > 13 || Count('~') < 2 || Count('~') > 8 || Count('S') < 4 || Count('S') > 5) return null;
            if (Count('K') != 2 || Count('U') != 2 || Count('d') != 2 || Count('m') != 1) return null;

            var dist = new int[N, N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) dist[y, x] = -1;
            var q = new Queue<(int x, int y)>();
            foreach (var ey in entries) { dist[ey, 0] = 0; q.Enqueue((0, ey)); }
            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                foreach (var d in Baskin.Dirs)
                {
                    int nx = x + d[0], ny = y + d[1];
                    if (!Walk(nx, ny) || dist[ny, nx] >= 0) continue;
                    dist[ny, nx] = dist[y, x] + 1; q.Enqueue((nx, ny));
                }
            }
            int pockets = 0;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                if (!Walk(x, y) || dist[y, x] >= 0) continue;
                if (At(x, y) == 'd' || At(x, y) == 'm') return null;
                pockets++;
            }
            if (pockets > 2) return null;
            int cx = -1, cy = -1;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (At(x, y) == 'C') { cx = x; cy = y; }
            var near = Baskin.Dirs.Select(d => (x: cx + d[0], y: cy + d[1])).Where(p => In(p.x, p.y) && dist[p.y, p.x] >= 0).ToList();
            if (near.Count < 2) return null;
            if (!Baskin.Dirs.Any(d => In(cx + d[0], cy + d[1]) && At(cx + d[0], cy + d[1]) == '#')) return null;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                if (At(x, y) == 'U' && !Baskin.Dirs.Any(d => In(x + d[0], y + d[1]) && dist[y + d[1], x + d[0]] >= 0)) return null;
            // Elle çizilmiş haritalardaki gibi hiçbir dronun girişe açık atış hattı yok (ilk turda girişteki uzmana ateş edemez)
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                if (At(x, y) != 'd') continue;
                int sx = x - 1;
                while (sx > 0 && (At(sx, y) == '.' || At(sx, y) == '~')) sx--;
                if (sx == 0 && At(0, y) == 'E') return null;
            }
            return near.Min(p => dist[p.y, p.x]);
        }

        /// <summary>Hedef dolabın dört yanından kaçı duvar ya da tahta kenarı.</summary>
        static int Closed(string[] rows)
        {
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                if (rows[y][x] == 'C') return Baskin.Dirs.Count(d => !In(x + d[0], y + d[1]) || rows[y + d[1]][x + d[0]] == '#');
            return 0;
        }

        /// <summary>Hiçbir duvara dört yandan değmeyen tek karelik duvarların sayısı.</summary>
        static int Pillars(string[] rows)
        {
            int n = 0;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                if (rows[y][x] == '#' && !Baskin.Dirs.Any(d => In(x + d[0], y + d[1]) && rows[y + d[1]][x + d[0]] == '#')) n++;
            return n;
        }

        static void Put(char[,] g, Rng r, char ch, int count, Func<int, int, bool> where)
        {
            var spots = new List<(int x, int y)>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (g[y, x] == '.' && where(x, y)) spots.Add((x, y));
            for (int i = 0; i < count && spots.Count > 0; i++)
            {
                int k = (int)Math.Floor(r.Next() * spots.Count);
                g[spots[k].y, spots[k].x] = ch;
                spots.RemoveAt(k);
            }
        }

        static bool In(int x, int y) => x >= 0 && y >= 0 && x < N && y < N;
        static int Int(Rng r, int min, int max) => min + (int)Math.Floor(r.Next() * (max - min + 1));

        /// <summary>Harekât tohumu ve sıra numarasından birbirinden bağımsız alt tohumlar (splitmix tarzı karıştırma).</summary>
        static uint Mix(uint seed, uint index)
        {
            unchecked
            {
                uint h = seed * 0x9E3779B1u ^ (index + 0x7F4A7C15u) * 0x85EBCA77u;
                h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16;
                return h;
            }
        }
    }
}
