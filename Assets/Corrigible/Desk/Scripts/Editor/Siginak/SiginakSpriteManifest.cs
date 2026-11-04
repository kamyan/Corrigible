using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Sığınak sprite ve doku kiti (Docs/UI_Siginak/sprites/README.md; Revizyon 2: Docs/UI_Siginak_Rev2/sprites/README.md).
    /// <c>Art/Siginak/Sprites</c> (Revizyon 2'ninkiler <c>Sprites/Rev2</c>): uGUI sprite'ları, PPU 100, Full Rect, 9-dilim
    /// kenarı tablodan; döşenenler Wrap Repeat. <c>Art/Siginak/Textures</c>: malzeme ya da RawImage/harita
    /// meshi dokuları (Default, mipmap'li; uzaktan bakılan harita taraması ve sayaç çarkı titremesin).
    /// <see cref="DeskSpriteImporter"/> bu iki klasör için <see cref="Apply"/>'ı çağırır.
    /// </summary>
    public static class SiginakSpriteManifest
    {
        public const string Root = DeskSpriteManifest.Root + "/Art/Siginak";
        public const string SpritesDir = Root + "/Sprites";
        public const string TexturesDir = Root + "/Textures";
        public const string SourceDir = "Docs/UI_Siginak/sprites";
        /// <summary>Revizyon 2 kiti (Docs/UI_Siginak_Rev2/sprites/README.md): sprite'lar ayrı alt klasörde, dokular ortak klasörde.</summary>
        public const string Rev2SourceDir = "Docs/UI_Siginak_Rev2/sprites";
        public const string Rev2SpritesDir = SpritesDir + "/Rev2";
        public const int PixelsPerUnit = 100;

        public enum Kind { Sliced, Tiled, Simple, Texture }

        public sealed class Entry
        {
            public readonly string Name; public readonly Kind Kind; public readonly int Border; public readonly bool Repeat, Point, Generated, Rev2;
            readonly Vector4? edges;
            public Entry(string name, Kind kind, int border = 0, bool repeat = false, bool point = false, bool generated = false,
                bool rev2 = false, Vector4? edges = null)
            {
                Name = name; Kind = kind; Border = border; Repeat = repeat || kind == Kind.Tiled; Point = point; Generated = generated;
                Rev2 = rev2; this.edges = edges;
            }
            public string Dir => Kind == Kind.Texture ? TexturesDir : Rev2 ? Rev2SpritesDir : SpritesDir;
            public string AssetPath => Dir + "/" + Name + ".png";
            public string SourcePath => (Rev2 ? Rev2SourceDir : SourceDir) + "/" + Name + ".png";
            /// <summary>9 dilim kenarı (Unity sırası: sol, alt, sağ, üst); README'de eşit değilse ayrıca verilir.</summary>
            public Vector4 SpriteBorder => edges ?? Vector4.one * Border;
            /// <summary>
            /// İşaretler (glyph_*) 72 px çizilip 18–22 px gösterilir: mipmap'siz ince çizgi kırılır ya da kaybolur
            /// (Rev2 1.2 README notu: Generate Mip Maps açık, Trilinear).
            /// </summary>
            public bool Mip => Kind == Kind.Texture || Name.StartsWith("glyph_");
            public FilterMode Filter => Point ? FilterMode.Point : Kind != Kind.Texture && Mip ? FilterMode.Trilinear : FilterMode.Bilinear;
        }

        static Entry R2(string name, Kind kind, int border = 0, bool repeat = false, Vector4? edges = null) =>
            new Entry(name, kind, border, repeat, rev2: true, edges: edges);

        /// <summary>Yumuşak kenarlı türev (SiginakTextureBaker.SoftEdges): kaynağın çevresinde 1 px saydam pay, dilim kenarı +1.</summary>
        static Entry Soft(string source, int border, Kind kind = Kind.Sliced) => new Entry(source + SoftSuffix, kind, border + 1, generated: true);
        public const string SoftSuffix = "_aa";

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
            // Yumuşak kenarlı türevler: eğik kâğıtlardaki zemin, düğme, gölge ve çerçeveler (dokunun kenarı saydam, eğik kenar basamaksız)
            Soft("ui_fill_r0", 3), Soft("ui_fill_r2", 4), Soft("ui_fill_r3", 5), Soft("ui_frame_1px_r2", 4), Soft("ui_frame_1_5px_r2", 4),
            Soft("ui_frame_2px_r3", 5), Soft("ui_frame_3px_r0", 5), Soft("ui_frame_double_6px", 8), Soft("ui_frame_dashed_2px", 2, Kind.Tiled),
            // İnce çizgiler: ortada 1 px (yatay ya da dikey) dolu, iki yanında 1 px saydam; kesik ayraç deseninin üstü ve altı saydam
            new Entry("ui_line_h_aa", Kind.Sliced, generated: true, edges: new Vector4(0, 1, 0, 1)),
            new Entry("ui_line_v_aa", Kind.Sliced, generated: true, edges: new Vector4(1, 0, 1, 0)),
            new Entry("ui_dash_h_aa", Kind.Tiled, generated: true, edges: new Vector4(0, 1, 0, 1)),
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
            // Projede üretilenler (SiginakTextureBaker): beton, linolyum, kadran, afiş piktogramı
            new Entry("tx_concrete_back", Kind.Texture, generated: true), new Entry("tx_concrete_side", Kind.Texture, generated: true),
            new Entry("tx_concrete_floor", Kind.Texture, generated: true), new Entry("tx_lino", Kind.Texture, repeat: true, generated: true),
            new Entry("tx_radio_dial", Kind.Texture, generated: true), new Entry("tx_poster_pictogram", Kind.Texture, generated: true),
            // Revizyon 2 · S4: merdiven boşluğunun fayansı (döşenir)
            new Entry("tx_stair_tiles", Kind.Texture, repeat: true, generated: true),
            // CRT geçiş camının köşe örtüsü (K5b: köşe 74/64): dış kenar opak, içi saydam; çerçeve rengiyle boyanır
            new Entry("ui_crt_glass_cover", Kind.Sliced, 96, generated: true),
            // Görev fişinin ekip diski (Revizyon 2 · S2): dolgu ve kenar renkli, 44 px çizilip 22 px gösterilir
            new Entry("ui_team_disc", Kind.Simple, generated: true),
            // Baskın parçalarının küçük resimleri (SiginakTextureBaker.PieceThumbs): 3D prefab'lardan çizilir, kayıtta adla aranır.
            // Yağma hedefi (ikmal sandığı); inceleme kartında olay nesneleri (basınçlı tüp, kaynak kolu kaidesi, tavan enkazı), sivil
            // sunucu ve kopya dolabı
            new Entry("thumb_crate", Kind.Simple, generated: true),
            new Entry("thumb_tube", Kind.Simple, generated: true), new Entry("thumb_weld", Kind.Simple, generated: true),
            new Entry("thumb_debris", Kind.Simple, generated: true), new Entry("thumb_civ", Kind.Simple, generated: true),
            new Entry("thumb_spawner", Kind.Simple, generated: true),
            // İstasyon simgeleri (SiginakTextureBaker.KeyIcons): masadaki tuş başlıkları ve üst şeridin tuş kutuları; beyaz, boyanır
            new Entry("tx_key_shelter", Kind.Texture, generated: true), new Entry("tx_key_missions", Kind.Texture, generated: true),
            new Entry("tx_key_door", Kind.Texture, generated: true), new Entry("tx_key_desk", Kind.Texture, generated: true),
            new Entry("tx_key_settings", Kind.Texture, generated: true),

            // Revizyon 2 · işaretler: bölge özelliği, imza ve makine türü (düz ve kare çerçeveli), tehdit kademesi, yeniden işgal
            R2("glyph_corridor", Kind.Simple), R2("glyph_sand", Kind.Simple), R2("glyph_locker", Kind.Simple), R2("glyph_mortar", Kind.Simple),
            R2("glyph_drone", Kind.Simple), R2("glyph_maint", Kind.Simple), R2("glyph_shield", Kind.Simple),
            R2("glyph_corridor_frame", Kind.Simple), R2("glyph_sand_frame", Kind.Simple), R2("glyph_locker_frame", Kind.Simple),
            R2("glyph_mortar_frame", Kind.Simple), R2("glyph_drone_frame", Kind.Simple), R2("glyph_maint_frame", Kind.Simple),
            R2("glyph_shield_frame", Kind.Simple),
            // 1.2 eki: on bölge özelliği ve üç makine türü (düz ve kare çerçeveli); eşleme raid_tokens.json → regionTraits, machineTypes.glyph
            R2("glyph_safe", Kind.Simple), R2("glyph_short", Kind.Simple), R2("glyph_reinforce", Kind.Simple), R2("glyph_open", Kind.Simple),
            R2("glyph_cooling", Kind.Simple), R2("glyph_crowd", Kind.Simple), R2("glyph_armored", Kind.Simple), R2("glyph_grid", Kind.Simple),
            R2("glyph_factory", Kind.Simple), R2("glyph_patrol", Kind.Simple),
            R2("glyph_armordrone", Kind.Simple), R2("glyph_bomber", Kind.Simple), R2("glyph_replicator", Kind.Simple),
            R2("glyph_safe_frame", Kind.Simple), R2("glyph_short_frame", Kind.Simple), R2("glyph_reinforce_frame", Kind.Simple),
            R2("glyph_open_frame", Kind.Simple), R2("glyph_cooling_frame", Kind.Simple), R2("glyph_crowd_frame", Kind.Simple),
            R2("glyph_armored_frame", Kind.Simple), R2("glyph_grid_frame", Kind.Simple), R2("glyph_factory_frame", Kind.Simple),
            R2("glyph_patrol_frame", Kind.Simple), R2("glyph_armordrone_frame", Kind.Simple), R2("glyph_bomber_frame", Kind.Simple),
            R2("glyph_replicator_frame", Kind.Simple),
            R2("threat_tier_1", Kind.Simple), R2("threat_tier_2", Kind.Simple), R2("threat_tier_3", Kind.Simple), R2("threat_tier_4", Kind.Simple),
            R2("mark_sector_reocc", Kind.Simple),
            // Revizyon 2 · çerçeve ve malzeme: sonuç damgası, baskın panelinin kontrplak şeridi (L 2, T 2, R 2, B 6), ozalit ızgarası, bant
            R2("ui_frame_double_6px", Kind.Sliced, 8), R2("ui_ply_strip", Kind.Sliced, edges: new Vector4(2, 6, 2, 2)),
            R2("tx_ozalit_grid", Kind.Tiled), R2("ui_tape", Kind.Simple),
            // Revizyon 2 · can pipleri, alarm ölçeğinin kesik hücresi, ikonlar, rozetler, görev fişinin ok ucu
            R2("pip_fill", Kind.Simple), R2("pip_empty", Kind.Simple), R2("cell_dashed", Kind.Simple),
            R2("icon_eye", Kind.Simple), R2("icon_person", Kind.Simple), R2("icon_antenna", Kind.Simple), R2("icon_move", Kind.Simple),
            R2("icon_bolt", Kind.Simple), R2("icon_rules", Kind.Simple), R2("icon_retreat", Kind.Simple), R2("icon_radio", Kind.Simple),
            R2("icon_undo", Kind.Simple), R2("icon_target", Kind.Simple), R2("icon_armor", Kind.Simple), R2("icon_close", Kind.Simple),
            R2("icon_plate", Kind.Simple), R2("icon_civilian", Kind.Simple),
            R2("badge_clamp", Kind.Simple), R2("badge_wrench", Kind.Simple), R2("ui_pointer", Kind.Simple),
            // 1.2 eki: zarar vermeyen niyetlerin rozetleri (kalkan, kopyalama)
            R2("badge_shield", Kind.Simple), R2("badge_clone", Kind.Simple),
            // Revizyon 2 · dünyadaki Quad dokuları: FON virgülü, açık renk onda bir çarkı (kırmızının yerine), tebeşir çentiği
            R2("ui_counter_comma", Kind.Texture), R2("tx_counter_digits_tenths", Kind.Texture, repeat: true),
            R2("decal_tally5", Kind.Texture),
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
            ti.filterMode = e.Filter;
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
            ti.mipmapEnabled = e.Mip;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spritePixelsPerUnit = PixelsPerUnit;
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteGenerateFallbackPhysicsShape = false;
            s.spriteBorder = e.SpriteBorder;
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
                var src = e.SourcePath;
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
