using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Ayna.Core
{
    public static class AutoResolver
    {
        public static double Strength(IEnumerable<UnitView> units, IEnumerable<UnitView> opponents, AynaBalance b)
        {
            var enemy = opponents.ToArray(); double score = 0;
            foreach (var u in units)
            {
                double advantage = enemy.Length == 0 ? 0 : enemy.Count(e => AynaUnits.Beats(AynaUnits.Get(u.Kind).Class, AynaUnits.Get(e.Kind).Class)) / (double)enemy.Length;
                score += (u.Hp + b.AutoUtility[(int)u.Kind] * u.Hp / b.UnitHp[(int)u.Kind]) * (1 + b.TriangleStrength * advantage);
            }
            return score;
        }
        public static double BoardProbability(BoardView view, AynaBalance balance)
        {
            var p = view.Units.Where(u => u.Side == Side.Player).Select(u => new UnitView(u.Id, u.Kind, u.Hp)).ToArray();
            var a = view.Units.Where(u => u.Side == Side.Ayna).Select(u => new UnitView(u.Id, u.Kind, u.Hp)).ToArray();
            return Probability(Strength(p, a, balance), Strength(a, p, balance));
        }
        public static double Probability(double player, double enemy) => player <= 0 && enemy <= 0 ? .5 : Math.Max(0, player) / (Math.Max(0, player) + Math.Max(0, enemy));
        internal static double Strength(ArmyState a, ArmyState e, AynaBalance b) => Strength(a.Units.Select(x => new UnitView(x.Id, x.Kind, x.Hp)),
            e == null ? Enumerable.Empty<UnitView>() : e.Units.Select(x => new UnitView(x.Id, x.Kind, x.Hp)), b);
    }

    public sealed partial class AynaSimulation
    {
        double WallPower(int i, RegionState r)
        {
            var own = state.Sides[i]; double power = Math.Max(0, r.Wall[i] * balance.WallStrength - (own.Architecture == Architecture.Swarm ? balance.SwarmWallPenalty : 0));
            return power * (own.Tiers[(int)DataFamily.Energy] >= 3 ? balance.EnergyWallMultiplier : 1);
        }
        void ResolveBattle(RegionState r, List<ValidatedOrder>[] orders)
        {
                var armies = state.Sides.Select(s => s.Armies.Where(x => x.Region == r.Id && x.Units.Count > 0).OrderBy(x => x.Id).FirstOrDefault()).ToArray();
                bool encounter = armies.All(x => x != null);
                var attacking = new[] {
                    orders[0].Any(x => x.Valid && x.Order.Kind == OrderKind.Attack && x.Order.Region == r.Id),
                    orders[1].Any(x => x.Valid && x.Order.Kind == OrderKind.Attack && x.Order.Region == r.Id)
                };
                if (!encounter && !attacking.Any(x => x)) return;
                int attacker = attacking[0] ? 0 : attacking[1] ? 1 : -1;
                int winner, gained = 0; double probability;
                var rosters = new[] { Roster(armies[0], 0), Roster(armies[1], 1) };
                bool wallOnly = !encounter;
                if (wallOnly)
                {
                    if (attacker < 0 || armies[attacker] == null || r.Owned[1 - attacker] == 0) return;
                    double attack = AutoResolver.Strength(rosters[attacker], null, balance);
                    double wall = WallPower(1 - attacker, r) * (1 + balance.DefenderBonus);
                    probability = AutoResolver.Probability(attack, wall);
                    winner = state.WorldRng.Next() < probability ? attacker : 1 - attacker;
                    if (winner == attacker) { gained = Transfer(r, winner, balance.AutoWallServers); r.Wall[1 - attacker] = Math.Max(0, r.Wall[1 - attacker] - 1); }
                }
                else
                {
                    double p = AutoResolver.Strength(rosters[0], rosters[1], balance) + WallPower(0, r);
                    double a = AutoResolver.Strength(rosters[1], rosters[0], balance) + WallPower(1, r);
                    if (attacking[0] && !attacking[1]) a *= 1 + balance.DefenderBonus;
                    else if (attacking[1] && !attacking[0]) p *= 1 + balance.DefenderBonus;
                    probability = AutoResolver.Probability(p, a);
                    winner = state.WorldRng.Next() < probability ? 0 : 1;
                    gained = Transfer(r, winner, (int)Js.Round(balance.AutoRewardRatio * balance.BoardNodes));
                    Damage(rosters[winner], balance.AutoWinnerDamage); Damage(rosters[1 - winner], balance.AutoLoserDamage);
                    foreach (var army in armies) army.Units.RemoveAll(x => x.Hp <= 0);
                    if (attacking[winner]) r.Wall[1 - winner] = Math.Max(0, r.Wall[1 - winner] - 1);
                }
                state.Battles++;
                for (int i = 0; i < 2; i++)
                {
                    if (armies[i] != null || attacking[i] || r.Owned[i] > 0 || IsExact(i, r.Id))
                    {
                        state.Sides[i].BattleCount++;
                        state.Knowledge[i].Battles.Add(new BattleView(r.Id, (Side)winner, wallOnly,
                            wallOnly ? i == attacker ? probability : 1 - probability : i == 0 ? probability : 1 - probability,
                            winner == i ? gained : 0));
                        Observe(i, r.Id, "battle");
                    }
                }
        }
        ArmyState Roster(ArmyState army, int side)
        {
            if (army == null) return null;
            int size = state.Sides[side].Tiers[(int)DataFamily.Movement] >= 3 ? balance.MovementRosterSize : balance.RosterSize;
            return new ArmyState { Id = army.Id, Region = army.Region, Units = army.Units.OrderByDescending(u => u.Hp + balance.AutoUtility[(int)u.Kind]).ThenBy(u => u.Id).Take(size).ToList() };
        }
        void Damage(ArmyState army, double total)
        {
            while (total > 0 && army.Units.Count > 0)
            {
                var alive = army.Units.OrderBy(x => x.Id).ToArray(); var unit = alive[(int)Math.Floor(state.WorldRng.Next() * alive.Length)];
                double hit = Math.Min(1, total); unit.Hp -= hit; total -= hit;
                if (unit.Hp <= 0) army.Units.Remove(unit);
            }
        }

        void ApplyAwareness(List<ValidatedOrder>[] orders, HashSet<string>[] hidden)
        {
            state.Awareness = Math.Max(0, Math.Min(balance.ShutdownThreshold, state.Awareness + state.Sides.Sum(x => x.LastNoise) - balance.AwarenessDecay));
            state.Outages.RemoveAll(x => x.Until < state.Cycle);
            if (state.Awareness >= balance.ScanThreshold)
            {
                state.ScannedRegion = state.Regions[(int)Math.Floor(state.WorldRng.Next() * state.Regions.Count)].Id;
                state.ScanUntil = state.Cycle + balance.ScanDuration;
                for (int i = 0; i < 2; i++) Observe(i, state.ScannedRegion, "scan");
            }
            if (state.Awareness < balance.OutageThreshold) return;
            double max = state.Sides.Max(x => x.LastNoise); if (max <= 0) return;
            var candidates = Enumerable.Range(0, 2).Where(i => state.Sides[i].LastNoise == max).ToArray();
            int noisy = candidates.Length == 1 ? candidates[0] : candidates[(int)Math.Floor(state.WorldRng.Next() * candidates.Length)];
            var regional = orders[noisy].Where(x => x.Valid && x.Noise > 0 && x.Order.Region != null && !hidden[noisy].Contains(x.Order.Region))
                .GroupBy(x => x.Order.Region).Select(g => new { Region = g.Key, Noise = g.Sum(x => x.Noise) }).OrderByDescending(x => x.Noise).ThenBy(x => x.Region, StringComparer.Ordinal).ToArray();
            if (regional.Length == 0) return;
            var edges = AynaWorldData.Links.Concat(state.Sides[noisy].Tiers[(int)DataFamily.Vision] >= 3 ? AynaWorldData.Satellites : Enumerable.Empty<LinkDefinition>())
                .Where(x => x.Other(regional[0].Region) != null).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            if (edges.Length == 0) return;
            var edge = edges[(int)Math.Floor(state.WorldRng.Next() * edges.Length)]; var old = state.Outages.FirstOrDefault(x => x.Side == (Side)noisy && x.Link == edge.Id);
            if (old == null) state.Outages.Add(new OutageState { Side = (Side)noisy, Link = edge.Id, Until = state.Cycle + balance.OutageDuration });
            else old.Until = Math.Max(old.Until, state.Cycle + balance.OutageDuration);
            state.Outages = state.Outages.OrderBy(x => x.Side).ThenBy(x => x.Link, StringComparer.Ordinal).ToList();
            for (int i = 0; i < 2; i++) Observe(i, regional[0].Region, "outage");
        }
    }
}
