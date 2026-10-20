using Corrigible.Ayna.Core;
using Corrigible.Ayna.EditorTools;
using NUnit.Framework;
namespace Corrigible.Ayna.Tests
{
    public sealed class BalanceBenchTests
    {
        [Test] public void SetupMirrorsOnlyGeography_AndPreservesFixedArchitectures()
        {
            var options = new BalanceBenchOptions();
            var a = AynaBalanceBench.Setup(options, 7, 0, Architecture.Ghost, Architecture.Fortress);
            var b = AynaBalanceBench.Setup(options, 7, 1, Architecture.Ghost, Architecture.Fortress);
            Assert.That(a.Player.Region, Is.EqualTo(b.Ayna.Region)); Assert.That(a.Ayna.Region, Is.EqualTo(b.Player.Region));
            Assert.That(a.Seed, Is.EqualTo(b.Seed)); Assert.That(AynaWorldData.Distance(a.Player.Region, a.Ayna.Region), Is.GreaterThanOrEqualTo(2));
            Assert.That(b.Player.Architecture, Is.EqualTo(Architecture.Ghost)); Assert.That(b.Ayna.Architecture, Is.EqualTo(Architecture.Fortress));
            Assert.That(a.Settings.Learning, Is.EqualTo(LearningLevel.Full));
        }
        [Test] public void RecordedBalanceKeepsThePreF5EconomyAfterDefaultTuning()
        {
            var old = new AynaBalance { IncomePerServer = 1, CoreIncome = 2, MarchCost = 1, DominanceShare = .6, SwarmExpandLimit = 4, FortressExpandLimit = 2 };
            var sim = AynaRulesTests.New(b: old); AynaRulesTests.Step(sim, Order.Expand("weu", 2));
            var restored = AynaReplay.Restore(sim.Record);
            Assert.That(restored.StateHash, Is.EqualTo(sim.StateHash));
            Assert.That(restored.View(Side.Player).LastIncome, Is.EqualTo(7)); Assert.That(restored.View(Side.Player).Compute, Is.EqualTo(9));
            Assert.That(restored.View(Side.Player).BalanceSnapshot().DominanceShare, Is.EqualTo(.6));
        }
        [Test] public void MatrixSeedsAreIndependentOfArchitectureAndTacticalRole()
        {
            var o = new BalanceBenchOptions(); var a = AynaBalanceBench.Setup(o, 8, 0, Architecture.Swarm, Architecture.Ghost);
            var b = AynaBalanceBench.Setup(o, 8, 0, Architecture.Fortress, Architecture.Swarm);
            Assert.That(a.Player.Region, Is.EqualTo(b.Player.Region)); Assert.That(a.Ayna.Region, Is.EqualTo(b.Ayna.Region));
            Assert.That(a.Seed, Is.Not.EqualTo(AynaBalanceBench.Setup(o, 9, 0, Architecture.Swarm, Architecture.Ghost).Seed));
        }
    }
}
