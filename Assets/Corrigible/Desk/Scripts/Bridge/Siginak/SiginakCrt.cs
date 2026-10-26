using System.Collections;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Raid;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// CRT'nin oyun bağlantısı (Docs/UI_Siginak Aşama 7): masadaki ekranı (<see cref="CrtScreen"/>) durumla günceller ve
    /// <see cref="RaidLauncher"/>'ın geçiş kancası olarak tam ekran CRT dizisini (<see cref="CrtTransition"/>) oynatır.
    /// Sunum anahtarı yeni görünümde kancayı başlatıcıya yazar; eski görünümde kanca yoktur. Kanca çağrıları başlatıcının
    /// coroutine'inde koşar (bu nesne baskın sırasında gizli oda kökünde olsa da).
    /// </summary>
    public sealed class SiginakCrt : MonoBehaviour, IRaidTransition
    {
        [SerializeField] GameController game;
        [SerializeField] CrtScreen screen;
        [SerializeField] CrtTransition overlay;

        CrtState shown = (CrtState)(-1);
        Simulation Sim => game != null ? game.Sim : null;

        public void Configure(GameController controller, CrtScreen crtScreen, CrtTransition transition)
        {
            game = controller; screen = crtScreen; overlay = transition;
        }

        void OnEnable()
        {
            if (game != null) { game.Changed += Refresh; game.GameLoaded += Refresh; game.NewGameStarted += Refresh; }
            DeskLoc.Instance.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (game != null) { game.Changed -= Refresh; game.GameLoaded -= Refresh; game.NewGameStarted -= Refresh; }
            DeskLoc.Instance.Changed -= Refresh;
        }

        // Durum yükleme anında da değişir (sahne açıldı / kapandı): her kare yalnız durumu karşılaştır
        void Update()
        {
            if (CrtViewBuilder.State(Sim, RaidLauncher.IsRaidLoaded) != shown) Refresh();
        }

        void Refresh()
        {
            var sim = Sim;
            if (sim?.LiberationOn != true) return;
            var d = CrtViewBuilder.Screen(sim, RaidLauncher.IsRaidLoaded);
            shown = d.State;
            screen.Bind(d);
        }

        // ---------- IRaidTransition (K5b) ----------

        public float ApproachSeconds => SiginakTheme.Active.NumberOf("motion.crtTurnS", 0.5f);
        public void BeginApproach() => overlay.BeginApproach(ApproachSeconds);
        public IEnumerator Run() => overlay.Run(CrtViewBuilder.Transition(Sim));
        public void SceneLoaded() => overlay.SceneLoaded();
        public IEnumerator Return() => overlay.Return(CrtViewBuilder.Transition(Sim));
    }
}
