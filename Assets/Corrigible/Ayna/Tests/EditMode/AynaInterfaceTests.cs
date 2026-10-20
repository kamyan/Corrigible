using System;
using System.Linq;
using System.Reflection;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.EditorTools;
using Corrigible.Ayna.Game;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Corrigible.Ayna.Tests
{
    public sealed class AynaInterfaceTests
    {
        [Test] public void ProductionScene_HasEveryRequiredInterfaceReference()
        {
            const string path="Assets/Corrigible/Ayna/Scenes/Ayna.unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AynaInterfaceView>(true)).Single();
                foreach(var field in typeof(AynaInterfaceView).GetFields(BindingFlags.Instance|BindingFlags.NonPublic).Where(f=>f.GetCustomAttribute<SerializeField>()!=null))
                {
                    var value=field.GetValue(view);Assert.That(value,Is.Not.Null,field.Name);
                    if(value is Array array){Assert.That(array.Length,Is.GreaterThan(0),field.Name);foreach(var item in array)Assert.That(item,Is.Not.Null,field.Name);}
                }
                var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true));
                Assert.That(transforms.Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),Is.Zero);
                Assert.That(view.Controller,Is.Not.Null);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        [Test] public void DisplayCostsAndHealth_ComeFromCurrentBalance()
        {
            var v=AynaExampleFixture.Create();var rules=new AynaBalance();
            foreach(var army in v.A9.Armies)foreach(var unit in army.Units)Assert.That(unit.MaxHp,Is.EqualTo(rules.UnitHp[(int)unit.Kind]));
            foreach(var node in v.A8.Nodes){Assert.That(node.Cost,Is.EqualTo(rules.TrainingCosts[node.Tier-1]));Assert.That(node.Duration,Is.EqualTo(rules.TrainingDurations[node.Tier-1]));}
            Assert.That(v.A2.DominanceTarget,Is.EqualTo((int)Math.Ceiling(AynaWorldData.Regions.Sum(r=>r.Capacity)*rules.DominanceShare)));
            Assert.That(v.A8.SingularityCost,Is.EqualTo(rules.SingularityCost));Assert.That(v.A9.MaxUnits,Is.EqualTo(rules.MaxUnits));
        }
        [Test] public void CancelMemoryErasure_RemovesThePendingConfirmation()
        {
            var go=new GameObject("Confirmation test");var path="Temp/AynaInterfaceEraseTest"+Guid.NewGuid().ToString("N");
            try{var controller=go.AddComponent<AynaGameController>();controller.Initialize(path);controller.RequestEraseMemory();Assert.That(controller.EraseConfirmationPending,Is.True);controller.CancelEraseMemory();Assert.That(controller.EraseConfirmationPending,Is.False);Assert.That(controller.EraseMemory(),Is.False);}
            finally{UnityEngine.Object.DestroyImmediate(go);if(System.IO.Directory.Exists(path))System.IO.Directory.Delete(path,true);}
        }
        [Test] public void IsometricBoard_AllCellCentresCanBeClicked_OutsideRejected()
        {
            var go=new GameObject("Board hit test",typeof(RectTransform),typeof(UnityEngine.CanvasRenderer));
            try
            {
                var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(1700,880);rect.position=new Vector3(1000,600,0);
                var board=go.AddComponent<AynaBoardGraphic>();
                var measure=typeof(AynaBoardGraphic).GetMethod("Measure",BindingFlags.Instance|BindingFlags.NonPublic);var cell=typeof(AynaBoardGraphic).GetMethod("Cell",BindingFlags.Instance|BindingFlags.NonPublic);
                measure.Invoke(board,null);
                for(int i=0;i<64;i++){var local=(Vector2)cell.Invoke(board,new object[]{i});var screen=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(local));Assert.That(board.CellAtScreen(screen,null),Is.EqualTo(i),"Cell "+i);}
                Assert.That(board.CellAtScreen(new Vector2(-5000,-5000),null),Is.EqualTo(-1));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
