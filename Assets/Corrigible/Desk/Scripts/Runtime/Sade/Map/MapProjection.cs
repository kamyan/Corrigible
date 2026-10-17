using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Sade haritanın Miller izdüşümü; sonuç 1920×1080 tasarım pikselidir, orijin sol üsttedir.</summary>
    public static class MapProjection
    {
        public const float DesignWidth = 1920;
        public const float DesignHeight = 1080;
        public const double CentralMeridian = 25.5;
        public const double ScalePxPerRad = 360.6816;
        public const double EquatorY = 640;
        public static readonly Vector2 DesignSize = new Vector2(DesignWidth, DesignHeight);

        public static Vector2 ToMap(double longitude, double latitude) => ToMap(longitude, latitude, null);

        /// <summary>Veri dosyası farklı izdüşüm sabitleri verirse onları kullanır; enlem kutuplarda sonludur.</summary>
        public static Vector2 ToMap(double longitude, double latitude, SadeMapProjection projection)
        {
            if (double.IsNaN(longitude) || double.IsInfinity(longitude)) throw new ArgumentOutOfRangeException(nameof(longitude));
            if (double.IsNaN(latitude) || double.IsInfinity(latitude)) throw new ArgumentOutOfRangeException(nameof(latitude));
            double meridian = projection?.CentralMeridianDeg ?? CentralMeridian;
            double scale = projection?.ScalePxPerRad ?? ScalePxPerRad;
            double equator = projection?.EquatorY ?? EquatorY;
            double delta = (longitude - meridian) % 360;
            if (delta < -180) delta += 360;
            if (delta > 180) delta -= 360;
            double phi = Math.Max(-90, Math.Min(90, latitude)) * Math.PI / 180;
            double x = DesignWidth / 2 + scale * delta * Math.PI / 180;
            double y = equator - scale * 1.25 * Math.Log(Math.Tan(Math.PI / 4 + 0.4 * phi));
            return new Vector2((float)x, (float)y);
        }
    }
}
