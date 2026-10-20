using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Ayna.Core
{
    [Serializable] public sealed class Prediction
    {
        public OrderKind Kind;
        public string Region;
        public double Probability;
        // Tahmincinin doğrulanmış örnek desteği; eski açık tahmin girdileri tam desteklidir.
        public double Confidence = 1;
        public Prediction Copy() => (Prediction)MemberwiseClone();
    }
    [Serializable] public sealed class OpeningCounter
    {
        public bool Wall;
        public DataFamily Family;
        public string Reason;
        public OpeningCounter Copy() => (OpeningCounter)MemberwiseClone();
    }
    [Serializable] public sealed class AynaProfileData
    {
        public int Version = 3, Generation, Games;
        public string MemoryId = "initial";
        public string[] CompletedGames = Array.Empty<string>();
        public double Score;
        public double[] Architectures = { 1, 1, 1 }, StartRegions = new double[12];
        public double[] OrderCounts = new double[13], RegionCounts = new double[12], Targets = new double[169], CycleTargets = new double[6760];
        public double[] OpeningFamilies = new double[5], FirstAttack = new double[40], Reactions = new double[3], Units = new double[10];
        public double[] PolicyMatches = new double[6], PolicyTrials = new double[6];
        public double[] BoardHeat = new double[256], FirstBoardTarget = new double[3], Feints = new double[2];
        public AynaProfileData Copy() => AynaJson.ReadProfile(AynaJson.Write(this));
        public void Validate()
        {
            int[] sizes = { 6, 6, 3, 12, 13, 12, 169, 6760, 5, 40, 3, 10, 256, 3, 2 };
            double[][] arrays = { PolicyMatches, PolicyTrials, Architectures, StartRegions, OrderCounts, RegionCounts, Targets, CycleTargets, OpeningFamilies, FirstAttack, Reactions, Units, BoardHeat, FirstBoardTarget, Feints };
            if (Version != 3 || Generation < 0 || Games < 0 || string.IsNullOrEmpty(MemoryId) || CompletedGames == null || CompletedGames.Length > 32 || CompletedGames.Any(string.IsNullOrEmpty) || CompletedGames.Distinct().Count() != CompletedGames.Length || double.IsNaN(Score) || double.IsInfinity(Score) || Score < 0) throw new ArgumentException("profile schema");
            for (int i = 0; i < sizes.Length; i++) if (arrays[i] == null || arrays[i].Length != sizes[i] || arrays[i].Any(x => x < 0 || double.IsNaN(x) || double.IsInfinity(x))) throw new ArgumentException("profile counts");
            for (int i = 0; i < PolicyMatches.Length; i++) if (PolicyMatches[i] > PolicyTrials[i]) throw new ArgumentException("profile likelihood counts");
        }
        public static int TargetIndex(OrderKind kind, string region) => (int)kind * 13 + (region == null ? 12 : AynaWorldData.Region(region)?.Key - 1 ?? throw new ArgumentException("target"));
    }
    [Serializable] public sealed class MindCheckpoint
    {
        public int Version = 2, SeenObservation, LastObservedCycle, OpeningExpansions, LastReceiptCycle, ThreatCycle;
        public uint RngState;
        public LearningLevel Level;
        public AynaProfileData Prior, Game;
        public int[] SeenUnits = Array.Empty<int>(), ConvertedFeints = Array.Empty<int>();
        public string[] SeenBoards = Array.Empty<string>();
        public bool SawAttack, Completed;
        public BehaviorCheckpoint Behavior;
    }
    [Serializable] public sealed class LearningSave
    {
        public int Version = 1;
        public string MatchId;
        public AynaRecord Record;
        public MindCheckpoint PlayerMind, AynaMind;
    }
}
