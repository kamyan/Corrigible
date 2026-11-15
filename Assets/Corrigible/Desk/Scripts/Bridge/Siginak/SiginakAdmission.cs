using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kapı ve kabul akışı (Revizyon 3 · Aşama 6, R9/R17; PLAN §1.3–§1.5, §1.11, §1.13, §1.14): K8/K8A/K8R pozları ve geri zinciri,
    /// vuruş algısı (canlı oyunda grup değişince bir kez; yüklemede yok), tezgâh durumu, damga olaylarından Core komutları
    /// (<c>GameController.ExecuteBoardCommand</c>), kabul dizisi (kapı), sesler, "KAPI" teleksi, kayıttan devamda animasyonsuz kurulum.
    /// Kartlı oyunda kapının tek sahibi.
    /// <para>
    /// KOS aşama 2 (aday grubu): kapıda 2–3 aday kartı ve grup fişi durur. Simülasyonu yalnız iki komutla değiştirir:
    /// <c>ChooseCandidate</c> (KABUL damgası bir karta; dolu sığınakta fişte işaretlenen kişiyle) ve <c>PassCandidates</c> (GERİ ÇEVİR
    /// damgası grup fişine). Her komuttan önce Core'un kendi denetimi okunur (<c>CandidateChoiceCheck</c>, <c>CandidatePassCheck</c>); ret
    /// komutsuz bir <see cref="AdmissionSound.Deny"/>'dır. Yollanacak kişinin işareti sunum durumudur (<see cref="AdmissionUi.ReplaceId"/>;
    /// komut yazmaz, kayda girmez). Yeni grup bir kez vurur ve teleks satırı düşer; süresi dolan grup için "gitti" satırı. Görünümler
    /// (<see cref="AdmissionStation"/>, <see cref="CandidateGroupView"/>, damgalar) yalnız anlık görüntüyü
    /// (<see cref="AdmissionSnapshotBuilder"/>) çizer ve olay yayar; ses burada <see cref="CueOf"/> ile çalınır. Eski tek adaylı kabulün
    /// inceleme araçları (büyüteç, interkom, buğu camı, talep defteri), belgesi, talimat şeridi ve bulgu fişleri aşama 7'de söküldü.
    /// </para>
    /// <para>
    /// Esc'in tek girişi <c>LiberationDeskView.Escape</c> → <see cref="HandleEscape"/>; sürüklemesiz sağ tık <c>SiginakDesk.Back</c> →
    /// <see cref="HandleBack"/>. Bir dizi oynarken masa kilitlidir (<see cref="Busy"/>): o sırada Esc ve sağ tıkı bu bileşen kendisi
    /// okur ve diziyi sona atlatır.
    /// </para>
    /// </summary>
    public sealed class SiginakAdmission : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakCameraRig rig;
        [Tooltip("Kapı satırı (KAPI: grubun gelişi ya da gidişi), kapanış teleksinden sonra.")]
        [SerializeField] TelexFeed telex;
        [Header("Kapı")]
        [Tooltip("Kanat, çark, vuruş lambası, merdiven ışığı (kabul dizisi kapıyı burada açar).")]
        [SerializeField] DoorProp door;
        [Tooltip("Kapının eski denetleyicisi: kartlı oyunda Visit/Reopen buraya gelir, Busy buradan okunur.")]
        [SerializeField] SiginakDoor doorController;
        [Header("Kabul istasyonu")]
        [SerializeField] AdmissionStation station;
        [Tooltip("Karar damgaları: KABUL ve GERİ ÇEVİR (sıra önemsiz; her biri kendi seçimini taşır).")]
        [SerializeField] StampDecision[] stamps = Array.Empty<StampDecision>();
        [Header("Oda")]
        [Tooltip("Teleks satırı dosyalama bitince basılır (ShiftFiling).")]
        [SerializeField] SiginakDesk desk;
        [SerializeField] PlanningBoardPresenter planningBoard;
        [Tooltip("İstasyonun üzerine gelme etiketi (istasyonda bağ yoksa bu verilir).")]
        [SerializeField] HoverTag hoverTag;
        [Tooltip("K8R çapası (Poses/K8R): tıklanan kâğıda göre her okumada yeniden konur.")]
        [SerializeField] Transform readAnchor;
        [Tooltip("K8 kadrajının altındaki masa kenarı (\"MASA · masaya dön\"); isteğe bağlı. Sahnede bilinçli olarak boş: K8 kadrajında görünmüyor (Aşama 6).")]
        [SerializeField] DeskTarget deskEdge;

        enum Sequence { None, Admit, Out }

        readonly AdmissionUi ui = new AdmissionUi();
        readonly List<(string title, string body)> pendingTelex = new List<(string, string)>();
        /// <summary>Bu sunum durumunun ait olduğu kayıt (başka bir kayıt yüklenince sunum durumu sıfırlanır).</summary>
        GameRecord uiOf;
        AdmissionSnapshot last;
        /// <summary>Vuruşu çalmış (ya da yüklemede duyurulmuş sayılan) grubun anahtarı; null = kapıda grup yoktu.</summary>
        string announcedKey;
        bool commanding, lockedByMe;
        int escapeFrame = -10;
        RightButtonGesture right;

        Sequence sequence;
        StampChoice sequenceChoice;
        DecisionTimeline timeline;
        int sequenceId;
        float sequenceT, openedAt, closedAt;
        bool stationDone, cameraMoved, doorOpened, leafSounded, doorClosed;

        Simulation Sim => game != null ? game.Sim : null;
        /// <summary>Kapıda bekleyen aday grubunun anahtarı; grup yoksa (ya da harekât bittiyse) null.</summary>
        string GroupKey => AdmissionSnapshotBuilder.GroupWaiting(Sim) ? AdmissionSnapshotBuilder.GroupKey(Sim) : null;

        /// <summary>Bir dizi oynuyor (masa girdisi kapalı).</summary>
        public bool Busy => sequence != Sequence.None;
        /// <summary>Kamera K8 ailesinde ve kabul kartlı oyunda var.</summary>
        public bool Active => rig != null && InFamily(rig.Current) && Sim?.Board != null && Sim.Admission != null;
        /// <summary>Sunum durumu (dolu sığınakta işaretli yollanacak kişi).</summary>
        public AdmissionUi Ui => ui;
        /// <summary>Son çizilen anlık görüntü (testler ve sahne denetimleri için).</summary>
        public AdmissionSnapshot Last => last;
        public SiginakDoor DoorController => doorController;
        public PlanningBoardPresenter PlanningBoard => planningBoard;

        /// <summary>Kapanış teleksine satır (başlık, gövde): "KAPI" + grubun gelişi ya da gidişi.</summary>
        public event Action<string, string> TelexLine;
        /// <summary>Çalınan her ses olayı (testler; ses <see cref="Sfx"/> ile ayrıca çalar).</summary>
        public event Action<AdmissionSound> Sounded;

        /// <summary>K8 ailesi: kapı (K8), tezgâh (K8A), okuma (K8R).</summary>
        public static bool InFamily(SiginakPose pose) => pose == SiginakPose.Door || pose == SiginakPose.Admission ||
            pose == SiginakPose.AdmissionRead;

        // ------------------------------------------------------------------ yaşam döngüsü

        void OnEnable()
        {
            if (game != null) { game.Changed += Refresh; game.GameLoaded += Restore; game.NewGameStarted += NewGame; }
            DeskLoc.Instance.Changed += Relabel;
            if (rig != null) { rig.PoseChanged += PoseChanged; rig.Arrived += OnArrived; }
            if (station != null)
            {
                station.CounterClicked += ClickCounter;
                station.DeskKeyClicked += ClickDeskKey;
                station.PaperClicked += ClickPaper;
                station.ReplaceClicked += ClickReplace;
                station.Sound += Play;
                if (station.Tag == null && hoverTag != null) station.Tag = hoverTag;
            }
            foreach (var s in stamps) if (s != null) { s.Pressed += StampPressed; s.Sound += Play; }
            ShareHoverTag();
            if (deskEdge != null) { deskEdge.Clicked += ClickDeskKey; deskEdge.BlockedOverride = DeskEdgeBlocked; }
            Restore();
        }

        void OnDisable()
        {
            if (game != null) { game.Changed -= Refresh; game.GameLoaded -= Restore; game.NewGameStarted -= NewGame; }
            DeskLoc.Instance.Changed -= Relabel;
            if (rig != null) { rig.PoseChanged -= PoseChanged; rig.Arrived -= OnArrived; }
            if (station != null)
            {
                station.CounterClicked -= ClickCounter;
                station.DeskKeyClicked -= ClickDeskKey;
                station.PaperClicked -= ClickPaper;
                station.ReplaceClicked -= ClickReplace;
                station.Sound -= Play;
            }
            foreach (var s in stamps) if (s != null) { s.Pressed -= StampPressed; s.Sound -= Play; }
            if (deskEdge != null) { deskEdge.Clicked -= ClickDeskKey; deskEdge.BlockedOverride = null; }
            EndSequence(false);
            right.Reset();
        }

        void StampPressed(StampChoice choice) => PressStamp(choice);
        /// <summary>
        /// Üzerine gelme etiketi: sahne bağı tek (bu bileşende); bağı boş damgalara verilir.
        /// </summary>
        void ShareHoverTag()
        {
            var tag = hoverTag != null ? hoverTag : station != null ? station.Tag : null;
            if (tag == null) return;
            foreach (var s in stamps) if (s != null && s.Tag == null) s.Tag = tag;
        }

        /// <summary>
        /// Tıklanabilirlik: istasyon (damgalar, aday kâğıtları dahil) K8 ailesinde, dizi yokken, oda kilitli değilken. Aile dışında
        /// çarpıştırıcıları kapalıdır: K1'den kapıya tık kapının kendi hedefine düşer.
        /// </summary>
        void ApplyInteractive()
        {
            bool on = Interactive;
            if (station != null)
            {
                station.SetReading(rig != null && rig.Current == SiginakPose.AdmissionRead);
                station.SetInteractive(on);
            }
        }

        /// <summary>Masa kenarı yalnız K8 ailesinde (ve oda kilitli değilken) tıklanır.</summary>
        bool DeskEdgeBlocked() => !Active || Busy || DeskTarget.Blocked != null && DeskTarget.Blocked();

        void Update()
        {
            if (Sim?.Board != null && door != null)
            {
                // Vuruş lambası kartlı oyunda burada: aday varken amber (vuruş parlamaları Knock'ta), yoksa sönük. Eski denetleyicinin
                // yükleme sıfırlaması (ResetClosed) bir kare sonra düzelir.
                bool waiting = GroupKey != null;
                if (door.Knocking != waiting && sequence != Sequence.Admit) door.SetKnocking(waiting);
            }
            ReadSequenceInput();
            Advance(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Dizi sürerken Esc ve sürüklemesiz sağ tık diziyi sona atlatır (masa kilitli olduğu için bu tuşlar <c>DeskInput</c>'a ve
        /// kameraya ulaşmaz; SiginakShiftClose kalıbı). Aynı karede <see cref="HandleEscape"/> ikinci kez işlemez.
        /// </summary>
        void ReadSequenceInput()
        {
            if (sequence == Sequence.None) { right.Reset(); return; }
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { escapeFrame = Time.frameCount; Skip(); return; }
            var mouse = Mouse.current;
            if (mouse == null) { right.Reset(); return; }
            var button = mouse.rightButton;
            var position = mouse.position.ReadValue();
            if (button.wasPressedThisFrame) right.Press(position);
            if (!right.Held) return;
            if (button.isPressed) right.Move(position, mouse.delta.ReadValue(), SiginakCameraRig.RightClickSlop);
            else if (button.wasReleasedThisFrame) { if (right.Release()) Skip(); }
            else right.Reset();
        }

        /// <summary>
        /// Diziyi ve teleks satırını <paramref name="dt"/> kadar ilerletir (edit modda testler sürer). Kapının kendi hareketini
        /// <see cref="DoorProp"/> sürer.
        /// </summary>
        public void Advance(float dt)
        {
            if (dt < 0) dt = 0;
            if (sequence != Sequence.None) StepSequence(dt);
            FlushTelex();
        }

        // ------------------------------------------------------------------ durum

        /// <summary>Yeni oyun: sunum durumu sıfır, kapıdaki grup (varsa) duyurulmuş sayılır; vuruş yok.</summary>
        void NewGame()
        {
            EndSequence(false);
            ResetPresentation();
            uiOf = Sim?.Record;
            ui.Reset();
            announcedKey = GroupKey;
            Render(false);
        }

        /// <summary>
        /// Kayıttan devam (ve açılış): sunum durumu sıfır, bekleyen grup duyurulmuş sayılır (vuruş çalmaz, D12), istasyon animasyonsuz
        /// son durumuna oturur.
        /// </summary>
        public void Restore()
        {
            EndSequence(false);
            ResetPresentation();
            uiOf = Sim?.Record;
            ui.Reset();
            announcedKey = GroupKey;
            pendingTelex.Clear();
            Render(false);
        }

        /// <summary>
        /// Oyun değişti (<c>GameController.Changed</c>): yeni grup bir kez vurur (tezgâh iner) ve teleks satırını bekletir, istasyon
        /// yeniden çizilir. Kendi komutlarımızın ve dizilerin sırasında çizim ertelenir (sunum kendi sırasıyla gelir).
        /// </summary>
        public void Refresh()
        {
            if (commanding || sequence != Sequence.None) return;
            var sim = Sim;
            if (sim == null) return;
            if (!ReferenceEquals(sim.Record, uiOf)) { Restore(); return; }
            if (sim.Admission != null)
            {
                string key = GroupKey;
                var adm = sim.Admission;
                // Bekleyen grup karar verilmeden gitti (süre aşımı bu kapanışta): tek satır. Aynı kapanışta yeni (ör. acil) grup gelse de
                // yazılır, geliş satırından önce. Seçme ve geri çevirme teleks yazmaz (PressStamp announcedKey'i sıfırlar).
                if (announcedKey != null && key != announcedKey)
                {
                    var gone = adm.Passed.Count > 0 ? adm.Passed[adm.Passed.Count - 1] : null;
                    if (gone != null && gone.Kind == "expired" && gone.Shift == sim.S.Day)
                        pendingTelex.Add((Loc.T("KAPI"), Loc.T("Adaylar karar beklemeden gitti.")));
                }
                if (key != null && key != announcedKey)
                {
                    // Yeni grup: kapı bir kez vurur, teleks satırı kapanış teleksinden sonra basılır
                    if (door != null) door.Knock();
                    Play(AdmissionSound.Knock);
                    int wait = Math.Max(0, adm.GroupWaitingUntil - sim.S.Day);
                    pendingTelex.Add((Loc.T("KAPI"), adm.GroupEmergency
                        ? Loc.F("ACİL · {0} aday kapıda. Bekleme: {1} vardiya.", adm.Group.Count, wait)
                        : Loc.F("{0} aday kapıda. Bekleme: {1} vardiya.", adm.Group.Count, wait)));
                }
                announcedKey = key;
            }
            Render(true);
        }

        void Relabel() => Render(false);

        void ResetPresentation()
        {
            right.Reset();
            ReturnStamps(false);
        }

        /// <summary>
        /// Anlık görüntüyü kurar ve görünümlere bağlar. K8 ailesindeyken inik tezgâh, görülecek bir şey kalmasa da ancak aileden
        /// çıkışta katlanır (PLAN §1.6 T4); karar dizisi kendi katlanışını oynatır.
        /// </summary>
        void Render(bool animate)
        {
            var sim = Sim;
            if (sim?.Board == null || sim.Admission == null) { last = null; return; }
            var s = AdmissionSnapshotBuilder.Build(sim, ui);
            if (s == null) return;
            // Kurucunun saymadığı işaret (yer açıldı, kişi panodan düştü, grup değişti) sunum durumundan da silinir: sığınak yeniden
            // dolarsa eski işaret kendiliğinden geri gelmez
            if (ui.ReplaceId != null && s.Group?.ReplaceId != ui.ReplaceId) ui.ReplaceId = ui.ReplaceForGroup = null;
            if (s.Counter == CounterState.Folded && sequence == Sequence.None && rig != null && InFamily(rig.Current) && station != null &&
                (station.Counter == CounterState.Down || station.Counter == CounterState.Lowering))
                s.Counter = CounterState.Down;
            last = s;
            BindAll(s, animate);
        }

        void BindAll(AdmissionSnapshot s, bool animate)
        {
            if (station != null) station.Bind(s, animate);
            ApplyInteractive();
            foreach (var st in stamps)
            {
                if (st == null) continue;
                var d = s.Stamps[(int)st.Choice];
                if (d != null) st.Bind(d);
            }
        }

        /// <summary>İstasyon tıklanır: K8 ailesinde, dizi yokken ve oda (kapı dışındaki kilitlerle) kilitli değilken.</summary>
        bool Interactive => Active && sequence == Sequence.None && (surface == null || !surface.BlockedExceptDoor);

        /// <summary>Komut yazılabilir mi (Core'un denetimi ayrıca okunur).</summary>
        bool CanAct => game != null && Sim?.Board != null && Sim.Admission != null && Interactive;

        // ------------------------------------------------------------------ pozlar ve geri zinciri

        /// <summary>KAPI istasyonuna (K8) girer (KAPI tuşu, 4 / K, K1'de kapıya tık).</summary>
        public void Enter()
        {
            if (rig == null || Sim?.Board == null || Busy) return;
            if (surface != null && surface.BlockedExceptDoor) return;
            rig.Focus(SiginakPose.Door);
        }

        /// <summary>K8 ailesinden masaya (K1) döner (MASA tuşu, masa kenarı, K8'de Esc / sağ tık).</summary>
        public void Leave()
        {
            if (sequence != Sequence.None) Skip();
            ReturnStamps(false);
            if (rig != null) rig.ResetView();
        }

        /// <summary>Esc'in tek girişi (<c>LiberationDeskView.Escape</c>): bir seviye geri; işlediyse true.</summary>
        public bool HandleEscape()
        {
            // Dizi bu karede Esc'le zaten atlandı: aynı tuş bir seviye daha geri gitmesin
            if (Time.frameCount == escapeFrame) return true;
            return Back();
        }

        /// <summary>Sürüklemesiz sağ tık: bir seviye geri (damga → dizi → K8R → K8A → K8 → K1).</summary>
        public void HandleBack() => Back();

        bool Back()
        {
            foreach (var s in stamps)
                if (s != null && s.Held) { s.ReturnToRack(true); return true; }
            if (sequence != Sequence.None) { Skip(); return true; }
            if (rig == null || Sim?.Board == null) return false;
            switch (rig.Current)
            {
                case SiginakPose.AdmissionRead: rig.Focus(SiginakPose.Admission); return true;
                case SiginakPose.Admission: rig.Focus(SiginakPose.Door); return true;
                case SiginakPose.Door: Leave(); return true;
                default: return false;
            }
        }

        void PoseChanged(SiginakPose pose)
        {
            if (!InFamily(pose))
            {
                ReturnStamps(false);
                if (sequence != Sequence.None) Skip();
                // Aileden çıkış: görülecek bir şey yoksa tezgâh şimdi katlanır (T4)
                Render(true);
                return;
            }
            ApplyInteractive();
        }

        /// <summary>Kamera poza oturdu: tıklanabilirlik yeniden kurulur.</summary>
        public void OnArrived(SiginakPose pose) => ApplyInteractive();

        // ------------------------------------------------------------------ istasyon girdileri (olayların karşılığı)

        /// <summary>Tezgâha tık: K8'de inik tezgâh K8A'ya götürür; K8R'de okumayı bitirir.</summary>
        public void ClickCounter()
        {
            if (!Interactive) return;
            if (rig.Current == SiginakPose.AdmissionRead) { rig.Focus(SiginakPose.Admission); return; }
            if (rig.Current != SiginakPose.Admission && CounterDown) rig.Focus(SiginakPose.Admission);
        }

        /// <summary>
        /// Kâğıda tık (aday kartı, grup fişi): K8A'da kâğıt K8R'de okunur (çapa kâğıda göre); K8R'de ikinci tık K8A'ya döner; K8'de tezgâha
        /// tık gibi.
        /// </summary>
        public void ClickPaper(AdmissionPaper kind, int index)
        {
            if (!Interactive) return;
            var pose = rig.Current;
            if (pose == SiginakPose.AdmissionRead) { rig.Focus(SiginakPose.Admission); return; }
            if (pose != SiginakPose.Admission) { if (CounterDown) rig.Focus(SiginakPose.Admission); return; }
            if (station != null && readAnchor != null)
            {
                var read = station.ReadPose(kind, index, ReadDistance);
                readAnchor.SetPositionAndRotation(read.position, read.rotation);
            }
            rig.Focus(SiginakPose.AdmissionRead);
        }

        /// <summary>MASA tuşu ve masa kenarı: doğrudan K1.</summary>
        public void ClickDeskKey()
        {
            if (Busy || rig == null || !InFamily(rig.Current)) return;
            Leave();
        }

        /// <summary>Tezgâh inik mi: istasyon varsa onun durumu, yoksa (testler) Core'dan aynı kural (kapıda grup bekliyor).</summary>
        bool CounterDown => station != null ? station.Counter == CounterState.Down : GroupKey != null;

        /// <summary>
        /// Grup fişindeki bir satıra tık (dolu sığınak): kişi yollanacak olarak işaretlenir; aynı kişiye ikinci tık işareti kaldırır.
        /// Yalnız sunum durumu (<see cref="AdmissionUi.ReplaceId"/>): komut yazmaz. Kişi KABUL basışında aynı komutla yollanır.
        /// </summary>
        public void ClickReplace(string personId)
        {
            if (!Interactive || string.IsNullOrEmpty(personId)) return;
            string key = GroupKey;
            if (key == null) return;
            bool same = ui.Replace(key) == personId;
            ui.ReplaceForGroup = same ? null : key;
            ui.ReplaceId = same ? null : personId;
            Play(AdmissionSound.DocDrop);
            Render(true);
        }

        static float ReadDistance => Num("admission.readDistanceM", 0.318f);

        static float Num(string key, float fallback) => SiginakTheme.Active != null ? SiginakTheme.Active.NumberOf(key, fallback) : fallback;

        ActResult Command(Func<Simulation, ActResult> command)
        {
            commanding = true;
            try { return game.ExecuteBoardCommand(command); }
            finally { commanding = false; }
        }

        // ------------------------------------------------------------------ damgalar → ChooseCandidate / PassCandidates ve diziler

        /// <summary>Damga basıldı: hedef kart, görünümün son kabul ettiği KABUL hedefidir (<see cref="CandidateGroupView.StampCandidateId"/>).</summary>
        public bool PressStamp(StampChoice choice) =>
            PressStamp(choice, station != null && station.Candidates != null ? station.Candidates.StampCandidateId : null);

        /// <summary>
        /// Damga basıldı. KABUL: <c>ChooseCandidate</c> (<paramref name="candidateId"/>; dolu sığınakta fişte işaretli kişi aynı komutla
        /// yollanır). GERİ ÇEVİR: <c>PassCandidates</c>. Core denetimi önce; ret komutsuz <c>Deny</c>, damga rafına. Başarıda iz kartta /
        /// fişte kalır ve karar dizisi başlar: KABUL'de kâğıtlar çekmeceye, kamera K8'e, tezgâh katlanır, sonra çark, kanat, bekleme,
        /// kapanış; GERİ ÇEVİR'de istasyonun çekmece dizisi (kapı açılmaz). Başka seçim komutsuz reddedilir. İşlediyse true.
        /// </summary>
        public bool PressStamp(StampChoice choice, string candidateId)
        {
            if (!CanAct) { Deny(choice); return false; }
            var sim = Sim;
            Func<Simulation, ActResult> command;
            // Komuttan önceki görüntü: grup ve kartları; iz bunun üstüne basılır (komuttan sonra grup Core'dan düşer)
            var before = AdmissionSnapshotBuilder.Build(sim, ui);
            if (choice == StampChoice.Admit)
            {
                // Tek doğruluk kaynağı görüntü: kurucu, yer açıldıysa ya da kişi panodan düştüyse işareti saymaz
                string replace = before?.Group?.ReplaceId;
                if (sim.CandidateChoiceCheck(candidateId, replace) != null) { Deny(choice); return false; }
                AdmissionSnapshotBuilder.StampCard(before?.Group, candidateId, sim.B);
                command = s => s.ChooseCandidate(candidateId, replace);
            }
            else if (choice == StampChoice.Reject)
            {
                if (sim.CandidatePassCheck() != null) { Deny(choice); return false; }
                AdmissionSnapshotBuilder.StampPass(before?.Group);
                command = s => s.PassCandidates();
            }
            else { Deny(choice); return false; }
            var result = Command(command);
            if (result == null || !result.Ok) { Deny(choice); Render(true); return false; }
            ui.ReplaceId = ui.ReplaceForGroup = null;
            // Kendi kararımız: grup bizim komutumuzla kalktı; Refresh bunu "gitti" saymasın
            announcedKey = null;
            if (station != null && before != null) station.Bind(before, true);
            StartSequence(choice);
            return true;
        }

        void Deny(StampChoice choice)
        {
            Play(AdmissionSound.Deny);
            foreach (var s in stamps) if (s != null && s.Choice == choice) s.ReturnToRack(true);
        }

        /// <summary>Damga izinin satırları (çevrilmiş; sayılar dengeden): tek kaynak <see cref="AdmissionSnapshotBuilder.ImpressionLines"/>.</summary>
        public static string[] ImpressionLines(StampChoice choice, Balance b) => AdmissionSnapshotBuilder.ImpressionLines(choice, b);

        /// <summary>İzin rengi (<c>admission.stampMarks.{seçim}.color</c>, "#RRGGBB"); tema yoksa null. Tek kaynak <see cref="AdmissionSnapshotBuilder.StampColorHex"/>.</summary>
        public static string ImpressionColor(StampChoice choice) => AdmissionSnapshotBuilder.StampColorHex(choice);

        void StartSequence(StampChoice choice)
        {
            sequence = choice == StampChoice.Admit ? Sequence.Admit : Sequence.Out;
            sequenceChoice = choice;
            timeline = DecisionTimeline.From(SiginakTheme.Active);
            sequenceT = openedAt = closedAt = 0;
            cameraMoved = doorOpened = leafSounded = doorClosed = false;
            stationDone = station == null;
            int id = ++sequenceId;
            if (rig != null && !rig.Locked) { rig.Locked = true; lockedByMe = true; }
            if (door != null && sequence == Sequence.Admit) door.SetKnocking(false);
            ApplyInteractive();   // dizi: hepsi etkileşimsiz
            if (station != null) station.PlayDecision(choice, () => { if (id == sequenceId) stationDone = true; });
            // Komut basışta yazıldı: istasyon dizisi bitince uygulanacak son görüntü komuttan SONRAKİ durumdur (aday yok). Dizi
            // sürerken Refresh susar; bu bağlanmazsa FinishSequence basıştan önceki görüntüyü (grup, tezgâh İNİK) uygular: tezgâh
            // katlanıştan sonra yeniden iner, kartlar geri gelir. Dizi sırasında Bind yalnız son görüntüyü saklar.
            if (station != null && station.SequenceActive)
            {
                var after = AdmissionSnapshotBuilder.Build(Sim, ui);
                if (after != null) station.Bind(after, true);
            }
            // İstasyon dizisi anında bittiyse (istasyon yok ya da modeli bağlı değil: kabuk/test) GERİ ÇEVİR'in de ilk adımı bu karede biter
            if (sequence == Sequence.Out && stationDone) EndSequence(true);
        }

        void StepSequence(float dt)
        {
            sequenceT += dt;
            if (sequence == Sequence.Out)
            {
                if (stationDone) EndSequence(true);
                return;
            }
            var t = timeline;
            if (!cameraMoved && sequenceT >= t.DocsS)
            {
                cameraMoved = true;
                if (rig != null && rig.Current != SiginakPose.Door) rig.Focus(SiginakPose.Door);
            }
            if (!doorOpened && stationDone && sequenceT >= t.OpenAtS)
            {
                doorOpened = true;
                openedAt = sequenceT;
                // Koridor boş: ziyaretçi listesi yok (siluet gizli)
                if (door != null) door.SetOpen(true, null, 1, false, t.WheelS, t.LeafS);
            }
            if (doorOpened && !leafSounded && sequenceT >= openedAt + t.WheelS)
            {
                leafSounded = true;
                Play(AdmissionSound.Open);
            }
            if (doorOpened && !doorClosed && sequenceT >= openedAt + t.WheelS + t.LeafS + t.HoldS)
            {
                doorClosed = true;
                closedAt = sequenceT;
                if (door != null) door.SetOpen(false);
                Play(AdmissionSound.Close);
            }
            if (doorClosed && (door == null || !door.Moving || sequenceT >= closedAt + t.CloseCapS)) EndSequence(true);
        }

        /// <summary>Dizi sona atlar (Esc, sağ tık, aileden çıkış): kapı kapalı, kamera dizinin son pozunda, istasyon son durumunda.</summary>
        void Skip()
        {
            if (sequence == Sequence.None) return;
            bool admit = sequence == Sequence.Admit;
            if (admit && door != null) door.ResetClosed();
            if (admit && rig != null && rig.Current != SiginakPose.Door && InFamily(rig.Current)) rig.Snap(SiginakPose.Door);
            EndSequence(false);
        }

        void EndSequence(bool animate)
        {
            if (sequence == Sequence.None) return;
            sequence = Sequence.None;
            sequenceId++;
            if (lockedByMe && rig != null) rig.Locked = false;
            lockedByMe = false;
            ReturnStamps(false);
            Render(animate);
        }

        void ReturnStamps(bool animate)
        {
            foreach (var s in stamps) if (s != null) s.ReturnToRack(animate);
        }

        // ------------------------------------------------------------------ teleks

        /// <summary>"KAPI" satırı kapanışın "VARDİYA DEVREDİLDİ"sinden sonra basılır (dosyalama karanlığı bitince).</summary>
        void FlushTelex()
        {
            if (pendingTelex.Count == 0 || desk != null && desk.ShiftFiling) return;
            int shift = surface != null ? surface.Shift : (Sim?.S.Day ?? 0) + 1;
            foreach (var (title, body) in pendingTelex)
            {
                if (telex != null) telex.Post(shift, title, body, TelexKind.Info);
                TelexLine?.Invoke(title, body);
            }
            pendingTelex.Clear();
        }

        // ------------------------------------------------------------------ ses

        void Play(AdmissionSound sound)
        {
            Sounded?.Invoke(sound);
            Sfx.Play(CueOf(sound));
        }

        /// <summary>
        /// Kabul sesi → ses olayı (PLAN §1.13; klibi olmayan ses sessiz kalır). Her değer açık yazılır: yeni bir ses <c>default</c>'a
        /// düşüp başka bir ses çalmasın.
        /// </summary>
        public static Cue CueOf(AdmissionSound sound)
        {
            switch (sound)
            {
                case AdmissionSound.CounterDown: return Cue.AdmissionCounterDown;
                case AdmissionSound.Drawer: return Cue.AdmissionDrawer;
                case AdmissionSound.StampLift: return Cue.AdmissionStampLift;
                case AdmissionSound.CounterFold: return Cue.AdmissionCounterFold;
                case AdmissionSound.StampPress: return Cue.StampHeavy;
                case AdmissionSound.StampReturn: return Cue.BoardReturn;
                case AdmissionSound.Deny: return Cue.BoardDeny;
                case AdmissionSound.DocDrop: return Cue.PaperSlide;
                case AdmissionSound.Knock: return Cue.DoorKnock;
                case AdmissionSound.Open: return Cue.DoorOpen;
                case AdmissionSound.Close: return Cue.DoorClose;
                default: return Cue.BoardDeny;
            }
        }
    }

    /// <summary>
    /// Kabul dizisinin zaman çizelgesi (PLAN §1.11; süreler <c>admission.motion</c> ve <c>_local_a6</c>), t = 0 basışın sonu: aday kâğıtları
    /// çekmeceye (<see cref="DocsS"/> = <c>drawerS</c>), kamera K8'e, tezgâh katlanır (<see cref="FoldS"/> = lamba + kanat + konsollar),
    /// kanat açılmadan önce katlanış biter (<see cref="OpenAtS"/>); çark <see cref="WheelS"/>, kanat <see cref="LeafS"/>, açık bekleme
    /// <see cref="HoldS"/>, sonra kapanış.
    /// </summary>
    public readonly struct DecisionTimeline
    {
        public readonly float DocsS, FoldS, WheelS, LeafS, HoldS;
        /// <summary>Kapanış başladıktan sonra kapının oturmasını bekleme sınırı (kapı bağlı değilse ya da hiç durmazsa).</summary>
        public readonly float CloseCapS;

        public DecisionTimeline(float docsS, float foldS, float wheelS, float leafS, float holdS, float closeCapS)
        {
            DocsS = docsS; FoldS = foldS; WheelS = wheelS; LeafS = leafS; HoldS = holdS; CloseCapS = closeCapS;
        }

        /// <summary>Çark dönmeye başlar: kâğıtlar çekmecede ve tezgâh katlı (kanat açılırken tezgâh katlı, R9_open; eleştiri 3).</summary>
        public float OpenAtS => DocsS + FoldS;
        /// <summary>Kanat kapanmaya başlar.</summary>
        public float CloseAtS => OpenAtS + WheelS + LeafS + HoldS;

        public static DecisionTimeline From(SiginakTheme theme)
        {
            float N(string key, float fallback) => theme != null ? theme.NumberOf(key, fallback) : fallback;
            float fold = N("admission.motion.lampS", 0.08f) + N("admission.motion.counterFoldS", 0.35f) + N("admission.motion.counterBracketS", 0.15f);
            // Kapanış: kanat 0,6 s + çark; kapı bağlıysa durması beklenir, bu yalnız üst sınır
            float cap = N("motion.doorLeafS", 0.8f) + N("motion.doorWheelS", 0.5f);
            return new DecisionTimeline(N("admission.motion.drawerS", 0.35f), fold, N("admission.motion.admitWheelS", 0.5f),
                N("admission.motion.admitLeafS", 0.8f), N("admission.motion.admitHoldS", 0.6f), cap);
        }
    }
}
