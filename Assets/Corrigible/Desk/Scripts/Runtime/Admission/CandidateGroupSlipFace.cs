using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Aday grubunun kapı fişi (210 × 74 mm; KOS aşama 2, tasarım §7). Sol blok: künye "KYGA · KAPI FİŞİ" + geliş vardiyası, başlık
    /// ("3 ADAY"), kırmızı çizgi, bekleme satırı (son vardiyada uyarı kırmızısı), acil satırı, alta yaslı ipucu. GERİ ÇEVİR damgası bu
    /// bloğa basılır; izi bloğun ortasında, 3° dönük, kalın tek kenar. Sağ blok (dikey bölmenin sağı): yer durumu ("YER VAR · 8/10" /
    /// "YER YOK · 10/10"), dolu sığınakta ipucu (yolluk), yollanabilir kişiler iki sütun × 5 satır (işaret karesi + "Ad · Yer"; seçili
    /// satır uyarı kırmızısı) ve en altta seçimin özeti (yolluk ön izlemesi).
    /// <para><b>Veri sözleşmesi</b> (<see cref="CandidateGroupData"/>): bütün metinler çevrilmiş gelir; yüz <c>Loc</c> çağırmaz. Canlı
    /// sayısı <see cref="MaxRows"/>'u aşarsa ilk on satır yazılır ve <see cref="AdmissionPaperFace.Overflowing"/> yanar (kapasite dengede
    /// 10). Satır tıklaması görünümde (<c>CandidateGroupView</c>): <see cref="RowAt"/> kâğıt yerelindeki mm'yi satıra çevirir.</para>
    /// </summary>
    public sealed class CandidateGroupSlipFace : AdmissionPaperFace
    {
        const float Px = 0.25f;
        public const float WidthMm = 210f, HeightMm = 74f;
        public const int MaxRows = 10, RowsPerColumn = 5;

        const float Border = 1 * Px, Pad = 4f;
        /// <summary>Sol blok (damga alanı) ve sağ blok (yer durumu); aralarında dikey bölme.</summary>
        public const float LeftX = Pad, LeftRight = 74f, DividerX = 77f, RightX = 81f, RightRight = WidthMm - Pad;
        public const float TopMm = Pad, BottomMm = HeightMm - Pad;
        const float LeftW = LeftRight - LeftX, RightW = RightRight - RightX;

        const float HeaderEm = 14 * Px, HeaderLine = 18 * Px, HeaderSpacing = 0.08f, ShiftSpacing = 0.04f;
        const float TitleGap = 3 * Px, TitleEm = 24 * Px, TitleLine = 24 * 1.1f * Px, TitleSpacing = 0.04f;
        const float RuleGap = 6 * Px, RuleMm = 2 * Px;
        const float BodyEm = 17 * Px, BodyLine = 17 * 1.25f * Px, NoteEm = 14 * Px, NoteLine = 14 * 1.25f * Px, NoteGap = 4 * Px;
        const float RoomEm = 18 * Px, RoomLine = 22 * Px, RoomSpacing = 0.04f, RoomGap = 4 * Px, RowsGap = 6 * Px;
        const float RowEm = 15 * Px, RowLine = 5.2f, ColumnGap = 3f, MarkMm = 2.6f, MarkGap = 1.5f;
        const float StampEm = 22 * Px, StampLine = 22 * 1.08f * Px, StampSpacing = 0.1f, StampBorder = 4 * Px;
        const float StampPadTop = 4 * Px, StampPadSide = 10 * Px, StampPadBottom = 3 * Px;
        /// <summary>GERİ ÇEVİR izinin dönüşü (CSS −3°).</summary>
        public const float StampTiltDeg = 3f;
        const float StampOpacity = 0.92f;

        [Header("Kâğıt kenarı")]
        [SerializeField] AdmissionFrame edge = new AdmissionFrame();

        [Header("Sol blok")]
        [SerializeField] TMP_Text header;
        [SerializeField] TMP_Text shift;
        [SerializeField] TMP_Text title;
        [SerializeField] Image redRule;
        [Tooltip("Bekleme satırı.")]
        [SerializeField] TMP_Text body;
        [Tooltip("Acil satırı.")]
        [SerializeField] TMP_Text note;
        [Tooltip("Alta yaslı ipucu.")]
        [SerializeField] TMP_Text hint;

        [Header("Sağ blok (yer durumu)")]
        [SerializeField] Image divider;
        [SerializeField] TMP_Text roomTitle;
        [SerializeField] TMP_Text roomHint;
        [SerializeField] TMP_Text replaceLine;
        [SerializeField] TMP_Text[] rows = new TMP_Text[MaxRows];
        [SerializeField] Image[] rowMarks = new Image[MaxRows];

        [Header("GERİ ÇEVİR izi (dönük kök)")]
        [SerializeField] RectTransform stamp;
        [SerializeField] CanvasGroup stampGroup;
        [SerializeField] AdmissionFrame stampFrame = new AdmissionFrame();
        [SerializeField] TMP_Text stampText;

        bool stampOn;
        float stampT = 1f, rowsTop;
        int rowCount;

        public CandidateGroupData Data { get; private set; }
        public override Vector2 SizeMm => new Vector2(WidthMm, HeightMm);
        protected override bool HasData => Data != null;
        bool HasStamp => Data != null && Data.PassStampLines != null && Data.PassStampLines.Length > 0 && !string.IsNullOrEmpty(Data.PassStampLines[0]);

        /// <summary>İz görünür (veri izi taşıyorsa).</summary>
        public bool StampVisible => stampOn && HasStamp;
        /// <summary>İzin merkezi (fişin sol üstünden mm, y aşağı): sol bloğun ortası.</summary>
        public Vector2 StampCenterMm => new Vector2((LeftX + LeftRight) / 2f, (TopMm + BottomMm) / 2f + 4f);
        /// <summary>Yazılan satır sayısı (son yerleşim).</summary>
        public int RowCount { get { EnsureLaidOut(); return rowCount; } }

        public TMP_Text Header => header;
        public TMP_Text Shift => shift;
        public TMP_Text Title => title;
        public TMP_Text Body => body;
        public TMP_Text Note => note;
        public TMP_Text Hint => hint;
        public TMP_Text RoomTitle => roomTitle;
        public TMP_Text RoomHint => roomHint;
        public TMP_Text ReplaceLine => replaceLine;
        public RectTransform Stamp => stamp;
        public TMP_Text StampText => stampText;
        public TMP_Text Row(int i) => rows != null && i >= 0 && i < rows.Length ? rows[i] : null;
        public Image RowMark(int i) => rowMarks != null && i >= 0 && i < rowMarks.Length ? rowMarks[i] : null;

        /// <summary>Grubu yüze yazar; null boş yüz bırakır.</summary>
        public void Bind(CandidateGroupData g)
        {
            Data = g;
            Request();
        }

        /// <summary>GERİ ÇEVİR izini gösterir/gizler: <paramref name="animate"/> basış (<c>admission.motion.stampPressS</c>), yoksa hemen.</summary>
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

        /// <summary>Nokta (fişin sol üstünden mm, y aşağı) damga alanında mı: sol blok.</summary>
        public bool InStampArea(Vector2 mm) => mm.x >= LeftX && mm.x <= LeftRight && mm.y >= TopMm && mm.y <= BottomMm;

        /// <summary>Noktanın (fişin sol üstünden mm, y aşağı) düştüğü satırın dizini; satır kutusunun dışındaysa −1.</summary>
        public int RowAt(Vector2 mm)
        {
            EnsureLaidOut();
            for (int i = 0; i < rowCount; i++)
            {
                var r = RowRect(i);
                if (mm.x >= r.xMin && mm.x <= r.xMax && mm.y >= r.yMin && mm.y <= r.yMax) return i;
            }
            return -1;
        }

        static float ColumnW => (RightW - ColumnGap) / 2f;

        /// <summary>Satır kutusu (mm; sol üst köken, y aşağı).</summary>
        public Rect RowRect(int i)
        {
            EnsureLaidOut();
            int column = i / RowsPerColumn, row = i % RowsPerColumn;
            return new Rect(RightX + column * (ColumnW + ColumnGap), rowsTop + row * RowLine, ColumnW, RowLine);
        }

        protected override void Layout()
        {
            var g = Data;
            Color ink = AdmissionPaperStyle.Ink, label = AdmissionPaperStyle.Label, secondary = AdmissionPaperStyle.Secondary, alarm = AdmissionPaperStyle.Alarm;
            float overflow = 0f;

            edge.Place(0, 0, WidthMm, HeightMm, Border, AdmissionPaperStyle.SlipEdge);

            // ---- sol blok
            float x0 = LeftX, w = LeftW, y = TopMm;
            Put(header, g.SlipHeader, x0, y, w, HeaderEm, HeaderLine, HeaderSpacing, label, false);
            Put(shift, g.SlipShift, x0 + w, y, 0, HeaderEm, HeaderLine, ShiftSpacing, label, false, TextAlignmentOptions.BaselineRight);
            float headerW = WidthOf(header, g.SlipHeader, HeaderEm, HeaderSpacing) + WidthOf(shift, g.SlipShift, HeaderEm, ShiftSpacing);
            if (headerW > w + Eps) overflow = headerW - w;
            y += HeaderLine + TitleGap;
            Put(title, g.SlipTitle, x0, y, w, TitleEm, TitleLine, TitleSpacing, ink, false);
            overflow = Mathf.Max(overflow, WidthOf(title, g.SlipTitle, TitleEm, TitleSpacing) - w);
            y += TitleLine + RuleGap;
            Box(redRule, x0, y, w, RuleMm, AdmissionPaperStyle.Rule);
            y += RuleMm + RuleGap;
            y += Put(body, g.WaitLine, x0, y, w, BodyEm, BodyLine, 0f, g.LastShift ? alarm : ink, true);
            if (!string.IsNullOrEmpty(g.EmergencyLine)) y += NoteGap;
            y += Put(note, g.EmergencyLine, x0, y, w, NoteEm, NoteLine, 0f, secondary, true);
            float hintH = 0f;
            if (hint != null && !string.IsNullOrEmpty(g.HintLine))
            {
                hintH = Put(hint, g.HintLine, x0, 0, w, NoteEm, NoteLine, 0f, label, true);
                Put(hint, g.HintLine, x0, BottomMm - hintH, w, NoteEm, NoteLine, 0f, label, true);
            }
            else Put(hint, null, 0, 0, 0, NoteEm, NoteLine, 0f, label, true);
            float leftBottom = y;
            bool leftOver = leftBottom > BottomMm - hintH + Eps;

            // ---- bölme
            Box(divider, DividerX, TopMm, Border, BottomMm - TopMm, AdmissionPaperStyle.Dotted);

            // ---- sağ blok: yer durumu, ipucu, satırlar, seçim özeti
            float rx = RightX, rw = RightW, ry = TopMm;
            Put(roomTitle, g.RoomTitle, rx, ry, rw, RoomEm, RoomLine, RoomSpacing, g.Full ? alarm : ink, false);
            overflow = Mathf.Max(overflow, WidthOf(roomTitle, g.RoomTitle, RoomEm, RoomSpacing) - rw);
            ry += RoomLine;
            if (!string.IsNullOrEmpty(g.RoomHint)) ry += RoomGap;
            ry += Put(roomHint, g.RoomHint, rx, ry, rw, NoteEm, NoteLine, 0f, label, true);
            ry += RowsGap;
            rowsTop = ry;

            int total = g.Replace != null ? g.Replace.Count : 0;
            rowCount = Mathf.Min(total, MaxRows);
            if (total > MaxRows) overflow = Mathf.Max(overflow, 1f);
            float textW = ColumnW - MarkMm - MarkGap;
            for (int i = 0; i < MaxRows; i++)
            {
                var text = Row(i);
                var mark = RowMark(i);
                if (i >= rowCount)
                {
                    Put(text, null, 0, 0, 0, RowEm, RowLine, 0f, ink, false);
                    Show(mark, false);
                    continue;
                }
                var d = g.Replace[i];
                var r = RowRect(i);
                Color colour = d.Selected ? alarm : d.Enabled ? ink : label;
                Box(mark, r.xMin, r.yMin + (RowLine - MarkMm) / 2f, MarkMm, MarkMm, d.Selected ? alarm : AdmissionPaperStyle.Dotted);
                string line = string.IsNullOrEmpty(d.Label) ? d.Name : d.Label;
                Put(text, line, r.xMin + MarkMm + MarkGap, r.yMin, textW, RowEm, RowLine, 0f, colour, false);
                overflow = Mathf.Max(overflow, WidthOf(text, line, RowEm, 0f) - textW);
            }
            float rowsBottom = rowsTop + Mathf.Min(rowCount, RowsPerColumn) * RowLine;

            float replaceH = 0f;
            if (replaceLine != null && !string.IsNullOrEmpty(g.ReplaceLine))
            {
                replaceH = Put(replaceLine, g.ReplaceLine, rx, 0, rw, NoteEm, NoteLine, 0f, alarm, true);
                Put(replaceLine, g.ReplaceLine, rx, BottomMm - replaceH, rw, NoteEm, NoteLine, 0f, alarm, true);
            }
            else Put(replaceLine, null, 0, 0, 0, NoteEm, NoteLine, 0f, alarm, true);
            bool rightOver = rowCount > 0 && rowsBottom > BottomMm - replaceH + Eps;

            LayoutStamp(g);

            ContentBottomMm = Mathf.Max(leftBottom + hintH, rowsBottom + replaceH);
            Overflowing = overflow > Eps || leftOver || rightOver;
        }

        void LayoutStamp(CandidateGroupData g)
        {
            if (!HasStamp || stamp == null) { ApplyStampAlpha(); return; }
            var colour = Fade(AdmissionPaperStyle.Hex(g.PassStampColor, AdmissionPaperStyle.Stamp(StampChoice.Reject)), StampOpacity);
            string text = g.PassStampLines[0];
            float textW = WidthOf(stampText, text, StampEm, StampSpacing);
            float sw = StampBorder * 2 + StampPadSide * 2 + textW, sh = StampBorder * 2 + StampPadTop + StampPadBottom + StampLine;
            var c = StampCenterMm;

            var corner = new Vector2(0, 1);
            if (stamp.anchorMin != corner) stamp.anchorMin = corner;
            if (stamp.anchorMax != corner) stamp.anchorMax = corner;
            var pivot = new Vector2(0.5f, 0.5f);
            if (stamp.pivot != pivot) stamp.pivot = pivot;
            var size = new Vector2(sw, sh);
            if (stamp.sizeDelta != size) stamp.sizeDelta = size;
            var pos = new Vector3(c.x, -c.y, 0f);
            if (stamp.anchoredPosition3D != pos) stamp.anchoredPosition3D = pos;
            var rot = Quaternion.Euler(0, 0, StampTiltDeg);
            if (stamp.localRotation != rot) stamp.localRotation = rot;
            if (stamp.localScale != Vector3.one) stamp.localScale = Vector3.one;

            stampFrame.Place(0, 0, sw, sh, StampBorder, colour);
            Put(stampText, text, StampBorder + StampPadSide, StampBorder + StampPadTop, textW, StampEm, StampLine, StampSpacing, colour, false);
            ApplyStampAlpha();
        }
    }
}
