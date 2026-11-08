using System;
using System.IO;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Serbest kart yerleşimi yalnız sunumdur; simülasyondan ve gerçek oyuncu klasöründen ayrıdır.</summary>
    public sealed class PlanningBoardLayoutTests
    {
        const string StorageKey = "Corrigible.TestStorage";
        string previousStorage, directory;

        [SetUp]
        public void IsolateStorage()
        {
            previousStorage = SessionState.GetString(StorageKey, "");
            directory = Path.Combine(Path.GetTempPath(), "Corrigible-BoardLayout-" + Guid.NewGuid().ToString("N"));
            SessionState.SetString(StorageKey, directory);
        }

        [TearDown]
        public void RestoreStorage()
        {
            if (string.IsNullOrEmpty(previousStorage)) SessionState.EraseString(StorageKey);
            else SessionState.SetString(StorageKey, previousStorage);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void DiskRoundTrip_KeepsFreeCoordinatesAndEquipmentOffset_WithoutChangingTheSimulation()
        {
            var sim = Simulation.NewGame(79);
            int commands = sim.Record.commands.Count;
            uint rng = sim.S.Rng.State;
            var layout = new PlanningBoardLayout();
            layout.Entries.Add(new PlanningBoardPlacement { Key = "0:sakin-1", Position = new Vector2(125.5f, -83.75f) });
            layout.Entries.Add(new PlanningBoardPlacement { Key = "1:esya-1", OwnerId = "sakin-1", Position = new Vector2(38.25f, -41.5f) });

            PlanningBoardLayoutStore.Save(sim.Record, layout);
            Assert.AreEqual(Path.Combine(directory, PlanningBoardLayoutStore.FileName), PlanningBoardLayoutStore.FilePath);
            Assert.IsTrue(File.Exists(PlanningBoardLayoutStore.FilePath));
            Assert.IsFalse(File.Exists(PlanningBoardLayoutStore.FilePath + ".writing"));
            var loaded = PlanningBoardLayoutStore.Load(sim.Record);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(sim.Record.seed, loaded.Seed);
            Assert.AreEqual(2, loaded.Entries.Count);
            Assert.AreEqual(layout.Entries[0].Position, loaded.Entries[0].Position, "Kartın kesirli konumu ızgaraya yuvarlanmamalı.");
            Assert.AreEqual("sakin-1", loaded.Entries[1].OwnerId);
            Assert.AreEqual(layout.Entries[1].Position, loaded.Entries[1].Position, "Bağlı eşyanın kişi üzerindeki görsel ofseti korunmalı.");
            Assert.IsNull(PlanningBoardLayoutStore.Load(Simulation.NewGame(80).Record), "Başka harekâtın konumları yüklenmemeli.");
            Assert.AreEqual(commands, sim.Record.commands.Count, "Yerleşim yeni oyuncu komutu üretmemeli.");
            Assert.AreEqual(rng, sim.S.Rng.State, "Dosya okuma/yazma oyun rastgeleliğini tüketmemeli.");
        }

        /// <summary>
        /// Sürüm 2 (PLAN §1.7, eleştiri 14): fiş yerleri pano mm ve açıyla, anahtar <c>production:&lt;tür&gt;:&lt;k&gt;</c>; depoya geri alınan
        /// fişin yeri silinir, sonrakiler birer sıra kayar; eğitim konusu saklanır. Diskten dönüşte hepsi aynı, simülasyon değişmez.
        /// </summary>
        [Test]
        public void V2RoundTrip_KeepsChipPosesAnglesAndTrainingTopic_AfterUnstageCompaction()
        {
            var sim = Simulation.NewGame(79);
            int commands = sim.Record.commands.Count;
            uint rng = sim.S.Rng.State;

            var ui = new BoardUiState { TrainingTopic = "cook" };
            ui.SetChipPose(BoardUiState.ChipId("metal", 0), new Vector3(612.25f, 318.5f, -7.5f));
            ui.SetChipPose(BoardUiState.ChipId("metal", 1), new Vector3(655.75f, 330.125f, 4.25f));
            ui.SetChipPose(BoardUiState.ChipId("metal", 2), new Vector3(701.5f, 322f, 11f));
            ui.SetChipPose(BoardUiState.ChipId("devre", 0), new Vector3(780.5f, 341.25f, -2f));
            // metal:1 depoya geri alındı (board_stage 3 → 2): metal:2'nin yeri metal:1'e kayar
            ui.RemoveChip("metal", 1, 3);
            Assert.AreEqual(3, ui.ChipPoses.Count);
            Assert.AreEqual(new Vector3(701.5f, 322f, 11f), ui.ChipPose(BoardUiState.ChipId("metal", 1)), "Sonraki fiş yerinde kalmalı.");
            Assert.IsNull(ui.ChipPose(BoardUiState.ChipId("metal", 2)));

            var layout = new PlanningBoardLayout();
            layout.Entries.Add(new PlanningBoardPlacement { Key = "0:sakin-1", Position = new Vector2(1, 2) });   // sürüm 1 artığı
            PlanningBoardLayoutStore.Capture(ui, layout);
            Assert.AreEqual(3, layout.Entries.Count, "Yerleşim yalnız fiş yerlerinden kurulur.");
            Assert.IsTrue(layout.Entries.TrueForAll(e => e.Key.StartsWith("production:", StringComparison.Ordinal)));
            PlanningBoardLayoutStore.Save(sim.Record, layout);
            StringAssert.Contains("\"Version\":" + PlanningBoardLayoutStore.CurrentVersion, File.ReadAllText(PlanningBoardLayoutStore.FilePath));

            var loaded = PlanningBoardLayoutStore.Load(sim.Record);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(PlanningBoardLayoutStore.CurrentVersion, loaded.Version);
            Assert.AreEqual("cook", loaded.TrainingTopic);
            var back = new BoardUiState { SelectedId = "sakin-1" };
            PlanningBoardLayoutStore.Apply(loaded, back);
            Assert.IsNull(back.SelectedId, "Seçim dosyaya girmez; yükleme sıfırlar.");
            Assert.AreEqual("cook", back.TrainingTopic);
            CollectionAssert.AreEquivalent(ui.ChipPoses, back.ChipPoses, "Fiş yeri ve açısı kesirleriyle korunmalı.");
            Assert.AreEqual(new Vector3(612.25f, 318.5f, -7.5f), back.ChipPose("chip:metal:0"));
            Assert.AreEqual(new Vector3(780.5f, 341.25f, -2f), back.ChipPose("chip:devre:0"));

            Assert.IsNull(PlanningBoardLayoutStore.Load(Simulation.NewGame(80).Record), "Başka harekâtın yerleşimi yüklenmemeli.");
            Assert.AreEqual(commands, sim.Record.commands.Count, "Yerleşim komut üretmemeli.");
            Assert.AreEqual(rng, sim.S.Rng.State, "Yerleşim oyun rastgeleliğini tüketmemeli.");
        }

        /// <summary>
        /// Sürüm 1 dosyası (sürüm alanı yok): tohum denetimi kalır; tuval birimli <c>production:*</c> girdileri okunmaz; eğitim konusu
        /// yoktur ("tek"). Geçersiz konu ve bilinmeyen fiş anahtarı da sunuma girmez.
        /// </summary>
        [Test]
        public void V1File_IgnoresCanvasProductionEntries_AndKeepsSeedCheck()
        {
            var record = Simulation.NewGame(79).Record;
            string seed = record.seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Directory.CreateDirectory(directory);
            File.WriteAllText(PlanningBoardLayoutStore.FilePath,
                "{\"Seed\":" + seed + ",\"Entries\":[" +
                "{\"Key\":\"production:metal\",\"OwnerId\":\"\",\"Position\":{\"x\":540.0,\"y\":345.0}}," +
                "{\"Key\":\"production:devre:0\",\"OwnerId\":\"\",\"Position\":{\"x\":-120.0,\"y\":80.0}}," +
                "{\"Key\":\"0:sakin-1\",\"OwnerId\":\"\",\"Position\":{\"x\":12.5,\"y\":-3.0}}]}");

            var loaded = PlanningBoardLayoutStore.Load(record);
            Assert.IsNotNull(loaded, "Tohumu tutan sürüm 1 dosyası okunur.");
            Assert.AreEqual(PlanningBoardLayoutStore.CurrentVersion, loaded.Version, "Okunan dosya bellekte güncel sürüme geçer.");
            Assert.AreEqual(1, loaded.Entries.Count);
            Assert.AreEqual("0:sakin-1", loaded.Entries[0].Key);
            Assert.IsNull(loaded.TrainingTopic);
            var ui = new BoardUiState { TrainingTopic = "emp" };
            PlanningBoardLayoutStore.Apply(loaded, ui);
            Assert.AreEqual("tek", ui.TrainingTopic);
            Assert.IsEmpty(ui.ChipPoses, "Tuval birimli eski fiş yerleri pano mm sayılmamalı.");

            File.WriteAllText(PlanningBoardLayoutStore.FilePath, "{\"Seed\":" + (record.seed + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"Entries\":[]}");
            Assert.IsNull(PlanningBoardLayoutStore.Load(record), "Başka tohumun sürüm 1 dosyası yüklenmemeli.");

            var odd = new PlanningBoardLayout { Version = PlanningBoardLayoutStore.CurrentVersion, TrainingTopic = "uçuş" };
            odd.Entries.Add(new PlanningBoardPlacement { Key = "production:altin:0", Position = new Vector2(500, 300) });
            odd.Entries.Add(new PlanningBoardPlacement { Key = "production:metal:x", Position = new Vector2(500, 300) });
            odd.Entries.Add(new PlanningBoardPlacement { Key = "production:kumas:3", Position = new Vector2(600.5f, 310), Angle = 9 });
            PlanningBoardLayoutStore.Apply(odd, ui);
            Assert.AreEqual("tek", ui.TrainingTopic);
            Assert.AreEqual(1, ui.ChipPoses.Count);
            Assert.AreEqual(new Vector3(600.5f, 310, 9), ui.ChipPose("chip:kumas:3"));
        }

        /// <summary>
        /// Sürüm 3 (Aşama 5 · PLAN §1.15, E22): defter açılımı, formdaki tarif kartının yeri (<c>recipe:&lt;iş emri&gt;</c>) ve yeni ürünün
        /// durumu diskten aynı döner. Sürüm 2 dosyası hatasız okunur ve <b>fiş yerleri ile eğitim konusu korunur</b> (eleştiri 1); yeni
        /// alanlar varsayılanla gelir. Sürüm 1'in tuval birimli fiş girdileri yine atılır. Simülasyon değişmez.
        /// </summary>
        [Test]
        public void Layout_V3_RoundTrip_V2Compatible()
        {
            Assert.AreEqual(3, PlanningBoardLayoutStore.CurrentVersion);
            var sim = Simulation.NewGame(79);
            int commands = sim.Record.commands.Count;
            uint rng = sim.S.Rng.State;
            string seed = sim.Record.seed.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // v3 gidiş-dönüş
            var ui = new BoardUiState { TrainingTopic = "craft", BookSpread = 2 };
            ui.SetChipPose(BoardUiState.ChipId("metal", 0), new Vector3(612.25f, 318.5f, -7.5f));
            ui.SetChipPose(BoardUiState.ChipId("kumas", 0), new Vector3(668.5f, 331.75f, 6f));
            ui.SetRecipePose("is-3", new Vector3(520.5f, 330.25f, -6f));
            ui.SetRecipePose("is-12", new Vector3(498f, 326f, 2.5f));
            ui.NewItem = new NewItemState { Day = 4, Id = "esya-12", Slid = true, Touched = false };
            var layout = new PlanningBoardLayout();
            PlanningBoardLayoutStore.Capture(ui, layout);
            CollectionAssert.AreEqual(new[] { "production:kumas:0", "production:metal:0", "recipe:is-12", "recipe:is-3" },
                layout.Entries.ConvertAll(e => e.Key), "Önce fişler, sonra tarif kartları; ikisi de anahtar sırasıyla.");
            Assert.AreEqual(2, layout.BookSpread);
            Assert.AreEqual("esya-12", layout.NewItem.Id);
            PlanningBoardLayoutStore.Save(sim.Record, layout);
            string json = File.ReadAllText(PlanningBoardLayoutStore.FilePath);
            StringAssert.Contains("\"Version\":3", json);
            StringAssert.Contains("\"BookSpread\":2", json);

            var back = new BoardUiState { SelectedId = "sakin-1" };
            PlanningBoardLayoutStore.Apply(PlanningBoardLayoutStore.Load(sim.Record), back);
            Assert.IsNull(back.SelectedId);
            Assert.AreEqual("craft", back.TrainingTopic);
            Assert.AreEqual(2, back.BookSpread);
            CollectionAssert.AreEquivalent(ui.ChipPoses, back.ChipPoses);
            CollectionAssert.AreEquivalent(ui.RecipePoses, back.RecipePoses);
            Assert.AreEqual(new Vector3(520.5f, 330.25f, -6f), back.RecipePose("is-3"), "Tarif kartının serbest yeri kesirleriyle.");
            Assert.IsNotNull(back.NewItem);
            Assert.AreEqual(4, back.NewItem.Day);
            Assert.AreEqual("esya-12", back.NewItem.Id);
            Assert.IsTrue(back.NewItem.Slid);
            Assert.IsFalse(back.NewItem.Touched);
            Assert.AreNotSame(ui.NewItem, back.NewItem);

            // yeni ürün yoksa dosyada boş kalır ve geri yüklenmez; açılım 0–2'ye kırpılır
            var none = new BoardUiState { BookSpread = 7 };
            var noneLayout = new PlanningBoardLayout();
            PlanningBoardLayoutStore.Capture(none, noneLayout);
            Assert.AreEqual(2, noneLayout.BookSpread);
            Assert.IsNull(noneLayout.NewItem);
            PlanningBoardLayoutStore.Save(sim.Record, noneLayout);
            var noneBack = new BoardUiState { NewItem = new NewItemState { Id = "esya-1" } };
            PlanningBoardLayoutStore.Apply(PlanningBoardLayoutStore.Load(sim.Record), noneBack);
            Assert.IsNull(noneBack.NewItem, "Kimliksiz yeni ürün yok sayılır.");
            Assert.AreEqual(2, noneBack.BookSpread);
            Assert.IsEmpty(noneBack.RecipePoses);
            var odd = new PlanningBoardLayout { Version = 3, BookSpread = -4 };
            odd.Entries.Add(new PlanningBoardPlacement { Key = "recipe:", Position = new Vector2(1, 2) });
            PlanningBoardLayoutStore.Apply(odd, noneBack);
            Assert.AreEqual(0, noneBack.BookSpread);
            Assert.IsEmpty(noneBack.RecipePoses, "Kimliksiz tarif anahtarı okunmaz.");

            // v2 dosyası: fiş yerleri ve konu korunur, yeni alanlar varsayılan
            Directory.CreateDirectory(directory);
            File.WriteAllText(PlanningBoardLayoutStore.FilePath,
                "{\"Version\":2,\"Seed\":" + seed + ",\"TrainingTopic\":\"cook\",\"Entries\":[" +
                "{\"Key\":\"production:metal:0\",\"OwnerId\":\"\",\"Position\":{\"x\":612.5,\"y\":318.0},\"Angle\":-7.5}," +
                "{\"Key\":\"production:devre:1\",\"OwnerId\":\"\",\"Position\":{\"x\":742.25,\"y\":326.0},\"Angle\":3.0}]}");
            var v2 = PlanningBoardLayoutStore.Load(sim.Record);
            Assert.IsNotNull(v2, "Sürüm 2 dosyası hatasız okunur.");
            Assert.AreEqual(3, v2.Version, "Bellekte sürüm 3.");
            Assert.AreEqual(2, v2.Entries.Count, "Sürüm 2'nin fiş yerleri silinmez.");
            Assert.AreEqual("cook", v2.TrainingTopic, "Sürüm 2'nin eğitim konusu silinmez.");
            var v2ui = new BoardUiState { BookSpread = 1 };
            v2ui.SetRecipePose("is-1", Vector3.one);
            PlanningBoardLayoutStore.Apply(v2, v2ui);
            Assert.AreEqual("cook", v2ui.TrainingTopic);
            Assert.AreEqual(new Vector3(612.5f, 318f, -7.5f), v2ui.ChipPose("chip:metal:0"));
            Assert.AreEqual(new Vector3(742.25f, 326f, 3f), v2ui.ChipPose("chip:devre:1"));
            Assert.AreEqual(0, v2ui.BookSpread);
            Assert.IsEmpty(v2ui.RecipePoses);
            Assert.IsNull(v2ui.NewItem);

            // v1 dosyası: tuval birimli fiş girdileri ve konu yine atılır
            File.WriteAllText(PlanningBoardLayoutStore.FilePath,
                "{\"Seed\":" + seed + ",\"TrainingTopic\":\"cook\",\"Entries\":[" +
                "{\"Key\":\"production:metal:0\",\"OwnerId\":\"\",\"Position\":{\"x\":-120.0,\"y\":80.0}}]}");
            var v1 = PlanningBoardLayoutStore.Load(sim.Record);
            Assert.AreEqual(3, v1.Version);
            Assert.IsEmpty(v1.Entries);
            Assert.IsNull(v1.TrainingTopic);

            Assert.AreEqual(commands, sim.Record.commands.Count, "Yerleşim komut üretmez.");
            Assert.AreEqual(rng, sim.S.Rng.State, "Yerleşim RNG çekmez.");
        }

        [Test]
        public void StorageOverride_DoesNotReadOrOverwriteAnotherSessionsLayout()
        {
            var record = Simulation.NewGame(79).Record;
            var first = new PlanningBoardLayout();
            first.Entries.Add(new PlanningBoardPlacement { Key = "0:sakin-1", Position = new Vector2(11.5f, 24.25f) });
            PlanningBoardLayoutStore.Save(record, first);
            string firstFile = PlanningBoardLayoutStore.FilePath;
            string firstJson = File.ReadAllText(firstFile);

            SessionState.SetString(StorageKey, Path.Combine(directory, "second-session"));
            Assert.IsNull(PlanningBoardLayoutStore.Load(record));
            var second = new PlanningBoardLayout();
            second.Entries.Add(new PlanningBoardPlacement { Key = "0:sakin-1", Position = new Vector2(-91.25f, 14.5f) });
            PlanningBoardLayoutStore.Save(record, second);
            Assert.AreNotEqual(firstFile, PlanningBoardLayoutStore.FilePath);
            Assert.AreEqual(second.Entries[0].Position, PlanningBoardLayoutStore.Load(record).Entries[0].Position);
            Assert.AreEqual(firstJson, File.ReadAllText(firstFile));

            SessionState.SetString(StorageKey, directory);
            Assert.AreEqual(first.Entries[0].Position, PlanningBoardLayoutStore.Load(record).Entries[0].Position);
        }
    }
}
