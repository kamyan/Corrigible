using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sayaç modülünün altına asılan ± fişi (K3 notu › geri bildirim): teleks kâğıdı, “+” yeşil, “−” kırmızı, “±0” gri;
    /// işaret rengi tek başına taşımaz. Sallanarak iner (0,25 s, iki salınım), 1,5 s asılı kalır, 0,2 s'de solar.
    /// Vardiya raporu açıkken iğnelenir (K6b): <see cref="Release"/> gelene kadar asılı kalır.
    /// <para>KOS aşama 4: dinlenme metni (<see cref="SetRest"/>; ör. ERZAK sayacında "AÇ 2", "YARIM TAYIN 3 V", "KARARTMA") varken fiş
    /// görünür ve iğneli durur; geçici fark fişi solunca ya da iğne kalkınca dinlenme metnine döner. null dinlenme metnini kaldırır.</para>
    /// </summary>
    public sealed class DeltaSlip : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Renderer paper;
        [SerializeField] TMP_Text label;

        float shownAt = -100, hold = 1.5f, fade = 0.2f, swing = 0.25f;
        bool pinned, showingRest;
        string restText;
        /// <summary>Dinlenme metninin mürekkebi; yalnız <see cref="SetRest"/> yazar (çağıran temadan okur).</summary>
        Color restInk;
        Color paperColor = Color.white;
        MaterialPropertyBlock block;

        public bool Visible => gameObject.activeSelf;
        /// <summary>Dinlenme metni (yoksa null).</summary>
        public string RestText => restText;
        /// <summary>Fiş şu an dinlenme metnini gösteriyor.</summary>
        public bool ShowingRest => showingRest && gameObject.activeSelf;
        /// <summary>Fişte yazan metin (görünmüyorsa null).</summary>
        public string Text => gameObject.activeSelf && label != null ? label.text : null;

        public void Configure(Renderer paperRenderer, TMP_Text text) { paper = paperRenderer; label = text; }

        /// <summary>Fişi gösterir; süreler tokenlardan (motion.deltaTagHoldS, deltaTagFadeS). İğneli fiş kendiliğinden solmaz.</summary>
        public void Show(string text, Color ink, bool pin = false)
        {
            pinned = pin;
            showingRest = false;
            var t = SiginakTheme.Active;
            if (t != null)
            {
                hold = t.Number("motion.deltaTagHoldS", 1.5f);
                fade = t.Number("motion.deltaTagFadeS", 0.2f);
                paperColor = t.ColorOf("ui.slipTag[0]");
            }
            label.text = text;
            label.color = ink;
            shownAt = Time.unscaledTime;
            gameObject.SetActive(true);
            Apply(1, 0);
        }

        /// <summary>Geçici fiş kalkar: dinlenme metni varsa ona döner, yoksa fiş gizlenir.</summary>
        public void Hide()
        {
            pinned = false;
            if (restText != null) { ShowRest(); return; }
            showingRest = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Dinlenme metni: fiş boştayken (ya da zaten dinlenme metnindeyken) hemen yazılır; geçici ya da iğneli fiş sürüyorsa o kalkınca.
        /// null ya da boş metin dinlenme metnini kaldırır (dinlenme metni gösteriliyorsa fiş gizlenir).
        /// </summary>
        public void SetRest(string text, Color ink)
        {
            string next = string.IsNullOrEmpty(text) ? null : text;
            if (next == restText && ink == restInk) return;
            restText = next;
            restInk = ink;
            if (restText == null)
            {
                if (showingRest) { showingRest = false; gameObject.SetActive(false); }
                return;
            }
            if (!gameObject.activeSelf || showingRest) ShowRest();
        }

        void ShowRest()
        {
            pinned = false;
            showingRest = true;
            var t = SiginakTheme.Active;
            if (t != null) paperColor = t.ColorOf("ui.slipTag[0]");
            if (label != null) { label.text = restText; label.color = restInk; }
            gameObject.SetActive(true);
            Apply(1, 0);
        }

        /// <summary>İğneli fiş kalkar: şimdi solmaya başlar.</summary>
        public void Release()
        {
            if (!pinned) return;
            pinned = false;
            shownAt = Time.unscaledTime - hold;
        }

        void Update()
        {
            if (showingRest) return;
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
