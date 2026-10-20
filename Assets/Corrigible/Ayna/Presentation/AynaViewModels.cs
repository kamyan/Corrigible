using System;
using System.Linq;
using Corrigible.Ayna.Core;

namespace Corrigible.Ayna.Presentation
{
    public enum AynaPhase { Start, Planning, Reveal, Conflict, Roster, Board, BoardResult, Results, End }
    [Serializable] public sealed class RegionVm { public string Id; public int Capacity, Own, EnemyMin, EnemyMax; public Knowledge Knowledge; public DataFamily Family; public bool Core; }
    [Serializable] public sealed class ReceiptVm { public OrderKind Kind; public string Region; public RejectReason Rejection; public double Cost, Noise; }
    [Serializable] public sealed class UnitVm { public int Id; public UnitKind Kind; public double Hp, MaxHp; public int Cell; public Side Side, Controller; public bool Moved, Acted; }
    [Serializable] public sealed class ResearchVm { public DataFamily Family; public int Tier, Servers, Required; public double Cost; public int Duration, Progress; public bool Complete, Running, Available; }
    [Serializable] public sealed class ProductionVm { public UnitKind Kind; public double Cost; public bool Available; }
    [Serializable] public sealed class ArmyVm { public int Id; public string Region; public UnitVm[] Units; }
    [Serializable] public sealed class A1StartVm { public int Generation, Games; public double Score; public LearningLevel Learning; public bool HasMemory; public string[] Regions; }
    [Serializable] public sealed class A2PlanningVm { public int Cycle, Servers, DominanceTarget; public double Compute, Income, Cost, Noise, Awareness, Readability; public RegionVm[] Regions; public Order[] Queue; public ReceiptVm[] Preview; public Prediction[] Predictions; }
    [Serializable] public sealed class A3RevealVm { public int Cycle; public ReceiptVm[] OwnOrders; public Observation[] EnemySignals; public bool Surprise; }
    [Serializable] public sealed class A4ConflictVm { public string Region; public bool Pending; public BattleView[] Resolved; }
    [Serializable] public sealed class A5RosterVm { public string Region; public int Slots; public UnitVm[] Available, KnownEnemy; public int UnknownEnemy; }
    [Serializable] public sealed class A6BoardVm { public int Turn, MaxTurns; public BoardPhase Phase; public Side IntentSide; public BoardTile[] Tiles; public UnitVm[] Units; public BoardNodeView[] Nodes; public BoardTrapView[] VisibleTraps; public BoardIntentView[] VisibleIntents; public BoardCommand[] DraftIntents = Array.Empty<BoardCommand>(); public int[] CoreCells; public double[] CoreHp; }
    [Serializable] public sealed class A7BattleResultVm { public bool Available; public Side Winner; public int[] Nodes; public double[] Noise; public UnitVm[] Survivors; }
    [Serializable] public sealed class A8ResearchVm { public ResearchVm[] Nodes; public bool CanStartSingularity, SingularityStarted; public int SingularityProgress, SingularityRequired, SingularityFamilies, TrainedFamilies; public double SingularityCost; }
    [Serializable] public sealed class A9ArmiesVm { public ArmyVm[] Armies; public ProductionVm[] Production; public double RepairCost, NewArmyCost, MarchCost; public int MaxArmies, MaxUnits; public bool CanUseSatellites; }
    [Serializable] public sealed class A10ModelVm { public int Generation; public LearningLevel Learning; public Prediction[] TopThree; public double[] Readability, OrderCounts, Heat; }
    [Serializable] public sealed class A11SingularityVm { public bool Announced; public int Progress, Required; public DataFamily[] Families; public string[] PossibleTargets; }
    [Serializable] public sealed class A12EndVm { public bool Finished, ProfileCommitted; public Outcome Outcome; public int Decisions, Hits, Surprises, Generation; public double[] Readability; }
    [Serializable] public sealed class AynaScreens
    {
        public AynaPhase Phase;
        public A1StartVm A1; public A2PlanningVm A2; public A3RevealVm A3; public A4ConflictVm A4;
        public A5RosterVm A5; public A6BoardVm A6; public A7BattleResultVm A7; public A8ResearchVm A8;
        public A9ArmiesVm A9; public A10ModelVm A10; public A11SingularityVm A11; public A12EndVm A12;
    }
    // Girdiler SideView(Player) ve BoardView(Player) ile sınırlıdır; gizli dünya/kayıt alınmaz.
    public static class AynaViewModels
    {
        public static UnitVm Unit(UnitView u) => new UnitVm { Id = u.Id, Kind = u.Kind, Hp = u.Hp, Cell = -1, Side = Side.Player, Controller = Side.Player };
        public static UnitVm Unit(BoardUnitView u) => new UnitVm { Id = u.Id, Kind = u.Kind, Hp = u.Hp, Cell = u.Cell, Side = u.Side, Controller = u.Controller, Moved = u.Moved, Acted = u.Acted };
        static ReceiptVm Receipt(OrderReceipt r) => new ReceiptVm { Kind = r.Kind, Region = r.Region, Rejection = r.Rejection, Cost = r.Cost, Noise = r.Noise };
        public static A1StartVm Start(AynaProfileData profile, LearningLevel level) => new A1StartVm { Generation = profile.Generation + 1, Games = profile.Games, Score = profile.Score, Learning = level, HasMemory = level != LearningLevel.Off && profile.Games > 0, Regions = AynaWorldData.Regions.Select(r => r.Id).ToArray() };
        public static AynaScreens Build(SideView v, BoardView board, AynaProfileData memory, AynaPhase phase, LearningLevel level,
            Plan queue, OrderReceipt[] preview, string pendingRegion, A7BattleResultVm lastBattle, double[] history, int decisions, int hits, int surprises, bool committed, double[] learnedOrders = null, double[] heat = null)
        {
            if (v == null || v.Side != Side.Player || board != null && board.Side != Side.Player) throw new ArgumentException("player view required");
            var b = v.BalanceSnapshot(); var ownArmy = v.Armies.FirstOrDefault(a => a.Region == pendingRegion);
            var enemy = v.Region(pendingRegion)?.EnemyArmies ?? Array.Empty<ArmyView>().ToList().AsReadOnly();
            var knownEnemy = enemy.Where(a => a.RosterKnown).SelectMany(a => a.Units).Select(u => new UnitVm { Id = u.Id, Kind = u.Kind, Hp = u.Hp, Side = Side.Ayna, Cell = -1 }).ToArray();
            var research = Enumerable.Range(0, 5).SelectMany(f => Enumerable.Range(1, 3).Select(t => new ResearchVm { Family = (DataFamily)f, Tier = t, Servers = v.Data((DataFamily)f), Required = b.TrainingServers[t - 1], Cost = b.TrainingCosts[t - 1], Duration = b.TrainingDurations[t - 1], Progress = v.Training?.Family == (DataFamily)f && v.Training.Tier == t ? v.Training.Progress : 0, Complete = v.Tiers[f] >= t, Running = v.Training?.Family == (DataFamily)f && v.Training.Tier == t, Available = v.Training == null && v.Tiers[f] == t - 1 && v.Data((DataFamily)f) >= b.TrainingServers[t - 1] && v.Compute >= b.TrainingCosts[t - 1] })).ToArray();
            var families = v.EnemySingularityFamilies.ToArray();
            return new AynaScreens
            {
                Phase = phase, A1 = Start(memory, level),
                A2 = new A2PlanningVm { Cycle = v.Cycle, Servers = v.OwnServers, DominanceTarget = (int)Math.Ceiling(AynaWorldData.Regions.Sum(r=>r.Capacity)*b.DominanceShare), Compute = v.Compute, Income = v.OwnServers * b.IncomePerServer + b.CoreIncome, Cost = preview.Where(r => r.Rejection == RejectReason.None).Sum(r => r.Cost), Noise = preview.Where(r => r.Rejection == RejectReason.None).Sum(r => r.Noise), Awareness = v.Awareness, Readability = v.Readability, Regions = v.Regions.Select(r => new RegionVm { Id = r.Id, Capacity = AynaWorldData.Region(r.Id).Capacity, Family = AynaWorldData.Region(r.Id).Family, Own = r.OwnServers, EnemyMin = r.EnemyMin, EnemyMax = r.EnemyMax, Knowledge = r.Knowledge, Core = r.Id == v.CoreRegion }).ToArray(), Queue = queue.Orders.Select(o => o.Copy()).ToArray(), Preview = preview.Select(Receipt).ToArray(), Predictions = v.IncomingForecast.Select(p => p.Copy()).ToArray() },
                A3 = new A3RevealVm { Cycle = v.ReceiptCycle, OwnOrders = v.Receipts.Select(Receipt).ToArray(), EnemySignals = v.Observations.Where(o => o.Cycle == v.ReceiptCycle).ToArray(), Surprise = v.LastSurprise },
                A4 = new A4ConflictVm { Region = pendingRegion, Pending = pendingRegion != null, Resolved = v.Battles.ToArray() },
                A5 = new A5RosterVm { Region = pendingRegion, Slots = v.Tiers[(int)DataFamily.Movement] >= 3 ? b.MovementRosterSize : b.RosterSize, Available = ownArmy?.Units.Select(u => { var item=Unit(u); item.MaxHp=b.UnitHp[(int)u.Kind]; return item; }).ToArray() ?? Array.Empty<UnitVm>(), KnownEnemy = knownEnemy, UnknownEnemy = enemy.Where(a => !a.RosterKnown).Sum(a => a.Count) },
                A6 = board == null ? null : new A6BoardVm { Turn = board.Turn, MaxTurns = board.BalanceSnapshot().BoardTurns, Phase = board.Phase, IntentSide = board.IntentSide, Tiles = board.Tiles.ToArray(), Units = board.Units.Select(Unit).ToArray(), Nodes = board.Nodes.ToArray(), VisibleTraps = board.Traps.ToArray(), VisibleIntents = board.Intents.ToArray(), CoreCells = board.CoreCells.ToArray(), CoreHp = board.CoreHp.ToArray() },
                A7 = lastBattle ?? new A7BattleResultVm(), A8 = new A8ResearchVm { Nodes = research, CanStartSingularity = v.Singularity == null && v.Tiers.Count(t => t >= 3) >= b.SingularityFamilies && v.Compute >= b.SingularityCost, SingularityProgress = v.Singularity?.Progress ?? 0, SingularityStarted = v.Singularity != null, SingularityRequired=b.SingularityDuration, SingularityCost=b.SingularityCost, SingularityFamilies=b.SingularityFamilies, TrainedFamilies=v.Tiers.Count(t=>t>=3) },
                A9 = new A9ArmiesVm { Armies = v.Armies.Select(a => new ArmyVm { Id = a.Id, Region = a.Region, Units = a.Units.Select(u => { var item=Unit(u); item.MaxHp=b.UnitHp[(int)u.Kind]; return item; }).ToArray() }).ToArray(), Production = AynaUnits.All.Select(u => new ProductionVm { Kind = u.Kind, Cost = b.ProductionCost(v.Architecture, u.Kind), Available = v.Tiers[(int)u.Family] >= u.Tier && v.Data(u.Family) > 0 }).ToArray(), RepairCost = b.RepairCost, NewArmyCost = b.NewArmyCost, MarchCost=b.MarchCost, MaxArmies=b.MaxArmies, MaxUnits=b.MaxUnits, CanUseSatellites=v.Tiers[(int)DataFamily.Vision]>=3 },
                A10 = new A10ModelVm { Generation = memory.Generation + 1, Learning = level, TopThree = v.IncomingForecast.OrderByDescending(p => p.Probability).ThenBy(p => p.Kind).ThenBy(p => p.Region, StringComparer.Ordinal).Take(3).Select(p => p.Copy()).ToArray(), Readability = (double[])history.Clone(), OrderCounts = (double[])(learnedOrders ?? memory.OrderCounts).Clone(), Heat = (double[])(heat ?? memory.BoardHeat).Clone() },
                A11 = new A11SingularityVm { Announced = v.EnemySingularityProgress.HasValue, Progress = v.EnemySingularityProgress ?? 0, Required = b.SingularityDuration, Families = families, PossibleTargets = v.Regions.Where(r => r.EnemyMax > 0 && families.Contains(AynaWorldData.Region(r.Id).Family)).Select(r => r.Id).ToArray() },
                A12 = new A12EndVm { Finished = v.Outcome.Finished, ProfileCommitted = committed, Outcome = v.Outcome, Decisions = decisions, Hits = hits, Surprises = surprises, Generation = memory.Generation, Readability = (double[])history.Clone() }
            };
        }
    }
}
