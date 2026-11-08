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
        {
            planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = 1
        });

        static void Close(Simulation sim)
        {
            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
        }

        [Test]
        public void DiskRoundTrip_RestoresLockedReceiptProductionTrainingAndApplicant_ThenContinuesIdentically()
        {
            var sim = NewGame();
            var trainee = sim.Board.People.Single(p => p.Job == "rest");
            Assert.IsTrue(sim.TrainResident(trainee.Id, "craft").Ok);
            Assert.IsTrue(sim.QueueProduction("emp_cell").Ok);
            foreach (var input in sim.BoardRecipes.Single(r => r.Id == "emp_cell").Inputs)
                Assert.IsTrue(sim.ReserveProductionMaterial(input.Kind, input.Quantity).Ok);
            Close(sim);
            Assert.IsNotNull(sim.Admission.Candidate);
            Assert.IsTrue(sim.InspectApplicant("papers").Ok);
            Assert.IsTrue(sim.PrintShift().Ok);
            sim.Record.day = sim.S.Day;

            SaveStore.Write(sim.Record);
            Assert.IsFalse(File.Exists(SaveStore.SavePath + ".writing"));
            var loaded = SaveStore.Load();
            Assert.IsNotNull(loaded);
            var restored = Replay.Restore(loaded);
            Assert.AreEqual(JsonUtility.ToJson(sim.Board), JsonUtility.ToJson(restored.Board));
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);

            string receipt = sim.Board.ReceiptId;
            Assert.IsTrue(sim.FileShift(receipt).Ok);
            Assert.IsTrue(restored.FileShift(receipt).Ok);
            Assert.AreEqual(JsonUtility.ToJson(sim.Board), JsonUtility.ToJson(restored.Board));
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(sim.Shelter.Supplies, restored.Shelter.Supplies);
            Assert.AreEqual(sim.S.Day, restored.S.Day);
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
            var invalid = new GameRecord { day = 2, balance = new Balance(), commands = null };
            File.WriteAllText(SaveStore.SavePath, JsonUtility.ToJson(invalid));
            Assert.IsNull(SaveStore.Load());
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
