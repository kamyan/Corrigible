using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Komuta masasında oturan oyuncunun sınırlı bakışını ve harita odağını yönetir.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CommandPostCamera : MonoBehaviour
    {
        [SerializeField] CommandPostDesk desk;
        [SerializeField] Camera view;
        [SerializeField] Transform homePose;
        [SerializeField] Transform mapPose;
        [SerializeField] Transform shelterPose;
        [SerializeField] Transform operationsPose;
        [SerializeField, Range(.01f, .3f)] float sensitivity = .09f;
        [SerializeField] Vector2 pitchLimits = new Vector2(10f, 60f);
        [SerializeField, Range(0f, 60f)] float yawLimit = 28f;
        [SerializeField, Range(26f, 60f)] float homeFieldOfView = 52f;
        [SerializeField, Range(26f, 60f)] float mapFieldOfView = 34f;
        [SerializeField, Min(.1f)] float positionSmoothing = 10f;
        [SerializeField, Min(.1f)] float rotationSmoothing = 18f;
        [SerializeField, Min(.1f)] float zoomSmoothing = 12f;

        Vector3 initialPosition;
        Quaternion initialRotation;
        Vector3 targetPosition;
        float baseYaw;
        float yaw;
        float pitch;
        float targetFieldOfView;
        bool initialized;
        bool dragging;
        bool focused;

        public bool IsFocused => focused;
        public void FocusShelter() { Initialize(); SetTargetPose(shelterPose, 48); }
        public void FocusOperations() { Initialize(); SetTargetPose(operationsPose, 46); }

        void SetTargetPose(Transform pose, float fov)
        {
            if (pose == null) return;
            focused = false; dragging = false; yaw = 0;
            targetPosition = pose.position; baseYaw = pose.eulerAngles.y;
            pitch = Mathf.Clamp(Mathf.DeltaAngle(0, pose.eulerAngles.x), pitchLimits.x, pitchLimits.y);
            targetFieldOfView = fov;
        }

        void Awake()
        {
            Initialize();
        }

        void Initialize()
        {
            if (initialized) return;
            if (!view) view = GetComponent<Camera>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initialized = true;
            SetTarget(false);
        }

        /// <summary>Haritanın üzerindeki yakın okuma konumuna yumuşak geçişi başlatır.</summary>
        public void FocusMap()
        {
            Initialize();
            SetTarget(true);
        }

        /// <summary>Bakış açısını, yakınlaştırmayı ve oturma konumunu başlangıca döndürür.</summary>
        public void ResetView()
        {
            Initialize();
            SetTarget(false);
        }

        void SetTarget(bool mapFocused)
        {
            focused = mapFocused;
            dragging = false;
            Transform pose = mapFocused && mapPose ? mapPose : homePose;
            targetPosition = pose ? pose.position : initialPosition;
            Vector3 angles = pose ? pose.rotation.eulerAngles : initialRotation.eulerAngles;
            baseYaw = angles.y;
            yaw = 0f;
            pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, angles.x), pitchLimits.x, pitchLimits.y);
            targetFieldOfView = Mathf.Clamp(mapFocused ? mapFieldOfView : homeFieldOfView, 26f, 60f);
        }

        void LateUpdate()
        {
            if (!Application.isPlaying || !view || !view.enabled || !desk || desk.InteractionBlocked)
            {
                dragging = false;
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && !TextFieldSelected())
            {
                if (shelterPose != null && keyboard.digit2Key.wasPressedThisFrame) FocusShelter();
                else if (operationsPose != null && keyboard.digit3Key.wasPressedThisFrame) FocusMap();
                else if (keyboard.digit1Key.wasPressedThisFrame)
                {
                    if (operationsPose != null) FocusMap(); else ResetView();
                }
                if (keyboard.homeKey.wasPressedThisFrame) ResetView();
                else if (keyboard.fKey.wasPressedThisFrame)
                {
                    if (focused) ResetView();
                    else FocusMap();
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                bool overOverlay = (mouse.rightButton.isPressed || scroll != 0) && desk.PointerOverOverlay;
                if (!mouse.rightButton.isPressed || overOverlay) dragging = false;
                if (mouse.rightButton.wasPressedThisFrame && !overOverlay) dragging = true;
                if (dragging)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    yaw = Mathf.Clamp(yaw + delta.x * sensitivity, -yawLimit, yawLimit);
                    pitch = Mathf.Clamp(pitch - delta.y * sensitivity, pitchLimits.x, pitchLimits.y);
                }
                if (!overOverlay)
                    targetFieldOfView = Mathf.Clamp(targetFieldOfView - scroll * .025f, 26f, 60f);
            }
            else dragging = false;

            float elapsed = Time.unscaledDeltaTime;
            transform.position = Vector3.Lerp(transform.position, targetPosition,
                1f - Mathf.Exp(-positionSmoothing * elapsed));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(pitch, baseYaw + yaw, 0f),
                1f - Mathf.Exp(-rotationSmoothing * elapsed));
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, targetFieldOfView,
                1f - Mathf.Exp(-zoomSmoothing * elapsed));
        }

        static bool TextFieldSelected()
        {
            EventSystem events = EventSystem.current;
            GameObject selected = events ? events.currentSelectedGameObject : null;
            return selected && (selected.GetComponentInParent<TMP_InputField>() ||
                                selected.GetComponentInParent<InputField>());
        }

        void OnDisable()
        {
            dragging = false;
        }
    }
}
