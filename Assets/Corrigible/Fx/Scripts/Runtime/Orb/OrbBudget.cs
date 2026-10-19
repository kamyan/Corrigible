using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>Sıra başını OrbClock saklar; ertelenen küre bir sonraki karede öncelik alır.</summary>
    public sealed class OrbBudget : MonoBehaviour
    {
        [SerializeField,Min(0)] public int maxOrbsPerFrame;
        [SerializeField,Min(0)] public float maxCpuMilliseconds;
        public bool Allows(int rendered,double elapsedMs)=>(maxOrbsPerFrame<=0||rendered<maxOrbsPerFrame)&&(maxCpuMilliseconds<=0||elapsedMs<maxCpuMilliseconds);
    }
}
