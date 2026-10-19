using System.Linq;
using Corrigible.Core;
using Corrigible.Desk;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Corrigible.Fx.Tests
{
    public sealed class FxLiveIntegrationTests
    {
        SceneSetup[] setup;
        const string Root="Assets/Corrigible/Fx/Scenes/";
        [SetUp] public void Before()=>setup=EditorSceneManager.GetSceneManagerSetup();
        [TearDown] public void After(){if(setup.Any(s=>s.isLoaded)&&setup.Count(s=>s.isActive)==1)EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        [Test] public void LiveDeskHasRealRegionsCommandsAndAllRequiredScreens()
        {
            var scene=EditorSceneManager.OpenScene(Root+"FxDeskGame.unity");
            var surface=Object.FindFirstObjectByType<FxLiveDeskSurface>();
            Assert.That(surface,Is.Not.Null);Assert.That(surface.source.Game,Is.Not.Null);
            var i=surface.interaction;
            Assert.That(i.source,Is.Null);Assert.That(i.commands,Is.Null);
            CollectionAssert.AreEquivalent(GameData.Regions.Select(r=>r.Id),i.map.regions.Select(r=>r.Id));
            foreach(var region in i.map.regions){Assert.That(region.Node.regionId,Is.EqualTo(region.Id));Assert.That(region.HitArea.RegionId,Is.EqualTo(region.Id));}
            Assert.That(i.map.teams.Length,Is.GreaterThanOrEqualTo(Simulation.TeamMax));
            Assert.That(i.map.reports.Length,Is.GreaterThanOrEqualTo(4));
            Assert.That(Object.FindFirstObjectByType<FxDebugMenu>(FindObjectsInactive.Include),Is.Null);
            Assert.That(Object.FindFirstObjectByType<MockSadeDeskDataSource>(FindObjectsInactive.Include),Is.Null);
            var presenter=Object.FindFirstObjectByType<DeskPresenter>();var so=new SerializedObject(presenter);
            Assert.That(so.FindProperty("alternateSurface").objectReferenceValue,Is.SameAs(surface));
            foreach(var field in new[]{"intro","campaign","eventModal","gameOver","fakeWin","settingsView","tutorial"})Assert.That(so.FindProperty(field).objectReferenceValue,Is.Not.Null,field);
            Assert.That(Object.FindFirstObjectByType<RaidLauncher>().RaidSceneName,Is.EqualTo("FxBaskin"));
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),Is.Zero,t.name);
        }
        [Test] public void LiveRaidUsesCanonicalSourceAndSingleInputGate()
        {
            var scene=EditorSceneManager.OpenScene(Root+"FxBaskin.unity");var hud=Object.FindFirstObjectByType<FxRaidHud>();
            Assert.That(hud.source,Is.TypeOf<FxLiveRaidSource>());Assert.That(hud.source,Is.Not.InstanceOf<IRaidPreviewSource>());
            Assert.That(hud.board.sandboxHotkeys,Is.False);Assert.That(Object.FindFirstObjectByType<FxRaidSource>(),Is.Null);
            Assert.That(Object.FindFirstObjectByType<FxDebugMenu>(FindObjectsInactive.Include),Is.Null);
            Assert.That(Object.FindFirstObjectByType<RaidEventSystemGate>(),Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<EventSystem>().sendNavigationEvents,Is.False);
            Assert.That(Object.FindFirstObjectByType<GameController>(),Is.Null);
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),Is.Zero,t.name);
        }
        [Test] public void BuildStartsAtLiveFxAndKeepsOriginalScenesAvailable()
        {
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            Assert.That(scenes[0],Is.EqualTo(Root+"FxDeskGame.unity"));
            Assert.That(scenes,Does.Contain(Root+"FxBaskin.unity"));
            Assert.That(scenes,Does.Contain("Assets/Corrigible/Desk/Scenes/DeskGame.unity"));
            Assert.That(scenes,Does.Contain("Assets/Corrigible/Scenes/Baskin.unity"));
        }
    }
}
