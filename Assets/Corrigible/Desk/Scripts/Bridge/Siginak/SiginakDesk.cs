using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Raid;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Sığınak masasının oyun bağlantısı (Docs/UI_Siginak Aşama 4): sayaç rafı, anahtar rayı, vardiya şalteri, teleks,
    /// kenar satırı, ATLA plakası, odadaki tıklanır nesneler ve umut işaretleri. Simülasyonu yalnız okur; komutlar
    /// <see cref="LiberationDeskView"/>'in yöntemlerinden geçer. Yalnız yeni görünümde etkindir (sığınak kökünün altında).
    /// Enter: açık harita fişinin eylemi, fiş yoksa şalter (iki adım). Esc: fiş / figür taşıma, açık kapak, yoksa ayarlar.
    /// Sürüklemesiz sağ tık (kameranın <see cref="SiginakCameraRig.BackClicked"/>'i): K4'te panonun geri adımı.
    /// Kol inince vardiya dizisi ve rapor <see cref="SiginakShift"/>'tedir (Aşama 8); o sürerken masa kilitlidir.
    /// <para>
    /// Kartlı oyunda (Revizyon 3 · R19) şalterin kilidi, ön kontrolü ve K6'daki Esc / sağ tık <see cref="SiginakShiftClose"/>'a
    /// gider: K6'da kapak açıksa kapanır, değilse masaya (K1) dönülür; fiş eldeyken tuşları kapanış kendisi okur. K6'da kenar
    /// satırı gizlidir; dosyalama karanlığında sayaçlar eski değerde bekler, sonra 0,4 s'de döner.
    /// </para>
    /// <para>
    /// Revizyon 3 · Aşama 6 (R9/R17): kartlı oyunda KAPI tuşu <see cref="SiginakAdmission.Enter"/>'a gider (K8); K8 ailesinde (K8, K8A,
    /// K8G, K8R) sürüklemesiz sağ tık <see cref="SiginakAdmission.HandleBack"/>'tir, kenar satırı gizlidir ve Enter şaltere gitmez.
    /// Esc kabule yalnız <c>LiberationDeskView.Escape</c>'ten girer (burada ikinci kez çağrılmaz). Kabulün görülen durumu yan dosyaya
    /// <see cref="SaveNotes"/> ile yazılır.
    /// </para>
    /// </summary>
    public sealed class SiginakDesk : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] SiginakSettings settings;
        [SerializeField] Camera view;
        [Header("Masa")]
        [SerializeField] CounterBank counters;
        [Tooltip("Arka duvardaki YZ ALARMI panosu (sayaç + kırmızı ışık).")]
        [SerializeField] AlarmPanel alarm;
        [SerializeField] StationKeyRail rail;
        [SerializeField] ShiftSwitch shiftSwitch;
        [SerializeField] TelexFeed telex;
        [SerializeField] HopeMarks hope;
        [Tooltip("Mantar panodaki KURTARILAN BÖLGELER (Revizyon 2): özgür bölge kartları ve sıradaki hedef.")]
        [SerializeField] FreedRegionsBoard freedBoard;
        [Tooltip("Arka duvardaki tebeşir vardiya çentiği (Revizyon 2).")]
        [SerializeField] ChalkTally tally;
        [Tooltip("Vardiyaya bağlı harekât izleri (Revizyon 2).")]
        [SerializeField] CampaignWear wear;
        [SerializeField] DeskTarget shelfTarget, crtTarget, trayTarget, telexTarget;
        [Header("Ekran")]
        [SerializeField] EdgeLine edge;
        [SerializeField] SettingsPlaque plaque;
        [Tooltip("Harita fişleri: Enter ve Esc önce onlara gider.")]
        [SerializeField] SiginakMap map;
        [Tooltip("Vardiya dizisi, rapor ve EVRAK tepsisi (Aşama 8).")]
        [SerializeField] SiginakShift shift;
        [Tooltip("Operasyon raporu: sayaçlar rapor inene kadar baskından önceki değerde bekler.")]
        [SerializeField] SiginakOperation operation;
        [Tooltip("Açılış ve emir belgesi (Aşama 9).")]
        [SerializeField] SiginakBriefing briefing;
        [Tooltip("Sığınağın kapısı: 4 · KAPI tuşu bekleyen ziyaretçinin kâğıdını açar (Revizyon 2).")]
        [SerializeField] SiginakDoor door;
        [Tooltip("Eğitim notu: başka kâğıt açık değilken Esc notları kapatır (Revizyon 2).")]
        [SerializeField] SiginakTutorial tutorial;
        [SerializeField] PlanningBoardPresenter planningBoard;
        [Tooltip("Kartlı oyunun vardiya kapanışı (Revizyon 3 · R19): şalter kilidi, ön kontrol, K6'nın Esc / sağ tık sırası.")]
        [SerializeField] SiginakShiftClose shiftClose;
        [Tooltip("Kartlı oyunun kapısı ve kabul istasyonu (Revizyon 3 · Aşama 6): KAPI tuşu, K8 ailesinin sağ tıkı, görülen durum.")]
        [SerializeField] SiginakAdmission admission;

        /// <summary>KAPI tuşunun etiketi (kaynak metinler; gösterirken çevrilir).</summary>
        static readonly string DoorWaitingTitle = Loc.N("KAPI · ZİYARETÇİ VAR"),
            DoorWaitingLine = Loc.N("Kapıya dön. Ziyaretçi vardiya bitene kadar bekler."),
            DoorRaidLine = Loc.N("Baskın sürüyor: kapıya gidilemez."),
            DoorIdleLine = Loc.N("Kapıda kimse yok. Biri çalınca lamba yanıp söner.");
        /// <summary>Kartlı oyunda KAPI tuşunun etiketi (Aşama 6; "ziyaretçi" yerine kabul başvurusu).</summary>
        static readonly string DoorApplicantTitle = Loc.N("KAPI · ADAY VAR"),
            DoorApplicantLine = Loc.N("Kapıda başvuru var: tezgâha bak."),
            DoorEmptyLine = Loc.N("Kapıda kimse yok.");

        bool countersShown;

        Simulation Sim => game != null ? game.Sim : null;
        bool Locked => surface == null || surface.InputBlocked;
        /// <summary>Vardiya dizisi oynuyor ya da rapor açık (LiberationDeskView.InputBlocked bunu da sayar).</summary>
        public bool ShiftBusy => shift != null && shift.Busy;
        /// <summary>Kartlı kapanışın dosyalaması sürüyor (kabulün "TALİMAT n" teleksi "VARDİYA DEVREDİLDİ"den sonra basılsın).</summary>
        public bool ShiftFiling => shiftClose != null && shiftClose.Filing;
        /// <summary>Kabul bağlı ve kartlı oyun: KAPI ve K8 ailesi <see cref="SiginakAdmission"/>'da.</summary>
        bool AdmissionActive => admission != null && Sim?.Board != null;

        public void Configure(GameController controller, LiberationDeskView desk, SiginakCameraRig cameraRig, SiginakSettings settingsAsset, Camera camera,
            CounterBank bank, StationKeyRail keys, ShiftSwitch lever, TelexFeed feed, HopeMarks marks, FreedRegionsBoard freed,
            DeskTarget shelf, DeskTarget crt, DeskTarget tray, DeskTarget telexPaper, EdgeLine edgeLine, SettingsPlaque settingsPlaque,
            SiginakMap liberationMap, SiginakShift shiftSequence, SiginakBriefing orderPaper)
        {
            map = liberationMap; shift = shiftSequence; briefing = orderPaper;
            game = controller; surface = desk; rig = cameraRig; settings = settingsAsset; view = camera;
            counters = bank; rail = keys; shiftSwitch = lever; telex = feed; hope = marks; freedBoard = freed;
            shelfTarget = shelf; crtTarget = crt; trayTarget = tray; telexTarget = telexPaper; edge = edgeLine; plaque = settingsPlaque;
        }

        void OnEnable()
        {
            DeskTarget.Blocked = () => Locked;
            if (game != null) { game.Changed += Refresh; game.GameLoaded += Loaded; game.NewGameStarted += NewGame; }
            DeskLoc.Instance.Changed += Relabel;
            rail.Pressed += Station;
            edge.StationClicked += Station;
            plaque.SkipClicked += Skip;
            rig.PoseChanged += PoseChanged;
            rig.BackClicked += Back;
            shiftSwitch.Armed += PreCheck;
            telex.Posted += Store;
            shelfTarget.Clicked += ShowShelter;
            crtTarget.Clicked += FocusCrt;
            trayTarget.Clicked += OpenTray;
            Restore();
            Refresh();
        }

        void OnDisable()
        {
            if (game != null) { game.Changed -= Refresh; game.GameLoaded -= Loaded; game.NewGameStarted -= NewGame; }
            DeskLoc.Instance.Changed -= Relabel;
            rail.Pressed -= Station;
            edge.StationClicked -= Station;
            plaque.SkipClicked -= Skip;
            rig.PoseChanged -= PoseChanged;
            rig.BackClicked -= Back;
            shiftSwitch.Armed -= PreCheck;
            telex.Posted -= Store;
            shelfTarget.Clicked -= ShowShelter;
            crtTarget.Clicked -= FocusCrt;
            trayTarget.Clicked -= OpenTray;
            if (DeskTarget.Blocked != null) DeskTarget.Blocked = null;
        }

        void ShowShelter() => surface.ShowShelter();
        void FocusCrt() { if (!Locked) rig.FocusOperations(); }

        void Skip()
        {
            shift?.Skip();
            briefing?.Skip();
        }

        /// <summary>EVRAK tepsisi: son raporlar (K6); dosyalanmış rapor yoksa kılavuz (brifing Aşama 9'da tepsiye girer).</summary>
        void OpenTray()
        {
            if (Locked) return;
            if (shift == null || !shift.OpenTray()) surface.OpenGuide();
        }

        /// <summary>
        /// Esc: kartlı oyunda K6'da vardiya kapanışı (açık kapak, yoksa masaya); öteki pozlarda harita fişi / figür taşıma, açık
        /// şalter kapağı, sonra eğitim notu. Bir şey yaptıysa true (ayarlar açılmaz).
        /// </summary>
        public bool HandleEscape() => ShiftCloseActive && rig != null && rig.Current == SiginakPose.ShiftClose && shiftClose.HandleEscape() ||
            map != null && map.isActiveAndEnabled && map.HandleEscape() || shiftSwitch != null && shiftSwitch.Cancel() ||
            tutorial != null && tutorial.isActiveAndEnabled && tutorial.HandleEscape();

        /// <summary>Kartlı oyun ve kapanış bağlı: şalterin kilidi, ön kontrolü ve K6 girdisi <see cref="SiginakShiftClose"/>'ta.</summary>
        bool ShiftCloseActive => shiftClose != null && Sim?.Board != null;

        /// <summary>
        /// Sürüklemesiz sağ tık (Karar K7), <see cref="SiginakCameraRig.BackClicked"/>: K4'te panonun geri adımı (tutulan kart
        /// geri döner, açık dosya ya da defter kapanır, yoksa masaya dönülür; <see cref="PlanningBoardPresenter.HandleBack"/>).
        /// K6'da (kartlı oyun) açık kapak kapanır, değilse masaya dönülür (<see cref="SiginakShiftClose.HandleBack"/>; fiş eldeyken
        /// kamera askıda, tık buraya gelmez). K8 ailesinde (kartlı oyun) bir seviye geri: damga → K8R → K8A → K8 → K1, K8G geldiği
        /// poza (<see cref="SiginakAdmission.HandleBack"/>; dizi sürerken masa kilitli, sağ tıkı kabul kendisi okur).
        /// </summary>
        void Back(SiginakPose pose)
        {
            if (pose == SiginakPose.ShiftClose && ShiftCloseActive) { shiftClose.HandleBack(); return; }
            if (Locked) return;
            if (SiginakAdmission.InFamily(pose) && AdmissionActive) { admission.HandleBack(); return; }
            if (pose == SiginakPose.Shelf && Sim?.Board != null && planningBoard != null) planningBoard.HandleBack();
        }

        void Station(int index)
        {
            if (Locked) return;
            switch (index)
            {
                case StationKeyRail.Shelter: surface.ShowShelter(); break;
                case StationKeyRail.Door:
                    if (AdmissionActive) admission.Enter();
                    else if (Sim?.Board != null && planningBoard != null) planningBoard.ShowAdmissions();
                    else door?.Visit();
                    break;
                case StationKeyRail.Desk: rig.ResetView(); break;
            }
        }

        void Update()
        {
            ReadKeys();

            bool locked = Locked;
            int active = StationOf(rig.Current);
            rail.SetActive(active, locked);
            DoorKey();
            edge.SetActiveStation(active);
            edge.SetEnded(Sim != null && Sim.S.Ending != null);
            // K4 ve K6'da sayaç şeridi ve istasyon çipleri gizli (bulgu 20, PROMPT §2); kartlı oyunda K8 ailesinde de (Aşama 6)
            edge.SetVisible(CounterBankInView(), locked, EdgeHidden(rig.Current, Sim?.Board != null));
            // Dizi boyunca sağ üstte ATLA (kenar satırı açık olsa da); başka zaman plaka yok (ayarlar odadaki dolapta)
            bool playing = shift != null && shift.Playing || briefing != null && briefing.OpeningPlaying;
            plaque.SetSkip(playing);
            var sim = Sim;
            if (ShiftCloseActive) shiftClose.UpdateLock();
            else if (sim != null)
            {
                // Eski yol: aynı nedenler ve satırlar; R19 kipindeki oda şalterinin plakası için kısa neden de geçer (W1 isteği)
                bool raid = sim.S.Raid != null || RaidLauncher.IsRaidLoaded, reportOpen = shift != null && shift.ReportOpen;
                var (plate, line) = SiginakShiftClose.LockReason(raid, sim.S.Ending != null, false, reportOpen, false, null);
                shiftSwitch.SetDisabled(plate != null, line, plate);
            }
        }

        /// <summary>Anahtar rayında yanık istasyon: K8 ailesi KAPI tuşudur (Aşama 6); dizi, CRT ve ayarlar hiçbiri.</summary>
        public static int StationOf(SiginakPose pose) => pose switch
        {
            SiginakPose.Shelf => StationKeyRail.Shelter,
            SiginakPose.Desk => StationKeyRail.Desk,
            SiginakPose.Door => StationKeyRail.Door,
            SiginakPose.Admission => StationKeyRail.Door,
            SiginakPose.BreathGlass => StationKeyRail.Door,
            SiginakPose.AdmissionRead => StationKeyRail.Door,
            _ => -1,
        };

        /// <summary>
        /// Kenar satırı (sayaç şeridi + istasyon çipleri) gizli mi: K4 ve K6'da her zaman; kartlı oyunda K8, K8A, K8G, K8R'de de (PROMPT §2,
        /// R17-not). Eski yolun K8'i (ziyaretçi kâğıdı) değişmez.
        /// </summary>
        public static bool EdgeHidden(SiginakPose pose, bool boardMode) => pose == SiginakPose.Shelf || pose == SiginakPose.ShiftClose ||
            boardMode && SiginakAdmission.InFamily(pose);

        void ReadKeys()
        {
            var kb = Keyboard.current;
            if (kb == null || Locked || TextFieldSelected()) return;
            if (Sim?.Board != null && planningBoard != null && planningBoard.IsOpen) return;
            // Kabul tezgâhında Enter şaltere gitmez (eski kabul penceresi açıkken de yutuluyordu)
            if (AdmissionActive && admission.Active) return;
            // Enter: açık kâğıttaki asıl eylem; kâğıt yoksa şalter (iki adım)
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                if (map == null || !map.isActiveAndEnabled || !map.HandleEnter()) shiftSwitch.Press();
        }

        static bool TextFieldSelected()
        {
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            return selected && (selected.GetComponentInParent<TMP_InputField>() || selected.GetComponentInParent<InputField>());
        }

        bool CounterBankInView()
        {
            var face = counters.Face;
            if (face == null || view == null) return false;
            var p = view.WorldToViewportPoint(face.bounds.center);
            return p.z > 0 && p.x > 0 && p.x < 1 && p.y > 0 && p.y < 1;
        }

        // ---------- Vardiya şalteri ----------

        /// <summary>Kapak kalkınca ön kontrol: boşta kişi, görevlisi olup duran oda, verilmemiş baskın emri (K6 notu). Pencere açılmaz.</summary>
        void PreCheck()
        {
            var sim = Sim;
            if (sim?.Shelter == null) return;
            if (sim.Board != null)
            {
                if (shiftClose != null) { shiftClose.PreCheck(); return; }
                rig.Focus(SiginakPose.Shift);
                telex.Post(surface.Shift, Loc.T("ÖN KONTROL"), sim.BoardPlanCheck() ?? Loc.T("Plan hazır. Devir fişini çıkar."), TelexKind.Info);
                return;
            }
            var notes = new System.Collections.Generic.List<string>();
            int idle = ShelterLogic.AvailableWorkers(sim.Shelter);
            if (idle > 0) notes.Add(Loc.F("boşta {0} kişi", idle));
            foreach (var room in sim.Shelter.Rooms)
                if (room.Workers > 0 && ShelterViewBuilder.Condition(sim, room) == RoomCondition.Stopped)
                    notes.Add(Loc.F("{0} durdu", ShelterLogic.RoomName(room.Id)));
            if (sim.DoorVisitor != null) notes.Add(Loc.T("kapıda biri bekliyor: açılmadı sayılır"));
            var next = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            if (next != null && sim.LiberationRaidCheck(next.Id) == null) notes.Add(Loc.F("baskın emri verilmedi: {0}", next.Name));
            telex.Post(surface.Shift, Loc.T("ÖN KONTROL"),
                notes.Count == 0 ? Loc.T("eksik yok · kolu indirmek için yeniden bas") : string.Join(" · ", notes), TelexKind.Info);
        }

        // ---------- Durum ----------

        void NewGame() { telex.Clear(); countersShown = false; Refresh(); }

        void Loaded() { Restore(); countersShown = false; Refresh(); }

        void Restore() => telex.Restore(SiginakDeskStore.LoadTelex(Sim?.Record));

        void Store(TelexLine _) => SaveNotes();

        /// <summary>Teleks satırları, EVRAK raporları ve devir fişi arşivi yan dosyaya (kayda girmez).</summary>
        public void SaveNotes()
        {
            var sim = Sim;
            // Henüz oynanmamış oyun (gün 0, komut yok) eski kaydın notlarının üstüne yazmasın (GameController.Save ile aynı kural)
            if (sim?.Record == null || sim.S.Day == 0 && sim.Record.commands.Count == 0) return;
            SiginakDeskStore.Save(sim.Record, telex.Lines, shift != null ? shift.Tray.Reports : null, SavedPose(rig.Current).ToString(),
                shiftClose != null ? shiftClose.ArchiveFor(sim.Record) : null,
                admission != null ? admission.SeenFor(sim.Record) : SiginakDeskStore.LoadAdmissionSeen(sim.Record));
        }

        /// <summary>
        /// KAYITTAN DEVAM'ın döneceği poz: istasyonlar; dizi, vardiya şalteri (K6, K6a), CRT bakışı ve kapının K8 ailesi (K8, K8A, K8G,
        /// K8R) masaya sayılır.
        /// </summary>
        public static SiginakPose SavedPose(SiginakPose pose) => pose == SiginakPose.Map || pose == SiginakPose.Shelf ? pose : SiginakPose.Desk;

        void PoseChanged(SiginakPose pose)
        {
            SaveNotes();
            // Kapıdan masaya dönülünce ziyaretçi hâlâ bekliyorsa fiş bir kez sallanır
            if (pose == SiginakPose.Desk && rail.Count > StationKeyRail.Door && rail[StationKeyRail.Door].Attention) rail[StationKeyRail.Door].Nudge();
        }

        /// <summary>
        /// KAPI: biri beklerken lamba yanıp söner (1 Hz), etiket bekleyeni söyler; baskında nedenini yazar. Kartlı oyunda kapıdaki
        /// kabul başvurusu (Aşama 6), eski yolda ziyaretçi.
        /// </summary>
        void DoorKey()
        {
            if (rail.Count <= StationKeyRail.Door) return;
            var key = rail[StationKeyRail.Door];
            var sim = Sim;
            bool raid = sim != null && (sim.S.Raid != null || RaidLauncher.IsRaidLoaded);
            bool board = sim?.Board != null;
            bool waiting = sim != null && sim.S.Ending == null && (board ? sim.Admission?.Candidate != null : sim.DoorVisitor != null);
            key.SetAttention(waiting && !raid);
            key.Target.TitleOverride = waiting ? board ? DoorApplicantTitle : DoorWaitingTitle : null;
            key.Target.LineOverride = Loc.T(raid ? DoorRaidLine : waiting ? board ? DoorApplicantLine : DoorWaitingLine : board ? DoorEmptyLine : DoorIdleLine);
        }

        void Relabel() => Refresh();

        void Refresh()
        {
            var sim = Sim;
            if (sim == null) return;
            var shelter = sim.Shelter;
            var lib = sim.S.Liberation;
            int free = lib.Regions.Count(r => r.Completed), regions = lib.Regions.Count;
            double capacity = ShelterLogic.SuppliesCapacity(shelter, sim.B);
            // Vardiya dizisinde sayaçlar 0,40'a kadar eski değerde bekler (SiginakShift sırayla döndürür)
            // Baskından dönüşte de rapor inene kadar (SiginakOperation); kartlı oyunda dosyalama karanlığı açılana kadar (SiginakShiftClose)
            if ((shift == null || !shift.HoldCounters) && (shiftClose == null || !shiftClose.HoldCounters) &&
                (operation == null || !operation.HoldCounters)) ShowCounters(countersShown);
            // Revizyon 2: GÜVEN, ALARM ve 4 KAPI fişiyle tek satıra sığsın diye ayırıcılar tek boşluklu. Revizyon 3: sayaç plakası
            // gibi HAMMADDE (Soru 3)
            edge.SetValues(Loc.F("VARDİYA {0:000} · FON {1} · ERZAK {2}/{3} · HAMMADDE {4} · ÖZGÜR {5}/{6} · GÜVEN {7}/{8} · ALARM {9}/{10}", surface.Shift,
                DeskFormat.Number(sim.S.Fon, 1), DeskFormat.Number(System.Math.Floor(shelter.Supplies), 0), DeskFormat.Number(capacity, 0),
                DeskFormat.Number(System.Math.Floor(shelter.Materials), 0), free, regions, sim.S.Trust, sim.TrustMax,
                DeskFormat.Number(sim.S.Alarm, 0), DeskFormat.Number(sim.B.liberationAlarmThreshold, 0)));
            hope.Show(free, settings == null || settings.hopeMarks);
            FreedList(sim);
            if (tally != null) tally.Show(surface.Shift);
            if (wear != null) wear.Show(surface.Shift, settings == null || settings.campaignWear);
        }

        /// <summary>Karşı saldırı: duvardaki alarm lambası üç kez yanar (vardiya dizisinde, rapordan önce).</summary>
        public void FlashAlarm() { if (alarm != null) alarm.Flash(3); }

        /// <summary>
        /// Sayaç rafı ve alarm panosu simülasyonun değerleriyle; vardiya dizisi sırayla döndürür, rafın fişlerini kendisi
        /// iğneler (alarm panosu kendi fişini asar). <paramref name="roll"/> &gt; 0: rafın bütün modüllerinin çark süresi (kartlı
        /// kapanış 0,4 s; VARDİYA için <paramref name="shiftRoll"/> önceliklidir).
        /// </summary>
        public void ShowCounters(bool animate, float stagger = 0, float shiftRoll = -1, bool slips = true, float roll = -1)
        {
            var sim = Sim;
            if (sim == null) return;
            var shelter = sim.Shelter;
            var lib = sim.S.Liberation;
            counters.Show(surface.Shift, sim.S.Fon, shelter.Supplies, ShelterLogic.SuppliesCapacity(shelter, sim.B), shelter.Materials,
                lib.Regions.Count(r => r.Completed), lib.Regions.Count, sim.S.Trust, sim.TrustMax, animate, stagger, shiftRoll, slips, roll);
            if (alarm != null)
                alarm.Show(sim.S.Alarm, sim.B.liberationAlarmThreshold, animate, stagger * 6,
                    Loc.F("Eşiğe ({0}) varınca YZ karşı saldırır, bir sektör yeniden işgal edilir. Her vardiya +{1}; telsiz odası düşürür.",
                        DeskFormat.Number(sim.B.liberationAlarmThreshold, 0), ShelterViewBuilder.Rate(sim.AlarmPerShift)));
            if (alarm != null) alarm.SetRadioActive(ShelterLogic.RadioWorkers(shelter) > 0);
            countersShown = true;
        }

        /// <summary>
        /// Mantar panodaki KURTARILAN BÖLGELER: özgürleşme sırasıyla kartlar (ad, son sunucu sektörünün vardiyası), sonraki yuvada
        /// sıradaki hedef (cephe bölgesi, özgür değilse) kurşun kalemle. Yeni kart vardiya dizisi dışında da düşer.
        /// </summary>
        void FreedList(Simulation sim)
        {
            if (freedBoard == null) return;
            var lib = sim.S.Liberation;
            var freed = lib.Regions.Where(r => r.Completed)
                .Select(r => (id: r.Id, day: r.Sectors.Max(s => s.CapturedDay)))
                .OrderBy(r => r.day)
                // Kartta ad iki satır (“BATI / AVRUPA”): raptiye ilk satırın ortasına düşmesin
                .Select(r => (DeskText.Upper(GameData.Region(r.id).Name).Replace(' ', '\n'), r.day + 1)).ToList();
            var front = lib.Regions.FirstOrDefault(r => r.Id == lib.CurrentRegion);
            string next = front != null && !front.Completed ? GameData.Region(front.Id).Name : null;
            freedBoard.Show(freed, next, countersShown);
        }
    }
}
