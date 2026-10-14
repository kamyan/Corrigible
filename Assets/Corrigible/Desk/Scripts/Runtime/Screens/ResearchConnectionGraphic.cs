using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Ön koşul kartlarının arasında tek uGUI mesh ile çizilen iplikler ve uç raptiyeleri.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ResearchConnectionGraphic : MaskableGraphic
    {
        public struct Link
        {
            public RectTransform From, To;
            public bool Complete, Satisfied, Running;
        }

        [SerializeField, Min(0.5f)] float lineWidth = 2;
        [SerializeField, Min(1)] float dashLength = 6;
        [SerializeField, Min(1)] float gapLength = 4;
        readonly List<Link> links = new List<Link>();
        Vector2[] lastPoints = new Vector2[0];

        public void SetLinks(IReadOnlyList<Link> values)
        {
            links.Clear();
            if (values != null) for (int i = 0; i < values.Count; i++) links.Add(values[i]);
            if (lastPoints.Length != links.Count * 2) lastPoints = new Vector2[links.Count * 2];
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable(); raycastTarget = false;
            DeskTheme.Changed += SetVerticesDirty;
        }

        protected override void OnDisable()
        {
            DeskTheme.Changed -= SetVerticesDirty;
            base.OnDisable();
        }

        void LateUpdate()
        {
            bool changed = false;
            for (int i = 0; i < links.Count; i++)
            {
                var link = links[i];
                if (link.From == null || link.To == null) continue;
                var a = Point(link.From, true); var b = Point(link.To, false);
                if ((a - lastPoints[i * 2]).sqrMagnitude > 0.01f || (b - lastPoints[i * 2 + 1]).sqrMagnitude > 0.01f) changed = true;
                lastPoints[i * 2] = a; lastPoints[i * 2 + 1] = b;
            }
            if (changed) SetVerticesDirty();
        }

        Vector2 Point(RectTransform card, bool right)
        {
            var r = card.rect;
            return rectTransform.InverseTransformPoint(card.TransformPoint(new Vector3(right ? r.xMax - 2 : r.xMin + 2, r.center.y)));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var theme = DeskTheme.Active;
            if (theme == null) return;
            foreach (var link in links)
            {
                if (link.From == null || link.To == null || !link.From.gameObject.activeInHierarchy || !link.To.gameObject.activeInHierarchy) continue;
                var a = Point(link.From, true); var b = Point(link.To, false);
                var ink = theme.Get(link.Complete ? "ink.green" : link.Running || link.Satisfied ? "ink.blue" : "ink.thread");
                if (!link.Satisfied && !link.Complete && !link.Running) ink.a *= 0.5f;
                var shadow = theme.Get("ink.graphite"); shadow.a *= 0.2f;
                DrawCurve(vh, a + new Vector2(1.5f, -2), b + new Vector2(1.5f, -2), lineWidth + 0.6f, shadow, !link.Satisfied);
                DrawCurve(vh, a, b, lineWidth, ink, !link.Satisfied && !link.Complete);
                var pin = theme.Get(link.Complete ? "ink.green" : "ink.pin");
                Pin(vh, a, 3.5f, pin); Pin(vh, b, 3.5f, pin);
            }
        }

        void DrawCurve(VertexHelper vh, Vector2 a, Vector2 b, float width, Color ink, bool dashed)
        {
            float reach = Mathf.Max(14, Mathf.Abs(b.x - a.x) * 0.4f);
            var c1 = a + Vector2.right * reach; var c2 = b - Vector2.right * reach;
            var previous = a;
            float traveled = 0;
            for (int i = 1; i <= 24; i++)
            {
                float t = i / 24f, u = 1 - t;
                var next = u * u * u * a + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * b;
                if (!dashed) Segment(vh, previous, next, width, ink);
                else
                {
                    float length = Vector2.Distance(previous, next), at = 0, period = Mathf.Max(2, dashLength + gapLength);
                    while (at < length - 0.001f)
                    {
                        float phase = (traveled + at) % period;
                        bool on = phase < dashLength;
                        float step = Mathf.Min(length - at, Mathf.Max(0.01f, (on ? dashLength : period) - phase));
                        if (on) Segment(vh, Vector2.Lerp(previous, next, at / length), Vector2.Lerp(previous, next, (at + step) / length), width, ink);
                        at += step;
                    }
                }
                traveled += Vector2.Distance(previous, next);
                previous = next;
            }
        }

        static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color ink)
        {
            if ((b - a).sqrMagnitude < 0.0001f) return;
            var direction = (b - a).normalized;
            var normal = new Vector2(-direction.y, direction.x) * width * 0.5f;
            int start = vh.currentVertCount;
            vh.AddVert(a - normal, ink, Vector2.zero); vh.AddVert(a + normal, ink, Vector2.zero);
            vh.AddVert(b + normal, ink, Vector2.zero); vh.AddVert(b - normal, ink, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }

        static void Pin(VertexHelper vh, Vector2 center, float radius, Color ink)
        {
            int start = vh.currentVertCount;
            vh.AddVert(center, ink, Vector2.zero);
            for (int i = 0; i <= 10; i++)
            {
                float angle = i * Mathf.PI * 2 / 10;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, ink, Vector2.zero);
                if (i > 0) vh.AddTriangle(start, start + i, start + i + 1);
            }
        }
    }
}
