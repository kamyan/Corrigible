using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Tavan ampulünün kırpışması (K1 notu › hareket): 3–9 s'de bir 2–3 kez, her biri 60–120 ms, şiddet %100 → %60.
    /// Işık şiddeti ve cam küresinin emissive'i birlikte düşer (paylaşılan malzemeye dokunmadan, MaterialPropertyBlock).
    /// <see cref="Scale"/> vardiya dizisinin karartmasıyla (K6) çarpılır. Süreler tokenlarda (motion.bulbFlicker).
    /// Revizyon 3: cam, çok malzemeli model renderer'ının bir yuvası olabilir (<see cref="glassSlot"/>); ampulün gölgesiz dolgu
    /// ışığı da (<see cref="fills"/>) aynı çarpanla kırpışır. Taban şiddetleri poz ışığı tarafından değiştirilebilir.
    /// </summary>
    public sealed class BulbFlicker : MonoBehaviour
    {
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] Light bulb;
        [SerializeField] Renderer glass;
        [Tooltip("Camın malzeme yuvası (model renderer'ında); −1 = renderer'ın tamamı (tek malzemeli eski cam).")]
        [SerializeField] int glassSlot = -1;
        [Tooltip("Ampulle birlikte kırpışan gölgesiz dolgu ışıkları.")]
        [SerializeField] Light[] fills = new Light[0];

        float baseIntensity;
        float[] fillBase = new float[0];
        Vector4 baseEmission;
        MaterialPropertyBlock block;
        float nextBurst, blinkEnd;
        int blinksLeft;
        bool dimmed;

        /// <summary>Dış karartma çarpanı (1 = tam); vardiya dizisi ampulü %30'a indirir.</summary>
        public float Scale { get; set; } = 1;

        /// <summary>Umut işareti (9/12): kırpışmalar seyrekleşir (aralık × 2,5).</summary>
        public bool Calm { get; set; }

        public void Configure(Light light, Renderer glassRenderer, int slot = -1, Light[] fillLights = null)
        {
            bulb = light; glass = glassRenderer; glassSlot = slot; fills = fillLights ?? new Light[0];
        }

        /// <summary>Ampulün taban şiddeti (kırpışma ve karartma bunun üstüne çarpılır); poz ışığı yazar.</summary>
        public float BaseIntensity { get => baseIntensity; set => baseIntensity = value; }

        /// <summary>Dolgu ışığının taban şiddeti.</summary>
        public void SetFillBase(int index, float intensity) { if (index >= 0 && index < fillBase.Length) fillBase[index] = intensity; }
        public float FillBase(int index) => index >= 0 && index < fillBase.Length ? fillBase[index] : 0;

        Material GlassMaterial => glass == null ? null : glassSlot < 0 ? glass.sharedMaterial
            : glassSlot < glass.sharedMaterials.Length ? glass.sharedMaterials[glassSlot] : null;

        void OnEnable()
        {
            if (bulb == null) { enabled = false; return; }
            baseIntensity = bulb.intensity;
            fillBase = new float[fills.Length];
            for (int i = 0; i < fills.Length; i++) fillBase[i] = fills[i] != null ? fills[i].intensity : 0;
            var glassMaterial = GlassMaterial;
            if (glassMaterial != null) baseEmission = glassMaterial.GetVector(EmissionId);   // doğrusal değer, dönüşümsüz
            block ??= new MaterialPropertyBlock();
            nextBurst = Time.time + Range("periodS", 3, 9);
        }

        void OnDisable() => Apply(1);

        /// <summary>Hemen bir kırpışma (ör. biri kapıyı yumrukladı): 2–3 kez, sonra her zamanki aralık.</summary>
        public void Burst()
        {
            if (!isActiveAndEnabled || blinksLeft > 0) return;
            nextBurst = Time.time;
        }

        void Update()
        {
            float now = Time.time;
            if (blinksLeft == 0 && now >= nextBurst)
            {
                blinksLeft = Mathf.RoundToInt(Range("blinks", 2, 3));
                blinkEnd = now;
            }
            if (blinksLeft > 0 && now >= blinkEnd)
            {
                dimmed = !dimmed;
                if (!dimmed) blinksLeft--;
                blinkEnd = now + Range("blinkS", 0.06f, 0.12f);
                if (blinksLeft == 0) nextBurst = now + Range("periodS", 3, 9) * (Calm ? 2.5f : 1);
            }
            float flicker = dimmed ? SiginakTheme.Active.Number("motion.bulbFlicker.min", 0.6f) : 1;
            Apply(flicker);
        }

        void Apply(float flicker)
        {
            if (bulb == null || block == null) return;
            float k = flicker * Scale;
            bulb.intensity = baseIntensity * k;
            for (int i = 0; i < fills.Length && i < fillBase.Length; i++) if (fills[i] != null) fills[i].intensity = fillBase[i] * k;
            if (glass == null) return;
            if (glassSlot < 0)
            {
                glass.GetPropertyBlock(block);
                block.SetVector(EmissionId, baseEmission * k);
                glass.SetPropertyBlock(block);
            }
            else
            {
                glass.GetPropertyBlock(block, glassSlot);
                block.SetVector(EmissionId, baseEmission * k);
                glass.SetPropertyBlock(block, glassSlot);
            }
        }

        static float Range(string key, float min, float max)
        {
            var t = SiginakTheme.Active;
            return Random.Range(t.Number("motion.bulbFlicker." + key + "[0]", min), t.Number("motion.bulbFlicker." + key + "[1]", max));
        }
    }
}
