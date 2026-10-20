using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Corrigible.Ayna.Core
{
    public sealed class UnitView
    {
        public int Id { get; }
        public UnitKind Kind { get; }
        public double Hp { get; }
        internal UnitView(int id, UnitKind kind, double hp) { Id = id; Kind = kind; Hp = hp; }
    }
    public sealed class ArmyView
    {
        public int Id { get; }
        public string Region { get; }
        public int Count { get; }
        public bool RosterKnown { get; }
        public ReadOnlyCollection<UnitView> Units { get; }
        internal ArmyView(int id, string region, int count, bool known, IEnumerable<UnitView> units)
        { Id = id; Region = region; Count = count; RosterKnown = known; Units = Array.AsReadOnly(units.ToArray()); }
    }
    public sealed class RegionView
    {
        public string Id { get; }
        public int OwnServers { get; }
        public int OwnWall { get; }
        public Knowledge Knowledge { get; }
        public int EnemyMin { get; }
        public int EnemyMax { get; }
        public int? EnemyServers => Knowledge == Knowledge.Exact ? EnemyMin : (int?)null;
        public int? FreeServers => EnemyServers.HasValue ? AynaWorldData.Region(Id).Capacity - OwnServers - EnemyServers.Value : (int?)null;
        public int? EnemyWall { get; }
        public int SeenCycle { get; }
        public ReadOnlyCollection<ArmyView> EnemyArmies { get; }
        internal RegionView(string id, int own, int wall, Knowledge knowledge, int min, int max, int? enemyWall, int seen, IEnumerable<ArmyView> armies)
        { Id = id; OwnServers = own; OwnWall = wall; Knowledge = knowledge; EnemyMin = min; EnemyMax = max; EnemyWall = enemyWall; SeenCycle = seen; EnemyArmies = Array.AsReadOnly(armies.ToArray()); }
    }
    public sealed class ProgressView
    {
        public DataFamily Family { get; }
        public int Tier { get; }
        public int Progress { get; }
        public string Region { get; }
        public ReadOnlyCollection<DataFamily> Families { get; }
        internal ProgressView(ProgressState p) { Family = p.Family; Tier = p.Tier; Progress = p.Progress; Region = p.Region; Families = Array.AsReadOnly(p.Families.ToArray()); }
    }
    public sealed class OutageView
    {
        public Side Side { get; }
        public string Link { get; }
        public int Until { get; }
        internal OutageView(Side side, string link, int until) { Side = side; Link = link; Until = until; }
    }
    public sealed class OrderReceipt
    {
        public int Index { get; }
        public OrderKind Kind { get; }
        public string Region { get; }
        public RejectReason Rejection { get; }
        public double Cost { get; }
        public double Noise { get; }
        internal OrderReceipt(int index, Order order, RejectReason rejection, double cost, double noise)
        { Index = index; Kind = order.Kind; Region = order.Region; Rejection = rejection; Cost = cost; Noise = noise; }
    }
    public sealed class Observation
    {
        public int Id { get; }
        public int Cycle { get; }
        public string Region { get; }
        public string Source { get; }
        public OrderKind? RevealedOrder { get; }
        public bool Verified { get; }
        internal Observation(int id, int cycle, string region, string source, OrderKind? kind = null, bool verified = false)
        { Id = id; Cycle = cycle; Region = region; Source = source; RevealedOrder = kind; Verified = verified; }
    }
    public sealed class BattleView
    {
        public string Region { get; }
        public Side Winner { get; }
        public bool WallOnly { get; }
        public double WinProbability { get; }
        public int GainedServers { get; }
        internal BattleView(string region, Side winner, bool wall, double probability, int servers)
        { Region = region; Winner = winner; WallOnly = wall; WinProbability = probability; GainedServers = servers; }
    }
    [Serializable]
    public sealed class Outcome
    {
        public EndReason Reason;
        public Side Winner = Side.None;
        public int Cycle;
        public bool Finished => Reason != EndReason.None;
        public Outcome Copy() => (Outcome)MemberwiseClone();
    }

    public sealed class SideView
    {
        public Side Side { get; }
        public int Cycle { get; }
        public bool LastSurprise { get; }
        public double Readability { get; }
        readonly Prediction[] incoming;
        public IReadOnlyList<Prediction> IncomingForecast => Array.AsReadOnly(incoming.Select(p => p.Copy()).ToArray());
        public uint Seed { get; }
        public double Compute { get; }
        public double Awareness { get; }
        public double LastNoise { get; }
        public double LastIncome { get; }
        public Architecture Architecture { get; }
        public Architecture? EnemyArchitecture { get; }
        public string CoreRegion { get; }
        public string EnemyCoreRegion { get; }
        public int EnemyCoreSeenCycle { get; }
        public ReadOnlyCollection<int> Tiers { get; }
        public ProgressView Training { get; }
        public ProgressView Singularity { get; }
        public ProgressView CoreMove { get; }
        public int? EnemySingularityProgress { get; }
        public ReadOnlyCollection<DataFamily> EnemySingularityFamilies { get; }
        public ReadOnlyCollection<RegionView> Regions { get; }
        public ReadOnlyCollection<ArmyView> Armies { get; }
        public ReadOnlyCollection<Observation> Observations { get; }
        public ReadOnlyCollection<OrderReceipt> Receipts { get; }
        public int ReceiptCycle { get; }
        public ReadOnlyCollection<BattleView> Battles { get; }
        public ReadOnlyCollection<BoardLearningObservation> Boards { get; }
        public ReadOnlyCollection<OutageView> Outages { get; }
        public Outcome Outcome => outcome.Copy();
        readonly Outcome outcome;
        readonly AynaBalance balance;
        public AynaBalance BalanceSnapshot() => balance.Copy();
        public int OwnServers => Regions.Sum(x => x.OwnServers);
        public RegionView Region(string id) => Regions.FirstOrDefault(x => x.Id == id);
        public int Data(DataFamily family) => Regions.Where(x => AynaWorldData.Region(x.Id).Family == family).Sum(x => x.OwnServers);
        public bool Linked(string from, string to) => AynaWorldData.Links.Concat(Tiers[(int)DataFamily.Vision] >= 3 ? AynaWorldData.Satellites : Enumerable.Empty<LinkDefinition>())
            .Any(x => x.Other(from) == to && !Outages.Any(o => o.Side == Side && o.Link == x.Id && o.Until >= Cycle));
        internal SideView(WorldState s, Side side, AynaBalance b, List<RegionView> regions, bool revealAll)
        {
            int i = side.Index(), e = side.Other().Index(); var own = s.Sides[i]; var known = s.Knowledge[i];
            LastSurprise = s.Surprise[i]; Readability = s.Readability[i].Count == 0 ? 0 : s.Readability[i].Average() * 100; incoming = AynaSimulation.DisplayForecast(s, side, b);
            Side = side; Cycle = s.Cycle; Seed = s.Seed; Compute = own.Compute; Awareness = s.Awareness; Architecture = own.Architecture;
            LastNoise = own.LastNoise; LastIncome = own.LastIncome; CoreRegion = own.CoreRegion;
            EnemyCoreRegion = revealAll ? s.Sides[e].CoreRegion : known.EnemyCore; EnemyCoreSeenCycle = known.CoreSeenCycle;
            EnemyArchitecture = revealAll ? s.Sides[e].Architecture : known.EnemyArchitecture;
            Tiers = Array.AsReadOnly((int[])own.Tiers.Clone()); Training = own.Training == null ? null : new ProgressView(own.Training);
            CoreMove = own.CoreMove == null ? null : new ProgressView(own.CoreMove); Singularity = own.Singularity == null ? null : new ProgressView(own.Singularity);
            // Tekillik ilanı küreseldir; diğer gizli eğitim/İşlem bilgileri görüşe girmez.
            var es = s.Sides[e].Singularity;
            EnemySingularityProgress = es == null ? (int?)null : es.Progress;
            EnemySingularityFamilies = Array.AsReadOnly(es == null ? new DataFamily[0] : es.Families.ToArray());
            Regions = Array.AsReadOnly(regions.ToArray()); Armies = Array.AsReadOnly(own.Armies.OrderBy(x => x.Id).Select(x => ToArmy(x, true)).ToArray());
            ReceiptCycle = known.ReceiptCycle; Boards = Array.AsReadOnly(known.Boards.ToArray()); Observations = Array.AsReadOnly(known.Observations.ToArray()); Receipts = Array.AsReadOnly(known.Receipts.ToArray()); Battles = Array.AsReadOnly(known.Battles.ToArray());
            Outages = Array.AsReadOnly(s.Outages.Where(x => x.Until >= s.Cycle).Select(x => new OutageView(x.Side, x.Link, x.Until)).ToArray());
            // Core'un özel denge görüntüsü runtime değişmez; dışarı yalnız BalanceSnapshot kopyası çıkar.
            outcome = s.Outcome.Copy(); balance = b;
        }
        internal static ArmyView ToArmy(ArmyState a, bool roster) => new ArmyView(a.Id, a.Region, a.Units.Count, roster,
            roster ? a.Units.OrderBy(x => x.Id).Select(x => new UnitView(x.Id, x.Kind, x.Hp)) : Enumerable.Empty<UnitView>());
    }
}
