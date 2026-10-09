using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Fare üzerindeyken hedef grafiğin (çerçeve ya da yazı) rengini değiştirir; ayrılınca eski rengine döner.</summary>
    public sealed class UiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Graphic target;
        [SerializeField] Color hoverColor = new Color(92 / 255f, 200 / 255f, 217 / 255f);

        Color normal;
        bool over;

        public void Setup(Graphic g, Color c) { target = g; hoverColor = c; }

        public void OnPointerEnter(PointerEventData e)
        {
            var s = GetComponent<Selectable>();
            if (target == null || over || (s != null && !s.interactable)) return;
            over = true;
            normal = target.color;
            target.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData e) => Restore();
        void OnDisable() => Restore();

        void Restore()
        {
            if (!over || target == null) return;
            over = false;
            // Bu arada kod rengi değiştirdiyse (ör. seçili kart) onu ezme
            if (target.color == hoverColor) target.color = normal;
        }
    }
}
