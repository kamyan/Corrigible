namespace Corrigible.Desk
{
    public enum SadeEscapeLayer { Confirmation, Tooltip, Legend, LargePanel, RegionCard, Pause }

    /// <summary>Sunumun tek seçim ve panel sırası. Oyun kuralı veya gerçek simülasyon verisi taşımaz.</summary>
    public sealed class SadeInteractionState
    {
        public string SelectedRegion { get; private set; }
        public string Drawer { get; private set; }
        public bool FullDossier { get; private set; }
        public bool Legend { get; set; }
        public bool Tooltip { get; set; }
        public bool Confirmation { get; set; }
        public bool Pause { get; set; }
        public string AssigningTeam { get; set; }

        public void Select(string region)
        {
            SelectedRegion = region;
            FullDossier = false;
            Tooltip = false;
        }

        public void ClearSelection()
        {
            SelectedRegion = null;
            FullDossier = false;
        }

        public void ToggleDrawer(string id)
        {
            Drawer = Drawer == id ? null : id;
            FullDossier = false;
            AssigningTeam = null;
        }

        public void OpenDrawer(string id) { Drawer = id; FullDossier = false; }

        public void OpenDossier()
        {
            if (SelectedRegion == null) return;
            Drawer = null;
            FullDossier = true;
            Tooltip = false;
        }

        public void CloseLargePanel() { Drawer = null; FullDossier = false; AssigningTeam = null; }

        public SadeEscapeLayer Escape()
        {
            if (Confirmation) { Confirmation = false; return SadeEscapeLayer.Confirmation; }
            if (Tooltip) { Tooltip = false; return SadeEscapeLayer.Tooltip; }
            if (Legend) { Legend = false; return SadeEscapeLayer.Legend; }
            if (Drawer != null || FullDossier) { CloseLargePanel(); return SadeEscapeLayer.LargePanel; }
            if (SelectedRegion != null) { ClearSelection(); return SadeEscapeLayer.RegionCard; }
            Pause = !Pause;
            return SadeEscapeLayer.Pause;
        }
    }
}
