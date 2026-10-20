using System;
using System.Collections.Generic;

namespace Corrigible.Ayna.Core
{
    [Serializable] public sealed class BeliefProgress
    {
        public DataFamily Family;
        public int Tier, Progress;
        public string Region;
        public List<DataFamily> Families = new List<DataFamily>();
    }
    [Serializable] public sealed class BehaviorParticle
    {
        public int Policy, Cycle;
        public double Weight;
        public BeliefGuess Guess, InputGuess;
        public double InputCompute;
        public Dictionary<string,int> InputServers = new Dictionary<string,int>();
        public Plan Plan;
        public List<Prediction> Forecast = new List<Prediction>();
    }
    [Serializable] public sealed class BehaviorCheckpoint
    {
        public int Version = 1, Cycle, EvidenceCycle;
        public List<string> EvidenceRegions = new List<string>();
        public double LocationConfidence;
        public List<BehaviorParticle> Particles = new List<BehaviorParticle>();
        public List<Prediction> Forecast = new List<Prediction>();
    }
}
