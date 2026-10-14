using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    [Serializable]
    public struct IncomeBreakdown
    {
        public double Central, RegionalBase, BudgetBonus;
        public double Total => (Central + RegionalBase) + BudgetBonus;
    }

    [Serializable]
    public sealed class EconomyTransaction
    {
        public int Day;
        public string Kind, Id;
        /// <summary>Stok değişimleri: gelir pozitif, harcama negatiftir.</summary>
        public double Fon, Sg;
    }

    [Serializable]
    public sealed class RecoveryJob
    {
        public string Region, Team;
        public int FinishDay;
    }

    /// <summary>Sürüm 3 ekonomisi. Eski kayıtların fiyat, ödeme ve RNG yollarına ek tüketim yapmaz.</summary>
    public sealed partial class Simulation
    {
        public bool ModernEconomy => S.EconomyVersion >= 3;
        public bool ResearchPaused => ModernEconomy && S.Research.Paused;
        public bool ResearchActive => S.Research.Cur != null && !ResearchPaused && S.Illusion == null;
        public IncomeBreakdown LastIncome => S.LastIncome;
        public int LastIncomeDay => S.LastIncomeDay;
        public double LastResearchPayment => S.LastResearchPayment;
        public IReadOnlyList<EconomyTransaction> TransactionLedger => S.TransactionLedger;
        public double LastNetCashFlow => LastIncome.Total - LastResearchPayment;

        public IncomeBreakdown IncomeBreakdown
        {
            get
            {
                double regional = Per(S.Regions.Sum(r => r.Left || r.Off > 0 ? 0 : r.Econ * r.Trust / 100) / B.fonDiv * (S.Aware ? 1 : B.preAwareFund));
                if (!ModernEconomy) return new IncomeBreakdown { RegionalBase = regional };
                double baseContribution = regional * B.regionalFundMul;
                return new IncomeBreakdown
                {
                    Central = Per(B.centralFund), RegionalBase = baseContribution,
                    BudgetBonus = baseContribution * (BudgetMultiplier - 1),
                };
            }
        }

        public double BudgetMultiplier => !ModernEconomy || S.BudgetLevel == 0 ? 1 : S.BudgetLevel == 1 ? B.budgetTier1Multiplier : B.budgetTier2Multiplier;
        public double NextResearchPayment => !ResearchActive ? 0 : Math.Min(ModernEconomy ? ResearchNeed : Per(ResearchNeed), Math.Max(0, S.Fon + FundRate - S.ResearchReserve));
        public double ResearchFundingRatio => ResearchNeed > 0 ? NextResearchPayment / (ModernEconomy ? ResearchNeed : Per(ResearchNeed)) : 0;

        public int OperationLevel(string id)
        {
            if (!ModernEconomy) return S.Done(id) ? 2 : 0;
            return S.Operations.Contains(id + "_2") ? 2 : S.Operations.Contains(id + "_1") ? 1 : 0;
        }

        public bool OperationPurchased(string id)
        {
            if (id == "butce_1") return S.BudgetLevel >= 1;
            if (id == "butce_2") return S.BudgetLevel >= 2;
            return S.Operations.Contains(id);
        }

        public double OperationCost(string id) => id switch
        {
            "anomali_1" => B.anomalyTier1Cost, "anomali_2" => B.anomalyTier2Cost,
            "dogrulama" => B.verificationCost,
            "guclu_1" => B.networkTier1Cost, "guclu_2" => B.networkTier2Cost,
            "kuantum" => B.quantumCost,
            "bilinc_1" => B.communicationTier1Cost, "bilinc_2" => B.communicationTier2Cost,
            "analog" => B.analogCost,
            "butce_1" => B.budgetTier1Cost, "butce_2" => B.budgetTier2Cost,
            _ => 0,
        };

        public string OperationCheck(string id)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (!ModernEconomy) return Loc.T("Bu kayıt eski ekonomi kurallarıyla sürüyor.");
            var op = GameData.Operation(id);
            if (op == null) return Loc.T("Geçersiz operasyon geliştirmesi.");
            if (OperationPurchased(id)) return Loc.T("Bu geliştirme satın alındı.");
            if (op.Req != null && !OperationPurchased(op.Req)) return Loc.F("Önce {0} satın alınmalı.", GameData.OperationItem(op.Req).Name);
            if (id == "dogrulama" && !MediaOn) return Loc.T("Bu krizde medya tehdidi kapalı.");
            if (op.LegacyId == null && !S.Aware) return Loc.T("Bütçe anlaşması için önce YZ tespit edilmeli.");
            if (S.Fon < OperationCost(id)) return Loc.F("{0} fon gerekiyor.", OperationCost(id));
            return null;
        }

        public ActResult BuyOperation(string id) => RunCommand(() => BuyOperationCore(id), "operation", id);
        public ActResult BuyBudgetUpgrade() => BuyOperation(S.BudgetLevel == 0 ? "butce_1" : "butce_2");

        ActResult BuyOperationCore(string id)
        {
            string why = OperationCheck(id);
            if (why != null) return ActResult.Fail(why);
            var op = GameData.OperationItem(id);
            S.Fon -= OperationCost(id);
            if (op.LegacyId == null)
            {
                S.BudgetLevel = op.Tier;
                return ActResult.Success();
            }
            S.Operations.Add(id);
            if (op.Tier == 0 || op.Tier == 2) S.Research.Done.Add(op.LegacyId);
            if (id == "analog") foreach (var r in S.Regions) r.Dep = Math.Max(0, r.Dep - B.analogDependencyReduction);
            if (S.FirstBranch == null) S.FirstBranch = op.Branch;
            CheckPredictions();
            AddNews(Loc.F("Operasyon geliştirmesi satın alındı: {0}.", op.Name));
            return ActResult.Success();
        }

        public double ResearchCost(string id)
        {
            if (!ModernEconomy) return GameData.ResearchItem(id).Cost;
            return id switch { "yorum" => B.interpretationCost, "duzelt" => B.corrigibilityCost, "kilit" => B.valueLockCost, _ => GameData.ResearchItem(id).Cost };
        }

        public int ResearchDays(string id)
        {
            if (!ModernEconomy) return GameData.ResearchItem(id).Days;
            return id switch { "yorum" => B.interpretationDays, "duzelt" => B.corrigibilityDays, "kilit" => B.valueLockDays, _ => GameData.ResearchItem(id).Days };
        }

        public string ResearchCheck(string id)
        {
            if (!GameData.Research.Any(r => r.Id == id)) return Loc.T("Geçersiz araştırma.");
            var rs = GameData.ResearchItem(id);
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (ModernEconomy && rs.Branch != GameData.BranchAlign) return Loc.T("Operasyon geliştirmeleri peşin satın alınır.");
            if (S.Done(id)) return Loc.T("Bu araştırma tamamlandı.");
            if (S.Research.Cur != null) return Loc.T("Aynı anda tek araştırma yürür.");
            if (rs.Req != null && !S.Done(rs.Req)) return Loc.F("Önce {0} tamamlanmalı.", GameData.ResearchItem(rs.Req).Name);
            if (rs.Branch == GameData.BranchAlign && !S.Aware) return Loc.T("Gözlemlenmemiş bir zekâ hizalanamaz. Önce YZ’yi tespit et.");
            if (ModernEconomy && Studied < 1) return Loc.T("Hizalama için en az bir gözlem noktası gerekiyor.");
            return null;
        }

        public string SetResearchPausedCheck(bool paused)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (!ModernEconomy) return Loc.T("Bu kayıt eski ekonomi kurallarıyla sürüyor.");
            if (S.Research.Cur == null) return Loc.T("Yürüyen Hizalama projesi yok.");
            if (S.PendingEvent != null) return Loc.T("Önce bekleyen olay kartını çöz.");
            if (S.Research.Paused == paused) return paused ? Loc.T("Proje zaten duraklatıldı.") : Loc.T("Proje zaten sürüyor.");
            return null;
        }

        public string ResearchPausedCheck(bool paused) => SetResearchPausedCheck(paused);

        public ActResult SetResearchPaused(bool paused) => RunCommand(() =>
        {
            string why = SetResearchPausedCheck(paused);
            if (why != null) return ActResult.Fail(why);
            S.Research.Paused = paused;
            return ActResult.Success();
        }, "research_pause", paused ? "1" : "0");

        public double RecoveryCost => B.recoveryCost;
        public int RecoveryDays => B.recoveryDays;
        public double RecoveryExpectedGain(string id)
        {
            var r = S.Region(id);
            return r == null ? 0 : Math.Min(B.recoveryGain, Math.Max(0, Def(id).Economy - r.Econ));
        }

        public string RecoveryCheck(string id)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (!ModernEconomy) return Loc.T("Bu kayıt eski ekonomi kurallarıyla sürüyor.");
            var r = S.Region(id);
            if (r == null) return Loc.T("Geçersiz bölge.");
            if (r.Left) return Loc.T("Bölge ittifak dışı.");
            if (r.Off > 0) return Loc.T("Bölge çevrimdışı.");
            if (r.Q) return Loc.T("Önce bölgenin karantinasını kaldır.");
            if (S.RecoveryJobs.Any(j => j.Region == id)) return Loc.T("Bölgede altyapı desteği zaten sürüyor.");
            if (Def(id).Economy - r.Econ < B.recoveryMinGap) return Loc.F("Altyapı desteği için en az {0} ekonomi açığı gerekiyor.", B.recoveryMinGap);
            if (TeamsOn && IdleTeamAt(id) == null) return Loc.T("Bölgede boşta saha ekibi yok.");
            if (S.Fon < RecoveryCost) return Loc.F("{0} fon gerekiyor.", RecoveryCost);
            return null;
        }

        public ActResult StartRecovery(string id) => RunCommand(() =>
        {
            string why = RecoveryCheck(id);
            if (why != null) return ActResult.Fail(why);
            var team = TeamsOn ? IdleTeamAt(id) : null;
            S.Fon -= RecoveryCost;
            if (team != null) { team.Busy = B.recoveryDays; team.Task = "recovery"; }
            S.RecoveryJobs.Add(new RecoveryJob { Region = id, Team = team?.Id, FinishDay = S.Day + B.recoveryDays });
            return ActResult.Success();
        }, "recovery", id);

        void StepRecovery()
        {
            if (!ModernEconomy) return;
            for (int i = 0; i < S.RecoveryJobs.Count;)
            {
                var job = S.RecoveryJobs[i];
                if (S.Day < job.FinishDay) { i++; continue; }
                var r = S.Region(job.Region);
                bool valid = r != null && !r.Left && r.Off == 0 && !r.Q && (job.Team == null || Team(job.Team) != null);
                if (valid)
                {
                    double gain = RecoveryExpectedGain(job.Region);
                    r.Econ += gain;
                    AddLog(Loc.F("{0}: altyapı desteği tamamlandı, ekonomi +{1:0.0}.", Def(job.Region).Name, gain), "economy");
                }
                else AddLog(Loc.F("{0}: altyapı desteği erişim kaybı nedeniyle sonuç vermedi. Fon iade edilmedi.", Def(job.Region).Name), "economy");
                S.RecoveryJobs.RemoveAt(i);
            }
        }

        public double DiplomacyCost => B.diplomacyCost;
        public double DiplomacySg => B.diplomacySg;
        public double DiplomaticTrustAfter(string id)
        {
            var r = S.Region(id);
            if (r == null) return 0;
            if (r.Trust >= B.diplomacyTrustCap) return r.Trust;
            double trust = Math.Min(B.diplomacyTrustCap, r.Trust + B.diplomacyTrustGain);
            return r.Left ? Math.Max(B.diplomacyReturnTrust, trust) : trust;
        }

        public string DiplomaticCheck(string id)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (!ModernEconomy) return Loc.T("Bu kayıt eski ekonomi kurallarıyla sürüyor.");
            var r = S.Region(id);
            if (r == null) return Loc.T("Geçersiz bölge.");
            if (r.Trust >= B.diplomacyTrustCap) return Loc.F("Güven zaten {0} veya üstünde.", B.diplomacyTrustCap);
            if (S.Day < r.DiplomacyUntil) return Loc.F("Yeni görüşme için {0} gün bekle.", r.DiplomacyUntil - S.Day);
            if (S.Fon < DiplomacyCost || S.Sg < DiplomacySg) return Loc.F("Diplomatik görüşme {0} fon ve {1} siyasi güç ister.", DiplomacyCost, DiplomacySg);
            return null;
        }

        public ActResult DiplomaticTalk(string id) => RunCommand(() =>
        {
            string why = DiplomaticCheck(id);
            if (why != null) return ActResult.Fail(why);
            var r = S.Region(id);
            S.Fon -= DiplomacyCost; S.Sg -= DiplomacySg;
            r.Trust = DiplomaticTrustAfter(id);
            if (r.Trust >= B.diplomacyReturnTrust) r.Left = false;
            r.DiplomacyUntil = S.Day + Dur(B.diplomacyCooldownDays);
            return ActResult.Success();
        }, "diplomacy", id);

        ActResult RunCommand(Func<ActResult> apply, string kind, string a = null, string b = null)
        {
            double fon = S.Fon, sg = S.Sg;
            var result = apply();
            if (result.Ok) AddTransaction(kind == "act" ? a : kind, b ?? a, S.Fon - fon, S.Sg - sg);
            return Rec(result, kind, a, b);
        }

        void AddTransaction(string kind, string id, double fon, double sg = 0)
        {
            if (fon == 0 && sg == 0) return;
            S.TransactionLedger.Add(new EconomyTransaction { Day = S.Day, Kind = kind, Id = id, Fon = fon, Sg = sg });
        }

        double DetectionMultiplier => !ModernEconomy ? (S.Done("anomali") ? 2 : 1) : OperationLevel("anomali") == 2 ? B.anomalyTier2Multiplier : OperationLevel("anomali") == 1 ? B.anomalyTier1Multiplier : 1;
        double NetworkReduction => !ModernEconomy ? (S.Done("guclu") ? 0.3 : 0) : OperationLevel("guclu") == 2 ? B.networkTier2Reduction : OperationLevel("guclu") == 1 ? B.networkTier1Reduction : 0;
        double NetworkSabotageMultiplier => !ModernEconomy ? (S.Done("guclu") ? 0.5 : 1) : OperationLevel("guclu") == 2 ? B.networkTier2SabotageMultiplier : OperationLevel("guclu") == 1 ? B.networkTier1SabotageMultiplier : 1;
        double CommunicationLossMultiplier => !ModernEconomy ? (S.Done("bilinc") ? 0.5 : 1) : OperationLevel("bilinc") == 2 ? B.communicationTier2LossMultiplier : OperationLevel("bilinc") == 1 ? B.communicationTier1LossMultiplier : 1;
    }
}
