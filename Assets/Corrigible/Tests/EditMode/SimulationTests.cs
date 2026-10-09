using System.Globalization;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    public class SimulationTests
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [Test]
        public void Rng_MatchesBrowserPrototype()
        {
            // Değerler tarayıcı prototipindeki mulberry32'den (sim.js) alındı
            var r = new Rng(42);
            Assert.AreEqual(0.601103751920164, r.Next(), 1e-15);
            Assert.AreEqual(0.448290558997542, r.Next(), 1e-15);
            Assert.AreEqual(0.852465793490410, r.Next(), 1e-15);
            Assert.AreEqual(0.669734041439369, r.Next(), 1e-15);
        }

        [Test]
        public void SameSeedAndCommands_GiveSameGame()
        {
            string a = PlayScripted("manipulator", 1234), b = PlayScripted("manipulator", 1234);
            Assert.AreEqual(a, b);
        }

        [TestCase("manipulator", 7u, "acik@185 fon=350.837433")]
        [TestCase("kahin", 99u, "sessiz@230 fon=698.699413")]
        [TestCase("solucan", 12345u, "acik@192 fon=414.256816")]
        public void FullGame_MatchesBrowserPrototype(string type, uint seed, string expected)
        {
            // Aynı tohum ve aynı komutlarla prototipteki sim.js'in verdiği sonuç
            Assert.AreEqual(expected, PlayScripted(type, seed));
        }

        [Test]
        public void PassivePlayer_AlwaysLoses()
        {
            foreach (var type in GameData.AiTypes.Select(t => t.Id))
                for (uint seed = 1; seed <= 20; seed++)
                {
                    var sim = Simulation.NewGame(null, type, seed);
                    while (sim.S.Ending == null && sim.S.Day < 2000)
                    {
                        if (sim.S.PendingGift != null) sim.ResolveGift(Simulation.GiftAccept);
                        sim.Step(); sim.S.Events.Clear();
                    }
                    Assert.AreEqual(EndingKind.Loss, GameData.Ending(sim.S.Ending).Kind, $"{type} tohum {seed}: {sim.S.Ending}@{sim.S.Day}");
                }
        }

        /// <summary>
        /// İlk oyun testinde bulunan açık: "her yeri izle, görünen her sızmayı denetle" oyunu 41–60. günde Silme ile bitiriyordu.
        /// YZ çekirdeğiyle Silme en erken ~130. günde mümkün olmalı.
        /// </summary>
        [Test]
        public void MonitorEverythingAuditEverything_CannotEradicateEarly()
        {
            string[] cheap = { "af", "sa", "eeu", "sea", "oc", "ru", "me", "in", "weu", "jk", "na", "cn" };
            foreach (var type in GameData.AiTypes.Select(t => t.Id))
                for (uint seed = 1; seed <= 40; seed++)
                {
                    var sim = Simulation.NewGame(null, type, seed);
                    var s = sim.S;
                    while (s.Ending == null && s.Day < 120)
                    {
                        if (s.PendingGift != null) sim.ResolveGift(Simulation.GiftReject);
                        foreach (var id in cheap)
                            if (!s.Region(id).Monitored && s.Fon >= Simulation.MonitorCost(id)) sim.Act(Actions.Monitor, id);
                        foreach (var r in s.Regions.Where(r => (sim.Shown(r).V ?? 0) > 0.5).ToList())
                            if (s.Fon >= sim.AuditCost && s.Sg >= 1) sim.Act(Actions.Audit, r.Id);
                        if (s.CoreKnown != null && s.Sg >= sim.SalterSg(s.Region(s.CoreKnown))) sim.Act(Actions.KillSwitch, s.CoreKnown);
                        sim.Step(); s.Events.Clear();
                    }
                    Assert.AreNotEqual("silme", s.Ending, $"{type} tohum {seed}: Silme {s.Day}. günde");
                }
        }

        [Test]
        public void Alignment_CannotStartBeforeDetection()
        {
            var sim = Simulation.NewGame(null, "manipulator", 5);
            var res = sim.StartResearch("yorum");
            Assert.IsFalse(res.Ok);
            StringAssert.Contains("Gözlemlenmemiş", res.Msg);
        }

        [Test]
        public void Core_EscapesOnFirstDetection()
        {
            var sim = Simulation.NewGame(null, "manipulator", 11);
            var s = sim.S;
            string origin = s.Origin;
            s.Fon = 1000;
            foreach (var r in s.Regions) sim.Act(Actions.Monitor, r.Id);
            while (!s.Aware && s.Day < 200) { sim.Step(); s.Events.Clear(); }
            Assert.IsTrue(s.Aware);
            Assert.AreNotEqual(origin, s.Core, "YZ fark edildiği an çekirdeğini taşımalı");
            Assert.GreaterOrEqual(s.Region(s.Core).Inf, sim.B.coreSeed);
        }

        /// <summary>Prototipteki sim.js ile karşılaştırma için kullanılan sabit oyun senaryosu.</summary>
        static string PlayScripted(string type, uint seed)
        {
            var sim = Simulation.NewGame(null, type, seed);
            var s = sim.S;
            sim.Act(Actions.Monitor, "na"); sim.Act(Actions.Monitor, "cn");
            while (s.Ending == null && s.Day < 2000)
            {
                if (s.PendingGift != null) sim.ResolveGift(Simulation.GiftAccept);
                if (s.Aware && s.Research.Cur == null) sim.StartResearch("yorum");
                sim.Step(); s.Events.Clear();
            }
            return $"{s.Ending}@{s.Day} fon={s.Fon.ToString("F6", Inv)}";
        }
    }
}
