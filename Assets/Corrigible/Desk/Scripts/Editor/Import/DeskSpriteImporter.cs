using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Masa varlıklarının içe aktarma ayarları (PROMPT §6.1, §6.3, §6.4):
    /// sprite'lar @2x PPU 200, 9-dilim kenarı ve pivot <c>sprites.json</c>'dan; dokular atlas dışı Wrap Repeat; harita PPU 200.
    /// </summary>
    public sealed class DeskSpriteImporter : AssetPostprocessor
    {
        const int PixelsPerUnit = 200;

        void OnPreprocessTexture()
        {
            var path = assetPath.Replace('\\', '/');
            if (!path.EndsWith(".png")) return;
            var ti = (TextureImporter)assetImporter;

            if (path.StartsWith(DeskSpriteManifest.SpritesDir + "/")) Sprite(ti, path);
            else if (path.StartsWith(DeskSpriteManifest.TexturesDir + "/")) Texture(ti);
            else if (path.StartsWith(DeskSpriteManifest.MapDir + "/")) Map(ti);
            // Sade kiti (Docs/UI_Sade PROMPT §4.6–4.7)
            else if (path.StartsWith(SadeSpriteManifest.SpritesDir + "/")) SadeSprite(ti, path);
            else if (path.StartsWith(SadeSpriteManifest.TexturesDir + "/")) SadeTexture(ti, path);
            else if (path.StartsWith(SadeSpriteManifest.MapDir + "/") || path.StartsWith(SadeSpriteManifest.RaidDir + "/")) SadeLayer(ti, path);
            // Sığınak kiti (Docs/UI_Siginak/sprites/README.md)
            else if (SiginakSpriteManifest.Owns(path)) SiginakSpriteManifest.Apply(ti, path);
        }

        static void SadeSprite(TextureImporter ti, string path)
        {
            Common(ti);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.wrapMode = TextureWrapMode.Clamp;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spritePixelsPerUnit = PixelsPerUnit;
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteGenerateFallbackPhysicsShape = false;
            var e = SadeSpriteManifest.Get(System.IO.Path.GetFileNameWithoutExtension(path));
            if (e != null)
            {
                s.spriteBorder = e.Border;
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = e.Pivot;
            }
            else if (!path.EndsWith("_contact_sheet.png")) Debug.LogWarning($"[Sade] sprites.json'da yok: {path}");
            ti.SetTextureSettings(s);
        }

        /// <summary>Döşeme dokuları Wrap Repeat; ekran boyu lamba dokuları Clamp.</summary>
        static void SadeTexture(TextureImporter ti, string path)
        {
            Common(ti);
            ti.textureType = TextureImporterType.Default;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.wrapMode = System.IO.Path.GetFileName(path).StartsWith("tx_lamp") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        }

        /// <summary>
        /// Harita katmanları (3840×2160) ve ozalit dokuları (2048): Default, Clamp, mipmap yok, en çok 4096. Bellek için BC7
        /// (yüksek kalite); taban dokusunun alfası yok.
        /// </summary>
        static void SadeLayer(TextureImporter ti, string path)
        {
            Common(ti);
            ti.textureType = TextureImporterType.Default;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            if (System.IO.Path.GetFileName(path).StartsWith("map_base")) ti.alphaSource = TextureImporterAlphaSource.None;
            ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone", overridden = true, maxTextureSize = 4096, format = TextureImporterFormat.BC7, compressionQuality = 100,
            });
        }

        static void Common(TextureImporter ti)
        {
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.alphaIsTransparency = true;
            ti.sRGBTexture = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.isReadable = false;
        }

        static void Sprite(TextureImporter ti, string path)
        {
            Common(ti);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.wrapMode = TextureWrapMode.Clamp;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spritePixelsPerUnit = PixelsPerUnit;
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteGenerateFallbackPhysicsShape = false;

            var e = DeskSpriteManifest.Get(System.IO.Path.GetFileNameWithoutExtension(path));
            if (e != null)
            {
                s.spriteBorder = e.Border;
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = e.Pivot;
            }
            else Debug.LogWarning($"[Desk] sprites.json'da yok: {path}");
            ti.SetTextureSettings(s);
        }

        static void Texture(TextureImporter ti)
        {
            Common(ti);
            ti.textureType = TextureImporterType.Default;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.wrapMode = TextureWrapMode.Repeat;
        }

        static void Map(TextureImporter ti)
        {
            Common(ti);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 4096;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spritePixelsPerUnit = PixelsPerUnit;
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteGenerateFallbackPhysicsShape = false;
            ti.SetTextureSettings(s);
        }
    }
}
