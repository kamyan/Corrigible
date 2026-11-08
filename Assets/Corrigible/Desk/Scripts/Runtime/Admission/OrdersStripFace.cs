using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Talimat şeridinin yüzü (80 × 210 mm; PLAN §1.9, KK <c>kabul_talimati_v2/_v3_{tr,en}.html</c>, 1× = 4 px/mm): künye + "No. n",
    /// SÜRÜM n, alt başlık, kırmızı çizgi, numaralı kurallar (Core sırası; metin sarar, küçülmez), önceki sürümden gelen kuralın altında
    /// "sürüm n’den" eki ya da yeni kuralın ters zeminli "YENİ · V.nnn" kutusu (sol kalın çizgi), alta yaslı kesikli arşiv kutusu
    /// (önceki sürümün ilk kuralı üstü çizili) ve noktalı çizgili alt satır. Tasarım v3'ünün İLKE satırı çizilmez (D6-A).
    /// <para>ESKİ cebindeki şeritler yalnız başını gösterir (<see cref="BindHead"/>). Satır işaretçisinin hizası
    /// <see cref="RuleCenterMm"/>: maddenin numara kutusunun dikey merkezi, şeridin üst kenarından mm (TMP satır bilgisinden;
    /// sabit tablo yok, dil ve satır kırılımı değişince kendiliğinden doğru).</para>
    /// </summary>
    public sealed class OrdersStripFace : AdmissionPaperFace
    {
        const float Px = 0.25f;
        public const float WidthMm = 80f, HeightMm = 210f;
        /// <summary>Satır işaretçisinin sıfırı (pivot): şeridin üst kenarının 18 mm altı (AJ:102–108).</summary>
        public const float MarkerZeroMm = 18f;
        /// <summary>İşaretçinin kayma aralığı (m): 0 … −0,17 → en alt madde merkezi 188 mm.</summary>
        public const float MarkerTravelMm = 170f;

        const float Border = 1 * Px, PadTop = 14 * Px, PadSide = 18 * Px, PadBottom = 12 * Px;
        const float HeaderEm = 14 * Px, HeaderLine = 18 * Px, HeaderSpacing = 0.08f, NumberSpacing = 0.04f;
        const float TitleGap = 4 * Px, TitleEm = 30 * Px, TitleLine = 30 * 1.15f * Px, TitleSpacing = 0.04f;
        const float SubGap = 1 * Px, SubEm = 16 * Px, SubLine = 16 * 1.3f * Px, SubSpacing = 0.02f;
        const float RuleGap = 8 * Px, RuleMm = 2 * Px;
        const float ItemGap = 12 * Px, BoxMm = 24 * Px, BoxBorder = 1.5f * Px, BoxEm = 14 * Px, TextGap = 10 * Px;
        const float TextEm = 18 * Px, TextLine = 18 * 1.32f * Px;
        const float TagGap = 3 * Px, TagEm = 13 * Px, TagSpacing = 0.04f;
        const float NewPadTop = 8 * Px, NewPadSide = 10 * Px, NewPadBottom = 10 * Px, NewBorder = 1.5f * Px, NewBar = 6 * Px;
        const float PillGap = 8 * Px, PillMm = 22 * Px, PillPad = 7 * Px, PillSpacing = 0.1f, NewTextGap = 6 * Px;
        const float ArchiveGap = 10 * Px, ArchivePadTop = 8 * Px, ArchivePadSide = 10 * Px, ArchivePadBottom = 9 * Px, ArchiveBorder = 1.5f * Px;
        const float ArchiveTitleSpacing = 0.08f, ArchiveTextGap = 3 * Px, ArchiveTextEm = 16 * Px, ArchiveTextLine = 16 * 1.32f * Px;
        const float FooterPad = 10 * Px, FooterRule = 1 * Px, FooterEm = 13 * Px;

        public const float ContentLeftMm = Border + PadSide, ContentTopMm = Border + PadTop;
        public const float ContentWidthMm = WidthMm - 2 * (Border + PadSide), ContentBottomLimitMm = HeightMm - Border - PadBottom;

        /// <summary>Bir kural satırının nesneleri (prefab'da dört yuva; Core en çok üç etkin kural verir).</summary>
        [Serializable]
        public sealed class RuleSlot
        {
            [Tooltip("Satırın kökü (kapalıysa kullanılmıyor).")]
            public RectTransform root;
            [Tooltip("Numara kutusunun dolgusu (yeni kuralda mürekkep, öteki kurallarda kapalı).")]
            public Image boxFill;
            public AdmissionFrame box = new AdmissionFrame();
            public TMP_Text number;
            public TMP_Text text;
            [Tooltip("\"sürüm n’den\" eki (önceki sürümden gelen kural).")]
            public TMP_Text tag;
            [Tooltip("Yeni kuralın çerçevesi (sol kenar kalın).")]
            public AdmissionFrame newFrame = new AdmissionFrame();
            [Tooltip("Yeni kuralın \"YENİ · V.nnn\" kutusu (ters zemin) ve yazısı.")]
            public Image pill;
            public TMP_Text pillText;
        }

        [Header("Kâğıt kenarı (1 px #D2C7AF)")]
        [SerializeField] AdmissionFrame edge = new AdmissionFrame();

        [Header("Baş")]
        [SerializeField] TMP_Text header;
        [SerializeField] TMP_Text number;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] Image redRule;

        [Header("Kurallar")]
        [SerializeField] RuleSlot[] rules = new RuleSlot[4];

        [Header("Arşiv kutusu (kesikli) ve alt satır (noktalı çizgi)")]
        [SerializeField] AdmissionFrame archiveFrame = new AdmissionFrame();
        [SerializeField] TMP_Text archiveTitle;
        [SerializeField] TMP_Text archiveRule;
        [SerializeField] Image footerRule;
        [SerializeField] TMP_Text footer;
        [SerializeField] TMP_Text footer2;

        readonly float[] centres = new float[8];
        int ruleCount;
        bool headOnly;

        public OrdersData Data { get; private set; }
        /// <summary>Yalnız baş çiziliyor (ESKİ cebi).</summary>
        public bool HeadOnly => headOnly;
        /// <summary>Çizilen kural sayısı (son yerleşim).</summary>
        public int RuleCount { get { EnsureLaidOut(); return ruleCount; } }
        /// <summary>Kuralların bittiği yer ve arşiv/alt bloğun üstü (mm; kurallar bunu aşmamalı).</summary>
        public float RulesBottomMm { get; private set; }
        public float BottomBlockTopMm { get; private set; }

        public override Vector2 SizeMm => new Vector2(WidthMm, HeightMm);
        protected override bool HasData => Data != null;

        public TMP_Text Header => header;
        public TMP_Text Number => number;
        public TMP_Text Title => title;
        public TMP_Text Subtitle => subtitle;
        public TMP_Text ArchiveTitle => archiveTitle;
        public TMP_Text ArchiveRule => archiveRule;
        public TMP_Text Footer => footer;
        public TMP_Text Footer2 => footer2;
        public RuleSlot Rule(int i) => rules != null && i >= 0 && i < rules.Length ? rules[i] : null;
        public int SlotCount => rules != null ? rules.Length : 0;

        /// <summary>Şeridin tamamını yazar (yuvadaki şerit); null boş yüz.</summary>
        public void Bind(OrdersData d)
        {
            Data = d;
            headOnly = false;
            Request();
        }

        /// <summary>Yalnız künye, başlık ve alt başlık (ESKİ cebi: başlıkları üstten görünür).</summary>
        public void BindHead(OrdersData d)
        {
            Data = d;
            headOnly = true;
            Request();
        }

        /// <summary>
        /// <paramref name="i"/>. maddenin numara kutusunun dikey merkezi (şeridin üst kenarından mm). Madde yoksa (−1 dahil) işaretçinin
        /// sıfırı <see cref="MarkerZeroMm"/> (en üst) döner; istasyon işaretçiyi <c>−(merkez − 18) / 1000</c> m'ye, alt sınır −0,17'ye koyar.
        /// </summary>
        public float RuleCenterMm(int i)
        {
            EnsureLaidOut();
            return i >= 0 && i < ruleCount ? centres[i] : MarkerZeroMm;
        }

        protected override void OnCleared()
        {
            ruleCount = 0;
            RulesBottomMm = BottomBlockTopMm = 0;
        }

        protected override void Layout()
        {
            var d = Data;
            Color ink = AdmissionPaperStyle.Ink, label = AdmissionPaperStyle.Label, secondary = AdmissionPaperStyle.Secondary;
            float x0 = ContentLeftMm, w = ContentWidthMm, y = ContentTopMm, bottom = ContentBottomLimitMm;
            float overflow = 0f;

            edge.Place(0, 0, WidthMm, HeightMm, Border, AdmissionPaperStyle.StripEdge);

            // ---- künye + No.
            Put(header, d.Header, x0, y, w, HeaderEm, HeaderLine, HeaderSpacing, label, false);
            Put(number, d.Number, x0 + w, y, 0, HeaderEm, HeaderLine, NumberSpacing, label, false, TextAlignmentOptions.BaselineRight);
            float headW = WidthOf(header, d.Header, HeaderEm, HeaderSpacing) + WidthOf(number, d.Number, HeaderEm, NumberSpacing);
            if (headW > w + Eps) overflow = headW - w;
            y += HeaderLine;

            // ---- SÜRÜM n + alt başlık
            y += TitleGap;
            y += Mathf.Max(TitleLine, Put(title, d.Title, x0, y, w, TitleEm, TitleLine, TitleSpacing, ink, true));
            y += SubGap;
            y += Put(subtitle, d.Subtitle, x0, y, w, SubEm, SubLine, SubSpacing, secondary, true);

            // ---- kırmızı çizgi
            y += RuleGap;
            Box(redRule, x0, y, w, RuleMm, AdmissionPaperStyle.Rule);
            y += RuleMm;

            ruleCount = 0;
            if (headOnly)
            {
                if (rules != null) foreach (var r in rules) if (r != null) Show(r.root, false);
                ShowArchive(false);
                ShowFooter(false);
                RulesBottomMm = BottomBlockTopMm = y;
                ContentBottomMm = y;
                Overflowing = overflow > Eps;
                return;
            }

            // ---- alt blok (alttan yukarı): noktalı çizgi + alt satır, üstünde arşiv kutusu
            float blockTop = LayoutFooter(d, x0, w, bottom, label);
            blockTop = LayoutArchive(d, x0, w, blockTop, label);
            BottomBlockTopMm = blockTop;

            // ---- kurallar
            int count = d.Rules.Count;
            int slots = rules != null ? rules.Length : 0;
            if (count > slots) overflow = Mathf.Max(overflow, 1f);   // yuva yok: taşma
            for (int i = 0; i < slots; i++)
            {
                var slot = rules[i];
                if (slot == null) continue;
                bool used = i < count && i < centres.Length;
                Show(slot.root, used);
                if (!used) continue;
                y += ItemGap;
                y = d.Rules[i].IsNew ? NewRule(slot, d.Rules[i], x0, y, w, ink, out centres[i]) : PlainRule(slot, d.Rules[i], x0, y, w, ink, label, out centres[i]);
                ruleCount = i + 1;
            }
            RulesBottomMm = y;
            ContentBottomMm = y;
            Overflowing = overflow > Eps || y > blockTop + Eps;
        }

        float PlainRule(RuleSlot s, RuleData r, float x0, float top, float w, Color ink, Color label, out float centre)
        {
            HandoverSlipText.Place(s.root, 0, 0, WidthMm, HeightMm, new Vector2(0, 1));
            Show(s.boxFill, false);
            s.newFrame.SetActive(false);
            Show(s.pill, false);
            Show(s.pillText, false);
            s.box.Place(x0, top, BoxMm, BoxMm, BoxBorder, ink);
            NumberIn(s.number, r.Number, x0, top, ink);
            float tx = x0 + BoxMm + TextGap, tw = w - BoxMm - TextGap;
            float h = Put(s.text, r.Text, tx, top, tw, TextEm, TextLine, 0f, ink, true);
            if (!string.IsNullOrEmpty(r.Tag))
            {
                float tagLine = NormalLine(s.tag, TagEm);
                h += TagGap + Put(s.tag, r.Tag, tx, top + h + TagGap, tw, TagEm, tagLine, TagSpacing, label, true);
            }
            else Put(s.tag, null, 0, 0, 0, TagEm, 1, 0, label, true);
            centre = top + BoxMm / 2f;
            return top + Mathf.Max(BoxMm, h);
        }

        float NewRule(RuleSlot s, RuleData r, float x0, float top, float w, Color ink, out float centre)
        {
            HandoverSlipText.Place(s.root, 0, 0, WidthMm, HeightMm, new Vector2(0, 1));
            Show(s.tag, false);
            var reverse = AdmissionPaperStyle.Reverse;
            float ix = x0 + NewBar + NewPadSide, iw = w - NewBar - NewBorder - 2 * NewPadSide;
            float rowTop = top + NewBorder + NewPadTop;
            // numara kutusu dolu (ters), yanında "YENİ · V.nnn" kutusu, ikisi dikeyde ortalı
            Box(s.boxFill, ix, rowTop, BoxMm, BoxMm, ink);
            s.box.Place(ix, rowTop, BoxMm, BoxMm, BoxBorder, ink);
            NumberIn(s.number, r.Number, ix, rowTop, reverse);
            if (!string.IsNullOrEmpty(r.Tag) && s.pill != null)
            {
                float textW = WidthOf(s.pillText, r.Tag, TagEm, PillSpacing);
                float px = ix + BoxMm + PillGap, py = rowTop + (BoxMm - PillMm) / 2f;
                Box(s.pill, px, py, textW + 2 * PillPad, PillMm, ink);
                Put(s.pillText, r.Tag, px + PillPad, py + (PillMm - NormalLine(s.pillText, TagEm)) / 2f, textW, TagEm, NormalLine(s.pillText, TagEm), PillSpacing, reverse, false);
            }
            else { Show(s.pill, false); Show(s.pillText, false); }
            float textTop = rowTop + BoxMm + NewTextGap;
            float h = Put(s.text, r.Text, ix, textTop, iw, TextEm, TextLine, 0f, ink, true);
            float boxBottom = textTop + h + NewPadBottom + NewBorder;
            s.newFrame.Place(x0, top, w, boxBottom - top, NewBorder, ink, NewBar);
            centre = rowTop + BoxMm / 2f;
            return boxBottom;
        }

        static void NumberIn(TMP_Text t, string n, float x, float top, Color colour)
        {
            if (t == null) return;
            float line = NormalLine(t, BoxEm);
            Put(t, n, x + BoxMm / 2f, top + (BoxMm - line) / 2f, BoxMm, BoxEm, line, 0f, colour, false, TextAlignmentOptions.Baseline);
        }

        float LayoutFooter(OrdersData d, float x0, float w, float bottom, Color label)
        {
            bool any = !string.IsNullOrEmpty(d.Footer) || !string.IsNullOrEmpty(d.Footer2);
            ShowFooter(any);
            if (!any) return bottom;
            float line = NormalLine(footer, FooterEm);
            float textTop = bottom - line;
            Put(footer, d.Footer, x0, textTop, w, FooterEm, line, 0f, label, false);
            Put(footer2, d.Footer2, x0 + w, textTop, 0, FooterEm, line, 0f, label, false, TextAlignmentOptions.BaselineRight);
            float ruleTop = textTop - FooterPad - FooterRule;
            Box(footerRule, x0, ruleTop, w, FooterRule, AdmissionPaperStyle.Dotted);
            return ruleTop;
        }

        float LayoutArchive(OrdersData d, float x0, float w, float blockTop, Color label)
        {
            bool any = !string.IsNullOrEmpty(d.ArchiveTitle);
            ShowArchive(any);
            if (!any) return blockTop;
            float ix = x0 + ArchiveBorder + ArchivePadSide, iw = w - 2 * (ArchiveBorder + ArchivePadSide);
            float titleLine = NormalLine(archiveTitle, TagEm);
            // önce ölç, sonra alttan yukarı yerleştir
            float textH = string.IsNullOrEmpty(d.ArchiveRule) ? 0f : Put(archiveRule, Strike(d.ArchiveRule), ix, 0, iw, ArchiveTextEm, ArchiveTextLine, 0f, label, true);
            float inner = titleLine + (textH > 0 ? ArchiveTextGap + textH : 0f);
            float h = ArchiveBorder + ArchivePadTop + inner + ArchivePadBottom + ArchiveBorder;
            float top = blockTop - ArchiveGap - h;
            float y = top + ArchiveBorder + ArchivePadTop;
            Put(archiveTitle, d.ArchiveTitle, ix, y, iw, TagEm, titleLine, ArchiveTitleSpacing, label, false);
            if (textH > 0) Put(archiveRule, Strike(d.ArchiveRule), ix, y + titleLine + ArchiveTextGap, iw, ArchiveTextEm, ArchiveTextLine, 0f, label, true);
            else Put(archiveRule, null, 0, 0, 0, ArchiveTextEm, 1, 0, label, true);
            archiveFrame.Place(x0, top, w, h, ArchiveBorder, label);
            return top;
        }

        /// <summary>Arşivdeki kural üstü çizili (TMP <c>&lt;s&gt;</c>).</summary>
        static string Strike(string s) => string.IsNullOrEmpty(s) ? s : "<s>" + s + "</s>";

        void ShowArchive(bool on)
        {
            archiveFrame.SetActive(on);
            if (!on) { Show(archiveTitle, false); Show(archiveRule, false); }
        }

        void ShowFooter(bool on)
        {
            Show(footerRule, on);
            if (!on) { Show(footer, false); Show(footer2, false); }
        }
    }
}
