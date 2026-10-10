using System;
using UnityEngine;

namespace Corrigible.Game.Audio
{
    /// <summary>Oyuncunun ses ayarları (PlayerPrefs'te kalıcı). <see cref="AudioDirector"/> her karede uygular.</summary>
    public static class AudioPrefs
    {
        const string Prefix = "corrigible.audio.";

        public enum Channel { Master, Music, Effects, Ambience }

        static readonly float[] values = new float[4];
        static bool loaded, muted;

        public static event Action Changed;

        public static float Get(Channel c) { Load(); return values[(int)c]; }

        public static void Set(Channel c, float v)
        {
            Load();
            v = Mathf.Clamp01(v);
            if (Mathf.Approximately(values[(int)c], v)) return;
            values[(int)c] = v;
            PlayerPrefs.SetFloat(Prefix + c, v);
            Changed?.Invoke();
        }

        public static bool Muted
        {
            get { Load(); return muted; }
            set { Load(); muted = value; PlayerPrefs.SetInt(Prefix + "muted", value ? 1 : 0); Changed?.Invoke(); }
        }

        /// <summary>Kanalın etkin düzeyi (genel düzey ve sessiz modu ayrıca AudioListener'a uygulanır).</summary>
        public static float Level(Channel c) => Get(c);

        public static void Save() => PlayerPrefs.Save();

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            foreach (Channel c in Enum.GetValues(typeof(Channel)))
                values[(int)c] = PlayerPrefs.GetFloat(Prefix + c, c == Channel.Master ? 0.8f : 1f);
            muted = PlayerPrefs.GetInt(Prefix + "muted", 0) == 1;
        }
    }
}
