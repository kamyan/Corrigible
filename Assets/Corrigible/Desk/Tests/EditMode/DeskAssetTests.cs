using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Corrigible.Desk;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>F1 varlık hattı: içe aktarma ayarları, sprite kaydı, tema anahtarları ve font asset'leri.</summary>
    public class DeskAssetTests
    {
        [Test]
        public void SpriteImport_FollowsManifest()
        {
            foreach (var e in DeskSpriteManifest.All)
            {
                var path = DeskSpriteManifest.SpritesDir + "/" + e.file;
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.IsNotNull(ti, path);
                Assert.AreEqual(200f, ti.spritePixelsPerUnit, path);
                Assert.IsFalse(ti.mipmapEnabled, path);
                Assert.AreEqual(e.Border, ti.spriteBorder, path + " 9-dilim kenarı");
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var pivot = new Vector2(sp.pivot.x / sp.rect.width, sp.pivot.y / sp.rect.height);
                Assert.That(Vector2.Distance(pivot, e.Pivot), Is.LessThan(0.01f), path + " pivot");
            }
        }

        [Test]
        public void Textures_RepeatAndOutsideAtlas()
        {
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { DeskSpriteManifest.TexturesDir }))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g));
                Assert.AreEqual(TextureWrapMode.Repeat, ti.wrapMode, ti.assetPath);
                Assert.AreEqual(TextureImporterType.Default, ti.textureType, ti.assetPath);
            }
        }

        [Test]
        public void Registry_HasEverySpriteWithImageType()
        {
            var reg = DeskSprites.Instance;
            Assert.IsNotNull(reg);
            Assert.AreEqual(DeskSpriteManifest.All.Count(), reg.sprites.Count);
            Assert.IsTrue(reg.TryGet("sh_dash_r3_1", out var dash) && dash.type == Image.Type.Tiled);
            Assert.IsTrue(reg.TryGet("stamp_frame", out var stamp) && stamp.type == Image.Type.Sliced);
            Assert.IsNotNull(reg.Texture("tx_felt"));
        }

        /// <summary>Bileşen kodunda geçen her tema anahtarı temada tanımlı olmalı (eksik anahtar eflatun çizer).</summary>
        [Test]
        public void ThemeKeys_UsedInCode_Exist()
        {
            var theme = DeskTheme.Active;
            var rx = new Regex(@"""((?:ink|paper|desk|trust|status|badges|lamp|envelope|modal|button|speed|close|action|dockTabs|advisor)\.[A-Za-z.]+|black|white)""");
            var missing = Directory.GetFiles(DeskSpriteManifest.Root + "/Scripts", "*.cs", SearchOption.AllDirectories)
                .SelectMany(f => rx.Matches(File.ReadAllText(f)).Cast<Match>().Select(m => m.Groups[1].Value))
                .Distinct().Where(k => !theme.TryGet(k, false, out _)).ToList();
            Assert.IsEmpty(missing, "temada olmayan anahtarlar: " + string.Join(", ", missing));
        }

        [Test]
        public void Fonts_FallbackAndWeights()
        {
            var t = DeskTheme.Active;
            foreach (var f in new[] { t.cpRegular, t.cpBold, t.lfRegular, t.lfSemiBold, t.lfExtraBold, t.kalamRegular })
            {
                Assert.IsNotNull(f);
                Assert.AreEqual(AtlasPopulationMode.Static, f.atlasPopulationMode, f.name);
                Assert.IsTrue(f.fallbackFontAssetTable.Any(x => x.name.StartsWith("DejaVuSans")), f.name + " DejaVu yedeği");
                foreach (var c in "İıĞğŞşÇçÖöÜü%") Assert.IsTrue(f.characterLookupTable.ContainsKey(c), f.name + " " + c);
            }
            Assert.AreEqual(t.lfBold, t.lfRegular.fontWeightTable[7].regularTypeface, "<b> gerçek Bold asset'e gider");
            Assert.AreEqual(t.cpBold, t.cpRegular.fontWeightTable[7].regularTypeface);
            var dejavu = t.cpRegular.fallbackFontAssetTable.First(x => x.name.StartsWith("DejaVuSans"));
            foreach (var c in "←→↑↓↔✓✗⠿№") Assert.IsTrue(dejavu.characterLookupTable.ContainsKey(c), "DejaVu " + c);
            Assert.IsNotNull(t.styleSheet.GetStyle("num"));
            Assert.IsNotNull(t.styleSheet.GetStyle("hand"));
            // <font="…"> etiketi Resources yolundan bulunur (TMP Settings değişmeden)
            Assert.IsNotNull(Resources.Load<TMP_FontAsset>(TMP_Settings.defaultFontAssetPath + "CP-Bold SDF"));
        }
    }
}
