using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Hazır earcut üçgenlerinden bölge dolgusu. Halka başlangıçları ve düz köşeler serileştirilir; sahne yeniden
    /// yüklendiğinde JSON'u ya da editör kurucusunu gerektirmez. Doku UV'si tüm bölgelerde aynı tasarım fazındadır.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RegionMeshGraphic : MaskableGraphic, ICanvasRaycastFilter, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Serializable]
        public sealed class Polygon
        {
            public Vector2[] Vertices = Array.Empty<Vector2>();
            /// <summary>İlk değer 0, son değer köşe sayısıdır; aradaki her aralık bir halka.</summary>
            public int[] RingOffsets = Array.Empty<int>();
            public int[] Triangles = Array.Empty<int>();
            public Vector4 Bounds;
        }

        [SerializeField] string regionId;
        [SerializeField] Polygon[] polygons = Array.Empty<Polygon>();
        [SerializeField] Texture texture;
        [Tooltip("Desenin tasarım pikseli cinsinden tekrar aralığı; 0 = düz renk.")]
        [SerializeField] float tileSize;

        SadeMapRegion source;
        public string RegionId => regionId;
        public int PolygonCount => polygons.Length;
        public event Action<string> Clicked;
        /// <summary>Üzerine gelme (true) ve çıkma (false); kurtuluş haritasının sarı dolgusu için.</summary>
        public event Action<string, bool> Hovered;
        public override Texture mainTexture => texture != null ? texture : s_WhiteTexture;

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left) Clicked?.Invoke(regionId);
        }

        public void OnPointerEnter(PointerEventData e) => Hovered?.Invoke(regionId, true);
        public void OnPointerExit(PointerEventData e) => Hovered?.Invoke(regionId, false);

        /// <summary>En büyük çokgenin dış halkası (tasarım pikseli, y aşağı); kontur çizgisi için.</summary>
        public Vector2[] LargestOuterRing()
        {
            Polygon best = null;
            float bestArea = -1;
            foreach (var polygon in polygons)
            {
                if (polygon == null || polygon.RingOffsets.Length < 2) continue;
                float area = (polygon.Bounds.z - polygon.Bounds.x) * (polygon.Bounds.w - polygon.Bounds.y);
                if (area > bestArea) { bestArea = area; best = polygon; }
            }
            if (best == null) return Array.Empty<Vector2>();
            int start = best.RingOffsets[0], end = best.RingOffsets[1];
            var ring = new Vector2[end - start];
            Array.Copy(best.Vertices, start, ring, 0, ring.Length);
            return ring;
        }

        /// <summary>Aynı geometriyi başka bir grafiğe kopyalar (kurucu: tarama ve seçim katmanları aynı çokgeni çizer).</summary>
        public void CopyGeometryFrom(RegionMeshGraphic other)
        {
            if (other == null) return;
            regionId = other.regionId;
            polygons = other.polygons;
            SetVerticesDirty();
        }

        /// <summary>Yalnız geometri değişince hazırlar; oluşan düz diziler sahne/prefab içinde kalıcıdır.</summary>
        public void Configure(SadeMapRegion geometry, Texture fillTexture = null, float tileSize = 0)
        {
            if (!ReferenceEquals(source, geometry))
            {
                source = geometry;
                regionId = geometry?.Id;
                var input = geometry?.Polygons ?? Array.Empty<SadeMapPolygon>();
                polygons = new Polygon[input.Length];
                for (int p = 0; p < input.Length; p++) polygons[p] = Prepare(input[p]);
                SetVerticesDirty();
            }
            SetAppearance(color, fillTexture, tileSize);
        }

        /// <summary>VM'den gelen rengi/dokuyu uygular; aynı değerle tekrar bağlama mesh'i kirletmez.</summary>
        public void SetAppearance(Color fill, Texture fillTexture = null, float tileDesignPx = 0)
        {
            if (color != fill) color = fill;
            if (texture != fillTexture)
            {
                texture = fillTexture;
                SetMaterialDirty();
                SetVerticesDirty();
            }
            float tile = fillTexture != null ? (tileDesignPx > 0 ? tileDesignPx : 1) : 0;
            if (!Mathf.Approximately(tileSize, tile)) { tileSize = tile; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (polygons == null) return;
            var rect = rectTransform.rect;
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            foreach (var polygon in polygons)
            {
                if (polygon == null) continue;
                int first = vh.currentVertCount;
                foreach (var point in polygon.Vertices)
                {
                    vertex.position = new Vector3(rect.xMin + point.x, rect.yMax - point.y);
                    vertex.uv0 = tileSize > 0 ? new Vector2(point.x / tileSize, -point.y / tileSize) : Vector2.zero;
                    vh.AddVert(vertex);
                }
                var tris = polygon.Triangles;
                for (int i = 0; i + 2 < tris.Length; i += 3)
                    vh.AddTriangle(first + tris[i], first + tris[i + 1], first + tris[i + 2]);
            }
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local)) return false;
            var rect = rectTransform.rect;
            return ContainsMapPoint(new Vector2(local.x - rect.xMin, rect.yMax - local.y));
        }

        /// <summary>Tasarım uzayında çift-tek testi; deniz, çokgenler arası boşluk ve delikler tıklanamaz.</summary>
        public bool ContainsMapPoint(Vector2 point)
        {
            foreach (var polygon in polygons)
            {
                if (polygon == null || point.x < polygon.Bounds.x || point.y < polygon.Bounds.y || point.x > polygon.Bounds.z || point.y > polygon.Bounds.w) continue;
                bool inside = false;
                var vertices = polygon.Vertices;
                for (int ring = 0; ring + 1 < polygon.RingOffsets.Length; ring++)
                {
                    int start = polygon.RingOffsets[ring], end = polygon.RingOffsets[ring + 1];
                    bool boundary = false;
                    for (int i = start, j = end - 1; i < end; j = i++)
                    {
                        Vector2 a = vertices[j], b = vertices[i];
                        if (OnSegment(point, a, b)) { boundary = true; break; }
                        if ((a.y > point.y) != (b.y > point.y) &&
                            point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                    }
                    if (boundary)
                    {
                        // Dış kıyı çizgisi bölgeye dahildir; göl/deniz deliğinin kenarı değildir.
                        if (ring == 0) return true;
                        inside = false;
                        break;
                    }
                }
                if (inside) return true;
            }
            return false;
        }

        static bool OnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 edge = b - a, delta = point - a;
            float length = edge.sqrMagnitude;
            if (length < 0.0000001f) return delta.sqrMagnitude < 0.0000001f;
            float cross = edge.x * delta.y - edge.y * delta.x;
            if (Mathf.Abs(cross) > 0.0001f * Mathf.Sqrt(length)) return false;
            float dot = Vector2.Dot(delta, edge);
            return dot >= -0.0001f && dot <= length + 0.0001f;
        }

        static Polygon Prepare(SadeMapPolygon input)
        {
            var rings = input?.Rings ?? Array.Empty<float[][]>();
            int count = 0;
            foreach (var ring in rings) count += ring?.Length ?? 0;
            var result = new Polygon
            {
                Vertices = new Vector2[count],
                RingOffsets = new int[rings.Length + 1],
                Triangles = input?.Triangles != null ? (int[])input.Triangles.Clone() : Array.Empty<int>(),
                Bounds = new Vector4(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue)
            };
            int index = 0;
            for (int r = 0; r < rings.Length; r++)
            {
                result.RingOffsets[r] = index;
                foreach (var point in rings[r] ?? Array.Empty<float[]>())
                {
                    if (point == null || point.Length < 2) throw new ArgumentException("Harita köşesi iki koordinat taşımalıdır.");
                    var vertex = new Vector2(point[0], point[1]);
                    result.Vertices[index++] = vertex;
                    result.Bounds = new Vector4(Mathf.Min(result.Bounds.x, vertex.x), Mathf.Min(result.Bounds.y, vertex.y),
                        Mathf.Max(result.Bounds.z, vertex.x), Mathf.Max(result.Bounds.w, vertex.y));
                }
            }
            result.RingOffsets[rings.Length] = index;
            foreach (int triangle in result.Triangles)
                if (triangle < 0 || triangle >= count) throw new ArgumentException("Harita üçgen indisi köşe dizisinin dışında.");
            return result;
        }
    }
}
