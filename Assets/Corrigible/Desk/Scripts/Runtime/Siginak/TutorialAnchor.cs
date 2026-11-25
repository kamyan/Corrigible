using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Rehberin işaret edebileceği bir hedef (geliştirici, 2026-11-24: "şuraya bas şuraya bas"): sahnede ya da prefab'da hedef nesnenin
    /// üstünde durur, kimliği (<see cref="Id"/>, ör. "key.shelter", "raid.end") rehber adımının istediği hedeftir. Ekran dikdörtgeni
    /// nesnenin kendisinden okunur: UI öğesi (RectTransform, tuvalin kamerasıyla) ya da 3B nesne (altındaki açık renderer'ların, yoksa
    /// collider'ların sınırı; <see cref="view"/> kamerasıyla, boşsa ana kamera). Yalnız okur; girdiyi değiştirmez.
    /// </summary>
    public sealed class TutorialAnchor : MonoBehaviour
    {
        [Tooltip("Rehber adımının hedef kimliği (PlanningTutorialFlow / TutorialTargets).")]
        [SerializeField] string id;
        [Tooltip("3B hedefi gösteren kamera (masa kamerası); boşsa Camera.main. UI öğesinde tuvalin kamerası kullanılır.")]
        [SerializeField] Camera view;

        static readonly List<TutorialAnchor> Live = new List<TutorialAnchor>();
        static readonly Vector3[] Corners = new Vector3[4];

        public string Id => id;

        /// <summary>Kurulum (tek seferlik sahne betiği ve testler).</summary>
        public void Configure(string anchorId, Camera camera = null) { id = anchorId; view = camera; }

        void OnEnable() { if (!Live.Contains(this)) Live.Add(this); }
        void OnDisable() => Live.Remove(this);

        /// <summary>Kimliğin görünür ilk hedefinin ekran dikdörtgeni (piksel, sol alt köşe başlangıç); yoksa false.</summary>
        public static bool TryRect(string anchorId, out Rect rect)
        {
            rect = default;
            if (string.IsNullOrEmpty(anchorId)) return false;
            for (int i = 0; i < Live.Count; i++)
            {
                var a = Live[i];
                if (a == null || a.id != anchorId || !a.isActiveAndEnabled) continue;
                if (a.ScreenRect(out rect)) return true;
            }
            return false;
        }

        /// <summary>Kimlikte en az bir etkin hedef var mı (sahne bağı testleri).</summary>
        public static bool Exists(string anchorId) => Live.Exists(a => a != null && a.id == anchorId && a.isActiveAndEnabled);

        /// <summary>Hedefin ekran dikdörtgeni; görünmüyorsa (kameranın arkasında, sıfır boy) false.</summary>
        public bool ScreenRect(out Rect rect)
        {
            rect = default;
            if (transform is RectTransform rt && rt.GetComponentInParent<Canvas>() is Canvas canvas)
            {
                var root = canvas.rootCanvas;
                var cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera != null ? root.worldCamera : view != null ? view : Camera.main;
                rt.GetWorldCorners(Corners);
                // Kameralı tuval (dünya uzayı, kamera ekranı): köşe kameranın arkasındaysa görünmez sayılır (ayna görüntü çizilmez)
                if (cam != null) return Project(cam, Corners, out rect) && OnScreen(rect);
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                for (int i = 0; i < 4; i++)
                {
                    var p = RectTransformUtility.WorldToScreenPoint(null, Corners[i]);
                    x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
                }
                rect = Rect.MinMaxRect(x0, y0, x1, y1);
                return rect.width > 1 && rect.height > 1 && OnScreen(rect);
            }
            var camera = view != null ? view : Camera.main;
            if (camera == null || !camera.isActiveAndEnabled) return false;
            if (!Bounds(out var b)) return false;
            return Project(camera, b, out rect) && OnScreen(rect);
        }

        /// <summary>Dikdörtgen ekranla kesişiyor mu (ekran dışındaki hedef işaretlenmez).</summary>
        public static bool OnScreen(Rect rect) => rect.Overlaps(new Rect(0, 0, Screen.width, Screen.height));

        bool Bounds(out Bounds b)
        {
            b = default; bool any = false;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (any) return true;
            foreach (var c in GetComponentsInChildren<Collider>())
            {
                if (!c.enabled) continue;
                if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds);
            }
            return any;
        }

        /// <summary>Dünya noktalarının ekran dikdörtgeni; bir nokta kameranın arkasındaysa false.</summary>
        public static bool Project(Camera camera, IList<Vector3> points, out Rect rect)
        {
            rect = default;
            if (camera == null || points == null || points.Count == 0) return false;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var w in points)
            {
                var p = camera.WorldToScreenPoint(w);
                if (p.z <= 0) return false;
                x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
            }
            rect = Rect.MinMaxRect(x0, y0, x1, y1);
            return rect.width > 1 && rect.height > 1;
        }

        /// <summary>Dünya kutusunun ekran dikdörtgeni (sekiz köşe); bir köşe kameranın arkasındaysa false.</summary>
        public static bool Project(Camera camera, Bounds b, out Rect rect)
        {
            rect = default;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                var p = camera.WorldToScreenPoint(corner);
                if (p.z <= 0) return false;
                x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
            }
            rect = Rect.MinMaxRect(x0, y0, x1, y1);
            return rect.width > 1 && rect.height > 1;
        }
    }
}
