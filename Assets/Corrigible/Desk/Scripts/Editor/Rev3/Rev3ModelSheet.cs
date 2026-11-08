using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Revizyon 3 model prefab'larının doğrulama görüntüleri (Aşama 1 › “sayfa/*.jpg ile görsel karşılaştırma”, “aile aile
    /// görüntü”). Boş, kaydedilmeyen bir sahnede çizer; açık sahneyi değiştirir, çağıran sonra kendi sahnesini açar.
    /// <list type="bullet">
    /// <item><see cref="RenderModelSheet"/>: modelin sayfası gibi önden / sağdan / üstten (ortografik) ve perspektif; ikinci
    /// satırda hareketli parçalar aralığın sonunda ve yazı yuvaları EN örnekle.</item>
    /// <item><see cref="RenderFamily"/>: ailenin bütün modelleri etiketli küçük karelerde (her model kendi ölçeğinde).</item>
    /// </list>
    /// </summary>
    public static class Rev3ModelSheet
    {
        public const string FamilyOutDir = "Docs/UI_Siginak_Rev3/screens_unity_rev3";
        static readonly Color Paper = new Color(0.925f, 0.898f, 0.835f);
        static readonly Vector3 PerspectiveFrom = new Vector3(0.62f, 0.55f, -0.75f);

        sealed class Rig : IDisposable
        {
            public readonly Camera Cam;
            public readonly RenderTexture Rt;
            readonly List<GameObject> owned = new List<GameObject>();
            public readonly TextMeshPro Label;

            public Rig(int w, int h)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.46f, 0.45f, 0.43f);
                AddLight(new Vector3(48, -32, 0), 1.25f, true);
                AddLight(new Vector3(18, 150, 0), 0.45f, false);
                var cg = new GameObject("Sheet camera"); owned.Add(cg);
                Cam = cg.AddComponent<Camera>();
                Cam.clearFlags = CameraClearFlags.SolidColor;
                Cam.backgroundColor = Paper;
                Cam.nearClipPlane = 0.005f; Cam.farClipPlane = 60;
                Rt = new RenderTexture(w, h, 24) { antiAliasing = 4 };
                Cam.targetTexture = Rt;
                var lg = new GameObject("Sheet label"); owned.Add(lg);
                Label = lg.AddComponent<TextMeshPro>();
                Label.font = DeskTheme.Active.Font(DeskFont.CpBold);
                Label.color = new Color(0.12f, 0.11f, 0.09f);
                Label.alignment = TextAlignmentOptions.Bottom;
                Label.textWrappingMode = TextWrappingModes.NoWrap;
                Label.gameObject.SetActive(false);
            }

            void AddLight(Vector3 euler, float intensity, bool shadows)
            {
                var g = new GameObject("Sheet light"); owned.Add(g);
                var l = g.AddComponent<Light>();
                l.type = LightType.Directional; l.intensity = intensity; l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
                g.transform.rotation = Quaternion.Euler(euler);
            }

            public void Dispose()
            {
                Cam.targetTexture = null;
                foreach (var g in owned) if (g) UnityEngine.Object.DestroyImmediate(g);
                Rt.Release(); UnityEngine.Object.DestroyImmediate(Rt);
            }

            /// <summary>Çizip <paramref name="sheet"/>'in (x, y) hücresine kopyalar (y aşağıdan).</summary>
            public void Shoot(Texture2D sheet, int x, int y)
            {
                Cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = Rt;
                sheet.ReadPixels(new Rect(0, 0, Rt.width, Rt.height), x, y);
                RenderTexture.active = prev;
            }

            public void Ortho(Bounds b, Vector3 viewDir, Vector3 up)
            {
                Cam.orthographic = true;
                var right = Vector3.Cross(up, viewDir).normalized;
                float w = Mathf.Abs(Vector3.Dot(b.size, right.Abs())), h = Mathf.Abs(Vector3.Dot(b.size, up.Abs()));
                float aspect = (float)Rt.width / Rt.height;
                Cam.orthographicSize = Mathf.Max(h / 2, w / 2 / aspect) * 1.18f + 0.002f;
                float depth = b.extents.magnitude * 2 + 1;
                Cam.transform.position = b.center - viewDir * depth;
                Cam.transform.rotation = Quaternion.LookRotation(viewDir, up);
                Cam.farClipPlane = depth * 2 + 2;
                Label.gameObject.SetActive(false);
            }

            public void Perspective(Bounds b, string label = null)
            {
                Cam.orthographic = false;
                Cam.fieldOfView = 30;
                float r = Mathf.Max(b.extents.magnitude, 0.01f);
                float dist = r / Mathf.Sin(Mathf.Deg2Rad * Cam.fieldOfView / 2) * 1.05f;
                Cam.transform.position = b.center + PerspectiveFrom.normalized * dist;
                Cam.transform.rotation = Quaternion.LookRotation(b.center - Cam.transform.position, Vector3.up);
                Cam.farClipPlane = dist * 3 + 2;
                if (string.IsNullOrEmpty(label)) { Label.gameObject.SetActive(false); return; }
                // Etiket kameranın önünde, kadrajın altında; görünür yükseklik d·2·tan(fov/2)
                float d = Cam.nearClipPlane * 4 + 0.02f;
                float viewH = 2 * d * Mathf.Tan(Mathf.Deg2Rad * Cam.fieldOfView / 2);
                Label.gameObject.SetActive(true);
                Label.text = label;
                Label.fontSize = viewH * 0.03f / 0.1f / 0.59f;
                Label.rectTransform.sizeDelta = new Vector2(viewH * Cam.aspect * 0.96f, viewH * 0.14f);
                Label.transform.SetPositionAndRotation(Cam.transform.position + Cam.transform.forward * d - Cam.transform.up * viewH * 0.41f,
                    Cam.transform.rotation);
            }
        }

        static Vector3 Abs(this Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        static Rev3Model Spawn(GameObject prefab)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var model = go.GetComponent<Rev3Model>();
            model.ResetPose();
            return model;
        }

        static Bounds WorldBounds(Rev3Model model)
        {
            var rs = model.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled && !(r.GetComponent<TMP_Text>())).ToList();
            if (rs.Count == 0) return new Bounds(model.transform.position, Vector3.one * 0.1f);
            var b = rs[0].bounds;
            foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }

        static void ShowSamples(Rev3Model model, bool english) { foreach (var s in model.Slots) if (s) s.ShowSample(english); }

        static void PoseEnd(Rev3Model model)
        {
            foreach (var p in model.Parts)
                if (p && p.Kind != PartKind.Swap) p.Set(Mathf.Abs(p.RangeTo - p.Rest) >= Mathf.Abs(p.RangeFrom - p.Rest) ? p.RangeTo : p.RangeFrom);
        }

        public static IEnumerable<GameObject> Prefabs(string family = null) =>
            AssetDatabase.FindAssets("t:Prefab", new[] { Rev3ModelImporter.PrefabsDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => family == null || p.StartsWith(Rev3ModelImporter.PrefabsDir + "/" + family + "/"))
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(g => g && g.GetComponent<Rev3Model>());

        /// <summary>
        /// Model sayfası: üst satır önden · sağdan · üstten (ortografik, dinlenme pozu, TR örnek), alt satır perspektif dinlenme ·
        /// perspektif aralık sonu (dönen/kayan parçalar dinlenmeden en uzak uçta) · perspektif EN örnek. JPG.
        /// </summary>
        public static void RenderModelSheet(GameObject prefab, string path, int cellW = 640, int cellH = 440)
        {
            using var rig = new Rig(cellW, cellH);
            var sheet = new Texture2D(cellW * 3, cellH * 2, TextureFormat.RGB24, false);
            var model = Spawn(prefab);
            try
            {
                ShowSamples(model, false);
                var b = WorldBounds(model);
                rig.Ortho(b, Vector3.forward, Vector3.up); rig.Shoot(sheet, 0, cellH);
                rig.Ortho(b, Vector3.left, Vector3.up); rig.Shoot(sheet, cellW, cellH);
                rig.Ortho(b, Vector3.down, Vector3.forward); rig.Shoot(sheet, cellW * 2, cellH);
                rig.Perspective(b); rig.Shoot(sheet, 0, 0);
                PoseEnd(model);
                rig.Perspective(WorldBounds(model)); rig.Shoot(sheet, cellW, 0);
                model.ResetPose();
                ShowSamples(model, true);
                rig.Perspective(b); rig.Shoot(sheet, cellW * 2, 0);
            }
            finally { UnityEngine.Object.DestroyImmediate(model.gameObject); }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, sheet.EncodeToJPG(88));
            UnityEngine.Object.DestroyImmediate(sheet);
        }

        /// <summary>Bir ailenin bütün modellerinin sayfası <paramref name="dir"/>/&lt;Model&gt;.jpg.</summary>
        public static string RenderModelSheets(string dir, string family = null)
        {
            int n = 0;
            foreach (var prefab in Prefabs(family)) { RenderModelSheet(prefab, Path.Combine(dir, prefab.name + ".jpg")); n++; }
            return n + " sayfa → " + dir;
        }

        /// <summary>Aile görüntüsü: her model kendi ölçeğinde, etiketli kare; 5 sütun. TR ya da EN örnek metinle.</summary>
        public static string RenderFamily(string family, bool english, string path = null, int tileW = 384, int tileH = 300, int columns = 5)
        {
            var prefabs = Prefabs(family).ToList();
            if (prefabs.Count == 0) return family + ": prefab yok";
            int rows = (prefabs.Count + columns - 1) / columns;
            path ??= $"{FamilyOutDir}/A1_{family}_{(english ? "EN" : "TR")}.jpg";
            using var rig = new Rig(tileW, tileH);
            var sheet = new Texture2D(tileW * columns, tileH * rows, TextureFormat.RGB24, false);
            var fill = Enumerable.Repeat((Color32)Paper, sheet.width * sheet.height).ToArray();
            sheet.SetPixels32(fill);
            for (int i = 0; i < prefabs.Count; i++)
            {
                var model = Spawn(prefabs[i]);
                try
                {
                    ShowSamples(model, english);
                    var m = Rev3Manifest.Load(model.ModelName);
                    var b = WorldBounds(model);
                    var size = b.size;
                    rig.Perspective(b, $"{model.ModelName}\n{size.x * 1000:0}×{size.y * 1000:0}×{size.z * 1000:0} mm · {m.tris} üçgen");
                    int col = i % columns, row = rows - 1 - i / columns;
                    rig.Shoot(sheet, col * tileW, row * tileH);
                }
                finally { UnityEngine.Object.DestroyImmediate(model.gameObject); }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, sheet.EncodeToJPG(86));
            UnityEngine.Object.DestroyImmediate(sheet);
            return $"{family}: {prefabs.Count} model → {path}";
        }

        [MenuItem("Corrigible/Revizyon 3/Aile görüntülerini çiz (TR ve EN)")]
        static void RenderAllFamiliesMenu()
        {
            var families = Prefabs().Select(p => p.GetComponent<Rev3Model>().Family).Distinct().OrderBy(f => f, StringComparer.Ordinal).ToList();
            foreach (var f in families) { RenderFamily(f, false); RenderFamily(f, true); }
            Debug.Log("[Rev3] aile görüntüleri: " + string.Join(", ", families));
        }
    }
}
