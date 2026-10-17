using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>214 px düğüm ipucu. Yalnız verilen gösterilen değeri kullanır; bilinmeyeni sıfıra dönüştürmez.</summary>
    public sealed class SadeNodeTooltipView : SadeTooltipCardBase
    {
        [SerializeField] TMP_Text regionName, trustText, value, delta, perDay, hint;
        [SerializeField] Image trustFill, trustFrame, deltaArrow;
        [SerializeField] RectTransform trustRoot, valueRow, rampRoot, marker, footer;

        public static bool TryShownValue(JObject data, out float value)
        {
            value = 0;
            if (data == null || (string)data["trust"] == "unknown" || !HasNumber(data["value"])) return false;
            value = Number(data["value"]);
            return true;
        }

        protected override void Render(JObject data)
        {
            string trust = (string)data["trust"] ?? "unknown";
            string source = (string)data["nameKey"] ?? (string)data["name"] ?? "region." + (string)data["regionId"];
            DeskUi.SetText(regionName, DeskText.Upper(Text(source)));
            SadeUi.Type(regionName, "nodeTipName");
            string trustSource = trust == "verified" ? Loc.N("onaylandı") : trust == "report" ? Loc.N("~ rapor") :
                trust == "suspicious" ? Loc.N("şüpheli") : Loc.N("bilinmiyor");
            Label(trustText, trustSource);
            float trustWidth = Mathf.Ceil(SadeUi.Width(trustText, trustText != null ? trustText.text : "")) + 12;
            trustWidth = Mathf.Clamp(trustWidth, 44, 94);
            SadeUi.Place(trustRoot, 201 - trustWidth, 10, trustWidth, 18);
            string trustColor = trust == "report" ? "sade.carbon" : trust == "suspicious" ? "sade.status.warning" :
                trust == "verified" ? "sade.status.info" : "sade.ink3";
            DeskUi.Tint(trustText, trustColor);
            SadeUi.Sprite(trustFill, "sh_fill_r2", trust == "report" ? "sade.onion" : "sade.paper");
            SadeUi.Sprite(trustFrame, trust == "report" || trust == "unknown" ? "sh_dash_r2_1" : "sh_frame_r2_1", trustColor);

            float nameWidth = 188 - trustWidth - 8;
            FitText(regionName, nameWidth, 13);
            if (regionName != null) regionName.textWrappingMode = TextWrappingModes.Normal;
            float nameHeight = regionName != null ? Mathf.Max(18, regionName.GetPreferredValues(regionName.text, nameWidth, 200).y) : 18;
            SadeUi.Place(regionName, 13, 10, nameWidth, nameHeight);
            float growth = Mathf.Max(0, nameHeight - 18);

            bool known = TryShownValue(data, out float shown);
            bool approx = (bool?)data["approx"] == true || trust == "report";
            DeskUi.SetText(value, known ? SadeUi.Value(shown, (int?)data["decimals"] ?? 0, false, approx) : "?");
            SadeUi.Type(value, "nodeTipValue");
            DeskUi.Tint(value, trust == "report" ? "sade.carbon" : "sade.ink");
            float valueWidth = Mathf.Ceil(SadeUi.Width(value, value != null ? value.text : "?"));
            SadeUi.Place(value, 0, 0, valueWidth, 34);
            SadeUi.Place(valueRow, 13, 34 + growth, 188, 34);

            bool hasDelta = known && HasNumber(data["deltaPerDay"]);
            DeskUi.SetActive(delta, hasDelta); DeskUi.SetActive(deltaArrow, hasDelta); DeskUi.SetActive(perDay, hasDelta);
            if (hasDelta)
            {
                float change = Number(data["deltaPerDay"]);
                string color = (bool?)data["deltaGood"] == true ? "sade.status.good" : "sade.status.threat";
                SadeUi.Sprite(deltaArrow, Mathf.Approximately(change, 0) ? "ic_flat_12b" : change < 0 ? "ic_arrow_down_12b" : "ic_arrow_up_12b", color);
                DeskUi.SetText(delta, DeskFormat.Number(Mathf.Abs(change), 1)); DeskUi.Tint(delta, color);
                Label(perDay, Loc.N("/gün"));
                float x = valueWidth + 10;
                SadeUi.Place(deltaArrow, x, 9, 12, 12);
                float dw = Mathf.Ceil(SadeUi.Width(delta, delta != null ? delta.text : ""));
                SadeUi.Place(delta, x + 15, 7, dw, 18);
                SadeUi.Place(perDay, x + 17 + dw, 10, 45, 16);
            }
            SadeUi.Place(rampRoot, 13, 72 + growth, 188, 16);
            DeskUi.SetActive(marker, known);
            if (known) SadeUi.Place(marker, Mathf.Clamp01(shown / 100) * 188 - 2, 0, 4, 16);
            SadeUi.Place(footer, 13, 96 + growth, 188, 16);
            Label(hint, Loc.N("tıkla: bölge kartı"));
            Card.sizeDelta = new Vector2(214, 119 + growth);
        }
    }
}
