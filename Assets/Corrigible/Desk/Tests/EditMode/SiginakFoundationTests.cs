using System.IO;
using System.Linq;
using Corrigible.Desk.EditorTools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 1: token içe aktarımı, font glifleri ve sprite içe aktarma ayarları.</summary>
    public class SiginakFoundationTests
    {
        static string Json => File.ReadAllText(SiginakThemeSetup.TokensPath);

        static SiginakTheme Fresh(out SiginakThemeSetup.Report report)
        {
            var theme = ScriptableObject.CreateInstance<SiginakTheme>();
            report = SiginakThemeSetup.Fill(theme, Json);
            return theme;
        }

        static bool HasKey(SiginakTheme t, string key) =>
            t.colors.Any(c => Matches(c.key, key)) || t.numbers.Any(n => Matches(n.key, key)) || t.strings.Any(s => Matches(s.key, key));

        static bool Matches(string have, string key) => have == key || have.StartsWith(key + ".") || have.StartsWith(key + "[");

        [Test]
        public void Tokens_AllColorsParse()
        {
            var theme = Fresh(out var report);
            try
            {
                Assert.IsEmpty(report.BadHex, "okunamayan renkler");
                int hexInJson = 0;
                void Count(JToken t, bool skip)
                {
                    if (skip) return;
                    if (t is JObject o) foreach (var p in o.Properties()) Count(p.Value, p.Name.StartsWith("_") || p.Name == "meta" || p.Name == "reuse");
                    else if (t is JArray a) foreach (var x in a) Count(x, false);
                    else if (t is JValue v && v.Type == JTokenType.String && ((string)v).StartsWith("#")) hexInJson++;
                }
                Count(JObject.Parse(Json), false);
                // Her renk yazısı bir renk anahtarı ya da gölge katmanı olur (dal dışındaki gölgeler ikisi birden)
                int parsed = theme.colors.Count + theme.shadows.Sum(s => s.layers.Length);
                Assert.GreaterOrEqual(parsed, hexInJson, "JSON'daki renklerin hepsi temaya girmeli");
                Assert.IsFalse(theme.colors.Any(c => c.color == Color.magenta), "eflatun renk");
            }
            finally { Object.DestroyImmediate(theme); }
        }

        [Test]
        public void Tokens_EveryNewPathResolves()
        {
            var theme = Fresh(out _);
            try
            {
                var paths = ((JArray)JObject.Parse(Json)["_new"]).Select(p => (string)p).ToList();
                Assert.Greater(paths.Count, 250);
                var missing = paths.Where(p =>
                    p.StartsWith("colors.") ? !HasKey(theme, p.Substring(7)) :
                    p.StartsWith("type.") ? !theme.type.Any(t => t.role == p.Substring(5)) :
                    p.StartsWith("shadows.") ? !theme.shadows.Any(s => s.key == p.Substring(8)) :
                    !HasKey(theme, p)).ToList();
                Assert.IsEmpty(missing, "temada karşılığı olmayan yeni tokenlar: " + string.Join(", ", missing));
            }
            finally { Object.DestroyImmediate(theme); }
        }

        [Test]
        public void Tokens_ReusedSadeKeysExist()
        {
            var sade = SadeTheme.Active;
            Assert.IsNotNull(sade);
            var reuse = (JArray)JObject.Parse(Json)["reuse"]["colors"];
            var missing = reuse.Select(r => (string)r).Where(k => k.EndsWith(".*")
                ? !sade.colors.Any(c => c.key.StartsWith(k.Substring(0, k.Length - 1)))
                : !sade.TryGet(k, out _) && !sade.colors.Any(c => c.key.StartsWith(k + "["))).ToList();   // [gövde, kenar] çiftleri
            Assert.IsEmpty(missing, "Sade temasında olmayan ortak anahtarlar: " + string.Join(", ", missing));
            foreach (var shadow in new[] { "card", "hoverCard", "plaque", "eventSlip", "topButton", "action", "actionHover" })
                Assert.IsNotEmpty(sade.Shadow(shadow), "Sade gölgesi: " + shadow);
        }

        [Test]
        public void Tokens_SpotValues()
        {
            var t = Fresh(out _);
            try
            {
                Assert.AreEqual("B8955A", ColorUtility.ToHtmlStringRGB(t.ColorOf("brass.metal")));
                Assert.AreEqual(0.3f, t.ColorOf("crt.scanline").a, 1e-4);
                Assert.AreEqual("6E5A40", ColorUtility.ToHtmlStringRGB(t.ColorOf("bunker.soil[0]")));
                Assert.AreEqual("D9C08B", ColorUtility.ToHtmlStringRGB(t.ColorOf("ui.folder[0]")));
                Assert.AreEqual("round", t.Text("stamp.free.edge"));
                Assert.AreEqual("1A4789", ColorUtility.ToHtmlStringRGB(t.ColorOf("stamp.free")));
                Assert.AreEqual(5, t.Number("mapLiberation.frontContour.width"));
                Assert.AreEqual(-45, t.Number("mapLiberation.occupiedHatch[1].angle"));
                Assert.AreEqual(0.32f, t.ColorOf("mapLiberation.occupiedHatch[1]").a, 1e-4);
                Assert.AreEqual(new Vector3(0, 1.52f, -0.15f), t.Vector("cameras.K1.pos"));
                Assert.AreEqual(56.7f, t.Number("cameras.K1.fovV"), 1e-4);
                Assert.AreEqual(0.36f, t.Number("materials.brass.roughness"), 1e-4);
                Assert.AreEqual("A6E8B8", ColorUtility.ToHtmlStringRGB(t.ColorOf("materials.crtScreen.emissive")));
                Assert.AreEqual(1, t.Number("lights.bulb.shadows"));
                Assert.AreEqual(4, t.Number("motion.coverAutoCloseS"));
                Assert.AreEqual(new Vector2(820, 52), new Vector2(t.Number("space.telexStrip.w"), t.Number("space.telexStrip.h")));
                Assert.IsTrue(t.TryType("plateLabel", out var plate));
                Assert.AreEqual(DeskFont.CpBold, plate.font); Assert.AreEqual(16, plate.size); Assert.AreEqual(0.06f, plate.letterSpacingEm, 1e-4);
                Assert.IsTrue(t.TryType("analystNote", out var note)); Assert.AreEqual(DeskFont.KalamBold, note.font);
                Assert.IsTrue(t.TryType("sectorName", out var sector)); Assert.AreEqual(DeskFont.LfSemiBold, sector.font);
                Assert.IsTrue(t.TryType("wallStencil", out var stencil)); Assert.AreEqual(DeskFont.CpBold, stencil.font, "Big Shoulders yok: CP 700 yedeği");
                Assert.AreEqual("Big Shoulders Stencil Display", t.Text("type.wallStencil.fallback"));
                Assert.IsTrue(t.TryType("hoverTitle", out _), "ortak rol Sade'den okunur");
                var report = t.Shadow("report");
                Assert.AreEqual(1, report.Length); Assert.AreEqual(new Vector2(5, 7), report[0].offset); Assert.AreEqual(0.45f, report[0].color.a, 1e-4);
                Assert.IsNotEmpty(t.Shadow("hoverCard"), "ortak gölge Sade'den okunur");
                Assert.AreEqual("1E1B16", ColorUtility.ToHtmlStringRGB(t.ColorOf("ink")), "ortak renk Sade'den okunur");
            }
            finally { Object.DestroyImmediate(t); }
        }

        /// <summary>Resources'taki asset JSON'la güncel olmalı (token değişince “Tokenları içe aktar” çalıştırılmalı).</summary>
        [Test]
        public void Tokens_AssetIsUpToDate()
        {
            var asset = SiginakTheme.Active;
            Assert.IsNotNull(asset, "Resources/SiginakTheme.asset yok");
            var fresh = Fresh(out _);
            try
            {
                CollectionAssert.AreEquivalent(fresh.colors.Select(c => c.key + "=" + c.color), asset.colors.Select(c => c.key + "=" + c.color));
                CollectionAssert.AreEquivalent(fresh.numbers.Select(n => n.key + "=" + n.value), asset.numbers.Select(n => n.key + "=" + n.value));
                CollectionAssert.AreEquivalent(fresh.strings.Select(s => s.key + "=" + s.value), asset.strings.Select(s => s.key + "=" + s.value));
                CollectionAssert.AreEquivalent(fresh.type.Select(s => s.role + s.font + s.size), asset.type.Select(s => s.role + s.font + s.size));
                Assert.AreEqual(fresh.shadows.Count, asset.shadows.Count);
            }
            finally { Object.DestroyImmediate(fresh); }
        }

        /// <summary>Sığınak kodunda geçen her tema anahtarı (ColorOf / Number / NumberOf / Vector / Shadow / ApplyType) çözülür.</summary>
        [Test]
        public void ThemeKeys_UsedInSiginakCode_Resolve()
        {
            var theme = SiginakTheme.Active;
            var rx = new System.Text.RegularExpressions.Regex(@"\b(ColorOf|Number|NumberOf|Vector|Vector2Of)\(""([A-Za-z0-9_.\[\]]+)""");
            var files = Directory.GetFiles(DeskSpriteManifest.Root + "/Scripts", "*.cs", SearchOption.AllDirectories)
                .Where(f => f.Replace('\\', '/').Contains("/Siginak/") || Path.GetFileName(f).StartsWith("Siginak")).ToList();
            Assert.Greater(files.Count, 20);
            var missing = files.SelectMany(f => rx.Matches(File.ReadAllText(f)).Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => (kind: m.Groups[1].Value, key: m.Groups[2].Value, file: Path.GetFileName(f))))
                .Where(k => !k.key.EndsWith(".") && !Resolves(theme, k.kind, k.key))
                .Select(k => k.file + ": " + k.key).Distinct().ToList();
            Assert.IsEmpty(missing, "temada olmayan anahtarlar: " + string.Join(", ", missing));
        }

        static bool Resolves(SiginakTheme t, string kind, string key)
        {
            if (kind == "ColorOf") return t.TryGet(key, out _) || SadeTheme.Active.TryGet(key, out _);
            if (kind == "Vector") return t.TryNumber(key + "[0]", out _) && t.TryNumber(key + "[2]", out _);
            if (kind == "Vector2Of") return t.TryNumber(key + "[0]", out _);
            return t.TryNumber(key, out _) || SadeTheme.Active.TryNumber(key, out _);
        }

        /// <summary>PROMPT §2: zorunlu glifler her yüzde statik atlas zincirinde (kendi atlası ya da DejaVu yedeği) bulunur.</summary>
        [Test]
        public void Fonts_CoverRequiredGlyphs()
        {
            var theme = SiginakTheme.Active;
            var desk = DeskTheme.Active;
            var faces = theme.type.Select(t => t.font).Distinct().ToList();
            Assert.That(faces, Is.SupersetOf(new[] { DeskFont.CpRegular, DeskFont.CpBold, DeskFont.LfRegular, DeskFont.LfSemiBold, DeskFont.LfBold, DeskFont.KalamBold }));
            foreach (var face in faces)
            {
                var font = desk.Font(face);
                Assert.IsNotNull(font, face.ToString());
                Assert.AreEqual("", SiginakFontPreview.MissingStatic(font, SiginakFontPreview.RequiredGlyphs), face + " eksik glif");
            }
        }

        [Test]
        public void Sprites_ImportedWithManifestSettings()
        {
            if (Directory.Exists(SiginakSpriteManifest.SourceDir))
            {
                var packaged = Directory.GetFiles(SiginakSpriteManifest.SourceDir, "*.png").Select(Path.GetFileNameWithoutExtension);
                Assert.IsEmpty(packaged.Where(n => SiginakSpriteManifest.Get(n) == null), "tabloda olmayan paket sprite'ı");
            }
            foreach (var e in SiginakSpriteManifest.All)
            {
                var ti = AssetImporter.GetAtPath(e.AssetPath) as TextureImporter;
                Assert.IsNotNull(ti, e.AssetPath);
                Assert.AreEqual(e.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, ti.wrapMode, e.Name);
                Assert.AreEqual(e.Point ? FilterMode.Point : FilterMode.Bilinear, ti.filterMode, e.Name);
                if (e.Kind == SiginakSpriteManifest.Kind.Texture)
                {
                    Assert.AreEqual(TextureImporterType.Default, ti.textureType, e.Name);
                    Assert.IsTrue(ti.mipmapEnabled, e.Name);
                    continue;
                }
                Assert.AreEqual(TextureImporterType.Sprite, ti.textureType, e.Name);
                Assert.AreEqual(SiginakSpriteManifest.PixelsPerUnit, ti.spritePixelsPerUnit, e.Name);
                Assert.AreEqual(Vector4.one * e.Border, ti.spriteBorder, e.Name);
                var s = new TextureImporterSettings();
                ti.ReadTextureSettings(s);
                Assert.AreEqual(SpriteMeshType.FullRect, s.spriteMeshType, e.Name);
            }
        }
    }
}
