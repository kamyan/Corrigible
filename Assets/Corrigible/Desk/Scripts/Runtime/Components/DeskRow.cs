using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Yatay dizici (CSS <c>display:flex; gap</c> karşılığı). LayoutGroup değildir: her karede çalışmaz, yalnızca
    /// <see cref="Layout"/> çağrılınca (veri değişince) çocukları sırayla yerleştirir. TMP çocukların genişliği
    /// metnin tercih edilen genişliğidir; diğerleri kendi genişliğini korur. Dikeyde ortaya hizalar.
    /// <c>LayoutElement.ignoreLayout</c> işaretli çocuklar (kâğıt katmanları, çizgi) dizilmez.
    /// </summary>
    [ExecuteAlways]
    public sealed class DeskRow : MonoBehaviour
    {
        [SerializeField] float gap = 8;
        [SerializeField] float padLeft;
        [SerializeField] float padRight;
        [Tooltip("Çocukları sağa yasla (sağdan sola dizer).")]
        [SerializeField] bool alignRight;
        [Tooltip("Kendi genişliğini içeriğe göre ayarla.")]
        [SerializeField] bool fitWidth;
        [Tooltip("Son çocuk kalan genişliği doldurur (ör. kesilen mesaj).")]
        [SerializeField] bool lastFills;

        public float Width { get; private set; }
        public float Gap { get => gap; set => gap = value; }

        public void Layout()
        {
            var rt = (RectTransform)transform;
            int n = rt.childCount;
            float x = padLeft;
            float total = rt.rect.width;
            RectTransform last = null;
            for (int i = 0; i < n; i++) if (Counts(rt.GetChild(i))) last = (RectTransform)rt.GetChild(i);

            if (!alignRight)
            {
                for (int i = 0; i < n; i++)
                {
                    var c = (RectTransform)rt.GetChild(i);
                    if (!Counts(c)) continue;
                    float w = Measure(c);
                    if (lastFills && c == last) w = Mathf.Max(0, total - padRight - x);
                    Set(c, x, w, 0);
                    x += w + gap;
                }
                Width = (last != null ? x - gap : x) + padRight;
            }
            else
            {
                x = padRight;
                for (int i = n - 1; i >= 0; i--)
                {
                    var c = (RectTransform)rt.GetChild(i);
                    if (!Counts(c)) continue;
                    float w = Measure(c);
                    Set(c, x, w, 1);
                    x += w + gap;
                }
                Width = (last != null ? x - gap : x) + padLeft;
            }
            if (fitWidth && !Mathf.Approximately(rt.sizeDelta.x, Width)) rt.sizeDelta = new Vector2(Width, rt.sizeDelta.y);
        }

        static bool Counts(Transform c) =>
            c.gameObject.activeSelf && !(c.TryGetComponent<LayoutElement>(out var le) && le.ignoreLayout);

        static float Measure(RectTransform c)
        {
            if (c.TryGetComponent<TMP_Text>(out var t))
            {
                return Mathf.Round(DeskUi.TextWidth(t, t.text));
            }
            return c.sizeDelta.x;
        }

        /// <param name="side">0 = soldan, 1 = sağdan.</param>
        static void Set(RectTransform c, float x, float w, int side)
        {
            float rot = c.localEulerAngles.z;
            if (Mathf.Abs(Mathf.DeltaAngle(rot, 0)) > 0.01f)
            {
                // Döndürülmüş öğe (damga): merkezden yerleşir
                // Önceki çapa ortada değilse dikey kaydırma geçersizdir
                float y = Mathf.Approximately(c.anchorMin.y, 0.5f) && Mathf.Approximately(c.anchorMax.y, 0.5f) ? c.anchoredPosition.y : 0;
                c.anchorMin = c.anchorMax = new Vector2(side, 0.5f);
                c.pivot = new Vector2(0.5f, 0.5f);
                c.anchoredPosition = new Vector2(side == 0 ? x + w / 2 : -(x + w / 2), y);
                return;
            }
            c.anchorMin = c.anchorMax = new Vector2(side, 0.5f);
            c.pivot = new Vector2(side, 0.5f);
            c.anchoredPosition = new Vector2(side == 0 ? x : -x, c.anchoredPosition.y);
            if (c.GetComponent<TMP_Text>() != null) c.sizeDelta = new Vector2(w, c.sizeDelta.y);
        }
    }
}
