using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Masanın görünüm durumu: okunmuş baskın raporları.</summary>
    public sealed class DeskBridgeState
    {
        /// <summary>Okunmuş baskın raporu sayısı; fazlası operasyon raporu olarak açılır (SiginakOperation).</summary>
        public int RaidLogSeen;
        /// <summary>Son rapor bekletiliyor: harekâtı kapıda biten kararın kâğıdı henüz açık.</summary>
        public bool HoldEnding;
    }

    /// <summary>
    /// Simülasyondan masanın tam ekran kâğıdını kurar: harekât bittiyse (ve kapının kâğıdı kapandıysa) son rapor
    /// (<see cref="FinalReportBuilder"/>). Oda, sayaçlar ve operasyon raporu simülasyonu doğrudan okur.
    /// </summary>
    public sealed class SimDeskBuilder
    {
        public DeskViewModel Build(Simulation sim, DeskBridgeState state)
        {
            var vm = new DeskViewModel();
            if (sim.S.Ending != null && !state.HoldEnding) vm.FinalReport = FinalReportBuilder.Build(sim);
            return vm;
        }
    }
}
