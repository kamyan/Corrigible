using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// KARŞILAŞTIR aracı (büyüteç; PLAN §1.7, D11): tık ya da belgeye sürükle-bırak → <see cref="Used"/> (komutu sunucu yazar).
    /// <see cref="PlayInspect"/> büyüteci dinlenme pozundan inceleme pozuna (<c>magnifierMoveS</c> 0,25; DA:124 Top yereli
    /// (0,03; 0,0048; −0,027) yaw −3) götürür, sonra <c>Carriage</c>'ı (0 … 0,22 m) bulgunun alanına sürer (<c>markerS</c> 0,2).
    /// Alan Core'un kodundan: <c>date</c> → DÜZ., <c>serial</c>/<c>match</c> → SEVK No., <c>seal</c> → MÜHÜR, <c>origin</c> → GELDİĞİ YER;
    /// mercek merkezinin belge yerelindeki hedefi <see cref="FieldLensMm"/>. Kullanılmışsa inceleme pozunda kalır; tezgâh katlanırken
    /// (istasyon) dinlenme pozuna döner. Mercekte büyütme yok (tasarımda çizilmemiş). Komut yazmaz, ses çalmaz.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MagnifierTool : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("Tezgâh kanadı (Top): inceleme pozu ve sürükleme düzlemi bunun çerçevesinde.")]
        [SerializeField] Transform frame;
        [SerializeField] PartDriver carriage;
        [SerializeField] Collider hit;
        [SerializeField] Camera view;

        /// <summary>Belgenin tezgâhtaki yeri (Top yereli) ve dönüşü; <see cref="AdmissionStation.DocOnCounterLocal"/> ile aynı.</summary>
        static Vector3 DocLocal => AdmissionStation.DocOnCounterLocal;
        static float DocRot => AdmissionStation.DocOnCounterRot;
        /// <summary>DA:124 inceleme pozu (Top yereli) ve dönüşü; araba 0,158 = SEVK No.</summary>
        public static readonly Vector3 InspectLocal = new Vector3(0.03f, 0.0048f, -0.027f);
        public const float InspectYaw = -3f;
        public const float InspectCarriage = 0.158f;
        /// <summary>Mercek merkezinin araba yerelindeki yeri (<c>Lens</c> çapası) ve arabanın sıfırı (pivot −0,11).</summary>
        public static readonly Vector3 LensOnCarriage = new Vector3(0f, 0.009f, -0.085f);
        public const float CarriagePivotX = -0.11f;

        public static float MoveS => BoardViewUtil.Num("admission.motion.magnifierMoveS", 0.25f);
        public static float CarriageS => BoardViewUtil.Num("admission.motion.markerS", 0.2f);
        public static float HoverMm => BoardViewUtil.Num("motion.hoverLiftMm", 4f);
        public static float HoverS => BoardViewUtil.Num("motion.board.cardLiftS", 0.08f);
        public static float DenyMm => BoardViewUtil.Num("motion.board.denyShakeMm", 2f);
        public static float DenyS => BoardViewUtil.Num("motion.board.denyShakeS", 0.18f);

        public enum MagnifierState { Rest, Dragging, ToInspect, Inspect, Returning }

        public event Action Used;
        public event Action<AdmissionSound> Sound;

        ToolData data;
        MagnifierState state = MagnifierState.Rest;
        bool initialized, interactive, hovered, boundOnce;
        Vector3 restLocalPos;
        Quaternion restLocalRot;
        Pose from, dragPose;
        string field;
        Action pendingDone;
        readonly AdmissionClock clock = new AdmissionClock();
        readonly AdmissionTween pose = new AdmissionTween(1f), slide = new AdmissionTween(0f), hover = new AdmissionTween(0f);
        float shakeStart = -1f, appliedCarriage = float.NaN;
        Camera eventCamera;

        public MagnifierState State => state;
        public bool Moving => pose.Moving || slide.Moving || clock.Pending;
        public float CarriageValue => slide.Value;
        public string FieldCode => field;
        public bool Interactive => interactive;
        public Collider Hit => hit;
        public PartDriver Carriage => carriage;
        public Camera View { get => view; set => view = value; }
        /// <summary>Dünya noktası belgenin üstünde mi (istasyon verir).</summary>
        public Func<Vector3, bool> DropAccepts { get; set; }
        public HoverTag Tag { get; set; }
        public string HoverTitle => data?.Title;
        public string Line => data?.Reason;

        public void Configure(Transform topFrame, PartDriver carriagePart, Collider collider, Camera camera)
        {
            frame = topFrame; carriage = carriagePart; hit = collider; view = camera;
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
            state = MagnifierState.Rest;
            pose.Snap(1f);
            slide.Snap(0f);
            appliedCarriage = float.NaN;
            ApplyCollider();
        }

        // ------------------------------------------------------------------ alanlar

        /// <summary>
        /// Bulgu alanının mercek hedefi, belge yerelinde mm (merkez köken, x sağa, y yukarı). SEVK No. DA:124'ün pozundan türetilir
        /// (araba 0,158); öteki alanlar KK <c>kimlik_sevk_*</c> düzeninde SEVK No.'ya göre (UNVERIFIED: W3 yüzünün gerçek yerleşimi).
        /// </summary>
        public static Vector2 FieldLensMm(string code)
        {
            var serial = SerialLensMm;
            switch (code)
            {
                case "date": return serial + new Vector2(-23f, 21f);
                case "seal": return serial + new Vector2(-41f, -19f);
                case "origin": return serial + new Vector2(-66f, -7.5f);
                default: return serial;   // serial, match ve bilinmeyen
            }
        }

        static Vector2 SerialLensMm
        {
            get
            {
                // DA pozunda merceğin Top yerelindeki yeri → belge yereli
                var lensTop = InspectLocal + Quaternion.Euler(0f, InspectYaw, 0f) * (new Vector3(CarriagePivotX + InspectCarriage, 0f, 0f) + LensOnCarriage);
                var d = Quaternion.Inverse(Quaternion.Euler(0f, DocRot, 0f)) * (lensTop - DocLocal);
                return new Vector2(d.x, d.z) * 1000f;
            }
        }

        /// <summary>Alanın inceleme pozu (Top yereli) ve araba değeri: kök belgeye hizalı, mercek alanın üstünde.</summary>
        public static void InspectFor(string code, out Vector3 localPos, out Quaternion localRot, out float carriageValue)
        {
            var target = FieldLensMm(code) / 1000f;
            // DA pozunda kökün belge yerelindeki yeri
            var docRot = Quaternion.Euler(0f, DocRot, 0f);
            var root0 = Quaternion.Inverse(docRot) * (InspectLocal - DocLocal);
            float rx = root0.x, ry = target.y - LensOnCarriage.z;
            carriageValue = Mathf.Clamp(target.x - rx - CarriagePivotX, 0f, 0.22f);
            var rootDoc = new Vector3(rx, root0.y, ry);
            localPos = DocLocal + docRot * rootDoc;
            localRot = Quaternion.Euler(0f, InspectYaw, 0f);
        }

        // ------------------------------------------------------------------ veri

        /// <summary>
        /// Aracın durumu (başlık, Core iletisi, kullanıldı mı). İnceleme pozuna yalnız akış götürür (<see cref="PlayInspect"/>,
        /// karşılaştırma anı); bağlama götürmez: tasarımda büyüteç yalnız karşılaştırma anında belgededir (R17_inspect, R17_read),
        /// sonraki durumlarda dinlenme yerindedir (R17_pending, R17_stamped). Kullanılmadıysa (yeni aday) dinlenme pozuna döner.
        /// </summary>
        public void Bind(ToolData d)
        {
            EnsureInit();
            bool first = !boundOnce;
            boundOnce = true;
            data = d;
            bool used = d != null && d.Used;
            if (!used && (state == MagnifierState.Inspect || state == MagnifierState.ToInspect)) ReturnToRest(!first);
            ApplyCollider();
        }

        /// <summary>İnceleme pozuna ve alanın araba değerine anında oturur (kayıttan devam).</summary>
        public void SnapInspect(string fieldCode)
        {
            EnsureInit();
            field = fieldCode;
            clock.Clear();
            state = MagnifierState.Inspect;
            pose.Snap(1f);
            InspectFor(field, out _, out _, out float c);
            slide.Snap(c);
            ApplyPose();
            ApplyCollider();
        }

        /// <summary>İnceleme pozuna gider ve arabayı Core kodunun alanına (<c>date/serial/seal/origin/match</c>) sürer; bitince <paramref name="done"/>.</summary>
        public void PlayInspect(string fieldCode, Action done)
        {
            EnsureInit();
            field = fieldCode;
            if (done != null) pendingDone += done;
            if (state == MagnifierState.Inspect)
            {
                InspectFor(field, out _, out _, out float c);
                float t0 = clock.Now;
                slide.Go(c, CarriageS, CardEase.EaseInOut, t0);
                clock.At(t0 + CarriageS, at => Finish());
                return;
            }
            if (state == MagnifierState.ToInspect) return;   // giderken alan güncellendi; araba adımı yeni alanı okur
            StartInspect(field);
        }

        void StartInspect(string fieldCode)
        {
            field = fieldCode;
            from = new Pose(transform.position, transform.rotation);
            state = MagnifierState.ToInspect;
            clock.Clear();
            float t0 = clock.Now;
            pose.Snap(0f);
            pose.Go(1f, MoveS, CardEase.EaseInOut, t0);
            clock.At(t0 + MoveS, at =>
            {
                if (state != MagnifierState.ToInspect) return;
                state = MagnifierState.Inspect;
                InspectFor(field, out _, out _, out float c);
                slide.Go(c, CarriageS, CardEase.EaseInOut, at);
                clock.At(at + CarriageS, _ => Finish());
            });
            EndHover();
            ApplyCollider();
        }

        void Finish()
        {
            var d = pendingDone;
            pendingDone = null;
            d?.Invoke();
        }

        /// <summary>Dinlenme pozuna döner (animasyonla 0,25 s; araba 0'a).</summary>
        public void ReturnToRest(bool animate)
        {
            EnsureInit();
            clock.Clear();
            Finish();
            if (!animate || state == MagnifierState.Rest)
            {
                state = MagnifierState.Rest;
                pose.Snap(1f);
                slide.Snap(0f);
                ApplyPose();
                ApplyCollider();
                return;
            }
            from = new Pose(transform.position, transform.rotation);
            state = MagnifierState.Returning;
            pose.Snap(0f);
            pose.Go(1f, MoveS, CardEase.EaseInOut, clock.Now);
            slide.Go(0f, MoveS, CardEase.EaseInOut, clock.Now);
            ApplyCollider();
        }

        public void SetInteractive(bool on)
        {
            EnsureInit();
            interactive = on;
            if (!on)
            {
                EndHover();
                if (state == MagnifierState.Dragging) ReturnToRest(false);
            }
            ApplyCollider();
        }

        void ApplyCollider()
        {
            if (hit != null && hit.enabled != interactive) hit.enabled = interactive;
        }

        // ------------------------------------------------------------------ kare

        public void Advance(float dt)
        {
            EnsureInit();
            clock.Tick(dt);
            float now = clock.Now;
            pose.Tick(now); slide.Tick(now); hover.Tick(now);
            if (state == MagnifierState.Returning && !pose.Moving) state = MagnifierState.Rest;
            ApplyPose();
            if (!clock.Pending && !pose.Moving && !slide.Moving && !hover.Moving && shakeStart < 0f) clock.Rebase();
        }

        void Update() => Advance(Time.unscaledDeltaTime);

        Pose RestPose => transform.parent != null
            ? new Pose(transform.parent.TransformPoint(restLocalPos), transform.parent.rotation * restLocalRot)
            : new Pose(restLocalPos, restLocalRot);

        Pose InspectPose
        {
            get
            {
                InspectFor(field, out var lp, out var lr, out _);
                return frame != null ? new Pose(frame.TransformPoint(lp), frame.rotation * lr) : new Pose(lp, lr);
            }
        }

        Vector3 Up => frame != null ? frame.up : Vector3.up;

        void ApplyPose()
        {
            Pose p;
            switch (state)
            {
                case MagnifierState.ToInspect: p = Blend(from, InspectPose, pose.Value); break;
                case MagnifierState.Inspect: p = InspectPose; break;
                case MagnifierState.Returning: p = Blend(from, RestPose, pose.Value); break;
                case MagnifierState.Dragging: p = dragPose; break;
                default:
                    p = RestPose;
                    if (hover.Value > 0f) p.position += Up * (HoverMm / 1000f) * hover.Value;
                    break;
            }
            if (shakeStart >= 0f)
            {
                float u = DenyS > 0f ? (clock.Now - shakeStart) / DenyS : 1f;
                if (u >= 1f) shakeStart = -1f;
                else p.position += p.rotation * Vector3.right * (DenyMm / 1000f * Mathf.Sin(3f * Mathf.PI * u));
            }
            if (transform.position != p.position || transform.rotation != p.rotation) transform.SetPositionAndRotation(p.position, p.rotation);
            float c = slide.Value;
            if (carriage != null && appliedCarriage != c) { appliedCarriage = c; carriage.Set(c); }
        }

        static Pose Blend(Pose a, Pose b, float t) =>
            new Pose(Vector3.LerpUnclamped(a.position, b.position, t), Quaternion.SlerpUnclamped(a.rotation, b.rotation, t));

        // ------------------------------------------------------------------ işaretçi

        static bool Blocked => DeskTarget.Blocked != null && DeskTarget.Blocked();
        bool Enabled => data != null && data.Enabled;
        bool Usable(PointerEventData e) => e != null && e.button == PointerEventData.InputButton.Left && interactive && !Blocked;

        Camera RayCamera => view != null ? view : eventCamera != null ? eventCamera : Camera.main;

        bool PlanePoint(Vector2 screen, out Vector3 world)
        {
            world = default;
            var cam = RayCamera;
            if (cam == null) return false;
            var plane = frame != null ? new Plane(frame.up, frame.TransformPoint(0f, AdmissionStation.DocOnCounterLocal.y, 0f)) : new Plane(Vector3.up, transform.position);
            var ray = cam.ScreenPointToRay(screen);
            if (!plane.Raycast(ray, out float d)) return false;
            world = ray.GetPoint(d);
            return true;
        }

        void Deny()
        {
            shakeStart = clock.Now;
            Sound?.Invoke(AdmissionSound.Deny);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactive || Blocked || hovered) return;
            hovered = true;
            if (state == MagnifierState.Rest && Enabled) hover.Go(1f, HoverS, CardEase.Linear, clock.Now);
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

        void OnDisable() => EndHover();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Usable(eventData) || eventData.dragging) return;
            if (state != MagnifierState.Rest && state != MagnifierState.Inspect) return;
            if (!Enabled) { Deny(); return; }
            Used?.Invoke();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureInit();
            if (!Usable(eventData) || state != MagnifierState.Rest) return;
            if (eventData.pressEventCamera != null) eventCamera = eventData.pressEventCamera;
            if (!Enabled) { Deny(); return; }
            state = MagnifierState.Dragging;
            dragPose = new Pose(transform.position, transform.rotation);
            hover.Snap(0f);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (state != MagnifierState.Dragging || eventData == null) return;
            if (PlanePoint(eventData.position, out var p)) dragPose = new Pose(p + Up * 0.0005f, dragPose.rotation);
            ApplyPose();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (state != MagnifierState.Dragging) return;
            bool accepted = eventData != null && PlanePoint(eventData.position, out var p) && DropAccepts != null && DropAccepts(p);
            // Bırakılan yerden dinlenmeye dönmeye başlar; kabul edildiyse sunucu komutu yazar ve başarıda Bind/PlayInspect bu yerden
            // inceleme pozuna götürür (retde dönüş sürer)
            ReturnToRest(true);
            if (accepted) Used?.Invoke();
        }
    }
}
