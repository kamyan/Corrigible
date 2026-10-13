using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Şerit grafik geometrisi (PROMPT §7.4, C4): gün/değer → ekran pikseli, doğrusal ara değer, Catmull-Rom → kübik Bézier
    /// yumuşatma ve iki seri arası bant poligonu. Referans SVG'yle aynı formüller (testlerle sabitlenir).
    /// Ekran koordinatı sol-üst orijinli, y aşağı.
    /// </summary>
    public static class DeskChart
    {
        public static float X(ChartPlotVM p, float day) => p.X0 + (p.X1 - p.X0) * day / p.DayMax;
        public static float Y(ChartPlotVM p, float value) => p.Y0 - (p.Y0 - p.Y1) * value / 100f;

        /// <summary>Serinin verilen gündeki değeri (noktalar arası doğrusal; uçların dışında uç değer).</summary>
        public static float ValueAt(List<float[]> pts, float day)
        {
            if (pts == null || pts.Count == 0) return 0;
            if (day <= pts[0][0]) return pts[0][1];
            for (int i = 1; i < pts.Count; i++)
            {
                if (day > pts[i][0]) continue;
                float t = (day - pts[i - 1][0]) / Mathf.Max(1e-5f, pts[i][0] - pts[i - 1][0]);
                return Mathf.Lerp(pts[i - 1][1], pts[i][1], t);
            }
            return pts[pts.Count - 1][1];
        }

        /// <summary>
        /// Tekdüze Catmull-Rom → kübik Bézier (uçlarda nokta ikilenir): c1 = Pᵢ + (Pᵢ₊₁ − Pᵢ₋₁)/6, c2 = Pᵢ₊₁ − (Pᵢ₊₂ − Pᵢ)/6;
        /// kontrol noktalarının y'si çizim alanına (Y1…Y0) kırpılır. Her segment <paramref name="steps"/> parçaya örneklenir.
        /// </summary>
        public static void Smooth(ChartPlotVM p, List<float[]> pts, List<Vector2> result, int steps = 12)
        {
            result.Clear();
            int n = pts?.Count ?? 0;
            if (n == 0) return;
            Vector2 P(int i)
            {
                i = Mathf.Clamp(i, 0, n - 1);
                return new Vector2(X(p, pts[i][0]), Y(p, pts[i][1]));
            }
            float yMin = Mathf.Min(p.Y0, p.Y1), yMax = Mathf.Max(p.Y0, p.Y1);
            result.Add(P(0));
            for (int i = 0; i < n - 1; i++)
            {
                Vector2 p0 = P(i - 1), p1 = P(i), p2 = P(i + 1), p3 = P(i + 2);
                var c1 = p1 + (p2 - p0) / 6f;
                var c2 = p2 - (p3 - p1) / 6f;
                c1.y = Mathf.Clamp(c1.y, yMin, yMax);
                c2.y = Mathf.Clamp(c2.y, yMin, yMax);
                for (int s = 1; s <= steps; s++)
                {
                    float t = s / (float)steps, u = 1 - t;
                    result.Add(u * u * u * p1 + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * p2);
                }
            }
        }

        /// <summary>
        /// İki seri arası bant (referans: 0,5 günlük adımlarla doğrusal ara değer): <paramref name="upper"/> ve
        /// <paramref name="lower"/> aynı uzunlukta ekran noktalarıdır (şerit poligonu).
        /// </summary>
        public static void Band(ChartPlotVM p, List<float[]> a, List<float[]> b, float from, float to,
            List<Vector2> upper, List<Vector2> lower, float step = 0.5f)
        {
            upper.Clear(); lower.Clear();
            if (to <= from) return;
            for (float d = from; d <= to + 1e-4f; d += step)
            {
                float day = Mathf.Min(d, to);
                float x = X(p, day);
                upper.Add(new Vector2(x, Y(p, ValueAt(a, day))));
                lower.Add(new Vector2(x, Y(p, ValueAt(b, day))));
            }
        }
    }
}
