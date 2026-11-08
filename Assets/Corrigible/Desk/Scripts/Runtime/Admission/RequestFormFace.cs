using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// DIŞ KAYIT talep formunun yüzü (100 × 133,3 mm; PLAN §1.8, DA <c>requestForm()</c> 840 × 1120 px = 8,4 px/mm): başlık
    /// "KYGA · DIŞ KAYIT TALEBİ" + sağda vardiya, kırmızı çizgi, altı satır (etiket Courier 700, değer Kalam 700 el yazısı, alt çizgi),
    /// gözetmenin altında "(BOŞTA üst kartı)", altta "sonuç sonraki kapanışta"; karbon kopyada zemin, mürekkep ve etiket mavi, köşede
    /// KOPYA. Gözetmen adı her çizimde Core'dan gelir; Boşta boşsa (<see cref="RequestData.SupervisorMissing"/>) değer kırmızı
    /// ("gözetmen yok · sonuç gelmez") ve üst kart notu gizli.
    /// <para><b>Veri sözleşmesi</b> (W4 kurucusu): <see cref="RequestData.Labels"/>[0..5] satır etiketleri (AD, BELGE No., SEVK No.,
    /// MÜHÜR, SORULACAK, GÖZETMEN; eksikse prefab'daki sabit yazı kalır), <see cref="RequestData.Values"/>[0..5] değerler (5. satırda
    /// <see cref="RequestData.Supervisor"/> doluysa o yazılır), <see cref="RequestData.Values"/>[<see cref="ShiftIndex"/>] başlığın
    /// sağındaki vardiya ("V.005"). Başlık, üst kart notu, alt satır ve KOPYA prefab'da sabit yazıdır (<c>DeskLocText</c>).</para>
    /// El yazısı değer satıra sığmazsa yatayda en çok %70'e sıkışır (elle yazılmış kâğıt), yine sığmazsa <see cref="Overflowing"/>.
    /// </summary>
    public sealed class RequestFormFace : AdmissionPaperFace
    {
        const float Px = 1f / 8.4f;
        public const float WidthMm = 100f, HeightMm = 1120 * Px;
        /// <summary>Satır sayısı (etiket + değer) ve vardiya değerinin dizini.</summary>
        public const int RowCount = 6, ShiftIndex = 6, SupervisorRow = 5;
        /// <summary>El yazısının en çok sıkışması.</summary>
        public const float MinValueScale = 0.7f;

        const float Margin = 44 * Px, FrameInset = 6.5f * Px, FrameMm = 3 * Px, FrameAlpha = 0.55f;
        const float HeadEm = 30 * Px, HeadBaseline = 74 * Px;
        const float RuleTop = 96 * Px, RuleMm = 5 * Px;
        const float RowBaseline0 = 180 * Px, RowPitch = 92 * Px, LabelEm = 30 * Px;
        const float ValueX = 330 * Px, ValueDrop = 6 * Px, ValueEm = 46 * Px;
        const float UnderlineX = 320 * Px, UnderlineDrop = 20 * Px, UnderlineMm = 2 * Px, UnderlineAlpha = 0.4f;
        const float FooterEm = 28 * Px, FooterBaseline = 1000 * Px, CopyX = 610 * Px, CopyBaseline = 1060 * Px;

        /// <summary>Formun bir satırı.</summary>
        [Serializable]
        public sealed class FormRow
        {
            public TMP_Text label;
            public TMP_Text value;
            public Image underline;
        }

        [Header("Kâğıt (asıl yaprak ve karbon kopya malzemesi)")]
        [SerializeField] Renderer paper;
        [SerializeField] Material sheetMaterial;
        [SerializeField] Material carbonMaterial;

        [Header("İç çerçeve (3 px, %55)")]
        [SerializeField] AdmissionFrame frame = new AdmissionFrame();

        [Header("Başlık (sabit, DeskLocText) ve vardiya")]
        [SerializeField] TMP_Text header;
        [SerializeField] TMP_Text shift;
        [SerializeField] Image redRule;

        [Header("Satırlar (etiketler sabit, DeskLocText; değerler dinamik)")]
        [SerializeField] FormRow[] rows = new FormRow[RowCount];
        [Tooltip("Gözetmenin altındaki not \"(BOŞTA üst kartı)\" (sabit, DeskLocText; değer sütununda).")]
        [SerializeField] TMP_Text supervisorNote;
        [SerializeField] Image noteUnderline;

        [Header("Alt (sabit, DeskLocText)")]
        [SerializeField] TMP_Text footer;
        [Tooltip("Karbon kopyanın köşesindeki KOPYA.")]
        [SerializeField] TMP_Text copyMark;

        public RequestData Data { get; private set; }
        public bool Carbon { get; private set; }
        public override Vector2 SizeMm => new Vector2(WidthMm, HeightMm);
        protected override bool HasData => Data != null;

        public TMP_Text Header => header;
        public TMP_Text Shift => shift;
        public TMP_Text SupervisorNote => supervisorNote;
        public TMP_Text FooterText => footer;
        public TMP_Text CopyMark => copyMark;
        public Renderer Paper => paper;
        public Material SheetMaterial => sheetMaterial;
        public Material CarbonMaterial => carbonMaterial;
        public FormRow Row(int i) => rows != null && i >= 0 && i < rows.Length ? rows[i] : null;

        /// <summary>Formu yazar: <paramref name="carbon"/> = defterde kalan karbon kopya. null boş yüz.</summary>
        public void Bind(RequestData d, bool carbon)
        {
            Data = d;
            Carbon = carbon;
            ApplyPaper();
            Request();
        }

        void ApplyPaper()
        {
            if (paper == null) return;
            var m = Carbon ? carbonMaterial : sheetMaterial;
            if (m != null && paper.sharedMaterial != m) paper.sharedMaterial = m;
        }

        // ================================================================== GİDEN ağzındaki görünür alt parça

        bool cropInit;
        Vector3 paperRestPos, paperRestScale;
        RectMask2D cropMask;

        /// <summary>Görünen alt parçanın yüksekliği (mm; kâğıt boyuysa tam kâğıt).</summary>
        public float VisibleBottomMm { get; private set; } = float.MaxValue;

        /// <summary>
        /// Yalnız kâğıdın alt <paramref name="visibleMm"/> mm'si görünür (GİDEN · TELSİZ ağzına girmiş yaprak; tasarımda yalnız ağızdan
        /// sarkan alt parça çizilir, DA:166 "printPlane … [0.1, 0.067]"). Üst kısım ağzın içinde sayılır: kâğıt dörtgeni kısalır, yüz
        /// <see cref="RectMask2D"/> ile kırpılır. Kâğıt boyu ya da üstü tam kâğıdı geri getirir.
        /// </summary>
        public void ShowBottom(float visibleMm)
        {
            float h = SizeMm.y;
            float v = Mathf.Clamp(visibleMm, 0f, h);
            if (Mathf.Abs(v - Mathf.Min(VisibleBottomMm, h)) < Eps) return;
            VisibleBottomMm = v;
            float hidden = h - v;
            if (paper != null)
            {
                var t = paper.transform;
                if (!cropInit) { paperRestPos = t.localPosition; paperRestScale = t.localScale; }
                // dörtgen kendi y ekseninde kısalır ve alt kenarı yerinde kalır (yerel y = kâğıdın üst yönü)
                t.localScale = new Vector3(paperRestScale.x, paperRestScale.y * (v / h), paperRestScale.z);
                var upInParent = t.parent != null ? t.parent.InverseTransformDirection(t.up) : t.up;
                t.localPosition = paperRestPos - upInParent.normalized * (hidden * 0.5f / 1000f);
            }
            cropInit = true;
            if (face != null)
            {
                if (cropMask == null) cropMask = face.GetComponent<RectMask2D>();
                if (cropMask == null && hidden > Eps) cropMask = face.gameObject.AddComponent<RectMask2D>();
                if (cropMask != null)
                {
                    cropMask.padding = new Vector4(0f, 0f, 0f, hidden);   // sol, alt, sağ, üst (tuval birimi = mm)
                    cropMask.enabled = hidden > Eps;
                }
            }
        }

        protected override void Layout()
        {
            var d = Data;
            Color label = Carbon ? AdmissionPaperStyle.CarbonLabel : AdmissionPaperStyle.Label;
            Color secondary = Carbon ? AdmissionPaperStyle.CarbonLabel : AdmissionPaperStyle.Secondary;
            Color ink = Carbon ? AdmissionPaperStyle.CarbonInk : AdmissionPaperStyle.Signature;
            Color alarm = AdmissionPaperStyle.Alarm;
            float overflow = 0f;
            float right = WidthMm - Margin;

            frame.Place(FrameInset, FrameInset, WidthMm - 2 * FrameInset, HeightMm - 2 * FrameInset, FrameMm, Fade(AdmissionPaperStyle.Label, FrameAlpha));

            // ---- başlık ve vardiya (aynı taban çizgisi)
            float headLine = NormalLine(header, HeadEm);
            float headTop = HeadBaseline - Ascent(header, HeadEm, headLine);
            Put(header, header != null ? header.text : null, Margin, headTop, right - Margin, HeadEm, headLine, 0f, label, false);
            string shiftText = d.Values != null && d.Values.Length > ShiftIndex ? d.Values[ShiftIndex] : null;
            Put(shift, shiftText, right, headTop, 0, HeadEm, headLine, 0f, label, false, TextAlignmentOptions.BaselineRight);
            float headW = WidthOf(header, header != null ? header.text : null, HeadEm, 0) + WidthOf(shift, shiftText, HeadEm, 0) + 2f;
            if (headW > right - Margin + Eps) overflow = headW - (right - Margin);
            Box(redRule, Margin, RuleTop, right - Margin, RuleMm, AdmissionPaperStyle.Rule);

            // ---- satırlar
            var underline = Fade(AdmissionPaperStyle.Label, UnderlineAlpha);
            float valueW = right - ValueX;
            for (int i = 0; i < RowCount; i++)
            {
                var r = Row(i);
                if (r == null) continue;
                float baseline = RowBaseline0 + i * RowPitch;
                // veri etiket vermezse prefab'daki sabit yazı (DeskLocText'in o anki çevirisi) kalır
                string lab = d.Labels != null && d.Labels.Length > i && !string.IsNullOrEmpty(d.Labels[i]) ? d.Labels[i] : r.label != null ? r.label.text : null;
                float labLine = NormalLine(r.label, LabelEm);
                Put(r.label, lab, Margin, baseline - Ascent(r.label, LabelEm, labLine), ValueX - Margin - 2f, LabelEm, labLine, 0f, label, false);
                float lw = WidthOf(r.label, lab, LabelEm, 0);
                if (Margin + lw > UnderlineX + Eps) overflow = Mathf.Max(overflow, Margin + lw - UnderlineX);

                string val = d.Values != null && d.Values.Length > i ? d.Values[i] : null;
                bool missing = false;
                if (i == SupervisorRow)
                {
                    if (!string.IsNullOrEmpty(d.Supervisor)) val = d.Supervisor;
                    missing = d.SupervisorMissing;
                }
                overflow = Mathf.Max(overflow, Value(r.value, val, baseline + ValueDrop, valueW, missing ? alarm : ink));
                Box(r.underline, UnderlineX, baseline + UnderlineDrop, right - UnderlineX, UnderlineMm, underline);
            }

            // ---- üst kart notu (gözetmen satırının altında, değer sütununda; gözetmen yoksa gizli)
            float noteBaseline = RowBaseline0 + RowCount * RowPitch;
            bool note = supervisorNote != null && !d.SupervisorMissing && !string.IsNullOrEmpty(supervisorNote.text);
            if (supervisorNote != null)
            {
                if (note) overflow = Mathf.Max(overflow, Value(supervisorNote, supervisorNote.text, noteBaseline + ValueDrop, valueW, ink));
                else Show(supervisorNote, false);
            }
            if (noteUnderline != null)
            {
                if (note) Box(noteUnderline, UnderlineX, noteBaseline + UnderlineDrop, right - UnderlineX, UnderlineMm, underline);
                else Show(noteUnderline, false);
            }

            // ---- alt satır ve KOPYA
            float footLine = NormalLine(footer, FooterEm);
            Put(footer, footer != null ? footer.text : null, Margin, FooterBaseline - Ascent(footer, FooterEm, footLine), right - Margin, FooterEm, footLine, 0f, secondary, false);
            if (copyMark != null)
            {
                if (Carbon) Put(copyMark, copyMark.text, CopyX, CopyBaseline - Ascent(copyMark, FooterEm, footLine), right - CopyX, FooterEm, footLine, 0f, label, false);
                else Show(copyMark, false);
            }

            ContentBottomMm = FooterBaseline;
            Overflowing = overflow > Eps;
        }

        /// <summary>El yazısı değer: taban çizgisinde, sığmazsa en çok %70'e sıkışır; kalan taşma (mm) döner.</summary>
        float Value(TMP_Text t, string text, float baseline, float width, Color colour)
        {
            if (t == null) return 0f;
            float line = NormalLine(t, ValueEm);
            float em = ValueEm;
            float natural = WidthOf(t, text, ValueEm, 0);
            if (natural > width && natural > 0) em = Mathf.Max(ValueEm * MinValueScale, ValueEm * width / natural);
            Put(t, text, ValueX, baseline - Ascent(t, em, line), width, em, line, 0f, colour, false);
            float w = WidthOf(t, text, em, 0);
            return Mathf.Max(0f, w - width);
        }

        /// <summary>Satır kutusunun üstünden taban çizgisine uzaklık (yerleştirme taban çizgisinden yapılır).</summary>
        static float Ascent(TMP_Text t, float em, float line) => HandoverSlipText.Baseline(t, em, line);

        /// <summary>Kurucunun (tek seferlik editör script'i) kâğıt malzemesi bağlaması.</summary>
        public void ConfigurePaper(Renderer renderer, Material sheet, Material carbon)
        {
            paper = renderer;
            sheetMaterial = sheet;
            carbonMaterial = carbon;
            ApplyPaper();
        }
    }
}
