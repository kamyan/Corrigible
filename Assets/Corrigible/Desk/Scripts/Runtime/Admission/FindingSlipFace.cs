using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bulgu fişinin yüzü (90 × 60 mm; PLAN §1.7, KK <c>bulgu_*_{tr,en}.html</c>, 1× = 4 px/mm): başlık satırı ("KYGA · İNCELEME
    /// BULGUSU" + vardiya), simge + başlık, kırmızı çizgi, gövde (sarar, küçülmez), alta yaslı not ve −3° sonuç damgası (düz ya da
    /// kesikli kutu; renk <see cref="SlipVerdict"/>'ten). İNCELEME FİŞİ aynı düzeni kullanır (<see cref="SlipData.Review"/>).
    /// Gövde harf harf dökülebilir (<see cref="Type"/>, GÖRÜŞME fişi 0,03 s/harf; süreyi istasyon verir).
    /// <para>Simge araçtan seçilir (<see cref="SlipData.Tool"/>; inceleme fişinde ayrı simge); sözcüksüz beyaz sprite, rengi ikincil
    /// mürekkep. Bütün metinler anlık görüntüden çevrilmiş gelir.</para>
    /// </summary>
    public sealed class FindingSlipFace : AdmissionPaperFace
    {
        // ---- ölçüler (tasarımın 1× pikseli ÷ 4)
        const float Px = 0.25f;
        public const float WidthMm = 90f, HeightMm = 60f;
        const float Border = 1 * Px, PadTop = 10 * Px, PadSide = 14 * Px, PadBottom = 10 * Px;
        const float HeaderEm = 14 * Px, HeaderLine = 18 * Px, HeaderSpacing = 0.08f, ShiftSpacing = 0.04f;
        const float TitleGap = 3 * Px, IconMm = 24 * Px, IconGap = 8 * Px, TitleEm = 17 * Px, TitleLine = 17 * 1.15f * Px, TitleSpacing = 0.04f;
        const float RuleGap = 6 * Px, RuleMm = 2 * Px;
        const float BodyGap = 6 * Px, BodyEm = 18 * Px, BodyLine = 18 * 1.28f * Px;
        const float NoteEm = 14 * Px, NoteLine = 14 * 1.25f * Px, NoteGap = 8 * Px;
        const float StampEm = 14 * Px, StampLine = 14 * 1.08f * Px, StampSpacing = 0.06f, StampBorder = 2 * Px;
        const float StampPadTop = 1 * Px, StampPadRight = 5 * Px, StampPadBottom = 1 * Px, StampPadLeft = 6 * Px;
        /// <summary>Sonuç damgasının dönüşü (CSS −3°: tuvalin önünden bakınca saat yönünün tersi).</summary>
        public const float StampTiltDeg = 3f;
        const float StampOpacity = 0.92f;

        /// <summary>İçerik alanı (mm): sol, üst, genişlik, alt.</summary>
        public const float ContentLeftMm = Border + PadSide, ContentTopMm = Border + PadTop;
        public const float ContentWidthMm = WidthMm - 2 * (Border + PadSide), ContentBottomLimitMm = HeightMm - Border - PadBottom;

        [Header("Kâğıt kenarı (1 px #CDBF97)")]
        [SerializeField] AdmissionFrame edge = new AdmissionFrame();

        [Header("Başlık")]
        [SerializeField] TMP_Text header;
        [SerializeField] TMP_Text shift;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] Image redRule;

        [Header("Gövde ve alt")]
        [SerializeField] TMP_Text body;
        [SerializeField] TMP_Text note;

        [Header("Sonuç damgası (dönük kök; düz ve kesikli çerçeve)")]
        [SerializeField] RectTransform stamp;
        [SerializeField] AdmissionFrame stampFrame = new AdmissionFrame();
        [SerializeField] AdmissionFrame stampDashed = new AdmissionFrame();
        [SerializeField] TMP_Text stampText;

        [Header("Simgeler (beyaz sprite; AdmissionTool sırası + inceleme fişi)")]
        [SerializeField] Sprite[] toolIcons = new Sprite[4];
        [SerializeField] Sprite reviewIcon;

        string boundKey;
        int bodyChars;
        float typeElapsed, typeTotal;
        bool typing;

        /// <summary>Yüzdeki fiş (null: boş yüz).</summary>
        public SlipData Data { get; private set; }
        /// <summary>Gövde dökülüyor.</summary>
        public bool Typing => typing;
        /// <summary>Gövdenin görünen harf sayısı (döküm sırasında artar; tam ise gövdenin harf sayısı).</summary>
        public int VisibleCharacters => body != null ? Mathf.Min(body.maxVisibleCharacters, bodyChars) : 0;
        /// <summary>Gövdenin harf sayısı (döküm süresi = bu × 0,03 s).</summary>
        public int BodyCharacters => bodyChars;
        /// <summary>Gövdenin satır sayısı (son yerleşim).</summary>
        public int BodyLines { get; private set; }
        /// <summary>Alta yaslı not ve damga satırının üstü (gövde bunu aşmamalı).</summary>
        public float FooterTopMm { get; private set; }

        public override Vector2 SizeMm => new Vector2(WidthMm, HeightMm);
        protected override bool HasData => Data != null;

        public TMP_Text Header => header;
        public TMP_Text Shift => shift;
        public Image Icon => icon;
        public TMP_Text Title => title;
        public Image RedRule => redRule;
        public TMP_Text Body => body;
        public TMP_Text Note => note;
        public RectTransform Stamp => stamp;
        public TMP_Text StampText => stampText;
        public AdmissionFrame StampFrame => stampFrame;
        public AdmissionFrame StampDashed => stampDashed;
        public AdmissionFrame Edge => edge;

        /// <summary>Fişi yüze yazar; null boş yüz bırakır. Anahtarı (<see cref="SlipData.Key"/>) değişen fişte döküm sıfırlanır (tam görünür).</summary>
        public void Bind(SlipData d)
        {
            Data = d;
            string key = d == null ? null : (d.Key ?? "") + "|" + d.Body;
            if (key != boundKey) { typing = false; typeElapsed = typeTotal = 0f; }
            boundKey = key;
            Request();
        }

        /// <summary>Gövdeyi <paramref name="seconds"/> içinde harf harf döker (0 ya da daha az = hemen tam).</summary>
        public void Type(float seconds)
        {
            EnsureLaidOut();
            if (seconds <= 0f || bodyChars == 0) { typing = false; ApplyVisible(bodyChars); return; }
            typing = true;
            typeElapsed = 0f;
            typeTotal = seconds;
            ApplyVisible(0);
        }

        public override void Advance(float dt)
        {
            if (!typing) return;
            typeElapsed += Mathf.Max(0f, dt);
            if (typeElapsed >= typeTotal) { typing = false; ApplyVisible(bodyChars); return; }
            ApplyVisible(Mathf.FloorToInt(bodyChars * typeElapsed / typeTotal));
        }

        void ApplyVisible(int n)
        {
            if (body == null) return;
            // tam görünürken TMP'nin "sınırsız" değeri: yeniden bağlamada sınır kalmaz
            int value = n >= bodyChars ? 99999 : Mathf.Max(0, n);
            if (body.maxVisibleCharacters != value) body.maxVisibleCharacters = value;
        }

        protected override void OnCleared()
        {
            typing = false;
            bodyChars = 0;
            BodyLines = 0;
        }

        protected override void Layout()
        {
            var d = Data;
            Color ink = AdmissionPaperStyle.Ink, label = AdmissionPaperStyle.Label;
            float x0 = ContentLeftMm, w = ContentWidthMm, y = ContentTopMm, bottom = ContentBottomLimitMm;

            edge.Place(0, 0, WidthMm, HeightMm, Border, AdmissionPaperStyle.SlipEdge);

            // ---- başlık satırı: sol künye, sağda vardiya (ikisi de tek satır, taban çizgisi ortak)
            Put(header, d.Header, x0, y, w, HeaderEm, HeaderLine, HeaderSpacing, label, false);
            Put(shift, d.Shift, x0 + w, y, 0, HeaderEm, HeaderLine, ShiftSpacing, label, false, TextAlignmentOptions.BaselineRight);
            float overflow = 0f;
            float headerW = WidthOf(header, d.Header, HeaderEm, HeaderSpacing) + WidthOf(shift, d.Shift, HeaderEm, ShiftSpacing);
            if (headerW > w + Eps) overflow = headerW - w;
            y += HeaderLine;

            // ---- simge + başlık (satır yüksekliği simgenin; başlık dikeyde ortalı)
            y += TitleGap;
            var sprite = IconFor(d);
            float titleX = x0;
            if (icon != null)
            {
                bool showIcon = sprite != null;
                Show(icon, showIcon);
                if (showIcon)
                {
                    if (icon.sprite != sprite) icon.sprite = sprite;
                    Box(icon, x0, y, IconMm, IconMm, AdmissionPaperStyle.Secondary);
                    titleX = x0 + IconMm + IconGap;
                }
            }
            float rowH = Mathf.Max(IconMm, TitleLine);
            Put(title, d.Title, titleX, y + (rowH - TitleLine) / 2f, x0 + w - titleX, TitleEm, TitleLine, TitleSpacing, ink, false);
            float titleW = WidthOf(title, d.Title, TitleEm, TitleSpacing);
            if (titleX + titleW > x0 + w + Eps) overflow = Mathf.Max(overflow, titleX + titleW - x0 - w);
            y += rowH;

            // ---- kırmızı çizgi
            y += RuleGap;
            Box(redRule, x0, y, w, RuleMm, AdmissionPaperStyle.Rule);
            y += RuleMm;

            // ---- gövde (sarar)
            y += BodyGap;
            float bodyH = Put(body, d.Body, x0, y, w, BodyEm, BodyLine, 0f, ink, true);
            BodyLines = bodyH > 0 ? Mathf.RoundToInt(bodyH / BodyLine) : 0;
            bodyChars = body != null && !string.IsNullOrEmpty(d.Body) ? body.textInfo.characterCount : 0;
            ApplyVisible(typing ? Mathf.FloorToInt(bodyChars * (typeTotal > 0 ? typeElapsed / typeTotal : 1f)) : bodyChars);
            float bodyBottom = y + bodyH;

            // ---- alt satır: damga sağda (alta yaslı), not solda (sarar, alta yaslı)
            float stampW = 0f, stampH = 0f;
            bool hasStamp = !string.IsNullOrEmpty(d.Verdict);
            Show(stamp, hasStamp);
            if (hasStamp)
            {
                var colour = Fade(AdmissionPaperStyle.Verdict(d.Kind), StampOpacity);
                float textW = WidthOf(stampText, d.Verdict, StampEm, StampSpacing);
                stampW = StampBorder * 2 + StampPadLeft + StampPadRight + textW;
                stampH = StampBorder * 2 + StampPadTop + StampPadBottom + StampLine;
                PlaceStamp(x0 + w - stampW, bottom - stampH, stampW, stampH);
                Put(stampText, d.Verdict, StampBorder + StampPadLeft, StampBorder + StampPadTop, textW, StampEm, StampLine, StampSpacing, colour, false);
                stampFrame.SetActive(!d.Dashed);
                stampDashed.SetActive(d.Dashed);
                (d.Dashed ? stampDashed : stampFrame).Place(0, 0, stampW, stampH, StampBorder, colour);
            }
            float noteW = Mathf.Max(10f, w - (hasStamp ? stampW + NoteGap : 0f));
            float noteH = 0f;
            if (note != null && !string.IsNullOrEmpty(d.Note))
            {
                // önce ölç (yükseklik), sonra alta yaslı yerleştir
                noteH = Put(note, d.Note, x0, 0, noteW, NoteEm, NoteLine, 0f, label, true);
                Put(note, d.Note, x0, bottom - noteH, noteW, NoteEm, NoteLine, 0f, label, true);
            }
            else Put(note, null, 0, 0, 0, NoteEm, NoteLine, 0, label, true);

            float footer = Mathf.Max(noteH, stampH);
            FooterTopMm = bottom - footer;
            ContentBottomMm = bodyBottom;
            Overflowing = overflow > Eps || bodyBottom > FooterTopMm + Eps;
        }

        void PlaceStamp(float x, float top, float w, float h)
        {
            if (stamp == null) return;
            // Dönme kutunun ortasından: pivot orta, konum orta
            var corner = new Vector2(0, 1);
            if (stamp.anchorMin != corner) stamp.anchorMin = corner;
            if (stamp.anchorMax != corner) stamp.anchorMax = corner;
            var pivot = new Vector2(0.5f, 0.5f);
            if (stamp.pivot != pivot) stamp.pivot = pivot;
            var size = new Vector2(w, h);
            if (stamp.sizeDelta != size) stamp.sizeDelta = size;
            var pos = new Vector3(x + w / 2f, -(top + h / 2f), 0f);
            if (stamp.anchoredPosition3D != pos) stamp.anchoredPosition3D = pos;
            var rot = Quaternion.Euler(0, 0, StampTiltDeg);
            if (stamp.localRotation != rot) stamp.localRotation = rot;
            if (stamp.localScale != Vector3.one) stamp.localScale = Vector3.one;
            // Çocuklar (çerçeve, yazı) çapası (0, 1) ile kutunun sol üst köşesine göre yerleşir; pivot ortada olduğu için dönme
            // kutunun ortasından olur
        }

        Sprite IconFor(SlipData d)
        {
            if (d.Review || d.Tool == null) return reviewIcon;
            int i = (int)d.Tool.Value;
            return toolIcons != null && i >= 0 && i < toolIcons.Length ? toolIcons[i] : null;
        }

        /// <summary>Kurucunun (tek seferlik editör script'i) simge bağlaması.</summary>
        public void ConfigureIcons(Sprite[] tools, Sprite review)
        {
            toolIcons = tools ?? new Sprite[4];
            reviewIcon = review;
        }
    }
}
