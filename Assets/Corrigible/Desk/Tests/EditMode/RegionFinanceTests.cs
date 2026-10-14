using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.UI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Bölge fon kartı: gelir payı, eski kurallar, kesinti nedenleri ve gizli veriden bağımsızlık.</summary>
    public sealed class RegionFinanceTests
    {
        static Simulation Scenario(int version = 3, double scale = 1.5, bool aware = true)
        {
            var sim = Simulation.NewGame(null, "manipulator", 75, new Balance(),
                new ExpOptions { Teams = false, TimeScale = scale }, null, version);
            sim.S.Aware = aware;
            sim.S.BudgetLevel = 2;
            foreach (var r in sim.S.Regions) { r.Econ = 0; r.Trust = 0; r.Off = 0; r.Left = false; }
            var first = sim.S.Regions[0]; first.Econ = 80; first.Trust = 50;
            var second = sim.S.Regions[1]; second.Econ = 20; second.Trust = 100;
            return sim;
        }

        static DossierVM Dossier(Simulation sim, string id) => new SimDeskBuilder().Build(new DeskBuildInput
        {
            Sim = sim,
            State = new DeskBridgeState { Selected = id },
            MapSpec = JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Mock/map.json").text),
        }).Dossier;

        [TestCase(1, 1, false)]
        [TestCase(1, 2, true)]
        [TestCase(2, 1.5, true)]
        [TestCase(3, 1, false)]
        [TestCase(3, 1.5, true)]
        [TestCase(3, 2, true)]
        public void Contribution_UsesAwarenessBudgetAndTimeScaleWithoutCentral(int version, double scale, bool aware)
        {
            var sim = Scenario(version, scale, aware);
            var first = RegionFinance.Read(sim, sim.S.Regions[0]);
            double expected = 40 / sim.B.fonDiv * (aware ? 1 : sim.B.preAwareFund) / scale;
            if (version >= 3) expected *= sim.B.regionalFundMul * sim.B.budgetTier2Multiplier;
            Assert.AreEqual(expected, first.PerDay, 1e-12);
            Assert.AreEqual(200d / 3, first.SharePercent, 1e-12);
            double sum = sim.S.Regions.Sum(r => RegionFinance.Read(sim, r).PerDay);
            var income = sim.IncomeBreakdown;
            Assert.AreEqual(income.RegionalBase + income.BudgetBonus, sum, 1e-12);
            Assert.AreEqual(income.Total - income.Central, sum, 1e-12, "Merkez ödeneği bölgelere yazılmamalı");
        }

        [Test]
        public void Contribution_UsesCurrentCampaignBalanceAndBudgetChangesImmediately()
        {
            var sim = Scenario(); sim.S.BudgetLevel = 0;
            sim.B.fonDiv /= 1.15;
            var r = sim.S.Regions[0];
            double before = RegionFinance.Read(sim, r).PerDay;
            sim.S.Fon = 100;
            Assert.IsTrue(sim.BuyBudgetUpgrade().Ok);
            Assert.AreEqual(before * sim.B.budgetTier1Multiplier, RegionFinance.Read(sim, r).PerDay, 1e-12);
            r.Trust = 25;
            Assert.AreEqual(before * sim.B.budgetTier1Multiplier / 2, RegionFinance.Read(sim, r).PerDay, 1e-12);
        }

        [Test]
        public void OfflineOrLeft_StopsContributionAndReweightsOtherMembers()
        {
            var sim = Scenario(); var first = sim.S.Regions[0]; var second = sim.S.Regions[1];
            first.Off = 5;
            var off = RegionFinance.Read(sim, first);
            Assert.AreEqual(0, off.PerDay); Assert.AreEqual(0, off.SharePercent);
            Assert.IsNotEmpty(off.Note);
            Assert.AreEqual(100, RegionFinance.Read(sim, second).SharePercent, 1e-12);
            first.Off = 0; first.Left = true;
            var left = RegionFinance.Read(sim, first);
            Assert.AreEqual(0, left.PerDay); Assert.AreNotEqual(off.Note, left.Note);
            second.Left = true;
            Assert.IsTrue(sim.S.Regions.All(r => RegionFinance.Read(sim, r).PerDay == 0));
            Assert.AreEqual(sim.IncomeBreakdown.Central, sim.FundRate, 1e-12);
        }

        [Test]
        public void Quarantine_KeepsCurrentContributionAndShowsStatusWithoutReadingInfiltration()
        {
            var sim = Scenario(); var r = sim.S.Regions[0];
            var normal = RegionFinance.Read(sim, r);
            r.Q = true; r.Inf = 95; r.FakeLow = 3;
            var quarantined = RegionFinance.Read(sim, r);
            Assert.AreEqual(normal.PerDay, quarantined.PerDay, 1e-12);
            Assert.AreNotEqual(normal.Note, quarantined.Note);
            r.Inf = 0; r.FakeLow = 99;
            var hiddenChanged = RegionFinance.Read(sim, r);
            Assert.AreEqual(quarantined.Summary, hiddenChanged.Summary);
            Assert.AreEqual(quarantined.Note, hiddenChanged.Note);
        }

        [Test]
        public void ZeroEconomyOrTrust_ShowsDifferentReasonsAndFiniteZeroShare()
        {
            var sim = Scenario(); var r = sim.S.Regions[0];
            r.Trust = 0;
            var noTrust = RegionFinance.Read(sim, r);
            Assert.AreEqual(0, noTrust.PerDay);
            r.Trust = 50; r.Econ = 0;
            var noEconomy = RegionFinance.Read(sim, r);
            Assert.AreEqual(0, noEconomy.PerDay); Assert.AreNotEqual(noTrust.Note, noEconomy.Note);
            sim.S.Regions[1].Econ = 0;
            Assert.AreEqual(0, RegionFinance.Read(sim, r).SharePercent);
        }

        [Test]
        public void Dossier_UpdatesFinanceFromCurrentPublicEconomy()
        {
            var sim = Scenario(); var r = sim.S.Regions[0];
            var first = Dossier(sim, r.Id).Finance;
            Assert.IsNotNull(first); Assert.IsNotEmpty(first.Title);
            Assert.IsNotEmpty(first.Contribution); Assert.IsNotEmpty(first.Note);
            r.Econ /= 2;
            var updated = Dossier(sim, r.Id).Finance;
            Assert.AreEqual(first.PerDay / 2, updated.PerDay, 1e-12);
            Assert.Less(updated.SharePercent, first.SharePercent);
        }
    }
}
