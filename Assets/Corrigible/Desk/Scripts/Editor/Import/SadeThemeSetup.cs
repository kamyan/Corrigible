using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Sade ve baskın temalarını token dosyalarından doldurur (Sade PROMPT §4.3): <c>Data/Sade/tokens_sade.json</c> →
    /// <c>Resources/SadeTheme.asset</c>, <c>Data/Sade/raid_tokens.json</c> → <c>Resources/RaidTheme.asset</c>.
    /// Renkler ve sayılar JSON yoluyla düzleşir (tokens_sade “colors” dalı öneksiz: “status.threat”; diğer dallar adıyla:
    /// “desk.bar.fill.alignment”). Kenar dizisi [genişlik, “#hex”] renk + “.width” sayısı olur; [x, y, “#hex”, α] gölge olur.
    /// </summary>
    public static class SadeThemeSetup
    {
        public const string DataDir = DeskSpriteManifest.Root + "/Data/Sade";
        public const string SadeTokensPath = DataDir + "/tokens_sade.json";
        public const string RaidTokensPath = DataDir + "/raid_tokens.json";
        public const string SadeThemePath = DeskSpriteManifest.Root + "/Resources/SadeTheme.asset";
        public const string RaidThemePath = DeskSpriteManifest.Root + "/Resources/RaidTheme.asset";

        // Tokenlarda olmayıp ölçü tablosundan (TSV) alınan değerler; KARARLAR.md › F1
        static readonly (string role, DeskFont font, float size, float em)[] ExtraTypes =
        {
            ("chipSlip", DeskFont.CpBold, 11, 0.05f),        // “~” fişi: CP 700 11, harf aralığı 0,55 px
            ("chipQuestion", DeskFont.KalamBold, 18, 0),     // etiket satırındaki şüpheli “?”
        };
        static readonly (string key, string hex, float alpha)[] ExtraColors =
        {
            ("desk.time.calHeadText", "#FFF3EE", 1),         // takvim başlığı yazısı
            ("desk.dock.tabBorder", "#000000", 1),           // sekme kenarı (kapalı α .55, açık 1)
            ("raid.worldLabels.spentPaper", "#E9DDC6", 1),   // turu biten figürün can fişi (B.3)
        };
        static readonly (string key, float x, float y, string hex, float alpha)[] ExtraShadows =
        {
            ("dockTab", 2, 0, "#000000", 0.28f),             // sekmenin sağ gölgesi
        };

        static readonly Regex HexRx = new Regex(@"^#([0-9A-Fa-f]{6}(?:[0-9A-Fa-f]{2})?)(?:.*?α\s*([0-9]*[.,]?[0-9]+))?", RegexOptions.Compiled);

        [MenuItem("Corrigible/Sade UI/Tokenları içe aktar")]
        public static void Import()
        {
            ImportSade();
            ImportRaid();
            AssetDatabase.SaveAssets();
            Debug.Log("[Sade] Tokenlar içe aktarıldı: " + SadeThemePath + ", " + RaidThemePath);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        sealed class Sink
        {
            public readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();
            public readonly Dictionary<string, float> numbers = new Dictionary<string, float>();
            public readonly Dictionary<string, List<SadeTheme.ShadowLayer>> shadows = new Dictionary<string, List<SadeTheme.ShadowLayer>>();
        }

        static void ImportSade()
        {
            var theme = LoadOrCreate<SadeTheme>(SadeThemePath);
            var root = JObject.Parse(File.ReadAllText(SadeTokensPath));
            var sink = new Sink();
            var types = new List<SadeTheme.TypeStyle>();
            foreach (var p in root.Properties())
            {
                switch (p.Name)
                {
                    case "meta": break;
                    case "type":
                        foreach (var t in ((JObject)p.Value).Properties())
                        {
                            if (!(t.Value is JArray a) || a.Count < 4) continue;
                            types.Add(new SadeTheme.TypeStyle
                            {
                                role = t.Name, font = Font((string)a[0], (int)a[1]), size = (float)a[2], letterSpacingEm = (float)a[3],
                            });
                        }
                        break;
                    case "shadows":
                        foreach (var s in ((JObject)p.Value).Properties()) Shadow(s.Value, s.Name, sink);
                        break;
                    case "colors":
                        Flatten(p.Value, "", sink);
                        break;
                    default:
                        Flatten(p.Value, p.Name, sink);
                        break;
                }
            }
            foreach (var x in ExtraTypes) { types.RemoveAll(t => t.role == x.role); types.Add(new SadeTheme.TypeStyle { role = x.role, font = x.font, size = x.size, letterSpacingEm = x.em }); }
            foreach (var x in ExtraColors) sink.colors[x.key] = Hex(x.hex, x.alpha);
            foreach (var x in ExtraShadows) sink.shadows[x.key] = new List<SadeTheme.ShadowLayer> { new SadeTheme.ShadowLayer { offset = new Vector2(x.x, x.y), color = Hex(x.hex, x.alpha) } };

            Fill(theme, sink);
            theme.type = types;
            theme.shadows = new List<SadeTheme.NamedShadow>();
            foreach (var kv in sink.shadows) theme.shadows.Add(new SadeTheme.NamedShadow { key = kv.Key, layers = kv.Value.ToArray() });
            theme.Invalidate();
            EditorUtility.SetDirty(theme);
        }

        static void ImportRaid()
        {
            var theme = LoadOrCreate<RaidTheme>(RaidThemePath);
            var root = JObject.Parse(File.ReadAllText(RaidTokensPath));
            var sink = new Sink();
            foreach (var p in root.Properties()) if (p.Name != "meta") Flatten(p.Value, p.Name, sink);
            Fill(theme, sink);
            theme.Invalidate();
            EditorUtility.SetDirty(theme);
        }

        static void Fill(TokenTheme theme, Sink sink)
        {
            theme.colors = new List<TokenTheme.NamedColor>();
            foreach (var kv in sink.colors) theme.colors.Add(new TokenTheme.NamedColor { key = kv.Key, color = kv.Value });
            theme.numbers = new List<TokenTheme.NamedFloat>();
            foreach (var kv in sink.numbers) theme.numbers.Add(new TokenTheme.NamedFloat { key = kv.Key, value = kv.Value });
        }

        static string Join(string path, string name) => string.IsNullOrEmpty(path) ? name : path + "." + name;

        static void Flatten(JToken tok, string path, Sink sink)
        {
            switch (tok)
            {
                case JObject o:
                    if (o["hex"] is JValue h && h.Type == JTokenType.String)
                    {
                        sink.colors[path] = Hex((string)h, o["alpha"] != null ? (float)o["alpha"] : 1);
                        return;
                    }
                    foreach (var p in o.Properties()) Flatten(p.Value, Join(path, p.Name), sink);
                    return;
                case JArray a:
                    // Kenar: [genişlik, “#hex”]
                    if (a.Count == 2 && IsNum(a[0]) && IsHex(a[1]))
                    {
                        sink.colors[path] = Hex((string)a[1], 1);
                        sink.numbers[path + ".width"] = (float)a[0];
                        return;
                    }
                    // Gölge: [x, y, “#hex”, α] ya da bunların listesi
                    if (IsShadow(a) || (a.Count > 0 && a[0] is JArray first && IsShadow(first)))
                    {
                        Shadow(a, path, sink);
                        if (IsShadow(a)) sink.colors[path] = Hex((string)a[2], (float)a[3]);
                        return;
                    }
                    for (int i = 0; i < a.Count; i++) Flatten(a[i], path + "[" + i + "]", sink);
                    return;
                case JValue v:
                    if (v.Type == JTokenType.String)
                    {
                        var m = HexRx.Match((string)v);
                        if (m.Success) sink.colors[path] = Hex("#" + m.Groups[1].Value, m.Groups[2].Success ? Num(m.Groups[2].Value) : 1);
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
            if (IsShadow(a)) layers.Add(Layer(a));
            else foreach (var l in a) if (l is JArray la && IsShadow(la)) layers.Add(Layer(la));
            if (layers.Count > 0) sink.shadows[key] = layers;
        }

        static SadeTheme.ShadowLayer Layer(JArray a) =>
            new SadeTheme.ShadowLayer { offset = new Vector2((float)a[0], (float)a[1]), color = Hex((string)a[2], (float)a[3]) };

        static bool IsShadow(JArray a) => a.Count == 4 && IsNum(a[0]) && IsNum(a[1]) && IsHex(a[2]) && IsNum(a[3]);
        static bool IsNum(JToken t) => t.Type == JTokenType.Integer || t.Type == JTokenType.Float;
        static bool IsHex(JToken t) => t.Type == JTokenType.String && ((string)t).StartsWith("#");
        static float Num(string s) => float.Parse(s.Replace(',', '.').StartsWith(".") ? "0" + s.Replace(',', '.') : s.Replace(',', '.'), CultureInfo.InvariantCulture);

        static Color Hex(string hex, float alpha)
        {
            ColorUtility.TryParseHtmlString(hex.Length > 9 ? hex.Substring(0, 7) : hex, out var c);
            c.a = hex.Length == 9 ? c.a * alpha : alpha;
            return c;
        }

        static DeskFont Font(string family, int weight)
        {
            if (family.StartsWith("Courier")) return weight >= 700 ? DeskFont.CpBold : DeskFont.CpRegular;
            if (family.StartsWith("Kalam")) return weight >= 700 ? DeskFont.KalamBold : DeskFont.KalamRegular;
            return weight >= 800 ? DeskFont.LfExtraBold : weight >= 700 ? DeskFont.LfBold : weight >= 600 ? DeskFont.LfSemiBold
                : weight >= 500 ? DeskFont.LfMedium : DeskFont.LfRegular;
        }
    }
}
