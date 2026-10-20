using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Core;

namespace Corrigible.Ayna.Mind.Bots
{
    public enum BotStyle { Balanced, Greedy, Aggro, Stealth, Habit, Random, Surprise }
    public interface IPlanBot
    {
        string Id { get; }
        Plan MakePlan(SideView view);
    }

    public static class PlanFactory
    {
        // Canlı dünya okunmaz: yalnız görülen temas için, mevcut Yürü emriyle güvenli komşuya çekilme adayı.
        public static Plan Withdraw(SideView v, Plan source, AynaBalance balance = null)
        {
            var plan = source.Copy(); var b = balance ?? v.BalanceSnapshot(); double cash = v.Compute;
            foreach (var army in v.Armies.OrderBy(a => a.Id))
            {
                var current = v.Region(army.Region);
                if (current.EnemyArmies.Count == 0 || cash < b.MarchCost) continue;
                var safe = AynaWorldData.Neighbours(army.Region, v.Tiers[(int)DataFamily.Vision] >= 3)
                    .Where(n => v.Linked(army.Region, n)).Select(v.Region)
                    .Where(r => r.OwnServers > 0 && r.EnemyMin == 0 && r.EnemyArmies.Count == 0)
                    .OrderByDescending(r => r.OwnServers).ThenBy(r => r.Id, StringComparer.Ordinal).FirstOrDefault();
                if (safe == null) continue;
                plan.Orders.RemoveAll(o => o.ArmyId == army.Id && (o.Kind == OrderKind.March || o.Kind == OrderKind.Attack || o.Kind == OrderKind.Produce || o.Kind == OrderKind.Repair));
                plan.Orders.Insert(0, new Order { Kind = OrderKind.March, ArmyId = army.Id, Region = safe.Id }); cash -= b.MarchCost;
            }
            return plan;
        }
        public static Plan Build(SideView v, BotStyle style, int variant, Rng rng = null, AynaBalance balance = null, IReadOnlyDictionary<string, double> targetWeights = null)
        {
            var b = balance ?? v.BalanceSnapshot(); var orders = new List<Order>(); double cash = v.Compute;
            var regionReserved = new HashSet<string>();
            var costs = new Dictionary<Order, double>();
            void Add(Order o, double cost) { if (cost <= cash) { orders.Add(o); costs[o] = cost; cash -= cost; } }
            bool quiet = style == BotStyle.Stealth;
            bool trainFirst = quiet || style == BotStyle.Balanced && variant % 3 == 1;
            void Research()
            {
                if (v.Singularity == null && v.Tiers.Count(x => x >= 3) >= b.SingularityFamilies)
                { Add(new Order { Kind = OrderKind.StartSingularity }, b.SingularityCost); return; }
                if (v.Training != null) return;
                var eligible = Enumerable.Range(0, 5).Where(f => v.Tiers[f] < 3 && v.Data((DataFamily)f) >= b.TrainingServers[v.Tiers[f]]).ToArray();
                // Tekillik için üç aileyi bitir; yalnız tek aileyi derinleştirip veri kıtlığına takılma.
                var path = Enumerable.Range(0, 5).OrderByDescending(f => v.Data((DataFamily)f)).ThenByDescending(f => v.Tiers[f]).ThenBy(f => f).Take(b.SingularityFamilies).ToArray();
                var target = eligible.OrderByDescending(f => path.Contains(f)).ThenByDescending(f => v.Tiers[f]).ThenByDescending(f => v.Data((DataFamily)f)).ThenBy(f => (f + variant) % 5).DefaultIfEmpty(-1).First();
                if (target >= 0) Add(new Order { Kind = OrderKind.Train, Family = (DataFamily)target }, b.TrainingCosts[v.Tiers[target]]);
            }
            if (trainFirst && style != BotStyle.Greedy && style != BotStyle.Random) Research();

            // Ekonomik ve Sürpriz politikası aynı askerî/araştırma araçlarına sahiptir.
            // Ayrılan tek ekonomik tercih genişleme hedeflerinin sıralamasıdır.
            if (style != BotStyle.Greedy || b.SurprisePolicy >= 2)
            {
                foreach (var army in v.Armies.OrderBy(x => x.Id))
                {
                    if (army.Units.Count == 0)
                    {
                        if (v.Region(army.Region).OwnServers > 0)
                        {
                            var replacement = AynaUnits.All.Where(u => v.Tiers[(int)u.Family] >= u.Tier && v.Data(u.Family) > 0).OrderBy(u => b.ProductionCost(v.Architecture, u.Kind)).ThenBy(u => u.Kind).FirstOrDefault();
                            if (replacement != null) Add(new Order { Kind = OrderKind.Produce, ArmyId = army.Id, Unit = replacement.Kind }, b.ProductionCost(v.Architecture, replacement.Kind));
                        }
                        else
                        {
                            var home = AynaWorldData.Neighbours(army.Region, v.Tiers[0] >= 3).Where(n => v.Linked(army.Region, n))
                                .OrderBy(n => v.Regions.Where(r => r.OwnServers > 0).Select(r => AynaWorldData.Distance(n, r.Id)).DefaultIfEmpty(int.MaxValue).Min()).ThenBy(n => n, StringComparer.Ordinal).FirstOrDefault();
                            if (home != null) Add(new Order { Kind = OrderKind.March, ArmyId = army.Id, Region = home }, b.MarchCost);
                        }
                        continue;
                    }
                    var targets = v.Regions.Where(r => r.EnemyMin > 0 && (r.Id == army.Region || v.Linked(army.Region, r.Id)))
                        .OrderByDescending(r => r.Id == v.EnemyCoreRegion).ThenByDescending(r => r.EnemyMin).ThenBy(r => r.Id, StringComparer.Ordinal).ToArray();
                    if (targets.Length > 0 && (!quiet || v.EnemySingularityProgress.HasValue))
                    {
                        var t = targets[(variant / 3) % targets.Length]; Add(Order.Attack(t.Id, army.Id), b.AttackCost);
                    }
                    else if (!quiet && style != BotStyle.Habit)
                    {
                        var enemyRegions = v.Regions.Where(r => r.EnemyMin > 0 || r.EnemyArmies.Count > 0).ToArray();
                        var destination = AynaWorldData.Neighbours(army.Region, v.Tiers[(int)DataFamily.Vision] >= 3).Where(n => v.Linked(army.Region, n))
                            .Select(n => new { Id = n, Dist = enemyRegions.Length == 0 ? AynaWorldData.Distance(n, v.CoreRegion) : enemyRegions.Min(r => AynaWorldData.Distance(n, r.Id)),
                                Enemy = v.Region(n).EnemyMin })
                            .OrderBy(x => enemyRegions.Length == 0 ? -x.Dist : x.Dist).ThenByDescending(x => x.Enemy).ThenBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault();
                        if (destination != null && destination.Id != army.Region) Add(new Order { Kind = OrderKind.March, Region = destination.Id, ArmyId = army.Id }, b.MarchCost);
                    }
                    if (v.Region(army.Region).OwnServers <= 0) continue;
                    foreach (var u in army.Units.Where(u => u.Hp < b.UnitHp[(int)u.Kind])) Add(new Order { Kind = OrderKind.Repair, ArmyId = army.Id, UnitId = u.Id }, b.RepairCost);
                    if (army.Count < b.MaxUnits && style != BotStyle.Habit)
                    {
                        var available = AynaUnits.All.Where(u => v.Tiers[(int)u.Family] >= u.Tier && v.Data(u.Family) > 0).OrderByDescending(u => b.UnitHp[(int)u.Kind] + b.AutoUtility[(int)u.Kind]).ThenBy(u => u.Kind).ToArray();
                        if (available.Length > 0 && (style == BotStyle.Aggro || cash > b.UnitCosts.Max() + b.ExpandCost * b.ExpandLimit))
                        {
                            var unit = available[variant % available.Length]; Add(new Order { Kind = OrderKind.Produce, ArmyId = army.Id, Unit = unit.Kind }, b.ProductionCost(v.Architecture, unit.Kind));
                        }
                    }
                }
            }

            var expansion = v.Regions.Where(r => r.OwnServers < AynaWorldData.Region(r.Id).Capacity - r.EnemyMin &&
                (r.OwnServers > 0 || v.Regions.Any(o => o.OwnServers > 0 && v.Linked(o.Id, r.Id))))
                .Select(r => new { Region = r, Free = r.FreeServers ?? Math.Max(0, AynaWorldData.Region(r.Id).Capacity - r.OwnServers - (r.EnemyMin + r.EnemyMax) / 2),
                    DataValue = b.TrainingServers[Math.Min(2, v.Tiers[(int)AynaWorldData.Region(r.Id).Family])] - v.Data(AynaWorldData.Region(r.Id).Family) })
                .Where(x => x.Free > 0);
            if (style == BotStyle.Habit) expansion = expansion.OrderBy(x => AynaWorldData.Region(x.Region.Id).Key);
            else if (style == BotStyle.Surprise)
            {
                var predictions = v.IncomingForecast; expansion = predictions.Count > 0 ? expansion.OrderBy(x => MindSession.TargetProbability(predictions, OrderKind.Expand, x.Region.Id)) : expansion.OrderBy(x => PriorModel.ConditionalTargetProbability(PriorModel.Predict(v), OrderKind.Expand, x.Region.Id));
            }
            else if (style == BotStyle.Greedy && b.SurprisePolicy >= 2)
                expansion = expansion.OrderByDescending(x => Math.Min(x.Free, b.ExpansionLimit(v.Architecture, v.Tiers[(int)DataFamily.Network])))
                    .ThenByDescending(x => Math.Min(Math.Max(0,x.DataValue),Math.Min(x.Free,b.ExpansionLimit(v.Architecture,v.Tiers[(int)DataFamily.Network]))))
                    .ThenByDescending(x=>x.Free).ThenBy(x=>x.Region.Id,StringComparer.Ordinal);
            else if (style == BotStyle.Random && rng != null) expansion = expansion.OrderBy(x => rng.Next());
            else expansion = expansion.OrderByDescending(x => style == BotStyle.Greedy ? x.Free : x.DataValue > 0 ? b.ScoreData : 0)
                .ThenByDescending(x => x.Free).ThenBy(x => (AynaWorldData.Region(x.Region.Id).Key + variant) % 12);
            if (targetWeights != null) expansion = expansion.OrderByDescending(x => targetWeights.TryGetValue(x.Region.Id, out double w) ? w : 0).ThenBy(x => x.Region.Id, StringComparer.Ordinal);
            foreach (var x in expansion.ToArray())
            {
                if ((quiet || style == BotStyle.Surprise && b.SurprisePolicy >= 4) && regionReserved.Count >= 1) break;
                int amount = Math.Min(x.Free, Math.Min(b.ExpansionLimit(v.Architecture, v.Tiers[(int)DataFamily.Network]), (int)Math.Floor(cash / b.ExpandCost)));
                if (amount <= 0) continue;
                double cost = amount * b.ExpandCost;
                if (quiet && cash >= cost + (v.Architecture == Architecture.Ghost ? b.GhostHideCost : b.HideCost))
                    Add(new Order { Kind = OrderKind.Hide, Region = x.Region.Id }, v.Architecture == Architecture.Ghost ? b.GhostHideCost : b.HideCost);
                Add(Order.Expand(x.Region.Id, amount), cost); regionReserved.Add(x.Region.Id);
                if (style == BotStyle.Random && regionReserved.Count >= b.PriorPlanOrders) break;
            }
            if (!trainFirst && (style != BotStyle.Greedy || b.SurprisePolicy >= 2) && style != BotStyle.Random && style != BotStyle.Habit) Research();
            if (style == BotStyle.Stealth || style == BotStyle.Balanced && variant % 3 == 2)
            {
                var r = v.Regions.Where(x => x.OwnServers > 0 && x.OwnWall < b.MaxWall).OrderByDescending(x => x.Id == v.CoreRegion).ThenByDescending(x => x.EnemyMin).FirstOrDefault();
                if (r != null) Add(new Order { Kind = OrderKind.BuildWall, Region = r.Id }, v.Architecture == Architecture.Fortress ? b.FortressWallCost : b.WallCost);
            }
            if (style == BotStyle.Surprise && b.SurprisePolicy >= 4)
            {
                // Sürpriz odaklı referans, tek ana genişlemeyi daha pahalı yan emirlerle bozamaz.
                var focus = orders.FirstOrDefault(o => o.Kind == OrderKind.Expand);
                if (focus != null) orders.RemoveAll(o => o != focus && o.Kind != OrderKind.Train && o.Kind != OrderKind.StartSingularity && costs[o] >= costs[focus]);
            }
            if (orders.Count == 0) orders.Add(new Order { Kind = OrderKind.Wait });
            return new Plan(orders);
        }
    }
}
