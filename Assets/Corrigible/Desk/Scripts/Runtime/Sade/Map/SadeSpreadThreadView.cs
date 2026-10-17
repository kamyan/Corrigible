using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Gösterilen iki düğüm arasındaki yayılım ipliği; referansın hazır koordinatlarını kopyalamaz.</summary>
    [ExecuteAlways]
    public sealed class SadeSpreadThreadView : MonoBehaviour
    {
        [SerializeField] string from, to;
        [SerializeField] UILineGraphic line, shadow;
        [SerializeField] RectTransform arrow;
        readonly Vector2[] curve = new Vector2[3];
        readonly Vector2[] shadowCurve = new Vector2[3];

        public string From => from;
        public string To => to;

        // Çizgi ve ok katmanları batch için ayrı ebeveynlerde tutulabilir;
        // görünürlük yine bu görünümün yaşam döngüsünü izler.
        void OnEnable() => SetGraphicsActive(true);
        void OnDisable() => SetGraphicsActive(false);

        void SetGraphicsActive(bool active)
        {
            DeskUi.SetActive(line, active);
            DeskUi.SetActive(shadow, active);
            DeskUi.SetActive(arrow, active);
        }

        public static void Curve(Vector2 from, Vector2 to, bool sourceIsCore, out Vector2 a, out Vector2 c, out Vector2 b)
        {
            var dir = (to - from).normalized;
            a = from + dir * (sourceIsCore ? 30 : 24);
            b = to - dir * 30;
            c = (a + b) * .5f + new Vector2(0, 10);
        }

        public void Bind(Vector2 source, Vector2 destination, bool sourceIsCore)
        {
            Curve(source, destination, sourceIsCore, out var a, out var c, out var b);
            curve[0] = new Vector2(a.x, -a.y);
            curve[1] = new Vector2(c.x, -c.y);
            curve[2] = new Vector2(b.x, -b.y);
            if (line != null) line.SetPoints(curve);
            for (int i = 0; i < 3; i++) shadowCurve[i] = curve[i] + new Vector2(1.5f, -2);
            if (shadow != null) shadow.SetPoints(shadowCurve);
            if (arrow == null) return;
            const float t = .58f, u = 1 - t;
            var p = u * u * a + 2 * u * t * c + t * t * b;
            var tangent = 2 * (u * (c - a) + t * (b - c));
            arrow.anchoredPosition = new Vector2(p.x, -p.y);
            arrow.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
        }
    }
}
