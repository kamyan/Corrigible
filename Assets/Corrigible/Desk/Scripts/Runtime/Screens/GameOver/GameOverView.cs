using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Oyun sonu (PROMPT §7.4, C4): ahşap + keçe zemin üstünde son rapor (başlık satırı, SONUÇ bloğu, büyük KAYBEDİLDİ
    /// damgası, KPI'lar, şerit grafik), kırılma anları (plaka, 7 kart, raptiyeler arası iplik) ve alt çubuk. Tam ekrandır;
    /// açıkken masanın canvas'ları kapatılır (fazladan çizim olmasın). Enter yeniden başlatır, L günlükleri açar.
    /// </summary>
    public sealed class GameOverView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("Açıkken kapatılan masa canvas'ları (BG, harita, HUD, pencereler, teleks, alt sıra, iplik).")]
        [SerializeField] Canvas[] hideWhileOpen = Array.Empty<Canvas>();

        [Header("Rapor")]
        [SerializeField] TMP_Text headerTitle, headerMeta, headerRight;
        [SerializeField] DeskRow headerRow;
        [SerializeField] TMP_Text resultCap, title, who, lead;
        [SerializeField] StampView bigStamp;
        [Tooltip("Uzun başlıkta (EN) damga ile başlık sonu arasındaki en az boşluk (TR tasarımında ≈25).")]
        [SerializeField] float bigStampGap = 24;
        float bigStampLeft = float.NaN;
        [SerializeField] KpiCardView[] kpis = Array.Empty<KpiCardView>();
        [SerializeField] StripChartView chart;

        [Header("Kırılma anları")]
        [SerializeField] TMP_Text timelineLabel, timelineSub;
        [SerializeField] DeskRow timelineRow;
        [SerializeField] RectTransform timelinePlaque;
        [SerializeField] TimelineCardView[] cards = Array.Empty<TimelineCardView>();

        [Header("Alt çubuk")]
        [SerializeField] TMP_Text endbarInfo;
        [SerializeField] DeskButton[] actions = Array.Empty<DeskButton>();
        [SerializeField] DeskRow actionsRow;

        GameOverVM vm;

        public static GameOverView Instance { get; private set; }
        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>Alt çubuk eylemi (komut: MainMenu, ReviewLogs, RetrySameSeed, Restart).</summary>
        public event Action<string> ActionInvoked;

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < actions.Length; i++)
            {
                int k = i;
                actions[i].onClick.AddListener(() => Invoke(k));
            }
            root.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Invoke(int index)
        {
            if (vm?.Endbar == null || index >= vm.Endbar.Actions.Count) return;
            ActionInvoked?.Invoke(CommandOf(vm.Endbar.Actions[index], index));
        }

        /// <summary>Tuşla eylem (Enter, L): VM'de o tuşa bağlı eylem.</summary>
        public void InvokeKey(string key)
        {
            if (vm?.Endbar == null) return;
            for (int i = 0; i < vm.Endbar.Actions.Count; i++)
                if (string.Equals(vm.Endbar.Actions[i].Key, key, StringComparison.OrdinalIgnoreCase)) { Invoke(i); return; }
        }

        /// <summary>Komut VM'de yoksa sıradan çıkarılır (C4: ana menü, günlükler, aynı tohum, yeniden başla).</summary>
        static string CommandOf(CommandVM c, int index)
        {
            if (!string.IsNullOrEmpty(c.Command)) return c.Command;
            return index switch { 0 => "MainMenu", 1 => "ReviewLogs", 2 => "RetrySameSeed", _ => "Restart" };
        }

        bool canvasesHidden;

        public void Bind(GameOverVM g)
        {
            vm = g;
            bool open = g != null;
            root.SetActive(open);
            // Masa canvas'larına yalnız rapor açılıp kapanırken dokunulur: her bağlamada açmak, taktik harita
            // (RaidLauncher) sırasında gizlenen masayı geri getiriyordu
            // Edit mod önizlemesinde canvas'lara dokunulmaz: kapalı kaydedilirse oyunda masa görünmezdi (rapor zaten ekranı örter)
            if (Application.isPlaying && open != canvasesHidden)
            {
                canvasesHidden = open;
                foreach (var c in hideWhileOpen) if (c != null) c.enabled = !open;
            }
            if (!open) return;
            var T = DeskLoc.Instance;

            var r = g.Report;
            if (r != null)
            {
                DeskUi.SetText(headerTitle, T.T(r.Header?.Title));
                DeskUi.SetText(headerMeta, T.T(r.Header?.Meta));
                DeskUi.SetText(headerRight, T.T(r.Header?.Right));
                headerRow.Layout();
                DeskUi.SetText(resultCap, T.T(r.ResultCap));
                DeskUi.SetText(title, T.T(r.Title));
                DeskUi.SetText(who, T.T(r.Who));
                DeskUi.SetText(lead, T.T(r.Lead));
                DeskUi.SetActive(bigStamp, r.BigStamp != null);
                if (r.BigStamp != null)
                {
                    // CSS .bigstamp left:686 (sabit sol kenar); başlık uzarsa (EN) damga başlığın en az bigStampGap sağında
                    var sr = (RectTransform)bigStamp.transform;
                    if (float.IsNaN(bigStampLeft)) bigStampLeft = sr.anchoredPosition.x - sr.pivot.x * sr.sizeDelta.x;
                    bigStamp.Set(T.T(r.BigStamp.Text), StampInk.Red, StampSize.Large, r.BigStamp.RotDeg, "gameover.big");
                    var tr = title.rectTransform;
                    float titleRight = tr.anchoredPosition.x - tr.pivot.x * tr.rect.width + DeskUi.TextWidth(title, title.text);
                    float left = Mathf.Max(bigStampLeft, Mathf.Round(titleRight + bigStampGap));
                    sr.anchoredPosition = new Vector2(left + sr.pivot.x * sr.sizeDelta.x, sr.anchoredPosition.y);
                }
            }

            for (int i = 0; i < kpis.Length; i++)
            {
                bool on = i < g.Kpis.Count;
                DeskUi.SetActive(kpis[i], on);
                if (on) kpis[i].Bind(g.Kpis[i]);
            }
            chart.Bind(g.Chart);

            var t = g.Timeline;
            if (t != null)
            {
                DeskUi.SetText(timelineLabel, T.T(t.Label));
                DeskUi.SetText(timelineSub, T.T(t.LabelSub));
                timelineRow.Layout();
                timelinePlaque.sizeDelta = new Vector2(Mathf.Round(timelineRow.Width) + 2, timelinePlaque.sizeDelta.y);
                for (int i = 0; i < cards.Length; i++)
                {
                    bool on = i < t.Cards.Count;
                    DeskUi.SetActive(cards[i], on);
                    if (!on) continue;
                    var c = t.Cards[i];
                    t.Types.TryGetValue(c.Type ?? "", out var type);
                    cards[i].Bind(c, type, i < t.Rotations.Count ? t.Rotations[i] : 0);
                }
            }

            var e = g.Endbar;
            if (e != null)
            {
                DeskUi.SetText(endbarInfo, T.T(e.Info));
                for (int i = 0; i < actions.Length; i++)
                {
                    bool on = i < e.Actions.Count;
                    DeskUi.SetActive(actions[i], on);
                    if (!on) continue;
                    actions[i].SetKind(e.Actions[i].Kind);
                    actions[i].SetLabel(e.Actions[i].Label);
                    actions[i].SetShortcut(e.Actions[i].Key);
                }
                actionsRow.Layout();
            }
        }
    }
}
