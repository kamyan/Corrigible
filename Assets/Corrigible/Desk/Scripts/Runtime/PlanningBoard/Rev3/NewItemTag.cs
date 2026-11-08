using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Depoya yeni gelen ürünün kırmızı "YENİ" levhası (Revizyon 3 · Aşama 5, R18d; PLAN §1.11, D6-A, D9-A). Sahnedeki World Space
    /// tuvaldir (ölçek 0,001: 1 birim = 1 mm; <c>SiginakRoom/Planning board/YENİ etiketi</c>): kırmızı zemin (<see cref="background"/>)
    /// ve CP 700 sözcük (<see cref="word"/>). Boyut ve renkler temadan (<c>board.newTag.*</c>: 32 × 13 mm, #B4291C, #F7EFD8, em 9,4 mm).
    /// <list type="bullet">
    /// <item>Kit yığınının üst kartına iliştirilir ve onu izler: sol kenarı kart merkezinin <see cref="offsetMm"/>.x (8 mm) sağında,
    /// alt kenarı kartın üst kenarına <see cref="offsetMm"/>.y (3 mm) biner (<c>dress_room_board.js:155–156</c>); kartın önünde.</item>
    /// <item>İlk dokunuşta <c>motion.board.itemTuckS</c> (0,15 s) içinde alfa ile söner, sonra gizlenir.</item>
    /// <item>Sözcüğü sunucu çevirip verir (<see cref="SetText"/>); sahnede <c>DeskLocText</c> yoktur. Komut yazmaz.</item>
    /// </list>
    /// Edit modda <c>Update</c> çalışmadığı için zaman <see cref="Advance"/> ile ilerler.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NewItemTag : MonoBehaviour
    {
        [Tooltip("Kırmızı zemin (tuvalin tamamı).")]
        [SerializeField] Image background;
        [Tooltip("YENİ / NEW (CP 700; fontun ışıksız malzemesi; otomatik punto yok).")]
        [SerializeField] TMP_Text word;
        [Tooltip("Sönme için (yoksa zemin ve yazının alfası değişir).")]
        [SerializeField] CanvasGroup group;
        [Tooltip("x: levhanın sol kenarı kart merkezinden sağa (mm); y: alt kenarın kartın üst kenarına binişi (mm).")]
        [SerializeField] Vector2 offsetMm = new Vector2(8f, 3f);
        [Tooltip("Levhanın kart kökünden öne (−z) uzaklığı (mm): kartın yüz tuvali 1 mm öndedir, levha onun 0,6 mm önünde.")]
        [SerializeField] float frontMm = 1.6f;

        Transform target;
        bool shown, fading;
        float alpha = 1f, fadeElapsed, fadeSeconds;
        Color backBase = Color.white, wordBase = Color.white;

        /// <summary>İliştirildiği kart (gösterilmiyorsa null).</summary>
        public Transform Target => shown || fading ? target : null;
        /// <summary>Levha görünüyor (sönmekte olan dahil değil).</summary>
        public bool Shown => shown;
        public bool Fading => fading;
        public float Alpha => alpha;
        public TMP_Text Word => word;
        public Image Background => background;
        /// <summary>Levhanın boyutu (mm, temadan).</summary>
        public static Vector2 SizeMm => new Vector2(BoardViewUtil.Num("board.newTag.sizeMm[0]", 32f), BoardViewUtil.Num("board.newTag.sizeMm[1]", 13f));
        /// <summary>Levhanın merkezinin kart merkezine göre yereli (metre; kartın yüzü −z).</summary>
        public Vector3 OffsetLocal
        {
            get
            {
                var size = SizeMm;
                float cardH = BoardViewUtil.Num("cards.sizesMm.item[1]", 50f);
                return new Vector3(offsetMm.x + size.x / 2f, cardH / 2f + size.y / 2f - offsetMm.y, -frontMm) / 1000f;
            }
        }

        /// <summary>Levhayı karta iliştirir ya da kaldırır; <paramref name="animate"/> false ise anında (sönmeden).</summary>
        public void Show(Transform card, bool on, bool animate)
        {
            ApplyTheme();
            if (on && card != null)
            {
                target = card;
                shown = true;
                fading = false;
                SetAlpha(1f);
                if (!gameObject.activeSelf) gameObject.SetActive(true);
                Follow();
                return;
            }
            if (!shown && !fading) { Hide(); return; }
            shown = false;
            if (!animate || target == null)
            {
                Hide();
                return;
            }
            if (fading) return;
            fading = true;
            fadeElapsed = 0;
            fadeSeconds = BoardViewUtil.Num("motion.board.itemTuckS", 0.15f);
            if (fadeSeconds <= 0) Hide();
        }

        /// <summary>Levhanın sözcüğü (bu dilde, "YENİ" / "NEW").</summary>
        public void SetText(string value) => BoardViewUtil.SetText(word, value);

        /// <summary>Boyut, renk ve punto temadan (yalnız değişince yazar).</summary>
        public void ApplyTheme()
        {
            var size = SizeMm;
            if (transform is RectTransform rt)
            {
                if (rt.sizeDelta != size) rt.sizeDelta = size;
                var half = new Vector2(0.5f, 0.5f);
                if (rt.pivot != half) rt.pivot = half;
            }
            backBase = BoardViewUtil.Col("board.newTag.color", new Color32(0xB4, 0x29, 0x1C, 0xFF));
            wordBase = BoardViewUtil.Col("board.newTag.ink", new Color32(0xF7, 0xEF, 0xD8, 0xFF));
            if (word != null)
            {
                float em = BoardViewUtil.Num("board.newTag.capMm", 9.4f);
                if (!Mathf.Approximately(word.fontSize, em)) word.fontSize = em;
                if (word.enableAutoSizing) word.enableAutoSizing = false;
            }
            SetAlpha(alpha);
        }

        /// <summary>Sönmeyi ilerletir ve kartı izler.</summary>
        public void Advance(float dt)
        {
            if (fading)
            {
                fadeElapsed += Mathf.Max(0, dt);
                float t = fadeSeconds > 0 ? Mathf.Clamp01(fadeElapsed / fadeSeconds) : 1f;
                SetAlpha(1f - t);
                if (t >= 1f) Hide();
            }
            Follow();
        }

        void Update() => Advance(Time.unscaledDeltaTime);

        /// <summary>Kart kendi <c>Update</c>'inde hareket eder; levha karenin sonunda yerine oturur.</summary>
        void LateUpdate() => Follow();

        void OnDisable()
        {
            if (fading) Hide();
        }

        void Follow()
        {
            if (target == null || (!shown && !fading)) return;
            var pos = target.TransformPoint(OffsetLocal);
            var rot = target.rotation;
            if (transform.position != pos || transform.rotation != rot) transform.SetPositionAndRotation(pos, rot);
        }

        void Hide()
        {
            shown = false;
            fading = false;
            target = null;
            SetAlpha(1f);
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        void SetAlpha(float a)
        {
            alpha = a;
            if (group != null)
            {
                if (!Mathf.Approximately(group.alpha, a)) group.alpha = a;
                SetColour(background, backBase);
                SetColour(word, wordBase);
                return;
            }
            SetColour(background, new Color(backBase.r, backBase.g, backBase.b, backBase.a * a));
            SetColour(word, new Color(wordBase.r, wordBase.g, wordBase.b, wordBase.a * a));
        }

        static void SetColour(Graphic g, Color c)
        {
            if (g != null && g.color != c) g.color = c;
        }
    }
}
