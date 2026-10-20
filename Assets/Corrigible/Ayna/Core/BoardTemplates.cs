using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Ayna.Core
{
    public static class BoardTemplates
    {
        // İlk plan, onaylı Güney Asya örneğidir; tuzak konumları şablona gömülü değildir.
        static readonly string[][] layouts = {
            new[] { "MM.#..S.", "M..#.N..", "......#.", ".#N.....", "......#S", "S.#...N.", "..#....G", ".S....GG" },
            new[] { "MM......", "M.S..#..", "..N.....", ".#..N.#.", ".#....#.", ".....N..", "..#..S.G", "......GG" },
            new[] { "MM......", "M..#..S.", ".N.#....", "...N....", "....N...", "....#.S.", ".S..#..G", "......GG" },
            new[] { "MM......", "M...S...", "..N..#..", ".#......", "...N..#.", "..#..N..", "...S...G", "......GG" }
        };
        public static int Count => layouts.Length;
        public static int TransformCell(int cell, int transform)
        {
            int x = cell % 8, y = cell / 8;
            if (transform >= 4) x = 7 - x;
            for (int i = 0; i < transform % 4; i++) { int old = x; x = 7 - y; y = old; }
            return y * 8 + x;
        }
        internal static BoardState Create(BoardSetup setup, AynaBalance b)
        {
            if (setup == null || setup.IntentSide == Side.None || setup.Fighters == null || setup.Servers == null || setup.Servers.Length != 2 || setup.VisionThree?.Length != 2 || setup.HasCore?.Length != 2 || setup.Capacity < 1 || setup.Servers.Any(x => x < 0) || setup.Servers.Sum() > setup.Capacity) throw new ArgumentException("board setup");
            var rng = new Rng(setup.Seed);
            int template = setup.Template < 0 ? (int)(rng.Next() * Count) : setup.Template;
            int transform = setup.Transform < 0 ? (int)(rng.Next() * 8) : setup.Transform;
            if (template < 0 || template >= Count || transform < 0 || transform >= 8 || !Enum.IsDefined(typeof(Side), setup.IntentSide) || setup.FirstStrikeSide != Side.None && setup.FirstStrikeSide != setup.IntentSide.Other()) throw new ArgumentException("board template/side");
            var s = new BoardState { TrackLearning = true, Balance = b.Copy(), Template = template, Transform = transform, IntentSide = setup.IntentSide,
                Phase = setup.FirstStrikeSide == Side.None ? BoardPhase.Intents : BoardPhase.FirstStrike,
                Tiles = new BoardTile[64], VisionThree = (bool[])setup.VisionThree.Clone(), CoreHp = new[] { b.CoreCabinetHp, b.CoreCabinetHp }, CoreCells = new[] { -1, -1 } };
            var spawn = new[] { new List<int>(), new List<int>() };
            for (int cell = 0; cell < 64; cell++)
            {
                char c = layouts[template][cell / 8][cell % 8]; int to = TransformCell(cell, transform);
                s.Tiles[to] = c == '#' ? BoardTile.Rack : c == 'S' ? BoardTile.Civilian : BoardTile.Floor;
                if (c == 'N') s.Nodes.Add(new BoardNode { Cell = to, Owner = Side.None });
                if (c == 'M' || c == 'G') spawn[c == 'M' ? setup.IntentSide.Index() : setup.IntentSide.Other().Index()].Add(to);
            }
            // Dördüncü kadro yuvası için konuş köşesine en yakın boş zemin seçilir.
            for (int side = 0; side < 2; side++)
            {
                var origin = spawn[side][0];
                spawn[side].AddRange(Enumerable.Range(0, 64).Where(c => s.Tiles[c] == BoardTile.Floor && !s.Nodes.Any(n => n.Cell == c) && !spawn.SelectMany(x => x).Contains(c)).OrderBy(c => BoardSimulation.Distance(origin, c)).ThenBy(c => c).Take(1));
                if (setup.Fighters.Count(f => f.Side.Index() == side) > b.MovementRosterSize) throw new ArgumentException("roster size");
                int slot = 0;
                foreach (var f in setup.Fighters.Where(f => f.Side.Index() == side))
                {
                    int cell = f.Cell < 0 ? spawn[side][slot++] : f.Cell;
                    if (f.Id < 1 || cell < 0 || cell >= 64 || s.CoreCells.Contains(cell) || s.Tiles[cell] != BoardTile.Floor || s.Units.Any(u => u.Cell == cell || u.Id == f.Id) || !Enum.IsDefined(typeof(UnitKind), f.Kind) || double.IsNaN(f.Hp) || double.IsInfinity(f.Hp) || f.Hp <= 0 || f.Hp > b.UnitHp[(int)f.Kind]) throw new ArgumentException("fighter");
                    s.Units.Add(new BoardUnit { Id = f.Id, Cell = cell, Hp = f.Hp, Kind = f.Kind, Side = f.Side, ControlledBy = f.Side });
                }
                if (setup.HasCore[side])
                {
                    int core = Enumerable.Range(0, 64).Where(c => s.Tiles[c] == BoardTile.Floor && !s.Units.Any(u => u.Cell == c) && !s.Nodes.Any(n => n.Cell == c) && !s.CoreCells.Contains(c)).OrderBy(c => BoardSimulation.Distance(origin, c)).ThenBy(c => c).First();
                    s.CoreCells[side] = core; s.CoreHp[side] = b.CoreCabinetHp;
                }
            }
            int p = (int)Js.Round(b.BoardNodes * setup.Servers[0] / (double)setup.Capacity), a = (int)Js.Round(b.BoardNodes * setup.Servers[1] / (double)setup.Capacity);
            while (p + a > b.BoardNodes) { if (p > a) p--; else a--; }
            var nodes = s.Nodes.OrderBy(n => BoardSimulation.Distance(n.Cell, spawn[0][0]) - BoardSimulation.Distance(n.Cell, spawn[1][0])).ThenBy(n => n.Cell).ToArray();
            for (int i = 0; i < p; i++) nodes[i].Owner = Side.Player;
            for (int i = 0; i < a; i++) nodes[nodes.Length - 1 - i].Owner = Side.Ayna;
            if (setup.Traps == null || setup.Traps.Count > b.MaxTraps || setup.Traps.Distinct().Count() != setup.Traps.Count) throw new ArgumentException("traps");
            foreach (int cell in setup.Traps)
            {
                if (!LegalTrap(s, cell)) throw new ArgumentException("trap cell");
                s.Traps.Add(new BoardTrap { Cell = cell, Owner = setup.IntentSide });
            }
            s.InitialPresence = new[] { setup.Fighters.Any(f => f.Side == Side.Player), setup.Fighters.Any(f => f.Side == Side.Ayna) }; s.TrapsPlaced = setup.Traps.Count > 0; return s;
        }
        internal static bool LegalTrap(BoardState s, int cell) => cell >= 0 && cell < 64 && s.Tiles[cell] == BoardTile.Floor && !s.Units.Any(u => u.Cell == cell) && !s.Nodes.Any(n => n.Cell == cell) && !s.CoreCells.Contains(cell) && !s.Traps.Any(t => t.Cell == cell);
    }
}
