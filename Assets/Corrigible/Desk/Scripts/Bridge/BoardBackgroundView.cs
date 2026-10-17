using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Keçe/lamba 3D tahtanın arkasındadır; tabanın izdüşümüyle gölgeyi yalnız kadraj değişince yeniler.</summary>
    [ExecuteAlways, DefaultExecutionOrder(100)]
    public sealed class BoardBackgroundView : MonoBehaviour
    {
        [SerializeField] Camera boardCamera;
        [SerializeField] Canvas background;
        [SerializeField] RawImage felt;
        [SerializeField] UIPolygonGraphic shadow;
        [SerializeField] Vector3[] baseCorners;
        [SerializeField] Vector2 shadowOffset = new Vector2(15, -18);
        [SerializeField] float feltTile = 128;
        Matrix4x4 previous;
        Vector2 previousPixels;
        readonly Vector2[] points = new Vector2[4];

        public UIPolygonGraphic Shadow => shadow;
        void OnEnable() => Refresh(true);
        void LateUpdate() => Refresh();
        public void Refresh(bool force = false)
        {
            if (boardCamera == null || background == null || shadow == null || baseCorners == null || baseCorners.Length != 4) return;
            var pixels = new Vector2(boardCamera.pixelWidth, boardCamera.pixelHeight);
            var matrix = boardCamera.projectionMatrix * boardCamera.worldToCameraMatrix;
            if (!force && previous == matrix && previousPixels == pixels) return;
            previous = matrix; previousPixels = pixels;
            background.planeDistance = boardCamera.farClipPlane - 1;
            var root = (RectTransform)shadow.transform;
            float scale = Mathf.Min(pixels.x / 1920, pixels.y / 1080);
            for (int i = 0; i < 4; i++)
            {
                Vector2 screen = boardCamera.WorldToScreenPoint(baseCorners[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen + shadowOffset * scale, boardCamera, out points[i]);
            }
            shadow.SetConvex(points);
            if (felt != null) felt.uvRect = new Rect(0, 0, pixels.x / Mathf.Max(.01f, scale) / feltTile, pixels.y / Mathf.Max(.01f, scale) / feltTile);
        }
    }
}
