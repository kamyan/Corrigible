using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Aday kartının yüzü (148 × 105 mm; KOS aşama 2, tasarım §7; ölçü ailesi eski kimlik belgesiyle aynı, 1× = 4 px/mm).
    /// Yukarıdan aşağı: künye "KYGA · ADAY KARTI" + sağda geliş vardiyası; ad (Courier 700, 7 mm) ve aynı satırın sağında kademe kutusu
    /// (KEFİLLİ / KAYITSIZ / ŞÜPHELİ, rengi veriden); ustalık; kırmızı çizgi; solda portre, sağında ŞÜPHE satırı ve BEDEN; altında en
    /// çok üç özellik satırı (GETİRİ / BEDEL etiketi + ad, altında etki; bedel etiketi uyarı kırmızısı). Koçan yok.
    /// <para>KABUL izi kartın yerelinde (38; 5) mm'de (kart merkezinden; y yukarı; portrenin sağındaki boş alan), 4° dönük: çift kenar + bölme çizgisi + ikinci satır
    /// (eski kimlik belgesinin KABUL izi). <see cref="ShowImpression"/> basış anında animasyonlu
    /// (<c>admission.motion.stampPressS</c>).</para>
    /// <para><b>Veri sözleşmesi</b> (<see cref="CandidateCardData"/>): bütün metinler çevrilmiş gelir; yüz <c>Loc</c> çağırmaz. Sabit
    /// etiketler (künye, ŞÜPHE, BEDEN) prefab'da <c>DeskLocText</c>. Gizli kimlik veride yoktur; yüz yalnız veriyi çizer.</para>
    /// </summary>
    public sealed class CandidateCardFace : AdmissionPaperFace
    {
        const float Px = 0.25f;
        public const float WidthMm = 148f, HeightMm = 105f;
        public const int MaxPerks = 3;
        /// <summary>KABUL izinin merkezi: kart merkezinden (38; 5) mm (y yukarı): portrenin sağındaki boş alan, özellik satırlarını örtmez.</summary>
        public static readonly Vector2 ImpressionOffsetMm = new Vector2(38f, 5f);
        public const float ImpressionTiltDeg = 4f;

        const float Border = 1 * Px, PadTop = 18 * Px, PadRight = 20 * Px, PadBottom = 16 * Px, PadLeft = 22 * Px;
        const float InnerInset = 7 * Px, InnerLine = 1 * Px, InnerGap = 1 * Px;
        const float HeaderEm = 14 * Px, HeaderLine = 18 * Px, HeaderSpacing = 0.08f, IssuedSpacing = 0.04f;
        const float NameGap = 3 * Px, NameEm = 28 * Px, NameLine = 28 * 1.1f * Px, NameSpacing = 0.04f;
        const float TierEm = 14 * Px, TierLine = 18 * Px, TierSpacing = 0.08f, TierBorder = 2 * Px, TierPad = 4 * Px, TierGap = 12 * Px;
        const float JobEm = 17 * Px, JobLine = 17 * 1.3f * Px;
        const float RuleGap = 6 * Px, RuleMm = 2 * Px, GridGap = 10 * Px;
        const float PortraitW = 108 * Px, PortraitH = 122 * Px, PortraitBorder = 1.5f * Px, PortraitColumn = 112 * Px, PortraitGap = 14 * Px;
        const float LabelEm = 13 * Px, LabelLine = 13 * 1.5f * Px, LabelSpacing = 0.08f;
        const float DoubtEm = 17 * Px, DoubtLine = 17 * 1.25f * Px, FieldRowGap = 6 * Px;
        const float StatsEm = 16 * Px, StatsLine = 16 * 1.25f * Px;
        const float PerkTop = 8 * Px, PerkTagEm = 13 * Px, PerkTagGap = 8 * Px, PerkEm = 16 * Px, PerkLine = 16 * 1.25f * Px, PerkGap = 4 * Px;

        // KABUL izi (KK damga_kabul: 6 px çift kenar, dolgu 8/21/6/23)
        const float ImpEm1 = 31 * Px, ImpLine1 = 31 * 1.05f * Px, ImpSpacing1 = 0.08f;
        const float ImpEm2 = 20 * Px, ImpLine2 = 20 * 1.1f * Px, ImpSpacing2 = 0.14f, ImpLineGap = 4 * Px;
        const float ImpDividerGap = 5 * Px, ImpDivider = 2 * Px, ImpDouble = 6 * Px;
        const float ImpPadTop = 8 * Px, ImpPadRight = 21 * Px, ImpPadBottom = 6 * Px, ImpPadLeft = 23 * Px;

        public const float ContentLeftMm = Border + PadLeft, ContentTopMm = Border + PadTop;
        public const float ContentRightMm = WidthMm - Border - PadRight, ContentBottomLimitMm = HeightMm - Border - PadBottom;
        public const float ContentWidthMm = ContentRightMm - ContentLeftMm;

        [Header("Kâğıt kenarı (1 px) ve iç çift çerçeve")]
        [SerializeField] AdmissionFrame edge = new AdmissionFrame();
        [SerializeField] AdmissionFrame innerOuter = new AdmissionFrame();
        [SerializeField] AdmissionFrame innerInner = new AdmissionFrame();

        [Header("Künye, ad, kademe")]
        [Tooltip("\"KYGA · ADAY KARTI\" (sabit, DeskLocText).")]
        [SerializeField] TMP_Text header;
        [Tooltip("Geliş vardiyası (\"V.005\").")]
        [SerializeField] TMP_Text issued;
        [SerializeField] TMP_Text nameText;
        [Tooltip("Ustalık (\"Teknisyen · eğitimli\").")]
        [SerializeField] TMP_Text profession;
        [SerializeField] Image redRule;
        [Tooltip("Kademe işareti (ad satırının sağında kutu).")]
        [SerializeField] TMP_Text tierText;
        [SerializeField] AdmissionFrame tierFrame = new AdmissionFrame();

        [Header("Portre")]
        [SerializeField] PortraitLibrary portraits;
        [SerializeField] Image portraitGround;
        [SerializeField] RawImage portrait;
        [SerializeField] AdmissionFrame portraitFrame = new AdmissionFrame();

        [Header("Alanlar (etiketler sabit, DeskLocText)")]
        [Tooltip("\"ŞÜPHE\" (sabit, DeskLocText).")]
        [SerializeField] TMP_Text originLabel;
        [Tooltip("Şüphe satırı (sarar).")]
        [SerializeField] TMP_Text origin;
        [SerializeField] TMP_Text statsLabel;
        [SerializeField] TMP_Text stats;

        [Header("Özellik satırları (en çok 3)")]
        [SerializeField] TMP_Text[] perkTags = new TMP_Text[MaxPerks];
        [SerializeField] TMP_Text[] perkNames = new TMP_Text[MaxPerks];
        [SerializeField] TMP_Text[] perkEffects = new TMP_Text[MaxPerks];

        [Header("KABUL izi (dönük kök)")]
        [SerializeField] RectTransform impression;
        [SerializeField] CanvasGroup impressionGroup;
        [SerializeField] AdmissionFrame impressionOuter = new AdmissionFrame();
        [SerializeField] AdmissionFrame impressionInner = new AdmissionFrame();
        [SerializeField] Image impressionDivider;
        [SerializeField] TMP_Text impressionLine1;
        [SerializeField] TMP_Text impressionLine2;

        bool impressionOn;
        float impressionT = 1f;

        public CandidateCardData Data { get; private set; }
        /// <summary>İz görünür (veri izi taşıyorsa).</summary>
        public bool ImpressionVisible => impressionOn && HasImpression;
        /// <summary>İzin dış ölçüsü (mm) ve merkezi (kartın sol üstünden, y aşağı).</summary>
        public Vector2 ImpressionSizeMm { get; private set; }
        public Vector2 ImpressionCenterMm { get; private set; } = new Vector2(WidthMm / 2f + ImpressionOffsetMm.x, HeightMm / 2f - ImpressionOffsetMm.y);
        /// <summary>Özellik satırlarının alt kenarı (mm; son yerleşim).</summary>
        public float PerksBottomMm { get; private set; }

        public override Vector2 SizeMm => new Vector2(WidthMm, HeightMm);
        protected override bool HasData => Data != null;
        bool HasImpression => Data != null && Data.StampLines != null && Data.StampLines.Length > 0 && !string.IsNullOrEmpty(Data.StampLines[0]);

        public TMP_Text Header => header;
        public TMP_Text Issued => issued;
        public TMP_Text NameText => nameText;
        public TMP_Text Profession => profession;
        public TMP_Text TierText => tierText;
        public TMP_Text OriginLabel => originLabel;
        public TMP_Text Origin => origin;
        public TMP_Text Stats => stats;
        public RawImage Portrait => portrait;
        public RectTransform Impression => impression;
        public CanvasGroup ImpressionGroup => impressionGroup;
        public TMP_Text ImpressionLine1 => impressionLine1;
        public TMP_Text ImpressionLine2 => impressionLine2;
        public TMP_Text PerkTag(int i) => At(perkTags, i);
        public TMP_Text PerkName(int i) => At(perkNames, i);
        public TMP_Text PerkEffect(int i) => At(perkEffects, i);

        static TMP_Text At(TMP_Text[] list, int i) => list != null && i >= 0 && i < list.Length ? list[i] : null;

        /// <summary>Kartı yüze yazar; null boş yüz bırakır.</summary>
        public void Bind(CandidateCardData d)
        {
            Data = d;
            Request();
        }

        /// <summary>KABUL izini gösterir/gizler: <paramref name="animate"/> basış (0 → tam, <c>admission.motion.stampPressS</c>), yoksa hemen.</summary>
        public void ShowImpression(bool on, bool animate)
        {
            impressionOn = on;
            impressionT = on && animate ? 0f : 1f;
            EnsureLaidOut();
            ApplyImpressionAlpha();
        }

        public override void Advance(float dt)
        {
            if (impressionT >= 1f) return;
            float s = BoardViewUtil.Num("admission.motion.stampPressS", 0.12f);
            impressionT = s > 0 ? Mathf.Min(1f, impressionT + Mathf.Max(0f, dt) / s) : 1f;
            ApplyImpressionAlpha();
        }

        void ApplyImpressionAlpha()
        {
            bool show = ImpressionVisible;
            Show(impression, show);
            if (impressionGroup == null) return;
            float a = show ? impressionT : 0f;
            if (impressionGroup.alpha != a) impressionGroup.alpha = a;
        }

        protected override void OnCleared()
        {
            Show(impression, false);
        }

        protected override void Layout()
        {
            var d = Data;
            Color ink = AdmissionPaperStyle.Ink, label = AdmissionPaperStyle.Label, secondary = AdmissionPaperStyle.Secondary;
            float overflow = 0f;

            // ---- kâğıt kenarı ve iç çift çerçeve
            edge.Place(0, 0, WidthMm, HeightMm, Border, AdmissionPaperStyle.SlipEdge);
            var frameColour = AdmissionPaperStyle.DocFrame;
            innerOuter.Place(InnerInset, InnerInset, WidthMm - 2 * InnerInset, HeightMm - 2 * InnerInset, InnerLine, frameColour);
            float i2 = InnerInset + InnerLine + InnerGap;
            innerInner.Place(i2, i2, WidthMm - 2 * i2, HeightMm - 2 * i2, InnerLine, frameColour);

            // ---- künye: solda sabit başlık, sağda geliş vardiyası
            float x0 = ContentLeftMm, w = ContentWidthMm, y = ContentTopMm;
            Put(header, Text(header), x0, y, w, HeaderEm, HeaderLine, HeaderSpacing, label, false);
            Put(issued, d.Arrived, x0 + w, y, 0, HeaderEm, HeaderLine, IssuedSpacing, label, false, TextAlignmentOptions.BaselineRight);
            float headW = WidthOf(header, Text(header), HeaderEm, HeaderSpacing) + WidthOf(issued, d.Arrived, HeaderEm, IssuedSpacing);
            if (headW > w + Eps) overflow = headW - w;
            y += HeaderLine;

            // ---- ad + kademe kutusu (aynı satır; kutu sağa yaslı, satırda dikey ortalı)
            y += NameGap;
            Put(nameText, d.Name, x0, y, w, NameEm, NameLine, NameSpacing, ink, false);
            float nameW = WidthOf(nameText, d.Name, NameEm, NameSpacing);
            float tierW = 0f;
            bool hasTier = !string.IsNullOrEmpty(d.TierName);
            if (hasTier)
            {
                var tierColour = AdmissionPaperStyle.Hex(d.TierColor, TierFallback(d.Tier));
                float textW = WidthOf(tierText, d.TierName, TierEm, TierSpacing);
                tierW = textW + 2 * (TierBorder + TierPad);
                float tierH = TierLine + 2 * TierBorder, tx = x0 + w - tierW, ty = y + (NameLine - tierH) / 2f;
                tierFrame.Place(tx, ty, tierW, tierH, TierBorder, tierColour);
                Put(tierText, d.TierName, tx + TierBorder + TierPad, ty + TierBorder, textW, TierEm, TierLine, TierSpacing, tierColour, false);
            }
            else
            {
                tierFrame.SetActive(false);
                Put(tierText, null, 0, 0, 0, TierEm, TierLine, TierSpacing, label, false);
            }
            float nameRow = nameW + (hasTier ? TierGap + tierW : 0f);
            if (nameRow > w + Eps) overflow = Mathf.Max(overflow, nameRow - w);
            y += NameLine;

            // ---- ustalık, kırmızı çizgi
            Put(profession, d.Mastery, x0, y, w, JobEm, JobLine, 0f, secondary, false);
            overflow = Mathf.Max(overflow, WidthOf(profession, d.Mastery, JobEm, 0f) - w);
            y += JobLine;
            y += RuleGap;
            Box(redRule, x0, y, w, RuleMm, AdmissionPaperStyle.Rule);
            y += RuleMm + GridGap;

            // ---- portre (solda) + ŞÜPHE ve BEDEN (sağında)
            float gridTop = y;
            float pw = PortraitW + 2 * PortraitBorder, ph = PortraitH + 2 * PortraitBorder;
            Box(portraitGround, x0, gridTop, pw, ph, AdmissionPaperStyle.PortraitGround);
            PlacePortrait(d, x0 + PortraitBorder, gridTop + PortraitBorder);
            portraitFrame.Place(x0, gridTop, pw, ph, PortraitBorder, secondary);

            float fx = x0 + PortraitColumn + PortraitGap, fw = x0 + w - fx, fy = gridTop;
            fy += Field(originLabel, origin, d.Suspicion, fx, fy, fw, DoubtEm, DoubtLine, ink) + FieldRowGap;
            fy += Field(statsLabel, stats, d.Stats, fx, fy, fw, StatsEm, StatsLine, ink);
            y = Mathf.Max(gridTop + ph, fy);

            // ---- özellik satırları: etiket + ad, altında etki (sarar)
            y += PerkTop;
            int n = d.Perks != null ? Mathf.Min(d.Perks.Count, MaxPerks) : 0;
            if (d.Perks != null && d.Perks.Count > MaxPerks) overflow = Mathf.Max(overflow, 1f);
            for (int i = 0; i < MaxPerks; i++)
            {
                var tag = At(perkTags, i); var name = At(perkNames, i); var effect = At(perkEffects, i);
                var p = i < n ? d.Perks[i] : null;
                if (p == null)
                {
                    Put(tag, null, 0, 0, 0, PerkTagEm, PerkLine, LabelSpacing, label, false);
                    Put(name, null, 0, 0, 0, PerkEm, PerkLine, 0f, ink, false);
                    Put(effect, null, 0, 0, 0, PerkEm, PerkLine, 0f, secondary, true);
                    continue;
                }
                if (i > 0) y += PerkGap;
                float tagW = WidthOf(tag, p.Tag, PerkTagEm, LabelSpacing);
                Put(tag, p.Tag, x0, y, tagW, PerkTagEm, PerkLine, LabelSpacing, p.Cost ? AdmissionPaperStyle.Alarm : label, false);
                float nx = x0 + (tagW > 0 ? tagW + PerkTagGap : 0f);
                Put(name, p.Name, nx, y, x0 + w - nx, PerkEm, PerkLine, 0f, ink, false);
                overflow = Mathf.Max(overflow, nx + WidthOf(name, p.Name, PerkEm, 0f) - (x0 + w));
                y += PerkLine;
                y += Put(effect, p.Effect, x0, y, w, PerkEm, PerkLine, 0f, secondary, true);
            }
            PerksBottomMm = y;

            LayoutImpression(d);

            ContentBottomMm = y;
            Overflowing = overflow > Eps || y > ContentBottomLimitMm + Eps;
        }

        /// <summary>Kademe işaretinin yedek rengi (veri renk vermezse): kefilli yeşil, kayıtsız kahverengi, şüpheli kırmızı.</summary>
        static Color TierFallback(int tier)
        {
            switch (tier)
            {
                case 0: return BoardViewUtil.Col("admission.candidates.tier0", new Color32(0x24, 0x59, 0x2E, 0xFF));
                case 1: return BoardViewUtil.Col("admission.candidates.tier1", new Color32(0x85, 0x40, 0x05, 0xFF));
                default: return BoardViewUtil.Col("admission.candidates.tier2", new Color32(0x9A, 0x1E, 0x14, 0xFF));
            }
        }

        /// <summary>Etiket (Courier 700, 13 px) + değer (sarar); dönen değer kapladığı yükseklik.</summary>
        static float Field(TMP_Text labelText, TMP_Text value, string v, float x, float top, float width, float em, float line, Color ink)
        {
            float h = Put(labelText, Text(labelText), x, top, width, LabelEm, LabelLine, LabelSpacing, AdmissionPaperStyle.Label, false);
            h += Put(value, v, x, top + h, width, em, line, 0f, ink, true);
            return h;
        }

        static string Text(TMP_Text t) => t != null ? t.text : null;

        void PlacePortrait(CandidateCardData d, float x, float top)
        {
            if (portrait == null) return;
            Texture tex = null;
            if (portraits != null)
            {
                tex = portraits.TextureFor(PortraitLibrary.KeyOf(d.PortraitKey));
                if (tex == null) tex = portraits.Placeholder;
            }
            Show(portrait, tex != null);
            if (tex == null) return;
            if (portrait.texture != tex) portrait.texture = tex;
            var uv = d.PortraitFlip ? new Rect(1, 0, -1, 1) : new Rect(0, 0, 1, 1);
            if (portrait.uvRect != uv) portrait.uvRect = uv;
            HandoverSlipText.Place(portrait.rectTransform, x, top, PortraitW, PortraitH, new Vector2(0, 1));
            if (portrait.color != Color.white) portrait.color = Color.white;
        }

        /// <summary>Ortası (cx, cy) mm'de, <paramref name="deg"/> dönük dikdörtgen.</summary>
        static void Centered(RectTransform rt, float cx, float cy, float w, float h, float deg)
        {
            var corner = new Vector2(0, 1);
            if (rt.anchorMin != corner) rt.anchorMin = corner;
            if (rt.anchorMax != corner) rt.anchorMax = corner;
            var pivot = new Vector2(0.5f, 0.5f);
            if (rt.pivot != pivot) rt.pivot = pivot;
            var size = new Vector2(w, h);
            if (rt.sizeDelta != size) rt.sizeDelta = size;
            var pos = new Vector3(cx, -cy, 0f);
            if (rt.anchoredPosition3D != pos) rt.anchoredPosition3D = pos;
            var rot = Quaternion.Euler(0, 0, deg);
            if (rt.localRotation != rot) rt.localRotation = rot;
            if (rt.localScale != Vector3.one) rt.localScale = Vector3.one;
        }

        // ================================================================== KABUL izi

        void LayoutImpression(CandidateCardData d)
        {
            if (!HasImpression || impression == null) { ImpressionSizeMm = Vector2.zero; ApplyImpressionAlpha(); return; }
            var colour = Fade(AdmissionPaperStyle.Hex(d.StampColor, AdmissionPaperStyle.Stamp(StampChoice.Admit)), AdmissionPaperStyle.StampOpacity);
            string l1 = d.StampLines[0];
            string l2 = d.StampLines.Length > 1 ? d.StampLines[1] : null;
            bool two = !string.IsNullOrEmpty(l2);

            float w1 = WidthOf(impressionLine1, l1, ImpEm1, ImpSpacing1);
            float w2 = two ? WidthOf(impressionLine2, l2, ImpEm2, ImpSpacing2) : 0f;
            float innerW = Mathf.Max(w1, w2);
            float innerH = ImpLine1 + (two ? ImpDividerGap + ImpDivider + ImpLineGap + ImpLine2 : 0f);
            float bw = ImpDouble * 2 + ImpPadLeft + ImpPadRight + innerW, bh = ImpDouble * 2 + ImpPadTop + ImpPadBottom + innerH;
            ImpressionSizeMm = new Vector2(bw, bh);

            // merkez: kart merkezinden ImpressionOffsetMm (y yukarı); iz karttan taşmasın diye içeri kayar
            float cx = WidthMm / 2f + ImpressionOffsetMm.x, cy = HeightMm / 2f - ImpressionOffsetMm.y;
            float margin = InnerInset;
            cx = Mathf.Clamp(cx, margin + bw / 2f, Mathf.Max(margin + bw / 2f, WidthMm - margin - bw / 2f));
            cy = Mathf.Clamp(cy, margin + bh / 2f, Mathf.Max(margin + bh / 2f, HeightMm - margin - bh / 2f));
            ImpressionCenterMm = new Vector2(cx, cy);
            Centered(impression, cx, cy, bw, bh, ImpressionTiltDeg);

            // çift çerçeve: 2 px çizgi, 2 px boşluk, 2 px çizgi
            float t = ImpDouble / 3f;
            impressionOuter.Place(0, 0, bw, bh, t, colour);
            impressionInner.Place(2 * t, 2 * t, bw - 4 * t, bh - 4 * t, t, colour);

            float x = ImpDouble + ImpPadLeft, y = ImpDouble + ImpPadTop;
            Put(impressionLine1, l1, x + innerW / 2f, y, innerW, ImpEm1, ImpLine1, ImpSpacing1, colour, false, TextAlignmentOptions.Baseline);
            y += ImpLine1;
            if (two)
            {
                y += ImpDividerGap;
                Box(impressionDivider, x, y, innerW, ImpDivider, colour);
                y += ImpDivider + ImpLineGap;
                Put(impressionLine2, l2, x + innerW / 2f, y, innerW, ImpEm2, ImpLine2, ImpSpacing2, colour, false, TextAlignmentOptions.Baseline);
            }
            else
            {
                Show(impressionDivider, false);
                Put(impressionLine2, null, 0, 0, 0, ImpEm2, ImpLine2, 0, colour, false);
            }
            ApplyImpressionAlpha();
        }

        /// <summary>Kurucunun (tek seferlik editör script'i) portre kitaplığı bağlaması.</summary>
        public void ConfigurePortraits(PortraitLibrary library) => portraits = library;
    }
}
