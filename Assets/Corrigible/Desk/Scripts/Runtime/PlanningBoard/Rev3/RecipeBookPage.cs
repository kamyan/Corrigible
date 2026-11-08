using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Tarif defterinin bir sayfa yüzü (Revizyon 3 · Aşama 5, R18a; PLAN §1.5, Karar D4-A): sayfanın önündeki World Space tuval
    /// (99 × 140 birim, 1 birim = 1 mm, ölçek 0,001). Künye, ad, kırmızı çizgi, tarif kutusu (ÇIKTI, GİRDİ, SÜRE), el notu, alt satır,
    /// köşe sekmesi ve [B] İŞ EMRİ ▶ sekmesi TMP ve küçük şekillerle; kâğıt, çizgiler ve sırt gölgesi sözcüksüz pişirilmiş sayfa
    /// dokusundadır (<c>tx_recipe_page</c>).
    /// <list type="bullet">
    /// <item>Düzen tasarım kaynağının (<c>kagit_kaynak/tarif_acilim_n_{tr,en}.html</c>, 560 × 800 px sayfa) CSS akışından yazılır
    /// (<see cref="RecipePageMetrics.Geometry"/>): konumlar px × (99 / 560) yatay, px × 0,175 düşey; yazı boyları px × 0,175 mm em.
    /// Okuma pozunda 14 px'lik etiketler (künye, ÇIKTI/GİRDİ/SÜRE, yuva, sayfa no, sekmeler) 18 px'e büyür (D5-A); konumları
    /// tasarımdaki gibi kalır, yazı satırına ortalanır.</item>
    /// <item>Tek satırlık alanlar sığmazsa punto küçülmez, <see cref="Overflowing"/> olur; el notu en çok iki satıra sarar.</item>
    /// <item>Yazılarda otomatik punto yok, <c>raycastTarget</c> kapalı, fontun ışıksız malzemesi (kurucu yazar; ışıklı TMP World Space
    /// uGUI'de mürekkebi soldurur); tuvalde <c>GraphicRaycaster</c> yok. Tıklama sayfanın <see cref="Hit"/> çarpıştırıcısından
    /// (<see cref="RecipeBookView"/> açıkken açar).</item>
    /// <item>Sayfa <c>Loc</c> çağırmaz: sözcükler anlık görüntüden (<see cref="RecipeData"/>, <see cref="BookWords"/>) gelir; dil
    /// değişince sunucu yeniden bağlar. Aynı veride iş yapmaz. Kapalıyken gelen bağlama açıldıktan sonraki ilk karede uygulanır.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RecipeBookPage : MonoBehaviour
    {
        const float Eps = 0.01f;
        public const int InputRows = 2;

        [Tooltip("Sayfa tuvali (99 × 140 birim, ölçek 0,001). Boşsa bu nesnenin RectTransform'u.")]
        [SerializeField] RectTransform face;
        [Tooltip("Tarifin yazıları ve kutusu (boş sayfada kapalı). Alt satır ve köşe sekmesi bunun dışındadır.")]
        [SerializeField] RectTransform content;
        [Tooltip("Sayfanın tıklama çarpıştırıcısı (99 × 140 × 2 mm); defter açıkken RecipeBookView açar. Yaprak yüzlerinde hep kapalı.")]
        [SerializeField] BoxCollider hit;

        [Header("Künye ve ad")]
        [SerializeField] TMP_Text headerLeft;
        [SerializeField] TMP_Text headerRight;
        [SerializeField] TMP_Text title;
        [SerializeField] Image redRule;

        [Header("Tarif kutusu")]
        [SerializeField] Image boxEdge;
        [SerializeField] Image boxFill;
        [Tooltip("Fotoğraf köşeleri: sol üst, sağ üst, sağ alt, sol alt.")]
        [SerializeField] BookPageShape[] corners = new BookPageShape[4];
        [SerializeField] TMP_Text outputLabel, inputLabel, timeLabel;
        [SerializeField] BookPageShape outputDots, inputDots, timeDots;
        [SerializeField] Image iconEdge, iconFill, outputIcon;
        [SerializeField] TMP_Text outCount, outName, slotWord;
        [SerializeField] Image[] inputIcons = new Image[InputRows];
        [SerializeField] TMP_Text[] inputNames = new TMP_Text[InputRows];
        [SerializeField] BookPageShape[] inputLeaders = new BookPageShape[InputRows];
        [SerializeField] TMP_Text[] inputQty = new TMP_Text[InputRows];
        [SerializeField] Image clock;
        [SerializeField] TMP_Text timeText;

        [Header("İŞ EMRİ ▶ sekmesi [B]")]
        [SerializeField] Image tabEdge;
        [SerializeField] Image tabFill;
        [SerializeField] BookPageShape tabArrow;
        [SerializeField] TMP_Text tabText;

        [Header("El notu ve alt")]
        [SerializeField] TMP_Text note;
        [SerializeField] Image footerRule;
        [SerializeField] TMP_Text pageNumber, spreadText;
        [Tooltip("Köşe sekmesi: dik üçgen, içindeki ok ve yanındaki yazı (sağ sayfada 'sonraki ►', solda '◄ önceki').")]
        [SerializeField] BookPageShape cornerTab, cornerArrow;
        [SerializeField] TMP_Text cornerText;

        [Header("Simgeler (kart setinden)")]
        [Tooltip("BoardLayout.KitKinds sırasıyla: kit_tek, kit_emp, kit_ana, armor, medkit, emp_cell.")]
        [SerializeField] Sprite[] icons = new Sprite[6];
        [Tooltip("BoardLayout.MaterialKinds sırasıyla: metal, devre, kumas, kimyasal.")]
        [SerializeField] Sprite[] materialIcons = new Sprite[4];

        [Header("Ölçüler ve renkler (kurucu prefab'a yazar)")]
        [SerializeField] RecipePageMetrics layout = new RecipePageMetrics();
        [SerializeField] RecipePagePalette palette = new RecipePagePalette();

        string cachedKey;
        bool laidOut, pending;

        /// <summary>Bütün sayfalarda gerçekleşen yerleşim sayısı (önbellekten dönmeyen bağlamalar; test ve başarım).</summary>
        public static int LayoutCount;

        /// <summary>Sayfadaki tarif (null: boş sayfa).</summary>
        public RecipeData Recipe { get; private set; }
        public BookWords Words { get; private set; }
        public bool RightPage { get; private set; }
        public bool ShowsPrev { get; private set; }
        public bool ShowsNext { get; private set; }
        public bool ShowsTab { get; private set; }
        /// <summary>Son yerleşimde tek satırlık bir alan sütununa sığmadı ya da not iki satırı aştı (taşma testi okur).</summary>
        public bool Overflowing { get; private set; }
        /// <summary>Taşan alanların adları (tanı için; boşsa taşma yok).</summary>
        public IReadOnlyList<string> OverflowFields => overflow;
        readonly List<string> overflow = new List<string>();

        public RectTransform Face => face != null ? face : transform as RectTransform;
        public RectTransform Content => content;
        public BoxCollider Hit => hit;
        public RecipePageMetrics Layout => layout;
        public RecipePagePalette Palette => palette;

        public TMP_Text HeaderLeft => headerLeft;
        public TMP_Text HeaderRight => headerRight;
        public TMP_Text Title => title;
        public Image RedRule => redRule;
        public Image BoxEdge => boxEdge;
        public Image BoxFill => boxFill;
        public IReadOnlyList<BookPageShape> Corners => corners;
        public TMP_Text OutputLabel => outputLabel;
        public TMP_Text InputLabel => inputLabel;
        public TMP_Text TimeLabel => timeLabel;
        public Image IconEdge => iconEdge;
        public Image OutputIcon => outputIcon;
        public TMP_Text OutCount => outCount;
        public TMP_Text OutName => outName;
        public TMP_Text SlotWord => slotWord;
        public IReadOnlyList<Image> InputIcons => inputIcons;
        public IReadOnlyList<TMP_Text> InputNames => inputNames;
        public IReadOnlyList<TMP_Text> InputQty => inputQty;
        public Image Clock => clock;
        public TMP_Text TimeText => timeText;
        public Image TabEdge => tabEdge;
        public TMP_Text TabText => tabText;
        public TMP_Text Note => note;
        public Image FooterRule => footerRule;
        public TMP_Text PageNumber => pageNumber;
        public TMP_Text SpreadText => spreadText;
        public BookPageShape CornerTab => cornerTab;
        public TMP_Text CornerText => cornerText;
        public IReadOnlyList<Sprite> Icons => icons;
        public IReadOnlyList<Sprite> MaterialIcons => materialIcons;

        /// <summary>Adlar (punto taraması "ad ≥ 18 px" sınıfı): sayfa başlığı ve çıktı adı.</summary>
        public IEnumerable<TMP_Text> NameTexts
        {
            get
            {
                if (title != null) yield return title;
                if (outName != null) yield return outName;
            }
        }

        /// <summary>Öteki görünür yazılar (punto taraması "≥ 14 px" sınıfı).</summary>
        public IEnumerable<TMP_Text> ValueTexts
        {
            get
            {
                var names = new HashSet<TMP_Text>(NameTexts);
                foreach (var t in GetComponentsInChildren<TMP_Text>(false))
                    if (!names.Contains(t) && !string.IsNullOrEmpty(t.text)) yield return t;
            }
        }

        /// <summary>Kurucunun (tek seferlik editör script'i) ölçü, renk ve simge yazımı.</summary>
        public void Configure(RecipePageMetrics metrics, RecipePagePalette colours, Sprite[] kitIcons = null, Sprite[] matIcons = null)
        {
            if (metrics != null) layout = metrics;
            if (colours != null) palette = colours;
            if (kitIcons != null) icons = kitIcons;
            if (matIcons != null) materialIcons = matIcons;
            cachedKey = null;
        }

        // ================================================================== bağlama

        /// <summary>
        /// Sayfayı bağlar; <paramref name="recipe"/> null ise boş sayfa. <paramref name="rightPage"/>: sağ sayfa (iç boşluklar ve açılım
        /// satırı); <paramref name="showPrev"/>/<paramref name="showNext"/>: köşe sekmeleri (solda önceki, sağda sonraki);
        /// <paramref name="showTab"/>: [B] İŞ EMRİ ▶. Veri önceki bağlamayla aynıysa hiçbir şey yapmaz.
        /// </summary>
        public void Bind(RecipeData recipe, BookWords words, bool rightPage, bool showPrev, bool showNext, bool showTab)
        {
            Recipe = recipe;
            Words = words;
            RightPage = rightPage;
            ShowsPrev = !rightPage && showPrev;
            ShowsNext = rightPage && showNext;
            ShowsTab = showTab && recipe != null;
            string key = Key();
            if (laidOut && !pending && key == cachedKey) return;
            cachedKey = key;
            if (!gameObject.activeInHierarchy) { pending = true; return; }
            Relayout();
        }

        /// <summary>Aynı veriyi yeniden yazar (önbelleği atlar).</summary>
        public void Refresh()
        {
            cachedKey = Key();
            if (!gameObject.activeInHierarchy) { pending = true; return; }
            Relayout();
        }

        // Kapalıyken gelen bağlama: açıldıktan sonraki ilk karede (çocuk yazılar o zaman uyanmış olur)
        void LateUpdate()
        {
            if (pending && gameObject.activeInHierarchy) Relayout();
        }

        string Key()
        {
            var sb = new System.Text.StringBuilder(256);
            sb.Append(RightPage ? 'R' : 'L').Append(ShowsPrev ? 'p' : '-').Append(ShowsNext ? 'n' : '-').Append(ShowsTab ? 't' : '-').Append('|');
            var w = Words;
            if (w != null) sb.Append(w.Header).Append('|').Append(w.Output).Append('|').Append(w.Input).Append('|').Append(w.Time).Append('|')
                .Append(w.Next).Append('|').Append(w.Prev).Append('|').Append(w.Tab).Append('|');
            var r = Recipe;
            if (r == null) return sb.Append("∅").ToString();
            sb.Append(r.Id).Append('|').Append(r.Name).Append('|').Append(r.Title).Append('|').Append(r.Number).Append('|').Append(r.SlotWord).Append('|')
                .Append(r.OutLine).Append('|').Append(r.TurnsLine).Append('|').Append(r.Note).Append('|').Append(r.PageLabel).Append('|').Append(r.SpreadLabel);
            foreach (var i in r.Inputs)
                if (i != null) sb.Append('|').Append(i.Kind).Append(':').Append(i.Name).Append(':').Append(i.BookQty);
            return sb.ToString();
        }

        // ================================================================== dönüşüm

        /// <summary>Dünya noktasının sayfa mm'si (sol üst köken, y aşağı; tuvalin dışında da hesaplanır).</summary>
        public Vector2 PageMm(Vector3 world)
        {
            var rt = Face;
            if (rt == null) return new Vector2(-1, -1);
            var local = rt.InverseTransformPoint(world);
            var r = rt.rect;
            return new Vector2(local.x - r.xMin, r.yMax - local.y);
        }

        /// <summary>Sayfa mm'sinin (sol üst köken) dünya noktası; <paramref name="liftMm"/> tuvalin önüne (yüz yönüne) öteler.</summary>
        public Vector3 WorldAt(Vector2 mm, float liftMm = 0f)
        {
            var rt = Face;
            if (rt == null) return transform.position;
            var r = rt.rect;
            return rt.TransformPoint(new Vector3(r.xMin + mm.x, r.yMax - mm.y, -liftMm));
        }

        /// <summary>Tarif kutusunun ortasının dünya pozu (dönüş tuvalinki: yazının okunduğu yüz −ileri).</summary>
        public Pose RecipeBoxPose()
        {
            var g = (layout ?? new RecipePageMetrics()).Geometry(RightPage);
            var rt = Face;
            return new Pose(WorldAt(g.Box.center), rt != null ? rt.rotation : transform.rotation);
        }

        // ================================================================== yerleşim

        void Relayout()
        {
            pending = false;
            laidOut = true;
            LayoutCount++;
            overflow.Clear();
            var m = layout ?? (layout = new RecipePageMetrics());
            var p = palette ?? (palette = new RecipePagePalette());
            var g = m.Geometry(RightPage);
            var r = Recipe;
            var w = Words ?? new BookWords();

            bool show = r != null;
            if (content != null && content.gameObject.activeSelf != show) content.gameObject.SetActive(show);
            if (show) LayoutRecipe(r, w, m, p, g);
            LayoutFooter(r, w, m, p, g);
            Overflowing = overflow.Count > 0;
        }

        void LayoutRecipe(RecipeData r, BookWords w, RecipePageMetrics m, RecipePagePalette p, RecipePageGeometry g)
        {
            float em(float px) => px * m.emMmPerPx;

            // ---- künye (sol ve sağ, tek satır) · ad · kırmızı çizgi
            float hl = Single(headerLeft, w.Header, em(m.headerEmPx), m.headerSpacingEm, p.label, g.ContentLeft, g.HeaderCenter, TextAlignmentOptions.BaselineLeft);
            float hr = Single(headerRight, r.Number, em(m.headerEmPx), m.headerRightSpacingEm, p.label, g.ContentRight, g.HeaderCenter, TextAlignmentOptions.BaselineRight);
            Fits("künye", hl + hr + g.X(m.headerGapPx), g.ContentWidth);
            float tw = Single(title, r.Title, em(m.titleEmPx), m.titleSpacingEm, p.ink, g.ContentLeft, g.TitleCenter, TextAlignmentOptions.BaselineLeft);
            Fits("ad", tw, g.ContentWidth);
            Box(redRule, g.RedRule, p.red);

            // ---- tarif kutusu, köşeler
            Box(boxEdge, g.Box, p.boxEdge);
            Box(boxFill, Inset(g.Box, g.X(m.boxBorderPx), g.Y(m.boxBorderPx), g.X(m.boxBorderPx), g.Y(m.boxBorderPx)), p.box);
            for (int i = 0; i < corners.Length && i < g.Corners.Length; i++)
            {
                var c = corners[i];
                if (c == null) continue;
                c.Kind = (BookPageShape.Shape)i;   // CornerTopLeft, CornerTopRight, CornerBottomRight, CornerBottomLeft
                Box(c, g.Corners[i], p.corner);
            }

            // ---- İŞ EMRİ ▶ sekmesi (kutunun sağında; dikey yazı alttan üste)
            bool tab = ShowsTab;
            SetActive(tabEdge, tab); SetActive(tabFill, tab); SetActive(tabArrow, tab); SetActive(tabText, tab);
            if (tab)
            {
                float tEm = em(m.tabEmPx);
                Style(tabText, tEm, m.tabSpacingEm, p.ink, false);
                Set(tabText, w.Tab);
                float len = Width(tabText, w.Tab);
                float arrow = em(m.tabArrowPx), gap = g.Y(m.tabGapPx);
                float column = arrow + gap + len;
                // Sekme tasarım boyunda (TR "İŞ EMRİ"); daha uzun dilde (EN "WORK ORDER") punto küçülmez, sekme aşağı doğru uzar
                var tabRect = g.Tab;
                float need = column + 2 * g.Y(m.tabBorderPx) + 2 * gap;
                if (need > tabRect.height) tabRect.height = need;
                Box(tabEdge, tabRect, p.secondary);
                Box(tabFill, Inset(tabRect, 0, g.Y(m.tabBorderPx), g.X(m.tabBorderPx), g.Y(m.tabBorderPx)), p.box);
                Fits("İŞ EMRİ sekmesi", column, tabRect.height - 2 * g.Y(m.tabBorderPx));
                float top = tabRect.y + (tabRect.height - column) / 2;
                float cx = tabRect.x + (tabRect.width - g.X(m.tabBorderPx)) / 2;
                if (tabArrow != null)
                {
                    tabArrow.Kind = BookPageShape.Shape.ArrowRight;
                    float a = arrow * m.tabArrowInset;
                    Box(tabArrow, new Rect(cx - a / 2, top + (arrow - a) / 2, a, a), p.ink);
                }
                if (tabText != null)
                {
                    float line = HandoverSlipText.NormalLine(tabText, tEm);
                    float d = HandoverSlipText.Baseline(tabText, tEm, line) - line / 2;   // taban çizgisi satır ortasının bu kadar altında
                    // 90° sola döndürülmüş satır: taban çizgisi görsel ortanın sağında kalır
                    HandoverSlipText.PlaceLine(tabText, cx + d, top + arrow + gap + len / 2, len, line, TextAlignmentOptions.Baseline);
                    var rot = Quaternion.Euler(0, 0, 90);
                    if (tabText.rectTransform.localRotation != rot) tabText.rectTransform.localRotation = rot;
                }
            }

            // ---- ÇIKTI
            float lEm = em(m.labelEmPx);
            Label(outputLabel, outputDots, w.Output, g.OutLabelCenter, m, p, g, "ÇIKTI");
            Box(iconEdge, g.IconBox, p.secondary);
            Box(iconFill, Inset(g.IconBox, g.X(m.iconBorderPx), g.Y(m.iconBorderPx), g.X(m.iconBorderPx), g.Y(m.iconBorderPx)), p.page);
            Picture(outputIcon, Pick(icons, Array.IndexOf(BoardLayout.KitKinds, r.Id)), g.Icon, p.secondary);

            float cEm = em(m.countEmPx);
            Style(outCount, cEm, 0, p.ink, false);
            Set(outCount, r.OutLine);
            float cw = Width(outCount, r.OutLine);
            float countLine = g.Y(m.countLinePx);
            float baseline = g.CountTop + (outCount != null ? HandoverSlipText.Baseline(outCount, cEm, countLine) : cEm * 0.78f);
            if (outCount != null) HandoverSlipText.PlaceLine(outCount, g.TextX, baseline, Mathf.Max(cw, Eps), countLine, TextAlignmentOptions.BaselineLeft);
            float nameX = g.TextX + cw + (string.IsNullOrEmpty(r.OutLine) ? 0 : g.X(m.countGapPx));
            float nEm = em(m.outNameEmPx);
            Style(outName, nEm, 0, p.ink, false);
            Set(outName, r.Name);
            float nw = Width(outName, r.Name);
            if (outName != null)
                HandoverSlipText.PlaceLine(outName, nameX, baseline, Mathf.Max(nw, Eps), HandoverSlipText.NormalLine(outName, nEm), TextAlignmentOptions.BaselineLeft);
            Fits("çıktı adı", nameX + nw, g.InnerRight);
            float sw = Single(slotWord, r.SlotWord, em(m.slotEmPx), m.slotSpacingEm, p.label, g.TextX, g.SlotCenter, TextAlignmentOptions.BaselineLeft);
            Fits("yuva", g.TextX + sw, g.InnerRight);

            // ---- GİRDİ (her girdi: simge, ad, nokta çizgisi, "× n")
            Label(inputLabel, inputDots, w.Input, g.InLabelCenter, m, p, g, "GİRDİ");
            int n = r.Inputs.Count;
            if (n > InputRows) overflow.Add("girdi sayısı " + n);
            for (int i = 0; i < InputRows; i++)
            {
                var input = i < n ? r.Inputs[i] : null;
                var icon = At(inputIcons, i);
                var name = At(inputNames, i);
                var lead = At(inputLeaders, i);
                var qty = At(inputQty, i);
                bool on = input != null;
                SetActive(icon, on); SetActive(name, on); SetActive(lead, on); SetActive(qty, on);
                if (!on) continue;
                float cy = g.InRowCenter(i);
                float iconMm = g.X(m.inputIconPx);
                Picture(icon, Pick(materialIcons, Array.IndexOf(BoardLayout.MaterialKinds, input.Kind)),
                    new Rect(g.InnerLeft, cy - g.Y(m.inputIconPx) / 2, iconMm, g.Y(m.inputIconPx)), p.secondary);
                float nx = g.InnerLeft + iconMm + g.X(m.inputNameGapPx);
                float w1 = Single(name, input.Name, em(m.inputNameEmPx), 0, p.ink, nx, cy, TextAlignmentOptions.BaselineLeft);
                float w2 = Single(qty, input.BookQty, em(m.qtyEmPx), 0, p.ink, g.InnerRight, cy, TextAlignmentOptions.BaselineRight);
                float l0 = nx + w1 + g.X(m.inputLeaderMarginPx), l1 = g.InnerRight - w2 - g.X(m.inputLeaderMarginPx);
                Fits("girdi " + (i + 1), l0 + g.X(m.leaderMinPx), l1);
                Dots(lead, l0, l1, cy + g.Y(m.inputLeaderDropPx), m, p, g);
            }

            // ---- SÜRE
            Label(timeLabel, timeDots, w.Time, g.TimeLabelCenter, m, p, g, "SÜRE");
            Picture(clock, clock != null ? clock.sprite : null, new Rect(g.InnerLeft, g.TimeCenter - g.Y(m.clockPx) / 2, g.X(m.clockPx), g.Y(m.clockPx)), p.secondary);
            float tx = g.InnerLeft + g.X(m.clockPx) + g.X(m.clockGapPx);
            float tw2 = Single(timeText, r.TurnsLine, em(m.timeEmPx), 0, p.ink, tx, g.TimeCenter, TextAlignmentOptions.BaselineLeft);
            Fits("süre", tx + tw2, g.InnerRight);

            // ---- el notu (kurşun kalem, −2°; sarar, en çok iki satır)
            bool hasNote = !string.IsNullOrEmpty(r.Note);
            SetActive(note, hasNote);
            if (hasNote && note != null)
            {
                float noteEm = em(m.noteEmPx), noteLine = g.Y(m.noteLinePx);
                HandoverSlipText.Style(note, noteEm, 0, p.pencil, true, noteLine);
                Set(note, r.Note);
                HandoverSlipText.PlaceLine(note, g.NoteLeft, g.NoteTop + HandoverSlipText.Baseline(note, noteEm, noteLine), g.NoteWidth, noteLine,
                    TextAlignmentOptions.BaselineLeft);
                int lines = HandoverSlipText.Lines(note);
                if (lines > m.noteMaxLines) overflow.Add("not " + lines + " satır");
                if (g.NoteTop + lines * noteLine > g.FooterRule.y - Eps) overflow.Add("not alt çizgiye taşıyor");
                // CSS rotate(−2deg) saat yönünün tersi; tuvalde (y yukarı) +z
                var rot = Quaternion.Euler(0, 0, -m.noteTiltDeg);
                if (note.rectTransform.localRotation != rot) note.rectTransform.localRotation = rot;
            }
        }

        /// <summary>Alt çizgi, sayfa no, açılım (sağ sayfa), köşe sekmesi. Boş sayfada da çizgi ve sekme durur.</summary>
        void LayoutFooter(RecipeData r, BookWords w, RecipePageMetrics m, RecipePagePalette p, RecipePageGeometry g)
        {
            float em(float px) => px * m.emMmPerPx;
            var ruleColour = p.secondary;
            ruleColour.a *= m.footerRuleAlpha;
            Box(footerRule, g.FooterRule, ruleColour);

            float fEm = em(m.footerEmPx);
            string number = r != null ? r.PageLabel : null;
            float pw = Single(pageNumber, number, fEm, 0, p.label, g.PageNumberX, g.FooterCenter,
                RightPage ? TextAlignmentOptions.BaselineLeft : TextAlignmentOptions.BaselineRight);
            SetActive(pageNumber, !string.IsNullOrEmpty(number));

            string spread = RightPage && r != null ? r.SpreadLabel : null;
            bool hasSpread = !string.IsNullOrEmpty(spread);
            SetActive(spreadText, hasSpread);
            float spw = hasSpread ? Single(spreadText, spread, fEm, m.spreadSpacingEm, p.secondary, g.SpreadX, g.FooterCenter, TextAlignmentOptions.Baseline) : 0;

            bool corner = RightPage ? ShowsNext : ShowsPrev;
            SetActive(cornerTab, corner); SetActive(cornerArrow, corner); SetActive(cornerText, corner);
            float gap = g.X(m.footerGapPx);
            if (corner)
            {
                if (cornerTab != null) cornerTab.Kind = RightPage ? BookPageShape.Shape.CornerBottomRight : BookPageShape.Shape.CornerBottomLeft;
                Box(cornerTab, g.CornerTab, p.secondary);
                if (cornerArrow != null) cornerArrow.Kind = RightPage ? BookPageShape.Shape.ArrowRight : BookPageShape.Shape.ArrowLeft;
                Box(cornerArrow, g.CornerArrow, p.page);
                string text = RightPage ? w.Next : w.Prev;
                float cw = Single(cornerText, text, em(m.cornerTextEmPx), m.cornerTextSpacingEm, p.secondary, g.CornerTextX, g.CornerTextCenter,
                    RightPage ? TextAlignmentOptions.BaselineRight : TextAlignmentOptions.BaselineLeft);
                if (RightPage)
                {
                    float left = g.CornerTextX - cw;
                    if (hasSpread) Fits("sonraki ↔ açılım", g.SpreadX + spw / 2 + gap, left);
                    else Fits("sonraki ↔ sayfa no", g.PageNumberX + pw + gap, left);
                }
                else Fits("önceki ↔ sayfa no", g.CornerTextX + cw + gap, g.PageNumberX - pw);
            }
            if (RightPage && hasSpread) Fits("sayfa no ↔ açılım", g.PageNumberX + pw + gap, g.SpreadX - spw / 2);
        }

        /// <summary>Bölüm etiketi (ÇIKTI, GİRDİ, SÜRE) ve sağına kutunun iç kenarına dek noktalı kılavuz.</summary>
        void Label(TMP_Text label, BookPageShape dots, string text, float centerY, RecipePageMetrics m, RecipePagePalette p, RecipePageGeometry g, string field)
        {
            float lw = Single(label, text, m.labelEmPx * m.emMmPerPx, m.labelSpacingEm, p.label, g.InnerLeft, centerY, TextAlignmentOptions.BaselineLeft);
            float x0 = g.InnerLeft + lw + g.X(m.dotsGapPx);
            Fits(field, x0, g.InnerRight);
            Dots(dots, x0, g.InnerRight, centerY, m, p, g);
        }

        void Dots(BookPageShape dots, float x0, float x1, float centerY, RecipePageMetrics m, RecipePagePalette p, RecipePageGeometry g)
        {
            if (dots == null) return;
            dots.Kind = BookPageShape.Shape.Dots;
            dots.SetDots(g.X(m.dotPitchPx), g.X(m.dotPx));
            float h = g.Y(m.dotsHeightPx);
            Box(dots, new Rect(x0, centerY - h / 2, Mathf.Max(0, x1 - x0), h), p.leader);
        }

        // ================================================================== yardımcılar

        void Fits(string field, float used, float limit)
        {
            if (used > limit + Eps) overflow.Add(field);
        }

        /// <summary>Tek satırlık yazı, satır ortası <paramref name="centerY"/>'de; genişliği (mm) döner.</summary>
        static float Single(TMP_Text t, string text, float em, float spacingEm, Color colour, float x, float centerY, TextAlignmentOptions align)
        {
            if (t == null) return 0f;
            Style(t, em, spacingEm, colour, false);
            Set(t, text);
            float width = Width(t, text);
            float line = HandoverSlipText.NormalLine(t, em);
            float top = centerY - line / 2;
            HandoverSlipText.PlaceLine(t, x, top + HandoverSlipText.Baseline(t, em, line), Mathf.Max(width, Eps), line, align);
            return width;
        }

        static void Style(TMP_Text t, float em, float spacingEm, Color colour, bool wrap) => HandoverSlipText.Style(t, em, spacingEm * 100f, colour, wrap);
        static void Set(TMP_Text t, string v) => HandoverSlipText.Set(t, v);
        static float Width(TMP_Text t, string v) => HandoverSlipText.Width(t, v);

        static void Box(Graphic g, Rect mm, Color colour)
        {
            if (g == null) return;
            HandoverSlipText.Place(g.rectTransform, mm.x, mm.y, mm.width, mm.height, new Vector2(0, 1));
            if (g.color != colour) g.color = colour;
            if (g.raycastTarget) g.raycastTarget = false;
        }

        static void Picture(Image img, Sprite sprite, Rect mm, Color colour)
        {
            if (img == null) return;
            if (img.sprite != sprite) img.sprite = sprite;
            if (img.preserveAspect != true) img.preserveAspect = true;
            bool on = sprite != null;
            if (img.enabled != on) img.enabled = on;
            Box(img, mm, colour);
        }

        static Rect Inset(Rect r, float left, float top, float right, float bottom) =>
            new Rect(r.x + left, r.y + top, Mathf.Max(0, r.width - left - right), Mathf.Max(0, r.height - top - bottom));

        static void SetActive(Component c, bool on)
        {
            if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
        }

        static T At<T>(T[] a, int i) where T : class => a != null && i >= 0 && i < a.Length ? a[i] : null;
        static Sprite Pick(Sprite[] a, int i) => a != null && i >= 0 && i < a.Length ? a[i] : null;
    }

    /// <summary>
    /// Tarif sayfasının ölçüleri. Kaynak tasarım HTML'i <c>tasarim/kagit_kaynak/tarif_acilim_n_*.html</c> (bir sayfa 560 × 800 px,
    /// 1× = 4 px/mm; C §1.4): her değer CSS pikselinden yazılır, akış <see cref="Geometry"/>'de hesaplanır. Sayfa modeli 99 × 140 mm:
    /// yatay ölçek 99 / 560, düşey ve yazı ölçeği 0,175 mm/px (0,25 tasarım mm/px × 0,70). Yazı boyları px em; okuma pozunda
    /// 14–15 px'lik etiketler 18 px (D5-A); harf aralıkları em.
    /// </summary>
    [Serializable]
    public sealed class RecipePageMetrics
    {
        public const float DesignWidthPx = 560f, DesignHeightPx = 800f;

        [Header("Kâğıt")]
        public Vector2 sizeMm = new Vector2(99, 140);
        [Tooltip("Yazı boyu ölçeği: tasarım px → mm em (0,175).")]
        public float emMmPerPx = 0.175f;
        [Tooltip("Kenar çizgisi 1 px (dokuda); iç boşluk üst 26, dış kenar 30, sırt tarafı 34 px.")]
        public float borderPx = 1, padTopPx = 26, padOuterPx = 30, padSpinePx = 34;

        [Header("Künye ve ad")]
        public float headerLinePx = 18, headerEmPx = 18, headerSpacingEm = 0.08f, headerRightSpacingEm = 0.04f, headerGapPx = 12;
        public float titleGapPx = 4, titleEmPx = 28, titleLinePx = 28 * 1.15f, titleSpacingEm = 0.04f;
        public float redGapPx = 8, redPx = 2;

        [Header("Tarif kutusu")]
        public float boxGapPx = 30, boxLeftPx = 6, boxRightPx = 44, boxBorderPx = 1;
        public float boxPadTopPx = 20, boxPadSidePx = 22, boxPadBottomPx = 24;
        [Tooltip("Fotoğraf köşesi 26 px, kutunun kenarından 4 px taşar.")]
        public float cornerPx = 26, cornerOutPx = 4;
        [Tooltip("Etiket satırı: tasarımdaki 14 px Courier'nin 'normal' satırı (1,123 em); etiket 18 px'e büyür, satır yeri aynı kalır.")]
        public float labelRowPx = 14 * 1.123f, labelEmPx = 18, labelSpacingEm = 0.08f;
        [Tooltip("Noktalı kılavuz: etiketten 8 px sonra; nokta adımı 5 px, nokta 2 px, kutu 3 px.")]
        public float dotsGapPx = 8, dotPitchPx = 5, dotPx = 2, dotsHeightPx = 3;
        [Tooltip("Çıktı satırı: simge kutusu 92 px + 1,5 px çerçeve (içerik kutusu), simge 70 px, metin 16 px sonra.")]
        public float outputGapPx = 10, iconBoxPx = 95, iconBorderPx = 1.5f, iconPx = 70, iconTextGapPx = 16;
        [Tooltip("'1 ×' 34 px (satır 34), ad 24 px aynı taban çizgisinde 10 px sonra; yuva 18 px 4 px altta.")]
        public float countEmPx = 34, countLinePx = 34, countGapPx = 10, outNameEmPx = 24, outRowPx = 36, slotGapPx = 4, slotEmPx = 18, slotSpacingEm = 0.04f;
        [Tooltip("Girdi satırı en az 44 px: simge 30, ad 21 px (12 px sonra), kılavuz 10 px pay ve 4 px aşağıda, '× n' 26 px.")]
        public float inputGapPx = 22, inputRowsGapPx = 6, inputRowPx = 44, inputIconPx = 30, inputNameGapPx = 12, inputNameEmPx = 21;
        public float inputLeaderMarginPx = 10, inputLeaderDropPx = 4, leaderMinPx = 10, qtyEmPx = 26;
        [Tooltip("Süre satırı: etiketten 20 px sonra; satır 4 + 4 px altta, 27 px; saat 24 px, yazı 10 px sonra 24 px.")]
        public float timeGapPx = 20, timeRowGapPx = 8, timeRowPx = 27, clockPx = 24, clockGapPx = 10, timeEmPx = 24;

        [Header("İŞ EMRİ ▶ sekmesi")]
        [Tooltip("Kutunun sağından 40 px taşar; 38 × 150 px + 1,5 px çerçeve (solu yok); üstü kutunun %40'ında; ok 16 px, 8 px boşluk, dikey yazı 18 px.")]
        public float tabOutPx = 40, tabWidthPx = 38, tabHeightPx = 150, tabBorderPx = 1.5f, tabTopFraction = 0.4f;
        public float tabArrowPx = 16, tabArrowInset = 0.625f, tabGapPx = 8, tabEmPx = 18, tabSpacingEm = 0.12f;

        [Header("El notu (Kalam 700, −2°)")]
        public float noteGapPx = 26, noteLeftPx = 20, noteRightPx = 30, noteEmPx = 22, noteLinePx = 22 * 1.25f, noteTiltDeg = -2f;
        public int noteMaxLines = 2;

        [Header("Alt")]
        [Tooltip("Alt çizgi alttan 44 px, yanlardan 24 px, 1 px, %40; sayfa no ve açılım alttan 14 px. Punto 19 (tasarım 18): sağ sayfanın alt köşesi okuma pozunda kameradan en uzak, 18 px orada 13,9 ekran px kalıyordu (Aşama 5).")]
        public float footerRuleBottomPx = 44, footerRuleSidePx = 24, footerRulePx = 1, footerRuleAlpha = 0.4f;
        public float footerBottomPx = 14, footerSidePx = 24, footerEmPx = 19, spreadSpacingEm = 0.08f, footerGapPx = 8;
        [Tooltip("Köşe sekmesi 58 px üçgen; ok (38…48, 40…52) px; yazı köşeden 64 px, alttan 6 px, 18 px.")]
        public float cornerTabPx = 58, cornerTextOffsetPx = 64, cornerTextBottomPx = 6, cornerTextLinePx = 15 * 1.123f, cornerTextEmPx = 19, cornerTextSpacingEm = 0.04f;
        public Vector4 cornerArrowPx = new Vector4(38, 40, 10, 12);

        [Header("Tıklama bölgeleri (mm)")]
        [Tooltip("Köşe sekmesinin tık alanı: köşede 16 × 16 mm (çizilen üçgenin 1,6 katı) ve yazının şeridi.")]
        public float cornerHitMm = 16, cornerLabelHitWidthMm = 33, cornerLabelHitHeightMm = 7;

        /// <summary>Tek satırlık tasarım yerleşimi (sol ya da sağ sayfa).</summary>
        public RecipePageGeometry Geometry(bool rightPage) => new RecipePageGeometry(this, rightPage);

        static RecipePageMetrics defaults;
        /// <summary>Kod varsayılanları (saf bölge hesabı, <see cref="RecipeBookView.ZoneAt"/>).</summary>
        public static RecipePageMetrics Default => defaults ?? (defaults = new RecipePageMetrics());
    }

    /// <summary>
    /// Tarif sayfasının CSS akışından hesaplanmış yerleri (mm, sayfanın sol üstünden, y aşağı). Sol sayfanın içeriği 31 px'ten, sağ
    /// sayfanınki 35 px'ten başlar (sırt tarafı 34 px); öteki her şey aynı akıştır. Tarif kutusu bütün tariflerde iki girdi satırıyla
    /// aynı yükseklikte (B §2: her tarifte tam iki girdi).
    /// </summary>
    public readonly struct RecipePageGeometry
    {
        public readonly bool RightPage;
        public readonly float Sx, Sy;
        public readonly float ContentLeft, ContentRight, HeaderCenter, TitleCenter;
        public readonly Rect RedRule, Box, Tab, IconBox, Icon, FooterRule, CornerTab, CornerArrow;
        public readonly Rect[] Corners;
        public readonly float InnerLeft, InnerRight, InnerTop, OutLabelCenter, CountTop, TextX, SlotCenter, InLabelCenter, InRowsTop, InRowStep;
        public readonly float TimeLabelCenter, TimeCenter, NoteLeft, NoteTop, NoteWidth, FooterCenter, PageNumberX, SpreadX, CornerTextX, CornerTextCenter;
        public readonly Rect NextHit, NextLabelHit, PrevHit, PrevLabelHit;

        public float ContentWidth => ContentRight - ContentLeft;
        public float InRowCenter(int i) => InRowsTop + InRowStep * (i + 0.5f);
        public float X(float px) => px * Sx;
        public float Y(float px) => px * Sy;

        public RecipePageGeometry(RecipePageMetrics m, bool right)
        {
            RightPage = right;
            Sx = m.sizeMm.x / RecipePageMetrics.DesignWidthPx;
            Sy = m.sizeMm.y / RecipePageMetrics.DesignHeightPx;
            float W = RecipePageMetrics.DesignWidthPx, H = RecipePageMetrics.DesignHeightPx, b = m.borderPx;
            float sx = Sx, sy = Sy;
            Rect R(float x, float y, float w, float h) => new Rect(x * sx, y * sy, w * sx, h * sy);

            float cl = b + (right ? m.padSpinePx : m.padOuterPx), cr = W - b - (right ? m.padOuterPx : m.padSpinePx);
            float headerTop = b + m.padTopPx;
            float titleTop = headerTop + m.headerLinePx + m.titleGapPx;
            float redTop = titleTop + m.titleLinePx + m.redGapPx;
            float boxTop = redTop + m.redPx + m.boxGapPx, boxLeft = cl + m.boxLeftPx, boxRight = cr - m.boxRightPx;
            float innerTop = boxTop + m.boxBorderPx + m.boxPadTopPx;
            float il = boxLeft + m.boxBorderPx + m.boxPadSidePx, ir = boxRight - m.boxBorderPx - m.boxPadSidePx;
            float outRowTop = innerTop + m.labelRowPx + m.outputGapPx;
            float colH = m.outRowPx + m.slotGapPx + m.labelRowPx, colTop = outRowTop + (m.iconBoxPx - colH) / 2;
            float inLabelTop = outRowTop + m.iconBoxPx + m.inputGapPx;
            float inRowsTop = inLabelTop + m.labelRowPx + m.inputRowsGapPx;
            float timeLabelTop = inRowsTop + RecipeBookPage.InputRows * m.inputRowPx + m.timeGapPx;
            float timeRowTop = timeLabelTop + m.labelRowPx + m.timeRowGapPx;
            float boxBottom = timeRowTop + m.timeRowPx + m.boxPadBottomPx + m.boxBorderPx;

            ContentLeft = cl * sx; ContentRight = cr * sx;
            HeaderCenter = (headerTop + m.headerLinePx / 2) * sy;
            TitleCenter = (titleTop + m.titleLinePx / 2) * sy;
            RedRule = R(cl, redTop, cr - cl, m.redPx);
            Box = R(boxLeft, boxTop, boxRight - boxLeft, boxBottom - boxTop);
            float cIn = m.boxBorderPx - m.cornerOutPx;   // köşe kutunun iç (dolgu) kenarından 4 px taşar
            float cx0 = boxLeft + cIn, cy0 = boxTop + cIn, cx1 = boxRight - cIn - m.cornerPx, cy1 = boxBottom - cIn - m.cornerPx;
            Corners = new[] { R(cx0, cy0, m.cornerPx, m.cornerPx), R(cx1, cy0, m.cornerPx, m.cornerPx), R(cx1, cy1, m.cornerPx, m.cornerPx), R(cx0, cy1, m.cornerPx, m.cornerPx) };
            float padTop = boxTop + m.boxBorderPx, padH = boxBottom - boxTop - 2 * m.boxBorderPx;
            float tabRight = boxRight - m.boxBorderPx + m.tabOutPx, tabW = m.tabWidthPx + m.tabBorderPx, tabH = m.tabHeightPx + 2 * m.tabBorderPx;
            Tab = R(tabRight - tabW, padTop + m.tabTopFraction * padH, tabW, tabH);
            InnerLeft = il * sx; InnerRight = ir * sx; InnerTop = innerTop * sy;
            OutLabelCenter = (innerTop + m.labelRowPx / 2) * sy;
            IconBox = R(il, outRowTop, m.iconBoxPx, m.iconBoxPx);
            float iconPad = (m.iconBoxPx - m.iconPx) / 2;
            Icon = R(il + iconPad, outRowTop + iconPad, m.iconPx, m.iconPx);
            CountTop = colTop * sy;
            TextX = (il + m.iconBoxPx + m.iconTextGapPx) * sx;
            SlotCenter = (colTop + m.outRowPx + m.slotGapPx + m.labelRowPx / 2) * sy;
            InLabelCenter = (inLabelTop + m.labelRowPx / 2) * sy;
            InRowsTop = inRowsTop * sy; InRowStep = m.inputRowPx * sy;
            TimeLabelCenter = (timeLabelTop + m.labelRowPx / 2) * sy;
            TimeCenter = (timeRowTop + m.timeRowPx / 2) * sy;
            NoteTop = (boxBottom + m.noteGapPx) * sy;
            NoteLeft = (cl + m.noteLeftPx) * sx;
            NoteWidth = (cr - m.noteRightPx - cl - m.noteLeftPx) * sx;

            // ---- alt (sayfanın dolgu kutusuna göre mutlak: CSS position absolute)
            float pl = b, pr = W - b, pb = H - b;
            float ruleY = pb - m.footerRuleBottomPx - m.footerRulePx;
            FooterRule = R(pl + m.footerRuleSidePx, ruleY, pr - pl - 2 * m.footerRuleSidePx, m.footerRulePx);
            FooterCenter = (pb - m.footerBottomPx - m.labelRowPx / 2) * sy;
            PageNumberX = (right ? pl + m.footerSidePx : pr - m.footerSidePx) * sx;
            SpreadX = W / 2 * sx;
            float tabX = right ? pr - m.cornerTabPx : pl, tabY = pb - m.cornerTabPx;
            CornerTab = R(tabX, tabY, m.cornerTabPx, m.cornerTabPx);
            var a = m.cornerArrowPx;
            CornerArrow = right ? R(tabX + a.x, tabY + a.y, a.z, a.w) : R(tabX + m.cornerTabPx - a.x - a.z, tabY + a.y, a.z, a.w);
            CornerTextX = (right ? pr - m.cornerTextOffsetPx : pl + m.cornerTextOffsetPx) * sx;
            CornerTextCenter = (pb - m.cornerTextBottomPx - m.cornerTextLinePx / 2) * sy;

            // ---- tık bölgeleri (mm): köşe karesi + yazı şeridi
            float pw = m.sizeMm.x, ph = m.sizeMm.y, k = m.cornerHitMm;
            NextHit = new Rect(pw - k, ph - k, k, k);
            NextLabelHit = new Rect(pw - k - m.cornerLabelHitWidthMm, ph - m.cornerLabelHitHeightMm, m.cornerLabelHitWidthMm + k, m.cornerLabelHitHeightMm);
            PrevHit = new Rect(0, ph - k, k, k);
            PrevLabelHit = new Rect(0, ph - m.cornerLabelHitHeightMm, m.cornerLabelHitWidthMm + k, m.cornerLabelHitHeightMm);
        }

        /// <summary>Tarif kutusu ya da İŞ EMRİ sekmesi ([B] basılı tutup sürükleme bölgesi).</summary>
        public bool InRecipe(Vector2 mm) => Box.Contains(mm) || Tab.Contains(mm);
        public bool InNext(Vector2 mm) => NextHit.Contains(mm) || NextLabelHit.Contains(mm);
        public bool InPrev(Vector2 mm) => PrevHit.Contains(mm) || PrevLabelHit.Contains(mm);
    }

    /// <summary>Tarif sayfasının renkleri (KK; kurucu token karşılıklarından yazar).</summary>
    [Serializable]
    public sealed class RecipePagePalette
    {
        [Tooltip("Mürekkep #1E1B16 (ink): ad, değerler, sekme yazısı ve oku.")]
        public Color ink = new Color32(0x1E, 0x1B, 0x16, 0xFF);
        [Tooltip("Etiket #675F50 (doorPaper.head): künye, ÇIKTI/GİRDİ/SÜRE, yuva, sayfa no.")]
        public Color label = new Color32(0x67, 0x5F, 0x50, 0xFF);
        [Tooltip("İkincil #463F34 (report.summaryLine): simge, simge çerçevesi, sekme çerçevesi, köşe sekmesi, açılım, alt çizgi.")]
        public Color secondary = new Color32(0x46, 0x3F, 0x34, 0xFF);
        [Tooltip("Kırmızı çizgi #D2483F (doorPaper.rule).")]
        public Color red = new Color32(0xD2, 0x48, 0x3F, 0xFF);
        [Tooltip("Kutu zemini #FBF7EC (ui.frameButton) ve çizgisi #D2C7AF (slip.edge).")]
        public Color box = new Color32(0xFB, 0xF7, 0xEC, 0xFF);
        public Color boxEdge = new Color32(0xD2, 0xC7, 0xAF, 0xFF);
        [Tooltip("Fotoğraf köşesi #2E2A24.")]
        public Color corner = new Color32(0x2E, 0x2A, 0x24, 0xFF);
        [Tooltip("Sayfa kâğıdı #F3EBD3 (simge kutusu zemini, köşe sekmesinin oku).")]
        public Color page = new Color32(0xF3, 0xEB, 0xD3, 0xFF);
        [Tooltip("Noktalı kılavuz #8E8166 (ui.reportRule).")]
        public Color leader = new Color32(0x8E, 0x81, 0x66, 0xFF);
        [Tooltip("Kurşun kalem el notu #4A4A48 (pen.graphite).")]
        public Color pencil = new Color32(0x4A, 0x4A, 0x48, 0xFF);
    }
}
