using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Haritanın tasarım alanı: 1920×1080 tasarım pikseli, orijin sol üstte (bölge çokgenleri bu uzaydadır).</summary>
    public static class MapProjection
    {
        public const float DesignWidth = 1920;
        public const float DesignHeight = 1080;
        public static readonly Vector2 DesignSize = new Vector2(DesignWidth, DesignHeight);
    }
}
