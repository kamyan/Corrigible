using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Revizyon 3 kartlı panonun görünümü (R15/R16), <c>SiginakRoom/Planning board</c> kökünde. <see cref="BoardSnapshot"/>'ı
    /// okur, simülasyona dokunmaz: kart havuzlarını kimliğe göre tutar (kişi 10, gerekirse büyür; eşya 32; hammadde 4; fiş 8),
    /// kartları ceplere, Boşta destesine, sahiplerinin arkasına (kit → zırh → yardımcı, sıkıştırılmış), depoya ve iş emrine
    /// yerleştirir; tebeşir başlığı (değişen parça 0,2 s silinip yazılır), PLAN kapağı (0 ↔ 180°, 0,3 s), kilit çubuğu (0,4 s,
    /// yalnız K4'te; uzaktayken anında, K4'e varınca bir kez oynar), kilit etiketi 2. satırı, hurda 2. satırı, MASAYA DÖN
    /// tabelasının üzerine gelme kayması ve lamba camı burada. Düzen yalnız değişince kurulur; karede yalnız hareket eden
    /// sahiplerin eşyaları izlenir. Sürükleme denetleyicisinin sahiplendiği karta (<see cref="BoardDragController.Owns"/>)
    /// dokunulmaz, bırakılması onun animasyonu bitene kadar ertelenir.
    /// <para>Aşama 5 (R18; PLAN §1.6, §1.11, §1.12): iş emrinin TARİF kutusundaki tarif kartı ve [B] defterden alınan kopya (tarif
    /// havuzu, 2 kart), formun durum satırı ve DURDU damgası (<see cref="WorkOrderView.SetSheet(OrderData)"/>), son kapanışın yeni
    /// ürünü (K4'e ilk varışta TARİF kutusundan yığına kayış, YENİ levhası; ikisi de sunum durumu), defter kipi
    /// (<see cref="SetBookMode"/>) ve K4'ten çıkışta defterin kapanması.</para>
    /// </summary>
    public sealed class BoardView3D : MonoBehaviour
    {
        const int PersonPool = 10, ItemPool = 32, ChipPool = 8, RecipePool = 2;
        /// <summary>Bu yakınlıktaki (4 mm) kart cebine yay çizmeden oturur.</summary>
        const float SettleNearSqr = 0.004f * 0.004f;

        [Header("Pano modeli (PlanningBoard)")]
        [SerializeField] Rev3Model model;
        [SerializeField] PartDriver backSign, flap, hurdaLid, lockRod, lamp;
        [Tooltip("Pano lambasının ışığı (BoardLamp); lamba camı (Lamp swap) bunun açık/kapalı durumunu izler. Boşsa cam değişmez.")]
        [SerializeField] Light boardLamp;
        [Tooltip("TXT_Header: tebeşir başlık (dinamik, DeskLocText yok).")]
        [SerializeField] TMP_Text header;
        [Tooltip("Kilit etiketinin 2. satırı (kilit nedeni); 1. satır TXT_LockTag'de sabit, DeskLocText ile.")]
        [SerializeField] TMP_Text lockTagLine2;
        [Tooltip("Hurda plakasının 2. satırı (denge değerinden, eleştiri #9); 1. satır TXT_P_hurda'da sabit.")]
        [SerializeField] TMP_Text hurdaLine2;
        [Tooltip("MASAYA DÖN tabelasının DeskTarget'ı (üzerine gelince BackSign 4 mm öne kayar; tıklamayı sunucu bağlar).")]
        [SerializeField] DeskTarget backTarget;

        [Header("Kartlar")]
        [SerializeField] BoardCardView personPrefab;
        [SerializeField] BoardCardView itemPrefab;
        [SerializeField] BoardCardView materialPrefab;
        [SerializeField] BoardCardView chipPrefab;
        [Tooltip("Aşama 5: tarif kartı (RecipeCard.prefab); havuzda iki kart: iş emrindeki ve defterden alınan kopya.")]
        [SerializeField] BoardCardView recipePrefab;
        [Tooltip("Planning board/Kartlar (birim dönüşüm); kartların localPosition'ı model yerelidir.")]
        [SerializeField] Transform cardRoot;
        [SerializeField] PortraitLibrary portraits;

        [Header("Alt görünümler")]
        [SerializeField] BostaDeckView deckView;
        [SerializeField] StoreView store;
        [SerializeField] WorkOrderView order;
        [SerializeField] TrainingTopics topics;
        [SerializeField] PersonFolderView folder;
        [SerializeField] LostEnvelopeView lostEnvelope;
        [SerializeField] HurdaBin hurda;
        [SerializeField] BoardMarks marks;
        [SerializeField] BoardDragController drag;
        [Tooltip("Aşama 5: kancadaki 3B tarif defteri (Tarif defteri kökündeki RecipeBookView).")]
        [SerializeField] RecipeBookView book;
        [Tooltip("Üzerine gelme etiketi; konu satırlarına, dosyaya ve KAYIPLAR zarfına verilir (kendi etiketleri boşsa).")]
        [SerializeField] HoverTag hoverTag;

        [Header("Girdi kapısı (yalnız K4'e varınca açık)")]
        [Tooltip("Pano erişimi çarpıştırıcısı: K4'te kapalı (bütün ışınları yutar), başka pozlarda açık.")]
        [SerializeField] Collider boardEntry;
        [Tooltip("K4'te açılan sabit hedefler: BackSign, deste tepsisi, hurda önü, Pano yüzü …")]
        [SerializeField] Collider[] staticColliders = new Collider[0];
        [Tooltip("Aşama 5: Pano yüzü çarpıştırıcısı; defter kipinde açık kalan tek pano hedefi (MASAYA DÖN levhasıyla birlikte).")]
        [SerializeField] Collider boardFace;

        // ------------------------------------------------------------------ havuzlar
        readonly Dictionary<string, BoardCardView> persons = new Dictionary<string, BoardCardView>();
        readonly Dictionary<string, BoardCardView> items = new Dictionary<string, BoardCardView>();
        readonly Dictionary<string, BoardCardView> chips = new Dictionary<string, BoardCardView>();
        readonly BoardCardView[] materials = new BoardCardView[4];
        readonly List<BoardCardView> freePersons = new List<BoardCardView>();
        readonly List<BoardCardView> freeItems = new List<BoardCardView>();
        readonly List<BoardCardView> freeChips = new List<BoardCardView>();
        readonly List<BoardCardView> freeRecipes = new List<BoardCardView>();
        /// <summary>[B] Defterden alınmış, henüz form kartı olmamış ya da havuza dönmemiş kopyalar.</summary>
        readonly List<BoardCardView> recipeCopies = new List<BoardCardView>();
        BoardCardView formRecipe;
        /// <summary>Form kartı az önce kopyadan geldi: kayıtlı poz yoksa bırakıldığı pozda kalır (bir kez; sıçramaz).</summary>
        bool formFromCopy;
        readonly List<BoardCardView> pendingRelease = new List<BoardCardView>();
        readonly List<BoardCardView> exiting = new List<BoardCardView>();
        readonly List<BoardCardView> active = new List<BoardCardView>();
        int created;
        bool poolsReady;

        // ------------------------------------------------------------------ Render tamponları (ayırma yok)
        readonly HashSet<string> seen = new HashSet<string>();
        readonly List<string> gone = new List<string>();
        readonly HashSet<BoardCardView> fresh = new HashSet<BoardCardView>();
        readonly HashSet<BoardCardView> placed = new HashSet<BoardCardView>();
        readonly HashSet<BoardCardView> clickable = new HashSet<BoardCardView>();
        readonly HashSet<BoardCardView> deckLower = new HashSet<BoardCardView>();
        readonly List<BoardCardView> deckList = new List<BoardCardView>();
        readonly List<BoardCardView> chipList = new List<BoardCardView>();
        readonly List<BoardCardView> skipped = new List<BoardCardView>();
        readonly BoardCardView[] materialBuffer = new BoardCardView[4];
        readonly Dictionary<string, FollowEntry> followByOwner = new Dictionary<string, FollowEntry>();
        readonly List<FollowEntry> follows = new List<FollowEntry>();
        readonly Stack<FollowEntry> followPool = new Stack<FollowEntry>();
        static readonly ItemSlotKind[] SlotOrder = { ItemSlotKind.Kit, ItemSlotKind.Armor, ItemSlotKind.Aid };
        readonly int[] chipWanted = new int[4];
        readonly List<BoardCardView> kindCards = new List<BoardCardView>();
        static readonly Comparison<BoardCardView> ByChipIndex = CompareChipIndex;

        Func<string, BoardCardView> storeItemCard;
        Func<string, Vector3?> chipPoseLookup;
        bool subViewsHooked;

        // yeni ürün (sunum durumu; anahtar = eşya kimliği + gün)
        enum SlideState { None, Waiting, Sliding, Done }
        NewItemData newItem;
        string newItemKey;
        SlideState slide;
        bool newTouched;
        BoardCardView newCard;

        bool bookMode;

        // ------------------------------------------------------------------ durum
        BoardSnapshot last;
        bool rendered, interactive, pendingRelayout, deckOpenShown;
        bool lampKnown, lampOn;

        // tebeşir başlık
        string pendingHeader;
        float headerSwapAt;

        // kilit ve kapak
        bool lockKnown, lockedTarget, lockSeen;
        float flapFrom, flapTo, flapStart, flapSeconds;
        bool flapActive;
        float rodFrom, rodTo, rodStart, rodSeconds, rodOffset;
        bool rodActive, rodHideAtEnd, rodSoundAtEnd;

        // MASAYA DÖN kayması
        float backFrom, backTo, backStart, backValue;
        bool backActive;

        sealed class FollowEntry
        {
            public BoardCardView Owner;
            public readonly List<BoardCardView> Items = new List<BoardCardView>(3);
            public bool Active;
        }

        // ------------------------------------------------------------------ genel API

        public IReadOnlyList<BoardCardView> ActiveCards => active;
        public bool Interactive => interactive;
        public BostaDeckView Deck => deckView;
        public StoreView Store => store;
        public WorkOrderView Order => order;
        public TrainingTopics Topics => topics;
        public PersonFolderView Folder => folder;
        public LostEnvelopeView LostEnvelope => lostEnvelope;
        public HurdaBin Hurda => hurda;
        public BoardMarks Marks => marks;
        public BoardDragController Drag => drag;
        /// <summary>Son çizilen anlık görüntü (yoksa null).</summary>
        public BoardSnapshot Last => last;

        /// <summary>
        /// İş emri fişlerinin kayıtlı pozu: fiş kimliği ("chip:metal:0") → (mm.x, mm.y, açı); yoksa null. Sunucu
        /// <c>BoardUiState.ChipPoses</c>'tan bağlar; boşsa fişler kendiliğinden yerlerine (<see cref="BoardLayout.ChipSpot"/>) dizilir.
        /// </summary>
        public Func<string, Vector3?> ChipPose { get; set; }

        /// <summary>Kendi seslerini (kapak, asma kilit, eşya sıkışması) ve deste ile hurdanın seslerini toplar. Sürükleme
        /// denetleyicisinin sesleri burada değildir (sunucu onu ayrıca dinler).</summary>
        public event Action<BoardSound> Sound;

        // ------------------------------------------------------------------ Aşama 5 (R18; PLAN §1.6, §1.11, §1.12)

        /// <summary>Kancadaki tarif defteri (sahnede bağlıysa).</summary>
        public RecipeBookView Book => book;

        /// <summary>İş emrinin TARİF kutusundaki tarif kartı; kutu boşsa null.</summary>
        public BoardCardView OrderRecipeCard => formRecipe != null && formRecipe.gameObject.activeSelf ? formRecipe : null;

        /// <summary>Defter kipi açık mı (<see cref="SetBookMode"/>).</summary>
        public bool BookMode => bookMode;

        /// <summary>Yeni ürün kartı (kayış ya da YENİ levhası için; yoksa null).</summary>
        public BoardCardView NewItemCard => newItem != null ? newCard : null;
        /// <summary>Yeni ürün K4'e varışı bekliyor (TARİF kutusunda duruyor).</summary>
        public bool NewItemWaiting => slide == SlideState.Waiting;
        /// <summary>Yeni ürün yığına kayıyor.</summary>
        public bool NewItemSliding => slide == SlideState.Sliding;

        /// <summary>
        /// [B] Defterden alınan tarifin kopyası: havuzdaki boş tarif kartı (form kartı değil), tarifin verisiyle bağlanmış, mıknatısı
        /// gizli, çarpıştırıcısı kapalı ve gizli. Sunucu ve sürükleme denetleyicisi konumlar, gösterir ve sürer. Bırakma kabul edilip
        /// iş emri o tarifle kurulunca sonraki çizimde form kartı olur (havuzda tek form kartı); değilse
        /// <see cref="ReleaseRecipeCopy"/> ile döner.
        /// </summary>
        public BoardCardView AcquireRecipeCopy(RecipeData r)
        {
            if (r == null) return null;
            EnsurePools();
            BoardCardView c = null;
            if (freeRecipes.Count > 0) { c = freeRecipes[freeRecipes.Count - 1]; freeRecipes.RemoveAt(freeRecipes.Count - 1); }
            else if (recipePrefab != null && cardRoot != null) c = Create(recipePrefab);
            if (c == null) return null;
            if (!recipeCopies.Contains(c)) recipeCopies.Add(c);
            if (last != null) Prep(c, last);
            c.Bind(CopyData(r, last != null ? last.Book : null));
            c.SetMagnetVisible(false);
            c.SetInteractive(false);
            c.SetVisible(false);
            return c;
        }

        /// <summary>
        /// [B] Kopya kart havuza döner (reddedilen ya da deftere dönen bırakma). Form kartı olmuş kopyada etkisizdir; tutulan kart
        /// sürükleme denetleyicisi bırakınca döner.
        /// </summary>
        public void ReleaseRecipeCopy(BoardCardView c)
        {
            if (c == null || c == formRecipe || !recipeCopies.Remove(c)) return;
            if (Owned(c) || c.Moving) { if (!pendingRelease.Contains(c)) pendingRelease.Add(c); }
            else Release(c);
        }

        /// <summary>Defterdeki tarifin kart verisi (kopya): kimlik = tarif; süre ve girdiler sayfadan, "GİRDİ" defter sözcüğünden.</summary>
        static RecipeCardData CopyData(RecipeData r, BookWords words)
        {
            var d = new RecipeCardData
            {
                Id = r.Id, RecipeId = r.Id, Name = r.Name, IconKey = r.Id, TurnsLine = r.TurnsLine,
                OutLine = Corrigible.Core.Loc.T("→ 1 ürün"), NeedsLabel = words != null ? words.Input : null,
                Turns = r.Turns, Done = -1,
            };
            foreach (var i in r.Inputs) if (i != null) d.Inputs.Add(i);
            return d;
        }

        /// <summary>
        /// [B] Formdaki tarif kartının kayıtlı pozu: iş emri kimliği → (mm.x, mm.y, açı); yoksa null (kart TARİF kutusunun çapasına
        /// oturur). Sunucu <c>BoardUiState.RecipePoses</c>'tan bağlar.
        /// </summary>
        public Func<string, Vector3?> RecipePose { get; set; }

        /// <summary>
        /// Yeni ürünün kayışı henüz oynamadı mı (sunum durumu, <c>BoardUiState.NewItem.Slid</c> değilse true); null ise oynamış sayılır
        /// (kart doğrudan yığında).
        /// </summary>
        public Func<NewItemData, bool> NewItemPending { get; set; }

        /// <summary>
        /// YENİ levhası hâlâ asılı mı (sunum durumu, <c>BoardUiState.NewItem.Touched</c> değilse true); null ise yalnız bu oturumun
        /// dokunuşu sayılır.
        /// </summary>
        public Func<NewItemData, bool> NewItemTagged { get; set; }

        /// <summary>Yeni ürünün kayışı bitti (sunucu <c>slid = true</c> yazar).</summary>
        public event Action<NewItemData> NewItemSlid;
        /// <summary>Yeni ürün kartına ilk dokunuş (üzerine gelme ya da tık; sunucu <c>touched = true</c> yazar, etiket söner).</summary>
        public event Action<NewItemData> NewItemTouched;

        /// <summary>
        /// Defter kipi (defter açıkken; sunucu açar ve kapatır): kart, sabit hedef, konu satırı, dosya, KAYIPLAR, hurda ve deste
        /// çarpıştırıcıları kapanır; <see cref="boardFace"/> (Pano yüzü), MASAYA DÖN levhası ve defterin kendi (sayfa) çarpıştırıcıları
        /// açık kalır. Defter dışına tık böylece yalnız pano yüzüne düşer; sunucu defteri kapatır (§1.12).
        /// </summary>
        public void SetBookMode(bool on)
        {
            if (bookMode == on) return;
            bookMode = on;
            ApplyInteractivity();
        }

        Transform Frame => model != null ? model.transform : transform;
        bool Owned(BoardCardView c) => c != null && drag != null && drag.Owns(c);

        // ------------------------------------------------------------------ yaşam döngüsü

        void Awake()
        {
            EnsureDelegates();
            if (topics != null && topics.Tag == null) topics.Tag = hoverTag;
            if (folder != null && folder.Tag == null) folder.Tag = hoverTag;
            if (lostEnvelope != null && lostEnvelope.Tag == null) lostEnvelope.Tag = hoverTag;
            if (book != null && book.Tag == null) book.Tag = hoverTag;
            EnsurePools();
        }

        void Start()
        {
            if (hurdaLid != null && hurda == null) hurdaLid.Set(0);
            ApplyInteractivity();
        }

        void OnEnable()
        {
            if (backTarget != null) backTarget.Hovered += OnBackHovered;
            if (deckView != null) deckView.Sound += Forward;
            if (hurda != null) hurda.Sound += Forward;
            HookSubViews();
        }

        void OnDisable()
        {
            if (backTarget != null) backTarget.Hovered -= OnBackHovered;
            if (deckView != null) deckView.Sound -= Forward;
            if (hurda != null) hurda.Sound -= Forward;
            UnhookSubViews();
        }

        /// <summary>
        /// Aşama 5 alt görünümlerinin olayları (depo kayışı ve sesi, defter sesi). Edit modda <c>OnEnable</c> çalışmadığından ilk
        /// çizimde de bağlanır; iki kez bağlanmaz.
        /// </summary>
        void HookSubViews()
        {
            if (subViewsHooked) return;
            subViewsHooked = true;
            if (store != null) { store.Sound += Forward; store.SlideFinished += OnSlideFinished; }
            if (book != null) book.Sound += Forward;
        }

        void UnhookSubViews()
        {
            if (!subViewsHooked) return;
            subViewsHooked = false;
            if (store != null) { store.Sound -= Forward; store.SlideFinished -= OnSlideFinished; }
            if (book != null) book.Sound -= Forward;
        }

        void Forward(BoardSound s) => Sound?.Invoke(s);

        /// <summary>Depo ve iş emri geri çağrıları (Render, Awake'ten önce çağrılsa da hazır olsun diye tembel).</summary>
        void EnsureDelegates()
        {
            if (storeItemCard == null) storeItemCard = StoreItemCard;
            if (chipPoseLookup == null) chipPoseLookup = LookupChipPose;
            HookSubViews();
        }

        /// <summary>Kartın sözcükleri ve dili anlık görüntüden (bağlamadan önce).</summary>
        static void Prep(BoardCardView c, BoardSnapshot s)
        {
            c.Words = s.Words;
            c.English = s.English;
        }

        void EnsurePools()
        {
            if (poolsReady || cardRoot == null) return;
            poolsReady = true;
            if (personPrefab != null) for (int i = 0; i < PersonPool; i++) freePersons.Add(Create(personPrefab));
            if (itemPrefab != null) for (int i = 0; i < ItemPool; i++) freeItems.Add(Create(itemPrefab));
            if (chipPrefab != null) for (int i = 0; i < ChipPool; i++) freeChips.Add(Create(chipPrefab));
            if (materialPrefab != null) for (int i = 0; i < materials.Length; i++) materials[i] = Create(materialPrefab);
            if (recipePrefab != null) for (int i = 0; i < RecipePool; i++) freeRecipes.Add(Create(recipePrefab));
        }

        BoardCardView Create(BoardCardView prefab)
        {
            var c = Instantiate(prefab, cardRoot, false);
            c.name = prefab.name + " " + (++created).ToString("00");
            c.Controller = drag;
            if (c.Kind == BoardCardKind.Person) c.Portraits = portraits;
            c.Touched += OnCardTouched;
            c.SetInteractive(false);
            c.SetVisible(false);
            return c;
        }

        // ------------------------------------------------------------------ Render

        /// <summary>
        /// Anlık görüntüyü çizer. <paramref name="animate"/> yanlışsa (ilk çizim, kayıttan devam, dil değişimi) her şey
        /// anında yerine oturur; doğruysa yer değiştiren kartlar ve parçalar süresiyle hareket eder. Sürükleme
        /// denetleyicisinin sahiplendiği kartlar atlanır; o kartların işi bitince düzen kendiliğinden yeniden kurulur.
        /// </summary>
        public void Render(BoardSnapshot s, bool animate)
        {
            if (s == null) return;
            EnsurePools();
            EnsureDelegates();
            last = s;
            bool anim = animate && rendered;
            pendingRelayout = false;
            skipped.Clear();
            fresh.Clear();
            placed.Clear();
            clickable.Clear();

            RenderTexts(s, anim);
            RenderPersons(s, anim);
            RenderItems(s, anim);
            RenderStore(s, anim);
            RenderChips(s, anim);
            RenderOrder(s, anim);
            if (book != null) { book.Locked = s.Locked; book.RecipesDraggable = s.RecipesDraggable; }
            if (topics != null && s.Topics != null) topics.SetRows(s.Topics.Labels, s.Topics.Mastered, s.Topics.RingRow, anim);
            RenderLock(s.Locked, anim);

            rendered = true;
            RebuildActive();
            ApplyInteractivity();
        }

        public BoardCardView CardFor(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (persons.TryGetValue(id, out var c) || items.TryGetValue(id, out c) || chips.TryGetValue(id, out c)) return c;
            if (formRecipe != null && formRecipe.Id == id) return formRecipe;
            for (int i = 0; i < recipeCopies.Count; i++) if (recipeCopies[i] != null && recipeCopies[i].Id == id) return recipeCopies[i];
            int k = Array.IndexOf(BoardLayout.MaterialKinds, id);
            if (k >= 0) return materials[k];
            for (int i = 0; i < materials.Length; i++) if (materials[i] != null && materials[i].Id == id) return materials[i];
            return null;
        }

        // ------------------------------------------------------------------ kişiler

        void RenderPersons(BoardSnapshot s, bool anim)
        {
            seen.Clear();
            foreach (var p in s.People)
            {
                if (p == null || string.IsNullOrEmpty(p.Id)) continue;
                if (p.Lost)
                {
                    // Ölen kişi: yalnız zaten gösterilen kartta KAYIP durumu yazılır (yeni kart açılmaz); kart aşağıda emekliye ayrılır.
                    if (persons.TryGetValue(p.Id, out var dead)) { Prep(dead, s); dead.Bind(p); }
                    continue;
                }
                var card = Acquire(persons, freePersons, personPrefab, p.Id, true, out bool isNew);
                if (card == null) continue;
                seen.Add(p.Id);
                Prep(card, s);
                card.Bind(p);
                if (isNew) fresh.Add(card);
            }

            gone.Clear();
            foreach (var kv in persons) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
            for (int i = 0; i < gone.Count; i++) RetirePerson(gone[i], s, anim);

            // Cepler
            float pocketZ = BoardLayout.CardZ(BoardLayout.PocketLevel);
            foreach (var kv in s.PocketOccupant)
            {
                if (kv.Value == null || !persons.TryGetValue(kv.Value, out var card)) continue;
                if (BoardLayout.SlotIndexOfJob(kv.Key) < 0) continue;
                placed.Add(card);
                clickable.Add(card);
                if (Owned(card)) { Skip(card); continue; }
                var target = BoardLayout.MmToLocal(BoardLayout.CardCentre(kv.Key), pocketZ);
                card.SetMagnetVisible(true);
                if (fresh.Contains(card) || !anim) card.Place(target, 0);
                else if (!BoardViewUtil.AtTarget(card, target, 0))
                {
                    // Neredeyse yerindeyse (denetleyicinin bıraktığı yer) yalnız oturur; uzaktaysa takas yayı (6 mm).
                    if ((card.transform.localPosition - target).sqrMagnitude < SettleNearSqr)
                        card.MoveTo(target, 0, BoardViewUtil.Num("motion.board.settleS", 0.06f), CardEase.EaseIn);
                    else
                        card.ArcTo(target, 0, BoardViewUtil.Num("motion.board.swapS", 0.32f), BoardViewUtil.Num("cards.swapLiftMm", 6f));
                }
                card.SetVisible(true);
            }

            // Boşta destesi (0 = üst kart)
            deckList.Clear();
            deckLower.Clear();
            int n = s.DeckOrder.Count;
            for (int j = 0; j < n; j++)
            {
                if (s.DeckOrder[j] == null || !persons.TryGetValue(s.DeckOrder[j], out var card)) continue;
                if (placed.Contains(card)) continue;
                placed.Add(card);
                clickable.Add(card);
                if (Owned(card)) { Skip(card); continue; }
                if (fresh.Contains(card))
                {
                    int at = deckList.Count;
                    card.Place(BoardLayout.MmToLocal(BoardLayout.DeckClosed(at), BoardLayout.CardZ(BoardLayout.DeckClosedLevel(at, n))), 0);
                }
                card.SetMagnetVisible(true);
                card.SetVisible(true);
                if (deckList.Count > 0) deckLower.Add(card);
                deckList.Add(card);
            }
            if (deckView != null) deckView.Layout(deckList, anim);
            else LayoutDeckFallback(anim);

            // Ne cepte ne destede olan (olmamalı): gizli
            foreach (var kv in persons)
            {
                var card = kv.Value;
                if (placed.Contains(card) || Owned(card)) continue;
                card.SetVisible(false);
            }
        }

        void LayoutDeckFallback(bool anim)
        {
            int n = deckList.Count;
            for (int j = 0; j < n; j++)
            {
                var target = BoardLayout.MmToLocal(BoardLayout.DeckClosed(j), BoardLayout.CardZ(BoardLayout.DeckClosedLevel(j, n)));
                BoardViewUtil.Pose(deckList[j], target, 0, anim, BoardLayout.DeckPerCardS, CardEase.EaseInOut);
            }
        }

        void RetirePerson(string id, BoardSnapshot s, bool anim)
        {
            var card = persons[id];
            persons.Remove(id);
            if (Owned(card)) { pendingRelease.Add(card); return; }
            // KAYIP: pano görünürken (K4) zarfa gider, sonra bırakılır; değilse hemen bırakılır (eleştiri #22).
            if (IsLost(s, id) && anim && interactive && lostEnvelope != null && lostEnvelope.Visible && cardRoot != null)
            {
                var to = cardRoot.InverseTransformPoint(lostEnvelope.SlotWorld);
                card.SetInteractive(false);
                card.SetMagnetVisible(false);
                card.ArcTo(to, 0, BoardViewUtil.Num("motion.board.lostTransitS", 0.45f), BoardViewUtil.Num("cards.heldLiftMm", 10f));
                exiting.Add(card);
                return;
            }
            Release(card);
        }

        static bool IsLost(BoardSnapshot s, string id)
        {
            foreach (var l in s.Lost) if (l != null && l.Id == id) return true;
            foreach (var p in s.People) if (p != null && p.Id == id && p.Lost) return true;
            return false;
        }

        // ------------------------------------------------------------------ eşyalar

        void RenderItems(BoardSnapshot s, bool anim)
        {
            seen.Clear();
            foreach (var it in s.Items)
            {
                if (it == null || string.IsNullOrEmpty(it.Id)) continue;
                var card = Acquire(items, freeItems, itemPrefab, it.Id, true, out bool isNew);
                if (card == null) continue;
                seen.Add(it.Id);
                Prep(card, s);
                card.Bind(it);
                if (isNew) fresh.Add(card);
            }
            gone.Clear();
            foreach (var kv in items) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
            for (int i = 0; i < gone.Count; i++) RetireSimple(items, gone[i]);

            // Sahiplerine göre, kit → zırh → yardımcı (sıkıştırılmış)
            for (int i = 0; i < follows.Count; i++) { follows[i].Owner = null; follows[i].Items.Clear(); follows[i].Active = false; followPool.Push(follows[i]); }
            follows.Clear();
            followByOwner.Clear();
            for (int pass = 0; pass < SlotOrder.Length; pass++)
            {
                foreach (var it in s.Items)
                {
                    if (it == null || it.Slot != SlotOrder[pass] || string.IsNullOrEmpty(it.OwnerId)) continue;
                    if (!items.TryGetValue(it.Id, out var card)) continue;
                    if (!persons.TryGetValue(it.OwnerId, out var owner) || !placed.Contains(owner))
                    {
                        if (!Owned(card)) card.SetVisible(false);
                        continue;
                    }
                    if (!followByOwner.TryGetValue(it.OwnerId, out var entry))
                    {
                        entry = followPool.Count > 0 ? followPool.Pop() : new FollowEntry();
                        entry.Owner = owner;
                        followByOwner[it.OwnerId] = entry;
                        follows.Add(entry);
                    }
                    entry.Items.Add(card);
                }
            }

            bool tucked = false;
            for (int e = 0; e < follows.Count; e++)
            {
                var entry = follows[e];
                bool ownerBusy = Owned(entry.Owner) || entry.Owner.Moving;
                for (int nIdx = 0; nIdx < entry.Items.Count; nIdx++)
                {
                    var card = entry.Items[nIdx];
                    clickable.Add(card);
                    if (Owned(card)) { Skip(card); continue; }
                    card.SetMagnetVisible(false);
                    ItemPose(entry.Owner, nIdx, out var pos, out float angle);
                    if (fresh.Contains(card) || ownerBusy) card.Place(pos, angle);
                    else if (BoardViewUtil.Pose(card, pos, angle, anim, BoardViewUtil.Num("motion.board.itemTuckS", 0.15f), CardEase.EaseOut)) tucked = true;
                    card.SetVisible(true);
                }
                entry.Active = ownerBusy;
            }
            if (tucked) Sound?.Invoke(BoardSound.Tuck);
        }

        /// <summary>Sahibin şu anki pozuna göre n. eşyanın pozu (sahip merkezinin 49 + 14·n mm altında, arkasında).</summary>
        static void ItemPose(BoardCardView owner, int n, out Vector3 pos, out float angle)
        {
            var t = owner.transform;
            angle = t.localEulerAngles.z;
            Vector2 off = BoardLayout.ItemOffsetMm(n);
            Vector3 d = Quaternion.Euler(0, 0, angle) * new Vector3(off.x / 1000f, -off.y / 1000f, 0);
            float dz = BoardLayout.CardZ(BoardLayout.ItemLevel(n)) - BoardLayout.CardZ(BoardLayout.PocketLevel);
            pos = t.localPosition + d + new Vector3(0, 0, dz);
        }

        /// <summary>
        /// Sahibin eşyalarını sahibin şu anki pozuna oturtur (sürükleme denetleyicisi tutulan kartın eşyaları için her karede
        /// çağırabilir; çağırmasa da hareket eden ya da tutulan sahiplerin eşyaları <c>LateUpdate</c>'te izlenir).
        /// </summary>
        public void SnapItemsToOwner(BoardCardView owner)
        {
            if (owner == null || string.IsNullOrEmpty(owner.Id)) return;
            if (followByOwner.TryGetValue(owner.Id, out var entry) && entry.Owner == owner) PlaceItems(entry, Owned(owner));
        }

        void PlaceItems(FollowEntry e, bool disableInput)
        {
            for (int n = 0; n < e.Items.Count; n++)
            {
                var card = e.Items[n];
                if (card == null || Owned(card)) continue;
                ItemPose(e.Owner, n, out var pos, out float angle);
                card.Place(pos, angle);
                if (disableInput) card.SetInteractive(false);
            }
        }

        void FollowOwners()
        {
            for (int i = 0; i < follows.Count; i++)
            {
                var e = follows[i];
                if (e.Owner == null) continue;
                bool held = Owned(e.Owner);
                bool act = held || e.Owner.Moving;
                if (act || e.Active) PlaceItems(e, held);
                if (e.Active && !act)
                    for (int n = 0; n < e.Items.Count; n++)
                    {
                        var c = e.Items[n];
                        if (c != null && !Owned(c)) c.SetInteractive(interactive && clickable.Contains(c));
                    }
                e.Active = act;
            }
        }

        // ------------------------------------------------------------------ depo, fişler

        void RenderStore(BoardSnapshot s, bool anim)
        {
            UpdateNewItem(s);
            // Depodaki eşyalar: yığın başına en çok üçü çizilir, en yenisi (TopIds sonu) üstte ve sürüklenebilir.
            seen.Clear();
            for (int k = 0; k < s.Kits.Length; k++)
            {
                var kit = s.Kits[k];
                if (kit == null) continue;
                int m = kit.TopIds.Count;
                for (int d = 0; d < m; d++)
                {
                    string id = kit.TopIds[d];
                    if (id == null || !items.TryGetValue(id, out var card)) continue;
                    seen.Add(id);
                    if (d == m - 1) clickable.Add(card);
                    if (Owned(card)) { Skip(card); continue; }
                    if (HeldForSlide(id)) continue;   // yeni ürün: TARİF kutusunda bekler ya da kayar (RenderNewItem)
                    if (fresh.Contains(card))
                        card.Place(BoardLayout.MmToLocal(BoardLayout.KitCard(k, m - 1 - d), BoardLayout.CardZ(BoardLayout.KitLevel(m - 1 - d))), 0);
                }
            }
            foreach (var it in s.Items)
            {
                if (it == null || !string.IsNullOrEmpty(it.OwnerId) || seen.Contains(it.Id)) continue;
                if (items.TryGetValue(it.Id, out var card) && !Owned(card)) card.SetVisible(false);
            }

            for (int i = 0; i < materials.Length; i++)
            {
                var c = materials[i];
                if (c != null) { clickable.Add(c); Prep(c, s); }
                materialBuffer[i] = Owned(c) ? null : c;
                if (c != null && materialBuffer[i] == null) Skip(c);
            }
            if (store != null) store.Layout(s, storeItemCard, materialBuffer, anim);
            RenderNewItem(s, anim);
        }

        BoardCardView StoreItemCard(string id)
        {
            if (id == null || !items.TryGetValue(id, out var c) || Owned(c) || HeldForSlide(id)) return null;
            return c;
        }

        // ------------------------------------------------------------------ yeni ürün: kayış ve YENİ (Aşama 5 · R18d, D9-A)

        bool HeldForSlide(string id) =>
            newItem != null && id == newItem.ItemId && (slide == SlideState.Waiting || slide == SlideState.Sliding);

        /// <summary>
        /// Anlık görüntünün yeni ürününü izler (çizimden önce): yeni kimlik ya da gün gelince durum sıfırlanır; kayışın oynayıp
        /// oynamadığını sunum durumu söyler (<see cref="NewItemPending"/>). Depo kayışı kendi kesildiyse (devre dışı kalma) bitmiş sayılır.
        /// </summary>
        void UpdateNewItem(BoardSnapshot s)
        {
            var d = s.NewItem;
            string key = d != null && !string.IsNullOrEmpty(d.ItemId) ? d.ItemId + "@" + d.Day.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
            if (key != newItemKey)
            {
                if (slide == SlideState.Sliding && store != null && newCard != null && store.Sliding == newCard) store.CancelSlide();
                newItemKey = key;
                newItem = key != null ? d : null;
                slide = SlideState.None;
                newTouched = false;
                newCard = null;
            }
            else if (key != null) newItem = d;
            if (newItem == null) return;
            if (slide == SlideState.None)
                slide = NewItemPending != null && NewItemPending(newItem) ? SlideState.Waiting : SlideState.Done;
            else if (slide == SlideState.Waiting && (NewItemPending == null || !NewItemPending(newItem)))
                slide = SlideState.Done;
            else if (slide == SlideState.Sliding && (store == null || store.Sliding == null))
            {
                slide = SlideState.Done;   // kayış kesildi (depo devre dışı kaldı): bitmiş say
                NewItemSlid?.Invoke(newItem);
            }
        }

        /// <summary>
        /// Yeni ürünün kartı: K4'e gelmeden TARİF kutusunda bekler (kayıttan devam, K1'den bakış); K4'teyken kayış başlar. YENİ
        /// levhası dokunulana dek kartın üstünde.
        /// </summary>
        void RenderNewItem(BoardSnapshot s, bool anim)
        {
            BoardCardView card = null;
            if (newItem != null && items.TryGetValue(newItem.ItemId, out var c) && !Owned(c) && IsStored(s, newItem.ItemId)) card = c;
            newCard = card;
            if (newItem != null && card == null && slide != SlideState.Done)
            {
                if (slide == SlideState.Sliding && store != null && store.Sliding != null) store.CancelSlide();
                slide = SlideState.Done;   // eşya depoda değil (sahibine verildi, tutuluyor ya da tüketildi)
            }

            if (card != null && slide == SlideState.Waiting)
            {
                if (interactive) StartSlide(card);
                else
                {
                    card.SetMagnetVisible(true);
                    card.Place(WaitingPose(), 0);
                    card.SetVisible(true);
                }
            }
            else if (card != null && slide == SlideState.Sliding) card.SetVisible(true);

            if (store == null) return;
            if (store.NewTag != null) store.NewTag.SetText(Corrigible.Core.Loc.T("YENİ"));
            bool tagged = card != null && !newTouched && (NewItemTagged == null || NewItemTagged(newItem));
            store.ShowNew(card, tagged, anim);
        }

        static bool IsStored(BoardSnapshot s, string id)
        {
            var it = s.Item(id);
            return it != null && string.IsNullOrEmpty(it.OwnerId);
        }

        /// <summary>Bekleyen ürünün yeri: TARİF kutusunun ortası, tarif kartının bir seviye önünde.</summary>
        Vector3 WaitingPose()
        {
            var mm = order != null ? order.RecipeHomeMm(cardRoot) : BoardLayout.Recipe;
            return BoardLayout.MmToLocal(mm, BoardLayout.CardZ(WorkOrderView.RecipeLevel + 1f));
        }

        void StartSlide(BoardCardView card)
        {
            if (card == null || store == null) { slide = SlideState.Done; return; }
            slide = SlideState.Sliding;
            store.SlideIn(card, WaitingPose(), BoardViewUtil.Num("motion.board.productToStoreS", 0.3f));
        }

        void OnSlideFinished(BoardCardView card)
        {
            if (newItem == null || card != newCard || slide != SlideState.Sliding) return;
            slide = SlideState.Done;
            NewItemSlid?.Invoke(newItem);
        }

        void OnCardTouched(BoardCardView c)
        {
            if (c == null || newItem == null || c != newCard || newTouched || !interactive) return;
            newTouched = true;
            if (store != null) store.ShowNew(c, false, true);
            NewItemTouched?.Invoke(newItem);
        }

        // ------------------------------------------------------------------ iş emri: form ve tarif kartı (Aşama 5 · R18c)

        /// <summary>
        /// Formun yazıları ve TARİF kutusundaki tarif kartı. Kart yoksa form kartı havuza döner. [B] Kabul edilen bırakmanın
        /// kopyası (aynı tarif) form kartı olur: oyuncunun bıraktığı yerde kalır (sunucu pozu önceden yazar), sıçramaz.
        /// </summary>
        void RenderOrder(BoardSnapshot s, bool anim)
        {
            var o = s.Order;
            if (order != null && o != null) order.SetSheet(o);
            var d = o != null ? o.Card : null;
            if (d == null)
            {
                formFromCopy = false;
                if (formRecipe != null)
                {
                    var old = formRecipe;
                    formRecipe = null;
                    if (Owned(old)) { pendingRelease.Add(old); Skip(old); }
                    else Release(old);
                }
                return;
            }

            var card = formRecipe;
            bool isNew = false;
            if (card == null || card.Id != d.Id)
            {
                var copy = FindCopy(d.RecipeId);
                if (copy != null)
                {
                    recipeCopies.Remove(copy);
                    if (card != null && card != copy) { if (Owned(card)) pendingRelease.Add(card); else Release(card); }
                    card = copy;
                    formFromCopy = true;
                }
                else if (card == null)
                {
                    if (freeRecipes.Count > 0) { card = freeRecipes[freeRecipes.Count - 1]; freeRecipes.RemoveAt(freeRecipes.Count - 1); }
                    else if (recipePrefab != null && cardRoot != null) card = Create(recipePrefab);
                    if (card == null) return;
                    isNew = true;
                }
                formRecipe = card;
            }
            clickable.Add(card);
            if (Owned(card)) { Skip(card); return; }
            Prep(card, s);
            card.Bind(d);
            string key = !string.IsNullOrEmpty(o.OrderId) ? o.OrderId : d.Id;
            Vector3? pose = RecipePose != null ? RecipePose(key) : null;
            if (!pose.HasValue && formFromCopy && card.gameObject.activeSelf)
            {
                // Kabul edilen bırakma: emrin kimliği komuttan sonra belli olur, sunucu pozu henüz yazmamış olabilir. Kart
                // bırakıldığı yerde (iş emri alanına kırpılarak) kalır; sunucu OrderRecipeCard'ın pozunu sonra saklar.
                var here = BoardLayout.LocalToMm(card.Moving ? card.Target : card.transform.localPosition);
                float angle = card.Moving ? card.TargetAngle : card.transform.localEulerAngles.z;
                pose = new Vector3(here.x, here.y, Mathf.DeltaAngle(0, angle));
            }
            formFromCopy = false;
            order?.LayoutRecipe(card, pose, anim && !isNew);
        }

        BoardCardView FindCopy(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return null;
            for (int i = recipeCopies.Count - 1; i >= 0; i--)
                if (recipeCopies[i] != null && recipeCopies[i].Id == recipeId) return recipeCopies[i];
            return null;
        }

        Vector3? LookupChipPose(string id) => ChipPose != null ? ChipPose(id) : null;

        void RenderChips(BoardSnapshot s, bool anim)
        {
            seen.Clear();
            chipList.Clear();
            int drawn = Mathf.Min(s.Chips.Count, ChipPool);
            RekeyChips(s, drawn);
            for (int i = 0; i < drawn; i++)
            {
                var data = s.Chips[i];
                BoardCardView card = null;
                if (data != null && !string.IsNullOrEmpty(data.Id))
                {
                    card = Acquire(chips, freeChips, chipPrefab, data.Id, true, out _);
                    if (card != null)
                    {
                        seen.Add(data.Id);
                        Prep(card, s);
                        card.Bind(data);
                        clickable.Add(card);
                        if (Owned(card)) { Skip(card); card = null; }
                    }
                }
                chipList.Add(card);   // sahiplenilen fiş null: sıra (kendiliğinden yer) korunur
            }
            gone.Clear();
            foreach (var kv in chips) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
            for (int i = 0; i < gone.Count; i++) RetireSimple(chips, gone[i]);
            if (order != null) order.Layout(chipList, chipPoseLookup, anim);
        }

        /// <summary>
        /// Fiş kimlikleri tür içi sıradır (<c>chip:tür:k</c>). Bir türün fiş sayısı azaldıysa (fiş depoya geri alındı) fazlalık önce
        /// sürükleme denetleyicisinin tuttuğu fişten, sonra en yüksek sıradan düşülür ve kalan kartlar 0'dan yeniden numaralanır:
        /// geri alınan fiş iş emrine geri uçmaz, kalanlar yerinde durur (sunucu kayıtlı yerleri <c>BoardUiState.RemoveChip</c> ile
        /// aynı biçimde kaydırır). Sayı artınca ya da aynı kalınca hiçbir şey yapılmaz.
        /// </summary>
        void RekeyChips(BoardSnapshot s, int drawn)
        {
            var kinds = BoardLayout.MaterialKinds;
            Array.Clear(chipWanted, 0, chipWanted.Length);
            for (int i = 0; i < drawn; i++)
            {
                var d = s.Chips[i];
                int k = d != null ? Array.IndexOf(kinds, d.Kind) : -1;
                if (k >= 0) chipWanted[k]++;
            }
            for (int k = 0; k < kinds.Length; k++)
            {
                kindCards.Clear();
                foreach (var kv in chips)
                    if (BoardViewUtil.TryParseChip(kv.Key, out var kind, out _) && kind == kinds[k]) kindCards.Add(kv.Value);
                int surplus = kindCards.Count - chipWanted[k];
                if (surplus <= 0) continue;
                kindCards.Sort(ByChipIndex);
                for (int pass = 0; pass < 2 && surplus > 0; pass++)
                    for (int i = kindCards.Count - 1; i >= 0 && surplus > 0; i--)
                    {
                        var c = kindCards[i];
                        bool held = Owned(c);
                        if (pass == 0 && !held) continue;
                        chips.Remove(c.Id);
                        if (held) pendingRelease.Add(c); else Release(c);
                        kindCards.RemoveAt(i);
                        surplus--;
                    }
                for (int i = 0; i < kindCards.Count; i++) chips.Remove(kindCards[i].Id);
                for (int i = 0; i < kindCards.Count; i++) chips[BoardViewUtil.ChipId(kinds[k], i)] = kindCards[i];   // Bind yeni kimliği yazar
            }
        }

        static int CompareChipIndex(BoardCardView a, BoardCardView b)
        {
            BoardViewUtil.TryParseChip(a.Id, out _, out int ia);
            BoardViewUtil.TryParseChip(b.Id, out _, out int ib);
            return ia.CompareTo(ib);
        }

        // ------------------------------------------------------------------ yazılar, dosya, zarf

        void RenderTexts(BoardSnapshot s, bool anim)
        {
            RenderHeader(s.Header, anim);
            BoardViewUtil.SetText(lockTagLine2, s.LockReason);
            BoardViewUtil.SetText(hurdaLine2, s.HurdaPlateLine);
            if (lostEnvelope != null)
            {
                lostEnvelope.SetCover(s.LostCover);
                if (lostEnvelope.IsOpen) lostEnvelope.Open(s.Lost);
            }
            if (folder != null)
            {
                // İpucu biçimi: seçili kişi yok (eşya seçiliyken de SelectedId dolu ama dosya yok).
                folder.SetCover(s.FolderCoverLine, s.Folder == null);
                if (folder.IsOpen)
                {
                    if (s.Folder == null) folder.Close();
                    else folder.Open(s.Folder);
                }
            }
        }

        void RenderHeader(string text, bool anim)
        {
            if (header == null) return;
            text = text ?? "";
            if (pendingHeader != null) { header.text = pendingHeader; pendingHeader = null; }
            string old = header.text ?? "";
            if (old == text) return;
            if (!anim || marks == null || old.Length == 0) { header.text = text; return; }
            float seconds = BoardViewUtil.Num("motion.board.chalkRewriteS", 0.2f);
            marks.Smudge(ChangedRectMm(old, text), seconds);
            pendingHeader = text;
            headerSwapAt = Time.unscaledTime + seconds * 0.5f;
        }

        /// <summary>Eski başlığın değişen parçasının (ortak baş ve son dışı) pano mm dikdörtgeni; ölçülemezse arduvazın tamamı.</summary>
        Rect ChangedRectMm(string old, string now)
        {
            var slate = BoardLayout.Slate;
            int p = 0, max = Mathf.Min(old.Length, now.Length);
            while (p < max && old[p] == now[p]) p++;
            int q = 0;
            while (q < old.Length - p && q < now.Length - p && old[old.Length - 1 - q] == now[now.Length - 1 - q]) q++;
            int first = p, lastIdx = old.Length - 1 - q;
            if (lastIdx < first) { first = Mathf.Max(0, p - 1); lastIdx = Mathf.Min(old.Length - 1, p); }

            header.ForceMeshUpdate();   // bekleyen yazı az önce konduysa textInfo eskidir
            var info = header.textInfo;
            if (info == null || info.characterCount == 0) return slate;
            var frame = Frame;
            float xMin = float.MaxValue, xMax = float.MinValue;
            for (int i = 0; i < info.characterCount; i++)
            {
                var ci = info.characterInfo[i];
                if (ci.index < first || ci.index > lastIdx) continue;
                Vector2 a = BoardLayout.LocalToMm(frame.InverseTransformPoint(header.transform.TransformPoint(ci.bottomLeft)));
                Vector2 b = BoardLayout.LocalToMm(frame.InverseTransformPoint(header.transform.TransformPoint(ci.topRight)));
                xMin = Mathf.Min(xMin, Mathf.Min(a.x, b.x));
                xMax = Mathf.Max(xMax, Mathf.Max(a.x, b.x));
            }
            if (xMin > xMax) return slate;
            const float pad = 1.5f;
            xMin = Mathf.Max(slate.xMin, xMin - pad);
            xMax = Mathf.Min(slate.xMax, xMax + pad);
            if (xMax <= xMin) return slate;
            return new Rect(xMin, slate.y, xMax - xMin, slate.height);
        }

        // ------------------------------------------------------------------ kilit, kapak

        void RenderLock(bool locked, bool anim)
        {
            bool first = !lockKnown;
            lockKnown = true;
            if (!first && locked == lockedTarget) return;
            lockedTarget = locked;
            if (first) { SnapLock(locked); lockSeen = locked; return; }
            if (anim && interactive) { AnimateLock(locked); lockSeen = locked; }
            else
            {
                SnapLock(locked);
                if (interactive) lockSeen = locked;   // uzaktayken değiştiyse K4'e varınca bir kez oynar
            }
        }

        void SnapLock(bool locked)
        {
            flapActive = rodActive = false;
            if (flap != null) flap.Set(locked ? 180f : 0f);
            rodOffset = locked ? 0f : RodOut;
            ApplyRod();
            if (lockRod != null) lockRod.SetVisible(locked);
        }

        void AnimateLock(bool locked)
        {
            float now = Time.unscaledTime;
            if (flap != null)
            {
                flapFrom = flap.Value; flapTo = locked ? 180f : 0f; flapStart = now;
                flapSeconds = BoardViewUtil.Num("motion.board.flapS", 0.3f);
                flapActive = !Mathf.Approximately(flapFrom, flapTo);
                if (flapActive) Sound?.Invoke(BoardSound.Flap);
            }
            if (lockRod != null)
            {
                bool wasVisible = lockRod.gameObject.activeSelf;
                if (locked && !wasVisible) rodOffset = RodOut;
                lockRod.SetVisible(true);
                rodFrom = rodOffset; rodTo = locked ? 0f : RodOut; rodStart = now;
                rodSeconds = BoardViewUtil.Num("motion.board.lockRodS", 0.4f);
                rodHideAtEnd = !locked;
                rodSoundAtEnd = locked;
                rodActive = true;
                if (!locked) Sound?.Invoke(BoardSound.Padlock);
                ApplyRod();
            }
        }

        void ReplayLock()
        {
            if (!lockKnown || lockSeen == lockedTarget) return;
            SnapLock(lockSeen);
            AnimateLock(lockedTarget);
            lockSeen = lockedTarget;
        }

        static float RodOut => BoardViewUtil.Num("board.lock.rodOutM", 1.195f);

        void ApplyRod()
        {
            if (lockRod == null) return;
            lockRod.transform.localPosition = lockRod.PivotLocal + new Vector3(rodOffset, 0, 0);
        }

        // ------------------------------------------------------------------ girdi kapısı

        /// <summary>
        /// K4'e varınca (<c>rig.Current == Shelf && !rig.InTransition</c>) true: kart ve sabit hedef çarpıştırıcıları açılır,
        /// Pano erişimi kapanır; uzaktayken değişen kilit bir kez oynar. false: tersi; açık deste toplanır, dosya ve zarf kapanır.
        /// </summary>
        public void SetInteractive(bool on)
        {
            bool arrived = on && !interactive;
            interactive = on;
            if (!on)
            {
                if (deckView != null && deckView.IsOpen) deckView.Close(true);
                if (folder != null && folder.IsOpen) folder.Close();
                if (lostEnvelope != null && lostEnvelope.IsOpen) lostEnvelope.Close();
                // B6: açık defter kamera giderken animasyonla kapanır; defter kipi K4'e dönüşte kapalı başlar
                if (book != null && book.State != BookState.Closed && book.State != BookState.Closing) book.Close(true);
                bookMode = false;
            }
            if (book != null) book.SetInteractive(on);
            ApplyInteractivity();
            if (arrived)
            {
                ReplayLock();
                if (slide == SlideState.Waiting && newCard != null) StartSlide(newCard);
            }
        }

        /// <summary>Defter kipinde açık kalan sabit hedef: pano yüzü ve MASAYA DÖN levhası.</summary>
        bool KeptInBookMode(Collider c) => c == boardFace || (backTarget != null && c.gameObject == backTarget.gameObject);

        void ApplyInteractivity()
        {
            // Defter kipinde (§1.12) yalnız pano yüzü, MASAYA DÖN ve defterin kendi çarpıştırıcıları açık.
            bool live = interactive && !bookMode;
            if (boardEntry != null) boardEntry.enabled = !interactive;
            if (staticColliders != null)
                for (int i = 0; i < staticColliders.Length; i++)
                {
                    var col = staticColliders[i];
                    if (col != null) col.enabled = interactive && (!bookMode || KeptInBookMode(col));
                }
            if (boardFace != null) boardFace.enabled = interactive;
            if (hurda != null && hurda.Front != null) hurda.Front.enabled = live;
            if (topics != null) topics.SetInteractive(live);
            if (folder != null) folder.SetInteractive(live);
            if (lostEnvelope != null) lostEnvelope.SetInteractive(live);
            for (int i = 0; i < active.Count; i++)
            {
                var c = active[i];
                if (c == null || Owned(c)) continue;
                c.SetInteractive(live && clickable.Contains(c) && (deckOpenShown || !deckLower.Contains(c)));
            }
            for (int i = 0; i < follows.Count; i++)
                if (follows[i].Owner != null && Owned(follows[i].Owner))
                    for (int n = 0; n < follows[i].Items.Count; n++) if (follows[i].Items[n] != null) follows[i].Items[n].SetInteractive(false);
        }

        // ------------------------------------------------------------------ havuz yardımcıları

        BoardCardView Acquire(Dictionary<string, BoardCardView> live, List<BoardCardView> free, BoardCardView prefab, string id, bool grow, out bool isNew)
        {
            isNew = false;
            if (live.TryGetValue(id, out var c)) return c;
            c = Reclaim(id);
            if (c == null)
            {
                if (free.Count > 0) { c = free[free.Count - 1]; free.RemoveAt(free.Count - 1); }
                else if (grow && prefab != null && cardRoot != null) c = Create(prefab);
                if (c == null) return null;
                isNew = true;
            }
            live[id] = c;
            return c;
        }

        BoardCardView Reclaim(string id)
        {
            for (int i = pendingRelease.Count - 1; i >= 0; i--)
                if (pendingRelease[i] != null && pendingRelease[i].Id == id) { var c = pendingRelease[i]; pendingRelease.RemoveAt(i); return c; }
            for (int i = exiting.Count - 1; i >= 0; i--)
                if (exiting[i] != null && exiting[i].Id == id) { var c = exiting[i]; exiting.RemoveAt(i); return c; }
            return null;
        }

        void RetireSimple(Dictionary<string, BoardCardView> live, string id)
        {
            var card = live[id];
            live.Remove(id);
            if (Owned(card)) pendingRelease.Add(card);
            else Release(card);
        }

        void Release(BoardCardView c)
        {
            if (c == null) return;
            c.SetInteractive(false);
            c.SetVisible(false);
            c.Unbind();   // havuzdaki kartın kimliği ve yüz önbelleği silinir; sonraki bağlama her şeyi yazar
            switch (c.Kind)
            {
                case BoardCardKind.Person: if (!freePersons.Contains(c)) freePersons.Add(c); break;
                case BoardCardKind.Item: if (!freeItems.Contains(c)) freeItems.Add(c); break;
                case BoardCardKind.Chip: if (!freeChips.Contains(c)) freeChips.Add(c); break;
                case BoardCardKind.Recipe:
                    recipeCopies.Remove(c);
                    if (c == formRecipe) formRecipe = null;
                    if (!freeRecipes.Contains(c)) freeRecipes.Add(c);
                    break;
            }
        }

        void Skip(BoardCardView c)
        {
            pendingRelayout = true;
            if (!skipped.Contains(c)) skipped.Add(c);
        }

        void RebuildActive()
        {
            active.Clear();
            foreach (var c in persons.Values) active.Add(c);
            foreach (var c in items.Values) active.Add(c);
            for (int i = 0; i < materials.Length; i++) if (materials[i] != null) active.Add(materials[i]);
            foreach (var c in chips.Values) active.Add(c);
            if (formRecipe != null) active.Add(formRecipe);
        }

        bool DrainReleases()
        {
            bool changed = false;
            for (int i = pendingRelease.Count - 1; i >= 0; i--)
            {
                var c = pendingRelease[i];
                if (c != null && (Owned(c) || c.Moving)) continue;
                pendingRelease.RemoveAt(i);
                Release(c);
                changed = true;
            }
            for (int i = exiting.Count - 1; i >= 0; i--)
            {
                var c = exiting[i];
                if (c != null && c.Moving) continue;
                exiting.RemoveAt(i);
                Release(c);
                changed = true;
            }
            return changed;
        }

        bool AnySkippedOwned()
        {
            for (int i = 0; i < skipped.Count; i++) if (Owned(skipped[i])) return true;
            return false;
        }

        // ------------------------------------------------------------------ kare

        void Update()
        {
            float now = Time.unscaledTime;
            if (pendingHeader != null && now >= headerSwapAt && header != null) { header.text = pendingHeader; pendingHeader = null; }

            if (flapActive && flap != null)
            {
                float t = flapSeconds <= 0 ? 1 : Mathf.Clamp01((now - flapStart) / flapSeconds);
                flap.Set(Mathf.Lerp(flapFrom, flapTo, BoardViewUtil.InOut(t)));
                if (t >= 1) flapActive = false;
            }
            if (rodActive)
            {
                float t = rodSeconds <= 0 ? 1 : Mathf.Clamp01((now - rodStart) / rodSeconds);
                rodOffset = Mathf.Lerp(rodFrom, rodTo, BoardViewUtil.InOut(t));
                ApplyRod();
                if (t >= 1)
                {
                    rodActive = false;
                    if (rodHideAtEnd && lockRod != null) lockRod.SetVisible(false);
                    if (rodSoundAtEnd) Sound?.Invoke(BoardSound.Padlock);
                }
            }
            if (backActive && backSign != null)
            {
                float seconds = BoardViewUtil.Num("motion.board.cardLiftS", 0.08f);
                float t = seconds <= 0 ? 1 : Mathf.Clamp01((now - backStart) / seconds);
                backValue = Mathf.Lerp(backFrom, backTo, BoardViewUtil.Out(t));
                backSign.Set(backValue);
                if (t >= 1) backActive = false;
            }
        }

        void LateUpdate()
        {
            FollowOwners();
            bool changed = DrainReleases();

            if (lamp != null && boardLamp != null)
            {
                bool on = boardLamp.isActiveAndEnabled && boardLamp.intensity > 0f;
                if (!lampKnown || on != lampOn) { lampKnown = true; lampOn = on; lamp.SetSwap(on ? 0 : 1); }
            }

            bool open = deckView != null && deckView.IsOpen;
            if (open != deckOpenShown) { deckOpenShown = open; changed = true; }

            if (changed) { RebuildActive(); ApplyInteractivity(); }
            if (pendingRelayout && last != null && !AnySkippedOwned()) Render(last, true);
        }

        void OnBackHovered(bool on)
        {
            if (backSign == null) return;
            backFrom = backValue;
            backTo = on ? -BoardViewUtil.Num("cards.hoverLiftMm", 4f) / 1000f : 0f;
            backStart = Time.unscaledTime;
            backActive = true;
        }
    }

    /// <summary>Pano görünümlerinin ortak küçük yardımcıları (tema sayısı, yumuşatma, poz, metin).</summary>
    internal static class BoardViewUtil
    {
        public static float Num(string key, float fallback)
        {
            var t = SiginakTheme.Active;
            return t != null ? t.NumberOf(key, fallback) : fallback;
        }

        public static Color Col(string key, Color fallback)
        {
            var t = SiginakTheme.Active;
            if (t != null && t.TryGet(key, out var c)) return c;
            var sade = SadeTheme.Active;
            if (sade != null && sade.TryGet(key, out c)) return c;
            return fallback;
        }

        /// <summary>Kübik gir-çık.</summary>
        public static float InOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        /// <summary>Kübik çıkış.</summary>
        public static float Out(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return 1f - u * u * u;
        }

        const string ChipPrefix = "chip:";

        /// <summary>Fiş kimliği <c>"chip:tür:k"</c> (köprünün <c>BoardUiState.ChipId</c>'siyle aynı biçim).</summary>
        public static string ChipId(string kind, int index) => ChipPrefix + kind + ":" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary><c>"chip:tür:k"</c> kimliğini çözer; biçim tutmazsa false.</summary>
        public static bool TryParseChip(string id, out string kind, out int index)
        {
            kind = null; index = -1;
            if (string.IsNullOrEmpty(id) || !id.StartsWith(ChipPrefix, StringComparison.Ordinal)) return false;
            int colon = id.LastIndexOf(':');
            if (colon <= ChipPrefix.Length) return false;
            if (!int.TryParse(id.Substring(colon + 1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out index))
            {
                index = -1;
                return false;
            }
            kind = id.Substring(ChipPrefix.Length, colon - ChipPrefix.Length);
            return kind.Length > 0;
        }

        public static void SetText(TMP_Text t, string v)
        {
            if (t == null) return;
            v = v ?? "";
            if (t.text != v) t.text = v;
        }

        static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-10f;

        /// <summary>Kart bu pozda duruyor ya da oraya gidiyor mu.</summary>
        public static bool AtTarget(BoardCardView c, Vector3 local, float angle)
        {
            if (c.Moving) return Near(c.Target, local);
            return Near(c.transform.localPosition, local) && Mathf.Abs(Mathf.DeltaAngle(c.transform.localEulerAngles.z, angle)) < 0.05f;
        }

        /// <summary>
        /// Kartı poza götürür: canlandırma yoksa anında, varsa şu anki pozundan <paramref name="seconds"/> içinde. Zaten oradaysa
        /// ya da oraya gidiyorsa dokunmaz. Hareket başladıysa true.
        /// </summary>
        public static bool Pose(BoardCardView c, Vector3 local, float angle, bool animate, float seconds, CardEase ease)
        {
            if (c == null) return false;
            if (!animate || seconds <= 0)
            {
                if (c.Moving || !AtTarget(c, local, angle)) c.Place(local, angle);
                return false;
            }
            if (AtTarget(c, local, angle)) return false;
            c.MoveTo(local, angle, seconds, ease);
            return true;
        }
    }
}
