using System;
using System.Collections;
using System.Collections.Generic;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using Corrigible.Game.Raid;
using UnityEngine;
using UnityEngine.InputSystem;
using Where = Corrigible.Desk.ShiftReceiptPaper.Where;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kartlı oyunun vardiya kapanışı (Revizyon 3 · R19, PLAN §1.3–§1.11; kamera K6 = <see cref="SiginakPose.ShiftClose"/>).
    /// Kapak kalkınca ön kontrol (K6 + teleks <c>ÖN KONTROL</c>); kol inince <c>GameController.PrintBoardShift</c> (kayıt hemen
    /// yazılır), fiş kol −35°'ye varınca ağızdan dile çıkar. Fişe tık ele getirir (okumak zaman ilerletmez), EVRAK'a sürüklenip
    /// bırakılınca kabul edilen bırakıştan 0,30 s sonra, tam karanlıkta <c>GameController.FileBoardShift(id)</c> günü tam bir kez
    /// ilerletir; kamera K1'e kesilir, şalter 0,3 s'de hazıra döner, sayaçlar 0,4 s'de döner, teleks “VARDİYA DEVREDİLDİ”
    /// 1,2 s'de basılır. Reddedilen her bırakış ve her dosyalama reddi fişi dile döndürür. Kilit nedenlerini (<see cref="LockReason"/>)
    /// her karede şaltere iletir. Fiş eldeyken ve arşiv okunurken Esc ile sürüklemesiz sağ tıkı kendisi okur (masa o sırada
    /// kilitli). Sunumun ses olaylarını (<see cref="ShiftSound"/>) <see cref="CueOf"/> ile çalar. Kayıttan devamda basılı fiş
    /// animasyonsuz dilde geri gelir (<see cref="Restore"/>). Dosyalanan son 5 fiş EVRAK arşivindedir (yan dosya, kayda girmez).
    /// <para>
    /// Simülasyonu yalnız iki komutla değiştirir (<c>board_print</c>, <c>board_file</c>); fiş verisi
    /// <see cref="HandoverSlipFactsBuilder"/> ile salt okunur kurulur. Eski yol (<c>planningBoardEnabled = false</c>) bu bileşeni
    /// kullanmaz.
    /// </para>
    /// </summary>
    public sealed class SiginakShiftClose : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakDesk desk;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] ShiftSwitch shiftSwitch;
        [SerializeField] ShiftReceiptMachine machine;
        [SerializeField] EvrakTray tray;
        [SerializeField] TelexFeed telex;
        [Tooltip("Dosyalamanın tam ekran kararması (alfa 0 → 1 → 0; ışın yalnız karanlıkta alınır).")]
        [SerializeField] CanvasGroup fade;
        [SerializeField] Camera view;
        [Tooltip("EVRAK'tan okunan emir belgesi açıkken şalter kilitli (“belge açık”).")]
        [SerializeField] SiginakBriefing briefing;

        /// <summary>Gong kısık çalar (R19-not “tek tok, kısık”, R19b #6).</summary>
        const float GongVolume = 0.5f;

        readonly List<HandoverSlipFacts> archive = new List<HandoverSlipFacts>();
        /// <summary>Arşivin ait olduğu kayıt (yükleme / yeni oyun sırasında öteki dinleyicilerin erken yan dosya yazımına karşı).</summary>
        GameRecord archiveOf;
        HandoverSlipFacts live;
        string planIssue;
        bool filing, printing, rejectPending, reading, readSeen, suspendedByMe;
        int readFrame = -10, inputFrame = -10;
        SiginakPose readReturn;
        RightButtonGesture right;
        Coroutine flow;

        Simulation Sim => game != null ? game.Sim : null;
        ShiftReceiptPaper Paper => machine != null ? machine.Paper : null;

        /// <summary>Kapanış masayı kilitliyor: fiş elde ya da hareket hâlinde, sürükleniyor, dosyalama ya da arşiv okuma sürüyor.</summary>
        public bool Busy => filing || reading || Holding || PaperMoving;
        /// <summary>Fiş elde (canlı ya da arşivden okunan; ele gelirken de).</summary>
        public bool Holding
        {
            get
            {
                var p = Paper;
                if (p == null) return false;
                var where = p.State;
                return where == Where.ToHand || where == Where.Hand || where == Where.Reading;
            }
        }
        /// <summary>Sayaçlar dosyalama karanlığı bitene dek eski değerde tutulur.</summary>
        public bool HoldCounters { get; private set; }
        /// <summary>Dosyalama sürüyor (kabul edilen bırakıştan şalterin hazıra dönüşüne kadar).</summary>
        public bool Filing => filing;
        /// <summary>EVRAK arşivi: dosyalanan son fişler, eskiden yeniye (yan dosyaya <see cref="SiginakDesk.SaveNotes"/> yazar).</summary>
        public IReadOnlyList<HandoverSlipFacts> Archive => archive;

        /// <summary>
        /// Yan dosyaya yazılacak arşiv: <paramref name="record"/> bu arşivin kaydıysa bellekteki, değilse (kayıttan devamda ya da
        /// yeni oyunda bu bileşen henüz <c>GameLoaded</c>/<c>NewGameStarted</c>'ı işlemeden, ör. kameranın poz değişimi
        /// <see cref="SiginakDesk.SaveNotes"/>'u erken çağırınca) dosyadaki. Böylece başka oyunun arşivi kayda yazılmaz.
        /// </summary>
        public IReadOnlyList<HandoverSlipFacts> ArchiveFor(GameRecord record) =>
            record == null || ReferenceEquals(record, archiveOf) ? archive : SiginakDeskStore.LoadSlips(record);

        /// <summary>Fiş sürükleniyor, tepsiye iniyor ya da dile dönüyor.</summary>
        bool PaperMoving
        {
            get
            {
                var p = Paper;
                if (p == null) return false;
                var where = p.State;
                return p.IsDragging || where == Where.Dragging || where == Where.ToTray || where == Where.Returning;
            }
        }

        void OnEnable()
        {
            if (game != null) { game.Changed += Changed; game.GameLoaded += Restore; game.NewGameStarted += NewGame; }
            DeskLoc.Instance.Changed += Relabel;
            if (shiftSwitch != null) { shiftSwitch.LeverArrived += LeverArrived; shiftSwitch.Sound += Play; }
            if (machine != null)
            {
                machine.ReceiptFiled += FileReceipt;
                machine.DragChanged += DragChanged;
                machine.Clicked += SlipClicked;
                machine.Sound += Play;
                machine.CanFile = CanFile;
            }
            Restore();
        }

        void OnDisable()
        {
            if (game != null) { game.Changed -= Changed; game.GameLoaded -= Restore; game.NewGameStarted -= NewGame; }
            DeskLoc.Instance.Changed -= Relabel;
            if (shiftSwitch != null) { shiftSwitch.LeverArrived -= LeverArrived; shiftSwitch.Sound -= Play; }
            if (machine != null)
            {
                machine.ReceiptFiled -= FileReceipt;
                machine.DragChanged -= DragChanged;
                machine.Clicked -= SlipClicked;
                machine.Sound -= Play;
                if (machine.CanFile == (Func<string, bool>)CanFile) machine.CanFile = null;
            }
            StopFlow();
            reading = false;
            right.Reset();
            if (suspendedByMe && rig != null) rig.InputSuspended = false;
            suspendedByMe = false;
        }

        // ---------- Şalter ----------

        /// <summary>Kapak kalktı: kamera K6'ya, teleks ön kontrol satırı (plan geçerliyse “Plan hazır. Devir fişini çıkar.”).</summary>
        public void PreCheck()
        {
            var sim = Sim;
            if (sim?.Board == null) return;
            if (rig != null) rig.Focus(SiginakPose.ShiftClose);
            if (telex != null && surface != null)
                telex.Post(surface.Shift, Loc.T("ÖN KONTROL"), sim.BoardPlanCheck() ?? Loc.T("Plan hazır. Devir fişini çıkar."), TelexKind.Info);
        }

        /// <summary>
        /// Kol indi (şalterin <c>Thrown</c>'ı, <see cref="SiginakShift"/> iletir): fişi basar. Kayıt tıkla aynı karede yazılır; fiş kol
        /// aşağıya varınca çıkar (makine bekler). Ret kolu boş ağızla geri kaldırır (<see cref="ShiftSwitch.RejectThrow"/>, kol
        /// varınca “ağız boş” sesi) ve teleks YAPILAMADI basar.
        /// </summary>
        public void Throw()
        {
            var sim = Sim;
            if (sim?.Board == null || game == null) { if (shiftSwitch != null) shiftSwitch.Release(); return; }
            if (rig != null) rig.Focus(SiginakPose.ShiftClose);
            rejectPending = false;
            ActResult printed;
            // Basımın Changed bildirimi fişi kol inince çıkacak biçimde sunar (feed); başka tazeleme beslemeyi başlatmaz (Risk 3)
            printing = true;
            try
            {
                printed = game.PrintBoardShift();
                // Changed fişi zaten sunduysa (aynı kimlik) yeniden sunulmaz
                var p = Paper;
                if (printed.Ok && (p == null || p.ReceiptId != Sim?.Board?.ReceiptId)) Refresh(true);
            }
            finally { printing = false; }
            if (printed.Ok) return;
            rejectPending = true;
            if (shiftSwitch != null) shiftSwitch.RejectThrow();
            if (telex != null && surface != null) telex.Post(surface.Shift, Loc.T("YAPILAMADI"), printed.Msg, TelexKind.Failed);
        }

        void LeverArrived(bool down)
        {
            // Basım reddi: kol indi, ağızdan fiş çıkmaz (“tık”); kol şalterin kendisinde 0,4 s'de kalkar
            if (!down || !rejectPending) return;
            rejectPending = false;
            if (Sim?.Board != null && Sim.Board.Printed) return;
            Play(ShiftSound.MouthEmpty);
        }

        /// <summary>
        /// Her kare (kartlı oyunda <see cref="SiginakDesk"/> çağırır): kilit nedenini (<see cref="LockReason"/>) şaltere ve plakaya
        /// iletir. Basılıyken kilit yoktur; dosyalama sürerken şalter karanlıkta değişmez.
        /// </summary>
        public void UpdateLock()
        {
            var sim = Sim;
            if (sim == null || shiftSwitch == null || filing) return;
            var board = sim.Board;
            bool printed = board != null && board.Printed;
            bool raid = sim.S.Raid != null || RaidLauncher.IsRaidLoaded;
            bool ended = sim.S.Ending != null;
            bool story = game != null && game.StoryHold || briefing != null && briefing.Busy;
            // Rapor yalnız eski yolda açılır (kartlı oyunda EVRAK fiş arşividir, Karar D7-A)
            var (plate, line) = LockReason(raid, ended, story, false, printed, printed ? null : planIssue);
            shiftSwitch.SetDisabled(plate != null, line, plate);
        }

        /// <summary>
        /// Şalter kilidinin nedeni; öncelik baskın &gt; harekât bitti &gt; belge açık &gt; rapor açık (eski yol) &gt; plan. Basılıyken
        /// kilit yoktur (fiş dosyalanmalı). <c>plate</c> plakanın kısa metni, <c>line</c> üzerine gelme satırının uzun metni (o anki
        /// dilde); kilit yoksa ikisi de null. <paramref name="planIssue"/>: <c>BoardPlanCheck()</c>'in iletisi (null: plan geçerli).
        /// </summary>
        public static (string plate, string line) LockReason(bool raid, bool ended, bool story, bool report, bool printed, string planIssue)
        {
            if (printed) return (null, null);
            if (raid) return (Loc.T("baskın sürüyor"), Loc.T("Baskın sürüyor: vardiya bitirilemez."));
            if (ended) return (Loc.T("harekât bitti"), Loc.T("Harekât bitti."));
            if (story) return (Loc.T("belge açık"), Loc.T("Önce açılış belgesini kapat."));
            if (report) return (Loc.T("rapor açık"), Loc.T("Rapor açık: önce EVRAK tepsisine dosyala."));
            if (!string.IsNullOrEmpty(planIssue)) return (Loc.T("plan düzeltilmeli"), planIssue);
            return (null, null);
        }

        // ---------- Esc ve sağ tık (el dışı) ----------

        /// <summary>
        /// Esc (el dışı; <see cref="SiginakDesk.HandleEscape"/>): açık kapağı kapatır, değilse K6'dan K1'e döner. Bir şey yaptıysa
        /// true. Fiş eldeyken ya da hareket hâlindeyken Esc'i <c>Update</c> okur (masa kilitli, buraya gelmez); aynı karede ikinci
        /// kez işlenmez.
        /// </summary>
        public bool HandleEscape()
        {
            if (Busy || inputFrame == Time.frameCount) return true;
            if (shiftSwitch != null && shiftSwitch.Cancel()) return true;
            if (rig != null && rig.Current == SiginakPose.ShiftClose) { rig.ResetView(); return true; }
            return false;
        }

        /// <summary>K6'da sürüklemesiz sağ tık (el dışı; <see cref="SiginakCameraRig.BackClicked"/>): <see cref="HandleEscape"/> ile aynı sıra.</summary>
        public void HandleBack() => HandleEscape();

        // ---------- Fiş ----------

        /// <summary>
        /// Fişe sürüklemesiz tık (makinenin <c>Clicked</c>'ı): dildeki (çıkan, dile dönen) fiş ele gelir, eldeki dile döner; arşivden
        /// okunan fiş tepsiye döner (Karar D5-A).
        /// </summary>
        void SlipClicked()
        {
            var p = Paper;
            if (p == null || filing) return;
            if (p.ReadOnly) { if (p.State == Where.Reading) PutBack(); return; }
            switch (p.State)
            {
                case Where.Feeding:
                case Where.Tongue:
                case Where.Returning:
                    // K1'den tık: önce K6 (el hedefi canlı kameradan; fiş kamerayla birlikte gelir, K1'in lens kaydırması girmez)
                    if (rig != null && rig.Current != SiginakPose.ShiftClose) rig.Focus(SiginakPose.ShiftClose);
                    p.TakeToHand();
                    SyncSuspend();
                    break;
                case Where.ToHand:
                case Where.Hand:
                    PutBack();
                    break;
            }
        }

        void PutBack()
        {
            var p = Paper;
            if (p == null) return;
            inputFrame = Time.frameCount;
            right.Reset();
            p.PutBack();
        }

        void DragChanged(bool on)
        {
            SyncSuspend();
            if (!on && tray != null && !filing) tray.SetReceiptTarget(false);
        }

        /// <summary>EVRAK'a bırakılan fiş dosyalanabilir mi (eski SS:148 korumaları; değilse makine fişi dile döndürür).</summary>
        bool CanFile(string id)
        {
            var board = Sim?.Board;
            var p = Paper;
            return !filing && !reading && board != null && board.Printed && !string.IsNullOrEmpty(id) && board.ReceiptId == id &&
                   (p == null || !p.ReadOnly) && surface != null && !surface.BlockedExceptShift;
        }

        void FileReceipt(string id)
        {
            if (!CanFile(id))
            {
                // Bulgu 17: fiş hiçbir durumda tepside asılı kalmaz
                if (!filing && machine != null) machine.Reject();
                return;
            }
            flow = StartCoroutine(FilingFlow(id));
        }

        /// <summary>
        /// Dosyalama zaman çizelgesi (PLAN §1.8; <see cref="FilingTimeline"/>), t = 0 kabul edilen bırakış: fiş tepsiye (0,2 s) →
        /// kararma (0,1 s) → tam karanlıkta gong ve <c>FileBoardShift</c>; başarıda arşiv, tepsideki kopya, K1'e kesme → açılma
        /// (0,1 s) → şalter hazıra, sayaçlar 0,4 s, teleks 1,2 s. Ret: teleks YAPILAMADI, fiş tepsiden dile.
        /// </summary>
        IEnumerator FilingFlow(string id)
        {
            filing = true;
            HoldCounters = true;
            SyncSuspend();
            var timeline = FilingTimeline.From(SiginakTheme.Active);
            // Dosyalanan fişin verisi devirden önce (devirde ReceiptId / ReceiptText silinir; B §0)
            var filedFacts = HandoverSlipFactsBuilder.Build(Sim) ?? live;
            var p = Paper;
            if (p != null && p.State != Where.ToTray && p.State != Where.Tray) p.FileToTray();
            if (tray != null) tray.SetReceiptTarget(false);

            float t = 0;
            while (t < timeline.ToTrayS) { yield return null; t += Time.unscaledDeltaTime; }
            if (fade != null) fade.blocksRaycasts = true;
            while (t < timeline.FileAtS)
            {
                if (fade != null) fade.alpha = timeline.Alpha(t);
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            if (fade != null) fade.alpha = 1;
            // Tam karanlık bir kare çizilir: dosyalama ve K1 kesmesi aynı karede ağırdır, kare uzun sürerse siyah hiç görünmezdi
            // (Aşama 4 Play denemesi: alfa en çok 0,5'te kalıyordu)
            yield return null;

            Play(ShiftSound.Gong);
            var filed = game != null ? game.FileBoardShift(id) : ActResult.Fail(null);
            if (filed.Ok)
            {
                AddToArchive(filedFacts);
                if (tray != null) tray.ShowFiled(filedFacts);
                if (p != null) p.Hide();
                live = null;
                if (rig != null) rig.Snap(SiginakPose.Desk);
                if (desk != null) desk.SaveNotes();
            }
            else if (telex != null && surface != null) telex.Post(surface.Shift, Loc.T("YAPILAMADI"), filed.Msg, TelexKind.Failed);

            // K1 kesmesi de karanlıkta çizilir; açılma (DarkOutS) dosyalamadan sonraki ilk kareden sayılır
            yield return null;
            float outT = 0;
            while (outT < timeline.DarkOutS)
            {
                if (fade != null) fade.alpha = Mathf.Clamp01(1 - outT / timeline.DarkOutS);
                yield return null;
                outT += Time.unscaledDeltaTime;
            }
            if (fade != null) { fade.alpha = 0; fade.blocksRaycasts = false; }

            filing = false;
            HoldCounters = false;
            flow = null;
            if (filed.Ok)
            {
                if (shiftSwitch != null) shiftSwitch.SetAwaitingReceipt(false, false);
                if (desk != null) desk.ShowCounters(true, 0, -1, true, timeline.CountersS);
                if (telex != null && surface != null)
                    telex.Post(surface.Shift, Loc.T("VARDİYA DEVREDİLDİ"), Loc.T("Devir fişi EVRAK kutusuna kondu. Yeni vardiya başladı."),
                        TelexKind.Done, typeSeconds: timeline.TelexS);
                Refresh(true);
            }
            else
            {
                if (machine != null) machine.Reject();
                if (desk != null) desk.ShowCounters(true);
            }
            SyncSuspend();
        }

        void StopFlow()
        {
            if (flow != null) { StopCoroutine(flow); flow = null; }
            if (!filing) return;
            filing = false;
            if (fade != null) { fade.alpha = 0; fade.blocksRaycasts = false; }
            if (HoldCounters) { HoldCounters = false; if (desk != null) desk.ShowCounters(false); }
        }

        void AddToArchive(HandoverSlipFacts facts)
        {
            if (facts == null) return;
            archive.Add(facts);
            if (archive.Count > SiginakDeskStore.SlipKeep) archive.RemoveRange(0, archive.Count - SiginakDeskStore.SlipKeep);
        }

        // ---------- EVRAK arşivi (Karar D7-A) ----------

        /// <summary>
        /// EVRAK'a tık: fiş basılı değilse ve arşivde fiş varsa son fişi elde salt okunur gösterir (kamera K6'ya; okuma bitince
        /// geldiği poza döner). Gösterdiyse true; değilse çağıran emir belgesini açar.
        /// </summary>
        public bool OpenTray()
        {
            var board = Sim?.Board;
            var p = Paper;
            if (board == null || board.Printed || p == null || tray == null || Busy || surface != null && surface.InputBlocked) return false;
            var last = tray.LastFiled ?? (archive.Count > 0 ? archive[archive.Count - 1] : null);
            if (last == null) return false;
            readReturn = rig != null ? rig.Current : SiginakPose.Desk;
            p.ReadArchived(last, tray.SlipDropPose);
            // Kâğıt okumayı reddettiyse (kamera bağlı değil, canlı fiş var) çağıran emir belgesini açar
            if (!p.ReadOnly) return false;
            // El hedefi her karede canlı kameradan: kamera K6'ya giderken fiş onunla gelir
            if (rig != null && rig.Current != SiginakPose.ShiftClose) rig.Focus(SiginakPose.ShiftClose);
            reading = true;
            readSeen = true;
            readFrame = Time.frameCount;
            tray.SetFiledHidden(true);
            SyncSuspend();
            return true;
        }

        /// <summary>Arşiv fişi okunurken ←: fiş tepsiye döner, emir belgesi okuma kipinde açılır (eski rapor tarayıcısının ← davranışı).</summary>
        void ToBriefing()
        {
            if (briefing == null || tray == null || !tray.HasBriefing) return;
            PutBack();
            if (rig != null && rig.Current == SiginakPose.ShiftClose && readReturn != SiginakPose.ShiftClose) rig.Focus(readReturn);
            briefing.OpenReading();
        }

        void EndReading()
        {
            reading = false;
            if (tray != null) tray.SetFiledHidden(false);
            if (rig != null && rig.Current == SiginakPose.ShiftClose && readReturn != SiginakPose.ShiftClose) rig.Focus(readReturn);
            SyncSuspend();
        }

        // ---------- Kare ----------

        void Update()
        {
            var p = Paper;
            if (reading && p != null)
            {
                // Arşiv fişi tepsiye dönüp gizlendi; hiç görünmediyse (ReadArchived reddetti) iki kare sonra bırakılır
                if (p.State != Where.Hidden) readSeen = true;
                else if (readSeen || Time.frameCount - readFrame > 1) EndReading();
            }
            SyncSuspend();
            ReadHandInput(p);
        }

        /// <summary>
        /// Fiş eldeyken (ve arşiv okunurken) Esc ve sürüklemesiz sağ tık fişi geri koyar, ← emir belgesine geçer. Masa bu sırada
        /// kilitli olduğu için bu tuşlar <c>DeskInput</c>'a ve kameraya ulaşmaz (PLAN §1.6).
        /// </summary>
        void ReadHandInput(ShiftReceiptPaper p)
        {
            bool hand = !filing && p != null && (p.State == Where.ToHand || p.State == Where.Hand || p.State == Where.Reading);
            if (!hand) { right.Reset(); return; }
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { PutBack(); return; }
            if (reading && kb != null && kb.leftArrowKey.wasPressedThisFrame) { ToBriefing(); return; }
            var mouse = Mouse.current;
            if (mouse == null) { right.Reset(); return; }
            var button = mouse.rightButton;
            var position = mouse.position.ReadValue();
            if (button.wasPressedThisFrame) right.Press(position);
            if (!right.Held) return;
            if (button.isPressed) right.Move(position, mouse.delta.ReadValue(), SiginakCameraRig.RightClickSlop);
            else if (button.wasReleasedThisFrame) { if (right.Release()) PutBack(); }
            else right.Reset();
        }

        void LateUpdate()
        {
            var p = Paper;
            // BlockedExceptShift ⊆ InputBlocked: fiş eldeyken masa kilitli, fişin kendisi tıklanır kalır
            if (p != null) p.Interactable = !filing && surface != null && !surface.BlockedExceptShift;
        }

        /// <summary>Kamera kısayolları, bakış ve sağ tık kapanış meşgulken kapalı (yalnız kendi açtığı askıyı kaldırır).</summary>
        void SyncSuspend()
        {
            if (rig == null) return;
            bool want = Busy;
            if (want && !suspendedByMe) { rig.InputSuspended = true; suspendedByMe = true; }
            else if (!want && suspendedByMe) { rig.InputSuspended = false; suspendedByMe = false; }
        }

        // ---------- Durum ----------

        void Changed()
        {
            planIssue = PlanIssue();
            // Dosyalama sürerken tazeleme yok: şalter kararmada kalkmaz, fiş tepside kalır
            if (!filing) Refresh(false);
        }

        void Relabel()
        {
            planIssue = PlanIssue();
            if (!filing) Refresh(false);
        }

        /// <summary>Basılı fişi o anki veriyle sunar; basılı değilse fişi gizler. Basım anında (<c>printing</c>) kol inince çıkar.</summary>
        void Refresh(bool force)
        {
            if (machine == null || reading) return;
            var sim = Sim;
            var board = sim?.Board;
            if (board != null && board.Printed && !string.IsNullOrEmpty(board.ReceiptId))
            {
                live = HandoverSlipFactsBuilder.Build(sim);
                machine.Present(board.ReceiptId, live, printing);
                return;
            }
            live = null;
            var p = Paper;
            if (force || p == null || !string.IsNullOrEmpty(p.ReceiptId)) machine.Present(null, null, false);
        }

        /// <summary>
        /// Kayıttan devam (<c>GameLoaded</c>) ve açılış: arşiv yan dosyadan, tepsideki kopya; basılı fiş animasyonsuz dilde,
        /// şalter fiş bekliyor duruşunda (olaysız); basılı değilse şalter anında hazırda.
        /// </summary>
        public void Restore() => ResetState(true);

        void NewGame() => ResetState(false);

        // Ad bilerek Reset değil: Unity MonoBehaviour.Reset iletisi parametre almaz (editörde bileşen eklenince çağrılır)
        void ResetState(bool load)
        {
            StopFlow();
            reading = rejectPending = printing = false;
            right.Reset();
            // Okunan arşiv fişi kapanır (okuma bayrağı burada iner; kâğıt Reading'de kalırsa masa kilitli kalırdı)
            var paper = Paper;
            if (paper != null && paper.ReadOnly) paper.Hide();
            archive.Clear();
            archiveOf = Sim?.Record;
            if (load)
            {
                var saved = SiginakDeskStore.LoadSlips(archiveOf);
                if (saved != null) foreach (var f in saved) AddToArchive(f);
            }
            if (tray != null)
            {
                tray.SetReceiptTarget(false);
                tray.SetFiledHidden(false);
                tray.ShowFiled(archive.Count > 0 ? archive[archive.Count - 1] : null);
            }
            planIssue = PlanIssue();
            var board = Sim?.Board;
            if (shiftSwitch != null && board != null && !board.Printed)
            {
                // Fiş beklemiyor: şalter anında hazırda (süren bir basım reddi de anında biter; eski yolun şalterine dokunulmaz)
                shiftSwitch.SetAwaitingReceipt(false, true);
                shiftSwitch.Release(false);
            }
            Refresh(true);
            SyncSuspend();
        }

        /// <summary><c>BoardPlanCheck</c> önbelleği (her karede metin üretilmesin; Risk 12): komut, yükleme ve dil değişiminde.</summary>
        string PlanIssue()
        {
            var sim = Sim;
            return sim?.Board != null ? sim.BoardPlanCheck() : null;
        }

        // ---------- Ses ----------

        void Play(ShiftSound sound) => Sfx.Play(CueOf(sound), sound == ShiftSound.Gong ? GongVolume : 1f);

        /// <summary>Sunumun ses olayı → çalınacak <see cref="Cue"/> (PLAN §1.11). Klipsiz cue sessiz kalır.</summary>
        public static Cue CueOf(ShiftSound sound) => sound switch
        {
            ShiftSound.CoverOpen => Cue.ShiftCoverOpen,
            ShiftSound.CoverShut => Cue.ShiftCoverShut,
            ShiftSound.Lever => Cue.ShiftLever,
            ShiftSound.Denied => Cue.SwitchClack,
            ShiftSound.SlipPrint => Cue.SlipPrint,
            ShiftSound.SlipTear => Cue.SlipTear,
            ShiftSound.SlipLift => Cue.BoardLift,
            ShiftSound.SlipReturn => Cue.BoardReturn,
            ShiftSound.SlipFile => Cue.SlipFile,
            ShiftSound.MouthEmpty => Cue.SlipMouthEmpty,
            ShiftSound.Gong => Cue.ShiftGong,
            _ => throw new ArgumentOutOfRangeException(nameof(sound), sound, null),
        };

        // ---------- Zaman çizelgesi ----------

        /// <summary>
        /// Dosyalamanın süreleri (PLAN §1.8; token <c>motion.shiftClose.*</c>), t = 0 kabul edilen bırakış: fiş tepsiye
        /// <see cref="ToTrayS"/> (0,2), kararma <c>darkS</c>'in yarısı (0,1; <c>afterDrop.darkSplit</c>), tam karanlıkta dosyalama
        /// (<see cref="FileAtS"/> 0,3), açılma öteki yarısı (<see cref="LightAtS"/> 0,4); sonra sayaçlar <see cref="CountersS"/> (0,4),
        /// teleks satırı <see cref="TelexS"/> (1,2).
        /// </summary>
        public readonly struct FilingTimeline
        {
            public readonly float ToTrayS, DarkInS, DarkOutS, CountersS, TelexS;

            public FilingTimeline(float toTrayS, float darkS, float countersS, float telexS)
            {
                ToTrayS = Mathf.Max(0, toTrayS);
                DarkInS = DarkOutS = Mathf.Max(0.001f, darkS * 0.5f);
                CountersS = countersS;
                TelexS = telexS;
            }

            /// <summary>Tam karanlık: gong ve <c>FileBoardShift</c>.</summary>
            public float FileAtS => ToTrayS + DarkInS;
            /// <summary>Kararma açıldı: şalter, sayaçlar ve teleks.</summary>
            public float LightAtS => FileAtS + DarkOutS;

            /// <summary>Kararmanın alfası, bırakıştan <paramref name="t"/> saniye sonra (0 → 1 doğrusal, tepe <see cref="FileAtS"/>, 1 → 0).</summary>
            public float Alpha(float t)
            {
                if (t <= ToTrayS || t >= LightAtS) return 0;
                if (t < FileAtS) return Mathf.Clamp01((t - ToTrayS) / DarkInS);
                return Mathf.Clamp01(1 - (t - FileAtS) / DarkOutS);
            }

            public static FilingTimeline From(SiginakTheme theme)
            {
                float N(string key, float fallback) => theme != null ? theme.Number(key, fallback) : fallback;
                return new FilingTimeline(N("motion.shiftClose.slipToTrayS", 0.2f), N("motion.shiftClose.darkS", 0.2f),
                    N("motion.shiftClose.countersS", 0.4f), N("motion.shiftClose.telexPrintS", 1.2f));
            }
        }
    }
}
