using System;
using UnityEngine;

namespace Corrigible.Desk
{
    public interface IRaidDataSource { RaidViewModel Current { get; } event Action<RaidViewModel> Changed; }
    public interface IRaidPreviewSource { void LoadPreset(int screen); void LoadBoardPreset(string id); }
    public interface IRaidCommands
    {
        void SelectUnit(char letter); void ClearSelection();
        void HoverTile(Vector2Int tile); void ClearHover();
        void MoveSelectedTo(Vector2Int tile); void ArmAbility(); void UseAbilityOn(Vector2Int tile);
        void Undo(); void EndTurn(); void CancelPending(); void SpeedUpMachines(bool on);
        void RequestRetreat(); void ToggleLog(); void ToggleRules(); void ContinueToReport();
    }
    [Serializable] public sealed class RaidTeamModel
    {
        public char Letter; public string Name, Thumb; public int Hp, Max, MoveRange;
        public bool Selected, Move, Ability, Armed, Down, Warn;
        /// <summary>Zırh plakası (ilk hasarı karşılar); yedek uzman (acemi); baskına çıkmadı (sığınakta yaralı).</summary>
        public int Armor; public bool Rookie, Absent;
    }
    [Serializable] public sealed class RaidTargetModel
    { public string Name, Thumb; public int Hp, Max; public bool Armor, Broken; }
    [Serializable] public sealed class RaidCardRow
    { public string Icon, Text, Detail; public bool Good, Used, Armed; }
    [Serializable] public sealed class RaidInspectorModel
    { public string Thumb, Title; public int Hp, Max; public bool Win, IsLootMission; public RaidCardRow[] Rows = Array.Empty<RaidCardRow>(); }
    [Serializable] public sealed class RaidQueueItem { public int Order; public string State; }
    [Serializable] public sealed class RaidLogModel { public string Text,Kind; }
    [Serializable] public sealed class RaidUnusedModel { public char Letter; public string Name; public bool Move,Ability; public int ThreatOrder; }
    [Serializable] public sealed class RaidViewModel
    {
        public bool LastTurn,Finished,Success,EventVisible,IsLootMission;
        public string EventLine,ResultTitle,ResultSubtitle,ResultReason;
        public int Standing,TeamCount;public Vector3 Spot;
        public RaidQueueItem[] Queue=Array.Empty<RaidQueueItem>();
        public RaidLogModel[] Log=Array.Empty<RaidLogModel>();
        public RaidUnusedModel[] Unused=Array.Empty<RaidUnusedModel>();
        public int Turn, Turns, Civilians, CivilianBroken;
        /// <summary>Sivil güven: bu baskında kırılanlar düşülmüş hâli ve tavanı (tavan 0: harekâtsız önizleme, etiket SİVİL kalır).</summary>
        public int Trust, TrustMax;
        public bool Locked, AbilityArmed, CanUndo, EndConfirm, RetreatArmed, FastMachines, Paused, LogUnread;
        public string Phase, OpenPanel, AbilityTip, AbilityLabel, Status;
        public RaidTeamModel[] Team = Array.Empty<RaidTeamModel>();
        public RaidTargetModel[] Targets = Array.Empty<RaidTargetModel>();
        public RaidTeamModel UnitCard; public RaidInspectorModel HoverCard;
        public RaidCardRow[] UnitRows = Array.Empty<RaidCardRow>();
        public string[] Effects = Array.Empty<string>();
        public Vector2Int[] CollisionTiles = Array.Empty<Vector2Int>();
        public Vector2Int[] Moves = Array.Empty<Vector2Int>(), AbilityTargets = Array.Empty<Vector2Int>();
    }
}
