using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Son rapor şerit grafiği: diş bantlı kâğıt üstünde ızgara (veri ölçeğinden, doku döşeme değil), eksen yazıları, tek seri
    /// (Catmull-Rom), kırılma numaraları ve iğneleri, uç etiketi. Bütün geometri VM'den her bağlamada yeniden kurulur.
    /// Koordinatlar 1920×1080 tasarım pikseli (sol-üst); grafikler tasarım köküne gerili.
    /// </summary>
    public sealed class StripChartView : MonoBehaviour
    {
        [SerializeField] RectTransform root;
        [Header("Izgara ve eksenler")]
        [SerializeField] Image gridMinorTemplate, gridMajorTemplate;
        [SerializeField] TMP_Text yLabelTemplate, xLabelTemplate;
        [SerializeField] Image eventRule;
        [Header("Seri")]
        [SerializeField] UILineGraphic real;
        [Header("İşaretler")]
        [SerializeField] RectTransform markerTemplate;
        [Tooltip("Kırılma iğnesi (1 px kesik 2/3); tasarım köküne gerili.")]
        [SerializeField] UILineGraphic needleTemplate;
        [Tooltip("Uç etiketi: başlık ve değer.")]
        [SerializeField] TMP_Text[] endLabels = System.Array.Empty<TMP_Text>();
        [Header("Başlık ve dipnot")]
        [SerializeField] TMP_Text title, subtitle, footnote;
        [SerializeField] DeskRow headRow;
        [SerializeField] TMP_Text[] legendLabels = System.Array.Empty<TMP_Text>();
        [SerializeField] DeskRow[] legendItems = System.Array.Empty<DeskRow>();
        [SerializeField] DeskRow legendRow;
        [Tooltip("Izgara çizgilerinin CSS opaklığı (ince .32, kalın .55); Linear'da kâğıt üstü eşdeğerine çevrilir.")]
        [SerializeField] float minorAlpha = 0.32f, majorAlpha = 0.55f, paperBackground = 0.94f;

        DeskPool<Image> minors, majors;
        DeskPool<TMP_Text> yLabels, xLabels;
        DeskPool<RectTransform> markers;
        DeskPool<UILineGraphic> needles;
        static readonly List<Vector2> a = new List<Vector2>(512);

        public void Bind(ChartVM c)
        {
            if (c == null || !Application.isPlaying) return;
            var T = DeskLoc.Instance;
            minors ??= new DeskPool<Image>(gridMinorTemplate); majors ??= new DeskPool<Image>(gridMajorTemplate);
            yLabels ??= new DeskPool<TMP_Text>(yLabelTemplate); xLabels ??= new DeskPool<TMP_Text>(xLabelTemplate);
            markers ??= new DeskPool<RectTransform>(markerTemplate);
            needles ??= new DeskPool<UILineGraphic>(needleTemplate);
            var p = c.Plot ?? new ChartPlotVM();
            var g = c.Grid ?? new ChartGridVM();

            DeskUi.SetText(title, T.T(c.Title));
            DeskUi.SetText(subtitle, T.T(c.Subtitle));
            headRow.Layout();
            for (int i = 0; i < legendLabels.Length && i < c.Legend.Count; i++) { DeskUi.SetText(legendLabels[i], T.T(c.Legend[i])); legendItems[i].Layout(); }
            legendRow.Layout();
            DeskUi.SetText(footnote, T.T(c.Footnote));

            // ---- ızgara: ince (5 gün / %5), kalın (10 gün / %25); kalın çizgiyle çakışan ince çizilmez
            var gridInk = DeskTheme.Active.Get("ink.chartGridMinor");
            var gridInkMajor = DeskTheme.Active.Get("ink.chartGrid");
            gridInk.a = DeskMath.LinearOverlayAlpha(gridInk, minorAlpha, paperBackground);
            gridInkMajor.a = DeskMath.LinearOverlayAlpha(gridInkMajor, majorAlpha, paperBackground);
            int mi = 0, ma = 0;
            for (float d = 0; d <= p.DayMax + 1e-3f; d += g.MinorDays)
            {
                bool major = Mathf.Abs(d / g.MajorDays - Mathf.Round(d / g.MajorDays)) < 1e-3f;
                var img = major ? majors.Get(ma++) : minors.Get(mi++);
                Line(img, X(p, d), p.Y1, 1, p.Y0 - p.Y1, major ? gridInkMajor : gridInk, vertical: true);
            }
            for (float v = 0; v <= 100 + 1e-3f; v += g.MinorPct)
            {
                bool major = Mathf.Abs(v / g.MajorPct - Mathf.Round(v / g.MajorPct)) < 1e-3f;
                var img = major ? majors.Get(ma++) : minors.Get(mi++);
                Line(img, p.X0, Y(p, v), p.X1 - p.X0, 1, major ? gridInkMajor : gridInk, vertical: false);
            }
            minors.Trim(mi); majors.Trim(ma);

            // ---- eksen yazıları: y her kalın yüzdede (sağa yaslı, x0 − 10), x her kalın günde (ortalı)
            int yi = 0;
            for (float v = 0; v <= 100 + 1e-3f; v += g.MajorPct)
                PlaceText(yLabels.Get(yi++), DeskFormat.Percent(v, 0), p.X0 - 10, Y(p, v), TextAlignmentOptions.Right);
            yLabels.Trim(yi);
            int xi = 0;
            for (float d = 0; d <= p.DayMax - g.MajorDays / 2 + 1e-3f; d += g.MajorDays)
                PlaceText(xLabels.Get(xi++), DeskFormat.DayCode(Mathf.RoundToInt(d)), X(p, d), p.Y0 + 12, TextAlignmentOptions.Center);
            xLabels.Trim(xi);

            Line(eventRule, p.X0, c.EventRuleY, p.X1 - p.X0, 1, eventRule.color, false);

            var pts = c.Series?.Points;
            DeskChart.Smooth(p, pts, a); real.SetPointsTopLeft(a, Vector2.zero);

            // ---- kırılma numaraları: y = olay çizgisinin 11 px üstü; iğne olay çizgisinden eğrinin 4 px üstüne
            for (int i = 0; i < c.Events.Count; i++)
            {
                float d = c.Events[i];
                var m = markers.Get(i);
                float x = X(p, d);
                Place(m, x, c.EventRuleY - 11);
                DeskUi.Tint(m.Find("Disc").GetComponent<Image>(), "ink.ink");
                DeskUi.SetText(m.Find("N").GetComponent<TMP_Text>(), (i + 1).ToString());
                a.Clear();
                a.Add(new Vector2(x, c.EventRuleY)); a.Add(new Vector2(x, Y(p, DeskChart.ValueAt(pts, d)) - 4));
                needles.Get(i).SetPointsTopLeft(a, Vector2.zero);
            }
            markers.Trim(c.Events.Count); needles.Trim(c.Events.Count);

            // ---- uç etiketi: serinin sonu (başlık üstte, değer altta)
            if (pts != null && pts.Count > 0 && endLabels.Length >= 2 && c.EndLabels.Count >= 2)
            {
                var last = pts[pts.Count - 1];
                float ex = X(p, last[0]) + 10;
                SetEnd(endLabels[0], c.EndLabels[0], ex, Y(p, last[1]) + 4);
                SetEnd(endLabels[1], c.EndLabels[1], ex, Y(p, last[1]) + 19);
            }
        }

        static float X(ChartPlotVM p, float d) => DeskChart.X(p, d);
        static float Y(ChartPlotVM p, float v) => DeskChart.Y(p, v);

        /// <summary>İnce çizgi: mutlak tam piksele yaslı (Chrome gibi), sol-üst çapalı.</summary>
        static void Line(Image img, float x, float y, float w, float h, Color col, bool vertical)
        {
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            if (vertical) x = Mathf.Round(x - 0.5f); else y = Mathf.Round(y - 0.5f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            img.color = col;
        }

        static void Place(RectTransform r, float x, float y)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, -y);
        }

        /// <summary>Metni (x, y) noktasına (dikeyde ortalı) koyar; hizalama sol/orta/sağ.</summary>
        static void PlaceText(TMP_Text t, string text, float x, float y, TextAlignmentOptions align)
        {
            DeskUi.SetText(t, text);
            t.alignment = align;
            var r = t.rectTransform;
            float px = align == TextAlignmentOptions.Right ? 1 : align == TextAlignmentOptions.Center ? 0.5f : 0;
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(px, 0.5f);
            r.anchoredPosition = new Vector2(x, -y);
        }

        /// <summary>Uç etiketi: SVG taban çizgisi y; kutunun tabanı tabana göre (yazı boyunun ~%22'si alt boşluk).</summary>
        static void SetEnd(TMP_Text t, ChartEndLabelVM l, float x, float baseline)
        {
            DeskUi.SetText(t, DeskLoc.Instance.T(l.Text));
            if (!Mathf.Approximately(t.fontSize, l.Size)) t.fontSize = l.Size;
            ColorUtility.TryParseHtmlString(l.Color ?? "#1E1B16", out var col);
            t.color = col;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 0.5f);
            r.anchoredPosition = new Vector2(x, -(baseline - l.Size * 0.18f));
        }
    }
}
