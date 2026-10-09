using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Baskın tahtasının üstünde oklar: düşman niyetleri ve önizlemedeki itmeler. Koordinatlar kare (0–8) cinsinden.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RaidOverlayGraphic : MaskableGraphic
    {
        public struct Arrow { public Vector2 From, To; public Color Color; public bool Dashed; }

        readonly List<Arrow> arrows = new List<Arrow>();
        readonly List<(Rect r, Color c)> ghosts = new List<(Rect, Color)>();

        public void Clear() { arrows.Clear(); ghosts.Clear(); SetVerticesDirty(); }
        public void Add(Arrow a) { arrows.Add(a); SetVerticesDirty(); }
        public void Ghost(int x, int y, Color c) { ghosts.Add((new Rect(x + 0.15f, y + 0.15f, 0.7f, 0.7f), c)); SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float cell = r.width / 8f;
            Vector2 P(Vector2 p) => new Vector2(r.xMin + p.x * cell, r.yMax - p.y * cell);
            foreach (var a in arrows)
            {
                var from = P(a.From); var to = P(a.To);
                var dir = (to - from).normalized;
                var end = to - dir * cell * 0.18f;
                float w = Mathf.Max(2f, cell * 0.055f);
                if (a.Dashed) UiMesh.DashedLine(vh, from, end, w, a.Color, cell * 0.14f, cell * 0.1f);
                else UiMesh.Line(vh, from, end, w, a.Color);
                // Ok ucu
                var n = new Vector2(-dir.y, dir.x);
                float h = cell * 0.16f;
                int s = vh.currentVertCount;
                vh.AddVert(end + dir * h, a.Color, Vector4.zero);
                vh.AddVert(end + n * h * 0.6f, a.Color, Vector4.zero);
                vh.AddVert(end - n * h * 0.6f, a.Color, Vector4.zero);
                vh.AddTriangle(s, s + 1, s + 2);
            }
            foreach (var (gr, c) in ghosts)
            {
                var a = P(new Vector2(gr.xMin, gr.yMin)); var b = P(new Vector2(gr.xMax, gr.yMax));
                var rr = Rect.MinMaxRect(a.x, b.y, b.x, a.y);
                float w = Mathf.Max(1.5f, cell * 0.04f);
                UiMesh.DashedLine(vh, new Vector2(rr.xMin, rr.yMin), new Vector2(rr.xMax, rr.yMin), w, c, cell * 0.08f, cell * 0.06f);
                UiMesh.DashedLine(vh, new Vector2(rr.xMax, rr.yMin), new Vector2(rr.xMax, rr.yMax), w, c, cell * 0.08f, cell * 0.06f);
                UiMesh.DashedLine(vh, new Vector2(rr.xMax, rr.yMax), new Vector2(rr.xMin, rr.yMax), w, c, cell * 0.08f, cell * 0.06f);
                UiMesh.DashedLine(vh, new Vector2(rr.xMin, rr.yMax), new Vector2(rr.xMin, rr.yMin), w, c, cell * 0.08f, cell * 0.06f);
            }
        }
    }
}
