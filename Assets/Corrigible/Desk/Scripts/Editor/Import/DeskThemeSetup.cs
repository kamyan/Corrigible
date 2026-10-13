using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary><c>Docs/UI_C/spec/tokens.json</c> → <c>Resources/DeskTheme.asset</c> (renkler, İllüzyon paleti, rampa, fontlar, gölgeler, hareket).</summary>
    public static class DeskThemeSetup
    {
        public const string TokensPath = "Docs/UI_C/spec/tokens.json";
        public const string ThemePath = DeskSpriteManifest.Root + "/Resources/DeskTheme.asset";

        /// <summary>İllüzyonda değişen kâğıt tonları: tema anahtarı ← tokens.json'daki illusion anahtarı.</summary>
        static readonly (string key, string from)[] IllusionMap =
        {
            ("paper.page", "page"), ("paper.pageEdge", "pageEdge"), ("paper.manila", "manila"), ("paper.manilaEdge", "manilaEdge"),
            ("paper.index", "index"), ("paper.telex", "telex"), ("paper.dialFace", "dialFace"), ("advisor.title", "advisorTitle"),
        };

        [MenuItem("Corrigible/Desk UI/Temayı tokens.json'dan doldur")]
        public static void Fill()
        {
            var json = JObject.Parse(File.ReadAllText(TokensPath));
            var theme = AssetDatabase.LoadAssetAtPath<DeskTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<DeskTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            // Renkler: colors altındaki her "#hex[@alfa]" değeri nokta yoluyla anahtar olur
            var colors = new List<DeskTheme.NamedColor>();
            var col = (JObject)json["colors"];
            foreach (var prop in col.Properties())
                if (prop.Name != "infiltrationRamp" && prop.Name != "illusion") Walk(prop.Value, prop.Name, colors);
            // Bileşen tablosundan gelen ek tonlar (tokens.json'da renk ağacında olmayanlar)
            Add(colors, "paper.dialFace", "#F1E9D6");
            Add(colors, "advisor.title", "#9A1E14");
            Add(colors, "button.inkHover", "#35302A");
            Add(colors, "button.inkDisabled", "#8C8476");
            Add(colors, "button.inkDisabledEdge", "#6F6759");
            Add(colors, "button.inkDisabledText", "#ECE5D5");
            Add(colors, "button.paperDisabled", "#E3DACB");
            Add(colors, "button.paperDisabledEdge", "#A89D86");
            Add(colors, "button.linkDisabled", "#8A8170");
            Add(colors, "speed.edge", "#9A9284");
            Add(colors, "speed.disabled", "#3A352E");
            Add(colors, "speed.disabledEdge", "#5E584E");
            Add(colors, "speed.disabledText", "#8A8274");
            Add(colors, "close.disabledIcon", "#9A917F");
            Add(colors, "action.pressed", "#F3EEDF");
            Add(colors, "action.disabled", "#EEE8DA");
            Add(colors, "cal.header", "#FFF3EE");
            Add(colors, "event.slotEdge", "#BDB29A");
            // F8.3: sahte zafer duyurusu (masanın kâğıtlarına bilerek benzemeyen beyaz resmî kâğıt), uyarı fişi kenarı
            Add(colors, "fakewin.paper", "#FBFAF6");
            Add(colors, "fakewin.edge", "#D8D2C2");
            Add(colors, "fakewin.muted", "#5C6B5E");
            Add(colors, "toast.edge", "#CDBE8E");
            Add(colors, "black", "#000000");
            Add(colors, "white", "#FFFFFF");
            // Büyük yarı saydam örtüler (karartma, spot halkası) Linear karışımda açık kalır: sRGB karışımına eşdeğer alfa
            for (int i = 0; i < colors.Count; i++)
            {
                var nc = colors[i];
                if (!nc.key.StartsWith("modal.") || nc.color.a >= 1) continue;
                nc.color.a = DeskMath.LinearOverlayAlpha(nc.color, nc.color.a);
                colors[i] = nc;
            }
            theme.colors = colors;

            var ill = (JObject)col["illusion"];
            theme.illusionOverrides = new List<DeskTheme.NamedColor>();
            foreach (var (key, from) in IllusionMap)
                if (ill[from] is JValue v && TryColor((string)v, out var c)) theme.illusionOverrides.Add(new DeskTheme.NamedColor { key = key, color = c });
            theme.stampOpacityIllusion = (float)ill["stampOpacity"];

            var ramp = new List<DeskTheme.RampStep>();
            foreach (var r in col["infiltrationRamp"])
            {
                TryColor((string)r["fill"], out var fill); TryColor((string)r["text"], out var text);
                ramp.Add(new DeskTheme.RampStep { min = (int)r["min"], max = (int)r["max"], fill = fill, text = text });
            }
            theme.infiltrationRamp = ramp.ToArray();

            // Gölgeler
            var sh = json["shadows"]["presets"];
            theme.s1 = Shadow(sh["S1"]); theme.s2 = Shadow(sh["S2"]); theme.s3 = Shadow(sh["S3"]); theme.s5 = Shadow(sh["S5"]);
            theme.stampOpacity = (float)json["components"]["stamp"]["normal"]["opacity"];
            theme.telexSpeed = (float)json["motion"]["telexSpeedPxPerSec"];

            // Fontlar
            TMP_FontAsset F(string n) => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DeskFontSetup.AssetPath(n));
            theme.cpRegular = F("CP-Regular"); theme.cpBold = F("CP-Bold");
            theme.lfRegular = F("LF-Regular"); theme.lfMedium = F("LF-Medium"); theme.lfSemiBold = F("LF-SemiBold");
            theme.lfBold = F("LF-Bold"); theme.lfExtraBold = F("LF-ExtraBold");
            theme.kalamRegular = F("Kalam-Regular"); theme.kalamBold = F("Kalam-Bold");
            theme.styleSheet = AssetDatabase.LoadAssetAtPath<TMP_StyleSheet>(DeskFontSetup.StyleSheetPath);

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            DeskTheme.Active = theme;
            Debug.Log($"[Desk] Tema dolduruldu: {colors.Count} renk, {theme.illusionOverrides.Count} İllüzyon tonu, {ramp.Count} rampa adımı.");
        }

        static void Walk(JToken t, string path, List<DeskTheme.NamedColor> list)
        {
            if (t is JObject o) { foreach (var p in o.Properties()) Walk(p.Value, path + "." + p.Name, list); return; }
            if (t is JValue v && v.Type == JTokenType.String && TryColor((string)v, out var c))
                list.Add(new DeskTheme.NamedColor { key = path, color = c });
        }

        static void Add(List<DeskTheme.NamedColor> list, string key, string hex)
        {
            if (list.Exists(c => c.key == key)) return;
            TryColor(hex, out var c);
            list.Add(new DeskTheme.NamedColor { key = key, color = c });
        }

        /// <summary>“#RRGGBB”, “#RRGGBB@0.35”, “#RRGGBB@0.22 (açıklama)”, “#RRGGBB / …”.</summary>
        public static bool TryColor(string s, out Color c)
        {
            c = default;
            if (string.IsNullOrEmpty(s) || s[0] != '#' || s.Length < 7) return false;
            if (!ColorUtility.TryParseHtmlString(s.Substring(0, 7), out c)) return false;
            if (s.Length > 8 && s[7] == '@')
            {
                int end = 8;
                while (end < s.Length && (char.IsDigit(s[end]) || s[end] == '.')) end++;
                if (float.TryParse(s.Substring(8, end - 8), NumberStyles.Float, CultureInfo.InvariantCulture, out var a)) c.a = a;
            }
            return true;
        }

        static DeskTheme.ShadowSpec Shadow(JToken t)
        {
            TryColor((string)t["color"], out var c);
            c.a = (float)t["alpha"];
            return new DeskTheme.ShadowSpec { offset = new Vector2((float)t["x"], (float)t["y"]), color = c };
        }
    }
}
