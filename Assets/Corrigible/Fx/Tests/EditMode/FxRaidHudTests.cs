using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Corrigible.Fx.Tests
{
    public sealed class FxRaidHudTests
    {
        GameObject root;FxRaidSource source;
        [SetUp] public void SetUp()
        {
            root=new GameObject("FX Baskın veri testi");root.SetActive(false);source=root.AddComponent<FxRaidSource>();
            source.boardStates=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Sade/Mock/raid_board_states.json");
            source.screenStates=Enumerable.Range(1,9).Select(i=>AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Sade/Mock/baskin_S"+i+".json")).ToArray();
            source.ConfigureTheme(AssetDatabase.LoadAssetAtPath<SadeTheme>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:SadeTheme")[0])));
        }
        [TearDown] public void TearDown()=>Object.DestroyImmediate(root);
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)]
        public void FixturesHaveSharedViewModelAndDoNotConsumeRandomness(int screen)
        {
            source.LoadPreset(screen);var vm=source.Current;Assert.That(vm.Team.Length,Is.EqualTo(3));Assert.That(vm.Targets.Length,Is.GreaterThan(0));Assert.That(source.State.Rs,Is.Zero);
            Assert.That(vm.Locked,Is.EqualTo(screen==3||screen==9));Assert.That(vm.EndConfirm,Is.EqualTo(screen==4));Assert.That(vm.RetreatArmed,Is.EqualTo(screen==8));Assert.That(vm.Finished,Is.EqualTo(screen==9));
            Assert.That(vm.OpenPanel,Is.EqualTo(screen==5?"log":screen==6?"rules":null));
        }
        [TestCase(1)] [TestCase(2)] [TestCase(4)]
        public void MachinePresentationResolvesExactlyOnceAndMatchesCanonicalResult(int screen)
        {
            source.LoadPreset(screen);var expected=source.State.Clone();Assert.That(Baskin.EndTurn(expected),Is.True);
            source.EndWithFx(0);if(!source.MachineRunning)source.EndWithFx(0);Assert.That(source.MachineRunning,Is.True);
            source.EndWithFx(0);for(int i=0;i<300&&source.MachineRunning;i++)source.TickMachines(i+1);
            Assert.That(source.MachineRunning,Is.False);Assert.That(JsonConvert.SerializeObject(source.State),Is.EqualTo(JsonConvert.SerializeObject(expected)));
        }
        [Test] public void PreviewDoesNotMutateStateAndUndoRestoresTheMove()
        {
            source.LoadPreset(2);var before=JsonConvert.SerializeObject(source.State);Assert.That(source.Preview,Is.Not.Null);Assert.That(source.Preview.Fx.Count,Is.GreaterThan(0));source.ClearHover();Assert.That(JsonConvert.SerializeObject(source.State),Is.EqualTo(before));
            source.LoadPreset(1);source.Select('T');var unit=source.State.Units.First(x=>x.Type=="tek");var from=new Vector2Int(unit.X,unit.Y);var to=source.Current.Moves.First();source.Move(to);Assert.That(source.Current.CanUndo,Is.True);source.Undo();Assert.That(new Vector2Int(source.State.Units.First(x=>x.Type=="tek").X,source.State.Units.First(x=>x.Type=="tek").Y),Is.EqualTo(from));
        }
        [Test] public void RetreatRequiresSecondCommandAndReportReturnsToMockStart()
        {
            source.LoadPreset(1);source.Retreat();Assert.That(source.Current.RetreatArmed,Is.True);Assert.That(source.Current.Finished,Is.False);source.Retreat();Assert.That(source.Current.Finished,Is.True);Assert.That(source.Current.Success,Is.False);source.Continue();Assert.That(source.ContinueCount,Is.EqualTo(1));Assert.That(source.Screen,Is.EqualTo(1));Assert.That(source.Current.Finished,Is.False);
        }
        [Test] public void SpotHasTransparentInteriorAndSoftOuterBand()
        {
            var canvas=new GameObject("Spot testi",typeof(RectTransform),typeof(Canvas));var go=new GameObject("Spot",typeof(RectTransform));go.transform.SetParent(canvas.transform,false);var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(1920,1080);var spot=go.AddComponent<FxRaidSpot>();
            try{spot.color=new Color(0,0,0,.72f);spot.SetSpot(Vector2.zero,150,0,Color.clear);Canvas.ForceUpdateCanvases();Assert.That(spot.canvasRenderer,Is.Not.Null);var mesh=spot.canvasRenderer.GetMesh();bool inner=false,outer=false;for(int i=0;i<mesh.vertexCount;i++){var radius=((Vector2)mesh.vertices[i]).magnitude;if(Mathf.Abs(radius-129)<.01f&&mesh.colors32[i].a==0)inner=true;if(Mathf.Abs(radius-150)<.01f&&mesh.colors32[i].a>180)outer=true;}Assert.That(inner&&outer,Is.True);Assert.That(spot.raycastTarget,Is.True);}finally{Object.DestroyImmediate(canvas);}
        }
        [Test] public void LogFadeCoversTopAndLeavesBottomTransparent()
        {
            var canvas=new GameObject("Solma testi",typeof(RectTransform),typeof(Canvas));var go=new GameObject("Solma",typeof(RectTransform));go.transform.SetParent(canvas.transform,false);((RectTransform)go.transform).sizeDelta=new Vector2(352,46);var fade=go.AddComponent<FxRaidLogFade>();
            try{fade.color=Color.black;Canvas.ForceUpdateCanvases();var colors=fade.canvasRenderer.GetMesh().colors32;Assert.That(colors.Length,Is.EqualTo(4));Assert.That(colors[0].a,Is.Zero);Assert.That(colors[1].a,Is.EqualTo(255));Assert.That(colors[2].a,Is.EqualTo(255));Assert.That(colors[3].a,Is.Zero);}finally{Object.DestroyImmediate(canvas);}
        }
        [Test] public void AuthoredPanelsHaveNoMissingScriptsAndRuntimeHasNoLiveController()
        {
            var guids=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Corrigible/Fx/Prefabs/Raid/F6"});Assert.That(guids.Length,Is.EqualTo(10));
            foreach(var guid in guids){var go=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));Assert.That(go.GetComponent<FxPopupView>(),Is.Not.Null);foreach(var t in go.GetComponentsInChildren<Transform>(true))Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),Is.Zero);}
            Assert.That(File.ReadAllText("Assets/Corrigible/Fx/Scripts/Runtime/Raid/FxRaidHud.cs"),Does.Not.Contain("GameController"));
        }
    }
}
