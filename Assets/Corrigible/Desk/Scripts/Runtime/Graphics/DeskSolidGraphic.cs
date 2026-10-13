using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Büyük alanda düz renk (eğitim karartması). Atlas beyazının ortasından tek UV ile çizilir (<see cref="SpotlightGraphic"/>
    /// gibi; aynı batch). Küçük bir dolgu sprite'ını Image ile büyük alana germek, sprite'ın kenar piksellerini atlas
    /// dolgusuyla karıştırıp onlarca piksele yayar: yan yana parçalar arasında açık renkli dikişler çıkar.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DeskSolidGraphic : MaskableGraphic
    {
        [Tooltip("Atlas beyazı (ör. sh_fill_r0); yalnız ortasındaki renk örneklenir.")]
        [SerializeField] Sprite sprite;

        public override Texture mainTexture => sprite != null ? sprite.texture : s_WhiteTexture;

        /// <summary>Sprite'ın ortasının UV'si (boşsa beyaz dokunun ortası).</summary>
        public static Vector2 SolidUV(Sprite sprite)
        {
            if (sprite == null) return new Vector2(0.5f, 0.5f);
            var o = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            return new Vector2((o.x + o.z) * 0.5f, (o.y + o.w) * 0.5f);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var uv = SolidUV(sprite);
            Color32 c = color;
            vh.AddVert(new Vector3(r.xMin, r.yMin), c, uv);
            vh.AddVert(new Vector3(r.xMin, r.yMax), c, uv);
            vh.AddVert(new Vector3(r.xMax, r.yMax), c, uv);
            vh.AddVert(new Vector3(r.xMax, r.yMin), c, uv);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
