using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bir karar damgası (raf yuvasında; PLAN §1.10, D11). İki yol, ikisi de iki adım ("geri dönülmez iş iki adımlıdır"):
    /// <list type="bullet">
    /// <item>Sürükle-bırak: raftan sürükleyince damga raftaki yüksekliğinin 25 mm üstüne kalkar (<c>stampLiftS</c> 0,1;
    /// <see cref="AdmissionSound.StampLift"/>) ve o yükseklikte imlecin altında kalarak onu izler; bırakış noktası damganın tezgâhtaki
    /// izdüşümüdür (<see cref="DropPoint"/>). Belgenin damga alanında bırakılınca 0,12 s'de basar (<c>stampPressS</c>; "tak",
    /// <see cref="AdmissionSound.StampPress"/>),
    /// başka yere bırakılınca rafına 0,25 s kavisle döner (<c>motion.board.returnArcS</c>; <see cref="AdmissionSound.StampReturn"/>).</item>
    /// <item>Tık-tık: damgaya tık = elde (imleci 25 mm yukarıda izler), belgeye tık = bas, başka yere tık ya da Esc/sağ tık
    /// (<see cref="ReturnToRack"/>) = rafına.</item>
    /// </list>
    /// Basış bitince <see cref="Pressed"/> yayılır (komutu sunucu yazar); işleyici damgayı rafına döndürmediyse damga "kullanılmış"
    /// pozuna gider (Top yereli (0,072; 0,038; −0,087) yaw −14; YARDIM (−0,10; 0,03; −0,075)) ve tezgâh katlanırken rafına döner.
    /// Kilitliyken (<see cref="StampData.Enabled"/> false) damga kalkmaz, yuvanın önünde pirinç dil görünür, tıkta 2 mm titrer
    /// (<c>motion.board.denyShakeMm</c>) ve <see cref="AdmissionSound.Deny"/>. Komut yazmaz, ses çalmaz. Edit modda <see cref="Advance"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StampDecision : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] StampChoice choice;
        [Tooltip("Damganın çarpıştırıcısı (kök).")]
        [SerializeField] Collider hit;
        [Tooltip("Band parçasının renderer'ı: kararın mürekkep rengi MaterialPropertyBlock ile (prefab'ın swap dizisi boş).")]
        [SerializeField] Renderer band;
        [Tooltip("Kilit dili (pirinç küp 40 × 6 × 3 mm; D21): yalnız kilitliyken görünür.")]
        [SerializeField] GameObject lockTongue;
        [Tooltip("Tezgâh kanadı (Top): sürükleme düzlemi ve kullanılmış poz bunun çerçevesinde.")]
        [SerializeField] Transform frame;
        [Tooltip("Sürükleme ışınının kamerası (boşsa olayın kamerası, o da yoksa Camera.main).")]
        [SerializeField] Camera view;
        [Tooltip("Sapın yazısı (DecisionStamp/TXT_Cap): anlık görüntünün çevrilmiş kısa adı (StampData.Cap) yazılır; dört örnek aynı " +
            "prefab'dan geldiği için DeskLocText yok (bütünleyici C).")]
        [SerializeField] TMP_Text capLabel;

        /// <summary>Kullanılmış poz (Top yereli; DA:153–154).</summary>
        public static readonly Vector3 UsedLocal = new Vector3(0.072f, 0.038f, -0.087f);
        public static readonly Vector3 UsedAidLocal = new Vector3(-0.10f, 0.03f, -0.075f);
        public const float UsedYaw = -14f;
        /// <summary>Sürükleme düzlemi Top yerelinde belge yüzeyi yüksekliğinde (0,0043).</summary>
        public const float DragPlaneY = 0.0043f;

        public static float LiftS => BoardViewUtil.Num("admission.motion.stampLiftS", 0.1f);
        public static float PressS => BoardViewUtil.Num("admission.motion.stampPressS", 0.12f);
        public static float ReturnS => BoardViewUtil.Num("motion.board.returnArcS", 0.25f);
        public static float LiftMm => BoardViewUtil.Num("motion.team.liftMm", 25f);
        public static float HoverMm => BoardViewUtil.Num("motion.hoverLiftMm", 4f);
        public static float HoverS => BoardViewUtil.Num("motion.board.cardLiftS", 0.08f);
        public static float DenyMm => BoardViewUtil.Num("motion.board.denyShakeMm", 2f);
        public static float DenyS => BoardViewUtil.Num("motion.board.denyShakeS", 0.18f);

        public enum StampState { Rack, Lifting, Held, Pressing, Used, Returning }

        public event Action<StampChoice> Pressed;
        public event Action<AdmissionSound> Sound;
        /// <summary>Damga raftan kalktı (sürükleme ya da tık-tık'ın ilk tıkı). İstasyon belgeyi çekmeceden alır.</summary>
        public event Action<StampChoice> Lifted;

        StampData data;
        StampState state = StampState.Rack;
        bool initialized, enabledByData, interactive, dragging, clickHeld, hovered;
        Vector3 restLocalPos;
        Quaternion restLocalRot;
        Pose from, heldPose;
        float heldHeight;
        Vector2 screen;
        Camera eventCamera;
        readonly AdmissionClock clock = new AdmissionClock();
        readonly AdmissionTween t = new AdmissionTween(1f), hover = new AdmissionTween(0f);
        float shakeStart = -1f, shakeOffsetMm;
        MaterialPropertyBlock block;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public StampChoice Choice => choice;
        public StampState State => state;
        /// <summary>Elde (kalkıyor ya da tutuluyor: sürükleme ya da tık-tık'ın ilk tıkından sonra).</summary>
        public bool Held => state == StampState.Lifting || state == StampState.Held;
        public bool Moving => t.Moving || clock.Pending || shakeStart >= 0f;
        public bool Enabled => enabledByData;
        public bool Interactive => interactive;
        /// <summary>Dünya noktası belgenin damga alanında mı (istasyon verir).</summary>
        public Func<Vector3, bool> DropAccepts { get; set; }
        /// <summary>Basışın hedef pozu (istasyon verir: belgenin damga alanı, −4°). Boşsa bırakılan nokta, o anki dönüş.</summary>
        public Func<Pose> PressTarget { get; set; }
        public HoverTag Tag { get; set; }
        public Collider Hit => hit;
        public GameObject LockTongue => lockTongue;
        public Camera View { get => view; set => view = value; }
        /// <summary>Sapın yazısı (anlık görüntüden; boşsa yazılmaz).</summary>
        public TMP_Text CapLabel { get => capLabel; set => capLabel = value; }
        /// <summary>Elde damganın sürükleme düzleminden yüksekliği (raftaki yükseklik + 25 mm; kalkışta ölçülür).</summary>
        public float HeldHeight => heldHeight;
        /// <summary>Ret titremesinin o anki ötelemesi (mm, damganın x ekseninde).</summary>
        public float ShakeOffsetMm => shakeOffsetMm;

        public string HoverTitle => data?.Plate?.Replace('\n', ' ');
        public string Line => enabledByData ? null : (data?.Reason ?? data?.ShortReason);

        /// <summary>Kurucu (tek seferlik sahne script'i) ve testler. Kökün o anki yerel pozu raf pozu olur.</summary>
        public void Configure(StampChoice stampChoice, Collider collider, Renderer bandRenderer, GameObject tongue, Transform topFrame, Camera camera)
        {
            choice = stampChoice; hit = collider; band = bandRenderer; lockTongue = tongue; frame = topFrame; view = camera;
            initialized = false;
            EnsureInit();
        }

        void Awake() => EnsureInit();

        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            restLocalPos = transform.localPosition;
            restLocalRot = transform.localRotation;
            state = StampState.Rack;
            t.Snap(1f);
            ApplyLock();
            ApplyCollider();
        }

        // ------------------------------------------------------------------ veri

        public void Bind(StampData d)
        {
            EnsureInit();
            data = d;
            enabledByData = d != null && d.Enabled;
            ApplyBand(d?.BandColor);
            if (capLabel != null && !string.IsNullOrEmpty(d?.Cap) && capLabel.text != d.Cap) capLabel.text = d.Cap;
            ApplyLock();
            if (!enabledByData && Held) ReturnToRack(true);
        }

        void ApplyBand(string hex)
        {
            if (band == null || string.IsNullOrEmpty(hex) || !ColorUtility.TryParseHtmlString(hex, out var c)) return;
            if (block == null) block = new MaterialPropertyBlock();
            band.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            band.SetPropertyBlock(block);
        }

        void ApplyLock()
        {
            if (lockTongue != null && lockTongue.activeSelf == enabledByData) lockTongue.SetActive(!enabledByData);
        }

        public void SetInteractive(bool on)
        {
            EnsureInit();
            interactive = on;
            if (!on)
            {
                EndHover();
                if (Held) ReturnToRack(false);
            }
            ApplyCollider();
        }

        void ApplyCollider()
        {
            bool on = interactive && state != StampState.Pressing && state != StampState.Used;
            if (hit != null && hit.enabled != on) hit.enabled = on;
        }

        // ------------------------------------------------------------------ pozlar

        Transform Parent => transform.parent;
        Pose RestPose => Parent != null ? new Pose(Parent.TransformPoint(restLocalPos), Parent.rotation * restLocalRot) : new Pose(restLocalPos, restLocalRot);
        Pose Current => new Pose(transform.position, transform.rotation);
        Vector3 Up => frame != null ? frame.up : Vector3.up;

        Pose UsedPose
        {
            get
            {
                var local = choice == StampChoice.Aid ? UsedAidLocal : UsedLocal;
                var rot = Quaternion.Euler(0f, UsedYaw, 0f);
                return frame != null ? new Pose(frame.TransformPoint(local), frame.rotation * rot) : new Pose(local, rot);
            }
        }

        Vector3 DragOrigin => frame != null ? frame.TransformPoint(0f, DragPlaneY, 0f) : RestPose.position;
        Plane DragPlane => new Plane(Up, DragOrigin);

        Camera RayCamera => view != null ? view : eventCamera != null ? eventCamera : Camera.main;

        /// <summary>Ekran noktasının sürükleme düzlemindeki dünya noktası (ışın düzleme değmezse false).</summary>
        public bool PlanePoint(Vector2 screenPoint, out Vector3 world)
        {
            world = default;
            var cam = RayCamera;
            if (cam == null) return false;
            var ray = cam.ScreenPointToRay(screenPoint);
            if (!DragPlane.Raycast(ray, out float d)) return false;
            world = ray.GetPoint(d);
            return true;
        }

        /// <summary>Ekran noktasının ışını ile damganın tutulduğu yükseklikteki düzlemin kesişimi (damga imlecin altında).</summary>
        bool HeldPoint(Vector2 screenPoint, out Vector3 world)
        {
            world = default;
            var cam = RayCamera;
            if (cam == null) return false;
            var plane = new Plane(Up, DragOrigin + Up * heldHeight);
            var ray = cam.ScreenPointToRay(screenPoint);
            if (!plane.Raycast(ray, out float d)) return false;
            world = ray.GetPoint(d);
            return true;
        }

        /// <summary>Elde damga bu ekran noktasındayken bırakılırsa tezgâhtaki izdüşümü (damga alanı denetimi ve basış bunu kullanır).</summary>
        public bool DropPoint(Vector2 screenPoint, out Vector3 world)
        {
            if (!HeldPoint(screenPoint, out world)) return false;
            world -= Up * heldHeight;
            return true;
        }

        Pose HeldTarget()
        {
            if (HeldPoint(screen, out var p)) heldPose = new Pose(p, heldPose.rotation);
            return heldPose;
        }

        // ------------------------------------------------------------------ hareketler

        void Lift(bool byDrag, Vector2 at)
        {
            dragging = byDrag;
            clickHeld = !byDrag;
            screen = at;
            from = Current;
            heldHeight = Mathf.Max(0f, DragPlane.GetDistanceToPoint(from.position)) + LiftMm / 1000f;
            heldPose = new Pose(from.position + Up * (LiftMm / 1000f), from.rotation);
            state = StampState.Lifting;
            EndHover();
            clock.Clear();
            t.Snap(0f);
            t.Go(1f, LiftS, CardEase.EaseOut, clock.Now);
            Emit(AdmissionSound.StampLift);
            ApplyCollider();
            Lifted?.Invoke(choice);
        }

        /// <summary>Elde damgayı ekran noktasına götürür (tık-tık'ta her kare; sürüklemede işaretçi olayı).</summary>
        public void Follow(Vector2 screenPoint)
        {
            if (Held) screen = screenPoint;
        }

        /// <summary>Tık-tık'ın ikinci tıkı ekran noktasında (istasyon belgeye düşen tıkı buraya verir).</summary>
        public void ClickAtScreen(Vector2 screenPoint)
        {
            if (!Held) return;
            if (DropPoint(screenPoint, out var p)) ClickAt(p);
            else ReturnToRack(true);
        }

        /// <summary>Tık-tık'ın ikinci tıkı (tezgâhtaki nokta): damga alanındaysa basar, değilse rafına döner.</summary>
        public void ClickAt(Vector3 world)
        {
            if (!Held) return;
            if (DropAccepts != null && DropAccepts(world)) Press(world);
            else ReturnToRack(true);
        }

        /// <summary>Damgayı <paramref name="world"/> noktasına (ya da istasyonun hedef pozuna) basar; basış bitince <see cref="Pressed"/>.</summary>
        public void Press(Vector3 world)
        {
            EnsureInit();
            if (state == StampState.Pressing || state == StampState.Used) return;
            dragging = clickHeld = false;
            from = Current;
            var target = PressTarget != null ? PressTarget() : new Pose(world, from.rotation);
            state = StampState.Pressing;
            clock.Clear();
            t.Snap(0f);
            float t0 = clock.Now;
            t.Go(1f, PressS, CardEase.EaseIn, t0);
            heldPose = target;
            ApplyCollider();
            clock.At(t0 + PressS, at =>
            {
                if (state != StampState.Pressing) return;
                Place(heldPose);   // basış belgenin üstünde biter ("tak" anında damga yüzeyde; son kare atlanmaz)
                Emit(AdmissionSound.StampPress);
                Pressed?.Invoke(choice);
                if (state != StampState.Pressing) return;   // işleyici rafına döndürdü (komut reddi)
                from = heldPose;
                state = StampState.Used;
                t.Snap(0f);
                t.Go(1f, ReturnS, CardEase.EaseInOut, at);
                ApplyCollider();
            });
        }

        /// <summary>Rafına döner: <paramref name="animate"/> ise 0,25 s kavis (25 mm tepe) + dönüş sesi; değilse anında.</summary>
        public void ReturnToRack(bool animate)
        {
            EnsureInit();
            dragging = clickHeld = false;
            clock.Clear();
            if (!animate)
            {
                state = StampState.Rack;
                t.Snap(1f);
                Place(RestPose);
                ApplyCollider();
                return;
            }
            if (state == StampState.Rack || state == StampState.Returning) return;
            from = Current;
            state = StampState.Returning;
            t.Snap(0f);
            t.Go(1f, ReturnS, CardEase.EaseInOut, clock.Now);
            Emit(AdmissionSound.StampReturn);
            ApplyCollider();
        }

        void Deny()
        {
            shakeStart = clock.Now;
            Emit(AdmissionSound.Deny);
        }

        // ------------------------------------------------------------------ kare

        public void Advance(float dt)
        {
            EnsureInit();
            clock.Tick(dt);
            float now = clock.Now;
            t.Tick(now);
            hover.Tick(now);
            if (state == StampState.Lifting && !t.Moving) state = StampState.Held;
            if (state == StampState.Returning && !t.Moving) { state = StampState.Rack; ApplyCollider(); }

            shakeOffsetMm = 0f;
            if (shakeStart >= 0f)
            {
                float u = DenyS > 0f ? (now - shakeStart) / DenyS : 1f;
                if (u >= 1f) shakeStart = -1f;
                else shakeOffsetMm = DenyMm * Mathf.Sin(3f * Mathf.PI * u);
            }
            ApplyPose();
            if (!t.Moving && !hover.Moving && !clock.Pending && shakeStart < 0f) clock.Rebase();
        }

        void Update()
        {
            if (clickHeld && Held)
            {
                var mouse = Mouse.current;
                if (mouse != null) Follow(mouse.position.ReadValue());
            }
            Advance(Time.unscaledDeltaTime);
        }

        void ApplyPose()
        {
            Pose p;
            float e = t.Value;
            switch (state)
            {
                case StampState.Lifting: p = Blend(from, HeldTarget(), e); break;
                case StampState.Held: p = HeldTarget(); break;
                case StampState.Pressing: p = Blend(from, heldPose, e); break;
                case StampState.Used: p = Blend(from, UsedPose, e); break;
                case StampState.Returning:
                {
                    var rest = RestPose;
                    p = Blend(from, rest, e);
                    p.position += Up * (LiftMm / 1000f) * Mathf.Sin(Mathf.PI * e);
                    break;
                }
                default:
                    p = RestPose;
                    if (hover.Value > 0f) p.position += Up * (HoverMm / 1000f) * hover.Value;
                    break;
            }
            if (shakeOffsetMm != 0f) p.position += p.rotation * Vector3.right * (shakeOffsetMm / 1000f);
            Place(p);
        }

        static Pose Blend(Pose a, Pose b, float t) =>
            new Pose(Vector3.LerpUnclamped(a.position, b.position, t), Quaternion.SlerpUnclamped(a.rotation, b.rotation, t));

        void Place(Pose p)
        {
            if (transform.position != p.position || transform.rotation != p.rotation) transform.SetPositionAndRotation(p.position, p.rotation);
        }

        void Emit(AdmissionSound s) => Sound?.Invoke(s);

        // ------------------------------------------------------------------ işaretçi

        static bool Blocked => DeskTarget.Blocked != null && DeskTarget.Blocked();

        bool Usable(PointerEventData e) => e != null && e.button == PointerEventData.InputButton.Left && interactive && !Blocked;

        void Remember(PointerEventData e)
        {
            if (e == null) return;
            var cam = e.pressEventCamera != null ? e.pressEventCamera : e.enterEventCamera;
            if (cam != null) eventCamera = cam;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactive || Blocked || hovered) return;
            hovered = true;
            if (state == StampState.Rack && enabledByData) hover.Go(1f, HoverS, CardEase.Linear, clock.Now);
            if (Tag != null) Tag.Show(this);
        }

        public void OnPointerExit(PointerEventData eventData) => EndHover();

        void EndHover()
        {
            if (hover.Target != 0f) hover.Go(0f, HoverS, CardEase.Linear, clock.Now);
            if (!hovered) return;
            hovered = false;
            if (Tag != null) Tag.Hide(this);
        }

        void OnDisable()
        {
            EndHover();
            if (Held) ReturnToRack(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            EnsureInit();
            if (!Usable(eventData) || eventData.dragging) return;
            Remember(eventData);
            if (Held)
            {
                if (clickHeld) ClickAtScreen(eventData.position);
                return;
            }
            if (state != StampState.Rack) return;
            if (!enabledByData) { Deny(); return; }
            Lift(false, eventData.position);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureInit();
            if (!Usable(eventData)) return;
            Remember(eventData);
            if (clickHeld && Held) { clickHeld = false; dragging = true; screen = eventData.position; return; }
            if (state != StampState.Rack) return;
            if (!enabledByData) { Deny(); return; }
            Lift(true, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || eventData == null) return;
            screen = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            dragging = false;
            if (eventData != null) screen = eventData.position;
            if (DropPoint(screen, out var p) && DropAccepts != null && DropAccepts(p)) Press(p);
            else ReturnToRack(true);
        }
    }
}
