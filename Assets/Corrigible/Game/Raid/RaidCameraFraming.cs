using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.Raid
{
    /// <summary>İzometrik maketi değişen ekran boyutunda iki kâğıt sütununun arasında tutar.</summary>
    public sealed class RaidCameraFraming : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform viewport;
        [SerializeField] Vector3 boardCenter;
        [SerializeField] float pixelsPerUnit = 88.3885f;
        CanvasScaler scaler;

        void Awake() { if (canvas != null) scaler = canvas.GetComponent<CanvasScaler>(); }

        void Update()
        {
            // Geniş ekranda 1080 referans yüksekliği korunur; sol kâğıtlar üst üste binmez.
            if (scaler != null && scaler.enabled)
                scaler.matchWidthOrHeight = Screen.width / (float)Mathf.Max(1, Screen.height) > 16f / 9f ? 1f : .5f;
        }

        void LateUpdate()
        {
            if (targetCamera == null || canvas == null || viewport == null) return;
            float scale = Mathf.Max(0.01f, canvas.scaleFactor);
            targetCamera.orthographicSize = targetCamera.pixelHeight / scale / (pixelsPerUnit * 2f);
            Vector3 point = viewport.TransformPoint(viewport.rect.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, point);
            float dx = (screen.x - targetCamera.pixelWidth * 0.5f) / scale / pixelsPerUnit;
            float dy = (screen.y - targetCamera.pixelHeight * 0.5f) / scale / pixelsPerUnit;
            var t = targetCamera.transform;
            t.position = boardCenter - t.forward * 14f - t.right * dx - t.up * dy;
        }
    }
}
