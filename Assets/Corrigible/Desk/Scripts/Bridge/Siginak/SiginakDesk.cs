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
        [SerializeField] StationKeyRail rail;
        [SerializeField] ShiftSwitch shiftSwitch;
        [SerializeField] TelexFeed telex;
        [SerializeField] HopeMarks hope;
        [SerializeField] TMP_Text[] freedEntries = System.Array.Empty<TMP_Text>();
        [SerializeField] DeskTarget shelfTarget, crtTarget, trayTarget, telexTarget;
        [Header("Ekran")]
        [SerializeField] EdgeLine edge;
        [SerializeField] SettingsPlaque plaque;
        [Tooltip("Harita fişleri: Enter ve Esc önce onlara gider.")]
        [SerializeField] SiginakMap map;
        [Tooltip("Vardiya dizisi, rapor ve EVRAK tepsisi (Aşama 8).")]
        [SerializeField] SiginakShift shift;
        [Tooltip("Açılış ve emir belgesi (Aşama 9).")]
        [SerializeField] SiginakBriefing briefing;

        int lastMapStation = StationKeyRail.Command;
        bool countersShown;

        Simulation Sim => game != null ? game.Sim : null;
        bool Locked => surface == null || surface.InputBlocked;
        /// <summary>Vardiya dizisi oynuyor ya da rapor açık (LiberationDeskView.InputBlocked bunu da sayar).</summary>
        public bool ShiftBusy => shift != null && shift.Busy;

        public void Configure(GameController controller, LiberationDeskView desk, SiginakCameraRig cameraRig, SiginakSettings settingsAsset, Camera camera,
            CounterBank bank, StationKeyRail keys, ShiftSwitch lever, TelexFeed feed, HopeMarks marks, TMP_Text[] freed,
            DeskTarget shelf, DeskTarget crt, DeskTarget tray, DeskTarget telexPaper, EdgeLine edgeLine, SettingsPlaque settingsPlaque,
            SiginakMap liberationMap, SiginakShift shiftSequence, SiginakBriefing orderPaper)
        {
            map = liberationMap; shift = shiftSequence; briefing = orderPaper;
            game = controller; surface = desk; rig = cameraRig; settings = settingsAsset; view = camera;
            counters = bank; rail = keys; shiftSwitch = lever; telex = feed; hope = marks; freedEntries = freed;
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

        /// <summary>Esc: harita fişi / figür taşıma, sonra açık şalter kapağı. Bir şey yaptıysa true (ayarlar açılmaz).</summary>
        public bool HandleEscape() => map != null && map.isActiveAndEnabled && map.HandleEscape() || shiftSwitch != null && shiftSwitch.Cancel();

        void Station(int index)
        {
            if (Locked) return;
            switch (index)
            {
                case StationKeyRail.Command: lastMapStation = index; rig.FocusMap(); break;
                case StationKeyRail.Shelter: surface.ShowShelter(); break;
                case StationKeyRail.Missions: lastMapStation = index; surface.ShowPlanning(); break;
                case StationKeyRail.Guide: surface.OpenGuide(); break;
                case StationKeyRail.Desk: rig.ResetView(); break;
            }
        }

        void Update()
        {
            ReadKeys();

            bool locked = Locked;
            int active = rig.Current switch
            {
                SiginakPose.Map => lastMapStation,
                SiginakPose.Shelf => StationKeyRail.Shelter,
                SiginakPose.Desk => StationKeyRail.Desk,
                _ => -1,
            };
            rail.SetActive(active, locked);
            edge.SetActiveStation(active);
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
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) lastMapStation = StationKeyRail.Command;
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) lastMapStation = StationKeyRail.Missions;
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

        /// <summary>Kapak kalkınca ön kontrol: boşta kişi, enerjisiz oda, verilmemiş baskın emri (K6 notu). Pencere açılmaz.</summary>
        void PreCheck()
        {
            var sim = Sim;
            if (sim?.Shelter == null) return;
            var notes = new System.Collections.Generic.List<string>();
            int idle = ShelterLogic.AvailableWorkers(sim.Shelter);
            if (idle > 0) notes.Add(Loc.F("boşta {0} kişi", idle));
            bool energyShort = ShelterLogic.EnergyCapacity(sim.Shelter, sim.B) < ShelterLogic.EnergyDemand(sim.Shelter, sim.B);
            if (energyShort) notes.Add(Loc.T("enerjisiz oda var"));
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

        void PoseChanged(SiginakPose _) => SaveNotes();

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
            if (shift == null || !shift.HoldCounters) ShowCounters(countersShown);
            edge.SetValues(Loc.F("VARDİYA {0:000}  ·  FON {1}  ·  ERZAK {2}/{3}  ·  MALZEME {4}  ·  ÖZGÜR {5}/{6}", surface.Shift,
                DeskFormat.Number(sim.S.Fon, 1), DeskFormat.Number(System.Math.Floor(shelter.Supplies), 0), DeskFormat.Number(capacity, 0),
                DeskFormat.Number(System.Math.Floor(shelter.Materials), 0), free, regions));
            hope.Show(free, settings == null || settings.hopeMarks);
            FreedList(sim);
        }

        /// <summary>Sayaç rafı simülasyonun değerleriyle; vardiya dizisi sırayla döndürür, fişleri kendisi iğneler.</summary>
        public void ShowCounters(bool animate, float stagger = 0, float shiftRoll = -1, bool slips = true)
        {
            var sim = Sim;
            if (sim == null) return;
            var shelter = sim.Shelter;
            var lib = sim.S.Liberation;
            counters.Show(surface.Shift, sim.S.Fon, shelter.Supplies, ShelterLogic.SuppliesCapacity(shelter, sim.B), shelter.Materials,
                lib.Regions.Count(r => r.Completed), lib.Regions.Count, animate, stagger, shiftRoll, slips);
            countersShown = true;
        }

        /// <summary>Mantar panodaki KURTARILAN BÖLGELER listesi: özgürleşme sırasıyla, son sunucu sektörünün vardiyası.</summary>
        void FreedList(Simulation sim)
        {
            var freed = sim.S.Liberation.Regions.Where(r => r.Completed)
                .Select(r => (id: r.Id, day: r.Sectors.Max(s => s.CapturedDay)))
                .OrderBy(r => r.day).ToList();
            int first = Mathf.Max(0, freed.Count - freedEntries.Length);
            for (int i = 0; i < freedEntries.Length; i++)
            {
                int n = first + i;
                var entry = freedEntries[i];
                if (entry == null) continue;
                entry.text = n < freed.Count
                    ? Loc.F("{0}. {1}   V.{2:000} ✓", n + 1, Loc.T(GameData.Region(freed[n].id).Name), freed[n].day + 1)
                    : (n + 1) + ".";
                var c = entry.color;
                c.a = n < freed.Count ? 1 : 0.35f;
                entry.color = c;
            }
        }
    }
}
