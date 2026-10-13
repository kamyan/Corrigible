using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    public enum PolygonFill { Solid, Texture }

    /// <summary>
    /// Dolu poligon Graphic'i (PROMPT §4.5). İki biçim:
    /// <b>Şerit</b> (<see cref="SetBand"/>): aynı sayıda örneği olan üst ve alt eğri arası dörtgen şerit (C4 bantları);
    /// <b>Dışbükey poligon</b> (<see cref="SetConvex"/>): merkezden yelpaze.
    /// Düz renkte atlas beyazı (<c>sh_fill_r0</c> merkez UV), dokuda tekrar eden doku (UV = konum / döşeme; Wrap = Repeat; ayrı draw call).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIPolygonGraphic : MaskableGraphic
    {
        [SerializeField] PolygonFill fill;
        [SerializeField] Sprite sprite;
        [SerializeField] Texture2D texture;
        [Tooltip("Doku döşeme boyu (px), ör. tx_hatch_red için 10.")]
        [SerializeField] Vector2 tile = new Vector2(10, 10);
        [SerializeField] List<Vector2> upper = new List<Vector2>();
        [SerializeField] List<Vector2> lower = new List<Vector2>();
        [SerializeField] bool band = true;

        public override Texture mainTexture =>
            fill == PolygonFill.Texture && texture != null ? texture : sprite != null ? sprite.texture : s_WhiteTexture;

        public void SetFill(PolygonFill f, Texture2D tex = null)
        {
            fill = f; if (tex != null) texture = tex;
            SetMaterialDirty(); SetVerticesDirty();
        }

        /// <summary>Üst ve alt eğri arası şerit (yerel uzay, y yukarı). İki liste aynı uzunlukta olmalı.</summary>
        public void SetBand(IList<Vector2> top, IList<Vector2> bottom)
        {
            band = true;
            Copy(top, upper); Copy(bottom, lower);
            SetVerticesDirty();
        }

        /// <summary>Dışbükey poligon (yerel uzay).</summary>
        public void SetConvex(IList<Vector2> pts)
        {
            band = false;
            Copy(pts, upper); lower.Clear();
            SetVerticesDirty();
        }

        static void Copy(IList<Vector2> src, List<Vector2> dst)
        {
            dst.Clear();
            for (int i = 0; i < src.Count; i++) dst.Add(src[i]);
        }

        Vector2 SpriteUV()
        {
            if (sprite == null) return new Vector2(0.5f, 0.5f);
            var o = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            return new Vector2((o.x + o.z) * 0.5f, (o.y + o.w) * 0.5f);
        }

        Vector2 UV(Vector2 p, Vector2 solid) =>
            fill == PolygonFill.Texture && texture != null ? new Vector2(p.x / tile.x, p.y / tile.y) : solid;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var col = (Color32)color;
            var suv = SpriteUV();
            if (band)
            {
                int n = Mathf.Min(upper.Count, lower.Count);
                if (n < 2) return;
                for (int i = 0; i < n; i++)
                {
                    vh.AddVert(upper[i], col, UV(upper[i], suv));
                    vh.AddVert(lower[i], col, UV(lower[i], suv));
                }
                for (int i = 0; i < n - 1; i++)
                {
                    int a = i * 2;
                    vh.AddTriangle(a, a + 2, a + 1);
                    vh.AddTriangle(a + 1, a + 2, a + 3);
                }
                return;
            }
            if (upper.Count < 3) return;
            var c = Vector2.zero;
            for (int i = 0; i < upper.Count; i++) c += upper[i];
            c /= upper.Count;
            vh.AddVert(c, col, UV(c, suv));
            for (int i = 0; i < upper.Count; i++) vh.AddVert(upper[i], col, UV(upper[i], suv));
            for (int i = 0; i < upper.Count; i++) vh.AddTriangle(0, 1 + i, 1 + (i + 1) % upper.Count);
        }
    }
}
