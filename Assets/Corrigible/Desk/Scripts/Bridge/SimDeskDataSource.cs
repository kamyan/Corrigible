using System;
using Corrigible.Game;
using Corrigible.Game.Raid;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Oyun köprüsü: <see cref="GameController"/>'ın durumundan masanın ekran kâğıtlarını (<see cref="DeskViewModel"/>) kurar.
    /// Bir komut sonuçlanınca, vardiya bitince ya da taktik harita kapanınca kirlenir; karede en çok bir kez yeniden kurar.
    /// Okunmuş baskın raporlarını (<see cref="DeskBridgeState"/>) tutar.
    /// </summary>
    public sealed class SimDeskDataSource : MonoBehaviour, IDeskDataSource
    {
        [SerializeField] GameController game;

        readonly SimDeskBuilder builder = new SimDeskBuilder();
        readonly DeskBridgeState state = new DeskBridgeState();
        DeskViewModel current;
        bool dirty, subscribed;

        /// <summary>Kirliyse hemen yeniden kurar (aynı karede dil değişimi ve denetim için).</summary>
        public DeskViewModel Current
        {
            get
            {
                if (current == null || dirty) { current = Build(); dirty = false; }
                return current;
            }
        }
        public event Action<DeskViewModel> Changed;

        public GameController Game => game;
        public DeskBridgeState State => state;

        void OnEnable()
        {
            if (game == null) return;
            game.Changed += MarkDirty;
            game.NewGameStarted += ResetState;
            game.GameLoaded += ResetState;
            DeskLoc.Instance.Changed += MarkDirty;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            subscribed = true;
            // GameController ilk oyunu kendi Awake'inde kurar (bu abonelikten önce)
            if (game.Sim != null) ResetState();
        }

        void OnDisable()
        {
            if (!subscribed) return;
            game.Changed -= MarkDirty;
            game.NewGameStarted -= ResetState;
            game.GameLoaded -= ResetState;
            DeskLoc.Instance.Changed -= MarkDirty;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            subscribed = false;
        }

        public void MarkDirty() => dirty = true;

        /// <summary>Taktik harita kapandı: baskın raporu açılsın.</summary>
        void OnSceneUnloaded(Scene s) => MarkDirty();

        void LateUpdate()
        {
            if (!dirty || game?.Sim == null) return;
            dirty = false;
            current = Build();
            Changed?.Invoke(current);
        }

        /// <summary>Hemen yeniden kur ve yayınla (testler ve komutlar sonrası aynı karede görmek için).</summary>
        public void Flush()
        {
            dirty = false;
            current = Build();
            Changed?.Invoke(current);
        }

        DeskViewModel Build() => game?.Sim == null ? new DeskViewModel() : builder.Build(game.Sim, state);

        /// <summary>Yeni oyun ya da kayıttan devam: eski baskın raporları okunmuş sayılır.</summary>
        void ResetState()
        {
            state.RaidLogSeen = game.Sim.S.RaidLog.Count;
            MarkDirty();
        }
    }
}
