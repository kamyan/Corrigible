using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Kişi kartının durumları (R16 · <c>c_states</c>); birkaçı aynı anda olabilir.</summary>
    [Flags]
    public enum PersonStates
    {
        None = 0,
        Injured = 1,
        Bedridden = 2,
        Restricted = 4,
        Review = 8,
        Lost = 16,
        Selected = 32,
    }

    /// <summary>
    /// Kişi kartının durum katmanları (PLAN §3.2 "Overlays"; <c>boardtex.js</c> <c>personCard</c>). Hiçbiri durumu yalnız renkle
    /// taşımaz; her birinin şekli ve sözcüğü vardır, şerit de yazar:
    /// <list type="bullet">
    /// <item>YARALI: portrenin sol üstünde yarı saydam bant (30 × 8,4 mm, (12, 36), −35,5°) + sağa yaslı sözcük (47,5; 75,0).
    /// Modelin 3B <c>Tape</c> parçası yüz tuvaliyle aynı düzlemde kaldığı için gizli kalır (çizilen bant kullanılır).</item>
    /// <item>YATIYOR: portre ortasında (27; 53,1) −14,9° mavi çift çerçeveli damga; genişlik = sözcük + 5 mm, yükseklik 1,32 em.</item>
    /// <item>KISITLI: sağ üstte kırmızı üçgen (54 → 78,4; 1,6 → 26) ve köşegen sözcük; KAYIP: siyah yas köşesi (50 → 78,4; 1,6 → 30),
    /// köşegen sözcük ve portre kararır (<c>_BaseColor</c> 0,601 / 0,588 / 0,566, MaterialPropertyBlock).</item>
    /// <item>İNCELE: modelin 3B <c>Flag</c> parçası (üst kenardan 8 mm taşan kırmızı bayrak) ve üstündeki 3B sözcük.</item>
    /// <item>Seçili: 1,2 mm sarı çerçeve, 0,6 mm içeride.</item>
    /// </list>
    /// Köşe ve bayrak sözcükleri süstür (şerit ve üzerine gelme satırı taşır); tasarımdaki gibi alanlarına sığdırılır.
    /// </summary>
    public sealed class CardStateOverlay : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static MaterialPropertyBlock block;

        [Header("YARALI")]
        [SerializeField] Image tape;
        [SerializeField] TMP_Text injuredWord;
        [Header("YATIYOR")]
        [SerializeField] RectTransform stamp;
        [SerializeField] RectTransform stampFrame;
        [SerializeField] TMP_Text stampWord;
        [Tooltip("Damga sözcüğünün puntosu (mm) ve çerçevenin sözcükten geniş olduğu pay (mm).")]
        [SerializeField] float stampEmMm = 8.6f, stampPadMm = 5f;
        [Tooltip("Dış çizginin kalınlığı (mm): çerçeve resmi çizgiyi dışa doğru taşır.")]
        [SerializeField] float stampStrokeMm = 0.9f;
        [Header("KISITLI / KAYIP köşeleri")]
        [SerializeField] Image restrictedCorner;
        [SerializeField] TMP_Text restrictedWord;
        [SerializeField] Image lostCorner;
        [SerializeField] TMP_Text lostWord;
        [SerializeField] float cornerWordEmMm = 4.6f, restrictedWordMaxMm = 20f, lostWordMaxMm = 22f;
        [Header("İNCELE (3B bayrak)")]
        [SerializeField] PartDriver flag;
        [SerializeField] TMP_Text flagWord;
        [Tooltip("3B bayrak sözcüğünün puntosu (TextMeshPro fontSize = em mm / 100) ve en geniş yazısı (m).")]
        [SerializeField] float flagWordSize = 0.056f, flagWordMaxM = 0.0164f;
        [Tooltip("İNCELE bayrağı modeldeki yerinden bu kadar yukarıda gösterilir (mm): ataşı adın üstünde kalır (R16a). Model parçasını " +
                 "bu bileşen sürer; iç içe modelde geçersiz kılma yok.")]
        [SerializeField] float flagLiftMm = 6f;
        [Header("KAYIP portresi ve seçim")]
        [SerializeField] Renderer portrait;
        [SerializeField] Color lostPortraitTint = new Color(0.601f, 0.588f, 0.566f, 1f);
        [SerializeField] Image selectedFrame;

        PersonStates applied = (PersonStates)(-1);
        Vector3 flagRest;
        bool flagRestKnown;
        string injured, bed, restricted, review, lost;

        public PersonStates Applied => applied;
        public TMP_Text InjuredWord => injuredWord;
        public TMP_Text StampWord => stampWord;
        public TMP_Text RestrictedWord => restrictedWord;
        public TMP_Text LostWord => lostWord;
        public TMP_Text FlagWord => flagWord;
        public Renderer Portrait => portrait;
        public PartDriver Flag => flag;

        /// <summary>Kişi kartının verisinden durum bayrakları (veri zaten canlılığa göre süzülmüştür).</summary>
        public static PersonStates StatesOf(PersonCardData d)
        {
            if (d == null) return PersonStates.None;
            var s = PersonStates.None;
            if (d.Injured) s |= PersonStates.Injured;
            if (d.Bedridden) s |= PersonStates.Bedridden;
            if (d.Restricted) s |= PersonStates.Restricted;
            if (d.Review) s |= PersonStates.Review;
            if (d.Lost) s |= PersonStates.Lost;
            if (d.Selected) s |= PersonStates.Selected;
            return s;
        }

        /// <summary>Durumları uygular; durumlar ve sözcükler aynıysa hiçbir şey yazmaz.</summary>
        public void Apply(PersonStates s, BoardWords w)
        {
            w ??= BoardCardView.DefaultWords;
            if (s == applied && injured == w.Injured && bed == w.Bedridden && restricted == w.Restricted && review == w.Review && lost == w.Lost) return;
            applied = s;
            injured = w.Injured; bed = w.Bedridden; restricted = w.Restricted; review = w.Review; lost = w.Lost;

            bool isLost = (s & PersonStates.Lost) != 0;

            // YARALI: kayıp kartta bant yok (tasarım: hurt && !lost)
            bool hurt = (s & PersonStates.Injured) != 0 && !isLost;
            Show(tape, hurt);
            Show(injuredWord, hurt);
            if (hurt && injuredWord != null) injuredWord.text = injured ?? "";

            // YATIYOR damgası: çerçeve sözcüğe göre genişler
            bool bedridden = (s & PersonStates.Bedridden) != 0;
            Show(stamp, bedridden);
            if (bedridden && stampWord != null)
            {
                stampWord.text = bed ?? "";
                if (stampFrame != null)
                {
                    float wordWidth = string.IsNullOrEmpty(bed) ? 0 : stampWord.GetPreferredValues(bed, float.PositiveInfinity, float.PositiveInfinity).x;
                    var size = new Vector2(wordWidth + stampPadMm + stampStrokeMm, stampEmMm * 1.32f + stampStrokeMm);
                    if (stampFrame.sizeDelta != size) stampFrame.sizeDelta = size;
                }
            }

            bool isRestricted = (s & PersonStates.Restricted) != 0;
            Show(restrictedCorner, isRestricted);
            Show(restrictedWord, isRestricted);
            if (isRestricted) Fit(restrictedWord, restricted, cornerWordEmMm, restrictedWordMaxMm);

            Show(lostCorner, isLost);
            Show(lostWord, isLost);
            if (isLost) Fit(lostWord, lost, cornerWordEmMm, lostWordMaxMm);
            TintPortrait(isLost);

            bool flagged = (s & PersonStates.Review) != 0;
            if (flagged) PlaceFlag();
            if (flag != null) flag.SetVisible(flagged);
            Show(flagWord, flagged);
            if (flagged) Fit(flagWord, review, flagWordSize, flagWordMaxM);

            Show(selectedFrame, (s & PersonStates.Selected) != 0);
        }

        /// <summary>Bayrak parçasını model pozundan <see cref="flagLiftMm"/> yukarıya koyar (yalnız değişince yazar).</summary>
        void PlaceFlag()
        {
            if (flag == null) return;
            var t = flag.transform;
            if (!flagRestKnown) { flagRest = t.localPosition; flagRestKnown = true; }
            var p = flagRest + Vector3.up * (flagLiftMm / 1000f);
            if (t.localPosition != p) t.localPosition = p;
        }

        /// <summary>Önbelleği boşaltır (sonraki <see cref="Apply"/> her şeyi yeniden yazar).</summary>
        public void Invalidate() => applied = (PersonStates)(-1);

        void TintPortrait(bool on)
        {
            if (portrait == null) return;
            if (!on) { portrait.SetPropertyBlock(null); return; }
            block ??= new MaterialPropertyBlock();
            block.Clear();
            block.SetColor(BaseColorId, lostPortraitTint);
            portrait.SetPropertyBlock(block);
        }

        static void Show(Component c, bool on)
        {
            if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
        }

        /// <summary>Süs sözcüğünü alanına sığdırır (tasarımın <c>fit</c>'i, en çok %70'e kadar).</summary>
        static void Fit(TMP_Text t, string value, float designSize, float maxWidth)
        {
            if (t == null) return;
            value ??= "";
            if (!Mathf.Approximately(t.fontSize, designSize)) t.fontSize = designSize;
            t.text = value;
            if (value.Length == 0) return;
            // GetPreferredValues genişliği 0,01 birime yukarı yuvarlar; metre ölçekli 3B bayrak sözcüğünde bu 10 mm demek.
            // Ölçü bu yüzden çizilen yazının sınırından.
            t.ForceMeshUpdate(true, true);
            float width = t.textBounds.size.x;
            if (width > maxWidth && width > 0) t.fontSize = Mathf.Max(designSize * 0.7f, designSize * maxWidth / width);
        }
    }
}
