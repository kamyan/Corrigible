namespace Corrigible.Desk
{
    /// <summary>Alternatif masa sunumu. Oyun köprüsü görünüm modelini ve ortak komut sınırını sağlar.</summary>
    public interface IDeskSurface
    {
        bool InputBlocked { get; }
        void BindGame(DeskViewModel model, IDeskCommands commands);
        void OpenWindow(WindowId window);
        void ToggleWindow(WindowId window);
        void SelectRegion(string region);
        void RegionAction(int index);
        void FieldOperation();
        void Escape();
        void StepSpeed(int direction);
    }
}
