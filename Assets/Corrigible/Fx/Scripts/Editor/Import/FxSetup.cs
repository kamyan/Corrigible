using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace Corrigible.Fx.EditorTools
{
    /// <summary>Yeniden çalıştırılabilen asset içe aktarma. Sahne veya prefab üretmez.</summary>
    public static class FxSetup
    {
        public const string Root=FxAssetImporter.Root,ThemePath=Root+"/Settings/FxTheme.asset";
        public const string Characters=" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~ÇĞİÖŞÜçğıöşüÂâÎîÛû“”‘’·–—−≈±×→←↑↓›‹…";
        static readonly string[] Names={"ChakraPetch-Regular","ChakraPetch-Medium","ChakraPetch-SemiBold","ChakraPetch-Bold","IBMPlexMono-Regular","IBMPlexMono-Medium","IBMPlexMono-SemiBold","IBMPlexSans-Regular","IBMPlexSans-Medium","IBMPlexSans-SemiBold"};
        [MenuItem("Tools/FX UI/Import All Foundation Assets")]
        public static void ImportAll(){Directory.CreateDirectory(Root+"/Settings");Directory.CreateDirectory(Root+"/Art/Fonts/SDF");AssetDatabase.Refresh();ImportFonts();ImportTokens();BuildAtlas();AssetDatabase.SaveAssets();Debug.Log("[FX F1] Font, tema ve atlas hazır.");}
        public static void ImportFonts()
        {
            foreach(string name in Names)
            {
                string path=Root+"/Art/Fonts/SDF/"+name+".asset";
                if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path)!=null)continue;
                var font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Art/Fonts/"+name+".ttf");int size=name.Contains("Mono")?1024:2048;
                TMP_FontAsset fa=null;
                for(int pt=90;pt>=72;pt-=2)
                {
                    fa=TMP_FontAsset.CreateFontAsset(font,pt,9,GlyphRenderMode.SDFAA,size,size,AtlasPopulationMode.Dynamic,false);
                    fa.TryAddCharacters(Characters,out string missing,true);
                    if(string.IsNullOrEmpty(missing))break;
                    Object.DestroyImmediate(fa.material);Object.DestroyImmediate(fa.atlasTexture);Object.DestroyImmediate(fa);fa=null;
                }
                if(fa==null)throw new Exception("Font glifleri sığmadı: "+name);
                fa.name=name+" SDF";AssetDatabase.CreateAsset(fa,path);fa.atlasTexture.name=name+" Atlas";AssetDatabase.AddObjectToAsset(fa.atlasTexture,fa);fa.material.name=name+" Base";AssetDatabase.AddObjectToAsset(fa.material,fa);
                fa.atlasPopulationMode=AtlasPopulationMode.Static;fa.material.shader=Shader.Find("TextMeshPro/Distance Field");EditorUtility.SetDirty(fa);EditorUtility.SetDirty(fa.material);
            }
            foreach(string name in Names)
            {
                var fa=Font(name);string family=name.Split('-')[0];foreach(string other in Names)if(other.StartsWith(family+"-")) {int weight=other.EndsWith("Regular")?4:other.EndsWith("Medium")?5:other.EndsWith("SemiBold")?6:7;fa.fontWeightTable[weight].regularTypeface=Font(other);}EditorUtility.SetDirty(fa);
            }
        }
        static TMP_FontAsset Font(string name)=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Art/Fonts/SDF/"+name+".asset");
        [MenuItem("Tools/FX UI/Import Tokens")]
        public static void ImportTokens()
        {
            var theme=AssetDatabase.LoadAssetAtPath<FxTheme>(ThemePath);if(theme==null){theme=ScriptableObject.CreateInstance<FxTheme>();AssetDatabase.CreateAsset(theme,ThemePath);}
            theme.colors.Clear();theme.numbers.Clear();theme.types.Clear();theme.sprites.Clear();theme.fonts.Clear();theme.glows.Clear();
            var tokens=JObject.Parse(File.ReadAllText(Root+"/Data/tokens_fx.json"));Flatten(theme,tokens,"");Flatten(theme,JObject.Parse(File.ReadAllText(Root+"/Data/map_fx.json")),"map");Flatten(theme,JObject.Parse(File.ReadAllText(Root+"/Data/raid_fx_palette.json")),"palette");
            theme.colors.Add(new Corrigible.Desk.TokenTheme.NamedColor{key="colors.hal.h1Text",color=Parse("#FFE0A8")});theme.colors.Add(new Corrigible.Desk.TokenTheme.NamedColor{key="colors.hal.h3Text",color=Parse("#D4ECFF")});
            foreach(string n in Names)theme.fonts.Add(new FxTheme.FontEntry{key=n,font=Font(n)});
            foreach(var p in (JObject)tokens["type"])
            {
                if(!(p.Value is JArray a))continue;string family=((string)a[0]).Replace(" ","");int weight=(int)a[1];string suffix=weight>=700?"Bold":weight>=600?"SemiBold":weight>=500?"Medium":"Regular";
                string color=((string)a[4]).Split(' ',';', '(')[0];if(color=="h2Text")color="hal.h2Text";if(color=="h1")color="hal.h1";if(color=="h2")color="hal.h2";if(color=="h3")color="hal.h3";
                if(color.StartsWith("#")){string key="type_"+p.Key;theme.colors.Add(new Corrigible.Desk.TokenTheme.NamedColor{key="colors."+key,color=Parse(color)});color=key;}
                if(color=="hâl")color="hal.h2";if(color=="şüpheli")color="am";if(color=="takım")color="mt";if(color=="renk"||color.Length==0)color="tx";
                theme.types.Add(new FxTheme.TypeEntry{key=p.Key,fontKey=family+"-"+suffix,size=(float)a[2],spacing=(float)a[3]*100,colorKey=color});
            }
            foreach(JObject e in FxAssetImporter.Sprites)
            {
                theme.sprites.Add(new FxTheme.SpriteEntry{key=(string)e["name"],sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Sprites/"+(string)e["file"]),type=(Image.Type)Enum.Parse(typeof(Image.Type),(string)e["imageType"]),padding=(float)e["pad1x"],tint=(bool)e["tint"],size=new Vector2((float)e["size1x"][0],(float)e["size1x"][1])});
            }
            foreach(string file in Directory.GetFiles(Root+"/Art/Icons","*.png"))theme.sprites.Add(new FxTheme.SpriteEntry{key=Path.GetFileNameWithoutExtension(file),sprite=AssetDatabase.LoadAssetAtPath<Sprite>(file),type=Image.Type.Simple,tint=true,size=Path.GetFileNameWithoutExtension(file).EndsWith("_s")?Vector2.one*16:Vector2.one*24});
            theme.Invalidate();theme.additive=Material("FxAdditive","UI/FxAdditive");theme.line=Material("FxLine","UI/FxLine");
            Glow(theme,"GlowCyan35","ChakraPetch-SemiBold","cy",.35f);Glow(theme,"GlowCyan45","IBMPlexMono-SemiBold","cy",.45f);Glow(theme,"GlowAmber50","IBMPlexMono-SemiBold","am",.5f);Glow(theme,"GlowViolet45","IBMPlexMono-SemiBold","vi",.45f);Glow(theme,"GlowMint60","ChakraPetch-Bold","mt",.6f);Glow(theme,"GlowRed60","IBMPlexSans-Regular","rd",.6f);Glow(theme,"LabelHalo","ChakraPetch-SemiBold","bg",1);Glow(theme,"StateColor","ChakraPetch-Bold","hal.h2",.6f);
            EditorUtility.SetDirty(theme);AssetDatabase.SaveAssets();
        }
        static Material Material(string name,string shader)
        {
            string path=Root+"/Settings/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}return m;
        }
        static void Glow(FxTheme t,string name,string fontKey,string color,float alpha)
        {
            string path=Root+"/Settings/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(t.Font(fontKey).material);AssetDatabase.CreateAsset(m,path);}
            m.CopyPropertiesFromMaterial(t.Font(fontKey).material);m.EnableKeyword("UNDERLAY_ON");var c=t.Color(color);c.a=alpha;m.SetColor("_UnderlayColor",c);m.SetFloat("_UnderlayOffsetX",0);m.SetFloat("_UnderlayOffsetY",0);m.SetFloat("_UnderlayDilate",.1f);m.SetFloat("_UnderlaySoftness",.6f);
            if(name=="LabelHalo"){m.EnableKeyword("GLOW_ON");c=t.Color("cy");c.a=.35f;m.SetColor("_GlowColor",c);m.SetFloat("_GlowOuter",.15f);}
            EditorUtility.SetDirty(m);t.glows.Add(new FxTheme.GlowEntry{key=name,fontKey=fontKey,material=m});
        }
        static Color Parse(string text){ColorUtility.TryParseHtmlString(text,out var c);return c;}
        static void Flatten(FxTheme t,JToken node,string path)
        {
            if(node is JObject obj){foreach(var p in obj)Flatten(t,p.Value,path.Length==0?p.Key:path+"."+p.Key);return;}
            if(node.Type==JTokenType.String && ((string)node).StartsWith("#")&&ColorUtility.TryParseHtmlString((string)node,out var hex)){t.colors.Add(new Corrigible.Desk.TokenTheme.NamedColor{key=path,color=hex});return;}
            if(node is JArray arr){if(arr.Count==4 && arr[0].Type!=JTokenType.String && (arr[0].Type==JTokenType.Integer||arr[0].Type==JTokenType.Float)&& (arr[3].Type==JTokenType.Float||arr[3].Type==JTokenType.Integer) && (double)arr[3]<=1 && path.StartsWith("colors.")){t.colors.Add(new Corrigible.Desk.TokenTheme.NamedColor{key=path,color=new Color((float)arr[0]/255,(float)arr[1]/255,(float)arr[2]/255,(float)arr[3])});return;}for(int i=0;i<arr.Count;i++)Flatten(t,arr[i],path+"."+i);return;}
            if(node.Type==JTokenType.Float||node.Type==JTokenType.Integer)t.numbers.Add(new Corrigible.Desk.TokenTheme.NamedFloat{key=path,value=(float)node});
        }
        [MenuItem("Tools/FX UI/Build Atlas")]
        public static void BuildAtlas()
        {
            string path=Root+"/Art/FxUI.spriteatlasv2";
            if(AssetDatabase.LoadMainAssetAtPath(path)==null){var a=new SpriteAtlasAsset();a.Add(new Object[]{AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root+"/Art/Sprites"),AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root+"/Art/Icons")});SpriteAtlasAsset.Save(a,path);AssetDatabase.Refresh();}
            var imp=(SpriteAtlasImporter)AssetImporter.GetAtPath(path);imp.includeInBuild=true;imp.packingSettings=new SpriteAtlasPackingSettings{padding=4,enableRotation=false,enableTightPacking=false,enableAlphaDilation=true,blockOffset=1};imp.textureSettings=new SpriteAtlasTextureSettings{generateMipMaps=false,sRGB=true,filterMode=FilterMode.Bilinear,anisoLevel=0,readable=false};imp.SetPlatformSettings(new TextureImporterPlatformSettings{name="DefaultTexturePlatform",maxTextureSize=4096,textureCompression=TextureImporterCompression.Uncompressed,format=TextureImporterFormat.Automatic});imp.SaveAndReimport();SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
        }
    }
}
