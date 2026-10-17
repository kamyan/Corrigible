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
        /// <summary>Sade kitinin ayrı atlası (Sade PROMPT §4.6): DeskUI atlası değişmesin.</summary>
        public const string SadeAtlasPath = DeskSpriteManifest.Root + "/Art/SadeUI.spriteatlasv2";

        [MenuItem("Corrigible/Desk UI/Atlası kur")]
        public static void Build() => Build(AtlasPath, DeskSpriteManifest.SpritesDir);

        [MenuItem("Corrigible/Sade UI/Atlası kur")]
        public static void BuildSade() => Build(SadeAtlasPath, SadeSpriteManifest.SpritesDir);

        static void Build(string atlasPath, string folder)
        {
            if (AssetDatabase.LoadMainAssetAtPath(atlasPath) == null)
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder) });
                SpriteAtlasAsset.Save(atlas, atlasPath);
                AssetDatabase.Refresh();
            }

            var imp = (SpriteAtlasImporter)AssetImporter.GetAtPath(atlasPath);
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
            Debug.Log("[Desk] Atlas hazır: " + atlasPath);
        }
    }
}
