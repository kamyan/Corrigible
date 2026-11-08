using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kabul kâğıtlarının (kimlik/sevk belgesi, talimat şeridi, bulgu fişi, talep formu; Revizyon 3 · Aşama 6, PLAN §1.6–§1.10, W3)
    /// ortak yüz tabanı: World Space tuval, 1 birim = 1 mm, sol üst orijin, y aşağı (<c>HandoverSlipFace</c> kalıbı). Kâğıt rengi
    /// kâğıdın Quad malzemesindedir; tuvalde yalnız yazılar, çizgiler, çerçeveler ve simgeler durur. Tuval ışın almaz
    /// (<c>GraphicRaycaster</c> yok; tık kâğıdın çarpıştırıcısından), yazılar fontun kendi (ışıksız) malzemesiyle (GELISTIRME
    /// "Işıklı TMP … mürekkebi soldurur").
    /// <list type="bullet">
    /// <item>Bağlama (<see cref="Request"/>) nesne kapalıyken gelirse açıldıktan sonraki ilk karede uygulanır (TMP ölçüsü uyanık yazı
    /// ister).</item>
    /// <item>Dil değişince (<c>DeskLoc.Changed</c>) yerleşim o karenin sonunda (<c>LateUpdate</c>) yinelenir: sahnedeki sabit yazılar
    /// (<c>DeskLocText</c>) aynı olayda çevrilir, ölçü onlardan sonra alınır.</item>
    /// <item>Satırlar sarar, punto küçülmez, otomatik punto yok; sığmazsa <see cref="Overflowing"/>.</item>
    /// </list>
    /// Yüzler <c>Loc</c> çağırmaz: dinamik metin anlık görüntüyle çevrilmiş gelir, sabit etiketler prefab'da <c>DeskLocText</c>'tir.
    /// Renkler <c>SiginakTheme</c>'den (yoksa tasarım değeri), ölçüler tasarım HTML'inin CSS pikselinden (<c>kagit_kaynak/*.html</c>).
    /// </summary>
    public abstract class AdmissionPaperFace : MonoBehaviour
    {
        /// <summary>Kıyas payı (mm).</summary>
        protected const float Eps = 0.01f;

        [Tooltip("Yüz tuvali (kâğıt ölçüsünde birim, sol üst pivot, ölçek 0,001; kâğıdın 0,4 mm önünde).")]
        [SerializeField] protected RectTransform face;
        [Tooltip("Yazıların kökü (tuvalin çocuğu, tuvali kaplar); boş yüzde kapalı.")]
        [SerializeField] protected RectTransform content;

        /// <summary>Bütün kabul kâğıdı yüzlerinde gerçekleşen yerleşim sayısı (test ve başarım).</summary>
        public static int LayoutCount;

        bool laidOut, pending, subscribed;
        string cachedLang;

        /// <summary>Bir yazı kâğıdın içerik alanını aştı (sarma sonrası bile).</summary>
        public bool Overflowing { get; protected set; }
        /// <summary>İçeriğin en alt kenarı (kâğıdın üstünden mm; son yerleşim).</summary>
        public float ContentBottomMm { get; protected set; }
        public RectTransform FaceRect => face;
        public RectTransform Content => content;
        /// <summary>Kâğıdın ölçüsü (mm).</summary>
        public abstract Vector2 SizeMm { get; }

        /// <summary>Yüzde çizilecek veri var (yoksa içerik kapanır).</summary>
        protected abstract bool HasData { get; }
        /// <summary>Veriyi yerleştirir (içerik açık, veri var); <see cref="Overflowing"/> ve <see cref="ContentBottomMm"/>'yi yazar.</summary>
        protected abstract void Layout();
        /// <summary>Veri kalkınca (boş yüz) yüzün kendi durumunu sıfırlar.</summary>
        protected virtual void OnCleared() { }

        // ================================================================== yaşam döngüsü

        /// <summary>Yerleşim ister: etkinse hemen, değilse açıldıktan sonraki ilk karede.</summary>
        protected void Request()
        {
            cachedLang = Loc.Lang;
            if (!gameObject.activeInHierarchy) { pending = true; return; }
            Relayout();
        }

        /// <summary>Aynı veriyi o anki dilde yeniden yazar (dil değişimi; testler de çağırır).</summary>
        public void Refresh() => Request();

        /// <summary>Bekleyen yerleşimi (etkinse) şimdi yapar; ölçü okuyan çağrılardan önce.</summary>
        protected void EnsureLaidOut()
        {
            if ((pending || !laidOut) && gameObject.activeInHierarchy) Relayout();
        }

        void Relayout()
        {
            pending = false;
            laidOut = true;
            LayoutCount++;
            bool show = HasData;
            if (content != null && content.gameObject.activeSelf != show) content.gameObject.SetActive(show);
            if (!show)
            {
                Overflowing = false;
                ContentBottomMm = 0;
                OnCleared();
                return;
            }
            Layout();
        }

        protected virtual void OnEnable()
        {
            if (laidOut && cachedLang != Loc.Lang) pending = true;
            if (subscribed) return;
            DeskLoc.Instance.Changed += OnLanguageChanged;
            subscribed = true;
        }

        protected virtual void OnDisable()
        {
            if (!subscribed) return;
            DeskLoc.Instance.Changed -= OnLanguageChanged;
            subscribed = false;
        }

        void OnLanguageChanged()
        {
            if (this == null) { DeskLoc.Instance.Changed -= OnLanguageChanged; return; }
            if (!laidOut && !pending) return;
            // Sabit etiketler (DeskLocText) aynı olayda çevrilir: ölçü karenin sonunda
            cachedLang = Loc.Lang;
            pending = true;
        }

        protected virtual void Update() => Advance(Time.unscaledDeltaTime);

        protected virtual void LateUpdate()
        {
            if (pending && gameObject.activeInHierarchy) Relayout();
        }

        /// <summary>Yüzün kendi kısa hareketleri (döküm, damga izi); edit modda testler doğrudan sürer.</summary>
        public virtual void Advance(float dt) { }

        // ================================================================== yerleşim yardımcıları

        /// <summary>
        /// Yazıyı yerleştirir: (<paramref name="x"/>, <paramref name="top"/>) satır kutusunun sol üstü (hizalama sağsa x sağ kenar),
        /// <paramref name="em"/> punto (mm), <paramref name="line"/> satır adımı (mm), <paramref name="spacingEm"/> harf aralığı (em).
        /// Boş metin yazıyı gizler ve 0 döner; yoksa kapladığı yükseklik (satır × adım).
        /// </summary>
        protected static float Put(TMP_Text t, string text, float x, float top, float width, float em, float line, float spacingEm,
            Color colour, bool wrap, TextAlignmentOptions align = TextAlignmentOptions.BaselineLeft)
        {
            if (t == null) return 0f;
            bool show = !string.IsNullOrEmpty(text);
            if (t.gameObject.activeSelf != show) t.gameObject.SetActive(show);
            if (!show) return 0f;
            HandoverSlipText.Style(t, em, spacingEm * 100f, colour, wrap, line);
            HandoverSlipText.Set(t, text);
            float w = wrap ? width : Mathf.Max(width, HandoverSlipText.Width(t, text));
            HandoverSlipText.PlaceLine(t, x, top + HandoverSlipText.Baseline(t, em, line), w, line, align);
            int lines = wrap ? Mathf.Max(1, HandoverSlipText.Lines(t)) : 1;
            return lines * line;
        }

        /// <summary>Tek satırlık yazının genişliği (mm; o anki stil ve puntoyla).</summary>
        protected static float WidthOf(TMP_Text t, string text, float em, float spacingEm)
        {
            if (t == null || string.IsNullOrEmpty(text)) return 0f;
            HandoverSlipText.Style(t, em, spacingEm * 100f, t.color, false);
            return HandoverSlipText.Width(t, text);
        }

        /// <summary>Fontun "normal" satır yüksekliği (CSS <c>line-height: normal</c>).</summary>
        protected static float NormalLine(TMP_Text t, float em) => HandoverSlipText.NormalLine(t, em);

        /// <summary>Düz dikdörtgen (çizgi, dolgu ya da simge): sol üst köşesi (x, top).</summary>
        protected static void Box(Graphic g, float x, float top, float width, float height, Color colour)
        {
            if (g == null) return;
            if (!g.gameObject.activeSelf) g.gameObject.SetActive(true);
            HandoverSlipText.Place(g.rectTransform, x, top, width, height, new Vector2(0, 1));
            if (g.color != colour) g.color = colour;
        }

        protected static void Show(Component c, bool on)
        {
            if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
        }

        protected static Color Fade(Color c, float alpha) => new Color(c.r, c.g, c.b, c.a * alpha);
    }

    /// <summary>
    /// Dört kenarlı çerçeve (tuvalde dört <see cref="Image"/>): düz (sprite'sız) ya da kesikli/noktalı (döşenen yatay ve dikey
    /// sprite). Kalınlık görüntü yüksekliğidir; döşenen sprite hücresi kalınlıktan yüksekse kırpılır.
    /// </summary>
    [Serializable]
    public sealed class AdmissionFrame
    {
        public Image top, right, bottom, left;

        public bool Wired => top != null && right != null && bottom != null && left != null;

        /// <summary>Çerçeveyi (x, y) sol üst köşeli, w × h dış ölçülü kutuya yerleştirir; sol kenar ayrıca kalın olabilir.</summary>
        public void Place(float x, float y, float w, float h, float thickness, Color colour, float leftThickness = -1f)
        {
            float lt = leftThickness > 0 ? leftThickness : thickness;
            Bar(top, x, y, w, thickness, colour);
            Bar(bottom, x, y + h - thickness, w, thickness, colour);
            Bar(left, x, y, lt, h, colour);
            Bar(right, x + w - thickness, y, thickness, h, colour);
        }

        public void SetActive(bool on)
        {
            foreach (var g in new[] { top, right, bottom, left })
                if (g != null && g.gameObject.activeSelf != on) g.gameObject.SetActive(on);
        }

        public Color Colour => top != null ? top.color : Color.clear;

        static void Bar(Image g, float x, float y, float w, float h, Color colour)
        {
            if (g == null) return;
            if (!g.gameObject.activeSelf) g.gameObject.SetActive(true);
            HandoverSlipText.Place(g.rectTransform, x, y, w, h, new Vector2(0, 1));
            if (g.color != colour) g.color = colour;
        }
    }

    /// <summary>
    /// Kabul kâğıtlarının renkleri: tema anahtarı (<c>SiginakTheme</c>; <c>_local_a6</c> <c>admission.paper.*</c>,
    /// <c>admission.slipVerdict.*</c>, <c>admission.stampMarks.*</c>) ya da yoksa tasarımın değeri (<c>kagit_kaynak</c>).
    /// </summary>
    internal static class AdmissionPaperStyle
    {
        /// <summary>Mürekkep #1E1B16: başlıklar, adlar, değerler, kurallar.</summary>
        public static Color Ink => BoardViewUtil.Col("ink", new Color32(0x1E, 0x1B, 0x16, 0xFF));
        /// <summary>Etiket #675F50 (künye, alan etiketleri, not, arşiv).</summary>
        public static Color Label => BoardViewUtil.Col("admission.paper.label", new Color32(0x67, 0x5F, 0x50, 0xFF));
        /// <summary>İkincil #463F34 (alt başlık, meslek, simge).</summary>
        public static Color Secondary => BoardViewUtil.Col("admission.paper.ink", new Color32(0x46, 0x3F, 0x34, 0xFF));
        /// <summary>Kırmızı çizgi #D2483F.</summary>
        public static Color Rule => BoardViewUtil.Col("admission.paper.rule", new Color32(0xD2, 0x48, 0x3F, 0xFF));
        /// <summary>Noktalı alt çizgi #8E8166.</summary>
        public static Color Dotted => BoardViewUtil.Col("ui.reportRule", new Color32(0x8E, 0x81, 0x66, 0xFF));
        /// <summary>Ters zeminli kutunun yazısı #F4ECD8.</summary>
        public static Color Reverse => BoardViewUtil.Col("ui.inkButtonText", new Color32(0xF4, 0xEC, 0xD8, 0xFF));
        /// <summary>Fiş kenarı #CDBF97.</summary>
        public static Color SlipEdge => BoardViewUtil.Col("doorPaper.edge", new Color32(0xCD, 0xBF, 0x97, 0xFF));
        /// <summary>Belgenin iç çift çerçevesi #B9AC8E.</summary>
        public static Color DocFrame => BoardViewUtil.Col("slip.rule", new Color32(0xB9, 0xAC, 0x8E, 0xFF));
        /// <summary>Portre zemini #EAE0C2.</summary>
        public static Color PortraitGround => BoardViewUtil.Col("doorPaper.block", new Color32(0xEA, 0xE0, 0xC2, 0xFF));
        /// <summary>Düzenleyen mührün moru #5A3F78 (temada anahtar yok: tasarım değeri).</summary>
        public static Color Seal => BoardViewUtil.Col("admission.paper.seal", new Color32(0x5A, 0x3F, 0x78, 0xFF));
        /// <summary>Şerit kenarı #D2C7AF.</summary>
        public static Color StripEdge => BoardViewUtil.Col("admission.paper.stripEdge", new Color32(0xD2, 0xC7, 0xAF, 0xFF));
        /// <summary>Talep formu değerleri (kalem) #1A2F5A.</summary>
        public static Color Signature => BoardViewUtil.Col("pen.signature", new Color32(0x1A, 0x2F, 0x5A, 0xFF));
        public static Color CarbonInk => BoardViewUtil.Col("admission.paper.carbonInk", new Color32(0x2E, 0x3C, 0x6E, 0xFF));
        public static Color CarbonLabel => BoardViewUtil.Col("admission.paper.carbonLabel", new Color32(0x4A, 0x56, 0x80, 0xFF));
        /// <summary>Uyarı kırmızısı #9A1E14 ("gözetmen yok · sonuç gelmez").</summary>
        public static Color Alarm => BoardViewUtil.Col("admission.paper.alarm", new Color32(0x9A, 0x1E, 0x14, 0xFF));
        /// <summary>Damga izlerinin opaklığı (KK <c>damga_*</c> 0,93).</summary>
        public static float StampOpacity => BoardViewUtil.Num("admission.stampMarks.opacity", 0.93f);

        /// <summary>Bulgu fişinin sonuç damgasının rengi: eşleşme yeşil, uyuşmazlık kahverengi, görüşme nötr, bekleyen/inceleme gri.</summary>
        public static Color Verdict(SlipVerdict kind)
        {
            switch (kind)
            {
                case SlipVerdict.Match:
                case SlipVerdict.Found:
                    return BoardViewUtil.Col("admission.slipVerdict.match", new Color32(0x24, 0x59, 0x2E, 0xFF));
                case SlipVerdict.Mismatch:
                case SlipVerdict.Differs:
                    return BoardViewUtil.Col("admission.slipVerdict.mismatch", new Color32(0x85, 0x40, 0x05, 0xFF));
                case SlipVerdict.Neutral:
                    return BoardViewUtil.Col("admission.slipVerdict.neutral", new Color32(0x46, 0x3F, 0x34, 0xFF));
                default:
                    return BoardViewUtil.Col("admission.slipVerdict.pending", new Color32(0x67, 0x5F, 0x50, 0xFF));
            }
        }

        /// <summary>Karar damgası izinin rengi (veri vermezse token).</summary>
        public static Color Stamp(StampChoice choice)
        {
            switch (choice)
            {
                case StampChoice.Admit: return BoardViewUtil.Col("admission.stampMarks.admit.color", new Color32(0x24, 0x59, 0x2E, 0xFF));
                case StampChoice.Restricted: return BoardViewUtil.Col("admission.stampMarks.restricted.color", new Color32(0x85, 0x40, 0x05, 0xFF));
                case StampChoice.Reject: return BoardViewUtil.Col("admission.stampMarks.reject.color", new Color32(0x9A, 0x1E, 0x14, 0xFF));
                default: return BoardViewUtil.Col("admission.stampMarks.aid.color", new Color32(0x46, 0x3F, 0x34, 0xFF));
            }
        }

        /// <summary>"#RRGGBB" ya da "#RRGGBBAA"; çözülemezse <paramref name="fallback"/>.</summary>
        public static Color Hex(string hex, Color fallback) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
    }
}
