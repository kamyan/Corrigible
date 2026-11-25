using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis hamlelerinin API'si (aşama 6, Docs/TESIS-BASKINLARI-TASARIMI.md §4): eylem kimlikleri ve bedelleri, önkoşullar (eski
    /// tahta, kilit, kapı, kabin), fırlatma, hamle değerlerinin dengeden gelmesi ve geri almanın bütün durum alanlarını kapsaması.
    /// Parça B: pusu, koşu darbesi, iki kişilik iş (ağır kepenk, büyük sandık), menfez, büyük sandığın plandaki yeri ve ödülü; her
    /// hamlenin önizlemesi uygulamayla aynıdır ve geri alma tam durumu getirir. Sürükleme ve yük <see cref="FacilityFallenTests"/>'te.
    /// </summary>
    public class FacilityMoveTests
    {
        static Balance B0 => Bal(events: false);

        /// <summary>G'nin tahtası (olaysız, makinesiz).</summary>
        static Baskin.BState Entry(int wing = 0, int seed = 41)
        {
            var p = Params(Wings[wing], seed, 1, false);
            var B = Open(p, new List<RoomReport>(), B0);
            Calm(B);
            return B;
        }

        /// <summary>İkmal kanadının H'si (elle G ve A raporu), kaçış başladı (taşıyıcı Teknisyen), makinesiz.</summary>
        static Baskin.BState LootTarget(int seed = 43)
        {
            var wing = Wings.First(w => w[0].Mission == "loot");
            var p = Params(wing, seed, 1, false);
            var H = Open(p, new List<RoomReport> { Rep(1), Rep(2) }, B0);
            Calm(H);
            StartEscape(H);
            return H;
        }

        /// <summary>Taşıyıcıyı düz boş hattın başına, alıcıyı <paramref name="k"/> kare öteye koyar.</summary>
        static (Baskin.Unit From, Baskin.Unit To, Baskin.Pt At, int[] D) ThrowLine(Baskin.BState H, int k)
        {
            var line = FindLine(H, k);
            Assert.IsNotNull(line, "düz hat yok: " + H.Room.Id);
            var c = H.Units.First(u => u.Id == H.Carrier);
            var to = Standing(H).First(u => u != c);
            Place(c, line.Value.At);
            Place(to, Step(line.Value.At, line.Value.D, k));
            return (c, to, line.Value.At, line.Value.D);
        }

        // ---------- API ----------
        [Test]
        public void Kinds_AndCosts()
        {
            CollectionAssert.AreEqual(new[] { "pusu", "kosu", "surukle", "firlat", "ortak", "menfez" }, Baskin.ActionKinds);
            Assert.AreEqual(Baskin.ActionCost.Ability, Baskin.CostOf(Baskin.ActAmbush));
            Assert.AreEqual(Baskin.ActionCost.Ability, Baskin.CostOf(Baskin.ActThrow));
            Assert.AreEqual(Baskin.ActionCost.Ability, Baskin.CostOf(Baskin.ActJoint));
            Assert.AreEqual(Baskin.ActionCost.MoveAndAbility, Baskin.CostOf(Baskin.ActRush));
            Assert.AreEqual(Baskin.ActionCost.MoveAndAbility, Baskin.CostOf(Baskin.ActDrag));
            Assert.AreEqual(Baskin.ActionCost.Move, Baskin.CostOf(Baskin.ActVent));
            Assert.Throws<ArgumentException>(() => Baskin.CostOf("yok"));
        }

        [Test]
        public void MoveRules_FromBalance()
        {
            var d = MoveRules.From(null);
            Assert.AreEqual((4, 3, 2, 3, 1), (d.AmbushRange, d.RushRun, d.RushPush, d.ThrowRange, d.DragCost));
            var b = new Balance { facilityAmbushRange = 5, facilityRushRun = 2, facilityRushPush = 1, facilityThrowRange = 4, facilityDragMoveCost = 0 };
            var m = MoveRules.From(b);
            Assert.AreEqual((5, 2, 1, 4, 0), (m.AmbushRange, m.RushRun, m.RushPush, m.ThrowRange, m.DragCost));
            Assert.AreEqual((2, 1), (new Balance().facilityBigCrateMetal, new Balance().facilityBigCrateCircuits));
            // Oda kurulumu dengeden taşır; tahtalar paylaşır
            var p = Params(Wings[0], 41, 1, false);
            var B = Open(p, new List<RoomReport>(), new Balance { liberationRegionEvents = false, facilityThrowRange = 5 });
            Assert.AreEqual(5, B.Moves.ThrowRange);
            Assert.AreSame(B.Moves, B.Clone().Moves);
        }

        [Test]
        public void NotOnBoard_NoActions()
        {
            var H = LootTarget();
            var c = H.Units.First(u => u.Id == H.Carrier);
            Assert.IsTrue(Baskin.CanAction(H, c, Baskin.ActThrow));
            foreach (var flag in new[] { "gate", "cab", "dead" })
            {
                var C = H.Clone();
                var u = C.Units.First(v => v.Id == c.Id);
                if (flag == "gate") u.AtGate = true; else if (flag == "cab") u.Cab = true; else u.Dead = true;
                foreach (var kind in Baskin.ActionKinds) Assert.IsFalse(Baskin.CanAction(C, u, kind), flag + " " + kind);
            }
            var machineTurn = H.Clone();
            machineTurn.Phase = "lost";
            Assert.IsFalse(Baskin.CanAction(machineTurn, machineTurn.Units.First(v => v.Id == c.Id), Baskin.ActThrow));
            Assert.IsFalse(Baskin.CanAction(H, H.Units.First(u => u.Side == 'e'), Baskin.ActThrow), "makine eylem yapmaz");
        }

        [Test]
        public void LockedOrFrozen_OnlyAbilityKinds()
        {
            var H = LootTarget();
            var c = H.Units.First(u => u.Id == H.Carrier);
            c.Moved = true; // kilitli ya da donmuş uzmanın turu böyle başlar
            Assert.IsTrue(Baskin.CanAction(H, c, Baskin.ActThrow), "fırlatma yeteneğin yerine: serbest");
            Assert.IsTrue(Baskin.CanAction(H, c, Baskin.ActAmbush));
            Assert.IsTrue(Baskin.CanAction(H, c, Baskin.ActJoint));
            Assert.IsFalse(Baskin.CanAction(H, c, Baskin.ActRush), "koşu yürüyüş ister");
            Assert.IsFalse(Baskin.CanAction(H, c, Baskin.ActDrag));
            c.Acted = true;
            foreach (var kind in Baskin.ActionKinds) Assert.IsFalse(Baskin.CanAction(H, c, kind), kind);
        }

        [Test]
        public void TypeConditions()
        {
            var H = LootTarget();
            var c = H.Units.First(u => u.Id == H.Carrier);
            var other = Standing(H).First(u => u != c);
            Assert.IsFalse(Baskin.CanAction(H, other, Baskin.ActThrow), "fırlatma yalnız taşıyıcıda");
            foreach (var u in Standing(H))
                Assert.AreEqual(u.Type == "ana", Baskin.CanAction(H, u, Baskin.ActVent), "menfez yalnız Analist: " + u.Type);
        }

        // ---------- Fırlatma ----------
        [Test]
        public void Throw_ChangesCarrier([Values(1, 2, 3)] int k)
        {
            var H = LootTarget();
            var (c, to, _, _) = ThrowLine(H, k);
            var targets = Baskin.ActionTargets(H, c, Baskin.ActThrow);
            Assert.IsTrue(targets.Any(t => t.X == to.X && t.Y == to.Y), "alıcı hedef");
            var pv = Baskin.PreviewAction(H, c.Id, Baskin.ActThrow, to.X, to.Y);
            Assert.AreEqual(to.Id, pv.Carrier, "önizleme taşıyıcıyı gösterir");
            Assert.IsEmpty(pv.Moves);
            Assert.IsTrue(Baskin.DoAction(H, c.Id, Baskin.ActThrow, to.X, to.Y));
            Assert.AreEqual(to.Id, H.Carrier);
            Assert.IsTrue(c.Acted && c.Moved, "yeteneğin yerine");
            CollectionAssert.AreEqual(pv.Fx.Select(f => f.K), H.FxList.Select(f => f.K));
            var fx = H.FxList.Single(f => f.K == "throw");
            Assert.AreEqual((to.X, to.Y, c.Id, to.Id), (fx.X, fx.Y, fx.Unit, fx.N));
            Assert.AreEqual(Loc.F("{0} yükü fırlattı; yükü artık {1} taşıyor.", Baskin.UnitTypes[c.Type].Name, Baskin.UnitTypes[to.Type].Name), H.Log.Last().Text);
            Assert.IsTrue(Baskin.CanAction(H, to, Baskin.ActThrow), "alıcı geri atabilir");
        }

        [Test]
        public void Throw_LineBlocked_BodyDoesNot()
        {
            var H = LootTarget();
            var (c, to, at, d) = ThrowLine(H, 2);
            var mid = Step(at, d, 1);
            var cell = Baskin.CellAt(H, mid.X, mid.Y);
            cell.O = new Baskin.Obj { K = "civ", Hp = 1, Max = 1 };
            Assert.IsEmpty(Baskin.ActionTargets(H, c, Baskin.ActThrow).Where(t => t.X == to.X && t.Y == to.Y), "nesne keser");
            cell.O = null;
            cell.T = "wall";
            Assert.IsEmpty(Baskin.ActionTargets(H, c, Baskin.ActThrow).Where(t => t.X == to.X && t.Y == to.Y), "duvar keser");
            cell.T = "floor";
            var foe = H.Units.First(u => u.Side == 'e');
            foe.Dead = false; foe.Hp = 1; Place(foe, mid);
            Assert.IsEmpty(Baskin.ActionTargets(H, c, Baskin.ActThrow).Where(t => t.X == to.X && t.Y == to.Y), "makine keser");
            foe.Dead = true;
            var third = Standing(H).First(u => u != c && u != to);
            Place(third, mid);
            Assert.IsTrue(Baskin.ActionTargets(H, c, Baskin.ActThrow).Any(t => t.X == mid.X && t.Y == mid.Y), "aradaki ekip uzmanı ilk alıcıdır");
            Assert.IsFalse(Baskin.ActionTargets(H, c, Baskin.ActThrow).Any(t => t.X == to.X && t.Y == to.Y), "arkasındaki değil");
            third.Dead = true; third.Hp = 0;
            Assert.IsTrue(Baskin.ActionTargets(H, c, Baskin.ActThrow).Any(t => t.X == to.X && t.Y == to.Y), "gövde hattı kesmez");
        }

        [Test]
        public void Throw_RangeIsBalance()
        {
            var H = LootTarget();
            var (c, to, _, _) = ThrowLine(H, 4);
            Assert.IsFalse(Baskin.ActionTargets(H, c, Baskin.ActThrow).Any(t => t.X == to.X && t.Y == to.Y), "4 kare: menzil 3");
            H.Moves = new MoveRules(4, 3, 2, 4, 1);
            Assert.IsTrue(Baskin.ActionTargets(H, c, Baskin.ActThrow).Any(t => t.X == to.X && t.Y == to.Y), "menzil dengeden");
        }

        [Test]
        public void Throw_NotToGate()
        {
            var H = LootTarget();
            var (c, to, _, _) = ThrowLine(H, 2);
            to.AtGate = true;
            Assert.IsFalse(Baskin.ActionTargets(H, c, Baskin.ActThrow).Any(t => t.X == to.X && t.Y == to.Y), "kapıdaki tahtada değil");
        }

        [Test]
        public void Throw_Undo()
        {
            var H = LootTarget();
            var (c, to, _, _) = ThrowLine(H, 2);
            AssertUndoRestores(H, b => Baskin.DoAction(b, c.Id, Baskin.ActThrow, to.X, to.Y), "fırlatma");
            Assert.AreEqual(c.Id, H.Carrier);
        }

        [Test]
        public void Throw_ThenCatcherCarriesThroughGate()
        {
            var H = LootTarget();
            var (c, to, _, _) = ThrowLine(H, 2);
            Assert.IsTrue(Baskin.DoAction(H, c.Id, Baskin.ActThrow, to.X, to.Y));
            // Atılan yükü alıcı düşürürse yük yere düşer (taşıyıcı kimse o)
            Down(H, to);
            Assert.AreEqual(0, H.Carrier);
            Assert.AreEqual((to.X, to.Y), (H.CargoX, H.CargoY));
        }

        // ---------- Geri alma ----------
        /// <summary>CopyInto bütün BState alanlarını kapsar: hamleden sonra her alan (evre ve geçmiş hariç) varsayılan dışına çekilir;
        /// geri alma tam dökümü hamleden önceki hâle getirmeli. Yeni alan CopyInto'ya eklenmezse burada kırılır.</summary>
        [Test]
        public void CopyInto_CoversEveryField()
        {
            var B = LootTarget();
            var c = B.Units.First(u => u.Id == B.Carrier);
            var to = Reach(B, c);
            string before = FullDump(B);
            Assert.IsTrue(Baskin.DoMove(B, c.Id, to.X, to.Y));
            int changed = 0;
            foreach (var f in typeof(Baskin.BState).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (f.Name == "Hist" || f.Name == "Phase" || f.IsInitOnly) continue;
                var v = f.GetValue(B);
                if (f.FieldType == typeof(int)) f.SetValue(B, (int)v + 7);
                else if (f.FieldType == typeof(bool)) f.SetValue(B, !(bool)v);
                else if (f.FieldType == typeof(string)) f.SetValue(B, (string)v + "x");
                else if (f.FieldType == typeof(double)) f.SetValue(B, (double)v + 1);
                else if (!f.FieldType.IsValueType) f.SetValue(B, null);
                else Assert.Fail("sınanmayan alan türü: " + f.Name);
                changed++;
            }
            Assert.GreaterOrEqual(changed, 40);
            Assert.AreNotEqual(before, FullDump(B));
            Assert.IsTrue(Baskin.Undo(B));
            Assert.AreEqual(before, FullDump(B), "CopyInto bir alanı atlıyor");
        }

        static Baskin.Pt Reach(Baskin.BState B, Baskin.Unit u)
        {
            var r = Baskin.Reachable(B, u).Where(t => !Baskin.IsGateTile(B, t.X, t.Y)).ToList();
            Assert.IsNotEmpty(r);
            return r[0];
        }

        [Test]
        public void Signature_NewSegments_OnlyWhenSet()
        {
            var B = Entry();
            string sig = Baskin.Signature(B);
            StringAssert.DoesNotContain("|y", sig);
            StringAssert.DoesNotContain("|h", sig);
            StringAssert.DoesNotContain("|k", sig);
            StringAssert.DoesNotContain("|B", sig);
            B.CargoX = 3; B.CargoY = 4; B.HoldId = 2; B.HoldX = 5; B.HoldY = 6; B.CrateOpen = true;
            var u = BySlot(B, 0);
            u.AmbushDir = 1; u.AmbushX = u.X; u.AmbushY = u.Y;
            string s2 = Baskin.Signature(B);
            StringAssert.Contains("|y34|h2.56", s2);
            StringAssert.Contains("|B|R", s2);
            StringAssert.Contains($"a1{u.X}{u.Y}", s2);
            Assert.IsTrue(s2.EndsWith("|R" + B.Room.Id), "oda eki sonda");
        }

        [Test]
        public void Hold_ClearedAtTurnEndAndPass()
        {
            var B = Entry();
            B.HoldId = BySlot(B, 0).Id; B.HoldX = 1; B.HoldY = 1;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((0, -1, -1), (B.HoldId, B.HoldX, B.HoldY));
            Assert.IsTrue(B.Log.Any(l => l.Text == Loc.T("Tutuş boşa gitti.")));
            B.HoldId = BySlot(B, 0).Id; B.HoldX = 1; B.HoldY = 1;
            ToGate(B);
            Assert.IsTrue(Baskin.Pass(B));
            Assert.AreEqual(0, B.HoldId, "GEÇ tutuşu bırakır");
        }

        // ---------- Parça B: yardımcılar ----------
        /// <summary>G tahtası (olaysız, makinesiz) ve üstünde <paramref name="len"/> + 1 karelik düz boş hat (kanatlar sırayla aranır).</summary>
        static (Baskin.BState B, Baskin.Pt At, int[] D) EntryLine(int len)
        {
            for (int w = 0; w < Wings.Count; w++)
            {
                var B = Entry(w);
                var line = FindLine(B, len);
                if (line != null) return (B, line.Value.At, line.Value.D);
            }
            Assert.Fail("düz hat yok: " + len);
            return default;
        }

        /// <summary>Devre dışı bir makineyi kareye dron olarak geri getirir (zırhsız, buzsuz, niyetsiz).</summary>
        static Baskin.Unit Foe(Baskin.BState B, Baskin.Pt at, int hp = 5)
        {
            var e = B.Units.First(u => u.Side == 'e');
            e.Dead = false; e.Type = "dron"; e.Hp = e.Max = hp; e.Armor = 0; e.Ice = 0; e.Frozen = false; e.Intent = null;
            Place(e, at);
            return e;
        }

        /// <summary>A'nın tahtası (olaysız, makinesiz; elle G raporu).</summary>
        static Baskin.BState Middle(int wing = 0, int seed = 41)
        {
            var p = Params(Wings[wing], seed, 1, false);
            var B = Open(p, new List<RoomReport> { Rep(1) }, B0);
            Calm(B);
            Assert.AreEqual(RoomType.Middle, B.Room.Type);
            return B;
        }

        /// <summary>Büyük sandıklı ilk ikmal kanadının H'si (elle G ve A raporu; kaçış başlamadı), makinesiz.</summary>
        static (RaidParams P, List<RoomReport> Reps, Baskin.BState B) CrateTarget(int seed = 43)
        {
            var wing = Wings.First(w => w[0].Mission == "loot" && w[2].BigCrate != null);
            var p = Params(wing, seed, 1, false);
            var reps = new List<RoomReport> { Rep(1), Rep(2) };
            var H = Open(p, reps, B0);
            Calm(H);
            return (p, reps, H);
        }

        /// <summary>İş karesinin sekiz komşusundan geçilebilir ve birimsiz olanlar (y, x sırası).</summary>
        static List<Baskin.Pt> WorkTiles(Baskin.BState B, Baskin.Pt at)
        {
            var o = new List<Baskin.Pt>();
            for (int y = at.Y - 1; y <= at.Y + 1; y++)
                for (int x = at.X - 1; x <= at.X + 1; x++)
                    if ((x != at.X || y != at.Y) && FreeAt(B, x, y)) o.Add(new Baskin.Pt(x, y));
            return o;
        }

        /// <summary>Önizleme uygulamayla aynı (efektler, yer değişimleri) ve asıl durumu değiştirmez; sonra eylem uygulanır (Assert).</summary>
        static Baskin.PreviewResult Act(Baskin.BState B, Baskin.Unit u, string kind, Baskin.Pt at, int other = 0)
        {
            string dump = FullDump(B);
            var pv = Baskin.PreviewAction(B, u.Id, kind, at.X, at.Y, other);
            Assert.IsNotNull(pv, "önizleme: " + kind);
            Assert.AreEqual(dump, FullDump(B), "önizleme durumu değiştirdi: " + kind);
            var before = B.Units.ToDictionary(v => v.Id, v => (v.X, v.Y));
            Assert.IsTrue(Baskin.DoAction(B, u.Id, kind, at.X, at.Y, other), "eylem: " + kind);
            CollectionAssert.AreEqual(pv.Fx.Select(f => f.K + f.X + f.Y + f.Unit), B.FxList.Select(f => f.K + f.X + f.Y + f.Unit), "önizleme efektleri: " + kind);
            var moved = B.Units.Where(v => before.ContainsKey(v.Id) && before[v.Id] != (v.X, v.Y)).Select(v => v.Id).OrderBy(i => i);
            CollectionAssert.AreEqual(moved, pv.Moves.Select(m => m.Unit).OrderBy(i => i), "önizleme yer değişimleri: " + kind);
            Assert.AreEqual(B.Carrier, pv.Carrier);
            return pv;
        }

        // ---------- Pusu ----------
        [Test]
        public void Ambush_TargetsAndCost()
        {
            var (B, at, d) = EntryLine(4);
            var u = BySlot(B, 1);
            Place(u, at);
            var t = Baskin.ActionTargets(B, u, Baskin.ActAmbush);
            var expected = Baskin.Dirs.Select(v => new Baskin.Pt(at.X + v[0], at.Y + v[1]))
                .Where(p => p.X >= 0 && p.Y >= 0 && p.X < Baskin.N && p.Y < Baskin.N && Baskin.CellAt(B, p.X, p.Y).T != "wall" && Baskin.CellAt(B, p.X, p.Y).O == null
                    && Baskin.UnitAt(B, p.X, p.Y)?.Side != 'p');
            CollectionAssert.AreEqual(expected.Select(p => (p.X, p.Y)), t.Select(p => (p.X, p.Y)), "dik komşular, Dirs sırası");
            var first = Step(at, d, 1);
            var pv = Act(B, u, Baskin.ActAmbush, first);
            Assert.AreEqual(Array.FindIndex(Baskin.Dirs, v => v[0] == d[0] && v[1] == d[1]), u.AmbushDir);
            Assert.AreEqual((at.X, at.Y), (u.AmbushX, u.AmbushY));
            Assert.IsTrue(u.Acted && u.Moved, "yeteneğin yerine");
            CollectionAssert.AreEqual(Enumerable.Range(1, 4).Select(k => Step(at, d, k)).Select(p => (p.X, p.Y)), pv.Line.Select(p => (p.X, p.Y)), "önizleme hattı");
            CollectionAssert.AreEqual(pv.Line.Select(p => (p.X, p.Y)), Baskin.AmbushLine(B, u).Select(p => (p.X, p.Y)));
            Assert.AreEqual(Loc.F("{0} pusu kurdu.", Baskin.UnitTypes[u.Type].Name), B.Log.Last().Text);
        }

        [Test]
        public void Ambush_HitsFirstMachineOnLine_ThenCleared([Values(1, 2, 3, 4)] int k)
        {
            var (B, at, d) = EntryLine(4);
            B.StillTurns = 99; // makineler yürümez: hat kurulan yerinde vurur
            var u = BySlot(B, 1);
            Place(u, at);
            var e = Foe(B, Step(at, d, k));
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActAmbush, Step(at, d, 1).X, Step(at, d, 1).Y));
            int rs = B.Rs;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(4, e.Hp, "pusu 1 hasar");
            Assert.IsTrue(B.FxList.Any(f => f.K == "ambush-hit" && f.Unit == e.Id && f.N == u.Id));
            Assert.IsTrue(B.Units.All(v => v.AmbushDir == -1 && v.AmbushX == -1 && v.AmbushY == -1), "hat tur sonunda silinir");
            Assert.AreNotEqual(rs, B.Rs, "plan çekti");
        }

        [Test]
        public void Ambush_RangeIsBalance()
        {
            var (B, at, d) = EntryLine(4);
            B.StillTurns = 99;
            B.Moves = new MoveRules(3, 3, 2, 3, 1);
            var u = BySlot(B, 1);
            Place(u, at);
            var e = Foe(B, Step(at, d, 4));
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActAmbush, Step(at, d, 1).X, Step(at, d, 1).Y));
            Assert.AreEqual(3, Baskin.AmbushLine(B, u).Count);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(5, e.Hp, "4. kare: menzil 3");
        }

        [Test]
        public void Ambush_StopsAtFriendObjectWall([Values("dost", "nesne", "duvar")] string block)
        {
            var (B, at, d) = EntryLine(4);
            B.StillTurns = 99;
            var u = BySlot(B, 1);
            Place(u, at);
            var e = Foe(B, Step(at, d, 3));
            var mid = Step(at, d, 2);
            var friend = BySlot(B, 2);
            int friendHp = friend.Hp;
            if (block == "dost") Place(friend, mid);
            else if (block == "nesne") Baskin.CellAt(B, mid.X, mid.Y).O = new Baskin.Obj { K = "civ", Hp = 1, Max = 1 };
            else Baskin.CellAt(B, mid.X, mid.Y).T = "wall";
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActAmbush, Step(at, d, 1).X, Step(at, d, 1).Y));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(5, e.Hp, block + " hattı keser");
            Assert.AreEqual(friendHp, friend.Hp, "dost vurulmaz");
        }

        [Test]
        public void Ambush_TwoLinesHitTwice_KillDropsIntent()
        {
            var (B, at, d) = EntryLine(4);
            B.StillTurns = 99;
            var a = BySlot(B, 0); var b = BySlot(B, 1);
            Place(a, at);
            Place(b, Step(at, d, 4));
            var e = Foe(B, Step(at, d, 2), hp: 2);
            Assert.IsTrue(Baskin.DoAction(B, a.Id, Baskin.ActAmbush, Step(at, d, 1).X, Step(at, d, 1).Y));
            Assert.IsTrue(Baskin.DoAction(B, b.Id, Baskin.ActAmbush, Step(at, d, 3).X, Step(at, d, 3).Y));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(e.Dead, "iki hat ayrı ayrı vurur");
            Assert.AreEqual(2, B.FxList.Count(f => f.K == "ambush-hit"));
            Assert.IsFalse(Baskin.Intents(B).Any(i => i.Unit == e.Id), "ölen makinenin niyeti düşer");
            Assert.AreEqual("player", B.Phase);
        }

        [Test]
        public void Ambush_OwnerOffBoard_NoFire()
        {
            var (B, at, d) = EntryLine(4);
            B.StillTurns = 99;
            var u = BySlot(B, 1);
            Place(u, at);
            var e = Foe(B, Step(at, d, 2));
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActAmbush, Step(at, d, 1).X, Step(at, d, 1).Y));
            Down(B, u);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(5, e.Hp, "düşenin hattı ateşlenmez");
            Assert.AreEqual(-1, u.AmbushDir);
        }

        [Test]
        public void Ambush_NotFromCloud()
        {
            var (B, at, _) = EntryLine(4);
            var u = BySlot(B, 1);
            Place(u, at);
            Assert.IsNotEmpty(Baskin.ActionTargets(B, u, Baskin.ActAmbush));
            B.Ev = new Baskin.EvState { Id = "bulut" };
            Baskin.CellAt(B, at.X, at.Y).Cloud = true;
            Assert.IsEmpty(Baskin.ActionTargets(B, u, Baskin.ActAmbush), "buluttan pusu kurulmaz");
        }

        /// <summary>İlk karesinde ayakta ekip uzmanı duran ya da bulutlu yön pusu hedefi değildir (hat orada biterdi); makine ve gövde
        /// ilk kareyi kapatmaz.</summary>
        [Test]
        public void Ambush_NotTowardFriendOrCloud([Values("dost", "bulut", "makine", "govde")] string first)
        {
            var (B, at, d) = EntryLine(4);
            var u = BySlot(B, 1);
            Place(u, at);
            var t = Step(at, d, 1);
            bool Offered() => Baskin.ActionTargets(B, u, Baskin.ActAmbush).Any(q => q.X == t.X && q.Y == t.Y);
            Assert.IsTrue(Offered());
            var friend = BySlot(B, 2);
            if (first == "dost") Place(friend, t);
            else if (first == "bulut") { B.Ev = new Baskin.EvState { Id = "bulut" }; Baskin.CellAt(B, t.X, t.Y).Cloud = true; }
            else if (first == "makine") Foe(B, t);
            else { Place(friend, t); Down(B, friend); }
            bool offered = first == "makine" || first == "govde";
            Assert.AreEqual(offered, Offered(), first);
            Assert.AreEqual(offered, Baskin.DoAction(B, u.Id, Baskin.ActAmbush, t.X, t.Y), first);
        }

        [Test]
        public void Ambush_MachineAiBlind_NoRngDraw()
        {
            var (B, at, d) = EntryLine(4);
            var u = BySlot(B, 1);
            Place(u, at);
            var e = Foe(B, Step(at, d, 3));
            var C = B.Clone();
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActAmbush, Step(at, d, 1).X, Step(at, d, 1).Y));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(Baskin.EndTurn(C));
            var e2 = C.Units.First(v => v.Id == e.Id);
            Assert.AreEqual((e2.X, e2.Y, e2.Intent?.Kind, e2.Intent?.Dir), (e.X, e.Y, e.Intent?.Kind, e.Intent?.Dir), "plan hattı görmez");
            Assert.AreEqual(C.Rs, B.Rs, "pusu RNG çekmez");
        }

        [Test]
        public void Ambush_Undo()
        {
            var (B, at, d) = EntryLine(4);
            var u = BySlot(B, 1);
            Place(u, at);
            AssertUndoRestores(B, b => Baskin.DoAction(b, u.Id, Baskin.ActAmbush, Step(at, d, 1).X, Step(at, d, 1).Y), "pusu");
            Assert.AreEqual(-1, B.Units.First(v => v.Id == u.Id).AmbushDir);
        }

        // ---------- Koşu darbesi ----------
        [Test]
        public void Rush_PushesTwo()
        {
            var (B, at, d) = EntryLine(6);
            var u = BySlot(B, 0); // Teknisyen: yürüyüş 3
            Place(u, at);
            var e = Foe(B, Step(at, d, 4));
            Assert.IsTrue(Baskin.ActionTargets(B, u, Baskin.ActRush).Any(t => t.X == e.X && t.Y == e.Y));
            var pv = Act(B, u, Baskin.ActRush, Step(at, d, 4));
            Assert.AreEqual((Step(at, d, 3).X, Step(at, d, 3).Y), (u.X, u.Y), "koşucu makinenin önünde");
            Assert.AreEqual((Step(at, d, 6).X, Step(at, d, 6).Y), (e.X, e.Y), "iki kare itme");
            Assert.AreEqual(5, e.Hp, "darbenin kendi hasarı yok");
            Assert.AreEqual(3, u.Hp);
            Assert.IsTrue(u.Acted && u.Moved);
            CollectionAssert.AreEqual(new[] { "rush", "push", "push" }, B.FxList.Select(f => f.K));
            Assert.AreEqual(2, pv.Moves.Count);
        }

        [Test]
        public void Rush_FirstPushBlocked_NoSecond([Values("duvar", "nesne")] string block)
        {
            var (B, at, d) = EntryLine(6);
            var u = BySlot(B, 0);
            Place(u, at);
            var e = Foe(B, Step(at, d, 4));
            var behind = Step(at, d, 5);
            if (block == "duvar") Baskin.CellAt(B, behind.X, behind.Y).T = "wall";
            else Baskin.CellAt(B, behind.X, behind.Y).O = new Baskin.Obj { K = "cool", Hp = 5, Max = 5 };
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActRush, e.X, e.Y));
            Assert.AreEqual((Step(at, d, 4).X, Step(at, d, 4).Y), (e.X, e.Y), "yerinde kaldı");
            Assert.AreEqual(4, e.Hp, "tek çarpışma: 1 hasar");
            Assert.AreEqual(1, B.FxList.Count(f => f.K == "push"), "ikinci itme yok");
        }

        [Test]
        public void Rush_CableBurns()
        {
            var (B, at, d) = EntryLine(6);
            var u = BySlot(B, 0);
            Place(u, at);
            var e = Foe(B, Step(at, d, 4));
            var c = Step(at, d, 5);
            Baskin.CellAt(B, c.X, c.Y).T = "cable";
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActRush, e.X, e.Y));
            Assert.AreEqual(4, e.Hp, "kablo 1 hasar");
            Assert.AreEqual((Step(at, d, 6).X, Step(at, d, 6).Y), (e.X, e.Y), "itme sürer");
        }

        [Test]
        public void Rush_LineLength([Values(2, 3, 4, 5, 6)] int k)
        {
            var (B, at, d) = EntryLine(6);
            var tek = BySlot(B, 0); // yürüyüş 3: koşu 3
            var ana = B.Units.First(v => v.Side == 'p' && v.Type == "ana"); // yürüyüş 4: koşu 3–4
            var e = Foe(B, Step(at, d, k));
            Place(tek, at);
            bool hit(Baskin.Unit v) => Baskin.ActionTargets(B, v, Baskin.ActRush).Any(t => t.X == e.X && t.Y == e.Y);
            Assert.AreEqual(k == 4, hit(tek), "Teknisyen L = " + (k - 1));
            Place(tek, Step(at, d, -10));
            Place(ana, at);
            Assert.AreEqual(k == 4 || k == 5, hit(ana), "Analist L = " + (k - 1));
        }

        [Test]
        public void Rush_FriendObjectWall_NotOffered_BodyDoesNot([Values("dost", "nesne", "duvar", "govde")] string block)
        {
            var (B, at, d) = EntryLine(6);
            var u = BySlot(B, 0);
            Place(u, at);
            var e = Foe(B, Step(at, d, 4));
            var mid = Step(at, d, 2);
            if (block == "dost") Place(BySlot(B, 1), mid);
            else if (block == "nesne") Baskin.CellAt(B, mid.X, mid.Y).O = new Baskin.Obj { K = "civ", Hp = 1, Max = 1 };
            else if (block == "duvar") Baskin.CellAt(B, mid.X, mid.Y).T = "wall";
            else { var b = BySlot(B, 1); Place(b, mid); Down(B, b); }
            bool hit = Baskin.ActionTargets(B, u, Baskin.ActRush).Any(t => t.X == e.X && t.Y == e.Y);
            Assert.AreEqual(block == "govde", hit, block);
            if (hit)
            {
                Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActRush, e.X, e.Y));
                Assert.AreEqual((Step(at, d, 3).X, Step(at, d, 3).Y), (u.X, u.Y), "gövdenin üstünden koşar");
            }
        }

        [Test]
        public void Rush_EndsOnGate_StaysOnBoard()
        {
            var B = Entry();
            var g = B.Room.Forward[0];
            var g2 = B.Room.Forward[1];
            int dy = g2.Y - g.Y, dx = g2.X - g.X; // iki geçit karesi bitişik: koşu geçit boyunca, makine öbür geçitte
            var start = new Baskin.Pt(g.X - 3 * dx, g.Y - 3 * dy);
            if (start.X < 0 || start.Y < 0 || start.X >= Baskin.N || start.Y >= Baskin.N) { (g, g2) = (g2, g); dx = -dx; dy = -dy; start = new Baskin.Pt(g.X - 3 * dx, g.Y - 3 * dy); }
            for (int k = 0; k < 3; k++)
            {
                var c = Baskin.CellAt(B, start.X + dx * k, start.Y + dy * k);
                c.T = "floor"; c.O = null;
            }
            foreach (var v in B.Units.Where(v => v.Side == 'p')) Place(v, new Baskin.Pt(-5, -5));
            var u = BySlot(B, 0);
            Place(u, start);
            var e = Foe(B, g2);
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActRush, g2.X, g2.Y));
            Assert.AreEqual((g.X, g.Y), (u.X, u.Y));
            Assert.IsFalse(u.AtGate, "koşu kapıya sokmaz");
            Assert.IsTrue(Baskin.OnBoard(u));
            Assert.IsTrue(Baskin.EnterGate(B, u.Id), "aynı tur kapıya girebilir");
        }

        [Test]
        public void Rush_PicksCargo_PushedMachineDoesNot()
        {
            var (B, at, d) = EntryLine(6);
            var u = BySlot(B, 0);
            Place(u, at);
            var e = Foe(B, Step(at, d, 4));
            var end = Step(at, d, 3);
            B.CargoX = end.X; B.CargoY = end.Y;
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActRush, e.X, e.Y));
            Assert.AreEqual(u.Id, B.Carrier, "koşu bitişi yükü alır");
            Assert.AreEqual(-1, B.CargoX);
            // İtilen makine yükün karesine gelse de almaz (makineler yüke dokunmaz)
            var (C, at2, d2) = EntryLine(6);
            var r = BySlot(C, 0);
            Place(r, at2);
            var f = Foe(C, Step(at2, d2, 4));
            C.CargoX = Step(at2, d2, 6).X; C.CargoY = Step(at2, d2, 6).Y;
            Assert.IsTrue(Baskin.DoAction(C, r.Id, Baskin.ActRush, f.X, f.Y));
            Assert.AreEqual((C.CargoX, C.CargoY), (f.X, f.Y), "makine yükün karesinde");
            Assert.AreEqual(0, C.Carrier, "itilen almaz");
        }

        /// <summary>Koşu turnikenin kapalı yönünden geçmez (yürüyüşün kuralı, <see cref="Baskin.CanStep"/>); açık yönde geçer.</summary>
        [Test]
        public void Rush_Turnstile()
        {
            var B = Entry();
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++) { var c = Baskin.CellAt(B, x, y); c.T = "floor"; c.O = null; }
            foreach (var v in B.Units.Where(v => v.Side == 'p')) Place(v, new Baskin.Pt(-5, -5));
            var u = BySlot(B, 0); // Teknisyen: koşu 3
            // Hol (4, 3) çevresinde; (2, 3) turnikeli dış kare; vardiya içeri (Mode 0): holden (3, 3) → (2, 3) kapalı
            B.Ev = new Baskin.EvState { Id = "turnike", Q = new Baskin.Pt(4, 3), Area2 = new List<Baskin.Pt> { new Baskin.Pt(2, 3) } };
            Assert.IsFalse(Baskin.CanStep(B, 3, 3, 2, 3));
            Assert.IsTrue(Baskin.CanStep(B, 2, 3, 3, 3));
            // Batıya, holün içinden dışarı: (4, 3) → (1, 3)'teki makine; yol kapalı kenardan geçer
            Place(u, new Baskin.Pt(5, 3));
            var e = Foe(B, new Baskin.Pt(1, 3));
            Assert.IsFalse(Baskin.ActionTargets(B, u, Baskin.ActRush).Any(t => t.X == 1 && t.Y == 3), "kapalı yön");
            var ev = B.Ev;
            B.Ev = null;
            Assert.IsTrue(Baskin.ActionTargets(B, u, Baskin.ActRush).Any(t => t.X == 1 && t.Y == 3), "turnikesiz aynı hat açık");
            B.Ev = ev;
            // Doğuya, dışarıdan hole: açık yön
            Place(u, new Baskin.Pt(1, 3));
            Place(e, new Baskin.Pt(5, 3));
            Assert.IsTrue(Baskin.ActionTargets(B, u, Baskin.ActRush).Any(t => t.X == 5 && t.Y == 3), "açık yön");
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActRush, 5, 3));
            Assert.AreEqual((4, 3), (u.X, u.Y));
        }

        [Test]
        public void Rush_Undo()
        {
            var (B, at, d) = EntryLine(6);
            var u = BySlot(B, 0);
            Place(u, at);
            var e = Foe(B, Step(at, d, 4));
            AssertUndoRestores(B, b => Baskin.DoAction(b, u.Id, Baskin.ActRush, e.X, e.Y), "koşu");
        }

        // ---------- İki kişilik iş ----------
        [Test]
        public void Joint_TwoSameTurn_OpensShutter()
        {
            var B = Middle();
            var sh = B.Room.Shutter.Value;
            var doors = B.Room.ShutterDoors;
            var a = BySlot(B, 0); var b = BySlot(B, 1);
            Assert.IsNull(Baskin.UnitAt(B, doors[0].X, doors[0].Y));
            Assert.IsNull(Baskin.UnitAt(B, doors[1].X, doors[1].Y));
            Place(a, doors[0]); Place(b, doors[1]);
            Assert.IsFalse(Baskin.Reachable(B, a).Any(t => t.X == sh.X && t.Y == sh.Y), "kepenk kapalı");
            var pv1 = Act(B, a, Baskin.ActJoint, sh);
            Assert.IsFalse(pv1.Opens);
            Assert.AreEqual((a.Id, sh.X, sh.Y), (B.HoldId, B.HoldX, B.HoldY), "ilk uzman tutar");
            Assert.IsTrue(a.Acted && a.Moved, "yeteneğin yerine");
            Assert.AreEqual("wall", Baskin.CellAt(B, sh.X, sh.Y).T);
            Assert.AreEqual(Loc.F("{0} kepengi tutuyor: ikinci uzman bu tur gelmeli.", Baskin.UnitTypes[a.Type].Name), B.Log.Last().Text);
            Assert.IsFalse(Baskin.ActionTargets(B, a, Baskin.ActJoint).Any(), "tutan ikinci kez yapamaz");
            var pv2 = Act(B, b, Baskin.ActJoint, sh);
            Assert.IsTrue(pv2.Opens);
            var c = Baskin.CellAt(B, sh.X, sh.Y);
            Assert.AreEqual("floor", c.T, "kepenk kalktı");
            Assert.IsTrue(c.Shutter, "bayrak kalır");
            Assert.AreEqual((0, -1, -1), (B.HoldId, B.HoldX, B.HoldY));
            Assert.AreEqual(Loc.T("Kepenk kalktı."), B.Log.Last().Text);
            StringAssert.Contains($"|k{sh.X}{sh.Y}", Baskin.Signature(B));
            // Yol kısalır: kepengin karesinden yürünür; açık kepenk artık iş değil
            Assert.IsTrue(Baskin.Reachable(B, a).Any(t => t.X == sh.X && t.Y == sh.Y), "kepengin karesi yürünür");
            var C = B.Clone();
            var ca = C.Units.First(v => v.Id == a.Id);
            ca.Acted = ca.Moved = false;
            Assert.IsEmpty(Baskin.ActionTargets(C, ca, Baskin.ActJoint).Where(t => t.X == sh.X && t.Y == sh.Y), "açık kepenk iş değil");
        }

        [Test]
        public void Joint_SecondNextTurn_Wasted()
        {
            var B = Middle();
            var sh = B.Room.Shutter.Value;
            var a = BySlot(B, 0); var b = BySlot(B, 1);
            Place(a, B.Room.ShutterDoors[0]); Place(b, B.Room.ShutterDoors[1]);
            Assert.IsTrue(Baskin.DoAction(B, a.Id, Baskin.ActJoint, sh.X, sh.Y));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(0, B.HoldId, "tutuş boşa gitti");
            Assert.IsTrue(B.Log.Any(l => l.Text == Loc.T("Tutuş boşa gitti.")));
            Assert.IsTrue(Baskin.DoAction(B, b.Id, Baskin.ActJoint, sh.X, sh.Y));
            Assert.AreEqual("wall", Baskin.CellAt(B, sh.X, sh.Y).T, "ertesi tur açmaz, yeniden tutar");
            Assert.AreEqual(b.Id, B.HoldId);
        }

        [Test]
        public void Joint_HolderPulledAwayOrDown_Rehold([Values(false, true)] bool down)
        {
            var B = Middle();
            var sh = B.Room.Shutter.Value;
            var a = BySlot(B, 0); var b = BySlot(B, 1);
            Place(a, B.Room.ShutterDoors[0]); Place(b, B.Room.ShutterDoors[1]);
            Assert.IsTrue(Baskin.DoAction(B, a.Id, Baskin.ActJoint, sh.X, sh.Y));
            if (down) Down(B, a);
            else Place(a, Baskin.Reachable(B, b).First(t => Math.Max(Math.Abs(t.X - sh.X), Math.Abs(t.Y - sh.Y)) >= 2));
            Assert.IsTrue(Baskin.DoAction(B, b.Id, Baskin.ActJoint, sh.X, sh.Y));
            Assert.AreEqual("wall", Baskin.CellAt(B, sh.X, sh.Y).T, "tutuş bozuldu");
            Assert.AreEqual(b.Id, B.HoldId, "ikinci yeni tutuş olur");
        }

        [Test]
        public void Joint_EightNeighbours()
        {
            var B = Middle();
            var sh = B.Room.Shutter.Value;
            var u = BySlot(B, 0);
            for (int y = sh.Y - 2; y <= sh.Y + 2; y++)
                for (int x = sh.X - 2; x <= sh.X + 2; x++)
                {
                    if (!FreeAt(B, x, y) && !(u.X == x && u.Y == y)) continue;
                    Place(u, new Baskin.Pt(x, y));
                    bool near = Math.Max(Math.Abs(x - sh.X), Math.Abs(y - sh.Y)) == 1;
                    Assert.AreEqual(near, Baskin.ActionTargets(B, u, Baskin.ActJoint).Any(t => t.X == sh.X && t.Y == sh.Y), $"{x},{y}");
                }
            // Sekiz komşudan herhangi ikisindeki iki uzman kaldırır
            var side = WorkTiles(B, sh);
            Assert.GreaterOrEqual(side.Count, 2);
            var a = BySlot(B, 1); var b = BySlot(B, 2);
            Place(u, new Baskin.Pt(-5, -5));
            Place(a, side[0]); Place(b, side[1]);
            Assert.IsTrue(Baskin.DoAction(B, a.Id, Baskin.ActJoint, sh.X, sh.Y));
            Assert.IsTrue(Baskin.DoAction(B, b.Id, Baskin.ActJoint, sh.X, sh.Y));
            Assert.AreEqual("floor", Baskin.CellAt(B, sh.X, sh.Y).T);
        }

        [Test]
        public void Joint_Undo_HoldAndOpen()
        {
            var B = Middle();
            var sh = B.Room.Shutter.Value;
            var a = BySlot(B, 0); var b = BySlot(B, 1);
            Place(a, B.Room.ShutterDoors[0]); Place(b, B.Room.ShutterDoors[1]);
            AssertUndoRestores(B, s => Baskin.DoAction(s, a.Id, Baskin.ActJoint, sh.X, sh.Y), "tutuş");
            Assert.IsTrue(Baskin.DoAction(B, a.Id, Baskin.ActJoint, sh.X, sh.Y));
            AssertUndoRestores(B, s => Baskin.DoAction(s, b.Id, Baskin.ActJoint, sh.X, sh.Y), "açılış");
            Assert.AreEqual(a.Id, B.HoldId, "geri alma tutuşu getirir");
            Assert.AreEqual("wall", Baskin.CellAt(B, sh.X, sh.Y).T);
        }

        [Test]
        public void Shutter_And_Crate_NotDynamiteTarget()
        {
            var B = Middle();
            var sh = B.Room.Shutter.Value;
            var m = BySlot(B, 0);
            m.Type = "maden"; m.Charges = 2;
            Place(m, B.Room.ShutterDoors[0]);
            Assert.IsFalse(Baskin.Targets(B, m).Any(t => t.X == sh.X && t.Y == sh.Y), "kepenk dinamitle açılmaz");
            Baskin.CellAt(B, sh.X, sh.Y).Shutter = false;
            Assert.IsTrue(Baskin.Targets(B, m).Any(t => t.X == sh.X && t.Y == sh.Y), "sıradan duvar hedef");
            Baskin.CellAt(B, sh.X, sh.Y).Crate = true;
            Assert.IsFalse(Baskin.Targets(B, m).Any(t => t.X == sh.X && t.Y == sh.Y), "sandık nişi dinamitle açılmaz");
        }

        [Test]
        public void Joint_Crate_OpensAndReports()
        {
            var (p, reps, H) = CrateTarget();
            var cr = H.Room.BigCrate.Value;
            Assert.IsTrue(Baskin.CellAt(H, cr.X, cr.Y).Crate && Baskin.CellAt(H, cr.X, cr.Y).T == "wall");
            var side = WorkTiles(H, cr);
            Assert.GreaterOrEqual(side.Count, 2, H.Room.Id);
            var a = BySlot(H, 0); var b = BySlot(H, 1);
            Place(a, side[0]); Place(b, side[1]);
            Assert.IsTrue(Baskin.DoAction(H, a.Id, Baskin.ActJoint, cr.X, cr.Y));
            Assert.AreEqual(Loc.F("{0} büyük sandığı tutuyor: ikinci uzman bu tur gelmeli.", Baskin.UnitTypes[a.Type].Name), H.Log.Last().Text);
            var pv = Act(H, b, Baskin.ActJoint, cr);
            Assert.IsTrue(pv.Opens);
            Assert.IsTrue(H.CrateOpen);
            Assert.AreEqual("wall", Baskin.CellAt(H, cr.X, cr.Y).T, "niş duvar kalır");
            Assert.AreEqual(Loc.T("Büyük sandık açıldı."), H.Log.Last().Text);
            StringAssert.Contains("|B", Baskin.Signature(H));
            Assert.IsEmpty(Baskin.ActionTargets(H, BySlot(H, 2), Baskin.ActJoint).Where(t => t.X == cr.X && t.Y == cr.Y));
            StartEscape(H);
            var r = PassRoom(p, reps, H);
            Assert.IsTrue(r.Crate, "rapor");
            Assert.IsTrue(RoomReport.Decode(r.Encode()).Crate, "rapor dizgisi");
        }

        // ---------- Menfez ----------
        static Baskin.Unit Analyst(Baskin.BState B) => B.Units.First(u => u.Side == 'p' && u.Type == "ana");

        [Test]
        public void Vent_Teleports_KeepsAbility()
        {
            var B = Middle();
            var v = B.Room.Vents;
            var ana = Analyst(B);
            Place(ana, v[0]);
            CollectionAssert.AreEqual(new[] { (v[1].X, v[1].Y) }, Baskin.ActionTargets(B, ana, Baskin.ActVent).Select(t => (t.X, t.Y)));
            var pv = Act(B, ana, Baskin.ActVent, v[1]);
            Assert.AreEqual((v[1].X, v[1].Y), (ana.X, ana.Y));
            Assert.IsTrue(ana.Moved, "yürüyüş biter");
            Assert.IsFalse(ana.Acted, "yetenek durur");
            Assert.IsTrue(Baskin.CanAct(B, ana));
            Assert.AreEqual(1, pv.Moves.Count);
            var fx = B.FxList.Single(f => f.K == "vent");
            Assert.AreEqual((v[0].X, v[0].Y, v[1].X - v[0].X, v[1].Y - v[0].Y), (fx.X, fx.Y, fx.Dx, fx.Dy));
            Assert.IsFalse(Baskin.CanAction(B, ana, Baskin.ActVent), "bir kez");
        }

        [Test]
        public void Vent_AnalystOnly()
        {
            var B = Middle();
            var v = B.Room.Vents;
            var tek = BySlot(B, 0);
            Assert.AreNotEqual("ana", tek.Type);
            Place(tek, v[0]);
            Assert.IsEmpty(Baskin.ActionTargets(B, tek, Baskin.ActVent));
        }

        [Test]
        public void Vent_NeedsMoveLeft()
        {
            var B = Middle();
            var v = B.Room.Vents;
            var ana = Analyst(B);
            var n = Baskin.Dirs.Select(d => new Baskin.Pt(v[0].X + d[0], v[0].Y + d[1]))
                .First(t => FreeAt(B, t.X, t.Y) && Baskin.CellAt(B, t.X, t.Y).Vent == 0 && !Baskin.IsGateTile(B, t.X, t.Y));
            Place(ana, n);
            ana.MoveRange = 1;
            Assert.IsEmpty(Baskin.ActionTargets(B, ana, Baskin.ActVent), "uca varmak bütün yürüyüşü harcar");
            ana.MoveRange = 2;
            CollectionAssert.AreEqual(new[] { (v[1].X, v[1].Y) }, Baskin.ActionTargets(B, ana, Baskin.ActVent).Select(t => (t.X, t.Y)));
            Assert.IsTrue(Baskin.DoAction(B, ana.Id, Baskin.ActVent, v[1].X, v[1].Y));
            Assert.AreEqual((v[0].X, v[0].Y), (B.FxList.Single(f => f.K == "vent").X, B.FxList.Single(f => f.K == "vent").Y), "yakın uç");
        }

        [Test]
        public void Vent_FarEndOccupied_BodyDoesNot()
        {
            var B = Middle();
            var v = B.Room.Vents;
            var ana = Analyst(B);
            Place(ana, v[0]);
            var other = BySlot(B, 0);
            Place(other, v[1]);
            Assert.IsEmpty(Baskin.ActionTargets(B, ana, Baskin.ActVent), "öbür uçta ayakta birim");
            Down(B, other);
            Assert.IsNotEmpty(Baskin.ActionTargets(B, ana, Baskin.ActVent), "gövde kapatmaz");
        }

        [Test]
        public void Vent_DoMoveDoesNotTeleport_PicksCargo()
        {
            var B = Middle();
            var v = B.Room.Vents;
            var ana = Analyst(B);
            var n = Baskin.Dirs.Select(d => new Baskin.Pt(v[0].X + d[0], v[0].Y + d[1])).First(t => FreeAt(B, t.X, t.Y) && Baskin.CellAt(B, t.X, t.Y).Vent == 0);
            Place(ana, n);
            Assert.IsTrue(Baskin.DoMove(B, ana.Id, v[0].X, v[0].Y));
            Assert.AreEqual((v[0].X, v[0].Y), (ana.X, ana.Y), "yürüyüş menfezde durur");
            Assert.IsFalse(Baskin.CanAction(B, ana, Baskin.ActVent), "yürüdükten sonra menfez yok");
            Assert.IsTrue(Baskin.Undo(B));
            ana = Analyst(B);
            Place(ana, v[0]);
            B.CargoX = v[1].X; B.CargoY = v[1].Y;
            Assert.IsTrue(Baskin.DoAction(B, ana.Id, Baskin.ActVent, v[1].X, v[1].Y));
            Assert.AreEqual(ana.Id, B.Carrier, "menfez çıkışı yükü alır");
        }

        [Test]
        public void Vent_Undo()
        {
            var B = Middle();
            var v = B.Room.Vents;
            var ana = Analyst(B);
            Place(ana, v[0]);
            AssertUndoRestores(B, b => Baskin.DoAction(b, ana.Id, Baskin.ActVent, v[1].X, v[1].Y), "menfez");
        }

        // ---------- Büyük sandık: plan ----------
        [Test]
        public void BigCrate_OnlyLootTarget_WallWithTwoWorkTiles()
        {
            var rooms = Plan79.Rooms;
            var lootTargets = rooms.Where(r => r.Type == RoomType.Target && r.Mission == "loot").ToList();
            Assert.IsTrue(rooms.Where(r => r.BigCrate != null).All(r => r.Type == RoomType.Target && r.Mission == "loot"), "yalnız ikmal hedef odası");
            int with = lootTargets.Count(r => r.BigCrate != null);
            Assert.GreaterOrEqual(with * 4, lootTargets.Count * 3, $"ikmal hedef odalarının çoğunda yer var: {with}/{lootTargets.Count}");
            foreach (var r in lootTargets.Where(r => r.BigCrate != null))
            {
                var c = r.BigCrate.Value;
                foreach (bool events in new[] { false, true })
                    foreach (int tier in new[] { 1, 3 })
                    {
                        var cells = r.PlanCells(tier, events);
                        Assert.IsTrue(cells[c.Y, c.X].T == "wall" && cells[c.Y, c.X].Crate && !cells[c.Y, c.X].Shutter, r.Id);
                        int work = 0;
                        for (int y = c.Y - 1; y <= c.Y + 1; y++)
                            for (int x = c.X - 1; x <= c.X + 1; x++)
                                if ((x != c.X || y != c.Y) && x >= 0 && y >= 0 && x < Baskin.N && y < Baskin.N && cells[y, x].T != "wall" && cells[y, x].O == null && cells[y, x].Door == 0)
                                    work++;
                        Assert.GreaterOrEqual(work, 2, r.Id + ": iş karesi");
                        int crates = 0;
                        for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (cells[y, x].Crate) crates++;
                        Assert.AreEqual(1, crates, r.Id);
                    }
            }
            foreach (var r in rooms.Where(r => r.BigCrate == null))
            {
                var cells = r.PlanCells(1, false);
                for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) Assert.IsFalse(cells[y, x].Crate, r.Id);
            }
        }

        /// <summary>Sandık yeri önbellekteki ve taze üretilen planda aynı (yerleşim plandan türer, saklı durum yok); nişin karesi plan
        /// satırında duvar kalır. RNG çekilmediğini <c>FacilityMapsTests.Golden_Seed79</c> özeti kilitler.</summary>
        [Test]
        public void BigCrate_SameInCachedAndFreshPlan()
        {
            var fresh = new FacilityPlan(79).Rooms;
            var cached = Plan79.Rooms;
            CollectionAssert.AreEqual(cached.Select(r => r.BigCrate.HasValue ? $"{r.BigCrate.Value.X}{r.BigCrate.Value.Y}" : "-"),
                fresh.Select(r => r.BigCrate.HasValue ? $"{r.BigCrate.Value.X}{r.BigCrate.Value.Y}" : "-"));
            // Oda kurulumu sandıktan etkilenmez: nişin karesi duvardı, satırlar aynı (FacilityMapsTests.Golden_Seed79 özeti kilitler)
            foreach (var r in cached.Where(r => r.BigCrate != null)) Assert.AreEqual('#', r.Rows[r.BigCrate.Value.Y][r.BigCrate.Value.X], r.Id);
        }

        /// <summary>Ödül: kazanılan ikmal baskınında, sandık açıldıysa kartlı yolda 2 metal ve 1 devre; açılmadıysa yok.</summary>
        [Test]
        public void BigCrate_Reward_OnlyWhenOpenedAndWon()
        {
            for (uint seed = 79; seed < 110; seed++)
            {
                var probe = Sim(seed);
                if (probe.S.Raid.Params.MissionKind != "loot") continue;
                var plan = FacilityMaps.For(seed);
                var h = plan.Room(probe.S.Raid.Params.Rooms[2]);
                if (h?.BigCrate == null) continue;
                var results = new List<(bool Big, int Metal, int Circuits, bool Ok)>();
                foreach (bool open in new[] { false, true })
                {
                    var sim = Sim(seed);
                    int metal0 = sim.Board.Material("metal")?.Quantity ?? 0, dev0 = sim.Board.Material("devre")?.Quantity ?? 0;
                    for (int guard = 0; guard < 6 && !sim.RaidRoomsClosed; guard++)
                    {
                        var B = sim.OpenRaidRoom();
                        if (open && B.Room.Type == RoomType.Target && !B.Escape)
                        {
                            Calm(B);
                            var cr = B.Room.BigCrate.Value;
                            var side = WorkTiles(B, cr);
                            Assert.GreaterOrEqual(side.Count, 2, B.Room.Id);
                            var team = Standing(B);
                            Place(team[0], side[0]); Place(team[1], side[1]);
                            Assert.IsTrue(Baskin.DoAction(B, team[0].Id, Baskin.ActJoint, cr.X, cr.Y));
                            Assert.IsTrue(Baskin.DoAction(B, team[1].Id, Baskin.ActJoint, cr.X, cr.Y));
                            Assert.IsTrue(B.CrateOpen);
                        }
                        PlayRoom(B);
                        var ok = sim.AdvanceRaidRoom(sim.RaidRoomReport(B));
                        Assert.IsTrue(ok.Ok, ok.Msg);
                    }
                    var res = sim.FacilityRaidResult();
                    Assert.IsTrue(sim.ResolveRaid(res).Ok);
                    var rw = sim.S.RaidLog.Last().LiberationReward;
                    results.Add((rw.BigCrate, sim.Board.Material("metal").Quantity - metal0, sim.Board.Material("devre").Quantity - dev0, res.Success));
                }
                // İki baskın da kazanılmalı (ödül yalnız zaferde); test yardımcısının botu bu tohumda kaybettiyse sonraki tohum (sürüm 10)
                if (!(results[0].Ok && results[1].Ok)) continue;
                Assert.IsFalse(results[0].Big);
                Assert.IsTrue(results[1].Big);
                Assert.AreEqual(new Balance().facilityBigCrateMetal, results[1].Metal - results[0].Metal);
                Assert.AreEqual(new Balance().facilityBigCrateCircuits, results[1].Circuits - results[0].Circuits);
                return;
            }
            Assert.Fail("büyük sandıklı ikmal baskını bulunamadı");
        }

        /// <summary>Kaybedilen baskında büyük sandık açılmış olsa da ödül yoktur (kazanılan aynı raporlarla verilir). Kartsız yol:
        /// <see cref="FacilityFallenTests.Death_CardlessPath_Unchanged"/>.</summary>
        [Test]
        public void BigCrate_Reward_NotOnLostRaid()
        {
            for (uint seed = 79; seed < 110; seed++)
            {
                if (Sim(seed).S.Raid.Params.MissionKind != "loot") continue;
                var got = new List<(bool Big, int Metal, int Circuits)>();
                foreach (bool won in new[] { true, false })
                {
                    var sim = Sim(seed);
                    int n = sim.S.Raid.Params.Squad.Count;
                    int metal0 = sim.Board.Material("metal")?.Quantity ?? 0, dev0 = sim.Board.Material("devre")?.Quantity ?? 0;
                    Assert.IsTrue(sim.AdvanceRaidRoom(Rep(1, squad: n)).Ok);
                    Assert.IsTrue(sim.AdvanceRaidRoom(Rep(2, squad: n)).Ok);
                    var last = Rep(3, squad: n, end: won ? "won" : "lost");
                    last.Last = true; last.Success = won; last.Cargo = won ? 0 : -2; last.Crate = true;
                    Assert.IsTrue(sim.AdvanceRaidRoom(last).Ok);
                    var res = sim.FacilityRaidResult();
                    Assert.AreEqual(won, res.Success);
                    Assert.IsTrue(sim.ResolveRaid(res).Ok);
                    var rw = sim.S.RaidLog.Last().LiberationReward;
                    got.Add((rw.BigCrate, (sim.Board.Material("metal")?.Quantity ?? 0) - metal0, (sim.Board.Material("devre")?.Quantity ?? 0) - dev0));
                }
                Assert.IsTrue(got[0].Big, "kazanılan: ödül (sınamanın yolu doğru)");
                Assert.IsFalse(got[1].Big, "kaybedilen: ödül yok");
                Assert.AreEqual((0, 0), (got[1].Metal, got[1].Circuits), "kaybedilen baskında malzeme yok");
                return;
            }
            Assert.Fail("ikmal baskını bulunamadı");
        }

        // ---------- Önizleme refaktörü ----------
        static readonly MethodInfo ApplyAbilityM = typeof(Baskin).GetMethod("ApplyAbility", BindingFlags.NonPublic | BindingFlags.Static);
        static readonly MethodInfo CheckEscapeM = typeof(Baskin).GetMethod("CheckEscape", BindingFlags.NonPublic | BindingFlags.Static);

        /// <summary>Aşama 6 öncesi <see cref="Baskin.Preview"/>'in gövdesi (Diff'e taşınmadan önceki kod, satır satır); taşıyıcı
        /// sonradan eklenen alandır ve aynı kopyadan yazılır.</summary>
        static Baskin.PreviewResult OldPreview(Baskin.BState B, int uid, int x, int y)
        {
            var C = B.Clone();
            C.FxList.Clear();
            var u = C.Units.FirstOrDefault(v => v.Id == uid);
            ApplyAbilityM.Invoke(null, new object[] { C, u, x, y });
            CheckEscapeM.Invoke(null, new object[] { C });
            var moves = new List<(int, Baskin.Pt, Baskin.Pt)>();
            foreach (var cu in C.Units)
            {
                var ou = B.Units.FirstOrDefault(v => v.Id == cu.Id);
                if (ou != null && (ou.X != cu.X || ou.Y != cu.Y)) moves.Add((cu.Id, new Baskin.Pt(ou.X, ou.Y), new Baskin.Pt(cu.X, cu.Y)));
            }
            return new Baskin.PreviewResult { Fx = C.FxList, Moves = moves, Collateral = C.Collateral - B.Collateral, Won = C.Phase == "won", Escape = C.Escape && !B.Escape, Intents = Baskin.Intents(C), Hazards = Baskin.Hazards(C), Carrier = C.Carrier };
        }

        /// <summary>Preview → Diff ayrımı birebir: tesis odalarında (giriş ve şartlı ara oda, ikmal ve sunucu) her uzmanın
        /// her yetenek hedefi için önizleme, eski gövdenin sonucuyla tam dökümde aynı; asıl durum değişmez.</summary>
        [Test]
        public void Preview_Unchanged()
        {
            // Tek kip: yalnız tesis odaları. Her bölgeden bir kanadın giriş odası (şartsız) ve ara odası (bölgenin şartıyla; elle giriş
            // raporu); ikmal ve sunucu karışık
            var boards = new List<Baskin.BState>();
            for (int w = 0; w < Wings.Count; w += 3)
            {
                var p = Params(Wings[w], 41 + w, 1, true);
                boards.Add(Open(p, new List<RoomReport>(), Bal()));
                boards.Add(Open(p, new List<RoomReport> { Rep(1) }, Bal()));
            }
            boards.Add(LootTarget());
            int tested = 0;
            foreach (var B in boards)
            {
                string dump = FullDump(B);
                foreach (var u in Baskin.Alive(B, 'p').Where(v => Baskin.CanAct(B, v)))
                    foreach (var t in Baskin.Targets(B, u))
                    {
                        var now = Baskin.Preview(B, u.Id, t.X, t.Y);
                        var old = OldPreview(B, u.Id, t.X, t.Y);
                        Assert.AreEqual(DumpAny(old), DumpAny(now), $"{B.Room?.Id ?? B.Layout} {u.Type} {t.X},{t.Y}");
                        tested++;
                    }
                Assert.AreEqual(dump, FullDump(B), "önizleme durumu değiştirmez");
            }
            Assert.Greater(tested, 60);
        }
    }
}
