using System;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    public sealed class FacilityTitleTests
    {
        static Baskin.Pt P(int x, int y) => new Baskin.Pt(x, y);

        static Baskin.BState Board(FacilityTitle title, string gear = null, bool active = true, params string[] perks)
        {
            var p = WithGear(Params(Wing("weu_1"), 13, events: false), 0, gear);
            p.Squad[0].PersonId = "leader";
            p.Squad[0].Titles = title;
            p.Squad[0].Move = 4;
            p.Perks.AddRange(perks);
            var balance = Bal(false);
            balance.facilityTitlesEnabled = active;
            return Open(p, Array.Empty<RoomReport>(), balance);
        }

        [TestCase(true, 6, 1)]
        [TestCase(false, 4, 0)]
        public void Marksman_HitsSixTilesAway_AndRecordsAnActualHit(bool active, int range, int work)
        {
            var b = Board(FacilityTitle.Marksman, active: active);
            Flat(b, P(1, 3), P(1, 4), P(1, 5));
            var person = BySlot(b, 0);
            var machine = AddMachine(b, "dron", P(7, 3), 1);
            machine.Idle = true;
            Assert.IsTrue(Baskin.DoAction(b, person.Id, Baskin.ActAmbush, 2, 3));
            Assert.AreEqual(range, Baskin.AmbushLine(b, person).Count);
            Assert.AreEqual(FacilityTitle.None, person.Work, "pusu kurmak henüz isabet değil");
            Assert.IsTrue(Baskin.EndTurn(b));
            Assert.AreEqual(active, machine.Dead);
            Assert.AreEqual(work, (int)person.Work);
        }

        [Test]
        public void Marksman_StacksWithSight()
        {
            var b = Board(FacilityTitle.Marksman, null, true, FacilityGear.Sight);
            Assert.AreEqual(7, Baskin.AmbushRangeFor(b, BySlot(b, 0)));
            Assert.AreEqual(5, Baskin.AmbushRangeFor(b, BySlot(b, 1)));
        }

        [Test]
        public void Ram_ThirdPushAndRecordedContact()
        {
            var b = Board(FacilityTitle.Ram);
            Flat(b, P(0, 3), P(1, 4), P(1, 5));
            var person = BySlot(b, 0);
            var machine = AddMachine(b, "dron", P(4, 3));
            Assert.IsTrue(Baskin.DoAction(b, person.Id, Baskin.ActRush, 4, 3));
            Assert.AreEqual(7, machine.X);
            Assert.AreEqual(FacilityTitle.Ram, person.Work);
        }

        [Test]
        public void Carrier_UsesFullWalk_AndCreditsOnlyAnExtractedBody()
        {
            var b = Board(FacilityTitle.Carrier);
            Flat(b, P(1, 3), P(1, 4), P(0, 5));
            var carrier = BySlot(b, 0);
            var body = BySlot(b, 1);
            Baskin.TestHurt(b, body, body.Hp + body.Armor);
            Assert.IsTrue(Baskin.DoAction(b, carrier.Id, Baskin.ActDrag, 5, 3, body.Id));
            Assert.AreEqual(4, body.X);
            Assert.AreEqual(1 << 1, carrier.Dragged);
            var reports = new System.Collections.Generic.List<RoomReport>();
            PassRoom(b.Params, reports, b);
            var recorded = RoomReport.Decode(reports[0].Encode());
            Assert.AreEqual(1 << 1, recorded.Team[0].Dragged);
            Assert.AreEqual(FacilityTitle.None, FacilityTitles.RaidWorks(0, new[] { recorded }, new RaidResult()));
            var escaped = new RaidResult();
            escaped.Extracted.Add(1);
            Assert.AreEqual(FacilityTitle.Carrier, FacilityTitles.RaidWorks(0, new[] { recorded }, escaped));
        }

        [Test]
        public void Thrower_ExtendedTargetAndUndoRevertsWork()
        {
            var b = Board(FacilityTitle.Thrower);
            Flat(b, P(1, 3), P(6, 3), P(0, 5));
            var actor = BySlot(b, 0);
            b.Carrier = actor.Id;
            AssertUndoRestores(b, s => Baskin.DoAction(s, actor.Id, Baskin.ActThrow, 6, 3), "beş kare fırlatma");
            actor = Fresh(b, actor);
            Assert.AreEqual(FacilityTitle.None, actor.Work);
            Assert.IsTrue(Baskin.DoAction(b, actor.Id, Baskin.ActThrow, 6, 3));
            Assert.AreEqual(FacilityTitle.Thrower, actor.Work);
            Assert.AreEqual(BySlot(b, 1).Id, b.Carrier);
        }

        [Test]
        public void Builder_AddsAnActualDeviceTurn()
        {
            var b = Board(FacilityTitle.Builder, FacilityGear.Turret);
            Flat(b, P(3, 3), P(1, 4), P(0, 5));
            var actor = BySlot(b, 0);
            var at = FreeNeighbor(b, P(3, 3)).Value;
            Assert.IsTrue(Baskin.DoAction(b, actor.Id, Baskin.ActTurret, at.X, at.Y));
            Assert.AreEqual(4, b.Units.Single(Baskin.IsGadget).Turns);
            Assert.AreEqual(FacilityTitle.Builder, actor.Work);
        }

        [Test]
        public void Handler_SecondCommandWorks_AndResetsNextTurn()
        {
            var b = Board(FacilityTitle.Handler, FacilityGear.Dog);
            Flat(b, P(0, 6), P(1, 6), P(2, 6));
            var dog = b.Units.Single(Baskin.IsDog);
            Place(dog, P(3, 3));
            b.CargoX = 4; b.CargoY = 3;
            Assert.IsTrue(Baskin.DoAction(b, dog.Id, Baskin.ActBark, 3, 3));
            Assert.IsTrue(Baskin.CanAct(b, dog));
            Assert.IsTrue(dog.Moved);
            Assert.IsTrue(Baskin.DoAction(b, dog.Id, Baskin.ActFetch, 4, 3));
            Assert.AreEqual(dog.Id, b.Carrier);
            Assert.IsFalse(Baskin.CanAct(b, dog));
            Assert.IsTrue(Baskin.EndTurn(b));
            Assert.IsTrue(Baskin.CanAct(b, dog));
            Assert.AreEqual(0, dog.DogCommands);
        }

        [Test]
        public void Thresholds_CapAndPriorityAreDeterministic()
        {
            var b = new Balance();
            var person = new ResidentState();
            var all = FacilityTitles.Order.Aggregate(FacilityTitle.None, (a, t) => a | t);
            Assert.AreEqual(FacilityTitle.None, FacilityTitles.Credit(person, all, b));
            Assert.AreEqual(FacilityTitle.Carrier | FacilityTitle.Thrower, FacilityTitles.Credit(person, all, b));
            Assert.AreEqual(FacilityTitle.None, FacilityTitles.Credit(person, all, b));
            CollectionAssert.AreEqual(new[] { 3, 3, 3, 3, 3, 3 }, person.Deeds);
            var tie = new ResidentState();
            b.facilityMarksmanAt = b.facilityCarrierAt = b.facilityRamTitleAt = 1;
            Assert.AreEqual(FacilityTitle.Marksman | FacilityTitle.Carrier, FacilityTitles.Credit(tie, all, b));
        }

        static void Finish(Simulation sim, FacilityTitle work)
        {
            while (!sim.RaidRoomsClosed)
            {
                var b = sim.OpenRaidRoom();
                Calm(b);
                BySlot(b, 0).Work = work;
                if (b.Room.Type == RoomType.Target && !b.Escape) StartEscape(b);
                ToGate(b);
                if (b.Phase == "player") Assert.IsTrue(Baskin.Pass(b));
                Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(b)).Ok);
            }
            Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
        }

        [Test]
        public void WinningFirstCapture_CreditsOncePerRaid_AndReplayRestoresAward()
        {
            var sim = Sim(tune: b => b.facilityMarksmanAt = 1);
            var id = sim.S.Raid.Params.Squad[0].PersonId;
            var sector = sim.S.Raid.Params.SectorId;
            Finish(sim, FacilityTitle.Marksman);
            var person = sim.Board.Person(id);
            Assert.AreEqual(1, person.Deeds[0]);
            Assert.AreEqual(FacilityTitle.Marksman, person.Titles);
            Assert.AreEqual(person.Titles, sim.S.RaidLog.Last().LiberationReward.Squad[0].NewTitles);
            Assert.AreEqual("f10", sim.Record.facility);
            var replay = Replay.Restore(sim.Record);
            CollectionAssert.AreEqual(person.Deeds, replay.Board.Person(id).Deeds);
            Assert.AreEqual(person.Titles, replay.Board.Person(id).Titles);
            Assert.AreEqual(person.Titles, replay.S.RaidLog.Last().LiberationReward.Squad[0].NewTitles);

            var siege = ForceSiege(sim);
            Assert.AreEqual(sector, siege.Sector);
            if (!sim.SiegeDecisionDue) ShelterEventKit.Close(sim);
            Assert.IsTrue(sim.YieldSector().Ok);
            ShelterEventKit.Close(sim);
            Assert.IsTrue(TryStartRaid(sim));
            Assert.AreEqual(sector, sim.S.Raid.Params.SectorId);
            Finish(sim, FacilityTitle.Marksman);
            Assert.AreEqual(1, person.Deeds[0], "yeniden alınan kanat ilerleme vermez");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DefeatOrDisabled_NeverCreditsARecordedWork(bool active)
        {
            var sim = Sim(tune: b => b.facilityTitlesEnabled = active);
            var b = sim.OpenRaidRoom();
            var person = sim.Board.Person(BySlot(b, 0).PersonId);
            BySlot(b, 0).Work = FacilityTitle.Marksman;
            Assert.IsTrue(Baskin.Retreat(b));
            var report = sim.RaidRoomReport(b);
            Assert.AreEqual(active, sim.AdvanceRaidRoomCheck(report) == null);
            if (!active) report.Team[0].Work = FacilityTitle.None;
            Assert.IsTrue(sim.AdvanceRaidRoom(report).Ok);
            Assert.IsTrue(sim.ResolveRaid(sim.FacilityRaidResult()).Ok);
            Assert.IsNull(person.Deeds);
        }
    }
}
