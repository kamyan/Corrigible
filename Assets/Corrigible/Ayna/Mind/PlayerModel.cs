using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Core;
using Corrigible.Ayna.Mind.Bots;
using Prediction = Corrigible.Ayna.Core.Prediction;

namespace Corrigible.Ayna.Mind
{
    public sealed class MindSession
    {
        readonly AynaProfileData prior;
        readonly AynaProfileData game;
        readonly HashSet<int> seenUnits = new HashSet<int>();
        readonly HashSet<string> seenBoards = new HashSet<string>();
        readonly HashSet<int> convertedFeints = new HashSet<int>();
        int seenObservation, lastCycle, openingExpansions, lastReceiptCycle, threatCycle;
        bool sawAttack, completed, legacy;
        BehaviorModel behavior;
        public Rng Rng { get; }
        public LearningLevel Level { get; }
        public AynaProfileData ProfileAtStart => prior.Copy();
        public bool HasEvidence => Level != LearningLevel.Off && (prior.Targets.Sum() + game.Targets.Sum() > 0);
        public MindSession(uint seed, AynaProfileData profile = null, LearningLevel level = LearningLevel.Full)
        {
            if ((uint)level > (uint)LearningLevel.Full) throw new ArgumentException("learning level");
            Rng = new Rng(seed); Level = level;
            prior = level == LearningLevel.Off ? new AynaProfileData() : (profile ?? new AynaProfileData()).Copy();
            game = new AynaProfileData { Architectures = new double[3] };
            if (level != LearningLevel.Off) behavior = new BehaviorModel();
        }
        public void Observe(SideView view)
        {
            if (Level == LearningLevel.Off || completed) return;
            var rules = view.BalanceSnapshot();
            if (view.ReceiptCycle > lastReceiptCycle)
            {
                if (threatCycle > 0 && threatCycle < view.ReceiptCycle)
                {
                    var replies = view.Observations.Where(o => o.Cycle == view.ReceiptCycle && o.Verified && o.RevealedOrder.HasValue).Select(o => o.RevealedOrder.Value).ToArray();
                    int reaction = replies.Contains(OrderKind.Attack) ? 1 : replies.Contains(OrderKind.March) ? 2 : replies.Any(k => k == OrderKind.BuildWall || k == OrderKind.Repair || k == OrderKind.Produce) ? 0 : -1;
                    // Görülmeyen cevap savunma değildir; yalnız doğrulanmış rakip eylemi sayılır.
                    if (reaction >= 0) game.Reactions[reaction]++; threatCycle = 0;
                }
                if (view.Receipts.Any(r => r.Rejection == RejectReason.None && r.Kind == OrderKind.Attack)) threatCycle = view.ReceiptCycle;
                lastReceiptCycle = view.ReceiptCycle;
            }
            foreach (var observation in view.Observations.Where(o => o.Id > seenObservation).OrderBy(o => o.Id))
            {
                if (observation.Source == "feint") game.Feints[0]++;
                if (!observation.Verified || !observation.RevealedOrder.HasValue) continue;
                behavior?.Learn(observation, game, rules);
                var kind = observation.RevealedOrder.Value; int index = AynaProfileData.TargetIndex(kind, observation.Region);
                game.OrderCounts[(int)kind]++; game.Targets[index]++;
                game.CycleTargets[(Math.Min(40, Math.Max(1, observation.Cycle)) - 1) * 169 + index]++;
                if (observation.Region != null)
                {
                    game.RegionCounts[AynaWorldData.Region(observation.Region).Key - 1]++;
                    if (kind == OrderKind.Expand && openingExpansions++ < 3) game.OpeningFamilies[(int)AynaWorldData.Region(observation.Region).Family]++;
                }
                if (kind == OrderKind.Attack)
                {
                    if (!sawAttack) { game.FirstAttack[Math.Min(39, observation.Cycle - 1)]++; sawAttack = true; }
                    foreach (var feint in view.Observations.Where(o => o.Id < observation.Id && o.Source == "feint" && o.Region == observation.Region))
                        if (convertedFeints.Add(feint.Id)) game.Feints[1]++;
                }
            }
            seenObservation = Math.Max(seenObservation, view.Observations.Select(o => o.Id).DefaultIfEmpty(0).Max());
            foreach (var unit in view.Regions.SelectMany(r => r.EnemyArmies).Where(a => a.RosterKnown).SelectMany(a => a.Units))
                if (seenUnits.Add(unit.Id)) game.Units[(int)unit.Kind]++;
            foreach (var board in view.Boards.Where(b => !seenBoards.Contains(b.Id)))
            {
                foreach (int cell in board.Cells) game.BoardHeat[board.Template * 64 + cell]++;
                if (board.FirstTarget >= 0) game.FirstBoardTarget[board.FirstTarget]++;
                // Tahtada ölen ama önceki harita görüşünde açılmamış kadro da gözlemdir.
                for (int i = 0; i < board.Units.Count; i++) if (seenUnits.Add(board.UnitIds[i])) game.Units[(int)board.Units[i]]++;
                seenBoards.Add(board.Id);
            }
            lastCycle = view.Cycle;
            behavior?.Prepare(view, prior, game, Level, PredictCounts(view));
        }
        public IReadOnlyList<Prediction> Predict(SideView view)
        {
            var predictions = (behavior?.Forecast(view.Cycle) ?? PredictCounts(view)).Select(p => p.Copy()).ToArray();
            var b = view.BalanceSnapshot(); double persistence = Level == LearningLevel.Light ? b.LightProfileFactor : 1;
            foreach (var p in predictions) p.Confidence = Math.Min(1, (persistence * prior.OrderCounts[(int)p.Kind] + game.OrderCounts[(int)p.Kind]) / b.SurpriseEvidence);
            return predictions;
        }
        public BeliefGuess SampleBelief(SideView view)
        {
            var fallback = BeliefSampler.Sample(view, Rng);
            return HasEvidence ? behavior?.SampleCurrent(view) ?? fallback : fallback;
        }
        // Yalnız doğrulanmış geçmişten kurulan güncel inanç; canlı gizli dünya veya RNG okunmaz.
        public string LearnedCore(SideView view) => HasEvidence ? behavior?.SampleCurrent(view)?.EnemyCore : null;
        public BotStyle SuggestedStyle(BotStyle fallback, AynaBalance balance) => behavior?.Style(fallback, balance.BehaviorStyleThreshold) ?? fallback;
        IReadOnlyList<Prediction> PredictCounts(SideView view)
        {
            var b = view.BalanceSnapshot(); var oldPrior = PriorModel.Predict(view); var entries = new List<Prediction>();
            double persistence = Level == LearningLevel.Light ? b.LightProfileFactor : 1;
            foreach (OrderKind kind in Enum.GetValues(typeof(OrderKind)))
            {
                bool regional = kind != OrderKind.Train && kind != OrderKind.StartSingularity && kind != OrderKind.Wait;
                foreach (string region in regional ? AynaWorldData.Regions.Select(r => r.Id) : new string[] { null })
                {
                    if (region != null)
                    {
                        var r = view.Region(region);
                        // Yalnız kesin olarak imkânsız hedefler elenir; bilinmeyen alanlar sıfırlanmaz.
                        // Dolu bölgeye ücretli genişleme teklifi de geçerli emirdir; sonuçsuz olması hedefi imkânsız yapmaz.
                        if (kind == OrderKind.Attack && r.OwnServers == 0 ||
                            (kind == OrderKind.BuildWall || kind == OrderKind.MoveCore || kind == OrderKind.Produce || kind == OrderKind.Repair) && r.EnemyMax == 0) continue;
                    }
                    int index = AynaProfileData.TargetIndex(kind, region), context = (Math.Min(40, view.Cycle) - 1) * 169 + index;
                    double seedWeight = oldPrior.FirstOrDefault(p => p.Kind == kind && p.Region == region)?.Probability ?? 1.0 / 169;
                    double mass = b.ModelPriorMass * seedWeight;
                    if (Level != LearningLevel.Off) mass += persistence * (prior.Targets[index] + b.ModelContextWeight * prior.CycleTargets[context]) + b.ModelGameWeight * (game.Targets[index] + b.ModelContextWeight * game.CycleTargets[context]);
                    entries.Add(new Prediction { Kind = kind, Region = region, Probability = mass });
                }
            }
            double sum = entries.Sum(p => p.Probability); foreach (var entry in entries) entry.Probability /= sum;
            return entries.AsReadOnly();
        }
        public static double TargetProbability(IEnumerable<Prediction> forecast, OrderKind kind, string region)
        {
            double sum = forecast.Where(p => p.Kind == kind).Sum(p => p.Probability);
            return sum == 0 ? 0 : forecast.Where(p => p.Kind == kind && p.Region == region).Sum(p => p.Probability) / sum;
        }
        public double[] Heat(BoardView board)
        {
            var heat = new double[64]; if (Level == LearningLevel.Off) return heat;
            double factor = Level == LearningLevel.Light ? board.BalanceSnapshot().LightProfileFactor : 1;
            for (int cell = 0; cell < 64; cell++)
            {
                int canonical = board.Side.Other() == board.IntentSide ? cell : 63 - cell;
                heat[BoardTemplates.TransformCell(cell, board.Transform)] = factor * prior.BoardHeat[board.Template * 64 + canonical] + game.BoardHeat[board.Template * 64 + canonical];
            }
            double sum = heat.Sum(); if (sum > 0) for (int i = 0; i < 64; i++) heat[i] /= sum;
            return heat;
        }
        public AynaProfileData Complete(SideView revealedEndView, StartChoice revealedOpening, string matchId)
        {
            if (!revealedEndView.Outcome.Finished || string.IsNullOrEmpty(matchId)) throw new InvalidOperationException("unfinished match");
            if (Level == LearningLevel.Off) return new AynaProfileData();
            Observe(revealedEndView);
            if (prior.CompletedGames.Contains(matchId)) return prior.Copy();
            if (revealedOpening == null || AynaWorldData.Region(revealedOpening.Region) == null || (uint)revealedOpening.Architecture > 2) throw new ArgumentException("debrief");
            game.Architectures[(int)revealedOpening.Architecture] = 1; game.StartRegions[AynaWorldData.Region(revealedOpening.Region).Key - 1] = 1;
            var merged = prior.Copy(); double weight = revealedEndView.BalanceSnapshot().ProfileMergeWeight * (Level == LearningLevel.Light ? revealedEndView.BalanceSnapshot().LightProfileFactor : 1);
            foreach (var field in typeof(AynaProfileData).GetFields().Where(f => f.FieldType == typeof(double[])))
            {
                var old = (double[])field.GetValue(merged); var current = (double[])field.GetValue(game);
                for (int i = 0; i < old.Length; i++) old[i] = (1 - weight) * old[i] + weight * current[i];
            }
            merged.Generation++; merged.Games++; merged.Score += revealedEndView.Outcome.Winner == revealedEndView.Side ? 1 : revealedEndView.Outcome.Winner == Side.None ? .5 : 0;
            merged.CompletedGames = prior.CompletedGames.Concat(new[] { matchId }).Reverse().Take(32).Reverse().ToArray(); completed = true; merged.Validate(); return merged;
        }
        public MindCheckpoint Checkpoint() => new MindCheckpoint { Version = legacy ? 1 : 2, Behavior = behavior?.Checkpoint(), Prior = prior.Copy(), Game = game.Copy(), Level = Level, RngState = Rng.State, LastReceiptCycle = lastReceiptCycle, ThreatCycle = threatCycle, ConvertedFeints = convertedFeints.OrderBy(x => x).ToArray(), SeenObservation = seenObservation, LastObservedCycle = lastCycle, OpeningExpansions = openingExpansions, SeenUnits = seenUnits.OrderBy(x => x).ToArray(), SeenBoards = seenBoards.OrderBy(x => x, StringComparer.Ordinal).ToArray(), SawAttack = sawAttack, Completed = completed };
        public static MindSession Restore(MindCheckpoint data)
        {
            if (data == null || (data.Version != 1 && data.Version != 2) || data.Prior == null || data.Game == null || data.SeenObservation < 0 || data.LastObservedCycle < 0 || data.SeenUnits == null || data.SeenBoards == null) throw new ArgumentException("mind checkpoint");
            data.Prior = data.Prior.Copy(); data.Game = data.Game.Copy();
            var s = new MindSession(1, data.Prior, data.Level); s.Rng.State = data.RngState; s.legacy = data.Version == 1; s.behavior = s.legacy || data.Level == LearningLevel.Off ? null : BehaviorModel.Restore(data.Behavior);
            var gameCopy = data.Game.Copy(); foreach (var field in typeof(AynaProfileData).GetFields()) field.SetValue(s.game, field.GetValue(gameCopy));
            s.lastReceiptCycle = data.LastReceiptCycle; s.threatCycle = data.ThreatCycle; s.convertedFeints.UnionWith(data.ConvertedFeints ?? Array.Empty<int>()); s.seenObservation = data.SeenObservation; s.lastCycle = data.LastObservedCycle; s.openingExpansions = data.OpeningExpansions; s.sawAttack = data.SawAttack; s.completed = data.Completed;
            s.seenUnits.UnionWith(data.SeenUnits); s.seenBoards.UnionWith(data.SeenBoards); return s;
        }
    }
}
