using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Sığınak olayı testlerinin ortak kurulumu. Test derlemesi Core'un iç (internal) alanlarını göremez: gizli kimlik yalnız
    /// olasılıkları 0/1 yaparak, zincir yalnız davranışla sınanır. Public durum (erzak, alarm, güven, yatış) doğrudan yazılabilir;
    /// kayıt testleri yalnız komut kullanır.
    /// </summary>
    internal static class ShelterEventKit
    {
        static readonly Dictionary<string, string> LimitNames = new Dictionary<string, string>
        {
            { "buyuk_hirsizlik", "BigTheft" }, { "kacak", "Deserter" }, { "erzak_azaliyor", "LowRation" }, { "hirsizlik", "Theft" },
            { "agir_yarali", "Wounded" }, { "kavga", "Feud" }, { "anma", "Memorial" }, { "karartma", "Blackout" },
            { "takas", "Trade" }, { "cagri", "Call" }, { "konvoy", "Convoy" },
        };

        /// <summary>Olayın tekrar sınırını ve beklemesini yazar (alan adları <c>event…Max</c>, <c>event…Cooldown</c>).</summary>
        public static Balance Limit(Balance b, string kind, int max, int cooldown = 0)
        {
            string name = LimitNames[kind];
            typeof(Balance).GetField("event" + name + "Max").SetValue(b, max);
            typeof(Balance).GetField("event" + name + "Cooldown").SetValue(b, cooldown);
            return b;
        }

        /// <summary>
        /// Olaylar açık, olağan olayların hepsi susturulmuş (sınır 0; affın vadeli sonucu olan büyük hırsızlık dışında). Alarm ve açlıktan ayrılma kapalı, acil grup yok, erzak ve fon
        /// bol, aralık 1 vardiya; İkmal erzak ve hurda getirmez (erzak değişimi yalnız tüketimdir). Kapı <paramref name="knock"/> = 100
        /// iken kapalıdır.
        /// </summary>
        public static Balance Quiet(int knock = 100)
        {
            var b = new Balance
            {
                shelterEventsEnabled = true, planningBoardEnabled = true, liberationAlarmPerShift = 0, liberationAlarmPerFailure = 0,
                admissionKnockMin = knock, admissionKnockMax = knock, boardStartSupplies = 60, boardSuppliesCapacity = 100, startFon = 900,
                eventGapMin = 1, eventGapMax = 1, desertStreak = 0, candidateEmergencyBelow = 0, candidateTier0CostChance = 0,
                boardGatherSupplies = 0, boardGatherSuppliesPerTier = 0, boardGatherMetal = 0, boardGatherCircuits = 0,
            };
            // Büyük hırsızlık kendiliğinden tetiklenmez (yalnız affın vadeli sonucu); varsayılan sınırı kalır.
            foreach (string kind in ShelterEventData.Ordinary) if (kind != "buyuk_hirsizlik") Limit(b, kind, 0);
            return b;
        }

        /// <summary>Yalnız verilen olağan olaylar açık (sınır 9, bekleme yok).</summary>
        public static Balance On(params string[] kinds)
        {
            var b = Quiet();
            foreach (string kind in kinds) Limit(b, kind, 9);
            return b;
        }

        /// <summary>İlk gruptan sonraki her aday sızmacıdır; insan izi yok.</summary>
        public static Balance Robots(Balance b, int delay = 1, int gap = 1)
        {
            b.candidateTier0Robot = b.candidateTier1Robot = b.candidateTier2Robot = 1;
            b.humanTraceChance = 0;
            b.humanSecondTraceChance = 0;
            b.traceDelayMin = b.traceDelayMax = delay;
            b.secondTraceGapMin = b.secondTraceGapMax = gap;
            return b;
        }

        /// <summary>Hiçbir aday sızmacı değildir; insan izi ve ikinci izi verilen olasılıkla.</summary>
        public static Balance Humans(Balance b, double trace, double second, int delay = 1, int gap = 1)
        {
            b.candidateTier0Robot = b.candidateTier1Robot = b.candidateTier2Robot = 0;
            b.humanTraceChance = trace;
            b.humanSecondTraceChance = second;
            b.traceDelayMin = b.traceDelayMax = delay;
            b.secondTraceGapMin = b.secondTraceGapMax = gap;
            return b;
        }

        public static Simulation New(Balance balance, uint seed = 79) => Simulation.NewGame(seed, balance);

        public static void Close(Simulation sim)
        {
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }

        public static string Pending(Simulation sim) => sim.PendingShelterEvent?.Kind ?? "-";

        /// <summary>Vardiyayı kapatır ve açılan olayı doğrular ("-" = olay yok).</summary>
        public static ShelterEventInstance CloseTo(Simulation sim, string kind)
        {
            Close(sim);
            Assert.AreEqual(kind, Pending(sim), "vardiya " + sim.S.Day + " kapanışında beklenen olay");
            return sim.PendingShelterEvent;
        }

        public static void Decide(Simulation sim, string option)
        {
            var decided = sim.DecideShelterEvent(option);
            Assert.IsTrue(decided.Ok, option + ": " + decided.Msg);
            Assert.IsNull(sim.PendingShelterEvent, "karar olayı kapatır");
        }

        public static ShelterEventOption Option(Simulation sim, string id) => sim.ShelterEventOptions().Single(o => o.Id == id);

        public static List<ShelterEventLogEntry> Log(Simulation sim, string kind) => sim.ShelterEvents.Log.Where(l => l.Kind == kind).ToList();

        public static int Count(Simulation sim, string kind) => sim.ShelterEvents.Count(kind)?.Count ?? 0;

        /// <summary>Kapıya grup gelene dek kapatır.</summary>
        public static List<CommunityApplicant> NextGroup(Simulation sim)
        {
            int limit = sim.S.Day + 8;
            while (sim.Admission.Group.Count == 0 && sim.S.Day < limit) Close(sim);
            Assert.Greater(sim.Admission.Group.Count, 0, "kapıya grup gelmedi");
            return sim.Admission.Group;
        }

        /// <summary>
        /// Önce <paramref name="skip"/> grubu geri çevirir (ilk grup sızmacı olamaz), sonra gelen grubun ilk adayını alır.
        /// Yalnız komut kullanır.
        /// </summary>
        public static string Admit(Simulation sim, int skip = 1)
        {
            for (int i = 0; i < skip; i++)
            {
                NextGroup(sim);
                Assert.IsTrue(sim.PassCandidates().Ok);
            }
            string id = NextGroup(sim)[0].Resident.Id;
            var chosen = sim.ChooseCandidate(id);
            Assert.IsTrue(chosen.Ok, chosen.Msg);
            return id;
        }

        /// <summary>
        /// Kişiyi yeni yolla sığınaktan çıkarır (eski serbest yollama aşama 7'de silindi): sığınak dolu sayılır (kapasite = canlı sayısı),
        /// kapıda grup yoksa test düzeni olarak tek bir aday konur (aynı tohumla kurulan ikiz oyunun ilk grubundan; kimliği çakışmasın diye
        /// yeniden adlandırılır, insan ve özelliksiz), sonra <c>ChooseCandidate(aday, personId)</c>: kişi yolluğuyla gider, hedefi giden
        /// olay kapanır. Fiş basılmadan ve baskın dışında geçerlidir.
        /// </summary>
        public static ActResult ReplaceAtTheDoor(Simulation sim, string personId)
        {
            var adm = sim.Admission;
            if (adm.Group.Count == 0)
            {
                var b = sim.B.Clone();
                b.shelterEventsEnabled = false;
                b.admissionKnockMin = b.admissionKnockMax = 1;
                b.candidateEmergencyBelow = 0;
                var twin = Simulation.NewGame(sim.S.Seed, b);
                for (int i = 0; i < 8 && twin.Admission.Group.Count == 0; i++) Close(twin);
                Assert.Greater(twin.Admission.Group.Count, 0, "test düzeni: ikiz oyunda aday yok");
                var c = twin.Admission.Group[0];
                c.Resident.Id = "resident-kapi-" + adm.Files.Count;
                c.Identity = new AdmissionIdentity();
                c.Perks.Clear();
                adm.Group.Add(c);
                adm.GroupArrivalShift = sim.S.Day;
                adm.GroupWaitingUntil = sim.S.Day + 2;
            }
            sim.B.boardResidentCapacity = sim.Board.People.Count(p => p.Alive);
            return sim.ChooseCandidate(adm.Group[0].Resident.Id, personId);
        }

        /// <summary>Kapıyı kapatır: bekleyen grup geri çevrilir, yeni grup gelmez (durum doğrudan yazılır).</summary>
        public static void ShutDoor(Simulation sim)
        {
            if (sim.Admission.Group.Count > 0) Assert.IsTrue(sim.PassCandidates().Ok);
            sim.Admission.NextArrivalShift = 100000;
        }

        public static uint After(uint state, int draws)
        {
            var rng = new Rng(state);
            for (int i = 0; i < draws; i++) rng.Next();
            return rng.State;
        }

        /// <summary>İki RNG durumu arasındaki çekim sayısı.</summary>
        public static int Draws(uint before, uint after, int cap = 400)
        {
            var rng = new Rng(before);
            for (int i = 0; i <= cap; i++)
            {
                if (rng.State == after) return i;
                rng.Next();
            }
            Assert.Fail("çekim sayısı " + cap + " sınırını aştı");
            return -1;
        }

        /// <summary>Kapanışın çekim sayısı.</summary>
        public static int CloseDraws(Simulation sim)
        {
            uint before = sim.S.Rng.State;
            Close(sim);
            return Draws(before, sim.S.Rng.State);
        }

        public static string BoardJson(Simulation sim) => UnityEngine.JsonUtility.ToJson(sim.Board);

        static bool Fit(Simulation sim, ResidentState p) => p.Alive && p.Down <= 0 && p.Hp > 0 && !p.Restricted && !p.Isolated
            && sim.S.Day >= sim.Admission.File(p.Id).IdleUntil;

        /// <summary>Üç saha rolünü sağlıklı kişilerle ve kitleriyle doldurur (eksik kit doğrudan eklenir).</summary>
        public static void FillSquad(Simulation sim)
        {
            foreach (string role in PlanningBoardData.FieldJobs)
            {
                var person = sim.Board.People.FirstOrDefault(p => Fit(sim, p) && p.Job == role);
                if (person == null)
                {
                    person = sim.Board.People.First(p => Fit(sim, p) && !PlanningBoardData.FieldJobs.Contains(p.Job));
                    var assigned = sim.AssignResident(person.Id, role);
                    Assert.IsTrue(assigned.Ok, assigned.Msg);
                }
                if (!sim.Board.Equipment.Any(e => e.OwnerId == person.Id && e.Kind == "kit_" + role && e.Charges > 0))
                    Assert.IsNotNull(sim.AddBoardEquipment("kit_" + role, person.Id, true));
            }
        }

        /// <summary>
        /// Baskın emri verir ve baskını oda zinciriyle bitirir (<see cref="FacilityKit.ResolveRaid"/>). <paramref name="fates"/> kadro
        /// sırasıyla: 0 sağ döner, 1 düşer ve tesiste kalır (ölür), 2 geride kalır (ölür; yalnız başarıda), 3 düşer ve tesisten çıkarılır
        /// (yatar; yalnız başarıda). Başarısız baskın giriş odasında geri çekilmedir.
        /// </summary>
        public static void Raid(Simulation sim, bool success, params int[] fates)
        {
            FillSquad(sim);
            StartRaid(sim);
            ResolveRaid(sim, success, fates);
        }

        public static void StartRaid(Simulation sim)
        {
            // Kartlı oyunda bekleyen sunucu ödülü baskın emrini kilitler: test düzeni hep tarifi seçer (seçilecek ödül yoksa hiçbir şey)
            FacilityKit.ChooseReward(sim);
            var region = sim.LiberationRegion(sim.S.Liberation.CurrentRegion);
            if (region.Completed) Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
            var sector = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            var started = sim.StartLiberationRaid(sector.Id);
            Assert.IsTrue(started.Ok, started.Msg);
        }

        /// <summary>Süren baskını oda zinciriyle bitirir (<see cref="FacilityKit.ResolveRaid"/>; kaderler <see cref="Raid"/>'deki gibi).</summary>
        public static void ResolveRaid(Simulation sim, bool success, params int[] fates) => FacilityKit.ResolveRaid(sim, success, fates);

        /// <summary>Olağan olayın tetiğini bu vardiya için kurar (kapanıştan önce çağrılır).</summary>
        public static void Arrange(Simulation sim, string kind)
        {
            double eaten = sim.BoardConsumption;
            switch (kind)
            {
                case "kacak": sim.Shelter.Supplies = 0; break;
                case "erzak_azaliyor": sim.Shelter.Supplies = eaten * 2; break;
                case "karartma": sim.S.Alarm = sim.B.liberationAlarmThreshold - 1; break;
                case "hirsizlik": sim.Shelter.Supplies = eaten + sim.B.theftSupplies + 0.5; break;
                // Tesiste yalnız çıkarılan düşen yaralı döner: kazanılan baskında giriş odasında düşen, ekip çıkarken alınır
                case "agir_yarali": Raid(sim, true, 3, 0, 0); break;
                case "kavga": Raid(sim, false, 0, 0, 0); break;
                // Geri çekilmede yerde kalan düşen ölür
                case "anma": Raid(sim, false, 1, 0, 0); break;
                case "takas": sim.Shelter.Supplies = eaten + 1; sim.AddBoardMaterial("metal", sim.B.tradeMetal); break;
                case "cagri": sim.Shelter.Supplies = 90; Raid(sim, true, 0, 0, 0); break;
                case "konvoy": sim.S.Trust = sim.B.convoyTrustAt; break;
                default: Assert.Fail("olağan olay değil: " + kind); break;
            }
        }

        /// <summary>Olağan olayı tetikleyen denge: yalnız o olay açık; kaçak ilk aç kapanışta.</summary>
        public static Balance For(string kind)
        {
            var b = On(kind);
            b.eventDeserterStreak = 1;
            return b;
        }

        /// <summary>
        /// Verilen olayı açık bırakan oyun. Zincir olayları için sızmacı alınır ve izler görmezden gelinir; büyük hırsızlık için
        /// hırsız affedilir. <paramref name="tune"/> dengeyi oyun kurulmadan önce değiştirir.
        /// </summary>
        public static Simulation Open(string kind, Action<Balance> tune = null, uint seed = 79)
        {
            Balance b;
            Simulation sim;
            if (ShelterEventData.IsChain(kind))
            {
                b = Robots(Quiet(1));
                tune?.Invoke(b);
                sim = New(b, seed);
                Admit(sim);
                ShutDoor(sim);
                CloseTo(sim, "iz");
                if (kind == "iz") return sim;
                Decide(sim, "yoksay");
                CloseTo(sim, "ikinci_iz");
                if (kind == "ikinci_iz") return sim;
                Decide(sim, "yoksay");
                for (int i = 0; i < sim.B.interventionShifts; i++) CloseTo(sim, "-");
                CloseTo(sim, "sabotaj");
                return sim;
            }
            if (kind == "buyuk_hirsizlik")
            {
                b = Limit(Quiet(), "hirsizlik", 1);
                b.bigTheftChance = 1;
                b.bigTheftDelayMin = b.bigTheftDelayMax = 1;
                tune?.Invoke(b);
                sim = New(b, seed);
                Arrange(sim, "hirsizlik");
                CloseTo(sim, "hirsizlik");
                Decide(sim, "affet");
                sim.Shelter.Supplies = 30;
                CloseTo(sim, "buyuk_hirsizlik");
                return sim;
            }
            b = For(kind);
            tune?.Invoke(b);
            sim = New(b, seed);
            Arrange(sim, kind);
            CloseTo(sim, kind);
            return sim;
        }
    }
}
