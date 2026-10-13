using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bildirim kâğıdının belge kipi (F8.3; taslaklar <c>drafts/perde</c>, <c>baskin_brifing</c>, <c>baskin_sonuc</c>): kaynakların
    /// altında sonuç satırları (bedel/kazanç rengiyle), yan yana sarı kutular, kesik çerçeveli risk satırı ve düğmeler
    /// (ilki mürekkep + Enter, ikincisi kâğıt + Esc). Kâğıdın yüksekliğine eklenecek boyu döner.
    /// </summary>
    public sealed class EventDocView : MonoBehaviour
    {
        [SerializeField] TMP_Text linesCap, linesRight;
        [SerializeField] RectTransform linesRule;
        [SerializeField] RectTransform lines;
        [SerializeField] EventLineRow lineTemplate;
        [SerializeField] RectTransform[] boxes = Array.Empty<RectTransform>();
        [SerializeField] TMP_Text[] boxCaps = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text[] boxRights = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text[] boxTexts = Array.Empty<TMP_Text>();
        [SerializeField] RectTransform risk;
        [SerializeField] TMP_Text riskText;
        [SerializeField] RectTransform buttons;
        [SerializeField] DeskButton primary, secondary;
        [SerializeField] KeyChipView primaryKey, secondaryKey;
        [SerializeField] float width = 656, capHeight = 22, lineHeight = 22, gap = 12, boxHeight = 50, boxGap = 10, buttonHeight = 30;

        DeskPool<EventLineRow> rows;
        EventDocVM vm;

        public event Action<string> Invoked;

        void Awake()
        {
            if (lineTemplate != null) lineTemplate.gameObject.SetActive(false);
            primary.onClick.AddListener(() => Invoked?.Invoke(Command(0)));
            secondary.onClick.AddListener(() => Invoked?.Invoke(Command(1)));
        }

        string Command(int i) => vm != null && i < vm.Buttons.Count ? vm.Buttons[i].Command : null;

        /// <summary>Enter: birincil düğme; Esc: ikincil (varsa).</summary>
        public void Press(bool primaryButton) { var c = Command(primaryButton ? 0 : 1); if (c != null) Invoked?.Invoke(c); }

        static void Top(RectTransform r, float y) { r.anchoredPosition = new Vector2(r.anchoredPosition.x, -Mathf.Round(y)); }

        public float Bind(EventDocVM d)
        {
            vm = d;
            var T = DeskLoc.Instance;
            float y = 0;

            bool hasLines = d.Lines.Count > 0;
            DeskUi.SetActive(linesCap, hasLines && d.LinesHeader != null);
            DeskUi.SetActive(linesRight, hasLines && d.LinesHeader != null);
            DeskUi.SetActive(linesRule, hasLines && d.LinesHeader != null);
            if (hasLines && d.LinesHeader != null)
            {
                DeskUi.SetText(linesCap, T.T(d.LinesHeader.Title));
                DeskUi.SetText(linesRight, d.LinesHeader.Right);
                Top(linesCap.rectTransform, y); Top(linesRight.rectTransform, y);
                float capW = Mathf.Round(DeskUi.TextWidth(linesCap, linesCap.text)), rightW = Mathf.Round(DeskUi.TextWidth(linesRight, linesRight.text));
                linesRule.anchoredPosition = new Vector2(capW + 8, -Mathf.Round(y + 7));
                linesRule.sizeDelta = new Vector2(width - capW - rightW - 16, linesRule.sizeDelta.y);
                y += capHeight;
            }
            rows ??= new DeskPool<EventLineRow>(lineTemplate, lines);
            Top(lines, y);
            for (int i = 0; i < d.Lines.Count; i++)
            {
                var r = rows.Get(i);
                var lr = (RectTransform)r.transform;
                lr.anchorMin = lr.anchorMax = new Vector2(0, 1); lr.pivot = new Vector2(0, 1);
                lr.anchoredPosition = new Vector2(0, -i * lineHeight);
                lr.sizeDelta = new Vector2(width, lineHeight);
                r.Bind(d.Lines[i], d.Lines[i].Status != "threat", width, i == d.Lines.Count - 1);
            }
            rows.Trim(d.Lines.Count);
            if (hasLines) y += d.Lines.Count * lineHeight + gap;

            int nb = Mathf.Min(d.Boxes.Count, boxes.Length);
            float bw = nb > 0 ? (width - (nb - 1) * boxGap) / nb : 0;
            for (int i = 0; i < boxes.Length; i++)
            {
                bool on = i < nb;
                DeskUi.SetActive(boxes[i], on);
                if (!on) continue;
                var b = d.Boxes[i];
                boxes[i].anchoredPosition = new Vector2(Mathf.Round(i * (bw + boxGap)), -Mathf.Round(y));
                boxes[i].sizeDelta = new Vector2(Mathf.Round(bw), boxHeight);
                DeskUi.SetText(boxCaps[i], T.T(b.Cap));
                DeskUi.SetText(boxRights[i], T.T(b.Right));
                var row = b.Rows.Count > 0 ? b.Rows[0] : null;
                DeskUi.SetText(boxTexts[i], row == null ? "" : T.T(row.Label) + (string.IsNullOrEmpty(row.Value) ? "" : " <style=num>" + row.Value + "</style>"));
            }
            if (nb > 0) y += boxHeight + gap;

            bool hasRisk = !string.IsNullOrEmpty(d.Risk);
            DeskUi.SetActive(risk, hasRisk);
            if (hasRisk)
            {
                DeskUi.SetText(riskText, T.T(d.Risk));
                riskText.ForceMeshUpdate();
                float h = Mathf.Max(28, riskText.preferredHeight + 8);
                risk.sizeDelta = new Vector2(width, h);
                Top(risk, y);
                y += h + gap;
            }

            int nbtn = d.Buttons.Count;
            DeskUi.SetActive(buttons, nbtn > 0);
            if (nbtn > 0)
            {
                Top(buttons, y + 4);
                primary.SetLabel(T.T(d.Buttons[0].Label));
                primaryKey.Set(d.Buttons[0].Key ?? "Enter");
                DeskUi.SetActive(secondary, nbtn > 1);
                DeskUi.SetActive(secondaryKey, nbtn > 1);
                var pr = (RectTransform)primary.transform; var pk = (RectTransform)primaryKey.transform;
                float x = 0;
                if (nbtn > 1)
                {
                    secondary.SetLabel(T.T(d.Buttons[1].Label));
                    secondaryKey.Set(d.Buttons[1].Key ?? "Esc");
                    var sr = (RectTransform)secondary.transform; var sk = (RectTransform)secondaryKey.transform;
                    sr.anchoredPosition = new Vector2(0, 0);
                    x = sr.sizeDelta.x + 8;
                    sk.anchoredPosition = new Vector2(x, -6);
                    x += sk.sizeDelta.x + 8;
                }
                float kw = pk.sizeDelta.x;
                pr.anchoredPosition = new Vector2(x, 0);
                pr.sizeDelta = new Vector2(width - x - kw - 8, buttonHeight);
                pk.anchoredPosition = new Vector2(width - kw, -6);
                y += 4 + buttonHeight;
            }
            return y;
        }
    }
}
