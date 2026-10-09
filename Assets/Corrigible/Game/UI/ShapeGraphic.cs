using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Dikdörtgenine sığan basit şekil: disk, halka, kesik halka, taralı daire, rozet (onay / ünlem),
    /// elmas (ekip işareti), üçgen (dron) ya da kare.
    /// Harita düğümleri bu parçalardan oluşur; edit modda da çizildiği için Scene görünümünde görünür.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ShapeGraphic : MaskableGraphic
    {
        public enum Kind { Disc, Ring, DashedRing, Hatch, Check, Alert, Diamond, Triangle, Square }

        [SerializeField] Kind kind = Kind.Disc;
        [Tooltip("Halka ve tarama çizgilerinin kalınlığı.")]
        [SerializeField, Min(0.1f)] float thickness = 1.5f;
        [Tooltip("Rozetteki işaretin rengi.")]
        [SerializeField] Color markColor = new Color(10 / 255f, 17 / 255f, 20 / 255f);

        public Kind Shape { get => kind; set { if (kind != value) { kind = value; SetVerticesDirty(); } } }
        public float Thickness { get => thickness; set { if (!Mathf.Approximately(thickness, value)) { thickness = value; SetVerticesDirty(); } } }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var c = r.center;
            float rad = Mathf.Min(r.width, r.height) / 2;
            switch (kind)
            {
                case Kind.Disc:
                    UiMesh.Disc(vh, c, rad - 0.5f, color);
                    break;
                case Kind.Ring:
                    UiMesh.Ring(vh, c, rad - thickness / 2 - 0.5f, thickness, color);
                    break;
                case Kind.DashedRing:
                    for (int a = 0; a < 360; a += 24) UiMesh.Arc(vh, c, rad - thickness / 2 - 0.5f, thickness, a, a + 12, color);
                    break;
                case Kind.Hatch:
                {
                    // Çemberin içinde kalan 45° çizgi parçaları
                    var dir = new Vector2(0.7071f, -0.7071f);
                    var n = new Vector2(0.7071f, 0.7071f);
                    float step = Mathf.Max(4f, rad / 4.5f);
                    for (float o = -rad; o <= rad; o += step)
                    {
                        float half = Mathf.Sqrt(Mathf.Max(0, rad * rad - o * o)) - thickness;
                        if (half <= 0) continue;
                        var m = c + n * o;
                        UiMesh.Line(vh, m - dir * half, m + dir * half, thickness, color);
                    }
                    break;
                }
                case Kind.Check:
                    UiMesh.Disc(vh, c, rad - 0.5f, color);
                    float k = rad / 6.5f;
                    UiMesh.Line(vh, c + new Vector2(-3f, 0f) * k, c + new Vector2(-0.8f, -2.4f) * k, 1.8f * k, markColor);
                    UiMesh.Line(vh, c + new Vector2(-0.8f, -2.4f) * k, c + new Vector2(3.2f, 2.4f) * k, 1.8f * k, markColor);
                    UiMesh.Disc(vh, c + new Vector2(-0.8f, -2.4f) * k, 0.9f * k, markColor, 8);
                    break;
                case Kind.Diamond:
                    Poly(vh, new[] { c + new Vector2(0, rad), c + new Vector2(rad, 0), c + new Vector2(0, -rad), c + new Vector2(-rad, 0) });
                    break;
                case Kind.Triangle:
                    Poly(vh, new[] { c + new Vector2(0, rad), c + new Vector2(rad * 0.95f, -rad * 0.8f), c + new Vector2(-rad * 0.95f, -rad * 0.8f) });
                    break;
                case Kind.Square:
                    UiMesh.Rect(vh, new Rect(c.x - rad * 0.85f, c.y - rad * 0.85f, rad * 1.7f, rad * 1.7f), color);
                    break;
                case Kind.Alert:
                    UiMesh.Disc(vh, c, rad - 0.5f, color);
                    float q = rad / 6.5f;
                    UiMesh.Line(vh, c + new Vector2(0, 3.2f) * q, c + new Vector2(0, -0.6f) * q, 1.8f * q, markColor);
                    UiMesh.Disc(vh, c + new Vector2(0, -2.8f) * q, 1f * q, markColor, 8);
                    break;
            }
        }

        // Dışbükey çokgen (yelpaze üçgenleme)
        void Poly(VertexHelper vh, Vector2[] pts)
        {
            int start = vh.currentVertCount;
            foreach (var p in pts) vh.AddVert(p, color, Vector4.zero);
            for (int i = 1; i < pts.Length - 1; i++) vh.AddTriangle(start, start + i, start + i + 1);
        }
    }
}
