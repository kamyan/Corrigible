using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Game.Raid;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Bina kitinin okunurluk kapısı (BASKIN-MEKANLARI Katman 3 › Kabul kapısı). Bir yüzün rengi, dokusunun doğrusal ortalaması ×
    /// malzemenin _BaseColor'ı (lamba altındaki ortalama yansıtma); renk farkı CIE76 ΔE*ab (sRGB → CIE Lab, D65). Sınırlar:
    /// <list type="bullet">
    /// <item>üst yüz L* 58–74; batı ve güney yüzü (dik istif dahil) L* ≥ 40; kanal ve kuyu L* ≤ 30 (koyu oluk: bakır L* 53 ve
    /// turuncu şerit L* 45 üstünde okunur);</item>
    /// <item>bütün yüzlerde doygunluk C* ≤ 22; ahşap piyon (PawnWood) ve kontrplağa ΔE ≥ 15; ayrılmış renklere (YZ kırmızıları,
    /// sivil mavisi, iyi yeşili, tehlike turuncusu, eğitim sarısı) ΔE ≥ 20;</item>
    /// <item>duvar yüzlerinde desen kontrastı ΔL* ≤ 15 (dokunun × _BaseColor L* dağılımında %5 ile %95 arası);</item>
    /// <item>URP Lit, mat (smoothness ≤ 0,15; kanal ve kuyuda su için ≤ 0,35), metal değil, ışımaz;</item>
    /// <item>duvar karosu her maskede (ayrık, raf) en çok 3 etkin renderer.</item>
    /// </list>
    /// <see cref="RaidKitTextureBaker"/> kapıdan geçmeyen kiti kütüphaneye bağlamaz; RaidRoomKitTests aynı kapıyı her kite uygular.
    /// </summary>
    public static class RaidKitGate
    {
        public const float TopLMin = 58, TopLMax = 74, SideLMin = 40, FloorLMax = 30, ChromaMax = 22;
        public const float WoodDeltaEMin = 15, ReservedDeltaEMin = 20, PatternDeltaLMax = 15;
        public const float SmoothnessMax = 0.15f, WaterSmoothnessMax = 0.35f;
        public const int RendererBudget = 3;

        /// <summary>Ahşap: piyonlar (PawnWood) ve kontrplak taban (raid_tokens.json › colors.plywood*).</summary>
        public static readonly (string name, Color color)[] Wood =
        {
            ("PawnWood", Hex("C9A77A")), ("plywoodTop", Hex("C9A36B")), ("plywoodWest", Hex("B38B57")), ("plywoodSouth", Hex("93703F")),
        };

        /// <summary>Ayrılmış renkler (BASKIN-MEKANLARI › Ayrılmış işaretler): yalnız kendi anlamları için.</summary>
        public static readonly (string name, Color color)[] Reserved =
        {
            ("aiRed", Hex("9A1E14")), ("intentRed", Hex("B8241A")), ("standeeRule", Hex("D2483F")), ("civilianBlue", Hex("2C4C8E")),
            ("good", Hex("24592E")), ("hazard", Hex("A9520A")), ("training", Hex("F5D646")),
        };

        public enum FaceKind { Top, Side, Floor }

        /// <summary>Bir yüzün ölçümü.</summary>
        public sealed class Face
        {
            public string Name;
            public FaceKind Kind;
            public Material Material;
            /// <summary>Yüzün ortalama rengi (sRGB): doku ortalaması × _BaseColor, doğrusal uzayda.</summary>
            public Color Mean;
            public Vector3 Lab;
            public float Chroma => Mathf.Sqrt(Lab.y * Lab.y + Lab.z * Lab.z);
            /// <summary>Desen: dokunun × _BaseColor L* dağılımında %5 ve %95 (dokusuz yüzde ikisi de ortalamanın L*'ı).</summary>
            public float PatternLow, PatternHigh;
            public float Pattern => PatternHigh - PatternLow;
            public string NearestWood, NearestReserved;
            public float WoodDeltaE, ReservedDeltaE;
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;

        /// <summary>sRGB → CIE Lab (D65, 2°).</summary>
        public static Vector3 Lab(Color srgb)
        {
            var c = srgb.linear;
            double r = c.r, g = c.g, b = c.b;
            double x = (0.4124564 * r + 0.3575761 * g + 0.1804375 * b) / 0.95047;
            double y = 0.2126729 * r + 0.7151522 * g + 0.0721750 * b;
            double z = (0.0193339 * r + 0.1191920 * g + 0.9503041 * b) / 1.08883;
            double F(double t) => t > 216.0 / 24389 ? System.Math.Pow(t, 1.0 / 3) : (24389.0 / 27 * t + 16) / 116;
            double fx = F(x), fy = F(y), fz = F(z);
            return new Vector3((float)(116 * fy - 16), (float)(500 * (fx - fy)), (float)(200 * (fy - fz)));
        }

        /// <summary>CIE76: Lab uzayında Öklid uzaklığı.</summary>
        public static float DeltaE(Color a, Color b) => Vector3.Distance(Lab(a), Lab(b));

        /// <summary>Kitin yedi yüzü: üst, batı, güney, dik istifin batısı ve güneyi, kanal tabanı, kuyu.</summary>
        public static Face[] Faces(RaidRoomKit kit) => new[]
        {
            Measure("wallTop", FaceKind.Top, kit.wallTop),
            Measure("wallWest", FaceKind.Side, kit.wallWest),
            Measure("wallSouth", FaceKind.Side, kit.wallSouth),
            Measure("columnWest", FaceKind.Side, kit.columnWest),
            Measure("columnSouth", FaceKind.Side, kit.columnSouth),
            Measure("channelFloor", FaceKind.Floor, kit.channelFloor),
            Measure("wellFloor", FaceKind.Floor, kit.wellFloor),
        };

        public static Face Measure(string name, FaceKind kind, Material m)
        {
            var face = new Face { Name = name, Kind = kind, Material = m };
            if (m == null) return face;
            var tint = (m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white).linear;
            var px = Pixels(m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") as Texture2D : null);
            double r = 0, g = 0, b = 0;
            var ls = new float[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                var lin = ((Color)px[i]).linear;
                r += lin.r; g += lin.g; b += lin.b;
                ls[i] = Lab(new Color(lin.r * tint.r, lin.g * tint.g, lin.b * tint.b).gamma).x;
            }
            int n = px.Length;
            face.Mean = new Color((float)(r / n) * tint.r, (float)(g / n) * tint.g, (float)(b / n) * tint.b).gamma;
            face.Lab = Lab(face.Mean);
            System.Array.Sort(ls);
            face.PatternLow = ls[Mathf.Clamp(Mathf.FloorToInt(0.05f * (n - 1)), 0, n - 1)];
            face.PatternHigh = ls[Mathf.Clamp(Mathf.CeilToInt(0.95f * (n - 1)), 0, n - 1)];
            (face.NearestWood, face.WoodDeltaE) = Nearest(face.Mean, Wood);
            (face.NearestReserved, face.ReservedDeltaE) = Nearest(face.Mean, Reserved);
            return face;
        }

        static (string, float) Nearest(Color c, (string name, Color color)[] set)
        {
            var best = set.Select(s => (s.name, de: DeltaE(c, s.color))).OrderBy(s => s.de).First();
            return (best.name, best.de);
        }

        /// <summary>Dokunun sRGB pikselleri (PNG dosyasından, içe aktarmanın sıkıştırmasından bağımsız); dokusuz yüzde tek beyaz piksel.</summary>
        public static Color32[] Pixels(Texture2D tex)
        {
            if (tex == null) return new[] { new Color32(255, 255, 255, 255) };
            string path = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new FileNotFoundException("dokunun dosyası yok", path);
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!t.LoadImage(File.ReadAllBytes(path))) throw new IOException("doku okunamadı: " + path);
                return t.GetPixels32();
            }
            finally { Object.DestroyImmediate(t); }
        }

        /// <summary>Kapı: kitin bütün sınırları. Boş liste: kit geçer.</summary>
        public static List<string> Check(RaidRoomKit kit, RaidPieceLibrary lib)
        {
            var failures = new List<string>();
            foreach (var f in Faces(kit)) failures.AddRange(CheckFace(kit.id, f));
            failures.AddRange(CheckRenderers(kit, lib));
            return failures;
        }

        /// <summary>Bir yüzün renk, desen ve malzeme sınırları.</summary>
        public static List<string> CheckFace(string kitId, Face f)
        {
            var failures = new List<string>();
            string at = kitId + " " + f.Name;
            if (f.Material == null) { failures.Add(at + ": malzeme yok"); return failures; }
            float L = f.Lab.x;
            if (f.Kind == FaceKind.Top && (L < TopLMin || L > TopLMax)) failures.Add($"{at}: L* {L:F1} (üst yüz {TopLMin}–{TopLMax})");
            if (f.Kind == FaceKind.Side && L < SideLMin) failures.Add($"{at}: L* {L:F1} (yan yüz ≥ {SideLMin})");
            if (f.Kind == FaceKind.Floor && L > FloorLMax) failures.Add($"{at}: L* {L:F1} (kanal ≤ {FloorLMax})");
            if (f.Chroma > ChromaMax) failures.Add($"{at}: C* {f.Chroma:F1} (≤ {ChromaMax})");
            if (f.WoodDeltaE < WoodDeltaEMin) failures.Add($"{at}: {f.NearestWood} ΔE {f.WoodDeltaE:F1} (≥ {WoodDeltaEMin})");
            if (f.ReservedDeltaE < ReservedDeltaEMin) failures.Add($"{at}: {f.NearestReserved} ΔE {f.ReservedDeltaE:F1} (≥ {ReservedDeltaEMin})");
            if (f.Kind != FaceKind.Floor && f.Pattern > PatternDeltaLMax) failures.Add($"{at}: desen ΔL* {f.Pattern:F1} (≤ {PatternDeltaLMax})");
            failures.AddRange(CheckMaterial(at, f.Material, f.Kind == FaceKind.Floor ? WaterSmoothnessMax : SmoothnessMax));
            return failures;
        }

        public static List<string> CheckMaterial(string at, Material m, float smoothnessMax)
        {
            var failures = new List<string>();
            if (m.shader == null || m.shader.name != "Universal Render Pipeline/Lit") failures.Add(at + ": URP Lit değil (" + m.shader?.name + ")");
            if (m.HasProperty("_Smoothness") && m.GetFloat("_Smoothness") > smoothnessMax + 1e-4f) failures.Add($"{at}: smoothness {m.GetFloat("_Smoothness"):F2} (≤ {smoothnessMax})");
            if (m.HasProperty("_Metallic") && m.GetFloat("_Metallic") > 1e-4f) failures.Add(at + ": metalik");
            if (m.IsKeywordEnabled("_EMISSION")) failures.Add(at + ": ışıma (_EMISSION) açık");
            if (m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 1e-3f) failures.Add(at + ": ışıma rengi siyah değil");
            return failures;
        }

        /// <summary>Duvar karosu bu kitle her maskede, ayrık ve kayar raf hâlinde en çok 3 etkin renderer (gövde + iki köprü; raf: gövde + çark).</summary>
        public static List<string> CheckRenderers(RaidRoomKit kit, RaidPieceLibrary lib)
        {
            var failures = new List<string>();
            string path = lib != null && lib.wall != null ? AssetDatabase.GetAssetPath(lib.wall) : null;
            if (string.IsNullOrEmpty(path)) { failures.Add(kit.id + ": duvar prefab'ı yok"); return failures; }
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var skin = root.GetComponent<RaidTileSkin>();
                if (skin == null) { failures.Add(kit.id + ": Wall.prefab'da RaidTileSkin yok"); return failures; }
                for (int mask = 0; mask < 16; mask++)
                    foreach (bool isolated in new[] { false, true })
                        foreach (int axis in new[] { RaidTileMask.AxisNone, RaidTileMask.AxisNS, RaidTileMask.AxisEW })
                        {
                            skin.Apply(mask, isolated, axis, kit);
                            int on = root.GetComponentsInChildren<Renderer>().Count(r => r.enabled);
                            if (on > RendererBudget) failures.Add($"{kit.id}: maske {mask} ayrık {isolated} raf {axis}: {on} renderer (≤ {RendererBudget})");
                        }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return failures;
        }

        /// <summary>Kitin ölçüm özeti: yüz başına L*, C*, en yakın ahşap ve ayrılmış renk, desen ΔL*.</summary>
        public static string Report(RaidRoomKit kit)
        {
            var lines = Faces(kit).Select(f => f.Material == null ? $"  {f.Name}: malzeme yok" :
                $"  {f.Name}: #{ColorUtility.ToHtmlStringRGB(f.Mean)} L* {f.Lab.x:F1} C* {f.Chroma:F1} · {f.NearestWood} ΔE {f.WoodDeltaE:F1} · {f.NearestReserved} ΔE {f.ReservedDeltaE:F1} · desen ΔL* {f.Pattern:F1}");
            return kit.id + "\n" + string.Join("\n", lines);
        }
    }
}
