using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Rehberin işaretçisi (geliştirici, 2026-11-24: "adım adım göstererek, şuraya bas şuraya bas"): hedefin ekran dikdörtgeninin
    /// çevresinde nabız atan bir çerçeve ve üstünde (yer yoksa altında) kısa bir kâğıt etiket ("BURAYA BAS", "MUTFAK"…). Rehber notunun
    /// tuvalinde (ekran üstü), tıklamayı yutmaz. Yalnız gösterir; hedefi <c>SiginakTutorial</c> çözer. Renkler SiginakTheme'den
    /// (<c>ui.focusRing</c>, <c>paper</c>, <c>ink</c>).
    /// </summary>
    public sealed class TutorialPointerView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [Tooltip("Çerçevenin dört kenarı: üst, alt, sol, sağ.")]
        [SerializeField] Image top, bottom, left, right;
        [SerializeField] RectTransform tag;
        [SerializeField] Image tagPaper;
        [SerializeField] TMP_Text tagText;
        [Tooltip("Çerçevenin hedefin dışındaki boşluğu (px) ve nabzın genliği.")]
        [SerializeField, Min(0)] float padding = 8, pulse = 6;
        [SerializeField, Min(0.1f)] float pulseSeconds = 0.9f;
        [SerializeField, Min(1)] float thickness = 4;

        RectTransform area;
        Rect target;
        bool visible;
        string shownLabel;

        /// <summary>Gösteriliyor mu ve hedefin ekran dikdörtgeni (testler).</summary>
        public bool Visible => visible;
        public Rect Target => target;
        public string Label => shownLabel;

        void Awake()
        {
            area = (RectTransform)transform;
            var theme = SiginakTheme.Active;
            if (theme != null)
            {
                var ring = theme.ColorOf("ui.focusRing");
                foreach (var i in new[] { top, bottom, left, right }) if (i != null) i.color = ring;
                if (tagPaper != null) tagPaper.color = ring;
                if (tagText != null) tagText.color = theme.ColorOf("ink");
            }
            foreach (var g in GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            if (group != null) { group.alpha = 0; group.blocksRaycasts = false; group.interactable = false; }
        }

        /// <summary>Hedefin çevresini işaretler (<paramref name="screen"/>: piksel, sol alt başlangıç); <paramref name="label"/> etiket metni.</summary>
        public void Point(Rect screen, string label)
        {
            target = screen; visible = true;
            if (label != shownLabel) { shownLabel = label; if (tagText != null) tagText.text = label ?? ""; }
            if (tag != null) tag.gameObject.SetActive(!string.IsNullOrEmpty(label));
        }

        public void Hide() => visible = false;

        void LateUpdate()
        {
            if (group != null) group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1 : 0, Time.unscaledDeltaTime / 0.15f);
            if (!visible || area == null) return;
            float s = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / pulseSeconds);
            float pad = padding + pulse * s;
            var canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, new Vector2(target.xMin - pad, target.yMin - pad), cam, out var lo);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, new Vector2(target.xMax + pad, target.yMax + pad), cam, out var hi);
            float w = hi.x - lo.x, h = hi.y - lo.y;
            Place(top, new Vector2(lo.x, hi.y - thickness), new Vector2(w, thickness));
            Place(bottom, new Vector2(lo.x, lo.y), new Vector2(w, thickness));
            Place(left, new Vector2(lo.x, lo.y), new Vector2(thickness, h));
            Place(right, new Vector2(hi.x - thickness, lo.y), new Vector2(thickness, h));
            if (tag != null && tag.gameObject.activeSelf)
            {
                // Etiket çerçevenin üstünde ortada; tuvalin üst kenarına sığmazsa altında. Hafif zıplar.
                var size = tag.rect.size;
                float bob = 4 * s;
                bool below = hi.y + size.y + 10 > area.rect.yMax;
                float x = Mathf.Clamp((lo.x + hi.x) / 2, area.rect.xMin + size.x / 2, area.rect.xMax - size.x / 2);
                float y = below ? lo.y - 8 - size.y / 2 - bob : hi.y + 8 + size.y / 2 + bob;
                tag.anchoredPosition = new Vector2(x, y) - AreaOffset(tag);
            }
        }

        /// <summary>Alanın merkezine göre yerel noktadan çocuğun anchoredPosition'ına (çocuk merkez çapalı, pivot ortada).</summary>
        Vector2 AreaOffset(RectTransform child) => Vector2.Scale(area.rect.size, child.anchorMin - new Vector2(0.5f, 0.5f)) + area.rect.center;

        void Place(Image bar, Vector2 min, Vector2 size)
        {
            if (bar == null) return;
            var rt = bar.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = Vector2.zero;
            rt.sizeDelta = size;
            rt.anchoredPosition = min - area.rect.center;
        }
    }
}
