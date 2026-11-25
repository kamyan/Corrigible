using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis baskınının oda zinciri (aşama 2): ilk oda, oda tohumları, kapıda bekleme ve GEÇ, kendiliğinden geçiş, taşınanlar ve
    /// diziliş, dönüş odası (donmuş oda, makine kaydırma, şartın baştan başlaması), temiz oda ve yürüyerek çıkış, tesisin girişi,
    /// süre sonları ve geri çekilme, hazırlık etkileri (pano, anten, vana), vana kuralı, makinelerin donanıma bakışı, kapıdakinin
    /// olaylardan etkilenmemesi, sonucun raporlardan toplanması ve simülasyon tarafı (emir, RNG, taşınmayan düşenin ölümü, bayrak
    /// kapalı). Düşen kişi, sürükleme ve ölüm kuralının ayrıntısı <see cref="FacilityFallenTests"/>'te.
    /// Motor testleri aksi yazılmadıkça olaysız kurulur ve makineleri kapatır (<see cref="FacilityKit.Calm"/>).
    /// </summary>
    public class FacilityChainTests
    {
        static Balance B0 => Bal(events: false);

        /// <summary>Zincir kuralları (geçiş, temiz oda, dönüş tahtası, makine niyeti) makineli odayla sınanır: giriş 2, ara 3 makine
        /// (aşama 4 kararından önceki değerler). Balance varsayılanı (1, 1) <c>FacilityRoomTests</c>'te kilitli.</summary>
        static Balance Bal(bool events = true)
        {
            var b = FacilityKit.Bal(events);
            b.facilityEntryMachines = 2;
            b.facilityMiddleMachines = 3;
            return b;
        }

        /// <summary>Olaysız kanat baskını ve G'nin tahtası.</summary>
        static (RaidParams P, List<RoomReport> Reps, Baskin.BState B) Entry(IReadOnlyList<FacilityRoom> wing, int seed = 11, int tier = 1, bool events = false)
        {
            var p = Params(wing, seed, tier, events);
            var reps = new List<RoomReport>();
            return (p, reps, Open(p, reps, Bal(events)));
        }

        /// <summary>Elle G raporu (geçildi) ve A'nın tahtası.</summary>
        static (RaidParams P, List<RoomReport> Reps, Baskin.BState B) Middle(IReadOnlyList<FacilityRoom> wing, int seed = 11, bool events = false, RaidParams p = null)
        {
            p = p ?? Params(wing, seed, 1, events);
            var reps = new List<RoomReport> { Rep(1, squad: RaidChain.SquadCount(p)) };
            return (p, reps, Open(p, reps, Bal(events)));
        }

        static bool In(int x, int y) => x >= 0 && y >= 0 && x < Baskin.N && y < Baskin.N;
        static bool Same(Baskin.Pt a, Baskin.Pt b) => a.X == b.X && a.Y == b.Y;
        static bool Free(Baskin.BState B, int x, int y) =>
            In(x, y) && Baskin.CellAt(B, x, y).T != "wall" && Baskin.CellAt(B, x, y).O == null && Baskin.UnitAt(B, x, y) == null;
        static List<Baskin.Unit> Foes(Baskin.BState B) => B.Units.Where(u => u.Side == 'e' && !u.Dead).ToList();

        /// <summary>Testin kendi BFS'i: duvar ve nesne geçilmez, birimler sayılmaz; kaynak geçilmez olsa da başlar.</summary>
        static int[,] Walk(Baskin.BState B, IEnumerable<Baskin.Pt> from, bool sourceAlways = false)
        {
            var d = new int[Baskin.N, Baskin.N];
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) d[y, x] = -1;
            var q = new Queue<Baskin.Pt>();
            foreach (var f in from)
                if (sourceAlways || Baskin.CellAt(B, f.X, f.Y).T != "wall" && Baskin.CellAt(B, f.X, f.Y).O == null) { d[f.Y, f.X] = 0; q.Enqueue(f); }
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var dd in Baskin.Dirs)
                {
                    int x = p.X + dd[0], y = p.Y + dd[1];
                    if (!In(x, y) || d[y, x] >= 0 || Baskin.CellAt(B, x, y).T == "wall" || Baskin.CellAt(B, x, y).O != null) continue;
                    d[y, x] = d[p.Y, p.X] + 1;
                    q.Enqueue(new Baskin.Pt(x, y));
                }
            }
            return d;
        }

        // ---------- İlk oda ve tohumlar ----------
        [Test]
        public void Open_FirstRoom_IsCreateRoomOfEntry([Values(1, 3)] int tier)
        {
            int n = 0;
            foreach (var wing in Wings)
            {
                var p = Params(wing, 500 + n++, tier);
                var b = Bal();
                var B = RaidChain.Open(b, Plan79, p, new List<RoomReport>());
                var C = Baskin.CreateRoom(p, wing[0], FacilityMaps.Setup(b, wing[0], RaidChain.RoomSeed(p.Seed, 1, false)));
                Assert.AreEqual(Baskin.Signature(C), Baskin.Signature(B), wing[0].Id);
                var team = B.Units.Where(u => u.Side == 'p').OrderBy(u => u.Id).ToList();
                CollectionAssert.AreEqual(Enumerable.Range(0, team.Count).ToList(), team.Select(u => u.Slot).ToList(), wing[0].Id + ": kadro sırası");
                Assert.IsTrue(B.Units.Where(u => u.Side == 'e').All(u => u.Slot == -1), wing[0].Id + ": makinede Slot −1");
                Assert.AreEqual(wing[0].West.Count, B.Exits.Count, wing[0].Id);
                for (int i = 0; i < B.Exits.Count; i++) Assert.IsTrue(Same(wing[0].West[i], B.Exits[i]), wing[0].Id + ": çıkışlar batı girişi");
            }
        }

        [Test]
        public void RoomSeeds_AreMixes()
        {
            foreach (int seed in new[] { 0, 1, 12345, -77, int.MaxValue })
            {
                var all = new List<int>();
                foreach (var (room, ret) in new[] { (1, false), (2, false), (3, false), (1, true), (2, true) })
                {
                    int s = RaidChain.RoomSeed(seed, room, ret);
                    Assert.AreEqual(unchecked((int)Rng.Mix((uint)seed, (uint)(room * 2 + (ret ? 1 : 0)))), s, $"{seed} {room} {ret}");
                    all.Add(s);
                }
                CollectionAssert.AllItemsAreUnique(all, seed.ToString());
            }
        }

        // ---------- Kapı ----------
        /// <summary>Kapı karesinin kenara dönük yönünde, araya bir şey girmeyen bir dron karesi: dronun o yöne atışı kareyi geçer.</summary>
        static (Baskin.Pt At, int Dir)? RayTo(Baskin.BState B, Baskin.Pt gate, Baskin.Unit skip)
        {
            for (int dir = 0; dir < 4; dir++)
            {
                var d = Baskin.Dirs[dir];
                if (In(gate.X + d[0], gate.Y + d[1])) continue;
                for (int k = 2; k < Baskin.N; k++)
                {
                    int x = gate.X - d[0] * k, y = gate.Y - d[1] * k;
                    if (!In(x, y)) break;
                    bool clear = true;
                    for (int j = 0; j < k && clear; j++)
                    {
                        int cx = gate.X - d[0] * j, cy = gate.Y - d[1] * j;
                        var u = Baskin.UnitAt(B, cx, cy);
                        clear = Baskin.CellAt(B, cx, cy).T != "wall" && Baskin.CellAt(B, cx, cy).O == null && (u == null || u == skip);
                    }
                    if (!clear) break;
                    if (Free(B, x, y)) return (new Baskin.Pt(x, y), dir);
                }
            }
            return null;
        }

        [Test]
        public void Gate_OwnWalk_TakesSpecialistOffBoard()
        {
            int tested = 0;
            foreach (var wing in Wings)
            {
                var (_, _, B) = Entry(wing);
                Calm(B, keep: 1);
                var tek = BySlot(B, 0);
                var gate = B.Room.Forward[0];
                var near = FreeNeighbor(B, gate);
                if (near == null) continue;
                tek.X = near.Value.X; tek.Y = near.Value.Y;
                var dron = Foes(B).Single();
                var ray = RayTo(B, gate, tek);
                if (ray == null) continue;
                dron.X = ray.Value.At.X; dron.Y = ray.Value.At.Y;
                dron.Intent = new Baskin.Intent { Kind = "shoot", Dir = ray.Value.Dir };
                Assert.IsTrue(Baskin.DoMove(B, tek.Id, gate.X, gate.Y), wing[0].Id);
                Assert.IsTrue(tek.AtGate, wing[0].Id);
                Assert.IsFalse(Baskin.OnBoard(tek));
                Assert.IsNull(Baskin.UnitAt(B, gate.X, gate.Y), "kapı karesi boşalır");
                Assert.IsFalse(Baskin.Alive(B, 'p').Contains(tek));
                var view = Baskin.Intents(B).Single(v => v.Unit == dron.Id);
                Assert.IsTrue(view.TargetNone, wing[0].Id + ": atış kapıdaki uzmanı hedeflemez");
                Assert.AreEqual("player", B.Phase);
                Assert.IsTrue(Baskin.Undo(B));
                tek = BySlot(B, 0);
                Assert.IsFalse(tek.AtGate);
                Assert.IsTrue(tek.X == near.Value.X && tek.Y == near.Value.Y, "geri alma eski kare");
                // Denetim: tahtada kapı karesinde duran uzman atışın hedefidir
                tek.X = gate.X; tek.Y = gate.Y;
                Assert.IsFalse(Baskin.Intents(B).Single(v => v.Unit == dron.Id).TargetNone);
                tested++;
            }
            Assert.Greater(tested, 10);
        }

        [Test]
        public void Gate_PlacedUnit_NeedsEnterGate()
        {
            foreach (var wing in Wings.Take(12))
            {
                var (_, _, B) = Entry(wing);
                Calm(B);
                var tek = BySlot(B, 0);
                var g = B.Room.Forward[1];
                tek.X = g.X; tek.Y = g.Y;
                Assert.IsFalse(tek.AtGate);
                Assert.AreSame(tek, Baskin.UnitAt(B, g.X, g.Y));
                Assert.IsFalse(tek.Moved);
                Assert.IsTrue(Baskin.EnterGate(B, tek.Id), wing[0].Id);
                Assert.IsTrue(tek.AtGate);
                Assert.IsFalse(tek.Moved, "EnterGate adım harcamaz");
                Assert.IsTrue(Baskin.Undo(B));
                Assert.IsFalse(BySlot(B, 0).AtGate);
                var other = BySlot(B, 1);
                Assert.IsFalse(Baskin.IsGateTile(B, other.X, other.Y));
                Assert.IsFalse(Baskin.EnterGate(B, other.Id), "kapı karesi olmayan karede");
            }
        }

        [Test]
        public void Gate_LastSpecialist_DoesNotLose()
        {
            foreach (var wing in Wings.Take(12))
            {
                var (_, _, B) = Entry(wing);
                Calm(B);
                ToGate(B);
                Assert.AreEqual("player", B.Phase, wing[0].Id);
                Assert.IsTrue(Baskin.CanPass(B));
                string sig = Baskin.Signature(B);
                Assert.IsFalse(Baskin.EndTurn(B), "herkes kapıdayken tur bitmez");
                Assert.AreEqual(sig, Baskin.Signature(B));
            }
        }

        [Test]
        public void Pass_NeedsEveryStandingSpecialist()
        {
            var (_, _, B) = Entry(Wings[0]);
            Calm(B);
            ToGate(B, BySlot(B, 0));
            ToGate(B, BySlot(B, 1));
            Assert.IsFalse(Baskin.CanPass(B));
            Assert.IsFalse(Baskin.Pass(B));
            Assert.AreEqual("player", B.Phase);
            BySlot(B, 2).Dead = true;
            Assert.IsTrue(Baskin.CanPass(B));
        }

        [Test]
        public void Pass_IsNotUndoable()
        {
            var (_, _, B) = Entry(Wings[3]);
            Calm(B);
            ToGate(B);
            Assert.IsTrue(Baskin.Pass(B));
            Assert.AreEqual("pass", B.Phase);
            Assert.AreEqual(0, B.Hist.Count);
            Assert.IsFalse(Baskin.Undo(B));
            Assert.IsFalse(Baskin.EndTurn(B));
            Assert.IsFalse(Baskin.Pass(B));
        }

        [Test]
        public void AutoPass_WhenLastBoardSpecialistFalls()
        {
            int tested = 0;
            foreach (var wing in Wings)
            {
                var (_, _, B) = Entry(wing);
                Calm(B, keep: 1);
                ToGate(B, BySlot(B, 0));
                ToGate(B, BySlot(B, 1));
                var last = BySlot(B, 2);
                last.Hp = 1; last.Armor = 0;
                var dron = Foes(B).Single();
                int dir = -1;
                for (int d = 0; d < 4 && dir < 0; d++)
                {
                    int x = last.X - Baskin.Dirs[d][0], y = last.Y - Baskin.Dirs[d][1];
                    if (Free(B, x, y)) { dron.X = x; dron.Y = y; dir = d; }
                }
                if (dir < 0) continue;
                dron.Intent = new Baskin.Intent { Kind = "shoot", Dir = dir };
                Assert.IsTrue(Baskin.EndTurn(B), wing[0].Id);
                Assert.IsTrue(last.Dead, wing[0].Id);
                Assert.AreEqual("pass", B.Phase, wing[0].Id + ": kalanlar kapıda, oda kendiliğinden geçilir");
                tested++;
            }
            Assert.Greater(tested, 10);
        }

        // ---------- Taşınanlar ----------
        [Test]
        public void Carry_HpPlateCellsCharges_AndClearsLocks()
        {
            var wing = Wings[4];
            var p = Params(wing, 31, 1, false);
            p.Squad = new List<SquadSlot>
            {
                new SquadSlot { Type = "tek", Hp = 3, Armor = 1 },
                new SquadSlot { Type = "emp", Hp = 4, EmpBoosts = 2, EmpBoostDamage = 1 },
                new SquadSlot { Type = "maden", Hp = 3 },
                new SquadSlot { Type = "ana", Hp = 3 },
            };
            p.DynamiteCharges = 2;
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            Calm(B);
            var tek = BySlot(B, 0); var emp = BySlot(B, 1); var maden = BySlot(B, 2); var ana = BySlot(B, 3);
            Assert.AreEqual(2, emp.EmpBoosts);
            Assert.AreEqual(2, maden.Charges);
            tek.Hp = 1; tek.Armor = 0; tek.Locked = true; tek.Frozen = true; tek.Ice = 1;
            emp.EmpBoosts = 1;
            maden.Charges = 1;
            ana.Dead = true; ana.Hp = 0;
            int ax = ana.X, ay = ana.Y;
            var r = PassRoom(p, reps, B);
            Assert.AreEqual("d", r.Team[3].State);
            Assert.AreEqual(1, r.Team[3].Fell);
            Assert.AreEqual(ax, r.Team[3].X);
            Assert.AreEqual(ay, r.Team[3].Y);
            Assert.AreEqual(0, r.Team[3].Hp);
            var A = Open(p, reps, B0);
            Assert.AreEqual(2, A.Room.Index);
            var t2 = BySlot(A, 0);
            Assert.AreEqual(1, t2.Hp);
            Assert.AreEqual(0, t2.Armor);
            Assert.IsFalse(t2.Locked);
            Assert.IsFalse(t2.Frozen);
            Assert.AreEqual(0, t2.Ice);
            Assert.IsFalse(t2.AtGate);
            Assert.AreEqual(1, BySlot(A, 1).EmpBoosts);
            Assert.AreEqual(1, BySlot(A, 2).Charges);
            Assert.IsNull(BySlot(A, 3), "düşen kişi sonraki odaya konmaz");
            Assert.AreEqual(3, A.Units.Count(u => u.Side == 'p'));
            // Sonraki raporda düşen kişi aynen kopyalanır
            Calm(A);
            var r2 = PassRoom(p, reps, A);
            Assert.AreEqual(r.Team[3].State + r.Team[3].Fell + r.Team[3].X + r.Team[3].Y, r2.Team[3].State + r2.Team[3].Fell + r2.Team[3].X + r2.Team[3].Y);
        }

        [Test]
        public void NewRoom_SquadOnWestThenNearest()
        {
            foreach (var wing in Wings)
            {
                foreach (bool three in new[] { false, true })
                {
                    var p = Params(wing, 41, 1, true);
                    var g = Rep(1);
                    if (!three) g.Team[1].State = "d";
                    var reps = new List<RoomReport> { g };
                    var B = Open(p, reps);
                    var room = B.Room;
                    var team = Standing(B);
                    Assert.AreEqual(three ? 3 : 2, team.Count, room.Id);
                    Assert.IsTrue(Same(room.West[0], new Baskin.Pt(team[0].X, team[0].Y)), room.Id);
                    Assert.IsTrue(Same(room.West[1], new Baskin.Pt(team[1].X, team[1].Y)), room.Id);
                    if (!three) { Assert.AreEqual(2, team[1].Slot); continue; }
                    var s = Walk(B, room.West);
                    var parts = room.Event != null ? new HashSet<int>(room.PartTiles(3)) : new HashSet<int>();
                    (int x, int y)? best = null;
                    for (int y = 0; y < Baskin.N; y++)
                        for (int x = 0; x < Baskin.N; x++)
                        {
                            var c = B.Cells[y, x];
                            if (c.T != "floor" || c.O != null || c.Door != 0 || c.Vent != 0 || parts.Contains(y * Baskin.N + x) || s[y, x] < 0) continue;
                            if (room.West.Take(2).Any(w => w.X == x && w.Y == y)) continue;
                            if (best == null || s[y, x] < s[best.Value.y, best.Value.x]) best = (x, y);
                        }
                    Assert.AreEqual(best, (team[2].X, team[2].Y), room.Id + ": üçüncü kişi");
                }
            }
        }

        [Test]
        public void Machines_DoNotStartOnSquadTiles_WithCarry([Range(1, 20)] int seed)
        {
            foreach (var wing in Wings)
            {
                var p = Params(wing, seed * 7919, seed % 2 == 0 ? 3 : 1, true);
                var g = Rep(1);
                if (seed % 3 == 0) g.Team[seed % 2].State = "d";
                var reps = new List<RoomReport> { g };
                foreach (int idx in new[] { 2, 3 })
                {
                    var room = wing[idx - 1];
                    var setup = RaidChain.Setup(Bal(), room, p, reps, false);
                    var B = Baskin.RoomBoard(p, room, setup);
                    var squad = B.Units.Where(u => u.Side == 'p').ToList();
                    for (int i = 0; i < g.Team.Count; i++)
                        Assert.AreEqual(g.Team[i].State == "a", squad.Any(u => u.Slot == i), $"{room.Id}: {i}. kişi");
                    foreach (var e in B.Units.Where(u => u.Side == 'e'))
                    {
                        Assert.IsFalse(squad.Any(u => u.X == e.X && u.Y == e.Y), $"{room.Id}: makine kadro karesinde");
                        Assert.IsFalse(room.Forward.Any(t => t.X == e.X && t.Y == e.Y), $"{room.Id}: makine ileri geçitte");
                        Assert.IsFalse(room.West.Any(t => t.X == e.X && t.Y == e.Y), $"{room.Id}: makine batı girişinde");
                    }
                    reps.Add(Rep(idx, squad: 3));
                    reps[reps.Count - 1].Team = g.Team.Select(m => new RoomMember { State = m.State, Hp = m.Hp }).ToList();
                }
            }
        }

        // ---------- Dönüş odası ----------
        /// <summary>G (makineler kalır) ve A geçilir; A'nın tahtası değiştirilebilir. H'de kaçış başlar, geçilir.</summary>
        static Baskin.BState ToReturn(RaidParams p, List<RoomReport> reps, Balance b, Action<Baskin.BState> middle = null, bool calmEntry = false)
        {
            var G = Open(p, reps, b);
            if (calmEntry) Calm(G); else ClearGate(G);
            PassRoom(p, reps, G);
            var A = Open(p, reps, b);
            middle?.Invoke(A);
            ClearGate(A);
            PassRoom(p, reps, A);
            var H = Open(p, reps, b);
            StartEscape(H);
            Calm(H);
            PassRoom(p, reps, H);
            return Open(p, reps, b);
        }

        [Test]
        public void Return_RebuildsFrozenRoom()
        {
            int n = 0;
            foreach (var wing in Wings)
            {
                var p = Params(wing, 900 + n++, 1, false);
                var reps = new List<RoomReport>();
                Baskin.Pt civ = default, spawner = default;
                var R = ToReturn(p, reps, B0, A =>
                {
                    var m = Foes(A).Where(u => !Baskin.IsGateTile(A, u.X, u.Y)).OrderByDescending(u => u.Max).First();
                    m.Hp = 1;
                    var civs = Enumerable.Range(0, 64).Select(k => new Baskin.Pt(k % 8, k / 8)).Where(t => Baskin.CellAt(A, t.X, t.Y).O?.K == "civ").ToList();
                    civ = civs[0];
                    Baskin.CellAt(A, civ.X, civ.Y).O = null;
                    spawner = Enumerable.Range(0, 64).Select(k => new Baskin.Pt(k % 8, k / 8)).First(t => Baskin.CellAt(A, t.X, t.Y).O?.K == "spawner");
                });
                var a = reps[1];
                Assert.IsTrue(a.Machines.Any(m => m.Hp == 1), wing[1].Id);
                Assert.AreEqual(2, R.Room.Index, wing[1].Id);
                var foes = Foes(R);
                Assert.AreEqual(a.Machines.Count, foes.Count, wing[1].Id);
                for (int i = 0; i < foes.Count; i++)
                {
                    Assert.AreEqual(a.Machines[i].Type, foes[i].Type, wing[1].Id);
                    Assert.AreEqual(a.Machines[i].Hp, foes[i].Hp, wing[1].Id);
                    Assert.AreEqual(a.Machines[i].Armor, foes[i].Armor, wing[1].Id);
                }
                Assert.IsNull(Baskin.CellAt(R, civ.X, civ.Y).O, wing[1].Id + ": kırılan sivil yok");
                Assert.AreEqual("spawner", Baskin.CellAt(R, spawner.X, spawner.Y).O?.K, wing[1].Id + ": kopya dolabı var");
                var team = Standing(R);
                Assert.IsTrue(Same(R.Room.Forward[0], new Baskin.Pt(team[0].X, team[0].Y)), wing[1].Id + ": doğudan girer");
                Assert.IsTrue(Same(R.Room.Forward[1], new Baskin.Pt(team[1].X, team[1].Y)), wing[1].Id);
                Assert.AreEqual(B0.facilityReturnTurns, R.MaxTurns);
                Assert.IsTrue(R.Escape);
                Assert.AreEqual(R.Room.West.Count, R.Exits.Count);
                for (int i = 0; i < R.Exits.Count; i++) Assert.IsTrue(Same(R.Room.West[i], R.Exits[i]));
                Assert.AreEqual(1, R.Turn);
                Assert.AreEqual("player", R.Phase);
            }
        }

        [Test]
        public void Return_ShiftsMachineOffGate()
        {
            foreach (var wing in Wings)
            {
                var p = Params(wing, 77, 1, false);
                var reps = new List<RoomReport>();
                var G = Open(p, reps, B0); ClearGate(G); PassRoom(p, reps, G);
                var A = Open(p, reps, B0); Calm(A); PassRoom(p, reps, A);
                var f0 = A.Room.Forward[0];
                reps[1].Machines = new List<RoomMachine> { new RoomMachine { Type = "dron", X = f0.X, Y = f0.Y, Hp = 2 } };
                var H = Open(p, reps, B0); StartEscape(H); Calm(H); PassRoom(p, reps, H);
                Assert.AreEqual(new RoomStep(2, true), RaidChain.Next(p, reps).Value);
                Assert.AreEqual(1, Foes(Open(p, reps, B0)).Count, wing[1].Id + ": makine sayısı aynı");
                // Başlangıç yeri dönüş tahtasından (CreateRoom'un planı makineyi yürütür)
                var R = Baskin.ReturnBoard(p, wing[1], RaidChain.Setup(B0, wing[1], p, reps, true));
                var foes = Foes(R);
                Assert.AreEqual(1, foes.Count, wing[1].Id);
                var m = foes[0];
                Assert.AreNotEqual(2, Baskin.CellAt(R, m.X, m.Y).Door, wing[1].Id + ": makine geçitte kalmaz");
                // Beklenen kare: kaynaktan BFS, en küçük uzaklık, eşitlikte küçük y sonra küçük x; kadro karesi, geçit, menfez değil
                var squad = R.Units.Where(u => u.Side == 'p').Select(u => u.Y * Baskin.N + u.X).ToList();
                var d = Walk(R, new[] { f0 }, sourceAlways: true);
                (int x, int y)? best = null;
                for (int y = 0; y < Baskin.N; y++)
                    for (int x = 0; x < Baskin.N; x++)
                    {
                        var c = R.Cells[y, x];
                        if (d[y, x] < 0 || c.T == "wall" || c.O != null || c.Door != 0 || c.Vent != 0 || squad.Contains(y * Baskin.N + x)) continue;
                        if (best == null || d[y, x] < d[best.Value.y, best.Value.x]) best = (x, y);
                    }
                Assert.AreEqual(best, (m.X, m.Y), wing[1].Id);
                Assert.AreEqual("dron", m.Type);
                Assert.AreEqual(2, m.Hp);
            }
        }

        [Test]
        public void Return_EventRestartsFromPlan()
        {
            int tested = 0;
            foreach (var wing in Wings.Where(w => w[1].Event == "raf" || w[1].Event == "tup"))
            {
                var p = Params(wing, 123, 1, true);
                var b = Bal(true);
                var reps = new List<RoomReport>();
                Baskin.Pt rubble = default;
                var R = ToReturn(p, reps, b, A =>
                {
                    Calm(A);
                    // Gidişte şart bozulur (olay nesneleri kalkar, raflar söner); rapor bunları taşımaz
                    for (int y = 0; y < Baskin.N; y++)
                        for (int x = 0; x < Baskin.N; x++)
                        {
                            var c = Baskin.CellAt(A, x, y);
                            if (c.O != null && Baskin.IsEventObject(c.O.K)) c.O = null;
                            c.Shelf = false;
                        }
                    rubble = Enumerable.Range(0, 64).Select(k => new Baskin.Pt(k % 8, k / 8))
                        .First(t => t.X > 0 && t.Y > 0 && t.X < 7 && t.Y < 7 && Baskin.CellAt(A, t.X, t.Y).T == "wall" && !A.Room.Cells1[t.Y, t.X].Shelf);
                    Baskin.CellAt(A, rubble.X, rubble.Y).T = "floor";
                    A.Rubble.Add(rubble);
                }, calmEntry: true);
                Assert.AreEqual(2, R.Room.Index, wing[1].Id);
                CollectionAssert.Contains(reps[1].Rubble, rubble.Y * Baskin.N + rubble.X);
                var plan = R.Room.Cells1;
                for (int y = 0; y < Baskin.N; y++)
                    for (int x = 0; x < Baskin.N; x++)
                    {
                        var c = R.Cells[y, x]; var s = plan[y, x];
                        string at = $"{wing[1].Id} ({x},{y})";
                        Assert.AreEqual(x == rubble.X && y == rubble.Y ? "floor" : s.T, c.T, at);
                        Assert.AreEqual(s.Shelf, c.Shelf, at + " raf");
                        Assert.AreEqual(s.Crack, c.Crack, at);
                        Assert.AreEqual(s.Door, c.Door, at);
                        Assert.AreEqual(s.Vent, c.Vent, at);
                        Assert.AreEqual(s.Shutter, c.Shutter, at);
                        Assert.AreEqual(s.Phase, c.Phase, at);
                        bool evPlan = s.O != null && Baskin.IsEventObject(s.O.K), evNow = c.O != null && Baskin.IsEventObject(c.O.K);
                        Assert.AreEqual(evPlan, evNow, at + " olay nesnesi");
                        if (evPlan) Assert.AreEqual(s.O.K, c.O.K, at);
                    }
                Assert.IsTrue(R.Rubble.Any(t => Same(t, rubble)), wing[1].Id + ": yıkılan duvar kalıcı");
                Assert.IsNotNull(R.Ev, wing[1].Id);
                Assert.AreEqual(wing[1].Event, R.Ev.Id);
                tested++;
            }
            Assert.AreEqual(6, tested);
        }

        // ---------- Temiz oda ve yürüyerek çıkış ----------
        static void Clean(Baskin.BState B)
        {
            Calm(B);
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    if (Baskin.CellAt(B, x, y).O?.K == "spawner") Baskin.CellAt(B, x, y).O = null;
        }

        [Test]
        public void CleanRoom_IsWalked()
        {
            foreach (var wing in Wings.Take(9))
                foreach (bool entryClean in new[] { false, true })
                {
                    var p = Params(wing, 61, 1, false);
                    var reps = new List<RoomReport>();
                    var G = Open(p, reps, B0);
                    if (entryClean) Calm(G); else ClearGate(G);
                    var g = PassRoom(p, reps, G);
                    Assert.AreEqual(entryClean, RaidChain.IsClean(g), wing[0].Id);
                    var A = Open(p, reps, B0); Clean(A);
                    var a = PassRoom(p, reps, A);
                    Assert.IsTrue(RaidChain.IsClean(a));
                    var H = Open(p, reps, B0); StartEscape(H); Calm(H);
                    var h = PassRoom(p, reps, H);
                    var next = RaidChain.Next(p, reps);
                    if (entryClean)
                    {
                        Assert.IsNull(next, wing[0].Id);
                        Assert.IsTrue(h.Last);
                    }
                    else
                    {
                        Assert.AreEqual(new RoomStep(1, true), next.Value, wing[0].Id + ": A atlanır, G dönüşü");
                        Assert.IsFalse(h.Last);
                        var R = Open(p, reps, B0);
                        Assert.AreEqual(1, R.Room.Index);
                    }
                    Assert.AreEqual(1, reps.Count(r => r.Room == 2), "A için ikinci rapor yok");
                }
        }

        [Test]
        public void WalkOut_ServerWins_LootNeedsCargo()
        {
            foreach (var sector in new[] { "na_3", "sa_3", "na_1", "sa_1" })
                foreach (bool lose in new[] { false, true })
                {
                    var wing = Wing(sector);
                    var p = Params(wing, 71, 1, false);
                    bool loot = p.MissionKind == "loot";
                    if (lose && !loot) continue;
                    var reps = new List<RoomReport>();
                    var G = Open(p, reps, B0); Calm(G); PassRoom(p, reps, G);
                    var A = Open(p, reps, B0); Clean(A); PassRoom(p, reps, A);
                    var H = Open(p, reps, B0); StartEscape(H); Calm(H);
                    if (lose) { var c = H.Units.First(u => u.Id == H.Carrier); c.Dead = true; c.Hp = 0; }
                    var h = PassRoom(p, reps, H);
                    Assert.AreEqual("pass", h.End);
                    Assert.IsTrue(h.Last, sector);
                    Assert.IsNull(RaidChain.Next(p, reps));
                    if (!loot) { Assert.IsTrue(h.Success, sector); Assert.AreEqual(-1, h.Cargo); }
                    else if (!lose) { Assert.AreEqual(0, h.Cargo); Assert.IsTrue(h.Success, sector + ": yük kapıda"); }
                    else { Assert.AreEqual(-2, h.Cargo); Assert.IsFalse(h.Success, sector + ": yük kayboldu"); }
                    Assert.AreEqual(h.Success, RaidChain.Result(p, reps).Success);
                }
        }

        [Test]
        public void FinalExit_OwnWalkOnly()
        {
            int tested = 0;
            foreach (var wing in Wings.Where(w => w[0].Mission == "server"))
            {
                var p = Params(wing, 81, 1, false);
                var reps = new List<RoomReport>();
                var G = Open(p, reps, B0); ClearGate(G); PassRoom(p, reps, G);
                var A = Open(p, reps, B0); Clean(A); PassRoom(p, reps, A);
                var H = Open(p, reps, B0); StartEscape(H); Calm(H); PassRoom(p, reps, H);
                Assert.AreEqual(new RoomStep(1, true), RaidChain.Next(p, reps).Value);
                var R = Open(p, reps, B0);
                Calm(R);
                Assert.IsTrue(Baskin.IsFinalExit(R));
                Assert.IsFalse(Baskin.CanPass(R));
                var w0 = R.Room.West[0];
                var u0 = BySlot(R, 0);
                var near = FreeNeighbor(R, w0);
                if (near == null) continue;
                u0.X = near.Value.X; u0.Y = near.Value.Y;
                Assert.IsTrue(Baskin.DoMove(R, u0.Id, w0.X, w0.Y), wing[0].Id);
                Assert.IsTrue(u0.Out, "kendi yürüyüşüyle tahliye");
                Assert.IsFalse(u0.AtGate);
                var u1 = BySlot(R, 1);
                var w1 = R.Room.West[1];
                u1.X = w1.X; u1.Y = w1.Y;
                Assert.IsTrue(Baskin.OnBoard(u1), "konan uzman tahtada");
                Assert.IsFalse(u1.Out);
                Assert.AreEqual("player", R.Phase);
                Assert.IsTrue(Baskin.EnterGate(R, u1.Id));
                Assert.IsTrue(u1.Out);
                ToGate(R);
                Assert.AreEqual("won", R.Phase, wing[0].Id);
                var r = File(p, reps, R);
                Assert.IsTrue(r.Last);
                Assert.IsTrue(r.Success);
                Assert.AreEqual("won", r.End);
                Assert.IsTrue(r.Team.All(m => m.State == "a"));
                tested++;
            }
            Assert.Greater(tested, 6);
        }

        // ---------- Süre ve geri çekilme ----------
        static void RunOut(Baskin.BState B)
        {
            for (int guard = 0; guard < 20 && B.Phase == "player"; guard++) Assert.IsTrue(Baskin.EndTurn(B));
        }

        [Test]
        public void Timeout_EntryAndMiddle_IsWithdrawal()
        {
            foreach (var wing in Wings.Take(6))
                foreach (int idx in new[] { 1, 2 })
                {
                    var p = Params(wing, 91, 1, false);
                    var reps = new List<RoomReport>();
                    var B = Open(p, reps, B0);
                    if (idx == 2) { Calm(B); PassRoom(p, reps, B); B = Open(p, reps, B0); }
                    Calm(B);
                    for (int i = 0; i < B.MaxTurns; i++) { Assert.AreEqual("player", B.Phase); Assert.IsTrue(Baskin.EndTurn(B)); }
                    Assert.AreEqual("lost", B.Phase, wing[0].Id);
                    Assert.IsTrue(B.Retreated);
                    Assert.AreEqual(Loc.T("Süre doldu: ekip geri çekildi."), B.OutcomeReason);
                    var r = File(p, reps, B);
                    Assert.AreEqual(idx, r.Room);
                    Assert.IsTrue(r.Retreat);
                    Assert.IsTrue(r.Last);
                    Assert.IsFalse(r.Success);
                    Assert.AreEqual(B.MaxTurns, r.Turns);
                    Assert.IsTrue(r.Team.All(m => m.State == "a"), "herkes ayakta döner");
                    var res = RaidChain.Result(p, reps);
                    Assert.IsTrue(res.Retreat);
                    Assert.IsFalse(res.Success);
                    Assert.AreEqual(0, res.Down);
                }
        }

        /// <summary>
        /// Sürüm 10: giriş ya da ara odada süre dolarken kapıda bekleyen varsa baskın bitmez: kapıdakiler geçer, kapıya varamayan uzman
        /// sağ geri çekilir ("w", canıyla; sonraki odalara girmez, düşmüş sayılmaz). Sonraki oda yalnız kapıdakilerle açılır.
        /// </summary>
        [Test]
        public void Timeout_EntryAndMiddle_SomeAtGate_PassWithThem()
        {
            foreach (var wing in Wings.Take(6))
                foreach (int idx in new[] { 1, 2 })
                {
                    var p = Params(wing, 91, 1, false);
                    var reps = new List<RoomReport>();
                    var B = Open(p, reps, B0);
                    if (idx == 2) { Calm(B); PassRoom(p, reps, B); B = Open(p, reps, B0); }
                    Calm(B);
                    var team = B.Units.Where(Baskin.IsSpecialist).OrderBy(u => u.Slot).ToList();
                    var stays = team[team.Count - 1];
                    foreach (var u in team.Take(team.Count - 1)) ToGate(B, u);
                    int hp = stays.Hp;
                    for (int i = 0; i < B.MaxTurns && B.Phase == "player"; i++) Assert.IsTrue(Baskin.EndTurn(B), wing[0].Id);
                    Assert.AreEqual("pass", B.Phase, $"{wing[0].Id} oda {idx}: kapıdakilerle geçilir");
                    Assert.IsFalse(B.Retreated);
                    Assert.IsTrue(stays.Withdrew && !stays.Dead && !Baskin.OnBoard(stays), "kapıya varamayan geri çekildi");
                    var r = File(p, reps, B);
                    Assert.IsFalse(r.Last, "baskın sürer");
                    Assert.AreEqual("w", r.Team[stays.Slot].State);
                    Assert.AreEqual(hp, r.Team[stays.Slot].Hp, "canıyla döner");
                    Assert.IsTrue(r.Team.Where((m, i) => i != stays.Slot).All(m => m.State == "a"));
                    var next = Open(p, reps, B0);
                    Assert.IsFalse(next.Units.Any(u => Baskin.IsSpecialist(u) && u.Slot == stays.Slot), "geri çekilen sonraki odaya girmez");
                    Assert.AreEqual(team.Count - 1, next.Units.Count(Baskin.IsSpecialist));
                }
        }

        /// <summary>Sürüm 10: geri çekilen uzman baskının sonucunda ayakta ve canıyla sayılır (Down'a girmez).</summary>
        [Test]
        public void Withdrawn_CountsAsUpInResult()
        {
            var wing = Wings.First();
            var p = Params(wing, 92, 1, false);
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            Calm(B);
            var team = B.Units.Where(Baskin.IsSpecialist).OrderBy(u => u.Slot).ToList();
            var stays = team[team.Count - 1];
            foreach (var u in team.Take(team.Count - 1)) ToGate(B, u);
            for (int i = 0; i < B.MaxTurns && B.Phase == "player"; i++) Baskin.EndTurn(B);
            File(p, reps, B);
            // Ara odada da süre dolar ve kapıda kimse yoktur: ekip geri çekilir, baskın biter
            B = Open(p, reps, B0);
            Calm(B);
            for (int i = 0; i < B.MaxTurns && B.Phase == "player"; i++) Baskin.EndTurn(B);
            Assert.AreEqual("lost", B.Phase);
            var r = File(p, reps, B);
            Assert.IsTrue(r.Last && r.Retreat);
            var res = RaidChain.Result(p, reps);
            Assert.AreEqual(0, res.Down);
            Assert.AreEqual(stays.Hp, res.Hp[stays.Slot]);
            Assert.IsFalse(res.Left.Contains(stays.Slot));
        }

        [Test]
        public void Timeout_Target_AsToday()
        {
            var today = new[]
            {
                Loc.T("Süre doldu: YZ ikmal deposunu kilitledi."), Loc.T("Süre doldu: YZ çekirdeğini binadan taşıdı."),
                Loc.T("Süre doldu: YZ sunucularını yedekledi ve binayı kilitledi."),
            };
            foreach (var wing in Wings.Take(6))
            {
                var (p, reps, B) = Middle(wing, 93);
                Calm(B); PassRoom(p, reps, B);
                var H = Open(p, reps, B0);
                Calm(H);
                RunOut(H);
                Assert.AreEqual("lost", H.Phase, wing[2].Id);
                Assert.IsFalse(H.Retreated);
                CollectionAssert.Contains(today, H.OutcomeReason);
                var r = File(p, reps, H);
                Assert.IsTrue(r.Last);
                Assert.IsFalse(r.Retreat);
            }
        }

        [Test]
        public void Timeout_Escape_GateGoesOn()
        {
            foreach (var wing in Wings.Take(9))
            {
                var p = Params(wing, 95, 1, false);
                var reps = new List<RoomReport>();
                var R = ToReturn(p, reps, B0);
                Assert.AreEqual(2, R.Room.Index);
                Calm(R);
                ToGate(R, BySlot(R, 0));
                RunOut(R);
                Assert.AreEqual("pass", R.Phase, wing[1].Id + ": kapıdaki geçer");
                Assert.IsTrue(BySlot(R, 1).Left);
                Assert.IsTrue(BySlot(R, 2).Left);
                var r = File(p, reps, R);
                Assert.AreEqual("pass", r.End);
                CollectionAssert.AreEqual(new[] { "a", "l", "l" }, r.Team.Select(m => m.State).ToArray());
                Assert.AreEqual(2, r.Team[1].Fell);
                Assert.AreEqual(new RoomStep(1, true), RaidChain.Next(p, reps).Value, "G temiz değil: dönüş sürer");
            }
        }

        [Test]
        public void Timeout_Escape_NobodyAtGate_Ends()
        {
            foreach (var sector in new[] { "na_3", "weu_3", "na_1", "weu_1" })
            {
                var wing = Wing(sector);
                var (p, reps, A) = Middle(wing, 97);
                Calm(A); PassRoom(p, reps, A);
                var H = Open(p, reps, B0);
                StartEscape(H); Calm(H);
                RunOut(H);
                bool loot = p.MissionKind == "loot";
                Assert.AreEqual(loot ? "lost" : "won", H.Phase, sector);
                var r = File(p, reps, H);
                Assert.IsTrue(r.Last);
                Assert.IsTrue(r.Team.All(m => m.State == "l"), sector);
                var res = RaidChain.Result(p, reps);
                Assert.AreEqual(!loot, res.Success);
                CollectionAssert.AreEqual(new[] { 0, 1, 2 }, res.Left, sector + ": Left dolu");
                Assert.AreEqual(3, res.Down);
            }
        }

        [Test]
        public void Retreat_OutboundOnly()
        {
            var wing = Wings[7];
            for (int idx = 1; idx <= 3; idx++)
            {
                var p = Params(wing, 99, 1, false);
                var reps = new List<RoomReport>();
                var B = Open(p, reps, B0);
                for (int k = 1; k < idx; k++) { Calm(B); PassRoom(p, reps, B); B = Open(p, reps, B0); }
                Assert.IsTrue(Baskin.Retreat(B), "gidiş " + idx);
                var r = File(p, reps, B);
                Assert.IsTrue(r.Retreat);
                Assert.IsTrue(r.Last);
                Assert.AreEqual("lost", r.End);
                Assert.IsTrue(RaidChain.Result(p, reps).Retreat);
            }
            var p2 = Params(wing, 99, 1, false);
            var R = ToReturn(p2, new List<RoomReport>(), B0);
            Assert.IsTrue(R.Escape);
            Assert.IsFalse(Baskin.Retreat(R), "dönüş odasında geri çekilme yok");
            Assert.AreEqual("player", R.Phase);
        }

        // ---------- Hazırlık ----------
        static RaidParams NoSafe(RaidParams p) { p.Traits.Remove("guvenli"); return p; }

        [Test]
        public void Prep_Panel_QuietTurns()
        {
            int tested = 0;
            foreach (var wing in Wings.Where(w => w[0].Device == "pano"))
            {
                var p = NoSafe(Params(wing, 101, 1, false));
                var broken = new List<RoomReport> { Rep(1, device: "pano"), Rep(2) };
                var H = Open(p, broken, B0);
                Assert.AreEqual(B0.facilityPanelQuietTurns, H.QuietTurns, wing[2].Id);
                Assert.AreEqual(0, H.Spawns.Count, wing[2].Id + ": tur 1");
                int spt = H.SpawnPerTurn;
                Calm(H);
                H.SpawnPerTurn = spt;
                Assert.IsTrue(Baskin.EndTurn(H));
                Assert.AreEqual(2, H.Turn);
                Assert.AreEqual(0, H.Spawns.Count, wing[2].Id + ": tur 2");
                Assert.IsTrue(Baskin.EndTurn(H));
                Assert.AreEqual(3, H.Turn);
                bool spawner = Enumerable.Range(0, 64).Any(k => Baskin.CellAt(H, k % 8, k / 8).O?.K == "spawner");
                Assert.IsTrue(spawner);
                Assert.Greater(H.Spawns.Count, 0, wing[2].Id + ": tur 3");
                var sound = Open(p, new List<RoomReport> { Rep(1), Rep(2) }, B0);
                Assert.AreEqual(0, sound.QuietTurns);
                Assert.Greater(sound.Spawns.Count, 0, wing[2].Id + ": pano sağlamken tur 1");
                tested++;
            }
            Assert.Greater(tested, 10);
        }

        [Test]
        public void Prep_Antenna_OneMachineLess([Values(1, 3)] int tier)
        {
            int tested = 0;
            foreach (var wing in Wings.Where(w => w.Any(r => r.Device == "anten")))
            {
                var p = Params(wing, 103, tier, false);
                if (tier == 1 && p.Traits.Contains("takviye")) continue;
                int at = wing[0].Device == "anten" ? 1 : 2;
                var sound = new List<RoomReport> { Rep(1), Rep(2) };
                var broken = new List<RoomReport> { Rep(1, device: at == 1 ? "anten" : ""), Rep(2, device: at == 2 ? "anten" : "") };
                var room = wing[2];
                var S = Baskin.RoomBoard(p, room, RaidChain.Setup(B0, room, p, sound, false));
                var K = Baskin.RoomBoard(p, room, RaidChain.Setup(B0, room, p, broken, false));
                Assert.AreEqual(B0.facilityAntennaMachines, RaidChain.Setup(B0, room, p, broken, false).MachinesLess);
                int Count(Baskin.BState B) => B.Units.Count(u => u.Side == 'e');
                (int Start, int Extra) Split(Baskin.BState B)
                {
                    int first = B.Units.Where(u => u.Side == 'p').Min(u => u.Id);
                    return (B.Units.Count(u => u.Side == 'e' && u.Id < first), B.Units.Count(u => u.Side == 'e' && u.Id > first));
                }
                Assert.AreEqual(Count(S) - 1, Count(K), room.Id);
                var (s0, s1) = Split(S); var (k0, k1) = Split(K);
                if (tier == 3) { Assert.AreEqual(s0, k0, room.Id + ": başlangıç aynı"); Assert.AreEqual(s1 - 1, k1, room.Id + ": ekten"); }
                else { Assert.AreEqual(s0 - 1, k0, room.Id + ": başlangıç dronundan"); Assert.AreEqual(0, k1); }
                Assert.IsTrue(K.Units.Any(u => u.Side == 'e' && u.Type == "bakim"), room.Id + ": bakım robotu kalır");
                Assert.IsTrue(K.Log.Any(l => l.Text == Loc.F("Çağrı anteni kırık: {0} makine eksik geldi.", 1)));
                tested++;
            }
            Assert.Greater(tested, 8);
        }

        [Test]
        public void Prep_Antenna_NoWakeOnReturn()
        {
            int tested = 0;
            foreach (var wing in Wings.Where(w => w[1].Device == "anten").Take(6))
            {
                var p = NoSafe(Params(wing, 105, 1, false));
                var reps = new List<RoomReport>();
                var R = ToReturn(p, reps, B0, A => { var d = A.Room.DeviceAt.Value; Baskin.CellAt(A, d.X, d.Y).O = null; });
                Assert.AreEqual("anten", reps[1].Device);
                Assert.AreEqual(2, R.Room.Index);
                Assert.AreEqual(0, R.SpawnPerTurn, wing[1].Id);
                Assert.IsTrue(R.Log.Any(l => l.Text == Loc.T("Çağrı anteni kırık: dönüşte kopya dolapları makine uyandırmaz.")));
                Assert.IsTrue(Enumerable.Range(0, 64).Any(k => Baskin.CellAt(R, k % 8, k / 8).O?.K == "spawner"));
                Assert.AreEqual(0, R.Spawns.Count);
                for (int t = 0; t < R.MaxTurns && R.Phase == "player"; t++)
                {
                    Assert.IsTrue(Baskin.EndTurn(R));
                    Assert.AreEqual(0, R.Spawns.Count, wing[1].Id + ": tur " + R.Turn);
                }
                // Denetim: anten sağlamken dönüşte uyanma var
                var reps2 = new List<RoomReport>();
                var R2 = ToReturn(p, reps2, B0);
                Assert.Greater(R2.SpawnPerTurn, 0);
                tested++;
            }
            Assert.Greater(tested, 3);
        }

        [Test]
        public void Prep_Valve_Server_TargetHpLess()
        {
            int tested = 0;
            foreach (var wing in Wings.Where(w => w[1].Device == "vana"))
                foreach (bool core in new[] { false, true })
                {
                    var p = Params(wing, 107, 1, false);
                    p.MissionKind = "server";
                    p.IsCore = core;
                    p.TargetHpBonus = 1;
                    var room = wing[2];
                    var S = Baskin.RoomBoard(p, room, RaidChain.Setup(B0, room, p, new List<RoomReport> { Rep(1), Rep(2) }, false));
                    var setup = RaidChain.Setup(B0, room, p, new List<RoomReport> { Rep(1), Rep(2, device: "vana") }, false);
                    Assert.AreEqual(B0.facilityValveTargetHp, setup.TargetHpLess);
                    Assert.AreEqual(0, setup.StillTurns);
                    var K = Baskin.RoomBoard(p, room, setup);
                    Assert.AreEqual(Baskin.TargetsLeft(S).Count, Baskin.TargetsLeft(K).Count);
                    for (int i = 0; i < Baskin.TargetsLeft(S).Count; i++)
                    {
                        int hs = Baskin.TargetsLeft(S)[i].O.Hp, hk = Baskin.TargetsLeft(K)[i].O.Hp;
                        Assert.AreEqual(Math.Max(1, hs - 2), hk, room.Id);
                        Assert.AreEqual(core ? "core" : "node", Baskin.TargetsLeft(K)[i].O.K);
                        Assert.AreEqual(hk, Baskin.TargetsLeft(K)[i].O.Max);
                    }
                    tested++;
                }
            Assert.Greater(tested, 10);
        }

        [Test]
        public void Prep_Valve_Loot_MachinesStillFirstTurn()
        {
            int tested = 0, moved = 0;
            foreach (var wing in Wings.Where(w => w[1].Device == "vana" && w[0].Mission == "loot"))
            {
                var p = Params(wing, 109, 1, false);
                var room = wing[2];
                var reps = new List<RoomReport> { Rep(1), Rep(2, device: "vana") };
                var setup = RaidChain.Setup(B0, room, p, reps, false);
                Assert.AreEqual(B0.facilityValveStillTurns, setup.StillTurns);
                Assert.AreEqual(0, setup.TargetHpLess);
                string Spots(Baskin.BState B) => string.Join(",", B.Units.Where(u => u.Side == 'e' && !u.Dead).Select(u => $"{u.Type}{u.X}{u.Y}"));
                var start = Baskin.RoomBoard(p, room, setup);
                var H = Open(p, reps, B0);
                Assert.AreEqual(Spots(start), Spots(H), room.Id + ": tur 1 planı yürümez");
                Assert.AreEqual(1, H.StillTurns);
                string before = Spots(H);
                Assert.IsTrue(Baskin.EndTurn(H));
                if (H.Phase == "player" && Spots(H) != before) moved++;
                tested++;
            }
            Assert.Greater(tested, 8);
            Assert.Greater(moved, 0, "tur 2 planında makineler yürür");
        }

        // ---------- Vana ve donanım ----------
        /// <summary>Hedef kareye EMP atışı (uzaklık 2..5, düz hat) için boş kare; yoksa null.</summary>
        static Baskin.Pt? EmpSpot(Baskin.BState B, Baskin.Pt target, Baskin.Unit skip = null)
        {
            foreach (var d in Baskin.Dirs)
                for (int k = 2; k <= 5; k++)
                {
                    int x = target.X + d[0] * k, y = target.Y + d[1] * k;
                    if (In(x, y) && Baskin.CellAt(B, x, y).T != "wall" && Baskin.CellAt(B, x, y).O == null
                        && (Baskin.UnitAt(B, x, y) == null || Baskin.UnitAt(B, x, y) == skip)) return new Baskin.Pt(x, y);
                }
            return null;
        }

        static void Ready(Baskin.Unit u) { u.Moved = false; u.Acted = false; }

        [Test]
        public void Valve_OnlyTechnicianSabotage()
        {
            int tested = 0, pushed = 0;
            string immune = Loc.T("Soğutma vanasını yalnız Teknisyen kapatabilir.");
            string off = Loc.F("{0} devre dışı: etkisi hedef odasında görülecek.", Loc.T(Baskin.ObjNames["vana"]));
            foreach (var wing in Wings.Where(w => w[1].Device == "vana"))
            {
                var (p, reps, A) = Middle(wing, 111);
                Calm(A);
                var v = A.Room.DeviceAt.Value;
                Assert.AreEqual("vana", Baskin.CellAt(A, v.X, v.Y).O.K);
                Assert.AreEqual(1, Baskin.CellAt(A, v.X, v.Y).O.Hp);
                var emp = A.Units.First(u => u.Side == 'p' && u.Type == "emp");
                var spot = EmpSpot(A, v, emp);
                if (spot == null) continue;
                emp.X = spot.Value.X; emp.Y = spot.Value.Y;
                Assert.IsTrue(Baskin.DoAct(A, emp.Id, v.X, v.Y), wing[1].Id);
                Assert.AreEqual(1, Baskin.CellAt(A, v.X, v.Y).O?.Hp, wing[1].Id + ": EMP vanayı kapatmaz");
                Assert.IsTrue(A.FxList.Any(f => f.K == "armor" && f.X == v.X && f.Y == v.Y));
                Assert.IsTrue(A.Log.Any(l => l.Text == immune), wing[1].Id + ": satır 6");
                Assert.IsTrue(Baskin.Undo(A));
                // Çarpma: EMP'nin ittiği birim vanaya çarpar
                emp = A.Units.First(u => u.Side == 'p' && u.Type == "emp");
                var ana = A.Units.First(u => u.Side == 'p' && u.Type == "ana");
                int ox = ana.X, oy = ana.Y;
                foreach (var d in Baskin.Dirs)
                {
                    int bx = v.X - d[0], by = v.Y - d[1], tx = v.X - 2 * d[0], ty = v.Y - 2 * d[1];
                    if (!Free(A, bx, by) && !(ana.X == bx && ana.Y == by) || !In(tx, ty) || Baskin.UnitAt(A, tx, ty) != null && Baskin.UnitAt(A, tx, ty) != ana) continue;
                    ana.X = bx; ana.Y = by; ana.Hp = 3;
                    var es = EmpSpot(A, new Baskin.Pt(tx, ty), emp);
                    if (es == null || es.Value.X == bx && es.Value.Y == by) { ana.X = ox; ana.Y = oy; continue; }
                    emp.X = es.Value.X; emp.Y = es.Value.Y; Ready(emp);
                    Assert.IsTrue(Baskin.DoAct(A, emp.Id, tx, ty));
                    Assert.IsTrue(A.FxList.Any(f => f.K == "push" && f.Unit == ana.Id), wing[1].Id);
                    Assert.AreEqual(1, Baskin.CellAt(A, v.X, v.Y).O?.Hp, wing[1].Id + ": çarpma vanayı kapatmaz");
                    Assert.IsTrue(A.Log.Any(l => l.Text == immune));
                    pushed++;
                    break;
                }
                // Sabotaj: kapatır
                var (p2, reps2, A2) = Middle(wing, 111);
                Calm(A2);
                var tek = A2.Units.First(u => u.Side == 'p' && u.Type == "tek");
                var near = FreeNeighbor(A2, v, gateToo: true);
                Assert.IsNotNull(near, wing[1].Id);
                tek.X = near.Value.X; tek.Y = near.Value.Y;
                Assert.IsTrue(Baskin.DoAct(A2, tek.Id, v.X, v.Y), wing[1].Id);
                Assert.IsNull(Baskin.CellAt(A2, v.X, v.Y).O, wing[1].Id + ": sabotaj kapatır");
                Assert.IsTrue(A2.Log.Any(l => l.Text == off));
                Assert.AreEqual("vana", PassRoom(p2, reps2, A2).Device);
                tested++;
            }
            Assert.Greater(tested, 10);
            Assert.Greater(pushed, 3);
        }

        [Test]
        public void PanelAndAntenna_BreakByAnyDamage()
        {
            int tested = 0;
            foreach (var wing in Wings.Take(12))
            {
                var (p, reps, G) = Entry(wing);
                Calm(G);
                var d = G.Room.DeviceAt.Value;
                string k = G.Room.Device;
                Assert.AreEqual(2, Baskin.CellAt(G, d.X, d.Y).O.Hp, wing[0].Id);
                var emp = G.Units.First(u => u.Side == 'p' && u.Type == "emp");
                var spot = EmpSpot(G, d, emp);
                if (spot == null) continue;
                emp.X = spot.Value.X; emp.Y = spot.Value.Y;
                Assert.IsTrue(Baskin.DoAct(G, emp.Id, d.X, d.Y));
                Assert.AreEqual(1, Baskin.CellAt(G, d.X, d.Y).O?.Hp, wing[0].Id + ": bir EMP");
                Ready(emp);
                Assert.IsTrue(Baskin.DoAct(G, emp.Id, d.X, d.Y));
                Assert.IsNull(Baskin.CellAt(G, d.X, d.Y).O, wing[0].Id + ": iki EMP kırar");
                Assert.IsTrue(G.Log.Any(l => l.Text == Loc.F("{0} devre dışı: etkisi hedef odasında görülecek.", Loc.T(Baskin.ObjNames[k]))));
                Assert.IsTrue(Baskin.Undo(G));
                Assert.IsTrue(Baskin.Undo(G));
                Assert.AreEqual(2, Baskin.CellAt(G, d.X, d.Y).O.Hp);
                var tek = G.Units.First(u => u.Side == 'p' && u.Type == "tek");
                var near = FreeNeighbor(G, d, gateToo: true);
                Assert.IsNotNull(near);
                tek.X = near.Value.X; tek.Y = near.Value.Y;
                Assert.IsTrue(Baskin.DoAct(G, tek.Id, d.X, d.Y));
                Assert.IsNull(Baskin.CellAt(G, d.X, d.Y).O, wing[0].Id + ": tek sabotaj kırar");
                Assert.AreEqual(k, PassRoom(p, reps, G).Device);
                tested++;
            }
            Assert.Greater(tested, 6);
        }

        [Test]
        public void Machines_DoNotAimAtDevices()
        {
            int tested = 0;
            foreach (var wing in Wings)
            {
                var (_, _, G) = Entry(wing);
                Calm(G, keep: 1);
                var dron = Foes(G).Single();
                Assert.AreEqual("dron", dron.Type);
                var dev = G.Room.DeviceAt.Value;
                G.Smart = false;
                G.StillTurns = 99;
                // Dronun dört atışından yalnız biri bir şeye (donanıma) değer; öbürleri duvar ya da boş
                (int X, int Y, int Dir)? found = null;
                for (int y = 0; y < Baskin.N && found == null; y++)
                    for (int x = 0; x < Baskin.N && found == null; x++)
                    {
                        if (!Free(G, x, y) && !(dron.X == x && dron.Y == y)) continue;
                        int devDir = -1; bool ok = true;
                        for (int dir = 0; dir < 4 && ok; dir++)
                        {
                            int cx = x, cy = y;
                            while (true)
                            {
                                cx += Baskin.Dirs[dir][0]; cy += Baskin.Dirs[dir][1];
                                if (!In(cx, cy) || Baskin.CellAt(G, cx, cy).T == "wall") break;
                                var u = Baskin.UnitAt(G, cx, cy);
                                if (u != null && u != dron) { ok = false; break; }
                                var o = Baskin.CellAt(G, cx, cy).O;
                                if (o == null) continue;
                                if (cx == dev.X && cy == dev.Y && devDir < 0) devDir = dir; else ok = false;
                                break;
                            }
                        }
                        if (ok && devDir >= 0) found = (x, y, devDir);
                    }
                if (found == null) continue;
                dron.X = found.Value.X; dron.Y = found.Value.Y; dron.Intent = null;
                Assert.IsTrue(Baskin.EndTurn(G), wing[0].Id);
                Assert.AreEqual(found.Value.X, dron.X);
                Assert.IsNotNull(dron.Intent);
                Assert.AreNotEqual(found.Value.Dir, dron.Intent.Dir, wing[0].Id + ": dron donanıma nişan almaz");
                tested++;
            }
            Assert.Greater(tested, 5);
        }

        [Test]
        public void AtGate_UnaffectedByEvents([Range(1, 12)] int seed)
        {
            foreach (var wing in Wings)
            {
                var (p, reps, A) = Middle(wing, seed * 131, events: true);
                Assert.IsNotNull(A.Ev, wing[1].Id);
                Calm(A);
                var u = BySlot(A, 0);
                ToGate(A, u);
                string before = $"{u.X},{u.Y},{u.Hp},{u.Armor},{u.Frozen},{u.Ice},{u.Cab},{u.Dead}";
                for (int t = 0; t < A.MaxTurns - 1 && A.Phase == "player"; t++) Assert.IsTrue(Baskin.EndTurn(A));
                Assert.IsTrue(u.AtGate);
                Assert.AreEqual(before, $"{u.X},{u.Y},{u.Hp},{u.Armor},{u.Frozen},{u.Ice},{u.Cab},{u.Dead}", wing[1].Id + ": kapıdaki uzman olaylardan etkilenmez");
            }
        }

        // ---------- Sonuç ----------
        [Test]
        public void Result_SumsRooms()
        {
            var p = Params(Wings[0], 1, 1, false);
            var g = Rep(1); g.Turns = 3; g.Collateral = 1; g.Civilians = 2;
            var a = Rep(2); a.Turns = 4; a.Civilians = 1; a.Machines.Add(new RoomMachine { Type = "dron", X = 5, Y = 5, Hp = 2 });
            var h = Rep(3); h.Turns = 5; h.Collateral = 2; h.Civilians = 3;
            var r = Rep(2, ret: true); r.Turns = 2; r.Collateral = 1; r.Civilians = 0; r.Last = true; r.Success = true;
            r.Team[0] = new RoomMember { State = "a", Hp = 2, Armor = 1 };
            r.Team[1] = new RoomMember { State = "d", Hp = 0, Armor = 0, Fell = 1 };
            r.Team[2] = new RoomMember { State = "l", Hp = 0, Armor = 1, Fell = 2 };
            var reps = new List<RoomReport> { g, a, h, r };
            Assert.IsTrue(RaidChain.EndsHere(p, reps.Take(3).ToList(), r), "G temiz: A dönüşünden yürüyerek çıkış");
            var res = RaidChain.Result(p, reps);
            Assert.AreEqual(14, res.Turns);
            Assert.AreEqual(4, res.Collateral);
            Assert.AreEqual(2 + 0 + 3, res.CiviliansLeft, "oda başına son ziyaret");
            CollectionAssert.AreEqual(new[] { 2, 0, 0 }, res.Hp);
            CollectionAssert.AreEqual(new[] { 1, 0, 1 }, res.Armor);
            CollectionAssert.AreEqual(new[] { 2 }, res.Left);
            Assert.AreEqual(2, res.Down);
            Assert.IsTrue(res.Success);
            Assert.IsFalse(res.Retreat);
            Assert.AreEqual("", res.Reason);
            r.Last = false;
            Assert.Throws<InvalidOperationException>(() => RaidChain.Result(p, reps));
        }

        // ---------- Simülasyon ----------
        /// <summary>Baskın emri kanadın üç odasını yazar; zincir giriş odasından, raporsuz başlar.</summary>
        [Test]
        public void StartRaid_SetsRooms()
        {
            var sim = Sim();
            var raid = sim.S.Raid;
            string sector = raid.Params.SectorId;
            CollectionAssert.AreEqual(new[] { FacilityMaps.RoomId(sector, 1), FacilityMaps.RoomId(sector, 2), FacilityMaps.RoomId(sector, 3) }, raid.Params.Rooms);
            Assert.AreEqual(0, raid.Reports.Count);
            Assert.AreEqual(new RoomStep(1, false), sim.RaidRoom.Value);
            Assert.IsFalse(sim.RaidRoomsClosed);
        }

        /// <summary>
        /// Tek kip (kayıt 21): ResolveRaid elle verilen sonucu yok sayar. Son oda raporlanmadan reddedilir (kayda girmez); kapanınca
        /// sonuç oda raporlarından türetilir ve kayda o yazılır: verilenin başarısı, Hp ve Down alanları girmez.
        /// </summary>
        [Test]
        public void ResolveRaid_IgnoresGivenResult_UntilLastRoom()
        {
            var sim = Sim();
            int n = sim.Record.commands.Count;
            var given = new RaidResult { Success = true, Down = 2, Hp = { 0, 0, 9 }, Armor = { 0, 0, 0 }, Reason = "elle" };
            var early = sim.ResolveRaid(given);
            Assert.IsFalse(early.Ok);
            Assert.AreEqual(Loc.T("Baskının son odası raporlanmadı."), early.Msg);
            Assert.AreEqual(n, sim.Record.commands.Count, "reddedilen sonuç kayda girmez");
            Assert.IsNotNull(sim.S.Raid);
            var G = sim.OpenRaidRoom();
            Calm(G);
            Assert.IsTrue(Baskin.Retreat(G));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(G)).Ok);
            Assert.IsTrue(sim.RaidRoomsClosed);
            string derived = sim.FacilityRaidResult().Encode();
            var res = sim.ResolveRaid(given);
            Assert.IsTrue(res.Ok, res.Msg);
            var log = sim.S.RaidLog.Last();
            Assert.IsFalse(log.Success, "türetilmiş sonuç: giriş odasında geri çekilme");
            Assert.AreEqual(0, log.Down);
            Assert.IsTrue(log.LiberationReward.Squad.All(o => !o.Dead && !o.Down && o.HpAfter == o.HpBefore), "verilen canlar yok sayılır");
            var cmd = sim.Record.commands.Last();
            Assert.AreEqual("raidres", cmd.kind);
            Assert.AreEqual(derived, cmd.a, "kayda türetilmiş sonuç girer");
            Assert.AreNotEqual(given.Encode(), cmd.a);
        }

        /// <summary>
        /// Test yardımcısının kendi testi (<see cref="FacilityKit.ResolveRaid(Simulation, bool, int[])"/>; olay ve son testleri ona
        /// dayanır): her kader bileşimi baskın günlüğünde ve kadronun dönüşünde beklenen dala gider. 0 döner, 1 düşer ve ölür, 2 geride
        /// kalır ve ölür, 3 düşer ve çıkarılır (yatar); başarısızlık giriş odasında geri çekilmedir.
        /// </summary>
        [TestCase(true, new[] { 0, 0, 0 })]
        [TestCase(true, new[] { 1, 0, 0 })]
        [TestCase(true, new[] { 0, 2, 0 })]
        [TestCase(true, new[] { 3, 0, 0 })]
        [TestCase(true, new[] { 3, 1, 0 })]
        [TestCase(true, new[] { 2, 3, 0 })]
        [TestCase(false, new[] { 0, 0, 0 })]
        [TestCase(false, new[] { 1, 0, 0 })]
        [TestCase(false, new[] { 1, 1, 1 })]
        public void Kit_ResolveRaid_FollowsTheFates(bool success, int[] fates)
        {
            var sim = Sim();
            var squad = sim.S.Raid.Params.Squad;
            ShelterEventKit.ResolveRaid(sim, success, fates);
            Assert.IsNull(sim.S.Raid);
            var log = sim.S.RaidLog.Last();
            Assert.AreEqual(success, log.Success);
            Assert.AreEqual(fates.Count(f => f != 0), log.Down, "düşen sayısı");
            Assert.AreEqual(success, sim.LiberationSector(log.SectorId).Captured);
            var res = RaidResult.Decode(sim.Record.commands.Last().a);
            Assert.AreEqual(!success, res.Retreat, "başarısızlık geri çekilmedir");
            var outcome = log.LiberationReward.Squad;
            Assert.AreEqual(squad.Count, outcome.Count);
            for (int i = 0; i < squad.Count; i++)
            {
                var o = outcome[i];
                var person = sim.Board.Person(squad[i].PersonId);
                string at = "kadro " + i + " kader " + fates[i];
                Assert.AreEqual(fates[i] == 1 || fates[i] == 2, o.Dead, at + ": ölü");
                Assert.AreEqual(fates[i] == 1 || fates[i] == 2, !person.Alive, at + ": kişi");
                Assert.AreEqual(fates[i] == 2, o.Left, at + ": geride kaldı");
                Assert.AreEqual(fates[i] != 0, o.Down, at + ": düştü");
                Assert.AreEqual(fates[i] == 3, res.Extracted.Contains(i), at + ": çıkarıldı");
                if (fates[i] == 3) Assert.GreaterOrEqual(person.Down, 1, at + ": yatar");
                if (fates[i] == 0) Assert.AreEqual(squad[i].Hp, o.HpAfter, at + ": can");
            }
        }

        [Test]
        public void Chain_DrawsNoMainRng()
        {
            foreach (uint seed in new uint[] { 79, 3, 11 })
            {
                var sim = Sim(seed);
                uint rng = sim.S.Rng.State;
                PlayRaid(sim);
                Assert.AreEqual(rng, sim.S.Rng.State, seed.ToString());
                Assert.IsNull(sim.S.Raid);
            }
        }

        /// <summary>Aşama 6 ölüm kuralı (aşama 2'nin ara kuralı "biri ayakta döndüyse düşen kurtarılır" kalktı): taşınmayan düşen ölür,
        /// ayakta dönen yaşar; bütün ekip düşerse hepsi ölür.</summary>
        [Test]
        public void FellOff_NotCarried_Dies()
        {
            foreach (bool survivor in new[] { true, false })
            {
                var sim = Sim();
                var squad = sim.S.Raid.Params.Squad;
                var G = sim.OpenRaidRoom();
                Calm(G);
                var fallen = BySlot(G, 1);
                fallen.Dead = true; fallen.Hp = 0;
                ToGate(G);
                Assert.IsTrue(Baskin.Pass(G));
                Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(G)).Ok);
                var A = sim.OpenRaidRoom();
                Calm(A);
                if (survivor) Assert.IsTrue(Baskin.Retreat(A));
                else
                {
                    foreach (var u in Standing(A)) { u.Dead = true; u.Hp = 0; }
                    Assert.IsTrue(Baskin.EndTurn(A));
                    Assert.AreEqual("lost", A.Phase);
                    Assert.AreEqual(Loc.T("Ekibin tamamı yere düştü."), A.OutcomeReason);
                }
                var r = sim.RaidRoomReport(A);
                Assert.IsTrue(r.Last);
                Assert.IsTrue(sim.AdvanceRaidRoom(r).Ok);
                Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
                var person = sim.Board.Person(squad[1].PersonId);
                Assert.IsFalse(person.Alive, "taşınmayan düşen ölür");
                if (survivor)
                {
                    Assert.IsTrue(sim.Board.Person(squad[0].PersonId).Alive, "ayakta dönen yaşar");
                    Assert.AreEqual(0, sim.Board.Person(squad[0].PersonId).Down);
                }
                else
                {
                    Assert.IsTrue(squad.All(s => !sim.Board.Person(s.PersonId).Alive), "kimse ayakta dönmedi: düşenler ölür");
                }
            }
        }
    }
}
