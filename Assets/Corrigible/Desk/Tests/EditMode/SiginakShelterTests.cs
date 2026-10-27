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
            Assert.AreEqual(RoomCondition.Working, Room(sim, ShelterLogic.Generator).Condition);
            Assert.AreEqual(RoomCondition.Damaged, Room(sim, ShelterLogic.Workshop).Condition);
            Assert.AreEqual(RoomCondition.Damaged, Room(sim, ShelterLogic.Infirmary).Condition);
            Assert.IsTrue(rooms.Single(r => r.Id == ShelterLogic.Living).Selected);
            Assert.AreEqual(sim.B.shelterWorkersPerLevel, Room(sim, ShelterLogic.Living).Capacity, "kapasite = yuva sayısı");
        }

        [Test]
        public void V014_EnergyDeficit_DarkensConsumersAndNotesGenerator()
        {
            var sim = NewLiberation();
            SiginakFixtures.ApplyShelterV014(sim);
            Assert.AreEqual(RoomCondition.NoPower, Room(sim, ShelterLogic.Workshop).Condition, "enerji açığı odayı karartır");
            Assert.AreEqual(RoomCondition.Damaged, Room(sim, ShelterLogic.Infirmary).Condition);
            Assert.IsTrue(Room(sim, ShelterLogic.Workshop).Top, "atölye SV3: en üst seviye");
            StringAssert.Contains("3/3", Room(sim, ShelterLogic.Workshop).Line, "K8: kartta 3/3");

            var d = ShelterViewBuilder.Board(sim, ShelterLogic.Generator);
            Assert.AreEqual(Loc.F("{0} için {1} enerji eksik: {2} personel ata ya da geliştir.", ShelterLogic.RoomName(ShelterLogic.Workshop), "2", 2), d.Note);
            Assert.AreEqual(SlipButtonState.Normal, d.Action.State, "62 fon / 19 malzeme jeneratör geliştirmeye yeter");
            Assert.AreEqual(Loc.T("GELİŞTİR"), d.Action.Label);
            Assert.IsTrue(d.ReadinessOk);
            Assert.AreEqual(Loc.F("V.{0:000}", sim.S.Day + 1), d.Shift);
            StringAssert.Contains(DeskFormat.Signed(-2, 0), d.Summary[1].value, "ENERJİ satırında açık kırmızı");
        }

        [Test]
        public void Board_MissingMaterials_ShortWithNumbers()
        {
            var sim = NewLiberation();
            sim.Shelter.Materials = 5;
            var d = ShelterViewBuilder.Board(sim, ShelterLogic.Infirmary);
            var cost = ShelterLogic.UpgradeCost(sim.Shelter, ShelterLogic.Infirmary, sim.B);
            Assert.AreEqual(Loc.T("ONAR"), d.Action.Label, "hasarlı oda: ONAR");
            Assert.AreEqual(SlipButtonState.Short, d.Action.State);
            Assert.AreEqual(Loc.T("EKSİK"), d.Action.Stamp);
            Assert.AreEqual(Loc.F("Eksik: {0}", Loc.F("{0} malzeme", DeskFormat.Number(cost.Materials - 5, 0))), d.Readiness);
            Assert.AreEqual(Loc.T("Hasarlı: onarılana kadar çalışmaz, personel alamaz."), d.Note);
            Assert.AreEqual(SlipButtonState.Disabled, d.Plus.State, "hasarlı oda personel alamaz");
        }

        [Test]
        public void Board_TopLevelAndFullRoom()
        {
            var sim = NewLiberation();
            var d = ShelterViewBuilder.Board(sim, ShelterLogic.Living);
            Assert.AreEqual(SlipButtonState.Short, d.Plus.State, "yaşam alanı 2/2: DOLU");
            Assert.AreEqual(Loc.T("DOLU"), d.Plus.Stamp);
            Assert.AreEqual(SlipButtonState.Normal, d.Minus.State);

            sim.Shelter.Room(ShelterLogic.Living).Level = sim.B.shelterMaxRoomLevel;
            var top = ShelterViewBuilder.Board(sim, ShelterLogic.Living);
            Assert.AreEqual(Loc.T("EN ÜST SEVİYE"), top.Action.Label);
            Assert.AreEqual(SlipButtonState.Disabled, top.Action.State);
        }

        [Test]
        public void Upgrade_StampsRoomForOneShift()
        {
            var sim = NewLiberation();
            Assert.IsFalse(Room(sim, ShelterLogic.Generator).Fresh);
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Generator).Ok);
            Assert.IsTrue(Room(sim, ShelterLogic.Generator).Fresh, "EMİR UYGULANDI damgası");
            Assert.IsFalse(Room(sim, ShelterLogic.Living).Fresh);
            sim.Step();
            Assert.IsFalse(Room(sim, ShelterLogic.Generator).Fresh, "bir vardiya sonra kalkar");
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
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
