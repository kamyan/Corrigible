using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// “X” basılmış kutu (acil durum fonu seçenekleri; C1 .opt/.cb): 16×16, 1,5 px mürekkep kenar, dizin kâğıdı.
    /// Seçenek etiketi Franklin 13, ek bilgi Courier 11 ink3. Grup davranışını sahibi yönetir (<see cref="Clicked"/>).
    /// </summary>
    [ExecuteAlways]
    public sealed class RadioCheckView : Selectable, UnityEngine.EventSystems.IPointerClickHandler
    {
        [SerializeField] string label = "Kapalı";
        [SerializeField] string hint;
        [SerializeField] bool isOn;
        [SerializeField] DeskVisualState preview;

        [SerializeField] Image boxFill;
        [SerializeField] Image boxFrame;
        [SerializeField] TMP_Text mark;
        [SerializeField] TMP_Text text;
        [SerializeField] TMP_Text hintText;

        public event Action<RadioCheckView> Clicked;
        public bool IsOn => isOn;

        public void Set(string labelText, string hintString, bool on)
        {
            label = labelText; hint = hintString; isOn = on; Render();
        }

        public void SetOn(bool on)
        {
            if (isOn == on) return;
            isOn = on; Render();
        }

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData e)
        {
            if (IsInteractable()) Clicked?.Invoke(this);
        }

        protected override void OnEnable() { base.OnEnable(); DeskUi.NoNavigation(this); DeskTheme.Changed += Render; DeskLoc.Instance.Changed += Render; Render(); }
        protected override void OnDisable() { base.OnDisable(); DeskTheme.Changed -= Render; DeskLoc.Instance.Changed -= Render; }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            transition = Transition.None;
            DeskUi.Deferred(this, Render);
        }
#endif

        protected override void DoStateTransition(SelectionState state, bool instant) => Render();

        void Render()
        {
            var theme = DeskTheme.Active;
            if (theme == null || text == null) return;
            bool disabled = preview == DeskVisualState.Disabled || (preview == DeskVisualState.None && !IsInteractable());
            bool hover = preview == DeskVisualState.Hover || (preview == DeskVisualState.None && currentSelectionState == SelectionState.Highlighted);

            DeskSprites.Apply(boxFill, "sh_fill_r0");
            DeskUi.Tint(boxFill, hover ? "paper.hover" : "paper.index");
            DeskSprites.Apply(boxFrame, disabled ? "sh_dash_r0_1" : "sh_frame_r0_1");   // CSS 1,5 px → referansta 1 px
            DeskUi.Tint(boxFrame, disabled ? "ink.ink3" : "ink.ink");
            DeskUi.SetActive(mark, isOn);
            if (mark.font != theme.cpBold) mark.font = theme.cpBold;
            DeskUi.Tint(mark, "ink.ink");
            DeskUi.SetText(mark, "X");

            if (text.font != theme.lfRegular) text.font = theme.lfRegular;
            DeskUi.Tint(text, disabled ? "ink.disabledText" : "ink.ink");
            var t = DeskLoc.Instance.T(label);
            DeskUi.SetText(text, t);
            bool hasHint = !string.IsNullOrEmpty(hint);
            DeskUi.SetActive(hintText, hasHint);
            if (hasHint)
            {
                if (hintText.font != theme.cpRegular) hintText.font = theme.cpRegular;
                DeskUi.Tint(hintText, "ink.ink3");
                DeskUi.SetText(hintText, hint);
            }

            // Kutu 16 + 6 + etiket + 6 + ipucu; yükseklik 24
            const float gap = 6;
            float x = 16 + gap;
            float tw = Mathf.Round(DeskUi.TextWidth(text, t));
            Row(text.rectTransform, x, tw); x += tw;
            if (hasHint)
            {
                float hw = Mathf.Round(DeskUi.TextWidth(hintText, hint));
                x += gap; Row(hintText.rectTransform, x, hw); x += hw;
            }
            var rt = (RectTransform)transform;
            if (rt.sizeDelta != new Vector2(x, 24)) rt.sizeDelta = new Vector2(x, 24);
        }

        static void Row(RectTransform c, float x, float w)
        {
            c.anchorMin = c.anchorMax = new Vector2(0, 0.5f);
            c.pivot = new Vector2(0, 0.5f);
            c.anchoredPosition = new Vector2(x, 0);
            c.sizeDelta = new Vector2(w, 24);
        }
    }
}
