using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
namespace Corrigible.Desk.Tests
{
    public sealed class PlanningTutorialFlowTests
    {
        static void At(PlanningTutorialFlow f,string id)=>Assert.AreEqual(id,f.Current?.Id);
        /// <summary>İlk vardiya dosyalanmış, hedef onaylanmış: konu bölümünün başı.</summary>
        static PlanningTutorialFlow InTopics(PlanningTutorialContext c)
        {
            var f=new PlanningTutorialFlow(PlanningTutorialFlow.GoalIndex);c.Day=System.Math.Max(c.Day,1);f.Update(c);At(f,"goal");f.Acknowledge();f.Update(c);return f;
        }

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
            c.Printed=true;f.Update(c);At("file");f.Update(c);At("file");c.Day=1;c.Printed=false;f.Update(c);At("goal");
            f.Acknowledge();f.Update(c);At("watch");Assert.IsFalse(f.Finished,"rehber ilk aday grubunu ve ilk olayı bekler");
        }
        [Test] public void EarlierRaidOrEarlyLoss_DoesNotStrandTheGuideInPreparationOrActions()
        {
            var f=new PlanningTutorialFlow();var c=new PlanningTutorialContext{RaidOrdered=true};f.Update(c);Assert.AreEqual("raidSelect",f.Current.Id);
            c.RaidOver=true;f.Update(c);Assert.AreEqual("result",f.Current.Id);
        }
        [Test] public void FiledFirstShift_SkipsTheFirstShiftSteps_ButNotTheGoal()
        {
            var f=new PlanningTutorialFlow(1);var c=new PlanningTutorialContext{Day=1};f.Update(c);At(f,"goal");
            // Hedef bir bilgi adımıdır: kendiliğinden geçilmez; sonraki vardiyada sefere çıkmak geri götürmez.
            c.RaidOrdered=true;c.Day=2;f.Update(c);At(f,"goal");
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

        // ---------------------------------------------------------------- KOS aşama 8: aday grubu, olay, İFŞA, son şalter

        [Test] public void Topics_EventThenGroup_FollowTheGame_ThenExposureSwitchAndEnd()
        {
            var c=new PlanningTutorialContext();var f=InTopics(c);At(f,"watch");Assert.IsTrue(f.InTopics);
            c.Day=2;c.EventOpen=true;f.Update(c);At(f,"event");
            c.EventInHand=true;f.Update(c);At(f,"decide");
            c.EventInHand=false;f.Update(c);At(f,"event");   // tepsiye geri kondu: önce al
            c.EventInHand=true;f.Update(c);At(f,"decide");
            c.EventInHand=c.EventOpen=false;c.EventDone=true;f.Update(c);At(f,"watch");
            c.Day=3;c.GroupWaiting=true;f.Update(c);At(f,"door");
            c.AtCounter=true;f.Update(c);At(f,"stamp");
            c.AtCounter=false;f.Update(c);At(f,"door");      // tezgâhtan ayrıldı: geri götürür
            c.AtCounter=true;f.Update(c);At(f,"stamp");
            c.GroupWaiting=false;c.GroupDone=true;f.Update(c);At(f,"exposure");Assert.IsFalse(f.InTopics);
            f.Acknowledge();f.Update(c);At(f,"switch");f.Acknowledge();f.Update(c);At(f,"go");f.Acknowledge();Assert.IsTrue(f.Finished);
        }
        [Test] public void Topics_GroupFirst_AndASecondEventDoesNotReopenADoneTopic()
        {
            var c=new PlanningTutorialContext();var f=InTopics(c);
            c.Day=3;c.GroupWaiting=true;f.Update(c);At(f,"door");
            // Grup varken olay da açıldı: sıradaki konu olaydır (önce karar, sonra kapı)
            c.EventOpen=true;f.Update(c);At(f,"event");c.EventOpen=false;c.EventDone=true;f.Update(c);At(f,"door");
            c.GroupWaiting=false;c.GroupDone=true;f.Update(c);At(f,"exposure");
            c.EventOpen=true;c.GroupWaiting=true;f.Update(c);At(f,"exposure");
        }
        [Test] public void Topics_ExpiredGroupCountsAsDone_AndWatchEndsOnItsLastDay()
        {
            var c=new PlanningTutorialContext{EventDone=true};var f=InTopics(c);At(f,"watch");
            c.Day=PlanningTutorialFlow.WatchLastDay-1;f.Update(c);At(f,"watch");
            c.Day=PlanningTutorialFlow.WatchLastDay;c.GroupWaiting=true;f.Update(c);At(f,"door");   // kapıda grup varken son gün de bekler
            c.GroupWaiting=false;f.Update(c);At(f,"exposure");                                    // konu çıkmadan son gün: rehber sürer, mühür kalkar
            var g=InTopics(new PlanningTutorialContext());var d=new PlanningTutorialContext{Day=4,GroupDone=true,EventDone=true};g.Update(d);At(g,"exposure");
        }
        [Test] public void Resume_InsideTopics_RepicksFromTheGame()
        {
            int door=PlanningTutorialFlow.IndexOf("door");
            var f=new PlanningTutorialFlow(door);f.Update(new PlanningTutorialContext{Day=5});At(f,"watch");
            f=new PlanningTutorialFlow(door);f.Update(new PlanningTutorialContext{Day=5,EventOpen=true,EventInHand=true});At(f,"decide");
            f=new PlanningTutorialFlow(door);f.Update(new PlanningTutorialContext{Day=5,GroupDone=true,EventDone=true});At(f,"exposure");
        }
        [Test] public void Blocked_DoesNotPickATopic()
        {
            var c=new PlanningTutorialContext();var f=InTopics(c);c.GroupWaiting=true;c.Blocked=true;Assert.IsFalse(f.Update(c));At(f,"watch");
            c.Blocked=false;f.Update(c);At(f,"door");
        }
        [Test] public void Bodies_FillLiveValues_GoalLineReadsTheFourCounters()
        {
            var sim=Simulation.NewGame(79,new Balance());
            Loc.SetLanguage(Loc.Turkish);
            string goal=PlanningTutorialFlow.GoalLine(sim);
            int total=sim.S.Liberation.Regions.Sum(r=>r.Sectors.Count);
            StringAssert.Contains("sektör\u00a00/"+total,goal);   // etiket ve değer bölünmez boşlukla bağlı
            StringAssert.Contains("güven\u00a0"+sim.S.Trust,goal);
            StringAssert.Contains("/"+Loc.Num(sim.B.liberationAlarmThreshold,"0.#"),goal);
            StringAssert.Contains("ifşa\u00a00/"+sim.B.exposureThreshold,goal);
            foreach(var s in PlanningTutorialFlow.Steps)
            {
                string body=PlanningTutorialFlow.BodyText(s,sim);
                Assert.IsFalse(body.Contains("{0}"),s.Id+": yer tutucu dolmadı");
                if(s.Id=="goal"||s.Id=="watch")StringAssert.EndsWith(goal,body);
            }
            StringAssert.Contains(sim.B.lastSwitchMinShift+". vardiyadan",PlanningTutorialFlow.BodyText(PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf("switch")],sim));
            StringAssert.Contains("İfşa "+sim.B.exposureThreshold,PlanningTutorialFlow.BodyText(PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf("exposure")],sim));
            StringAssert.Contains(Loc.Num(sim.DismissSuppliesPreview,"0.##")+" erzak",PlanningTutorialFlow.BodyText(PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf("stamp")],sim));
        }
        [Test] public void OldAdmissionPath_LeftNoGuideText()
        {
            foreach(var s in PlanningTutorialFlow.Steps)
                foreach(var word in new[]{"büyüteç","talimat","gözetmen","interkom","gözetleme","KISITLI","YARDIM"})
                    Assert.IsFalse(s.Body.Contains(word)||s.Title.Contains(word),s.Id+": eski kabul yolunun sözcüğü: "+word);
        }
    }
}
