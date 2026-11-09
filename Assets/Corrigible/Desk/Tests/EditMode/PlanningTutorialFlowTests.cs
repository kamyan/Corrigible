using System.Linq;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
namespace Corrigible.Desk.Tests
{
    public sealed class PlanningTutorialFlowTests
    {
        [Test] public void FirstShift_RequiresRealActions_AndPhysicalFiling()
        {
            var f=new PlanningTutorialFlow();var c=new PlanningTutorialContext();
            void At(string id)=>Assert.AreEqual(id,f.Current?.Id);
            At("intro");f.Update(c);At("intro");f.Acknowledge();f.Update(c);At("board");
            c.BoardOpen=true;f.Update(c);At("person");c.PersonSelected=true;f.Update(c);At("assign");f.Update(c);At("assign");
            c.AssignedCook=true;f.Update(c);At("gear");f.Acknowledge();f.Update(c);At("slip");c.SlipOpen=true;f.Update(c);At("order");
            c.RaidOrdered=c.RaidLoaded=true;f.Update(c);At("raidSelect");c.RaidSelected=true;f.Update(c);At("raidMove");
            c.RaidMoved=true;f.Update(c);At("raidAbility");c.RaidActed=true;f.Update(c);At("raidTurn");c.RaidTurnEnded=true;f.Update(c);At("raid");
            c.RaidOver=true;f.Update(c);At("result");c.RaidOrdered=c.RaidLoaded=false;c.Raids=1;f.Update(c);At("result");
            c.ReportOpen=true;f.Update(c);At("report");c.ReportOpen=false;f.Update(c);At("rest");f.Acknowledge();f.Update(c);At("print");
            c.Printed=true;f.Update(c);At("file");f.Update(c);At("file");c.Day=1;f.Update(c);At("go");f.Acknowledge();Assert.IsTrue(f.Finished);
        }
        [Test] public void EarlierRaidOrEarlyLoss_DoesNotStrandTheGuideInPreparationOrActions()
        {
            var f=new PlanningTutorialFlow();var c=new PlanningTutorialContext{RaidOrdered=true};f.Update(c);Assert.AreEqual("raidSelect",f.Current.Id);
            c.RaidOver=true;f.Update(c);Assert.AreEqual("result",f.Current.Id);
        }
        [Test] public void BlockingSequence_Waits_AndResumeRestoresOnlyAValidStep()
        {
            var f=new PlanningTutorialFlow(1);var c=new PlanningTutorialContext{Blocked=true,BoardOpen=true};f.Update(c);Assert.AreEqual("board",f.Current.Id);
            c.Blocked=false;f.Update(c);Assert.AreEqual("person",f.Current.Id);
            Assert.AreEqual(0,new PlanningTutorialFlow(-1).Index);Assert.AreEqual(PlanningTutorialFlow.Steps.Length-1,new PlanningTutorialFlow(999).Index);
        }
        [Test] public void RealAssignment_AlsoCountsAsHandlingAPersonCard()
        {
            var f=new PlanningTutorialFlow(2);f.Update(new PlanningTutorialContext{AssignedCook=true});Assert.AreEqual("gear",f.Current.Id);
        }
        [Test] public void Steps_AreUniqueAndSkippable_WithoutGameState()
        {
            CollectionAssert.AllItemsAreUnique(PlanningTutorialFlow.Steps.Select(s=>s.Id));Assert.IsTrue(PlanningTutorialFlow.Steps.All(s=>!string.IsNullOrWhiteSpace(s.Body)&&(s.Info||s.Done!=null)));
            var f=new PlanningTutorialFlow();f.Skip();Assert.IsFalse(f.Update(new PlanningTutorialContext()));Assert.IsNull(f.Current);
        }
    }
}
