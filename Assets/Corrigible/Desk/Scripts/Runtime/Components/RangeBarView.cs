using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Aralık çubuğu (dosya SIZMA, C1 .rng): 1,5 px çizgi (y 8), çentikler (1×9, y 4), olası aralık bandı
    /// (turuncu tarama %75, y 5, h 7), işaretler (3×15, y 1) ve etiketler (Courier 700 11, y 15).
    /// Çentik/işaret/etiket öğeleri şablondan havuzlanır; yalnızca veri değişince yeniden yerleşir.
    /// </summary>
    [ExecuteAlways]
    public sealed class RangeBarView : MonoBehaviour
    {
        [Serializable]
        public struct Marker
        {
            public float at;
            public Color color;
            public string label;
            public Color labelColor;
            [Tooltip("Etiket işaretin solunda mı?")] public bool labelLeft;
        }

        [SerializeField] float min, max = 100;
        [SerializeField] float[] ticks = { 0, 50, 100 };
        [SerializeField] bool hasBand = true;
        [SerializeField] Vector2 band = new Vector2(29, 47);
        [SerializeField] List<Marker> markers = new List<Marker>();

        [SerializeField] Image track;
        [SerializeField] Image bandImage;
        [SerializeField] Image tickTemplate;
        [SerializeField] Image markerTemplate;
        [SerializeField] TMP_Text labelTemplate;

        readonly List<Image> tickPool = new List<Image>();
        readonly List<Image> markerPool = new List<Image>();
        readonly List<TMP_Text> labelPool = new List<TMP_Text>();

        void OnEnable() { DeskTheme.Changed += Apply; Apply(); }
        void OnDisable() => DeskTheme.Changed -= Apply;
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(float rangeMin, float rangeMax, float[] tickValues, Vector2? bandRange, List<Marker> markerList)
        {
            min = rangeMin; max = rangeMax; ticks = tickValues ?? Array.Empty<float>();
            hasBand = bandRange.HasValue; if (hasBand) band = bandRange.Value;
            markers = markerList ?? new List<Marker>();
            Apply();
        }

        float X(float v, float w) => Mathf.Approximately(max, min) ? 0 : (v - min) / (max - min) * w;

        public void Apply()
        {
            if (track == null) return;
            float w = ((RectTransform)transform).rect.width;

            DeskSprites.Apply(track, "sh_fill_r0");
            DeskUi.Tint(track, "ink.ink2");
            Place(track.rectTransform, 0, 8, w, 1);   // CSS 1,5 px → Chrome 1 px (TSV)

            DeskUi.SetActive(bandImage, hasBand);
            if (hasBand)
            {
                DeskSprites.Apply(bandImage, "pt_hatch");
                DeskUi.Tint(bandImage, "trust.suspicious.line", 0.75f);
                float a = X(band.x, w), b = X(band.y, w);
                Place(bandImage.rectTransform, a, 5, b - a, 7);
            }

            if (!Ensure(tickPool, tickTemplate, ticks.Length)) return;   // prefab asset'inde havuz yok
            for (int i = 0; i < ticks.Length; i++)
            {
                var t = tickPool[i];
                DeskSprites.Apply(t, "sh_fill_r0"); DeskUi.Tint(t, "ink.ink2");
                Place(t.rectTransform, Mathf.Min(X(ticks[i], w), w - 1), 4, 1, 9);
            }

            if (!Ensure(markerPool, markerTemplate, markers.Count) || !Ensure(labelPool, labelTemplate, markers.Count)) return;
            for (int i = 0; i < markers.Count; i++)
            {
                var m = markers[i];
                var img = markerPool[i];
                DeskSprites.Apply(img, "sh_fill_r0");
                DeskUi.SetColor(img, m.color);
                float x = X(m.at, w);
                Place(img.rectTransform, Mathf.Round(x - 1.5f), 1, 3, 15);

                var lbl = labelPool[i];
                DeskUi.SetText(lbl, m.label);
                DeskUi.SetColor(lbl, m.labelColor);
                float lw = Mathf.Round(DeskUi.TextWidth(lbl, m.label));
                lbl.alignment = m.labelLeft ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                Place(lbl.rectTransform, m.labelLeft ? x - 4 - lw : x + 4, 15, lw, 11);
            }
        }

        static bool Ensure<T>(List<T> pool, T template, int count) where T : Component
        {
            // Prefab asset'inde (sahne dışı) havuz kopyası yaratılamaz; yalnızca sahnedeki örnekte
            if (template == null || !template.gameObject.scene.IsValid()) return false;
            DeskUi.SetActive(template, false);
            pool.RemoveAll(p => p == null);
            if (pool.Count == 0)
                foreach (Transform c in template.transform.parent)
                    if (c != template.transform && c.name.StartsWith(template.name + "#") && c.TryGetComponent<T>(out var e)) pool.Add(e);
            while (pool.Count < count)
            {
                var copy = Instantiate(template, template.transform.parent);
                copy.name = template.name + "#" + pool.Count;
                copy.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                pool.Add(copy);
            }
            for (int i = 0; i < pool.Count; i++) DeskUi.SetActive(pool[i], i < count);
            return true;
        }

        static void Place(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        }
    }
}
