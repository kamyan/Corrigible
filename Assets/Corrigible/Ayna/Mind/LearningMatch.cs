using System;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind.Bots;

namespace Corrigible.Ayna.Mind
{
    // F4 denetleyicisi bu saf oturumu kullanabilir; disk veya Unity yaşam döngüsü içermez.
    public sealed class LearningMatch
    {
        public string MatchId { get; }
        public AynaSimulation Simulation { get; }
        public MindSession AynaMind { get; }
        public MindSession PlayerMind { get; }
        public LearningMatch(StartSetup setup, string matchId, AynaProfileData profile = null, AynaBalance balance = null, AynaProfileData playerProfile = null, bool chooseOpening = true)
        {
            if (string.IsNullOrEmpty(matchId)) throw new ArgumentException("match id");
            MatchId = matchId; var start = setup.Copy(); var b = (balance ?? new AynaBalance()).Copy();
            AynaMind = new MindSession(start.Seed ^ 0x2468ACE0u, profile, start.Settings.Learning);
            if (playerProfile != null) PlayerMind = new MindSession(start.Seed ^ 0x13579BDFu, playerProfile, start.Settings.Learning);
            if (chooseOpening)
            {
                start.Ayna = OpeningPolicy.Choose(AynaMind, start.Settings.Learning); start.AynaCounters = OpeningPolicy.Counters(AynaMind, b);
                if (PlayerMind != null) { start.Player = OpeningPolicy.Choose(PlayerMind, start.Settings.Learning); start.PlayerCounters = OpeningPolicy.Counters(PlayerMind, b); }
            }
            Simulation = AynaSimulation.Create(start, b, AynaMind.ProfileAtStart);
        }
        LearningMatch(string id, AynaSimulation sim, MindSession ayna, MindSession player)
        { MatchId = id; Simulation = sim; AynaMind = ayna; PlayerMind = player; }
        public void PrepareCycle()
        {
            if (Simulation.IsResolving || Simulation.Outcome.Finished) throw new InvalidOperationException("planning phase");
            AynaMind.Observe(Simulation.View(Side.Ayna));
            if (Simulation.View(Side.Player).IncomingForecast.Count == 0 && !Simulation.LockForecast(Side.Ayna, AynaMind.Predict(Simulation.View(Side.Ayna)))) throw new InvalidOperationException("forecast");
            if (PlayerMind != null)
            {
                PlayerMind.Observe(Simulation.View(Side.Player));
                if (Simulation.View(Side.Ayna).IncomingForecast.Count == 0 && !Simulation.LockForecast(Side.Player, PlayerMind.Predict(Simulation.View(Side.Player)))) throw new InvalidOperationException("forecast");
            }
        }
        public Plan MakeAynaPlan() => MacroPlanner.Choose(Simulation.View(Side.Ayna), AynaMind);
        public void Begin(Plan player)
        { PrepareCycle(); Simulation.BeginCycle(player, MakeAynaPlan()); }
        public void ResolveAuto(Plan player)
        { Begin(player); while (Simulation.IsResolving) Simulation.ResolvePendingAuto(); AynaMind.Observe(Simulation.View(Side.Ayna)); PlayerMind?.Observe(Simulation.View(Side.Player)); }
        public AynaProfileData CompleteProfile() => AynaMind.Complete(Simulation.View(Side.Ayna, true), Simulation.Record.Setup.Player, MatchId);
        public string Save() => AynaJson.Write(new LearningSave { MatchId = MatchId, Record = Simulation.Record, AynaMind = AynaMind.Checkpoint(), PlayerMind = PlayerMind?.Checkpoint() });
        public static LearningMatch Restore(string json)
        {
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<LearningSave>(json, new Newtonsoft.Json.JsonSerializerSettings { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None, MaxDepth = 64 });
            if (data == null || data.Version != 1 || string.IsNullOrEmpty(data.MatchId) || data.Record == null || data.AynaMind == null) throw new ArgumentException("learning save");
            var sim = AynaReplay.Restore(data.Record);
            if (data.AynaMind.Level != data.Record.Setup.Settings.Learning || data.PlayerMind != null && data.PlayerMind.Level != data.Record.Setup.Settings.Learning || data.AynaMind.Completed && !sim.Outcome.Finished || data.Record.ProfileAtStart == null || AynaJson.Write(data.Record.ProfileAtStart.Copy()) != AynaJson.Write(data.AynaMind.Prior?.Copy())) throw new ArgumentException("learning save mismatch");
            return new LearningMatch(data.MatchId, sim, MindSession.Restore(data.AynaMind), data.PlayerMind == null ? null : MindSession.Restore(data.PlayerMind));
        }
    }
}
