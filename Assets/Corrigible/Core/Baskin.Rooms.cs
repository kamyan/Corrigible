using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis odaları (Docs/TESIS-BASKINLARI-TASARIMI.md): giydirilmiş taslaktan odanın planı (<see cref="BuildRoom"/>: plan tahtası,
    /// bölge özelliği, A odasında kepenk ve menfez, şart, son denetim) ve planın taktik tahtası (<see cref="CreateRoom"/>). Plan
    /// kendi akışlarından üretilir (<see cref="FacilityMaps"/>); oda kurulumu yalnız oda tohumundan çeker. Şart kurulumu
    /// <see cref="EventSetup"/>'ı değiştirmeden kullanır: ayrılmış karelere geçici bir maske konur, A odasında donanım kurulum
    /// süresince hedef dolap sayılır.
    /// </summary>
    public static partial class Baskin
    {
        /// <summary>Giydirilmiş taslak ve odanın kimliği (<see cref="FacilityMaps"/> kurar, <see cref="BuildRoom"/> okur).</summary>
        internal sealed class RoomDraft
        {
            public string Id, Region, SectorId, MapKind, Device, Event, Mission;
            public int Wing, Index;
            public RoomType Type;
            public List<string> Traits = new List<string>();
            public string[] Rows;
            public HashSet<int> Inner = new HashSet<int>();
            /// <summary>Sürüm 10: satırlar ve iç kareler bu dönüşle döndürülmüştür (<see cref="FacilityRoom.Turn"/>).</summary>
            public int Turn;
        }

        /// <summary>Sürüm 10: karenin kanonik (giriş batıda) x'i; tesis odası döndürülmüş olabilir, örnek tahtada gerçek x.</summary>
        internal static int CanonX(BState B, Pt t) => CanonX(B?.Room ?? B?.PlanRoom, t);
        static int CanonX(FacilityRoom room, Pt t) => room == null ? t.X : room.ToCanon(t).X;

        /// <summary>Şart kurulumunda ayrılmış karelere konan geçici nesne (kurulumdan sonra kalkar).</summary>
        const string MaskKind = "_mask";

        /// <summary>Tesis odasının donanımı (giriş ve ara oda): sigorta panosu, çağrı anteni, soğutma vanası.</summary>
        public static bool IsDevice(string k) => k == "pano" || k == "anten" || k == "vana";

        /// <summary>Taslak harfinin nesnesi (yer tutucu canlarla; oda kurulurken canlar dengeden yeniden yazılır).</summary>
        static Obj DraftObj(char ch)
        {
            switch (ch)
            {
                case 'C': return NewObj("node", 4);
                case 'P': return NewObj("pano", 1);
                case 'R': return NewObj("anten", 1);
                case 'V': return NewObj("vana", 1);
                case 'S': return NewObj("civ", 1);
                case 'K': return NewObj("cool", 1);
                case 'U': return NewObj("spawner", 2);
                default: return null;
            }
        }

        /// <summary>Hücrenin döküm harfi (<see cref="FacilityRoom.Rows"/>): kepenk, duvar, kablo, geçit, menfez, nesne, zemin sırasıyla.</summary>
        static char RowChar(Cell c)
        {
            if (c.Shutter) return '=';
            if (c.T == "wall") return '#';
            if (c.T == "cable") return '~';
            if (c.Door == 1) return 'E';
            if (c.Door == 2) return 'F';
            if (c.Vent != 0) return 'o';
            if (c.O == null) return '.';
            switch (c.O.K)
            {
                case "node": case "core": return 'C';
                case "pano": return 'P';
                case "anten": return 'R';
                case "vana": return 'V';
                case "civ": return 'S';
                case "cool": return 'K';
                case "spawner": return 'U';
                default: return '?';
            }
        }

        static Cell[,] CloneCells(Cell[,] src)
        {
            var o = new Cell[N, N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) o[y, x] = src[y, x].Clone();
            return o;
        }

        static string[] DumpRows(Cell[,] cells) =>
            Enumerable.Range(0, N).Select(y => new string(Enumerable.Range(0, N).Select(x => RowChar(cells[y, x])).ToArray())).ToArray();

        // ---------- Plan ----------
        /// <summary>
        /// Odanın planı: taslaktan hücreler, bölge özelliği (serpiştirme, yapı kurallarını bozmadan), A odasında donanım erişimi,
        /// kepenk ve menfez, yapı denetimi, şart (iki kademe) ve son denetim. Olmazsa null ve <paramref name="why"/> kısa ret kodu.
        /// </summary>
        internal static FacilityRoom BuildRoom(RoomDraft d, int traitSeed, Rng structure, out string why)
        {
            why = null;
            var cells = new Cell[N, N];
            var west = new List<Pt>();
            var forward = new List<Pt>();
            Pt? focus = null;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    char ch = d.Rows[y][x];
                    var c = new Cell { T = ch == '#' ? "wall" : ch == '~' ? "cable" : "floor", O = DraftObj(ch) };
                    if (ch == 'E') { c.Door = 1; west.Add(new Pt(x, y)); }
                    else if (ch == 'F') { c.Door = 2; forward.Add(new Pt(x, y)); }
                    if (c.O != null && (IsTarget(c.O.K) || IsDevice(c.O.K))) focus = new Pt(x, y);
                    cells[y, x] = c;
                }
            if (focus == null) { why = "odak"; return null; }
            var room = new FacilityRoom
            {
                Id = d.Id, Region = d.Region, SectorId = d.SectorId, Wing = d.Wing, Index = d.Index, Type = d.Type, Mission = d.Mission,
                MapKind = d.MapKind, Device = d.Device, Event = d.Event, West = west, Forward = forward, Focus = focus.Value,
                DeviceAt = d.Type != RoomType.Target ? focus : null, TargetAt = d.Type == RoomType.Target ? focus : null,
                DraftRows = d.Rows, Inner = d.Inner ?? new HashSet<int>(), Turn = d.Turn,
            };
            var P = new BState
            {
                Params = new RaidParams { Tier = 3, Event = d.Event, Traits = d.Traits, CivilianHp = 1, MissionKind = d.Mission, Region = d.Region, SectorId = d.SectorId },
                Rs = traitSeed, Cells = cells, Exits = new List<Pt>(west), PlanRoom = room,
            };
            // Serpiştirme: geçit karelerine ve ileri geçidin yanına parça düşmez; her parça yapı kurallarını korur (kepenk ve menfez
            // henüz yok). "acik" dalı duvar yıkar, yolu yalnız açar.
            ApplyRegionTraits(P, P.Params, west,
                allow: pt => CellAt(P, pt.X, pt.Y).Door == 0 && forward.All(f => Man(f.X, f.Y, pt.X, pt.Y) >= 2),
                ok: b => StructureWhy(b, room) == null);
            if (d.Type == RoomType.Middle)
            {
                if (!PlaceShutter(P, room, structure)) { why = "kepenk"; return null; }
                // Donanım erişimi kepenkten sonra: bölme kepenginin duvarı donanımın bir komşusunu kapatmış olabilir
                var access = Dirs.Select(dd => new Pt(room.Focus.X + dd[0], room.Focus.Y + dd[1])).Where(t => Passable(P, t.X, t.Y)).ToList();
                if (access.Count == 0) { why = "odak"; return null; }
                if (access.Count == 1) room.DeviceAccess = access[0];
                if (!PlaceVents(P, room, structure)) { why = "menfez"; return null; }
            }
            var reserved = new List<Pt>(room.Forward);
            reserved.AddRange(room.Vents);
            if (room.DeviceAccess is Pt da) reserved.Add(da);
            reserved.AddRange(room.ShutterDoors);
            room.Reserved = reserved.GroupBy(Key).Select(g => g.First()).OrderBy(t => t.Y).ThenBy(t => t.X).ToList();
            why = FinalWhy(P.Cells, room, null, new HashSet<int>());
            if (why != null) return null;
            room.BaseCells = CloneCells(P.Cells);
            room.Rows = DumpRows(room.BaseCells);
            if (d.Event != null)
            {
                var v3 = SetupVariant(P, room, 3);
                if (v3.Ev == null) { why = "sart"; return null; }
                var v1 = v3;
                if (d.Event == "tavan" || d.Event == "tup")
                {
                    v1 = SetupVariant(P, room, 1);
                    if (v1.Ev == null || !LowSubsetOfHigh(d.Event, v1, v3)) { why = "sart-kademe"; return null; }
                }
                room.Cells3 = v3.Cells; room.Ev3 = v3.Ev;
                room.Cells1 = v1.Cells; room.Ev1 = v1.Ev;
                room.Parts = ExtractParts(d.Event, v1, v3);
            }
            PlaceBigCrate(room);
            why = RecheckRoom(room);
            return why == null ? room : null;
        }

        /// <summary>
        /// Büyük sandık (aşama 6, belge §4.5): ikmal kanadının hedef odasında bir duvar nişi (<see cref="Cell.Crate"/>; T = "wall"
        /// kalır, satırlar ve özet değişmez). Aday: üç hâlde de duvar, kepenk ve raf rayı değil; sekiz komşusundan en az ikisi iş
        /// karesi (üç hâlde de geçilebilir, geçit ve menfez değil, kademe 3 parçası değil, her hâlde batı girişine bağlı). Hedef dolaba
        /// Manhattan uzaklığı, sonra y, sonra x küçük olan seçilir; RNG yok. Aday yoksa sandık yok.
        /// </summary>
        static void PlaceBigCrate(FacilityRoom room)
        {
            if (room.Type != RoomType.Target || room.Mission != "loot" || room.TargetAt == null || room.BaseCells == null) return;
            var states = new List<Cell[,]> { room.BaseCells };
            if (room.Cells1 != null) states.Add(room.Cells1);
            if (room.Cells3 != null && room.Cells3 != room.Cells1) states.Add(room.Cells3);
            var rail = new HashSet<int>();
            foreach (var ev in new[] { room.Ev1, room.Ev3 })
                if (room.Event == "raf" && ev?.Area != null) foreach (var t in ev.Area) rail.Add(Key(t));
            var parts = room.Event != null ? new HashSet<int>(room.PartTiles(3)) : new HashSet<int>();
            var steps = states.Select(c => ExitSteps(new BState { Cells = c, Exits = new List<Pt>(room.West) })).ToList();
            bool Work(int x, int y)
            {
                if (!Inb(x, y) || parts.Contains(y * N + x)) return false;
                for (int i = 0; i < states.Count; i++)
                {
                    var c = states[i][y, x];
                    if (c.T == "wall" || c.O != null || c.Door != 0 || c.Vent != 0 || steps[i][y, x] < 0) return false;
                }
                return true;
            }
            var focus = room.TargetAt.Value;
            Pt? best = null;
            foreach (var t in AllTiles())
            {
                if (rail.Contains(Key(t)) || states.Any(c => c[t.Y, t.X].T != "wall" || c[t.Y, t.X].Shutter)) continue;
                int work = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if ((dx != 0 || dy != 0) && Work(t.X + dx, t.Y + dy)) work++;
                if (work < 2) continue;
                if (best == null || Man(t.X, t.Y, focus.X, focus.Y) < Man(best.Value.X, best.Value.Y, focus.X, focus.Y)) best = t;
            }
            if (best == null) return;
            room.BigCrate = best;
            foreach (var c in states) c[best.Value.Y, best.Value.X].Crate = true;
        }

        /// <summary>
        /// Ağır kepenk (A odası, yapı akışından 1 çekim): x 1–6'da var olan bir duvar; odağa ve geçit karelerine bitişik değil,
        /// karşılıklı iki yanı geçilebilir ve girişe bağlı, açılınca ileri geçide yolu en az <see cref="FacilityMaps.ShutterSaving"/>
        /// adım kısaltır.
        /// </summary>
        static bool PlaceShutter(BState P, FacilityRoom room, Rng structure)
        {
            var s0 = ExitSteps(P);
            int Near(int[,] s) => room.Forward.Select(f => s[f.Y, f.X]).Where(v => v >= 0).DefaultIfEmpty(-1).Min();
            int d0 = Near(s0);
            if (d0 < 0) return false;
            var doors = AllTiles().Where(t => CellAt(P, t.X, t.Y).Door != 0).ToList();
            bool Open(int x, int y) => Passable(P, x, y) && s0[y, x] >= 0;
            var cands = new List<(Pt At, Pt A, Pt B)>();
            foreach (var t in AllTiles())
            {
                var c = CellAt(P, t.X, t.Y);
                if (c.T != "wall" || CanonX(room, t) < 1 || CanonX(room, t) > 6 || Dist(t, room.Focus) == 1 || doors.Any(dr => Dist(dr, t) == 1)) continue;
                // Karşılıklı iki yan: önce kuzey–güney, sonra batı–doğu (ikisi de geçilebilir ve girişe bağlı)
                (Pt A, Pt B)? pair = null;
                if (Open(t.X, t.Y - 1) && Open(t.X, t.Y + 1)) pair = (new Pt(t.X, t.Y - 1), new Pt(t.X, t.Y + 1));
                else if (Open(t.X - 1, t.Y) && Open(t.X + 1, t.Y)) pair = (new Pt(t.X - 1, t.Y), new Pt(t.X + 1, t.Y));
                if (pair == null) continue;
                c.T = "floor";
                int d1 = Near(ExitSteps(P));
                c.T = "wall";
                if (d1 < 0 || d0 - d1 < FacilityMaps.ShutterSaving) continue;
                if (Dirs.Count(dd => Open(t.X + dd[0], t.Y + dd[1])) < 2) continue;
                cands.Add((t, pair.Value.A, pair.Value.B));
            }
            // Gevşetme (§9.2/1, ilk sıra): mevcut duvarda aday yoksa geçide giden en kısa yolun üstüne duvar–kepenk–duvar bölmesi
            bool partition = cands.Count == 0;
            if (partition) cands = PartitionShutters(P, room, s0, doors);
            if (cands.Count == 0) return false;
            var pick = cands[(int)Math.Floor(structure.Next() * cands.Count)];
            if (partition) foreach (var w in PartitionSides(P, pick.At, pick.A)) CellAt(P, w.X, w.Y).T = "wall";
            var at = CellAt(P, pick.At.X, pick.At.Y);
            at.T = "wall";
            at.Shutter = true;
            room.Shutter = pick.At;
            room.ShutterPartition = partition;
            room.ShutterDoors = new List<Pt> { pick.A, pick.B };
            return true;
        }

        /// <summary>
        /// Bölme kepengi adayları (yol sırasıyla): girişten en yakın ileri geçit karesine giden en kısa yolun x 2–6'daki boş zemin
        /// karesi; odağa ve geçit karelerine bitişik değil. Kare kepenk, yola dik iki boş zemin komşusu duvar olunca geçide yol
        /// açıkken en çok <see cref="FacilityMaps.GateMaxSteps"/>, kapalıyken en çok <see cref="FacilityMaps.GateClosedMaxSteps"/> adım
        /// kalmalı ve kepenk açılınca en az <see cref="FacilityMaps.ShutterSaving"/> kısalmalı. Kapı komşuları yolun önceki ve sonraki
        /// karesi.
        /// </summary>
        static List<(Pt At, Pt A, Pt B)> PartitionShutters(BState P, FacilityRoom room, int[,] s0, List<Pt> doors)
        {
            var o = new List<(Pt At, Pt A, Pt B)>();
            Pt? end = null;
            foreach (var f in room.Forward) if (s0[f.Y, f.X] >= 0 && (end == null || s0[f.Y, f.X] < s0[end.Value.Y, end.Value.X])) end = f;
            if (end == null) return o;
            // Geçitten girişe doğru geri yürüyüş (eşitlikte Dirs sırası), sonra girişten geçide sıralanır
            var path = new List<Pt> { end.Value };
            var p = end.Value;
            while (s0[p.Y, p.X] > 0)
            {
                foreach (var d in Dirs)
                {
                    int x = p.X + d[0], y = p.Y + d[1];
                    if (Inb(x, y) && s0[y, x] == s0[p.Y, p.X] - 1 && Passable(P, x, y)) { p = new Pt(x, y); break; }
                }
                path.Add(p);
            }
            path.Reverse();
            int Max(int[,] s) { int m = -1; foreach (var f in room.Forward) { if (s[f.Y, f.X] < 0) return -1; m = Math.Max(m, s[f.Y, f.X]); } return m; }
            int Min(int[,] s) => room.Forward.Select(f => s[f.Y, f.X]).Where(v => v >= 0).DefaultIfEmpty(-1).Min();
            for (int i = 1; i < path.Count - 1; i++)
            {
                var t = path[i];
                var c = CellAt(P, t.X, t.Y);
                if (CanonX(room, t) < 2 || CanonX(room, t) > 6 || c.T != "floor" || c.O != null || c.Door != 0 || c.Vent != 0) continue;
                if (Dist(t, room.Focus) == 1 || doors.Any(dr => Dist(dr, t) == 1)) continue;
                var prev = path[i - 1];
                var sides = PartitionSides(P, t, prev);
                c.T = "wall";
                foreach (var w in sides) CellAt(P, w.X, w.Y).T = "wall";
                var closed = ExitSteps(P);
                c.T = "floor";
                var open = ExitSteps(P);
                foreach (var w in sides) CellAt(P, w.X, w.Y).T = "floor";
                int dc = Max(closed), dcMin = Min(closed), dOpen = Max(open), doMin = Min(open);
                var next = new Pt(2 * t.X - prev.X, 2 * t.Y - prev.Y);
                if (dc < 0 || dc > FacilityMaps.GateClosedMaxSteps || dOpen < 0 || dOpen > FacilityMaps.GateMaxSteps) continue;
                if (doMin < 0 || dcMin - doMin < FacilityMaps.ShutterSaving) continue;
                if (!Inb(next.X, next.Y) || !Passable(P, next.X, next.Y) || closed[next.Y, next.X] < 0 || closed[prev.Y, prev.X] < 0) continue;
                o.Add((t, prev, next));
            }
            return o;
        }

        /// <summary>Bölme kepenginin (yolda <paramref name="prev"/>'den sonra gelen kare) yola dik iki komşusundan boş zemin olanlar
        /// (bölmenin duvarları olurlar).</summary>
        static List<Pt> PartitionSides(BState P, Pt t, Pt prev)
        {
            var o = new List<Pt>();
            int dx = t.X - prev.X, dy = t.Y - prev.Y;
            foreach (var s in new[] { new Pt(t.X + dy, t.Y + dx), new Pt(t.X - dy, t.Y - dx) })
            {
                if (!Inb(s.X, s.Y)) continue;
                var c = CellAt(P, s.X, s.Y);
                if (c.T == "floor" && c.O == null && c.Door == 0 && c.Vent == 0) o.Add(s);
            }
            return o;
        }

        /// <summary>
        /// Menfez çifti (A odası, yapı akışından 1 çekim): batı ucu x ≤ 3 ve girişten en az 2 kare, ileri uç x ≥ 4 ve ileri geçide ya
        /// da donanıma en çok 2 kare; uçlar arası yürüyüş en az <see cref="FacilityMaps.VentMinWalk"/>. Uçlar boş zemin, girişe bağlı,
        /// kepenge ve kopya dolabına bitişik değil, donanımın erişim karesi değil.
        /// </summary>
        static bool PlaceVents(BState P, FacilityRoom room, Rng structure)
        {
            var s = ExitSteps(P);
            var spawners = AllTiles().Where(t => CellAt(P, t.X, t.Y).O?.K == "spawner").ToList();
            bool Ok(Pt t)
            {
                var c = CellAt(P, t.X, t.Y);
                return c.T == "floor" && c.O == null && c.Door == 0 && s[t.Y, t.X] >= 0
                    && !(room.Shutter is Pt sh && Dist(sh, t) == 1) && !spawners.Any(sp => Dist(sp, t) == 1)
                    && !(room.DeviceAccess is Pt da && Same(da, t));
            }
            var westEnds = AllTiles().Where(t => Ok(t) && CanonX(room, t) <= 3 && room.West.Min(w => Dist(w, t)) >= 2).ToList();
            var fwdEnds = AllTiles().Where(t => Ok(t) && CanonX(room, t) >= 4 && (room.Forward.Min(f => Dist(f, t)) <= 2 || Dist(t, room.Focus) <= 2)).ToList();
            var pairs = new List<(Pt W, Pt F)>();
            foreach (var w in westEnds)
            {
                var walk = Steps(P, new[] { w });
                foreach (var f in fwdEnds) if (walk[f.Y, f.X] >= FacilityMaps.VentMinWalk) pairs.Add((w, f));
            }
            if (pairs.Count == 0) return false;
            var pick = pairs[(int)Math.Floor(structure.Next() * pairs.Count)];
            CellAt(P, pick.W.X, pick.W.Y).Vent = 1;
            CellAt(P, pick.F.X, pick.F.Y).Vent = 1;
            room.Vents = new List<Pt> { pick.W, pick.F };
            return true;
        }

        /// <summary>Karelerden yürüyüş uzaklığı (duvar ve nesne geçilmez, <paramref name="open"/> karesi geçilir: açık kepenk;
        /// ulaşılamayan −1). Tahtayı değiştirmez.</summary>
        static int[,] Steps(BState B, IEnumerable<Pt> from, Pt? open = null)
        {
            var dist = new int[N, N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) dist[y, x] = -1;
            var q = new Queue<Pt>();
            bool Walk(int x, int y) => Passable(B, x, y) || open is Pt o && o.X == x && o.Y == y;
            foreach (var f in from) if (Walk(f.X, f.Y) && dist[f.Y, f.X] < 0) { dist[f.Y, f.X] = 0; q.Enqueue(f); }
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var d in Dirs)
                {
                    int x = p.X + d[0], y = p.Y + d[1];
                    if (!Inb(x, y) || !Walk(x, y) || dist[y, x] >= 0) continue;
                    dist[y, x] = dist[p.Y, p.X] + 1; q.Enqueue(new Pt(x, y));
                }
            }
            return dist;
        }

        // ---------- Denetim ----------
        /// <summary>
        /// Yapı kuralları (birimler ve turnike yönü yok sayılır; kepenk kapalı): ileri geçit girişe bağlı ve en çok
        /// <see cref="FacilityMaps.GateMaxSteps"/> adım (bölme kepenginde <see cref="FacilityMaps.GateClosedMaxSteps"/>; kepenk açıkken
        /// yine en çok <see cref="FacilityMaps.GateMaxSteps"/>) ("ileri-yol"), odağın girişe bağlı bir komşusu ("odak"), en çok iki kapalı
        /// cep ("cep"), kopya dolapları girişe bağlı ve ileri geçitten uzak ("dolap"); kepenk ve menfez konduysa onların yanları
        /// ("kepenk", "menfez"). Geçerse null.
        /// </summary>
        internal static string StructureWhy(BState B, FacilityRoom c)
        {
            var s = ExitSteps(B);
            if (c.Type != RoomType.Target)
            {
                // Kapalıyken en çok GateMaxSteps; yalnız bölme kepenginde (A) en çok GateClosedMaxSteps. Açık kepenkle en çok GateMaxSteps
                int closedMax = c.Shutter != null && c.ShutterPartition ? FacilityMaps.GateClosedMaxSteps : FacilityMaps.GateMaxSteps;
                foreach (var f in c.Forward) if (s[f.Y, f.X] < 0 || s[f.Y, f.X] > closedMax) return "ileri-yol";
                if (c.Shutter is Pt sh)
                {
                    var open = Steps(B, c.West, sh);
                    foreach (var f in c.Forward) if (open[f.Y, f.X] < 0 || open[f.Y, f.X] > FacilityMaps.GateMaxSteps) return "ileri-yol";
                }
            }
            if (!Dirs.Any(d => Inb(c.Focus.X + d[0], c.Focus.Y + d[1]) && s[c.Focus.Y + d[1], c.Focus.X + d[0]] >= 0)) return "odak";
            int pockets = 0;
            foreach (var t in AllTiles()) if (Passable(B, t.X, t.Y) && s[t.Y, t.X] < 0) pockets++;
            if (pockets > 2) return "cep";
            foreach (var t in AllTiles())
            {
                if (CellAt(B, t.X, t.Y).O?.K != "spawner") continue;
                if (!Dirs.Any(d => Inb(t.X + d[0], t.Y + d[1]) && s[t.Y + d[1], t.X + d[0]] >= 0)) return "dolap";
                if (c.Forward.Any(f => Dist(f, t) <= 2)) return "dolap";
            }
            if (c.Shutter != null && c.ShutterDoors.Any(t => !Passable(B, t.X, t.Y) || s[t.Y, t.X] < 0)) return "kepenk";
            foreach (var v in c.Vents)
            {
                var cell = CellAt(B, v.X, v.Y);
                if (cell.T != "floor" || cell.O != null || s[v.Y, v.X] < 0) return "menfez";
            }
            return null;
        }

        /// <summary>
        /// Son denetim: yapı kuralları, şart parçaları (<paramref name="parts"/> verilirse: parça ayrılmış kareye düşmez, fiziksel
        /// parça girişten en az 2 kare; hol ve turnike kenarı hariç) ve başlangıç makinelerinin yeri (<paramref name="excluded"/>
        /// kareleri dışında en az bir bakım, <see cref="FacilityMaps.MinDroneSpots"/> dron yeri).
        /// </summary>
        static string FinalWhy(Cell[,] cells, FacilityRoom c, IEnumerable<PlanPart> parts, HashSet<int> excluded)
        {
            var B = new BState { Cells = cells, Exits = new List<Pt>(c.West) };
            var why = StructureWhy(B, c);
            if (why != null) return why;
            if (parts != null)
            {
                var reserved = new HashSet<int>(c.Reserved.Select(Key));
                foreach (var p in parts)
                {
                    if (p.Kind == "hol" || p.Kind == "turnike" || p.Kind == "turnike-acik") continue;
                    if (reserved.Contains(Key(p.At))) return "parca";
                    if (FacilityMaps.IsPhysicalPart(p.Kind) && c.West.Any(w => Dist(w, p.At) < 2)) return "parca";
                }
            }
            var s0 = ExitSteps(B);
            var taken = new HashSet<int>();
            foreach (var t in SquadTiles(B, c, excluded, 3)) if (t is Pt q) taken.Add(Key(q));
            if (MachineSpots(B, c, s0, excluded, taken, true).Count < FacilityMaps.MinMaintSpots) return "makine-yeri";
            if (MachineSpots(B, c, s0, excluded, taken, false).Count < FacilityMaps.MinDroneSpots) return "makine-yeri";
            return null;
        }

        /// <summary>
        /// Planın bütün hâllerini yeniden sınar: şartsız tahta, kademe 1–2, kademe 3 ve rafın öbür konumu (geçit son karede). Geçerse
        /// null, değilse "hâl:kod".
        /// </summary>
        internal static string RecheckRoom(FacilityRoom r)
        {
            string why = FinalWhy(r.BaseCells, r, null, new HashSet<int>());
            if (why != null) return "base:" + why;
            if (r.Event == null) return null;
            if (r.Cells3 == null || r.Ev3 == null || r.Cells1 == null || r.Ev1 == null) return "sart:yok";
            var ex = new HashSet<int>(r.PartTiles(3));
            why = FinalWhy(r.Cells1, r, r.PartsAt(1), ex);
            if (why != null) return "k1:" + why;
            why = FinalWhy(r.Cells3, r, r.PartsAt(3), ex);
            if (why != null) return "k3:" + why;
            if (r.Event == "raf")
            {
                foreach (var (cells, ev) in new[] { (r.Cells1, r.Ev1), (r.Cells3, r.Ev3) })
                {
                    var alt = CloneCells(cells);
                    var rail = ev.Area;
                    alt[rail[0].Y, rail[0].X].T = "wall";
                    alt[rail[rail.Count - 1].Y, rail[rail.Count - 1].X].T = "floor";
                    why = FinalWhy(alt, r, r.PartsAt(3), ex);
                    if (why != null) return "raf:" + why;
                }
            }
            return null;
        }

        // ---------- Şart ----------
        /// <summary>
        /// Şartı plan tahtasının kopyasına <paramref name="tier"/> kademesiyle kurar (<see cref="EventSetup"/> değişmeden): ayrılmış
        /// kareler kurulum boyunca maskelenir (turnike hariç: hol kenarı geçide değebilir, maske hole serbest giriş açardı); A odasında
        /// donanım kurulum boyunca hedef dolap sayılır (şartın odağı, kaçış güvencesinin koruduğu nesne). Olmazsa Ev null.
        /// </summary>
        static (Cell[,] Cells, EvState Ev) SetupVariant(BState P, FacilityRoom c, int tier)
        {
            var E = P.Clone();
            var q = P.Params;
            E.Params = new RaidParams
            {
                Tier = tier, Event = q.Event, Traits = q.Traits, CivilianHp = q.CivilianHp, MissionKind = q.MissionKind,
                Region = q.Region, SectorId = q.SectorId,
            };
            bool mask = c.Event != "turnike";
            if (mask)
                foreach (var t in c.Reserved)
                {
                    var cell = E.Cells[t.Y, t.X];
                    if (cell.T != "wall" && cell.O == null) cell.O = NewObj(MaskKind, 1);
                }
            var dev = c.Type == RoomType.Middle ? E.Cells[c.Focus.Y, c.Focus.X].O : null;
            if (dev != null) dev.K = "node";
            EventSetup(E, E.Params, c.DraftRows);
            if (dev != null) dev.K = c.Device;
            if (mask)
                foreach (var t in c.Reserved)
                    if (E.Cells[t.Y, t.X].O?.K == MaskKind) E.Cells[t.Y, t.X].O = null;
            return (E.Cells, E.Ev);
        }

        /// <summary>
        /// Düşük kademenin kurulumu yüksek kademeninkinin kırpılmışı mı: tavanda çatlaklar alt küme ve en çok bir eksik, öbür her
        /// şey aynı; tüpte tüp listesi önek ve en çok bir eksik, hücreler eksik tüpün karesi dışında aynı.
        /// </summary>
        static bool LowSubsetOfHigh(string ev, (Cell[,] Cells, EvState Ev) lo, (Cell[,] Cells, EvState Ev) hi)
        {
            bool SameCell(Cell a, Cell b, bool obj) => a.T == b.T && a.Shelf == b.Shelf && a.Phase == b.Phase && (!obj || a.O?.K == b.O?.K);
            if (ev == "tavan")
            {
                int extra = 0;
                foreach (var t in AllTiles())
                {
                    Cell a = lo.Cells[t.Y, t.X], b = hi.Cells[t.Y, t.X];
                    if (!SameCell(a, b, true)) return false;
                    if (a.Crack && !b.Crack) return false;
                    if (b.Crack && !a.Crack) extra++;
                }
                return extra <= 1;
            }
            if (ev == "tup")
            {
                var f1 = lo.Ev.Fuses; var f3 = hi.Ev.Fuses;
                if (f1.Count > f3.Count || f3.Count - f1.Count > 1) return false;
                for (int i = 0; i < f1.Count; i++) if (f1[i].X != f3[i].X || f1[i].Y != f3[i].Y || f1[i].Dir != f3[i].Dir) return false;
                var diff = new HashSet<int>(f3.Skip(f1.Count).Select(f => f.Y * N + f.X));
                foreach (var t in AllTiles())
                {
                    Cell a = lo.Cells[t.Y, t.X], b = hi.Cells[t.Y, t.X];
                    if (!SameCell(a, b, !diff.Contains(Key(t))) || a.Crack != b.Crack) return false;
                }
                return true;
            }
            return true;
        }

        /// <summary>Şartın parçaları (en yüksek kademenin kurulumundan); yalnız düşük kademede olmayan çatlak ve tüp 3. kademededir. Aşama 12:
        /// Katar ve Yalpa parça üretmez (kenar geometriden, <see cref="IsOpenEdge"/>; yatış takvimden, <see cref="TiltSchedule"/>).</summary>
        static List<PlanPart> ExtractParts(string ev, (Cell[,] Cells, EvState Ev) lo, (Cell[,] Cells, EvState Ev) hi)
        {
            var o = new List<PlanPart>();
            var e = hi.Ev;
            var cells = hi.Cells;
            void Add(string kind, Pt at, int dir = -1, int min = 1, Pt? to = null) => o.Add(new PlanPart { Kind = kind, At = at, Dir = dir, MinTier = min, To = to });
            switch (ev)
            {
                case "bulut":
                    Add("meme", e.P);
                    if (!Same(e.P, e.Q)) Add("meme", e.Q);
                    foreach (var t in e.Area.Concat(e.Area2).GroupBy(Key).Select(g => g.First())) Add("bulut", t);
                    break;
                case "raf":
                    foreach (var t in e.Area) Add("raf", t);
                    break;
                case "gaz":
                    Add("sizinti", e.P);
                    foreach (var t in e.Area) Add("gaz", t);
                    break;
                case "tavan":
                    foreach (var t in AllTiles())
                        if (cells[t.Y, t.X].Crack) Add("catlak", t, min: lo.Cells[t.Y, t.X].Crack ? 1 : 3);
                    break;
                case "asansor":
                    Add("durak", e.P);
                    Add("durak", e.Q);
                    break;
                case "kriyo":
                    foreach (var t in e.Area) Add("kriyo", t);
                    break;
                case "turnike":
                    foreach (var t in e.Area) Add("hol", t);
                    foreach (var g in HolEdges(new BState { Cells = cells, Ev = e }).ToList())
                        Add(g.Both ? "turnike-acik" : "turnike", new Pt(g.X, g.Y), to: new Pt(g.X2, g.Y2));
                    break;
                case "kaynak":
                    Add("kol", e.P, dir: e.Mode);
                    break;
                case "faz":
                    foreach (var t in AllTiles())
                    {
                        var c = cells[t.Y, t.X];
                        if (c.T == "cable" && c.Phase != 0) Add(c.Phase == 1 ? "faz-a" : "faz-b", t);
                    }
                    break;
                case "suzgec":
                    Add("suzgec", e.P);
                    break;
                case "tup":
                    foreach (var f in e.Fuses)
                        Add("tup", new Pt(f.X, f.Y), dir: f.Dir, min: lo.Ev.Fuses.Any(l => l.X == f.X && l.Y == f.Y && l.Dir == f.Dir) ? 1 : 3);
                    break;
                case "yag":
                    Add("hortum", e.P);
                    foreach (var t in e.Area) Add("yag", t);
                    break;
            }
            return o;
        }

        /// <summary>
        /// Şartın olay adımı işleyen turları (1..<paramref name="maxTurns"/>−1; son turda olay adımı yok): oda başına takvim. Darbe
        /// ve evre güdümlü şartlarda (kaynak, tavan, tüp) boş.
        /// </summary>
        internal static IReadOnlyList<int> EventStepTurns(string id, int maxTurns)
        {
            var B = new BState { MaxTurns = maxTurns, Ev = new EvState { Id = id } };
            var o = new List<int>();
            for (int t = 1; t < maxTurns; t++)
            {
                bool step;
                switch (id)
                {
                    case "bulut": step = t % 2 == 1; break;
                    case "raf": step = ShiftAt(B, t); break;
                    case "gaz": step = GasFillAt(t) || GasVentAt(t); break;
                    case "asansor": step = LiftDeparts(B, t); break;
                    case "kriyo": step = FreezeAt(B, t); break;
                    case "turnike": step = GateTurnAt(B, t); break;
                    case "suzgec": step = DrainAt(t); break;
                    case "yag": step = t == 2; break;
                    case "faz": step = LivePhase(t + 1) != LivePhase(t); break;
                    // Aşama 12: Yalpa 2., 4., 6. tur (bütçe içinde); Katar'ın adımı yok
                    case "yalpa": step = TiltAt(B, t); break;
                    default: step = false; break;
                }
                if (step) o.Add(t);
            }
            return o;
        }

        // ---------- Oda kurulumu ----------
        /// <summary>
        /// Tesis odasının taktik tahtası: yapı, sabit nesneler ve şart parçaları plandan kopyalanır; makineler oda tohumuyla
        /// (setup.Seed) yerleşir. Serpiştirme yoktur. S.Rng'ye dokunmaz. Dönüş ziyaretinde (setup.Return) tahta odanın son raporundan
        /// kurulur (<see cref="ReturnBoard"/>). Sıradaki oda, taşınanlar ve hazırlık oda zincirinin işidir (RaidChain.Setup).
        /// </summary>
        public static BState CreateRoom(RaidParams p, FacilityRoom room, RoomSetup setup)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            // Aşama 11: savunma odası kendi tahtasını kurar (Baskin.Defense.cs)
            var B = setup.Return ? ReturnBoard(p, room, setup) : setup.Defense ? DefenseBoard(p, room, setup) : RoomBoard(p, room, setup);
            PlanEnemies(B);
            FieldDogReport(B);
            if (!p.Traits.Contains("guvenli")) PlaceSpawns(B);
            B.FxList.Clear();
            return B;
        }

        /// <summary>
        /// İlk ziyaret tahtasının tur bütçesi (<see cref="BState.MaxTurns"/>): kurulumun süresi (<see cref="RoomSetup.Turns"/>; Eşik dahil),
        /// hedef odasında "kisa" özelliği bir tur kısaltır. <see cref="RoomBoard"/> ve plan okuyucusu (<see cref="Simulation.FacilityRoomTurns"/>)
        /// bu tek kuralı okur (aşama 12 kapanışı). Dönüş ve savunma tahtası kendi süresini kurar. Saf.
        /// </summary>
        public static int FirstVisitTurns(int setupTurns, FacilityRoom room, ICollection<string> traits) =>
            setupTurns - (room != null && room.Type == RoomType.Target && traits != null && traits.Contains("kisa") ? 1 : 0);

        /// <summary>
        /// <see cref="CreateRoom"/>'un ilk bölümü (gidiş): tahta, başlangıç makineleri (planlanmadan önceki yerlerinde), kadro (ayaktaki
        /// taşınanlarla; düşen kişi konmaz), hedef odada kademe ekleri ve imza, olay, açılış ve hazırlık satırları. Testler makinelerin
        /// başlangıç yerini buradan okur (plan onları yürütür). setup ekleri varsayılanındayken çıktı ve çekim sırası aşama 1'in aynısı.
        /// </summary>
        internal static BState RoomBoard(RaidParams p, FacilityRoom room, RoomSetup setup)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (room.Event != null && !string.IsNullOrEmpty(p.Event) && p.Event != room.Event)
                throw new ArgumentException("Baskının olayı odanın olayı değil: " + p.Event + " / " + room.Event + " (" + room.Id + ")", nameof(p));
            bool target = room.Type == RoomType.Target;
            var B = new BState
            {
                Params = p, Rs = setup.Seed, Room = room, Layout = room.NameKey,
                MaxTurns = FirstVisitTurns(setup.Turns, room, p.Traits),
                SpawnPerTurn = p.Traits.Contains("kopya") ? 2 : 1,
                IsCore = target && p.IsCore && p.MissionKind != "loot",
                Signature = string.IsNullOrEmpty(p.Signature) ? null : p.Signature,
                EnemyHp = p.Traits.Contains("guclu") ? 1 : 0,
                EnemyMove = p.Traits.Contains("devriye") ? 1 : 0,
                Fog = p.Traits.Contains("sis"),
                Smart = p.Tactics > 0,
                Moves = setup.Moves ?? MoveRules.From(null),
            };
            Rnd(B);
            bool eventOn = room.Event != null && !string.IsNullOrEmpty(p.Event);
            var src = !eventOn ? room.BaseCells : p.Tier >= 3 ? room.Cells3 : room.Cells1;
            var ev = !eventOn ? null : p.Tier >= 3 ? room.Ev3 : room.Ev1;
            int cabinet = p.Traits.Contains("zirhli") ? 1 : 0;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var c = src[y, x].Clone();
                    var o = c.O;
                    if (o != null)
                    {
                        int hp = o.Hp;
                        if (IsTarget(o.K))
                        {
                            o.K = B.IsCore ? "core" : "node";
                            hp = (B.IsCore ? 5 : 4) + cabinet + (IsLoot(B) || B.IsCore ? 0 : p.TargetHpBonus);
                            // Tesis hazırlığı (soğutma vanası kapalı, sunucu görevi): hedef dolap eksik canla, en az 1
                            if (setup.TargetHpLess > 0) hp = Math.Max(1, hp - setup.TargetHpLess);
                        }
                        else if (o.K == "civ") hp = Math.Max(1, p.CivilianHp);
                        else if (o.K == "cool" || o.K == "tup" || o.K == "kol") hp = 1;
                        else if (o.K == "spawner") hp = 2;
                        else if (IsDevice(o.K)) hp = Math.Max(1, setup.DeviceHp);
                        o.Hp = o.Max = hp;
                    }
                    B.Cells[y, x] = c;
                }
            // Tesis aşama 10: önlemlerin yapısal kaplaması (çift cidar, koku yayıcı; RNG'siz), kadro ve makine karelerinden önce
            ApplyCounterCells(B.Cells, room, eventOn, B.Moves.DoubleWall, B.Moves.Emitter, B.Moves.DoubleWalls, B.Moves.EmitterHp);
            // Tesis aşama 11: odanın izleri (kalıcı tahta; RNG'siz), kaplamadan sonra, kadro ve makine karelerinden önce. İzsiz emirde hiçbir şey
            if (setup.Marks != null && setup.Marks.Count > 0) FacilityMarks.Apply(B.Cells, room, setup.Marks, B.Rubble, eventOn);
            B.Exits = new List<Pt>(room.West);
            var excluded = eventOn ? new HashSet<int>(room.PartTiles(3)) : new HashSet<int>();
            // Kadronun kareleri önce ayrılır; makineler (kadrodan önce eklenir: kimlik sırası Create ile eş) onlara konmaz. Taşınan
            // kadroda yalnız ayaktakiler girer (önceki odada düşen konmaz)
            var squad = SquadOf(p);
            var carry = setup.Carry;
            if (carry != null && carry.Count != squad.Count)
                throw new ArgumentException("Odaya giren kadro emirdeki kadroyla uyuşmuyor: " + room.Id, nameof(setup));
            var standing = Enumerable.Range(0, squad.Count).Where(i => carry == null || carry[i].State == "a").ToList();
            // Ekiple gelen düşenler (geçitten sürüklendi) ayaktakilerden sonra yer alır; kareleri makineye kapalıdır. Sedye (aşama 8,
            // K24): ekiple gelen gövde tahtaya konmaz, rapor onu önceki rapordan taşır
            var carried = carry == null || B.Moves.Stretcher ? new List<int>()
                : Enumerable.Range(0, squad.Count).Where(i => carry[i].State == "d" && carry[i].Body == 1).ToList();
            // Aşama 8: ekiple giren köpekler ve tutsaklar ayaktakilerden sonra, gövdelerden önce yer alır; kareleri makineye kapalıdır
            var extra = EnteringUnits(B, squad, setup);
            var spots = SquadTiles(B, room, excluded, standing.Count + extra.Count + carried.Count);
            var taken = new HashSet<int>(spots.Where(t => t != null).Select(t => Key(t.Value)));
            var bodyTiles = new HashSet<int>(spots.Skip(standing.Count).Where(t => t != null).Select(t => Key(t.Value)));
            var s0 = ExitSteps(B);
            // Tesis hazırlığı (çağrı anteni kırık, hedef odası): önce kademe ekinden, yetmezse başlangıç dronundan; bakım robotu kalır
            int tierCut = target ? Math.Min(Math.Max(0, setup.MachinesLess), TierExtra(p)) : 0;
            int startCut = target ? Math.Max(0, setup.MachinesLess) - tierCut : 0;
            int start = setup.Machines - Math.Min(startCut, Math.Max(0, setup.Machines - 1));
            for (int m = 0; m < start; m++)
            {
                bool maint = m == 0;
                var cand = MachineSpots(B, room, s0, excluded, taken, maint);
                // Sürüm 10: odağın yanında boş kare kalmadıysa (dar oda, koku yayıcı) bakım robotu öbür makine karelerinden birine konur:
                // makine ve çekim sayısı önlemden bağımsız kalır
                if (cand.Count == 0 && maint) cand = MachineSpots(B, room, s0, excluded, taken, false);
                if (cand.Count == 0) continue;
                var t = cand[(int)Math.Floor(Rnd(B) * cand.Count)];
                B.Units.Add(NewUnit(B, maint ? "bakim" : "dron", t.X, t.Y));
            }
            AddSquad(B, p, squad, standing, spots, carry);
            if (target)
            {
                AddTierMachines(B, p, pt => CellAt(B, pt.X, pt.Y).Door == 0 && CellAt(B, pt.X, pt.Y).Vent == 0 && !excluded.Contains(Key(pt)) && !bodyTiles.Contains(Key(pt)), tierCut);
                if (B.Signature != null) ApplySignature(B, p, p.Tier >= 3 ? 2 : 1);
            }
            B.Ev = ev?.Clone();
            // Aşama 11: izden gelen moloz raf rayındaysa raf durur (dönüş tahtasının kuralı; izsiz emirde Rubble boş)
            if (B.Ev != null && B.Rubble.Count > 0 && B.Rubble.Any(t => src[t.Y, t.X].Shelf)) B.Ev.Stop = true;
            if (B.Ev != null) SayEvent(B, B.Ev.Id);
            if (target) SayOpening(B, p);
            B.QuietTurns = Math.Max(0, setup.QuietTurns);
            B.StillTurns = Math.Max(0, setup.StillTurns);
            int less = tierCut + (setup.Machines - start);
            if (B.QuietTurns > 0) Say(B, Loc.F("Sigorta panosu kırık: kopya dolapları ilk {0} tur makine uyandırmaz.", B.QuietTurns), "good");
            if (less > 0) Say(B, Loc.F("Çağrı anteni kırık: {0} makine eksik geldi.", less), "good");
            if (setup.TargetHpLess > 0 && target && !IsLoot(B)) Say(B, Loc.F("Soğutma vanası kapalı: hedef dolap {0} can eksik.", setup.TargetHpLess), "good");
            if (B.StillTurns > 0) Say(B, Loc.F("Soğutma vanası kapalı: makineler ilk {0} tur yürüyemez.", B.StillTurns), "good");
            // Köpekler ve tutsaklar (aşama 8), sonra gövdeler en sonda (kademe ekleri ve imzadan sonra): öbür birimlerin kimlik sırası
            // değişmez
            AddUnits(B, extra, spots.Skip(standing.Count).ToList());
            AddBodies(B, p, squad, carried, spots.Skip(standing.Count + extra.Count).ToList(), carry, room.West[0]);
            return B;
        }

        /// <summary>
        /// Kadronun kareleri, kadro sırasıyla: önce giriş kareleri (<paramref name="gate"/>; verilmezse batı girişi, y artan), taşanlar
        /// girişe yürüme uzaklığı en küçük boş zemine (geçit, menfez ve parça karesi değil; eşitlikte küçük y, sonra küçük x). Yer
        /// yoksa null. Dönüş odasında giriş ileri geçittir (ekip doğudan girer).
        /// </summary>
        static List<Pt?> SquadTiles(BState B, FacilityRoom room, HashSet<int> excluded, int count, IReadOnlyList<Pt> gate = null)
        {
            var g = gate ?? room.West;
            var o = new List<Pt?>();
            var taken = new HashSet<int>();
            int[,] s = null;
            for (int i = 0; i < count; i++)
            {
                if (i < g.Count) { o.Add(g[i]); taken.Add(Key(g[i])); continue; }
                s = s ?? (gate == null ? ExitSteps(B) : Steps(B, g));
                Pt? best = null;
                foreach (var t in AllTiles())
                {
                    var c = CellAt(B, t.X, t.Y);
                    if (c.T != "floor" || c.O != null || c.Door != 0 || c.Vent != 0 || excluded.Contains(Key(t)) || taken.Contains(Key(t)) || s[t.Y, t.X] < 0) continue;
                    if (best == null || s[t.Y, t.X] < s[best.Value.Y, best.Value.X]) best = t;
                }
                o.Add(best);
                if (best != null) taken.Add(Key(best.Value));
            }
            return o;
        }

        /// <summary>
        /// Başlangıç makinesinin aday kareleri (y, x sırası): boş zemin, uzak yarı (x ≥ 4), girişe bağlı; geçit, menfez, parça ve
        /// kadro karesi değil. Bakım robotu odağa en çok 2 kare (odak kasa odasındaysa içeride); dron kasa odasının dışında ve girişe
        /// açık atış hattı yok.
        /// </summary>
        static List<Pt> MachineSpots(BState B, FacilityRoom room, int[,] s0, HashSet<int> excluded, HashSet<int> taken, bool maint)
        {
            var o = new List<Pt>();
            int focusKey = Key(room.Focus);
            foreach (var t in AllTiles())
            {
                var c = CellAt(B, t.X, t.Y);
                int k = Key(t);
                if (c.T != "floor" || c.O != null || UnitAt(B, t.X, t.Y) != null || c.Door != 0 || c.Vent != 0) continue;
                if (excluded.Contains(k) || taken.Contains(k) || CanonX(room, t) < 4 || s0[t.Y, t.X] < 0) continue;
                if (maint)
                {
                    if (Dist(t, room.Focus) > 2 || room.Inner.Contains(focusKey) && !room.Inner.Contains(k)) continue;
                }
                else
                {
                    if (room.Inner.Contains(k)) continue;
                    // Girişin ateş hattı: kanonik batıya doğru (dönük odada gerçek yönü başka) duvar ya da nesne yoksa giriş karesi görür
                    var ct = room.ToCanon(t);
                    int sx = ct.X - 1;
                    bool Clear(int cx) { var a = room.FromCanon(new Pt(cx, ct.Y)); return CellAt(B, a.X, a.Y).T != "wall" && CellAt(B, a.X, a.Y).O == null; }
                    while (sx > 0 && Clear(sx)) sx--;
                    if (sx == 0 && room.West.Any(w => Same(w, room.FromCanon(new Pt(0, ct.Y))))) continue;
                }
                o.Add(t);
            }
            return o;
        }
    }
}
