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
    /// <summary>Sığınağın kapısı: kâğıdın evreleri (kapalı, açık, sonuç), bedel ve gerekçe, vardiya raporu satırı, sahne bağlantıları.</summary>
    public class SiginakDoorTests
    {
        static Simulation New()
        {
            var sim = Simulation.NewGame(77, new Balance { startFon = 100 });
            while (sim.S.Day < 3) sim.Step();
            sim.S.Door.Visit = null;
            return sim;
        }

        static void Knock(Simulation sim, string card) => sim.S.Door.Visit = new DoorVisit { Day = sim.S.Day, Card = card };

        [Test]
        public void ClosedDoor_OffersPeekAskOpenAndRefuse_AndRevealsWhatWasSeen()
        {
            var sim = New();
            Knock(sim, "anne");
            var card = DoorCards.Get("anne");
            var d = DoorViewBuilder.Build(sim, DoorPhase.Closed, sim.DoorVisitor, false, false);
            Assert.AreEqual(new[] { DoorViewBuilder.Peek, DoorViewBuilder.Ask, Simulation.DoorOpen, Simulation.DoorRefuse }, d.Buttons.Select(b => b.Id).ToArray());
            Assert.IsTrue(d.Buttons.Single(b => b.Id == Simulation.DoorOpen).Primary);
            Assert.AreEqual(DeskText.Upper(Loc.T(card.Who)), d.Title);
            Assert.AreEqual(Loc.T(card.Knock), d.Body, "bakmadan ve sormadan yalnız duyulan");
            Assert.AreEqual(Loc.F("VARDİYA {0:000}", sim.S.Day + 1), d.Code);

            d = DoorViewBuilder.Build(sim, DoorPhase.Closed, sim.DoorVisitor, true, true);
            StringAssert.Contains(Loc.T(card.Peek), d.Body);
            StringAssert.Contains(Loc.T(card.Ask), d.Body);
            Assert.IsFalse(d.Buttons[0].Enabled, "bir kez bakılır");
            Assert.IsFalse(d.Buttons[1].Enabled, "bir kez sorulur");
        }

        [Test]
        public void ArmedVisitor_CanBeTalkedToThroughTheDoor()
        {
            var sim = New();
            Knock(sim, "silahli");
            var ids = DoorViewBuilder.Build(sim, DoorPhase.Closed, sim.DoorVisitor, false, false).Buttons.Select(b => b.Id).ToArray();
            Assert.AreEqual(new[] { DoorViewBuilder.Peek, DoorViewBuilder.Ask, "konus", Simulation.DoorOpen, Simulation.DoorRefuse }, ids);
        }

        [Test]
        public void OpenDoor_ShowsPricesAndWhyAnOptionIsClosed()
        {
            var sim = New();
            Knock(sim, "anne");
            Assert.IsTrue(sim.AnswerDoor(Simulation.DoorOpen).Ok);
            sim.Shelter.Supplies = 3;
            var d = DoorViewBuilder.Build(sim, DoorPhase.Open, sim.DoorVisitor, true, false);
            Assert.AreEqual(Loc.T(DoorCards.Get("anne").Opened), d.Body);
            var give = d.Buttons.Single(b => b.Id == "erzak");
            Assert.IsFalse(give.Enabled);
            Assert.AreEqual(sim.DoorAnswerCheck("erzak"), give.Detail, "seçilemeyen düğme gerekçesini yazar");
            var clinic = d.Buttons.Single(b => b.Id == "revir");
            Assert.IsFalse(clinic.Enabled);
            Assert.AreEqual(Loc.T("Revirde görevli yok."), clinic.Detail);
            sim.Shelter.Supplies = 20;
            give = DoorViewBuilder.Build(sim, DoorPhase.Open, sim.DoorVisitor, true, false).Buttons.Single(b => b.Id == "erzak");
            Assert.IsTrue(give.Enabled);
            Assert.AreEqual(Loc.F("{0} erzak", DeskFormat.Signed(-8, 0)), give.Detail, "bedel düğmede");
        }

        [Test]
        public void Result_ShowsTheOutcomeAndItsEffects_AndADeadlyDoorHoldsTheFinalReport()
        {
            var sim = New();
            Knock(sim, "anne");
            var visit = sim.DoorVisitor;
            sim.AnswerDoor(Simulation.DoorRefuse);
            var d = DoorViewBuilder.Build(sim, DoorPhase.Result, visit, false, false);
            StringAssert.Contains(Loc.T(DoorCards.Get("anne").Ignore.Text), d.Body);
            StringAssert.Contains(Loc.F("Güven {0}", DeskFormat.Signed(-1, 0)), d.Body);
            Assert.AreEqual(DoorViewBuilder.Close, d.Buttons.Single().Id);
            Assert.IsNull(d.Stamp);

            Knock(sim, "kopya");
            visit = sim.DoorVisitor;
            sim.AnswerDoor(Simulation.DoorOpen);
            Assert.AreEqual("sizma", sim.S.Ending);
            d = DoorViewBuilder.Build(sim, DoorPhase.Result, visit, true, true);
            Assert.AreEqual(DeskText.Upper(GameData.Ending("sizma").Title), d.Stamp);
            Assert.AreEqual(DoorViewBuilder.Ending, d.Buttons.Single().Id);
            var state = new DeskBridgeState { HoldEnding = true };
            Assert.IsNull(new SimDeskBuilder().Build(sim, state, false).GameOver, "kâğıt kapanana kadar son rapor bekler");
            state.HoldEnding = false;
            Assert.IsNotNull(new SimDeskBuilder().Build(sim, state, false).GameOver);
        }

        [Test]
        public void ShiftReport_ListsTheVisitorLeftAtTheDoor()
        {
            var sim = New();
            Knock(sim, "kurye");
            var before = ShiftReportBuilder.Capture(sim);
            sim.Step();
            var input = ShiftReportBuilder.Input(sim, before, ShiftReportBuilder.Ledger(sim), null);
            Assert.AreEqual("kurye", input.door.Single().card);
            var d = ShiftReportBuilder.Build(input);
            var row = d.Teams.Last();
            Assert.AreEqual(Loc.F("Kapı · {0}", Loc.T(DoorCards.Get("kurye").Who)), row.Label);
            Assert.AreEqual(Loc.T("AÇILMADI"), row.Value);
            // Kurye koliyi kapıda bıraktı: erzak satırı kapıyı ayrı gösterir, toplam simülasyonla aynı
            var door = d.Production.Single(r => r.Label == Loc.T("Kapı · açılmadı"));
            Assert.AreEqual(ShiftReportBuilder.Delta(input.door.Single().supplies, 1), door.Value);
            Assert.IsFalse(d.Production.Any(r => r.Label == Loc.T("Erzak tükendi")), "kapının erzağı düzeltme satırına düşmez");
        }

        [Test]
        public void DeskGameScene_DoorIsWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var door = roots.SelectMany(g => g.GetComponentsInChildren<SiginakDoor>(true)).Single();
                var so = new SerializedObject(door);
                foreach (var f in new[] { "game", "surface", "source", "rig", "telex", "prop", "card", "target", "soundPoint", "view", "bulb" })
                    Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
                var surface = roots.SelectMany(g => g.GetComponentsInChildren<LiberationDeskView>(true)).Single();
                Assert.AreSame(door, new SerializedObject(surface).FindProperty("siginakDoor").objectReferenceValue);

                var card = (DoorCardView)so.FindProperty("card").objectReferenceValue;
                Assert.IsFalse(card.gameObject.activeSelf, "kâğıt kapalı başlar");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, card.GetComponentInParent<Canvas>(true).renderMode);
                var slots = new SerializedObject(card).FindProperty("buttons");
                Assert.GreaterOrEqual(slots.arraySize, DoorCards.All.Max(c => c.Options.Length));
                Assert.GreaterOrEqual(slots.arraySize, DoorCards.All.Max(c => c.Closed.Length) + 4, "bak, sor, kapalı seçenekler, aç, açma");

                var prop = (DoorProp)so.FindProperty("prop").objectReferenceValue;
                var ps = new SerializedObject(prop);
                foreach (var f in new[] { "leaf", "wheel", "lamp", "lampOn", "lampOff", "lampLight", "peepCover", "corridorLight", "silhouette" })
                    Assert.IsNotNull(ps.FindProperty(f).objectReferenceValue, f);
                Assert.IsFalse(((Light)ps.FindProperty("lampLight").objectReferenceValue).enabled, "lamba yalnız biri çalınca");
                Assert.IsFalse(((Light)ps.FindProperty("corridorLight").objectReferenceValue).enabled, "koridor yalnız kapı açılınca");
                Assert.IsFalse(((GameObject)ps.FindProperty("silhouette").objectReferenceValue).activeSelf);
                var target = (DeskTarget)so.FindProperty("target").objectReferenceValue;
                Assert.AreEqual("SIĞINAK KAPISI", target.Title);
                Assert.AreEqual("K", target.KeyHint);

                var rig = (SiginakCameraRig)so.FindProperty("rig").objectReferenceValue;
                Assert.IsTrue(rig.TryGetPose(SiginakPose.Door, out var pose), "kapı pozu");
                // Kapı kâğıdın sağında, ekranda görünür
                var cam = (Camera)so.FindProperty("view").objectReferenceValue;
                rig.Snap(SiginakPose.Door);
                var p = cam.WorldToViewportPoint(prop.transform.position + Vector3.up * 1.0f);
                rig.Snap(SiginakPose.Desk);
                Assert.That(p.x, Is.InRange(0.45f, 0.8f), "kapı kâğıdın sağında");
                Assert.That(p.y, Is.InRange(0.2f, 0.8f));
                Assert.Greater(p.z, 0);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
