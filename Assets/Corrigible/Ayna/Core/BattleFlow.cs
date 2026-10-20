using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace Corrigible.Ayna.Core
{
    public sealed class BattleRequest
    {
        public string Id { get; }
        public string Region { get; }
        public ReadOnlyCollection<UnitView> PlayerRoster { get; }
        public ReadOnlyCollection<UnitView> AynaRoster { get; }
        internal BattleRequest(string region, int cycle, ArmyState p, ArmyState a)
        { Region = region; Id = cycle + ":" + region; PlayerRoster = Array.AsReadOnly((p?.Units ?? new List<UnitState>()).Select(u => new UnitView(u.Id, u.Kind, u.Hp)).ToArray()); AynaRoster = Array.AsReadOnly((a?.Units ?? new List<UnitState>()).Select(u => new UnitView(u.Id, u.Kind, u.Hp)).ToArray()); }
    }
    [Serializable] public sealed class AynaInput
    {
        public string Kind;
        public Plan Player, Ayna;
        public Side Side = Side.None, IntentSide = Side.Ayna, FirstStrikeSide = Side.None;
        public int[] PlayerRoster, AynaRoster, Cells;
        public BoardCommand Command;
        public BoardPlan Intents;
        public List<Prediction> Predictions;
    }
    public sealed partial class AynaSimulation
    {
        [JsonObject(MemberSerialization.Fields)] internal sealed class ResolutionState
        {
            internal List<ValidatedOrder>[] Orders;
            internal HashSet<string>[] Hidden;
            internal Plan Player, Ayna;
            internal int Cursor;
            internal string Region;
            internal BoardSimulation Board;
            internal int[] PlayerRoster, AynaRoster;
        }
        public bool IsResolving => state.Pending != null;
        public BattleRequest PendingBattle(Side side)
        {
            side.Index(); var pending = state.Pending;
            if (pending?.Region == null) return null;
            var armies = BattleArmies(pending.Region);
            // Komuta savaşı karşılaşma veya çekirdek baskınıdır; savunma kadrosu boş olabilir.
            return new BattleRequest(pending.Region, state.Cycle, Roster(armies[0], 0), Roster(armies[1], 1));
        }
        ArmyState[] BattleArmies(string region) => state.Sides.Select(s => s.Armies.Where(a => a.Region == region && a.Units.Count > 0).OrderBy(a => a.Id).FirstOrDefault()).ToArray();
        void RecordInput(AynaInput input)
        {
            if (record != null) record.Inputs.Add(JsonConvert.DeserializeObject<AynaInput>(AynaJson.Write(input)));
        }
        void AdvanceBattles()
        {
            var pending = state.Pending;
            while (pending.Cursor < state.Regions.Count)
            {
                var r = state.Regions[pending.Cursor++]; var armies = BattleArmies(r.Id);
                bool encounter = armies.All(a => a != null);
                bool attack = pending.Orders.Any(list => list.Any(o => o.Valid && o.Order.Kind == OrderKind.Attack && o.Order.Region == r.Id));
                int attacker = pending.Orders[0].Any(o => o.Valid && o.Order.Kind == OrderKind.Attack && o.Order.Region == r.Id) ? 0 : 1;
                bool coreRaid = attack && armies[attacker] != null && !state.Sides[1 - attacker].CoreDestroyed && state.Sides[1 - attacker].CoreRegion == r.Id;
                if (encounter || coreRaid) { pending.Region = r.Id; return; }
                if (attack) ResolveBattle(r, pending.Orders);
            }
            state.Pending = null;
            FinishCycle(pending.Orders, pending.Hidden, pending.Player, pending.Ayna);
        }
        public bool ResolvePendingAuto()
        {
            var p = state.Pending; if (p?.Region == null || p.Board != null) return false;
            ConsumeFirstStrike(p.Region);
            RecordInput(new AynaInput { Kind = "auto" }); ResolveBattle(state.Region(p.Region), p.Orders); p.Region = null; AdvanceBattles(); return true;
        }
        public bool StartBoard(int[] playerRoster = null, int[] aynaRoster = null, Side intentSide = Side.Ayna, Side firstStrikeSide = Side.None)
        {
            var p = state.Pending; if (p?.Region == null || p.Board != null || intentSide == Side.None || !Enum.IsDefined(typeof(Side), intentSide)) return false;
            if (firstStrikeSide != Side.None && firstStrikeSide != intentSide.Other()) return false;
            if (state.Forecasts.Any(f => f.Cycle == state.Cycle))
            {
                var eligible = Enumerable.Range(0,2).Select(i => state.FirstStrike[i] && (balance.SurprisePolicy < 3 || state.FirstStrikeRegion[i] == p.Region)).ToArray();
                Side earned = eligible.Count(x => x) == 1 ? eligible[0] ? Side.Player : Side.Ayna : Side.None;
                if (firstStrikeSide != Side.None && firstStrikeSide != earned) return false;
                firstStrikeSide = earned;
            }
            if (firstStrikeSide != Side.None) intentSide = firstStrikeSide.Other();
            var armies = BattleArmies(p.Region); var selected = new List<BoardFighter>(); var chosen = new[] { playerRoster, aynaRoster };
            for (int i = 0; i < 2; i++)
            {
                var ids = chosen[i] ?? Roster(armies[i], i)?.Units.Select(u => u.Id).ToArray() ?? Array.Empty<int>();
                int max = state.Sides[i].Tiers[(int)DataFamily.Movement] >= 3 ? balance.MovementRosterSize : balance.RosterSize;
                if (armies[i] != null && ids.Length < 1 || ids.Length > max || ids.Distinct().Count() != ids.Length || ids.Any(id => armies[i] == null || !armies[i].Units.Any(u => u.Id == id))) return false;
                chosen[i] = (int[])ids.Clone();
                foreach (int id in ids.OrderBy(id => id)) { var u = armies[i].Units.First(x => x.Id == id); selected.Add(new BoardFighter { Id = id, Side = (Side)i, Kind = u.Kind, Hp = u.Hp }); }
            }
            var r = state.Region(p.Region);
            p.Board = BoardSimulation.Create(new BoardSetup { Seed = AynaWorldData.BoardSeed(state.Seed, state.Cycle, r.Id), Fighters = selected, Servers = (int[])r.Owned.Clone(), Capacity = AynaWorldData.Region(r.Id).Capacity,
                IntentSide = intentSide, FirstStrikeSide = firstStrikeSide, VisionThree = state.Sides.Select(s => s.Tiers[(int)DataFamily.Vision] >= 3).ToArray(), HasCore = state.Sides.Select(s => s.CoreRegion == r.Id).ToArray() }, balance);
            ConsumeFirstStrike(p.Region);
            p.PlayerRoster = chosen[0]; p.AynaRoster = chosen[1];
            RecordInput(new AynaInput { Kind = "board", PlayerRoster = chosen[0], AynaRoster = chosen[1], IntentSide = intentSide, FirstStrikeSide = firstStrikeSide }); return true;
        }
        void ConsumeFirstStrike(string region)
        {
            for(int i=0;i<2;i++) if(balance.SurprisePolicy<3 || state.FirstStrikeRegion[i]==region) state.FirstStrike[i]=false;
        }
        public BoardView Board(Side side) => state.Pending?.Board?.View(side);
        public bool PlaceBoardTraps(IEnumerable<int> cells)
        {
            var list = cells?.ToArray(); if (state.Pending?.Board == null || !state.Pending.Board.PlaceTraps(list)) return false;
            RecordInput(new AynaInput { Kind = "traps", Cells = list }); return true;
        }
        public bool SetBoardIntents(BoardPlan plan)
        {
            if (state.Pending?.Board == null || !state.Pending.Board.SetIntents(plan)) return false;
            RecordInput(new AynaInput { Kind = "intents", Intents = plan }); return true;
        }
        public bool ApplyBoardCommand(Side side, BoardCommand command)
        {
            if (state.Pending?.Board == null || !state.Pending.Board.Apply(side, command)) return false;
            RecordInput(new AynaInput { Kind = "command", Side = side, Command = command }); return true;
        }
        public BoardView PreviewBoardCommand(Side side, BoardCommand command) => state.Pending?.Board?.Preview(side, command);
        public bool UndoBoard(Side side)
        {
            if (state.Pending?.Board == null || !state.Pending.Board.Undo(side)) return false;
            RecordInput(new AynaInput { Kind = "undo", Side = side }); return true;
        }
        public bool EndBoardResponse(Side side)
        {
            if (state.Pending?.Board == null || !state.Pending.Board.EndResponse(side)) return false;
            RecordInput(new AynaInput { Kind = "end-response", Side = side }); return true;
        }
        public bool RetreatBoard(Side side)
        {
            if (state.Pending?.Board == null || !state.Pending.Board.Retreat(side)) return false;
            RecordInput(new AynaInput { Kind = "retreat", Side = side }); return true;
        }
        public bool CompleteBoard()
        {
            var p = state.Pending; if (p?.Board == null || !p.Board.Finished) return false;
            RecordInput(new AynaInput { Kind = "complete-board" });
            var result = p.Board.Result; var r = state.Region(p.Region); var armies = BattleArmies(r.Id);
            var original = (int[])r.Owned.Clone(); int pFromA = Math.Min(result.Nodes[0], original[1]), aFromP = Math.Min(result.Nodes[1], original[0]);
            int pFree = Math.Min(r.Free, result.Nodes[0] - pFromA), aFree = Math.Min(r.Free - pFree, result.Nodes[1] - aFromP);
            r.Owned[0] += pFromA - aFromP + pFree; r.Owned[1] += aFromP - pFromA + aFree; r.Free -= pFree + aFree;
            for (int i = 0; i < 2; i++)
            {
                var ids = i == 0 ? p.PlayerRoster : p.AynaRoster;
                if (armies[i] != null)
                {
                foreach (var unit in armies[i].Units.Where(u => ids.Contains(u.Id))) unit.Hp = result.Survivors.FirstOrDefault(u => u.Id == unit.Id)?.Hp ?? 0;
                armies[i].Units.RemoveAll(u => u.Hp <= 0);
                }
                state.Knowledge[i].Boards.Add(new BoardLearningObservation(state.Cycle + ":" + r.Id, p.Board.State, (Side)i, state.Cycle, r.Id));
                state.Sides[i].CoreDestroyed |= result.CoreBroken[i];
                state.Sides[i].LastNoise += result.Noise[i] * balance.NoiseMultiplier(state.Sides[i].Architecture);
                state.Sides[i].BattleCount++;
                state.Knowledge[i].Battles.Add(new BattleView(r.Id, result.Winner, false, result.Winner == (Side)i ? 1 : result.Winner == Side.None ? .5 : 0, i == 0 ? pFromA + pFree : aFromP + aFree));
                if (result.Winner == (Side)i && p.Orders[i].Any(o => o.Valid && o.Order.Kind == OrderKind.Attack && o.Order.Region == r.Id)) r.Wall[1 - i] = Math.Max(0, r.Wall[1 - i] - 1);
                Observe(i, r.Id, "battle");
            }
            state.Battles++; p.Region = null; p.Board = null; AdvanceBattles(); return true;
        }
    }
}
