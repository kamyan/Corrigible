using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 8, parça C (Docs/TESIS-BASKINLARI-TASARIMI.md §5.5; uygulama tasarımı §5.8–5.9, K14–K16, K30, K33): köpek. Odaya
    /// sahibiyle girer (kadro dizinleri kaymaz), yürür ve bir komut verir (havla, getir, kokla), uzmanın hamlelerini veremez, kapıya
    /// girmez ama ekiple geçer, tesisin girişinde tahliye olur, yükü taşır, düşerse tahtadan çekilir (gövde değildir) ve baskından sonra
    /// dinlenir. Her komutun önizlemesi uygulamayla aynı, geri alma tam durumu getirir.
    /// </summary>
    public class FacilityDogTests
    {
        static Balance B0 => Bal(events: false);

        static int LootWing => Wings.FindIndex(w => w[0].Mission == "loot");
        static int ServerWing => Wings.FindIndex(w => w[0].Mission != "loot");

        /// <summary>Kanadın olaysız baskın değerleri; <paramref name="owner"/>'ın yardımcı yuvasında köpek.</summary>
        static RaidParams P(int wing = 0, int seed = 41, int owner = 0, List<string> perks = null)
        {
            var p = WithGear(Params(Wings[wing], seed, 1, false), owner, FacilityGear.Dog);
            if (perks != null) p.Perks = perks;
            return p;
        }

        static Baskin.Unit DogOf(Baskin.BState B) => B.Units.FirstOrDefault(Baskin.IsDog);

        static Baskin.Pt Pt(int x, int y) => new Baskin.Pt(x, y);

        /// <summary>Köpekli G (düz zemin): uzmanlar alt sırada, köpek (3, 3).</summary>
        static (Baskin.BState B, Baskin.Unit Dog) FlatDog(int wing = 0)
        {
            var B = Open(P(wing), new List<RoomReport>(), B0);
            Flat(B, Pt(0, 7), Pt(1, 7), Pt(2, 7));
            var d = DogOf(B);
            Place(d, Pt(3, 3));
            return (B, d);
        }

        // ---------- Giriş ----------
        [Test]
        public void Dog_EntersWithOwner_SlotMinusOne_IndicesUnchanged()
        {
            var plain = Open(Params(Wings[0], 41, 1, false), new List<RoomReport>(), B0);
            Assert.IsFalse(plain.Units.Any(Baskin.IsDog), "köpeksiz kadroda köpek yok");
            for (int owner = 0; owner < 3; owner++)
            {
                var B = Open(P(owner: owner), new List<RoomReport>(), B0);
                var dogs = B.Units.Where(Baskin.IsDog).ToList();
                Assert.AreEqual(1, dogs.Count);
                var d = dogs[0];
                Assert.AreEqual(owner, d.Owner);
                Assert.AreEqual(-1, d.Slot, "köpek kadro dizinlerini kaydırmaz");
                Assert.AreEqual('p', d.Side);
                Assert.IsFalse(Baskin.IsSpecialist(d));
                Assert.AreEqual(B0.facilityDogHp, d.Hp);
                Assert.AreEqual(B0.facilityDogHp, d.Max);
                Assert.AreEqual(B0.facilityDogMove, d.MoveRange);
                Assert.AreSame(d, Baskin.UnitAt(B, d.X, d.Y), "köpek kendi karesinde");
                Assert.Greater(B.Units.IndexOf(d), B.Units.FindLastIndex(Baskin.IsSpecialist), "birim sırası: kadrodan sonra");
                CollectionAssert.AreEqual(new[] { 0, 1, 2 }, Standing(B).Select(u => u.Slot));
                CollectionAssert.AreEqual(Standing(plain).Select(u => (u.X, u.Y)), Standing(B).Select(u => (u.X, u.Y)), "uzmanların kareleri aynı");
                Assert.AreEqual(FacilityGear.Dog, BySlot(B, owner).Gear);
                Assert.AreEqual(-1, BySlot(B, owner).GearLeft);
            }
        }

        [Test]
        public void Dog_Fur_PlusOne()
        {
            var B = Open(P(perks: new List<string> { FacilityGear.Fur }), new List<RoomReport>(), B0);
            Assert.AreEqual(B0.facilityDogHp + B0.facilityFurHp, DogOf(B).Hp);
            Assert.AreEqual(B0.facilityDogHp + B0.facilityFurHp, DogOf(B).Max);
        }

        // ---------- Hamle ----------
        [Test]
        public void Dog_MovesFive_OneCommand_NoSpecialistActions()
        {
            var (B, d) = FlatDog();
            var reach = Baskin.Reachable(B, d);
            Assert.IsTrue(Has(reach, Pt(7, 2)), "beş adım (Manhattan 5) yürür");
            Assert.IsFalse(Has(reach, Pt(7, 1)), "altıncı adım yok");
            var tek = BySlot(B, 0);
            foreach (var k in Baskin.ActionKinds.Concat(Baskin.GearActionKinds).Append(Baskin.ActCommand))
                Assert.IsFalse(Baskin.CanAction(B, d, k), "köpek veremez: " + k);
            foreach (var k in new[] { Baskin.ActBark, Baskin.ActFetch, Baskin.ActSniff, Baskin.ActCommand })
                Assert.IsFalse(Baskin.CanAction(B, tek, k), "uzman veremez: " + k);
            Assert.IsEmpty(Baskin.Targets(B, d), "köpeğin yeteneği yok (DoAct)");
            Assert.IsTrue(Baskin.DoMove(B, d.Id, 5, 4));
            Assert.IsTrue(Baskin.CanAction(B, d, Baskin.ActBark), "yürüdükten sonra komut");
            ActPreviewed(B, d, Baskin.ActBark, Pt(5, 4));
            Assert.IsFalse(Baskin.CanMove(B, d), "komuttan sonra yürüyüş yok");
            Assert.IsFalse(Baskin.CanAction(B, d, Baskin.ActBark), "turda bir komut");
        }

        [Test]
        public void Dog_Bark_MachinesInRangeTargetDog_ClearedAfterPlan_Undo()
        {
            var (B, d) = FlatDog();
            var near = AddMachine(B, "dron", Pt(3, 0));
            var far = AddMachine(B, "dron", Pt(7, 3));
            int down = DirOf(new[] { 0, 1 });
            Assert.IsFalse(Baskin.TestTeamLike(B, near, d));
            Assert.AreEqual(3, Baskin.TestScore(B, near, "shoot", down), 1e-9, "havlamayan köpek düşük puan");
            AssertUndoRestores(B, s => Baskin.DoAction(s, d.Id, Baskin.ActBark, 3, 3), "havla");
            d = Fresh(B, d);
            near = Fresh(B, near);
            far = Fresh(B, far);
            Assert.IsFalse(d.Bark, "geri alma havlamayı siler");
            ActPreviewed(B, d, Baskin.ActBark, Pt(3, 3));
            Assert.IsTrue(d.Bark);
            Assert.IsTrue(B.FxList.Any(f => f.K == "bark" && f.Unit == d.Id));
            Assert.IsTrue(Baskin.TestTeamLike(B, near, d), "erimdeki makine ekip gibi");
            Assert.IsFalse(Baskin.TestTeamLike(B, far, d), "erim dışı (Manhattan 4) değil");
            Assert.AreEqual(10, Baskin.TestScore(B, near, "shoot", down), 1e-9, "havlayan köpek ekip gibi puanlanır");
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsFalse(B.Units.Any(u => u.Bark), "havlama yalnız o planı etkiler");
        }

        // ---------- Yük ----------
        [Test]
        public void Dog_Fetch_TakesCargo_NotByWalking_Undo()
        {
            var (B, d) = FlatDog(LootWing);
            B.CargoX = 4; B.CargoY = 3;
            Assert.IsTrue(Baskin.DoMove(B, d.Id, 4, 3));
            Assert.AreEqual(0, B.Carrier, "köpek yürüyerek yük almaz");
            Assert.AreEqual(4, B.CargoX);
            CollectionAssert.AreEqual(new[] { (4, 3) }, Baskin.ActionTargets(B, d, Baskin.ActFetch).Select(t => (t.X, t.Y)));
            AssertUndoRestores(B, s => Baskin.DoAction(s, d.Id, Baskin.ActFetch, 4, 3), "getir");
            ActPreviewed(B, d, Baskin.ActFetch, Pt(4, 3));
            d = Fresh(B, d);
            Assert.AreEqual(d.Id, B.Carrier);
            Assert.AreEqual(-1, B.CargoX);

            var (C, e) = FlatDog(LootWing);
            C.CargoX = 3; C.CargoY = 4;
            Assert.IsTrue(Has(Baskin.ActionTargets(C, e, Baskin.ActFetch), Pt(3, 4)), "dik komşudaki yük");
            C.CargoX = 3; C.CargoY = 5;
            Assert.IsEmpty(Baskin.ActionTargets(C, e, Baskin.ActFetch), "iki kare öte değil");
        }

        [Test]
        public void Throw_ToDog_DogDownDropsCargo()
        {
            var (B, d) = FlatDog(LootWing);
            var tek = BySlot(B, 0);
            Place(tek, Pt(1, 3));
            B.Carrier = tek.Id;
            Assert.IsTrue(Has(Baskin.ActionTargets(B, tek, Baskin.ActThrow), Pt(3, 3)), "köpek fırlatmanın alıcısı");
            ActPreviewed(B, tek, Baskin.ActThrow, Pt(3, 3));
            Assert.AreEqual(d.Id, B.Carrier);
            Baskin.TestHurt(B, d, 99);
            Assert.AreEqual(0, B.Carrier, "taşıyan köpek düşünce yük düşer");
            Assert.AreEqual((3, 3), (B.CargoX, B.CargoY));
        }

        /// <summary>Yükü taşıyan köpek odalar arasında taşır (rapor Cargo = sahibin sırası, CargoDog); tesisin girişinde tahliye olursa yük
        /// çıkar, tahtada kalırsa yük kaybedilir ama köpek yaşar.</summary>
        [Test]
        public void Dog_CarriesCargoAcrossRooms_ExitWins_LeftLoses()
        {
            foreach (bool exit in new[] { true, false })
            {
                var p = P(LootWing);
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
                H.Carrier = DogOf(H).Id;
                var rh = PassRoom(p, reps, H);
                Assert.AreEqual(0, rh.Cargo, "Cargo sahibin kadro sırası (K33)");
                Assert.IsTrue(rh.CargoDog);
                Assert.IsFalse(rh.Last, "dönüş odası kalmalı");
                var A = Open(p, reps, B0);
                Assert.IsTrue(A.Room.Index == 2 && A.Escape);
                Assert.AreEqual(DogOf(A).Id, A.Carrier, "dönüşte taşıyıcı köpek");
                Calm(A);
                var ra = PassRoom(p, reps, A);
                Assert.IsTrue(ra.CargoDog);
                var G = Open(p, reps, B0);
                Assert.IsTrue(Baskin.IsFinalExit(G));
                Calm(G);
                var dog = DogOf(G);
                Assert.AreEqual(dog.Id, G.Carrier);
                if (exit)
                {
                    var w = G.Room.West.First(t => Baskin.UnitAt(G, t.X, t.Y) == null && FreeNeighbor(G, t) != null);
                    Place(dog, FreeNeighbor(G, w).Value);
                    Assert.IsTrue(Baskin.DoMove(G, dog.Id, w.X, w.Y));
                    Assert.IsTrue(dog.Out, "köpek tesisin girişinde tahliye olur");
                    Assert.AreEqual("player", G.Phase, "köpek uzman değil: baskın sürer");
                }
                ToGate(G);
                Assert.AreEqual(exit ? "won" : "lost", G.Phase);
                if (!exit) StringAssert.Contains("Köpek", G.OutcomeReason);
                var rg = File(p, reps, G);
                Assert.IsTrue(rg.Last);
                Assert.AreEqual(exit, rg.Success);
                Assert.AreEqual(exit ? 0 : -2, rg.Cargo);
                Assert.AreEqual(exit, rg.CargoDog);
                var u = rg.Units.Single(x => x.Class == Baskin.ClassDog);
                Assert.AreEqual("a", u.State, "köpek yaşar");
            }
        }

        // ---------- Kokla ----------
        [Test]
        public void Dog_Sniff_ForwardGate_OncePerRoom_ReportSniff_Undo()
        {
            var p = P(ServerWing);
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            ClearGate(B);
            var d = DogOf(B);
            var gate = B.Room.Forward[0];
            Place(d, FreeNeighbor(B, gate).Value);
            Assert.IsTrue(Has(Baskin.ActionTargets(B, d, Baskin.ActSniff), gate));
            AssertUndoRestores(B, s => Baskin.DoAction(s, d.Id, Baskin.ActSniff, gate.X, gate.Y), "kokla");
            Assert.AreEqual(0, B.Sniffed, "geri alma koklamayı siler");
            ActPreviewed(B, d, Baskin.ActSniff, gate);
            d = Fresh(B, d);
            Assert.AreEqual(2, B.Sniffed, "G'de koklanan A");
            Calm(B);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsEmpty(Baskin.ActionTargets(B, d, Baskin.ActSniff), "oda başına bir");
            var r = PassRoom(p, reps, B);
            Assert.AreEqual(2, r.Sniff);
            Assert.AreEqual(2, RoomReport.Decode(r.Encode()).Sniff);

            var A = Open(p, reps, B0);
            var da = DogOf(A);
            Assert.IsNotNull(da, "köpek ekiple geçer");
            Place(da, FreeNeighbor(A, A.Room.Forward[0]).Value);
            Assert.IsTrue(Baskin.DoAction(A, da.Id, Baskin.ActSniff, A.Room.Forward[0].X, A.Room.Forward[0].Y));
            Assert.AreEqual(3, A.Sniffed, "A'da koklanan H");
            ClearGate(A);
            PassRoom(p, reps, A);

            var H = Open(p, reps, B0);
            Calm(H);
            Assert.AreEqual(0, H.Room.Forward.Count);
            Assert.IsEmpty(Baskin.ActionTargets(H, DogOf(H), Baskin.ActSniff), "hedef odada verilmez");

            var E = Open(p, new List<RoomReport>(), B0);
            Calm(E);
            var de = DogOf(E);
            Place(de, FreeNeighbor(E, E.Room.Forward[0]).Value);
            E.Escape = true;
            Assert.IsEmpty(Baskin.ActionTargets(E, de, Baskin.ActSniff), "kaçışta verilmez");
        }

        /// <summary>Rapordaki Sniff, AdvanceRaidRoom'da sıradaki odanın planını açar (teyp kalıbı); kayıttan devamda aynı Known.</summary>
        [Test]
        public void Dog_Sniff_LearnsNextRoom_OnAdvance_SameAfterReplay()
        {
            foreach (bool sniff in new[] { false, true })
            {
                var sim = Sim();
                var rooms = sim.S.Raid.Params.Rooms;
                int n = RaidChain.SquadCount(sim.S.Raid.Params);
                Assert.IsFalse(sim.FacilityKnown(rooms[1]), "A bilinmiyor");
                var r1 = Rep(1, squad: n);
                if (sniff) r1.Sniff = 2;
                Assert.IsTrue(sim.AdvanceRaidRoom(r1).Ok);
                Assert.IsTrue(sim.FacilityKnown(rooms[0]), "teyp");
                Assert.AreEqual(sniff, sim.FacilityKnown(rooms[1]), "kokla: sıradaki oda");
                Assert.IsFalse(sim.FacilityKnown(rooms[2]));
                sim.Record.day = sim.S.Day;
                var back = Replay.Restore(sim.Record);
                Assert.AreEqual(0, back.ReplayFailures);
                CollectionAssert.AreEqual(sim.Facility.Known, back.Facility.Known, "kayıttan devamda aynı Known");
            }
        }

        // ---------- Düşme, sahip, geçiş ----------
        [Test]
        public void Dog_Down_RemovedNotBody_CarriedAsDownToRaidEnd()
        {
            var p = P(ServerWing);
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            ClearGate(B);
            var d = DogOf(B);
            Baskin.TestHurt(B, d, 99);
            Assert.IsTrue(d.Dead);
            Assert.IsFalse(Baskin.IsBody(d), "köpek gövde değildir");
            Assert.IsFalse(Baskin.LyingBody(d));
            Assert.IsNull(Baskin.UnitAt(B, d.X, d.Y), "tahtadan çekilir");
            Assert.IsTrue(B.FxList.Any(f => f.K == "dog-down" && f.Unit == d.Id));
            Assert.AreEqual("player", B.Phase);
            var r = PassRoom(p, reps, B);
            var u = r.Units.Single();
            Assert.AreEqual((Baskin.ClassDog, "d", 0), (u.Class, u.State, u.Owner));
            var A = Open(p, reps, B0);
            Assert.IsFalse(A.Units.Any(Baskin.IsDog), "düşen köpek sonraki odaya konmaz");
            ClearGate(A);
            var ra = PassRoom(p, reps, A);
            Assert.AreEqual("d", ra.Units.Single().State, "baskın sonuna kadar taşınır");
            Assert.IsTrue(ra.Team.All(m => m.State == "a"), "kadro etkilenmez");
        }

        [Test]
        public void Dog_OwnerDown_DogPlays_PassesWithTeam_NotAtGate()
        {
            var p = P(ServerWing);
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            ClearGate(B);
            var d = DogOf(B);
            Down(B, BySlot(B, 0));
            Assert.IsTrue(Baskin.CanMove(B, d), "sahibi düşen köpek oynar");
            ToGate(B);
            Assert.IsTrue(Baskin.CanPass(B), "köpek nerede olursa olsun GEÇ verilir");
            var gate = B.Room.Forward[0];
            Place(d, FreeNeighbor(B, gate).Value);
            Assert.IsTrue(Baskin.DoMove(B, d.Id, gate.X, gate.Y));
            Assert.IsFalse(d.AtGate, "köpek kapıya girmez");
            Assert.IsTrue(Baskin.OnBoard(d));
            Assert.IsFalse(Baskin.EnterGate(B, d.Id));
            Baskin.TestHurt(B, d, 1);
            int hp = d.Hp;
            Assert.IsTrue(Baskin.Pass(B));
            var r = File(p, reps, B);
            Assert.AreEqual("a", r.Units.Single().State);
            var A = Open(p, reps, B0);
            var da = DogOf(A);
            Assert.IsNotNull(da, "ekiple geçti");
            Assert.AreEqual(hp, da.Hp, "can taşınır");
            Assert.AreEqual(0, da.Owner);
            Assert.Greater(A.Units.IndexOf(da), A.Units.FindLastIndex(u => Baskin.IsSpecialist(u) && !u.Dead), "ayaktakilerden sonra");
            Assert.AreSame(da, Baskin.UnitAt(A, da.X, da.Y));
        }

        // ---------- Dönüş: dinlenme ----------
        [Test]
        public void Dog_Down_RestsAfterRaid_StandingDoesNot()
        {
            foreach (bool down in new[] { true, false })
            {
                string owner = null, dogId = null;
                var sim = Sim(before: s =>
                {
                    owner = s.PlanningSquad()[0].PersonId;
                    dogId = s.AddBoardEquipment(FacilityGear.Dog, owner).Id;
                });
                Assert.AreEqual(FacilityGear.Dog, sim.S.Raid.Params.Squad[0].Gear);
                int n = RaidChain.SquadCount(sim.S.Raid.Params);
                Assert.IsTrue(sim.AdvanceRaidRoom(Rep(1, squad: n)).Ok);
                Assert.IsTrue(sim.AdvanceRaidRoom(Rep(2, squad: n)).Ok);
                var last = Rep(3, squad: n);
                last.Last = true;
                last.Success = true;
                if (sim.S.Raid.Params.MissionKind == "loot") last.Cargo = 1;
                last.Units.Add(new RoomUnit { Class = Baskin.ClassDog, Type = "kopek", Owner = 0, State = down ? "d" : "a", Hp = down ? 0 : 2 });
                Assert.IsTrue(sim.AdvanceRaidRoom(last).Ok);
                var res = sim.ResolveRaid(null);
                Assert.IsTrue(res.Ok, res.Msg);
                var dog = sim.Board.EquipmentItem(dogId);
                Assert.AreEqual(owner, dog.OwnerId, "köpek sahibinde kalır");
                Assert.AreEqual(down ? sim.B.facilityDogRestTurns : 0, dog.Rest);
                Assert.AreEqual(down, sim.S.RaidLog.Last().LiberationReward.Squad[0].DogDown);
                if (down) Assert.IsNull(sim.PlanningSquad()[0].Gear, "dinlenen köpek sonraki baskına gitmez");
            }
        }
    }
}
