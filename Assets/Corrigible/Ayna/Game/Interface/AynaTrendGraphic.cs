using System;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Ayna.Game
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AynaTrendGraphic : MaskableGraphic
    {
        [SerializeField] Color gridColor = new Color(.15f, .34f, .45f, .4f), lineColor = new Color(.5f, .72f, 1f, 1f), fillColor = new Color(.15f, .4f, .85f, .13f);
        [SerializeField] float lineWidth = 2;
        double[] values = Array.Empty<double>();
        public void SetValues(double[] source) { values = source == null ? Array.Empty<double>() : (double[])source.Clone(); SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect(); float inset = Mathf.Max(4, lineWidth * 2); r = new Rect(r.xMin + inset, r.yMin + inset, Mathf.Max(1, r.width - 2 * inset), Mathf.Max(1, r.height - 2 * inset));
            for (int i = 0; i <= 4; i++) Line(vh, new Vector2(r.xMin, r.yMin + r.height * i / 4), new Vector2(r.xMax, r.yMin + r.height * i / 4), 1, gridColor);
            for (int i = 1; i < values.Length; i++)
            {
                Vector2 a = Point(r, i - 1), b = Point(r, i); int start = vh.currentVertCount;
                vh.AddVert(new Vector3(a.x, r.yMin), fillColor, Vector2.zero); vh.AddVert(a, fillColor, Vector2.zero); vh.AddVert(b, fillColor, Vector2.zero); vh.AddVert(new Vector3(b.x, r.yMin), fillColor, Vector2.zero); vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
                Line(vh, a, b, lineWidth, lineColor);
            }
            for (int i = 0; i < values.Length; i++) { Vector2 p = Point(r, i); Quad(vh, new Rect(p.x - 2, p.y - 2, 4, 4), lineColor); }
        }
        Vector2 Point(Rect r, int index) => new Vector2(r.xMin + r.width * index / Math.Max(1, values.Length - 1), r.yMin + r.height * Mathf.Clamp01((float)values[index] / 100f));
        static void Quad(VertexHelper vh, Rect r, Color c) { int i = vh.currentVertCount; vh.AddVert(new Vector3(r.xMin, r.yMin), c, Vector2.zero); vh.AddVert(new Vector3(r.xMin, r.yMax), c, Vector2.zero); vh.AddVert(new Vector3(r.xMax, r.yMax), c, Vector2.zero); vh.AddVert(new Vector3(r.xMax, r.yMin), c, Vector2.zero); vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3); }
        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c) { Vector2 d = (b - a).normalized; Vector2 n = new Vector2(-d.y, d.x) * width / 2; int i = vh.currentVertCount; vh.AddVert(a - n, c, Vector2.zero); vh.AddVert(a + n, c, Vector2.zero); vh.AddVert(b + n, c, Vector2.zero); vh.AddVert(b - n, c, Vector2.zero); vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3); }
    }
}
