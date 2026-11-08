using System.Collections.Generic;
using System.Linq;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 10: punto taraması (1920×1080'de 14 px altı metin yok; tasarım istisnaları gerekçeli).</summary>
    public class SiginakTextScanTests
    {
        [Test]
        public void DeskGameScene_NoTextBelowMinimum()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var entries = SiginakTextScan.Scan(scene);
                Assert.Greater(entries.Count, 200, "oda ve ekran katmanlarındaki metinler bulundu");
                var bad = entries.Where(e => e.Violation).Select(e => $"{e.Pixels:0.0} px · {e.Pose} · {e.Path}").ToList();
                Assert.IsEmpty(bad, string.Join("\n", bad));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>Revizyon 2 · Aşama 10: baskın HUD'u ve panelleri (kurallar, telsiz kaydı, onaylar, üzerine gelme kartı) de ≥ 14 px.</summary>
        [Test]
        public void BaskinScene_NoTextBelowMinimum()
        {
            var scene = EditorSceneManager.OpenScene(SiginakTextScan.RaidScene, OpenSceneMode.Additive);
            try
            {
                var entries = SiginakTextScan.ScanRaid(scene);
                Assert.Greater(entries.Count, 100, "baskın HUD'u, panelleri ve çalışırken kurulan satırlar bulundu");
                var bad = entries.Where(e => e.Violation).Select(e => $"{e.Pixels:0.0} px · {e.Path}").ToList();
                Assert.IsEmpty(bad, string.Join("\n", bad));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>
        /// Revizyon 3 · Aşama 3 (E11): kart prefab'ları en kötü örneklerle panonun yerlerinde, K4'te (TR ve EN) ölçülür; kart adı
        /// ≥ 18 px, değerler ≥ 14 px, küçük yazılar gerekçeli; otomatik punto ve tek satırlık alanda taşma yok.
        /// </summary>
        [Test]
        public void BoardCards_AtK4_NameAtLeast18_ValuesAtLeast14()
        {
            var scene = EditorSceneManager.OpenScene(SiginakTextScan.DeskScene, OpenSceneMode.Additive);
            try
            {
                var bad = new List<string>();
                foreach (bool english in new[] { false, true })
                {
                    var entries = SiginakTextScan.ScanBoardCards(scene, english);
                    if (entries.Count == 0) Assert.Ignore("Kart prefab'ları ya da pano yok (" + SiginakTextScan.CardPrefabDir + ")");
                    Assert.IsTrue(entries.Any(e => e.Threshold >= SiginakTextScan.CardNameMinimum), "kişi kartının adı 18 px eşiğiyle ölçüldü");
                    Assert.IsTrue(entries.All(e => e.Path.StartsWith("Card/")), "girdiler Card/<tür>/<alan> adlı");
                    bad.AddRange(entries.Where(e => e.Violation)
                        .Select(e => $"{e.Pixels:0.0}/{e.Threshold:0} px · {e.Pose} · {e.Path} · “{e.Text}”{(e.Problem != null ? " · " + e.Problem : "")}"));
                }
                Assert.IsEmpty(bad, string.Join("\n", bad));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>
        /// Revizyon 3 · Aşama 3 (E12): dünya tuvalindeki yazı kendi yerinden ölçülür (tuvalin merkezinden değil): aynı tuvaldeki iki
        /// yazıdan kadraj dışına konanı "kadraj dışı" (0 px) döner, ortadaki kadrajda ölçülür.
        /// </summary>
        [Test]
        public void WorldText_IsMeasuredAtItsOwnPosition()
        {
            var made = new List<Object>();
            try
            {
                var camGo = new GameObject("Scan test camera") { hideFlags = HideFlags.HideAndDontSave };
                made.Add(camGo);
                var cam = camGo.AddComponent<Camera>();
                cam.enabled = false;
                var rt = new RenderTexture(1920, 1080, 0) { hideFlags = HideFlags.HideAndDontSave };
                made.Add(rt);
                cam.targetTexture = rt;
                cam.fieldOfView = 40;
                cam.nearClipPlane = 0.03f;
                camGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                var canvasGo = new GameObject("Scan test canvas", typeof(RectTransform), typeof(Canvas)) { hideFlags = HideFlags.HideAndDontSave };
                made.Add(canvasGo);
                canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                canvasGo.transform.SetPositionAndRotation(new Vector3(0, 0, 1), Quaternion.identity);
                canvasGo.transform.localScale = Vector3.one * 0.001f;
                ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(100, 100);

                TextMeshProUGUI Text(string name, Vector2 at)
                {
                    var go = new GameObject(name, typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
                    go.transform.SetParent(canvasGo.transform, false);
                    var t = go.AddComponent<TextMeshProUGUI>();
                    var font = DeskTheme.Active != null ? DeskTheme.Active.Font(DeskFont.CpBold) : null;
                    if (font != null) t.font = font;
                    t.fontSize = 20;
                    t.enableAutoSizing = false;
                    t.text = "ÖLÇÜ";
                    t.rectTransform.sizeDelta = new Vector2(60, 24);
                    t.rectTransform.anchoredPosition = at;
                    return t;
                }

                var inside = Text("Ortada", Vector2.zero);
                var outside = Text("Dışarıda", new Vector2(5000, 0));   // 5 m sağda: kamera 1 m'de ~0,73 m genişlik görür
                var cameras = new List<(string, Camera)> { ("Test", cam) };

                var a = SiginakTextScan.MeasureWorld(inside, cameras, true, true);
                var b = SiginakTextScan.MeasureWorld(outside, cameras, false, true);
                Assert.AreEqual("Test", a.pose, "ortadaki yazı kadrajda");
                Assert.Greater(a.px, 10f, "20 mm em 1 m'de ~30 px");
                Assert.AreEqual(0f, b.px, "kadraj dışındaki yazı ölçülmez");
                Assert.AreEqual("kadraj dışı", b.pose);
                Assert.That(Vector3.Distance(SiginakTextScan.Centre(outside), canvasGo.transform.position), Is.GreaterThan(4f),
                    "yazının merkezi kendi dikdörtgeninden (tuvalin merkezinden değil)");
            }
            finally
            {
                foreach (var o in made)
                {
                    if (o is RenderTexture r) r.Release();
                    if (o != null) Object.DestroyImmediate(o);
                }
            }
        }
    }
}
