using Corrigible.Core;
using Corrigible.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Açılış ve emir belgesinin oyun bağlantısı (Docs/UI_Siginak Aşama 9, K7). Yeni oyunda (<see cref="LiberationDeskView"/>
    /// çağırır) kamera harita pozuna oturur, <see cref="OpeningSequence"/> oynar (siyah → jeneratör → ampul), teleks
    /// “001/00 KYGA-07 HAT AÇIK” basar, belge düşer. BAŞLA (Enter): kayıt varsa ilk basış teleks uyarısıdır, ikinci basış
    /// onaydır (kayıt hiçbir zaman sorulmadan üstüne yazılmaz); ALINDI damgası, belge EVRAK tepsisine, kamera K1'e.
    /// KAYITTAN DEVAM (tık ya da Tab → Enter): kayıt açılır, belge tepsiye, son rapor 0,3 s görünür, kamera kayıttaki poza.
    /// H ve tepsi belgeyi okuma kipinde açar (Enter / Esc kapatır). Esc belge açıkken AYARLAR'ı açar. Boşluk açılışı atlar.
    /// </summary>
    public sealed class SiginakBriefing : MonoBehaviour
    {
        enum State { Idle, Opening, Ready, Reading, Leaving }

        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] TelexFeed telex;
        [SerializeField] EvrakTray tray;
        [SerializeField] SiginakShift shift;
        [SerializeField] BriefingView view;
        [SerializeField] OpeningSequence opening;
        [SerializeField] Camera camera3d;

        State state;
        SavedSummary? save;
        bool armed, keyboardFocus;
        int settingsSeen = -10;

        Simulation Sim => game != null ? game.Sim : null;
        /// <summary>Açılış ya da belge: masa kilitli (LiberationDeskView.InputBlocked).</summary>
        public bool Busy => state != State.Idle;
        public bool OpeningPlaying => opening != null && opening.Playing;
        /// <summary>Belge okunurken AYARLAR plakası da çalışır.</summary>
        public bool AllowsSettings => state == State.Ready || state == State.Reading;

        public void Configure(GameController controller, LiberationDeskView desk, SiginakCameraRig cameraRig, TelexFeed feed, EvrakTray paperTray,
            SiginakShift shiftControls, BriefingView briefingView, OpeningSequence openingSequence, Camera camera)
        {
            game = controller; surface = desk; rig = cameraRig; telex = feed; tray = paperTray; shift = shiftControls; view = briefingView;
            opening = openingSequence; camera3d = camera;
        }

        void OnEnable()
        {
            if (game != null) game.GameLoaded += Loaded;
            DeskLoc.Instance.Changed += Relabel;
            view.ActionClicked += Action;
            view.TabClicked += Continue;
        }

        void OnDisable()
        {
            if (game != null) game.GameLoaded -= Loaded;
            DeskLoc.Instance.Changed -= Relabel;
            view.ActionClicked -= Action;
            view.TabClicked -= Continue;
            Stop();
        }

        static SiginakTheme T => SiginakTheme.Active;

        BriefingData Data() => BriefingBuilder.Build(state == State.Reading, save, armed);

        // ---------- Açılış ----------

        /// <summary>Yeni oyun: harita pozunda karanlıktan açılış, sonra emir belgesi (BAŞLA / KAYITTAN DEVAM).</summary>
        public void OpenOpening()
        {
            opening.Stop();
            view.Hide();
            armed = keyboardFocus = false;
            save = BriefingBuilder.Summarize(game.SavedRecord());
            state = State.Opening;
            tray.SetBriefing(false);
            rig.Locked = true;
            rig.Focus(SiginakPose.Map, true);
            opening.Play(Cue);
        }

        void Cue(OpeningSequence.Cue cue, bool skipped)
        {
            switch (cue)
            {
                case OpeningSequence.Cue.Lights:
                    telex.Post(surface.Shift, Loc.T("KYGA-07 HAT AÇIK"), "", TelexKind.Info, true);
                    break;
                case OpeningSequence.Cue.Document:
                    view.Show(Data(), !skipped);
                    break;
                case OpeningSequence.Cue.End:
                    if (state == State.Opening) state = State.Ready;
                    break;
            }
        }

        /// <summary>Boşluk ya da ATLA: açılış doğrudan belgeye geçer.</summary>
        public void Skip()
        {
            if (opening.Playing) opening.Skip();
        }

        // ---------- Okuma kipi (H, EVRAK) ----------

        /// <summary>Belgeyi okuma kipinde açar (dosya ve sekme yok, “Kapat”). Başka bir kâğıt açıksa false.</summary>
        public bool OpenReading()
        {
            if (state != State.Idle || !tray.HasBriefing) return false;
            state = State.Reading;
            keyboardFocus = false;
            rig.Locked = true;
            tray.SetBriefingOut(true);
            view.Show(Data(), true);
            return true;
        }

        void CloseReading()
        {
            state = State.Leaving;
            view.File(tray.DropPoint, camera3d, () =>
            {
                tray.SetBriefingOut(false);
                rig.Locked = false;
                state = State.Idle;
            });
        }

        // ---------- Tuşlar ----------

        void Update()
        {
            if (SettingsView.Instance != null && SettingsView.Instance.IsOpen) settingsSeen = Time.frameCount;
            var kb = Keyboard.current;
            if (kb == null || state == State.Idle || settingsSeen >= Time.frameCount - 1) return;
            if (state == State.Opening)
            {
                if (kb.spaceKey.wasPressedThisFrame) opening.Skip();
                return;
            }
            if (state == State.Leaving || view.Animating) return;
            bool enter = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
            if (state == State.Reading)
            {
                if (enter || kb.escapeKey.wasPressedThisFrame) CloseReading();
                return;
            }
            if (kb.tabKey.wasPressedThisFrame && save != null)
            {
                keyboardFocus = true;
                Disarm();
                view.SetFocus(1 - view.Focused, true);
            }
            else if (enter)
            {
                if (view.Focused == 1) Continue();
                else Action();
            }
        }

        // Esc belge açıkken AYARLAR'ı açar. LateUpdate'te: DeskInput aynı karede ayarları açık görüp kapatmasın; ayarlar bu ya
        // da önceki karede açıksa (Esc onları kapattı) yeniden açılmaz.
        void LateUpdate()
        {
            var kb = Keyboard.current;
            if (kb == null || state != State.Ready || !kb.escapeKey.wasPressedThisFrame) return;
            if (SettingsView.Instance == null || SettingsView.Instance.IsOpen || settingsSeen >= Time.frameCount - 1) return;
            SettingsView.Instance.Open();
        }

        void Disarm()
        {
            if (!armed) return;
            armed = false;
            view.Bind(Data());
        }

        // ---------- BAŞLA ----------

        void Action()
        {
            if (view.Animating) return;
            if (state == State.Reading) { CloseReading(); return; }
            if (state != State.Ready) return;
            // Kayıt varken ilk basış uyarıdır; kayıt sorulmadan üstüne yazılmaz (ilk komut yeni oyunu kaydeder)
            if (save is SavedSummary s && !armed)
            {
                armed = true;
                var (status, message) = BriefingBuilder.Warning(s);
                telex.Post(surface.Shift, status, message, TelexKind.Alarm);
                view.Bind(Data());
                view.SetFocus(0, keyboardFocus);
                return;
            }
            state = State.Leaving;
            view.StampReceived(() =>
            {
                surface.BeginCampaign();
                rig.Locked = false;
                rig.Focus(SiginakPose.Desk, T.Number("motion.opening.toDeskS", 0.8f));
                view.File(tray.DropPoint, camera3d, () =>
                {
                    tray.SetBriefing(true);
                    state = State.Idle;
                });
            });
        }

        // ---------- KAYITTAN DEVAM ----------

        void Continue()
        {
            if (state != State.Ready || view.Animating || save == null) return;
            state = State.Leaving;
            if (!game.ContinueSaved())
            {
                state = State.Ready;
                telex.Post(surface.Shift, Loc.T("YAPILAMADI"), Loc.T("Kayıt açılamadı."), TelexKind.Failed);
                return;
            }
            // GameLoaded: teleks, EVRAK ve masa kayda döndü, hikâye kapandı. Belge tepsiye, son rapor kısa görünür, kayıttaki poz.
            var pose = System.Enum.TryParse<SiginakPose>(SiginakDeskStore.LoadPose(Sim?.Record), out var p) ? p : SiginakPose.Map;
            rig.Locked = true;
            view.File(tray.DropPoint, camera3d, () =>
            {
                tray.SetBriefing(true);
                if (!shift.FlashLatest(T.Number("motion.opening.reportFlashS", 0.3f), () => Arrive(pose))) Arrive(pose);
            });
        }

        void Arrive(SiginakPose pose)
        {
            state = State.Idle;
            rig.Locked = false;
            rig.Focus(pose, T.Number("motion.poseS", 0.45f));
        }

        // ---------- Durum ----------

        // Kayıttan devam başka yerden (ayarlar) geldiyse açık belge kapanır; kendi devamımız (Leaving) sürer
        void Loaded()
        {
            if (state == State.Leaving) return;
            if (state != State.Idle) Stop();
            tray.SetBriefing(true);
        }

        void Stop()
        {
            if (opening != null) opening.Stop();
            if (view != null) view.Hide();
            if (rig != null) rig.Locked = false;
            state = State.Idle;
        }

        void Relabel()
        {
            if (view.IsOpen) view.Bind(Data());
        }
    }
}
