using System;
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
    }

    /// <summary>
    /// Sığınak kamerası: pozlar arasında 0,45 s ease-in-out geçiş (K1 → harita önce 0,15 s yükselir, sonra eğilir),
    /// sağ tuşla sınırlı bakış, tekerlekle yaklaşma, 1/2/3/F/Home kısayolları. Kamera her pozda fiziksel kalır: dikey FOV
    /// odak uzaklığıyla, K1'in ufuk kaydırması lens kaydırmasıyla verilir; geçişte ikisi birlikte değişir (sıçrama olmaz).
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

        SiginakPose current = SiginakPose.Desk, previous = SiginakPose.Desk;
        float elapsed = float.MaxValue, duration = 0.45f;
        State from, to;
        float yaw, pitch, zoom = 1, shownYaw, shownPitch, shownZoom = 1;
        bool dragging;

        struct State { public Vector3 position; public Quaternion rotation; public float fieldOfView, shift; }

        public SiginakPose Current => current;
        public bool InTransition => elapsed < duration;
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
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) FocusMap();
                else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) FocusShelter();
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
