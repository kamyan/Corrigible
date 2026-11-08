using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kişi kartının yüzü, 80 × 120 mm (PLAN §3.2; <c>boardtex.js</c> <c>personCard</c>; D1/D2 kararlarıyla). Yüz mm'si sol üstten,
    /// y aşağı; yazıların konumu taban çizgisidir:
    /// <list type="bullet">
    /// <item>Ad: Libre Franklin 700, 14 mm, (4,6; 14,6), en çok 58,4 mm (mıknatıs x ≥ 63). Sığmayan numaralı adın eki 9,4 mm
    /// yazılır; yine sığmazsa kart mıknatısı gizler ve ad 70,8 mm'ye kadar uzar (<see cref="NameUsesMagnetSpace"/>).</item>
    /// <item>Meslek: LF 600, 9,5 mm, (4,8; 26,2), en çok 72 mm. Portre (5, 29) 44 × 48,2 mm, ışık alan dörtgen (kartta, tuvalde değil).</item>
    /// <item>Ustalık: 7 satır (Soru 1), harf CP 700 9,5 mm x 52,5; taban çizgileri 35,4 / 41,7 / 48,0 / 55,8 / 62,1 / 68,4 / 76,2
    /// (eleştirmen 5: Ü ve İ satırlarının üstünde 7,8 mm, ötekilerde 6,3 mm); kademe kadar dolu pip (x 61,4 / 66,0 / 70,6, y = taban − 2,8,
    /// Ø 3,8), kalanı soluk halka. Kademe 0'da harf soluk.</item>
    /// <item>Değerler: etiket taban 89, değer taban 100,6 (CP 700 9,5 / 11,5 mm); TR üç hücre × 23,4 mm (merkez 16,2 / 39,6 / 63,0),
    /// EN hücreleri genişler (HP 3,5–24,5 · MOVE 24,5–47,5 · POWER 47,5–76,5) ki "2/3" (20,7 mm), MOVE ve POWER 9,5 mm'de sığsın.
    /// Yaralıyken CAN değeri kırmızı.</item>
    /// <item>Şerit: LF 600 9,4 mm, merkez (40; 113,7), en çok 72 mm; rengi tonundan (mavi yatıyor, kırmızı kısıtlı, soluk boşta).</item>
    /// </list>
    /// Durum katmanları <see cref="CardStateOverlay"/>'dedir.
    /// </summary>
    public sealed class PersonCardFace : CardFace
    {
        public const int Rows = 7, PipsPerRow = 3, Stats = 3;

        [SerializeField] TMP_Text nameText, jobText, stripText;
        [SerializeField] TMP_Text[] letters = new TMP_Text[Rows];
        [Tooltip("Satır sırasıyla: satır · 3 + j.")]
        [SerializeField] Image[] pips = new Image[Rows * PipsPerRow];
        [SerializeField] TMP_Text[] statLabels = new TMP_Text[Stats], statValues = new TMP_Text[Stats];
        [SerializeField] RectTransform[] dividers = new RectTransform[Stats - 1];
        [SerializeField] CardStateOverlay overlay;

        [Header("Pipler (resim beyazsa renk boyar, renkliyse beyaz)")]
        [SerializeField] Sprite pipFill;
        [SerializeField] Sprite pipRing;
        [SerializeField] Color pipFillTint = Color.white, pipRingTint = Color.white;

        [Header("Renkler (kurucu prefab'a yazar)")]
        [SerializeField] Color ink;
        [SerializeField] Color mute, dim, red, blue;

        [Header("Ad (mm)")]
        [SerializeField] float nameMaxMm = 58.4f;
        [SerializeField] float nameMaxNoMagnetMm = 70.8f, suffixEmMm = 9.4f;

        [Header("Alan genişlikleri ve değer hücreleri (mm)")]
        [SerializeField] float jobMaxMm = 72f;
        [SerializeField] float stripMaxMm = 72f;
        [SerializeField] float[] statCentresTr = { 16.2f, 39.6f, 63.0f }, statWidthsTr = { 23.4f, 23.4f, 23.4f }, dividersTr = { 28.4f, 51.8f };
        [SerializeField] float[] statCentresEn = { 14.0f, 36.0f, 62.0f }, statWidthsEn = { 21f, 23f, 29f }, dividersEn = { 24.5f, 47.5f };
        [Tooltip("EN değer etiketlerinin harf aralığı (TMP birimi, em/100). CP-Bold eşaralıklı: 9,5 mm'de MOVE 22,8, POWER 28,5 mm; " +
                 "hücreleri kenara kadar doldurup ayraçta birleşmesinler diye sıkılır (punto küçülmez).")]
        [SerializeField] float statLabelSpacingEn = -6f;

        // ---- önbellek
        bool bound, cachedEnglish, cellsEnglish, cellsPlaced, cachedHurt;
        string cachedName, cachedJob, cachedStrip;
        StripTone cachedTone;
        PersonStates cachedStates;
        string[] cachedLetters, cachedLabels, cachedValues;
        int[] cachedTiers;
        string wInjured, wBed, wRestricted, wReview, wLost;
        string suffixOpen;
        bool nameUsesMagnetSpace;

        public override BoardCardKind Kind => BoardCardKind.Person;
        public override TMP_Text NameText => nameText;
        public CardStateOverlay Overlay => overlay;
        public TMP_Text JobText => jobText;
        public TMP_Text StripText => stripText;
        public IReadOnlyList<TMP_Text> Letters => letters;
        public IReadOnlyList<Image> Pips => pips;
        public IReadOnlyList<TMP_Text> StatLabels => statLabels;
        public IReadOnlyList<TMP_Text> StatValues => statValues;
        public IReadOnlyList<RectTransform> Dividers => dividers;

        /// <summary>Ad 58,4 mm'ye sığmadı: kart mıknatısı gizlenmeli (D2; süs, durum taşımaz).</summary>
        public bool NameUsesMagnetSpace => nameUsesMagnetSpace;

        public override IEnumerable<TMP_Text> ValueTexts
        {
            get
            {
                if (jobText != null) yield return jobText;
                foreach (var t in letters) if (t != null) yield return t;
                foreach (var t in statLabels) if (t != null) yield return t;
                foreach (var t in statValues) if (t != null) yield return t;
                if (stripText != null) yield return stripText;
            }
        }

        public override IEnumerable<(TMP_Text text, string reason)> SmallTexts
        {
            get
            {
                if (overlay == null) yield break;
                if (overlay.InjuredWord != null) yield return (overlay.InjuredWord, "YARALI sözcüğü: bant deseni, kırmızı CAN değeri ve şerit taşır");
                if (overlay.StampWord != null) yield return (overlay.StampWord, "YATIYOR damgası: şerit 'Yatıyor · n v.' taşır");
                if (overlay.RestrictedWord != null) yield return (overlay.RestrictedWord, "KISITLI köşe sözcüğü: köşe şekli ve şerit taşır");
                if (overlay.LostWord != null) yield return (overlay.LostWord, "KAYIP köşe sözcüğü: yas köşesi ve şerit taşır");
                if (overlay.FlagWord != null) yield return (overlay.FlagWord, "İNCELE bayrağı: bayrak şekli ve üzerine gelme satırı taşır");
            }
        }

        public override IEnumerable<(TMP_Text text, float maxWidthMm)> SingleLineFields
        {
            get
            {
                if (nameText != null) yield return (nameText, nameUsesMagnetSpace ? nameMaxNoMagnetMm : nameMaxMm);
                if (jobText != null) yield return (jobText, jobMaxMm);
                if (stripText != null) yield return (stripText, stripMaxMm);
                var widths = cellsEnglish ? statWidthsEn : statWidthsTr;
                for (int i = 0; i < Stats; i++)
                {
                    if (statLabels[i] != null) yield return (statLabels[i], widths[i]);
                    if (statValues[i] != null) yield return (statValues[i], widths[i]);
                }
                if (overlay != null && overlay.InjuredWord != null) yield return (overlay.InjuredWord, 42.5f);
            }
        }

        public override void Invalidate()
        {
            bound = false;
            cellsPlaced = false;
            if (overlay != null) overlay.Invalidate();
        }

        /// <summary>Kişinin kartını bağlar; değerler önceki bağlamayla aynıysa hiçbir şey yazmaz. Değiştiyse true.</summary>
        public bool Bind(PersonCardData d, BoardWords words, bool english)
        {
            if (d == null) return false;
            words ??= BoardCardView.DefaultWords;
            var states = CardStateOverlay.StatesOf(d);
            if (bound && Matches(d, words, english, states)) return false;
            Store(d, words, english, states);
            CountBind();

            ApplyName(d.Name);
            SetText(jobText, d.Job);

            for (int i = 0; i < Rows; i++)
            {
                int tier = Mathf.Clamp(d.MasteryTiers != null && i < d.MasteryTiers.Length ? d.MasteryTiers[i] : 0, 0, PipsPerRow);
                var letter = i < letters.Length ? letters[i] : null;
                SetText(letter, d.MasteryLetters != null && i < d.MasteryLetters.Length ? d.MasteryLetters[i] : "");
                SetColor(letter, tier >= 1 ? ink : dim);
                for (int j = 0; j < PipsPerRow; j++)
                {
                    int k = i * PipsPerRow + j;
                    if (k >= pips.Length) break;
                    bool filled = j < tier;
                    SetSprite(pips[k], filled ? pipFill : pipRing, filled ? pipFillTint : pipRingTint);
                }
            }

            if (!cellsPlaced || cellsEnglish != english) PlaceCells(english);
            for (int i = 0; i < Stats; i++)
            {
                if (i < statLabels.Length)
                {
                    SetText(statLabels[i], d.StatLabels != null && i < d.StatLabels.Length ? d.StatLabels[i] : "");
                    SetColor(statLabels[i], mute);
                }
                if (i < statValues.Length)
                {
                    SetText(statValues[i], d.StatValues != null && i < d.StatValues.Length ? d.StatValues[i] : "");
                    SetColor(statValues[i], i == 0 && d.Hurt ? red : ink);
                }
            }

            SetText(stripText, d.Strip);
            SetColor(stripText, ToneColor(d.StripTone));

            if (overlay != null) overlay.Apply(states, words);
            bound = true;
            return true;
        }

        public Color ToneColor(StripTone tone)
        {
            switch (tone)
            {
                case StripTone.Blue: return blue;
                case StripTone.Red: return red;
                case StripTone.Mute: return mute;
                default: return ink;
            }
        }

        bool Matches(PersonCardData d, BoardWords w, bool english, PersonStates states) =>
            cachedEnglish == english && cachedName == d.Name && cachedJob == d.Job && cachedStrip == d.Strip && cachedTone == d.StripTone
            && cachedHurt == d.Hurt && cachedStates == states
            && Same(cachedLetters, d.MasteryLetters) && Same(cachedTiers, d.MasteryTiers)
            && Same(cachedLabels, d.StatLabels) && Same(cachedValues, d.StatValues)
            && wInjured == w.Injured && wBed == w.Bedridden && wRestricted == w.Restricted && wReview == w.Review && wLost == w.Lost;

        void Store(PersonCardData d, BoardWords w, bool english, PersonStates states)
        {
            cachedEnglish = english; cachedName = d.Name; cachedJob = d.Job; cachedStrip = d.Strip; cachedTone = d.StripTone;
            cachedHurt = d.Hurt; cachedStates = states;
            Copy(ref cachedLetters, d.MasteryLetters); Copy(ref cachedTiers, d.MasteryTiers);
            Copy(ref cachedLabels, d.StatLabels); Copy(ref cachedValues, d.StatValues);
            wInjured = w.Injured; wBed = w.Bedridden; wRestricted = w.Restricted; wReview = w.Review; wLost = w.Lost;
        }

        /// <summary>
        /// Ad: sığarsa olduğu gibi; sığmayan numaralı adın eki ("Suna 12" → " 12") 9,4 mm; yine sığmazsa mıknatıs alanı kullanılır.
        /// Ad çevrilmez (kişi adı); ek boyutu zengin metin etiketiyle yazılır.
        /// </summary>
        void ApplyName(string name)
        {
            name ??= "";
            nameUsesMagnetSpace = false;
            string shown = name;
            if (nameText != null && PreferredWidth(nameText, name) > nameMaxMm + 0.01f)
            {
                int cut = SuffixStart(name);
                if (cut > 0)
                {
                    suffixOpen ??= string.Concat("<size=", suffixEmMm.ToString("0.###", CultureInfo.InvariantCulture), ">");
                    shown = string.Concat(name.Substring(0, cut), suffixOpen, name.Substring(cut), "</size>");
                }
                if (PreferredWidth(nameText, shown) > nameMaxMm + 0.01f) nameUsesMagnetSpace = true;
            }
            SetText(nameText, shown);
        }

        /// <summary>Adın sondaki sayı ekinin (boşlukla) başladığı yer: "Suna 12" → 4; eki yoksa −1.</summary>
        public static int SuffixStart(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            int i = name.Length;
            while (i > 0 && char.IsDigit(name[i - 1])) i--;
            if (i == name.Length || i < 2 || name[i - 1] != ' ') return -1;
            return i - 1;
        }

        void PlaceCells(bool english)
        {
            cellsEnglish = english;
            cellsPlaced = true;
            var centres = english ? statCentresEn : statCentresTr;
            var widths = english ? statWidthsEn : statWidthsTr;
            var divs = english ? dividersEn : dividersTr;
            for (int i = 0; i < Stats; i++)
            {
                if (i < statLabels.Length)
                {
                    PlaceCell(statLabels[i], centres[i], widths[i]);
                    float spacing = english ? statLabelSpacingEn : 0f;
                    if (statLabels[i] != null && !Mathf.Approximately(statLabels[i].characterSpacing, spacing)) statLabels[i].characterSpacing = spacing;
                }
                if (i < statValues.Length) PlaceCell(statValues[i], centres[i], widths[i]);
            }
            for (int i = 0; i < dividers.Length && i < divs.Length; i++)
            {
                var rt = dividers[i];
                if (rt == null) continue;
                var p = new Vector2(divs[i], rt.anchoredPosition.y);
                if (rt.anchoredPosition != p) rt.anchoredPosition = p;
            }
        }

        static void PlaceCell(TMP_Text t, float centre, float width)
        {
            if (t == null) return;
            var rt = t.rectTransform;
            var p = new Vector2(centre, rt.anchoredPosition.y);
            if (rt.anchoredPosition != p) rt.anchoredPosition = p;
            var s = new Vector2(width, rt.sizeDelta.y);
            if (rt.sizeDelta != s) rt.sizeDelta = s;
        }
    }
}
