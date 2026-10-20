using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kart ipliği (PROMPT §A.11): iki raptiye arasında 1,6 px iplik + gölgesi (1,5, 2) α .25. Uçlar dünya noktası olarak
    /// verilir (düğüm ve kartın raptiyesi farklı canvas'larda olabilir); kart açılınca iplik 120 ms'de çizilerek uzar.
    /// Kök, canvas'ı kaplayan bir dikdörtgendir; iplik ve gölge onu kaplar.
    /// </summary>
    public sealed class CardStringView : MonoBehaviour
    {
        [SerializeField] UILineGraphic thread;
        [SerializeField] UILineGraphic shadow;
        [SerializeField] Image pinFrom;
        [SerializeField] Image pinTo;
        [SerializeField] Vector2 shadowOffset = new Vector2(1.5f, 2);
        [SerializeField] float thickness = 1.6f;

        Vector3 fromWorld, toWorld;
        Component fromSource, toSource;
        DeskTween grow;
        bool hasEnds;
        readonly Vector2[] pts = new Vector2[2];

        public bool Visible => gameObject.activeSelf;

        void OnEnable() => Style();
        void OnValidate() => DeskUi.Deferred(this, Style);

        void Style()
        {
            if (thread != null) { DeskUi.Tint(thread, "sade.desk.regionCard.string.color"); thread.Thickness = thickness; }
            if (shadow != null) { DeskUi.Tint(shadow, "sade.desk.regionCard.string.shadow"); shadow.Thickness = thickness; }
            SadeUi.Sprite(pinFrom, "pin_black_14");
            SadeUi.Sprite(pinTo, "pin_black_14");
        }

        /// <summary>Uçları ayarlar; <paramref name="animate"/> ise iplik baştan çizilir (kart açılışı), değilse hemen tam.</summary>
        public void SetEnds(Vector3 from, Vector3 to, bool animate, Component fromCanvasSource = null, Component toCanvasSource = null)
        {
            fromWorld = from; toWorld = to; hasEnds = true;
            fromSource = fromCanvasSource; toSource = toCanvasSource;
            DeskUi.SetActive(this, true);
            if (animate && Application.isPlaying) { grow.Snap(0); var th = SadeTheme.Active; grow.To(1, th != null ? th.Number("motion.cardStringS", 0.12f) : 0.12f); }
            else grow.Snap(1);
            Redraw();
        }

        public void Hide() { hasEnds = false; DeskUi.SetActive(this, false); }

        /// <summary>Kart hareket ederken uçları izler; çizilme ilerlemesini başa sarmaz.</summary>
        public void FollowEnds(Vector3 from, Vector3 to, Component fromCanvasSource = null, Component toCanvasSource = null)
        {
            if (!hasEnds) return;
            bool changed = fromWorld != from || toWorld != to || fromSource != fromCanvasSource || toSource != toCanvasSource;
            fromSource = fromCanvasSource; toSource = toCanvasSource;
            var rt = (RectTransform)transform;
            if (!changed && !DeskCanvasProjection.RequiresProjection(fromSource, rt) && !DeskCanvasProjection.RequiresProjection(toSource, rt)) return;
            fromWorld = from; toWorld = to; Redraw();
        }

        void Update()
        {
            if (hasEnds && grow.Tick(Time.unscaledDeltaTime)) Redraw();
        }

        void Redraw()
        {
            var rt = (RectTransform)transform;
            var projectedFrom = DeskCanvasProjection.WorldPoint(fromSource, fromWorld, rt);
            var projectedTo = DeskCanvasProjection.WorldPoint(toSource, toWorld, rt);
            Vector2 a = rt.InverseTransformPoint(projectedFrom), b = rt.InverseTransformPoint(projectedTo);
            var end = Vector2.Lerp(a, b, grow.Value);
            Set(thread, a, end, Vector2.zero);
            Set(shadow, a, end, new Vector2(shadowOffset.x, -shadowOffset.y));
            if (pinFrom != null) pinFrom.rectTransform.position = projectedFrom;
            DeskUi.SetActive(pinTo, grow.Value >= 1);
            if (pinTo != null) pinTo.rectTransform.position = projectedTo;
        }

        void Set(UILineGraphic g, Vector2 a, Vector2 b, Vector2 offset)
        {
            if (g == null) return;
            pts[0] = a + offset; pts[1] = b + offset;
            g.SetPoints(pts);
        }
    }
}
