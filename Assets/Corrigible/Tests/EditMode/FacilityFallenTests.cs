using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis baskınında düşen kişi (aşama 6, Docs/TESIS-BASKINLARI-TASARIMI.md §4.3–4.4, §3.6): gövde tahtada kalır, sürükleme
    /// (bıraktığı kare, menzil, kapı, tesisin girişi, geri alma), yerdeki yük (düşme, alma, odada kalma), oda raporunun yeni parçaları,
    /// zincirde taşıma ve dönüşte yerleştirme, temiz odadan toplama, yürüyerek çıkış, ölüm kuralının tablo satırları ve kayıt.
    /// Motor testleri olaysız kurulur ve makineleri kapatır.
    /// </summary>
    public class FacilityFallenTests
    {
        static Balance B0 => Bal(events: false);

        /// <summary>Zincir testleri için makineli oda (giriş 2, ara 3 makine): dönüşte oda temiz kalmasın.</summary>
        static Balance BM
        {
            get
            {
                var b = Bal(events: false);
                b.facilityEntryMachines = 2;
                b.facilityMiddleMachines = 3;
                return b;
            }
        }

        static Simulation NewSim() => Sim(tune: b => b.liberationRegionEvents = false);

        /// <summary>Makineler ve kopya dolapları kalkar: oda geçilince temizdir.</summary>
        static void Clean(Baskin.BState B)
        {
            Calm(B);
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    if (Baskin.CellAt(B, x, y).O?.K == "spawner") Baskin.CellAt(B, x, y).O = null;
        }

        /// <summary>G'nin tahtası (olaysız, makinesiz): kadro batı girişinde, 0 ve 1 bitişik.</summary>
        static (RaidParams P, List<RoomReport> Reps, Baskin.BState B) Entry(IReadOnlyList<FacilityRoom> wing, int seed = 13)
        {
            var p = Params(wing, seed, 1, false);
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            Calm(B);
            return (p, reps, B);
        }

        /// <summary>İkmal kanadının H'si (elle G ve A raporu), kaçış başladı (taşıyıcı Teknisyen), makinesiz.</summary>
        static (RaidParams P, List<RoomReport> Reps, Baskin.BState B) LootTarget(int seed = 17)
        {
            var wing = Wings.First(w => w[0].Mission == "loot");
            var p = Params(wing, seed, 1, false);
            var reps = new List<RoomReport> { Rep(1), Rep(2) };
            var H = Open(p, reps, B0);
            Calm(H);
            StartEscape(H);
            return (p, reps, H);
        }

        /// <summary>Uzmanı bir kapı karesinin boş komşusuna, gövdeyi aynı kareye koyar ve kapıya sürükler (Assert).</summary>
        static void DragToGate(Baskin.BState B, Baskin.Unit dragger, Baskin.Unit body)
        {
            foreach (var g in Baskin.GateTiles(B))
            {
                if (Baskin.UnitAt(B, g.X, g.Y) != null) continue;
                var n = FreeNeighbor(B, g);
                if (n == null) continue;
                Place(dragger, n.Value);
                Place(body, n.Value);
                Assert.IsTrue(Baskin.DoAction(B, dragger.Id, Baskin.ActDrag, g.X, g.Y, body.Id), "kapıya sürükleme: " + B.Room.Id);
                return;
            }
            Assert.Fail("kapıya sürükleme yeri yok: " + B.Room.Id);
        }

        /// <summary>Öbür ayaktakiler kapıya girer, sonra <paramref name="dragger"/> gövdeyi kapıya sürükler.</summary>
        static void DragOut(Baskin.BState B, Baskin.Unit dragger, Baskin.Unit body)
        {
            foreach (var u in Standing(B).Where(u => u != dragger).ToList()) ToGate(B, u);
            DragToGate(B, dragger, body);
        }

        // ---------- Tahta: gövde ----------
        [Test]
        public void Body_StaysOnTile_NotTargeted()
        {
            var (_, _, B) = Entry(Wings[0]);
            var dragger = BySlot(B, 0); var body = BySlot(B, 1);
            int x = body.X, y = body.Y;
            Down(B, body);
            Assert.IsTrue(B.Units.Contains(body), "gövde tahtada kalır");
            Assert.IsTrue(Baskin.IsBody(body));
            Assert.IsTrue(Baskin.LyingBody(body));
            Assert.IsNull(Baskin.UnitAt(B, x, y), "gövde kareyi kapatmaz");
            Assert.IsFalse(Baskin.Alive(B, 'p').Contains(body));
            Assert.IsTrue(Baskin.Reachable(B, dragger).Any(t => t.X == x && t.Y == y), "gövdenin karesine yürünür");
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((x, y), (body.X, body.Y), "makine evresinden sonra yerinde");
        }

        // ---------- Sürükleme ----------
        [Test]
        public void Drag_BodyToLeftTile()
        {
            int tested = 0;
            foreach (var wing in Wings.Take(12))
            {
                var (_, _, B) = Entry(wing);
                var dragger = BySlot(B, 0); var body = BySlot(B, 1);
                Down(B, body);
                CollectionAssert.Contains(Baskin.DragBodies(B, dragger), body.Id);
                int range = (dragger.MoveRange > 0 ? dragger.MoveRange : Baskin.UnitTypes[dragger.Type].Move) - B.Moves.DragCost;
                var targets = Baskin.ActionTargets(B, dragger, Baskin.ActDrag, body.Id);
                Assert.IsNotEmpty(targets, wing[0].Id);
                foreach (int steps in new[] { 1, 2 })
                {
                    var cand = targets.Where(q => Baskin.WalkPath(B, dragger, range, q.X, q.Y).Count == steps + 1 && !Baskin.IsGateTile(B, q.X, q.Y)).ToList();
                    if (cand.Count == 0) continue;
                    var t = cand[0];
                    var C = B.Clone();
                    var cu = C.Units.First(u => u.Id == dragger.Id); var cb = C.Units.First(u => u.Id == body.Id);
                    var path = Baskin.WalkPath(C, cu, range, t.X, t.Y);
                    var left = path[path.Count - 2];
                    Assert.IsTrue(Baskin.DoAction(C, cu.Id, Baskin.ActDrag, t.X, t.Y, cb.Id));
                    Assert.AreEqual((t.X, t.Y), (cu.X, cu.Y));
                    Assert.AreEqual((left.X, left.Y), (cb.X, cb.Y), wing[0].Id + ": gövde bıraktığı karede");
                    if (steps == 1) Assert.AreEqual((dragger.X, dragger.Y), (cb.X, cb.Y), "tek adımda başlangıç karesi");
                    Assert.IsTrue(cu.Moved && cu.Acted, "yürüyüş ve yetenek harcanır");
                    Assert.IsFalse(Baskin.CanAct(C, cu));
                    Assert.IsTrue(cb.Dead && !cb.AtGate && !cb.Out);
                    var fx = C.FxList.Single(f => f.K == "drag");
                    Assert.AreEqual(cb.Id, fx.Unit);
                    Assert.AreEqual((body.X, body.Y, cb.X - body.X, cb.Y - body.Y), (fx.X, fx.Y, fx.Dx, fx.Dy));
                    tested++;
                }
            }
            Assert.Greater(tested, 12);
        }

        [Test]
        public void Drag_RangeMinusCost()
        {
            foreach (var wing in Wings.Take(6))
            {
                var (_, _, B) = Entry(wing);
                var dragger = BySlot(B, 0); var body = BySlot(B, 1);
                Down(B, body);
                int move = dragger.MoveRange > 0 ? dragger.MoveRange : Baskin.UnitTypes[dragger.Type].Move;
                var walk = Baskin.Reachable(B, dragger);
                var drag = Baskin.ActionTargets(B, dragger, Baskin.ActDrag, body.Id);
                Assert.IsTrue(drag.All(t => Baskin.WalkPath(B, dragger, move, t.X, t.Y).Count - 1 <= move - 1), wing[0].Id);
                Assert.IsTrue(drag.All(t => walk.Any(w => w.X == t.X && w.Y == t.Y)));
                Assert.Less(drag.Count, walk.Count, wing[0].Id + ": menzil bir kısa");
                B.Moves = new MoveRules(4, 3, 2, 3, 0);
                CollectionAssert.AreEqual(walk.Select(t => t.Y * 8 + t.X), Baskin.ActionTargets(B, dragger, Baskin.ActDrag, body.Id).Select(t => t.Y * 8 + t.X),
                    "bedel 0: Reachable'ın aynısı (aynı kural ve sıra)");
                B.Moves = new MoveRules(4, 3, 2, 3, move);
                Assert.IsEmpty(Baskin.ActionTargets(B, dragger, Baskin.ActDrag, body.Id), "menzil 0: sürükleme yok");
            }
        }

        [Test]
        public void Drag_NeedsBodyNextToDragger_AndCanMove()
        {
            var (_, _, B) = Entry(Wings[1]);
            var dragger = BySlot(B, 0); var body = BySlot(B, 1); var third = BySlot(B, 2);
            Down(B, body);
            Assert.IsEmpty(Baskin.ActionTargets(B, dragger, Baskin.ActDrag, 0), "gövde verilmedi");
            Assert.IsEmpty(Baskin.ActionTargets(B, dragger, Baskin.ActDrag, third.Id), "ayaktaki uzman sürüklenmez");
            Assert.IsEmpty(Baskin.DragBodies(B, third).Where(id => Math.Abs(body.X - third.X) + Math.Abs(body.Y - third.Y) > 1));
            var far = FindLine(B, 0, pt => Math.Abs(pt.X - dragger.X) + Math.Abs(pt.Y - dragger.Y) >= 3);
            Assert.IsNotNull(far);
            Place(body, far.Value.At);
            Assert.IsEmpty(Baskin.DragBodies(B, dragger), "uzak gövde");
            Place(body, new Baskin.Pt(dragger.X, dragger.Y));
            CollectionAssert.AreEqual(new[] { body.Id }, Baskin.DragBodies(B, dragger), "üstündeki gövde");
            dragger.Moved = true;
            Assert.IsFalse(Baskin.CanAction(B, dragger, Baskin.ActDrag), "kilitli/donmuş ya da yürümüş: sürükleme yok");
            Assert.IsEmpty(Baskin.ActionTargets(B, dragger, Baskin.ActDrag, body.Id));
        }

        [Test]
        public void Drag_IntoGate_BodyAtGate_CarriedToNextRoom()
        {
            foreach (var wing in Wings.Take(9))
            {
                var (p, reps, B) = Entry(wing);
                var dragger = BySlot(B, 0); var body = BySlot(B, 1);
                Down(B, body);
                DragToGate(B, dragger, body);
                Assert.IsTrue(dragger.AtGate);
                Assert.IsTrue(body.AtGate, "gövde sürükleyenle kapıda");
                Assert.AreEqual((dragger.X, dragger.Y), (body.X, body.Y));
                Assert.IsTrue(Baskin.IsBody(body));
                Assert.IsFalse(Baskin.LyingBody(body));
                Assert.IsFalse(Baskin.CanPass(B), "üçüncü uzman tahtada");
                var r = PassRoom(p, reps, B);
                Assert.AreEqual("d", r.Team[1].State);
                Assert.AreEqual(1, r.Team[1].Body, "ekiple");
                Assert.AreEqual(0, r.Team[1].Picked);
                Assert.AreEqual(1, r.Team[1].Fell);
                Assert.AreEqual(0, r.Team[0].Body);
                StringAssert.Contains(":1:0", r.Encode());
                var A = Open(p, reps, B0);
                var placed = BySlot(A, 1);
                Assert.IsNotNull(placed, wing[1].Id + ": ekiple gelen gövde sonraki odaya konur");
                Assert.IsTrue(placed.Dead && placed.Hp == 0 && !placed.AtGate);
                Assert.AreEqual(A.Units.Max(u => u.Id), placed.Id, "gövde en sonda eklenir");
                Assert.AreNotEqual("wall", Baskin.CellAt(A, placed.X, placed.Y).T);
                Assert.IsFalse(A.Units.Any(u => u.Side == 'e' && !u.Dead && u.X == placed.X && u.Y == placed.Y), "makine gövdenin üstünde başlamaz");
                // Aynı raporla gövdesiz: öbür birimlerin kimlik ve tür sırası aynı
                var alt = reps.Select(x => RoomReport.Decode(x.Encode())).ToList();
                alt[0].Team[1].Body = 0;
                var A0 = Open(p, alt, B0);
                CollectionAssert.AreEqual(A0.Units.Select(u => u.Id + u.Type), A.Units.Where(u => u != placed).Select(u => u.Id + u.Type));
                Assert.IsNull(BySlot(A0, 1), "gövdesiz raporda düşen konmaz (bugünkü kural)");
            }
        }

        [Test]
        public void Drag_FinalExit_BodyOut()
        {
            foreach (var wing in Wings.Where(w => w[0].Mission == "server").Take(4))
            {
                var (p, reps, B) = Entry(wing);
                B.Escape = true;
                Assert.IsTrue(Baskin.IsFinalExit(B));
                var dragger = BySlot(B, 0); var body = BySlot(B, 1);
                Down(B, body);
                DragOut(B, dragger, body);
                Assert.IsTrue(dragger.Out && body.Out, "ikisi de tesisten çıktı");
                Assert.AreEqual("won", B.Phase);
                StringAssert.Contains("2/3", B.OutcomeReason, "çıkarılan düşen çıkan uzman sayılmaz");
                var r = File(p, reps, B);
                Assert.AreEqual(2, r.Team[1].Body, "tesisten çıkarıldı");
                Assert.IsTrue(r.Last);
                CollectionAssert.AreEqual(new[] { 1 }, RaidChain.Result(p, reps).Extracted);
            }
        }

        [Test]
        public void Drag_Undo()
        {
            var (_, _, B) = Entry(Wings[2]);
            var dragger = BySlot(B, 0); var body = BySlot(B, 1);
            Down(B, body);
            var t = Baskin.ActionTargets(B, dragger, Baskin.ActDrag, body.Id).Last();
            AssertUndoRestores(B, b => Baskin.DoAction(b, dragger.Id, Baskin.ActDrag, t.X, t.Y, body.Id), "sürükleme");
            // Kapıya sürükleme de geri alınır (gövde yeniden yerde); geri alma birimleri kopyalarıyla değiştirir
            dragger = BySlot(B, 0); body = BySlot(B, 1);
            var g = Baskin.GateTiles(B).First(q => FreeNeighbor(B, q) != null);
            var n = FreeNeighbor(B, g).Value;
            Place(dragger, n); Place(body, n);
            B.Hist.Clear();
            AssertUndoRestores(B, b => Baskin.DoAction(b, dragger.Id, Baskin.ActDrag, g.X, g.Y, body.Id), "kapıya sürükleme");
            Assert.IsFalse(BySlot(B, 1).AtGate);
        }

        [Test]
        public void Drag_Preview_EqualsDoAction()
        {
            var (_, _, B) = Entry(Wings[3]);
            var dragger = BySlot(B, 0); var body = BySlot(B, 1);
            Down(B, body);
            var t = Baskin.ActionTargets(B, dragger, Baskin.ActDrag, body.Id).Last();
            string dump = FullDump(B);
            var pv = Baskin.PreviewAction(B, dragger.Id, Baskin.ActDrag, t.X, t.Y, body.Id);
            Assert.AreEqual(dump, FullDump(B), "önizleme durumu değiştirmez");
            Assert.IsNull(Baskin.PreviewAction(B, dragger.Id, Baskin.ActDrag, t.X, t.Y, 0), "geçersiz eylem");
            Assert.IsNull(Baskin.PreviewAction(B, dragger.Id, Baskin.ActDrag, dragger.X, dragger.Y, body.Id));
            var before = B.Units.ToDictionary(u => u.Id, u => (u.X, u.Y));
            Assert.IsTrue(Baskin.DoAction(B, dragger.Id, Baskin.ActDrag, t.X, t.Y, body.Id));
            var moved = B.Units.Where(u => before[u.Id] != (u.X, u.Y)).Select(u => (u.Id, before[u.Id].X, before[u.Id].Y, u.X, u.Y)).ToList();
            CollectionAssert.AreEquivalent(moved, pv.Moves.Select(m => (m.Unit, m.From.X, m.From.Y, m.To.X, m.To.Y)).ToList(), "gövdenin yer değişimi önizlemede");
            CollectionAssert.AreEqual(B.FxList.Select(f => f.K), pv.Fx.Select(f => f.K));
            Assert.AreEqual(B.Carrier, pv.Carrier);
        }

        // ---------- Yük ----------
        [Test]
        public void CarrierFalls_CargoDrops()
        {
            var (_, _, H) = LootTarget();
            var c = H.Units.First(u => u.Id == H.Carrier);
            int x = c.X, y = c.Y;
            Down(H, c);
            Assert.AreEqual(0, H.Carrier, "taşıyıcı yok");
            Assert.AreEqual((x, y), (H.CargoX, H.CargoY), "yük düştüğü karede");
            Assert.IsTrue(H.FxList.Any(f => f.K == "cargo-drop" && f.X == x && f.Y == y));
            Assert.AreEqual(Loc.F("{0} düştü: yük yere düştü. Kareye giren ilk ekip üyesi alır.", Baskin.UnitTypes[c.Type].Name), H.Log.Last().Text);
            StringAssert.Contains($"|y{x}{y}", Baskin.Signature(H));
        }

        [Test]
        public void Cargo_PickedByOwnMove()
        {
            foreach (string how in new[] { "walk", "drag" })
            {
                var (_, _, H) = LootTarget();
                var c = H.Units.First(u => u.Id == H.Carrier);
                var team = Standing(H).Where(u => u != c).ToList();
                var cargo = new Baskin.Pt(c.X, c.Y);
                Down(H, c);
                var mover = team[0];
                var n = FreeNeighbor(H, cargo);
                Assert.IsNotNull(n);
                Place(mover, n.Value);
                if (how == "walk")
                {
                    AssertUndoRestores(H, b => Baskin.DoMove(b, mover.Id, cargo.X, cargo.Y), "yük alma geri alınır");
                    Assert.IsTrue(Baskin.DoMove(H, mover.Id, cargo.X, cargo.Y));
                }
                else
                {
                    // Sürükleyen yükün karesine varır: gövdeyi (taşıyıcının kendisi) yanından alıp yükü de alır
                    Assert.IsTrue(Baskin.DoAction(H, mover.Id, Baskin.ActDrag, cargo.X, cargo.Y, c.Id));
                }
                Assert.AreEqual(mover.Id, H.Carrier, how);
                Assert.AreEqual(-1, H.CargoX);
                Assert.IsTrue(H.FxList.Any(f => f.K == "cargo-pick"));
            }
        }

        [Test]
        public void Cargo_LeftInRoom_ReportMinus2()
        {
            var (p, reps, H) = LootTarget();
            var c = H.Units.First(u => u.Id == H.Carrier);
            int key = c.Y * Baskin.N + c.X;
            Down(H, c);
            var r = PassRoom(p, reps, H);
            Assert.AreEqual(-2, r.Cargo, "yük odada kaldı: kayıp");
            Assert.AreEqual(key, r.CargoAt);
            Assert.IsTrue(r.Last);
            Assert.IsFalse(r.Success);
            Assert.AreEqual(key, RoomReport.Decode(r.Encode()).CargoAt);
        }

        [Test]
        public void Cargo_LeftInRoom_FinalExitReason()
        {
            var wing = Wings.First(w => w[0].Mission == "loot");
            var (p, reps, G) = Entry(wing);
            G.Escape = true;
            var tek = Standing(G).First(u => u.Type == "tek");
            G.Carrier = tek.Id;
            Down(G, tek);
            foreach (var u in Standing(G)) ToGate(G, u);
            Assert.AreEqual("lost", G.Phase);
            Assert.AreEqual(Loc.T("Yük odada kaldı: ikmal malzemeleri kaybedildi."), G.OutcomeReason);
        }

        // ---------- Rapor biçimi ----------
        [Test]
        public void Report_BodyParts_OnlyWhenSet()
        {
            var r = Rep(1);
            r.Team[1] = new RoomMember { State = "d", Fell = 1, X = 2, Y = 3 };
            Assert.AreEqual("1|0|0|pass|0|0|0|-1|0|0||a:3:0:0:-1:0:-1:-1,d:0:0:0:-1:1:2:3,a:3:0:0:-1:0:-1:-1|||", r.Encode(), "gövdesiz: eski dizgi");
            r.Team[1].Body = 1;
            StringAssert.Contains(",d:0:0:0:-1:1:2:3:1:0,", r.Encode());
            r.Team[1].Body = 0; r.Team[1].Picked = 1;
            StringAssert.Contains(",d:0:0:0:-1:1:2:3:0:1,", r.Encode());
            var d = RoomReport.Decode(r.Encode());
            Assert.AreEqual((0, 1), (d.Team[1].Body, d.Team[1].Picked));
            Assert.AreEqual(r.Encode(), d.Encode());
        }

        [Test]
        public void Report_Decode_OldString_Defaults()
        {
            const string old = "3|0|7|won|1|1|0|-2|2|1||a:3:1:2:-1:0:5:2,d:0:0:0:1:1:4:4,l:0:1:0:0:3:0:7|dron:5:2:2:0|civ:1:3:2:2|19,27";
            var r = RoomReport.Decode(old);
            Assert.IsTrue(r.Team.All(m => m.Body == 0 && m.Picked == 0));
            Assert.AreEqual(-1, r.CargoAt);
            Assert.IsEmpty(r.Opened);
            Assert.IsFalse(r.Crate);
            Assert.AreEqual(old, r.Encode(), "eski dizgi aynen döner");
        }

        [Test]
        public void Report_TailFields_Trimmed()
        {
            var r = Rep(2);
            string baseline = r.Encode();
            Assert.AreEqual(15, baseline.Split('|').Length);
            r.CargoAt = 12;
            Assert.AreEqual(baseline + "|12", r.Encode());
            r.CargoAt = -1; r.Opened.Add(19);
            Assert.AreEqual(baseline + "|-1|19", r.Encode());
            r.Opened.Add(27); r.Crate = true;
            Assert.AreEqual(baseline + "|-1|19,27|1", r.Encode());
            r.Opened.Clear();
            Assert.AreEqual(baseline + "|-1||1", r.Encode(), "boş liste yer tutar");
            var d = RoomReport.Decode(r.Encode());
            Assert.IsTrue(d.Crate);
            Assert.IsEmpty(d.Opened);
            Assert.AreEqual(r.Encode(), d.Encode());
        }

        [Test]
        public void RaidResult_Extracted_RoundTrip_OldStringSame()
        {
            var a = new RaidResult { Success = true, Collateral = 1, Down = 1, Turns = 9, Hp = { 3, 0, 2 }, Armor = { 0, 0, 1 }, CiviliansLeft = 2 };
            Assert.AreEqual("1|0|1|1|9|3,0,2|0,0,1|2", a.Encode(), "Extracted boş: eski dizgi");
            a.Left.Add(2);
            Assert.AreEqual("1|0|1|1|9|3,0,2|0,0,1|2|2", a.Encode());
            a.Left.Clear(); a.Extracted.Add(1);
            Assert.AreEqual("1|0|1|1|9|3,0,2|0,0,1|2||1", a.Encode(), "Left boş da olsa yer tutar");
            var d = RaidResult.Decode(a.Encode());
            Assert.IsEmpty(d.Left);
            CollectionAssert.AreEqual(new[] { 1 }, d.Extracted);
            Assert.IsEmpty(RaidResult.Decode("1|0|1|1|9|3,0,2|0,0,1|2|2").Extracted);
        }

        // ---------- Zincir ----------
        [Test]
        public void Lying_PlacedOnReturnBoard()
        {
            int tested = 0;
            foreach (var wing in Wings.Take(12))
            {
                var p = Params(wing, 23, 1, false);
                var reps = new List<RoomReport>();
                var G = Open(p, reps, BM); ClearGate(G); PassRoom(p, reps, G);
                var A = Open(p, reps, BM); ClearGate(A);
                var body = Standing(A)[1];
                int slot = body.Slot, x = body.X, y = body.Y;
                Down(A, body);
                var ra = PassRoom(p, reps, A);
                Assert.AreEqual((0, 2, x, y), (ra.Team[slot].Body, ra.Team[slot].Fell, ra.Team[slot].X, ra.Team[slot].Y));
                var H = Open(p, reps, BM); StartEscape(H); Calm(H); PassRoom(p, reps, H);
                Assert.AreEqual(0, reps[2].Team[slot].Body, "H'de taşınmadı");
                if (RaidChain.Next(p, reps)?.Room != 2) continue;
                var R = Open(p, reps, BM);
                var lying = BySlot(R, slot);
                Assert.IsNotNull(lying, wing[1].Id + ": dönüş odasında yatan gövde");
                Assert.IsTrue(Baskin.LyingBody(lying));
                if (Baskin.CellAt(R, x, y).T != "wall" && Baskin.CellAt(R, x, y).O == null) Assert.AreEqual((x, y), (lying.X, lying.Y));
                Assert.AreEqual(R.Units.Max(u => u.Id), lying.Id);
                tested++;
            }
            Assert.Greater(tested, 3);
        }

        [Test]
        public void Clean_Collects_IntoNextReturnRoom_ThenExtracted()
        {
            int tested = 0;
            foreach (var wing in Wings.Take(12))
            {
                var p = Params(wing, 29, 1, false);
                var reps = new List<RoomReport>();
                var G = Open(p, reps, BM); ClearGate(G); PassRoom(p, reps, G);
                var A = Open(p, reps, BM); Clean(A);
                var body = Standing(A)[1];
                int slot = body.Slot;
                Down(A, body);
                PassRoom(p, reps, A);
                var H = Open(p, reps, BM); StartEscape(H); Calm(H); PassRoom(p, reps, H);
                if (RaidChain.Next(p, reps)?.Room != 1) continue;
                var setup = RaidChain.Setup(BM, FacilityKit.Plan79.Room(p.Rooms[0]), p, reps, true);
                Assert.AreEqual((1, 1), (setup.Carry[slot].Body, setup.Carry[slot].Picked), wing[1].Id + ": temiz A'dan toplandı");
                Assert.AreEqual(0, reps.Last().Team[slot].Body, "rapor değişmez: toplama kurulumdadır");
                var R = Open(p, reps, BM);
                var carried = BySlot(R, slot);
                Assert.IsNotNull(carried, "toplanan gövde ekiple G'ye girer");
                Assert.IsTrue(Baskin.LyingBody(carried));
                Calm(R);
                var dragger = Standing(R).First();
                DragOut(R, dragger, carried);
                Assert.AreNotEqual("player", R.Phase);
                var r = File(p, reps, R);
                Assert.AreEqual((2, 1), (r.Team[slot].Body, r.Team[slot].Picked));
                CollectionAssert.Contains(RaidChain.Result(p, reps).Extracted, slot);
                tested++;
            }
            Assert.Greater(tested, 2);
        }

        [Test]
        public void WalkOut_ExtractsCarriedAndClean()
        {
            foreach (var wing in Wings.Take(6))
            {
                var p = Params(wing, 31, 1, false);
                var reps = new List<RoomReport>();
                var G = Open(p, reps, B0); Clean(G);
                var lyingG = Standing(G)[1];
                Down(G, lyingG);
                PassRoom(p, reps, G);
                var A = Open(p, reps, B0); Clean(A); PassRoom(p, reps, A);
                var H = Open(p, reps, B0); StartEscape(H); Clean(H);
                var team = Standing(H);
                var victim = team.First(u => u.Id != H.Carrier);
                var dragger = team.First(u => u != victim);
                Down(H, victim);
                DragOut(H, dragger, victim);
                Assert.IsTrue(Baskin.Pass(H), wing[2].Id);
                var r = File(p, reps, H);
                Assert.IsTrue(r.Last, "G ve A temiz: yürüyerek çıkış");
                Assert.AreEqual((2, 1), (r.Team[lyingG.Slot].Body, r.Team[lyingG.Slot].Picked), "batıdaki temiz odada yatan çıkarılır");
                Assert.AreEqual((2, 0), (r.Team[victim.Slot].Body, r.Team[victim.Slot].Picked), "kapıdan ekiple geçen çıkarılır");
                CollectionAssert.AreEquivalent(new[] { lyingG.Slot, victim.Slot }, RaidChain.Result(p, reps).Extracted);
            }
        }

        [Test]
        public void WalkOut_LyingInLastRoom_NotExtracted()
        {
            var wing = Wings[5];
            var p = Params(wing, 33, 1, false);
            var reps = new List<RoomReport>();
            var G = Open(p, reps, B0); Clean(G); PassRoom(p, reps, G);
            var A = Open(p, reps, B0); Clean(A); PassRoom(p, reps, A);
            var H = Open(p, reps, B0); StartEscape(H); Clean(H);
            var victim = Standing(H).First(u => u.Id != H.Carrier);
            Down(H, victim);
            var r = PassRoom(p, reps, H);
            Assert.IsTrue(r.Last);
            Assert.AreEqual(0, r.Team[victim.Slot].Body, "son odada yerde kalan çıkarılmaz");
            Assert.IsEmpty(RaidChain.Result(p, reps).Extracted);
        }

        [Test]
        public void Opened_ShutterStaysOpenOnReturn()
        {
            int tested = 0;
            foreach (var wing in Wings.Where(w => w[1].Shutter != null).Take(12))
            {
                var p = Params(wing, 37, 1, false);
                var reps = new List<RoomReport>();
                var G = Open(p, reps, BM); ClearGate(G); PassRoom(p, reps, G);
                var A = Open(p, reps, BM); ClearGate(A);
                var s = wing[1].Shutter.Value;
                Assert.IsTrue(Baskin.CellAt(A, s.X, s.Y).Shutter && Baskin.CellAt(A, s.X, s.Y).T == "wall");
                Baskin.CellAt(A, s.X, s.Y).T = "floor";
                StringAssert.Contains($"|k{s.X}{s.Y}", Baskin.Signature(A));
                var ra = PassRoom(p, reps, A);
                CollectionAssert.AreEqual(new[] { s.Y * 8 + s.X }, ra.Opened);
                var H = Open(p, reps, BM); StartEscape(H); Calm(H); PassRoom(p, reps, H);
                if (RaidChain.Next(p, reps)?.Room != 2) continue;
                var R = Open(p, reps, BM);
                Assert.AreEqual("floor", Baskin.CellAt(R, s.X, s.Y).T, wing[1].Id + ": dönüşte kepenk açık");
                Assert.IsTrue(Baskin.CellAt(R, s.X, s.Y).Shutter, "bayrak kalır");
                Assert.IsFalse(R.Rubble.Any(t => t.X == s.X && t.Y == s.Y), "moloz değil");
                tested++;
            }
            Assert.Greater(tested, 2);
        }

        // ---------- Ölüm kuralı (simülasyon) ----------
        [Test]
        public void Death_CollectedOrExtracted_Recovers([Values(0, 1)] int picked)
        {
            var sim = NewSim();
            var squad = sim.S.Raid.Params.Squad.ToList();
            int n = squad.Count;
            Assert.IsTrue(sim.AdvanceRaidRoom(Rep(1, squad: n)).Ok);
            Assert.IsTrue(sim.AdvanceRaidRoom(Rep(2, squad: n)).Ok);
            var last = Rep(3, squad: n); last.Last = true; last.Success = true;
            last.Team[1] = new RoomMember { State = "d", Fell = picked == 1 ? 1 : 3, Body = 2, Picked = picked };
            Assert.IsTrue(sim.AdvanceRaidRoom(last).Ok);
            CollectionAssert.AreEqual(new[] { 1 }, sim.FacilityRaidResult().Extracted);
            Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
            var person = sim.Board.Person(squad[1].PersonId);
            Assert.IsTrue(person.Alive, "çıkarılan düşen yaşar");
            Assert.Greater(person.Down, 0, "yatış");
            StringAssert.EndsWith("||1", sim.Record.commands.Last(c => c.kind == "raidres").a, "raidres Extracted taşır (Left boş)");
        }

        [Test]
        public void Death_NotCarried_Dies([Values(1, 3)] int fell)
        {
            var sim = NewSim();
            var squad = sim.S.Raid.Params.Squad.ToList();
            int n = squad.Count;
            Assert.IsTrue(sim.AdvanceRaidRoom(Rep(1, squad: n)).Ok);
            Assert.IsTrue(sim.AdvanceRaidRoom(Rep(2, squad: n)).Ok);
            var last = Rep(3, squad: n); last.Last = true; last.Success = true;
            last.Team[1] = new RoomMember { State = "d", Fell = fell, Body = 0 };
            Assert.IsTrue(sim.AdvanceRaidRoom(last).Ok);
            Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
            Assert.IsFalse(sim.Board.Person(squad[1].PersonId).Alive, "taşınmayan düşen ölür");
            Assert.IsTrue(sim.Board.Person(squad[0].PersonId).Alive);
            Assert.IsTrue(sim.Board.Person(squad[2].PersonId).Alive);
        }

        [Test]
        public void Death_Retreat_LyingDie_StandingReturn()
        {
            var sim = NewSim();
            var squad = sim.S.Raid.Params.Squad.ToList();
            var G = sim.OpenRaidRoom();
            Calm(G);
            Down(G, BySlot(G, 1));
            Assert.IsTrue(Baskin.Retreat(G));
            var r = sim.RaidRoomReport(G);
            Assert.IsTrue(r.Last && r.Retreat);
            Assert.IsTrue(sim.AdvanceRaidRoom(r).Ok);
            Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
            Assert.IsFalse(sim.Board.Person(squad[1].PersonId).Alive, "geri çekilmede yerde yatan ölür");
            Assert.IsTrue(sim.Board.Person(squad[0].PersonId).Alive, "ayaktakiler döner");
            Assert.IsTrue(sim.Board.Person(squad[2].PersonId).Alive);
        }

        [Test]
        public void Death_Retreat_BodyAtGate_Dies()
        {
            var sim = NewSim();
            var squad = sim.S.Raid.Params.Squad.ToList();
            var G = sim.OpenRaidRoom();
            Calm(G);
            var body = BySlot(G, 1);
            Down(G, body);
            DragToGate(G, BySlot(G, 0), body);
            Assert.IsTrue(body.AtGate);
            Assert.IsTrue(Baskin.Retreat(G));
            var r = sim.RaidRoomReport(G);
            Assert.AreEqual(0, r.Team[1].Body, "oda geçilmedi: kapıdaki gövde yerde sayılır");
            Assert.IsTrue(sim.AdvanceRaidRoom(r).Ok);
            Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
            Assert.IsFalse(sim.Board.Person(squad[1].PersonId).Alive, "çıkarılmadı: ölür");
            Assert.IsTrue(sim.Board.Person(squad[0].PersonId).Alive, "kapıdaki sürükleyen döner");
        }

        [Test]
        public void Death_TimeoutBeforeEscape_GAH([Values(1, 2, 3)] int room)
        {
            var sim = NewSim();
            var squad = sim.S.Raid.Params.Squad.ToList();
            for (int k = 1; k < room; k++)
            {
                var X = sim.OpenRaidRoom();
                Calm(X); ToGate(X); Assert.IsTrue(Baskin.Pass(X));
                Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(X)).Ok);
            }
            var B = sim.OpenRaidRoom();
            Calm(B);
            Down(B, BySlot(B, 1));
            for (int guard = 0; guard < 20 && B.Phase == "player"; guard++) Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual("lost", B.Phase);
            var r = sim.RaidRoomReport(B);
            Assert.IsTrue(r.Last);
            Assert.IsTrue(sim.AdvanceRaidRoom(r).Ok);
            Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
            Assert.IsFalse(sim.Board.Person(squad[1].PersonId).Alive, "süre doldu: yerde yatan ölür");
            Assert.IsTrue(sim.Board.Person(squad[0].PersonId).Alive);
        }

        [Test]
        public void Death_ExtractedByDrag_Recovers_FullReplaySame()
        {
            var sim = NewSim();
            var squad = sim.S.Raid.Params.Squad.ToList();
            // G: düşeni kapıya sürükle (ekiple), A: yeniden sürükle, H: kaçışta batı kapısına sürükle; G ve A temiz: yürüyerek çıkış
            var G = sim.OpenRaidRoom(); Clean(G);
            var body = BySlot(G, 1);
            Down(G, body);
            DragOut(G, BySlot(G, 0), body);
            Assert.IsTrue(Baskin.Pass(G));
            var rg = sim.RaidRoomReport(G);
            Assert.AreEqual(1, rg.Team[1].Body);
            Assert.IsTrue(sim.AdvanceRaidRoom(rg).Ok);
            // Kayıttan devam: ekiple gelen gövdeyle aynı tahta
            sim.Record.day = sim.S.Day;
            var mid = Replay.Restore(sim.Record);
            Assert.AreEqual(0, mid.ReplayFailures);
            Assert.AreEqual(Baskin.Signature(sim.OpenRaidRoom()), Baskin.Signature(mid.OpenRaidRoom()));
            var A = sim.OpenRaidRoom(); Clean(A);
            body = BySlot(A, 1);
            Assert.IsTrue(Baskin.LyingBody(body), "ekiple gelen gövde A'da yerde");
            DragOut(A, BySlot(A, 0), body);
            Assert.IsTrue(Baskin.Pass(A));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(A)).Ok);
            var H = sim.OpenRaidRoom(); StartEscape(H); Clean(H);
            body = BySlot(H, 1);
            var dragger = Standing(H).First(u => u.Id == H.Carrier);
            DragOut(H, dragger, body);
            Assert.IsTrue(Baskin.Pass(H));
            var rh = sim.RaidRoomReport(H);
            Assert.IsTrue(rh.Last);
            Assert.AreEqual(2, rh.Team[1].Body, "yürüyerek çıkış: ekiple geçen çıkarıldı");
            Assert.IsTrue(sim.AdvanceRaidRoom(rh).Ok);
            Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
            var person = sim.Board.Person(squad[1].PersonId);
            Assert.IsTrue(person.Alive);
            Assert.Greater(person.Down, 0);
            Assert.IsTrue(sim.S.RaidLog.Last().Success);
            // Tam yeniden oynatma aynı durumu verir
            sim.Record.day = sim.S.Day;
            var again = Replay.Restore(sim.Record);
            Assert.AreEqual(0, again.ReplayFailures);
            Assert.AreEqual(State(sim), State(again));
        }

        // ---------- Aşama 6 kapanışı: eksik tablo satırları ve kural kenarları ----------
        /// <summary>Kaçışta süre doldu: tahtada kalan ayaktaki uzman geride kalır ve ölür; kapıya sürüklenen gövde ekiple geçer (temiz
        /// G ve A: yürüyerek çıkış, çıkarıldı ve yaşar).</summary>
        [Test]
        public void Death_TimeoutEscape_LeftDies_GateGoesOn()
        {
            var sim = NewSim();
            var squad = sim.S.Raid.Params.Squad.ToList();
            int n = squad.Count;
            Assert.IsTrue(sim.AdvanceRaidRoom(Rep(1, squad: n)).Ok);
            Assert.IsTrue(sim.AdvanceRaidRoom(Rep(2, squad: n)).Ok);
            var H = sim.OpenRaidRoom();
            Clean(H);
            StartEscape(H);
            var body = BySlot(H, 1); var stay = BySlot(H, 2);
            Down(H, body);
            DragToGate(H, BySlot(H, 0), body);
            for (int guard = 0; guard < 30 && H.Phase == "player"; guard++) Assert.IsTrue(Baskin.EndTurn(H));
            Assert.AreEqual("pass", H.Phase, "kapıdaki geçer");
            Assert.IsTrue(stay.Left && stay.Dead, "tahtada kalan geride kaldı");
            Assert.IsFalse(body.Breath, "gövde turun başında zaten kapıdaydı");
            var r = sim.RaidRoomReport(H);
            Assert.IsTrue(r.Last, "G ve A temiz: yürüyerek çıkış");
            Assert.AreEqual("l", r.Team[2].State);
            Assert.AreEqual(2, r.Team[1].Body, "kapıdaki gövde ekiple geçti, yürüyerek çıkışta çıkarıldı");
            Assert.IsTrue(sim.AdvanceRaidRoom(r).Ok);
            var res = sim.FacilityRaidResult();
            CollectionAssert.AreEqual(new[] { 2 }, res.Left);
            CollectionAssert.AreEqual(new[] { 1 }, res.Extracted);
            Assert.IsTrue(sim.ResolveRaid(res).Ok);
            Assert.IsFalse(sim.Board.Person(squad[2].PersonId).Alive, "geride kalan ölür");
            Assert.IsTrue(sim.Board.Person(squad[1].PersonId).Alive, "kapıdaki gövde kurtarıldı");
            Assert.IsTrue(sim.Board.Person(squad[0].PersonId).Alive);
        }

        /// <summary>Kartsız yol (planlama panosu kapalı, <c>ReturnSquad</c>) değişmez: çıkarılmayan düşen ölmez, sonucu çıkarılanla aynı;
        /// büyük sandık ödülü yoktur.</summary>
        [Test]
        public void Death_CardlessPath_Unchanged()
        {
            var outcomes = new List<string>();
            foreach (int body in new[] { 0, 2 })
                foreach (bool crate in new[] { false, true })
                {
                    var sim = LegacySimulation.NewGame(101, new Balance
                    {
                        shelterEventsEnabled = false, startFon = 100, liberationRegionEvents = false,
                    });
                    Assert.IsNull(sim.Board, "kartsız yol");
                    while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step();
                    var started = sim.StartLiberationRaid("weu_1");
                    Assert.IsTrue(started.Ok, started.Msg);
                    Assert.IsTrue(RaidChain.IsFacility(sim.S.Raid.Params));
                    Assert.AreEqual("loot", sim.S.Raid.Params.MissionKind);
                    var team = sim.S.Teams.First(t => t.Id == sim.S.Raid.Team);
                    int n = sim.S.Raid.Params.Squad.Count;
                    Assert.IsTrue(sim.AdvanceRaidRoom(Rep(1, squad: n)).Ok);
                    Assert.IsTrue(sim.AdvanceRaidRoom(Rep(2, squad: n)).Ok);
                    var last = Rep(3, squad: n); last.Last = true; last.Success = true; last.Cargo = 0; last.Crate = crate;
                    last.Team[1] = new RoomMember { State = "d", Fell = 3, Body = body };
                    Assert.IsTrue(sim.AdvanceRaidRoom(last).Ok);
                    var res = sim.FacilityRaidResult();
                    Assert.AreEqual(body == 2 ? 1 : 0, res.Extracted.Count);
                    double materials = sim.Shelter.Materials;
                    Assert.IsTrue(sim.ResolveRaid(res).Ok);
                    var rw = sim.S.RaidLog.Last().LiberationReward;
                    Assert.IsFalse(rw.BigCrate, "kartsız yolda büyük sandık ödülü yok");
                    outcomes.Add(string.Join(";", rw.Squad.Select(s => $"{s.Type}:{s.HpAfter}:{s.Down}:{s.Left}"))
                        + "|" + string.Join(";", team.Members.Select(m => $"{m.Role}:{m.Down}:{m.Wounds}"))
                        + $"|m{sim.Shelter.Materials - materials:R}|f{sim.S.Fon:R}");
                }
            Assert.IsTrue(outcomes.All(o => o == outcomes[0]), "gövde ve sandık kartsız yolu değiştirmez:\n" + string.Join("\n", outcomes));
            StringAssert.Contains(":0:True:", outcomes[0], "düşen yatar (ölüm yok)");
        }

        /// <summary>Gövde itilmez: EMP'nin itmesi yanındaki ayaktaki uzmanı iter, aynı karedeki gövdeyi itmez.</summary>
        [Test]
        public void Body_NotPushed()
        {
            int tested = 0;
            foreach (bool dead in new[] { false, true })
                foreach (var wing in Wings.Take(6))
                {
                    var (_, _, B) = Entry(wing);
                    var line = FindLine(B, 5);
                    if (line == null) continue;
                    var (at, d) = line.Value;
                    foreach (var v in B.Units.Where(v => v.Side == 'p')) Place(v, new Baskin.Pt(-5, -5));
                    var emp = BySlot(B, 1);
                    Assert.AreEqual("emp", emp.Type);
                    var other = BySlot(B, 2);
                    Place(emp, at);
                    Place(other, Step(at, d, 4));
                    if (dead) Down(B, other);
                    var t = Step(at, d, 3);
                    Assert.IsTrue(Baskin.DoAct(B, emp.Id, t.X, t.Y), wing[0].Id);
                    var to = dead ? Step(at, d, 4) : Step(at, d, 5);
                    Assert.AreEqual((to.X, to.Y), (other.X, other.Y), dead ? "gövde yerinde" : "ayaktaki itilir");
                    Assert.AreEqual(!dead, B.FxList.Any(f => f.K == "push" && f.Unit == other.Id));
                    tested++;
                    break;
                }
            Assert.AreEqual(2, tested);
        }

        /// <summary>İtilen ya da çekilen uzman yükü almaz (EMP itmesi, kanca çekmesi); yalnız kendi hareketiyle varan alır (karar D14).</summary>
        [Test]
        public void Cargo_NotPickedWhenPushed([Values("itme", "cekme")] string how)
        {
            int tested = 0;
            foreach (var wing in Wings.Take(6))
            {
                var (_, _, B) = Entry(wing);
                var line = FindLine(B, 5);
                if (line == null) continue;
                var (at, d) = line.Value;
                foreach (var v in B.Units.Where(v => v.Side == 'p')) Place(v, new Baskin.Pt(-5, -5));
                var friend = BySlot(B, 0);
                Baskin.Pt cargo;
                if (how == "itme")
                {
                    var emp = BySlot(B, 1);
                    Place(emp, at);
                    Place(friend, Step(at, d, 4));
                    cargo = Step(at, d, 5);
                    B.CargoX = cargo.X; B.CargoY = cargo.Y;
                    Assert.IsTrue(Baskin.DoAct(B, emp.Id, Step(at, d, 3).X, Step(at, d, 3).Y));
                }
                else
                {
                    var ana = BySlot(B, 2);
                    Assert.AreEqual("ana", ana.Type);
                    Place(ana, at);
                    Place(friend, Step(at, d, 3));
                    cargo = Step(at, d, 2);
                    B.CargoX = cargo.X; B.CargoY = cargo.Y;
                    Assert.IsTrue(Baskin.DoAct(B, ana.Id, friend.X, friend.Y));
                }
                Assert.AreEqual((cargo.X, cargo.Y), (friend.X, friend.Y), how + ": uzman yükün karesine geldi");
                Assert.AreEqual(0, B.Carrier, how + ": yükü almadı");
                Assert.AreEqual((cargo.X, cargo.Y), (B.CargoX, B.CargoY));
                Assert.IsFalse(B.FxList.Any(f => f.K == "cargo-pick"));
                tested++;
                break;
            }
            Assert.AreEqual(1, tested);
        }

        /// <summary>Sürükleme yolu kapı karesinden geçip kapı olmayan bir kareye varırsa gövde kapıya bırakılmaz: yoldaki kapı olmayan son
        /// kareye (burada sürükleyenin başlangıç karesine) gelir. Gövde kapıya yalnız sürükleyenle birlikte varışta girer.</summary>
        [Test]
        public void Drag_ThroughGateTile_BodyNotLeftOnGate()
        {
            int tested = 0;
            foreach (var wing in Wings.Take(12))
            {
                var (_, _, B) = Entry(wing);
                foreach (var g in Baskin.GateTiles(B))
                {
                    var side = Baskin.Dirs.Select(d => new Baskin.Pt(g.X + d[0], g.Y + d[1]))
                        .Where(q => q.X >= 0 && q.Y >= 0 && q.X < Baskin.N && q.Y < Baskin.N && !Baskin.IsGateTile(B, q.X, q.Y)).ToList();
                    if (side.Count < 2) continue;
                    var s = side[0]; var n = side[1];
                    // Yalnız s → kapı → n açık: yol kapı karesinden geçmek zorunda
                    for (int y = 0; y < Baskin.N; y++)
                        for (int x = 0; x < Baskin.N; x++)
                        {
                            var c = Baskin.CellAt(B, x, y);
                            bool open = x == s.X && y == s.Y || x == g.X && y == g.Y || x == n.X && y == n.Y;
                            c.T = open ? "floor" : "wall"; c.O = null;
                        }
                    foreach (var v in B.Units.Where(v => v.Side == 'p')) Place(v, new Baskin.Pt(-5, -5));
                    var dragger = BySlot(B, 0); var body = BySlot(B, 1);
                    Place(dragger, s); Place(body, s);
                    Down(B, body);
                    CollectionAssert.AreEqual(new[] { (s.X, s.Y), (g.X, g.Y), (n.X, n.Y) },
                        Baskin.WalkPath(B, dragger, 2, n.X, n.Y).Select(q => (q.X, q.Y)).ToArray(), "yol kapıdan geçer");
                    Assert.IsTrue(Baskin.DoAction(B, dragger.Id, Baskin.ActDrag, n.X, n.Y, body.Id), wing[0].Id);
                    Assert.AreEqual((n.X, n.Y), (dragger.X, dragger.Y));
                    Assert.IsFalse(dragger.AtGate);
                    Assert.AreEqual((s.X, s.Y), (body.X, body.Y), "gövde kapının üstünde değil, kapı olmayan son karede");
                    Assert.IsTrue(Baskin.LyingBody(body));
                    var fx = B.FxList.Single(f => f.K == "drag");
                    Assert.AreEqual((0, 0), (fx.Dx, fx.Dy));
                    tested++;
                    break;
                }
            }
            Assert.Greater(tested, 3);
        }

        /// <summary>Taşıyıcı geçilemez bir karede (raf kaymasının duvarı) düşerse yük en yakın açık kareye düşer (çekimsiz) ve kendi
        /// hareketiyle varan uzman alır.</summary>
        [Test]
        public void CarrierFalls_OnImpassable_CargoToNearestOpen()
        {
            var (_, _, H) = LootTarget();
            var c = H.Units.First(u => u.Id == H.Carrier);
            Baskin.CellAt(H, c.X, c.Y).T = "wall";
            var C = H.Clone();
            int rs = H.Rs;
            Down(H, c);
            Assert.AreEqual(0, H.Carrier);
            Assert.IsTrue(H.CargoX >= 0);
            Assert.AreNotEqual((c.X, c.Y), (H.CargoX, H.CargoY), "duvara düşmez");
            var cell = Baskin.CellAt(H, H.CargoX, H.CargoY);
            Assert.IsTrue(cell.T != "wall" && cell.O == null && cell.Door == 0 && cell.Vent == 0);
            Assert.IsNull(Baskin.UnitAt(H, H.CargoX, H.CargoY));
            bool freeNb = Baskin.Dirs.Any(d => FreeAt(C, c.X + d[0], c.Y + d[1]) && Baskin.CellAt(C, c.X + d[0], c.Y + d[1]).Door == 0
                && Baskin.CellAt(C, c.X + d[0], c.Y + d[1]).Vent == 0);
            if (freeNb) Assert.AreEqual(1, Math.Abs(H.CargoX - c.X) + Math.Abs(H.CargoY - c.Y), "boş komşu varken komşuya");
            Assert.IsTrue(H.FxList.Any(f => f.K == "cargo-drop" && f.X == H.CargoX && f.Y == H.CargoY));
            Assert.AreEqual(rs, H.Rs, "RNG çekmez");
            Down(C, C.Units.First(u => u.Id == c.Id));
            Assert.AreEqual((H.CargoX, H.CargoY), (C.CargoX, C.CargoY), "deterministik");
            var cargo = new Baskin.Pt(H.CargoX, H.CargoY);
            var mover = Standing(H).FirstOrDefault(u => Baskin.Reachable(H, u).Any(t => t.X == cargo.X && t.Y == cargo.Y));
            if (mover == null)
            {
                mover = Standing(H).First();
                var nb = FreeNeighbor(H, cargo);
                Assert.IsNotNull(nb);
                Place(mover, nb.Value);
            }
            Assert.IsTrue(Baskin.DoMove(H, mover.Id, cargo.X, cargo.Y));
            Assert.AreEqual(mover.Id, H.Carrier, "yük alınabilir");
        }

        // ---------- Son nefes (aşama 6 kapanışı, geliştirici kararı) ----------
        /// <summary>Makineleri kapatır, birini dron olarak hedefin dik komşusuna koyar ve hedefe ateş niyeti verir (makine evresinde
        /// vurur). Boş komşu yoksa false.</summary>
        static bool Shooter(Baskin.BState B, Baskin.Unit target)
        {
            Calm(B);
            var e = B.Units.FirstOrDefault(u => u.Side == 'e');
            if (e == null) return false;
            for (int d = 0; d < 4; d++)
            {
                int x = target.X - Baskin.Dirs[d][0], y = target.Y - Baskin.Dirs[d][1];
                if (!FreeAt(B, x, y)) continue;
                e.Dead = false; e.Type = "dron"; e.Hp = e.Max = 5; e.Armor = 0; e.Ice = 0; e.Frozen = false;
                Place(e, new Baskin.Pt(x, y));
                e.Intent = new Baskin.Intent { Kind = "shoot", Dir = d };
                return true;
            }
            return false;
        }

        /// <summary>Uzman tek darbede düşer (can 1, plakasız, buzsuz).</summary>
        static void Frail(Baskin.Unit u) { u.Hp = 1; u.Armor = 0; u.Ice = 0; }

        /// <summary>Makine evresinde tahtadaki son uzman düşer, kalanlar kapıda: oda kendiliğinden geçilir ve gövde ekiple gider (Body 1,
        /// sonraki odaya konur). Kural kapalıyken (ölçüm anahtarı) eski kural: yerde kalır. RNG çekilmez.</summary>
        [Test]
        public void LastBreath_MachinePhase_AutoPass_GoesWithTeam([Values(false, true)] bool off)
        {
            int tested = 0;
            foreach (var wing in Wings.Take(9))
            {
                var bal = B0;
                bal.facilityLastBreathDies = off;
                var p = Params(wing, 13, 1, false);
                var reps = new List<RoomReport>();
                var B = Open(p, reps, bal);
                Assert.AreEqual(!off, B.Moves.LastBreath);
                var last = BySlot(B, 2);
                if (!Shooter(B, last)) continue;
                Frail(last);
                ToGate(B, BySlot(B, 0));
                ToGate(B, BySlot(B, 1));
                var twin = B.Clone();
                twin.Moves = MoveRules.From(new Balance { facilityLastBreathDies = !off });
                Assert.IsTrue(Baskin.EndTurn(B), wing[0].Id);
                Assert.IsTrue(Baskin.EndTurn(twin));
                Assert.IsTrue(last.Dead);
                Assert.AreEqual("pass", B.Phase, wing[0].Id + ": kendiliğinden geçiş");
                Assert.AreEqual(!off, last.Breath);
                Assert.AreEqual(twin.Rs, B.Rs, "kural RNG çekmez");
                Assert.AreEqual(!off, B.Log.Any(l => l.Text == Loc.F("{0} son nefeste düştü; ekip onu da götürdü.", Baskin.UnitTypes[last.Type].Name)));
                var r = File(p, reps, B);
                Assert.AreEqual("d", r.Team[2].State);
                Assert.AreEqual(off ? 0 : 1, r.Team[2].Body, off ? "kural kapalı: yerde" : "ekiple");
                var A = Open(p, reps, bal);
                var placed = BySlot(A, 2);
                Assert.AreEqual(!off, placed != null, "ekiple giden gövde sonraki odaya konur");
                if (placed != null) Assert.IsTrue(Baskin.LyingBody(placed) && !placed.Breath);
                tested++;
            }
            Assert.Greater(tested, 4);
        }

        /// <summary>Oyuncuya bir tur verilen gövde (bir oyuncu turunun başında tahtada yattı) eski kurala tabidir: taşınmazsa yerde kalır.
        /// Aynı odada sonraki makine evresinde düşen ve oda o evrede biterse ekiple gider.</summary>
        [Test]
        public void LastBreath_LyingOneTurn_NotCarried_Dies()
        {
            int tested = 0;
            foreach (var wing in Wings.Take(9))
            {
                var p = Params(wing, 13, 1, false);
                var reps = new List<RoomReport>();
                var B = Open(p, reps, B0);
                var early = BySlot(B, 2);
                if (!Shooter(B, early)) continue;
                Frail(early);
                Assert.IsTrue(Baskin.EndTurn(B));
                Assert.AreEqual("player", B.Phase, "ayaktakiler tahtada: oda sürer");
                Assert.IsTrue(early.Dead && !early.Breath);
                ToGate(B, BySlot(B, 0));
                var late = BySlot(B, 1);
                if (!Shooter(B, late)) continue;
                Frail(late);
                Assert.IsTrue(Baskin.EndTurn(B));
                Assert.AreEqual("pass", B.Phase);
                Assert.IsFalse(early.Breath, "bir tur yatan: eski kural");
                Assert.IsTrue(late.Breath, "bu evrede düşen: son nefes");
                var r = File(p, reps, B);
                Assert.AreEqual((0, 1), (r.Team[2].Body, r.Team[1].Body));
                tested++;
            }
            Assert.Greater(tested, 4);
        }

        /// <summary>Tesisin girişinde (kaçış) öbürleri tahliye edildikten sonra son uzman makine evresinde düşerse gövde de çıkarılır
        /// (Body 2, yaşar).</summary>
        [Test]
        public void LastBreath_FinalExit_Extracted()
        {
            int tested = 0;
            foreach (var wing in Wings.Where(w => w[0].Mission == "server").Take(6))
            {
                var (p, reps, B) = Entry(wing);
                B.Escape = true;
                Assert.IsTrue(Baskin.IsFinalExit(B));
                var last = BySlot(B, 2);
                ToGate(B, BySlot(B, 0));
                ToGate(B, BySlot(B, 1));
                Assert.IsTrue(BySlot(B, 0).Out && BySlot(B, 1).Out);
                if (!Shooter(B, last)) continue;
                Frail(last);
                Assert.IsTrue(Baskin.EndTurn(B));
                Assert.AreEqual("won", B.Phase, wing[0].Id);
                Assert.IsTrue(last.Breath);
                var r = File(p, reps, B);
                Assert.AreEqual(2, r.Team[2].Body);
                CollectionAssert.AreEqual(new[] { 2 }, RaidChain.Result(p, reps).Extracted);
                tested++;
            }
            Assert.Greater(tested, 1);
        }

        /// <summary>Geri çekilme sayılan oda (kaçıştan önce süre doldu, kapıda kimse yok) değişmez: son makine evresinde düşen de yerde
        /// kalır ve ölür.</summary>
        [Test]
        public void LastBreath_TimeoutBeforeEscape_Unchanged()
        {
            int tested = 0;
            foreach (var wing in Wings.Take(9))
            {
                var (p, reps, B) = Entry(wing);
                var victim = BySlot(B, 2);
                if (!Shooter(B, victim)) continue;
                Frail(victim);
                B.Turn = B.MaxTurns;
                Assert.IsTrue(Baskin.EndTurn(B));
                Assert.AreEqual("lost", B.Phase, wing[0].Id);
                Assert.IsTrue(B.Retreated);
                Assert.IsTrue(victim.Dead && !victim.Breath);
                Assert.AreEqual(0, File(p, reps, B).Team[2].Body);
                tested++;
            }
            Assert.Greater(tested, 4);
        }

        /// <summary>Sürüm 10: kaçıştan önce süre dolarken kapıda bekleyen varsa oda onlarla geçilir; son makine evresinde düşen son nefes
        /// sayılır ve kapıdakilerle gider (Body 1), kapıya varamayan ayaktaki uzman geri çekilir ("w").</summary>
        [Test]
        public void LastBreath_TimeoutBeforeEscape_SomeoneAtGate_PassesWithTheBody()
        {
            int tested = 0;
            foreach (var wing in Wings.Take(9))
            {
                var (p, reps, B) = Entry(wing);
                var victim = BySlot(B, 2);
                if (!Shooter(B, victim)) continue;
                Frail(victim);
                ToGate(B, BySlot(B, 0));
                B.Turn = B.MaxTurns;
                Assert.IsTrue(Baskin.EndTurn(B));
                Assert.AreEqual("pass", B.Phase, wing[0].Id);
                Assert.IsFalse(B.Retreated);
                Assert.IsTrue(victim.Dead && victim.Breath, "son nefes");
                var r = File(p, reps, B);
                Assert.AreEqual(1, r.Team[2].Body, "kapıdakilerle gider");
                Assert.AreEqual("w", r.Team[1].State, "kapıya varamayan geri çekildi");
                Assert.AreEqual("a", r.Team[0].State);
                tested++;
            }
            Assert.Greater(tested, 4);
        }

        /// <summary>Yeni alan geri almada gelir (Unit kopyası) ve imzaya yalnız varsa girer.</summary>
        [Test]
        public void LastBreath_Undo_Signature()
        {
            var (_, _, B) = Entry(Wings[2]);
            var body = BySlot(B, 1);
            Down(B, body);
            string sig = Baskin.Signature(B);
            body.Breath = true;
            Assert.AreNotEqual(sig, Baskin.Signature(B), "imzada");
            var mover = BySlot(B, 0);
            var to = Baskin.Reachable(B, mover).First(t => !Baskin.IsGateTile(B, t.X, t.Y));
            AssertUndoRestores(B, b => Baskin.DoMove(b, mover.Id, to.X, to.Y), "Breath tam dökümde");
            body = BySlot(B, 1);
            Assert.IsTrue(body.Breath);
            Assert.IsTrue(Baskin.DoMove(B, mover.Id, to.X, to.Y));
            BySlot(B, 1).Breath = false;
            Assert.IsTrue(Baskin.Undo(B));
            Assert.IsTrue(BySlot(B, 1).Breath, "geri alma alanı getirir");
        }

        /// <summary>Uzman bir kapı karesindeyse kapı karesi olmayan ilk boş kareye konur: kapıya girmeden tahtada kalır (Assert).</summary>
        static void OffGate(Baskin.BState B, Baskin.Unit u)
        {
            if (!Baskin.IsGateTile(B, u.X, u.Y)) return;
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    if (FreeAt(B, x, y) && !Baskin.IsGateTile(B, x, y)) { Place(u, new Baskin.Pt(x, y)); return; }
            Assert.Fail("kapı dışında boş kare yok: " + B.Room.Id);
        }

        /// <summary>Kaçış kurulumu (hedef odası, kaçış başladı, makineler kalktı): kapıya giren (taşıyıcı varsa o, yükle geçsin), makine
        /// evresinde düşecek gövde ve kapıya girmeden tahtada kalan ayaktaki(ler). Kapıdaki kapıya girer, kalan kapı karesinden çekilir.</summary>
        static (Baskin.Unit Gate, Baskin.Unit Body, List<Baskin.Unit> Stay) EscapeTrio(Baskin.BState B)
        {
            var team = Standing(B);
            Assert.GreaterOrEqual(team.Count, 3, "kadro üç kişi");
            var gate = team.FirstOrDefault(u => u.Id == B.Carrier) ?? team[0];
            var body = team.First(u => u != gate);
            var stay = team.Where(u => u != gate && u != body).ToList();
            ToGate(B, gate);
            foreach (var u in stay) OffGate(B, u);
            return (gate, body, stay);
        }

        /// <summary>Kaçışta süre doldu (hedef odası, dönüş yolu; A temiz değil): son makine evresinde düşen gövde, kapıda bekleyen biri
        /// olduğu için ekiple gider (Body 1, dönüş odasına konur). Aynı anda tahtada kalan ayaktaki geride kalır (Left, ölür, konmaz). Kural
        /// kapalıyken (ölçüm anahtarı) eski kural: gövde yerde kalır, dönüş odasına gelmez. RNG çekilmez.</summary>
        [Test]
        public void LastBreath_TimeoutEscape_GoesWithTeam_LeftDie([Values(false, true)] bool off)
        {
            int tested = 0;
            foreach (var wing in Wings.Take(12))
            {
                var bal = BM;
                bal.facilityLastBreathDies = off;
                var p = Params(wing, 23, 1, false);
                var reps = new List<RoomReport>();
                var G = Open(p, reps, bal); ClearGate(G); PassRoom(p, reps, G);
                var A = Open(p, reps, bal); ClearGate(A); PassRoom(p, reps, A);
                var H = Open(p, reps, bal);
                Assert.AreEqual(!off, H.Moves.LastBreath);
                StartEscape(H); Calm(H);
                var (gate, body, stay) = EscapeTrio(H);
                if (!Shooter(H, body)) continue;
                Frail(body);
                H.Turn = H.MaxTurns;
                var twin = H.Clone();
                twin.Moves = MoveRules.From(new Balance { facilityLastBreathDies = !off });
                Assert.IsTrue(Baskin.EndTurn(H), wing[2].Id);
                Assert.IsTrue(Baskin.EndTurn(twin));
                Assert.AreEqual("pass", H.Phase, wing[2].Id + ": süre doldu, kapıdaki geçer");
                Assert.IsTrue(gate.AtGate && !gate.Dead);
                Assert.IsTrue(body.Dead && !body.Left, "makine evresinde düştü");
                Assert.AreEqual(!off, body.Breath, off ? "kural kapalı: son nefes yok" : "son nefes: ekiple gider");
                Assert.IsTrue(stay.All(u => u.Left && u.Dead && !u.Breath), "tahtada kalan ayaktaki geride kaldı");
                Assert.AreEqual(twin.Rs, H.Rs, "kural RNG çekmez");
                Assert.AreEqual(off ? 0 : 1, H.Log.Count(l => l.Text == Loc.F("{0} son nefeste düştü; ekip onu da götürdü.", Baskin.UnitTypes[body.Type].Name)));
                var r = File(p, reps, H);
                Assert.AreEqual(("d", off ? 0 : 1), (r.Team[body.Slot].State, r.Team[body.Slot].Body), off ? "kural kapalı: yerde" : "ekiple");
                foreach (var u in stay) Assert.AreEqual(("l", 0), (r.Team[u.Slot].State, r.Team[u.Slot].Body), "geride kalan");
                if (RaidChain.Next(p, reps)?.Room != 2) continue;
                Assert.IsFalse(r.Last, "A temiz değil: dönüş sürer");
                var R = Open(p, reps, bal);
                var placed = BySlot(R, body.Slot);
                Assert.AreEqual(!off, placed != null, wing[1].Id + ": ekiple giden gövde dönüş odasına konur");
                if (placed != null) Assert.IsTrue(Baskin.LyingBody(placed) && !placed.Breath);
                foreach (var u in stay) Assert.IsNull(BySlot(R, u.Slot), "geride kalan konmaz");
                Assert.IsNotNull(BySlot(R, gate.Slot));
                tested++;
            }
            Assert.Greater(tested, 3);
        }

        /// <summary>Kaçışta süre doldu, G ve A temiz (yürüyerek çıkış): son nefes alan gövde çıkarılır (Body 2) ve kişi yaşar; tahtada kalan
        /// ayaktaki geride kalır ve ölür. Kural kapalıyken gövde hedef odasında yatar (Body 0), çıkarılmaz ve kişi ölür.</summary>
        [Test]
        public void LastBreath_TimeoutEscape_WalkOut_PersonFate([Values(false, true)] bool off)
        {
            var sim = Sim(tune: b => { b.liberationRegionEvents = false; b.facilityLastBreathDies = off; });
            var squad = sim.S.Raid.Params.Squad.ToList();
            int n = squad.Count;
            Assert.IsTrue(sim.AdvanceRaidRoom(Rep(1, squad: n)).Ok);
            Assert.IsTrue(sim.AdvanceRaidRoom(Rep(2, squad: n)).Ok);
            var H = sim.OpenRaidRoom();
            Assert.AreEqual(!off, H.Moves.LastBreath, "denge anahtarı odaya gelir");
            Clean(H);
            StartEscape(H);
            var (gate, body, stay) = EscapeTrio(H);
            Assert.IsTrue(Shooter(H, body), "nişancı yeri");
            Frail(body);
            H.Turn = H.MaxTurns;
            Assert.IsTrue(Baskin.EndTurn(H));
            Assert.AreEqual("pass", H.Phase, "kapıdaki geçer");
            Assert.AreEqual(!off, body.Breath);
            Assert.IsTrue(stay.All(u => u.Left && u.Dead));
            var r = sim.RaidRoomReport(H);
            Assert.IsTrue(r.Last, "G ve A temiz: yürüyerek çıkış");
            Assert.AreEqual(off ? 0 : 2, r.Team[body.Slot].Body, off ? "kural kapalı: hedef odasında yatar" : "ekiple geçti, yürüyerek çıkışta çıkarıldı");
            Assert.IsTrue(sim.AdvanceRaidRoom(r).Ok);
            var res = sim.FacilityRaidResult();
            CollectionAssert.AreEquivalent(stay.Select(u => u.Slot), res.Left);
            CollectionAssert.AreEqual(off ? new int[0] : new[] { body.Slot }, res.Extracted);
            Assert.IsTrue(sim.ResolveRaid(res).Ok);
            Assert.AreEqual(!off, sim.Board.Person(squad[body.Slot].PersonId).Alive, off ? "kural kapalı: taşınmayan ölür" : "son nefes: kurtarıldı");
            foreach (var u in stay) Assert.IsFalse(sim.Board.Person(squad[u.Slot].PersonId).Alive, "geride kalan ölür");
            Assert.IsTrue(sim.Board.Person(squad[gate.Slot].PersonId).Alive);
            sim.Record.day = sim.S.Day;
            var again = Replay.Restore(sim.Record);
            Assert.AreEqual(0, again.ReplayFailures);
            Assert.AreEqual(State(sim), State(again));
        }

        /// <summary>Tesisin girişinde (kaçış) öbürleri tahliye edildikten sonra taşıyıcı makine evresinde düşer: yük yerde kalır ve oda
        /// FinishEscape ile kaybedilir ("lost"), ama gövde son nefesle yine çıkarılır (Body 2, Extracted). Kural kapalıyken yerde kalır.</summary>
        [Test]
        public void LastBreath_FinalExit_CargoLost_StillExtracted([Values(false, true)] bool off)
        {
            int tested = 0;
            foreach (var wing in Wings.Where(w => w[0].Mission == "loot").Take(6))
            {
                var bal = B0;
                bal.facilityLastBreathDies = off;
                var p = Params(wing, 13, 1, false);
                var reps = new List<RoomReport>();
                var B = Open(p, reps, bal);
                Calm(B);
                B.Escape = true;
                Assert.IsTrue(Baskin.IsFinalExit(B) && Baskin.IsLoot(B));
                var team = Standing(B);
                var carrier = team.FirstOrDefault(u => u.Type == "tek") ?? team[0];
                B.Carrier = carrier.Id;
                foreach (var u in team.Where(u => u != carrier)) ToGate(B, u);
                Assert.IsTrue(team.Where(u => u != carrier).All(u => u.Out && !u.Dead), "öbürleri tahliye edildi");
                if (!Shooter(B, carrier)) continue;
                Frail(carrier);
                var twin = B.Clone();
                twin.Moves = MoveRules.From(new Balance { facilityLastBreathDies = !off });
                Assert.IsTrue(Baskin.EndTurn(B), wing[0].Id);
                Assert.IsTrue(Baskin.EndTurn(twin));
                Assert.AreEqual("lost", B.Phase, wing[0].Id + ": yük kayıp");
                Assert.AreEqual(Loc.T("Yük odada kaldı: ikmal malzemeleri kaybedildi."), B.OutcomeReason);
                Assert.IsTrue(carrier.Dead && B.Carrier == 0 && B.CargoX >= 0, "yük yerde");
                Assert.AreEqual(!off, carrier.Breath);
                Assert.AreEqual(twin.Rs, B.Rs, "kural RNG çekmez");
                var r = File(p, reps, B);
                Assert.AreEqual(("lost", -2, false, true), (r.End, r.Cargo, r.Success, r.Last));
                Assert.AreEqual(off ? 0 : 2, r.Team[carrier.Slot].Body, off ? "kural kapalı: yerde" : "kayıp odada da çıkarıldı");
                var res = RaidChain.Result(p, reps);
                Assert.IsFalse(res.Success);
                CollectionAssert.IsEmpty(res.Left);
                CollectionAssert.AreEqual(off ? new int[0] : new[] { carrier.Slot }, res.Extracted);
                tested++;
            }
            Assert.Greater(tested, 1);
        }
    }
}
