using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Corrigible.Ayna.Core
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.Fields)]
    public sealed class BoardSimulation
    {
        BoardState state;
        readonly Stack<BoardState> undo = new Stack<BoardState>();
        internal BoardState State => state;
        internal string StateHash
        {
            get { using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(AynaJson.Write(new { state, undo = undo.ToArray() })))).Replace("-", ""); }
        }
        BoardSimulation(BoardState s) { state = s; }
        public static BoardSimulation Create(BoardSetup setup, AynaBalance balance = null)
        {
            var b = (balance ?? new AynaBalance()).Copy(); b.Validate();
            var sim = new BoardSimulation(BoardTemplates.Create(setup, b)); sim.CheckEnd(); return sim;
        }
        public BoardView View(Side side) { side.Index(); return new BoardView(state, side); }
        public bool Finished => state.Phase == BoardPhase.Finished;
        public BoardResult Result => Finished ? new BoardResult(state) : null;
        public static int Distance(int a, int b) => Math.Abs(a % 8 - b % 8) + Math.Abs(a / 8 - b / 8);
        static bool Area(int a, int b) => Math.Abs(a % 8 - b % 8) <= 1 && Math.Abs(a / 8 - b / 8) <= 1;
        static IEnumerable<int> Adjacent(int cell) => Enumerable.Range(0, 64).Where(c => Distance(cell, c) == 1);
        Side Controller(BoardUnit u) => u.ControlledUntil >= state.Turn ? u.ControlledBy : u.Side;
        bool Ready(BoardUnit u, Side side) => u != null && u.Hp > 0 && Controller(u) == side && u.StunnedUntil < state.Turn;
        bool Occupied(int cell, int ignore = -1) => state.Units.Any(u => u.Hp > 0 && u.Id != ignore && u.Cell == cell) || state.CoreCells.Contains(cell);
        bool Landing(int cell, int id) => cell >= 0 && cell < 64 && state.Tiles[cell] == BoardTile.Floor && !Occupied(cell, id);
        int Movement(BoardUnit u) => state.Balance.UnitMove[(int)u.Kind] + (state.Units.Any(r => r.Hp > 0 && Controller(r) == Controller(u) && r.Kind == UnitKind.Relay && r.Id != u.Id && Distance(r.Cell, u.Cell) <= state.Balance.RelayRange) ? state.Balance.RelayMoveBonus : 0);
        List<int> Path(BoardUnit u, int to)
        {
            if (to == u.Cell || Distance(u.Cell, to) > Movement(u) || !Landing(to, u.Id)) return null;
            var q = new Queue<int>(); var previous = new Dictionary<int, int> { [u.Cell] = -1 }; var distance = new Dictionary<int, int> { [u.Cell] = 0 }; q.Enqueue(u.Cell);
            bool flying = AynaUnits.Get(u.Kind).Class == UnitClass.Air, spider = u.Kind == UnitKind.Spider;
            while (q.Count > 0)
            {
                int cell = q.Dequeue(); if (cell == to) break;
                if (distance[cell] >= Movement(u)) continue;
                foreach (int n in Adjacent(cell))
                {
                    if (previous.ContainsKey(n) || state.CoreCells.Contains(n)) continue;
                    if (!flying && Occupied(n, u.Id) || !flying && !spider && state.Tiles[n] != BoardTile.Floor || spider && state.Tiles[n] == BoardTile.Civilian) continue;
                    previous[n] = cell; distance[n] = distance[cell] + 1; q.Enqueue(n);
                }
            }
            if (!previous.ContainsKey(to)) return null;
            var path = new List<int>(); for (int c = to; c != u.Cell; c = previous[c]) path.Add(c); path.Reverse(); return path;
        }
        bool Line(int from, int to, int min, int max)
        {
            int d = Distance(from, to); if (d < min || d > max || from % 8 != to % 8 && from / 8 != to / 8) return false;
            int step = from % 8 == to % 8 ? to > from ? 8 : -8 : to > from ? 1 : -1;
            for (int c = from + step; c != to; c += step) if (state.Tiles[c] != BoardTile.Floor || Occupied(c)) return false;
            return true;
        }
        bool Target(BoardUnit u, int cell)
        {
            if (cell < 0 || cell >= 64 || state.Tiles[cell] == BoardTile.Rack) return false;
            if (state.Units.Any(t => t.Hp > 0 && t.Cell == cell && Controller(t) == Controller(u))) return false;
            return !Enumerable.Range(0, 2).Any(i => state.CoreCells[i] == cell && i == Controller(u).Index());
        }
        bool Valid(BoardUnit u, BoardCommand c, Side side)
        {
            if (c == null || (uint)c.Action > (uint)BoardAction.Wait || !Ready(u, side)) return false;
            if (c.Action == BoardAction.Wait) return true;
            if (c.Action == BoardAction.Move) return !u.Moved && Path(u, c.To) != null;
            bool special = c.Action == BoardAction.Ability && (u.Kind == UnitKind.Overload || u.Kind == UnitKind.Emp || u.Kind == UnitKind.PhantomOrder);
            if (u.Acted || c.To < 0 || c.To >= 64 || !special && !Target(u, c.To)) return false;
            int d = Distance(u.Cell, c.To); var b = state.Balance;
            if (c.Action == BoardAction.Attack) return d == 1;
            switch (u.Kind)
            {
                case UnitKind.Sniper: return Line(u.Cell, c.To, b.SniperMinRange, b.SniperMaxRange);
                case UnitKind.Emp: return d >= b.EmpMinRange && d <= b.EmpMaxRange;
                case UnitKind.Overload: return c.To == u.Cell;
                case UnitKind.Intrusion: return state.Units.Any(t => t.Hp > 0 && t.Cell == c.To && Controller(t) != side && AynaUnits.Get(t.Kind).Class == UnitClass.Ground) && d == 1;
                case UnitKind.Spider: return d == 1;
                case UnitKind.PhantomOrder:
                    var enemy = state.Units.FirstOrDefault(t => t.Hp > 0 && t.Id == c.TargetUnitId && Controller(t) != side);
                    if (enemy == null || Distance(u.Cell, enemy.Cell) > b.PhantomRange) return false;
                    var intent = state.Intents.LastOrDefault(x => x.UnitId == enemy.Id && x.Action != BoardAction.Wait);
                    if (intent == null || intent.Action == BoardAction.Ability && enemy.Kind == UnitKind.PhantomOrder) return false;
                    var projected = enemy.Copy(); var move = state.Intents.FirstOrDefault(x => x.UnitId == enemy.Id && x.Action == BoardAction.Move);
                    if (move != null && intent.Action != BoardAction.Move) projected.Cell = move.To;
                    var redirect = intent.Copy(); redirect.To = c.To;
                    return Valid(projected, redirect, Controller(enemy));
                default: return false;
            }
        }
        // Bilinen tahta geometrisiyle adaylar; görünmeyen tuzaklar geçerliliği ele vermez.
        public IReadOnlyList<int> MovementPath(Side side, int unitId, int to)
        {
            var unit = state.Units.FirstOrDefault(u => u.Id == unitId);
            if (!Ready(unit, side)) return Array.AsReadOnly(Array.Empty<int>());
            return (Path(unit, to) ?? new List<int>()).AsReadOnly();
        }
        public IReadOnlyList<BoardCommand> LegalCommands(Side side, int unitId = 0, bool? movement = null)
        {
            var result = new List<BoardCommand>();
            if (Finished) return result.AsReadOnly();
            foreach (var u in state.Units.Where(u => Ready(u, side) && (unitId == 0 || u.Id == unitId)).OrderBy(u => u.Id))
            {
                foreach (var action in new[] { BoardAction.Move, BoardAction.Attack, BoardAction.Ability }.Where(a => !movement.HasValue || (a == BoardAction.Move) == movement.Value))
                {
                    var targets = u.Kind == UnitKind.PhantomOrder && action == BoardAction.Ability ? state.Units.Where(t => t.Hp > 0 && Controller(t) != side).Select(t => t.Id).ToArray() : new[] { 0 };
                    foreach (int target in targets) for (int to = 0; to < 64; to++)
                    {
                        var c = new BoardCommand { UnitId = u.Id, Action = action, To = to, TargetUnitId = target };
                        if (Valid(u, c, side)) result.Add(c);
                    }
                }
            }
            return result.AsReadOnly();
        }
        void Hit(int cell, double raw, BoardUnit attacker, Side source)
        {
            var b = state.Balance;
            var target = state.Units.FirstOrDefault(u => u.Hp > 0 && u.Cell == cell);
            if (target != null)
            {
                double damage = raw;
                if (attacker != null && AynaUnits.Beats(AynaUnits.Get(attacker.Kind).Class, AynaUnits.Get(target.Kind).Class)) damage += b.ClassBonus;
                if (target.Kind == UnitKind.Guardian) damage = Math.Max(b.MinDamage, damage - b.GuardianArmor);
                target.Hp -= damage;
            }
            for (int i = 0; i < 2; i++) if (state.CoreCells[i] == cell && state.CoreHp[i] > 0) state.CoreHp[i] -= Math.Max(b.MinDamage, raw - b.CoreCabinetArmor);
            if (state.Tiles[cell] == BoardTile.Civilian)
            { state.Tiles[cell] = BoardTile.Floor; state.Noise[source.Index()] += b.CivilianNoise; }
        }
        void CaptureRelay(BoardUnit u)
        {
            if (u.Hp <= 0 || u.Kind != UnitKind.Relay) return;
            var node = state.Nodes.FirstOrDefault(n => n.Cell == u.Cell); if (node != null) node.Owner = Controller(u);
        }
        void Execute(BoardUnit u, BoardCommand c, bool resolving)
        {
            var b = state.Balance; Side source = Controller(u);
            if (c.Action == BoardAction.Move)
            {
                var path = Path(u, c.To); u.Moved = true;
                foreach (int cell in path)
                {
                    // Uçan/duvar aşan birim ara engelde duramaz; tuzak yalnız zemin geçişinde tetiklenir.
                    if (state.Tiles[cell] != BoardTile.Floor || Occupied(cell, u.Id)) continue;
                    u.Cell = cell; CaptureRelay(u);
                    if (state.TrackLearning) state.Trace.Add(new BoardStep { Side = source, Turn = state.Turn, Cell = cell, Action = c.Action, TargetKind = cell == c.To && state.Nodes.Any(n => n.Cell == cell) ? 0 : -1 });
                    var trap = state.Traps.FirstOrDefault(t => t.Cell == cell && t.Owner != source);
                    if (trap != null) { u.Hp -= b.TrapDamage; state.Traps.Remove(trap); u.Acted = true; break; }
                }
                return;
            }
            if (c.Action == BoardAction.Wait) { u.Moved = u.Acted = true; return; }
            u.Acted = true;
            if (state.TrackLearning)
            {
                int center = c.Action == BoardAction.Ability && u.Kind == UnitKind.Overload ? u.Cell : c.To;
                bool area = c.Action == BoardAction.Ability && (u.Kind == UnitKind.Emp || u.Kind == UnitKind.Overload);
                bool At(int cell) => cell == center || area && Area(center, cell);
                int target = c.TargetUnitId != 0 || state.Units.Any(t => t.Id != u.Id && t.Hp > 0 && At(t.Cell)) || state.CoreCells.Any(cell => cell >= 0 && At(cell)) ? 1 :
                    Enumerable.Range(0, 64).Any(cell => At(cell) && state.Tiles[cell] == BoardTile.Civilian) ? 2 : state.Nodes.Any(n => At(n.Cell)) ? 0 : -1;
                state.Trace.Add(new BoardStep { Side = source, Turn = state.Turn, Cell = center, Action = c.Action, TargetKind = target });
            }
            if (c.Action == BoardAction.Attack) { Hit(c.To, b.BasicDamage, u, source); return; }
            switch (u.Kind)
            {
                case UnitKind.Sniper: Hit(c.To, b.SniperDamage, u, source); break;
                case UnitKind.Spider:
                    Hit(c.To, b.BasicDamage, u, source);
                    var victim = state.Units.FirstOrDefault(t => t.Hp > 0 && t.Cell == c.To);
                    int dx = c.To % 8 - u.Cell % 8, dy = c.To / 8 - u.Cell / 8;
                    int x = c.To % 8 + dx, y = c.To / 8 + dy;
                    if (victim != null && x >= 0 && x < 8 && y >= 0 && y < 8 && Landing(y * 8 + x, victim.Id))
                    {
                        victim.Cell = y * 8 + x;
                        var trap = state.Traps.FirstOrDefault(t => t.Cell == victim.Cell && t.Owner != Controller(victim));
                        if (trap != null) { victim.Hp -= b.TrapDamage; victim.Moved = victim.Acted = true; state.Traps.Remove(trap); }
                    }
                    break;
                case UnitKind.Emp:
                    foreach (var t in state.Units.Where(t => t.Hp > 0 && Area(c.To, t.Cell)))
                    {
                        var type = AynaUnits.Get(t.Kind).Class; if (type == UnitClass.Cyber) continue;
                        int duration = type == UnitClass.Air ? b.EmpAirTurns : b.EmpGroundTurns;
                        int offset = resolving && Controller(t) == state.IntentSide.Other() ? 1 : 0;
                        t.StunnedUntil = Math.Max(t.StunnedUntil, state.Turn + duration - 1 + offset);
                    }
                    break;
                case UnitKind.Overload:
                    var cells = Enumerable.Range(0, 64).Where(cell => Area(u.Cell, cell)).ToArray();
                    foreach (int cell in cells) Hit(cell, b.OverloadDamage, u, source);
                    u.Hp = 0; state.Noise[source.Index()] += b.OverloadNoise; break;
                case UnitKind.Intrusion:
                    var controlled = state.Units.First(t => t.Hp > 0 && t.Cell == c.To); controlled.ControlledBy = source;
                    controlled.ControlledUntil = state.Turn + (resolving ? 1 : 0); controlled.Moved = controlled.Acted = false; break;
                case UnitKind.PhantomOrder:
                    var intent = state.Intents.Last(x => x.UnitId == c.TargetUnitId && x.Action != BoardAction.Wait); intent.To = c.To; break;
            }
        }
        public bool Apply(Side side, BoardCommand c)
        {
            if (state.Phase != BoardPhase.Response && state.Phase != BoardPhase.FirstStrike || side != state.IntentSide.Other()) return false;
            var u = c == null ? null : state.Units.FirstOrDefault(x => x.Id == c.UnitId);
            if (!Valid(u, c, side)) return false;
            undo.Push(state.Copy()); Execute(u, c, false); CheckEnd(); return true;
        }
        public bool Undo(Side side)
        {
            if (side != state.IntentSide.Other() || undo.Count == 0) return false;
            state = undo.Pop(); return true;
        }
        public BoardView Preview(Side side, BoardCommand c)
        {
            if (side != state.IntentSide.Other()) return null;
            var copy = CreateBelief(View(side), false); return copy.Apply(side, c) ? copy.View(side) : null;
        }
        internal BoardSimulation Copy()
        {
            var copy = new BoardSimulation(state.Copy()); foreach (var s in undo.Reverse()) copy.undo.Push(s.Copy()); return copy;
        }
        public bool PlaceTraps(IEnumerable<int> cells)
        {
            if (state.Turn != 1 || state.Phase != BoardPhase.Intents && state.Phase != BoardPhase.FirstStrike || state.TrapsPlaced || undo.Count != 0) return false;
            var list = cells?.ToArray(); if (list == null || list.Length > state.Balance.MaxTraps || list.Distinct().Count() != list.Length || list.Any(c => !BoardTemplates.LegalTrap(state, c))) return false;
            state.Traps = list.Select(c => new BoardTrap { Cell = c, Owner = state.IntentSide }).ToList(); state.TrapsPlaced = true; return true;
        }
        public bool SetIntents(BoardPlan plan)
        {
            if (state.Phase != BoardPhase.Intents || plan?.Commands == null || plan.Commands.Any(c => c == null)) return false;
            var trial = new BoardSimulation(state.Copy()); trial.state.Traps.Clear();
            var accepted = new List<BoardCommand>();
            foreach (var c in plan.Commands)
            {
                var command = c.Copy(); var u = trial.state.Units.FirstOrDefault(x => x.Id == c.UnitId);
                if (c.Action == BoardAction.Attack && u != null)
                {
                    var decoy = trial.state.Units.Where(t => t.Hp > 0 && t.Kind == UnitKind.Decoy && trial.Controller(t) != state.IntentSide && Distance(u.Cell, t.Cell) == 1).OrderBy(t => t.Id).FirstOrDefault();
                    if (decoy != null) command.To = decoy.Cell;
                }
                if (!trial.Valid(u, command, state.IntentSide)) return false;
                trial.Execute(u, command, true); accepted.Add(command);
            }
            state.Intents = accepted; state.Phase = BoardPhase.Response; undo.Clear(); return true;
        }
        public bool EndResponse(Side side)
        {
            if (side != state.IntentSide.Other() || Finished || state.Phase == BoardPhase.Intents) return false;
            undo.Clear();
            if (state.Phase == BoardPhase.FirstStrike)
            { ResetActions(); state.Phase = BoardPhase.Intents; return true; }
            foreach (var c in state.Intents.ToArray())
            {
                var u = state.Units.FirstOrDefault(x => x.Id == c.UnitId);
                if (Valid(u, c, state.IntentSide)) Execute(u, c, true);
                CheckEnd(); if (Finished) return true;
            }
            foreach (var n in state.Nodes)
            {
                var unit = state.Units.FirstOrDefault(u => u.Hp > 0 && u.Cell == n.Cell); if (unit == null) continue;
                bool p = state.Units.Any(u => u.Hp > 0 && Controller(u) == Side.Player && Distance(u.Cell, n.Cell) == 1);
                bool a = state.Units.Any(u => u.Hp > 0 && Controller(u) == Side.Ayna && Distance(u.Cell, n.Cell) == 1);
                if (!(p && a) || unit.Kind == UnitKind.Relay) n.Owner = Controller(unit);
            }
            if (state.Turn >= state.Balance.BoardTurns) { state.Phase = BoardPhase.Finished; return true; }
            state.Turn++; state.Intents.Clear(); ResetActions(); state.Phase = BoardPhase.Intents; return true;
        }
        void ResetActions() { foreach (var u in state.Units) { u.Moved = false; u.Acted = false; } }
        public bool Retreat(Side side)
        {
            side.Index(); if (Finished) return false; state.Retreated = side; state.Phase = BoardPhase.Finished; undo.Clear(); return true;
        }
        void CheckEnd()
        {
            if (Enumerable.Range(0, 2).Any(i => !state.Units.Any(u => u.Hp > 0 && u.Side == (Side)i) && (state.InitialPresence[i] || state.CoreCells[i] < 0)) || state.CoreHp.Any(h => h <= 0)) state.Phase = BoardPhase.Finished;
        }
        // İnanç kopyası yalnız açık görünümden kurulur; gizli tuzaklar taşınmaz.
        public static BoardSimulation CreateBelief(BoardView v, bool responseProjection = true)
        {
            var s = new BoardState { Balance = v.BalanceSnapshot(), Turn = v.Turn, Template = v.Template, Transform = v.Transform, Phase = v.Phase == BoardPhase.Finished ? BoardPhase.Finished : responseProjection ? BoardPhase.Response : v.Phase,
                IntentSide = responseProjection ? v.Side.Other() : v.IntentSide, Tiles = v.Tiles.ToArray(), CoreHp = v.CoreHp.ToArray(), CoreCells = v.CoreCells.ToArray(), VisionThree = new bool[2], InitialPresence = v.InitialPresence.ToArray() };
            s.Units = v.Units.Select(u => new BoardUnit { Id = u.Id, Side = u.Side, ControlledBy = u.Controller, ControlledUntil = u.ControlledUntil, Kind = u.Kind, Hp = u.Hp, Cell = u.Cell, StunnedUntil = u.StunnedUntil, Moved = u.Moved, Acted = u.Acted }).ToList();
            s.Nodes = v.Nodes.Select(n => new BoardNode { Cell = n.Cell, Owner = n.Owner }).ToList(); s.Traps = v.Traps.Select(t => new BoardTrap { Cell = t.Cell, Owner = t.Owner }).ToList();
            s.Noise = v.Noise.ToArray(); s.VisionThree[v.Side.Index()] = v.FullTrapSight;
            s.Intents = v.Intents.Select(i => i.Command()).ToList(); return new BoardSimulation(s);
        }
    }
}
