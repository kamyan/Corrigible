using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>Kabul dosyaları ve gecikmeli ihanet: kimlik, kişi-vardiya, kalıcı iz ve müdahale aralığı.</summary>
    public class CommunityAdmissionTests
    {
        static Simulation New(uint seed = 317, double robotChance = 0, double faultChance = 0, int protocolInterval = 100)
        {
            return Simulation.NewGame(seed, new Balance
            {
                planningBoardEnabled = true,
                admissionKnockMin = 2, admissionKnockMax = 2,
                admissionRobotChance = robotChance, admissionPaperFaultChance = faultChance,
                liberationAlarmThreshold = 10000,
                boardStartSupplies = 60, boardSuppliesCapacity = 100,
                admissionProtocolInterval = protocolInterval
            });
        }

        static void Close(Simulation sim)
        {
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }

        static CommunityApplicant Next(Simulation sim)
        {
            int limit = sim.S.Day + sim.B.admissionKnockMax + 2;
            while (sim.Admission.Candidate == null && sim.S.Day < limit) Close(sim);
            Assert.IsNotNull(sim.Admission.Candidate);
            return sim.Admission.Candidate;
        }

        static CommunityApplicant LaterApplicant(Simulation sim)
        {
            Next(sim);
            Assert.IsTrue(sim.DecideApplicant("reject").Ok);
            return Next(sim);
        }

        static ResidentAdmissionFile AdmitRobot(Simulation sim)
        {
            var candidate = LaterApplicant(sim);
            string id = candidate.Resident.Id;
            Assert.IsTrue(sim.DecideApplicant("admit").Ok);
            Assert.IsTrue(sim.AssignResident(id, "gather").Ok);
            return sim.Admission.File(id);
        }

        [Test]
        public void FirstVisitor_AndEightFoundersAreSafe_AndCandidateBecomesTheSameResident()
        {
            var sim = New(robotChance: 1, faultChance: 1);
            Assert.AreEqual(8, sim.Board.People.Count);
            Assert.AreEqual(8, sim.Admission.Files.Count(f => f.Founder));
            var candidate = Next(sim);
            string id = candidate.Resident.Id;
            var actual = candidate.Resident;
            Assert.IsTrue(sim.InspectApplicant("papers").Ok);
            StringAssert.Contains("uyumlu", candidate.PaperFinding);
            Assert.IsTrue(sim.DecideApplicant("admit").Ok);
            Assert.AreSame(actual, sim.Board.Person(id));
            Assert.AreSame(candidate.Papers, sim.Admission.File(id).Papers);
            Assert.IsNull(sim.Admission.Candidate);
            Assert.IsFalse(sim.DecideApplicant("admit").Ok, "Aynı aday ikinci kez kabul edilemez.");
            Assert.IsTrue(sim.AssignResident(id, "gather").Ok);
            for (int i = 0; i < 6; i++) Close(sim);
            Assert.AreEqual(0, sim.Admission.File(id).ClueCount, "Öğretici aday kesin insandır.");
        }

        [Test]
        public void InnocentPaperFault_IsNotRobotProof_AndHonestGameplayStatsStayUnchanged()
        {
            var sim = New(robotChance: 0, faultChance: 1);
            var candidate = LaterApplicant(sim);
            string id = candidate.Resident.Id;
            int hp = candidate.Resident.MaxHp, move = candidate.Resident.Move, power = candidate.Resident.Power;
            Assert.IsTrue(sim.InspectApplicant("papers").Ok);
            Assert.IsNotNull(candidate.PaperFinding);
            Assert.IsFalse(candidate.PaperFinding.Contains("alanları birbiriyle uyumlu"));
            Assert.IsTrue(sim.DecideApplicant("admit").Ok);
            Assert.IsTrue(sim.AssignResident(id, "gather").Ok);
            for (int i = 0; i < 6; i++) Close(sim);
            Assert.AreEqual(0, sim.Admission.File(id).ClueCount);
            Assert.IsFalse(sim.Admission.Events.Any(e => e.PersonId == id && e.Kind == "sabotage"));
            Assert.AreEqual(hp, sim.Board.Person(id).MaxHp);
            Assert.AreEqual(move, sim.Board.Person(id).Move);
            Assert.AreEqual(power, sim.Board.Person(id).Power);
        }

        [Test]
        public void RobotContributesFirst_ThenLeavesTwoIndependentClues_AndOneFullInterventionWindow()
        {
            var sim = New(robotChance: 1);
            var file = AdmitRobot(sim);
            int alive = sim.Board.People.Count(p => p.Alive);
            Close(sim);
            Assert.AreEqual(0, file.ClueCount, "İlk tam vardiya normal katkıdır.");
            Close(sim);
            Assert.AreEqual(1, file.ClueCount);
            Assert.IsFalse(sim.Admission.Events.Any(e => e.Kind == "sabotage"));
            Close(sim);
            Assert.AreEqual(2, file.ClueCount);
            Assert.IsNotNull(sim.ResidentAdmissionRisk(file.PersonId));
            CollectionAssert.AreEquivalent(new[] { "clue_access", "clue_component" }, sim.Admission.Events.Where(e => e.PersonId == file.PersonId && e.Kind.StartsWith("clue_")).Select(e => e.Kind));
            Assert.IsFalse(sim.Admission.Events.Any(e => e.Kind == "sabotage"), "İkinci iz ile aynı kapanışta zarar verilemez.");
            Assert.IsTrue(sim.RestrictResident(file.PersonId).Ok);
            Close(sim);
            Assert.IsFalse(sim.Admission.Events.Any(e => e.Kind == "sabotage"));
            Assert.AreEqual(alive, sim.Board.People.Count(p => p.Alive), "Tek kabul sessiz toplu ölüm yaratamaz.");
        }

        [Test]
        public void IgnoredWarnings_CauseOnlyOneRoleRelatedLocalLoss()
        {
            var sim = New(robotChance: 1);
            var file = AdmitRobot(sim);
            for (int i = 0; i < 4; i++) Close(sim);
            Assert.AreEqual(1, sim.Admission.Events.Count(e => e.PersonId == file.PersonId && e.Kind == "sabotage"));
            Assert.IsTrue(sim.Board.People.All(p => p.Alive));
            Assert.IsNull(sim.S.Ending);
            Close(sim);
            Assert.AreEqual(1, sim.Admission.Events.Count(e => e.PersonId == file.PersonId && e.Kind == "sabotage"));
        }

        [Test]
        public void DeadWitnessCannotEraseLogs_AndASeparateWatcherCanStopPreparation()
        {
            var sim = New(robotChance: 1);
            var file = AdmitRobot(sim);
            Close(sim);
            Close(sim);
            var witness = sim.Board.People.First(p => p.Id != file.PersonId && p.Job == "rest");
            witness.Alive = false;
            witness.Hp = 0;
            witness.Job = "rest";
            Assert.AreEqual(1, sim.Admission.Events.Count(e => e.PersonId == file.PersonId && e.Kind == "clue_access"));
            // K4-A (biçim 15): gözetmen Boşta'nın ilk uygun kişisi; ölen tanık atlanır, Boşta'da kalan kurucu gözetir
            var watcher = sim.AdmissionWatcher(file.PersonId);
            Assert.IsNotNull(watcher);
            Assert.IsTrue(watcher.Alive);
            Assert.AreNotEqual(witness.Id, watcher.Id);
            Assert.AreEqual("rest", watcher.Job);
            Assert.IsTrue(sim.ReviewResident(file.PersonId).Ok);
            Close(sim);
            Assert.AreEqual(2, file.ClueCount);
            Assert.IsTrue(file.Contained);
            Assert.IsTrue(file.Resident.Restricted);
            Close(sim);
            Assert.IsFalse(sim.Admission.Events.Any(e => e.PersonId == file.PersonId && e.Kind == "sabotage"));
        }

        [Test]
        public void RestrictedWorkRequiresAnotherPersonsWholeShift_AndCannotUseSensitiveJobs()
        {
            var sim = New();
            var candidate = Next(sim);
            string id = candidate.Resident.Id;
            Assert.IsTrue(sim.DecideApplicant("restricted").Ok);
            Assert.IsFalse(sim.AssignResident(id, "gather").Ok, "İNCELE yokken kısıtlı kişi gözetimsiz çalışamaz.");
            // K4-A (biçim 15): gözetmen Boşta'nın ilk uygun kişisi; kısıtlı kişinin kendisi sayılmaz
            var watcher = sim.AdmissionWatcher(id);
            Assert.IsNotNull(watcher);
            Assert.AreEqual(sim.Board.People.First(p => p.Alive && p.Id != id && p.Job == "rest" && !p.Restricted && p.Down <= 0).Id, watcher.Id);
            Assert.IsTrue(sim.ReviewResident(id).Ok);
            Assert.IsTrue(sim.AssignResident(id, "gather").Ok);
            Assert.IsFalse(sim.AssignResident(id, "tek").Ok);
            Assert.IsFalse(sim.AssignResident(id, "craft").Ok);
            Assert.IsTrue(sim.AssignResident(id, "cook").Ok);
            Assert.IsNull(sim.BoardPlanCheck(), "Boşta'da ayrı bir gözetmen varken kısıtlı kişi çalışabilir.");

            // Boşta'nın uygun kişileri birer birer cebe alınır (emekli "watch" artık gözetmen sayılmaz, D1)
            var idle = sim.Board.People.Where(p => p.Alive && p.Id != id && p.Job == "rest" && !p.Restricted && p.Down <= 0).ToList();
            Assert.GreaterOrEqual(idle.Count, 2, "takaslar Boşta'ya kişi döndürdü");
            string[] away = { "watch", "contact", "train" };
            for (int i = 0; i < idle.Count - 1; i++) Assert.IsTrue(sim.AssignResident(idle[i].Id, away[i]).Ok, idle[i].Id + " → " + away[i]);
            var last = idle[idle.Count - 1];
            Assert.AreSame(last, sim.AdmissionWatcher(id), "tek uygun Boşta kişisi gözetmendir");
            Assert.IsNull(sim.BoardPlanCheck());
            Assert.IsTrue(sim.AssignResident(last.Id, away[idle.Count - 1]).Ok);
            Assert.IsNull(sim.AdmissionWatcher(id));
            string why = sim.BoardPlanCheck();
            Assert.IsNotNull(why, "Gözetmenin başka işe gitmesi gizli ücretsiz vardiya yaratamaz.");
            StringAssert.Contains(candidate.Resident.Name, why);
            StringAssert.Contains(Loc.T("Kısıtlı sakin için ayrı gözetmen gerekiyor."), why);
        }

        [Test]
        public void CandidateRecordCheck_UsesAWatcher_AndGivesOnlyOneExtraDecisionShift()
        {
            var sim = New();
            var candidate = Next(sim);
            string id = candidate.Resident.Id;
            // Boşta'yı boşalt: emekli "watch" görevi gözetmen sayılmaz (D1), talep reddedilir ve kayda girmez
            foreach (var p in sim.Board.People.Where(p => p.Alive && p.Job == "rest").ToArray())
                Assert.IsTrue(sim.AssignResident(p.Id, "watch").Ok || sim.AssignResident(p.Id, "contact").Ok);
            Assert.IsNull(sim.AdmissionWatcher(id));
            int commands = sim.Record.commands.Count;
            var refused = sim.InspectApplicant("record");
            Assert.IsFalse(refused.Ok);
            Assert.AreEqual(Loc.T("Gözetmen yok: dış kayıt için Boşta'da ayrı, sağlam ve kısıtsız bir sakin bırak."), refused.Msg);
            Assert.AreEqual(commands, sim.Record.commands.Count);

            // Biri Boşta'ya döner: o gözetir
            var back = sim.Board.People.First(p => p.Alive && (p.Job == "watch" || p.Job == "contact"));
            Assert.IsTrue(sim.AssignResident(back.Id, "rest").Ok);
            Assert.AreSame(back, sim.AdmissionWatcher(id));
            Assert.IsTrue(sim.InspectApplicant("record").Ok);
            int waiting = candidate.WaitingUntil;
            Close(sim);
            Assert.AreEqual(id, sim.Admission.Candidate.Resident.Id);
            Assert.Contains("record_done", sim.Admission.Candidate.Inspections);
            Assert.AreEqual(sim.S.Day + 1, sim.Admission.Candidate.WaitingUntil);
            Assert.AreEqual(waiting + 1, sim.Admission.Candidate.WaitingUntil);
            Close(sim);
            Assert.IsFalse(sim.Admission.Candidate?.Resident.Id == id, "Dış kayıt talebi sonsuz ücretsiz bekleme vermez.");
            Assert.IsTrue(sim.Admission.Events.Any(e => e.PersonId == id && e.Kind == "default_reject"));
        }

        [Test]
        public void Replay_RestoresGeneratedIdentityDocumentsInstructionsCommandsAndClues()
        {
            var sim = New(robotChance: 1, faultChance: 1);
            var candidate = LaterApplicant(sim);
            string id = candidate.Resident.Id;
            Assert.IsTrue(sim.InspectApplicant("papers").Ok);
            Assert.IsTrue(sim.InspectApplicant("interview").Ok);
            Assert.IsTrue(sim.InspectApplicant("breath").Ok);
            Assert.IsTrue(sim.DecideApplicant("admit").Ok);
            Assert.IsTrue(sim.AssignResident(id, "gather").Ok);
            Close(sim);
            Close(sim);
            // K4-A (biçim 15): Boşta'daki kurucu gözetir; ayrı görev ataması yok
            Assert.IsTrue(sim.HasAdmissionWatcher(id));
            Assert.IsTrue(sim.ReviewResident(id).Ok);
            Close(sim);
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(sim.Record);
            var file = sim.Admission.File(id);
            var copy = restored.Admission.File(id);
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(file.Papers.Serial, copy.Papers.Serial);
            Assert.AreEqual(file.Papers.Seal, copy.Papers.Seal);
            Assert.AreEqual(file.Papers.Origin, copy.Papers.Origin);
            Assert.AreEqual(file.Papers.IssuedShift, copy.Papers.IssuedShift);
            Assert.AreEqual(file.ClueCount, copy.ClueCount);
            Assert.AreEqual(file.Contained, copy.Contained);
            Assert.AreEqual(file.Resident.MaxHp, copy.Resident.MaxHp);
            Assert.AreEqual(file.Resident.Move, copy.Resident.Move);
            Assert.AreEqual(file.Resident.Power, copy.Resident.Power);
            Assert.AreSame(restored.Board.Person(id), copy.Resident);
            CollectionAssert.AreEqual(sim.Admission.Events.Select(e => e.Kind + ":" + e.PersonId + ":" + e.Shift), restored.Admission.Events.Select(e => e.Kind + ":" + e.PersonId + ":" + e.Shift));
        }

        [Test]
        public void NewProtocol_FlagsOnlyMatchingEarlierFiles_AndKeepsTheOldDatedRules()
        {
            var sim = New(protocolInterval: 4);
            var candidate = Next(sim);
            string id = candidate.Resident.Id;
            bool matched = candidate.Papers.Serial.StartsWith("N-17/");
            Assert.IsTrue(sim.DecideApplicant("admit").Ok);
            while (sim.S.Day < 4) Close(sim);
            Assert.AreEqual(2, sim.Admission.Protocol.Version);
            Assert.AreEqual(4, sim.Admission.Protocol.IssuedShift);
            Assert.AreEqual(matched, sim.Admission.File(id).ReviewNeeded);
            Assert.IsFalse(sim.Admission.Files.Where(f => f.Founder).Any(f => f.ReviewNeeded));
            Assert.AreEqual(1, sim.Admission.ProtocolArchive.Count);
            Assert.AreEqual(1, sim.Admission.ProtocolArchive[0].Version);
            Assert.That(sim.Admission.Protocol.Rules.Count, Is.InRange(2, 3));
        }

        [Test]
        public void RobotSeries_IncludeValidPapersAndDifferentFixedBreathSigns()
        {
            bool sawVisible = false, sawAbsent = false;
            for (uint seed = 1; seed <= 32; seed++)
            {
                var sim = New(seed, robotChance: 1, faultChance: 0);
                var candidate = LaterApplicant(sim);
                Assert.IsTrue(sim.InspectApplicant("papers").Ok);
                StringAssert.Contains("uyumlu", candidate.PaperFinding);
                Assert.IsTrue(sim.InspectApplicant("breath").Ok);
                sawVisible |= candidate.Breath.Contains("buğusu görüldü");
                sawAbsent |= candidate.Breath.Contains("buğusu görülmedi");
            }
            Assert.IsTrue(sawVisible && sawAbsent, "Her robotu ele veren tek ve evrensel bir nefes işareti yoktur.");
        }

        [Test]
        public void RepeatedPresentationReads_DoNotRerollIdentityOrConsumeRandomness()
        {
            var sim = New(robotChance: 1, faultChance: 1);
            var candidate = LaterApplicant(sim);
            uint rng = sim.S.Rng.State;
            string serial = candidate.Papers.Serial;
            for (int i = 0; i < 20; i++)
            {
                sim.ApplicantDecisionCheck("admit");
                sim.ApplicantDecisionPreview("restricted");
                sim.ApplicantInspectionCheck("papers");
                sim.ResidentAdmissionRisk(sim.Board.People[0].Id);
            }
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(serial, candidate.Papers.Serial);
        }
    }
}
