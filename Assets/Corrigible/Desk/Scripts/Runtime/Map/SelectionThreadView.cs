using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Seçim ipliği (C1 §7.1 madde 10): seçili düğümün raptiyesinden açık dosyanın sekmesindeki raptiyeye; 1,5 px #2B2622 +
    /// gölge kopyası (1,5; 2; %25; 1,8 px). Uçlar gerçek düğüm ve sekme konumlarından okunur; en-boy oranı değişince ya da
    /// pencere taşınınca (F3) kendiliğinden güncellenir (yalnızca konum değişince yeniden çizilir). İllüzyonda gizlenir.
    /// </summary>
    public sealed class SelectionThreadView : MonoBehaviour
    {
        [SerializeField] UILineGraphic shadow;
        [SerializeField] UILineGraphic thread;
        [SerializeField] Image endPin;
        [SerializeField] Vector2 shadowOffset = new Vector2(1.5f, 2);
        [Tooltip("Raptiye düğüm merkezinin 14 px üstünde (map.json pin).")]
        [SerializeField] Vector2 sourceOffset = new Vector2(0, 14);
        [Tooltip("Sekmedeki raptiye: sekmenin sol-üstünden sağa 16, aşağı 14.")]
        [SerializeField] Vector2 targetOffset = new Vector2(16, -14);

        RectTransform source, target;
        bool wanted, suppressed;
        Vector3 lastA, lastB;
        static readonly List<Vector2> pts = new List<Vector2>(2);
        static readonly Vector3[] corners = new Vector3[4];

        public void SetTarget(RectTransform tab) { target = tab; lastA = lastB = Vector3.positiveInfinity; }

        /// <summary>Seçili düğüm (null = seçim yok).</summary>
        public void Bind(RectTransform node, bool visible)
        {
            source = node;
            wanted = visible;
            ApplyActive();
            // Raptiye: sabit boy, pivot iğne ucunda (sprite pivotu)
            var pr = endPin.rectTransform;
            var sp = endPin.sprite;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            if (sp != null) pr.pivot = new Vector2(sp.pivot.x / sp.rect.width, sp.pivot.y / sp.rect.height);
            pr.sizeDelta = new Vector2(14, 14);
            lastA = lastB = Vector3.positiveInfinity;
            Refresh();
        }


        /// <summary>Dosya arkadaysa ya da kapalıysa iplik gizlenir (sekmesi başka pencerenin altında kalır).</summary>
        public void SetSuppressed(bool on)
        {
            if (suppressed == on) return;
            suppressed = on;
            ApplyActive();
        }

        void ApplyActive()
        {
            bool on = wanted && source != null && !suppressed;
            if (gameObject.activeSelf != on) gameObject.SetActive(on);
            if (on) { lastA = lastB = Vector3.positiveInfinity; Refresh(); }
        }

        void LateUpdate() => Refresh();

        /// <summary>Uçları yeniden okur (yakalama aracı canvas'ı başka çözünürlüğe çizmeden önce de çağırır).</summary>
        public void Refresh()
        {
            if (source == null || target == null) return;
            var a = source.TransformPoint(sourceOffset);
            target.GetWorldCorners(corners);   // 1 = sol-üst
            var b = corners[1] + target.TransformVector(targetOffset);
            if (a == lastA && b == lastB) return;
            lastA = a; lastB = b;
            Set(shadow, a, b, shadowOffset);
            Set(thread, a, b, Vector2.zero);
            endPin.rectTransform.position = b;
        }

        static void Set(UILineGraphic g, Vector3 a, Vector3 b, Vector2 offsetDown)
        {
            var rt = g.rectTransform;
            pts.Clear();
            pts.Add((Vector2)rt.InverseTransformPoint(a) + new Vector2(offsetDown.x, -offsetDown.y));
            pts.Add((Vector2)rt.InverseTransformPoint(b) + new Vector2(offsetDown.x, -offsetDown.y));
            g.SetPoints(pts);
        }
    }
}
