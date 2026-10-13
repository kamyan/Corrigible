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
