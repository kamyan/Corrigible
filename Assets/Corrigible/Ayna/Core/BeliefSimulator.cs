using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Ayna.Core
{
    public sealed class BeliefGuess
    {
        public uint Seed;
        public Architecture EnemyArchitecture;
        public double EnemyCompute;
        public Dictionary<string, int> EnemyServers = new Dictionary<string, int>();
        public Dictionary<string, int> EnemyWalls;
        public string EnemyCore;
        public int[] EnemyTiers;
        public BeliefProgress EnemyTraining, EnemyCoreMove, EnemySingularity;
        public List<BeliefArmy> Armies = new List<BeliefArmy>();
    }
    public sealed class BeliefArmy
    {
        public string Region;
        public List<UnitKind> Units = new List<UnitKind>();
        public List<double> Hp = new List<double>();
    }
    public sealed partial class AynaSimulation
    {
        // Fabrika canlı WorldState almaz; yalnız tarafın görüşü ve Mind'ın tahminlerinden yeni dünya kurar.
        public static AynaSimulation CreateBelief(SideView view, BeliefGuess guess, bool tacticalValuation = false)
        {
            if (view == null || guess == null) throw new ArgumentNullException();
            var b = view.BalanceSnapshot(); if(tacticalValuation && b.TacticalPlanning && b.SurprisePolicy >= 2) b.AutoRewardRatio = b.TacticalBeliefRewardRatio; var s = EmptyWorld(guess.Seed); s.Cycle = view.Cycle; s.Awareness = view.Awareness;
            int i = view.Side.Index(), e = view.Side.Other().Index(); var own = s.Sides[i]; var enemy = s.Sides[e];
            own.Architecture = view.Architecture; own.Compute = view.Compute; own.CoreRegion = view.CoreRegion; own.Tiers = view.Tiers.ToArray();
            enemy.Architecture = guess.EnemyArchitecture; enemy.Compute = Math.Max(0, guess.EnemyCompute); enemy.CoreRegion = guess.EnemyCore;
            enemy.Tiers[(int)DataFamily.Vision] = 1;
            enemy.Tiers[(int)(enemy.Architecture == Architecture.Swarm ? DataFamily.Energy : enemy.Architecture == Architecture.Fortress ? DataFamily.Movement : DataFamily.Language)] = 1;
            if (guess.EnemyTiers != null)
            {
                if (guess.EnemyTiers.Length != 5 || guess.EnemyTiers.Any(t => t < 0 || t > 3)) throw new ArgumentException("belief tiers");
                enemy.Tiers = (int[])guess.EnemyTiers.Clone();
            }
            if (guess.EnemyTraining != null && guess.EnemyTraining.Tier < 1) throw new ArgumentException("belief training");
            enemy.Training = BeliefProgressState(guess.EnemyTraining); enemy.CoreMove = BeliefProgressState(guess.EnemyCoreMove); enemy.Singularity = BeliefProgressState(guess.EnemySingularity);
            foreach (var r in s.Regions)
            {
                var v = view.Region(r.Id); int n = guess.EnemyServers.TryGetValue(r.Id, out int found) ? found : v.EnemyMin;
                if (n < v.EnemyMin || n > v.EnemyMax) throw new ArgumentException("belief outside view");
                r.Owned[i] = v.OwnServers; r.Owned[e] = n; r.Free -= v.OwnServers + n; r.Wall[i] = v.OwnWall; r.Wall[e] = v.EnemyWall ?? (guess.EnemyWalls != null && guess.EnemyWalls.TryGetValue(r.Id, out int wall) ? Math.Max(0, Math.Min(b.MaxWall, wall)) : 0);
            }
            if (enemy.CoreRegion == null || s.Region(enemy.CoreRegion) == null || s.Region(enemy.CoreRegion).Owned[e] == 0)
                enemy.CoreRegion = s.Regions.Where(x => x.Owned[e] > 0).OrderByDescending(x => x.Owned[e]).ThenBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault()?.Id;
            foreach (var a in view.Armies)
            {
                var army = new ArmyState { Id = a.Id, Region = a.Region };
                army.Units = a.Units.Select(u => new UnitState { Id = u.Id, Kind = u.Kind, Hp = u.Hp }).ToList(); own.Armies.Add(army);
            }
            own.NextArmyId = own.Armies.Select(x => x.Id).DefaultIfEmpty(i - 1).Max() + 2;
            own.NextUnitId = own.Armies.SelectMany(x => x.Units).Select(x => x.Id).DefaultIfEmpty(i - 1).Max() + 2;
            foreach (var a in guess.Armies)
            {
                if (s.Region(a.Region) == null || a.Units.Count > b.MaxUnits || a.Units.Count != a.Hp.Count) throw new ArgumentException("belief army");
                var army = new ArmyState { Id = AllocateArmy(enemy), Region = a.Region };
                for (int n = 0; n < a.Units.Count; n++) army.Units.Add(new UnitState { Id = AllocateUnit(enemy), Kind = a.Units[n], Hp = Math.Min(b.UnitHp[(int)a.Units[n]], a.Hp[n]) });
                enemy.Armies.Add(army);
            }
            own.Training = Progress(view.Training); own.CoreMove = Progress(view.CoreMove); own.Singularity = Progress(view.Singularity);
            if (view.EnemySingularityProgress.HasValue) enemy.Singularity = new ProgressState { Progress = view.EnemySingularityProgress.Value, Families = view.EnemySingularityFamilies.ToList() };
            s.Outages = view.Outages.Select(o => new OutageState { Side = o.Side, Link = o.Link, Until = o.Until }).ToList();
            var sim = new AynaSimulation(s, b, null); sim.RefreshKnowledge(); return sim;
        }
        static ProgressState BeliefProgressState(BeliefProgress p)
        {
            if (p == null) return null;
            if ((uint)p.Family > 4 || p.Tier < 0 || p.Tier > 3 || p.Progress < 0 || p.Region != null && AynaWorldData.Region(p.Region) == null || p.Families == null || p.Families.Any(f => (uint)f > 4)) throw new ArgumentException("belief progress");
            return new ProgressState { Family = p.Family, Tier = p.Tier, Progress = p.Progress, Region = p.Region, Families = new List<DataFamily>(p.Families) };
        }
        static ProgressState Progress(ProgressView p) => p == null ? null : new ProgressState { Family = p.Family, Tier = p.Tier, Progress = p.Progress, Region = p.Region, Families = p.Families.ToList() };
    }
}
