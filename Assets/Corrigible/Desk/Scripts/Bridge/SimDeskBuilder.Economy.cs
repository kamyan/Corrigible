using System;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    public sealed partial class SimDeskBuilder
    {
        EconomyRecoveryVM Recovery(RegionState r)
        {
            if (!sim.ModernEconomy) return null;
            string recoveryWhy = sim.RecoveryCheck(r.Id), diplomacyWhy = sim.DiplomaticCheck(r.Id);
            return new EconomyRecoveryVM
            {
                Title = Loc.T("TOPARLANMA"),
                RecoveryText = recoveryWhy ?? Loc.F("{0} fon peşin · {1} gün · ekonomi +{2:0.#}", sim.RecoveryCost, sim.B.recoveryDays, sim.RecoveryExpectedGain(r.Id)),
                DiplomacyText = diplomacyWhy ?? Loc.F("{0} fon + {1} SG · güven {2:0} → {3:0}{4}", sim.DiplomacyCost, sim.B.diplomacySg, r.Trust, sim.DiplomaticTrustAfter(r.Id), r.Left ? Loc.T(" · ittifaka döner") : ""),
                Recovery = new CommandVM { Label = Loc.T("Altyapıyı toparla"), Command = recoveryWhy == null ? "Economy:Recover:" + r.Id : null },
                Diplomacy = new CommandVM { Label = Loc.T("Diplomatik görüşme"), Command = diplomacyWhy == null ? "Economy:Diplomacy:" + r.Id : null },
            };
        }

        CommandVM TeamHire()
        {
            if (!sim.TeamsOn) return null;
            double cost = sim.ModernEconomy ? sim.B.teamHireCost : Simulation.TeamHireFon;
            int days = sim.ModernEconomy ? sim.B.teamHireDays : Simulation.TeamHireDays;
            bool can = S.Ending == null && S.Teams.Count < Simulation.TeamMax && S.Fon >= cost;
            return new CommandVM
            {
                Label = S.Teams.Count >= Simulation.TeamMax ? Loc.T("Ekip kapasitesi dolu") : S.Ending != null ? Loc.T("Oyun bitti.") : S.Fon < cost ? Loc.F("Yeni ekip {0} fon ister.", cost) : Loc.F("{0} fon peşin · {1} gün eğitim", cost, days),
                Command = can ? "Team:Hire" : null,
            };
        }

        void EconomyBreakdown(GaugeVM gauge)
        {
            var income = sim.IncomeBreakdown;
            var paid = sim.LastIncome;
            double spent = sim.LastResearchPayment;
            gauge.Delta = Delta((float)(paid.Total - spent), true);
            gauge.Note = Loc.F("Son gün net {0} · sonraki gelir {1}", Num(N(paid.Total - spent)), Num(N(income.Total)));
            var b = gauge.Breakdown;
            b.Items.Clear();
            void Item(string label, double value) => b.Items.Add(new BreakdownItemVM { Label = label, Value = DeskFormat.Signed((float)value, 2), Status = value >= 0 ? "good" : "threat" });
            Item(Loc.T("Merkez ödeneği · son gün"), paid.Central);
            Item(Loc.T("Bölgeler · son gün"), paid.RegionalBase);
            Item(Loc.T("Bütçe anlaşması · son gün"), paid.BudgetBonus);
            Item(Loc.T("Hizalama · son ödenen"), -spent);
            Item(Loc.T("Sonraki gün brüt tahmin"), income.Total);
            Item(Loc.T("Hizalama · tam hız ihtiyacı"), -sim.ResearchNeed);
            Item(Loc.T("Sonraki gün araştırma tahmini"), -sim.NextResearchPayment);
            foreach (var t in sim.TransactionLedger.Where(t => t.Kind != "income" && t.Kind != "research" && (t.Fon != 0 || t.Sg != 0)).Reverse().Take(4))
            {
                string kind = t.Kind switch
                {
                    "operation" => GameData.Operation(t.Id)?.Name ?? Loc.T("Geliştirme"), "recovery" => Loc.T("Altyapı"), "diplomacy" => Loc.T("Diplomasi"),
                    "gift" => Loc.T("Hediye"), "donation" => Loc.T("Bağış"), "hire" => Loc.T("Ekip"),
                    _ => Loc.T("Tek seferlik işlem"),
                };
                b.Items.Add(new BreakdownItemVM
                {
                    Label = Loc.F("G{0} · {1}", t.Day, kind),
                    Value = Loc.F("{0:+0.##;-0.##;0} F · {1:+0.##;-0.##;0} SG", t.Fon, t.Sg),
                    Status = t.Fon >= 0 && t.Sg >= 0 ? "good" : "threat",
                });
            }
            b.Total = Loc.F("Son gün net {0:+0.00;-0.00;0}", paid.Total - spent);
            b.TotalRight = Loc.T("Peşin işlemler ayrı gösterilir");
        }
    }
}
