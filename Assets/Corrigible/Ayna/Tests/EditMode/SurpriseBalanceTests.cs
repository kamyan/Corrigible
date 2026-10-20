using System;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind;
using NUnit.Framework;
namespace Corrigible.Ayna.Tests
{
    public sealed class SurpriseBalanceTests
    {
        static Prediction[] Forecast(double confidence=1) => new[]{new Prediction{Kind=OrderKind.Expand,Region="weu",Probability=.05,Confidence=confidence},new Prediction{Kind=OrderKind.Expand,Region="na",Probability=.95,Confidence=confidence}};
        [Test] public void UnknownBehaviorDoesNotEarnSurprise_AndBonusCannotRepayTheOrder()
        {
            var unknown=AynaRulesTests.New();unknown.LockForecast(Side.Ayna,Forecast(0));unknown.ResolveCycle(new Plan(new[]{Order.Expand("weu",1)}),new Plan());Assert.That(unknown.View(Side.Player).LastSurprise,Is.False);
            var known=AynaRulesTests.New();known.LockForecast(Side.Ayna,Forecast());known.ResolveCycle(new Plan(new[]{Order.Expand("weu",1)}),new Plan());Assert.That(known.View(Side.Player).LastSurprise);Assert.That(known.View(Side.Player).Compute-unknown.View(Side.Player).Compute,Is.EqualTo(.5));
        }
        [Test] public void CheapSecondaryOrderCannotFarmTheBonus()
        {
            var s=AynaRulesTests.New();s.LockForecast(Side.Ayna,new[]{new Prediction{Kind=OrderKind.Expand,Region="weu",Probability=.81},new Prediction{Kind=OrderKind.Expand,Region="na",Probability=.09},new Prediction{Kind=OrderKind.Hide,Region="weu",Probability=.01},new Prediction{Kind=OrderKind.Hide,Region="ru",Probability=.09}});
            s.ResolveCycle(new Plan(new[]{Order.Expand("weu",2),new Order{Kind=OrderKind.Hide,Region="weu"}}),new Plan());Assert.That(s.View(Side.Player).LastSurprise,Is.False);
        }
        [Test] public void FullRegionBidDoesNotEarnSurprise()
        {
            var s=AynaRulesTests.New();AynaRulesTests.Give(s,"weu",0,AynaWorldData.Region("weu").Capacity);s.LockForecast(Side.Ayna,Forecast());s.ResolveCycle(new Plan(new[]{Order.Expand("weu",1)}),new Plan());Assert.That(s.View(Side.Player).Receipts.Single().Rejection,Is.EqualTo(RejectReason.None));Assert.That(s.View(Side.Player).LastSurprise,Is.False);
        }
        [Test] public void LegacyReplayRetainsOriginalBonus_AndProfileV2Migrates()
        {
            var s=AynaRulesTests.New(b:new AynaBalance{SurprisePolicy=0});s.LockForecast(Side.Ayna,Forecast(0));s.ResolveCycle(new Plan(new[]{Order.Expand("weu",1)}),new Plan());var r=s.Record;r.RulesVersion=r.FormatVersion=r.MindVersion=3;Assert.That(AynaReplay.Restore(r).StateHash,Is.EqualTo(s.StateHash));Assert.That(s.View(Side.Player).Compute,Is.EqualTo(new AynaBalance().StartCompute - new AynaBalance().ExpandCost + new AynaBalance().SurpriseCompute + 4 * new AynaBalance().IncomePerServer + new AynaBalance().CoreIncome).Within(1e-9));
            var p=new AynaProfileData{Version=2,PolicyMatches=new double[5],PolicyTrials=new double[5]};p.PolicyMatches[0]=p.PolicyTrials[0]=2;var migrated=p.Copy();Assert.That(migrated.Version,Is.EqualTo(3));Assert.That(migrated.PolicyMatches.Length,Is.EqualTo(6));Assert.That(migrated.PolicyMatches[0],Is.EqualTo(2));
        }
        [TestCase(false)] [TestCase(true)]
        public void FirstStrikeBelongsToTheSurprisingRegion(bool matching)
        {
            var s=BattleFlowTests.Encounter();string region=s.PendingBattle(Side.Player).Region;
            s.State.Forecasts[1].Cycle=s.State.Cycle;s.State.FirstStrike[0]=true;s.State.FirstStrikeRegion[0]=matching?region:"unrelated";
            Assert.That(s.StartBoard());Assert.That(s.Board(Side.Player).Phase==BoardPhase.FirstStrike,Is.EqualTo(matching));Assert.That(s.State.FirstStrike[0],Is.EqualTo(!matching));
        }
        [TestCase(false)] [TestCase(true)]
        public void ReorderingEqualMainOrdersCannotManufactureSurprise(bool reverse)
        {
            var s=AynaRulesTests.New();
            s.LockForecast(Side.Ayna,new[]{new Prediction{Kind=OrderKind.Expand,Region="weu",Probability=.05},new Prediction{Kind=OrderKind.Expand,Region="na",Probability=.85},new Prediction{Kind=OrderKind.Expand,Region="ru",Probability=.1}});
            var orders=new[]{Order.Expand("weu",1),Order.Expand("na",1)};
            s.ResolveCycle(new Plan(reverse?orders.Reverse():orders),new Plan());
            Assert.That(s.View(Side.Player).LastSurprise,Is.False);
        }
        [Test] public void SurprisingPeerSetDoesNotInventATacticalRegion()
        {
            var s=AynaRulesTests.New();
            s.LockForecast(Side.Ayna,new[]{new Prediction{Kind=OrderKind.Expand,Region="weu",Probability=.05},new Prediction{Kind=OrderKind.Expand,Region="na",Probability=.05},new Prediction{Kind=OrderKind.Expand,Region="ru",Probability=.9}});
            s.ResolveCycle(new Plan(new[]{Order.Expand("weu",1),Order.Expand("na",1)}),new Plan());
            Assert.That(s.View(Side.Player).LastSurprise);Assert.That(s.State.FirstStrikeRegion[0],Is.Null);
            Assert.That(AynaReplay.Restore(s.Record).StateHash,Is.EqualTo(s.StateHash));
        }
        [Test] public void ColdAndOffModelsHaveNoLearnedCore_WithoutSpendingRng()
        {
            var s=AynaRulesTests.New();foreach(var level in new[]{LearningLevel.Off,LearningLevel.Full}){
                var m=new MindSession(123,level:level);m.Observe(s.View(Side.Ayna));uint before=m.Rng.State;
                Assert.That(m.LearnedCore(s.View(Side.Ayna)),Is.Null);Assert.That(m.Rng.State,Is.EqualTo(before));
            }
        }
        [Test] public void ColdModelConfidenceUsesOnlyVerifiedEvidence()
        {
            var s=AynaRulesTests.New();var m=new MindSession(1);m.Observe(s.View(Side.Ayna));Assert.That(m.Predict(s.View(Side.Ayna)).All(p=>p.Confidence==0));Assert.That(m.Checkpoint().Behavior.Particles.Count,Is.EqualTo(6));
            var p=new AynaProfileData{Games=1};p.OrderCounts[0]=new AynaBalance().SurpriseEvidence;var warm=new MindSession(1,p);warm.Observe(s.View(Side.Ayna));Assert.That(warm.Predict(s.View(Side.Ayna)).Where(x=>x.Kind==OrderKind.Expand).All(x=>x.Confidence==1));
        }
    }
}
