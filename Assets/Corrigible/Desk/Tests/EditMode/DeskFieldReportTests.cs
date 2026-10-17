using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Harita fişi yalnız açık raporu gösterir; görünüm kurmak fon veya gizli bilgi üretmez.</summary>
    public class DeskFieldReportTests
    {
        static Simulation Ready()
        {
            var sim = Simulation.NewGame(null, "manipulator", 731u,
                exp: new ExpOptions { Teams = true, TimeScale = 1.5 },
                rules: new RuleOptions { FieldReports = true });
            sim.S.Day = 20; sim.S.Aware = true;
            sim.S.Region("na").Monitored = true;
            sim.S.FieldReports.Add(new FieldReport
            {
                Id = "field-1", Region = "na", Day = 18, ExpiresDay = 60, Reward = 1,
                Headline = "Açık saha raporu.",
            });
            return sim;
        }

        static DeskViewModel Build(Simulation sim) => new SimDeskBuilder().Build(new DeskBuildInput
        {
            Sim = sim, Speed = 0, SecondsPerDay = 1, State = new DeskBridgeState(),
            MapSpec = JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Mock/map.json").text),
        });

        [TearDown] public void RestoreLanguage() => Loc.SetLanguage(Loc.Turkish);

        [Test]
        public void PendingReport_HasACollectCommandAndDoesNotPayDuringPresentation()
        {
            var sim = Ready(); double fon = sim.S.Fon; uint rng = sim.S.Rng.State;
            var report = Build(sim).Map.Reports.Single();
            Assert.AreEqual("Report:Collect:field-1", report.Command);
            Assert.AreEqual("+1", report.RewardText);
            StringAssert.Contains("Açık saha raporu.", report.Tooltip);
            Assert.AreEqual(fon, sim.S.Fon); Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.IsEmpty(sim.S.News, "Fiş dosyalanmadan haber bandına düşmemeli.");
        }

        [Test]
        public void InaccessibleExpiredAndEndedReports_AreNotClickableOnTheMap()
        {
            var sim = Ready(); sim.S.Region("na").Left = true;
            Assert.IsEmpty(Build(sim).Map.Reports);
            sim.S.Region("na").Left = false; sim.S.FieldReports[0].ExpiresDay = sim.S.Day;
            Assert.IsEmpty(Build(sim).Map.Reports);
            sim.S.FieldReports[0].ExpiresDay = 60; sim.S.Ending = "silme";
            Assert.IsEmpty(Build(sim).Map.Reports);
        }

        [Test]
        public void Illusion_DoesNotHideOrRewriteAnExistingPaper()
        {
            var sim = Ready();
            var before = Build(sim).Map.Reports.Single();
            sim.S.Illusion = new IllusionState { Start = 0, Tek = 60, Trust = 70 };
            sim.S.Region("na").Inf = 99; sim.S.Region("na").FakeLow = 1;
            var after = Build(sim).Map.Reports.Single();
            Assert.AreEqual(before.Tooltip, after.Tooltip);
            Assert.AreEqual(before.RewardText, after.RewardText);
            Assert.AreEqual(before.X, after.X); Assert.AreEqual(before.Y, after.Y);
        }

        [Test]
        public void LegacyJson_DefaultsAllowReportsWithoutRewritingExistingBalance()
        {
            var balance = JsonUtility.FromJson<Balance>("{\"startFon\":73,\"growth\":0.123}");
            Assert.AreEqual(73, balance.startFon);
            Assert.AreEqual(0.123, balance.growth);
            Assert.AreEqual(1, balance.reportReward);
            Assert.AreEqual(0.08, balance.reportIncomeShare);
            var rules = JsonUtility.FromJson<RuleOptions>("{\"Stage\":3,\"Events\":true}");
            Assert.IsFalse(rules.FieldReports, "Eski oyunun geçmişine üretim eklenmemeli.");
        }
    }
}
