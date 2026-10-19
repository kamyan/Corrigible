using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace Corrigible.Fx.EditorTools
{
    /// <summary>Yalnız FX klasörünü işler; Sade importer ayarlarını değiştirmez.</summary>
    public sealed class FxAssetImporter : AssetPostprocessor
    {
        public const string Root="Assets/Corrigible/Fx";
        static JObject manifest;
        public static JArray Sprites => (JArray)(manifest??(manifest=JObject.Parse(File.ReadAllText(Root+"/Data/sprites.json"))))["sprites"];
        public static JObject Entry(string file){foreach(JObject e in Sprites)if((string)e["file"]==file)return e;return null;}
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(Root+"/Art/"))return;
            var t=(TextureImporter)assetImporter;t.mipmapEnabled=false;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.npotScale=TextureImporterNPOTScale.None;t.alphaIsTransparency=true;
            t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=4096;
            if(assetPath.Contains("/Sprites/")||assetPath.Contains("/Icons/"))
            {
                t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=200;
                var e=Entry(Path.GetFileName(assetPath));if(e!=null){var b=(JArray)e["border1x"];t.spriteBorder=new Vector4((float)b[0]*2,(float)b[1]*2,(float)b[2]*2,(float)b[3]*2);}
                var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(settings);
            }
            else
            {
                t.textureType=TextureImporterType.Default;string n=Path.GetFileNameWithoutExtension(assetPath);
                if(assetPath.Contains("/Textures/")&&!n.Contains("vignette")&&!n.Contains("scan_160")&&!n.Contains("scan_90")&&!n.Contains("_bg")&&!n.Contains("cone"))t.wrapMode=TextureWrapMode.Repeat;
                if(assetPath.Contains("/Orb/")){t.sRGBTexture=false;t.alphaIsTransparency=false;}
                if(assetPath.Contains("/Map/")){t.textureCompression=TextureImporterCompression.CompressedHQ;var pc=t.GetPlatformTextureSettings("Standalone");pc.overridden=true;pc.name="Standalone";pc.maxTextureSize=4096;pc.format=TextureImporterFormat.BC7;t.SetPlatformTextureSettings(pc);}
            }
        }
    }
}
