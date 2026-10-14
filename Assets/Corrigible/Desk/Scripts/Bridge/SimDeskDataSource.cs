using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using Corrigible.Game.Raid;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Oyun köprüsü (F8): <see cref="GameController"/>'ın durumundan <see cref="DeskViewModel"/> kurar. Oyun ilerledikçe ya da
    /// bir komut sonuçlandıkça kirlenir; karede en çok bir kez yeniden kurar (zaman durunca hiç). Görünüm durumunu
    /// (<see cref="DeskBridgeState"/>: seçim, okundu işaretleri, uyarılar, gösterge geçmişi) tutar.
    /// </summary>
    public sealed class SimDeskDataSource : MonoBehaviour, IDeskDataSource, IDeskPreviewSource
    {
        [SerializeField] GameController game;
        [Tooltip("Tasarım haritası (map.json): başlık ve ızgara etiketleri.")]
        [SerializeField] TextAsset mapSpec;

        readonly SimDeskBuilder builder = new SimDeskBuilder();
        readonly DeskBridgeState state = new DeskBridgeState();
        JObject spec;
        DeskViewModel current;
        bool dirty, subscribed, started;

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
        public SimDeskBuilder Builder => builder;

        void OnEnable()
        {
            if (game == null) return;
            game.Changed += MarkDirty;
            game.EventsRaised += OnEvents;
            game.NewGameStarted += OnNewGame;
            game.GameLoaded += OnLoaded;
            DeskLoc.Instance.Changed += MarkDirty;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            subscribed = true;
            // GameController ilk oyunu kendi Awake'inde kurar (bu abonelikten önce): giriş dosyası açık başlar
            if (!started && game.Sim != null) { started = true; OnNewGame(); }
        }

        void OnDisable()
        {
            if (!subscribed) return;
            game.Changed -= MarkDirty;
            game.EventsRaised -= OnEvents;
            game.NewGameStarted -= OnNewGame;
            game.GameLoaded -= OnLoaded;
            DeskLoc.Instance.Changed -= MarkDirty;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            subscribed = false;
        }

        public void MarkDirty() => dirty = true;

        /// <summary>Taktik harita kapandı: operasyon raporu açılsın.</summary>
        void OnSceneUnloaded(Scene s) { if (s.name == RaidLauncher.SceneName) MarkDirty(); }

        void Update()
        {
            // Masanın kâğıtları (giriş, kampanya, belge, ayarlar) zamanı durdurur; olay kartı ve baskın zaten Blocked
            if (game?.Sim != null) game.Hold = state.WantsHold(game.Sim.S);
        }

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

        DeskViewModel Build()
        {
            if (game?.Sim == null) return new DeskViewModel();
            spec ??= mapSpec != null ? JObject.Parse(mapSpec.text) : new JObject();
            return builder.Build(new DeskBuildInput
            {
                Sim = game.Sim, Speed = game.Speed, DayProgress = game.DayProgress, SecondsPerDay = game.SecondsPerDay(game.Speed),
                Profile = game.Profile, State = state, MapSpec = spec, Info = Info(game.Sim, state), RaidScene = RaidLauncher.IsRaidLoaded,
            });
        }

        /// <summary>
        /// Edit mod önizlemesi: oyunun açılışta kuracağı masa (oyuncunun ayarlarıyla yeni oyun, sabit tohum; giriş kâğıdı
        /// <paramref name="intro"/> ise açık). Play'e basınca gelen yerleşim ve içerikle aynı: harita bölgeleri, pencereler,
        /// listeler. Tercihe, kayda ve profile yazmaz; bu bileşenin görünüm durumuna dokunmaz.
        /// </summary>
        public DeskViewModel BuildPreview(bool intro)
        {
            if (game == null || Application.isPlaying) return null;
            var sim = game.PreviewOpeningGame();
            var st = new DeskBridgeState { IntroOpen = intro, RaidLogSeen = sim.S.RaidLog.Count };
            var json = mapSpec != null ? JObject.Parse(mapSpec.text) : new JObject();
            return new SimDeskBuilder().Build(new DeskBuildInput
            {
                Sim = sim, Speed = 0, DayProgress = 0, SecondsPerDay = game.SecondsPerDay(0),
                Profile = game.Profile, State = st, MapSpec = json, Info = Info(sim, st), RaidScene = false,
            });
        }

        DeskGameInfo Info(Simulation sim, DeskBridgeState st)
        {
            var b = sim.B;
            return new DeskGameInfo
            {
                Campaign = game.Campaign, OnboardingOff = game.OnboardingOff, OnboardingStage = game.OnboardingStage,
                EventsEnabled = game.EventsEnabled, ExpTeams = game.ExpTeams, ExpTimeScale = game.ExpTimeScale,
                TutorialDone = Corrigible.Game.Tutorial.TutorialController.Completed,
                // Kayıt dosyası yalnızca giriş dosyası açıkken okunur
                SavedSummary = st.IntroOpen ? game.SavedGameSummary() : null,
                StartFon = b.startFon, StartSg = b.startSg, Veto = b.vetoBase,
            };
        }

        /// <summary>Yeni oyun (rakip, kural, kampanya krizi, yeniden başla): giriş dosyası açılır, eski baskın raporları okunmuş sayılır.</summary>
        void OnNewGame()
        {
            state.Reset();
            state.IntroOpen = true;
            state.RaidLogSeen = game.Sim.S.RaidLog.Count;
            MarkDirty();
        }

        /// <summary>Kayıt yüklendi: masa duraklatılmış açılır.</summary>
        void OnLoaded()
        {
            state.Reset();
            state.IntroOpen = false;
            state.RaidLogSeen = game.Sim.S.RaidLog.Count;
            MarkDirty();
        }

        void OnEvents(List<SimEvent> events)
        {
            var S = game.Sim.S;
            foreach (var e in events)
            {
                switch (e.Type)
                {
                    case SimEventType.News:
                        if (e.Headline?.Kind == "alarm" && S.Ending == null) state.CriticalNewsId = e.Headline.Id;
                        break;
                    case SimEventType.Team:
                        state.Push(e.Text, S.Day, "ok", builder.TimeNow);
                        state.TeamsBadge = true;
                        break;
                    case SimEventType.Veil:
                        // Perde raporu: masa belgesi (spot + ölçüm karşılaştırması)
                        state.VeilOpen = true;
                        break;
                    case SimEventType.End:
                        // Oyun sonu: zaman durur, açık belgeler kapanır; Sessiz Zafer'de önce sahte duyuru (arkadaki masa
                        // son ana kadar illüzyonu gösterir), bozulunca rapor
                        game.SetSpeed(0);
                        state.Review = false;
                        state.Messages.Clear();
                        state.CriticalNewsId = null;
                        state.VeilOpen = false;
                        state.RaidBrief = null;
                        if (S.Ending == "sessiz")
                        {
                            game.Sim.HideEnding = true;
                            state.FakeWin = true;
                            AudioDirector.Instance?.FakeWinShown();
                        }
                        break;
                }
            }
            MarkDirty();
        }
    }
}
