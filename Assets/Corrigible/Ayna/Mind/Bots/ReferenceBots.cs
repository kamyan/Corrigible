using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Core;

namespace Corrigible.Ayna.Mind.Bots
{
    public sealed class HabitBot : IPlanBot { public string Id => "Habit"; public Plan MakePlan(SideView v) => PlanFactory.Build(v, BotStyle.Habit, 0); }
    public sealed class GreedyBot : IPlanBot { public string Id => "Greedy"; public Plan MakePlan(SideView v) => PlanFactory.Build(v, BotStyle.Greedy, 0); }
    public sealed class AggroBot : IPlanBot { public string Id => "Aggro"; public Plan MakePlan(SideView v) => PlanFactory.Build(v, BotStyle.Aggro, 0); }
    public sealed class StealthBot : IPlanBot { public string Id => "Stealth"; public Plan MakePlan(SideView v) => PlanFactory.Build(v, BotStyle.Stealth, 0); }
    public sealed class SurpriseBot : IPlanBot { public string Id => "Surprise"; public Plan MakePlan(SideView v) => PlanFactory.Build(v, BotStyle.Surprise, 0); }

    public sealed class RandomBot : IPlanBot
    {
        readonly Rng rng;
        public string Id => "Random";
        public RandomBot(uint seed) { rng = new Rng(seed); }
        public Plan MakePlan(SideView v)
        {
            var b = v.BalanceSnapshot(); var choices = new List<(Order Order, double Cost)>();
            foreach (var r in v.Regions)
            {
                if ((r.FreeServers ?? (AynaWorldData.Region(r.Id).Capacity - r.OwnServers - r.EnemyMin)) > 0 && (r.OwnServers > 0 || v.Regions.Any(o => o.OwnServers > 0 && v.Linked(o.Id, r.Id))))
                    choices.Add((Order.Expand(r.Id, 1), b.ExpandCost));
                if (r.OwnServers > 0 && r.OwnWall < b.MaxWall) choices.Add((new Order { Kind = OrderKind.BuildWall, Region = r.Id }, v.Architecture == Architecture.Fortress ? b.FortressWallCost : b.WallCost));
                choices.Add((new Order { Kind = OrderKind.Feint, Region = r.Id }, v.Architecture == Architecture.Ghost ? b.GhostFeintCost : b.FeintCost));
                choices.Add((new Order { Kind = OrderKind.Hide, Region = r.Id }, v.Architecture == Architecture.Ghost ? b.GhostHideCost : b.HideCost));
            }
            foreach (var army in v.Armies.Where(a => a.Count > 0))
                foreach (var r in v.Regions)
                {
                    if (v.Linked(army.Region, r.Id)) choices.Add((new Order { Kind = OrderKind.March, ArmyId = army.Id, Region = r.Id }, b.MarchCost));
                    if (r.EnemyMin > 0 && (r.Id == army.Region || v.Linked(army.Region, r.Id))) choices.Add((Order.Attack(r.Id, army.Id), b.AttackCost));
                }
            if (v.Training == null) for (int f = 0; f < 5; f++)
                if (v.Tiers[f] < 3 && v.Data((DataFamily)f) >= b.TrainingServers[v.Tiers[f]]) choices.Add((new Order { Kind = OrderKind.Train, Family = (DataFamily)f }, b.TrainingCosts[v.Tiers[f]]));
            var output = new List<Order>(); var used = new HashSet<string>(); double cash = v.Compute;
            int limit = 1 + (int)Math.Floor(rng.Next() * b.PriorPlanOrders);
            while (choices.Count > 0 && output.Count < limit)
            {
                int index = (int)Math.Floor(rng.Next() * choices.Count); var item = choices[index]; choices.RemoveAt(index);
                string key = item.Order.Kind == OrderKind.Attack || item.Order.Kind == OrderKind.March ? "army:" + item.Order.ArmyId : item.Order.Kind + ":" + item.Order.Region;
                if (item.Cost > cash || used.Contains(key)) continue;
                used.Add(key); output.Add(item.Order); cash -= item.Cost;
            }
            return new Plan(output);
        }
    }
    public sealed class MindBot : IPlanBot
    {
        readonly MindSession session;
        public string Id => "Mind";
        public MindBot(uint seed, AynaProfileData prior = null, LearningLevel level = LearningLevel.Off) { session = new MindSession(seed, prior, level); }
        public Plan MakePlan(SideView v) => MacroPlanner.Choose(v, session);
    }
}
