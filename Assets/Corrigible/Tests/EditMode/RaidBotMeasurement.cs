using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Corrigible.Core;

namespace Corrigible.Tests
{
    /// <summary>
    /// Baskın botuyla (<see cref="RaidBot"/>) denge ölçümü: altı kadro (Teknisyen + iki uzman) × iki görev × altı harita türü ×
    /// dört kademe. Tasarım eşlidir: her senaryo (tahta, bölge, kademe, tohum) her kadroyla oynanır, kadro farkları aynı
    /// tahtalardan okunur. İsteğe bağlı dinamit ve sis hakkı taraması da aynı senaryoların bir kısmında eşli koşar.
    /// Senaryolar kampanyanın baskın emrini taklit eder (<see cref="Params"/>, <c>Simulation.StartLiberationRaid</c> ile aynı
    /// alanlar): harekât tohumunun harita havuzu, bölgenin özelliği ve imza makinesi, kademenin kopya/güçlü özellikleri,
    /// gelişmiş akıl, sunucu sertliği ve ikmalin ek turu dengeden. Kadro tam canlı ve plakasız; son bölgenin çekirdeği yok.
    /// Raporu Markdown, hücre tablosunu CSV olarak yazar. Baskınlar iş parçacıklarına dağılır; sonuç sıradan bağımsızdır.
    /// </summary>
    public static class RaidBotMeasurement
    {
        /// <summary>Teknisyen'in yanındaki iki uzman; ilki karşılaştırmanın tabanı (eski botun kadrosu).</summary>
        public static readonly string[][] Squads =
        {
            new[] { "emp", "ana" }, new[] { "emp", "maden" }, new[] { "emp", "duman" },
            new[] { "ana", "maden" }, new[] { "ana", "duman" }, new[] { "maden", "duman" },
        };

        public static readonly string[] Missions = { "loot", "server" };

        public sealed class Config
        {
            /// <summary>İlk harekât tohumu (<see cref="RaidParams.MapSeed"/>); her tohum 30 haritalık bir havuz (türü başına 5).</summary>
            public uint FirstMapSeed = 7001;
            /// <summary>80 tohum: hücre (kadro × görev × tür × kademe) başına 400 baskın.</summary>
            public int MapSeeds = 80;
            public int[] Tiers = { 1, 2, 3, 4 };
            /// <summary>Hak taraması: ilk bu kadar tohumun senaryolarında, varsayılandan farklı her değer.</summary>
            public bool Sweeps = true;
            public int SweepMapSeeds = 20;
            public int[] DynamiteValues = { 1, 2, 3 }, SmokeValues = { 2, 3, 4 };
            public Balance Balance = new Balance();
            public int Alternatives = 8;
            /// <summary>Botun puan ağırlıkları (boşsa varsayılan).</summary>
            public RaidBot.Weights Weights;
            /// <summary>0: işlemci sayısı.</summary>
            public int Threads;
            public string Report = "Temp/raid_bot_report.md", Csv = "Temp/raid_bot_cells.csv";
            public Action<string> Progress;
        }

        public sealed class Scenario
        {
            public int Id, SeedIndex, MapIndex, MapSlot, Tier, Seed;
            public uint MapSeed;
            public string Kind, Mission, Region;
        }

        public sealed class RaidRun
        {
            public Scenario Sc;
            public int Squad, Dynamite, Smoke;
            /// <summary>"ana" (varsayılan haklar), "dinamit" ya da "sis" (tarama).</summary>
            public string Group;
            public bool Won;
            public int Down, Collateral, Turns, Walls, DynamiteHits, Smokes, Smoked;
            /// <summary>zafer, süre, teknisyen (düştü), ekip (hepsi düştü), kasa (imha edildi).</summary>
            public string End;
        }

        const string Main = "ana";

        public static string SquadName(int q) => "T+" + string.Join("+", Squads[q].Select(r => Baskin.UnitTypes[r].Short));

