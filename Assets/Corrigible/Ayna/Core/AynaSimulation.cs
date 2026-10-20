using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Ayna.Core
{
    public sealed partial class AynaSimulation
    {
        readonly WorldState state;
        readonly AynaBalance balance;
        readonly AynaRecord record;
        internal WorldState State => state;
        internal string StateHash => state.Hash();
        public AynaRecord Record => record?.Copy();
        public Outcome Outcome => state.Outcome.Copy();

        AynaSimulation(WorldState world, AynaBalance rules, AynaRecord rec) { state = world; balance = rules; record = rec; }
        public static AynaSimulation Create(StartSetup setup, AynaBalance rules = null, AynaProfileData profile = null)
        {
            if (setup == null || setup.Player == null || setup.Ayna == null || setup.Settings == null) throw new ArgumentNullException(nameof(setup));
            var start = setup.Copy(); var b = (rules ?? new AynaBalance()).Copy(); b.Validate();
            if (!Enum.IsDefined(typeof(Architecture), start.Player.Architecture) || !Enum.IsDefined(typeof(Architecture), start.Ayna.Architecture)) throw new ArgumentException("architecture");
            if (AynaWorldData.Region(start.Player.Region) == null || AynaWorldData.Region(start.Ayna.Region) == null) throw new ArgumentException("region");
            if (AynaWorldData.Distance(start.Player.Region, start.Ayna.Region) < 2)
            {
                start.Ayna.Region = start.Ayna.RankedRegions.Concat(AynaWorldData.Regions.Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal))
                    .First(x => AynaWorldData.Distance(start.Player.Region, x) >= 2);
            }
            var s = EmptyWorld(start.Seed);
            var sim = new AynaSimulation(s, b, new AynaRecord { Setup = start.Copy(), Balance = b.Copy(), ProfileAtStart = start.Settings.Learning == LearningLevel.Off ? new AynaProfileData() : (profile ?? new AynaProfileData()).Copy() });
            for (int i = 0; i < 2; i++)
            {
                var c = i == 0 ? start.Player : start.Ayna; var own = s.Sides[i];
                own.Architecture = c.Architecture; own.CoreRegion = c.Region; own.Compute = b.StartCompute; own.Tiers[(int)DataFamily.Vision] = 1;
                var r = s.Region(c.Region); r.Owned[i] = b.StartServers; r.Free -= b.StartServers;
                var kinds = c.Architecture == Architecture.Swarm ? new[] { UnitKind.Scout, UnitKind.Emp, UnitKind.Emp } : c.Architecture == Architecture.Fortress ?
                    new[] { UnitKind.Scout, UnitKind.Guardian, UnitKind.Guardian } : new[] { UnitKind.Scout, UnitKind.Decoy, UnitKind.Decoy };
                own.Tiers[(int)(c.Architecture == Architecture.Swarm ? DataFamily.Energy : c.Architecture == Architecture.Fortress ? DataFamily.Movement : DataFamily.Language)] = 1;
                if (c.Architecture == Architecture.Fortress) r.Wall[i] = Math.Min(b.MaxWall, b.FortressStartWall);
                var army = new ArmyState { Id = AllocateArmy(own), Region = c.Region };
                foreach (var kind in kinds) army.Units.Add(new UnitState { Id = AllocateUnit(own), Kind = kind, Hp = b.UnitHp[(int)kind] });
                own.Armies.Add(army);
            }
            for (int i = 0; i < 2; i++)
            {
                var counters = i == 0 ? start.PlayerCounters : start.AynaCounters;
                int max = start.Settings.Learning == LearningLevel.Off ? 0 : start.Settings.Learning == LearningLevel.Light ? b.LightOpeningCounters : b.MaxOpeningCounters;
                if (counters.Count > max || counters.Count(c => c.Wall) > 1 || counters.Count(c => !c.Wall) > 1 || counters.Any(c => (uint)c.Family > 4)) throw new ArgumentException("opening counters");
                foreach (var c in counters)
                    if (c.Wall) s.Region(s.Sides[i].CoreRegion).Wall[i] = Math.Min(b.MaxWall, s.Region(s.Sides[i].CoreRegion).Wall[i] + b.CounterWall);
                    else s.Sides[i].Tiers[(int)c.Family] = Math.Max(1, s.Sides[i].Tiers[(int)c.Family]);
            }
            sim.RefreshKnowledge(); return sim;
        }

        static WorldState EmptyWorld(uint seed)
        {
            var s = new WorldState { Seed = seed, WorldRng = new Rng(seed) };
            s.Regions = AynaWorldData.Regions.OrderBy(x => x.Id, StringComparer.Ordinal).Select(x => new RegionState { Id = x.Id, Free = x.Capacity }).ToList();
            for (int i = 0; i < 2; i++) { s.Sides[i].NextArmyId = i + 1; s.Sides[i].NextUnitId = i + 1; }
            foreach (var k in s.Knowledge) k.Regions = s.Regions.Select(x => new KnownRegion { Id = x.Id }).ToList();
            return s;
        }

        public SideView View(Side side, bool revealAll = false)
        {
            int i = side.Index(), e = side.Other().Index();
            if (revealAll && !state.Outcome.Finished) throw new InvalidOperationException("revealAll");
            var regions = new List<RegionView>();
            foreach (var r in state.Regions)
            {
                var k = state.Knowledge[i].Regions.First(x => x.Id == r.Id);
                bool exact = revealAll || IsExact(i, r.Id);
                bool nearby = state.Regions.Any(x => x.Owned[i] > 0 && Linked(i, x.Id, r.Id));
                var knowledge = exact ? Knowledge.Exact : nearby || k.SeenCycle >= 0 ? Knowledge.Estimate : Knowledge.Unknown;
                int cap = AynaWorldData.Region(r.Id).Capacity - r.Owned[i];
                int min = 0, max = cap;
                if (exact) min = max = r.Owned[e];
                else if (k.SeenCycle >= 0)
                {
                    int span = Math.Max(1, state.Cycle - k.SeenCycle) * balance.NetworkExpandLimit;
                    min = Math.Max(0, k.EnemyServers - span); max = Math.Min(cap, k.EnemyServers + span);
                }
                var armies = exact ? state.Sides[e].Armies.Where(x => x.Region == r.Id && x.Units.Count > 0).OrderBy(x => x.Id).Select(x => SideView.ToArmy(x, revealAll || HasArmy(i, r.Id) || Scanned(r.Id))) :
                    k.Armies.AsEnumerable();
                regions.Add(new RegionView(r.Id, r.Owned[i], r.Wall[i], knowledge, min, max, exact ? r.Wall[e] : (int?)null, exact ? state.Cycle : k.SeenCycle, armies));
            }
            return new SideView(state, side, balance, regions, revealAll);
        }

        bool Scanned(string id) => state.ScannedRegion == id && state.ScanUntil >= state.Cycle;
        bool HasArmy(int i, string id) => state.Sides[i].Armies.Any(x => x.Region == id && x.Units.Count > 0);
        bool IsExact(int i, string id) => state.Region(id).Owned[i] > 0 || HasArmy(i, id) || Scanned(id);
        internal bool Linked(int i, string from, string to) => AynaWorldData.Links.Concat(state.Sides[i].Tiers[(int)DataFamily.Vision] >= 3 ? AynaWorldData.Satellites : Enumerable.Empty<LinkDefinition>())
            .Any(x => x.Other(from) == to && !state.Outages.Any(o => (int)o.Side == i && o.Link == x.Id && o.Until >= state.Cycle));

        void RefreshKnowledge()
        {
            for (int i = 0; i < 2; i++)
            {
                int e = 1 - i; var k = state.Knowledge[i];
                foreach (var r in state.Regions.Where(x => IsExact(i, x.Id)))
                {
                    var known = k.Regions.First(x => x.Id == r.Id); known.EnemyServers = r.Owned[e]; known.EnemyWall = r.Wall[e]; known.SeenCycle = state.Cycle;
                    known.Armies = state.Sides[e].Armies.Where(x => x.Region == r.Id && x.Units.Count > 0).OrderBy(x => x.Id).Select(x => SideView.ToArmy(x, HasArmy(i, r.Id) || Scanned(r.Id))).ToList();
                    if (HasArmy(i, r.Id) || Scanned(r.Id))
                    {
                        if (state.Sides[e].CoreRegion == r.Id) { k.EnemyCore = r.Id; k.CoreSeenCycle = state.Cycle; k.EnemyArchitecture = state.Sides[e].Architecture; }
                        else if (k.EnemyCore == r.Id) { k.EnemyCore = null; k.CoreSeenCycle = state.Cycle; }
                    }
                }
            }
        }

        void Observe(int i, string region, string source, OrderKind? kind = null, bool verified = false) => state.Knowledge[i].Observations.Add(new Observation(state.Knowledge[i].NextObservationId++, state.Cycle, region, source, kind, verified));

        public IReadOnlyList<OrderReceipt> PreviewPlan(Side side, Plan plan)
        {
            // Doğrulama saf çalışır; bütçe ve tekrar kümeleri yalnız yereldir.
            int i = side.Index(); var validated = ValidatePlan(i, plan, preview: true);
            var hidden = validated.Where(x => x.Valid && x.Order.Kind == OrderKind.Hide).Select(x => x.Order.Region).ToHashSet();
            return validated.Select(x => new OrderReceipt(x.Index, x.Order, x.Rejection, x.Cost,
                x.Valid && !hidden.Contains(x.Order.Region) ? x.Noise * balance.NoiseMultiplier(state.Sides[i].Architecture) : 0)).ToList().AsReadOnly();
        }

        public void ResolveCycle(Plan player, Plan ayna)
        {
            BeginCycle(player, ayna);
            while (IsResolving) if (!ResolvePendingAuto()) throw new InvalidOperationException("pending board");
        }
        public void BeginCycle(Plan player, Plan ayna)
        {
            if (IsResolving) throw new InvalidOperationException("resolving");
            if (state.Outcome.Finished) throw new InvalidOperationException("finished");
            if (player == null || ayna == null) throw new ArgumentNullException(nameof(player));
            var p = player.Copy(); var a = ayna.Copy();
            var accepted = new[] { ValidatePlan(0, p), ValidatePlan(1, a) };
            var exactBefore = new[] { state.Regions.Where(x => IsExact(0, x.Id)).Select(x => x.Id).ToHashSet(), state.Regions.Where(x => IsExact(1, x.Id)).Select(x => x.Id).ToHashSet() };
            var hidden = accepted.Select(list => list.Where(x => x.Valid && x.Order.Kind == OrderKind.Hide).Select(x => x.Order.Region).ToHashSet()).ToArray();
            for (int i = 0; i < 2; i++)
            {
                var own = state.Sides[i]; var k = state.Knowledge[i]; k.Battles.Clear(); k.ReceiptCycle = state.Cycle; own.LastNoise = 0;
                own.Compute -= accepted[i].Where(x => x.Valid).Sum(x => x.Cost);
                k.Receipts = accepted[i].Select(x => new OrderReceipt(x.Index, x.Order, x.Rejection, x.Cost, x.Valid && !hidden[i].Contains(x.Order.Region) ? x.Noise * balance.NoiseMultiplier(own.Architecture) : 0)).ToList();
                foreach (var v in accepted[i].Where(x => x.Valid))
                {
                    if (!hidden[i].Contains(v.Order.Region)) own.LastNoise += v.Noise * balance.NoiseMultiplier(own.Architecture);
                    int observer = 1 - i;
                    if (hidden[i].Contains(OrderTarget(i, v.Order))) continue;
                    if (v.Order.Kind == OrderKind.Feint) Observe(observer, v.Order.Region, "feint", OrderKind.Attack, false);
                    else if (v.Order.Kind == OrderKind.StartSingularity) Observe(observer, null, "singularity", OrderKind.StartSingularity, true);
                    else if (OrderTarget(i, v.Order) is string target && exactBefore[observer].Contains(target)) Observe(observer, target, "reveal", v.Order.Kind, true);
                    else if (v.Noise > 0 && v.Order.Region != null && state.Regions.Any(r => r.Owned[observer] > 0 && Linked(observer, r.Id, v.Order.Region))) Observe(observer, v.Order.Region, "activity");
                }
            }
            ApplyForecastRewards(accepted);
            RecordInput(new AynaInput { Kind = "begin", Player = p, Ayna = a });
            MoveArmies(accepted); ResolveExpansions(accepted);
            state.Pending = new ResolutionState { Orders = accepted, Hidden = hidden, Player = p, Ayna = a };
            AdvanceBattles();
        }
        void FinishCycle(List<ValidatedOrder>[] accepted, HashSet<string>[] hidden, Plan p, Plan a)
        {
            for (int i = 0; i < 2; i++) { EnsureCore(i); ApplyDevelopment(i, accepted[i]); EnsureCore(i); }
            for (int i = 0; i < 2; i++) { var own = state.Sides[i]; own.LastIncome = state.Servers(i) * balance.IncomePerServer + (own.CoreRegion == null ? 0 : balance.CoreIncome); own.Compute += own.LastIncome; }
            ApplyAwareness(accepted, hidden); CheckEnding(); RefreshKnowledge();
            if (record != null) record.Cycles.Add(new CycleRecord { Cycle = state.Cycle, Player = p, Ayna = a });
            if (!state.Outcome.Finished) state.Cycle++;
        }

        static int AllocateArmy(SideState side) { int id = side.NextArmyId; side.NextArmyId += 2; return id; }
        static int AllocateUnit(SideState side) { int id = side.NextUnitId; side.NextUnitId += 2; return id; }

        void EnsureCore(int i)
        {
            var own = state.Sides[i]; if (own.CoreDestroyed) { own.CoreRegion = null; own.CoreMove = null; return; }
            if (own.CoreRegion != null && state.Region(own.CoreRegion).Owned[i] > 0) return;
            own.CoreRegion = state.Regions.Where(x => x.Owned[i] > 0).OrderByDescending(x => x.Owned[i]).ThenBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault()?.Id;
            own.CoreMove = null;
        }

        void CheckEnding()
        {
            var s = state;
            if (s.Awareness >= balance.ShutdownThreshold) { Finish(EndReason.Shutdown, Side.None); return; }
            var erased = s.Sides.Select(x => x.CoreDestroyed || x.CoreRegion == null).ToArray();
            if (erased.Any(x => x)) { Finish(EndReason.Erasure, erased[0] == erased[1] ? Side.None : erased[0] ? Side.Ayna : Side.Player); return; }
            var singular = s.Sides.Select(x => x.Singularity != null && x.Singularity.Progress >= balance.SingularityDuration).ToArray();
            if (singular.Any(x => x)) { Finish(EndReason.Singularity, singular[0] == singular[1] ? Side.None : singular[0] ? Side.Player : Side.Ayna); return; }
            int p = s.Servers(0), a = s.Servers(1);
            if (p >= AynaWorldData.TotalServers * balance.DominanceShare || a >= AynaWorldData.TotalServers * balance.DominanceShare) { Finish(EndReason.Dominance, p == a ? Side.None : p > a ? Side.Player : Side.Ayna); return; }
            if (s.Cycle >= balance.MaxCycles) Finish(EndReason.Time, p == a ? Side.None : p > a ? Side.Player : Side.Ayna);
        }
        void Finish(EndReason reason, Side winner) { state.Outcome = new Outcome { Reason = reason, Winner = winner, Cycle = state.Cycle }; }
    }
}
