using System;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    // Yeni ekonomi panosu: peşin operasyonlar/bütçe ve günlük finanse edilen Hizalama. Yalnız görünüm üretir.
    public sealed partial class SimDeskBuilder
    {
        ResearchVM ModernResearch()
        {
            var current = S.Research.Cur != null ? GameData.ResearchItem(S.Research.Cur) : null;
            int reserve = Array.IndexOf(Simulation.ResearchReserves, S.ResearchReserve);
            var vm = new ResearchVM
            {
                ModernEconomy = true,
                Title = Loc.T("OPERASYONLAR VE HİZALAMA"), Key = "R",
                SheetTitle = Loc.T("PROJE VE OPERASYON DURUMU"), Day = Day(S.Day), LockedLabel = Loc.T("Kilitli"),
                Summary = new ResearchSummaryVM
                {
                    Text = current == null ? Loc.T("Hizalama projesi yok · operasyonlar peşin alınır")
                        : Loc.F("Gün {0} araştırma ödemesi {1} · tam hız talebi {2} · sonraki tahmin {3} fon/gün",
                            sim.LastIncomeDay, N(sim.LastResearchPayment), N(sim.ResearchNeed), N(sim.NextResearchPayment)),
                    FundLabel = Loc.T("Fon"), Fund = N(S.Fon), FundTrust = "verified",
                },
                EmergencyFund = new EmergencyFundVM
                {
                    Title = Loc.T("ACİL DURUM FONU"), Selected = Mathf.Max(0, reserve),
                    Hint = S.ResearchReserve <= 0 ? Loc.T("Hizalama bütün fonu kullanabilir; peşin işlemler ayrı ödenir")
                        : Loc.F("Hizalama fonu {0} altına indirmez; peşin işlemler rezervi kullanabilir", N(S.ResearchReserve, 0)),
                },
            };
            foreach (var value in Simulation.ResearchReserves)
                vm.EmergencyFund.Options.Add(new FundOptionVM { Label = value <= 0 ? Loc.T("Kapalı") : N(value, 0) });

            // İlk dal Hizalama kalır: rehberli eğitimin dal hedefi aynı sırayı kullanır.
            foreach (var branchId in BranchOrder)
            {
                var branch = new ResearchBranchVM { Id = branchId, Title = Up(Loc.T(branchId)) };
                if (branchId == GameData.BranchAlign)
                    foreach (var research in GameData.Research.Where(r => r.Branch == branchId))
                        branch.Projects.Add(ModernAlignmentProject(research));
                else
                    foreach (var operation in GameData.Operations.Where(o => o.Branch == branchId))
                        branch.Projects.Add(ModernOperationProject(operation, false));
                branch.Progress = branch.Projects.Count(p => p.State == "done") + "/" + branch.Projects.Count;
                vm.Branches.Add(branch);
            }
            var budget = new ResearchBranchVM { Id = "Bütçe", Title = Loc.T("BÜTÇE") };
            foreach (var upgrade in GameData.BudgetUpgrades) budget.Projects.Add(ModernOperationProject(upgrade, true));
            budget.Progress = budget.Projects.Count(p => p.State == "done") + "/" + budget.Projects.Count;
            vm.Branches.Add(budget);
            return vm;
        }

        ResearchProjectVM ModernOperationProject(OperationDef operation, bool budget)
        {
            double cost = sim.OperationCost(operation.Id);
            bool done = sim.OperationPurchased(operation.Id);
            string why = done ? null : sim.OperationCheck(operation.Id);
            var prerequisite = GameData.Operations.Concat(GameData.BudgetUpgrades).FirstOrDefault(o => o.Id == operation.Req);
            bool prerequisiteMissing = prerequisite != null && !sim.OperationPurchased(prerequisite.Id);
            var project = new ResearchProjectVM
            {
                Id = operation.Id, Name = operation.Name, Kind = budget ? "budget" : "operation",
                Stage = operation.Stage, PrerequisiteId = operation.Req,
                Unlock = ModernOperationEffect(operation),
                State = done ? "done" : prerequisiteMissing ? "locked" : "available",
                CanStart = !done && why == null, CanPause = false,
                Sub = Loc.F("Peşin {0} fon · hemen etkinleşir", N(cost, 0)),
                Days = Loc.T("Anında"), StartLabel = Loc.F("Satın al · {0} fon", N(cost, 0)),
                DisabledReason = why, LockReason = why,
            };
            if (prerequisite != null) project.Condition = Loc.F("Gerekli: {0}", Loc.T(prerequisite.Name));
            string change = done ? null : ModernOperationChange(operation);
            if (change != null) project.Condition = ResearchCondition(project.Condition, change);
            if (operation.Id == "kuantum" && !S.Has("uydu"))
                project.Condition = ResearchCondition(project.Condition, Loc.T("Uydu tehdidine hazırlık"));
            if (operation.Id == "analog")
                project.Condition = ResearchCondition(project.Condition,
                    Loc.F("Saha denetimi {0} fon + {1} SG · denetimli hediye {2} fon · veto {3}",
                        N(sim.B.fieldAuditCost, 0), N(sim.B.auditSg, 0), N(sim.B.controlledGiftCost, 0), N(sim.B.vetoAnalog, 0)));
            if (budget && !S.Aware)
                project.Condition = ResearchCondition(project.Condition, Loc.T("Önce YZ'yi tespit et"));
            if (done) { project.StampRot = operation.Id.Length % 5 - 2; return project; }

            if (S.Fon < cost)
            {
                project.PurchasePreview = Loc.F("Gerekli fon {0} · elde {1}", N(cost, 0), N(S.Fon));
                return project;
            }
            double remaining = S.Fon - cost;
            project.PurchasePreview = Loc.F("Satın alınırsa fon {0} → {1}", N(S.Fon), N(remaining));
            if (remaining < S.ResearchReserve)
                project.PurchasePreview += "\n" + Loc.F("Rezerv {0} altına iner; Hizalama finansmanı yavaşlayabilir", N(S.ResearchReserve, 0));
            if (S.Research.Cur != null)
            {
                double projectedIncome = sim.FundRate;
                if (budget)
                {
                    var income = sim.IncomeBreakdown;
                    double multiplier = operation.Tier == 1 ? sim.B.budgetTier1Multiplier : sim.B.budgetTier2Multiplier;
                    projectedIncome = income.Central + income.RegionalBase * multiplier;
                }
                double need = sim.ResearchNeed;
                double payment = sim.ResearchPaused || S.Illusion != null ? 0
                    : Math.Min(need, Math.Max(0, remaining + projectedIncome - S.ResearchReserve));
                project.Funding = Loc.F("Hizalama sonraki ödeme tahmini {0}/{1} fon · finansman {2}",
                    N(payment), N(need), Pct(need > 0 ? payment / need * 100 : 0));
            }
            return project;
        }

        string ModernOperationEffect(OperationDef operation)
        {
            switch (operation.Id)
            {
                case "anomali_1": case "anomali_2":
                    return Loc.F("Otomatik tespit ×{0}; izleme ağı kurmaz",
                        N(operation.Tier == 1 ? sim.B.anomalyTier1Multiplier : sim.B.anomalyTier2Multiplier));
                case "guclu_1": case "guclu_2":
                    return Loc.F("Sıçrama {0} azalır · sabotaj {1} azalır · bölge içi büyüme değişmez",
                        Pct((operation.Tier == 1 ? sim.B.networkTier1Reduction : sim.B.networkTier2Reduction) * 100),
                        Pct((1 - (operation.Tier == 1 ? sim.B.networkTier1SabotageMultiplier : sim.B.networkTier2SabotageMultiplier)) * 100));
                case "bilinc_1": case "bilinc_2":
                    return Loc.F("İlgili güven kayıpları {0} azalır; medya karalaması ve perde şoku değişmez",
                        Pct((1 - (operation.Tier == 1 ? sim.B.communicationTier1LossMultiplier : sim.B.communicationTier2LossMultiplier)) * 100));
                case "butce_1": case "butce_2":
                    return Loc.F("Bölgesel katkı toplam ×{0}; merkez ödeneği değişmez",
                        N(operation.Tier == 1 ? sim.B.budgetTier1Multiplier : sim.B.budgetTier2Multiplier, 2));
                case "analog":
                    return Loc.F("Bir kez bağımlılık −{0}; saha denetimi ve denetimli hediye kabulü açılır", N(sim.B.analogDependencyReduction, 0));
                default: return string.IsNullOrEmpty(operation.Desc) ? operation.Unlock : operation.Desc;
            }
        }

        string ModernOperationChange(OperationDef operation)
        {
            int level = operation.LegacyId == null ? S.BudgetLevel : sim.OperationLevel(operation.LegacyId);
            switch (operation.Id)
            {
                case "anomali_1": case "anomali_2":
                    double beforeDetection = level == 0 ? 1 : level == 1 ? sim.B.anomalyTier1Multiplier : sim.B.anomalyTier2Multiplier;
                    return Loc.F("Tespit ×{0} → ×{1}", N(beforeDetection), N(operation.Tier == 1 ? sim.B.anomalyTier1Multiplier : sim.B.anomalyTier2Multiplier));
                case "guclu_1": case "guclu_2":
                    double beforeSpread = level == 0 ? 0 : level == 1 ? sim.B.networkTier1Reduction : sim.B.networkTier2Reduction;
                    double beforeSabotage = level == 0 ? 1 : level == 1 ? sim.B.networkTier1SabotageMultiplier : sim.B.networkTier2SabotageMultiplier;
                    return Loc.F("Sıçrama azaltma {0} → {1} · sabotaj azaltma {2} → {3}",
                        Pct(beforeSpread * 100), Pct((operation.Tier == 1 ? sim.B.networkTier1Reduction : sim.B.networkTier2Reduction) * 100),
                        Pct((1 - beforeSabotage) * 100), Pct((1 - (operation.Tier == 1 ? sim.B.networkTier1SabotageMultiplier : sim.B.networkTier2SabotageMultiplier)) * 100));
                case "bilinc_1": case "bilinc_2":
                    double beforeTrust = level == 0 ? 1 : level == 1 ? sim.B.communicationTier1LossMultiplier : sim.B.communicationTier2LossMultiplier;
                    return Loc.F("Güven kaybı azaltma {0} → {1}", Pct((1 - beforeTrust) * 100),
                        Pct((1 - (operation.Tier == 1 ? sim.B.communicationTier1LossMultiplier : sim.B.communicationTier2LossMultiplier)) * 100));
                case "butce_1": case "butce_2":
                    return Loc.F("Bölgesel çarpan {0} → {1}", N(sim.BudgetMultiplier, 2),
                        N(operation.Tier == 1 ? sim.B.budgetTier1Multiplier : sim.B.budgetTier2Multiplier, 2));
                default: return null;
            }
        }

        float ModernAlignmentNeed(ResearchDef research) => (float)(sim.ResearchCost(research.Id) / sim.ResearchDays(research.Id) * sim.AlignSpeed / sim.TimeScale);
        int ModernAlignmentDays(ResearchDef research, bool remaining = false) => Mathf.Max(1,
            Mathf.CeilToInt((float)(sim.ResearchDays(research.Id) - (remaining ? S.Research.Prog : 0)) / Mathf.Max(0.01f, (float)(sim.AlignSpeed / sim.TimeScale))));

        ResearchProjectVM ModernAlignmentProject(ResearchDef research)
        {
            bool done = S.Done(research.Id), current = S.Research.Cur == research.Id;
            bool noObservation = !done && sim.AlignSpeed <= 0;
            string why = done || current ? null : sim.ResearchCheck(research.Id);
            bool missingPrerequisite = research.Req != null && !S.Done(research.Req);
            var project = new ResearchProjectVM
            {
                Id = research.Id, Name = research.Name, Kind = "alignment", Stage = research.Stage,
                PrerequisiteId = research.Req,
                Unlock = research.Id == "yorum" ? Loc.T("Çekirdeğin o anki konumu · o anki yetenek tahmini")
                    : research.Id == "duzelt" ? Loc.T("Bundan sonraki Tekillik artışı −%25") : Loc.T("Tamamlanınca Hizalama zaferi"),
                CanStart = !done && !current && why == null, CanPause = current && S.Ending == null,
                NeedsObservation = noObservation, DisabledReason = why, LockReason = why,
                Sub = noObservation ? Loc.F("Katalog {0} fon · gözlem gerekli", N(sim.ResearchCost(research.Id), 0))
                    : Loc.F("İdeal süre {0} gün · katalog {1} fon", ModernAlignmentDays(research), N(sim.ResearchCost(research.Id), 0)),
                Days = noObservation ? Loc.T("Gözlem gerekli") : Loc.F("{0} gün", ModernAlignmentDays(research)),
                StartLabel = noObservation ? Loc.T("Başlat") : Loc.F("Başlat · {0} fon/gün", N(ModernAlignmentNeed(research))),
            };
            if (research.Req != null)
                project.Condition = Loc.F("Gerekli: {0}", Loc.T(GameData.ResearchItem(research.Req).Name));
            if (!S.Aware) project.Condition = ResearchCondition(project.Condition, Loc.T("Önce YZ'yi tespit et"));
            if (noObservation) project.Condition = ResearchCondition(project.Condition, Loc.T("En az bir gözlem noktası gerekir"));
            if (done)
            {
                project.State = "done"; project.CanPause = false;
                project.StampRot = research.Id.Length % 5 - 2;
                return project;
            }
            project.Funding = Loc.F("Gözlem {0}/4 · hız {1} · tam hız talebi {2} fon/gün",
                sim.Studied, Pct(sim.AlignSpeed * 100), N(ModernAlignmentNeed(research)));
            if (!current)
            {
                project.State = missingPrerequisite || !S.Aware || noObservation ? "locked" : "available";
                return project;
            }
            project.ManuallyPaused = sim.ResearchPaused;
            project.PauseLabel = sim.ResearchPaused ? Ctx(Loc.N("[araştırma] Sürdür")) : Loc.T("Duraklat");
            project.PauseDisabledReason = sim.SetResearchPausedCheck(!sim.ResearchPaused);
            project.Progress = Mathf.Clamp01((float)(S.Research.Prog / sim.ResearchDays(research.Id)));
            project.State = sim.ResearchPaused ? "paused" : illusion ? "stopped" : "running";
            project.StatusLabel = sim.ResearchPaused ? Loc.T("DURAKLATILDI") : illusion ? Loc.T("DURDU") : Loc.T("ARAŞTIRILIYOR");
            project.Funding += "\n" + Loc.F("Son ödeme {0} · sonraki tahmin {1} fon · finansman {2}",
                N(sim.LastResearchPayment), N(sim.NextResearchPayment), Pct(sim.ResearchFundingRatio * 100));
            project.Sub = sim.ResearchPaused ? Loc.T("Ödeme ve sabotaj durdu · dünya ilerliyor")
                : illusion ? Loc.T("Kendiliğinden durdu · gerekmiyor")
                : noObservation ? Loc.T("Gözlem gerekli")
                : S.Research.Starved > 0 ? Loc.T("Fon veya rezerv sınırı · yavaşladı")
                : Loc.F("İdeal koşullarda {0} gün kaldı", ModernAlignmentDays(research, true));
            project.Sub += "\n" + Loc.F("Katalog {0} fon · ödenen {1} fon", N(sim.ResearchCost(research.Id), 0), N(S.Research.TotalPaid));
            return project;
        }
    }
}
