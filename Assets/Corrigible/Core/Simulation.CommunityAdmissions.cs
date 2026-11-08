using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Corrigible.Core
{
    public sealed partial class Simulation
    {
        public CommunityAdmissionState Admission => Board?.Admission;

        static readonly string[] AdmissionNames = { "Rauf", "Meral", "Arda", "Ece", "Murat", "Aylin", "Baran", "Suna", "Eren", "Banu", "Ozan", "Lale", "Sinan", "Asli", "Kerem", "Sema", "Tamer", "Dilan", "Yalcin", "Feride", "Umut", "Ayca", "Levent", "Naz" };
        static readonly string[] AdmissionOrigins = { Loc.N("Kuzey tahliye hattı"), Loc.N("Doğu aktarma merkezi"), Loc.N("Güney sahra istasyonu") };
        static readonly string[] AdmissionSeals = { "KYGA-K", "KYGA-D", "KYGA-G" };

        void SetupAdmissions()
        {
            if (Board == null || !B.planningBoardEnabled) return;
            Board.Admission = new CommunityAdmissionState { NextArrivalShift = S.Day + AdmissionInterval() };
            foreach (var person in Board.People)
            {
                var papers = new AdmissionDocument { Name = person.Name, Profession = person.Profession, Origin = Loc.N("Kurucu sığınak ekibi"), Portrait = person.Id, IssuedShift = S.Day, Serial = "K-" + person.Id, ReferralSerial = "K-" + person.Id, Seal = "KYGA-K" };
                Admission.Files.Add(new ResidentAdmissionFile { PersonId = person.Id, Resident = person, Papers = papers, Identity = FounderIdentity(papers), Founder = true, RecordVerified = true, AcceptedShift = S.Day, ArrivalShift = S.Day });
            }
            Admission.Protocol = MakeAdmissionProtocol(1);
        }

        /// <summary>
        /// Biçim 15 (H3): kurucunun dış kaydı kendi belgesidir. Boş kimlikte kayıtlı mühür null kalıyor, kurucu dosyasının İNCELE'si
        /// yanlış "mühür uyuşmuyor" bulgusu veriyordu. RNG çekmez; kurucu insandır (buğulu).
        /// </summary>
        AdmissionIdentity FounderIdentity(AdmissionDocument papers) => new AdmissionIdentity
        {
            Model = "human", BreathVisible = true, RegisteredOrigin = papers.Origin, RegisteredSerial = papers.Serial,
            RegisteredSeal = "KYGA-K", RegisteredIssueShift = S.Day,
        };

        int AdmissionInterval()
        {
            int min = Math.Max(1, B.admissionKnockMin), max = Math.Max(min, B.admissionKnockMax);
            return min + (int)Math.Floor(S.Rng.Next() * (max - min + 1));
        }

        AdmissionProtocol MakeAdmissionProtocol(int version)
        {
            var p = new AdmissionProtocol { Version = version, IssuedShift = S.Day };
            if (version == 1)
            {
                p.Change = Loc.N("Temel kabul talimatı: belge ve sevk kaydını karşılaştırın.");
                p.Rules.Add(Loc.N("Düzenleme tarihi bugünden ileri olamaz; kimlik ve sevk numaraları aynı olmalıdır."));
                p.Rules.Add(Loc.N("Belge kusuru robot kanıtı değildir. Doğru belge ve iyi çalışma da insanlık garantisi değildir."));
            }
            else if (version == 2)
            {
                p.Change = Loc.N("Yeni istihbarat: N-17 boş sevk belgeleri çalındı; yalnız bu serideki eski dosyalar işaretlendi.");
                p.Rules.Add(Loc.N("N-17 serisi bağımsız dış kayıtla teyit edilmelidir; serinin kendisi robot kanıtı değildir."));
                p.Rules.Add(Loc.N("Soğuk gözlemde S-6 gövdelerinde nefes buğusu yoktur. Bu işaret bütün robot serilerini kapsamaz."));
                p.Rules.Add(Loc.N("Belge kusurunu, dış kaydı ve erişim izlerini birlikte değerlendirin."));
            }
            else
            {
                p.Change = Loc.N("Yeni üretim kaydı: R-4 serileri nefes buğusunu taklit edebilir; önceki S-6 bilgisi geçerlidir.");
                p.Rules.Add(Loc.N("N-17 serisi bağımsız dış kayıtla teyit edilmelidir; serinin kendisi robot kanıtı değildir."));
                p.Rules.Add(Loc.N("Nefes buğusu insanlık garantisi değildir. R-4 serileri bu işareti taklit eder."));
                p.Rules.Add(Loc.N("Birbirinden bağımsız erişim ve parça kayıtları kayıp tanık olsa da dosyada kalır."));
            }
            return p;
        }

        string AdmissionCommandCheck()
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (Admission == null) return Loc.T("Bu kayıtta topluluk kabulü yok.");
            if (S.Raid != null) return Loc.T("Baskın sürüyor.");
            if (Board.Locked) return Loc.T("Vardiya planı kilitli.");
            return null;
        }

        public string ApplicantInspectionCheck(string kind)
        {
            string why = AdmissionCommandCheck();
            if (why != null) return why;
            if (Admission.Candidate == null) return Loc.T("Kapıda aday yok.");
            if (kind != "papers" && kind != "interview" && kind != "breath" && kind != "record") return Loc.T("Böyle bir inceleme yok.");
            if (Admission.Candidate.Inspections.Contains(kind)) return Loc.T("Bu inceleme zaten yapıldı.");
            if (kind == "record" && !HasAdmissionWatcher(Admission.Candidate.Resident.Id)) return Loc.T("Gözetmen yok: dış kayıt için Boşta'da ayrı, sağlam ve kısıtsız bir sakin bırak.");
            return null;
        }

        public ActResult InspectApplicant(string kind)
        {
            string why = ApplicantInspectionCheck(kind);
            if (why != null) return ActResult.Fail(why);
            var c = Admission.Candidate;
            c.Inspections.Add(kind);
            if (kind == "papers") c.PaperFinding = CompareAdmissionPapers(c.Papers, c.Identity);
            else if (kind == "interview")
            {
                if (c.Identity.RegisteredOrigin == AdmissionOrigins[0]) c.Interview = Loc.N("Tahliye yolunu sordun. Aday Kuzey tahliye hattından geldiğini söyledi. Beyanı belgeyle karşılaştır; bu yanıt insanlık kanıtı değildir.");
                else if (c.Identity.RegisteredOrigin == AdmissionOrigins[1]) c.Interview = Loc.N("Tahliye yolunu sordun. Aday Doğu aktarma merkezinden geldiğini söyledi. Beyanı belgeyle karşılaştır; bu yanıt insanlık kanıtı değildir.");
                else c.Interview = Loc.N("Tahliye yolunu sordun. Aday Güney sahra istasyonundan geldiğini söyledi. Beyanı belgeyle karşılaştır; bu yanıt insanlık kanıtı değildir.");
            }
            else if (kind == "breath") c.Breath = c.Identity.BreathVisible
                ? Loc.N("Soğuk gözlemde nefes buğusu görüldü. Bu sonuç bütün robot serilerini dışlamaz.")
                : Loc.N("Soğuk gözlemde nefes buğusu görülmedi. S-6 talimatıyla karşılaştırın; tek başına kesin hüküm vermeyin.");
            else
            {
                // Biçim 15 (H1): gözetim tek bir kişi-vardiyadır; aday talebi bekleyen dosya incelemesini iptal eder (ReviewResident ile aynı),
                // yoksa dosyanın ReviewPending'i sonsuza dek takılı kalıyordu.
                foreach (var f in Admission.Files) f.ReviewPending = false;
                Admission.ObservationTarget = c.Resident.Id;
            }
            return Rec(ActResult.Success(kind == "record" ? Loc.T("Dış kayıt teyidi bu vardiyanın gözetim işine yazıldı; sonuç sonraki brifingde.") : Loc.T("İnceleme dosyaya işlendi.")), "inspectApplicant", kind);
        }

        /// <summary>
        /// Belge karşılaştırmasının ilk uyuşmazlığı, kod olarak: "date" | "serial" | "seal" | "origin" | "match" (sıra metinlerle aynı).
        /// Sunum bulgu damgasını metinden çıkaramaz (D4); metin de bu koddan türer.
        /// </summary>
        static string PaperCode(AdmissionDocument p, AdmissionIdentity identity)
        {
            if (p.IssuedShift != identity.RegisteredIssueShift) return "date";
            if (p.Serial != p.ReferralSerial) return "serial";
            if (p.Seal != identity.RegisteredSeal) return "seal";
            if (p.Origin != identity.RegisteredOrigin) return "origin";
            return "match";
        }

        static string CompareAdmissionPapers(AdmissionDocument p, AdmissionIdentity identity)
        {
            switch (PaperCode(p, identity))
            {
                case "date": return Loc.N("Düzenleme tarihi dış kayıttan farklı. Belge kusuru robot kanıtı değildir.");
                case "serial": return Loc.N("Kimlik ve sevk numaraları uyuşmuyor. Kopyalama hatası da olabilir; dış kayıtla teyit edin.");
                case "seal": return Loc.N("Düzenleyen mühür bu tahliye hattının örneğiyle uyuşmuyor. Bu bir belge kusurudur.");
                case "origin": return Loc.N("Geldiği yer sevk kaydından farklı. Kişinin robot olduğu sonucu çıkarılamaz.");
                default: return Loc.N("Kimlik ve sevk alanları birbiriyle uyumlu. Geçerli belge insanlık garantisi değildir.");
            }
        }

        /// <summary>
        /// Yapılmış bir incelemenin sonucu, kod olarak (salt okunur; RNG çekmez, durum değiştirmez; D4/D5). Yalnız oyuncunun o
        /// incelemenin metninde zaten gördüğünü verir; yapılmamış incelemede null. <paramref name="personId"/> kapıdaki aday ya da
        /// kişi dosyası. Türler: <c>"papers"</c> → <see cref="PaperCode"/> ("papers" yapıldıysa); <c>"record"</c> → <see cref="PaperCode"/>
        /// (adayda "record_done"; dosyada "record_done" ya da gözetimli dosya incelemesi tamamlandıysa; kurucularda ikisi de yok → null);
        /// <c>"breath"</c> → "fog" | "clear"; <c>"interview"</c> → "north" | "east" | "south" (adayın söylediği hat).
        /// </summary>
        public string AdmissionFindingCode(string personId, string kind)
        {
            if (Admission == null || string.IsNullOrEmpty(personId)) return null;
            List<string> done;
            AdmissionDocument papers;
            AdmissionIdentity identity;
            bool recordDone;
            var c = Admission.Candidate;
            if (c != null && c.Resident != null && c.Resident.Id == personId)
            {
                done = c.Inspections; papers = c.Papers; identity = c.Identity;
                recordDone = done != null && done.Contains("record_done");
            }
            else
            {
                var f = Admission.File(personId);
                if (f == null) return null;
                done = f.Inspections; papers = f.Papers; identity = f.Identity;
                // Aday döneminde teyit edilen dosyada LastObservedShift −1 kalır; yalnız dosya incelemesi onu yazar.
                recordDone = (done != null && done.Contains("record_done")) || f.LastObservedShift >= 0;
            }
            if (papers == null || identity == null) return null;
            switch (kind)
            {
                case "papers": return done != null && done.Contains("papers") ? PaperCode(papers, identity) : null;
                case "record": return recordDone ? PaperCode(papers, identity) : null;
                case "breath": return done != null && done.Contains("breath") ? (identity.BreathVisible ? "fog" : "clear") : null;
                case "interview":
                    if (done == null || !done.Contains("interview")) return null;
                    // InspectApplicant'ın görüşme metniyle aynı dallanma (bilinmeyen hat Güney metnini alır)
                    if (identity.RegisteredOrigin == AdmissionOrigins[0]) return "north";
                    if (identity.RegisteredOrigin == AdmissionOrigins[1]) return "east";
                    return "south";
                default: return null;
            }
        }

        public string ApplicantDecisionCheck(string choice)
        {
            string why = AdmissionCommandCheck();
            if (why != null) return why;
            if (Admission.Candidate == null) return Loc.T("Kapıda aday yok.");
            if (choice != "admit" && choice != "restricted" && choice != "reject" && choice != "aid") return Loc.T("Böyle bir kabul kararı yok.");
            if ((choice == "admit" || choice == "restricted") && Board.People.Count(p => p.Alive) >= B.boardResidentCapacity)
                return Loc.F("Topluluk kapasitesi {0}; kabul için önce güvenli ayrılma kararı ver.", B.boardResidentCapacity);
            if (choice == "restricted" && Board.People.Any(p => p.Alive && p.Restricted)) return Loc.T("Tek gözetim yeri dolu; önce mevcut kısıtlı kabulü çöz.");
            if (choice == "aid" && Shelter.Supplies < B.admissionAidRations) return Loc.F("Dış yardım için {0} erzak gerekiyor.", B.admissionAidRations);
            return null;
        }

        public string ApplicantDecisionPreview(string choice)
        {
            if (choice == "restricted") return Loc.F("Kritik saha, üretim ve temas erişimi kapalı. Çalışma için ayrı gözetmen gerekir; {0} vardiya sonra erişim kararını yenile.", B.admissionRestrictedShifts);
            if (choice == "aid") return Loc.F("Dışarıdan {0} erzak verilir; aday topluluğa katılmaz.", B.admissionAidRations);
            if (choice == "admit") return Loc.T("Aday aynı kimlik ve gerçek yetenekleriyle topluluğa katılır; belge geçerliliği insanlık garantisi değildir.");
            return Loc.T("Aday dış tahliye hattına yönlendirilir; bu vardiya kapanınca bekleyen karar da ret sayılır.");
        }

        public ActResult DecideApplicant(string choice)
        {
            string why = ApplicantDecisionCheck(choice);
            if (why != null) return ActResult.Fail(why);
            FinishApplicant(choice, false);
            return Rec(ActResult.Success(Loc.T("Kapı kararı kaydedildi.")), "decideApplicant", choice);
        }

        void FinishApplicant(string choice, bool automatic)
        {
            var c = Admission.Candidate;
            if (c == null) return;
            if (choice == "admit" || choice == "restricted")
            {
                c.Resident.Job = "rest";
                c.Resident.Restricted = choice == "restricted";
                Board.People.Add(c.Resident);
                Admission.Files.Add(new ResidentAdmissionFile
                {
                    PersonId = c.Resident.Id, Resident = c.Resident, Papers = c.Papers, Identity = c.Identity,
                    AcceptedShift = S.Day, ArrivalShift = c.ArrivalShift,
                    RestrictedUntil = choice == "restricted" ? S.Day + B.admissionRestrictedShifts : -1,
                    ReviewPending = c.Inspections.Contains("record") && !c.Inspections.Contains("record_done"),
                    RecordVerified = c.Inspections.Contains("record_done"), RecordFinding = c.PaperFinding,
                    PaperFinding = c.PaperFinding, Interview = c.Interview, Breath = c.Breath,
                    Inspections = new List<string>(c.Inspections), LastReviewedClues = c.Inspections.Contains("record_done") ? 0 : -1
                });
                Shelter.Population = Board.People.Count(p => p.Alive);
            }
            else if (choice == "aid") Shelter.Supplies = Math.Max(0, Shelter.Supplies - B.admissionAidRations);
            AddAdmissionEvent(c.Resident.Id, automatic ? "default_reject" : choice, automatic
                ? Loc.N("Bekleyen kapı kararı vardiya kapanışında ret sayıldı; aday dış tahliye hattına yönlendirildi.")
                : choice == "admit" ? Loc.N("Aday tam erişimle kabul edildi; belgeleri kalıcı kişi dosyasına taşındı.")
                : choice == "restricted" ? Loc.N("Aday kısıtlı kabul edildi; kritik görevler kapalı, çalışma ayrı gözetmen gerektiriyor.")
                : choice == "aid" ? Loc.N("Adaya dışarıdan erzak verildi; topluluğa katılmadı.")
                : Loc.N("Aday dış tahliye hattına yönlendirildi."));
            if (choice != "admit" && choice != "restricted" && Admission.ObservationTarget == c.Resident.Id) Admission.ObservationTarget = null;
            Admission.Candidate = null;
        }

        /// <summary>
        /// K4-A (biçim 15): dış kaydın ve dosya incelemesinin gözetmeni Boşta'nın ilk uygun kişisi, <c>Board.People</c> sırasıyla:
        /// canlı, yatmıyor, kısıtsız, hedefin kendisi değil ve görevi <c>"rest"</c> (emekli <c>"watch"</c> görevi artık sayılmaz, D1).
        /// Kural dinamiktir (D2): komut anında, kapanış başında (kilitli planla) ve plan denetiminde yeniden okunur. Sunum gözetmenin
        /// adını her zaman buradan okur; Boşta destesinin üst kartından türetmez (deste hedefi dışlamaz, emekli görevlileri de gösterir).
        /// </summary>
        public ResidentState AdmissionWatcher(string targetId) =>
            Board?.People.FirstOrDefault(p => p.Alive && p.Down <= 0 && !p.Restricted && p.Id != targetId && p.Job == "rest");

        public bool HasAdmissionWatcher(string targetId) => AdmissionWatcher(targetId) != null;
        public bool IsResidentObserved(string id) => Admission != null && Admission.ObservationTarget == id && HasAdmissionWatcher(id);

        public string ResidentReviewCheck(string id)
        {
            string why = AdmissionCommandCheck();
            if (why != null) return why;
            var file = Admission.File(id);
            if (file == null || file.Departed || !file.Resident.Alive) return Loc.T("İncelenebilen canlı bir sakin seç.");
            if (file.ReviewPending) return Loc.T("Bu dosyanın incelemesi zaten bekliyor.");
            if (file.RecordVerified && file.LastReviewedClues == file.ClueCount && !file.ReviewNeeded && (!file.Resident.Restricted || Admission.ObservationTarget == id)) return Loc.T("Bu dosyada yeni incelenecek bulgu yok.");
            if (!HasAdmissionWatcher(id)) return Loc.T("Gözetmen yok: dış kayıt için Boşta'da ayrı, sağlam ve kısıtsız bir sakin bırak.");
            return null;
        }

        public ActResult ReviewResident(string id)
        {
            string why = ResidentReviewCheck(id);
            if (why != null) return ActResult.Fail(why);
            // Gözetim tek bir gerçek kişi-vardiya kullanır; yeni hedef önceki bekleyen emri iptal eder.
            foreach (var other in Admission.Files) other.ReviewPending = false;
            Admission.ObservationTarget = id;
            Admission.File(id).ReviewPending = true;
            return Rec(ActResult.Success(Loc.T("Dosya incelemesi gözetmenin vardiyasına yazıldı; sonuç sonraki brifingde.")), "reviewResident", id);
        }

        public string ResidentRestrictionCheck(string id, bool restricted = true)
        {
            string why = AdmissionCommandCheck();
            if (why != null) return why;
            var f = Admission.File(id);
            if (f == null || f.Departed || !f.Resident.Alive) return Loc.T("Erişimi değiştirilebilen canlı bir sakin seç.");
            if (f.Resident.Restricted == restricted && (restricted ? f.RestrictedUntil > S.Day : true)) return Loc.T("Bu erişim kararı zaten uygulanıyor.");
            if (restricted && Board.People.Any(p => p.Alive && p.Restricted && p.Id != id)) return Loc.T("Tek gözetim yeri dolu; önce mevcut kısıtlı kabulü çöz.");
            return null;
        }

        public ActResult RestrictResident(string id, bool restricted = true)
        {
            string why = ResidentRestrictionCheck(id, restricted);
            if (why != null) return ActResult.Fail(why);
            var f = Admission.File(id);
            f.Resident.Restricted = restricted;
            f.Resident.Job = "rest";
            if (restricted) foreach (var item in Board.Equipment.Where(e => e.OwnerId == id)) item.OwnerId = null;
            f.RestrictedUntil = restricted ? S.Day + B.admissionRestrictedShifts : -1;
            f.Contained = restricted && f.ClueCount >= 2;
            ResolveAdmissionEvents(id, "restriction_expired");
            AddAdmissionEvent(id, restricted ? "restricted" : "released", restricted
                ? Loc.N("Erişim kısıtlandı; kişi yedeğe alındı. Çalışması ayrı gözetmen gerektirir.")
                : Loc.N("Kişinin erişim kısıtı kaldırıldı; yeni görev açıkça seçilmelidir."));
            return Rec(ActResult.Success(Loc.T("Erişim kararı kaydedildi.")), "restrictResident", id, restricted ? "1" : "0");
        }

        public string ResidentDismissCheck(string id)
        {
            string why = AdmissionCommandCheck();
            if (why != null) return why;
            var f = Admission.File(id);
            if (f == null || f.Departed || !f.Resident.Alive) return Loc.T("Ayrılabilecek canlı bir sakin seç.");
            return null;
        }

        public ActResult DismissResident(string id)
        {
            string why = ResidentDismissCheck(id);
            if (why != null) return ActResult.Fail(why);
            var f = Admission.File(id);
            f.Departed = true;
            f.Resident.Job = "rest";
            Board.People.Remove(f.Resident);
            foreach (var item in Board.Equipment.Where(e => e.OwnerId == id)) item.OwnerId = null;
            if (Admission.ObservationTarget == id) Admission.ObservationTarget = null;
            Shelter.Population = Board.People.Count(p => p.Alive);
            ResolveAdmissionEvents(id, null);
            AddAdmissionEvent(id, "departed", Loc.N("Kişi güvenli dış tahliye hattına yönlendirildi; kişi ve ustalığı artık aktif kadroda değil."));
            return Rec(ActResult.Success(Loc.T("Ayrılma kararı kaydedildi.")), "dismissResident", id);
        }

        public string ResidentAdmissionRisk(string id)
        {
            var f = Admission?.File(id);
            if (f == null || f.Departed || !f.Resident.Alive) return null;
            if (f.Resident.Restricted && f.RestrictedUntil <= S.Day) return Loc.T("Kısıtlı kabul süresi doldu; erişimi yenile, serbest bırak veya ayrılma kararı ver. Bu sürede çalışamaz.");
            if (f.ClueCount >= 2 && !f.Contained && !f.Sabotaged) return Loc.T("İki bağımsız bulgu var. Sonraki kapanıştan önce erişimi kısıtlayabilir, hassas görevini kaldırabilir veya kişiyi ayırabilirsin.");
            return f.ReviewNeeded ? Loc.T("Güncel talimat bu eski dosyayla eşleşti; yalnız bu kişiye inceleme fişi açıldı.") : null;
        }

        public bool CanRestrictedResidentWork(string id)
        {
            var f = Admission?.File(id);
            return f != null && f.RestrictedUntil > (Board.Filing ? S.Day - 1 : S.Day) && IsResidentObserved(id);
        }

        /// <summary>Gün arttıktan sonra, pano işlerini ödemeden önce çağrılır; gözetim aynı vardiya bütçesini okur.</summary>
        void AdvanceAdmissions()
        {
            if (Admission == null) return;
            CompleteAdmissionObservation();
            foreach (var f in Admission.Files.ToArray())
            {
                if (f.Departed || !f.Resident.Alive) continue;
                if (f.Resident.Restricted && f.RestrictedUntil < S.Day)
                {
                    f.Resident.Job = "rest";
                    if (!Admission.Events.Any(e => e.PersonId == f.PersonId && e.Kind == "restriction_expired" && !e.Resolved))
                        AddAdmissionEvent(f.PersonId, "restriction_expired", Loc.N("Kısıtlı kabul süresi doldu; çalışma durdu. Erişim kararını yenile, serbest bırak veya ayrılma kararı ver."));
                }
                AdvanceInfiltrator(f);
            }
            int interval = Math.Max(1, B.admissionProtocolInterval);
            int version = Math.Min(3, 1 + S.Day / interval);
            if (version > Admission.Protocol.Version)
            {
                int previousVersion = Admission.Protocol.Version;
                Admission.ProtocolArchive.Add(Admission.Protocol);
                Admission.Protocol = MakeAdmissionProtocol(version);
                AddAdmissionEvent(null, "protocol", Admission.Protocol.Change);
                foreach (var f in Admission.Files.Where(x => !x.Founder && !x.Departed && x.Resident.Alive && ((previousVersion < 2 && x.Papers.Serial.StartsWith("N-17/", StringComparison.Ordinal)) || (version >= 3 && x.Inspections.Contains("breath")))))
                {
                    f.ReviewNeeded = true;
                    AddAdmissionEvent(f.PersonId, "file_review", previousVersion < 2 && f.Papers.Serial.StartsWith("N-17/", StringComparison.Ordinal)
                        ? Loc.N("Yeni talimattaki N-17 serisi bu kişinin eski belgesiyle eşleşti. Bu eşleşme robot kanıtı değildir.")
                        : Loc.N("Yeni talimat eski nefes gözleminin sınırını açıkladı; ilgili kişinin dosyasına inceleme fişi eklendi."));
                }
            }
            if (Admission.Candidate != null && S.Day >= Admission.Candidate.WaitingUntil) FinishApplicant("reject", true);
            int contactAdvance = Board.People.Any(p => p.Alive && p.Hp > 0 && p.Down <= 0 && !p.Restricted && p.Job == "contact")
                ? Math.Max(0, B.admissionContactAdvance) : 0;
            if (Admission.Candidate == null && S.Day + contactAdvance >= Admission.NextArrivalShift) GenerateApplicant();
        }

        void CompleteAdmissionObservation()
        {
            string id = Admission.ObservationTarget;
            if (id == null || !HasAdmissionWatcher(id)) return;
            var c = Admission.Candidate;
            if (c != null && c.Resident.Id == id && c.Inspections.Contains("record") && c.WaitingUntil <= S.Day)
            {
                c.WaitingUntil = S.Day + 1;
                c.PaperFinding = CompareAdmissionPapers(c.Papers, c.Identity);
                c.Inspections.Add("record_done");
                AddAdmissionEvent(id, "record", Loc.N("Dış kayıt karşılaştırması tamamlandı; belge bulgusu dosyada. Bu kontrol insanlık sertifikası değildir."));
                // Sonuç için yalnız bir ek karar vardiyası; gözetmen sonsuz bekleme yaratamaz.
                Admission.ObservationTarget = null;
                return;
            }
            var f = Admission.File(id);
            if (f == null || f.Departed || !f.Resident.Alive || !f.ReviewPending) return;
            f.ReviewPending = false;
            f.RecordVerified = true;
            f.ReviewNeeded = false;
            f.LastObservedShift = S.Day;
            f.RecordFinding = CompareAdmissionPapers(f.Papers, f.Identity);
            if (f.Identity.Robot && f.ClueCount >= 1 && f.ClueCount < 2) AddInfiltratorClue(f);
            if (f.Identity.Robot && f.ClueCount >= 2)
            {
                f.Contained = true;
                f.Resident.Restricted = true;
                f.Resident.Job = "rest";
                foreach (var item in Board.Equipment.Where(e => e.OwnerId == id)) item.OwnerId = null;
                f.RestrictedUntil = S.Day + B.admissionRestrictedShifts;
                AddAdmissionEvent(id, "contained", Loc.N("Dış kayıt ve kalıcı erişim izi birlikte doğrulandı; sabotaj hazırlığı kesildi, kritik erişim kapatıldı."));
            }
            f.LastReviewedClues = f.ClueCount;
            ResolveAdmissionEvents(id, "file_review");
            AddAdmissionEvent(id, "record", Loc.N("Dış kayıt karşılaştırması tamamlandı; belge bulgusu dosyada. Bu kontrol insanlık sertifikası değildir."));
        }

        void AdvanceInfiltrator(ResidentAdmissionFile f)
        {
            if (!f.Identity.Robot || f.Contained || f.Sabotaged || f.AcceptedShift >= S.Day) return;
            string job = f.Resident.Job;
            bool access = job != "rest" && job != "train" && job != "watch" && f.Resident.Down <= 0;
            if (f.Resident.Restricted && !CanRestrictedResidentWork(f.PersonId)) access = false;
            if (!access) return;
            f.LastAccessJob = job;
            f.ActiveShifts++;
            // İlk tam vardiya normal katkıdır. Ardından iki kalıcı kayıt ve tam bir müdahale aralığı vardır.
            if (f.ActiveShifts <= Math.Max(1, B.admissionSettleShifts)) return;
            if (f.ClueCount < 2)
            {
                if (f.LastClueShift < 0 || S.Day - f.LastClueShift >= Math.Max(1, B.admissionClueGap)) AddInfiltratorClue(f);
                return;
            }
            if (S.Day - f.LastClueShift < Math.Max(1, B.admissionClueGap)) return;
            double scale = f.Resident.Restricted ? B.admissionSabotageRestrictedScale : 1;
            if (job == "craft" || job == "tek" || job == "emp" || job == "ana")
            {
                int loss = (int)Math.Ceiling(B.admissionSabotageMaterials * scale);
                var metal = Board.Material("metal");
                int available = BoardMaterialFree("metal");
                if (metal != null) metal.Quantity -= Math.Min(available, loss);
                AddAdmissionEvent(f.PersonId, "sabotage", Loc.N("Önceden iz bırakan parça müdahalesi gerçekleşti; yalnız metal deposu zarar gördü. Kimse ölmedi."));
            }
            else
            {
                Shelter.Supplies = Math.Max(0, Shelter.Supplies - B.admissionSabotageRations * scale);
                AddAdmissionEvent(f.PersonId, "sabotage", Loc.N("Önceden iz bırakan stok yönlendirmesi gerçekleşti; yalnız erzak kaybedildi. Kimse ölmedi."));
            }
            f.Sabotaged = true;
            f.Resident.Job = "rest";
            f.Resident.Restricted = true;
            foreach (var item in Board.Equipment.Where(e => e.OwnerId == f.PersonId)) item.OwnerId = null;
            f.RestrictedUntil = S.Day + B.admissionRestrictedShifts;
        }

        void AddInfiltratorClue(ResidentAdmissionFile f)
        {
            f.ClueCount++;
            f.LastClueShift = S.Day;
            AddAdmissionEvent(f.PersonId, f.ClueCount == 1 ? "clue_access" : "clue_component", f.ClueCount == 1
                ? Loc.N("Görev dışı erişim kaydı bulundu. Kayıt dosyaya kopyalandı; görev değişikliği veya dış kayıt incelemesi mümkün.")
                : Loc.N("İkinci bağımsız bulgu: bakım defteriyle uyuşmayan köprü parçası bulundu. Erişim günlüğü ve parça kaydı tanıktan bağımsız saklandı. Sonraki kapanıştan önce müdahale edebilirsin."));
        }

        void GenerateApplicant()
        {
            int number = Board.NextResidentId++;
            string id = "resident-" + number.ToString(CultureInfo.InvariantCulture);
            string name = AdmissionNames[(Admission.GeneratedApplicants + (int)(S.Seed % (uint)AdmissionNames.Length)) % AdmissionNames.Length];
            if (Admission.GeneratedApplicants >= AdmissionNames.Length || Admission.Files.Any(f => f.Resident.Name == name)) name = name + " " + number.ToString(CultureInfo.InvariantCulture);
            var person = CreateAdmissionResident(id);
            person.Name = name;
            person.Job = "rest";
            bool first = Admission.GeneratedApplicants == 0;
            bool robot = !first && S.Rng.Next() < B.admissionRobotChance;
            bool fault = !first && S.Rng.Next() < B.admissionPaperFaultChance;
            int origin = (int)Math.Floor(S.Rng.Next() * AdmissionOrigins.Length);
            string series = S.Rng.Next() < 0.5 ? "N-17/" : "T-04/";
            string serial = series + number.ToString("D3", CultureInfo.InvariantCulture);
            string model = robot ? (S.Rng.Next() < 0.5 ? "S-6" : "R-4") : "human";
            var identity = new AdmissionIdentity { Robot = robot, PaperFault = fault, Model = model, BreathVisible = model != "S-6", RegisteredOrigin = AdmissionOrigins[origin], RegisteredSerial = serial, RegisteredSeal = AdmissionSeals[origin], RegisteredIssueShift = S.Day };
            var papers = new AdmissionDocument { Name = person.Name, Profession = person.Profession, Origin = identity.RegisteredOrigin, Portrait = id, IssuedShift = S.Day, Serial = serial, ReferralSerial = serial, Seal = identity.RegisteredSeal };
            if (fault)
            {
                int type = (int)Math.Floor(S.Rng.Next() * 4);
                if (type == 0) papers.IssuedShift = S.Day + 1;
                else if (type == 1) papers.Serial = series + (number + 1).ToString("D3", CultureInfo.InvariantCulture);
                else if (type == 2) papers.Seal = AdmissionSeals[(origin + 1) % AdmissionSeals.Length];
                else papers.Origin = AdmissionOrigins[(origin + 1) % AdmissionOrigins.Length];
            }
            Admission.Candidate = new CommunityApplicant { Resident = person, Identity = identity, Papers = papers, ArrivalShift = S.Day, WaitingUntil = S.Day + 1 };
            Admission.GeneratedApplicants++;
            Admission.NextArrivalShift = S.Day + AdmissionInterval();
            AddAdmissionEvent(id, "arrival", first ? Loc.N("İlk aday kapıda; güvenli öğretici vakada kimlik ve sevk belgesini karşılaştırabilirsin.") : Loc.N("Kapıda yeni bir aday var; belge dosyası ve güncel kabul talimatı hazır."));
        }

        void AddAdmissionEvent(string id, string kind, string text)
        {
            Admission.Events.Add(new AdmissionEvent { PersonId = id, Kind = kind, Text = text, Shift = S.Day });
        }

        void ResolveAdmissionEvents(string id, string kind)
        {
            foreach (var e in Admission.Events.Where(e => e.PersonId == id && (kind == null || e.Kind == kind))) e.Resolved = true;
        }
    }
}
