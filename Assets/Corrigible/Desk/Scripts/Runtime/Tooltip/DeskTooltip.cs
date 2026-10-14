using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// İpucu servisi (PROMPT §8): 400 ms gecikmeyle imlecin 8 px altında açılır, ekran kenarında ters açılır, en fazla 420 px.
    /// İki tür: kâğıt üstünde düz metin (düğüm, kesilen danışman mesajı) ve sayaç döküm fişi. Tek örnek Tooltip canvas'ında.
    /// Düz metin imleci izler; sayaç dökümü kaynağın altında sabit durur. Her karede bellek ayırmaz.
    /// </summary>
    public sealed class DeskTooltip : MonoBehaviour
    {
        [SerializeField] RectTransform canvasRoot;
        [SerializeField] RectTransform textRoot;
        [SerializeField] TMP_Text text;
        [SerializeField] BreakdownTooltipView breakdown;
        [SerializeField] float padX = 10, padY = 7;
        [SerializeField] float offsetY = 8;
        [SerializeField] float maxWidth = 420;

        public static DeskTooltip Instance { get; private set; }

        object owner;
        float showAt;
        bool pending, visible;
        string pendingText;
        BreakdownVM pendingBreakdown;
        RectTransform active;
        RectTransform source;
        Canvas rootCanvas;
        Vector3 activeScale = Vector3.one;
        readonly Vector3[] corners = new Vector3[4];

        void Awake()
        {
            Instance = this;
            var canvas = GetComponentInParent<Canvas>();
            rootCanvas = canvas != null ? canvas.rootCanvas : null;
            DeskUi.SetActive(textRoot, false);
            DeskUi.SetActive(breakdown, false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public static void ShowText(object who, string message)
        {
            if (Instance == null || string.IsNullOrEmpty(message)) return;
            Instance.Request(who, message, null, null);
        }

        public static void ShowBreakdown(object who, BreakdownVM b, RectTransform anchor = null)
        {
            if (Instance == null || b == null) return;
            Instance.Request(who, null, b, anchor);
        }

        public static void RefreshBreakdown(object who, BreakdownVM b)
        {
            if (Instance == null || Instance.owner != who || b == null) return;
            Instance.pendingBreakdown = b;
            if (Instance.visible && Instance.breakdown != null) Instance.breakdown.Bind(b);
        }

        public static void Hide(object who)
        {
            if (Instance == null || Instance.owner != who) return;
            Instance.HideNow();
        }

        void Request(object who, string message, BreakdownVM b, RectTransform anchor)
        {
            HideNow();
            owner = who; pendingText = message; pendingBreakdown = b;
            source = anchor;
            pending = true;
            showAt = Time.unscaledTime + (DeskTheme.Active != null ? DeskTheme.Active.tooltipDelay : 0.4f);
        }

        void HideNow()
        {
            pending = visible = false;
            owner = null;
            DeskUi.SetActive(textRoot, false);
            DeskUi.SetActive(breakdown, false);
            if (active != null) active.localScale = activeScale;
            active = null;
            source = null;
        }

        void Update()
        {
            if (pending && Time.unscaledTime >= showAt)
            {
                pending = false; visible = true;
                if (pendingBreakdown != null)
                {
                    DeskUi.SetActive(breakdown, true);
                    breakdown.Bind(pendingBreakdown);
                    active = (RectTransform)breakdown.transform;
                }
                else
                {
                    DeskUi.SetActive(textRoot, true);
                    DeskUi.SetText(text, pendingText);
                    var pref = text.GetPreferredValues(pendingText, maxWidth - padX * 2, 1000);
                    float w = Mathf.Min(maxWidth, Mathf.Ceil(pref.x) + padX * 2);
                    var size = text.GetPreferredValues(pendingText, w - padX * 2, 1000);
                    textRoot.sizeDelta = new Vector2(w, Mathf.Ceil(size.y) + padY * 2);
                    active = textRoot;
                }
                activeScale = active.localScale;
            }
        }

        void LateUpdate() { if (visible && active != null) Follow(); }

        void Follow()
        {
            var mouse = Mouse.current;
            if (mouse == null && source == null) return;
            Place(mouse != null ? mouse.position.ReadValue() : Vector2.zero);
        }

        void Place(Vector2 screenPoint)
        {
            if (canvasRoot == null || active == null || !(active.parent is RectTransform parent)) return;
            var camera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, camera, out var p)) return;

            // Konum ve boyut aynı uzayda olmalı: panelin ebeveyni, canvas ile aynı pivotta/ölçekte olmayabilir.
            var bounds = LocalBounds(canvasRoot, parent);
            if (rootCanvas != null)
            {
                var pixels = rootCanvas.pixelRect;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pixels.min, camera, out var lo) &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pixels.max, camera, out var hi))
                    bounds = Rect.MinMaxRect(Mathf.Max(bounds.xMin, lo.x), Mathf.Max(bounds.yMin, lo.y),
                        Mathf.Min(bounds.xMax, hi.x), Mathf.Min(bounds.yMax, hi.y));
            }
            const float margin = 8;
            bounds = Rect.MinMaxRect(bounds.xMin + margin, bounds.yMin + margin, bounds.xMax - margin, bounds.yMax - margin);
            if (bounds.width <= 0 || bounds.height <= 0) return;

            var rawSize = Vector2.Scale(active.rect.size, new Vector2(Mathf.Abs(activeScale.x), Mathf.Abs(activeScale.y)));
            float fit = Mathf.Min(1, Mathf.Min(bounds.width / Mathf.Max(1, rawSize.x), bounds.height / Mathf.Max(1, rawSize.y)));
            active.localScale = activeScale * fit;
            var size = rawSize * fit;
            float x = p.x, y = p.y - offsetY;
            if (source != null)
            {
                var anchor = LocalBounds(source, parent);
                x = anchor.center.x - size.x / 2;
                y = anchor.yMin - offsetY;
                if (y - size.y < bounds.yMin && anchor.yMax + offsetY + size.y <= bounds.yMax)
                    y = anchor.yMax + offsetY + size.y;
            }
            else
            {
                if (x + size.x > bounds.xMax) x = p.x - size.x;
                if (y - size.y < bounds.yMin) y = p.y + offsetY + size.y;
            }
            x = Mathf.Clamp(x, bounds.xMin, bounds.xMax - size.x);
            y = Mathf.Clamp(y, bounds.yMin + size.y, bounds.yMax);
            active.pivot = new Vector2(0, 1);
            active.position = parent.TransformPoint(new Vector3(x, y, 0));
        }

        Rect LocalBounds(RectTransform rect, RectTransform parent)
        {
            rect.GetWorldCorners(corners);
            var lo = new Vector2(float.MaxValue, float.MaxValue);
            var hi = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in corners)
            {
                Vector2 point = parent.InverseTransformPoint(corner);
                lo = Vector2.Min(lo, point);
                hi = Vector2.Max(hi, point);
            }
            return Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
        }
    }
}
