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
        Canvas rootCanvas;
        Vector3 panelScale;
        readonly Vector3[] corners = new Vector3[4];

        void Awake()
        {
            instance = this;
            panelScale = panel.localScale;
            var canvas = GetComponentInParent<Canvas>();
            rootCanvas = canvas != null ? canvas.rootCanvas : null;
            panel.gameObject.SetActive(false);
        }
        void OnDestroy() { if (instance == this) instance = null; }

        public static void Show(string content)
        {
            if (instance == null || string.IsNullOrEmpty(content)) return;
            instance.text.text = content;
            instance.panel.localScale = instance.panelScale;
            instance.panel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(instance.panel);
            instance.Follow();
        }

        public static void Hide() { if (instance != null) instance.panel.gameObject.SetActive(false); }

        void LateUpdate() { if (panel.gameObject.activeSelf) Follow(); }

        void Follow()
        {
            if (Mouse.current == null) return;
            Place(Mouse.current.position.ReadValue());
        }

        void Place(Vector2 screenPoint)
        {
            if (!(panel.parent is RectTransform parent)) return;
            var camera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, camera, out var p)) return;

            // Görünür alanı panelin gerçek ebeveynine çevir; merkez pivot ya da doğrudan canvas çocuğu varsayma.
            ((RectTransform)transform).GetWorldCorners(corners);
            var lo = new Vector2(float.MaxValue, float.MaxValue);
            var hi = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in corners)
            {
                Vector2 point = parent.InverseTransformPoint(corner);
                lo = Vector2.Min(lo, point);
                hi = Vector2.Max(hi, point);
            }
            if (rootCanvas != null)
            {
                var pixels = rootCanvas.pixelRect;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pixels.min, camera, out var screenLo) &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pixels.max, camera, out var screenHi))
                {
                    lo = Vector2.Max(lo, screenLo);
                    hi = Vector2.Min(hi, screenHi);
                }
            }
            lo += Vector2.one * 8;
            hi -= Vector2.one * 8;
            if (hi.x <= lo.x || hi.y <= lo.y) return;

            var rawSize = Vector2.Scale(panel.rect.size, new Vector2(Mathf.Abs(panelScale.x), Mathf.Abs(panelScale.y)));
            float fit = Mathf.Min(1, Mathf.Min((hi.x - lo.x) / Mathf.Max(1, rawSize.x), (hi.y - lo.y) / Mathf.Max(1, rawSize.y)));
            panel.localScale = panelScale * fit;
            var size = rawSize * fit;
            float x = p.x + offset.x + size.x > hi.x ? p.x - offset.x - size.x : p.x + offset.x;
            float y = p.y + offset.y - size.y < lo.y ? p.y - offset.y + size.y : p.y + offset.y;
            x = Mathf.Clamp(x, lo.x, hi.x - size.x);
            y = Mathf.Clamp(y, lo.y + size.y, hi.y);
            panel.pivot = new Vector2(0, 1);
            panel.position = parent.TransformPoint(new Vector3(x, y, 0));
        }
    }
}
