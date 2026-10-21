using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Game.Raid
{
    /// <summary>
    /// Ana sahnede: strateji simülasyonu bir baskın başlatınca (ya da kayıtta süren bir baskın varsa) Baskin sahnesini ana
    /// sahnenin üstüne yükler ve kriz masasını gizler. Baskın bitip sahne kapanınca masayı geri açar. Simülasyon ve oyun
    /// durumu hep ana sahnede kalır; Baskin sahnesi yalnızca taktik savaşı oynatır ve sonucu GameController'a verir.
    /// </summary>
    public sealed class RaidLauncher : MonoBehaviour
    {
        public const string SceneName = "Baskin";

        [SerializeField] GameController game;
        [SerializeField] string raidSceneName = SceneName;
        public string RaidSceneName => raidSceneName;
        static readonly HashSet<string> activeScenes = new HashSet<string>();
        readonly Dictionary<Behaviour, bool> visibility = new Dictionary<Behaviour, bool>();
        [Tooltip("Baskın sürerken kapatılan bileşenler (ana kamera ve dinleyicisi, kriz masası, pencere ve eğitim canvas'ları). " +
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

        IEnumerator LoadRoutine()
        {
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
                float elapsed = 0;
                while (elapsed < transitionSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / transitionSeconds));
                    mainCamera.transform.SetPositionAndRotation(Vector3.Lerp(cameraPosition, monitorFocusPose.position, t), Quaternion.Slerp(cameraRotation, monitorFocusPose.rotation, t));
                    mainCamera.fieldOfView = Mathf.Lerp(cameraFieldOfView, monitorFieldOfView, t);
                    yield return null;
                }
                mainCamera.transform.SetPositionAndRotation(monitorFocusPose.position, monitorFocusPose.rotation);
                mainCamera.fieldOfView = monitorFieldOfView;
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
