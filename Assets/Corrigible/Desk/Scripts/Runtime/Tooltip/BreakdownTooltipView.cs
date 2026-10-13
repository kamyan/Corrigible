using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sayaç döküm fişi (C5 “gösterge · döküm ipucu”, 420 px): başlık + not + güven işareti, kesik çizgi, kalemler (değer
    /// renkli), toplam satırı, 30 günlük şerit grafik. Kâğıt göstergenin güven türüdür (RAPOR → karbon).
    /// </summary>
    public sealed class BreakdownTooltipView : MonoBehaviour
    {
        [SerializeField] PaperPanel paper;
        [SerializeField] DeskRow headRow;
        [SerializeField] TMP_Text title, note;
        [SerializeField] TrustMarkView mark;
        [SerializeField] RectTransform rowsRoot;
        [SerializeField] RectTransform rowTemplate;
        [SerializeField] RectTransform sumRoot;
        [SerializeField] TMP_Text total, totalRight;
        [SerializeField] RectTransform chart;
        [SerializeField] UILineGraphic series;
        [SerializeField] float rowHeight = 22;

        DeskPool<RectTransform> rows;
        static readonly List<Vector2> pts = new List<Vector2>(32);

        public void Bind(BreakdownVM b)
        {
            var T = DeskLoc.Instance;
            var trust = DeskUi.ParseTrust(b.Trust);
            paper.SetKind(trust == TrustLevel.Report ? PaperKind.Carbon : PaperKind.Page);
            DeskUi.SetText(title, T.T(b.Title));
            DeskUi.SetText(note, T.T(b.Note));
            mark.Set(trust, trust != TrustLevel.Verified);
            headRow.Layout();

            rows ??= new DeskPool<RectTransform>(rowTemplate, rowsRoot);
            for (int i = 0; i < b.Items.Count; i++)
            {
                var r = rows.Get(i);
                r.anchoredPosition = new Vector2(0, -rowHeight * i);
                var t = r.GetComponentsInChildren<TMP_Text>(true);
                DeskUi.SetText(t[0], T.T(b.Items[i].Label));
                DeskUi.SetText(t[1], b.Items[i].Value);
                DeskUi.Tint(t[1], DeskUi.StatusKey(DeskUi.ParseStatus(b.Items[i].Status)));
            }
            rows.Trim(b.Items.Count);

            float y = rowHeight * b.Items.Count + 4;
            sumRoot.anchoredPosition = new Vector2(sumRoot.anchoredPosition.x, rowsRoot.anchoredPosition.y - y);
            DeskUi.SetText(total, T.T(b.Total));
            DeskUi.SetText(totalRight, T.T(b.TotalRight));
            chart.anchoredPosition = new Vector2(chart.anchoredPosition.x, sumRoot.anchoredPosition.y - 24.5f - 8);

            // Şerit: 30 gün, 6…394 px arası; değer 0–100 aralığında tepe/dip arasına ölçeklenir
            pts.Clear();
            if (b.Series != null && b.Series.Length > 1)
            {
                float mn = float.MaxValue, mx = float.MinValue;
                foreach (var v in b.Series) { mn = Mathf.Min(mn, v); mx = Mathf.Max(mx, v); }
                float span = Mathf.Max(1, mx - mn);
                var cr = series.rectTransform.rect;   // noktalar çizginin kendi uzayında (grafiğe gerilmiş, pivot ortada)
                for (int i = 0; i < b.Series.Length; i++)
                {
                    float x = 6 + (cr.width - 12) * i / (b.Series.Length - 1);
                    float yy = 12 + (cr.height - 24) * (1 - (b.Series[i] - mn) / span);
                    pts.Add(new Vector2(cr.xMin + x, cr.yMax - yy));
                }
            }
            series.SetPoints(pts);

            var rt = (RectTransform)transform;
            float h = -(chart.anchoredPosition.y) + chart.rect.height + 10;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
        }
    }
}
