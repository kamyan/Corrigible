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
        public double startSg = 5;
        public double cpRate = 1.0;
        public double fonDiv = 150;
        public double preAwareFund = 0.4;
        public double depRate = 0.15;
        public double audit = 20;

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
