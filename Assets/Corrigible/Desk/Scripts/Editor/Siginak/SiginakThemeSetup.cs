using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Sığınak temasını token dosyasından doldurur (Docs/UI_Siginak PROMPT §4): <c>Data/Siginak/tokens_siginak.json</c> →
    /// <c>Resources/SiginakTheme.asset</c>. Kurallar Sade içe aktarıcısıyla aynı: “colors” öneksiz, öteki dallar adıyla,
    /// “_” ile başlayan açıklamalar, “meta”, “_new” ve “reuse” atlanır. Farklar: “hex”li nesnenin öteki alanları da okunur
    /// (malzeme pürüzlülüğü, ışık şiddeti); [renk, kelime] çifti renk + “.edge” metni olur; renk olmayan metinler
    /// <see cref="SiginakTheme.strings"/>'e girer.
    /// </summary>
    public static class SiginakThemeSetup
    {
        public const string DataDir = DeskSpriteManifest.Root + "/Data/Siginak";
        public const string TokensPath = DataDir + "/tokens_siginak.json";
        public const string ThemePath = DeskSpriteManifest.Root + "/Resources/SiginakTheme.asset";

        static readonly Regex HexRx = new Regex(@"^#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.Compiled);
        static readonly HashSet<string> Skipped = new HashSet<string> { "meta", "_new", "reuse" };

        [MenuItem("Corrigible/Sığınak/Tokenları içe aktar")]
        public static void Import()
        {
            var theme = AssetDatabase.LoadAssetAtPath<SiginakTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<SiginakTheme>();
                Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));
                AssetDatabase.CreateAsset(theme, ThemePath);
            }
            var report = Fill(theme, File.ReadAllText(TokensPath));
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            SiginakTheme.Active = null;
            Debug.Log($"[Sığınak] Tokenlar içe aktarıldı: {ThemePath} · {report}");
        }

        /// <summary>İçe aktarmanın sonucu: okunamayan renk yazıları ve sayılar (test bunları boş bekler).</summary>
        public sealed class Report
        {
            public int Colors, Numbers, Strings, Types, Shadows;
            public readonly List<string> BadHex = new List<string>();
            public override string ToString() =>
                $"{Colors} renk, {Numbers} sayı, {Strings} metin, {Types} yazı rolü, {Shadows} gölge" +
                (BadHex.Count > 0 ? ", okunamayan renk: " + string.Join(", ", BadHex) : "");
        }

        sealed class Sink
        {
            public readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();
            public readonly Dictionary<string, float> numbers = new Dictionary<string, float>();
            public readonly Dictionary<string, string> strings = new Dictionary<string, string>();
            public readonly Dictionary<string, List<SadeTheme.ShadowLayer>> shadows = new Dictionary<string, List<SadeTheme.ShadowLayer>>();
            public readonly Report report = new Report();
        }

        /// <summary>JSON metnini verilen temaya yazar (asset kaydetmez); test aynı yolu kullanır.</summary>
        public static Report Fill(SiginakTheme theme, string json)
        {
            var root = JObject.Parse(json);
            RestoreRemoved(root);
            var sink = new Sink();
            var types = new List<SadeTheme.TypeStyle>();
            foreach (var p in root.Properties())
            {
                if (Skipped.Contains(p.Name) || p.Name.StartsWith("_")) continue;
                switch (p.Name)
                {
                    case "colors":
                        Flatten(p.Value, "", sink);
                        break;
                    case "type":
                        foreach (var t in ((JObject)p.Value).Properties())
                        {
                            if (t.Name.StartsWith("_") || !(t.Value is JArray a) || a.Count < 4) continue;
                            string family = (string)a[0];
                            types.Add(new SadeTheme.TypeStyle
                            {
                                role = t.Name, font = Font(family, (int)a[1]), size = (float)a[2], letterSpacingEm = (float)a[3],
                            });
                            if (!Mapped(family)) sink.strings["type." + t.Name + ".fallback"] = family;
                        }
                        break;
                    case "shadows":
                        foreach (var s in ((JObject)p.Value).Properties())
                            if (!s.Name.StartsWith("_")) Shadow(s.Value, s.Name, sink);
                        break;
                    default:
                        Flatten(p.Value, p.Name, sink);
                        break;
                }
            }

            theme.colors = new List<TokenTheme.NamedColor>();
            foreach (var kv in sink.colors) theme.colors.Add(new TokenTheme.NamedColor { key = kv.Key, color = kv.Value });
            theme.numbers = new List<TokenTheme.NamedFloat>();
            foreach (var kv in sink.numbers) theme.numbers.Add(new TokenTheme.NamedFloat { key = kv.Key, value = kv.Value });
            theme.strings = new List<SiginakTheme.NamedString>();
            foreach (var kv in sink.strings) theme.strings.Add(new SiginakTheme.NamedString { key = kv.Key, value = kv.Value });
            theme.type = types;
            theme.shadows = new List<SadeTheme.NamedShadow>();
            foreach (var kv in sink.shadows) theme.shadows.Add(new SadeTheme.NamedShadow { key = kv.Key, layers = kv.Value.ToArray() });
            theme.Invalidate();

            var r = sink.report;
            r.Colors = sink.colors.Count; r.Numbers = sink.numbers.Count; r.Strings = sink.strings.Count;
            r.Types = types.Count; r.Shadows = sink.shadows.Count;
            return r;
        }

        /// <summary>
        /// Token 1.3 (Revizyon 3, ek 11-05) kartlı oyundan çıkan değerleri <c>{"removed": true}</c> düğümüne çevirir; eski
        /// değerleri “_was_1_3”te durur. Eski yol (<c>planningBoardEnabled = false</c>) bunları hâlâ okuduğu için kaldırılmış
        /// düğüm eski değeriyle yerine konur (Rev3 IMPLEMENTATION_MAP › Soru 5). Kaldırılmamış, yalnız değişmiş yollar
        /// (ör. <c>cameras.K4</c>) yeni değeriyle kalır.
        /// </summary>
        static void RestoreRemoved(JObject root)
        {
            if (!(root["_was_1_3"] is JObject was)) return;
            foreach (var p in was.Properties())
            {
                var keys = p.Name.Split('.');
                JObject parent = root;
                for (int i = 0; i < keys.Length - 1 && parent != null; i++) parent = parent[keys[i]] as JObject;
                if (parent == null) continue;
                string last = keys[keys.Length - 1];
                if (parent[last] is JObject node && node["removed"] is JValue removed && removed.Type == JTokenType.Boolean && (bool)removed)
                    parent[last] = p.Value.DeepClone();
            }
        }

        static string Join(string path, string name) => string.IsNullOrEmpty(path) ? name : path + "." + name;

        static void Flatten(JToken tok, string path, Sink sink)
        {
            switch (tok)
            {
                case JObject o:
                    if (o["hex"] is JValue h && h.Type == JTokenType.String)
                    {
                        sink.colors[path] = Hex((string)h, o["alpha"] != null ? (float)o["alpha"] : 1, path, sink);
                        foreach (var p in o.Properties())
                            if (p.Name != "hex" && p.Name != "alpha" && !p.Name.StartsWith("_")) Flatten(p.Value, Join(path, p.Name), sink);
                        return;
                    }
                    foreach (var p in o.Properties())
                        if (!p.Name.StartsWith("_")) Flatten(p.Value, Join(path, p.Name), sink);
                    return;
                case JArray a:
                    // Kenar: [genişlik, “#hex”]
                    if (a.Count == 2 && IsNum(a[0]) && IsHex(a[1]))
                    {
                        sink.colors[path] = Hex((string)a[1], 1, path, sink);
                        sink.numbers[path + ".width"] = (float)a[0];
                        return;
                    }
                    // Damga: [“#hex”, kenar deseni]
                    if (a.Count == 2 && IsHex(a[0]) && a[1].Type == JTokenType.String && !IsHex(a[1]))
                    {
                        sink.colors[path] = Hex((string)a[0], 1, path, sink);
                        sink.strings[path + ".edge"] = (string)a[1];
                        return;
                    }
                    // Gölge: [x, y, “#hex”, α]
                    if (IsShadow(a))
                    {
                        Shadow(a, path, sink);
                        sink.colors[path] = Hex((string)a[2], (float)a[3], path, sink);
                        return;
                    }
                    for (int i = 0; i < a.Count; i++) Flatten(a[i], path + "[" + i + "]", sink);
                    return;
                case JValue v:
                    if (v.Type == JTokenType.String)
                    {
                        var s = (string)v;
                        if (s.StartsWith("#")) sink.colors[path] = Hex(s, 1, path, sink);
                        else sink.strings[path] = s;
                    }
                    else if (v.Type == JTokenType.Integer || v.Type == JTokenType.Float) sink.numbers[path] = (float)v;
                    else if (v.Type == JTokenType.Boolean) sink.numbers[path] = (bool)v ? 1 : 0;
                    return;
            }
        }

        static void Shadow(JToken tok, string key, Sink sink)
        {
            if (!(tok is JArray a)) return;
            var layers = new List<SadeTheme.ShadowLayer>();
            if (IsShadow(a)) layers.Add(Layer(a, key, sink));
            else foreach (var l in a) if (l is JArray la && IsShadow(la)) layers.Add(Layer(la, key, sink));
            if (layers.Count > 0) sink.shadows[key] = layers;
        }

        static SadeTheme.ShadowLayer Layer(JArray a, string key, Sink sink) =>
            new SadeTheme.ShadowLayer { offset = new Vector2((float)a[0], (float)a[1]), color = Hex((string)a[2], (float)a[3], key, sink) };

        static bool IsShadow(JArray a) => a.Count == 4 && IsNum(a[0]) && IsNum(a[1]) && IsHex(a[2]) && IsNum(a[3]);
        static bool IsNum(JToken t) => t.Type == JTokenType.Integer || t.Type == JTokenType.Float;
        static bool IsHex(JToken t) => t.Type == JTokenType.String && ((string)t).StartsWith("#");

        static Color Hex(string hex, float alpha, string path, Sink sink)
        {
            if (!HexRx.IsMatch(hex) || !ColorUtility.TryParseHtmlString(hex, out var c))
            {
                sink.report.BadHex.Add(path + " = " + hex);
                return Color.magenta;
            }
            c.a = hex.Length == 9 ? c.a * alpha : alpha;
            return c;
        }

        static bool Mapped(string family) => family.StartsWith("Courier") || family.StartsWith("Kalam") || family.StartsWith("Libre Franklin")
            || family.StartsWith("Big Shoulders Stencil");

        /// <summary>
        /// Aile + ağırlık → masa fontu. Big Shoulders Stencil Revizyon 3'te eklendi (tek ağırlık, 800); projede olmayan başka
        /// aile Courier Prime 700'e düşer ve asıl aile “type.&lt;rol&gt;.fallback” metninde saklanır.
        /// </summary>
        static DeskFont Font(string family, int weight)
        {
            if (family.StartsWith("Courier")) return weight >= 700 ? DeskFont.CpBold : DeskFont.CpRegular;
            if (family.StartsWith("Kalam")) return weight >= 700 ? DeskFont.KalamBold : DeskFont.KalamRegular;
            if (family.StartsWith("Big Shoulders Stencil")) return DeskFont.StencilExtraBold;
            if (!family.StartsWith("Libre Franklin")) return DeskFont.CpBold;
            return weight >= 800 ? DeskFont.LfExtraBold : weight >= 700 ? DeskFont.LfBold : weight >= 600 ? DeskFont.LfSemiBold
                : weight >= 500 ? DeskFont.LfMedium : DeskFont.LfRegular;
        }
    }
}
