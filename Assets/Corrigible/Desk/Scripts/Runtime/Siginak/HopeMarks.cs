using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Umut işaretleri (K1 notu › öneri; ayrı ve kapatılabilir): özgür bölge sayısı eşiklere ulaştıkça odaya küçük
    /// işaretler girer — 3: filizli teneke, KAYIP ilanına BULUNDU damgası · 6: çocuk çizimi, ikinci kupa · 9: ampul
    /// titremesi seyrekleşir · 12: afişe “YUKARI ÇIKIYORUZ”, havalandırmadan gün ışığı. (1: panodaki ilk isim listeyle gelir.)
    /// </summary>
    public sealed class HopeMarks : MonoBehaviour
    {
        [Serializable] public struct Mark { public int threshold; public GameObject[] objects; }

        [SerializeField] Mark[] marks = Array.Empty<Mark>();
        [SerializeField] BulbFlicker flicker;
        [SerializeField] int calmFlickerFrom = 9;

        public void Configure(Mark[] hopeMarks, BulbFlicker bulb) { marks = hopeMarks; flicker = bulb; }

        /// <summary><paramref name="enabled"/> kapalıysa (ayar) hiçbir işaret görünmez.</summary>
        public void Show(int freeRegions, bool enabled)
        {
            foreach (var m in marks)
                foreach (var go in m.objects)
                    if (go != null) go.SetActive(enabled && freeRegions >= m.threshold);
            if (flicker != null) flicker.Calm = enabled && freeRegions >= calmFlickerFrom;
        }
    }
}
