using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    public enum PlanningDropKind { Job, Production, MaterialInput, Inventory, Inbox, Scrap }

    /// <summary>Geçerli görevin açık hedefi; bırakma kararı kimliklerle bağlayıcıya gider.</summary>
    public sealed class PlanningBoardDropZone : MonoBehaviour, IDropHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] PlanningDropKind kind;
        [SerializeField] string targetId;
        [SerializeField] Image face, highlight;
        [SerializeField] RectTransform cardAnchor;
        public PlanningDropKind Kind => kind;
        public string TargetId => targetId;
        public RectTransform CardAnchor => cardAnchor;
        public bool Interactable { get; set; } = true;
        public event Action<PlanningBoardCard, PlanningBoardDropZone> Dropped;
        public event Action<PlanningBoardDropZone> Clicked;
        public event Action<PlanningBoardDropZone, bool> HoverChanged;

        public void Configure(PlanningDropKind dropKind, string id, Image background, Image glow, RectTransform anchor)
        { kind = dropKind; targetId = id; face = background; highlight = glow; cardAnchor = anchor; }

        public bool Accepts(PlanningCardKind card) => kind switch
        {
            PlanningDropKind.Job => card == PlanningCardKind.Person,
            PlanningDropKind.Production => card == PlanningCardKind.Material,
            PlanningDropKind.MaterialInput => card == PlanningCardKind.Material,
            PlanningDropKind.Inventory => targetId == "materials" ? card == PlanningCardKind.Material : card == PlanningCardKind.Equipment || card == PlanningCardKind.Material,
            PlanningDropKind.Inbox => card == PlanningCardKind.Receipt,
            PlanningDropKind.Scrap => card == PlanningCardKind.Equipment,
            _ => false,
        };

        public void ShowTarget(PlanningBoardCard card)
        {
            if (highlight) highlight.enabled = card != null && Interactable && Accepts(card.Kind);
        }
        public void OnDrop(PointerEventData e)
        {
            var card = PlanningBoardCard.Dragging;
            if (Interactable && card != null && Accepts(card.Kind)) Dropped?.Invoke(card, this);
        }
        public void OnPointerClick(PointerEventData e)
        { if (Interactable && e.button == PointerEventData.InputButton.Left) Clicked?.Invoke(this); }
        public void OnPointerEnter(PointerEventData e) => HoverChanged?.Invoke(this, true);
        public void OnPointerExit(PointerEventData e) => HoverChanged?.Invoke(this, false);
    }
}
