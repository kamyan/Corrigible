using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Harita ve kâğıt farklı canvas düzlemlerindeyken aynı ekran noktasını korur.</summary>
    public static class DeskCanvasProjection
    {
        public static Canvas RootCanvas(Component source)
        {
            var canvas = source != null ? source.GetComponentInParent<Canvas>() : null;
            return canvas != null ? canvas.rootCanvas : null;
        }

        public static bool IsWorldSpace(Component source)
        {
            var canvas = RootCanvas(source);
            return canvas != null && canvas.renderMode == RenderMode.WorldSpace;
        }

        public static bool RequiresProjection(Component source, RectTransform target)
        {
            var from = RootCanvas(source);
            var to = RootCanvas(target);
            return from != null && to != null && from != to &&
                (from.renderMode != RenderMode.ScreenSpaceOverlay || to.renderMode != RenderMode.ScreenSpaceOverlay);
        }

        /// <summary>Noktayı hedef dikdörtgenin dünya düzlemine, kameraların ekrandaki izdüşümüyle taşır.</summary>
        public static Vector3 WorldPoint(Component source, Vector3 world, RectTransform target)
        {
            if (target == null || !RequiresProjection(source, target)) return world;
            var from = RootCanvas(source);
            var to = RootCanvas(target);
            var sourceCamera = from.renderMode == RenderMode.ScreenSpaceOverlay ? null : from.worldCamera;
            var targetCamera = to.renderMode == RenderMode.ScreenSpaceOverlay ? null : to.worldCamera;
            var screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, world);
            return RectTransformUtility.ScreenPointToWorldPointInRectangle(target, screen, targetCamera, out var point)
                ? point : world;
        }

        public static Vector2 LocalPoint(Component source, Vector3 world, RectTransform target) =>
            target.InverseTransformPoint(WorldPoint(source, world, target));
    }
}
