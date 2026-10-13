using System.Linq;
using Corrigible.Desk;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>F3 etkileşim: geçiş yapısı, Linear örtü alfası, sahte kaynağın niyetlere tepkisi, pencere/ipucu/onay bağlantıları.</summary>
    public class DeskInteractionTests
    {
        const string Root = "Assets/Corrigible/Desk";

        // ------------------------------------------------------------------ hareket
        [Test]
        public void Tween_DefaultIsIdle()
        {
            var t = new DeskTween();
            Assert.IsFalse(t.Running);
            Assert.IsFalse(t.Tick(0.016f), "varsayılan yapı ilerlemez (bölme yok)");
            Assert.AreEqual(0f, t.Value);
        }

        [Test]
        public void Tween_EasesOutAndStops()
        {
            var t = new DeskTween();
            t.Snap(0);
            t.To(1, 0.1f);
            Assert.IsTrue(t.Tick(0.05f));
            Assert.AreEqual(0.75f, t.Value, 1e-4f, "ease-out: yarı sürede %75");
            Assert.IsTrue(t.Tick(0.05f));
            Assert.AreEqual(1f, t.Value, 1e-5f);
            Assert.IsFalse(t.Running);
            Assert.IsFalse(t.Tick(0.05f), "bitince değer değişmez");
            t.To(1, 0.1f);
            Assert.IsFalse(t.Running, "aynı hedefe yeniden başlamaz");
            t.To(0, 0);
            Assert.AreEqual(0f, t.Value, "süre 0 = anında");
        }

        [Test]
        public void LinearOverlayAlpha_MatchesCssBlend()
        {
            ColorUtility.TryParseHtmlString("#0E0905", out var scrim);
            float a = DeskMath.LinearOverlayAlpha(scrim, 0.78f);
            Assert.AreEqual(0.94f, a, 0.01f, "karartma kâğıt üstünde ≈0,94");
            // Linear'da karıştırılınca sRGB sonucu CSS'inkine eşit olmalı
            float bg = 0.85f, c = (scrim.r + scrim.g + scrim.b) / 3f;
            float css = c * 0.78f + bg * 0.22f;
            float lin = Mathf.LinearToGammaSpace(Mathf.GammaToLinearSpace(c) * a + Mathf.GammaToLinearSpace(bg) * (1 - a));
            Assert.AreEqual(css, lin, 0.002f);
            Assert.AreEqual(1f, DeskMath.LinearOverlayAlpha(scrim, 1), 1e-5f);
            Assert.AreEqual(0f, DeskMath.LinearOverlayAlpha(scrim, 0), 1e-5f);
        }

        // ------------------------------------------------------------------ sahte kaynak
        static MockDeskDataSource Mock(out GameObject go)
        {
            go = new GameObject("MockTest");
            var src = go.AddComponent<MockDeskDataSource>();
            var so = new SerializedObject(src);
            var presets = so.FindProperty("presets");
            presets.arraySize = 1;
            presets.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/C1_normal.json");
            so.FindProperty("mapSpec").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/map.json");
            so.ApplyModifiedPropertiesWithoutUndo();
            return src;
        }

        [Test]
        public void Mock_SelectRegion_BuildsDossier()
        {
            var src = Mock(out var go);
            try
            {
                Assert.AreEqual("DAS", src.Current.Dossier?.RegionId, "C1'de Doğu Asya seçili");
                src.SelectRegion("BAV");
                Assert.AreEqual("BAV", src.Current.Dossier.RegionId);
                Assert.AreEqual(4, src.Current.Dossier.Actions.Count);
                Assert.IsTrue(src.Current.Dossier.Actions.All(a => !string.IsNullOrEmpty(a.Preview?.What)), "her eylemin önizlemesi VM'den gelir");
                Assert.IsTrue(src.Current.Map.Regions.Single(n => n.Id == "BAV").Selected);
                src.SelectRegion(null);
                Assert.IsNull(src.Current.Dossier, "dosya kapanınca seçim kalkar (açık karar 9)");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Mock_UnknownRegion_UsesUnknownDossier()
        {
            var src = Mock(out var go);
            try
            {
                var unknown = src.Current.Map.Regions.First(n => n.Trust == "unknown");
                src.SelectRegion(unknown.Id);
                var d = src.Current.Dossier;
                Assert.AreEqual(unknown.Id, d.RegionId);
                Assert.IsNull(d.Field?.Team, "ekipsiz bölgede baskın kapalı");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Mock_SpeedAndResearch()
        {
            var src = Mock(out var go);
            try
            {
                var c = src.Current.Clock;
                src.SetSpeed(GameSpeed.X2);
                Assert.AreEqual("2x", c.Speed); Assert.IsFalse(c.Paused);
                src.TogglePause(); Assert.IsTrue(c.Paused);
                src.StepSpeed(+1); Assert.AreEqual("4x", c.Speed); Assert.IsFalse(c.Paused, "hız değişimi sürdürür");
                src.StepSpeed(+1); Assert.AreEqual("4x", c.Speed, "üst sınır");
                src.StepSpeed(-1); src.StepSpeed(-1); src.StepSpeed(-1); Assert.AreEqual("1x", c.Speed, "alt sınır");

                var running = src.Current.Research.Branches.SelectMany(b => b.Projects).First(p => p.State == "running");
                src.SetResearchState(running.Id, false);
                Assert.AreEqual("paused", running.State);
                src.SetResearchState(running.Id, true);
                Assert.AreEqual("running", running.State, "▶ duraklatılmışı sürdürür");
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ------------------------------------------------------------------ prefab ve sahne bağlantıları
        static GameObject Prefab(string rel) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + rel + ".prefab");

        static Object Field(Object o, string name) => new SerializedObject(o).FindProperty(name).objectReferenceValue;

        [TestCase("Windows/HABERLER", "newsTemplate")]
        [TestCase("Windows/SAHAEKİPLERİ", "teamTemplate")]
        [TestCase("Windows/YZGÜNLÜĞÜ", "logTemplate")]
        [TestCase("Windows/REHBER", "guideTemplate")]
        public void ContentWindows_AreWired(string rel, string template)
        {
            var p = Prefab(rel);
            Assert.IsNotNull(p, rel);
            var v = p.GetComponent<ContentWindowView>();
            Assert.IsNotNull(Field(v, "window")); Assert.IsNotNull(Field(v, "content")); Assert.IsNotNull(Field(v, template));
            var fw = p.GetComponent<FolderWindow>();
            Assert.IsTrue(new SerializedObject(fw).FindProperty("autoTabWidth").boolValue);
            Assert.IsNotNull(fw.Tab.GetComponent<FolderTabHandle>(), "sekmeden sürüklenir");
            var sr = p.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            Assert.IsNotNull(sr.viewport.GetComponent<UnityEngine.UI.RectMask2D>(), "açık karar 7: RectMask2D");
            Assert.IsFalse(sr.horizontal);
        }

        [Test]
        public void InteractivePrefabs_HaveHitAreasAndLiftChild()
        {
            Assert.IsNotNull(Field(Prefab("Components/RegionNode").GetComponent<RegionNodeView>(), "visual"));
            Assert.IsNotNull(Field(Prefab("Components/RegionLabel").GetComponent<RegionLabelView>(), "hoverFrame"));
            Assert.IsNotNull(Field(Prefab("Screens/KillSwitch").GetComponent<KillSwitchEnvelopeView>(), "visual"));
            Assert.IsNotNull(Field(Prefab("Screens/GaugeCell").GetComponent<GaugeCellView>(), "hover"));
            Assert.IsNotNull(Field(Prefab("Screens/Telex").GetComponent<TelexStripView>(), "viewHover"));
            Assert.IsNotNull(Field(Prefab("Screens/AdvisorBand").GetComponent<AdvisorBandView>(), "messageHover"));
            Assert.IsNotNull(Field(Prefab("Screens/GoalCard").GetComponent<GoalCardView>(), "minimize"));
            foreach (var f in new[] { "Screens/ResearchFolder", "Screens/RegionDossier" })
            {
                var fw = Prefab(f).GetComponent<FolderWindow>();
                Assert.IsNotNull(fw.Tab.GetComponent<FolderTabHandle>(), f);
                Assert.IsTrue(fw.Body.GetComponent<UnityEngine.UI.Graphic>().raycastTarget, f + ": gövde haritaya tıklamayı engeller");
            }
            var modal = Prefab("Screens/ConfirmModal").GetComponent<ConfirmModalView>();
            Assert.IsNotNull(Field(modal, "scrim")); Assert.IsNotNull(Field(modal, "card"));
            Assert.IsNotNull(Prefab("Screens/Tooltip").GetComponent<DeskTooltip>());
        }

        [Test]
        public void Sandbox_WindowManagerAndInput()
        {
            var scene = EditorSceneManager.OpenScene(Root + "/Scenes/DeskUI_Sandbox.unity", OpenSceneMode.Additive);
            try
            {
                var wm = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WindowManager>(true)).Single();
                var list = new SerializedObject(wm).FindProperty("windows");
                Assert.AreEqual(6, list.arraySize, "araştırma, dosya, haberler, ekipler, günlük, rehber");
                var ids = Enumerable.Range(0, list.arraySize)
                    .Select(i => ((FolderWindow)list.GetArrayElementAtIndex(i).FindPropertyRelative("window").objectReferenceValue).Id).ToList();
                CollectionAssert.AreEquivalent(new[] { WindowId.Research, WindowId.Dossier, WindowId.News, WindowId.Teams, WindowId.AiLog, WindowId.Guide }, ids,
                    "hedef kartı pencere değildir (kendi başına sürüklenir)");
                var pres = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DeskPresenter>(true)).Single();
                Assert.IsNotNull(pres.GetComponent<DeskInput>());
                Assert.IsNotNull(Field(pres, "windows")); Assert.IsNotNull(Field(pres, "confirm"));
                Assert.AreEqual(4, new SerializedObject(pres).FindProperty("contentWindows").arraySize);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void Layout_IsAuthoredInScenes()
        {
            // Yerleşim sahnede: Play yalnız veriyi doldurur; harita ve hedef kartı sürüklenir, olay spotu haritadaki düğümü izler
            var map = Prefab("Screens/Map");
            Assert.IsNotNull(map.GetComponent<DeskDraggablePanel>(), "harita sürüklenir");
            Assert.IsTrue(map.transform.Find("Sheet").GetComponent<UnityEngine.UI.Graphic>().raycastTarget, "harita kâğıdından tutulur");
            Assert.IsNotNull(Prefab("Screens/GoalCard").GetComponent<DeskDraggablePanel>(), "hedef kartı sürüklenir");
            var modal = Prefab("Screens/EventModal").GetComponent<EventModalView>();
            Assert.AreNotEqual(0f, ((RectTransform)Field(modal, "log")).localEulerAngles.z, "günlüğün eğimi prefab'da");
            foreach (var path in new[] { "/Scenes/DeskGame.unity", "/Scenes/DeskUI_Sandbox.unity" })
            {
                var scene = EditorSceneManager.OpenScene(Root + path, OpenSceneMode.Additive);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var list = new SerializedObject(roots.SelectMany(g => g.GetComponentsInChildren<WindowManager>(true)).Single()).FindProperty("windows");
                    var open = Enumerable.Range(0, list.arraySize)
                        .Select(i => (FolderWindow)list.GetArrayElementAtIndex(i).FindPropertyRelative("window").objectReferenceValue)
                        .Where(w => w.gameObject.activeSelf).Select(w => w.Id).ToList();
                    CollectionAssert.AreEqual(new[] { WindowId.Research }, open, path + ": açılışta yalnız araştırma açık (sahnedeki durum)");
                    var em = roots.SelectMany(g => g.GetComponentsInChildren<EventModalView>(true)).Single();
                    Assert.IsNotNull(Field(em, "map"), path + ": olay spotu ve ipliği haritaya bağlı");
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
        }

        [Test]
        public void Dossier_SendChipsReplaceRaid()
        {
            var src = Mock(out var go);
            var dossier = (GameObject)PrefabUtility.InstantiatePrefab(Prefab("Screens/RegionDossier"));
            DeskUi.EditPreview = true;
            try
            {
                var v = dossier.GetComponentInChildren<RegionDossierView>(true);
                var d = src.Current.Dossier;
                d.Field.Team = null;
                d.Field.SendLabel = "Gönder";
                d.Field.Send.Add(new CommandVM { Label = "A · 3 gün", Kind = "paper-small", Command = "Team:Send:alfa:DAS" });
                d.Field.Send.Add(new CommandVM { Label = "B · 2 gün", Kind = "ink-small", Key = "B", Command = "Team:Send:bravo:DAS" });
                v.Bind(d);
                var row = (DeskRow)Field(v, "sendRow");
                Assert.IsTrue(row.gameObject.activeSelf, "ekip yokken fişler");
                Assert.IsFalse(v.Raid.gameObject.activeSelf, "baskın düğmesinin yerinde");
                Assert.AreEqual(2, v.SendChips.Count(c => c.gameObject.activeSelf));
                Assert.AreEqual(DeskButtonVariant.Ink, v.SendChips[1].Variant, "en yakını mürekkep");
                Assert.LessOrEqual(row.Width, ((RectTransform)row.transform).rect.width, "satıra sığar");

                d.Field.Send.Clear();
                v.Bind(d);
                Assert.IsFalse(row.gameObject.activeSelf);
                Assert.IsTrue(v.Raid.gameObject.activeSelf, "gönderilecek ekip yoksa eski düğme (kapalı, nedeni notta)");
            }
            finally { DeskUi.EditPreview = false; Object.DestroyImmediate(dossier); Object.DestroyImmediate(go); }
        }
    }
}
