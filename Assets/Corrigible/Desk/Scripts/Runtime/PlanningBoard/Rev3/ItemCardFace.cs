using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Eşya kartının yüzü, 80 × 50 mm (PLAN §3.3; <c>boardtex.js</c> <c>itemCard</c>). Kimlik bandı alt 14 mm'dir (y 34,6–48,6):
    /// kişinin arkasına sıkışınca yalnız o görünür. Yuva deseni (kit düz, zırh taralı, yardımcı noktalı) üç ayrı ışık alan taban
    /// malzemesindedir; kart onu <see cref="BoardCardView"/> ile değiştirir.
    /// <list type="bullet">
    /// <item>Yuva etiketi: CP 700 5,4 mm, aralık 0,4 mm, (5, 9); kullanım satırı: LF 400 5,4 mm, (5; 17,6), 46 mm'de sarar, satır
    /// 6,4 mm, en çok 3 satır (gerekçeli küçük yazılar).</item>
    /// <item>Büyük piktogram 23 × 23 mm (53; 3,4); bantta aynı piktogram açık renkle 9,6 mm (2,5; 36,7).</item>
    /// <item>Ad: LF 600 9,4 mm, (14,6; 44,8), en çok 63,5 mm (D2).</item>
    /// </list>
    /// </summary>
    public sealed class ItemCardFace : CardFace
    {
        [SerializeField] TMP_Text slotLabel, useLine, nameText;
        [SerializeField] Image pictogram, bandIcon;
        [Tooltip("Kit yığınlarının sırasıyla (BoardLayout.KitKinds): kit_tek, kit_emp, kit_ana, armor, medkit, emp_cell.")]
        [SerializeField] Sprite[] pictograms = new Sprite[6];
        [Tooltip("Piktogram ve bant simgesinin boyası (resim beyazsa tasarım rengi, renkliyse beyaz).")]
        [SerializeField] Color pictogramTint = Color.white, bandIconTint = Color.white;
        [SerializeField] float nameMaxMm = 63.5f, slotLabelMaxMm = 46f;
        [SerializeField] int maxUseLines = 3;

        bool bound;
        string cachedKind, cachedName, cachedSlotLabel, cachedUse;

        public override BoardCardKind Kind => BoardCardKind.Item;
        public override TMP_Text NameText => null;
        public TMP_Text ItemName => nameText;
        public TMP_Text SlotLabel => slotLabel;
        public TMP_Text UseLine => useLine;
        public Image Pictogram => pictogram;
        public Image BandIcon => bandIcon;
        public int MaxUseLines => maxUseLines;

        public override IEnumerable<TMP_Text> ValueTexts
        {
            get { if (nameText != null) yield return nameText; }
        }

        public override IEnumerable<(TMP_Text text, string reason)> SmallTexts
        {
            get
            {
                if (slotLabel != null) yield return (slotLabel, "yuva etiketi: kimlik bandının deseni ve eşyanın adı taşır");
                if (useLine != null) yield return (useLine, "kullanım satırı: eşyanın üzerine gelme satırı ve kişi dosyası taşır");
            }
        }

        public override IEnumerable<(TMP_Text text, float maxWidthMm)> SingleLineFields
        {
            get
            {
                if (nameText != null) yield return (nameText, nameMaxMm);
                if (slotLabel != null) yield return (slotLabel, slotLabelMaxMm);
            }
        }

        public override void Invalidate() => bound = false;

        /// <summary>Eşya kartını bağlar; aynıysa hiçbir şey yazmaz. Değiştiyse true.</summary>
        public bool Bind(ItemCardData d)
        {
            if (d == null) return false;
            if (bound && cachedKind == d.Kind && cachedName == d.Name && cachedSlotLabel == d.SlotLabel && cachedUse == d.UseLine) return false;
            bound = true;
            cachedKind = d.Kind; cachedName = d.Name; cachedSlotLabel = d.SlotLabel; cachedUse = d.UseLine;
            CountBind();

            SetText(slotLabel, d.SlotLabel);
            SetText(useLine, d.UseLine);
            SetText(nameText, d.Name);
            var sprite = PictogramFor(d.Kind);
            SetSprite(pictogram, sprite, pictogramTint);
            SetSprite(bandIcon, sprite, bandIconTint);
            return true;
        }

        Sprite PictogramFor(string kind)
        {
            int i = System.Array.IndexOf(BoardLayout.KitKinds, kind);
            return i >= 0 && i < pictograms.Length ? pictograms[i] : null;
        }
    }
}
