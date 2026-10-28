using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Harekâtın harita havuzu (<see cref="RaidMaps"/>): yeni oyunun tohumundan deterministik, kurallara uyan ve harekâttan
    /// harekâta değişen haritalar; havuzsuz baskın (eğitim, örnek baskın) elle çizilmiş haritalarda kalır.
    /// </summary>
    public sealed class RaidMapsTests
    {
        [Test]
        public void Pool_IsSameForSameSeed_AndDiffersBetweenCampaigns()
        {
            var a = RaidMaps.Pool(17).Select(m => string.Join("/", m.Rows)).ToList();
            var b = RaidMaps.Pool(18).Select(m => string.Join("/", m.Rows)).ToList();
            var again = RaidMaps.Pool(17).Select(m => string.Join("/", m.Rows)).ToList();
            CollectionAssert.AreEqual(a, again);
            Assert.AreEqual(0, a.Intersect(b).Count(), "iki harekât aynı haritayı paylaşmaz");
        }

        [Test]
        public void Pool_MapsFollowTheRules([Range(1, 60)] int seed)
        {
            var pool = RaidMaps.Pool((uint)seed);
            Assert.AreEqual(RaidMaps.PoolSize, pool.Count);
            Assert.AreEqual(pool.Count, pool.Select(m => string.Join("/", m.Rows)).Distinct().Count(), "haritalar farklı");
            Assert.AreEqual(pool.Count, pool.Select(m => m.Name).Distinct().Count(), "adlar farklı");
            foreach (var m in pool)
            {
                Assert.IsTrue(RaidMaps.Valid(m.Rows), string.Join("/", m.Rows));
                Assert.IsFalse(Baskin.Layouts.Any(l => l.Rows.SequenceEqual(m.Rows)), "üretilmiş harita");
            }
        }

        [Test]
        public void Pool_HasEveryKindEqually_AndEachMapFitsItsKind([Range(1, 20)] int seed)
        {
            var pool = RaidMaps.Pool((uint)seed);
            var kinds = pool.Select(m => RaidMaps.KindOf(m.Name)).ToList();
            foreach (var kind in RaidMaps.Kinds)
                Assert.AreEqual(RaidMaps.PoolSize / RaidMaps.Kinds.Length, kinds.Count(k => k == kind), kind);
            for (int i = 0; i < pool.Count; i++)
                Assert.IsTrue(RaidMaps.Valid(pool[i].Rows, kinds[i]), kinds[i] + ": " + string.Join("/", pool[i].Rows));
        }

        [Test]
        public void Kinds_HaveTheirShape()
        {
            int Count(string[] rows, char c) => rows.Sum(row => row.Count(ch => ch == c));
            for (uint seed = 1; seed <= 30; seed++)
            {
                // Kasa odası: hedef dolabın en az iki yanı kapalı; iki hat: bir satırda dört bitişik duvar; şebeke: kablo kuşağı;
                // rehine: en az üç sivil yolun ortasında; açık salon: korunak dışında tek karelik sütunlar
                var vault = RaidMaps.Generate(RaidMaps.Vault, new Rng(seed));
                if (vault != null) Assert.GreaterOrEqual(Count(vault, '#'), 8);
                var lanes = RaidMaps.Generate(RaidMaps.Lanes, new Rng(seed));
                if (lanes != null) Assert.IsTrue(lanes.Any(row => row.Contains("####")), string.Join("/", lanes));
                var grid = RaidMaps.Generate(RaidMaps.Grid, new Rng(seed));
                if (grid != null) Assert.GreaterOrEqual(Count(grid, '~'), 5);
                var hostage = RaidMaps.Generate(RaidMaps.Hostage, new Rng(seed));
                if (hostage != null) Assert.GreaterOrEqual(hostage.Skip(1).Take(6).Sum(row => row.Substring(2, 4).Count(ch => ch == 'S')), 3);
                var hall = RaidMaps.Generate(RaidMaps.Hall, new Rng(seed));
                if (hall != null) Assert.LessOrEqual(Count(hall, '#'), 9);
            }
        }

        [Test]
        public void Slot_DealsEveryMapBeforeRepeating_AndNeverTwiceInARow([Values(3u, 77u, 4242u)] uint seed)
        {
            var slots = Enumerable.Range(1, RaidMaps.PoolSize * 4).Select(n => RaidMaps.Slot(seed, n)).ToList();
            for (int bag = 0; bag < 4; bag++)
                CollectionAssert.AreEquivalent(Enumerable.Range(0, RaidMaps.PoolSize), slots.Skip(bag * RaidMaps.PoolSize).Take(RaidMaps.PoolSize), "torba " + bag);
            for (int i = 1; i < slots.Count; i++) Assert.AreNotEqual(slots[i - 1], slots[i], "baskın " + (i + 1));
        }

        [Test]
        public void Create_UsesCampaignPool_OnlyWithMapSeed()
        {
            var pool = RaidMaps.Pool(5).Select(m => m.Name).ToList();
            var hand = Baskin.Layouts.Select(l => l.Name).ToList();
            for (int seed = 1; seed <= 20; seed++)
            {
                Assert.Contains(Baskin.Create(new RaidParams { Seed = seed, MapSeed = 5 }).Layout, pool);
                Assert.Contains(Baskin.Create(new RaidParams { Seed = seed }).Layout, hand);
            }
        }

        [Test]
        public void CampaignRaid_CarriesTheCampaignSeedAsMapSeed()
        {
            var sim = Simulation.NewGame(4242, new Balance());
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.AreEqual(sim.S.Seed, sim.S.Raid.Params.MapSeed);
            Assert.AreNotEqual(0u, sim.S.Raid.Params.MapSeed);
            Assert.AreEqual(1, sim.S.Raid.Params.MapSlot, "harekâtın ilk baskını torbanın ilk haritası");
        }
    }
}
