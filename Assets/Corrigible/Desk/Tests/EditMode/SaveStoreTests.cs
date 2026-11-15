using System;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Gerçek oyuncu klasörüne dokunmadan disk kaydı, sürüm arşivi ve kartlı modelin devamı.</summary>
    public sealed class SaveStoreTests
    {
        const string StorageKey = "Corrigible.TestStorage";
        string originalStorage, directory;

        [SetUp]
        public void UseIsolatedStorage()
        {
            originalStorage = SessionState.GetString(StorageKey, "");
            directory = Path.Combine(Path.GetTempPath(), "Corrigible-SaveStore-" + Guid.NewGuid().ToString("N"));
            SessionState.SetString(StorageKey, directory);
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void RestoreStorage()
        {
            if (string.IsNullOrEmpty(originalStorage)) SessionState.EraseString(StorageKey);
            else SessionState.SetString(StorageKey, originalStorage);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        static Simulation NewGame(uint seed = 79) => Simulation.NewGame(seed, new Balance
        { shelterEventsEnabled = false,
            planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = 1
        });

        static void Close(Simulation sim)
        {
            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
        }

        static string GroupDump(Simulation sim)
        {
            var a = sim.Admission;
            return a.GroupArrivalShift + "|" + a.GroupWaitingUntil + "|" + a.GroupEmergency + "|" + a.NextArrivalShift + "|" + a.Passed.Count + "|" +
                string.Join(";", a.Group.Select(c => c.Resident.Id + ":" + c.Resident.Name + ":" + c.Tier + ":" + c.SuspicionKey + ":" + c.SuspicionArg + ":" +
                    string.Join(",", c.Perks) + ":" + JsonUtility.ToJson(c.Resident)));
        }

        [Test]
        public void DiskRoundTrip_RestoresLockedReceiptProductionTrainingAndCandidateGroup_ThenContinuesIdentically()
        {
            var sim = NewGame();
            var trainee = sim.Board.People.Single(p => p.Job == "rest");
            Assert.IsTrue(sim.TrainResident(trainee.Id, "craft").Ok);
            Assert.IsTrue(sim.QueueProduction("emp_cell").Ok);
            foreach (var input in sim.BoardRecipes.Single(r => r.Id == "emp_cell").Inputs)
                Assert.IsTrue(sim.ReserveProductionMaterial(input.Kind, input.Quantity).Ok);
            Close(sim);
            // İlk gruptan bir aday alınır (candidate_choose kayıtta), sonraki kapanışta ikinci grup kapıda bekler
            Assert.Greater(sim.Admission.Group.Count, 1, "kapıda aday grubu");
            string chosen = sim.Admission.Group[0].Resident.Id;
            Assert.IsTrue(sim.ChooseCandidate(chosen).Ok);
            Close(sim);
            Assert.Greater(sim.Admission.Group.Count, 1, "ikinci grup");
            Assert.IsTrue(sim.PrintShift().Ok);
            sim.Record.day = sim.S.Day;

            SaveStore.Write(sim.Record);
            Assert.IsFalse(File.Exists(SaveStore.SavePath + ".writing"));
            var loaded = SaveStore.Load();
            Assert.IsNotNull(loaded);
            Assert.IsTrue(loaded.commands.Any(c => c.kind == "candidate_choose" && c.a == chosen), "seçim komutu kayıtta");
            var restored = Replay.Restore(loaded);
            Assert.AreEqual(JsonUtility.ToJson(sim.Board), JsonUtility.ToJson(restored.Board));
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.IsNotNull(restored.Board.Person(chosen), "alınan kişi yüklemeden sonra panoda");
            Assert.AreEqual(GroupDump(sim), GroupDump(restored), "bekleyen grup (kimlikler, kademeler, şüphe, özellikler) aynı");

            string receipt = sim.Board.ReceiptId;
            Assert.IsTrue(sim.FileShift(receipt).Ok);
            Assert.IsTrue(restored.FileShift(receipt).Ok);
            Assert.AreEqual(JsonUtility.ToJson(sim.Board), JsonUtility.ToJson(restored.Board));
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(sim.Shelter.Supplies, restored.Shelter.Supplies);
            Assert.AreEqual(sim.S.Day, restored.S.Day);

            // Aynı seçim iki oyunda aynı duruma varır
            Assert.AreEqual(GroupDump(sim), GroupDump(restored));
            string second = sim.Admission.Group[sim.Admission.Group.Count - 1].Resident.Id;
            Assert.IsTrue(sim.ChooseCandidate(second).Ok);
            Assert.IsTrue(restored.ChooseCandidate(second).Ok);
            Assert.AreEqual(JsonUtility.ToJson(sim.Board), JsonUtility.ToJson(restored.Board));
            Assert.AreEqual(GroupDump(sim), GroupDump(restored));
            Close(sim);
            Close(restored);
            Assert.AreEqual(JsonUtility.ToJson(sim.Board), JsonUtility.ToJson(restored.Board));
            Assert.AreEqual(sim.Shelter.Supplies, restored.Shelter.Supplies);
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
        }

        [Test]
        public void NewVersion_PreservesEveryDistinctOlderFileBeforeAtomicReplacement()
        {
            var current = NewGame().Record;
            var older = new GameRecord { version = GameRecord.CurrentVersion - 1, seed = 1, day = 4 };
            string first = JsonUtility.ToJson(older);
            File.WriteAllText(SaveStore.SavePath, first);
            SaveStore.Write(current);
            string archive = SaveStore.SavePath + ".v" + older.version + ".backup";
            Assert.AreEqual(first, File.ReadAllText(archive));

            older.seed = 2;
            string second = JsonUtility.ToJson(older);
            File.WriteAllText(SaveStore.SavePath, second);
            SaveStore.Write(current);
            Assert.AreEqual(first, File.ReadAllText(archive));
            Assert.AreEqual(second, File.ReadAllText(archive + ".1"));
            Assert.AreEqual(JsonUtility.ToJson(current), File.ReadAllText(SaveStore.SavePath));
            Assert.IsFalse(File.Exists(SaveStore.SavePath + ".writing"));
        }

        [Test]
        public void UnreadableOldJson_IsPreservedAndDoesNotPreventSavingTheNewGame()
        {
            const string broken = "{this is not json";
            const string earlier = "an earlier damaged save";
            string archive = SaveStore.SavePath + ".unreadable.backup";
            File.WriteAllText(archive, earlier);
            File.WriteAllText(SaveStore.SavePath, broken);
            var sim = NewGame();
            Assert.IsTrue(sim.PrintShift().Ok);

            SaveStore.Write(sim.Record);
            Assert.AreEqual(earlier, File.ReadAllText(archive));
            Assert.AreEqual(broken, File.ReadAllText(archive + ".1"));
            Assert.IsNotNull(SaveStore.Load());
            Assert.AreEqual(JsonUtility.ToJson(sim.Record), File.ReadAllText(SaveStore.SavePath));
        }

        [Test]
        public void Load_RejectsARecordWithoutCommandHistoryEvenIfItHasADay()
        {
            var invalid = new GameRecord { day = 2, balance = new Balance { shelterEventsEnabled = false }, commands = null };
            File.WriteAllText(SaveStore.SavePath, JsonUtility.ToJson(invalid));
            Assert.IsNull(SaveStore.Load());
        }

        /// <summary>
        /// Sürüm 16'nın olaylardan önceki (aşama 1–2) kayıtlarında olay alanı yoktur; JsonUtility onu C# varsayılanıyla (olaylar açık)
        /// doldururdu ve kayıt olaylar açık yeniden oynanırdı. Alanı taşımayan kayıt yüklenmez; aynı kayıt alanla yüklenir.
        /// </summary>
        [Test]
        public void Load_RejectsAVersion16RecordWrittenBeforeShelterEvents()
        {
            var sim = NewGame();
            Close(sim);
            string json = JsonUtility.ToJson(sim.Record);
            StringAssert.Contains("\"shelterEventsEnabled\":false", json);
            File.WriteAllText(SaveStore.SavePath, json);
            Assert.IsNotNull(SaveStore.Load(), "olay alanını taşıyan kayıt yüklenir");

            File.WriteAllText(SaveStore.SavePath, json.Replace("\"shelterEventsEnabled\":false,", ""));
            var fromDefaults = JsonUtility.FromJson<GameRecord>(File.ReadAllText(SaveStore.SavePath));
            Assert.AreEqual(GameRecord.CurrentVersion, fromDefaults.version);
            Assert.IsTrue(fromDefaults.balance.shelterEventsEnabled, "eksik alan C# varsayılanıyla dolar");
            Assert.IsNull(SaveStore.Load(), "aşama 1–2 kaydı olaylar açık yeniden oynanmaz");
        }

        /// <summary>
        /// KOS aşama 7: silinen eski kabul komutlarından birini taşıyan sürüm 17 kaydı (olaylar kapalı geliştirme kaydı) yüklenmez, çünkü
        /// Replay o komutları artık tanımaz; yeni oyun yazılırken arşivlenir. Aynı kayıt komut olmadan yüklenir.
        /// </summary>
        [Test]
        public void Load_RejectsARecordWithARetiredAdmissionCommand_AndWriteArchivesIt()
        {
            var sim = NewGame();
            Close(sim);
            string json = JsonUtility.ToJson(sim.Record);
            File.WriteAllText(SaveStore.SavePath, json);
            Assert.IsNotNull(SaveStore.Load(), "eski komutsuz kayıt yüklenir");
            foreach (string kind in new[] { "inspectApplicant", "decideApplicant", "reviewResident", "restrictResident", "dismissResident" })
            {
                var rec = JsonUtility.FromJson<GameRecord>(json);
                rec.commands.Add(new SavedCommand { day = rec.day, kind = kind, a = "resident-1" });
                Assert.IsTrue(SaveStore.HasRetiredCommand(rec), kind);
                File.WriteAllText(SaveStore.SavePath, JsonUtility.ToJson(rec));
                Assert.IsNull(SaveStore.Load(), kind + ": yüklenmez");
            }
            string retired = File.ReadAllText(SaveStore.SavePath);
            var fresh = NewGame(27);
            Assert.IsTrue(fresh.PrintShift().Ok);
            SaveStore.Write(fresh.Record);
            Assert.AreEqual(retired, File.ReadAllText(SaveStore.SavePath + ".retired.backup"), "yeni oyun yazılırken arşivlenir");
            Assert.AreEqual(27u, SaveStore.Load().seed);
        }

        [Test]
        public void StorageOverride_SeparatesWriteAndDeleteFromOtherSessions()
        {
            var first = NewGame(13);
            Assert.IsTrue(first.PrintShift().Ok);
            SaveStore.Write(first.Record);
            string firstPath = SaveStore.SavePath;
            string firstJson = File.ReadAllText(firstPath);

            string secondDirectory = Path.Combine(directory, "second-session");
            SessionState.SetString(StorageKey, secondDirectory);
            var second = NewGame(27);
            Assert.IsTrue(second.PrintShift().Ok);
            SaveStore.Write(second.Record);
            Assert.AreEqual(secondDirectory, SaveStore.StorageDirectory);
            Assert.AreEqual(27u, SaveStore.Load().seed);
            SaveStore.Delete();
            Assert.IsFalse(File.Exists(SaveStore.SavePath));
            Assert.AreEqual(firstJson, File.ReadAllText(firstPath));

            SessionState.EraseString(StorageKey);
            Assert.AreEqual(Application.persistentDataPath, SaveStore.StorageDirectory);
        }
    }
}
