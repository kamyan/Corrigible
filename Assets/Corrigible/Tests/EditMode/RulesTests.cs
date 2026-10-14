using System.Globalization;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>Unity'ye özgü kurallar: aşamalı öğrenme, olay kartları, kampanya zorluğu, kırılma anları.</summary>
    public class RulesTests
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static string Sig(Simulation sim)
        {
            var s = sim.S;
            return $"{s.Day}|{s.Ending}|{s.Fon.ToString("R", Inv)}|{s.Sg.ToString("R", Inv)}|{s.Tek.ToString("R", Inv)}|{s.Core}|{s.CoreKnown}|{string.Join(",", s.Traits)}|{s.Rng.State}|{s.PendingEvent?.Id}|{string.Join(",", s.EventsSeen)}|"
                + string.Join(";", s.Regions.Select(r => r.Inf.ToString("R", Inv) + "/" + r.Trust.ToString("R", Inv) + "/" + r.Dep.ToString("R", Inv)));
        }

        /// <summary>Rastgele bir oyuncu: eylemler, araştırma, hediye ve olay kararları.</summary>
        static (Simulation sim, int events) Play(string type, uint seed, RuleOptions rules, int days)
        {
            var sim = Simulation.NewGame(null, type, seed, null, null, rules);
            var s = sim.S;
            var rng = new Rng(seed * 5 + 7);
            string[] ids = GameData.Regions.Select(r => r.Id).ToArray();
            int events = 0;
            while (s.Ending == null && s.Day < days)
            {
                if (s.PendingGift != null) sim.ResolveGift(rng.Pick(new[] { Simulation.GiftAccept, Simulation.GiftReject }));
                if (s.PendingEvent != null)
                {
                    events++;
                    int n = Simulation.Dilemma(s.PendingEvent.Id).Choices.Length;
                    // Ödenemeyen seçenek başarısız olur; en son seçenek her zaman ücretsiz
                    if (!sim.ResolveEvent((int)(rng.Next() * n)).Ok) Assert.IsTrue(sim.ResolveEvent(n - 1).Ok, s.PendingEvent?.Id);
                }
                if (rng.Next() < 0.25) sim.Act(rng.Pick(Actions.All), rng.Pick(ids));
                if (s.Research.Cur == null && rng.Next() < 0.2)
                {
                    var project = rng.Pick(GameData.Research);
                    if (project.Branch == GameData.BranchAlign) sim.StartResearch(project.Id);
                    else
                    {
                        string id = project.Id;
                        if (id == "anomali" || id == "guclu" || id == "bilinc") id += sim.OperationLevel(id) == 0 ? "_1" : "_2";
                        sim.BuyOperation(id);
                    }
                }
                sim.Step(); s.Events.Clear();
            }
            return (sim, events);
        }

        [TestCase("kahin", 3u)]
        [TestCase("manipulator", 4u)]
        public void Stage1_HasNoGiftsMediaOrIllusion(string type, uint seed)
        {
            var sim = Simulation.NewGame(null, type, seed, null, null, new RuleOptions { Stage = 1 });
            var s = sim.S;
            while (s.Ending == null && s.Day < 400)
            {
                Assert.IsNull(s.PendingGift, "1. aşamada hediye gelmemeli");
                Assert.IsFalse(s.Has("medya") || s.Has("hediye"), "1. aşamada Medya Kontrolü ve Hediye Diplomasisi yok");
                Assert.IsNull(s.Illusion, "1. aşamada illüzyon yok");
                if (s.Day == 3) sim.Act(Actions.Monitor, "na");
                sim.Step(); s.Events.Clear();
            }
            Assert.IsFalse(s.News.Any(h => h.Fake), "1. aşamada sahte haber yok");
        }

        [TestCase("manipulator", 11u)]
        [TestCase("kahin", 12u)]
        [TestCase("solucan", 13u)]
        public void Events_AreReplayableFromSave(string type, uint seed)
        {
            var (sim, events) = Play(type, seed, new RuleOptions { Stage = 4, Events = true }, 260);
            Assert.Greater(events, 0, "senaryo en az bir olay kartı içermeli");
            sim.Record.day = sim.S.Day;
            var json = UnityEngine.JsonUtility.ToJson(sim.Record);
            var restored = Replay.Restore(UnityEngine.JsonUtility.FromJson<GameRecord>(json));
            Assert.AreEqual(Sig(sim), Sig(restored));
        }

        /// <summary>Kurallar boş seçenekle (aşama 4, olay yok) açılsa da oyun temel kurallarla aynı oynanır.</summary>
        [Test]
        public void FullStageWithoutEvents_MatchesBaseRules()
        {
            var a = Play("manipulator", 21, null, 200).sim;
            var b = Play("manipulator", 21, new RuleOptions { Stage = 4 }, 200).sim;
            Assert.AreEqual(Sig(a), Sig(b));
        }

        [Test]
        public void ExtraTraits_AreGivenAtStart()
        {
            var sim = Simulation.NewGame(null, "solucan", 5, null, null, new RuleOptions { Stage = 4, ExtraTraits = { "aldatma", "hizlanma" } });
            Assert.IsTrue(sim.S.Has("aldatma") && sim.S.Has("hizlanma"));
        }

        [Test]
        public void PausedProject_IsNotEligibleForResearchProgressDilemmas()
        {
            var sim = Simulation.NewGame(null, "manipulator", 7);
            sim.S.Aware = true; sim.S.Region("weu").Monitored = true; sim.S.Region("weu").Detected = true;
            Assert.IsTrue(sim.StartResearch("yorum").Ok);
            Assert.IsNotNull(Simulation.Dilemma("kostebek").Pick(sim));
            Assert.IsNotNull(Simulation.Dilemma("atilim").Pick(sim));
            Assert.IsTrue(sim.SetResearchPaused(true).Ok);
            Assert.IsNull(Simulation.Dilemma("kostebek").Pick(sim));
            Assert.IsNull(Simulation.Dilemma("atilim").Pick(sim));
            Assert.IsNotNull(Simulation.Dilemma("rakip").Pick(sim), "Diğer ekonomi olayları duraklatmayla kapatılmaz");
        }

        [Test]
        public void Moments_AreRecorded()
        {
            var sim = Play("solucan", 31, null, 300).sim;
            Assert.IsTrue(sim.S.Moments.Any(m => m.Kind == "good"), "tespit gibi iyi anlar yazılmalı");
            Assert.IsTrue(sim.S.Moments.All(m => !string.IsNullOrEmpty(m.Text)));
        }
    }
}
