using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>R8 model parçaları: kanal arama, sinyal ibresi ve tüpün ışıması. Yalnız sunum durumu.</summary>
    public sealed class CrtInstruments : MonoBehaviour
    {
        [SerializeField] Renderer glass, led;
        [SerializeField] Canvas face;
        [SerializeField] Light spill, dialLight;
        [SerializeField] PartDriver dial, needle, tune;
        MaterialPropertyBlock block;
        Color emission, ledEmission;
        float spillIntensity, approachAt = -1, signal = .5f;
        bool powered = true;
        public float Boost { get; private set; } = 1;
        public bool Powered => powered;
        void Awake()
        {
            block = new MaterialPropertyBlock();
            emission = glass && glass.sharedMaterial.HasProperty("_EmissionColor") ? glass.sharedMaterial.GetColor("_EmissionColor") : Color.black;
            ledEmission = led && led.sharedMaterial.HasProperty("_EmissionColor") ? led.sharedMaterial.GetColor("_EmissionColor") : Color.green;
            spillIntensity = spill ? spill.intensity : 0;
        }
        public void Bind(CrtScreenData data)
        {
            signal = data.State == CrtState.Disabled ? 0 : data.State == CrtState.Live ? 1 : data.State == CrtState.Establishing ? .8f : .5f;
            if (needle) needle.AnimateTo(Mathf.Lerp(needle.RangeFrom, needle.RangeTo, signal), SiginakTheme.Active.NumberOf("motion.crtInstrumentS", .3f));
        }
        public void BeginApproach() { approachAt = Time.unscaledTime; Power(true); }
        public void CompleteApproach() { approachAt = -1; Illuminate(1.4f); if (dial) dial.Set(0); if (tune) tune.Set(0); }
        public void Power(bool on) { powered = on; if (!on) approachAt = -1; Illuminate(on ? 1 : 0); }
        void Illuminate(float boost)
        {
            Boost = boost;
            if (face) face.enabled = powered;
            if (spill) { spill.enabled = powered; spill.intensity = spillIntensity * boost; }
            if (dialLight) dialLight.enabled = powered;
            Emission(glass, emission * boost); Emission(led, ledEmission * (powered ? 1 : 0));
        }
        void Emission(Renderer renderer, Color value)
        {
            if (!renderer) return;
            block ??= new MaterialPropertyBlock(); renderer.GetPropertyBlock(block); block.SetColor("_EmissionColor", value); renderer.SetPropertyBlock(block);
        }
        void Update()
        {
            if (approachAt < 0) return;
            float t = Time.unscaledTime - approachAt;
            float seconds = SiginakTheme.Active.NumberOf("motion.crtDiveS", 2.4f);
            float search = SiginakTheme.Active.NumberOf("motion.crtInstrumentS", .3f);
            float wave = Mathf.Sin(Mathf.Clamp01(t / Mathf.Max(.01f, search)) * Mathf.PI);
            if (dial) dial.Set(-.04f * wave);
            if (tune) tune.Set(-50 * wave);
            Illuminate(Mathf.Lerp(1, 1.4f, Mathf.Clamp01((t - seconds + .6f) / .6f)));
        }
    }
}
