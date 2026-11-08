using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Revizyon 3 model hattı (PROMPT §4): <c>Docs/UI_Siginak_Rev3/modeller</c> OBJ + manifest → prefab. Kalıcı bir asset
    /// hattıdır, sahne kurmaz (AGENTS kural 5): odaya yerleştirme her aşamanın tek seferlik script'iyle yapılır.
    /// <list type="bullet">
    /// <item>OBJ'ler <c>Art/Rev3/Models/&lt;aile&gt;</c> altına kopyalanır; ayarı <see cref="Rev3ModelPostprocessor"/> kilitler.</item>
    /// <item>Prefab: kökte <see cref="Rev3Model"/>, altında <c>Body</c> ve manifestteki parçalar (<c>parent</c>,
    /// <c>pivotLocal</c>); hareketli parçada <see cref="PartDriver"/>, dinlenme pozunda.</item>
    /// <item>Malzeme anahtarı → <see cref="Rev3MaterialLibrary"/>; <c>TXT_*</c> → TMP (<see cref="Rev3TextSlot"/>; punto
    /// <c>capMm</c>'den, fontun büyük harf oranıyla); çapalar boş nesne; ışık noktaları kapalı <see cref="Light"/> (açıp kapamak
    /// poz ışığının işi).</item>
    /// <item>2 cm'den küçük renderer gölge vermez (README › Bütçe ve kurallar).</item>
    /// </list>
    /// Prefab var olan yolun üstüne yazılır (GUID korunur). Rapor <see cref="ReportPath"/>.
    /// </summary>
    public static class Rev3ModelImporter
    {
        public const string ArtDir = DeskSpriteManifest.Root + "/Art/Rev3";
        public const string ModelsDir = ArtDir + "/Models";
        public const string PrefabsDir = DeskSpriteManifest.Root + "/Prefabs/Rev3";
        public const string ReportPath = "Docs/UI_Siginak_Rev3/model_import_report.txt";

        /// <summary>Yazının yüzeyden ayrılması (z-fighting olmasın), metre.</summary>
        const float TextLift = 0.0002f;
        /// <summary>2 cm'den küçük parça gölge vermez.</summary>
        const float NoShadowBelow = 0.02f;
        /// <summary>TextMeshPro (3B) ölçeği: fontSize 1 → 0,1 birim em.</summary>
        const float TmpEmPerFontSize = 0.1f;

        public static string PrefabPath(string family, string model) => PrefabsDir + "/" + family + "/" + model + ".prefab";
        public static string ObjPath(string family, string model) => ModelsDir + "/" + family + "/" + model + ".obj";

        [MenuItem("Corrigible/Revizyon 3/Modelleri içe aktar")]
        static void ImportAllMenu() => Debug.Log("[Rev3] " + ImportAll());

        public sealed class Result
        {
            public string Name, Family;
            public int Tris, ManifestTris, Parts, Texts, Anchors, Lights;
            /// <summary>Prefab kutusu ile OBJ köşelerinin kutusu arasındaki en büyük fark (mm; içe aktarma doğruluğu).</summary>
            public float ObjErrorMm;
            /// <summary>Manifest kutusunun prefab kutusundan geniş olduğu en büyük pay (mm; three.js'in yaklaşık kutusu).</summary>
            public float ManifestSlackMm;
            public string Note = "";
            public readonly List<string> Warnings = new List<string>();
            public override string ToString() =>
                $"{Family,-10} {Name,-22} üçgen {Tris,5}/{ManifestTris,-5} OBJ ±{ObjErrorMm,4:0.00} mm  manifest payı {ManifestSlackMm,5:0.00} mm  " +
                $"parça {Parts,2} yazı {Texts,2} çapa {Anchors,2} ışık {Lights}{Note}" +
                (Warnings.Count > 0 ? "  !! " + string.Join("; ", Warnings) : "");
        }

        /// <summary>Bütün modelleri (ya da yalnız bir aileyi) içe aktarır; malzeme kitaplığı her seferinde bütün anahtarlarla kurulur.</summary>
        public static string ImportAll(string onlyFamily = null)
        {
            var manifests = Rev3Manifest.LoadAll();
            var log = new StringBuilder();
            var lib = Rev3MaterialSetup.Build(manifests, log);
            var list = onlyFamily == null ? manifests : manifests.Where(m => m.family == onlyFamily).ToList();

            int copied = 0;
            AssetDatabase.StartAssetEditing();
            try { foreach (var m in list) if (CopySource(m)) copied++; }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();

            var results = new List<Result>();
            foreach (var m in list)
            {
                try { results.Add(BuildPrefab(m, lib)); }
                catch (Exception e) { var r = new Result { Name = m.name, Family = m.family }; r.Warnings.Add("HATA " + e.Message); results.Add(r); }
            }
            AssetDatabase.SaveAssets();

            var report = new StringBuilder();
            report.AppendLine("# Revizyon 3 · model içe aktarma raporu (Corrigible/Revizyon 3/Modelleri içe aktar)");
            report.AppendLine("# üçgen = prefab/manifest · OBJ = sıfır pozda prefab kutusu ile OBJ köşelerinin farkı (≤ 0,1 mm)");
            report.AppendLine("# manifest payı = manifest kutusunun prefab kutusundan genişliği (three.js setFromObject döndürülmüş küçük parçada");
            report.AppendLine("#   kutuyu yaklaşık hesaplar; prefab manifest kutusunun 1 mm içinde kalmalı)");
            report.Append(log);
            report.AppendLine($"Model: {results.Count}, kopyalanan OBJ: {copied}, uyarılı: {results.Count(r => r.Warnings.Count > 0)}");
            foreach (var r in results.OrderBy(r => r.Family, StringComparer.Ordinal).ThenBy(r => r.Name, StringComparer.Ordinal))
                report.AppendLine(r.ToString());
            if (onlyFamily == null) File.WriteAllText(ReportPath, report.ToString());
            return $"{results.Count} model, {results.Count(r => r.Warnings.Count > 0)} uyarılı" +
                   (results.Any(r => r.Warnings.Count > 0) ? ":\n" + string.Join("\n", results.Where(r => r.Warnings.Count > 0)) : "");
        }

        /// <summary>
        /// Paketteki dışa aktarıcı (<c>threejs/r3/export_obj.mjs</c>) geometriyi indekssiz sanıp köşeleri üçer üçer yüz yapar;
        /// indeksli <c>PlaneGeometry</c> ile kurulan model bozuk çıkar. Köşe, normal ve UV aynı kalır, yalnız yüzler three.js'in
        /// <c>PlaneGeometry</c> dizin sırasıyla yeniden yazılır (Aşama 1 raporu). Model → (ızgara x, ızgara y).
        /// </summary>
        static readonly Dictionary<string, (int x, int y)> PlaneRepairs = new Dictionary<string, (int x, int y)>
        {
            ["HandoverSlip"] = (4, 8),   // threejs/r3/models/desk.js › HandoverSlip: PlaneGeometry(0,10, 0,15, 4, 8)
        };

        /// <summary>OBJ ve MTL'yi kopyalar; içerik aynıysa dokunmaz (gereksiz yeniden içe aktarma olmasın).</summary>
        static bool CopySource(Rev3Manifest m)
        {
            string dir = ModelsDir + "/" + m.family;
            Directory.CreateDirectory(dir);
            bool changed = false;
            foreach (var (src, dst) in new[] { (m.ObjSource, dir + "/" + m.name + ".obj"), (m.MtlSource, dir + "/" + m.name + ".mtl") })
            {
                var bytes = File.ReadAllBytes(src);
                if (dst.EndsWith(".obj") && PlaneRepairs.TryGetValue(m.name, out var grid))
                    bytes = Encoding.UTF8.GetBytes(RepairPlane(Encoding.UTF8.GetString(bytes), grid.x, grid.y));
                if (File.Exists(dst) && File.ReadAllBytes(dst).SequenceEqual(bytes)) continue;
                File.WriteAllBytes(dst, bytes);
                changed = true;
            }
            return changed;
        }

        static string RepairPlane(string obj, int gridX, int gridY)
        {
            var lines = obj.Split('\n').Where(l => !l.StartsWith("f ")).ToList();
            int verts = lines.Count(l => l.StartsWith("v "));
            if (verts != (gridX + 1) * (gridY + 1)) throw new Exception($"düzlem onarımı: {verts} köşe ≠ {(gridX + 1) * (gridY + 1)}");
            int insertAt = lines.FindLastIndex(l => l.StartsWith("usemtl ")) + 1;
            string F(int i) => $"{i + 1}/{i + 1}/{i + 1}";
            var faces = new List<string>();
            int row = gridX + 1;
            for (int iy = 0; iy < gridY; iy++)
                for (int ix = 0; ix < gridX; ix++)
                {
                    int a = ix + row * iy, b = ix + row * (iy + 1), c = ix + 1 + row * (iy + 1), d = ix + 1 + row * iy;
                    faces.Add($"f {F(a)} {F(b)} {F(d)}");
                    faces.Add($"f {F(b)} {F(c)} {F(d)}");
                }
            lines.InsertRange(insertAt, faces);
            lines.Insert(1, "# Corrigible: yüzler PlaneGeometry dizininden yeniden kuruldu (dışa aktarıcı indeksli geometriyi indekssiz yazmış)");
            return string.Join("\n", lines);
        }

        /// <summary>OBJ köşelerinin sıfır pozdaki model uzayı kutusu (parça köşeleri pivotuna göre; OBJ x = −Unity x).</summary>
        public static Bounds ObjBounds(Rev3Manifest m, string objPath)
        {
            var pivots = m.parts.ToDictionary(p => p.name, p => Rev3Manifest.V(p.pivotModel));
            pivots["Body"] = Vector3.zero;
            var current = Vector3.zero;
            bool any = false;
            var b = new Bounds();
            foreach (var l in File.ReadLines(objPath))
            {
                if (l.StartsWith("o ")) current = pivots.TryGetValue(l.Substring(2).Trim(), out var pv) ? pv : Vector3.zero;
                else if (l.StartsWith("v "))
                {
                    var f = l.Split(' ');
                    var v = new Vector3(-float.Parse(f[1], CultureInfo.InvariantCulture), float.Parse(f[2], CultureInfo.InvariantCulture),
                        float.Parse(f[3], CultureInfo.InvariantCulture)) + current;
                    if (!any) { b = new Bounds(v, Vector3.zero); any = true; } else b.Encapsulate(v);
                }
            }
            return b;
        }

        /// <summary>OBJ'deki grup adları (sırasıyla).</summary>
        static List<string> ObjGroups(string objPath) =>
            File.ReadLines(objPath).Where(l => l.StartsWith("o ")).Select(l => l.Substring(2).Trim()).ToList();

        static Result BuildPrefab(Rev3Manifest m, Rev3MaterialLibrary lib)
        {
            var result = new Result { Name = m.name, Family = m.family, ManifestTris = m.tris };
            string objPath = ObjPath(m.family, m.name);
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            if (src == null) throw new Exception("OBJ içe aktarılmadı: " + objPath);

            // Grup → (mesh, malzeme anahtarları). Tek gruplu dosyada mesh kök nesnededir.
            var groupNames = ObjGroups(objPath);
            var groups = new Dictionary<string, (Mesh mesh, string[] keys)>(StringComparer.Ordinal);
            foreach (var mf in src.GetComponentsInChildren<MeshFilter>(true))
            {
                string g = mf.transform == src.transform && groupNames.Count == 1 ? groupNames[0] : mf.name;
                var r = mf.GetComponent<MeshRenderer>();
                groups[g] = (mf.sharedMesh, r ? r.sharedMaterials.Select(x => x ? x.name : "").ToArray() : new string[0]);
            }
            foreach (var g in groupNames.Where(g => !groups.ContainsKey(g))) result.Warnings.Add("grup içe aktarılmadı: " + g);

            var root = new GameObject(m.name);
            try
            {
                var model = root.AddComponent<Rev3Model>();
                var nodes = new Dictionary<string, Transform>(StringComparer.Ordinal);
                var pivots = new Dictionary<string, Vector3>(StringComparer.Ordinal) { ["Body"] = Vector3.zero };
                var body = new GameObject("Body").transform;
                body.SetParent(root.transform, false);
                nodes["Body"] = body;
                AddMesh(body, "Body", groups, lib, result);

                // Parçalar: ebeveyni önce kurulmuş olanlar sırayla
                var drivers = new List<PartDriver>();
                var pending = m.parts.ToList();
                while (pending.Count > 0)
                {
                    var next = pending.FirstOrDefault(p => nodes.ContainsKey(p.parent ?? "Body"));
                    if (next == null) { result.Warnings.Add("ebeveyni olmayan parça: " + string.Join(",", pending.Select(p => p.name))); break; }
                    pending.Remove(next);
                    var t = new GameObject(next.name).transform;
                    t.SetParent(nodes[next.parent ?? "Body"], false);
                    t.localPosition = Rev3Manifest.V(next.pivotLocal);
                    nodes[next.name] = t;
                    pivots[next.name] = Rev3Manifest.V(next.pivotModel);
                    var renderer = AddMesh(t, next.name, groups, lib, result);
                    drivers.Add(ConfigurePart(t, next, renderer, lib, result));
                }

                var slots = m.texts.Select(x => AddText(x, nodes, pivots, result)).Where(x => x != null).ToArray();
                var anchors = m.anchors.Select(a => AddPoint(a.name, a.part, a.pos, nodes, pivots, result)).Where(x => x != null).ToArray();
                var lights = m.lights.Where(l => l.type == "point" || l.type == "spot")
                    .Select(l => AddLight(l, nodes, pivots, result)).Where(x => x != null).ToArray();

                model.Configure(m.name, m.family, m.tris, m.Bounds, drivers.ToArray(), slots, anchors, lights);

                result.Parts = drivers.Count; result.Texts = slots.Length; result.Anchors = anchors.Length; result.Lights = lights.Length;
                result.Tris = Tris(model);
                var bounds = ZeroPoseBounds(model);
                var expected = m.Bounds;
                result.ObjErrorMm = BoundsError(bounds, ObjBounds(m, objPath)) * 1000;
                result.ManifestSlackMm = Mathf.Max((bounds.min - expected.min).Abs().MaxComponent(), (bounds.max - expected.max).Abs().MaxComponent()) * 1000;
                float outside = Mathf.Max((expected.min - bounds.min).MaxComponent(), (bounds.max - expected.max).MaxComponent()) * 1000;
                if (PlaneRepairs.ContainsKey(m.name)) result.Note = "  (yüzler onarıldı)";
                if (result.Tris != m.tris) result.Warnings.Add($"üçgen {result.Tris} ≠ {m.tris}");
                if (result.ObjErrorMm > 0.1f) result.Warnings.Add($"OBJ kutusundan {result.ObjErrorMm:0.00} mm farklı");
                if (outside > 1) result.Warnings.Add($"manifest kutusunun {outside:0.0} mm dışında");

                string path = PrefabPath(m.family, m.name);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            return result;
        }

        static MeshRenderer AddMesh(Transform t, string group, Dictionary<string, (Mesh mesh, string[] keys)> groups, Rev3MaterialLibrary lib, Result result)
        {
            if (!groups.TryGetValue(group, out var g) || g.mesh == null) return null;
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = g.mesh;
            var r = t.gameObject.AddComponent<MeshRenderer>();
            var mats = new Material[g.keys.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = lib.Get(g.keys[i]);
                if (mats[i] == null) result.Warnings.Add($"malzeme yok: {group}/{g.keys[i]}");
            }
            r.sharedMaterials = mats;
            bool noShadow = MaxExtent(g.mesh.bounds.size) < NoShadowBelow || g.keys.Length > 0 && g.keys.All(lib.NoShadow);
            r.shadowCastingMode = noShadow ? ShadowCastingMode.Off : ShadowCastingMode.On;
            r.receiveShadows = true;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            return r;
        }

        static float MaxExtent(Vector3 v) => Mathf.Max(v.x, Mathf.Max(v.y, v.z));

        static readonly Regex KeyRx = new Regex(@"[A-Za-z][A-Za-z0-9_]*", RegexOptions.Compiled);

        static PartDriver ConfigurePart(Transform t, Rev3Manifest.Part p, MeshRenderer renderer, Rev3MaterialLibrary lib, Result result)
        {
            var driver = t.gameObject.AddComponent<PartDriver>();
            var kind = p.kind == "slide" ? PartKind.Slide : p.kind == "swap" ? PartKind.Swap : PartKind.Rotate;
            var axis = p.axis == "x" ? PartAxis.X : p.axis == "y" ? PartAxis.Y : PartAxis.Z;
            float from = p.range != null && p.range.Length > 1 ? p.range[0] : 0, to = p.range != null && p.range.Length > 1 ? p.range[1] : 0;
            if (kind != PartKind.Swap && (p.range == null || p.axis == null)) result.Warnings.Add($"{p.name}: aralık ya da eksen yok");

            Renderer swapRenderer = null; int slot = 0; Material[] swaps = null;
            if (kind == PartKind.Swap && renderer != null)
            {
                // Notta geçen malzeme anahtarları (sırayla) swap durumlarıdır; durum 0 modelin kendi malzemesi
                var keys = KeyRx.Matches(p.note ?? "").Cast<Match>().Select(x => x.Value).Where(k => lib.Get(k) != null).Distinct().ToList();
                var own = renderer.sharedMaterials;
                for (int i = 0; i < own.Length && swaps == null; i++)
                {
                    var ownKey = keys.FirstOrDefault(k => lib.Get(k) == own[i]);
                    if (ownKey == null) continue;
                    var states = new List<Material> { own[i] };
                    states.AddRange(keys.Where(k => k != ownKey).Select(lib.Get).Where(x => x != own[i]));
                    if (states.Count > 1) { swapRenderer = renderer; slot = i; swaps = states.ToArray(); }
                }
            }
            driver.Configure(p.name, kind, axis, from, to, p.rest ?? 0, Rev3Manifest.V(p.pivotLocal), swapRenderer, slot, swaps);
            return driver;
        }

        static Transform AddPoint(string name, string part, float[] pos, Dictionary<string, Transform> nodes, Dictionary<string, Vector3> pivots, Result result)
        {
            string key = part ?? "Body";
            if (!nodes.TryGetValue(key, out var parent)) { result.Warnings.Add($"{name}: parça yok ({key})"); return null; }
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = Rev3Manifest.V(pos) - pivots[key];
            return t;
        }

        static readonly Regex HexRx = new Regex(@"#[0-9A-Fa-f]{6}", RegexOptions.Compiled);

        static Rev3TextSlot AddText(Rev3Manifest.Text x, Dictionary<string, Transform> nodes, Dictionary<string, Vector3> pivots, Result result)
        {
            var t = AddPoint(x.name, x.part, x.center, nodes, pivots, result);
            if (t == null) return null;
            var normal = Rev3Manifest.V(x.normal).normalized;
            var up = Rev3Manifest.V(x.up).normalized;
            if (normal == Vector3.zero) normal = Vector3.back;
            if (up == Vector3.zero) up = Vector3.up;
            var localPos = t.localPosition + normal * TextLift;
            var localRot = Quaternion.LookRotation(-normal, up);
            var size = Rev3Manifest.V2(x.size);
            bool surface = x.capMm == null && string.IsNullOrEmpty(x.sample);
            // RectTransform eklenince eski Transform yok edilir: nesne önceden tutulur, konum ve dönüş bileşenden sonra yazılır
            var go = t.gameObject;
            if (surface) go.AddComponent<RectTransform>();
            var tmp = surface ? null : go.AddComponent<TextMeshPro>();
            var rt = (RectTransform)go.transform;
            rt.localPosition = localPos;
            rt.localRotation = localRot;
            rt.sizeDelta = size;
            var slot = rt.gameObject.AddComponent<Rev3TextSlot>();
            if (surface)
            {
                slot.Configure(x.name, x.role, SlotFont(x), x.sample, x.sampleEn, 0, size, true, null);
                return slot;
            }

            var font = FontFor(SlotFont(x));
            tmp.font = font;
            tmp.rectTransform.sizeDelta = size;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = x.align == "left" ? TextAlignmentOptions.Left : x.align == "right" ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
            tmp.color = TextColor(x.role);
            tmp.text = SampleText(x);
            float cap = x.capMm ?? 0;
            if (cap > 0) tmp.fontSize = FontSizeForCap(font, cap);
            else
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 0.01f;
                tmp.fontSizeMax = FontSizeForCap(font, size.y * 1000 * 0.7f);
            }
            var mr = rt.GetComponent<MeshRenderer>();
            if (mr) { mr.shadowCastingMode = ShadowCastingMode.Off; mr.lightProbeUsage = LightProbeUsage.Off; }
            slot.Configure(x.name, x.role, SlotFont(x), SampleText(x), SampleLines(x.sampleEn, x.note), cap, size, false, tmp);
            return slot;
        }

        static readonly Regex LinesRx = new Regex(@"(iki|üç|dört|beş|altı|yedi|[2-9])\s+satır", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Örnek metin: notu birden çok satır diyen yuvada (“üç satır, 14 mm aralık”) örnekteki “ / ” satır ayırıcısıdır.
        /// Örnekler yalnız referanstır; görünümler gerçek metni yazar.
        /// </summary>
        public static string SampleText(Rev3Manifest.Text x) => SampleLines(x.sample, x.note);

        public static string SampleLines(string sample, string note) =>
            !string.IsNullOrEmpty(sample) && LinesRx.IsMatch(note ?? "") ? sample.Replace(" / ", "\n") : sample ?? "";

        /// <summary>
        /// Yuvanın fontu: rol şablon yazısıysa (<c>wallStencil</c>; token <c>type.wallStencil</c> = Big Shoulders Stencil 800)
        /// ya da rol metni Big Shoulders diyorsa şablon fontu; yoksa manifestin <c>font</c> alanı. (Paketin giydirme modülleri
        /// fontu role göre seçiyor; ör. menü başlığında alan Courier, çizim şablon fontu.)
        /// </summary>
        public static string SlotFont(Rev3Manifest.Text x)
        {
            var role = x.role ?? "";
            return role.StartsWith("wallStencil") || role.Contains("Big Shoulders") ? "Big Shoulders Stencil Display 800" : x.font;
        }

        /// <summary>Büyük harf yüksekliği (mm) → TextMeshPro fontSize: em = fontSize × 0,1 birim; cap = em × capLine / pointSize.</summary>
        public static float FontSizeForCap(TMP_FontAsset font, float capMm)
        {
            float ratio = font != null && font.faceInfo.pointSize > 0 && font.faceInfo.capLine > 0
                ? font.faceInfo.capLine / font.faceInfo.pointSize : 0.7f;
            return capMm / 1000f / (TmpEmPerFontSize * ratio);
        }

        /// <summary>Manifestteki aile adı (ör. “Courier Prime 700”) → masa fontu.</summary>
        public static TMP_FontAsset FontFor(string family)
        {
            var theme = DeskTheme.Active;
            family ??= "";
            if (family.StartsWith("Big Shoulders")) return theme.Font(DeskFont.StencilExtraBold);
            if (family.StartsWith("Kalam")) return theme.Font(family.Contains("700") ? DeskFont.KalamBold : DeskFont.KalamRegular);
            if (family.StartsWith("Libre Franklin")) return theme.Font(family.Contains("800") ? DeskFont.LfExtraBold
                : family.Contains("700") ? DeskFont.LfBold : family.Contains("600") ? DeskFont.LfSemiBold : DeskFont.LfRegular);
            return theme.Font(family.Contains("400") ? DeskFont.CpRegular : DeskFont.CpBold);
        }

        /// <summary>Rol metnindeki renk (ör. “stamp (kırmızı #9A1E14)”), yoksa role göre: tebeşir ve şablon krem, el yazısı mavi mürekkep, öteki mürekkep.</summary>
        static Color TextColor(string role)
        {
            role ??= "";
            var m = HexRx.Match(role);
            string hex = m.Success ? m.Value
                : role.StartsWith("chalk") ? "#E6E2D4"
                : role.StartsWith("wallStencil") ? "#D8D2BE"
                : role.StartsWith("handNote") ? "#1A2F5A"
                : "#1E1B16";
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.black;
        }

        static readonly Regex SpotRx = new Regex(@"(\d+)\s*°", RegexOptions.Compiled);
        static readonly Regex TargetRx = new Regex(@"hedef[^(]*\(([^)]*)\)", RegexOptions.Compiled);
        static readonly Regex RangeRx = new Regex(@"menzil\s*([0-9]+(?:,[0-9]+)?)", RegexOptions.Compiled);

        /// <summary>Türkçe ondalıklı üçlü: “0, 1,29, −0,02” (öğe ayıracı “, ”).</summary>
        static bool TryVector(string s, out Vector3 v)
        {
            v = default;
            var parts = s.Split(new[] { ", " }, StringSplitOptions.None);
            if (parts.Length != 3) return false;
            var f = new float[3];
            for (int i = 0; i < 3; i++)
                if (!float.TryParse(parts[i].Replace('−', '-').Replace(',', '.').Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out f[i])) return false;
            v = new Vector3(f[0], f[1], f[2]);
            return true;
        }

        static Light AddLight(Rev3Manifest.LightPoint x, Dictionary<string, Transform> nodes, Dictionary<string, Vector3> pivots, Result result)
        {
            var t = AddPoint(x.name, x.part, x.pos, nodes, pivots, result);
            if (t == null) return null;
            var light = t.gameObject.AddComponent<Light>();
            light.type = x.type == "spot" ? LightType.Spot : LightType.Point;
            light.color = ColorUtility.TryParseHtmlString(x.color ?? "", out var c) ? c : Color.white;
            light.shadows = x.shadows ? LightShadows.Soft : LightShadows.None;
            light.intensity = 1;
            var range = RangeRx.Match(x.note ?? "");
            light.range = range.Success ? float.Parse(range.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture) : 3;
            if (light.type == LightType.Spot)
            {
                var angle = SpotRx.Match(x.note ?? "");
                light.spotAngle = angle.Success ? float.Parse(angle.Groups[1].Value, CultureInfo.InvariantCulture) : 60;
                var target = TargetRx.Match(x.note ?? "");
                if (target.Success && TryVector(target.Groups[1].Value, out var aim) && aim != Rev3Manifest.V(x.pos))
                    t.rotation = Quaternion.LookRotation(aim - Rev3Manifest.V(x.pos), Vector3.up);
                else t.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            }
            light.enabled = false;
            return light;
        }

        // ------------------------------------------------------------------ doğrulama (test de kullanır)

        public static int Tris(Rev3Model model)
        {
            int n = 0;
            foreach (var mf in GeometryFilters(model))
                for (int s = 0; s < mf.sharedMesh.subMeshCount; s++) n += (int)mf.sharedMesh.GetIndexCount(s) / 3;
            return n;
        }

        /// <summary>İki kutunun en büyük köşe farkı (metre).</summary>
        public static float BoundsError(Bounds b, Bounds expected) =>
            Mathf.Max((b.min - expected.min).Abs().MaxComponent(), (b.max - expected.max).Abs().MaxComponent());

        /// <summary>
        /// Sıfır pozda (dönüşsüz, parçalar pivotta) model uzayındaki sınır kutusu. Konum parçaların
        /// <see cref="PartDriver.PivotLocal"/> zincirinden hesaplanır; o anki pozdan bağımsızdır.
        /// </summary>
        public static Bounds ZeroPoseBounds(Rev3Model model)
        {
            bool any = false;
            var total = new Bounds();
            foreach (var mf in GeometryFilters(model))
            {
                var offset = Vector3.zero;
                for (var t = mf.transform; t != null && t != model.transform; t = t.parent)
                {
                    var d = t.GetComponent<PartDriver>();
                    offset += d != null ? d.PivotLocal : t.localPosition;
                }
                var mb = mf.sharedMesh.bounds;
                var b = new Bounds(mb.center + offset, mb.size);
                if (!any) { total = b; any = true; } else total.Encapsulate(b);
            }
            return total;
        }

        /// <summary>Modelin geometrisi: yazı yuvalarının TMP mesh'leri hariç.</summary>
        public static IEnumerable<MeshFilter> GeometryFilters(Rev3Model model) =>
            model.GetComponentsInChildren<MeshFilter>(true).Where(mf => mf.sharedMesh != null && mf.GetComponent<TMP_Text>() == null);

        static Vector3 Abs(this Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        static float MaxComponent(this Vector3 v) => Mathf.Max(v.x, Mathf.Max(v.y, v.z));
    }
}
