using System;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using Corrigible.Game.Raid;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Son şalterin oda bağlantısı (KOS aşama 6; Docs/KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md §4, §7). Arka duvardaki kapaklı şalter
    /// (<see cref="ShiftSwitch"/>, R19 kipi, <c>lastSwitch</c> açıları) iki adımla çekilir: 1. tık kapağı kaldırır (kamera KLS pozuna
    /// geçer, teleks çeşidi söyler), 2. tık kolu indirir; kilit yeniden sınanır ve ÇEKİLME EMRİ / TAHLİYE EMRİ kâğıdı masaya düşer.
    /// Onay kâğıttaki DAMGALA düğmesine fare tıkıdır (Enter bağlı değil: geri alınmaz karar); damga inince komut
    /// <c>GameController.ExecuteBoardCommand(s =&gt; s.PullLastSwitch())</c> ile gider, başarılıysa ışıklar söner ve son raporu açılır.
    /// Damgadan önce Esc ya da sürüklemesiz sağ tık vazgeçer: kâğıt kalkar, kol yukarı, kapak kapanır, komut yazılmaz.
    /// <para>
    /// Kilit (<see cref="LastSwitchViewBuilder.Lock"/>): harekât bitti, baskın, rehber sürüyor, belge açık, masa meşgul, Core'un gerekçesi
    /// (5. vardiyadan önce). Kilitliyken KİLİTLİ bandı görünür, lamba söner, tık yalnız titretir. Plaka ve üzerine gelme satırı
    /// çekilirse gelecek çeşidi (<see cref="Simulation.LastSwitchReasons"/>) önceden söyler. Simülasyonu yalnız okur; tek komutu
    /// <c>last_switch</c>. Kâğıdın ve kolun durumu kayda girmez (kayıttan devamda kol yukarıda, kâğıt kapalı).
    /// </para>
    /// </summary>
    public sealed class SiginakLastSwitch : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] ShiftSwitch lastSwitch;
        [SerializeField] LastSwitchPlate plate;
        [Tooltip("ÇEKİLME / TAHLİYE EMRİ: emir belgesinin dosyasız kopyası (SiginakHud/Withdrawal order).")]
        [SerializeField] BriefingView order;
        [SerializeField] TelexFeed telex;
        [Header("Kilit kaynakları")]
        [SerializeField] SiginakTutorial tutorial;
        [SerializeField] SiginakShiftClose shiftClose;
        [SerializeField] SiginakShelterEvent shelterEvent;
        [SerializeField] SiginakAdmission admission;
        [SerializeField] SiginakBriefing briefing;

        enum Step { Idle, Pending, Open, Stamping }
        Step step;
        float orderAt, deniedUntil;
        string lastDenied;

        Simulation Sim => game != null ? game.Sim : null;

        /// <summary>Kol inik (emir bekleniyor), kâğıt açık ya da damga basılıyor: masa kilitli (LiberationDeskView.InputBlocked).</summary>
        public bool Busy => step != Step.Idle;
        /// <summary>Emir kâğıdı açık (damgadan önce).</summary>
        public bool OrderOpen => step == Step.Open || step == Step.Stamping;
        public ShiftSwitch Switch => lastSwitch;
        public LastSwitchPlate Plate => plate;
        public BriefingView Order => order;
        /// <summary>Son kilidin kısa nedeni (yoksa null; testler).</summary>
        public string LockPlate { get; private set; }
        /// <summary>Teleks satırı basıldı (durum, ileti; testler).</summary>
        public event Action<string, string> TelexLine;
        /// <summary>Komut sonucu (testler).</summary>
        public event Action<ActResult> Pulled;

        static float DeniedRepeatS => SiginakTheme.Active != null ? SiginakTheme.Active.NumberOf("motion.lastSwitch.deniedRepeatS", 3f) : 3f;
        static float OrderDelayS => SiginakTheme.Active != null ? SiginakTheme.Active.NumberOf("motion.lastSwitch.orderDelayS", 0.4f) : 0.4f;

        void OnEnable()
        {
            if (lastSwitch != null)
            {
                lastSwitch.Armed += Armed;
                lastSwitch.Thrown += Thrown;
                lastSwitch.Cancelled += CoverClosed;
                lastSwitch.Denied += Denied;
                lastSwitch.Sound += Play;
            }
            if (order != null) order.ActionClicked += StampClicked;
            if (rig != null) rig.BackClicked += Back;
            if (game != null) { game.NewGameStarted += Reset; game.GameLoaded += Reset; }
            DeskLoc.Instance.Changed += Relabel;
        }

        void OnDisable()
        {
            if (lastSwitch != null)
            {
                lastSwitch.Armed -= Armed;
                lastSwitch.Thrown -= Thrown;
                lastSwitch.Cancelled -= CoverClosed;
                lastSwitch.Denied -= Denied;
                lastSwitch.Sound -= Play;
            }
            if (order != null) order.ActionClicked -= StampClicked;
            if (rig != null) rig.BackClicked -= Back;
            if (game != null) { game.NewGameStarted -= Reset; game.GameLoaded -= Reset; }
            DeskLoc.Instance.Changed -= Relabel;
        }

        /// <summary>Kilidin anlık girdileri (sunumun durumu).</summary>
        LastSwitchViewBuilder.LockInputs Inputs() => new LastSwitchViewBuilder.LockInputs
        {
            RaidLoaded = RaidLauncher.IsRaidLoaded,
            Tutorial = tutorial != null && tutorial.isActiveAndEnabled && tutorial.Active,
            Story = game != null && game.StoryHold || briefing != null && briefing.Busy,
            DeskBusy = shiftClose != null && shiftClose.Busy || shelterEvent != null && shelterEvent.isActiveAndEnabled && shelterEvent.Busy ||
                admission != null && admission.isActiveAndEnabled && admission.Busy ||
                FinalReportView.Instance != null && FinalReportView.Instance.IsOpen ||
                SettingsCabinet.Instance != null && SettingsCabinet.Instance.IsOpen,
        };

        void Update()
        {
            var sim = Sim;
            if (lastSwitch == null) return;
            if (sim?.Board == null)
            {
                // Eski oda yolunda son şalter yok
                lastSwitch.SetDisabled(true, Loc.T("Bu kayıtta son şalter yok."), Loc.T("mühürlü"));
                ShowPlate(sim);
                return;
            }
            if (step == Step.Idle)
            {
                var (lockPlate, lockLine) = LastSwitchViewBuilder.Lock(sim, Inputs());
                LockPlate = lockPlate;
                lastSwitch.SetDisabled(lockPlate != null, lockLine, lockPlate);
            }
            if (step == Step.Pending && Time.unscaledTime >= orderAt) OpenOrder();
            ShowPlate(sim);
        }

        void ShowPlate(Simulation sim)
        {
            if (plate == null) return;
            var (vPlate, vLine, evac) = sim?.Board != null ? LastSwitchViewBuilder.Variant(sim) : (null, null, false);
            plate.Show(lastSwitch.Phase, lastSwitch.LockPlate, lastSwitch.LockLine, vPlate, vLine, evac);
        }

        // ------------------------------------------------------------------ şalter

        void Armed()
        {
            var sim = Sim;
            if (sim == null) return;
            if (rig != null) rig.Focus(SiginakPose.LastSwitch);
            var reasons = sim.LastSwitchReasons();
            Post(LastSwitchViewBuilder.SwitchName, reasons.Count == 0
                ? Loc.T("çekilirse ÇEKİLME: düzenli çekilme, yenilgi sayılmaz.")
                : Loc.F("çekilirse TAHLİYE: {0}. Yenilgi sayılır.", LastSwitchViewBuilder.ReasonList(sim, reasons)), TelexKind.Info);
        }

        /// <summary>Mühürlüyken tık: titremenin yanında teleks nedeni söyler ("SON ŞALTER · Son şalter 5. vardiyadan önce çekilemez.").</summary>
        void Denied()
        {
            string why = lastSwitch != null ? lastSwitch.LockLine : null;
            if (string.IsNullOrEmpty(why)) return;
            // Art arda tıklar teleksi doldurmasın: aynı neden kısa süre içinde bir kez
            if (why == lastDenied && Time.unscaledTime < deniedUntil) return;
            lastDenied = why;
            deniedUntil = Time.unscaledTime + DeniedRepeatS;
            Post(LastSwitchViewBuilder.SwitchName, why, TelexKind.Info);
        }

        void CoverClosed()
        {
            if (step == Step.Idle && rig != null && rig.Current == SiginakPose.LastSwitch) rig.Focus(SiginakPose.Desk);
        }

        void Thrown()
        {
            var sim = Sim;
            string why = sim == null ? Loc.T("Harekât bitti.") : LastSwitchViewBuilder.Lock(sim, Inputs()).line;
            if (why != null)
            {
                lastSwitch.RejectThrow();
                Post(Loc.T("YAPILAMADI"), why, TelexKind.Alarm);
                return;
            }
            lastSwitch.SetAwaitingReceipt(true);
            step = Step.Pending;
            orderAt = Time.unscaledTime + OrderDelayS;
        }

        void OpenOrder()
        {
            var sim = Sim;
            if (sim == null || order == null) { Cancel(); return; }
            if (rig != null) rig.Focus(SiginakPose.Desk);
            order.Show(LastSwitchViewBuilder.Order(sim), true);
            step = Step.Open;
            Sfx.Play(Cue.PaperSlide);
        }

        void StampClicked()
        {
            if (step != Step.Open) return;
            step = Step.Stamping;
            Sfx.Play(Cue.StampHeavy);
            order.StampReceived(Stamped);
        }

        void Stamped()
        {
            var r = game != null ? game.ExecuteBoardCommand(s => s.PullLastSwitch()) : ActResult.Fail(Loc.T("Harekât bitti."));
            Pulled?.Invoke(r);
            if (r.Ok)
            {
                Sfx.Play(Cue.PowerDown);
                if (order != null) order.Hide();
                step = Step.Idle;
                return;
            }
            Post(Loc.T("YAPILAMADI"), r.Msg, TelexKind.Alarm);
            Cancel(false);
        }

        // ------------------------------------------------------------------ vazgeç

        /// <summary>Esc: kâğıt açıksa ya da kol inikse vazgeç, kapak açıksa kapat. Bir şey yaptıysa true.</summary>
        public bool HandleEscape()
        {
            if (step == Step.Stamping) return true;
            if (step != Step.Idle) { Cancel(); return true; }
            if (lastSwitch != null && lastSwitch.Phase == ShiftPhase.CoverUp && lastSwitch.Cancel()) return true;
            return false;
        }

        void Back(SiginakPose pose)
        {
            if (step == Step.Stamping) return;
            if (step != Step.Idle) { Cancel(); return; }
            if (pose == SiginakPose.LastSwitch)
            {
                if (lastSwitch != null && lastSwitch.Phase == ShiftPhase.CoverUp) lastSwitch.Cancel();
                else if (rig != null) rig.Focus(SiginakPose.Desk);
            }
        }

        /// <summary>Damgadan önce vazgeçiş: kâğıt kalkar, kol yukarı, kapak kapanır; komut yazılmaz.</summary>
        public void Cancel() => Cancel(true);

        void Cancel(bool notify)
        {
            bool wasOpen = OrderOpen;
            step = Step.Idle;
            if (order != null && order.gameObject.activeSelf) { order.Hide(); if (wasOpen) Sfx.Play(Cue.PaperSlide); }
            if (lastSwitch != null) lastSwitch.SetAwaitingReceipt(false);
            if (rig != null && rig.Current == SiginakPose.LastSwitch) rig.Focus(SiginakPose.Desk);
            if (notify) Post(LastSwitchViewBuilder.SwitchName, Loc.T("emir damgalanmadı; şalter geri kaldırıldı."), TelexKind.Info);
        }

        void Reset()
        {
            step = Step.Idle;
            if (order != null) order.Hide();
            if (lastSwitch != null) lastSwitch.SetAwaitingReceipt(false, true);
        }

        void Relabel()
        {
            var sim = Sim;
            if (OrderOpen && sim != null && sim.S.Ending == null && order != null) order.Bind(LastSwitchViewBuilder.Order(sim));
        }

        void Post(string status, string message, TelexKind kind)
        {
            if (telex != null && surface != null) telex.Post(surface.Shift, status, message, kind);
            TelexLine?.Invoke(status, message);
        }

        void Play(ShiftSound sound) => Sfx.Play(SiginakShiftClose.CueOf(sound));
    }
}
