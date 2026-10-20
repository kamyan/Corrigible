using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Corrigible.Ayna.Core
{
    [Serializable] public sealed class BoardStep
    {
        public Side Side;
        public int Turn, Cell, TargetKind = -1;
        public BoardAction Action;
        public BoardStep Copy() => (BoardStep)MemberwiseClone();
    }
    public sealed class BoardLearningObservation
    {
        public string Id { get; }
        public int Template { get; }
        public int Cycle { get; }
        public string Region { get; }
        public ReadOnlyCollection<int> Cells { get; }
        public ReadOnlyCollection<UnitKind> Units { get; }
        public ReadOnlyCollection<int> UnitIds { get; }
        public ReadOnlyCollection<double> UnitHp { get; }
        public int FirstTarget { get; }
        internal BoardLearningObservation(string id, BoardState board, Side observer, int cycle = 0, string region = null)
        {
            Id = id; Template = board.Template; Cycle = cycle; Region = region;
            var enemy = observer.Other();
            int Canonical(int cell) { int c = Enumerable.Range(0, 64).First(x => BoardTemplates.TransformCell(x, board.Transform) == cell); return enemy == board.IntentSide ? c : 63 - c; }
            Cells = Array.AsReadOnly(board.Trace.Where(t => t.Side == enemy && t.Turn <= 2 && t.Action == BoardAction.Move).Select(t => Canonical(t.Cell)).Distinct().ToArray());
            UnitHp = Array.AsReadOnly(board.Units.Where(u => u.Side == enemy).OrderBy(u => u.Id).Select(u => Math.Max(0, u.Hp)).ToArray());
            UnitIds = Array.AsReadOnly(board.Units.Where(u => u.Side == enemy).OrderBy(u => u.Id).Select(u => u.Id).ToArray());
            Units = Array.AsReadOnly(board.Units.Where(u => u.Side == enemy).OrderBy(u => u.Id).Select(u => u.Kind).ToArray());
            var first = board.Trace.FirstOrDefault(t => t.Side == enemy && t.TargetKind >= 0);
            FirstTarget = first?.TargetKind ?? -1;
        }
    }
}
