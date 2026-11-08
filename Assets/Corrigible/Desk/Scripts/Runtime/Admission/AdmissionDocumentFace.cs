using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kimlik/sevk belgesinin yüzü (148 × 105 mm; PLAN §1.6, §1.10, KK <c>kimlik_sevk_*_{tr,en}.html</c>, 1× = 4 px/mm). Sol bölüm:
    /// künye "KYGA · KİMLİK / SEVK BELGESİ" + sağda "düz. V.nnn", ad (Courier 700, 7 mm), meslek, kırmızı çizgi, portre (çerçeveli,
    /// üstünde düzenleyen mührün halkası ve kodu) ve alanlar (GELDİĞİ YER, BELGE No., MÜHÜR, BEDEN). Koçan çizgisi x 103 mm'de
    /// (noktalı); sağda SEVK koçanı (SEVK No., GÜZERGÂH, TARİH, altta "kapıda koparılır").
    /// <para>Karar damgasının izi belgenin yerelinde (−12; −31) mm'de (belge merkezinden; sol alt boş alan), −4° (KK <c>damga_*</c>):
    /// KABUL ve KISITLI çift kenar + bölme çizgisi + ikinci satır, REDDET kalın düz kenar ve geniş aralık, YARDIM çift kenar iki satır.
    /// İz belgenin içinde kalacak kadar yana kayar. <see cref="ShowImpression"/> basış anında (animasyonlu, <c>stampPressS</c>) ya da
    /// kayıttan devamda animasyonsuz.</para>
    /// <para><b>Veri sözleşmesi</b> (<see cref="DocumentData"/>, W4): <c>Name</c> belgede yazıldığı gibi (tasarım büyük harf), <c>Issued</c>
    /// vardiya ("V.005"; künyede "düz." sabit önekiyle ve koçanda TARİH), <c>Serial</c> BELGE No., <c>Referral</c> SEVK No., <c>Origin</c>
    /// GELDİĞİ YER ve GÜZERGÂH, <c>Stats</c> BEDEN satırı, <c>PortraitKey</c> + <c>PortraitFlip</c> portre, <c>StampChoiceId</c> Core
    /// seçimi, <c>StampLines</c> izin satırları, <c>StampColor</c> "#RRGGBB" (boşsa token). Sabit etiketler prefab'da <c>DeskLocText</c>.</para>
    /// </summary>
    public sealed class AdmissionDocumentFace : AdmissionPaperFace
    {
        const float Px = 0.25f;
        public const float WidthMm = 148f, HeightMm = 105f;
        /// <summary>Kopan koçan çizgisi (KK x 412 px).</summary>
        public const float StubLineMm = 412 * Px;
        /// <summary>Damga izinin merkezi: belge merkezinden (−12; −31) mm (DA:145; y yukarı).</summary>
        public static readonly Vector2 ImpressionOffsetMm = new Vector2(-12f, -31f);
        /// <summary>İzin dönüşü (CSS −4°: tuvalin önünden bakınca saat yönünün tersi).</summary>
        public const float ImpressionTiltDeg = 4f;

        const float Border = 1 * Px, PadTop = 18 * Px, PadRight = 20 * Px, PadBottom = 16 * Px, PadLeft = 22 * Px;
        const float InnerInset = 7 * Px, InnerLine = 1 * Px, InnerGap = 1 * Px;
        const float StubTop = 10 * Px, StubWidth = 6 * Px;
        const float StubColumn = 140 * Px, ColumnGap = 34 * Px;
        const float HeaderEm = 14 * Px, HeaderLine = 18 * Px, HeaderSpacing = 0.08f, IssuedSpacing = 0.04f;
        const float NameGap = 3 * Px, NameEm = 28 * Px, NameLine = 28 * 1.1f * Px, NameSpacing = 0.04f;
        const float JobEm = 17 * Px, JobLine = 17 * 1.3f * Px;
        const float RuleGap = 6 * Px, RuleMm = 2 * Px;
        const float GridGap = 10 * Px, PortraitW = 108 * Px, PortraitH = 122 * Px, PortraitBorder = 1.5f * Px, PortraitColumn = 112 * Px, PortraitGap = 14 * Px;
        const float FieldColumnGap = 12 * Px, FieldRowGap = 6 * Px;
        const float LabelEm = 13 * Px, LabelLine = 13 * 1.5f * Px, LabelSpacing = 0.08f;
        const float PlaceEm = 17 * Px, PlaceLine = 17 * 1.25f * Px;
        const float CodeEm = 16 * Px, CodeLine = 16 * 1.35f * Px;
        const float StatsEm = 16 * Px, StatsLine = 16 * 1.25f * Px;
        const float SealX = 62 * Px, SealY = 84 * Px, SealMm = 70 * Px, SealTextEm = 15 * Px, SealTilt = 12f, SealAlpha = 0.86f;
        const float StubGap = 4 * Px, StubTitleEm = 22 * Px, StubTitleLine = 22 * 1.05f * Px, StubTitleSpacing = 0.06f;
        /// <summary>Koçan künyesi ("KYGA"): KK'de 13 px etiket; okuma pozunda (K8R eğik bakış, 14° eğik) kâğıdın kameraya en uzak köşesinde
        /// 13 px ≈ 13,7 ekran px kalıyor (D14 eşiği 14) → 14 px (≈ 14,8 ekran px; başlık satırıyla aynı punto).</summary>
        const float StubOrgEm = 14 * Px;
        const float StubRuleBefore = 3 * Px, StubRuleAfter = 6 * Px, RouteEm = 15 * Px, RouteLine = 15 * 1.3f * Px;
        const float FootEm = 12 * Px, FootSpacing = 0.04f;
        const float PerforationAlpha = 0.55f;

        // damga izi (KK damga_*: border 6 px double / 9 px solid; dolgu; satırlar)
        const float ImpEm1 = 31 * Px, ImpLine1 = 31 * 1.05f * Px, ImpSpacing1 = 0.08f;
        const float ImpEm2 = 20 * Px, ImpLine2 = 20 * 1.1f * Px, ImpSpacing2 = 0.14f, ImpLineGap = 4 * Px;
        const float ImpDividerGap = 5 * Px, ImpDivider = 2 * Px;
        const float ImpRejectEm = 39 * Px, ImpRejectLine = 39 * 1.05f * Px, ImpRejectSpacing = 0.22f;
        const float ImpDouble = 6 * Px, ImpSolid = 9 * Px;

        public const float ContentLeftMm = Border + PadLeft, ContentTopMm = Border + PadTop;
        public const float ContentRightMm = WidthMm - Border - PadRight, ContentBottomLimitMm = HeightMm - Border - PadBottom;
        public const float StubLeftMm = ContentRightMm - StubColumn;
        public const float MainWidthMm = StubLeftMm - ColumnGap - ContentLeftMm;

        [Header("Kâğıt kenarı (1 px) ve iç çift çerçeve (3 px double)")]
        [SerializeField] AdmissionFrame edge = new AdmissionFrame();
        [SerializeField] AdmissionFrame innerOuter = new AdmissionFrame();
        [SerializeField] AdmissionFrame innerInner = new AdmissionFrame();
        [Tooltip("Kopan koçan çizgisi: dikey döşenen nokta sprite'ı.")]
        [SerializeField] Image perforation;

        [Header("Künye ve ad")]
        [Tooltip("\"KYGA · KİMLİK / SEVK BELGESİ\" (sabit, DeskLocText).")]
        [SerializeField] TMP_Text header;
        [Tooltip("\"düz.\" (sabit, DeskLocText).")]
        [SerializeField] TMP_Text issuedPrefix;
        [SerializeField] TMP_Text issued;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text profession;
        [SerializeField] Image redRule;

        [Header("Portre ve mühür")]
        [SerializeField] PortraitLibrary portraits;
        [SerializeField] Image portraitGround;
        [SerializeField] RawImage portrait;
        [SerializeField] AdmissionFrame portraitFrame = new AdmissionFrame();
        [Tooltip("Mühür halkası (beyaz halka sprite'ı, mor boyanır; dönük).")]
        [SerializeField] Image sealRing;
        [SerializeField] TMP_Text sealText;

        [Header("Alanlar (etiketler sabit, DeskLocText)")]
        [SerializeField] TMP_Text originLabel;
        [SerializeField] TMP_Text origin;
        [SerializeField] TMP_Text serialLabel;
        [SerializeField] TMP_Text serial;
        [SerializeField] TMP_Text sealLabel;
        [SerializeField] TMP_Text seal;
        [SerializeField] TMP_Text statsLabel;
        [SerializeField] TMP_Text stats;

        [Header("SEVK koçanı (etiketler sabit, DeskLocText)")]
        [SerializeField] TMP_Text stubOrg;
        [SerializeField] TMP_Text stubTitle;
        [SerializeField] Image stubRule;
        [SerializeField] TMP_Text referralLabel;
        [SerializeField] TMP_Text referral;
        [SerializeField] TMP_Text routeLabel;
        [SerializeField] TMP_Text route;
        [Tooltip("\"→ Sığınak 07\" (sabit, DeskLocText).")]
        [SerializeField] TMP_Text routeSuffix;
        [SerializeField] TMP_Text dateLabel;
        [SerializeField] TMP_Text date;
        [Tooltip("\"kapıda koparılır\" (sabit, DeskLocText).")]
        [SerializeField] TMP_Text stubFoot;

        [Header("Karar damgası izi (dönük kök)")]
        [SerializeField] RectTransform impression;
        [SerializeField] CanvasGroup impressionGroup;
        [SerializeField] AdmissionFrame impressionOuter = new AdmissionFrame();
        [SerializeField] AdmissionFrame impressionInner = new AdmissionFrame();
        [SerializeField] Image impressionDivider;
        [SerializeField] TMP_Text impressionLine1;
        [SerializeField] TMP_Text impressionLine2;

        bool impressionOn;
        float impressionT = 1f;

        public DocumentData Data { get; private set; }
        /// <summary>İz görünür (veri izi taşıyorsa).</summary>
        public bool ImpressionVisible => impressionOn && HasImpression;
        /// <summary>İzin dış ölçüsü (mm) ve merkezi (belgenin sol üstünden, y aşağı).</summary>
        public Vector2 ImpressionSizeMm { get; private set; }
        public Vector2 ImpressionCenterMm { get; private set; }
        /// <summary>Sol bölümün ve koçanın içerik alt kenarı (mm).</summary>
        public float MainBottomMm { get; private set; }
        public float StubBottomMm { get; private set; }

        public override Vector2 SizeMm => new Vector2(WidthMm, HeightMm);
        protected override bool HasData => Data != null;
        bool HasImpression => Data != null && Data.StampLines != null && Data.StampLines.Length > 0 && !string.IsNullOrEmpty(Data.StampLines[0]);

        public TMP_Text NameText => nameText;
        public TMP_Text Profession => profession;
        public TMP_Text Issued => issued;
        public TMP_Text OriginLabel => originLabel;
        public TMP_Text Origin => origin;
        public TMP_Text Serial => serial;
        public TMP_Text Seal => seal;
        public TMP_Text Stats => stats;
        public TMP_Text Referral => referral;
        public TMP_Text Route => route;
        public TMP_Text Date => date;
        public TMP_Text StubFoot => stubFoot;
        public Image Perforation => perforation;
        public RawImage Portrait => portrait;
        public RectTransform Impression => impression;
        public CanvasGroup ImpressionGroup => impressionGroup;
        public TMP_Text ImpressionLine1 => impressionLine1;
        public TMP_Text ImpressionLine2 => impressionLine2;
        public AdmissionFrame ImpressionOuter => impressionOuter;
        public AdmissionFrame ImpressionInner => impressionInner;
        public Image ImpressionDivider => impressionDivider;

        public void Bind(DocumentData d)
        {
            Data = d;
            Request();
        }

        /// <summary>Damga izini gösterir/gizler: <paramref name="animate"/> basış (0 → %93 opaklık, <c>admission.motion.stampPressS</c>), yoksa hemen.</summary>
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
            // Opaklık (0,93) izin renginde; grup basışın girişini taşır
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

            // ---- kâğıt kenarı, iç çift çerçeve, koçan çizgisi
            edge.Place(0, 0, WidthMm, HeightMm, Border, AdmissionPaperStyle.SlipEdge);
            var frameColour = AdmissionPaperStyle.DocFrame;
            innerOuter.Place(InnerInset, InnerInset, WidthMm - 2 * InnerInset, HeightMm - 2 * InnerInset, InnerLine, frameColour);
            float i2 = InnerInset + InnerLine + InnerGap;
            innerInner.Place(i2, i2, WidthMm - 2 * i2, HeightMm - 2 * i2, InnerLine, frameColour);
            Box(perforation, StubLineMm, StubTop, StubWidth, HeightMm - 2 * StubTop, Fade(secondary, PerforationAlpha));

            // ---- sol bölüm
            float x0 = ContentLeftMm, w = MainWidthMm, y = ContentTopMm;
            Put(header, Text(header), x0, y, w, HeaderEm, HeaderLine, HeaderSpacing, label, false);
            float issuedW = WidthOf(issued, d.Issued, HeaderEm, IssuedSpacing);
            Put(issued, d.Issued, x0 + w, y, 0, HeaderEm, HeaderLine, IssuedSpacing, label, false, TextAlignmentOptions.BaselineRight);
            float space = HeaderEm * 0.6f;   // Courier'in boşluğu (tek aralıklı, 0,6 em)
            float prefixW = WidthOf(issuedPrefix, Text(issuedPrefix), HeaderEm, IssuedSpacing);
            Put(issuedPrefix, Text(issuedPrefix), x0 + w - issuedW - (issuedW > 0 ? space : 0), y, 0, HeaderEm, HeaderLine, IssuedSpacing, label, false, TextAlignmentOptions.BaselineRight);
            float headW = WidthOf(header, Text(header), HeaderEm, HeaderSpacing) + prefixW + space + issuedW;
            if (headW > w + Eps) overflow = headW - w;
            y += HeaderLine;

            y += NameGap;
            Put(nameText, d.Name, x0, y, w, NameEm, NameLine, NameSpacing, ink, false);
            float nameW = WidthOf(nameText, d.Name, NameEm, NameSpacing);
            if (nameW > w + Eps) overflow = Mathf.Max(overflow, nameW - w);
            y += NameLine;
            y += Put(profession, d.Profession, x0, y, w, JobEm, JobLine, 0f, secondary, true);
            y += RuleGap;
            Box(redRule, x0, y, w, RuleMm, AdmissionPaperStyle.Rule);
            y += RuleMm + GridGap;

            // portre: 108 × 122 px + 1,5 px çerçeve (içerik kutusu)
            float gridTop = y;
            float pw = PortraitW + 2 * PortraitBorder, ph = PortraitH + 2 * PortraitBorder;
            Box(portraitGround, x0, gridTop, pw, ph, AdmissionPaperStyle.PortraitGround);
            PlacePortrait(d, x0 + PortraitBorder, gridTop + PortraitBorder);
            portraitFrame.Place(x0, gridTop, pw, ph, PortraitBorder, secondary);
            PlaceSeal(d, x0 + SealX, gridTop + SealY);

            // alanlar: iki sütun (GELDİĞİ YER ve BEDEN iki sütunu kaplar)
            float fx = x0 + PortraitColumn + PortraitGap, fw = x0 + w - fx;
            float half = (fw - FieldColumnGap) / 2f;
            float fy = gridTop;
            fy += Field(originLabel, origin, d.Origin, fx, fy, fw, PlaceEm, PlaceLine, ink, true) + FieldRowGap;
            float left = Field(serialLabel, serial, d.Serial, fx, fy, half, CodeEm, CodeLine, ink, false);
            float right = Field(sealLabel, seal, d.Seal, fx + half + FieldColumnGap, fy, half, CodeEm, CodeLine, ink, false);
            overflow = Mathf.Max(overflow, WidthOf(serial, d.Serial, CodeEm, 0) - half, WidthOf(seal, d.Seal, CodeEm, 0) - half);
            fy += Mathf.Max(left, right) + FieldRowGap;
            fy += Field(statsLabel, stats, d.Stats, fx, fy, fw, StatsEm, StatsLine, ink, true);
            MainBottomMm = Mathf.Max(gridTop + ph, fy);

            // ---- SEVK koçanı (dikey akış, öğe arası 1 mm; "kapıda koparılır" alta yaslı)
            float sx = StubLeftMm, sw = StubColumn, sy = ContentTopMm;
            float orgLine = NormalLine(stubOrg, StubOrgEm);
            sy += Put(stubOrg, Text(stubOrg), sx, sy, sw, StubOrgEm, orgLine, LabelSpacing, label, false) + StubGap;
            sy += Put(stubTitle, Text(stubTitle), sx, sy, sw, StubTitleEm, StubTitleLine, StubTitleSpacing, ink, false) + StubGap;
            sy += StubRuleBefore;
            Box(stubRule, sx, sy, sw, RuleMm, AdmissionPaperStyle.Rule);
            sy += RuleMm + StubRuleAfter + StubGap;
            sy += Field(referralLabel, referral, d.Referral, sx, sy, sw, CodeEm, CodeLine, ink, false, StubGap) + StubGap;
            overflow = Mathf.Max(overflow, WidthOf(referral, d.Referral, CodeEm, 0) - sw);
            sy += Field(routeLabel, route, d.Origin, sx, sy, sw, RouteEm, RouteLine, ink, true, StubGap);
            sy += Put(routeSuffix, Text(routeSuffix), sx, sy, sw, RouteEm, RouteLine, 0f, ink, true) + StubGap;
            sy += Field(dateLabel, date, d.Issued, sx, sy, sw, CodeEm, CodeLine, ink, false, StubGap);
            float footLine = NormalLine(stubFoot, FootEm);
            float footTop = ContentBottomLimitMm - footLine;
            Put(stubFoot, Text(stubFoot), sx, footTop, sw, FootEm, footLine, FootSpacing, label, false);
            StubBottomMm = sy;

            LayoutImpression(d);

            ContentBottomMm = Mathf.Max(MainBottomMm, StubBottomMm);
            Overflowing = overflow > Eps || MainBottomMm > ContentBottomLimitMm + Eps || StubBottomMm > footTop + Eps;
        }

        /// <summary>Etiket (Courier 700, 13 px, satır 1,5) + değer; dönen değer kapladığı yükseklik.</summary>
        float Field(TMP_Text labelText, TMP_Text value, string v, float x, float top, float width, float em, float line, Color ink, bool wrap, float gap = 0f)
        {
            float h = Put(labelText, Text(labelText), x, top, width, LabelEm, LabelLine, LabelSpacing, AdmissionPaperStyle.Label, false);
            h += gap;
            h += Put(value, v, x, top + h, width, em, line, 0f, ink, wrap);
            return h;
        }

        static string Text(TMP_Text t) => t != null ? t.text : null;

        void PlacePortrait(DocumentData d, float x, float top)
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
            // Tam doku (tasarım viewBox 300 × 340 "slice"); D18: sayılı adda belirlenimci ayna (UV x −1)
            var uv = d.PortraitFlip ? new Rect(1, 0, -1, 1) : new Rect(0, 0, 1, 1);
            if (portrait.uvRect != uv) portrait.uvRect = uv;
            HandoverSlipText.Place(portrait.rectTransform, x, top, PortraitW, PortraitH, new Vector2(0, 1));
            if (portrait.color != Color.white) portrait.color = Color.white;
        }

        void PlaceSeal(DocumentData d, float x, float top)
        {
            bool show = !string.IsNullOrEmpty(d.Seal);
            Show(sealRing, show);
            Show(sealText, show);
            if (!show) return;
            var colour = Fade(AdmissionPaperStyle.Seal, SealAlpha);
            if (sealRing != null)
            {
                Centered(sealRing.rectTransform, x + SealMm / 2f, top + SealMm / 2f, SealMm, SealMm, SealTilt);
                if (sealRing.color != colour) sealRing.color = colour;
            }
            if (sealText != null)
            {
                HandoverSlipText.Style(sealText, SealTextEm, 0, colour, false);
                HandoverSlipText.Set(sealText, d.Seal);
                if (sealText.alignment != TextAlignmentOptions.Center) sealText.alignment = TextAlignmentOptions.Center;
                Centered(sealText.rectTransform, x + SealMm / 2f, top + SealMm / 2f, SealMm, NormalLine(sealText, SealTextEm), SealTilt);
            }
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

        // ================================================================== damga izi

        void LayoutImpression(DocumentData d)
        {
            if (!HasImpression || impression == null) { ImpressionSizeMm = Vector2.zero; ApplyImpressionAlpha(); return; }
            AdmissionIds.TryChoice(d.StampChoiceId, out var choice);
            var colour = AdmissionPaperStyle.Hex(d.StampColor, AdmissionPaperStyle.Stamp(choice));
            colour = Fade(colour, AdmissionPaperStyle.StampOpacity);
            string l1 = d.StampLines[0];
            string l2 = d.StampLines.Length > 1 ? d.StampLines[1] : null;
            bool reject = choice == StampChoice.Reject;
            bool divider = choice == StampChoice.Admit || choice == StampChoice.Restricted;
            if (reject) l2 = null;

            // KK dolguları (px): kabul/kısıtlı 8/21/6/23, ret 8/29/5/34, yardım 16/39/13/39
            float padT, padR, padB, padL;
            if (reject) { padT = 8 * Px; padR = 29 * Px; padB = 5 * Px; padL = 34 * Px; }
            else if (choice == StampChoice.Aid) { padT = 16 * Px; padR = 39 * Px; padB = 13 * Px; padL = 39 * Px; }
            else { padT = 8 * Px; padR = 21 * Px; padB = 6 * Px; padL = 23 * Px; }
            float border = reject ? ImpSolid : ImpDouble;

            float em1 = reject ? ImpRejectEm : ImpEm1, line1 = reject ? ImpRejectLine : ImpLine1, sp1 = reject ? ImpRejectSpacing : ImpSpacing1;
            float w1 = WidthOf(impressionLine1, l1, em1, sp1);
            float w2 = string.IsNullOrEmpty(l2) ? 0f : WidthOf(impressionLine2, l2, ImpEm2, ImpSpacing2);
            float innerW = Mathf.Max(w1, w2);
            float innerH = line1 + (divider && !string.IsNullOrEmpty(l2) ? ImpDividerGap + ImpDivider : 0f) + (string.IsNullOrEmpty(l2) ? 0f : ImpLineGap + ImpLine2);
            float bw = border * 2 + padL + padR + innerW, bh = border * 2 + padT + padB + innerH;
            ImpressionSizeMm = new Vector2(bw, bh);

            // merkez: belge merkezinden (−12; −31) mm (y yukarı); iz belgeden taşmasın diye yatayda içeri kayar
            float cx = WidthMm / 2f + ImpressionOffsetMm.x, cy = HeightMm / 2f - ImpressionOffsetMm.y;
            float margin = InnerInset;
            cx = Mathf.Clamp(cx, margin + bw / 2f, Mathf.Max(margin + bw / 2f, WidthMm - margin - bw / 2f));
            cy = Mathf.Clamp(cy, margin + bh / 2f, Mathf.Max(margin + bh / 2f, HeightMm - margin - bh / 2f));
            ImpressionCenterMm = new Vector2(cx, cy);
            Centered(impression, cx, cy, bw, bh, ImpressionTiltDeg);

            // çerçeve: çift (2 px çizgi, 2 px boşluk, 2 px çizgi) ya da kalın düz
            if (reject)
            {
                impressionOuter.Place(0, 0, bw, bh, border, colour);
                impressionInner.SetActive(false);
            }
            else
            {
                float t = border / 3f;
                impressionOuter.Place(0, 0, bw, bh, t, colour);
                impressionInner.Place(2 * t, 2 * t, bw - 4 * t, bh - 4 * t, t, colour);
            }

            float x = border + padL, y = border + padT;
            Put(impressionLine1, l1, x + innerW / 2f, y, innerW, em1, line1, sp1, colour, false, TextAlignmentOptions.Baseline);
            y += line1;
            if (divider && !string.IsNullOrEmpty(l2))
            {
                y += ImpDividerGap;
                Box(impressionDivider, x, y, innerW, ImpDivider, colour);
                y += ImpDivider;
            }
            else Show(impressionDivider, false);
            if (!string.IsNullOrEmpty(l2))
            {
                y += ImpLineGap;
                Put(impressionLine2, l2, x + innerW / 2f, y, innerW, ImpEm2, ImpLine2, ImpSpacing2, colour, false, TextAlignmentOptions.Baseline);
            }
            else Put(impressionLine2, null, 0, 0, 0, ImpEm2, ImpLine2, 0, colour, false);
            ApplyImpressionAlpha();
        }

        /// <summary>Kurucunun (tek seferlik editör script'i) portre kitaplığı bağlaması.</summary>
        public void ConfigurePortraits(PortraitLibrary library) => portraits = library;
    }
}
