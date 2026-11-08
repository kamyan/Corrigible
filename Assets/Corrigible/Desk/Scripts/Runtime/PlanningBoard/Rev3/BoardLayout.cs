using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kartlı panonun bütün geometrisi (Revizyon 3 · R15, token <c>board.layoutMm</c>): pano mm'si iç alanın sol üst köşesinden,
    /// y aşağı. Model yereli metre: x = −0,580 + mm.x / 1000, y = 1,593 − mm.y / 1000; pano yüzü z −0,014, kartlar önünde (daha
    /// eksi z). Ölçüler temadan okunur; buradaki sayılar yalnız tema anahtarı yokken kullanılan token değerleridir. Saf hesap:
    /// sahneye dokunmaz, testlerden doğrudan çağrılır.
    /// </summary>
    public static class BoardLayout
    {
        /// <summary>Pano yüzünün (baskı) model yerelindeki z'si.</summary>
        public const float FaceZ = -0.014f;
        public const float InnerLeft = -0.580f, InnerTop = 1.593f;
        public const float InnerWidthMm = 1160, InnerHeightMm = 600;
        /// <summary>Cepteki kişi kartının seviyesi (z = FaceZ − 0,0003 − 0,0007 · seviye).</summary>
        public const float PocketLevel = 3;

        /// <summary>Model çapalarının cep adları ve karşılık gelen Core görevleri (aynı sırayla).</summary>
        public static readonly string[] Slots = { "tek", "emp", "ana", "uret", "mutf", "egit" };
        public static readonly string[] Jobs = { "tek", "emp", "ana", "craft", "cook", "train" };
        /// <summary>Hammadde türleri (stok kartlarının sırası) ve kit yığınlarının türleri.</summary>
        public static readonly string[] MaterialKinds = { "metal", "devre", "kumas", "kimyasal" };
        public static readonly string[] KitKinds = { "kit_tek", "kit_emp", "kit_ana", "armor", "medkit", "emp_cell" };

        static float N(string key, float fallback)
        {
            var t = SiginakTheme.Active;
            return t != null ? t.NumberOf(key, fallback) : fallback;
        }

        static Rect R(string key, float x, float y, float w, float h) =>
            new Rect(N(key + "[0]", x), N(key + "[1]", y), N(key + "[2]", w), N(key + "[3]", h));

        static Vector2 P(string key, float x, float y) => new Vector2(N(key + "[0]", x), N(key + "[1]", y));

        // ---------------------------------------------------------------- dönüşüm

        public static Vector3 MmToLocal(Vector2 mm, float z) => new Vector3(InnerLeft + mm.x / 1000f, InnerTop - mm.y / 1000f, z);

        public static Vector2 LocalToMm(Vector3 local) => new Vector2((local.x - InnerLeft) * 1000f, (InnerTop - local.y) * 1000f);

        /// <summary>Seviyedeki kartın z'si (seviye büyüdükçe öne).</summary>
        public static float CardZ(float level) => FaceZ - 0.0003f - 0.0007f * level;

        // ---------------------------------------------------------------- cepler

        public static int SlotIndexOfJob(string job) => System.Array.IndexOf(Jobs, job);

        public static string JobOfSlot(string slot)
        {
            int i = System.Array.IndexOf(Slots, slot);
            return i < 0 ? null : Jobs[i];
        }

        public static string SlotOfJob(string job)
        {
            int i = SlotIndexOfJob(job);
            return i < 0 ? null : Slots[i];
        }

        static readonly float[,] PocketFallback =
        {
            { 14, 44, 108, 194 }, { 132, 44, 108, 194 }, { 250, 44, 108, 194 },
            { 14, 244, 108, 194 }, { 132, 244, 108, 194 }, { 250, 244, 180, 194 },
        };

        static readonly float[,] CentreFallback = { { 68, 128 }, { 186, 128 }, { 304, 128 }, { 68, 328 }, { 186, 328 }, { 304, 328 } };

        /// <summary>Cep dikdörtgeni (Core görevine göre). EĞİTİM 180 mm geniş (konu listesi dahil).</summary>
        public static Rect Pocket(string job)
        {
            int i = SlotIndexOfJob(job);
            if (i < 0) return default;
            return R("board.layoutMm.pockets." + Slots[i], PocketFallback[i, 0], PocketFallback[i, 1], PocketFallback[i, 2], PocketFallback[i, 3]);
        }

        /// <summary>Cepteki kişi kartının merkezi.</summary>
        public static Vector2 CardCentre(string job)
        {
            int i = SlotIndexOfJob(job);
            if (i < 0) return default;
            return P("board.layoutMm.cardCentres." + Slots[i], CentreFallback[i, 0], CentreFallback[i, 1]);
        }

        public static Rect Bosta => R("board.layoutMm.bosta", 380, 44, 770, 194);
        public static Rect Order => R("board.layoutMm.order", 446, 244, 450, 194);
        public static Rect Sheet => R("board.layoutMm.sheet", 450, 266, 440, 170);
        public static Rect Inputs => R("board.layoutMm.inputs", 582, 296, 294, 56);
        public static Vector2 Recipe => P("board.layoutMm.recipe", 514, 328);
        public static Rect Folder => R("board.layoutMm.folder", 906, 244, 244, 194);
        public static Rect FolderFace => R("board.layoutMm.folderFace", 963, 268, 130, 180);
        public static Rect Hurda => R("board.layoutMm.hurda", 1012, 474, 130, 126);
        /// <summary>DEPO bandı (hammadde ve kitler; eşya buraya bırakılınca sahipsiz olur).</summary>
        public static Rect Depot => new Rect(0, N("board.layoutMm.depotY", 446), InnerWidthMm, InnerHeightMm - N("board.layoutMm.depotY", 446));
        public static Rect Back => R("board.layoutMm.header.back", 6, 6, 124, 28);
        public static Rect Title => R("board.layoutMm.header.title", 140, 7, 404, 26);
        public static Rect Slate => R("board.layoutMm.header.slate", 556, 5, 440, 30);
        public static Rect Flap => R("board.layoutMm.header.flap", 1006, 5, 144, 30);
        public static Vector2 Book => P("board.layoutMm.book", 70, 452);

        // ---------------------------------------------------------------- tarif kartı (Aşama 5 · R18b, [B])

        /// <summary>Tarif kartının boyu (token <c>cards.sizesMm.recipe</c>, 112 × 80 mm).</summary>
        public static Vector2 RecipeSizeMm => P("cards.sizesMm.recipe", 112, 80);

        /// <summary>İş emrindeki tarif kartının seviyesi (fişlerin 0–1 seviyesiyle aynı düzlem; kart girdi alanıyla çakışmaz).</summary>
        public const float RecipeLevel = 1;

        /// <summary>
        /// İş emri üstünde tarif kartı merkezinin serbest olabileceği dikdörtgen: İŞ EMRİ alanının yarım kart boyu kadar içi (kart
        /// alanın dışına taşmaz). Alan karttan darsa merkez çizgisine iner.
        /// </summary>
        public static Rect RecipeOnOrder
        {
            get
            {
                var order = Order;
                var half = RecipeSizeMm * 0.5f;
                float x0 = order.xMin + half.x, x1 = order.xMax - half.x, y0 = order.yMin + half.y, y1 = order.yMax - half.y;
                if (x1 < x0) x0 = x1 = order.center.x;
                if (y1 < y0) y0 = y1 = order.center.y;
                return Rect.MinMaxRect(x0, y0, x1, y1);
            }
        }

        /// <summary>Tarif kartı merkezini <see cref="RecipeOnOrder"/> içine kırpar (içtekine dokunulmaz).</summary>
        public static Vector2 ClampRecipe(Vector2 mm)
        {
            var r = RecipeOnOrder;
            return new Vector2(Mathf.Clamp(mm.x, r.xMin, r.xMax), Mathf.Clamp(mm.y, r.yMin, r.yMax));
        }
        public static float LockY => N("board.layoutMm.lockY", 241);

        // ---------------------------------------------------------------- deste

        public static Vector2 Deck => P("board.layoutMm.deck", 1098, 128);
        public static float HandLeftX => N("board.layoutMm.handLeft", 462);

        /// <summary>Açık eldeki kart aralığı: min(82, 636 / (n − 1)) mm (636 = deste x − elin sol ucu).</summary>
        public static float HandStep(int n)
        {
            float max = N("board.deck.maxStepMm", 82);
            return n <= 1 ? max : Mathf.Min(max, (Deck.x - HandLeftX) / (n - 1));
        }

        /// <summary>Kapalı destede j. kart (0 = üst): her alttaki 1 mm sağ-aşağı.</summary>
        public static Vector2 DeckClosed(int j) => Deck + new Vector2(j, j);
        public static float DeckClosedLevel(int j, int n) => PocketLevel + (n - 1 - j) * 1.6f;

        /// <summary>Açık elde j. kart: üst kart destede kalır, diğerleri sola.</summary>
        public static Vector2 HandOpen(int j, int n) => new Vector2(Deck.x - j * HandStep(n), Deck.y);
        public static float HandLevel(int j, int n) => PocketLevel + (n - 1 - j) * 2f;

        public static float DeckPerCardS => N("motion.board.deckPerCardS", 0.32f);
        public static float DeckGapS => N("motion.board.deckGapS", 0.07f);

        /// <summary>Açılırken sıradaki kartın (0 = ilk hareket eden) başlangıcı ve bitişi, saniye.</summary>
        public static float DeckStart(int order) => order * DeckGapS;
        public static float DeckEnd(int order) => DeckStart(order) + DeckPerCardS;

        // ---------------------------------------------------------------- eşya yığını

        static readonly float[] ItemLevels = { 2f, 1.1f, 0.2f };

        /// <summary>
        /// Kişinin arkasındaki n. eşya (sıkıştırılmış: kit, zırh, yardımcı): merkezi kişi merkezinin 49 + 14·n mm altında, 14 mm
        /// şeridi görünür. Dönen değer kişi merkezine göre mm (y aşağı).
        /// </summary>
        public static Vector2 ItemOffsetMm(int n) => new Vector2(0, 49 + 14 * n);
        public static float ItemLevel(int n) => ItemLevels[Mathf.Clamp(n, 0, 2)];

        // ---------------------------------------------------------------- depo, fişler, konular

        public static Vector2 Material(int i) => new Vector2(N($"board.layoutMm.materialsX[{i}]", 180 + 88 * i), N("board.layoutMm.materialsY", 502));
        public static Vector2 Kit(int i) => new Vector2(N($"board.layoutMm.kitsX[{i}]", 541 + 82 * i), N("board.layoutMm.kitsY", 502));
        /// <summary>Kit yığınında j. kart (0 = en alttaki çizilen), seviye 2 − j.</summary>
        public static Vector2 KitCard(int kit, int j) => Kit(kit) + new Vector2(0.9f * j, 0.9f * j);
        public static float KitLevel(int j) => 2 - j;
        /// <summary>×n etiketinin yeri (kit merkezinin sağ altı).</summary>
        public static Vector2 KitCount(int kit) => Kit(kit) + new Vector2(26, 34);

        static readonly Vector3[] ChipSpots = { new Vector3(612, 314, -4), new Vector3(668, 330, 3), new Vector3(742, 318, -2), new Vector3(800, 334, 5) };

        /// <summary>İş emri girdi alanındaki i. fişin kendiliğinden yeri (mm) ve açısı (derece); dört yer döner, seviye i % 2.</summary>
        public static Vector2 ChipSpot(int i, out float angleDeg, out float level)
        {
            var s = ChipSpots[i % ChipSpots.Length];
            int lap = i / ChipSpots.Length;
            angleDeg = s.z;
            level = (i % 2) + lap * 0.7f;
            return new Vector2(s.x + lap * 3, s.y + lap * 3);
        }

        /// <summary>Eğitim konusu satırı (7 satır, 16 mm aralık; satır i, i. ve i+1. çizgi arası).</summary>
        public static Rect TopicRow(int row) =>
            new Rect(N("board.layoutMm.topics7.x", 354), N("board.layoutMm.topics7.y0", 271) + N("board.layoutMm.topics7.dy", 16) * row,
                N("board.layoutMm.topics7.w", 74), N("board.layoutMm.topics7.dy", 16));

        /// <summary>Konu yazısının taban çizgisi (sol).</summary>
        public static Vector2 TopicBaseline(int row)
        {
            var r = TopicRow(row);
            return new Vector2(r.x + 2, r.y + r.height - 3.5f);
        }

        // ---------------------------------------------------------------- isabet

        /// <summary>
        /// Pano yüzündeki noktanın bölgesi (sürüklenen kartın türüne göre geçerli olanlar): kişi için cepler ve BOŞTA, eşya ve fiş
        /// için DEPO bandı, stok kartı için İŞ EMRİ. Hurda ve kişi kartı hedefleri çarpıştırıcıyla çözülür (çıkıntılı), burada yok.
        /// Tarif kartı (Aşama 5 · [B]): İŞ EMRİ içi <see cref="BoardTargetKind.Order"/>, dışı her yer <see cref="BoardTargetKind.Book"/>
        /// (deftere dön: kopya için komutsuz dönüş, formdaki kart için iptal).
        /// </summary>
        public static BoardTarget ZoneAt(Vector2 mm, BoardCardKind dragged)
        {
            switch (dragged)
            {
                case BoardCardKind.Person:
                    foreach (var job in Jobs) if (Pocket(job).Contains(mm)) return BoardTarget.Pocket(job);
                    if (Bosta.Contains(mm)) return BoardTarget.Bosta();
                    return BoardTarget.None;
                case BoardCardKind.Item:
                case BoardCardKind.Chip:
                    return Depot.Contains(mm) ? BoardTarget.Store() : BoardTarget.None;
                case BoardCardKind.Material:
                    return Order.Contains(mm) ? BoardTarget.Order() : BoardTarget.None;
                case BoardCardKind.Recipe:
                    return Order.Contains(mm) ? BoardTarget.Order() : BoardTarget.Book();
                default:
                    return BoardTarget.None;
            }
        }
    }
}
