using System.Collections.Generic;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Son 30 gün: rapor çizgisi, son oynama ve saha ölçümü; değerler doğrudan VM serileridir.</summary>
    public sealed class SadeDossierHistoryView : MonoBehaviour
    {
        [SerializeField] UILineGraphic report, tampered;
        [SerializeField] Image lastRing, fieldDiamond;
        [SerializeField] TMP_Text lastValue, fieldLabel, reportLabel, firstDay, today;
        readonly List<Vector2> points = new List<Vector2>();

        public void Bind(JObject history)
        {
            if (firstDay != null) firstDay.fontSize = 11;
            if (today != null) today.fontSize = 11;
            points.Clear();
            var values = history?["report"] as JArray;
            bool hasHistory = values != null && values.Count > 0;
            DeskUi.SetActive(reportLabel, hasHistory); DeskUi.SetActive(firstDay, hasHistory); DeskUi.SetActive(today, hasHistory);
            if (!hasHistory)
            {
                DeskUi.SetActive(report, false); DeskUi.SetActive(tampered, false);
                DeskUi.SetActive(lastRing, false); DeskUi.SetActive(lastValue, false);
                DeskUi.SetActive(fieldDiamond, false); DeskUi.SetActive(fieldLabel, false);
                return;
            }
            bool edited = (bool?)history["tamperedLast"] ?? false;
            float first = (float)values[0][0], last = (float)values[values.Count - 1][0];
            float span = Mathf.Max(1, last - first);
            for (int i = 0; i < values.Count - (edited ? 1 : 0); i++) points.Add(Point((float)values[i][0], (float)values[i][1], first, span));
            DeskUi.SetActive(report, true); report.SetPoints(points);
            DeskUi.SetActive(tampered, edited);
            Vector2 end = Point(last, (float)values[values.Count - 1][1], first, span);
            if (edited && values.Count > 1)
            {
                tampered.SetPoints(new[] { Point((float)values[values.Count - 2][0], (float)values[values.Count - 2][1], first, span), end });
            }
            DeskUi.SetActive(lastRing, edited); DeskUi.SetActive(lastValue, edited);
            if (edited)
            {
                SadeUi.Place(lastRing, end.x - 4, -end.y - 4, 8, 8);
                SadeUi.Place(lastValue, end.x + 8, -end.y - 6, 28, 14);
                DeskUi.SetText(lastValue, DeskFormat.Number((float)values[values.Count - 1][1], 0));
            }
            var fields = history["field"] as JArray;
            bool hasField = fields != null && fields.Count > 0;
            DeskUi.SetActive(fieldDiamond, hasField); DeskUi.SetActive(fieldLabel, hasField);
            if (hasField)
            {
                var field = fields[fields.Count - 1];
                Vector2 p = Point((float)field[0], (float)field[1], first, span);
                SadeUi.Place(fieldDiamond, p.x - 3.5f, -p.y - 3.5f, 7, 7);
                string text = SadePanelText.F("card.field", DeskFormat.Number((float)field[1], 0));
                DeskUi.SetText(fieldLabel, text);
                float width = SadeUi.Width(fieldLabel, text);
                SadeUi.Place(fieldLabel, p.x - 12 - width, -p.y - 7, width, 16);
            }
            DeskUi.SetText(reportLabel, Loc.T("rapor"));
            DeskUi.SetText(firstDay, Loc.F("G-{0}", ((int)first).ToString("000")));
            DeskUi.SetText(today, Loc.T("bugün"));
        }

        // Referans: solda 34 px ölçek payı, sağda 22 px; 0–60 aralığı 78 px yükseklikte.
        static Vector2 Point(float day, float value, float first, float span) => new Vector2(34 + (day - first) / span * 356, -(8 + (60 - value) / 60 * 78));
    }
}
