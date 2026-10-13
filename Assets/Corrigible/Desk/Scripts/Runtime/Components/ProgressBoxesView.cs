using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Hedef kartı kutuları (C1 .bx): 7 × (22×12), aralık 4, 1 px ink2 kenar; dolu kısım “/” tarama (#4A4640), son kutu kısmi.
    /// Kutular prefab'da hazır durur; burada yalnızca tarama genişlikleri ayarlanır.
    /// </summary>
    [ExecuteAlways]
    public sealed class ProgressBoxesView : MonoBehaviour
    {
        [Range(0, 1)] [SerializeField] float value = 0.42f;
        [SerializeField] Image[] frames = System.Array.Empty<Image>();
        [SerializeField] Image[] hatches = System.Array.Empty<Image>();

        const float BoxW = 22, BoxH = 12, Gap = 4;

        void OnEnable() { DeskTheme.Changed += Apply; Apply(); }
        void OnDisable() => DeskTheme.Changed -= Apply;
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            if (Mathf.Approximately(value, fraction)) return;
            value = fraction; Apply();
        }

        public void Apply()
        {
            int n = Mathf.Min(frames.Length, hatches.Length);
            float filled = value * n;
            for (int i = 0; i < n; i++)
            {
                var f = frames[i]; var h = hatches[i];
                if (f == null || h == null) continue;
                DeskSprites.Apply(f, "sh_frame_r0_1");
                DeskUi.Tint(f, "ink.ink2");
                Place(f.rectTransform, i * (BoxW + Gap), BoxW);
                float part = Mathf.Clamp01(filled - i);
                DeskUi.SetActive(h, part > 0.001f);
                DeskSprites.Apply(h, "pt_hatch");
                DeskUi.Tint(h, "ink.graphite");
                // Tarama kenarın içinde: 1 px içeriden
                Place(h.rectTransform, i * (BoxW + Gap) + 1, (BoxW - 2) * part, BoxH - 2, -1);
            }
            var rt = (RectTransform)transform;
            var size = new Vector2(n * BoxW + Mathf.Max(0, n - 1) * Gap, BoxH);
            if (rt.sizeDelta != size) rt.sizeDelta = size;
        }

        static void Place(RectTransform r, float x, float w, float h = BoxH, float y = 0)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h);
        }
    }
}
