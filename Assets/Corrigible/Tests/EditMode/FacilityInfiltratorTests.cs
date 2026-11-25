using System;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    public sealed class FacilityInfiltratorTests
    {
        static Baskin.Pt P(int x, int y) => new Baskin.Pt(x, y);

        static Baskin.BState Board(int robots, int tells = 0, int room = 3)
        {
            var wing = Wing("weu_1");
            var p = Params(wing, 13, events: false);
            p.MissionKind = "server";
            p.FieldTurn = 2; p.Turncoats = robots; p.Tells = tells;
            var b = Baskin.CreateRoom(p, wing[room - 1], FacilityMaps.Setup(Bal(false), wing[room - 1], 13));
            Flat(b, P(2, 3), P(0, 3), P(1, 5));
            // Gerçek hedef hâlâ duruyor; düz zemin kurulumunda silinen dolabın yerini tutar.
            b.Cells[7, 7].O = new Baskin.Obj { K = "node", Hp = 4, Max = 4 };
            foreach (var u in Standing(b)) u.PersonName = u.Type;
            return b;
        }

        static string Visible(Baskin.BState b)
        {
            var fields = typeof(RaidParams).GetFields(BindingFlags.Public | BindingFlags.Instance);
            var parameters = string.Join("|", fields.Select(f => f.Name + "=" + DumpAny(f.GetValue(b.Params))));
            // FullDump sabit planı ve emri referansla kıyaslar. Ayrı emirlerin açık alanlarını yukarıda karşılaştır.
            var view = b.Clone();
            view.Params = null;
            return parameters + FullDump(view) + DumpAny(Baskin.Intents(b)) + DumpAny(Baskin.Hazards(b));
        }

        [Test]
        public void HiddenIdentity_PublicBoardPreviewUndoAndForecast_AgreeUntilCommittedPhase()
        {
            foreach (string field in new[] { "Tells", "Turncoats", "FieldTurn" })
                Assert.IsTrue(typeof(RaidParams).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).IsAssembly);
            Assert.IsNull(typeof(Baskin.Unit).GetField("PulseContact"));
            var human = Board(0);
            var robot = Board(1, 1);
            Assert.AreEqual(Visible(human), Visible(robot));
            foreach (var b in new[] { human, robot })
            {
                var emp = BySlot(b, 1);
                Assert.AreEqual(DumpAny(Baskin.Preview(human, BySlot(human, 1).Id, 2, 3)),
                    DumpAny(Baskin.Preview(robot, BySlot(robot, 1).Id, 2, 3)));
                Assert.IsTrue(Baskin.DoAct(b, emp.Id, 2, 3));
                Assert.IsTrue(Baskin.Undo(b));
                Assert.IsTrue(Baskin.EndTurn(b));
                Assert.IsFalse(BySlot(b, 0).Locked, "geri alınan EMP sonraki evrede iz bırakmaz");
            }
            Assert.AreEqual(Visible(human), Visible(robot));
            human = human.Clone(); robot = robot.Clone();
            Assert.IsTrue(Baskin.ForecastTurn(human));
            Assert.IsTrue(Baskin.ForecastTurn(robot));
            Assert.AreEqual(Visible(human), Visible(robot), "botun ileri bakışı kimliği açmamalı");
        }

        [TestCase(false, true, 4, true)] [TestCase(true, false, 4, true)]
        [TestCase(false, true, 5, false)] [TestCase(true, false, 6, false)]
        [TestCase(false, false, 2, false)]
        public void ActualOrder_SnapshotsExistingTraceAndIdentity(bool robot, bool trace, int delay, bool tells)
        {
            var sim = Sim(tune: b => b.infiltratorFieldEnabled = true, before: s =>
            {
                var f = s.Admission.File(s.PlanningSquad()[0].PersonId);
                f.Founder = false;
                f.Identity = new AdmissionIdentity { Robot = robot, HumanTrace = trace, TraceDelay = delay };
            });
            var p = sim.S.Raid.Params;
            Assert.AreEqual(tells ? 1 : 0, p.Tells);
            Assert.AreEqual(robot ? 1 : 0, p.Turncoats);
            var b = sim.OpenRaidRoom();
            Flat(b, P(3, 3), P(0, 3), P(5, 6));
            Assert.IsTrue(Baskin.DoAct(b, BySlot(b, 1).Id, 2, 3));
            Assert.IsFalse(BySlot(b, 0).Locked);
            Assert.IsTrue(Baskin.EndTurn(b));
            Assert.AreEqual(tells, BySlot(b, 0).Locked);
            Assert.AreEqual(tells, BySlot(b, 0).Moved);
            Assert.IsTrue(Baskin.CanAct(b, BySlot(b, 0)));
            AddDog(b, 1, P(4, 4));
            Assert.IsTrue(Baskin.EndTurn(b));
            Assert.AreEqual(tells, b.Log.Any(l => l.Text.Contains("hırladı")));
        }

        [TestCase(0, "sabotaj", 1)] [TestCase(1, "emp", 3)] [TestCase(2, "kanca", 3)]
        public void SilentTurncoat_ShowsRoleIntentBeforeFirstAttack(int slot, string ability, int distance)
        {
            var b = Board(1 << slot);
            var u = BySlot(b, slot);
            var target = BySlot(b, (slot + 1) % 3);
            Place(BySlot(b, (slot + 2) % 3), P(7, 6));
            Place(u, P(1, 2)); Place(target, P(1 + distance, 2));
            target.Hp = target.Max = 8; target.Armor = 0;
            Assert.IsTrue(Baskin.EndTurn(b));
            Assert.IsTrue(Baskin.IsSpecialist(u));
            Assert.IsTrue(Baskin.EndTurn(b));
            Assert.IsTrue(Baskin.IsTurncoat(u));
            Assert.IsFalse(Baskin.CanAct(b, u));
            Assert.AreEqual(8, target.Hp);
            Assert.AreEqual(1, u.X);
            var intent = Baskin.Intents(b).Single(i => i.Unit == u.Id);
            Assert.AreEqual(ability, intent.Kind);
            Assert.AreEqual(target.X, intent.TargetX);
            if (ability == "emp") Assert.AreEqual(5, intent.Area.Count);
            Assert.IsTrue(Baskin.EndTurn(b));
            Assert.Less(target.Hp, 8);
            if (ability == "kanca") Assert.AreEqual(3, target.X);
            if (ability == "sabotaj") Assert.AreEqual(3, target.X);
        }

        [Test]
        public void TechnicianGuardsEmptyTile_AndLeavesMachineSpawnBudgetUnchanged()
        {
            var boards = new[] { Board(0), Board(1) };
            foreach (var b in boards)
            {
                Place(BySlot(b, 0), P(0, 0)); Place(BySlot(b, 1), P(5, 5));
                for (int x = 1; x <= 6; x++) AddMachine(b, "dron", P(x, 7)).Idle = true;
                b.Cells[3, 6].O = new Baskin.Obj { K = "spawner", Hp = 2, Max = 2 };
                b.SpawnPerTurn = 1; b.Turn = 2;
                Assert.IsTrue(Baskin.EndTurn(b));
            }
            var unit = boards[1].Units.Single(Baskin.IsTurncoat);
            Assert.AreEqual("sabotaj", unit.Intent.Kind);
            Assert.AreEqual(boards[0].Rs, boards[1].Rs);
            Assert.AreEqual(1, boards[1].Spawns.Count);
        }

        [TestCase("loot", false, 3)] [TestCase("server", true, 3)]
        [TestCase("server", false, 1)] [TestCase("server", false, 2)]
        public void OutsideAttackingServerTarget_NoDefection(string mission, bool defense, int room)
        {
            var b = Board(1, room: room);
            b.Params.MissionKind = mission;
            if (defense) b.Params.Mode = "defense";
            b.Turn = 2;
            Assert.IsTrue(Baskin.EndTurn(b));
            Assert.IsTrue(Baskin.IsSpecialist(BySlot(b, 0)));
        }

        [TestCase(false)] [TestCase(true)]
        public void GateAndCab_StillRevealLivingAgent(bool cab)
        {
            var b = Board(1);
            var u = BySlot(b, 0); u.Cab = cab; u.AtGate = !cab;
            b.Turn = 2;
            Assert.IsTrue(Baskin.EndTurn(b));
            Assert.IsTrue(Baskin.IsTurncoat(u));
            Assert.AreEqual(cab, u.Cab);
            Assert.IsFalse(u.AtGate);
        }

        [TestCase(false)] [TestCase(true)]
        public void AdmittedAgent_FieldOutcomeAndInterrogation_Replay(bool capture)
        {
            var balance = ShelterEventKit.Robots(ShelterEventKit.Quiet(1), 6);
            balance.infiltratorFieldEnabled = true;
            balance.shelterEventsEnabled = false;
            balance.liberationRegionEvents = false;
            var s = ShelterEventKit.New(balance);
            while (s.NextLiberationSector(s.S.Liberation.CurrentRegion).MissionKind != "server")
            {
                ShelterEventKit.StartRaid(s); WinRaid(s); ShelterEventKit.Close(s);
            }
            var id = ShelterEventKit.Admit(s);
            var person = s.Board.Person(id);
            while (person.Down > 0) ShelterEventKit.Close(s);
            var kit = s.Board.Equipment.FirstOrDefault(e => e.OwnerId == id && e.Kind.StartsWith("kit_"));
            string role = kit == null ? "tek" : kit.Kind.Substring(4);
            if (kit == null) Assert.IsTrue(s.EquipResident(s.Board.Equipment.First(e => e.Kind == "kit_" + role && !e.Bound).Id, id).Ok);
            Assert.IsTrue(s.AssignResident(id, role).Ok);
            ShelterEventKit.StartRaid(s);
            while (s.RaidRoom.Value.Room < 3)
            {
                var entry = s.OpenRaidRoom(); Calm(entry); ToGate(entry);
                Assert.IsTrue(Baskin.Pass(entry)); Assert.IsTrue(s.AdvanceRaidRoom(s.RaidRoomReport(entry)).Ok);
            }
            var restored = Replay.Restore(s.Record);
            Assert.AreEqual(0, restored.ReplayFailures);
            var b = restored.OpenRaidRoom();
            Calm(b); b.Turn = 2;
            Assert.IsTrue(Baskin.EndTurn(b));
            var u = b.Units.Single(Baskin.IsTurncoat);
            Assert.AreEqual(id, u.PersonId);
            if (capture) Down(b, u);
            Assert.IsTrue(Baskin.Retreat(b));
            Assert.IsTrue(restored.AdvanceRaidRoom(restored.RaidRoomReport(b)).Ok);
            var res = RaidResult.Decode(restored.FacilityRaidResult().Encode());
            CollectionAssert.AreEqual(new[] { u.Slot }, res.Turned);
            Assert.AreEqual(capture, res.Subdued.Contains(u.Slot));
            Assert.AreEqual(!capture, res.Left.Contains(u.Slot));
            Assert.AreEqual(3, res.Hp.Count);
            Assert.IsTrue(restored.ResolveRaid(res).Ok);
            var end = Replay.Restore(restored.Record);
            Assert.AreEqual(0, end.ReplayFailures);
            Assert.AreEqual(capture, end.Board.Person(id).Alive);
            Assert.IsTrue(end.Admission.File(id).Revealed);
            Assert.AreEqual(capture, end.Board.Person(id).Isolated);
            if (!capture) return;
            Assert.AreEqual("saha", end.PendingShelterEvent.Area);
            Assert.IsTrue(end.DecideShelterEvent("sorgula").Ok);
            for (int i = 0; i < balance.isolationShifts; i++) ShelterEventKit.Close(end);
            Assert.IsTrue(end.Admission.File(id).Departed);
            Assert.IsTrue(Replay.Restore(end.Record).Admission.File(id).Departed);
        }

        [Test]
        public void FieldFingerprint_IsConditional()
        {
            var b = Bal();
            Assert.AreEqual("f10", FacilityMaps.FingerprintFor(b), "saha C# varsayılanıyla açık");
            b.infiltratorFieldEnabled = false;
            Assert.AreEqual("f8s10", FacilityMaps.FingerprintFor(b));
            b.infiltratorFieldEnabled = true;
            var sim = Simulation.NewGame(79, b);
            Assert.AreEqual("f10", sim.Record.facility);
            sim.Record.facility = "f8";
            Assert.IsFalse(Replay.MapMatches(sim.Record));
        }
    }
}
