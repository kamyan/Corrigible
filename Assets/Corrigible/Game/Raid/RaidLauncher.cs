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
        [Tooltip("Baskın sürerken kapatılan bileşenler (ana kamera ve dinleyicisi, kriz masası, pencere ve eğitim canvas'ları). " +
                 "Nesneler değil bileşenler kapatılır ki görünümlerin olay abonelikleri sürsün.")]
        [SerializeField] Behaviour[] hideDuringRaid = new Behaviour[0];

        bool loading;
        Scene mainScene;

        public static bool IsRaidLoaded => SceneManager.GetSceneByName(SceneName).isLoaded;

        void Start()
        {
            mainScene = gameObject.scene;
            game.EventsRaised += OnEvents;
            game.GameLoaded += Sync;
            game.NewGameStarted += Sync;
            SceneManager.sceneUnloaded += OnUnloaded;
            Sync();
        }

        void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnUnloaded;
            if (game == null) return;
            game.EventsRaised -= OnEvents;
            game.GameLoaded -= Sync;
            game.NewGameStarted -= Sync;
        }

        void OnEvents(List<SimEvent> events) { if (events.Any(e => e.Type == SimEventType.Raid)) Sync(); }

        void Sync()
        {
            bool want = game.Sim != null && game.Sim.S.Raid != null;
            var scene = SceneManager.GetSceneByName(SceneName);
            if (want && !scene.isLoaded && !loading) Load();
            else if (!want && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
        }

        void Load()
        {
            loading = true;
            SetMainVisible(false);
            var op = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
            op.completed += _ =>
            {
                loading = false;
                // Işıklandırma ve ortam ayarları etkin sahneden gelir
                var s = SceneManager.GetSceneByName(SceneName);
                if (s.isLoaded) SceneManager.SetActiveScene(s);
            };
        }

        void OnUnloaded(Scene s)
        {
            if (s.name != SceneName) return;
            if (mainScene.isLoaded) SceneManager.SetActiveScene(mainScene);
            SetMainVisible(true);
        }

        void SetMainVisible(bool on)
        {
            foreach (var b in hideDuringRaid) if (b != null) b.enabled = on;
        }
    }
}
