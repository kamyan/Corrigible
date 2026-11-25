using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis odasının taktik tahtası (<see cref="Baskin.CreateRoom"/>, aşama 1): kadronun ve başlangıç makinelerinin yeri, şart
    /// parçalarının ve yapının dışında kalma, determinizm ve RNG çekim sırası, imza eki, geri alma, yeni karelerle motorun
    /// çalışması, kademe varyantı, olaysız plan, yanlış şart ve makine sayıları. Odalar <c>FacilityMaps.For(79)</c>'dan gelir.
    /// Başlangıç makinelerinin yeri <see cref="Baskin.RoomBoard"/>'dan okunur: <see cref="Baskin.CreateRoom"/>'un sonundaki
    /// makine planı onları yürütür.
    /// </summary>
    public class FacilityRoomTests
    {
        static IReadOnlyList<FacilityRoom> Rooms => FacilityMaps.For(79).Rooms;

        /// <summary>Bölgenin kampanya değerleriyle baskın: özelliği, imza düşmanı ve olayı.</summary>
        static RaidParams Params(FacilityRoom room, int tier)
        {
            var def = GameData.Region(room.Region);
            return new RaidParams
            {
                Region = room.Region, SectorId = room.SectorId, MissionKind = room.Mission, Traits = new List<string> { def.Trait },
                Signature = def.Enemy, Event = FacilityMaps.EventFor(def, true, true), Tier = tier, Tactics = 1,
            };
        }

        static RoomSetup Setup(FacilityRoom room, int seed) => FacilityMaps.Setup(new Balance(), room, seed);

        static int Key(int x, int y) => y * Baskin.N + x;
        static int Man(int ax, int ay, int bx, int by) => Math.Abs(ax - bx) + Math.Abs(ay - by);
        static bool In(int x, int y) => x >= 0 && y >= 0 && x < Baskin.N && y < Baskin.N;
        static bool Open(Baskin.BState B, int x, int y) => In(x, y) && B.Cells[y, x].T != "wall" && B.Cells[y, x].O == null;

        /// <summary>Batı girişinden yürüyüş uzaklığı (testin kendi BFS'i: duvar ve nesne geçilmez, birimler sayılmaz; −1 ulaşılmaz).</summary>
        static int[,] Steps(Baskin.BState B, IEnumerable<Baskin.Pt> from)
        {
            var d = new int[Baskin.N, Baskin.N];
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) d[y, x] = -1;
            var q = new Queue<Baskin.Pt>();
            foreach (var f in from) if (Open(B, f.X, f.Y)) { d[f.Y, f.X] = 0; q.Enqueue(f); }
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var dd in Baskin.Dirs)
                {
                    int x = p.X + dd[0], y = p.Y + dd[1];
                    if (!Open(B, x, y) || d[y, x] >= 0) continue;
                    d[y, x] = d[p.Y, p.X] + 1; q.Enqueue(new Baskin.Pt(x, y));
                }
            }
            return d;
        }

        static List<Baskin.Unit> Squad(Baskin.BState B) => B.Units.Where(u => u.Side == 'p').OrderBy(u => u.Id).ToList();

        /// <summary>Başlangıç makineleri (kadrodan önce eklenir) ve hedef odanın kademe ekleri (kadrodan sonra).</summary>
        static (List<Baskin.Unit> Start, List<Baskin.Unit> Extra) Machines(Baskin.BState B)
        {
            int first = B.Units.Where(u => u.Side == 'p').Min(u => u.Id);
            var foes = B.Units.Where(u => u.Side == 'e').OrderBy(u => u.Id).ToList();
            return (foes.Where(u => u.Id < first).ToList(), foes.Where(u => u.Id > first).ToList());
        }

        static bool SameCell(Baskin.Cell a, Baskin.Cell b) =>
            a.T == b.T && a.O?.K == b.O?.K && a.Door == b.Door && a.Vent == b.Vent && a.Shutter == b.Shutter
            && a.Crack == b.Crack && a.Shelf == b.Shelf && a.Phase == b.Phase;

        // ---------- Kadro ----------
        [Test]
        public void Squad_FillsWestThenNearestFloor()
        {
            var four = new List<SquadSlot>
            {
                new SquadSlot { Type = "tek", Hp = 3 }, new SquadSlot { Type = "emp", Hp = 3 }, new SquadSlot { Type = "ana", Hp = 2 }, new SquadSlot { Type = "tek", Hp = 3 },
            };
            foreach (var room in Rooms)
                foreach (var squad in new[] { null, four })
                {
                    var p = Params(room, 1);
                    p.Squad = squad;
                    var B = Baskin.CreateRoom(p, room, Setup(room, 7));
                    var team = Squad(B);
                    int size = squad?.Count ?? 3;
                    Assert.AreEqual(size, team.Count, room.Id);
                    int west = room.Type == RoomType.Entry ? 3 : 2;
                    Assert.AreEqual(west, room.West.Count, room.Id);
                    for (int i = 0; i < west; i++) Assert.IsTrue(team[i].X == room.West[i].X && team[i].Y == room.West[i].Y, $"{room.Id}: {i}. üye girişte değil");
                    // Taşanlar: girişe yürüme uzaklığı en küçük boş zemin (geçit, menfez ve parça karesi değil); eşitlikte y, sonra x
                    var s = Steps(B, room.West);
                    var parts = room.Event != null ? new HashSet<int>(room.PartTiles(3)) : new HashSet<int>();
                    var taken = new HashSet<int>(room.West.Select(t => Key(t.X, t.Y)));
                    for (int i = west; i < size; i++)
                    {
                        (int x, int y)? best = null;
                        for (int y = 0; y < Baskin.N; y++)
                            for (int x = 0; x < Baskin.N; x++)
                            {
                                var c = B.Cells[y, x];
                                if (c.T != "floor" || c.O != null || c.Door != 0 || c.Vent != 0 || parts.Contains(Key(x, y)) || taken.Contains(Key(x, y)) || s[y, x] < 0) continue;
                                if (best == null || s[y, x] < s[best.Value.y, best.Value.x]) best = (x, y);
                            }
                        Assert.IsNotNull(best, room.Id);
                        Assert.AreEqual(best.Value, (team[i].X, team[i].Y), $"{room.Id}: {i}. üye");
                        taken.Add(Key(best.Value.x, best.Value.y));
                    }
                    if (squad != null) Assert.AreEqual(room.Type == RoomType.Entry ? 1 : 2, size - west, room.Id + ": taşan üye sayısı");
                }
        }

        // ---------- Makineler ----------
        [Test]
        public void StartMachines_FollowTheMapRules([Range(1, 20)] int seed)
        {
            foreach (var room in Rooms)
                foreach (int tier in new[] { 1, 3 })
                {
                    var p = Params(room, tier);
                    var setup = Setup(room, seed);
                    var B = Baskin.RoomBoard(p, room, setup);
                    var (start, extra) = Machines(B);
                    // Aşama 4 kararı: giriş ve ara oda yalnız bakım robotuyla başlar, hedef odası 3 makineyle
                    Assert.AreEqual(room.Type == RoomType.Target ? 3 : 1, setup.Machines, room.Id);
                    Assert.AreEqual(setup.Machines, start.Count, $"{room.Id} T{tier}: başlangıç makinesi");
                    int tierExtra = Math.Max(0, tier - 1) + (p.Traits.Contains("takviye") ? 1 : 0);
                    Assert.AreEqual(room.Type == RoomType.Target ? Math.Min(7, 3 + tierExtra) - 3 : 0, extra.Count, $"{room.Id} T{tier}: kademe eki");
                    Assert.AreEqual("bakim", start[0].Type, room.Id);
                    foreach (var u in start.Skip(1))
                        Assert.IsTrue(u.Type == "dron" || room.Type == RoomType.Target && u.Type == p.Signature, $"{room.Id}: {u.Type}");
                    var s = Steps(B, room.West);
                    foreach (var u in start)
                    {
                        var c = B.Cells[u.Y, u.X];
                        string at = $"{room.Id} T{tier} {u.Type} {u.X},{u.Y}";
                        // Sürüm 10: uzak yarı kanonik düzende (giriş batıda; oda döndürülmüş olabilir)
                        var cu = room.ToCanon(new Baskin.Pt(u.X, u.Y));
                        Assert.GreaterOrEqual(cu.X, 4, at);
                        Assert.GreaterOrEqual(s[u.Y, u.X], 0, at + ": girişten ulaşılmaz");
                        Assert.IsTrue(c.T == "floor" && c.O == null && c.Door == 0 && c.Vent == 0, at + ": kablo, geçit ya da menfez");
                        if (u == start[0])
                        {
                            Assert.LessOrEqual(Man(u.X, u.Y, room.Focus.X, room.Focus.Y), 2, at + ": bakım odağa uzak");
                            continue;
                        }
                        // Dronun girişe açık atış hattı yok (kanonik batıya doğru)
                        int sx = cu.X - 1;
                        bool Clear(int cx) { var a = room.FromCanon(new Baskin.Pt(cx, cu.Y)); return B.Cells[a.Y, a.X].T != "wall" && B.Cells[a.Y, a.X].O == null; }
                        while (sx > 0 && Clear(sx)) sx--;
                        var w0 = room.FromCanon(new Baskin.Pt(0, cu.Y));
                        Assert.IsFalse(sx == 0 && room.West.Any(w => w.X == w0.X && w.Y == w0.Y), at + ": girişe açık hat");
                    }
                }
        }

        [Test]
        public void Machines_NeverStartOnPartsOrStructure([Range(1, 20)] int seed)
        {
            foreach (var room in Rooms)
                foreach (int tier in new[] { 1, 2, 3 })
                {
                    var B = Baskin.RoomBoard(Params(room, tier), room, Setup(room, seed));
                    var parts = new HashSet<int>(room.PartTiles(3));
                    foreach (var u in B.Units)
                    {
                        string at = $"{room.Id} T{tier} {u.Type} {u.X},{u.Y}";
                        Assert.IsFalse(parts.Contains(Key(u.X, u.Y)), at + ": şart parçasında");
                        if (u.Side == 'e') Assert.IsTrue(B.Cells[u.Y, u.X].Door == 0 && B.Cells[u.Y, u.X].Vent == 0, at + ": geçit ya da menfez");
                    }
                }
        }

        // ---------- Determinizm ve RNG ----------
        [Test]
        public void CreateRoom_IsDeterministic_AndPlanIndependentOfSeed()
        {
            var fixedRooms = new List<string>();
            foreach (var room in Rooms)
            {
                var p = Params(room, 3);
                Assert.AreEqual(Baskin.Signature(Baskin.CreateRoom(p, room, Setup(room, 5))), Baskin.Signature(Baskin.CreateRoom(p, room, Setup(room, 5))), room.Id);
                var plan = room.Event == null ? room.BaseCells : room.Cells3;
                var layouts = new HashSet<string>();
                for (int seed = 1; seed <= 20; seed++)
                {
                    var B = Baskin.CreateRoom(p, room, Setup(room, seed));
                    for (int y = 0; y < Baskin.N; y++)
                        for (int x = 0; x < Baskin.N; x++)
                            Assert.IsTrue(SameCell(B.Cells[y, x], plan[y, x]), $"{room.Id} tohum {seed}: {x},{y} plandan farklı");
                    var start = Baskin.RoomBoard(p, room, Setup(room, seed));
                    layouts.Add(string.Join(";", start.Units.Where(u => u.Side == 'e').Select(u => $"{u.X}{u.Y}")));
                }
                if (layouts.Count == 1) fixedRooms.Add(room.Id);
            }
            // Sürüm 10: dar odada tek aday kare kalabilir (in_1.2: kademe 3'ün tek bakım robotu odağın yanındaki tek boş karede);
            // yine de odaların neredeyse hepsinde makine yerleri tohumla değişir
            Assert.LessOrEqual(fixedRooms.Count, 2, "makine yerleri tohumla değişmiyor: " + string.Join(", ", fixedRooms));
        }

        /// <summary>Çekim sırası (<c>guvenli</c>, <c>Tactics = 0</c>): boş çekim, başlangıç makinesi başına 1, kademe eki başına 2,
        /// makine planında makine başına 1. Oda tohumunun mulberry32 akışı (<c>Rng_IsMulberry32</c>) o kadar ilerler.</summary>
        [Test]
        public void CreateRoom_DrawsInTheDocumentedOrder()
        {
            foreach (var room in Rooms)
                foreach (int tier in new[] { 1, 3 })
                {
                    var p = Params(room, tier);
                    p.Traits = new List<string> { "guvenli" };
                    p.Tactics = 0;
                    var B = Baskin.CreateRoom(p, room, Setup(room, 1234));
                    var (start, extra) = Machines(B);
                    int draws = 1 + start.Count + 2 * extra.Count + B.Units.Count(u => u.Side == 'e');
                    var r = new Rng(1234u);
                    for (int i = 0; i < draws; i++) r.Next();
                    Assert.AreEqual(r.State, (uint)B.Rs, $"{room.Id} T{tier}");
                    Assert.IsEmpty(B.Spawns, room.Id);
                }
        }

        [Test]
        public void RoomEvent_DrawsNoRandomness([Range(1, 30)] int seed)
        {
            foreach (var room in Rooms.Where(r => r.Event != null))
            {
                var on = Params(room, 1 + seed % 4);
                on.Traits = new List<string> { "guvenli" };
                var off = Params(room, 1 + seed % 4);
                off.Traits = new List<string> { "guvenli" };
                off.Event = null;
                var a = Baskin.CreateRoom(on, room, Setup(room, seed));
                var b = Baskin.CreateRoom(off, room, Setup(room, seed));
                Assert.IsNotNull(a.Ev, room.Id);
                Assert.IsNull(b.Ev, room.Id);
                Assert.AreEqual(b.Rs, a.Rs, room.Id);
            }
        }

        // ---------- İmza ve geri alma ----------
        [Test]
        public void Signature_HasRoomSuffix()
        {
            foreach (var room in Rooms)
                StringAssert.EndsWith("|R" + room.Id, Baskin.Signature(Baskin.CreateRoom(Params(room, 1), room, Setup(room, 3))));
        }

        [Test]
        public void Undo_KeepsTheRoom()
        {
            var room = Rooms.First(r => r.Type == RoomType.Entry);
            var B = Baskin.CreateRoom(Params(room, 1), room, Setup(room, 1));
            string before = Baskin.Signature(B);
            var tek = B.Units.First(u => u.Side == 'p');
            var to = Baskin.Reachable(B, tek).First();
            Assert.IsTrue(Baskin.DoMove(B, tek.Id, to.X, to.Y));
            Assert.AreNotEqual(before, Baskin.Signature(B));
            var plan = B.Room;
            Assert.IsTrue(Baskin.Undo(B));
            Assert.AreSame(plan, B.Room);
            Assert.AreSame(room, B.Room);
            Assert.AreEqual(before, Baskin.Signature(B));
        }

        // ---------- Yeni kareler ve motor ----------
        /// <summary>
        /// Yeni kareler motoru bozmaz: her oda hamlesiz <see cref="Baskin.EndTurn"/> × süre istisnasız biter. Olaysız tahtada:
        /// donanıma bitişik Teknisyen'in sabotajı canı düşürür ve kırar (sivil kaybı yok, kaçış başlamaz); kepenk yürünemez;
        /// kepenge itilen makine 1 hasar alır ve yerinde kalır; ileri geçit ve menfez karelerine yürünür.
        /// </summary>
        [Test]
        public void NewTiles_DoNotBreakTheEngine([Range(1, 6)] int seed)
        {
            foreach (var room in Rooms)
            {
                int tier = 1 + (seed - 1) % 4;
                var p = Params(room, tier);
                if (seed % 2 == 0) p.Event = null;
                var B = Baskin.CreateRoom(p, room, Setup(room, seed));
                int turns = 0;
                while (B.Phase == "player" && turns < B.MaxTurns) { Baskin.EndTurn(B); turns++; }
                Assert.AreNotEqual("player", B.Phase, $"{room.Id}: {turns} turda bitmedi");

                if (room.Type == RoomType.Target) continue;
                Device(room, p, seed);
                Walkable(room, p, seed);
                if (room.Type == RoomType.Middle) Shutter(room, p, seed);
            }
        }

        /// <summary>Olaysız tahta, makineler devre dışı (testin kurduğu kareleri bozmasın).</summary>
        static Baskin.BState Quiet(FacilityRoom room, RaidParams p, int seed, int deviceHp = 0)
        {
            var q = Params(room, p.Tier);
            q.Event = null;
            var setup = Setup(room, seed);
            if (deviceHp > 0) setup.DeviceHp = deviceHp;
            var B = Baskin.CreateRoom(q, room, setup);
            foreach (var u in B.Units.Where(v => v.Side == 'e')) u.Dead = true;
            B.Spawns.Clear();
            return B;
        }

        static Baskin.Unit Tek(Baskin.BState B) => B.Units.First(u => u.Side == 'p' && u.Type == "tek");

        static void Device(FacilityRoom room, RaidParams p, int seed)
        {
            var dev = room.DeviceAt.Value;
            foreach (int hp in new[] { 3, 0 })
            {
                var B = Quiet(room, p, seed, hp);
                var tek = Tek(B);
                var o = B.Cells[dev.Y, dev.X].O;
                Assert.AreEqual(room.Device, o.K, room.Id);
                Assert.AreEqual(hp > 0 ? hp : Setup(room, seed).DeviceHp, o.Hp, room.Id);
                var side = Baskin.Dirs.Select(d => new Baskin.Pt(dev.X + d[0], dev.Y + d[1]))
                    .First(t => Open(B, t.X, t.Y) && (Baskin.UnitAt(B, t.X, t.Y) == null || Baskin.UnitAt(B, t.X, t.Y) == tek));
                tek.X = side.X; tek.Y = side.Y;
                int collateral = B.Collateral;
                Assert.IsTrue(Baskin.DoAct(B, tek.Id, dev.X, dev.Y), room.Id + ": sabotaj");
                if (hp == 3) Assert.AreEqual(1, B.Cells[dev.Y, dev.X].O?.Hp, room.Id + ": can düşmedi");
                else Assert.IsNull(B.Cells[dev.Y, dev.X].O, room.Id + ": donanım kırılmadı");
                Assert.AreEqual(collateral, B.Collateral, room.Id);
                Assert.IsFalse(B.Escape, room.Id);
                Assert.AreEqual("player", B.Phase, room.Id);
            }
        }

        static void Walkable(FacilityRoom room, RaidParams p, int seed)
        {
            var B = Quiet(room, p, seed);
            var tek = Tek(B);
            // İleri geçidin iki karesi bitişik: birinden öbürüne yürünür
            var f0 = room.Forward[0];
            var f1 = room.Forward[1];
            tek.X = f0.X; tek.Y = f0.Y;
            Assert.IsTrue(Baskin.Reachable(B, tek).Any(t => t.X == f1.X && t.Y == f1.Y), room.Id + ": geçit yürünmez");
            foreach (var v in room.Vents)
            {
                var from = Baskin.Dirs.Select(d => new Baskin.Pt(v.X + d[0], v.Y + d[1])).First(t => Open(B, t.X, t.Y) && (Baskin.UnitAt(B, t.X, t.Y) == null || Baskin.UnitAt(B, t.X, t.Y) == tek));
                tek.X = from.X; tek.Y = from.Y;
                Assert.IsTrue(Baskin.Reachable(B, tek).Any(t => t.X == v.X && t.Y == v.Y), $"{room.Id}: menfez {v.X},{v.Y} yürünmez");
            }
        }

        static void Shutter(FacilityRoom room, RaidParams p, int seed)
        {
            var sh = room.Shutter.Value;
            var B = Quiet(room, p, seed);
            Assert.IsTrue(B.Cells[sh.Y, sh.X].T == "wall" && B.Cells[sh.Y, sh.X].Shutter, room.Id);
            var tek = Tek(B);
            var a = room.ShutterDoors[0];
            tek.X = a.X; tek.Y = a.Y;
            Assert.IsFalse(Baskin.Reachable(B, tek).Any(t => t.X == sh.X && t.Y == sh.Y), room.Id + ": kepenge yürünür");
            // Kepengin bir yanında makine, onun ardında Teknisyen: sabotaj (1) ve kepenge çarpma (1)
            foreach (var door in room.ShutterDoors)
            {
                int bx = 2 * door.X - sh.X, by = 2 * door.Y - sh.Y;
                if (!In(bx, by)) continue;
                var b = Quiet(room, p, seed);
                var t = Tek(b);
                foreach (var u in b.Units.Where(v => v != t && (v.X == bx && v.Y == by || v.X == door.X && v.Y == door.Y))) u.Dead = true;
                b.Cells[by, bx].T = "floor";
                b.Cells[by, bx].O = null;
                var m = b.Units.First(u => u.Side == 'e');
                m.Dead = false; m.Type = "dron"; m.X = door.X; m.Y = door.Y; m.Hp = m.Max = 5; m.Armor = 0; m.Ice = 0;
                t.X = bx; t.Y = by;
                Assert.IsTrue(Baskin.DoAct(b, t.Id, door.X, door.Y), room.Id + ": sabotaj");
                Assert.AreEqual(3, m.Hp, room.Id + ": kepenge çarpan makine");
                Assert.IsTrue(m.X == door.X && m.Y == door.Y, room.Id + ": makine kepengi geçti");
                Assert.IsTrue(b.Cells[sh.Y, sh.X].T == "wall" && b.Cells[sh.Y, sh.X].Shutter, room.Id);
                return;
            }
            Assert.Fail(room.Id + ": kepengin iki yanında da itme yeri yok");
        }

        // ---------- Kademe ve olay ----------
        [Test]
        public void TierVariant_BoardShowsOnlyItsParts()
        {
            var rooms = Rooms.Where(r => r.Event == "tavan" || r.Event == "tup").ToList();
            Assert.IsNotEmpty(rooms);
            foreach (var room in rooms)
                foreach (int tier in new[] { 1, 2, 3, 4 })
                {
                    var B = Baskin.CreateRoom(Params(room, tier), room, Setup(room, 9));
                    var parts = room.PartsAt(tier).ToList();
                    if (room.Event == "tavan")
                    {
                        int cracks = 0;
                        for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (B.Cells[y, x].Crack) cracks++;
                        Assert.AreEqual(parts.Count(q => q.Kind == "catlak"), cracks, $"{room.Id} T{tier}");
                    }
                    else
                    {
                        int tubes = 0;
                        for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (B.Cells[y, x].O?.K == "tup") tubes++;
                        Assert.AreEqual(parts.Count(q => q.Kind == "tup"), B.Ev.Fuses.Count, $"{room.Id} T{tier}");
                        Assert.AreEqual(B.Ev.Fuses.Count, tubes, $"{room.Id} T{tier}");
                    }
                }
        }

        [Test]
        public void RegionEventsOff_UsesTheBasePlan()
        {
            foreach (var room in Rooms)
            {
                var p = Params(room, 3);
                p.Event = null;
                var B = Baskin.CreateRoom(p, room, Setup(room, 4));
                Assert.IsNull(B.Ev, room.Id);
                for (int y = 0; y < Baskin.N; y++)
                    for (int x = 0; x < Baskin.N; x++)
                    {
                        var c = B.Cells[y, x];
                        Assert.IsTrue(c.O?.K != "tup" && c.O?.K != "kol" && !c.Crack && !c.Shelf && c.Phase == 0, $"{room.Id}: {x},{y} şart parçası");
                        Assert.IsTrue(SameCell(c, room.BaseCells[y, x]), $"{room.Id}: {x},{y} şartsız plandan farklı");
                    }
            }
        }

        [Test]
        public void WrongEvent_Throws()
        {
            var middle = Rooms.First(r => r.Type == RoomType.Middle && r.Event != null && r.Event != "gaz");
            var p = Params(middle, 1);
            p.Event = "gaz";
            Assert.Throws<ArgumentException>(() => Baskin.CreateRoom(p, middle, Setup(middle, 1)));
            var entry = Rooms.First(r => r.Type == RoomType.Entry);
            var q = Params(entry, 1);
            q.Event = "gaz";
            Baskin.BState B = null;
            Assert.DoesNotThrow(() => B = Baskin.CreateRoom(q, entry, Setup(entry, 1)));
            Assert.IsNull(B.Ev);
        }

        [Test]
        public void MachineCounts_PerRoomType()
        {
            foreach (var room in Rooms)
                foreach (int tier in new[] { 1, 2, 3 })
                    foreach (bool reinforced in new[] { false, true })
                        foreach (int extraMachines in new[] { 0, 2 })
                        {
                            var p = Params(room, tier);
                            p.Traits = reinforced ? new List<string> { "takviye" } : new List<string>();
                            p.ExtraMachines = extraMachines;
                            var B = Baskin.RoomBoard(p, room, Setup(room, 2));
                            var foes = B.Units.Where(u => u.Side == 'e').ToList();
                            string at = $"{room.Id} T{tier} takviye={reinforced} ek={extraMachines}";
                            if (room.Type != RoomType.Target)
                            {
                                Assert.AreEqual(1, foes.Count, at);   // aşama 4 kararı: giriş ve ara oda yalnız bakım robotu
                                Assert.IsFalse(foes.Any(u => u.Type == p.Signature), at + ": G/A'da imza makinesi");
                                continue;
                            }
                            Assert.AreEqual(Math.Min(7, 3 + Math.Max(0, tier - 1) + (reinforced ? 1 : 0) + extraMachines), foes.Count, at);
                            Assert.GreaterOrEqual(foes.Count(u => u.Type == p.Signature), Math.Min(tier >= 3 ? 2 : 1, foes.Count), at + ": imza");
                        }
        }
    }
}
