using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Game.UI
{
    /// <summary>Üzerine gelme / ayrılma olaylarını koda iletir (araç ipucu, eylem önizlemesi).</summary>
    public sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action Entered, Exited;
        /// <summary>Doluysa üzerine gelince bu metin araç ipucunda gösterilir.</summary>
        public Func<string> Tooltip;

        public void OnPointerEnter(PointerEventData e)
        {
            Entered?.Invoke();
            if (Tooltip != null) TooltipView.Show(Tooltip());
        }

        public void OnPointerExit(PointerEventData e)
        {
            Exited?.Invoke();
            if (Tooltip != null) TooltipView.Hide();
        }

        void OnDisable() { if (Tooltip != null) TooltipView.Hide(); }

        public static HoverRelay On(Component c) => c.GetComponent<HoverRelay>() ?? c.gameObject.AddComponent<HoverRelay>();
    }
}
