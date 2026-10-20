using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Corrigible.Ayna.Core
{
    [Serializable]
    public sealed class CycleRecord
    {
        public int Cycle;
        public Plan Player, Ayna;
    }
    [Serializable]
    public sealed class AynaRecord
    {
        public int FormatVersion = 4, RulesVersion = 6, MindVersion = 6, WorldDataVersion = 1;
        public StartSetup Setup;
        public AynaBalance Balance;
        public AynaProfileData ProfileAtStart;
        public List<AynaInput> Inputs = new List<AynaInput>();
        public List<CycleRecord> Cycles = new List<CycleRecord>();
        public AynaRecord Copy() => AynaJson.ReadRecord(AynaJson.Write(this));
    }
    public static class AynaJson
    {
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, FloatParseHandling = FloatParseHandling.Double, MaxDepth = 64 };
        public static string Write(object value) => JsonConvert.SerializeObject(value, Settings);
        public static AynaProfileData ReadProfile(string json)
        {
            var p = JsonConvert.DeserializeObject<AynaProfileData>(json, Settings);
            if (p == null || (p.Version < 1 || p.Version > 3) || p.Architectures == null || p.Architectures.Length != 3 || p.OrderCounts == null || p.OrderCounts.Length != 13 || p.RegionCounts == null || p.RegionCounts.Length != 12)
                throw new ArgumentException("profile version/schema");
            if (p.Version < 3)
            {
                if (p.PolicyMatches?.Length == 5) Array.Resize(ref p.PolicyMatches, 6);
                if (p.PolicyTrials?.Length == 5) Array.Resize(ref p.PolicyTrials, 6);
                p.Version = 3;
            }
            p.Validate(); return p;
        }
        public static AynaRecord ReadRecord(string json)
        {
            var r = JsonConvert.DeserializeObject<AynaRecord>(json, Settings);
            if (r == null || (r.FormatVersion < 1 || r.FormatVersion > 4) || (r.RulesVersion < 1 || r.RulesVersion > 6) || (r.MindVersion < 1 || r.MindVersion > 6) || r.WorldDataVersion != 1 || r.Setup == null || r.Balance == null || r.Cycles == null || r.FormatVersion >= 2 && (r.Inputs == null || r.Inputs.Count == 0 && r.Cycles.Count > 0))
                throw new ArgumentException("record version/schema");
            if (r.RulesVersion < 4) r.Balance.SurprisePolicy = 0;
            return r;
        }
    }
    public static class AynaReplay
    {
        public static AynaSimulation Restore(AynaRecord rec)
        {
            if (rec == null) throw new ArgumentNullException(nameof(rec));
            var safe = rec.Copy(); var sim = AynaSimulation.Create(safe.Setup, safe.Balance, safe.ProfileAtStart);
            if (safe.FormatVersion >= 2 && safe.Inputs != null && safe.Inputs.Count > 0)
            {
                foreach (var input in safe.Inputs)
                {
                    if (input == null) throw new ArgumentException("input");
                    bool ok;
                    switch (input.Kind)
                    {
                        case "forecast": ok = sim.LockForecast(input.Side, input.Predictions); break;
                        case "begin": sim.BeginCycle(input.Player, input.Ayna); ok = true; break;
                        case "auto": ok = sim.ResolvePendingAuto(); break;
                        case "board": ok = sim.StartBoard(input.PlayerRoster, input.AynaRoster, input.IntentSide, input.FirstStrikeSide); break;
                        case "traps": ok = sim.PlaceBoardTraps(input.Cells); break;
                        case "intents": ok = sim.SetBoardIntents(input.Intents); break;
                        case "command": ok = sim.ApplyBoardCommand(input.Side, input.Command); break;
                        case "undo": ok = sim.UndoBoard(input.Side); break;
                        case "end-response": ok = sim.EndBoardResponse(input.Side); break;
                        case "retreat": ok = sim.RetreatBoard(input.Side); break;
                        case "complete-board": ok = sim.CompleteBoard(); break;
                        default: throw new ArgumentException("input kind");
                    }
                    if (!ok) throw new ArgumentException("input rejected");
                }
                if (AynaJson.Write(safe.Cycles) != AynaJson.Write(sim.Record.Cycles)) throw new ArgumentException("cycle/input mismatch");
                return sim;
            }
            foreach (var c in safe.Cycles)
            {
                if (c == null || c.Cycle != sim.View(Side.Player).Cycle || c.Player == null || c.Ayna == null || sim.Outcome.Finished) throw new ArgumentException("cycle sequence");
                sim.ResolveCycle(c.Player, c.Ayna);
            }
            return sim;
        }
    }
}
