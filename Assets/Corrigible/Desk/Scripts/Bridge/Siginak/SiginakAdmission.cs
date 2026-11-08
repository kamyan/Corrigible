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
    /// Kapı ve kabul akışı (Revizyon 3 · Aşama 6, R9/R17; PLAN §1.3–§1.5, §1.11, §1.13, §1.14): K8/K8A/K8G/K8R pozları ve geri zinciri,
    /// vuruş algısı (canlı oyunda aday değişince bir kez; yüklemede yok), tezgâh durumu, araç ve damga olaylarından Core komutları
    /// (<c>GameController.ExecuteBoardCommand</c>; yeni komut yok), kabul dizisi (kapı), sesler, "TALİMAT n" teleksi, görülen sürüm
    /// (<see cref="SiginakDeskStore"/>), kayıttan devamda animasyonsuz kurulum. Kartlı oyunda kapının tek sahibi.
    /// <para>
    /// Simülasyonu yalnız iki var olan komutla değiştirir: <c>InspectApplicant</c> (araçlar) ve <c>DecideApplicant</c> (damgalar). Her
    /// komuttan önce Core'un kendi denetimi okunur (<c>ApplicantInspectionCheck</c>, <c>ApplicantDecisionCheck</c>); ret komutsuz bir
    /// <see cref="AdmissionSound.Deny"/>'dır. Görünümler (<see cref="AdmissionStation"/>, araçlar, damgalar) yalnız anlık görüntüyü
    /// (<see cref="AdmissionSnapshotBuilder"/>) çizer ve olay yayar; ses burada <see cref="CueOf"/> ile çalınır.
    /// </para>
    /// <para>
    /// Sunum durumu (<see cref="AdmissionUi"/>: belge tezgâhta mı, görülen talimat sürümü, görülen dış kayıt sonucu) kayda girmez,
    /// <c>liberation_desk.json</c>'a <see cref="SiginakDesk.SaveNotes"/> ile yazılır. Yeni oyunda ve yan dosyasız devamda güncel
    /// sürüm görülmüş sayılır (v1 hiç "yeni" değildir; eleştiri 8). Talimat değişimi ve dış kayıt sonucu ilk K8/K8A varışında bir
    /// kez oynar (D12).
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
        [Tooltip("Talimat değişiminin satırı (TALİMAT n), kapanış teleksinden sonra.")]
        [SerializeField] TelexFeed telex;
        [Header("Kapı")]
        [Tooltip("Kanat, çark, vuruş lambası, merdiven ışığı (kabul dizisi kapıyı burada açar).")]
        [SerializeField] DoorProp door;
        [Tooltip("Kapının eski denetleyicisi: kartlı oyunda Visit/Reopen buraya gelir, Busy buradan okunur.")]
        [SerializeField] SiginakDoor doorController;
        [Header("Kabul istasyonu")]
        [SerializeField] AdmissionStation station;
        [SerializeField] ColdGlassView glass;
        [SerializeField] IntercomTool intercom;
        [SerializeField] MagnifierTool magnifier;
        [SerializeField] RequestPadTool pad;
        [Tooltip("Dört karar damgası (sıra önemsiz; her biri kendi seçimini taşır).")]
        [SerializeField] StampDecision[] stamps = Array.Empty<StampDecision>();
        [Header("Oda")]
        [Tooltip("Görülen durum masanın yan dosyasına yazılır (SaveNotes); teleks satırı dosyalama bitince basılır.")]
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
        /// <summary>Bu sunum durumunun ait olduğu kayıt (yükleme / yeni oyun sırasında erken yan dosya yazımına karşı).</summary>
        GameRecord uiOf;
        AdmissionSnapshot last;
        string announcedId;
        int protocolEvents;
        bool commanding, glassPending, glassFog, glassOpen, lockedByMe;
        SiginakPose glassReturn = SiginakPose.Admission;
        int escapeFrame = -10;
        RightButtonGesture right;

        Sequence sequence;
        StampChoice sequenceChoice;
        DecisionTimeline timeline;
        int sequenceId;
        float sequenceT, openedAt, closedAt;
        bool stationDone, cameraMoved, doorOpened, leafSounded, doorClosed;

        Simulation Sim => game != null ? game.Sim : null;
        string CandidateId => Sim?.Admission?.Candidate?.Resident?.Id;
        /// <summary>Görülmemiş talimat (kurucuyla aynı kural: 0 = bilinmiyor, yeni sayılmaz).</summary>
        bool OrdersUnseen => Sim?.Admission?.Protocol != null && ui.SeenOrdersVersion > 0 && ui.SeenOrdersVersion < Sim.Admission.Protocol.Version;

        /// <summary>Bir dizi oynuyor (masa girdisi kapalı).</summary>
        public bool Busy => sequence != Sequence.None;
        /// <summary>Kamera K8 ailesinde ve kabul kartlı oyunda var.</summary>
        public bool Active => rig != null && InFamily(rig.Current) && Sim?.Board != null && Sim.Admission != null;
        /// <summary>Sunum durumu (belge tezgâhta, görülen sürüm, görülen sonuç).</summary>
        public AdmissionUi Ui => ui;
        /// <summary>Son çizilen anlık görüntü (testler ve sahne denetimleri için).</summary>
        public AdmissionSnapshot Last => last;
        public SiginakDoor DoorController => doorController;
        public PlanningBoardPresenter PlanningBoard => planningBoard;

        /// <summary>Kapanış teleksine satır (başlık, gövde): "TALİMAT n" + değişiklik metni.</summary>
        public event Action<string, string> TelexLine;
        /// <summary>Çalınan her ses olayı (testler; ses <see cref="Sfx"/> ile ayrıca çalar).</summary>
        public event Action<AdmissionSound> Sounded;

        /// <summary>K8 ailesi: kapı (K8), tezgâh (K8A), buğu camı (K8G), okuma (K8R).</summary>
        public static bool InFamily(SiginakPose pose) => pose == SiginakPose.Door || pose == SiginakPose.Admission ||
            pose == SiginakPose.BreathGlass || pose == SiginakPose.AdmissionRead;

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
                station.DocumentTaken += DocumentTaken;
                station.Sound += Play;
                if (station.Tag == null && hoverTag != null) station.Tag = hoverTag;
            }
            if (magnifier != null) { magnifier.Used += MagnifierUsed; magnifier.Sound += Play; }
            if (intercom != null) { intercom.Used += IntercomUsed; intercom.Sound += Play; }
            if (glass != null) { glass.Used += GlassUsed; glass.Clicked += GlassClicked; glass.Sound += Play; }
            if (pad != null) { pad.Used += PadUsed; pad.PaperClicked += PadPaperClicked; pad.Sound += Play; }
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
                station.DocumentTaken -= DocumentTaken;
                station.Sound -= Play;
            }
            if (magnifier != null) { magnifier.Used -= MagnifierUsed; magnifier.Sound -= Play; }
            if (intercom != null) { intercom.Used -= IntercomUsed; intercom.Sound -= Play; }
            if (glass != null) { glass.Used -= GlassUsed; glass.Clicked -= GlassClicked; glass.Sound -= Play; }
            if (pad != null) { pad.Used -= PadUsed; pad.PaperClicked -= PadPaperClicked; pad.Sound -= Play; }
            foreach (var s in stamps) if (s != null) { s.Pressed -= StampPressed; s.Sound -= Play; }
            if (deskEdge != null) { deskEdge.Clicked -= ClickDeskKey; deskEdge.BlockedOverride = null; }
            EndSequence(false);
            right.Reset();
        }

        void MagnifierUsed() => UseTool(AdmissionTool.Papers);
        void IntercomUsed() => UseTool(AdmissionTool.Interview);
        void GlassUsed() => UseTool(AdmissionTool.Breath);
        void PadUsed() => UseTool(AdmissionTool.Record);
        void StampPressed(StampChoice choice) => PressStamp(choice);
        void PadPaperClicked(AdmissionPaper kind) => ClickPaper(kind, 0);

        /// <summary>K8G'de cama tık (kullanılmış araç da olsa): geldiği poza döner (PLAN §1.5; W2 <c>Clicked</c> her tıkta gelir).</summary>
        void GlassClicked()
        {
            if (sequence != Sequence.None || rig == null) return;
            if (rig.Current == SiginakPose.BreathGlass) { rig.Focus(glassReturn); return; }
            // K8/K8A'da kullanılmış ya da kilitli cam: komutsuz ret sesi (öteki araçlar kendi Deny'larını çalar; cam yalnız Clicked yayar)
            var breath = last?.Tools[(int)AdmissionTool.Breath];
            if (Interactive && breath != null && !breath.Enabled) Play(AdmissionSound.Deny);
        }

        /// <summary>
        /// Üzerine gelme etiketi: sahne bağı tek (bu bileşende); bağı boş görünümlere verilir. Buğu camı ve interkom istasyonun dışındadır
        /// (kapı kanadı ve kasası), etiketi yalnız buradan alırlar (W2 sözleşmesi).
        /// </summary>
        void ShareHoverTag()
        {
            var tag = hoverTag != null ? hoverTag : station != null ? station.Tag : null;
            if (tag == null) return;
            if (glass != null && glass.Tag == null) glass.Tag = tag;
            if (intercom != null && intercom.Tag == null) intercom.Tag = tag;
            if (magnifier != null && magnifier.Tag == null) magnifier.Tag = tag;
            if (pad != null && pad.Tag == null) pad.Tag = tag;
            foreach (var s in stamps) if (s != null && s.Tag == null) s.Tag = tag;
        }

        /// <summary>
        /// Tıklanabilirlik: istasyon (damgalar, büyüteç, defter dahil) ve istasyon dışındaki iki araç (buğu camı, interkom) aynı kurala
        /// uyar — K8 ailesinde, dizi yokken, oda kilitli değilken. Aile dışında çarpıştırıcıları kapalıdır: K1'den kapıya tık kapının
        /// kendi hedefine düşer.
        /// </summary>
        void ApplyInteractive()
        {
            bool on = Interactive;
            if (station != null)
            {
                station.SetReading(rig != null && rig.Current == SiginakPose.AdmissionRead);
                station.SetInteractive(on);
            }
            if (glass != null) glass.SetInteractive(on);
            if (intercom != null) intercom.SetInteractive(on);
        }

        /// <summary>Masa kenarı yalnız K8 ailesinde (ve oda kilitli değilken) tıklanır.</summary>
        bool DeskEdgeBlocked() => !Active || Busy || DeskTarget.Blocked != null && DeskTarget.Blocked();

        void Update()
        {
            if (Sim?.Board != null && door != null)
            {
                // Vuruş lambası kartlı oyunda burada: aday varken amber (vuruş parlamaları Knock'ta), yoksa sönük. Eski denetleyicinin
                // yükleme sıfırlaması (ResetClosed) bir kare sonra düzelir.
                bool waiting = CandidateId != null && Sim.S.Ending == null;
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
        /// Diziyi, bekleyen buğu oynatımını ve teleks satırını <paramref name="dt"/> kadar ilerletir (edit modda testler sürer). Kapının
        /// kendi hareketini <see cref="DoorProp"/> sürer.
        /// </summary>
        public void Advance(float dt)
        {
            if (dt < 0) dt = 0;
            if (sequence != Sequence.None) StepSequence(dt);
            if (glassPending && rig != null && rig.Current == SiginakPose.BreathGlass && !rig.InTransition)
            {
                glassPending = false;
                glassOpen = true;
                if (glass != null) glass.Play(glassFog, null);
            }
            FlushTelex();
        }

        // ------------------------------------------------------------------ durum

        /// <summary>Yeni oyun: v1 görülmüş, aday yok sayılır; vuruş yok.</summary>
        void NewGame()
        {
            EndSequence(false);
            ResetPresentation();
            var sim = Sim;
            uiOf = sim?.Record;
            ui.Reset();
            if (sim?.Admission != null)
            {
                ui.SeenOrdersVersion = sim.Admission.Protocol.Version;
                ui.SeenResultFor = ArrivedResultFor(sim);
                protocolEvents = ProtocolEvents(sim);
            }
            announcedId = CandidateId;
            Render(false);
            SnapMagnifier();
        }

        /// <summary>
        /// Kayıttan devam (ve açılış): sunum durumu yan dosyadan (yoksa ya da oynanmamış oyunda yeni oyun kuralıyla), bekleyen aday
        /// duyurulmuş sayılır (vuruş çalmaz, D12), istasyon animasyonsuz son durumuna oturur.
        /// </summary>
        public void Restore()
        {
            EndSequence(false);
            ResetPresentation();
            var sim = Sim;
            uiOf = sim?.Record;
            ui.Reset();
            if (sim?.Admission != null)
            {
                bool played = sim.Record != null && sim.Record.commands.Count > 0;
                if (!(played && ui.Apply(SiginakDeskStore.LoadAdmissionSeen(sim.Record))))
                {
                    ui.SeenOrdersVersion = sim.Admission.Protocol.Version;
                    ui.SeenResultFor = ArrivedResultFor(sim);
                }
                protocolEvents = ProtocolEvents(sim);
            }
            announcedId = CandidateId;
            pendingTelex.Clear();
            Render(false);
            SnapMagnifier();
        }

        /// <summary>
        /// Yeni oyun ve yan dosyasız yükleme için <see cref="AdmissionUi.SeenResultFor"/> başlangıcı (PLAN §1.2, D12): kapıdaki adayın dış
        /// kayıt sonucu zaten geldiyse görülmüş sayılır (yüklemede ağızdan çıkış animasyonu yok, fiş statik); gelmediyse null — sonuç
        /// sonraki kapanışta gelince ilk K8/K8A varışına dek BEKLENİYOR kalır (§1.8). Adayı koşulsuz "görülmüş" saymak bekleyen talebin
        /// sonucunu K1'de çıkarırdı.
        /// </summary>
        string ArrivedResultFor(Simulation sim)
        {
            var id = CandidateId;
            return id != null && sim.AdmissionFindingCode(id, "record") != null ? id : null;
        }

        /// <summary>
        /// Oyun değişti (<c>GameController.Changed</c>): yeni aday bir kez vurur (tezgâh iner), yeni talimat teleks satırını bekletir,
        /// istasyon yeniden çizilir. Kendi komutlarımızın ve dizilerin sırasında çizim ertelenir (sunum kendi sırasıyla gelir).
        /// </summary>
        public void Refresh()
        {
            if (commanding || sequence != Sequence.None) return;
            var sim = Sim;
            if (sim == null) return;
            if (!ReferenceEquals(sim.Record, uiOf)) { Restore(); return; }
            if (sim.Admission != null)
            {
                var id = sim.S.Ending == null ? CandidateId : null;
                if (id != null && id != announcedId)
                {
                    announcedId = id;
                    if (door != null) door.Knock();
                    Play(AdmissionSound.Knock);
                }
                int protocols = ProtocolEvents(sim);
                if (protocols > protocolEvents)
                {
                    var protocol = sim.Admission.Protocol;
                    pendingTelex.Add((Loc.F("TALİMAT {0}", protocol.Version), Loc.T(protocol.Change)));
                }
                protocolEvents = protocols;
            }
            Render(true);
        }

        void Relabel() => Render(false);

        static int ProtocolEvents(Simulation sim) => sim.Admission.Events.Count(e => e.Kind == "protocol");

        void ResetPresentation()
        {
            glassPending = glassOpen = false;
            glassReturn = SiginakPose.Admission;
            right.Reset();
            ReturnStamps(false);
            // Kayıttan devam / yeni oyun: süren araç hareketleri animasyonsuz biter (W2 sözleşmesi 6)
            if (intercom != null) intercom.Stop();
            if (glass != null) glass.ResetClosed();
        }

        /// <summary>
        /// Kayıttan devamda büyüteç dinlenme pozundadır: tasarımda büyüteç yalnız karşılaştırma anında belgenin üstündedir (R17_inspect,
        /// R17_read); sonraki durumlarda (R17_pending, R17_stamped) cetvel dinlenme yerinde, kıskaçtaki fişleri örtmez.
        /// </summary>
        void SnapMagnifier()
        {
            if (magnifier != null) magnifier.ReturnToRest(false);
        }

        /// <summary>Başka bir araç ya da damga işe girince büyüteç dinlenmeye döner (R17_pending / R17_stamped).</summary>
        void RestMagnifier()
        {
            if (magnifier == null) return;
            var st = magnifier.State;
            if (st == MagnifierTool.MagnifierState.Inspect || st == MagnifierTool.MagnifierState.ToInspect) magnifier.ReturnToRest(true);
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
            if (magnifier != null && s.Tools[(int)AdmissionTool.Papers] != null) magnifier.Bind(s.Tools[(int)AdmissionTool.Papers]);
            if (intercom != null && s.Tools[(int)AdmissionTool.Interview] != null) intercom.Bind(s.Tools[(int)AdmissionTool.Interview]);
            if (glass != null && s.Tools[(int)AdmissionTool.Breath] != null) glass.Bind(s.Tools[(int)AdmissionTool.Breath]);
            if (pad != null)
            {
                if (s.Tools[(int)AdmissionTool.Record] != null) pad.Bind(s.Tools[(int)AdmissionTool.Record]);
                // Yaprak GİDEN'de: talep gönderildi ve sonuç (görülmüş olarak) henüz gelmedi = kıskaçta BEKLENİYOR fişi var (W4 sözleşmesi c)
                if (s.Request != null)
                {
                    bool outgoing = s.Request.Sent && s.Slips.Any(x => x != null && x.Tool == AdmissionTool.Record && x.Kind == SlipVerdict.Pending);
                    pad.Bind(s.Request, outgoing);
                    // Animasyonsuz bağlama (kayıttan devam, dil değişimi): yaprak uçmaz, GİDEN'de oturur. Defterin ilk bağlaması
                    // talepsiz bir anlık görüntüyle tükenmiş olabilir; o zaman Bind gönderimi animasyonla başlatırdı.
                    if (!animate && outgoing && pad.State != RequestPadTool.PadState.Outgoing) pad.SnapOutgoing();
                }
            }
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

        /// <summary>
        /// Yan dosyaya yazılacak görülen durum: <paramref name="record"/> bu durumun kaydıysa bellekteki, değilse dosyadaki (yükleme
        /// sırasında başka bir dinleyici <see cref="SiginakDesk.SaveNotes"/>'u erken çağırırsa önceki oyunun durumu yazılmasın).
        /// </summary>
        public SiginakDeskStore.AdmissionSeen SeenFor(GameRecord record) =>
            record != null && ReferenceEquals(record, uiOf) ? ui.ToSeen() : SiginakDeskStore.LoadAdmissionSeen(record);

        void SaveNotes() { if (desk != null) desk.SaveNotes(); }

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

        /// <summary>Sürüklemesiz sağ tık: bir seviye geri (damga → dizi → K8R → K8A → K8 → K1; K8G geldiği poza).</summary>
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
                case SiginakPose.BreathGlass: rig.Focus(glassReturn); return true;
                case SiginakPose.Admission: rig.Focus(SiginakPose.Door); return true;
                case SiginakPose.Door: Leave(); return true;
                default: return false;
            }
        }

        void PoseChanged(SiginakPose pose)
        {
            if (!InFamily(pose))
            {
                glassPending = false;
                if (glassOpen) { glassOpen = false; if (glass != null) glass.Close(); }
                ReturnStamps(false);
                if (sequence != Sequence.None) Skip();
                // Aileden çıkış: görülecek bir şey yoksa tezgâh şimdi katlanır (T4)
                Render(true);
                return;
            }
            if (pose != SiginakPose.BreathGlass)
            {
                glassPending = false;
                if (glassOpen) { glassOpen = false; if (glass != null) glass.Close(); }
            }
            ApplyInteractive();
        }

        /// <summary>
        /// Kamera poza oturdu. K8 ya da K8A'ya ilk varışta görülmemiş talimat değişimi oynar (eski şerit ESKİ cebine, yenisi yuvaya,
        /// eşleşen dosya varsa İNCELEME FİŞİ) ve gelmiş dış kayıt sonucunun fişi ağızdan bir kez çıkar (PLAN §1.8, §1.9).
        /// </summary>
        public void OnArrived(SiginakPose pose)
        {
            ApplyInteractive();
            if (pose != SiginakPose.Door && pose != SiginakPose.Admission) return;
            var sim = Sim;
            if (sim?.Admission == null || sequence != Sequence.None) return;
            bool changed = false;
            if (OrdersUnseen)
            {
                var s = last ?? AdmissionSnapshotBuilder.Build(sim, ui);
                if (station != null && s != null)
                    station.PlayOrdersChange(s.Archive.Count > 0 ? s.Archive[0] : null, s.Orders, s.Slips.FirstOrDefault(x => x != null && x.Review));
                ui.SeenOrdersVersion = sim.Admission.Protocol.Version;
                changed = true;
            }
            var id = CandidateId;
            if (id != null && ui.SeenResultFor != id && sim.AdmissionFindingCode(id, "record") != null)
            {
                ui.SeenResultFor = id;
                changed = true;
            }
            if (!changed) return;
            SaveNotes();
            Render(true);
        }

        // ------------------------------------------------------------------ istasyon girdileri (olayların karşılığı)

        /// <summary>Tezgâha tık: K8'de inik tezgâh K8A'ya götürür; K8R'de okumayı bitirir.</summary>
        public void ClickCounter()
        {
            if (!Interactive) return;
            if (rig.Current == SiginakPose.AdmissionRead) { rig.Focus(SiginakPose.Admission); return; }
            if (rig.Current != SiginakPose.Admission && CounterDown) rig.Focus(SiginakPose.Admission);
        }

        /// <summary>
        /// Kâğıda tık. K8A'da çekmecedeki belge tezgâha alınır (T3), tezgâhtaki kâğıt K8R'de okunur (çapa kâğıda göre); K8R'de ikinci
        /// tık K8A'ya döner; K8 / K8G'de tezgâha tık gibi.
        /// </summary>
        public void ClickPaper(AdmissionPaper kind, int index)
        {
            if (!Interactive) return;
            var pose = rig.Current;
            if (pose == SiginakPose.AdmissionRead) { rig.Focus(SiginakPose.Admission); return; }
            if (pose != SiginakPose.Admission) { if (CounterDown) rig.Focus(SiginakPose.Admission); return; }
            var id = CandidateId;
            if (kind == AdmissionPaper.Document && id != null && !ui.DocumentOnCounter(id))
            {
                TakeDocument(id);
                SaveNotes();
                Render(true);
                return;
            }
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

        /// <summary>
        /// Tezgâh inik mi: istasyon varsa onun durumu, yoksa (testler) Core'dan aynı kural (aday var ya da görülmemiş talimat).
        /// </summary>
        bool CounterDown => station != null ? station.Counter == CounterState.Down : CandidateId != null || OrdersUnseen;

        static float ReadDistance => Num("admission.readDistanceM", 0.318f);

        static float Num(string key, float fallback) => SiginakTheme.Active != null ? SiginakTheme.Active.NumberOf(key, fallback) : fallback;

        void TakeDocument(string candidateId)
        {
            ui.DocumentOnCounterFor = candidateId;
            if (station != null) station.TakeDocument();
        }

        /// <summary>
        /// İstasyon belgeyi çekmeceden kendisi aldı (çekmecedeki belgeye tık, incelemenin T3'ü): sunum durumu ve yan dosya (P15: kayıttan
        /// devamda belge tezgâhta).
        /// </summary>
        void DocumentTaken()
        {
            var id = CandidateId;
            if (id == null || ui.DocumentOnCounter(id)) return;
            ui.DocumentOnCounterFor = id;
            SaveNotes();
            // K8'den çekmecedeki belgeye tık: belge tezgâha, kamera tezgâha (K8A)
            if (rig != null && rig.Current == SiginakPose.Door && Interactive) rig.Focus(SiginakPose.Admission);
        }

        // ------------------------------------------------------------------ araçlar → InspectApplicant

        /// <summary>
        /// Bir aracın kullanımı: tam bir <c>inspectApplicant</c> komutu (Core denetimi önce; ret komutsuz <c>Deny</c>). İlk araç belgeyi
        /// çekmeceden tezgâha alır. Başarıda aracın hareketi ve fişin ağızdan çıkışı; SOĞUK GÖZLEM'de kamera K8G'ye gider, kapak ve
        /// buğu varışta oynar. K8G'de cama ikinci tık geldiği poza döner. İşlediyse true.
        /// </summary>
        public bool UseTool(AdmissionTool tool)
        {
            if (tool == AdmissionTool.Breath && rig != null && rig.Current == SiginakPose.BreathGlass && sequence == Sequence.None)
            {
                rig.Focus(glassReturn);
                return false;
            }
            if (!CanAct) return false;
            var sim = Sim;
            string kind = AdmissionIds.KindOf(tool);
            if (sim.ApplicantInspectionCheck(kind) != null) { Play(AdmissionSound.Deny); return false; }
            var id = CandidateId;
            bool took = !ui.DocumentOnCounter(id);
            if (took) TakeDocument(id);
            var result = Command(s => s.InspectApplicant(kind));
            if (result == null || !result.Ok)
            {
                Play(AdmissionSound.Deny);
                Render(true);
                if (took) SaveNotes();
                return false;
            }
            if (tool != AdmissionTool.Papers) RestMagnifier();
            var snap = AdmissionSnapshotBuilder.Build(sim, ui);
            var slip = snap?.Slips.LastOrDefault(x => x != null && x.Tool == tool);
            switch (tool)
            {
                case AdmissionTool.Papers:
                    if (magnifier != null) magnifier.PlayInspect(sim.AdmissionFindingCode(id, "papers"), null);
                    break;
                case AdmissionTool.Interview:
                    // LED hat açıkken yanar: cızırtı + gövdenin harf harf dökümü (fiş yüzü aynı süreyle döker)
                    if (intercom != null) intercom.Play(IntercomTool.CrackleS + IntercomTool.TranscriptSeconds(slip?.Body), null);
                    break;
                case AdmissionTool.Breath:
                    glassReturn = rig.Current == SiginakPose.Door ? SiginakPose.Door : SiginakPose.Admission;
                    glassFog = sim.AdmissionFindingCode(id, "breath") == "fog";
                    glassPending = true;
                    rig.Focus(SiginakPose.BreathGlass);
                    break;
                case AdmissionTool.Record:
                    if (pad != null) pad.PlaySend(null);
                    break;
            }
            // Fiş ağızdan bir kez çıkar (anahtarına göre; istasyon aynı anahtarı Bind'de yeniden oynatmaz)
            if (station != null && slip != null) station.PlayInspection(tool, slip);
            Render(true);
            if (took) SaveNotes();
            return true;
        }

        ActResult Command(Func<Simulation, ActResult> command)
        {
            commanding = true;
            try { return game.ExecuteBoardCommand(command); }
            finally { commanding = false; }
        }

        // ------------------------------------------------------------------ damgalar → DecideApplicant ve diziler

        /// <summary>
        /// Damga belgeye basıldı: tam bir <c>decideApplicant</c> komutu (Core denetimi önce; ret komutsuz <c>Deny</c>, damga rafına).
        /// Başarıda iz belgede kalır ve karar dizisi başlar (PLAN §1.11): KABUL / KISITLI'da belge ve fişler çekmeceye, kamera K8'e,
        /// tezgâh katlanır, sonra çark, kanat, bekleme, kapanış; REDDET ve YARDIM'da istasyonun çekmece dizisi. İşlediyse true.
        /// </summary>
        public bool PressStamp(StampChoice choice)
        {
            if (!CanAct) { Deny(choice); return false; }
            var sim = Sim;
            string id = AdmissionIds.ChoiceOf(choice);
            if (sim.ApplicantDecisionCheck(id) != null) { Deny(choice); return false; }
            // Komuttan önceki görüntü: aday ve belgesi; iz bunun üstüne basılır (komuttan sonra aday yoktur)
            var before = AdmissionSnapshotBuilder.Build(sim, ui);
            if (before != null) AdmissionSnapshotBuilder.Stamp(before.Document, choice, sim.B);
            var result = Command(s => s.DecideApplicant(id));
            if (result == null || !result.Ok) { Deny(choice); Render(true); return false; }
            if (station != null && before != null) station.Bind(before, true);
            RestMagnifier();
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
            sequence = choice == StampChoice.Admit || choice == StampChoice.Restricted ? Sequence.Admit : Sequence.Out;
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
            // sürerken Refresh susar; bu bağlanmazsa FinishSequence basıştan önceki görüntüyü (aday, tezgâh İNİK) uygular: tezgâh
            // katlanıştan sonra yeniden iner, belge ve fişler geri gelir. Dizi sırasında Bind yalnız son görüntüyü saklar (+ şerit).
            if (station != null && station.SequenceActive)
            {
                var after = AdmissionSnapshotBuilder.Build(Sim, ui);
                if (after != null) station.Bind(after, true);
            }
            // İstasyon dizisi anında bittiyse (istasyon yok ya da modeli bağlı değil: kabuk/test) REDDET / YARDIM'ın da ilk adımı bu karede biter
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

        /// <summary>"TALİMAT n" satırı kapanışın "VARDİYA DEVREDİLDİ"sinden sonra basılır (dosyalama karanlığı bitince).</summary>
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
                case AdmissionSound.TinsOut: return Cue.AdmissionDrawer;
                case AdmissionSound.Marker: return Cue.AdmissionMarker;
                case AdmissionSound.SlipOut: return Cue.AdmissionSlip;
                case AdmissionSound.IntercomCrackle: return Cue.AdmissionIntercom;
                case AdmissionSound.GlassCover: return Cue.AdmissionGlass;
                case AdmissionSound.Fog: return Cue.AdmissionGlass;
                case AdmissionSound.PadTear: return Cue.AdmissionPadTear;
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
    /// Kabul dizisinin zaman çizelgesi (PLAN §1.11; süreler <c>admission.motion</c> ve <c>_local_a6</c>), t = 0 basışın sonu: belge ve fişler
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

        /// <summary>Çark dönmeye başlar: belge çekmecede ve tezgâh katlı (kanat açılırken tezgâh katlı, R9_open; eleştiri 3).</summary>
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
