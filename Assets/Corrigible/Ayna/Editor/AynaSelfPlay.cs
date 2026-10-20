using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind.Bots;
using Corrigible.Ayna.Mind;
using Corrigible.Core;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Corrigible.Ayna.EditorTools
{
    [Serializable]
    public sealed class SelfPlayOptions
    {
        public int Games = 500;
        public uint Seed = 1901026;
        public string PlayerBot = "Mind", AynaBot = "Mind";
        public string OutputDirectory = "Docs/AYNA/denge/F1";
        public bool Tactical;
        public AynaBalance Balance = new AynaBalance();
    }
    [Serializable]
    public sealed class SelfPlayGame
    {
        public int Index, Cycles, Battles, PlayerServers, AynaServers, CoreBreaks;
        public uint Seed;
        public Architecture PlayerArchitecture, AynaArchitecture;
        public string PlayerStart, AynaStart;
        public Outcome Outcome;
    }
    [Serializable]
    public sealed class MatchupSummary
    {
        public string Match;
        public int Games, Wins, Losses, Draws;
        public double PlayerScoreRate;
        public double PlayerWinRate, WilsonLow, WilsonHigh;
    }
    [Serializable]
    public sealed class SelfPlayReport
    {
        public string Run, CodeVersion;
        public SelfPlayOptions Options;
        public double Seconds, MedianCycles, BeforeLimitRate, AverageBattles, PlannerMedianMs, PlannerP95Ms, PlayerWinRate;
        public List<SelfPlayGame> Games = new List<SelfPlayGame>();
        public List<MatchupSummary> Matchups = new List<MatchupSummary>();
        public List<string> Failures = new List<string>();
    }
    public static class AynaSelfPlay
    {
        [MenuItem("Corrigible/AYNA/Kendi kendine oynat")]
        public static void MenuRun() => Start(new SelfPlayOptions());
        [MenuItem("Corrigible/AYNA/Tahtayla kendi kendine oynat")]
        public static void MenuTactical() => Start(new SelfPlayOptions { Games = 90, Tactical = true, OutputDirectory = "Docs/AYNA/denge/F2/world_menu" });

        public static void AssertEditorSafe()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed) throw new InvalidOperationException("Editör Play/derleme durumunda.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Kaydedilmemiş sahne var; otomatik kaydetme veya atma yapılmadı.");
        }
        public static string RunDefault(int games, string outputDirectory) => Run(new SelfPlayOptions { Games = games, OutputDirectory = outputDirectory });
        public static string Run(SelfPlayOptions options)
        {
            if (job != null) throw new InvalidOperationException("Bir AYNA koşusu zaten çalışıyor.");
            string result = null;
            foreach (var step in RunSteps(options, value => result = value)) { }
            return result;
        }
        static IEnumerator<int> job;
        static int completed, requested;
        static string jobResult, jobError;
        public static string Start(SelfPlayOptions options)
        {
            AssertEditorSafe();
            if (job != null) throw new InvalidOperationException("Bir AYNA koşusu zaten çalışıyor.");
            if (options == null || options.Games < 1 || options.Balance == null) throw new ArgumentException(nameof(options));
            options.Balance.Validate();
            var copy = new SelfPlayOptions { Games = options.Games, Seed = options.Seed, PlayerBot = options.PlayerBot, AynaBot = options.AynaBot,
                OutputDirectory = options.OutputDirectory, Balance = options.Balance.Copy() };
            completed = 0; requested = copy.Games; jobResult = jobError = null;
            job = RunSteps(copy, value => jobResult = value).GetEnumerator();
            EditorApplication.update += Tick;
            return Status();
        }
        public static string Status() => AynaJson.Write(new { Running = job != null, Completed = completed, Requested = requested, Result = jobResult, Error = jobError });
        public static void Cancel()
        {
            if (job == null) return;
            EditorApplication.update -= Tick;
            job?.Dispose(); job = null; jobError = "Koşu iptal edildi; profil veya sahne değiştirilmedi.";
        }
        static void Tick()
        {
            try
            {
                AssertEditorSafe();
                if (job.MoveNext()) completed = job.Current;
                else { EditorApplication.update -= Tick; job.Dispose(); job = null; }
            }
            catch (Exception ex)
            {
                EditorApplication.update -= Tick; job?.Dispose(); job = null; jobError = ex.Message;
            }
        }
        static IEnumerable<int> RunSteps(SelfPlayOptions options, Action<string> finish)
        {
            AssertEditorSafe();
            if (options == null || options.Games < 1) throw new ArgumentException(nameof(options));
            options = new SelfPlayOptions { Games = options.Games, Seed = options.Seed, PlayerBot = options.PlayerBot, AynaBot = options.AynaBot, Tactical = options.Tactical, OutputDirectory = options.OutputDirectory, Balance = options.Balance.Copy() };
            var b = options.Balance.Copy(); b.TacticalPlanning = options.Tactical; b.Validate(); options.Balance = b.Copy();
            string sources = string.Join("\n", Directory.GetFiles("Assets/Corrigible/Ayna", "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal).Select(File.ReadAllText));
            string sourceHash;
            using (var hash = SHA256.Create()) sourceHash = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(sources))).Replace("-", "").ToLowerInvariant();
            var report = new SelfPlayReport { Run = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture), CodeVersion = "AYNA-F2/" + sourceHash, Options = options };
            var timings = new List<double>(); var total = Stopwatch.StartNew();
            for (int g = 0; g < options.Games; g++)
            {
                uint seed = unchecked(options.Seed + (uint)(g / (options.Tactical ? 18 : 9)));
                var random = new Rng(seed); var pArch = (Architecture)((g % 9) / 3); var aArch = (Architecture)(g % 3);
                var pairs = AynaWorldData.Regions.SelectMany(p => AynaWorldData.Regions.Where(a => AynaWorldData.Distance(p.Id, a.Id) >= 2).Select(a => (P: p.Id, A: a.Id))).ToArray();
                var pair = pairs[(int)Math.Floor(random.Next() * pairs.Length)];
                if ((g / 9) % 2 == 1) pair = (pair.A, pair.P);
                var setup = new StartSetup { Seed = seed, Player = new StartChoice { Architecture = pArch, Region = pair.P }, Ayna = new StartChoice { Architecture = aArch, Region = pair.A }, Settings = new AynaSettings { Learning = LearningLevel.Off } };
                var sim = AynaSimulation.Create(setup, b);
                var pbot = Bot(options.PlayerBot, seed ^ 0x13579BDFu); var abot = Bot(options.AynaBot, seed ^ 0x2468ACE0u);
                int battles = 0, coreBreaks = 0;
                while (!sim.Outcome.Finished)
                {
                    var pview = sim.View(Side.Player); var aview = sim.View(Side.Ayna);
                    var sw = Stopwatch.StartNew(); var pp = pbot.MakePlan(pview); sw.Stop(); if (pbot.Id == "Mind") timings.Add(sw.Elapsed.TotalMilliseconds);
                    sw.Restart(); var ap = abot.MakePlan(aview); sw.Stop(); if (abot.Id == "Mind") timings.Add(sw.Elapsed.TotalMilliseconds);
                    if (!options.Tactical) sim.ResolveCycle(pp, ap);
                    else
                    {
                        sim.BeginCycle(pp, ap);
                        while (sim.IsResolving)
                        {
                            if (!sim.StartBoard(intentSide: (g / 9) % 2 == 0 ? Side.Ayna : Side.Player)) throw new InvalidOperationException("board start");
                            coreBreaks += PlayBoard(sim, seed ^ (uint)g);
                        }
                    }
                    battles += sim.View(Side.Player).Battles.Concat(sim.View(Side.Ayna).Battles).Select(x => x.Region).Distinct().Count();
                }
                report.Games.Add(new SelfPlayGame { Index = g, Seed = seed, Cycles = sim.Outcome.Cycle, Outcome = sim.Outcome, Battles = battles, CoreBreaks = coreBreaks,
                    PlayerArchitecture = pArch, AynaArchitecture = aArch, PlayerStart = pair.P, AynaStart = pair.A,
                    PlayerServers = sim.View(Side.Player).OwnServers, AynaServers = sim.View(Side.Ayna).OwnServers });
                yield return g + 1;
            }
            total.Stop(); report.Seconds = total.Elapsed.TotalSeconds;
            Complete(report, timings); Directory.CreateDirectory(options.OutputDirectory);
            var prefix = Path.Combine(options.OutputDirectory, report.Run + "_" + options.PlayerBot + "_" + options.AynaBot + "_" + options.Games);
            File.WriteAllText(prefix + ".json", AynaJson.Write(report)); File.WriteAllText(prefix + ".md", Markdown(report));
            finish(AynaJson.Write(new { report.Run, report.Seconds, report.MedianCycles, report.BeforeLimitRate, report.AverageBattles, report.PlannerMedianMs, report.PlannerP95Ms, report.PlayerWinRate, report.Failures, json = prefix + ".json", markdown = prefix + ".md" }));
        }

        static int PlayBoard(AynaSimulation sim, uint seed)
        {
            var first = sim.Board(Side.Player); var intent = first.IntentSide; var response = intent.Other();
            var rng = new Rng(seed); sim.PlaceBoardTraps(TrapPlanner.Choose(sim.Board(intent), rng));
            int guard = 0;
            while (sim.Board(response).Phase != BoardPhase.Finished && guard++ < 20)
            {
                if (sim.Board(intent).Phase == BoardPhase.Intents)
                {
                    var plan = BoardPlanner.Choose(sim.Board(intent));
                    if (!sim.SetBoardIntents(plan)) throw new InvalidOperationException("board intents");
                }
                foreach (var c in BoardPlanner.Choose(sim.Board(response)).Commands)
                {
                    if (sim.Board(response).Phase == BoardPhase.Finished) break;
                    // Gerçek gizli tuzak, görünümde üretilen planın kalan kısmını geçersizleştirebilir.
                    sim.ApplyBoardCommand(response, c);
                }
                if (sim.Board(response).Phase != BoardPhase.Finished && !sim.EndBoardResponse(response)) throw new InvalidOperationException("board response");
            }
            int broken = sim.Board(response).Result.CoreBroken.Count(x => x);
            if (!sim.CompleteBoard()) throw new InvalidOperationException("board result"); return broken;
        }
        public static IPlanBot Bot(string id, uint seed)
        {
            switch (id)
            {
                case "Mind": return new MindBot(seed);
                case "Habit": return new HabitBot();
                case "Random": return new RandomBot(seed);
                case "Greedy": return new GreedyBot();
                case "Aggro": return new AggroBot();
                case "Stealth": return new StealthBot();
                case "Surprise": return new SurpriseBot();
                default: throw new ArgumentException(id);
            }
        }
        static double Quantile(IEnumerable<double> source, double q)
        {
            var values = source.OrderBy(x => x).ToArray(); if (values.Length == 0) return 0;
            double pos = (values.Length - 1) * q; int lo = (int)Math.Floor(pos), hi = (int)Math.Ceiling(pos);
            return values[lo] + (values[hi] - values[lo]) * (pos - lo);
        }
        static void Complete(SelfPlayReport r, List<double> times)
        {
            int n = r.Games.Count;
            r.MedianCycles = Quantile(r.Games.Select(x => (double)x.Cycles), .5); r.AverageBattles = r.Games.Average(x => x.Battles);
            r.BeforeLimitRate = r.Games.Count(x => x.Cycles < r.Options.Balance.MaxCycles) / (double)n;
            r.PlayerWinRate = r.Games.Count(x => x.Outcome.Winner == Side.Player) / (double)n;
            r.PlannerMedianMs = Quantile(times, .5); r.PlannerP95Ms = Quantile(times, .95);
            foreach (var group in r.Games.GroupBy(x => x.PlayerArchitecture + "/" + x.AynaArchitecture).OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                int count = group.Count(), wins = group.Count(x => x.Outcome.Winner == Side.Player), losses = group.Count(x => x.Outcome.Winner == Side.Ayna);
                double p = wins / (double)count, z = 1.959963984540054, center = (p + z * z / (2 * count)) / (1 + z * z / count);
                double half = z * Math.Sqrt(p * (1 - p) / count + z * z / (4 * count * count)) / (1 + z * z / count);
                r.Matchups.Add(new MatchupSummary { Match = group.Key, Games = count, Wins = wins, Losses = losses, Draws = count - wins - losses, PlayerScoreRate = (wins + (count - wins - losses) * .5) / count, PlayerWinRate = p, WilsonLow = center - half, WilsonHigh = center + half });
            }
            bool self = r.Options.PlayerBot == "Mind" && r.Options.AynaBot == "Mind";
            if (self)
            {
                if (r.Options.Games >= 500 && r.Seconds >= 300) r.Failures.Add("500 oyun: 5 dakika aşıldı");
                if (r.MedianCycles < 25 || r.MedianCycles > 35) r.Failures.Add("Ortanca döngü 25–35 dışında");
                if (r.BeforeLimitRate < .9) r.Failures.Add("40'tan önce bitiş %90 altında");
                if (r.AverageBattles < 6 || r.AverageBattles > 10) r.Failures.Add("Ortalama savaş 6–10 dışında");
                foreach (var m in r.Matchups)
                {
                    var names = m.Match.Split('/');
                    if (names[0] == names[1] && (m.PlayerScoreRate < .45 || m.PlayerScoreRate > .55)) r.Failures.Add(m.Match + ": aynı mimari %45–55 dışında");
                    if (m.PlayerWinRate > .65 || m.Losses / (double)m.Games > .65) r.Failures.Add(m.Match + ": taraflardan biri %65 üstünde");
                }
                foreach (var reason in (r.Options.Tactical ? new[] { EndReason.Erasure, EndReason.Singularity, EndReason.Dominance } : new[] { EndReason.Singularity, EndReason.Dominance }))
                    if (r.Games.Count(x => x.Outcome.Reason == reason) / (double)n < .1) r.Failures.Add(reason + ": sonuç oranı %10 altında");
                double shutdown = r.Games.Count(x => x.Outcome.Reason == EndReason.Shutdown) / (double)n;
                if (shutdown > .15) r.Failures.Add("Son Şalter %15 üstünde");
            }
            else if (r.Options.PlayerBot == "Mind" && r.Options.AynaBot == "Random" && r.PlayerWinRate < .9) r.Failures.Add("Random'a karşı %90 altında");
            else if (r.Options.PlayerBot == "Mind" && r.Options.AynaBot == "Greedy" && r.PlayerWinRate < .6) r.Failures.Add("Greedy'ye karşı %60 altında");
            if (r.PlannerMedianMs > 50) r.Failures.Add("Planlayıcı p50 50 ms üstünde");
        }
        static string Markdown(SelfPlayReport r)
        {
            var sb = new StringBuilder(); sb.AppendLine("# AYNA · F1 kendi kendine oynatma"); sb.AppendLine();
            sb.AppendLine("Koşu: " + r.Run + " · Kod: " + r.CodeVersion + " · " + r.Options.PlayerBot + " / " + r.Options.AynaBot); sb.AppendLine();
            sb.AppendLine("Bütün oranlarda payda tüm oyunlardır; beraberlik/ortak yenilgi kazanma sayılmaz. Kalıcı oyuncu profili kullanılmadı. Bu koşuda öğrenme yoktur."); sb.AppendLine();
            sb.AppendLine("| Ölçüt | Ölçülen |"); sb.AppendLine("|---|---|");
            foreach (var kv in new[] { ("Oyun", (double)r.Games.Count), ("Saniye", r.Seconds), ("Ortanca döngü", r.MedianCycles), ("40'tan önce oran", r.BeforeLimitRate),
                ("Ortalama savaş", r.AverageBattles), ("Plan p50 ms", r.PlannerMedianMs), ("Plan p95 ms", r.PlannerP95Ms), ("Oyuncu kazanma", r.PlayerWinRate) })
                sb.AppendLine("| " + kv.Item1 + " | " + kv.Item2.ToString("0.####", CultureInfo.InvariantCulture) + " |");
            sb.AppendLine(); sb.AppendLine("| Mimari | n | Galibiyet | Yenilgi | Diğer | Kazanma | %95 Wilson |"); sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var m in r.Matchups) sb.AppendLine(FormattableString.Invariant($"| {m.Match} | {m.Games} | {m.Wins} | {m.Losses} | {m.Draws} | {m.PlayerWinRate:P1} | {m.WilsonLow:P1}–{m.WilsonHigh:P1} |"));
            sb.AppendLine(); sb.AppendLine("| Bitiş | Adet | Oran |"); sb.AppendLine("|---|---|---|");
            foreach (var g in r.Games.GroupBy(x => x.Outcome.Reason).OrderBy(x => x.Key)) sb.AppendLine(FormattableString.Invariant($"| {g.Key} | {g.Count()} | {g.Count() / (double)r.Games.Count:P1} |"));
            sb.AppendLine(); sb.AppendLine("## Geçmeyen kabul ölçütleri"); sb.AppendLine();
            if (r.Failures.Count == 0) sb.AppendLine("Bu koşunun uygulanan eşikleri geçti."); else foreach (var failure in r.Failures) sb.AppendLine("- " + failure);
            return sb.ToString();
        }
    }
}
