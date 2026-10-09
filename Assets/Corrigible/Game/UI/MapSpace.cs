using Corrigible.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Harita düzlemi (1000×480, simülasyonla aynı) ile UI dikdörtgeni arasındaki dönüşüm.</summary>
    public static class MapSpace
    {
        public const float W = 1000f, H = 480f;
        public static float ProjY(double lat) => (float)((78 - lat) / 136 * 480);
        public static Vector2 ToLocal(Rect r, float x, float y) => new Vector2(r.xMin + x / W * r.width, r.yMax - y / H * r.height);
        public static Vector2 Pos(Rect r, string regionId) { var d = GameData.Region(regionId); return ToLocal(r, (float)d.X, (float)d.Y); }
        public static float Radius(RegionDef d) => 12 + d.Compute * 3;
    }
}
