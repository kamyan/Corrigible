using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Alt sekme (C5 dosya sekmesi): kendi kâğıt rengi, üst köşeler 7, alta yaslı. Yükseklik normal 36, üstüne gelince 40,
    /// açık pencerede 44 (kenar mürekkep). Kilitliyken gri, kesik kenar, kilit. Rozet: sarı sayı ya da kırmızı nokta.
    /// </summary>
    public sealed class DockTabView : Selectable, IPointerClickHandler
    {
        [SerializeField] RectTransform visual;
        [SerializeField] Image fill;
        [SerializeField] Image frame;
        [SerializeField] Image highlight;
        [SerializeField] Image lockIcon;
        [SerializeField] DeskRow row;
        [SerializeField] TMP_Text label;
        [SerializeField] KeyChipView key;
        [SerializeField] CountBadgeView badge;
        [SerializeField] DeskVisualState preview;

        Color paper = Color.white;
        bool open;
        DeskVisualState current = DeskVisualState.Normal;
        DeskTween height;

        public string TabId { get; private set; }
        public event Action<DockTabView> Clicked;

        public void OnPointerClick(PointerEventData e) { if (IsInteractable()) Clicked?.Invoke(this); }

        /// <summary>Pencere açık mı (pencere yöneticisinden; VM'deki ilk durumu geçersiz kılar).</summary>
        public void SetOpen(bool on)
        {
            if (open == on) return;
            open = on; Render();
        }
        public float Width => ((RectTransform)transform).sizeDelta.x;

        public void Bind(DockTabVM t)
        {
            TabId = t.Id;
            open = t.Open;
            interactable = !t.Disabled;
            ColorUtility.TryParseHtmlString(t.Color, out paper);
            DeskUi.SetText(label, DeskLoc.Instance.T(t.Label));
            key.Set(t.Key);
            bool hasBadge = t.Badge != null && !DeskTheme.Illusion;
            DeskUi.SetActive(badge, hasBadge);
            if (hasBadge)
            {
                if (t.Badge.Type == "dot") badge.SetDot(); else badge.SetCount(t.Badge.Value);
            }
            row.Layout();
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(row.Width, 44);
            Render();
        }

        protected override void OnEnable() { base.OnEnable(); DeskUi.NoNavigation(this); DeskTheme.Changed += Render; height.Snap(height.Target); Render(); }

        void Update() { if (height.Tick(Time.unscaledDeltaTime)) ApplyHeight(); }

        void ApplyHeight()
        {
            float h = height.Value;
            visual.sizeDelta = new Vector2(0, h);
            var r = (RectTransform)row.transform;
            float padBottom = Mathf.Lerp(6, 12, (h - 36) / 8f);
            r.anchoredPosition = new Vector2(0, padBottom); r.sizeDelta = new Vector2(0, h - padBottom);
        }
        protected override void OnDisable() { base.OnDisable(); DeskTheme.Changed -= Render; }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            current = state switch
            {
                SelectionState.Highlighted => DeskVisualState.Hover,
                SelectionState.Pressed => DeskVisualState.Pressed,
                SelectionState.Disabled => DeskVisualState.Disabled,
                _ => DeskVisualState.Normal,
            };
            Render();
        }

        void Render()
        {
            if (fill == null || DeskTheme.Active == null) return;
            var s = preview != DeskVisualState.None ? preview : (!IsInteractable() ? DeskVisualState.Disabled : current);
            bool disabled = s == DeskVisualState.Disabled;
            float h = disabled ? 36 : open || s == DeskVisualState.Pressed ? 44 : s == DeskVisualState.Hover ? 40 : 36;

            // Görsel kâğıt alta yaslı; yüksekliği duruma göre değişir (36 → 40 → 44, ~80 ms), isabet alanı 44 kalır
            visual.anchorMin = new Vector2(0, 0); visual.anchorMax = new Vector2(1, 0); visual.pivot = new Vector2(0.5f, 0);
            visual.anchoredPosition = Vector2.zero;
            var r = (RectTransform)row.transform;
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0.5f, 0);
            if (preview != DeskVisualState.None || !Application.isPlaying || height.Target <= 0) height.Snap(h);
            else height.To(h, h > height.Value && s == DeskVisualState.Pressed ? DeskTheme.Active.press : DeskTheme.Active.hoverLift);
            ApplyHeight();

            DeskSprites.Apply(fill, "sh_fill_r7_top");
            DeskUi.SetColor(fill, disabled ? DeskTheme.Active.Get("dockTabs.disabled") : paper);
            DeskSprites.Apply(frame, disabled ? "sh_dash_r7_top_1" : "sh_frame_r7_top_1");
            DeskUi.Tint(frame, "black", open ? 1 : 0.5f);
            DeskUi.SetActive(highlight, !disabled);
            DeskUi.SetActive(lockIcon, disabled);
            DeskUi.Tint(label, disabled ? "dockTabs.disabledText" : "ink.ink");
        }
    }
}
