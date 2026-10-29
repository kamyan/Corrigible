using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;
using UnityEngine;

namespace Corrigible.Game.Audio
{
    /// <summary>
    /// Oyunun sesi. Simülasyonu yalnızca okur, hiçbir karar vermez; rastgelelik UnityEngine.Random'dan gelir, simülasyon
    /// RNG'sine dokunmaz. Görünümler tek seferlik sesleri <see cref="Sfx"/> üzerinden çalar.
    /// <list type="bullet">
    /// <item>Müzik: drone hep çalar; warm katmanı işgal oranıyla girer (dünya ne kadar işgal altındaysa o kadar sahte huzur);
    /// grit dünyanın uğultusu. Hepsi sığınağın duvarlarının ardından (alçak geçiren filtre) duyulur.</item>
    /// <item>Bölge tamamen geri alınınca alarm; harekât bitince kapanış ezgisi.</item>
    /// <item>Baskında müzik kısılır, telsiz hışırtısı ve bina uğultusu döngüsü girer. Saha sahayı görmez, telsizden duyar:
    /// baskın sesleri (Raid*) ve bina uğultusu telsizin dar bandından geçer (<see cref="SoundBank.fieldThroughRadio"/>).</item>
    /// </list>
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        [SerializeField] GameController game;
        [SerializeField] SoundBank bank;
        [Tooltip("Katman sesinin hedefe yaklaşma hızı (birim/saniye).")]
        [SerializeField, Min(0.05f)] float fadeSpeed = 0.35f;

        public SoundBank Bank => bank;

        enum Layer { Drone, Warm, Grit }
        AudioSource[] layers;
        AudioLowPassFilter[] filters;
        readonly List<AudioSource> voices = new List<AudioSource>();
        readonly Dictionary<AudioSource, (AudioHighPassFilter Low, AudioLowPassFilter High)> radioBand = new Dictionary<AudioSource, (AudioHighPassFilter, AudioLowPassFilter)>();
        readonly Dictionary<LoopCue, AudioSource> loopSources = new Dictionary<LoopCue, AudioSource>();
        bool ended;

        void Awake()
        {
            Instance = this;
            var clips = new[] { bank.drone, bank.warm, bank.grit };
            layers = new AudioSource[clips.Length];
            filters = new AudioLowPassFilter[clips.Length];
            double start = AudioSettings.dspTime + 0.2;
            for (int i = 0; i < clips.Length; i++)
            {
                var s = NewSource("Music " + (Layer)i);
                s.clip = clips[i]; s.loop = true; s.volume = 0;
                filters[i] = s.gameObject.AddComponent<AudioLowPassFilter>();
                filters[i].cutoffFrequency = bank.pausedCutoff;
                if (s.clip != null) s.PlayScheduled(start);   // aynı anda başlar: katmanlar ritimde kalır
                layers[i] = s;
            }
            for (int i = 0; i < 10; i++) voices.Add(RadioBand(NewSource("Sfx " + i)));
            foreach (LoopCue l in System.Enum.GetValues(typeof(LoopCue)))
            {
                var s = NewSource("Loop " + l);
                s.clip = bank.Loop(l).clip; s.loop = true; s.volume = 0;
                if (l == LoopCue.RaidAmbience) SetRadioBand(RadioBand(s), bank.fieldThroughRadio);
                if (s.clip != null) s.Play();
                loopSources[l] = s;
            }
        }

        /// <summary>Kaynağa telsiz bandını (alçak ve yüksek kesim) ekler; kapalı başlar.</summary>
        AudioSource RadioBand(AudioSource s)
        {
            var low = s.gameObject.AddComponent<AudioHighPassFilter>();
            var high = s.gameObject.AddComponent<AudioLowPassFilter>();
            low.cutoffFrequency = bank.radioLowCut;
            high.cutoffFrequency = bank.radioHighCut;
            radioBand[s] = (low, high);
            SetRadioBand(s, false);
            return s;
        }

        void SetRadioBand(AudioSource s, bool on)
        {
            if (!radioBand.TryGetValue(s, out var f)) return;
            f.Low.enabled = f.High.enabled = on;
        }

        /// <summary>Sahadan gelen ses: baskın tahtasının olayları (adım, atış, hasar, patlama, uyanma).</summary>
        static bool FromField(Cue cue) => cue >= Cue.RaidStep && cue <= Cue.RaidSpawn;

