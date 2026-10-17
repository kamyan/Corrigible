using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Corrigible.Desk.Bridge
{
    /// <summary>Ana masanın EventSystem'i eklemeli baskında korunur; tek başına sahnede yerel sistem kullanılır.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class RaidEventSystemGate : MonoBehaviour
    {
        [SerializeField] EventSystem localSystem;
        [SerializeField] Behaviour localModule;
        void Awake()
        {
            bool other=FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Any(s=>s!=localSystem&&s.isActiveAndEnabled);
            if(other){localSystem.enabled=false;localModule.enabled=false;}
        }
    }
}
