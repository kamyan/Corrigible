using Corrigible.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Bölgeler arası bağlantılar ve uydu hatları. Simülasyon yokken (edit mod) tüm bağlantıları nötr çizer.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapLinksGraphic : MaskableGraphic
    {
        [SerializeField] UiTheme theme;
        Simulation sim;

        Color Line2 => theme != null ? theme.line2 : Palette.Line2;

        public void Bind(Simulation s) { sim = s; SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float k = r.width / MapSpace.W;
            float w = Mathf.Max(1f, 1.6f * k);
            if (sim == null)
            {
                foreach (var l in GameData.Links) Link(vh, r, l.A, l.B, l.Wrap, Line2, w, 0, k);
                return;
            }
            var s = sim.S;
            bool ended = sim.EndRevealed;
            double Shown(RegionState x) => sim.Shown(x).V ?? 0;
            foreach (var l in GameData.Links)
            {
                var A = s.Region(l.A); var B = s.Region(l.B);
                bool q = (A.Q || B.Q) && !ended, off = (A.Off > 0 || B.Off > 0) && !ended;
                bool hot = Shown(A) > 15 && Shown(B) > 15;
                var c = hot ? Color.Lerp(Line2, (theme != null ? theme.threat : Palette.Threat), 0.55f) : Line2;
                if (off) c.a = 0.15f; else if (q) c.a = 0.5f;
                Link(vh, r, l.A, l.B, l.Wrap, c, w, q ? 6 * k : 0, k);
            }
            if (s.Has("uydu"))
                foreach (var l in GameData.Satellites)
                    Dotted(vh, r, l.A, l.B, l.Wrap, Palette.Alpha(theme != null ? theme.verify : Palette.Verify, 0.6f), k);
        }

        static void Link(VertexHelper vh, Rect r, string a, string b, bool wrap, Color c, float w, float dash, float k)
        {
            var pa = MapSpace.Pos(r, a); var pb = MapSpace.Pos(r, b);
            if (wrap)
            {
                // Pasifik: iki uç haritanın kenarına uzanır
                Seg(vh, pa, new Vector2(r.xMin, pa.y + 6 * k), w, c, dash);
                Seg(vh, pb, new Vector2(r.xMax, pb.y - 6 * k), w, c, dash);
            }
            else Seg(vh, pa, pb, w, c, dash);
        }

        static void Seg(VertexHelper vh, Vector2 a, Vector2 b, float w, Color c, float dash)
        {
            if (dash <= 0) UiMesh.Line(vh, a, b, w, c);
            else UiMesh.DashedLine(vh, a, b, w, c, dash * 0.6f, dash);
        }

        static void Dotted(VertexHelper vh, Rect r, string a, string b, bool wrap, Color c, float k)
        {
            var pa = MapSpace.Pos(r, a); var pb = MapSpace.Pos(r, b);
            void Dots(Vector2 x, Vector2 y)
            {
                float len = Vector2.Distance(x, y);
                for (float t = 0; t < len; t += 7 * k) UiMesh.Disc(vh, Vector2.Lerp(x, y, t / len), 0.9f * k, c, 6);
            }
            if (wrap) { Dots(pa, new Vector2(r.xMin, pa.y + 6 * k)); Dots(pb, new Vector2(r.xMax, pb.y - 6 * k)); }
            else Dots(pa, pb);
        }
    }
}
