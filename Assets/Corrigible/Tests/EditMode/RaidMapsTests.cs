using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis odalarının harita üreticisi (<see cref="RaidMaps"/>): tohumlu, kurallara uyan, türüne uyan haritalar ve üreticinin bit
    /// kilidi. Koddan kurulan örnek tahta (<see cref="Baskin.Create"/>) havuzsuzdur: elle çizilmiş haritalarda kalır.
    /// </summary>
    public sealed class RaidMapsTests
    {
        [Test]
        public void Generate_FitsItsKind_AndFollowsTheRules([Range(1, 20)] int seed)
        {
            int made = 0;
            foreach (var kind in RaidMaps.Kinds)
            {
                var rows = RaidMaps.Generate(kind, new Rng((uint)seed));
                if (rows == null) continue;
                made++;
                Assert.AreEqual(Baskin.N, rows.Length, kind);
                Assert.IsTrue(RaidMaps.Valid(rows, kind), kind + ": " + string.Join("/", rows));
                Assert.IsFalse(Baskin.Layouts.Any(l => l.Rows.SequenceEqual(rows)), "üretilmiş harita: " + kind);
            }
            Assert.Greater(made, 0, "tohum hiçbir tür üretmedi");
        }

        /// <summary>
        /// Üreticinin doğrudan bit kilidi (tek tahta havuzunun kilidinin yerine): 6 tür × 4 tohum <see cref="RaidMaps.Generate(string, Rng)"/>
        /// dökümünün FNV-1a özeti. Tesis odaları bu çıktının giydirilmiş hâli; değişirse <c>FacilityMaps.GeneratorVersion</c> artar ve bu
        /// değer bilinçli güncellenir.
        /// </summary>
        [Test]
        public void Generate_Golden()
        {
            var dump = new System.Text.StringBuilder();
            foreach (var kind in RaidMaps.Kinds)
                foreach (uint seed in new uint[] { 1, 7, 79, 4242 })
                {
                    var rows = RaidMaps.Generate(kind, new Rng(seed));
                    dump.Append(kind).Append(':').Append(seed).Append('=').Append(rows == null ? "-" : string.Join("/", rows)).Append('\n');
                }
            uint h = 2166136261;
            foreach (char c in dump.ToString()) { h ^= c; h *= 16777619; }
            Assert.AreEqual(GoldenHash, h, dump.ToString());
        }

        /// <summary>Kayıt 21 öncesi Core ile yakalandı (aynı değer; tek tahtanın kaldırılması üreticiyi değiştirmedi).</summary>
        const uint GoldenHash = 0x862CC4DEu;

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

        /// <summary>Örnek tahta havuzsuzdur: MapSeed (harekât emrinde kitin işareti) tahtayı değiştirmez; elle çizilmiş haritalarda kalır.</summary>
        [Test]
        public void Create_IgnoresMapSeed()
        {
            var hand = Baskin.Layouts.Select(l => l.Name).ToList();
            for (int seed = 1; seed <= 20; seed++)
            {
                var plain = Baskin.Create(new RaidParams { Seed = seed });
                var marked = Baskin.Create(new RaidParams { Seed = seed, MapSeed = 7919 });
                Assert.Contains(marked.Layout, hand);
                Assert.AreEqual(Baskin.Signature(plain), Baskin.Signature(marked), "tohum " + seed);
            }
        }

        [Test]
        public void CampaignRaid_CarriesTheCampaignSeedAsMapSeed()
        {
            var sim = LegacySimulation.NewGame(4242, new Balance { shelterEventsEnabled = false });
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.AreEqual(sim.S.Seed, sim.S.Raid.Params.MapSeed);
            Assert.AreNotEqual(0u, sim.S.Raid.Params.MapSeed, "sunum tesis odasının bina kitini MapSeed ile seçer");
        }
    }
}
