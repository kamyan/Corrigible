using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Tasarımdan gelen sayısal kurallar (görünüm hesabı; oyun kuralı değil). Testlerle sabitlenir.</summary>
    public static class DeskMath
    {
        /// <summary>Sayaç ibresi: θ = (değer − 50) × 1,2° (saat yönü; 0 → −60°, 100 → +60°). Unity z = −θ.</summary>
        public static float NeedleDegrees(float value) => (Mathf.Clamp(value, 0, 100) - 50f) * 1.2f;

        public static float NeedleZ(float value) => -NeedleDegrees(value);

        /// <summary>
        /// Belirsizlik kaması (Radial360, üstten saat yönü): değer ± u aralığı.
        /// Dönüş: fillAmount ve Image'ın z açısı (kama değer − u'dan başlar).
        /// </summary>
        public static (float fill, float z) Wedge(float value, float uncertainty)
        {
            float span = Mathf.Max(0, uncertainty) * 2f * 1.2f;
            float startDeg = NeedleDegrees(value) - uncertainty * 1.2f;
            return (span / 360f, -startDeg);
        }

        /// <summary>Tehlike bölgesi [a, b] (0–100) için aynı Radial360 hesabı.</summary>
        public static (float fill, float z) Zone(float from, float to)
        {
            float a = NeedleDegrees(from), b = NeedleDegrees(to);
            return ((b - a) / 360f, -a);
        }

        /// <summary>Harita projeksiyonu (ekran pikseli, sol-üst orijin): x = 960 + (boylam − 27) × 3,5; y = 622 − enlem × 4,45.</summary>
        public static Vector2 Project(double lon, double lat) => new Vector2((float)(960 + (lon - 27) * 3.5), (float)(622 - lat * 4.45));

        /// <summary>
        /// TSV satırı → sol-üst köşe. Döndürülmüş öğede TSV kutusu dönmüş sınırlayıcı kutudur; asıl kutu (layout) merkez etrafında döner:
        /// x = bboxMerkezX − w/2, y = bboxMerkezY − h/2.
        /// </summary>
        public static Vector2 UnrotatedTopLeft(Rect bbox, Vector2 layoutSize) =>
            new Vector2(bbox.center.x - layoutSize.x / 2f, bbox.center.y - layoutSize.y / 2f);

        /// <summary>Ekran (sol-üst, y aşağı) dikdörtgenini ebeveynin sol-üstüne göre anchoredPosition'a çevirir (anchor/pivot sol-üst).</summary>
        public static Vector2 AnchoredFromScreen(Vector2 childTopLeft, Vector2 parentTopLeft) =>
            new Vector2(childTopLeft.x - parentTopLeft.x, -(childTopLeft.y - parentTopLeft.y));

        /// <summary>CSS dönüşü (saat yönü derece) → Unity z.</summary>
        public static float CssRotationToZ(float cssDeg) => -cssDeg;

        /// <summary>
        /// Linear renk uzayı telafisi (KARARLAR: Linear'da kalınır): CSS yarı saydam örtüyü sRGB'de karıştırır, Unity Linear'da.
        /// Aynı görünen sonucu (tipik arka plan <paramref name="backgroundSrgb"/> üstünde) veren Linear alfayı döndürür.
        /// Ör. #0E0905@0,78 karartma kâğıt (≈0,85) üstünde ≈0,94 olur.
        /// </summary>
        public static float LinearOverlayAlpha(Color overlay, float alpha, float backgroundSrgb = 0.85f)
        {
            float c = (overlay.r + overlay.g + overlay.b) / 3f;
            float target = c * alpha + backgroundSrgb * (1 - alpha);
            float lb = Mathf.GammaToLinearSpace(backgroundSrgb), lc = Mathf.GammaToLinearSpace(c);
            float denom = lb - lc;
            if (Mathf.Abs(denom) < 1e-4f) return alpha;
            return Mathf.Clamp01((lb - Mathf.GammaToLinearSpace(target)) / denom);
        }

        /// <summary>Sızma rampası adımı (0–19, 20–39, 40–59, 60–79, 80+).</summary>
        public static int RampIndex(double infiltration) => Mathf.Clamp((int)(Mathf.Max(0, (float)infiltration) / 20f), 0, 4);
    }
}
