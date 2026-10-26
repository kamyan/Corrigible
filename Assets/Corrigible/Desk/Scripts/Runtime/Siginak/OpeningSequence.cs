using System;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Açılış (K7 notu › açılış akışı): siyah ekran, jeneratör marşı (0,8 s), tavan ampulü üç kırpışmayla yanar (0,6 s);
    /// ışık gelince sayaçlar ve teleks (“001/00 KYGA-07 HAT AÇIK”), ardından emir belgesi düşer. Siyah, HUD canvas'ının en
    /// altındaki tam ekran görüntüdür (ATLA plakası ve teleks şeridi üstünde kalır). Adımları bağlayıcıya <see cref="Cue"/>
    /// olarak bildirir; <see cref="Skip"/> (Boşluk) kalanları “atlandı” diye bildirir ve ışığı son hâline koyar.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        public enum Cue { Lights, Document, End }

        [SerializeField] Image black;
        [SerializeField] BulbFlicker bulb;

        Action<Cue, bool> handler;
        float startedAt;
        int next;
        bool playing;

        public bool Playing => playing;

        public void Configure(Image blackout, BulbFlicker bulbFlicker) { black = blackout; bulb = bulbFlicker; }

        static SiginakTheme T => SiginakTheme.Active;
        static float GeneratorS => T.Number("motion.opening.generatorS", 0.8f);
        static float LightsAt => T.Number("motion.generatorBackS", 1.4f);
        static float DocumentAt => T.Number("motion.opening.documentAtS", 1.5f);
        static float EndAt => DocumentAt + T.Number("motion.briefDropS", 0.35f) + T.Number("motion.briefLiftS", 0.25f);

        float At(int i) => i == 0 ? LightsAt : i == 1 ? DocumentAt : EndAt;

        public void Play(Action<Cue, bool> onCue)
        {
            if (playing) Finish(true);
            handler = onCue;
            startedAt = Time.unscaledTime;
            next = 0;
            playing = true;
            if (black != null) black.gameObject.SetActive(true);
            Lights(0);
        }

        public void Skip()
        {
            if (playing) Finish(true);
        }

        /// <summary>Bildirmeden durur (yeni oyun / kayıttan devam), ışık gelir.</summary>
        public void Stop()
        {
            playing = false;
            handler = null;
            Lights(float.MaxValue);
        }

        void Update()
        {
            if (!playing) return;
            float t = Time.unscaledTime - startedAt;
            Lights(t);
            while (playing && next < 3 && t >= At(next))
            {
                var cue = (Cue)next++;
                if (cue == Cue.End) { Finish(false); return; }
                handler?.Invoke(cue, false);
            }
        }

        void OnDisable()
        {
            if (playing) Stop();
        }

        void Finish(bool skipped)
        {
            while (next < 2) handler?.Invoke((Cue)next++, skipped);
            next = 3;
            playing = false;
            Lights(float.MaxValue);
            var h = handler;
            handler = null;
            h?.Invoke(Cue.End, skipped);
        }

        /// <summary>0 → jeneratör (karanlık) → ampul üç kırpışmayla yanar; siyah örtü ışığın tersi.</summary>
        void Lights(float t)
        {
            float level;
            if (t < GeneratorS) level = 0;
            else
            {
                float p = (t - GeneratorS) / Mathf.Max(0.01f, LightsAt - GeneratorS);
                level = p >= 1 ? 1 : Mathf.Lerp(0.55f, 1, p) * ((int)(p * 6) % 2 == 1 ? 0.15f : 1);
            }
            if (bulb != null) bulb.Scale = level;
            if (black == null) return;
            var c = black.color;
            c.a = 1 - level;
            black.color = c;
            bool show = level < 0.999f;
            if (black.gameObject.activeSelf != show) black.gameObject.SetActive(show);
        }
    }
}
