using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Teleks daktilosunun silindirinden çıkan kâğıtların ortak hareketi (saf, durumsuz; teleks kâğıdı ve devir fişi aynı yoldan
    /// çıkar). Silindirde her an en çok bir kâğıt durur: yeni teleks satırında o anki kâğıt düşer (<see cref="DropPose(float, SiginakTheme)"/>),
    /// yenisi alttan satır satır yazılarak çıkar (<see cref="RowRetract"/>, <see cref="Ease"/>); devirde teleks kâğıdı düşer, fiş aynı
    /// yerden yazılarak çıkar (<see cref="TypedFeed(float, int, float)"/>).
    /// </summary>
    public static class PlatenFeed
    {
        /// <summary>
        /// Yazılarak çıkışın yol kesri (saf): <paramref name="t"/> ∈ [0, 1] süresince <paramref name="lines"/> satır adımı; her adımın
        /// ilk <paramref name="slide"/> kesrinde kâğıt bir satır ease-out kayar, kalanında durur (harfler basılıyor). <paramref name="lines"/>
        /// 0 ise tek ease-out kayış. Monoton, 0'da 0, 1'de 1.
        /// </summary>
        public static float TypedFeed(float t, int lines, float slide)
        {
            t = Mathf.Clamp01(t);
            if (lines <= 0) return CardEasing.Evaluate(CardEase.EaseOut, t);
            if (t >= 1) return 1;
            float x = t * lines;
            int k = Mathf.FloorToInt(x);
            float s = slide > 0 ? Mathf.Clamp01((x - k) / slide) : 1;
            return (k + CardEasing.Evaluate(CardEase.EaseOut, s)) / lines;
        }

        /// <summary><see cref="DropPose(float, Vector3, float, Vector3)"/>, değerler <c>motion.telexTear.*</c> token'larından.</summary>
        public static Pose DropPose(float t, SiginakTheme theme)
        {
            float N(string key, float fallback) => theme != null ? theme.NumberOf(key, fallback) : fallback;
            var move = new Vector3(N("motion.telexTear.moveMm[0]", 10), N("motion.telexTear.moveMm[1]", 150), N("motion.telexTear.moveMm[2]", 170)) * 0.001f;
            var euler = new Vector3(N("motion.telexTear.euler[0]", 55), N("motion.telexTear.euler[1]", 0), N("motion.telexTear.euler[2]", 10));
            return DropPose(t, move, N("motion.telexTear.fallMm", 200) * 0.001f, euler);
        }

        /// <summary>
        /// Düşen kâğıdın <paramref name="t"/> ∈ [0, 1] anındaki ofseti (saf; Teleprinter yereli, m) ve dinlenme dönüşüne göre dönüşü:
        /// <c>move·t − fall·t²</c> (silindirden yukarı çekilir, arkaya (+z) atılıp daktilonun arkasına düşer), dönüş <c>euler·t</c>
        /// (+x: üstü arkaya yatar).
        /// </summary>
        public static Pose DropPose(float t, Vector3 moveM, float fallM, Vector3 euler)
        {
            t = Mathf.Clamp01(t);
            return new Pose(moveM * t + Vector3.down * (fallM * t * t), Quaternion.Euler(euler * t));
        }

        /// <summary>
        /// Teleks kâğıdının o an basılan satıra göre geri alma mesafesi (saf, m; kâğıdın yukarı ekseni boyunca, dinlenme pozundan
        /// aşağı): <paramref name="typed"/> harf basılmışken, başı (<paramref name="rowStarts"/>) <paramref name="typed"/>'i geçmeyen son
        /// satırın değeri (<paramref name="rowRetractM"/>; o satırın taban çizgisi yazım çizgisinde). 0 harfte ilk satırın değeri;
        /// <paramref name="typed"/> ≥ <paramref name="length"/> ise <paramref name="endRetractM"/> (satır bitti: kâğıt dinlenmeye, 0).
        /// Satırlar yukarıdan aşağı dizili olduğundan sonuç harf sayısıyla artmaz; değerler 0'ın altına inmez.
        /// </summary>
        public static float RowRetract(int typed, int length, IReadOnlyList<int> rowStarts, IReadOnlyList<float> rowRetractM, float endRetractM = 0)
        {
            if (rowStarts == null || rowRetractM == null || rowStarts.Count == 0 || rowRetractM.Count == 0) return Mathf.Max(0, endRetractM);
            if (typed >= length) return Mathf.Max(0, endRetractM);
            int n = Mathf.Min(rowStarts.Count, rowRetractM.Count);
            float r = rowRetractM[0];
            for (int i = 1; i < n; i++)
            {
                if (rowStarts[i] > typed) break;
                r = Mathf.Min(r, rowRetractM[i]);
            }
            return Mathf.Max(0, r);
        }

        /// <summary>
        /// Satır adımı kayışı (saf): <paramref name="from"/>'dan <paramref name="to"/>'ya <paramref name="slideS"/> saniyede ease-out
        /// (fişin satır adımıyla aynı eğri, <see cref="CardEase.EaseOut"/>); <paramref name="since"/> ≥ <paramref name="slideS"/> ise hedef.
        /// </summary>
        public static float Ease(float from, float to, float since, float slideS)
        {
            if (slideS <= 0 || since >= slideS) return to;
            if (since <= 0) return from;
            return Mathf.LerpUnclamped(from, to, CardEasing.Evaluate(CardEase.EaseOut, since / slideS));
        }
    }
}
