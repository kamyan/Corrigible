using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 11, kalıcı tahta (Docs/TESIS-BASKINLARI-TASARIMI.md §9.1; uygulama tasarımı §2.1–2.2, §5.1). Parça P1: izlerin
    /// uygulanması (<see cref="FacilityMarks.Apply"/>: her tür, uygulanamayan izler, 108 odalık bağ testi), türetilmesi
    /// (<see cref="FacilityMarks.Derive"/>) ve birleştirilmesi (<see cref="FacilityMarks.Merge"/>), oda zincirinin emirdeki izleri ilk
    /// ziyaret ve savunma tahtasına indirmesi, kırık donanım izinin hedef odası hazırlığı ve izsiz emrin tahtası. Parça P2 (simülasyon,
    /// uygulama tasarımı §3.6–3.7): <c>raidres</c>'te iz toplama (başarısız baskının tekrarında ilk ziyaret tahtası ve bina planı izleri
    /// taşır), YZ onarımı (işgaldeki kanatta vardiya başına, en eski önce; alınmış kanatta yok; onarılan donanım geri gelir), savunmada
    /// terk edilen odanın izlerinin silinmesi ve alt bayrak kapalıyken (<see cref="Balance.facilityMarks"/>) aşama 10 tahtaları.
    /// </summary>
    public class FacilityMarksTests
    {
        static Balance B0 => Bal(events: false);
        static IReadOnlyList<FacilityRoom> Rooms => Plan79.Rooms;

        static FacilityMark M(FacilityRoom r, string kind, Baskin.Pt at, int hp = 0, int max = 0, int day = 1) =>
            new FacilityMark { Room = r.Id, Kind = kind, X = at.X, Y = at.Y, Hp = hp, Max = max, Day = day };

        static IEnumerable<Baskin.Pt> Tiles() { for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) yield return new Baskin.Pt(x, y); }

        static bool Floor(Baskin.Cell[,] c, Baskin.Pt t) => c[t.Y, t.X].T == "floor" && c[t.Y, t.X].O == null && c[t.Y, t.X].Door == 0 && c[t.Y, t.X].Vent == 0;

        /// <summary>Konunca odanın bağlarını (<see cref="FacilityMarks.Links"/>) bozup bozmadığı (hücreler geri konur).</summary>
        static bool Cuts(Baskin.Cell[,] cells, FacilityRoom room, Baskin.Pt t)
        {
            var before = FacilityMarks.Links(cells, room);
            cells[t.Y, t.X].O = new Baskin.Obj { K = "tel", Hp = 1, Max = 1 };
            var after = FacilityMarks.Links(cells, room);
            cells[t.Y, t.X].O = null;
            return before.Where((b, i) => b && !after[i]).Any();
        }

        /// <summary>Engelin bağı bozmadan konabileceği boş zemin kareleri (y, x).</summary>
        static List<Baskin.Pt> SafeFloors(Baskin.Cell[,] cells, FacilityRoom room) => Tiles().Where(t => Floor(cells, t) && !Cuts(cells, room, t)).ToList();

        static Baskin.Pt? Find(Baskin.Cell[,] c, Func<Baskin.Cell, bool> ok) => Tiles().Where(t => ok(c[t.Y, t.X])).Select(t => (Baskin.Pt?)t).FirstOrDefault();

        static bool Same(Baskin.Pt a, Baskin.Pt b) => a.X == b.X && a.Y == b.Y;

        static string Key(IEnumerable<FacilityMark> marks) => string.Join(" ", marks.Select(m => $"{m.Kind}:{m.X}{m.Y}:{m.Hp}/{m.Max}"));

        // ---------- Uygulama ----------
        [Test]
        public void Apply_EachKind()
        {
            var rA = Rooms.First(r => r.Type == RoomType.Middle && r.Shutter != null && r.DeviceAt != null);
            var cells = rA.PlanCells(1, false);
            var wall = Find(cells, c => c.T == "wall" && !c.Shutter && !c.Crate).Value;
            cells[wall.Y, wall.X].Double = true;
            var sh = rA.Shutter.Value;
            var dev = rA.DeviceAt.Value;
            var safe = SafeFloors(cells, rA);
            Assert.GreaterOrEqual(safe.Count, 3);
            var marks = new List<FacilityMark>
            {
                M(rA, FacilityMarks.Wire, safe[0], 2, 3), M(rA, FacilityMarks.Wall, wall), M(rA, FacilityMarks.Shutter, sh),
                M(rA, FacilityMarks.Device, dev),
            };
            var rubble = new List<Baskin.Pt>();
            var skipped = FacilityMarks.Apply(cells, rA, marks, rubble, false);
            Assert.IsEmpty(skipped);
            Assert.AreEqual("floor", cells[wall.Y, wall.X].T);
            Assert.IsFalse(cells[wall.Y, wall.X].Double, "çift cidar yıkıldı");
            Assert.IsTrue(rubble.Count == 1 && Same(rubble[0], wall));
            Assert.AreEqual("floor", cells[sh.Y, sh.X].T);
            Assert.IsTrue(cells[sh.Y, sh.X].Shutter, "kepenk bayrağı kalır");
            Assert.IsNull(cells[dev.Y, dev.X].O, "kırık donanım");
            var o = cells[safe[0].Y, safe[0].X].O;
            Assert.AreEqual(("tel", 2, 3), (o.K, o.Hp, o.Max));
            // Kalas ve hurda kalan canıyla (ayrı kareler, bağı bozmadan)
            var safe2 = SafeFloors(cells, rA);
            Assert.IsEmpty(FacilityMarks.Apply(cells, rA, new[] { M(rA, FacilityMarks.Plank, safe2[0], 1, 3), M(rA, FacilityMarks.Scrap, safe2[safe2.Count - 1], 1, 1) }, null, false));
            Assert.AreEqual("kalas", cells[safe2[0].Y, safe2[0].X].O.K);
            Assert.AreEqual(1, cells[safe2[0].Y, safe2[0].X].O.Hp);
            Assert.AreEqual("hurda", cells[safe2[safe2.Count - 1].Y, safe2[safe2.Count - 1].X].O.K);
            // Soğutma ünitesi ve kopya dolabı
            foreach (var kind in new[] { FacilityMarks.Cool, FacilityMarks.Spawner })
            {
                var r = Rooms.First(x => Tiles().Any(t => x.PlanCells(1, false)[t.Y, t.X].O?.K == kind));
                var c = r.PlanCells(1, false);
                var at = Find(c, x => x.O?.K == kind).Value;
                Assert.IsEmpty(FacilityMarks.Apply(c, r, new[] { M(r, kind, at) }, null, false));
                Assert.IsNull(c[at.Y, at.X].O, kind);
            }
            // Başka odanın izi bu odaya uygulanmaz
            var other = Rooms.First(r => r.Id != rA.Id);
            var c2 = rA.PlanCells(1, false);
            var w2 = Find(c2, c => c.T == "wall" && !c.Shutter && !c.Crate).Value;
            Assert.IsEmpty(FacilityMarks.Apply(c2, rA, new[] { M(other, FacilityMarks.Wall, w2) }, null, false));
            Assert.AreEqual("wall", c2[w2.Y, w2.X].T);
        }

        [Test]
        public void Apply_SkipsWhatNoLongerFits_InListOrder()
        {
            var rA = Rooms.First(r => r.Type == RoomType.Middle && r.Shutter != null && r.DeviceAt != null && r.Vents.Count > 0);
            var cells = rA.PlanCells(1, false);
            var floor = Find(cells, c => c.T == "floor" && c.O == null && c.Door == 0 && c.Vent == 0).Value;
            var wallNotShutter = Find(cells, c => c.T == "wall" && !c.Shutter).Value;
            var wall = Find(cells, c => c.T == "wall").Value;
            var marks = new List<FacilityMark>
            {
                M(rA, FacilityMarks.Wall, floor),                 // artık duvar değil
                M(rA, FacilityMarks.Shutter, wallNotShutter),     // kepenk değil
                M(rA, FacilityMarks.Device, floor),               // donanım yok
                M(rA, FacilityMarks.Cool, floor),                 // soğutma ünitesi yok
                M(rA, FacilityMarks.Wire, rA.West[0], 3, 3),      // batı girişi
                M(rA, FacilityMarks.Wire, rA.Forward[0], 3, 3),   // ileri geçit
                M(rA, FacilityMarks.Plank, rA.Vents[0], 3, 3),    // menfez
                M(rA, FacilityMarks.Scrap, wall, 1, 1),           // zemin değil
                M(rA, FacilityMarks.Scrap, rA.DeviceAt.Value, 1, 1), // dolu (donanım)
            };
            string before = string.Join("", Tiles().Select(t => cells[t.Y, t.X].T + cells[t.Y, t.X].O?.K));
            var skipped = FacilityMarks.Apply(cells, rA, marks, new List<Baskin.Pt>(), false);
            CollectionAssert.AreEqual(marks, skipped, "hepsi atlanır, liste sırasıyla");
            Assert.AreEqual(before, string.Join("", Tiles().Select(t => cells[t.Y, t.X].T + cells[t.Y, t.X].O?.K)), "hücreler değişmez");
            // Koku yayıcı (önlem kaplaması) ve sivil sunucu karesi doludur
            var rc = Rooms.First(r => Tiles().Any(t => r.PlanCells(1, false)[t.Y, t.X].O?.K == "civ"));
            var cc = rc.PlanCells(1, false);
            Baskin.ApplyCounterCells(cc, rc, false, new List<int> { 0, 0, 0, 0, 2, 0 }, new Balance());
            var em = Find(cc, c => c.O?.K == "yayici");
            Assert.IsNotNull(em, "yayıcı kondu");
            var civ = Find(cc, c => c.O?.K == "civ").Value;
            var sk = FacilityMarks.Apply(cc, rc, new[] { M(rc, FacilityMarks.Wire, em.Value, 3, 3), M(rc, FacilityMarks.Wire, civ, 3, 3) }, null, false);
            Assert.AreEqual(2, sk.Count);
            Assert.AreEqual("yayici", cc[em.Value.Y, em.Value.X].O.K);
            // Bağı kesen engel atlanır (bir odada bulunur)
            bool found = false;
            foreach (var r in Rooms)
            {
                var c = r.PlanCells(1, false);
                var cut = Tiles().Where(t => Floor(c, t) && Cuts(c, r, t)).Select(t => (Baskin.Pt?)t).FirstOrDefault();
                if (cut == null) continue;
                var skip = FacilityMarks.Apply(c, r, new[] { M(r, FacilityMarks.Wire, cut.Value, 3, 3) }, null, false);
                Assert.AreEqual(1, skip.Count, r.Id);
                Assert.IsNull(c[cut.Value.Y, cut.Value.X].O);
                found = true;
                break;
            }
            Assert.IsTrue(found, "bağı kesen kare bulunamadı");
        }

        [Test]
        public void Apply_EventPartTiles_SkippedAtTier3()
        {
            int n = 0;
            foreach (var r in Rooms.Where(x => x.Event != null))
            {
                var c = r.PlanCells(3, true);
                var part = r.PartTiles(3).Select(k => new Baskin.Pt(k % Baskin.N, k / Baskin.N)).Where(t => Floor(c, t)).Select(t => (Baskin.Pt?)t).FirstOrDefault();
                if (part == null) continue;
                var sk = FacilityMarks.Apply(c, r, new[] { M(r, FacilityMarks.Wire, part.Value, 3, 3) }, null, true);
                Assert.AreEqual(1, sk.Count, r.Id + " şart parçası");
                Assert.IsNull(c[part.Value.Y, part.Value.X].O);
                n++;
            }
            Assert.Greater(n, 0);
        }

        [Test]
        public void Links_Kept_108Rooms_RandomMarks()
        {
            int idx = 0, applied = 0, skipped = 0;
            foreach (var r in Rooms)
            {
                idx++;
                foreach (bool ev in new[] { false, true })
                {
                    if (ev && r.Event == null) continue;
                    var cells = r.PlanCells(ev ? 3 : 1, ev);
                    var rng = new Rng(Rng.Mix(79u, (uint)(idx * 2 + (ev ? 1 : 0))));
                    var marks = new List<FacilityMark>();
                    for (int i = 0; i < 24; i++)
                    {
                        string kind = FacilityMarks.Kinds[(int)Math.Floor(rng.Next() * FacilityMarks.Kinds.Length)];
                        var at = new Baskin.Pt((int)Math.Floor(rng.Next() * Baskin.N), (int)Math.Floor(rng.Next() * Baskin.N));
                        int hp = 1 + (int)Math.Floor(rng.Next() * 3);
                        marks.Add(M(r, kind, at, hp, 3));
                    }
                    var before = FacilityMarks.Links(cells, r);
                    var sk = FacilityMarks.Apply(cells, r, marks, new List<Baskin.Pt>(), ev);
                    var after = FacilityMarks.Links(cells, r);
                    for (int i = 0; i < before.Count; i++) Assert.IsTrue(!before[i] || after[i], $"{r.Id} ev={ev} bağ {i} koptu");
                    Assert.IsTrue(before.All(b => b), $"{r.Id}: planda bağlar tutar");
                    foreach (var t in Tiles())
                    {
                        var c = cells[t.Y, t.X];
                        if (c.O != null && Baskin.IsBarrier(c.O.K)) Assert.IsTrue(c.Door == 0 && c.Vent == 0 && c.T == "floor", $"{r.Id} {t.X}{t.Y}");
                    }
                    applied += marks.Count - sk.Count;
                    skipped += sk.Count;
                }
            }
            Assert.AreEqual(108, idx);
            Assert.Greater(applied, 0);
            Assert.Greater(skipped, 0);
        }

        // ---------- Türetme ve birleştirme ----------
        [Test]
        public void Derive_FromVisits_PermanentUnion_ObjectsFromLastVisit()
        {
            var rA = Rooms.First(r => r.Type == RoomType.Middle && r.Shutter != null && r.DeviceAt != null
                && Tiles().Any(t => r.PlanCells(1, false)[t.Y, t.X].O?.K == "cool" || r.PlanCells(1, false)[t.Y, t.X].O?.K == "spawner"));
            var plan = rA.PlanCells(1, false);
            var walls = Tiles().Where(t => plan[t.Y, t.X].T == "wall" && !plan[t.Y, t.X].Shutter).ToList();
            var floor = Tiles().First(t => Floor(plan, t));
            var safe = SafeFloors(plan, rA);
            int K(Baskin.Pt t) => t.Y * Baskin.N + t.X;
            RoomObject O(string k, Baskin.Pt t, int hp, int max) => new RoomObject { K = k, X = t.X, Y = t.Y, Hp = hp, Max = max };
            var planObjs = Tiles().Where(t => plan[t.Y, t.X].O != null).Select(t => O(plan[t.Y, t.X].O.K, t, plan[t.Y, t.X].O.Hp, plan[t.Y, t.X].O.Max)).ToList();
            var broken = planObjs.First(o => o.K == "cool" || o.K == "spawner");
            var v1 = new RoomReport { Room = 2, End = "pass", Device = rA.Device };
            v1.Rubble.AddRange(new[] { K(walls[0]), K(floor) });
            v1.Opened.Add(K(rA.Shutter.Value));
            v1.Objects.AddRange(planObjs);
            v1.Objects.Add(O("tel", safe[0], 1, 3));
            var v2 = new RoomReport { Room = 2, Return = true, End = "pass", Device = "" };
            v2.Rubble.AddRange(new[] { K(walls[0]), K(walls[1]) });
            v2.Objects.AddRange(planObjs.Where(o => o != broken && !Baskin.IsDevice(o.K)));
            v2.Objects.Add(O("kalas", safe[1], 2, 3));
            v2.Objects.Add(O("hurda", safe[2], 1, 1));
            v2.Objects.Add(O("yayici", safe[3], 1, 1));
            v2.Objects.Add(O("node", safe[4], 4, 4));
            var d = FacilityMarks.Derive(rA, new[] { v1, v2 }, 7, plan);
            Assert.IsTrue(d.All(m => m.Room == rA.Id && m.Day == 7));
            var expect = new List<FacilityMark>
            {
                M(rA, "duvar", walls[0]), M(rA, "duvar", walls[1]), M(rA, "kepenk", rA.Shutter.Value), M(rA, "donanim", rA.DeviceAt.Value),
                M(rA, broken.K, new Baskin.Pt(broken.X, broken.Y)),
            };
            expect.AddRange(new[] { M(rA, "kalas", safe[1], 2, 3), M(rA, "hurda", safe[2], 1, 1) }.OrderBy(m => m.Y).ThenBy(m => m.X));
            Assert.AreEqual(Key(expect), Key(d), "dönüş ziyaretinde çift sayım yok; tel son ziyarette yok; yayıcı, hedef ve sivil iz değil");
            // Ziyaret yoksa boş
            Assert.IsEmpty(FacilityMarks.Derive(rA, new List<RoomReport>(), 7));
        }

        [Test]
        public void Merge_KeepsPermanent_UpdatesAndDropsBarriers()
        {
            var r = Rooms[4];
            var other = Rooms[0];
            var p = new Baskin.Pt(1, 1);
            var marks = new List<FacilityMark>
            {
                M(other, "tel", p, 3, 3, 1), M(r, "duvar", new Baskin.Pt(2, 2), 0, 0, 1), M(r, "tel", new Baskin.Pt(3, 3), 3, 3, 1),
                M(r, "cool", new Baskin.Pt(4, 4), 0, 0, 1),
            };
            var derived = new[]
            {
                M(r, "duvar", new Baskin.Pt(2, 2), 0, 0, 5), M(r, "tel", new Baskin.Pt(3, 3), 1, 3, 5), M(r, "kalas", new Baskin.Pt(5, 5), 3, 3, 5),
                M(r, "duvar", new Baskin.Pt(6, 6), 0, 0, 5),
            };
            var (added, gone) = FacilityMarks.Merge(marks, r.Id, derived);
            Assert.AreEqual((2, 0), (added, gone));
            Assert.AreEqual("tel:11:3/3:1 duvar:22:0/0:1 tel:33:1/3:1 cool:44:0/0:1 kalas:55:3/3:5 duvar:66:0/0:5",
                string.Join(" ", marks.Select(m => $"{m.Kind}:{m.X}{m.Y}:{m.Hp}/{m.Max}:{m.Day}")), "kalıcı izin günü ve sırası korunur");
            (added, gone) = FacilityMarks.Merge(marks, r.Id, new[] { M(r, "duvar", new Baskin.Pt(2, 2)) });
            Assert.AreEqual((0, 2), (added, gone), "kırılan tel ve kalas düşer");
            Assert.AreEqual("tel:11 duvar:22 cool:44 duvar:66", string.Join(" ", marks.Select(m => $"{m.Kind}:{m.X}{m.Y}")));
            Assert.AreEqual(3, FacilityMarks.Wipe(marks, r.Id));
            Assert.AreEqual(1, marks.Count);
            Assert.IsTrue(FacilityMarks.InWing(r.Id, r.SectorId));
            Assert.IsFalse(FacilityMarks.InWing(r.Id, r.SectorId + "x"));
            Assert.IsFalse(FacilityMarks.InWing("bozuk", r.SectorId));
        }

        // ---------- Oda zinciri ----------
        [Test]
        public void Board_AppliesOrderMarks_ReportDerivesThemBack()
        {
            var wing = Wings[2];
            var g = wing[0];
            var a = wing[1];
            var plan = g.PlanCells(1, false);
            var wall = Tiles().First(t => plan[t.Y, t.X].T == "wall" && !plan[t.Y, t.X].Shutter && t.X > 0 && t.Y > 0 && t.X < 7 && t.Y < 7
                && Baskin.Dirs.Any(d => plan[t.Y + d[1], t.X + d[0]].T == "floor"));
            var tel = SafeFloors(plan, g).Last();
            var aPlan = a.PlanCells(1, false);
            var aWall = Tiles().First(t => aPlan[t.Y, t.X].T == "wall" && !aPlan[t.Y, t.X].Shutter && !aPlan[t.Y, t.X].Crate);
            var p = Params(wing, 41, 1, false);
            p.Marks = new List<FacilityMark> { M(g, "duvar", wall), M(g, "donanim", g.DeviceAt.Value), M(g, "tel", tel, 2, 3), M(a, "duvar", aWall) };
            var reps = new List<RoomReport>();
            var B = Open(p, reps);
            Assert.AreEqual("floor", Baskin.CellAt(B, wall.X, wall.Y).T);
            Assert.IsTrue(B.Rubble.Any(t => Same(t, wall)));
            Assert.IsNull(Baskin.CellAt(B, g.DeviceAt.Value.X, g.DeviceAt.Value.Y).O);
            Assert.AreEqual("tel", Baskin.CellAt(B, tel.X, tel.Y).O?.K);
            Assert.AreEqual(2, Baskin.CellAt(B, tel.X, tel.Y).O.Hp);
            StringAssert.Contains("|r", Baskin.Signature(B));
            Calm(B);
            var r = PassRoom(p, reps, B);
            Assert.AreEqual(g.Device, r.Device, "kırık donanım raporda kırık");
            var d = FacilityMarks.Derive(g, new[] { r }, 2);
            Assert.AreEqual(Key(new[] { M(g, "duvar", wall), M(g, "donanim", g.DeviceAt.Value), M(g, "tel", tel, 2, 3) }), Key(d), "tahtadan geri türetilir");
            var A = Open(p, reps);
            Assert.AreEqual("floor", Baskin.CellAt(A, aWall.X, aWall.Y).T, "A'nın izi A'da");
            // Savunma tahtası da uygular
            var dp = Params(wing, 41, 1, false);
            dp.Mode = "defense";
            dp.Marks = p.Marks;
            var D = Open(dp, new List<RoomReport>());
            Assert.AreEqual("tel", Baskin.CellAt(D, tel.X, tel.Y).O?.K);
            Assert.AreEqual("floor", Baskin.CellAt(D, wall.X, wall.Y).T);
        }

        [Test]
        public void MarkedDevice_TargetPreparationPersists()
        {
            var b = B0;
            int checkedKinds = 0;
            foreach (var kind in new[] { "pano", "anten", "vana" })
            {
                var wing = Wings.FirstOrDefault(w => w[0].Device == kind || w[1].Device == kind);
                if (wing == null) continue;
                var room = wing[0].Device == kind ? wing[0] : wing[1];
                var p = Params(wing, 41, 1, false);
                p.Marks = new List<FacilityMark> { M(room, "donanim", room.DeviceAt.Value) };
                var s0 = RaidChain.Setup(b, wing[2], p, new List<RoomReport>(), false);
                var s = RaidChain.Setup(b, wing[2], p, new List<RoomReport>(), false, Plan79);
                CollectionAssert.AreEqual(new[] { kind }, RaidChain.MarkedDevices(p, Plan79));
                Assert.AreEqual((0, 0, 0, 0), (s0.QuietTurns, s0.MachinesLess, s0.StillTurns, s0.TargetHpLess), "plansız: iz hazırlığa girmez");
                if (kind == "pano") Assert.AreEqual(b.facilityPanelQuietTurns, s.QuietTurns);
                if (kind == "anten") Assert.AreEqual(b.facilityAntennaMachines, s.MachinesLess);
                if (kind == "vana") Assert.AreEqual(p.MissionKind == "loot" ? (b.facilityValveStillTurns, 0) : (0, b.facilityValveTargetHp), (s.StillTurns, s.TargetHpLess));
                checkedKinds++;
            }
            Assert.Greater(checkedKinds, 1);
        }

        [Test]
        public void Unmarked_Or_Unfitting_Order_SameBoard()
        {
            var wing = Wings[0];
            var p0 = Params(wing, 41, 1, false);
            var p1 = Params(wing, 41, 1, false);
            var floor = Tiles().First(t => Floor(wing[0].PlanCells(1, false), t));
            // Başka kanadın izi ve uygulanamayan iz (zeminde duvar izi): tahta izsiz emrinkiyle aynı
            p1.Marks = new List<FacilityMark> { M(Wings[1][0], "duvar", new Baskin.Pt(0, 0)), M(wing[0], "duvar", floor) };
            var s0 = RaidChain.Setup(B0, wing[0], p0, new List<RoomReport>(), false, Plan79);
            Assert.IsNull(s0.Marks);
            Assert.AreEqual(Baskin.Signature(Open(p0, new List<RoomReport>())), Baskin.Signature(Open(p1, new List<RoomReport>())));
            Assert.AreEqual("", p0.Mode);
            Assert.IsEmpty(p0.Marks);
        }
        // ---------- Simülasyon (P2) ----------
        static Simulation Sim(Action<Balance> tune = null)
        {
            var b = Bal(events: false);
            tune?.Invoke(b);
            return ShelterEventKit.New(b, 79);
        }

        /// <summary>Planla tahtanın hücre eşitliği (yapı, kapı, kepenk, nesne türü ve engelin canı; hedef dolabın türü tahtada core/node).</summary>
        static void AssertSameCells(Baskin.Cell[,] plan, Baskin.BState B, string where)
        {
            foreach (var t in Tiles())
            {
                var a = plan[t.Y, t.X];
                var c = Baskin.CellAt(B, t.X, t.Y);
                string at = where + " " + t.X + "," + t.Y;
                Assert.AreEqual(a.T, c.T, at);
                Assert.AreEqual(a.Double, c.Double, at);
                Assert.AreEqual(a.Shutter, c.Shutter, at);
                Assert.AreEqual(a.Door, c.Door, at);
                if (a.O != null && Baskin.IsTarget(a.O.K)) Assert.IsTrue(c.O != null && Baskin.IsTarget(c.O.K), at);
                else Assert.AreEqual(a.O?.K, c.O?.K, at);
                if (a.O != null && FacilityMarks.IsObjectKind(a.O.K)) Assert.AreEqual((a.O.Hp, a.O.Max), (c.O.Hp, c.O.Max), at);
            }
        }

        /// <summary>Tahtada iz bırakır (oyunun yaptığı gibi): bir iç duvar yıkılır (moloz), boş bir zemine tel konur. Kareler döner.</summary>
        static (Baskin.Pt Wall, Baskin.Pt Tel) Scar(Baskin.BState B)
        {
            var room = B.Room;
            var plan = room.PlanCells(1, false);
            var wall = Tiles().First(t => t.X > 0 && t.Y > 0 && t.X < 7 && t.Y < 7 && Baskin.CellAt(B, t.X, t.Y).T == "wall"
                && !Baskin.CellAt(B, t.X, t.Y).Shutter && !Baskin.CellAt(B, t.X, t.Y).Crate && plan[t.Y, t.X].T == "wall");
            var cell = Baskin.CellAt(B, wall.X, wall.Y);
            cell.T = "floor"; cell.Double = false;
            B.Rubble.Add(wall);
            plan[wall.Y, wall.X].T = "floor";
            var tel = SafeFloors(plan, room).First(t => Baskin.UnitAt(B, t.X, t.Y) == null && !Baskin.IsGateTile(B, t.X, t.Y)
                && !room.West.Any(w => Same(w, t)) && Floor(B.Cells, t));
            Baskin.CellAt(B, tel.X, tel.Y).O = new Baskin.Obj { K = "tel", Hp = 2, Max = 3 };
            return (wall, tel);
        }

        /// <summary>Başarısız baskın iz bırakır; tekrar emrinde emir kopyası, ilk ziyaret tahtası ve bina planı izleri taşır (hücre hücre).</summary>
        [Test]
        public void Sim_FailedRaid_LeavesMarks_RetryBoardAndPlanCarryThem()
        {
            var sim = Sim(b => b.facilityRepairPerShift = 0);
            ShelterEventKit.FillSquad(sim);
            ShelterEventKit.StartRaid(sim);
            string sector = sim.S.Raid.Params.SectorId;
            CollectionAssert.IsEmpty(sim.S.Raid.Params.Marks, "ilk emirde iz yok");
            var G = sim.OpenRaidRoom();
            Calm(G);
            var (wall, tel) = Scar(G);
            Assert.IsTrue(Baskin.Retreat(G));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(G)).Ok);
            Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
            var reward = sim.S.RaidLog.Last().LiberationReward;
            Assert.IsFalse(sim.S.RaidLog.Last().Success);
            Assert.AreEqual(2, reward.MarksNew);
            Assert.AreEqual(0, reward.MarksGone);
            var g = sim.FacilityPlan.Room(FacilityMaps.RoomId(sector, 1));
            Assert.AreEqual(Key(new[] { M(g, "duvar", wall), M(g, "tel", tel, 2, 3) }), Key(sim.FacilityMarksOf(g.Id)));
            Assert.IsTrue(sim.Facility.Marks.All(m => m.Day == sim.S.Day));
            ShelterEventKit.Close(sim);
            ShelterEventKit.FillSquad(sim);
            Assert.IsTrue(sim.StartLiberationRaid(sector).Ok);
            Assert.AreEqual(Key(sim.Facility.Marks), Key(sim.S.Raid.Params.Marks), "emir kopyası");
            Assert.AreNotSame(sim.Facility.Marks[0], sim.S.Raid.Params.Marks[0]);
            var R = sim.OpenRaidRoom();
            Assert.AreEqual("floor", Baskin.CellAt(R, wall.X, wall.Y).T);
            Assert.IsTrue(R.Rubble.Any(t => Same(t, wall)));
            Assert.AreEqual("tel", Baskin.CellAt(R, tel.X, tel.Y).O?.K);
            AssertSameCells(sim.FacilityPlanCells(g), R, g.Id);
            // Emirden sonra yazılan iz süren baskının tahtasını değiştirmez (emir kopyası)
            sim.Facility.Marks.Clear();
            Assert.AreEqual(Baskin.Signature(R), Baskin.Signature(sim.OpenRaidRoom()));
        }

        /// <summary>Onarım: işgaldeki her kanatta vardiya başına bir iz (listede en önde), bölge ve sektör sırasıyla; alınmış kanatta yok;
        /// günlüğe bugünün günüyle. Ayarla iki.</summary>
        [Test]
        public void Sim_Repair_OldestFirst_PerOccupiedWing_Logged()
        {
            foreach (int per in new[] { 1, 2 })
            {
                var sim = Sim(b => b.facilityRepairPerShift = per);
                var w1 = CaptureNext(sim);
                Assert.AreEqual("weu_1", w1.Id);
                var p = Plan79;
                FacilityMark Mk(string room, string kind, int x) => new FacilityMark { Room = room, Kind = kind, X = x, Y = 1, Day = 1 };
                sim.Facility.Marks.AddRange(new[]
                {
                    Mk("weu_2.1", "duvar", 1), Mk("weu_1.1", "tel", 2), Mk("weu_2.1", "tel", 3), Mk("weu_3.2", "kepenk", 4), Mk("weu_2.2", "donanim", 5),
                });
                ShelterEventKit.Close(sim);
                string at = "vardiya başına " + per;
                if (per == 1)
                {
                    Assert.AreEqual("weu_2.1:duvar:1:1:0:0:" + sim.S.Day + ";weu_3.2:kepenk:4:1:0:0:" + sim.S.Day,
                        string.Join(";", sim.Facility.Repairs.Select(MarkKey)), at);
                    Assert.AreEqual("weu_1.1:tel weu_2.1:tel weu_2.2:donanim", string.Join(" ", sim.Facility.Marks.Select(m => m.Room + ":" + m.Kind)), at);
                    ShelterEventKit.Close(sim);
                    Assert.AreEqual("weu_1.1:tel weu_2.2:donanim", string.Join(" ", sim.Facility.Marks.Select(m => m.Room + ":" + m.Kind)), at);
                }
                else
                {
                    Assert.AreEqual("weu_2.1:duvar weu_2.1:tel weu_3.2:kepenk", string.Join(" ", sim.Facility.Repairs.Select(m => m.Room + ":" + m.Kind)), at);
                    Assert.AreEqual("weu_1.1:tel weu_2.2:donanim", string.Join(" ", sim.Facility.Marks.Select(m => m.Room + ":" + m.Kind)), at);
                }
                Assert.IsTrue(sim.Facility.Marks.Any(m => m.Room == "weu_1.1"), "alınmış kanatta onarım yok");
            }
        }

        /// <summary>Onarılan donanım bina planına geri gelir ve hedef odası hazırlığı erir (emrin kırık donanım izi kalmaz).</summary>
        [Test]
        public void Sim_RepairedDevice_ComesBack_PreparationMelts()
        {
            var sim = Sim();
            var g = sim.FacilityPlan.Room("weu_1.1");
            Assert.IsNotNull(g.Device);
            var at = g.DeviceAt.Value;
            sim.Facility.Marks.Add(M(g, "donanim", at));
            Assert.IsNull(sim.FacilityPlanCells(g)[at.Y, at.X].O, "plan kırık donanımı gösterir");
            var p = new RaidParams { Marks = sim.WingMarks("weu_1") };
            CollectionAssert.AreEqual(new[] { g.Device }, RaidChain.MarkedDevices(p, sim.FacilityPlan));
            ShelterEventKit.Close(sim);
            CollectionAssert.IsEmpty(sim.Facility.Marks);
            Assert.AreEqual("donanim", sim.Facility.Repairs.Single().Kind);
            Assert.AreEqual(g.Device, sim.FacilityPlanCells(g)[at.Y, at.X].O?.K, "onarılan donanım geri gelir");
            CollectionAssert.IsEmpty(RaidChain.MarkedDevices(new RaidParams { Marks = sim.WingMarks("weu_1") }, sim.FacilityPlan), "hazırlık erir");
        }

        /// <summary>Savunmada terk edilen odanın izleri silinir (sonuçtan bağımsız); savunma tahtası izleri uygular, oynanan son odanın izleri
        /// raporundan güncellenir.</summary>
        [Test]
        public void Sim_Defense_WipesAbandonedRoom_KeepsLastRoom()
        {
            foreach (bool hold in new[] { true, false })
            {
                var sim = Sim();
                var w = CaptureNext(sim);
                var g = sim.FacilityPlan.Room(FacilityMaps.RoomId(w.Id, 1));
                var a = sim.FacilityPlan.Room(FacilityMaps.RoomId(w.Id, 2));
                var gTel = SafeFloors(g.PlanCells(1, false), g).Last();
                var aTel = SafeFloors(a.PlanCells(1, false), a).Last();
                sim.Facility.Marks.Add(M(g, "tel", gTel, 2, 3));
                sim.Facility.Marks.Add(M(a, "tel", aTel, 2, 3));
                ForceSiege(sim);
                Defend(sim);
                Assert.AreEqual(2, sim.S.Raid.Params.Marks.Count);
                bool sawG = false, sawA = false;
                PlayDefense(sim, 1, hold, B =>
                {
                    if (B.Room.Index == 1) sawG = Baskin.CellAt(B, gTel.X, gTel.Y).O?.K == "tel";
                    if (B.Room.Index == 2) sawA = Baskin.CellAt(B, aTel.X, aTel.Y).O?.K == "tel";
                });
                string at = hold ? "tutuldu" : "düştü";
                Assert.IsTrue(sawG && sawA, at + ": savunma tahtası izleri uygular");
                var reward = sim.S.RaidLog.Last().LiberationReward;
                Assert.AreEqual(1, reward.MarksWiped, at);
                CollectionAssert.IsEmpty(sim.FacilityMarksOf(g.Id), at + ": terk edilen odanın izi silinir");
                Assert.AreEqual(Key(new[] { M(a, "tel", aTel, 2, 3) }), Key(sim.FacilityMarksOf(a.Id)), at + ": son odanın izi raporundan");
            }
        }

        /// <summary>Karar verilmeyen kuşatmanın kanadı aynı kapanışta düşer ve onarılır (K11: bırakma onarımdan önce).</summary>
        [Test]
        public void Sim_AutoYieldedWing_RepairedTheSameNight()
        {
            var sim = Sim();
            var w = CaptureNext(sim);
            var g = sim.FacilityPlan.Room(FacilityMaps.RoomId(w.Id, 1));
            sim.Facility.Marks.Add(M(g, "duvar", new Baskin.Pt(0, 0)));
            ForceSiege(sim);
            CollectionAssert.IsEmpty(sim.Facility.Repairs, "alınmış kanat onarılmaz");
            ShelterEventKit.Close(sim);
            Assert.IsFalse(w.Captured);
            Assert.AreEqual(g.Id, sim.Facility.Repairs.Single().Room);
        }

        /// <summary>Alt bayrak kapalı: iz toplanmaz, emir izsiz, onarım yok, plan izleri uygulamaz (aşama 10 tahtaları).</summary>
        [Test]
        public void Sim_MarksOff_Stage10Boards()
        {
            var sim = Sim(b => b.facilityMarks = false);
            Assert.IsFalse(sim.MarksOn);
            ShelterEventKit.FillSquad(sim);
            ShelterEventKit.StartRaid(sim);
            var G = sim.OpenRaidRoom();
            Calm(G);
            Scar(G);
            Assert.IsTrue(Baskin.Retreat(G));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(G)).Ok);
            Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
            CollectionAssert.IsEmpty(sim.Facility.Marks);
            Assert.AreEqual(0, sim.S.RaidLog.Last().LiberationReward.MarksNew);
            var g = G.Room;
            sim.Facility.Marks.Add(M(g, "duvar", new Baskin.Pt(0, 0)));
            Assert.AreEqual(g.PlanCells(1, false)[0, 0].T, sim.FacilityPlanCells(g)[0, 0].T, "plan izi uygulamaz");
            CollectionAssert.IsEmpty(sim.FacilityMarksOf(g.Id));
            ShelterEventKit.Close(sim);
            CollectionAssert.IsEmpty(sim.Facility.Repairs, "onarım yok");
            ShelterEventKit.FillSquad(sim);
            ShelterEventKit.StartRaid(sim);
            CollectionAssert.IsEmpty(sim.S.Raid.Params.Marks, "emir izsiz");
        }
    }
}
