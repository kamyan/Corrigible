using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Sade tahta: k=1,5, 35°/45° ve ekran ortasının 12 referans piksel üstünde sabit merkez.</summary>
    [ExecuteAlways, DefaultExecutionOrder(-100)]
    public sealed class SadeRaidCameraFraming : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] Canvas referenceCanvas;
        [SerializeField] SadeTheme theme;
        [SerializeField] Vector3 boardCenter;

        public Camera Camera => targetCamera;
        public Canvas ReferenceCanvas => referenceCanvas;
        public float PixelsPerUnit => theme.Number("raid.camera.pixelsPerUnitAt1080", 132.5825f);

        void OnEnable() => Refresh();
        void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (targetCamera == null || referenceCanvas == null || theme == null) return;
            // Expand, referans 1920×1080: 16:10'da dikey, 21:9'da yatay keçe genişler.
            float scale = Mathf.Min(targetCamera.pixelWidth / 1920f, targetCamera.pixelHeight / 1080f);
            if(!targetCamera.orthographic)targetCamera.orthographic = true;
            float size=Size(targetCamera.pixelHeight, scale, PixelsPerUnit);
            if(targetCamera.orthographicSize!=size)targetCamera.orthographicSize=size;
            var t = targetCamera.transform;
            var rotation=Quaternion.Euler(35,45,0);
            if(t.rotation!=rotation)t.rotation=rotation;
            float up = 540 - theme.Number("raid.camera.boardCenterPx[1]", 528);
            var position=boardCenter - t.forward * 20 - t.up * (up / PixelsPerUnit);
            if(t.position!=position)t.position=position;
        }

        public static float Size(float pixelHeight, float canvasScale, float pixelsPerUnit) =>
            pixelHeight / Mathf.Max(.001f, canvasScale) / (2 * pixelsPerUnit);
    }
}
