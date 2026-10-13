using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Üstüne gelme / çıkma / tıklama olaylarını C# olayı olarak aktarır (sayaç etiketi, teleks penceresi, danışman mesajı).
    /// Raycast için aynı nesnede <see cref="DeskHitArea"/> gerekir.
    /// </summary>
    public sealed class DeskHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public event Action<DeskHover> Entered, Exited, Clicked;
        public bool IsOver { get; private set; }

        public void OnPointerEnter(PointerEventData e) { IsOver = true; Entered?.Invoke(this); }
        public void OnPointerExit(PointerEventData e) { IsOver = false; Exited?.Invoke(this); }
        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(this);

        void OnDisable()
        {
            if (!IsOver) return;
            IsOver = false;
            Exited?.Invoke(this);
        }
    }
}
