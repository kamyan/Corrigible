using UnityEngine;
using Corrigible.Ayna.Core;
namespace Corrigible.Ayna.Game
{
    [CreateAssetMenu(menuName = "Corrigible/AYNA/Denge")]
    public sealed class AynaBalanceConfig : ScriptableObject
    {
        [SerializeField] AynaBalance values = new AynaBalance { TacticalPlanning = true };
        public AynaBalance Snapshot() => values.Copy();
    }
}
