using System;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sayaç rafının tek modülü (K1 notu): pirinç plaka, pencerede çarklar, isteğe bağlı onda bir çarkı (FON; Revizyon 2:
    /// açık renk çark, önünde pirinç virgül) ve sabit ek (“/40”). Değer değişince çarklar sağdan sola 0,12 s arayla
    /// yuvarlanır, altına ± fişi asılır.
    /// </summary>
    public sealed class CounterModule : MonoBehaviour
    {
        [Tooltip("Tam sayı haneleri, soldan sağa.")]
        [SerializeField] CounterWheel[] wheels = Array.Empty<CounterWheel>();
        [Tooltip("Onda bir çarkı (yalnız FON).")]
        [SerializeField] CounterWheel tenths;
        [Tooltip("Ondalık işareti (yalnız onda bir çarkı olan modülde): Türkçede pirinç virgül, İngilizcede kuyruksuz nokta.")]
        [SerializeField] GameObject decimalComma, decimalPoint;
        [SerializeField] TMP_Text suffix;
        [SerializeField] DeltaSlip slip;

        [Tooltip("Yetersiz kaynakta titreme (Revizyon 2 · S2: “basınca FON sayacı iki kez titrer”): genlik ve bir gidiş-dönüş süresi.")]
        [SerializeField, Min(0)] float shakeMeters = 0.0025f;
        [SerializeField, Min(0.01f)] float shakeSeconds = 0.09f;

        double value;
        long shown = -1;
        Vector3 rest;
        float shakeAt = -1, raised;
        int shakes;

        public event Action Ticked;
        public double Value => value;
        int Scale => tenths != null ? 10 : 1;
        public double Max => Math.Pow(10, wheels.Length) - 1.0 / Scale;

        public void Configure(CounterWheel[] integerWheels, CounterWheel tenthsWheel, TMP_Text suffixText, DeltaSlip deltaSlip,
            GameObject comma = null, GameObject point = null)
        {
            wheels = integerWheels; tenths = tenthsWheel; suffix = suffixText; slip = deltaSlip; decimalComma = comma; decimalPoint = point;
        }

        void Awake()
        {
            rest = transform.localPosition;
            foreach (var w in wheels) if (w != null) w.Ticked += Tick;
            if (tenths != null) tenths.Ticked += Tick;
        }

        void OnDestroy()
        {
            foreach (var w in wheels) if (w != null) w.Ticked -= Tick;
            if (tenths != null) tenths.Ticked -= Tick;
        }

        void OnEnable() { DeskLoc.Instance.Changed += ApplyDecimal; ApplyDecimal(); }
        void OnDisable() => DeskLoc.Instance.Changed -= ApplyDecimal;

        void ApplyDecimal()
        {
            bool en = DeskFormat.Lang == DeskLang.En;
            if (decimalComma != null) decimalComma.SetActive(!en);
            if (decimalPoint != null) decimalPoint.SetActive(en);
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
                // Artı yeşil, eksi mürekkep: kırmızı yalnız işgal ve tehdit içindir (Revizyon 2 · renk kuralı)
                double delta = (scaled - shown) / (double)Scale;
                slip.Show(DeskFormat.Signed(delta, tenths != null ? 1 : 0), t.ColorOf(delta > 0 ? "status.good" : "ink"));
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

        /// <summary>Modül yerinde <paramref name="times"/> kez yana titrer (eksik kaynağı gösterir; değer değişmez).</summary>
        public void Shake(int times = 2)
        {
            shakes = Mathf.Max(1, times);
            shakeAt = Time.unscaledTime;
        }

        /// <summary>Vardiya raporunun özet gözünün üzerine gelince modül (ve fişi) biraz kalkar (Revizyon 2 · S5).</summary>
        public void Raise(bool on)
        {
            float target = on ? shakeMeters * 0.8f : 0;
            if (Mathf.Approximately(raised, target)) return;
            raised = target;
            if (shakeAt < 0) transform.localPosition = rest + Vector3.up * raised;
        }

        void Update()
        {
            if (shakeAt < 0) return;
            float p = (Time.unscaledTime - shakeAt) / shakeSeconds;
            if (p >= shakes)
            {
                shakeAt = -1;
                transform.localPosition = rest + Vector3.up * raised;
                return;
            }
            transform.localPosition = rest + Vector3.up * raised + Vector3.right * (Mathf.Sin(p * Mathf.PI * 2) * shakeMeters);
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
