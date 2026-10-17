using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>Serileştirilmiş kimlikle deniz, HUD etiketi ve danışman fişinin fare olaylarını iletir.</summary>
    public sealed class SadePointerTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] string id;
        public string Id => id;
        public void SetId(string newId) => id = newId;
        public event Action<string> Clicked;
        public event Action<string, bool> Hovered;
        public void OnPointerEnter(PointerEventData e) => Hovered?.Invoke(id, true);
        public void OnPointerExit(PointerEventData e) => Hovered?.Invoke(id, false);
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left) Clicked?.Invoke(id);
        }
    }
}
