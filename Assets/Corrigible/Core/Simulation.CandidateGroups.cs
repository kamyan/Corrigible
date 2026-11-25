using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>Aday grubunun statik verisi: özellik ve şüphe kimlikleri kayıtta saklanır, metinler gösterimde çevrilir.</summary>
    public static class CandidateData
    {
        public static readonly string[] Benefits = { "kit", "zirh", "devre", "erzak", "usta", "saglam" };
        public static readonly string[] Costs = { "yarali", "obur", "acemi", "yavas" };
        public static readonly string[][] Suspicions =
        {
            new[] { "kefil", "kayit_tam", "sevk_tam" },
            new[] { "kayit_yok", "belge_yanmis", "cift_kayit" },
            new[] { "tek_basina", "yarasiz", "gece_gecis" },
        };
        /// <summary>Kurucu adları (SetupPlanningBoard'da S.Rng ile karıştırılır); aday havuzunda yoktur. İngilizce, çevrilmez.</summary>
        public static readonly string[] Founders = { "Hazel", "Robin", "Margot", "Quinn", "Agnes", "Iris", "Walter", "Victor", "Harold", "Nell" };
        /// <summary>
        /// Aday adları (<c>CandidateName</c> sırayla dağıtır); kurucu adları bu havuzda yoktur. İngilizce, ASCII, çevrilmez. İlk 22'sinin
        /// portresi var (<c>Desk/Data/Rev3/PortraitLibrary.asset</c>); sıra portrelerin kişilerine bağlıdır, değiştirme.
        /// </summary>
        public static readonly string[] Names =
        {
            "Frank", "Edith", "Leo", "Maya", "Owen", "Clara", "Hugo", "Nora", "Felix", "Mabel", "Grant", "Ivy", "Theo", "Ruth", "Albert", "June",
            "Bernard", "Doris", "Milo", "Lena", "Ray", "Daisy", "Simon", "Alice", "Ben", "Grace", "Carl", "Paula", "Dean", "Rose", "Adam", "Wendy",
            "Jack", "Holly", "Neil", "Mona", "Glen", "Lucy", "Roy", "Amy", "Miles", "Elsie", "Sam", "Joan", "Ted", "Fay", "Luke", "Kate",
            "Eli", "Olive", "Rex", "Beth", "Gus", "Pearl", "Nate", "Lily", "Joel", "Ann", "Abe", "Greta", "Cole", "Tina", "Dale", "Eve",
        };

        public static bool IsCost(string perk) => Array.IndexOf(Costs, perk) >= 0;

        public static string TierName(int tier)
        {
            switch (tier)
            {
                case 0: return Loc.T("Kefilli");
                case 1: return Loc.T("Kayıtsız");
                default: return Loc.T("Şüpheli");
            }
        }

        /// <summary>Şüphe satırının o dildeki metni; <paramref name="arg"/> yalnız "kefil" satırında kullanılır.</summary>
        public static string SuspicionLine(string key, string arg)
        {
            switch (key)
            {
                case "kefil": return Loc.F("kefili var: {0}", arg);
                case "kayit_tam": return Loc.T("KYGA kaydı tam");
                case "sevk_tam": return Loc.T("sevk kâğıdı mühürlü ve tam");
                case "kayit_yok": return Loc.T("kaydı yok");
                case "belge_yanmis": return Loc.T("belgesi yanmış");
                case "cift_kayit": return Loc.T("adı listede iki kez geçiyor");
                case "tek_basina": return Loc.T("işgal bölgesinden tek başına geldi");
                case "yarasiz": return Loc.T("üç gün yürümüş, yarası yok");
                case "gece_gecis": return Loc.T("gece devriyesinin arasından geçmiş");
                default: return key;
            }
        }

        public static string PerkName(string perk, Balance b)
        {
            switch (perk)
            {
                case "kit": return Loc.T("Kendi kitiyle gelir");
                case "zirh": return Loc.T("Zırhıyla gelir");
                case "devre": return Loc.F("{0} devre getirir", Math.Max(0, b.perkCircuits));
                case "erzak": return Loc.T("Erzak çuvalıyla gelir");
                case "usta": return Loc.T("Deneyimli");
                case "saglam": return Loc.T("Dayanıklı");
                case "yarali": return Loc.T("Yaralı gelir");
                case "obur": return Loc.T("Çift tayın yer");
                case "acemi": return Loc.T("Eğitimsiz");
                case "yavas": return Loc.T("Aksak");
                default: return perk;
            }
        }

        /// <summary>Özelliğin etkisi; <paramref name="subject"/> adayın konusudur (kitin türü ondan gelir).</summary>
        public static string PerkEffect(string perk, Balance b, string subject)
        {
            switch (perk)
            {
                case "kit": return Loc.F("{0} üzerinde gelir; kişiye bağlıdır.", PlanningBoardData.EquipmentName("kit_" + subject));
                case "zirh": return Loc.T("Üzerinde bir zırh plakası; kişiye bağlıdır.");
                case "devre": return Loc.F("Katıldıktan sonraki {0} kapanışta toplam {1} devre.", Math.Max(1, b.perkCircuitShifts), Math.Max(0, b.perkCircuits));
                case "erzak": return Loc.F("Katıldıktan sonraki {0} kapanışta toplam {1:0.##} erzak.", Math.Max(1, b.perkSupplyShifts), Math.Max(0, b.perkSupplies));
                case "usta": return Loc.T("Ustalığı deneyimli kademeden başlar.");
                case "saglam": return Loc.F("En yüksek can +{0}.", Math.Max(0, b.perkSturdyHp));
                case "yarali": return Loc.F("Canı 1; {0} vardiya yatar.", Math.Max(0, b.perkWoundedShifts));
                case "obur": return Loc.F("Kişi başı tüketimin {0:0.##} katını yer.", Math.Max(1, b.perkDoubleRationFactor));
                case "acemi": return Loc.T("Ustalık puanı 0; işte ve sahada deneyim kazanır.");
                case "yavas": return Loc.F("Hareket −{0}.", Math.Max(0, b.perkSlowMove));
                default: return perk;
            }
        }
    }

    public sealed partial class Simulation
    {
        const int CandidateDraws = 10, CandidateSlots = 3;

        /// <summary>
        /// Kapanışta bir aday grubu üretir. Çekim sırası sabittir ve kayıt biçiminin parçasıdır: sonraki aralık, grup boyu, sonra
        /// HER ZAMAN üç aday için onar çekim (konu, profil, kademe, özellik 1, özellik 2, şüphe satırı, sızmacı, iz gecikmesi,
        /// insan izi, ikinci iz). Bütün çekimler önce yapılır; dönüşümler ve kırpmalar ardından RNG'siz uygulanır.
        /// </summary>
        void GenerateCandidateGroup(bool emergency)
        {
            double uInterval = S.Rng.Next(), uSize = S.Rng.Next();
            var u = new double[CandidateSlots][];
            for (int i = 0; i < CandidateSlots; i++)
            {
                u[i] = new double[CandidateDraws];
                for (int k = 0; k < CandidateDraws; k++) u[i][k] = S.Rng.Next();
            }

            int knockMin = Math.Max(1, B.admissionKnockMin), knockMax = Math.Max(knockMin, B.admissionKnockMax);
            int sizeMin = Math.Max(1, Math.Min(CandidateSlots, B.candidateGroupMin)), sizeMax = Math.Max(sizeMin, Math.Min(CandidateSlots, B.candidateGroupMax));
            int size = emergency ? CandidateSlots : sizeMin + Pick(uSize, sizeMax - sizeMin + 1);
            // Acil geliş olağan aralık sayacına dokunmaz; çekimi yine yapılır ve atılır.
            if (!emergency) Admission.NextArrivalShift = S.Day + knockMin + Pick(uInterval, knockMax - knockMin + 1);

            bool first = Admission.GeneratedGroups == 0;
            var tiers = new int[size];
            for (int i = 0; i < size; i++) tiers[i] = CandidateTier(u[i][2]);
            if (size > 1 && tiers.All(t => t == tiers[0])) tiers[size - 1] = tiers[0] == 1 ? 2 : 1;
            string[] emergencyRoles = PlanningBoardData.FieldJobs
                .OrderBy(r => Board.Equipment.Any(e => e.Kind == "kit_" + r && e.Charges > 0) ? 1 : 0).ToArray();
            var founders = Admission.Files.Where(f => f.Founder && !f.Departed && f.Resident.Alive).Select(f => f.Resident.Name).ToList();

            var group = new List<CommunityApplicant>();
            for (int i = 0; i < size; i++)
            {
                double[] d = u[i];
                int tier = tiers[i];
                string subject = emergency ? emergencyRoles[i % emergencyRoles.Length]
                    : PlanningBoardData.CandidateSubjects[Pick(d[0], PlanningBoardData.CandidateSubjects.Length)];
                int number = Board.NextResidentId++;
                string id = "resident-" + number.ToString(CultureInfo.InvariantCulture);
                var person = MakeBoardResident(id, CandidateName(number, group), subject, Pick(d[1], 4));
                var perks = CandidatePerks(tier, d[3], d[4]);
                NormalizeCandidatePerks(perks, person, emergency);
                // Acil aday çekilen özelliğine EK olarak kişiye bağlı rol kitini taşır.
                if (emergency) perks.Add("kit");
                ApplyCandidatePerks(perks, person);

                string[] lines = CandidateData.Suspicions[tier];
                double scaled = d[5] * lines.Length;
                int line = Math.Min(lines.Length - 1, (int)Math.Floor(scaled));
                string key = lines[line], arg = null;
                if (key == "kefil")
                {
                    if (founders.Count == 0) key = "kayit_tam";
                    else arg = founders[Pick(scaled - line, founders.Count)];
                }

                double p = Math.Min(1, Math.Max(0, B.humanSecondTraceChance));
                bool second = d[9] < p;
                // Aynı çekimden bağımsız ikinci değer: iz bırakma kararı ile aralık birbirini ele vermez.
                double rest = second ? (p <= 0 ? 0 : d[9] / p) : (p >= 1 ? 0 : (d[9] - p) / (1 - p));
                int delayMin = Math.Max(1, B.traceDelayMin), delayMax = Math.Max(delayMin, B.traceDelayMax);
                int gapMin = Math.Max(1, B.secondTraceGapMin), gapMax = Math.Max(gapMin, B.secondTraceGapMax);
                var identity = new AdmissionIdentity
                {
                    Robot = !first && d[6] < CandidateRobotChance(tier),
                    TraceDelay = delayMin + Pick(d[7], delayMax - delayMin + 1),
                    HumanTrace = d[8] < B.humanTraceChance,
                    HumanSecondTrace = second,
                    SecondTraceGap = gapMin + Pick(rest, gapMax - gapMin + 1),
                };
                group.Add(new CommunityApplicant
                {
                    Resident = person, Identity = identity, ArrivalShift = S.Day,
                    Tier = tier, SuspicionKey = key, SuspicionArg = arg, Portrait = id, Perks = perks,
                });
                Admission.GeneratedApplicants++;
            }
            Admission.Group = group;
            Admission.GroupArrivalShift = S.Day;
            Admission.GroupWaitingUntil = S.Day + Math.Max(1, B.candidateWaitShifts);
            Admission.GroupEmergency = emergency;
            Admission.GeneratedGroups++;
        }

        static int Pick(double u, int count) => count <= 1 ? 0 : Math.Min(count - 1, (int)Math.Floor(u * count));

        int CandidateTier(double u)
        {
            double t0 = Math.Max(0, B.candidateTier0Share), t2 = Math.Max(0, B.candidateTier2Share);
            return u < t0 ? 0 : u < 1 - t2 ? 1 : 2;
        }

        double CandidateRobotChance(int tier) => tier == 0 ? B.candidateTier0Robot : tier == 1 ? B.candidateTier1Robot : B.candidateTier2Robot;

        List<string> CandidatePerks(int tier, double first, double second)
        {
            string[] good = CandidateData.Benefits, bad = CandidateData.Costs;
            if (tier == 0) return new List<string> { first < B.candidateTier0CostChance ? bad[Pick(second, bad.Length)] : good[Pick(second, good.Length)] };
            int a = Pick(first, good.Length);
            if (tier == 1) return new List<string> { good[a] };
            int b = Pick(second, good.Length - 1);
            return new List<string> { good[a], good[b >= a ? b + 1 : b] };
        }

        /// <summary>
        /// Çekimden sonraki RNG'siz dönüşümler: saha dışı konuda kit, etkisiz aksaklık, usta ile acemi. Acil grupta kit zaten ek
        /// olarak verilir; çekilen <c>kit</c> saha dışı konudaki kuralla devreye (o da varsa erzağa) çevrilir ki ek getiri kaybolmasın.
        /// </summary>
        void NormalizeCandidatePerks(List<string> perks, ResidentState person, bool emergency)
        {
            if (perks.Contains("kit") && (emergency || !PlanningBoardData.FieldJobs.Contains(person.TrainingSubject)))
            {
                perks.Remove("kit");
                string swap = !perks.Contains("devre") ? "devre" : !perks.Contains("erzak") ? "erzak" : null;
                if (swap != null) perks.Add(swap);
            }
            if (perks.Contains("yavas") && person.Move - Math.Max(0, B.perkSlowMove) < 2)
            {
                perks.Remove("yavas");
                if (!perks.Contains("obur")) perks.Add("obur");
            }
            if (perks.Contains("usta") && perks.Contains("acemi")) perks.RemoveAt(Math.Max(perks.IndexOf("usta"), perks.IndexOf("acemi")));
        }

        void ApplyCandidatePerks(List<string> perks, ResidentState person)
        {
            var skill = person.Experience.First(x => x.Subject == person.TrainingSubject);
            if (perks.Contains("usta")) skill.Points = B.boardExperiencedAt;
            if (perks.Contains("acemi")) skill.Points = 0;
            if (perks.Contains("saglam")) person.MaxHp += Math.Max(0, B.perkSturdyHp);
            if (perks.Contains("yavas")) person.Move -= Math.Max(0, B.perkSlowMove);
            person.Hp = person.MaxHp;
            if (perks.Contains("yarali")) { person.Hp = 1; person.Down = Math.Max(0, B.perkWoundedShifts); }
        }

        string CandidateName(int number, List<CommunityApplicant> group)
        {
            string[] pool = CandidateData.Names;
            string name = pool[(Admission.GeneratedApplicants + (int)(S.Seed % (uint)pool.Length)) % pool.Length];
            bool taken = Admission.GeneratedApplicants >= pool.Length || Board.People.Any(p => p.Name == name)
                || Admission.Files.Any(f => f.Resident.Name == name) || group.Any(c => c.Resident.Name == name);
            return taken ? name + " " + number.ToString(CultureInfo.InvariantCulture) : name;
        }

        // ---------- Kapanış ----------
        /// <summary>
        /// Pano kapanışının arkasında çağrılır. Kapıda grup yoksa: canlı sayısı eşiğin altındaysa acil grup, değilse vadesi gelen
        /// olağan grup üretilir. Canlı kimse kalmadıysa çekim yapılmaz.
        /// </summary>
        void AdvanceCandidateDoor()
        {
            if (Admission == null || Admission.Group.Count > 0) return;
            int alive = Board.People.Count(p => p.Alive);
            if (alive == 0) return;
            if (alive < B.candidateEmergencyBelow) GenerateCandidateGroup(true);
            else if (S.Day >= Admission.NextArrivalShift) GenerateCandidateGroup(false);
        }

        /// <summary>Kapıdaki bütün adayları verilen türle ("pass", "notchosen", "expired") geçmişe yazar ve kapıyı boşaltır.</summary>
        void ClearCandidateGroup(string kind)
        {
            foreach (var c in Admission.Group)
                Admission.Passed.Add(new PassedCandidate { Name = c.Resident.Name, Kind = kind, Tier = c.Tier, Shift = S.Day, Robot = c.Identity.Robot });
            Admission.Group.Clear();
            Admission.GroupEmergency = false;
        }

        // ---------- Komutlar (RNG çekmez) ----------
        /// <summary>Plan kilidi kapıyı kapatmaz: baskın çözüldükten sonra, fiş basılana kadar seçim ve geri çevirme geçerlidir.</summary>
        string CandidateCommandCheck()
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (Admission == null) return Loc.T("Bu kayıtta topluluk kabulü yok.");
            if (S.Raid != null) return Loc.T("Baskın sürüyor.");
            if (Board.Printed) return Loc.T("Fiş basıldı; kapı bu vardiya kapalı.");
            if (Admission.Group.Count == 0) return Loc.T("Kapıda aday yok.");
            return null;
        }

        /// <summary>Yolluk: yollanan kişiyle giden erzak (stok azsa eldeki kadar).</summary>
        public double DismissSuppliesPreview => Shelter == null ? 0 : Math.Min(Shelter.Supplies, Math.Max(0, B.dismissSupplies));

        public string CandidateChoiceCheck(string candidateId, string replaceId)
        {
            string why = CandidateCommandCheck();
            if (why != null) return why;
            if (string.IsNullOrEmpty(candidateId) || !Admission.Group.Any(c => c.Resident.Id == candidateId)) return Loc.T("Bu aday kapıda değil.");
            bool full = Board.People.Count(p => p.Alive) >= B.boardResidentCapacity;
            if (string.IsNullOrEmpty(replaceId))
                return full ? Loc.F("Sığınak dolu ({0}); aday için yollanacak kişiyi seç.", B.boardResidentCapacity) : null;
            if (!full) return Loc.T("Boş yer varken kimse yollanamaz.");
            var f = Admission.File(replaceId);
            if (f == null || f.Departed || !f.Resident.Alive || !Board.People.Contains(f.Resident)) return Loc.T("Yollanacak canlı bir sakin seç.");
            return null;
        }

        /// <summary>
        /// Gruptan bir adayı alır; sığınak doluysa <paramref name="replaceId"/> aynı komutta yolluğuyla gönderilir. Diğer adaylar
        /// gider. Kişi Boşta'ya gelir ve o kapanışın tüketimine katılır; plan kilidine ve fişe dokunulmaz.
        /// </summary>
        public ActResult ChooseCandidate(string candidateId, string replaceId = null)
        {
            string why = CandidateChoiceCheck(candidateId, replaceId);
            if (why != null) return ActResult.Fail(why);
            replaceId = string.IsNullOrEmpty(replaceId) ? null : replaceId;
            var c = Admission.Group.First(x => x.Resident.Id == candidateId);
            string leaver = null;
            if (replaceId != null)
            {
                var gone = Admission.File(replaceId);
                leaver = gone.Resident.Name;
                SendAway(gone, "yollandi", true);
                CloseOrphanShelterEvent();
            }
            var person = c.Resident;
            person.Job = "rest";
            Board.People.Add(person);
            Admission.Files.Add(new ResidentAdmissionFile
            {
                PersonId = candidateId, Resident = person, Identity = c.Identity,
                ArrivalShift = c.ArrivalShift, AcceptedShift = S.Day,
                Tier = c.Tier, SuspicionKey = c.SuspicionKey, SuspicionArg = c.SuspicionArg, Perks = new List<string>(c.Perks),
                CircuitInstallments = c.Perks.Contains("devre") ? Math.Max(1, B.perkCircuitShifts) : 0,
                SupplyInstallments = c.Perks.Contains("erzak") ? Math.Max(1, B.perkSupplyShifts) : 0,
                // Sızma saatinin ilk vadesi: üretimde çekilen gizli gecikme (insan için de yazılır).
                TraceClock = Math.Max(1, c.Identity.TraceDelay),
            });
            // Teçhizat kimliği seçim anında tüketilir; alınmayan aday kimlik harcamaz.
            if (c.Perks.Contains("kit")) AddBoardEquipment("kit_" + person.TrainingSubject, candidateId, true);
            if (c.Perks.Contains("zirh")) AddBoardEquipment("armor", candidateId, true);
            Admission.Group.Remove(c);
            ClearCandidateGroup("notchosen");
            Shelter.Population = Board.People.Count(p => p.Alive);
            return Rec(ActResult.Success(leaver == null
                ? Loc.F("{0} topluluğa katıldı.", person.Name)
                : Loc.F("{0} topluluğa katıldı; {1} yolluğuyla gönderildi.", person.Name, leaver)), "candidate_choose", candidateId, replaceId);
        }

        public string CandidatePassCheck() => CandidateCommandCheck();

        /// <summary>Bütün grubu geri çevirir; ceza yoktur.</summary>
        public ActResult PassCandidates()
        {
            string why = CandidatePassCheck();
            if (why != null) return ActResult.Fail(why);
            ClearCandidateGroup("pass");
            return Rec(ActResult.Success(Loc.T("Adaylar geri çevrildi.")), "candidate_pass");
        }

        // ---------- Yollama ve kişiye bağlı teçhizat ----------
        /// <summary>
        /// Kişi sığınaktan çıkar: panodan düşer, bağlı teçhizatı onunla gider, bağlı olmayanı depoya döner, kalan taksitler kesilir.
        /// <paramref name="rations"/> ise yolluk erzaktan düşer (stok azsa eldeki kadar).
        /// </summary>
        void SendAway(ResidentAdmissionFile f, string reason, bool rations)
        {
            string id = f.PersonId;
            f.Departed = true;
            f.DepartReason = reason;
            f.DepartShift = S.Day;
            f.CircuitInstallments = 0;
            f.SupplyInstallments = 0;
            f.Resident.Job = "rest";
            Board.People.Remove(f.Resident);
            ReleaseResidentEquipment(id, true);
            ClearShelterPersonState(f);
            if (rations) Shelter.Supplies -= DismissSuppliesPreview;
            Shelter.Population = Board.People.Count(p => p.Alive);
        }

        /// <summary>
        /// Kişinin cebini boşaltır: bağlı olmayan eşya depoya döner; kişiye bağlı eşya <paramref name="leaving"/> ise yok olur,
        /// değilse kişide kalır.
        /// </summary>
        void ReleaseResidentEquipment(string id, bool leaving)
        {
            if (leaving) Board.Equipment.RemoveAll(e => e.OwnerId == id && e.Bound);
            foreach (var item in Board.Equipment.Where(e => e.OwnerId == id && !e.Bound)) item.OwnerId = null;
        }
    }
}
