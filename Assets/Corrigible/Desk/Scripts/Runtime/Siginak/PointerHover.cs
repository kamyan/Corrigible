using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>UI öğesinin üzerinde imleç var mı (K7: KAYITTAN DEVAM sekmesi üzerine gelince 6 px dışarı kayar).</summary>
    public sealed class PointerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool Over { get; private set; }

        public void OnPointerEnter(PointerEventData eventData) => Over = true;
        public void OnPointerExit(PointerEventData eventData) => Over = false;
        void OnDisable() => Over = false;
    }
}
