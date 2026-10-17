using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Corrigible.Desk.Tests
{
    /// <summary>F7: dil geçişleri simülasyonu değiştirmez; durağan spot mesh'i tekrar kirletmez.</summary>
    public class SadePolishTests
    {
        [Test] public void Spotlight_UnchangedSpotDoesNotRebuildVertices()
        {
            var go=new GameObject("Spot denetimi",typeof(RectTransform),typeof(CanvasRenderer));
            try
            {
                var spot=go.AddComponent<SpotlightGraphic>();int dirties=0;spot.RegisterDirtyVerticesCallback(()=>dirties++);
                spot.SetSpot(new Vector2(7,9),74,12,Color.black);Assert.AreEqual(1,dirties);
                spot.SetSpot(new Vector2(7,9),74,12,Color.black);Assert.AreEqual(1,dirties);
                spot.SetSpot(new Vector2(8,9),74,12,Color.black);Assert.AreEqual(2,dirties);
                spot.SetSpot(new Vector2(8,9),74,12,Color.black,false);Assert.AreEqual(3,dirties);
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void Intro_LanguageRefreshUsesInitialProfileWithoutChangingGame()
        {
            var lang=DeskLoc.Instance.Lang;
            try
            {
                DeskLoc.Instance.SetLanguage(DeskLang.Tr);
                var profile=new PlayerProfile{runs=3,karantina=9,salter=4};
                var sim=Simulation.NewGame(profile,"manipulator",7,new Balance());
                var state=new DeskBridgeState{IntroOpen=true};
                var input=new DeskBuildInput{Sim=sim,State=state,Info=new DeskGameInfo(),Profile=new PlayerProfile{runs=99},MapSpec=JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Mock/map.json").text)};
                uint rs=sim.S.Rng.State;int commands=sim.Record.commands.Count;
                var tr=new SimDeskBuilder().Build(input).Intro.Intercept.Lines.ToArray();
                DeskLoc.Instance.SetLanguage(DeskLang.En);
                var en=new SimDeskBuilder().Build(input).Intro.Intercept.Lines.ToArray();
                Assert.AreEqual(Profiling.ComputeCounters(profile,"manipulator").Lines.Take(3).Select(x=>Loc.F("> {0}",x)).ToArray(),en);
                Assert.AreNotEqual(string.Join("|",tr),string.Join("|",en));Assert.AreEqual(rs,sim.S.Rng.State);Assert.AreEqual(commands,sim.Record.commands.Count);
            }
            finally{DeskLoc.Instance.SetLanguage(lang);}
        }
        [Test] public void SharedTextLayer_FollowsHoverResizeAndVisibility()
        {
            var go=new GameObject("Ortak yazı katmanı",typeof(RectTransform));var source=new GameObject("Düğüm",typeof(RectTransform));
            try
            {
                var target=new GameObject("Metin çapası",typeof(RectTransform),typeof(CanvasGroup));target.transform.SetParent(go.transform,false);
                var layer=go.AddComponent<SadeMapTextLayer>();var so=new SerializedObject(layer);var entries=so.FindProperty("anchors");entries.arraySize=1;var entry=entries.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("Source").objectReferenceValue=source.transform;entry.FindPropertyRelative("Target").objectReferenceValue=target.transform;entry.FindPropertyRelative("Visibility").objectReferenceValue=target.GetComponent<CanvasGroup>();so.ApplyModifiedPropertiesWithoutUndo();
                source.transform.position=new Vector3(13,17,0);((RectTransform)source.transform).sizeDelta=new Vector2(64,52);layer.Refresh();Assert.AreEqual(source.transform.position,target.transform.position);Assert.AreEqual(new Vector2(64,52),((RectTransform)target.transform).sizeDelta);
                source.transform.position+=Vector3.up;layer.Refresh();Assert.AreEqual(source.transform.position,target.transform.position);
                source.SetActive(false);layer.Refresh();Assert.AreEqual(0,target.GetComponent<CanvasGroup>().alpha);source.SetActive(true);layer.Refresh();Assert.AreEqual(1,target.GetComponent<CanvasGroup>().alpha);
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(source);}
        }
        [Test] public void PreviewCommandsCannotTargetLiveRaid()
        {Assert.IsTrue(typeof(IRaidPreviewSource).IsAssignableFrom(typeof(MockRaidDataSource)));Assert.IsFalse(typeof(IRaidPreviewSource).IsAssignableFrom(typeof(LiveRaidDataSource)));}
    }
}
