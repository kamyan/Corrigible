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
    /// kenar satırı, AYARLAR plakası, odadaki tıklanır nesneler ve umut işaretleri. Simülasyonu yalnız okur; komutlar
    /// <see cref="LiberationDeskView"/>'in yöntemlerinden geçer. Yalnız yeni görünümde etkindir (sığınak kökünün altında).
    /// Enter: açık harita fişinin eylemi, fiş yoksa şalter (iki adım). Esc: fiş / figür taşıma, açık kapak, yoksa ayarlar.
    /// Kol inince vardiya dizisi ve rapor <see cref="SiginakShift"/>'tedir (Aşama 8); o sürerken masa kilitlidir.
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

        /// <summary>KAPI tuşunun etiketi (kaynak metinler; gösterirken çevrilir).</summary>
        static readonly string DoorWaitingTitle = Loc.N("KAPI · ZİYARETÇİ VAR"),
            DoorWaitingLine = Loc.N("Kapıya dön. Ziyaretçi vardiya bitene kadar bekler."),
            DoorRaidLine = Loc.N("Baskın sürüyor: kapıya gidilemez."),
            DoorIdleLine = Loc.N("Kapıda kimse yok. Biri çalınca lamba yanıp söner.");

        bool countersShown;

        Simulation Sim => game != null ? game.Sim : null;
        bool Locked => surface == null || surface.InputBlocked;
        /// <summary>Vardiya dizisi oynuyor ya da rapor açık (LiberationDeskView.InputBlocked bunu da sayar).</summary>
        public bool ShiftBusy => shift != null && shift.Busy;

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
            edge.SettingsClicked += OpenSettings;
            plaque.SettingsClicked += OpenSettings;
            plaque.SkipClicked += Skip;
            rig.PoseChanged += PoseChanged;
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
            edge.SettingsClicked -= OpenSettings;
            plaque.SettingsClicked -= OpenSettings;
            plaque.SkipClicked -= Skip;
            rig.PoseChanged -= PoseChanged;
            shiftSwitch.Armed -= PreCheck;
            telex.Posted -= Store;
            shelfTarget.Clicked -= ShowShelter;
            crtTarget.Clicked -= FocusCrt;
            trayTarget.Clicked -= OpenTray;
            if (DeskTarget.Blocked != null) DeskTarget.Blocked = null;
        }

        void ShowShelter() => surface.ShowShelter();
        void FocusCrt() { if (!Locked) rig.FocusOperations(); }
        // Emir belgesi açıkken de AYARLAR (K7 notu › Esc)
        void OpenSettings() { if (!Locked || briefing != null && briefing.AllowsSettings) SettingsView.Instance?.Open(); }

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

        /// <summary>Esc: harita fişi / figür taşıma, açık şalter kapağı, sonra eğitim notu. Bir şey yaptıysa true (ayarlar açılmaz).</summary>
        public bool HandleEscape() => map != null && map.isActiveAndEnabled && map.HandleEscape() || shiftSwitch != null && shiftSwitch.Cancel() ||
            tutorial != null && tutorial.isActiveAndEnabled && tutorial.HandleEscape();

        void Station(int index)
        {
            if (Locked) return;
            switch (index)
            {
                case StationKeyRail.Shelter: surface.ShowShelter(); break;
                case StationKeyRail.Missions: surface.ShowPlanning(); break;
                case StationKeyRail.Door: door?.Visit(); break;
                case StationKeyRail.Desk: rig.ResetView(); break;
            }
        }

        void Update()
        {
            ReadKeys();

            bool locked = Locked;
            int active = rig.Current switch
            {
                SiginakPose.Map => StationKeyRail.Missions,
                SiginakPose.Shelf => StationKeyRail.Shelter,
                SiginakPose.Desk => StationKeyRail.Desk,
                SiginakPose.Door => StationKeyRail.Door,
                _ => -1,
            };
            rail.SetActive(active, locked);
            DoorKey();
            edge.SetActiveStation(active);
            edge.SetEnded(Sim != null && Sim.S.Ending != null);
            edge.SetVisible(CounterBankInView(), locked);
            // Dizi boyunca sağ üstte ATLA (kenar satırı açık olsa da)
            bool playing = shift != null && shift.Playing || briefing != null && briefing.OpeningPlaying;
            plaque.SetMode(playing, !playing && edge.Visible);
            var sim = Sim;
            if (sim != null)
            {
                bool raid = sim.S.Raid != null || RaidLauncher.IsRaidLoaded, reportOpen = shift != null && shift.ReportOpen;
                shiftSwitch.SetDisabled(raid || sim.S.Ending != null || reportOpen,
                    raid ? Loc.T("Baskın sürüyor: vardiya bitirilemez.") : sim.S.Ending != null ? Loc.T("Harekât bitti.")
                    : reportOpen ? Loc.T("Rapor açık: önce EVRAK tepsisine dosyala.") : null);
            }
        }

        void ReadKeys()
        {
            var kb = Keyboard.current;
            if (kb == null || Locked || TextFieldSelected()) return;
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

        /// <summary>Teleks satırları ve EVRAK raporları yan dosyaya (kayda girmez).</summary>
        public void SaveNotes()
        {
            var sim = Sim;
            // Henüz oynanmamış oyun (gün 0, komut yok) eski kaydın notlarının üstüne yazmasın (GameController.Save ile aynı kural)
            if (sim?.Record == null || sim.S.Day == 0 && sim.Record.commands.Count == 0) return;
            SiginakDeskStore.Save(sim.Record, telex.Lines, shift != null ? shift.Tray.Reports : null, SavedPose(rig.Current).ToString());
        }

        /// <summary>KAYITTAN DEVAM'ın döneceği poz: istasyonlar; dizi ve CRT bakışı masaya sayılır.</summary>
        static SiginakPose SavedPose(SiginakPose pose) => pose == SiginakPose.Map || pose == SiginakPose.Shelf ? pose : SiginakPose.Desk;

        void PoseChanged(SiginakPose pose)
        {
            SaveNotes();
            // Kapıdan masaya dönülünce ziyaretçi hâlâ bekliyorsa fiş bir kez sallanır
            if (pose == SiginakPose.Desk && rail.Count > StationKeyRail.Door && rail[StationKeyRail.Door].Attention) rail[StationKeyRail.Door].Nudge();
        }

        /// <summary>KAPI: ziyaretçi beklerken lamba yanıp söner, etiket bekleyeni söyler; baskında nedenini yazar.</summary>
        void DoorKey()
        {
            if (rail.Count <= StationKeyRail.Door) return;
            var key = rail[StationKeyRail.Door];
            var sim = Sim;
            bool raid = sim != null && (sim.S.Raid != null || RaidLauncher.IsRaidLoaded);
            bool waiting = sim != null && sim.DoorVisitor != null && sim.S.Ending == null;
            key.SetAttention(waiting && !raid);
            key.Target.TitleOverride = waiting ? DoorWaitingTitle : null;
            key.Target.LineOverride = Loc.T(raid ? DoorRaidLine : waiting ? DoorWaitingLine : DoorIdleLine);
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
            // Baskından dönüşte de rapor inene kadar (SiginakOperation)
            if ((shift == null || !shift.HoldCounters) && (operation == null || !operation.HoldCounters)) ShowCounters(countersShown);
            // Revizyon 2: GÜVEN, ALARM ve 4 KAPI fişiyle tek satıra sığsın diye ayırıcılar tek boşluklu
            edge.SetValues(Loc.F("VARDİYA {0:000} · FON {1} · ERZAK {2}/{3} · MALZEME {4} · ÖZGÜR {5}/{6} · GÜVEN {7}/{8} · ALARM {9}/{10}", surface.Shift,
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
        /// iğneler (alarm panosu kendi fişini asar).
        /// </summary>
        public void ShowCounters(bool animate, float stagger = 0, float shiftRoll = -1, bool slips = true)
        {
            var sim = Sim;
            if (sim == null) return;
            var shelter = sim.Shelter;
            var lib = sim.S.Liberation;
            counters.Show(surface.Shift, sim.S.Fon, shelter.Supplies, ShelterLogic.SuppliesCapacity(shelter, sim.B), shelter.Materials,
                lib.Regions.Count(r => r.Completed), lib.Regions.Count, sim.S.Trust, sim.TrustMax, animate, stagger, shiftRoll, slips);
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
                .Select(r => (DeskText.Upper(Loc.T(GameData.Region(r.id).Name)).Replace(' ', '\n'), r.day + 1)).ToList();
            var front = lib.Regions.FirstOrDefault(r => r.Id == lib.CurrentRegion);
            string next = front != null && !front.Completed ? Loc.T(GameData.Region(front.Id).Name) : null;
            freedBoard.Show(freed, next, countersShown);
        }
    }
}
