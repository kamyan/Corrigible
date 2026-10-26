using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 4: sayaç çarkları, teleks, sayılı gerekçeler, şalter ve sahne bağlantıları.</summary>
    public class SiginakDeskTests
    {
        static readonly int StId = Shader.PropertyToID("_BaseMap_ST");

        static Simulation NewLiberation(double fon) => Simulation.NewGame(null, "manipulator", 731, new Balance { startFon = fon },
            rules: new RuleOptions { LiberationCampaign = true });

        static CounterWheel Wheel(GameObject parent)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.transform.SetParent(parent.transform);
            return q.AddComponent<CounterWheel>();
        }

        static float OffsetY(CounterWheel w)
        {
            var block = new MaterialPropertyBlock();
            w.GetComponent<Renderer>().GetPropertyBlock(block);
            return block.GetVector(StId).w;
        }

        [Test]
        public void CounterWheel_OffsetFollowsReadmeFormula()
        {
            var go = new GameObject("Wheel");
            try
            {
                var w = Wheel(go);
                for (int d = 0; d < 10; d++)
                {
                    w.Set(d);
                    Assert.AreEqual(1 - (d + 1) / 11f, OffsetY(w), 1e-5f, "rakam " + d);
                    Assert.AreEqual(d, w.Digit);
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void CounterModule_ShowsFundsWithTenthsAndClamps()
        {
            var go = new GameObject("Module");
            try
            {
                var wheels = new[] { Wheel(go), Wheel(go), Wheel(go) };
                var tenths = Wheel(go);
                var module = go.AddComponent<CounterModule>();
                module.Configure(wheels, tenths, null, null);
                module.Show(62.04, null, false);
                Assert.AreEqual("062", string.Concat(wheels.Select(w => w.Digit)));
                Assert.AreEqual(0, tenths.Digit);
                module.Show(52.5, null, false);
                Assert.AreEqual("052", string.Concat(wheels.Select(w => w.Digit)));
                Assert.AreEqual(5, tenths.Digit);
                module.Show(12345, null, false);
                Assert.AreEqual("999", string.Concat(wheels.Select(w => w.Digit)), "üç hane + onda bir: en çok 999,9");
                Assert.AreEqual(9, tenths.Digit);
                module.Show(-3, null, false);
                Assert.AreEqual("000", string.Concat(wheels.Select(w => w.Digit)));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void TelexFeed_CodesCountPerShiftAndRestoreContinues()
        {
            var go = new GameObject("Telex");
            try
            {
                var feed = go.AddComponent<TelexFeed>();
                Assert.AreEqual("014/01", feed.Post(14, "A", "x", TelexKind.Done).code);
                Assert.AreEqual("014/02", feed.Post(14, "B", "y", TelexKind.Failed).code);
                Assert.AreEqual("015/01", feed.Post(15, "C", "z", TelexKind.Info).code, "yeni vardiyada sayaç baştan");
                for (int i = 0; i < 40; i++) feed.Post(15, "D", "n", TelexKind.Info);
                Assert.AreEqual(30, feed.Lines.Count, "son 30 satır");
                var saved = feed.Lines.ToList();
                feed.Clear();
                Assert.AreEqual(0, feed.Lines.Count);
                feed.Restore(saved);
                Assert.AreEqual("015/42", feed.Post(15, "E", "m", TelexKind.Info).code, "kayıttan devamda sayaç sürer");
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary>PROMPT Aşama 4 kabulü: yapılamayan eylem teleksten gerekçeyle ve sayıyla basılır.</summary>
        [Test]
        public void TelexReasons_IncludeNeededAndHeldAmounts()
        {
            var sim = NewLiberation(100);
            sim.Shelter.Materials = 5;
            var cost = ShelterLogic.UpgradeCost(sim.Shelter, ShelterLogic.Infirmary, sim.B);
            string why = TelexReasons.Upgrade(sim, ShelterLogic.Infirmary);
            StringAssert.Contains(DeskFormat.Number(cost.Materials, 0), why);
            StringAssert.Contains(Loc.F("{0}: malzeme yetersiz ({1} gerekli / {2} var)", Loc.F("{0} onarımı", ShelterLogic.RoomName(ShelterLogic.Infirmary)),
                DeskFormat.Number(cost.Materials, 0), DeskFormat.Number(5, 0)), why);

            var poor = NewLiberation(9);
            string sector = poor.NextLiberationSector(poor.S.Liberation.CurrentRegion).Id;
            Assert.AreEqual(Loc.F("Baskın: fon yetersiz ({0} gerekli / {1} var)", DeskFormat.Number(poor.B.liberationRaidCost, 0), DeskFormat.Number(9, 1)),
                TelexReasons.Raid(poor, sector));
            Assert.IsNull(TelexReasons.Raid(sim, sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id), "yapılabilir emir için gerekçe yok");
        }

        [Test]
        public void ShiftSwitch_TwoStepsAndCancel()
        {
            var go = new GameObject("Switch");
            try
            {
                var sw = go.AddComponent<ShiftSwitch>();
                int armed = 0, thrown = 0, cancelled = 0;
                sw.Armed += () => armed++; sw.Thrown += () => thrown++; sw.Cancelled += () => cancelled++;
                sw.Press();
                Assert.AreEqual(ShiftSwitch.State.CoverOpen, sw.Current);
                Assert.IsTrue(sw.Cancel());
                Assert.AreEqual(ShiftSwitch.State.Idle, sw.Current);
                Assert.IsFalse(sw.Cancel(), "kapalı kapakta Esc ayarlara kalır");
                sw.Press(); sw.Press();
                Assert.AreEqual(ShiftSwitch.State.Thrown, sw.Current);
                Assert.AreEqual((2, 1, 1), (armed, thrown, cancelled));
                sw.Press();
                Assert.AreEqual(1, thrown, "kol inikken yeni basış yok sayılır");
                sw.Release();
                sw.SetDisabled(true, "baskın");
                sw.Press();
                Assert.AreEqual(ShiftSwitch.State.Disabled, sw.Current);
                Assert.AreEqual(2, armed);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void DeskGameScene_DeskControlsAreWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var desk = roots.SelectMany(g => g.GetComponentsInChildren<SiginakDesk>(true)).Single();
                var so = new SerializedObject(desk);
                foreach (var f in new[] { "game", "surface", "rig", "settings", "view", "counters", "rail", "shiftSwitch", "telex", "hope", "shelfTarget",
                             "crtTarget", "trayTarget", "telexTarget", "edge", "plaque" })
                    Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
                Assert.AreEqual(3, so.FindProperty("freedEntries").arraySize);

                var surface = roots.SelectMany(g => g.GetComponentsInChildren<LiberationDeskView>(true)).Single();
                var sv = new SerializedObject(surface);
                Assert.AreSame(desk, sv.FindProperty("siginak").objectReferenceValue);
                Assert.IsNotNull(sv.FindProperty("telex").objectReferenceValue);

                var room = desk.transform.parent.gameObject;
                Assert.AreEqual(5, room.GetComponentInChildren<StationKeyRail>(true).Count);
                foreach (var target in room.GetComponentsInChildren<DeskTarget>(true))
                    Assert.Greater(target.GetComponent<BoxCollider>().size.sqrMagnitude, 0, target.name + " çarpıştırıcısı boş");
                // Ekran katmanı Overlay; harita fişleri (Aşama 5) ayrı World Space canvas'lardır
                var hud = room.GetComponentInChildren<HoverTag>(true).GetComponent<Canvas>();
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, hud.renderMode);

                var sw = roots.SelectMany(g => g.GetComponentsInChildren<SiginakPresentationSwitch>(true)).Single();
                var physics = (Behaviour)new SerializedObject(sw).FindProperty("physicsRaycaster").objectReferenceValue;
                Assert.IsInstanceOf<PhysicsRaycaster>(physics);
                Assert.IsFalse(physics.enabled, "eski görünümde fizik ışını kapalı");
                var legacy = new SerializedObject(sw).FindProperty("legacyObjects");
                Assert.IsTrue(Enumerable.Range(0, legacy.arraySize).Any(i => ((GameObject)legacy.GetArrayElementAtIndex(i).objectReferenceValue).name == "LiberationHudCanvas"),
                    "yeni görünümde eski HUD ve alt çubuk gizlenir");
                Assert.IsFalse(room.transform.Find("Hope marks/Second mug").gameObject.activeSelf, "umut işaretleri açılışta gizli");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
