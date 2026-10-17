using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Haritadaki toplanabilir saha fişi. Sabit isabet alanı üstünde kâğıt hafifçe kalkar; yeniden bağlanan havuz öğesi
    /// her zaman güncel komutu taşır. Ödenek süresi işareti, raporun güvenilirliğini ifade etmez.
    /// </summary>
    public sealed class FieldReportView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] Button collect;
        [SerializeField] RectTransform visual;
        [SerializeField] TMP_Text title, reward;
        [SerializeField] Image expiryMark;
        [SerializeField] Image focusFrame;

        FieldReportVM data;
        DeskTween lift;
        bool hovered, selected;
        float phase;

        public FieldReportVM Data => data;
        public Button CollectButton => collect;
        public event Action<string> Clicked;

        void Awake()
        {
            if (collect != null) collect.onClick.AddListener(Collect);
        }

        void OnDestroy()
        {
            if (collect != null) collect.onClick.RemoveListener(Collect);
        }

        void OnDisable()
        {
            hovered = selected = false;
            lift.Snap(0);
            if (visual != null) visual.anchoredPosition = Vector2.zero;
            DeskUi.SetActive(focusFrame, false);
            DeskTooltip.Hide(this);
        }

        public void Bind(FieldReportVM report)
        {
            bool changed = data?.Id != report?.Id;
            bool tooltipChanged = data?.Tooltip != report?.Tooltip;
            data = report;
            if (data == null) return;
            if (changed)
            {
                hovered = selected = false;
                lift.Snap(0);
                DeskTooltip.Hide(this);
                uint hash = 2166136261;
                foreach (char c in data.Id ?? "") hash = (hash ^ c) * 16777619;
                phase = hash % 6283 / 1000f;
            }
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(data.X, -data.Y);
            DeskUi.SetText(title, data.Title);
            DeskUi.SetText(reward, data.RewardText);
            DeskUi.SetActive(expiryMark, data.Expiring);
            DeskUi.SetActive(focusFrame, hovered || selected);
            if (collect != null) collect.interactable = !string.IsNullOrEmpty(data.Command);
            if (hovered && tooltipChanged) DeskTooltip.ShowText(this, data.Tooltip);
            Pose();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            hovered = true;
            SetFocus();
            DeskTooltip.ShowText(this, data?.Tooltip);
        }

        public void OnPointerExit(PointerEventData e)
        {
            hovered = false;
            SetFocus();
            DeskTooltip.Hide(this);
        }

        public void OnSelect(BaseEventData e) { selected = true; SetFocus(); }
        public void OnDeselect(BaseEventData e) { selected = false; SetFocus(); }

        void SetFocus()
        {
            bool on = hovered || selected;
            DeskUi.SetActive(focusFrame, on);
            lift.To(on ? 2 : 0, DeskTheme.Active != null ? DeskTheme.Active.hoverLift : 0.08f);
        }

        void Collect()
        {
            if (data == null || string.IsNullOrEmpty(data.Command)) return;
            DeskTooltip.Hide(this);
            // Komut eşzamanlı yenileme yapabilir; kimlik ve komut çağrı anında okunur, eski havuz öğesi yakalanmaz.
            string command = data.Command;
            if (collect != null) collect.interactable = false;
            Clicked?.Invoke(command);
        }

        void Update()
        {
            lift.Tick(Time.unscaledDeltaTime);
            Pose();
        }

        void Pose()
        {
            if (visual == null) return;
            float breath = Application.isPlaying && !hovered && !selected ? Mathf.Sin(Time.unscaledTime * 1.8f + phase) * 0.65f : 0;
            visual.anchoredPosition = new Vector2(0, lift.Value + breath);
        }
    }
}
