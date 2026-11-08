using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 6 kabul istasyonunun anlık görüntüsü (PLAN §1.7–§1.10, W4; E §3.1 E1–E8): gerçek kartlı simülasyondan belge,
    /// talimat şeridi ve ESKİ cebi, araç ve damga kilitleri (Core denetimleri aynen), bulgu fişleri (Core kodlarından), talep formu ve
    /// gözetmen (<see cref="Simulation.AdmissionWatcher"/>), tek gözlem yuvası uyarıları (H2/H4), madde eşlemesi. Kurucu saf: RNG çekmez,
    /// komut yazmaz, gizli kimliği okumaz. Tohum 79 zaman çizelgesi (W0 C8): Day 4 "Suna 9" <c>resident-9</c> N-17/009, talimat v2;
    /// Day 8 v3; <c>Retired()</c> sonrası Boşta Cem, İpek, Zeynep.
    /// </summary>
    public sealed class Rev3AdmissionSnapshotTests
    {
        const string BuilderPath = "Assets/Corrigible/Desk/Scripts/Bridge/Siginak/AdmissionSnapshotBuilder.cs";
        const string C2 = "Gözetmen yok: dış kayıt için Boşta'da ayrı, sağlam ve kısıtsız bir sakin bırak.";

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        // ---------------------------------------------------------------- kurulum

        static Balance Bal() => new Balance { planningBoardEnabled = true, liberationAlarmPerShift = 0 };

        /// <summary>Sunucunun ilk açılıştaki <c>RetireJobs</c>'u: otomatik üretim ve emekli görevlerin (care, gather, watch, contact) BOŞTA'ya alınması.</summary>
        static Simulation Retired(Simulation sim)
        {
            Ok(sim.EnableAutomaticProduction());
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                Ok(sim.AssignResident(p.Id, "rest"));
            return sim;
        }

        static Simulation Game(Balance b = null, uint seed = 79) => Simulation.NewGame(seed, b ?? Bal());

        /// <summary>Tohum 79, emekliler Boşta'da, 4 kapanış: kapıda "Suna 9" (<c>resident-9</c>, N-17/009), talimat v2.</summary>
        static Simulation AtDoor(Balance b = null)
        {
            var sim = Retired(Game(b));
            for (int i = 0; i < 4; i++) Close(sim);
            Assert.AreEqual(4, sim.S.Day);
            Assert.IsNotNull(sim.Admission.Candidate, "tohum 79: Day 4 aday (W0 C8)");
            Assert.AreEqual("resident-9", sim.Admission.Candidate.Resident.Id);
            return sim;
        }

        /// <summary>Her kapanışta aday gelebilen oyun (knockMax 1): ilki reddedilir, ikincisi kapıda (ilk aday asla robot/kusurlu değildir).</summary>
        static Simulation SecondApplicant(double robotChance, double faultChance = 0, int knockMax = 1)
        {
            var sim = Retired(Game(new Balance
            {
                planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = knockMax,
                admissionRobotChance = robotChance, admissionPaperFaultChance = faultChance,
            }));
            for (int i = 0; i < 8 && sim.Admission.Candidate == null; i++) Close(sim);
            Assert.IsNotNull(sim.Admission.Candidate);
            Ok(sim.DecideApplicant("reject"));
            for (int i = 0; i < 8 && sim.Admission.Candidate == null; i++) Close(sim);
            Assert.IsNotNull(sim.Admission.Candidate);
            return sim;
        }

        static void Close(Simulation sim)
        {
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }

        static void Ok(ActResult r) => Assert.IsTrue(r.Ok, r.Msg);

        static AdmissionSnapshot Build(Simulation sim, AdmissionUi ui = null) => AdmissionSnapshotBuilder.Build(sim, ui ?? new AdmissionUi());

        static string Cand(Simulation sim) => sim.Admission.Candidate.Resident.Id;

        static SlipData SlipOf(AdmissionSnapshot s, AdmissionTool tool) => s.Slips.FirstOrDefault(x => x.Tool == tool);

        static string Fingerprint(Simulation sim)
        {
            var b = sim.Board;
            var sb = new StringBuilder();
            sb.Append(sim.S.Rng.State).Append('|').Append(sim.Record.commands.Count).Append('|').Append(sim.S.Day).Append('|')
              .Append(sim.Shelter.Supplies.ToString("R")).Append('|').Append(b.Locked).Append(b.Printed).Append('|');
            foreach (var p in b.People) sb.Append(p.Id).Append(p.Job).Append(p.Hp).Append(p.Down).Append(p.Restricted).Append(';');
            var a = sim.Admission;
            sb.Append(a.ObservationTarget).Append('|').Append(a.Events.Count).Append('|').Append(a.NextArrivalShift).Append('|')
              .Append(a.GeneratedApplicants).Append('|').Append(a.Protocol.Version).Append('|');
            var c = a.Candidate;
            if (c != null) sb.Append(c.Resident.Id).Append(c.WaitingUntil).Append(string.Join(",", c.Inspections)).Append('|');
            foreach (var f in a.Files)
                sb.Append(f.PersonId).Append(f.ReviewPending).Append(f.ReviewNeeded).Append(f.RecordVerified).Append(f.RestrictedUntil)
                  .Append(f.LastObservedShift).Append(f.ClueCount).Append(';');
            return sb.ToString();
        }

        /// <summary>Anlık görüntünün bütün alanları, karşılaştırma için.</summary>
        internal static string Dump(AdmissionSnapshot s)
        {
            var sb = new StringBuilder();
            void Add(object v) => sb.Append(v ?? "∅").Append('¦');
            Add(s.Active); Add(s.Locked); Add(s.LockReason); Add(s.Counter); Add(s.CandidateId); Add(s.DocumentOnCounter); Add(s.OrdersUnseen);
            Add(s.BreathFog); Add(s.MarkerRule); Add(s.CounterHoverTitle); Add(s.CounterHoverLine);
            var d = s.Document;
            if (d != null)
            {
                Add(d.Name); Add(d.Profession); Add(d.Origin); Add(d.Issued); Add(d.Serial); Add(d.Referral); Add(d.Seal); Add(d.PortraitKey);
                Add(d.Stats); Add(d.PortraitFlip); Add(d.StampChoiceId); Add(d.StampColor); Add(d.StampLines == null ? null : string.Join("/", d.StampLines));
            }
            foreach (var o in new[] { s.Orders }.Concat(s.Archive))
            {
                if (o == null) { Add(null); continue; }
                Add(o.Version); Add(o.Header); Add(o.Number); Add(o.Title); Add(o.Subtitle); Add(o.ArchiveTitle); Add(o.ArchiveRule); Add(o.Footer); Add(o.Footer2);
                foreach (var r in o.Rules) { Add(r.Number); Add(r.Text); Add(r.Tag); Add(r.IsNew); }
            }
            foreach (var t in s.Tools) { Add(t.Used); Add(t.Enabled); Add(t.Title); Add(t.Reason); }
            foreach (var t in s.Stamps) { Add(t.Plate); Add(t.Cap); Add(t.Reason); Add(t.ShortReason); Add(t.Enabled); Add(t.BandColor); }
            foreach (var x in s.Slips)
            {
                Add(x.Tool); Add(x.Review); Add(x.Header); Add(x.Shift); Add(x.Title); Add(x.Icon); Add(x.Body); Add(x.Note); Add(x.Verdict); Add(x.Kind);
                Add(x.Dashed); Add(x.Key);
            }
            var q = s.Request;
            Add(q.Sent); Add(q.Enabled); Add(q.Reason); Add(q.Warning); Add(q.Supervisor); Add(q.SupervisorMissing);
            Add(string.Join("/", q.Labels)); Add(string.Join("/", q.Values));
            var w = s.Words;
            Add(w.Drawer); Add(w.Out); Add(w.Holder); Add(w.Archive); Add(w.Slip); Add(w.Findings); Add(w.Under); Add(w.Glass); Add(w.Intercom); Add(w.Desk);
            Add(w.Rations);
            return sb.ToString();
        }

        /// <summary>Talimat şeridi dışındaki bütün oyuncu metinleri (şerit tasarımda da "robot" der, C Ç23).</summary>
        static IEnumerable<string> TextsOutsideOrders(AdmissionSnapshot s)
        {
            var d = s.Document;
            if (d != null)
            {
                foreach (var t in new[] { d.Name, d.Profession, d.Origin, d.Issued, d.Serial, d.Referral, d.Seal, d.Stats }) yield return t;
                if (d.StampLines != null) foreach (var t in d.StampLines) yield return t;
            }
            foreach (var t in s.Tools) { yield return t.Title; yield return t.Reason; }
            foreach (var t in s.Stamps) { yield return t.Plate; yield return t.Cap; yield return t.Reason; yield return t.ShortReason; }
            foreach (var x in s.Slips) foreach (var t in new[] { x.Header, x.Shift, x.Title, x.Body, x.Note, x.Verdict }) yield return t;
            foreach (var t in s.Request.Labels.Concat(s.Request.Values)) yield return t;
            yield return s.Request.Reason;
            yield return s.Request.Warning;
            yield return s.CounterHoverTitle;
            yield return s.CounterHoverLine;
            yield return s.LockReason;
        }

        // ---------------------------------------------------------------- E1 · saflık

        [Test]
        public void Build_IsPure_TrEn_AndReadsNoHiddenState()
        {
            var sim = AtDoor();
            Ok(sim.InspectApplicant("papers"));
            Ok(sim.InspectApplicant("breath"));
            Ok(sim.InspectApplicant("record"));
            string before = Fingerprint(sim);
            var ui = new AdmissionUi { SeenOrdersVersion = 1, DocumentOnCounterFor = Cand(sim) };
            string first = Dump(Build(sim, ui));
            for (int i = 0; i < 20; i++)
            {
                if (i % 2 == 0) Loc.SetLanguage(Loc.Turkish); else Rev3AdmissionEnglish.Use();
                Build(sim, ui);
                Build(sim, null);
            }
            Loc.SetLanguage(Loc.Turkish);
            Assert.AreEqual(before, Fingerprint(sim), "kurucu Core durumunu, RNG'yi ve kaydı değiştirmez");
            Assert.AreEqual(first, Dump(Build(sim, ui)), "aynı durum aynı görüntü");
            Assert.AreEqual(1, ui.SeenOrdersVersion, "kurucu sunum durumunu da yazmaz");

            Assert.IsTrue(File.Exists(BuilderPath), BuilderPath);
            string source = File.ReadAllText(BuilderPath);
            foreach (string hidden in new[] { "Identity", ".Robot", "PaperFault", "NextArrivalShift", "Sabotaged", "ActiveShifts", "LastAccessJob", "Rng" })
                Assert.IsFalse(source.Contains(hidden), "kabul görüntüsü gizli durumu ya da RNG'yi okumaz: " + hidden);
            foreach (string command in new[] { "InspectApplicant(", "DecideApplicant(", "ReviewResident(", "RestrictResident(", "DismissResident(", "AssignResident(" })
                Assert.IsFalse(source.Contains(command), "kurucu komut çağırmaz: " + command);
        }

        // ---------------------------------------------------------------- E2 · durumlar

        [Test]
        public void States_NoApplicant_Arrival_Inspected_Pending_Result_Decided()
        {
            // Başvuru yok (Day 0): tezgâh katlı, belge ve fiş yok, araçlar kapalı (Core: "Kapıda aday yok.")
            var sim = Retired(Game());
            var none = Build(sim);
            Assert.IsTrue(none.Active);
            Assert.IsNull(none.CandidateId);
            Assert.IsNull(none.Document);
            Assert.AreEqual(CounterState.Folded, none.Counter);
            Assert.IsEmpty(none.Slips);
            Assert.IsFalse(none.Locked);
            Assert.IsNull(none.LockReason);
            Assert.AreEqual("KABUL TEZGÂHI", none.CounterHoverTitle);
            Assert.AreEqual("başvuru yok", none.CounterHoverLine);
            for (int i = 0; i < 4; i++)
            {
                Assert.IsFalse(none.Tools[i].Enabled);
                Assert.IsFalse(none.Tools[i].Used);
                Assert.AreEqual("Kapıda aday yok.", none.Tools[i].Reason);
                Assert.IsFalse(none.Stamps[i].Enabled);
                Assert.AreEqual("Kapıda aday yok.", none.Stamps[i].Reason);
            }
            Assert.IsFalse(none.Request.Sent);
            Assert.AreEqual(-1, none.MarkerRule);

            // Aday geldi: tezgâh iner, belge çekmecede (sunum durumu), bütün araç ve damgalar açık
            for (int i = 0; i < 4; i++) Close(sim);
            string id = Cand(sim);
            var arrived = Build(sim);
            Assert.AreEqual(id, arrived.CandidateId);
            Assert.IsNotNull(arrived.Document);
            Assert.IsFalse(arrived.DocumentOnCounter);
            Assert.AreEqual(CounterState.Down, arrived.Counter);
            Assert.AreEqual("başvuru · Suna 9", arrived.CounterHoverLine);
            Assert.IsTrue(arrived.Tools.All(t => t.Enabled && !t.Used && t.Reason == null));
            Assert.IsTrue(arrived.Stamps.All(t => t.Enabled && t.Reason == null && t.ShortReason == null));
            Assert.IsEmpty(arrived.Slips);
            Assert.IsTrue(Build(sim, new AdmissionUi { DocumentOnCounterFor = id }).DocumentOnCounter);
            Assert.IsFalse(Build(sim, new AdmissionUi { DocumentOnCounterFor = "resident-1" }).DocumentOnCounter, "belge başka adaya bağlı");

            // İncelendi: fiş kıskaçta, kullanılmış araç kapalı, neden Core'un iletisi
            Ok(sim.InspectApplicant("papers"));
            var inspected = Build(sim);
            Assert.AreEqual(1, inspected.Slips.Count);
            Assert.AreEqual(AdmissionTool.Papers, inspected.Slips[0].Tool);
            var used = inspected.Tools[(int)AdmissionTool.Papers];
            Assert.IsTrue(used.Used);
            Assert.IsFalse(used.Enabled);
            Assert.AreEqual(sim.ApplicantInspectionCheck("papers"), used.Reason);
            Assert.AreEqual("Bu inceleme zaten yapıldı.", used.Reason);

            // Dış kayıt bekliyor: BEKLENİYOR (kesikli), talep gönderildi
            Ok(sim.InspectApplicant("record"));
            var pending = Build(sim);
            Assert.AreEqual(id, sim.Admission.ObservationTarget);
            var wait = SlipOf(pending, AdmissionTool.Record);
            Assert.AreEqual(SlipVerdict.Pending, wait.Kind);
            Assert.IsTrue(wait.Dashed);
            Assert.AreEqual("BEKLENİYOR", wait.Verdict);
            Assert.AreEqual(AdmissionSnapshotBuilder.CandidateSlipKey(id, AdmissionTool.Record), wait.Key);
            Assert.IsTrue(pending.Request.Sent);

            // Sonuç geldi (kapanış): BEKLENİYOR'un yerini KAYIT BULUNDU alır, aday hâlâ kapıda
            Close(sim);
            Assert.AreEqual(id, Cand(sim));
            Assert.IsTrue(sim.Admission.Candidate.Inspections.Contains("record_done"));
            var result = Build(sim);
            Assert.AreEqual(2, result.Slips.Count);
            var found = SlipOf(result, AdmissionTool.Record);
            Assert.AreEqual(SlipVerdict.Found, found.Kind);
            Assert.IsFalse(found.Dashed);
            Assert.AreEqual(AdmissionSnapshotBuilder.ResultSlipKey(id), found.Key);
            Assert.AreEqual("record:resident-9:result", found.Key);
            Assert.IsFalse(result.Slips.Any(x => x.Kind == SlipVerdict.Pending));

            // Karar sonrası: aday yok, tezgâh katlanır (talimat görülmüş)
            Ok(sim.DecideApplicant("admit"));
            var decided = Build(sim, new AdmissionUi { SeenOrdersVersion = sim.Admission.Protocol.Version });
            Assert.IsNull(decided.CandidateId);
            Assert.IsNull(decided.Document);
            Assert.IsEmpty(decided.Slips);
            Assert.AreEqual(CounterState.Folded, decided.Counter);
        }

        [Test]
        public void OrdersUnseen_LowersTheCounter_UntilTheVersionIsSeen()
        {
            var sim = AtDoor();
            Assert.AreEqual(2, sim.Admission.Protocol.Version);
            Assert.IsTrue(sim.Admission.Events.Any(e => e.Kind == "protocol" && e.Shift == 4), "bu vardiya talimat değişti");
            Assert.IsTrue(Build(sim, new AdmissionUi { SeenOrdersVersion = 1 }).OrdersUnseen);
            Assert.IsFalse(Build(sim, new AdmissionUi { SeenOrdersVersion = 2 }).OrdersUnseen);
            Assert.IsFalse(Build(sim, new AdmissionUi()).OrdersUnseen, "0 = bilinmiyor; yeni sayılmaz (yükleme, D12)");
            Ok(sim.DecideApplicant("reject"));
            Assert.AreEqual(CounterState.Down, Build(sim, new AdmissionUi { SeenOrdersVersion = 1 }).Counter, "başvuru olmasa da iner (R17e)");
            Assert.AreEqual(CounterState.Folded, Build(sim, new AdmissionUi { SeenOrdersVersion = 2 }).Counter);
        }

        [Test]
        public void Inactive_LegacyGame_GivesInertDefaults()
        {
            var sim = Simulation.NewGame(79, new Balance { planningBoardEnabled = false, liberationAlarmPerShift = 0 });
            var s = Build(sim);
            Assert.IsFalse(s.Active);
            Assert.AreEqual(CounterState.Folded, s.Counter);
            Assert.IsNull(s.Orders);
            Assert.IsNull(s.Document);
            Assert.IsNotNull(s.Request);
            Assert.AreEqual(AdmissionSnapshotBuilder.RequestRows, s.Request.Labels.Length);
            Assert.AreEqual("EVRAK", s.Words.Drawer);
            for (int i = 0; i < 4; i++)
            {
                Assert.IsNotNull(s.Tools[i]);
                Assert.IsFalse(s.Tools[i].Enabled);
                Assert.IsNotNull(s.Stamps[i]);
                Assert.IsFalse(s.Stamps[i].Enabled);
            }
            Assert.IsNotNull(AdmissionSnapshotBuilder.Build(null, null), "oyun yokken de çizilebilir");
        }

        // ---------------------------------------------------------------- E3 · belge

        [Test]
        public void Document_FromCorePapers()
        {
            var sim = AtDoor();
            var c = sim.Admission.Candidate;
            var d = Build(sim).Document;
            Assert.AreEqual("Suna 9", d.Name);
            Assert.AreEqual(c.Papers.Name, d.Name);
            Assert.AreEqual(Loc.T(c.Papers.Profession), d.Profession);
            Assert.AreEqual(Loc.T(c.Papers.Origin), d.Origin);
            Assert.AreEqual("V.005", d.Issued);
            Assert.AreEqual("N-17/009", d.Serial);
            Assert.AreEqual(c.Papers.ReferralSerial, d.Referral);
            Assert.AreEqual(c.Papers.Seal, d.Seal);
            Assert.AreEqual("suna", d.PortraitKey, "portre adı izler: kurucu Suna'nın yüzü (D18)");
            Assert.IsTrue(d.PortraitFlip, "sayılı ad aynalanır (D18-B)");
            Assert.AreEqual(Loc.F("CAN {0}/{1} · HAR {2} · GÜÇ {3}", c.Resident.Hp, c.Resident.MaxHp, c.Resident.Move, c.Resident.Power), d.Stats);
            Assert.IsNull(d.StampChoiceId);
            Assert.IsNull(d.StampLines);
            Assert.IsFalse(AdmissionSnapshotBuilder.NumberedName("Suna"));
            Assert.IsFalse(AdmissionSnapshotBuilder.NumberedName("Suna Kaya"));
            Assert.IsTrue(AdmissionSnapshotBuilder.NumberedName("Eren 12"));
        }

        // ---------------------------------------------------------------- E4 · talimat şeridi

        [Test]
        public void Strip_FollowsProtocol_V1_V2AtDay4_V3AtDay8()
        {
            var sim = Game();
            var v1 = Build(sim).Orders;
            Assert.AreEqual(1, v1.Version);
            Assert.AreEqual("KYGA · KABUL TALİMATI", v1.Header);
            Assert.AreEqual("No. 1", v1.Number);
            Assert.AreEqual("SÜRÜM 1", v1.Title);
            Assert.AreEqual("V.001’den geçerli · 2 kural", v1.Subtitle);
            Assert.AreEqual(sim.Admission.Protocol.Rules.Count, v1.Rules.Count);
            Assert.AreEqual(2, v1.Rules.Count, "Core v1 iki kural (D6-A)");
            Assert.IsTrue(v1.Rules.All(r => !r.IsNew && r.Tag == null));
            Assert.AreEqual("1", v1.Rules[0].Number);
            Assert.AreEqual(Loc.T(sim.Admission.Protocol.Rules[0]), v1.Rules[0].Text);
            Assert.AreEqual("", v1.ArchiveTitle);
            Assert.AreEqual("", v1.Footer);
            Assert.AreEqual("KYGA · KAPI", v1.Footer2);
            Assert.IsEmpty(Build(sim).Archive);

            for (int i = 0; i < 4; i++) Close(sim);
            var s2 = Build(sim);
            var v2 = s2.Orders;
            Assert.AreEqual(2, v2.Version);
            Assert.AreEqual("SÜRÜM 2", v2.Title);
            Assert.AreEqual("V.005’ten geçerli · 3 kural", v2.Subtitle, "Day 4 → V.005 (oyunun V.{Day+1} kuralı, D7)");
            Assert.AreEqual(3, v2.Rules.Count);
            Assert.IsTrue(v2.Rules.All(r => r.IsNew && r.Tag == "YENİ · V.005"), "v2'nin maddeleri v1'de yok");
            Assert.AreEqual("ARŞİV · SÜRÜM 1 · V.001", v2.ArchiveTitle);
            Assert.AreEqual(Loc.T(sim.Admission.ProtocolArchive[0].Rules[0]), v2.ArchiveRule);
            Assert.AreEqual("sürüm 1 arşivde", v2.Footer);
            Assert.AreEqual(1, s2.Archive.Count);
            Assert.AreEqual(1, s2.Archive[0].Version);
            Assert.AreEqual("SÜRÜM 1", s2.Archive[0].Title);

            for (int i = 0; i < 4; i++) Close(sim);
            var s3 = Build(sim);
            var v3 = s3.Orders;
            Assert.AreEqual(3, v3.Version);
            Assert.AreEqual("V.009’dan geçerli · 3 kural", v3.Subtitle);
            Assert.IsFalse(v3.Rules[0].IsNew, "N-17 maddesi v2'de aynen var");
            Assert.AreEqual("sürüm 2’den", v3.Rules[0].Tag);
            Assert.IsTrue(v3.Rules[1].IsNew);
            Assert.AreEqual("YENİ · V.009", v3.Rules[1].Tag);
            Assert.IsTrue(v3.Rules[2].IsNew, "v3 madde 3 v2 madde 3'ten farklı (SCA)");
            Assert.AreEqual("ARŞİV · SÜRÜM 2 · V.005", v3.ArchiveTitle);
            Assert.AreEqual("sürüm 2 arşivde", v3.Footer);
            CollectionAssert.AreEqual(new[] { 2, 1 }, s3.Archive.Select(o => o.Version).ToArray(), "ESKİ cebi yeniden eskiye");
        }

        [Test]
        public void Strip_English_UsesShiftLetterS_AndWrapsNothing()
        {
            var sim = Game();
            for (int i = 0; i < 8; i++) Close(sim);
            Rev3AdmissionEnglish.Use();
            var v3 = Build(sim).Orders;
            Assert.AreEqual("VERSION 3", v3.Title);
            Assert.AreEqual("valid from S.009 · 3 rules", v3.Subtitle);
            Assert.AreEqual("from version 2", v3.Rules[0].Tag);
            Assert.AreEqual("NEW · S.009", v3.Rules[1].Tag);
            Assert.AreEqual("KYGA · ADMISSION RULES", v3.Header);
            Assert.AreEqual("ARCHIVE · VERSION 2 · S.005", v3.ArchiveTitle);
        }

        /// <summary>Ayrılma eki sayının okunuşundaki son sözcüğe uyar; İngilizcede ek yazılmaz.</summary>
        [TestCase(1, "001’den")]
        [TestCase(3, "003’ten")]
        [TestCase(5, "005’ten")]
        [TestCase(6, "006’dan")]
        [TestCase(9, "009’dan")]
        [TestCase(10, "010’dan")]
        [TestCase(40, "040’tan")]
        [TestCase(100, "100’den")]
        public void Ablative_FollowsTheSpokenNumber(int n, string expected)
        {
            Assert.AreEqual(expected, TrNumber.WithAblative(n));
            Rev3AdmissionEnglish.Use();
            Assert.AreEqual(expected.Substring(0, 3), TrNumber.WithAblative(n));
        }

        /// <summary>D3: damga sapı ve bekleyen fiş tasarımın sözcükleriyle; aynı Türkçe anahtarın öteki yerleri bugünkü karşılığında kalır.</summary>
        [Test]
        public void English_StampCapsAndPendingSlip_UseContextWords()
        {
            var sim = AtDoor();
            CollectionAssert.AreEqual(new[] { "KABUL", "KISITLI" }, Build(sim).Stamps.Take(2).Select(x => x.Cap).ToArray());
            Ok(sim.InspectApplicant("record"));
            Assert.AreEqual("BEKLENİYOR", SlipOf(Build(sim), AdmissionTool.Record).Verdict);

            Rev3AdmissionEnglish.Use();
            var en = Build(sim);
            Assert.AreEqual("ADMIT", en.Stamps[(int)StampChoice.Admit].Cap);
            Assert.AreEqual("LIMIT", en.Stamps[(int)StampChoice.Restricted].Cap);
            Assert.AreEqual("PENDING", SlipOf(en, AdmissionTool.Record).Verdict);
            Assert.AreEqual("ADMISSION", Loc.T("KABUL"));
            Assert.AreEqual("RESTRICTED", Loc.T("KISITLI"));
            Assert.AreEqual("AWAITING", Loc.T("BEKLENİYOR"));
            Assert.AreEqual("ADMISSION", DeskLoc.Instance.Ctx("no-such-context", "KABUL"), "bağlam satırı yoksa düz çeviri");
        }

        // ---------------------------------------------------------------- E5 · damgalar

        [Test]
        public void Stamps_Locks_AreCoreChecks()
        {
            var open = Build(AtDoor());
            Assert.IsTrue(open.Stamps.All(x => x.Enabled && x.Reason == null));
            CollectionAssert.AreEqual(new[] { "KABUL ·\nTAM ERİŞİM", "KISITLI\nKABUL", "REDDET", "DIŞARIDAN\nYARDIM" }, open.Stamps.Select(x => x.Plate).ToArray());
            CollectionAssert.AreEqual(new[] { "KABUL", "KISITLI", "RET", "YARDIM" }, open.Stamps.Select(x => x.Cap).ToArray());
            for (int i = 0; i < 4; i++) Assert.AreEqual(AdmissionSnapshotBuilder.StampColorHex((StampChoice)i), open.Stamps[i].BandColor);

            // Kapasite: KABUL ve KISITLI kilitli, tasarımın kısa etiketi; REDDET ve YARDIM açık
            var b = Bal();
            b.boardResidentCapacity = 8;
            var full = AtDoor(b);
            var cap = Build(full);
            foreach (var choice in new[] { StampChoice.Admit, StampChoice.Restricted })
            {
                var st = cap.Stamps[(int)choice];
                Assert.IsFalse(st.Enabled);
                Assert.AreEqual(full.ApplicantDecisionCheck(AdmissionIds.ChoiceOf(choice)), st.Reason);
                Assert.AreEqual("Topluluk kapasitesi 8; kabul için önce güvenli ayrılma kararı ver.", st.Reason);
                Assert.AreEqual("KAPASİTE 8/8", st.ShortReason);
            }
            Assert.IsTrue(cap.Stamps[(int)StampChoice.Reject].Enabled);
            Assert.IsTrue(cap.Stamps[(int)StampChoice.Aid].Enabled);

            // Tek kısıtlı yer: yalnız KISITLI kilitli, kısa etiket yok (Core iletisi)
            var one = AtDoor();
            Ok(one.RestrictResident(one.Board.People[0].Id));
            var single = Build(one);
            var restricted = single.Stamps[(int)StampChoice.Restricted];
            Assert.IsFalse(restricted.Enabled);
            Assert.AreEqual("Tek gözetim yeri dolu; önce mevcut kısıtlı kabulü çöz.", restricted.Reason);
            Assert.IsNull(restricted.ShortReason);
            Assert.IsTrue(single.Stamps[(int)StampChoice.Admit].Enabled);

            // Erzak yetmez: YARDIM kilitli, "erzak yetersiz"
            var rich = Bal();
            rich.admissionAidRations = 1000;
            var poor = AtDoor(rich);
            var aid = Build(poor).Stamps[(int)StampChoice.Aid];
            Assert.IsFalse(aid.Enabled);
            Assert.AreEqual(poor.ApplicantDecisionCheck("aid"), aid.Reason);
            Assert.AreEqual("erzak yetersiz", aid.ShortReason);

            // Plan kilitli: bütün araç ve damgalar kilitli, neden Core'un iletisi; kısa etiket yok
            var printed = AtDoor();
            Ok(printed.PrintShift());
            var locked = Build(printed);
            Assert.IsTrue(locked.Locked);
            Assert.AreEqual("Vardiya planı kilitli.", locked.LockReason);
            Assert.IsTrue(locked.Stamps.All(x => !x.Enabled && x.Reason == "Vardiya planı kilitli." && x.ShortReason == null));
            Assert.IsTrue(locked.Tools.All(x => !x.Enabled && x.Reason == "Vardiya planı kilitli."));
            Assert.IsFalse(locked.Request.Enabled);

            // Baskın
            var raid = AtDoor();
            Ok(raid.StartLiberationRaid("weu_1"));
            var during = Build(raid);
            Assert.IsTrue(during.Locked);
            Assert.AreEqual("Baskın sürüyor.", during.LockReason);
            Assert.IsTrue(during.Stamps.All(x => !x.Enabled && x.Reason == "Baskın sürüyor."));
        }

        [Test]
        public void Lock_WithoutApplicant_ComesFromTheCoreCommandCheck()
        {
            var sim = Retired(Game());
            Assert.IsNull(sim.Admission.Candidate);
            Assert.IsFalse(Build(sim).Locked);
            Ok(sim.PrintShift());
            var s = Build(sim);
            Assert.IsTrue(s.Locked, "aday yokken de kilit görünür (okuma serbest)");
            Assert.AreEqual("Vardiya planı kilitli.", s.LockReason);
        }

        // ---------------------------------------------------------------- E6 · araçlar ve gözetmen

        [Test]
        public void Tools_Locks_AreCoreChecks_AndTheSupervisorIsTheCoreWatcher()
        {
            var sim = AtDoor();
            string id = Cand(sim);
            var s = Build(sim);
            for (int i = 0; i < 4; i++)
            {
                var tool = (AdmissionTool)i;
                Assert.AreEqual(sim.ApplicantInspectionCheck(AdmissionIds.KindOf(tool)) == null, s.Tools[i].Enabled, tool.ToString());
                Assert.AreEqual(AdmissionSnapshotBuilder.ToolTitle(tool), s.Tools[i].Title);
            }
            Assert.AreEqual("BÜYÜTEÇ · belgeyle karşılaştır", s.Tools[(int)AdmissionTool.Papers].Title);
            Assert.AreEqual("İNTERKOM · görüşme", s.Tools[(int)AdmissionTool.Interview].Title);
            Assert.AreEqual("BUĞU CAMI · soğuk gözlem", s.Tools[(int)AdmissionTool.Breath].Title);
            Assert.AreEqual("TALEP DEFTERİ · dış kayıt", s.Tools[(int)AdmissionTool.Record].Title);
            var watcher = sim.AdmissionWatcher(id);
            Assert.IsNotNull(watcher);
            Assert.AreEqual("Cem", watcher.Name, "tohum 79 Retired: Boşta'nın ilki Cem (W0 C1)");
            Assert.AreEqual(watcher.Name, s.Request.Supervisor);
            Assert.IsFalse(s.Request.SupervisorMissing);
            Assert.IsTrue(s.Request.Enabled);
            Assert.IsNull(s.Request.Reason);

            // Boşta boşaltılır (gerçek komutlar): DIŞ KAYIT kilitli, kısa etiket R17-not'tan, Core iletisi talep formunda
            string[] away = { "watch", "contact", "train" };
            var idle = sim.Board.People.Where(x => x.Alive && x.Job == "rest" && !x.Restricted && x.Down <= 0).ToList();
            Assert.LessOrEqual(idle.Count, away.Length);
            for (int i = 0; i < idle.Count; i++) Assert.IsTrue(sim.AssignResident(idle[i].Id, away[i]).Ok, idle[i].Name + " → " + away[i]);
            Assert.IsNull(sim.AdmissionWatcher(id));
            var empty = Build(sim);
            var record = empty.Tools[(int)AdmissionTool.Record];
            Assert.IsFalse(record.Enabled);
            Assert.AreEqual("gözetmen yok · BOŞTA boş", record.Reason);
            Assert.AreEqual(C2, sim.ApplicantInspectionCheck("record"));
            Assert.AreEqual(C2, empty.Request.Reason);
            Assert.IsFalse(empty.Request.Enabled);
            Assert.IsTrue(empty.Request.SupervisorMissing);
            Assert.IsNull(empty.Request.Supervisor);
            Assert.AreEqual("gözetmen yok · BOŞTA boş", empty.Request.Values[AdmissionSnapshotBuilder.RequestSupervisor]);
            Assert.AreEqual("", empty.Request.Labels[AdmissionSnapshotBuilder.RequestSupervisorNote]);
            Assert.IsTrue(empty.Tools[(int)AdmissionTool.Papers].Enabled, "öteki incelemeler gözetmen istemez");
        }

        [Test]
        public void Watcher_IsDeckTopForCandidate_SecondForTopFile()
        {
            var sim = AtDoor();
            string id = Cand(sim);
            var board = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState(), false, false, false);
            Assert.GreaterOrEqual(board.DeckOrder.Count, 2);
            var top = board.DeckOrder[0];
            Assert.AreEqual(sim.AdmissionWatcher(id).Id, top, "aday destede değil: gözetmeni BOŞTA üst kartı");
            Assert.AreEqual("Cem", sim.Board.Person(top).Name);
            var s = Build(sim);
            Assert.AreEqual(sim.Board.Person(top).Name, s.Request.Supervisor);
            Assert.AreEqual("(BOŞTA üst kartı)", s.Request.Labels[AdmissionSnapshotBuilder.RequestSupervisorNote]);
            // Üst kartın kendi dosya incelemesinde gözetmen ikinci uygun kişidir (hedef kendini gözetemez)
            Assert.AreEqual(board.DeckOrder[1], sim.AdmissionWatcher(top).Id);
            Assert.AreEqual("İpek", sim.AdmissionWatcher(top).Name);
        }

        [Test]
        public void Request_Form_FieldsFromThePapers()
        {
            var sim = AtDoor();
            var p = sim.Admission.Candidate.Papers;
            var r = Build(sim).Request;
            Assert.AreEqual(AdmissionSnapshotBuilder.RequestRows, r.Labels.Length);
            Assert.AreEqual(r.Labels.Length, r.Values.Length);
            Assert.AreEqual("KYGA · DIŞ KAYIT TALEBİ", r.Labels[AdmissionSnapshotBuilder.RequestHeader]);
            Assert.AreEqual("V.005", r.Values[AdmissionSnapshotBuilder.RequestHeader]);
            Assert.AreEqual("AD", r.Labels[AdmissionSnapshotBuilder.RequestName]);
            Assert.AreEqual(p.Name, r.Values[AdmissionSnapshotBuilder.RequestName]);
            Assert.AreEqual("BELGE No.", r.Labels[AdmissionSnapshotBuilder.RequestSerial]);
            Assert.AreEqual(p.Serial, r.Values[AdmissionSnapshotBuilder.RequestSerial]);
            Assert.AreEqual("SEVK No.", r.Labels[AdmissionSnapshotBuilder.RequestReferral]);
            Assert.AreEqual(p.ReferralSerial, r.Values[AdmissionSnapshotBuilder.RequestReferral]);
            Assert.AreEqual("MÜHÜR", r.Labels[AdmissionSnapshotBuilder.RequestSeal]);
            Assert.AreEqual(p.Seal, r.Values[AdmissionSnapshotBuilder.RequestSeal]);
            Assert.AreEqual("SORULACAK", r.Labels[AdmissionSnapshotBuilder.RequestAsk]);
            Assert.AreEqual(Loc.F("{0} kaydı", Loc.T(p.Origin)), r.Values[AdmissionSnapshotBuilder.RequestAsk]);
            Assert.AreEqual("GÖZETMEN", r.Labels[AdmissionSnapshotBuilder.RequestSupervisor]);
            Assert.AreEqual("Cem", r.Values[AdmissionSnapshotBuilder.RequestSupervisor]);
            Assert.AreEqual("sonuç sonraki kapanışta", r.Labels[AdmissionSnapshotBuilder.RequestFooter]);
            Assert.AreEqual("KOPYA", r.Labels[AdmissionSnapshotBuilder.RequestCarbon]);
            Assert.IsFalse(r.Sent);
            Ok(sim.InspectApplicant("record"));
            var sent = Build(sim).Request;
            Assert.IsTrue(sent.Sent);
            Assert.IsFalse(sent.Enabled);
            Assert.AreEqual("Bu inceleme zaten yapıldı.", sent.Reason);
            Assert.AreEqual("Cem", sent.Values[AdmissionSnapshotBuilder.RequestSupervisor], "karbon kopyada gözetmen canlı (D2-A)");
        }

        // ---------------------------------------------------------------- fişler

        [Test]
        public void Slips_FromCoreCodes_InInspectionOrder()
        {
            var sim = AtDoor();
            string id = Cand(sim);
            var p = sim.Admission.Candidate.Papers;
            Ok(sim.InspectApplicant("papers"));
            var s1 = Build(sim);
            var papers = SlipOf(s1, AdmissionTool.Papers);
            Assert.AreEqual("KYGA · İNCELEME BULGUSU", papers.Header);
            Assert.AreEqual("V.005", papers.Shift);
            Assert.AreEqual("BELGELERİ KARŞILAŞTIR", papers.Title);
            Assert.AreEqual("papers", papers.Icon);
            Assert.AreEqual(Loc.F("Belge No. {0} · sevk No. {1}. Mühür {2}, düzenleme V.{3:000}.", p.Serial, p.ReferralSerial, p.Seal, 5), papers.Body);
            Assert.AreEqual("match", sim.AdmissionFindingCode(id, "papers"));
            Assert.AreEqual("EŞLEŞİYOR", papers.Verdict);
            Assert.AreEqual(SlipVerdict.Match, papers.Kind);
            Assert.AreEqual("N-17: dış kayıt gerekir (madde 1)", papers.Note, "N-17 serisi, dış kayıt istenmedi (v2 madde 1)");
            Assert.AreEqual("cand:resident-9:papers", papers.Key);
            Assert.IsFalse(papers.Review);
            Assert.AreEqual(0, s1.MarkerRule, "v2'de N-17 belgesi → madde 1 (dizin 0)");

            Ok(sim.InspectApplicant("interview"));
            var interview = SlipOf(Build(sim), AdmissionTool.Interview);
            Assert.AreEqual("GÖRÜŞME YAP", interview.Title);
            Assert.AreEqual(Loc.T(sim.Admission.Candidate.Interview), interview.Body, "görüşme gövdesi Core metni");
            Assert.AreEqual("GÖRÜŞÜLDÜ", interview.Verdict);
            Assert.AreEqual(SlipVerdict.Neutral, interview.Kind);

            Ok(sim.InspectApplicant("breath"));
            var s3 = Build(sim);
            var breath = SlipOf(s3, AdmissionTool.Breath);
            Assert.AreEqual("fog", sim.AdmissionFindingCode(id, "breath"), "Suna 9 insan");
            Assert.IsTrue(s3.BreathFog);
            Assert.AreEqual("SOĞUK GÖZLEM", breath.Title);
            Assert.AreEqual("Soğuk camda nefes buğusu görüldü.", breath.Body);
            Assert.AreEqual("Buğu tek başına insanlık garantisi değildir.", breath.Note);
            Assert.AreEqual("BUĞU VAR", breath.Verdict);
            Assert.AreEqual(SlipVerdict.Neutral, breath.Kind);
            Assert.AreEqual(1, s3.MarkerRule, "v2 soğuk gözlem → madde 2 (dizin 1)");

            Ok(sim.InspectApplicant("record"));
            var s4 = Build(sim);
            var pending = SlipOf(s4, AdmissionTool.Record);
            Assert.AreEqual("DIŞ KAYIT TEYİDİ", pending.Title);
            Assert.AreEqual(Loc.F("Gözetmen: {0} · {1} kaydı telsizle soruldu.", "Cem", p.Seal), pending.Body);
            Assert.AreEqual("sonuç V.005 kapanışında", pending.Note);
            Assert.IsNull(SlipOf(s4, AdmissionTool.Papers).Note, "dış kayıt istendi: N-17 notu düşer (belge uyumlu)");
            Assert.AreEqual(0, s4.MarkerRule, "dış kayıt → N-17 maddesi");
            CollectionAssert.AreEqual(new AdmissionTool?[] { AdmissionTool.Papers, AdmissionTool.Interview, AdmissionTool.Breath, AdmissionTool.Record },
                s4.Slips.Select(x => x.Tool).ToArray(), "kıskaç sırası = yapılış sırası");
            Assert.IsTrue(s4.Tools.All(t => t.Used && !t.Enabled && t.Reason == "Bu inceleme zaten yapıldı."));

            Close(sim);
            var s5 = Build(sim);
            var result = SlipOf(s5, AdmissionTool.Record);
            Assert.AreEqual("V.006", result.Shift, "sonuç kapanışın olayının vardiyası");
            Assert.AreEqual(Loc.F("{0} kaydı belgeyle eşleşiyor.", p.Seal), result.Body);
            Assert.AreEqual("KAYIT BULUNDU", result.Verdict);
            Assert.AreEqual(4, s5.Slips.Count);
        }

        /// <summary>
        /// PLAN §1.8 (bütünleyici C): dış kayıt sonucu kapanışta K1'deyken ağızdan çıkmaz; canlı sunum durumunda (görülen sürüm &gt; 0)
        /// sonuç görülene dek fiş BEKLENİYOR anahtarıyla kalır (GİDEN yaprağı yerinde), <see cref="AdmissionUi.SeenResultFor"/> adaya
        /// eşitlenince (ilk K8/K8A varışı) sonuç anahtarıyla gelir. Boş durum (0 = bilinmiyor) sonucu hemen gösterir.
        /// </summary>
        [Test]
        public void Slips_RecordResult_HeldUntilSeen()
        {
            var sim = AtDoor();
            string id = Cand(sim);
            Ok(sim.InspectApplicant("record"));
            int version = sim.Admission.Protocol.Version;
            var live = new AdmissionUi { SeenOrdersVersion = version };
            Assert.IsTrue(AdmissionSnapshotBuilder.HoldsResult(live, id));
            Close(sim);
            Assert.AreEqual(id, Cand(sim));
            Assert.IsTrue(sim.Admission.Candidate.Inspections.Contains("record_done"));
            live.SeenOrdersVersion = sim.Admission.Protocol.Version;

            var held = SlipOf(Build(sim, live), AdmissionTool.Record);
            Assert.AreEqual(SlipVerdict.Pending, held.Kind, "sonuç görülmedi: BEKLENİYOR kalır");
            Assert.IsTrue(held.Dashed);
            Assert.AreEqual(AdmissionSnapshotBuilder.CandidateSlipKey(id, AdmissionTool.Record), held.Key, "anahtar değişmez: fiş ikinci kez çıkmaz");
            Assert.IsTrue(Build(sim, live).Request.Sent);

            live.SeenResultFor = id;
            Assert.IsFalse(AdmissionSnapshotBuilder.HoldsResult(live, id));
            var shown = SlipOf(Build(sim, live), AdmissionTool.Record);
            Assert.AreEqual(SlipVerdict.Found, shown.Kind);
            Assert.AreEqual(AdmissionSnapshotBuilder.ResultSlipKey(id), shown.Key, "yeni anahtar: varışta ağızdan bir kez çıkar");
            Assert.AreEqual(SlipVerdict.Found, SlipOf(Build(sim), AdmissionTool.Record).Kind, "boş durum (0 = bilinmiyor) tutmaz");
        }

        [Test]
        public void Slips_FaultyPapers_Mismatch_AndRecordDiffers()
        {
            var sim = SecondApplicant(0, faultChance: 1);
            string id = Cand(sim);
            Ok(sim.InspectApplicant("papers"));
            Ok(sim.InspectApplicant("record"));
            string code = sim.AdmissionFindingCode(id, "papers");
            Assert.IsNotNull(code);
            Assert.AreNotEqual("match", code, "kusur payı 1: ikinci adayın belgesi kusurlu");
            var papers = SlipOf(Build(sim), AdmissionTool.Papers);
            Assert.AreEqual("UYUŞMUYOR", papers.Verdict);
            Assert.AreEqual(SlipVerdict.Mismatch, papers.Kind);
            Assert.AreEqual(AdmissionSnapshotBuilder.PaperPhrase(code), papers.Note, "dış kayıt istendi: not farklı alanı söyler");

            Close(sim);
            Assert.AreEqual(id, Cand(sim));
            Assert.AreEqual(code, sim.AdmissionFindingCode(id, "record"));
            var result = SlipOf(Build(sim), AdmissionTool.Record);
            Assert.AreEqual("KAYIT FARKLI", result.Verdict);
            Assert.AreEqual(SlipVerdict.Differs, result.Kind);
            var fields = new Dictionary<string, string>
            {
                { "date", "düzenleme tarihi" }, { "serial", "sevk numarası" }, { "seal", "mühür" }, { "origin", "geldiği yer" },
            };
            Assert.AreEqual(Loc.F("{0} kaydı belgeden farklı: {1}.", sim.Admission.Candidate.Papers.Seal, fields[code]), result.Body);
        }

        [Test]
        public void Slips_ClearBreath_IsNoFog()
        {
            // S-6 gövdesi buğu yapmaz; sunum yalnız kodu okur ("clear"), modeli değil
            for (uint seed = 1; seed < 60; seed++)
            {
                var sim = Retired(Simulation.NewGame(seed, new Balance
                {
                    planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = 1, admissionRobotChance = 1,
                }));
                Close(sim);
                if (sim.Admission.Candidate == null) continue;
                Ok(sim.DecideApplicant("reject"));
                Close(sim);
                if (sim.Admission.Candidate == null) continue;
                Ok(sim.InspectApplicant("breath"));
                if (sim.AdmissionFindingCode(Cand(sim), "breath") != "clear") continue;
                var s = Build(sim);
                var slip = SlipOf(s, AdmissionTool.Breath);
                Assert.IsFalse(s.BreathFog);
                Assert.AreEqual("Soğuk camda nefes buğusu görülmedi.", slip.Body);
                Assert.AreEqual("BUĞU YOK", slip.Verdict);
                Assert.AreEqual(SlipVerdict.Mismatch, slip.Kind, "BUĞU YOK kahverengi (D15)");
                return;
            }
            Assert.Fail("60 tohumda buğusuz (S-6) ikinci aday bulunamadı");
        }

        [Test]
        public void ReviewSlip_AtV3_ForTheBreathFile_UntilReviewed()
        {
            var sim = AtDoor();
            string suna = Cand(sim);
            Ok(sim.InspectApplicant("breath"));
            Ok(sim.DecideApplicant("admit"));
            for (int i = 0; i < 4; i++) Close(sim);
            Assert.AreEqual(8, sim.S.Day);
            Assert.AreEqual(3, sim.Admission.Protocol.Version);
            Assert.IsTrue(sim.Admission.File(suna).ReviewNeeded, "v3: buğu gözlemli eski dosya işaretlenir");
            var s = Build(sim, new AdmissionUi { SeenOrdersVersion = 2 });
            Assert.AreEqual(CounterState.Down, s.Counter);
            var review = s.Slips[0];
            Assert.IsTrue(review.Review, "İNCELEME FİŞİ en altta");
            Assert.IsNull(review.Tool);
            Assert.AreEqual("KYGA · İNCELEME FİŞİ", review.Header);
            Assert.AreEqual("V.009", review.Shift);
            Assert.AreEqual("TALİMAT 3 · MADDE 2", review.Title);
            Assert.AreEqual("Eşleşen eski dosya: Suna 9", review.Body);
            Assert.AreEqual("Dosyasına İNCELE sekmesi.", review.Note);
            Assert.AreEqual("İNCELE", review.Verdict);
            Assert.AreEqual(SlipVerdict.Review, review.Kind);
            Assert.AreEqual(AdmissionSnapshotBuilder.ReviewSlipKey(8), review.Key);
            Assert.AreEqual("review:8", review.Key);

            Ok(sim.ReviewResident(suna));
            if (sim.Admission.Candidate != null) Ok(sim.DecideApplicant("reject"));
            Close(sim);
            Assert.IsFalse(sim.Admission.File(suna).ReviewNeeded);
            Assert.IsFalse(Build(sim).Slips.Any(x => x.Review), "dosya incelenince kıskaçtan kalkar");
        }

        // ---------------------------------------------------------------- E7 · izler

        [Test]
        public void Impressions_FromBalance()
        {
            var b = new Balance();
            CollectionAssert.AreEqual(new[] { "KABUL", "TAM ERİŞİM" }, AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Admit, b));
            CollectionAssert.AreEqual(new[] { "KISITLI KABUL", "3 VARDİYA" }, AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Restricted, b));
            CollectionAssert.AreEqual(new[] { "REDDET" }, AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Reject, b));
            CollectionAssert.AreEqual(new[] { "DIŞARIDAN YARDIM", "2 ERZAK" }, AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Aid, b));
            Assert.AreEqual("4 VARDİYA", AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Restricted, new Balance { admissionRestrictedShifts = 4 })[1]);
            Assert.AreEqual("2,5 ERZAK", AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Aid, new Balance { admissionAidRations = 2.5 })[1]);

            var d = new DocumentData();
            AdmissionSnapshotBuilder.Stamp(d, StampChoice.Restricted, new Balance { admissionRestrictedShifts = 4 });
            Assert.AreEqual("restricted", d.StampChoiceId);
            CollectionAssert.AreEqual(new[] { "KISITLI KABUL", "4 VARDİYA" }, d.StampLines);
            Assert.AreEqual("#854005", d.StampColor, "renk temadan (admission.stampMarks.restricted.color)");
            Assert.AreEqual("#24592E", AdmissionSnapshotBuilder.StampColorHex(StampChoice.Admit));
            Assert.AreEqual("#9A1E14", AdmissionSnapshotBuilder.StampColorHex(StampChoice.Reject));
            Assert.AreEqual("#463F34", AdmissionSnapshotBuilder.StampColorHex(StampChoice.Aid));

            Rev3AdmissionEnglish.Use();
            Assert.AreEqual("3 SHIFTS", AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Restricted, b)[1]);
            Assert.AreEqual("2 RATIONS", AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Aid, b)[1]);
            Assert.AreEqual("2.5 RATIONS", AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Aid, new Balance { admissionAidRations = 2.5 })[1]);
        }

        // ---------------------------------------------------------------- E8 · gizli kimlik

        [Test]
        public void HiddenIdentity_SameSnapshot()
        {
            // faultChance 0: robotta model çekimi kusur türü çekiminden önce gelir (SCA); kusursuzken görünür belge aynı
            Simulation Inspected(double robotChance)
            {
                var sim = SecondApplicant(robotChance, 0, knockMax: 4);
                Ok(sim.InspectApplicant("papers"));
                Ok(sim.InspectApplicant("interview"));
                Ok(sim.InspectApplicant("record"));
                return sim;
            }
            var human = Inspected(0);
            var robot = Inspected(1);
            Assert.AreEqual(human.S.Day, robot.S.Day);
            Assert.AreNotEqual(human.S.Rng.State, robot.S.Rng.State, "gizli durum gerçekten ayrıştı");
            var ui = new AdmissionUi { SeenOrdersVersion = human.Admission.Protocol.Version };
            Assert.AreEqual(Dump(Build(human, ui)), Dump(Build(robot, ui)), "görüntü gizli kimliği yazmaz (soğuk gözlemden önce)");
            Close(human);
            Close(robot);
            Assert.AreEqual(Dump(Build(human, ui)), Dump(Build(robot, ui)), "dış kayıt sonucu da aynı");
        }

        [Test]
        public void Snapshot_NeverSaysRobot_OutsideTheOrders_TrEn()
        {
            var sim = SecondApplicant(1);
            foreach (string kind in new[] { "papers", "interview", "breath", "record" }) Ok(sim.InspectApplicant(kind));
            void Scan(string when)
            {
                foreach (bool english in new[] { false, true })
                {
                    if (english) Rev3AdmissionEnglish.Use(); else Loc.SetLanguage(Loc.Turkish);
                    foreach (var t in TextsOutsideOrders(Build(sim)))
                        Assert.IsFalse(t != null && t.IndexOf("robot", System.StringComparison.OrdinalIgnoreCase) >= 0,
                            when + (english ? " EN: " : " TR: ") + t);
                }
                Loc.SetLanguage(Loc.Turkish);
            }
            Scan("inceleme");
            Close(sim);
            Scan("sonuç");
        }

        // ---------------------------------------------------------------- uyarılar (H2/H4)

        [Test]
        public void Warnings_H2_H4()
        {
            // H2: gözlem yuvası bir dosyanın bekleyen incelemesinde → talep onu iptal eder
            var sim = AtDoor();
            string id = Cand(sim);
            var q = sim.Board.People[0];
            Ok(sim.ReviewResident(q.Id));
            string slot = Loc.F("Gözetmen şu an {0} dosyasında; bu talep onu iptal eder.", q.Name);
            Assert.AreEqual(slot, AdmissionSnapshotBuilder.RequestWarning(sim));
            Assert.AreEqual(slot, Build(sim).Request.Warning);
            var other = sim.Board.People[1];
            Assert.AreEqual(slot, AdmissionSnapshotBuilder.ReviewWarning(sim, other.Id), "başka dosyanın İNCELE'si de yuvayı alır");
            Assert.IsNull(AdmissionSnapshotBuilder.ReviewWarning(sim, q.Id), "aynı dosya için uyarı yok");

            // Talep gönderildi: dosya incelemesi iptal (Core, son istek kazanır); şimdi dosya İNCELE'si talebi iptal eder
            Ok(sim.InspectApplicant("record"));
            Assert.IsFalse(sim.Admission.File(q.Id).ReviewPending);
            Assert.IsNull(AdmissionSnapshotBuilder.RequestWarning(sim));
            Assert.AreEqual("Kapıdaki dış kayıt talebi iptal olur.", AdmissionSnapshotBuilder.ReviewWarning(sim, q.Id));

            // Yine de İNCELE verilirse talep sessizce düşer: fiş ve defter bunu söyler
            Ok(sim.ReviewResident(q.Id));
            Assert.AreNotEqual(id, sim.Admission.ObservationTarget);
            var cancelled = Build(sim);
            Assert.AreEqual("talep iptal edildi · sonuç gelmez", cancelled.Request.Warning);
            Assert.AreEqual("talep iptal edildi · sonuç gelmez", SlipOf(cancelled, AdmissionTool.Record).Body);

            // H4: gözetimle çalışan kısıtlı kişi
            var h4 = Retired(Game());
            for (int i = 0; i < 3; i++) Close(h4);
            var r = h4.Board.People.First(x => x.Alive && x.Job == "cook");
            Ok(h4.RestrictResident(r.Id));
            Ok(h4.ReviewResident(r.Id));
            Close(h4);
            Assert.IsNotNull(h4.Admission.Candidate, "tohum 79: Day 4 aday");
            Assert.AreEqual(r.Id, h4.Admission.ObservationTarget, "dosya incelemesi bitti, gözlem yuvası kişide kalır");
            Ok(h4.AssignResident(r.Id, "gather"));
            Assert.IsTrue(h4.CanRestrictedResidentWork(r.Id));
            string works = Loc.F("{0} gözetimle çalışıyor; bu talep onun planını bozar.", r.Name);
            Assert.AreEqual(works, AdmissionSnapshotBuilder.RequestWarning(h4));
            Assert.AreEqual(works, Build(h4).Request.Warning);
            Assert.AreEqual(works, AdmissionSnapshotBuilder.ReviewWarning(h4, h4.Board.People[0].Id));

            // Uyarısız durum
            var calm = AtDoor();
            Assert.IsNull(AdmissionSnapshotBuilder.RequestWarning(calm));
            Assert.IsNull(AdmissionSnapshotBuilder.ReviewWarning(calm, calm.Board.People[0].Id));
        }

        // ---------------------------------------------------------------- madde eşlemesi

        [Test]
        public void RuleMap_CoreRuleCountsAndKeys()
        {
            var sim = Game();
            for (int i = 0; i < 8; i++) Close(sim);
            var adm = sim.Admission;
            Assert.AreEqual(2, adm.ProtocolArchive.Count);
            var v1 = adm.ProtocolArchive[0].Rules;
            var v2 = adm.ProtocolArchive[1].Rules;
            var v3 = adm.Protocol.Rules;
            Assert.AreEqual(2, v1.Count, "v1");
            Assert.AreEqual(3, v2.Count, "v2");
            Assert.AreEqual(3, v3.Count, "v3");
            Assert.AreEqual("Düzenleme tarihi bugünden ileri olamaz; kimlik ve sevk numaraları aynı olmalıdır.", v1[0]);
            Assert.AreEqual("N-17 serisi bağımsız dış kayıtla teyit edilmelidir; serinin kendisi robot kanıtı değildir.", v2[0]);
            Assert.AreEqual("Soğuk gözlemde S-6 gövdelerinde nefes buğusu yoktur. Bu işaret bütün robot serilerini kapsamaz.", v2[1]);
            Assert.AreEqual("Belge kusurunu, dış kaydı ve erişim izlerini birlikte değerlendirin.", v2[2]);
            Assert.AreEqual(v2[0], v3[0], "v3 madde 1 = v2 madde 1 (\"sürüm 2’den\")");
            Assert.AreEqual("Nefes buğusu insanlık garantisi değildir. R-4 serileri bu işareti taklit eder.", v3[1]);
            Assert.AreEqual("Birbirinden bağımsız erişim ve parça kayıtları kayıp tanık olsa da dosyada kalır.", v3[2]);

            string Rule(List<string> rules, int item) => item > 0 ? rules[item - 1] : null;
            StringAssert.Contains("numaraları", Rule(v1, AdmissionRuleMap.Papers(1, "T-04/009")));
            StringAssert.Contains("N-17", Rule(v2, AdmissionRuleMap.Papers(2, "N-17/009")));
            StringAssert.Contains("N-17", Rule(v3, AdmissionRuleMap.Papers(3, "N-17/010")));
            StringAssert.Contains("Belge kusurunu", Rule(v2, AdmissionRuleMap.Papers(2, "T-04/011")));
            Assert.AreEqual(0, AdmissionRuleMap.Papers(3, "T-04/011"), "v3'te N-17 dışı belgeye madde yok (eleştiri 16)");
            StringAssert.Contains("buğusu", Rule(v2, AdmissionRuleMap.Breath(2)));
            StringAssert.Contains("buğusu", Rule(v3, AdmissionRuleMap.Breath(3)));
            Assert.AreEqual(0, AdmissionRuleMap.Breath(1));
            StringAssert.Contains("dış kayıt", Rule(v2, AdmissionRuleMap.Record(2)));
            StringAssert.Contains("dış kayıt", Rule(v3, AdmissionRuleMap.Record(3)));
            Assert.AreEqual(0, AdmissionRuleMap.Record(1));
            StringAssert.Contains("N-17", Rule(v2, AdmissionRuleMap.ReviewRule(2)), "v2 inceleme fişi N-17 dosyaları için");
            StringAssert.Contains("buğusu", Rule(v3, AdmissionRuleMap.ReviewRule(3)), "v3 inceleme fişi buğu gözlemli dosyalar için");
            Assert.IsTrue(AdmissionRuleMap.IsN17("N-17/204"));
            Assert.IsFalse(AdmissionRuleMap.IsN17("T-04/204"));
            Assert.IsFalse(AdmissionRuleMap.IsN17(null));
        }

        [Test]
        public void Words_AreTranslated_InTheCurrentLanguage()
        {
            var tr = Build(Game()).Words;
            Assert.AreEqual("EVRAK", tr.Drawer);
            Assert.AreEqual("GİDEN · TELSİZ", tr.Out);
            Assert.AreEqual("TALİMAT", tr.Holder);
            Assert.AreEqual("ESKİ", tr.Archive);
            Assert.AreEqual("BULGU", tr.Slip);
            Assert.AreEqual("BULGULAR", tr.Findings);
            Assert.AreEqual("BUĞU CAMI", tr.Glass);
            Assert.AreEqual("İNTERKOM", tr.Intercom);
            Assert.AreEqual("MASA", tr.Desk);
            Assert.AreEqual("ERZAK", tr.Rations);
            Rev3AdmissionEnglish.Use();
            var en = Build(Game()).Words;
            Assert.AreEqual("PAPERS", en.Drawer, "EN'de EVRAK = PAPERS (Aşama 4 D3)");
            Assert.AreEqual("OUT · RADIO", en.Out);
            Assert.AreEqual("ORDERS", en.Holder);
            Assert.AreEqual("BREATH GLASS", en.Glass);
        }
    }

    /// <summary>
    /// İngilizce tablo: <c>strings.txt</c> + Aşama 6'nın henüz tabloya girmemiş metinleri (PLAN §4; W6 tabloya yazar, aynı değerlerle).
    /// Tablodaki değer önce gelir.
    /// </summary>
    internal static class Rev3AdmissionEnglish
    {
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";

        internal static readonly Dictionary<string, string> Stage6 = new Dictionary<string, string>
        {
            { "GİDEN · TELSİZ", "OUT · RADIO" }, { "TALİMAT", "ORDERS" }, { "ESKİ", "OLD" }, { "BULGU", "FINDING" }, { "BUĞU CAMI", "BREATH GLASS" },
            { "KYGA · KABUL TALİMATI", "KYGA · ADMISSION RULES" }, { "No. {0}", "No. {0}" }, { "SÜRÜM {0}", "VERSION {0}" },
            { "V.{0} geçerli · {1} kural", "valid from S.{0} · {1} rules" }, { "sürüm {0}’den", "from version {0}" },
            { "YENİ · V.{0:000}", "NEW · S.{0:000}" }, { "ARŞİV · SÜRÜM {0} · V.{1:000}", "ARCHIVE · VERSION {0} · S.{1:000}" },
            { "sürüm {0} arşivde", "version {0} archived" }, { "KYGA · KAPI", "KYGA · DOOR" },
            { "KYGA · İNCELEME BULGUSU", "KYGA · CHECK FINDING" }, { "EŞLEŞİYOR", "MATCHES" }, { "UYUŞMUYOR", "MISMATCH" },
            { "GÖRÜŞÜLDÜ", "INTERVIEWED" }, { "BUĞU VAR", "FOG SEEN" }, { "BUĞU YOK", "NO FOG" }, { "KAYIT BULUNDU", "RECORD FOUND" },
            { "KAYIT FARKLI", "RECORD DIFFERS" },
            { "Belge No. {0} · sevk No. {1}. Mühür {2}, düzenleme V.{3:000}.", "Doc No. {0} · transfer No. {1}. Seal {2}, issued S.{3:000}." },
            { "N-17: dış kayıt gerekir (madde {0})", "N-17: outside record required (rule {0})" },
            { "Soğuk camda nefes buğusu görüldü.", "Breath fogs the cold glass." },
            { "Soğuk camda nefes buğusu görülmedi.", "The cold glass stays clear." },
            { "Buğu tek başına insanlık garantisi değildir.", "Fog alone does not prove a human." },
            { "Gözetmen: {0} · {1} kaydı telsizle soruldu.", "Supervisor: {0} · {1} record requested by radio." },
            { "sonuç V.{0:000} kapanışında", "result at S.{0:000} closing" },
            { "{0} kaydı belgeyle eşleşiyor.", "{0} record matches the paper." },
            { "{0} kaydı belgeden farklı: {1}.", "{0} record differs from the paper: {1}." },
            { "düzenleme tarihi", "issue date" }, { "sevk numarası", "transfer number" }, { "mühür", "seal" }, { "geldiği yer", "origin" },
            { "KYGA · İNCELEME FİŞİ", "KYGA · REVIEW SLIP" }, { "TALİMAT {0} · MADDE {1}", "ORDERS {0} · ITEM {1}" },
            { "Eşleşen eski dosya: {0}", "Matching old file: {0}" }, { "Dosyasına İNCELE sekmesi.", "REVIEW tab on the file." },
            { "KYGA · DIŞ KAYIT TALEBİ", "KYGA · OUTSIDE RECORD REQUEST" }, { "BELGE No.", "DOC No." }, { "SEVK No.", "TRANSFER No." },
            { "SORULACAK", "ASK" }, { "GÖZETMEN", "SUPERVISOR" }, { "(BOŞTA üst kartı)", "(top card of IDLE)" },
            { "sonuç sonraki kapanışta", "result at the next close" }, { "KOPYA", "CARBON COPY" }, { "{0} kaydı", "{0} record" },
            { "gözetmen yok · BOŞTA boş", "no supervisor · IDLE empty" }, { "gözetmen yok · sonuç gelmez", "no supervisor · no result" },
            { "talep iptal edildi · sonuç gelmez", "request cancelled · no result" },
            { "Gözetmen şu an {0} dosyasında; bu talep onu iptal eder.", "The supervisor is on {0}'s file; this request cancels it." },
            { "{0} gözetimle çalışıyor; bu talep onun planını bozar.", "{0} works under supervision; this request breaks that plan." },
            { "Kapıdaki dış kayıt talebi iptal olur.", "The outside record request at the door is cancelled." },
            { "KABUL TEZGÂHI", "ADMISSION DESK" }, { "başvuru · {0}", "application · {0}" }, { "başvuru yok", "no application" },
            { "BÜYÜTEÇ", "MAGNIFIER" }, { "belgeyle karşılaştır", "compare with the paper" }, { "görüşme", "interview" },
            { "soğuk gözlem", "cold observation" }, { "TALEP DEFTERİ", "REQUEST PAD" }, { "dış kayıt", "outside record" },
            { "KABUL ·\nTAM ERİŞİM", "ADMIT ·\nFULL ACCESS" }, { "KISITLI\nKABUL", "RESTRICTED\nADMISSION" }, { "DIŞARIDAN\nYARDIM", "OUTSIDE\nAID" },
            { "RET", "REJECT" }, { "YARDIM", "AID" }, { "TAM ERİŞİM", "FULL ACCESS" }, { "{0} ERZAK", "{0} RATIONS" },
            { "KAPASİTE {0}/{1}", "CAPACITY {0}/{1}" }, { "CAN {0}/{1} · HAR {2} · GÜÇ {3}", "HP {0}/{1} · MOVE {2} · POWER {3}" },
            { "Belge alanları uyumlu", "Paper fields agree" }, { "Düzenleme tarihi kayıttan farklı", "Issue date differs from the record" },
            { "Belge No. ≠ sevk No.", "Doc No. ≠ transfer No." }, { "Mühür hattın örneğiyle uyuşmuyor", "Seal does not match the line's sample" },
            { "Geldiği yer kayıttan farklı", "Origin differs from the record" }, { "Soğuk gözlem: buğu var", "Cold observation: fog seen" },
            { "Soğuk gözlem: buğu yok", "Cold observation: no fog" }, { "İZLER", "TRACES" }, { "İZ 1 · erişim kaydı", "TRACE 1 · access log" },
            { "İZ 2 · parça kaydı", "TRACE 2 · parts log" }, { "İnceleme fişi açıldı · talimat {0}", "Review slip opened · orders {0}" },
            { "KARAR V.{0:000} · {1}", "DECISION S.{0:000} · {1}" }, { "KISITLI · {0} V", "LIMIT · {0} S" },
        };

        /// <summary>İngilizceye geçer: gerçek tablo, eksikse Aşama 6 sözlüğü.</summary>
        internal static void Use()
        {
            var en = new Dictionary<string, string>();
            if (File.Exists(TablePath))
            {
                var table = Loc.ParseTable(File.ReadAllText(TablePath));
                if (table.TryGetValue(Loc.English, out var t)) foreach (var kv in t) en[kv.Key] = kv.Value;
            }
            foreach (var kv in Stage6) if (!en.ContainsKey(kv.Key) || string.IsNullOrEmpty(en[kv.Key])) en[kv.Key] = kv.Value;
            Loc.SetLanguage(Loc.English, en);
        }
    }
}
