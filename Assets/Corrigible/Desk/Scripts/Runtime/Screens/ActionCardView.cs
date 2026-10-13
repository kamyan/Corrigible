using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bölge eylem kartı (C5 eylem kartı, §7.1): dizin kâğıdı; üstüne gelince 2 px kalkar, mürekkep kenar ve büyük gölge
    /// (dosyadaki ÖNİZLEME notu güncellenir); basınca iner; devre dışıyken kesik kenar ve neden metni. Tehlikeli eylemde üstte
    /// 3 px kırmızı şerit, kırmızı başlık ve bedel.
    /// </summary>
    public sealed class ActionCardView : Selectable, IPointerClickHandler
    {
        [SerializeField] RectTransform visual;
        [SerializeField] Image fill;
        [SerializeField] Image frame;
        [SerializeField] Image dangerBar;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] KeyChipView key;
        [SerializeField] TMP_Text desc;
        [SerializeField] TMP_Text cost;
        [SerializeField] RectTransform content;
        [SerializeField] DeskVisualState preview;
        [SerializeField] bool danger;

        string reason;
        bool dim;
        CanvasGroup group;
        DeskVisualState current = DeskVisualState.Normal;
        DeskTween lift;

        /// <summary>Herhangi bir karta fareyle gelinince VM'deki görsel durum (durağan referans) bırakılır.</summary>
        static bool live;

        public event System.Action<ActionCardView> Clicked;
        public event System.Action<ActionCardView, bool> Hovered;
        public int Index { get; set; }

        public void Bind(ActionVM a)
        {
            danger = a.Danger;
            reason = a.DisabledReason;
            interactable = string.IsNullOrEmpty(reason);
            // C2: önerilmeyen eylem sönük (opaklık .55), hâlâ tıklanabilir
            dim = a.State == "dim";
            if (dim && group == null && !TryGetComponent(out group)) group = DeskUi.AddTransient<CanvasGroup>(gameObject);
            if (group != null) group.alpha = dim ? 0.55f : 1;
            preview = live ? DeskVisualState.None : a.State switch
            {
                "hover" => DeskVisualState.Hover, "pressed" => DeskVisualState.Pressed, "disabled" => DeskVisualState.Disabled, _ => DeskVisualState.None,
            };
            DeskSprites.Apply(icon, a.Icon switch { "antenna" => "ic_antenna_16", "lens" => "ic_lens_16", "target" => "ic_target_16", "power" => "ic_power_16", _ => "ic_circle_14" });
            DeskUi.SetText(title, DeskLoc.Instance.T(a.Title));
            key.Set(a.Key);
            DeskUi.SetText(desc, DeskLoc.Instance.T(string.IsNullOrEmpty(reason) ? a.Desc : reason));
            DeskUi.SetText(cost, DeskLoc.Instance.T(a.Cost));
            Render();
        }

        public void SetPreview(DeskVisualState s) { preview = s; Render(); }

        public void OnPointerClick(PointerEventData e)
        {
            if (IsInteractable()) Clicked?.Invoke(this);
        }

        public override void OnPointerEnter(PointerEventData e)
        {
            if (!live) { live = true; foreach (var c in transform.parent.GetComponentsInChildren<ActionCardView>()) c.SetPreview(DeskVisualState.None); }
            base.OnPointerEnter(e);
            Hovered?.Invoke(this, true);
        }
        public override void OnPointerExit(PointerEventData e) { base.OnPointerExit(e); Hovered?.Invoke(this, false); }

        protected override void OnEnable() { base.OnEnable(); DeskUi.NoNavigation(this); DeskTheme.Changed += Render; lift.Snap(lift.Target); Render(); }

        void Update() { if (lift.Tick(Time.unscaledDeltaTime)) DeskUi.SetY(visual, lift.Value); }
        protected override void OnDisable() { base.OnDisable(); DeskTheme.Changed -= Render; }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            transition = Transition.None;
            DeskUi.Deferred(this, Render);
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
            Render();
        }

        void Render()
        {
            var theme = DeskTheme.Active;
            if (theme == null || fill == null) return;
            var s = preview != DeskVisualState.None ? preview : (!IsInteractable() ? DeskVisualState.Disabled : current);
            bool disabled = s == DeskVisualState.Disabled, hover = s == DeskVisualState.Hover, pressed = s == DeskVisualState.Pressed;

            DeskSprites.Apply(fill, "sh_fill_r2");
            DeskUi.Tint(fill, disabled ? "action.disabled" : hover ? "paper.hover" : pressed ? "action.pressed" : "paper.index");
            DeskSprites.Apply(frame, disabled ? "sh_dash_r2_1" : "sh_frame_r2_1");
            DeskUi.Tint(frame, disabled ? "button.paperDisabledEdge" : hover || pressed ? "ink.ink" : "paper.actionCardEdge");
            var sh = fill.GetComponent<Shadow>();
            if (sh != null)
            {
                sh.enabled = !disabled && !pressed;
                var c = new Color(0.235f, 0.176f, 0.078f, hover ? 0.45f : 0.35f);
                sh.effectColor = c;
                sh.effectDistance = hover ? new Vector2(3, -5) : new Vector2(2, -2);
            }
            DeskUi.SetActive(dangerBar, danger && !disabled);
            if (DeskTheme.Illusion) DeskUi.SetActive(dangerBar, false);

            string inkTitle = danger ? "ink.red" : disabled ? "ink.disabledText" : "ink.ink";
            DeskUi.Tint(title, inkTitle);
            DeskUi.Tint(icon, inkTitle);
            DeskUi.Tint(cost, danger ? "ink.red" : disabled ? "ink.disabledText" : "ink.ink2");
            DeskUi.Tint(desc, "ink.ink2");

            // Tehlikeli kartta 3 px üst şerit içeriği 2 px aşağı iter
            if (content != null) content.anchoredPosition = new Vector2(content.anchoredPosition.x, danger ? -2 : 0);
            // Üstüne gelince 2 px kalkar (~80 ms), basınca iner (60 ms); sabit önizleme durumu animasyonsuz
            float y = hover ? 2 : pressed ? -1 : 0;
            if (preview != DeskVisualState.None || !Application.isPlaying) lift.Snap(y);
            else lift.To(y, y < lift.Value ? theme.press : theme.hoverLift);
            DeskUi.SetY(visual, lift.Value);
        }

        /// <summary>Kaynak dizileri çeviri tablosuna toplatmak için.</summary>
        internal static readonly string[] Words = { Loc.N("ONAYLANDI"), Loc.N("TAMAM") };
    }
}