        /// <summary>Kampanyanın baskın emriyle aynı parametreler (<c>StartLiberationRaidCore</c>); kadro tam canlı, plakasız.</summary>
        public static RaidParams Params(Balance b, Scenario sc, string[] pick, int dynamite, int smoke)
        {
            var def = GameData.Region(sc.Region);
            var traits = new List<string> { def.Trait };
            if (sc.Tier >= 3) traits.Add("kopya");
            if (sc.Tier >= 4) traits.Add("guclu");
            bool server = sc.Mission == "server";
            var roles = Simulation.Roles.Where(r => r == "tek" || pick.Contains(r)).ToList();
            return new RaidParams
            {
                Seed = sc.Seed, Region = def.Id, RegionName = def.Name, Compute = def.Compute,
                SectorId = def.Id + "_" + (server ? 3 : 1), MissionKind = sc.Mission,
                RaidNumber = sc.Tier, Tier = sc.Tier, IsCore = false,
                Traits = traits, Signature = def.Enemy,
                Squad = roles.Select(r => new SquadSlot { Type = r, Hp = Simulation.MaxHp(r) }).ToList(), Roles = roles,
                MapSeed = sc.MapSeed, MapSlot = sc.MapSlot, Tactics = 1,
                TargetHpBonus = server ? b.liberationServerTargetHp : 0,
                ExtraMachines = server ? b.liberationServerMachines : 0,
                ExtraTurns = server ? b.liberationServerExtraTurns : b.liberationLootExtraTurns,
                CivilianHp = b.liberationCivilianHp,
                DynamiteCharges = dynamite, SmokeCharges = smoke,
            };
        }

        /// <summary>
        /// Senaryolar: her tohumun havuzundaki her harita (torbadaki sırası <see cref="RaidMaps.Slot"/>'un tersi) × kademe × görev.
        /// Bölge tohum, harita, kademe ve görevle döner (12 tohumda her hücre bütün bölgeleri görür); baskın tohumu bunlardan türer.
        /// </summary>
        public static List<Scenario> Scenarios(Config cfg)
        {
            var list = new List<Scenario>();
            for (int j = 0; j < cfg.MapSeeds; j++)
            {
                uint mapSeed = cfg.FirstMapSeed + (uint)j;
                var pool = RaidMaps.Pool(mapSeed);
                var slotOf = new int[pool.Count];
                for (int k = 1; k <= RaidMaps.PoolSize; k++) slotOf[RaidMaps.Slot(mapSeed, k)] = k;
                for (int i = 0; i < pool.Count; i++)
                {
                    string kind = RaidMaps.KindOf(pool[i].Name);
                    if (kind == null || slotOf[i] == 0) continue;
                    foreach (int tier in cfg.Tiers)
                        for (int m = 0; m < Missions.Length; m++)
                            list.Add(new Scenario
                            {
                                Id = list.Count, SeedIndex = j, MapSeed = mapSeed, MapIndex = i, MapSlot = slotOf[i], Kind = kind,
                                Tier = tier, Mission = Missions[m], Region = GameData.Regions[(j * 5 + i + tier * 3 + m * 7) % GameData.Regions.Length].Id,
                                Seed = RaidSeed(mapSeed, i, tier, m),
                            });
                }
            }
            return list;
        }

        static int RaidSeed(uint mapSeed, int map, int tier, int mission)
        {
            var r = new Rng(unchecked(mapSeed * 2654435761u ^ (uint)(map * 7919 + tier * 104729 + mission * 15485863)));
            r.Next();
            return (int)Math.Floor(r.Next() * 2147483647);
        }

        /// <summary>Bütün ölçümü koşar, raporu yazar ve kısa bir özet döndürür.</summary>
        public static string Run(Config cfg)
        {
            var clock = Stopwatch.StartNew();
            var b = cfg.Balance;
            var scenarios = Scenarios(cfg);
            var runs = new List<RaidRun>();
            foreach (var sc in scenarios)
                for (int q = 0; q < Squads.Length; q++)
                    runs.Add(new RaidRun { Sc = sc, Squad = q, Dynamite = b.liberationDynamiteCharges, Smoke = b.liberationSmokeCharges, Group = Main });
            if (cfg.Sweeps)
                foreach (var sc in scenarios.Where(s => s.SeedIndex < cfg.SweepMapSeeds))
                    for (int q = 0; q < Squads.Length; q++)
                    {
                        if (Squads[q].Contains("maden"))
                            foreach (int v in cfg.DynamiteValues.Where(v => v != b.liberationDynamiteCharges))
                                runs.Add(new RaidRun { Sc = sc, Squad = q, Dynamite = v, Smoke = b.liberationSmokeCharges, Group = "dinamit" });
                        if (Squads[q].Contains("duman"))
                            foreach (int v in cfg.SmokeValues.Where(v => v != b.liberationSmokeCharges))
                                runs.Add(new RaidRun { Sc = sc, Squad = q, Dynamite = b.liberationDynamiteCharges, Smoke = v, Group = "sis" });
                    }
            Execute(runs, cfg);
            clock.Stop();
            var report = Report(cfg, runs, clock.Elapsed);
            Write(cfg.Report, report);
            Write(cfg.Csv, Cells(runs));
            var main = runs.Where(r => r.Group == Main).ToList();
            return string.Join("\n", Enumerable.Range(0, Squads.Length).Select(q =>
                SquadName(q) + " " + Pct(Sum(main.Where(r => r.Squad == q)).Rate))) + $"\n{runs.Count} baskın, {Num(clock.Elapsed.TotalMinutes, "0.0")} dk → {cfg.Report}";
        }

