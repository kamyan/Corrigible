using System;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Bölge kartındaki kutu ya da tam dosyadaki eylem satırı. Karar yalnız sahip komut denetleyicisindedir.</summary>
    public sealed class SadeCardActionView : Selectable, IPointerClickHandler
    {
        [SerializeField] RectTransform visual;
        [SerializeField] SadePaper paper;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text label, cost, effect;
        [SerializeField] KeyChipView key;
        [SerializeField] bool row;

        string reason, actionId;
        bool danger, previewHover, hovered;
        SelectionState state;
        DeskTween lift;
        public string CommandId => SadeCardText.Command(actionId);
        public string ActionId => actionId;
        public event Action<string> Clicked;
        public event Action<string> PreviewRequested;

        public void Bind(JObject data)
        {
            if (data == null) return;
            actionId = (string)data["id"];
            danger = (bool?)data["danger"] ?? actionId == "killSwitch";
            reason = (string)data["disabledReason"];
            bool disabled = (bool?)data["disabled"] == true || (bool?)data["enabled"] == false;
            if (disabled && string.IsNullOrEmpty(reason)) reason = Loc.N("Şu anda kullanılamıyor");
            interactable = !disabled && string.IsNullOrEmpty(reason);
            previewHover = (bool?)data["hover"] ?? false;
            DeskUi.SetText(label, SadePanelText.T("action." + actionId));
            SadeUi.Type(label, row ? "actionRowName" : "actionName");
            if (label != null)
            {
                label.textWrappingMode = TextWrappingModes.Normal;
                if (row) SadeUi.Place(label, 60, 3, 92, 30);
            }
            SadeUi.Sprite(icon, "ic_" + ((string)data["icon"] ?? SadeCardText.Icon(actionId)) + (row ? "_s" : "_l"));
            if (key != null) key.Set((string)data["key"]);
            DeskUi.SetText(cost, SadeCardText.Cost((string)data["cost"]));
            DeskUi.SetText(effect, SadePanelText.T((string)data["effectKey"]));
            if (row)
            {
                SadeUi.Type(cost, "actionRowCost");
                SadeUi.Type(effect, "actionRowEffect");
                LayoutRow();
            }
            Render(true);
        }

        public void SetPreview(bool on) { previewHover = on; Render(false); }

        protected override void OnEnable() { base.OnEnable(); transition = Transition.None; Render(true); }
        protected override void OnDisable() { base.OnDisable(); hovered = false; DeskTooltip.Hide(this); }
        protected override void DoStateTransition(SelectionState next, bool instant) { state = next; Render(instant); }

        public override void OnPointerEnter(PointerEventData e)
        {
            hovered = true;
            base.OnPointerEnter(e);
            PreviewRequested?.Invoke(CommandId);
            if (!IsInteractable()) DeskTooltip.ShowText(this, SadePanelText.T(reason));
        }

        public override void OnPointerExit(PointerEventData e)
        {
            hovered = false; previewHover = false;
            base.OnPointerExit(e);
            DeskTooltip.Hide(this);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && IsInteractable()) Clicked?.Invoke(CommandId);
        }

        void Update() { if (lift.Tick(Time.unscaledDeltaTime) && visual != null) visual.anchoredPosition = new Vector2(0, lift.Value); }

        void LayoutRow()
        {
            float width = ((RectTransform)transform).rect.width;
            float cw = cost != null ? Mathf.Min(SadeUi.Width(cost, cost.text), Mathf.Max(0, width - 248)) : 0;
            SadeUi.Place(cost, 158, 8, cw, 18);
            SadeUi.Place(effect, 164 + cw, 3, Mathf.Max(20, width - 174 - cw), 28);
            if (effect != null) { effect.textWrappingMode = TextWrappingModes.Normal; effect.alignment = TextAlignmentOptions.MidlineRight; }
        }

        void Render(bool instant)
        {
            if (paper == null || visual == null) return;
            bool disabled = !IsInteractable();
            bool press = !disabled && state == SelectionState.Pressed;
            bool hover = !disabled && (hovered || previewHover || state == SelectionState.Highlighted);
            string ink = disabled ? "ink.disabledText" : danger ? "sade.status.threat" : "sade.ink";
            paper.Set(disabled ? "button.paperDisabled" : hover ? "sade.panels.hoverPaper" : "sade.paper",
                disabled ? "button.paperDisabledEdge" : hover ? "sade.ink" : danger ? "sade.desk.regionCard.actions.danger.border" : "sade.edge",
                disabled || press ? null : hover ? "actionHover" : "action");
            paper.SetSprites("sh_fill_r3", disabled ? "sh_dash_r3_1" : hover ? "sh_frame_r2_2" : "sh_frame_r3_1");
            DeskUi.Tint(label, ink); DeskUi.Tint(icon, ink);
            DeskUi.Tint(cost, danger ? "sade.panels.aiMeta" : "sade.ink2");
            DeskUi.Tint(effect, danger ? "sade.panels.aiMeta" : "sade.ink3");
            float target = press ? -1 : hover ? 2 : 0;
            if (instant || !Application.isPlaying) lift.Snap(target);
            else lift.To(target, DeskTheme.Active != null ? DeskTheme.Active.hoverLift : .08f);
            visual.anchoredPosition = new Vector2(0, lift.Value);
        }
    }
}
