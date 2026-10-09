using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// uGUI mesh'ine çizgi, disk ve yay ekler. Kenarlarda 1 birimlik saydamlaşan şerit (yumuşatma) bulunur;
    /// böylece harita ve grafik çizimleri MSAA olmadan da pürüzsüz görünür.
    /// </summary>
    public static class UiMesh
    {
        const float Feather = 0.9f;

        static int V(VertexHelper vh, Vector2 p, Color32 c)
        {
            vh.AddVert(p, c, Vector4.zero);
            return vh.currentVertCount - 1;
        }

        static Color32 Clear(Color32 c) => new Color32(c.r, c.g, c.b, 0);

        /// <summary>a'dan b'ye kalınlığı w olan düz çizgi.</summary>
        public static void Line(VertexHelper vh, Vector2 a, Vector2 b, float w, Color col)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized;
            Color32 c = col, z = Clear(c);
            float h = w / 2, o = h + Feather;
            int i0 = V(vh, a + n * o, z), i1 = V(vh, a + n * h, c), i2 = V(vh, a - n * h, c), i3 = V(vh, a - n * o, z);
            int j0 = V(vh, b + n * o, z), j1 = V(vh, b + n * h, c), j2 = V(vh, b - n * h, c), j3 = V(vh, b - n * o, z);
            Strip(vh, i0, i1, j1, j0); Strip(vh, i1, i2, j2, j1); Strip(vh, i2, i3, j3, j2);
        }

        public static void DashedLine(VertexHelper vh, Vector2 a, Vector2 b, float w, Color col, float on, float off)
        {
            float len = Vector2.Distance(a, b);
            if (len < 1e-4f) return;
            var dir = (b - a) / len;
            for (float t = 0; t < len; t += on + off) Line(vh, a + dir * t, a + dir * Mathf.Min(len, t + on), w, col);
        }

        public static void Polyline(VertexHelper vh, Vector2[] pts, float w, Color col)
        {
            for (int i = 1; i < pts.Length; i++) Line(vh, pts[i - 1], pts[i], w, col);
            // Köşelerde boşluk kalmasın
            for (int i = 1; i < pts.Length - 1; i++) Disc(vh, pts[i], w / 2, col, 8);
        }

        public static void Disc(VertexHelper vh, Vector2 c, float r, Color col, int seg = 0)
        {
            if (r <= 0) return;
            if (seg <= 0) seg = Mathf.Clamp(Mathf.CeilToInt(r * 1.4f), 10, 72);
            Color32 cc = col, z = Clear(cc);
            int center = V(vh, c, cc);
            int first = vh.currentVertCount;
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                V(vh, c + u * r, cc);
                V(vh, c + u * (r + Feather), z);
            }
            for (int i = 0; i < seg; i++)
            {
                int p = first + i * 2, q = p + 2;
                vh.AddTriangle(center, p, q);
                Strip(vh, p, p + 1, q + 1, q);
            }
        }

        /// <summary>Yarıçapı r (çizginin ortası), kalınlığı w olan yay; açılar derece, saat yönünün tersine, 0 = sağ.</summary>
        public static void Arc(VertexHelper vh, Vector2 c, float r, float w, float fromDeg, float toDeg, Color col)
        {
            float span = toDeg - fromDeg;
            int seg = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(span) / 360f * Mathf.Max(16, r * 1.4f)), 2, 96);
            Color32 cc = col, z = Clear(cc);
            float h = w / 2;
            int first = vh.currentVertCount;
            for (int i = 0; i <= seg; i++)
            {
                float a = (fromDeg + span * i / seg) * Mathf.Deg2Rad;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                V(vh, c + u * (r + h + Feather), z);
                V(vh, c + u * (r + h), cc);
                V(vh, c + u * (r - h), cc);
                V(vh, c + u * Mathf.Max(0, r - h - Feather), z);
            }
            for (int i = 0; i < seg; i++)
            {
                int p = first + i * 4, q = p + 4;
                Strip(vh, p, p + 1, q + 1, q); Strip(vh, p + 1, p + 2, q + 2, q + 1); Strip(vh, p + 2, p + 3, q + 3, q + 2);
            }
        }

        public static void Ring(VertexHelper vh, Vector2 c, float r, float w, Color col) => Arc(vh, c, r, w, 0, 360, col);

        public static void Rect(VertexHelper vh, Rect r, Color col)
        {
            Color32 c = col;
            int a = V(vh, new Vector2(r.xMin, r.yMin), c), b = V(vh, new Vector2(r.xMin, r.yMax), c),
                d = V(vh, new Vector2(r.xMax, r.yMax), c), e = V(vh, new Vector2(r.xMax, r.yMin), c);
            Strip(vh, a, b, d, e);
        }

        static void Strip(VertexHelper vh, int a, int b, int c, int d)
        {
            vh.AddTriangle(a, b, c);
            vh.AddTriangle(c, d, a);
        }
    }
}
