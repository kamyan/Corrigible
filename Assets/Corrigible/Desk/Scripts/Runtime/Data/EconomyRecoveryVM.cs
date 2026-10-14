using System;

namespace Corrigible.Desk
{
    [Serializable]
    public sealed class EconomyRecoveryVM
    {
        public string Title, RecoveryText, DiplomacyText;
        public CommandVM Recovery, Diplomacy;
    }
}
