using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// İpucu servisi (PROMPT §8): 400 ms gecikmeyle imlecin 8 px altında açılır, ekran kenarında ters açılır, en fazla 420 px.
    /// İki tür: kâğıt üstünde düz metin (düğüm, kesilen danışman mesajı) ve sayaç döküm fişi. Tek örnek Tooltip canvas'ında.
    /// Açıkken imleci izler; her karede bellek ayırmaz.
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

        void Awake()
        {
            Instance = this;
            DeskUi.SetActive(textRoot, false);
            DeskUi.SetActive(breakdown, false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public static void ShowText(object who, string message)
        {
            if (Instance == null || string.IsNullOrEmpty(message)) return;
            Instance.Request(who, message, null);
        }

        public static void ShowBreakdown(object who, BreakdownVM b)
        {
            if (Instance == null || b == null) return;
            Instance.Request(who, null, b);
        }

        public static void Hide(object who)
        {
            if (Instance == null || Instance.owner != who) return;
            Instance.HideNow();
        }

        void Request(object who, string message, BreakdownVM b)
        {
            HideNow();
            owner = who; pendingText = message; pendingBreakdown = b;
            pending = true;
            showAt = Time.unscaledTime + (DeskTheme.Active != null ? DeskTheme.Active.tooltipDelay : 0.4f);
        }

        void HideNow()
        {
            pending = visible = false;
            owner = null;
            DeskUi.SetActive(textRoot, false);
            DeskUi.SetActive(breakdown, false);
            active = null;
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
            }
            if (visible && active != null) Follow();
        }

        void Follow()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, mouse.position.ReadValue(), null, out var p)) return;
            var r = canvasRoot.rect;
            var size = active.sizeDelta;
            float x = p.x, y = p.y - offsetY;   // sol-üst köşe imlecin 8 px altında
            if (x + size.x > r.xMax) x = p.x - size.x;
            if (y - size.y < r.yMin) y = p.y + offsetY + size.y;
            x = Mathf.Clamp(x, r.xMin, r.xMax - size.x);
            y = Mathf.Clamp(y, r.yMin + size.y, r.yMax);
            active.anchorMin = active.anchorMax = new Vector2(0.5f, 0.5f);
            active.pivot = new Vector2(0, 1);
            var pos = new Vector2(Mathf.Round(x), Mathf.Round(y)) - r.center;
            if (active.anchoredPosition != pos) active.anchoredPosition = pos;
        }
    }
}
