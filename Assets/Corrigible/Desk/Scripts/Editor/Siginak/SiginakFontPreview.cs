using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Aşama 1 font denemesi (Docs/UI_Siginak PROMPT §5): sığınak arayüzünün kullandığı her yüzde zorunlu glif satırı ve
    /// TR/EN örnek metin. <c>Scenes/Siginak_FontTest.unity</c> sahnesini kurar (açık sahneye dokunmadan, ek sahne olarak),
    /// kameradan 1920×1080 PNG çeker ve statik atlas zincirinde bulunmayan glifleri raporlar. Örnek metinler geliştirici
    /// içindir, oyuncuya gösterilmez.
    /// </summary>
    public static class SiginakFontPreview
    {
        public const string ScenePath = DeskSpriteManifest.Root + "/Scenes/Siginak_FontTest.unity";
        public const string CapturePath = "Docs/UI_Siginak/screens_unity/A1_fonts.png";

        /// <summary>PROMPT §2: her font asset'i bu glifleri içermeli (eksik glif = hata).</summary>
        public const string RequiredGlyphs = "ĞÜŞİÖÇ ğüşıöç ✓ ▸ · → − ± × … “ ” ‘ ’ — % ▮";

        /// <summary>Sığınak yazı tablosundaki yüzler: rol, yüz, TR örneği, EN örneği.</summary>
        static readonly (string role, string face, string tr, string en)[] Rows =
        {
            ("orderBody", "Courier Prime 400", "Dünya çoktan düştü. Emirler kâğıtla gider.", "The world has already fallen. Orders go on paper."),
            ("plateLabel", "Courier Prime 700", "VARDİYAYI BİTİR · ÖZGÜR BÖLGE 01/12 · İŞGAL %66,7", "END SHIFT · FREE REGIONS 01/12 · OCCUPIED 66.7%"),
            ("taskBody", "Libre Franklin 400", "Teknisyeni ikmal kasasının yanına götür; kasayı kırma.", "Move the technician next to the supply crate; do not break it."),
            ("sectorName", "Libre Franklin 600", "02 Tahliye geçidi · YAĞMA · BASKINA HAZIR", "02 Evacuation passage · LOOT · READY TO RAID"),
            ("inkButton", "Libre Franklin 700", "Enter · BASKIN EMRİ VER · −12 fon", "Enter · ORDER THE RAID · −12 funds"),
            ("analystNote", "Kalam 700", "→ jeneratöre 2 kişi, atölye döner", "→ 2 staff to the generator, workshop runs"),
            ("wallStencil", "yedek: Courier Prime 700", "KYGA / SIĞINAK 07 · AĞA BAĞLANMA", "KYGA / SHELTER 07 · STAY OFF THE NET"),
        };

        [MenuItem("Corrigible/Sığınak/Font deneme sahnesi")]
        public static string Build()
        {
            var active = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var report = new StringBuilder();
            try
            {
                var siginak = SiginakTheme.Active;
                var desk = DeskTheme.Active;
                Color paper = siginak.ColorOf("paper"), ink = siginak.ColorOf("ink"), ink2 = siginak.ColorOf("ink2");

                var camGo = new GameObject("Camera");
                SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = paper;
                cam.orthographic = true;
                cam.cullingMask = 1 << 5;

                var canvasGo = new GameObject("FontTest", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                SceneManager.MoveGameObjectToScene(canvasGo, scene);
                canvasGo.layer = 5;
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = MapProjection.DesignSize;

                var texts = new List<TMP_Text>();
                float y = -36;
                Text(canvasGo.transform, "Title", "SIĞINAK · AŞAMA 1 · FONT DENEMESİ", DeskFont.CpBold, 22, ink, 40, ref y, texts);
                y -= 10;
                foreach (var row in Rows)
                {
                    siginak.TryType(row.role, out var style);
                    var font = style.font;
                    Text(canvasGo.transform, row.role + "/Label", $"{row.face} · {row.role} · {style.size:0} px", DeskFont.CpBold, 14, ink2, 20, ref y, texts);
                    Text(canvasGo.transform, row.role + "/Glyphs", RequiredGlyphs, font, 30, ink, 40, ref y, texts);
                    Text(canvasGo.transform, row.role + "/TR", "TR  " + row.tr, font, 22, ink, 30, ref y, texts);
                    Text(canvasGo.transform, row.role + "/EN", "EN  " + row.en, font, 22, ink, 30, ref y, texts);
                    y -= 14;
                    var missing = MissingStatic(desk.Font(font), RequiredGlyphs + row.tr + row.en);
                    report.AppendLine($"{row.face} ({desk.Font(font)?.name}): " + (missing.Length == 0 ? "tam" : "EKSİK " + missing));
                }

                Canvas.ForceUpdateCanvases();
                foreach (var t in texts) t.ForceMeshUpdate(true, true);
                Capture(cam, 1920, 1080, CapturePath);
                EditorSceneManager.SaveScene(scene, ScenePath);
                report.AppendLine("Sahne: " + ScenePath + " · Görüntü: " + CapturePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (active.IsValid()) SceneManager.SetActiveScene(active);
            }
            Debug.Log("[Sığınak] Font denemesi\n" + report);
            return report.ToString();
        }

        static void Text(Transform parent, string name, string value, DeskFont font, float size, Color color, float height,
            ref float y, List<TMP_Text> texts)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(48, y);
            rt.sizeDelta = new Vector2(1824, height);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = DeskTheme.Active.Font(font);
            t.fontSize = size;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.text = value;
            texts.Add(t);
            y -= height;
        }

        /// <summary>
        /// Asset'in kendisinde ya da statik yedeklerinde (dinamik yedekler sayılmaz; derlemede boşaltılırlar) bulunmayan
        /// karakterler. Boşluk ve satır sonu atlanır.
        /// </summary>
        public static string MissingStatic(TMP_FontAsset font, string text)
        {
            if (font == null) return text;
            var missing = new StringBuilder();
            foreach (var c in text.Distinct())
                if (c > ' ' && !HasStatic(font, c, new HashSet<TMP_FontAsset>())) missing.Append(c);
            return missing.ToString();
        }

        static bool HasStatic(TMP_FontAsset font, char c, HashSet<TMP_FontAsset> seen)
        {
            if (font == null || !seen.Add(font)) return false;
            if (font.atlasPopulationMode == AtlasPopulationMode.Static && font.characterLookupTable.ContainsKey(c)) return true;
            return font.fallbackFontAssetTable != null && font.fallbackFontAssetTable.Any(f => HasStatic(f, c, seen));
        }

        static void Capture(Camera cam, int width, int height, string path)
        {
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(tex);
            }
        }
    }
}
