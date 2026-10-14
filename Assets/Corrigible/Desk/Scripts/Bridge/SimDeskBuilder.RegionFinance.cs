using Corrigible.Core;
using Corrigible.Game.UI;

namespace Corrigible.Desk.Bridge
{
    public sealed partial class SimDeskBuilder
    {
        RegionFinanceVM Finance(RegionState r)
        {
            var finance = RegionFinance.Read(sim, r);
            return new RegionFinanceVM
            {
                Title = Loc.T("FON KATKISI"),
                Contribution = finance.Summary,
                Note = finance.Note,
                PerDay = finance.PerDay,
                SharePercent = finance.SharePercent,
            };
        }
    }
}
