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
        {
            planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100
        });

        /// <summary>Kapıya her vardiya bir aday gelir; robot ve belge kusuru payı ayarlı.</summary>
        static Simulation Doors(uint seed = 79, double robotChance = 0, int knockMax = 1, double faultChance = 0) =>
            Simulation.NewGame(seed, new Balance
            {
                planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = knockMax,
                admissionRobotChance = robotChance, admissionPaperFaultChance = faultChance
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
        /// E11 durumu: V.002'de ilk aday kısıtlı kabul edilir, V.003'te ikinci aday kapıda; bir kişi sıfırdan eğitimde, bir kişi yaralı
        /// ama ayakta; baskın dönüşünde kadronun ilki kurtarılır (yatıyor), üçüncüsü geride kalır (ölür). Plan baskınla kilitli.
        /// </summary>
        static Simulation Mixed(out string rescued, out string lost, out string restricted)
        {
            var sim = Doors();
            Close(sim);
            Assert.IsNotNull(sim.Admission.Candidate, "V.002: ilk aday kapıda.");
            restricted = sim.Admission.Candidate.Resident.Id;
            var decided = sim.DecideApplicant("restricted");
            Assert.IsTrue(decided.Ok, decided.Msg);
            Close(sim);
            Assert.IsNotNull(sim.Admission.Candidate, "V.003: ikinci aday kapıda.");
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
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Retreat = true, Hp = { 0, squad[1].Hp, 0 }, Armor = { 0, 1, 0 }, Left = { 2 } }).Ok);
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
            foreach (var e in b.Equipment) sb.Append(e.Id).Append(e.Kind).Append(e.OwnerId).Append(e.Charges).Append(';');
            foreach (var m in b.Materials) sb.Append(m.Kind).Append(m.Quantity).Append(';');
            foreach (var m in b.ProductionInputs) sb.Append(m.Kind).Append(m.Quantity).Append(';');
            foreach (var o in b.Orders) sb.Append(o.Id).Append(o.RecipeId).Append(o.Remaining).Append(o.Started).Append(';');
            var a = sim.Admission;
            sb.Append(a.ObservationTarget).Append('|').Append(a.Events.Count).Append('|').Append(a.NextArrivalShift).Append('|')
              .Append(a.Candidate?.Resident.Id).Append(a.Candidate?.WaitingUntil).Append('|');
            foreach (var f in a.Files) sb.Append(f.PersonId).Append(f.ReviewPending).Append(f.ReviewNeeded).Append(f.RestrictedUntil).Append(f.ClueCount).Append(';');
            return sb.ToString();
        }

        /// <summary>Core'un o kişi için yazdığı kabul riskinin türü (Türkçe metnin başından; yalnız test).</summary>
        static SlipWarningKind CoreKind(string risk)
        {
            Assert.IsNotNull(risk);
            if (risk.StartsWith("Kısıtlı kabul süresi doldu", StringComparison.Ordinal)) return SlipWarningKind.FileRestrictionExpired;
            if (risk.StartsWith("İki bağımsız bulgu", StringComparison.Ordinal)) return SlipWarningKind.FileClues;
            StringAssert.StartsWith("Güncel talimat", risk);
            return SlipWarningKind.FileReview;
        }

        /// <summary>
        /// Fişi kurar, vardiyayı kapatır ve tahmini gerçek kapanışla karşılaştırır: ERZAK, EĞİTİM (deneyim, kademe), ÜRETİM (ilerleme,
        /// tamamlanan). Kapanışın başında gizli bir kabul olayı (müdahale, kapatma) olduysa fiş onu bilerek bilmez; o kapanış atlanır.
        /// </summary>
        static HandoverSlipFacts CloseAndCompare(Simulation sim, string label)
        {
            var f = Build(sim);
            var trainee = BoardDropRules.Occupant(sim.Board, "train");
            var order = sim.Board.Orders.FirstOrDefault();
            int remaining = order?.Remaining ?? 0;
            int events = sim.Admission.Events.Count;
            Close(sim);
            var hidden = sim.Admission.Events.Skip(events).Where(e => e.Kind == "sabotage" || e.Kind == "contained").Select(e => e.Kind).ToList();
            if (hidden.Count > 0)
            {
                TestContext.WriteLine(label + ": kapanışta gizli kabul olayı (" + string.Join(", ", hidden) + "); fiş bunu bilmez, karşılaştırma atlandı.");
                return f;
            }
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

            // SIĞINAK: ÜRETİM, MUTFAK, EĞİTİM, sonra iki BOŞTA (emekliler deste sırasıyla)
            CollectionAssert.AreEqual(new[] { At(sim, 3).Name, At(sim, 4).Name, trainee.Name, At(sim, 6).Name, At(sim, 7).Name }, f.shelter.Select(r => r.name));
            CollectionAssert.AreEqual(new[] { "craft", "cook", "train", "rest", "rest" }, f.shelter.Select(r => r.job));
            Assert.AreEqual("care", f.shelter[2].subject);
            Assert.IsTrue(f.shelter.Where((r, i) => i != 2).All(r => string.IsNullOrEmpty(r.subject)), "Konu yalnız eğitim satırında.");
            Assert.IsTrue(f.shelter.All(r => r.state == SlipPersonState.None && r.stateShifts == 0), "V.001'de durum eki yok.");

            // KAPANIŞTA BEKLENEN: ERZAK 24,0 → 21,9 / 60 −2,1 · EĞİTİM +2 (2→4) · ÜRETİM iş emri yok · çıktı yok · KAPI açık başvuru yok
            Assert.AreEqual(21.9, f.suppliesAfter, Eps);
            Assert.AreEqual(-2.1, f.suppliesAfter - f.supplies, Eps);
            Assert.AreEqual(sim.BoardFoodPreview, f.suppliesAfter - f.supplies, Eps);
            Assert.IsTrue(f.training.active);
            Assert.AreEqual(trainee.Name, f.training.name);
            Assert.AreEqual("care", f.training.subject);
            Assert.AreEqual(2, f.training.xpBefore);
            Assert.AreEqual(4, f.training.xpAfter);
            Assert.AreEqual(1, f.training.tierBefore);
            Assert.AreEqual(1, f.training.tierAfter, "Kademe değişmez: satır \"(2→4)\" biter.");
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
            Assert.AreEqual(8, f.alive, "8 kurucu + kısıtlı kabul − kaybedilen.");

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
            CollectionAssert.AreEqual(new[] { "craft", "cook", "train", "rest", "rest", "rest", "rest" }, rows.Select(r => r.job));
            Assert.AreEqual("emp", rows[2].subject);
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

            // EĞİTİM: sıfırdan, kademe atlar (→ EĞİTİMLİ)
            Assert.IsTrue(f.training.active);
            Assert.AreEqual(At(sim, 5).Name, f.training.name);
            Assert.AreEqual("emp", f.training.subject);
            Assert.AreEqual(0, f.training.xpBefore);
            Assert.AreEqual(sim.B.boardTrainingGain, f.training.xpAfter);
            Assert.AreEqual(0, f.training.tierBefore);
            Assert.AreEqual(1, f.training.tierAfter);

            // ÜRETİM: iş emri yok, otomatik üretim kapalı
            Assert.AreEqual(SlipProductionKind.None, f.production.kind);

            // KAPI: 1 açık başvuru, bu kapanışta ret sayılacak → KAPI kutusu; DOSYA riski yok
            Assert.AreEqual(1, f.openApplications);
            Assert.AreEqual(1, f.warnings.Count);
            Assert.AreEqual(SlipWarningKind.Door, f.warnings[0].kind);
            Assert.IsTrue(string.IsNullOrEmpty(f.warnings[0].name));
        }

        [Test]
        public void Facts_FileWarnings_FollowCoreRiskOrder_OneBoxPerKind()
        {
            var sim = Ready();
            var adm = sim.Admission;
            adm.File(At(sim, 5).Id).ReviewNeeded = true;
            adm.File(At(sim, 6).Id).ReviewNeeded = true;
            adm.File(At(sim, 7).Id).ClueCount = 2;
            adm.File(At(sim, 3).Id).ClueCount = 2;
            adm.File(At(sim, 3).Id).ReviewNeeded = true;     // Core bulguyu incelemeden önce yazar
            At(sim, 4).Restricted = true;
            adm.File(At(sim, 4).Id).RestrictedUntil = sim.S.Day;   // süresi bu vardiya doldu

            var f = Build(sim);
            CollectionAssert.AreEqual(new[] { SlipWarningKind.FileRestrictionExpired, SlipWarningKind.FileClues, SlipWarningKind.FileReview },
                f.warnings.Select(w => w.kind), "Kutu sırası Core'un kabul riski sırası; tür başına tek kutu.");
            Assert.AreEqual(At(sim, 4).Name, f.warnings[0].name);
            Assert.AreEqual(At(sim, 3).Name + ", " + At(sim, 7).Name, f.warnings[1].name, "Aynı türün adları panodaki sırayla, virgülle.");
            Assert.AreEqual(At(sim, 5).Name + ", " + At(sim, 6).Name, f.warnings[2].name);
            foreach (var w in f.warnings)
                foreach (string name in w.name.Split(new[] { ", " }, StringSplitOptions.None))
                {
                    var person = sim.Board.People.Single(p => p.Name == name);
                    Assert.AreEqual(w.kind, CoreKind(sim.ResidentAdmissionRisk(person.Id)), name + ": fişin türü Core'un oyunda gösterdiği riskle aynı.");
                }
            var cook = f.shelter.Single(r => r.name == At(sim, 4).Name);
            Assert.AreEqual(SlipPersonState.RestrictionEnded, cook.state, "kısıtlı · bitti");
            Assert.AreEqual(0, cook.stateShifts);

            // Kapatılan dosyada bulgu kutusu düşer, inceleme kalır
            adm.File(At(sim, 3).Id).Contained = true;
            f = Build(sim);
            Assert.AreEqual(At(sim, 7).Name, f.warnings[1].name);
            Assert.AreEqual(At(sim, 3).Name + ", " + At(sim, 5).Name + ", " + At(sim, 6).Name, f.warnings[2].name);
            Assert.AreEqual(SlipWarningKind.FileReview, CoreKind(sim.ResidentAdmissionRisk(At(sim, 3).Id)));
        }

        [Test]
        public void Facts_DoorWarning_FollowsCoreRejection()
        {
            var sim = Doors();
            Close(sim);
            var candidate = sim.Admission.Candidate;
            Assert.IsNotNull(candidate);
            string id = candidate.Resident.Id;
            var f = Build(sim);
            Assert.AreEqual(1, f.openApplications);
            CollectionAssert.AreEqual(new[] { SlipWarningKind.Door }, f.warnings.Select(w => w.kind), "Kararsız başvuru bu kapanışta reddedilir.");

            // Dış kayıt teyidi bu vardiya gözetimde: karar bir vardiya uzar, kutu çıkmaz. K4-A (biçim 15): gözetmen Boşta'nın ilk
            // uygun kişisi (burada BOŞTA'daki kurucu), ayrı görev ataması yok
            Assert.AreSame(At(sim, 7), sim.AdmissionWatcher(id));
            var inspected = sim.InspectApplicant("record");
            Assert.IsTrue(inspected.Ok, inspected.Msg);
            f = Build(sim);
            Assert.AreEqual(1, f.openApplications);
            CollectionAssert.IsEmpty(f.warnings);
            Close(sim);
            Assert.AreEqual(id, sim.Admission.Candidate?.Resident.Id, "Core: dış kayıt kararı bir vardiya uzattı.");

            // Uzatma bir kez: şimdi kutu yine çıkar ve Core adayı kapanışta reddeder
            f = Build(sim);
            CollectionAssert.AreEqual(new[] { SlipWarningKind.Door }, f.warnings.Select(w => w.kind));
            Close(sim);
            Assert.AreNotEqual(id, sim.Admission.Candidate?.Resident.Id, "Core: kararsız başvuru kapanışta ret sayıldı.");
            Assert.IsTrue(sim.Admission.Events.Any(e => e.PersonId == id && e.Kind == "default_reject"));
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
            Assert.AreEqual(0, v002.training.tierBefore);
            Assert.AreEqual(1, v002.training.tierAfter);

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
            Assert.IsTrue(back.training.active);
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
            // Aynı tohum ve görünür veri; ikinci adayın robot olup olmaması ve sıradaki gelişin zamanı gizlidir
            Simulation SecondApplicant(double robotChance)
            {
                var sim = Doors(79, robotChance, knockMax: 4);
                for (int i = 0; i < 8 && sim.Admission.Candidate == null; i++) Close(sim);
                Assert.IsNotNull(sim.Admission.Candidate);
                Assert.IsTrue(sim.DecideApplicant("reject").Ok);
                for (int i = 0; i < 8 && sim.Admission.Candidate == null; i++) Close(sim);
                Assert.IsNotNull(sim.Admission.Candidate);
                return sim;
            }
            var human = SecondApplicant(0);
            var robot = SecondApplicant(1);
            Assert.AreEqual(human.S.Day, robot.S.Day);
            Assert.AreEqual(human.Admission.Candidate.Papers.Serial, robot.Admission.Candidate.Papers.Serial, "Görünür belge aynı.");
            Assert.AreNotEqual(human.S.Rng.State, robot.S.Rng.State, "Gizli durum gerçekten ayrıştı (robot modeli ayrı çekilir).");
            Assert.AreEqual(Dump(Build(human)), Dump(Build(robot)), "Fiş gizli kimliği ve gelecek gelişi yazmaz (sıradaki geliş: insan "
                + human.Admission.NextArrivalShift + ", robot " + robot.Admission.NextArrivalShift + ").");
        }
    }
}
