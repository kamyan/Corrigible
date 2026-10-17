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
    /// <summary>Silme hedefi kalan işi ve ölçekli sayacı gösterir; illüzyonun gerçeğini açmaz.</summary>
    public class DeskErasureTests
    {
        static Simulation Ready()
        {
            var sim = Simulation.NewGame(null, "manipulator", 271u, exp: new ExpOptions { Teams = true, TimeScale = 1.5 });
            sim.S.Aware = true; sim.S.Core = sim.S.CoreKnown = null;
            foreach (var r in sim.S.Regions) { r.Inf = 0; r.Monitored = r.Detected = true; }
            return sim;
        }

        static DeskViewModel Build(Simulation sim) => new SimDeskBuilder().Build(new DeskBuildInput
        {
            Sim = sim, Speed = 0, SecondsPerDay = 0.9f, State = new DeskBridgeState(),
            MapSpec = JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Mock/map.json").text),
        });

        [TearDown] public void RestoreLanguage() => Loc.SetLanguage(Loc.Turkish);

        [Test]
        public void DestroyedCore_PrioritizesRemainingCopiesOverAnActiveResearchProject()
        {
            var sim = Ready();
            sim.S.Region("na").Inf = 7.16; sim.S.Region("weu").Inf = 13.03;
            sim.S.Research.Cur = "yorum";
            var goal = Build(sim).Goal;
            Assert.AreEqual("Silme:", goal.Next.Label);
            Assert.AreEqual("2 bölgeyi temizle", goal.Next.Name);
            Assert.AreEqual("0/23 gün", goal.Next.Right);
            Assert.IsNull(goal.Next.Link);
            StringAssert.Contains("23 gün aralıksız", Explain.NextStep(sim));
            StringAssert.Contains("2 bölge", Explain.Erasure(sim).Text);
        }

        [Test]
        public void CleanCountdown_IsVisibleInTheGoalCard()
        {
            var sim = Ready(); sim.S.CleanDays = 8;
            var goal = Build(sim).Goal;
            Assert.AreEqual("Temizliği koru", goal.Next.Name);
            Assert.AreEqual("8/23 gün", goal.Next.Right);
            Assert.AreEqual(Mathf.Round((0.75f + 0.25f * 8 / 23) * 100), goal.Tracks.Single(t => t.Name == "Silme").Percent);
        }

        [Test]
        public void UnverifiedZeroReport_RequestsAnAuditInsteadOfGuaranteeingCleanliness()
        {
            var sim = Ready(); sim.S.Region("cn").Monitored = false;
            Assert.AreEqual("Denetimle doğrula", Build(sim).Goal.Next.Name);
            sim.S.Region("cn").Inf = 0.6;
            Assert.AreEqual(0, Mathf.RoundToInt((float)sim.Shown(sim.S.Region("cn")).V.Value));
            Assert.AreEqual("1 bölgeyi temizle", Build(sim).Goal.Next.Name, "Yuvarlanmış sıfır, görünen küçük sızmayı yok saymaz.");
        }

        [Test]
        public void OfflineBackup_ExplainsWhyZeroDoesNotStartTheCountdown()
        {
            var sim = Ready(); sim.S.Traits.Add("yedek"); sim.S.Region("cn").Off = 3;
            Assert.AreEqual("Yedekleri kontrol et", Build(sim).Goal.Next.Name);
            StringAssert.Contains("dönüşünde yeniden denetle", Explain.Erasure(sim).Text);
        }

        [Test]
        public void Illusion_DoesNotExposeTheRealCleanupOrCountdown()
        {
            var sim = Ready(); sim.S.Day = 40; sim.S.CleanDays = 8;
            sim.S.Illusion = new IllusionState { Start = 20, Tek = 40, Trust = 70 };
            foreach (var r in sim.S.Regions) { r.Inf = 80; r.FakeLow = 1; }
            var goal = Build(sim).Goal;
            Assert.AreEqual("TEMİZ", goal.Stamp.Text);
            Assert.AreNotEqual("Silme:", goal.Next.Label);
            Assert.AreNotEqual("8/23 gün", goal.Next.Right);
            Assert.AreEqual("Kopyaları temizle", Explain.NextStep(sim));
        }
    }
}
