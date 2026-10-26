using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sayaç modülünün altına asılan ± fişi (K3 notu › geri bildirim): teleks kâğıdı, “+” yeşil, “−” kırmızı, “±0” gri;
    /// işaret rengi tek başına taşımaz. Sallanarak iner (0,25 s, iki salınım), 1,5 s asılı kalır, 0,2 s'de solar.
    /// Vardiya raporu açıkken iğnelenir (K6b): <see cref="Release"/> gelene kadar asılı kalır.
    /// </summary>
    public sealed class DeltaSlip : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Renderer paper;
        [SerializeField] TMP_Text label;

        float shownAt = -100, hold = 1.5f, fade = 0.2f, swing = 0.25f;
        bool pinned;
        Color paperColor = Color.white;
        MaterialPropertyBlock block;

        public bool Visible => gameObject.activeSelf;

        public void Configure(Renderer paperRenderer, TMP_Text text) { paper = paperRenderer; label = text; }

        /// <summary>Fişi gösterir; süreler tokenlardan (motion.deltaTagHoldS, deltaTagFadeS). İğneli fiş kendiliğinden solmaz.</summary>
        public void Show(string text, Color ink, bool pin = false)
        {
            pinned = pin;
            var t = SiginakTheme.Active;
            hold = t.Number("motion.deltaTagHoldS", 1.5f);
            fade = t.Number("motion.deltaTagFadeS", 0.2f);
            paperColor = t.ColorOf("ui.slipTag[0]");
            label.text = text;
            label.color = ink;
            shownAt = Time.unscaledTime;
            gameObject.SetActive(true);
            Apply(1, 0);
        }

        public void Hide() { pinned = false; gameObject.SetActive(false); }

        /// <summary>İğneli fiş kalkar: şimdi solmaya başlar.</summary>
        public void Release()
        {
            if (!pinned) return;
            pinned = false;
            shownAt = Time.unscaledTime - hold;
        }

        void Update()
        {
            float age = Time.unscaledTime - shownAt;
            if (pinned) age = Mathf.Min(age, hold);
            if (age > hold + fade) { Hide(); return; }
            float alpha = age <= hold ? 1 : 1 - (age - hold) / fade;
            // İki salınımlı sönümlü sarkaç: asma noktası üst kenar
            float angle = age < swing ? Mathf.Sin(age / swing * Mathf.PI * 2) * 6 * (1 - age / swing) : 0;
            Apply(alpha, angle);
        }

        void Apply(float alpha, float angle)
        {
            transform.localRotation = Quaternion.Euler(0, 0, angle);
            if (paper != null)
            {
                block ??= new MaterialPropertyBlock();
                paper.GetPropertyBlock(block);
                var c = paperColor; c.a = alpha;
                block.SetColor(ColorId, c);
                paper.SetPropertyBlock(block);
            }
            if (label != null) label.alpha = alpha;
        }
    }
}
