using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Sığınak odasının kamera pozları (Docs/UI_Siginak PROMPT §5 Aşama 3, tokens.cameras).</summary>
    public enum SiginakPose
    {
        /// <summary>K1 başlangıç: fiziksel kamera, lens kaydırmalı.</summary>
        Desk,
        /// <summary>Üst harita pozu: K2, K3, K6b, K7.</summary>
        Map,
        /// <summary>K4 raf / kesit maketi.</summary>
        Shelf,
        /// <summary>K5a CRT bakışı (istasyon değil).</summary>
        Crt,
        /// <summary>K6a vardiya anı (yalnız dizide).</summary>
        Shift,
        /// <summary>Sığınağın kapısı (sağ duvar; yalnız biri kapıyı çalınca).</summary>
        Door,
    }

    /// <summary>
    /// Sığınak kamerası: pozlar arasında 0,45 s ease-in-out geçiş (K1 → harita önce 0,15 s yükselir, sonra eğilir),
    /// sağ tuşla sınırlı bakış, tekerlekle yaklaşma, 2/3/F/Home kısayolları. Kamera her pozda fiziksel kalır: dikey FOV
    /// odak uzaklığıyla, K1'in ufuk kaydırması lens kaydırmasıyla verilir; geçişte ikisi birlikte değişir (sıçrama olmaz).
    /// Baskına geçişte kamerayı üst üste binen üç vuruşla (geri çekil, dön, yaklaş) kesintisiz CRT'ye götürür, dönüşte aynı
    /// yoldan geri getirir (<see cref="BeginDive"/>, <see cref="BeginReturn"/>); o sırada rig başlatıcıda kapalıdır, yolu
    /// <see cref="RunDive"/> sürer.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class SiginakCameraRig : MonoBehaviour
    {
        [Serializable] public struct PoseSlot
        {
            public SiginakPose pose;
            public Transform anchor;
            [Tooltip("Dikey görüş açısı (derece).")] public float fieldOfView;
            [Tooltip("Dikey lens kaydırması (sensör yüksekliği oranı); yalnız K1'de −0,324.")] public float lensShiftY;
        }

        [SerializeField] Camera view;
        [Tooltip("Girdi kilidi ve Overlay üstündeki imleç (CommandPostDesk).")]
        [SerializeField] CommandPostDesk desk;
        [SerializeField] PoseSlot[] poses = Array.Empty<PoseSlot>();
        [SerializeField] Vector2 sensorSizeMm = new Vector2(36, 20.25f);
        [Header("Geçiş (motion.poseS, motion.poseRiseS)")]
        [SerializeField, Min(0.01f)] float transitionSeconds = 0.45f;
        [SerializeField, Min(0)] float riseSeconds = 0.15f;
        [SerializeField, Min(0)] float riseMeters = 0.35f;
        [Header("Bakış ve yaklaşma")]
        [SerializeField, Range(0.01f, 0.3f)] float sensitivity = 0.09f;
        [SerializeField] Vector2 yawLimit = new Vector2(-25, 25);
        [SerializeField] Vector2 pitchLimit = new Vector2(-15, 18);
        [Tooltip("Görüş açısı çarpanının sınırları (tekerlek).")]
        [SerializeField] Vector2 zoomLimit = new Vector2(0.6f, 1.1f);
        [SerializeField, Min(0.1f)] float lookSmoothing = 18;
        [Header("Baskına geçiş (motion.crtDiveS; vuruşlar yolun kesirleri, üst üste biner)")]
        [Tooltip("Geri çekilme pozu (CrtBack): kamera önce buraya doğru çıkar (oda, masa ve CRT birlikte görünür), sonra CRT'ye döner.")]
        [SerializeField] Transform diveBack;
        [Tooltip("Geri çekilme pozunda dikey görüş açısı (derece).")]
        [SerializeField] float diveBackFieldOfView = 56;
        [Tooltip("Geri çekilme: konum, bakış, görüş açısı ve lens kaydırması başlangıçtan CrtBack'e (yolun 0–1 kesri).")]
        [SerializeField] Vector2 diveBackSpan = new Vector2(0, 0.6f);
        [Tooltip("Dönüş: bakış masadan CRT'ye (yolun kesri; geri çekilme bitmeden başlar).")]
        [SerializeField] Vector2 diveTurnSpan = new Vector2(0.3f, 0.85f);
        [Tooltip("Yaklaşma: konum, bakış ve görüş açısı ekrana (yolun kesri; dönüş bitmeden başlar).")]
        [SerializeField] Vector2 divePushSpan = new Vector2(0.45f, 1);

        SiginakPose current = SiginakPose.Desk, previous = SiginakPose.Desk;
        float elapsed = float.MaxValue, duration = 0.45f;
        State from, to;
        float yaw, pitch, zoom = 1, shownYaw, shownPitch, shownZoom = 1;
        bool dragging;

        struct State { public Vector3 position; public Quaternion rotation; public float fieldOfView, shift; }

        public SiginakPose Current => current;
        /// <summary>Odanın kamerası (ekran noktası → harita düzlemi).</summary>
        public Camera View => view;
        public bool InTransition => elapsed < duration;
        /// <summary>Geçişin kalan süresi (s); geçiş yoksa 0.</summary>
        public float TransitionRemaining => Mathf.Max(0, duration - elapsed);
        /// <summary>Ekip figürü taşınırken bakış ve tekerlek kapalı (Ekip-figuru.md).</summary>
        public bool InputSuspended { get; set; }
        /// <summary>Vardiya dizisi ve rapor (K6): kısayollar, bakış ve tekerlek kapalı; pozları dizi sürer.</summary>
        public bool Locked { get; set; }
        public event Action<SiginakPose> PoseChanged;

        public bool IsFocused => current == SiginakPose.Map;
        public void FocusMap() => Focus(SiginakPose.Map);
        public void FocusShelter() => Focus(SiginakPose.Shelf);
        public void FocusOperations() => Focus(SiginakPose.Crt);
        public void ResetView() => Focus(SiginakPose.Desk);

        /// <summary>Editör kurucusu bağlar (çalışma anında hiyerarşi yaratmaz).</summary>
        public void Configure(Camera camera, CommandPostDesk commandDesk, PoseSlot[] slots, Vector2 sensorMm,
            float poseSeconds, float poseRiseSeconds)
        {
            view = camera; desk = commandDesk; poses = slots; sensorSizeMm = sensorMm;
            transitionSeconds = poseSeconds; riseSeconds = poseRiseSeconds;
        }

        /// <summary>Baskın geçişinin geri çekilme pozu (editör kurucusu ve testler).</summary>
        public void ConfigureDive(Transform back, float backFieldOfView)
        {
            diveBack = back; diveBackFieldOfView = backFieldOfView;
        }

        public bool TryGetPose(SiginakPose pose, out PoseSlot slot)
        {
            foreach (var p in poses) if (p.pose == pose && p.anchor != null) { slot = p; return true; }
            slot = default;
            return false;
        }

        /// <summary>Poza geçer; <paramref name="instant"/> ise hemen oturur (editörde de çalışır).</summary>
        public void Focus(SiginakPose pose, bool instant = false) => Focus(pose, instant, transitionSeconds);

        /// <summary>Poza <paramref name="seconds"/> sürede geçer (vardiya dizisi: K6a'ya 0,4 s, harita pozuna 0,6 s).</summary>
        public void Focus(SiginakPose pose, float seconds) => Focus(pose, false, seconds);

        void Focus(SiginakPose pose, bool instant, float seconds)
        {
            if (!TryGetPose(pose, out var slot)) return;
            if (!instant && pose == current) { yaw = pitch = 0; zoom = 1; return; }
            duration = Mathf.Max(0.01f, seconds);
            previous = current;
            current = pose;
            yaw = pitch = 0; zoom = 1;
            dragging = false;
            to = new State { position = slot.anchor.position, rotation = slot.anchor.rotation, fieldOfView = slot.fieldOfView, shift = slot.lensShiftY };
            if (instant)
            {
                from = to;
                elapsed = float.MaxValue;
                shownYaw = shownPitch = 0; shownZoom = 1;
                Apply(to, 0, 0, 1);
            }
            else
            {
                from = Capture();
                elapsed = 0;
            }
            PoseChanged?.Invoke(pose);
        }

        public void Snap(SiginakPose pose) => Focus(pose, true);

        void Awake() { if (!view) view = GetComponent<Camera>(); }

        void OnEnable()
        {
            if (!view) view = GetComponent<Camera>();
            Snap(current);
        }

        void OnDisable() => dragging = false;

        void LateUpdate()
        {
            if (!Application.isPlaying || !view || !view.enabled) return;
            bool blocked = Locked || desk != null && desk.InteractionBlocked;
            if (!blocked) ReadInput(); else dragging = false;
            Step(Time.unscaledDeltaTime);
        }

        /// <summary>Geçişi ve bakışı <paramref name="dt"/> kadar ilerletip kameraya uygular (testler editörde doğrudan çağırır).</summary>
        public void Step(float dt)
        {
            if (elapsed < duration) elapsed += dt;
            float k = 1 - Mathf.Exp(-lookSmoothing * dt);
            shownYaw = Mathf.Lerp(shownYaw, yaw, k);
            shownPitch = Mathf.Lerp(shownPitch, pitch, k);
            shownZoom = Mathf.Lerp(shownZoom, zoom, k);
            Apply(Evaluate(), shownYaw, shownPitch, shownZoom);
        }

        void ReadInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && !TextFieldSelected())
            {
                if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) FocusShelter();
                else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) FocusMap();
                else if (keyboard.homeKey.wasPressedThisFrame) ResetView();
                else if (keyboard.fKey.wasPressedThisFrame) { if (current == SiginakPose.Map) ResetView(); else FocusMap(); }
            }

            var mouse = Mouse.current;
            if (mouse == null || InputSuspended) { dragging = false; return; }
            float scroll = mouse.scroll.ReadValue().y;
            bool overOverlay = (mouse.rightButton.isPressed || scroll != 0) && desk != null && desk.PointerOverOverlay;
            if (!mouse.rightButton.isPressed || overOverlay) dragging = false;
            if (mouse.rightButton.wasPressedThisFrame && !overOverlay) dragging = true;
            if (dragging)
            {
                var delta = mouse.delta.ReadValue();
                yaw = Mathf.Clamp(yaw + delta.x * sensitivity, yawLimit.x, yawLimit.y);
                pitch = Mathf.Clamp(pitch - delta.y * sensitivity, pitchLimit.x, pitchLimit.y);
            }
            if (!overOverlay && scroll != 0) zoom = Mathf.Clamp(zoom - scroll * 0.0006f, zoomLimit.x, zoomLimit.y);
        }

        /// <summary>Geçişin o anki hâli. K1 → harita: önce yükselir (yalnız konum), sonra eğilir ve yaklaşır.</summary>
        State Evaluate()
        {
            if (elapsed >= duration) return to;
            float t = Mathf.Clamp01(elapsed / duration);
            bool rise = previous == SiginakPose.Desk && current == SiginakPose.Map && riseSeconds > 0 && riseSeconds < duration;
            if (!rise) return Lerp(from, to, Ease(t));
            var risen = from;
            risen.position += Vector3.up * riseMeters;
            if (elapsed < riseSeconds) return Lerp(from, risen, Ease(elapsed / riseSeconds));
            return Lerp(risen, to, Ease((elapsed - riseSeconds) / (duration - riseSeconds)));
        }

        static float Ease(float t) => t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;

        // ---------- Baskına geçiş (CRT'ye dalış ve dönüş) ----------

        // Yolun dört durağı (başlangıç → geri çekilme → CRT'ye dönük → hedef); dönüşte aynı yol tersinden okunur
        State diveFrom, diveBackState, diveTurned, diveTo;
        bool diveReverse;
        float diveElapsed, diveSeconds, diveDelay;

        static float DivePathSeconds => SiginakTheme.Active != null ? SiginakTheme.Active.NumberOf("motion.crtDiveS", 2.4f) : 2.4f;

        /// <summary>CRT'ye dalışın süresi (dönüşte de aynı; bekleme hariç).</summary>
        public float DiveSeconds => DivePathSeconds;
        /// <summary>Dalış ya da dönüş sürüyor (kamera denetimi başlatıcıda kilitli; <see cref="StepDive"/> ilerletir).</summary>
        public bool Diving => diveElapsed < diveSeconds;

        /// <summary>
        /// Baskına dalış, tek ve kesintisiz hareket: kamera bulunduğu yerden geri çekilme pozuna doğru çıkar (görüş açısı genişler,
        /// lens kaydırması sıfırlanır), daha varmadan CRT'ye dönmeye, dönüş bitmeden ekrana yaklaşmaya başlar ve
        /// <paramref name="target"/> (CrtDive) pozunda <paramref name="fieldOfView"/> açısıyla durur. Vuruşlar yolun kesirleri
        /// olarak üst üste biner (<c>diveBackSpan</c>, <c>diveTurnSpan</c>, <c>divePushSpan</c>), her biri yumuşak başlar ve
        /// biter: yol ortasında durma ya da ani dönüş yok.
        /// </summary>
        public void BeginDive(Transform target, float fieldOfView)
        {
            var end = new State { position = target.position, rotation = target.rotation, fieldOfView = fieldOfView, shift = 0 };
            Plan(Capture(), target, end, false, 0);
        }

        /// <summary>
        /// Baskından dönüş, dalışın aynası: kamera <paramref name="delay"/> boyunca monitörde (<paramref name="from"/>) bekler,
        /// sonra aynı yolu tersinden yürüyüp kameranın geçerli pozuna (genelde harita) oturur.
        /// </summary>
        public void BeginReturn(Transform from, float fieldOfView, float delay)
        {
            var monitor = new State { position = from.position, rotation = from.rotation, fieldOfView = fieldOfView, shift = 0 };
            var pose = TryGetPose(current, out var slot)
                ? new State { position = slot.anchor.position, rotation = slot.anchor.rotation, fieldOfView = slot.fieldOfView, shift = slot.lensShiftY }
                : Capture();
            Plan(pose, from, monitor, true, delay);
        }

        /// <summary>Dalışı ya da dönüşü <paramref name="dt"/> kadar ilerletip kameraya uygular.</summary>
        public void StepDive(float dt)
        {
            if (!Diving) return;
            diveElapsed += dt;
            Apply(EvaluateDive(), 0, 0, 1);
        }

        /// <summary>Atlandı: kamera hemen yolun sonuna oturur.</summary>
        public void FinishDive()
        {
            diveElapsed = diveSeconds;
            Apply(SampleDive(diveReverse ? 0 : 1), 0, 0, 1);
        }

        /// <summary>Coroutine sarmalayıcısı: her kare ilerler, <paramref name="skip"/> doğru dönerse sona atlar.</summary>
        public IEnumerator RunDive(Func<bool> skip)
        {
            while (Diving)
            {
                if (skip != null && skip()) { FinishDive(); yield break; }
                StepDive(Time.unscaledDeltaTime);
                yield return null;
            }
        }

        /// <param name="from">Yolun başı (dalışta kameranın o anki hâli, dönüşte varılacak poz).</param>
        /// <param name="target">CRT hedefi (CrtDive); geri çekilme yerinden ona bakılır.</param>
        void Plan(State from, Transform target, State to, bool reverse, float delay)
        {
            diveFrom = from;
            diveTo = to;
            diveBackState = diveBack != null
                ? new State { position = diveBack.position, rotation = diveBack.rotation, fieldOfView = diveBackFieldOfView, shift = 0 }
                : from;
            // Geri çekilme yerinden CRT'ye bakış: yaklaşma bu bakış doğrusuna yakın gider
            diveTurned = diveBackState;
            var look = target.position - diveBackState.position;
            if (look.sqrMagnitude > 1e-6f) diveTurned.rotation = Quaternion.LookRotation(look, Vector3.up);
            diveReverse = reverse;
            diveDelay = Mathf.Max(0, delay);
            diveSeconds = diveDelay + Mathf.Max(0.01f, DivePathSeconds);
            diveElapsed = 0;
            Apply(EvaluateDive(), 0, 0, 1);
        }

        State EvaluateDive()
        {
            float u = Mathf.Clamp01((diveElapsed - diveDelay) / Mathf.Max(0.01f, diveSeconds - diveDelay));
            return SampleDive(diveReverse ? 1 - u : u);
        }

        /// <summary>
        /// Yolun <paramref name="u"/> (0–1) noktası. Konum, bakış (eğim ve sapma), görüş açısı ve lens kaydırması üç ağırlığın
        /// toplamıdır; ağırlıklar kendi kesirlerinde 0'dan 1'e smootherstep ile çıkar (hız ve ivme sürekli). u = 0 başlangıç,
        /// u = 1 hedef, ikisi de birebir.
        /// </summary>
        State SampleDive(float u)
        {
            float back = Smoother(diveBackSpan, u), turn = Smoother(diveTurnSpan, u), push = Smoother(divePushSpan, u);
            Vector2 a = Angles(diveFrom.rotation), b = Angles(diveBackState.rotation), t = Angles(diveTurned.rotation), e = Angles(diveTo.rotation);
            float pitch = a.x + back * Mathf.DeltaAngle(a.x, b.x) + turn * Mathf.DeltaAngle(b.x, t.x) + push * Mathf.DeltaAngle(t.x, e.x);
            float yaw = a.y + back * Mathf.DeltaAngle(a.y, b.y) + turn * Mathf.DeltaAngle(b.y, t.y) + push * Mathf.DeltaAngle(t.y, e.y);
            return new State
            {
                position = diveFrom.position + back * (diveBackState.position - diveFrom.position) + push * (diveTo.position - diveBackState.position),
                rotation = Quaternion.Euler(pitch, yaw, 0),
                fieldOfView = diveFrom.fieldOfView + back * (diveBackState.fieldOfView - diveFrom.fieldOfView) + push * (diveTo.fieldOfView - diveBackState.fieldOfView),
                shift = diveFrom.shift + back * (diveBackState.shift - diveFrom.shift) + push * (diveTo.shift - diveBackState.shift),
            };
        }

        /// <summary>Eğim (−180…180) ve sapma; kamera pozlarında yuvarlanma yok.</summary>
        static Vector2 Angles(Quaternion q)
        {
            var e = q.eulerAngles;
            return new Vector2(Mathf.DeltaAngle(0, e.x), e.y);
        }

        /// <summary>Kesir içinde 0 → 1 smootherstep (6t⁵ − 15t⁴ + 10t³): uçlarda hız ve ivme sıfır.</summary>
        static float Smoother(Vector2 span, float u)
        {
            if (span.y <= span.x) return u >= span.x ? 1 : 0;
            float t = Mathf.Clamp01((u - span.x) / (span.y - span.x));
            return t * t * t * (t * (6 * t - 15) + 10);
        }

        static State Lerp(State a, State b, float t) => new State
        {
            position = Vector3.Lerp(a.position, b.position, t),
            rotation = Quaternion.Slerp(a.rotation, b.rotation, t),
            fieldOfView = Mathf.Lerp(a.fieldOfView, b.fieldOfView, t),
            shift = Mathf.Lerp(a.shift, b.shift, t),
        };

        State Capture() => new State
        {
            position = transform.position, rotation = transform.rotation,
            fieldOfView = view != null && view.usePhysicalProperties ? Camera.FocalLengthToFieldOfView(view.focalLength, view.sensorSize.y) : view != null ? view.fieldOfView : 60,
            shift = view != null ? view.lensShift.y : 0,
        };

        void Apply(State s, float lookYaw, float lookPitch, float lookZoom)
        {
            if (!view) view = GetComponent<Camera>();
            var rotation = Quaternion.AngleAxis(lookYaw, Vector3.up) * s.rotation * Quaternion.Euler(lookPitch, 0, 0);
            transform.SetPositionAndRotation(s.position, rotation);
            view.usePhysicalProperties = true;
            view.sensorSize = sensorSizeMm;
            view.gateFit = Camera.GateFitMode.Vertical;
            view.focalLength = Camera.FieldOfViewToFocalLength(Mathf.Clamp(s.fieldOfView * lookZoom, 10, 100), sensorSizeMm.y);
            view.lensShift = new Vector2(0, s.shift);
            view.nearClipPlane = 0.03f;
        }

        static bool TextFieldSelected()
        {
            var events = EventSystem.current;
            var selected = events ? events.currentSelectedGameObject : null;
            return selected && (selected.GetComponentInParent<TMP_InputField>() || selected.GetComponentInParent<InputField>());
        }
    }
}
