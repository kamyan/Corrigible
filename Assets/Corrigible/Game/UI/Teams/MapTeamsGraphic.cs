using Corrigible.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Haritada yoldaki ekiplerin kalan yolu (kesik çizgi). İşaretlerin kendisi MapView'in yerleştirdiği TeamMarker'lardır.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapTeamsGraphic : MaskableGraphic
    {
        Simulation sim;

        public void Bind(Simulation s) { sim = s; SetVerticesDirty(); }

        /// <summary>Ekibin harita düzlemindeki (1000×480) konumu: bölgede ya da iki bölge arasında ilerlemeye göre.</summary>
        public static Vector2 MapPos(TeamState t)
        {
            if (t.Dest == null) { var d = GameData.Region(t.At); return new Vector2((float)d.X, (float)d.Y); }
            var A = GameData.Region(t.From); var B = GameData.Region(t.Dest);
            float k = t.Trip > 0 ? (t.Trip - t.Eta) / (float)t.Trip : 0;
            return Vector2.Lerp(new Vector2((float)A.X, (float)A.Y), new Vector2((float)B.X, (float)B.Y), k);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (sim == null || !sim.TeamsOn) return;
            var r = GetPixelAdjustedRect();
            float k = r.width / MapSpace.W;
            foreach (var t in sim.S.Teams)
            {
                if (t.Dest == null) continue;
                var p = MapPos(t);
                var to = MapSpace.Pos(r, t.Dest);
                UiMesh.DashedLine(vh, MapSpace.ToLocal(r, p.x, p.y), to, Mathf.Max(1f, 1.4f * k), color, 2 * k, 4 * k);
            }
        }
    }
}
