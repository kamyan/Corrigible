using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>Taktik harita sunumu: kamera izdüşümü, birim etiketi, Baskin sahnesinin katmanları.</summary>
    public class SadeRaidBoardTests
    {
        const string Root = "Assets/Corrigible/Desk";
        static T Ref<T>(Object owner, string field) where T : Object => new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;

        [TestCase(1920, 1080)]
        [TestCase(1920, 1200)]
        [TestCase(3440, 1440)]
        public void Camera_ProjectsGridAndHeightToReferencePixels(int width, int height)
        {
            var go = new GameObject("Kamera izdüşüm testi");
            var camera = go.AddComponent<Camera>();
            var target = new RenderTexture(width, height, 16);
            camera.targetTexture = target;
            try
            {
                float scale = Mathf.Min(width / 1920f, height / 1080f);
                float ppu = SadeTheme.Active.Number("raid.camera.pixelsPerUnitAt1080");
                camera.orthographic = true;
                camera.orthographicSize = SadeRaidCameraFraming.Size(height, scale, ppu);
                camera.transform.rotation = Quaternion.Euler(35, 45, 0);
                camera.transform.position = -camera.transform.forward * 20 - camera.transform.up * (12 / ppu);
                foreach (var p in new[] { new Vector3(0, 0, 0), new Vector3(8, 8, 0), new Vector3(3.5f, 2.5f, .9f), new Vector3(4, 4, 0) })
                {
                    Vector3 actual = camera.WorldToScreenPoint(new Vector3(p.x - 4, p.z, 4 - p.y));
                    float expectedX = width / 2f + (p.x + p.y - 8) * 93.75f * scale;
                    float expectedTop = height / 2f - 12 * scale + (p.y - p.x) * 53.775f * scale - p.z * 108.62f * scale;
                    Assert.AreEqual(expectedX, actual.x, 1, "x izdüşümü");
                    Assert.AreEqual(expectedTop, height - actual.y, 1, "üstten y izdüşümü");
                }
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(target); }
        }

        [Test]
        public void MachineWalk_FollowsCoreMoveRules()
        {
            // Core robotu yeni plana doğrudan taşır; tahta yürüyüşü aynı kuralla kurar: komşu kareler, duvar/nesne/ekip yok, menzil içinde
            int walks = 0;
            foreach (int seed in new[] { 11, 731, 2024 })
            {
                var b = Baskin.Create(new RaidParams { Seed = seed, Compute = 5 });
                for (int turn = 0; turn < 4 && b.Phase == "player"; turn++)
                {
                    var before = b.Units.Where(u => u.Side == 'e' && !u.Dead).ToDictionary(u => u.Id, u => (u.X, u.Y));
                    Baskin.EndTurn(b);
                    var pushed = new HashSet<int>(b.FxList.Where(f => f.K == "push" || f.K == "pull").Select(f => f.Unit));
                    foreach (var u in b.Units.Where(u => u.Side == 'e' && !u.Dead && before.ContainsKey(u.Id) && !pushed.Contains(u.Id)))
                    {
                        var at = before[u.Id];
                        if (at == (u.X, u.Y)) continue;
                        var path = RaidBoard3D.MachinePath(b, at, (u.X, u.Y));
                        Assert.AreEqual((u.X, u.Y), path.Last(), "yeni karesinde biter");
                        Assert.LessOrEqual(path.Count, Baskin.UnitTypes[u.Type].Move + b.EnemyMove, "menzil içinde");
                        foreach (var step in path)
                        {
                            Assert.AreEqual(1, Mathf.Abs(step.x - at.Item1) + Mathf.Abs(step.y - at.Item2), "komşu kare");
                            var cell = Baskin.CellAt(b, step.x, step.y);
                            Assert.AreNotEqual("wall", cell.T);
                            Assert.IsNull(cell.O);
                            Assert.AreNotEqual('p', Baskin.UnitAt(b, step.x, step.y)?.Side ?? 'e', "ekip üyesinin içinden geçmez");
                            at = step;
                        }
                        walks++;
                    }
                }
            }
            Assert.Greater(walks, 5);
        }

        [Test]
        public void TilePrefabs_CollidersReachWhatTheyShow()
        {
            // Kare görünen gövdesinin her yerinden tıklanır: duvar bloğunun tepesine tıklayan arkadaki kareyi seçmemeli. Giydirmenin
            // (RaidTileSkin) bütün çocukları açıkken de: köprüler, el çarkı, kanalın kolları, kenarları, bağlantısı ve kuyusu
            foreach (var name in new[] { "Floor", "Cable", "Wall" })
            {
                var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Prefabs/Raid/" + name + ".prefab"));
                try
                {
                    foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.SetActive(true);
                    var box = go.GetComponent<BoxCollider>();
                    float top = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds.max.y).DefaultIfEmpty(0).Max();
                    Assert.GreaterOrEqual(box.center.y + box.size.y / 2, top - 0.05f, name + " çarpıştırıcısı görünen tepesine uzanır");
                    Assert.AreEqual(1, box.size.x, 1e-3, name); Assert.AreEqual(1, box.size.z, 1e-3, name);
                }
                finally { Object.DestroyImmediate(go); }
            }
        }

        [Test]
        public void LabelPrefab_ShowsPipsAndTargetArmorWithoutHealthText()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<RaidScreenLabel>(Root + "/Prefabs/Sade/Raid/RaidScreenLabel.prefab");
            Assert.IsNotNull(prefab);
            var canvasObject = new GameObject("Etiket testi", typeof(RectTransform), typeof(Canvas));
            var cameraObject = new GameObject("Kamera", typeof(Camera));
            try
            {
                var label = Object.Instantiate(prefab, canvasObject.transform);
                label.Configure(cameraObject.GetComponent<Camera>(), canvasObject.GetComponent<Canvas>(), DeskTheme.Active.cpBold);
                label.ShowHealth(3, 4, Color.red, true, true);
                Assert.AreEqual(3, label.Health); Assert.AreEqual(4, label.Maximum);
                Assert.IsTrue(label.HasTarget); Assert.IsTrue(label.HasArmor);
                Assert.IsFalse(Ref<TMPro.TMP_Text>(label, "caption").gameObject.activeSelf);
                var array = new SerializedObject(label).FindProperty("pips");
                Assert.AreEqual(4, Enumerable.Range(0, array.arraySize).Count(i => ((Image)array.GetArrayElementAtIndex(i).objectReferenceValue).gameObject.activeSelf));
                label.ShowHealth(1, 2, Color.black, false, false);
                Assert.IsFalse(label.HasTarget); Assert.IsFalse(label.HasArmor);
            }
            finally { Object.DestroyImmediate(canvasObject); Object.DestroyImmediate(cameraObject); }
        }

        [Test]
        public void HoverInspector_RendersEmptySquareWithoutThumbnail()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<HoverInspectorView>(Root + "/Prefabs/Sade/Raid/HoverInspector.prefab");
            Assert.IsNotNull(prefab);
            var canvasObject = new GameObject("Kart testi", typeof(RectTransform), typeof(Canvas));
            try
            {
                var card = Object.Instantiate(prefab, canvasObject.transform);
                var thumbnail = Ref<Image>(card, "thumbnail");
                card.Render(new RaidInspectorModel { Title = "Robot", Thumb = "thumb_maint", Hp = 2, Max = 2 });
                Assert.IsTrue(thumbnail.enabled);
                // Boş karenin kartında küçük resim yok (Thumb null); kart yine çizilir, resim gizlenir.
                Assert.DoesNotThrow(() => card.Render(new RaidInspectorModel { Title = "Boş kare" }));
                Assert.IsTrue(card.gameObject.activeSelf);
                Assert.IsFalse(thumbnail.enabled);
                // Sprite tablosunda olmayan ad önceki kartın resmini göstermez
                card.Render(new RaidInspectorModel { Title = "Robot", Thumb = "thumb_maint", Hp = 2, Max = 2 });
                card.Render(new RaidInspectorModel { Title = "Bilinmeyen", Thumb = "thumb_yok_boyle_resim" });
                Assert.IsFalse(thumbnail.enabled);
            }
            finally { Object.DestroyImmediate(canvasObject); }
        }

        /// <summary>
        /// Gri alt satır (mekânın adı, BASKIN-MEKANLARI Katman 6): yalnız verildiğinde başlığın altında görünür ve başlık yukarı
        /// kayar; verilmezse gizli ve başlık prefab'daki yerinde (dikey ortada). Alt satır kurala ve can piplerine değmez.
        /// </summary>
        [Test]
        public void HoverInspector_SubtitleShowsUnderTheTitleOnlyWhenSet()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<HoverInspectorView>(Root + "/Prefabs/Sade/Raid/HoverInspector.prefab");
            var canvasObject = new GameObject("Kart testi", typeof(RectTransform), typeof(Canvas));
            try
            {
                var card = Object.Instantiate(prefab, canvasObject.transform);
                var title = Ref<TMPro.TMP_Text>(card, "title");
                var subtitle = Ref<TMPro.TMP_Text>(card, "subtitle");
                Assert.IsNotNull(subtitle, "alt satır bağlı");
                Assert.AreNotSame(title, subtitle);
                Assert.GreaterOrEqual(subtitle.fontSize, 11f, "HUD ölçeğinde (1,27) en az 14 px");
                var tr = title.rectTransform;
                var sr = subtitle.rectTransform;
                var rest = (tr.anchoredPosition.y, tr.sizeDelta.y);

                var row = (RectTransform)new SerializedObject(card).FindProperty("rows").GetArrayElementAtIndex(0).objectReferenceValue;
                var rule = new[] { new RaidCardRow { Icon = "ic_eye_s", Text = "Geçilmez; atışı durdurur" } };
                card.Render(new RaidInspectorModel { Title = "Duvar", Rows = rule });
                float rowY = row.anchoredPosition.y;
                Assert.IsFalse(subtitle.gameObject.activeSelf, "alt satırsız kartta gizli");
                Assert.AreEqual(rest, (tr.anchoredPosition.y, tr.sizeDelta.y), "başlık prefab'daki yerinde");

                card.Render(new RaidInspectorModel { Title = "Duvar", Subtitle = "Arşiv rafı", Rows = rule });
                Assert.IsTrue(subtitle.gameObject.activeSelf);
                Assert.AreEqual("Arşiv rafı", subtitle.text);
                Assert.AreEqual(RaidHudView.C("ink.ink3"), subtitle.color, "gri (ink3)");
                Assert.Greater(tr.anchoredPosition.y, rest.Item1, "başlık yukarı kayar");
                Assert.Less(tr.sizeDelta.y, rest.Item2, "başlık tek satıra iner");
                Assert.LessOrEqual(sr.anchoredPosition.y, tr.anchoredPosition.y - tr.sizeDelta.y + 0.01f, "alt satır başlığın altında");
                Assert.GreaterOrEqual(sr.anchoredPosition.y - sr.sizeDelta.y, -49f, "kuralın (y −49) üstünde");
                Assert.LessOrEqual(sr.anchoredPosition.x + sr.sizeDelta.x, 276f, "can piplerine (x 276) değmez");
                Assert.AreEqual(rowY, row.anchoredPosition.y, "satırlar yerinde: alt satır başlık bloğuna sığar");
                Assert.AreEqual(new Vector2(0, 1), sr.anchorMin, "başlık gibi sol üst çapa");
                Assert.AreEqual(new Vector2(0, 1), sr.pivot);

                // Kart yeniden alt satırsız çizilince başlık geri döner (kart nesnesi kareler arasında yeniden kullanılır)
                card.Render(new RaidInspectorModel { Title = "Boş kare" });
                Assert.IsFalse(subtitle.gameObject.activeSelf);
                Assert.AreEqual(rest, (tr.anchoredPosition.y, tr.sizeDelta.y));
            }
            finally { Object.DestroyImmediate(canvasObject); }
        }

        [Test]
        public void BaskinScene_UsesCameraBackgroundAndOverlayLabels()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            try
            {
                var components = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
                var board = components.OfType<RaidBoard3D>().Single();
                Assert.IsNotNull(Ref<RaidScreenLabel>(board, "screenLabelPrefab"));
                var labels = Ref<Canvas>(board, "screenLabelCanvas");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, labels.renderMode);
                var background = components.OfType<BoardBackgroundView>().Single();
                var canvas = Ref<Canvas>(background, "background");
                Assert.AreEqual(RenderMode.ScreenSpaceCamera, canvas.renderMode); Assert.AreSame(board.Camera, canvas.worldCamera);
                Assert.AreEqual(1, components.OfType<SadeRaidCameraFraming>().Count());
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
