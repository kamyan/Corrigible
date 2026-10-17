using System;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Bölge panelinin saha ekibi ve baskın bağlantısı. Başarı yüzdesi doğrudan gösterilen VM'den gelir.</summary>
    public sealed class SadeCardTeamView : MonoBehaviour
    {
        [SerializeField] TMP_Text letter, nameLabel, sub, raidLabel, raidChance;
        [SerializeField] Button raid;
        [SerializeField] KeyChipView raidKey;

        public string CommandId { get; private set; } = "raid";
        public event Action RaidRequested;
        void Awake() { if (raid != null) raid.onClick.AddListener(RequestRaid); }
        void OnDestroy() { if (raid != null) raid.onClick.RemoveListener(RequestRaid); }
        void RequestRaid() => RaidRequested?.Invoke();

        public void Bind(JObject data)
        {
            DeskUi.SetActive(this, data != null);
            if (data == null) return;
            string id = (string)data["id"];
            CommandId = (string)data["command"] ?? "raid";
            DeskUi.SetText(letter, id);
            SadeUi.Type(nameLabel, "teamName");
            string nameText = data["name"] != null ? SadeCardText.Literal((string)data["name"]) : SadeCardText.TeamName(id);
            DeskUi.SetText(nameLabel, nameText);
            SadeUi.Type(sub, "teamSub");
            string subText = data["sub"] != null ? SadeCardText.Literal((string)data["sub"]) : SadeCardText.TeamSub((string)data["city"], SadePanelText.T((string)data["statusKey"] ?? "team.ready"));
            DeskUi.SetText(sub, subText);
            string actionText = data["actionLabel"] != null ? SadeCardText.Literal((string)data["actionLabel"]) : SadePanelText.T("card.raid");
            DeskUi.SetText(raidLabel, actionText);
            bool hasChance = data["raidSuccess"] != null && data["raidSuccess"].Type != JTokenType.Null;
            DeskUi.SetActive(raidChance, hasChance);
            if (hasChance) DeskUi.SetText(raidChance, DeskFormat.Percent((float)data["raidSuccess"], 0));
            if (raidKey != null) raidKey.Set((string)data["raidKey"] ?? "B");
            if (raid != null) raid.interactable = (bool?)data["raidEnabled"] ?? hasChance;
            float rw = SadeUi.Width(raidLabel, raidLabel.text), chanceW = hasChance ? SadeUi.Width(raidChance, raidChance.text) : 0;
            float buttonW = 12 + rw + (hasChance ? chanceW + 6 : 0) + 7 + 18 + 12 + 2;
            float width = ((RectTransform)transform).rect.width;
            SadeUi.Place(raid, width - 11 - buttonW, 10, buttonW, 36);
            SadeUi.Place(raidLabel, 12, 10, rw, 17);
            SadeUi.Place(raidChance, 12 + rw + 6, 10, chanceW, 17);
            SadeUi.Place(raidKey, buttonW - 31, 9, 18, 18);
            float textW = Mathf.Max(40, width - buttonW - 70);
            SadeUi.Place(nameLabel, 49, 13, textW, 18);
            SadeUi.Place(sub, 49, 31, textW, 15);
        }
    }
}
