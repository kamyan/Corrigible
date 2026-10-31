using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Tasarımdan gelen sayısal kurallar (görünüm hesabı; oyun kuralı değil).</summary>
    public static class DeskMath
    {
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
    }
}
