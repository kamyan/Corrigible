using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Panonun deposunda kancada asılı 3B tarif defteri (Revizyon 3 · Aşama 5, R18a; PLAN §1.4), <c>Planning board/Tarif defteri</c>
    /// kökünde. Ekranda düğme yok, arka plan karartılmaz; ses çalmaz, <see cref="Sound"/> yayar. Simülasyonu okumaz: sayfalar anlık
    /// görüntünün <see cref="BoardSnapshot.Recipes"/>'inden çizilir (<see cref="Bind"/>).
    /// <list type="bullet">
    /// <item>Kapalı defterin üzerine gelince kök 4 mm öne gelir (<c>recipeBook.hoverMm</c>, <c>motion.board.cardLiftS</c>; D10: öteleme,
    /// tasarımın "eğilir"i rapora sapma), 0,4 s sonra etiket "TARİF DEFTERİ · 6 tarif".</item>
    /// <item>Tık (B1): kök kancadan okuma pozuna (<c>recipeBook.readPoseLocal</c>, pano çerçevesinde) <c>bookForwardS</c> 0,35 s
    /// kübik ease-out; kapak 0,10 s sonra başlar, <c>bookCoverS</c> 0,25 s gir-çık (toplam 0,35 s, örtüşük).</item>
    /// <item>Sağ alt "sonraki ►" / sol alt "◄ önceki" köşesi ya da ←/→ (sunucu) yaprağı çevirir (B2/B3, <c>pageTurnS</c> 0,25 s): yaprak
    /// görünür, ön yüzü eski sağ sayfa, arka yüzü yeni sol sayfa; açılan sayfa çevirme başında, örtülen sayfa yaprak oturmadan
    /// (%90) yeni açılıma geçer. Çevirme sürerken tık yok sayılır (B4).</item>
    /// <item>Sayfalar dışına tık (kapak kenarı), Esc, K4'ten çıkış kapatır (B5/B6): kapak ilk 0,25 s'de, kök <c>bookCloseS</c> 0,3 s'de
    /// kancaya. Pano boşluğu tıkını sunucu (<c>BackgroundClicked</c>) kapatır.</item>
    /// <item>[B] Tarif kutusuna (ya da İŞ EMRİ ▶ sekmesine) basılı tutup sürükleme eşiğini geçince <see cref="RecipeLifted"/>; sunucu
    /// kopyayı denetçiye verir ve <see cref="Lower"/> der (B7: <c>bookDropS</c> 0,25 s alçak poz). Kilitli planda kart kalkmaz, kök
    /// 2 mm titrer + <see cref="BoardSound.Deny"/> (B10).</item>
    /// </list>
    /// Kapak ve yaprak modelin −178° / −176°'sine +θ yönünden (okuyucuya doğru) döner, uçta parçaya oturur: ters yön kapağı ve yaprağı
    /// defterin sayfa bloğunun içinden geçirirdi (<see cref="PersonFolderView"/>'in kapağıyla aynı kural). <c>PartDriver.AnimateTo</c>
    /// doğrusal olduğundan açılar her karede görünümden yazılır. Edit modda <c>Update</c> çalışmaz: geçişler <see cref="Advance"/> ile.
    /// Çarpıştırıcılar yalnız K4 etkileşimliyken açık: kapalıyken gövde (<c>closedHit</c>), açıkken iki sayfa.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RecipeBookView : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>Örtülen sayfanın yeni açılıma geçtiği çevirme oranı (yaprak sayfaya oturmadan hemen önce).</summary>
        public const float CoveredPageSwitchAt = 0.9f;

        [Tooltip("Defterin kökü (kanca ↔ okuma ↔ alçak pozlar bunun yerel pozudur). Boşsa bu nesne.")]
        [SerializeField] Transform root;
        [Tooltip("Planning board model kökü: pozlar bu çerçevede (model yereli) tanımlı.")]
        [SerializeField] Transform frame;
        [Tooltip("Modelin Cover (y 0…−178°) ve Leaf (y 0…−176°) parçaları.")]
        [SerializeField] PartDriver cover, leaf;
        [Tooltip("Sol ve sağ statik sayfa yüzleri; yaprağın ön ve arka yüzleri.")]
        [SerializeField] RecipeBookPage pageLeft, pageRight, leafFront, leafBack;
        [Tooltip("Kapalı defterin gövde çarpıştırıcısı; açık defterin iki sayfa çarpıştırıcısı.")]
        [SerializeField] BoxCollider closedHit, hitLeft, hitRight;
        [Tooltip("[B] Sayfalarda İŞ EMRİ ▶ sekmesi çizilir (D1-B; A'da çizilmez).")]
        [SerializeField] bool orderTab = true;

        /// <summary>Defterin ses olayları (açılış, sayfa, kapanış; [B] ret). Sunucu <c>CueOf</c> ile çalar.</summary>
        public event Action<BoardSound> Sound;
        /// <summary>[B] Tarif kutusundan kart kalktı: tarif kimliği, işaretçi olayı, sayfadaki kutunun dünya pozu.</summary>
        public event Action<string, PointerEventData, Pose> RecipeLifted;
        /// <summary>Açılım değişti (0–2); sunucu <c>BoardUiState.BookSpread</c>'e yazar.</summary>
        public event Action<int> SpreadChanged;
        /// <summary>Kapalı deftere tık (defter açılmaya başladıktan sonra; sunucu seçimi temizler vb.).</summary>
        public event Action Clicked;

        RecipeBookMotion motion;
        readonly List<RecipeData> recipes = new List<RecipeData>();
        BookWords words;
        string title, line;
        BookState state = BookState.Closed;
        int spread, turnFrom, turnTo;
        bool coveredSwitched, interactive, hovered, started;
        GameObject pressGo;

        public BookState State => state;
        public bool IsOpen => state != BookState.Closed && state != BookState.Closing;
        public bool Moving => motion != null && (motion.PoseMoving || motion.CoverMoving || motion.LeafMoving);
        /// <summary>Açık (ya da açılınca gösterilecek) açılım, 0'dan.</summary>
        public int Spread => SpreadCount <= 0 ? 0 : Mathf.Clamp(spread, 0, SpreadCount - 1);
        public int SpreadCount => (recipes.Count + 1) / 2;
        /// <summary>Kilitli plan: defter okunur, tarif kalkmaz (B10).</summary>
        public bool Locked { get; set; }
        /// <summary>[B] Tarif kutuları basılı tutulup sürüklenebilir.</summary>
        public bool RecipesDraggable { get; set; }
        /// <summary>Üzerine gelme etiketi (BoardView3D verir).</summary>
        public HoverTag Tag { get; set; }
        public bool Interactive => interactive;
        public bool Hovered => hovered;

        public string HoverTitle => title;
        public string Line => line;

        public Transform Root => root != null ? root : transform;
        public Transform Frame => frame;
        public PartDriver Cover => cover;
        public PartDriver Leaf => leaf;
        public RecipeBookPage PageLeft => pageLeft;
        public RecipeBookPage PageRight => pageRight;
        public RecipeBookPage LeafFront => leafFront;
        public RecipeBookPage LeafBack => leafBack;
        public BoxCollider ClosedHit => closedHit;
        public BoxCollider HitLeft => hitLeft;
        public BoxCollider HitRight => hitRight;
        public IReadOnlyList<RecipeData> Recipes => recipes;
        /// <summary>Kapağın açılma oranı (0 kapalı, 1 = −178°), yaprağın çevrilme oranı (0 sağda, 1 = −176°), üzerine gelme oranı.</summary>
        public float CoverProgress => motion != null ? motion.CoverProgress : 0f;
        public float LeafProgress => motion != null ? motion.LeafProgress : 0f;
        public float HoverProgress => motion != null ? motion.HoverProgress : 0f;
        /// <summary>Kökün titremesiz, üzerine gelmesiz yerel pozu (geçişin o anki yeri).</summary>
        public Pose BasePose => motion != null ? new Pose(motion.BasePosition, motion.BaseRotation) : new Pose(Root.localPosition, Root.localRotation);

        // ------------------------------------------------------------------ token

        public static float ForwardS => BoardViewUtil.Num("motion.board.bookForwardS", 0.35f);
        public static float CoverS => BoardViewUtil.Num("motion.board.bookCoverS", 0.25f);
        public static float CoverDelayS => BoardViewUtil.Num("recipeBook.coverDelayS", 0.1f);
        public static float TurnS => BoardViewUtil.Num("motion.board.pageTurnS", 0.25f);
        public static float CloseS => BoardViewUtil.Num("motion.board.bookCloseS", 0.3f);
        public static float DropS => BoardViewUtil.Num("motion.board.bookDropS", 0.25f);
        public static float HoverMm => BoardViewUtil.Num("recipeBook.hoverMm", 4f);
        public static float HoverS => BoardViewUtil.Num("motion.board.cardLiftS", 0.08f);
        public static float DenyMm => BoardViewUtil.Num("motion.board.denyShakeMm", 2f);
        public static float DenyS => BoardViewUtil.Num("motion.board.denyShakeS", 0.18f);

        /// <summary>Kancadaki kapalı poz (model yereli; Aşama 3 sahne pozu, three.js <c>P(18, 600, zf − 0,019)</c>): (−0,562; 0,993; −0,033), x −4°.</summary>
        public static Pose ClosedPoseLocal => new Pose(new Vector3(-0.562f, 0.993f, -0.033f), Quaternion.Euler(-4f, 0f, 0f));
        /// <summary>Okuma pozu (model yereli; token <c>recipeBook.readPoseLocal</c>, yedek (0; 1,227; −0,665), x −9°).</summary>
        public static Pose ReadPoseLocal => PoseToken("recipeBook.readPoseLocal", new Vector3(0f, 1.227f, -0.665f), -9f);
        /// <summary>[B] Alçak poz (token <c>recipeBook.lowPoseLocal</c>, yedek (−0,02; 1,062; −0,62), x −14°).</summary>
        public static Pose LowPoseLocal => PoseToken("recipeBook.lowPoseLocal", new Vector3(-0.02f, 1.062f, -0.62f), -14f);

        static Pose PoseToken(string key, Vector3 fallback, float tilt) =>
            new Pose(new Vector3(BoardViewUtil.Num(key + ".pos[0]", fallback.x), BoardViewUtil.Num(key + ".pos[1]", fallback.y),
                BoardViewUtil.Num(key + ".pos[2]", fallback.z)), Quaternion.Euler(BoardViewUtil.Num(key + ".tiltX", tilt), 0f, 0f));

        // ------------------------------------------------------------------ kurulum

        /// <summary>Kurucunun (tek seferlik editör script'i) ve testlerin bağları. Kökün o anki yerel pozu kapalı poz olur.</summary>
        public void Configure(Transform bookRoot, Transform modelFrame, PartDriver coverPart, PartDriver leafPart, RecipeBookPage left, RecipeBookPage right,
            RecipeBookPage front, RecipeBookPage back, BoxCollider closed, BoxCollider leftHit, BoxCollider rightHit)
        {
            root = bookRoot; frame = modelFrame; cover = coverPart; leaf = leafPart;
            pageLeft = left; pageRight = right; leafFront = front; leafBack = back;
            closedHit = closed; hitLeft = leftHit; hitRight = rightHit;
            motion = null;
            started = false;
            state = BookState.Closed;
            EnsureInit();
        }

        void Awake() => EnsureInit();

        void EnsureInit()
        {
            if (motion != null) return;
            motion = new RecipeBookMotion(Root, frame, cover, leaf);
            if (started) return;
            started = true;
            SetPagesActive(false);
            SetLeafFacesActive(false);
            if (leaf != null) leaf.SetVisible(true);
            UpdateColliders();
        }

        // ------------------------------------------------------------------ veri

        /// <summary>Sayfaları ve üzerine gelme etiketini bağlar (sayfalar değişmeyen veride iş yapmaz).</summary>
        public void Bind(IReadOnlyList<RecipeData> data, BookWords bookWords, string hoverTitle, string hoverLine)
        {
            EnsureInit();
            recipes.Clear();
            if (data != null) foreach (var r in data) recipes.Add(r);
            words = bookWords;
            title = hoverTitle;
            line = hoverLine;
            if (state == BookState.Turning) return;   // çevirme bitince son veriyle yazılır
            if (state != BookState.Closed) BindStatic(Spread);
        }

        RecipeData RecipeAt(int index) => index >= 0 && index < recipes.Count ? recipes[index] : null;

        void BindLeft(RecipeBookPage page, int s)
        {
            if (page != null) page.Bind(RecipeAt(2 * s), words, false, s > 0, false, orderTab);
        }

        void BindRight(RecipeBookPage page, int s)
        {
            if (page != null) page.Bind(RecipeAt(2 * s + 1), words, true, false, s < SpreadCount - 1, orderTab);
        }

        void BindStatic(int s)
        {
            BindLeft(pageLeft, s);
            BindRight(pageRight, s);
        }

        // ------------------------------------------------------------------ aç / çevir / kapa

        public void Open()
        {
            EnsureInit();
            if (state == BookState.Opening || state == BookState.Open || state == BookState.Turning || state == BookState.Lowered) return;
            EndHover();
            pressGo = null;
            SetPagesActive(true);   // önce: yazılar etkin hiyerarşide uyansın (TMP ölçüsü)
            BindStatic(Spread);
            if (leaf != null) leaf.SetVisible(false);
            state = BookState.Opening;
            motion.GoTo(RecipeBookMotion.Target.Read, ForwardS, CardEase.EaseOut);
            motion.CoverTo(true, CoverDelayS, CoverS);
            Sound?.Invoke(BoardSound.BookOpen);
            UpdateColliders();
        }

        public void Close(bool animate = true)
        {
            EnsureInit();
            if (state == BookState.Turning) FinishTurn();
            EndHover();
            pressGo = null;
            if (!animate)
            {
                motion.SnapTo(RecipeBookMotion.Target.Closed);
                motion.SnapCover(false);
                motion.Tick(0f);
                FinishClose();
                return;
            }
            if (state == BookState.Closed || state == BookState.Closing) return;
            state = BookState.Closing;
            motion.GoTo(RecipeBookMotion.Target.Closed, CloseS, CardEase.EaseInOut);
            motion.CoverTo(false, 0f, Mathf.Min(CoverS, CloseS));
            Sound?.Invoke(BoardSound.BookClose);
            UpdateColliders();
        }

        void FinishClose()
        {
            state = BookState.Closed;
            SetPagesActive(false);
            SetLeafFacesActive(false);
            motion.SnapLeaf(false);
            if (leaf != null) leaf.SetVisible(true);
            motion.Tick(0f);
            UpdateColliders();
        }

        /// <summary>+1 sonraki, −1 önceki açılım (yalnız açıkken; çevirme sürerken ve sınırda yok sayılır).</summary>
        public void Turn(int dir)
        {
            EnsureInit();
            if (state != BookState.Open || dir == 0) return;
            int from = Spread, to = from + Math.Sign(dir);
            if (to < 0 || to >= SpreadCount) return;
            turnFrom = from; turnTo = to; spread = to;
            coveredSwitched = false;
            state = BookState.Turning;
            if (leaf != null) leaf.SetVisible(true);
            SetLeafFacesActive(true);
            if (to > from)
            {
                // ileri: yaprak eski sağ sayfayı kaldırır (ön), arkasında yeni sol sayfa; altından yeni sağ sayfa açılır
                BindRight(leafFront, from);
                BindLeft(leafBack, to);
                BindRight(pageRight, to);
                motion.SnapLeaf(false);
                motion.LeafTo(true, TurnS);
            }
            else
            {
                // geri: yaprak eski sol sayfayı kaldırır (arka), önünde yeni sağ sayfa; altından yeni sol sayfa açılır
                BindLeft(leafBack, from);
                BindRight(leafFront, to);
                BindLeft(pageLeft, to);
                motion.SnapLeaf(true);
                motion.LeafTo(false, TurnS);
            }
            motion.Tick(0f);
            Sound?.Invoke(BoardSound.PageTurn);
            SpreadChanged?.Invoke(to);
            UpdateColliders();
        }

        /// <summary>Açılımı anında ayarlar (kayıttan devam; açılınca buradan başlar). Olay yaymaz.</summary>
        public void SetSpread(int value)
        {
            EnsureInit();
            if (state == BookState.Turning) FinishTurn();
            spread = Mathf.Max(0, value);
            if (state != BookState.Closed) BindStatic(Spread);
        }

        void StepTurn()
        {
            bool forward = turnTo > turnFrom;
            float turned = forward ? motion.LeafProgress : 1f - motion.LeafProgress;
            if (!coveredSwitched && turned >= CoveredPageSwitchAt)
            {
                coveredSwitched = true;
                if (forward) BindLeft(pageLeft, turnTo); else BindRight(pageRight, turnTo);
            }
            if (!motion.LeafMoving) FinishTurn();
        }

        void FinishTurn()
        {
            BindStatic(Spread);
            SetLeafFacesActive(false);
            motion.SnapLeaf(false);
            if (leaf != null) leaf.SetVisible(false);
            motion.Tick(0f);
            state = BookState.Open;
            UpdateColliders();
        }

        /// <summary>[B] Tarif tutulurken alçak poz (B7) ya da okuma pozuna dönüş (B9).</summary>
        public void Lower(bool on)
        {
            EnsureInit();
            if (on)
            {
                if (state == BookState.Turning) FinishTurn();
                if (state != BookState.Open) return;
                state = BookState.Lowered;
                motion.GoTo(RecipeBookMotion.Target.Low, DropS, CardEase.EaseInOut);
            }
            else
            {
                if (state != BookState.Lowered) return;
                state = BookState.Open;
                motion.GoTo(RecipeBookMotion.Target.Read, ForwardS, CardEase.EaseOut);
            }
            UpdateColliders();
        }

        /// <summary>Sol (0) ya da sağ (1) sayfanın tarif kutusunun dünya pozu.</summary>
        public Pose RecipeBoxPose(int side)
        {
            var page = side == 0 ? pageLeft : pageRight;
            if (page != null) return page.RecipeBoxPose();
            var r = Root;
            return new Pose(r.position, r.rotation);
        }

        /// <summary>Çarpıştırıcılar yalnız K4 etkileşimliyken açık; false açık defteri kapatır (B6).</summary>
        public void SetInteractive(bool on)
        {
            EnsureInit();
            interactive = on;
            if (!on)
            {
                EndHover();
                pressGo = null;
                if (state == BookState.Opening || state == BookState.Open || state == BookState.Turning || state == BookState.Lowered) Close(true);
            }
            UpdateColliders();
        }

        void UpdateColliders()
        {
            if (closedHit != null) closedHit.enabled = interactive && state == BookState.Closed;
            bool pages = interactive && (state == BookState.Open || state == BookState.Turning);
            if (hitLeft != null) hitLeft.enabled = pages;
            if (hitRight != null) hitRight.enabled = pages;
            // yaprak yüzleri tık almaz (çevirme sürerken tık yok sayılır)
            if (leafFront != null && leafFront.Hit != null && leafFront.Hit != hitLeft && leafFront.Hit != hitRight) leafFront.Hit.enabled = false;
            if (leafBack != null && leafBack.Hit != null && leafBack.Hit != hitLeft && leafBack.Hit != hitRight) leafBack.Hit.enabled = false;
        }

        void SetPagesActive(bool on)
        {
            if (pageLeft != null && pageLeft.gameObject.activeSelf != on) pageLeft.gameObject.SetActive(on);
            if (pageRight != null && pageRight.gameObject.activeSelf != on) pageRight.gameObject.SetActive(on);
        }

        void SetLeafFacesActive(bool on)
        {
            if (leafFront != null && leafFront.gameObject.activeSelf != on) leafFront.gameObject.SetActive(on);
            if (leafBack != null && leafBack.gameObject.activeSelf != on) leafBack.gameObject.SetActive(on);
        }

        // ------------------------------------------------------------------ kare

        /// <summary>Geçişleri ilerletir (Edit modda testler doğrudan çağırır).</summary>
        public void Advance(float dt)
        {
            EnsureInit();
            motion.Tick(dt);
            switch (state)
            {
                case BookState.Opening:
                    if (!motion.PoseMoving && !motion.CoverMoving) { state = BookState.Open; UpdateColliders(); }
                    break;
                case BookState.Closing:
                    if (!motion.PoseMoving && !motion.CoverMoving) FinishClose();
                    break;
                case BookState.Turning:
                    StepTurn();
                    break;
            }
            if (hovered && (Blocked || !interactive || state != BookState.Closed)) EndHover();
        }

        void Update() => Advance(Time.unscaledDeltaTime);

        void OnDisable() => EndHover();

        // ------------------------------------------------------------------ bölgeler

        /// <summary>
        /// Sayfa noktasının bölgesi (saf; sayfa mm'si 99 × 140, sol üst köken, y aşağı). Sonraki yalnız sağ sayfada ve son açılımdan
        /// önce, önceki yalnız sol sayfada ve ilk açılımdan sonra (sağ/sol alt köşede 16 × 16 mm ve yazısının şeridi). Tarif bölgesi
        /// (kutu + İŞ EMRİ ▶ sekmesi) yalnız <paramref name="recipesDraggable"/> iken; sayfa dışı <see cref="BookZone.Outside"/>.
        /// </summary>
        public static BookZone ZoneAt(bool rightPage, Vector2 pageMm, int spread, int spreadCount, bool recipesDraggable)
        {
            var m = RecipePageMetrics.Default;
            if (pageMm.x < 0 || pageMm.y < 0 || pageMm.x > m.sizeMm.x || pageMm.y > m.sizeMm.y) return BookZone.Outside;
            var g = m.Geometry(rightPage);
            if (rightPage && spread < spreadCount - 1 && g.InNext(pageMm)) return BookZone.Next;
            if (!rightPage && spread > 0 && g.InPrev(pageMm)) return BookZone.Prev;
            if (recipesDraggable && g.InRecipe(pageMm)) return rightPage ? BookZone.RecipeR : BookZone.RecipeL;
            return BookZone.Page;
        }

        /// <summary>Işın sonucunun bölgesi: sayfa çarpıştırıcılarından biri değilse sayfa dışı; boş sayfanın kutusu sayfa sayılır.</summary>
        public BookZone ZoneOf(RaycastResult hit)
        {
            var go = hit.gameObject;
            if (go == null) return BookZone.Outside;
            bool right;
            if (hitRight != null && go == hitRight.gameObject) right = true;
            else if (hitLeft != null && go == hitLeft.gameObject) right = false;
            else return BookZone.Outside;
            var page = right ? pageRight : pageLeft;
            var col = right ? hitRight : hitLeft;
            Vector2 mm = page != null ? page.PageMm(hit.worldPosition) : ColliderMm(col, hit.worldPosition);
            var zone = ZoneAt(right, mm, Spread, SpreadCount, RecipesDraggable || Locked);
            if ((zone == BookZone.RecipeL || zone == BookZone.RecipeR) && RecipeAt(2 * Spread + (right ? 1 : 0)) == null) zone = BookZone.Page;
            return zone;
        }

        static Vector2 ColliderMm(BoxCollider c, Vector3 world)
        {
            if (c == null) return new Vector2(-1, -1);
            var p = c.transform.InverseTransformPoint(world) - c.center;
            var s = c.size;
            var size = RecipePageMetrics.Default.sizeMm;
            return new Vector2((p.x / Mathf.Max(1e-6f, s.x) + 0.5f) * size.x, (0.5f - p.y / Mathf.Max(1e-6f, s.y)) * size.y);
        }

        // ------------------------------------------------------------------ işaretçi

        static bool Blocked => DeskTarget.Blocked != null && DeskTarget.Blocked();

        public void OnPointerEnter(PointerEventData eventData)
        {
            EnsureInit();
            if (!interactive || state != BookState.Closed || Blocked || hovered) return;
            hovered = true;
            motion.Hover(true);
            if (Tag != null) Tag.Show(this);
        }

        public void OnPointerExit(PointerEventData eventData) => EndHover();

        void EndHover()
        {
            if (motion != null) motion.Hover(false);
            if (!hovered) return;
            hovered = false;
            if (Tag != null) Tag.Hide(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressGo = eventData != null ? eventData.pointerPressRaycast.gameObject : null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            EnsureInit();
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left || eventData.dragging || !interactive || Blocked) return;
            var go = eventData.pointerCurrentRaycast.gameObject;
            if (go == null || go != pressGo) return;
            if (state == BookState.Closed)
            {
                if (closedHit != null && go != closedHit.gameObject) return;
                Open();
                Clicked?.Invoke();
                return;
            }
            if (state != BookState.Open) return;   // B4: açılırken, çevrilirken, alçakta ve kapanırken tık yok sayılır
            switch (ZoneOf(eventData.pointerCurrentRaycast))
            {
                case BookZone.Next: Turn(1); break;
                case BookZone.Prev: Turn(-1); break;
                case BookZone.Outside: Close(); break;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureInit();
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left || !interactive || Blocked || state != BookState.Open) return;
            var zone = ZoneOf(eventData.pointerPressRaycast);
            if (zone != BookZone.RecipeL && zone != BookZone.RecipeR) return;
            int side = zone == BookZone.RecipeR ? 1 : 0;
            var recipe = RecipeAt(2 * Spread + side);
            if (recipe == null) return;
            if (Locked || !RecipesDraggable)
            {
                // B10: kilitli planda kart kalkmaz; defter 2 mm titrer, ret sesi
                motion.Shake(DenyMm, DenyS);
                Sound?.Invoke(BoardSound.Deny);
                return;
            }
            // Sunucu kopyayı denetçiye verir (e.pointerDrag kopyaya geçer) ve Lower(true) der (B7)
            RecipeLifted?.Invoke(recipe.Id, eventData, RecipeBoxPose(side));
        }

        // Sürükleme işleyicisi olmazsa EventSystem bu nesneye sürükleme başlatmaz; kopya benimsenince olaylar karta gider
        public void OnDrag(PointerEventData eventData) { }
        public void OnEndDrag(PointerEventData eventData) { }
    }

    /// <summary>
    /// Defterin hareketi: kök kapalı (sahnede yakalanan yerel poz), okuma ve alçak pozlar (pano çerçevesindeki token'lar) arasında;
    /// kapak 0 ile modelin açık açısı arasında; yaprak 0 ile −176° arasında. Kapak ve yaprak +θ yönünden (okuyucuya doğru) döner
    /// ve uçta parçaya oturtulur. Üzerine gelme (çerçevenin −z'sinde) ve ret titremesi (çerçevenin x'inde, üç yarım dalga) kökün
    /// pozuna eklenir. Her yeni geçiş o anki pozdan başlar (yön ortada değişirse kaldığı yerden döner).
    /// </summary>
    internal sealed class RecipeBookMotion
    {
        public enum Target { Closed, Read, Low }

        readonly Transform root, frame;
        readonly PartDriver cover, leaf;
        readonly Vector3 closedPos;
        readonly Quaternion closedRot;

        Vector3 basePos, fromPos, toPos;
        Quaternion baseRot, fromRot, toRot;
        float poseElapsed, poseSeconds;
        CardEase poseEase;

        float cover01, coverFrom, coverTo, coverElapsed, coverDelay, coverSeconds;
        float leaf01, leafFrom, leafTo, leafElapsed, leafSeconds;
        float hover, hoverTarget;
        float shakeElapsed, shakeSeconds, shakeMm, shakeOffsetMm;
        bool shaking;
        float appliedCover = -1f, appliedLeaf = -1f;

        public RecipeBookMotion(Transform root, Transform frame, PartDriver cover, PartDriver leaf)
        {
            this.root = root; this.frame = frame; this.cover = cover; this.leaf = leaf;
            closedPos = basePos = root.localPosition;
            closedRot = baseRot = root.localRotation;
        }

        public Target Current { get; private set; } = Target.Closed;
        public bool PoseMoving { get; private set; }
        public bool CoverMoving { get; private set; }
        public bool LeafMoving { get; private set; }
        public float CoverProgress => cover01;
        public float LeafProgress => leaf01;
        public float HoverProgress => hover;
        public float ShakeOffsetMm => shakeOffsetMm;
        public Vector3 BasePosition => basePos;
        public Quaternion BaseRotation => baseRot;

        public void GoTo(Target target, float seconds, CardEase ease)
        {
            Resolve(target, out toPos, out toRot);
            fromPos = basePos; fromRot = baseRot;
            Current = target;
            poseElapsed = 0; poseSeconds = seconds; poseEase = ease;
            PoseMoving = seconds > 0 && ((toPos - basePos).sqrMagnitude > 1e-12f || Quaternion.Angle(toRot, baseRot) > 1e-4f);
            if (!PoseMoving) { basePos = toPos; baseRot = toRot; }
        }

        public void SnapTo(Target target)
        {
            Resolve(target, out basePos, out baseRot);
            Current = target;
            PoseMoving = false;
        }

        public void CoverTo(bool open, float delay, float seconds)
        {
            coverFrom = cover01; coverTo = open ? 1f : 0f;
            coverElapsed = 0; coverDelay = Mathf.Max(0, delay); coverSeconds = Mathf.Max(0, seconds);
            CoverMoving = !Mathf.Approximately(coverFrom, coverTo);
            if (CoverMoving && coverSeconds <= 0 && coverDelay <= 0) { cover01 = coverTo; CoverMoving = false; }
        }

        public void SnapCover(bool open) { cover01 = open ? 1f : 0f; CoverMoving = false; }

        public void LeafTo(bool turned, float seconds)
        {
            leafFrom = leaf01; leafTo = turned ? 1f : 0f;
            leafElapsed = 0; leafSeconds = Mathf.Max(0, seconds);
            LeafMoving = !Mathf.Approximately(leafFrom, leafTo);
            if (LeafMoving && leafSeconds <= 0) { leaf01 = leafTo; LeafMoving = false; }
        }

        public void SnapLeaf(bool turned) { leaf01 = turned ? 1f : 0f; LeafMoving = false; }

        public void Hover(bool on) => hoverTarget = on ? 1f : 0f;

        public void Shake(float mm, float seconds)
        {
            if (seconds <= 0 || Mathf.Approximately(mm, 0)) return;
            shaking = true;
            shakeMm = mm; shakeElapsed = 0; shakeSeconds = seconds; shakeOffsetMm = 0;
        }

        public void Tick(float dt)
        {
            dt = Mathf.Max(0f, dt);
            if (PoseMoving)
            {
                poseElapsed += dt;
                float u = poseSeconds > 0 ? Mathf.Clamp01(poseElapsed / poseSeconds) : 1f;
                float e = CardEasing.Evaluate(poseEase, u);
                basePos = Vector3.LerpUnclamped(fromPos, toPos, e);
                baseRot = Quaternion.SlerpUnclamped(fromRot, toRot, e);
                if (u >= 1f) { PoseMoving = false; basePos = toPos; baseRot = toRot; }
            }
            if (CoverMoving)
            {
                coverElapsed += dt;
                float u = coverSeconds > 0 ? Mathf.Clamp01((coverElapsed - coverDelay) / coverSeconds) : (coverElapsed >= coverDelay ? 1f : 0f);
                cover01 = Mathf.Lerp(coverFrom, coverTo, CardEasing.Evaluate(CardEase.EaseInOut, u));
                if (u >= 1f) { CoverMoving = false; cover01 = coverTo; }
            }
            if (LeafMoving)
            {
                leafElapsed += dt;
                float u = leafSeconds > 0 ? Mathf.Clamp01(leafElapsed / leafSeconds) : 1f;
                leaf01 = Mathf.Lerp(leafFrom, leafTo, CardEasing.Evaluate(CardEase.EaseInOut, u));
                if (u >= 1f) { LeafMoving = false; leaf01 = leafTo; }
            }
            if (!Mathf.Approximately(hover, hoverTarget))
            {
                float s = RecipeBookView.HoverS;
                hover = s <= 0 ? hoverTarget : Mathf.MoveTowards(hover, hoverTarget, dt / s);
            }
            if (shaking)
            {
                shakeElapsed += dt;
                float u = shakeSeconds > 0 ? shakeElapsed / shakeSeconds : 1f;
                if (u >= 1f) { shaking = false; shakeOffsetMm = 0; }
                else shakeOffsetMm = shakeMm * Mathf.Sin(3f * Mathf.PI * u);
            }
            Apply();
        }

        void Apply()
        {
            var p = basePos;
            if (hover > 0f) p += ParentVector(new Vector3(0f, 0f, -RecipeBookView.HoverMm * 0.001f * hover));
            if (!Mathf.Approximately(shakeOffsetMm, 0f)) p += ParentVector(new Vector3(shakeOffsetMm * 0.001f, 0f, 0f));
            if (root.localPosition != p) root.localPosition = p;
            if (root.localRotation != baseRot) root.localRotation = baseRot;
            // Tam karşılaştırma: Approximately son karedeki 0,99999 → 1 geçişini "değişmedi" sayar, uçtaki PartDriver.Set (−178°)
            // hiç çalışmaz, parçanın değeri 0'da kalırdı (görünüş aynı, değer yanlış)
            if (appliedCover != cover01) { ApplyPart(cover, cover01); appliedCover = cover01; }
            if (appliedLeaf != leaf01) { ApplyPart(leaf, leaf01); appliedLeaf = leaf01; }
        }

        /// <summary>Parçanın uç değeri (aralığın büyük mutlak ucu: Cover −178, Leaf −176).</summary>
        static float End(PartDriver part) => Mathf.Abs(part.RangeTo) > Mathf.Abs(part.RangeFrom) ? part.RangeTo : part.RangeFrom;

        /// <summary>Açılış yönündeki süpürme açısı: negatif uç +θ yönünden (360 + uç) gider.</summary>
        public static float Sweep(PartDriver part)
        {
            if (part == null) return 0f;
            float end = End(part);
            return end < 0f ? 360f + end : end;
        }

        static void ApplyPart(PartDriver part, float t)
        {
            if (part == null) return;
            if (t >= 1f) { part.Set(End(part)); return; }
            if (t <= 0f) { part.Set(0f); return; }
            var axis = part.Axis == PartAxis.X ? Vector3.right : part.Axis == PartAxis.Y ? Vector3.up : Vector3.forward;
            var tr = part.transform;
            tr.localPosition = part.PivotLocal;
            tr.localRotation = Quaternion.AngleAxis(Sweep(part) * t, axis);
        }

        void Resolve(Target target, out Vector3 pos, out Quaternion rot)
        {
            if (target == Target.Closed) { pos = closedPos; rot = closedRot; return; }
            var pose = target == Target.Read ? RecipeBookView.ReadPoseLocal : RecipeBookView.LowPoseLocal;
            if (frame == null) { pos = pose.position; rot = pose.rotation; return; }
            var world = frame.TransformPoint(pose.position);
            var worldRot = frame.rotation * pose.rotation;
            var parent = root.parent;
            pos = parent != null ? parent.InverseTransformPoint(world) : world;
            rot = parent != null ? Quaternion.Inverse(parent.rotation) * worldRot : worldRot;
        }

        /// <summary>Pano çerçevesindeki bir ötelemenin kökün ebeveyn uzayındaki karşılığı.</summary>
        Vector3 ParentVector(Vector3 frameLocal)
        {
            var world = frame != null ? frame.TransformVector(frameLocal) : frameLocal;
            var parent = root.parent;
            if (frame == null) return frameLocal;
            return parent != null ? parent.InverseTransformVector(world) : world;
        }
    }
}
