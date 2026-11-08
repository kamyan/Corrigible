using Corrigible.Core;

namespace Corrigible.Desk.Tests
{
    /// <summary>Eski oda/kapı kurallarının regresyon testlerini açıkça eski simülasyon kipinde tutar.</summary>
    internal static class LegacySimulation
    {
        internal static Simulation NewGame(uint? seed = null, Balance balance = null)
        {
            var legacyBalance = balance ?? new Balance();
            legacyBalance.planningBoardEnabled = false;
            return Simulation.NewGame(seed, legacyBalance);
        }
    }
}
