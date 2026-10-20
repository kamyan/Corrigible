using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Bir fiziksel cihazın dünya tuvalindeki düğmesini masanın mevcut komutuna bağlar.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class CommandPostControl : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] CommandPostDesk desk;
        [SerializeField] CommandPostAction action;
        [SerializeField] string label;
        Button button;
        void Awake() { button = GetComponent<Button>(); button.onClick.AddListener(Activate); }
        void OnDestroy() { if (button != null) button.onClick.RemoveListener(Activate); }
        void Activate() { if (desk != null) desk.Execute(action); }
        public void OnPointerEnter(PointerEventData eventData) { if (desk != null && !desk.InteractionBlocked) desk.SetHover(label); }
        public void OnPointerExit(PointerEventData eventData) { if (desk != null) desk.SetHover(null); }
        void OnDisable() { if (desk != null) desk.SetHover(null); }
    }
}
