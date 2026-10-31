using System;
using System.IO;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Baskın botuyla denge ölçümü (<see cref="RaidBot"/>, <see cref="RaidBotMeasurement"/>). Normal takımda koşmaz: hepsi
    /// [Explicit], elle seçilir (Test Runner'da ya da CLI'da <c>run_tests --filter RaidBotMeasurementTests --include_explicit true</c>).
    /// Raporlar ve ilerleme <c>Temp/</c> altına yazılır. Ölçüm uzun sürer; Unity'nin 180 sn'lik test süresi <c>Timeout</c> ile açılır.
    /// </summary>
    [TestFixture, Explicit("Denge ölçümü: elle çalıştırılır."), Category("Olcum")]
    public sealed class RaidBotMeasurementTests
    {
        const int Hours = 3600 * 1000;

        static void Ready(Simulation sim) { while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step(); }

        /// <summary>Ölçümün senaryosu kampanyanın baskın emriyle (ikmal ve sunucu sektörü) aynı tahtayı kurar; bot aynı tahtada aynı
        /// oyunu oynar.</summary>
        [Test, Explicit]
        public void Scenario_MatchesCampaignRaidOrder_AndBotIsDeterministic()
        {
            var balance = new Balance { startFon = 100 };
            var sim = Simulation.NewGame(4242, balance);
            foreach (var (sector, pick) in new[] { ("weu_1", "maden,duman"), ("weu_2", "emp,ana"), ("weu_3", "ana,maden") })
            {
                Ready(sim);
                Assert.IsTrue(sim.StartLiberationRaid(sector, pick).Ok, sector);
                var p = sim.S.Raid.Params;
                var sc = new RaidBotMeasurement.Scenario { Seed = p.Seed, MapSeed = p.MapSeed, MapSlot = p.MapSlot, Tier = p.Tier, Mission = p.MissionKind, Region = p.Region };
                var mine = RaidBotMeasurement.Params(balance, sc, Simulation.ParsePick(pick).ToArray(), balance.liberationDynamiteCharges, balance.liberationSmokeCharges);
                // Plakalar sığınaktan gelir; ölçümde kadro plakasız (imza zırhı içermez)
                Assert.AreEqual(Baskin.Signature(Baskin.Create(p)), Baskin.Signature(Baskin.Create(mine)), sector);
                CollectionAssert.AreEqual(p.Traits, mine.Traits, sector);
                CollectionAssert.AreEqual(p.Squad.Select(x => x.Type + x.Hp), mine.Squad.Select(x => x.Type + x.Hp), sector);
                Assert.AreEqual((p.TargetHpBonus, p.ExtraMachines, p.ExtraTurns, p.CivilianHp, p.Tactics, p.Signature),
                    (mine.TargetHpBonus, mine.ExtraMachines, mine.ExtraTurns, mine.CivilianHp, mine.Tactics, mine.Signature), sector);

                var a = Baskin.Create(mine);
                var b = Baskin.Create(mine);
                new RaidBot().Play(a);
                new RaidBot().Play(b);
                Assert.AreNotEqual("player", a.Phase, sector);
                Assert.AreEqual(Baskin.Signature(a), Baskin.Signature(b), sector);
                Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok, sector);
            }
        }

        /// <summary>Kısa ölçüm (bir harekât tohumu, 240 senaryo × 6 kadro = 1440 baskın, tarama yok): düzeneği denemek ve Unity dışında
        /// koşan aynı kodla sonucun aynı olduğunu görmek için. Editörün Mono'su .NET'ten çok yavaştır (Debug derlemede baskın başına
        /// ~1–2 sn): tam ölçüm Unity'de saatler sürer.</summary>
        [Test, Explicit, Timeout(Hours)]
        public void Measure_Quick()
        {
            var cfg = Config(1, 0, "Temp/raid_bot_quick.md", "Temp/raid_bot_quick.csv");
            cfg.Sweeps = false;
            var summary = RaidBotMeasurement.Run(cfg);
            TestContext.WriteLine(summary);
        }

        /// <summary>Tam ölçüm: 80 tohum (hücre başına 400 baskın, 115 200 baskın), ilk 20 tohumda dinamit 1/2/3 ve sis 2/3/4 taraması
        /// (57 600 baskın). .NET'te (Core + bu klasördeki üç dosya bir konsol projesine bağlanarak) 12 çekirdekte ~15 dk; Unity'de
        /// saatler.</summary>
        [Test, Explicit, Timeout(24 * Hours)]
        public void Measure_Full()
        {
            var summary = RaidBotMeasurement.Run(Config(80, 20, "Temp/raid_bot_report.md", "Temp/raid_bot_cells.csv"));
            TestContext.WriteLine(summary);
        }

        static RaidBotMeasurement.Config Config(int seeds, int sweepSeeds, string report, string csv)
        {
            const string progress = "Temp/raid_bot_progress.txt";
            File.WriteAllText(progress, "");
            return new RaidBotMeasurement.Config
            {
                MapSeeds = seeds, SweepMapSeeds = sweepSeeds, Report = report, Csv = csv,
                Progress = line => File.AppendAllText(progress, DateTime.Now.ToString("HH:mm:ss") + " " + line + "\n"),
            };
        }
    }
}
