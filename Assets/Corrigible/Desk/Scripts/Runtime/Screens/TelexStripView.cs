using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Teleks şeridi (C1 §7.1): sol plaka (lamba + TELEKS) ve RectMask2D'li pencerede kayan öğeler. İçerik görünümü dolduracak kadar yinelenir,
    /// ~26 px/sn sola kayar ve kesintisiz döner (üstüne gelince durur; F3). Öğe: gün, kaynak, metin, güven işareti;
    /// sahte haberde metin ink3 + 2 px kırmızı çizgi + kırmızı “SAHTE” damgası.
    /// </summary>
    public sealed class TelexStripView : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [Tooltip("Plaka metne göre uzar (C2: “TELEKS · 0 ms”); görünüm plakanın hemen sağından başlar.")]
        [SerializeField] RectTransform plate, view;
        [SerializeField] float plateLeftPad = 27, plateRightPad = 11;
        [Tooltip("Teleks kâğıdı dokusu (@2x); döşeme görünüm genişliğinden hesaplanır, plaka uzayınca noktalar sıkışmaz.")]
        [SerializeField] RawImage paper;
        [SerializeField] Image lamp;
        [SerializeField] RectTransform run;
        [SerializeField] TelexItemView itemTemplate;
        [SerializeField] float gap = 34;
        [SerializeField] float padLeft = 16;
        [Tooltip("Durağan görünümde başlangıç kayması (referans: −0,3 px).")]
        [SerializeField] float startOffset = -0.3f;
        [SerializeField] bool scroll = true;

        DeskPool<TelexItemView> pool;
        readonly List<TelexItemVM> boundItems = new List<TelexItemVM>();
        readonly List<float> starts = new List<float>(), widths = new List<float>();
        DeskLang lastLanguage;
        DeskTheme lastTheme;
        float cycle, coverageWidth = -1;
        double offset;
        int copies;
        bool hovered, hasBound, lastIllusion, plateReady, lampReady, lastLamp;
        string plateText;

        [SerializeField] DeskHover viewHover;

        public bool Paused { get => hovered; set => hovered = value; }
        public event System.Action ItemClicked;

        void Awake()
        {
            if (viewHover == null) return;
            viewHover.Entered += _ => hovered = true;
            viewHover.Exited += _ => hovered = false;
            viewHover.Clicked += _ => ItemClicked?.Invoke();
        }

        public void Bind(TelexVM t)
        {
            if (t == null) return;
            var translatedLabel = DeskLoc.Instance.T(t.Label);
            if (!plateReady || plateText != translatedLabel)
            {
                plateReady = true;
                plateText = translatedLabel;
                DeskUi.SetText(label, translatedLabel);
                if (plate != null && label != null)
                {
                    float lw = Mathf.Round(DeskUi.TextWidth(label, label.text));
                    float pw = plateLeftPad + lw + plateRightPad;
                    label.rectTransform.sizeDelta = new Vector2(lw + 2, label.rectTransform.sizeDelta.y);
                    plate.sizeDelta = new Vector2(pw, plate.sizeDelta.y);
                    if (view != null) view.offsetMin = new Vector2(pw, view.offsetMin.y);
                }
            }
            FitPaper();
            if (!lampReady || lastLamp != t.LampOn)
            {
                lampReady = true;
                lastLamp = t.LampOn;
                DeskSprites.Apply(lamp, t.LampOn ? "deco_lamp_on" : "deco_lamp_off");
            }
            if (!Application.isPlaying && !DeskUi.EditPreview) return;
            if (ContentMatches(t))
            {
                EnsureCoverage();
                return;
            }

            // Yeni bir haber öne eklendiğinde ekrandaki ilk ortak haberi aynı x konumunda tut.
            // Güven damgası veya dil değişimi haberi başka bir öğe yapmaz.
            int anchor = -1;
            double anchorX = double.PositiveInfinity;
            if (cycle > 0 && t.Items != null)
            {
                for (int i = 0; i < boundItems.Count; i++)
                {
                    int match = FindIdentity(t.Items, boundItems[i]);
                    if (match < 0) continue;
                    double x = padLeft + starts[i] + offset;
                    if (x + widths[i] <= 0) x += (System.Math.Floor(-(x + widths[i]) / cycle) + 1) * cycle;
                    if (x < anchorX) { anchor = match; anchorX = x; }
                }
            }

            pool ??= new DeskPool<TelexItemView>(itemTemplate, run);
            bool firstBind = !hasBound;
            hasBound = true;
            lastLanguage = DeskLoc.Instance.Lang;
            lastTheme = DeskTheme.Active;
            lastIllusion = DeskTheme.Illusion;
            boundItems.Clear(); starts.Clear(); widths.Clear();
            cycle = 0;
            int n = t.Items?.Count ?? 0;
            for (int k = 0; k < n; k++)
            {
                var item = pool.Get(k);
                item.Bind(t.Items[k]);
                boundItems.Add(TelexItemView.CopyContent(t.Items[k]));
                starts.Add(cycle);
                widths.Add(Mathf.Max(0, item.Width));
                cycle += Mathf.Max(1, widths[k] + gap);
            }
            if (anchor >= 0) offset = anchorX - padLeft - starts[anchor];
            else if (firstBind || n == 0) offset = startOffset;
            NormalizeOffset();
            coverageWidth = -1;
            EnsureCoverage();
            ApplyOffset();
        }

        bool ContentMatches(TelexVM t)
        {
            if (!hasBound || lastLanguage != DeskLoc.Instance.Lang || lastTheme != DeskTheme.Active || lastIllusion != DeskTheme.Illusion) return false;
            if (boundItems.Count != (t.Items?.Count ?? 0)) return false;
            for (int i = 0; i < boundItems.Count; i++) if (!TelexItemView.SameContent(boundItems[i], t.Items[i])) return false;
            return true;
        }

        static int FindIdentity(List<TelexItemVM> items, TelexItemVM old)
        {
            if (old == null) return -1;
            for (int i = 0; i < items.Count; i++)
            {
                var candidate = items[i];
                if (candidate == null) continue;
                if (!string.IsNullOrEmpty(old.Id) && !string.IsNullOrEmpty(candidate.Id))
                {
                    if (old.Id == candidate.Id) return i;
                }
                else if (old.Day == candidate.Day && old.Source == candidate.Source && old.Text == candidate.Text) return i;
            }
            return -1;
        }

        void EnsureCoverage()
        {
            if (pool == null || run == null) return;
            float width = view != null ? Mathf.Max(0, view.rect.width) : 0;
            if (Mathf.Approximately(width, coverageWidth)) return;
            coverageWidth = width;
            int n = boundItems.Count;
            // Tam görünüm + bir döngü: en kısa tek haber de geniş ekranda boşluğa düşmez.
            copies = cycle > 0 ? Mathf.Max(2, Mathf.CeilToInt((width + Mathf.Abs(padLeft)) / cycle) + 1) : 0;
            for (int k = 0; k < copies * n; k++)
            {
                int i = k % n;
                var item = pool.Get(k);
                item.Bind(boundItems[i]);
                var rt = (RectTransform)item.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
                rt.pivot = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(padLeft + starts[i] + (k / n) * cycle, 0);
            }
            pool.Trim(copies * n);
            run.sizeDelta = new Vector2(n > 0 ? padLeft + copies * cycle : 0, run.sizeDelta.y);
        }

        void OnRectTransformDimensionsChange() => FitPaper();
        void OnDisable() => hovered = false;

        /// <summary>Nokta dokusu CSS'teki gibi görünümün sol kenarından başlar; döşeme boyu doku @2x / 2.</summary>
        void FitPaper()
        {
            if (paper == null || paper.texture == null) return;
            var r = paper.rectTransform.rect;
            float tw = paper.texture.width * 0.5f, th = paper.texture.height * 0.5f;
            var uv = new Rect(0, 0, r.width / tw, r.height / th);
            if (paper.uvRect != uv) paper.uvRect = uv;
        }

        void Update()
        {
            EnsureCoverage();
            Advance(Time.unscaledDeltaTime);
        }

        void Advance(double seconds)
        {
            if (!scroll || hovered || cycle <= 0 || run == null || DeskTheme.Active == null) return;
            offset -= DeskTheme.Active.telexSpeed * seconds;
            NormalizeOffset();
            ApplyOffset();
        }

        void NormalizeOffset()
        {
            if (cycle > 0) offset -= System.Math.Ceiling(offset / cycle) * cycle;
        }

        void ApplyOffset()
        {
            if (run != null) run.anchoredPosition = new Vector2((float)offset, run.anchoredPosition.y);
        }

        /// <summary>Durağan yakalama için kaymayı durdurur ve referans konumuna getirir.</summary>
        public void Freeze(bool on)
        {
            scroll = !on;
            if (on) { offset = startOffset; NormalizeOffset(); ApplyOffset(); }
        }
    }
}
