using System;
using System.Linq;

namespace Corrigible.Ayna.Core
{
    public sealed class BeliefSimulator
    {
        readonly AynaSimulation prototype;
        public BeliefSimulator(SideView view, BeliefGuess guess) { prototype = AynaSimulation.CreateBelief(view, guess, tacticalValuation: true); }
        public bool PublishForecast(Side observer, System.Collections.Generic.IEnumerable<Prediction> predictions) => prototype.LockForecast(observer, predictions);
        public SideView View(Side side) => prototype.View(side);
        public SideView Evaluate(Side side, Plan own, Plan enemy)
        {
            var sim = prototype.CopyBelief();
            sim.ResolveCycle(side == Side.Player ? own : enemy, side == Side.Player ? enemy : own);
            return sim.View(side);
        }
    }
    public sealed partial class AynaSimulation
    {
        internal AynaSimulation CopyBelief()
        {
            if (record != null) throw new InvalidOperationException("belief only");
            var s = EmptyWorld(state.Seed); s.Cycle = state.Cycle; s.Awareness = state.Awareness;
            s.WorldRng.State = state.WorldRng.State;
            foreach (var r in s.Regions)
            {
                var original = state.Region(r.Id); r.Free = original.Free; r.Owned = (int[])original.Owned.Clone(); r.Wall = (int[])original.Wall.Clone();
            }
            for (int i = 0; i < 2; i++)
            {
                var original = state.Sides[i]; var dest = s.Sides[i];
                dest.Architecture = original.Architecture; dest.Compute = original.Compute; dest.CoreRegion = original.CoreRegion; dest.CoreDestroyed = original.CoreDestroyed;
                dest.NextArmyId = original.NextArmyId; dest.NextUnitId = original.NextUnitId;
                dest.Tiers = (int[])original.Tiers.Clone(); dest.Training = CopyProgress(original.Training); dest.CoreMove = CopyProgress(original.CoreMove); dest.Singularity = CopyProgress(original.Singularity);
                dest.Armies = original.Armies.Select(a => new ArmyState { Id = a.Id, Region = a.Region, Units = a.Units.Select(u => new UnitState { Id = u.Id, Kind = u.Kind, Hp = u.Hp }).ToList() }).ToList();
            }
            s.Forecasts = state.Forecasts.Select(f => new ForecastState { Cycle = f.Cycle, Entries = f.Entries.Select(p => p.Copy()).ToList() }).ToArray();
            s.Outages = state.Outages.Select(o => new OutageState { Side = o.Side, Link = o.Link, Until = o.Until }).ToList();
            return new AynaSimulation(s, balance, null);
        }
        static ProgressState CopyProgress(ProgressState p) => p == null ? null : new ProgressState { Family = p.Family, Tier = p.Tier, Progress = p.Progress, Region = p.Region, Families = p.Families.ToList() };
    }
}
