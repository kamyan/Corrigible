using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>Vardiya geliri: merkez fonu + özgür bölgelerin katkısı.</summary>
    [Serializable]
    public struct IncomeBreakdown
    {
        public double Central, RegionalBase;
        public double Total => Central + RegionalBase;
    }

    [Serializable]
    public sealed class EconomyTransaction
    {
        public int Day;
        public string Kind, Id;
        /// <summary>Fon değişimi: gelir pozitif, harcama negatiftir.</summary>
        public double Fon;
    }

    public sealed partial class Simulation
    {
        public IncomeBreakdown LastIncome => S.LastIncome;
        public int LastIncomeDay => S.LastIncomeDay;
        public IReadOnlyList<EconomyTransaction> TransactionLedger => S.TransactionLedger;

        public IncomeBreakdown IncomeBreakdown => new IncomeBreakdown
        {
            Central = B.liberationDailyFunds,
            RegionalBase = S.Liberation.Regions.Count(r => r.Completed) * B.liberationRegionIncome,
        };

        /// <summary>Komutu uygular; başarılıysa fon değişimini deftere (kimlik <paramref name="ledger"/>, yoksa b ya da a), komutu kayda yazar.</summary>
        ActResult RunCommand(Func<ActResult> apply, string kind, string a = null, string b = null, string ledger = null)
        {
            double fon = S.Fon;
            var result = apply();
            if (result.Ok) AddTransaction(kind, ledger ?? b ?? a, S.Fon - fon);
            return Rec(result, kind, a, b);
        }

        void AddTransaction(string kind, string id, double fon)
        {
            if (fon == 0) return;
            S.TransactionLedger.Add(new EconomyTransaction { Day = S.Day, Kind = kind, Id = id, Fon = fon });
        }
    }
}
