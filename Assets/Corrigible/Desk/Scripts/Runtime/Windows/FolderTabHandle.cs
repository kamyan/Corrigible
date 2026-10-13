using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Klasör sekmesi tutamacı: sürükleme (⠿ ve sekme başlığı) ve tıklama (arkadaki klasör öne gelir) olaylarını
    /// <see cref="WindowManager"/>'a iletir. Sekmedeki düğmeler (kapat) kendi olaylarını alır.
    /// </summary>
    public sealed class FolderTabHandle : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] FolderWindow window;

        public void OnPointerClick(PointerEventData e)
        {
            if (!e.dragging && WindowManager.Instance != null) WindowManager.Instance.BringToFront(window);
        }

        public void OnBeginDrag(PointerEventData e) { if (WindowManager.Instance != null) WindowManager.Instance.BeginDrag(window, e); }
        public void OnDrag(PointerEventData e) { if (WindowManager.Instance != null) WindowManager.Instance.Drag(window, e); }
        public void OnEndDrag(PointerEventData e) { if (WindowManager.Instance != null) WindowManager.Instance.EndDrag(window, e); }
    }
}
