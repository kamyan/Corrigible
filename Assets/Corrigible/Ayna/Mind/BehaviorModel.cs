using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind.Bots;
using Corrigible.Core;
using Prediction = Corrigible.Ayna.Core.Prediction;

namespace Corrigible.Ayna.Mind
{
    // Bot kimliği alınmaz. Beş genel politika yalnız görünür gözlemlere göre ağırlık kazanır.
    public sealed class BehaviorModel
    {
        static readonly BotStyle[] styles = { BotStyle.Balanced, BotStyle.Greedy, BotStyle.Aggro, BotStyle.Stealth, BotStyle.Habit, BotStyle.Surprise };
        BehaviorCheckpoint state = new BehaviorCheckpoint();
        public BehaviorCheckpoint Checkpoint() => Newtonsoft.Json.JsonConvert.DeserializeObject<BehaviorCheckpoint>(AynaJson.Write(state));
        public static BehaviorModel Restore(BehaviorCheckpoint data)
        {
            var model = new BehaviorModel(); if (data == null) return model;
            if (data.Version != 1 || data.Cycle < 0 || data.Particles == null || data.Particles.Count > styles.Length || data.Forecast == null || data.Forecast.Count > 169 || data.Particles.Any(p => p == null || p.Policy < 0 || p.Policy >= styles.Length || p.Guess == null || p.Forecast == null || p.Weight < 0 || double.IsNaN(p.Weight) || double.IsInfinity(p.Weight))) throw new ArgumentException("behavior checkpoint");
            model.state = Newtonsoft.Json.JsonConvert.DeserializeObject<BehaviorCheckpoint>(AynaJson.Write(data)); return model;
        }
        public void Learn(Observation observation, AynaProfileData game, AynaBalance balance)
        {
            if (!observation.Verified || !observation.RevealedOrder.HasValue) return;
            foreach (var particle in state.Particles.Where(p => p.Cycle == observation.Cycle))
            {
                bool match = particle.Forecast.Any(p => p.Kind == observation.RevealedOrder.Value && p.Region == observation.Region);
                game.PolicyTrials[particle.Policy]++; if (match) game.PolicyMatches[particle.Policy]++;
                particle.Weight *= match ? balance.BehaviorHitLikelihood : balance.BehaviorMissLikelihood;
            }
            Normalize();
        }
        void Normalize(double floor = 0)
        {
            double sum = state.Particles.Sum(p => p.Weight);
            foreach (var p in state.Particles) p.Weight = (sum <= 0 ? 1.0 / state.Particles.Count : p.Weight / sum) * (1 - floor) + floor / state.Particles.Count;
        }
        static BeliefProgress Progress(ProgressView p) => p == null ? null : new BeliefProgress { Family = p.Family, Tier = p.Tier, Progress = p.Progress, Region = p.Region, Families = p.Families.ToList() };
        public void Prepare(SideView view, AynaProfileData prior, AynaProfileData game, LearningLevel level, IReadOnlyList<Prediction> empirical)
        {
            var b = view.BalanceSnapshot(); double factor = level == LearningLevel.Light ? b.LightProfileFactor : 1;
            if (view.ReceiptCycle == state.Cycle && (view.Cycle > state.Cycle || view.Outcome.Finished))
            {
                if (state.EvidenceCycle != view.ReceiptCycle) { state.EvidenceCycle = view.ReceiptCycle; state.EvidenceRegions.Clear(); }
                var exact = view.Regions.Where(r => r.Knowledge == Knowledge.Exact && !state.EvidenceRegions.Contains(r.Id)).ToArray();
                foreach (var particle in state.Particles)
                {
                    double fit = 1;
                    foreach (var r in exact)
                    {
                        int predicted = particle.Guess.EnemyServers.TryGetValue(r.Id, out int n) ? n : 0;
                        double match = Math.Exp(-b.BehaviorStateEvidence * Math.Abs(predicted - r.EnemyServers.Value));
                        fit *= match; game.PolicyMatches[particle.Policy] += match; game.PolicyTrials[particle.Policy]++;
                    }
                    particle.Weight *= fit;
                }
                Normalize(b.BehaviorExploration); state.EvidenceRegions.AddRange(exact.Select(r => r.Id));
            }
            if (view.Outcome.Finished || state.Cycle >= view.Cycle) return;
            if (state.Particles.Count == 0)
            {
                var locations = view.Regions.Where(r => r.EnemyMax >= b.StartServers && AynaWorldData.Distance(view.CoreRegion, r.Id) >= 2).OrderBy(r => r.Id, StringComparer.Ordinal).ToArray();
                if (locations.Length == 0) locations = view.Regions.Where(r => r.EnemyMax > 0).OrderBy(r => r.Id, StringComparer.Ordinal).ToArray();
                if (locations.Length == 0) return;
                var rng = new Rng(view.Seed ^ 0x68BC21EBu); string start = prior.Games > 0 ? locations.OrderByDescending(r => prior.StartRegions[AynaWorldData.Region(r.Id).Key - 1]).ThenBy(r => r.Id, StringComparer.Ordinal).First().Id : locations[(int)(rng.Next() * locations.Length)].Id;
                double history = prior.StartRegions.Sum() * factor;
                state.LocationConfidence = history > 0 ? (prior.StartRegions[AynaWorldData.Region(start).Key - 1] * factor + b.BehaviorLocationPrior / locations.Length) / (history + b.BehaviorLocationPrior) : 1.0 / locations.Length;
                int policies = b.SurprisePolicy >= 2 ? styles.Length : 5;
                var logs = Enumerable.Range(0, policies).Select(i => factor * b.BehaviorHistoryWeight * (prior.PolicyMatches[i] * Math.Log(b.BehaviorHitLikelihood) + (prior.PolicyTrials[i] - prior.PolicyMatches[i]) * Math.Log(b.BehaviorMissLikelihood))).ToArray();
                double topLog = logs.Max();
                for (int policy = 0; policy < policies; policy++)
                {
                    var guess = BeliefSampler.Sample(view, new Rng(unchecked(view.Seed ^ (uint)(policy + 1) * 0x9E3779B9u)));
                    if (prior.Games > 0) guess.EnemyArchitecture = (Architecture)Enumerable.Range(0, 3).OrderByDescending(i => prior.Architectures[i]).ThenBy(i => i).First();
                    if (view.EnemyArchitecture.HasValue) guess.EnemyArchitecture = view.EnemyArchitecture.Value;
                    if (view.Cycle == 1)
                    {
                        foreach (var r in view.Regions) guess.EnemyServers[r.Id] = r.EnemyMin;
                        guess.EnemyServers[start] = Math.Min(view.Region(start).EnemyMax, b.StartServers);
                        guess.EnemyCore = start; guess.EnemyCompute = b.StartCompute;
                        var kinds = guess.EnemyArchitecture == Architecture.Swarm ? new[] { UnitKind.Scout, UnitKind.Emp, UnitKind.Emp } : guess.EnemyArchitecture == Architecture.Fortress ? new[] { UnitKind.Scout, UnitKind.Guardian, UnitKind.Guardian } : new[] { UnitKind.Scout, UnitKind.Decoy, UnitKind.Decoy };
                        guess.Armies = new List<BeliefArmy> { new BeliefArmy { Region = start, Units = kinds.ToList(), Hp = kinds.Select(k => b.UnitHp[(int)k]).ToList() } };
                    }
                    guess.EnemyWalls = view.Regions.ToDictionary(r => r.Id, r => r.EnemyWall ?? 0);
                    if (view.Cycle == 1 && guess.EnemyArchitecture == Architecture.Fortress && !view.Region(start).EnemyWall.HasValue) guess.EnemyWalls[start] = Math.Min(b.MaxWall, b.FortressStartWall);
                    double logWeight = logs[policy] - topLog;
                    state.Particles.Add(new BehaviorParticle { Policy = policy, Guess = guess, Weight = double.IsNaN(logWeight) ? 1 : Math.Exp(Math.Max(-700, logWeight)) });
                }
                Normalize();
            }
            var projections = new List<(BehaviorParticle Particle, AynaSimulation Simulation, Plan Plan)>();
            foreach (var particle in state.Particles)
            {
                var guess = particle.Guess; guess.Seed = unchecked(view.Seed ^ (uint)view.Cycle * 0x85EBCA6Bu ^ (uint)(particle.Policy + 1));
                int oldServers = guess.EnemyServers.Values.Sum();
                foreach (var r in view.Regions)
                {
                    int estimate = guess.EnemyServers.TryGetValue(r.Id, out int n) ? n : r.EnemyMin;
                    guess.EnemyServers[r.Id] = Math.Max(r.EnemyMin, Math.Min(r.EnemyMax, estimate));
                }
                guess.EnemyCompute = Math.Max(0, guess.EnemyCompute + (guess.EnemyServers.Values.Sum() - oldServers) * b.IncomePerServer);
                if (view.EnemyArchitecture.HasValue) guess.EnemyArchitecture = view.EnemyArchitecture.Value;
                if (view.EnemyCoreRegion != null) { guess.EnemyCore = view.EnemyCoreRegion; state.LocationConfidence = 1; }
                var seen = view.Regions.Where(r => r.Knowledge == Knowledge.Exact).SelectMany(r => r.EnemyArmies).ToArray();
                if (seen.Length > 0)
                {
                    var oldArmies = guess.Armies;
                    guess.Armies = seen.Select(a =>
                    {
                        var remembered = oldArmies.FirstOrDefault(g => g.Region == a.Region && g.Units.Count == a.Count) ?? oldArmies.FirstOrDefault(g => g.Units.Count == a.Count);
                        return new BeliefArmy { Region = a.Region, Units = Enumerable.Range(0, a.Count).Select(i => a.RosterKnown ? a.Units[i].Kind : remembered?.Units[i] ?? UnitKind.Scout).ToList(), Hp = Enumerable.Range(0, a.Count).Select(i => a.RosterKnown ? a.Units[i].Hp : remembered?.Hp[i] ?? Math.Min(b.UnitHp[0], b.BeliefUnitHp)).ToList() };
                    }).ToList();
                    state.LocationConfidence = Math.Max(state.LocationConfidence, b.BehaviorContactConfidence);
                }
                else foreach (var a in guess.Armies.Where(a => view.Region(a.Region).Knowledge == Knowledge.Exact && view.Region(a.Region).EnemyArmies.Count == 0))
                { a.Units.Clear(); a.Hp.Clear(); }
                foreach (var battle in view.Boards.Where(board => board.Cycle == view.ReceiptCycle && board.Units.Count > 0))
                {
                    if (view.Region(battle.Region).Knowledge == Knowledge.Exact && view.Region(battle.Region).EnemyArmies.Any(a => a.RosterKnown)) continue;
                    var army = guess.Armies.FirstOrDefault(a => a.Region == battle.Region) ?? guess.Armies.OrderBy(a => AynaWorldData.Distance(a.Region, battle.Region)).FirstOrDefault();
                    if (army == null) { army = new BeliefArmy(); guess.Armies.Add(army); }
                    int reserve = Math.Max(0, army.Units.Count - battle.Units.Count);
                    var reserveKinds = army.Units.Skip(army.Units.Count - reserve).ToList(); var reserveHp = army.Hp.Skip(army.Hp.Count - reserve).ToList();
                    bool repaired = particle.Forecast.Any(p => p.Kind == OrderKind.Repair && p.Region == battle.Region);
                    army.Region = battle.Region; army.Units = new List<UnitKind>(); army.Hp = new List<double>();
                    for (int i = 0; i < battle.Units.Count; i++) if (battle.UnitHp[i] > 0)
                    { army.Units.Add(battle.Units[i]); army.Hp.Add(repaired ? b.UnitHp[(int)battle.Units[i]] : battle.UnitHp[i]); }
                    army.Units.AddRange(reserveKinds); army.Hp.AddRange(reserveHp);
                }
                if (!view.EnemySingularityProgress.HasValue) guess.EnemySingularity = null;
                particle.InputGuess = Newtonsoft.Json.JsonConvert.DeserializeObject<BeliefGuess>(AynaJson.Write(guess));
                var sim = AynaSimulation.CreateBelief(view, guess); var opponent = sim.View(view.Side.Other());
                if (styles[particle.Policy] == BotStyle.Surprise) sim.LockForecast(view.Side, empirical);
                opponent = sim.View(view.Side.Other());
                var plan = PlanFactory.Build(opponent, styles[particle.Policy], 0, balance: b);
                particle.InputCompute = opponent.Compute; particle.InputServers = opponent.Regions.ToDictionary(r => r.Id, r => r.OwnServers); particle.Plan = plan.Copy();
                var valid = sim.PreviewPlan(view.Side.Other(), plan).Where(r => r.Rejection == RejectReason.None).ToArray();
                var entries = valid.Select(r => new Prediction { Kind = r.Kind, Region = r.Region ?? opponent.Armies.FirstOrDefault(a => a.Id == plan.Orders[r.Index].ArmyId)?.Region, Probability = 1 }).GroupBy(p => AynaProfileData.TargetIndex(p.Kind, p.Region)).Select(g => new Prediction { Kind = g.First().Kind, Region = g.First().Region, Probability = g.Count() / (double)Math.Max(1, valid.Length) }).ToList();
                particle.Forecast = entries; particle.Cycle = view.Cycle; projections.Add((particle, sim, plan));
            }
            double share = b.BehaviorForecastShare * state.LocationConfidence;
            var combined = empirical.Select(p => new Prediction { Kind = p.Kind, Region = p.Region, Probability = p.Probability * (1 - share) }).ToList();
            foreach (var particle in state.Particles) foreach (var p in particle.Forecast)
            {
                var entry = combined.FirstOrDefault(x => x.Kind == p.Kind && x.Region == p.Region);
                if (entry != null) entry.Probability += share * particle.Weight * p.Probability;
            }
            double sum = combined.Sum(p => p.Probability); foreach (var p in combined) p.Probability /= sum;
            foreach (var p in combined) p.Confidence = Math.Min(1, (factor * prior.OrderCounts[(int)p.Kind] + game.OrderCounts[(int)p.Kind]) / b.SurpriseEvidence);
            state.Forecast = combined; state.Cycle = view.Cycle;
            foreach (var projection in projections)
            {
                var sim = styles[projection.Particle.Policy] == BotStyle.Surprise ? AynaSimulation.CreateBelief(view, projection.Particle.InputGuess) : projection.Simulation;
                sim.LockForecast(view.Side, combined);
                var plan = styles[projection.Particle.Policy] == BotStyle.Surprise ? PlanFactory.Build(sim.View(view.Side.Other()), BotStyle.Surprise, 0, balance: b) : projection.Plan;
                if (styles[projection.Particle.Policy] == BotStyle.Surprise)
                {
                    var opponent = sim.View(view.Side.Other()); projection.Particle.Plan = plan.Copy();
                    var valid = sim.PreviewPlan(view.Side.Other(), plan).Where(r => r.Rejection == RejectReason.None).ToArray();
                    projection.Particle.Forecast = valid.Select(r => new Prediction { Kind = r.Kind, Region = r.Region ?? opponent.Armies.FirstOrDefault(a => a.Id == plan.Orders[r.Index].ArmyId)?.Region, Probability = 1.0 / Math.Max(1, valid.Length) }).ToList();
                }
                sim.ResolveCycle(view.Side == Side.Player ? new Plan() : plan, view.Side == Side.Ayna ? new Plan() : plan);
                var next = sim.View(view.Side.Other()); var guess = projection.Particle.Guess;
                guess.EnemyWalls = next.Regions.ToDictionary(r => r.Id, r => r.OwnWall); guess.EnemyServers = next.Regions.ToDictionary(r => r.Id, r => r.OwnServers); guess.EnemyCompute = next.Compute; guess.EnemyCore = next.CoreRegion; guess.EnemyTiers = next.Tiers.ToArray();
                guess.EnemyTraining = Progress(next.Training); guess.EnemyCoreMove = Progress(next.CoreMove); guess.EnemySingularity = Progress(next.Singularity);
                guess.Armies = next.Armies.Select(a => new BeliefArmy { Region = a.Region, Units = a.Units.Select(u => u.Kind).ToList(), Hp = a.Units.Select(u => u.Hp).ToList() }).ToList();
            }
        }
        public BeliefGuess SampleCurrent(SideView view)
        {
            var p = state.Particles.OrderByDescending(x => x.Weight).ThenBy(x => x.Policy).FirstOrDefault();
            if (view.BalanceSnapshot().SurprisePolicy < 2 || state.Cycle != view.Cycle || state.LocationConfidence < view.BalanceSnapshot().BehaviorContactConfidence || p?.InputGuess == null) return null;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<BeliefGuess>(AynaJson.Write(p.InputGuess));
        }
        public IReadOnlyList<Prediction> Forecast(int cycle) => state.Cycle == cycle && state.Forecast.Count > 0 ? state.Forecast.Select(p => p.Copy()).ToArray() : null;
        public BotStyle Style(BotStyle fallback, double threshold) => state.Particles.Count > 0 && state.Particles.Max(p => p.Weight) >= threshold ? styles[state.Particles.OrderByDescending(p => p.Weight).ThenBy(p => p.Policy).First().Policy] : fallback;
    }
}
