using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// HEDEF etiketi (PROMPT §A.6): kâğıt etiket, 36 px bayrak dairesi, “HEDEF” başlığı ve iki satır. Satır: ad (LF 600 12,
    /// sütun en az 62 px), 96×7 çubuk (Hizalama yeşil, Silme mavi), yüzde (CP 700 13, 30 px sağa yaslı). Satır 13, aralık 5.
    /// </summary>
    public sealed class GoalChipView : MonoBehaviour
    {
        [SerializeField] SadePaper paper;
        [SerializeField] IconCircleView icon;
        [SerializeField] RectTransform body;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text[] names = new TMP_Text[2];
        [SerializeField] ThresholdBarView[] bars = new ThresholdBarView[2];
        [SerializeField] TMP_Text[] values = new TMP_Text[2];

        [Header("Önizleme (edit mod; oyunda VM)")]
        [SerializeField] string sampleTitle = "HEDEF";
        [SerializeField] List<SadeGoalVM> sample = new List<SadeGoalVM>
        {
            new SadeGoalVM { Id = "alignment", Label = "Hizalama", Percent = 42, Color = "good" },
            new SadeGoalVM { Id = "deletion", Label = "Silme", Percent = 28, Color = "info" },
        };

        const float NameMin = 62, BarW = 96, ValueW = 30, InRowGap = 7, RowH = 13, RowGap = 5;
        bool bound;

        void OnEnable() { if (!bound) Render(sampleTitle, sample); }
        void OnValidate() => DeskUi.Deferred(this, () => { if (!bound) Render(sampleTitle, sample); });

        /// <summary>Önizleme verisini çizer (kurucu ve galeri).</summary>
        public void ShowSample() => Render(sampleTitle, sample);

        public void Bind(string titleText, IList<SadeGoalVM> goals)
        {
            bound = true;
            Render(titleText, goals);
        }

        void Render(string titleText, IList<SadeGoalVM> goals)
        {
            if (body == null || goals == null) return;
            var T = DeskLoc.Instance;
            if (icon != null) icon.Set("goal", false);
            SadeUi.Type(title, "chipLabel");
            DeskUi.Tint(title, "sade.ink2");
            DeskUi.SetText(title, T.T(titleText));

            // Ad sütunu: tasarımda 62 px; EN uzunsa en uzun ada göre genişler
            float nameW = NameMin;
            for (int i = 0; i < names.Length && i < goals.Count; i++)
            {
                SadeUi.Type(names[i], "goalName");
                nameW = Mathf.Max(nameW, Mathf.Ceil(SadeUi.Width(names[i], T.T(goals[i].Label))));
            }

            float bodyW = nameW + InRowGap + BarW + InRowGap + ValueW;
            for (int i = 0; i < names.Length; i++)
            {
                bool on = i < goals.Count;
                DeskUi.SetActive(names[i], on); DeskUi.SetActive(bars[i], on); DeskUi.SetActive(values[i], on);
                if (!on) continue;
                var g = goals[i];
                float y = 20 + i * (RowH + RowGap);   // başlık satırı 14 + 6
                DeskUi.Tint(names[i], "sade.ink");
                DeskUi.SetText(names[i], T.T(g.Label));
                SadeUi.Place(names[i], 0, y + 0.5f, nameW, 12);
                bars[i].SetFill(BarW, g.Percent, g.Color == "info" ? "sade.desk.bar.fill.deletion" : "sade.desk.bar.fill.alignment");
                SadeUi.Place(bars[i], nameW + InRowGap, y + 3, BarW, 7);
                SadeUi.Type(values[i], "goalValue");
                DeskUi.Tint(values[i], "sade.ink");
                values[i].alignment = TextAlignmentOptions.Right;
                DeskUi.SetText(values[i], DeskFormat.Percent(g.Percent, 0));
                SadeUi.Place(values[i], nameW + InRowGap + BarW + InRowGap, y, ValueW, RowH);
            }

            SadeUi.Place(title, 0, 1.5f, Mathf.Ceil(SadeUi.Width(title, title.text)), 11);
            // Blok: başlık 14 + 6 + iki satır (13 + 5 + 13) = 51; 58 px iç alana ortalı → üst boşluk 4,5
            SadeUi.Place(body, 57, 4.5f, bodyW, 51);
            var rt = (RectTransform)transform;
            float w = 57 + bodyW + 14 + 1;
            if (!Mathf.Approximately(rt.sizeDelta.x, w) || !Mathf.Approximately(rt.sizeDelta.y, 62)) rt.sizeDelta = new Vector2(w, 62);
            if (icon != null) SadeUi.Place(icon, 11, 13, 36, 36);
            if (paper != null) paper.Apply();
        }
    }
}
