using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace Corrigible.Ayna.Core
{
    public enum BoardTile { Floor, Rack, Civilian }
    public enum BoardPhase { Intents, Response, FirstStrike, Finished }
    public enum BoardAction { Move, Attack, Ability, Wait }
    [Serializable] public sealed class BoardCommand
    {
        public int UnitId, To, TargetUnitId;
        public BoardAction Action;
        public BoardCommand Copy() => (BoardCommand)MemberwiseClone();
    }
    [Serializable] public sealed class BoardPlan
    {
        public List<BoardCommand> Commands = new List<BoardCommand>();
        public BoardPlan Copy() => new BoardPlan { Commands = Commands.Select(c => c.Copy()).ToList() };
    }
    [Serializable] public sealed class BoardFighter
    {
        public int Id;
        public Side Side;
        public UnitKind Kind;
        public double Hp;
        public int Cell = -1;
        public BoardFighter Copy() => (BoardFighter)MemberwiseClone();
    }
    [Serializable] public sealed class BoardSetup
    {
        public uint Seed;
        public int Template = -1, Transform = -1;
        public Side IntentSide = Side.Ayna, FirstStrikeSide = Side.None;
        public List<BoardFighter> Fighters = new List<BoardFighter>();
        public int[] Servers = { 3, 3 };
        public int Capacity = 8;
        public bool[] VisionThree = new bool[2], HasCore = new bool[2];
        public List<int> Traps = new List<int>();
    }
    public sealed class BoardUnitView
    {
        public int Id { get; }
        public Side Side { get; }
        public Side Controller { get; }
        public UnitKind Kind { get; }
        public double Hp { get; }
        public int Cell { get; }
        public int StunnedUntil { get; }
        public int ControlledUntil { get; }
        public bool Moved { get; }
        public bool Acted { get; }
        internal BoardUnitView(BoardUnit u, int turn)
        { Id = u.Id; Side = u.Side; Controller = u.ControlledUntil >= turn ? u.ControlledBy : u.Side; Kind = u.Kind; Hp = u.Hp; Cell = u.Cell; StunnedUntil = u.StunnedUntil; ControlledUntil = u.ControlledUntil; Moved = u.Moved; Acted = u.Acted; }
    }
    public sealed class BoardNodeView
    {
        public int Cell { get; }
        public Side Owner { get; }
        internal BoardNodeView(int cell, Side owner) { Cell = cell; Owner = owner; }
    }
    public sealed class BoardTrapView
    {
        public int Cell { get; }
        public Side Owner { get; }
        internal BoardTrapView(int cell, Side owner) { Cell = cell; Owner = owner; }
    }
    public sealed class BoardIntentView
    {
        public int Order { get; }
        public BoardAction Action { get; }
        public int UnitId { get; }
        public int To { get; }
        public int TargetUnitId { get; }
        internal BoardIntentView(BoardCommand c, int order) { Order = order; Action = c.Action; UnitId = c.UnitId; To = c.To; TargetUnitId = c.TargetUnitId; }
        public BoardCommand Command() => new BoardCommand { UnitId = UnitId, Action = Action, To = To, TargetUnitId = TargetUnitId };
    }
    public sealed class BoardResult
    {
        public Side Winner { get; }
        public ReadOnlyCollection<int> Nodes { get; }
        public ReadOnlyCollection<double> Noise { get; }
        public ReadOnlyCollection<bool> CoreBroken { get; }
        public ReadOnlyCollection<BoardUnitView> Survivors { get; }
        public bool Retreated { get; }
        internal BoardResult(BoardState s)
        {
            var count = new[] { s.Nodes.Count(n => n.Owner == Side.Player), s.Nodes.Count(n => n.Owner == Side.Ayna) };
            var lost = Enumerable.Range(0, 2).Select(i => s.CoreHp[i] <= 0 || !s.Units.Any(u => u.Side == (Side)i && u.Hp > 0) && (s.InitialPresence[i] || s.CoreCells[i] < 0)).ToArray();
            if (s.Retreated != Side.None || lost.Any(x => x))
            {
                Side winner = s.Retreated != Side.None ? s.Retreated.Other() : lost[0] == lost[1] ? Side.None : lost[0] ? Side.Ayna : Side.Player;
                count = new[] { winner == Side.Player ? s.Balance.BoardNodes : 0, winner == Side.Ayna ? s.Balance.BoardNodes : 0 };
            }
            Winner = count[0] == count[1] ? Side.None : count[0] > count[1] ? Side.Player : Side.Ayna;
            Nodes = Array.AsReadOnly(count); Noise = Array.AsReadOnly((double[])s.Noise.Clone());
            CoreBroken = Array.AsReadOnly(s.CoreHp.Select(h => h <= 0).ToArray());
            Survivors = Array.AsReadOnly(s.Units.Where(u => u.Hp > 0).OrderBy(u => u.Id).Select(u => new BoardUnitView(u, s.Turn)).ToArray()); Retreated = s.Retreated != Side.None;
        }
    }
    public sealed class BoardView
    {
        public Side Side { get; }
        public Side IntentSide { get; }
        public int Turn { get; }
        public int Template { get; }
        public int Transform { get; }
        public BoardPhase Phase { get; }
        public bool FullTrapSight { get; }
        public ReadOnlyCollection<double> Noise { get; }
        public ReadOnlyCollection<BoardTile> Tiles { get; }
        public ReadOnlyCollection<BoardUnitView> Units { get; }
        public ReadOnlyCollection<BoardNodeView> Nodes { get; }
        public ReadOnlyCollection<BoardTrapView> Traps { get; }
        public ReadOnlyCollection<BoardIntentView> Intents { get; }
        public ReadOnlyCollection<bool> InitialPresence { get; }
        public ReadOnlyCollection<double> CoreHp { get; }
        public ReadOnlyCollection<int> CoreCells { get; }
        public BoardResult Result { get; }
        readonly AynaBalance balance;
        public AynaBalance BalanceSnapshot() => balance.Copy();
        internal BoardView(BoardState s, Side side)
        {
            Side = side; IntentSide = s.IntentSide; Turn = s.Turn; Template = s.Template; Transform = s.Transform; Phase = s.Phase;
            FullTrapSight = s.VisionThree[side.Index()]; Noise = Array.AsReadOnly((double[])s.Noise.Clone());
            Tiles = Array.AsReadOnly((BoardTile[])s.Tiles.Clone());
            Units = Array.AsReadOnly(s.Units.Where(u => u.Hp > 0).OrderBy(u => u.Id).Select(u => new BoardUnitView(u, s.Turn)).ToArray());
            Nodes = Array.AsReadOnly(s.Nodes.Select(n => new BoardNodeView(n.Cell, n.Owner)).ToArray());
            Traps = Array.AsReadOnly(s.Traps.Where(t => t.Owner == side || s.VisionThree[side.Index()] || s.Units.Any(u => u.Hp > 0 && u.Side == side && u.Kind == UnitKind.Scout && BoardSimulation.Distance(u.Cell, t.Cell) <= s.Balance.ScoutSight)).Select(t => new BoardTrapView(t.Cell, t.Owner)).ToArray());
            Intents = Array.AsReadOnly(s.Intents.Select((c, i) => new BoardIntentView(c, i)).ToArray());
            InitialPresence = Array.AsReadOnly((bool[])s.InitialPresence.Clone());
            CoreHp = Array.AsReadOnly((double[])s.CoreHp.Clone()); CoreCells = Array.AsReadOnly((int[])s.CoreCells.Clone());
            balance = s.Balance; Result = s.Phase == BoardPhase.Finished ? new BoardResult(s) : null;
        }
    }
    [JsonObject(MemberSerialization.Fields)] internal sealed class BoardUnit
    {
        internal int Id, Cell, StunnedUntil, ControlledUntil;
        internal Side Side, ControlledBy;
        internal UnitKind Kind;
        internal double Hp;
        internal bool Moved, Acted;
        internal BoardUnit Copy() => (BoardUnit)MemberwiseClone();
    }
    [JsonObject(MemberSerialization.Fields)] internal sealed class BoardNode
    { internal int Cell; internal Side Owner; internal BoardNode Copy() => (BoardNode)MemberwiseClone(); }
    [JsonObject(MemberSerialization.Fields)] internal sealed class BoardTrap
    { internal int Cell; internal Side Owner; internal BoardTrap Copy() => (BoardTrap)MemberwiseClone(); }
    [JsonObject(MemberSerialization.Fields)] internal sealed class BoardState
    {
        internal int Turn = 1, Template, Transform;
        internal BoardPhase Phase;
        internal Side IntentSide, Retreated = Side.None;
        internal BoardTile[] Tiles;
        internal List<BoardUnit> Units = new List<BoardUnit>();
        internal List<BoardNode> Nodes = new List<BoardNode>();
        internal List<BoardTrap> Traps = new List<BoardTrap>();
        internal List<BoardCommand> Intents = new List<BoardCommand>();
        internal double[] CoreHp, Noise = new double[2];
        internal int[] CoreCells;
        internal bool TrapsPlaced;
        internal bool TrackLearning;
        internal List<BoardStep> Trace = new List<BoardStep>();
        internal bool[] InitialPresence = new bool[2];
        internal bool[] VisionThree;
        internal AynaBalance Balance;
        internal BoardState Copy() => new BoardState { Turn = Turn, Template = Template, Transform = Transform, Phase = Phase, IntentSide = IntentSide, Retreated = Retreated, TrapsPlaced = TrapsPlaced,
            TrackLearning = TrackLearning, Trace = Trace.Select(t => t.Copy()).ToList(), Tiles = (BoardTile[])Tiles.Clone(), Units = Units.Select(u => u.Copy()).ToList(), Nodes = Nodes.Select(n => n.Copy()).ToList(), Traps = Traps.Select(t => t.Copy()).ToList(),
            Intents = Intents.Select(c => c.Copy()).ToList(), CoreHp = (double[])CoreHp.Clone(), CoreCells = (int[])CoreCells.Clone(), VisionThree = (bool[])VisionThree.Clone(), InitialPresence = (bool[])InitialPresence.Clone(), Noise = (double[])Noise.Clone(), Balance = Balance };
    }
}
