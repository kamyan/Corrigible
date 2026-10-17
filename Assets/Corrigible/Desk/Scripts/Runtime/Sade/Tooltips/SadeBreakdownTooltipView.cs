using System.Collections.Generic;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>286 px gösterge fişi. FON denge çubuklarını; diğer göstergeler verilen kalemleri aynı kapta çizer.</summary>
    public sealed class SadeBreakdownTooltipView : SadeTooltipCardBase
    {
        [SerializeField] TMP_Text subtitle, headNet, headUnit, incomeLabel, expenseLabel, incomeValue, expenseValue;
        [SerializeField] TMP_Text netLabel, netValue, netUnit, runway, historyStart, historyEvent, historyEnd;
        [SerializeField] GameObject balanceRoot, genericRoot, historyRoot;
        [SerializeField] RectTransform netRoot;
        [SerializeField] Image incomeFill;
        [SerializeField] Image runwayIcon;
        [SerializeField] ChipTrustMarkView trustMark;
        [SerializeField] RawImage deficit;
        [SerializeField] Image[] expenseSegments, keySwatches;
        [SerializeField] TMP_Text[] keyLabels, itemLabels, itemValues;
        [SerializeField] SadeHistoryGraphic history;
        [SerializeField] TMP_Text note;

        readonly List<float> historyValues = new List<float>(32);
        readonly List<int> eventDays = new List<int>(4);

        /// <summary>Çubuklar yalnız gösterilen kalemlerden toplanır; negatif giderler genişliğe ters yönde sızmaz.</summary>
        public static float Sum(JArray items)
        {
            float total = 0;
            if (items != null) foreach (var item in items) total += Mathf.Max(0, Number(item["value"]));
            return total;
        }

        protected override void Render(JObject data)
        {
            bool fund = data["income"] is JArray || data["expense"] is JArray ||
                ((string)data["target"] == "fund" && !(data["items"] is JArray));
            Label(title, fund ? Loc.N("FON") : (string)data["titleKey"] ?? (string)data["title"] ?? TargetTitle((string)data["target"]));
            SadeUi.Type(title, "sectionCap");
            Label(subtitle, (string)data["subtitle"] ?? (fund ? Loc.N("günlük denge") : Loc.N("döküm")));
            float titleWidth = Mathf.Ceil(SadeUi.Width(title, title != null ? title.text : ""));
            SadeUi.Place(title, 13, 10, titleWidth, 20);
            bool hasTrust = data["trust"] != null;
            DeskUi.SetActive(trustMark, hasTrust);
            if (hasTrust && trustMark != null) trustMark.Set(DeskUi.ParseTrust((string)data["trust"]));
            float trustWidth = hasTrust && trustMark != null ? trustMark.Width + 6 : 0;
            SadeUi.Place(trustMark, 19 + titleWidth, 12, Mathf.Max(0, trustWidth - 6), 14);
            SadeUi.Place(subtitle, 21 + titleWidth + trustWidth, 9, Mathf.Max(0, 180 - titleWidth - trustWidth), 20);
            float net = Number(data["net"]);
            bool hasNet = HasNumber(data["net"]);
            DeskUi.SetActive(headNet, hasNet); DeskUi.SetActive(headUnit, hasNet);
            DeskUi.SetText(headNet, DeskFormat.Signed(net));
            DeskUi.Tint(headNet, net < 0 ? "sade.status.threat" : "sade.status.good");
            Label(headUnit, Loc.N("/gün"));
            DeskUi.SetActive(balanceRoot, fund); DeskUi.SetActive(genericRoot, !fund);

            float contentBottom;
            if (fund)
            {
                var income = data["income"] as JArray; var expenses = data["expense"] as JArray;
                float incoming = Sum(income), outgoing = Sum(expenses), scale = Mathf.Max(.001f, outgoing > 0 ? outgoing : incoming);
                Label(incomeLabel, Loc.N("Gelir")); Label(expenseLabel, Loc.N("Gider"));
                float labelWidth = Mathf.Max(40, Mathf.Ceil(Mathf.Max(SadeUi.Width(incomeLabel, incomeLabel.text), SadeUi.Width(expenseLabel, expenseLabel.text))));
                SadeUi.Place(incomeLabel, 0, 0, labelWidth, 22);
                SadeUi.Place(expenseLabel, 0, 22, labelWidth, 22);
                float barX = labelWidth + 8, barWidth = 212 - barX;
                SadeUi.Place(incomeFill.transform.parent, barX, 5, barWidth, 12);
                if (expenseSegments.Length > 0) SadeUi.Place(expenseSegments[0].transform.parent, barX, 27, barWidth, 12);
                DeskUi.SetText(incomeValue, DeskFormat.Signed(incoming));
                DeskUi.SetText(expenseValue, DeskFormat.Signed(-outgoing));
                float innerWidth = barWidth - 2;
                float fillWidth = innerWidth * Mathf.Clamp01(incoming / scale);
                SadeUi.Place(incomeFill, 1, 1, fillWidth, 10);
                SadeUi.Place(deficit, 1 + fillWidth, 1, innerWidth - fillWidth, 10);
                if (deficit != null) deficit.uvRect = new Rect(0, 0, Mathf.Max(0, innerWidth - fillWidth) / 8, 10f / 8);
                float x = 1;
                for (int i = 0; i < expenseSegments.Length; i++)
                {
                    bool active = expenses != null && i < expenses.Count;
                    DeskUi.SetActive(expenseSegments[i], active);
                    if (!active) continue;
                    float width = innerWidth * Mathf.Max(0, Number(expenses[i]["value"])) / scale;
                    SadeUi.Place(expenseSegments[i], x, 1, width, 10); x += width;
                }
                BindKeys(income, expenses);
                contentBottom = 117 + extraKeyHeight;
                if (note != null) DeskUi.SetActive(note, false);
            }
            else contentBottom = BindItems(data);

            bool netOn = fund || hasNet || data["total"] != null;
            DeskUi.SetActive(netRoot, netOn);
            SadeUi.Place(netRoot, 13, contentBottom, 260, 30);
            Label(netLabel, Loc.N("Net"));
            DeskUi.SetText(netValue, hasNet ? DeskFormat.Signed(net) : Text((string)data["total"] ?? ""));
            DeskUi.Tint(netValue, net < 0 ? "sade.status.threat" : "sade.status.good");
            Label(netUnit, Loc.N("/gün")); DeskUi.SetActive(netUnit, hasNet);
            DeskUi.SetText(runway, HasNumber(data["runwayDays"]) ? SadePanelText.F("tip.runway", DeskFormat.Number(Number(data["runwayDays"]), 0)) : Text((string)data["totalRight"] ?? ""));
            float runwayWidth = Mathf.Max(69, Mathf.Ceil(SadeUi.Width(runway, runway.text)));
            SadeUi.Place(runway, 260 - runwayWidth, 5, runwayWidth, 25);
            SadeUi.Place(runwayIcon, 245 - runwayWidth, 12, 13, 13);
            DeskUi.SetActive(runwayIcon, HasNumber(data["runwayDays"]));
            historyValues.Clear(); eventDays.Clear();
            var past = data["history"] as JObject;
            if (past?["values"] is JArray values) foreach (var v in values) if (HasNumber(v)) historyValues.Add(Number(v));
            int firstDay = (int?)past?["fromDay"] ?? 0;
            if (past?["events"] is JArray events) foreach (var e in events) eventDays.Add((int?)e["day"] ?? firstDay);
            bool showHistory = historyValues.Count > 1;
            DeskUi.SetActive(historyRoot, showHistory);
            float historyY = contentBottom + (netOn ? 34 : 4);
            SadeUi.Place(historyRoot != null ? historyRoot.transform : null, 13, historyY, 260, 56);
            if (history != null) history.Bind(historyValues, firstDay, eventDays);
            if (showHistory)
            {
                DeskUi.SetText(historyStart, Loc.F("{0} · {1}", DeskFormat.DayCode(firstDay), DeskFormat.Number(historyValues[0])));
                DeskUi.SetText(historyEnd, Loc.F("bugün · {0}", DeskFormat.Number(historyValues[historyValues.Count - 1])));
                var ev = (past?["events"] as JArray)?.First as JObject;
                DeskUi.SetText(historyEvent, ev == null ? "" : Loc.F("{0} {1}", Text((string)ev["key"]), DeskFormat.Signed(Number(ev["value"]))));
            }
            Card.sizeDelta = new Vector2(286, showHistory ? historyY + 57 : contentBottom + (netOn ? 36 : 10));
        }

        void BindKeys(JArray income, JArray expense)
        {
            int count = (income?.Count ?? 0) + (expense?.Count ?? 0);
            float x = 0, y = 0;
            for (int i = 0; i < keyLabels.Length; i++)
            {
                bool active = i < count; DeskUi.SetActive(keyLabels[i], active);
                if (i < keySwatches.Length) DeskUi.SetActive(keySwatches[i], active);
                if (!active) continue;
                bool isIncome = i < (income?.Count ?? 0);
                var item = isIncome ? income[i] : expense[i - (income?.Count ?? 0)];
                string label = Text((string)item["key"] ?? (string)item["label"] ?? "");
                DeskUi.SetText(keyLabels[i], isIncome ? label : Loc.F("{0} {1}", label, DeskFormat.Number(Number(item["value"]))));
                float width = Mathf.Ceil(SadeUi.Width(keyLabels[i], keyLabels[i].text));
                // Referansın 212 px anahtarı Türkçede dahi 220 px olur; uzun dilde ikinci satıra geçer.
                if (x > 0 && x + width + 13 > 224) { x = 0; y += 17; }
                if (i < keySwatches.Length)
                {
                    SadeUi.Place(keySwatches[i], x, 4 + y, 9, 9);
                    DeskUi.Tint(keySwatches[i], isIncome ? "sade.status.good" : i == (income?.Count ?? 0) ? "sade.status.threat" : "sade.raid.turnLastText");
                }
                SadeUi.Place(keyLabels[i], x + 13, y, width, 17); x += width + 23;
            }
            // Anahtarın ikinci satırı varsa aşağıdaki satırları aynı miktarda iteriz.
            extraKeyHeight = y;
        }
        float extraKeyHeight;

        float BindItems(JObject data)
        {
            var items = data["items"] as JArray ?? data["rows"] as JArray;
            int count = Mathf.Min(items?.Count ?? 0, itemLabels.Length);
            for (int i = 0; i < itemLabels.Length; i++)
            {
                bool active = i < count; DeskUi.SetActive(itemLabels[i], active);
                if (i < itemValues.Length) DeskUi.SetActive(itemValues[i], active);
                if (!active) continue;
                var item = items[i];
                Label(itemLabels[i], (string)item["labelKey"] ?? (string)item["key"] ?? (string)item["label"] ?? "");
                FitText(itemLabels[i], 180, 12);
                string v = HasNumber(item["value"]) ? SadeUi.Value(Number(item["value"]), (int?)item["decimals"] ?? 1,
                    (bool?)item["percent"] == true, (bool?)item["approx"] == true) : (string)item["value"] ?? "?";
                if (i < itemValues.Length) { DeskUi.SetText(itemValues[i], v); DeskUi.Tint(itemValues[i], (string)item["status"] == "bad" ? "sade.status.threat" : (string)item["status"] == "good" ? "sade.status.good" : "sade.ink"); }
            }
            float bottom = 47 + count * 22;
            bool hasNote = !string.IsNullOrEmpty((string)data["note"]);
            DeskUi.SetActive(note, hasNote);
            if (hasNote)
            {
                Label(note, (string)data["note"]); SadeUi.Place(note, 13, bottom, 260, 36);
                if (note != null) { note.textWrappingMode = TextWrappingModes.Normal; bottom += Mathf.Max(20, note.GetPreferredValues(note.text, 260, 500).y) + 6; }
            }
            return bottom;
        }

        static string TargetTitle(string id)
        {
            switch (id)
            {
                case "politicalPower": return Loc.N("SİYASİ GÜÇ");
                case "globalTrust": return Loc.N("KÜRESEL GÜVEN");
                case "dependence": return Loc.N("BAĞIMLILIK");
                case "singularity": return Loc.N("TEKİLLİK");
                case "alignment": return Loc.N("HİZALAMA");
                case "deletion": return Loc.N("SİLME");
                default: return Loc.N("DÖKÜM");
            }
        }
    }
}
