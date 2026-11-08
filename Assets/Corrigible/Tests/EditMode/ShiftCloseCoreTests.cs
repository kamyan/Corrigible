using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Corrigible.Core;
using Corrigible.EditorTools;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 4 vardiya kapanışının Core değişmezleri (PLAN W4, §5.1): fiş EVRAK'a girmeden gün ilerlemez, dünkü fiş
    /// yeniden kullanılmaz, kayıtta eksik devir yüklemeyi durdurur, yinelenen devir günü bir kez ilerletir, fiş önizlemesi saftır ve
    /// dil yalnız sözcükleri değiştirir. Aşama 4 Core'u değiştirmedi (o gün <c>GameRecord.CurrentVersion</c> 14; Aşama 6 gözetmen kuralıyla 15); bu testler bugünkü davranışı kilitler.
    /// </summary>
    public class ShiftCloseCoreTests
    {
        static Simulation Ready(uint seed = 79) => Simulation.NewGame(seed, new Balance
        {
            planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100
        });

        static void Close(Simulation sim)
        {
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }

        /// <summary>Kaydın bağımsız kopyası (yeniden oynatma kaydı kendine bağlar; asıl kayıt değişmesin).</summary>
        static GameRecord CopyOf(GameRecord rec, int day) => new GameRecord
        {
            version = rec.version, seed = rec.seed, balance = rec.balance.Clone(), day = day,
            commands = rec.commands.Select(c => new SavedCommand { day = c.day, kind = c.kind, a = c.a, b = c.b }).ToList(),
        };

        static string Fingerprint(Simulation sim)
        {
            var b = sim.Board;
            var sb = new StringBuilder();
            sb.Append(sim.S.Rng.State).Append('|').Append(sim.Record.commands.Count).Append('|').Append(sim.S.Day).Append('|')
              .Append(sim.Shelter.Supplies.ToString("R")).Append('|').Append(b.Locked).Append(b.Printed).Append(b.Filing).Append('|')
              .Append(b.ReceiptId).Append('|').Append(b.LastFiledReceiptId).Append('|').Append(b.ReceiptText).Append('|');
            foreach (var p in b.People)
                sb.Append(p.Id).Append(p.Job).Append(p.Hp).Append(p.Down).Append(p.Alive).Append(p.TrainingSubject)
                  .Append(string.Join(",", p.Experience.Select(x => x.Subject + x.Points))).Append(';');
            foreach (var e in b.Equipment) sb.Append(e.Id).Append(e.OwnerId).Append(';');
            foreach (var m in b.Materials) sb.Append(m.Kind).Append(m.Quantity).Append(';');
            foreach (var o in b.Orders) sb.Append(o.Id).Append(o.Remaining).Append(o.Started).Append(';');
            sb.Append(sim.Admission.Events.Count);
            return sb.ToString();
        }

        static IEnumerable<string> Digits(string line) => Regex.Matches(line, @"\d+").Cast<Match>().Select(m => m.Value);

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        // ---------------------------------------------------------------- fiş EVRAK'a girmeden gün ilerlemez

        [Test]
        public void Step_AfterPrint_DoesNotAdvance()
        {
            var sim = Ready();
            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            string receipt = sim.Board.ReceiptId;
            string before = Fingerprint(sim);
            int day = sim.S.Day;
            for (int i = 0; i < 3; i++) sim.Step();
            Assert.AreEqual(day, sim.S.Day, "Basılı fiş tek başına zamanı ilerletmez; yalnız devir kutusu.");
            Assert.AreEqual(before, Fingerprint(sim), "Step basılı planı değiştirmez, RNG çekmez, komut yazmaz.");
            Assert.IsTrue(sim.Board.Printed);
            Assert.IsTrue(sim.Board.Locked);
            Assert.IsFalse(sim.Board.Filing);
            Assert.AreEqual(receipt, sim.Board.ReceiptId);

            Assert.IsTrue(sim.FileShift(receipt).Ok);
            Assert.AreEqual(day + 1, sim.S.Day);
            sim.Step();
            Assert.AreEqual(day + 1, sim.S.Day, "Devirden sonra yeni fiş basılmadan zaman yine durur.");
        }

        // ---------------------------------------------------------------- aynı fiş yeniden kullanılmaz

        [Test]
        public void File_YesterdaysReceipt_RejectedAfterNextPrint()
        {
            var sim = Ready();
            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.AreEqual("vardiya-0", sim.Board.ReceiptId);
            Assert.IsTrue(sim.FileShift("vardiya-0").Ok);
            Assert.AreEqual(1, sim.S.Day);
            Assert.AreEqual("vardiya-0", sim.Board.LastFiledReceiptId);
            Assert.IsFalse(sim.FileShift("vardiya-0").Ok, "Basılı fiş yokken dünkü fiş geçmez.");

            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.AreEqual("vardiya-1", sim.Board.ReceiptId);
            string before = Fingerprint(sim);
            var stale = sim.FileShift("vardiya-0");
            Assert.IsFalse(stale.Ok, "Ertesi vardiya basılıyken dünkü fiş reddedilir.");
            Assert.AreEqual("Bu fiş bu vardiyanın etkin devir fişi değil.", stale.Msg);
            Assert.AreEqual(before, Fingerprint(sim), "Ret gün, RNG, komut ve planı değiştirmez.");
            Assert.IsTrue(sim.Board.Printed);

            Assert.IsTrue(sim.FileShift("vardiya-1").Ok);
            Assert.AreEqual(2, sim.S.Day);
            CollectionAssert.AreEqual(new[] { "vardiya-0@0", "vardiya-1@1" },
                sim.Record.commands.Where(c => c.kind == "board_file").Select(c => c.a + "@" + c.day),
                "Her devir bir kez, eski günle kaydedilir.");
            Assert.IsFalse(sim.FileShift("vardiya-1").Ok, "Aynı fiş ikinci kez devredilmez.");
            Assert.AreEqual(2, sim.S.Day);
        }

        // ---------------------------------------------------------------- kayıt ve yeniden oynatma

        [Test]
        public void Replay_MissingBoardFile_Throws()
        {
            var sim = Ready();
            Close(sim);
            Close(sim);
            Assert.AreEqual(2, sim.S.Day);

            var whole = Replay.Restore(CopyOf(sim.Record, sim.S.Day));
            Assert.AreEqual(sim.S.Day, whole.S.Day);
            Assert.AreEqual(sim.S.Rng.State, whole.S.Rng.State);

            var lastMissing = CopyOf(sim.Record, sim.S.Day);
            lastMissing.commands.RemoveAt(lastMissing.commands.FindLastIndex(c => c.kind == "board_file"));
            var ex = Assert.Throws<InvalidOperationException>(() => Replay.Restore(lastMissing));
            StringAssert.Contains("devir komutu eksik", ex.Message);

            var firstMissing = CopyOf(sim.Record, sim.S.Day);
            firstMissing.commands.RemoveAt(firstMissing.commands.FindIndex(c => c.kind == "board_file"));
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(firstMissing), "Eksik devirden sonraki komutlar da gün ilerletemez.");
        }

        [Test]
        public void Replay_DuplicateBoardFile_AdvancesOnce()
        {
            var sim = Ready();
            Close(sim);
            Assert.AreEqual(1, sim.S.Day);

            var duplicated = CopyOf(sim.Record, sim.S.Day);
            int at = duplicated.commands.FindIndex(c => c.kind == "board_file");
            var file = duplicated.commands[at];
            duplicated.commands.Insert(at + 1, new SavedCommand { day = file.day, kind = file.kind, a = file.a, b = file.b });
            var restored = Replay.Restore(duplicated);
            Assert.AreEqual(1, restored.S.Day, "Aynı gün yinelenen devir günü bir kez ilerletir.");
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(sim.Shelter.Supplies, restored.Shelter.Supplies);
            Assert.AreEqual("vardiya-0", restored.Board.LastFiledReceiptId);
            Assert.IsFalse(restored.Board.Printed);

            // Ertesi vardiyanın basılı fişinin yanında kayda düşmüş dünkü devir de yok sayılır
            Assert.IsTrue(sim.PrintShift().Ok);
            var stale = CopyOf(sim.Record, sim.S.Day);
            stale.commands.Add(new SavedCommand { day = sim.S.Day, kind = "board_file", a = "vardiya-0" });
            var pending = Replay.Restore(stale);
            Assert.AreEqual(1, pending.S.Day);
            Assert.IsTrue(pending.Board.Printed, "Dünkü fiş bugünkü basılı fişi devretmez.");
            Assert.AreEqual("vardiya-1", pending.Board.ReceiptId);
            Assert.IsTrue(sim.FileShift("vardiya-1").Ok);
            Assert.IsTrue(pending.FileShift("vardiya-1").Ok);
            Assert.AreEqual(sim.S.Day, pending.S.Day);
            Assert.AreEqual(sim.S.Rng.State, pending.S.Rng.State);
        }

        // ---------------------------------------------------------------- fiş önizlemesi

        [Test]
        public void Preview_StableAndPure_LanguageChangesWordsOnly()
        {
            var sim = Ready();
            Assert.IsTrue(sim.EnableAutomaticProduction().Ok);
            Assert.IsTrue(sim.TrainResident(sim.Board.People.Single(p => p.Job == "rest").Id, "craft").Ok);
            Assert.IsTrue(sim.QueueProduction("emp_cell").Ok);
            foreach (var input in sim.BoardRecipes.Single(r => r.Id == "emp_cell").Inputs)
                Assert.IsTrue(sim.ReserveProductionMaterial(input.Kind, input.Quantity).Ok);
            string before = Fingerprint(sim);

            string tr = sim.BoardReceiptPreview;
            Assert.IsNotNull(tr);
            for (int i = 0; i < 20; i++) Assert.AreEqual(tr, sim.BoardReceiptPreview, "Önizleme her okumada aynı.");
            Assert.AreEqual(before, Fingerprint(sim), "Önizleme RNG çekmez, komut yazmaz, durumu değiştirmez.");

            Loc.SetLanguage(Loc.English, Loc.ParseTable(File.ReadAllText(LocalizationTool.TableFile))[Loc.English]);
            string en = sim.BoardReceiptPreview;
            Assert.AreNotEqual(tr, en, "İngilizce tabloyla sözcükler değişir.");
            string[] trLines = tr.Split('\n'), enLines = en.Split('\n');
            Assert.AreEqual(trLines.Length, enLines.Length, "Dil satır sayısını değiştirmez.");
            for (int i = 0; i < trLines.Length; i++)
                CollectionAssert.AreEqual(Digits(trLines[i]).ToList(), Digits(enLines[i]).ToList(), "Satır " + i + ": sayılar aynı (yalnız ayırıcı dile göre).");
            foreach (var p in sim.Board.People.Where(p => p.Alive))
                StringAssert.Contains(p.Name, en, "Kişi adları çevrilmez.");
            Assert.AreEqual(before, Fingerprint(sim), "Dil değişimi oyunu değiştirmez.");
            Loc.SetLanguage(Loc.Turkish);
            Assert.AreEqual(tr, sim.BoardReceiptPreview);

            // Basılı fiş basıldığı dilde saklanır; önizleme basılan planı o anki dilde yazar
            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            Assert.AreEqual(tr, sim.Board.ReceiptText);
            Assert.AreEqual(tr, sim.BoardReceiptPreview, "Basılıyken plan kilitli: önizleme = basılan plan.");
            string printed = Fingerprint(sim);
            Loc.SetLanguage(Loc.English, Loc.ParseTable(File.ReadAllText(LocalizationTool.TableFile))[Loc.English]);
            Assert.AreEqual(en, sim.BoardReceiptPreview);
            Assert.AreEqual(tr, sim.Board.ReceiptText, "Kayıtlı fiş metni dil değişiminde yeniden yazılmaz.");
            Assert.AreEqual(printed, Fingerprint(sim));
        }
    }
}
