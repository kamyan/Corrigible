using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kabul istasyonu (Revizyon 3 · Aşama 6, R17; PLAN §1.6–§1.11): tezgâh (Top, BracketL/R), tezgâh lambası (cam yuvası 4 +
    /// <c>CounterSpot</c> GameObject'i), evrak çekmecesi, talimat işaretçisi (Marker + Jewel), şerit yuvası ve ESKİ cebi, BULGU ağzı ve
    /// kıskaç yelpazesi, belgenin iki yeri (çekmece tablası ↔ tezgâhtaki <c>Doc</c>), erzak tenekeleri, MASA tuşu ve gevşek araçların
    /// görünürlüğü. Yalnız çizer ve olay yayar: komut yazmaz, ses çalmaz (<see cref="Sound"/>), simülasyonu okumaz — durum
    /// <see cref="AdmissionSnapshot"/>'tan gelir (<see cref="Bind"/>).
    /// <list type="bullet">
    /// <item>T1 (iniş): konsollar ∓90 → 0 (<c>counterBracketS</c> 0,15) → Top 88 → 0 (<c>counterLeafS</c> 0,4; sonunda "tank") → lamba
    /// (<c>lampS</c> 0,08). T2: aday varsa çekmece 0 → −0,22 (<c>drawerS</c> 0,35), belge tablada. T3: belge tabladan <c>Doc</c>'a
    /// (<c>docDropS</c> 0,2), çekmece kapanır. T4 (katlanış): lamba söner (0,08) → Top 0 → 88 (<c>counterFoldS</c> 0,35) → konsollar
    /// (0,15). T5: <see cref="Bind"/>(…, false) animasyonsuz son duruma oturur.</item>
    /// <item>Gevşek araçlar T4 başında gizlenir, T1'de Top 30°'nin altına inince görünür (katlı kanatta panelle çakışırlar).</item>
    /// <item>Fiş ağızdan <c>slipS</c> 0,35 s'de çıkar (yol <c>slipPathMm</c> 160: ağzın 40 mm içinden başlar), kıskaca <c>slipDropS</c>
    /// 0,15 s'de düşer; aynı anahtarlı fiş ikinci kez çıkmaz. Aynı anda gelen fişler sırayla çıkar.</item>
    /// <item>İşaretçi <c>markerS</c> 0,2 s'de maddenin hizasına (<c>y = −(RuleCenterMm(i) − 18) / 1000</c>, alt sınır −0,17), yakut
    /// yanar; talimat değişince en üste döner.</item>
    /// </list>
    /// Açılar ve kaymalar her karede görünümden <see cref="PartDriver.Set"/> ile yazılır (<c>AnimateTo</c> doğrusaldır; uygulanan değer
    /// tam eşitlikle karşılaştırılır — GELISTIRME "Approximately son adımı yutar"). Edit modda <c>Update</c> çalışmaz: <see cref="Advance"/>.
    /// Çarpıştırıcılar yalnız etkileşimliyken ve ilgili nesne görünürken açık (işleyicisiz alt çarpıştırıcı ışını yutmasın).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AdmissionStation : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler,
        IPointerDownHandler, IPointerClickHandler
    {
        [SerializeField] HoverTag hoverTag;

        [Header("Tezgâh (AdmissionCounter)")]
        [Tooltip("AdmissionCounter model kökü (model uzayı; kıskaç yelpazesi ve okuma yönü bunun çerçevesinde).")]
        [SerializeField] Transform counterBody;
        [SerializeField] PartDriver top, bracketL, bracketR;
        [Tooltip("Tezgâh kanadının çarpıştırıcısı: üst yüzü keçeyi geçmemeli (kâğıt ve araç çarpıştırıcıları üstte kalsın).")]
        [SerializeField] Collider counterHit;
        [Tooltip("Tezgâhtaki MASA tuşu (KeyKey ikizi) çarpıştırıcısı.")]
        [SerializeField] Collider deskKeyHit;

        [Header("Tezgâh lambası (CounterLamp)")]
        [SerializeField] Renderer lampBody;
        [SerializeField] int lampSlot = 4;
        [SerializeField] Material lampOn, lampOff;
        [Tooltip("CounterSpot ışığının GameObject'i: istasyon durumu buradan girer (PoseLighting Light.enabled'ı pozla yazar).")]
        [SerializeField] GameObject counterSpot;

        [Header("Panel (AdmissionPanel)")]
        [Tooltip("AdmissionPanel model kökü (model uzayı).")]
        [SerializeField] Transform panel;
        [SerializeField] PartDriver drawer, marker, jewel;

        [Header("Kâğıtlar (kökler; yüz bileşeni aynı nesnede)")]
        [SerializeField] Transform documentPaper;
        [SerializeField] Transform stripPaper;
        [SerializeField] Transform[] archivePapers = new Transform[0];
        [Tooltip("Fiş havuzu (5 × FindingSlip): dört araç + İNCELEME FİŞİ.")]
        [SerializeField] Transform[] slipPapers = new Transform[0];

        [Header("Çekmece ve araçlar")]
        [SerializeField] GameObject[] tins = new GameObject[0];
        [Tooltip("Top'un gevşek çocukları (damga rafı ve damgalar, büyüteç, talep defteri, kalem, MASA tuşu ve plakası).")]
        [SerializeField] GameObject[] looseTools = new GameObject[0];
        [SerializeField] StampDecision[] stamps = new StampDecision[0];
        [SerializeField] MagnifierTool magnifier;
        [SerializeField] RequestPadTool pad;

        // ------------------------------------------------------------------ geometri (model/manifest, DA)

        /// <summary>Kanadın katlı açısında gevşek araçların göründüğü sınır (derece; DA:111 araçları yalnız inikken kurar).</summary>
        public const float ToolsVisibleBelowDeg = 30f;
        /// <summary>Belge tezgâhta: Top yereli (0,02; 0,0043; −0,13), −3° (DA:142 <c>DOC</c>).</summary>
        public static readonly Vector3 DocOnCounterLocal = new Vector3(0.02f, 0.0043f, -0.13f);
        public const float DocOnCounterRot = -3f;
        /// <summary>Belge çekmecede: Drawer yereli (model (−0,26; 0,0248; 0,08) − pivot (−0,26; 0,057; −0,034)), 3° (DA:139).</summary>
        public static readonly Vector3 DocInDrawerLocal = new Vector3(0f, -0.0322f, 0.114f);
        public const float DocInDrawerRot = 3f;
        /// <summary>Belge 148 × 105 mm.</summary>
        public static readonly Vector2 DocSizeMm = new Vector2(148f, 105f);
        /// <summary>Damga alanı: belge yerelinde (−12; −31) mm, 60 × 35 mm (DA:145; PLAN §1.10); iz belgeye göre −4°.</summary>
        public static readonly Vector2 StampAreaCenterMm = new Vector2(-12f, -31f);
        public static readonly Vector2 StampAreaSizeMm = new Vector2(60f, 35f);
        public const float ImpressionRot = -4f;
        /// <summary>Ağızdan çıkmış fiş: panel yereli (0,02; 0,032; −0,064), normal (0; 0,94; −0,34), üst (0; 0,34; 0,94) (DA:162).</summary>
        public static readonly Vector3 SlipMouthOut = new Vector3(0.02f, 0.032f, -0.064f);
        public static readonly Vector3 SlipMouthNormal = new Vector3(0f, 0.94f, -0.34f);
        public static readonly Vector3 SlipMouthUp = new Vector3(0f, 0.34f, 0.94f);
        /// <summary>Şerit yuvası: panel yereli <c>TXT_Strip</c> (0,185; 0,155; −0,0187) + 0,4 mm öne; 80 × 210 mm.</summary>
        public static readonly Vector3 StripSlotLocal = new Vector3(0.185f, 0.155f, -0.0191f);
        public static readonly Vector2 StripSizeMm = new Vector2(80f, 210f);
        /// <summary>ESKİ cebine giren şeridin ara pozu (DA:105–106): (0,33; 0,215; −0,034), 6°, normal (0; 0,17; −0,985).</summary>
        public static readonly Vector3 StripTransitLocal = new Vector3(0.33f, 0.215f, -0.034f);
        public static readonly Vector3 StripTransitNormal = new Vector3(0f, 0.17f, -0.985f);
        public static readonly Vector3 StripTransitUp = new Vector3(0f, 0.985f, 0.17f);
        public const float StripTransitRot = 6f;
        /// <summary>İşaretçinin sıfırı şeridin üst kenarının bu kadar altında (pivot 0,242; şerit üstü 0,155 + 0,105).</summary>
        public const float MarkerZeroBelowStripTopMm = 18f;
        /// <summary>K8R: tezgâh kâğıtları için okuma yönü, tezgâh model uzayında (token K8R counterLocalPos − counterLocalTarget).</summary>
        public static readonly Vector3 CounterReadDirection = new Vector3(0f, 0.4f, -0.1f);

        /// <summary>ESKİ cebindeki <paramref name="i"/>. şerit (panel yereli; DA:102–104).</summary>
        public static Vector3 ArchiveLocal(int i) => new Vector3(0.321f + i * 0.007f, 0.115f + i * 0.016f, -0.030f + i * 0.007f);
        public static float ArchiveRot(int i) => i == 0 ? -1.5f : 2f;

        /// <summary>
        /// Kıskaçtaki <paramref name="n"/> fişin <paramref name="i"/>.'si (tezgâh model uzayı; DA:158–161). Yükseklik DA'nın 4 mm'si yerine
        /// 6 mm: kıskaç tabanının önündeki BULGULAR yazısı (y 5,7 mm) fişlerin altında kalmalı (R17_pending'de fişler yazıyı örter).
        /// Katman adımı DA'nın 0,4 mm'si yerine 1 mm (<see cref="ClipStackStepM"/>).
        /// </summary>
        public static Vector3 ClipLocal(int i, int n) => new Vector3(0.02f + (i - (n - 1) * 0.5f) * 0.014f, 0.0060f + i * ClipStackStepM, -0.078f - i * 0.004f);

        /// <summary>
        /// Kıskaçta üst üste binen fişlerin yükseklik adımı (m). Yazı yüzü kâğıdın 0,4 mm önünde (<c>FindingSlip/Face</c>): adım 0,4 mm olunca
        /// alttaki fişin yazısı üstteki fişin kâğıdıyla aynı düzleme düşüp içinden okunuyordu (A10 G09).
        /// </summary>
        public const float ClipStackStepM = 0.001f;

        /// <summary>Tezgâh lambası spotunun hedefi (tezgâh model uzayı; DA:214 <c>admWorld([0.04, 0, -0.21])</c>): koni tezgâhın ortasına bakar.</summary>
        public static readonly Vector3 CounterSpotTargetLocal = new Vector3(0.04f, 0f, -0.21f);

        /// <summary>Spotu tasarımdaki hedefe çevirir (model prefab'ındaki ışık dik aşağı bakar; tasarımda tezgâh ortasına, ~11° eğik).</summary>
        void AimCounterSpot()
        {
            if (counterSpot == null || counterBody == null) return;
            var t = counterSpot.transform;
            var dir = counterBody.TransformPoint(CounterSpotTargetLocal) - t.position;
            if (dir.sqrMagnitude < 1e-8f) return;
            var rot = Quaternion.LookRotation(dir, Vector3.up);
            if (t.rotation != rot) t.rotation = rot;
        }
        public static float ClipRot(int i) => (i % 2 == 1 ? 4f : -3f) + i;

        // ------------------------------------------------------------------ token

        public static float BracketS => BoardViewUtil.Num("admission.motion.counterBracketS", 0.15f);
        public static float LeafS => BoardViewUtil.Num("admission.motion.counterLeafS", 0.4f);
        public static float LampS => BoardViewUtil.Num("admission.motion.lampS", 0.08f);
        public static float FoldS => BoardViewUtil.Num("admission.motion.counterFoldS", 0.35f);
        public static float DrawerS => BoardViewUtil.Num("admission.motion.drawerS", 0.35f);
        public static float AidDrawerS => BoardViewUtil.Num("admission.motion.aidDrawerS", 0.35f);
        public static float DocDropS => BoardViewUtil.Num("admission.motion.docDropS", 0.2f);
        public static float MarkerS => BoardViewUtil.Num("admission.motion.markerS", 0.2f);
        public static float SlipS => BoardViewUtil.Num("admission.motion.slipS", 0.35f);
        public static float SlipDropS => BoardViewUtil.Num("admission.motion.slipDropS", 0.15f);
        public static float StripOutS => BoardViewUtil.Num("admission.motion.stripOutS", 0.25f);
        public static float StripInS => BoardViewUtil.Num("admission.motion.stripInS", 0.3f);
        public static float HoldS => BoardViewUtil.Num("admission.motion.admitHoldS", 0.6f);
        public static float TranscriptPerCharS => BoardViewUtil.Num("admission.motion.transcriptPerCharS", 0.03f);
        public static float SlipPathMm => BoardViewUtil.Num("admission.slipPathMm", 160f);
        public static float HoverLiftMm => BoardViewUtil.Num("motion.hoverLiftMm", 4f);
        public static float HoverS => BoardViewUtil.Num("motion.board.cardLiftS", 0.08f);
        /// <summary>T1 toplamı: konsollar + kanat + lamba (0,63 s).</summary>
        public static float UnfoldTotalS => BracketS + LeafS + LampS;
        /// <summary>T4 toplamı: lamba + kanat + konsollar (0,58 s).</summary>
        public static float FoldTotalS => LampS + FoldS + BracketS;

        // ------------------------------------------------------------------ olaylar

        /// <summary>İnik tezgâha tıklandı (K8 → K8A).</summary>
        public event Action CounterClicked;
        /// <summary>MASA tuşu (doğrudan K1).</summary>
        public event Action DeskKeyClicked;
        /// <summary>Okunabilir bir kâğıda tıklandı (K8R); dizin aynı türdeki kâğıdın sırası (fişte kıskaç sırası, alttan).</summary>
        public event Action<AdmissionPaper, int> PaperClicked;
        public event Action<AdmissionSound> Sound;
        /// <summary>Belge çekmeceden tezgâha alınmaya başladı (T3; tık, ilk araç ya da damga). Sunucu <c>AdmissionUi</c>'ye yazar.</summary>
        public event Action DocumentTaken;

        // ------------------------------------------------------------------ durum

        enum DocLoc { Hidden, Drawer, Counter, Fixed }
        enum SlipLoc { Inside, Mouth, Clip, Stow, Fixed }
        enum StripLoc { Slot, Transit, Above, Fixed }
        enum Part { None, Counter, DeskKey, Document, Strip, Slip }

        sealed class SlipSlot
        {
            public Transform paper;
            public FindingSlipFace face;
            public Collider hit;
            public string key;
            public SlipData data;
            public SlipLoc from, to;
            public Pose fixedFrom;
            public readonly AdmissionTween t = new AdmissionTween(0f);
            public int clip, stow;
            public bool visible, queued, emerging, landed;
            public int gen;
        }

        readonly AdmissionClock clock = new AdmissionClock();
        readonly AdmissionTween brackets = new AdmissionTween(1f), topDeg = new AdmissionTween(88f), drawerZ = new AdmissionTween(0f),
            markerY = new AdmissionTween(0f), docT = new AdmissionTween(1f), stripT = new AdmissionTween(1f), hover = new AdmissionTween(0f);
        readonly List<SlipSlot> slots = new List<SlipSlot>();
        readonly List<string> clipOrder = new List<string>();

        AdmissionDocumentFace documentFace;
        OrdersStripFace stripFace;
        OrdersStripFace[] archiveFaces = new OrdersStripFace[0];
        Collider documentHit, stripHit;
        StampDecision[] subscribed = new StampDecision[0];

        bool initialized, bound, interactive, muted, sequence, ordersChanging, toolsShown, lampLit, tinsShown, hovered, reading;
        CounterState counter = CounterState.Folded;
        int counterGen, sequenceGen;
        DocLoc docFrom = DocLoc.Hidden, docTo = DocLoc.Hidden;
        Pose docFixed;
        StripLoc stripFrom = StripLoc.Slot, stripTo = StripLoc.Slot;
        Pose stripFixed;
        float emergeFreeAt;
        int markerRule = -1;
        bool jewelOn;
        string boundCandidate;
        OrdersData boundOrders;
        AdmissionSnapshot latest;
        Action onDown;
        GameObject pressGo;
        Part hoverPart;
        int hoverSlip = -1;
        float appliedTop = float.NaN, appliedBracket = float.NaN, appliedDrawer = float.NaN, appliedMarker = float.NaN, appliedHover = float.NaN;

        // ------------------------------------------------------------------ genel okuma

        public CounterState Counter => counter;
        /// <summary>Tezgâh, çekmece, belge, şerit ya da fiş hareket ediyor (ya da bir dizi sürüyor).</summary>
        public bool Moving => sequence || ordersChanging || clock.Pending || brackets.Moving || topDeg.Moving || drawerZ.Moving || markerY.Moving
            || docT.Moving || stripT.Moving || SlipsMoving;
        /// <summary>Karar dizisi sürüyor (<see cref="PlayDecision(StampChoice, Action)"/>).</summary>
        public bool SequenceActive => sequence;
        public bool OrdersChanging => ordersChanging;
        public bool Interactive => interactive;
        public HoverTag Tag { get => hoverTag; set => hoverTag = value; }

        public string HoverTitle => HoverText(true);
        public string Line => HoverText(false);

        /// <summary>Konsolların katlanma oranı (0 açık, 1 duvara yatık), kanat açısı (0 inik, 88 katlı), çekmece kayması (m), işaretçi (m).</summary>
        public float BracketProgress => brackets.Value;
        public float TopAngle => topDeg.Value;
        public float DrawerOffset => drawerZ.Value;
        public float MarkerOffset => markerY.Value;
        public bool LampOn => lampLit;
        public bool ToolsVisible => toolsShown;
        public bool TinsVisible => tinsShown;
        public int MarkerRule => markerRule;
        public bool JewelOn => jewelOn;
        public bool DocumentVisible => docTo != DocLoc.Hidden;
        public bool DocumentOnCounter => docTo == DocLoc.Counter && !docT.Moving;
        public bool DocumentInDrawer => docTo == DocLoc.Drawer && !docT.Moving;
        /// <summary>Kıskaçtaki (ya da çıkmakta olan) fişlerin anahtarları, kıskaç sırasıyla.</summary>
        public IReadOnlyList<string> SlipKeys => clipOrder;
        public Transform DocumentPaper => documentPaper;
        public Transform StripPaper => stripPaper;
        public IReadOnlyList<Transform> SlipPapers => slipPapers;
        public IReadOnlyList<StampDecision> Stamps => stamps;
        public MagnifierTool Magnifier => magnifier;
        public RequestPadTool Pad => pad;
        public PartDriver Top => top;
        public PartDriver Drawer => drawer;
        public PartDriver MarkerPart => marker;
        public PartDriver Jewel => jewel;
        /// <summary>İşaretçi hizası: maddenin şerit üstünden mm'si. Boşsa şerit yüzünün <c>RuleCenterMm</c>'i (testler değiştirebilir).</summary>
        public Func<int, float> RuleCenterMm { get; set; }

        float FoldedDeg => top != null && top.RangeTo != 0 ? top.RangeTo : 88f;
        float DrawerOpen => drawer != null && drawer.RangeTo != 0 ? drawer.RangeTo : -0.22f;
        float MarkerMin => marker != null && marker.RangeTo != 0 ? marker.RangeTo : -0.17f;
        bool Configured => top != null || drawer != null;
        static bool Blocked => DeskTarget.Blocked != null && DeskTarget.Blocked();

        // ------------------------------------------------------------------ kurulum

        /// <summary>Tezgâh bağları (tek seferlik sahne script'i ve testler).</summary>
        public void ConfigureCounter(Transform counterModel, PartDriver topPart, PartDriver bracketLeft, PartDriver bracketRight, Collider counterCollider, Collider deskKeyCollider)
        {
            counterBody = counterModel; top = topPart; bracketL = bracketLeft; bracketR = bracketRight; counterHit = counterCollider; deskKeyHit = deskKeyCollider;
            Reinit();
        }

        /// <summary>Tezgâh lambası: gövde renderer'ı, cam yuvası (4), açık/sönük cam malzemeleri, <c>CounterSpot</c> GameObject'i.</summary>
        public void ConfigureLamp(Renderer body, int slot, Material on, Material off, GameObject spot)
        {
            lampBody = body; lampSlot = slot; lampOn = on; lampOff = off; counterSpot = spot;
            Reinit();
        }

        public void ConfigurePanel(Transform panelModel, PartDriver drawerPart, PartDriver markerPart, PartDriver jewelPart)
        {
            panel = panelModel; drawer = drawerPart; marker = markerPart; jewel = jewelPart;
            Reinit();
        }

        /// <summary>Kâğıt kökleri: belge, şerit, ESKİ cebi (en çok 2), fiş havuzu (5). Yüz bileşenleri ve çarpıştırıcılar köklerden okunur.</summary>
        public void ConfigurePapers(Transform document, Transform strip, Transform[] archive, Transform[] slips)
        {
            documentPaper = document; stripPaper = strip; archivePapers = archive ?? new Transform[0]; slipPapers = slips ?? new Transform[0];
            Reinit();
        }

        public void ConfigureTools(GameObject[] loose, StampDecision[] stampList, MagnifierTool magnifierTool, RequestPadTool padTool, GameObject[] rationTins)
        {
            looseTools = loose ?? new GameObject[0]; stamps = stampList ?? new StampDecision[0]; magnifier = magnifierTool; pad = padTool;
            tins = rationTins ?? new GameObject[0];
            Reinit();
        }

        void Reinit()
        {
            initialized = false;
            bound = false;
            EnsureInit();
        }

        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            documentFace = Face<AdmissionDocumentFace>(documentPaper);
            documentHit = Hit(documentPaper);
            stripFace = Face<OrdersStripFace>(stripPaper);
            stripHit = Hit(stripPaper);
            archiveFaces = new OrdersStripFace[archivePapers.Length];
            for (int i = 0; i < archivePapers.Length; i++) archiveFaces[i] = Face<OrdersStripFace>(archivePapers[i]);
            slots.Clear();
            foreach (var p in slipPapers)
            {
                if (p == null) continue;
                slots.Add(new SlipSlot { paper = p, face = Face<FindingSlipFace>(p), hit = Hit(p) });
            }
            clipOrder.Clear();

            foreach (var s in subscribed) if (s != null) s.Lifted -= OnStampLifted;
            subscribed = stamps ?? new StampDecision[0];
            foreach (var s in subscribed)
            {
                if (s == null) continue;
                s.Lifted += OnStampLifted;
                s.DropAccepts = StampAreaContains;
                s.PressTarget = StampPressPose;
                if (s.Tag == null) s.Tag = hoverTag;
            }
            if (magnifier != null)
            {
                magnifier.DropAccepts = DocumentContains;
                if (magnifier.Tag == null) magnifier.Tag = hoverTag;
            }
            if (pad != null && pad.Tag == null) pad.Tag = hoverTag;

            // Başlangıç: katlı, kâğıtsız (Bind gelene dek)
            counter = CounterState.Folded;
            brackets.Snap(1f); topDeg.Snap(FoldedDeg); drawerZ.Snap(0f); markerY.Snap(0f); hover.Snap(0f);
            docFrom = docTo = DocLoc.Hidden; docT.Snap(1f);
            stripFrom = stripTo = StripLoc.Slot; stripT.Snap(1f);
            toolsShown = false; lampLit = false; tinsShown = false; sequence = false; ordersChanging = false; jewelOn = false; markerRule = -1;
            appliedTop = appliedBracket = appliedDrawer = appliedMarker = appliedHover = float.NaN;
            ApplyAll(true);
        }

        static T Face<T>(Transform root) where T : Component
        {
            if (root == null) return null;
            var c = root.GetComponent<T>();
            return c != null ? c : root.GetComponentInChildren<T>(true);
        }

        static Collider Hit(Transform root)
        {
            if (root == null) return null;
            var c = root.GetComponent<Collider>();
            return c != null ? c : root.GetComponentInChildren<Collider>(true);
        }

        // ------------------------------------------------------------------ bağlama

        /// <summary>
        /// Anlık görüntüyü uygular. <paramref name="animate"/> true ise durum farkından hareket kurar (tezgâh iner/katlanır, yeni aday
        /// belgesi çekmeceyle gelir, yeni anahtarlı fiş ağızdan çıkar, işaretçi kayar); false ise her şey son durumuna oturur (T5) ve
        /// süren diziler sona atlar. Karar dizisi sürerken tezgâh, belge ve fişler diziye aittir; son görüntü dizi bitince uygulanır.
        /// İlk bağlama hep animasyonsuzdur.
        /// </summary>
        public void Bind(AdmissionSnapshot s, bool animate)
        {
            EnsureInit();
            if (!bound) animate = false;
            bound = true;
            if (!animate && (clock.Pending || sequence || ordersChanging)) SnapAll();
            latest = s;
            if (sequence) { BindOrders(s, animate); ApplyAll(false); return; }

            var target = s != null && (s.Counter == CounterState.Down || s.Counter == CounterState.Lowering) ? CounterState.Down : CounterState.Folded;
            if (!animate) SnapCounter(target);
            else if (target == CounterState.Down && (counter == CounterState.Folded || counter == CounterState.Folding)) StartUnfold();
            else if (target == CounterState.Folded && (counter == CounterState.Down || counter == CounterState.Lowering)) StartFold(clock.Now);

            BindDocument(s, animate);
            BindOrders(s, animate);
            BindSlips(s, animate);
            ApplyAll(false);
        }

        void BindDocument(AdmissionSnapshot s, bool animate)
        {
            var doc = s?.Document;
            string id = doc != null ? s.CandidateId : null;
            if (doc == null)
            {
                boundCandidate = null;
                if (docTo != DocLoc.Hidden) HideDocument();
                if (animate && drawerZ.Target != 0f && !tinsShown) MoveDrawer(0f, DrawerS, clock.Now, true);
                else if (!animate) drawerZ.Snap(0f);
                return;
            }
            if (documentFace != null) documentFace.Bind(doc);
            if (documentFace != null) documentFace.ShowImpression(doc.StampChoiceId != null, false);
            bool newCandidate = id != boundCandidate;
            boundCandidate = id;
            if (newCandidate || docTo == DocLoc.Hidden)
            {
                bool onCounter = s.DocumentOnCounter;
                SnapDocument(onCounter ? DocLoc.Counter : DocLoc.Drawer);
                if (onCounter) { if (animate) MoveDrawer(0f, DrawerS, clock.Now, true); else drawerZ.Snap(0f); }
                else if (!animate) drawerZ.Snap(counter == CounterState.Down ? DrawerOpen : 0f);
                else if (counter == CounterState.Down) OpenDrawerForDocument(clock.Now);
                // iniyorsa: T1 sonunda (onDown) açılır
                return;
            }
            if (s.DocumentOnCounter && docTo == DocLoc.Drawer)
            {
                if (animate) TakeDocument(); else { SnapDocument(DocLoc.Counter); drawerZ.Snap(0f); }
            }
        }

        void BindOrders(AdmissionSnapshot s, bool animate)
        {
            var orders = s?.Orders;
            if (ordersChanging) return;
            bool keepOld = animate && s != null && s.OrdersUnseen && boundOrders != null && orders != null && boundOrders.Version != orders.Version;
            if (!keepOld)
            {
                boundOrders = orders;
                if (stripFace != null) stripFace.Bind(orders);
                BindArchive(s, 0);
            }
            SetMarkerRule(s != null ? s.MarkerRule : -1, animate && !keepOld);
        }

        void BindArchive(AdmissionSnapshot s, int skip)
        {
            for (int i = 0; i < archivePapers.Length; i++)
            {
                int k = i + skip;
                var data = s != null && k < s.Archive.Count ? s.Archive[k] : null;
                var p = archivePapers[i];
                if (p == null) continue;
                if (data != null && archiveFaces[i] != null) archiveFaces[i].BindHead(data);
                SetActive(p.gameObject, data != null);
            }
        }

        void SetMarkerRule(int rule, bool animate)
        {
            markerRule = rule;
            float target = MarkerTarget(rule);
            bool on = rule >= 0;
            if (animate && target != markerY.Target)
            {
                markerY.Go(target, MarkerS, CardEase.EaseInOut, clock.Now);
                if (on) Emit(AdmissionSound.Marker);
            }
            else if (!animate) markerY.Snap(target);
            jewelOn = on;
        }

        float MarkerTarget(int rule)
        {
            if (rule < 0) return 0f;
            float mm = RuleCenterMm != null ? RuleCenterMm(rule) : stripFace != null ? stripFace.RuleCenterMm(rule) : MarkerZeroBelowStripTopMm;
            return Mathf.Clamp(-(mm - MarkerZeroBelowStripTopMm) / 1000f, MarkerMin, 0f);
        }

        void BindSlips(AdmissionSnapshot s, bool animate)
        {
            var list = s != null ? s.Slips : null;
            var keep = new HashSet<string>();
            clipOrder.Clear();
            if (list != null)
                foreach (var d in list)
                {
                    if (d == null || string.IsNullOrEmpty(d.Key)) continue;
                    var slot = SlotOf(d.Key);
                    // Talimat değişimi oynamadan İNCELEME FİŞİ çıkmaz (PlayOrdersChange çıkarır)
                    if (slot == null && animate && d.Review && s.OrdersUnseen) continue;
                    if (slot == null)
                    {
                        slot = FreeSlot();
                        if (slot == null) continue;
                        Assign(slot, d);
                        if (animate) Emerge(slot); else LandSlot(slot);
                    }
                    else
                    {
                        slot.data = d;
                        if (slot.face != null) slot.face.Bind(d);
                    }
                    keep.Add(d.Key);
                    clipOrder.Add(d.Key);
                }
            foreach (var slot in slots)
                if (slot.key != null && !keep.Contains(slot.key)) FreeSlotNow(slot);
            Reclip();
        }

        SlipSlot SlotOf(string key)
        {
            foreach (var s in slots) if (s.key == key) return s;
            return null;
        }

        SlipSlot FreeSlot()
        {
            foreach (var s in slots) if (s.key == null) return s;
            return null;
        }

        void Assign(SlipSlot slot, SlipData d)
        {
            slot.key = d.Key;
            slot.data = d;
            slot.gen++;
            if (slot.face != null) slot.face.Bind(d);
        }

        void FreeSlotNow(SlipSlot slot)
        {
            slot.key = null;
            slot.data = null;
            slot.gen++;
            slot.visible = slot.queued = slot.emerging = slot.landed = false;
            slot.t.Snap(0f);
            if (slot.paper != null) SetActive(slot.paper.gameObject, false);
        }

        void Reclip()
        {
            foreach (var slot in slots)
                if (slot.key != null) slot.clip = Mathf.Max(0, clipOrder.IndexOf(slot.key));
        }

        void LandSlot(SlipSlot slot)
        {
            slot.gen++;
            slot.queued = slot.emerging = false;
            slot.landed = slot.visible = true;
            slot.from = slot.to = SlipLoc.Clip;
            slot.t.Snap(1f);
            if (slot.face != null) slot.face.Type(0f);
        }

        /// <summary>Fişi ağızdan çıkarır (önceki fiş çıkarken sıraya girer).</summary>
        void Emerge(SlipSlot slot)
        {
            int gen = ++slot.gen;
            float start = Mathf.Max(clock.Now, emergeFreeAt);
            emergeFreeAt = start + SlipS + SlipDropS;
            slot.queued = true;
            slot.visible = false;
            slot.emerging = true;
            slot.landed = false;
            slot.from = SlipLoc.Inside; slot.to = SlipLoc.Mouth;
            slot.t.Snap(0f);
            clock.At(start, at =>
            {
                if (slot.gen != gen) return;
                slot.queued = false;
                slot.visible = true;
                slot.t.Go(1f, SlipS, CardEase.EaseOut, at);
                Emit(AdmissionSound.SlipOut);
            });
            clock.At(start + SlipS, at =>
            {
                if (slot.gen != gen) return;
                if (HeldByRuler(slot))
                {
                    // R17_inspect: cetvel belgedeyken karşılaştırma fişi ağızda kalır; bırakma SyncRulerHold'da
                    slot.emerging = false;
                    slot.landed = true;
                    return;
                }
                slot.from = SlipLoc.Mouth; slot.to = SlipLoc.Clip;
                slot.t.Snap(0f);
                slot.t.Go(1f, SlipDropS, CardEase.EaseIn, at);
            });
            clock.At(start + SlipS + SlipDropS, at =>
            {
                if (slot.gen != gen) return;
                slot.emerging = false;
                slot.landed = true;
            });
        }

        /// <summary>
        /// Cetvel belgenin üstünde ya da yolda. İnceleme pozundaki 34 mm'lik cetvel kıskaçtaki fişin alt 13 mm'sini (not ve sonuç damgası)
        /// örter; tasarımda bu anda fiş ağızdadır (DA:158–165, R17_inspect).
        /// </summary>
        bool RulerOnDocument => magnifier != null && (magnifier.State == MagnifierTool.MagnifierState.ToInspect || magnifier.State == MagnifierTool.MagnifierState.Inspect);

        bool HeldByRuler(SlipSlot slot) => RulerOnDocument && slot.data != null && slot.data.Tool == AdmissionTool.Papers;

        /// <summary>
        /// A10 G06: karşılaştırma fişi cetvel belgedeyken ağızda durur (okunur, tıklanır), cetvel kalkınca kıskaca düşer (<c>slipDropS</c>).
        /// Kıskaçta duran fiş cetvel anında belgeye konduysa (fikstür) ağza anında geçer.
        /// </summary>
        void SyncRulerHold()
        {
            foreach (var slot in slots)
            {
                if (slot.key == null || !slot.visible || slot.queued || slot.emerging || slot.t.Moving) continue;
                bool hold = HeldByRuler(slot);
                if (hold && slot.to == SlipLoc.Clip)
                {
                    slot.from = slot.to = SlipLoc.Mouth;
                    slot.t.Snap(1f);
                }
                else if (!hold && slot.to == SlipLoc.Mouth)
                {
                    int gen = ++slot.gen;
                    float now = clock.Now;
                    slot.emerging = true;
                    slot.landed = false;
                    slot.from = SlipLoc.Mouth; slot.to = SlipLoc.Clip;
                    slot.t.Snap(0f);
                    slot.t.Go(1f, SlipDropS, CardEase.EaseIn, now);
                    clock.At(now + SlipDropS, at =>
                    {
                        if (slot.gen != gen) return;
                        slot.emerging = false;
                        slot.landed = true;
                    });
                }
            }
        }

        bool SlipsMoving
        {
            get
            {
                foreach (var s in slots) if (s.key != null && (s.t.Moving || s.queued || s.emerging)) return true;
                return false;
            }
        }

        // ------------------------------------------------------------------ tezgâh

        void SnapCounter(CounterState target)
        {
            counterGen++;
            onDown = null;
            bool down = target == CounterState.Down;
            counter = down ? CounterState.Down : CounterState.Folded;
            brackets.Snap(down ? 0f : 1f);
            topDeg.Snap(down ? 0f : FoldedDeg);
            lampLit = down;
            toolsShown = down;
            if (!down) ReturnToolsToRest(false);
        }

        void StartUnfold()
        {
            int gen = ++counterGen;
            float t0 = clock.Now;
            counter = CounterState.Lowering;
            brackets.Go(0f, BracketS, CardEase.EaseInOut, t0);
            clock.At(t0 + BracketS, at => { if (gen == counterGen) topDeg.Go(0f, LeafS, CardEase.EaseIn, at); });
            clock.At(t0 + BracketS + LeafS, at =>
            {
                if (gen != counterGen) return;
                Emit(AdmissionSound.CounterDown);
                lampLit = true;
            });
            clock.At(t0 + BracketS + LeafS + LampS, at =>
            {
                if (gen != counterGen) return;
                counter = CounterState.Down;
                toolsShown = true;
                if (docTo == DocLoc.Drawer && drawerZ.Target == 0f) OpenDrawerForDocument(at);
                var then = onDown;
                onDown = null;
                then?.Invoke();
            });
        }

        void StartFold(float t0)
        {
            int gen = ++counterGen;
            onDown = null;
            counter = CounterState.Folding;
            lampLit = false;
            toolsShown = false;
            ReturnToolsToRest(false);
            Emit(AdmissionSound.CounterFold);
            if (drawerZ.Target != 0f && !sequence)
            {
                MoveDrawer(0f, DrawerS, t0, true);
                if (docTo == DocLoc.Drawer) clock.At(t0 + DrawerS, at => { if (gen == counterGen && docTo == DocLoc.Drawer) HideDocument(); });
            }
            clock.At(t0 + LampS, at => { if (gen == counterGen) topDeg.Go(FoldedDeg, FoldS, CardEase.EaseInOut, at); });
            clock.At(t0 + LampS + FoldS, at => { if (gen == counterGen) brackets.Go(1f, BracketS, CardEase.EaseInOut, at); });
            clock.At(t0 + LampS + FoldS + BracketS, at => { if (gen == counterGen) counter = CounterState.Folded; });
        }

        void ReturnToolsToRest(bool animate)
        {
            foreach (var s in stamps) if (s != null) s.ReturnToRack(animate);
            if (magnifier != null) magnifier.ReturnToRest(animate);
        }

        void MoveDrawer(float target, float seconds, float start, bool sound)
        {
            if (drawerZ.Target == target && !drawerZ.Moving && drawerZ.Value == target) return;
            drawerZ.Go(target, seconds, CardEase.EaseInOut, start);
            if (sound) Emit(AdmissionSound.Drawer);
        }

        void OpenDrawerForDocument(float start) => MoveDrawer(DrawerOpen, DrawerS, start, true);

        // ------------------------------------------------------------------ belge

        void SnapDocument(DocLoc loc)
        {
            docFrom = docTo = loc;
            docT.Snap(1f);
        }

        void HideDocument()
        {
            docFrom = docTo = DocLoc.Hidden;
            docT.Snap(1f);
            if (documentFace != null) documentFace.ShowImpression(false, false);
        }

        void MoveDocument(DocLoc to, float seconds, float start)
        {
            docFixed = CurrentDocPose();
            docFrom = docTo == DocLoc.Hidden ? to : DocLoc.Fixed;
            docTo = to;
            docT.Snap(0f);
            docT.Go(1f, seconds, CardEase.EaseInOut, start);
        }

        /// <summary>
        /// T3: belge çekmece tablasından tezgâhtaki <c>Doc</c> çapasına (<c>docDropS</c> 0,2), sonra çekmece kapanır (<c>drawerS</c> 0,35).
        /// Belge çekmecede değilse ya da tezgâh inik değilse bir şey yapmaz. <see cref="DocumentTaken"/> yayar.
        /// </summary>
        public void TakeDocument()
        {
            EnsureInit();
            if (sequence || docTo != DocLoc.Drawer) return;
            if (counter != CounterState.Down && counter != CounterState.Lowering) return;
            if (counter == CounterState.Lowering) { onDown += TakeDocument; return; }
            float t0 = clock.Now;
            if (drawerZ.Value == 0f && !drawerZ.Moving) { SnapDocument(DocLoc.Counter); DocumentTaken?.Invoke(); return; }
            MoveDocument(DocLoc.Counter, DocDropS, t0);
            Emit(AdmissionSound.DocDrop);
            int gen = counterGen;
            clock.At(t0 + DocDropS, at => { if (gen == counterGen && !sequence) MoveDrawer(0f, DrawerS, at, true); });
            DocumentTaken?.Invoke();
        }

        /// <summary>Karar izini gösterir: belge yüzü damgalı veriyle bağlanır (sunucu kurar; basış anında). Belge tezgâhta değilse yalnız bağlar.</summary>
        public void ShowImpression(DocumentData stamped, bool animate)
        {
            EnsureInit();
            if (documentFace == null || stamped == null) return;
            documentFace.Bind(stamped);
            documentFace.ShowImpression(stamped.StampChoiceId != null, animate);
        }

        /// <summary>Başarısız basışta iz silinir.</summary>
        public void ClearImpression()
        {
            if (documentFace != null) documentFace.ShowImpression(false, false);
        }

        // ------------------------------------------------------------------ incelemeler

        /// <summary>
        /// Bir incelemenin sunumu: belge çekmecedeyse önce tezgâha alınır (T3); fiş ağızdan çıkar (anahtarı zaten gösteriliyorsa ikinci
        /// kez çıkmaz); KARŞILAŞTIR'da işaretçi son görüntünün maddesine kayar; GÖRÜŞME'de gövde 0,03 s/harf dökülür.
        /// <see cref="Bind"/>'dan önce ya da sonra çağrılabilir.
        /// </summary>
        public void PlayInspection(AdmissionTool tool, SlipData slip)
        {
            EnsureInit();
            if (docTo == DocLoc.Drawer) TakeDocument();
            SlipSlot slot = null;
            if (slip != null && !string.IsNullOrEmpty(slip.Key))
            {
                slot = SlotOf(slip.Key);
                if (slot == null)
                {
                    slot = FreeSlot();
                    if (slot != null)
                    {
                        Assign(slot, slip);
                        if (!clipOrder.Contains(slip.Key)) clipOrder.Add(slip.Key);
                        Reclip();
                        Emerge(slot);
                    }
                }
            }
            if (tool == AdmissionTool.Papers && latest != null && latest.Document != null) SetMarkerRule(latest.MarkerRule, true);
            if (tool == AdmissionTool.Interview && slot != null && slot.face != null && slot.data != null)
                slot.face.Type((slot.data.Body ?? "").Length * TranscriptPerCharS);
            ApplyAll(false);
        }

        // ------------------------------------------------------------------ talimat değişimi

        /// <summary>
        /// Talimat değişimi (R17e): eski şerit yuvadan ESKİ cebinin ara pozuna (<c>stripOutS</c> 0,25), yeni şerit yuvaya oturur
        /// (<c>stripInS</c> 0,3), işaretçi en üste; varsa İNCELEME FİŞİ ağızdan çıkar. Tezgâh katlıysa önce iner.
        /// </summary>
        public void PlayOrdersChange(OrdersData oldStrip, OrdersData newStrip, SlipData reviewSlip)
        {
            EnsureInit();
            if (counter != CounterState.Down)
            {
                if (counter == CounterState.Folded || counter == CounterState.Folding) StartUnfold();
                onDown += () => PlayOrdersChange(oldStrip, newStrip, reviewSlip);
                return;
            }
            var old = oldStrip ?? boundOrders;
            var neu = newStrip ?? latest?.Orders;
            ordersChanging = true;
            int gen = ++sequenceGen;
            if (old != null && stripFace != null) stripFace.Bind(old);
            int skip = latest != null && latest.Archive.Count > 0 && old != null && latest.Archive[0].Version == old.Version ? 1 : 0;
            BindArchive(latest, skip);
            float t0 = clock.Now;
            stripFixed = CurrentStripPose();
            stripFrom = StripLoc.Slot; stripTo = StripLoc.Transit;
            stripT.Snap(0f);
            stripT.Go(1f, StripOutS, CardEase.EaseInOut, t0);
            SetMarkerRule(-1, true);
            clock.At(t0 + StripOutS, at =>
            {
                if (gen != sequenceGen) return;
                BindArchive(latest, 0);
                if (stripFace != null) stripFace.Bind(neu);
                stripFrom = StripLoc.Above; stripTo = StripLoc.Slot;
                stripT.Snap(0f);
                stripT.Go(1f, StripInS, CardEase.EaseOut, at);
            });
            clock.At(t0 + StripOutS + StripInS, at =>
            {
                if (gen != sequenceGen) return;
                ordersChanging = false;
                boundOrders = neu;
                var review = reviewSlip;
                if (review == null && latest != null) foreach (var d in latest.Slips) if (d != null && d.Review && SlotOf(d.Key) == null) { review = d; break; }
                if (review != null && !string.IsNullOrEmpty(review.Key) && SlotOf(review.Key) == null)
                {
                    var slot = FreeSlot();
                    if (slot != null)
                    {
                        Assign(slot, review);
                        if (!clipOrder.Contains(review.Key)) clipOrder.Insert(0, review.Key);   // İNCELEME FİŞİ en altta
                        Reclip();
                        Emerge(slot);
                    }
                }
            });
            ApplyAll(false);
        }

        // ------------------------------------------------------------------ karar

        /// <summary>Kararın istasyon kısmı (PLAN §1.11; çekmece, tenekeler, katlanış). Kapı <c>SiginakAdmission</c>'da. Bitince <paramref name="done"/>.</summary>
        public void PlayDecision(StampChoice c, Action done) => PlayDecision(c, null, done);

        /// <summary>
        /// Kararın istasyon kısmı. <paramref name="stowed"/>: kâğıtlar çekmeceye girip çekmece kapanmaya başladığında (KABUL/KISITLI'da
        /// 0,35 s; kamera K8'e bu anda döner); <paramref name="done"/>: katlanış bitince.
        /// <list type="bullet">
        /// <item>KABUL / KISITLI: çekmece açılır (0,35), belge ve fişler tablaya (0,2) · 0,35 çekmece kapanır + T4 (0,58) · 0,93 bitti.</item>
        /// <item>REDDET: 0,2 çekmece açılır, belge tablaya · 0,75 çekmece kapanır ("dışarı döner") · 1,1 T4 · 1,68 bitti.</item>
        /// <item>YARDIM: çekmece açılır, iki teneke görünür, belge tablaya · 0,95 (çekmece + <c>admitHoldS</c>) çekmece kapanır
        /// (<c>aidDrawerS</c>) · 1,3 T4 · 1,88 bitti.</item>
        /// </list>
        /// Model bağlanmamışsa (kabuk/test) iki geri çağrı da hemen çalışır.
        /// </summary>
        public void PlayDecision(StampChoice c, Action stowed, Action done)
        {
            EnsureInit();
            if (!Configured) { stowed?.Invoke(); done?.Invoke(); return; }
            sequence = true;
            int gen = ++sequenceGen;
            counterGen++;   // süren iniş/katlanış adımları düşer; dizi kendi katlanışını kurar
            onDown = null;
            float t0 = clock.Now;
            float open, close, closeS;
            switch (c)
            {
                case StampChoice.Reject:
                    open = t0 + DocDropS;
                    close = open + DrawerS + DocDropS;
                    closeS = DrawerS;
                    break;
                case StampChoice.Aid:
                    open = t0;
                    close = open + DrawerS + HoldS;
                    closeS = AidDrawerS;
                    break;
                default:
                    open = t0;
                    close = open + DrawerS;
                    closeS = DrawerS;
                    break;
            }
            clock.At(open, at =>
            {
                if (gen != sequenceGen) return;
                drawerZ.Go(DrawerOpen, DrawerS, CardEase.EaseInOut, at);
                if (c == StampChoice.Aid) { tinsShown = true; Emit(AdmissionSound.TinsOut); }
                else Emit(AdmissionSound.Drawer);
                StowPapers(at);
            });
            clock.At(close, at =>
            {
                if (gen != sequenceGen) return;
                drawerZ.Go(0f, closeS, CardEase.EaseInOut, at);
                if (c == StampChoice.Aid) Emit(AdmissionSound.Drawer);
                stowed?.Invoke();
            });
            float fold = c == StampChoice.Admit || c == StampChoice.Restricted ? close : close + closeS;
            clock.At(close + closeS, at =>
            {
                if (gen != sequenceGen) return;
                HideDocument();
                foreach (var slot in slots) if (slot.key != null) FreeSlotNow(slot);
                clipOrder.Clear();
                tinsShown = false;
            });
            clock.At(fold, at => { if (gen == sequenceGen) StartFold(at); });
            float end = Mathf.Max(fold + FoldTotalS, close + closeS);
            clock.At(end, at =>
            {
                if (gen != sequenceGen) return;
                sequence = false;
                FinishSequence();
                done?.Invoke();
            });
            ApplyAll(false);
        }

        void StowPapers(float start)
        {
            if (docTo != DocLoc.Hidden) MoveDocument(DocLoc.Drawer, DocDropS, start);
            int i = 0;
            foreach (var slot in slots)
            {
                if (slot.key == null) continue;
                slot.gen++;
                slot.queued = slot.emerging = false;
                slot.landed = false;
                if (!slot.visible) continue;   // sırada bekleyen fiş hiç çıkmaz; dizinin sonunda boşalır
                slot.fixedFrom = SlipPose(slot, slot.from, slot.to, slot.t.Value);
                slot.from = SlipLoc.Fixed; slot.to = SlipLoc.Stow;
                slot.stow = i++;
                slot.t.Snap(0f);
                slot.t.Go(1f, DocDropS, CardEase.EaseInOut, start);
            }
        }

        void FinishSequence()
        {
            var s = latest;
            boundCandidate = null;
            BindDocument(s, false);
            BindSlips(s, false);
            BindOrders(s, false);
            var target = s != null && (s.Counter == CounterState.Down || s.Counter == CounterState.Lowering) ? CounterState.Down : CounterState.Folded;
            if (target == CounterState.Down && counter != CounterState.Down) StartUnfold();
        }

        /// <summary>Süren bütün geçişleri ve dizileri sesiz sona atlatır (geri çağrılar çalışır).</summary>
        void SnapAll()
        {
            muted = true;
            for (int i = 0; i < 8 && (clock.Pending || AnyTweenMoving); i++) Tick(1000f);
            muted = false;
        }

        bool AnyTweenMoving => brackets.Moving || topDeg.Moving || drawerZ.Moving || markerY.Moving || docT.Moving || stripT.Moving || SlipsMoving;

        // ------------------------------------------------------------------ damga yardımcıları

        void OnStampLifted(StampChoice c)
        {
            if (docTo == DocLoc.Drawer) TakeDocument();
        }

        /// <summary>Elde bir damga varsa rafına (0,25 s kavis) döndürür (Esc / sağ tık zincirinin ilk adımı).</summary>
        public bool ReturnHeldStamp()
        {
            bool any = false;
            foreach (var s in stamps) if (s != null && s.Held) { s.ReturnToRack(true); any = true; }
            return any;
        }

        public StampDecision HeldStamp
        {
            get
            {
                foreach (var s in stamps) if (s != null && s.Held) return s;
                return null;
            }
        }

        /// <summary>Dünya noktası belgenin damga alanında mı (belge tezgâhta ve duruyorken; 60 × 35 mm, düzleme ≤ 30 mm).</summary>
        public bool StampAreaContains(Vector3 world)
        {
            if (documentPaper == null || docTo != DocLoc.Counter || docT.Moving || sequence) return false;
            var mm = DocumentMm(world, out float height);
            if (Mathf.Abs(height) > 30f) return false;
            var d = mm - StampAreaCenterMm;
            return Mathf.Abs(d.x) <= StampAreaSizeMm.x * 0.5f && Mathf.Abs(d.y) <= StampAreaSizeMm.y * 0.5f;
        }

        /// <summary>Dünya noktası belgenin üstünde mi (çekmecede ya da tezgâhta; büyütecin bırakılışı).</summary>
        public bool DocumentContains(Vector3 world)
        {
            if (documentPaper == null || docTo == DocLoc.Hidden || docT.Moving || sequence) return false;
            var mm = DocumentMm(world, out float height);
            if (Mathf.Abs(height) > 50f) return false;
            return Mathf.Abs(mm.x) <= DocSizeMm.x * 0.5f && Mathf.Abs(mm.y) <= DocSizeMm.y * 0.5f;
        }

        /// <summary>Dünya noktasının belge yerelindeki mm'si (merkez köken, x sağa, y yukarı) ve belge düzlemine uzaklığı (mm).</summary>
        public Vector2 DocumentMm(Vector3 world, out float heightMm)
        {
            var pose = CurrentDocPose();
            var d = world - pose.position;
            var right = pose.rotation * Vector3.right;
            var up = pose.rotation * Vector3.up;
            var normal = pose.rotation * Vector3.back;
            heightMm = Vector3.Dot(d, normal) * 1000f;
            return new Vector2(Vector3.Dot(d, right), Vector3.Dot(d, up)) * 1000f;
        }

        /// <summary>Basılan damganın pozu: damga alanının merkezi belge yüzeyinde, belgeye göre −4° (damga kökü lastik yüzün ortası).</summary>
        public Pose StampPressPose()
        {
            var pose = CurrentDocPose();
            var right = pose.rotation * Vector3.right;
            var up = pose.rotation * Vector3.up;
            var normal = pose.rotation * Vector3.back;
            var p = pose.position + right * (StampAreaCenterMm.x / 1000f) + up * (StampAreaCenterMm.y / 1000f) + normal * 0.0002f;
            var rot = Quaternion.AngleAxis(ImpressionRot, normal) * Quaternion.LookRotation(up, normal);
            return new Pose(p, rot);
        }

        // ------------------------------------------------------------------ okuma pozu

        /// <summary>
        /// K8R çapasının dünya pozu: kâğıdın merkezinden <paramref name="distanceM"/> geride, kâğıda bakar. Tezgâhta yatan kâğıtlarda
        /// yön tezgâh model uzayında (0; 0,4; −0,1) (token K8R yönü, eğim 75,96°; ekranın üstü duvara), dik kâğıtlarda (şerit, GİDEN
        /// yaprağı) kâğıdın yüz normali. Kâğıt yoksa istasyonun pozu.
        /// </summary>
        public Pose ReadPose(AdmissionPaper kind, int index, float distanceM)
        {
            EnsureInit();
            if (kind == AdmissionPaper.Slip)
            {
                SyncRulerHold();
                ApplySlips();
            }
            var paper = PaperOf(kind, index);
            if (paper == null) return new Pose(transform.position, transform.rotation);
            var center = paper.position;
            var normal = paper.rotation * Vector3.back;
            var frame = counterBody != null ? counterBody : panel != null ? panel : transform;
            var counterUp = frame.up;
            if (Vector3.Dot(normal, counterUp) > 0.7f)
            {
                // Okuma başlayınca tezgâhın hover kalkışı biter; çapa hareketin sonundaki kâğıdı hedeflesin.
                if (top != null)
                {
                    var lift = Vector3.up * (hover.Value * HoverLiftMm * 0.001f);
                    center -= top.transform.parent != null ? top.transform.parent.TransformVector(lift) : lift;
                }
                var dir = frame.TransformDirection(CounterReadDirection.normalized);
                var pos = center + dir * distanceM;
                return new Pose(pos, Quaternion.LookRotation(center - pos, frame.forward));
            }
            var at = center + normal * distanceM;
            return new Pose(at, Quaternion.LookRotation(-normal, paper.up));
        }

        Transform PaperOf(AdmissionPaper kind, int index)
        {
            switch (kind)
            {
                case AdmissionPaper.Document: return docTo != DocLoc.Hidden ? documentPaper : null;
                case AdmissionPaper.Strip: return stripPaper;
                case AdmissionPaper.Slip:
                    foreach (var s in slots) if (s.key != null && s.visible && s.clip == index) return s.paper;
                    return null;
                case AdmissionPaper.Request: return pad != null ? pad.OutgoingPaper : null;
                case AdmissionPaper.Carbon: return pad != null ? pad.CarbonPaper : null;
            }
            return null;
        }

        // ------------------------------------------------------------------ etkileşim

        /// <summary>Çarpıştırıcılar yalnız etkileşimliyken (K8 ailesi; sunucu kararı) açık. Damgalara, büyütece ve deftere de geçer.</summary>
        public void SetInteractive(bool on)
        {
            EnsureInit();
            interactive = on;
            foreach (var s in stamps) if (s != null) s.SetInteractive(on);
            if (magnifier != null) magnifier.SetInteractive(on);
            if (pad != null) pad.SetInteractive(on);
            if (!on) EndHover();
            ApplyColliders();
        }

        /// <summary>Yakın okumada tezgâh yerinde kalır; kâğıt hedefleri ve üzerine gelme metinleri çalışır.</summary>
        public void SetReading(bool on)
        {
            if (reading == on) return;
            reading = on;
            if (on) EndHover();
        }

        void ApplyColliders()
        {
            bool down = counter == CounterState.Down;
            SetCollider(counterHit, interactive);
            SetCollider(deskKeyHit, interactive && down && toolsShown);
            SetCollider(documentHit, interactive && docTo != DocLoc.Hidden && !sequence && (down || counter == CounterState.Lowering));
            SetCollider(stripHit, interactive && down);
            foreach (var s in slots) SetCollider(s.hit, interactive && s.key != null && s.visible && s.landed && !sequence);
            // Elde damga varken büyüteç tık almaz: dinlenme pozundaki kutusu (300 × 15,5 × 145 mm) belgenin damga alanını örter; tık-tık'ın
            // ikinci tıkı elde damgaya ya da istasyona düşmeli (W2), büyütece değil (yoksa inspectApplicant papers yazılırdı).
            bool magnifierOn = interactive && HeldStamp == null;
            if (magnifier != null && magnifier.Interactive != magnifierOn) magnifier.SetInteractive(magnifierOn);
        }

        static void SetCollider(Collider c, bool on)
        {
            if (c != null && c.enabled != on) c.enabled = on;
        }

        Part PartOf(GameObject go, out int slip)
        {
            slip = -1;
            if (go == null) return Part.None;
            if (counterHit != null && go == counterHit.gameObject) return Part.Counter;
            if (deskKeyHit != null && go == deskKeyHit.gameObject) return Part.DeskKey;
            if (documentHit != null && go == documentHit.gameObject) return Part.Document;
            if (stripHit != null && go == stripHit.gameObject) return Part.Strip;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].hit != null && go == slots[i].hit.gameObject && slots[i].key != null) { slip = i; return Part.Slip; }
            return Part.None;
        }

        string HoverText(bool title)
        {
            var s = latest;
            var w = s?.Words;
            switch (hoverPart)
            {
                case Part.Counter: return title ? s?.CounterHoverTitle : s?.CounterHoverLine;
                case Part.DeskKey: return title ? w?.Desk : null;
                case Part.Document:
                    if (docTo == DocLoc.Drawer) return title ? w?.Drawer : s?.Document?.Name;
                    return title ? s?.Document?.Name : null;
                case Part.Strip: return title ? w?.Holder : boundOrders?.Title;
                case Part.Slip:
                    var d = hoverSlip >= 0 && hoverSlip < slots.Count ? slots[hoverSlip].data : null;
                    return title ? w?.Findings : d?.Title;
            }
            return null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactive || Blocked) return;
            UpdateHover(eventData != null ? eventData.pointerCurrentRaycast.gameObject : null);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!interactive || Blocked) { EndHover(); return; }
            UpdateHover(eventData != null ? eventData.pointerCurrentRaycast.gameObject : null);
        }

        public void OnPointerExit(PointerEventData eventData) => EndHover();

        void UpdateHover(GameObject go)
        {
            var part = PartOf(go, out int slip);
            if (part == Part.None) { EndHover(); return; }
            hoverPart = part;
            hoverSlip = slip;
            bool lift = !reading && part == Part.Counter && counter == CounterState.Down;
            hover.Go(lift ? 1f : 0f, HoverS, CardEase.Linear, clock.Now);
            if (!hovered)
            {
                hovered = true;
                if (hoverTag != null) hoverTag.Show(this);
            }
        }

        void EndHover()
        {
            if (hover.Target != 0f) hover.Go(0f, HoverS, CardEase.Linear, clock.Now);
            hoverPart = Part.None;
            hoverSlip = -1;
            if (!hovered) return;
            hovered = false;
            if (hoverTag != null) hoverTag.Hide(this);
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
            if (go == null || (pressGo != null && go != pressGo)) return;
            var held = HeldStamp;
            if (held != null) { held.ClickAtScreen(eventData.position); return; }
            Click(PartOf(go, out int slip), slip);
        }

        /// <summary>Bir parçaya tık (işaretçi olayı ve testler).</summary>
        void Click(Part part, int slip)
        {
            switch (part)
            {
                case Part.Counter:
                    if (counter == CounterState.Down && !sequence) CounterClicked?.Invoke();
                    break;
                case Part.DeskKey:
                    if (counter == CounterState.Down) DeskKeyClicked?.Invoke();
                    break;
                case Part.Document:
                    if (sequence) break;
                    if (docTo == DocLoc.Drawer && !docT.Moving) TakeDocument();
                    else if (docTo == DocLoc.Counter && !docT.Moving) PaperClicked?.Invoke(AdmissionPaper.Document, 0);
                    break;
                case Part.Strip:
                    if (counter == CounterState.Down && !ordersChanging) PaperClicked?.Invoke(AdmissionPaper.Strip, 0);
                    break;
                case Part.Slip:
                    var s = slip >= 0 && slip < slots.Count ? slots[slip] : null;
                    if (s != null && s.landed && !sequence)
                    {
                        // Kıskaçtaki öbür fişlerin alt kenarı da cetvelin altında kalmasın (A10 G06)
                        if (RulerOnDocument && s.to == SlipLoc.Clip) magnifier.ReturnToRest(true);
                        PaperClicked?.Invoke(AdmissionPaper.Slip, s.clip);
                    }
                    break;
            }
        }

        /// <summary>Çarpıştırıcısına göre tık (sentetik testler ve klavye yolu; işaretçi olayının dalıyla aynı).</summary>
        public void ClickCollider(Collider c)
        {
            EnsureInit();
            if (c == null || !interactive) return;
            Click(PartOf(c.gameObject, out int slip), slip);
        }

        // ------------------------------------------------------------------ kare

        /// <summary>Geçişleri ilerletir (Edit modda testler doğrudan çağırır).</summary>
        public void Advance(float dt)
        {
            EnsureInit();
            Tick(dt);
        }

        void Tick(float dt)
        {
            clock.Tick(dt);
            SyncRulerHold();
            ApplyAll(false);
            if (!clock.Pending && !AnyTweenMoving && !hover.Moving && !sequence && !ordersChanging)
            {
                clock.Rebase();
                emergeFreeAt = 0f;
            }
        }

        void Update() => Advance(Time.unscaledDeltaTime);

        void OnDisable() => EndHover();

        void ApplyAll(bool force)
        {
            float now = clock.Now;
            brackets.Tick(now); topDeg.Tick(now); drawerZ.Tick(now); markerY.Tick(now); docT.Tick(now); stripT.Tick(now); hover.Tick(now);
            foreach (var s in slots) s.t.Tick(now);

            if (counter == CounterState.Lowering && topDeg.Value < ToolsVisibleBelowDeg) toolsShown = true;

            // Tezgâh: konsollar, kanat (+ üzerine gelme kalkışı)
            float b = brackets.Value;
            if (force || appliedBracket != b)
            {
                appliedBracket = b;
                if (bracketL != null) bracketL.Set(bracketL.RangeTo * b);
                if (bracketR != null) bracketR.Set(bracketR.RangeTo * b);
            }
            float deg = topDeg.Value, lift = hover.Value * HoverLiftMm * 0.001f;
            if (top != null && (force || appliedTop != deg || appliedHover != lift))
            {
                appliedTop = deg;
                appliedHover = lift;
                top.Set(deg);
                if (lift != 0f) top.transform.localPosition = top.PivotLocal + Vector3.up * lift;
            }
            float z = drawerZ.Value;
            if (drawer != null && (force || appliedDrawer != z)) { appliedDrawer = z; drawer.Set(z); }

            // İşaretçi: madde hizası düzen oturduktan sonra değişirse (TMP satır bilgisi) duruyorken yeni hizaya oturur
            if (markerRule >= 0 && !markerY.Moving && !ordersChanging)
            {
                float t = MarkerTarget(markerRule);
                if (t != markerY.Value) markerY.Snap(t);
            }
            float my = markerY.Value;
            if (marker != null && (force || appliedMarker != my)) { appliedMarker = my; marker.Set(my); }
            if (jewel != null) jewel.SetSwap(jewelOn ? 1 : 0);

            ApplyLamp();
            foreach (var go in looseTools) if (go != null) SetActive(go, toolsShown);
            foreach (var go in tins) if (go != null) SetActive(go, tinsShown);
            ApplyDocument();
            ApplyStrip();
            ApplySlips();
            ApplyColliders();
        }

        void ApplyLamp()
        {
            if (counterSpot != null)
            {
                if (lampLit) AimCounterSpot();
                SetActive(counterSpot, lampLit);
            }
            if (lampBody == null) return;
            var mat = lampLit ? lampOn : lampOff;
            if (mat == null) return;
            var mats = lampBody.sharedMaterials;
            if (lampSlot < 0 || lampSlot >= mats.Length || mats[lampSlot] == mat) return;
            mats[lampSlot] = mat;
            lampBody.sharedMaterials = mats;
        }

        // ------------------------------------------------------------------ pozlar

        /// <summary>Kâğıdın dönüşü: yüz normali <paramref name="normal"/>, üst kenarı <paramref name="up"/>, normal çevresinde <paramref name="rotDeg"/>
        /// (Quad'ın görünen yüzü −z'dedir).</summary>
        public static Quaternion PaperRotation(Vector3 normal, Vector3 up, float rotDeg) =>
            Quaternion.AngleAxis(rotDeg, normal.normalized) * Quaternion.LookRotation(-normal.normalized, up.normalized);

        static Pose InFrame(Transform frame, Vector3 local, Quaternion rot)
        {
            if (frame == null) return new Pose(local, rot);
            return new Pose(frame.TransformPoint(local), frame.rotation * rot);
        }

        static Pose Blend(Pose a, Pose b, float t) =>
            new Pose(Vector3.LerpUnclamped(a.position, b.position, t), Quaternion.SlerpUnclamped(a.rotation, b.rotation, t));

        Transform DrawerFrame => drawer != null ? drawer.transform : panel;
        Transform TopFrame => top != null ? top.transform : counterBody;

        Pose DocPose(DocLoc loc)
        {
            switch (loc)
            {
                case DocLoc.Counter: return InFrame(TopFrame, DocOnCounterLocal, PaperRotation(Vector3.up, Vector3.forward, DocOnCounterRot));
                case DocLoc.Drawer: return InFrame(DrawerFrame, DocInDrawerLocal, PaperRotation(Vector3.up, Vector3.forward, DocInDrawerRot));
                case DocLoc.Fixed: return docFixed;
            }
            return documentPaper != null ? new Pose(documentPaper.position, documentPaper.rotation) : default;
        }

        Pose CurrentDocPose()
        {
            if (docTo == DocLoc.Hidden) return DocPose(DocLoc.Counter);
            if (!docT.Moving && docT.Value >= 1f) return DocPose(docTo);
            return Blend(DocPose(docFrom), DocPose(docTo), docT.Value);
        }

        void ApplyDocument()
        {
            if (documentPaper == null) return;
            bool visible = docTo != DocLoc.Hidden;
            if (visible) Place(documentPaper, CurrentDocPose());
            SetActive(documentPaper.gameObject, visible);
        }

        Pose StripPose(StripLoc loc)
        {
            switch (loc)
            {
                case StripLoc.Transit: return InFrame(panel, StripTransitLocal, PaperRotation(StripTransitNormal, StripTransitUp, StripTransitRot));
                case StripLoc.Above: return InFrame(panel, StripSlotLocal + Vector3.up * (StripSizeMm.y / 1000f), Quaternion.identity);
                case StripLoc.Fixed: return stripFixed;
            }
            return InFrame(panel, StripSlotLocal, Quaternion.identity);
        }

        Pose CurrentStripPose()
        {
            if (!stripT.Moving && stripT.Value >= 1f) return StripPose(stripTo);
            return Blend(StripPose(stripFrom), StripPose(stripTo), stripT.Value);
        }

        void ApplyStrip()
        {
            if (stripPaper == null) return;
            Place(stripPaper, CurrentStripPose());
            if (archivePapers == null) return;
            for (int i = 0; i < archivePapers.Length; i++)
                if (archivePapers[i] != null) Place(archivePapers[i], InFrame(panel, ArchiveLocal(i), PaperRotation(Vector3.back, Vector3.up, ArchiveRot(i))));
        }

        Pose SlipPose(SlipSlot slot, SlipLoc from, SlipLoc to, float t)
        {
            var a = SlipAt(slot, from);
            if (t >= 1f) return SlipAt(slot, to);
            if (t <= 0f) return a;
            return Blend(a, SlipAt(slot, to), t);
        }

        Pose SlipAt(SlipSlot slot, SlipLoc loc)
        {
            var mouthRot = PaperRotation(SlipMouthNormal, SlipMouthUp, 0f);
            switch (loc)
            {
                case SlipLoc.Inside:
                {
                    // Ağızdan çıkış doğrultusu fiş düzleminde, üst kenarın tersi (odaya ve aşağı); yol 160 mm, görünen 120
                    var dir = -SlipMouthUp.normalized;
                    return InFrame(panel, SlipMouthOut - dir * (SlipPathMm / 1000f), mouthRot);
                }
                case SlipLoc.Mouth: return InFrame(panel, SlipMouthOut, mouthRot);
                case SlipLoc.Clip:
                {
                    int n = Mathf.Max(1, clipOrder.Count);
                    int i = Mathf.Clamp(slot.clip, 0, n - 1);
                    return InFrame(counterBody != null ? counterBody : panel, ClipLocal(i, n), PaperRotation(Vector3.up, Vector3.forward, ClipRot(i)));
                }
                case SlipLoc.Stow:
                {
                    var local = DocInDrawerLocal + new Vector3(0f, 0.0004f * (slot.stow + 1), 0f);
                    return InFrame(DrawerFrame, local, PaperRotation(Vector3.up, Vector3.forward, DocInDrawerRot + slot.stow));
                }
                case SlipLoc.Fixed: return slot.fixedFrom;
            }
            return default;
        }

        void ApplySlips()
        {
            foreach (var slot in slots)
            {
                if (slot.paper == null) continue;
                bool on = slot.key != null && slot.visible;
                if (on) Place(slot.paper, SlipPose(slot, slot.from, slot.to, slot.t.Value));
                SetActive(slot.paper.gameObject, on);
            }
        }

        static void Place(Transform t, Pose p)
        {
            if (t.position != p.position || t.rotation != p.rotation) t.SetPositionAndRotation(p.position, p.rotation);
        }

        static void SetActive(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        void Emit(AdmissionSound s)
        {
            if (!muted) Sound?.Invoke(s);
        }
    }

    // ====================================================================== hareket (MonoBehaviour değil)

    /// <summary>
    /// Tek kanallı geçiş: <see cref="Go"/> o anki değerden hedefe, verilen başlangıç zamanından (saat zamanı) itibaren süre boyunca
    /// yumuşatılmış ilerler. Uçta değer hedefe tam eşitlenir (son kare atlanmaz). Başlangıç zamanı planlanmış zaman olduğundan adım
    /// boyu süreleri kaydırmaz.
    /// </summary>
    internal sealed class AdmissionTween
    {
        float from, to, start, seconds;
        CardEase ease;

        public AdmissionTween(float v) { Value = from = to = v; }

        public float Value { get; private set; }
        public float Target => to;
        public bool Moving { get; private set; }

        public void Snap(float v)
        {
            Value = from = to = v;
            Moving = false;
        }

        public void Go(float target, float duration, CardEase curve, float startTime)
        {
            from = Value; to = target; start = startTime; seconds = duration; ease = curve;
            Moving = duration > 0f && from != target;
            if (!Moving) Value = to;
        }

        public void Tick(float clock)
        {
            if (!Moving) return;
            float u = seconds > 0f ? Mathf.Clamp01((clock - start) / seconds) : 1f;
            if (u >= 1f) { Value = to; Moving = false; return; }
            Value = Mathf.LerpUnclamped(from, to, CardEasing.Evaluate(ease, u));
        }
    }

    /// <summary>
    /// Görünüm saati ve zamanlanmış adımlar: <see cref="At"/> mutlak saat zamanında çalışır, adım planlanmış zamanı alır (geçişler
    /// ondan başlar). Aynı zamanlı adımlar ekleniş sırasıyla çalışır; adım içinde eklenen ve zamanı gelmiş adımlar aynı karede çalışır.
    /// Boşta saat sıfırlanır (uzun oturumda float hassasiyeti düşmesin).
    /// </summary>
    internal sealed class AdmissionClock
    {
        struct Item
        {
            public float time;
            public int order;
            public Action<float> act;
        }

        readonly List<Item> items = new List<Item>();
        int order;

        public float Now { get; private set; }
        public bool Pending => items.Count > 0;

        public void At(float time, Action<float> act)
        {
            if (act == null) return;
            items.Add(new Item { time = time, order = order++, act = act });
        }

        public void After(float delay, Action<float> act) => At(Now + delay, act);

        public void Tick(float dt)
        {
            Now += Mathf.Max(0f, dt);
            for (int guard = 0; guard < 10000; guard++)
            {
                int best = -1;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].time > Now) continue;
                    if (best < 0 || items[i].time < items[best].time || (items[i].time == items[best].time && items[i].order < items[best].order)) best = i;
                }
                if (best < 0) return;
                var item = items[best];
                items.RemoveAt(best);
                item.act(item.time);
            }
        }

        public void Clear() => items.Clear();

        /// <summary>Bekleyen adım yokken saati sıfırlar.</summary>
        public void Rebase()
        {
            if (items.Count == 0) Now = 0f;
        }
    }
}
