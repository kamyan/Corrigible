using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Teleks şeridi (C1 §7.1): sol plaka (lamba + TELEKS) ve RectMask2D'li pencerede kayan öğeler. İçerik iki kopya dizilir,
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
        TelexVM lastBound;
        float cycle, offset;
        bool hovered;

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
            DeskUi.SetText(label, DeskLoc.Instance.T(t.Label));
            if (plate != null)
            {
                float lw = Mathf.Round(DeskUi.TextWidth(label, label.text));
                float pw = plateLeftPad + lw + plateRightPad;
                label.rectTransform.sizeDelta = new Vector2(lw + 2, label.rectTransform.sizeDelta.y);
                plate.sizeDelta = new Vector2(pw, plate.sizeDelta.y);
                if (view != null) { view.offsetMin = new Vector2(pw, view.offsetMin.y); }
            }
            FitPaper();
            DeskSprites.Apply(lamp, t.LampOn ? "deco_lamp_on" : "deco_lamp_off");
            if (!Application.isPlaying && !DeskUi.EditPreview) return;
            if (t == lastBound && cycle > 0) return;   // aynı haber listesi: akış kaldığı yerden sürer
            lastBound = t;
            pool ??= new DeskPool<TelexItemView>(itemTemplate, run);
            float x = padLeft;
            int n = t.Items.Count;
            // İki kopya: döngü kesintisiz görünsün
            for (int k = 0; k < 2 * n; k++)
            {
                var item = pool.Get(k);
                item.Bind(t.Items[k % n]);
                var rt = (RectTransform)item.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f); rt.pivot = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(x, 0);
                x += item.Width + gap;
                if (k == n - 1) cycle = x - padLeft;
            }
            pool.Trim(2 * n);
            run.sizeDelta = new Vector2(x, run.sizeDelta.y);
            offset = startOffset;
            run.anchoredPosition = new Vector2(offset, run.anchoredPosition.y);
        }

        void OnRectTransformDimensionsChange() => FitPaper();

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
            if (!scroll || hovered || cycle <= 0 || DeskTheme.Active == null) return;
            offset -= DeskTheme.Active.telexSpeed * Time.unscaledDeltaTime;
            if (offset <= -cycle) offset += cycle;
            run.anchoredPosition = new Vector2(offset, run.anchoredPosition.y);
        }

        /// <summary>Durağan yakalama için kaymayı durdurur ve referans konumuna getirir.</summary>
        public void Freeze(bool on)
        {
            scroll = !on;
            if (on) { offset = startOffset; run.anchoredPosition = new Vector2(offset, run.anchoredPosition.y); }
        }
    }
}
