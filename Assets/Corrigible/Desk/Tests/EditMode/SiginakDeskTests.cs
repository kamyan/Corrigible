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

        static Simulation NewLiberation(double fon) => Simulation.NewGame(731, new Balance { startFon = fon });

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
        public void DeskGameScene_CounterBankHasSixDialsInOrder()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var bank = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CounterBank>(true)).Single();
                var modules = new SerializedObject(bank).FindProperty("modules");
                Assert.AreEqual(System.Enum.GetValues(typeof(CounterBank.Slot)).Length, modules.arraySize);
                float last = float.NegativeInfinity;
                for (int i = 0; i < modules.arraySize; i++)
                {
                    var module = (CounterModule)modules.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.IsNotNull(module, "sayaç " + i);
                    var label = module.transform.Find("Label");
                    float x = module.transform.Find("Window bezel").localPosition.x;
                    // Etiket kendi penceresinin üstünde; sayaçlar soldan sağa sırayla
                    Assert.AreEqual(x, label.localPosition.x, 1e-3f, module.name + " etiketi");
                    Assert.Greater(x, last, module.name);
                    last = x;
                }
                var trust = (CounterModule)modules.GetArrayElementAtIndex((int)CounterBank.Slot.Trust).objectReferenceValue;
                Assert.AreEqual("GÜVEN", trust.transform.Find("Label").GetComponent<DeskLocText>().Source);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void AlarmPanel_LightsNearThresholdAndBlinksOnLastStep()
        {
            Assert.AreEqual(0, AlarmPanel.LevelFor(0, 10, 3));
            Assert.AreEqual(0, AlarmPanel.LevelFor(6.9, 10, 3));
            Assert.AreEqual(1, AlarmPanel.LevelFor(7, 10, 3));
            Assert.AreEqual(1, AlarmPanel.LevelFor(8.95, 10, 3), "gösterilen değer aşağı yuvarlanır");
            Assert.AreEqual(2, AlarmPanel.LevelFor(9, 10, 3));
        }

        [Test]
        public void DeskGameScene_AlarmPanelIsWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var desk = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SiginakDesk>(true)).Single();
                var panel = new SerializedObject(desk).FindProperty("alarm").objectReferenceValue as AlarmPanel;
                Assert.IsNotNull(panel, "masa alarm panosuna bağlı");
                var so = new SerializedObject(panel);
                foreach (var field in new[] { "counter", "beacon", "lampOn", "lampOff", "target" })
                    Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, field);
                Assert.AreEqual("YZ ALARMI", panel.Counter.transform.Find("Label").GetComponent<DeskLocText>().Source);
                Assert.Greater(panel.GetComponent<BoxCollider>().size.sqrMagnitude, 0, "üzerine gelince açıklama için çarpıştırıcı");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
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
                             "crtTarget", "trayTarget", "telexTarget", "edge", "plaque", "door", "freedBoard", "tally" })
                    Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
                Assert.AreEqual(12, ((FreedRegionsBoard)so.FindProperty("freedBoard").objectReferenceValue).Count, "her bölgeye bir kart yuvası");

                var surface = roots.SelectMany(g => g.GetComponentsInChildren<LiberationDeskView>(true)).Single();
                var sv = new SerializedObject(surface);
                Assert.AreSame(desk, sv.FindProperty("siginak").objectReferenceValue);
                Assert.IsNotNull(sv.FindProperty("telex").objectReferenceValue);

                var room = desk.transform.parent.gameObject;
                // Revizyon 2: 1 · 2 · 3 · 4 KAPI · H · Home; yalnız kapı tuşunun dikkat lambası var
                var rail = room.GetComponentInChildren<StationKeyRail>(true);
                Assert.AreEqual(6, rail.Count);
                for (int i = 0; i < rail.Count; i++)
                    Assert.AreEqual(i == StationKeyRail.Door, new SerializedObject(rail[i]).FindProperty("jewelAttention").objectReferenceValue != null, rail[i].name);
                Assert.AreEqual("4", rail[StationKeyRail.Door].Target.KeyHint);
                foreach (var target in room.GetComponentsInChildren<DeskTarget>(true))
                    Assert.Greater(target.GetComponent<BoxCollider>().size.sqrMagnitude, 0, target.name + " çarpıştırıcısı boş");
                // Ekran katmanı Overlay; harita fişleri (Aşama 5) ayrı World Space canvas'lardır
                var hud = room.GetComponentInChildren<HoverTag>(true).GetComponent<Canvas>();
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, hud.renderMode);

                var physics = roots.SelectMany(g => g.GetComponentsInChildren<PhysicsRaycaster>(true)).Single();
                Assert.IsTrue(physics.enabled, "odadaki hedefler için fizik ışını açık");
                // Baskında oda kamerası kapalıyken ışını açık kalırsa tahtanın parçaları "arayüz" sayılır, karelere tıklanamaz
                var launcher = roots.SelectMany(g => g.GetComponentsInChildren<Corrigible.Game.Raid.RaidLauncher>(true)).Single();
                var hidden = new SerializedObject(launcher).FindProperty("hideDuringRaid");
                Assert.IsTrue(Enumerable.Range(0, hidden.arraySize).Any(i => hidden.GetArrayElementAtIndex(i).objectReferenceValue == physics),
                    "odanın fizik ışını baskın sürerken kapanır");
                Assert.IsFalse(roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Any(t => t.name == "LiberationHudCanvas"),
                    "eski HUD ve alt çubuk kaldırıldı (Aşama 10)");
                Assert.IsFalse(room.transform.Find("Hope marks/Child drawing").gameObject.activeSelf, "umut işaretleri açılışta gizli");
                // Revizyon 2: ikinci kupa harekât izlerine geçti (V.016'dan)
                Assert.IsFalse(room.transform.Find("Campaign wear/Second mug").gameObject.activeSelf, "harekât izleri açılışta gizli");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
