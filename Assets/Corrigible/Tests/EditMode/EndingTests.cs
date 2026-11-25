using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.ShelterEventKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Sığınağın sonları (biçim 17, aşama 5): ifşa kaynakları (tavansız), "konum çözüldü" vardiyası ve dusus, alarm ara bedeli,
    /// terk (eksik kadro sayacı ve acil grup), son şalter ve iki çeşidi, zaferin tonu, son raporu, öncelik, gizlilik ve kayıt.
    /// Public durum yalnız kurulum için doğrudan yazılır; kayıt testleri yalnız komut kullanır.
    /// </summary>
    public class EndingTests
    {
        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        // ---------------------------------------------------------------- yardımcılar

        static int Alive(Simulation sim) => sim.Board.People.Count(p => p.Alive);

        /// <summary>Açlıkla boşalan sığınak: ilk aç kapanışta ayrılma, acil grup 3'ün altında; kapı olağan grup getirmez.</summary>
        static Balance Starve(int residents, Balance b = null)
        {
            b = b ?? Quiet();
            b.desertStreak = 1;
            b.boardStartResidents = residents;
            b.candidateEmergencyBelow = 3;
            return b;
        }

        /// <summary>Aç kapanış (erzak sıfırlanır).</summary>
        static void CloseHungry(Simulation sim)
        {
            sim.Shelter.Supplies = 0;
            Close(sim);
        }

        /// <summary>Tok kapanış.</summary>
        static void CloseFed(Simulation sim)
        {
            sim.Shelter.Supplies = 50;
            Close(sim);
        }

        /// <summary>Bütün sektörleri, mevcut bölgenin son sektörü dışında geri alınmış sayar; son baskın başarıyla çözülür.</summary>
        static void WinNow(Simulation sim)
        {
            string current = sim.S.Liberation.CurrentRegion;
            foreach (var r in sim.S.Liberation.Regions)
                foreach (var s in r.Sectors)
                    if (r.Id != current || s.Index < r.Sectors.Count - 1) s.Captured = true;
            Raid(sim, true);
            Assert.AreEqual("silme", sim.S.Ending);
        }

        /// <summary>Kapanışa kadar vardiyaları kapatır (VARDİYA = gün + 1).</summary>
        static void CloseUntilShift(Simulation sim, int shift)
        {
            while (sim.S.Day + 1 < shift) Close(sim);
        }

        static string Json(object o) => UnityEngine.JsonUtility.ToJson(o);

        /// <summary>Karşılaştırma için durum: sığınak (pano dahil), gün, son, güven, alarm ve RNG.</summary>
        static string State(Simulation s) => Json(s.Shelter) + "|" + s.S.Day + "|" + s.S.Ending + "|" + s.S.Trust + "|" + s.S.Alarm + "|" + s.S.Rng.State;

        static GameRecord RoundTrip(GameRecord rec) => UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(rec));

        // ---------------------------------------------------------------- ifşa

        /// <summary>İki kaynak ve sorgu: görmezden gelinen iz +1 (iki kâğıt), tamamlanan sabotaj +2, sorgu −1; günlükte kaynak ve kişi.</summary>
        [Test]
        public void Exposure_TwoSources_IgnoredTraceAndSabotage_LogCauseAndPerson()
        {
            var sim = Open("sabotaj");
            string id = sim.PendingShelterEvent.PersonId;
            Assert.AreEqual(4, sim.Shelter.Exposure, "iki yoksay + sabotaj");
            Decide(sim, "sorgula");
            Assert.AreEqual(3, sim.Shelter.Exposure);
            var steps = Log(sim, "ifsa").Select(l => l.Option + ":" + (l.PersonId == id ? "kisi" : l.PersonId) + ":" + l.Amount).ToArray();
            CollectionAssert.AreEqual(new[] { "iz:kisi:1", "ikinci_iz:kisi:1", "sabotaj:kisi:2", "sorgula:kisi:-1" }, steps);

            // Konvoy ve tecrit ifşaya dokunmaz
            sim = Open("iz");
            Decide(sim, "tecrit");
            Assert.AreEqual(0, sim.Shelter.Exposure);
            sim = Open("konvoy");
            Decide(sim, "kabul");
            Assert.AreEqual(0, sim.Shelter.Exposure);
            Assert.IsEmpty(Log(sim, "ifsa"));
        }

        /// <summary>
        /// Kararla eşiğe varılır (ikinci tur kararı): karar anında son yok; o vardiyanın kapanışı uyarır, uyarı iki vardiya sürer,
        /// sığınak ikinci kapanışta düşer. Fiş ve günlük kalan vardiyayı yazar.
        /// </summary>
        [Test]
        public void Exposure_ReachesThresholdByDecision_TwoWarningShifts_ThenFalls()
        {
            var sim = Open("iz", b => b.secondTraceGapMin = b.secondTraceGapMax = 20);
            sim.Shelter.Exposure = 4;
            Assert.AreEqual(Loc.F("İfşa +{0}.", 1), Option(sim, "yoksay").Cost, "eşikten uzakken düz bedel");
            sim.Shelter.Exposure = 5;
            Assert.IsFalse(sim.LocationCompromised);
            Assert.AreEqual(Loc.F("İfşa +{0}: eşiğe ulaşır; kapanışta konum çözülür, {1} vardiyan kalır.", 1, 2), Option(sim, "yoksay").Cost,
                "adalet ilkesi (a): eşiğe vardıran karar kâğıtta söyler");
            Decide(sim, "yoksay");
            Assert.AreEqual(6, sim.Shelter.Exposure);
            Assert.IsNull(sim.S.Ending, "karar anında son yok");
            Assert.IsFalse(sim.Shelter.FallPending, "uyarı kapanışta kurulur");
            Assert.IsTrue(sim.LocationCompromised, "eşikteki ifşa okunur");
            StringAssert.Contains("İFŞA · eşikte: bu kapanışta konum çözülür.", sim.BoardReceiptPreview);
            Assert.AreEqual(1, sim.Shelter.FallGrace, "kararla varılan eşik: bir ek uyarı vardiyası");
            Assert.AreEqual(2, sim.FallShiftsLeft);

            CloseTo(sim, "-");
            Assert.IsNull(sim.S.Ending, "eşiğe ulaşılan kapanışta harekât bitmez");
            Assert.IsTrue(sim.Shelter.FallPending);
            Assert.AreEqual(1, Log(sim, "konum").Count);
            Assert.AreEqual(2, Log(sim, "konum").Single().Amount, "günlük uyarının süresini yazar");
            Assert.AreEqual(2, sim.FallShiftsLeft);
            StringAssert.Contains(Loc.F("İFŞA · konum çözüldü: bu vardiya dahil {0} vardiya kaldı.", 2), sim.BoardReceiptPreview);
            CollectionAssert.AreEqual(new[] { "konum" }, sim.LastSwitchReasons());

            Close(sim);
            Assert.IsNull(sim.S.Ending, "uyarının ilk kapanışı düşürmez");
            Assert.IsTrue(sim.Shelter.FallPending);
            Assert.AreEqual(0, sim.Shelter.FallGrace);
            Assert.AreEqual(1, sim.FallShiftsLeft);
            StringAssert.Contains("İFŞA · konum çözüldü: bu vardiya son vardiya.", sim.BoardReceiptPreview);

            Close(sim);
            Assert.AreEqual("dusus", sim.S.Ending, "ikinci kapanış düşürür");
            Assert.AreEqual(1, Log(sim, "konum").Count);

            // Süre Balance'tan: 1 iken eski kural (bir uyarı vardiyası)
            sim = Open("iz", b => { b.secondTraceGapMin = b.secondTraceGapMax = 20; b.exposureDecisionWarnShifts = 1; });
            sim.Shelter.Exposure = 5;
            Decide(sim, "yoksay");
            Assert.AreEqual(0, sim.Shelter.FallGrace);
            CloseTo(sim, "-");
            Assert.AreEqual(1, Log(sim, "konum").Single().Amount);
            StringAssert.Contains("İFŞA · konum çözüldü: bu vardiya son vardiya.", sim.BoardReceiptPreview);
            Close(sim);
            Assert.AreEqual("dusus", sim.S.Ending);
        }

        /// <summary>
        /// Kararla varılan eşiğin uyarısı ikinci vardiyasında da sorguyla kalkar; ek süre sıfırlanır. Uyarı sürerken gelen sabotaj
        /// süreyi uzatmaz. (Sabotajın ifşası 0'a kurulur ki sorgu eşiğin altına indirebilsin.)
        /// </summary>
        [Test]
        public void Exposure_DecisionGrace_SecondWarningShift_InterrogationLifts_GraceResets()
        {
            var sim = Open("ikinci_iz", b => { b.interventionShifts = 1; b.exposureSabotage = 0; });
            sim.Shelter.Exposure = 5;
            Decide(sim, "yoksay");
            Assert.AreEqual(6, sim.Shelter.Exposure);
            Assert.AreEqual(1, sim.Shelter.FallGrace);
            CloseTo(sim, "-");
            Assert.IsTrue(sim.Shelter.FallPending);
            CloseTo(sim, "sabotaj");
            Assert.IsNull(sim.S.Ending, "ilk uyarı kapanışı düşürmez");
            Assert.IsTrue(sim.Shelter.FallPending);
            Assert.AreEqual(0, sim.Shelter.FallGrace, "uyarı sürerken açılan sabotaj süreyi uzatmaz");
            Assert.AreEqual(1, sim.FallShiftsLeft);
            Assert.IsTrue(Option(sim, "sorgula").Open);
            Decide(sim, "sorgula");
            Assert.AreEqual(5, sim.Shelter.Exposure);
            Assert.IsFalse(sim.Shelter.FallPending, "uyarı ikinci vardiyasında da sorguyla kalkar");
            Assert.AreEqual(0, sim.Shelter.FallGrace);
            Assert.AreEqual(0, sim.FallShiftsLeft);
            Assert.AreEqual(1, Log(sim, "konum_kalkti").Count);
            Close(sim);
            Assert.IsNull(sim.S.Ending);
        }

        /// <summary>Sabotaj kapanışta eşiği doldurur: aynı kapanış uyarır (adım 6'dan sonra 7), sonraki kapanış düşürür.</summary>
        [Test]
        public void Exposure_ReachesThresholdBySabotageAtClosing_SameClosingWarns_NextFalls()
        {
            var sim = Open("ikinci_iz");
            sim.Shelter.Exposure = 3;
            Decide(sim, "yoksay");
            Assert.AreEqual(4, sim.Shelter.Exposure);
            for (int i = 0; i < sim.B.interventionShifts; i++) CloseTo(sim, "-");
            Assert.IsFalse(sim.Shelter.FallPending);
            CloseTo(sim, "sabotaj");
            Assert.AreEqual(6, sim.Shelter.Exposure);
            Assert.IsTrue(sim.Shelter.FallPending, "sabotajın açıldığı kapanış uyarır");
            Assert.AreEqual(0, sim.Shelter.FallGrace, "sabotajla varılan eşikte bir vardiya kalır");
            Assert.AreEqual(1, Log(sim, "konum").Single().Amount);
            Assert.AreEqual(1, sim.FallShiftsLeft);
            Assert.IsNull(sim.S.Ending);
            Decide(sim, "sok");
            Close(sim);
            Assert.AreEqual("dusus", sim.S.Ending);
        }

        /// <summary>
        /// Uyarı vardiyasında sorgu ifşayı eşiğin altına indirir: uyarı o anda kalkar (fiş satırı ve tahliye nedeni de), kapanışta
        /// düşüş yok.
        /// </summary>
        [Test]
        public void Exposure_InterrogationBelowThreshold_WarningLiftsAtOnce_NoFall()
        {
            var sim = Open("ikinci_iz");
            sim.Shelter.Exposure = 3;
            Decide(sim, "yoksay");
            for (int i = 0; i < sim.B.interventionShifts; i++) CloseTo(sim, "-");
            CloseTo(sim, "sabotaj");
            Assert.AreEqual(6, sim.Shelter.Exposure);
            Assert.AreEqual(2, Log(sim, "ifsa").Last().Amount);
            Assert.IsTrue(sim.Shelter.FallPending);
            CollectionAssert.AreEqual(new[] { "konum" }, sim.LastSwitchReasons());
            Decide(sim, "sorgula");
            Assert.AreEqual(5, sim.Shelter.Exposure);
            Assert.IsFalse(sim.Shelter.FallPending, "uyarı karar anında kalkar");
            Assert.IsFalse(sim.LocationCompromised);
            Assert.AreEqual(1, Log(sim, "konum_kalkti").Count);
            Assert.AreEqual(sim.S.Day, Log(sim, "konum_kalkti").Single().Shift);
            CollectionAssert.IsEmpty(sim.LastSwitchReasons(), "kalkmış uyarı tahliye nedeni değildir");
            Assert.IsFalse(sim.BoardReceiptPreview.Contains("İFŞA"), "eşiğin altındayken fiş satırı yok");
            Close(sim);
            Assert.IsNull(sim.S.Ending);
            Assert.IsFalse(sim.Shelter.FallPending);
            Assert.AreEqual(1, Log(sim, "konum_kalkti").Count);

            // Eşik kapalıyken düşüş yok (sayaç yine tavansız)
            sim = Open("ikinci_iz", b => b.exposureThreshold = 0);
            sim.Shelter.Exposure = 4;
            Decide(sim, "yoksay");
            for (int i = 0; i < sim.B.interventionShifts; i++) CloseTo(sim, "-");
            CloseTo(sim, "sabotaj");
            Assert.AreEqual(7, sim.Shelter.Exposure);
            Assert.IsFalse(sim.Shelter.FallPending);
            Decide(sim, "sok");
            Close(sim);
            Assert.IsNull(sim.S.Ending);
        }

        /// <summary>
        /// İnceleme bulgusu (tavan): sayaç eşikte kırpılmaz. Eşikteki ifşaya gelen sabotaj da +2 yazar; sorgu onu eşiğin altına
        /// indiremez, düşüş gelir. Kırpma olsaydı ek bir kötü olay (sabotaj) sığınağı sorgu fırsatıyla kurtarırdı.
        /// </summary>
        [Test]
        public void Exposure_NoCeiling_SabotageAtTheThresholdNeverHelps()
        {
            var sim = Open("ikinci_iz", b => b.interventionShifts = 0);
            sim.Shelter.Exposure = 5;
            Decide(sim, "yoksay");
            Assert.AreEqual(6, sim.Shelter.Exposure);
            CloseTo(sim, "sabotaj");
            Assert.AreEqual(8, sim.Shelter.Exposure, "tavan yok");
            Assert.AreEqual(2, Log(sim, "ifsa").Last().Amount);
            Assert.IsTrue(sim.Shelter.FallPending);
            Decide(sim, "sorgula");
            Assert.AreEqual(7, sim.Shelter.Exposure);
            Assert.IsTrue(sim.Shelter.FallPending, "hâlâ eşikte: uyarı sürer");
            Assert.IsTrue(sim.LocationCompromised);
            Assert.IsEmpty(Log(sim, "konum_kalkti"));
            // Eşiğe görmezden gelinen izle (kararla) varıldı: uyarı iki vardiya, sabotaj süreyi kısaltmaz
            Close(sim);
            Assert.IsNull(sim.S.Ending);
            Close(sim);
            Assert.AreEqual("dusus", sim.S.Ending);
            // Rapor gerçek değişimleri yazar (5'e kurulum günlükte yok)
            CollectionAssert.AreEqual(new[] { 1, 1, 2, -1 }, sim.EndReport.ExposureSteps.Select(x => x.Delta).ToArray());
        }

        /// <summary>
        /// İnceleme bulgusu (uyarı sonrası ikinci sabotaj): iki sızmacı, bütün izler görmezden gelinir. Birinin sabotajı ifşayı eşiğe
        /// taşır (uyarı), sorgu eşiğin altına indirir (uyarı kalkar); ertesi kapanışta ötekinin sabotajı eşiği yeniden aşar. Bu
        /// kapanış K4'e göre yalnız uyarır, düşürmez; düşüş bir sonraki kapanıştadır. Yalnız komutlar.
        /// </summary>
        [Test]
        public void Exposure_WarningLiftedByInterrogation_NextSabotageWarnsAgain_DoesNotFall()
        {
            var sim = New(Robots(Quiet(1), 1, 1));
            sim.B.interventionShifts = 2;
            string a = Admit(sim);
            Assert.AreEqual("-", Pending(sim));
            string c = Admit(sim, 0);
            ShutDoor(sim);
            Assert.AreEqual("iz", Pending(sim));
            Assert.AreEqual(a, sim.PendingShelterEvent.PersonId);
            Decide(sim, "yoksay");
            Assert.AreEqual(a, CloseTo(sim, "ikinci_iz").PersonId);
            sim.B.interventionShifts = 0;   // ikinci sızmacının sabotajı birinciden hemen sonra
            Decide(sim, "yoksay");
            Assert.AreEqual(c, CloseTo(sim, "iz").PersonId);
            Decide(sim, "yoksay");
            Assert.AreEqual(c, CloseTo(sim, "ikinci_iz").PersonId);
            Decide(sim, "yoksay");
            Assert.AreEqual(4, sim.Shelter.Exposure);

            Assert.AreEqual(a, CloseTo(sim, "sabotaj").PersonId);
            Assert.AreEqual(6, sim.Shelter.Exposure);
            Assert.IsTrue(sim.Shelter.FallPending);
            Decide(sim, "sorgula");
            Assert.AreEqual(5, sim.Shelter.Exposure);
            Assert.IsFalse(sim.Shelter.FallPending);

            Assert.AreEqual(c, CloseTo(sim, "sabotaj").PersonId);
            Assert.IsNull(sim.S.Ending, "sabotajla eşiğe varılan kapanış uyarır, düşürmez");
            Assert.AreEqual(7, sim.Shelter.Exposure);
            Assert.IsTrue(sim.Shelter.FallPending);
            Assert.AreEqual(2, Log(sim, "konum").Count);
            StringAssert.Contains("İFŞA · konum çözüldü: bu vardiya son vardiya.", sim.BoardReceiptPreview);
            // İkinci tur kararı: ilk sabotajcı sorguda olsa da sorgu tecrit yeri istemez; ama 7 → 6 eşiğin altına inmez
            Assert.IsTrue(Option(sim, "sorgula").Open, "sorgu tecrit yeri istemez");
            Decide(sim, "sorgula");
            Assert.AreEqual(6, sim.Shelter.Exposure);
            Assert.IsTrue(sim.Shelter.FallPending);
            Close(sim);
            Assert.AreEqual("dusus", sim.S.Ending);
        }

        /// <summary>Uyarı vardiyasında son baskın zaferi getirir; ifşa ve izli sızmacı tonu belirler.</summary>
        [Test]
        public void Exposure_FinalRaidWinsDuringTheWarningShift()
        {
            var sim = Open("iz", b => b.secondTraceGapMin = b.secondTraceGapMax = 20);
            sim.Shelter.Exposure = 5;
            Decide(sim, "yoksay");
            CloseTo(sim, "-");
            Assert.IsTrue(sim.Shelter.FallPending);
            WinNow(sim);
            Assert.AreEqual("golgeli", sim.VictoryTone, "izi görülmüş, ortaya çıkmamış sızmacı içeride");
            sim.Board.People.First(p => p.Alive && sim.Admission.File(p.Id).Tier >= 0).Alive = false;
            Assert.AreEqual("bedelli", sim.VictoryTone, "sızmacı içeride değilse ifşa tonu bozar");
        }

        /// <summary>Ara bedel: ifşa 3 ve üstündeyken vardiya alarmı 0,5 fazla; olaylar kapalıyken ya da bedel 0'ken yok.</summary>
        [Test]
        public void Exposure_AlarmBonus_FromThreeOnly_EventsOffNone()
        {
            var sim = New(Quiet());
            Assert.AreEqual(0, sim.AlarmPerShift);
            sim.Shelter.Exposure = 2;
            Assert.AreEqual(0, sim.ExposureAlarmBonus);
            double before = sim.S.Alarm;
            Close(sim);
            Assert.AreEqual(before, sim.S.Alarm);

            sim.Shelter.Exposure = 3;
            Assert.AreEqual(0.5, sim.ExposureAlarmBonus);
            Assert.AreEqual(0.5, sim.AlarmPerShift, "bonus vardiya alarmının içinde (karartma tetiği de görür)");
            Close(sim);
            Assert.AreEqual(before + 0.5, sim.S.Alarm, 1e-9);

            var off = Quiet();
            off.shelterEventsEnabled = false;
            sim = New(off);
            sim.Shelter.Exposure = 5;
            Assert.AreEqual(0, sim.AlarmPerShift);

            var zero = Quiet();
            zero.exposureAlarmBonus = 0;
            sim = New(zero);
            sim.Shelter.Exposure = 5;
            Assert.AreEqual(0, sim.AlarmPerShift);
        }

        // ---------------------------------------------------------------- terk

        /// <summary>
        /// Ayrılmayla 3'ün altına inilen kapanışta acil grup gelir, sayaç 0; sonraki üç kapanış 1, 2, terk. Fiş sayacı bu kapanıştan
        /// sonraki değeriyle yazar (1/3, 2/3, 3/3 ve "sığınak boşalır").
        /// </summary>
        [Test]
        public void Abandon_StarvationBelowThree_EmergencyGroupStartsTheCount_ThirdClosingEnds()
        {
            var sim = New(Starve(3));
            CloseHungry(sim);
            Assert.AreEqual(3, Alive(sim), "kurucular kuruluş kapanışında ayrılmaz");
            CloseHungry(sim);
            Assert.AreEqual(2, Alive(sim));
            Assert.AreEqual(1, sim.Shelter.Deserted);
            Assert.IsTrue(sim.Admission.GroupEmergency);
            Assert.AreEqual(0, sim.Shelter.UnderstaffedStreak, "grup bu kapanışta geldi: sayım sonraki kapanışta başlar");
            Assert.AreEqual(sim.S.Day + 1, sim.Shelter.EmergencyDoorShift);
            Assert.IsTrue(sim.Shelter.DesertedSinceFull);
            Assert.IsTrue(sim.UnderstaffedCounting, "sayaç bu kapanışta işleyecek");
            Assert.AreEqual(1, sim.UnderstaffedAtClose);
            StringAssert.Contains("SIĞINAK EKSİK · 1/3.", sim.BoardReceiptPreview, "ilk eksik vardiya da fişte");

            CloseFed(sim);
            Assert.AreEqual(1, sim.Shelter.UnderstaffedStreak);
            StringAssert.Contains("SIĞINAK EKSİK · 2/3.", sim.BoardReceiptPreview);
            Assert.IsNull(sim.S.Ending);
            CloseFed(sim);
            Assert.AreEqual(2, sim.Shelter.UnderstaffedStreak);
            StringAssert.Contains("SIĞINAK EKSİK · 3/3: bu kapanışta sığınak boşalır.", sim.BoardReceiptPreview);
            Assert.IsNull(sim.S.Ending);
            CloseFed(sim);
            Assert.AreEqual(3, sim.Shelter.UnderstaffedStreak);
            Assert.AreEqual("terk", sim.S.Ending);
        }

        /// <summary>
        /// Varsayılan eşik (<c>candidateEmergencyBelow</c> 4, tek kip denge turu; öbür terk testleri 3'e sabitler): terk sayacı acil
        /// grupla aynı eşiği okur. Dört kişilik sığınakta tek açlık ayrılması kalanı üçe indirir ve sayım başlar; üçüncü kapanış terk.
        /// Eşik 3'te aynı kurulum (üç canlı) sayılmaz.
        /// </summary>
        [Test]
        public void Abandon_DefaultThreshold_ThreeLeftAfterDesertion_Counts_ThresholdThreeDoesNot()
        {
            Assert.AreEqual(4, new Balance().candidateEmergencyBelow);
            var b = Starve(4);
            b.candidateEmergencyBelow = new Balance().candidateEmergencyBelow;
            var sim = New(b);
            CloseHungry(sim);
            Assert.AreEqual(4, Alive(sim), "kurucular kuruluş kapanışında ayrılmaz");
            CloseHungry(sim);
            Assert.AreEqual(3, Alive(sim));
            Assert.IsTrue(sim.Admission.GroupEmergency, "üç canlı eşiğin altında: acil grup");
            Assert.IsTrue(sim.Shelter.DesertedSinceFull);
            Assert.IsTrue(sim.UnderstaffedCounting);
            CloseFed(sim);
            Assert.AreEqual(1, sim.Shelter.UnderstaffedStreak);
            CloseFed(sim);
            Assert.AreEqual(2, sim.Shelter.UnderstaffedStreak);
            Assert.IsNull(sim.S.Ending);
            CloseFed(sim);
            Assert.AreEqual("terk", sim.S.Ending);

            var three = New(Starve(4));
            CloseHungry(three);
            CloseHungry(three);
            Assert.AreEqual(3, Alive(three));
            Assert.IsFalse(three.Admission.GroupEmergency, "eşik 3: üç canlı eksik sayılmaz");
            Assert.IsFalse(three.UnderstaffedCounting);
            for (int i = 0; i < 4; i++) CloseFed(three);
            Assert.IsNull(three.S.Ending);
            Assert.AreEqual(0, three.Shelter.UnderstaffedStreak);
        }

        /// <summary>Vardiya acil grupla açıldıysa içinde aday alınsa da sayılır; canlı 3'e ulaşan kapanış sıfırlar.</summary>
        [Test]
        public void Abandon_TakingSomeoneButStillBelow_StillCounts_ReachingThreeResets()
        {
            var sim = New(Starve(3));
            CloseHungry(sim);
            CloseHungry(sim);
            CloseHungry(sim);
            Assert.AreEqual(1, Alive(sim));
            Assert.AreEqual(1, sim.Shelter.UnderstaffedStreak);
            Assert.IsTrue(sim.Admission.GroupEmergency);
            Assert.IsTrue(sim.ChooseCandidate(sim.Admission.Group[0].Resident.Id).Ok);
            Assert.AreEqual(2, Alive(sim));
            CloseFed(sim);
            Assert.AreEqual(2, sim.Shelter.UnderstaffedStreak, "aday alındı ama canlı hâlâ 3'ün altında");
            Assert.IsNull(sim.S.Ending);
            Assert.IsTrue(sim.Admission.GroupEmergency, "yeni acil grup");
            Assert.IsTrue(sim.ChooseCandidate(sim.Admission.Group[0].Resident.Id).Ok);
            Assert.AreEqual(3, Alive(sim));
            Assert.IsFalse(sim.UnderstaffedCounting, "canlı 3: sayaç bu kapanışta sıfırlanacak");
            Assert.IsFalse(sim.BoardReceiptPreview.Contains("SIĞINAK EKSİK"));
            CollectionAssert.IsEmpty(sim.LastSwitchReasons());
            CloseFed(sim);
            Assert.AreEqual(0, sim.Shelter.UnderstaffedStreak, "3 ve üstüyle kapanan vardiya sıfırlar");
            Assert.IsFalse(sim.Shelter.DesertedSinceFull, "ayrılma dönemi de kapanır");
            Assert.IsNull(sim.S.Ending);
        }

        /// <summary>Soru 6 varsayılanı: yalnız ölümlerle 3'ün altına inen, ayrılma yaşamamış sığınak kendiliğinden bitmez.</summary>
        [Test]
        public void Abandon_DeathsOnlyBelowThree_NeverEnds()
        {
            var b = Quiet();
            b.boardStartResidents = 4;
            b.candidateEmergencyBelow = 3;
            var sim = New(b);
            Raid(sim, false, 1, 1, 0);
            Assert.AreEqual(2, Alive(sim));
            for (int i = 0; i < 10; i++)
            {
                CloseFed(sim);
                Assert.IsNull(sim.S.Ending);
                Assert.AreEqual(0, sim.Shelter.UnderstaffedStreak);
                Assert.IsTrue(sim.Admission.GroupEmergency, "acil grup kapıda");
            }
            Assert.AreEqual(0, sim.Shelter.Deserted);
            Assert.IsEmpty(sim.LastSwitchReasons(), "ayrılmasız eksik kadro tahliye nedeni değildir");
        }

        /// <summary>
        /// İnceleme bulgusu (eski ayrılma): harekâtın başında bir kişi açlıktan ayrılır ama sığınak tam kadroyla kapanır; çok sonra
        /// yalnız ölümlerle 3'ün altına iner. Terk işlemez (soru 6): ayrılma o eksik kadro döneminde yaşanmadı.
        /// </summary>
        [Test]
        public void Abandon_OldDesertion_ThenDeathsBelowThree_NeverEnds()
        {
            var sim = New(Starve(5));
            CloseFed(sim);
            CloseHungry(sim);
            Assert.AreEqual(4, Alive(sim));
            Assert.AreEqual(1, sim.Shelter.Deserted);
            Assert.IsFalse(sim.Shelter.DesertedSinceFull, "canlı 4 ile kapandı: ayrılma dönemi kapandı");
            for (int i = 0; i < 6; i++) CloseFed(sim);
            Raid(sim, false, 1, 1, 0);
            Assert.AreEqual(2, Alive(sim));
            for (int i = 0; i < 6; i++)
            {
                CloseFed(sim);
                Assert.IsNull(sim.S.Ending);
                Assert.IsTrue(sim.Admission.GroupEmergency);
                Assert.AreEqual(0, sim.Shelter.UnderstaffedStreak);
                Assert.IsFalse(sim.UnderstaffedCounting);
                CollectionAssert.IsEmpty(sim.LastSwitchReasons());
            }

            // Bu eksik dönemde yeni bir açlık ayrılması sayımı başlatır
            CloseHungry(sim);
            Assert.AreEqual(1, Alive(sim));
            Assert.IsTrue(sim.Shelter.DesertedSinceFull);
            Assert.IsTrue(sim.UnderstaffedCounting);
            sim.Shelter.Supplies = 50;
            for (int i = 0; i < 3 && sim.S.Ending == null; i++) CloseFed(sim);
            Assert.AreEqual("terk", sim.S.Ending);
        }

        /// <summary>Olağan grupla açılan vardiya sayacı tutar (sıfırlamaz, artırmaz); acil grup dönünce sayım sürer.</summary>
        [Test]
        public void Abandon_NormalGroupAtTheDoor_HoldsTheCount()
        {
            var sim = New(Starve(3));
            CloseHungry(sim);
            CloseHungry(sim);
            CloseFed(sim);
            Assert.AreEqual(1, sim.Shelter.UnderstaffedStreak);
            // Vardiya kapıda olağan grupla açılmış gibi (public kapı durumu)
            sim.Admission.GroupEmergency = false;
            sim.Shelter.EmergencyDoorShift = -1;
            CloseFed(sim);
            Assert.AreEqual(1, sim.Shelter.UnderstaffedStreak, "tutulur");
            Assert.IsNull(sim.S.Ending);
            Assert.IsTrue(sim.Admission.GroupEmergency, "bekleme bitti, acil grup geldi");
            CloseFed(sim);
            Assert.AreEqual(2, sim.Shelter.UnderstaffedStreak);
            CloseFed(sim);
            Assert.AreEqual("terk", sim.S.Ending);

            // Terk kapalıyken sayaç işler, son gelmez
            var b = Starve(3);
            b.abandonUnderstaffedShifts = 0;
            sim = New(b);
            CloseHungry(sim);
            CloseHungry(sim);
            for (int i = 0; i < 5; i++) CloseFed(sim);
            Assert.AreEqual(5, sim.Shelter.UnderstaffedStreak);
            Assert.IsNull(sim.S.Ending);
        }

        // ---------------------------------------------------------------- öncelik

        [Test]
        public void Endings_Priority_SameClosing()
        {
            // topluluk > dusus
            var sim = New(Quiet());
            sim.Shelter.Exposure = 6;
            sim.Shelter.FallPending = true;
            foreach (var p in sim.Board.People) p.Alive = false;
            Close(sim);
            Assert.AreEqual("topluluk", sim.S.Ending);

            // dusus > terk
            sim = New(Starve(3));
            CloseHungry(sim);
            CloseHungry(sim);
            CloseFed(sim);
            CloseFed(sim);
            Assert.AreEqual(2, sim.Shelter.UnderstaffedStreak);
            sim.Shelter.Exposure = 6;
            sim.Shelter.FallPending = true;
            CloseFed(sim);
            Assert.AreEqual(3, sim.Shelter.UnderstaffedStreak, "sayaç sonlardan önce güncellenir");
            Assert.AreEqual("dusus", sim.S.Ending);

            // terk kapanışında alarm adımı işlemez: dagilma ve karşı saldırı gelmez
            sim = New(Starve(3));
            CloseHungry(sim);
            CloseHungry(sim);
            CloseFed(sim);
            CloseFed(sim);
            sim.B.liberationAlarmPerShift = 50;
            sim.S.Trust = 1;
            double alarm = sim.S.Alarm;
            CloseFed(sim);
            Assert.AreEqual("terk", sim.S.Ending);
            Assert.AreEqual(alarm, sim.S.Alarm);
            Assert.AreEqual(0, sim.S.Counterattacks);
        }

        // ---------------------------------------------------------------- son şalter

        [Test]
        public void LastSwitch_EarliestShift_LockedPlanAndPrintedSlip_RaidBlocks_OnceOnly()
        {
            var sim = New(Quiet());
            int commands = sim.Record.commands.Count;
            var early = sim.PullLastSwitch();
            Assert.IsFalse(early.Ok);
            Assert.AreEqual("Son şalter 5. vardiyadan önce çekilemez.", early.Msg);
            Assert.AreEqual(commands, sim.Record.commands.Count, "başarısız çağrı kayda yazılmaz");
            CloseUntilShift(sim, 4);
            Assert.IsNotNull(sim.LastSwitchCheck(), "4. vardiya");
            Close(sim);
            Assert.AreEqual(5, sim.S.Day + 1);
            Assert.IsNull(sim.LastSwitchCheck());

            FillSquad(sim);
            StartRaid(sim);
            Assert.AreEqual("Baskın sürüyor.", sim.LastSwitchCheck());
            ResolveRaid(sim, false);
            Assert.IsTrue(sim.Board.Locked, "baskından sonra plan kilitli");
            Assert.IsNull(sim.LastSwitchCheck(), "kilitli planda çekilebilir");
            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.IsNull(sim.LastSwitchCheck(), "fiş basıldıktan sonra da");

            uint rng = sim.S.Rng.State;
            commands = sim.Record.commands.Count;
            var pulled = sim.PullLastSwitch();
            Assert.IsTrue(pulled.Ok, pulled.Msg);
            Assert.AreEqual("Son şalter çekildi: çekilme emri damgalandı.", pulled.Msg);
            Assert.AreEqual(rng, sim.S.Rng.State, "çekim yok");
            Assert.AreEqual("salter", sim.S.Ending);
            Assert.AreEqual("cekilme", sim.Shelter.SwitchKind);
            Assert.AreEqual(sim.S.Day, sim.Shelter.SwitchShift);
            Assert.AreEqual(commands + 1, sim.Record.commands.Count);
            Assert.AreEqual("last_switch", sim.Record.commands.Last().kind);
            Assert.AreEqual(sim.S.Day, sim.Record.commands.Last().day);

            var again = sim.PullLastSwitch();
            Assert.IsFalse(again.Ok);
            Assert.AreEqual("Oyun bitti.", again.Msg);
            Assert.AreEqual(commands + 1, sim.Record.commands.Count, "tek kez işlenir");

            // Açık olay ve olaylar kapalı oyun engel değildir; eski oda yolunda şalter yok
            sim = New(For("konvoy"));
            CloseUntilShift(sim, 5);
            Arrange(sim, "konvoy");
            CloseTo(sim, "konvoy");
            Assert.IsNull(sim.LastSwitchCheck());
            var off = Quiet();
            off.shelterEventsEnabled = false;
            sim = New(off);
            CloseUntilShift(sim, 5);
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            Assert.AreEqual("cekilme", sim.Shelter.SwitchKind);
            var legacy = LegacySimulation.NewGame(79, new Balance { shelterEventsEnabled = false });
            Assert.AreEqual("Bu kayıtta son şalter yok.", legacy.LastSwitchCheck());
            Assert.IsNull(legacy.EndReport);
            Assert.IsNull(legacy.VictoryTone);
        }

        [Test]
        public void LastSwitch_TwoKinds_Reasons()
        {
            Simulation Ready(Action<Simulation> arrange, Balance b = null)
            {
                var s = New(b ?? Quiet());
                CloseUntilShift(s, 5);
                arrange?.Invoke(s);
                return s;
            }

            var sim = Ready(null);
            CollectionAssert.IsEmpty(sim.LastSwitchReasons());
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            Assert.AreEqual("cekilme", sim.Shelter.SwitchKind);
            CollectionAssert.IsEmpty(sim.Shelter.SwitchReasons);

            void Evacuation(Action<Simulation> arrange, params string[] reasons)
            {
                var s = Ready(arrange);
                uint rng = s.S.Rng.State;
                CollectionAssert.AreEqual(reasons, s.LastSwitchReasons());
                Assert.AreEqual(rng, s.S.Rng.State);
                var r = s.PullLastSwitch();
                Assert.IsTrue(r.Ok, r.Msg);
                Assert.AreEqual("Son şalter çekildi: tahliye emri damgalandı.", r.Msg);
                Assert.AreEqual("tahliye", s.Shelter.SwitchKind);
                CollectionAssert.AreEqual(reasons, s.Shelter.SwitchReasons);
                CollectionAssert.AreEqual(reasons, s.EndReport.SwitchReasons);
                Assert.AreEqual("tahliye", s.EndReport.SwitchKind);
            }
            Evacuation(s => s.Shelter.Exposure = 6, "konum");
            // Eksik kadro sayacı (kurulum: ayrılma dönemi, canlı acil eşiğinin altında)
            void Short(Simulation s, int streak)
            {
                s.B.candidateEmergencyBelow = 99;
                s.Shelter.DesertedSinceFull = true;
                s.Shelter.UnderstaffedStreak = streak;
            }
            Evacuation(s => Short(s, 1), "kadro");
            Evacuation(s => s.S.Trust = 1, "guven");
            Evacuation(s => { s.Shelter.Exposure = 6; Short(s, 2); s.S.Trust = 1; }, "konum", "kadro", "guven");
            sim = Ready(s => { Short(s, 1); s.B.abandonUnderstaffedShifts = 0; });
            CollectionAssert.IsEmpty(sim.LastSwitchReasons(), "terk kapalıyken eksik kadro tahliye nedeni değildir");

            // Güven 2 tahliye değildir; olaylar kapalıyken ifşa okunmaz
            sim = Ready(s => s.S.Trust = 2);
            CollectionAssert.IsEmpty(sim.LastSwitchReasons());
            var off = Quiet();
            off.shelterEventsEnabled = false;
            sim = Ready(s => s.Shelter.Exposure = 6, off);
            CollectionAssert.IsEmpty(sim.LastSwitchReasons());
        }

        /// <summary>
        /// İnceleme bulgusu: açlık ayrılmasıyla 3'ün altına inilen kapanıştan sonraki ilk vardiya. Sayaç henüz 0 ama bu kapanışta
        /// işleyecek (kapıda acil grup): şalter tahliyedir, fişte "SIĞINAK EKSİK · 1/3".
        /// </summary>
        [Test]
        public void LastSwitch_FirstShortShiftAfterDesertion_IsEvacuation()
        {
            var sim = New(Starve(3));
            while (sim.S.Day + 1 < 5) CloseFed(sim);
            CloseHungry(sim);
            Assert.AreEqual(6, sim.S.Day + 1);
            Assert.AreEqual(2, Alive(sim));
            Assert.AreEqual(0, sim.Shelter.UnderstaffedStreak);
            Assert.IsTrue(sim.Admission.GroupEmergency);
            StringAssert.Contains("SIĞINAK EKSİK · 1/3.", sim.BoardReceiptPreview);
            // Aç kapanışta ayrılma oldu ve sıradaki belli: açlık da tahliye nedenidir (ikinci tur kararı)
            CollectionAssert.AreEqual(new[] { "kadro", "aclik" }, sim.LastSwitchReasons());
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            Assert.AreEqual("tahliye", sim.Shelter.SwitchKind);
            CollectionAssert.AreEqual(new[] { "kadro", "aclik" }, sim.EndReport.SwitchReasons);
        }

        /// <summary>
        /// İkinci tur kararı: aç sığınaktan ayrılmalar sürerken (açlık sayacı ayrılma eşiğinde ve sıradaki ayrılacak kişi belli) son
        /// şalter tahliyedir ("aclik"). Eşiğin altındaki açlık, tok kapanışla eşiğin altına inen sayaç ve ayrılacak kişinin olmaması
        /// tahliye değildir; olaylar kapalıyken neden yok. Okuyucu çekim yapmaz.
        /// </summary>
        [Test]
        public void LastSwitch_HungerDesertionsOngoing_IsEvacuation()
        {
            var b = Quiet();
            b.desertStreak = 2;
            var sim = New(b);
            CloseUntilShift(sim, 5);
            CloseHungry(sim);
            Assert.AreEqual(1, sim.Shelter.HungryStreak);
            Assert.IsNotNull(sim.NextDeserter, "bir aç kapanış daha ayrılma getirir");
            Assert.IsFalse(sim.DesertionsOngoing, "sayaç eşiğin altında: ayrılmalar başlamadı");
            CollectionAssert.IsEmpty(sim.LastSwitchReasons());
            int alive = Alive(sim);
            CloseHungry(sim);
            Assert.AreEqual(alive - 1, Alive(sim), "eşikte ilk ayrılma");
            Assert.AreEqual(2, sim.Shelter.HungryStreak);
            Assert.IsTrue(sim.DesertionsOngoing);
            uint rng = sim.S.Rng.State;
            CollectionAssert.AreEqual(new[] { "aclik" }, sim.LastSwitchReasons());
            Assert.AreEqual(rng, sim.S.Rng.State, "okuyucu çekim yapmaz");

            // Tok kapanış sayacı 1 azaltır: eşiğin altında, ayrılmalar durdu
            CloseFed(sim);
            Assert.AreEqual(1, sim.Shelter.HungryStreak);
            Assert.IsFalse(sim.DesertionsOngoing);
            CollectionAssert.IsEmpty(sim.LastSwitchReasons());
            // Sayaç eşikte kalsa da tok kapanıştan sonra ayrılma sürmüyor sayılır
            sim.Shelter.HungryStreak = 2;
            Assert.IsFalse(sim.Shelter.Starving);
            Assert.IsFalse(sim.DesertionsOngoing, "son kapanış tok");

            // Ayrılacak kişi yoksa (son kişi gitmez) neden yok
            sim.Shelter.Starving = true;
            Assert.IsTrue(sim.DesertionsOngoing);
            foreach (var p in sim.Board.People.Where(p => p.Alive).Skip(1).ToArray()) p.Alive = false;
            Assert.IsNull(sim.NextDeserter);
            CollectionAssert.DoesNotContain(sim.LastSwitchReasons(), "aclik");

            // Çekimde kayda ve rapora geçer; olaylar kapalıyken açlık nedeni yok
            sim = New(b);
            CloseUntilShift(sim, 5);
            sim.Shelter.HungryStreak = 2;
            sim.Shelter.Starving = true;
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            Assert.AreEqual("tahliye", sim.Shelter.SwitchKind);
            CollectionAssert.AreEqual(new[] { "aclik" }, sim.EndReport.SwitchReasons);
            var off = Quiet();
            off.desertStreak = 2;
            off.shelterEventsEnabled = false;
            sim = New(off);
            CloseUntilShift(sim, 5);
            sim.Shelter.HungryStreak = 2;
            sim.Shelter.Starving = true;
            Assert.IsFalse(sim.DesertionsOngoing);
            CollectionAssert.IsEmpty(sim.LastSwitchReasons());
        }

        // ---------------------------------------------------------------- zaferin tonu

        /// <summary>
        /// İkinci tur kararı: temiz zaferin ölüm sınırı harekât uzunluğuyla büyür: <c>victoryCleanMaxDeaths</c> +
        /// VARDİYA ÷ <c>victoryCleanDeathShifts</c>. Varsayılan taban 0, her 15 vardiyada +1 (aşama 8 ölçümü): 30–44. vardiyada 2,
        /// 45–59'da 3; hedef uzunluk (35–45) bir basamağı da kapsar. Satırın sınırı da onu yazar. Sıfırken sınır sabit.
        /// </summary>
        [Test]
        public void VictoryTone_DeathLimit_ScalesWithCampaignLength()
        {
            Simulation At(int shift, int deaths, Action<Balance> tune = null)
            {
                var b = Quiet();
                tune?.Invoke(b);
                var s = New(b);
                while (s.S.Day + 1 < shift) CloseFed(s);
                foreach (var p in s.Board.People.Skip(4).Take(deaths)) p.Alive = false;
                WinNow(s);
                return s;
            }
            var sim = At(1, 0);
            Assert.AreEqual(0, sim.EndReport.Criteria[1].Limit, "1. vardiyada taban sınır 0");
            Assert.AreEqual("temiz", sim.VictoryTone);
            Assert.AreEqual("bedelli", At(1, 1).VictoryTone);
            Assert.AreEqual(1, At(15, 1).EndReport.Criteria[1].Limit, "sınır 15. vardiyada artar");
            Assert.AreEqual("temiz", At(15, 1).VictoryTone);
            sim = At(14, 1);
            Assert.AreEqual(0, sim.EndReport.Criteria[1].Limit);
            Assert.AreEqual("bedelli", sim.VictoryTone);
            sim = At(30, 2);
            Assert.AreEqual(30, sim.S.Day + 1);
            Assert.AreEqual(30 / 15, sim.EndReport.Criteria[1].Limit, "30. vardiyada 0 + 2");
            Assert.AreEqual("temiz", sim.VictoryTone);
            Assert.AreEqual("bedelli", At(30, 3).VictoryTone);
            sim = At(30, 4, b => { b.victoryCleanMaxDeaths = 2; b.victoryCleanDeathShifts = 20; });
            Assert.AreEqual(3, sim.EndReport.Criteria[1].Limit, "taban 2, her 20 vardiyada +1");
            Assert.AreEqual("bedelli", sim.VictoryTone);
            Assert.IsFalse(sim.EndReport.Criteria[1].Met);
            sim = At(30, 3, b => { b.victoryCleanMaxDeaths = 2; b.victoryCleanDeathShifts = 0; });
            Assert.AreEqual(2, sim.EndReport.Criteria[1].Limit, "sıfır: sabit sınır");
            Assert.AreEqual("bedelli", sim.VictoryTone);
        }

        /// <summary>
        /// İkinci tur kararı: gölgeli yalnız izi görmezden gelinmiş ve ortaya çıkmamış sızmacıda. İzi araştırılmış (tecrit, bulgu yok)
        /// sızmacı gölgelemez; eşik Balance'ta (iki görmezden gelinmiş iz istenince tek iz gölgelemez).
        /// </summary>
        [Test]
        public void VictoryTone_Shadow_OnlyAnIgnoredTraceShadows()
        {
            var sim = Open("iz", b => { b.secondTraceGapMin = b.secondTraceGapMax = 20; b.isolationFindChance = 0; });
            string id = sim.PendingShelterEvent.PersonId;
            Decide(sim, "tecrit");
            for (int i = 0; i < sim.B.isolationShifts; i++) CloseTo(sim, "-");
            Assert.AreEqual(1, Log(sim, "bulgu_yok").Count, "tecrit bulgu vermedi");
            Assert.IsFalse(sim.Board.Person(id).Isolated);
            WinNow(sim);
            Assert.AreEqual("temiz", sim.VictoryTone, "araştırılmış iz gölgelemez");
            Assert.IsTrue(sim.EndReport.Criteria[0].Met);
            var hidden = sim.EndReport.Hidden.Single();
            Assert.AreEqual(id, hidden.Id);
            Assert.AreEqual(1, hidden.Clues);
            Assert.AreEqual(0, hidden.IgnoredTraces);

            sim = Open("iz", b => { b.secondTraceGapMin = b.secondTraceGapMax = 20; b.victoryShadowIgnoredTraces = 2; });
            Decide(sim, "yoksay");
            sim.Shelter.Exposure = 0;
            WinNow(sim);
            Assert.AreEqual("temiz", sim.VictoryTone, "eşik 2 iken tek görmezden gelinmiş iz gölgelemez");
            Assert.AreEqual(1, sim.EndReport.Hidden.Single().IgnoredTraces);

            sim = Open("iz", b => b.secondTraceGapMin = b.secondTraceGapMax = 20);
            Decide(sim, "yoksay");
            WinNow(sim);
            Assert.AreEqual("golgeli", sim.VictoryTone, "varsayılan: tek görmezden gelinmiş iz gölgeler");
        }

        [Test]
        public void VictoryTone_ThreeTones_RowByRow()
        {
            // Ölüm satırı burada sabit sınırla (2) sınanır; harekât uzunluğuyla ölçeklenmesi VictoryTone_DeathLimit_ScalesWithCampaignLength'te
            Balance Fixed() { var b = Quiet(); b.victoryCleanMaxDeaths = 2; b.victoryCleanDeathShifts = 0; return b; }
            var sim = New(Fixed());
            WinNow(sim);
            Assert.AreEqual("temiz", sim.VictoryTone);
            var rows = sim.EndReport.Criteria;
            CollectionAssert.AreEqual(new[] { "golge", "olum", "aclik", "ifsa", "guven" }, rows.Select(r => r.Id).ToArray());
            Assert.IsTrue(rows.All(r => r.Met));

            // Her ölçüt tek başına bozulunca bedelli; satır "tutmadı" der
            void Costly(int row, Action<Simulation> arrange)
            {
                var s = New(Fixed());
                arrange(s);
                WinNow(s);
                Assert.AreEqual("bedelli", s.VictoryTone, rows[row].Id);
                var criteria = s.EndReport.Criteria;
                for (int i = 0; i < criteria.Count; i++) Assert.AreEqual(i != row, criteria[i].Met, criteria[i].Id);
                Assert.AreEqual("bedelli", s.EndReport.Tone);
            }
            Costly(1, s => { foreach (var p in s.Board.People.Skip(4).Take(3)) p.Alive = false; });
            Costly(2, s => s.Shelter.Deserted = 1);
            Costly(3, s => s.Shelter.Exposure = 3);
            Costly(4, s => s.Shelter.MinTrust = 4);

            // Ölüm 2 ve ifşa 2 temizdir (sınır dahil)
            sim = New(Fixed());
            foreach (var p in sim.Board.People.Skip(4).Take(2)) p.Alive = false;
            sim.Shelter.Exposure = 2;
            WinNow(sim);
            Assert.AreEqual("temiz", sim.VictoryTone);

            // Güven harekât boyunca izlenir: sivil bedelle düşüp sonra geri çıkması temizliği bozar
            sim = New(Quiet());
            Assert.AreEqual(sim.S.Trust, sim.Shelter.MinTrust, "başlangıç güveni");
            FillSquad(sim);
            StartRaid(sim);
            // Kayıpsız geri çekilme; giriş odasında güvenin üç fazlası kadar sivil kırıldı (test düzeni)
            int costly = sim.S.Trust - 3;
            FacilityKit.ResolveRaid(sim, false, null, null, B => { if (B.Room.Type == RoomType.Entry && !B.Escape) B.Collateral = costly; });
            int low = sim.S.Trust;
            Assert.Less(low, sim.B.victoryCleanMinTrust);
            Assert.AreEqual(low, sim.Shelter.MinTrust);
            sim.S.Trust = sim.TrustMax;
            Close(sim);
            WinNow(sim);
            Assert.AreEqual("bedelli", sim.VictoryTone);
            Assert.IsFalse(sim.EndReport.Criteria[4].Met);
            Assert.AreEqual(low, sim.EndReport.Criteria[4].Value);

            // İzli ve ortaya çıkmamış sızmacı içerideyken gölgeli
            sim = Open("iz", b => b.secondTraceGapMin = b.secondTraceGapMax = 20);
            Decide(sim, "yoksay");
            string infiltrator = Log(sim, "iz").Single().PersonId;
            WinNow(sim);
            Assert.AreEqual("golgeli", sim.VictoryTone);
            Assert.IsFalse(sim.EndReport.Criteria[0].Met);
            Assert.AreEqual(1, sim.EndReport.Criteria[0].Value);
            var hidden = sim.EndReport.Hidden.Single();
            Assert.AreEqual(infiltrator, hidden.Id);
            Assert.AreEqual("icerde", hidden.Where);
            Assert.AreEqual(1, hidden.Clues);
            Assert.AreEqual(1, hidden.IgnoredTraces);

            // İzsiz sızmacı gölgelemez, raporun kapı bölümünde görünür
            sim = New(Robots(Quiet(1), delay: 50));
            string quiet = Admit(sim);
            ShutDoor(sim);
            WinNow(sim);
            Assert.AreEqual("temiz", sim.VictoryTone);
            hidden = sim.EndReport.Hidden.Single();
            Assert.AreEqual(quiet, hidden.Id);
            Assert.AreEqual("icerde", hidden.Where);
            Assert.AreEqual(0, hidden.Clues);
            Assert.IsTrue(sim.EndReport.Survivors.Single(p => p.Id == quiet).Infiltrator);
            Assert.IsFalse(sim.EndReport.Survivors.Where(p => p.Id != quiet).Any(p => p.Infiltrator));

            // Ortaya çıkmış (sabotaj) sızmacı gölgelemez
            sim = Open("sabotaj");
            Decide(sim, "sorgula");
            WinNow(sim);
            Assert.AreEqual("bedelli", sim.VictoryTone, "ifşa 3");
            Assert.IsTrue(sim.EndReport.Criteria[0].Met);
            Assert.IsEmpty(sim.EndReport.Hidden);
        }

        // ---------------------------------------------------------------- son raporu

        [Test]
        public void EndReport_DoorCounts_SentHumans_HiddenInfiltrators_Deserters()
        {
            var b = Robots(Quiet(1), delay: 1, gap: 50);
            b.boardResidentCapacity = 14;
            var sim = New(b);
            // İlk grup insandır (geri çevrilir), sonraki gruplar sızmacı
            int humansPassed = NextGroup(sim).Count;
            Assert.IsTrue(sim.PassCandidates().Ok);
            int robotsPassed = NextGroup(sim).Count;
            Assert.IsTrue(sim.PassCandidates().Ok);
            var group = NextGroup(sim);
            string robot = group[0].Resident.Id;
            robotsPassed += group.Count - 1;
            Assert.IsTrue(sim.ChooseCandidate(robot).Ok);
            ShutDoor(sim);
            var e = CloseTo(sim, "iz");
            Assert.AreEqual(robot, e.PersonId);
            Decide(sim, "yolla");

            // İnsan: iz bırakır, yollanır
            sim.B.candidateTier0Robot = sim.B.candidateTier1Robot = sim.B.candidateTier2Robot = 0;
            sim.B.humanTraceChance = 1;
            sim.Admission.NextArrivalShift = sim.S.Day + 1;
            group = NextGroup(sim);
            string human = group[0].Resident.Id;
            int humansPassed2 = group.Count - 1;
            Assert.IsTrue(sim.ChooseCandidate(human).Ok);
            ShutDoor(sim);
            e = CloseTo(sim, "iz");
            Assert.AreEqual(human, e.PersonId);
            Decide(sim, "yolla");

            // İzsiz sızmacı içeride kalır
            sim.B.candidateTier0Robot = sim.B.candidateTier1Robot = sim.B.candidateTier2Robot = 1;
            sim.B.traceDelayMin = sim.B.traceDelayMax = 80;
            sim.Admission.NextArrivalShift = sim.S.Day + 1;
            group = NextGroup(sim);
            string quiet = group[0].Resident.Id;
            robotsPassed += group.Count - 1;
            Assert.IsTrue(sim.ChooseCandidate(quiet).Ok);
            ShutDoor(sim);

            // Bir kayıp ve bir açlık ayrılması (yeni gelen o kapanışta ayrılmaz)
            Raid(sim, false, 1, 0, 0);
            string dead = sim.Board.People.Single(p => !p.Alive).Id;
            sim.B.desertStreak = 1;
            CloseHungry(sim);
            sim.B.desertStreak = 0;
            sim.Shelter.Supplies = 50;
            Assert.AreEqual(1, sim.Shelter.Deserted);
            string deserter = Log(sim, "ayrilma").Single().PersonId;
            CloseUntilShift(sim, 5);
            Assert.IsNull(sim.EndReport, "son yokken null");
            Assert.IsTrue(sim.PullLastSwitch().Ok);

            var r = sim.EndReport;
            Assert.AreEqual("salter", r.Ending);
            Assert.IsNull(r.Tone);
            Assert.IsEmpty(r.Criteria);
            Assert.AreEqual(sim.S.Day, r.Shift);
            Assert.AreEqual(3, r.Admitted);
            Assert.AreEqual(2, r.Sent);
            Assert.AreEqual(1, r.SentHumans);
            Assert.AreEqual(human, r.SentHumanList.Single().Id);
            Assert.AreEqual("yollandi", r.SentHumanList.Single().Where);
            Assert.AreEqual(0, r.Removed);
            Assert.AreEqual(humansPassed + robotsPassed + humansPassed2, r.Passed);
            Assert.AreEqual(robotsPassed, r.PassedInfiltrators);
            CollectionAssert.AreEquivalent(new[] { robot + ":yollandi:1", quiet + ":icerde:0" },
                r.Hidden.Select(p => p.Id + ":" + p.Where + ":" + p.Clues).ToArray(), "yollanan sızmacı da ortaya çıkmamıştır");
            Assert.AreEqual(dead, r.Dead.Single().Id);
            Assert.AreEqual(1, r.Deaths);
            Assert.AreEqual(deserter, r.Deserters.Single().Id);
            Assert.AreEqual("aclik", r.Deserters.Single().Where);
            Assert.GreaterOrEqual(r.Deserters.Single().Shift, 0);
            Assert.AreEqual(Alive(sim), r.Survivors.Count);
            Assert.IsTrue(r.Survivors.Any(p => p.Founder));
            Assert.IsEmpty(r.ExposureSteps, "yollamak ifşa değildir");
            Assert.AreEqual(0, r.Exposure);
            Assert.AreEqual(sim.Shelter.MinTrust, r.MinTrust);
            Assert.AreEqual(0, r.RegionsFree);
            Assert.AreEqual(0, r.SectorsFree);

            // İfşa adımları kişi adıyla
            sim = Open("sabotaj");
            string id = sim.PendingShelterEvent.PersonId;
            Decide(sim, "sok");
            CloseUntilShift(sim, 5);
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            r = sim.EndReport;
            CollectionAssert.AreEqual(new[] { "iz", "ikinci_iz", "sabotaj" }, r.ExposureSteps.Select(x => x.Cause).ToArray());
            Assert.IsTrue(r.ExposureSteps.All(x => x.PersonId == id && !string.IsNullOrEmpty(x.Name)));
            CollectionAssert.AreEqual(new[] { 1, 1, 2 }, r.ExposureSteps.Select(x => x.Delta).ToArray());
            Assert.AreEqual(1, r.Removed);
            Assert.IsEmpty(r.Hidden, "sökülen sızmacı ortaya çıkmıştı");
        }

        // ---------------------------------------------------------------- gizlilik

        [Test]
        public void Privacy_VictoryToneAndEndReport_NullWithoutAnEnding()
        {
            // Uyarı vardiyası, işleyen terk sayacı, gizli ve izli sızmacı: ikisi de null
            var sim = Open("iz", b => b.secondTraceGapMin = b.secondTraceGapMax = 20);
            Decide(sim, "yoksay");
            sim.Shelter.Exposure = 6;
            CloseTo(sim, "-");
            Assert.IsTrue(sim.Shelter.FallPending);
            sim.Shelter.UnderstaffedStreak = 2;
            Assert.IsNull(sim.VictoryTone);
            Assert.IsNull(sim.EndReport);

            // Zafer dışı sonlarda ton yok, rapor dolu
            Close(sim);
            Assert.AreEqual("dusus", sim.S.Ending);
            Assert.IsNull(sim.VictoryTone);
            Assert.IsNotNull(sim.EndReport);
            Assert.AreEqual("dusus", sim.EndReport.Ending);
            Assert.IsNull(sim.EndReport.Tone);
            Assert.IsNull(sim.EndReport.SwitchKind);
            Assert.AreEqual(1, sim.EndReport.Hidden.Count);

            // Yeni sayaçlar public durumda; kimlik alanları (internal) serileşmez. Kimlikten türemedikleri ayrıca sınanır:
            // HiddenIdentity_ExposureWarning_HumanAndInfiltrator_SameShelterState.
            string json = BoardJson(sim) + Json(sim.S);
            foreach (string visible in new[] { "\"FallPending\"", "\"UnderstaffedStreak\"", "\"DesertedSinceFull\"", "\"EmergencyDoorShift\"", "\"MinTrust\"", "\"SwitchKind\"" })
                StringAssert.Contains(visible, Json(sim.S), visible);
            foreach (string hidden in new[] { "\"Robot\"", "\"TraceDelay\"", "\"Clock\"", "\"TraceClock\"", "\"Sabotaged\"" })
                StringAssert.DoesNotContain(hidden, json, hidden);
        }

        /// <summary>
        /// Gizlilik: aynı tohumda iz bırakan bir insan ile bir sızmacı, izleri görmezden gelinip ifşa eşiğe varırken birebir aynı
        /// sığınak durumunu (ifşa, uyarı, sayaçlar), fişi, kâğıt bedellerini, şalter nedenlerini ve alarm ara bedelini görür. İkisi
        /// iki uyarı vardiyası boyunca aynı kalır; ikinci kapanışta ikisi de düşer ve kimlik yalnız son raporunda ayrışır.
        /// </summary>
        [Test]
        public void HiddenIdentity_ExposureWarning_HumanAndInfiltrator_SameShelterState()
        {
            string Visible(Simulation s) => Json(s.Shelter) + "|" + s.BoardReceiptPreview + "|" + string.Join(",", s.LastSwitchReasons())
                + "|" + s.LocationCompromised + "|" + s.UnderstaffedCounting + "|" + s.ExposureAlarmBonus + "|" + s.AlarmPerShift + "|" + s.S.Alarm
                + "|" + s.S.Rng.State + "|" + s.S.Ending + "|" + (s.PendingShelterEvent?.Kind ?? "-")
                + "|" + string.Join(";", s.ShelterEventOptions().Select(o => o.Id + ":" + o.Cost + ":" + o.Risk + ":" + o.Open));
            // Müdahale süresi uzun: sabotaj iki uyarı vardiyasından sonra gelirdi (kimlik düşüşten önce ayrışmasın)
            var pair = new[] { Robots(Quiet(1)), Humans(Quiet(1), 1, 1) }.Select(b => { b.interventionShifts = 5; return New(b); }).ToArray();
            var ids = pair.Select(s => Admit(s)).ToArray();
            Assert.AreEqual(ids[0], ids[1]);
            foreach (var s in pair) { ShutDoor(s); s.Shelter.Exposure = 4; }
            foreach (var s in pair) CloseTo(s, "iz");
            Assert.AreEqual(Visible(pair[0]), Visible(pair[1]), "iz");
            foreach (var s in pair) Decide(s, "yoksay");
            Assert.AreEqual(Visible(pair[0]), Visible(pair[1]), "iz yoksay");
            foreach (var s in pair) CloseTo(s, "ikinci_iz");
            Assert.AreEqual(Visible(pair[0]), Visible(pair[1]), "ikinci iz: eşik metni ve sabotaj riski aynı");
            StringAssert.Contains("kapanışta konum çözülür", Option(pair[0], "yoksay").Cost);
            foreach (var s in pair) Decide(s, "yoksay");
            Assert.AreEqual(6, pair[1].Shelter.Exposure);
            Assert.AreEqual(Visible(pair[0]), Visible(pair[1]), "eşikte");
            foreach (var s in pair) Close(s);
            Assert.IsTrue(pair[1].Shelter.FallPending);
            Assert.AreEqual(Visible(pair[0]), Visible(pair[1]), "uyarı vardiyası (müdahale vardiyası)");
            // Kararla varılan eşik (ikinci tur kararı): uyarının ilk kapanışı ikisini de düşürmez; ikinci uyarı vardiyası da aynı
            foreach (var s in pair) Close(s);
            Assert.IsNull(pair[1].S.Ending);
            Assert.AreEqual(1, pair[1].FallShiftsLeft, "ikinci uyarı vardiyası son");
            Assert.AreEqual(Visible(pair[0]), Visible(pair[1]), "ikinci uyarı vardiyası");
            foreach (var s in pair) Close(s);
            Assert.AreEqual("dusus", pair[1].S.Ending);
            Assert.AreEqual(1, pair[0].EndReport.Hidden.Count, "kimlik yalnız son raporunda");
            Assert.IsEmpty(pair[1].EndReport.Hidden);
        }

        /// <summary>Okuyucular çekim yapmaz ve durumu değiştirmez; başarısız şalter kayda yazılmaz.</summary>
        [Test]
        public void NewReaders_DrawNothing()
        {
            var sim = Open("iz", b => { b.secondTraceGapMin = b.secondTraceGapMax = 20; b.lastSwitchMinShift = 99; });
            Decide(sim, "yoksay");
            sim.Shelter.Exposure = 6;
            uint rng = sim.S.Rng.State;
            string before = Json(sim.S);
            int commands = sim.Record.commands.Count;
            for (int i = 0; i < 3; i++)
            {
                _ = sim.VictoryTone;
                _ = sim.EndReport;
                _ = sim.LastSwitchCheck();
                _ = sim.LastSwitchReasons();
                _ = sim.LocationCompromised;
                _ = sim.ExposureAlarmBonus;
                _ = sim.AlarmPerShift;
                _ = sim.BoardReceiptPreview;
                Assert.IsFalse(sim.PullLastSwitch().Ok, "erken");
            }
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(before, Json(sim.S));
            Assert.AreEqual(commands, sim.Record.commands.Count);

            CloseTo(sim, "-");
            Close(sim);
            Assert.AreEqual("dusus", sim.S.Ending);
            rng = sim.S.Rng.State;
            before = Json(sim.S);
            Assert.AreEqual(Json(sim.EndReport), Json(sim.EndReport), "her okumada aynı");
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(before, Json(sim.S));
        }

        // ---------------------------------------------------------------- veri, denge, kayıt

        [Test]
        public void GameData_Endings_ContainEveryId()
        {
            CollectionAssert.AreEqual(new[] { "topluluk", "silme", "dagilma", "vuruldu", "sizma", "dusus", "terk", "salter" },
                GameData.Endings.Select(e => e.Id).ToArray());
            foreach (var e in GameData.Endings)
            {
                Assert.AreSame(e, GameData.Ending(e.Id));
                Assert.IsFalse(string.IsNullOrEmpty(e.Title), e.Id);
                Assert.IsFalse(string.IsNullOrEmpty(e.Text), e.Id);
            }
            Assert.AreNotEqual(GameData.Ending("sizma").Text, GameData.Ending("dusus").Text, "dusus sizma ile aynı cümleyi kullanmaz");
        }

        [Test]
        public void Balance_EndingFields_DefaultsAndFlat()
        {
            var b = new Balance();
            Assert.AreEqual(6, b.exposureThreshold);
            Assert.AreEqual(2, b.exposureWarnAt1);
            Assert.AreEqual(4, b.exposureWarnAt2);
            Assert.AreEqual(3, b.exposureAlarmAt);
            Assert.AreEqual(0.5, b.exposureAlarmBonus);
            Assert.AreEqual(3, b.abandonUnderstaffedShifts);
            Assert.AreEqual(5, b.lastSwitchMinShift);
            Assert.AreEqual(2, b.evacuationTrustBelow);
            Assert.AreEqual(0, b.victoryCleanMaxDeaths);
            Assert.AreEqual(2, b.victoryCleanMaxExposure);
            Assert.AreEqual(5, b.victoryCleanMinTrust);
            Assert.AreEqual(2, b.exposureDecisionWarnShifts);
            Assert.AreEqual(15, b.victoryCleanDeathShifts);
            Assert.AreEqual(1, b.victoryShadowIgnoredTraces);
            var fields = typeof(Balance).GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (string name in new[] { "exposureThreshold", "exposureWarnAt1", "exposureWarnAt2", "exposureAlarmAt", "exposureAlarmBonus",
                         "abandonUnderstaffedShifts", "lastSwitchMinShift", "evacuationTrustBelow", "victoryCleanMaxDeaths",
                         "victoryCleanMaxExposure", "victoryCleanMinTrust", "exposureDecisionWarnShifts", "victoryCleanDeathShifts",
                         "victoryShadowIgnoredTraces" })
            {
                var f = fields.Single(x => x.Name == name);
                Assert.IsTrue(f.FieldType == typeof(int) || f.FieldType == typeof(double), name);
            }
            var copy = b.Clone();
            copy.exposureThreshold = 9;
            copy.exposureAlarmBonus = 2;
            copy.exposureDecisionWarnShifts = 5;
            copy.victoryCleanDeathShifts = 1;
            Assert.AreEqual(6, b.exposureThreshold);
            Assert.AreEqual(0.5, b.exposureAlarmBonus);
            Assert.AreEqual(2, b.exposureDecisionWarnShifts);
            Assert.AreEqual(15, b.victoryCleanDeathShifts);
            Assert.AreEqual(22, GameRecord.CurrentVersion, "22: görevde iyileşme; ifşa eşiği, ara bedel, dusus, terk ve son şalter (biçim 17; 18 üretilmiş harita; 19: tesis oda zinciri (raid_room), tesis parmak izi; 20: g5 paket yerleşimi; 21: tesis baskını tek kip)");
            Assert.AreEqual(22, new GameRecord().version);
        }

        [Test]
        public void Replay_LastSwitch_JsonRoundTrip_SameEndingAndKind()
        {
            // Yalnız komutla kurulmuş oyun
            var sim = New(Quiet());
            CloseUntilShift(sim, 6);
            Assert.IsTrue(sim.AssignResident(sim.Board.People[3].Id, "cook").Ok);
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(RoundTrip(sim.Record));
            Assert.AreEqual("salter", restored.S.Ending);
            Assert.AreEqual(sim.S.Day, restored.S.Day);
            Assert.AreEqual(sim.Shelter.SwitchKind, restored.Shelter.SwitchKind);
            Assert.AreEqual(sim.Shelter.SwitchShift, restored.Shelter.SwitchShift);
            Assert.AreEqual(State(sim), State(restored));
            Assert.AreEqual(Json(sim.EndReport), Json(restored.EndReport));
            Assert.AreEqual(22, RoundTrip(sim.Record).version);

            // Tahliye çeşidi: erzaksız üç kişilik sığınak; ayrılmadan sonraki ilk vardiyada çekilir (yalnız komutlar)
            var t = Starve(3);
            t.boardStartSupplies = 0;
            t.lastSwitchMinShift = 1;
            sim = New(t);
            while (Alive(sim) >= 3) Close(sim);
            CollectionAssert.AreEqual(new[] { "kadro", "aclik" }, sim.LastSwitchReasons());
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            Assert.AreEqual("tahliye", sim.Shelter.SwitchKind);
            sim.Record.day = sim.S.Day;
            restored = Replay.Restore(RoundTrip(sim.Record));
            Assert.AreEqual("salter", restored.S.Ending);
            Assert.AreEqual("tahliye", restored.Shelter.SwitchKind);
            CollectionAssert.AreEqual(sim.Shelter.SwitchReasons, restored.Shelter.SwitchReasons);
            Assert.AreEqual(State(sim), State(restored));
        }

        /// <summary>İki görmezden gelinen sızmacı ifşayı eşiğe taşır; kayıt aynı düşüşe, kayıt ortasından devam da aynı sona varır.</summary>
        [Test]
        public void Replay_FallAndAbandon_ContinueToTheSameEnding()
        {
            // dusus: yalnız komutlar (iki sızmacı, bütün izler görmezden gelinir, sabotajcı sökülür)
            Simulation PlayToFall(Simulation s, int limit)
            {
                while (s.S.Ending == null && s.S.Day < limit)
                {
                    var e = s.PendingShelterEvent;
                    if (e != null) Assert.IsTrue(s.DecideShelterEvent(e.Kind == "sabotaj" ? "sok" : "yoksay").Ok);
                    if (s.Admission.Group.Count > 0)
                    {
                        if (s.Admission.Files.Count(f => !f.Founder) < 2 && s.Admission.GeneratedGroups > 1)
                            Assert.IsTrue(s.ChooseCandidate(s.Admission.Group[0].Resident.Id).Ok);
                        else Assert.IsTrue(s.PassCandidates().Ok);
                    }
                    Close(s);
                }
                return s;
            }
            var b = Robots(Quiet(1), delay: 1, gap: 1);
            var sim = PlayToFall(New(b), 40);
            Assert.AreEqual("dusus", sim.S.Ending);
            // Eşiğe görmezden gelinen izle varıldı: iki uyarı vardiyasında sabotaj da yazdı (tavansız)
            Assert.GreaterOrEqual(sim.Shelter.Exposure, sim.B.exposureThreshold);
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(RoundTrip(sim.Record));
            Assert.AreEqual("dusus", restored.S.Ending);
            Assert.AreEqual(sim.S.Day, restored.S.Day);
            Assert.AreEqual(State(sim), State(restored));

            // Kayıt ortasından devam: düşüşten önceki bir fiş kapanışına kadar kesilen kayıt, aynı oyunla sürdürülür
            var cut = RoundTrip(sim.Record);
            int k = cut.commands.FindLastIndex(c => c.kind == "board_file" && c.day < sim.S.Day - 3);
            Assert.Greater(k, 0);
            cut.day = cut.commands[k].day + 1;
            cut.commands = cut.commands.Take(k + 1).ToList();
            var resumed = PlayToFall(Replay.Restore(cut), 40);
            Assert.AreEqual("dusus", resumed.S.Ending);
            Assert.AreEqual(sim.S.Day, resumed.S.Day);
            Assert.AreEqual(State(sim), State(resumed));

            // terk: erzaksız başlayan üç kişilik sığınak, yalnız komutlar
            var t = Starve(3);
            t.boardStartSupplies = 0;
            sim = New(t);
            for (int i = 0; i < 12 && sim.S.Ending == null; i++) Close(sim);
            Assert.AreEqual("terk", sim.S.Ending);
            sim.Record.day = sim.S.Day;
            restored = Replay.Restore(RoundTrip(sim.Record));
            Assert.AreEqual("terk", restored.S.Ending);
            Assert.AreEqual(sim.S.Day, restored.S.Day);
            Assert.AreEqual(State(sim), State(restored));
        }
    }
}
