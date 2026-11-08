using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 6 gözetmen kuralı (K4-A, biçim 15; PLAN §0.2 C1–C7, E §2 C1–C12): dış kaydın ve dosya incelemesinin gözetmeni
    /// Boşta'nın ilk uygun kişisidir (<c>Board.People</c> sırası; canlı, yatmıyor, kısıtsız, hedef değil, görevi <c>rest</c>); kural dinamiktir
    /// (komut anı, kapanış başı, plan denetimi). Boşta boşken talep ve İNCELE reddedilir; talepten sonra Boşta boşalırsa sonuç gelmez. İNCELE +
    /// DIŞ KAYIT aynı vardiyada dosyayı takılı bırakmaz (H1), kurucu dosyasının İNCELE'si "uyumlu" der (H3). Salt okunur
    /// <see cref="Simulation.AdmissionFindingCode"/> yalnız yapılmış incelemenin metinde yazan sonucunu kod olarak verir. Okumalar RNG
    /// çekmez; kayıttan aynı oyun kurulur. Tohum 79 zaman çizelgesi (C8) kilitlidir.
    /// </summary>
    public class AdmissionSupervisorTests
    {
        const string NoWatcher = "Gözetmen yok: dış kayıt için Boşta'da ayrı, sağlam ve kısıtsız bir sakin bırak.";

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        // ---------------------------------------------------------------- kurulum

        /// <summary>Kapıya her vardiya bir aday gelir (Rev3HandoverSlipFactsTests.Doors ile aynı denge).</summary>
        static Simulation Doors(uint seed = 79, double robotChance = 0, double faultChance = 0, int residents = 8, bool retire = true)
        {
            var sim = Simulation.NewGame(seed, new Balance
            {
                planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = 1,
                admissionRobotChance = robotChance, admissionPaperFaultChance = faultChance, boardStartResidents = residents,
                admissionProtocolInterval = 100,
            });
            if (retire) Retire(sim);
            return sim;
        }

        /// <summary>Kapıya kimse gelmez; sunucunun ilk açılıştaki <c>RetireJobs</c>'u uygulanmış (Rev3BoardSnapshotTests.Retired gibi).</summary>
        static Simulation Retired(uint seed = 79)
        {
            var sim = Simulation.NewGame(seed, new Balance
            {
                planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100
            });
            Retire(sim);
            return sim;
        }

        /// <summary>PlanningBoardPresenter.RetireJobs'un Core karşılığı: emekli görevler gerçek komutlarla BOŞTA'ya.</summary>
        static void Retire(Simulation sim)
        {
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                Assert.IsTrue(sim.AssignResident(p.Id, "rest").Ok);
        }

        static void Close(Simulation sim)
        {
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }

        static CommunityApplicant Candidate(Simulation sim)
        {
            int limit = sim.S.Day + 6;
            while (sim.Admission.Candidate == null && sim.S.Day < limit) Close(sim);
            Assert.IsNotNull(sim.Admission.Candidate);
            return sim.Admission.Candidate;
        }

        static ResidentState At(Simulation sim, int index) => sim.Board.People[index];

        /// <summary>Boşta'nın hedef dışındaki bütün uygun kişilerini gerçek komutlarla emekli ceplere alır (yeni kimse BOŞTA'ya dönmez).</summary>
        static void EmptyIdle(Simulation sim, string targetId)
        {
            string[] away = { "watch", "contact", "care" };
            int used = 0;
            foreach (var p in sim.Board.People.Where(x => x.Alive && x.Id != targetId && x.Job == "rest" && !x.Restricted && x.Down <= 0).ToArray())
            {
                Assert.Less(used, away.Length, "test düzeni: en çok üç uygun Boşta kişisi beklenir");
                Assert.IsTrue(sim.AssignResident(p.Id, away[used++]).Ok, p.Id);
            }
            Assert.IsNull(sim.AdmissionWatcher(targetId));
        }

        static GameRecord CopyOf(GameRecord rec, int day) => new GameRecord
        {
            version = rec.version, seed = rec.seed, balance = rec.balance.Clone(), day = day,
            commands = rec.commands.Select(c => new SavedCommand { day = c.day, kind = c.kind, a = c.a, b = c.b }).ToList(),
        };

        /// <summary>Kabul ve pano durumunun görünür özeti (yeniden oynatma eşitliği için; JsonUtility'siz).</summary>
        static string Fingerprint(Simulation sim)
        {
            var sb = new StringBuilder();
            sb.Append(sim.S.Day).Append('|').Append(sim.S.Rng.State).Append('|').Append(sim.Shelter.Supplies).Append('|');
            foreach (var p in sim.Board.People)
                sb.Append(p.Id).Append(p.Job).Append(p.Hp).Append(p.Alive).Append(p.Restricted).Append(p.Down).Append(';');
            var a = sim.Admission;
            sb.Append('|').Append(a.ObservationTarget).Append('|').Append(a.NextArrivalShift).Append('|').Append(a.GeneratedApplicants).Append('|');
            if (a.Candidate != null)
                sb.Append(a.Candidate.Resident.Id).Append(a.Candidate.WaitingUntil).Append(string.Join(",", a.Candidate.Inspections))
                    .Append(a.Candidate.PaperFinding).Append(a.Candidate.Papers.Serial);
            sb.Append('|');
            foreach (var f in a.Files)
                sb.Append(f.PersonId).Append(f.ReviewPending).Append(f.RecordVerified).Append(f.ReviewNeeded).Append(f.LastObservedShift)
                    .Append(f.LastReviewedClues).Append(f.ClueCount).Append(f.Contained).Append(f.RecordFinding).Append(f.RestrictedUntil)
                    .Append(string.Join(",", f.Inspections)).Append(';');
            sb.Append('|');
            foreach (var e in a.Events) sb.Append(e.Kind).Append(':').Append(e.PersonId).Append(':').Append(e.Shift).Append(':').Append(e.Resolved).Append(';');
            return sb.ToString();
        }

        // ---------------------------------------------------------------- C1 · sıra ve uygunluk

        [Test]
        public void C1_Watcher_IsFirstEligibleIdle_InBoardOrder()
        {
            var sim = Retired();
            // Tohum 79: kurucular tek emp ana craft cook care gather rest; emekliler (care, gather) BOŞTA'ya → Boşta 5, 6, 7
            Assert.AreEqual(new[] { "rest", "rest", "rest" }, new[] { At(sim, 5).Job, At(sim, 6).Job, At(sim, 7).Job });
            Assert.AreSame(At(sim, 5), sim.AdmissionWatcher(null), "aday (pano dışı) için destenin ilk uygun kişisi");
            Assert.AreSame(At(sim, 5), sim.AdmissionWatcher("resident-99"));
            Assert.AreSame(At(sim, 6), sim.AdmissionWatcher(At(sim, 5).Id), "hedef kendini gözetemez: ikinci uygun kişi");
            Assert.AreSame(At(sim, 5), sim.AdmissionWatcher(At(sim, 7).Id));
            Assert.AreEqual("Cem", At(sim, 5).Name, "tohum 79 kadrosu (Aşama 4 W4'te kodla okundu)");
            Assert.AreEqual("İpek", At(sim, 6).Name);
            Assert.AreEqual("Zeynep", At(sim, 7).Name);

            // Yatan atlanır (test düzeni)
            At(sim, 5).Down = 2;
            Assert.AreSame(At(sim, 6), sim.AdmissionWatcher(null), "yatan gözetmen olamaz");
            At(sim, 5).Down = 0;

            // Kısıtlı atlanır (gerçek komut)
            Assert.IsTrue(sim.RestrictResident(At(sim, 5).Id).Ok);
            Assert.AreSame(At(sim, 6), sim.AdmissionWatcher(null), "kısıtlı gözetmen olamaz");
            Assert.IsTrue(sim.RestrictResident(At(sim, 5).Id, false).Ok);
            Assert.AreSame(At(sim, 5), sim.AdmissionWatcher(null));

            // Emekli "watch" görevi artık gözetmen sayılmaz (D1); görevdeki kişi Boşta'da değildir
            Assert.IsTrue(sim.AssignResident(At(sim, 5).Id, "watch").Ok);
            Assert.AreSame(At(sim, 6), sim.AdmissionWatcher(null), "watch görevi gözetmen değil");

            // Ölü atlanır (test düzeni)
            At(sim, 6).Alive = false;
            At(sim, 6).Hp = 0;
            Assert.AreSame(At(sim, 7), sim.AdmissionWatcher(null), "ölü gözetmen olamaz");
            Assert.IsTrue(sim.HasAdmissionWatcher(null));
            Assert.IsFalse(sim.HasAdmissionWatcher(At(sim, 7).Id), "tek uygun kişi hedefin kendisi");
        }

        // ---------------------------------------------------------------- C2 · Boşta boş

        [Test]
        public void C2_Watcher_None_LocksRecordAndReview()
        {
            // 6 kurucu: tek emp ana craft cook care → emeklilik yoksa BOŞTA boş (PLAN P18'in Core eşi)
            var sim = Doors(residents: 6, retire: false);
            Assert.IsFalse(sim.Board.People.Any(p => p.Job == "rest"));
            var c = Candidate(sim);
            Assert.IsNull(sim.AdmissionWatcher(c.Resident.Id));
            int commands = sim.Record.commands.Count;
            Assert.AreEqual(NoWatcher, sim.ApplicantInspectionCheck("record"));
            var refused = sim.InspectApplicant("record");
            Assert.IsFalse(refused.Ok);
            Assert.AreEqual(NoWatcher, refused.Msg);
            var founder = At(sim, 0);
            Assert.AreEqual(NoWatcher, sim.ResidentReviewCheck(founder.Id));
            Assert.IsFalse(sim.ReviewResident(founder.Id).Ok);
            Assert.AreEqual(commands, sim.Record.commands.Count, "reddedilen talep kayda girmez");
            Assert.IsNull(sim.Admission.ObservationTarget);
            Assert.IsNull(sim.ApplicantInspectionCheck("papers"), "öteki incelemeler gözetmen istemez");

            // Boşta'daki tek kişi yatıyorsa da gözetmen yok
            var other = Doors();
            var oc = Candidate(other);
            foreach (var p in other.Board.People.Where(p => p.Job == "rest")) p.Down = 1;   // test düzeni
            Assert.AreEqual(NoWatcher, other.ApplicantInspectionCheck("record"));
            Assert.AreEqual(NoWatcher, other.ResidentReviewCheck(At(other, 0).Id));
            Assert.IsNotNull(oc);
        }

        // ---------------------------------------------------------------- C3 · dış kayıt bir vardiya uzatır

        [Test]
        public void C3_RecordCheck_ExtendsOneShift_WithIdleWatcher()
        {
            var sim = Doors();
            var c = Candidate(sim);
            string id = c.Resident.Id;
            Assert.AreSame(At(sim, 5), sim.AdmissionWatcher(id));
            Assert.IsNull(sim.AdmissionFindingCode(id, "record"), "talep yokken sonuç kodu yok");
            int before = sim.Record.commands.Count;
            Assert.IsTrue(sim.InspectApplicant("record").Ok);
            Assert.AreEqual(before + 1, sim.Record.commands.Count);
            var last = sim.Record.commands[before];
            Assert.AreEqual("inspectApplicant", last.kind);
            Assert.AreEqual("record", last.a);
            Assert.AreEqual(id, sim.Admission.ObservationTarget);
            Assert.IsNull(sim.AdmissionFindingCode(id, "record"), "sonuç kapanışta gelir");
            int waiting = c.WaitingUntil;
            Close(sim);
            Assert.AreSame(c, sim.Admission.Candidate, "aday aynı");
            Assert.Contains("record_done", c.Inspections);
            Assert.AreEqual(waiting + 1, c.WaitingUntil);
            Assert.AreEqual(sim.S.Day + 1, c.WaitingUntil);
            Assert.IsNull(sim.Admission.ObservationTarget);
            Assert.AreEqual("match", sim.AdmissionFindingCode(id, "record"), "ilk aday kusursuz");
            Assert.IsTrue(sim.Admission.Events.Any(e => e.PersonId == id && e.Kind == "record"));
            Close(sim);
            Assert.AreNotSame(c, sim.Admission.Candidate, "tek ek vardiya");
            Assert.IsTrue(sim.Admission.Events.Any(e => e.PersonId == id && e.Kind == "default_reject"));
        }

        // ---------------------------------------------------------------- C4 · kısıtlı çalışma

        [Test]
        public void C4_RestrictedWork_NeedsIdleWatcherAndObservation()
        {
            var sim = Doors();
            var c = Candidate(sim);
            string id = c.Resident.Id;
            Assert.IsTrue(sim.DecideApplicant("restricted").Ok);
            Assert.IsFalse(sim.AssignResident(id, "cook").Ok, "İNCELE yokken çalışamaz");
            Assert.AreSame(At(sim, 5), sim.AdmissionWatcher(id));
            Assert.IsTrue(sim.ReviewResident(id).Ok);
            Assert.IsTrue(sim.IsResidentObserved(id));
            Assert.IsTrue(sim.CanRestrictedResidentWork(id));
            Assert.IsTrue(sim.AssignResident(id, "cook").Ok);    // aşçı BOŞTA'ya döner
            Assert.IsFalse(sim.AssignResident(id, "tek").Ok);
            Assert.IsFalse(sim.AssignResident(id, "craft").Ok);
            Assert.IsTrue(sim.AssignResident(id, "gather").Ok, "emekli toplama görevi kısıtlıya açık");
            Assert.IsNull(sim.BoardPlanCheck());

            // BOŞTA'nın uygunları cebe gider (takassız emekli ceplere): gözetmen kalmaz, plan geçersiz
            var idle = sim.Board.People.Where(p => p.Alive && p.Id != id && p.Job == "rest" && !p.Restricted && p.Down <= 0).ToList();
            string[] away = { "watch", "contact", "care", "train" };
            Assert.LessOrEqual(idle.Count, away.Length);
            for (int i = 0; i < idle.Count; i++) Assert.IsTrue(sim.AssignResident(idle[i].Id, away[i]).Ok, idle[i].Id + " → " + away[i]);
            Assert.IsNull(sim.AdmissionWatcher(id));
            Assert.IsFalse(sim.CanRestrictedResidentWork(id));
            Assert.AreEqual(Loc.F("{0}: {1}", c.Resident.Name, Loc.T("Kısıtlı sakin için ayrı gözetmen gerekiyor.")), sim.BoardPlanCheck());
            Assert.IsFalse(sim.PrintShift().Ok, "fiş basılmaz");
        }

        // ---------------------------------------------------------------- C5 · dinamik gözetmen (D2-A)

        [Test]
        public void C5_Watcher_MovedAfterRequest_DynamicRule()
        {
            var sim = Doors();
            var c = Candidate(sim);
            string id = c.Resident.Id;
            Assert.AreSame(At(sim, 5), sim.AdmissionWatcher(id));
            Assert.IsTrue(sim.InspectApplicant("record").Ok);

            // Talepteki gözetmen emekli cebe gider: sıradaki uygun kişi gözetir
            Assert.IsTrue(sim.AssignResident(At(sim, 5).Id, "watch").Ok);
            Assert.AreSame(At(sim, 6), sim.AdmissionWatcher(id));
            // Takas: cep sahibi BOŞTA'ya döner ve destede önde olduğu için gözetmen olur (Board.People sırası; P19 için not)
            var crafter = sim.Board.People.First(p => p.Job == "craft");
            Assert.IsTrue(sim.AssignResident(At(sim, 6).Id, "craft").Ok);
            Assert.AreEqual("rest", crafter.Job);
            Assert.AreSame(crafter, sim.AdmissionWatcher(id), "takasla BOŞTA'ya dönen kişi sırada önde");
            Close(sim);
            Assert.Contains("record_done", c.Inspections, "gözetmen değişse de sonuç gelir");
            Assert.AreSame(c, sim.Admission.Candidate);

            // Talepten sonra BOŞTA tamamen boşalırsa sonuç gelmez; aday kapanışta ret sayılır
            var empty = Doors();
            var ec = Candidate(empty);
            string eid = ec.Resident.Id;
            Assert.IsTrue(empty.InspectApplicant("record").Ok);
            EmptyIdle(empty, eid);
            Assert.IsNull(empty.BoardPlanCheck());
            Close(empty);
            Assert.IsFalse(ec.Inspections.Contains("record_done"));
            Assert.AreNotSame(ec, empty.Admission.Candidate);
            Assert.IsTrue(empty.Admission.Events.Any(e => e.PersonId == eid && e.Kind == "default_reject"));
            Assert.IsNull(empty.Admission.ObservationTarget, "reddedilen adayın gözetim isteği düşer");
        }

        // ---------------------------------------------------------------- C6 · okumalar RNG çekmez

        [Test]
        public void C6_WatcherRule_AndFindingCodes_ConsumeNoRandomness()
        {
            var sim = Doors(robotChance: 1, faultChance: 1);
            var c = Candidate(sim);
            Assert.IsTrue(sim.InspectApplicant("papers").Ok);
            Assert.IsTrue(sim.InspectApplicant("breath").Ok);
            uint rng = sim.S.Rng.State;
            string serial = c.Papers.Serial;
            int events = sim.Admission.Events.Count, commands = sim.Record.commands.Count;
            string before = Fingerprint(sim);
            for (int i = 0; i < 20; i++)
            {
                sim.AdmissionWatcher(c.Resident.Id);
                sim.HasAdmissionWatcher(c.Resident.Id);
                sim.ApplicantInspectionCheck("record");
                sim.ResidentReviewCheck(At(sim, 7).Id);
                foreach (var kind in new[] { "papers", "interview", "breath", "record", "?" })
                {
                    sim.AdmissionFindingCode(c.Resident.Id, kind);
                    sim.AdmissionFindingCode(At(sim, 0).Id, kind);
                }
            }
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(serial, c.Papers.Serial);
            Assert.AreEqual(events, sim.Admission.Events.Count);
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.AreEqual(before, Fingerprint(sim));
        }

        // ---------------------------------------------------------------- C7 · yeniden oynatma

        [Test]
        public void C7_Replay_RestoresSupervisedRecordAndReview()
        {
            var sim = Doors(robotChance: 1, faultChance: 1);
            var first = Candidate(sim);
            string firstId = first.Resident.Id;
            Assert.IsTrue(sim.InspectApplicant("papers").Ok);
            Assert.IsTrue(sim.InspectApplicant("record").Ok);
            Close(sim);
            Assert.Contains("record_done", first.Inspections);
            Assert.IsTrue(sim.DecideApplicant("admit").Ok);
            var robot = Candidate(sim);
            string robotId = robot.Resident.Id;
            Assert.IsTrue(sim.DecideApplicant("admit").Ok);
            Assert.IsTrue(sim.AssignResident(robotId, "gather").Ok);
            Close(sim);
            Close(sim);
            Assert.AreEqual(1, sim.Admission.File(robotId).ClueCount, "ilk tam vardiya katkı, sonra iz 1");
            Assert.IsTrue(sim.ReviewResident(robotId).Ok);
            Close(sim);
            var file = sim.Admission.File(robotId);
            Assert.AreEqual(2, file.ClueCount, "gözetimli inceleme iz 2'yi hızlandırır");
            Assert.IsTrue(file.Contained);
            Assert.AreEqual(sim.S.Day, file.LastObservedShift);
            Assert.AreEqual(robotId, sim.Admission.ObservationTarget, "dosya incelemesinde hedef temizlenmez");

            var restored = Replay.Restore(CopyOf(sim.Record, sim.S.Day));
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(Fingerprint(sim), Fingerprint(restored));
            Assert.AreEqual(sim.AdmissionFindingCode(firstId, "record"), restored.AdmissionFindingCode(firstId, "record"));
            Assert.AreEqual(sim.AdmissionFindingCode(robotId, "record"), restored.AdmissionFindingCode(robotId, "record"));
        }

        [Test]
        public void C7_Replay_BoardJsonEqual()
        {
            var sim = Doors();
            Candidate(sim);
            Assert.IsTrue(sim.InspectApplicant("record").Ok);
            Assert.IsTrue(sim.ReviewResident(At(sim, 7).Id).Ok, "son istek kazanır (D3-a)");
            Close(sim);
            var restored = Replay.Restore(CopyOf(sim.Record, sim.S.Day));
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(sim.Board), UnityEngine.JsonUtility.ToJson(restored.Board));
        }

        // ---------------------------------------------------------------- C8 · tohum 79 zaman çizelgesi

        /// <summary>Bir vardiyanın kapanışında kapıya gelen aday: gün, kimlik, ad, belge serisi, köken, görünür düzenleme günü.</summary>
        static string Arrival(Simulation sim)
        {
            var c = sim.Admission.Candidate;
            return c == null || c.ArrivalShift != sim.S.Day ? null
                : sim.S.Day + " " + c.Resident.Id + " " + c.Resident.Name + " " + c.Papers.Serial + " " + c.Papers.IssuedShift;
        }

        /// <summary>
        /// Tohum 79, varsayılan denge (alarm kapalı: alarm RNG çekmez, yalnız oyunun sonunu uzak tutar), kararsız bırakılan kapı,
        /// yalnız fiş basma + devir. Değerler çevrimdışı mulberry32 hesabından (E §5.1; a6spec/tools/adm79.py); Unity'de ilk koşuda
        /// gözlenen değerle doğrulanır (bütünleyici: "Values to lock"). Robotluk yalnız yansımayla okunur (sunuma geçmez).
        /// </summary>
        [Test]
        public void C8_Seed79_AdmissionTimeline_Golden()
        {
            var sim = Simulation.NewGame(79, new Balance { liberationAlarmPerShift = 0 });
            Assert.IsNotNull(sim.Admission);
            Assert.AreEqual(4, sim.Admission.NextArrivalShift, "ilk aday V.005");
            var arrivals = new List<string>();
            var versions = new List<string>();
            var identities = new Dictionary<string, AdmissionIdentity>();
            int version = sim.Admission.Protocol.Version;
            for (int day = 1; day <= 16; day++)
            {
                Close(sim);
                Assert.AreEqual(day, sim.S.Day);
                string a = Arrival(sim);
                if (a != null) { arrivals.Add(a); identities[sim.Admission.Candidate.Resident.Id] = sim.Admission.Candidate.Identity; }
                if (sim.Admission.Protocol.Version != version)
                {
                    version = sim.Admission.Protocol.Version;
                    versions.Add(sim.S.Day + " v" + version + " " + sim.Admission.Protocol.IssuedShift);
                }
            }
            // Gözlenen değerler iletide: Unity'de fark çıkarsa bütünleyici bunları kilitler
            string observed = "gözlenen: " + string.Join(" | ", arrivals) + " · sürümler: " + string.Join(" | ", versions) + " · rng " + sim.S.Rng.State;
            CollectionAssert.AreEqual(new[]
            {
                "4 resident-9 Suna 9 N-17/009 4",
                "8 resident-10 Eren N-17/010 8",
                "12 resident-11 Banu T-04/011 12",
                "14 resident-12 Ozan T-04/012 15",   // tarih kusuru: düzenleme bir gün ileri
                "16 resident-13 Lale T-04/013 16",
            }, arrivals, observed);
            CollectionAssert.AreEqual(new[] { "4 v2 4", "8 v3 8" }, versions, observed);
            Assert.AreEqual(5, sim.Admission.Events.Count(e => e.Kind == "arrival"));
            Assert.AreEqual(4, sim.Admission.Events.Count(e => e.Kind == "default_reject"), "kararsız adaylar ertesi kapanışta ret");

            // Gizli kimlik (yalnız test; yansıma; sunuma geçmez): Eren R-4 (buğulu), Ozan S-6 (buğusuz), öteki üçü insan
            var model = typeof(AdmissionIdentity).GetField("Model", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(model, "AdmissionIdentity.Model (internal)");
            CollectionAssert.AreEqual(new[] { "human", "R-4", "human", "S-6", "human" },
                new[] { "resident-9", "resident-10", "resident-11", "resident-12", "resident-13" }.Select(id => (string)model.GetValue(identities[id])).ToArray());
        }

        /// <summary>
        /// C8 sondası: aynı zaman çizelgesinde Day 4'te Suna 9'a dış kayıt istenir (gözetmen Cem); sonuç Day 5'te gelir, aday Day 6'da
        /// ret sayılır ve sonraki aday bir gün kaymaz (NextArrivalShift 8 aday kapıdayken geçmez). Değerler UNVERIFIED → bütünleyici kilitler.
        /// </summary>
        [Test]
        public void C8_Seed79_RecordOnFirstCandidate_Probe()
        {
            var sim = Simulation.NewGame(79, new Balance { liberationAlarmPerShift = 0 });
            Retire(sim);
            while (sim.S.Day < 4) Close(sim);
            var c = sim.Admission.Candidate;
            Assert.IsNotNull(c);
            Assert.AreEqual("Suna 9", c.Resident.Name);
            var watcher = sim.AdmissionWatcher(c.Resident.Id);
            Assert.AreEqual("Cem", watcher.Name, "Boşta Cem, İpek, Zeynep; destenin üstü");
            Assert.IsTrue(sim.InspectApplicant("record").Ok);
            Close(sim);
            Assert.AreSame(c, sim.Admission.Candidate);
            Assert.Contains("record_done", c.Inspections);
            Assert.AreEqual(6, c.WaitingUntil);
            Assert.AreEqual("match", sim.AdmissionFindingCode(c.Resident.Id, "record"));
            Close(sim);
            Assert.IsNull(sim.Admission.Candidate, "Day 6: Suna 9 ret, yeni aday yok");
            while (sim.S.Day < 8) Close(sim);
            Assert.IsNotNull(sim.Admission.Candidate);
            Assert.AreEqual("Eren", sim.Admission.Candidate.Resident.Name, "dış kayıt geliş gününü kaydırmaz");
            Assert.AreEqual("resident-10", sim.Admission.Candidate.Resident.Id);
            Assert.AreEqual("N-17/010", sim.Admission.Candidate.Papers.Serial);
        }

        // ---------------------------------------------------------------- H1 · İNCELE + DIŞ KAYIT

        [Test]
        public void H1_ReviewThenRecord_SameShift_DoesNotStickTheFile()
        {
            var sim = Doors();
            var c = Candidate(sim);
            var x = At(sim, 7);
            Assert.IsTrue(sim.ReviewResident(x.Id).Ok);
            Assert.IsTrue(sim.Admission.File(x.Id).ReviewPending);
            Assert.IsTrue(sim.InspectApplicant("record").Ok, "son istek kazanır (D3-a)");
            Assert.IsFalse(sim.Admission.File(x.Id).ReviewPending, "aday talebi bekleyen dosya incelemesini iptal eder");
            Assert.AreEqual(c.Resident.Id, sim.Admission.ObservationTarget);
            Close(sim);
            Assert.Contains("record_done", c.Inspections);
            Assert.IsNull(sim.ResidentReviewCheck(x.Id), "dosya yeniden incelenebilir (takılı kalmadı)");
            Assert.IsTrue(sim.ReviewResident(x.Id).Ok);
        }

        // ---------------------------------------------------------------- H3 · kurucu dosyası

        [Test]
        public void H3_FounderReview_FindsMatchingPapers()
        {
            var sim = Retired();
            var x = At(sim, 7);
            var file = sim.Admission.File(x.Id);
            Assert.IsTrue(file.Founder);
            Assert.IsNull(sim.AdmissionFindingCode(x.Id, "record"), "kurucu: teyit yok");
            Assert.IsNull(sim.AdmissionFindingCode(x.Id, "papers"));
            Assert.IsNull(sim.ResidentReviewCheck(x.Id));
            Assert.IsTrue(sim.ReviewResident(x.Id).Ok);
            Close(sim);
            Assert.IsFalse(file.ReviewPending);
            Assert.AreEqual(sim.S.Day, file.LastObservedShift);
            Assert.AreEqual("Kimlik ve sevk alanları birbiriyle uyumlu. Geçerli belge insanlık garantisi değildir.", file.RecordFinding);
            Assert.AreEqual("match", sim.AdmissionFindingCode(x.Id, "record"));
            Assert.AreEqual(Loc.T("Bu dosyada yeni incelenecek bulgu yok."), sim.ResidentReviewCheck(x.Id));
            Assert.AreEqual(0, file.ClueCount);
            Assert.IsFalse(file.Contained);
        }

        // ---------------------------------------------------------------- AdmissionFindingCode

        [Test]
        public void FindingCode_NullUntilInspected_ThenMatchesTheText()
        {
            var sim = Doors();
            var c = Candidate(sim);
            string id = c.Resident.Id;
            foreach (var kind in new[] { "papers", "interview", "breath", "record" })
                Assert.IsNull(sim.AdmissionFindingCode(id, kind), kind);
            Assert.IsNull(sim.AdmissionFindingCode(id, "x"));
            Assert.IsNull(sim.AdmissionFindingCode("yok-1", "papers"));
            Assert.IsNull(sim.AdmissionFindingCode(null, "papers"));

            Assert.IsTrue(sim.InspectApplicant("papers").Ok);
            Assert.AreEqual("match", sim.AdmissionFindingCode(id, "papers"));
            StringAssert.Contains("uyumlu", c.PaperFinding);
            Assert.IsTrue(sim.InspectApplicant("interview").Ok);
            string origin = sim.AdmissionFindingCode(id, "interview");
            StringAssert.Contains(origin == "north" ? "Kuzey tahliye" : origin == "east" ? "Doğu aktarma" : "Güney sahra", c.Interview, origin);
            Assert.IsTrue(sim.InspectApplicant("breath").Ok);
            Assert.AreEqual("fog", sim.AdmissionFindingCode(id, "breath"), "ilk aday insan: buğu var");
            StringAssert.Contains("buğusu görüldü", c.Breath);
            Assert.IsNull(sim.AdmissionFindingCode(id, "record"));

            // Aday döneminde teyit edilip kabul edilen dosya: LastObservedShift −1 kalsa da sonuç kodu var (eleştiri 6)
            Assert.IsTrue(sim.InspectApplicant("record").Ok);
            Close(sim);
            Assert.IsTrue(sim.DecideApplicant("admit").Ok);
            var file = sim.Admission.File(id);
            Assert.AreEqual(-1, file.LastObservedShift);
            Assert.AreEqual("match", sim.AdmissionFindingCode(id, "record"));
            Assert.AreEqual("match", sim.AdmissionFindingCode(id, "papers"));
            Assert.AreEqual("fog", sim.AdmissionFindingCode(id, "breath"));
            Assert.AreEqual(origin, sim.AdmissionFindingCode(id, "interview"));
        }

        [Test]
        public void FindingCode_PaperFaultsAndBreath_FollowTheCoreTexts()
        {
            var seen = new HashSet<string>();
            var texts = new Dictionary<string, string>
            {
                { "date", "Düzenleme tarihi" }, { "serial", "numaraları uyuşmuyor" }, { "seal", "mühür" }, { "origin", "Geldiği yer" },
                { "match", "uyumlu" },
            };
            bool fog = false, clear = false;
            for (uint seed = 1; seed <= 40; seed++)
            {
                var sim = Doors(seed, robotChance: 1, faultChance: 1, retire: false);
                Candidate(sim);
                Assert.IsTrue(sim.DecideApplicant("reject").Ok);
                var c = Candidate(sim);
                string id = c.Resident.Id;
                Assert.IsTrue(sim.InspectApplicant("papers").Ok);
                Assert.IsTrue(sim.InspectApplicant("breath").Ok);
                Assert.IsTrue(sim.InspectApplicant("interview").Ok);
                string code = sim.AdmissionFindingCode(id, "papers");
                Assert.IsTrue(texts.ContainsKey(code), code);
                StringAssert.Contains(texts[code], c.PaperFinding, seed + ": " + code);
                seen.Add(code);
                string b = sim.AdmissionFindingCode(id, "breath");
                if (b == "fog") { fog = true; StringAssert.Contains("buğusu görüldü", c.Breath); }
                else { Assert.AreEqual("clear", b); clear = true; StringAssert.Contains("buğusu görülmedi", c.Breath); }
                string o = sim.AdmissionFindingCode(id, "interview");
                StringAssert.Contains(o == "north" ? "Kuzey tahliye" : o == "east" ? "Doğu aktarma" : "Güney sahra", c.Interview, seed + ": " + o);
            }
            CollectionAssert.IsSupersetOf(seen, new[] { "date", "serial", "seal", "origin" }, "dört kusur türü de görüldü: " + string.Join(",", seen));
            Assert.IsTrue(fog && clear, "S-6 buğusuz, R-4 buğulu");
        }

        // ---------------------------------------------------------------- C10–C12 · biçim

        [Test]
        public void C10_Version15()
        {
            Assert.AreEqual(15, GameRecord.CurrentVersion, "K4-A gözetmen kuralı + H1/H3 (biçim 15); eski biçim yüklenmez (SaveStoreTests: CurrentVersion − 1)");
            Assert.AreEqual(15, new GameRecord().version);
            var sim = Doors();
            Assert.AreEqual(15, sim.Record.version);
        }

        // ---------------------------------------------------------------- C11 · gizli kimlik görünür veriyi değiştirmez

        [Test]
        public void C11_HiddenIdentity_SameVisibleData()
        {
            string Visible(double robotChance)
            {
                var sim = Doors(robotChance: robotChance, faultChance: 0);
                Candidate(sim);
                Assert.IsTrue(sim.DecideApplicant("reject").Ok);
                var c = Candidate(sim);
                string id = c.Resident.Id;
                Assert.IsTrue(sim.InspectApplicant("papers").Ok);
                Assert.IsTrue(sim.InspectApplicant("interview").Ok);
                var sb = new StringBuilder();
                var p = c.Papers;
                sb.Append(p.Name).Append(p.Profession).Append(p.Origin).Append(p.Serial).Append(p.ReferralSerial).Append(p.Seal).Append(p.IssuedShift).Append('|');
                sb.Append(c.Resident.MaxHp).Append(c.Resident.Move).Append(c.Resident.Power).Append(c.Resident.Trait).Append('|');
                sb.Append(c.PaperFinding).Append(c.Interview).Append('|');
                sb.Append(sim.AdmissionWatcher(id)?.Name).Append('|');
                foreach (var kind in new[] { "papers", "interview", "breath", "record" })
                    sb.Append(sim.ApplicantInspectionCheck(kind)).Append(sim.AdmissionFindingCode(id, kind)).Append(';');
                foreach (var choice in new[] { "admit", "restricted", "reject", "aid" })
                    sb.Append(sim.ApplicantDecisionCheck(choice)).Append(sim.ApplicantDecisionPreview(choice)).Append(';');
                return sb.ToString();
            }
            Assert.AreEqual(Visible(0), Visible(1), "soğuk gözlemden önce robot ve insan aynı görünür");
        }
    }
}
