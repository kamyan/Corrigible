using System;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sayaç rafının tek modülü (K1 notu): pirinç plaka, pencerede çarklar, isteğe bağlı kırmızı onda bir çarkı (FON) ve
    /// sabit ek (“/40”). Değer değişince çarklar sağdan sola 0,12 s arayla yuvarlanır, altına ± fişi asılır.
    /// </summary>
    public sealed class CounterModule : MonoBehaviour
    {
        [Tooltip("Tam sayı haneleri, soldan sağa.")]
        [SerializeField] CounterWheel[] wheels = Array.Empty<CounterWheel>();
        [Tooltip("Onda bir çarkı (yalnız FON).")]
        [SerializeField] CounterWheel tenths;
        [SerializeField] TMP_Text suffix;
        [SerializeField] DeltaSlip slip;

        double value;
        long shown = -1;

        public event Action Ticked;
        public double Value => value;
        int Scale => tenths != null ? 10 : 1;
        public double Max => Math.Pow(10, wheels.Length) - 1.0 / Scale;

        public void Configure(CounterWheel[] integerWheels, CounterWheel tenthsWheel, TMP_Text suffixText, DeltaSlip deltaSlip)
        {
            wheels = integerWheels; tenths = tenthsWheel; suffix = suffixText; slip = deltaSlip;
        }

        void Awake()
        {
            foreach (var w in wheels) if (w != null) w.Ticked += Tick;
            if (tenths != null) tenths.Ticked += Tick;
        }

        void OnDestroy()
        {
            foreach (var w in wheels) if (w != null) w.Ticked -= Tick;
            if (tenths != null) tenths.Ticked -= Tick;
        }

        void Tick() => Ticked?.Invoke();

        /// <summary>
        /// Değeri gösterir. Gösterilen hane (aşağı yuvarlanmış) değişmişse ve <paramref name="animate"/> ise çarklar döner
        /// ve fark fişi asılır; ilk gösterimde ya da animasyonsuz hemen oturur. <paramref name="rollSeconds"/> çark dönüş
        /// süresi (negatifse token); <paramref name="withSlip"/> false ise fiş asılmaz (vardiya dizisi fişleri kendisi iğneler).
        /// </summary>
        public void Show(double v, string suffixText, bool animate, float delay = 0, float rollSeconds = -1, bool withSlip = true)
        {
            if (suffix != null && suffix.text != (suffixText ?? "")) suffix.text = suffixText ?? "";
            v = Math.Max(0, Math.Min(Max, v));
            long scaled = (long)Math.Floor(v * Scale + 1e-6);
            if (shown < 0 || !animate || !Application.isPlaying)
            {
                for (int i = 0; i < wheels.Length; i++) wheels[i].Set(Digit(scaled, IntegerPlace(i)));
                if (tenths != null) tenths.Set((int)(scaled % 10));
                shown = scaled; value = v;
                return;
            }
            value = v;
            if (scaled == shown) return;
            int direction = Math.Sign(scaled - shown);
            var t = SiginakTheme.Active;
            float roll = rollSeconds > 0 ? rollSeconds : t.Number("motion.counterRollS", 0.35f), step = t.Number("motion.counterWheelDelayS", 0.12f);
            int order = 0;
            if (tenths != null) tenths.RollTo((int)(scaled % 10), direction, roll, delay + order++ * step);
            for (int i = wheels.Length - 1; i >= 0; i--)
            {
                int digit = Digit(scaled, IntegerPlace(i));
                if (digit != wheels[i].Digit) wheels[i].RollTo(digit, direction, roll, delay + order * step);
                order++;
            }
            if (withSlip && slip != null)
            {
                double delta = (scaled - shown) / (double)Scale;
                slip.Show(DeskFormat.Signed(delta, tenths != null ? 1 : 0), t.ColorOf(delta > 0 ? "status.good" : "status.threat"));
            }
            shown = scaled;
        }

        /// <summary>Vardiya raporu: fişe verilen farkı (“+0,4”, “±0”) yazar ve <see cref="Unpin"/> gelene kadar asılı tutar.</summary>
        public void Pin(string text, Color ink)
        {
            if (slip != null) slip.Show(text, ink, true);
        }

        public void Unpin()
        {
            if (slip != null && slip.Visible) slip.Release();
        }

        /// <summary>i. tam sayı çarkının ölçekli değerdeki basamağı (onda bir varsa bir kayık).</summary>
        int IntegerPlace(int i) => wheels.Length - 1 - i + (tenths != null ? 1 : 0);

        static int Digit(long scaled, int place)
        {
            for (int k = 0; k < place; k++) scaled /= 10;
            return (int)(scaled % 10);
        }
    }
}
