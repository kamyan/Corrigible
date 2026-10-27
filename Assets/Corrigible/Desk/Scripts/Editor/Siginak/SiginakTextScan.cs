using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Punto taraması (PROMPT §7.4, Aşama 10): sığınak odasındaki ve ekran katmanlarındaki bütün TMP metinlerinin 1920×1080'de
    /// ekrandaki em boyunu ölçer. Overlay metin: punto × canvas'a göre ölçek (1080p referans). Dünya metni: her rig pozunun
    /// kamerasıyla izdüşürülür, metnin kadrajda olduğu pozların en büyüğü alınır (okunduğu poz). 14 px altı ihlaldir
    /// (tokens › type._notes.minimums: ikincil ≥ 14). Tasarımın kendi küçük puntoları ve anlatı süsleri gerekçeli istisnadır.
    /// Sahnenin kaydedilmiş hâlini okur (editörde ya da Play'de); sahneyi değiştirmez.
    /// </summary>
    public static class SiginakTextScan
    {
        public const float Minimum = 14;
        public const string ReportPath = "Docs/UI_Siginak/punto_taramasi.txt";
        static readonly string[] Roots = { "SiginakRoom", "SiginakCrtOverlay" };

        public struct Entry
        {
            public string Path, Text, Pose, Exception;
            public float Pixels;
            public bool Violation => Exception == null && Pixels < Minimum;
        }

        /// <summary>
        /// Gerekçeli istisnalar: yol parçası → neden. Tasarım panolarında (K1–K8) bu boyda olan ya da hiçbir pozda okunmak için
        /// konmamış (anlatı, doku) metinler.
        /// </summary>
        static readonly (string part, string why)[] Exceptions =
        {
            ("/Key/Text", "tuş fişi CP 700 13 (tasarım: keyChip 13)"),
            ("Operation slip/Paper/Close/Text", "“× Esc” tuş fişi CP 700 13 (tasarım: keyChip 13)"),
            ("Paper tray/Plate", "pirinç plaka; adı üzerine gelme etiketinde okunur"),
            ("Teleprinter/Paper text", "masadaki teleks kâğıdı; okunur boyda değilse satırlar ekranın altındaki şeritte basılır"),
            ("Cork board/", "mantar pano notları anlatı süsü; liste K1'de dekor (KURTARILAN BÖLGELER)"),
            ("Shell/", "duvar yazıları ve afiş küçük satırları anlatı süsü"),
            ("Keyboard", "klavye tuş dokusu"),
            ("Test pattern", "CRT test deseni K-07 (tasarım: %28 desen)"),
            ("/Stamp/Word", "haritadaki ÖZGÜR damgasının bandı (tasarım ~9 ekran px, Aşama 5; durum tarama ve lejantta da var)"),
            ("/Stamp/Day", "haritadaki ÖZGÜR damgasının vardiya kodu (tasarım ~9 ekran px, Aşama 5)"),
            ("Team figure/Body/Letter", "figür plakasındaki “A” harfi (simge)"),
            ("Report sheet/", "EVRAK tepsisindeki A5 kâğıt başlığı (okuma görünümü ayrıca açılır)"),
            ("/Stencil", "boyalı şablon yazı (dekor)"),
        };

        [MenuItem("Corrigible/Sığınak/Punto taraması")]
        public static string Run()
        {
            var entries = Scan(SceneManager.GetActiveScene());
            File.WriteAllText(ReportPath, Report(entries));
            int bad = entries.Count(e => e.Violation);
            return $"{entries.Count} metin, {bad} ihlal, {entries.Count(e => e.Exception != null && e.Pixels < Minimum)} istisna · {ReportPath}";
        }

        public static List<Entry> Scan(Scene scene)
        {
            var list = new List<Entry>();
            var roots = scene.GetRootGameObjects().Where(g => Roots.Contains(g.name)).ToArray();
            var rig = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SiginakCameraRig>(true)).FirstOrDefault();
            var cameras = rig != null ? PoseCameras(rig) : new List<(string, Camera)>();
            try
            {
                foreach (var root in roots)
                    foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        var (px, pose) = Measure(t, cameras);
                        string path = PathOf(t.transform);
                        list.Add(new Entry { Path = path, Text = Short(t.text), Pixels = px, Pose = pose, Exception = ExceptionOf(path, px) });
                    }
            }
            finally
            {
                foreach (var (_, cam) in cameras)
                {
                    var rt = cam.targetTexture;
                    cam.targetTexture = null;
                    if (rt != null) rt.Release();
                    Object.DestroyImmediate(cam.gameObject);
                }
            }
            return list;
        }

        static string ExceptionOf(string path, float px)
        {
            if (px >= Minimum) return null;
            foreach (var (part, why) in Exceptions) if (path.Contains(part)) return why;
            return null;
        }

        /// <summary>Rig pozlarıyla aynı fiziksel kameralar (sensör, odak, lens kaydırması), 1920×1080 hedef.</summary>
        static List<(string, Camera)> PoseCameras(SiginakCameraRig rig)
        {
            var sensor = new SerializedObject(rig).FindProperty("sensorSizeMm").vector2Value;
            var list = new List<(string, Camera)>();
            foreach (SiginakPose pose in System.Enum.GetValues(typeof(SiginakPose)))
            {
                if (!rig.TryGetPose(pose, out var slot)) continue;
                var go = new GameObject("Scan camera " + pose) { hideFlags = HideFlags.HideAndDontSave };
                var cam = go.AddComponent<Camera>();
                cam.enabled = false;
                cam.targetTexture = new RenderTexture(1920, 1080, 0) { hideFlags = HideFlags.HideAndDontSave };
                cam.usePhysicalProperties = true;
                cam.sensorSize = sensor;
                cam.gateFit = Camera.GateFitMode.Vertical;
                cam.focalLength = Camera.FieldOfViewToFocalLength(slot.fieldOfView, sensor.y);
                cam.lensShift = new Vector2(0, slot.lensShiftY);
                cam.nearClipPlane = 0.03f;
                go.transform.SetPositionAndRotation(slot.anchor.position, slot.anchor.rotation);
                list.Add((pose.ToString(), cam));
            }
            return list;
        }

        static (float px, string pose) Measure(TMP_Text t, List<(string, Camera)> cameras)
        {
            var canvas = t.GetComponentsInParent<Canvas>(true).LastOrDefault();
            if (t is TextMeshProUGUI && canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                // Overlay: CanvasScaler 1080 referans, yüksekliğe göre → 1080p'de ölçek 1; canvas altındaki yerel ölçekler çarpılır
                float scale = 1;
                for (var p = t.transform; p != null && p != canvas.transform; p = p.parent) scale *= Mathf.Abs(p.localScale.y);
                return (t.fontSize * scale, "Overlay");
            }
            // Dünya: em yüksekliği (UGUI: canvas pikseli × ölçek; TMP 3D: punto 10 = 1 m). Dünya canvas'ındaki metin canvas'ın
            // merkezinden ölçülür: düzen editörde kurulmamış olabilir, canvas'ın metinleri aynı pozda okunur
            float em = t.fontSize * Mathf.Abs(t.transform.lossyScale.y) * (t is TextMeshPro ? 0.1f : 1);
            var center = t is TextMeshProUGUI && canvas != null ? canvas.transform.position : t.transform.position;
            var up = t.transform.up * em;
            float best = 0;
            string bestPose = "kadraj dışı";
            foreach (var (name, cam) in cameras)
            {
                var a = cam.WorldToScreenPoint(center);
                if (a.z <= cam.nearClipPlane || a.x < 0 || a.x > 1920 || a.y < 0 || a.y > 1080) continue;
                var b = cam.WorldToScreenPoint(center + up);
                float px = Vector2.Distance(a, b);
                if (px > best) { best = px; bestPose = name; }
            }
            return (best, bestPose);
        }

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var p = t; p != null; p = p.parent) parts.Add(p.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        static string Short(string s)
        {
            s = (s ?? "").Replace("\n", " ⏎ ");
            return s.Length > 40 ? s.Substring(0, 40) + "…" : s;
        }

        public static string Report(List<Entry> entries)
        {
            var sb = new StringBuilder();
            int bad = entries.Count(e => e.Violation);
            sb.AppendLine($"Sığınak punto taraması · {System.DateTime.Now:yyyy-MM-dd HH:mm} · 1920×1080 · en az {Minimum} px");
            sb.AppendLine($"{entries.Count} metin · {bad} ihlal · {entries.Count(e => e.Exception != null && e.Pixels < Minimum)} gerekçeli istisna");
            sb.AppendLine();
            sb.AppendLine("İHLALLER");
            foreach (var e in entries.Where(e => e.Violation).OrderBy(e => e.Pixels)) sb.AppendLine($"  {e.Pixels,5:0.0} px · {e.Pose,-11} · {e.Path} · “{e.Text}”");
            if (bad == 0) sb.AppendLine("  yok");
            sb.AppendLine();
            sb.AppendLine("İSTİSNALAR (14 px altı, gerekçeli)");
            foreach (var e in entries.Where(e => e.Exception != null && e.Pixels < Minimum).OrderBy(e => e.Path))
                sb.AppendLine($"  {e.Pixels,5:0.0} px · {e.Pose,-11} · {e.Path} · {e.Exception}");
            sb.AppendLine();
            sb.AppendLine("EN KÜÇÜK 20 (istisnasız)");
            foreach (var e in entries.Where(e => e.Exception == null).OrderBy(e => e.Pixels).Take(20))
                sb.AppendLine($"  {e.Pixels,5:0.0} px · {e.Pose,-11} · {e.Path}");
            return sb.ToString();
        }
    }
}
