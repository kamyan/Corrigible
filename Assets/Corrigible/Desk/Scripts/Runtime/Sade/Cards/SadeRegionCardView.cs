using System;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>S-Masa2'nin küçük bölge kartı; etkileşimleri sahibine bildirir, simülasyon kuralı çalıştırmaz.</summary>
    public sealed class SadeRegionCardView : PopupCardBase
    {
        [SerializeField] TMP_Text code, statusLabel, statusQuestion, moreLabel, previewCost, previewSource;
        [SerializeField] Image statusRing, previewArrow;
        [SerializeField] SadeCardInfiltrationView infiltration;
        [SerializeField] SadeCardStatView[] stats = Array.Empty<SadeCardStatView>();
        [SerializeField] SadeCardActionView[] actions = Array.Empty<SadeCardActionView>();
        [SerializeField] StampView previewStamp;
        [SerializeField] SadeCardTeamView team;
        [SerializeField] DeskButton fullFile;

        public string RegionId { get; private set; }
        public Vector2 Size => Card.rect.size;
        public event Action<string> ActionRequested;
        public event Action<string> PreviewRequested;

        protected override void Awake()
        {
            base.Awake();
            foreach (var action in actions)
            {
                if (action == null) continue;
                action.Clicked += Execute;
                action.PreviewRequested += Preview;
            }
            if (team != null) team.RaidRequested += RequestRaid;
            if (fullFile != null) fullFile.onClick.AddListener(RequestFullFile);
        }

        void Execute(string action) => ActionRequested?.Invoke(action);
        void RequestRaid() => Execute(team != null ? team.CommandId : "raid");
        void RequestFullFile() => Execute("fullDossier");
        void Preview(string action)
        {
            foreach (var view in actions) if (view != null) view.SetPreview(view.CommandId == action);
            PreviewRequested?.Invoke(action);
        }

        public void Bind(JObject data)
        {
            if (data == null) return;
            RegionId = (string)data["regionId"];
            SadeUi.Type(title, "cardTitle");
            DeskUi.SetText(title, SadePanelText.T((string)data["nameKey"] ?? "region." + RegionId));
            SadeUi.Type(code, "cardCode"); DeskUi.SetText(code, (string)data["code"]);
            float titleWidth = SadeUi.Width(title, title.text);
            if (titleWidth > Size.x - 96)
            {
                DeskUi.SetText(title, SadePanelText.T("region.short." + RegionId));
                titleWidth = SadeUi.Width(title, title.text);
            }
            SadeUi.Place(title, 17, 14.5f, titleWidth, 26);
            SadeUi.Place(code, 27 + titleWidth, 17, 48, 20);
            DeskUi.SetText(statusLabel, SadePanelText.T((string)data["status"]?["textKey"]));
            SadeUi.Type(statusLabel, "cardStatus");
            bool suspicious = (string)data["status"]?["trust"] == "suspicious";
            DeskUi.SetActive(statusRing, suspicious); DeskUi.SetActive(statusQuestion, suspicious);
            infiltration?.Bind(data["infiltration"] as JObject);
            var statData = data["stats"] as JArray;
            for (int i = 0; i < stats.Length; i++)
            {
                bool active = statData != null && i < statData.Count;
                DeskUi.SetActive(stats[i], active);
                if (active) stats[i].Bind(statData[i] as JObject);
            }
            var actionData = data["actions"] as JArray;
            for (int i = 0; i < actions.Length; i++)
            {
                bool active = actionData != null && i < actionData.Count;
                DeskUi.SetActive(actions[i], active);
                if (active) actions[i].Bind(actionData[i] as JObject);
            }
            BindPreview(data["preview"] as JObject);
            team?.Bind(data["team"] as JObject);
            DeskUi.SetText(moreLabel, SadePanelText.T("card.more"));
            if (fullFile != null) fullFile.SetLabel(SadePanelText.T("card.fullFile"));
        }

        void BindPreview(JObject preview)
        {
            if (preview == null) return;
            int days = (int?)preview["days"] ?? 0, fund = (int?)preview["costFund"] ?? 0, power = (int?)preview["costPower"] ?? 0;
            string cost = power > 0
                ? days > 0 ? Loc.F("{0} gün\n{1} fon · {2} SG", days, fund, power) : Loc.F("hemen\n{0} SG", power)
                : days > 0 ? SadePanelText.F("card.previewDays", days, fund) : Loc.T("hemen");
            DeskUi.SetText(previewCost, cost); SadeUi.Type(previewCost, "previewNum");
            DeskUi.SetText(previewSource, days > 0 ? SadePanelText.T("card.previewSource") : Loc.T("işlem")); SadeUi.Type(previewSource, "preview");
            previewStamp?.Set(SadePanelText.T((string)preview["resultStampKey"] ?? "stamp.verified"), StampInk.Blue, StampSize.Small, -3);
            float cw = SadeUi.Width(previewCost, cost), sw = SadeUi.Width(previewSource, previewSource.text);
            SadeUi.Place(previewCost, 34, power > 0 ? 5 : 11, cw, power > 0 ? 30 : 18);
            // Şerit simgeleri sabit; maliyet İngilizcede büyüdüğünde kaynak ve damga kalan alana taşınır.
            SadeUi.Place(previewArrow, 42 + cw, 13, 14, 14);
            SadeUi.Place(previewSource, 64 + cw, 11, sw, 18);
            if (previewStamp != null)
            {
                var stamp = (RectTransform)previewStamp.transform;
                SadeUi.Place(stamp, Mathf.Min(330 - stamp.sizeDelta.x - 9, 70 + cw + sw), 9, stamp.sizeDelta.x, stamp.sizeDelta.y);
            }
        }
    }
}
