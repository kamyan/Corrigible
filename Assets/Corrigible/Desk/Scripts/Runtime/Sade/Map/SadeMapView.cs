using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masadaki kâğıt haritanın bölge dolguları ve harita ↔ dünya dönüşümü. Bütün bölgeler düz kâğıt (kara rengi) çizilir;
    /// işgal, cephe ve özgür işaretlerini <see cref="LiberationMapLayer"/> yapar. Dolgular tıklama alanı olarak etkin kalır.
    /// </summary>
    public sealed class SadeMapView : MonoBehaviour
    {
        [Serializable] public sealed class RegionSlot
        {
            public string Id;
            [Tooltip("Bölge merkezi (harita pikseli): spot ve ipliğin bölge ucu.")]
            public Vector2 NodePoint;
            public RegionMeshGraphic Fill;
        }

        [SerializeField] RegionSlot[] regions = Array.Empty<RegionSlot>();
        [SerializeField] MapCoverFitter cover;

        public RegionSlot[] Regions => regions;
        public float WorldScale => (cover != null ? cover.Scale : 1f) * transform.lossyScale.x;

        public Vector3 RegionWorld(string id)
        {
            foreach (var slot in regions) if (slot.Id == id) return MapToWorld(slot.NodePoint.x, slot.NodePoint.y);
            return transform.position;
        }

        public Vector3 MapToWorld(float x, float y)
        {
            var rt = (RectTransform)transform;
            var p = cover != null ? cover.ToCanvas(new Vector2(x, y)) : new Vector2(x, y);
            var size = cover != null ? cover.ViewportSize : rt.rect.size;
            return rt.TransformPoint(new Vector3(p.x - size.x * 0.5f, size.y * 0.5f - p.y, 0f));
        }

        /// <summary>Bütün bölge dolgularını açar ve <paramref name="land"/> rengine boyar.</summary>
        public void SetLand(Color land)
        {
            foreach (var slot in regions)
            {
                if (slot.Fill == null) continue;
                slot.Fill.gameObject.SetActive(true);
                slot.Fill.SetAppearance(land);
            }
        }
    }
}
