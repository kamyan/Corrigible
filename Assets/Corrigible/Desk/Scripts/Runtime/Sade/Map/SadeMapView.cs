using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masadaki kâğıt haritanın bölge dolguları. Bütün bölgeler düz kâğıt (kara rengi) çizilir;
    /// işgal, cephe ve özgür işaretlerini <see cref="LiberationMapLayer"/> yapar. Dolgular tıklama alanı olarak etkin kalır.
    /// </summary>
    public sealed class SadeMapView : MonoBehaviour, IPointerClickHandler
    {
        [Serializable] public sealed class RegionSlot
        {
            public string Id;
            public RegionMeshGraphic Fill;
        }

        [SerializeField] RegionSlot[] regions = Array.Empty<RegionSlot>();

        public RegionSlot[] Regions => regions;
        public event Action BackgroundClicked;

        // SeaHit arka planı bu nesneye aktarır. Bölge dolguları kendi tıklarını işler.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) BackgroundClicked?.Invoke();
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
