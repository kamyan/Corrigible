using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Üst kenarla dikişsiz birleşen, iki mürekkep kenarlı kâğıt işaretçi.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SadeTooltipPointerGraphic : MaskableGraphic
    {
        [SerializeField] Sprite sprite;
        public override Texture mainTexture => sprite != null ? sprite.texture : s_WhiteTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 uv = new Vector2(.5f, .5f);
            if (sprite != null)
            {
                var o = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
                uv = new Vector2((o.x + o.z) * .5f, (o.y + o.w) * .5f);
            }
            var theme = SadeTheme.Active;
            Add(vh, new Vector2(-10, -1), new Vector2(0, 10), new Vector2(10, -1), theme != null ? theme.Get("ink") : Color.black, uv);
            // İç üçgenin alt kenarı biraz aşağı iner; kartın yatay kenarı işaretçinin altında görünmez.
            Add(vh, new Vector2(-8.5f, -2), new Vector2(0, 8), new Vector2(8.5f, -2), theme != null ? theme.Get("index") : Color.white, uv);
        }

        static void Add(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color, Vector2 uv)
        {
            int n = vh.currentVertCount;
            vh.AddVert(a, color, uv); vh.AddVert(b, color, uv); vh.AddVert(c, color, uv); vh.AddTriangle(n, n + 1, n + 2);
        }
    }
}
