using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Punto taraması (Aşama 10; Revizyon 2'de genişletildi): sığınak odasındaki ve ekran katmanlarındaki (oda, CRT, eğitim notu,
    /// DeskUI: son rapor ve ayarlar) bütün TMP metinlerinin ve baskın HUD'unun (Baskin sahnesinin Overlay canvas'ları ve çalışırken
    /// kurulan etiket, telsiz satırı, üzerine gelme prefab'ları) 1920×1080'de ekrandaki em boyunu ölçer. Overlay metin: punto ×
    /// canvas'a göre ölçek (1080p referans). Dünya metni: her rig pozunun kamerasıyla izdüşürülür, metnin kadrajda olduğu pozların
    /// en büyüğü alınır (okunduğu poz). 14 px altı ihlaldir (tokens › type._notes.minimums: ikincil ≥ 14). Tasarımın kendi küçük
    /// puntoları ve anlatı süsleri gerekçeli istisnadır. Sahnelerin kaydedilmiş hâlini okur; değiştirmez.
    /// </summary>
    public static class SiginakTextScan
    {
        public const float Minimum = 14;
        public const string ReportPath = "Docs/UI_Siginak_Rev2/punto_taramasi.txt";
        public const string DeskScene = "Assets/Corrigible/Desk/Scenes/DeskGame.unity", RaidScene = "Assets/Corrigible/Scenes/Baskin.unity";
        static readonly string[] Roots = { "SiginakRoom", "SiginakCrtOverlay", "SiginakTutorial", "DeskUI" };
        /// <summary>
        /// Baskında çalışırken kurulan Overlay metinleri ve kuruldukları yer (sahnedeki yol; ölçeği çarpılır): dünya etiketi,
        /// telsiz kaydının satırı. Üzerine gelme kartı sahnede (RaidHud içinde) tarandığı için listede yok.
        /// </summary>
        static readonly (string prefab, string host)[] RaidPrefabs =
        {
            ("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidScreenLabel.prefab", null),
            ("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidLogRow.prefab", "RaidPanelCanvas/RaidPanels/RaidLogDrawer"),
        };

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
            ("Paper tray/Plate", "pirinç plaka; adı üzerine gelme etiketinde okunur"),
            ("Teleprinter/Paper text", "masadaki teleks kâğıdı; okunur boyda değilse satırlar ekranın altındaki şeritte basılır"),
            ("Shell/", "duvar yazıları ve afiş küçük satırları anlatı süsü"),
            ("Radio log/", "CRT önündeki telsiz kayıt defteri (tex2.js logBook ölçüsünde; son dört kayıt teleks, CRT ve raporlarda da okunur)"),
            ("CRT desk/Radio/", "telsizin yüz baskısı (tex2.js radioFace ölçüsünde: kanal rakamları, KANAL, SİNYAL, SES/AYAR, isim plakası; dekor, bilgi taşımaz)"),
            ("Test pattern", "CRT test deseni K-07 (tasarım: %28 desen)"),
            ("/Stamp/Word", "haritadaki ÖZGÜR damgasının bandı (tasarım ~9 ekran px, Aşama 5; durum tarama ve lejantta da var)"),
            ("/Stamp/Day", "haritadaki ÖZGÜR damgasının vardiya kodu (tasarım ~9 ekran px, Aşama 5)"),
            ("Team figure/Body/Letter", "figür plakasındaki “A” harfi (simge)"),
            ("Report sheet/", "EVRAK tepsisindeki A5 kâğıt başlığı (okuma görünümü ayrıca açılır)"),
            ("/Stencil", "boyalı şablon yazı (dekor)"),
            ("/KeyChip/Label", "eğitim notundaki tuş fişi CP 700 13 (tasarım: keyChip 13)"),
            ("RaidScreenLabel/", "baskın dünya etiketi: puntoyu çağıran verir (bayrak 20, durum ≥ 14); prefab'daki değer yer tutucu"),
        };

        [MenuItem("Corrigible/Sığınak/Punto taraması")]
        public static string Run()
        {
            var entries = new List<Entry>();
            foreach (var path in new[] { DeskScene, RaidScene })
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try { entries.AddRange(path == DeskScene ? Scan(scene) : ScanRaid(scene)); }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            File.WriteAllText(ReportPath, Report(entries));
            int bad = entries.Count(e => e.Violation);
            return $"{entries.Count} metin, {bad} ihlal, {entries.Count(e => e.Exception != null && e.Pixels < Minimum)} istisna · {ReportPath}";
        }

        /// <summary>Baskın: sahnenin Overlay canvas'ları ve çalışırken kurulan prefab'lar (hepsi 1080p referanslı Overlay).</summary>
        public static List<Entry> ScanRaid(Scene scene)
        {
            var list = new List<Entry>();
            foreach (var canvas in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true))
                         .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay))
                foreach (var t in canvas.GetComponentsInChildren<TMP_Text>(true))
                    list.Add(Overlay(t, canvas.transform, PathOf(t.transform)));
            foreach (var (path, host) in RaidPrefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                float hostScale = 1;
                var at = host == null ? null : Find(scene, host);
                for (var p = at; p != null && !(p.TryGetComponent<Canvas>(out var c) && c.isRootCanvas); p = p.parent) hostScale *= Mathf.Abs(p.localScale.y);
                foreach (var t in prefab.GetComponentsInChildren<TMP_Text>(true))
                    list.Add(Overlay(t, prefab.transform.parent, System.IO.Path.GetFileNameWithoutExtension(path) + "/" + PathOf(t.transform), hostScale));
            }
            return list;
        }

        static Transform Find(Scene scene, string path)
        {
            int slash = path.IndexOf('/');
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == (slash < 0 ? path : path.Substring(0, slash)));
            return root == null ? null : slash < 0 ? root.transform : root.transform.Find(path.Substring(slash + 1));
        }

        static Entry Overlay(TMP_Text t, Transform canvas, string path, float scale = 1)
        {
            for (var p = t.transform; p != null && p != canvas; p = p.parent) scale *= Mathf.Abs(p.localScale.y);
            float px = t.fontSize * scale;
            return new Entry { Path = path, Text = Short(t.text), Pixels = px, Pose = "Overlay", Exception = ExceptionOf(path, px) };
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
