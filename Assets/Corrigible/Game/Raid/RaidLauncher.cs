using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Game.Raid
{
    /// <summary>
    /// DeskGame'de: simülasyon bir baskın başlatınca (ya da kayıtta süren bir baskın varsa) Baskin sahnesini komuta odasının
    /// üstüne yükler ve odanın kamera, canvas ve girdilerini kapatır. Baskın bitip sahne kapanınca hepsini geri açar.
    /// Simülasyon ve oyun durumu hep DeskGame'de kalır; Baskin sahnesi yalnızca taktik savaşı oynatır ve sonucu
    /// GameController'a verir.
    /// </summary>
    public sealed class RaidLauncher : MonoBehaviour
    {
        public const string SceneName = "Baskin";

        [SerializeField] GameController game;
        [SerializeField] string raidSceneName = SceneName;
        public string RaidSceneName => raidSceneName;
        static readonly HashSet<string> activeScenes = new HashSet<string>();
        readonly Dictionary<Behaviour, bool> visibility = new Dictionary<Behaviour, bool>();
        [Tooltip("Baskın sürerken kapatılan bileşenler (ana kamera, dinleyicisi ve fizik ışını, odanın canvas'ları ve girdileri). " +
                 "Nesneler değil bileşenler kapatılır ki görünümlerin olay abonelikleri sürsün.")]
        [SerializeField] Behaviour[] hideDuringRaid = new Behaviour[0];

        [Header("Operasyon monitörü")]
        [SerializeField] bool enableMonitorTransition;
        [SerializeField] Camera mainCamera;
        [SerializeField] Transform monitorFocusPose;
        [SerializeField, Min(0)] float transitionSeconds = .8f;
        [SerializeField, Range(5, 120)] float monitorFieldOfView = 35;
        [Tooltip("Monitöre yaklaşırken ve baskın boyunca kilitlenen kamera denetimi ve giriş bileşenleri. Görünen canvas'lar açık kalır.")]
        [SerializeField] Behaviour[] lockDuringTransition = new Behaviour[0];

        readonly Dictionary<Behaviour, bool> transitionLocks = new Dictionary<Behaviour, bool>();
        Vector3 cameraPosition;
        Quaternion cameraRotation;
        float cameraFieldOfView;
        bool cameraPoseSaved;
        public bool IsTransitioning { get; private set; }
        public Transform MonitorFocusPose => monitorFocusPose;
        public float MonitorFieldOfView => monitorFieldOfView;

        /// <summary>
        /// Monitöre yaklaşmanın hedefini değiştirir (sığınak sunumu kendi CRT'sini kullanır; eski görünüme dönünce geri yazar).
        /// Yalnız ekleme API'si: yükleme ve geri dönüş akışı aynıdır.
        /// </summary>
        public void SetMonitorTransition(Transform pose, float fieldOfView)
        {
            monitorFocusPose = pose;
            monitorFieldOfView = fieldOfView;
        }

        bool loading, unloading;
        Scene mainScene;

        public static bool IsRaidLoaded => SceneManager.GetSceneByName(SceneName).isLoaded || activeScenes.Any(n => SceneManager.GetSceneByName(n).isLoaded);

        /// <summary>Bu taktik sahnenin yükleme/kapatma yaşam döngüsünü ana masadaki başlatıcı yönetiyor mu?</summary>
        public static bool ManagesScene(string sceneName) => activeScenes.Contains(sceneName);

        void Start()
        {
            mainScene = gameObject.scene;
            activeScenes.Add(raidSceneName);
            game.EventsRaised += OnEvents;
            game.Changed += Sync;
            game.GameLoaded += Sync;
            game.NewGameStarted += Sync;
            SceneManager.sceneUnloaded += OnUnloaded;
            Sync();
        }

        void OnDestroy()
        {
            RestoreCameraAndLocks();
            SceneManager.sceneUnloaded -= OnUnloaded;
            activeScenes.Remove(raidSceneName);
            if (game == null) return;
            game.EventsRaised -= OnEvents;
            game.Changed -= Sync;
            game.GameLoaded -= Sync;
            game.NewGameStarted -= Sync;
        }

        void OnEvents(List<SimEvent> events) { if (events.Any(e => e.Type == SimEventType.Raid)) Sync(); }

        void Sync()
        {
            bool want = game.Sim != null && game.Sim.S.Raid != null;
            var scene = SceneManager.GetSceneByName(raidSceneName);
            if (want && !scene.isLoaded && !loading && !unloading) Load();
            else if (!want && scene.isLoaded && !unloading) { unloading=true; SceneManager.UnloadSceneAsync(scene); }
        }

        void Load()
        {
            loading = true;
            StartCoroutine(LoadRoutine());
        }

        /// <summary>
        /// Emir ile yaklaşma arasındaki bekleme (sığınak görünümü: “EMİR VERİLDİ” damgası iner, 0,3 s sonra kamera CRT'ye).
        /// Bu sürede girdi kilitlidir (<see cref="IsTransitioning"/>). Varsayılan 0: eski davranış.
        /// </summary>
        public float TransitionDelaySeconds { get; set; }

        /// <summary>
        /// Geçiş kancası (sığınak görünümünün CRT dizisi). Varsa yaklaşma süresini verir, sahne yüklenmeden önce dizisi beklenir,
        /// sahne açılınca/kapanınca haber alır. Yoksa (varsayılan) akış eskisi gibidir.
        /// </summary>
        public IRaidTransition Transition { get; set; }

        IEnumerator LoadRoutine()
        {
            if (TransitionDelaySeconds > 0)
            {
                IsTransitioning = true;
                yield return new WaitForSecondsRealtime(TransitionDelaySeconds);
                IsTransitioning = false;
            }
            if (enableMonitorTransition && mainCamera != null && monitorFocusPose != null)
            {
                cameraPosition = mainCamera.transform.position;
                cameraRotation = mainCamera.transform.rotation;
                cameraFieldOfView = mainCamera.fieldOfView;
                cameraPoseSaved = true;
                foreach (var b in lockDuringTransition)
                {
                    if (b == null || b == this || transitionLocks.ContainsKey(b)) continue;
                    transitionLocks.Add(b, b.enabled);
                    b.enabled = false;
                }
                IsTransitioning = true;
                float seconds = Transition != null && Transition.ApproachSeconds > 0 ? Transition.ApproachSeconds : transitionSeconds;
                Transition?.BeginApproach();
                float elapsed = 0;
                while (elapsed < seconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / seconds));
                    mainCamera.transform.SetPositionAndRotation(Vector3.Lerp(cameraPosition, monitorFocusPose.position, t), Quaternion.Slerp(cameraRotation, monitorFocusPose.rotation, t));
                    mainCamera.fieldOfView = Mathf.Lerp(cameraFieldOfView, monitorFieldOfView, t);
                    yield return null;
                }
                mainCamera.transform.SetPositionAndRotation(monitorFocusPose.position, monitorFocusPose.rotation);
                mainCamera.fieldOfView = monitorFieldOfView;
                // Geçiş dizisi (CRT açılışı, senkron); bitene kadar yükleme bekler, giriş kilitli kalır
                if (Transition != null) yield return Transition.Run();
                IsTransitioning = false;
            }
            // Yaklaşma sırasında yeni oyun açılmışsa artık eski baskını yükleme.
            if (game == null || game.Sim == null || game.Sim.S.Raid == null)
            {
                loading = false;
                RestoreCameraAndLocks();
                yield break;
            }
            SetMainVisible(false);
            var op = SceneManager.LoadSceneAsync(raidSceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                loading = false;
                SetMainVisible(true);
                RestoreCameraAndLocks();
                yield break;
            }
            op.completed += _ =>
            {
                loading = false;
                // Işıklandırma ve ortam ayarları etkin sahneden gelir
                var s = SceneManager.GetSceneByName(raidSceneName);
                if (s.isLoaded) SceneManager.SetActiveScene(s);
                Transition?.SceneLoaded();
                Sync();
            };
        }

        void OnUnloaded(Scene s)
        {
            if (s.name != raidSceneName) return;
            unloading=false;
            if (mainScene.isLoaded) SceneManager.SetActiveScene(mainScene);
            SetMainVisible(true);
            RestoreCameraAndLocks();
            if (Transition != null) StartCoroutine(Transition.Return());
            Sync();
        }

        void RestoreCameraAndLocks()
        {
            if (cameraPoseSaved && mainCamera != null)
            {
                mainCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                mainCamera.fieldOfView = cameraFieldOfView;
            }
            cameraPoseSaved = false;
            IsTransitioning = false;
            foreach (var pair in transitionLocks) if (pair.Key != null) pair.Key.enabled = pair.Value;
            transitionLocks.Clear();
        }

        void SetMainVisible(bool on)
        {
            foreach (var b in hideDuringRaid)
            {
                if (b == null) continue;
                if (!on) { if (!visibility.ContainsKey(b)) visibility.Add(b, b.enabled); b.enabled = false; }
                else if (visibility.TryGetValue(b, out bool wasEnabled)) b.enabled = wasEnabled;
            }
            if (on) visibility.Clear();
        }
    }
}
