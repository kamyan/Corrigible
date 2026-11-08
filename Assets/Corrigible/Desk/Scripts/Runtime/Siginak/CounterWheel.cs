using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Mekanik sayacın tek çarkı (sprites/README › Sayaç çarkı, shader'sız): Quad'ın dokusu 11 hücreli rakam şeridi
    /// (0–9 + ikinci 0), Tiling (1, 1/11), rakam d için Offset.y = 1 − (d + 1)/11. Konum sürekli tutulur; 9 → 0 geçişi
    /// ikinci 0 hücresinden dolaşır, görüntü sıçramaz. Doku kaydırması paylaşılan malzemeye dokunmaz: Play'de çarkın kendi
    /// malzeme kopyasıyla (SRP Batcher uyumlu; property block her çarkı batcher dışına itiyordu, Aşama 10 performans), editörde
    /// MaterialPropertyBlock ile. Revizyon 3: <see cref="drum"/> doluysa modelin tamburu döner (rakam d için +36·d derece, yerel x;
    /// 0° “0”'ı gösterir), doku kaydırılmaz.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class CounterWheel : MonoBehaviour
    {
        static readonly int StId = Shader.PropertyToID("_BaseMap_ST");
        const int Cells = 11;

        [SerializeField] Renderer face;
        [Tooltip("Revizyon 3 sayaç rafının tamburu (Wheel_* parçası); doluysa doku yerine tambur döner.")]
        [SerializeField] PartDriver drum;
        [Tooltip("R2/R3 silindirinin yatay UV şeridi; oda tamburunun dikey UV düzeninden farklıdır.")]
        [SerializeField] bool lateralDrum;

        float position, from, to, start, duration, delay;
        bool rolling;
        int lastWhole;
        MaterialPropertyBlock block;
        Material instance;

        /// <summary>Çark bir rakam geçtiğinde (tık sesi için).</summary>
        public event Action Ticked;
        public bool Rolling => rolling;
        public int Digit => Mathf.RoundToInt(Mathf.Repeat(rolling ? to : position, 10)) % 10;

        void Awake() { if (face == null) face = GetComponent<Renderer>(); }

        void OnDestroy()
        {
            if (instance != null) Destroy(instance);
        }

        /// <summary>Animasyonsuz rakam.</summary>
        public void Set(int digit)
        {
            rolling = false;
            position = from = to = Mathf.Clamp(digit, 0, 9);
            lastWhole = Mathf.RoundToInt(position);
            Apply(position);
        }

        /// <summary>
        /// Rakama yuvarlanır: <paramref name="direction"/> &gt; 0 ileri (değer arttı), &lt; 0 geri. Süre ease-out, gecikme
        /// çark sırası içindir (motion.counterRollS, counterWheelDelayS).
        /// </summary>
        public void RollTo(int digit, int direction, float seconds, float delaySeconds)
        {
            int current = Digit;
            digit = Mathf.Clamp(digit, 0, 9);
            if (digit == current && !rolling) return;
            if (rolling) position = Mathf.Repeat(to, 10);
            int steps = direction >= 0 ? (digit - current + 10) % 10 : -((current - digit + 10) % 10);
            if (steps == 0) { Set(digit); return; }
            from = position;
            to = position + steps;
            start = Time.unscaledTime;
            duration = Mathf.Max(0.01f, seconds);
            delay = delaySeconds;
            lastWhole = Mathf.RoundToInt(from);
            rolling = true;
        }

        void Update()
        {
            if (!rolling) return;
            float t = Mathf.Clamp01((Time.unscaledTime - start - delay) / duration);
            float eased = 1 - Mathf.Pow(1 - t, 3);
            position = Mathf.Lerp(from, to, eased);
            int whole = Mathf.RoundToInt(position);   // görünen rakam değişince tık
            if (whole != lastWhole) { lastWhole = whole; Ticked?.Invoke(); }
            if (t >= 1) { rolling = false; position = Mathf.Repeat(to, 10); }
            Apply(position);
        }

        /// <summary>Tambur kipi (Revizyon 3); görünüm bağlantısı ve testler için.</summary>
        public void ConfigureDrum(PartDriver drumPart) => drum = drumPart;

        void Apply(float p)
        {
            float c = Mathf.Repeat(p, 10f);
            if (drum != null) { drum.Set(lateralDrum?Mathf.Repeat(162-c*36,360):c*36f); return; }
            if (face == null) face = GetComponent<Renderer>();
            var st = new Vector4(1, 1f / Cells, 0, 1 - (c + 1) / Cells);
            if (Application.isPlaying)
            {
                instance ??= face.material;
                instance.SetVector(StId, st);
                return;
            }
            block ??= new MaterialPropertyBlock();
            face.GetPropertyBlock(block);
            block.SetVector(StId, st);
            face.SetPropertyBlock(block);
        }
    }
}
