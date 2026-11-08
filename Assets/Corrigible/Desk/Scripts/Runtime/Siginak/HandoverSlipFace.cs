using System;
using System.Collections.Generic;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Devir fişinin 100 × 150 mm yüzü (Revizyon 3 · R19c, PLAN W3, Karar D4-A): World Space tuval, 1 birim = 1 mm, sol üst
    /// orijin, y aşağı; ışın almaz (tuvalde <c>GraphicRaycaster</c> yok, tıklama fişin çarpıştırıcısından). Kâğıt, kesik kenar,
    /// delik sırası ve kenar çizgisi modelin dokusundadır (<c>tx_handoverSlip</c>); tuvalde yalnız yazılar, çizgiler, diskler ve
    /// simgeler durur.
    /// <list type="bullet">
    /// <item><see cref="Bind"/> veriyi <see cref="HandoverSlipFormat.Format"/> ile o anki dilde satırlara çevirir ve yukarıdan
    /// aşağı dizer (tasarım HTML'inin CSS akışı, <see cref="HandoverSlipMetrics"/>); alt satır ve eylem satırı alta yaslıdır.
    /// Satırlar şablonlardan (<see cref="HandoverSlipRow"/>, <see cref="HandoverSlipWarning"/>) havuzla çoğaltılır.</item>
    /// <item>Kıvrım (D §0.5): model kâğıdı üst kenara doğru yüz yönüne 4 mm kıvrılır; her öğe kendi üst kenarındaki kâğıdın
    /// 0,4 mm önünde durur: yerel z = −(4 · ((150 − y) / 150)² + 0,4) mm (<see cref="CurlZ"/>). Tuval modelin düz düzleminde
    /// (z 0) durur.</item>
    /// <item>Satır sarar, punto küçülmez, otomatik punto yok. Sığmazsa (D9-A) son DOSYA kutusunun adları "… +n" ile kesilir;
    /// yine sığmazsa <see cref="Overflowing"/>.</item>
    /// <item>Dil değişince (<c>DeskLoc.Changed</c>) aynı veriyi yeniden yazar (yalnız sözcükler ve ondalık ayırıcı değişir).
    /// Nesne kapalıyken gelen bağlama açıldıktan sonraki ilk karede uygulanır (TMP ölçüsü uyanık yazı ister).</item>
    /// </list>
    /// Yüz <c>Loc</c> çağırmaz; sözcükler biçimleyiciden gelir. Renkler ve ölçüler prefab'da serileştirilir (kurucu token'dan yazar).
    /// </summary>
    public sealed class HandoverSlipFace : MonoBehaviour
    {
        const float Eps = 0.01f;

        [Tooltip("Yüz tuvali (100 × 150 birim, sol üst pivot, ölçek 0,001).")]
        [SerializeField] RectTransform face;
        [Tooltip("Yazıların kökü (tuvalin çocuğu, tuvali kaplar); boş yüzde kapalı.")]
        [SerializeField] RectTransform content;

        [Header("Başlık")]
        [SerializeField] TMP_Text headerLeft;
        [SerializeField] TMP_Text headerRight;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] Image redRule;

        [Header("Bölüm etiketleri ve çizgileri")]
        [SerializeField] TMP_Text fieldLabel;
        [SerializeField] Image fieldRule;
        [SerializeField] TMP_Text shelterLabel;
        [SerializeField] Image shelterRule;
        [SerializeField] TMP_Text expectedLabel;
        [SerializeField] Image expectedRule;

        [Header("Şablonlar (pasif kökün altında)")]
        [SerializeField] HandoverSlipRow fieldTemplate;
        [SerializeField] HandoverSlipRow shelterTemplate;
        [SerializeField] HandoverSlipRow expectedTemplate;
        [SerializeField] HandoverSlipWarning warningTemplate;

        [Header("Alt")]
        [SerializeField] Image footerRule;
        [SerializeField] TMP_Text footer;
        [Tooltip("Eylem satırının çift çizgisi: üst ve alt 1 px'lik çizgi.")]
        [SerializeField] Image actionRuleTop;
        [SerializeField] Image actionRuleBottom;
        [SerializeField] Image actionIcon;
        [SerializeField] TMP_Text action;

        [Header("Ölçüler ve renkler (kurucu prefab'a yazar)")]
        [SerializeField] HandoverSlipMetrics layout = new HandoverSlipMetrics();
        [SerializeField] HandoverSlipPalette palette = new HandoverSlipPalette();

        /// <summary>Bütün fiş yüzlerinde gerçekleşen yerleşim sayısı (önbellekten dönmeyen bağlamalar; test ve başarım).</summary>
        public static int LayoutCount;

        readonly List<HandoverSlipRow> fieldRows = new List<HandoverSlipRow>();
        readonly List<HandoverSlipRow> shelterRows = new List<HandoverSlipRow>();
        readonly List<HandoverSlipRow> expectedRows = new List<HandoverSlipRow>();
        readonly List<HandoverSlipWarning> boxes = new List<HandoverSlipWarning>();
        int usedField, usedShelter, usedExpected, usedBoxes;
        string cachedKey, cachedLang;
        bool laidOut, pending, subscribed;

        /// <summary>Yüzde yazan veri (null: boş yüz).</summary>
        public HandoverSlipFacts Facts { get; private set; }
        /// <summary>Son yerleşimin satırları (dil o anki dil).</summary>
        public HandoverSlipLines Lines { get; private set; }
        /// <summary>İçerik alt sınırı aştı (en son kutu “… +n” ile kesildikten sonra bile).</summary>
        public bool Overflowing { get; private set; }
        /// <summary>İçeriğin (son satırın ya da kutunun) alt kenarı (fişin üstünden mm).</summary>
        public float ContentBottomMm { get; private set; }
        /// <summary>Alta yaslı alt bloğun üstü (alt çizginin üstündeki boşluk dahil; içerik bunu aşmamalı).</summary>
        public float FooterTopMm { get; private set; }
        /// <summary>Son DOSYA kutusu kesildiyse kutuda kalan ad sayısı; kesilmediyse −1.</summary>
        public int CutNames { get; private set; } = -1;

        public HandoverSlipMetrics Layout => layout;
        public HandoverSlipPalette Palette => palette;
        public RectTransform FaceRect => face;
        public RectTransform Content => content;
        public TMP_Text HeaderLeft => headerLeft;
        public TMP_Text HeaderRight => headerRight;
        public TMP_Text Title => title;
        public TMP_Text Subtitle => subtitle;
        public Image RedRule => redRule;
        public TMP_Text FieldLabel => fieldLabel;
        public TMP_Text ShelterLabel => shelterLabel;
        public TMP_Text ExpectedLabel => expectedLabel;
        public Image FieldRule => fieldRule;
        public Image ShelterRule => shelterRule;
        public Image ExpectedRule => expectedRule;
        public Image FooterRule => footerRule;
        public TMP_Text Footer => footer;
        public Image ActionRuleTop => actionRuleTop;
        public Image ActionRuleBottom => actionRuleBottom;
        public Image ActionIcon => actionIcon;
        public TMP_Text Action => action;
        public HandoverSlipRow FieldTemplate => fieldTemplate;
        public HandoverSlipRow ShelterTemplate => shelterTemplate;
        public HandoverSlipRow ExpectedTemplate => expectedTemplate;
        public HandoverSlipWarning WarningTemplate => warningTemplate;

        /// <summary>Kullanılan saha satırları (yukarıdan aşağı).</summary>
        public IReadOnlyList<HandoverSlipRow> FieldRows => fieldRows.GetRange(0, usedField);
        public IReadOnlyList<HandoverSlipRow> ShelterRows => shelterRows.GetRange(0, usedShelter);
        public IReadOnlyList<HandoverSlipRow> ExpectedRows => expectedRows.GetRange(0, usedExpected);
        public IReadOnlyList<HandoverSlipWarning> Warnings => boxes.GetRange(0, usedBoxes);

        /// <summary>18 px sınıfındaki yazılar: saha ve sığınak satırlarının adları (punto taraması, D9: gövde ≥ 18).</summary>
        public IEnumerable<TMP_Text> NameTexts
        {
            get
            {
                for (int i = 0; i < usedField; i++) if (fieldRows[i].NameText != null) yield return fieldRows[i].NameText;
                for (int i = 0; i < usedShelter; i++) if (shelterRows[i].NameText != null) yield return shelterRows[i].NameText;
            }
        }

        /// <summary>14 px sınıfındaki öteki görünür yazılar (D9: ikincil ≥ 14).</summary>
        public IEnumerable<TMP_Text> ValueTexts
        {
            get
            {
                var names = new HashSet<TMP_Text>(NameTexts);
                if (content == null) yield break;
                foreach (var t in content.GetComponentsInChildren<TMP_Text>(false))
                    if (!names.Contains(t) && !string.IsNullOrEmpty(t.text)) yield return t;
            }
        }

        /// <summary>Kurucunun (tek seferlik editör script'i) ölçü ve renk yazımı.</summary>
        public void Configure(HandoverSlipMetrics metrics, HandoverSlipPalette colours)
        {
            if (metrics != null) layout = metrics;
            if (colours != null) palette = colours;
            cachedKey = null;
        }

        // ================================================================== bağlama

        /// <summary>Veriyi yüze yazar; null boş yüz bırakır. Veri ve dil önceki bağlamayla aynıysa hiçbir şey yapmaz.</summary>
        public void Bind(HandoverSlipFacts facts)
        {
            Facts = facts;
            string key = facts != null ? JsonUtility.ToJson(facts) : null;
            if (laidOut && !pending && key == cachedKey && cachedLang == Loc.Lang) return;
            cachedKey = key;
            cachedLang = Loc.Lang;
            Apply();
        }

        /// <summary>Aynı veriyi o anki dilde yeniden yazar (dil değişimi; önbelleği atlar).</summary>
        public void Refresh()
        {
            cachedLang = Loc.Lang;
            Apply();
        }

        void Apply()
        {
            if (!gameObject.activeInHierarchy) { pending = true; return; }
            Relayout();
        }

        void OnEnable()
        {
            // Kapalıyken dil değiştiyse (abonelik kapalıydı) açıldıktan sonraki ilk karede o anki dilde yeniden yazılır
            if (laidOut && cachedLang != Loc.Lang) { cachedLang = Loc.Lang; pending = true; }
            if (subscribed) return;
            DeskLoc.Instance.Changed += OnLanguageChanged;
            subscribed = true;
        }

        // Kapalıyken gelen bağlama: açıldıktan sonraki ilk karede (çocuk yazılar o zaman uyanmış olur; OnEnable'da değil)
        void LateUpdate()
        {
            if (pending && gameObject.activeInHierarchy) Relayout();
        }

        void OnDisable()
        {
            if (!subscribed) return;
            DeskLoc.Instance.Changed -= OnLanguageChanged;
            subscribed = false;
        }

        void OnLanguageChanged()
        {
            if (this == null) { DeskLoc.Instance.Changed -= OnLanguageChanged; return; }
            if (!laidOut && !pending) return;
            Refresh();
        }

        // ================================================================== yerleşim

        /// <summary>Kıvrım ofseti: üst kenarı fişin üstünden <paramref name="yTopMm"/> mm'de olan öğenin tuval yereli z'si (mm).</summary>
        public static float CurlZ(float yTopMm, HandoverSlipMetrics m)
        {
            float h = m != null && m.sizeMm.y > 0 ? m.sizeMm.y : 150f;
            float t = Mathf.Clamp01((h - yTopMm) / h);
            return -((m != null ? m.curlMm : 4f) * t * t + (m != null ? m.liftMm : 0.4f));
        }

        void Relayout()
        {
            pending = false;
            laidOut = true;
            LayoutCount++;
            CutNames = -1;
            var m = layout ?? (layout = new HandoverSlipMetrics());
            var p = palette ?? (palette = new HandoverSlipPalette());
            var l = HandoverSlipFormat.Format(Facts);
            Lines = l;
            bool show = Facts != null;
            if (content != null && content.gameObject.activeSelf != show) content.gameObject.SetActive(show);
            if (!show)
            {
                Overflowing = false;
                ContentBottomMm = 0;
                FooterTopMm = m.ContentBottomMm;
                usedField = usedShelter = usedExpected = usedBoxes = 0;
                return;
            }

            float x0 = m.ContentLeftMm, w = m.ContentWidthMm, y = m.ContentTopMm;

            // ---- başlık satırı (sol ve sağ; ikisi de tek satır)
            Single(headerLeft, l.HeaderLeft, m.headerEmMm, m.headerSpacingEm, p.label, x0, y, m.headerLineMm, TextAlignmentOptions.BaselineLeft);
            Single(headerRight, l.HeaderRight, m.headerEmMm, m.headerRightSpacingEm, p.label, x0 + w, y, m.headerLineMm, TextAlignmentOptions.BaselineRight);
            y += m.headerLineMm;

            // ---- başlık ve alt başlık (sarabilir)
            y += m.titleGapMm;
            y += Wrapped(title, l.Title, m.titleEmMm, m.titleSpacingEm, p.ink, x0, y, w, m.titleLineMm);
            y += m.subtitleGapMm;
            y += Wrapped(subtitle, l.Subtitle, m.subtitleEmMm, 0, p.secondary, x0, y, w, m.subtitleLineMm);
            y += m.redRuleGapMm;
            Box(redRule, x0, y, w, m.redRuleMm, p.red);
            y += m.redRuleMm;

            // ---- bölümler
            y = Section(fieldLabel, fieldRule, l.FieldLabel, y + m.sectionFirstGapMm, m, p);
            y = Rows(fieldRows, fieldTemplate, l.Field, y, ref usedField, "Saha", m, p);
            y = Section(shelterLabel, shelterRule, l.ShelterLabel, y + m.sectionGapMm, m, p);
            y = Rows(shelterRows, shelterTemplate, l.Shelter, y, ref usedShelter, "Sığınak", m, p);
            y = Section(expectedLabel, expectedRule, l.ExpectedLabel, y + m.sectionGapMm, m, p);
            y = Rows(expectedRows, expectedTemplate, l.Expected, y, ref usedExpected, "Kapanışta", m, p);

            // ---- uyarı kutuları
            float lastTop = y;
            usedBoxes = 0;
            for (int i = 0; i < l.Warnings.Count; i++)
            {
                var box = Pooled(boxes, warningTemplate, i, "Uyarı");
                if (box == null) break;
                y += i == 0 ? m.boxFirstGapMm : m.boxGapMm;
                lastTop = y;
                var data = l.Warnings[i];
                float h = box.Apply(data.Prefix, data.Text, m, p, w);
                HandoverSlipText.Place(box.Rect, x0, y, w, h, new Vector2(0, 1), CurlZ(y, m));
                y += h;
                usedBoxes = i + 1;
            }
            Hide(boxes, usedBoxes);

            // ---- alt blok (alta yaslı)
            FooterTopMm = LayoutFooter(l, m, p);

            // ---- taşma (D9-A): son DOSYA kutusunun adları "… +n"
            if (y > FooterTopMm + Eps && usedBoxes > 0)
            {
                var data = l.Warnings[usedBoxes - 1];
                var box = boxes[usedBoxes - 1];
                if (data.Kind != SlipWarningKind.Door && data.Names.Count > 1)
                    for (int shown = data.Names.Count - 1; shown >= 1; shown--)
                    {
                        data.Text = HandoverSlipFormat.WarningText(data.Kind, data.Names, shown);
                        float h = box.Apply(data.Prefix, data.Text, m, p, w);
                        HandoverSlipText.Place(box.Rect, x0, lastTop, w, h, new Vector2(0, 1), CurlZ(lastTop, m));
                        y = lastTop + h;
                        CutNames = shown;
                        if (y <= FooterTopMm + Eps) break;
                    }
            }

            ContentBottomMm = y;
            Overflowing = y > FooterTopMm + Eps;
        }

        float Section(TMP_Text label, Image rule, string text, float top, HandoverSlipMetrics m, HandoverSlipPalette p)
        {
            float x0 = m.ContentLeftMm, w = m.ContentWidthMm;
            HandoverSlipText.Style(label, m.sectionEmMm, m.sectionSpacingEm * 100f, p.label, false);
            HandoverSlipText.Set(label, text);
            float line = label != null ? HandoverSlipText.NormalLine(label, m.sectionEmMm) : m.sectionEmMm * 1.123f;
            float labelWidth = HandoverSlipText.Width(label, text);
            if (label != null)
                HandoverSlipText.PlaceLine(label, x0, top + HandoverSlipText.Baseline(label, m.sectionEmMm, line), labelWidth, line,
                    TextAlignmentOptions.BaselineLeft, CurlZ(top, m));
            float rx = x0 + labelWidth + m.sectionRuleGapMm;
            Box(rule, rx, top + (line - m.sectionRuleMm) / 2, Mathf.Max(0, x0 + w - rx), m.sectionRuleMm, Fade(p.secondary, p.ruleAlpha));
            return top + line + m.sectionAfterMm;
        }

        float Rows(List<HandoverSlipRow> pool, HandoverSlipRow template, List<HandoverSlipLines.Row> data, float y, ref int used,
            string name, HandoverSlipMetrics m, HandoverSlipPalette p)
        {
            used = 0;
            for (int i = 0; i < data.Count; i++)
            {
                var row = Pooled(pool, template, i, name);
                if (row == null) break;
                float h = row.Apply(data[i], m, p, m.ContentWidthMm);
                HandoverSlipText.Place(row.Rect, m.ContentLeftMm, y, m.ContentWidthMm, h, new Vector2(0, 1), CurlZ(y, m));
                y += h;
                used = i + 1;
            }
            Hide(pool, used);
            return y;
        }

        /// <summary>Alt çizgi, alt satır, çift çizgi ve eylem satırı alttan yukarı; dönen değer alt bloğun üstü (mm).</summary>
        float LayoutFooter(HandoverSlipLines l, HandoverSlipMetrics m, HandoverSlipPalette p)
        {
            float x0 = m.ContentLeftMm, w = m.ContentWidthMm, bottom = m.ContentBottomMm;

            // eylem: simge + yazı ortada; içerik yüksekliği simge ile yazının "normal" satırının büyüğü
            HandoverSlipText.Style(action, m.actionEmMm, m.actionSpacingEm * 100f, p.ink, false);
            HandoverSlipText.Set(action, l.Action);
            float actionLine = action != null ? HandoverSlipText.NormalLine(action, m.actionEmMm) : m.actionEmMm * 1.123f;
            float inner = Mathf.Max(m.actionIconMm, actionLine);
            float innerTop = bottom - m.actionPadBottomMm - inner;
            float ruleTop = innerTop - m.actionPadTopMm - m.actionRuleMm;
            float textWidth = HandoverSlipText.Width(action, l.Action);
            float total = m.actionIconMm + m.actionIconGapMm + textWidth;
            float start = x0 + (w - total) / 2;
            if (actionIcon != null)
            {
                float iy = innerTop + (inner - m.actionIconMm) / 2;
                Box(actionIcon, start, iy, m.actionIconMm, m.actionIconMm, p.ink);
            }
            if (action != null)
            {
                float textTop = innerTop + (inner - actionLine) / 2;
                HandoverSlipText.PlaceLine(action, start + m.actionIconMm + m.actionIconGapMm, textTop + HandoverSlipText.Baseline(action, m.actionEmMm, actionLine),
                    textWidth, actionLine, TextAlignmentOptions.BaselineLeft, CurlZ(textTop, m));
            }
            float thin = m.actionRuleMm / 3f;   // CSS 3 px double: çizgi, boşluk, çizgi
            Box(actionRuleTop, x0, ruleTop, w, thin, p.ink);
            Box(actionRuleBottom, x0, ruleTop + 2 * thin, w, thin, p.ink);

            // alt satır (sarabilir; alt kenarı çift çizginin 1,25 mm üstünde)
            HandoverSlipText.Style(footer, m.footerEmMm, 0, p.secondary, true, m.footerLineMm);
            HandoverSlipText.Set(footer, l.Footer);
            float textBottom = ruleTop - m.actionGapMm;
            HandoverSlipText.PlaceLine(footer, x0, textBottom, w, m.footerLineMm, TextAlignmentOptions.BaselineLeft);
            int lines = Mathf.Max(1, HandoverSlipText.Lines(footer));
            float footerTop = textBottom - lines * m.footerLineMm;
            if (footer != null)
                HandoverSlipText.PlaceLine(footer, x0, footerTop + HandoverSlipText.Baseline(footer, m.footerEmMm, m.footerLineMm), w, m.footerLineMm,
                    TextAlignmentOptions.BaselineLeft, CurlZ(footerTop, m));

            float footerRuleTop = footerTop - m.footerTextGapMm - m.footerRuleMm;
            Box(footerRule, x0, footerRuleTop, w, m.footerRuleMm, Fade(p.secondary, p.ruleAlpha));
            return footerRuleTop - m.footerPadMm;
        }

        // ================================================================== yardımcılar

        void Single(TMP_Text t, string text, float em, float spacingEm, Color colour, float x, float top, float line, TextAlignmentOptions align)
        {
            if (t == null) return;
            HandoverSlipText.Style(t, em, spacingEm * 100f, colour, false);
            HandoverSlipText.Set(t, text);
            HandoverSlipText.PlaceLine(t, x, top + HandoverSlipText.Baseline(t, em, line), HandoverSlipText.Width(t, text), line, align, CurlZ(top, layout));
        }

        /// <summary>Sarabilen yazı; kapladığı yükseklik (satır × satır yüksekliği).</summary>
        float Wrapped(TMP_Text t, string text, float em, float spacingEm, Color colour, float x, float top, float width, float line)
        {
            if (t == null) return line;
            HandoverSlipText.Style(t, em, spacingEm * 100f, colour, true, line);
            HandoverSlipText.Set(t, text);
            HandoverSlipText.PlaceLine(t, x, top + HandoverSlipText.Baseline(t, em, line), width, line, TextAlignmentOptions.BaselineLeft, CurlZ(top, layout));
            return Mathf.Max(1, HandoverSlipText.Lines(t)) * line;
        }

        /// <summary>Çizgi ya da simge: sol üst köşesi (x, top), kıvrım ofseti kendi üst kenarından.</summary>
        void Box(Graphic g, float x, float top, float width, float height, Color colour)
        {
            if (g == null) return;
            HandoverSlipText.Place(g.rectTransform, x, top, width, height, new Vector2(0, 1), CurlZ(top, layout));
            if (g.color != colour) g.color = colour;
        }

        static Color Fade(Color c, float alpha) => new Color(c.r, c.g, c.b, c.a * alpha);

        T Pooled<T>(List<T> pool, T template, int index, string name) where T : Component
        {
            while (pool.Count > index && pool[index] == null) pool.RemoveAt(index);
            if (index < pool.Count)
            {
                var existing = pool[index];
                if (!existing.gameObject.activeSelf) existing.gameObject.SetActive(true);
                return existing;
            }
            if (template == null || content == null) return null;
            var copy = Instantiate(template, content, false);
            copy.name = name + " " + (index + 1);
            if (!copy.gameObject.activeSelf) copy.gameObject.SetActive(true);
            pool.Add(copy);
            return copy;
        }

        static void Hide<T>(List<T> pool, int used) where T : Component
        {
            for (int i = used; i < pool.Count; i++)
                if (pool[i] != null && pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Fiş yüzünün ölçüleri (mm, fişin sol üstünden, y aşağı). Kaynak tasarım HTML'i <c>tasarim/kagit_kaynak/devir_fisi_*.html</c>
    /// (480 × 720 px @1×, 4,8 px/mm; C §7.2): her değer CSS pikselinden yazılır. Yazı boyutları em (mm), harf aralıkları em.
    /// </summary>
    [Serializable]
    public sealed class HandoverSlipMetrics
    {
        public const float PxPerMm = 4.8f;
        const float Px = 1f / PxPerMm;

        [Header("Kâğıt")]
        public Vector2 sizeMm = new Vector2(100, 150);
        [Tooltip("Gövdenin üstü (kesik kenar 0–7 px; gövde 6 px'ten başlar).")]
        public float bodyTopMm = 6 * Px;
        [Tooltip("Sol, sağ ve alt kenar çizgisi (1 px #C9B98A, dokuda).")]
        public float borderMm = 1 * Px;
        public float padTopMm = 12 * Px, padRightMm = 26 * Px, padBottomMm = 14 * Px, padLeftMm = 26 * Px;

        [Header("Başlık")]
        public float headerEmMm = 14 * Px, headerLineMm = 18 * Px, headerSpacingEm = 0.08f, headerRightSpacingEm = 0.04f;
        public float titleGapMm = 4 * Px, titleEmMm = 23 * Px, titleLineMm = 23 * 1.15f * Px, titleSpacingEm = 0.03f;
        public float subtitleGapMm = 2 * Px, subtitleEmMm = 15 * Px, subtitleLineMm = 15 * 1.3f * Px;
        public float redRuleGapMm = 8 * Px, redRuleMm = 2 * Px;

        [Header("Bölüm etiketi (satır yüksekliği yazının 'normal'i)")]
        public float sectionFirstGapMm = 8 * Px, sectionGapMm = 7 * Px, sectionAfterMm = 1 * Px;
        public float sectionEmMm = 14 * Px, sectionSpacingEm = 0.08f, sectionRuleGapMm = 8 * Px, sectionRuleMm = 1 * Px;

        [Header("Satırlar (adım 5 mm)")]
        public float rowMm = 24 * Px;
        public float discMm = 22 * Px, discRingMm = 1.5f * Px, discBottomMm = 1 * Px, discGapMm = 8 * Px, discEmMm = 14 * Px;
        public float nameEmMm = 18 * Px, valueEmMm = 15 * Px, jobEmMm = 14 * Px, jobSpacingEm = 0.04f;
        public float leaderMarginMm = 6 * Px, leaderBottomMm = 6 * Px, leaderHeightMm = 3 * Px, leaderMinMm = 14 * Px, leaderPitchMm = 5 * Px;
        public float labelColumnMm = 104 * Px, labelEmMm = 14 * Px, labelSpacingEm = 0.06f, deltaEmMm = 18 * Px, deltaGapMm = 8 * Px;

        [Header("Uyarı kutusu")]
        public float boxFirstGapMm = 10 * Px, boxGapMm = 6 * Px, boxBorderMm = 1.5f * Px, boxBarMm = 5 * Px;
        public float boxPadTopMm = 5 * Px, boxPadRightMm = 8 * Px, boxPadBottomMm = 6 * Px, boxPadLeftMm = 6 * Px;
        public float boxIconColumnMm = 18 * Px, boxIconGapMm = 7 * Px, boxIconMm = 16 * Px, boxIconTopMm = 1 * Px;
        public float boxTextEmMm = 15 * Px, boxTextLineMm = 20 * Px, boxPrefixEmMm = 14 * Px, boxPrefixSpacingEm = 0.06f;

        [Header("Alt (alta yaslı)")]
        public float footerPadMm = 8 * Px, footerRuleMm = 1 * Px, footerTextGapMm = 5 * Px, footerEmMm = 14 * Px, footerLineMm = 14 * 1.3f * Px;
        [Tooltip("Alt satır ile çift çizgi arası; çift çizgi toplam 3 px (çizgi, boşluk, çizgi).")]
        public float actionGapMm = 6 * Px, actionRuleMm = 3 * Px, actionPadTopMm = 6 * Px, actionPadBottomMm = 2 * Px;
        public float actionIconMm = 20 * Px, actionIconGapMm = 10 * Px, actionEmMm = 19 * Px, actionSpacingEm = 0.04f;

        [Header("Kıvrım (model: z = −0,004 · ((y + 0,075) / 0,15)²)")]
        public float curlMm = 4f;
        [Tooltip("Her öğe kâğıdın bu kadar önünde.")]
        public float liftMm = 0.4f;

        /// <summary>İçeriğin sol kenarı (kenar çizgisi + sol boşluk; 5,63 mm).</summary>
        public float ContentLeftMm => borderMm + padLeftMm;
        /// <summary>İçeriğin üst kenarı (gövdenin üstü + üst boşluk; 3,75 mm).</summary>
        public float ContentTopMm => bodyTopMm + padTopMm;
        /// <summary>İçerik genişliği (88,75 mm).</summary>
        public float ContentWidthMm => sizeMm.x - 2 * borderMm - padLeftMm - padRightMm;
        /// <summary>İçeriğin alt kenarı (146,88 mm).</summary>
        public float ContentBottomMm => sizeMm.y - borderMm - padBottomMm;
    }

    /// <summary>Fiş yüzünün renkleri (C §7.2; kurucu token karşılıklarından yazar).</summary>
    [Serializable]
    public sealed class HandoverSlipPalette
    {
        [Tooltip("Mürekkep #1E1B16: başlık, adlar, değerler, kutu çerçevesi, çift çizgi, simgeler.")]
        public Color ink = new Color32(0x1E, 0x1B, 0x16, 0xFF);
        [Tooltip("Etiket #675F50 (colors.doorPaper.head): başlık satırı, bölüm etiketleri, KAPANIŞTA etiketleri.")]
        public Color label = new Color32(0x67, 0x5F, 0x50, 0xFF);
        [Tooltip("İkincil #463F34 (colors.report.summaryLine): alt başlık, bölüm ve alt çizgiler, alt satır.")]
        public Color secondary = new Color32(0x46, 0x3F, 0x34, 0xFF);
        [Tooltip("Kırmızı çizgi #D2483F (colors.doorPaper.rule).")]
        public Color red = new Color32(0xD2, 0x48, 0x3F, 0xFF);
        [Tooltip("Takım diski #C9A36B (colors.slip.teamDisc) ve halkası #5C4826 (colors.slip.teamDiscRim).")]
        public Color disc = new Color32(0xC9, 0xA3, 0x6B, 0xFF);
        public Color discRim = new Color32(0x5C, 0x48, 0x26, 0xFF);
        [Tooltip("Nokta çizgisi #8E8166 (colors.ui.reportRule).")]
        public Color leader = new Color32(0x8E, 0x81, 0x66, 0xFF);
        [Tooltip("Bölüm ve alt çizgilerinin saydamlığı (CSS opacity 0,55).")]
        public float ruleAlpha = 0.55f;
    }

    /// <summary>
    /// Fiş yüzünün TMP yardımcıları. Tek satırlık yazılar taban çizgisi hizalı (<c>Baseline</c>: dikdörtgenin ortası taban
    /// çizgisi); CSS satır kutusunun taban çizgisi = üst + (satır − (yükselen − inen) · em) / 2 + yükselen · em (font yüz
    /// bilgisinden). Sarılan yazıların satır adımı <c>lineSpacing</c> ile CSS satır yüksekliğine eşitlenir.
    /// </summary>
    internal static class HandoverSlipText
    {
        const float AscentFallback = 0.78125f, ContentFallback = 1.1230469f;

        static float Ratio(TMP_Text t, Func<UnityEngine.TextCore.FaceInfo, float> pick, float fallback)
        {
            var f = t != null ? t.font : null;
            if (f == null || f.faceInfo.pointSize <= 0) return fallback;
            float v = pick(f.faceInfo) / f.faceInfo.pointSize;
            return v > 0 ? v : fallback;
        }

        public static float AscentRatio(TMP_Text t) => Ratio(t, fi => fi.ascentLine, AscentFallback);
        public static float ContentRatio(TMP_Text t) => Ratio(t, fi => fi.ascentLine - fi.descentLine, ContentFallback);
        public static float LineRatio(TMP_Text t) => Ratio(t, fi => fi.lineHeight, ContentFallback);

        public static float Ascent(TMP_Text t, float em) => AscentRatio(t) * em;
        /// <summary>CSS "normal" satır yüksekliği (fontun satır yüksekliği).</summary>
        public static float NormalLine(TMP_Text t, float em) => LineRatio(t) * em;
        /// <summary>Taban çizgisinin satır kutusunun üstünden uzaklığı.</summary>
        public static float Baseline(TMP_Text t, float em, float line) => (line - ContentRatio(t) * em) / 2 + AscentRatio(t) * em;

        public static void Style(TMP_Text t, float em, float spacing, Color colour, bool wrap, float line = 0)
        {
            if (t == null) return;
            if (t.enableAutoSizing) t.enableAutoSizing = false;
            if (!Mathf.Approximately(t.fontSize, em)) t.fontSize = em;
            if (!Mathf.Approximately(t.characterSpacing, spacing)) t.characterSpacing = spacing;
            float lineSpacing = wrap && line > 0 && em > 0 ? (line / em - LineRatio(t)) * 100f : 0f;
            if (!Mathf.Approximately(t.lineSpacing, lineSpacing)) t.lineSpacing = lineSpacing;
            var mode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            if (t.textWrappingMode != mode) t.textWrappingMode = mode;
            if (t.overflowMode != TextOverflowModes.Overflow) t.overflowMode = TextOverflowModes.Overflow;
            if (t.color != colour) t.color = colour;
            if (t.raycastTarget) t.raycastTarget = false;
            if (!t.richText) t.richText = true;
        }

        public static void Set(TMP_Text t, string v)
        {
            if (t == null) return;
            v ??= string.Empty;
            if (t.text != v) t.text = v;
        }

        /// <summary>Yazının tek satırdaki genişliği (mm; zengin metin etiketleri ölçülmez).</summary>
        public static float Width(TMP_Text t, string v) =>
            t == null || string.IsNullOrEmpty(v) ? 0f : t.GetPreferredValues(v, float.PositiveInfinity, float.PositiveInfinity).x;

        /// <summary>Yazının şu anki genişlikteki satır sayısı (ölçü için ağı yeniler).</summary>
        public static int Lines(TMP_Text t)
        {
            if (t == null || string.IsNullOrEmpty(t.text)) return 0;
            t.ForceMeshUpdate(true, true);
            return Mathf.Max(1, t.textInfo.lineCount);
        }

        /// <summary>Dikdörtgeni sol üst çapaya göre yerleştirir: (x, y) kutunun sol üstü (mm), z tuval yereli.</summary>
        public static void Place(RectTransform rt, float x, float y, float width, float height, Vector2 pivot, float z = 0f)
        {
            if (rt == null) return;
            var corner = new Vector2(0, 1);
            if (rt.anchorMin != corner) rt.anchorMin = corner;
            if (rt.anchorMax != corner) rt.anchorMax = corner;
            if (rt.pivot != pivot) rt.pivot = pivot;
            var size = new Vector2(Mathf.Max(0, width), Mathf.Max(0, height));
            if (rt.sizeDelta != size) rt.sizeDelta = size;
            var pos = new Vector3(x + pivot.x * size.x, -(y + (1 - pivot.y) * size.y), z);
            if (rt.anchoredPosition3D != pos) rt.anchoredPosition3D = pos;
            if (rt.localRotation != Quaternion.identity) rt.localRotation = Quaternion.identity;
            if (rt.localScale != Vector3.one) rt.localScale = Vector3.one;
        }

        /// <summary>
        /// Yazıyı taban çizgisine yerleştirir: <paramref name="x"/> hizalamaya göre sol, orta ya da sağ kenar;
        /// <paramref name="baseline"/> ilk satırın taban çizgisi (fişin üstünden mm). Sarılan satırlar aşağı iner.
        /// </summary>
        public static void PlaceLine(TMP_Text t, float x, float baseline, float width, float line, TextAlignmentOptions align, float z = 0f)
        {
            if (t == null) return;
            if (t.alignment != align) t.alignment = align;
            int h = (int)align & 0xFF;
            float px = h == (int)HorizontalAlignmentOptions.Right ? 1f : h == (int)HorizontalAlignmentOptions.Center ? 0.5f : 0f;
            var rt = t.rectTransform;
            var corner = new Vector2(0, 1);
            if (rt.anchorMin != corner) rt.anchorMin = corner;
            if (rt.anchorMax != corner) rt.anchorMax = corner;
            var pivot = new Vector2(px, 0.5f);
            if (rt.pivot != pivot) rt.pivot = pivot;
            var size = new Vector2(Mathf.Max(0.01f, width), Mathf.Max(0.01f, line));
            if (rt.sizeDelta != size) rt.sizeDelta = size;
            var pos = new Vector3(x, -baseline, z);
            if (rt.anchoredPosition3D != pos) rt.anchoredPosition3D = pos;
            if (rt.localRotation != Quaternion.identity) rt.localRotation = Quaternion.identity;
            if (rt.localScale != Vector3.one) rt.localScale = Vector3.one;
            if (t.margin != Vector4.zero) t.margin = Vector4.zero;
        }
    }
}
