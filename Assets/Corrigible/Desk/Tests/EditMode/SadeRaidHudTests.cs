using System.Linq;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Corrigible.Desk.Tests
{
    public class SadeRaidHudTests
    {
        GameObject owner;MockRaidDataSource source;
        void Ref(string name,Object value){var so=new SerializedObject(source);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        [SetUp]public void Setup()
        {
            owner=new GameObject("Mock veri sınırı");owner.SetActive(false);source=owner.AddComponent<MockRaidDataSource>();
            const string root="Assets/Corrigible/Desk";
            Ref("boardStates",AssetDatabase.LoadAssetAtPath<TextAsset>(root+"/Data/Sade/Mock/raid_board_states.json"));Ref("theme",SadeTheme.Active);
            var so=new SerializedObject(source);var a=so.FindProperty("screenStates");a.arraySize=2;for(int i=0;i<2;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<TextAsset>(root+"/Data/Sade/Mock/baskin_S"+(i+1)+".json");so.ApplyModifiedPropertiesWithoutUndo();source.LoadPreset(1);
        }
        [TearDown]public void Cleanup(){Object.DestroyImmediate(owner);}
        [Test]public void MoveUndo_RestoresPositionRightsAndDoesNotConsumeRandom()
        {
            source.Select('T');var before=source.State.Units[0].Clone();int rng=source.State.Rs;
            var destination=source.Current.Moves.First();source.Move(destination);
            Assert.IsFalse(source.Current.Team[0].Move);Assert.IsTrue(source.Current.CanUndo);Assert.AreEqual(destination.x,source.State.Units[0].X);
            source.Undo();Assert.AreEqual(before.X,source.State.Units[0].X);Assert.AreEqual(before.Y,source.State.Units[0].Y);Assert.IsTrue(source.Current.Team[0].Move);Assert.AreEqual(rng,source.State.Rs);
        }
        [Test]public void Preview_IsReadOnlyAndCancelUnwindsArmedThenSelectionThenPanel()
        {
            source.LoadPreset(2);int hp=source.State.Units[6].Hp;var positions=source.State.Units.Select(u=>(u.X,u.Y,u.Hp)).ToArray();
            source.Hover(new Vector2Int(4,6));Assert.IsTrue(source.Current.AbilityArmed);CollectionAssert.AreEqual(positions,source.State.Units.Select(u=>(u.X,u.Y,u.Hp)).ToArray());
            source.Cancel();Assert.IsFalse(source.Current.AbilityArmed);Assert.IsNotNull(source.Current.UnitCard);source.Cancel();Assert.IsNull(source.Current.UnitCard);
            source.Panel("log");source.Cancel();Assert.IsNull(source.Current.OpenPanel);Assert.IsFalse(source.Current.Paused);
        }
        [Test]public void AbilityUndo_RestoresVictimAndActionRights()
        {
            source.LoadPreset(2);var before=source.State.Units.Select(u=>(u.Id,u.Hp,u.X,u.Y)).ToArray();source.Use(new Vector2Int(4,6));
            Assert.IsFalse(source.Current.Team[0].Ability);Assert.IsTrue(source.Current.CanUndo);source.Undo();CollectionAssert.AreEqual(before,source.State.Units.Select(u=>(u.Id,u.Hp,u.X,u.Y)).ToArray());Assert.IsTrue(source.Current.Team[0].Ability);
        }
        [Test]public void TurnRequiresConfirmationThenLocksAllCommands()
        {
            source.EndTurn();Assert.IsTrue(source.Current.EndConfirm);Assert.IsFalse(source.Current.Locked);source.Cancel();Assert.IsFalse(source.Current.EndConfirm);
            source.EndTurn();source.EndTurn();Assert.IsTrue(source.Current.Locked);source.Select('T');Assert.IsNull(source.Current.UnitCard);source.Arm();Assert.IsFalse(source.Current.AbilityArmed);
            source.Speed(true);Assert.IsTrue(source.Current.FastMachines);source.Speed(false);Assert.IsFalse(source.Current.FastMachines);
        }
        [TestCase('T',2)][TestCase('E',2)][TestCase('A',3)]public void Selection_UsesUnitSpecificRows(char unit,int count)
        {source.Select(unit);Assert.AreEqual(count,source.Current.UnitRows.Length);Assert.AreEqual(unit,source.Current.UnitCard.Letter);}
        [Test]public void BadDestinationsCannotMoveOrDamageAndRetreatRequiresTwoRequests()
        {source.Select('T');source.Move(new Vector2Int(-1,-1));source.Arm();source.Use(new Vector2Int(99,99));Assert.IsFalse(source.Current.CanUndo);source.Retreat();Assert.IsTrue(source.Current.RetreatArmed);source.Cancel();Assert.IsFalse(source.Current.RetreatArmed);source.Retreat();source.Retreat();Assert.IsTrue(source.State.Retreated);}
    }
}
