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
    /// <summary>Rehberli ilk vardiyalar: adımların sırası, bekledikleri eylem, atlanan adımlar, atlama ve sahne bağlantıları.</summary>
    public class TutorialFlowTests
    {
        [Test]
        public void Steps_HaveUniqueIds_TitlesAndBodies()
        {
            CollectionAssert.AllItemsAreUnique(TutorialFlow.Steps.Select(s => s.Id));
            Assert.IsTrue(TutorialFlow.Steps.All(s => !string.IsNullOrEmpty(s.Title) && !string.IsNullOrEmpty(s.Body)));
            Assert.IsTrue(TutorialFlow.Steps.All(s => s.Info || s.Done != null), "bekleme adımının koşulu var");
            Assert.IsTrue(TutorialFlow.Steps.First().Info && TutorialFlow.Steps.Last().Info, "açılış ve kapanış TAMAM ile geçer");
        }

        [Test]
        public void Flow_WalksTheFirstShift_InOrder()
        {
            var f = new TutorialFlow();
            var c = new TutorialContext();
            void Expect(string id) => Assert.AreEqual(id, f.Current?.Id);
            Expect("intro");
            Assert.IsFalse(f.Update(c), "bilgi adımı TAMAM bekler");
            f.Acknowledge(); Expect("slip");
            c.SlipOpen = true; f.Update(c); Expect("order");
            c.RaidOrdered = true; f.Update(c); Expect("raid");
            c.RaidLoaded = true; f.Update(c); Expect("raid");
            c.RaidOver = true; f.Update(c); Expect("result");
            c.RaidLoaded = false; c.RaidOrdered = false; c.RaidOver = false; c.Raids = 1;
            f.Update(c); Expect("result");
            c.ReportOpen = true; f.Update(c); Expect("report");
            c.ReportOpen = false; f.Update(c); Expect("shelter");
            c.ShelterPose = true; f.Update(c); Expect("repair");
            c.WorkshopLevel = 1; f.Update(c); Expect("staff");
            c.WorkshopWorkers = 1; f.Update(c); Expect("shift");
            c.Day = 1; f.Update(c); Expect("shiftReport");
            c.ShiftReportOpen = true; f.Update(c); Expect("shiftReport");
            c.ShiftReportOpen = false; f.Update(c); Expect("go");
            f.Acknowledge();
            Assert.IsTrue(f.Finished);
            Assert.IsNull(f.Current);
        }

        [Test]
        public void Flow_PassesStepsThePlayerAlreadyDid()
        {
            var f = new TutorialFlow();
            f.Acknowledge();
            var c = new TutorialContext { RaidOrdered = true, RaidLoaded = true };
            Assert.IsTrue(f.Update(c));
            Assert.AreEqual("raid", f.Current.Id, "fiş ve emir zaten verildi");
            // Baskın bitmeden sahneden çıkıldıysa (geri çekilme sonrası rapor) adım yine geçer
            c.RaidLoaded = false; c.RaidOrdered = false; c.Raids = 1;
            f.Update(c);
            Assert.AreEqual("result", f.Current.Id);
        }

        [Test]
        public void ReportSteps_WaitForTheReportToOpenAndClose()
        {
            var f = new TutorialFlow();
            f.Acknowledge();
            var c = new TutorialContext { RaidOrdered = true, RaidLoaded = true, RaidOver = true };
            f.Update(c);
            Assert.AreEqual("result", f.Current.Id);
            c.RaidLoaded = false; c.RaidOrdered = false;
            f.Update(c);
            Assert.AreEqual("result", f.Current.Id, "rapor açılmadan kapanmış sayılmaz");
            c.ReportOpen = true; f.Update(c);
            c.ReportOpen = false; f.Update(c);
            Assert.AreEqual("shelter", f.Current.Id);
        }

        [Test]
        public void Skip_FinishesAtOnce()
        {
            var f = new TutorialFlow();
            f.Skip();
            Assert.IsTrue(f.Finished);
            Assert.IsFalse(f.Update(new TutorialContext { SlipOpen = true }));
        }

        [Test]
        public void DeskGameScene_TutorialIsWiredOutsideTheRoom_AboveTheRaid()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var tutorial = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SiginakTutorial>(true)).Single();
                Assert.IsNull(tutorial.transform.parent, "odanın kökünün dışında: baskında da çalışır");
                var so = new SerializedObject(tutorial);
                foreach (var name in new[] { "game", "briefing", "map", "shift", "rig", "launcher", "note" })
                    Assert.IsNotNull(so.FindProperty(name).objectReferenceValue, name);
                var canvas = tutorial.GetComponent<Canvas>();
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
                Assert.Greater(canvas.sortingOrder, 50, "baskın HUD'u ve paneller (20, 50) üstünde");
                var note = new SerializedObject(tutorial.GetComponentInChildren<TutorialNoteView>(true));
                foreach (var name in new[] { "card", "anchor", "group", "head", "title", "body", "okLabel", "skipLabel", "ok", "skip" })
                    Assert.IsNotNull(note.FindProperty(name).objectReferenceValue, name);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
