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
    [Serializable] public sealed class BalanceBenchOptions
    {
        public string Bot = "Mind", Directory = "Docs/AYNA/denge/F5/before", Runtime = "Unity Editor";
        public int Seeds = 8, SeedOffset;
        public uint Seed = 5102026;
        public bool Tactical;
        public LearningLevel Learning = LearningLevel.Full;
        public AynaBalance Balance = new AynaBalance();
    }
    [Serializable] public sealed class BalanceGame
    {
        public int Sample, Mirror, Role, Cycles, Battles, CoreBreaks, Decisions, Hits, Surprises;
        public uint Seed;
        public Architecture PlayerArchitecture, AynaArchitecture;
        public string PlayerStart, AynaStart, FinalHash;
        public Outcome Outcome;
        public int PlayerServers, AynaServers;
        public double Awareness;
    }
    [Serializable] public sealed class BalanceBenchReport
    {
        public string CodeVersion, BalanceHash;
        public BalanceBenchOptions Options;
        public List<BalanceGame> Games = new List<BalanceGame>();
        public double Seconds, MacroMedianMs, MacroP95Ms, BoardMedianMs, BoardP95Ms;
    }
    public static class AynaBalanceBench
    {
        static IEnumerator<int> job;
        static string output, error;
        static int completed;
        public static string Status() => AynaJson.Write(new { Running = job != null, Completed = completed, Output = output, Error = error });
        public static string Start(BalanceBenchOptions options)
        {
            AynaSelfPlay.AssertEditorSafe(); if (job != null) throw new InvalidOperationException("balance benchmark busy");
            completed = 0; output = error = null; job = Steps(options, p => output = p).GetEnumerator();
            EditorApplication.update += Tick; return Status();
        }
        public static void Cancel() { EditorApplication.update -= Tick; job?.Dispose(); job = null; }
        static void Tick()
        {
            try { AynaSelfPlay.AssertEditorSafe(); for (int i = 0; i < 4 && job != null; i++) if (job.MoveNext()) completed = job.Current; else Cancel(); }
            catch (Exception ex) { error = ex.ToString(); Cancel(); }
        }
        public static string Run(BalanceBenchOptions options)
        { string path = null; foreach (var step in Steps(options, p => path = p)) { } return path; }
        public static StartSetup Setup(BalanceBenchOptions o, int sample, int mirror, Architecture player, Architecture ayna)
        {
            var rng = new Rng(o.Seed + (uint)sample * 101);
            var pairs = AynaWorldData.Regions.SelectMany(p => AynaWorldData.Regions.Where(a => AynaWorldData.Distance(p.Id, a.Id) >= 2).Select(a => (P: p.Id, A: a.Id))).ToArray();
            var pair = pairs[(int)Math.Floor(rng.Next() * pairs.Length)]; if (mirror == 1) pair = (pair.A, pair.P);
            return new StartSetup { Seed = o.Seed + (uint)sample * 101, Player = new StartChoice { Architecture = player, Region = pair.P }, Ayna = new StartChoice { Architecture = ayna, Region = pair.A }, Settings = new AynaSettings { Learning = o.Learning } };
        }
        static string Hash(string text)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant(); }
        public static string SourceHash() => Hash(string.Join("\n", System.IO.Directory.GetFiles("Assets/Corrigible/Ayna", "*.cs", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal).Select(File.ReadAllText)));
        static double Quantile(List<double> values, double q) { if (values.Count == 0) return 0; var a = values.OrderBy(x => x).ToArray(); return a[(int)Math.Floor((a.Length - 1) * q)]; }
        static IEnumerable<int> Steps(BalanceBenchOptions options, Action<string> finish)
        {
            AynaSelfPlay.AssertEditorSafe();
            if (options == null || options.Seeds < 1 || options.SeedOffset < 0) throw new ArgumentException("benchmark options");
            options = Newtonsoft.Json.JsonConvert.DeserializeObject<BalanceBenchOptions>(AynaJson.Write(options));
            options.Balance.TacticalPlanning = options.Tactical; options.Balance.Validate();
            // Mimari seçimi sabit; öğrenme bu oyunun görünür geçmişiyle sınırlıdır. Disk profili yoktur.
            var report = new BalanceBenchReport { CodeVersion = SourceHash(), Options = options, BalanceHash = Hash(AynaJson.Write(options.Balance)) };
            var macroTimes = new List<double>(); var boardTimes = new List<double>(); var clock = Stopwatch.StartNew();
            System.IO.Directory.CreateDirectory(options.Directory);
            string path = Path.Combine(options.Directory, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + "_" + options.Bot + (options.Tactical ? "_tactical" : "_auto"));
            for (int sample = options.SeedOffset; sample < options.SeedOffset + options.Seeds; sample++)
                for (int pa = 0; pa < 3; pa++) for (int aa = 0; aa < 3; aa++) for (int mirror = 0; mirror < 2; mirror++) for (int role = 0; role < (options.Tactical ? 2 : 1); role++)
                {
                    var setup = Setup(options, sample, mirror, (Architecture)pa, (Architecture)aa);
                    var match = new LearningMatch(setup, "balance:" + options.Bot + ":" + sample + ":" + pa + ":" + aa + ":" + mirror + ":" + role, new AynaProfileData(), options.Balance, options.Bot == "Mind" ? new AynaProfileData() : null, chooseOpening: false);
                    var sim = match.Simulation; var bot = AynaSelfPlay.Bot(options.Bot, setup.Seed ^ 0x13579BDFu); var random = new Rng(setup.Seed ^ 0xB04Du);
                    var row = new BalanceGame { Sample = sample, Seed = setup.Seed, Mirror = mirror, Role = options.Tactical ? role : -1, PlayerArchitecture = (Architecture)pa, AynaArchitecture = (Architecture)aa, PlayerStart = setup.Player.Region, AynaStart = setup.Ayna.Region };
                    while (!sim.Outcome.Finished)
                    {
                        AynaSelfPlay.AssertEditorSafe(); var watch = Stopwatch.StartNew(); match.PrepareCycle(); watch.Stop(); double prepareMs = watch.Elapsed.TotalMilliseconds;
                        var before = sim.View(Side.Player); var top = before.IncomingForecast.OrderByDescending(p => p.Probability).ThenBy(p => p.Kind).ThenBy(p => p.Region, StringComparer.Ordinal).FirstOrDefault();
                        var player = options.Bot == "Mind" ? MacroPlanner.Choose(before, match.PlayerMind) : bot.MakePlan(before);
                        watch.Restart(); var ayna = match.MakeAynaPlan(); watch.Stop(); macroTimes.Add(prepareMs + watch.Elapsed.TotalMilliseconds);
                        sim.BeginCycle(player, ayna);
                        var actual = sim.View(Side.Player).Receipts.Where(r => r.Kind != OrderKind.Wait && (r.Rejection == RejectReason.None || r.Rejection == RejectReason.LostCondition)).ToArray();
                        if (actual.Length > 0) { row.Decisions++; if (sim.View(Side.Player).LastSurprise) row.Surprises++; if (top != null && actual.Any(r => r.Kind == top.Kind && (r.Region ?? before.Armies.FirstOrDefault(a => a.Id == player.Orders[r.Index].ArmyId)?.Region) == top.Region)) row.Hits++; }
                        while (sim.IsResolving)
                        {
                            if (!options.Tactical) { if (!sim.ResolvePendingAuto()) throw new InvalidOperationException("auto"); continue; }
                            if (!sim.StartBoard(intentSide: role == 0 ? Side.Ayna : Side.Player)) throw new InvalidOperationException("board");
                            var intent = sim.Board(Side.Player).IntentSide; var response = intent.Other();
                            var mind = intent == Side.Ayna ? match.AynaMind : match.PlayerMind;
                            var heat = mind?.Heat(sim.Board(intent));
                            if (!sim.PlaceBoardTraps(TrapPlanner.Choose(sim.Board(intent), mind?.Rng ?? random, heat))) throw new InvalidOperationException("traps");
                            int guard = 0;
                            while (sim.Board(response).Phase != BoardPhase.Finished && guard++ < options.Balance.BoardTurns * 2 + 2)
                            {
                                if (sim.Board(intent).Phase == BoardPhase.Intents)
                                {
                                    watch.Restart(); var plan = BoardPlanner.Choose(sim.Board(intent), responseHeat: heat); watch.Stop(); boardTimes.Add(watch.Elapsed.TotalMilliseconds);
                                    if (!sim.SetBoardIntents(plan)) throw new InvalidOperationException("intents");
                                }
                                var style = response == Side.Player && options.Bot == "Habit" ? BotStyle.Habit : response == Side.Player && options.Bot == "Random" ? BotStyle.Random : BotStyle.Balanced;
                                watch.Restart(); var reply = BoardPlanner.Choose(sim.Board(response), style, random); watch.Stop(); boardTimes.Add(watch.Elapsed.TotalMilliseconds);
                                foreach (var c in reply.Commands) { if (sim.Board(response).Phase == BoardPhase.Finished) break; sim.ApplyBoardCommand(response, c); }
                                if (sim.Board(response).Phase != BoardPhase.Finished && !sim.EndBoardResponse(response)) throw new InvalidOperationException("response");
                            }
                            if (sim.Board(response).Phase != BoardPhase.Finished) throw new InvalidOperationException("board guard");
                            row.CoreBreaks += sim.Board(response).Result.CoreBroken.Count(x => x);
                            if (!sim.CompleteBoard()) throw new InvalidOperationException("complete board");
                            match.AynaMind.Observe(sim.View(Side.Ayna)); match.PlayerMind?.Observe(sim.View(Side.Player));
                        }
                        row.Battles += sim.View(Side.Player).Battles.Concat(sim.View(Side.Ayna).Battles).Select(b => b.Region).Distinct().Count();
                        match.AynaMind.Observe(sim.View(Side.Ayna)); match.PlayerMind?.Observe(sim.View(Side.Player));
                    }
                    row.Cycles = sim.Outcome.Cycle; row.Outcome = sim.Outcome; row.PlayerServers = sim.View(Side.Player).OwnServers; row.AynaServers = sim.View(Side.Ayna).OwnServers; row.Awareness = sim.View(Side.Player).Awareness; row.FinalHash = Hash(AynaJson.Write(sim.Record)); report.Games.Add(row);
                    File.WriteAllText(path + ".partial.json", AynaJson.Write(report)); yield return report.Games.Count;
                }
            clock.Stop(); report.Seconds = clock.Elapsed.TotalSeconds; report.MacroMedianMs = Quantile(macroTimes, .5); report.MacroP95Ms = Quantile(macroTimes, .95); report.BoardMedianMs = Quantile(boardTimes, .5); report.BoardP95Ms = Quantile(boardTimes, .95);
            File.WriteAllText(path + ".json", AynaJson.Write(report)); File.Delete(path + ".partial.json"); finish(path + ".json");
        }
    }
}
