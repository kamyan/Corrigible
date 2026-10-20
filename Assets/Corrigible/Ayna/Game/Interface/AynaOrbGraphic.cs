using Corrigible.Ayna.Core;
using Corrigible.Ayna.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Ayna.Game
{
    /// <summary>Bağımsız uGUI küresi: dönen ağ, arkadaki sönük teller ve öndeki beyaz ışık noktaları.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AynaOrbGraphic : MaskableGraphic
    {
        [SerializeField] int seed = 1;
        RegionVm region;
        bool selected;
        float nextFrame;
        public float Radius { get; private set; } = 35;

        protected override void Awake() { base.Awake(); raycastTarget = false; }

        public void Bind(RegionVm value, bool isSelected, int index)
        {
            region = value; selected = isSelected; seed = index + 1;
            Radius = 24 + 2.2f * (value?.Capacity ?? AynaWorldData.Regions[Mathf.Clamp(index, 0, 11)].Capacity);
            raycastTarget = false;
            SetVerticesDirty();
        }

        void LateUpdate()
        {
            if (AynaInterfaceMotion.Reduced || Time.unscaledTime < nextFrame) return;
            nextFrame = Time.unscaledTime + 1f / 20;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float r = Mathf.Min(Radius, Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / 2.6f);
            if (r < 1) return;
            bool unknown = region == null || region.Knowledge == Knowledge.Unknown && region.Own == 0;
            float own = region?.Own ?? 0, enemy = region == null ? 0 : (region.EnemyMin + region.EnemyMax) * .5f;
            bool empty = own + enemy == 0;
            float level = own > 0 && enemy == 0 ? 8 : own == 0 && enemy > 0 ? 92 : own + enemy > 0 ? 30 + 40 * enemy / (own + enemy) : 0;
            var gold = new Color32(255, 181, 71, 255); var blue = new Color32(82, 163, 255, 255);
            Color tint = unknown || empty ? new Color(.55f, .72f, .82f) : Color.Lerp(gold, blue, Mathf.InverseLerp(20, 80, level));
            float time = Application.isPlaying && !AynaInterfaceMotion.Reduced ? Time.unscaledTime : 3.8f;
            float angle = time * (unknown ? .14f : .19f) + seed * .47f;
            var center = rectTransform.rect.center;
            Glow(vh, center, r * 1.4f, AynaMapGraphic.Alpha(tint, .055f));
            Ring(vh, center, r * .99f, .45f, AynaMapGraphic.Alpha(tint, unknown ? .12f : .2f));

            int rows = unknown || empty ? 6 : own > 0 && enemy == 0 ? 9 : 7;
            const int columns = 15;
            var points = new Vector3[rows + 1, columns];
            for (int y = 0; y <= rows; y++)
            {
                float latitude = Mathf.Lerp(-1.42f, 1.42f, y / (float)rows);
                for (int x = 0; x < columns; x++)
                {
                    float longitude = x * Mathf.PI * 2 / columns + angle + Mathf.Sin(latitude * 2) * .16f;
                    var p = new Vector3(Mathf.Cos(latitude) * Mathf.Cos(longitude), Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Sin(longitude));
                    p = new Vector3(p.x, p.y * .94f + p.z * .17f, p.z * .94f - p.y * .17f);
                    points[y, x] = p;
                }
            }
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    var p = points[y, x]; var next = points[y, (x + 1) % columns];
                    DrawWire(vh, center, r, p, next, tint, unknown);
                    if (y < rows)
                    {
                        DrawWire(vh, center, r, p, points[y + 1, x], tint, unknown);
                        if ((x + y + seed) % 3 != 0 && !empty) DrawWire(vh, center, r, p, points[y + 1, (x + 1) % columns], tint, unknown);
                    }
                    float pulse = .65f + .35f * Mathf.Sin(time * 1.3f + x * 4.1f + y * 1.9f + seed);
                    if (p.z > -.18f)
                    {
                        var screen = center + new Vector2(p.x, p.y) * r;
                        float alpha = (.35f + Mathf.Max(0, p.z) * .65f) * pulse;
                        Glow(vh, screen, unknown ? 2.7f : 4, AynaMapGraphic.Alpha(tint, .17f * alpha));
                        float size = p.z > .6f ? 2.05f : 1.15f;
                        AynaMapGraphic.Quad(vh, screen - Vector2.one * size / 2, Vector2.one * size, AynaMapGraphic.Alpha(Color.Lerp(tint, Color.white, .85f), alpha));
                    }
                }
            // Altın çekirdeğin yörüngeleri ile AYNA'nın köşeli ağ silueti farklı okunur.
            if (!unknown && !empty && level < 35)
            {
                for (int orbit = 0; orbit < 3; orbit++)
                {
                    Vector2 last = Vector2.zero;
                    for (int p = 0; p <= 40; p++)
                    {
                        float a = p / 40f * Mathf.PI * 2;
                        var current = center + Rotate(new Vector2(Mathf.Cos(a) * r * (1.03f + orbit * .06f), Mathf.Sin(a) * r * .3f), .5f + orbit * .8f + angle * .1f);
                        if (p > 0) AynaMapGraphic.Line(vh, last, current, .65f, AynaMapGraphic.Alpha(gold, .25f));
                        last = current;
                    }
                }
            }
            if (region?.Core == true)
            {
                Ring(vh, center, r * 1.22f, .9f, AynaMapGraphic.Alpha(gold, .48f));
                for (int p = 0; p < 8; p++)
                {
                    float a = p * Mathf.PI / 4 + angle * .1f;
                    var direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    AynaMapGraphic.Line(vh, center + direction * r * 1.16f, center + direction * r * 1.32f, 1, gold);
                }
            }
            if (selected)
            {
                var cyan = new Color32(91, 228, 255, 255);
                float edge = r * 1.28f;
                for (int c = 0; c < 4; c++)
                {
                    var corner = new Vector2(c % 2 == 0 ? -edge : edge, c < 2 ? -edge : edge);
                    var inward = new Vector2(-Mathf.Sign(corner.x), -Mathf.Sign(corner.y));
                    AynaMapGraphic.Line(vh, center + corner, center + corner + new Vector2(inward.x * 13, 0), 1.7f, cyan);
                    AynaMapGraphic.Line(vh, center + corner, center + corner + new Vector2(0, inward.y * 13), 1.7f, cyan);
                }
            }
        }

        static void DrawWire(VertexHelper vh, Vector2 center, float radius, Vector3 a, Vector3 b, Color tint, bool unknown)
        {
            float front = Mathf.Clamp01((a.z + b.z) * .5f + .3f);
            float alpha = Mathf.Lerp(.055f, unknown ? .22f : .65f, front);
            var first = center + new Vector2(a.x, a.y) * radius; var second = center + new Vector2(b.x, b.y) * radius;
            if (front > .65f && !unknown) AynaMapGraphic.Line(vh, first, second, 2.6f, AynaMapGraphic.Alpha(tint, .09f));
            AynaMapGraphic.Line(vh, first, second, front > .45f ? .85f : .5f, AynaMapGraphic.Alpha(Color.Lerp(tint, Color.white, front * .45f), alpha));
        }

        static Vector2 Rotate(Vector2 point, float angle) => new Vector2(point.x * Mathf.Cos(angle) - point.y * Mathf.Sin(angle), point.x * Mathf.Sin(angle) + point.y * Mathf.Cos(angle));

        static void Ring(VertexHelper vh, Vector2 center, float radius, float width, Color color)
        {
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI / 32, b = (i + 1) * Mathf.PI / 32;
                AynaMapGraphic.Line(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, color);
            }
        }

        static void Glow(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            int first = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            color.a = 0;
            const int count = 14;
            for (int i = 0; i <= count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, color, Vector2.zero);
                if (i > 0) vh.AddTriangle(first, first + i, first + i + 1);
            }
        }
    }
}
