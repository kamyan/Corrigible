using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    public enum LineCurve { Polyline, Quadratic, Cubic }

    /// <summary>
    /// Çizgi Graphic'i (PROMPT §4.5): çoklu çizgi, karesel ya da kübik Bézier; kalınlık, kesik deseni (px), yuvarlak uç.
    /// Doku olarak atlas'taki <c>sh_fill_r0</c>'ın merkez UV'si kullanılır; böylece diğer UI ile aynı batch'te kalır.
    /// Noktalar yerel uzaydadır (y yukarı); <see cref="SetPointsTopLeft"/> tasarım (sol-üst, y aşağı) koordinatı alır.
    /// Karesel eğride noktalar p0, c1, p1, c2, p2…; kübikte p0, c1a, c1b, p1, c2a, c2b, p2….
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UILineGraphic : MaskableGraphic
    {
        [SerializeField] Sprite sprite;
        [SerializeField] LineCurve curve;
        [SerializeField] List<Vector2> points = new List<Vector2> { new Vector2(0, 0), new Vector2(100, 0) };
        [SerializeField] float thickness = 1.5f;
        [Tooltip("Açık/kapalı uzunluklar (px); boşsa düz çizgi.")]
        [SerializeField] float[] dash = Array.Empty<float>();
        [SerializeField] float dashPhase;
        [SerializeField] bool roundCap;
        [Tooltip("Eğri bölme hassasiyeti (px): küçüldükçe daha çok parça.")]
        [SerializeField] float tolerance = 0.35f;

        // Paylaşılan tamponlar: her yeniden çizimde bellek ayırma yok
        static readonly List<Vector2> flat = new List<Vector2>(256);
        static readonly List<Vector2> piece = new List<Vector2>(256);

        public float Thickness { get => thickness; set { if (thickness != value) { thickness = value; SetVerticesDirty(); } } }
        public LineCurve Curve { get => curve; set { if (curve != value) { curve = value; SetVerticesDirty(); } } }

        public override Texture mainTexture => sprite != null ? sprite.texture : s_WhiteTexture;

        public void SetDash(float[] pattern, float phase = 0)
        {
            dash = pattern ?? Array.Empty<float>(); dashPhase = phase; SetVerticesDirty();
        }

        public void SetRoundCap(bool on) { roundCap = on; SetVerticesDirty(); }

        /// <summary>Yerel noktalar (y yukarı).</summary>
        public void SetPoints(IList<Vector2> pts)
        {
            points.Clear();
            for (int i = 0; i < pts.Count; i++) points.Add(pts[i]);
            SetVerticesDirty();
        }

        /// <summary>Tasarım koordinatları (ekran pikseli, sol-üst orijin, y aşağı) → bu dikdörtgenin sol-üstüne göre yerel nokta.</summary>
        public void SetPointsTopLeft(IList<Vector2> designPts, Vector2 rectTopLeftDesign)
        {
            points.Clear();
            var r = rectTransform.rect;
            for (int i = 0; i < designPts.Count; i++)
            {
                var d = designPts[i] - rectTopLeftDesign;
                points.Add(new Vector2(r.xMin + d.x, r.yMax - d.y));
            }
            SetVerticesDirty();
        }

        Vector2 UV()
        {
            if (sprite == null) return new Vector2(0.5f, 0.5f);
            var o = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            return new Vector2((o.x + o.z) * 0.5f, (o.y + o.w) * 0.5f);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points.Count < 2 || thickness <= 0) return;
            Flatten();
            if (flat.Count < 2) return;
            var uv = UV();
            if (dash == null || dash.Length < 2) Strip(vh, flat, uv);
            else Dashed(vh, uv);
        }

        // ------------------------------------------------------------------ eğri → çoklu çizgi
        void Flatten()
        {
            flat.Clear();
            switch (curve)
            {
                case LineCurve.Quadratic:
                    flat.Add(points[0]);
                    for (int i = 0; i + 2 < points.Count; i += 2) Quad(points[i], points[i + 1], points[i + 2]);
                    break;
                case LineCurve.Cubic:
                    flat.Add(points[0]);
                    for (int i = 0; i + 3 < points.Count; i += 3) Cubic(points[i], points[i + 1], points[i + 2], points[i + 3]);
                    break;
                default:
                    for (int i = 0; i < points.Count; i++) flat.Add(points[i]);
                    break;
            }
        }

        int Segments(float approxLen) => Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(approxLen / Mathf.Max(0.05f, tolerance))), 2, 96);

        void Quad(Vector2 a, Vector2 c, Vector2 b)
        {
            int n = Segments(Vector2.Distance(a, c) + Vector2.Distance(c, b));
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n, u = 1 - t;
                flat.Add(u * u * a + 2 * u * t * c + t * t * b);
            }
        }

        void Cubic(Vector2 a, Vector2 c1, Vector2 c2, Vector2 b)
        {
            int n = Segments(Vector2.Distance(a, c1) + Vector2.Distance(c1, c2) + Vector2.Distance(c2, b));
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n, u = 1 - t;
                flat.Add(u * u * u * a + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * b);
            }
        }

        // ------------------------------------------------------------------ kesik desen
        void Dashed(VertexHelper vh, Vector2 uv)
        {
            float total = 0;
            for (int i = 0; i < dash.Length; i++) total += Mathf.Max(0.01f, dash[i]);
            int di = 0;
            float left = 0, phase = dashPhase % total;
            if (phase < 0) phase += total;
            // Faz kadar desende ilerle
            while (true)
            {
                float d = Mathf.Max(0.01f, dash[di]);
                if (phase < d) { left = d - phase; break; }
                phase -= d; di = (di + 1) % dash.Length;
            }
            bool on = di % 2 == 0;
            piece.Clear();
            if (on) piece.Add(flat[0]);
            for (int i = 0; i + 1 < flat.Count; i++)
            {
                Vector2 a = flat[i], b = flat[i + 1];
                float seg = Vector2.Distance(a, b), pos = 0;
                while (seg - pos > left)
                {
                    pos += left;
                    var p = Vector2.Lerp(a, b, pos / seg);
                    if (on) { piece.Add(p); Strip(vh, piece, uv); piece.Clear(); }
                    else { piece.Clear(); piece.Add(p); }
                    on = !on;
                    di = (di + 1) % dash.Length;
                    left = Mathf.Max(0.01f, dash[di]);
                }
                left -= seg - pos;
                if (on) piece.Add(b);
            }
            if (on && piece.Count >= 2) Strip(vh, piece, uv);
        }

        // ------------------------------------------------------------------ şerit + uçlar
        void Strip(VertexHelper vh, List<Vector2> pts, Vector2 uv)
        {
            int n = pts.Count;
            if (n < 2) return;
            float hw = thickness * 0.5f;
            var col = (Color32)color;
            int start = vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                Vector2 dirPrev = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                Vector2 dirNext = i < n - 1 ? (pts[i + 1] - pts[i]).normalized : dirPrev;
                Vector2 tangent = (dirPrev + dirNext);
                tangent = tangent.sqrMagnitude < 1e-6f ? dirNext : tangent.normalized;
                var normal = new Vector2(-tangent.y, tangent.x);
                var segNormal = new Vector2(-dirNext.y, dirNext.x);
                float miter = Mathf.Clamp(1f / Mathf.Max(0.25f, Vector2.Dot(normal, segNormal)), 1f, 2.5f);
                vh.AddVert(pts[i] + normal * hw * miter, col, uv);
                vh.AddVert(pts[i] - normal * hw * miter, col, uv);
            }
            for (int i = 0; i < n - 1; i++)
            {
                int a = start + i * 2;
                vh.AddTriangle(a, a + 1, a + 3);
                vh.AddTriangle(a, a + 3, a + 2);
            }
            if (!roundCap) return;
            Cap(vh, pts[0], (pts[0] - pts[1]).normalized, hw, col, uv);
            Cap(vh, pts[n - 1], (pts[n - 1] - pts[n - 2]).normalized, hw, col, uv);
        }

        static void Cap(VertexHelper vh, Vector2 c, Vector2 outward, float r, Color32 col, Vector2 uv)
        {
            const int steps = 6;
            int center = vh.currentVertCount;
            vh.AddVert(c, col, uv);
            float baseAng = Mathf.Atan2(outward.y, outward.x);
            for (int i = 0; i <= steps; i++)
            {
                float a = baseAng - Mathf.PI / 2 + Mathf.PI * i / steps;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col, uv);
                if (i > 0) vh.AddTriangle(center, center + i, center + i + 1);
            }
        }
    }
}
