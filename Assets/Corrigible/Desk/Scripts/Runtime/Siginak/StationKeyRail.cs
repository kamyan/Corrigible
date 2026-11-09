using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masanın ön kenarındaki pirinç anahtar rayı: SIĞINAK · KAPI ·
    /// MASA; tuşların başlığında tuş adı yerine istasyonun simgesi. Etkin istasyonu bağlayıcı kamera pozundan yazar; tıklama
    /// dizini olay olarak çıkar. Üst kenar şeridinin çipleri aynı sırada.
    /// </summary>
    public sealed class StationKeyRail : MonoBehaviour
    {
        public const int Shelter = 0, Door = 1, Desk = 2;

        [SerializeField] StationKey[] keys = Array.Empty<StationKey>();

        public event Action<int> Pressed;
        public int ActiveIndex { get; private set; } = -1;
        public StationKey this[int index] => keys[index];
        public int Count => keys.Length;

        public void Configure(StationKey[] railKeys) => keys = railKeys;

        void Awake()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                int index = i;
                if (keys[i] != null && keys[i].Target != null) keys[i].Target.Clicked += () => Pressed?.Invoke(index);
            }
        }

        /// <summary>Etkin tuş (−1: hiçbiri) ve kilit (baskın, dizi sırasında bütün tuşlar devre dışı).</summary>
        public void SetActive(int index, bool disabled)
        {
            ActiveIndex = index;
            for (int i = 0; i < keys.Length; i++) if (keys[i] != null) keys[i].SetState(i == index, disabled);
        }
    }
}
