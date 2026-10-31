using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Tests
{
    /// <summary>
    /// Denge ölçümü için baskın botu: <see cref="Baskin"/> tahtasını baştan sona oynayan deterministik açgözlü oyuncu.
    /// Her turda uzmanların hamle + yetenek seçeneklerini durumun kopyalarında dener ve kaba bir puanla sıralar (makinelerin
    /// görünen niyetleri ve uyanma kareleri, <see cref="Baskin.Intents"/>); ilk adımın en iyi birkaç seçeneğini açgözlü
    /// tamamlar, bu tur planlarını kopyada gerçek makine evresiyle (<see cref="Baskin.EndTurn"/>) doğrular ve en iyisini oynar.
    /// Puan bir makine evresi sonrasına bakar ve görevin kalan yolunu ekler: sunucu görevinde Teknisyen'den hedefin yanına,
    /// ikmal görevinde Teknisyen'den kasanın yanına ve oradan çıkışa (kasa alındıysa taşıyıcıdan çıkışa) en kısa yol; makinenin
    /// durduğu kare bedelli (gelişmiş akıl hedefin önünü bedeniyle tutar, yolu açmak da ilerlemedir). Tek evre
    /// bakan puan Madenci'nin dinamitini (birkaç tur sonra kısalan yol) görmez ve sisi fazla değerli bulur; yol terimi ve
    /// "yıkılacak duvar" çekimi (<see cref="Weights.Breach"/>) bunu dengeler.
    /// Botun rastgeleliği yok: aynı tahta aynı oyunu verir. Gerçek durum yalnız motorun komutlarıyla (DoMove, DoAct, EndTurn)
    /// değişir; denemeler kopyada yapılır. Kum fırtınasında (gizli uyanma) doğrulama yapılmaz: gerçek evre gizli kareyi gösterirdi.
    /// </summary>
    public sealed class RaidBot
    {
        const int N = Baskin.N;
        /// <summary>Yolu olmayan hedefin uzaklığı.</summary>
        const int Far = 40;

        /// <summary>Puan ağırlıkları. Ölçek: görevin bir turluk ilerlemesi <see cref="Progress"/> puan.</summary>
        public sealed class Weights
        {
            public double Win = 10000, Loss = -10000;
            /// <summary>Görevin kalan turu başına (sürekli tahmin) ve süreye sığmayan tur başına.</summary>
            public double Progress = 45, Late = 60;
            /// <summary>Teknisyen'in canı, son canı ve düşmesi (Teknisyen'siz görev bitmez); öteki uzmanlar.</summary>
            public double TekHp = 8, TekLow = 10, TekDown = 300;
            public double Hp = 4, Low = 4, Down = 45, Plate = 3;
            /// <summary>Ayaktaki makine, canı, zırhı; evrede uyanacak makine; ayaktaki kopya dolabı.</summary>
            public double Enemy = 7, EnemyHp = 3, EnemyArmor = 2, NewEnemy = 9, Spawner = 6;
            /// <summary>Kırılan sivil sunucu; kilitlenen Teknisyen (yolu varsa) ve öteki uzman.</summary>
            public double Civilian = 12, TekLocked = 18, Locked = 3;
            /// <summary>Yıkılacak duvarın kazancının, Madenci yanına vardığında puana geçen payı (yıkınca kazancın tamamı).</summary>
            public double Breach = 0.6;
            /// <summary>Yolda makinenin durduğu karenin fazladan bedeli (adım): makine düşürülmeli ya da itilmeli; gelişmiş akıl hedefin
            /// önünü bedeniyle tutar.</summary>
            public int EnemyStep = 3;
        }

        public Weights W = new Weights();
        /// <summary>İlk adımda denenen seçenek sayısı: her biri açgözlü tamamlanır, gerçek makine evresiyle karşılaştırılır. Ölçümde
        /// (4 tohum, 5760 baskın) 1 → 3 → 6 → 10 seçenek T+E+A'yı %78 → %88 → %92 → %94'e çıkardı; kadrolar arası sıra ve farklar
        /// hemen hiç değişmedi. Puan ağırlıklarının ±%50 değişimi de sırayı değiştirmedi.</summary>
        public int Alternatives = 8;
        /// <summary>İzleme: her turun başında durum ve seçilen plan (plan uygulanmadan önce).</summary>
        public Action<Baskin.BState, List<Step>> OnTurn;

        /// <summary>Baskının sayaçları: yıkılan duvar, robota/nesneye atılan dinamit, sis bombası, sise kesilen niyet.</summary>
        public sealed class Tally { public int Walls, DynamiteHits, Smokes, SmokedShots; }

        /// <summary>Bir uzmanın tur hamlesi: isteğe bağlı yürüyüş, ardından isteğe bağlı yetenek.</summary>
        public struct Step { public int Unit, MX, MY, AX, AY; public bool Move, Act; }

        sealed class Option { public double Pre; public Step Step; public Baskin.BState State; }

        sealed class Line { public double Pre; public Baskin.BState State; public List<Step> Steps; public List<int> Left; }

        /// <summary>Tur bağlamı: Madenci'nin yıkması en çok yol kısaltan duvar ve o duvarın yanına uzaklık alanı.</summary>
        sealed class Context { public bool HasBreach; public int BX, BY, Gain; public int[,] Field; }

        /// <summary>Kaba makine evresi tahmini (birim sırası <c>C.Units</c>): hasar, kırılan plaka, kilit, nesne hasarı.</summary>
        sealed class Forecast
        {
            public int[] Dmg, Plate; public bool[] Locked;
            public readonly int[] ObjDmg = new int[N * N];
            public int NewEnemies, Collateral, Repairs, SpawnersBroken; public bool CrateLost;
        }

        struct Route { public int D1, D2; public int Total => D1 + D2; }

        /// <summary>Baskını sonuna kadar oynar (durum yerinde değişir).</summary>
        public Tally Play(Baskin.BState B)
        {
            var tally = new Tally();
            while (B.Phase == "player")
            {
                var plan = ChooseTurn(B);
                OnTurn?.Invoke(B, plan);
                foreach (var step in plan) Apply(B, step, tally);
                if (B.Phase != "player") break;
                Baskin.EndTurn(B);
                tally.SmokedShots += B.FxList.Count(f => f.K == "smoked");
            }
            return tally;
        }

        static void Apply(Baskin.BState B, Step step, Tally tally)
        {
            if (B.Phase != "player") return;
            if (step.Move && !Baskin.DoMove(B, step.Unit, step.MX, step.MY)) throw new InvalidOperationException("Bot hamlesi geçersiz: yürüyüş");
            if (!step.Act || B.Phase != "player") return;
            var u = UnitById(B, step.Unit);
            string ability = Baskin.UnitTypes[u.Type].Ability;
            bool wall = Baskin.CellAt(B, step.AX, step.AY).T == "wall";
            if (!Baskin.DoAct(B, step.Unit, step.AX, step.AY)) throw new InvalidOperationException("Bot hamlesi geçersiz: yetenek");
            if (ability == "dinamit") { if (wall) tally.Walls++; else tally.DynamiteHits++; }
            else if (ability == "duman") tally.Smokes++;
        }

        // ---------- Tur planı ----------
        List<Step> ChooseTurn(Baskin.BState B)
        {
            var ctx = MakeContext(B);
            var left = Baskin.Alive(B, 'p').Where(u => Baskin.CanAct(B, u)).Select(u => u.Id).ToList();
            var lines = new List<Line>();
            foreach (var first in Expand(B, left, ctx, Math.Max(1, Alternatives)))
            {
                var line = new Line { Pre = first.Pre, State = first.State, Steps = new List<Step> { first.Step }, Left = left.Where(id => id != first.Step.Unit).ToList() };
                while (line.Left.Count > 0 && line.State.Phase == "player")
                {
                    var next = Expand(line.State, line.Left, ctx, 1);
                    if (next.Count == 0) break;
                    line.State = next[0].State; line.Pre = next[0].Pre; line.Steps.Add(next[0].Step); line.Left.Remove(next[0].Step.Unit);
                }
                lines.Add(line);
            }
            if (lines.Count == 0) return new List<Step>();
            // Doğrulama: planın sonundaki durumda makine evresi kopyada gerçekten oynanır (zincirleme etkiler, ölen makinenin
            // boşa giden niyeti); yeni niyetlere bakılmaz. Kum fırtınasında yalnız kaba puan.
            Line pick = null;
            double pickPost = double.NegativeInfinity;
            foreach (var line in lines)
            {
                double post = line.Pre;
                if (line.State.Phase != "player") post = Score(line.State, ctx, false);
                else if (!B.Fog)
                {
                    var v = line.State.Clone();
                    Baskin.EndTurn(v);
                    post = Score(v, ctx, false);
                }
                if (pick == null || post > pickPost + 1e-9 || Math.Abs(post - pickPost) <= 1e-9 && line.Pre > pick.Pre + 1e-9) { pick = line; pickPost = post; }
            }
            return pick.Steps;
        }

        /// <summary>Kalan uzmanların bütün seçenekleri (dur/yürü × yetenek yok/hedef); kaba puana göre en iyi <paramref name="keep"/> tanesi.</summary>
        List<Option> Expand(Baskin.BState S, List<int> left, Context ctx, int keep)
        {
            var top = new List<Option>();
            var smoke = SmokeCells(S);
            void Offer(double pre, Step step, Baskin.BState state)
            {
                // Eşitlikte önce bulunan kalır
                int i = top.Count;
                while (i > 0 && pre > top[i - 1].Pre + 1e-9) i--;
                if (i >= keep) return;
                top.Insert(i, new Option { Pre = pre, Step = step, State = state });
                if (top.Count > keep) top.RemoveAt(keep);
            }
            foreach (int id in left)
            {
                var u = UnitById(S, id);
                if (u == null || !Baskin.CanAct(S, u)) continue;
                var spots = new List<Baskin.Pt> { new Baskin.Pt(u.X, u.Y) };
                if (Baskin.CanMove(S, u)) spots.AddRange(Baskin.Reachable(S, u));
                for (int si = 0; si < spots.Count; si++)
                {
                    var p = spots[si];
                    var M = S;
                    if (si > 0)
                    {
                        M = S.Clone();
                        if (!Baskin.DoMove(M, id, p.X, p.Y)) continue;
                    }
                    var step = new Step { Unit = id, Move = si > 0, MX = p.X, MY = p.Y };
                    Offer(Score(M, ctx, true), step, M);
                    if (M.Phase != "player") continue;
                    var mu = UnitById(M, id);
                    foreach (var t in Useful(M, mu, smoke))
                    {
                        var C = M.Clone();
                        if (!Baskin.DoAct(C, id, t.X, t.Y)) continue;
                        var acted = step;
                        acted.Act = true; acted.AX = t.X; acted.AY = t.Y;
                        Offer(Score(C, ctx, true), acted, C);
                    }
                }
            }
            return top;
        }

        /// <summary>Denemeye değer yetenek hedefleri: EMP yalnız birim ya da nesne olan kareye veya birimin yanına, sis yalnız bir
        /// niyetin geçtiği ya da vurduğu kareye; öteki yeteneklerin hedefleri zaten az.</summary>
        static IEnumerable<Baskin.Pt> Useful(Baskin.BState M, Baskin.Unit u, HashSet<int> smoke)
        {
            string ability = Baskin.UnitTypes[u.Type].Ability;
            foreach (var t in Baskin.Targets(M, u))
            {
                if (ability == "emp" && !EmpMatters(M, t.X, t.Y)) continue;
                if (ability == "duman" && !smoke.Contains(t.Y * N + t.X)) continue;
                yield return t;
            }
        }

        static bool EmpMatters(Baskin.BState M, int x, int y)
        {
            if (Baskin.UnitAt(M, x, y) != null) return true;
            var o = Baskin.CellAt(M, x, y).O;
            if (o != null && !Baskin.IsTarget(o.K)) return true;
            foreach (var d in Baskin.Dirs)
                if (Inb(x + d[0], y + d[1]) && Baskin.UnitAt(M, x + d[0], y + d[1]) != null) return true;
            return false;
        }

        /// <summary>Niyetlerin geçtiği kareler: atış ve kilit hattı ilk duvara ya da nesneye kadar (birimler yer değiştirir, sayılmaz),
        /// yakın vuruşun ve havanın hedefi.</summary>
        static HashSet<int> SmokeCells(Baskin.BState S)
        {
            var cells = new HashSet<int>();
            foreach (var iv in Baskin.Intents(S))
            {
                if (iv.Kind == "shoot" || iv.Kind == "lock")
                {
                    int range = iv.Kind == "lock" ? 4 : N;
                    for (int k = 1; k <= range; k++)
                    {
                        int x = iv.FromX + Baskin.Dirs[iv.Dir][0] * k, y = iv.FromY + Baskin.Dirs[iv.Dir][1] * k;
                        if (!Inb(x, y)) break;
                        cells.Add(y * N + x);
                        if (Baskin.CellAt(S, x, y).T == "wall" || Baskin.CellAt(S, x, y).O != null) break;
                    }
                }
                else if ((iv.Kind == "melee" || iv.Kind == "lob") && iv.HasTarget) cells.Add(iv.TargetY * N + iv.TargetX);
            }
            return cells;
        }

        // ---------- Puan ----------
        /// <param name="before">Makine evresinden önce (niyetlerden kaba tahmin) ya da sonra (gerçek evre oynandı).</param>
        double Score(Baskin.BState C, Context ctx, bool before)
        {
            var f = before && C.Phase == "player" ? Predict(C) : null;
            bool loot = Baskin.IsLoot(C);
            double s = 0;
            Baskin.Unit tek = null, miner = null;
            bool anyAlive = false, carrierDown = false, tekLocked = false;
            for (int i = 0; i < C.Units.Count; i++)
            {
                var u = C.Units[i];
                int hp = u.Dead ? 0 : u.Hp - (f?.Dmg[i] ?? 0);
                if (u.Side != 'p')
                {
                    if (hp > 0) s -= W.Enemy + W.EnemyHp * hp + W.EnemyArmor * u.Armor;
                    continue;
                }
                bool isTek = u.Type == "tek";
                if (hp <= 0)
                {
                    s -= isTek ? W.TekDown : W.Down;
                    if (u.Id == C.Carrier) carrierDown = true;
                    continue;
                }
                anyAlive = true;
                int plate = u.Armor - (f?.Plate[i] ?? 0);
                s += (isTek ? W.TekHp : W.Hp) * hp + W.Plate * plate - (hp == 1 ? isTek ? W.TekLow : W.Low : 0);
                bool locked = f != null ? f.Locked[i] : u.Locked;
                if (isTek) { tek = u; tekLocked = locked; }
                else if (locked) s -= W.Locked;
                if (u.Type == "maden" && u.Charges != 0) miner = u;
            }
            if (f != null) s += W.Spawner * f.SpawnersBroken - W.NewEnemy * f.NewEnemies - W.Civilian * f.Collateral;
            s -= W.Civilian * C.Collateral;
            int targetHp = 0;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var o = C.Cells[y, x].O;
                    if (o == null) continue;
                    if (o.K == "spawner") s -= W.Spawner;
                    else if (Baskin.IsTarget(o.K)) targetHp += o.Hp;
                }
            if (C.Phase == "won") return W.Win + s;
            if (C.Phase == "lost" || !anyAlive || carrierDown || f != null && f.CrateLost || tek == null) return W.Loss + s;
            int turnsLeft = before ? C.MaxTurns - C.Turn : C.MaxTurns - C.Turn + 1;
            if (turnsLeft <= 0) return W.Loss + s;

            var route = RouteOf(C, tek, -1, -1);
            bool carrying = loot && C.Carrier != 0;
            double need;
            int needTurns;
            if (!loot)
            {
                int h = targetHp + (f?.Repairs ?? 0);
                need = route.D1 / 3.0 + h / 2.0;
                needTurns = route.D1 == 0 ? Ceil(h, 2) : Ceil(route.D1, 3) + Ceil(h, 2) - 1;
            }
            else if (!carrying)
            {
                // Kasa açılınca Teknisyen o tur yürüyemez
                need = route.D1 / 3.0 + route.D2 / 3.0 + 0.5;
                needTurns = (route.D1 == 0 ? 1 : Ceil(route.D1, 3)) + Ceil(route.D2, 3);
            }
            else
            {
                need = route.D2 / 3.0;
                needTurns = Ceil(route.D2, 3);
            }
            s -= W.Progress * need + W.Late * Math.Max(0, needTurns - turnsLeft);
            if (tekLocked && (carrying ? route.D2 : route.D1) > 0) s -= W.TekLocked;
            if (ctx.HasBreach && miner != null && C.Cells[ctx.BY, ctx.BX].T == "wall")
            {
                int dm = ctx.Field[miner.Y, miner.X];
                if (dm >= 0) s += W.Progress * ctx.Gain / 3.0 * W.Breach * Math.Max(0, 1 - dm / 6.0);
            }
            return s;
        }

        static int Ceil(int a, int b) => a <= 0 ? 0 : (a + b - 1) / b;

        /// <summary>Görünen niyetlerin ve uyanma karelerinin (kum fırtınasında görünmez) bu evredeki sonucu, sırayla ve kabaca: ölen
        /// birimin karesindeki sonraki atış arkaya geçmez, soğutma ünitesi bir kez patlar.</summary>
        static Forecast Predict(Baskin.BState C)
        {
            int n = C.Units.Count;
            var f = new Forecast { Dmg = new int[n], Plate = new int[n], Locked = new bool[n] };
            bool loot = Baskin.IsLoot(C);
            foreach (var iv in Baskin.Intents(C))
            {
                int ei = IndexOf(C, iv.Unit);
                if (ei < 0 || C.Units[ei].Hp - f.Dmg[ei] <= 0) continue;
                if (!iv.HasTarget || iv.TargetNone) continue;
                int tx = iv.TargetX, ty = iv.TargetY;
                switch (iv.Kind)
                {
                    case "melee": Hit(C, f, tx, ty, 2, loot, true); break;
                    case "blast":
                        foreach (var a in iv.Area) Hit(C, f, a.X, a.Y, 1, loot, true);
                        f.Dmg[ei] = C.Units[ei].Hp;
                        break;
                    case "lock":
                    {
                        int ui = At(C, f, tx, ty);
                        if (ui >= 0 && C.Units[ui].Side == 'p') f.Locked[ui] = true;
                        break;
                    }
                    case "repair":
                    {
                        var o = C.Cells[ty, tx].O;
                        if (o != null && Baskin.IsTarget(o.K) && o.Hp < o.Max) f.Repairs++;
                        break;
                    }
                    case "shield": break;
                    case "clone":
                    {
                        int ui = At(C, f, tx, ty);
                        if (ui >= 0) HitUnit(C, f, ui, 1);
                        else if (C.Cells[ty, tx].T == "floor" && C.Cells[ty, tx].O == null) f.NewEnemies++;
                        break;
                    }
                    default: Hit(C, f, tx, ty, 1, loot, true); break; // atış ve havan
                }
            }
            if (!C.Fog)
                foreach (var p in C.Spawns)
                {
                    int ui = At(C, f, p.X, p.Y);
                    if (ui >= 0) HitUnit(C, f, ui, 1);
                    else if (C.Cells[p.Y, p.X].O == null) f.NewEnemies++;
                }
            return f;
        }

        static void Hit(Baskin.BState C, Forecast f, int x, int y, int n, bool loot, bool chain)
        {
            if (!Inb(x, y)) return;
            int ui = At(C, f, x, y);
            if (ui >= 0) { HitUnit(C, f, ui, n); return; }
            var o = C.Cells[y, x].O;
            int k = y * N + x;
            if (o == null || f.ObjDmg[k] >= o.Hp) return;
            f.ObjDmg[k] += n;
            if (f.ObjDmg[k] < o.Hp) return;
            if (o.K == "civ") f.Collateral++;
            else if (o.K == "spawner") f.SpawnersBroken++;
            else if (o.K == "cool" && chain) foreach (var d in Baskin.Dirs) Hit(C, f, x + d[0], y + d[1], 1, loot, false);
            else if (loot && Baskin.IsTarget(o.K)) f.CrateLost = true;
        }

        static void HitUnit(Baskin.BState C, Forecast f, int i, int n)
        {
            var u = C.Units[i];
            int hp = u.Hp - f.Dmg[i];
            if (hp <= 0) return;
            int a = Math.Min(u.Armor - f.Plate[i], n);
            if (a > 0) { f.Plate[i] += a; n -= a; }
            f.Dmg[i] += Math.Min(n, hp);
        }

        /// <summary>Karede ayakta (tahminde de düşmemiş) birimin sırası; yoksa −1.</summary>
        static int At(Baskin.BState C, Forecast f, int x, int y)
        {
            for (int i = 0; i < C.Units.Count; i++)
            {
                var u = C.Units[i];
                if (!u.Dead && u.X == x && u.Y == y) return u.Hp - f.Dmg[i] > 0 ? i : -1;
            }
            return -1;
        }

        static int IndexOf(Baskin.BState C, int id)
        {
            for (int i = 0; i < C.Units.Count; i++) if (C.Units[i].Id == id) return i;
            return -1;
        }

        static Baskin.Unit UnitById(Baskin.BState S, int id) => S.Units.FirstOrDefault(u => u.Id == id);

        static bool Inb(int x, int y) => x >= 0 && y >= 0 && x < N && y < N;

        // ---------- Yol ----------
        /// <summary>Yürünebilir kare: duvar ve nesne değil (birimlerin bedeli <see cref="Field"/>'da); (ex, ey) duvar olsa da açık sayılır.</summary>
        static bool Open(Baskin.BState S, int x, int y, int ex, int ey) =>
            Inb(x, y) && (x == ex && y == ey || S.Cells[y, x].T != "wall" && S.Cells[y, x].O == null);

        /// <summary>Kaynaklardan uzaklık (adım; ulaşılmaz −1). Makinenin durduğu kare <see cref="Weights.EnemyStep"/> fazla sayılır,
        /// dost birimin içinden geçilir. 64 karede düz Dijkstra.</summary>
        int[,] Field(Baskin.BState S, IEnumerable<Baskin.Pt> sources, int ex, int ey)
        {
            var extra = new int[N * N];
            foreach (var u in S.Units) if (!u.Dead && u.Side == 'e') extra[u.Y * N + u.X] = W.EnemyStep;
            var d = new int[N * N];
            var done = new bool[N * N];
            for (int i = 0; i < d.Length; i++) d[i] = int.MaxValue;
            foreach (var p in sources) d[p.Y * N + p.X] = Math.Min(d[p.Y * N + p.X], extra[p.Y * N + p.X]);
            while (true)
            {
                int k = -1;
                for (int i = 0; i < d.Length; i++) if (!done[i] && d[i] != int.MaxValue && (k < 0 || d[i] < d[k])) k = i;
                if (k < 0) break;
                done[k] = true;
                int x = k % N, y = k / N;
                foreach (var dd in Baskin.Dirs)
                {
                    int nx = x + dd[0], ny = y + dd[1];
                    if (!Open(S, nx, ny, ex, ey)) continue;
                    int n = ny * N + nx, c = d[k] + 1 + extra[n];
                    if (c < d[n]) d[n] = c;
                }
            }
            var field = new int[N, N];
            for (int i = 0; i < d.Length; i++) field[i / N, i % N] = d[i] == int.MaxValue ? -1 : d[i];
            return field;
        }

        /// <summary>Görevin kalan yolu: D1 Teknisyen → hedefin (kasanın) yanı, D2 kasanın yanı → çıkış; kasa alındıysa D1 0 ve D2
        /// taşıyıcı → çıkış. (ex, ey): yıkılmış sayılan duvar.</summary>
        Route RouteOf(Baskin.BState S, Baskin.Unit tek, int ex, int ey)
        {
            bool loot = Baskin.IsLoot(S);
            var carrier = loot && S.Carrier != 0 ? UnitById(S, S.Carrier) : null;
            var exits = loot ? Field(S, S.Exits, ex, ey) : null;
            if (carrier != null)
            {
                int d = carrier.Dead ? -1 : exits[carrier.Y, carrier.X];
                return new Route { D1 = 0, D2 = d < 0 ? Far : d };
            }
            var fromTek = Field(S, new[] { new Baskin.Pt(tek.X, tek.Y) }, ex, ey);
            var best = new Route { D1 = Far, D2 = loot ? Far : 0 };
            foreach (var t in Baskin.TargetsLeft(S))
                foreach (var dd in Baskin.Dirs)
                {
                    int ax = t.X + dd[0], ay = t.Y + dd[1];
                    if (!Inb(ax, ay) || fromTek[ay, ax] < 0) continue;
                    int d2 = loot ? exits[ay, ax] : 0;
                    if (d2 < 0) continue;
                    if (fromTek[ay, ax] + d2 < best.Total) best = new Route { D1 = fromTek[ay, ax], D2 = d2 };
                }
            return best;
        }

        /// <summary>Turun başında: hak kalan Madenci varsa, yıkılınca görevin yolunu en çok (en az iki adım) kısaltan duvar.</summary>
        Context MakeContext(Baskin.BState B)
        {
            var ctx = new Context();
            var players = Baskin.Alive(B, 'p');
            var tek = players.FirstOrDefault(u => u.Type == "tek");
            if (tek == null || !players.Any(u => u.Type == "maden" && u.Charges != 0)) return ctx;
            int now = RouteOf(B, tek, -1, -1).Total, gain = 1;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    if (B.Cells[y, x].T != "wall" || !Baskin.Dirs.Any(d => Open(B, x + d[0], y + d[1], -1, -1))) continue;
                    int g = now - RouteOf(B, tek, x, y).Total;
                    if (g > gain) { gain = g; ctx.HasBreach = true; ctx.BX = x; ctx.BY = y; ctx.Gain = g; }
                }
            if (!ctx.HasBreach) return ctx;
            var sides = Baskin.Dirs.Select(d => new Baskin.Pt(ctx.BX + d[0], ctx.BY + d[1])).Where(p => Open(B, p.X, p.Y, -1, -1)).ToList();
            ctx.Field = Field(B, sides, -1, -1);
            return ctx;
        }
    }
}
