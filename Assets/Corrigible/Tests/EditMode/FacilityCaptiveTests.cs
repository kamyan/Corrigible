using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 8, parça C (Docs/TESIS-BASKINLARI-TASARIMI.md §5.4; uygulama tasarımı §5.7, K12, K13, K27): kumanda kelepçesi ve
    /// tutsak. Kelepçe bitişik makineyi ele geçirir ve tükenir; tutsak yakalandığı tur oynamaz, sonraki üç oyuncu turunda yürür ve kendi
    /// eylemini (komuta) yapar, sonra yeniden makine olur ve bir planı niyetsiz geçirir. Tutsak makine sınırına sayılır, sonuç ve
    /// makine listelerine girmez, ekiple geçer. Kullanılan kelepçe baskından sonra panodan silinir.
    /// </summary>
    public class FacilityCaptiveTests
    {
        static Balance B0 => Bal(events: false);

        static Baskin.Pt Pt(int x, int y) => new Baskin.Pt(x, y);

        /// <summary>Kelepçeli G (düz zemin, makinesiz): Teknisyen (3, 3) kelepçeli, öbür uzmanlar alt köşede.</summary>
        static (Baskin.BState B, Baskin.Unit Tek) FlatClamp(int seed = 41)
        {
            var p = WithGear(Params(Wings[0], seed, 1, false), 0, FacilityGear.Clamp);
            var B = Open(p, new List<RoomReport>(), B0);
            Flat(B, Pt(3, 3), Pt(0, 7), Pt(1, 7));
            var tek = BySlot(B, 0);
            Assert.AreEqual(FacilityGear.Clamp, tek.Gear);
            Assert.AreEqual(1, tek.GearLeft);
            return (B, tek);
        }

        /// <summary>Düz zeminde tutsak (türü <paramref name="type"/>, (3, 3)); tek uzman alt köşede.</summary>
        static (Baskin.BState B, Baskin.Unit C) FlatCaptive(string type)
        {
            var B = Open(Params(Wings[0], 41, 1, false), new List<RoomReport>(), B0);
            Flat(B, Pt(0, 7));
            var c = Captive(AddMachine(B, type, Pt(3, 3)));
            return (B, c);
        }

        static List<Baskin.Pt> CommandTargets(Baskin.BState B, Baskin.Unit c) => Baskin.ActionTargets(B, c, Baskin.ActCommand);

        // ---------- Kelepçe ----------
        [Test]
        public void Clamp_AdjacentMachineOnly_NeedsGearAndCharge()
        {
            var (B, tek) = FlatClamp();
            var near = AddMachine(B, "bakim", Pt(4, 3));
            AddMachine(B, "dron", Pt(5, 3));
            AddMachine(B, "dron", Pt(4, 4));
            CollectionAssert.AreEquivalent(new[] { (4, 3) }, Baskin.ActionTargets(B, tek, Baskin.ActClamp).Select(t => (t.X, t.Y)), "yalnız dik komşu makine");
            near.Dead = true;
            Assert.IsEmpty(Baskin.ActionTargets(B, tek, Baskin.ActClamp), "düşen makine değil");
            near.Dead = false;
            tek.GearLeft = 0;
            Assert.IsFalse(Baskin.CanAction(B, tek, Baskin.ActClamp), "hak bitti");
            tek.GearLeft = 1;
            var emp = BySlot(B, 1);
            Place(emp, Pt(4, 2));
            Assert.IsFalse(Baskin.CanAction(B, emp, Baskin.ActClamp), "kelepçesiz uzman veremez");
        }

        [Test]
        public void Clamp_Captures_Consumed_NoActOnCaptureTurn_Undo()
        {
            var (B, tek) = FlatClamp();
            var e = AddMachine(B, "dron", Pt(4, 3));
            e.Intent = new Baskin.Intent { Kind = "shoot", Dir = 0 };
            e.Order = 1;
            AssertUndoRestores(B, s => Baskin.DoAction(s, tek.Id, Baskin.ActClamp, 4, 3), "kelepçe");
            ActPreviewed(B, tek, Baskin.ActClamp, Pt(4, 3));
            tek = Fresh(B, tek);
            e = Fresh(B, e);
            Assert.AreEqual('p', e.Side);
            Assert.IsTrue(Baskin.IsCaptive(e));
            Assert.IsFalse(Baskin.IsSpecialist(e));
            Assert.AreEqual(0, e.Owner);
            Assert.AreEqual(B0.facilityClampTurns, e.Turns);
            Assert.IsNull(e.Intent, "niyeti silinir");
            Assert.AreEqual(0, tek.GearLeft, "kelepçe tükenir");
            Assert.IsFalse(Baskin.CanMove(B, e), "yakalandığı tur yürümez");
            Assert.IsFalse(Baskin.CanAction(B, e, Baskin.ActCommand), "yakalandığı tur eylem yok");
            Assert.IsTrue(B.FxList.Any(f => f.K == "capture" && f.Unit == e.Id && f.N == tek.Id));
            Assert.IsEmpty(Baskin.Intents(B).Where(i => i.Unit == e.Id), "tutsağın niyeti görünmez");
        }

        /// <summary>K12: tutsak sonraki <c>facilityClampTurns</c> oyuncu turunda oynar; sonra bırakılır, bir planı niyetsiz geçirir.</summary>
        [Test]
        public void Captive_PlaysClampTurns_ThenIdleOnePhase()
        {
            var (B, tek) = FlatClamp();
            var e = AddMachine(B, "bakim", Pt(4, 3));
            Assert.IsTrue(Baskin.DoAction(B, tek.Id, Baskin.ActClamp, 4, 3));
            Place(tek, Pt(0, 0));
            for (int turn = 1; turn <= B0.facilityClampTurns; turn++)
            {
                Assert.IsTrue(Baskin.EndTurn(B), "tur " + turn);
                Assert.IsTrue(Baskin.IsCaptive(e), "tur " + turn + ": tutsak");
                Assert.IsTrue(Baskin.CanMove(B, e), "tur " + turn + ": yürür");
                Assert.AreEqual(B0.facilityClampTurns - turn, e.Turns);
            }
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual('e', e.Side, "bırakıldı");
            Assert.IsNull(e.Class);
            Assert.AreEqual(-1, e.Owner);
            Assert.IsNull(e.Intent, "bu planda niyetsiz");
            Assert.IsFalse(e.Idle, "plan bayrağı tüketti");
            Assert.IsTrue(B.FxList.Any(f => f.K == "release" && f.Unit == e.Id));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsNotNull(e.Intent, "sonraki planda yine niyetli");
        }

        [Test]
        public void Captive_Moves_TypeMove_NeverEntersGate()
        {
            var p = Params(Wings[0], 41, 1, false);
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            Calm(B);
            var gate = B.Room.Forward[0];
            var at = FreeNeighbor(B, gate).Value;
            var c = Captive(AddMachine(B, "dron", at));
            Assert.IsTrue(Baskin.Reachable(B, c).All(t => System.Math.Abs(t.X - at.X) + System.Math.Abs(t.Y - at.Y) <= Baskin.UnitTypes["dron"].Move));
            Assert.IsTrue(Baskin.DoMove(B, c.Id, gate.X, gate.Y));
            Assert.IsFalse(c.AtGate, "tutsak kapıya girmez");
            Assert.IsTrue(Baskin.OnBoard(c));
            Assert.IsFalse(Baskin.EnterGate(B, c.Id));
            ToGate(B);
            Assert.IsTrue(Baskin.CanPass(B), "tutsak nerede olursa olsun ekiple geçer");
        }

        // ---------- Komuta (K13) ----------
        [Test]
        public void Captive_Command_PerType()
        {
            // Dron ve zırhlı dron: atışın vurduğu ilk makine (tel geçer), 1 hasar
            foreach (var type in new[] { "dron", "zdron" })
            {
                var (B, c) = FlatCaptive(type);
                var m = AddMachine(B, "bakim", Pt(3, 6));
                Baskin.CellAt(B, 3, 5).O = new Baskin.Obj { K = "tel", Hp = 3, Max = 3 };
                Assert.IsTrue(Has(CommandTargets(B, c), Pt(3, 6)), type + ": tel atışı geçirir");
                AssertUndoRestores(B, s => Baskin.DoAction(s, c.Id, Baskin.ActCommand, 3, 6), type);
                ActPreviewed(B, c, Baskin.ActCommand, Pt(3, 6));
                Assert.AreEqual(2, Fresh(B, m).Hp, type);
                Assert.AreEqual(3, Baskin.CellAt(B, 3, 5).O.Hp, "tel hasar almaz");
            }
            // Bakım robotu: dik komşu makineye 2 hasar; plakasız dik komşu uzmana plaka
            {
                var (B, c) = FlatCaptive("bakim");
                var m = AddMachine(B, "dron", Pt(4, 3), 3);
                var tek = BySlot(B, 0);
                Place(tek, Pt(2, 3));
                tek.Armor = 0;
                var t = CommandTargets(B, c);
                Assert.IsTrue(Has(t, Pt(4, 3)) && Has(t, Pt(2, 3)));
                Assert.IsTrue(Baskin.DoAction(B, c.Id, Baskin.ActCommand, 4, 3));
                Assert.AreEqual(1, Fresh(B, m).Hp, "bakım: 2 hasar");
                Assert.IsTrue(Baskin.Undo(B));
                ActPreviewed(B, c, Baskin.ActCommand, Pt(2, 3));
                Assert.AreEqual(1, BySlot(B, 0).Armor, "bakım: plaka");
                Assert.IsTrue(Baskin.Undo(B));
                BySlot(B, 0).Armor = 1;
                Assert.IsFalse(Has(CommandTargets(B, Fresh(B, c)), Pt(2, 3)), "plakalı uzman hedef değil");
            }
            // Havan: üç kare öte, engellerin üstünden
            {
                var (B, c) = FlatCaptive("havan");
                var m = AddMachine(B, "dron", Pt(6, 3));
                Baskin.CellAt(B, 4, 3).T = "wall";
                CollectionAssert.AreEqual(new[] { (6, 3) }, CommandTargets(B, c).Select(x => (x.X, x.Y)));
                ActPreviewed(B, c, Baskin.ActCommand, Pt(6, 3));
                Assert.AreEqual(2, Fresh(B, m).Hp);
            }
            // Patlayan robot: kendi karesi; çevresine 1 hasar, kendisi düşer; çevresinde ekip birimi varsa verilmez
            {
                var (B, c) = FlatCaptive("bomba");
                var m = AddMachine(B, "dron", Pt(3, 4));
                CollectionAssert.AreEqual(new[] { (3, 3) }, CommandTargets(B, c).Select(x => (x.X, x.Y)));
                ActPreviewed(B, c, Baskin.ActCommand, Pt(3, 3));
                Assert.AreEqual(2, Fresh(B, m).Hp);
                Assert.IsTrue(Fresh(B, c).Dead, "patlayan tutsak düşer");
                Assert.IsTrue(Baskin.Undo(B));
                Assert.IsFalse(Fresh(B, c).Dead);
                Assert.IsNotEmpty(CommandTargets(B, Fresh(B, c)));
                Place(BySlot(B, 0), Pt(2, 3));
                Assert.IsEmpty(CommandTargets(B, Fresh(B, c)), "çevresindeki ekip birimini vurmaz");
            }
            // Kilitleyici: düz hatta dört kare içindeki ilk makine bir sonraki planında niyetsiz
            {
                var (B, c) = FlatCaptive("kilit");
                Place(c, Pt(2, 3));
                var m = AddMachine(B, "dron", Pt(2, 7));
                var far = AddMachine(B, "dron", Pt(7, 3));
                CollectionAssert.AreEqual(new[] { (2, 7) }, CommandTargets(B, c).Select(x => (x.X, x.Y)), "dört kare; beşinci değil");
                AssertUndoRestores(B, s => Baskin.DoAction(s, c.Id, Baskin.ActCommand, 2, 7), "kilit");
                ActPreviewed(B, c, Baskin.ActCommand, Pt(2, 7));
                m = Fresh(B, m);
                far = Fresh(B, far);
                Assert.IsTrue(m.Idle);
                Assert.IsFalse(far.Idle);
                Assert.IsTrue(Baskin.EndTurn(B));
                Assert.IsNull(m.Intent, "kilitlenen makinenin planı yok");
                Assert.IsNotNull(far.Intent);
            }
            // Kalkan dronu: plakasız dik komşu uzmana plaka; yoksa dron gibi
            {
                var (B, c) = FlatCaptive("kalkan");
                var m = AddMachine(B, "bakim", Pt(6, 3));
                var tek = BySlot(B, 0);
                Place(tek, Pt(3, 2));
                tek.Armor = 0;
                CollectionAssert.AreEqual(new[] { (3, 2) }, CommandTargets(B, c).Select(x => (x.X, x.Y)), "önce plaka");
                ActPreviewed(B, c, Baskin.ActCommand, Pt(3, 2));
                Assert.AreEqual(1, BySlot(B, 0).Armor);
                Assert.IsTrue(Baskin.Undo(B));
                BySlot(B, 0).Armor = 1;
                CollectionAssert.AreEqual(new[] { (6, 3) }, CommandTargets(B, Fresh(B, c)).Select(x => (x.X, x.Y)), "plakalı uzman: ateş");
                Assert.IsTrue(Baskin.DoAction(B, c.Id, Baskin.ActCommand, 6, 3));
                Assert.AreEqual(2, Fresh(B, m).Hp);
            }
            // Çoğaltıcı: kendi eylemi yok
            {
                var (B, c) = FlatCaptive("cogalt");
                AddMachine(B, "dron", Pt(4, 3));
                Assert.IsEmpty(CommandTargets(B, c));
            }
        }

        [Test]
        public void Captive_NeverTargetsTeam_ShotStopsAtTeam()
        {
            var (B, c) = FlatCaptive("dron");
            var tek = BySlot(B, 0);
            Place(tek, Pt(3, 5));
            AddMachine(B, "bakim", Pt(3, 6));
            AddDog(B, 0, Pt(5, 3));
            AddMachine(B, "bakim", Pt(6, 3));
            Assert.IsEmpty(CommandTargets(B, c), "ekip birimi hattı keser, hedef verilmez");
            Assert.IsFalse(Baskin.CanAction(B, c, Baskin.ActWire), "tutsak ekipman eylemi veremez");
            Assert.IsFalse(Baskin.CanAction(B, c, Baskin.ActBark));
            Assert.IsEmpty(Baskin.Targets(B, c), "makinenin oyuncu yeteneği yok");
        }

        // ---------- Sayım, sonuç, plan ----------
        [Test]
        public void Captive_CountsToMaxEnemies_NotInMachinesReport()
        {
            var (B, c) = FlatCaptive("dron");
            var clone = AddMachine(B, "cogalt", Pt(0, 0));
            for (int i = 0; i < 4; i++) AddMachine(B, "dron", Pt(7, i));
            int right = DirOf(new[] { 1, 0 });
            Assert.AreEqual(7, Baskin.TestScore(B, clone, "clone", right), 1e-9, "6 makine: çoğaltır");
            AddMachine(B, "dron", Pt(7, 5));
            Assert.AreEqual(-1, Baskin.TestScore(B, clone, "clone", right), 1e-9, "tutsak dahil 7: makine sınırı");
            c.Dead = true;
            Assert.AreEqual(7, Baskin.TestScore(B, clone, "clone", right), 1e-9, "düşen tutsak sayılmaz");
        }

        /// <summary>Tutsak ve bırakılan makine planlanmaz: RNG çekimi düşen makineyle aynı.</summary>
        [Test]
        public void Captive_AndIdle_PlanSkips_NoRngForThem()
        {
            int Rs(int mode)
            {
                var B = Open(Params(Wings[0], 41, 1, false), new List<RoomReport>(), B0);
                Flat(B, Pt(0, 7));
                AddMachine(B, "dron", Pt(6, 6));
                var x = AddMachine(B, "dron", Pt(3, 3));
                if (mode == 1) x.Dead = true;
                if (mode == 2) Captive(x);
                if (mode == 3) x.Idle = true;
                Assert.IsTrue(Baskin.EndTurn(B));
                return B.Rs;
            }
            int dead = Rs(1);
            Assert.AreNotEqual(Rs(0), dead, "planlanan makine çeker");
            Assert.AreEqual(dead, Rs(2), "tutsak planlanmaz");
            Assert.AreEqual(dead, Rs(3), "niyetsiz makine planlanmaz");
        }

        // ---------- Geçiş ----------
        [Test]
        public void Captive_PassesWithTeam_TurnsCarry_ReleasedLaterIsEnemy()
        {
            var p = Params(Wings.First(w => w[0].Mission != "loot"), 41, 1, false);
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            ClearGate(B);
            var at = FreeNeighbor(B, B.Room.Forward[0]).Value;
            var c = Captive(AddMachine(B, "bakim", at, 2), owner: 1, turns: 1);
            var r = PassRoom(p, reps, B);
            var ru = r.Units.Single(u => u.Class == Baskin.ClassCaptive);
            Assert.AreEqual(("bakim", 1, 1, "a"), (ru.Type, ru.Owner, ru.Turns, ru.State));
            Assert.AreEqual(B.Units.Count(u => u.Side == 'e' && !u.Dead && !u.Out), r.Machines.Count, "tutsak makine listesinde yok");

            var A = Open(p, reps, B0);
            var ca = A.Units.Single(Baskin.IsCaptive);
            Assert.AreEqual(("bakim", 1, 1, 2), (ca.Type, ca.Owner, ca.Turns, ca.Hp), "kalan tur ve can taşınır");
            Assert.AreEqual('p', ca.Side);
            Assert.IsTrue(Baskin.CanMove(A, ca), "yeni odada oynar");
            Assert.Greater(A.Units.IndexOf(ca), A.Units.FindLastIndex(Baskin.IsSpecialist), "kadrodan sonra");
            Calm(A);
            Assert.IsTrue(Baskin.EndTurn(A));
            Assert.IsTrue(Baskin.IsCaptive(ca), "bir tur daha");
            Assert.IsTrue(Baskin.EndTurn(A));
            Assert.AreEqual('e', ca.Side, "bu odada bırakılan tutsak düşman");
            ToGate(A);
            Assert.IsTrue(Baskin.Pass(A));
            var ra = File(p, reps, A);
            Assert.IsTrue(ra.Machines.Any(m => m.Type == "bakim"), "bırakılan makine raporda makine");
            Assert.IsFalse(ra.Units.Any(u => u.Class == Baskin.ClassCaptive));
        }

        [Test]
        public void Captive_Dies_LikeMachine_NotCarried()
        {
            var p = Params(Wings[0], 41, 1, false);
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            ClearGate(B);
            var c = Captive(AddMachine(B, "dron", FreeNeighbor(B, B.Room.Forward[0]).Value, 1));
            Baskin.TestHurt(B, c, 1);
            Assert.IsTrue(c.Dead);
            Assert.IsTrue(B.FxList.Any(f => f.K == "die" && f.Unit == c.Id));
            var r = PassRoom(p, reps, B);
            Assert.IsEmpty(r.Units, "düşen tutsak rapora girmez");
        }

        /// <summary>Tesisin girişinde (son çıkış) tutsak tahtada kalır; son uzman çıkınca baskın biter.</summary>
        [Test]
        public void Captive_StaysAtFinalExit_LastSpecialistEnds()
        {
            var p = Params(Wings.First(w => w[0].Mission != "loot"), 41, 1, false);
            var reps = new List<RoomReport>();
            for (int room = 1; room <= 2; room++)
            {
                var g = Open(p, reps, B0);
                ClearGate(g);
                PassRoom(p, reps, g);
            }
            var H = Open(p, reps, B0);
            Calm(H);
            StartEscape(H);
            Captive(AddMachine(H, "dron", FreeNeighbor(H, H.Room.West[0]).Value), turns: 3);
            PassRoom(p, reps, H);
            var A = Open(p, reps, B0);
            Calm(A);
            Assert.IsTrue(A.Units.Any(Baskin.IsCaptive));
            PassRoom(p, reps, A);
            var G = Open(p, reps, B0);
            Assert.IsTrue(Baskin.IsFinalExit(G));
            Calm(G);
            var c = G.Units.Single(Baskin.IsCaptive);
            ToGate(G);
            Assert.AreEqual("won", G.Phase, "tutsak uzman değil");
            Assert.IsFalse(c.Out, "tutsak tesisten çıkarılamaz");
        }

        // ---------- Dönüş: kelepçenin tükenmesi ----------
        [Test]
        public void Clamp_SpentRemoved_EvenOnLoss_UnusedKept()
        {
            foreach (var (spent, win) in new[] { (true, false), (true, true), (false, true) })
            {
                string owner = null, clampId = null;
                var sim = Sim(before: s =>
                {
                    owner = s.PlanningSquad()[0].PersonId;
                    clampId = s.AddBoardEquipment(FacilityGear.Clamp, owner).Id;
                });
                Assert.AreEqual(FacilityGear.Clamp, sim.S.Raid.Params.Squad[0].Gear);
                Assert.AreEqual(1, sim.S.Raid.Params.Squad[0].GearCharges);
                int n = RaidChain.SquadCount(sim.S.Raid.Params);
                Assert.IsTrue(sim.AdvanceRaidRoom(Rep(1, squad: n)).Ok);
                Assert.IsTrue(sim.AdvanceRaidRoom(Rep(2, squad: n)).Ok);
                var last = Rep(3, squad: n, end: win ? "pass" : "lost");
                last.Last = true;
                last.Success = win;
                if (win && sim.S.Raid.Params.MissionKind == "loot") last.Cargo = 1;
                last.Team[0].Gear = spent ? 0 : 1;
                var adv = sim.AdvanceRaidRoom(last);
                Assert.IsTrue(adv.Ok, adv.Msg);
                var res = sim.ResolveRaid(null);
                Assert.IsTrue(res.Ok, res.Msg);
                Assert.AreEqual(win, sim.S.RaidLog.Last().Success);
                Assert.AreEqual(!spent, sim.Board.EquipmentItem(clampId) != null, spent ? "kullanılan kelepçe silinir" : "kullanılmayan kalır");
                Assert.AreEqual(spent, sim.S.RaidLog.Last().LiberationReward.Squad[0].ClampSpent);
                Assert.IsTrue(sim.Board.Person(owner).Alive);
            }
        }
    }
}
