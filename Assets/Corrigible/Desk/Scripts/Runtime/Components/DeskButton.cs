using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    public enum DeskButtonVariant { Paper, PaperSmall, Ink, Link, Icon }

    /// <summary>Galeri ve testler için durumu sabitlemek (oyunda None).</summary>
    public enum DeskVisualState { None, Normal, Hover, Pressed, Disabled }

    /// <summary>
    /// Masa düğmesi (PROMPT §7.5 tablosu): kâğıt / mürekkep / bağlantı / ikon (kapat, küçült).
    /// Dört durum aynı mantıkla: üstüne gelince kâğıt kalkar, basınca iner, devre dışıyken silikleşir ve
    /// <b>nedenini yazar</b> (kilit ikonu + neden metni). Görsel çocuk kayar, isabet alanı sabit kalır.
    /// Genişlik içeriğe göre hesaplanır (LayoutGroup yok).
    /// </summary>
    [ExecuteAlways]
    public sealed class DeskButton : Button
    {
        [SerializeField] DeskButtonVariant variant;
        [SerializeField] string label = "Düğme";
        [Tooltip("Devre dışıyken etiketin yerine yazılan neden (ör. “Fon yetersiz”).")]
        [SerializeField] string disabledReason;
        [Tooltip("Klavye kısayolu (ör. Enter, B); boşsa fiş gizli.")]
        [SerializeField] string shortcut;
        [SerializeField] bool autoWidth = true;
        [Tooltip("Bağlantı normalde mavi (ör. “Araştırma ›”, “Avrasya dosyasını aç”).")]
        [SerializeField] bool accentLink;
        [Tooltip("0 = varyantın varsayılanı (13; küçükte 12).")]
        [SerializeField] float fontSizeOverride;
        [Tooltip("0 = varyantın varsayılanı (30 / 24 / 22).")]
        [SerializeField] float heightOverride;
        [Tooltip("Kâğıt düğmede yalnızca ikon (ör. araştırma satırındaki 24 px ⏸).")]
        [SerializeField] bool iconOnly;
        [Tooltip("Küçük boy (CSS .bt.sm: 24 px, iç boşluk 8, 12 px) her varyantta; ör. danışmanın “4× hızlandır” mürekkep düğmesi.")]
        [SerializeField] bool smallSize;
        [Tooltip("0 = varyantın varsayılanı (12 / 8 / 6); C4 alt çubuğu 16.")]
        [SerializeField] float padXOverride;
        [Tooltip("Boşsa varyantın yazı rengi; C4 alt çubuğundaki açık bağlantı “modal.footerText”.")]
        [SerializeField] string textKeyOverride;
        [SerializeField] DeskVisualState preview;

        [SerializeField] RectTransform visual;
        [SerializeField] Image fill;
        [SerializeField] Image frame;
        [SerializeField] Image lockIcon;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text text;
        [SerializeField] Image underline;
        [SerializeField] KeyChipView keyChip;
        [SerializeField] Image focusRing;

        DeskVisualState current = DeskVisualState.Normal;
        DeskTween lift;

        public DeskButtonVariant Variant => variant;

        /// <summary>Varyantı çalışırken değiştirir (VM'deki “link”, “ink-small” gibi türlerden).</summary>
        public void SetKind(DeskButtonVariant v, bool small)
        {
            if (variant == v && smallSize == small) return;
            variant = v; smallSize = small;
            Refresh();
        }

        /// <summary>VM düğme türü: link · ink · ink-small · paper · paper-small.</summary>
        public void SetKind(string kind)
        {
            switch (kind)
            {
                case "ink": SetKind(DeskButtonVariant.Ink, false); break;
                case "ink-small": SetKind(DeskButtonVariant.Ink, true); break;
                case "paper": SetKind(DeskButtonVariant.Paper, false); break;
                case "paper-small": SetKind(DeskButtonVariant.PaperSmall, false); break;
                case "link": SetKind(DeskButtonVariant.Link, false); break;
            }
        }

        public void SetLabel(string s)
        {
            if (label == s) return;
            label = s; Refresh();
        }

        public void SetShortcut(string key)
        {
            if (shortcut == key) return;
            shortcut = key; Refresh();
        }

        /// <summary>Devre dışı bırakır ve nedeni yazar; null neden = etkin.</summary>
        public void SetDisabled(string reason)
        {
            disabledReason = reason;
            interactable = string.IsNullOrEmpty(reason);
            Refresh();
        }

        public void SetPreview(DeskVisualState s) { preview = s; Refresh(); }

        /// <summary>İkonlu düğmenin ikon sprite'ı (ör. ⏸ / ▶).</summary>
        public void SetIcon(string spriteName)
        {
            if (icon != null) DeskSprites.Apply(icon, spriteName);
        }

        public void ShowFocus(bool on) => DeskUi.SetActive(focusRing, on);

        protected override void OnEnable()
        {
            base.OnEnable();
            DeskUi.NoNavigation(this);
            DeskTheme.Changed += Refresh;
            DeskLoc.Instance.Changed += Refresh;
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DeskTheme.Changed -= Refresh;
            DeskLoc.Instance.Changed -= Refresh;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            transition = Transition.None;
            DeskUi.Deferred(this, Refresh);
        }
#endif

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            current = state switch
            {
                SelectionState.Highlighted => DeskVisualState.Hover,
                SelectionState.Pressed => DeskVisualState.Pressed,
                SelectionState.Disabled => DeskVisualState.Disabled,
                _ => DeskVisualState.Normal,
            };
            Render(instant || !Application.isPlaying);
        }

        void Refresh() => Render(true);

        float laidWidth = -1;
        bool rendering;

        /// <summary>
        /// Sabit genişlikli düğme dışarıdan boyutlanınca (seçenek kartı, belge düğmesi) etiket yeni genişliğe göre yeniden
        /// ortalanır; etiket ve tür değişmediğinde SetLabel/SetKind yeniden çizmediği için gerekli.
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (rendering || autoWidth || !isActiveAndEnabled) return;
            if (Mathf.Approximately(((RectTransform)transform).rect.width, laidWidth)) return;
            Render(true);
        }

        DeskVisualState Effective => preview != DeskVisualState.None ? preview : (!IsInteractable() ? DeskVisualState.Disabled : current);

        void Render(bool instant)
        {
            if (rendering) return;
            rendering = true;
            try { RenderCore(instant); }
            finally { rendering = false; }
        }

        void RenderCore(bool instant)
        {
            var theme = DeskTheme.Active;
            if (theme == null || visual == null || text == null) return;
            var s = Effective;
            bool disabled = s == DeskVisualState.Disabled;
            bool small = variant == DeskButtonVariant.PaperSmall || smallSize;

            // ---- yüzey
            string fillKey = null, frameKey = null, textKey, frameSprite = "sh_frame_r3_1", fillSprite = "sh_fill_r3";
            Vector2 shadowOff = Vector2.zero; string shadowKey = null;
            float y = 0;
            switch (variant)
            {
                case DeskButtonVariant.Ink:
                    fillKey = s switch { DeskVisualState.Hover => "button.inkHover", DeskVisualState.Pressed => "black", DeskVisualState.Disabled => "button.inkDisabled", _ => "ink.ink" };
                    frameKey = disabled ? "button.inkDisabledEdge" : "black";
                    textKey = disabled ? "button.inkDisabledText" : "ink.creamOnInk";
                    if (s == DeskVisualState.Normal) { shadowOff = new Vector2(0, 2); shadowKey = "black"; }
                    if (s == DeskVisualState.Hover) { shadowOff = new Vector2(0, 3); shadowKey = "black"; }
                    if (s == DeskVisualState.Pressed) y = -2;
                    break;
                case DeskButtonVariant.Link:
                    textKey = s switch { DeskVisualState.Hover => accentLink ? "ink.blueDark" : "ink.blue", DeskVisualState.Pressed => "ink.blueDark", DeskVisualState.Disabled => "button.linkDisabled", _ => accentLink ? "ink.blue" : "ink.ink" };
                    if (s == DeskVisualState.Pressed) y = -1;
                    break;
                case DeskButtonVariant.Icon:
                    fillSprite = "sh_fill_r2"; frameSprite = disabled ? "sh_dash_r2_1" : "sh_frame_r2_1";
                    fillKey = s switch { DeskVisualState.Hover => "paper.hover", DeskVisualState.Pressed => "paper.page2", _ => "paper.page" };
                    frameKey = s == DeskVisualState.Hover || s == DeskVisualState.Pressed ? "ink.ink" : "ink.ink3";
                    textKey = s switch { DeskVisualState.Hover => "ink.ink", DeskVisualState.Disabled => "close.disabledIcon", _ => "ink.ink2" };
                    if (s == DeskVisualState.Pressed) y = -1;
                    break;
                default:
                    frameSprite = disabled ? "sh_dash_r3_1" : "sh_frame_r3_1";
                    fillKey = s switch { DeskVisualState.Hover => "paper.hover", DeskVisualState.Pressed => "paper.page2", DeskVisualState.Disabled => "button.paperDisabled", _ => "paper.page" };
                    frameKey = s switch { DeskVisualState.Hover => "ink.ink", DeskVisualState.Disabled => "button.paperDisabledEdge", _ => "ink.ink2" };
                    textKey = disabled ? "ink.disabledText" : "ink.ink";
                    if (s == DeskVisualState.Normal) { shadowOff = new Vector2(0, 2); shadowKey = "ink.ink2"; }
                    if (s == DeskVisualState.Hover) { shadowOff = new Vector2(0, 3); shadowKey = "ink.ink"; }
                    if (s == DeskVisualState.Pressed) y = -2;
                    break;
            }

            bool surface = variant != DeskButtonVariant.Link;
            DeskUi.SetActive(fill, surface);
            DeskUi.SetActive(frame, surface);
            if (surface)
            {
                DeskSprites.Apply(fill, fillSprite);
                DeskUi.Tint(fill, fillKey);
                DeskSprites.Apply(frame, frameSprite);
                DeskUi.Tint(frame, frameKey);
                var sh = fill.GetComponent<Shadow>();
                if (sh != null)
                {
                    sh.enabled = shadowKey != null;
                    if (shadowKey != null) { sh.effectDistance = new Vector2(shadowOff.x, -shadowOff.y); sh.effectColor = theme.Get(shadowKey); }
                }
            }

            // ---- içerik
            var rt = (RectTransform)transform;
            float h = heightOverride > 0 ? heightOverride : small ? 24 : variant switch { DeskButtonVariant.Link => 24, DeskButtonVariant.Icon => 22, _ => 30 };
            float padX = padXOverride > 0 ? padXOverride : variant switch { DeskButtonVariant.Link => 6, DeskButtonVariant.Icon => 0, _ => small ? 8 : 12 };
            if (!string.IsNullOrEmpty(textKeyOverride) && !disabled) textKey = textKeyOverride;
            float fontSize = fontSizeOverride > 0 ? fontSizeOverride : small ? 12 : 13;

            bool isIcon = variant == DeskButtonVariant.Icon || iconOnly;
            bool showLock = disabled && !isIcon && variant != DeskButtonVariant.Link;
            string shown = disabled && !string.IsNullOrEmpty(disabledReason) ? disabledReason : label;
            shown = isIcon ? null : DeskLoc.Instance.T(shown);

            DeskUi.SetActive(text, !isIcon);
            DeskUi.SetActive(lockIcon, showLock);
            DeskUi.SetActive(icon, isIcon);
            DeskUi.SetActive(underline, variant == DeskButtonVariant.Link);
            bool showKey = keyChip != null && !string.IsNullOrEmpty(shortcut) && !isIcon && !disabled;
            DeskUi.SetActive(keyChip, showKey);

            float textW = 0;
            if (!isIcon)
            {
                if (text.font != theme.lfSemiBold) text.font = theme.lfSemiBold;
                if (!Mathf.Approximately(text.fontSize, fontSize)) text.fontSize = fontSize;
                DeskUi.Tint(text, textKey);
                DeskUi.SetText(text, shown);
                textW = Mathf.Round(DeskUi.TextWidth(text, shown));
            }
            if (isIcon) DeskUi.Tint(icon, textKey);
            if (showLock) DeskUi.Tint(lockIcon, textKey);

            const float gap = 6;
            float keyW = showKey ? keyChip.Measure(shortcut) : 0;
            float content = textW + (showLock ? 13 + gap : 0) + (showKey ? gap + keyW : 0);
            // CSS: genişlik = içerik + iç boşluk + 1+1 px kenarlık (bağlantıda kenarlık yok)
            float border = variant == DeskButtonVariant.Link ? 0 : 2;
            float w = isIcon ? h : autoWidth ? content + padX * 2 + border : rt.sizeDelta.x;
            if (rt.sizeDelta != new Vector2(w, h)) rt.sizeDelta = new Vector2(w, h);
            laidWidth = rt.rect.width;

            float x = (w - content) / 2;
            if (showLock) { Row(lockIcon.rectTransform, x, 13, 13); x += 13 + gap; }
            if (!isIcon) { Row(text.rectTransform, x, textW, h); }
            if (variant == DeskButtonVariant.Link)
            {
                // Alt çizgi: metnin 3 px altında 1 px (üstüne gelince 2 px); devre dışıyken kesik
                bool thick = s == DeskVisualState.Hover || s == DeskVisualState.Pressed;
                DeskSprites.Apply(underline, disabled ? "ln_dash_h" : "sh_fill_r0");
                DeskUi.Tint(underline, textKey);
                var u = underline.rectTransform;
                u.anchorMin = u.anchorMax = new Vector2(0, 0.5f); u.pivot = new Vector2(0, 1);
                u.anchoredPosition = new Vector2(x, -(fontSize * 0.5f) - 0.5f);
                u.sizeDelta = new Vector2(textW, thick ? 2 : 1);
            }
            x += textW;
            if (showKey) { x += gap; Row((RectTransform)keyChip.transform, x, keyW, 18); }

            // ---- kalkma / inme
            if (instant) lift.Snap(y);
            else lift.To(y, y < lift.Value ? theme.press : theme.hoverLift);
            DeskUi.SetY(visual, lift.Value);
        }

        static void Row(RectTransform c, float x, float w, float h)
        {
            c.anchorMin = c.anchorMax = new Vector2(0, 0.5f);
            c.pivot = new Vector2(0, 0.5f);
            c.anchoredPosition = new Vector2(x, 0);
            c.sizeDelta = new Vector2(w, h);
        }

        void Update()
        {
            if (lift.Tick(Time.unscaledDeltaTime)) DeskUi.SetY(visual, lift.Value);
        }
    }
}
