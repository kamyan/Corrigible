using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 6: oda hâlleri, pano içeriği (not, maliyet, düğme hâlleri) ve sahne bağlantıları.</summary>
    public class SiginakShelterTests
    {
        static Simulation NewLiberation(double fon = 100) => Simulation.NewGame(731, new Balance { startFon = fon });

        static ShelterRoomData Room(Simulation sim, string id) => ShelterViewBuilder.Rooms(sim, id).Single(r => r.Id == id);

        [Test]
        public void Rooms_NewGame_WorkshopAndInfirmaryStartDamaged()
        {
            var sim = NewLiberation();
            var rooms = ShelterViewBuilder.Rooms(sim, ShelterLogic.Living);
            CollectionAssert.AreEqual(ShelterViewBuilder.Order, rooms.Select(r => r.Id).ToArray());
            Assert.AreEqual(RoomCondition.Working, Room(sim, ShelterLogic.Living).Condition);
            Assert.AreEqual(RoomCondition.Working, Room(sim, ShelterLogic.Radio).Condition);
            Assert.AreEqual(RoomCondition.Damaged, Room(sim, ShelterLogic.Workshop).Condition);
            Assert.AreEqual(RoomCondition.Damaged, Room(sim, ShelterLogic.Infirmary).Condition);
            Assert.IsTrue(rooms.Single(r => r.Id == ShelterLogic.Living).Selected);
            Assert.AreEqual(sim.B.shelterWorkersPerLevel, Room(sim, ShelterLogic.Living).Capacity, "kapasite = yuva sayısı");
        }

        [Test]
        public void V014_RadioWithoutStaff_NotesAlarm_AndSummaryShowsSquadAndPlates()
        {
            var sim = NewLiberation();
            SiginakFixtures.ApplyShelterV014(sim);
            Assert.AreEqual(RoomCondition.Working, Room(sim, ShelterLogic.Workshop).Condition, "malzeme var, depo boş: döküm sürer");
            Assert.AreEqual(RoomCondition.Damaged, Room(sim, ShelterLogic.Infirmary).Condition);
            Assert.IsTrue(Room(sim, ShelterLogic.Workshop).Top, "atölye SV3: en üst seviye");
            StringAssert.Contains("3/3", Room(sim, ShelterLogic.Workshop).Line, "K8: kartta 3/3");

            var d = ShelterViewBuilder.Board(sim, ShelterLogic.Radio);
            Assert.AreEqual(Loc.F("Görevli yok: alarm her vardiya +{0} yükselir.", "1"), d.Note);
            // Seviye 2 iki özgür bölge ister; fon ve malzeme yetse de düğme kilitli
            Assert.AreEqual(SlipButtonState.Disabled, d.Action.State);
            Assert.AreEqual(sim.ShelterLevelLock(ShelterLogic.Radio), d.ActionNote, "kilit gerekçesi düğmenin altında");
            Assert.AreEqual(Loc.F("V.{0:000}", sim.S.Day + 1), d.Shift);
            CollectionAssert.AreEqual(new[] { "T", "E", "A" }, d.Squad.Select(m => m.Letter).ToArray(), "uzmanlar diskle");
            Assert.AreEqual(Loc.F("{0}/{1}", 3, 3), d.Squad[0].Hp);
            StringAssert.Contains("0/3", d.Plates);
            // S3: asıl soru erzak kaç vardiya yeter (22 ÷ 1,4 = 15)
            Assert.AreEqual(Loc.T("ERZAK YETER"), d.SuppliesLabel);
            Assert.AreEqual(Loc.F("{0} VARDİYA", 15), d.SuppliesBig);
            Assert.IsFalse(d.SuppliesLow);
            Assert.IsNull(d.SuppliesStamp);
            Assert.AreEqual(Loc.F("ERZAK · {0} VARDİYA", 15), ShelterViewBuilder.Store(sim).Slate);
        }

        /// <summary>S3 › özet: n = ⌊erzak ÷ |fark|⌋; fark ≥ 0 → ERZAK ARTIYOR; 3 ve altı kırmızı + AZ KALDI.</summary>
        [Test]
        public void ShiftsLeft_FloorsAndFlagsLowAndRising()
        {
            Assert.AreEqual(15, ShelterViewBuilder.ShiftsLeft(22, -1.4));
            Assert.AreEqual(17, ShelterViewBuilder.ShiftsLeft(24, -1.4));
            Assert.AreEqual(3, ShelterViewBuilder.ShiftsLeft(4.2, -1.4), "tam bölünen: 3");
            Assert.AreEqual(0, ShelterViewBuilder.ShiftsLeft(0, -1.4));
            Assert.IsNull(ShelterViewBuilder.ShiftsLeft(10, 0), "fark ≥ 0: artıyor");
            Assert.IsNull(ShelterViewBuilder.ShiftsLeft(10, 0.7));

            var sim = NewLiberation();
            SiginakFixtures.ApplyShelterV014(sim);
            sim.Shelter.Supplies = 4;
            var low = ShelterViewBuilder.Board(sim, ShelterLogic.Living);
            Assert.AreEqual(Loc.F("{0} VARDİYA", 2), low.SuppliesBig);
            Assert.IsTrue(low.SuppliesLow);
            Assert.AreEqual(Loc.T("AZ KALDI"), low.SuppliesStamp);
            sim.Shelter.Room(ShelterLogic.Living).Workers = 2;
            sim.Shelter.Population = 2;
            var rising = ShelterViewBuilder.Board(sim, ShelterLogic.Living);
            Assert.AreEqual(Loc.T("ERZAK"), rising.SuppliesLabel);
            Assert.AreEqual(Loc.T("ARTIYOR"), rising.SuppliesBig);
            Assert.AreEqual(Loc.T("ERZAK ARTIYOR"), ShelterViewBuilder.Store(sim).Slate);
        }

        /// <summary>DEPO (S3, Soru 6 ara kararı a): 10 sandık = erzak sınırı, 5 kova = malzeme deposu, plaka rafı 3 yuva.</summary>
        [Test]
        public void Store_CountsCratesBinsAndPlates()
        {
            // V.014: erzak 22/30 → sandık 3: 7 dolu + yarım; malzeme 19/60 → kova 12: 2 kova, sonuncusu 7/12; plaka 0/3
            var d = ShelterViewBuilder.Store(22, 30, 19, 60, 0, 3);
            Assert.AreEqual(3, d.CrateValue, 1e-9);
            Assert.AreEqual(7, d.Crates);
            Assert.IsTrue(d.CratePartial);
            Assert.AreEqual(12, d.BinValue, 1e-9);
            Assert.AreEqual(2, d.Bins);
            Assert.AreEqual(7.0 / 12, d.LastBinFill, 1e-9);
            Assert.AreEqual(0, d.Plates);
            Assert.AreEqual(3, d.PlateCapacity);

            var full = ShelterViewBuilder.Store(30, 30, 60, 60, 3, 3);
            Assert.AreEqual(10, full.Crates);
            Assert.IsFalse(full.CratePartial);
            Assert.AreEqual(5, full.Bins);
            Assert.AreEqual(1, full.LastBinFill, 1e-9);
            Assert.AreEqual(3, full.Plates);

            // Yaşam alanı SV2: sınır 60 → sandık 6; atölye SV1: rafın bir yuvası açık
            var big = ShelterViewBuilder.Store(24, 60, 0, 60, 2, 1);
            Assert.AreEqual(6, big.CrateValue, 1e-9);
            Assert.AreEqual(4, big.Crates);
            Assert.IsFalse(big.CratePartial);
            Assert.AreEqual(0, big.Bins);
            Assert.AreEqual(1, big.PlateCapacity);
            Assert.AreEqual(1, big.Plates, "plaka açık yuvadan fazla gösterilmez");

            var exact = ShelterViewBuilder.Store(24, 30, 12, 60, 0, 0);
            Assert.AreEqual(8, exact.Crates);
            Assert.IsFalse(exact.CratePartial);
            Assert.AreEqual(1, exact.Bins);
            Assert.AreEqual(1, exact.LastBinFill, 1e-9);
        }

        [Test]
        public void Workshop_StopsWhenStorageFullOrNoMaterials()
        {
            var sim = NewLiberation();
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok);
            sim.Shelter.Plates = ShelterLogic.PlateCapacity(sim.Shelter, sim.B);
            Assert.AreEqual(RoomCondition.Stopped, Room(sim, ShelterLogic.Workshop).Condition);
            Assert.AreEqual(Loc.F("Depo dolu: {0} plaka hazır. Kırılan plakanın yerine yenisi dökülür; geliştirirsen depo büyür.", 1),
                ShelterViewBuilder.Board(sim, ShelterLogic.Workshop).Note);
            sim.Shelter.Plates = 0; sim.Shelter.Materials = 1;
            Assert.AreEqual(RoomCondition.Stopped, Room(sim, ShelterLogic.Workshop).Condition);
            Assert.AreEqual(Loc.F("Malzeme yetmiyor: plaka {0} malzeme ister. İkmal baskını malzeme getirir.", "4"), ShelterViewBuilder.Board(sim, ShelterLogic.Workshop).Note);
        }

        [Test]
        public void Board_MissingMaterials_ShortWithNumbers()
        {
            var sim = NewLiberation();
            sim.Shelter.Materials = 5;
            var d = ShelterViewBuilder.Board(sim, ShelterLogic.Infirmary);
            var cost = ShelterLogic.UpgradeCost(sim.Shelter, ShelterLogic.Infirmary, sim.B);
            // S3: yetersiz kaynakta düğme açık kalır, eksik kalem çizik + “yetersiz”; teleks gerekçesi sayılarla
            Assert.AreEqual(SlipButtonState.Short, d.Action.State);
            Assert.AreEqual(Loc.F("{0} · {1} + {2} · yetersiz", Loc.T("ONAR"), Loc.F("{0} fon", DeskFormat.Number(cost.Fon, 0)),
                "<s>" + Loc.F("{0} malzeme", DeskFormat.Number(cost.Materials, 0)) + "</s>"), d.Action.Label, "hasarlı oda: ONAR");
            Assert.AreEqual(TelexReasons.Upgrade(sim, ShelterLogic.Infirmary), d.Action.Hover);
            Assert.IsNull(d.ActionNote);
            Assert.AreEqual(Loc.T("Hasarlı: onarılana kadar çalışmaz, personel alamaz."), d.Note);
            Assert.AreEqual(SlipButtonState.Disabled, d.Plus.State, "hasarlı oda personel alamaz");
        }

        [Test]
        public void Board_TopLevelAndFullRoom()
        {
            var sim = NewLiberation();
            var d = ShelterViewBuilder.Board(sim, ShelterLogic.Living);
            Assert.AreEqual(SlipButtonState.Disabled, d.Plus.State, "yaşam alanı 2/2: oda dolu");
            Assert.AreEqual(Loc.T("Oda dolu"), d.Plus.Hover);
            Assert.AreEqual(SlipButtonState.Normal, d.Minus.State);
            Assert.AreEqual(Loc.F("{0}/{1}", 2, 2), d.Staff);
            Assert.IsNull(d.StaffNote);

            sim.Shelter.Room(ShelterLogic.Living).Level = sim.B.shelterMaxRoomLevel;
            var top = ShelterViewBuilder.Board(sim, ShelterLogic.Living);
            Assert.IsTrue(top.Top, "en üst seviyede düğme yerine EN ÜST SEVİYE damgası");
        }

        /// <summary>S3 kabulü: boşta kimse yokken [+] devre dışı ve “Boşta kimse yok”; görevli yokken [−] devre dışı.</summary>
        [Test]
        public void Board_Stepper_NoIdleAndNoStaff()
        {
            var sim = NewLiberation();
            SiginakFixtures.ApplyShelterV014(sim);
            var radio = sim.Shelter.Room(ShelterLogic.Radio);
            Assert.AreEqual(0, radio.Workers);
            // Boşta herkesi atölyeye ver: telsiz odasına verecek kimse kalmaz
            var workshop = sim.Shelter.Room(ShelterLogic.Workshop);
            workshop.Workers += ShelterLogic.AvailableWorkers(sim.Shelter);
            Assert.AreEqual(0, ShelterLogic.AvailableWorkers(sim.Shelter));
            var d = ShelterViewBuilder.Board(sim, ShelterLogic.Radio);
            Assert.AreEqual(SlipButtonState.Disabled, d.Plus.State);
            Assert.AreEqual(Loc.T("Boşta kimse yok"), d.StaffNote);
            Assert.AreEqual(Loc.T("Boşta kimse yok"), d.Plus.Hover);
            Assert.AreEqual(SlipButtonState.Disabled, d.Minus.State);
            Assert.AreEqual(Loc.T("Görevli yok"), d.Minus.Hover);
        }

        /// <summary>S3 › odaların çıktısı: ranzada yedek, telsizde görevliyle kadran, revirde yatan uzman.</summary>
        [Test]
        public void Rooms_ShowOutputs()
        {
            var sim = NewLiberation();
            SiginakFixtures.ApplyShelterV014(sim);
            Assert.AreEqual(sim.Shelter.Reserves.Count(r => r.Down <= 0), Room(sim, ShelterLogic.Living).Occupants);
            Assert.IsTrue(Room(sim, ShelterLogic.Living).Active, "görevli var: erzak kasası");
            Assert.IsFalse(Room(sim, ShelterLogic.Radio).Active, "telsizde görevli yok");
            sim.Shelter.Room(ShelterLogic.Radio).Workers = 1;
            Assert.IsTrue(Room(sim, ShelterLogic.Radio).Active);
            Assert.AreEqual(0, Room(sim, ShelterLogic.Infirmary).Occupants, "hasarlı revirde hasta yok");
            sim.Shelter.Room(ShelterLogic.Infirmary).Level = 1;
            sim.S.Teams[0].Members[0].Down = 2;
            Assert.AreEqual(1, Room(sim, ShelterLogic.Infirmary).Occupants);
        }

        [Test]
        public void Upgrade_StampsRoomForOneShift()
        {
            var sim = NewLiberation();
            Assert.IsFalse(Room(sim, ShelterLogic.Workshop).Fresh);
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok, "onarım (seviye 1) bölge istemez");
            Assert.IsTrue(Room(sim, ShelterLogic.Workshop).Fresh, "EMİR UYGULANDI damgası");
            Assert.IsFalse(Room(sim, ShelterLogic.Living).Fresh);
            sim.Step();
            Assert.IsFalse(Room(sim, ShelterLogic.Workshop).Fresh, "bir vardiya sonra kalkar");
        }

        [Test]
        public void DeskGameScene_ShelterIsWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var shelter = roots.SelectMany(g => g.GetComponentsInChildren<SiginakShelter>(true)).Single();
                var so = new SerializedObject(shelter);
                foreach (var f in new[] { "game", "surface", "rig", "tray", "board", "shelfTarget" })
                    Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
                Assert.AreEqual(4, so.FindProperty("rooms").arraySize);
                Assert.AreEqual(4, so.FindProperty("roomTargets").arraySize);
                var views = shelter.transform.root.GetComponentsInChildren<RoomView>(true);
                CollectionAssert.AreEquivalent(ShelterViewBuilder.Order, views.Select(v => v.RoomId).ToArray());
                foreach (var v in views)
                {
                    var vo = new SerializedObject(v);
                    Assert.AreEqual(6, vo.FindProperty("pawns").arraySize, v.RoomId);
                    Assert.IsNotNull(vo.FindProperty("lamp").objectReferenceValue, v.RoomId);
                    Assert.IsNotNull(vo.FindProperty("damage").objectReferenceValue, v.RoomId + " hasar grubu");
                }
                foreach (var t in shelter.transform.root.GetComponentsInChildren<DeskTarget>(true).Where(t => t.name == "Target"))
                    Assert.IsFalse(t.GetComponent<Collider>().enabled, "oda hedefleri yalnız raf pozunda açılır");
                Assert.AreEqual(6, new SerializedObject(shelter.transform.root.GetComponentInChildren<IdleTray>(true)).FindProperty("pawns").arraySize);
                var board = shelter.transform.root.GetComponentInChildren<ShelterClipboard>(true);
                Assert.AreEqual(RenderMode.WorldSpace, board.GetComponent<Canvas>().renderMode);
                // Revizyon 2 · S3: panonun bütün alanları, DEPO ve oda çıktıları bağlı
                var bo = new SerializedObject(board);
                for (var it = bo.GetIterator(); it.NextVisible(true);)
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.name != "m_Script")
                        Assert.IsNotNull(it.objectReferenceValue, "pano alanı: " + it.propertyPath);
                Assert.AreEqual(3, bo.FindProperty("squad").arraySize);
                var store = (ShelterStore)so.FindProperty("store").objectReferenceValue;
                Assert.IsNotNull(store, "DEPO");
                var st = new SerializedObject(store);
                Assert.AreEqual(ShelterStoreData.CrateSlots, st.FindProperty("crates").arraySize);
                Assert.AreEqual(ShelterStoreData.BinSlots, st.FindProperty("bins").arraySize);
                Assert.AreEqual(ShelterStoreData.PlateSlots, st.FindProperty("plates").arraySize);
                for (var it = st.GetIterator(); it.NextVisible(true);)
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.name != "m_Script")
                        Assert.IsNotNull(it.objectReferenceValue, "DEPO alanı: " + it.propertyPath);
                var living = views.Single(v => v.RoomId == ShelterLogic.Living);
                Assert.AreEqual(2, new SerializedObject(living).FindProperty("occupants").arraySize, "iki ranzada yedek");
                Assert.AreEqual(1, new SerializedObject(views.Single(v => v.RoomId == ShelterLogic.Infirmary)).FindProperty("occupants").arraySize);
                Assert.Greater(new SerializedObject(views.Single(v => v.RoomId == ShelterLogic.Radio)).FindProperty("activeLights").arraySize, 0);
                Assert.Greater(new SerializedObject(views.Single(v => v.RoomId == ShelterLogic.Workshop)).FindProperty("activeLights").arraySize, 0);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