        static void Execute(List<RaidRun> runs, Config cfg)
        {
            int threads = cfg.Threads > 0 ? cfg.Threads : Environment.ProcessorCount;
            int done = 0;
            // RaidMaps.Pool tek tohumluk ve kilitsiz bir önbellek tutar: her tohumun baskınları birlikte koşar, önbellek önceden dolar
            foreach (var group in runs.GroupBy(r => r.Sc.MapSeed))
            {
                RaidMaps.Pool(group.Key);
                var batch = group.ToArray();
                Parallel.For(0, batch.Length, new ParallelOptions { MaxDegreeOfParallelism = threads }, i => PlayOne(batch[i], cfg));
                done += batch.Length;
                cfg.Progress?.Invoke($"{done}/{runs.Count}");
            }
        }

        public static void PlayOne(RaidRun run, Config cfg)
        {
            var B = Baskin.Create(Params(cfg.Balance, run.Sc, Squads[run.Squad], run.Dynamite, run.Smoke));
            var tally = new RaidBot { Alternatives = cfg.Alternatives, W = cfg.Weights ?? new RaidBot.Weights() }.Play(B);
            var result = Baskin.Result(B);
            run.Won = result.Success; run.Down = result.Down; run.Collateral = result.Collateral; run.Turns = result.Turns;
            run.Walls = tally.Walls; run.DynamiteHits = tally.DynamiteHits; run.Smokes = tally.Smokes; run.Smoked = tally.SmokedShots;
            run.End = EndOf(B);
        }

        static string EndOf(Baskin.BState B)
        {
            if (B.Phase == "won") return "zafer";
            var team = B.Units.Where(u => u.Side == 'p').ToList();
            if (team.All(u => u.Dead) && !team.Any(u => u.Left)) return "ekip";
            if (team.Any(u => u.Type == "tek" && u.Dead && !u.Left)) return "teknisyen";
            if (B.Escape) return "kaçış";
            if (Baskin.IsLoot(B) && B.Carrier == 0 && Baskin.TargetsLeft(B).Count == 0) return "kasa";
            return "süre";
        }

        // ---------- Rapor ----------
        sealed class Agg
        {
            public int N, Won, Down, Collateral, Turns, Walls, DynamiteHits, Smokes, Smoked, AllWalls, AllSmokes;
            public double Rate => N == 0 ? double.NaN : 100.0 * Won / N;
            public double Per(int v) => N == 0 ? double.NaN : (double)v / N;
            public void Add(RaidRun r)
            {
                N++; Won += r.Won ? 1 : 0; Down += r.Down; Collateral += r.Collateral; Turns += r.Turns;
                Walls += r.Walls; DynamiteHits += r.DynamiteHits; Smokes += r.Smokes; Smoked += r.Smoked;
                if (r.Dynamite > 0 && r.Walls >= r.Dynamite) AllWalls++;
                if (r.Smoke > 0 && r.Smokes >= r.Smoke) AllSmokes++;
            }
        }

        static Agg Sum(IEnumerable<RaidRun> runs)
        {
            var a = new Agg();
            foreach (var r in runs) a.Add(r);
            return a;
        }

        /// <summary>Eşli fark (puan) ve %95 güven yarı genişliği: her baskın aynı senaryonun taban koşusuyla karşılaştırılır.</summary>
        static (double Mean, double Ci, int N) Paired(IEnumerable<RaidRun> runs, Func<RaidRun, RaidRun> baseOf)
        {
            var d = runs.Select(r => (r.Won ? 1.0 : 0.0) - (baseOf(r).Won ? 1.0 : 0.0)).ToList();
            if (d.Count < 2) return (double.NaN, double.NaN, d.Count);
            double mean = d.Average(), sd = Math.Sqrt(d.Sum(x => (x - mean) * (x - mean)) / (d.Count - 1));
            return (100 * mean, 100 * 1.96 * sd / Math.Sqrt(d.Count), d.Count);
        }

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static string Pct(double v) => double.IsNaN(v) ? "–" : v.ToString("0.0", Inv) + "%";
        static string Num(double v, string f = "0.00") => double.IsNaN(v) ? "–" : v.ToString(f, Inv);
        static string Diff((double Mean, double Ci, int N) d) => double.IsNaN(d.Mean) ? "–" : (d.Mean >= 0 ? "+" : "") + d.Mean.ToString("0.0", Inv) + " ±" + d.Ci.ToString("0.0", Inv);

