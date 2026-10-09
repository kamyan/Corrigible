using Corrigible.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Statik katman: enlem çizgileri ve kara noktaları. Bir kez çizilir.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapLandGraphic : MaskableGraphic
    {
        [SerializeField] Color gridColor = new Color(34 / 255f, 52 / 255f, 60 / 255f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float k = r.width / MapSpace.W;
            foreach (var lat in new[] { 60.0, 30.0, 0.0, -30.0 })
            {
                float y = MapSpace.ToLocal(r, 0, MapSpace.ProjY(lat)).y;
                UiMesh.Line(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), 1f, gridColor);
            }
            float rad = 1.7f * k;
            foreach (var dot in LandMask.Dots) UiMesh.Disc(vh, MapSpace.ToLocal(r, dot.x, dot.y), rad, color, 8);
        }
    }
}
