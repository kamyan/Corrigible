using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Son raporun işgal şeridi (Revizyon 2 · S8): x = vardiya 1…N her zaman tam genişlik (kısa harekât sola sıkışmaz), y = işgal
    /// yüzdesi. Zemin geri alınan (mavi %10), çizginin altı işgal (kırmızı %22), çizgi 3 px, çerçeve 1,5 px. Karşı saldırılar
    /// çizgide yukarı çıkıntı olarak görünür. <see cref="Drawn"/> çizginin soldan sağa ne kadarının çizildiği (0 → 1).
    /// Özel shader yok: alan ve çizgi dörtgen zinciri (<see cref="OnPopulateMesh"/>).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OccupationChart : MaskableGraphic
    {
        [SerializeField, Min(0.5f)] float lineWidth = 3, frameWidth = 1.5f;

        // Renkler temadan (SetColors: report.chartLine, report.chartOccupied, report.chartRetaken, report.summaryLine)
        Color line = new Color32(0x9A, 0x1E, 0x14, 0xFF);
        Color occupied = new Color(0.604f, 0.118f, 0.078f, 0.22f);
        Color retaken = new Color(0.102f, 0.278f, 0.537f, 0.10f);
        Color frame = new Color32(0x46, 0x3F, 0x34, 0xFF);

        readonly List<Vector2> points = new List<Vector2>();
        int shifts = 1;
        float drawn = 1;

        /// <summary>Çizilen kısım (0 → 1, soldan).</summary>
        public float Drawn
        {
            get => drawn;
            set { value = Mathf.Clamp01(value); if (Mathf.Approximately(drawn, value)) return; drawn = value; SetVerticesDirty(); }
        }

        public void SetColors(Color lineColor, Color occupiedFill, Color retakenFill, Color frameColor)
        {
            line = lineColor; occupied = occupiedFill; retaken = retakenFill; frame = frameColor;
            SetVerticesDirty();
        }

        /// <summary>Seri: x vardiya (1…<paramref name="shiftCount"/>), y yüzde.</summary>
        public void SetSeries(IReadOnlyList<Vector2> series, int shiftCount)
        {
            points.Clear();
            if (series != null) points.AddRange(series);
            shifts = Mathf.Max(2, shiftCount);
            SetVerticesDirty();
        }

        /// <summary>Vardiya ve yüzdenin grafik içindeki yerel noktası (pivottan bağımsız, rect'in içinde).</summary>
        public Vector2 Local(float shift, float pct)
        {
            var r = rectTransform.rect;
            return new Vector2(r.xMin + r.width * (shift - 1) / (shifts - 1), r.yMin + r.height * Mathf.Clamp(pct, 0, 100) / 100f);
        }

        /// <summary>Serinin verilen vardiyadaki değeri (noktalar arası doğrusal).</summary>
        public float ValueAt(float shift)
        {
            if (points.Count == 0) return 0;
            if (shift <= points[0].x) return points[0].y;
            for (int i = 1; i < points.Count; i++)
                if (shift <= points[i].x)
                {
                    float t = Mathf.InverseLerp(points[i - 1].x, points[i].x, shift);
                    return Mathf.Lerp(points[i - 1].y, points[i].y, t);
                }
            return points[points.Count - 1].y;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (r.width <= 0 || r.height <= 0) return;
            Quad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin), retaken);
            if (points.Count >= 2)
            {
                float limit = r.xMin + r.width * drawn;
                var pts = new List<Vector2>();
                foreach (var p in points)
                {
                    var q = Local(p.x, p.y);
                    if (q.x <= limit) { pts.Add(q); continue; }
                    // Çizimin ucu iki nokta arasında: kesişim
                    if (pts.Count > 0)
                    {
                        var a = pts[pts.Count - 1];
                        pts.Add(Vector2.Lerp(a, q, Mathf.InverseLerp(a.x, q.x, limit)));
                    }
                    break;
                }
                for (int i = 1; i < pts.Count; i++)
                {
                    var a = pts[i - 1]; var b = pts[i];
                    Quad(vh, new Vector2(a.x, r.yMin), a, b, new Vector2(b.x, r.yMin), occupied);
                }
                for (int i = 1; i < pts.Count; i++) Segment(vh, pts[i - 1], pts[i], lineWidth, line);
                // Köşelerde boşluk kalmasın: ara noktalara küçük kare
                for (int i = 1; i < pts.Count - 1; i++) Square(vh, pts[i], lineWidth * 0.5f, line);
            }
            float f = frameWidth;
            Quad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMin + f), new Vector2(r.xMax, r.yMin + f), new Vector2(r.xMax, r.yMin), frame);
            Quad(vh, new Vector2(r.xMin, r.yMax - f), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMax - f), frame);
            Quad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMin + f, r.yMax), new Vector2(r.xMin + f, r.yMin), frame);
            Quad(vh, new Vector2(r.xMax - f, r.yMin), new Vector2(r.xMax - f, r.yMax), new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin), frame);
        }

        void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color col)
        {
            col *= color;
            int i = vh.currentVertCount;
            vh.AddVert(a, col, Vector2.zero); vh.AddVert(b, col, Vector2.zero); vh.AddVert(c, col, Vector2.zero); vh.AddVert(d, col, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color col)
        {
            var dir = b - a;
            if (dir.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-dir.y, dir.x).normalized * (width * 0.5f);
            Quad(vh, a - n, a + n, b + n, b - n, col);
        }

        void Square(VertexHelper vh, Vector2 c, float h, Color col) =>
            Quad(vh, c + new Vector2(-h, -h), c + new Vector2(-h, h), c + new Vector2(h, h), c + new Vector2(h, -h), col);
    }
}
