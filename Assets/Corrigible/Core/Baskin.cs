using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Baskın: hücre tabanlı taktik savaş (Into the Breach esinli).
    /// Durum (BState) kopyalanabilir: geri alma ve önizleme kopya üzerinde çalışır. Rastgelelik yalnızca durumdaki
    /// mulberry32 (Rs) ile; aynı parametreler + aynı hamleler = aynı savaş.
    ///
    /// Tur yapısı: oyuncu turu (düşman niyetleri önceden görünür) → düşman evresi: niyetler sırayla uygulanır,
    /// işaretli karelerden yeni robotlar uyanır, düşmanlar yer değiştirip yeni niyet seçer, yeni uyanma kareleri işaretlenir.
    /// </summary>
    public static class Baskin
    {
        public const int N = 8;
        const int MaxEnemies = 7;
        public static readonly int[][] Dirs = { new[] { 0, -1 }, new[] { 1, 0 }, new[] { 0, 1 }, new[] { -1, 0 } };

        // '.' zemin, '#' duvar, '~' elektrikli kablo kanalı, 'C' hedef dolap, 'Y' ve 'S' sivil sunucu,
        // 'K' soğutma ünitesi, 'U' kopya dolabı, 'E' ekip girişi, 'd' dron, 'm' bakım robotu
        public static readonly (string Name, string[] Rows)[] Layouts =
        {
            (Loc.N("Ana sunucu salonu"), new[] { "S.~...U.", "..~.#..S", "E.#.K#..", "E.#..#C.", "E..~~...", "..#..#dm", ".S#.d.Y.", "....K..U" }),
            (Loc.N("Soğutma koridoru"), new[] { "E..S#..U", "E...#.d.", "E.K.~..S", "..#.~##.", "..#.~.C.", ".S..#.mK", "....#..d", "S.~~.Y.U" }),
            (Loc.N("Yedekli veri kasası"), new[] { ".S..U..S", "..#.d...", "E.#K..#.", "E.~.##C.", "E.#....m", "..#.Y#.d", "..S.K.#.", ".~..U..S" }),
        };

        public sealed class UnitType
        {
            public string Id, Short, Ability; public char Side; public int Hp, Move;
            /// <summary>Makine: başlangıç zırhı, niyet türleri (shoot, melee, repair, lob, blast, lock, shield, clone) ve uzaktan mı vurur.</summary>
            public int Armor; public string[] Kinds = Array.Empty<string>(); public bool Ranged;
            string name, desc;
            /// <summary>Gösterim adı (geçerli dilde).</summary>
            public string Name { get => Loc.T(name); set => name = value; }
            /// <summary>Makinenin ne yaptığı (ipucu kartı); oyuncu birimlerinde boş.</summary>
            public string Desc { get => string.IsNullOrEmpty(desc) ? "" : Loc.T(desc); set => desc = value; }
        }

        public static readonly Dictionary<string, UnitType> UnitTypes = new Dictionary<string, UnitType>
        {
            ["tek"] = new UnitType { Id = "tek", Side = 'p', Name = Loc.N("Teknisyen"), Short = "T", Hp = 3, Move = 3, Ability = "sabotaj" },
            ["emp"] = new UnitType { Id = "emp", Side = 'p', Name = Loc.N("EMP uzmanı"), Short = "E", Hp = 3, Move = 3, Ability = "emp" },
            ["ana"] = new UnitType { Id = "ana", Side = 'p', Name = Loc.N("Analist"), Short = "A", Hp = 2, Move = 4, Ability = "kanca" },
            ["dron"] = new UnitType { Id = "dron", Side = 'e', Name = Loc.N("Güvenlik dronu"), Short = "D", Hp = 2, Move = 3, Kinds = new[] { "shoot" }, Ranged = true,
                Desc = Loc.N("Düz hatta ilk engele ateş eder: 1 hasar.") },
            ["bakim"] = new UnitType { Id = "bakim", Side = 'e', Name = Loc.N("Bakım robotu"), Short = "B", Hp = 2, Move = 2, Kinds = new[] { "melee", "repair" },
                Desc = Loc.N("Bitişik kareye vurur (2 hasar) ya da hasarlı hedef dolabı onarır.") },
            // Bölgelerin imza düşmanları (her biri iki bölgede)
            ["zdron"] = new UnitType { Id = "zdron", Side = 'e', Name = Loc.N("Zırhlı dron"), Short = "Z", Hp = 2, Move = 3, Armor = 1, Kinds = new[] { "shoot" }, Ranged = true,
                Desc = Loc.N("Dron gibi düz hatta ateş eder; zırhı ilk hasarı karşılar.") },
            ["havan"] = new UnitType { Id = "havan", Side = 'e', Name = Loc.N("Havan robotu"), Short = "H", Hp = 2, Move = 1, Kinds = new[] { "lob" }, Ranged = true,
                Desc = Loc.N("Üç kare öteye, engellerin üstünden atar: 1 hasar.") },
            ["bomba"] = new UnitType { Id = "bomba", Side = 'e', Name = Loc.N("Patlayan robot"), Short = "P", Hp = 1, Move = 3, Kinds = new[] { "blast" },
                Desc = Loc.N("Yanına sokulur ve patlar: çevresindeki dört kareye 1 hasar.") },
            ["kilit"] = new UnitType { Id = "kilit", Side = 'e', Name = Loc.N("Kilitleyici"), Short = "K", Hp = 2, Move = 2, Kinds = new[] { "lock" }, Ranged = true,
                Desc = Loc.N("Düz hatta dört kareye kadar kilit atar: vurulan uzman bir tur yürüyemez.") },
            ["kalkan"] = new UnitType { Id = "kalkan", Side = 'e', Name = Loc.N("Kalkan dronu"), Short = "Q", Hp = 2, Move = 3, Kinds = new[] { "shield", "shoot" }, Ranged = true,
                Desc = Loc.N("Yanındaki makineye zırh verir; verecek kimse yoksa ateş eder.") },
            ["cogalt"] = new UnitType { Id = "cogalt", Side = 'e', Name = Loc.N("Çoğaltıcı"), Short = "Ç", Hp = 2, Move = 2, Kinds = new[] { "clone" },
                Desc = Loc.N("Yanındaki boş kareye yeni bir dron uyandırır.") },
        };

        /// <summary>
        /// Bölge özellikleri (<see cref="RegionDef.Trait"/>): taktik tahtayı değiştirir. Baskın parametresinin Traits listesinde
        /// kimlikle gelir; "kopya" ve "guclu" bölgeye değil zorluk kademesine bağlıdır.
        /// </summary>
        public static readonly Dictionary<string, (string Name, string Desc)> RegionTraits = new Dictionary<string, (string, string)>
        {
            ["guvenli"] = (Loc.N("Güvenli giriş"), Loc.N("İlk tur hiçbir robot uyanmaz.")),
            ["dar"] = (Loc.N("Dar koridor"), Loc.N("Fazladan duvarlar: atış hatları kısa, yollar uzun.")),
            ["kisa"] = (Loc.N("Kısa nöbet"), Loc.N("Baskın 6 değil 5 tur sürer.")),
            ["takviye"] = (Loc.N("Takviye"), Loc.N("Başta bir robot fazla.")),
            ["acik"] = (Loc.N("Açık arazi"), Loc.N("Duvarların bir kısmı yıkık: atış hatları uzun.")),
            ["sis"] = (Loc.N("Kum fırtınası"), Loc.N("Uyanacak robotların yeri görünmez.")),
            ["sogutma"] = (Loc.N("Soğutma ağı"), Loc.N("İki soğutma ünitesi fazla; kırılan ünite çevresine patlar.")),
            ["kalabalik"] = (Loc.N("Kalabalık şehir"), Loc.N("İki sivil sunucu fazla; GÜVEN kolay düşer.")),
            ["zirhli"] = (Loc.N("Zırhlı dolap"), Loc.N("Hedef dolapların bir canı fazla.")),
            ["sebeke"] = (Loc.N("Elektrik şebekesi"), Loc.N("Fazladan elektrikli kanal: üstüne düşen 1 hasar alır.")),
            ["fabrika"] = (Loc.N("Kopya fabrikası"), Loc.N("Bir kopya dolabı fazla.")),
            ["devriye"] = (Loc.N("Hızlı devriye"), Loc.N("Robotlar bir kare fazla yürür.")),
        };

        public static readonly Dictionary<string, (string Name, string Desc)> Abilities = new Dictionary<string, (string, string)>
        {
            ["sabotaj"] = (Loc.N("Sabotaj"), Loc.N("Bitişik kare. Dolaba 2 hasar; robota 1 hasar ve 1 kare itme.")),
            ["emp"] = (Loc.N("EMP atışı"), Loc.N("Düz hatta 2–5 kare uzağa, engellerin üstünden. Hedefteki robota ya da sıradan dolaba 1 hasar (zırhlı hedef dolaplara işlemez); çevresindeki 4 karedeki robotları dışarı iter.")),
            ["kanca"] = (Loc.N("Kanca"), Loc.N("Düz hatta 5 kare. Çarptığı robotu 1 kare kendine çeker; duvara ya da dolaba çarparsa Analist onun yanına atılır. Analistin 3 kare çevresindeki robotların niyeti kesin görünür.")),
        };

        public static readonly Dictionary<string, string> ObjNames = new Dictionary<string, string>
        {
            ["core"] = Loc.N("Çekirdek dolabı"), ["node"] = Loc.N("YZ düğüm dolabı"),
            ["civ"] = Loc.N("Sivil sunucu"), ["cool"] = Loc.N("Soğutma ünitesi"), ["spawner"] = Loc.N("Kopya dolabı"),
        };

        public static bool IsTarget(string k) => k == "core" || k == "node";
        public static bool IsLoot(BState state) => state.Params?.MissionKind == "loot";

        // ---------- Durum ----------
        public sealed class Obj { public string K; public int Hp, Max; public Obj Clone() => (Obj)MemberwiseClone(); }

        public sealed class Cell
        {
            public string T; // floor, wall, cable
            public Obj O;
            public Cell Clone() => new Cell { T = T, O = O?.Clone() };
        }

        public sealed class Intent { public string Kind; public int Dir; public Intent Clone() => (Intent)MemberwiseClone(); }

        public sealed class Unit
        {
            /// <summary>Armor: zırh plakası, ilk hasarı karşılar ve kırılır (yalnız ekip).</summary>
            public int Id, X, Y, Hp, Max, Order, Armor;
            public string Type; public char Side;
            /// <summary>Locked: kilitleyici vurdu, bu oyuncu turu yürüyemez (yeteneğini kullanabilir).</summary>
            public bool Moved, Acted, Dead, Locked;
            public Intent Intent;
            public Unit Clone() { var u = (Unit)MemberwiseClone(); u.Intent = Intent?.Clone(); return u; }
        }

        public struct Pt { public int X, Y; public Pt(int x, int y) { X = x; Y = y; } }

        /// <summary>Oynanış efekti: arayüz önizleme ve canlandırma için okur.</summary>
        public sealed class Fx
        {
            public string K; public int X, Y, N, Unit, Dx, Dy;
            public string Obj;
        }

        public sealed class LogLine { public int Turn; public string Text, Kind; }

        public sealed class BState
        {
            public RaidParams Params;
            public int Rs, Turn = 1, MaxTurns, SpawnPerTurn, Collateral, NextId = 1;
            public string Phase = "player", Layout, OutcomeReason;
            public bool IsCore, OutcomeSuccess, Retreated;
            /// <summary>Bölge ve kademe: imza düşmanı, makinelerin ek canı ve yürüyüşü, uyanma yerlerinin gizli olması (kum fırtınası).</summary>
            public string Signature;
            public int EnemyHp, EnemyMove;
            public bool Fog;
            public Cell[,] Cells = new Cell[N, N]; // [y, x]
            public List<Unit> Units = new List<Unit>();
            public List<Pt> Spawns = new List<Pt>();
            public List<LogLine> Log = new List<LogLine>();
            public List<Fx> FxList = new List<Fx>();
            public List<BState> Hist = new List<BState>();

            /// <summary>Derin kopya (geçmiş hariç).</summary>
            public BState Clone()
            {
                var c = (BState)MemberwiseClone();
                c.Cells = new Cell[N, N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) c.Cells[y, x] = Cells[y, x].Clone();
                c.Units = Units.Select(u => u.Clone()).ToList();
                c.Spawns = new List<Pt>(Spawns);
                c.Log = new List<LogLine>(Log);
                c.FxList = new List<Fx>(FxList);
                c.Hist = new List<BState>();
                return c;
            }
        }

        // ---------- Yardımcılar ----------
        static double Rnd(BState B)
        {
            unchecked
            {
                B.Rs = B.Rs + 0x6D2B79F5;
                int t = (B.Rs ^ (int)((uint)B.Rs >> 15)) * (1 | B.Rs);
                t = (t + (t ^ (int)((uint)t >> 7)) * (61 | t)) ^ t;
                return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
            }
        }

        static bool Inb(int x, int y) => x >= 0 && y >= 0 && x < N && y < N;
        public static Cell CellAt(BState B, int x, int y) => B.Cells[y, x];
        public static Unit UnitAt(BState B, int x, int y) => B.Units.FirstOrDefault(u => !u.Dead && u.X == x && u.Y == y);
        static int Man(int ax, int ay, int bx, int by) => Math.Abs(ax - bx) + Math.Abs(ay - by);
        public static List<Unit> Alive(BState B, char side) => B.Units.Where(u => !u.Dead && u.Side == side).ToList();
        static void AddFx(BState B, Fx f) => B.FxList.Add(f);
        static void Say(BState B, string text, string kind = "sys") => B.Log.Add(new LogLine { Turn = B.Turn, Text = text, Kind = kind });
        static Obj NewObj(string k, int hp) => new Obj { K = k, Hp = hp, Max = hp };
        public static int Sign(int v) => v > 0 ? 1 : v < 0 ? -1 : 0;

        static Unit NewUnit(BState B, string type, int x, int y)
        {
            var T = UnitTypes[type];
            int hp = T.Hp + (T.Side == 'e' ? B.EnemyHp : 0);
            return new Unit { Id = B.NextId++, Type = type, Side = T.Side, X = x, Y = y, Hp = hp, Max = hp, Armor = T.Armor };
        }

        /// <summary>Uyanan ya da başta eklenen makine: bölgenin imza düşmanı varsa üçte biri o (tek zar, sıra korunur).</summary>
        static string WakeType(BState B, double r) =>
            B.Signature == null ? (r < 0.6 ? "dron" : "bakim") : r < 0.4 ? "dron" : r < 0.7 ? "bakim" : B.Signature;

        /// <summary>Ekip girişinden bütün hedef dolaplara yol var mı (duvar ve nesne eklerken tahta kapanmasın).</summary>
        static bool Connected(BState B, List<Pt> entries)
        {
            var seen = new bool[N, N];
            var q = new Queue<Pt>();
            foreach (var e in entries) { seen[e.Y, e.X] = true; q.Enqueue(e); }
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var d in Dirs)
                {
                    int x = p.X + d[0], y = p.Y + d[1];
                    if (!Inb(x, y) || seen[y, x] || CellAt(B, x, y).T == "wall" || CellAt(B, x, y).O != null) continue;
                    seen[y, x] = true; q.Enqueue(new Pt(x, y));
                }
            }
            return TargetsLeft(B).All(t => Dirs.Any(d => Inb(t.X + d[0], t.Y + d[1]) && seen[t.Y + d[1], t.X + d[0]]));
        }

        /// <summary>Bölge özelliği tahtaya ekler: girişin yanı boş kalır, yol kapanmaz; olmazsa o parça eklenmez.</summary>
        static void Scatter(BState B, List<Pt> entries, int count, Action<Cell> place, Func<Pt, bool> where = null)
        {
            var spots = FreeTiles(B).Where(t => CellAt(B, t.X, t.Y).T == "floor" && !entries.Any(e => Man(e.X, e.Y, t.X, t.Y) <= 1) && (where == null || where(t))).ToList();
            for (int placed = 0; placed < count && spots.Count > 0;)
            {
                int k = (int)Math.Floor(Rnd(B) * spots.Count);
                var t = spots[k]; spots.RemoveAt(k);
                var cell = CellAt(B, t.X, t.Y);
                var before = cell.Clone();
                place(cell);
                if (Connected(B, entries)) placed++;
                else B.Cells[t.Y, t.X] = before;
            }
        }

        static void ApplyRegionTraits(BState B, RaidParams p, List<Pt> entries)
        {
            var t = p.Traits;
            if (t.Contains("dar")) Scatter(B, entries, 3, c => c.T = "wall");
            if (t.Contains("acik"))
            {
                var walls = new List<Pt>();
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (CellAt(B, x, y).T == "wall") walls.Add(new Pt(x, y));
                for (int i = 0; i < 3 && walls.Count > 0; i++)
                {
                    int k = (int)Math.Floor(Rnd(B) * walls.Count);
                    CellAt(B, walls[k].X, walls[k].Y).T = "floor"; walls.RemoveAt(k);
                }
            }
            if (t.Contains("sebeke")) Scatter(B, entries, 3, c => c.T = "cable");
            if (t.Contains("sogutma")) Scatter(B, entries, 2, c => c.O = NewObj("cool", 1));
            if (t.Contains("kalabalik")) Scatter(B, entries, 2, c => c.O = NewObj("civ", 1));
            if (t.Contains("fabrika")) Scatter(B, entries, 1, c => c.O = NewObj("spawner", 2), pt => pt.X >= 4);
        }

        static List<Pt> FreeTiles(BState B)
        {
            var o = new List<Pt>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                if (CellAt(B, x, y).T != "wall" && CellAt(B, x, y).O == null && UnitAt(B, x, y) == null) o.Add(new Pt(x, y));
            return o;
        }

        // ---------- Kurulum ----------
        public static BState Create(RaidParams p)
        {
            var B = new BState
            {
                Params = p, Rs = p.Seed,
                MaxTurns = p.Traits.Contains("kisa") ? 5 : 6,
                SpawnPerTurn = p.Traits.Contains("kopya") ? 2 : 1,
                IsCore = p.IsCore && p.MissionKind != "loot",
                Signature = string.IsNullOrEmpty(p.Signature) ? null : p.Signature,
                EnemyHp = p.Traits.Contains("guclu") ? 1 : 0,
                EnemyMove = p.Traits.Contains("devriye") ? 1 : 0,
                Fog = p.Traits.Contains("sis"),
            };
            Rnd(B);
            int cabinet = p.Traits.Contains("zirhli") ? 1 : 0;
            var L = Layouts[(p.Compute + p.RaidNumber + (p.Seed & 7)) % Layouts.Length];
            B.Layout = L.Name;
            var entries = new List<Pt>();
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    char ch = L.Rows[y][x];
                    var c = new Cell { T = ch == '#' ? "wall" : ch == '~' ? "cable" : "floor" };
                    if (ch == 'C') c.O = NewObj(B.IsCore ? "core" : "node", (B.IsCore ? 5 : 4) + cabinet);
                    else if (ch == 'Y' || ch == 'S') c.O = NewObj("civ", 1);
                    else if (ch == 'K') c.O = NewObj("cool", 1);
                    else if (ch == 'U') c.O = NewObj("spawner", 2);
                    B.Cells[y, x] = c;
                    if (ch == 'E') entries.Add(new Pt(x, y));
                    if (ch == 'd') B.Units.Add(NewUnit(B, "dron", x, y));
                    if (ch == 'm') B.Units.Add(NewUnit(B, "bakim", x, y));
                }
            ApplyRegionTraits(B, p, entries);
            // Kadro sığınaktan gelir (yaralı uzman eksik canla, yedek acemi, takılı plaka); verilmezse tam ve sağlam üç uzman
            var squad = p.Squad != null && p.Squad.Count > 0 ? p.Squad
                : new[] { "tek", "emp", "ana" }.Select(t => new SquadSlot { Type = t, Hp = UnitTypes[t].Hp }).ToList();
            for (int i = 0; i < squad.Count && i < entries.Count; i++)
            {
                var u = NewUnit(B, squad[i].Type, entries[i].X, entries[i].Y);
                u.Hp = Math.Max(1, Math.Min(u.Max, squad[i].Hp));
                u.Armor = Math.Max(0, squad[i].Armor);
                B.Units.Add(u);
            }
            // Zorluk kademesi: kademe başına bir makine fazla (takviye bölgesinde bir tane daha), uzak yarıda
            int extra = Math.Max(0, p.Tier - 1) + (p.Traits.Contains("takviye") ? 1 : 0);
            for (int i = 0; i < extra && Alive(B, 'e').Count < MaxEnemies; i++)
            {
                var spot = FreeTiles(B).Where(t => t.X >= 4).OrderByDescending(t => t.X).ToList();
                if (spot.Count > 0)
                {
                    var t = spot[(int)Math.Floor(Rnd(B) * Math.Min(6, spot.Count))];
                    B.Units.Add(NewUnit(B, WakeType(B, Rnd(B)), t.X, t.Y));
                }
            }
            // İmza düşmanı: tahtada en az bir (3. kademeden iki) bölge makinesi olur; eksikse son eklenen makineler döner
            if (B.Signature != null)
            {
                var foes = Alive(B, 'e').Where(u => u.Type != B.Signature).ToList();
                int have = Alive(B, 'e').Count(u => u.Type == B.Signature);
                int convert = Math.Min(foes.Count, Math.Max(0, (p.Tier >= 3 ? 2 : 1) - have));
                for (int i = 0; i < convert; i++)
                {
                    var u = foes[foes.Count - 1 - i];
                    var T = UnitTypes[B.Signature];
                    u.Type = B.Signature; u.Hp = u.Max = T.Hp + B.EnemyHp; u.Armor = T.Armor;
                }
            }
            string tgt = B.IsCore ? Loc.T("çekirdeğimin") : Loc.T("sunucularımın");
            Say(B, IsLoot(B) ? Loc.T("İkmal kasasını Teknisyen ile bitişik kareden aç. Kasayı imha etmeden malzemeleri al.") : p.Tier > 1 ? Loc.T("Yine sen. Bu sefer kapıda daha çok makine var.") : Loc.F("Demek {0} yerini buldun. Bakalım içeri girebilecek misin.", tgt), IsLoot(B) ? "sys" : "ai");
            PlanEnemies(B);
            // Güvenli giriş: ilk tur uyanma yok
            if (!p.Traits.Contains("guvenli")) PlaceSpawns(B);
            B.FxList.Clear();
            return B;
        }

        // ---------- Hasar, itme ----------
        static void Hurt(BState B, Unit u, int n, string why)
        {
            if (u.Dead || n <= 0) return;
            if (u.Armor > 0)
            {
                // Zırh plakası ilk hasarı karşılar ve kırılır
                int a = Math.Min(u.Armor, n);
                u.Armor -= a; n -= a;
                AddFx(B, new Fx { K = "plate", X = u.X, Y = u.Y, Unit = u.Id });
                Say(B, Loc.F("{0}: zırh plakası darbeyi karşıladı ve kırıldı.", UnitTypes[u.Type].Name), "sys");
                if (n <= 0) return;
            }
            u.Hp -= n;
            AddFx(B, new Fx { K = "dmg", X = u.X, Y = u.Y, N = n, Unit = u.Id });
            if (u.Hp > 0) return;
            u.Dead = true; u.Hp = 0;
            AddFx(B, new Fx { K = "die", X = u.X, Y = u.Y, Unit = u.Id });
            string w = why != null ? " (" + why + ")" : "";
            if (u.Side == 'p') Say(B, Loc.F("{0} yere düştü{1}.", UnitTypes[u.Type].Name, w), "bad");
            else Say(B, Loc.F("{0} devre dışı{1}.", UnitTypes[u.Type].Name, w), "good");
        }

        static void HurtObj(BState B, int x, int y, int n)
        {
            var c = CellAt(B, x, y);
            if (c.O == null || n <= 0) return;
            var o = c.O;
            o.Hp -= n;
            AddFx(B, new Fx { K = "dmg", X = x, Y = y, N = n, Obj = o.K });
            if (o.Hp > 0) return;
            c.O = null;
            AddFx(B, new Fx { K = "break", X = x, Y = y, Obj = o.K });
            if (o.K == "civ") { B.Collateral++; Say(B, Loc.T("Sivil sunucu kırıldı: hastane ve banka hizmetleri kesildi."), "bad"); }
            else if (o.K == "spawner") Say(B, Loc.T("Kopya dolabı kırıldı: oradan robot uyanmayacak."), "good");
            else if (o.K == "cool") { Say(B, Loc.T("Soğutma ünitesi patladı."), "sys"); foreach (var d in Dirs) DamageAt(B, x + d[0], y + d[1], 1, Loc.T("patlama")); }
            else if (IsTarget(o.K))
            {
                if (IsLoot(B)) Lose(B, Loc.T("İkmal kasası imha edildi. Malzemeler kurtarılamadı."));
                else { Say(B, Loc.F("{0} kırıldı.", Loc.T(ObjNames[o.K])), "good"); CheckWin(B); }
            }
        }

        static void DamageAt(BState B, int x, int y, int n, string why)
        {
            if (!Inb(x, y)) return;
            var u = UnitAt(B, x, y);
            if (u != null) Hurt(B, u, n, why);
            else if (CellAt(B, x, y).O != null) HurtObj(B, x, y, n);
        }

        static void Push(BState B, Unit u, int[] d)
        {
            if (u.Dead) return;
            int nx = u.X + d[0], ny = u.Y + d[1];
            AddFx(B, new Fx { K = "push", X = u.X, Y = u.Y, Dx = d[0], Dy = d[1], Unit = u.Id });
            if (!Inb(nx, ny) || CellAt(B, nx, ny).T == "wall") { Hurt(B, u, 1, Loc.T("duvara çarptı")); return; }
            if (CellAt(B, nx, ny).O != null) { Hurt(B, u, 1, Loc.T("dolaba çarptı")); HurtObj(B, nx, ny, 1); return; }
            var o = UnitAt(B, nx, ny);
            if (o != null) { Hurt(B, u, 1, Loc.T("çarpışma")); Hurt(B, o, 1, Loc.T("çarpışma")); return; }
            u.X = nx; u.Y = ny;
            if (CellAt(B, nx, ny).T == "cable") Hurt(B, u, 1, Loc.T("elektrik"));
        }

        static void CheckWin(BState B)
        {
            if (TargetsLeft(B).Count > 0) return;
            B.Phase = "won";
            B.OutcomeSuccess = true;
            B.OutcomeReason = IsLoot(B) ? Loc.T("İkmal malzemeleri alındı. Ekip tahliye edildi.") : B.IsCore ? Loc.T("Çekirdek yok edildi.") : Loc.T("YZ düğümü kırıldı.");
        }

        public static List<(int X, int Y, Obj O)> TargetsLeft(BState B)
        {
            var o = new List<(int, int, Obj)>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { var ob = CellAt(B, x, y).O; if (ob != null && IsTarget(ob.K)) o.Add((x, y, ob)); }
            return o;
        }

        // ---------- Hareket ----------
        /// <summary>Dost birimlerin içinden geçilebilir ama üstünde durulamaz; düşman, dolap ve duvar geçilmez.</summary>
        public static List<Pt> Reachable(BState B, Unit u)
        {
            var T = UnitTypes[u.Type];
            int move = T.Move + (u.Side == 'e' ? B.EnemyMove : 0);
            var seen = new Dictionary<int, int> { [u.Y * N + u.X] = 0 };
            var q = new Queue<Pt>();
            q.Enqueue(new Pt(u.X, u.Y));
            var o = new List<Pt>();
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                int d = seen[p.Y * N + p.X];
                if (d >= move) continue;
                foreach (var dd in Dirs)
                {
                    int nx = p.X + dd[0], ny = p.Y + dd[1];
                    if (!Inb(nx, ny) || seen.ContainsKey(ny * N + nx)) continue;
                    var c = CellAt(B, nx, ny);
                    if (c.T == "wall" || c.O != null) continue;
                    var other = UnitAt(B, nx, ny);
                    if (other != null && other.Side != u.Side) continue;
                    seen[ny * N + nx] = d + 1;
                    q.Enqueue(new Pt(nx, ny));
                    if (other == null) o.Add(new Pt(nx, ny));
                }
            }
            return o;
        }

        // ---------- Oyuncu yetenekleri ----------
        public static List<Pt> Targets(BState B, Unit u)
        {
            var o = new List<Pt>();
            var ab = UnitTypes[u.Type].Ability;
            if (ab == "sabotaj")
            {
                foreach (var d in Dirs)
                {
                    int x = u.X + d[0], y = u.Y + d[1];
                    if (Inb(x, y) && (UnitAt(B, x, y) != null || CellAt(B, x, y).O != null)) o.Add(new Pt(x, y));
                }
            }
            else if (ab == "emp")
            {
                foreach (var d in Dirs) for (int k = 2; k <= 5; k++) { int x = u.X + d[0] * k, y = u.Y + d[1] * k; if (Inb(x, y)) o.Add(new Pt(x, y)); }
            }
            else if (ab == "kanca")
            {
                foreach (var d in Dirs) { var h = HookTrace(B, u, d); if (h != null) o.Add(new Pt(h.X, h.Y)); }
            }
            return o;
        }

        sealed class Hit { public int X, Y, K; public bool Edge, None; }

        static Hit HookTrace(BState B, Unit u, int[] d)
        {
            for (int k = 1; k <= 5; k++)
            {
                int x = u.X + d[0] * k, y = u.Y + d[1] * k;
                if (!Inb(x, y)) return k > 1 ? new Hit { X = x - d[0], Y = y - d[1], K = k - 1, Edge = true } : null;
                if (CellAt(B, x, y).T == "wall" || CellAt(B, x, y).O != null || UnitAt(B, x, y) != null) return new Hit { X = x, Y = y, K = k };
            }
            return null;
        }

        static void ApplyAbility(BState B, Unit u, int x, int y)
        {
            var ab = UnitTypes[u.Type].Ability;
            var d = new[] { Sign(x - u.X), Sign(y - u.Y) };
            if (ab == "sabotaj")
            {
                var t = UnitAt(B, x, y);
                if (t != null) { Hurt(B, t, 1, Loc.T("sabotaj")); Push(B, t, d); }
                else if (IsLoot(B) && CellAt(B, x, y).O is Obj cargo && IsTarget(cargo.K))
                {
                    // Aynı yetenek komutu ve önizleme yolu kullanılır; ikmal görevi dolabı kırmak yerine açar.
                    CellAt(B, x, y).O = null;
                    AddFx(B, new Fx { K = "loot", X = x, Y = y, Obj = cargo.K });
                    Say(B, Loc.T("Teknisyen ikmal kasasını açtı; malzemeler güvenceye alındı."), "good");
                    CheckWin(B);
                }
                else if (CellAt(B, x, y).O != null) HurtObj(B, x, y, 2);
            }
            else if (ab == "emp")
            {
                AddFx(B, new Fx { K = "emp", X = x, Y = y });
                var o = CellAt(B, x, y).O;
                if (o != null && IsTarget(o.K) && UnitAt(B, x, y) == null) AddFx(B, new Fx { K = "armor", X = x, Y = y });
                else DamageAt(B, x, y, 1, Loc.T("EMP"));
                foreach (var dd in Dirs) { var t = UnitAt(B, x + dd[0], y + dd[1]); if (t != null) Push(B, t, dd); }
            }
            else if (ab == "kanca")
            {
                var h = HookTrace(B, u, d);
                if (h == null) return;
                AddFx(B, new Fx { K = "hook", X = h.X, Y = h.Y });
                var t = !h.Edge ? UnitAt(B, h.X, h.Y) : null;
                if (t != null)
                {
                    if (h.K > 1)
                    {
                        t.X -= d[0]; t.Y -= d[1];
                        AddFx(B, new Fx { K = "pull", Unit = t.Id });
                        if (CellAt(B, t.X, t.Y).T == "cable") Hurt(B, t, 1, Loc.T("elektrik"));
                    }
                }
                else if (h.K > 1 || h.Edge)
                {
                    int tx = h.Edge ? h.X : h.X - d[0], ty = h.Edge ? h.Y : h.Y - d[1];
                    if (!(tx == u.X && ty == u.Y)) { u.X = tx; u.Y = ty; if (CellAt(B, tx, ty).T == "cable") Hurt(B, u, 1, Loc.T("elektrik")); }
                }
            }
        }

        static void Snapshot(BState B) => B.Hist.Add(B.Clone());

        public static bool CanMove(BState B, Unit u) => B.Phase == "player" && !u.Dead && u.Side == 'p' && !u.Moved && !u.Acted;
        public static bool CanAct(BState B, Unit u) => B.Phase == "player" && !u.Dead && u.Side == 'p' && !u.Acted;
        static Unit ById(BState B, int id) => B.Units.FirstOrDefault(v => v.Id == id);

        public static bool DoMove(BState B, int uid, int x, int y)
        {
            var u = ById(B, uid);
            if (u == null || !CanMove(B, u)) return false;
            if (!Reachable(B, u).Any(t => t.X == x && t.Y == y)) return false;
            Snapshot(B);
            u.X = x; u.Y = y; u.Moved = true;
            return true;
        }

        public static bool DoAct(BState B, int uid, int x, int y)
        {
            var u = ById(B, uid);
            if (u == null || !CanAct(B, u)) return false;
            if (!Targets(B, u).Any(t => t.X == x && t.Y == y)) return false;
            Snapshot(B);
            B.FxList.Clear();
            ApplyAbility(B, u, x, y);
            u.Acted = true; u.Moved = true;
            if (Alive(B, 'p').Count == 0 && B.Phase == "player") Lose(B, Loc.T("Ekibin tamamı yere düştü."));
            return true;
        }

        /// <summary>Turdaki son hamleyi geri alır. Durum yerinde değiştirilir.</summary>
        public static bool Undo(BState B)
        {
            if (B.Hist.Count == 0 || B.Phase != "player") return false;
            var prev = B.Hist[B.Hist.Count - 1];
            var hist = B.Hist;
            hist.RemoveAt(hist.Count - 1);
            CopyInto(prev, B);
            B.Hist = hist;
            return true;
        }

        static void CopyInto(BState src, BState dst)
        {
            var c = src.Clone();
            dst.Params = c.Params; dst.Rs = c.Rs; dst.Turn = c.Turn; dst.MaxTurns = c.MaxTurns; dst.SpawnPerTurn = c.SpawnPerTurn;
            dst.Collateral = c.Collateral; dst.NextId = c.NextId; dst.Phase = c.Phase; dst.Layout = c.Layout; dst.OutcomeReason = c.OutcomeReason;
            dst.IsCore = c.IsCore; dst.OutcomeSuccess = c.OutcomeSuccess; dst.Retreated = c.Retreated;
            dst.Signature = c.Signature; dst.EnemyHp = c.EnemyHp; dst.EnemyMove = c.EnemyMove; dst.Fog = c.Fog;
            dst.Cells = c.Cells; dst.Units = c.Units; dst.Spawns = c.Spawns; dst.Log = c.Log; dst.FxList = c.FxList;
        }

        public sealed class PreviewResult
        {
            public List<Fx> Fx;
            public List<(int Unit, Pt From, Pt To)> Moves;
            public int Collateral;
            public bool Won;
        }

        /// <summary>Bir yeteneğin sonucu: durumun kopyasında uygulanır, değişen her şey listelenir.</summary>
        public static PreviewResult Preview(BState B, int uid, int x, int y)
        {
            var C = B.Clone();
            C.FxList.Clear();
            var u = ById(C, uid);
            ApplyAbility(C, u, x, y);
            var moves = new List<(int, Pt, Pt)>();
            foreach (var cu in C.Units)
            {
                var ou = ById(B, cu.Id);
                if (ou != null && (ou.X != cu.X || ou.Y != cu.Y)) moves.Add((cu.Id, new Pt(ou.X, ou.Y), new Pt(cu.X, cu.Y)));
            }
            return new PreviewResult { Fx = C.FxList, Moves = moves, Collateral = C.Collateral - B.Collateral, Won = C.Phase == "won" };
        }

        // ---------- Düşman ----------
        sealed class ITarget { public int X, Y, K; public bool None; }

        /// <summary>Kilitleyicinin ve havanın menzili.</summary>
        const int LockRange = 4, LobRange = 3;

        /// <summary>
        /// Niyetin vuracağı kare: ateş ve kilit düz hatta ilk engele (kilit en çok 4 kare), havan 3 kare öteye engellerin
        /// üstünden, patlama makinenin kendi karesine (çevresi <see cref="BlastArea"/>), öteki niyetler bitişik kareye.
        /// </summary>
        static ITarget IntentTarget(BState B, Unit e, string kind, int dir, int px, int py)
        {
            int dx = Dirs[dir][0], dy = Dirs[dir][1];
            if (kind == "shoot" || kind == "lock")
            {
                int range = kind == "lock" ? LockRange : N;
                for (int k = 1; k < N; k++)
                {
                    int x = px + dx * k, y = py + dy * k;
                    if (!Inb(x, y) || k > range) return new ITarget { X = x - dx, Y = y - dy, K = k - 1, None = true };
                    if (CellAt(B, x, y).T == "wall" || CellAt(B, x, y).O != null) return new ITarget { X = x, Y = y, K = k };
                    var u = UnitAt(B, x, y);
                    if (u != null && u.Id != e.Id) return new ITarget { X = x, Y = y, K = k };
                }
                return null;
            }
            if (kind == "lob")
            {
                int lx = px + dx * LobRange, ly = py + dy * LobRange;
                return Inb(lx, ly) ? new ITarget { X = lx, Y = ly, K = LobRange } : null;
            }
            if (kind == "blast") return new ITarget { X = px, Y = py, K = 0 };
            int tx = px + dx, ty = py + dy;
            return Inb(tx, ty) ? new ITarget { X = tx, Y = ty, K = 1 } : null;
        }

        /// <summary>Patlamanın vurduğu kareler: makinenin çevresindeki dört kare.</summary>
        public static List<Pt> BlastArea(int x, int y) =>
            Dirs.Select(d => new Pt(x + d[0], y + d[1])).Where(p => Inb(p.X, p.Y)).ToList();

        static double ScoreTarget(BState B, Unit e, string kind, int dir, int px, int py)
        {
            var t = IntentTarget(B, e, kind, dir, px, py);
            if (t == null || t.None) return 0;
            if (CellAt(B, t.X, t.Y).T == "wall") return kind == "shield" || kind == "clone" ? -1 : 0;
            var u = UnitAt(B, t.X, t.Y);
            // Onarım yalnızca hasarlı hedef dolaba anlamlı; birime yöneltilen onarım hiçbir şey yapmaz
            if (kind == "repair")
            {
                var ro = u == null || u.Id == e.Id ? CellAt(B, t.X, t.Y).O : null;
                return ro != null && IsTarget(ro.K) && ro.Hp < ro.Max ? 14 : -1;
            }
            // Kalkan: yanındaki zırhsız makineye; çoğaltma: yanındaki boş zemine (makine sınırı dolmadıysa)
            if (kind == "shield") return u != null && u.Id != e.Id && u.Side == 'e' && u.Armor == 0 ? 12 : -1;
            if (kind == "clone")
                return u == null && CellAt(B, t.X, t.Y).T == "floor" && CellAt(B, t.X, t.Y).O == null && Alive(B, 'e').Count < MaxEnemies ? 7 : -1;
            // Kilit hasar vermez: yalnız ekip üyesine anlamlı
            if (kind == "lock") return u != null && u.Id != e.Id && u.Side == 'p' && !u.Locked ? 9 + (u.Type == "tek" ? 3 : 0) : 0;
            // Patlama: çevredeki ekip üyeleri çeker; makine, hedef dolap ve sivil sunucu iter (yön tek: dir 0)
            if (kind == "blast")
            {
                if (dir != 0) return -100;
                double sum = 0;
                foreach (var a in BlastArea(px, py))
                {
                    var v = UnitAt(B, a.X, a.Y);
                    if (v != null && v.Id != e.Id) { sum += v.Side == 'p' ? 9 + (v.Type == "tek" ? 3 : 0) : -3; continue; }
                    var ao = CellAt(B, a.X, a.Y).O;
                    if (ao == null) continue;
                    sum += IsTarget(ao.K) ? -20 : ao.K == "spawner" ? -10 : ao.K == "civ" ? -2 : ao.K == "cool" ? 1 : 0;
                }
                return sum;
            }
            if (u != null && u.Id != e.Id)
                return u.Side == 'p' ? 10 + (u.Type == "tek" ? 3 : 0) + (u.Hp <= (kind == "melee" ? 2 : 1) ? 4 : 0) : -6;
            var o = CellAt(B, t.X, t.Y).O;
            if (o == null) return 0;
            if (IsTarget(o.K)) return -20;
            if (o.K == "spawner") return -10;
            // Makineler sivil sunucuya bilerek ateş etmez; hedefi kaçınca atış arkadaki sunucuyu yine vurabilir (sivil güven)
            if (o.K == "civ") return -2;
            if (o.K == "cool")
                return 2 + 4 * Dirs.Count(d => { var v = UnitAt(B, t.X + d[0], t.Y + d[1]); return v != null && v.Side == 'p'; });
            return 0;
        }

        static void PlanEnemies(BState B)
        {
            int order = 0;
            foreach (var e in Alive(B, 'e'))
            {
                var type = UnitTypes[e.Type];
                var kinds = type.Kinds;
                var spots = new List<Pt> { new Pt(e.X, e.Y) };
                spots.AddRange(Reachable(B, e));
                var players = Alive(B, 'p');
                var best = new List<(Pt pos, string kind, int dir)>();
                double bestScore = double.NegativeInfinity;
                foreach (var pos in spots)
                {
                    // Konum tercihi: uzaktan vuranlar mesafede durmayı, ötekiler yaklaşmayı sever
                    int near = players.Count > 0 ? players.Min(p => Man(p.X, p.Y, pos.X, pos.Y)) : 0;
                    double posScore = type.Ranged ? (near <= 1 ? -2 : 0) : -0.2 * near;
                    foreach (var kind in kinds)
                        for (int dir = 0; dir < 4; dir++)
                        {
                            double sc = ScoreTarget(B, e, kind, dir, pos.X, pos.Y) + posScore;
                            if (sc > bestScore + 1e-9) { bestScore = sc; best.Clear(); best.Add((pos, kind, dir)); }
                            else if (Math.Abs(sc - bestScore) < 1e-9) best.Add((pos, kind, dir));
                        }
                }
                var pk = best[(int)Math.Floor(Rnd(B) * best.Count)];
                e.X = pk.pos.X; e.Y = pk.pos.Y;
                e.Intent = new Intent { Kind = pk.kind, Dir = pk.dir };
                e.Order = ++order;
            }
        }

        static void PlaceSpawns(BState B)
        {
            B.Spawns = new List<Pt>();
            if (B.Phase != "player") return;
            int room = MaxEnemies - Alive(B, 'e').Count;
            var cand = new List<Pt>();
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var o = CellAt(B, x, y).O;
                    if (o == null || o.K != "spawner") continue;
                    foreach (var d in Dirs)
                    {
                        int nx = x + d[0], ny = y + d[1];
                        if (Inb(nx, ny) && CellAt(B, nx, ny).T != "wall" && CellAt(B, nx, ny).O == null && UnitAt(B, nx, ny) == null && !cand.Any(c => c.X == nx && c.Y == ny))
                            cand.Add(new Pt(nx, ny));
                    }
                }
            for (int i = 0; i < Math.Min(B.SpawnPerTurn, room) && cand.Count > 0; i++)
            {
                int k = (int)Math.Floor(Rnd(B) * cand.Count);
                B.Spawns.Add(cand[k]);
                cand.RemoveAt(k);
            }
        }

        public sealed class IntentView
        {
            public int Unit, Order, Dir, FromX, FromY, TargetX, TargetY;
            public string Kind;
            public bool HasTarget, TargetNone;
            /// <summary>Patlamanın vuracağı kareler (öteki niyetlerde boş).</summary>
            public List<Pt> Area = new List<Pt>();
        }

        /// <summary>Arayüz için: her düşmanın gösterilen niyeti (yalan olabilir).</summary>
        public static List<IntentView> Intents(BState B)
        {
            var list = new List<IntentView>();
            foreach (var e in Alive(B, 'e').Where(u => u.Intent != null).OrderBy(u => u.Order))
            {
                int dir = e.Intent.Dir;
                var t = IntentTarget(B, e, e.Intent.Kind, dir, e.X, e.Y);
                list.Add(new IntentView
                {
                    Unit = e.Id, Order = e.Order, Kind = e.Intent.Kind, Dir = dir,
                    FromX = e.X, FromY = e.Y, HasTarget = t != null, TargetNone = t != null && t.None, TargetX = t?.X ?? 0, TargetY = t?.Y ?? 0,
                    Area = e.Intent.Kind == "blast" ? BlastArea(e.X, e.Y) : new List<Pt>(),
                });
            }
            return list;
        }

        static void Lose(BState B, string reason) { B.Phase = "lost"; B.OutcomeSuccess = false; B.OutcomeReason = reason; }

        /// <summary>Bir makinenin niyeti (düşman evresinde, sırası gelince).</summary>
        static void ApplyIntent(BState B, Unit e, ITarget t)
        {
            switch (e.Intent.Kind)
            {
                case "repair":
                {
                    var o = CellAt(B, t.X, t.Y).O;
                    if (o != null && IsTarget(o.K) && o.Hp < o.Max) { o.Hp++; AddFx(B, new Fx { K = "heal", X = t.X, Y = t.Y }); Say(B, Loc.F("Bakım robotu {0} onardı.", LowerName(Loc.T(ObjNames[o.K]))), "bad"); }
                    break;
                }
                case "melee": DamageAt(B, t.X, t.Y, 2, Loc.T("bakım robotu")); break;
                case "lob": DamageAt(B, t.X, t.Y, 1, Loc.T("havan")); break;
                case "lock":
                {
                    var v = UnitAt(B, t.X, t.Y);
                    if (v == null || v.Side != 'p') break;
                    v.Locked = true;
                    AddFx(B, new Fx { K = "lock", X = v.X, Y = v.Y, Unit = v.Id });
                    Say(B, Loc.F("{0} kilitlendi: bu tur yürüyemez.", UnitTypes[v.Type].Name), "bad");
                    break;
                }
                case "shield":
                {
                    var v = UnitAt(B, t.X, t.Y);
                    if (v == null || v.Side != 'e' || v.Armor > 0) break;
                    v.Armor = 1;
                    AddFx(B, new Fx { K = "shield", X = v.X, Y = v.Y, Unit = v.Id });
                    Say(B, Loc.F("Kalkan dronu {0} zırhladı.", LowerName(UnitTypes[v.Type].Name)), "bad");
                    break;
                }
                case "clone":
                {
                    // Uyanma gibi: karede biri varsa uyanma engellenir ama duran 1 hasar alır
                    var v = UnitAt(B, t.X, t.Y);
                    if (v != null) { Hurt(B, v, 1, Loc.T("uyanmayı engelledi")); break; }
                    if (CellAt(B, t.X, t.Y).T == "wall" || CellAt(B, t.X, t.Y).O != null || Alive(B, 'e').Count >= MaxEnemies) break;
                    B.Units.Add(NewUnit(B, "dron", t.X, t.Y));
                    AddFx(B, new Fx { K = "spawn", X = t.X, Y = t.Y });
                    Say(B, Loc.T("Çoğaltıcı yeni bir dron uyandırdı."), "bad");
                    break;
                }
                case "blast":
                {
                    AddFx(B, new Fx { K = "blast", X = e.X, Y = e.Y, Unit = e.Id });
                    Say(B, Loc.T("Patlayan robot infilak etti."), "bad");
                    foreach (var a in BlastArea(e.X, e.Y)) DamageAt(B, a.X, a.Y, 1, Loc.T("patlama"));
                    if (!e.Dead) { e.Dead = true; e.Hp = 0; AddFx(B, new Fx { K = "die", X = e.X, Y = e.Y, Unit = e.Id }); }
                    break;
                }
                default: DamageAt(B, t.X, t.Y, 1, Loc.T("dron ateşi")); break;
            }
        }

        public static bool EndTurn(BState B)
        {
            if (B.Phase != "player") return false;
            B.FxList.Clear(); B.Hist.Clear();
            // Geçen turun kilidi çözülür; bu evrede vurulan uzman gelecek tur yürüyemez
            foreach (var u in B.Units) if (u.Side == 'p') u.Locked = false;
            Say(B, Loc.F("— Tur {0}: makineler hareket ediyor —", B.Turn), "turn");
            // 1) Niyetler sırayla (itilen robot niyetini yeni yerinden uygular)
            foreach (var e in Alive(B, 'e').OrderBy(u => u.Order).ToList())
            {
                if (e.Dead || e.Intent == null || B.Phase != "player") continue;
                var t = IntentTarget(B, e, e.Intent.Kind, e.Intent.Dir, e.X, e.Y);
                AddFx(B, new Fx { K = "attack", Unit = e.Id, X = t?.X ?? e.X, Y = t?.Y ?? e.Y });
                if (t == null || t.None) continue;
                ApplyIntent(B, e, t);
            }
            if (B.Phase != "player") return true;
            // 2) Uyanma: işaretli karede biri duruyorsa uyanma engellenir ama duran hasar alır
            foreach (var s in B.Spawns)
            {
                var u = UnitAt(B, s.X, s.Y);
                if (u != null) { Hurt(B, u, 1, Loc.T("uyanmayı engelledi")); Say(B, Loc.T("Uyanan kopya engellendi."), u.Side == 'p' ? "good" : "sys"); continue; }
                if (CellAt(B, s.X, s.Y).O != null) continue;
                var tp = WakeType(B, Rnd(B));
                B.Units.Add(NewUnit(B, tp, s.X, s.Y));
                AddFx(B, new Fx { K = "spawn", X = s.X, Y = s.Y });
                Say(B, Loc.F("Yeni bir {0} uyandı.", LowerName(UnitTypes[tp].Name)), "bad");
            }
            B.Spawns = new List<Pt>();
            if (Alive(B, 'p').Count == 0) { Lose(B, Loc.T("Ekibin tamamı yere düştü.")); return true; }
            B.Turn++;
            if (B.Turn > B.MaxTurns)
            {
                Lose(B, IsLoot(B) ? Loc.T("Süre doldu: YZ ikmal deposunu kilitledi.") : B.IsCore ? Loc.T("Süre doldu: YZ çekirdeğini binadan taşıdı.") : Loc.T("Süre doldu: YZ sunucularını yedekledi ve binayı kilitledi."));
                return true;
            }
            // 3) Yeni plan
            // Kilitli uzman bu tur yürüyemez (yeteneği serbest)
            foreach (var u in B.Units) if (u.Side == 'p') { u.Moved = u.Locked; u.Acted = false; }
            PlanEnemies(B);
            PlaceSpawns(B);
            if (B.Turn == B.MaxTurns) Say(B, Loc.T("Son tur."), "turn");
            return true;
        }

        static readonly System.Globalization.CultureInfo UiCulture = new System.Globalization.CultureInfo("tr-TR");

        /// <summary>Cümle içi ad: oyun dilinin küçük harfi (Türkçede “İ → i”); kısaltmalar (YZ, AI) korunur.</summary>
        static string LowerName(string name)
        {
            var culture = Loc.IsSource ? UiCulture : System.Globalization.CultureInfo.InvariantCulture;
            return string.Join(" ", name.Split(' ').Select(w => w.Length > 1 && w.ToUpper(culture) == w ? w : w.ToLower(culture)));
        }

        public static bool Retreat(BState B)
        {
            if (B.Phase != "player") return false;
            Lose(B, Loc.T("Ekip geri çekildi."));
            B.Retreated = true;
            return true;
        }

        public static RaidResult Result(BState B)
        {
            return new RaidResult
            {
                Success = B.Phase == "won", Retreat = B.Retreated, Collateral = B.Collateral,
                Down = B.Units.Count(u => u.Side == 'p' && u.Dead), Turns = Math.Min(B.Turn, B.MaxTurns),
                Reason = B.OutcomeReason ?? "",
                Hp = B.Units.Where(u => u.Side == 'p').Select(u => u.Dead ? 0 : u.Hp).ToList(),
                Armor = B.Units.Where(u => u.Side == 'p').Select(u => u.Armor).ToList(),
            };
        }

        /// <summary>Gerileme testleri için durumun özeti (RaidEngineTests).</summary>
        public static string Signature(BState B)
        {
            var objs = new List<string>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { var o = CellAt(B, x, y).O; if (o != null) objs.Add($"{o.K}{x}{y}:{o.Hp}"); }
            return $"{B.Phase}|t{B.Turn}|c{B.Collateral}|rs{B.Rs}|" + string.Join(",", B.Units.Select(u => $"{u.Type}{u.X}{u.Y}:{u.Hp}{(u.Dead ? "x" : "")}")) + "|" + string.Join(",", objs);
        }
    }
}
