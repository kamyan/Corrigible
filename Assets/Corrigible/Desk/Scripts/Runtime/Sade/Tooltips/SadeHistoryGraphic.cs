using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Gösterilen geçmişin alanı, çizgisi, olay kesikleri ve uç noktası; boşta iş ve bellek ayırma yok.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SadeHistoryGraphic : MaskableGraphic
    {
        [SerializeField] Sprite sprite;
        readonly List<float> values = new List<float>(32);
        readonly List<int> events = new List<int>(4);
        int fromDay;
        public override Texture mainTexture => sprite != null ? sprite.texture : s_WhiteTexture;

        public void Bind(IList<float> shownValues, int firstDay, IList<int> eventDays)
        {
            values.Clear(); events.Clear(); fromDay = firstDay;
            if (shownValues != null) for (int i = 0; i < shownValues.Count; i++) values.Add(shownValues[i]);
            if (eventDays != null) for (int i = 0; i < eventDays.Count; i++) events.Add(eventDays[i]);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (values.Count < 2) return;
            var r = rectTransform.rect;
            float min = values[0], max = min;
            for (int i = 1; i < values.Count; i++) { min = Mathf.Min(min, values[i]); max = Mathf.Max(max, values[i]); }
            float span = Mathf.Max(1, max - min), w = Mathf.Max(0, r.width - 3), h = Mathf.Max(0, r.height - 6);
            var uv = new Vector2(.5f, .5f);
            if (sprite != null)
            {
                var outer = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
                uv = new Vector2((outer.x + outer.z) * .5f, (outer.y + outer.w) * .5f);
            }
            var th = SadeTheme.Active;
            Color ink = th != null ? th.Get("ink2") : Color.black;
            Color red = DeskTheme.Active != null ? DeskTheme.Active.Get("sade.status.threat") : Color.red;
            Color paper = th != null ? th.Get("index") : Color.white;
            Color good = DeskTheme.Active != null ? DeskTheme.Active.Get("sade.status.good") : Color.green;
            Color area = red; area.a = .08f;
            Vector2 previous = Point(0);
            for (int i = 1; i < values.Count; i++)
            {
                Vector2 point = Point(i);
                Quad(vh, new Vector2(previous.x, r.yMin), previous, point, new Vector2(point.x, r.yMin), area, uv);
                previous = point;
            }
            Segment(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), 1, th != null ? th.Get("panels.dividerDotted") : ink, uv);
            foreach (int day in events)
            {
                float f = (day - fromDay) / (float)(values.Count - 1);
                if (f < 0 || f > 1) continue;
                float x = r.xMin + 1.5f + w * f;
                for (float y = r.yMin; y < r.yMax; y += 4)
                    Segment(vh, new Vector2(x, y), new Vector2(x, Mathf.Min(y + 2, r.yMax)), 1, good, uv);
            }
            previous = Point(0);
            for (int i = 1; i < values.Count; i++)
            {
                var point = Point(i); Segment(vh, previous, point, 1.8f, ink, uv); previous = point;
            }
            Disc(vh, previous, 5, paper, uv); Disc(vh, previous, 3.6f, red, uv);
            Vector2 Point(int i) => new Vector2(r.xMin + 1.5f + w * i / (values.Count - 1), r.yMin + 3 + h * (values[i] - min) / span);
        }

        static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color, Vector2 uv)
        {
            var n = new Vector2(-(b - a).y, (b - a).x).normalized * (width * .5f);
            Quad(vh, a - n, a + n, b + n, b - n, color, uv);
        }
        static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color, Vector2 uv)
        {
            int n = vh.currentVertCount;
            vh.AddVert(a, color, uv); vh.AddVert(b, color, uv); vh.AddVert(c, color, uv); vh.AddVert(d, color, uv);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
        static void Disc(VertexHelper vh, Vector2 center, float radius, Color color, Vector2 uv)
        {
            int n = vh.currentVertCount; vh.AddVert(center, color, uv);
            for (int i = 0; i <= 20; i++)
            {
                float a = i * Mathf.PI * 2 / 20;
                vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, color, uv);
                if (i > 0) vh.AddTriangle(n, n + i, n + i + 1);
            }
        }
    }
}
