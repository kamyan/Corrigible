using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kartlı panonun sürükleme ve tıklama denetçisi (Revizyon 3 · R15b, PLAN §1.5 ve §3.9), <c>Planning board</c> kökünde. Kartların
    /// işaretçi olayları buraya gelir (<see cref="BoardCardView"/> aktarır); boş pano tıklaması da <c>Pano yüzü</c>
    /// çarpıştırıcısından hiyerarşi boyunca buraya çıkar (<see cref="IPointerClickHandler"/>).
    /// <list type="bullet">
    /// <item><b>Durumlar:</b> boşta → üzerinde (4 mm kalkış 0,08 s, 0,4 s sonra etiket) → tutuldu (10 mm + 3°; desteden alınan −3°,
    /// eşya −4°; imleci kendi z'sindeki düzlemde izler) → hedef üstünde (geçerli hedefte amber çerçeve, 0,4 s sonra önizleme
    /// etiketi; uyarıda amber, değilse mavi çerçeve) → bırakıldı (cepte 0,06 s oturma; takasta iki kart birlikte 0,32 s, 6 mm
    /// kalkışlı düz yol) ya da geçersiz (0,25 s yay: düzlemde in-out, z 10 mm·sin(πt), eğim 0'a). Geçersiz cepler bütün sürükleme
    /// boyunca sönük. Kilitli planda kalkış yok; basışta 2 mm yanal sarsıntı (0,18 s) ve "tok".</item>
    /// <item><b>Hedef:</b> cep, BOŞTA, İŞ EMRİ ve DEPO bandı imlecin ışınının pano yüzü düzlemiyle (z = <see cref="BoardLayout.FaceZ"/>)
    /// kesişiminden (<see cref="BoardLayout.ZoneAt"/>); HURDA ve eşya sürüklemesinde kişi kartı çarpıştırıcılarıyla (tutulan kartın
    /// çarpıştırıcısı kapalı). Işın <c>eventData.position</c>'dan ve kendi kamerasından kurulur (<c>pointerCurrentRaycast</c> ya da
    /// <c>Mouse.current</c> konumu kullanılmaz).</item>
    /// <item><b>Komut:</b> bırakma geçerliyse (<see cref="CanDrop"/>) <see cref="DropRequested"/> yayınlanır; sunucu komutu yürütür
    /// ve <see cref="Resolve(bool)"/> ile cevaplar. Komut <c>Changed</c>'i eşzamanlı yayınlar: o sırada kart hâlâ bu denetçinindir
    /// (<see cref="Owns"/>), <see cref="BoardView3D.Render"/> ona dokunmaz. Kartın animasyonu bitince sahiplik bırakılır ve pano
    /// son anlık görüntüyle yeniden dizilir.</item>
    /// <item><b>Tıklama:</b> sürüklemesiz sol tık kartı seçer (<see cref="CardClicked"/>); eşya seçiliyken kişi kartına tık o kişiye
    /// takar (<see cref="TargetClicked"/>); kapalı destenin kartına tık eli açar, açık eldeki karta tık seçer ve eli toplar; kişiye
    /// çift tık dosyayı açar (<see cref="CardDoubleClicked"/>). Boş pano: el toplanır, plan açıksa <see cref="EmptyClicked"/>.
    /// BOŞTA alanına tık: kişi seçiliyse dinlenmeye gönderir, değilse eli açar/toplar. Sürüklemeden sonraki bırakış tık sayılmaz.</item>
    /// <item><b>Klavye</b> (<see cref="ReadKeys"/>): Tab / Shift+Tab hedefler arasında (tek emp ana üretim mutfak eğitim BOŞTA
    /// iş emri depo hurda; odak aynı amber çerçeve, hurdada kapak aralığı + ön yüz çerçevesi), Enter seçili kartı odaktaki hedefe
    /// bırakır (BOŞTA = dinlenme), ↑/↓ canlı kişiler arasında seçer. Esc'yi sunucu okur (<c>HandleBack</c> → <see cref="Cancel"/>).</item>
    /// <item><b>İptal:</b> tutulurken sağ tuş basışı, poz K4'ten ayrılınca, girdi kilitlenince ya da plan kilitlenince kart yayla
    /// yerine döner, komut yazılmaz. Tutulan kart başka bir yoldan kaybolursa (hurdaya gitti, kişi öldü) girdi hemen geri verilir
    /// ve tek bir <c>DragChanged(false)</c> yayınlanır.</item>
    /// </list>
    /// Tutulurken kamera kısayolları kapalıdır (<see cref="SiginakCameraRig.InputSuspended"/>). Sunum simülasyonu okumaz ve
    /// değiştirmez; seçim ve sıra anlık görüntüden (<see cref="BoardView3D.Last"/>) okunur.
    /// <para>
    /// Aşama 5 (R18; PLAN §1.6, §1.9, §1.12): sürükleme denemesinde <see cref="CanPick"/> reddederse kart kalkmaz, 2 mm titrer,
    /// "tok" ve (yalnız yetersiz stokta) serbest sayısı kırmızı yanar. Defter açıkken (<see cref="Suspended"/>) kart girdisi yoktur;
    /// her sürüklemesiz sol tık <see cref="BackgroundClicked"/> olur (plan kilitli olsa da). [B] Defterden alınan tarif kopyası
    /// <see cref="Adopt"/> ile tutulan kart olur (kalkış <c>recipeLiftS</c>, saat yönünde 6°; İŞ EMRİ ya da deftere dönüş); formdaki
    /// tarif kartı form içinde yalnız yer değiştirir, form dışına bırakılınca iptal önizlemesi verir ve kabulde kancadaki deftere kayar.
    /// Tarif kartının hedefi yalnız yüz düzleminden çözülür (çarpıştırıcı hedefi yok).
    /// </para>
    /// </summary>
    public sealed class BoardDragController : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Odanın kamerası (ışınlar buradan). Boşsa rig'in kamerası.")]
        [SerializeField] Camera view;
        [Tooltip("PlanningBoard model kökü: pano mm ↔ model yereli bunun çerçevesinde. Boşsa bu nesne.")]
        [SerializeField] Transform modelRoot;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] BoardView3D board;
        [SerializeField] BoardMarks marks;
        [SerializeField] HurdaBin hurda;
        [Tooltip("Üzerine gelme etiketi: kartın kendi etiketi ve bırakma önizlemesi.")]
        [SerializeField] HoverTag hoverTag;
        [Tooltip("Klavye yolu (Tab / Shift+Tab, Enter, ↑/↓) bu bileşende okunur. Sunucu kendisi okuyorsa kapatın.")]
        [SerializeField] bool readKeys = true;

        /// <summary>Klavye hedeflerinin sırası (PLAN §1.5): tek, emp, ana, üretim, mutfak, eğitim, BOŞTA, iş emri, depo, hurda.</summary>
        public static readonly BoardTarget[] KeyboardTargets =
        {
            BoardTarget.Pocket("tek"), BoardTarget.Pocket("emp"), BoardTarget.Pocket("ana"), BoardTarget.Pocket("craft"),
            BoardTarget.Pocket("cook"), BoardTarget.Pocket("train"), BoardTarget.Bosta(), BoardTarget.Order(), BoardTarget.Store(),
            BoardTarget.Hurda(),
        };

        const float WaitTimeoutS = 1f;
        const float RayLength = 5f;

        /// <summary>Denetçinin sürdürdüğü bir kart hareketi (oturma, takas, dönüş, yutulma).</summary>
        sealed class Anim
        {
            public BoardCardView Card;
            public BoardSound? EndSound;
            public bool WaitCallback, Done, ColliderWas;
            /// <summary>Bitince kart gizlenir (deftere dönen tarif kartı) / mıknatıs görünür (panoda oturan tarif kartı).</summary>
            public bool HideAtEnd, MagnetAtEnd;
            /// <summary>[B] Benimsenen kopyanın hareketi: bitince <see cref="AdoptEnded"/> (kabul bilgisiyle).</summary>
            public bool Adopted, Accepted;
        }

        /// <summary>Bırakma önizlemesi etiketi: başlık, gövde, çerçeve rengi (uyarı amber, değilse mavi).</summary>
        sealed class DropTag : IHoverSource, IHoverFrame
        {
            public string Title, Body;
            public bool Warning;
            public string HoverTitle => Title;
            public string Line => Body;
            public Color? Frame => Warning ? AmberColor : BlueColor;

            public void Set(DropPreview p)
            {
                Title = p.Title;
                Body = p.Body;
                Warning = p.Warning;
            }
        }

        // ------------------------------------------------------------------ durum
        readonly HashSet<BoardCardView> owned = new HashSet<BoardCardView>();
        readonly List<Anim> anims = new List<Anim>();
        readonly List<Rect> dimRects = new List<Rect>();
        readonly RaycastHit[] hits = new RaycastHit[16];
        readonly DropTag previewTag = new DropTag();

        BoardCardView held, waiting, waitingPartner, hovered, partnerWatch;
        BoardTarget heldTarget = BoardTarget.None, waitingTarget = BoardTarget.None;
        bool targetKnown, heldFromDeck, heldColliderWas, holdActive, pressDragged;
        bool rigSuspendedByMe, keepSuspendedUntilRightUp, hurdaFocusedByKeys, relayoutPending;
        bool editable = true, suspended;
        Vector3 homePos;
        float homeAngle, heldPlaneZ, heldBaseZ, heldTiltApplied, waitingSince, reasonUntil = -1, adoptBlendUntil = -1;
        BoardCardView adopted;
        Func<Pose> adoptReturn;
        Action<bool> adoptFinished;
        Vector2 grabOffset, lastScreen;
        string lastPreviewTitle;
        int keyboardIndex = -1;

        // ------------------------------------------------------------------ sözleşme

        /// <summary>Plan düzenlenebilir mi (sunucu anlık görüntüden yazar). Tutulurken false olursa kart yerine döner.</summary>
        public bool Editable
        {
            get => editable;
            set
            {
                editable = value;
                if (!value && held != null) Cancel();
            }
        }

        /// <summary>Kart tutuluyor, komut cevabı bekleniyor ya da denetçinin bir kart hareketi sürüyor.</summary>
        public bool Busy => held != null || waiting != null || anims.Count > 0;

        /// <summary>Tutulan (ya da bırakılıp komut cevabını bekleyen) kart.</summary>
        public BoardCardView Held => held != null ? held : waiting;

        /// <summary>Kart tutuluyor ya da denetçinin bir hareketinde: <see cref="BoardView3D.Render"/> onu atlar, bırakmayı erteler.</summary>
        public bool Owns(BoardCardView c) => c != null && owned.Contains(c);

        /// <summary>Geçerli bir hedefe bırakıldı: sunucu komutu yürütüp <see cref="Resolve(bool)"/> der.</summary>
        public event Action<BoardCardView, BoardTarget> DropRequested;

        /// <summary>Hedef bu kart için geçerli mi (sunucu: <c>BoardDropRules.Valid</c>). Boşsa her hedef geçerli sayılır.</summary>
        public Func<BoardCardView, BoardTarget, bool> CanDrop;

        /// <summary>Bırakma önizlemesi (sunucu: <c>BoardPreviewText.For</c>); başlığı boşsa etiket çıkmaz.</summary>
        public Func<BoardCardView, BoardTarget, DropPreview> Preview;

        /// <summary>Kart seçimi (sürüklemesiz sol tık, ↑/↓). Sunucu seçimi yazar ve yeniden çizer; komut değildir.</summary>
        public event Action<BoardCardView> CardClicked;
        /// <summary>Kişi kartına çift tık: sunucu dosyayı açar.</summary>
        public event Action<BoardCardView> CardDoubleClicked;
        /// <summary>Seçili kart bu hedefe bırakılsın (tık-tık ve klavye yolu; sunucu sürüklemeyle aynı kuralı uygular).</summary>
        public event Action<BoardTarget> TargetClicked;
        /// <summary>Boş panoya sürüklemesiz, plan açıkken sol tık (sunucu seçimi temizler).</summary>
        public event Action EmptyClicked;
        /// <summary>Tutma başladı (true) ve bitti (false; her tutma için bir kez).</summary>
        public event Action<bool> DragChanged;
        public event Action<BoardSound> Sound;

        /// <summary>Bırakmanın sonucu (kart, hedef, kabul); <see cref="Resolve(bool, string)"/> sonunda.</summary>
        public event Action<BoardCardView, BoardTarget, bool> Resolved;

        // ------------------------------------------------------------------ Aşama 5 (R18; PLAN §1.6, §1.9, §1.12)

        /// <summary>
        /// Kart alınabilir mi (sunucu: <c>BoardDropRules.CanPick</c>; yetersiz kaynakta serbest 0). Sürükleme denemesinde sorulur (tık
        /// seçimdir, sorulmaz). null dönerse alınır; metin ret nedenidir: kart kalkmaz (üzerine gelme kalkışı kalır), 2 mm titrer,
        /// <see cref="BoardSound.Deny"/>, ret etiketi imlecin yanında ve (yalnız yetersiz stokta, <see cref="FlashesStock"/>) depo
        /// kırmızısı. Boşsa her kart alınır.
        /// </summary>
        public Func<BoardCardView, string> CanPick { get; set; }

        /// <summary>
        /// Defter açıkken kart girdisi yok (defter kipi, PLAN §1.12): üzerine gelme, basış ve sürükleme yok sayılır; her sürüklemesiz sol
        /// tık (kart, pano yüzü, <see cref="ClickTarget(BoardTarget)"/>) <see cref="BackgroundClicked"/> olur; klavye yolu kapalı.
        /// Açılırken tutulan kart (benimsenen kopya dışında) yayla yerine döner. Benimsenen kopyanın sürüklemesi bundan etkilenmez.
        /// </summary>
        public bool Suspended
        {
            get => suspended;
            set
            {
                if (suspended == value) return;
                suspended = value;
                if (!value) return;
                if (held != null && held != adopted) Cancel();
                DropHover();
                ClearKeyboardTarget();
                HideReason();
            }
        }

        /// <summary>Alma reddedildi (kart, neden). Titreme, ses, ret etiketi ve kırmızı denetçinin içindedir; sunucu yalnız bilgi alır.</summary>
        public event Action<BoardCardView, string> PickDenied;

        /// <summary><see cref="Suspended"/> iken her sürüklemesiz sol tık; plan kilitli olsa da (sunucu defteri kapatır). Tık tüketilir.</summary>
        public event Action BackgroundClicked;

        /// <summary>
        /// [B] Benimsenen kopyanın hareketi bitti ve sahiplik bırakıldı (kart, kabul). Dönen kopya gizlenmiştir; sunucu onu burada
        /// havuza bırakabilir (<c>BoardView3D.ReleaseRecipeCopy</c>). Kabulde kopya iş emrinin üstünde oturmuştur.
        /// </summary>
        public event Action<BoardCardView, bool> AdoptEnded;

        /// <summary>[B] Şu an benimsenmiş (tutulan) kopya; yoksa null.</summary>
        public BoardCardView Adopted => adopted;

        /// <summary>
        /// [B] Dışarıda kalkan kartı (defterden alınan tarif kopyası) tutulan kart olarak benimser: kalkış (<c>recipeLiftS</c> 0,1 s,
        /// <see cref="BoardSound.RecipeLift"/>), saat yönünde 6° eğim, hedef ve bırakma sürüklemedeki gibi; kart pano düzleminde
        /// tutulur (sayfa pozundan imlecin altına kalkış süresince süzülür). Ret, Esc, deftere bırakma ya da iptalde kart
        /// <paramref name="returnTo"/> pozuna (dünya) 0,25 s kavisle döner ve gizlenir. <paramref name="finished"/> sonuç belli
        /// olunca bir kez çağrılır: kabulde true (defter kapanır, B8), dönüş başında false (defter öne gelir, B9); kartın hareketi
        /// bitince <see cref="AdoptEnded"/>. Benimsenemezse (girdi yok, plan kilitli, başka kart tutuluyor) hemen false.
        /// Sonraki sürükleme olayları kartın kendisine yönlendirilir (<c>e.pointerDrag</c>).
        /// </summary>
        public void Adopt(BoardCardView card, PointerEventData e, Func<Pose> returnTo, Action<bool> finished)
        {
            if (card == null || e == null || card.Kind != BoardCardKind.Recipe || !Ready || !editable || held != null || waiting != null || Owns(card))
            {
                finished?.Invoke(false);
                return;
            }
            if (card.Controller == null) card.Controller = this;
            card.SetVisible(true);
            adopted = card;
            adoptReturn = returnTo;
            adoptFinished = finished;
            pressDragged = true;
            BeginHold(card, e);
            // Sonraki OnDrag / OnEndDrag defterin sayfası yerine doğrudan karta (oradan bu denetçiye) gelir
            e.pointerDrag = card.gameObject;
        }

        /// <summary>
        /// Ret kırmızısı yalnız yetersiz stokta yanar (R18-not "yetersiz kaynak"): hammadde kartı ve Core'un "Serbest hammadde
        /// yetersiz." iletisi. Tarifin istemediği tür ya da emir yokluğu titrer ama kırmızı yakmaz.
        /// </summary>
        public static bool FlashesStock(BoardCardKind kind, string reason) =>
            kind == BoardCardKind.Material && !string.IsNullOrEmpty(reason) && reason == Corrigible.Core.Loc.T("Serbest hammadde yetersiz.");

        /// <summary>Ret kırmızısı yandı (hammadde türü); yalnız yetersiz stokta. Bilgi içindir (testler, sunucu); kırmızıyı denetçi yakar.</summary>
        public event Action<string> StockFlashed;

        /// <summary>Ret titreşiminin genliği (mm, <c>motion.board.denyShakeMm</c>) ve süresi (s, <c>motion.board.denyShakeS</c>, yedek 0,18).</summary>
        public static float DenyShakeMm => DenyMm;
        public static float DenyShakeS => DenyS;

        /// <summary>
        /// Kart bu ekran noktasında bırakılsa hedefi ne olur (sürüklemedeki çözümün aynısı: önce çarpıştırıcılar, tarif kartında
        /// yalnız yüz düzlemi). Durum değiştirmez; testler ve tanı için.
        /// </summary>
        public BoardTarget DropTargetAt(BoardCardView c, Vector2 screen) =>
            c == null ? BoardTarget.None : ResolveDropTarget(c, screen, out _, out _);

        /// <summary>Bırakma hedefi çarpıştırıcılarla (hurda ön yüzü, kişi kartı) aranır mı: tarif kartında hayır (yalnız yüz düzlemi).</summary>
        public static bool UsesColliderTargets(BoardCardKind kind) => kind != BoardCardKind.Recipe;

        /// <summary>Son bırakmada imlecin pano mm'si (yüz düzleminde).</summary>
        public Vector2 LastDropMm { get; private set; }

        /// <summary>
        /// Son bırakmanın fiş pozu (mm.x, mm.y, açı; <c>BoardUiState.ChipPoses</c> biçimi): fişte kartın merkezi, hammaddede imleç;
        /// ikisi de girdi alanına kırpılır (iç taraf ızgaraya oturtulmaz). Diğer kartlarda kartın merkezi ve açısı.
        /// </summary>
        public Vector3 LastDropPose { get; private set; }

        /// <summary>Tutulan karta uygulanan düzlem içi eğim (Unity derecesi, z ekseni; tasarımın saat yönü artı değerinin eksisi).</summary>
        public float HeldTiltDeg => heldTiltApplied;

        /// <summary>Tutulan kartın üzerinde durduğu hedef (yoksa None).</summary>
        public BoardTarget HoverTarget => heldTarget;

        /// <summary>Klavye yolu bu bileşende mi okunuyor.</summary>
        public bool ReadKeys { get => readKeys; set => readKeys = value; }

        /// <summary>Klavye odağındaki hedef (yoksa None).</summary>
        public BoardTarget KeyboardTarget => keyboardIndex >= 0 && keyboardIndex < KeyboardTargets.Length ? KeyboardTargets[keyboardIndex] : BoardTarget.None;

        /// <summary>Tek seferlik sahne script'i ve testler bağlar.</summary>
        public void Configure(Camera camera, Transform model, SiginakCameraRig cameraRig, BoardView3D boardView, BoardMarks boardMarks,
            HurdaBin bin, HoverTag tag)
        {
            view = camera; modelRoot = model; rig = cameraRig; board = boardView; marks = boardMarks; hurda = bin; hoverTag = tag;
        }

        // ------------------------------------------------------------------ tema

        static float Num(string key, float fallback)
        {
            var t = SiginakTheme.Active;
            return t != null ? t.NumberOf(key, fallback) : fallback;
        }

        static Color Col(string key, Color fallback)
        {
            var t = SiginakTheme.Active;
            return t != null && t.TryGet(key, out var c) ? c : fallback;
        }

        static float HoverLiftMm => Num("cards.hoverLiftMm", 4f);
        static float HeldLiftMm => Num("cards.heldLiftMm", 10f);
        static float SwapLiftMm => Num("cards.swapLiftMm", 6f);
        static float CardLiftS => Num("motion.board.cardLiftS", 0.08f);
        static float SwapS => Num("motion.board.swapS", 0.32f);
        static float ReturnArcS => Num("motion.board.returnArcS", 0.25f);
        static float SettleS => Num("motion.board.settleS", 0.06f);
        static float TuckS => Num("motion.board.itemTuckS", 0.15f);
        static float ChipOutS => Num("motion.board.chipOutS", 0.1f);
        static float DenyMm => Num("motion.board.denyShakeMm", 2f);
        static float DenyS => Num("motion.board.denyShakeS", 0.18f);
        static float ReasonS => Num("motion.board.reasonLabelS", 2f);
        static float RecipeLiftS => Num("motion.board.recipeLiftS", 0.1f);
        static Color AmberColor => Col("light.amber", new Color32(0xFF, 0xB0, 0x60, 0xFF));
        static Color BlueColor => Col("pen.signature", new Color32(0x1A, 0x2F, 0x5A, 0xFF));

        /// <summary>
        /// Tutulan kartın düzlem içi eğimi (Unity derecesi). Tasarımda saat yönü artıdır (kişi +3°, desteden alınan −3°, eşya −4°,
        /// tarif +6° — R18b, yerel <c>cards.recipeHeldTiltDeg</c>); kamera −z'den baktığı için Unity'de işaret tersine döner.
        /// </summary>
        public static float HeldTiltFor(BoardCardKind kind, bool fromDeck)
        {
            float design = kind == BoardCardKind.Item ? Num("cards.itemHeldTiltDeg", -4f)
                : kind == BoardCardKind.Recipe ? Num("cards.recipeHeldTiltDeg", 6f)
                : kind == BoardCardKind.Person && fromDeck ? Num("cards.deckPickTiltDeg", -3f)
                : Num("cards.heldTiltDeg", 3f);
            return -design;
        }

        static float ClickSlop => EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f;

        // ------------------------------------------------------------------ yardımcılar

        Transform Frame => modelRoot != null ? modelRoot : transform;
        Camera Cam => view != null ? view : rig != null && rig.View != null ? rig.View : Camera.main;
        BostaDeckView Deck => board != null ? board.Deck : null;
        BoardSnapshot Snap => board != null ? board.Last : null;

        /// <summary>Etkin ve hiyerarşide açık (edit modda da; <c>isActiveAndEnabled</c> OnEnable'ı bekler).</summary>
        static bool Live(Behaviour b) => b != null && b.enabled && b.gameObject.activeInHierarchy;

        /// <summary>Panoda girdi alınabilir: K4'e varılmış (geçiş bitmiş), oda kilitli değil.</summary>
        bool Ready
        {
            get
            {
                if (!Live(this)) return false;
                if (DeskTarget.Blocked != null && DeskTarget.Blocked()) return false;
                if (rig != null && (rig.Current != SiginakPose.Shelf || rig.InTransition || rig.Locked)) return false;
                return true;
            }
        }

        BoardCardView SelectedCard
        {
            get
            {
                var s = Snap;
                return s != null && !string.IsNullOrEmpty(s.SelectedId) ? board.CardFor(s.SelectedId) : null;
            }
        }

        string OwnerOf(BoardCardView item)
        {
            if (item == null || item.Kind != BoardCardKind.Item) return null;
            var s = Snap;
            var d = s != null ? s.Item(item.Id) : null;
            return d != null && !string.IsNullOrEmpty(d.OwnerId) ? d.OwnerId : null;
        }

        static bool Moved(PointerEventData e)
        {
            float slop = ClickSlop;
            return (e.position - e.pressPosition).sqrMagnitude >= slop * slop;
        }

        bool ScreenRay(Vector2 screen, out Ray ray)
        {
            var cam = Cam;
            if (cam == null) { ray = default; return false; }
            ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0));
            return true;
        }

        /// <summary>Dünya ışınının <paramref name="frame"/> yerelinde z = <paramref name="z"/> düzlemiyle kesişimi.</summary>
        static bool PlaneLocal(Ray ray, Transform frame, float z, out Vector3 local)
        {
            local = default;
            Vector3 o = frame != null ? frame.InverseTransformPoint(ray.origin) : ray.origin;
            Vector3 d = frame != null ? frame.InverseTransformPoint(ray.origin + ray.direction) - o : ray.direction;
            if (Mathf.Abs(d.z) < 1e-7f) return false;
            float t = (z - o.z) / d.z;
            if (t < 0) return false;
            local = o + d * t;
            return true;
        }

        bool FaceMm(Vector2 screen, out Vector2 mm)
        {
            mm = default;
            if (!ScreenRay(screen, out var ray) || !PlaneLocal(ray, Frame, BoardLayout.FaceZ, out var p)) return false;
            mm = BoardLayout.LocalToMm(p);
            return true;
        }

        Transform CardFrame(BoardCardView c) => c.transform.parent != null ? c.transform.parent : Frame;

        static Vector2 CardSizeMm(BoardCardKind kind)
        {
            switch (kind)
            {
                case BoardCardKind.Person: return new Vector2(80, 120);
                case BoardCardKind.Item: return new Vector2(80, 50);
                case BoardCardKind.Material: return new Vector2(80, 56);
                case BoardCardKind.Chip: return new Vector2(56, 40);
                default: return new Vector2(112, 80);
            }
        }

        static Rect CardRectMm(BoardCardView c)
        {
            var centre = BoardLayout.LocalToMm(c.Target);
            var size = CardSizeMm(c.Kind);
            return new Rect(centre.x - size.x / 2, centre.y - size.y / 2, size.x, size.y);
        }

        /// <summary>Hedefin pano mm dikdörtgeni (çerçeve ve odak için); dikdörtgeni yoksa false (hurda, kişi kartı).</summary>
        public static bool TargetRectMm(BoardTarget t, out Rect rect)
        {
            switch (t.Kind)
            {
                case BoardTargetKind.Pocket: rect = BoardLayout.Pocket(t.Job); return rect.width > 0;
                case BoardTargetKind.Bosta: rect = BoardLayout.Bosta; return true;
                case BoardTargetKind.Order: rect = BoardLayout.Order; return true;
                case BoardTargetKind.Store: rect = BoardLayout.Depot; return true;
                case BoardTargetKind.Topic: rect = BoardLayout.TopicRow(t.Row); return t.Row >= 0;
                default: rect = default; return false;
            }
        }

        /// <summary>Fiş merkezini iş emrinin girdi alanına kırpar (kenardaki içeri itilir, içtekine dokunulmaz).</summary>
        public static Vector2 ClampToInputs(Vector2 mm)
        {
            var r = BoardLayout.Inputs;
            return new Vector2(Mathf.Clamp(mm.x, r.xMin, r.xMax), Mathf.Clamp(mm.y, r.yMin, r.yMax));
        }

        static bool TryChipKind(string id, out int kindIndex)
        {
            kindIndex = -1;
            if (string.IsNullOrEmpty(id)) return false;
            int a = id.IndexOf(':');
            int b = id.LastIndexOf(':');
            if (a < 0 || b <= a) return false;
            string kind = id.Substring(a + 1, b - a - 1);
            kindIndex = Array.IndexOf(BoardLayout.MaterialKinds, kind);
            return kindIndex >= 0;
        }

        void Emit(BoardSound s) => Sound?.Invoke(s);

        // ------------------------------------------------------------------ hedef çözümü

        /// <summary>Pano yüzündeki noktanın hedefi (kartın türüne göre); <paramref name="drag"/> fişin iş emri içinde yer değiştirmesi.</summary>
        BoardTarget ZoneFor(BoardCardKind kind, Vector2 mm, bool drag)
        {
            switch (kind)
            {
                case BoardCardKind.Person:
                {
                    if (drag && BoardLayout.Pocket("train").Contains(mm))
                        for (int r = 0; r < 7; r++)
                            if (BoardLayout.TopicRow(r).Contains(mm)) return BoardTarget.Topic(r);
                    return BoardLayout.ZoneAt(mm, BoardCardKind.Person);
                }
                case BoardCardKind.Item:
                {
                    var t = BoardLayout.ZoneAt(mm, BoardCardKind.Item);
                    if (!t.IsNone) return t;
                    var s = Snap;
                    foreach (var job in BoardLayout.Jobs)
                        if (BoardLayout.Pocket(job).Contains(mm))
                            return s != null && s.PocketOccupant.TryGetValue(job, out var who) && !string.IsNullOrEmpty(who)
                                ? BoardTarget.Pocket(job) : BoardTarget.None;
                    return BoardTarget.None;
                }
                case BoardCardKind.Chip:
                    if (drag && BoardLayout.Order.Contains(mm)) return BoardTarget.Order();
                    return BoardLayout.ZoneAt(mm, BoardCardKind.Chip);
                default:
                    return BoardLayout.ZoneAt(mm, kind);
            }
        }

        void SortHits(int count)
        {
            for (int i = 1; i < count; i++)
            {
                var h = hits[i];
                int j = i - 1;
                while (j >= 0 && hits[j].distance > h.distance) { hits[j + 1] = hits[j]; j--; }
                hits[j + 1] = h;
            }
        }

        /// <summary>
        /// İmlecin altındaki ilk çarpıştırıcı: hurda ön yüzü → HURDA; eşya sürüklenirken kişi kartı (ya da bir kişinin eşyası) →
        /// o kişi. Başka bir şeyse <c>None</c> (yüz düzlemine bakılır).
        /// </summary>
        BoardTarget ColliderTarget(BoardCardView dragged, BoardCardKind kind, Ray ray, out BoardCardView personCard)
        {
            personCard = null;
            if (!UsesColliderTargets(kind)) return BoardTarget.None;
            int count = Physics.RaycastNonAlloc(ray, hits, RayLength, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (count > hits.Length) count = hits.Length;
            SortHits(count);
            for (int i = 0; i < count; i++)
            {
                var col = hits[i].collider;
                if (col == null) continue;
                if (hurda != null && col == hurda.Front) return BoardTarget.Hurda();
                var card = col.GetComponentInParent<BoardCardView>();
                if (card != null && (card == dragged || Owns(card))) continue;
                if (card == null || kind != BoardCardKind.Item) return BoardTarget.None;
                string pid = card.Kind == BoardCardKind.Person ? card.Id : OwnerOf(card);
                if (string.IsNullOrEmpty(pid)) return BoardTarget.None;
                personCard = card.Kind == BoardCardKind.Person ? card : board != null ? board.CardFor(pid) : null;
                return BoardTarget.Person(pid);
            }
            return BoardTarget.None;
        }

        BoardTarget ResolveDropTarget(BoardCardView c, Vector2 screen, out Rect rect, out bool hasRect)
        {
            rect = default;
            hasRect = false;
            if (!ScreenRay(screen, out var ray)) return BoardTarget.None;
            var t = ColliderTarget(c, c.Kind, ray, out var person);
            if (!t.IsNone)
            {
                if (person != null) { rect = CardRectMm(person); hasRect = true; }
                return t;
            }
            if (!PlaneLocal(ray, Frame, BoardLayout.FaceZ, out var p)) return BoardTarget.None;
            t = ZoneFor(c.Kind, BoardLayout.LocalToMm(p), true);
            hasRect = TargetRectMm(t, out rect);
            return t;
        }

        BoardTarget ResolveClickTarget(Vector2 screen)
        {
            if (!ScreenRay(screen, out var ray)) return new BoardTarget(BoardTargetKind.Empty);
            var sel = SelectedCard;
            var kind = sel != null ? sel.Kind : BoardCardKind.Person;
            var t = ColliderTarget(null, kind, ray, out _);
            if (!t.IsNone) return t;
            if (!PlaneLocal(ray, Frame, BoardLayout.FaceZ, out var p)) return new BoardTarget(BoardTargetKind.Empty);
            var mm = BoardLayout.LocalToMm(p);
            t = ZoneFor(kind, mm, false);
            if (t.IsNone && BoardLayout.Bosta.Contains(mm)) t = BoardTarget.Bosta();
            return t.IsNone ? new BoardTarget(BoardTargetKind.Empty) : t;
        }

        // ------------------------------------------------------------------ işaretçi (BoardCardView aktarır)

        public void OnCardPointerEnter(BoardCardView c, PointerEventData e)
        {
            if (c == null || suspended || held != null || waiting != null || !Ready || Owns(c)) return;
            hovered = c;
            if (editable && CanHoverLift(c)) c.SetLift(HoverLiftMm, 0, CardLiftS);
            if (hoverTag != null) hoverTag.Show(c);
        }

        public void OnCardPointerExit(BoardCardView c, PointerEventData e)
        {
            if (c == null) return;
            if (hoverTag != null) hoverTag.Hide(c);
            if (hovered != c) return;
            hovered = null;
            if (c != held && !Owns(c) && c.LiftMm > 0) c.SetLift(0, 0, CardLiftS);
        }

        /// <summary>Arkaya sıkışmış eşya öne çıkmaz; tarif kartı yalnız tutulabilen formdaki kartsa.</summary>
        bool CanHoverLift(BoardCardView c) => c.Kind == BoardCardKind.Recipe ? CanHoldRecipe(c) : OwnerOf(c) == null;

        /// <summary>[B] Formdaki tarif kartı tutulabilir mi (anlık görüntü: <c>Order.CardDraggable</c>). Kopya yalnız <see cref="Adopt"/> ile.</summary>
        bool CanHoldRecipe(BoardCardView c)
        {
            var order = Snap?.Order;
            if (order == null || !order.CardDraggable || string.IsNullOrEmpty(c.Id)) return false;
            return c.Id == order.OrderId || (order.Card != null && c.Id == order.Card.Id);
        }

        void DropHover()
        {
            var c = hovered;
            if (c == null) return;
            hovered = null;
            if (hoverTag != null) hoverTag.Hide(c);
            if (c != held && !Owns(c) && c.LiftMm > 0) c.SetLift(0, 0, CardLiftS);
        }

        /// <summary>
        /// Ret kırmızısı (PLAN §1.9): yalnız yetersiz stokta (<see cref="FlashesStock"/>) depodaki stok kartının serbest sayısı
        /// <c>denyFlashS</c> boyunca kırmızı (<c>StoreView.DenyFlash</c>) ve <see cref="StockFlashed"/>.
        /// </summary>
        void FlashStock(BoardCardView c, string reason)
        {
            if (c == null || !FlashesStock(c.Kind, reason)) return;
            if (board != null && board.Store != null) board.Store.DenyFlash(c.Id);
            StockFlashed?.Invoke(c.Id);
        }

        /// <summary>Alma reddi (PLAN §1.9): kart kalkmaz, titrer, "tok", ret etiketi; yalnız yetersiz stokta serbest sayısı kırmızı.</summary>
        void DenyPick(BoardCardView c, string why, Vector2 screen)
        {
            if (hoverTag != null) hoverTag.Hide(c);
            c.Shake(DenyMm, DenyS);
            Emit(BoardSound.Deny);
            FlashStock(c, why);
            lastScreen = screen;
            ShowReason(new DropPreview(c.HoverTitle, why, true, false), screen);
            PickDenied?.Invoke(c, why);
        }

        public void OnCardPointerDown(BoardCardView c, PointerEventData e)
        {
            if (e == null) return;
            if (e.button == PointerEventData.InputButton.Right)
            {
                if (held != null) CancelByRight();
                return;
            }
            if (e.button != PointerEventData.InputButton.Left) return;
            pressDragged = false;
            if (suspended) return;                       // defter kipi: basış bir şey yapmaz (tık defteri kapatır)
            HideReason();
            if (c == null || !Ready || held != null || waiting != null || Owns(c)) return;
            if (!editable)
            {
                // Kilitli plan: basışta 2 mm yanal sarsıntı ve "tok" (üzerine gelmede değil; fare gezinirken tıkırdamasın)
                c.Shake(DenyMm, DenyS);
                Emit(BoardSound.Deny);
            }
        }

        public void OnCardPointerUp(BoardCardView c, PointerEventData e) { }

        public void OnCardClick(BoardCardView c, PointerEventData e)
        {
            if (c == null || e == null || e.button != PointerEventData.InputButton.Left) return;
            if (pressDragged || e.dragging) return;   // sürüklemenin bırakışı tık değildir (bir sonraki basış sıfırlar)
            if (suspended)
            {
                if (Ready && held == null && waiting == null && !Moved(e)) BackgroundClicked?.Invoke();
                return;
            }
            if (!Ready || held != null || waiting != null || Owns(c) || Moved(e)) return;
            HideReason();

            if (e.clickCount >= 2 && c.Kind == BoardCardKind.Person)
            {
                CardDoubleClicked?.Invoke(c);
                return;
            }

            var sel = SelectedCard;
            if (sel != null && sel != c)
            {
                // Eşya seçiliyken kişi kartına (ya da bir kişinin eşyasına) tık = o kişiye tak
                if (sel.Kind == BoardCardKind.Item)
                {
                    string pid = c.Kind == BoardCardKind.Person ? c.Id : OwnerOf(c);
                    if (!string.IsNullOrEmpty(pid)) { CloseDeck(); TargetClicked?.Invoke(BoardTarget.Person(pid)); return; }
                }
                // Sahipli eşya ya da fiş seçiliyken depodaki karta tık = depoya
                bool selToStore = (sel.Kind == BoardCardKind.Item && OwnerOf(sel) != null) || sel.Kind == BoardCardKind.Chip;
                bool depotCard = c.Kind == BoardCardKind.Material || (c.Kind == BoardCardKind.Item && OwnerOf(c) == null);
                if (selToStore && depotCard) { CloseDeck(); TargetClicked?.Invoke(BoardTarget.Store()); return; }
            }

            var deck = Deck;
            if (deck != null && deck.Contains(c) && !deck.IsOpen)
            {
                deck.Toggle();   // kapalı destenin kartı = deste düğmesi
                return;
            }
            CloseDeck();         // açık eldeki karta tık: seçer ve eli toplar
            CardClicked?.Invoke(c);
        }

        public void OnCardBeginDrag(BoardCardView c, PointerEventData e)
        {
            if (c == null || e == null || e.button != PointerEventData.InputButton.Left) return;
            pressDragged = true;
            if (suspended || !Ready || !editable || held != null || waiting != null || Owns(c)) return;
            if (!Live(c) || string.IsNullOrEmpty(c.Id)) return;
            if (c.Kind == BoardCardKind.Recipe && !CanHoldRecipe(c)) return;   // defterin kopyası yalnız Adopt ile
            string why = CanPick != null ? CanPick(c) : null;
            if (!string.IsNullOrEmpty(why)) { DenyPick(c, why, e.position); return; }
            BeginHold(c, e);
        }

        public void OnCardDrag(BoardCardView c, PointerEventData e)
        {
            if (c == null || c != held || e == null) return;
            if (AbortIfGone()) return;
            Follow(c, e.position);
            UpdateTarget(c, e.position);
        }

        public void OnCardEndDrag(BoardCardView c, PointerEventData e)
        {
            if (c == null || c != held || e == null) return;
            if (AbortIfGone()) return;
            Follow(c, e.position);
            lastScreen = e.position;
            var t = ResolveDropTarget(c, e.position, out _, out _);
            bool valid = !t.IsNone && (CanDrop == null || CanDrop(c, t));
            DropPreview reason = DropPreview.Invalid;
            if (!valid && !t.IsNone && Preview != null) reason = Preview(c, t);
            RecordDrop(c, e.position);
            EndHold();

            if (!valid)
            {
                FlashStock(c, reason.Body);
                StartReturn(c);
                if (!string.IsNullOrEmpty(reason.Title)) ShowReason(reason, e.position);
                return;
            }
            waiting = c;
            waitingTarget = t;
            waitingSince = Time.unscaledTime;
            partnerWatch = null;
            // Takas ortağı komuttan önce okunur: komutun eşzamanlı Changed'i anlık görüntüyü değiştirir
            waitingPartner = SwapPartner(c, t);
            if (DropRequested == null) { Resolve(false); return; }
            DropRequested.Invoke(c, t);
        }

        /// <summary>Boş pano tıklaması (<c>Pano yüzü</c> ve işleyicisi olmayan pano çarpıştırıcıları hiyerarşi boyunca buraya çıkar).</summary>
        public void OnPointerClick(PointerEventData e)
        {
            if (e == null || e.button != PointerEventData.InputButton.Left) return;
            if (!Ready || held != null || waiting != null || e.dragging || Moved(e)) return;
            if (suspended)
            {
                // Defter kipi: pano yüzüne tık defteri kapatır (plan kilitli olsa da); seçim değişmez
                HideReason();
                BackgroundClicked?.Invoke();
                return;
            }
            if (board != null && !board.Interactive) return;
            HideReason();
            ClickTarget(ResolveClickTarget(e.position));
        }

        // ------------------------------------------------------------------ tutma

        void BeginHold(BoardCardView c, PointerEventData e)
        {
            if (hoverTag != null) hoverTag.Hide(c);
            HideReason();
            ClearKeyboardTarget();
            hovered = null;

            bool adopting = c == adopted;
            var deck = Deck;
            heldFromDeck = !adopting && deck != null && deck.Contains(c);
            lastPreviewTitle = null;
            homePos = c.Target;
            homeAngle = c.TargetAngle;
            if (c.Kind == BoardCardKind.Person)
            {
                if (heldFromDeck) deck.TakeOne(c);           // öbürleri alttan toplanır
                else if (deck != null && deck.IsOpen) deck.Close(true);
            }

            // Süren hareketi o anki pozda durdur (kalkış ayrı sürer)
            var basePos = c.transform.localPosition;
            basePos.z += c.LiftMm * 0.001f;
            float baseAngle = Mathf.DeltaAngle(0, c.transform.localEulerAngles.z - c.TiltDeg);
            c.Place(basePos, baseAngle);

            heldColliderWas = c.Collider != null && c.Collider.enabled;
            c.SetInteractive(false);
            held = c;
            owned.Add(c);

            heldTiltApplied = HeldTiltFor(c.Kind, heldFromDeck);
            float lift = HeldLiftMm;
            c.SetLift(lift, heldTiltApplied, adopting ? RecipeLiftS : CardLiftS);
            // Defterin kopyası pano düzleminde tutulur (C Ç17): sayfa pozundan imlecin altına kalkış süresince süzülür
            heldBaseZ = adopting ? BoardLayout.CardZ(BoardLayout.RecipeLevel) : basePos.z;
            heldPlaneZ = heldBaseZ - lift * 0.001f;
            adoptBlendUntil = adopting ? Time.unscaledTime + RecipeLiftS : -1;
            grabOffset = Vector2.zero;
            if (!adopting && ScreenRay(e.pressPosition, out var ray) && PlaneLocal(ray, CardFrame(c), heldPlaneZ, out var hit))
                grabOffset = new Vector2(basePos.x - hit.x, basePos.y - hit.y);
            if (c.Kind == BoardCardKind.Recipe) c.SetMagnetVisible(false);   // mıknatıs panoda görünür, elde gizli

            if (rig != null && !rig.InputSuspended) { rig.InputSuspended = true; rigSuspendedByMe = true; }
            targetKnown = false;
            heldTarget = BoardTarget.None;
            BuildDim(c);
            holdActive = true;
            Emit(adopting ? BoardSound.RecipeLift : BoardSound.Lift);
            DragChanged?.Invoke(true);

            Follow(c, e.position);
            UpdateTarget(c, e.position);
        }

        void Follow(BoardCardView c, Vector2 screen)
        {
            if (!ScreenRay(screen, out var ray) || !PlaneLocal(ray, CardFrame(c), heldPlaneZ, out var p)) return;
            var to = new Vector3(p.x + grabOffset.x, p.y + grabOffset.y, heldBaseZ);
            float left = c == adopted ? adoptBlendUntil - Time.unscaledTime : 0;
            if (left > 0.001f) c.MoveTo(to, c.TargetAngle, left, CardEase.EaseOut);
            else c.Place(to, c.TargetAngle);
            if (c.Kind == BoardCardKind.Person && board != null) board.SnapItemsToOwner(c);   // eşyalar sahibiyle gider
        }

        void UpdateTarget(BoardCardView c, Vector2 screen)
        {
            var t = ResolveDropTarget(c, screen, out var rect, out bool hasRect);
            if (targetKnown && t.Equals(heldTarget))
            {
                if (hoverTag != null && hoverTag.Owner == previewTag) hoverTag.SetAnchor(previewTag, screen);
                return;
            }
            targetKnown = true;
            heldTarget = t;
            bool valid = !t.IsNone && (CanDrop == null || CanDrop(c, t));
            if (marks != null)
            {
                if (valid && hasRect) marks.ShowGlow(rect);
                else marks.ClearGlow();
            }
            if (hurda != null) hurda.SetTargeted(valid && t.Kind == BoardTargetKind.Hurda);
            if (!t.IsNone && Preview != null) ShowPreview(Preview(c, t), screen, false);
            else HidePreview();
        }

        /// <summary>Sürükleme boyunca geçersiz ceplerin sönük örtüsü (kartın kendi yeri hariç).</summary>
        void BuildDim(BoardCardView c)
        {
            dimRects.Clear();
            if (marks == null) return;
            if (CanDrop != null)
            {
                var s = Snap;
                string home = null;
                if (c.Kind == BoardCardKind.Person) home = s?.Person(c.Id)?.JobId;
                else if (c.Kind == BoardCardKind.Item)
                {
                    string owner = OwnerOf(c);
                    if (owner != null && s != null)
                        foreach (var kv in s.PocketOccupant) if (kv.Value == owner) { home = kv.Key; break; }
                }
                switch (c.Kind)
                {
                    case BoardCardKind.Person:
                    case BoardCardKind.Item:
                        foreach (var job in BoardLayout.Jobs)
                            if (job != home && !CanDrop(c, BoardTarget.Pocket(job))) dimRects.Add(BoardLayout.Pocket(job));
                        break;
                    case BoardCardKind.Material:
                        if (!CanDrop(c, BoardTarget.Order())) dimRects.Add(BoardLayout.Order);
                        break;
                    case BoardCardKind.Recipe:
                        // Defterin kopyası: emir aynı tarifse ya da plan kuyruğu almıyorsa iş emri sönük
                        if (c == adopted && !CanDrop(c, BoardTarget.Order())) dimRects.Add(BoardLayout.Order);
                        break;
                }
            }
            if (dimRects.Count > 0) marks.Dim(dimRects);
            else marks.ClearDim();
        }

        void RecordDrop(BoardCardView c, Vector2 screen)
        {
            if (FaceMm(screen, out var cursor)) LastDropMm = cursor;
            var centre = BoardLayout.LocalToMm(c.Target);
            if (c.Kind == BoardCardKind.Chip) LastDropPose = Pose3(ClampToInputs(centre), c.TargetAngle);
            else if (c.Kind == BoardCardKind.Material) LastDropPose = Pose3(ClampToInputs(LastDropMm), 0);
            else if (c.Kind == BoardCardKind.Recipe) LastDropPose = Pose3(BoardLayout.ClampRecipe(centre), c.TargetAngle);
            else LastDropPose = Pose3(centre, c.TargetAngle);
        }

        static Vector3 Pose3(Vector2 mm, float angle) => new Vector3(mm.x, mm.y, angle);

        /// <summary>Tutmanın ortak sonu: işaretler, etiket, kamera kısayolları; bir kez <c>DragChanged(false)</c>.</summary>
        void EndHold()
        {
            if (marks != null) { marks.ClearGlow(); marks.ClearDim(); }
            if (hurda != null && !hurdaFocusedByKeys) hurda.SetTargeted(false);
            HidePreview();
            ReleaseRig();
            held = null;
            heldTarget = BoardTarget.None;
            targetKnown = false;
            if (holdActive)
            {
                holdActive = false;
                DragChanged?.Invoke(false);
            }
        }

        void ReleaseRig()
        {
            if (keepSuspendedUntilRightUp) return;
            if (rigSuspendedByMe && rig != null) rig.InputSuspended = false;
            rigSuspendedByMe = false;
        }

        void CancelByRight()
        {
            // Sağ tuş kalkana dek kamera askıda kalır: bırakış "geri" tıkı (masaya dönüş) sayılmasın
            keepSuspendedUntilRightUp = rigSuspendedByMe;
            Cancel();
        }

        /// <summary>Tutulan kart başka bir yoldan kayboldu (gizlendi ya da kimliği havuzdan düştü): girdi hemen geri verilir.</summary>
        bool AbortIfGone()
        {
            var c = held;
            if (c == null) return false;
            // Tarif kartları (kopya ve formdaki kart) panonun kimlik eşlemesinde olmayabilir: yalnız görünürlük ve kimlik
            bool gone = !Live(c) || string.IsNullOrEmpty(c.Id) || (c.Kind != BoardCardKind.Recipe && board != null && board.CardFor(c.Id) != c);
            if (!gone) return false;
            owned.Remove(c);
            if (c.Collider != null) c.SetInteractive(heldColliderWas);   // girdi, DragChanged(false)'tan önce
            EndHold();
            relayoutPending = true;
            if (c == adopted)
            {
                FinishAdopt(false);
                AdoptEnded?.Invoke(c, false);
            }
            return true;
        }

        BoardCardView SwapPartner(BoardCardView c, BoardTarget t)
        {
            if (c.Kind != BoardCardKind.Person || board == null) return null;
            string job = t.Kind == BoardTargetKind.Topic ? "train" : t.Kind == BoardTargetKind.Pocket ? t.Job : null;
            var s = Snap;
            if (job == null || s == null || !s.PocketOccupant.TryGetValue(job, out var id) || string.IsNullOrEmpty(id) || id == c.Id) return null;
            return board.CardFor(id);
        }

        // ------------------------------------------------------------------ bırakmanın cevabı

        /// <summary>Sunucunun cevabı: kabulde oturma / takas / kayma / yutulma, retde yayla dönüş.</summary>
        public void Resolve(bool accepted) => Resolve(accepted, null);

        /// <summary>
        /// Sunucunun cevabı; <paramref name="reason"/> doluysa (Core'un iletisi) reddedilen bırakmanın yerinde etiket hemen çıkar.
        /// Hammadde hazırlaması reddedilirse stok kartındaki serbest sayı kırmızı yanar.
        /// </summary>
        public void Resolve(bool accepted, string reason)
        {
            var c = waiting;
            if (c == null) return;
            var t = waitingTarget;
            var partner = waitingPartner;
            waiting = null;
            waitingPartner = null;
            waitingTarget = BoardTarget.None;

            if (!accepted)
            {
                partnerWatch = null;
                FlashStock(c, reason);
                StartReturn(c);
                if (!string.IsNullOrEmpty(reason))
                    ShowReason(new DropPreview(lastPreviewTitle ?? c.HoverTitle, reason, true, false), lastScreen);
                Resolved?.Invoke(c, t, false);
                FlushRelayout();
                return;
            }

            switch (c.Kind)
            {
                case BoardCardKind.Person:
                    AcceptPerson(c, t, partner);
                    break;
                case BoardCardKind.Item:
                    if (t.Kind == BoardTargetKind.Hurda) Swallow(c);
                    else
                    {
                        c.SetLift(0, 0, TuckS);
                        HandBack(c, true);
                        if (t.Kind == BoardTargetKind.Store) Emit(BoardSound.Settle);   // sahibine sıkışmayı pano "şık"lar
                    }
                    break;
                case BoardCardKind.Chip:
                    if (t.Kind == BoardTargetKind.Order)
                    {
                        var to = BoardLayout.MmToLocal(new Vector2(LastDropPose.x, LastDropPose.y), c.Target.z);
                        c.MoveTo(to, LastDropPose.z, SettleS, CardEase.EaseIn);
                        c.SetLift(0, 0, SettleS);
                        AddAnim(c, BoardSound.Settle);
                    }
                    else if (TryChipKind(c.Id, out int k))
                    {
                        // Depoya geri alınan fiş stok kartına süzülüp kaybolur
                        c.MoveTo(BoardLayout.MmToLocal(BoardLayout.Material(k), c.Target.z), 0, ChipOutS, CardEase.EaseIn);
                        c.SetLift(0, 0, ChipOutS);
                        AddAnim(c, BoardSound.Settle);
                    }
                    else { c.SetLift(0, 0, SettleS); HandBack(c, true); }
                    break;
                case BoardCardKind.Recipe:
                    AcceptRecipe(c, t);
                    break;
                default:
                    // Hammadde iş emrine: stok kartı yerine döner, fiş ondan çıkar (StoreView / WorkOrderView)
                    c.SetLift(0, 0, TuckS);
                    HandBack(c, true);
                    Emit(BoardSound.Settle);
                    break;
            }
            Resolved?.Invoke(c, t, true);
            FlushRelayout();
        }

        void AcceptPerson(BoardCardView c, BoardTarget t, BoardCardView partner)
        {
            string job = t.Kind == BoardTargetKind.Topic ? "train" : t.Kind == BoardTargetKind.Pocket ? t.Job : null;
            if (job != null && BoardLayout.SlotIndexOfJob(job) >= 0)
            {
                var dest = BoardLayout.MmToLocal(BoardLayout.CardCentre(job), BoardLayout.CardZ(BoardLayout.PocketLevel));
                bool swap = partner != null && partner != c;
                if (swap)
                {
                    // Takas: iki kart birlikte 0,32 s, düz yol, ortada 6 mm kalkış. Ortağı pano (ya da deste) komutun
                    // eşzamanlı Changed'inde yürütmeye başladı; varışı ikinci "tık".
                    c.ArcTo(dest, 0, SwapS, SwapLiftMm);
                    if (partner.Moving) partnerWatch = partner;
                }
                else
                {
                    c.MoveTo(dest, 0, SettleS, CardEase.EaseIn);
                    c.SetLift(0, 0, SettleS);
                }
                AddAnim(c, BoardSound.Settle);
                return;
            }
            if (t.Kind == BoardTargetKind.Bosta || t.Kind == BoardTargetKind.Deck)
            {
                var deck = Deck;
                if (deck != null) deck.AcceptDrop(c);   // bırakıldığı yerden 0,32 s'de sırasına kayar
                else c.SetLift(0, 0, SettleS);
                HandBack(c, true);
                return;
            }
            c.SetLift(0, 0, SettleS);
            HandBack(c, true);
        }

        /// <summary>
        /// [B] Tarif kartının kabulü: iş emrine (kuyruk, değiştirme ya da form içi yer değiştirme) bırakıldığı serbest pozda oturur
        /// (<c>settleS</c>, "tık", mıknatıs görünür); deftere (iptal) kancadaki defterin arkasına <c>returnArcS</c>'de kayar ve gizlenir.
        /// </summary>
        void AcceptRecipe(BoardCardView c, BoardTarget t)
        {
            bool wasAdopted = c == adopted;
            Anim a;
            if (t.Kind == BoardTargetKind.Order)
            {
                var to = BoardLayout.MmToLocal(new Vector2(LastDropPose.x, LastDropPose.y), c.Target.z);
                c.MoveTo(to, LastDropPose.z, SettleS, CardEase.EaseIn);
                c.SetLift(0, 0, SettleS);
                a = AddAnim(c, BoardSound.Settle);
                a.MagnetAtEnd = true;
            }
            else
            {
                c.MoveTo(BookLocal(c), 0, ReturnArcS, CardEase.EaseInOut);
                c.SetLift(0, 0, ReturnArcS);
                a = AddAnim(c, BoardSound.Return);
                a.HideAtEnd = true;
            }
            if (!wasAdopted) return;
            a.Adopted = true;
            a.Accepted = true;
            FinishAdopt(true);
        }

        /// <summary>Kancadaki defterin (gövde merkezi) kartın çerçevesindeki yeri; z kartın kendi düzlemi (defterin arkasında kalır).</summary>
        Vector3 BookLocal(BoardCardView c)
        {
            var frame = CardFrame(c);
            var book = board != null ? board.Book : null;
            Vector3 local = book != null
                ? frame.InverseTransformPoint(book.transform.TransformPoint(new Vector3(0.0525f, 0.074f, 0)))
                : BoardLayout.MmToLocal(BoardLayout.Book, c.Target.z);
            local.z = c.Target.z;
            return local;
        }

        /// <summary>[B] Benimseme sonucu belli: durumu temizler ve <c>finished</c>'i bir kez çağırır.</summary>
        void FinishAdopt(bool accepted)
        {
            var f = adoptFinished;
            adopted = null;
            adoptReturn = null;
            adoptFinished = null;
            adoptBlendUntil = -1;
            f?.Invoke(accepted);
        }

        /// <summary>[B] Kopya sayfaya döner: <c>returnTo</c> pozuna 0,25 s kavis, sonunda gizlenir; defter hemen öne gelir (B9).</summary>
        void ReturnToBook(BoardCardView c)
        {
            Vector3 to = c.Target;
            float angle = c.TargetAngle;
            if (adoptReturn != null)
            {
                var pose = adoptReturn();
                var frame = CardFrame(c);
                to = frame.InverseTransformPoint(pose.position);
                angle = Mathf.DeltaAngle(0, (Quaternion.Inverse(frame.rotation) * pose.rotation).eulerAngles.z);
            }
            c.ArcTo(to, angle, ReturnArcS, HeldLiftMm);
            var a = AddAnim(c, BoardSound.Return);
            a.HideAtEnd = true;
            a.Adopted = true;
            FinishAdopt(false);
        }

        void Swallow(BoardCardView c)
        {
            if (hurda == null)
            {
                c.SetVisible(false);
                HandBack(c, true);
                return;
            }
            var a = AddAnim(c, null);
            a.WaitCallback = true;
            hurda.Swallow(c, () => a.Done = true);
            if (!hurdaFocusedByKeys) hurda.SetTargeted(false);
        }

        void StartReturn(BoardCardView c)
        {
            if (c == null) return;
            if (c == adopted) { ReturnToBook(c); return; }
            Vector3 to = homePos;
            float angle = homeAngle;
            var deck = Deck;
            if (heldFromDeck && deck != null && deck.TryReturnPose(c, out var deckPose)) { to = deckPose; angle = 0; }
            c.ArcTo(to, angle, ReturnArcS, HeldLiftMm);
            var a = AddAnim(c, BoardSound.Return);
            if (c.Kind == BoardCardKind.Recipe) a.MagnetAtEnd = true;
        }

        Anim AddAnim(BoardCardView c, BoardSound? endSound)
        {
            for (int i = anims.Count - 1; i >= 0; i--) if (anims[i].Card == c) anims.RemoveAt(i);
            var a = new Anim { Card = c, EndSound = endSound, ColliderWas = heldColliderWas };
            anims.Add(a);
            owned.Add(c);
            return a;
        }

        /// <summary>Kart panoya geri verilir: sahiplik biter, girdi eski hâline döner, pano son görüntüyle yeniden dizilir.</summary>
        void HandBack(BoardCardView c, bool relayout)
        {
            if (c == null) return;
            bool colliderWas = heldColliderWas;
            for (int i = anims.Count - 1; i >= 0; i--)
                if (anims[i].Card == c) { colliderWas = anims[i].ColliderWas; anims.RemoveAt(i); }
            owned.Remove(c);
            if (c.Collider != null && c.isActiveAndEnabled) c.SetInteractive(colliderWas);
            if (relayout) relayoutPending = true;
        }

        void FlushRelayout()
        {
            if (!relayoutPending) return;
            relayoutPending = false;
            if (board != null && board.isActiveAndEnabled && board.Last != null) board.Render(board.Last, true);
        }

        /// <summary>Tutulan kartı yayla yerine döndürür, komut yazılmaz; cevap bekleyen bırakmayı reddedilmiş sayar.</summary>
        public void Cancel()
        {
            if (held != null)
            {
                var c = held;
                EndHold();
                StartReturn(c);
                return;
            }
            if (waiting != null) Resolve(false);
        }

        // ------------------------------------------------------------------ tıklama yolu

        /// <summary>
        /// Bir hedefe tık (boş pano, BOŞTA, deste tepsisi, hurda; sahnedeki DeskTarget'lar da buraya bağlanabilir): Deste → el
        /// açılır/toplanır; BOŞTA → kişi seçiliyse dinlenmeye (<see cref="TargetClicked"/>), değilse el; boş → el toplanır ve plan
        /// açıksa <see cref="EmptyClicked"/>; başka hedef → seçim varsa <see cref="TargetClicked"/>, yoksa boş tık.
        /// </summary>
        public void ClickTarget(BoardTarget t) => ClickTarget(t, false);

        void ClickTarget(BoardTarget t, bool fromKeyboard)
        {
            if (held != null || waiting != null) return;
            if (suspended)
            {
                if (!fromKeyboard) BackgroundClicked?.Invoke();   // defter kipi: sabit hedefler de defteri kapatır
                return;
            }
            var sel = SelectedCard;
            switch (t.Kind)
            {
                case BoardTargetKind.Deck:
                    ToggleDeck();
                    return;
                case BoardTargetKind.Bosta:
                    if (sel != null && sel.Kind == BoardCardKind.Person) { CloseDeck(); TargetClicked?.Invoke(t); }
                    else ToggleDeck();
                    return;
                case BoardTargetKind.None:
                case BoardTargetKind.Empty:
                    if (!fromKeyboard) EmptyClick();
                    return;
                default:
                    if (sel == null) { if (!fromKeyboard) EmptyClick(); return; }
                    CloseDeck();
                    TargetClicked?.Invoke(t);
                    return;
            }
        }

        void EmptyClick()
        {
            CloseDeck();
            if (editable) EmptyClicked?.Invoke();   // kilitli yüzey boş tık saymaz (PlanningBoardCardTests sözleşmesi)
        }

        void ToggleDeck()
        {
            var deck = Deck;
            if (deck != null) deck.Toggle();
        }

        void CloseDeck()
        {
            var deck = Deck;
            if (deck != null && deck.IsOpen) deck.Close(true);
        }

        // ------------------------------------------------------------------ önizleme etiketi

        void ShowPreview(DropPreview p, Vector2 screen, bool now)
        {
            if (hoverTag == null) return;
            if (string.IsNullOrEmpty(p.Title)) { HidePreview(); return; }
            lastPreviewTitle = p.Title;
            previewTag.Set(p);
            if (hoverTag.Owner == previewTag && !now) hoverTag.SetAnchor(previewTag, screen);
            else hoverTag.ShowAt(previewTag, screen, now);
        }

        void HidePreview()
        {
            if (hoverTag != null) hoverTag.Hide(previewTag);
        }

        void ShowReason(DropPreview p, Vector2 screen)
        {
            if (hoverTag == null || string.IsNullOrEmpty(p.Title)) return;
            ShowPreview(p, screen, true);
            reasonUntil = Time.unscaledTime + ReasonS;
        }

        void HideReason()
        {
            if (reasonUntil < 0) return;
            reasonUntil = -1;
            HidePreview();
        }

        // ------------------------------------------------------------------ klavye

        /// <summary>Klavye odağını sıradaki (dir &gt; 0) ya da önceki hedefe taşır; seçim varsa önizleme hedefin yanında çıkar.</summary>
        public void StepKeyboardTarget(int dir)
        {
            if (dir == 0) return;
            int n = KeyboardTargets.Length;
            keyboardIndex = keyboardIndex < 0 ? (dir > 0 ? 0 : n - 1) : ((keyboardIndex + dir) % n + n) % n;
            ShowKeyboardFocus();
        }

        public void ClearKeyboardTarget()
        {
            bool had = keyboardIndex >= 0;
            keyboardIndex = -1;
            if (marks != null) marks.ClearFocus();
            if (hurdaFocusedByKeys && hurda != null) { hurda.SetFocused(false); hurda.SetTargeted(false); }
            hurdaFocusedByKeys = false;
            if (had && held == null && reasonUntil < 0) HidePreview();
        }

        /// <summary>Enter: seçili kart odaktaki hedefe (tıkla aynı kural; BOŞTA = dinlenme, seçim yoksa el açılır).</summary>
        public void ActivateKeyboardTarget()
        {
            var t = KeyboardTarget;
            if (t.IsNone) return;
            ClickTarget(t, true);
            HidePreview();
        }

        /// <summary>↑/↓: canlı kişiler arasında anlık görüntünün sırasıyla (Bd.People) seçer; dinlenenler de seçilir.</summary>
        public void StepSelection(int dir)
        {
            var s = Snap;
            if (s == null || dir == 0) return;
            int n = s.People.Count;
            if (n == 0) return;
            int cur = -1;
            for (int i = 0; i < n; i++) if (s.People[i] != null && s.People[i].Id == s.SelectedId) { cur = i; break; }
            int step = dir > 0 ? 1 : -1;
            for (int k = 1; k <= n; k++)
            {
                int i = cur < 0 ? (step > 0 ? k - 1 : n - k) : ((cur + step * k) % n + n) % n;
                var p = s.People[i];
                if (p == null || p.Lost || string.IsNullOrEmpty(p.Id)) continue;
                var card = board.CardFor(p.Id);
                if (card == null) continue;
                CardClicked?.Invoke(card);
                if (keyboardIndex >= 0) ShowKeyboardFocus();
                return;
            }
        }

        void ShowKeyboardFocus()
        {
            var t = KeyboardTarget;
            if (t.IsNone) return;
            Vector3 world;
            if (t.Kind == BoardTargetKind.Hurda)
            {
                if (marks != null) marks.ClearFocus();
                if (hurda != null)
                {
                    hurda.SetFocused(true);
                    hurda.SetTargeted(true);
                    hurdaFocusedByKeys = true;
                }
                world = hurda != null && hurda.Front != null ? hurda.Front.bounds.center : Frame.TransformPoint(BoardLayout.MmToLocal(BoardLayout.Hurda.center, BoardLayout.FaceZ));
            }
            else
            {
                if (hurdaFocusedByKeys && hurda != null) { hurda.SetFocused(false); hurda.SetTargeted(false); }
                hurdaFocusedByKeys = false;
                TargetRectMm(t, out var rect);
                if (marks != null) marks.ShowFocus(rect);
                world = Frame.TransformPoint(BoardLayout.MmToLocal(new Vector2(rect.xMax, rect.yMin), BoardLayout.FaceZ));
            }

            var sel = SelectedCard;
            var cam = Cam;
            if (sel != null && Preview != null && cam != null)
            {
                var sp = cam.WorldToScreenPoint(world);
                ShowPreview(Preview(sel, t), new Vector2(sp.x, sp.y), false);
            }
            else HidePreview();
        }

        void ReadKeyboard()
        {
            if (!readKeys || suspended || held != null || waiting != null || !Ready) return;
            if (board != null)
            {
                if (!board.Interactive) return;
                if ((board.Folder != null && board.Folder.IsOpen) || (board.LostEnvelope != null && board.LostEnvelope.IsOpen)) return;
            }
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.tabKey.wasPressedThisFrame) StepKeyboardTarget(kb.shiftKey.isPressed ? -1 : 1);
            else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) ActivateKeyboardTarget();
            else if (kb.upArrowKey.wasPressedThisFrame) StepSelection(-1);
            else if (kb.downArrowKey.wasPressedThisFrame) StepSelection(1);
        }

        // ------------------------------------------------------------------ yaşam döngüsü

        void OnEnable()
        {
            if (rig != null) rig.PoseChanged += OnPoseChanged;
        }

        void OnDisable()
        {
            if (rig != null) rig.PoseChanged -= OnPoseChanged;
            BoardCardView endedCopy = null;
            if (held != null)
            {
                var c = held;
                EndHold();
                owned.Remove(c);
                if (c == adopted)
                {
                    c.SetLift(0, 0, 0);
                    c.SetVisible(false);
                    endedCopy = c;
                    FinishAdopt(false);
                }
                else
                {
                    c.Place(homePos, homeAngle);
                    c.SetLift(0, 0, 0);
                    c.SetInteractive(heldColliderWas);
                }
            }
            if (waiting != null)
            {
                var c = waiting;
                waiting = null;
                waitingPartner = null;
                waitingTarget = BoardTarget.None;
                owned.Remove(c);
                c.SetLift(0, 0, 0);
                if (c == adopted)
                {
                    c.SetVisible(false);
                    endedCopy = c;
                    FinishAdopt(false);
                }
                else
                {
                    c.Place(homePos, homeAngle);
                    c.SetInteractive(heldColliderWas);
                }
            }
            var endedAnims = new List<Anim>();
            for (int i = 0; i < anims.Count; i++)
            {
                var a = anims[i];
                if (a.Card == null) continue;
                if (!a.WaitCallback) { a.Card.Place(a.Card.Target, a.Card.TargetAngle); a.Card.SetLift(0, 0, 0); }
                if (a.Card.Collider != null) a.Card.SetInteractive(a.ColliderWas);
                if (a.HideAtEnd) a.Card.SetVisible(false);
                if (a.MagnetAtEnd) a.Card.SetMagnetVisible(true);
                owned.Remove(a.Card);
                if (a.Adopted) endedAnims.Add(a);
            }
            anims.Clear();
            if (adopted != null) FinishAdopt(false);
            if (endedCopy != null) AdoptEnded?.Invoke(endedCopy, false);
            foreach (var a in endedAnims) AdoptEnded?.Invoke(a.Card, a.Accepted);
            owned.Clear();
            partnerWatch = null;
            keepSuspendedUntilRightUp = false;
            ReleaseRig();
            ClearKeyboardTarget();
            reasonUntil = -1;
            HidePreview();
            relayoutPending = false;
        }

        void OnPoseChanged(SiginakPose pose)
        {
            if (pose == SiginakPose.Shelf) return;
            Cancel();                 // '3', '4/K', CRT, vardiya dizisi: kart yayla yerine
            ClearKeyboardTarget();
            HideReason();
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (held != null && !AbortIfGone())
            {
                if (!Ready || !editable) Cancel();
                else
                {
                    var mouse = Mouse.current;
                    if (mouse != null && mouse.rightButton.wasPressedThisFrame) CancelByRight();
                }
            }
            if (waiting != null && now - waitingSince > WaitTimeoutS) Resolve(false);

            if (keepSuspendedUntilRightUp)
            {
                var mouse = Mouse.current;
                if (mouse == null || !mouse.rightButton.isPressed)
                {
                    keepSuspendedUntilRightUp = false;
                    ReleaseRig();
                }
            }

            StepAnims();
            if (partnerWatch != null && !partnerWatch.Moving)
            {
                partnerWatch = null;
                Emit(BoardSound.Settle);
            }
            if (reasonUntil >= 0 && now >= reasonUntil) HideReason();

            if (held == null) ReadKeyboard();
            if (keyboardIndex >= 0 && !Ready) ClearKeyboardTarget();
            FlushRelayout();
        }

        void StepAnims()
        {
            for (int i = anims.Count - 1; i >= 0; i--)
            {
                if (i >= anims.Count) continue;
                var a = anims[i];
                if (a.Card == null) { anims.RemoveAt(i); continue; }
                bool done = a.WaitCallback ? a.Done : !a.Card.Moving;
                if (!done) continue;
                anims.RemoveAt(i);
                owned.Remove(a.Card);
                if (a.Card.Collider != null && a.Card.isActiveAndEnabled) a.Card.SetInteractive(a.ColliderWas);
                if (a.MagnetAtEnd) a.Card.SetMagnetVisible(true);
                if (a.HideAtEnd) a.Card.SetVisible(false);
                relayoutPending = true;
                if (a.EndSound.HasValue) Emit(a.EndSound.Value);
                if (a.Adopted) AdoptEnded?.Invoke(a.Card, a.Accepted);
            }
        }
    }
}
