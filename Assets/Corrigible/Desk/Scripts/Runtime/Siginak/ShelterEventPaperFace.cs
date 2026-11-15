using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Olay tutanağının yüzü (KOS aşama 4, tasarım §7): A5 yatay 210 × 148 mm, 1 birim = 1 mm, sol üst orijin (aday kartı ailesi,
    /// <see cref="AdmissionPaperFace"/>). Yukarıdan aşağı: kenar ve iç çerçeve; künye "KYGA · OLAY TUTANAĞI" (sabit, <c>DeskLocText</c>) ve
    /// sağda vardiya; başlık; kişi satırı (sönük); kırmızı çizgi; gövde (sarar); tablo başlığı "KARAR" | "BEDEL · RİSK" (sabit); en çok
    /// üç seçenek satırı (solda işaret kutusu + "1 · ad" + GÜVENLİ etiketi, sağda bedel, risk, kit kilidi ve kapalı gerekçesi alt alta;
    /// kapalı satır soluk ve sol sütununda küçük, dönük "KAPALI" izi; seçili satırın kutusu dolu, adı kalem mavisi); altta solda not
    /// satırı, sağda KARAR kutusu; en üstte karar izi ("KARARA BAĞLANDI" + seçenek, çift kenar, 4°).
    /// <para>Satırlar sarar, punto küçülmez; sığmazsa <see cref="AdmissionPaperFace.Overflowing"/> (içerik KARAR kutusunun ya da not
    /// satırının üstüne taşar). Tık eşlemesi görünümdedir: <see cref="RowAt"/>, <see cref="ConfirmContains"/>. Yüz <c>Loc</c> çağırmaz:
    /// dinamik metin <see cref="ShelterEventPaperData"/> ile çevrilmiş gelir.</para>
    /// </summary>
    public sealed class ShelterEventPaperFace : AdmissionPaperFace
    {
        const float Px = 0.25f;
        public const float WidthMm = 210f, HeightMm = 148f;
        public const int MaxRows = ShelterEventPaperData.MaxRows;

        const float Border = 1 * Px, InnerInset = 3f, InnerBorder = 1 * Px, Pad = 7f;
        const float HeaderEm = 14 * Px, HeaderLine = 18 * Px, HeaderSpacing = 0.08f, ShiftSpacing = 0.04f;
        const float TitleGap = 1.5f, TitleEm = 8f, TitleLine = 8.8f, TitleSpacing = 0.04f;
        const float PersonGap = 0.6f, PersonEm = 3.6f, PersonLine = 4.6f, PersonSpacing = 0.04f;
        const float RuleGap = 1.8f, RuleMm = 2 * Px;
        const float BodyEm = 3.8f, BodyLine = 3.8f * 1.3f;
        const float TableGap = 3f, ColEm = 3f, ColLine = 4f, ColSpacing = 0.08f, TableRuleMm = 1 * Px, TableRuleGap = 1.2f;
        const float RowPadTop = 1.2f;
        const float MarkMm = 4f, MarkGap = 2f, MarkBorder = 0.35f, TickInset = 0.9f;
        const float NameEm = 4.2f, NameLine = 5.2f, SafeEm = 2.8f, SafeLine = 3.6f, SafeSpacing = 0.08f;
        const float CostEm = 3.4f, CostLine = 4.4f, RiskEm = 3.3f, RiskLine = 4.2f;
        const float ColumnGap = 4f;
        const float ClosedEm = 2.8f, ClosedLine = 3.2f, ClosedBorder = 0.4f, ClosedPadX = 1.2f, ClosedPadY = 0.4f, ClosedTiltDeg = 3f;
        const float ConfirmW = 62f, ConfirmH = 15f, ConfirmBorder = 0.5f, ConfirmEm = 4.6f, ConfirmLine1 = 5.4f, ConfirmEm2 = 3f, ConfirmLine2 = 3.8f;
        const float NoteEm = 3.4f, NoteLine = 4.4f;
        const float StampEm1 = 6f, StampLineH1 = 6.8f, StampEm2 = 4.5f, StampLineH2 = 5.4f, StampSpacing = 0.1f;
        const float StampOuter = 0.9f, StampInnerGap = 1.2f, StampInnerBorder = 0.35f, StampPadX = 4f, StampPadY = 2.2f;
        /// <summary>Karar izinin dönüşü (derece).</summary>
        public const float StampTiltDeg = 4f;

        [Header("Kâğıt kenarı")]
        [SerializeField] AdmissionFrame edge = new AdmissionFrame();
        [SerializeField] AdmissionFrame inner = new AdmissionFrame();

        [Header("Künye ve başlık")]
        [Tooltip("Sabit künye (DeskLocText: \"KYGA · OLAY TUTANAĞI\").")]
        [SerializeField] TMP_Text header;
        [SerializeField] TMP_Text shift;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text person;
        [SerializeField] Image rule;
        [SerializeField] TMP_Text body;

        [Header("Tablo")]
        [Tooltip("Sabit sütun başlıkları (DeskLocText: \"KARAR\", \"BEDEL · RİSK\").")]
        [SerializeField] TMP_Text colDecision;
        [SerializeField] TMP_Text colCost;
        [SerializeField] Image tableRule;

        [Header("Satırlar (en çok üç)")]
        [SerializeField] TMP_Text[] rowName = new TMP_Text[MaxRows];
        [SerializeField] TMP_Text[] rowSafe = new TMP_Text[MaxRows];
        [SerializeField] TMP_Text[] rowCost = new TMP_Text[MaxRows];
        [SerializeField] TMP_Text[] rowRisk = new TMP_Text[MaxRows];
        [SerializeField] TMP_Text[] rowKit = new TMP_Text[MaxRows];
        [SerializeField] TMP_Text[] rowClosed = new TMP_Text[MaxRows];
        [SerializeField] AdmissionFrame[] rowMark = { new AdmissionFrame(), new AdmissionFrame(), new AdmissionFrame() };
        [Tooltip("Seçili satırın işaret kutusundaki dolgu.")]
        [SerializeField] Image[] rowTick = new Image[MaxRows];
        [Tooltip("Kapalı satırın küçük KAPALI izi (dönük kök, çerçeve, yazı).")]
        [SerializeField] RectTransform[] rowStamp = new RectTransform[MaxRows];
        [SerializeField] AdmissionFrame[] rowStampFrame = { new AdmissionFrame(), new AdmissionFrame(), new AdmissionFrame() };
        [SerializeField] TMP_Text[] rowStampText = new TMP_Text[MaxRows];
        [Tooltip("Satırlar arası ince çizgiler.")]
        [SerializeField] Image[] rowRules = new Image[MaxRows - 1];

        [Header("Alt bant")]
        [SerializeField] TMP_Text note;
        [SerializeField] AdmissionFrame confirmFrame = new AdmissionFrame();
        [Tooltip("Sabit KARAR sözcüğü (DeskLocText).")]
        [SerializeField] TMP_Text confirmLine1;
        [SerializeField] TMP_Text confirmLine2;

        [Header("Karar izi (dönük kök)")]
        [SerializeField] RectTransform stamp;
        [SerializeField] CanvasGroup stampGroup;
        [SerializeField] AdmissionFrame stampOuter = new AdmissionFrame();
        [SerializeField] AdmissionFrame stampInner = new AdmissionFrame();
        [SerializeField] TMP_Text stampLine1;
        [SerializeField] TMP_Text stampLine2;

        readonly Rect[] rowRects = new Rect[MaxRows];
        Rect confirmRect;
        int rowCount;
        bool stampOn;
        float stampT = 1f, rowsTop, rowsBottom;

        public ShelterEventPaperData Data { get; private set; }
        public override Vector2 SizeMm => new Vector2(WidthMm, HeightMm);
        protected override bool HasData => Data != null;
        bool HasStamp => Data != null && Data.StampLines != null && Data.StampLines.Length > 0 && !string.IsNullOrEmpty(Data.StampLines[0]);

        /// <summary>Karar izi görünür (veri izi taşıyorsa).</summary>
        public bool StampVisible => stampOn && HasStamp;
        /// <summary>Yazılan satır sayısı (son yerleşim).</summary>
        public int RowCount { get { EnsureLaidOut(); return rowCount; } }
        /// <summary>KARAR kutusu (mm; sol üst köken, y aşağı).</summary>
        public Rect ConfirmRect { get { EnsureLaidOut(); return confirmRect; } }

        public TMP_Text Header => header;
        public TMP_Text Shift => shift;
        public TMP_Text Title => title;
        public TMP_Text Person => person;
        public TMP_Text Body => body;
        public TMP_Text Note => note;
        public TMP_Text ConfirmText => confirmLine2;
        public RectTransform Stamp => stamp;
        public TMP_Text StampLine1 => stampLine1;
        public TMP_Text StampLine2 => stampLine2;
        public TMP_Text RowName(int i) => At(rowName, i);
        public TMP_Text RowSafe(int i) => At(rowSafe, i);
        public TMP_Text RowCost(int i) => At(rowCost, i);
        public TMP_Text RowRisk(int i) => At(rowRisk, i);
        public TMP_Text RowKit(int i) => At(rowKit, i);
        public TMP_Text RowClosed(int i) => At(rowClosed, i);
        public TMP_Text RowStampText(int i) => At(rowStampText, i);
        public Image RowTick(int i) => At(rowTick, i);

        static T At<T>(T[] list, int i) where T : class => list != null && i >= 0 && i < list.Length ? list[i] : null;

        /// <summary>Bütün yazılar (testler: dil, taşma ve punto denetimi).</summary>
        public System.Collections.Generic.IEnumerable<TMP_Text> Texts
        {
            get
            {
                foreach (var t in new[] { header, shift, title, person, body, colDecision, colCost, note, confirmLine1, confirmLine2, stampLine1, stampLine2 })
                    if (t != null) yield return t;
                foreach (var list in new[] { rowName, rowSafe, rowCost, rowRisk, rowKit, rowClosed, rowStampText })
                    if (list != null) foreach (var t in list) if (t != null) yield return t;
            }
        }

        // ------------------------------------------------------------------ tema

        static Color ThemeCol(string key, Color32 fallback) => BoardViewUtil.Col(key, fallback);
        static float ThemeNum(string key, float fallback) => BoardViewUtil.Num(key, fallback);

        /// <summary>Seçili satırın kalemi (#1A2F5A).</summary>
        public static Color SelectedInk => ThemeCol("shelterEvent.ink.selected", new Color32(0x1A, 0x2F, 0x5A, 0xFF));
        public static Color SafeInk => ThemeCol("shelterEvent.ink.safe", new Color32(0x24, 0x59, 0x2E, 0xFF));
        /// <summary>KOS aşama 6: eşiğe vardıran "güvenli" seçeneğin EŞİK etiketi (kapalı satırın kırmızısı).</summary>
        public static Color SafeWarnInk => ThemeCol("shelterEvent.ink.closed", new Color32(0x9A, 0x1E, 0x14, 0xFF));
        public static Color ClosedInk => ThemeCol("shelterEvent.ink.closed", new Color32(0x9A, 0x1E, 0x14, 0xFF));
        public static Color KitLockInk => ThemeCol("shelterEvent.ink.kitLock", new Color32(0x85, 0x40, 0x05, 0xFF));
        public static Color DecisionStamp => ThemeCol("shelterEvent.stamp.decision.color", new Color32(0x1A, 0x47, 0x89, 0xFF));
        public static Color ClosedStampInk => ThemeCol("shelterEvent.stamp.closed.color", new Color32(0x9A, 0x1E, 0x14, 0xFF));
        public static float ClosedAlpha => ThemeNum("shelterEvent.row.closedAlpha", 0.45f);
        static float DecisionColMm => ThemeNum("shelterEvent.layout.decisionColMm", 62f);
        static float RowGapMm => ThemeNum("shelterEvent.layout.rowGapMm", 2f);

        // ------------------------------------------------------------------ bağlama

        /// <summary>Tutanağı yüze yazar; null boş yüz bırakır.</summary>
        public void Bind(ShelterEventPaperData d)
        {
            Data = d;
            Request();
        }

        /// <summary>Karar izini gösterir/gizler: <paramref name="animate"/> basış (<c>admission.motion.stampPressS</c>), yoksa hemen.</summary>
        public void ShowStamp(bool on, bool animate)
        {
            stampOn = on;
            stampT = on && animate ? 0f : 1f;
            EnsureLaidOut();
            ApplyStampAlpha();
        }

        public override void Advance(float dt)
        {
            if (stampT >= 1f) return;
            float s = BoardViewUtil.Num("admission.motion.stampPressS", 0.12f);
            stampT = s > 0 ? Mathf.Min(1f, stampT + Mathf.Max(0f, dt) / s) : 1f;
            ApplyStampAlpha();
        }

        void ApplyStampAlpha()
        {
            bool show = StampVisible;
            Show(stamp, show);
            if (stampGroup == null) return;
            float a = show ? stampT : 0f;
            if (stampGroup.alpha != a) stampGroup.alpha = a;
        }

        protected override void OnCleared()
        {
            Show(stamp, false);
            rowCount = 0;
        }

        // ------------------------------------------------------------------ tık eşlemesi

        /// <summary>Noktanın (kâğıdın sol üstünden mm, y aşağı) düştüğü satır; satır dışındaysa −1.</summary>
        public int RowAt(Vector2 mm)
        {
            EnsureLaidOut();
            for (int i = 0; i < rowCount; i++)
            {
                var r = rowRects[i];
                if (mm.x >= r.xMin && mm.x <= r.xMax && mm.y >= r.yMin && mm.y <= r.yMax) return i;
            }
            return -1;
        }

        /// <summary>Satır kutusu (mm; sol üst köken, y aşağı).</summary>
        public Rect RowRect(int i)
        {
            EnsureLaidOut();
            return i >= 0 && i < rowCount ? rowRects[i] : Rect.zero;
        }

        /// <summary>Nokta KARAR kutusunda mı.</summary>
        public bool ConfirmContains(Vector2 mm)
        {
            EnsureLaidOut();
            return Data != null && confirmRect.Contains(mm);
        }

        // ------------------------------------------------------------------ yerleşim

        protected override void Layout()
        {
            var d = Data;
            Color ink = AdmissionPaperStyle.Ink, label = AdmissionPaperStyle.Label, secondary = AdmissionPaperStyle.Secondary;
            float overflow = 0f;
            float x0 = Pad, w = WidthMm - 2 * Pad;

            edge.Place(0, 0, WidthMm, HeightMm, Border, AdmissionPaperStyle.SlipEdge);
            inner.Place(InnerInset, InnerInset, WidthMm - 2 * InnerInset, HeightMm - 2 * InnerInset, InnerBorder, AdmissionPaperStyle.DocFrame);

            // ---- künye, başlık, kişi, çizgi, gövde
            float y = Pad;
            string head = header != null ? header.text : null;
            Put(header, head, x0, y, w, HeaderEm, HeaderLine, HeaderSpacing, label, false);
            Put(shift, d.Shift, x0 + w, y, 0, HeaderEm, HeaderLine, ShiftSpacing, label, false, TextAlignmentOptions.BaselineRight);
            overflow = Mathf.Max(overflow, WidthOf(header, head, HeaderEm, HeaderSpacing) + 4f + WidthOf(shift, d.Shift, HeaderEm, ShiftSpacing) - w);
            y += HeaderLine + TitleGap;
            Put(title, d.Title, x0, y, w, TitleEm, TitleLine, TitleSpacing, ink, false);
            overflow = Mathf.Max(overflow, WidthOf(title, d.Title, TitleEm, TitleSpacing) - w);
            y += TitleLine;
            if (!string.IsNullOrEmpty(d.Person)) y += PersonGap;
            y += Put(person, d.Person, x0, y, w, PersonEm, PersonLine, PersonSpacing, secondary, true);
            y += RuleGap;
            Box(rule, x0, y, w, RuleMm, AdmissionPaperStyle.Rule);
            y += RuleMm + RuleGap;
            y += Put(body, d.Body, x0, y, w, BodyEm, BodyLine, 0f, ink, true);

            // ---- tablo başlığı
            float leftW = DecisionColMm, rightX = x0 + leftW + ColumnGap, rightW = WidthMm - Pad - rightX;
            y += TableGap;
            Put(colDecision, colDecision != null ? colDecision.text : null, x0, y, leftW, ColEm, ColLine, ColSpacing, label, false);
            Put(colCost, colCost != null ? colCost.text : null, rightX, y, rightW, ColEm, ColLine, ColSpacing, label, false);
            y += ColLine + 0.4f;
            Box(tableRule, x0, y, w, TableRuleMm, AdmissionPaperStyle.Dotted);
            y += TableRuleMm + TableRuleGap;

            // ---- satırlar
            rowsTop = y;
            int total = d.Rows.Count;
            rowCount = Mathf.Min(total, MaxRows);
            if (total > MaxRows) overflow = Mathf.Max(overflow, 1f);
            float gap = RowGapMm, closedAlpha = ClosedAlpha;
            float nameX = x0 + MarkMm + MarkGap, nameW = leftW - MarkMm - MarkGap;
            for (int i = 0; i < MaxRows; i++)
            {
                if (i < MaxRows - 1) Show(At(rowRules, i), false);
                if (i >= rowCount) { HideRow(i); continue; }
                var r = d.Rows[i];
                float top = y, rowY = y + RowPadTop;
                float alpha = r.Enabled ? 1f : closedAlpha;
                Color nameInk = r.Selected ? SelectedInk : Fade(ink, alpha);

                // sol sütun: işaret kutusu, "1 · ad", GÜVENLİ, KAPALI izi
                var mark = rowMark != null && i < rowMark.Length ? rowMark[i] : null;
                float markY = rowY + (NameLine - MarkMm) / 2f;
                mark?.Place(x0, markY, MarkMm, MarkMm, MarkBorder, r.Selected ? SelectedInk : Fade(label, alpha));
                var tick = At(rowTick, i);
                if (r.Selected) Box(tick, x0 + TickInset, markY + TickInset, MarkMm - 2 * TickInset, MarkMm - 2 * TickInset, SelectedInk);
                else Show(tick, false);
                float left = Put(At(rowName, i), r.Name, nameX, rowY, nameW, NameEm, NameLine, 0f, nameInk, true);
                float ly = rowY + left;
                if (!string.IsNullOrEmpty(r.SafeTag))
                {
                    Put(At(rowSafe, i), r.SafeTag, nameX, ly, nameW, SafeEm, SafeLine, SafeSpacing, Fade(r.SafeWarn ? SafeWarnInk : SafeInk, alpha), false);
                    overflow = Mathf.Max(overflow, WidthOf(At(rowSafe, i), r.SafeTag, SafeEm, SafeSpacing) - nameW);
                    ly += SafeLine;
                }
                else Put(At(rowSafe, i), null, 0, 0, 0, SafeEm, SafeLine, 0f, ink, false);
                ly += LayoutRowStamp(i, r, nameX, ly);

                // sağ sütun: bedel, risk, kit kilidi, kapalı gerekçesi
                float ry = rowY;
                ry += Put(At(rowCost, i), r.Cost, rightX, ry, rightW, CostEm, CostLine, 0f, Fade(ink, alpha), true);
                ry += Put(At(rowRisk, i), r.Risk, rightX, ry, rightW, RiskEm, RiskLine, 0f, Fade(secondary, alpha), true);
                ry += Put(At(rowKit, i), r.KitLock, rightX, ry, rightW, RiskEm, RiskLine, 0f, Fade(KitLockInk, alpha), true);
                ry += Put(At(rowClosed, i), r.Closed, rightX, ry, rightW, RiskEm, RiskLine, 0f, ClosedInk, true);

                float bottom = Mathf.Max(ly, ry);
                y = bottom + gap;
                rowRects[i] = new Rect(x0, top, w, bottom + gap / 2f - top);
                if (i > 0) Box(At(rowRules, i - 1), x0, top - gap / 2f, w, TableRuleMm, Fade(AdmissionPaperStyle.Dotted, 0.7f));
            }
            rowsBottom = rowCount > 0 ? y - gap : rowsTop;

            // ---- alt bant: KARAR kutusu (sağ) ve not satırı (sol)
            float cx = WidthMm - Pad - ConfirmW, cy = HeightMm - Pad - ConfirmH;
            confirmRect = new Rect(cx, cy, ConfirmW, ConfirmH);
            Color confirmInk = d.ConfirmReady ? SelectedInk : label;
            confirmFrame.Place(cx, cy, ConfirmW, ConfirmH, ConfirmBorder, confirmInk);
            string word = confirmLine1 != null ? confirmLine1.text : null;
            float inside = ConfirmW - 2 * (ConfirmBorder + 1.6f);
            Put(confirmLine1, word, cx + ConfirmBorder + 1.6f, cy + ConfirmBorder + 1.2f, inside, ConfirmEm, ConfirmLine1, StampSpacing, confirmInk, false);
            overflow = Mathf.Max(overflow, WidthOf(confirmLine1, word, ConfirmEm, StampSpacing) - inside);
            float c2 = Put(confirmLine2, d.Confirm, cx + ConfirmBorder + 1.6f, cy + ConfirmBorder + 1.2f + ConfirmLine1, inside, ConfirmEm2, ConfirmLine2, 0f,
                d.ConfirmReady ? SelectedInk : label, true);
            if (ConfirmBorder + 1.2f + ConfirmLine1 + c2 > ConfirmH + Eps) overflow = Mathf.Max(overflow, 1f);

            float noteW = cx - 4f - x0, noteTop = HeightMm - Pad;
            if (note != null && !string.IsNullOrEmpty(d.Note))
            {
                float h = Put(note, d.Note, x0, 0, noteW, NoteEm, NoteLine, 0f, ClosedInk, true);
                noteTop = HeightMm - Pad - h;
                Put(note, d.Note, x0, noteTop, noteW, NoteEm, NoteLine, 0f, ClosedInk, true);
            }
            else Put(note, null, 0, 0, 0, NoteEm, NoteLine, 0f, ink, true);

            LayoutStamp(d);

            ContentBottomMm = rowsBottom;
            bool rowsOver = rowsBottom > Mathf.Min(cy, noteTop) - 1f + Eps;
            Overflowing = overflow > Eps || rowsOver;
        }

        void HideRow(int i)
        {
            Put(At(rowName, i), null, 0, 0, 0, NameEm, NameLine, 0f, Color.clear, false);
            Put(At(rowSafe, i), null, 0, 0, 0, SafeEm, SafeLine, 0f, Color.clear, false);
            Put(At(rowCost, i), null, 0, 0, 0, CostEm, CostLine, 0f, Color.clear, false);
            Put(At(rowRisk, i), null, 0, 0, 0, RiskEm, RiskLine, 0f, Color.clear, false);
            Put(At(rowKit, i), null, 0, 0, 0, RiskEm, RiskLine, 0f, Color.clear, false);
            Put(At(rowClosed, i), null, 0, 0, 0, RiskEm, RiskLine, 0f, Color.clear, false);
            if (rowMark != null && i < rowMark.Length) rowMark[i]?.SetActive(false);
            Show(At(rowTick, i), false);
            Show(At(rowStamp, i), false);
            rowRects[i] = Rect.zero;
        }

        /// <summary>Kapalı satırın küçük "KAPALI" izi: sol sütunda, adın (ve GÜVENLİ'nin) altında, 3° dönük. Kapladığı yükseklik.</summary>
        float LayoutRowStamp(int i, ShelterEventRowData r, float x, float top)
        {
            var root = At(rowStamp, i);
            var text = At(rowStampText, i);
            if (root == null || string.IsNullOrEmpty(r.ClosedStamp)) { Show(root, false); return 0f; }
            Show(root, true);
            var colour = Fade(ClosedStampInk, AdmissionPaperStyle.StampOpacity);
            float tw = WidthOf(text, r.ClosedStamp, ClosedEm, StampSpacing);
            float sw = tw + 2 * (ClosedBorder + ClosedPadX), sh = ClosedLine + 2 * (ClosedBorder + ClosedPadY);
            float top2 = top + 0.8f;
            PlaceRotated(root, new Vector2(x + sw / 2f, top2 + sh / 2f), new Vector2(sw, sh), ClosedTiltDeg);
            var frame = rowStampFrame != null && i < rowStampFrame.Length ? rowStampFrame[i] : null;
            frame?.Place(0, 0, sw, sh, ClosedBorder, colour);
            Put(text, r.ClosedStamp, ClosedBorder + ClosedPadX, ClosedBorder + ClosedPadY, tw, ClosedEm, ClosedLine, StampSpacing, colour, false);
            return sh + 1.2f;
        }

        void LayoutStamp(ShelterEventPaperData d)
        {
            if (!HasStamp || stamp == null) { ApplyStampAlpha(); return; }
            var colour = Fade(DecisionStamp, AdmissionPaperStyle.StampOpacity);
            string l1 = d.StampLines[0], l2 = d.StampLines.Length > 1 ? d.StampLines[1] : null;
            float w1 = WidthOf(stampLine1, l1, StampEm1, StampSpacing), w2 = WidthOf(stampLine2, l2, StampEm2, StampSpacing);
            float tw = Mathf.Max(w1, w2);
            float frame = StampOuter + StampInnerGap + StampInnerBorder;
            float sw = tw + 2 * (frame + StampPadX);
            float sh = StampLineH1 + (string.IsNullOrEmpty(l2) ? 0f : StampLineH2) + 2 * (frame + StampPadY);
            // İz satırların altındaki boşluğa basılır (yazıyı örtmesin); boşluk yetmezse satırların ortasına
            float free = confirmRect.yMin - rowsBottom;
            float cyMid = free >= sh + 6f ? rowsBottom + free / 2f : rowCount > 0 ? (rowsTop + rowsBottom) / 2f : HeightMm / 2f;
            PlaceRotated(stamp, new Vector2(WidthMm / 2f - 10f, cyMid), new Vector2(sw, sh), StampTiltDeg);
            stampOuter.Place(0, 0, sw, sh, StampOuter, colour);
            stampInner.Place(StampOuter + StampInnerGap, StampOuter + StampInnerGap, sw - 2 * (StampOuter + StampInnerGap),
                sh - 2 * (StampOuter + StampInnerGap), StampInnerBorder, colour);
            float ty = frame + StampPadY;
            Put(stampLine1, l1, sw / 2f, ty, 0, StampEm1, StampLineH1, StampSpacing, colour, false, TextAlignmentOptions.Baseline);
            Put(stampLine2, l2, sw / 2f, ty + StampLineH1, 0, StampEm2, StampLineH2, StampSpacing, colour, false, TextAlignmentOptions.Baseline);
            ApplyStampAlpha();
        }

        /// <summary>Dönük kökü (merkez pivotlu) sol üst köşeli yerelde <paramref name="centre"/>'e koyar.</summary>
        static void PlaceRotated(RectTransform t, Vector2 centre, Vector2 size, float tiltDeg)
        {
            var corner = new Vector2(0, 1);
            if (t.anchorMin != corner) t.anchorMin = corner;
            if (t.anchorMax != corner) t.anchorMax = corner;
            var pivot = new Vector2(0.5f, 0.5f);
            if (t.pivot != pivot) t.pivot = pivot;
            if (t.sizeDelta != size) t.sizeDelta = size;
            var pos = new Vector3(centre.x, -centre.y, 0f);
            if (t.anchoredPosition3D != pos) t.anchoredPosition3D = pos;
            var rot = Quaternion.Euler(0, 0, tiltDeg);
            if (t.localRotation != rot) t.localRotation = rot;
            if (t.localScale != Vector3.one) t.localScale = Vector3.one;
        }
    }
}
