using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind;
using Corrigible.Ayna.Presentation;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Ayna.Game
{
    [Serializable] public sealed class AynaGameSave
    {
        public int Version = 1;
        public string Match;
        public AynaPhase Phase;
        public Plan Queue = new Plan();
        public BoardPlan Intents = new BoardPlan();
        public A7BattleResultVm LastBattle;
        public List<double> Readability = new List<double>();
        public int Decisions, Hits, Surprises;
        public bool ProfileCommitted;
    }
    public sealed class AynaGameController : MonoBehaviour
    {
        [SerializeField] AynaBalanceConfig balanceAsset;
        [SerializeField] AynaDebugView debugView;
        LearningMatch match;
        AynaProfileStore profiles;
        AynaSaveStore saves;
        AynaProfileData memory = new AynaProfileData();
        LearningLevel level = LearningLevel.Full;
        AynaBalance runtimeBalance;
        Plan queue = new Plan(); BoardPlan intents = new BoardPlan();
        readonly List<double> history = new List<double>();
        A7BattleResultVm lastBattle;
        int decisions, hits, surprises;
        bool profileCommitted, erasePending;
        public AynaPhase Phase { get; private set; } = AynaPhase.Start;
        public AynaScreens Screens { get; private set; }
        public string Status { get; private set; } = "";
        public LearningMatch Match => match;
        public bool HasSave => saves?.Exists ?? false;
        public bool EraseConfirmationPending => erasePending;
        public event Action Changed;
        void Awake() { Initialize(Application.persistentDataPath); }
        void Start() { if (debugView != null) debugView.Bind(this); Refresh(); }
        public void Initialize(string directory, AynaBalance rules = null)
        {
            if (match != null) throw new InvalidOperationException("active match");
            runtimeBalance = rules?.Copy(); profiles = new AynaProfileStore(directory); saves = new AynaSaveStore(directory);
            memory = profiles.Load(level); Refresh();
        }
        public void SetLearning(LearningLevel value) { if (match != null && Phase != AynaPhase.End) return; erasePending = false; level = value; memory = profiles.Load(level); Refresh(); }
        public void NewGame(Architecture architecture, string region, uint seed)
        {
            erasePending = false; memory = profiles.Load(level);
            match = new LearningMatch(new StartSetup { Seed = seed, Player = new StartChoice { Architecture = architecture, Region = region }, Settings = new AynaSettings { Learning = level } }, Guid.NewGuid().ToString("N"), memory, runtimeBalance ?? (balanceAsset != null ? balanceAsset.Snapshot() : new AynaBalance { TacticalPlanning = true }));
            queue = new Plan(); intents = new BoardPlan(); history.Clear(); decisions = hits = surprises = 0; lastBattle = null; profileCommitted = false;
            Phase = AynaPhase.Planning; Status = ""; match.PrepareCycle(); Refresh();
        }
        public bool Queue(Order order)
        {
            if (Phase != AynaPhase.Planning || order == null) return false;
            var next = queue.Copy(); next.Orders.Add(order.Copy()); var preview = match.Simulation.PreviewPlan(Side.Player, next);
            if (preview.Any(r => r.Rejection != RejectReason.None)) { Status = Loc.T("Bu emir mevcut plana eklenemiyor."); Refresh(); return false; }
            queue = next; Status = ""; Refresh(); return true;
        }
        public void UndoQueue() { if (Phase == AynaPhase.Planning && queue.Orders.Count > 0) queue.Orders.RemoveAt(queue.Orders.Count - 1); Refresh(); }
        public void ClearQueue() { if (Phase == AynaPhase.Planning) queue = new Plan(); Refresh(); }
        public bool LockPlan()
        {
            if (Phase != AynaPhase.Planning) return false;
            var sim = match.Simulation; match.PrepareCycle(); var before = sim.View(Side.Player); var forecast = before.IncomingForecast;
            var plan = queue.Copy(); if (plan.Orders.Count == 0) plan.Orders.Add(new Order { Kind = OrderKind.Wait });
            var top = forecast.OrderByDescending(p => p.Probability).ThenBy(p => p.Kind).ThenBy(p => p.Region, StringComparer.Ordinal).FirstOrDefault();
            sim.BeginCycle(plan, match.MakeAynaPlan());
            var actual = sim.View(Side.Player).Receipts.Where(r => r.Kind != OrderKind.Wait && (r.Rejection == RejectReason.None || r.Rejection == RejectReason.LostCondition)).ToArray();
            if (actual.Length > 0) { decisions++; if (top != null && actual.Any(r => r.Kind == top.Kind && (r.Region ?? before.Armies.FirstOrDefault(a => a.Id == plan.Orders[r.Index].ArmyId)?.Region) == top.Region)) hits++; }
            if (sim.View(Side.Player).LastSurprise) surprises++;
            queue = new Plan(); Phase = AynaPhase.Reveal; Refresh(); return true;
        }
        public void Continue()
        {
            if (match == null) return;
            if (Phase == AynaPhase.End) { Home(); return; }
            if (Phase == AynaPhase.Reveal) Phase = match.Simulation.IsResolving ? AynaPhase.Conflict : AynaPhase.Results;
            else if (Phase == AynaPhase.BoardResult)
            { if (!match.Simulation.CompleteBoard()) return; match.AynaMind.Observe(match.Simulation.View(Side.Ayna)); Phase = match.Simulation.IsResolving ? AynaPhase.Conflict : AynaPhase.Results; }
            else if (Phase == AynaPhase.Results)
            {
                var v = match.Simulation.View(Side.Player); history.Add(v.Readability); while (history.Count > 12) history.RemoveAt(0);
                if (v.Outcome.Finished) { Phase = AynaPhase.End; CommitProfile(); }
                else { Phase = AynaPhase.Planning; match.PrepareCycle(); }
            }
            Refresh();
        }
        public bool AutoBattle()
        {
            if (Phase != AynaPhase.Conflict && Phase != AynaPhase.Roster) return false;
            if (!match.Simulation.ResolvePendingAuto()) return false;
            match.AynaMind.Observe(match.Simulation.View(Side.Ayna)); Phase = match.Simulation.IsResolving ? AynaPhase.Conflict : AynaPhase.Results; Refresh(); return true;
        }
        public void ChooseRoster() { if (Phase == AynaPhase.Conflict) Phase = AynaPhase.Roster; Refresh(); }
        public void CancelRoster() { if (Phase == AynaPhase.Roster) Phase = AynaPhase.Conflict; Refresh(); }
        public bool CommandBattle(int[] roster = null)
        {
            if (Phase != AynaPhase.Roster && Phase != AynaPhase.Conflict) return false;
            var sim = match.Simulation;
            if (!sim.StartBoard(roster)) { Status = Loc.T("Kadro seçimi geçersiz."); Refresh(); return false; }
            sim.PlaceBoardTraps(TrapPlanner.Choose(sim.Board(Side.Ayna), match.AynaMind.Rng, match.AynaMind.Heat(sim.Board(Side.Ayna))));
            intents = new BoardPlan(); Phase = AynaPhase.Board; PumpAyna(); Refresh(); return true;
        }
        void PumpAyna()
        {
            var sim = match.Simulation;
            for (int guard = 0; guard < 4; guard++)
            {
                var b = sim.Board(Side.Player); if (b == null) return;
                if (b.Phase == BoardPhase.Finished)
                {
                    lastBattle = new A7BattleResultVm { Available = true, Winner = b.Result.Winner, Nodes = b.Result.Nodes.ToArray(), Noise = b.Result.Noise.ToArray(), Survivors = b.Result.Survivors.Select(AynaViewModels.Unit).ToArray() }; Phase = AynaPhase.BoardResult; return;
                }
                if (b.Phase == BoardPhase.Intents && b.IntentSide == Side.Ayna)
                { if (!sim.SetBoardIntents(BoardPlanner.Choose(sim.Board(Side.Ayna), responseHeat: match.AynaMind.Heat(sim.Board(Side.Ayna))))) throw new InvalidOperationException("ayna intents"); continue; }
                if ((b.Phase == BoardPhase.Response || b.Phase == BoardPhase.FirstStrike) && b.IntentSide == Side.Player)
                {
                    foreach (var c in BoardPlanner.Choose(sim.Board(Side.Ayna)).Commands)
                    { if (sim.Board(Side.Player).Phase == BoardPhase.Finished) break; sim.ApplyBoardCommand(Side.Ayna, c); }
                    if (sim.Board(Side.Player).Phase != BoardPhase.Finished) sim.EndBoardResponse(Side.Ayna);
                    continue;
                }
                return;
            }
            throw new InvalidOperationException("board phase loop");
        }
        public bool BoardCommand(BoardCommand command)
        {
            if (Phase != AynaPhase.Board) return false; var sim = match.Simulation; var b = sim.Board(Side.Player);
            if (b.Phase == BoardPhase.Intents && b.IntentSide == Side.Player)
            {
                var next = intents.Copy(); next.Commands.Add(command.Copy());
                if (!BoardSimulation.CreateBelief(b, false).SetIntents(next)) return false;
                intents = next; Refresh(); return true;
            }
            if (!sim.ApplyBoardCommand(Side.Player, command)) return false; PumpAyna(); Refresh(); return true;
        }
        public BoardView Preview(BoardCommand command)
        {
            if (Phase != AynaPhase.Board) return null; var b = match.Simulation.Board(Side.Player);
            if (b.Phase == BoardPhase.Intents && b.IntentSide == Side.Player)
            { var copy = BoardSimulation.CreateBelief(b, false); var next = intents.Copy(); next.Commands.Add(command.Copy()); return copy.SetIntents(next) ? copy.View(Side.Player) : null; }
            return match.Simulation.PreviewBoardCommand(Side.Player, command);
        }
        public void UndoBoard()
        {
            if (Phase != AynaPhase.Board) return; var b = match.Simulation.Board(Side.Player);
            if (b.Phase == BoardPhase.Intents && b.IntentSide == Side.Player) { if (intents.Commands.Count > 0) intents.Commands.RemoveAt(intents.Commands.Count - 1); }
            else match.Simulation.UndoBoard(Side.Player);
            Refresh();
        }
        public void EndBoardTurn()
        {
            if (Phase != AynaPhase.Board) return; var sim = match.Simulation; var b = sim.Board(Side.Player);
            bool ok = b.Phase == BoardPhase.Intents && b.IntentSide == Side.Player ? sim.SetBoardIntents(intents) : sim.EndBoardResponse(Side.Player);
            if (!ok) return; intents = new BoardPlan(); PumpAyna(); Refresh();
        }
        public void Retreat() { if (Phase == AynaPhase.Board && match.Simulation.RetreatBoard(Side.Player)) { PumpAyna(); Refresh(); } }
        void CommitProfile()
        {
            if (profileCommitted || level == LearningLevel.Off) return;
            var old = match.AynaMind.ProfileAtStart; var current = profiles.Load(level);
            if (current.CompletedGames.Contains(match.MatchId)) { memory = current; profileCommitted = true; return; }
            var updated = match.CompleteProfile();
            profileCommitted = profiles.Commit(updated, old.Generation, old.MemoryId, level);
            if (profileCommitted) memory = updated; else Status = Loc.T("Hafıza değişti; eski oturum yeni profili ezmedi.");
        }
        public string ExportSave() => AynaJson.Write(new AynaGameSave { Match = match?.Save(), Phase = Phase, Queue = queue.Copy(), Intents = intents.Copy(), LastBattle = lastBattle, Readability = history.ToList(), Decisions = decisions, Hits = hits, Surprises = surprises, ProfileCommitted = profileCommitted });
        public void SaveGame() { if (match == null) return; saves.Save(ExportSave()); Status = Loc.T("AYNA oyunu kaydedildi."); Refresh(); }
        public void LoadGame() { RestoreSave(saves.Load()); Status = Loc.T("AYNA oyununa devam ediliyor."); Refresh(); }
        public void RestoreSave(string json)
        {
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<AynaGameSave>(json, new Newtonsoft.Json.JsonSerializerSettings { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None, MaxDepth = 64 });
            if (data == null || data.Version != 1 || data.Match == null || !Enum.IsDefined(typeof(AynaPhase), data.Phase) || data.Phase == AynaPhase.Start || data.Queue == null || data.Intents == null || data.Readability == null || data.Readability.Count > 12 || data.Readability.Any(x => double.IsNaN(x) || double.IsInfinity(x)) || data.Decisions < 0 || data.Hits < 0 || data.Hits > data.Decisions || data.Surprises < 0) throw new ArgumentException("game save");
            var restored = LearningMatch.Restore(data.Match); var sim = restored.Simulation; var board = sim.Board(Side.Player);
            bool valid = data.Phase == AynaPhase.Board ? board != null && board.Phase != BoardPhase.Finished : data.Phase == AynaPhase.BoardResult ? board?.Phase == BoardPhase.Finished : data.Phase == AynaPhase.Conflict || data.Phase == AynaPhase.Roster ? sim.IsResolving && board == null : data.Phase == AynaPhase.Planning ? !sim.IsResolving && !sim.Outcome.Finished : data.Phase == AynaPhase.End ? sim.Outcome.Finished : true;
            if (data.Phase == AynaPhase.BoardResult && (data.LastBattle == null || data.LastBattle.Nodes?.Length != 2 || data.LastBattle.Noise?.Length != 2)) throw new ArgumentException("battle result save");
            if (data.Phase == AynaPhase.Board && board.Phase == BoardPhase.Intents && board.IntentSide == Side.Player && !BoardSimulation.CreateBelief(board, false).SetIntents(data.Intents)) throw new ArgumentException("intent save");
            if (!valid || data.Phase == AynaPhase.Planning && sim.PreviewPlan(Side.Player, data.Queue).Any(r => r.Rejection != RejectReason.None)) throw new ArgumentException("save phase");
            erasePending = false; match = restored; level = sim.Record.Setup.Settings.Learning; memory = profiles.Load(level); Phase = data.Phase; queue = data.Queue.Copy(); intents = data.Intents.Copy(); lastBattle = data.LastBattle; history.Clear(); history.AddRange(data.Readability); decisions = data.Decisions; hits = data.Hits; surprises = data.Surprises; profileCommitted = data.ProfileCommitted; Refresh();
        }
        public void Home() { erasePending = false; match = null; Phase = AynaPhase.Start; memory = profiles.Load(level); Status = ""; Refresh(); }
        public void RequestEraseMemory() { if (Phase != AynaPhase.Start && Phase != AynaPhase.End) return; erasePending = true; Status = Loc.T("Hafızayı silmek için tekrar onayla."); Refresh(); }
        public void CancelEraseMemory() { erasePending = false; Refresh(); }
        public bool EraseMemory()
        { if (!erasePending || Phase != AynaPhase.Start && Phase != AynaPhase.End) return false; erasePending = false; memory = profiles.EraseMemory(); Status = Loc.T("AYNA hafızası silindi."); Refresh(); return true; }
        public void Refresh()
        {
            if (match == null) Screens = new AynaScreens { Phase = Phase, A1 = AynaViewModels.Start(memory, level) };
            else
            {
                var sim = match.Simulation; var stats = match.AynaMind.Checkpoint().Game;
                Screens = AynaViewModels.Build(sim.View(Side.Player), sim.Board(Side.Player), memory, Phase, level, queue, sim.PreviewPlan(Side.Player, queue).ToArray(), sim.PendingBattle(Side.Player)?.Region, lastBattle, history.ToArray(), decisions, hits, surprises, profileCommitted, stats.OrderCounts, stats.BoardHeat);
                if (Screens.A6 != null) Screens.A6.DraftIntents = intents.Commands.Select(c => c.Copy()).ToArray();
            }
            Changed?.Invoke();
        }
    }
}
