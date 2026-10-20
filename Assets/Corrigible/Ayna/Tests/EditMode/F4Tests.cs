using System;
using System.IO;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Game;
using Corrigible.Ayna.Mind;
using Corrigible.Ayna.Mind.Bots;
using Corrigible.Ayna.Presentation;
using Corrigible.Ayna.EditorTools;
using NUnit.Framework;
using UnityEngine;
namespace Corrigible.Ayna.Tests
{
    public sealed class F4Tests
    {
        string root; GameObject go;
        [SetUp] public void Setup() { root=Path.Combine("Temp","AynaF4Tests",Guid.NewGuid().ToString("N")); go=new GameObject("AynaF4Test"); }
        [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(go); if(Directory.Exists(root))Directory.Delete(root,true); }
        AynaGameController Game(uint seed=7, AynaBalance balance=null)
        { var c=go.AddComponent<AynaGameController>();c.Initialize(root,balance);c.NewGame(Architecture.Ghost,"weu",seed);return c; }
        static void Finish(AynaGameController c)
        {
            int guard=0;
            while(c.Phase!=AynaPhase.End && guard++<400)
            {
                if(c.Phase==AynaPhase.Planning)
                { foreach(var o in new GreedyBot().MakePlan(c.Match.Simulation.View(Side.Player)).Orders) c.Queue(o); Assert.That(c.LockPlan()); }
                else if(c.Phase==AynaPhase.Conflict) Assert.That(c.AutoBattle());
                else c.Continue();
            }
            Assert.That(c.Phase,Is.EqualTo(AynaPhase.End));
        }
        [Test] public void EntireGame_CommitsOneGeneration_AndNextGameKeepsIt()
        { var c=Game(balance:new AynaBalance{MaxCycles=4});Finish(c);Assert.That(c.Screens.A12.ProfileCommitted);Assert.That(new AynaProfileStore(root).Load(LearningLevel.Full).Generation,Is.EqualTo(1));c.SaveGame();c.Home();c.LoadGame();Assert.That(c.Phase,Is.EqualTo(AynaPhase.End));c.Continue();Assert.That(new AynaProfileStore(root).Load(LearningLevel.Full).Generation,Is.EqualTo(1));c.Home();c.NewGame(Architecture.Ghost,"weu",8);Assert.That(c.Screens.A1.Generation,Is.EqualTo(2)); }
        [Test] public void SecondGenerationCanSaveRestoreThenSaveRestoreAgain()
        {
            var c=Game(balance:new AynaBalance{MaxCycles=1});Finish(c);c.Home();c.NewGame(Architecture.Ghost,"weu",8);
            c.RestoreSave(c.ExportSave());c.RestoreSave(c.ExportSave());
            Assert.That(c.Match.Simulation.Record.ProfileAtStart.Generation,Is.EqualTo(1));Assert.That(c.Match.AynaMind.ProfileAtStart.Generation,Is.EqualTo(1));
            Assert.That(new AynaProfileStore(root).Load(LearningLevel.Full).Generation,Is.EqualTo(1));
        }
        [Test] public void PlanningSaveKeepsQueue_Forecast_AndNextAiPlan()
        { var c=Game();Assert.That(c.Queue(Order.Expand("weu",1)));string json=c.ExportSave();var checkpoint=AynaJson.Write(c.Match.AynaMind.Checkpoint());c.SaveGame();c.ClearQueue();c.LoadGame();Assert.That(c.Screens.A2.Queue.Length,Is.EqualTo(1));Assert.That(AynaJson.Write(c.Match.AynaMind.Checkpoint()),Is.EqualTo(checkpoint));var next=AynaJson.Write(c.Match.MakeAynaPlan());c.RestoreSave(json);Assert.That(AynaJson.Write(c.Match.MakeAynaPlan()),Is.EqualTo(next)); }
        [Test] public void RejectedQueueDoesNotSpend_OrMutateReplay()
        { var c=Game();string before=AynaJson.Write(c.Match.Simulation.Record);double cash=c.Screens.A2.Compute;Assert.That(c.Queue(Order.Expand("not-a-region",1)),Is.False);Assert.That(c.Screens.A2.Compute,Is.EqualTo(cash));Assert.That(AynaJson.Write(c.Match.Simulation.Record),Is.EqualTo(before)); }
        [Test] public void ResetRequiresConfirmation_AndOffDoesNotCreateProfile()
        { var c=go.AddComponent<AynaGameController>();c.Initialize(root,new AynaBalance{MaxCycles=1});Assert.That(c.EraseMemory(),Is.False);Assert.That(File.Exists(Path.Combine(root,"ayna_profile.json")),Is.False);c.SetLearning(LearningLevel.Off);c.NewGame(Architecture.Ghost,"weu",1);Finish(c);Assert.That(File.Exists(Path.Combine(root,"ayna_profile.json")),Is.False);c.Home();c.RequestEraseMemory();Assert.That(c.EraseMemory());Assert.That(new AynaProfileStore(root).Load(LearningLevel.Full).Generation,Is.Zero); }
        [Test] public void MemoryEraseConfirmationExpiresWhenChangingScreenOrLoading()
        {
            var c=go.AddComponent<AynaGameController>();c.Initialize(root,new AynaBalance{MaxCycles=1});
            c.RequestEraseMemory();c.NewGame(Architecture.Ghost,"weu",1);Finish(c);
            Assert.That(c.EraseMemory(),Is.False);Assert.That(new AynaProfileStore(root).Load(LearningLevel.Full).Generation,Is.EqualTo(1));
            c.SaveGame();c.RequestEraseMemory();c.LoadGame();Assert.That(c.EraseMemory(),Is.False);
            c.RequestEraseMemory();c.Home();Assert.That(c.EraseMemory(),Is.False);
        }
        [Test] public void CorruptSaveIsAtomic_AndLeavesCurrentGameIntact()
        { var c=Game();string before=c.ExportSave();Assert.Throws<ArgumentException>(()=>c.RestoreSave("{}"));Assert.That(c.ExportSave(),Is.EqualTo(before)); }
        [Test] public void FixtureUsesPerception_AndCorrectedQueueCosts()
        { var s=AynaExampleFixture.Create();Assert.That(s.A2.Cycle,Is.EqualTo(7));Assert.That(s.A2.Cost,Is.EqualTo(19));Assert.That(s.A2.Noise,Is.EqualTo(2));Assert.That(s.A2.Preview.All(p=>p.Rejection==RejectReason.None));var china=s.A2.Regions.Single(r=>r.Id=="cn");Assert.That(china.Knowledge,Is.Not.EqualTo(Knowledge.Exact));Assert.That(china.EnemyMin,Is.Zero);Assert.That(s.A5.UnknownEnemy,Is.Zero);Assert.That(s.A6.Tiles.Length,Is.EqualTo(64));Assert.That(s.A2.Regions.Single(r=>r.Id=="in").Knowledge,Is.EqualTo(Knowledge.Exact)); }
        [Test] public void ViewModelsRejectEnemyView_AndDoNotReadHiddenCore()
        { var sim=AynaRulesTests.New();Assert.Throws<ArgumentException>(()=>AynaViewModels.Build(sim.View(Side.Ayna),null,new AynaProfileData(),AynaPhase.Planning,LearningLevel.Full,new Plan(),Array.Empty<OrderReceipt>(),null,null,Array.Empty<double>(),0,0,0,false));var c=Game();Assert.That(c.Screens.A2.Regions.Where(r=>r.Knowledge==Knowledge.Unknown).All(r=>r.EnemyMin==0)); }
        [Test] public void BoardSaveUndoAndCompletion_WorkThroughController()
        {
            var c=Game(1781,new AynaBalance{MaxCycles=30,StartCompute=30}); int guard=0;
            while(c.Phase!=AynaPhase.Conflict && c.Phase!=AynaPhase.End && guard++<150)
            {if(c.Phase==AynaPhase.Planning){foreach(var o in PlanFactory.Build(c.Match.Simulation.View(Side.Player),BotStyle.Aggro,0).Orders)c.Queue(o);c.LockPlan();}else c.Continue();}
            Assert.That(c.Phase,Is.EqualTo(AynaPhase.Conflict));c.ChooseRoster();Assert.That(c.CommandBattle());string save=c.ExportSave();string hash=c.Match.Simulation.StateHash;c.RestoreSave(save);Assert.That(c.Match.Simulation.StateHash,Is.EqualTo(hash));
            var b=c.Match.Simulation.Board(Side.Player);var command=BoardSimulation.CreateBelief(b).LegalCommands(Side.Player).FirstOrDefault();
            if(command!=null && c.BoardCommand(command) && c.Phase==AynaPhase.Board){c.UndoBoard();}
            c.RestoreSave(save);c.Retreat();Assert.That(c.Phase,Is.EqualTo(AynaPhase.BoardResult));c.Continue();Assert.That(c.Phase==AynaPhase.Results||c.Phase==AynaPhase.Conflict);
        }
    }
}
