using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Sprite Atlas V2 <c>DeskUI</c> (PROMPT §6.2): yalnızca <c>Art/Sprites</c>; dokular ve harita dışarıda.
    /// 9-dilim bozulmasın diye sıkı paketleme ve döndürme kapalı, aralık 4, mipmap yok.
    /// </summary>
    public static class DeskAtlasSetup
    {
        public const string AtlasPath = DeskSpriteManifest.Root + "/Art/DeskUI.spriteatlasv2";

        [MenuItem("Corrigible/Desk UI/Atlası kur")]
        public static void Build()
        {
            if (AssetDatabase.LoadMainAssetAtPath(AtlasPath) == null)
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(DeskSpriteManifest.SpritesDir) });
                SpriteAtlasAsset.Save(atlas, AtlasPath);
                AssetDatabase.Refresh();
            }

            var imp = (SpriteAtlasImporter)AssetImporter.GetAtPath(AtlasPath);
            imp.includeInBuild = true;
            imp.packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1, padding = 4, enableRotation = false, enableTightPacking = false, enableAlphaDilation = true,
            };
            imp.textureSettings = new SpriteAtlasTextureSettings
            {
                readable = false, generateMipMaps = false, sRGB = true, filterMode = FilterMode.Bilinear, anisoLevel = 0,
            };
            imp.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform", maxTextureSize = 4096, textureCompression = TextureImporterCompression.Uncompressed,
                format = TextureImporterFormat.Automatic, crunchedCompression = false,
            });
            imp.SaveAndReimport();
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
            Debug.Log("[Desk] Atlas hazır: " + AtlasPath);
        }
    }
}
