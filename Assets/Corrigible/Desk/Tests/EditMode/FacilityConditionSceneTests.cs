using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Corrigible.Desk.Tests.FacilityMovesTestKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 12, parça P4b (aşama 12 tasarımı §11.4, §11.6): Katar ve Yalpa'nın sahne ve asset bağları. İki yeni renk
    /// (<c>RaidPieceLibrary.hazardBrink</c>, <c>hazardTilt</c>; asset ve C# yedeği aynı); tahtanın <c>brink</c>, <c>tilt</c>, <c>tilt_next</c>
    /// kenar dalları ve <c>slide</c> hattı kendi çizgileriyle çizilir, bilinmeyen tür yolundan kesik grafit çerçeveye düşmez ("Hazard mark"
    /// yok); "YALPA n" etiketi taşıyıcı karede tek kâğıt. Sahne ve prefab değişmez: Baskin.unity eklemeli açılır, kaydedilmez. Boş sahne
    /// açıkken koşar.
    /// </summary>
    public sealed class FacilityConditionSceneTests
    {
        const string LibraryPath = "Assets/Corrigible/Settings/RaidPieceLibrary.asset";

        static void AssertColor(Color expected, Color actual, string what)
        {
            Assert.AreEqual(expected.r, actual.r, 0.002f, what + " r");
            Assert.AreEqual(expected.g, actual.g, 0.002f, what + " g");
            Assert.AreEqual(expected.b, actual.b, 0.002f, what + " b");
            Assert.AreEqual(1, actual.a, 0.002f, what + " a");
        }

        [Test]
        public void HazardColors_Set_AssetAndFallback()
        {
            var lib = AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>(LibraryPath);
            Assert.IsNotNull(lib);
            var brink = new Color(0xA9 / 255f, 0x52 / 255f, 0x0A / 255f);
            var tilt = new Color(0x2C / 255f, 0x6E / 255f, 0x73 / 255f);
            AssertColor(brink, lib.hazardBrink, "asset hazardBrink #A9520A");
            AssertColor(tilt, lib.hazardTilt, "asset hazardTilt #2C6E73");
            var fresh = ScriptableObject.CreateInstance<RaidPieceLibrary>();
            try
            {
                AssertColor(brink, fresh.hazardBrink, "C# yedeği hazardBrink");
                AssertColor(tilt, fresh.hazardTilt, "C# yedeği hazardTilt");
            }
            finally { Object.DestroyImmediate(fresh); }
        }

        static int ActiveMarks(RaidBoard3D board) => board.GetComponentsInChildren<Transform>(false).Count(t => t.name == "Hazard mark");

        static void WithRoom(string region, System.Action<LiveRaidDataSource, RaidBoard3D> body, System.Action<Simulation> before = null)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            var lang = DeskLoc.Instance.Lang;
            try
            {
                DeskLoc.Instance.SetLanguage(DeskLang.Tr);
                var live = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LiveRaidDataSource>(true)).Single();
                var board = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RaidBoard3D>(true)).Single();
                var sim = FacilityDeskKit.Reach(79, region);
                before?.Invoke(sim);
                FacilityDeskKit.StartFirst(sim);
                live.OpenFacilityForValidation(sim);
                // Giriş odası şartsız: kenar ve ok yok
                Assert.IsNull(live.State.Ev);
                Assert.AreEqual(0, board.HazardShownCount("brink") + board.HazardShownCount("tilt_next"), "giriş odasında çizgi yok");
                FacilityDeskKit.Calm(live.State);
                FacilityDeskKit.ToGateCore(live.State);
                Republish(live);
                live.EndTurn();
                Assert.AreEqual(2, live.State.Room.Index, "ara oda açıldı");
                var B = live.State;
                FacilityDeskKit.Calm(B); Floor(B); Park(B);
                Republish(live);
                body(live, board);
            }
            finally { DeskLoc.Instance.SetLanguage(lang); EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>Ters sınama hedefi: <c>brink</c> dalı yokken kenar turnike kolu gibi (tür "edge") çizilirdi; işaret türü olsaydı kesik
        /// grafit çerçeveye düşerdi.</summary>
        [Test]
        public void Katar_BrinkEdges_OwnStrokes_NoFallbackFrame()
        {
            WithRoom("na", (live, board) =>
            {
                var B = live.State;
                Assert.AreEqual("katar", B.Ev?.Id);
                int edges = Baskin.Hazards(B).Edges.Count(e => e.Kind == "brink");
                Assert.Greater(edges, 0);
                Assert.AreEqual(2 * edges, board.HazardShownCount("brink"), "her kenar karesinde kesik çizgi ve tırnak");
                Assert.AreEqual(0, board.HazardShownCount("edge"), "turnike kolu yok");
                Assert.AreEqual(0, ActiveMarks(board), "kesik grafit çerçeve (bilinmeyen tür) yok");
            });
        }

        [Test]
        public void Yalpa_TiltArrowsLabelAndSlideLines_NoFallbackFrame()
        {
            WithRoom("oc", (live, board) =>
            {
                var B = live.State;
                Assert.AreEqual("yalpa", B.Ev?.Id);
                int col = Enumerable.Range(0, Baskin.N).Count(y => Baskin.CellAt(B, 0, y).T != "wall");
                Assert.AreEqual(col, board.HazardShownCount("tilt_next") - 1, "batı kenarında soluk oklar (+ YALPA 2 etiketi)");
                Assert.AreEqual(0, ActiveMarks(board), "önceki tur: kesik grafit çerçeve yok");
                Assert.AreEqual(0, board.HazardShownCount("slide"));

                Put(Team(B, "ana"), 0, 3);
                Machine(B, 4, 3);
                Baskin.CellAt(B, 2, 2).O = new Baskin.Obj { K = "hurda", Hp = 2, Max = 2 };
                B.Turn = 2;
                Republish(live);
                var lines = Baskin.Slides(B);
                Assert.Greater(lines.Count(l => !l.Hit), 0);
                Assert.Greater(lines.Count(l => l.Hit), 0);
                Assert.AreEqual(lines.Count(l => !l.Hit) + 2 * lines.Count(l => l.Hit), board.HazardShownCount("slide"), "kayan hat tek, çarpan hat iki parça (kırmızı uç)");
                Assert.AreEqual(col + 1, board.HazardShownCount("tilt"), "koyu oklar ve YALPA 1");
                Assert.AreEqual(0, board.HazardShownCount("tilt_next"));
                Assert.AreEqual(0, ActiveMarks(board), "yatış turu: kesik grafit çerçeve yok");
            });
        }
    }
}
