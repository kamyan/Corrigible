using System.Linq;
using Corrigible.Core;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Açık ekonomi/güven değerlerinden bölgenin güncel gelir katkısı. Merkez ödeneği bölgelere dağıtılmaz;
    /// mevcut gelir dökümünü paylaştırmak eski kayıt, kampanya, bütçe ve zaman ölçeğini aynen korur.
    /// </summary>
    public static class RegionFinance
    {
        public readonly struct Snapshot
        {
            public readonly double PerDay, SharePercent;
            public readonly string Summary, Note;

            public Snapshot(double perDay, double sharePercent, string note)
            {
                PerDay = perDay;
                SharePercent = sharePercent;
                Summary = Loc.F("{0:+0.00;-0.00;0.00} fon/gün · bölgesel pay %{1:0.#}", perDay, sharePercent);
                Note = note;
            }
        }

        static double Weight(RegionState r) => r.Left || r.Off > 0 ? 0 : r.Econ * r.Trust / 100;

        public static Snapshot Read(Simulation sim, RegionState region)
        {
            double totalWeight = sim.S.Regions.Sum(Weight);
            double share = totalWeight > 0 ? Weight(region) / totalWeight : 0;
            var income = sim.IncomeBreakdown;
            double contribution = (income.RegionalBase + income.BudgetBonus) * share;
            string note = region.Left ? Loc.T("İttifak dışı: fon katkısı kesildi.")
                : region.Off > 0 ? Loc.T("Çevrimdışı: fon katkısı kesildi.")
                : region.Trust <= 0 ? Loc.T("Yerel güven sıfır: fon katkısı yok.")
                : region.Econ <= 0 ? Loc.T("Ekonomi durmuş: fon katkısı yok.")
                : sim.S.Ending != null ? Loc.T("Son koşulların günlük katkısı · oyun sona erdi")
                : !sim.S.Aware ? Loc.T("Tespit öncesi düşük katkı · merkez ödeneği hariç")
                : region.Q ? Loc.T("Karantinada katkı sürer · merkez ödeneği hariç")
                : Loc.T("Güncel ekonomi ve güven · merkez ödeneği hariç");
            return new Snapshot(contribution, share * 100, note);
        }
    }
}
