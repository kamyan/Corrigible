using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Araştırma ağacında kâğıt proje fişi. Kararları VM taşır; kart sadece sunar ve kimlikli komut yollar.</summary>
    public sealed class ResearchProjectCardView : MonoBehaviour
    {
        [SerializeField] Image paper;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text unlockText;
        [SerializeField] TMP_Text costText;
        [SerializeField] TMP_Text conditionText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text days;
        [SerializeField] TMP_Text percent;
        [SerializeField] StampView doneStamp;
        [SerializeField] HatchBarView bar;
        [SerializeField] DeskButton startButton;
        [SerializeField] DeskButton pauseButton;

        ResearchProjectVM project;
        bool paused;
        const float Padding = 12f, MinimumHeight = 144f;

        public event System.Action<string, bool> Action;
        public DeskButton StartButton => startButton;
        public DeskButton PauseButton => pauseButton;
        public float Height { get; private set; } = MinimumHeight;

        void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(StartProject);
            if (pauseButton != null) pauseButton.onClick.AddListener(PauseProject);
        }

        void StartProject()
        {
            if (project != null && (project.State ?? "available") == "available" && project.CanStart && string.IsNullOrEmpty(project.DisabledReason))
                Action?.Invoke(project.Id ?? project.Name, true);
        }

        void PauseProject()
        {
            if (project != null && project.CanPause && string.IsNullOrEmpty(project.PauseDisabledReason)
                && (project.State == "running" || project.State == "stopped" || project.State == "paused"))
                Action?.Invoke(project.Id ?? project.Name, paused);
        }

        public void Bind(ResearchProjectVM p)
        {
            project = p;
            string state = p.State ?? "available";
            paused = p.ManuallyPaused ?? (state == "paused" || state == "stopped");
            bool done = state == "done", running = state == "running" || state == "paused" || state == "stopped", locked = state == "locked", available = state == "available";
            DeskUi.Tint(paper, locked ? "paper.page2" : "paper.index");
            DeskSprites.Apply(icon, done ? "ic_check_14b" : locked ? "ic_lock_14" : running ? paused || state == "stopped" ? "ic_pause_14" : "ic_play_14" : "ic_circle_14");
            DeskUi.Tint(icon, done ? "ink.green" : locked ? "ink.ink3" : "ink.ink");
            DeskUi.SetText(nameText, DeskLoc.Instance.T(p.Name));
            DeskUi.Tint(nameText, locked ? "ink.ink2" : "ink.ink");
            DeskUi.SetText(unlockText, string.IsNullOrEmpty(p.Unlock) ? DeskLoc.Instance.T(p.Sub)
                : p.Kind == "operation" || p.Kind == "budget" ? Loc.F("Etki: {0}", DeskLoc.Instance.T(p.Unlock))
                : Loc.F("Açar: {0}", DeskLoc.Instance.T(p.Unlock)));
            DeskUi.Tint(unlockText, "ink.ink2");
            string body = Body(p);
            DeskUi.SetActive(costText, !string.IsNullOrEmpty(body));
            DeskUi.SetText(costText, body);
            DeskUi.Tint(costText, "ink.ink3");
            bool blocked = !p.CanStart || !string.IsNullOrEmpty(p.DisabledReason);
            string condition = locked ? p.LockReason : available && blocked && !string.IsNullOrEmpty(p.LockReason) ? p.LockReason : p.Condition;
            if (running && !string.IsNullOrEmpty(p.PauseDisabledReason))
                condition = string.IsNullOrEmpty(condition) ? p.PauseDisabledReason : condition + "\n" + p.PauseDisabledReason;
            DeskUi.SetText(conditionText, DeskLoc.Instance.T(condition));
            DeskUi.SetActive(conditionText, !string.IsNullOrEmpty(condition));
            DeskUi.Tint(conditionText, locked || available && blocked ? "ink.orange" : "ink.ink3");

            DeskUi.SetActive(doneStamp, done);
            DeskUi.SetActive(bar, running);
            DeskUi.SetActive(percent, running);
            DeskUi.SetActive(pauseButton, running && p.CanPause);
            // Süre ve toplam fon gövdedeki maliyet satırındadır; alt sıranın tamamı tek düğmeye ayrılır.
            DeskUi.SetActive(days, false);
            DeskUi.SetActive(startButton, available);
            DeskUi.SetActive(statusText, locked || running);
            if (done && doneStamp != null) doneStamp.Set(Loc.T("ONAYLANDI"), StampInk.Green, StampSize.Small, p.StampRot, "research." + (p.Id ?? p.Name));
            if (running)
            {
                bar.Set(p.Progress);
                DeskUi.SetText(percent, DeskFormat.Percent(p.Progress * 100));
                DeskUi.SetText(statusText, !string.IsNullOrEmpty(p.StatusLabel) ? DeskLoc.Instance.T(p.StatusLabel)
                    : paused ? Loc.T("DURAKLATILDI") : Loc.T("ARAŞTIRILIYOR"));
                pauseButton.SetIcon(paused ? "ic_play_12" : "ic_pause_12");
                pauseButton.SetLabel(p.PauseLabel ?? (paused ? Loc.T("Sürdür") : Loc.T("Duraklat")));
                pauseButton.SetDisabled(p.PauseDisabledReason == null ? null : Loc.T("Bekliyor"));
            }
            if (locked) DeskUi.SetText(statusText, Loc.T("KİLİTLİ"));
            if (available)
            {
                DeskUi.SetText(days, DeskLoc.Instance.T(p.Days));
                startButton.SetLabel(p.StartLabel);
                // Uzun neden kartın gövdesindedir; düğme kısa durum etiketiyle kartın dışına taşmaz.
                startButton.SetDisabled(p.CanStart && string.IsNullOrEmpty(p.DisabledReason) ? null : Loc.N("Bekliyor"));
            }
            LayoutCard();
        }

        static string Body(ResearchProjectVM p)
        {
            var lines = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(p.Sub) && p.Sub != p.Unlock) lines.Add(DeskLoc.Instance.T(p.Sub));
            if (!string.IsNullOrEmpty(p.Funding)) lines.Add(DeskLoc.Instance.T(p.Funding));
            if (!string.IsNullOrEmpty(p.PurchasePreview)) lines.Add(DeskLoc.Instance.T(p.PurchasePreview));
            return string.Join("\n", lines);
        }

        public void LayoutCard()
        {
            if (project == null) return;
            var rt = (RectTransform)transform;
            float width = Mathf.Max(100, rt.rect.width), inner = width - Padding * 2;
            DeskUi.Place(icon.rectTransform, Padding, 13, 14, 14);
            float titleHeight = TextHeight(nameText, inner - 22, 18);
            DeskUi.Place(nameText.rectTransform, Padding + 22, Padding, inner - 22, titleHeight);
            float y = Padding + titleHeight + 7;
            float unlockHeight = TextHeight(unlockText, inner, 15);
            DeskUi.Place(unlockText.rectTransform, Padding, y, inner, unlockHeight);
            y += unlockHeight + 6;
            if (costText != null && costText.gameObject.activeSelf)
            {
                float costHeight = TextHeight(costText, inner, 14);
                DeskUi.Place(costText.rectTransform, Padding, y, inner, costHeight);
                y += costHeight + 5;
            }
            if (conditionText != null && conditionText.gameObject.activeSelf)
            {
                float conditionHeight = TextHeight(conditionText, inner, 14);
                DeskUi.Place(conditionText.rectTransform, Padding, y, inner, conditionHeight);
                y += conditionHeight + 6;
            }
            Height = Mathf.Max(MinimumHeight, y + 24 + Padding);
            if (!Mathf.Approximately(rt.sizeDelta.y, Height)) rt.sizeDelta = new Vector2(rt.sizeDelta.x, Height);
            float footer = Height - Padding - 24;
            if (days != null) DeskUi.Place(days.rectTransform, Padding, footer, 116, 24);
            if (startButton != null) PlaceButton(startButton, width - Padding, footer);
            if (pauseButton != null) PlaceButton(pauseButton, width - Padding, footer);
            if (doneStamp != null) DeskUi.Place((RectTransform)doneStamp.transform, Padding, footer + 2, ((RectTransform)doneStamp.transform).rect.width, 19);
            if (bar != null) DeskUi.Place((RectTransform)bar.transform, Padding, footer + 7, 72, 10);
            if (percent != null) DeskUi.Place(percent.rectTransform, Padding + 78, footer, 44, 24);
            if (statusText != null)
            {
                bool locked = project.State == "locked";
                float left = locked ? Padding : Padding + 126;
                float right = project.CanPause ? 36 : Padding;
                DeskUi.Place(statusText.rectTransform, left, footer, Mathf.Max(30, width - left - right), 24);
                DeskUi.Tint(statusText, locked ? "ink.ink3" : "ink.blue");
            }
        }

        static float TextHeight(TMP_Text text, float width, float min)
        {
            if (text == null) return 0;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.TopLeft;
            return Mathf.Max(min, Mathf.Ceil(text.GetPreferredValues(text.text, width, float.PositiveInfinity).y));
        }

        static void PlaceButton(DeskButton button, float right, float top)
        {
            var rt = (RectTransform)button.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(right, -top);
        }
    }
}
