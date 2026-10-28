using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Arka duvardaki YZ ALARMI panosu: sayaç rafıyla aynı mekanik sayaç (alarm / eşik) ve kırmızı ışık. Işık eşiğe birkaç
    /// basamak kala yanar, son basamakta yanıp söner; eşiğe varınca YZ karşı saldırır. Değeri bağlayıcı (SiginakDesk) yazar.
    /// </summary>
    public sealed class AlarmPanel : MonoBehaviour
    {
        [SerializeField] CounterModule counter;
        [SerializeField] Renderer beacon;
        [SerializeField] Material lampOn, lampOff;
        [Tooltip("Üzerine gelince açıklama (tıklanmaz); satırı bağlayıcı yazar.")]
        [SerializeField] DeskTarget target;
        [Tooltip("Işık eşiğe bu kadar basamak (ya da daha az) kala yanar.")]
        [SerializeField, Min(1)] int warnSteps = 3;
        [SerializeField, Min(0.1f)] float blinkSeconds = 0.5f;

        int level;
        bool lit;

        public CounterModule Counter => counter;
        /// <summary>0 sönük, 1 yanık, 2 yanıp söner.</summary>
        public int Level => level;

        /// <summary>Işığın hâli: eşiğe kalan basamak (gösterilen, aşağı yuvarlanmış değerle) 1 ise yanıp söner, <paramref name="warn"/> ya da daha azsa yanar.</summary>
        public static int LevelFor(double alarm, double threshold, int warn)
        {
            double left = threshold - Math.Floor(alarm);
            return left <= 1 ? 2 : left <= warn ? 1 : 0;
        }

        /// <param name="hint">Üzerine gelince çıkan satır (gösterilecek metin).</param>
        public void Show(double alarm, double threshold, bool animate, float delay = 0, string hint = null)
        {
            counter.Show(Math.Floor(alarm), "/" + Math.Round(threshold), animate, delay);
            if (target != null) target.LineOverride = hint;
            level = LevelFor(alarm, threshold, warnSteps);
            Light(level > 0);
        }

        void Update()
        {
            if (level == 2) Light(Mathf.Repeat(Time.unscaledTime, blinkSeconds * 2) < blinkSeconds);
        }

        void Light(bool on)
        {
            if (beacon == null || lit == on && beacon.sharedMaterial == (on ? lampOn : lampOff)) return;
            lit = on;
            beacon.sharedMaterial = on ? lampOn : lampOff;
        }
    }
}
