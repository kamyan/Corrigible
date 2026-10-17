using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>Çekirdeğin fiziksel imhasından sonra kalan kopyalar ve kesintisiz temizlik süresi.</summary>
    public class ErasureTests
    {
        static Simulation Ready(double scale = 1.5)
        {
            var sim = Simulation.NewGame(null, "solucan", 271u,
                new Balance { growth = 0, spread = 0, coreTek = 0, cpRate = 0, depRate = 0 },
                new ExpOptions { Teams = true, TimeScale = scale });
            var s = sim.S;
            s.Aware = true; s.Core = s.CoreKnown = Simulation.HQ;
            s.Fon = 1000; s.Sg = 200;
            s.NextNews = s.NextGift = int.MaxValue;
            foreach (var r in s.Regions) { r.Inf = 0; r.Monitored = r.Detected = true; }
            return sim;
        }

        static void RaidCore(Simulation sim, double infiltration = 60, bool backupDestroyed = true)
        {
            sim.S.Region(Simulation.HQ).Inf = infiltration;
            Assert.IsTrue(sim.StartRaid(Simulation.HQ).Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true, BackupDestroyed = backupDestroyed }).Ok);
        }

        static void Days(Simulation sim, int days)
        {
            for (int i = 0; i < days; i++) sim.Step();
        }

        [TestCase(1d, 15)]
        [TestCase(1.5d, 23)]
        [TestCase(2d, 30)]
        public void RaidThenAudit_WinsAfterExactlyTheRequiredCleanDays(double scale, int required)
        {
            var sim = Ready(scale);
            RaidCore(sim);
            Assert.IsNull(sim.S.Core);
            Assert.AreEqual(20, sim.S.Region(Simulation.HQ).Inf, "Çekirdeği yok etmek bütün kopyaları temizlemez.");
            Days(sim, required);
            Assert.IsNull(sim.S.Ending);
            Assert.AreEqual(0, sim.S.CleanDays);

            Assert.IsTrue(sim.Act(Actions.Audit, Simulation.HQ).Ok);
            Assert.AreEqual(0, sim.S.Region(Simulation.HQ).Inf);
            Days(sim, required - 1);
            Assert.IsNull(sim.S.Ending);
            Assert.AreEqual(required - 1, sim.S.CleanDays);
            sim.Step();
            Assert.AreEqual("silme", sim.S.Ending);
            Assert.AreEqual(required, sim.S.CleanDays);
        }

        [Test]
        public void ResidualCopy_ResetsTheConsecutiveCleanDays()
        {
            var sim = Ready();
            RaidCore(sim, 40);
            Days(sim, 8);
            Assert.AreEqual(8, sim.S.CleanDays);
            sim.S.Region("cn").Inf = 0.6;
            sim.Step();
            Assert.AreEqual(0, sim.S.CleanDays);
            Assert.IsNull(sim.S.Ending);
            sim.S.Region("cn").Inf = 0;
            Days(sim, sim.Dur(15) - 1);
            Assert.IsNull(sim.S.Ending);
            sim.Step();
            Assert.AreEqual("silme", sim.S.Ending);
        }

        [Test]
        public void SurvivingBackup_LetsTheCoreEscapeAndPreventsErasure()
        {
            var sim = Ready();
            sim.S.Traits.Add("yedek");
            RaidCore(sim, 40, false);
            Assert.IsNotNull(sim.S.Core);
            Assert.AreEqual(1, sim.S.CoreEscapes);
            Days(sim, sim.Dur(15));
            Assert.AreEqual(0, sim.S.CleanDays);
            Assert.IsNull(sim.S.Ending);
        }

        [Test]
        public void OfflineBackup_PreventsTheCountdownAndReturnsAsACopy()
        {
            var sim = Ready();
            sim.S.Traits.Add("yedek");
            RaidCore(sim, 40);
            sim.S.Region("cn").Off = 2;
            sim.Step();
            Assert.AreEqual(0, sim.S.CleanDays);
            sim.Step();
            Assert.AreEqual(15, sim.S.Region("cn").Inf);
            Assert.AreEqual(0, sim.S.CleanDays);
            Assert.IsNull(sim.S.Ending);
        }
    }
}
