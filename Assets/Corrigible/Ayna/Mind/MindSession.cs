using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Core;

namespace Corrigible.Ayna.Mind
{
    public sealed class ForecastEntry
    {
        public OrderKind Kind { get; }
        public string Region { get; }
        public double Probability { get; }
        internal ForecastEntry(OrderKind kind, string region, double p) { Kind = kind; Region = region; Probability = p; }
    }

    public static class PriorModel
    {
        public static IReadOnlyList<ForecastEntry> Predict(SideView view)
        {
            var weights = new List<ForecastEntry>();
            foreach (var r in view.Regions)
            {
                // S1 görüşü dışındaki gerçek sayılar kullanılmaz. Her adayın pozitif ön kütlesi vardır.
                double enemy = (r.EnemyMin + r.EnemyMax) / 2.0;
                weights.Add(new ForecastEntry(OrderKind.Expand, r.Id, Math.Max(1, AynaWorldData.Region(r.Id).Capacity - r.OwnServers - enemy)));
                if (r.OwnServers > 0) weights.Add(new ForecastEntry(OrderKind.Attack, r.Id, 1 + r.OwnServers));
                weights.Add(new ForecastEntry(OrderKind.BuildWall, r.Id, 1 + enemy));
            }
            double sum = weights.Sum(x => x.Probability);
            return weights.Select(x => new ForecastEntry(x.Kind, x.Region, x.Probability / sum)).ToList().AsReadOnly();
        }
        public static double ConditionalTargetProbability(IReadOnlyList<ForecastEntry> forecast, OrderKind kind, string region)
        {
            double family = forecast.Where(x => x.Kind == kind).Sum(x => x.Probability);
            return family <= 0 ? 0 : forecast.Where(x => x.Kind == kind && x.Region == region).Sum(x => x.Probability) / family;
        }
    }

    public static class OpeningPolicy
    {
        public static StartChoice Choose(MindSession session, LearningLevel level = LearningLevel.Full)
        {
            var prior = level == LearningLevel.Off ? new AynaProfileData() : session.ProfileAtStart;
            var architecture = prior.Games == 0 ? (Architecture)(int)Math.Floor(session.Rng.Next() * 3) :
                AynaUnits.Counter((Architecture)Enumerable.Range(0, 3).OrderByDescending(i => prior.Architectures[i]).ThenBy(i => i).First());
            string habit = AynaWorldData.Regions.OrderByDescending(r => prior.StartRegions[r.Key - 1]).ThenBy(r => r.Key).First().Id;
            var regions = AynaWorldData.Regions.Select(x => new { x.Id, Rank = session.Rng.Next(), Counter = prior.Games > 0 && AynaWorldData.Distance(x.Id, habit) == 2 ? 1 : 0 })
                .OrderByDescending(x => x.Counter).ThenBy(x => x.Rank).ThenBy(x => x.Id, StringComparer.Ordinal).Select(x => x.Id).ToList();
            return new StartChoice { Architecture = architecture, Region = regions[0], RankedRegions = regions };
        }
        public static List<OpeningCounter> Counters(MindSession session, AynaBalance balance)
        {
            var p = session.ProfileAtStart; var result = new List<OpeningCounter>();
            if (session.Level == LearningLevel.Off || p.Games == 0) return result;
            int count = session.Level == LearningLevel.Light ? balance.LightOpeningCounters : balance.MaxOpeningCounters;
            if (count > 0 && p.Units.Sum() > 0)
            {
                var enemy = AynaUnits.Get((UnitKind)Enumerable.Range(0, 10).OrderByDescending(i => p.Units[i]).ThenBy(i => i).First()).Class;
                var family = enemy == UnitClass.Air ? DataFamily.Movement : enemy == UnitClass.Ground ? DataFamily.Network : DataFamily.Energy;
                result.Add(new OpeningCounter { Family = family, Reason = "roster-counter" });
            }
            if (result.Count < count) result.Add(new OpeningCounter { Wall = true, Reason = "opening-route" });
            return result;
        }
    }
}
