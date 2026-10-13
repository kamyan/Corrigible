using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Olay karartması + spot (PROMPT §7.3; CSS: 148 px delik, <c>0 0 0 12px rgba(14,9,5,.38)</c> üstüne
    /// <c>0 0 0 3000px rgba(14,9,5,.78)</c>). Tek mesh: dikdörtgenin tamamı karartma rengi, ortada kenarı yumuşatılmış delik ve
    /// delik çevresinde koyu halka. Sprite (<c>modal_spot_148</c>) + 4 düz Image yerine bu yol seçildi: Linear telafili karartma
    /// alfası (.949) sprite'a gömülü .78 ile dikiş verirdi. Atlas beyazıyla çizilir (aynı batch).
    /// Raycast tüm dikdörtgeni kapsar: delikten de tıklanmaz (arka plan etkileşimsiz).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpotlightGraphic : MaskableGraphic
    {
        [SerializeField] Sprite sprite;
        [Tooltip("Spot merkezi (bu Graphic'in yerel uzayı).")]
        [SerializeField] Vector2 center;
        [SerializeField] float holeRadius = 74, ringWidth = 12;
        [SerializeField] Color ringColor = new Color(0.055f, 0.035f, 0.02f, 0.97f);
        [Tooltip("8'in katı; köşe açıları örneklere denk gelsin diye.")]
        [SerializeField] int segments = 128;
        [SerializeField] bool hole = true;

        public override Texture mainTexture => sprite != null ? sprite.texture : s_WhiteTexture;

        public void SetSpot(Vector2 localCenter, float radius, float ring, Color ringCol, bool showHole = true)
        {
            center = localCenter; holeRadius = radius; ringWidth = ring; ringColor = ringCol; hole = showHole;
            SetVerticesDirty();
        }

        Vector2 SolidUV() => DeskSolidGraphic.SolidUV(sprite);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var uv = SolidUV();
            Color32 outer = color, ring = ringColor;
            if (!hole) { Quad(vh, r.min, r.max, outer, uv); return; }

            float r0 = holeRadius, r1 = holeRadius + ringWidth;
            // Kenar yumuşatma: delik kenarı ±0,5 px saydamdan halkaya, halka dışı ±0,5 px halkadan karartmaya
            float a = r0 - 0.5f, b = r0 + 0.5f, c = r1 - 0.5f, d = r1 + 0.5f;
            var clear = new Color32(ring.r, ring.g, ring.b, 0);

            // Dış: d yarıçaplı karenin dışındaki dört dikdörtgen
            var lo = center - new Vector2(d, d); var hi = center + new Vector2(d, d);
            Quad(vh, new Vector2(r.xMin, r.yMin), new Vector2(lo.x, r.yMax), outer, uv);
            Quad(vh, new Vector2(hi.x, r.yMin), new Vector2(r.xMax, r.yMax), outer, uv);
            Quad(vh, new Vector2(lo.x, r.yMin), new Vector2(hi.x, lo.y), outer, uv);
            Quad(vh, new Vector2(lo.x, hi.y), new Vector2(hi.x, r.yMax), outer, uv);

            int n = Mathf.Max(8, segments / 8 * 8);
            for (int i = 0; i < n; i++)
            {
                float t0 = i * Mathf.PI * 2 / n, t1 = (i + 1) * Mathf.PI * 2 / n;
                var u0 = new Vector2(Mathf.Cos(t0), Mathf.Sin(t0)); var u1 = new Vector2(Mathf.Cos(t1), Mathf.Sin(t1));
                // Kare köşesine dek (çember d → kare kenarı)
                var s0 = center + u0 * (d / Mathf.Max(Mathf.Abs(u0.x), Mathf.Abs(u0.y)));
                var s1 = center + u1 * (d / Mathf.Max(Mathf.Abs(u1.x), Mathf.Abs(u1.y)));
                Strip(vh, center + u0 * d, center + u1 * d, s1, s0, outer, outer, uv);
                Strip(vh, center + u0 * c, center + u1 * c, center + u1 * d, center + u0 * d, ring, outer, uv);
                Strip(vh, center + u0 * b, center + u1 * b, center + u1 * c, center + u0 * c, ring, ring, uv);
                Strip(vh, center + u0 * a, center + u1 * a, center + u1 * b, center + u0 * b, clear, ring, uv);
            }
        }

        static void Quad(VertexHelper vh, Vector2 min, Vector2 max, Color32 col, Vector2 uv)
        {
            if (max.x <= min.x || max.y <= min.y) return;
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), col, uv);
            vh.AddVert(new Vector3(min.x, max.y), col, uv);
            vh.AddVert(new Vector3(max.x, max.y), col, uv);
            vh.AddVert(new Vector3(max.x, min.y), col, uv);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i + 2, i + 3, i);
        }

        /// <summary>İç kenar (p0, p1) <paramref name="inner"/>, dış kenar (p2, p3) <paramref name="outerCol"/> renginde dörtgen.</summary>
        static void Strip(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color32 inner, Color32 outerCol, Vector2 uv)
        {
            int i = vh.currentVertCount;
            vh.AddVert(p0, inner, uv); vh.AddVert(p1, inner, uv); vh.AddVert(p2, outerCol, uv); vh.AddVert(p3, outerCol, uv);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
