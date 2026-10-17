using UnityEngine;
namespace Corrigible.Desk.Bridge
{
    public class RaidCommands : MonoBehaviour, IRaidCommands
    {
        [SerializeField] RaidDataSource source;
        public void SelectUnit(char letter)=>source.Select(letter);
        public void ClearSelection()=>source.ClearSelection();
        public void HoverTile(Vector2Int tile)=>source.Hover(tile);
        public void ClearHover()=>source.ClearHover();
        public void MoveSelectedTo(Vector2Int tile)=>source.Move(tile);
        public void ArmAbility()=>source.Arm();
        public void UseAbilityOn(Vector2Int tile)=>source.Use(tile);
        public void Undo()=>source.Undo();public void EndTurn()=>source.EndTurn();
        public void CancelPending()=>source.Cancel();public void SpeedUpMachines(bool on)=>source.Speed(on);
        public void RequestRetreat()=>source.Retreat();public void ToggleLog()=>source.Panel("log");
        public void ToggleRules()=>source.Panel("rules");public void ContinueToReport()=>source.Continue();
    }
}
