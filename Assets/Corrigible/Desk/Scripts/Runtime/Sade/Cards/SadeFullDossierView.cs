using System;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>S-Masa9'un kaydırılabilir tam dosyası. Sekme ve kapat sabittir; sayfa içeriği serileştirilmiş satırlarla çizilir.</summary>
    public sealed class SadeFullDossierView : PopupCardBase
    {
        [SerializeField] RectTransform tab, content;
        [SerializeField] ScrollRect scroll;
        [SerializeField] TMP_Text tabSub, regionName, code, population, linksMeta, monitoring;
        [SerializeField] StampView classified;
        [SerializeField] SadeCardInfiltrationView infiltration;
        [SerializeField] RectTransform headRoot, infiltrationRoot, sourcesRoot, historyRoot, linksRoot, actionsRoot, teamRoot;
        [SerializeField] TMP_Text sourcesHeading, sourcesCount, historyHeading, historyUnit, linksHeading, linksCount, actionsHeading, actionsNote, teamHeading;
        [SerializeField] SadeCardSourceRow[] sources = Array.Empty<SadeCardSourceRow>();
        [SerializeField] SadeDossierHistoryView history;
        [SerializeField] SadeDossierLinkView[] links = Array.Empty<SadeDossierLinkView>();
        [SerializeField] SadeCardActionView[] actions = Array.Empty<SadeCardActionView>();
        [SerializeField] SadeCardTeamView team;
        [SerializeField] DeskButton[] sendChips = Array.Empty<DeskButton>();
        [SerializeField] RectTransform financeRoot, recoveryRoot;
        [SerializeField] TMP_Text financeHeading, financeValue, financeNote, recoveryHeading, recoveryNote;
        [SerializeField] DeskButton recoveryButton, diplomacyButton;
        readonly string[] sendCommands = new string[4];

        public string RegionId { get; private set; }
        public Vector2 Size => Card.rect.size;
        public event Action<string> ActionRequested;
        public event Action<string> PreviewRequested;
        public event Action<string> RegionRequested;

        protected override void Awake()
        {
            base.Awake();
            foreach (var action in actions)
            {
                if (action == null) continue;
                action.Clicked += Execute;
                action.PreviewRequested += Preview;
            }
            foreach (var link in links) if (link != null) link.RegionRequested += RequestRegion;
            if (team != null) team.RaidRequested += RequestRaid;
            for (int i = 0; i < sendChips.Length; i++)
            {
                int idx = i;
                if (sendChips[i] != null)
                    sendChips[i].onClick.AddListener(() => { if (!string.IsNullOrEmpty(sendCommands[idx])) Execute(sendCommands[idx]); });
            }
            if (recoveryButton != null) recoveryButton.onClick.AddListener(() => Execute("recover"));
            if (diplomacyButton != null) diplomacyButton.onClick.AddListener(() => Execute("diplomacy"));
        }

        void Execute(string action) => ActionRequested?.Invoke(action);
        void RequestRaid() => Execute(team != null ? team.CommandId : "raid");
        void RequestRegion(string region) => RegionRequested?.Invoke(region);
        void Preview(string action)
        {
            foreach (var view in actions) if (view != null) view.SetPreview(view.CommandId == action);
            PreviewRequested?.Invoke(action);
        }

        public void Bind(JObject data)
        {
            if (data == null) return;
            bool changed = RegionId != (string)data["regionId"];
            RegionId = (string)data["regionId"];
            DeskUi.SetText(title, SadePanelText.T("dossier.tab"));
            SadeUi.Type(title, "drawerTitle");
            DeskUi.SetText(tabSub, SadePanelText.T("dossier.tabSub"));
            LayoutTab();
            SadeUi.Type(regionName, "dossierTitle");
            DeskUi.SetText(regionName, SadePanelText.T((string)data["nameKey"] ?? "region." + RegionId));
            DeskUi.SetText(code, (string)data["code"]);
            float nameW = SadeUi.Width(regionName, regionName.text);
            SadeUi.Place(regionName, 0, 2, nameW, 29);
            SadeUi.Place(code, nameW + 10, 9, 42, 18);
            bool secret = (bool?)data["classified"] ?? false;
            DeskUi.SetActive(classified, secret);
            if (secret)
            {
                classified.Set(SadePanelText.T("stamp.classified"), StampInk.Red, StampSize.Normal, -4);
                var stamp = (RectTransform)classified.transform;
                SadeUi.Place(stamp, 428 - stamp.sizeDelta.x, 2, stamp.sizeDelta.x, stamp.sizeDelta.y);
            }
            var meta = data["meta"] as JObject;
            DeskUi.SetText(population, SadeCardText.Literal((string)meta?["population"]) ?? "—");
            int? linkCountMeta = (int?)meta?["links"];
            DeskUi.SetText(linksMeta, linkCountMeta.HasValue ? SadePanelText.F("dossier.links", linkCountMeta.Value) : "—");
            bool? monitored = (bool?)meta?["monitoringNet"];
            DeskUi.SetText(monitoring, monitored.HasValue ? monitored.Value ? Loc.T("İzleme ağı kurulu") : SadePanelText.T("dossier.noMonitoring") : "—");
            infiltration?.Bind(data["infiltration"] as JObject, data["sources"] as JArray);

            var sourceData = data["sources"] as JArray;
            int sourceCount = Mathf.Min(sources.Length, sourceData?.Count ?? 0);
            float sourceY = 22;
            for (int i = 0; i < sources.Length; i++)
            {
                bool on = i < sourceCount; DeskUi.SetActive(sources[i], on);
                if (!on) continue;
                sources[i].Bind(sourceData[i] as JObject, i == sourceCount - 1);
                SadeUi.Place(sources[i], 0, sourceY, 428, sources[i].Height);
                sourceY += sources[i].Height;
            }
            DeskUi.SetText(sourcesHeading, SadePanelText.T("dossier.sources"));
            DeskUi.SetText(sourcesCount, SadePanelText.F("dossier.sourcesCount", sourceData?.Count ?? 0));
            DeskUi.SetText(historyHeading, SadePanelText.T("dossier.history"));
            DeskUi.SetText(historyUnit, SadePanelText.T("dossier.historyUnit"));
            history?.Bind(data["history"] as JObject);
            var linkData = data["links"] as JArray;
            int linkCount = Mathf.Min(links.Length, linkData?.Count ?? 0);
            for (int i = 0; i < links.Length; i++)
            {
                bool on = i < linkCount; DeskUi.SetActive(links[i], on);
                if (!on) continue;
                links[i].Bind(linkData[i] as JObject);
                SadeUi.Place(links[i], i % 2 * 217, 22 + i / 2 * 42, 211, 36);
            }
            DeskUi.SetText(linksHeading, SadePanelText.T("dossier.linksHead"));
            DeskUi.SetText(linksCount, DeskFormat.Number(linkData?.Count ?? 0, 0));
            var actionData = data["actions"] as JArray;
            int actionCount = Mathf.Min(actions.Length, actionData?.Count ?? 0);
            for (int i = 0; i < actions.Length; i++)
            {
                bool on = i < actionCount; DeskUi.SetActive(actions[i], on);
                if (on) { actions[i].Bind(actionData[i] as JObject); SadeUi.Place(actions[i], 0, 22 + i * 39, 428, 34); }
            }
            DeskUi.SetText(actionsHeading, SadePanelText.T("dossier.actions"));
            DeskUi.SetText(actionsNote, SadePanelText.T("dossier.actionsNote"));
            DeskUi.SetText(teamHeading, SadePanelText.T("dossier.field"));
            var teamObj = data["team"] as JObject;
            team?.Bind(teamObj);
            var sendArr = teamObj?["send"] as JArray;
            int sendCount = Mathf.Min(sendChips.Length, sendArr?.Count ?? 0);
            for (int i = 0; i < sendChips.Length; i++)
            {
                bool on = i < sendCount;
                DeskUi.SetActive(sendChips[i], on);
                sendCommands[i] = null;
                if (!on || sendChips[i] == null) continue;
                var chip = sendArr[i] as JObject;
                sendCommands[i] = (string)chip?["command"];
                sendChips[i].SetLabel(SadeCardText.Literal((string)chip?["label"]));
                sendChips[i].SetDisabled(SadeCardText.Literal((string)chip?["disabledReason"]));
                SadeUi.Place(sendChips[i], i * 108, 82, 102, 28);
            }

            var finObj = data["finance"] as JObject;
            bool hasFin = finObj != null && financeRoot != null;
            DeskUi.SetActive(financeRoot, hasFin);
            if (hasFin)
            {
                DeskUi.SetText(financeHeading, SadePanelText.T("dossier.finance"));
                DeskUi.SetText(financeValue, SadeCardText.Literal((string)finObj["value"]));
                DeskUi.SetText(financeNote, SadeCardText.Literal((string)finObj["note"]));
            }

            var recObj = data["recovery"] as JObject;
            bool hasRec = recObj != null && recoveryRoot != null;
            DeskUi.SetActive(recoveryRoot, hasRec);
            if (hasRec)
            {
                DeskUi.SetText(recoveryHeading, SadePanelText.T("dossier.recovery"));
                DeskUi.SetText(recoveryNote, SadeCardText.Literal((string)recObj["note"]));
                if (recoveryButton != null)
                {
                    recoveryButton.SetLabel(SadeCardText.Literal((string)recObj["recoverLabel"]));
                    recoveryButton.SetDisabled(SadeCardText.Literal((string)recObj["recoverDisabled"]));
                }
                if (diplomacyButton != null)
                {
                    diplomacyButton.SetLabel(SadeCardText.Literal((string)recObj["diplomacyLabel"]));
                    diplomacyButton.SetDisabled(SadeCardText.Literal((string)recObj["diplomacyDisabled"]));
                }
            }

            float y = 0;
            Section(headRoot, ref y, 61, 12);
            Section(infiltrationRoot, ref y, 66, 14);
            Section(sourcesRoot, ref y, sourceY, 14);
            Section(historyRoot, ref y, 126, 14);
            float linkRows = Mathf.Ceil(linkCount / 2f);
            Section(linksRoot, ref y, 22 + linkRows * 36 + Mathf.Max(0, linkRows - 1) * 6, 14);
            Section(actionsRoot, ref y, 22 + actionCount * 34 + Mathf.Max(0, actionCount - 1) * 5, 14);
            float teamH = sendCount > 0 ? 112 : 78;
            Section(teamRoot, ref y, teamH, hasFin || hasRec ? 14 : 0);
            if (hasFin) Section(financeRoot, ref y, 62, hasRec ? 14 : 0);
            if (hasRec) Section(recoveryRoot, ref y, 76, 0);
            if (content != null) content.sizeDelta = new Vector2(428, y);
            if (changed && scroll != null) scroll.verticalNormalizedPosition = 1;
        }

        void LayoutTab()
        {
            float tw = SadeUi.Width(title, title.text), sw = SadeUi.Width(tabSub, tabSub.text);
            SadeUi.Place(title, 38, 11, tw, 18);
            SadeUi.Place(tabSub, 46 + tw, 12, sw, 16);
            float width = 46 + tw + sw + 8 + 22 + 8;
            if (tab != null) tab.sizeDelta = new Vector2(width, 38);
            SadeUi.Place(close, width - 30, 8, 22, 22);
        }

        static void Section(RectTransform root, ref float y, float height, float gap)
        {
            if (root != null) SadeUi.Place(root, 0, y, 428, height);
            y += height + gap;
        }
    }
}
