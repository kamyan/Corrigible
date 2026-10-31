using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masanın arka kenarındaki mekanik sayaç rafı (Karar 1/A): VARDİYA · FON · ERZAK x/kapasite · MALZEME ·
    /// ÖZGÜR BÖLGE x/12 · GÜVEN x/tavan. Üst HUD'un yerini alır; değerleri bağlayıcı (SiginakDesk) yazar.
    /// </summary>
    public sealed class CounterBank : MonoBehaviour
    {
        public enum Slot { Shift, Funds, Supplies, Materials, FreeRegions, Trust }

        [SerializeField] CounterModule[] modules = new CounterModule[6];
        [Tooltip("Görünürlük denetimi için yüz (kenar satırı sayaç rafı kadrajdayken kapanır).")]
        [SerializeField] Renderer face;

        public event Action Ticked;
        public Renderer Face => face;
        public CounterModule this[Slot slot] => modules[(int)slot];

        public void Configure(CounterModule[] slots, Renderer faceRenderer) { modules = slots; face = faceRenderer; }

        void Awake() { foreach (var m in modules) if (m != null) m.Ticked += Tick; }
        void OnDestroy() { foreach (var m in modules) if (m != null) m.Ticked -= Tick; }
        void Tick() => Ticked?.Invoke();

        /// <summary>
        /// Bütün değerler; <paramref name="stagger"/> modüller arası gecikme, <paramref name="shiftRoll"/> VARDİYA çarkının
        /// dönüş süresi (vardiya dizisinde VARDİYA 0,6 s → FON → ERZAK → MALZEME sırayla döner, fişleri dizi iğneler).
        /// </summary>
        public void Show(int shift, double funds, double supplies, double suppliesCapacity, double materials, int freeRegions, int regions,
            int trust, int trustMax, bool animate, float stagger = 0, float shiftRoll = -1, bool slips = true)
        {
            modules[0].Show(shift, null, animate, 0, shiftRoll, slips);
            modules[1].Show(funds, null, animate, stagger, -1, slips);
            modules[2].Show(Math.Floor(supplies), "/" + Math.Round(suppliesCapacity), animate, stagger * 2, -1, slips);
            modules[3].Show(Math.Floor(materials), null, animate, stagger * 3, -1, slips);
            modules[4].Show(freeRegions, "/" + regions, animate, stagger * 4, -1, slips);
            modules[5].Show(trust, "/" + trustMax, animate, stagger * 5, -1, slips);
        }

        /// <summary>Vardiya raporu fişleri: VARDİYA, FON, ERZAK, MALZEME sırasıyla; rapor dosyalanınca <see cref="Unpin"/>.</summary>
        public void Pin(string[] texts, Color[] inks)
        {
            // Boş metin: o sayaca fiş asılmaz (operasyon raporu yalnız FON, ERZAK, MALZEME)
            for (int i = 0; i < texts.Length && i < modules.Length; i++) if (!string.IsNullOrEmpty(texts[i])) modules[i].Pin(texts[i], inks[i]);
        }

        public void Unpin() { foreach (var m in modules) if (m != null) m.Unpin(); }
    }
}
