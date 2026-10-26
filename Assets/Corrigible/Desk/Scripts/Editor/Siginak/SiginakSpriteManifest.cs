using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Sığınak sprite ve doku kiti (Docs/UI_Siginak/sprites/README.md). <c>Art/Siginak/Sprites</c>: uGUI sprite'ları, PPU 100,
    /// Full Rect, 9-dilim kenarı tablodan; döşenenler Wrap Repeat. <c>Art/Siginak/Textures</c>: malzeme ya da RawImage/harita
    /// meshi dokuları (Default, mipmap'li; uzaktan bakılan harita taraması ve sayaç çarkı titremesin).
    /// <see cref="DeskSpriteImporter"/> bu iki klasör için <see cref="Apply"/>'ı çağırır.
    /// </summary>
    public static class SiginakSpriteManifest
    {
        public const string Root = DeskSpriteManifest.Root + "/Art/Siginak";
        public const string SpritesDir = Root + "/Sprites";
        public const string TexturesDir = Root + "/Textures";
        public const string SourceDir = "Docs/UI_Siginak/sprites";
        public const int PixelsPerUnit = 100;

        public enum Kind { Sliced, Tiled, Simple, Texture }

        public sealed class Entry
        {
            public readonly string Name; public readonly Kind Kind; public readonly int Border; public readonly bool Repeat, Point, Generated;
            public Entry(string name, Kind kind, int border = 0, bool repeat = false, bool point = false, bool generated = false)
            { Name = name; Kind = kind; Border = border; Repeat = repeat || kind == Kind.Tiled; Point = point; Generated = generated; }
            public string Dir => Kind == Kind.Texture ? TexturesDir : SpritesDir;
            public string AssetPath => Dir + "/" + Name + ".png";
        }

        static readonly Entry[] Entries =
        {
            // Düz zemin ve kenar (Sliced; README: dilim kenarı L = T = R = B)
            new Entry("ui_fill_r0", Kind.Sliced, 3), new Entry("ui_fill_r2", Kind.Sliced, 4), new Entry("ui_fill_r3", Kind.Sliced, 5),
            new Entry("ui_fill_r4", Kind.Sliced, 6), new Entry("ui_fill_r8", Kind.Sliced, 10),
            new Entry("ui_frame_1px_r2", Kind.Sliced, 4), new Entry("ui_frame_1_5px_r2", Kind.Sliced, 4),
            new Entry("ui_frame_2px_r3", Kind.Sliced, 5), new Entry("ui_frame_3px_r0", Kind.Sliced, 5),
            new Entry("ui_frame_4px_r0", Kind.Sliced, 8), new Entry("ui_frame_8px_r0", Kind.Sliced, 13),
            // 4 ve 8 px çerçevelerde çizgi kenardan 2/4 px içeride başlar: README kenarı (6/10) çizgiyi böler ve orta dilim
            // gerilince yarı saydam dolgu verir; kenar çizgi + yumuşatma dışına alındı.
            // Kesik kenar: kenarlar döşenir
            new Entry("ui_frame_dashed_2px", Kind.Tiled, 2), new Entry("ui_frame_dashed_3px", Kind.Tiled, 3),
            // Döşemeler
            new Entry("ui_dash_h", Kind.Tiled), new Entry("ui_telex_holes", Kind.Tiled), new Entry("ui_hazard_stripes", Kind.Tiled),
            new Entry("tx_paper_grain", Kind.Tiled), new Entry("crt_scanlines", Kind.Tiled, point: true),
            // Tek parça
            new Entry("crt_vignette", Kind.Simple), new Entry("crt_rollbar", Kind.Simple),
            new Entry("map_grease_ring", Kind.Simple), new Entry("map_candidate_ring", Kind.Simple),
            new Entry("map_stamp_free", Kind.Simple), new Entry("map_pin", Kind.Simple), new Entry("icon_team_figure", Kind.Simple),
            new Entry("mark_sector_done", Kind.Simple), new Entry("mark_sector_ready", Kind.Simple), new Entry("mark_sector_locked", Kind.Simple),
            new Entry("decal_coffee_ring", Kind.Simple), new Entry("decal_signature", Kind.Simple),
            new Entry("decal_first_aid_iso7010", Kind.Simple),
            // Dokular: harita taraması (RegionMeshGraphic), sayaç çarkı ve gölgesi (Quad malzemesi), CRT statiği (RawImage uvRect)
            new Entry("map_hatch_45_p16", Kind.Texture, repeat: true), new Entry("map_hatch_m45_p16", Kind.Texture, repeat: true),
            new Entry("map_hatch_45_p32", Kind.Texture, repeat: true),
            new Entry("tx_counter_digits", Kind.Texture, repeat: true), new Entry("tx_counter_digits_red", Kind.Texture, repeat: true),
            new Entry("tx_counter_shade", Kind.Texture), new Entry("tx_crt_static", Kind.Texture, repeat: true),
            // Projede üretilenler (SiginakTextureBaker): beton, linolyum, klavye, kadran, afiş piktogramı
            new Entry("tx_concrete_back", Kind.Texture, generated: true), new Entry("tx_concrete_side", Kind.Texture, generated: true),
            new Entry("tx_concrete_floor", Kind.Texture, generated: true), new Entry("tx_lino", Kind.Texture, repeat: true, generated: true),
            new Entry("tx_keyboard", Kind.Texture, generated: true), new Entry("tx_radio_dial", Kind.Texture, generated: true),
            new Entry("tx_poster_pictogram", Kind.Texture, generated: true),
            // CRT geçiş camının köşe örtüsü (K5b: köşe 74/64): dış kenar opak, içi saydam; çerçeve rengiyle boyanır
            new Entry("ui_crt_glass_cover", Kind.Sliced, 96, generated: true),
        };

        static readonly Dictionary<string, Entry> ByName = Entries.ToDictionary(e => e.Name);

        public static IEnumerable<Entry> All => Entries;
        public static Entry Get(string name) => ByName.TryGetValue(name, out var e) ? e : null;

        public static bool Owns(string path) => path.StartsWith(SpritesDir + "/") || path.StartsWith(TexturesDir + "/");

        /// <summary>İçe aktarma ayarları; tabloda olmayan dosya uyarı verir ve dokunulmaz.</summary>
        public static void Apply(TextureImporter ti, string path)
        {
            var e = Get(Path.GetFileNameWithoutExtension(path));
            if (e == null) { Debug.LogWarning("[Sığınak] sprite tablosunda yok: " + path); return; }
            ti.alphaIsTransparency = true;
            ti.sRGBTexture = true;
            ti.isReadable = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = e.Point ? FilterMode.Point : FilterMode.Bilinear;
            ti.wrapMode = e.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (e.Kind == Kind.Texture)
            {
                ti.textureType = TextureImporterType.Default;
                ti.mipmapEnabled = true;
                ti.anisoLevel = 4;
                // Harita taraması K2'de ~0,63 ekran px / doku pikseli: varsayılan mip çizgiyi yarı yarıya soldurur, referanstaki
                // koyu taramaya yaklaşmak için bir kademe keskin örneklenir
                ti.mipMapBias = e.Name.StartsWith("map_hatch") ? -1 : 0;
                return;
            }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spritePixelsPerUnit = PixelsPerUnit;
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteGenerateFallbackPhysicsShape = false;
            s.spriteBorder = Vector4.one * e.Border;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(s);
        }

        /// <summary>Paketteki PNG'leri tablodaki klasörlere kopyalar ve yeniden içe aktarır (paket güncellenince yeniden çalıştır).</summary>
        [MenuItem("Corrigible/Sığınak/Sprite'ları içe aktar")]
        public static string CopyFromPackage()
        {
            int copied = 0;
            var missing = new List<string>();
            foreach (var e in Entries.Where(e => !e.Generated))
            {
                var src = Path.Combine(SourceDir, e.Name + ".png");
                if (!File.Exists(src)) { missing.Add(e.Name); continue; }
                Directory.CreateDirectory(e.Dir);
                File.Copy(src, e.AssetPath, true);
                copied++;
            }
            AssetDatabase.Refresh();
            foreach (var e in Entries) if (File.Exists(e.AssetPath)) AssetDatabase.ImportAsset(e.AssetPath, ImportAssetOptions.ForceUpdate);
            string result = $"{copied} dosya kopyalandı" + (missing.Count > 0 ? ", pakette yok: " + string.Join(", ", missing) : "");
            Debug.Log("[Sığınak] " + result);
            return result;
        }
    }
}
