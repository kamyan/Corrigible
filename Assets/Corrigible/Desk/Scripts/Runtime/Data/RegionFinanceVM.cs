using System;

namespace Corrigible.Desk
{
    [Serializable]
    public sealed class RegionFinanceVM
    {
        public string Title, Contribution, Note;
        public double PerDay, SharePercent;
    }
}
