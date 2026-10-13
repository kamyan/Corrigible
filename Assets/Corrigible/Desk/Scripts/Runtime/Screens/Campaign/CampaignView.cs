using System;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Krizler arası kampanya dosyası (F8.3, taslak <c>drafts/kampanya</c>): oyun sonu keçesi üstünde kâğıt (başlık, alanlar,
    /// büyük başlık, krizler) + üç yükseltme kartı (olay seçenek kartının kendisi: 1/2/3 seç, Enter imzala) + alınan
    /// yükseltmeler notu + alt şerit. Kampanya bittiyse kart yok, alt şeritte “Kampanyayı bitir”.
    /// </summary>
    public sealed class CampaignView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text headerTitle, headerRight;
        [SerializeField] TMP_Text[] fieldKeys = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text[] fieldValues = Array.Empty<TMP_Text>();
        [SerializeField] StampView fieldStamp;
        [SerializeField] TMP_Text title, subtitle, resultsCap, resultsRight;
        [SerializeField] RectTransform resultsRule;
        [SerializeField] float resultsWidth = 1390;
        [SerializeField] RectTransform resultsRoot;
        [SerializeField] CampaignResultRow rowTemplate;
        [SerializeField] RectTransform paper;
        [SerializeField] float rowHeight = 41.5f, paperBottomPad = 18;
        [SerializeField] EventOptionView[] upgrades = Array.Empty<EventOptionView>();
        [SerializeField] GameObject note;
        [SerializeField] TMP_Text noteTitle, noteHand, noteText;
        [SerializeField] TMP_Text footerText;
        [SerializeField] DeskButton exit;
        [SerializeField] KeyChipView[] footerKeys = Array.Empty<KeyChipView>();
        [SerializeField] TMP_Text footerHint;
        [SerializeField] DeskRow footerRow;

        CampaignVM vm;
        DeskPool<CampaignResultRow> rows;
        int selected;
        float resultsTop;

        public bool IsOpen => root != null && root.activeSelf;
        public static CampaignView Instance { get; private set; }
        public event Action<string> Invoked;

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < upgrades.Length; i++)
            {
                upgrades[i].Index = i;
                upgrades[i].Hovered += Select;
                upgrades[i].Signed += k => Sign(k);
            }
            exit.onClick.AddListener(() => Invoked?.Invoke(vm?.Exit?.Command));
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            resultsTop = resultsRoot != null ? -resultsRoot.anchoredPosition.y : 0;
            root.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Select(int i)
        {
            if (vm == null || i < 0 || i >= vm.Upgrades.Count) return;
            selected = i;
            for (int k = 0; k < upgrades.Length && k < vm.Upgrades.Count; k++) upgrades[k].SetSelected(k == selected);
        }

        /// <summary>Enter: seçili yükseltme; kampanya bittiyse çıkış komutu.</summary>
        public void SignSelected()
        {
            if (vm == null) return;
            if (vm.Upgrades.Count == 0) { Invoked?.Invoke(vm.Exit?.Command); return; }
            Sign(selected);
        }

        void Sign(int i)
        {
            if (vm != null && i >= 0 && i < vm.Upgrades.Count) Invoked?.Invoke(vm.Upgrades[i].Sign?.Command);
        }

        public void Bind(CampaignVM model)
        {
            bool wasOpen = IsOpen;
            vm = model;
            root.SetActive(model != null);
            if (model == null) return;
            if (!wasOpen) selected = 0;
            var T = DeskLoc.Instance;
            var m = model.Memo;
            DeskUi.SetText(headerTitle, T.T(m?.Header?.Title));
            DeskUi.SetText(headerRight, T.T(m?.Header?.Right));
            for (int i = 0; i < fieldKeys.Length; i++)
            {
                bool on = m != null && i < m.Fields.Count;
                DeskUi.SetActive(fieldKeys[i], on);
                if (i < fieldValues.Length) DeskUi.SetActive(fieldValues[i], on && m.Fields[i].Stamp == null);
                if (!on) continue;
                DeskUi.SetText(fieldKeys[i], T.T(m.Fields[i].K));
                if (m.Fields[i].Stamp != null)
                {
                    var s = m.Fields[i].Stamp;
                    fieldStamp.Set(T.T(s.Text), StampView.ParseInk(s.Color), StampSize.Normal, s.Rot, "campaign.result", s.FontSize);
                }
                else if (i < fieldValues.Length) DeskUi.SetText(fieldValues[i], T.T(m.Fields[i].V));
            }
            DeskUi.SetActive(fieldStamp, m != null && m.Fields.Exists(f => f.Stamp != null));
            DeskUi.SetText(title, T.T(m?.Title));
            DeskUi.SetText(subtitle, T.T(m?.Subtitle));
            DeskUi.SetText(resultsCap, T.T(m?.SourcesHeader?.Title));
            DeskUi.SetText(resultsRight, m?.SourcesHeader?.Right);
            DeskUi.CapRule(resultsCap, resultsRule, resultsRight, resultsWidth);

            rows ??= new DeskPool<CampaignResultRow>(rowTemplate, resultsRoot);
            int n = m?.Sources.Count ?? 0;
            for (int i = 0; i < n; i++)
            {
                var r = rows.Get(i);
                r.Bind(i + 1, m.Sources[i], i == n - 1);
                ((RectTransform)r.transform).anchoredPosition = new Vector2(0, -i * rowHeight);
            }
            rows.Trim(n);
            // Kâğıt satır sayısına göre uzar
            paper.sizeDelta = new Vector2(paper.sizeDelta.x, resultsTop + n * rowHeight + paperBottomPad);

            for (int i = 0; i < upgrades.Length; i++)
            {
                bool on = i < model.Upgrades.Count;
                DeskUi.SetActive(upgrades[i], on);
                if (on) upgrades[i].Bind(model.Upgrades[i], i == selected);
            }
            DeskUi.SetActive(note, !string.IsNullOrEmpty(model.NoteTitle));
            DeskUi.SetText(noteTitle, T.T(model.NoteTitle));
            DeskUi.SetText(noteHand, T.T(model.NoteHand));
            DeskUi.SetText(noteText, T.T(model.NoteText));
            DeskUi.SetText(footerText, T.T(model.Footer?.Text));
            exit.SetLabel(T.T(model.Exit?.Label));
            var keys = model.Footer?.Hints.Count > 0 ? model.Footer.Hints[0].Keys : null;
            for (int i = 0; i < footerKeys.Length; i++)
            {
                bool on = keys != null && i < keys.Count;
                DeskUi.SetActive(footerKeys[i], on);
                if (on) footerKeys[i].Set(keys[i]);
            }
            DeskUi.SetText(footerHint, model.Footer?.Hints.Count > 0 ? T.T(model.Footer.Hints[0].Text) : "");
            if (footerRow != null) footerRow.Layout();
        }
    }
}
