using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Büyük hedeflerin üzerinde fosforlu sarı çerçeve (K1 notu): #F5D646 5 px + dışta 1,5 px mürekkep. Hedefin dünya
    /// sınırları ekrana izdüşürülür, Overlay'de dikdörtgen olarak çizilir (shader outline yok).
    /// </summary>
    public sealed class HoverHighlight : MonoBehaviour
    {
        [SerializeField] RectTransform frame;
        [SerializeField] Canvas canvas;
        [SerializeField] Camera view;
        [SerializeField] float padding = 6;

        Renderer[] current;
        readonly Vector3[] corners = new Vector3[8];

        public void Configure(RectTransform frameRect, Canvas rootCanvas, Camera camera) { frame = frameRect; canvas = rootCanvas; view = camera; }

        public void Show(Renderer[] targets) => current = targets;
        public void Hide(Renderer[] targets) { if (current == targets) current = null; }

        void LateUpdate()
        {
            bool show = current != null && current.Length > 0 && view != null;
            if (frame.gameObject.activeSelf != show) frame.gameObject.SetActive(show);
            if (!show) return;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (var r in current)
            {
                if (r == null) continue;
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                    corners[i] = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                foreach (var c in corners)
                {
                    var s = view.WorldToScreenPoint(c);
                    if (s.z <= 0) continue;
                    min = Vector2.Min(min, s); max = Vector2.Max(max, s);
                }
            }
            float scale = canvas.scaleFactor;
            frame.anchorMin = frame.anchorMax = frame.pivot = Vector2.zero;
            frame.anchoredPosition = min / scale - Vector2.one * padding;
            frame.sizeDelta = (max - min) / scale + Vector2.one * padding * 2;
        }
    }
}
