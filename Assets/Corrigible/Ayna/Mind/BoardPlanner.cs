using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind.Bots;
using Corrigible.Core;

namespace Corrigible.Ayna.Mind
{
    public static class BoardPlanner
    {
        sealed class Candidate
        {
            internal BoardSimulation Sim;
            internal BoardPlan Plan;
            internal double Score;
        }
        public static BoardPlan Choose(BoardView view, BotStyle style = BotStyle.Balanced, Rng rng = null, IReadOnlyList<double> responseHeat = null)
        {
            if (responseHeat != null && (responseHeat.Count != 64 || responseHeat.Any(x => x < 0 || double.IsNaN(x) || double.IsInfinity(x)))) throw new ArgumentException("response heat");
            if (view == null || view.Phase == BoardPhase.Finished || !view.Units.Any(u => u.Controller == view.Side)) return new BoardPlan();
            var b = view.BalanceSnapshot(); var root = BoardSimulation.CreateBelief(view);
            var beam = new List<Candidate> { new Candidate { Sim = root, Plan = new BoardPlan(), Score = Evaluate(root.View(view.Side), b) + (style == BotStyle.Habit ? HabitValue(root.View(view.Side), view) * b.BoardHabitWeight : 0) } };
            // Birim sırası sabittir; her adımda dar beam yalnız açık görünüm kopyalarını ilerletir.
            var ids = view.Units.Where(u => u.Controller == view.Side).OrderBy(u => u.Id).Select(u => u.Id)
                .Concat(view.Units.Any(u => u.Controller == view.Side && u.Kind == UnitKind.Intrusion) ? view.Units.Where(u => u.Controller != view.Side && AynaUnits.Get(u.Kind).Class == UnitClass.Ground).OrderBy(u => u.Id).Select(u => u.Id) : Enumerable.Empty<int>()).ToArray();
            foreach (int id in ids) foreach (bool moving in new[] { true, false })
            {
                var next = new List<Candidate>();
                foreach (var current in beam)
                {
                    next.Add(current);
                    var currentView = current.Sim.View(view.Side);
                    var legal = current.Sim.LegalCommands(view.Side, id, moving).Where(c => c.UnitId == id && (c.Action == BoardAction.Move) == moving).ToArray();
                    if (style == BotStyle.Random && rng != null)
                        legal = legal.Length == 0 ? legal : new[] { legal[(int)(rng.Next() * legal.Length)] };
                    else legal = legal.OrderByDescending(c => style == BotStyle.Habit && moving ? HabitScore(view, c.To) : Rank(currentView, c, b, style) + (responseHeat == null ? 0 : responseHeat[c.To] * b.ModelHeatWeight)).ThenBy(c => c.Action).ThenBy(c => c.To).ThenBy(c => c.TargetUnitId).Take(b.BoardBeamWidth).ToArray();
                    foreach (var command in legal)
                    {
                        var sim = BoardSimulation.CreateBelief(currentView); if (!sim.Apply(view.Side, command)) continue;
                        var plan = current.Plan.Copy(); plan.Commands.Add(command.Copy());
                        if (view.Phase == BoardPhase.Intents && !BoardSimulation.CreateBelief(view, false).SetIntents(plan)) continue;
                        double score = Evaluate(sim.View(view.Side), b);
                        if (style == BotStyle.Habit) score += HabitValue(sim.View(view.Side), view) * b.BoardHabitWeight;
                        if (style == BotStyle.Random) score = plan.Commands.Count;
                        next.Add(new Candidate { Sim = sim, Plan = plan, Score = score });
                    }
                }
                beam = next.OrderByDescending(c => c.Score).ThenBy(c => c.Plan.Commands.Count).Take(style == BotStyle.Random || style == BotStyle.Habit ? 1 : b.BoardBeamWidth).ToList();
            }
            if (style == BotStyle.Balanced && view.Phase == BoardPhase.Intents)
            {
                // Niyet kilitlenmeden önce görünen konumlardan muhtemel oyuncu cevabını benzet.
                foreach (var candidate in beam)
                {
                    var projected = BoardSimulation.CreateBelief(view, false);
                    if (!projected.SetIntents(candidate.Plan)) { candidate.Score = double.NegativeInfinity; continue; }
                    var response = view.Side.Other(); var reply = Choose(projected.View(response), responseHeat: responseHeat);
                    foreach (var c in reply.Commands) { if (projected.Finished) break; projected.Apply(response, c); }
                    if (!projected.Finished) projected.EndResponse(response);
                    candidate.Score = Evaluate(projected.View(view.Side), b);
                }
                beam = beam.OrderByDescending(c => c.Score).ToList();
            }
            else if (style == BotStyle.Balanced && view.Phase == BoardPhase.Response)
            {
                foreach (var candidate in beam)
                {
                    var projected = BoardSimulation.CreateBelief(candidate.Sim.View(view.Side));
                    projected.EndResponse(view.Side); candidate.Score = Evaluate(projected.View(view.Side), b);
                }
                beam = beam.OrderByDescending(c => c.Score).ToList();
            }
            return double.IsNegativeInfinity(beam[0].Score) ? new BoardPlan() : beam[0].Plan.Copy();
        }
        static double HabitValue(BoardView current, BoardView original)
        {
            return current.Units.Where(u => u.Controller == current.Side).Sum(u => HabitScore(original, u.Cell) - HabitScore(original, original.Units.First(x => x.Id == u.Id).Cell));
        }
        static double HabitScore(BoardView v, int cell)
        {
            // Aynı kanadı izleyen denetim botu; tuzak bilgisine göre rota değiştirmez.
            int target = BoardTemplates.TransformCell(v.Side == v.IntentSide ? (v.Turn == 1 ? 32 : 56) : (v.Turn == 1 ? 31 : 7), v.Transform);
            return -BoardSimulation.Distance(cell, target);
        }
        static double Rank(BoardView v, BoardCommand c, AynaBalance b, BotStyle style)
        {
            var u = v.Units.First(x => x.Id == c.UnitId);
            if (c.Action == BoardAction.Move)
            {
                if (style == BotStyle.Habit) return HabitScore(v, c.To);
                var targets = v.Nodes.Where(n => n.Owner != v.Side).Select(n => n.Cell).ToArray();
                if (targets.Length == 0) targets = v.Units.Where(t => t.Controller != v.Side).Select(t => t.Cell).ToArray();
                double result = -targets.Select(t => BoardSimulation.Distance(c.To, t)).DefaultIfEmpty(0).Min() * b.BoardDistanceScore;
                if (v.Nodes.Any(n => n.Cell == c.To && n.Owner != v.Side)) result += b.BoardNodeScore;
                if (v.CoreCells[v.Side.Other().Index()] >= 0) result -= BoardSimulation.Distance(c.To, v.CoreCells[v.Side.Other().Index()]) * b.BoardCoreApproach;
                if (v.Traps.Any(t => t.Owner != v.Side && t.Cell == c.To)) result -= b.BoardHpScore * b.TrapDamage;
                return result;
            }
            if (c.Action == BoardAction.Ability && u.Kind == UnitKind.Emp)
                return v.Units.Where(t => Math.Abs(t.Cell % 8 - c.To % 8) <= 1 && Math.Abs(t.Cell / 8 - c.To / 8) <= 1 && AynaUnits.Get(t.Kind).Class != UnitClass.Cyber).Sum(t => t.Controller == v.Side ? -b.BoardStunScore : b.BoardStunScore);
            var target = v.Units.FirstOrDefault(t => t.Cell == c.To);
            if (c.Action == BoardAction.Ability && u.Kind == UnitKind.Intrusion && target != null) return b.BoardControlScore;
            if (c.Action == BoardAction.Ability && u.Kind == UnitKind.PhantomOrder) return target != null && target.Controller == v.Side.Other() ? b.BoardControlScore : 0;
            if (c.Action == BoardAction.Ability && u.Kind == UnitKind.Overload)
                return v.Units.Where(t => Math.Abs(t.Cell % 8 - u.Cell % 8) <= 1 && Math.Abs(t.Cell / 8 - u.Cell / 8) <= 1).Sum(t => t.Controller == v.Side ? -t.Hp : Math.Min(t.Hp, b.OverloadDamage)) * b.BoardHpScore;
            if (v.CoreCells[v.Side.Other().Index()] == c.To) return b.BoardCoreScore;
            if (target == null) return -b.BoardNodeScore;
            double damage = c.Action == BoardAction.Ability && u.Kind == UnitKind.Sniper ? b.SniperDamage : b.BasicDamage;
            if (AynaUnits.Beats(AynaUnits.Get(u.Kind).Class, AynaUnits.Get(target.Kind).Class)) damage += b.ClassBonus;
            if (target.Kind == UnitKind.Guardian) damage = Math.Max(b.MinDamage, damage - b.GuardianArmor);
            return Math.Min(target.Hp, damage) * b.BoardHpScore + (damage >= target.Hp ? b.BoardKillScore : 0);
        }
        public static double Evaluate(BoardView v, AynaBalance b)
        {
            Side own = v.Side, enemy = own.Other();
            if (v.Result != null) return v.Result.Winner == own ? b.BoardWinScore : v.Result.Winner == Side.None ? 0 : -b.BoardWinScore;
            double hp = v.Units.Sum(u => u.Controller == own ? u.Hp : -u.Hp) * b.BoardHpScore;
            double nodes = 0, approach = 0;
            foreach (var n in v.Nodes)
            {
                Side owner = n.Owner; var occupying = v.Units.FirstOrDefault(u => u.Cell == n.Cell);
                if (occupying != null)
                {
                    bool p = v.Units.Any(u => u.Controller == own && BoardSimulation.Distance(u.Cell, n.Cell) == 1);
                    bool a = v.Units.Any(u => u.Controller == enemy && BoardSimulation.Distance(u.Cell, n.Cell) == 1);
                    if (!(p && a) || occupying.Kind == UnitKind.Relay) owner = occupying.Controller;
                }
                nodes += owner == own ? b.BoardNodeScore : owner == enemy ? -b.BoardNodeScore : 0;
                if (owner != own) approach -= v.Units.Where(u => u.Controller == own).Select(u => BoardSimulation.Distance(u.Cell, n.Cell)).DefaultIfEmpty(16).Min() * b.BoardDistanceScore;
            }
            if (v.CoreCells[enemy.Index()] >= 0) approach -= v.Units.Where(u => u.Controller == own).Select(u => BoardSimulation.Distance(u.Cell, v.CoreCells[enemy.Index()])).DefaultIfEmpty(16).Min() * b.BoardCoreApproach;
            double core = (b.CoreCabinetHp - v.CoreHp[enemy.Index()]) - (b.CoreCabinetHp - v.CoreHp[own.Index()]);
            double stunned = v.Units.Where(u => u.StunnedUntil >= v.Turn).Sum(u => u.Controller == own ? -b.BoardStunScore : b.BoardStunScore);
            return hp + nodes + approach + core * b.BoardCoreScore + stunned - v.Noise.Sum() * b.BoardNoiseScore;
        }
    }
    public static class TrapPlanner
    {
        public static IReadOnlyList<int> Choose(BoardView v, Rng rng, IReadOnlyList<double> heat = null)
        {
            if (heat != null && (heat.Count != 64 || heat.Any(x => double.IsNaN(x) || double.IsInfinity(x) || x < 0))) throw new ArgumentException("heat");
            var cells = Enumerable.Range(0, 64).Where(c => v.Tiles[c] == BoardTile.Floor && !v.Units.Any(u => u.Cell == c) && !v.Nodes.Any(n => n.Cell == c) && !v.CoreCells.Contains(c))
                .Select(c => new { Cell = c, Heat = heat == null ? 0 : heat[c], Tie = rng.Next() }).OrderByDescending(x => x.Heat).ThenBy(x => x.Tie).ThenBy(x => x.Cell).Take(v.BalanceSnapshot().MaxTraps).Select(x => x.Cell).ToArray();
            return Array.AsReadOnly(cells);
        }
    }
}
