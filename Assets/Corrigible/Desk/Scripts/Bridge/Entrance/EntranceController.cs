using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Menü sahnesi (Giris; Docs/AYARLAR-DOLABI-VE-MENU.md Katman 6–9): Sığınak 07'nin kapısının önündeki sahanlık. Oyun
    /// karanlıktan açılır, merdiven lambaları yanar. Kayıt özeti arka planda çıkar (kaydı bir kez oynatır) ve puantaj saatinin
    /// rafındaki karta yazılır. Karta tıklamak kayıttan devam, boş kart yeni harekâttır (kayıt varsa iki adım). Seçilen kart
    /// saate basılır, kapının zili çalar, kapı açılır, kamera eşikten geçer, ekran kararır ve DeskGame (yükleme kapı açılırken
    /// başladı) niyetle (<see cref="GameLaunch"/>) açılır. Ayarlar dolabı odadakiyle aynıdır. Son şalter (kapaklı bıçaklı şalter)
    /// ışıkları keser ve oyundan çıkar. Esc dolabı kapatır ya da sahanlığa döner; oyundan çıkarmaz.
    /// </summary>
    public sealed class EntranceController : MonoBehaviour
    {
        enum Phase { Intro, Idle, Clock, Entering, Leaving }

        [SerializeField] EntranceCamera cam;
        [Tooltip("Sahanlık (M0), saat (M2) ve içeri yürüyüşün durakları (M3: eşik, içerisi).")]
        [SerializeField] Transform overview, clockPose, threshold, inside;
        [SerializeField] Transform clockPressPose, switchPose;
        [SerializeField] float switchFieldOfView = 40;
        [SerializeField] EntranceInstruments instruments;
        [SerializeField] float overviewFieldOfView = 52, clockFieldOfView = 40;
        [SerializeField] SettingsCabinet cabinet;
        [SerializeField] BridgeSettings settings;
        [SerializeField] TimeClock clock;
        [SerializeField] DoorProp door;
        [Tooltip("Kapının tıklama hedefi (kilitli: saate götürür).")]
        [SerializeField] DeskTarget doorTarget;
        [SerializeField] ShiftSwitch mainSwitch;
        [Tooltip("Sahanlığın lambaları: açılışta yanar, son şalterle söner.")]
        [SerializeField] Light[] lamps = System.Array.Empty<Light>();
        [SerializeField] Renderer[] lampGlass = System.Array.Empty<Renderer>();
        [SerializeField] Material glassOn, glassOff;
        [Tooltip("Tam ekran siyah örtü (Overlay).")]
        [SerializeField] Image fade;

        Phase phase = Phase.Intro;
        float[] lampBase;
        Task<SavedSummary?> summary;
        bool summaryShown;
        SavedSummary? saved;
        AsyncOperation loading;
        public bool Ready => phase==Phase.Idle||phase==Phase.Clock;
        public bool Entering => phase==Phase.Entering;

        static SiginakTheme T => SiginakTheme.Active;

        void Awake()
        {
            GameLaunch.Pending = GameLaunch.Mode.None;
            lampBase = lamps.Select(l => l != null ? l.intensity : 0).ToArray();
            SetLamps(0);
            SetFade(1);
        }

        void OnEnable()
        {
            DeskTarget.Blocked = () => phase == Phase.Intro || phase == Phase.Entering || phase == Phase.Leaving || cabinet.IsOpen;
            cabinet.OpenRequested += OpenCabinet;
            cabinet.Closing += CabinetClosing;
            cabinet.Sound += CabinetSound;
            clock.ClockClicked += Continue;
            clock.ContinueChosen += Continue;
            clock.NewChosen += NewCampaign;
            clock.ArmChanged += Armed;
            clock.Stamped += Stamped;
            if (doorTarget != null) doorTarget.Clicked += ShowClock;
            mainSwitch.Armed += SwitchArmed;
            mainSwitch.Thrown += PowerDown;
            DeskLoc.Instance.Changed += Relabel;
        }

        void OnDisable()
        {
            Cursor.visible = true;
            if (DeskTarget.Blocked != null) DeskTarget.Blocked = null;
            cabinet.OpenRequested -= OpenCabinet;
            cabinet.Closing -= CabinetClosing;
            cabinet.Sound -= CabinetSound;
            clock.ClockClicked -= Continue;
            clock.ContinueChosen -= Continue;
            clock.NewChosen -= NewCampaign;
            clock.ArmChanged -= Armed;
            clock.Stamped -= Stamped;
            if (doorTarget != null) doorTarget.Clicked -= ShowClock;
            mainSwitch.Armed -= SwitchArmed;
            mainSwitch.Thrown -= PowerDown;
            DeskLoc.Instance.Changed -= Relabel;
        }

        void Start()
        {
            cabinet.Init(settings);
            cam.Go(overview, overviewFieldOfView, 0);
            clock.HideCard();
            // Kayıt özeti: Core saf C#, kayıt arka iş parçacığında bir kez oynatılır; kart özet hazır olunca belirir
            var record = SaveStore.Load();
            summary = record == null ? Task.FromResult<SavedSummary?>(null) : Task.Run(() => BriefingBuilder.Summarize(record));
            StartCoroutine(Intro());
        }

        IEnumerator Intro()
        {
            float s = T.NumberOf("motion.entrance.introS", 0.9f);
            for (float t = 0; t < s; t += Time.unscaledDeltaTime)
            {
                float k = t / s;
                SetFade(1 - k);
                // Floresan: iki kısa kırpışmayla yanar
                SetLamps(k < 0.3f ? 0 : k < 0.4f ? 0.6f : k < 0.5f ? 0.1f : k);
                yield return null;
            }
            SetFade(0);
            SetLamps(1);
            phase = Phase.Idle;
        }

        void Update()
        {
            if (!summaryShown && summary != null && summary.IsCompleted)
            {
                summaryShown = true;
                var s = summary.Status == TaskStatus.RanToCompletion ? summary.Result : null;
                // Delikler biten vardiyalar (kartın vardiyası sıradaki)
                saved=s;
                if (s is SavedSummary v) clock.ShowCard(v.Shift-1,v.Free,v.Regions);
                else clock.HideCard();
            }
            if(phase==Phase.Entering||phase==Phase.Leaving||phase==Phase.Intro)return;
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (cabinet.IsOpen) cabinet.Close();
            else if (clock.Armed) clock.Disarm();
            else if (mainSwitch.Cancel()) { }
            else if (phase == Phase.Clock) { phase = Phase.Idle; cam.Go(overview, overviewFieldOfView); }
        }

        void Relabel(){if(saved is SavedSummary v)clock.ShowCard(v.Shift-1,v.Free,v.Regions);}

        // ---------- Dolap ----------

        void OpenCabinet()
        {
            if (phase != Phase.Idle && phase != Phase.Clock) return;
            clock.Disarm();
            cam.Go(cabinet.CameraAnchor, cabinet.CameraFieldOfView);
            cabinet.Open(cam.Remaining);
        }

        void CabinetClosing()
        {
            if (phase == Phase.Clock) cam.Go(clockPose, clockFieldOfView);
            else cam.Go(overview, overviewFieldOfView);
        }

        void CabinetSound(SettingsCabinet.Noise n)
        {
            float pan = Sfx.PanOf(cam.View, cabinet.transform.position);
            switch (n)
            {
                case SettingsCabinet.Noise.Open: Sfx.Play(Cue.CabinetOpen, 1, pan); break;
                case SettingsCabinet.Noise.Close: Sfx.Play(Cue.CabinetClose, 1, pan); break;
                case SettingsCabinet.Noise.Detent: Sfx.Play(Cue.FaderDetent, 1, pan); break;
                case SettingsCabinet.Noise.DetentSoft: Sfx.Play(Cue.FaderDetent, 0.45f, pan, 1.15f); break;
                case SettingsCabinet.Noise.Switch: Sfx.Play(Cue.SwitchClack, 1, pan); break;
                case SettingsCabinet.Noise.Selector: Sfx.Play(Cue.SelectorClunk, 1, pan); break;
            }
        }

        // ---------- Puantaj saati ----------

        void ShowClock()
        {
            if (phase != Phase.Idle) return;
            phase = Phase.Clock;
            cam.Go(clockPose, clockFieldOfView);
        }

        void Armed(bool on) => Sfx.Play(Cue.SwitchClack, on ? 0.6f : 0.4f, Sfx.PanOf(cam.View, clock.transform.position), on ? 1.2f : 1.4f);

        void Continue() { clock.Disarm(); Enter(GameLaunch.Mode.Continue); }
        void NewCampaign() => Enter(GameLaunch.Mode.NewCampaign);

        void Enter(GameLaunch.Mode mode)
        {
            if (phase != Phase.Idle && phase != Phase.Clock) return;
            phase = Phase.Entering;
            Cursor.visible = false;
            GameLaunch.Pending = mode;
            StartCoroutine(EnterRoutine(mode));
        }

        IEnumerator EnterRoutine(GameLaunch.Mode mode)
        {
            if(Vector3.Distance(cam.transform.position,clockPose.position)>.05f)cam.Go(clockPose,clockFieldOfView,.35f);
            while(cam.Moving)yield return null;
            if(clockPressPose)cam.Go(clockPressPose,clockFieldOfView,.18f);
            clock.Punch(mode==GameLaunch.Mode.NewCampaign,()=>StartCoroutine(OpenDoor()));
        }

        void Stamped() => Sfx.Play(Cue.TimeClockPunch, 1, Sfx.PanOf(cam.View, clock.transform.position));

        IEnumerator OpenDoor()
        {
            Sfx.Play(Cue.DoorBuzzer, 1, Sfx.PanOf(cam.View, door.transform.position));
            if(instruments)instruments.Ring();
            float pose=T.NumberOf("motion.entrance.overviewS",.45f);
            cam.Go(overview,overviewFieldOfView,pose);
            yield return new WaitForSecondsRealtime(pose);
            Sfx.Play(Cue.DoorOpen, 1, Sfx.PanOf(cam.View, door.transform.position));
            door.SetOpen(true);
            yield return new WaitForSecondsRealtime(T.NumberOf("motion.doorWheelS",.5f));
            if(Application.CanStreamedLevelBeLoaded(GameLaunch.DeskScene))
            {
                loading=SceneManager.LoadSceneAsync(GameLaunch.DeskScene);
                loading.allowSceneActivation=false;
            }
            while(door.Moving)yield return null;
            float walk = T.NumberOf("motion.entrance.walkS", 1.6f);
            cam.Walk(new[] { threshold, inside }, overviewFieldOfView, walk,true);
            float dark=T.NumberOf("motion.entrance.thresholdFadeS",.3f);
            yield return new WaitForSecondsRealtime(Mathf.Max(0,walk-dark));
            yield return Fade(0,1,dark);
            yield return new WaitForSecondsRealtime(T.NumberOf("motion.entrance.blackHoldS",.2f));
            if (loading != null) loading.allowSceneActivation = true;
        }

        // ---------- Son şalter ----------

        void SwitchArmed()
        {
            Sfx.Play(Cue.SwitchClack,.6f,Sfx.PanOf(cam.View,mainSwitch.transform.position),1.2f);
            if(switchPose)cam.Go(switchPose,switchFieldOfView,.45f);
        }

        void PowerDown()
        {
            if (phase == Phase.Entering || phase == Phase.Leaving) return;
            phase = Phase.Leaving;
            if (cabinet.IsOpen) cabinet.Close();
            clock.Disarm();
            StartCoroutine(PowerDownRoutine());
        }

        IEnumerator PowerDownRoutine()
        {
            Sfx.Play(Cue.PowerDown, 1, Sfx.PanOf(cam.View, mainSwitch.transform.position));
            float s = T.NumberOf("motion.entrance.powerDownS", 0.6f);
            for (float t = 0; t < s; t += Time.unscaledDeltaTime)
            {
                float k = t / s;
                SetLamps((1 - k) * ((int)(k * 7) % 2 == 1 ? 0.2f : 1));
                yield return null;
            }
            SetLamps(0);
            yield return Fade(0, 1, 0.35f);
            yield return new WaitForSecondsRealtime(0.4f);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- Yardımcılar ----------

        IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetFade(Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            SetFade(to);
        }

        void SetFade(float a)
        {
            if (fade == null) return;
            var c = fade.color;
            c.a = a;
            fade.color = c;
            bool show = a > 0.001f;
            if (fade.gameObject.activeSelf != show) fade.gameObject.SetActive(show);
        }

        void SetLamps(float level)
        {
            for (int i = 0; i < lamps.Length; i++)
            {
                if (lamps[i] == null) continue;
                lamps[i].intensity = lampBase[i] * level;
                lamps[i].enabled = level > 0.001f;
            }
            if(instruments){instruments.SetPower(level);return;}
            var m = level > 0.5f ? glassOn : glassOff;
            if (m != null) foreach (var g in lampGlass) if (g != null) g.sharedMaterial = m;
        }
    }
}
