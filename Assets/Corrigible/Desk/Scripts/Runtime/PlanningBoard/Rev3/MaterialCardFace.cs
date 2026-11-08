using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Hammadde stok kartının yüzü, 80 × 56 mm (PLAN §3.4; <c>boardtex.js</c> <c>materialCard</c>):
    /// <list type="bullet">
    /// <item>Simge 10 × 10 mm (4; 3,6); ad CP 700 9,4 mm (16,4; 11,2), en çok 49,6 mm (mıknatısa kadar).</item>
    /// <item>Serbest sayısı CP 700 22 mm, sağa yaslı (75; 27,6), sıfırsa soluk; altında "serbest" 5 mm (75; 33,2).</item>
    /// <item>Pipler: stoğun ilk 20'si, 2 satır × 10, merkez (6,6 + 4,5·sütun; 20 + 5,6·satır), Ø 3,4. Pip serbestse dolu; değilse
    /// ayrılmış (kırmızı halka + çizgi, şekliyle ayrılır). Stok 20'yi aşınca ayrılmışlar önce çizilir ve "+n" (51,6; 31,6) yazılır.</item>
    /// <item>"stok n" sola (5; 47,4), "ayrı n" sağa (75; 47,4) CP 700 7,4 mm (D1 gerekçeli istisna); ayrılan varsa kırmızı.</item>
    /// </list>
    /// </summary>
    public sealed class MaterialCardFace : CardFace
    {
        public const int MaxPips = 20;

        [SerializeField] TMP_Text nameText, freeText, freeWord, moreText, stockText, heldText;
        [SerializeField] Image icon;
        [SerializeField] Image[] pips = new Image[MaxPips];
        [Tooltip("BoardLayout.MaterialKinds sırasıyla: metal, devre, kumas, kimyasal.")]
        [SerializeField] Sprite[] icons = new Sprite[4];
        [SerializeField] Sprite pipFill, pipReserved;
        [Tooltip("Resim beyazsa tasarım rengi, renkliyse beyaz.")]
        [SerializeField] Color iconTint = Color.white, pipFillTint = Color.white, pipReservedTint = Color.white;
        [SerializeField] Color ink, dim, mute, red;
        [SerializeField] float nameMaxMm = 49.6f, freeMaxMm = 30f, stockMaxMm = 36.5f, heldMaxMm = 33.5f;

        bool bound;
        string cachedKind, cachedName, cachedStock, cachedHeld, cachedFreeWord;
        int cachedStockN, cachedFree, cachedHeldN;

        public override BoardCardKind Kind => BoardCardKind.Material;
        public override TMP_Text NameText => null;
        public TMP_Text MaterialName => nameText;
        public TMP_Text FreeText => freeText;
        public TMP_Text FreeWord => freeWord;
        public TMP_Text MoreText => moreText;
        public TMP_Text StockText => stockText;
        public TMP_Text HeldText => heldText;
        public IReadOnlyList<Image> Pips => pips;

        public override IEnumerable<TMP_Text> ValueTexts
        {
            get
            {
                if (nameText != null) yield return nameText;
                if (freeText != null) yield return freeText;
            }
        }

        public override IEnumerable<(TMP_Text text, string reason)> SmallTexts
        {
            get
            {
                if (freeWord != null) yield return (freeWord, "'serbest' sözcüğü: büyük serbest sayısı taşır");
                if (moreText != null) yield return (moreText, "'+n' fazlası: 'stok n' satırı ve üzerine gelme satırı taşır");
                if (stockText != null) yield return (stockText, "'stok n' (D1 istisnası): pipler ve üzerine gelme satırı taşır");
                if (heldText != null) yield return (heldText, "'ayrı n' (D1 istisnası): kırmızı ayrılmış pipler ve üzerine gelme satırı taşır");
            }
        }

        public override IEnumerable<(TMP_Text text, float maxWidthMm)> SingleLineFields
        {
            get
            {
                if (nameText != null) yield return (nameText, nameMaxMm);
                if (freeText != null) yield return (freeText, freeMaxMm);
                if (stockText != null) yield return (stockText, stockMaxMm);
                if (heldText != null) yield return (heldText, heldMaxMm);
            }
        }

        public override void Invalidate() => bound = false;

        /// <summary>Stok kartını bağlar (<paramref name="freeWordText"/> = "serbest"/"free"); aynıysa hiçbir şey yazmaz.</summary>
        public bool Bind(MaterialCardData d, string freeWordText)
        {
            if (d == null) return false;
            if (bound && cachedKind == d.Kind && cachedName == d.Name && cachedStockN == d.Stock && cachedFree == d.Free && cachedHeldN == d.Held
                && cachedStock == d.StockLine && cachedHeld == d.HeldLine && cachedFreeWord == freeWordText) return false;
            bound = true;
            cachedKind = d.Kind; cachedName = d.Name; cachedStockN = d.Stock; cachedFree = d.Free; cachedHeldN = d.Held;
            cachedStock = d.StockLine; cachedHeld = d.HeldLine; cachedFreeWord = freeWordText;
            CountBind();

            int ki = System.Array.IndexOf(BoardLayout.MaterialKinds, d.Kind);
            SetSprite(icon, ki >= 0 && ki < icons.Length ? icons[ki] : null, iconTint);
            SetText(nameText, d.Name);

            int free = Mathf.Max(0, d.Free), stock = Mathf.Max(0, d.Stock);
            SetText(freeText, free.ToString(CultureInfo.InvariantCulture));
            SetColor(freeText, free > 0 ? ink : dim);
            SetText(freeWord, freeWordText);
            SetColor(freeWord, mute);

            // Pipler: stok ≤ 20 → i < serbest dolu; stok > 20 → ayrılmışlar önce
            int shown = Mathf.Min(stock, MaxPips);
            int reservedShown = stock > MaxPips ? Mathf.Min(Mathf.Max(0, stock - free), MaxPips) : 0;
            for (int i = 0; i < pips.Length; i++)
            {
                var pip = pips[i];
                if (pip == null) continue;
                bool on = i < shown;
                SetActive(pip, on);
                if (!on) continue;
                bool reserved = stock > MaxPips ? i < reservedShown : i >= free;
                SetSprite(pip, reserved ? pipReserved : pipFill, reserved ? pipReservedTint : pipFillTint);
            }

            bool more = stock > MaxPips;
            SetActive(moreText, more);
            if (more) SetText(moreText, (stock - MaxPips).ToString("+0;-0", CultureInfo.InvariantCulture));
            SetColor(moreText, mute);

            SetText(stockText, d.StockLine);
            SetColor(stockText, ink);
            SetText(heldText, d.HeldLine);
            SetColor(heldText, d.Held > 0 ? red : mute);
            return true;
        }
    }
}
