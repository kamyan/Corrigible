using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Harita noktasını izler; işaretin boyu ve yerel ofseti haritanın kaplama ölçeğinden bağımsızdır.</summary>
    [ExecuteAlways]
    public sealed class SadeMarkerAnchor : MonoBehaviour
    {
        [SerializeField] MapCoverFitter cover;
        [SerializeField] Vector2 mapPoint;
        [SerializeField] Vector2 offset;

        void OnEnable()
        {
            if (cover != null) cover.Changed += Refresh;
            Refresh();
        }

        void OnDisable() { if (cover != null) cover.Changed -= Refresh; }
        void OnValidate() => DeskUi.Deferred(this, Refresh);

        public Vector2 MapPoint => mapPoint;

        public void SetPoint(Vector2 point, Vector2 markerOffset)
        {
            mapPoint = point;
            offset = markerOffset;
            Refresh();
        }

        public void Refresh()
        {
            if (cover == null) return;
            var p = cover.ToCanvas(mapPoint) + offset;
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            var target = new Vector2(p.x, -p.y);
            if (rt.anchoredPosition != target) rt.anchoredPosition = target;
        }
    }
}
