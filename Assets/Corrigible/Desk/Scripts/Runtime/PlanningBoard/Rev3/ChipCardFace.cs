using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bir birimlik hammadde fişinin yüzü, 56 × 40 mm (PLAN §3.4; <c>boardtex.js</c> <c>materialChip</c>): üstteki kırmızı "ayrılmış"
    /// bandı tabanda; simge 14 mm (4, 9), "1" CP 700 15 mm sağa yaslı (51, 24), ad CP 700 9,4 mm ortalı (28; 35,4), en çok 50 mm.
    /// Mıknatısı yoktur.
    /// </summary>
    public sealed class ChipCardFace : CardFace
    {
        [SerializeField] TMP_Text oneText, nameText;
        [SerializeField] Image icon;
        [Tooltip("BoardLayout.MaterialKinds sırasıyla: metal, devre, kumas, kimyasal.")]
        [SerializeField] Sprite[] icons = new Sprite[4];
        [SerializeField] Color iconTint = Color.white;
        [SerializeField] float nameMaxMm = 50f, oneMaxMm = 12f;

        bool bound;
        string cachedKind, cachedName;

        public override BoardCardKind Kind => BoardCardKind.Chip;
        public override TMP_Text NameText => null;
        public TMP_Text ChipName => nameText;
        public TMP_Text OneText => oneText;

        public override IEnumerable<TMP_Text> ValueTexts
        {
            get
            {
                if (nameText != null) yield return nameText;
                if (oneText != null) yield return oneText;
            }
        }

        public override IEnumerable<(TMP_Text text, string reason)> SmallTexts { get { yield break; } }

        public override IEnumerable<(TMP_Text text, float maxWidthMm)> SingleLineFields
        {
            get
            {
                if (nameText != null) yield return (nameText, nameMaxMm);
                if (oneText != null) yield return (oneText, oneMaxMm);
            }
        }

        public override void Invalidate() => bound = false;

        /// <summary>Fişi bağlar; aynıysa hiçbir şey yazmaz. Değiştiyse true.</summary>
        public bool Bind(ChipData d)
        {
            if (d == null) return false;
            if (bound && cachedKind == d.Kind && cachedName == d.Name) return false;
            bound = true;
            cachedKind = d.Kind; cachedName = d.Name;
            CountBind();
            int ki = System.Array.IndexOf(BoardLayout.MaterialKinds, d.Kind);
            SetSprite(icon, ki >= 0 && ki < icons.Length ? icons[ki] : null, iconTint);
            SetText(nameText, d.Name);
            return true;
        }
    }
}