        AudioSource NewSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.spatialBlend = 0;
            return s;
        }

        void Start()
        {
            game.EventsRaised += OnEvents;
            game.NewGameStarted += Rebase;
            game.GameLoaded += Rebase;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (game == null) return;
            game.EventsRaised -= OnEvents;
            game.NewGameStarted -= Rebase;
            game.GameLoaded -= Rebase;
        }

        void Rebase() => ended = game.Sim?.S.Ending != null;

        // ------------------------------------------------------------------ çalma
        public void PlayCue(Cue cue, float volume = 1f, float pan = 0f, float pitch = 1f)
        {
            var c = bank.Cue(cue);
            if (c.clip == null) return;
            var s = voices.FirstOrDefault(v => !v.isPlaying) ?? voices.OrderBy(v => v.time / Mathf.Max(0.01f, v.clip != null ? v.clip.length : 1)).Last();
            s.clip = c.clip;
            SetRadioBand(s, bank.fieldThroughRadio && RaidLauncher.IsRaidLoaded && FromField(cue));
            s.volume = c.volume * volume * bank.sfxVolume * Fx;
            s.panStereo = pan;
            s.pitch = pitch * (1 + Random.Range(-c.pitchJitter, c.pitchJitter));
            s.Play();
        }

        void Play(AudioClip clip, float volume)
        {
            if (clip == null) return;
            var s = voices.FirstOrDefault(v => !v.isPlaying) ?? voices[0];
            s.clip = clip; s.volume = volume * bank.sfxVolume * Fx; s.panStereo = 0; s.pitch = 1;
            SetRadioBand(s, false);
            s.Play();
        }

        // Oyuncu ayarları (Ayarlar penceresi): genel düzey ve sessiz mod dinleyiciye, kanallar kaynaklara
        static float Fx => AudioPrefs.Level(AudioPrefs.Channel.Effects);
        static float MusicLevel => AudioPrefs.Level(AudioPrefs.Channel.Music);
        static float AmbienceLevel => AudioPrefs.Level(AudioPrefs.Channel.Ambience);

        // ------------------------------------------------------------------ müzik ve döngüler
        void Update()
        {
            AudioListener.volume = AudioPrefs.Muted ? 0 : AudioPrefs.Level(AudioPrefs.Channel.Master);
            var sim = game.Sim;
            var target = new float[layers.Length];
            bool raid = RaidLauncher.IsRaidLoaded;
            if (sim != null)
            {
                target[(int)Layer.Drone] = ended ? 0.4f : 1f;
                target[(int)Layer.Warm] = ended ? 0 : Mathf.InverseLerp(20, 60, (float)sim.Occupation);
                target[(int)Layer.Grit] = ended ? 0 : bank.gritVolume * 0.5f;
            }
            float duck = raid ? bank.raidDuck : 1f;
            for (int i = 0; i < layers.Length; i++)
            {
                float v = target[i] * bank.musicVolume * duck * (i == (int)Layer.Grit ? AmbienceLevel : MusicLevel);
                layers[i].volume = Mathf.MoveTowards(layers[i].volume, v, fadeSpeed * Time.unscaledDeltaTime);
            }
            Loop(LoopCue.RaidAmbience, raid ? 1 : 0);
            Loop(LoopCue.RadioHiss, raid ? 1 : 0);
        }

        void Loop(LoopCue l, float level)
        {
            if (!loopSources.TryGetValue(l, out var s) || s == null) return;   // betikler oyun sırasında yeniden derlendiyse
            float v = level * bank.Loop(l).volume * bank.sfxVolume * AmbienceLevel;
            s.volume = Mathf.MoveTowards(s.volume, v, 1.2f * Time.unscaledDeltaTime);
        }

        // ------------------------------------------------------------------ olaylar
        void OnEvents(List<SimEvent> events)
        {
            foreach (var e in events)
            {
                if (e.Type == SimEventType.RegionLiberated) Play(bank.alarm, 0.5f);
                else if (e.Type == SimEventType.Counterattack) Play(bank.alarm, 0.9f);
                else if (e.Type == SimEventType.End) { ended = true; PlayCue(Cue.EndErase); }
            }
        }
    }
}
