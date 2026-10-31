using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Arka duvardaki YZ ALARMI panosu: sayaç rafıyla aynı mekanik sayaç (alarm / eşik), altında on adımlık ölçek (Revizyon 2:
    /// dolu hücre 1–7 krem, 8–10 kırmızı; boş 8–10 kesik kırmızı kenar) ve kırmızı ışık. Işık eşiğe birkaç basamak kala yanar,
    /// son basamakta yanıp söner; eşiğe varınca YZ karşı saldırır. Değeri bağlayıcı (SiginakDesk) yazar.
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
        [Tooltip("On adımlık ölçeğin hücreleri, soldan sağa (gösterilen değer aşağı yuvarlanır).")]
        [SerializeField] Renderer[] scale = Array.Empty<Renderer>();
        [SerializeField] Material cellEmpty, cellEmptyHigh, cellLow, cellHigh;
        [Tooltip("Bu hücreden (1'den sayılır) sonrası kırmızı.")]
        [SerializeField, Min(1)] int highFrom = 8;
        [Tooltip("Panonun altındaki pirinç fiş “TELSİZ ODASI ETKİN” (Revizyon 2 · S3 önerisi): telsiz odasında görevli varken görünür.")]
        [SerializeField] GameObject radioPlaque;

        int level, flashes;
        bool lit;
        float flashAt = -1;

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
            int filled = (int)Math.Floor(alarm);
            for (int i = 0; i < scale.Length; i++)
            {
                if (scale[i] == null) continue;
                bool high = i + 1 >= highFrom;
                scale[i].sharedMaterial = i < filled ? (high ? cellHigh : cellLow) : (high ? cellEmptyHigh : cellEmpty);
            }
            level = LevelFor(alarm, threshold, warnSteps);
            Light(level > 0);
        }

        /// <summary>Telsiz odasında görevli var: alarm her vardiya daha yavaş yükselir; fiş görünür.</summary>
        public void SetRadioActive(bool on)
        {
            if (radioPlaque != null && radioPlaque.activeSelf != on) radioPlaque.SetActive(on);
        }

        /// <summary>Karşı saldırı (Revizyon 2 · S5b): rapordan önce ışık <paramref name="times"/> kez yanar (0,15 s yanık, 0,15 s sönük).</summary>
        public void Flash(int times = 3)
        {
            flashes = Mathf.Max(1, times);
            flashAt = Time.unscaledTime;
        }

        void Update()
        {
            if (flashAt >= 0)
            {
                float e = Time.unscaledTime - flashAt;
                if (e < flashes * 0.3f) { Light(e % 0.3f < 0.15f); return; }
                flashAt = -1;
                Light(level > 0);
            }
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
