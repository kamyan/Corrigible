using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kabul istasyonu (Revizyon 3 · Aşama 6, R17; KOS aşama 2 ve 7): tezgâh (Top, BracketL/R), tezgâh lambası (cam yuvası 4 +
    /// <c>CounterSpot</c> GameObject'i), evrak çekmecesi, aday grubu (<see cref="CandidateGroupView"/>: kartlar ve grup fişi), iki karar
    /// damgası (KABUL, GERİ ÇEVİR), MASA tuşu ve gevşek nesnelerin görünürlüğü. Yalnız çizer ve olay yayar: komut yazmaz, ses çalmaz
    /// (<see cref="Sound"/>), simülasyonu okumaz — durum <see cref="AdmissionSnapshot"/>'tan gelir (<see cref="Bind"/>).
    /// <list type="bullet">
    /// <item>T1 (iniş): konsollar ∓90 → 0 (<c>counterBracketS</c> 0,15) → Top 88 → 0 (<c>counterLeafS</c> 0,4; sonunda "tank") → lamba
    /// (<c>lampS</c> 0,08). T4 (katlanış): lamba söner (0,08) → Top 0 → 88 (<c>counterFoldS</c> 0,35) → konsollar (0,15). T5:
    /// <see cref="Bind"/>(…, false) animasyonsuz son duruma oturur.</item>
    /// <item>Gevşek nesneler T4 başında gizlenir, T1'de Top 30°'nin altına inince görünür (katlı kanatta panelle çakışırlar).</item>
    /// <item>Karar dizisi (<see cref="PlayDecision(StampChoice, Action, Action)"/>): çekmece açılır, aday kâğıtları çekmeceye iner,
    /// çekmece kapanır, tezgâh katlanır.</item>
    /// </list>
    /// Eski tek adaylı kabulün parçaları (belge, talimat şeridi ve ESKİ cebi, bulgu fişleri ve kıskacı, talimat işaretçisi, erzak
    /// tenekeleri, büyüteç, talep defteri) aşama 7'de söküldü.
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
        [Tooltip("AdmissionCounter model kökü (model uzayı; okuma yönü bunun çerçevesinde).")]
        [SerializeField] Transform counterBody;
        [SerializeField] PartDriver top, bracketL, bracketR;
        [Tooltip("Tezgâh kanadının çarpıştırıcısı: üst yüzü keçeyi geçmemeli (kâğıt ve damga çarpıştırıcıları üstte kalsın).")]
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
        [SerializeField] PartDriver drawer;

        [Header("Damgalar ve gevşek nesneler")]
        [Tooltip("Top'un gevşek çocukları (damga rafı ve damgalar, MASA tuşu ve plakası).")]
        [SerializeField] GameObject[] looseTools = new GameObject[0];
        [SerializeField] StampDecision[] stamps = new StampDecision[0];

        [Header("Aday grubu (KOS aşama 2)")]
        [Tooltip("Aday kartları ve grup fişi: damga hedefi, okuma, karar dizisinde çekmeceye kaldırma.")]
        [SerializeField] CandidateGroupView candidates;

        // ------------------------------------------------------------------ geometri (model/manifest, DA)

        /// <summary>Kanadın katlı açısında gevşek nesnelerin göründüğü sınır (derece; DA:111 araçları yalnız inikken kurar).</summary>
        public const float ToolsVisibleBelowDeg = 30f;
        /// <summary>Kâğıt çekmecede: Drawer yereli (model (−0,26; 0,0248; 0,08) − pivot (−0,26; 0,057; −0,034)), 3° (DA:139). Aday kâğıtları karar dizisinde buraya iner.</summary>
        public static readonly Vector3 DocInDrawerLocal = new Vector3(0f, -0.0322f, 0.114f);
        public const float DocInDrawerRot = 3f;
        /// <summary>Damga izinin kâğıda göre dönüşü (−4°).</summary>
        public const float ImpressionRot = -4f;
        /// <summary>K8R: tezgâh kâğıtları için okuma yönü, tezgâh model uzayında (token K8R counterLocalPos − counterLocalTarget).</summary>
        public static readonly Vector3 CounterReadDirection = new Vector3(0f, 0.4f, -0.1f);
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

        // ------------------------------------------------------------------ token

        public static float BracketS => BoardViewUtil.Num("admission.motion.counterBracketS", 0.15f);
        public static float LeafS => BoardViewUtil.Num("admission.motion.counterLeafS", 0.4f);
        public static float LampS => BoardViewUtil.Num("admission.motion.lampS", 0.08f);
        public static float FoldS => BoardViewUtil.Num("admission.motion.counterFoldS", 0.35f);
        public static float DrawerS => BoardViewUtil.Num("admission.motion.drawerS", 0.35f);
        public static float DocDropS => BoardViewUtil.Num("admission.motion.docDropS", 0.2f);
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
        /// <summary>Okunabilir bir kâğıda tıklandı (K8R): aday kartı (dizin kart sırası) ya da grup fişi.</summary>
        public event Action<AdmissionPaper, int> PaperClicked;
        public event Action<AdmissionSound> Sound;
        /// <summary>Grup fişindeki YER YOK listesinin bir satırına tıklandı: yollanacak kişinin kimliği (sunucu <c>AdmissionUi</c>'ye yazar).</summary>
        public event Action<string> ReplaceClicked;

        // ------------------------------------------------------------------ durum

        enum Part { None, Counter, DeskKey }

        readonly AdmissionClock clock = new AdmissionClock();
        readonly AdmissionTween brackets = new AdmissionTween(1f), topDeg = new AdmissionTween(88f), drawerZ = new AdmissionTween(0f),
            hover = new AdmissionTween(0f);

        bool initialized, bound, interactive, muted, sequence, toolsShown, lampLit, hovered, reading;
        CounterState counter = CounterState.Folded;
        int counterGen, sequenceGen;
        AdmissionSnapshot latest;
        Action onDown;
        GameObject pressGo;
        Part hoverPart;
        float appliedTop = float.NaN, appliedBracket = float.NaN, appliedDrawer = float.NaN, appliedHover = float.NaN;

        // ------------------------------------------------------------------ genel okuma

        public CounterState Counter => counter;
        /// <summary>Tezgâh, çekmece ya da aday kâğıtları hareket ediyor (ya da bir dizi sürüyor).</summary>
        public bool Moving => sequence || clock.Pending || brackets.Moving || topDeg.Moving || drawerZ.Moving || (candidates != null && candidates.Moving);
        /// <summary>Karar dizisi sürüyor (<see cref="PlayDecision(StampChoice, Action)"/>).</summary>
        public bool SequenceActive => sequence;
        public bool Interactive => interactive;
        public HoverTag Tag { get => hoverTag; set => hoverTag = value; }

        public string HoverTitle => HoverText(true);
        public string Line => HoverText(false);

        /// <summary>Konsolların katlanma oranı (0 açık, 1 duvara yatık), kanat açısı (0 inik, 88 katlı), çekmece kayması (m).</summary>
        public float BracketProgress => brackets.Value;
        public float TopAngle => topDeg.Value;
        public float DrawerOffset => drawerZ.Value;
        public bool LampOn => lampLit;
        public bool ToolsVisible => toolsShown;
        public IReadOnlyList<StampDecision> Stamps => stamps;
        /// <summary>Aday grubu görünümü (bağlı değilse null: damgaların hedefi yok).</summary>
        public CandidateGroupView Candidates => candidates;
        public PartDriver Top => top;
        public PartDriver Drawer => drawer;

        float FoldedDeg => top != null && top.RangeTo != 0 ? top.RangeTo : 88f;
        float DrawerOpen => drawer != null && drawer.RangeTo != 0 ? drawer.RangeTo : -0.22f;
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

        public void ConfigurePanel(Transform panelModel, PartDriver drawerPart)
        {
            panel = panelModel; drawer = drawerPart;
            Reinit();
        }

        public void ConfigureTools(GameObject[] loose, StampDecision[] stampList)
        {
            looseTools = loose ?? new GameObject[0]; stamps = stampList ?? new StampDecision[0];
            Reinit();
        }

        /// <summary>Aday grubu görünümü (tek seferlik sahne script'i ve testler).</summary>
        public void ConfigureCandidates(CandidateGroupView view)
        {
            candidates = view;
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
            foreach (var s in stamps)
            {
                if (s == null) continue;
                // Damganın hedefi bir aday kartı (KABUL) ya da grup fişidir (GERİ ÇEVİR)
                var stamp = s;
                s.DropAccepts = w => candidates != null && candidates.AcceptStamp(stamp.Choice, w);
                s.PressTarget = () => candidates != null ? candidates.StampPose(stamp.Choice) : new Pose(stamp.transform.position, stamp.transform.rotation);
                if (s.Tag == null) s.Tag = hoverTag;
            }
            if (candidates != null)
            {
                candidates.CardClicked -= OnCandidateCard; candidates.CardClicked += OnCandidateCard;
                candidates.SlipClicked -= OnCandidateSlip; candidates.SlipClicked += OnCandidateSlip;
                candidates.ReplaceClicked -= OnReplace; candidates.ReplaceClicked += OnReplace;
                candidates.Sound -= Emit; candidates.Sound += Emit;
                candidates.HeldStamp = () => HeldStamp;
                if (candidates.Tag == null) candidates.Tag = hoverTag;
            }

            // Başlangıç: katlı (Bind gelene dek)
            counter = CounterState.Folded;
            brackets.Snap(1f); topDeg.Snap(FoldedDeg); drawerZ.Snap(0f); hover.Snap(0f);
            toolsShown = false; lampLit = false; sequence = false;
            appliedTop = appliedBracket = appliedDrawer = appliedHover = float.NaN;
            ApplyAll(true);
        }

        void OnCandidateCard(int index)
        {
            if (counter == CounterState.Down && !sequence) PaperClicked?.Invoke(AdmissionPaper.Candidate, index);
        }

        void OnCandidateSlip()
        {
            if (counter == CounterState.Down && !sequence) PaperClicked?.Invoke(AdmissionPaper.GroupSlip, 0);
        }

        void OnReplace(string personId)
        {
            if (counter == CounterState.Down && !sequence) ReplaceClicked?.Invoke(personId);
        }

        // ------------------------------------------------------------------ bağlama

        /// <summary>
        /// Anlık görüntüyü uygular. <paramref name="animate"/> true ise durum farkından hareket kurar (tezgâh iner/katlanır, yeni grup
        /// kâğıtları gelir); false ise her şey son durumuna oturur (T5) ve süren diziler sona atlar. Karar dizisi sürerken tezgâh ve
        /// kartlar diziye aittir; son görüntü dizi bitince uygulanır. İlk bağlama hep animasyonsuzdur.
        /// </summary>
        public void Bind(AdmissionSnapshot s, bool animate)
        {
            EnsureInit();
            if (!bound) animate = false;
            bound = true;
            if (!animate && (clock.Pending || sequence)) SnapAll();
            latest = s;
            if (sequence) { ApplyAll(false); return; }

            var target = s != null && (s.Counter == CounterState.Down || s.Counter == CounterState.Lowering) ? CounterState.Down : CounterState.Folded;
            if (!animate) SnapCounter(target);
            else if (target == CounterState.Down && (counter == CounterState.Folded || counter == CounterState.Folding)) StartUnfold();
            else if (target == CounterState.Folded && (counter == CounterState.Down || counter == CounterState.Lowering)) StartFold(clock.Now);

            if (!animate) drawerZ.Snap(0f);
            else if (drawerZ.Target != 0f) MoveDrawer(0f, DrawerS, clock.Now, true);
            // Aday kartları: karar dizisi sürerken (yukarıdaki erken dönüş) bağlanmaz; kartlar o sırada diziye aittir
            if (candidates != null) candidates.Bind(s?.Group, animate);
            ApplyAll(false);
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
            if (!down) ReturnStampsToRack(false);
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
            ReturnStampsToRack(false);
            Emit(AdmissionSound.CounterFold);
            if (drawerZ.Target != 0f && !sequence) MoveDrawer(0f, DrawerS, t0, true);
            clock.At(t0 + LampS, at => { if (gen == counterGen) topDeg.Go(FoldedDeg, FoldS, CardEase.EaseInOut, at); });
            clock.At(t0 + LampS + FoldS, at => { if (gen == counterGen) brackets.Go(1f, BracketS, CardEase.EaseInOut, at); });
            clock.At(t0 + LampS + FoldS + BracketS, at => { if (gen == counterGen) counter = CounterState.Folded; });
        }

        void ReturnStampsToRack(bool animate)
        {
            foreach (var s in stamps) if (s != null) s.ReturnToRack(animate);
        }

        void MoveDrawer(float target, float seconds, float start, bool sound)
        {
            if (drawerZ.Target == target && !drawerZ.Moving && drawerZ.Value == target) return;
            drawerZ.Go(target, seconds, CardEase.EaseInOut, start);
            if (sound) Emit(AdmissionSound.Drawer);
        }

        // ------------------------------------------------------------------ karar

        /// <summary>Kararın istasyon kısmı (PLAN §1.11; çekmece, katlanış). Kapı <c>SiginakAdmission</c>'da. Bitince <paramref name="done"/>.</summary>
        public void PlayDecision(StampChoice c, Action done) => PlayDecision(c, null, done);

        /// <summary>
        /// Kararın istasyon kısmı. <paramref name="stowed"/>: kâğıtlar çekmeceye girip çekmece kapanmaya başladığında (KABUL'de 0,35 s;
        /// kamera K8'e bu anda döner); <paramref name="done"/>: katlanış bitince.
        /// <list type="bullet">
        /// <item>KABUL: çekmece açılır (0,35), kâğıtlar tablaya (0,2) · 0,35 çekmece kapanır + T4 (0,58) · 0,93 bitti.</item>
        /// <item>GERİ ÇEVİR: 0,2 çekmece açılır, kâğıtlar tablaya · 0,75 çekmece kapanır ("dışarı döner") · 1,1 T4 · 1,68 bitti.</item>
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
            bool reject = c == StampChoice.Reject;
            float open = reject ? t0 + DocDropS : t0;
            float close = reject ? open + DrawerS + DocDropS : open + DrawerS;
            clock.At(open, at =>
            {
                if (gen != sequenceGen) return;
                drawerZ.Go(DrawerOpen, DrawerS, CardEase.EaseInOut, at);
                Emit(AdmissionSound.Drawer);
                if (candidates != null) candidates.Stow(DocDropS);
            });
            clock.At(close, at =>
            {
                if (gen != sequenceGen) return;
                drawerZ.Go(0f, DrawerS, CardEase.EaseInOut, at);
                stowed?.Invoke();
            });
            float fold = reject ? close + DrawerS : close;
            clock.At(close + DrawerS, at =>
            {
                if (gen != sequenceGen) return;
                if (candidates != null) candidates.HideAll();
            });
            clock.At(fold, at => { if (gen == sequenceGen) StartFold(at); });
            float end = Mathf.Max(fold + FoldTotalS, close + DrawerS);
            clock.At(end, at =>
            {
                if (gen != sequenceGen) return;
                sequence = false;
                FinishSequence();
                done?.Invoke();
            });
            ApplyAll(false);
        }

        void FinishSequence()
        {
            var s = latest;
            if (candidates != null) candidates.Bind(s?.Group, false);
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

        bool AnyTweenMoving => brackets.Moving || topDeg.Moving || drawerZ.Moving || (candidates != null && candidates.Moving);

        // ------------------------------------------------------------------ damga yardımcıları

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

        // ------------------------------------------------------------------ okuma pozu

        /// <summary>
        /// K8R çapasının dünya pozu: kâğıdın merkezinden <paramref name="distanceM"/> geride, kâğıda bakar. Tezgâhta yatan kâğıtlarda
        /// yön tezgâh model uzayında (0; 0,4; −0,1) (token K8R yönü, eğim 75,96°; ekranın üstü duvara), dik kâğıtlarda kâğıdın yüz
        /// normali. Kâğıt yoksa istasyonun pozu.
        /// </summary>
        public Pose ReadPose(AdmissionPaper kind, int index, float distanceM)
        {
            EnsureInit();
            var paper = candidates != null && (kind == AdmissionPaper.Candidate || kind == AdmissionPaper.GroupSlip) ? candidates.PaperOf(kind, index) : null;
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

        // ------------------------------------------------------------------ etkileşim

        /// <summary>Çarpıştırıcılar yalnız etkileşimliyken (K8 ailesi; sunucu kararı) açık. Damgalara da geçer.</summary>
        public void SetInteractive(bool on)
        {
            EnsureInit();
            interactive = on;
            foreach (var s in stamps) if (s != null) s.SetInteractive(on);
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
            if (candidates != null) candidates.SetInteractive(interactive && down && !sequence);
        }

        static void SetCollider(Collider c, bool on)
        {
            if (c != null && c.enabled != on) c.enabled = on;
        }

        Part PartOf(GameObject go)
        {
            if (go == null) return Part.None;
            if (counterHit != null && go == counterHit.gameObject) return Part.Counter;
            if (deskKeyHit != null && go == deskKeyHit.gameObject) return Part.DeskKey;
            return Part.None;
        }

        string HoverText(bool title)
        {
            var s = latest;
            switch (hoverPart)
            {
                case Part.Counter: return title ? s?.CounterHoverTitle : s?.CounterHoverLine;
                case Part.DeskKey: return title ? s?.Words?.Desk : null;
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
            var part = PartOf(go);
            if (part == Part.None) { EndHover(); return; }
            hoverPart = part;
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
            Click(PartOf(go));
        }

        /// <summary>Bir parçaya tık (işaretçi olayı ve testler).</summary>
        void Click(Part part)
        {
            switch (part)
            {
                case Part.Counter:
                    if (counter == CounterState.Down && !sequence) CounterClicked?.Invoke();
                    break;
                case Part.DeskKey:
                    if (counter == CounterState.Down) DeskKeyClicked?.Invoke();
                    break;
            }
        }

        /// <summary>Çarpıştırıcısına göre tık (sentetik testler ve klavye yolu; işaretçi olayının dalıyla aynı).</summary>
        public void ClickCollider(Collider c)
        {
            EnsureInit();
            if (c == null || !interactive) return;
            Click(PartOf(c.gameObject));
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
            if (candidates != null) candidates.Advance(dt);
            ApplyAll(false);
            if (!clock.Pending && !AnyTweenMoving && !hover.Moving && !sequence) clock.Rebase();
        }

        void Update() => Advance(Time.unscaledDeltaTime);

        void OnDisable() => EndHover();

        void ApplyAll(bool force)
        {
            float now = clock.Now;
            brackets.Tick(now); topDeg.Tick(now); drawerZ.Tick(now); hover.Tick(now);

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

            ApplyLamp();
            foreach (var go in looseTools) if (go != null) SetActive(go, toolsShown);
            if (candidates != null) candidates.SetShown(toolsShown);
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
