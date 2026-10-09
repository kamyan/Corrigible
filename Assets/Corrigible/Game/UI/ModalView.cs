using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Pencereler (Pencereler canvas'ı): hangi pencerenin ne zaman açılacağını yönetir. Pencerelerin içeriği kendi
    /// bileşenlerinde (IntroModal, GiftModal, VeilModal, FakeWinModal, EndModal). "Overlay" edit modda kapalıdır;
    /// bir pencereyi düzenlemek için Hierarchy'de Overlay'i ve o pencereyi aç. Pencere açıkken oyun zamanı durur.
    /// </summary>
    public sealed class ModalView : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] CrisisDeskView desk;
        [SerializeField] ToastView toast;
        [SerializeField] UiTheme theme;
        [SerializeField] GameObject overlay;
        [SerializeField] IntroModal intro;
        [SerializeField] GiftModal gift;
        [SerializeField] VeilModal veil;
        [SerializeField] FakeWinModal fakeWin;
        [SerializeField] EndModal end;

        bool endHandled;
        MonoBehaviour[] all;

        public bool IsOpen => overlay != null && overlay.activeSelf;

        Simulation Sim => game.Sim;
        WorldState S => game.Sim.S;

        void Awake()
        {
            all = new MonoBehaviour[] { intro, gift, veil, fakeWin, end };
            intro.TypeChosen += id => game.NewGame(id);
            intro.ExpChanged += (teams, scale) => game.SetExperimental(teams, scale);
            intro.StartClicked += () => { Close(); game.SetSpeed(1); };
            // Eğitim zamanı kendisi başlatır ("Zamanı başlat" adımı)
            intro.TutorialClicked += () => { Close(); game.SetSpeed(0); };
            intro.ContinueClicked += () =>
            {
                if (game.ContinueSaved()) { Close(); toast.Show($"Kayıt yüklendi: {S.Day}. gün. Oyun duraklatıldı.", true); }
                else toast.Show("Kayıt yüklenemedi.");
            };
            gift.Chosen += choice =>
            {
                var res = game.ResolveGift(choice);
                if (!res.Ok) toast.Show(res.Msg); else Close();
            };
            veil.Closed += Close;
            end.AgainClicked += () => { Close(); game.NewGame(game.TypeId); };
            end.WipeClicked += () => { game.ClearProfile(); toast.Show("Hafıza silindi.", true); };
            end.LookClicked += () =>
            {
                Close();
                desk.EnterReplayMode();
                toast.Show("Harita artık gerçek değerleri gösteriyor. Yeni oyun için sağdaki Yeniden oyna düğmesini kullan.", true);
            };
        }

        void Start()
        {
            game.EventsRaised += OnEvents;
            game.NewGameStarted += OnNewGame;
            game.GameLoaded += OnLoaded;
            OnNewGame();
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.EventsRaised -= OnEvents;
            game.NewGameStarted -= OnNewGame;
            game.GameLoaded -= OnLoaded;
        }

        void Open(MonoBehaviour modal)
        {
            foreach (var m in all) m.gameObject.SetActive(m == modal);
            overlay.SetActive(true);
            game.Hold = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)modal.transform);
        }

        void Close()
        {
            fakeWin.Stop();
            overlay.SetActive(false);
            game.Hold = false;
        }

        // ---------- Akış ----------
        void OnNewGame()
        {
            endHandled = false;
            intro.Show(game.TypeId, S.CounterLines, game.SavedGameSummary(), Tutorial.TutorialController.Completed, theme);
            intro.ShowExperimental(game.ExpTeams, game.ExpTimeScale, theme);
            Open(intro);
        }

        void OnLoaded()
        {
            endHandled = false;
            Close();
            if (S.PendingGift != null) ShowGift();
        }

        void OnEvents(List<SimEvent> events)
        {
            bool giftE = false, endE = false, veilE = false;
            foreach (var e in events)
            {
                if (e.Type == SimEventType.Gift) giftE = true;
                else if (e.Type == SimEventType.End) endE = true;
                else if (e.Type == SimEventType.Veil) veilE = true;
            }
            if (endE) OnEnd();
            else if (veilE && !IsOpen) { veil.Show(S); Open(veil); }
            else if (giftE && S.PendingGift != null && !IsOpen) ShowGift();
        }

        void ShowGift()
        {
            gift.Show(Sim);
            Open(gift);
        }

        void OnEnd()
        {
            if (endHandled) return;
            endHandled = true;
            game.SetSpeed(0);
            if (S.Ending == "sessiz")
            {
                Sim.HideEnding = true;   // arkadaki masa son ana kadar illüzyonu göstersin
                desk.Render();
                Open(fakeWin);
                fakeWin.Play(S.Day, ShowEnd);
            }
            else ShowEnd();
        }

        void ShowEnd()
        {
            Sim.HideEnding = false;
            desk.Render();
            end.Show(Sim);
            Open(end);
            end.ShowChart(S);
        }
    }
}
