using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 4 devir fişinin verisi (PLAN W4; E8–E12): gerçek bir kartlı simülasyondan V.001 değerleri, saha ve sığınak
    /// satırları, durum ekleri, KAPANIŞTA BEKLENEN tahmini (gerçek kapanışla aynı), kapı ve DOSYA uyarıları. Veri kurmak RNG
    /// çekmez, komut yazmaz, Core durumunu değiştirmez, dilden bağımsızdır; gizli geleceği okumaz. Sahneye bağlı değildir.
    /// </summary>
    public sealed class Rev3HandoverSlipFactsTests
    {
        const string BuilderPath = "Assets/Corrigible/Desk/Scripts/Bridge/Siginak/HandoverSlipFactsBuilder.cs";
        const double Eps = 1e-9;

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        // ---------------------------------------------------------------- kurulum

        /// <summary>Kartlı oyun, kapıya kimse gelmez (<c>Rev3BoardSnapshotTests</c> ile aynı denge).</summary>
        static Simulation Ready(uint seed = 79) => Simulation.NewGame(seed, new Balance
        { shelterEventsEnabled = false,
            planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100
        });

        /// <summary>
        /// Kapıya <c>1..knockMax</c> vardiyada bir aday grubu gelir; <paramref name="robotChance"/> üç kademenin de sızmacı olasılığıdır
        /// (gizli; çekim sayısını değiştirmez).
        /// </summary>
        static Simulation Doors(uint seed = 79, double robotChance = 0, int knockMax = 1) =>
            Simulation.NewGame(seed, new Balance
            { shelterEventsEnabled = false,
                planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = knockMax,
                candidateTier0Robot = robotChance, candidateTier1Robot = robotChance, candidateTier2Robot = robotChance
            });

        /// <summary>Sunucunun ilk açılıştaki <c>RetireJobs</c>'u: otomatik üretim ve emekli görevlerin BOŞTA'ya alınması.</summary>
        static Simulation Retired(uint seed = 79)
        {
            var sim = Ready(seed);
            Assert.IsTrue(sim.EnableAutomaticProduction().Ok);
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                Assert.IsTrue(sim.AssignResident(p.Id, "rest").Ok);
            return sim;
        }

        static void Close(Simulation sim)
        {
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }

        static ResidentState At(Simulation sim, int index) => sim.Board.People[index];

        static HandoverSlipFacts Build(Simulation sim) => HandoverSlipFactsBuilder.Build(sim);

        /// <summary>EVRAK arşivinin biçimi (W5 aynı yolla yazar): null dizgi "" olur.</summary>
        static string Json(HandoverSlipFacts f) => UnityEngine.JsonUtility.ToJson(f);

        /// <summary>Verinin bütün public alanları, iç içe (null ile "" ayrılır; ondalıklar tam): eşitlik karşılaştırması için.</summary>
        static string Dump(object value)
        {
            var sb = new StringBuilder();
            Dump(value, sb);
            return sb.ToString();
        }

        static void Dump(object value, StringBuilder sb)
        {
            switch (value)
            {
                case null: sb.Append("null"); return;
                case string s: sb.Append('"').Append(s).Append('"'); return;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); return;
                case IList list:
                    sb.Append('[');
                    foreach (var item in list) { Dump(item, sb); sb.Append(','); }
                    sb.Append(']');
                    return;
            }
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum) { sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); return; }
            sb.Append(type.Name).Append('{');
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(x => x.MetadataToken))
            {
                sb.Append(field.Name).Append('=');
                Dump(field.GetValue(value), sb);
                sb.Append(';');
            }
            sb.Append('}');
        }

        /// <summary>
        /// E11 durumu: V.002'de ilk gruptan bir aday (bedelsiz özellikli) alınır ve kısıtlanır, V.003'te ikinci grup kapıda (geliş
        /// vardiyası: son vardiya değil); bir kişi sıfırdan eğitimde, bir kişi yaralı ama ayakta; baskın dönüşünde kadronun ilki kurtarılır
        /// (yatıyor), üçüncüsü geride kalır (ölür). Plan baskınla kilitli.
        /// </summary>
        static Simulation Mixed(out string rescued, out string lost, out string restricted)
        {
            var sim = Doors();
            Close(sim);
            Assert.Greater(sim.Admission.Group.Count, 1, "V.002: ilk aday grubu kapıda.");
            restricted = sim.Admission.Group.First(c => !c.Perks.Any(CandidateData.IsCost)).Resident.Id;
            var chosen = sim.ChooseCandidate(restricted);
            Assert.IsTrue(chosen.Ok, chosen.Msg);
            // Test düzeni: olay kısıtının durumu (eski elle kısıt aşama 7'de silindi)
            string rid = restricted;
            var r = sim.Board.Person(rid);
            r.Restricted = true;
            r.Job = "rest";
            sim.Admission.File(rid).RestrictedUntil = sim.S.Day + 3;
            foreach (var e in sim.Board.Equipment.Where(e => e.OwnerId == rid && !e.Bound)) e.OwnerId = null;
            Close(sim);
            Assert.Greater(sim.Admission.Group.Count, 1, "V.003: ikinci aday grubu kapıda.");
            Assert.AreEqual("care", At(sim, 5).Job);
            Assert.AreEqual(0, At(sim, 5).Skill("emp"));
            var trained = sim.TrainResident(At(sim, 5).Id, "emp");
            Assert.IsTrue(trained.Ok, trained.Msg);
            Assert.AreEqual("gather", At(sim, 6).Job);
            At(sim, 6).Hp = At(sim, 6).MaxHp - 1;
            var squad = sim.PlanningSquad();
            Assert.AreEqual(3, squad.Count);
            rescued = squad[0].PersonId;
            lost = squad[2].PersonId;
            var raid = sim.StartLiberationRaid("weu_1");
            Assert.IsTrue(raid.Ok, raid.Msg);
            // Tek kip: ilk kişi giriş odasında düşer ve ekipçe çıkarılır (yatar), üçüncüsü kaçışta geride kalır (ölür)
            FacilityDeskKit.ResolveRaid(sim, true, FacilityDeskKit.Rescued, FacilityDeskKit.Returns, FacilityDeskKit.LeftBehind);
            Assert.IsFalse(sim.Board.Person(lost).Alive);
            Assert.Greater(sim.Board.Person(rescued).Down, 0);
            Assert.IsTrue(sim.Board.Locked);
            return sim;
        }

        static string Fingerprint(Simulation sim)
        {
            var b = sim.Board;
            var sb = new StringBuilder();
            sb.Append(sim.S.Rng.State).Append('|').Append(sim.Record.commands.Count).Append('|').Append(sim.S.Day).Append('|')
              .Append(sim.Shelter.Supplies.ToString("R")).Append('|').Append(b.Locked).Append(b.Printed).Append(b.Filing).Append('|')
              .Append(b.ReceiptId).Append('|').Append(b.LastFiledReceiptId).Append('|').Append(b.AutomaticProduction).Append('|');
            foreach (var p in b.People)
                sb.Append(p.Id).Append(p.Job).Append(p.Hp).Append(p.Down).Append(p.Restricted).Append(p.Alive).Append(p.TrainingSubject)
                  .Append(string.Join(",", p.Experience.Select(x => x.Subject + x.Points))).Append(';');
            foreach (var e in b.Equipment) sb.Append(e.Id).Append(e.Kind).Append(e.OwnerId).Append(e.Charges).Append(e.Bound).Append(';');
            foreach (var m in b.Materials) sb.Append(m.Kind).Append(m.Quantity).Append(';');
            foreach (var m in b.ProductionInputs) sb.Append(m.Kind).Append(m.Quantity).Append(';');
            foreach (var o in b.Orders) sb.Append(o.Id).Append(o.RecipeId).Append(o.Remaining).Append(o.Started).Append(';');
            var a = sim.Admission;
            sb.Append(a.NextArrivalShift).Append('|')
              .Append(a.GroupArrivalShift).Append(a.GroupWaitingUntil).Append(a.GroupEmergency).Append(a.Passed.Count).Append('|');
            foreach (var c in a.Group) sb.Append(c.Resident.Id).Append(c.Tier).Append(c.SuspicionKey).Append(string.Join(",", c.Perks)).Append(';');
            foreach (var f in a.Files) sb.Append(f.PersonId).Append(f.Departed).Append(f.RestrictedUntil).Append(f.ClueCount).Append(';');
            return sb.ToString();
        }

        /// <summary>
        /// Fişi kurar, vardiyayı kapatır ve tahmini gerçek kapanışla karşılaştırır: ERZAK, EĞİTİM (deneyim, kademe), ÜRETİM (ilerleme,
        /// tamamlanan).
        /// </summary>
        static HandoverSlipFacts CloseAndCompare(Simulation sim, string label)
        {
            var f = Build(sim);
            var trainee = BoardDropRules.Occupant(sim.Board, "train");
            var order = sim.Board.Orders.FirstOrDefault();
            int remaining = order?.Remaining ?? 0;
            Close(sim);
            Assert.AreEqual(f.suppliesAfter, sim.Shelter.Supplies, Eps, label + ": ERZAK");
            if (f.training.active)
            {
                Assert.IsNotNull(trainee, label);
                Assert.AreEqual(f.training.xpAfter, trainee.Skill(f.training.subject), label + ": EĞİTİM deneyimi");
                Assert.AreEqual(f.training.tierAfter, sim.BoardSkillTier(trainee, f.training.subject), label + ": EĞİTİM kademesi");
            }
            else Assert.IsNull(trainee, label + ": eğitim cebi boştu");
            var completed = sim.Board.LastCompleted;
            if (f.production.kind == SlipProductionKind.Progress && f.production.after >= f.production.turns)
            {
                CollectionAssert.AreEqual(new[] { f.production.itemType }, completed, label + ": ÜRETİM tamamlandı");
                if (order != null) Assert.IsNull(sim.Board.Order(order.Id), label + ": biten iş emri kalkar");
            }
            else if (f.production.kind == SlipProductionKind.Progress)
            {
                CollectionAssert.IsEmpty(completed, label + ": ÜRETİM sürüyor");
                Assert.AreEqual(f.production.after, f.production.turns - sim.Board.Order(order.Id).Remaining, label + ": ÜRETİM ilerlemesi");
            }
            else
            {
                CollectionAssert.IsEmpty(completed, label + ": ÜRETİM çıktı yok");
                if (order != null) Assert.AreEqual(remaining, sim.Board.Order(order.Id).Remaining, label + ": iş emri ilerlemedi");
            }
            return f;
        }

        // ---------------------------------------------------------------- E8 · V.001

        [Test]
        public void Facts_NullWithoutBoard()
        {
            Assert.IsNull(HandoverSlipFactsBuilder.Build(null));
            Assert.IsNull(HandoverSlipFactsBuilder.Build(LegacySimulation.NewGame(79)), "Eski kip: kartlı pano yok, fiş yok.");
        }

        [Test]
        public void Facts_V001_Retired79()
        {
            var sim = Retired();
            var trainee = At(sim, 5);
            Assert.AreEqual("care", trainee.TrainingSubject, "Kurucu 6, kendi konusunda (Bakım) eğitimli başlar.");
            Assert.IsTrue(sim.TrainResident(trainee.Id, "care").Ok);
            var f = Build(sim);
            Assert.IsNotNull(f);

            // başlık: No. 001/1 · 8/10 kişi · erzak 24/60
            Assert.IsTrue(string.IsNullOrEmpty(f.receiptId), "Basılmadan kimlik yok.");
            Assert.AreEqual(1, f.shift);
            Assert.AreEqual(1, f.copy);
            Assert.AreEqual(8, f.alive);
            Assert.AreEqual(10, f.capacity);
            Assert.AreEqual(24, f.supplies, Eps);
            Assert.AreEqual(60, f.suppliesCapacity, Eps);

            // SAHA · BASKIN KADROSU: T kit · zırh | E kit · zırh | A kit (adlar tohumdan, koddan okunur)
            CollectionAssert.AreEqual(new[] { "tek", "emp", "ana" }, f.field.Select(r => r.role));
            CollectionAssert.AreEqual(new[] { At(sim, 0).Name, At(sim, 1).Name, At(sim, 2).Name }, f.field.Select(r => r.name));
            CollectionAssert.AreEqual(new[] { "kit_tek", "armor" }, f.field[0].items);
            CollectionAssert.AreEqual(new[] { "kit_emp", "armor" }, f.field[1].items);
            CollectionAssert.AreEqual(new[] { "kit_ana" }, f.field[2].items);

            // SIĞINAK: ÜRETİM, MUTFAK, sonra BOŞTA'lar (2026-11-24: EĞİTİM yok; eski kayıttan eğitimdeki kişi BOŞTA satırı)
            CollectionAssert.AreEqual(new[] { At(sim, 3).Name, At(sim, 4).Name, trainee.Name, At(sim, 6).Name, At(sim, 7).Name }, f.shelter.Select(r => r.name));
            CollectionAssert.AreEqual(new[] { "craft", "cook", "rest", "rest", "rest" }, f.shelter.Select(r => r.job));
            Assert.IsTrue(f.shelter.All(r => string.IsNullOrEmpty(r.subject)), "Konu satırı yok.");
            Assert.IsTrue(f.shelter.All(r => r.state == SlipPersonState.None && r.stateShifts == 0), "V.001'de durum eki yok.");

            // KAPANIŞTA BEKLENEN: ERZAK 24,0 → 21,5 / 60 −2,5 · EĞİTİM +2 (2→4) · ÜRETİM iş emri yok · çıktı yok · KAPI açık başvuru yok
            // (tasarım fişi 21,9 / −2,1 yazar: tüketim 0,35; aşama 8 dengesiyle 8 × 0,40 − 0,7 = 2,5)
            Assert.AreEqual(21.5, f.suppliesAfter, Eps);
            Assert.AreEqual(-2.5, f.suppliesAfter - f.supplies, Eps);
            Assert.AreEqual(sim.BoardFoodPreview, f.suppliesAfter - f.supplies, Eps);
            Assert.IsFalse(f.training.active, "EĞİTİM bölümü yok (2026-11-24)");
            Assert.AreEqual(SlipProductionKind.None, f.production.kind, "Otomatik üretim açık ama girdisiz: uygun tarif yok.");
            Assert.IsTrue(string.IsNullOrEmpty(f.production.itemType));
            Assert.AreEqual(0, f.openApplications);
            CollectionAssert.IsEmpty(f.warnings, "Aday yok, dosyalarda risk yok: uyarı kutusu yok.");

            // basılınca yalnız kimlik gelir
            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            Assert.AreEqual("vardiya-0", Build(sim).receiptId);
        }

        // ---------------------------------------------------------------- E9 · saflık, E10 · basılı plan

        [Test]
        public void Facts_IsPure()
        {
            var sim = Retired();
            Assert.IsTrue(sim.StageProductionMaterial("metal", 2).Ok);
            Assert.IsTrue(sim.StageProductionMaterial("kumas", 1).Ok);
            Assert.IsTrue(sim.TrainResident(At(sim, 7).Id, "craft").Ok);
            string before = Fingerprint(sim);
            uint rng = sim.S.Rng.State;
            int commands = sim.Record.commands.Count, day = sim.S.Day;

            string first = Dump(Build(sim));
            for (int i = 0; i < 20; i++) Assert.AreEqual(first, Dump(Build(sim)));
            Loc.SetLanguage(Loc.English, new Dictionary<string, string>());
            for (int i = 0; i < 20; i++) Assert.AreEqual(first, Dump(Build(sim)), "Veri dilden bağımsızdır.");
            Loc.SetLanguage(Loc.Turkish);
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.AreEqual(day, sim.S.Day);
            Assert.AreEqual(before, Fingerprint(sim));

            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            string printed = Fingerprint(sim);
            rng = sim.S.Rng.State;
            commands = sim.Record.commands.Count;
            string firstPrinted = Dump(Build(sim));
            for (int i = 0; i < 20; i++) Assert.AreEqual(firstPrinted, Dump(Build(sim)));
            Loc.SetLanguage(Loc.English, new Dictionary<string, string>());
            for (int i = 0; i < 20; i++) Assert.AreEqual(firstPrinted, Dump(Build(sim)));
            Loc.SetLanguage(Loc.Turkish);
            Assert.AreEqual(rng, sim.S.Rng.State, "Fişi okumak RNG çekmez.");
            Assert.AreEqual(commands, sim.Record.commands.Count, "Fişi okumak komut yazmaz.");
            Assert.AreEqual(day, sim.S.Day, "Fişi okumak zamanı ilerletmez.");
            Assert.IsTrue(sim.Board.Printed);
            Assert.AreEqual(printed, Fingerprint(sim));
        }

        [Test]
        public void Facts_SameBeforeAndAfterPrint()
        {
            var sim = Retired();
            Assert.IsTrue(sim.TrainResident(At(sim, 5).Id, "care").Ok);
            Assert.IsTrue(sim.StageProductionMaterial("metal", 2).Ok);
            Assert.IsTrue(sim.StageProductionMaterial("kumas", 1).Ok);
            var before = Build(sim);
            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            var after = Build(sim);
            Assert.AreEqual(sim.Board.ReceiptId, after.receiptId);
            Assert.AreEqual("vardiya-0", after.receiptId);
            after.receiptId = before.receiptId;
            Assert.AreEqual(Dump(before), Dump(after), "Basılıyken plan kilitli: fiş basılan planın aynısıdır.");

            Loc.SetLanguage(Loc.English, new Dictionary<string, string>());
            var english = Build(sim);
            english.receiptId = before.receiptId;
            Assert.AreEqual(Dump(before), Dump(english), "Dil yalnız sözcükleri değiştirir; veri aynı.");
            Loc.SetLanguage(Loc.Turkish);

            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            var next = Build(sim);
            Assert.AreEqual(2, next.shift, "Devirden sonra yeni vardiyanın fişi: No. 002/1.");
            Assert.AreEqual(1, next.copy);
            Assert.IsTrue(string.IsNullOrEmpty(next.receiptId));
            Assert.IsFalse(next.training.active, "Eğitim kapanışta biter; kişi BOŞTA'ya döner.");
        }

        [Test]
        public void Facts_JsonRoundTrip_EmptyNamesStayEmpty()
        {
            var sim = Mixed(out _, out _, out _);
            var f = Build(sim);
            var back = UnityEngine.JsonUtility.FromJson<HandoverSlipFacts>(Json(f));
            // JsonUtility 17 haneli bir double'ı en çok bir ulp kaydırarak okuyabilir (aşama 8 dengesiyle bu senaryoda
            // 20.299999999999998 → 20.3); fişte erzak bir ondalıkla yazılır. Sayılar toleransla, metin ikinci gidiş-dönüşte birebir.
            var again = UnityEngine.JsonUtility.FromJson<HandoverSlipFacts>(Json(back));
            Assert.AreEqual(Json(back), Json(again), "ikinci gidiş-dönüş birebir");
            Assert.AreEqual(f.supplies, back.supplies, Eps);
            Assert.AreEqual(f.suppliesAfter, back.suppliesAfter, Eps);
            back.supplies = f.supplies;
            back.suppliesAfter = f.suppliesAfter;
            Assert.AreEqual(Json(f), Json(back), "EVRAK arşivi gidiş-dönüşü veriyi değiştirmez.");
            Assert.IsTrue(string.IsNullOrEmpty(back.field[0].name), "Boş cep arşivden de boş döner (null → \"\").");
            Assert.IsTrue(string.IsNullOrEmpty(back.field[2].name));
            Assert.AreEqual(f.shelter.Count, back.shelter.Count);
            Assert.AreEqual(f.warnings.Count, back.warnings.Count);
            Assert.AreEqual(f.suppliesAfter, back.suppliesAfter, Eps);
        }

        // ---------------------------------------------------------------- E11 · durumlar ve uyarılar

        [Test]
        public void Facts_StatesAndWarnings()
        {
            var sim = Mixed(out string rescued, out string lost, out string restricted);
            var f = Build(sim);
            Assert.AreEqual(sim.S.Day + 1, f.shift);
            Assert.AreEqual(8, f.alive, "8 kurucu + alınan aday − kaybedilen.");

            // SAHA: kurtarılan yatışta (BOŞTA), kaybedilen öldü → iki boş cep; dönen EMP uzmanı kit ve zırhıyla
            CollectionAssert.AreEqual(new[] { "tek", "emp", "ana" }, f.field.Select(r => r.role));
            Assert.IsTrue(string.IsNullOrEmpty(f.field[0].name));
            CollectionAssert.IsEmpty(f.field[0].items);
            Assert.AreEqual(At(sim, 1).Name, f.field[1].name);
            CollectionAssert.AreEqual(new[] { "kit_emp", "armor" }, f.field[1].items);
            Assert.IsTrue(string.IsNullOrEmpty(f.field[2].name));
            CollectionAssert.IsEmpty(f.field[2].items);

            // SIĞINAK: cepler, sonra uygun BOŞTA'lar (yaralı da ayakta), en sonda yatan ve kısıtlı; ölüler yok
            var rows = f.shelter;
            CollectionAssert.AreEqual(new[]
            {
                At(sim, 3).Name, At(sim, 4).Name, At(sim, 5).Name, At(sim, 6).Name, At(sim, 7).Name,
                sim.Board.Person(rescued).Name, sim.Board.Person(restricted).Name,
            }, rows.Select(r => r.name));
            // 2026-11-24: EĞİTİM kaldırıldı; eski kayıttan eğitimdeki kişi BOŞTA satırıdır (emekli görev)
            CollectionAssert.AreEqual(new[] { "craft", "cook", "rest", "rest", "rest", "rest", "rest" }, rows.Select(r => r.job));
            Assert.IsTrue(string.IsNullOrEmpty(rows[2].subject));
            CollectionAssert.AreEqual(new[]
            {
                SlipPersonState.None, SlipPersonState.None, SlipPersonState.None, SlipPersonState.Hurt, SlipPersonState.None,
                SlipPersonState.Down, SlipPersonState.Restricted,
            }, rows.Select(r => r.state));
            Assert.AreEqual(0, rows[3].stateShifts);
            Assert.AreEqual(sim.Board.Person(rescued).Down, rows[5].stateShifts, "yatıyor n");
            Assert.AreEqual(sim.Admission.File(restricted).RestrictedUntil - sim.S.Day, rows[6].stateShifts, "kısıtlı n");
            Assert.AreEqual(2, rows[6].stateShifts, "V.002'de 3 vardiyalık kısıt, bugün V.003.");
            Assert.IsFalse(rows.Any(r => r.name == sim.Board.Person(lost).Name), "Ölüler fişte yok.");
            Assert.AreEqual(f.alive, f.shelter.Count + f.field.Count(r => !string.IsNullOrEmpty(r.name)), "Her canlı tek satırda.");

            // EĞİTİM bölümü yok (2026-11-24)
            Assert.IsFalse(f.training.active);

            // ÜRETİM: iş emri yok, otomatik üretim kapalı
            Assert.AreEqual(SlipProductionKind.None, f.production.kind);

            // KAPI: kapıdaki grubun aday sayısı; grup bu vardiya geldi (son vardiya değil) → KAPI kutusu yok; DOSYA riski yok
            Assert.AreEqual(sim.Admission.Group.Count, f.openApplications);
            Assert.Greater(f.openApplications, 1);
            CollectionAssert.IsEmpty(f.warnings);
        }

        /// <summary>
        /// KOS §7 (fiş uyarısı): grup geldiği vardiyada kutu yok; ikinci (son) vardiyasında tek bir KAPI kutusu (<c>DoorLastShift</c>, adsız),
        /// fiş basılınca da aynı; karar verilmezse Core grubu o kapanışta yollar ("expired") ve kutu kalkar. Eski <c>Door</c> türü hiç üretilmez.
        /// Fişi okumak durumu değiştirmez.
        /// </summary>
        [Test]
        public void Facts_DoorLastShift_OnlyOnTheSecondShift()
        {
            var sim = Simulation.NewGame(79, new Balance
            { shelterEventsEnabled = false,
                planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 3, admissionKnockMax = 3
            });
            for (int i = 0; i < 8 && sim.Admission.Group.Count == 0; i++)
            {
                var none = Build(sim);
                Assert.AreEqual(0, none.openApplications);
                CollectionAssert.IsEmpty(none.warnings, "Kapı boşken kutu yok.");
                Close(sim);
            }
            var adm = sim.Admission;
            int size = adm.Group.Count;
            Assert.Greater(size, 1, "kurulum: aday grubu");
            var ids = adm.Group.Select(c => c.Resident.Id).ToList();
            Assert.AreEqual(sim.S.Day, adm.GroupArrivalShift);
            Assert.AreEqual(sim.S.Day + 2, adm.GroupWaitingUntil);

            // Geliş vardiyası: kutu yok
            string before = Fingerprint(sim);
            var f = Build(sim);
            Assert.AreEqual(size, f.openApplications);
            CollectionAssert.IsEmpty(f.warnings, "Geliş vardiyasında grup bir vardiya daha bekler.");
            Assert.IsFalse(AdmissionSnapshotBuilder.GroupLastShift(sim));
            Assert.AreEqual(before, Fingerprint(sim), "Fişi okumak durumu değiştirmez.");

            // Son vardiya: tek KAPI kutusu, basılı fişte de
            Close(sim);
            CollectionAssert.AreEqual(ids, adm.Group.Select(c => c.Resident.Id), "Core: grup ikinci vardiyasını bekliyor.");
            Assert.IsTrue(AdmissionSnapshotBuilder.GroupLastShift(sim));
            f = Build(sim);
            Assert.AreEqual(size, f.openApplications);
            CollectionAssert.AreEqual(new[] { SlipWarningKind.DoorLastShift }, f.warnings.Select(w => w.kind));
            Assert.IsTrue(string.IsNullOrEmpty(f.warnings[0].name), "KAPI kutusu ad taşımaz.");
            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            CollectionAssert.AreEqual(new[] { SlipWarningKind.DoorLastShift }, Build(sim).warnings.Select(w => w.kind), "Basılı fişte de aynı.");
            Assert.AreEqual(Json(f).Replace("\"receiptId\":\"\"", "\"receiptId\":\"" + sim.Board.ReceiptId + "\""), Json(Build(sim)));

            // Kapanış: Core grubu yollar, kutu kalkar
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            Assert.AreEqual(0, adm.Group.Count, "Core: kararsız grup kapanışta gitti.");
            var gone = adm.Passed.Skip(adm.Passed.Count - size).ToList();
            Assert.IsTrue(gone.All(p => p.Kind == "expired" && p.Shift == sim.S.Day));
            f = Build(sim);
            Assert.AreEqual(0, f.openApplications);
            CollectionAssert.IsEmpty(f.warnings);

            // Son vardiyada karar verilirse kutu o an kalkar
            var decided = Doors();
            Close(decided);
            Close(decided);
            Assert.IsTrue(AdmissionSnapshotBuilder.GroupLastShift(decided), "kurulum: son vardiya");
            Assert.AreEqual(SlipWarningKind.DoorLastShift, Build(decided).warnings.Single().kind);
            Assert.IsTrue(decided.PassCandidates().Ok);
            CollectionAssert.IsEmpty(Build(decided).warnings);
            Assert.AreEqual(0, Build(decided).openApplications);
        }

        // ---------------------------------------------------------------- KAPANIŞTA BEKLENEN = kapanış

        [Test]
        public void Facts_PredictionMatchesClosing()
        {
            // V.001: emeklilik + kendi konusunda eğitim (2→4)
            var sim = Retired();
            Assert.IsTrue(sim.TrainResident(At(sim, 5).Id, "care").Ok);
            var v001 = CloseAndCompare(sim, "V.001");
            Assert.AreEqual(SlipProductionKind.None, v001.production.kind);

            // V.002: otomatik üretim girdileri tam (zırh plakası 0/1 → 1/1) + sıfırdan eğitim (→ EĞİTİMLİ)
            Assert.IsTrue(sim.StageProductionMaterial("metal", 2).Ok);
            Assert.IsTrue(sim.StageProductionMaterial("kumas", 1).Ok);
            Assert.AreEqual(0, At(sim, 6).Skill("emp"));
            Assert.IsTrue(sim.TrainResident(At(sim, 6).Id, "emp").Ok);
            var v002 = CloseAndCompare(sim, "V.002 otomatik üretim");
            Assert.AreEqual(SlipProductionKind.Progress, v002.production.kind);
            Assert.AreEqual("armor", v002.production.itemType);
            Assert.AreEqual(0, v002.production.done);
            Assert.AreEqual(1, v002.production.after);
            Assert.AreEqual(1, v002.production.turns);
            Assert.AreEqual(At(sim, 3).Name, v002.production.maker);

            // V.003: tarif uygun ama üretici yok → bakım kiti · çıktı yok
            Assert.IsTrue(sim.StageProductionMaterial("kumas", 1).Ok);
            Assert.IsTrue(sim.StageProductionMaterial("kimyasal", 1).Ok);
            Assert.IsTrue(sim.AssignResident(At(sim, 3).Id, "rest").Ok);
            var v003 = CloseAndCompare(sim, "V.003 üreticisiz otomatik üretim");
            Assert.AreEqual(SlipProductionKind.NoOutput, v003.production.kind);
            Assert.AreEqual("medkit", v003.production.itemType);
            Assert.IsFalse(v003.training.active);

            // İş emri: EMP hücresi 2 vardiya (0/2 → 1/2, sonra 1/2 → 2/2), sonra üreticisiz zırh emri ilerlemez
            var orders = Ready();
            Assert.IsTrue(orders.QueueProduction("emp_cell").Ok);
            Assert.IsTrue(orders.ReserveProductionMaterial("metal", orders.B.boardEmpMetal).Ok);
            Assert.IsTrue(orders.ReserveProductionMaterial("devre", orders.B.boardEmpCircuits).Ok);
            var first = CloseAndCompare(orders, "iş emri 1/2");
            Assert.AreEqual(SlipProductionKind.Progress, first.production.kind);
            Assert.AreEqual("emp_cell", first.production.itemType);
            Assert.AreEqual(0, first.production.done);
            Assert.AreEqual(1, first.production.after);
            Assert.AreEqual(2, first.production.turns);
            var second = CloseAndCompare(orders, "iş emri 2/2");
            Assert.AreEqual(1, second.production.done);
            Assert.AreEqual(2, second.production.after);
            Assert.IsTrue(orders.QueueProduction("armor").Ok);
            Assert.IsTrue(orders.ReserveProductionMaterial("metal", orders.B.boardArmorMetal).Ok);
            Assert.IsTrue(orders.ReserveProductionMaterial("kumas", orders.B.boardArmorCloth).Ok);
            Assert.IsTrue(orders.AssignResident(At(orders, 3).Id, "rest").Ok);
            var idle = CloseAndCompare(orders, "üreticisiz iş emri");
            Assert.AreEqual(SlipProductionKind.NoOutput, idle.production.kind);
            Assert.AreEqual("armor", idle.production.itemType);
            Assert.AreEqual(0, idle.production.after);

            // Baskın dönüşü: kilitli plan, yatan, kısıtlı, kapıda aday, ikmalde yaralı
            var raid = Mixed(out _, out _, out _);
            var back = CloseAndCompare(raid, "baskın dönüşü");
            Assert.IsFalse(back.training.active, "EĞİTİM bölümü yok (2026-11-24)");
        }

        // ---------------------------------------------------------------- E12 · gizli gelecek

        [Test]
        public void Facts_NoHiddenFuture()
        {
            Assert.IsTrue(File.Exists(BuilderPath), BuilderPath);
            string source = File.ReadAllText(BuilderPath);
            foreach (string hidden in new[] { "NextArrivalShift", ".Robot", "Sabotaged", "Identity", "ActiveShifts", "LastAccessJob", "Rng" })
                Assert.IsFalse(source.Contains(hidden), "Fiş verisi gizli geleceği ya da RNG'yi okumaz: " + hidden);
        }

        [Test]
        public void Facts_HiddenIdentityDoesNotChangeTheSlip()
        {
            // Aynı tohum ve görünür veri; adayların sızmacı olup olmaması gizlidir (olasılık 0 ve 1: çekimler aynı, yalnız gizli kimlik ayrışır).
            // İlk grup hiçbir zaman sızmacı taşımadığı için ikinci gruba bakılır: geliş vardiyası, son vardiya ve seçim anı.
            Simulation SecondGroup(double robotChance)
            {
                var sim = Doors(79, robotChance, knockMax: 4);
                for (int i = 0; i < 8 && sim.Admission.Group.Count == 0; i++) Close(sim);
                Assert.Greater(sim.Admission.Group.Count, 0);
                Assert.IsTrue(sim.PassCandidates().Ok);
                for (int i = 0; i < 8 && sim.Admission.Group.Count == 0; i++) Close(sim);
                Assert.Greater(sim.Admission.Group.Count, 0);
                return sim;
            }
            var human = SecondGroup(0);
            var robot = SecondGroup(1);
            Assert.AreEqual(human.S.Day, robot.S.Day);
            Assert.AreEqual(human.S.Rng.State, robot.S.Rng.State, "Kimlik aynı çekimin eşiğidir: görünür akış ayrışmaz.");
            CollectionAssert.AreEqual(human.Admission.Group.Select(c => c.Resident.Id + c.Tier + c.SuspicionKey + string.Join(",", c.Perks)),
                robot.Admission.Group.Select(c => c.Resident.Id + c.Tier + c.SuspicionKey + string.Join(",", c.Perks)), "Görünür adaylar aynı.");
            Assert.AreEqual(Dump(Build(human)), Dump(Build(robot)), "Geliş vardiyası: fiş gizli kimliği yazmaz.");
            Close(human);
            Close(robot);
            Assert.AreEqual(SlipWarningKind.DoorLastShift, Build(human).warnings.Single().kind, "kurulum: son vardiya");
            Assert.AreEqual(Dump(Build(human)), Dump(Build(robot)), "Son vardiya: fiş aynı.");
            string id = human.Admission.Group[0].Resident.Id;
            Assert.IsTrue(human.ChooseCandidate(id).Ok);
            Assert.IsTrue(robot.ChooseCandidate(id).Ok);
            Assert.AreEqual(Dump(Build(human)), Dump(Build(robot)), "Seçim anı: fiş aynı.");
        }
    }
}
