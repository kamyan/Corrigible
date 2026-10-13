using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Pencere olmayan bir paneli (harita, hedef kartı) sürükleyip bırakılan yerde bırakır. Sürükleme panelin herhangi bir
    /// yerinden başlar (düğmeler ve harita düğümleri tıklamayı kendileri alır; sürükleme eşiği aşılınca panel taşınır).
    /// Görünen kısım (<see cref="visible"/>) alanın (<see cref="bounds"/>) içinde tutulur. Yerleşim sahnededir: oyun
    /// başlarken panel sahnedeki yerinde durur.
    /// </summary>
    public sealed class DeskDraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("Taşınan panel (boşsa bu nesne).")]
        [SerializeField] RectTransform target;
        [Tooltip("Ekranda görünen kısım (ör. haritanın kâğıdı); alanın içinde tutulur. Boşsa panelin kendisi.")]
        [SerializeField] RectTransform visible;
        [Tooltip("İçinde kalınacak alan (boşsa canvas).")]
        [SerializeField] RectTransform bounds;
        [Tooltip("Sürüklerken kardeşlerinin üstüne çıkar (harita: hayır, seçim ipliği üstte kalmalı).")]
        [SerializeField] bool bringToFront;

        bool dragging;
        static readonly Vector3[] corners = new Vector3[4];

        RectTransform Target => target != null ? target : (RectTransform)transform;

        public void OnBeginDrag(PointerEventData e)
        {
            dragging = e.button == PointerEventData.InputButton.Left;
            if (dragging && bringToFront) Target.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData e)
        {
            if (!dragging) return;
            var t = Target;
            var area = bounds != null ? bounds : (RectTransform)t.GetComponentInParent<Canvas>().rootCanvas.transform;
            MoveWithin(t, area, ParentDelta(t, e), visible);
        }

        public void OnEndDrag(PointerEventData e) => dragging = false;

        /// <summary>İmlecin bu karedeki hareketi panelin ebeveyn uzayında (canvas ölçeğinden bağımsız).</summary>
        public static Vector2 ParentDelta(RectTransform rt, PointerEventData e)
        {
            var parent = rt.parent as RectTransform;
            if (parent == null) return e.delta;
            var cam = e.pressEventCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, cam, out var now);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position - e.delta, cam, out var before);
            return now - before;
        }

        /// <summary>Paneli kaydırır; görünen kısmı alanın dışına taşarsa geri iter.</summary>
        public static void MoveWithin(RectTransform rt, RectTransform area, Vector2 parentDelta, RectTransform visible = null)
        {
            rt.anchoredPosition += parentDelta;
            if (area == null) return;
            if (visible == null) visible = rt;
            visible.GetWorldCorners(corners);
            Vector2 min = area.InverseTransformPoint(corners[0]), max = area.InverseTransformPoint(corners[2]);
            var r = area.rect;
            var fix = Vector2.zero;
            if (min.x < r.xMin) fix.x = r.xMin - min.x; else if (max.x > r.xMax) fix.x = r.xMax - max.x;
            if (min.y < r.yMin) fix.y = r.yMin - min.y; else if (max.y > r.yMax) fix.y = r.yMax - max.y;
            if (fix == Vector2.zero || rt.parent == null) return;
            rt.anchoredPosition += (Vector2)rt.parent.InverseTransformVector(area.TransformVector(fix));
        }
    }
}
