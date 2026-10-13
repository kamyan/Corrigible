using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Fareyi izleyen tek araç ipucu (kriz masası canvas'ında en üstte). Metni HoverRelay.Tooltip verir.</summary>
    public sealed class TooltipView : MonoBehaviour
    {
        static TooltipView instance;

        [SerializeField] RectTransform panel;
        [SerializeField] TMP_Text text;
        [SerializeField] Vector2 offset = new Vector2(18, -18);

        void Awake() { instance = this; panel.gameObject.SetActive(false); }
        void OnDestroy() { if (instance == this) instance = null; }

        public static void Show(string content)
        {
            if (instance == null || string.IsNullOrEmpty(content)) return;
            instance.text.text = content;
            instance.panel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(instance.panel);
            instance.Follow();
        }

        public static void Hide() { if (instance != null) instance.panel.gameObject.SetActive(false); }

        void LateUpdate() { if (panel.gameObject.activeSelf) Follow(); }

        void Follow()
        {
            if (Mouse.current == null) return;
            var canvas = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, Mouse.current.position.ReadValue(), null, out var p);
            var size = panel.rect.size;
            var half = canvas.rect.size / 2;
            // Ekrandan taşmasın: sağa ya da aşağıya sığmıyorsa diğer yana aç
            float x = p.x + offset.x + size.x > half.x ? p.x - offset.x - size.x : p.x + offset.x;
            float y = p.y + offset.y - size.y < -half.y ? p.y - offset.y + size.y : p.y + offset.y;
            // Son güvence: panel her durumda ekranın içinde kalsın
            x = Mathf.Clamp(x, -half.x + 8, half.x - size.x - 8);
            y = Mathf.Clamp(y, -half.y + size.y + 8, half.y - 8);
            panel.anchoredPosition = new Vector2(x, y);
        }
    }
}
