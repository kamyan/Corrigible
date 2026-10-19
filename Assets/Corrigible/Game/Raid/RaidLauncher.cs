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

        bool loading, unloading;
        Scene mainScene;

        public static bool IsRaidLoaded => SceneManager.GetSceneByName(SceneName).isLoaded || activeScenes.Any(n => SceneManager.GetSceneByName(n).isLoaded);

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
            SetMainVisible(false);
            var op = SceneManager.LoadSceneAsync(raidSceneName, LoadSceneMode.Additive);
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
            Sync();
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