        static string Report(Config cfg, List<RaidRun> runs, TimeSpan elapsed)
        {
            var b = cfg.Balance;
            var main = runs.Where(r => r.Group == Main).ToList();
            var baseRun = main.Where(r => r.Squad == 0).ToDictionary(r => r.Sc.Id);
            var defaultRun = main.ToDictionary(r => (r.Sc.Id, r.Squad));
            RaidRun BaseOf(RaidRun r) => baseRun[r.Sc.Id];
            var sb = new StringBuilder();
            sb.AppendLine("# Baskın botu ölçümü");
            sb.AppendLine();
            sb.AppendLine($"- {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", Inv)} UTC, {runs.Count} baskın ({main.Count} ana + {runs.Count - main.Count} tarama), {Num(elapsed.TotalMinutes, "0.0")} dk.");
            sb.AppendLine($"- Harekât tohumları {cfg.FirstMapSeed}–{cfg.FirstMapSeed + (uint)cfg.MapSeeds - 1} (her biri 30 harita, türü başına 5); kademeler {string.Join(", ", cfg.Tiers)}; görevler ikmal ve sunucu; bölge senaryoyla döner.");
            sb.AppendLine($"- Senaryo başına her kadro aynı tahtada (eşli). Hücre (kadro × görev × tür × kademe) başına {main.Count / Math.Max(1, Squads.Length * 2 * RaidMaps.Kinds.Length * cfg.Tiers.Length)} baskın.");
            sb.AppendLine($"- Denge: dinamit {b.liberationDynamiteCharges}, sis {b.liberationSmokeCharges}, sunucu ek canı {b.liberationServerTargetHp}, sunucu ek makinesi {b.liberationServerMachines}, ikmal ek turu {b.liberationLootExtraTurns}, sunucu ek turu {b.liberationServerExtraTurns}, sivil canı {b.liberationCivilianHp}. Bot: ilk adımda {cfg.Alternatives} seçenek, gerçek evreyle doğrulama.");
            sb.AppendLine("- Fark sütunları T+E+A'ya göre eşli farktır (puan, ±%95 güven yarı genişliği).");
            sb.AppendLine();

