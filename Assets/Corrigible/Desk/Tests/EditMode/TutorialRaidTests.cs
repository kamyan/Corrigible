using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using UnityEditor;

namespace Corrigible.Desk.Tests
{
    /// <summary>Senaryolu ilk baskın: harekâtın ilk baskını hangi tohumla başlarsa başlasın aynı tahta kurulur (temel makine aklı,
    /// elle çizilmiş harita) ve rehberin adımları kasayı alıp girişe dönerek hasarsız zafere varır.</summary>
    public class TutorialRaidTests
    {
        static RaidParams FirstRaid(uint seed)
        {
            // Oyunun denge değerleriyle (GameController da bunları kullanır)
            var config = AssetDatabase.LoadAssetAtPath<BalanceConfig>("Assets/Corrigible/Settings/BalanceConfig.asset");
            Assert.IsNotNull(config);
            var sim = Simulation.NewGame(seed, config.CreateRuntimeCopy());
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            return sim.S.Raid.Params;
        }

        [Test]
        public void Guide_WinsTheFirstRaid_WithoutDamage_ForAnyCampaignSeed()
        {
            foreach (uint seed in new uint[] { 1, 7, 42, 101, 2024 })
            {
                var scripted = TutorialRaid.For(FirstRaid(seed));
                Assert.IsNotNull(scripted, "ilk baskın senaryolu (tohum {0})", seed);
                var b = Baskin.Create(scripted);
                var guide = new RaidGuide();
                char selected = '\0';
                while (!guide.Done)
                {
                    var s = guide.Current;
                    Assert.AreEqual("player", b.Phase, "adım {0}", guide.Index);
                    Assert.IsTrue(guide.Possible(b, selected), "adım {0} yapılabilir", guide.Index);
                    Assert.IsTrue(s.Kind == RaidGuideStep.End || guide.Tile(b).HasValue, "adım {0} karesi çerçevelenir", guide.Index);
                    var u = b.Units.FirstOrDefault(v => v.Side == 'p' && !v.Dead && Baskin.UnitTypes[v.Type].Short[0] == s.Letter);
                    switch (s.Kind)
                    {
                        case RaidGuideStep.Select: selected = s.Letter; break;
                        case RaidGuideStep.Move: Assert.IsTrue(Baskin.DoMove(b, u.Id, s.X, s.Y), "adım {0}", guide.Index); break;
                        case RaidGuideStep.Arm: Assert.IsTrue(Baskin.CanAct(b, u)); break;
                        case RaidGuideStep.Use: Assert.IsTrue(Baskin.DoAct(b, u.Id, s.X, s.Y), "adım {0}", guide.Index); break;
                        default: Assert.IsTrue(Baskin.EndTurn(b)); selected = '\0'; break;
                    }
                    if (s.Kind != RaidGuideStep.Select && s.Kind != RaidGuideStep.End) Assert.AreEqual(s.Letter, selected, "adım {0} seçili uzmanla", guide.Index);
                    guide.Advance();
                }
                Assert.AreEqual("won", b.Phase, "tohum {0}", seed);
                Assert.AreNotEqual(0, b.Carrier, "kasa alındı ve malzeme girişe taşındı");
                Assert.AreEqual(0, b.Collateral);
                Assert.IsTrue(b.Units.Where(v => v.Side == 'p').All(v => !v.Dead && v.Hp == v.Max), "uzmanlar yara almaz");
            }
        }

        [Test]
        public void Steps_SelectBeforeActing_AndCarryTheLessons()
        {
            var steps = TutorialRaid.Steps();
            Assert.AreEqual(30, steps.Count);
            Assert.AreEqual(RaidGuideStep.Select, steps[0].Kind);
            Assert.AreEqual(4, steps.Count(s => s.Kind == RaidGuideStep.End), "beş tur: üçüncüde kasa alınır, beşincide çıkışa varılır");
            Assert.IsTrue(steps.Where(s => s.Kind == RaidGuideStep.Use).All(s => steps[steps.IndexOf(s) - 1].Kind == RaidGuideStep.Arm), "yetenekten önce Q");
            Assert.IsNotNull(steps[0].Lesson);
            Assert.IsNotNull(steps.Last().Lesson);
            Assert.IsTrue(steps.All(s => !string.IsNullOrEmpty(TutorialRaid.Instruction(s))));
        }

        [Test]
        public void OnlyTheFreshFirstRaid_IsScripted()
        {
            var first = FirstRaid(5);
            Assert.IsNotNull(TutorialRaid.For(first));
            Assert.AreEqual(TutorialRaid.Seed, TutorialRaid.For(first).Seed);
            Assert.AreEqual(0, TutorialRaid.For(first).Tactics, "eğitimde temel makine aklı");
            Assert.AreEqual(0u, TutorialRaid.For(first).MapSeed, "eğitimde elle çizilmiş harita");
            Assert.AreNotSame(first, TutorialRaid.For(first), "harekâtın parametreleri değişmez");

            var wounded = FirstRaid(5);
            wounded.Squad[0].Hp--;
            Assert.IsNull(TutorialRaid.For(wounded), "yaralı kadro");
            var plated = FirstRaid(5);
            plated.Squad[1].Armor = 1;
            Assert.IsNull(TutorialRaid.For(plated), "zırhlı kadro");
            var other = FirstRaid(5);
            other.SectorId = "weu_2";
            Assert.IsNull(TutorialRaid.For(other), "başka sektör");
            Assert.IsNull(TutorialRaid.For(null));
        }
    }
}
