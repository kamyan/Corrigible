using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Gösterilen sızma, kaynak aralığı ve saha/rapor işaretleri; iki bölge panelinde ortak kullanılır.</summary>
    public sealed class SadeCardInfiltrationView : MonoBehaviour
    {
        [SerializeField] TMP_Text heading, value, question, reportLabel, fieldLabel, gapNote;
        [SerializeField] RectTransform rangeRoot;
        [SerializeField] Image[] segments = new Image[5];
        [SerializeField] Image bandFill, bandFrame, reportMarker, fieldMarker, fieldIcon;

        public void Bind(JObject data, JArray dossierSources = null)
        {
            if (data == null) return;
            SadeUi.Type(heading, "sectionCap");
            DeskUi.SetText(heading, SadePanelText.T("card.infiltration"));
            SadeUi.Type(value, "cardBig");
            bool unknown = data["value"] == null || data["value"].Type == JTokenType.Null || (string)data["trust"] == "unknown";
            DeskUi.SetText(value, unknown ? "?" : DeskFormat.Percent((float)data["value"], 0));
            bool suspicious = (string)data["trust"] == "suspicious";
            DeskUi.SetActive(question, suspicious);
            SadeUi.Type(question, "cardBigQ"); DeskUi.SetText(question, "?");

            float report = -1, field = -1;
            if (data["sources"] is JArray sources)
                foreach (var source in sources)
                {
                    if ((string)source["kind"] == "report") report = (float?)source["value"] ?? -1;
                    if ((string)source["kind"] == "field") field = (float?)source["value"] ?? -1;
                }
            else
            {
                report = unknown ? -1 : (float?)data["value"] ?? -1;
                if (dossierSources != null)
                    foreach (var source in dossierSources)
                        if ((string)source["trust"] == "verified") { field = (float?)source["value"] ?? -1; break; }
                if (report >= 0 && field >= 0 && Mathf.Abs(field - report) < 0.5f)
                {
                    if ((string)data["trust"] == "verified") report = -1;
                    else field = -1;
                }
            }

            float width = rangeRoot != null ? rangeRoot.rect.width : 0;
            var theme = SadeTheme.Active;
            for (int i = 0; i < segments.Length; i++)
            {
                SadeUi.Place(segments[i], width * i / segments.Length, 14, width / segments.Length, 8);
                if (theme != null) DeskUi.SetColor(segments[i], theme.RampFill(i * 20));
            }
            bool hasRange = data["range"] is JObject;
            DeskUi.SetActive(bandFill, hasRange); DeskUi.SetActive(bandFrame, hasRange);
            if (hasRange)
            {
                float low = Mathf.Clamp((float?)data["range"]["from"] ?? 0, 0, 100);
                float high = Mathf.Clamp((float?)data["range"]["to"] ?? low, low, 100);
                SadeUi.Place(bandFill, low * width / 100, 10, (high - low) * width / 100, 16);
                SadeUi.Place(bandFrame, low * width / 100, 10, (high - low) * width / 100, 16);
            }
            Marker(reportMarker, report, width); Marker(fieldMarker, field, width);
            DeskUi.SetActive(reportLabel, report >= 0);
            DeskUi.SetActive(fieldLabel, field >= 0); DeskUi.SetActive(fieldIcon, field >= 0);
            if (report >= 0 && reportLabel != null)
            {
                SadeUi.Type(reportLabel, "rangeLabel");
                string text = SadePanelText.F("card.report", DeskFormat.Number(report, 0));
                DeskUi.SetText(reportLabel, text);
                float rw = SadeUi.Width(reportLabel, text);
                SadeUi.Place(reportLabel, Mathf.Max(0, width * (report - 1) / 100 - rw), 33, rw, 14);
            }
            if (field >= 0 && fieldLabel != null)
            {
                SadeUi.Type(fieldLabel, "rangeLabel");
                string text = SadePanelText.F("card.field", DeskFormat.Number(field, 0));
                DeskUi.SetText(fieldLabel, text);
                float fw = SadeUi.Width(fieldLabel, text);
                float x = Mathf.Min(Mathf.Max(0, width - fw - 15), width * (field + 1) / 100);
                SadeUi.Place(fieldIcon, x, 33, 12, 12);
                SadeUi.Place(fieldLabel, x + 15, 33, fw, 14);
            }
            DeskUi.SetActive(gapNote, data["gapNote"] != null);
            if (data["gapNote"] != null) DeskUi.SetText(gapNote, SadePanelText.F("dossier.gap", (int)data["gapNote"]));
        }

        static void Marker(Image image, float point, float width)
        {
            DeskUi.SetActive(image, point >= 0);
            if (point >= 0) SadeUi.Place(image, Mathf.Clamp(point, 0, 100) * width / 100 - 2, 6, 4, 24);
        }
    }
}
