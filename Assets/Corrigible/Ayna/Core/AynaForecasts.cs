using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Newtonsoft.Json;

namespace Corrigible.Ayna.Core
{
    [JsonObject(MemberSerialization.Fields)] internal sealed class ForecastState
    {
        internal int Cycle;
        internal List<Prediction> Entries = new List<Prediction>();
    }
    public sealed partial class AynaSimulation
    {
        public bool LockForecast(Side observer, IEnumerable<Prediction> predictions)
        {
            int i = observer.Index();
            if (IsResolving || Outcome.Finished || state.Forecasts[i].Cycle == state.Cycle || predictions == null) return false;
            var list = predictions.Select(p => p?.Copy()).ToList();
            if (list.Count == 0 || list.Count > 169 || list.Any(p => p == null || (uint)p.Kind > (uint)OrderKind.Wait || p.Region != null && AynaWorldData.Region(p.Region) == null || double.IsNaN(p.Probability) || double.IsInfinity(p.Probability) || p.Probability < 0 || p.Probability > 1 || double.IsNaN(p.Confidence) || double.IsInfinity(p.Confidence) || p.Confidence < 0 || p.Confidence > 1) || Math.Abs(list.Sum(p => p.Probability) - 1) > 1e-9 || list.Select(p => AynaProfileData.TargetIndex(p.Kind, p.Region)).Distinct().Count() != list.Count) return false;
            state.Forecasts[i] = new ForecastState { Cycle = state.Cycle, Entries = list };
            RecordInput(new AynaInput { Kind = "forecast", Side = observer, Predictions = list }); return true;
        }
        internal static double Conditional(IEnumerable<Prediction> entries, OrderKind kind, string region)
        {
            double sum = entries.Where(p => p.Kind == kind).Sum(p => p.Probability);
            return sum <= 0 ? 0 : entries.Where(p => p.Kind == kind && p.Region == region).Sum(p => p.Probability) / sum;
        }
        string OrderTarget(int side, Order order) => order.Region ?? (order.ArmyId == 0 ? null : state.Sides[side].Armies.FirstOrDefault(a => a.Id == order.ArmyId)?.Region);
        void ApplyForecastRewards(List<ValidatedOrder>[] accepted)
        {
            state.Surprise = new bool[2]; state.FirstStrike = new bool[2]; state.FirstStrikeRegion = new string[2];
            for (int i = 0; i < 2; i++)
            {
                var forecast = state.Forecasts[1 - i]; if (forecast.Cycle != state.Cycle) continue;
                var scores = accepted[i].Where(o => o.Valid && o.Order.Kind != OrderKind.Wait).Select(o => Conditional(forecast.Entries, o.Order.Kind, OrderTarget(i, o.Order))).ToArray();
                if (scores.Length == 0) continue;
                var history = state.Readability[i]; history.Add(scores.Average()); while (history.Count > balance.ReadabilityWindow) history.RemoveAt(0);
                double probability = scores.Min(), reward = balance.SurpriseCompute; string rewardRegion = null;
                if (balance.SurprisePolicy >= 2)
                {
                    // Ana taahhüt: en pahalı anlamlı bölgesel emir; politika 4 eş bedelli emirleri birlikte değerlendirir.
                    // Sonuçsuz dolu bölge teklifleri ve yanına eklenen ucuz yem emirleri ödül üretmez.
                    var primary = accepted[i].Where(o => o.Valid && o.Cost > 0 && OrderTarget(i, o.Order) != null && o.Order.Kind != OrderKind.Wait &&
                        (o.Order.Kind != OrderKind.Expand || state.Region(o.Order.Region).Free > 0))
                        .OrderByDescending(o => o.Cost).ThenBy(o => o.Index).FirstOrDefault();
                    if (primary == null) continue;
                    string target = OrderTarget(i, primary.Order); rewardRegion = target;
                    var predicted = forecast.Entries.FirstOrDefault(p => p.Kind == primary.Order.Kind && p.Region == target);
                    if (predicted == null || predicted.Confidence < 1) continue;
                    probability = Conditional(forecast.Entries, primary.Order.Kind, target);
                    if (balance.SurprisePolicy >= 4)
                    {
                        var peers = accepted[i].Where(o => o.Valid && o.Cost == primary.Cost && OrderTarget(i, o.Order) != null &&
                            (o.Order.Kind != OrderKind.Expand || state.Region(o.Order.Region).Free > 0)).ToArray();
                        if (peers.Any(o => forecast.Entries.FirstOrDefault(p => p.Kind == o.Order.Kind && p.Region == OrderTarget(i, o.Order))?.Confidence != 1)) continue;
                        // Aynı türdeki ana hedef kümesinin toplam olasılığı: yalnız sıra değişikliği Sürpriz değildir.
                        probability = peers.GroupBy(o => o.Order.Kind).Max(g =>
                        {
                            var targets = g.Select(o => OrderTarget(i, o.Order)).Distinct().ToArray();
                            double mass = forecast.Entries.Where(p => p.Kind == g.Key).Sum(p => p.Probability);
                            return mass <= 0 ? 0 : forecast.Entries.Where(p => p.Kind == g.Key && targets.Contains(p.Region)).Sum(p => p.Probability) / mass;
                        });
                        rewardRegion = peers.Select(o => OrderTarget(i, o.Order)).Distinct().Count() == 1 ? target : null;
                    }
                    reward = Math.Min(balance.SurpriseCompute, primary.Cost * balance.SurpriseRebateFraction);
                }
                if (probability < balance.SurpriseThreshold)
                { state.Surprise[i] = state.FirstStrike[i] = true; state.FirstStrikeRegion[i] = rewardRegion; state.Sides[i].Compute += reward; }
            }
        }
        internal static Prediction[] DisplayForecast(WorldState state, Side target, AynaBalance balance)
        {
            var f = state.Forecasts[target.Other().Index()]; if (f.Cycle != state.Cycle) return Array.Empty<Prediction>();
            var entries = f.Entries.Select(p => p.Copy()).ToArray();
            if (state.Sides[target.Index()].Tiers[(int)DataFamily.Language] >= 3)
            {
                // Yalnız görüntü kopyası: dünya/akıl RNG'si ve gerçek ödül hesabı değişmez.
                var rng = new Rng(state.Seed ^ (uint)state.Cycle ^ (uint)(target.Index() + 1) * 0x9E3779B9u);
                var indices = Enumerable.Range(0, entries.Length).Select(i => (Id: i, Rank: rng.Next())).OrderBy(x => x.Rank).Take(entries.Length / 2);
                foreach (var index in indices) entries[index.Id].Probability *= Math.Max(0, 1 + (rng.Next() * 2 - 1) * balance.LanguageForecastNoise);
                double sum = entries.Sum(p => p.Probability); if (sum > 0) foreach (var p in entries) p.Probability /= sum;
            }
            return entries;
        }
    }
}