            sb.AppendLine("## Kadrolar");
            sb.AppendLine();
            sb.AppendLine("| Kadro | Toplam | İkmal | Sunucu | Fark | İkmal farkı | Sunucu farkı | Sivil kaybı / baskın | Düşen / baskın | Tur | n |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            for (int q = 0; q < Squads.Length; q++)
            {
                var all = main.Where(r => r.Squad == q).ToList();
                var a = Sum(all);
                var loot = all.Where(r => r.Sc.Mission == "loot").ToList();
                var server = all.Where(r => r.Sc.Mission == "server").ToList();
                sb.AppendLine($"| {SquadName(q)} | {Pct(a.Rate)} | {Pct(Sum(loot).Rate)} | {Pct(Sum(server).Rate)} | {(q == 0 ? "taban" : Diff(Paired(all, BaseOf)))} | {(q == 0 ? "" : Diff(Paired(loot, BaseOf)))} | {(q == 0 ? "" : Diff(Paired(server, BaseOf)))} | {Num(a.Per(a.Collateral))} | {Num(a.Per(a.Down))} | {Num(a.Per(a.Turns), "0.0")} | {a.N} |");
            }
            sb.AppendLine();

            void Breakdown(string title, IEnumerable<RaidRun> set, IList<string> cols, Func<RaidRun, string> colOf)
            {
                var rows = set.ToList();
                sb.AppendLine($"## {title}");
                sb.AppendLine();
                sb.AppendLine("Hücrede oran, parantezde T+E+A'ya göre eşli fark (puan).");
                sb.AppendLine();
                sb.AppendLine("| Kadro | " + string.Join(" | ", cols) + " |");
                sb.AppendLine("|---|" + string.Concat(cols.Select(_ => "---|")));
                for (int q = 0; q < Squads.Length; q++)
                {
                    var cells = cols.Select(c =>
                    {
                        var cell = rows.Where(r => r.Squad == q && colOf(r) == c).ToList();
                        var rate = Pct(Sum(cell).Rate);
                        return q == 0 ? rate : rate + " (" + Diff(Paired(cell, BaseOf)).Split(' ')[0] + ")";
                    });
                    sb.AppendLine($"| {SquadName(q)} | " + string.Join(" | ", cells) + " |");
                }
                var n = rows.Count(r => r.Squad == 0) / Math.Max(1, cols.Count);
                sb.AppendLine();
                sb.AppendLine($"Hücre başına ~{n} baskın.");
                sb.AppendLine();
            }

            var kinds = RaidMaps.Kinds;
            var tiers = cfg.Tiers.Select(t => "K" + t).ToList();
            Breakdown("Harita türüne göre (iki görev)", main, kinds, r => r.Sc.Kind);
            Breakdown("Kademeye göre (iki görev)", main, tiers, r => "K" + r.Sc.Tier);
            Breakdown("İkmal görevi, harita türüne göre", main.Where(r => r.Sc.Mission == "loot"), kinds, r => r.Sc.Kind);
            Breakdown("Sunucu görevi, harita türüne göre", main.Where(r => r.Sc.Mission == "server"), kinds, r => r.Sc.Kind);
            Breakdown("İkmal görevi, kademeye göre", main.Where(r => r.Sc.Mission == "loot"), tiers, r => "K" + r.Sc.Tier);
            Breakdown("Sunucu görevi, kademeye göre", main.Where(r => r.Sc.Mission == "server"), tiers, r => "K" + r.Sc.Tier);

            sb.AppendLine("## En büyük hücre farkları (kadro × görev × tür × kademe)");
            sb.AppendLine();
            sb.AppendLine("Her kadronun T+E+A'yı en çok geçtiği ve en çok geride kaldığı üç hücre (hücre küçük: güven aralığı geniş).");
            sb.AppendLine();
            sb.AppendLine("| Kadro | En çok önde | En çok geride |");
            sb.AppendLine("|---|---|---|");
            for (int q = 1; q < Squads.Length; q++)
            {
                var cells = main.Where(r => r.Squad == q).GroupBy(r => (r.Sc.Mission, r.Sc.Kind, r.Sc.Tier))
                    .Select(g => (g.Key, D: Paired(g, BaseOf), Rate: Sum(g).Rate)).ToList();
                string Show(IEnumerable<((string Mission, string Kind, int Tier) Key, (double Mean, double Ci, int N) D, double Rate)> xs) =>
                    string.Join("; ", xs.Select(c => $"{(c.Key.Mission == "loot" ? "ikmal" : "sunucu")}/{c.Key.Kind}/K{c.Key.Tier} {Pct(c.Rate)} {Diff(c.D)}"));
                sb.AppendLine($"| {SquadName(q)} | {Show(cells.OrderByDescending(c => c.D.Mean).Take(3))} | {Show(cells.OrderBy(c => c.D.Mean).Take(3))} |");
            }
            sb.AppendLine();

            sb.AppendLine("## Bitiş nedenleri (ana koşu, baskınların yüzdesi)");
            sb.AppendLine();
            var ends = new[] { "zafer", "süre", "teknisyen", "ekip", "kasa", "kaçış" };
            sb.AppendLine("| Kadro | Görev | " + string.Join(" | ", ends) + " |");
            sb.AppendLine("|---|---|" + string.Concat(ends.Select(_ => "---|")));
            for (int q = 0; q < Squads.Length; q++)
                foreach (var m in Missions)
                {
                    var set = main.Where(r => r.Squad == q && r.Sc.Mission == m).ToList();
                    sb.AppendLine($"| {SquadName(q)} | {(m == "loot" ? "ikmal" : "sunucu")} | " + string.Join(" | ", ends.Select(e => Pct(100.0 * set.Count(r => r.End == e) / Math.Max(1, set.Count)))) + " |");
                }
            sb.AppendLine();

            sb.AppendLine("## Yetenek kullanımı (ana koşu, baskın başına)");
            sb.AppendLine();
            sb.AppendLine("| Kadro | Görev | Dinamit: duvar | Dinamit: robot/nesne | Duvar hakkı biten baskın | Sis | Sis hakkı biten baskın | Sise kesilen niyet |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            for (int q = 0; q < Squads.Length; q++)
            {
                bool miner = Squads[q].Contains("maden"), smoke = Squads[q].Contains("duman");
                if (!miner && !smoke) continue;
                foreach (var m in Missions)
                {
                    var a = Sum(main.Where(r => r.Squad == q && r.Sc.Mission == m));
                    sb.AppendLine($"| {SquadName(q)} | {(m == "loot" ? "ikmal" : "sunucu")} | {(miner ? Num(a.Per(a.Walls)) : "")} | {(miner ? Num(a.Per(a.DynamiteHits)) : "")} | {(miner ? Pct(100.0 * a.AllWalls / a.N) : "")} | {(smoke ? Num(a.Per(a.Smokes)) : "")} | {(smoke ? Pct(100.0 * a.AllSmokes / a.N) : "")} | {(smoke ? Num(a.Per(a.Smoked)) : "")} |");
                }
            }
            sb.AppendLine();

            void Sweep(string group, string title, Func<RaidRun, int> valueOf, int def, string usage, Func<Agg, double> use)
            {
                var sweep = runs.Where(r => r.Group == group).ToList();
                if (sweep.Count == 0) return;
                var ids = new HashSet<int>(sweep.Select(r => r.Sc.Id));
                sb.AppendLine($"## {title}");
                sb.AppendLine();
                sb.AppendLine($"İlk {cfg.SweepMapSeeds} tohumun senaryoları; fark aynı kadronun varsayılan hakla ({def}) aynı senaryodaki koşusuna göre, \"T+E+A'ya göre\" sütunu tabana göre.");
                sb.AppendLine();
                sb.AppendLine($"| Kadro | Hak | Toplam | İkmal | Sunucu | Varsayılana göre | T+E+A'ya göre | {usage} | n |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
                foreach (int q in sweep.Select(r => r.Squad).Distinct().OrderBy(x => x))
                {
                    var values = sweep.Where(r => r.Squad == q).Select(valueOf).Concat(new[] { def }).Distinct().OrderBy(v => v);
                    foreach (int v in values)
                    {
                        var set = v == def ? main.Where(r => r.Squad == q && ids.Contains(r.Sc.Id)).ToList() : sweep.Where(r => r.Squad == q && valueOf(r) == v).ToList();
                        var a = Sum(set);
                        sb.AppendLine($"| {SquadName(q)} | {v} | {Pct(a.Rate)} | {Pct(Sum(set.Where(r => r.Sc.Mission == "loot")).Rate)} | {Pct(Sum(set.Where(r => r.Sc.Mission == "server")).Rate)} | {(v == def ? "–" : Diff(Paired(set, r => defaultRun[(r.Sc.Id, r.Squad)])))} | {Diff(Paired(set, BaseOf))} | {Num(use(a))} | {a.N} |");
                    }
                }
                sb.AppendLine();
            }
            Sweep("dinamit", "Dinamit hakkı taraması", r => r.Dynamite, b.liberationDynamiteCharges, "Duvar / baskın", a => a.Per(a.Walls));
            Sweep("sis", "Sis hakkı taraması", r => r.Smoke, b.liberationSmokeCharges, "Sis / baskın", a => a.Per(a.Smokes));
            return sb.ToString();
        }

        /// <summary>Hücre tablosu (ana koşu): kadro × görev × tür × kademe.</summary>
        static string Cells(List<RaidRun> runs)
        {
            var main = runs.Where(r => r.Group == Main).ToList();
            var baseRun = main.Where(r => r.Squad == 0).ToDictionary(r => r.Sc.Id);
            var sb = new StringBuilder("squad,mission,kind,tier,n,won,rate,delta_vs_TEA,ci95,down,collateral,walls,dynamite_hits,smokes,smoked\n");
            foreach (var g in main.GroupBy(r => (r.Squad, r.Sc.Mission, r.Sc.Kind, r.Sc.Tier)).OrderBy(g => g.Key))
            {
                var a = Sum(g);
                var d = Paired(g, r => baseRun[r.Sc.Id]);
                sb.AppendLine(string.Join(",", SquadName(g.Key.Squad), g.Key.Mission, g.Key.Kind, g.Key.Tier, a.N, a.Won, Num(a.Rate, "0.0"),
                    Num(d.Mean, "0.0"), Num(d.Ci, "0.0"), Num(a.Per(a.Down)), Num(a.Per(a.Collateral)), Num(a.Per(a.Walls)), Num(a.Per(a.DynamiteHits)), Num(a.Per(a.Smokes)), Num(a.Per(a.Smoked))));
            }
            return sb.ToString();
        }

        static void Write(string path, string text)
        {
            var full = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, text);
        }
    }
}
