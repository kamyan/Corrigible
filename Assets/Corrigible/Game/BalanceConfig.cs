using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Game
{
    /// <summary>Denge değerleri. Tasarımcı bu asset'i Inspector'da düzenler; kod değişmez.</summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "Corrigible/Balance Config")]
    public sealed class BalanceConfig : ScriptableObject
    {
        public Balance balance = new Balance();

        /// <summary>Oyun sırasında asset'in kendisi değişmesin diye kopya verilir.</summary>
        public Balance CreateRuntimeCopy() => (balance ?? new Balance()).Clone();
    }
}
