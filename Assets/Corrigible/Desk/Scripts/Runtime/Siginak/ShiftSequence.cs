using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Vardiya geçişinin zaman çizelgesi (K6 notu › ışık ve ses sırası, 0,00–2,60 s). Işıkları kendisi sürer: 0,05'te tavan
    /// ampulü üç titremeyle %30'a (0,35 s), masa lambası %60'a; 0,15'te şaltere amber ışık (0,2 s) ve oda kararır; 1,40'ta
    /// jeneratör yük alır: ampul ve lamba 0,3 s'de geri gelir, amber söner. Sayaç, teleks, kamera ve rapor adımlarını
    /// bağlayıcıya <see cref="Cue"/> olarak bildirir. <see cref="Skip"/> (Boşluk) kalan adımları sırayla “atlandı” diye
    /// bildirip ışıkları son hâline koyar. Süreler tokenlarda (motion.shift.*, motion.shiftSequenceS, lights.dim.*).
    /// </summary>
    public sealed class ShiftSequence : MonoBehaviour
    {
        /// <summary>Bağlayıcının sürdüğü adımlar, zaman sırasıyla.</summary>
        public enum Cue { Counters, Telex, Generator, Camera, Report, End }

        static readonly (Cue cue, string key, float fallback)[] Timeline =
        {
            (Cue.Counters, "motion.shift.countersAtS", 0.40f),
            (Cue.Telex, "motion.shift.telexAtS", 0.60f),
            (Cue.Generator, "motion.shift.generatorAtS", 1.40f),
            (Cue.Camera, "motion.shift.cameraAtS", 1.60f),
            (Cue.Report, "motion.shift.reportAtS", 2.20f),
        };

        /// <summary>Ortam ışığı çarpanı (oda kararır, K6a); ortam ışığını yazan sığınak bağlayıcısı bununla çarpar.</summary>
        public static float AmbientScale { get; private set; } = 1;

        [SerializeField] BulbFlicker bulb;
        [SerializeField] Light deskLamp;
        [Tooltip("Şalterdeki anlık amber nokta ışık (lights.amberSwitch); dizi dışında kapalı.")]
        [SerializeField] Light amber;

        Action<Cue, bool> handler;
        float startedAt;
        int next;
        bool playing, lampSaved;
        float lampBase, amberBase;

        public bool Playing => playing;
        public float Elapsed => playing ? Time.unscaledTime - startedAt : 0;

        public void Configure(BulbFlicker bulbFlicker, Light lamp, Light amberLight) { bulb = bulbFlicker; deskLamp = lamp; amber = amberLight; }

        void Awake()
        {
            if (amber != null) { amberBase = amber.intensity; amber.enabled = false; }
        }

        /// <summary>Diziyi baştan başlatır; <paramref name="onCue"/> her adımda (adım, atlandı mı) ile çağrılır.</summary>
        public void Play(Action<Cue, bool> onCue)
        {
            if (playing) Finish(true);
            handler = onCue;
            if (deskLamp != null && !lampSaved) { lampBase = deskLamp.intensity; lampSaved = true; }
            startedAt = Time.unscaledTime;
            next = 0;
            playing = true;
            Lights(0);
        }

        /// <summary>Boşluk / ATLA: kalan adımlar sırayla “atlandı” bildirilir, ışıklar son hâline geçer.</summary>
        public void Skip()
        {
            if (playing) Finish(true);
        }

        /// <summary>Yeni oyun / kayıttan devam: bildirmeden durur, ışıklar geri gelir.</summary>
        public void Stop()
        {
            playing = false;
            handler = null;
            Restore();
        }

        void Update()
        {
            if (!playing) return;
            float t = Time.unscaledTime - startedAt;
            Lights(t);
            while (playing && next < Timeline.Length && t >= At(next)) Fire(Timeline[next++].cue, false);
            if (playing && t >= SiginakTheme.Active.Number("motion.shiftSequenceS", 2.6f)) Finish(false);
        }

        void OnDisable()
        {
            if (playing) Stop();
        }

        static float At(int i) => SiginakTheme.Active.Number(Timeline[i].key, Timeline[i].fallback);

        void Finish(bool skipped)
        {
            while (next < Timeline.Length) Fire(Timeline[next++].cue, skipped);
            playing = false;
            Restore();
            var h = handler;
            handler = null;
            h?.Invoke(Cue.End, skipped);
        }

        void Fire(Cue cue, bool skipped) => handler?.Invoke(cue, skipped);

        // ---------- Işık ----------

        void Lights(float t)
        {
            var theme = SiginakTheme.Active;
            float dimAt = theme.Number("motion.shift.bulbDimAtS", 0.05f), dimS = theme.Number("motion.bulbDimS", 0.35f);
            float amberAt = theme.Number("motion.shift.amberAtS", 0.15f), amberS = theme.Number("motion.shift.amberS", 0.2f);
            float backAt = At(2), backS = theme.Number("motion.shift.generatorS", 0.3f);
            float bulbTo = theme.NumberOf("lights.dim.bulbScale", theme.Number("motion.bulbDimTo", 0.3f));
            float lampTo = theme.NumberOf("lights.dim.deskLampScale", 0.6f);
            float ambientTo = theme.NumberOf("lights.ambient.k6aScale", 0.7f);

            // Karartma (0 → 1) ve jeneratörün geri gelişi (1 → 0)
            float down = Smooth((t - dimAt) / dimS), up = Smooth((t - backAt) / backS);
            float dim = down * (1 - up);
            // Üç titreme: karartma süresince her altıda birin ikincisinde ışık yarıya düşer
            float p = (t - dimAt) / dimS;
            float blink = p > 0 && p < 1 && (int)(p * 6) % 2 == 1 ? 0.5f : 1;
            if (bulb != null) bulb.Scale = Mathf.Lerp(1, bulbTo, dim) * blink;
            if (deskLamp != null && lampSaved) deskLamp.intensity = lampBase * Mathf.Lerp(1, lampTo, dim);
            AmbientScale = Mathf.Lerp(1, ambientTo, Smooth((t - amberAt) / amberS) * (1 - up));
            if (amber != null)
            {
                float a = Smooth((t - amberAt) / amberS) * (1 - Smooth((t - backAt) / amberS));
                amber.enabled = a > 0.001f;
                amber.intensity = amberBase * a;
            }
        }

        void Restore()
        {
            if (bulb != null) bulb.Scale = 1;
            if (deskLamp != null && lampSaved) deskLamp.intensity = lampBase;
            if (amber != null) amber.enabled = false;
            AmbientScale = 1;
        }

        static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3 - 2 * x);
        }
    }
}
