using System;
using System.IO;
using Corrigible.Core;
using Corrigible.Fx.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx.Tests
{
    public sealed class FxFoundationTests
    {
        FxTheme Theme=>AssetDatabase.LoadAssetAtPath<FxTheme>(FxSetup.ThemePath);
        [Test] public void Manifest_All73SpritesHaveExactBordersAndSafeImport()
        {
            int count=0;
            foreach(Newtonsoft.Json.Linq.JObject e in FxAssetImporter.Sprites)
            {
                string path=FxSetup.Root+"/Art/Sprites/"+(string)e["file"];
                var imp=(TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(imp,Is.Not.Null,path);Assert.That(imp.spritePixelsPerUnit,Is.EqualTo(200));
                Assert.That(imp.mipmapEnabled,Is.False);Assert.That(imp.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
                var b=e["border1x"];Assert.That(imp.spriteBorder,Is.EqualTo(new Vector4((float)b[0]*2,(float)b[1]*2,(float)b[2]*2,(float)b[3]*2)),path);
                var sprite=Theme.SpriteInfo((string)e["name"]);Assert.That(sprite.sprite,Is.Not.Null);Assert.That(sprite.padding,Is.EqualTo((float)e["pad1x"]));Assert.That(sprite.tint,Is.EqualTo((bool)e["tint"]));count++;
            }
            Assert.That(count,Is.EqualTo(73));
        }
        [Test] public void Fonts_AllTenContainTurkishAndTypographyWithoutFallback()
        {
            Assert.That(Theme.fonts.Count,Is.EqualTo(10));
            foreach(var e in Theme.fonts){Assert.That(e.font,Is.Not.Null,e.key);Assert.That(e.font.atlasPopulationMode,Is.EqualTo(AtlasPopulationMode.Static));foreach(char c in FxSetup.Characters)Assert.That(e.font.HasCharacter(c,false,false),Is.True,e.key+" U+"+((int)c).ToString("X4"));}
            Assert.That(Theme.glows.Count,Is.LessThanOrEqualTo(10));
        }
        [Test] public void Theme_AllTypeTokensResolveRealWeightAndColor()
        {
            Assert.That(Theme.types.Count,Is.GreaterThan(100));
            foreach(var e in Theme.types){Assert.That(Theme.Font(e.fontKey),Is.Not.Null,e.key+" "+e.fontKey);Assert.That(e.size,Is.GreaterThanOrEqualTo(11),e.key);Assert.That(Theme.TryGet("colors."+e.colorKey,out var c),Is.True,e.key+" "+e.colorKey);}
            Assert.That(Theme.Color("cy"),Is.EqualTo(new Color(91/255f,228/255f,1,1)));
        }
        [TestCase(.2f,.6823f)] [TestCase(.5f,.9461f)] [TestCase(.8f,.9933f)]
        public void Entrance_MatchesCssBezierSamples(float x,float expected)=>Assert.That(FxTween.Ease(x),Is.EqualTo(expected).Within(.005f));
        [Test] public void Entrance_DelayAndReducedMotionSkipCorrectly(){Assert.That(FxTween.Progress(1,1,.7f,.1f),Is.Zero);Assert.That(FxTween.Progress(20,1,.7f),Is.EqualTo(1));Assert.That(FxTween.Progress(1,1,.7f,.1f,true),Is.EqualTo(1));}
        [Test] public void Clock_ReferencePhasesAndCustomFreezeAreStable()
        {
            var go=new GameObject("clock-test");try {var runner=go.AddComponent<FxLoopRunner>();var c=go.AddComponent<FxClock>();c.Configure(runner);c.Freeze();c.Advance(100);
            Assert.That(c.Phase(2,0,FxPhase.Pulse),Is.EqualTo(.5f));Assert.That(c.Phase(5,0,FxPhase.Led),Is.EqualTo(.45f));Assert.That(c.Phase(5,0,FxPhase.Sweep),Is.Zero);
            c.Freeze(new System.Collections.Generic.Dictionary<FxPhase,float>{{FxPhase.Pulse,.25f}});c.Advance(200);Assert.That(c.Phase(2,0,FxPhase.Pulse),Is.EqualTo(.25f));
            c.Resume();c.Reduced=true;Assert.That(c.CompleteEntrances,Is.True);Assert.That(c.Phase(2,0,FxPhase.Pulse),Is.EqualTo(.5f));c.Reduced=false;c.Advance(10.5);Assert.That(c.Phase(2,0,FxPhase.Rotation),Is.EqualTo(.25f));
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void Line_QuadraticEndpointsAndMidpointMatchDefinition()
        {
            Vector2 a=new Vector2(1,2),b=new Vector2(5,10),c=new Vector2(9,2);
            Assert.That(FxLineGraphic.Quadratic(a,b,c,0),Is.EqualTo(a));Assert.That(FxLineGraphic.Quadratic(a,b,c,1),Is.EqualTo(c));Assert.That(FxLineGraphic.Quadratic(a,b,c,.5f),Is.EqualTo(new Vector2(5,6)));
        }
        [Test] public void SharedPrefabs_AllReferencesRemainSerializedWithoutMissingScripts()
        {
            string[] paths=Directory.GetFiles(FxSetup.Root+"/Prefabs/Common","*.prefab");Assert.That(paths.Length,Is.EqualTo(17));
            foreach(string path in paths){var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.That(go,Is.Not.Null);foreach(var t in go.GetComponentsInChildren<Transform>(true))Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),Is.Zero,path);}
        }
        [Test] public void Trust_UsesUserChosenVerifiedTermAndEnglishFake()
        {
            var lang=Loc.Lang;var table=Loc.ParseTable(File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"));try {Loc.SetLanguage("tr");Assert.That(FxTrustBadge.Caption(FxTrust.Verified),Is.EqualTo("DOĞRULANDI"));Loc.SetLanguage("en",table["en"]);Assert.That(FxTrustBadge.Caption(FxTrust.Verified),Is.EqualTo("VERIFIED"));Assert.That(FxTrustBadge.Caption(FxTrust.Fake),Is.EqualTo("FAKE"));foreach(string s in FxCopy.Sources)Assert.That(table["en"].ContainsKey(s),Is.True,s);}finally{Loc.SetLanguage(lang,lang=="en"?table["en"]:null);}
        }
        [Test] public void Textures_TileRepeatAndBackdropClamp()
        {
            string folder=FxSetup.Root+"/Art/Textures/";
            foreach(string n in new[]{"tx_fx_led_6.png","tx_fx_quar_hex.png","tx_fx_scanlines_1x3.png"})Assert.That(((TextureImporter)AssetImporter.GetAtPath(folder+n)).wrapMode,Is.EqualTo(TextureWrapMode.Repeat));
            foreach(string n in new[]{"tx_fx_raid_bg.jpg","tx_fx_raid_cone_750x440.png","tx_fx_map_scan_160.png"})Assert.That(((TextureImporter)AssetImporter.GetAtPath(folder+n)).wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
        }
    }
}
