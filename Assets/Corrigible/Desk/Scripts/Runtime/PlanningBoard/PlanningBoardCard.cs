using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    public enum PlanningCardKind { Person, Equipment, Material, Recipe, Receipt, RoleTag }

    /// <summary>Panodaki tek gerçek kart; kimliği görünür metinden ve konumundan bağımsızdır.</summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class PlanningBoardCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler, IPointerDownHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] PlanningCardKind kind;
        [SerializeField] TMP_Text title, body, footer;
        [SerializeField] Image paper, accent;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform dragPlane;
        string id;
        Vector3 home;
        Vector2 grabOffset;
        Transform homeParent;
        int homeSibling;
        bool dragging, pendingHome, draggedSincePress;
        Color paperColor;

        public PlanningCardKind Kind => kind;
        public string Id => id;
        public bool Draggable { get; set; } = true;
        public bool IsDragging => dragging;
        public event Action<PlanningBoardCard> Selected;
        public event Action<PlanningBoardCard, bool> DragChanged;
        public event Action<PlanningBoardCard, PlanningBoardCard> CardDropped;
        public event Action<PlanningBoardCard, bool> HoverChanged;
        public static PlanningBoardCard Dragging { get; private set; }

        public void Configure(PlanningCardKind cardKind, TMP_Text heading, TMP_Text lines, TMP_Text bottom,
            Image sheet, Image stripe, RectTransform plane)
        {
            kind = cardKind; title = heading; body = lines; footer = bottom; paper = sheet; accent = stripe;
            dragPlane = plane; group = GetComponent<CanvasGroup>();
        }

        public void Bind(string identity, string heading, string lines, string bottom, Color color, bool selected)
        {
            id = identity;
            if (title) title.text = heading ?? "";
            if (body) body.text = lines ?? "";
            if (footer) footer.text = bottom ?? "";
            if (accent) accent.color = color;
            paperColor = SiginakTheme.Active.ColorOf("paper");
            if (paper) paper.color = selected ? Color.Lerp(paperColor, color, 0.20f) : paperColor;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && !dragging && !draggedSincePress) Selected?.Invoke(this);
        }

        public void OnPointerDown(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left)draggedSincePress=false; }

        public void OnBeginDrag(PointerEventData e)
        {
            if (!Draggable || string.IsNullOrEmpty(id) || e.button != PointerEventData.InputButton.Left) return;
            RestoreAfterDrag();
            home = transform.localPosition; homeParent = transform.parent; homeSibling = transform.GetSiblingIndex();
            draggedSincePress = true;
            dragging = true; Dragging = this;
            if (!group) group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.alpha = 0.9f;
            transform.SetParent(dragPlane, true); transform.SetAsLastSibling();
            grabOffset = Vector2.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragPlane, e.position, e.pressEventCamera, out var pointer))
                grabOffset = pointer - ((RectTransform)transform).anchoredPosition;
            Selected?.Invoke(this); DragChanged?.Invoke(this, true); OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!dragging || !dragPlane) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragPlane, e.position, e.pressEventCamera, out var p))
                ((RectTransform)transform).anchoredPosition = p - grabOffset;
        }

        /// <summary>Geçerli serbest bırakma, eski cebe dönmek yerine bu pano konumunu korur.</summary>
        public void KeepPosition(Vector2 position)
        {
            if (transform.parent != dragPlane) transform.SetParent(dragPlane, false);
            ((RectTransform)transform).anchoredPosition = position;
            home = transform.localPosition; homeParent = dragPlane; homeSibling = transform.GetSiblingIndex();
        }

        public void OnEndDrag(PointerEventData e) => FinishDrag(true);

        void FinishDrag(bool restoreHome)
        {
            if (!dragging) { if (restoreHome) RestoreAfterDrag(); return; }
            dragging = false;
            if (Dragging == this) Dragging = null;
            pendingHome = true;
            if (restoreHome) RestoreAfterDrag();
            if (group) { group.blocksRaycasts = true; group.alpha = 1; }
            DragChanged?.Invoke(this, false);
        }

        /// <summary>Unity SetActive içindeki OnDisable'da SetParent kabul etmez; yeniden çizimden önce yerine dön.</summary>
        public void RestoreAfterDrag()
        {
            if (!pendingHome || dragging) return;
            pendingHome = false;
            if (homeParent)
            {
                transform.SetParent(homeParent, false);
                transform.SetSiblingIndex(homeSibling);
                transform.localPosition = home;
            }
        }

        public void OnDrop(PointerEventData e)
        {
            var card = Dragging;
            if (card != null && card != this) CardDropped?.Invoke(card, this);
        }
        public void OnPointerEnter(PointerEventData e) => HoverChanged?.Invoke(this, true);
        public void OnPointerExit(PointerEventData e) => HoverChanged?.Invoke(this, false);
        void OnDisable() => FinishDrag(false);
    }
}
