using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Ray hız tuşu (30 px yuvarlak; C5): mürekkep zemin, 2 px kenar. Üstüne gelince pirinç kenar + %35 hale;
    /// seçiliyken basılı kalır (krem zemin, içe gölge, y +2); olay ve modal sırasında devre dışı.
    /// </summary>
    [ExecuteAlways]
    public sealed class SpeedKeyView : Button
    {
        [SerializeField] string label = "1×";
        [Tooltip("Etiket yerine ikon (ör. duraklat).")]
        [SerializeField] bool useIcon;
        [SerializeField] bool isOn;
        [SerializeField] DeskVisualState preview;

        [SerializeField] RectTransform visual;
        [SerializeField] Image glow;
        [SerializeField] Image fill;
        [SerializeField] Image ring;
        [SerializeField] Image inset;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text text;

        DeskVisualState current = DeskVisualState.Normal;

        public bool IsOn => isOn;

        public void SetOn(bool on)
        {
            if (isOn == on) return;
            isOn = on; Render();
        }

        public void SetPreview(DeskVisualState s) { preview = s; Render(); }

        protected override void OnEnable() { base.OnEnable(); DeskUi.NoNavigation(this); DeskTheme.Changed += Render; Render(); }
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
            bool disabled = s == DeskVisualState.Disabled;
            bool on = !disabled && (isOn || s == DeskVisualState.Pressed);
            bool hover = !disabled && !on && s == DeskVisualState.Hover;

            DeskSprites.Apply(fill, "sh_circle_32");
            DeskSprites.Apply(ring, "sh_ring_o30_2");
            DeskUi.Tint(fill, disabled ? "speed.disabled" : on ? "paper.page" : "ink.ink");
            DeskUi.Tint(ring, disabled ? "speed.disabledEdge" : on || hover ? "desk.brass" : "speed.edge");
            var sh = fill.GetComponent<Shadow>();
            if (sh != null) { sh.enabled = !disabled && !on; sh.effectDistance = new Vector2(0, -2); sh.effectColor = theme.Get("black"); }

            DeskUi.SetActive(glow, hover);
            if (hover) { DeskSprites.Apply(glow, "sh_circle_32"); DeskUi.Tint(glow, "desk.brass", 0.35f); }
            DeskUi.SetActive(inset, on);
            if (on) { DeskSprites.Apply(inset, "sh_circle30_inset_top"); DeskUi.Tint(inset, "black", 0.28f); }

            string ink = disabled ? "speed.disabledText" : on ? "ink.ink" : "paper.page";
            DeskUi.SetActive(icon, useIcon);
            DeskUi.SetActive(text, !useIcon);
            if (useIcon) DeskUi.Tint(icon, ink);
            else
            {
                if (text.font != theme.lfBold) text.font = theme.lfBold;
                if (!Mathf.Approximately(text.fontSize, 12)) text.fontSize = 12;
                DeskUi.Tint(text, ink);
                DeskUi.SetText(text, label);
            }
            DeskUi.SetY(visual, on ? -2 : 0);
        }
    }
}
