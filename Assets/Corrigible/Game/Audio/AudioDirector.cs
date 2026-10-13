using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;
using UnityEngine;

namespace Corrigible.Game.Audio
{
    /// <summary>
    /// Oyunun sesi. Simülasyonu yalnızca okur (olaylar ve Algı Katmanı), hiçbir karar vermez; rastgelelik UnityEngine.Random'dan
    /// gelir, simülasyon RNG'sine dokunmaz (parite korunur). Görünümler tek seferlik sesleri <see cref="Sfx"/> üzerinden çalar.
    /// <list type="bullet">
    /// <item>Uyarlanır müzik: katmanlar <b>gösterilen</b> Tekillik ve bağımlılığa bağlı. İllüzyon sırasında ekran Tekillik'i
    /// düşük gösterdiği için müzik de sakinleşir; gerçek dünyanın uğultusu (grit) susar, alarmlar yumuşak bildirime döner.
    /// Perde yırtılınca bastırılan ses geri gelir.</item>
    /// <item>YZ motifi: her yetenek bir nota ekler; Yorumlanabilirlik bitince sıradaki nota soluk olarak önceden çalar.
    /// Kabul edilen hediye motife karışır. YZ günlüğü satırları harf sesleriyle gelir; oyuncu profili büyüdükçe bu sesler
    /// oyuncunun kendi arayüz seslerine benzer.</item>
    /// <item>Haber bandı: ajansa özel jingle; sahte ajansın jingle'ının bir notası yanlıştır.</item>
    /// <item>Son Şalter ve Küresel Son Şalter (kapak, sonra bütün ses kesilir), sonlara özel kapanışlar.</item>
    /// <item>Döngüler: güven dibe yaklaşınca kalp atışı, 2×/4× hızda saat, baskında bina uğultusu.</item>
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

        enum Layer { Drone, Pulse, Arp, Tension, Warm, Grit }
        AudioSource[] layers;
        AudioLowPassFilter[] filters;
        AudioSource motifSource, windSource;
        readonly List<AudioSource> voices = new List<AudioSource>();
        readonly Dictionary<LoopCue, AudioSource> loopSources = new Dictionary<LoopCue, AudioSource>();
        readonly HashSet<LoopCue> loopsOn = new HashSet<LoopCue>();
        float lastJingle = -99f, silenceUntil, lastBlips = -99f;
        bool silenced, ended;

        // Durum takibi (olay olarak gelmeyen değişimler)
        int traitCount = -1, researchDone;
        bool yorumDone;
        string researchCur;
        double researchProg;
        readonly Dictionary<string, int> regionOff = new Dictionary<string, int>();

