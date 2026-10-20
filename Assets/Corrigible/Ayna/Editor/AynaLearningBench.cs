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
    [Serializable] public sealed class LearningBenchOptions
    {
        public int Series = 100, Games = 5, SeriesOffset, DiagnosticCases;
        public uint Seed = 3101026;
        public string Bot = "Habit", Directory = "Docs/AYNA/denge/F3";
        public LearningLevel Learning = LearningLevel.Full;
        public bool Tactical;
        public AynaBalance Balance = new AynaBalance();
    }
    [Serializable] public sealed class LearningGameResult
    {
        public int Series, Game, Cycles, Decisions, Hits, SurpriseCycles, Generation;
        public int[] OrdersByKind = new int[13], SurpriseByKind = new int[13];
        public uint Seed;
        public bool AynaWon, PlayerWon;
        public double Readability;
        public EndReason End;
        public Architecture AynaArchitecture, PlayerArchitecture;
        public int Counters;
    }
    [Serializable] public sealed class LearningBenchReport
    {
        public string CodeVersion;
        public LearningBenchOptions Options;
        public List<LearningGameResult> Games = new List<LearningGameResult>();
        public List<string> Failures = new List<string>();
        public double Seconds, FirstWinRate, LastWinRate, Gain, Top1FromThird, SurpriseRate, PlayerWinRate, PlannerMedianMs, PlannerP95Ms;
    }
    public static class AynaLearningBench
    {
        static IEnumerator<int> job;
        static int completed, requested;
        static string output, error;
        public static string Start(LearningBenchOptions options)
        {
            AynaSelfPlay.AssertEditorSafe(); if (job != null) throw new InvalidOperationException("learning benchmark busy");
            if (options == null || options.Series < 1 || options.Games < 1 || options.SeriesOffset < 0) throw new ArgumentException("benchmark count");
            options = Newtonsoft.Json.JsonConvert.DeserializeObject<LearningBenchOptions>(AynaJson.Write(options));
            options.Balance.TacticalPlanning = options.Tactical;
            completed = 0; requested = options.Series * options.Games; output = error = null;
            job = Steps(options).GetEnumerator(); EditorApplication.update += Tick; return Status();
        }
        public static string Status() => AynaJson.Write(new { Running = job != null, Completed = completed, Requested = requested, Output = output, Error = error });
        public static void Cancel() { EditorApplication.update -= Tick; job?.Dispose(); job = null; }
        static void Tick()
        {
            try
            {
                AynaSelfPlay.AssertEditorSafe();
                // Kare arası boşluk toplam ölçümü şişirmesin; oyun içi arama bütçesi değişmez.
                for (int i = 0; i < 4 && job != null; i++)
                    if (job.MoveNext()) completed = job.Current; else Cancel();
            }
            catch (Exception ex) { error = ex.ToString(); Cancel(); }
        }
        static IEnumerable<int> Steps(LearningBenchOptions options)
        {
            var report = new LearningBenchReport { Options = options }; var timings = new List<double>(); var diagnostics = new List<object>();
            string sources = string.Join("\n", System.IO.Directory.GetFiles("Assets/Corrigible/Ayna", "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal).Select(File.ReadAllText));
            using (var sha = SHA256.Create()) report.CodeVersion = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(sources))).Replace("-", "").ToLowerInvariant();
            var watch = Stopwatch.StartNew();
            string runTag = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + "_" + options.Bot + "_" + options.Learning;
            System.IO.Directory.CreateDirectory(Path.Combine(options.Directory, "partial"));
            System.IO.Directory.CreateDirectory(Path.Combine(options.Directory, "failures"));
            for (int series = options.SeriesOffset; series < options.SeriesOffset + options.Series; series++)
            {
                var profile = new AynaProfileData(); var opening = new Rng(options.Seed + (uint)series * 101);
                var fixedChoice = new StartChoice { Architecture = (Architecture)(series % 3), Region = AynaWorldData.Regions[(int)(opening.Next() * 12)].Id };
                for (int game = 0; game < options.Games; game++)
                {
                    uint seed = options.Seed + (uint)(series * 101 + game); var random = new Rng(seed ^ 0xB04Du);
                    var choice = options.Bot == "Random" ? new StartChoice { Architecture = (Architecture)(int)(random.Next() * 3), Region = AynaWorldData.Regions[(int)(random.Next() * 12)].Id } : fixedChoice.Copy();
                    var setup = new StartSetup { Seed = seed, Player = choice, Settings = new AynaSettings { Learning = options.Learning } };
                    var match = new LearningMatch(setup, options.Seed + ":" + series + ":" + game, profile, options.Balance);
                    var sim = match.Simulation; var bot = AynaSelfPlay.Bot(options.Bot, seed ^ 0x13579BDFu);
                    var row = new LearningGameResult { Series = series, Game = game, Seed = seed, PlayerArchitecture = choice.Architecture, AynaArchitecture = sim.View(Side.Ayna).Architecture, Counters = sim.Record.Setup.AynaCounters.Count };
                    while (!sim.Outcome.Finished)
                    {
                        AynaSelfPlay.AssertEditorSafe(); var timer = Stopwatch.StartNew(); match.PrepareCycle(); timer.Stop(); double forecastMs = timer.Elapsed.TotalMilliseconds; var before = sim.View(Side.Player); var forecast = match.AynaMind.Predict(sim.View(Side.Ayna));
                        var top = forecast.OrderByDescending(p => p.Probability).ThenBy(p => p.Kind).ThenBy(p => p.Region, StringComparer.Ordinal).First();
                        var pp = bot.MakePlan(before); timer.Restart(); var ap = match.MakeAynaPlan(); timer.Stop(); timings.Add(forecastMs + timer.Elapsed.TotalMilliseconds);
                        sim.BeginCycle(pp, ap);
                        var actual = sim.View(Side.Player).Receipts.Where(r => (r.Rejection == RejectReason.None || r.Rejection == RejectReason.LostCondition) && r.Kind != OrderKind.Wait).ToArray();
                        if (actual.Length > 0)
                        {
                            row.Decisions++; if (sim.View(Side.Player).LastSurprise) row.SurpriseCycles++;
                            var probabilities = actual.Select(r => new { Receipt = r, Probability = MindSession.TargetProbability(forecast, r.Kind, r.Region ?? before.Armies.FirstOrDefault(a => a.Id == pp.Orders[r.Index].ArmyId)?.Region) }).ToArray();
                            foreach (var r in actual) row.OrdersByKind[(int)r.Kind]++;
                            if (sim.View(Side.Player).LastSurprise) row.SurpriseByKind[(int)probabilities.OrderBy(p => p.Probability).ThenBy(p => p.Receipt.Index).First().Receipt.Kind]++;
                            if (sim.View(Side.Player).LastSurprise && game >= 2 && diagnostics.Count < options.DiagnosticCases)
                                diagnostics.Add(new { Series = series, Game = game, Cycle = before.Cycle, Compute = before.Compute, Servers = before.Regions.ToDictionary(r => r.Id, r => r.OwnServers), Forecast = forecast, Actual = actual.Select(r => pp.Orders[r.Index]).ToArray(), Probabilities = probabilities.Select(p => new { p.Receipt.Kind, p.Receipt.Region, p.Probability }).ToArray(), Behavior = match.AynaMind.Checkpoint().Behavior });
                            if (actual.Any(r => r.Kind == top.Kind && (r.Region ?? before.Armies.FirstOrDefault(a => a.Id == pp.Orders[r.Index].ArmyId)?.Region) == top.Region)) row.Hits++;
                        }
                        while (sim.IsResolving)
                        {
                            if (!options.Tactical) { sim.ResolvePendingAuto(); continue; }
                            if (!sim.StartBoard()) throw new InvalidOperationException("board");
                            var intent = sim.Board(Side.Player).IntentSide; var response = intent.Other();
                            var heat = match.AynaMind.Heat(sim.Board(Side.Ayna)); sim.PlaceBoardTraps(TrapPlanner.Choose(sim.Board(intent), match.AynaMind.Rng, intent == Side.Ayna ? heat : null));
                            int guard = 0;
                            while (sim.Board(response).Phase != BoardPhase.Finished && guard++ < 20)
                            {
                                if (sim.Board(intent).Phase == BoardPhase.Intents)
                                {
                                    var proposed = BoardPlanner.Choose(sim.Board(intent), responseHeat: intent == Side.Ayna ? heat : null);
                                    if (!sim.SetBoardIntents(proposed))
                                    {
                                        File.WriteAllText(Path.Combine(options.Directory, "failures", runTag + ".json"), AynaJson.Write(new { Save = match.Save(), Proposed = proposed, Heat = heat, Row = row, Cycle = sim.View(Side.Player).Cycle, report.CodeVersion }));
                                        throw new InvalidOperationException("intents");
                                    }
                                }
                                var style = options.Bot == "Habit" && response == Side.Player ? BotStyle.Habit : options.Bot == "Random" && response == Side.Player ? BotStyle.Random : BotStyle.Balanced;
                                foreach (var c in BoardPlanner.Choose(sim.Board(response), style, random).Commands) { if (sim.Board(response).Phase == BoardPhase.Finished) break; sim.ApplyBoardCommand(response, c); }
                                if (sim.Board(response).Phase != BoardPhase.Finished && !sim.EndBoardResponse(response)) throw new InvalidOperationException("response");
                            }
                            if (!sim.CompleteBoard()) throw new InvalidOperationException("board completion"); match.AynaMind.Observe(sim.View(Side.Ayna));
                        }
                        match.AynaMind.Observe(sim.View(Side.Ayna));
                    }
                    row.Cycles = sim.Outcome.Cycle; row.End = sim.Outcome.Reason; row.AynaWon = sim.Outcome.Winner == Side.Ayna; row.PlayerWon = sim.Outcome.Winner == Side.Player; row.Readability = sim.View(Side.Player).Readability;
                    profile = match.CompleteProfile(); row.Generation = profile.Generation; report.Games.Add(row);
                    if (report.Games.Count % 5 == 0) File.WriteAllText(Path.Combine(options.Directory, "partial", runTag + ".json"), AynaJson.Write(new { Report = report, LastProfile = profile, Series = series, Game = game, Complete = false }));
                    yield return report.Games.Count;
                }
            }
            watch.Stop(); report.Seconds = watch.Elapsed.TotalSeconds;
            report.FirstWinRate = report.Games.Where(g => g.Game == 0).Average(g => g.AynaWon ? 1 : 0); report.LastWinRate = report.Games.Where(g => g.Game == options.Games - 1).Average(g => g.AynaWon ? 1 : 0); report.Gain = report.LastWinRate - report.FirstWinRate;
            var late = report.Games.Where(g => g.Game >= 2).ToArray(); report.Top1FromThird = late.Sum(g => g.Hits) / (double)Math.Max(1, late.Sum(g => g.Decisions));
            report.SurpriseRate = report.Games.Sum(g => g.SurpriseCycles) / (double)Math.Max(1, report.Games.Sum(g => g.Decisions)); report.PlayerWinRate = report.Games.Average(g => g.PlayerWon ? 1 : 0);
            var sorted = timings.OrderBy(x => x).ToArray(); report.PlannerMedianMs = sorted[sorted.Length / 2]; report.PlannerP95Ms = sorted[(int)(sorted.Length * .95)];
            if (options.Learning != LearningLevel.Off && options.Bot == "Habit")
            { if (report.Gain < .15) report.Failures.Add("Habit kazanma artışı 15 puan altında"); if (report.Top1FromThird < .6) report.Failures.Add("Habit top-1 %60 altında"); if (report.SurpriseRate >= .05) report.Failures.Add("Habit Sürpriz %5 altında değil"); }
            if (options.Learning != LearningLevel.Off && options.Bot == "Random" && Math.Abs(report.Gain) > .05) report.Failures.Add("Random kazanma farkı ±5 puan dışında");
            if (options.Bot == "Surprise" && report.SurpriseRate < .3) report.Failures.Add("SurpriseBot Sürpriz %30 altında");
            if (report.PlannerMedianMs > 50) report.Failures.Add("Makro plan p50 50 ms üstünde");
            System.IO.Directory.CreateDirectory(options.Directory); string path = Path.Combine(options.Directory, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + "_" + options.Bot + "_" + options.Learning);
            File.WriteAllText(path + ".json", AynaJson.Write(report));
            if (diagnostics.Count > 0) { System.IO.Directory.CreateDirectory(Path.Combine(options.Directory, "diagnostics")); File.WriteAllText(Path.Combine(options.Directory, "diagnostics", runTag + ".json"), AynaJson.Write(diagnostics)); }
            var md = new StringBuilder("# AYNA · F3 öğrenme ölçümü\n\n"); md.AppendLine(options.Bot + ", " + options.Series + " seri × " + options.Games + " oyun; " + options.Learning + "; taktik=" + options.Tactical);
            md.AppendLine(FormattableString.Invariant($"\nSüre {report.Seconds:F2} sn; ilk/son kazanma {report.FirstWinRate:P1}/{report.LastWinRate:P1}; fark {report.Gain:P1}; top-1 {report.Top1FromThird:P1}; Sürpriz {report.SurpriseRate:P1}; oyuncu kazanma {report.PlayerWinRate:P1}; plan p50/p95 {report.PlannerMedianMs:F2}/{report.PlannerP95Ms:F2} ms."));
            md.AppendLine("\nKazanma paydası bütün oyunlar; top-1 en güçlü tür×hedef tahmininin en az bir geçerli emre isabetidir. Sürpriz paydası en az bir geçerli, Bekle dışı emri olan döngüler. Eş tohumlu öğrenmesiz kontrol ayrı koşulur; canlı oyuncu profili kullanılmaz.");
            foreach (string failure in report.Failures) md.AppendLine("\n- " + failure);
            File.WriteAllText(path + ".md", md.ToString()); output = path;
        }
    }
}
