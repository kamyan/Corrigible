using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Kart hareketinin yumuşatması (token <c>motion.board</c>: kalkış ease-out, oturma ease-in, takas ve dönüş in-out).</summary>
    public enum CardEase { Linear, EaseIn, EaseOut, EaseInOut }

    /// <summary>
    /// <see cref="CardEase"/> eğrileri (kübik). In-out, kamera rayının <c>Ease</c> eğrisiyle aynıdır; böylece K4 geçişi ile kart
    /// hareketleri aynı ritimde okunur. Saf hesap; testlerden doğrudan çağrılır.
    /// </summary>
    public static class CardEasing
    {
        /// <summary><paramref name="t"/> ∈ [0, 1] (dışı kırpılır) → yumuşatılmış ilerleme ∈ [0, 1].</summary>
        public static float Evaluate(CardEase ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case CardEase.EaseIn: return t * t * t;
                case CardEase.EaseOut: { float u = 1 - t; return 1 - u * u * u; }
                case CardEase.EaseInOut: return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
                default: return t;
            }
        }
    }
}
