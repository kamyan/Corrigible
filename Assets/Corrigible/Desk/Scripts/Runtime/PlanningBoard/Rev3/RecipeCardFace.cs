using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// 112 × 80 mm tarif kartının yüzü (Revizyon 3 · Aşama 5, R18b; PLAN §1.6; <c>boardtex.js</c> <c>recipeCard()</c>). Kırmızı çizgi
    /// (y 18,6), ayraç (y 58) ve çerçeveler taban dokusundadır (<c>tx_card_recipe.png</c>); tuvalde yalnız değişenler var. Yerleşim
    /// (face mm, sol üst köken, y aşağı; punto kuralı D6-A: ad ≥ 12 mm, değerler ≥ 9,4 mm):
    /// <list type="bullet">
    /// <item>Ad: LF 700 12 mm, taban (6, 15), en çok 80 mm (sağdaki simgeye 2 mm kalır); cümle düzeni ("Zırh plakası").</item>
    /// <item>Çıktı simgesi 17 mm, sol üst (88, 4); mıknatıs simgenin üstüne biner (model, tasarımdaki gibi).</item>
    /// <item>"GİRDİ": CP 700 5,4 mm, taban (6, 28), aralık 0,5 mm (gerekçeli küçük yazı: bölüm etiketi).</item>
    /// <item>İki girdi sütunu, adım 50 mm: simge 12 mm (6 + 50i, 31); "×n" CP 700 9,5 mm, taban (20 + 50i, 42); ad LF 400 9,4 mm,
    /// taban (6 + 50i, 54), en çok 48 mm.</item>
    /// <item>[B] İlerleme kutuları yalnız çok vardiyalı tarifte (D7-A; <see cref="RecipeCardData.Done"/> ≥ 0): ayracın altında ayrı
    /// satır, 6,4 × 6,4 mm, sol üst (6 + 8,2i; 60), çizgi 0,45 mm; dolu kutu = biten vardiya (iç 4 × 4 mm). Tasarımda kutular süre
    /// yazısının sağındadır ve "→" okunun üstüne biner (C Ç9); büyüyen puntoyla süre + kutular + çıktı tek satıra sığmadığı için
    /// kutular üst satıra alındı (sapma, rapora).</item>
    /// <item>Süre ve çıktı: CP 700 9,4 mm, taban y 74; süre solda (6), en çok 52 mm; "→ 1 ürün" sağa yaslı (106), en çok 48 mm.</item>
    /// </list>
    /// Metinler anlık görüntüden gelir (çevrilmiş); yüz <c>Loc</c> çağırmaz. Bağlama yalnız değişince yazar
    /// (<see cref="CardFace.ChangedBinds"/>).
    /// </summary>
    public sealed class RecipeCardFace : CardFace
    {
        /// <summary>Girdi sütunu sayısı (her tarifte iki girdi, B §2) ve en çok çizilen ilerleme kutusu.</summary>
        public const int Columns = 2, MaxBoxes = 2;

        [SerializeField] TMP_Text nameText, needsLabel, turnsText, outText;
        [Tooltip("Çıktının simgesi (17 mm, sağ üstte).")]
        [SerializeField] Image icon;
        [Tooltip("İki girdi sütunu: simge, \"×n\", ad.")]
        [SerializeField] Image[] inputIcons = new Image[Columns];
        [SerializeField] TMP_Text[] inputQty = new TMP_Text[Columns];
        [SerializeField] TMP_Text[] inputNames = new TMP_Text[Columns];
        [Tooltip("[B] İlerleme kutularının çerçevesi (yalnız çok vardiyalı tarifte görünür).")]
        [SerializeField] Image[] boxes = new Image[MaxBoxes];
        [Tooltip("[B] Kutunun dolgusu (biten vardiya); kutunun çocuğu.")]
        [SerializeField] Image[] boxFills = new Image[MaxBoxes];
        [Tooltip("Çıktı simgeleri, BoardLayout.KitKinds sırasıyla: kit_tek, kit_emp, kit_ana, armor, medkit, emp_cell.")]
        [SerializeField] Sprite[] icons = new Sprite[6];
        [Tooltip("Girdi simgeleri, BoardLayout.MaterialKinds sırasıyla: metal, devre, kumas, kimyasal.")]
        [SerializeField] Sprite[] materialIcons = new Sprite[4];
        [SerializeField] Color iconTint = Color.white;
        [SerializeField] float nameMaxMm = 80f, needsMaxMm = 40f, qtyMaxMm = 28f, inputNameMaxMm = 48f, turnsMaxMm = 52f, outMaxMm = 48f;

        bool bound;
        string cName, cIcon, cTurns, cOut, cNeeds;
        int cTurnsN, cDone;
        readonly string[] cKinds = new string[Columns], cQty = new string[Columns], cInputNames = new string[Columns];
        int cInputs;

        public override BoardCardKind Kind => BoardCardKind.Recipe;
        public override TMP_Text NameText => nameText;
        public TMP_Text NeedsLabel => needsLabel;
        public TMP_Text TurnsText => turnsText;
        public TMP_Text OutText => outText;
        public Image Icon => icon;
        public IReadOnlyList<Image> InputIcons => inputIcons;
        public IReadOnlyList<TMP_Text> InputQty => inputQty;
        public IReadOnlyList<TMP_Text> InputNames => inputNames;
        public IReadOnlyList<Image> Boxes => boxes;
        public IReadOnlyList<Image> BoxFills => boxFills;
        public IReadOnlyList<Sprite> Icons => icons;
        public IReadOnlyList<Sprite> MaterialIcons => materialIcons;

        /// <summary>Görünen ilerleme kutusu sayısı (yalnız çok vardiyalı, başlamış ya da başlamamış emir; tek vardiyalıkta 0).</summary>
        public int VisibleBoxes
        {
            get
            {
                int n = 0;
                if (boxes != null) foreach (var b in boxes) if (b != null && b.gameObject.activeSelf) n++;
                return n;
            }
        }

        /// <summary>Dolu kutu sayısı (biten vardiyalar).</summary>
        public int FilledBoxes
        {
            get
            {
                int n = 0;
                if (boxes == null || boxFills == null) return 0;
                for (int i = 0; i < boxes.Length && i < boxFills.Length; i++)
                    if (boxes[i] != null && boxes[i].gameObject.activeSelf && boxFills[i] != null && boxFills[i].gameObject.activeSelf) n++;
                return n;
            }
        }

        public override IEnumerable<TMP_Text> ValueTexts
        {
            get
            {
                if (turnsText != null) yield return turnsText;
                if (outText != null) yield return outText;
                if (inputQty != null) foreach (var t in inputQty) if (t != null) yield return t;
                if (inputNames != null) foreach (var t in inputNames) if (t != null) yield return t;
            }
        }

        public override IEnumerable<(TMP_Text text, string reason)> SmallTexts
        {
            get
            {
                if (needsLabel != null) yield return (needsLabel, "bölüm etiketi: girdi sütunları (simge, ×n, ad) taşır");
            }
        }

        public override IEnumerable<(TMP_Text text, float maxWidthMm)> SingleLineFields
        {
            get
            {
                if (nameText != null) yield return (nameText, nameMaxMm);
                if (needsLabel != null) yield return (needsLabel, needsMaxMm);
                if (inputQty != null) foreach (var t in inputQty) if (t != null) yield return (t, qtyMaxMm);
                if (inputNames != null) foreach (var t in inputNames) if (t != null) yield return (t, inputNameMaxMm);
                if (turnsText != null) yield return (turnsText, turnsMaxMm);
                if (outText != null) yield return (outText, outMaxMm);
            }
        }

        public override void Invalidate() => bound = false;

        /// <summary>Kartı bağlar; aynıysa hiçbir şey yazmaz (<see cref="CardFace.ChangedBinds"/> kalıbı). Değiştiyse true.</summary>
        public bool Bind(RecipeCardData d)
        {
            if (d == null) return false;
            string iconKey = string.IsNullOrEmpty(d.IconKey) ? d.RecipeId : d.IconKey;
            int inputs = Mathf.Min(Columns, d.Inputs.Count);
            if (bound && Same(d, iconKey, inputs)) return false;
            bound = true;
            cName = d.Name; cIcon = iconKey; cTurns = d.TurnsLine; cOut = d.OutLine; cNeeds = d.NeedsLabel;
            cTurnsN = d.Turns; cDone = d.Done; cInputs = inputs;
            CountBind();

            SetText(nameText, d.Name);
            SetText(needsLabel, d.NeedsLabel);
            SetText(turnsText, d.TurnsLine);
            SetText(outText, d.OutLine);
            SetSprite(icon, Pick(icons, System.Array.IndexOf(BoardLayout.KitKinds, iconKey)), iconTint);

            for (int i = 0; i < Columns; i++)
            {
                var input = i < inputs ? d.Inputs[i] : null;
                cKinds[i] = input?.Kind; cQty[i] = input?.CardQty; cInputNames[i] = input?.Name;
                var ic = At(inputIcons, i);
                if (input != null) SetSprite(ic, Pick(materialIcons, System.Array.IndexOf(BoardLayout.MaterialKinds, input.Kind)), iconTint);
                SetActive(ic, input != null);
                var q = At(inputQty, i);
                SetText(q, input?.CardQty);
                SetActive(q, input != null);
                var n = At(inputNames, i);
                SetText(n, input?.Name);
                SetActive(n, input != null);
            }

            // [B] Çok vardiyalı tarifte vardiya kadar kutu (en çok MaxBoxes), dolu = biten vardiya. Tek vardiyalıkta ve Done < 0'da kutu yok.
            int shown = d.Done >= 0 && d.Turns > 1 ? Mathf.Min(d.Turns, boxes != null ? boxes.Length : 0) : 0;
            int filled = Mathf.Clamp(d.Done, 0, shown);
            if (boxes != null)
                for (int i = 0; i < boxes.Length; i++)
                {
                    SetActive(boxes[i], i < shown);
                    SetActive(At(boxFills, i), i < filled);
                }
            return true;
        }

        bool Same(RecipeCardData d, string iconKey, int inputs)
        {
            if (cName != d.Name || cIcon != iconKey || cTurns != d.TurnsLine || cOut != d.OutLine || cNeeds != d.NeedsLabel) return false;
            if (cTurnsN != d.Turns || cDone != d.Done || cInputs != inputs) return false;
            for (int i = 0; i < inputs; i++)
            {
                var x = d.Inputs[i];
                if (x == null || cKinds[i] != x.Kind || cQty[i] != x.CardQty || cInputNames[i] != x.Name) return false;
            }
            return true;
        }

        static T At<T>(T[] list, int i) where T : class => list != null && i >= 0 && i < list.Length ? list[i] : null;

        static Sprite Pick(Sprite[] list, int i) => list != null && i >= 0 && i < list.Length ? list[i] : null;
    }
}