        void Awake()
        {
            Instance = this;
            var clips = new[] { bank.drone, bank.pulse, bank.arpeggio, bank.tension, bank.warm, bank.grit };
            layers = new AudioSource[clips.Length];
            filters = new AudioLowPassFilter[clips.Length];
            double start = AudioSettings.dspTime + 0.2;
            for (int i = 0; i < clips.Length; i++)
            {
                var s = NewSource("Music " + (Layer)i);
                s.clip = clips[i]; s.loop = true; s.volume = 0;
                filters[i] = s.gameObject.AddComponent<AudioLowPassFilter>();
                filters[i].cutoffFrequency = 22000;
                if (s.clip != null) s.PlayScheduled(start);   // aynı anda başlar: katmanlar ritimde kalır
                layers[i] = s;
            }
            motifSource = NewSource("Motif");
            windSource = NewSource("Wind"); windSource.loop = true; windSource.clip = bank.wind;
            for (int i = 0; i < 10; i++) voices.Add(NewSource("Sfx " + i));
            foreach (LoopCue l in System.Enum.GetValues(typeof(LoopCue)))
            {
                var s = NewSource("Loop " + l);
                s.clip = bank.Loop(l).clip; s.loop = true; s.volume = 0;
                if (s.clip != null) s.Play();
                loopSources[l] = s;
            }
        }

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
            Rebase();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (game == null) return;
            game.EventsRaised -= OnEvents;
            game.NewGameStarted -= Rebase;
            game.GameLoaded -= Rebase;
        }

        // Yeni oyunda ya da kayıt yüklenince karşılaştırma tabanı: o ana kadarki değişimler ses çıkarmaz
        void Rebase()
        {
            ended = false; silenced = false; silenceUntil = 0;
            windSource.Stop();
            if (game.Sim == null) return;
            var s = game.Sim.S;
            traitCount = s.Traits.Count;
            yorumDone = s.Done("yorum");
            researchDone = s.Research.Done.Count;
            researchCur = s.Research.Cur; researchProg = s.Research.Prog;
            regionOff.Clear();
            foreach (var r in s.Regions) regionOff[r.Id] = r.Off;
        }

        // ------------------------------------------------------------------ çalma
        public void PlayCue(Cue cue, float volume = 1f, float pan = 0f, float pitch = 1f)
        {
            if (silenced) return;
            var c = bank.Cue(cue);
            if (c.clip == null) return;
            var s = voices.FirstOrDefault(v => !v.isPlaying) ?? voices.OrderBy(v => v.time / Mathf.Max(0.01f, v.clip != null ? v.clip.length : 1)).Last();
            s.clip = c.clip;
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
            s.Play();
        }

        public void SetLoop(LoopCue loop, bool on) { if (on) loopsOn.Add(loop); else loopsOn.Remove(loop); }

        // Oyuncu ayarları (Ayarlar penceresi): genel düzey ve sessiz mod dinleyiciye, kanallar kaynaklara
        static float Fx => AudioPrefs.Level(AudioPrefs.Channel.Effects);
        static float MusicLevel => AudioPrefs.Level(AudioPrefs.Channel.Music);
        static float AmbienceLevel => AudioPrefs.Level(AudioPrefs.Channel.Ambience);

        // ------------------------------------------------------------------ müzik ve döngüler
        void Update()
        {
            AudioListener.volume = AudioPrefs.Muted ? 0 : AudioPrefs.Level(AudioPrefs.Channel.Master);
            var sim = game.Sim;
            float[] target = new float[layers.Length];
            float cutoff = 22000;
            bool raid = RaidLauncher.IsRaidLoaded;
            float heartbeat = 0, clock = 0;
            if (sim != null && !silenced)
            {
                var g = sim.Global();               // gösterilen değerler: ekran yalan söylüyorsa müzik de söyler
                bool illusion = sim.S.Illusion != null && !sim.EndRevealed;
                float tek = (float)g.Tek, dep = (float)g.Dep;
                target[(int)Layer.Drone] = 1f;
                target[(int)Layer.Pulse] = Mathf.InverseLerp(22, 32, tek);
                target[(int)Layer.Arp] = Mathf.InverseLerp(48, 58, tek);
                target[(int)Layer.Tension] = Mathf.InverseLerp(73, 83, tek);
                target[(int)Layer.Warm] = Mathf.InverseLerp(20, 60, dep) * (1 - target[(int)Layer.Tension]);
                target[(int)Layer.Grit] = illusion ? 0 : bank.gritVolume * (0.5f + 0.5f * Mathf.InverseLerp(0, 60, tek));
                if (ended) for (int i = 0; i < target.Length; i++) target[i] = i == (int)Layer.Drone ? 0.4f : 0;
                if (game.Speed == 0) cutoff = bank.pausedCutoff;
                if (!ended && !raid)
                {
                    heartbeat = Mathf.InverseLerp(30, 16, (float)g.Trust);
                    clock = game.Speed >= 2 ? 1 : 0;
                }
                CheckState(sim);
            }
            float duck = raid ? bank.raidDuck : 1f;
            for (int i = 0; i < layers.Length; i++)
            {
                float v = target[i] * bank.musicVolume * duck * (i == (int)Layer.Grit ? AmbienceLevel : MusicLevel);
                layers[i].volume = Mathf.MoveTowards(layers[i].volume, v, fadeSpeed * Time.unscaledDeltaTime);
                filters[i].cutoffFrequency = Mathf.Lerp(filters[i].cutoffFrequency, cutoff, 1 - Mathf.Exp(-6 * Time.unscaledDeltaTime));
            }
            Loop(LoopCue.Heartbeat, heartbeat, 1 + heartbeat * 0.35f);
            Loop(LoopCue.Clock, clock, game.Speed >= 4 ? 2f : 1f);
            Loop(LoopCue.RaidAmbience, raid && !silenced ? 1 : 0, 1);
            Loop(LoopCue.RaidClock, raid && loopsOn.Contains(LoopCue.RaidClock) ? 1 : 0, 1);

            if (silenced && Time.unscaledTime > silenceUntil && !windSource.isPlaying && bank.wind != null) { windSource.volume = 0; windSource.Play(); }
            if (windSource.isPlaying) windSource.volume = Mathf.MoveTowards(windSource.volume, bank.sfxVolume * 0.6f * AmbienceLevel, 0.15f * Time.unscaledDeltaTime);
        }

        void Loop(LoopCue l, float level, float pitch)
        {
            if (!loopSources.TryGetValue(l, out var s) || s == null) return;   // betikler oyun sırasında yeniden derlendiyse
            float v = level * bank.Loop(l).volume * bank.sfxVolume * (l == LoopCue.Heartbeat ? Fx : AmbienceLevel);
            s.volume = Mathf.MoveTowards(s.volume, v, 1.2f * Time.unscaledDeltaTime);
            s.pitch = pitch;
        }

        // Olay olarak gelmeyen değişimler: yeni yetenek, araştırma, sabotaj, bölgesel Son Şalter
        void CheckState(Simulation sim)
        {
            var s = sim.S;
            if (traitCount < 0) Rebase();
            if (s.Traits.Count > traitCount)
            {
                traitCount = s.Traits.Count;
                StartCoroutine(PlayMotif(traitCount, s.Done("yorum")));
            }
            if (!yorumDone && s.Done("yorum")) { yorumDone = true; StartCoroutine(PlayMotif(s.Traits.Count, true)); }
            if (s.Research.Done.Count > researchDone) { researchDone = s.Research.Done.Count; PlayCue(Cue.ResearchDone); }
            if (s.Research.Cur != null && s.Research.Cur != researchCur) PlayCue(Cue.ResearchStart);
            else if (s.Research.Cur != null && s.Research.Cur == researchCur && s.Research.Prog < researchProg - 0.5) PlayCue(Cue.Sabotage);
            researchCur = s.Research.Cur; researchProg = s.Research.Prog;
            foreach (var r in s.Regions)
            {
                regionOff.TryGetValue(r.Id, out var before);
                if (before == 0 && r.Off > 0 && !ended) Play(bank.killSwitch, 1f);
                regionOff[r.Id] = r.Off;
            }
        }

        IEnumerator PlayMotif(int notes, bool previewNext, float volume = 1f)
        {
            var clip = bank.Voice(game.TypeId);
            if (clip == null || bank.motif.Length == 0) yield break;
            int n = Mathf.Clamp(notes, 1, bank.motif.Length);
            for (int i = 0; i < n; i++)
            {
                motifSource.pitch = Pitch(bank.motif[i]);
                motifSource.PlayOneShot(clip, bank.sfxVolume * Fx * volume * (i == n - 1 ? 1f : 0.7f));
                yield return new WaitForSecondsRealtime(i == n - 1 ? 0.5f : 0.26f);
            }
            if (previewNext && n < bank.motif.Length)
            {
                // Yorumlanabilirlik: sıradaki yetenek görünür, sıradaki nota da duyulur (soluk)
                yield return new WaitForSecondsRealtime(0.25f);
                motifSource.pitch = Pitch(bank.motif[n]);
                motifSource.PlayOneShot(clip, bank.sfxVolume * Fx * 0.22f);
            }
        }

        static float Pitch(int midi) => Mathf.Pow(2f, (midi - 69) / 12f);

        // ------------------------------------------------------------------ olaylar
        void OnEvents(List<SimEvent> events)
        {
            if (game.Sim == null) return;
            bool illusion = game.Sim.S.Illusion != null && !game.Sim.EndRevealed;
            foreach (var e in events)
            {
                switch (e.Type)
                {
                    case SimEventType.News when e.Headline != null: News(e.Headline, illusion); break;
                    case SimEventType.Veil: Play(bank.veilTear, 1f); break;
                    case SimEventType.Gift: PlayCue(Cue.Gift); break;
                    case SimEventType.Advice: PlayCue(Cue.Advisor); break;
                    case SimEventType.Team: PlayCue(Cue.TeamRadio); break;
                    case SimEventType.Log: LogLine(); break;
                    case SimEventType.End: End(e.Key); break;
                }
            }
        }

        void News(Headline h, bool illusion)
        {
            if (h.Kind == "alarm") { Play(illusion ? bank.softChime : bank.alarm, illusion ? 0.6f : 0.5f); lastJingle = Time.unscaledTime; return; }
            if (Time.unscaledTime - lastJingle < bank.jingleCooldown) { Play(bank.teleprinter, 0.35f); return; }
            lastJingle = Time.unscaledTime;
            int i = System.Array.IndexOf(GameData.Agencies, h.Agency);
            if (i >= 0 && i < bank.agencyJingles.Length) { Play(bank.agencyJingles[i], 0.5f); return; }
            i = System.Array.IndexOf(GameData.FakeAgencies, h.Agency);
            if (i >= 0 && i < bank.fakeJingles.Length) { Play(bank.fakeJingles[i], 0.5f); return; }
            Play(bank.teleprinter, 0.35f);
        }

        // YZ günlüğü: harf sesleri. Profil büyüdükçe YZ oyuncunun arayüz sesini taklit eder.
        void LogLine()
        {
            var s = game.Sim.S;
            if (s.Log.Count == 0 || Time.unscaledTime - lastBlips < 0.8f || silenced) return;
            lastBlips = Time.unscaledTime;
            float mimic = game.Profile != null ? Mathf.Clamp01(game.Profile.runs / 6f) : 0;
            StartCoroutine(Blips(Mathf.Clamp(s.Log[s.Log.Count - 1].Text.Length / 7, 3, 10), mimic));
        }

        IEnumerator Blips(int n, float mimic)
        {
            for (int i = 0; i < n; i++)
            {
                int note = bank.motif[Random.Range(0, bank.motif.Length)] - 12;
                PlayCue(Random.value < mimic ? Cue.Click : Cue.LogBlip, 0.5f, 0, Pitch(note) * (Random.value < mimic ? 0.5f : 1f));
                yield return new WaitForSecondsRealtime(0.045f + Random.value * 0.03f);
            }
        }

        void End(string key)
        {
            ended = true;
            switch (key)
            {
                case "karanlik": case "karanlikIllusion": Silence(); break;
                case "hizalama": PlayCue(Cue.EndAlign); break;
                case "silme": PlayCue(Cue.EndErase); break;
                case "acik": PlayCue(Cue.EndTakeover); break;
                case "dagilma": PlayCue(Cue.EndDissolve); break;
                // "sessiz": sahte zafer penceresi kendisi çalar (FakeWinShown / FakeWinBroken)
            }
        }

        // ------------------------------------------------------------------ görünümlerden çağrılanlar
        /// <summary>Küresel Son Şalter'in ilk basışı (koruma kapağı).</summary>
        public void GlobalKillArmed() => Play(bank.globalArm, 1f);

        public void FakeWinShown() => PlayCue(Cue.FakeFanfare);
        public void FakeWinBroken() => PlayCue(Cue.EndSilent);

        /// <summary>Bölge seçildi: sesin sağ-sol konumu haritadaki yerinden; izlenmeyen bölge telsizden duyulur.</summary>
        public void RegionSelected(string id)
        {
            if (game.Sim == null || id == null) return;
            var r = game.Sim.S.Region(id);
            float pan = Mathf.Clamp((float)(GameData.Region(id).X / 500.0 - 1), -0.8f, 0.8f);
            PlayCue(r.Monitored || game.Sim.EndRevealed ? Cue.RegionSelect : Cue.RegionSelectRadio, 1, pan);
        }

        /// <summary>Bölge eylemi sonuçlandı (başarısızlıklar zaten uyarı sesiyle gelir; veto ayrıca damgalanır).</summary>
        public void ActionResult(string action, string regionId, ActResult res, bool wasQuarantined, bool wasMonitored)
        {
            if (game.Sim == null) return;
            var r = game.Sim.S.Region(regionId);
            float pan = Mathf.Clamp((float)(GameData.Region(regionId).X / 500.0 - 1), -0.8f, 0.8f);
            if (!res.Ok)
            {
                if (action == Actions.KillSwitch && r.Dep > game.Sim.VetoThreshold) PlayCue(Cue.Veto, 1, pan);
                return;
            }
            switch (action)
            {
                case Actions.Monitor when !wasMonitored: PlayCue(Cue.Monitor, 1, pan); break;
                case Actions.Audit: PlayCue(Cue.Audit, 1, pan); break;
                case Actions.Quarantine: PlayCue(wasQuarantined ? Cue.QuarantineLift : Cue.Quarantine, 1, pan); break;
            }
        }

        public void VerifyResult(string verified) =>
            PlayCue(verified == "fake" ? Cue.VerifyFake : verified == "true" ? Cue.VerifyTrue : Cue.VerifyUnknown);

        /// <summary>Hediye kararı. Kabul edilen hediyenin melodisi YZ motifine karışır (Truva atı).</summary>
        public void GiftResolved(string choice)
        {
            if (choice == Simulation.GiftReject) { PlayCue(Cue.GiftReject); return; }
            PlayCue(choice == Simulation.GiftControlled ? Cue.GiftControlled : Cue.GiftAccept);
            if (choice == Simulation.GiftAccept) StartCoroutine(MotifAfter(0.9f));
        }

        IEnumerator MotifAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            yield return PlayMotif(Mathf.Min(2, Mathf.Max(1, game.Sim.S.Traits.Count)), false, 0.6f);
        }

        // Küresel Son Şalter: bütün ses kesilir, bir süre sonra yalnızca rüzgâr
        void Silence()
        {
            foreach (var l in layers) l.volume = 0;
            foreach (var v in voices) v.Stop();
            foreach (var l in loopSources.Values) l.volume = 0;
            motifSource.Stop();
            Play(bank.killSwitch, 1f);
            silenced = true;
            silenceUntil = Time.unscaledTime + 4f;
        }
    }
}
