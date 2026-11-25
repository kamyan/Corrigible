using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 8, parça B (Docs/TESIS-BASKINLARI-TASARIMI.md §5; uygulama tasarımı §5.1–5.6, §5.9–5.10): tahta çerçevesi. Birim
    /// sınıfları ve süzgeç, eylem kimlikleri ve önkoşullar, örme seti (tel, kalas) ve ekip güvencesi, dinamit lokumu, aygıtlar (taret,
    /// yem, vinç) ve hurda, makine aklının ekip ve engel kuralları, oda raporunun aşama 8 parçaları, tahtaya işleyen kazanımlar (Eşik,
    /// Açık hat, Nişan, Makara, Kürk, Sedye). Her eylemin önizlemesi uygulamayla aynı ve geri alma tam durumu getirir. Köpek ve tutsak
    /// parça C'de.
    /// </summary>
    public class FacilityGearTests
    {
        static Balance B0 => Bal(events: false);

        /// <summary>G'nin tahtası (olaysız); <paramref name="keep"/> makine ayakta kalır.</summary>
        static Baskin.BState Entry(int keep = 0, int wing = 0, int seed = 41)
        {
            var p = Params(Wings[wing], seed, 1, false);
            var B = Open(p, new List<RoomReport>(), B0);
            Calm(B, keep);
            return B;
        }

        /// <summary>Kadro sırasındaki uzmanı düz boş hattın (<paramref name="len"/> + 1 kare) başına koyar ve ekipman verir.</summary>
        static (Baskin.Unit U, Baskin.Pt At, int[] D) Arm(Baskin.BState B, string gear, int left, int len = 4, int slot = 0, Func<Baskin.Pt, bool> ok = null)
        {
            var line = FindLine(B, len, ok);
            Assert.IsNotNull(line, "düz hat yok: " + B.Room?.Id);
            var u = BySlot(B, slot);
            Place(u, line.Value.At);
            if (gear != null) Gear(u, gear, left);
            return (u, line.Value.At, line.Value.D);
        }

        static Baskin.Unit Machine(Baskin.BState B, string type, Baskin.Pt at, int hp = 5)
        {
            var e = B.Units.First(u => u.Side == 'e');
            e.Dead = false; e.Out = false; e.Type = type; e.Hp = e.Max = hp; e.Armor = 0; e.Intent = null; e.Idle = false;
            Place(e, at);
            return e;
        }

        static Baskin.Obj Obj(string k, int hp = 3) => new Baskin.Obj { K = k, Hp = hp, Max = hp };

        static bool Has(List<Baskin.Pt> list, Baskin.Pt p) => list.Any(t => t.X == p.X && t.Y == p.Y);

        /// <summary>Önizleme uygulamayla aynı (efektler, yer değişimleri) ve asıl durumu değiştirmez; sonra eylem uygulanır.</summary>
        static void Act(Baskin.BState B, Baskin.Unit u, string kind, Baskin.Pt at)
        {
            string dump = FullDump(B);
            var pv = Baskin.PreviewAction(B, u.Id, kind, at.X, at.Y);
            Assert.IsNotNull(pv, "önizleme: " + kind);
            Assert.AreEqual(dump, FullDump(B), "önizleme durumu değiştirdi: " + kind);
            var before = B.Units.ToDictionary(v => v.Id, v => (v.X, v.Y));
            Assert.IsTrue(Baskin.DoAction(B, u.Id, kind, at.X, at.Y), "eylem: " + kind);
            CollectionAssert.AreEqual(pv.Fx.Select(f => f.K + f.X + f.Y + f.Unit + f.Obj), B.FxList.Select(f => f.K + f.X + f.Y + f.Unit + f.Obj), "önizleme efektleri: " + kind);
            var moved = B.Units.Where(v => before.ContainsKey(v.Id) && before[v.Id] != (v.X, v.Y)).Select(v => v.Id).OrderBy(i => i);
            CollectionAssert.AreEqual(moved, pv.Moves.Select(m => m.Unit).OrderBy(i => i), "önizleme yer değişimleri: " + kind);
        }

        /// <summary>A'dan B'ye L yolu (önce yatay, sonra dikey; <paramref name="vertical"/> ise önce dikey).</summary>
        static List<Baskin.Pt> LPath(Baskin.Pt a, Baskin.Pt b, bool vertical = false)
        {
            var o = new List<Baskin.Pt>();
            int x = a.X, y = a.Y;
            o.Add(new Baskin.Pt(x, y));
            void Walk(bool vert)
            {
                if (vert) while (y != b.Y) { y += Math.Sign(b.Y - y); o.Add(new Baskin.Pt(x, y)); }
                else while (x != b.X) { x += Math.Sign(b.X - x); o.Add(new Baskin.Pt(x, y)); }
            }
            Walk(vertical);
            Walk(!vertical);
            return o;
        }

        /// <summary>Tahtayı koridora çevirir: yol kareleri nesnesiz zemin, öbürleri nesnesiz duvar (<paramref name="keep"/> dokunulmaz);
        /// verilen uzman dışındaki uzmanlar tahtadan kalkar (Out).</summary>
        static void Corridor(Baskin.BState B, List<Baskin.Pt> path, Baskin.Unit u, params Baskin.Pt[] keep)
        {
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    if (keep.Any(k => k.X == x && k.Y == y)) continue;
                    var c = Baskin.CellAt(B, x, y);
                    c.O = null;
                    c.T = path.Any(p => p.X == x && p.Y == y) ? "floor" : "wall";
                }
            foreach (var v in B.Units.Where(v => Baskin.IsSpecialist(v) && v != u)) v.Out = true;
        }

        /// <summary>Yolun ortasında, kendisi ve iki komşusu kapı ve menfez olmayan bir dizin.</summary>
        static int Middle(Baskin.BState B, List<Baskin.Pt> path)
        {
            bool Plain(Baskin.Pt p) => Baskin.CellAt(B, p.X, p.Y).Door == 0 && Baskin.CellAt(B, p.X, p.Y).Vent == 0;
            for (int m = path.Count / 2; m < path.Count - 1; m++) if (Plain(path[m - 1]) && Plain(path[m]) && Plain(path[m + 1])) return m;
            for (int m = path.Count / 2 - 1; m >= 1; m--) if (Plain(path[m - 1]) && Plain(path[m]) && Plain(path[m + 1])) return m;
            Assert.Fail("yolun ortası yok");
            return -1;
        }

        // ---------- API ----------
        [Test]
        public void ActionKinds_StillSix_NewKindsSeparate()
        {
            CollectionAssert.AreEqual(new[] { "pusu", "kosu", "surukle", "firlat", "ortak", "menfez" }, Baskin.ActionKinds);
            CollectionAssert.AreEqual(new[] { "tel", "kalas", "lokum", "taret", "yem", "vinc", "kelepce" }, Baskin.GearActionKinds);
            CollectionAssert.AreEqual(new[] { "havla", "getir", "kokla", "komuta" }, Baskin.UnitActionKinds);
            CollectionAssert.AreEqual(Baskin.ActionKinds.Concat(Baskin.GearActionKinds).Concat(Baskin.UnitActionKinds), Baskin.AllActionKinds);
            foreach (var k in Baskin.GearActionKinds.Concat(Baskin.UnitActionKinds)) Assert.AreEqual(Baskin.ActionCost.Ability, Baskin.CostOf(k), k);
            Assert.AreEqual(FacilityGear.Wire, Baskin.GearOf(Baskin.ActWire));
            Assert.AreEqual(FacilityGear.Wire, Baskin.GearOf(Baskin.ActPlank));
            Assert.AreEqual(FacilityGear.Charge, Baskin.GearOf(Baskin.ActCharge));
            Assert.AreEqual(FacilityGear.Turret, Baskin.GearOf(Baskin.ActTurret));
            Assert.AreEqual(FacilityGear.Decoy, Baskin.GearOf(Baskin.ActDecoy));
            Assert.AreEqual(FacilityGear.Winch, Baskin.GearOf(Baskin.ActWinch));
            Assert.AreEqual(FacilityGear.Clamp, Baskin.GearOf(Baskin.ActClamp));
            Assert.IsNull(Baskin.GearOf(Baskin.ActAmbush));
            Assert.IsNull(Baskin.GearOf(Baskin.ActBark));
            foreach (var t in new[] { "taret", "yem", "vinc", "kopek" }) Assert.AreEqual('p', Baskin.UnitTypes[t].Side, t);
            foreach (var k in new[] { "tel", "kalas", "hurda" }) { Assert.IsTrue(Baskin.ObjNames.ContainsKey(k)); Assert.IsTrue(Baskin.IsBarrier(k)); }
            Assert.IsFalse(Baskin.IsBarrier("civ"));
        }

        [Test]
        public void GearKinds_NeedGearAndCharges()
        {
            var B = Entry();
            var (u, _, _) = Arm(B, null, 0);
            foreach (var k in Baskin.GearActionKinds) Assert.IsFalse(Baskin.CanAction(B, u, k), "ekipmansız " + k);
            Gear(u, FacilityGear.Wire, 2);
            Assert.IsTrue(Baskin.CanAction(B, u, Baskin.ActWire));
            Assert.IsTrue(Baskin.CanAction(B, u, Baskin.ActPlank));
            Assert.IsFalse(Baskin.CanAction(B, u, Baskin.ActCharge));
            Assert.IsFalse(Baskin.CanAction(B, u, Baskin.ActTurret));
            Gear(u, FacilityGear.Wire, 1);
            Assert.IsTrue(Baskin.CanAction(B, u, Baskin.ActWire));
            Assert.IsFalse(Baskin.CanAction(B, u, Baskin.ActPlank), "kalas 2 hak ister");
            Gear(u, FacilityGear.Wire, 0);
            Assert.IsFalse(Baskin.CanAction(B, u, Baskin.ActWire));
            foreach (var (gear, act) in new[] { (FacilityGear.Charge, Baskin.ActCharge), (FacilityGear.Turret, Baskin.ActTurret), (FacilityGear.Decoy, Baskin.ActDecoy), (FacilityGear.Winch, Baskin.ActWinch) })
            {
                Gear(u, gear, 1);
                foreach (var k in Baskin.GearActionKinds) Assert.AreEqual(k == act, Baskin.CanAction(B, u, k), gear + " " + k);
            }
            // Uzman birim eylemi veremez; ekipman eylemi yeteneğin yerinedir
            foreach (var k in Baskin.UnitActionKinds) Assert.IsFalse(Baskin.CanAction(B, u, k), k);
            Gear(u, FacilityGear.Turret, 1);
            u.Acted = true;
            Assert.IsFalse(Baskin.CanAction(B, u, Baskin.ActTurret));
        }

        [Test]
        public void Gadget_CannotMoveOrAct()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, null, 0);
            var g = AddGadget(B, "taret", Step(at, d, 1), d);
            Assert.IsFalse(Baskin.IsSpecialist(g));
            Assert.IsTrue(Baskin.IsGadget(g));
            Assert.IsFalse(Baskin.CanMove(B, g));
            Assert.IsFalse(Baskin.CanAct(B, g));
            foreach (var k in Baskin.AllActionKinds) Assert.IsFalse(Baskin.CanAction(B, g, k), k);
            Assert.IsFalse(Baskin.DoMove(B, g.Id, at.X, at.Y));
            // Aygıt ekip tarafıdır: uzman içinden geçer
            Assert.IsTrue(Baskin.Reachable(B, u).Any(t => t.X == Step(at, d, 2).X && t.Y == Step(at, d, 2).Y));
        }

        // ---------- Örme seti ----------
        [Test]
        public void Wire_Builds_Undo()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, FacilityGear.Wire, 2);
            var to = Step(at, d, 1);
            Assert.IsTrue(Has(Baskin.ActionTargets(B, u, Baskin.ActWire), to));
            AssertUndoRestores(B, b => Baskin.DoAction(b, u.Id, Baskin.ActWire, to.X, to.Y), "tel");
            Act(B, u, Baskin.ActWire, to);
            u = Fresh(B, u); // geri alma Units'i kopyalarla değiştirdi: eylemin sonucu güncel nesnede
            var o = Baskin.CellAt(B, to.X, to.Y).O;
            Assert.AreEqual(("tel", 3, 3), (o.K, o.Hp, o.Max));
            Assert.AreEqual(1, u.GearLeft);
            Assert.IsTrue(u.Acted && u.Moved);
            Assert.IsTrue(B.FxList.Any(f => f.K == "build" && f.Obj == "tel"));
            StringAssert.Contains("tel ördü", B.Log.Last().Text);
            Assert.IsFalse(Baskin.Reachable(B, u).Any(t => t.X == to.X && t.Y == to.Y), "engel yürüyüşü keser");
        }

        [Test]
        public void Plank_CostsTwo_Undo()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, FacilityGear.Wire, 2);
            var to = Step(at, d, 1);
            AssertUndoRestores(B, b => Baskin.DoAction(b, u.Id, Baskin.ActPlank, to.X, to.Y), "kalas");
            Act(B, u, Baskin.ActPlank, to);
            u = Fresh(B, u); // geri alma Units'i kopyalarla değiştirdi: eylemin sonucu güncel nesnede
            Assert.AreEqual(("kalas", 3), (Baskin.CellAt(B, to.X, to.Y).O.K, Baskin.CellAt(B, to.X, to.Y).O.Hp));
            Assert.AreEqual(0, u.GearLeft);
        }

        [Test]
        public void Wire_OnFreeFloorOnly()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, FacilityGear.Wire, 2);
            var to = Step(at, d, 1);
            var c = Baskin.CellAt(B, to.X, to.Y);
            bool Target() => Has(Baskin.ActionTargets(B, u, Baskin.ActWire), to);
            Assert.IsTrue(Target());
            c.O = Obj("civ", 1); Assert.IsFalse(Target(), "nesne"); c.O = null;
            c.T = "cable"; Assert.IsFalse(Target(), "kablo"); c.T = "floor";
            c.Door = 2; Assert.IsFalse(Target(), "kapı"); c.Door = 0;
            c.Vent = 1; Assert.IsFalse(Target(), "menfez"); c.Vent = 0;
            var e = Machine(B, "dron", to); Assert.IsFalse(Target(), "birim"); e.Dead = true;
            B.CargoX = to.X; B.CargoY = to.Y; Assert.IsFalse(Target(), "yük"); B.CargoX = B.CargoY = -1;
            var v = BySlot(B, 1); Place(v, to); Down(B, v); Assert.IsFalse(Target(), "gövde");
            v.X = v.Y = -1;
            Assert.IsTrue(Target());
        }

        /// <summary>Engel ekibi batı girişinden ve ileri geçitten koparamaz (§11.6); kaçışta ileri geçit bağı aranmaz.</summary>
        [Test]
        public void Barrier_CannotCutTeamFromWestOrForward()
        {
            var B = Entry();
            var room = B.Room;
            var path = LPath(room.West[0], room.Forward[0]);
            var u = BySlot(B, 0);
            Corridor(B, path, u);
            int m = Middle(B, path);
            Place(u, path[m]);
            Gear(u, FacilityGear.Wire, 2);
            Assert.IsEmpty(Baskin.ActionTargets(B, u, Baskin.ActWire), "iki komşu da bir bağı koparır");
            Assert.IsEmpty(Baskin.ActionTargets(B, u, Baskin.ActPlank));
            B.Escape = true;
            var t = Baskin.ActionTargets(B, u, Baskin.ActWire);
            Assert.AreEqual(1, t.Count, "kaçışta yalnız batı bağı");
            Assert.IsTrue(Has(t, path[m + 1]));
        }

        /// <summary>Hedef odada engel ekibi kalan hedeften koparamaz.</summary>
        [Test]
        public void Barrier_CannotCutTeamFromTarget()
        {
            var wing = Wings.First(w => w[0].Mission != "loot");
            var p = Params(wing, 43, 1, false);
            var H = Open(p, new List<RoomReport> { Rep(1), Rep(2) }, B0);
            Calm(H);
            var tg = Baskin.TargetsLeft(H)[0];
            var tp = new Baskin.Pt(tg.X, tg.Y);
            List<Baskin.Pt> path = null;
            foreach (var d in Baskin.Dirs)
                foreach (bool vert in new[] { false, true })
                {
                    var n = Step(tp, d, 1);
                    if (path != null || n.X < 0 || n.Y < 0 || n.X >= Baskin.N || n.Y >= Baskin.N) continue;
                    var cand = LPath(H.Room.West[0], n, vert);
                    if (cand.Count >= 4 && !cand.Any(c => c.X == tp.X && c.Y == tp.Y)) path = cand;
                }
            Assert.IsNotNull(path);
            var u = BySlot(H, 0);
            Corridor(H, path, u, tp);
            int m = Middle(H, path);
            Place(u, path[m]);
            Gear(u, FacilityGear.Wire, 2);
            Assert.IsEmpty(Baskin.ActionTargets(H, u, Baskin.ActWire));
        }

        /// <summary>Engel makineleri koparabilir: çıkmaz cepteki makinenin önü örülür.</summary>
        [Test]
        public void Barrier_MayCutMachines()
        {
            var B = Entry();
            var room = B.Room;
            var path = LPath(room.West[0], room.Forward[0]);
            var u = BySlot(B, 0);
            Corridor(B, path, u);
            int m = -1;
            int[] pd = null;
            for (int i = 1; i < path.Count - 1 && pd == null; i++)
                foreach (var d in Baskin.Dirs)
                {
                    var a = Step(path[i], d, 1); var b2 = Step(path[i], d, 2);
                    if (b2.X < 0 || b2.Y < 0 || b2.X >= Baskin.N || b2.Y >= Baskin.N || path.Any(p0 => p0.X == a.X && p0.Y == a.Y || p0.X == b2.X && p0.Y == b2.Y)) continue;
                    if (Baskin.CellAt(B, path[i].X, path[i].Y).Door != 0 || Baskin.CellAt(B, a.X, a.Y).Door != 0 || Baskin.CellAt(B, a.X, a.Y).Vent != 0) continue;
                    // Cebin komşusu yoldaki başka bir kare olmasın
                    if (Baskin.Dirs.Any(d2 => path.Any(p0 => (p0.X != path[i].X || p0.Y != path[i].Y) && p0.X == a.X + d2[0] && p0.Y == a.Y + d2[1]))) continue;
                    if (Baskin.Dirs.Any(d2 => path.Any(p0 => p0.X == b2.X + d2[0] && p0.Y == b2.Y + d2[1]))) continue;
                    m = i; pd = d; break;
                }
            Assert.IsNotNull(pd);
            var pocket = Step(path[m], pd, 1);
            var end = Step(path[m], pd, 2);
            Baskin.CellAt(B, pocket.X, pocket.Y).T = "floor";
            Baskin.CellAt(B, end.X, end.Y).T = "floor";
            Machine(B, "bakim", end);
            Place(u, path[m]);
            Gear(u, FacilityGear.Wire, 2);
            CollectionAssert.AreEqual(new[] { (pocket.X, pocket.Y) }, Baskin.ActionTargets(B, u, Baskin.ActWire).Select(t => (t.X, t.Y)));
        }

        /// <summary>Tel makine ateşini ve kilidi, pusuyu geçirir; kalas keser (K5).</summary>
        [Test]
        public void Wire_ShotsPass_PlankBlocks()
        {
            var B = Entry();
            var (v, at, d) = Arm(B, null, 0, len: 4);
            var e = Machine(B, "dron", at);
            var mid = Step(at, d, 1);
            Place(v, Step(at, d, 3));
            e.Intent = new Baskin.Intent { Kind = "shoot", Dir = DirOf(d) };
            e.Order = 1;
            Baskin.CellAt(B, mid.X, mid.Y).O = Obj("tel");
            var iv = Baskin.Intents(B).Single();
            Assert.AreEqual((v.X, v.Y), (iv.TargetX, iv.TargetY), "tel atışı geçirir");
            Baskin.CellAt(B, mid.X, mid.Y).O = Obj("kalas");
            iv = Baskin.Intents(B).Single();
            Assert.AreEqual((mid.X, mid.Y), (iv.TargetX, iv.TargetY), "kalas atışı keser");
            // Pusu hattı tel'den geçer, kalasta biter
            e.Dead = true;
            Place(v, at);
            v.AmbushDir = DirOf(d); v.AmbushX = at.X; v.AmbushY = at.Y;
            Assert.AreEqual(0, Baskin.AmbushLine(B, v).Count);
            Baskin.CellAt(B, mid.X, mid.Y).O = Obj("tel");
            Assert.AreEqual(4, Baskin.AmbushLine(B, v).Count);
        }

        [Test]
        public void Barrier_TeamBreaks_SabotageTwo()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, null, 0);
            Assert.AreEqual("tek", u.Type);
            var to = Step(at, d, 1);
            Baskin.CellAt(B, to.X, to.Y).O = Obj("tel");
            Assert.IsTrue(Baskin.DoAct(B, u.Id, to.X, to.Y));
            Assert.AreEqual(1, Baskin.CellAt(B, to.X, to.Y).O.Hp);
        }

        [Test]
        public void Barrier_PushInto_OneDamageEach()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, null, 0);
            var e = Machine(B, "bakim", Step(at, d, 1));
            var w = Step(at, d, 2);
            Baskin.CellAt(B, w.X, w.Y).O = Obj("tel");
            Assert.IsTrue(Baskin.DoAct(B, u.Id, e.X, e.Y));
            Assert.AreEqual(3, e.Hp, "sabotaj 1 + engele çarpma 1");
            Assert.AreEqual(2, Baskin.CellAt(B, w.X, w.Y).O.Hp);
            Assert.AreEqual((Step(at, d, 1).X, Step(at, d, 1).Y), (e.X, e.Y), "engel itmeyi durdurur");
            // Düşüren çarpma telsize "engele çarptı" diye geçer
            Assert.IsTrue(Baskin.Undo(B));
            e = B.Units.First(x => x.Id == e.Id);
            e.Hp = 2;
            Assert.IsTrue(Baskin.DoAct(B, u.Id, e.X, e.Y));
            Assert.IsTrue(e.Dead || B.Units.First(x => x.Id == e.Id).Dead);
            Assert.IsTrue(B.Log.Any(l => l.Text.Contains("engele çarptı")));
        }

        /// <summary>Engel oda raporuna yazılır, dönüş tahtası onu geri kurar (baskın içi kalıcılık).</summary>
        [Test]
        public void Barrier_InReport_RebuiltOnReturn()
        {
            var p = Params(Wings[0], 41, 1, false);
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            Calm(B, 1);
            var e = B.Units.First(x => x.Side == 'e' && !x.Dead);
            var (u, at, d) = Arm(B, FacilityGear.Wire, 2, ok: s => Math.Abs(s.X - e.X) + Math.Abs(s.Y - e.Y) > 3);
            var to = Step(at, d, 1);
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActWire, to.X, to.Y));
            ClearGate(B);
            var r = PassRoom(p, reps, B);
            Assert.IsTrue(r.Objects.Any(o => o.K == "tel" && o.X == to.X && o.Y == to.Y && o.Hp == 3));
            Assert.AreEqual(1, r.Team[0].Gear);
            if (RaidChain.IsClean(r)) Assert.Inconclusive("G temiz kaldı");
            reps.Add(Rep(2));
            reps.Add(Rep(3));
            var G = Open(p, reps, B0);
            Assert.IsTrue(G.Escape && G.Room.Index == 1);
            var back = Baskin.CellAt(G, to.X, to.Y).O;
            Assert.IsNotNull(back);
            Assert.AreEqual(("tel", 3), (back.K, back.Hp));
        }

        // ---------- Dinamit lokumu ----------
        static (Baskin.Unit U, Baskin.Pt Wall) ByWall(Baskin.BState B, int slot = 0)
        {
            foreach (var t in Enumerable.Range(0, 64).Select(i => new Baskin.Pt(i % 8, i / 8)))
            {
                if (!FreeAt(B, t.X, t.Y) || Baskin.CellAt(B, t.X, t.Y).Door != 0 || Baskin.CellAt(B, t.X, t.Y).Vent != 0) continue;
                foreach (var d in Baskin.Dirs)
                {
                    int x = t.X + d[0], y = t.Y + d[1];
                    if (x < 0 || y < 0 || x >= 8 || y >= 8) continue;
                    var c = Baskin.CellAt(B, x, y);
                    if (c.T != "wall" || c.Shutter || c.Crate) continue;
                    var u = BySlot(B, slot);
                    Place(u, t);
                    return (u, new Baskin.Pt(x, y));
                }
            }
            Assert.Fail("duvar yanı yok");
            return default;
        }

        [Test]
        public void Charge_WallToFloor_Rubble_Undo()
        {
            var B = Entry();
            var (u, w) = ByWall(B);
            Gear(u, FacilityGear.Charge, 2);
            Assert.IsTrue(Has(Baskin.ActionTargets(B, u, Baskin.ActCharge), w));
            AssertUndoRestores(B, b => Baskin.DoAction(b, u.Id, Baskin.ActCharge, w.X, w.Y), "lokum");
            Act(B, u, Baskin.ActCharge, w);
            u = Fresh(B, u); // geri alma Units'i kopyalarla değiştirdi: eylemin sonucu güncel nesnede
            Assert.AreEqual("floor", Baskin.CellAt(B, w.X, w.Y).T);
            Assert.IsTrue(B.Rubble.Any(r => r.X == w.X && r.Y == w.Y));
            Assert.AreEqual(1, u.GearLeft);
            CollectionAssert.AreEqual(new[] { "charge", "breach" }, B.FxList.Take(2).Select(f => f.K));
            Assert.IsTrue(B.Log.Any(l => l.Text.Contains("lokumla duvarı yıktı")));
        }

        [Test]
        public void Charge_NotShutterCrateOrRobot()
        {
            var B = Entry();
            var (u, w) = ByWall(B);
            Gear(u, FacilityGear.Charge, 2);
            var c = Baskin.CellAt(B, w.X, w.Y);
            c.Shutter = true; Assert.IsFalse(Has(Baskin.ActionTargets(B, u, Baskin.ActCharge), w), "kepenk"); c.Shutter = false;
            c.Crate = true; Assert.IsFalse(Has(Baskin.ActionTargets(B, u, Baskin.ActCharge), w), "sandık"); c.Crate = false;
            // Robota ve nesneye atılmaz (yalnız duvar)
            var free = FreeNeighbor(B, new Baskin.Pt(u.X, u.Y), gateToo: true);
            if (free is Baskin.Pt f)
            {
                Machine(B, "dron", f);
                Assert.IsFalse(Has(Baskin.ActionTargets(B, u, Baskin.ActCharge), f));
            }
            Gear(u, FacilityGear.Charge, 0);
            Assert.IsEmpty(Baskin.ActionTargets(B, u, Baskin.ActCharge));
        }

        /// <summary>Madenci'nin dinamiti ortak duvar kuralına taşındı: telsiz satırı, efekt sırası ve hak aynı.</summary>
        [Test]
        public void Charge_AbilityLogUnchanged()
        {
            var p = new RaidParams
            {
                Seed = 3, Traits = new List<string>(), DynamiteCharges = 1,
                Squad = new List<SquadSlot> { new SquadSlot { Type = "maden", Hp = 3 }, new SquadSlot { Type = "emp", Hp = 3 }, new SquadSlot { Type = "ana", Hp = 2 } },
            };
            var B = Baskin.Create(p);
            var mz = B.Units.First(x => x.Type == "maden");
            Baskin.Pt? wall = null;
            foreach (var t in Enumerable.Range(0, 64).Select(i => new Baskin.Pt(i % 8, i / 8)))
            {
                if (wall != null || !FreeAt(B, t.X, t.Y)) continue;
                foreach (var d in Baskin.Dirs)
                {
                    int x = t.X + d[0], y = t.Y + d[1];
                    if (wall == null && x >= 0 && y >= 0 && x < 8 && y < 8 && Baskin.CellAt(B, x, y).T == "wall") { Place(mz, t); wall = new Baskin.Pt(x, y); }
                }
            }
            Assert.IsNotNull(wall);
            Assert.IsTrue(Baskin.DoAct(B, mz.Id, wall.Value.X, wall.Value.Y));
            Assert.AreEqual(0, mz.Charges);
            CollectionAssert.AreEqual(new[] { "dynamite", "breach" }, B.FxList.Take(2).Select(f => f.K));
            Assert.IsTrue(B.Log.Any(l => l.Text == "Madenci duvarı yıktı."));
            Assert.AreEqual("floor", Baskin.CellAt(B, wall.Value.X, wall.Value.Y).T);
        }

        // ---------- Aygıtlar ----------
        [Test]
        public void Gadgets_Build_Undo_Preview()
        {
            foreach (var (gear, act) in new[] { (FacilityGear.Turret, Baskin.ActTurret), (FacilityGear.Decoy, Baskin.ActDecoy), (FacilityGear.Winch, Baskin.ActWinch) })
            {
                var B = Entry();
                var (u, at, d) = Arm(B, gear, 1);
                var to = Step(at, d, 1);
                Assert.IsTrue(Has(Baskin.ActionTargets(B, u, act), to), act);
                AssertUndoRestores(B, b => Baskin.DoAction(b, u.Id, act, to.X, to.Y), act);
                Act(B, u, act, to);
                u = Fresh(B, u); // geri alma Units'i kopyalarla değiştirdi: eylemin sonucu güncel nesnede
                var g = B.Units.Last();
                Assert.IsTrue(Baskin.IsGadget(g), act);
                Assert.AreEqual((act, to.X, to.Y, DirOf(d), 3, 2, 2, u.Slot), (g.Type, g.X, g.Y, g.Dir, g.Turns, g.Hp, g.Max, g.Owner), act);
                Assert.AreEqual(-1, g.Slot);
                Assert.AreEqual(0, u.GearLeft);
                Assert.IsTrue(B.FxList.Any(f => f.K == "gadget" && f.Unit == g.Id), act);
            }
        }

        [Test]
        public void Gadget_NotOnGateVentBodyCargo()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, FacilityGear.Turret, 1);
            var to = Step(at, d, 1);
            var c = Baskin.CellAt(B, to.X, to.Y);
            bool Target() => Has(Baskin.ActionTargets(B, u, Baskin.ActTurret), to);
            Assert.IsTrue(Target());
            c.Door = 2; Assert.IsFalse(Target(), "geçit"); c.Door = 0;
            c.Vent = 1; Assert.IsFalse(Target(), "menfez"); c.Vent = 0;
            B.CargoX = to.X; B.CargoY = to.Y; Assert.IsFalse(Target(), "yük"); B.CargoX = B.CargoY = -1;
            var v = BySlot(B, 1); Place(v, to); Down(B, v); Assert.IsFalse(Target(), "gövde");
        }

        /// <summary>Aygıt fitil kuralına uyar (§11.6): kurulduğu turun niyetleri önce uygulanır, aygıt aynı evrenin sonunda çalışır.</summary>
        [Test]
        public void Gadget_Fuse_IntentsFirst()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, FacilityGear.Turret, 1, len: 5);
            var e = Machine(B, "dron", Step(at, d, 3));
            e.Intent = new Baskin.Intent { Kind = "shoot", Dir = DirOf(new[] { -d[0], -d[1] }) };
            e.Order = 1;
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActTurret, Step(at, d, 1).X, Step(at, d, 1).Y));
            var g = B.Units.Last();
            Assert.IsTrue(Baskin.EndTurn(B));
            int attack = B.FxList.FindIndex(f => f.K == "attack" && f.Unit == e.Id);
            int hit = B.FxList.FindIndex(f => f.K == "turret-hit" && f.Unit == e.Id);
            Assert.GreaterOrEqual(attack, 0);
            Assert.Greater(hit, attack, "taret niyetlerden sonra ateşler");
            Assert.AreEqual(1, g.Hp, "dronun atışı önündeki tareti vurdu");
            Assert.AreEqual(4, e.Hp);
            Assert.AreEqual(2, g.Turns);
        }

        [Test]
        public void Turret_HitsFirstMachineInRange_StopsAtFriend_PassesWire()
        {
            foreach (var (k, friend, wire, hits) in new[] { (3, false, false, true), (5, false, false, false), (3, true, false, false), (3, false, true, true) })
            {
                var B = Entry();
                var (u, at, d) = Arm(B, null, 0, len: 6);
                var tp = Step(at, d, 1);
                AddGadget(B, "taret", tp, d);
                var e = Machine(B, "bakim", Step(tp, d, k));
                if (friend) Place(BySlot(B, 1), Step(tp, d, 1));
                if (wire) Baskin.CellAt(B, Step(tp, d, 1).X, Step(tp, d, 1).Y).O = Obj("tel");
                Assert.IsTrue(Baskin.EndTurn(B));
                Assert.AreEqual(hits, B.FxList.Any(f => f.K == "turret-hit" && f.Unit == e.Id), $"k{k} dost {friend} tel {wire}");
                Assert.AreEqual(hits ? 4 : 5, e.Hp);
            }
        }

        [Test]
        public void Decoy_WalksTwo_StopsAtBlockedOrGate()
        {
            foreach (int block in new[] { 0, 3, 2 })
            {
                var B = Entry();
                var (u, at, d) = Arm(B, null, 0, len: 4);
                var g = AddGadget(B, "yem", Step(at, d, 1), d);
                if (block == 3) Place(BySlot(B, 1), Step(at, d, 3));
                if (block == 2) Baskin.CellAt(B, Step(at, d, 2).X, Step(at, d, 2).Y).Door = 2;
                Assert.IsTrue(Baskin.EndTurn(B));
                var want = block == 0 ? Step(at, d, 3) : block == 3 ? Step(at, d, 2) : Step(at, d, 1);
                Assert.AreEqual((want.X, want.Y), (g.X, g.Y), "engel " + block);
                Assert.AreEqual(2, g.Turns);
            }
        }

        /// <summary>Yem ekip gibi hedef alınır (K9); taret, vinç ve öbür ekip birimleri 3; kilit yalnız uzmana.</summary>
        [Test]
        public void Machine_ScoresDecoyLikeTeam_OtherGadgetsLow_LockOnlySpecialist()
        {
            var B = Entry();
            var (v, at, d) = Arm(B, null, 0, len: 4);
            var e = Machine(B, "dron", at, hp: 2);
            v.X = v.Y = -1;
            v.Out = true;
            var g = AddGadget(B, "yem", Step(at, d, 2), new[] { -d[0], -d[1] });
            int dir = DirOf(d);
            Assert.IsTrue(Baskin.TestTeamLike(B, e, g));
            Assert.AreEqual(10, Baskin.TestScore(B, e, "shoot", dir));
            g.Type = "taret";
            Assert.IsFalse(Baskin.TestTeamLike(B, e, g));
            Assert.AreEqual(3, Baskin.TestScore(B, e, "shoot", dir));
            e.Type = "kilit";
            Assert.AreEqual(0, Baskin.TestScore(B, e, "lock", dir), "kilit aygıta yönelmez");
            g.Dead = true;
            var s = BySlot(B, 1);
            Place(s, Step(at, d, 2));
            Assert.GreaterOrEqual(Baskin.TestScore(B, e, "lock", dir), 9);
            // Patlama: yem 9, öbür aygıt 2
            s.X = s.Y = -1; s.Out = true;
            g.Dead = false; g.Type = "yem"; Place(g, Step(at, d, 1));
            e.Type = "bomba";
            double withDecoy = Baskin.TestScore(B, e, "blast", 0);
            g.Type = "vinc";
            Assert.AreEqual(7, withDecoy - Baskin.TestScore(B, e, "blast", 0), "patlamada yem 9, öbür aygıt 2");
        }

        /// <summary>Ekibe ulaşamayan makine engeli kırar (11, sivilin 9'undan önce); ulaşabilen engelle uğraşmaz (K8).</summary>
        [Test]
        public void Machine_StuckTargetsBarrier()
        {
            var B = Entry();
            bool Clear(Baskin.Pt s) => Baskin.Dirs.All(dd =>
            {
                int x = s.X + dd[0], y = s.Y + dd[1];
                return x >= 0 && y >= 0 && x < 8 && y < 8 && Baskin.UnitAt(B, x, y) == null && Baskin.CellAt(B, x, y).Door == 0;
            });
            var line = FindLine(B, 2, Clear);
            Assert.IsNotNull(line);
            var (at, d) = (line.Value.At, line.Value.D);
            var e = Machine(B, "dron", at);
            foreach (var dd in Baskin.Dirs)
            {
                var n = Step(at, dd, 1);
                if (dd == d) Baskin.CellAt(B, n.X, n.Y).O = Obj("kalas");
                else { Baskin.CellAt(B, n.X, n.Y).T = "wall"; Baskin.CellAt(B, n.X, n.Y).O = null; }
            }
            Assert.AreEqual(11, Baskin.TestScore(B, e, "shoot", DirOf(d), stuck: true));
            Assert.AreEqual(0, Baskin.TestScore(B, e, "shoot", DirOf(d), stuck: false));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsNotNull(e.Intent);
            Assert.AreEqual(("shoot", DirOf(d)), (e.Intent.Kind, e.Intent.Dir), "kapalı makine kalasa yönelir");
        }

        [Test]
        public void Winch_PullsUnit_Body_Cargo()
        {
            // Ayaktaki birim (uzman) bir kare
            var B = Entry();
            var (u, at, d) = Arm(B, null, 0, len: 5);
            AddGadget(B, "vinc", Step(at, d, 1), d);
            var v = BySlot(B, 1);
            Place(v, Step(at, d, 4));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((Step(at, d, 3).X, Step(at, d, 3).Y), (v.X, v.Y));
            Assert.IsTrue(B.FxList.Any(f => f.K == "winch-pull" && f.Unit == v.Id));
            // Gövde ve yük birlikte
            B = Entry();
            (u, at, d) = Arm(B, null, 0, len: 5);
            AddGadget(B, "vinc", Step(at, d, 1), d);
            v = BySlot(B, 1);
            Place(v, Step(at, d, 3));
            Down(B, v);
            B.CargoX = v.X; B.CargoY = v.Y;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((Step(at, d, 2).X, Step(at, d, 2).Y), (v.X, v.Y), "gövde çekildi (D16 istisnası)");
            Assert.AreEqual((v.X, v.Y), (B.CargoX, B.CargoY), "yük çekildi");
        }

        [Test]
        public void Winch_NoPullOntoGate_ForBodyCargo()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, null, 0, len: 5);
            AddGadget(B, "vinc", Step(at, d, 1), d);
            var v = BySlot(B, 1);
            Place(v, Step(at, d, 3));
            Down(B, v);
            Baskin.CellAt(B, Step(at, d, 2).X, Step(at, d, 2).Y).Door = 2;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((Step(at, d, 3).X, Step(at, d, 3).Y), (v.X, v.Y));
        }

        [Test]
        public void Pulley_PullsTwo()
        {
            var B = Entry();
            B.Moves = MoveRules.From(B0, new RaidParams { Perks = new List<string> { FacilityGear.Pulley } });
            Assert.AreEqual(2, B.Moves.WinchPull);
            var (u, at, d) = Arm(B, null, 0, len: 5);
            AddGadget(B, "vinc", Step(at, d, 1), d);
            var v = BySlot(B, 1);
            Place(v, Step(at, d, 4));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((Step(at, d, 2).X, Step(at, d, 2).Y), (v.X, v.Y));
        }

        [Test]
        public void Gadget_ExpiresToScrap()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, null, 0);
            var tp = Step(at, d, 1);
            var g = AddGadget(B, "taret", tp, d, turns: 1);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(g.Dead);
            Assert.AreEqual(("hurda", 1), (Baskin.CellAt(B, tp.X, tp.Y).O.K, Baskin.CellAt(B, tp.X, tp.Y).O.Hp));
            Assert.IsTrue(B.FxList.Any(f => f.K == "scrap" && f.Unit == g.Id));
        }

        [Test]
        public void Gadget_BrokenToScrap()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, null, 0);
            var tp = Step(at, d, 1);
            var g = AddGadget(B, "vinc", tp, d);
            Baskin.TestHurt(B, g, 5);
            Assert.IsTrue(g.Dead);
            Assert.AreEqual("hurda", Baskin.CellAt(B, tp.X, tp.Y).O?.K);
            Assert.IsTrue(B.FxList.Any(f => f.K == "device-break" && f.Unit == g.Id));
            Assert.IsFalse(B.FxList.Any(f => f.K == "die" && f.Unit == g.Id));
        }

        [Test]
        public void Scrap_NotOnGateOrIfCutsTeam()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, null, 0);
            var tp = Step(at, d, 1);
            var g = AddGadget(B, "taret", tp, d, turns: 1);
            Baskin.CellAt(B, tp.X, tp.Y).Door = 2;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(g.Dead);
            Assert.IsNull(Baskin.CellAt(B, tp.X, tp.Y).O, "geçitte hurda kalmaz");
            // Ekip güvencesi: koridorda uzmanla ileri geçit arasındaki aygıtın hurdası konmaz
            B = Entry();
            var path = LPath(B.Room.West[0], B.Room.Forward[0]);
            u = BySlot(B, 0);
            Corridor(B, path, u);
            int m = Middle(B, path);
            Place(u, path[m]);
            g = AddGadget(B, "taret", path[m + 1], new[] { 0, 1 });
            Baskin.TestHurt(B, g, 5);
            Assert.IsTrue(g.Dead);
            Assert.IsNull(Baskin.CellAt(B, path[m + 1].X, path[m + 1].Y).O);
        }

        /// <summary>Oyuncunun EMP'si, makine kilidi ve donma aygıta işlemez (K10).</summary>
        [Test]
        public void Gadget_ImmuneEmpAndLock()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, null, 0, len: 6, slot: 1);
            Assert.AreEqual("emp", u.Type);
            var g = AddGadget(B, "vinc", Step(at, d, 2), new[] { -d[0], -d[1] }, turns: 5);
            var g2 = AddGadget(B, "taret", Step(at, d, 3), d, turns: 5);
            Assert.IsTrue(Baskin.DoAct(B, u.Id, g.X, g.Y));
            Assert.AreEqual(2, g.Hp, "EMP aygıta hasar vermez");
            Assert.AreEqual((Step(at, d, 3).X, Step(at, d, 3).Y), (g2.X, g2.Y), "EMP aygıtı itmez");
            // Kilit
            B = Entry();
            (u, at, d) = Arm(B, null, 0, len: 4, slot: 1);
            u.X = u.Y = -1; u.Out = true;
            var e = Machine(B, "kilit", at);
            g = AddGadget(B, "vinc", Step(at, d, 2), d, turns: 5);
            e.Intent = new Baskin.Intent { Kind = "lock", Dir = DirOf(d) };
            e.Order = 1;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsFalse(g.Locked);
            Assert.IsTrue(B.FxList.Any(f => f.K == "attack" && f.Unit == e.Id));
            Assert.IsFalse(B.FxList.Any(f => f.K == "lock"));
        }

        /// <summary>Aygıt sonuç listelerine ve kaçışa girmez: son uzman tesisin girişinden çıkınca baskın biter.</summary>
        [Test]
        public void Gadget_NotInResults_NotInEscape()
        {
            var wing = Wings.First(w => w[0].Mission != "loot");
            var B = Open(Params(wing, 41, 1, false), new List<RoomReport>(), B0);
            Calm(B);
            B.Escape = true;
            Assert.IsTrue(Baskin.IsFinalExit(B));
            var u = BySlot(B, 0);
            BySlot(B, 1).Out = true;
            BySlot(B, 2).Out = true;
            Baskin.Pt? west = null, near = null;
            foreach (var w in B.Room.West)
            {
                if (west != null || Baskin.UnitAt(B, w.X, w.Y) != null && Baskin.UnitAt(B, w.X, w.Y) != u) continue;
                foreach (var dd in Baskin.Dirs)
                {
                    int x = w.X + dd[0], y = w.Y + dd[1];
                    if (west == null && FreeAt(B, x, y) && Baskin.CellAt(B, x, y).Door == 0) { west = w; near = new Baskin.Pt(x, y); }
                }
            }
            Assert.IsNotNull(west);
            Place(u, near.Value);
            var g = AddGadget(B, "taret", FreeNeighbor(B, near.Value) ?? FindLine(B, 1).Value.At, new[] { 1, 0 });
            Assert.IsTrue(Baskin.DoMove(B, u.Id, west.Value.X, west.Value.Y));
            Assert.AreEqual("won", B.Phase, "tahtada aygıt kalsa da son uzmanla baskın biter");
            Assert.IsFalse(g.Out || g.Dead);
            var res = Baskin.Result(B);
            Assert.AreEqual(3, res.Hp.Count);
            Assert.AreEqual(3, res.Armor.Count);
        }

        /// <summary>Aygıt odada kalır: rapora yazılır, dönüş tahtası onu kalan turuyla kurar.</summary>
        [Test]
        public void Gadget_StaysInRoom_ReturnBoardRestores()
        {
            var p = Params(Wings[0], 41, 1, false);
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            Calm(B, 1);
            var e = B.Units.First(x => x.Side == 'e' && !x.Dead);
            e.Intent = null;
            var (u, at, d) = Arm(B, FacilityGear.Turret, 1, ok: s => Math.Abs(s.X - e.X) + Math.Abs(s.Y - e.Y) > 3);
            var to = Step(at, d, 1);
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActTurret, to.X, to.Y));
            ClearGate(B);
            var r = PassRoom(p, reps, B);
            var ru = r.Units.Single();
            Assert.AreEqual((Baskin.ClassGadget, "taret", to.X, to.Y, DirOf(d), 3, 0, "a"), (ru.Class, ru.Type, ru.X, ru.Y, ru.Dir, ru.Turns, ru.Owner, ru.State));
            Assert.IsEmpty(RaidChain.CollectUnits(r), "aygıt ekiple gitmez");
            if (RaidChain.IsClean(r)) Assert.Inconclusive("G temiz kaldı");
            reps.Add(Rep(2));
            reps.Add(Rep(3));
            var G = Open(p, reps, B0);
            var g = G.Units.SingleOrDefault(Baskin.IsGadget);
            Assert.IsNotNull(g, "dönüş tahtası aygıtı kurar");
            Assert.AreEqual(("taret", DirOf(d), 3, 0), (g.Type, g.Dir, g.Turns, g.Owner));
        }

        /// <summary>Aygıt RNG çekmez: aygıtlı ve aygıtsız tahtada makine evresi aynı sayıda çekim yapar.</summary>
        [Test]
        public void Gadget_NoRngDraw()
        {
            var B1 = Entry(keep: 1);
            var e = B1.Units.First(x => x.Side == 'e' && !x.Dead);
            e.Intent = null;
            var line = FindLine(B1, 2, s => Math.Abs(s.X - e.X) + Math.Abs(s.Y - e.Y) > 4);
            Assert.IsNotNull(line);
            var B2 = B1.Clone();
            AddGadget(B2, "yem", line.Value.At, line.Value.D);
            Assert.IsTrue(Baskin.EndTurn(B1));
            Assert.IsTrue(Baskin.EndTurn(B2));
            Assert.AreEqual(B1.Rs, B2.Rs);
        }

        // ---------- Oda raporu ----------
        [Test]
        public void Report_GearPart_OnlyWhenSet()
        {
            var r = Rep(1);
            string old = r.Encode();
            StringAssert.Contains("a:3:0:0:-1:0:-1:-1,", old);
            r.Team[0].Gear = 2;
            string s = r.Encode();
            StringAssert.StartsWith(old.Split('|')[0], s);
            StringAssert.Contains("a:3:0:0:-1:0:-1:-1:0:0:2,", s);
            var back = RoomReport.Decode(s);
            Assert.AreEqual((2, -1, -1), (back.Team[0].Gear, back.Team[1].Gear, back.Team[2].Gear));
            Assert.AreEqual(s, back.Encode());
            r.Team[0].Gear = -1;
            Assert.AreEqual(old, r.Encode(), "ekipmansız rapor eski dizgi");
            r.Team[1].Body = 1;
            StringAssert.Contains("a:3:0:0:-1:0:-1:-1:1:0,", r.Encode());
        }

        [Test]
        public void Report_UnitsSniffCargoDog_Tail_Trimmed()
        {
            var r = Rep(2);
            Assert.AreEqual(15, r.Encode().Split('|').Length);
            r.Units.Add(new RoomUnit { Class = Baskin.ClassGadget, Type = "taret", Owner = 1, Hp = 2, X = 3, Y = 4, Dir = 2, Turns = 1 });
            Assert.AreEqual(19, r.Encode().Split('|').Length);
            r.Sniff = 3;
            Assert.AreEqual(20, r.Encode().Split('|').Length);
            r.CargoDog = true;
            Assert.AreEqual(21, r.Encode().Split('|').Length);
            var back = RoomReport.Decode(r.Encode());
            Assert.AreEqual(r.Encode(), back.Encode());
            var u = back.Units.Single();
            Assert.AreEqual((Baskin.ClassGadget, "taret", 1, "a", 2, 0, 3, 4, 2, 1), (u.Class, u.Type, u.Owner, u.State, u.Hp, u.Armor, u.X, u.Y, u.Dir, u.Turns));
            Assert.AreEqual((3, true), (back.Sniff, back.CargoDog));
            // Ara alanlar boşken de yer tutar
            var r2 = Rep(2);
            r2.Sniff = 1;
            var b2 = RoomReport.Decode(r2.Encode());
            Assert.AreEqual((1, 0, false), (b2.Sniff, b2.Units.Count, b2.CargoDog));
        }

        [Test]
        public void Report_Decode_OldString_Defaults()
        {
            var old = "1|0|3|pass|0|0|0|-1|0|2||a:3:0:0:-1:0:0:3,a:3:0:0:-1:0:0:4,d:0:0:0:-1:1:2:2:1:0|dron:5:5:2:0|civ:4:4:1:1|";
            var r = RoomReport.Decode(old);
            Assert.AreEqual(old, r.Encode());
            Assert.IsEmpty(r.Units);
            Assert.AreEqual((0, false), (r.Sniff, r.CargoDog));
            Assert.IsTrue(r.Team.All(m => m.Gear == -1));
            Assert.AreEqual(1, r.Team[2].Body);
        }

        /// <summary>Ekipmanın kalan hakkı odadan odaya taşınır (emirdeki SquadSlot.Gear → tahta → rapor → sonraki tahta).</summary>
        [Test]
        public void Carry_GearLeftAcrossRooms()
        {
            var p = Params(Wings[0], 41, 1, false);
            p.Squad = new List<SquadSlot>
            {
                new SquadSlot { Type = "tek", Hp = 3, Gear = FacilityGear.Wire, GearCharges = 2 },
                new SquadSlot { Type = "emp", Hp = 3 },
                new SquadSlot { Type = "ana", Hp = 2 },
            };
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            Calm(B);
            var u = BySlot(B, 0);
            Assert.AreEqual((FacilityGear.Wire, 2), (u.Gear, u.GearLeft));
            Assert.IsNull(BySlot(B, 1).Gear);
            var (_, at, d) = Arm(B, null, 0);
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActWire, Step(at, d, 1).X, Step(at, d, 1).Y));
            var r = PassRoom(p, reps, B);
            Assert.AreEqual((1, -1, -1), (r.Team[0].Gear, r.Team[1].Gear, r.Team[2].Gear));
            var A = Open(p, reps, B0);
            Assert.AreEqual((FacilityGear.Wire, 1), (BySlot(A, 0).Gear, BySlot(A, 0).GearLeft));
        }

        // ---------- Kazanımlar (tahta) ----------
        [Test]
        public void MoveRules_NewFields_AndPerks()
        {
            var m = MoveRules.From(null);
            var b = new Balance();
            Assert.AreEqual((b.facilityWireHp, b.facilityPlankHp, b.facilityPlankCost, b.facilityScrapHp, b.facilityDeviceTurns, b.facilityDeviceHp),
                (m.WireHp, m.PlankHp, m.PlankCost, m.ScrapHp, m.DeviceTurns, m.DeviceHp));
            Assert.AreEqual((b.facilityTurretRange, b.facilityWinchRange, b.facilityWinchPull, b.facilityDecoyStep, b.facilityClampTurns),
                (m.TurretRange, m.WinchRange, m.WinchPull, m.DecoyStep, m.ClampTurns));
            Assert.AreEqual((b.facilityDogHp, b.facilityDogMove, b.facilityBarkRange, false), (m.DogHp, m.DogMove, m.BarkRange, m.Stretcher));
            // Kurucunun varsayılanları dengeyle aynı
            var c = new MoveRules(4, 3, 2, 3, 1);
            Assert.AreEqual(DumpAny(m), DumpAny(c));
            var all = new RaidParams { Perks = new List<string>(FacilityGear.Perks) };
            var k = MoveRules.From(b, all);
            Assert.AreEqual(b.facilityAmbushRange + b.facilitySightRange, k.AmbushRange, "Nişan");
            Assert.AreEqual(b.facilityPulleyPull, k.WinchPull, "Makara");
            Assert.AreEqual(b.facilityDogHp + b.facilityFurHp, k.DogHp, "Kürk");
            Assert.IsTrue(k.Stretcher, "Sedye");
            Assert.IsTrue(MoveRules.From(new Balance { facilityStretcherAlways = true }).Stretcher, "ölçüm anahtarı");
            // Öbür kazanımlar tahta değerlerine işlemez
            var rest = new RaidParams { Perks = FacilityGear.Perks.Where(x => x != FacilityGear.Sight && x != FacilityGear.Pulley && x != FacilityGear.Fur && x != FacilityGear.Stretcher).ToList() };
            Assert.AreEqual(DumpAny(m), DumpAny(MoveRules.From(b, rest)));
        }

        [Test]
        public void Perk_Threshold_EntryPlusOne_OpenLine_ReturnPlusOne()
        {
            var b = Bal(false);
            var wing = Wings[0];
            var p = Params(wing, 41, 1, false);
            var q = Params(wing, 41, 1, false);
            q.Perks = new List<string> { FacilityGear.Threshold, FacilityGear.OpenLine };
            var none = new List<RoomReport>();
            Assert.AreEqual(b.facilityEntryTurns, RaidChain.Setup(b, wing[0], p, none, false).Turns);
            Assert.AreEqual(b.facilityEntryTurns + b.facilityThresholdTurns, RaidChain.Setup(b, wing[0], q, none, false).Turns, "Eşik");
            Assert.AreEqual(b.facilityMiddleTurns, RaidChain.Setup(b, wing[1], q, new List<RoomReport> { Rep(1) }, false).Turns, "Eşik yalnız giriş odası");
            var back = new List<RoomReport> { Rep(1), Rep(2), Rep(3) };
            Assert.AreEqual(b.facilityReturnTurns, RaidChain.Setup(b, wing[0], p, back, true).Turns);
            Assert.AreEqual(b.facilityReturnTurns + b.facilityOpenLineTurns, RaidChain.Setup(b, wing[0], q, back, true).Turns, "Açık hat; Eşik dönüşe işlemez");
            Assert.AreEqual(b.facilityReturnTurns + b.facilityOpenLineTurns, RaidChain.Setup(b, wing[1], q, back, true).Turns);
            // Tahta da aynı süreyle kurulur
            Assert.AreEqual(b.facilityEntryTurns + 1, Open(q, none, b).MaxTurns);
        }

        [Test]
        public void Perk_Sight_AmbushPlusOne()
        {
            var p = Params(Wings[0], 41, 1, false);
            p.Perks = new List<string> { FacilityGear.Sight };
            var B = Open(p, new List<RoomReport>(), B0);
            Calm(B);
            Assert.AreEqual(5, B.Moves.AmbushRange);
            var (u, at, d) = Arm(B, null, 0, len: 6);
            u.AmbushDir = DirOf(d); u.AmbushX = at.X; u.AmbushY = at.Y;
            Assert.AreEqual(5, Baskin.AmbushLine(B, u).Count);
        }

        // ---------- Sedye ----------
        /// <summary>Elle G raporu: kadro sırası 1 kapıdan ekiple geçen gövde (Body 1).</summary>
        static List<RoomReport> WithBody()
        {
            var g = Rep(1);
            g.Team[1] = new RoomMember { State = "d", Fell = 1, X = 0, Y = 0, Body = 1 };
            return new List<RoomReport> { g };
        }

        static Balance Stretch(bool on) { var b = Bal(false); b.facilityStretcherAlways = on; return b; }

        [Test]
        public void Stretcher_BodyStaysWithTeam_NotPlaced()
        {
            var p = Params(Wings[0], 41, 1, false);
            var reps = WithBody();
            var A = Open(p, reps, Stretch(true));
            Assert.IsTrue(A.Moves.Stretcher);
            Assert.IsNull(BySlot(A, 1), "sedyedeki gövde tahtaya konmaz");
            Calm(A);
            var r = PassRoom(p, reps, A);
            Assert.AreEqual(("d", 1, 1), (r.Team[1].State, r.Team[1].Body, r.Team[1].Fell), "rapor gövdeyi aynen taşır");
            // Kapalıyken bugünkü kural: gövde tahtaya konur
            var off = Open(p, WithBody(), Stretch(false));
            Assert.IsTrue(BySlot(off, 1).Dead);
        }

        [Test]
        public void Stretcher_ExtractedIfAnyStanding_OldRuleDies()
        {
            foreach (bool on in new[] { true, false })
            {
                var p = Params(Wings[0], 41, 1, false);
                var reps = WithBody();
                var A = Open(p, reps, Stretch(on));
                Calm(A);
                Assert.IsTrue(Baskin.Retreat(A));
                var r = File(p, reps, A);
                Assert.IsTrue(r.Last);
                var res = RaidChain.Result(p, reps);
                Assert.AreEqual(on, res.Extracted.Contains(1), "sedye " + on);
            }
        }

        [Test]
        public void Stretcher_DiesIfNoneStanding()
        {
            var p = Params(Wings[0], 41, 1, false);
            var reps = WithBody();
            var A = Open(p, reps, Stretch(true));
            Calm(A);
            Down(A, BySlot(A, 0));
            Down(A, BySlot(A, 2));
            Assert.IsTrue(Baskin.Retreat(A));
            var r = File(p, reps, A);
            Assert.IsTrue(r.Last);
            Assert.AreEqual((0, 2), (r.Team[1].Body, r.Team[1].Fell));
            Assert.IsEmpty(RaidChain.Result(p, reps).Extracted);
        }

        // ---------- Bütünlük ----------
        [Test]
        public void Signature_NewFieldsOnlyWhenSet()
        {
            var B = Entry();
            var (u, at, d) = Arm(B, null, 0);
            string sig = Baskin.Signature(B);
            StringAssert.DoesNotContain("|n", sig);
            Gear(u, FacilityGear.Wire, 2);
            StringAssert.Contains("qwire_kit2", Baskin.Signature(B));
            B.Sniffed = 2;
            StringAssert.Contains("|n2|R", Baskin.Signature(B));
            var g = AddGadget(B, "taret", Step(at, d, 1), d);
            StringAssert.Contains($"taret{g.X}{g.Y}:2Caw0d{DirOf(d)}u3", Baskin.Signature(B));
            Gear(u, null, -1);
            B.Sniffed = 0;
            B.Units.Remove(g);
            Assert.AreEqual(sig, Baskin.Signature(B), "ekler yalnız varsa");
        }
    }
}
