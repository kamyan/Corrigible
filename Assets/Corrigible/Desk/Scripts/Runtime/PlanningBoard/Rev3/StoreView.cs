using System;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Panonun DEPO bandı (R15a, R16b): dört hammadde stok kartı <c>Mat_*</c> çapalarında (sırayla metal, devre, kumaş,
    /// kimyasal; havuzdan çıkmaz), altı kit yığını <c>Kit_0..5</c>'te (kit_tek, kit_emp, kit_ana, armor, medkit, emp_cell).
    /// Yığında en çok üç kart çizilir: en yenisi üstte, çapada ve en önde (seviye 2), alttakiler 0,9 mm sağ-aşağıda ve
    /// arkada; "×n" etiketi (TXT_Count_i), boş yığında "×0" soluk. Reddedilen hazırlamada stok kartının serbest sayısı 0,2 s
    /// kırmızı yanar. Aşama 5 (R18d): yeni ürün TARİF kutusundan yığına 0,3 s'de kayar (<see cref="SlideIn"/>) ve üstünde kırmızı
    /// YENİ levhası durur (<see cref="ShowNew"/>). Kartları <see cref="BoardView3D"/> verir; sürükleme denetleyicisinin tuttuğu kart için null gelir ve
    /// dokunulmaz.
    /// </summary>
    public sealed class StoreView : MonoBehaviour
    {
        [Tooltip("TXT_Count_0..5 (kit yığınlarının ×n etiketi; dinamik, DeskLocText yok).")]
        [SerializeField] TMP_Text[] counts = new TMP_Text[6];
        [Tooltip("Aşama 5: yeni ürünün kırmızı YENİ levhası (sahnede; başta pasif).")]
        [SerializeField] NewItemTag newTag;

        readonly BoardCardView[] lastMaterials = new BoardCardView[4];
        readonly bool[] materialPlaced = new bool[4];
        readonly TMP_Text[] flashText = new TMP_Text[4];
        readonly BoardCardView[] flashOwner = new BoardCardView[4];
        readonly Color[] flashBase = new Color[4];
        readonly Color[] flashColour = new Color[4];
        readonly float[] flashUntil = new float[4];
        readonly bool[] flashing = new bool[4];
        Color[] countBase;
        int flashCount;

        void Awake() => CaptureCountColours();

        // ------------------------------------------------------------------ Aşama 5 (R18d; PLAN §1.11)

        /// <summary>Depo sesleri (ürün kayışının "şık"ı); BoardView3D toplar.</summary>
        public event Action<BoardSound> Sound;

        /// <summary>Ürün kayışı bitti (kart yığının üst kart yerinde); BoardView3D <c>NewItemSlid</c>'i yayar.</summary>
        public event Action<BoardCardView> SlideFinished;

        /// <summary>YENİ levhası (sahnede bağlıysa).</summary>
        public NewItemTag NewTag => newTag;

        /// <summary>Kayan kart (yoksa null).</summary>
        public BoardCardView Sliding => slideCard;

        BoardSnapshot lastLayout;
        BoardCardView slideCard;
        Vector3 slideFrom, slideTo;
        float slideElapsed, slideSeconds;

        /// <summary>
        /// Yeni ürün kartı TARİF kutusundan (<paramref name="fromLocal"/>, model yereli) yığının üst kart yerine (son
        /// <see cref="Layout"/>'taki yığını; bulunamazsa olduğu yerde kalır) <paramref name="seconds"/> saniyede ease-out kayar ve
        /// <see cref="BoardSound.ProductSlide"/> yayar ("şık"). Kayan karta <see cref="Layout"/> dokunmaz (BoardView3D onu vermez).
        /// </summary>
        public void SlideIn(BoardCardView card, Vector3 fromLocal, float seconds)
        {
            if (card == null) return;
            SlideIn(card, fromLocal, TopSlotOf(card.Id, out var to) ? to : fromLocal, seconds);
        }

        /// <summary>Kayış, hedefi açıkça verilerek (test ve yığını önceden bilen çağıran için).</summary>
        public void SlideIn(BoardCardView card, Vector3 fromLocal, Vector3 toLocal, float seconds)
        {
            if (card == null) return;
            if (slideCard != null && slideCard != card) FinishSlide();
            slideCard = card;
            slideFrom = fromLocal;
            slideTo = toLocal;
            slideElapsed = 0;
            slideSeconds = Mathf.Max(0, seconds);
            card.SetMagnetVisible(true);
            card.Place(fromLocal, 0);
            card.SetVisible(true);
            Sound?.Invoke(BoardSound.ProductSlide);
            if (slideSeconds <= 0) FinishSlide();
        }

        /// <summary>Süren kayışı keser (kart hedefinde biter, <see cref="SlideFinished"/> yayılmaz).</summary>
        public void CancelSlide()
        {
            if (slideCard == null) return;
            slideCard.Place(slideTo, 0);
            slideCard = null;
        }

        /// <summary>Kartın yığındaki yeri (son düzenin yığını; en yeni = üst kart, seviye 2). Yoksa false.</summary>
        public bool TopSlotOf(string itemId, out Vector3 local)
        {
            local = default;
            var s = lastLayout;
            if (s == null || string.IsNullOrEmpty(itemId)) return false;
            for (int k = 0; k < s.Kits.Length; k++)
            {
                var kit = s.Kits[k];
                if (kit == null) continue;
                int m = kit.TopIds.Count;
                for (int d = 0; d < m; d++)
                {
                    if (kit.TopIds[d] != itemId) continue;
                    int j = m - 1 - d;
                    local = BoardLayout.MmToLocal(BoardLayout.KitCard(k, j), BoardLayout.CardZ(BoardLayout.KitLevel(j)));
                    return true;
                }
            }
            return false;
        }

        /// <summary>Kayışı ilerletir (Edit modda testler, Play'de <c>Update</c>).</summary>
        public void Advance(float dt)
        {
            if (slideCard == null) return;
            slideElapsed += Mathf.Max(0, dt);
            float t = slideSeconds > 0 ? Mathf.Clamp01(slideElapsed / slideSeconds) : 1f;
            slideCard.Place(Vector3.LerpUnclamped(slideFrom, slideTo, CardEasing.Evaluate(CardEase.EaseOut, t)), 0);
            if (t >= 1f) FinishSlide();
        }

        void FinishSlide()
        {
            var c = slideCard;
            if (c == null) return;
            slideCard = null;
            c.Place(slideTo, 0);
            SlideFinished?.Invoke(c);
        }

        /// <summary>YENİ levhasını karta iliştirir ya da kaldırır (<paramref name="animate"/>: 0,15 s söner).</summary>
        public void ShowNew(BoardCardView card, bool on, bool animate)
        {
            if (newTag == null) return;
            newTag.Show(card != null ? card.transform : null, on && card != null, animate);
        }

        void CaptureCountColours()
        {
            if (countBase != null || counts == null) return;
            countBase = new Color[counts.Length];
            for (int i = 0; i < counts.Length; i++) countBase[i] = counts[i] != null ? counts[i].color : Color.black;
        }

        /// <summary>
        /// Depoyu dizer. <paramref name="itemCard"/> kimlikten eşya kartını verir (yoksa ya da tutuluyorsa null);
        /// <paramref name="materialCards"/> dört stok kartı (tutulan null). Hammadde kartlarının verisini burası bağlar.
        /// </summary>
        public void Layout(BoardSnapshot s, Func<string, BoardCardView> itemCard, BoardCardView[] materialCards, bool animate)
        {
            if (s == null) return;
            lastLayout = s;
            CaptureCountColours();
            float settle = BoardViewUtil.Num("motion.board.itemTuckS", 0.15f);
            float matLevel = BoardViewUtil.Num("board.layoutMm.materialLevel", 0f);

            int matCount = Mathf.Min(4, s.Materials.Length);
            for (int i = 0; i < matCount; i++)
            {
                var card = materialCards != null && i < materialCards.Length ? materialCards[i] : null;
                if (card == null) continue;
                lastMaterials[i] = card;
                if (s.Materials[i] != null) card.Bind(s.Materials[i]);
                card.SetMagnetVisible(true);
                var target = BoardLayout.MmToLocal(BoardLayout.Material(i), BoardLayout.CardZ(matLevel));
                BoardViewUtil.Pose(card, target, 0, animate && materialPlaced[i], settle, CardEase.EaseOut);
                card.SetVisible(true);
                materialPlaced[i] = true;
            }

            int kitCount = s.Kits.Length;
            for (int k = 0; k < kitCount; k++)
            {
                var kit = s.Kits[k];
                if (kit != null && itemCard != null)
                {
                    int m = kit.TopIds.Count;
                    for (int d = 0; d < m; d++)
                    {
                        var card = itemCard(kit.TopIds[d]);
                        if (card == null) continue;
                        int j = m - 1 - d;   // 0 = üst (en yeni), çapada ve en önde
                        var target = BoardLayout.MmToLocal(BoardLayout.KitCard(k, j), BoardLayout.CardZ(BoardLayout.KitLevel(j)));
                        card.SetMagnetVisible(true);
                        card.SetVisible(true);
                        BoardViewUtil.Pose(card, target, 0, animate, settle, CardEase.EaseOut);
                    }
                }
                SetCount(k, kit);
            }
        }

        void SetCount(int k, KitStackData kit)
        {
            if (counts == null || k >= counts.Length || counts[k] == null) return;
            var t = counts[k];
            BoardViewUtil.SetText(t, kit != null ? kit.CountText : "");
            var c = countBase != null && k < countBase.Length ? countBase[k] : t.color;
            float a = kit == null || kit.Count <= 0 ? BoardViewUtil.Num("board.depot.emptyCountAlpha", 0.35f) : 1f;
            var want = new Color(c.r, c.g, c.b, c.a * a);
            if (t.color != want) t.color = want;
        }

        /// <summary>
        /// Reddedilen hazırlama (yetersiz serbest stok): o hammaddenin stok kartındaki serbest sayı 0,2 s kırmızı yanar.
        /// Sayı <see cref="MaterialCardFace.FreeText"/>'tir (yoksa yüzdeki en büyük puntolu yazı, 22 mm); ilk kullanımda bulunur ve saklanır.
        /// </summary>
        public void DenyFlash(string materialKind)
        {
            int i = Array.IndexOf(BoardLayout.MaterialKinds, materialKind);
            if (i < 0 || lastMaterials[i] == null) return;
            var card = lastMaterials[i];
            if (flashOwner[i] != card) { flashOwner[i] = card; flashText[i] = FindNumber(card); }
            var t = flashText[i];
            if (t == null) return;
            if (!flashing[i]) { flashBase[i] = t.color; flashing[i] = true; flashCount++; }
            flashColour[i] = BoardViewUtil.Col("cards.material.reservedRing", new Color32(0x9A, 0x1E, 0x14, 0xFF));
            t.color = flashColour[i];
            flashUntil[i] = Time.unscaledTime + BoardViewUtil.Num("motion.board.denyFlashS", 0.2f);
        }

        static TMP_Text FindNumber(BoardCardView card)
        {
            if (card.Face is MaterialCardFace mf && mf.FreeText != null) return mf.FreeText;
            TMP_Text best = null;
            var texts = card.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
                if (best == null || texts[i].fontSize > best.fontSize) best = texts[i];
            return best;
        }

        void Update()
        {
            Advance(Time.unscaledDeltaTime);
            if (flashCount == 0) return;
            float now = Time.unscaledTime;
            for (int i = 0; i < flashing.Length; i++)
            {
                if (!flashing[i] || now < flashUntil[i]) continue;
                flashing[i] = false;
                flashCount--;
                Restore(i);
            }
        }

        void OnDisable()
        {
            CancelSlide();   // olay yayılmaz; BoardView3D sonraki çizimde kayışı bitmiş sayar
            for (int i = 0; i < flashing.Length; i++)
            {
                if (!flashing[i]) continue;
                flashing[i] = false;
                Restore(i);
            }
            flashCount = 0;
        }

        /// <summary>Yanıp sönme biter: yazı hâlâ kırmızıysa eski rengine döner (bu arada yüz yeniden bağlanıp rengi değiştirdiyse dokunulmaz).</summary>
        void Restore(int i)
        {
            var t = flashText[i];
            if (t != null && t.color == flashColour[i]) t.color = flashBase[i];
        }
    }
}
