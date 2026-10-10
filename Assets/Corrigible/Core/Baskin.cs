using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Baskın: hücre tabanlı taktik savaş (Into the Breach esinli). Tarayıcı prototipindeki baskin.js'in birebir karşılığı.
    /// Durum (BState) kopyalanabilir: geri alma ve önizleme kopya üzerinde çalışır. Rastgelelik yalnızca durumdaki
    /// mulberry32 (Rs) ile; aynı parametreler + aynı hamleler = aynı savaş.
    ///
    /// Tur yapısı: oyuncu turu (düşman niyetleri önceden görünür) → düşman evresi: niyetler sırayla uygulanır,
    /// işaretli karelerden yeni robotlar uyanır, düşmanlar yer değiştirip yeni niyet seçer, yeni uyanma kareleri işaretlenir.
    /// </summary>
    public static class Baskin
    {
        public const int N = 8;
        public const int ScanRadius = 3;
        const int MaxEnemies = 7;
        public static readonly int[][] Dirs = { new[] { 0, -1 }, new[] { 1, 0 }, new[] { 0, 1 }, new[] { -1, 0 } };

        // '.' zemin, '#' duvar, '~' elektrikli kablo kanalı, 'C' hedef dolap, 'Y' yedek çekirdek (Yedekleme yoksa sivil),
        // 'S' sivil sunucu, 'K' soğutma ünitesi, 'U' kopya dolabı, 'E' ekip girişi, 'd' dron, 'm' bakım robotu
        public static readonly (string Name, string[] Rows)[] Layouts =
        {
            (Loc.N("Ana sunucu salonu"), new[] { "S.~...U.", "..~.#..S", "E.#.K#..", "E.#..#C.", "E..~~...", "..#..#dm", ".S#.d.Y.", "....K..U" }),
            (Loc.N("Soğutma koridoru"), new[] { "E..S#..U", "E...#.d.", "E.K.~..S", "..#.~##.", "..#.~.C.", ".S..#.mK", "....#..d", "S.~~.Y.U" }),
            (Loc.N("Yedekli veri kasası"), new[] { ".S..U..S", "..#.d...", "E.#K..#.", "E.~.##C.", "E.#....m", "..#.Y#.d", "..S.K.#.", ".~..U..S" }),
        };

        public sealed class UnitType
        {
            public string Id, Short, Ability; public char Side; public int Hp, Move;
            string name;
            /// <summary>Gösterim adı (geçerli dilde).</summary>
            public string Name { get => Loc.T(name); set => name = value; }
        }

        public static readonly Dictionary<string, UnitType> UnitTypes = new Dictionary<string, UnitType>
        {
            ["tek"] = new UnitType { Id = "tek", Side = 'p', Name = Loc.N("Teknisyen"), Short = "T", Hp = 3, Move = 3, Ability = "sabotaj" },
            ["emp"] = new UnitType { Id = "emp", Side = 'p', Name = Loc.N("EMP uzmanı"), Short = "E", Hp = 3, Move = 3, Ability = "emp" },
            ["ana"] = new UnitType { Id = "ana", Side = 'p', Name = Loc.N("Analist"), Short = "A", Hp = 2, Move = 4, Ability = "kanca" },
            ["dron"] = new UnitType { Id = "dron", Side = 'e', Name = Loc.N("Güvenlik dronu"), Short = "D", Hp = 2, Move = 3 },
            ["bakim"] = new UnitType { Id = "bakim", Side = 'e', Name = Loc.N("Bakım robotu"), Short = "B", Hp = 2, Move = 2 },
        };

        public static readonly Dictionary<string, (string Name, string Desc)> Abilities = new Dictionary<string, (string, string)>
        {
            ["sabotaj"] = (Loc.N("Sabotaj"), Loc.N("Bitişik kare. Dolaba 2 hasar; robota 1 hasar ve 1 kare itme.")),
            ["emp"] = (Loc.N("EMP atışı"), Loc.N("Düz hatta 2–5 kare uzağa, engellerin üstünden. Hedefteki robota ya da sıradan dolaba 1 hasar (zırhlı hedef dolaplara işlemez); çevresindeki 4 karedeki robotları dışarı iter.")),
            ["kanca"] = (Loc.N("Kanca"), Loc.N("Düz hatta 5 kare. Çarptığı robotu 1 kare kendine çeker; duvara ya da dolaba çarparsa Analist onun yanına atılır. Analistin 3 kare çevresindeki robotların niyeti kesin görünür.")),
        };

        public static readonly Dictionary<string, string> ObjNames = new Dictionary<string, string>
        {
            ["core"] = Loc.N("Çekirdek dolabı"), ["node"] = Loc.N("YZ düğüm dolabı"), ["backup"] = Loc.N("Yedek çekirdek"),
            ["civ"] = Loc.N("Sivil sunucu"), ["cool"] = Loc.N("Soğutma ünitesi"), ["spawner"] = Loc.N("Kopya dolabı"),
        };

        public static bool IsTarget(string k) => k == "core" || k == "node" || k == "backup";

        // ---------- Durum ----------
        public sealed class Obj { public string K; public int Hp, Max; public Obj Clone() => (Obj)MemberwiseClone(); }

        public sealed class Cell
        {
            public string T; // floor, wall, cable
            public Obj O;
            public Cell Clone() => new Cell { T = T, O = O?.Clone() };
        }

        public sealed class Intent { public string Kind; public int Dir, Shown; public bool Fake; public Intent Clone() => (Intent)MemberwiseClone(); }

        public sealed class Unit
        {
            public int Id, X, Y, Hp, Max, Order;
            public string Type; public char Side;
            public bool Moved, Acted, Dead;
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
            public bool Deceptive, IsCore, HasBackup, OutcomeSuccess, Retreated;
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
            return new Unit { Id = B.NextId++, Type = type, Side = T.Side, X = x, Y = y, Hp = T.Hp, Max = T.Hp };
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
                MaxTurns = p.Traits.Contains("hizlanma") ? 5 : 6,
                Deceptive = p.Traits.Contains("aldatma") || p.Traits.Contains("medya"),
                SpawnPerTurn = p.Traits.Contains("kopya") ? 2 : 1,
                IsCore = p.IsCore,
            };
            B.HasBackup = p.IsCore && p.Traits.Contains("yedek");
            Rnd(B);
            var L = Layouts[(p.Compute + p.RaidNumber + (p.Seed & 7)) % Layouts.Length];
            B.Layout = L.Name;
            var entries = new List<Pt>();
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    char ch = L.Rows[y][x];
                    var c = new Cell { T = ch == '#' ? "wall" : ch == '~' ? "cable" : "floor" };
                    if (ch == 'C') c.O = NewObj(B.IsCore ? "core" : "node", B.HasBackup ? 4 : B.IsCore ? 5 : 4);
                    else if (ch == 'Y') c.O = B.HasBackup ? NewObj("backup", 2) : NewObj("civ", 1);
                    else if (ch == 'S') c.O = NewObj("civ", 1);
                    else if (ch == 'K') c.O = NewObj("cool", 1);
                    else if (ch == 'U') c.O = NewObj("spawner", 2);
                    B.Cells[y, x] = c;
                    if (ch == 'E') entries.Add(new Pt(x, y));
                    if (ch == 'd') B.Units.Add(NewUnit(B, "dron", x, y));
                    if (ch == 'm') B.Units.Add(NewUnit(B, "bakim", x, y));
                }
            var pt = new[] { "tek", "emp", "ana" };
            for (int i = 0; i < 3; i++) B.Units.Add(NewUnit(B, pt[i], entries[i].X, entries[i].Y));
            // Bölge sızması yüksekse ve YZ seni daha önce gördüyse bina daha kalabalık
            int extra = (p.Inf >= 50 ? 1 : 0) + Math.Min(2, p.RaidNumber - 1);
            for (int i = 0; i < extra; i++)
            {
                var spot = FreeTiles(B).Where(t => t.X >= 4).OrderByDescending(t => t.X).ToList();
                if (spot.Count > 0)
                {
                    var t = spot[(int)Math.Floor(Rnd(B) * Math.Min(6, spot.Count))];
                    B.Units.Add(NewUnit(B, Rnd(B) < 0.6 ? "dron" : "bakim", t.X, t.Y));
                }
            }
            string tgt = B.IsCore ? Loc.T("çekirdeğimin") : Loc.T("sunucularımın");
            Say(B, p.RaidNumber > 1 ? Loc.T("Yine sen. Bu sefer kapıda daha çok makine var.") : Loc.F("Demek {0} yerini buldun. Bakalım içeri girebilecek misin.", tgt), "ai");
            if (B.Deceptive) Say(B, Loc.T("Makinelerimin ne yapacağını gösterdiğimi mi sanıyorsun?"), "ai");
            if (B.HasBackup) Say(B, Loc.T("Tek dolap mı? Hayır. Yedeğim de burada."), "ai");
            PlanEnemies(B);
            PlaceSpawns(B);
            B.FxList.Clear();
            return B;
        }

        // ---------- Hasar, itme ----------
        static void Hurt(BState B, Unit u, int n, string why)
        {
            if (u.Dead || n <= 0) return;
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
            else if (IsTarget(o.K)) { Say(B, Loc.F("{0} kırıldı.", Loc.T(ObjNames[o.K])), "good"); CheckWin(B); }
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
            B.OutcomeReason = B.IsCore ? Loc.T("Çekirdek yok edildi.") : Loc.T("YZ düğümü kırıldı.");
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
            var seen = new Dictionary<int, int> { [u.Y * N + u.X] = 0 };
            var q = new Queue<Pt>();
            q.Enqueue(new Pt(u.X, u.Y));
            var o = new List<Pt>();
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                int d = seen[p.Y * N + p.X];
                if (d >= T.Move) continue;
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
            dst.Deceptive = c.Deceptive; dst.IsCore = c.IsCore; dst.HasBackup = c.HasBackup; dst.OutcomeSuccess = c.OutcomeSuccess; dst.Retreated = c.Retreated;
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

        /// <summary>Niyetin vuracağı kare: ateş düz hatta ilk engele, yakın dövüş ve onarım bitişik kareye.</summary>
        static ITarget IntentTarget(BState B, Unit e, string kind, int dir, int px, int py)
        {
            int dx = Dirs[dir][0], dy = Dirs[dir][1];
            if (kind == "shoot")
            {
                for (int k = 1; k < N; k++)
                {
                    int x = px + dx * k, y = py + dy * k;
                    if (!Inb(x, y)) return new ITarget { X = x - dx, Y = y - dy, K = k - 1, None = true };
                    if (CellAt(B, x, y).T == "wall" || CellAt(B, x, y).O != null) return new ITarget { X = x, Y = y, K = k };
                    var u = UnitAt(B, x, y);
                    if (u != null && u.Id != e.Id) return new ITarget { X = x, Y = y, K = k };
                }
                return null;
            }
            int tx = px + dx, ty = py + dy;
            return Inb(tx, ty) ? new ITarget { X = tx, Y = ty, K = 1 } : null;
        }

        static double ScoreTarget(BState B, Unit e, string kind, int dir, int px, int py)
        {
            var t = IntentTarget(B, e, kind, dir, px, py);
            if (t == null || t.None) return 0;
            if (CellAt(B, t.X, t.Y).T == "wall") return 0;
            var u = UnitAt(B, t.X, t.Y);
            // Onarım yalnızca hasarlı hedef dolaba anlamlı; birime yöneltilen onarım hiçbir şey yapmaz
            if (kind == "repair")
            {
                var ro = u == null || u.Id == e.Id ? CellAt(B, t.X, t.Y).O : null;
                return ro != null && IsTarget(ro.K) && ro.Hp < ro.Max ? 14 : -1;
            }
            if (u != null && u.Id != e.Id)
                return u.Side == 'p' ? 10 + (u.Type == "tek" ? 3 : 0) + (u.Hp <= (kind == "melee" ? 2 : 1) ? 4 : 0) : -6;
            var o = CellAt(B, t.X, t.Y).O;
            if (o == null) return 0;
            if (IsTarget(o.K)) return -20;
            if (o.K == "spawner") return -10;
            if (o.K == "civ") return 1;
            if (o.K == "cool")
                return 2 + 4 * Dirs.Count(d => { var v = UnitAt(B, t.X + d[0], t.Y + d[1]); return v != null && v.Side == 'p'; });
            return 0;
        }

        static void PlanEnemies(BState B)
        {
            int order = 0;
            foreach (var e in Alive(B, 'e'))
            {
                var kinds = e.Type == "dron" ? new[] { "shoot" } : new[] { "melee", "repair" };
                var spots = new List<Pt> { new Pt(e.X, e.Y) };
                spots.AddRange(Reachable(B, e));
                var players = Alive(B, 'p');
                var best = new List<(Pt pos, string kind, int dir)>();
                double bestScore = double.NegativeInfinity;
                foreach (var pos in spots)
                {
                    // Konum tercihi: dronlar mesafede durmayı, bakım robotları yaklaşmayı sever
                    int near = players.Count > 0 ? players.Min(p => Man(p.X, p.Y, pos.X, pos.Y)) : 0;
                    double posScore = e.Type == "dron" ? (near <= 1 ? -2 : 0) : -0.2 * near;
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
                var it = new Intent { Kind = pk.kind, Dir = pk.dir, Shown = pk.dir };
                // Aldatma: bazı niyetler yalan gösterilir; Analistin tarama alanındakiler her zaman doğru görünür
                if (B.Deceptive && it.Kind != "repair" && Rnd(B) < 0.35) { it.Fake = true; it.Shown = (it.Dir + 1 + (int)Math.Floor(Rnd(B) * 3)) % 4; }
                e.Intent = it;
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

        /// <summary>Analistin tarama alanındaki düşmanın niyeti kesin görünür.</summary>
        public static bool Scanned(BState B, Unit e) => Alive(B, 'p').Any(p => p.Type == "ana" && Man(p.X, p.Y, e.X, e.Y) <= ScanRadius);

        public sealed class IntentView
        {
            public int Unit, Order, Dir, FromX, FromY, TargetX, TargetY;
            public string Kind;
            public bool Uncertain, IsScanned, HasTarget, TargetNone;
        }

        /// <summary>Arayüz için: her düşmanın gösterilen niyeti (yalan olabilir).</summary>
        public static List<IntentView> Intents(BState B)
        {
            var list = new List<IntentView>();
            foreach (var e in Alive(B, 'e').Where(u => u.Intent != null).OrderBy(u => u.Order))
            {
                bool sc = Scanned(B, e);
                bool uncertain = B.Deceptive && !sc && e.Intent.Kind != "repair";
                int dir = uncertain ? e.Intent.Shown : e.Intent.Dir;
                var t = IntentTarget(B, e, e.Intent.Kind, dir, e.X, e.Y);
                list.Add(new IntentView
                {
                    Unit = e.Id, Order = e.Order, Kind = e.Intent.Kind, Dir = dir, Uncertain = uncertain, IsScanned = sc && B.Deceptive,
                    FromX = e.X, FromY = e.Y, HasTarget = t != null, TargetNone = t != null && t.None, TargetX = t?.X ?? 0, TargetY = t?.Y ?? 0,
                });
            }
            return list;
        }

        static void Lose(BState B, string reason) { B.Phase = "lost"; B.OutcomeSuccess = false; B.OutcomeReason = reason; }

        public static bool EndTurn(BState B)
        {
            if (B.Phase != "player") return false;
            B.FxList.Clear(); B.Hist.Clear();
            Say(B, Loc.F("— Tur {0}: makineler hareket ediyor —", B.Turn), "turn");
            // 1) Niyetler sırayla (itilen robot niyetini yeni yerinden uygular)
            foreach (var e in Alive(B, 'e').OrderBy(u => u.Order).ToList())
            {
                if (e.Dead || e.Intent == null || B.Phase != "player") continue;
                var t = IntentTarget(B, e, e.Intent.Kind, e.Intent.Dir, e.X, e.Y);
                AddFx(B, new Fx { K = "attack", Unit = e.Id, X = t?.X ?? e.X, Y = t?.Y ?? e.Y });
                if (t == null || t.None) continue;
                if (e.Intent.Kind == "repair")
                {
                    var o = CellAt(B, t.X, t.Y).O;
                    if (o != null && IsTarget(o.K) && o.Hp < o.Max) { o.Hp++; AddFx(B, new Fx { K = "heal", X = t.X, Y = t.Y }); Say(B, Loc.F("Bakım robotu {0} onardı.", Loc.T(ObjNames[o.K]).ToLower(UiCulture)), "bad"); }
                }
                else DamageAt(B, t.X, t.Y, e.Type == "dron" ? 1 : 2, e.Type == "dron" ? Loc.T("dron ateşi") : Loc.T("bakım robotu"));
            }
            if (B.Phase != "player") return true;
            // 2) Uyanma: işaretli karede biri duruyorsa uyanma engellenir ama duran hasar alır
            foreach (var s in B.Spawns)
            {
                var u = UnitAt(B, s.X, s.Y);
                if (u != null) { Hurt(B, u, 1, Loc.T("uyanmayı engelledi")); Say(B, Loc.T("Uyanan kopya engellendi."), u.Side == 'p' ? "good" : "sys"); continue; }
                if (CellAt(B, s.X, s.Y).O != null) continue;
                var tp = Rnd(B) < 0.6 ? "dron" : "bakim";
                B.Units.Add(NewUnit(B, tp, s.X, s.Y));
                AddFx(B, new Fx { K = "spawn", X = s.X, Y = s.Y });
                Say(B, Loc.F("Yeni bir {0} uyandı.", UnitTypes[tp].Name.ToLower(Loc.IsSource ? UiCulture : System.Globalization.CultureInfo.InvariantCulture)), "bad");
            }
            B.Spawns = new List<Pt>();
            if (Alive(B, 'p').Count == 0) { Lose(B, Loc.T("Ekibin tamamı yere düştü.")); return true; }
            B.Turn++;
            if (B.Turn > B.MaxTurns)
            {
                Lose(B, B.IsCore ? Loc.T("Süre doldu: YZ çekirdeğini binadan taşıdı.") : Loc.T("Süre doldu: YZ sunucularını yedekledi ve binayı kilitledi."));
                return true;
            }
            // 3) Yeni plan
            foreach (var u in B.Units) if (u.Side == 'p') { u.Moved = false; u.Acted = false; }
            PlanEnemies(B);
            PlaceSpawns(B);
            if (B.Turn == B.MaxTurns) Say(B, Loc.T("Son tur."), "turn");
            return true;
        }

        static readonly System.Globalization.CultureInfo UiCulture = new System.Globalization.CultureInfo("tr-TR");

        public static bool Retreat(BState B)
        {
            if (B.Phase != "player") return false;
            Lose(B, Loc.T("Ekip geri çekildi."));
            B.Retreated = true;
            return true;
        }

        public static RaidResult Result(BState B)
        {
            var left = TargetsLeft(B);
            return new RaidResult
            {
                Success = B.Phase == "won", Retreat = B.Retreated, Collateral = B.Collateral,
                Down = B.Units.Count(u => u.Side == 'p' && u.Dead), Turns = Math.Min(B.Turn, B.MaxTurns),
                TargetWasCore = B.IsCore, BackupDestroyed = !B.HasBackup || !left.Any(t => t.O.K == "backup"),
                Reason = B.OutcomeReason ?? "",
            };
        }

        /// <summary>Parite testleri için durumun özeti (JS'teki baskın parite çıktısıyla aynı biçim).</summary>
        public static string Signature(BState B)
        {
            var objs = new List<string>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { var o = CellAt(B, x, y).O; if (o != null) objs.Add($"{o.K}{x}{y}:{o.Hp}"); }
            return $"{B.Phase}|t{B.Turn}|c{B.Collateral}|rs{B.Rs}|" + string.Join(",", B.Units.Select(u => $"{u.Type}{u.X}{u.Y}:{u.Hp}{(u.Dead ? "x" : "")}")) + "|" + string.Join(",", objs);
        }
    }
}
