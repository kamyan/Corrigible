using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Haritanın tasarım dikdörtgenini kâğıdı kaplayacak kadar büyütür; harita üstündeki işaretler Changed sonrasında
    /// ToCanvas ile yalnız konumlarını yeniler.
    /// </summary>
    [ExecuteAlways]
    public sealed class MapCoverFitter : MonoBehaviour
    {
        [SerializeField] RectTransform viewport;
        [SerializeField] RectTransform mapRoot;

        bool applying;
        Vector2 lastSize;
        public float Scale { get; private set; } = 1;
        public Vector2 ViewportSize => viewport != null ? viewport.rect.size : MapProjection.DesignSize;
        public event Action Changed;

        void OnEnable() => Refresh();
        void OnValidate() => DeskUi.Deferred(this, Refresh);
        void OnRectTransformDimensionsChange() { if (!applying) Refresh(); }
        void LateUpdate() { if (ViewportSize != lastSize) Refresh(); }

        /// <summary>Editör kurucusu için serileştirilmiş iki referansı bağlar; çalışma anında hiyerarşi yaratmaz.</summary>
        public void Configure(RectTransform canvasViewport, RectTransform scaledMap)
        {
            viewport = canvasViewport;
            mapRoot = scaledMap;
            Refresh();
        }

        public void Refresh()
        {
            if (applying) return;
            applying = true;
            try
            {
                Vector2 size = ViewportSize;
                bool changed = size != lastSize;
                lastSize = size;
                float scale = CoverScale(size);
                changed |= !Mathf.Approximately(Scale, scale);
                Scale = scale;
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
                if (changed) Changed?.Invoke();
            }
            finally { applying = false; }
        }

        /// <summary>Tasarım noktasını ölçeklenmeyen canvas'ın sol-üst koordinatına çevirir.</summary>
        public Vector2 ToCanvas(Vector2 designPoint) => MapToCanvas(designPoint, ViewportSize);
        public Vector2 ToMap(Vector2 canvasPoint) => CanvasToMap(canvasPoint, ViewportSize);

        public static float CoverScale(Vector2 canvasSize) => Mathf.Max(
            Mathf.Max(0, canvasSize.x) / MapProjection.DesignWidth,
            Mathf.Max(0, canvasSize.y) / MapProjection.DesignHeight);

        public static Vector2 MapToCanvas(Vector2 designPoint, Vector2 canvasSize) =>
            canvasSize / 2 + (designPoint - MapProjection.DesignSize / 2) * CoverScale(canvasSize);

        public static Vector2 CanvasToMap(Vector2 canvasPoint, Vector2 canvasSize)
        {
            float scale = CoverScale(canvasSize);
            return scale > 0 ? (canvasPoint - canvasSize / 2) / scale + MapProjection.DesignSize / 2 : MapProjection.DesignSize / 2;
        }
    }
}
