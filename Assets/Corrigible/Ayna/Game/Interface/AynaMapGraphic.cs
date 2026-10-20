using System.Collections.Generic;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Ayna.Game
{
    /// <summary>Yalnız oyuncuya gösterilen bölge bilgisini coğrafi nokta boyasına ve gerçek bağlantı ağına çevirir.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AynaMapGraphic : MaskableGraphic
    {
        static readonly Color Cyan = new Color32(91, 228, 255, 255);
        static readonly Color Gold = new Color32(255, 181, 71, 255);
        static readonly Color Blue = new Color32(61, 139, 255, 255);
        static readonly Color Purple = new Color32(179, 145, 245, 255);
        static Vector2[][] landDots;
        RegionVm[] regions;
        Vector2[] markers;
        string selected;

        protected override void Awake() { base.Awake(); raycastTarget = false; }

        public void Bind(RegionVm[] values, string selectedRegion, Vector2[] markerPositions)
        {
            regions = values;
            selected = selectedRegion;
            markers = markerPositions;
            raycastTarget = false;
            SetVerticesDirty();
        }

        public void SetSelection(string region)
        {
            if (selected == region) return;
            selected = region;
            SetVerticesDirty();
        }

        public static float CoverScale(Vector2 size) => Mathf.Max(size.x / 1920f, size.y / 1080f);

        public static Vector2 Project(Vector2 designPoint, Vector2 size) =>
            size * .5f + (designPoint - new Vector2(960, 540)) * CoverScale(size);

        Vector2 Local(Vector2 point)
        {
            var rect = rectTransform.rect;
            var projected = Project(point, rect.size);
            return new Vector2(rect.xMin + projected.x, rect.yMax - projected.y);
        }

        Vector2 Marker(int index)
        {
            var rect = rectTransform.rect;
            var p = markers != null && index < markers.Length ? markers[index] : Project(AynaMapView.DesignPoints[index], rect.size);
            return new Vector2(rect.xMin + p.x, rect.yMax - p.y);
        }

        RegionVm Region(int index)
        {
            if (regions == null) return null;
            string id = AynaWorldData.Regions[index].Id;
            foreach (var region in regions) if (region != null && region.Id == id) return region;
            return null;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            if (rect.width < 1 || rect.height < 1) return;
            Quad(vh, new Vector2(rect.xMin, rect.yMin), rect.size, new Color32(2, 9, 17, 255));
            // Deniz taraması sabit aralıklı kalır; harita kaplar, işaretler ölçeklenmez.
            for (float y = 0; y < rect.height; y += 4)
                Quad(vh, new Vector2(rect.xMin, rect.yMax - y), new Vector2(rect.width, .6f), new Color(0.1f, .44f, .59f, .014f));
            for (float x = 10; x < rect.width; x += 34)
                for (float y = 12; y < rect.height; y += 34)
                    Quad(vh, new Vector2(rect.xMin + x, rect.yMax - y), Vector2.one, new Color(.35f, .7f, .8f, .07f));

            EnsureLandDots();
            var contours = AynaMapGeometry.Contours;
            for (int i = 0; i < contours.Length; i++)
            {
                var region = Region(i);
                bool highlighted = selected == AynaWorldData.Regions[i].Id;
                bool unknown = region == null || region.Knowledge == Knowledge.Unknown;
                float enemy = region == null ? 0 : (region.EnemyMin + region.EnemyMax) * .5f;
                float own = region?.Own ?? 0;
                float enemyShare = own + enemy > 0 ? enemy / (own + enemy) : 0;
                var stroke = highlighted ? Cyan : new Color(.28f, .73f, .88f, .24f);
                foreach (var path in contours[i])
                {
                    for (int p = 1; p < path.Length; p++)
                    {
                        var a = Local(path[p - 1]); var b = Local(path[p]);
                        if (highlighted || p % 3 == 0) Line(vh, a, b, highlighted ? 4.2f : 3.5f, Alpha(Cyan, highlighted ? .14f : .045f));
                        Line(vh, a, b, highlighted ? 1.5f : .6f, stroke);
                    }
                }
                var dots = landDots[i];
                for (int d = 0; d < dots.Length; d++)
                {
                    var p = Local(dots[d]);
                    if (unknown && own == 0)
                    {
                        Line(vh, p + new Vector2(-2.6f, -2.6f), p + new Vector2(2.6f, 2.6f), .65f, new Color(.36f, .53f, .62f, .16f));
                        continue;
                    }
                    // Sırayla örneklemek çekişmeli alanlarda iki tarafı da görünür tutar.
                    bool enemyDot = enemyShare > 0 && ((d * 37) % 101) < enemyShare * 101;
                    var tint = own + enemy == 0 ? new Color(.34f, .48f, .56f, .24f) : Alpha(enemyDot ? Blue : Gold, highlighted ? .65f : .4f);
                    Quad(vh, p - Vector2.one * .7f, Vector2.one * 1.4f, tint);
                }
            }

            // Paketteki öneri yerine simülasyonun gerçek, açık katalog bağlantıları çizilir.
            foreach (var link in AynaWorldData.Links)
            {
                int ai = AynaWorldData.Region(link.A).Key - 1, bi = AynaWorldData.Region(link.B).Key - 1;
                var a = Marker(ai); var b = Marker(bi);
                bool focused = link.A == selected || link.B == selected;
                var tint = Alpha(Cyan, focused ? .24f : .095f);
                if (link.Wrap)
                {
                    float edge = (a.y + b.y) * .5f;
                    Dashed(vh, a, new Vector2(rect.xMin - 5, edge), .75f, tint);
                    Dashed(vh, new Vector2(rect.xMax + 5, edge), b, .75f, tint);
                }
                else if (link.Cable) Dashed(vh, a, b, .75f, tint);
                else Line(vh, a, b, .75f, tint);
            }
        }

        static void EnsureLandDots()
        {
            if (landDots != null) return;
            var contours = AynaMapGeometry.Contours;
            landDots = new Vector2[contours.Length][];
            for (int r = 0; r < contours.Length; r++)
            {
                var dots = new List<Vector2>();
                foreach (var path in contours[r])
                {
                    var lo = new Vector2(float.MaxValue, float.MaxValue); var hi = -lo;
                    foreach (var point in path) { lo = Vector2.Min(lo, point); hi = Vector2.Max(hi, point); }
                    for (float y = Mathf.Ceil(lo.y / 10) * 10; y <= hi.y; y += 10)
                        for (float x = Mathf.Ceil(lo.x / 10) * 10; x <= hi.x; x += 10)
                        {
                            var p = new Vector2(x, y);
                            if (AynaMapGeometry.Contains(path, p)) dots.Add(p);
                        }
                }
                landDots[r] = dots.ToArray();
            }
        }

        internal static Color Alpha(Color value, float alpha) { value.a = alpha; return value; }

        internal static void Quad(VertexHelper vh, Vector2 bottomLeft, Vector2 size, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(bottomLeft, color, Vector2.zero);
            vh.AddVert(bottomLeft + new Vector2(0, size.y), color, Vector2.zero);
            vh.AddVert(bottomLeft + size, color, Vector2.zero);
            vh.AddVert(bottomLeft + new Vector2(size.x, 0), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }

        internal static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            if ((a - b).sqrMagnitude < .001f) return;
            var normal = new Vector2(-(b.y - a.y), b.x - a.x).normalized * width * .5f;
            int start = vh.currentVertCount;
            vh.AddVert(a - normal, color, Vector2.zero); vh.AddVert(a + normal, color, Vector2.zero);
            vh.AddVert(b + normal, color, Vector2.zero); vh.AddVert(b - normal, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }

        internal static void Dashed(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            float length = Vector2.Distance(a, b);
            if (length < .01f) return;
            var direction = (b - a) / length;
            for (float d = 0; d < length; d += 13) Line(vh, a + direction * d, a + direction * Mathf.Min(d + 5, length), width, color);
        }
    }
}
