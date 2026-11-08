using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>Boş pano yüzeyi; karta yuva seçtirmez, bırakılan konumu bağlayıcıya bildirir.</summary>
    public sealed class PlanningBoardSurface : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public bool Interactable { get; set; } = true;
        public event Action<PlanningBoardCard, PointerEventData> Dropped;
        public event Action Clicked;

        public void OnPointerClick(PointerEventData data)
        {
            if (Interactable && data.button == PointerEventData.InputButton.Left && !data.dragging && PlanningBoardCard.Dragging == null)
                Clicked?.Invoke();
        }

        public void OnDrop(PointerEventData data)
        {
            var card = PlanningBoardCard.Dragging;
            if (Interactable && card != null && card.Kind != PlanningCardKind.Receipt) Dropped?.Invoke(card, data);
        }
    }
}
