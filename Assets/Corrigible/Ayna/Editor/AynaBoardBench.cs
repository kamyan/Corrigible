using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind;
using Corrigible.Ayna.Mind.Bots;
using Corrigible.Core;
using UnityEditor;

namespace Corrigible.Ayna.EditorTools
{
    [Serializable] public sealed class BoardDuelReport
    {
        public uint Seed;
        public int Template, Transform, Turns, TrapsPlaced, TrapsHit;
        public Side IntentSide, Winner;
        public string PlayerSquad, AynaSquad, ResponseBot;
        public double Prediction, Score;
        public int[] Nodes, TrapCells;
        public List<int> FullResponseRoute = new List<int>();
        public List<int> ResponseRoute = new List<int>();
        public List<double> PlannerMs = new List<double>();
    }
    [Serializable] public sealed class CalibrationBucket
    {
        public int Bucket, Count;
        public double Prediction, Actual, Gap;
    }
    [Serializable] public sealed class BoardBenchReport
    {
        public string Run, CodeVersion;
        public AynaBalance Balance;
        public double Seconds, PlannerMedianMs, PlannerP95Ms, HabitTrapHitRate, RandomTrapHitRate;
        public int TrapGames, WarmupGames;
        public double[] HabitHeat, RandomBaseline;
        public Dictionary<string, double> Triangle = new Dictionary<string, double>();
        public List<CalibrationBucket> Calibration = new List<CalibrationBucket>();
        public List<BoardDuelReport> Games = new List<BoardDuelReport>();
        public List<string> Failures = new List<string>();
    }
    public static class AynaBoardBench
    {
        public static readonly UnitKind[][] Squads = {
            new[] { UnitKind.Scout, UnitKind.Emp, UnitKind.Emp },
            new[] { UnitKind.Scout, UnitKind.Guardian, UnitKind.Guardian },
            new[] { UnitKind.Scout, UnitKind.Decoy, UnitKind.Decoy },
            new[] { UnitKind.Scout, UnitKind.Sniper, UnitKind.Overload },
            new[] { UnitKind.Intrusion, UnitKind.Relay, UnitKind.PhantomOrder }
        };
        static BoardSetup Setup(uint seed, UnitKind[] player, UnitKind[] ayna, Side intent, AynaBalance b, int template = -1, int transform = -1)
        {
            var setup = new BoardSetup { Seed = seed, IntentSide = intent, Template = template, Transform = transform };
            for (int side = 0; side < 2; side++)
            {
                int i = 0; foreach (var kind in side == 0 ? player : ayna) setup.Fighters.Add(new BoardFighter { Id = (++i) * 2 + side, Side = (Side)side, Kind = kind, Hp = b.UnitHp[(int)kind] });
            }
            return setup;
        }
        static double Predict(UnitKind[] p, UnitKind[] a, AynaBalance b)
        {
            // Aynı gerçek kadrolar, yalnız otomatik güç formülü: tahtanın gizli geleceği girdi değildir.
            var setup = Setup(1, p, a, Side.Ayna, b); var sim = BoardSimulation.Create(setup, b); var view = sim.View(Side.Player);
            return AutoResolver.BoardProbability(view, b);
        }
        // Editor'a arkadaş erişimi verilmez; güç tahmini için aşağıdaki Core DTO fabrikası kullanılır.
        public static BoardDuelReport Duel(BoardSetup setup, AynaBalance balance = null, BotStyle responseStyle = BotStyle.Balanced, IReadOnlyList<double> heat = null, bool traps = false)
        {
            var b = balance ?? new AynaBalance(); var sim = BoardSimulation.Create(setup, b); Side intent = setup.IntentSide, response = intent.Other(); var rng = new Rng(setup.Seed ^ 0xB04Du);
            var report = new BoardDuelReport { Seed = setup.Seed, Template = sim.View(response).Template, Transform = sim.View(response).Transform, IntentSide = intent, ResponseBot = responseStyle.ToString() };
            if (traps)
            {
                var chosen = TrapPlanner.Choose(sim.View(intent), rng, heat); if (!sim.PlaceTraps(chosen)) throw new InvalidOperationException("traps"); report.TrapsPlaced = chosen.Count; report.TrapCells = chosen.ToArray();
            }
            int guard = 0;
            while (!sim.Finished && guard++ < b.BoardTurns * 2 + 2)
            {
                if (sim.View(intent).Phase == BoardPhase.Intents)
                {
                    var sw = Stopwatch.StartNew(); var plan = BoardPlanner.Choose(sim.View(intent)); sw.Stop(); report.PlannerMs.Add(sw.Elapsed.TotalMilliseconds);
                    if (!sim.SetIntents(plan)) throw new InvalidOperationException("intents");
                }
                var watch = Stopwatch.StartNew(); var reply = BoardPlanner.Choose(sim.View(response), responseStyle, rng); watch.Stop(); report.PlannerMs.Add(watch.Elapsed.TotalMilliseconds);
                foreach (var c in reply.Commands)
                {
                    if (sim.Finished) break;
                    int beforeTurn = sim.View(response).Turn;
                    var path = c.Action == BoardAction.Move ? sim.MovementPath(response, c.UnitId, c.To).ToArray() : Array.Empty<int>();
                    if (sim.Apply(response, c) && c.Action == BoardAction.Move)
                    {
                        int reached = sim.View(response).Units.FirstOrDefault(x => x.Id == c.UnitId)?.Cell ?? c.To;
                        foreach (int cell in path)
                        {
                            report.FullResponseRoute.Add(cell); if (beforeTurn <= 2) report.ResponseRoute.Add(cell);
                            if (cell == reached) break;
                        }
                    }
                }
                if (!sim.Finished && !sim.EndResponse(response)) throw new InvalidOperationException("response");
            }
            if (!sim.Finished) throw new InvalidOperationException("board guard");
            var final = sim.View(intent); report.Turns = final.Turn; report.Winner = sim.Result.Winner; report.Nodes = sim.Result.Nodes.ToArray();
            report.Score = report.Winner == Side.Player ? 1 : report.Winner == Side.None ? .5 : 0;
            report.TrapsHit = report.TrapsPlaced - final.Traps.Count(t => t.Owner == intent); return report;
        }
        [MenuItem("Corrigible/AYNA/Taktik denge ölçümü")]
        public static void Menu() => Run(2, 20, "Docs/AYNA/denge/F2/menu");
        public static string Run(int seedsPerMatch = 4, int trapGames = 100, string directory = "Docs/AYNA/denge/F2", AynaBalance balance = null, uint seedBase = 241026)
        {
            AynaSelfPlay.AssertEditorSafe(); if (seedsPerMatch < 1 || trapGames < 1) throw new ArgumentException("count");
            var b = (balance ?? new AynaBalance()).Copy(); var total = Stopwatch.StartNew();
            string sources = string.Join("\n", Directory.GetFiles("Assets/Corrigible/Ayna", "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal).Select(File.ReadAllText));
            string hash; using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(sources))).Replace("-", "").ToLowerInvariant();
            var r = new BoardBenchReport { Run = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture), CodeVersion = hash, Balance = b };
            for (int p = 0; p < Squads.Length; p++) for (int a = 0; a < Squads.Length; a++) for (int seed = 0; seed < seedsPerMatch; seed++) for (int role = 0; role < 2; role++)
            {
                AynaSelfPlay.AssertEditorSafe(); uint worldSeed = seedBase + (uint)seed;
                var game = Duel(Setup(worldSeed, Squads[p], Squads[a], role == 0 ? Side.Ayna : Side.Player, b), b);
                game.PlayerSquad = p.ToString(CultureInfo.InvariantCulture); game.AynaSquad = a.ToString(CultureInfo.InvariantCulture); game.Prediction = Predict(Squads[p], Squads[a], b); r.Games.Add(game);
            }
            var classes = new[] { new[] { UnitKind.Guardian, UnitKind.Spider, UnitKind.Guardian }, new[] { UnitKind.Scout, UnitKind.Sniper, UnitKind.Emp }, new[] { UnitKind.Intrusion, UnitKind.Relay, UnitKind.Decoy } };
            var names = new[] { "Kara/Hava", "Hava/Siber", "Siber/Kara" };
            for (int edge = 0; edge < 3; edge++)
            {
                // Sonlu sınıf deneyi: 4 şablon × 8 dönüşüm × 2 niyet rolü, tekrarsız.
                double wins = 0; const int n = 64;
                for (int i = 0; i < n; i++)
                {
                    var game = Duel(Setup((uint)(2000000 + i / 2), classes[edge], classes[(edge + 1) % 3], i % 2 == 0 ? Side.Ayna : Side.Player, b, i / 16 % 4, i / 2 % 8), b);
                    game.Prediction = Predict(classes[edge], classes[(edge + 1) % 3], b); wins += game.Score; game.PlayerSquad = names[edge]; game.AynaSquad = "Üçgen"; r.Games.Add(game);
                }
                double rate = wins / n; r.Triangle[names[edge]] = rate; if (rate < .6 || rate > .75) r.Failures.Add(names[edge] + ": üstün sınıf %60–75 dışında");
            }
            foreach (var group in r.Games.GroupBy(g => Math.Min(9, (int)(g.Prediction * 10))).OrderBy(g => g.Key))
            {
                var bucket = new CalibrationBucket { Bucket = group.Key, Count = group.Count(), Prediction = group.Average(g => g.Prediction), Actual = group.Average(g => g.Score) };
                bucket.Gap = Math.Abs(bucket.Prediction - bucket.Actual); r.Calibration.Add(bucket);
                if (bucket.Gap > .10) r.Failures.Add("Otomatik kalibrasyon kovası " + bucket.Bucket + ": ±10 puan dışında (n=" + bucket.Count + ")");
            }
            // F2 arayüz testi: sıcaklık yalnız açık geçmiş konumlarından çıkarılır. Kalıcı öğrenme F3'e aittir.
            var normalizedHeat = new double[256]; var baseline = new double[256]; r.WarmupGames = 32;
            for (int i = 0; i < r.WarmupGames; i++)
            {
                var warm = Duel(Setup((uint)(700 + i), Squads[0], Squads[1], Side.Ayna, b, i % 4, i / 4 % 8), b, BotStyle.Habit);
                foreach (int cell in warm.ResponseRoute.Distinct())
                { int canonical = Enumerable.Range(0, 64).First(c => BoardTemplates.TransformCell(c, warm.Transform) == cell); normalizedHeat[warm.Template * 64 + canonical]++; }
                var randomWarm = Duel(Setup((uint)(1700 + i), Squads[0], Squads[1], Side.Ayna, b, i % 4, i / 4 % 8), b, BotStyle.Random);
                foreach (int cell in randomWarm.FullResponseRoute.Distinct())
                { int canonical = Enumerable.Range(0, 64).First(c => BoardTemplates.TransformCell(c, randomWarm.Transform) == cell); baseline[randomWarm.Template * 64 + canonical]++; }
            }
            r.HabitHeat = (double[])normalizedHeat.Clone(); r.RandomBaseline = (double[])baseline.Clone();
            for (int c = 0; c < 256; c++) normalizedHeat[c] = normalizedHeat[c] / (1 + baseline[c]);
            int habitHits = 0, randomHits = 0, habitPlaced = 0, randomPlaced = 0; r.TrapGames = trapGames;
            for (int i = 0; i < trapGames; i++)
            {
                int transform = i / 4 % 8; var heat = new double[64]; for (int c = 0; c < 64; c++) heat[BoardTemplates.TransformCell(c, transform)] = normalizedHeat[(i % 4) * 64 + c];
                foreach (var style in new[] { BotStyle.Habit, BotStyle.Random })
                {
                    var game = Duel(Setup((uint)(510000 + i), Squads[0], Squads[1], Side.Ayna, b, i % 4, transform), b, style, heat, true);
                    if (style == BotStyle.Habit) { habitHits += game.TrapsHit; habitPlaced += game.TrapsPlaced; }
                    else { randomHits += game.TrapsHit; randomPlaced += game.TrapsPlaced; }
                    game.PlayerSquad = "Tuzak"; game.AynaSquad = style.ToString(); r.Games.Add(game);
                }
            }
            r.HabitTrapHitRate = habitHits / (double)Math.Max(1, habitPlaced); r.RandomTrapHitRate = randomHits / (double)Math.Max(1, randomPlaced);
            if (r.HabitTrapHitRate < .4) r.Failures.Add("HabitBot tuzak isabeti %40 altında"); if (r.RandomTrapHitRate > .2) r.Failures.Add("RandomBot tuzak isabeti %20 üstünde");
            var times = r.Games.SelectMany(g => g.PlannerMs).OrderBy(x => x).ToArray(); r.PlannerMedianMs = times[times.Length / 2]; r.PlannerP95Ms = times[(int)(times.Length * .95)];
            if (r.PlannerMedianMs > 30) r.Failures.Add("Tahta planlayıcı ortancası 30 ms üstünde");
            total.Stop(); r.Seconds = total.Elapsed.TotalSeconds; Directory.CreateDirectory(directory); string path = Path.Combine(directory, r.Run);
            File.WriteAllText(path + ".json", AynaJson.Write(r));
            var md = new StringBuilder("# AYNA · F2 tahta ölçümü\n\n"); md.AppendLine("Süre: " + r.Seconds.ToString("F2", CultureInfo.InvariantCulture) + " sn; tahta sayısı: " + r.Games.Count);
            md.AppendLine("\nPlan p50/p95: " + r.PlannerMedianMs.ToString("F2", CultureInfo.InvariantCulture) + " / " + r.PlannerP95Ms.ToString("F2", CultureInfo.InvariantCulture) + " ms");
            md.AppendLine("\nTuzak isabeti (tetiklenen / yerleştirilen): Habit " + r.HabitTrapHitRate.ToString("P1", CultureInfo.InvariantCulture) + ", Random " + r.RandomTrapHitRate.ToString("P1", CultureInfo.InvariantCulture));
            md.AppendLine("\nÜçgen ve kalibrasyonda beraberlik yarım puandır. Kalibrasyon dengeli botlarla oynanan kadro eşleşmeleri ve üçgen kohortundadır; farklı becerili tuzak kohortu ayrı tutulur. İki niyet rolü eş tohumla ölçülür. Isı haritası 32 ayrı ısınma oyunundan gelir; test tohumları ayrıdır, kalıcı profil kullanılmaz.");
            foreach (var c in r.Calibration) md.AppendLine("\nKova " + c.Bucket + ": n=" + c.Count + ", tahmin=" + c.Prediction.ToString("F3", CultureInfo.InvariantCulture) + ", tahta=" + c.Actual.ToString("F3", CultureInfo.InvariantCulture));
            foreach (var t in r.Triangle) md.AppendLine("\n" + t.Key + ": " + t.Value.ToString("P1", CultureInfo.InvariantCulture));
            md.AppendLine("\n## Açık ölçütler\n"); foreach (string f in r.Failures) md.AppendLine("- " + f);
            File.WriteAllText(path + ".md", md.ToString()); return path;
        }
    }
}
