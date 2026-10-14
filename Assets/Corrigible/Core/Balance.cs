using System;

namespace Corrigible.Core
{
    /// <summary>
    /// Tüm denge sabitleri. Oyunda BalanceConfig ScriptableObject'i içinde düzenlenir;
    /// tasarımcı kod değiştirmeden ayar yapar. Varsayılanlar tarayıcı prototipi v2.1 ile aynıdır.
    /// </summary>
    [Serializable]
    public class Balance
    {
        // Yayılma
        public double growth = 0.05;
        public double spread = 0.3;
        public double satBeta = 0.4;
        public double spreadMin = 15;

        // Tekillik
        public double tek = 3.5;
        public double tekPow = 2;
        public double qTek = 0.2;

        // Denetim yaması
        public int patchDays = 20;
        public double patchGrowth = 0.5;

        // Ekonomi
        public double startFon = 60;
        /// <summary>İzleme ağı maliyet çarpanı (kampanya yükseltmesi).</summary>
        public double monitorCostMul = 1;
        public double startSg = 5;
        public double cpRate = 1.0;
        public double fonDiv = 150;
        public double preAwareFund = 0.4;
        /// <summary>Acil durum fonunun başlangıç değeri: araştırma fonu bunun altına indirmez.</summary>
        public double researchReserve = 0;
        public double depRate = 0.15;
        public double audit = 20;

        // Bütünleşik ekonomi (kural sürümü 3). Eski kayıtlar researchReserve değerini kullanır.
        public double modernResearchReserve = 15;
        public double centralFund = 0.75;
        public double regionalFundMul = 0.8;
        public double budgetTier1Cost = 25;
        public double budgetTier2Cost = 40;
        public double budgetTier1Multiplier = 1.15;
        public double budgetTier2Multiplier = 1.35;
        public double anomalyTier1Cost = 12;
        public double anomalyTier2Cost = 18;
        public double verificationCost = 40;
        public double networkTier1Cost = 16;
        public double networkTier2Cost = 24;
        public double quantumCost = 60;
        public double communicationTier1Cost = 15;
        public double communicationTier2Cost = 20;
        public double analogCost = 50;
        public double anomalyTier1Multiplier = 1.5;
        public double anomalyTier2Multiplier = 2;
        public double networkTier1Reduction = 0.15;
        public double networkTier2Reduction = 0.3;
        public double networkTier1SabotageMultiplier = 0.75;
        public double networkTier2SabotageMultiplier = 0.5;
        public double communicationTier1LossMultiplier = 0.75;
        public double communicationTier2LossMultiplier = 0.5;
        public double analogDependencyReduction = 20;
        public double recoveryCost = 2;
        public double recoveryGain = 15;
        public double recoveryMinGap = 5;
        public int recoveryDays = 4;
        public double diplomacyCost = 8;
        public double diplomacySg = 2;
        public double diplomacyTrustGain = 15;
        public double diplomacyTrustCap = 70;
        public double diplomacyReturnTrust = 35;
        public int diplomacyCooldownDays = 20;

        // Yeni krizlerde katalog ve eylem bedelleri de ayar asset'inden okunur.
        public double interpretationCost = 68;
        public int interpretationDays = 25;
        public double corrigibilityCost = 98;
        public int corrigibilityDays = 30;
        public double valueLockCost = 165;
        public int valueLockDays = 38;
        public double monitorBaseCost = 10;
        public double monitorComputeCost = 4;
        public double auditCost = 15;
        public double fieldAuditCost = 20;
        public double auditSg = 1;
        public double quarantineSg = 8;
        public double killSwitchBaseCost = 40;
        public double globalKillSwitchSg = 50;
        public double controlledGiftCost = 20;
        public double verificationSg = 2;
        public double teamHireCost = 45;
        public int teamHireDays = 4;
        public double raidCost = 30;
        public double raidSg = 6;
        public double sgBeforeAware = 0.05;
        public double sgAfterAware = 0.25;
        public double sgPerDetected = 0.03;
        public double dependencyCostThreshold = 40;
        public double dependencyCostDivisor = 50;
        public double economyRecoveryRate = 0.02;
        public double trustTarget = 70;
        public double trustDrift = 0.05;
        public double quarantineEconomyLoss = 1;
        public double quarantineTrustLoss = 0.4;
        public double quarantineEconomyFloor = 10;
        public double departureTrust = 20;
        public double returnTrust = 35;
        public double shutdownTrustLoss = 30;
        public double shutdownEconomyLoss = 40;
        public double shutdownDependencyLoss = 40;
        public double damagedEconomyFloor = 5;
        public int shutdownDays = 20;
        public double giftEconomyGain = 10;
        public double giftTrustGain = 5;
        public double giftDependencyGain = 15;
        public double controlledGiftDependencyGain = 5;
        public double rejectedGiftLocalTrustLoss = 15;
        public double mediaTrustLoss = 3;
        public double veilTrustLoss = 5;
        public double veilDependencyLoss = 15;
        public double raidEconomyLossPerCollateral = 5;
        public double raidTrustLossPerCollateral = 2;
        public double raidFailureTrustLoss = 10;
        public double electionSg = 5;
        public double informantCost = 35;
        public double sanctionSg = 2;
        public double pressConferenceSg = 3;
        public double academicTeamCost = 25;
        public double emergencyResponseCost = 20;
        public double donationFon = 60;
        public double aiOfferFon = 50;

        // Hizalama
        public double alignBase = 0;
        public double alignPer = 0.25;
        public double alignMax = 1;
        public double sabotage = 0.1;
        public double sabotageLoss = 4;

        // Sessiz Zafer
        public double silentInf = 60;
        public double silentDep = 40;
        public int illusionDays = 30;
        public int silentBlock = 60;

        // Veto, tahmin, hediye
        public double vetoBase = 70;
        public double vetoAnalog = 85;
        public double misreadCost = 15;
        public double rejectSg = 2;
        public double rejectTrust = 2;
        public double giftInf = 8;

        // Çekirdek
        public double coreFloor = 20;
        public double coreSeed = 8;
        public double coreTek = 0.05;
        public double salterSg = 30;
        public double coreSalterSg = 20;

        public Balance Clone() => (Balance)MemberwiseClone();
    }
}
