using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Tek değerli ease-out geçiş (PROMPT §8 hareket: kalkma ~80 ms, basma 60 ms, klasör 120 ms, modal 150/180 ms).
    /// Yapıdır, bellek ayırmaz; sahibi her karede <see cref="Tick"/> çağırır ve değer değiştiyse uygular.
    /// </summary>
    public struct DeskTween
    {
        float from, to, t, duration;
        bool running;   // varsayılan (default) yapı: durgun, değer 0

        public float Value { get; private set; }
        public float Target => to;
        public bool Running => running;

        /// <summary>Animasyonsuz atar (ilk kurulum, Editor, sabit önizleme durumu).</summary>
        public void Snap(float v) { Value = from = to = v; running = false; }

        /// <summary>Hedef değiştiyse bulunduğu değerden yeni hedefe geçişi başlatır.</summary>
        public void To(float v, float seconds)
        {
            if (Mathf.Approximately(v, to) && (running || Mathf.Approximately(v, Value))) return;
            if (seconds <= 0) { Snap(v); return; }
            from = Value; to = v; duration = seconds; t = 0; running = true;
        }

        /// <summary>Bir adım ilerletir; değer değiştiyse true.</summary>
        public bool Tick(float dt)
        {
            if (!running) return false;
            t = Mathf.Min(1, t + dt / duration);
            float e = 1 - (1 - t) * (1 - t);
            Value = Mathf.LerpUnclamped(from, to, e);
            if (t >= 1) running = false;
            return true;
        }
    }
}
