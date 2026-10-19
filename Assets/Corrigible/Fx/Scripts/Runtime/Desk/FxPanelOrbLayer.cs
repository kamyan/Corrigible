using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>Panel içi RawImage, ortak cam mesh'inden sonra çizilir. Prefab kökü Canvas ayarını sıfırlayabilir.</summary>
    public sealed class FxPanelOrbLayer : MonoBehaviour
    {
        [SerializeField] public Canvas canvas;
        void OnEnable(){if(canvas!=null){canvas.overrideSorting=true;canvas.sortingOrder=5;}}
    }
}
