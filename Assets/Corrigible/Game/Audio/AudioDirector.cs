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
    /// <item>Baskında müzik kısılır, telsiz hışırtısı ve binanın sesi girer: bölgenin ortam yatağı (<see cref="SoundBank.sites"/>,
    /// <see cref="Sfx.RaidSite"/>; seyrek ayrıntılarıyla), bölgenin yatağı yoksa genel bina uğultusu (LoopCue.RaidAmbience).
    /// Saha sahayı görmez, telsizden duyar: baskın sesleri (Raid*), bölge olaylarının sesleri (Ev*) ve binanın sesi telsizin
    /// dar bandından geçer (<see cref="SoundBank.fieldThroughRadio"/>).</item>
    /// <item>Menü sahnesinde (kapının dışı, <c>game</c> boş) yalnız drone ve dünyanın uğultusu çalar; tek seferlik sesler aynı.</item>
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

        // Baskın yeri (Katman 7): iki yatak kaynağı (yeni klip sessiz olanda başlar, çalan söner) ve bir ayrıntı kaynağı
        AudioSource[] beds;
        AudioSource detail;
        RaidSite site;
        /// <summary>Sesi açılan yatak kaynağı; -1: açık baskının yatağı yok (uğultu çalar).</summary>
        int activeBed = -1;
        bool siteDetails, wasRaid;
        float nextDetailAt;
        int lastDetail = -1;
        /// <summary>Son saha sesinin ya da gözcü bildiriminin başladığı an (ayrıntı onları örtmesin).</summary>
        float lastFieldAt = float.NegativeInfinity;
        const float DetailQuiet = 0.4f;

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
            beds = new[] { SiteSource("Bed A"), SiteSource("Bed B") };
            detail = SiteSource("Bed Detail");
            detail.loop = false;
        }

        /// <summary>Baskın yerinin kaynağı: döngü, sessiz başlar, bina uğultusu gibi telsiz bandından.</summary>
        AudioSource SiteSource(string name)
        {
            var s = RadioBand(NewSource(name));
            s.loop = true; s.volume = 0;
            SetRadioBand(s, bank.fieldThroughRadio);
            return s;
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

        /// <summary>Sahadan gelen ses: baskın tahtasının olayları (adım, atış, hasar, patlama, uyanma) ve bölge olayları.</summary>
        static bool FromField(Cue cue) => cue >= Cue.RaidStep && cue <= Cue.RaidSpawn || cue >= Cue.EvCo2Burst && cue <= Cue.EvOilSlide;

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
            if (game == null) return;   // menü sahnesi: oyun yok
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

        void Rebase() => ended = game != null && game.Sim?.S.Ending != null;

        // ------------------------------------------------------------------ çalma
        public void PlayCue(Cue cue, float volume = 1f, float pan = 0f, float pitch = 1f)
        {
            var c = bank.Cue(cue);
            if (c.clip == null) return;
            if (FromField(cue) || cue == Cue.RadioReport) lastFieldAt = Time.unscaledTime;
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
            var sim = game != null ? game.Sim : null;
            var target = new float[layers.Length];
            bool raid = RaidLauncher.IsRaidLoaded;
            if (game == null)
            {
                // Menü sahnesi (kapının dışı): drone duvarın ardından, dünyanın uğultusu merdivenden
                target[(int)Layer.Drone] = 1f;
                target[(int)Layer.Grit] = bank.gritVolume * 0.5f;
            }
            else if (sim != null)
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
            // Baskın kapandı: yer unutulur, yatak ve ayrıntı söner (sonraki baskın yerini yeniden bildirir)
            if (wasRaid && !raid) { activeBed = -1; site = default; }
            wasRaid = raid;
            // Bölgenin yatağı varsa genel uğultunun yerine o çalar; yoksa uğultu yedek olarak kalır
            Loop(LoopCue.RaidAmbience, raid && activeBed < 0 ? 1 : 0);
            Loop(LoopCue.RadioHiss, raid ? 1 : 0);
            Site(raid);
        }

        void Loop(LoopCue l, float level)
        {
            if (!loopSources.TryGetValue(l, out var s) || s == null) return;   // betikler oyun sırasında yeniden derlendiyse
            float v = level * bank.Loop(l).volume * bank.sfxVolume * AmbienceLevel;
            s.volume = Mathf.MoveTowards(s.volume, v, 1.2f * Time.unscaledDeltaTime);
        }

        // ------------------------------------------------------------------ baskın yeri
        /// <summary>
        /// Açılan baskının yeri (<see cref="Sfx.RaidSite"/>). Bölgenin yatağı sessiz (ya da daha sessiz) kaynakta döngünün
        /// rastgele bir yerinden başlar, öbür kaynak söner; aynı yatak zaten çalıyorsa baştan başlamaz. Yatak yoksa uğultu çalar.
        /// </summary>
        public void SetRaidSite(string region, bool details)
        {
            site = bank.Site(region);
            siteDetails = details;
            lastDetail = -1;
            ScheduleDetail();
            activeBed = -1;
            if (site.bed == null || beds == null || beds.Length < 2 || beds[0] == null || beds[1] == null) return;
            for (int i = 0; i < beds.Length; i++)
                if (beds[i].clip == site.bed && beds[i].isPlaying) { activeBed = i; return; }
            int next = beds[0].volume <= beds[1].volume ? 0 : 1;
            var s = beds[next];
            s.clip = site.bed;
            // Klibin verisi henüz yüklenmediyse (ilk baskın) baştan başlar
            if (site.bed.loadState == AudioDataLoadState.Loaded) s.timeSamples = Random.Range(0, site.bed.samples);
            s.Play();
            activeBed = next;
        }

        void ScheduleDetail()
        {
            float lo = Mathf.Max(1f, site.detailGap.x), hi = Mathf.Max(lo, site.detailGap.y);
            nextDetailAt = Time.unscaledTime + Random.Range(lo, hi);
        }

        /// <summary>Yatakların düzeyi ve ayrıntıların zamanlaması (UnityEngine.Random; simülasyon RNG'sine dokunmaz).</summary>
        void Site(bool raid)
        {
            if (beds != null)
                for (int i = 0; i < beds.Length; i++) Fade(beds[i], raid && i == activeBed ? site.bedVolume : 0);
            if (detail == null) return;   // betikler oyun sırasında yeniden derlendiyse
            if (!raid) { Fade(detail, 0); return; }
            if (!siteDetails || site.details == null || site.details.Length == 0 || Time.unscaledTime < nextDetailAt) return;
            ScheduleDetail();
            // Son 0,4 sn'de bir saha sesi ya da gözcünün bildirimi başladıysa bu ayrıntı atlanır (onu örtmesin)
            if (Time.unscaledTime - lastFieldAt < DetailQuiet) return;
            int n = site.details.Length, pick = Random.Range(0, n);
            if (n > 1 && pick == lastDetail) pick = (pick + 1 + Random.Range(0, n - 1)) % n;   // aynısı arka arkaya gelmez
            if (site.details[pick] == null) return;
            lastDetail = pick;
            detail.clip = site.details[pick];
            detail.volume = site.detailVolume * bank.sfxVolume * AmbienceLevel;
            detail.panStereo = Random.Range(-0.5f, 0.5f);
            detail.Play();
        }

        /// <summary>Kaynağı hedef düzeye döngülerle aynı hızda kaydırır; düzeyi sıfır olan kaynak sönünce durur.</summary>
        void Fade(AudioSource s, float level)
        {
            if (s == null) return;   // betikler oyun sırasında yeniden derlendiyse
            float v = level * bank.sfxVolume * AmbienceLevel;
            s.volume = Mathf.MoveTowards(s.volume, v, 1.2f * Time.unscaledDeltaTime);
            if (level <= 0 && s.volume <= 0 && s.isPlaying) s.Stop();
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
