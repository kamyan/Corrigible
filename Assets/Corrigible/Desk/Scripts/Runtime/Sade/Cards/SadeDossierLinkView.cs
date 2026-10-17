using System;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Bağlantı çipi yalnız VM'nin gösterilen sızmasını kullanır; eksik değer bilinmiyor çizilir.</summary>
    public sealed class SadeDossierLinkView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] SadePaper paper;
        [SerializeField] Image disc, ring, quarantine, badge;
        [SerializeField] TMP_Text value, nameLabel, spread;
        string region;
        public event Action<string> RegionRequested;

        public void Bind(JObject data)
        {
            region = (string)data?["region"];
            if (data == null) return;
            bool spreadIn = (bool?)data["spreadIn"] ?? false;
            SadeUi.Sprite(ring, "node_ring_44", "sade.ink");
            paper.Set(spreadIn ? "sade.panels.chipBgAi" : "sade.desk.regionCard.paper",
                spreadIn ? "sade.panels.chipBorderAi" : "sade.desk.regionCard.border", null);
            string name = SadePanelText.T((string)data["nameKey"] ?? "region." + region);
            DeskUi.SetText(nameLabel, name);
            DeskUi.SetActive(spread, spreadIn);
            DeskUi.SetText(spread, SadePanelText.T("dossier.spreadIn"));
            bool missing = data["value"] == null || data["value"].Type == JTokenType.Null;
            DeskUi.SetText(value, missing ? "?" : DeskFormat.Number((float)data["value"], 0));
            if (!missing && SadeTheme.Active != null)
            {
                DeskUi.SetColor(disc, SadeTheme.Active.RampFill((float)data["value"]));
                DeskUi.SetColor(value, SadeTheme.Active.RampText((float)data["value"]));
            }
            else { DeskUi.Tint(disc, "sade.map.unknownLand"); DeskUi.Tint(value, "sade.ink2"); }
            DeskUi.SetActive(quarantine, (bool?)data["quarantine"] ?? false);
            var trust = DeskUi.ParseTrust((string)data["trust"]);
            DeskUi.SetActive(badge, trust == TrustLevel.Verified || trust == TrustLevel.Report);
            SadeUi.Sprite(badge, trust == TrustLevel.Verified ? "node_badge_seal_20" : "node_badge_report_22x16");
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && region != null) RegionRequested?.Invoke(region);
        }
    }
}
