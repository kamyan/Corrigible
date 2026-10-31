using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Haritanın tasarım dikdörtgenini kâğıdı kaplayacak kadar büyütür. Taşan kısmı (ve dikdörtgenden taşan bölge
    /// çokgenlerini) <see cref="viewport"/>'taki RectMask2D keser.
    /// </summary>
    [ExecuteAlways]
    public sealed class MapCoverFitter : MonoBehaviour
    {
        [SerializeField] RectTransform viewport;
        [SerializeField] RectTransform mapRoot;

        bool applying;
        Vector2 lastSize;
        public Vector2 ViewportSize => viewport != null ? viewport.rect.size : MapProjection.DesignSize;

        void OnEnable() => Refresh();
        void OnValidate() => DeskUi.Deferred(this, Refresh);
        void OnRectTransformDimensionsChange() { if (!applying) Refresh(); }
        void LateUpdate() { if (ViewportSize != lastSize) Refresh(); }

        public void Refresh()
        {
            if (applying) return;
            applying = true;
            try
            {
                Vector2 size = ViewportSize;
                lastSize = size;
                float scale = CoverScale(size);
                if (mapRoot != null)
                {
                    var middle = new Vector2(0.5f, 0.5f);
                    if (mapRoot.anchorMin != middle) mapRoot.anchorMin = middle;
                    if (mapRoot.anchorMax != middle) mapRoot.anchorMax = middle;
                    if (mapRoot.pivot != middle) mapRoot.pivot = middle;
                    if (mapRoot.anchoredPosition != Vector2.zero) mapRoot.anchoredPosition = Vector2.zero;
                    if (mapRoot.sizeDelta != MapProjection.DesignSize) mapRoot.sizeDelta = MapProjection.DesignSize;
                    var localScale = new Vector3(scale, scale, 1);
                    if (mapRoot.localScale != localScale) mapRoot.localScale = localScale;
                }
            }
            finally { applying = false; }
        }

        public static float CoverScale(Vector2 canvasSize) => Mathf.Max(
            Mathf.Max(0, canvasSize.x) / MapProjection.DesignWidth,
            Mathf.Max(0, canvasSize.y) / MapProjection.DesignHeight);
    }
}
