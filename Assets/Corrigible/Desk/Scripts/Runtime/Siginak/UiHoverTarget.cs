using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kâğıt UI öğesinin imleç durumu (görev fişi, Revizyon 2 · S2): üzerinde / basılı; başlığı varsa 400 ms sonra kâğıt
    /// etiket açılır (<see cref="HoverTag"/>). Metinleri sahibi yazar (gösterilecek hâliyle); boş başlıkta etiket açılmaz.
    /// </summary>
    public sealed class UiHoverTarget : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler
    {
        [SerializeField] HoverTag hoverTag;

        public bool Over { get; private set; }
        public bool Pressed { get; private set; }
        public string Title { get; set; }
        public string Line { get; set; }
        public string HoverTitle => Title;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Over = true;
            if (hoverTag != null && !string.IsNullOrEmpty(Title)) hoverTag.Show(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Over = false;
            Pressed = false;
            if (hoverTag != null) hoverTag.Hide(this);
        }

        public void OnPointerDown(PointerEventData eventData) => Pressed = eventData.button == PointerEventData.InputButton.Left;
        public void OnPointerUp(PointerEventData eventData) => Pressed = false;

        void OnDisable()
        {
            Over = Pressed = false;
            if (hoverTag != null) hoverTag.Hide(this);
        }
    }
}
