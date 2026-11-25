using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis aşama 10: önlem defterinin tahtadaki kuralları (Docs/TESIS-BASKINLARI-TASARIMI.md §6.3). Sayım (<see cref="CountTally"/>),
    /// altı önlem (cıvatalı ayak, çift cidar, kesici kol, kısa bağ, koku yayıcı, parazit ve makine aklı) ve beş cevabın etkisi; kademeler
    /// ve cevaplar emirden <see cref="MoveRules"/>'a iner (<see cref="MoveRules.From(Balance, RaidParams)"/>). Hepsinin varsayılanı 0/false:
    /// örnek tahtada (Moves null) ve önlemsiz emirde hiçbiri işlemez. Kurallar RNG çekmez; kaplama (çift cidar ve yayıcının yeri) RNG'siz
    /// ve tohumdan bağımsızdır. Bot ve sunum aynı public yardımcıları okur (kopya kural taşımaz).
    /// </summary>
    public static partial class Baskin
    {
        // ---------- Sayım ----------
        /// <summary>Sınıfın sayacını artırır: yalnız <see cref="MoveRules.Tally"/> iken ve sınıfın emirdeki kademesi 2'den küçükken.</summary>
        static void CountTally(BState B, string cls, int n = 1)
        {
            int i = FacilityCounters.Index(cls);
            if (B?.Moves == null || !B.Moves.Tally || i < 0 || n <= 0 || B.Moves.Level(i) >= 2) return;
            switch (i)
            {
                case 0: B.TallyPush += n; break;
                case 1: B.TallyBreach += n; break;
                case 2: B.TallyBuild += n; break;
                case 3: B.TallyCapture += n; break;
                case 4: B.TallyDog += n; break;
                case 5: B.TallyGadget += n; break;
            }
        }

        // ---------- itme · Cıvatalı ayak ----------
        /// <summary>
        /// Cıvatalı makine (kademe 1: imza makineleri, kademe 2: bakım robotu da): itilmez, kanca ve vinç çekmez, yağda kaymaz. Ekip
        /// tarafındaki (tutsak) cıvatasızdır; bırakılınca, sökülmediyse yeniden cıvatalı. Örnek tahtada hep false.
        /// </summary>
        public static bool Bolted(BState B, Unit u) =>
            B?.Moves != null && B.Moves.Bolt >= 1 && u != null && u.Side == 'e' && !u.Unbolted
            && (FacilityCounters.SignatureMachine(u.Type) || B.Moves.Bolt >= 2 && u.Type == "bakim");

        /// <summary>Cıvatalı makine itmeye ya da çekişe direndi. Fx "bolted": X/Y kare, Unit makine.</summary>
        static void BoltHold(BState B, Unit u)
        {
            AddFx(B, new Fx { K = "bolted", X = u.X, Y = u.Y, Unit = u.Id });
            Say(B, Loc.F("{0} cıvatalı: yerinden oynamadı.", UnitTypes[u.Type].Name), "bad");
        }

        /// <summary>Söküm anahtarı (cevap): Teknisyen'in sabotajı ayaktaki cıvatalı makinenin cıvatasını söker (aynı vuruşta iter). Fx
        /// "unbolt": X/Y kare, Unit makine.</summary>
        static void Unbolt(BState B, Unit t)
        {
            if (t.Dead || B.Moves == null || !B.Moves.Unbolt || !Bolted(B, t)) return;
            t.Unbolted = true;
            AddFx(B, new Fx { K = "unbolt", X = t.X, Y = t.Y, Unit = t.Id });
            Say(B, Loc.F("{0}: cıvatası söküldü.", UnitTypes[t.Type].Name), "good");
        }

        // ---------- yikim · Çift cidar ----------
        /// <summary>Kare çift cidarlı duvar mı (lokum tek hamlede <see cref="DoubleWallCost"/> hak ister).</summary>
        public static bool IsDoubleWall(BState B, int x, int y) => B != null && Inb(x, y) && CellAt(B, x, y).T == "wall" && CellAt(B, x, y).Double;

        /// <summary>Çift cidarın lokum bedeli: Delici şarjla (cevap) <see cref="MoveRules.PiercerCharges"/>, değilse
        /// <see cref="MoveRules.DoubleWallCharges"/>.</summary>
        public static int DoubleWallCost(BState B)
        {
            var m = B?.Moves ?? MoveRules.From(null);
            return m.Piercer ? m.PiercerCharges : m.DoubleWallCharges;
        }

        // ---------- orme · Kesici kol ----------
        /// <summary>
        /// Makine niyetinin hasarı (<see cref="ApplyIntent"/>: yakın vuruş, havan, patlama, atış): karede birim yoksa ve nesne tel örgüyse
        /// (kesici kol 1. kademe) ya da kalas duvarsa (2. kademe) hasar + <see cref="MoveRules.CutterDamage"/>. Fx "cut": X/Y kare, Obj nesne,
        /// N ek hasar. Önlemsiz tahtada <see cref="DamageAt"/>'in aynısı.
        /// </summary>
        static void MachineDamageAt(BState B, int x, int y, int n, string why)
        {
            if (Inb(x, y) && B.Moves != null && B.Moves.Cutter >= 1 && B.Moves.CutterDamage > 0 && UnitAt(B, x, y) == null
                && CellAt(B, x, y).O is Obj o && (o.K == "tel" || o.K == "kalas" && B.Moves.Cutter >= 2) && (B.Ev == null || !B.InEvent))
            {
                n += B.Moves.CutterDamage;
                AddFx(B, new Fx { K = "cut", X = x, Y = y, Obj = o.K, N = B.Moves.CutterDamage });
                if (o.Hp <= n) Say(B, Loc.F("Kesici kol {0} kesti.", LowerName(Loc.T(ObjNames[o.K]))), "bad");
            }
            DamageAt(B, x, y, n, why);
        }

        // ---------- ele · Kısa bağ ----------
        /// <summary>Kelepçenin vereceği oyuncu turu: <see cref="MoveRules.ClampTurns"/> − kısa bağ (1. kademeden) + Yedek röle (cevap), en az 1.</summary>
        public static int ClampTurnsFor(BState B, Unit u)
        {
            var m = B?.Moves ?? MoveRules.From(null);
            return Math.Max(1, m.ClampTurns - (m.ShortClamp >= 1 ? m.ShortClampTurns : 0) + m.RelayTurns);
        }

        // ---------- aygit · Parazit ----------
        /// <summary>Kurulacak aygıtın çalışma turu: <see cref="MoveRules.DeviceTurns"/> − parazit (1. kademeden) + Çift zemberek (cevap), en az 1.</summary>
        public static int DeviceTurnsFor(BState B, Unit u)
        {
            var m = B?.Moves ?? MoveRules.From(null);
            int bonus = TitleOn(B, u, FacilityTitle.Builder) ? m.TitleDeviceBonus : 0;
            return Math.Max(1, m.DeviceTurns - (m.Jam >= 1 ? m.JamTurns : 0) + m.SpringTurns + bonus);
        }

        // ---------- aygit · Makine aklı ----------
        /// <summary>
        /// Makine aklının (aygıt 2. kademe) kaçındığı kareler: ayaktaki uzmanların kurulu pusu hatları (<see cref="AmbushLine"/>) ve
        /// ayaktaki taretlerin hattı (<see cref="RunTurret"/>'in kuralı, birimler sayılmadan), birim sırasıyla, tekrarsız. Gizli bilgi
        /// değildir: oyuncunun kurduğu, görünen hatlar. Örnek tahtada boş.
        /// </summary>
        public static List<Pt> DangerTiles(BState B)
        {
            var o = new List<Pt>();
            if (B?.Room == null || B.Moves == null) return o;
            var seen = new HashSet<int>();
            void Add(Pt p) { if (seen.Add(Key(p))) o.Add(p); }
            foreach (var u in B.Units)
            {
                if (!OnBoard(u)) continue;
                if (IsSpecialist(u) && u.AmbushDir >= 0) foreach (var p in AmbushLine(B, u)) Add(p);
                if (IsGadget(u) && u.Type == "taret" && u.Dir >= 0)
                {
                    var d = Dirs[u.Dir];
                    for (int k = 1; k <= B.Moves.TurretRange; k++)
                    {
                        int x = u.X + d[0] * k, y = u.Y + d[1] * k;
                        if (!Inb(x, y) || CellAt(B, x, y).T == "wall" || StopsShot(CellAt(B, x, y).O) || InCloud(B, x, y)) break;
                        Add(new Pt(x, y));
                    }
                }
            }
            return o;
        }

        // ---------- kopek · Koku yayıcı ----------
        /// <summary>Köpeğin erimde (Manhattan ≤ <see cref="MoveRules.EmitterRange"/>) ayakta koku yayıcısı var mı: havla, getir ve kokla
        /// verilmez (yürüyüş serbest). Köpek değilse ya da örnek tahtada false.</summary>
        public static bool EmitterBlocks(BState B, Unit u)
        {
            if (B?.Moves == null || !IsDog(u)) return false;
            int r = B.Moves.EmitterRange;
            for (int y = Math.Max(0, u.Y - r); y <= Math.Min(N - 1, u.Y + r); y++)
                for (int x = Math.Max(0, u.X - r); x <= Math.Min(N - 1, u.X + r); x++)
                    if (Man(x, y, u.X, u.Y) <= r && CellAt(B, x, y).O?.K == "yayici") return true;
            return false;
        }

        // ---------- Kaplama: çift cidar ve koku yayıcı ----------
        /// <summary>
        /// Önlemlerin yapısal kaplaması (oda üretimine girmez, RNG'siz; Digest değişmez): çift cidar (kademe 1'de giriş ve ara oda, 2'de
        /// hedef oda da; <see cref="Balance.facilityDoubleWalls"/> duvar) ve koku yayıcı (her odada kademe kadar). Bina planı kopyası
        /// (<c>Simulation.FacilityPlanCells</c>) bunu çağırır: plan = emirdeki tahta. <paramref name="events"/>: odada şart kurulu (parça
        /// kareleri dışlanır). <paramref name="counters"/>: kademeler (<see cref="FacilityCounters.Classes"/> sırası; eksikse önlem yok).
        /// </summary>
        public static void ApplyCounterCells(Cell[,] cells, FacilityRoom room, bool events, IList<int> counters, Balance b)
        {
            b = b ?? new Balance();
            int Lv(int i) => counters != null && i < counters.Count ? Math.Max(0, Math.Min(2, counters[i])) : 0;
            ApplyCounterCells(cells, room, events, Lv(FacilityCounters.Index(FacilityCounters.Breach)), Lv(FacilityCounters.Index(FacilityCounters.Dog)),
                Math.Max(0, b.facilityDoubleWalls), Math.Max(1, b.facilityEmitterHp));
        }

        /// <summary>Kaplamanın gövdesi (oda tahtası <see cref="RoomBoard"/> emirdeki <see cref="MoveRules"/> değerleriyle çağırır).</summary>
        static void ApplyCounterCells(Cell[,] cells, FacilityRoom room, bool events, int wallLevel, int emitterLevel, int walls, int emitterHp)
        {
            if (cells == null || room == null) return;
            foreach (var t in DoubleWallTiles(cells, room, events, wallLevel, walls)) cells[t.Y, t.X].Double = true;
            foreach (var t in EmitterTiles(cells, room, events, emitterLevel)) cells[t.Y, t.X].O = NewObj("yayici", emitterHp);
        }

        /// <summary>Odanın şart parçası kareleri (şart kuruluysa; kadro ve makine de bu karelerde başlamaz).</summary>
        static HashSet<int> CounterExcluded(FacilityRoom room, bool events) =>
            events && room.Event != null ? new HashSet<int>(room.PartTiles(3)) : new HashSet<int>();

        /// <summary>
        /// Çift cidarın seçilen duvarları (y, x değil, puan sırasıyla): kademe 1'de giriş ve ara oda, 2'de hedef oda da. Aday: lokumun
        /// hedefi olabilecek duvar (kepenk, sandık nişi, raf ve şart parçası değil), en az iki zemin komşusu. Puan: duvar zemin olsaydı
        /// batı karelerinden ileri geçide (hedef odada hedeflerin bir dik komşusuna) en kısa yürüyüşün kısalması; büyükten küçüğe,
        /// eşitlikte odağa yakın, sonra y, x. İlk <paramref name="count"/> seçilir. Hücreleri değiştirmez.
        /// </summary>
        internal static List<Pt> DoubleWallTiles(Cell[,] cells, FacilityRoom room, bool events, int level, int count)
        {
            var o = new List<Pt>();
            if (level <= 0 || count <= 0 || room.Type == RoomType.Target && level < 2) return o;
            var excluded = CounterExcluded(room, events);
            const int Far = 4 * N * N;
            int Route(int wx, int wy)
            {
                var dist = new int[N, N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) dist[y, x] = -1;
                bool Open(int x, int y) => Inb(x, y) && (x == wx && y == wy || cells[y, x].T != "wall" && cells[y, x].O == null);
                bool Goal(int x, int y) => room.Type == RoomType.Target
                    ? Dirs.Any(d => Inb(x + d[0], y + d[1]) && cells[y + d[1], x + d[0]].O is Obj g && IsTarget(g.K))
                    : room.Forward.Any(f => f.X == x && f.Y == y);
                var q = new Queue<Pt>();
                foreach (var w in room.West) if (Open(w.X, w.Y) && dist[w.Y, w.X] < 0) { dist[w.Y, w.X] = 0; q.Enqueue(w); }
                while (q.Count > 0)
                {
                    var p = q.Dequeue();
                    if (Goal(p.X, p.Y)) return dist[p.Y, p.X];
                    foreach (var d in Dirs)
                    {
                        int x = p.X + d[0], y = p.Y + d[1];
                        if (!Open(x, y) || dist[y, x] >= 0) continue;
                        dist[y, x] = dist[p.Y, p.X] + 1;
                        q.Enqueue(new Pt(x, y));
                    }
                }
                return Far;
            }
            int baseRoute = Route(-1, -1);
            var cand = new List<(Pt T, int Gain, int Focus)>();
            foreach (var t in AllTiles())
            {
                var c = cells[t.Y, t.X];
                if (c.T != "wall" || c.Shutter || c.Crate || c.Shelf || excluded.Contains(Key(t))) continue;
                if (Dirs.Count(d => Inb(t.X + d[0], t.Y + d[1]) && cells[t.Y + d[1], t.X + d[0]].T != "wall") < 2) continue;
                cand.Add((t, baseRoute - Route(t.X, t.Y), Dist(t, room.Focus)));
            }
            // AllTiles y, x sırasıyla: kararlı sıralama eşitlikte y, x'i korur
            foreach (var c in cand.OrderByDescending(c => c.Gain).ThenBy(c => c.Focus).Take(count)) o.Add(c.T);
            return o;
        }

        /// <summary>
        /// Koku yayıcıların kareleri (her odada <paramref name="level"/> kadar, en çok 2), konuş sırasıyla. Aday: boş zemin (kablo değil),
        /// kapı, menfez, ayrılmış kare, şart parçası, olay bayraklı kare ve kopya dolabının dik komşusu (sürüm 10) değil; batı karelerine Manhattan ≥ 3; konunca batıdan ulaşılan
        /// hiçbir kare kopmaz ve ulaşılabilen hedef ulaşılabilir kalır. Birinci odağa en yakın; ikinci ileri geçide en yakın (hedef odada
        /// birinciden en uzak); eşitlikte y, x. Hücreleri değiştirmez (kopya üzerinde sınar).
        /// </summary>
        internal static List<Pt> EmitterTiles(Cell[,] cells, FacilityRoom room, bool events, int level)
        {
            var o = new List<Pt>();
            int want = Math.Min(2, level);
            if (want <= 0) return o;
            var excluded = CounterExcluded(room, events);
            var reserved = new HashSet<int>(room.Reserved.Select(Key));
            var work = new Cell[N, N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) work[y, x] = cells[y, x].Clone();
            for (int k = 0; k < want; k++)
            {
                var before = Reach(work, room);
                Pt? best = null;
                int bestScore = int.MaxValue;
                foreach (var t in AllTiles())
                {
                    var c = work[t.Y, t.X];
                    if (c.T != "floor" || c.O != null || c.Door != 0 || c.Vent != 0 || c.Shutter || c.Crate || c.Shelf) continue;
                    if (c.Oil || c.Gas || c.Cloud || c.Crack || c.Phase != 0 || excluded.Contains(Key(t)) || reserved.Contains(Key(t))) continue;
                    if (room.West.Any(w => Dist(w, t) < 3)) continue;
                    // Sürüm 10: kopya dolabının dibine konmaz (dar odada dolabın tek uyanma karesini kapatıyordu; çekim sayısı değişirdi)
                    if (Dirs.Any(d => Inb(t.X + d[0], t.Y + d[1]) && work[t.Y + d[1], t.X + d[0]].O?.K == "spawner")) continue;
                    int score = k == 0 ? Dist(t, room.Focus)
                        : room.Type == RoomType.Target ? -Dist(t, o[0])
                        : room.Forward.Count > 0 ? room.Forward.Min(f => Dist(f, t)) : Dist(t, room.Focus);
                    if (best != null && score >= bestScore) continue;
                    c.O = NewObj("yayici", 1);
                    bool ok = ReachKept(work, room, before);
                    c.O = null;
                    if (!ok) continue;
                    best = t;
                    bestScore = score;
                }
                if (best == null) break;
                o.Add(best.Value);
                work[best.Value.Y, best.Value.X].O = NewObj("yayici", 1);
            }
            return o;
        }

        /// <summary>Batı karelerinden yürünerek ulaşılan kareler (duvar ve nesne geçilmez, birimler sayılmaz).</summary>
        static bool[,] Reach(Cell[,] cells, FacilityRoom room)
        {
            var seen = new bool[N, N];
            bool Open(int x, int y) => Inb(x, y) && cells[y, x].T != "wall" && cells[y, x].O == null;
            var q = new Queue<Pt>();
            foreach (var w in room.West) if (Open(w.X, w.Y) && !seen[w.Y, w.X]) { seen[w.Y, w.X] = true; q.Enqueue(w); }
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var d in Dirs)
                {
                    int x = p.X + d[0], y = p.Y + d[1];
                    if (!Open(x, y) || seen[y, x]) continue;
                    seen[y, x] = true;
                    q.Enqueue(new Pt(x, y));
                }
            }
            return seen;
        }

        /// <summary>Önceden ulaşılan ve hâlâ açık her kare ulaşılır kalıyor, ulaşılan bir dik komşusu olan hedef onu koruyor mu
        /// (<see cref="ExitSafe"/> kalıbı, plan hücrelerinde).</summary>
        static bool ReachKept(Cell[,] cells, FacilityRoom room, bool[,] before)
        {
            var after = Reach(cells, room);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    if (before[y, x] && cells[y, x].T != "wall" && cells[y, x].O == null && !after[y, x]) return false;
                    if (!(cells[y, x].O is Obj g) || !IsTarget(g.K)) continue;
                    bool Had(bool[,] r) => Dirs.Any(d => Inb(x + d[0], y + d[1]) && r[y + d[1], x + d[0]]);
                    if (Had(before) && !Had(after)) return false;
                }
            return true;
        }
    }
}
