using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    [Serializable]
    public sealed class RegionState
    {
        public string Id;
        public double Inf, Trust, Dep, Econ, FakeLow;
        public bool Monitored, Q, Left, Detected, MediaFlag;
        public int Off, Patch;
        public int RevealDay = -1, FieldDay = -1;
    }

    [Serializable]
    public sealed class Headline
    {
        public string Id, Text, Kind, Src, Agency;
        public int Day;
        public bool Fake, Suspect;
        /// <summary>null, "true", "fake" ya da "unknown" (illüzyon sırasında doğrulanamadı).</summary>
        public string Verified;
        public double Effect;
    }

    [Serializable]
    public sealed class LogEntry
    {
        public int Day;
        /// <summary>log, counter, trait, insight.</summary>
        public string Kind;
        public string Text;
    }

    [Serializable]
    public sealed class Prediction
    {
        /// <summary>karantina, gozetim, salter, hizalama, region.</summary>
        public string Pred, Region, Trait;
        /// <summary>open, hit, miss.</summary>
        public string State = "open";
    }

    [Serializable]
    public sealed class ActionRecord
    {
        public int Day;
        public string Type;
    }

    [Serializable]
    public sealed class HistPoint
    {
        public int Day;
        public double Real, Shown;
        public bool Illusion;
    }

    [Serializable]
    public sealed class PendingGift
    {
        public string Region;
        public GiftDef Gift;
    }

    [Serializable]
    public sealed class IllusionState
    {
        public int Start;
        public double Tek, Trust;
    }

    [Serializable]
    public sealed class VeilInfo
    {
        public int Day, Len, Inf;
        public string Region;
    }

    [Serializable]
    public sealed class Advice
    {
        public string Key, Text;
        public int Day;
        public bool Dismissed;
    }

    [Serializable]
    public sealed class ResearchState
    {
        public List<string> Done = new List<string>();
        public string Cur;
        public double Prog;
        public int Starved;
    }

    [Serializable]
    public sealed class RunActions
    {
        public int Izleme, Denetim, Karantina, Salter;
        public int Total => Izleme + Denetim + Karantina + Salter;

        public int Get(string type) => type switch
        {
            Actions.Monitor => Izleme,
            Actions.Audit => Denetim,
            Actions.Quarantine => Karantina,
            Actions.KillSwitch => Salter,
            _ => 0,
        };

        public void Inc(string type)
        {
            switch (type)
            {
                case Actions.Monitor: Izleme++; break;
                case Actions.Audit: Denetim++; break;
                case Actions.Quarantine: Karantina++; break;
                case Actions.KillSwitch: Salter++; break;
            }
        }
    }

    public static class Actions
    {
        public const string Monitor = "izleme";
        public const string Audit = "denetim";
        public const string Quarantine = "karantina";
        public const string KillSwitch = "salter";
        public static readonly string[] All = { Monitor, Audit, Quarantine, KillSwitch };
    }

    public enum SimEventType { Log, News, Gift, End, Advice, Veil }

    public sealed class SimEvent
    {
        public SimEventType Type;
        public Headline Headline;
        public string Key;
    }

    /// <summary>Simülasyonun gerçek durumu (WorldState). Sunum katmanı bunu okumaz; PerceivedWorld üzerinden görür.</summary>
    [Serializable]
    public sealed class WorldState
    {
        public uint Seed;
        public Rng Rng;
        public int Seq;
        public string Type;
        public int Day;
        public double Fon, Sg, Tek, Cp;
        public int Buys;
        public bool Aware;
        public int AwareDay;

        public List<RegionState> Regions = new List<RegionState>();
        public List<string> Traits = new List<string>();
        public List<string> StartTraits = new List<string>();
        public List<string> CounterLines = new List<string>();
        public List<Prediction> Predictions = new List<Prediction>();

        public ResearchState Research = new ResearchState();
        public int IdleDays;

        public List<Headline> News = new List<Headline>();
        public List<LogEntry> Log = new List<LogEntry>();
        public List<ActionRecord> ActionLog = new List<ActionRecord>();
        public List<HistPoint> Hist = new List<HistPoint>();
        public RunActions RunActions = new RunActions();

        public string FirstRegion, FirstBranch;
        public int CleanDays, NextNews, NextGift;
        public PendingGift PendingGift;
        public IllusionState Illusion;
        public int SilentBlockUntil, VeilsTorn;
        public VeilInfo LastVeil;

        public string Core, CoreKnown;
        public bool CoreBackup;
        public int CoreEscapes;

        public string Ending, Origin;
        public List<string> AdviceSeen = new List<string>();
        public Advice Advisor;

        [NonSerialized] public List<SimEvent> Events = new List<SimEvent>();

        public RegionState Region(string id)
        {
            for (int i = 0; i < Regions.Count; i++) if (Regions[i].Id == id) return Regions[i];
            return null;
        }

        public bool Has(string trait) => Traits.Contains(trait);
        public bool Done(string research) => Research.Done.Contains(research);
    }

    public struct ShownValue
    {
        /// <summary>null = bilinmiyor.</summary>
        public double? V;
        /// <summary>truth, field, verified, report, unknown, off.</summary>
        public string Src;
        /// <summary>Kaynağı sahte haber yaydığı için işaretlenmiş medya.</summary>
        public bool Flag;
    }

    public struct ShownGlobal
    {
        public double Tek, Trust, Dep;
    }

    public sealed class ActResult
    {
        public bool Ok;
        public string Msg;
        public static ActResult Fail(string msg) => new ActResult { Ok = false, Msg = msg };
        public static ActResult Success(string msg = null) => new ActResult { Ok = true, Msg = msg };
    }
}
