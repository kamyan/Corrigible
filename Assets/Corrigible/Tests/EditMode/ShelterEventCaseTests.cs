using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.ShelterEventKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Sığınak olaylarının tek tek etkileri (tetik, her seçenek, vadeli sonucun iki ucu), açlık ve ayrılma, tayın kipi, karartma,
    /// kişi durumları (kısıtlı, affedilmiş, görev alamayan) ve D3 kararı (kısıtlı ya da tecritteki kişinin bağlı kiti).
    /// </summary>
    public class ShelterEventCaseTests
    {
        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        static Simulation Chance(string kind, double chance) => Open(kind, b =>
        {
            Limit(b, kind, 1);
            b.bigTheftChance = b.woundWorsenChance = b.feudInjuryChance = b.griefChance = b.callRefuseChance = chance;
        });

        // ---------------------------------------------------------------- kaçak

        [Test]
        public void Deserter_PersuadeLetGoOrLock()
        {
            // Tetik: aç kapanış ve açlık sayacı eşikte (varsayılan 2); tok kapanışta açılmaz
            var b = On("kacak");
            var sim = New(b);
            sim.Shelter.Supplies = 0;
            CloseTo(sim, "-");
            Assert.AreEqual(1, sim.Shelter.HungryStreak);
            sim.Shelter.Supplies = 0;
            var e = CloseTo(sim, "kacak");
            Assert.AreEqual(2, sim.Shelter.HungryStreak);
            var person = sim.Board.Person(e.PersonId);
            Assert.IsTrue(person.Alive && person.Down <= 0 && !person.Restricted && !person.Isolated);
            Assert.AreEqual(Loc.F("{0} gitmek istiyor: “Burada açlıktan öleceğiz.”", person.Name), sim.ShelterEventBody(e));
            CollectionAssert.AreEqual(new[] { "ikna", "birak", "kilitle" }, sim.ShelterEventOptions().Where(o => o.Open).Select(o => o.Id).ToArray());

            // İkna: kişi kalır, bu vardiya görev alamaz; erzak istemez
            var f = sim.Admission.File(e.PersonId);
            Decide(sim, "ikna");
            Assert.AreEqual("rest", person.Job);
            Assert.AreEqual(sim.S.Day + 1, f.IdleUntil);
            Assert.AreEqual(Loc.T("Bu sakin bu vardiya görev alamaz."), sim.AssignResident(person.Id, "cook").Msg);
            Assert.AreEqual(Loc.T("Bu sakin bu vardiya görev alamaz."), sim.TrainResident(person.Id, "cook").Msg);
            Assert.AreEqual(0, sim.Shelter.Supplies);
            sim.Shelter.Supplies = 50;
            CloseTo(sim, "-");
            Assert.IsTrue(sim.AssignResident(person.Id, "cook").Ok || person.Job == "cook", "sonraki vardiya görev alır");

            // Bırak: kişi yolluksuz gider, ayrılan sayacı artar
            sim = Open("kacak");
            e = sim.PendingShelterEvent;
            f = sim.Admission.File(e.PersonId);
            sim.Shelter.Supplies = 10;
            Decide(sim, "birak");
            Assert.IsNull(sim.Board.Person(e.PersonId));
            Assert.AreEqual("birakildi", f.DepartReason);
            Assert.AreEqual(1, sim.Shelter.Deserted);
            Assert.AreEqual(10, sim.Shelter.Supplies, "bırakılan kişi yolluk almaz");
            Assert.AreEqual(7, sim.Board.People.Count(p => p.Alive));

            // Kapıyı kilitle: üç vardiya kısıt, güven −1
            sim = Open("kacak");
            e = sim.PendingShelterEvent;
            f = sim.Admission.File(e.PersonId);
            int trust = sim.S.Trust;
            Decide(sim, "kilitle");
            Assert.IsTrue(f.Resident.Restricted);
            Assert.AreEqual(sim.S.Day + 3, f.RestrictedUntil);
            Assert.AreEqual(trust - 1, sim.S.Trust);

            // Güven 1 iken kilit kapalı; son kişiyken bırakmak kapalı
            sim = Open("kacak");
            sim.S.Trust = 1;
            Assert.AreEqual(Loc.T("Güven çok düşük."), Option(sim, "kilitle").ClosedReason);
            Assert.AreEqual(Loc.T("Güven çok düşük."), sim.DecideShelterEvent("kilitle").Msg);
            foreach (var p in sim.Board.People.Where(p => p.Id != sim.PendingShelterEvent.PersonId)) p.Alive = false;
            Assert.AreEqual(Loc.T("Son kişi sığınaktan çıkarılamaz."), Option(sim, "birak").ClosedReason);
            Assert.IsTrue(Option(sim, "ikna").Open);
            Assert.AreEqual(1, sim.S.Trust);
        }

        /// <summary>Kilitli planda verilen ikna kararında görev o an değişmez; bedel sonraki vardiyaya kayar.</summary>
        [Test]
        public void Idle_LockedPlanDecision_MovesToTheNextShift()
        {
            var sim = Open("kacak");
            var e = sim.PendingShelterEvent;
            var person = sim.Board.Person(e.PersonId);
            var f = sim.Admission.File(e.PersonId);
            if (person.Job == "rest") Assert.IsTrue(sim.AssignResident(person.Id, "watch").Ok);
            string job = person.Job;
            sim.Shelter.Supplies = 50;
            Raid(sim, false, 0, 0, 0);
            Assert.IsTrue(sim.Board.Locked);
            Decide(sim, "ikna");
            Assert.AreEqual(job, person.Job, "kilitli planda görev değişmez");
            Assert.AreEqual(sim.S.Day + 2, f.IdleUntil);
            CloseTo(sim, "-");
            Assert.AreEqual("rest", person.Job, "kapanış kişiyi Boşta'ya indirir");
            Assert.AreEqual(Loc.T("Bu sakin bu vardiya görev alamaz."), sim.AssignResident(person.Id, "watch").Msg);
            Assert.IsNull(sim.BoardPlanCheck());
            CloseTo(sim, "-");
            Assert.IsTrue(sim.AssignResident(person.Id, "watch").Ok);
        }

        // ---------------------------------------------------------------- erzak azalıyor ve tayın kipi

        [Test]
        public void LowRations_TriggersBelowThreeShiftsOfConsumption()
        {
            var sim = New(On("erzak_azaliyor"));
            double eaten = sim.BoardConsumption;
            Assert.AreEqual(2.5, eaten, 1e-9, "8 kişi, kademe 1 aşçı: 8 × 0,40 − 0,7");
            // Kapanıştan sonra tam üç vardiyalık erzak: eşik altında değil
            sim.Shelter.Supplies = eaten * 4;
            CloseTo(sim, "-");
            Assert.AreEqual(eaten * 3, sim.Shelter.Supplies, 1e-9);
            sim.Shelter.Supplies = eaten * 4 - 0.01;
            var e = CloseTo(sim, "erzak_azaliyor");
            Assert.IsNull(e.PersonId);
            Assert.AreEqual(Loc.F("Erzak {0} vardiyalık tüketimin altına indi.", 3), sim.ShelterEventBody(e));
            Assert.IsTrue(Option(sim, "tam").Safe);
            Assert.IsNotEmpty(Option(sim, "tam").Risk);
            double supplies = sim.Shelter.Supplies;
            Decide(sim, "tam");
            Assert.IsFalse(sim.HalfRationActive);
            Assert.AreEqual(-1, sim.Shelter.HalfRationUntil);
            Assert.AreEqual(supplies, sim.Shelter.Supplies);
        }

        [Test]
        public void HalfRation_AfterCookSaving_FourClosings_NoHeal_RaidHpRestored()
        {
            var b = On("erzak_azaliyor");
            var sim = New(b);
            double eaten = sim.BoardConsumption;
            sim.Shelter.Supplies = eaten * 2;
            CloseTo(sim, "erzak_azaliyor");
            Decide(sim, "yarim");
            Assert.AreEqual(sim.S.Day + 4, sim.Shelter.HalfRationUntil);
            Assert.IsTrue(sim.HalfRationActive);
            Assert.AreEqual(4, sim.HalfRationShiftsLeft);
            Assert.AreEqual(eaten, sim.BoardConsumption, 1e-12, "tam tayın tüketimi çarpansız okunur");

            // Yaralı ve yatan kişi: yarım tayında can kazanmazlar (Bakım yuvası boş: bakımcının iyileştirmesi ayrı kuraldır)
            sim.Board.People.First(p => p.Job == "care").Job = "watch";
            var resting = sim.Board.People.First(p => p.Job == "rest");
            resting.Hp = 1;
            var bed = sim.Board.People.First(p => p.Job == "gather");
            bed.Hp = 1;
            bed.Down = 2;
            bed.Job = "rest";

            // Baskın kadrosu 1 can eksik çıkar (en az 1), dönüşte farkı geri alır
            var squadPeople = PlanningBoardData.FieldJobs.Select(r => sim.Board.People.First(p => p.Job == r)).ToList();
            squadPeople[2].Hp = 1;
            var hpBefore = squadPeople.Select(p => p.Hp).ToArray();
            var slots = sim.PlanningSquad();
            CollectionAssert.AreEqual(new[] { hpBefore[0] - 1, hpBefore[1] - 1, 1 }, slots.Select(s => s.Hp).ToArray());
            Raid(sim, false, 0, 0, 0);
            CollectionAssert.AreEqual(hpBefore, squadPeople.Select(p => p.Hp).ToArray(), "eksik çıkılan can kalıcı kayıp değildir");

            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(sim.HalfRationActive, "kapanış " + i);
                Assert.AreEqual(4 - i, sim.HalfRationShiftsLeft);
                StringAssert.Contains(Loc.F("ERZAK · yarım tayın, {0} vardiya kaldı.", 4 - i), sim.BoardReceiptPreview);
                // Tüketim, aşçı tasarrufu düşüldükten sonra yarıya iner; fiş ön izlemesi kapanışla aynı sayıyı yazar
                double preview = sim.BoardFoodPreview, before = sim.Shelter.Supplies;
                // İlk üç kapanışta erzak eşiğin altında kalır (yarım tayın olayı bastırır); son kapanışta tayın biter ve eşik
                // yeniden işler, o yüzden üç vardiyalık tüketimin üstünde kurulur (sabit 10 tüketime bağlıydı)
                sim.Shelter.Supplies = before = i < 3 ? eaten * 2 : eaten * 4;
                preview = sim.BoardFoodPreview;
                Assert.AreEqual(-sim.BoardConsumption * 0.5, preview, 1e-12);
                // Yarım tayın sürerken erzak olayı yeniden açılmaz
                CloseTo(sim, "-");
                Assert.AreEqual(before + preview, sim.Shelter.Supplies, 1e-12);
                Assert.AreEqual(preview, sim.Board.LastSuppliesChange, 1e-12);
                Assert.AreEqual(1, resting.Hp, "dinlenme iyileştirmez");
                Assert.AreEqual(1, bed.Hp, "yatış can vermez");
            }
            Assert.AreEqual(0, bed.Down, "yatış süresi yine azalır");
            Assert.IsFalse(sim.HalfRationActive);
            Assert.AreEqual(0, sim.HalfRationShiftsLeft);
            Assert.IsFalse(sim.BoardReceiptPreview.Contains("yarım tayın"));
            CollectionAssert.AreEqual(squadPeople.Select(p => p.Hp).ToArray(), sim.PlanningSquad().Select(s => s.Hp).ToArray(), "tam tayında kadro kendi canıyla çıkar");

            // Beşinci kapanış tam tayın: dinlenme yeniden iyileştirir, erzak olayı yeniden açılabilir
            sim.Shelter.Supplies = 5;
            double full = sim.BoardFoodPreview;
            Assert.AreEqual(-sim.BoardConsumption, full, 1e-12);
            if (resting.Job != "rest") Assert.IsTrue(sim.AssignResident(resting.Id, "rest").Ok);
            CloseTo(sim, "erzak_azaliyor");
            Assert.AreEqual(5 + full, sim.Shelter.Supplies, 1e-12);
            Assert.AreEqual(2, resting.Hp);
        }

        /// <summary>Yarım tayın açlığı önler: tam tayınla aç geçecek kapanış yarım tayınla tok geçer.</summary>
        [Test]
        public void HalfRation_KeepsTheShelterFed_AndTheftCanFollow()
        {
            var b = On("erzak_azaliyor", "hirsizlik");
            Limit(b, "hirsizlik", 9, 0);
            var sim = New(b);
            double eaten = sim.BoardConsumption;
            sim.Shelter.Supplies = eaten * 2;
            CloseTo(sim, "erzak_azaliyor");
            Decide(sim, "yarim");
            // Bir vardiyalık tam tayından az, yarım tayından çok erzak
            sim.Shelter.Supplies = eaten * 0.75;
            CloseTo(sim, "-");
            Assert.IsFalse(sim.Shelter.Starving);
            Assert.AreEqual(0, sim.Shelter.HungryStreak);
            Assert.AreEqual(eaten * 0.25, sim.Shelter.Supplies, 1e-9);
            // Yarım tayın sürerken stok eşiğin üstünde olsa da hırsızlık tetiklenir
            sim.Shelter.Supplies = 40;
            var e = CloseTo(sim, "hirsizlik");
            Assert.AreEqual(2, e.Amount);
        }

        // ---------------------------------------------------------------- hırsızlık ve büyük hırsızlık

        [Test]
        public void Theft_SendAwayPardonOrRestrict()
        {
            var sim = New(For("hirsizlik"));
            double eaten = sim.BoardConsumption;
            // Çalınacak erzak yoksa (2'nin altı) tetiklenmez
            sim.Shelter.Supplies = eaten + 1.5;
            CloseTo(sim, "-");
            sim.Shelter.Supplies = eaten + 3;
            var e = CloseTo(sim, "hirsizlik");
            Assert.AreEqual(2, e.Amount);
            Assert.AreEqual("erzak", e.Resource);
            Assert.AreEqual(1, sim.Shelter.Supplies, 1e-9, "kayıp açılışta uygulanır");
            var person = sim.Board.Person(e.PersonId);
            var f = sim.Admission.File(e.PersonId);
            Assert.AreEqual(Loc.F("{0} depodan {1:0.##} erzak aldı.", person.Name, 2.0), sim.ShelterEventBody(e));
            Assert.IsTrue(sim.ShelterEventOptions().All(o => o.Open));

            // Yolla: yolluk eldeki kadar (1)
            StringAssert.Contains(Loc.F("Kişi, ustalığı ve kendi teçhizatı gider; {0:0.##} erzak yolluk.", 1.0), Option(sim, "yolla").Cost);
            Decide(sim, "yolla");
            Assert.IsNull(sim.Board.Person(e.PersonId));
            Assert.AreEqual("yollandi", f.DepartReason);
            Assert.AreEqual(0, sim.Shelter.Supplies, 1e-9);
            Assert.AreEqual(0, sim.Shelter.Deserted);

            // Affet: yalnız dosya notu
            sim = Open("hirsizlik");
            e = sim.PendingShelterEvent;
            f = sim.Admission.File(e.PersonId);
            string job = f.Resident.Job;
            Decide(sim, "affet");
            Assert.IsTrue(f.Pardoned);
            Assert.IsFalse(f.Resident.Restricted);
            Assert.AreEqual(job, f.Resident.Job);

            // Kısıtla: üç vardiya
            sim = Open("hirsizlik");
            e = sim.PendingShelterEvent;
            f = sim.Admission.File(e.PersonId);
            Decide(sim, "kisitla");
            Assert.IsTrue(f.Resident.Restricted);
            Assert.AreEqual(sim.S.Day + 3, f.RestrictedUntil);
            Assert.IsFalse(f.Pardoned);
        }

        /// <summary>Affedilmiş kişi bir daha hırsızlık hedefi olmaz; herkes affedildiyse olay açılmaz.</summary>
        [Test]
        public void Pardoned_NeverATheftTargetAgain()
        {
            var b = For("hirsizlik");
            b.bigTheftChance = 0;
            var sim = New(b);
            var thieves = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                Arrange(sim, "hirsizlik");
                Close(sim);
                var e = sim.PendingShelterEvent;
                if (e == null) continue;
                thieves.Add(e.PersonId);
                Decide(sim, "affet");
            }
            Assert.AreEqual(8, thieves.Count, "sekiz kişi birer kez; sonra hedef kalmaz");
            Assert.AreEqual(8, thieves.Distinct().Count());
            Assert.IsTrue(sim.Admission.Files.All(f => f.Pardoned));
            Assert.AreEqual(0, Log(sim, "buyuk_hirsizlik").Count);
        }

        [Test]
        public void BigTheft_FollowsAPardon_DelayWindow_DropsWithoutStock_MaxTwo()
        {
            // Olasılık 1, gecikme 1: bir sonraki kapanışta; en çok 5 erzak, eldeki kadar
            var sim = Open("buyuk_hirsizlik");
            var e = sim.PendingShelterEvent;
            Assert.AreEqual(5, e.Amount);
            Assert.AreEqual(30 - sim.BoardConsumption - 5, sim.Shelter.Supplies, 1e-9);
            var f = sim.Admission.File(e.PersonId);
            Assert.IsTrue(f.Pardoned, "hedef affedilen kişidir");
            Assert.AreEqual(Loc.F("{0} yeniden çaldı: {1:0.##} erzak eksik.", f.Resident.Name, 5.0), sim.ShelterEventBody(e));
            CollectionAssert.AreEqual(new[] { "yolla", "kisitla" }, sim.ShelterEventOptions().Select(o => o.Id).ToArray());
            Decide(sim, "kisitla");
            Assert.IsTrue(f.Resident.Restricted);
            Assert.AreEqual(sim.S.Day + 5, f.RestrictedUntil, "uzun kısıt");
            for (int i = 0; i < 6; i++) { sim.Shelter.Supplies = 30; CloseTo(sim, "-"); }

            sim = Open("buyuk_hirsizlik");
            f = sim.Admission.File(sim.PendingShelterEvent.PersonId);
            Decide(sim, "yolla");
            Assert.AreEqual("yollandi", f.DepartReason);

            // Stok azsa eldeki kadar çalınır; 1'in altındaysa kayıt düşer, ertelenmez
            Action<Balance> soon = b => { Limit(b, "hirsizlik", 1); b.bigTheftChance = 1; b.bigTheftDelayMin = b.bigTheftDelayMax = 1; };
            sim = Open("hirsizlik", soon);
            Decide(sim, "affet");
            sim.Shelter.Supplies = sim.BoardConsumption + 1.5;
            e = CloseTo(sim, "buyuk_hirsizlik");
            Assert.AreEqual(1.5, e.Amount, 1e-9);
            Assert.AreEqual(0, sim.Shelter.Supplies, 1e-9);

            sim = Open("hirsizlik", soon);
            Decide(sim, "affet");
            sim.Shelter.Supplies = sim.BoardConsumption + 0.5;
            CloseTo(sim, "-");
            for (int i = 0; i < 6; i++) { sim.Shelter.Supplies = 30; CloseTo(sim, "-"); }
            Assert.AreEqual(0, Count(sim, "buyuk_hirsizlik"));

            // Olasılık 0: hiç gelmez
            sim = Open("hirsizlik", b => { Limit(b, "hirsizlik", 1); b.bigTheftChance = 0; });
            Decide(sim, "affet");
            for (int i = 0; i < 8; i++) { sim.Shelter.Supplies = 30; CloseTo(sim, "-"); }

            // Gecikme penceresi: karardan 3–5 vardiya sonra; zaman kararın ardından ilk kapanışta çekilir
            var offsets = new HashSet<int>();
            for (uint seed = 1; seed <= 16; seed++)
            {
                sim = Open("hirsizlik", b => { Limit(b, "hirsizlik", 1); b.bigTheftChance = 1; }, seed);
                Decide(sim, "affet");
                int decided = sim.S.Day;
                while (sim.PendingShelterEvent == null && sim.S.Day < decided + 8) { sim.Shelter.Supplies = 30; Close(sim); }
                Assert.AreEqual("buyuk_hirsizlik", Pending(sim), "tohum " + seed);
                offsets.Add(sim.S.Day - decided);
            }
            CollectionAssert.IsSubsetOf(offsets, new[] { 3, 4, 5 });
            Assert.Greater(offsets.Count, 1);

            // Harekât başına en çok iki büyük hırsızlık: üçüncü affın kaydı düşer
            var many = Limit(Quiet(), "hirsizlik", 9);
            many.bigTheftChance = 1;
            many.bigTheftDelayMin = many.bigTheftDelayMax = 1;
            sim = New(many);
            var kinds = new List<string>();
            for (int i = 0; i < 9; i++)
            {
                // Kapanıştan sonra 5 erzak: eşiğin altında (hırsızlık tetiklenir) ve büyük hırsızlığa yetecek kadar
                sim.Shelter.Supplies = sim.BoardConsumption + 5;
                Close(sim);
                if (sim.PendingShelterEvent == null) continue;
                kinds.Add(Pending(sim));
                Decide(sim, ShelterEventData.SafeOption(Pending(sim)));
            }
            Assert.AreEqual(2, kinds.Count(k => k == "buyuk_hirsizlik"), string.Join(",", kinds));
            Assert.Greater(kinds.Count(k => k == "hirsizlik"), 3, string.Join(",", kinds));
        }

        /// <summary>
        /// Büyük hırsızlık zincir değildir: vadesi gelse de olağan olay aralığını bekler ve açılınca aralığı yeniden başlatır
        /// (§3 kural 2–3). Hırsız yatıyor, kısıtlı ya da tecritteyse çalamaz: kayıt bekler, kişi yeniden uygun olunca açılır.
        /// </summary>
        [Test]
        public void BigTheft_WaitsForTheGap_AndForAThiefWhoCanSteal()
        {
            // Hırsızlık X'te açıldı, vade X+1'de geldi; aralık X+3'e dek sürerse büyük hırsızlık X+3'ten önce açılmaz
            var sim = Open("hirsizlik", b => { Limit(b, "hirsizlik", 1); b.bigTheftChance = 1; b.bigTheftDelayMin = b.bigTheftDelayMax = 1; });
            int opened = sim.S.Day;
            Decide(sim, "affet");
            sim.ShelterEvents.NextEventShift = opened + 3;
            sim.Shelter.Supplies = 30;
            CloseTo(sim, "-");
            sim.Shelter.Supplies = 30;
            CloseTo(sim, "-");
            sim.Shelter.Supplies = 30;
            var e = CloseTo(sim, "buyuk_hirsizlik");
            Assert.AreEqual(opened + 3, sim.S.Day, "aralık dolunca");
            Assert.AreEqual(opened + 3, e.PrevNextEventShift);
            Assert.AreEqual(sim.S.Day + 1, sim.ShelterEvents.NextEventShift, "açılış aralığı yeniden başlatır (aralık 1)");

            // Kısıtlı hırsız çalmaz: kısıt bitene dek kayıt bekler, kısıtın bittiği kapanışta açılır
            sim = Open("hirsizlik", b => { Limit(b, "hirsizlik", 1); b.bigTheftChance = 1; b.bigTheftDelayMin = b.bigTheftDelayMax = 1; });
            var f = sim.Admission.File(sim.PendingShelterEvent.PersonId);
            Decide(sim, "affet");
            int decided = sim.S.Day;
            f.Resident.Restricted = true;
            f.Resident.Job = "rest";
            f.RestrictedUntil = decided + 3;
            for (int i = 0; i < 2; i++) { sim.Shelter.Supplies = 30; CloseTo(sim, "-"); }
            Assert.IsTrue(f.Resident.Restricted);
            sim.Shelter.Supplies = 30;
            e = CloseTo(sim, "buyuk_hirsizlik");
            Assert.AreEqual(f.PersonId, e.PersonId);
            Assert.IsFalse(f.Resident.Restricted);

            // Tecritteki hırsız da çalmaz; gitmişse kayıt düşer
            sim = Open("hirsizlik", b => { Limit(b, "hirsizlik", 1); b.bigTheftChance = 1; b.bigTheftDelayMin = b.bigTheftDelayMax = 1; });
            f = sim.Admission.File(sim.PendingShelterEvent.PersonId);
            Decide(sim, "affet");
            f.Resident.Isolated = true;
            f.Resident.Job = "rest";
            f.IsolatedUntil = 999;
            for (int i = 0; i < 3; i++) { sim.Shelter.Supplies = 30; CloseTo(sim, "-"); }
            Assert.AreEqual(0, Count(sim, "buyuk_hirsizlik"));
            Assert.IsTrue(ReplaceAtTheDoor(sim, f.PersonId).Ok);
            for (int i = 0; i < 3; i++) { sim.Shelter.Supplies = 30; CloseTo(sim, "-"); }
            Assert.AreEqual(0, Count(sim, "buyuk_hirsizlik"));
        }

        // ---------------------------------------------------------------- ağır yaralı

        [Test]
        public void Wounded_OpensWhenBedRestStarts_ReviveOrWait()
        {
            // Varsayılan denge: kendi alanında eğitimli saha kişisi 2 vardiya yatar; olay yatışın başladığı kapanışta açılır
            var sim = New(For("agir_yarali"));
            var tek = sim.Board.People.First(p => p.Job == "tek");
            Raid(sim, false, 1, 0, 0);
            Assert.AreEqual(2, tek.Down);
            Assert.AreEqual(0, tek.Hp);
            var e = CloseTo(sim, "agir_yarali");
            Assert.AreEqual(tek.Id, e.PersonId);
            Assert.AreEqual(1, e.Amount, "kâğıt kalan yatışı yazar");
            Assert.AreEqual(1, tek.Down);
            Assert.AreEqual(Loc.F("{0} ağır yaralı döndü: {1} vardiya yatacak.", tek.Name, 1), sim.ShelterEventBody(e));

            // Erzakla ayağa kaldır: 3 erzak, yatış biter, can en az 2
            double supplies = sim.Shelter.Supplies;
            tek.Hp = 1;
            Decide(sim, "kaldir");
            Assert.AreEqual(supplies - 3, sim.Shelter.Supplies);
            Assert.AreEqual(0, tek.Down);
            Assert.AreEqual(2, tek.Hp);
            Assert.IsTrue(sim.AssignResident(tek.Id, "tek").Ok);

            // Erzak yetmezse kapalı
            sim = Open("agir_yarali");
            sim.Shelter.Supplies = 2.9;
            Assert.AreEqual(Loc.F("{0:0.##} erzak gerekiyor.", 3.0), Option(sim, "kaldir").ClosedReason);
            Assert.IsFalse(sim.DecideShelterEvent("kaldir").Ok);
            Assert.IsTrue(Option(sim, "beklet").Open);

            // Beklet, olasılık 1: yatış bir vardiya uzar (o kapanış yatışı bitirecek olsa da)
            sim = Chance("agir_yarali", 1);
            var person = sim.Board.Person(sim.PendingShelterEvent.PersonId);
            Assert.AreEqual(1, person.Down);
            Decide(sim, "beklet");
            CloseTo(sim, "-");
            Assert.AreEqual(1, person.Down, "bitecek yatış bir vardiya uzadı");
            Assert.AreEqual("rest", person.Job);
            Assert.AreEqual(person.Id, Log(sim, "vadeli").Single().PersonId);
            Assert.AreEqual("yatis_uzar", Log(sim, "vadeli").Single().Option);
            Assert.AreEqual(Loc.T("Yatan yaralı önce dinlenmeli."), sim.AssignResident(person.Id, "tek").Msg);
            CloseTo(sim, "-");
            Assert.AreEqual(0, person.Down);
            Assert.AreEqual(1, Log(sim, "vadeli").Count, "uzama bir kez");

            // Beklet, olasılık 0: yatış vaktinde biter
            sim = Chance("agir_yarali", 0);
            person = sim.Board.Person(sim.PendingShelterEvent.PersonId);
            Decide(sim, "beklet");
            CloseTo(sim, "-");
            Assert.AreEqual(0, person.Down);
            Assert.AreEqual(0, Log(sim, "vadeli").Count);

            // Alanında eğitimsiz kişi üç vardiya yatar: kâğıt 2 yazar; uzama onu da bir artırır
            var b = For("agir_yarali");
            b.woundWorsenChance = 1;
            sim = New(b);
            var cook = sim.Board.People.First(p => p.Job == "cook");
            var old = sim.Board.People.First(p => p.Job == "tek");
            Assert.IsTrue(sim.AssignResident(cook.Id, "tek").Ok);
            Assert.IsTrue(sim.EquipResident(sim.Board.Equipment.First(x => x.Kind == "kit_tek").Id, cook.Id).Ok);
            Raid(sim, false, 1, 0, 0);
            Assert.AreEqual(3, cook.Down);
            e = CloseTo(sim, "agir_yarali");
            Assert.AreEqual(cook.Id, e.PersonId);
            Assert.AreEqual(2, e.Amount);
            Decide(sim, "beklet");
            CloseTo(sim, "-");
            Assert.AreEqual(2, cook.Down);

            // Tek vardiyalık yatış (yatış 2, ustalık indirimi 1) ağır yaralı sayılmaz; ölen kişi de
            var light = For("agir_yarali");
            light.boardInjuryTurns = 2;
            sim = New(light);
            Raid(sim, false, 1, 0, 0);
            CloseTo(sim, "-");
            sim = New(For("agir_yarali"));
            Raid(sim, false, 2, 0, 0);
            CloseTo(sim, "-");
        }

        // ---------------------------------------------------------------- kavga

        [Test]
        public void Feud_SeparateOrStayOut()
        {
            // Tetik: başarısız ya da ölümlü baskın; kayıpsız başarı kavga çıkarmaz
            var sim = New(For("kavga"));
            Raid(sim, true, 0, 0, 0);
            CloseTo(sim, "-");
            sim = New(For("kavga"));
            var roles = PlanningBoardData.FieldJobs.Select(r => sim.Board.People.First(p => p.Job == r)).ToList();
            Raid(sim, true, 2, 0, 0);
            var e = CloseTo(sim, "kavga");
            Assert.AreEqual(roles[1].Id, e.PersonId, "kadro sırasıyla ilk iki sağ kişi");
            Assert.AreEqual(roles[2].Id, e.OtherId);

            sim = Open("kavga");
            e = sim.PendingShelterEvent;
            string a = e.PersonId, other = e.OtherId;
            Assert.AreEqual(Loc.F("{0} ile {1} baskın dönüşü birbirine girdi.", sim.Board.Person(a).Name, sim.Board.Person(other).Name), sim.ShelterEventBody(e));

            // Ayır: iki vardiya aynı baskına giremezler
            Decide(sim, "ayir");
            var feud = sim.ShelterEvents.Feuds.Single();
            Assert.AreEqual(a, feud.A);
            Assert.AreEqual(other, feud.B);
            Assert.AreEqual(sim.S.Day + 2, feud.Until);
            string why = Loc.F("{0} ile {1} ayrı tutuluyor; aynı baskına giremezler.", sim.Board.Person(a).Name, sim.Board.Person(other).Name);
            Assert.AreEqual(why, sim.PlanningRaidCheck());
            Assert.AreEqual(why, sim.StartLiberationRaid("weu_1").Msg);
            CloseTo(sim, "-");
            Assert.AreEqual(why, sim.PlanningRaidCheck(), "ikinci vardiya");
            // Biri kadrodan çıkarsa baskın açılır
            string role = sim.Board.Person(other).Job;
            var spare = sim.Board.People.First(p => p.Job == "rest" || p.Job == "watch" || p.Job == "gather" || p.Job == "care");
            Assert.IsTrue(sim.AssignResident(spare.Id, role).Ok);
            FillSquad(sim);
            Assert.IsNull(sim.PlanningRaidCheck());
            Assert.IsTrue(sim.AssignResident(other, role).Ok);
            CloseTo(sim, "-");
            Assert.AreEqual(0, sim.ShelterEvents.Feuds.Count, "süre dolunca kayıt düşer");
            Assert.IsNull(sim.PlanningRaidCheck());

            // Karışma, olasılık 1: ikisinden biri 1 can kaybeder ve bir vardiya yatar
            sim = Chance("kavga", 1);
            e = sim.PendingShelterEvent;
            var pair = new[] { sim.Board.Person(e.PersonId), sim.Board.Person(e.OtherId) };
            var hp = pair.Select(p => p.Hp).ToArray();
            Decide(sim, "karisma");
            Assert.AreEqual(0, sim.ShelterEvents.Feuds.Count);
            CloseTo(sim, "-");
            var hurt = Log(sim, "vadeli").Single();
            Assert.AreEqual("kavga_yara", hurt.Option);
            int k = hurt.PersonId == pair[0].Id ? 0 : 1;
            Assert.AreEqual(pair[k].Id, hurt.PersonId);
            Assert.AreEqual(Math.Max(1, hp[k] - 1), pair[k].Hp);
            Assert.AreEqual(1, pair[k].Down);
            Assert.AreEqual("rest", pair[k].Job);
            Assert.AreEqual(hp[1 - k], pair[1 - k].Hp);
            Assert.AreEqual(0, pair[1 - k].Down);
            Assert.IsTrue(pair.All(p => p.Alive), "hiçbir olayda kişi doğrudan ölmez");

            // Karışma, olasılık 0: bir şey olmaz
            sim = Chance("kavga", 0);
            e = sim.PendingShelterEvent;
            Decide(sim, "karisma");
            CloseTo(sim, "-");
            Assert.AreEqual(0, Log(sim, "vadeli").Count);
            Assert.AreEqual(0, sim.Board.Person(e.PersonId).Down + sim.Board.Person(e.OtherId).Down);
        }

        // ---------------------------------------------------------------- anma

        [Test]
        public void Memorial_HoldOrCarryOn()
        {
            var sim = Open("anma");
            var e = sim.PendingShelterEvent;
            var dead = sim.Board.Person(e.PersonId);
            Assert.IsFalse(dead.Alive);
            Assert.AreEqual(Loc.F("{0} baskında kaybedildi. Sığınak onu anmak istiyor.", dead.Name), sim.ShelterEventBody(e));

            // Anma yap: 2 erzak, güven +1
            double supplies = sim.Shelter.Supplies;
            int trust = sim.S.Trust;
            Decide(sim, "an");
            Assert.AreEqual(supplies - 2, sim.Shelter.Supplies);
            Assert.AreEqual(trust + 1, sim.S.Trust);

            sim = Open("anma");
            sim.Shelter.Supplies = 1;
            Assert.AreEqual(Loc.F("{0:0.##} erzak gerekiyor.", 2.0), Option(sim, "an").ClosedReason);
            Assert.IsTrue(Option(sim, "devam").Open);

            // İşe devam, olasılık 1: sağ kadro arkadaşlarından biri bir vardiya görev alamaz
            sim = Chance("anma", 1);
            var mates = sim.S.RaidLog.Last().LiberationReward.Squad.Where(x => !x.Dead).Select(x => x.PersonId).ToList();
            Assert.AreEqual(2, mates.Count);
            Decide(sim, "devam");
            CloseTo(sim, "-");
            var grief = Log(sim, "vadeli").Single();
            Assert.AreEqual("yas", grief.Option);
            CollectionAssert.Contains(mates, grief.PersonId);
            var mourner = sim.Board.Person(grief.PersonId);
            Assert.AreEqual("rest", mourner.Job);
            Assert.AreEqual(sim.S.Day + 1, sim.Admission.File(mourner.Id).IdleUntil);
            Assert.AreEqual(Loc.T("Bu sakin bu vardiya görev alamaz."), sim.AssignResident(mourner.Id, "tek").Msg);
            CloseTo(sim, "-");
            Assert.IsTrue(sim.AssignResident(mourner.Id, "cook").Ok || mourner.Job == "cook");

            // Olasılık 0: kimse etkilenmez
            sim = Chance("anma", 0);
            Decide(sim, "devam");
            CloseTo(sim, "-");
            Assert.AreEqual(0, Log(sim, "vadeli").Count);
            Assert.IsTrue(sim.Admission.Files.All(f => f.IdleUntil < 0));

            // Ölümsüz baskın anma açmaz
            sim = New(For("anma"));
            Raid(sim, false, 1, 0, 0);
            CloseTo(sim, "-");
        }

        // ---------------------------------------------------------------- karartma

        [Test]
        public void Blackout_SilencesWorkshopAndKitchen_ForOneClosing()
        {
            // Pencere: oyuncunun göreceği alarm eşiğin 2 altında ve eşiğin altında
            var b = On("karartma");
            b.liberationAlarmPerShift = 1;
            var sim = New(b);
            double threshold = b.liberationAlarmThreshold;
            sim.S.Alarm = threshold - 4;
            CloseTo(sim, "-");
            Assert.AreEqual(threshold - 3, sim.S.Alarm);
            var e = CloseTo(sim, "karartma");
            Assert.AreEqual(threshold - 2, sim.S.Alarm, "kâğıt açıldığında alarm eşiğin 2 altında");
            Assert.AreEqual(Loc.T("YZ alarmı eşiğe yaklaştı. Makineler susturulabilir."), sim.ShelterEventBody(e));

            // Çalışmaya devam: değişiklik yok
            Decide(sim, "devam");
            Assert.AreEqual(threshold - 2, sim.S.Alarm);
            Assert.IsFalse(sim.BlackoutActive);

            // Karart: alarm −2; bu kapanış Üretim ve Mutfak çalışmaz
            sim = Open("karartma");
            double alarm = sim.S.Alarm;
            var cook = sim.Board.People.First(p => p.Job == "cook");
            var crafter = sim.Board.People.First(p => p.Job == "craft");
            int cookXp = cook.Skill("cook"), craftXp = crafter.Skill("craft");
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            foreach (var input in sim.BoardRecipes.First(r => r.Id == "armor").Inputs) Assert.IsTrue(sim.ReserveProductionMaterial(input.Kind, input.Quantity).Ok);
            double saved = sim.BoardConsumption;
            Decide(sim, "karart");
            Assert.AreEqual(alarm - 2, sim.S.Alarm);
            Assert.AreEqual(sim.S.Day + 1, sim.Shelter.BlackoutShift);
            Assert.IsTrue(sim.BlackoutActive);
            double raw = sim.BoardConsumption;
            Assert.AreEqual(8 * sim.B.boardSuppliesPerResident, raw, 1e-12, "karartmada aşçı tasarrufu yok");
            Assert.Greater(raw, saved);
            double preview = sim.BoardFoodPreview, supplies = sim.Shelter.Supplies;
            Assert.AreEqual(-raw, preview, 1e-12, "fiş ön izlemesi karartmayı sayar");
            int armor = sim.Board.Equipment.Count(x => x.Kind == "armor");
            sim.S.Alarm = 0;
            CloseTo(sim, "-");
            Assert.AreEqual(supplies + preview, sim.Shelter.Supplies, 1e-12);
            Assert.AreEqual(armor, sim.Board.Equipment.Count(x => x.Kind == "armor"), "üretim durdu");
            Assert.AreEqual(1, sim.Board.Orders.Count);
            Assert.IsFalse(sim.Board.Orders[0].Started);
            Assert.AreEqual(cookXp, cook.Skill("cook"), "aşçı deneyim almaz");
            Assert.AreEqual(craftXp, crafter.Skill("craft"));
            Assert.IsFalse(sim.BlackoutActive);

            // Sonraki kapanış olağan: üretim tamamlanır, mutfak çalışır
            Assert.AreEqual(saved, sim.BoardConsumption, 1e-12);
            CloseTo(sim, "-");
            Assert.AreEqual(armor + 1, sim.Board.Equipment.Count(x => x.Kind == "armor"));
            Assert.AreEqual(cookXp + sim.B.boardWorkGain, cook.Skill("cook"));
        }

        // ---------------------------------------------------------------- takas

        [Test]
        public void Trade_MetalForRations_RationsForCircuits()
        {
            // Erzak 8'in altında ve en az 2 serbest metal
            var sim = Open("takas");
            var e = sim.PendingShelterEvent;
            Assert.IsNull(e.PersonId);
            int metal = sim.BoardMaterialFree("metal"), circuits = sim.BoardMaterialFree("devre");
            Assert.AreEqual(1, sim.Shelter.Supplies, 1e-9);
            Assert.AreEqual(Loc.F("{0:0.##} erzak gerekiyor.", 4.0), Option(sim, "erzak").ClosedReason, "erzak yetmiyor");
            Decide(sim, "metal");
            Assert.AreEqual(metal - 2, sim.BoardMaterialFree("metal"));
            Assert.AreEqual(6, sim.Shelter.Supplies, 1e-9);
            Assert.AreEqual(sim.Board.Materials.Sum(m => m.Quantity), sim.Shelter.Materials);

            sim = Open("takas");
            sim.Shelter.Supplies = 7;
            sim.Board.Material("metal").Quantity = 1;
            Assert.AreEqual(Loc.F("{0} serbest metal gerekiyor.", 2), Option(sim, "metal").ClosedReason);
            Assert.IsFalse(sim.DecideShelterEvent("metal").Ok);
            Decide(sim, "erzak");
            Assert.AreEqual(3, sim.Shelter.Supplies, 1e-9);
            Assert.AreEqual(circuits + 1, sim.BoardMaterialFree("devre"));
            Assert.AreEqual(1, sim.BoardMaterialFree("metal"));

            sim = Open("takas");
            double supplies = sim.Shelter.Supplies;
            Decide(sim, "reddet");
            Assert.AreEqual(supplies, sim.Shelter.Supplies);
            Assert.AreEqual(metal, sim.BoardMaterialFree("metal"));

            // Erzak tavanı aşılmaz
            sim = Open("takas", b => b.boardSuppliesCapacity = 4);
            Decide(sim, "metal");
            Assert.AreEqual(4, sim.Shelter.Supplies, 1e-9);

            // İkinci tetik: bir saha kiti eksik ve erzak 12'nin üstünde
            sim = New(For("takas"));
            CloseTo(sim, "-");
            sim.Board.Equipment.RemoveAll(x => x.Kind == "kit_ana");
            Assert.IsTrue(sim.BoardKitMissing("kit_ana"));
            sim.Shelter.Supplies = 12 + sim.BoardConsumption;
            CloseTo(sim, "-");
            sim.Shelter.Supplies = 30;
            CloseTo(sim, "takas");

            // Erzak azken metal yoksa tetiklenmez
            sim = New(For("takas"));
            sim.Board.Material("metal").Quantity = 1;
            sim.Shelter.Supplies = sim.BoardConsumption + 1;
            CloseTo(sim, "-");
        }

        // ---------------------------------------------------------------- çağrı

        [Test]
        public void Call_SendRationsOrNot_DelayedTrustNeverBelowOne()
        {
            var sim = Open("cagri");
            double supplies = sim.Shelter.Supplies;
            int trust = sim.S.Trust;
            Decide(sim, "gonder");
            Assert.AreEqual(supplies - 4, sim.Shelter.Supplies);
            Assert.AreEqual(trust + 2, sim.S.Trust);

            // Güven tavanı aşılmaz
            sim = Open("cagri");
            sim.S.Trust = sim.TrustMax;
            Decide(sim, "gonder");
            Assert.AreEqual(sim.TrustMax, sim.S.Trust);

            sim = Open("cagri");
            sim.Shelter.Supplies = 3;
            Assert.AreEqual(Loc.F("{0:0.##} erzak gerekiyor.", 4.0), Option(sim, "gonder").ClosedReason);
            Assert.IsTrue(Option(sim, "gonderme").Open);

            // Yollama, olasılık 1: sonraki kapanışta güven −1
            sim = Chance("cagri", 1);
            trust = sim.S.Trust;
            Decide(sim, "gonderme");
            Assert.AreEqual(trust, sim.S.Trust, "kayıp karar anında değil, kapanışta");
            CloseTo(sim, "-");
            Assert.AreEqual(trust - 1, sim.S.Trust);
            Assert.AreEqual("cagri_guven", Log(sim, "vadeli").Single().Option);
            CloseTo(sim, "-");
            Assert.AreEqual(trust - 1, sim.S.Trust);

            // Güven 1 iken gecikmeli kayıp uygulanmaz: oyun bitmez
            sim = Chance("cagri", 1);
            sim.S.Trust = 1;
            Decide(sim, "gonderme");
            CloseTo(sim, "-");
            Assert.AreEqual(1, sim.S.Trust);
            Assert.IsNull(sim.S.Ending);
            Assert.AreEqual(0, Log(sim, "vadeli").Count);

            // Olasılık 0
            sim = Chance("cagri", 0);
            trust = sim.S.Trust;
            Decide(sim, "gonderme");
            CloseTo(sim, "-");
            Assert.AreEqual(trust, sim.S.Trust);

            // Başarısız baskın ya da az erzak çağrı getirmez
            sim = New(For("cagri"));
            Raid(sim, false, 0, 0, 0);
            CloseTo(sim, "-");
            sim.Shelter.Supplies = 12 + sim.BoardConsumption - sim.B.boardLootSupplies - 0.01;
            Raid(sim, true, 0, 0, 0);
            CloseTo(sim, "-");
            Assert.LessOrEqual(sim.Shelter.Supplies, 12);
        }

        // ---------------------------------------------------------------- konvoy

        [Test]
        public void Convoy_AcceptOrDecline_ClosedNearTheAlarmThreshold()
        {
            // Güven 15'in altında açılmaz
            var sim = New(For("konvoy"));
            sim.S.Trust = 14;
            CloseTo(sim, "-");
            sim.S.Trust = 15;
            var e = CloseTo(sim, "konvoy");
            Assert.IsNull(e.PersonId);

            double supplies = sim.Shelter.Supplies, alarm = sim.S.Alarm;
            int exposure = sim.Shelter.Exposure;
            Decide(sim, "kabul");
            Assert.AreEqual(supplies + 5, sim.Shelter.Supplies);
            Assert.AreEqual(alarm + 2, sim.S.Alarm);
            Assert.AreEqual(exposure, sim.Shelter.Exposure, "konvoy ifşaya dokunmaz");

            sim = Open("konvoy");
            supplies = sim.Shelter.Supplies;
            Decide(sim, "reddet");
            Assert.AreEqual(supplies, sim.Shelter.Supplies);
            Assert.AreEqual(0, sim.S.Alarm);

            // Erzak tavanı
            sim = Open("konvoy");
            sim.Shelter.Supplies = sim.B.boardSuppliesCapacity - 1;
            Decide(sim, "kabul");
            Assert.AreEqual(sim.B.boardSuppliesCapacity, sim.Shelter.Supplies);

            // Alarm + 2 eşiğe ulaşacaksa kabul kapalı: karar anında karşı saldırı üretilmez
            sim = Open("konvoy");
            sim.S.Alarm = sim.B.liberationAlarmThreshold - 2;
            Assert.AreEqual(Loc.T("Alarm eşiğe çok yakın."), Option(sim, "kabul").ClosedReason);
            Assert.AreEqual(Loc.T("Alarm eşiğe çok yakın."), sim.DecideShelterEvent("kabul").Msg);
            sim.S.Alarm = sim.B.liberationAlarmThreshold - 2.5;
            Assert.IsTrue(Option(sim, "kabul").Open);
            Decide(sim, "kabul");
            Assert.AreEqual(0, sim.S.Counterattacks);
        }

        // ---------------------------------------------------------------- açlık ve ayrılma

        static Balance Hungry(int streak = 3)
        {
            var b = Quiet();
            b.desertStreak = streak;
            return b;
        }

        [Test]
        public void Hunger_StreakUpWhenStarving_DownByOneWhenFed()
        {
            foreach (bool enabled in new[] { true, false })
            {
                var b = Quiet();
                b.shelterEventsEnabled = enabled;
                var sim = New(b);
                var seen = new List<int>();
                foreach (bool starve in new[] { true, true, true, false, true, false, false, false, false, true })
                {
                    sim.Shelter.Supplies = starve ? 0 : 50;
                    Close(sim);
                    Assert.AreEqual(starve, sim.Shelter.Starving);
                    seen.Add(sim.Shelter.HungryStreak);
                }
                CollectionAssert.AreEqual(new[] { 1, 2, 3, 2, 3, 2, 1, 0, 0, 1 }, seen, "olaylar " + (enabled ? "açık" : "kapalı"));
                Assert.AreEqual(8, sim.Board.People.Count(p => p.Alive), "ayrılma kapalı");
            }

            // Stok tam yetiyorsa kapanış aç sayılmaz
            var exact = New(Quiet());
            exact.Shelter.Supplies = exact.BoardConsumption;
            Close(exact);
            Assert.IsFalse(exact.Shelter.Starving);
            Assert.AreEqual(0, exact.Shelter.HungryStreak);
            Assert.AreEqual(0, exact.Shelter.Supplies, 1e-12);
        }

        [Test]
        public void Desertion_StartsAtTheStreak_OnePerHungryClosing_SlipWarns()
        {
            var sim = New(Hungry());
            Assert.IsNull(sim.NextDeserter);
            Assert.IsFalse(sim.BoardReceiptPreview.Contains("ERZAK"));
            sim.Shelter.Supplies = 0;
            CloseTo(sim, "-");
            StringAssert.Contains(Loc.F("ERZAK · aç vardiya: {0}.", 1), sim.BoardReceiptPreview);
            Assert.IsNull(sim.NextDeserter, "sayaç 1: bu kapanış aç geçse de kimse ayrılmaz");
            sim.Shelter.Supplies = 0;
            CloseTo(sim, "-");
            Assert.AreEqual(8, sim.Board.People.Count(p => p.Alive));

            // Sayaç 2: bu kapanış da aç geçerse ayrılacak kişi fişte yazar
            var next = sim.NextDeserter;
            Assert.IsNotNull(next);
            Assert.AreEqual("sakin-8", next.Id, "kitsiz kurucular ters sırayla");
            StringAssert.Contains(Loc.F("ERZAK · aç vardiya: {0}.", 2), sim.BoardReceiptPreview);
            StringAssert.Contains(Loc.F("ERZAK · açlık sürerse {0} ayrılır.", next.Name), sim.BoardReceiptPreview);
            uint rng = sim.S.Rng.State;
            sim.Shelter.Supplies = 0;
            CloseTo(sim, "-");
            Assert.AreEqual(rng, sim.S.Rng.State, "ayrılma çekim yapmaz");
            Assert.IsNull(sim.Board.Person("sakin-8"));
            var f = sim.Admission.File("sakin-8");
            Assert.IsTrue(f.Departed);
            Assert.AreEqual("aclik", f.DepartReason);
            Assert.AreEqual(sim.S.Day, f.DepartShift);
            Assert.AreEqual(1, sim.Shelter.Deserted);
            Assert.AreEqual("sakin-8", Log(sim, "ayrilma").Single().PersonId);
            Assert.AreEqual(7, sim.Shelter.Population);

            // Her aç kapanışta bir kişi daha
            sim.Shelter.Supplies = 0;
            Assert.AreEqual("sakin-7", sim.NextDeserter.Id);
            CloseTo(sim, "-");
            Assert.AreEqual(2, sim.Shelter.Deserted);

            // Tok kapanış ayrılmayı durdurur; sayaç birer birer iner
            sim.Shelter.Supplies = 50;
            CloseTo(sim, "-");
            Assert.AreEqual(2, sim.Shelter.Deserted);
            Assert.AreEqual(3, sim.Shelter.HungryStreak);
            Assert.IsNotNull(sim.NextDeserter, "sayaç hâlâ eşikte: bir aç kapanış yeter");
            sim.Shelter.Supplies = 50;
            CloseTo(sim, "-");
            Assert.AreEqual(2, sim.Shelter.HungryStreak);
            sim.Shelter.Supplies = 50;
            CloseTo(sim, "-");
            Assert.AreEqual(1, sim.Shelter.HungryStreak);
            Assert.IsNull(sim.NextDeserter);
            sim.Shelter.Supplies = 0;
            CloseTo(sim, "-");
            Assert.AreEqual(2, sim.Shelter.Deserted, "sayaç 2: eşiğin altında");

            // Eşik 0: ayrılma kapalı
            sim = New(Hungry(0));
            for (int i = 0; i < 6; i++) { sim.Shelter.Supplies = 0; Assert.IsNull(sim.NextDeserter); Close(sim); }
            Assert.AreEqual(0, sim.Shelter.Deserted);
        }

        /// <summary>Sıra: kurucu olmayan kitsiz en yeni kişi önce, sonra kitli yeni gelen, sonra kitsiz kurucular (ters sıra), en son kit taşıyan kurucular.</summary>
        [Test]
        public void Desertion_Order_NewcomersFirst_FoundersReversed_KitHoldersLast_NeverTheLastPerson()
        {
            var b = Humans(Hungry(1), 0, 0);
            b.admissionKnockMin = b.admissionKnockMax = 1;
            b.boardResidentCapacity = 12;
            var sim = New(b);
            string older = Admit(sim, 0), middle = Admit(sim, 0), newer = Admit(sim, 0);
            ShutDoor(sim);
            // Son katılan kişi katıldığı vardiyanın kapanışında ayrılmaz: sıra bir tok kapanıştan sonra sınanır
            sim.Shelter.Supplies = 50;
            Close(sim);
            Assert.AreEqual(0, sim.Shelter.Deserted);
            // Aday özellikleri çekimden gelir: kitleri elle kurulur (ortanca kit taşır, öbür ikisi taşımaz)
            sim.Board.Equipment.RemoveAll(x => (x.OwnerId == older || x.OwnerId == middle || x.OwnerId == newer) && x.Kind.StartsWith("kit_"));
            Assert.IsNotNull(sim.AddBoardEquipment("kit_emp", middle, true));
            // Şarjı bitmiş kit, kit sayılmaz
            var spent = sim.AddBoardEquipment("kit_tek", older, true);
            spent.Charges = 0;
            foreach (var p in sim.Board.People) p.Down = 0;
            CollectionAssert.AreEqual(new[] { "sakin-1", "sakin-2", "sakin-3" },
                sim.Board.Equipment.Where(x => x.Kind.StartsWith("kit_") && x.Charges > 0 && x.OwnerId != middle).Select(x => x.OwnerId).ToArray(), "kurucu kitleri ilk üç kişide");

            var left = new List<string>();
            for (int i = 0; i < 14; i++)
            {
                var next = sim.NextDeserter;
                sim.Shelter.Supplies = 0;
                Close(sim);
                var gone = Log(sim, "ayrilma").Select(l => l.PersonId).Skip(left.Count).ToList();
                Assert.LessOrEqual(gone.Count, 1, "kapanış başına en çok bir kişi");
                Assert.AreEqual(next?.Id, gone.FirstOrDefault(), "fişin yazdığı kişi ayrılır");
                left.AddRange(gone);
                Assert.IsNull(sim.S.Ending);
            }
            CollectionAssert.AreEqual(new[] { newer, older, middle, "sakin-8", "sakin-7", "sakin-6", "sakin-5", "sakin-4", "sakin-3", "sakin-2" }, left);
            Assert.AreEqual(1, sim.Board.People.Count(p => p.Alive), "son kişi gitmez");
            Assert.AreEqual("sakin-1", sim.Board.People.Single(p => p.Alive).Id);
            Assert.AreEqual(10, sim.Shelter.Deserted);
            Assert.IsNull(sim.NextDeserter);
            Assert.AreEqual(14, sim.Shelter.HungryStreak);
            Assert.IsFalse(sim.Board.Equipment.Any(x => x.Bound), "ayrılanın bağlı kiti onunla gider");
            Assert.AreEqual(3, sim.Board.Equipment.Count(x => x.Kind.StartsWith("kit_") && x.Charges > 0), "kurucuların bağsız kitleri depoya döner");
        }

        [Test]
        public void Desertion_SkipsBedIsolatedRestrictedAndJustAdmitted()
        {
            var b = Humans(Hungry(1), 0, 0);
            b.admissionKnockMin = b.admissionKnockMax = 1;
            var sim = New(b);
            NextGroup(sim);
            // Yatan, tecritteki ve kısıtlı kişi ayrılmaz
            sim.Board.Person("sakin-8").Down = 99;
            sim.Board.Person("sakin-7").Isolated = true;
            sim.Board.Person("sakin-7").Job = "rest";
            sim.Admission.File("sakin-7").IsolatedUntil = 999;
            sim.Board.Person("sakin-6").Restricted = true;
            sim.Board.Person("sakin-6").Job = "rest";
            sim.Admission.File("sakin-6").RestrictedUntil = 999;
            // Bu vardiya katılan kişi bu kapanışta ayrılmaz; sonraki kapanışta sıranın başındadır
            string fresh = sim.Admission.Group[0].Resident.Id;
            Assert.IsTrue(sim.ChooseCandidate(fresh).Ok);
            sim.Board.Person(fresh).Down = 0;
            ShutDoor(sim);
            Assert.AreEqual("sakin-5", sim.NextDeserter.Id);
            var left = new List<string>();
            for (int i = 0; i < 9; i++)
            {
                sim.Shelter.Supplies = 0;
                Close(sim);
            }
            left.AddRange(Log(sim, "ayrilma").Select(l => l.PersonId));
            CollectionAssert.AreEqual(new[] { "sakin-5", fresh, "sakin-4", "sakin-3", "sakin-2", "sakin-1" }, left);
            CollectionAssert.AreEquivalent(new[] { "sakin-6", "sakin-7", "sakin-8" }, sim.Board.People.Where(p => p.Alive).Select(p => p.Id).ToArray());
            Assert.IsNull(sim.NextDeserter);
            Assert.IsNull(sim.S.Ending);
        }

        /// <summary>
        /// Kısıt, süresinin son kapanışı bittikten sonra kalkar: kısıtı tam bu kapanışta biten kişi bu kapanışta ayrılmaz. Fişin
        /// "ayrılır" satırı ile kapanışta ayrılan aynı kişidir (eskiden kısıt kapanışın başında kalkıyor, sıradaki kitsiz kişi
        /// fişte yazmadığı hâlde ayrılıyordu).
        /// </summary>
        [Test]
        public void Desertion_RestrictionEndingThisClosing_StillProtects_SlipNamesTheOneWhoLeaves()
        {
            var sim = New(Hungry(1));
            // Katıldığı vardiyanın kapanışında kimse ayrılmaz: bir tok kapanış
            sim.Shelter.Supplies = 50;
            CloseTo(sim, "-");
            var f8 = sim.Admission.File("sakin-8");
            Assert.AreEqual("sakin-8", sim.NextDeserter?.Id, "test düzeni: kısıtsız sıranın başı");
            // Kısıt bu kapanışta biter (RestrictedUntil = kapanış günü)
            f8.Resident.Restricted = true;
            f8.Resident.Job = "rest";
            f8.RestrictedUntil = sim.S.Day + 1;
            var next = sim.NextDeserter;
            Assert.AreEqual("sakin-7", next.Id);
            StringAssert.Contains(Loc.F("ERZAK · açlık sürerse {0} ayrılır.", next.Name), sim.BoardReceiptPreview);
            sim.Shelter.Supplies = 0;
            CloseTo(sim, "-");
            Assert.AreEqual("sakin-7", Log(sim, "ayrilma").Single().PersonId, "fişin yazdığı kişi ayrılır");
            Assert.IsNotNull(sim.Board.Person("sakin-8"));
            Assert.IsFalse(f8.Resident.Restricted, "kısıt kapanışın sonunda kalktı");
            Assert.AreEqual(-1, f8.RestrictedUntil);
            // Sonraki aç kapanışta sıra yine onundur
            Assert.AreEqual("sakin-8", sim.NextDeserter.Id);
            sim.Shelter.Supplies = 0;
            CloseTo(sim, "-");
            Assert.AreEqual("sakin-8", Log(sim, "ayrilma").Last().PersonId);
        }

        /// <summary>Ayrılma pano kapanışının içinde, kapı arkasındadır: canlı sayısı 3'ün altına inen kapanışta acil grup gelir.</summary>
        [Test]
        public void Desertion_BelowThree_EmergencyGroupArrivesAtTheSameClosing()
        {
            var b = Hungry(1);
            b.boardStartResidents = 3;
            b.candidateEmergencyBelow = 3;
            // Ayrılma sırasını sınar: terk (eksik kadrolu üç vardiya) kapalı
            b.abandonUnderstaffedShifts = 0;
            var sim = New(b);
            // Kurucular kuruluş vardiyasının kapanışında "yeni katılmış" sayılır: ayrılma en erken ikinci kapanışta
            sim.Shelter.Supplies = 0;
            Close(sim);
            Assert.AreEqual(3, sim.Board.People.Count(p => p.Alive));
            Assert.IsNull(Log(sim, "ayrilma").FirstOrDefault());
            sim.Shelter.Supplies = 0;
            uint rng = sim.S.Rng.State;
            Close(sim);
            Assert.AreEqual(2, sim.Board.People.Count(p => p.Alive));
            Assert.AreEqual(1, sim.Shelter.Deserted);
            Assert.AreEqual(32, Draws(rng, sim.S.Rng.State), "ayrılma çekimsiz, acil grup 32 çekim");
            Assert.IsTrue(sim.Admission.GroupEmergency);
            Assert.AreEqual(3, sim.Admission.Group.Count);
            Assert.AreEqual(sim.S.Day, sim.Admission.GroupArrivalShift);
            Assert.IsTrue(sim.Admission.Group.All(c => c.Perks.Contains("kit")));

            // Acil adayın bağlı kiti onu ayrılma sırasında "kit taşıyan" yapar: kitsiz kurucu ondan önce gider
            string id = sim.Admission.Group[0].Resident.Id;
            Assert.IsTrue(sim.ChooseCandidate(id).Ok);
            sim.Board.Person(id).Down = 0;
            sim.Shelter.Supplies = 0;
            Close(sim);
            Assert.AreEqual(2, sim.Shelter.Deserted);
            sim.Shelter.Supplies = 0;
            Close(sim);
            Assert.AreEqual(3, sim.Shelter.Deserted);
            Assert.AreEqual(id, Log(sim, "ayrilma").Last().PersonId, "yeni gelen kitli kişi kitli kurucudan önce gider");
            Assert.AreEqual(1, sim.Board.People.Count(p => p.Alive));
            sim.Shelter.Supplies = 0;
            Close(sim);
            Assert.AreEqual(1, sim.Board.People.Count(p => p.Alive), "son kişi gitmez");
            Assert.IsNull(sim.S.Ending);
        }

        /// <summary>Fiş ön izlemesi kapanışla aynı sayıyı yazar: obur, erzak taksiti, yarım tayın ve karartma birlikte.</summary>
        [Test]
        public void FoodPreview_EqualsClose_WithHalfRationAndBlackoutTogether()
        {
            var b = On("erzak_azaliyor", "karartma");
            var sim = New(b);
            sim.Shelter.Supplies = sim.BoardConsumption * 2;
            CloseTo(sim, "erzak_azaliyor");
            Decide(sim, "yarim");
            sim.Shelter.Supplies = 20;
            sim.S.Alarm = sim.B.liberationAlarmThreshold - 1;
            CloseTo(sim, "karartma");
            Decide(sim, "karart");
            Assert.IsTrue(sim.HalfRationActive && sim.BlackoutActive);
            double raw = 8 * sim.B.boardSuppliesPerResident;
            Assert.AreEqual(raw, sim.BoardConsumption, 1e-12);
            double preview = sim.BoardFoodPreview, supplies = sim.Shelter.Supplies;
            Assert.AreEqual(-raw * 0.5, preview, 1e-12);
            StringAssert.Contains(Loc.F("Beklenen erzak değişimi: {0:0.##}", preview), sim.BoardReceiptPreview);
            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.AreEqual(preview, sim.BoardFoodPreview, 1e-12, "fiş basıldıktan sonra da aynı");
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            Assert.AreEqual(supplies + preview, sim.Shelter.Supplies, 1e-12);
        }

        // ---------------------------------------------------------------- kısıt ve D3

        [Test]
        public void Restricted_OnlyTrainingAndIdle_NoWatcher_Several_ExpiresAlone()
        {
            var b = For("hirsizlik");
            var sim = New(b);
            Arrange(sim, "hirsizlik");
            var e = CloseTo(sim, "hirsizlik");
            string first = e.PersonId;
            var person = sim.Board.Person(first);
            var f = sim.Admission.File(first);
            if (person.Job == "rest") Assert.IsTrue(sim.AssignResident(first, "watch").Ok);
            // Bağsız zırh depoya döner, bağlı zırh kişide kalır
            sim.Board.Equipment.RemoveAll(x => x.OwnerId == first);
            var loose = sim.AddBoardEquipment("medkit", first);
            var bound = sim.AddBoardEquipment("armor", first, true);
            Decide(sim, "kisitla");
            Assert.IsTrue(person.Restricted);
            Assert.AreEqual("rest", person.Job, "eğitimde olmayan kısıtlı kişi Boşta'ya iner");
            Assert.AreEqual(sim.S.Day + 3, f.RestrictedUntil);
            Assert.IsNull(loose.OwnerId);
            Assert.AreEqual(first, bound.OwnerId);

            // Yalnız Eğitim ve Boşta açık; gözetmen aranmaz
            foreach (string job in new[] { "tek", "emp", "ana", "craft", "cook", "care", "gather", "watch", "contact" })
                Assert.AreEqual(Loc.T("Kısıtlı sakin bu göreve erişemez."), sim.AssignResident(first, job).Msg, job);
            Assert.AreEqual(Loc.T("Bu sakin teçhizat kullanamıyor."), sim.EquipResident(loose.Id, first).Msg);
            string subject = PlanningBoardData.Subjects.First(s => person.Skill(s) == 0);
            var trained = sim.TrainResident(first, subject);
            Assert.IsTrue(trained.Ok, trained.Msg);
            Assert.AreEqual("train", person.Job);
            Assert.IsNull(sim.BoardPlanCheck());

            // İkinci kişi de kısıtlanabilir (tek gözetim yeri yok)
            Arrange(sim, "hirsizlik");
            e = CloseTo(sim, "hirsizlik");
            string second = e.PersonId;
            Assert.AreNotEqual(first, second, "kısıtlı kişi olay hedefi olmaz");
            Assert.AreEqual(sim.B.boardTrainingGain, person.Skill(subject), "kısıtlı kişi eğitim aldı");
            Decide(sim, "kisitla");
            Assert.AreEqual(2, sim.Board.People.Count(p => p.Restricted));
            Assert.AreEqual(sim.S.Day + 3, sim.Admission.File(second).RestrictedUntil);

            // Süre dolan kapanışta kendiliğinden kalkar
            sim.Shelter.Supplies = 50;
            CloseTo(sim, "-");
            Assert.IsTrue(person.Restricted);
            sim.Shelter.Supplies = 50;
            CloseTo(sim, "-");
            Assert.IsFalse(person.Restricted, "birinci kişinin üç vardiyası doldu");
            Assert.AreEqual(-1, f.RestrictedUntil);
            Assert.IsTrue(sim.Board.Person(second).Restricted);
            Assert.IsTrue(sim.AssignResident(first, "watch").Ok);
            sim.Shelter.Supplies = 50;
            CloseTo(sim, "-");
            Assert.IsFalse(sim.Board.Person(second).Restricted);
        }

        /// <summary>
        /// Kısıt kilitli planda da kararla hemen uygulanır: baskın dönüşü kısıtlanan kadro üyesi o an Boşta'ya iner, bağsız teçhizatı
        /// depoya döner, fiş basılır ve kişi bir sonraki baskına çıkamaz. (İkna farklıdır: kilitli planda görev değişmez, bedel
        /// sonraki vardiyaya kayar; bkz. <see cref="Deserter_PersuadeLetGoOrLock"/>.)
        /// </summary>
        [Test]
        public void Restricted_DecisionInALockedPlan_TakesEffectAtOnce()
        {
            var sim = Open("hirsizlik");
            var e = sim.PendingShelterEvent;
            var person = sim.Board.Person(e.PersonId);
            if (person.Job != "tek") Assert.IsTrue(sim.AssignResident(person.Id, "tek").Ok);
            Raid(sim, true, 0, 0, 0);
            Assert.IsTrue(sim.Board.Locked, "baskın emri planı kilitledi");
            Assert.AreSame(e, sim.PendingShelterEvent);
            Assert.AreEqual("tek", person.Job);
            Decide(sim, "kisitla");
            Assert.IsTrue(person.Restricted);
            Assert.AreEqual("rest", person.Job, "kilitli planda da hemen");
            Assert.IsFalse(sim.Board.Equipment.Any(x => x.OwnerId == person.Id && !x.Bound), "bağsız teçhizat depoya döndü");
            Assert.IsTrue(sim.PrintShift().Ok);
            StringAssert.Contains(Loc.F("{0}: {1}", person.Name, PlanningBoardData.JobName("rest")), sim.Board.ReceiptText);
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            Assert.AreEqual("rest", person.Job);
            Assert.IsTrue(person.Restricted);
            Assert.IsFalse(sim.AssignResident(person.Id, "tek").Ok, "kısıtlı kişi sahaya konamaz");
        }

        /// <summary>
        /// Tecrit bulguyla kişi çıkarabilir: son canlı kişide kapalıdır (kişi çıkaran seçenek kuralı, §3 kural 9); görmezden gelmek
        /// açık kalır. Eskiden açıktı ve bulgu son kişiyi söküp oyunu "topluluk" sonuyla bitiriyordu.
        /// </summary>
        [Test]
        public void Isolation_ClosedForTheLastLivingPerson()
        {
            var sim = Open("iz", b => b.isolationFindChance = 1);
            var e = sim.PendingShelterEvent;
            Assert.IsTrue(Option(sim, "tecrit").Open, "test düzeni: başkaları varken açık");
            foreach (var p in sim.Board.People.Where(p => p.Id != e.PersonId)) { p.Alive = false; p.Hp = 0; }
            Assert.AreEqual(1, sim.Board.People.Count(p => p.Alive));
            var isolate = Option(sim, "tecrit");
            Assert.IsFalse(isolate.Open);
            Assert.AreEqual(Loc.T("Son kişi tecrit edilemez: sığınakta başka kimse yok."), isolate.ClosedReason);
            Assert.AreEqual(isolate.ClosedReason, sim.DecideShelterEvent("tecrit").Msg);
            Assert.IsFalse(Option(sim, "yolla").Open);
            Assert.IsTrue(Option(sim, "yoksay").Open, "güvenli seçenek açık");
            Decide(sim, "yoksay");
            sim.Shelter.Supplies = 50;
            Close(sim);
            Assert.IsNull(sim.S.Ending);
            Assert.AreEqual(0, Log(sim, "bulgu").Count);
        }

        /// <summary>
        /// D3: kısıtlı ya da tecritteki kişinin bağlı kiti kit kilidinde "var" sayılır (bedava kit üretilmez); bedel kâğıtta yazar.
        /// Kişi sığınaktan çıkarsa kit onunla gider ve kilit açılır.
        /// </summary>
        [Test]
        public void D3_BoundKitOfRestrictedOrIsolatedOwner_CountsAsPresent_UntilTheOwnerLeaves()
        {
            string line = Loc.F("Bu sürede {0} kullanılamaz: kendi kiti ondadır.", PlanningBoardData.EquipmentName("kit_ana"));
            Func<Simulation, string> circuits = s => string.Join(",", s.BoardRecipes.First(r => r.Id == "kit_ana").Inputs.Select(i => i.Kind + i.Quantity));

            foreach (string option in new[] { "kisitla", "tecrit", "kilitle", "sorgula" })
            {
                string kind = option == "kisitla" ? "hirsizlik" : option == "tecrit" ? "iz" : option == "kilitle" ? "kacak" : "sabotaj";
                var sim = Open(kind);
                var e = sim.PendingShelterEvent;
                // Hedef, Analist kitinin tek örneğini bağlı taşıyor
                sim.Board.Equipment.RemoveAll(x => x.Kind == "kit_ana");
                var kit = sim.AddBoardEquipment("kit_ana", e.PersonId, true);
                Assert.AreEqual(line, Option(sim, option).KitLock, option + ": bedel kâğıtta ayrı satırda yazar");
                Assert.IsFalse(Option(sim, option).Cost.Contains(line), option + ": bedel metnine eklenmez (birleştirme çevrilemez)");
                foreach (var o in sim.ShelterEventOptions().Where(o => o.Id != option && o.Id != "tecrit" && o.Id != "sorgula" && o.Id != "kisitla" && o.Id != "kilitle"))
                    Assert.IsNull(o.KitLock, o.Id);
                Decide(sim, option);
                Assert.AreEqual(e.PersonId, kit.OwnerId, "bağlı kit kişide kalır");
                Assert.IsTrue(sim.Board.Person(e.PersonId).Restricted || sim.Board.Person(e.PersonId).Isolated);
                Assert.IsFalse(sim.BoardKitMissing("kit_ana"), option + ": kit kilidinde var sayılır");
                Assert.AreEqual("metal1,devre1", circuits(sim), "tarif devre ister: bedava kit yok");
                // Kişi kadroya giremez; kit de başkasına verilemez
                Assert.AreEqual(Loc.T("Bu teçhizat sahibine bağlı; devredilemez."), sim.EquipResident(kit.Id, sim.Board.People.First(p => p.Alive && p.Id != e.PersonId).Id).Msg);
            }

            // Aynı türden ikinci şarjlı kit varsa satır yazılmaz
            var spare = Open("hirsizlik");
            spare.AddBoardEquipment("kit_ana", spare.PendingShelterEvent.PersonId, true);
            Assert.IsNull(Option(spare, "kisitla").KitLock);
            // Bağlı olmayan kit zaten depoya döner: satır yok
            var loose = Open("hirsizlik");
            loose.Board.Equipment.RemoveAll(x => x.Kind == "kit_ana");
            var free = loose.AddBoardEquipment("kit_ana", loose.PendingShelterEvent.PersonId);
            Assert.IsNull(Option(loose, "kisitla").KitLock);
            Decide(loose, "kisitla");
            Assert.IsNull(free.OwnerId);

            // Kişi çıkarsa bağlı kit onunla gider: kit eksik, tarif devresiz
            foreach (string option in new[] { "yolla", "sok", "birak" })
            {
                string kind = option == "yolla" ? "hirsizlik" : option == "sok" ? "sabotaj" : "kacak";
                var sim = Open(kind);
                sim.Board.Equipment.RemoveAll(x => x.Kind == "kit_ana");
                sim.AddBoardEquipment("kit_ana", sim.PendingShelterEvent.PersonId, true);
                Assert.IsFalse(sim.BoardKitMissing("kit_ana"));
                Decide(sim, option);
                Assert.IsTrue(sim.BoardKitMissing("kit_ana"), option);
                Assert.AreEqual("metal1", circuits(sim), option);
            }

            // Tecrit bulguyla biterse de kit gider ve kilit açılır
            var found = Open("iz", b => b.isolationFindChance = 1);
            found.Board.Equipment.RemoveAll(x => x.Kind == "kit_ana");
            found.AddBoardEquipment("kit_ana", found.PendingShelterEvent.PersonId, true);
            Decide(found, "tecrit");
            CloseTo(found, "-"); CloseTo(found, "-");
            Assert.IsFalse(found.BoardKitMissing("kit_ana"));
            CloseTo(found, "-");
            Assert.IsTrue(found.BoardKitMissing("kit_ana"));
        }
    }
}
