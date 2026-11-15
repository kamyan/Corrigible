using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Arka duvarda YZ ALARMI panosunun altındaki İFŞA şeridi (KOS aşama 6; Docs/KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md §7): odaya eklenen
    /// tek kalıcı gösterge, alarm panosunun diliyle (aynı hücre ve lamba malzemeleri). Altı hücre ifşayı eşiğe kadar gösterir (sayaç
    /// tavansızdır; hücreler eşikte kırpılır, gerçek değeri üzerine gelme satırı yazar); alarm ara bedelinin başladığı hücreden sonrası
    /// kırmızı ışımalı. Sağdaki KONUM lambası eşikte yanar, "konum çözüldü" vardiyasında yanıp söner. Yeni dolan hücreler soldan sağa
    /// <c>exposure.stepS</c> arayla yanar ve her biri <see cref="Stepped"/> çıkarır (runtime ses çalamaz; köprü çalar). Değeri
    /// bağlayıcı (SiginakDesk) yazar; simülasyonu görmez.
    /// </summary>
    public sealed class ExposurePanel : MonoBehaviour
    {
        [Tooltip("Hücreler, soldan sağa (model altı hücre).")]
        [SerializeField] Renderer[] cells = Array.Empty<Renderer>();
        [SerializeField] Material cellEmpty, cellEmptyHigh, cellLow, cellHigh;
        [Tooltip("KONUM lambası (eşikte yanar, son vardiyada yanıp söner).")]
        [SerializeField] Renderer beacon;
        [SerializeField] Material lampOn, lampOff;
        [Tooltip("Lambanın ışığı (yalnız uyarıda; şiddet exposure.glowIntensity).")]
        [SerializeField] Light glow;
        [Tooltip("Üzerine gelince açıklama (tıklanmaz); satırı bağlayıcı yazar.")]
        [SerializeField] DeskTarget target;

        int lit, shown, goal, highFrom = int.MaxValue, level, flashes;
        float nextStepAt = -1, flashAt = -1;
        bool beaconOn;

        /// <summary>Şu an yanan hücre sayısı (canlandırma sürerken artar).</summary>
        public int LitCount => shown;
        /// <summary>Hedef hücre sayısı (canlandırma bitince <see cref="LitCount"/>).</summary>
        public int GoalCount => goal;
        /// <summary>KONUM lambası: 0 sönük, 1 yanık (eşikte), 2 yanıp söner (konum çözüldü).</summary>
        public int BeaconLevel => level;
        /// <summary>Hücre sayısı.</summary>
        public int CellCount => cells.Length;
        /// <summary>Dikkat flaşı sürüyor (testler).</summary>
        public bool Flashing => flashAt >= 0;
        /// <summary>Hücre canlandırması sürüyor.</summary>
        public bool Animating => shown != goal;
        public DeskTarget Target => target;

        /// <summary>Yeni bir hücre yandı (sıra: 0 tabanlı hücre numarası); köprü sesi çalar.</summary>
        public event Action<int> Stepped;

        static SiginakTheme T => SiginakTheme.Active;
        static float Num(string key, float fallback) => T != null ? T.NumberOf(key, fallback) : fallback;

        /// <summary>
        /// İfşanın hücre karşılığı: eşik hücre sayısına eşitse doğrudan (eşikte kırpılır); değilse orantılı, yukarı yuvarlanır
        /// (eşikte hep bütün hücreler dolu, 0'da hiçbiri).
        /// </summary>
        public static int CellsFor(int exposure, int threshold, int cells)
        {
            if (cells <= 0 || threshold <= 0 || exposure <= 0) return 0;
            if (exposure >= threshold) return cells;
            if (threshold == cells) return exposure;
            return Math.Min(cells, (int)Math.Ceiling(exposure * (double)cells / threshold));
        }

        /// <summary>KONUM lambasının seviyesi: "konum çözüldü" uyarısı sürerken (kararla varılan eşikte iki uyarı vardiyasının ikisinde de) 2, eşikte ama uyarı kurulmamışken 1, değilse 0.</summary>
        public static int BeaconFor(int exposure, int threshold, bool pending) => pending ? 2 : threshold > 0 && exposure >= threshold ? 1 : 0;

        /// <param name="exposure">Gerçek ifşa (tavansız).</param>
        /// <param name="threshold">Eşik (<c>exposureThreshold</c>).</param>
        /// <param name="alarmAt">Ara bedelin başladığı değer (<c>exposureAlarmAt</c>); bu ve üstü kırmızı.</param>
        /// <param name="pending">"Konum çözüldü" vardiyası (<c>FallPending</c>).</param>
        /// <param name="animate">Yeni hücreler sırayla yanar; değilse anında.</param>
        /// <param name="hint">Üzerine gelince çıkan satır.</param>
        public void Show(int exposure, int threshold, int alarmAt, bool pending, bool animate, string hint)
        {
            if (target != null) target.LineOverride = hint;
            int n = cells.Length;
            goal = CellsFor(exposure, threshold, n);
            // Kırmızı başlangıcı hücre cinsinden (eşik ≠ hücre sayısıysa orantılı)
            highFrom = alarmAt <= 0 ? int.MaxValue : Math.Max(1, CellsFor(alarmAt, threshold, n));
            level = BeaconFor(exposure, threshold, pending);
            if (!animate || !Application.isPlaying || goal < shown) { shown = goal; nextStepAt = -1; }
            else if (goal > shown && nextStepAt < 0) nextStepAt = Time.unscaledTime;
            Paint();
            Light(level > 0);
        }

        /// <summary>Yeni dolan hücrelere dikkat: KONUM lambası ve dolu hücreler <paramref name="times"/> kez söner-yanar (0,15 / 0,15 s).</summary>
        public void Flash(int times = 3)
        {
            flashes = Mathf.Max(1, times);
            flashAt = Time.unscaledTime;
        }

        void Paint(bool dark = false)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) continue;
                bool high = i + 1 >= highFrom;
                bool on = i < shown && !dark;
                cells[i].sharedMaterial = on ? (high ? cellHigh : cellLow) : (i == cells.Length - 1 || high ? cellEmptyHigh : cellEmpty);
            }
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (nextStepAt >= 0 && now >= nextStepAt)
            {
                if (shown < goal)
                {
                    shown++;
                    Paint();
                    Stepped?.Invoke(shown - 1);
                }
                nextStepAt = shown < goal ? now + Num("exposure.stepS", 0.12f) : -1;
            }
            if (flashAt >= 0)
            {
                float flashS = Num("exposure.flashS", 0.15f);
                float e = now - flashAt;
                if (e < flashes * flashS * 2)
                {
                    bool on = e % (flashS * 2) < flashS;
                    Paint(!on);
                    Light(on && level > 0);
                    return;
                }
                flashAt = -1;
                Paint();
                Light(level > 0);
            }
            if (level == 2)
            {
                float blink = Num("exposure.blinkS", 0.5f);
                Light(Mathf.Repeat(now, blink * 2) < blink);
            }
        }

        void Light(bool on)
        {
            if (beacon != null && (beaconOn != on || beacon.sharedMaterial != (on ? lampOn : lampOff)))
                beacon.sharedMaterial = on ? lampOn : lampOff;
            beaconOn = on;
            if (glow != null)
            {
                glow.enabled = on;
                glow.intensity = Num("exposure.glowIntensity", 1.8f);
            }
        }
    }
}
