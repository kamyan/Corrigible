using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Corrigible.Desk.Tests
{
    public class SadeRaidStateTests
    {
        GameObject root;MockRaidDataSource source;
        [SetUp]public void Setup()
        {
            root=new GameObject("F6 durum sınırı");root.SetActive(false);source=root.AddComponent<MockRaidDataSource>();var s=new SerializedObject(source);
            const string dir="Assets/Corrigible/Desk/Data/Sade/Mock/";s.FindProperty("boardStates").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TextAsset>(dir+"raid_board_states.json");s.FindProperty("theme").objectReferenceValue=SadeTheme.Active;var a=s.FindProperty("screenStates");a.arraySize=9;for(int i=0;i<9;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<TextAsset>(dir+"baskin_S"+(i+1)+".json");s.ApplyModifiedPropertiesWithoutUndo();
        }
        [TearDown]public void Cleanup()=>Object.DestroyImmediate(root);
        [TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)]
        public void Fixtures_PreservePanelStatesAndNeverConsumeRandom(int screen)
        {
            source.LoadPreset(screen);var vm=source.Current;Assert.AreEqual(0,source.State.Rs);
            if(screen==3){Assert.IsTrue(vm.Locked);Assert.IsTrue(vm.EventVisible);Assert.AreEqual("now",vm.Queue[2].State);source.Select('T');Assert.IsNull(source.Current.UnitCard);}
            if(screen==4){Assert.IsTrue(vm.EndConfirm);Assert.AreEqual(2,vm.Unused[0].ThreatOrder);}
            if(screen==5){Assert.AreEqual("log",vm.OpenPanel);Assert.Greater(vm.Log.Length,10);}
            if(screen==6)Assert.AreEqual("rules",vm.OpenPanel);
            if(screen==7){Assert.IsTrue(vm.LastTurn);Assert.IsTrue(vm.Team[1].Down);Assert.IsTrue(vm.Targets[1].Broken);Assert.AreEqual(1,vm.CivilianBroken);Assert.IsTrue(vm.HoverCard.Win);}
            if(screen==8){Assert.IsTrue(vm.RetreatArmed);source.Cancel();Assert.IsFalse(source.Current.RetreatArmed);}
            if(screen==9){Assert.IsTrue(vm.Finished);Assert.IsTrue(vm.Success);Assert.AreEqual(2,vm.Standing);}
        }
        [Test]public void UncertainIntent_IsNotPresentedAsCertainThreat()
        {
            source.LoadBoardPreset("B4");source.EndTurn();foreach(var unused in source.Current.Unused)if(unused.ThreatOrder>0)Assert.IsFalse(Baskin.Intents(source.State).First(i=>i.Order==unused.ThreatOrder).Uncertain);
        }
        [Test]public void BackupTargetRemainsInFormAfterDestruction()
        {source.LoadPreset(7);Assert.AreEqual(2,source.Current.Targets.Length);Assert.AreEqual(0,source.Current.Targets[1].Hp);Assert.IsFalse(source.Current.Finished);}
        [Test]public void RuleAndLegendKeysAreTranslated()
        {Assert.AreNotEqual("rules.1",RaidText.Get("rules.1"));Assert.AreNotEqual("legend.armor",RaidText.Get("legend.armor"));}
        [Test]public void LiveScene_UsesSerializedSadeSourceAndCommands()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                var live=all.OfType<LiveRaidDataSource>().Single();var hud=all.OfType<RaidHudView>().Single();var commands=all.OfType<RaidCommands>().Single();
                Assert.AreSame(live,new SerializedObject(hud).FindProperty("dataSource").objectReferenceValue);
                Assert.AreSame(commands,new SerializedObject(hud).FindProperty("commandSource").objectReferenceValue);
                Assert.AreSame(live,new SerializedObject(commands).FindProperty("source").objectReferenceValue);
                Assert.IsNotNull(new SerializedObject(hud).FindProperty("panels").objectReferenceValue);
                Assert.AreEqual(1,all.OfType<RaidEventSystemGate>().Count());
                Assert.AreEqual(0,all.OfType<MockRaidDataSource>().Count());
                Assert.AreEqual(0,all.Where(c=>c.GetType().Name=="RaidView").Count());
            }
            finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
