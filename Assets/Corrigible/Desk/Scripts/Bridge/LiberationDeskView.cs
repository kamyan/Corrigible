using System;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Raid;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Üç fiziksel istasyonu özgürleştirme komutlarına bağlar; arayüz sahnede saklanır.</summary>
    public sealed class LiberationDeskView : MonoBehaviour, IDeskSurface
    {
        [SerializeField] GameController game;
        [SerializeField] SimDeskDataSource source;
        [SerializeField] SadeMapView map;
        [SerializeField] CommandPostCamera cameraRig;
        [Tooltip("Sığınak görünümünün kamerası; etkinse eski komuta odası kamerasının yerine sürülür.")]
        [SerializeField] SiginakCameraRig siginakRig;
        [SerializeField] CommandPostDesk desk;
        [SerializeField] RaidLauncher launcher;
        [SerializeField] GameObject storyRoot;
        [SerializeField] TMP_Text summary, missionTitle, missionDetail, shelterSummary, roomDetail, resultNote;
        [SerializeField] Button[] sectorButtons = Array.Empty<Button>();
        [SerializeField] TMP_Text[] sectorLabels = Array.Empty<TMP_Text>();
        [SerializeField] Button[] roomButtons = Array.Empty<Button>();
        [SerializeField] TMP_Text[] roomLabels = Array.Empty<TMP_Text>();
        [SerializeField] Button chooseRegion, upgradeRoom, workerMinus, workerPlus, advanceDay, beginStory, continueStory;
        [SerializeField] Button commandStation, shelterStation, operationStation;
        [SerializeField] Button settingsButton;
        [Header("Harita görev fişi")]
        [SerializeField] RectTransform briefingCanvas, briefingRoot;
        [SerializeField] UILineGraphic briefingLink;
        [SerializeField] Button closeBriefing;
        [SerializeField] TMP_Text monitorStatus;
        [Header("Sığınak görünümü")]
        [Tooltip("Emir sonuçları ve gerekçeleri buraya da basılır (yeni görünümde tek geri bildirim kapısı).")]
        [SerializeField] TelexFeed telex;
        [SerializeField] SiginakDesk siginak;
        [Tooltip("Kurtuluş haritası (görev fişi, aday, ekip figürü); etkinken eski görev fişi gizlenir, bölge tıklaması ona gider.")]
        [SerializeField] SiginakMap siginakMap;
        [Tooltip("Açılış dizisi ve emir belgesi (K7); etkinken eski hikâye kâğıdının yerine geçer, H onu okuma kipinde açar.")]
        [SerializeField] SiginakBriefing siginakBriefing;
        readonly string[] roomIds = { ShelterLogic.Living, ShelterLogic.Generator, ShelterLogic.Workshop, ShelterLogic.Infirmary };
        IDeskCommands commands;
        string selectedRegion, selectedRoom = ShelterLogic.Living;
        bool storyOpen, lastBlocked, briefingOpen = true;
        readonly Vector2[] linkPoints = new Vector2[2];
        Simulation Sim => game != null ? game.Sim : null;
        IDeskCameraRig Rig => siginakRig != null && siginakRig.enabled ? siginakRig : cameraRig;
        bool NewMap => siginakMap != null && siginakMap.isActiveAndEnabled;
        bool NewBriefing => siginakBriefing != null && siginakBriefing.isActiveAndEnabled;
        /// <summary>Baskın sonucu henüz okunmadı: operasyon raporu modalı açılacak ya da açık (K3 dizisi onu bekler).</summary>
        public bool RaidReportPending => Sim != null && source != null && Sim.S.Raid == null && Sim.S.RaidLog.Count > source.State.RaidLogSeen;
        public bool InputBlocked => storyOpen || launcher != null && launcher.IsTransitioning || RaidLauncher.IsRaidLoaded ||
            SettingsView.Instance != null && SettingsView.Instance.IsOpen || GameOverView.Instance != null && GameOverView.Instance.IsOpen ||
            EventModalView.Instance != null && EventModalView.Instance.IsOpen || ConfirmModalView.Instance != null && ConfirmModalView.Instance.IsOpen ||
            siginak != null && siginak.isActiveAndEnabled && siginak.ShiftBusy || NewBriefing && siginakBriefing.Busy;

        void Start()
        {
            for (int i = 0; i < sectorButtons.Length; i++) { int slot = i; sectorButtons[i].onClick.AddListener(() => Launch(slot)); }
            for (int i = 0; i < roomButtons.Length; i++) { int slot = i; roomButtons[i].onClick.AddListener(() => { if (InputBlocked) return; selectedRoom = roomIds[slot]; Refresh(); }); }
            foreach (var r in map.Regions)
            {
                if (r.Node != null) r.Node.Clicked += SelectRegion;
                if (r.Fill != null) r.Fill.Clicked += SelectRegion;
            }
            chooseRegion.onClick.AddListener(() => OpenFront(selectedRegion));
            upgradeRoom.onClick.AddListener(() => UpgradeRoom(selectedRoom));
            workerMinus.onClick.AddListener(() => Assign(-1));
            workerPlus.onClick.AddListener(() => Assign(1));
            advanceDay.onClick.AddListener(() => AdvanceShift());
            beginStory.onClick.AddListener(CloseStory);
            continueStory.onClick.AddListener(() => { if (game.ContinueSaved()) CloseStory(); });
            commandStation.onClick.AddListener(ShowPlanning);
            shelterStation.onClick.AddListener(ShowShelter);
            operationStation.onClick.AddListener(ShowPlanning);
            closeBriefing.onClick.AddListener(() => { briefingOpen = false; UpdateBriefingPlacement(); });
            if (settingsButton != null) settingsButton.onClick.AddListener(() => { if (!InputBlocked) SettingsView.Instance?.Open(); });
            game.NewGameStarted += NewGame;
            game.GameLoaded += Loaded;
            game.Changed += Refresh;
            DeskLoc.Instance.Changed += Refresh;
            NewGame();
        }

        void OnDestroy()
        {
            if (game != null) { game.NewGameStarted -= NewGame; game.GameLoaded -= Loaded; game.Changed -= Refresh; }
            DeskLoc.Instance.Changed -= Refresh;
            if (map != null) foreach (var r in map.Regions)
            {
                if (r.Node != null) r.Node.Clicked -= SelectRegion;
                if (r.Fill != null) r.Fill.Clicked -= SelectRegion;
            }
        }

        // Ortak modal, presenter bağlamasının sonunda kapanır. Düğmelerin durumu
        // aynı bağlamada eski modal bayrağıyla kalmışsa sonraki karede yenilenir.
        void LateUpdate()
        {
            if (Sim?.LiberationOn == true && InputBlocked != lastBlocked) Refresh();
            UpdateBriefingPlacement();
        }

        /// <summary>Harita pozu ve görev fişi (3 GÖREVLER).</summary>
        public void ShowPlanning()
        {
            if (InputBlocked) return;
            if (NewMap) { siginakMap.ShowMissions(); return; }
            briefingOpen = true;
            Rig.FocusMap();
            Refresh();
        }

        public void ShowShelter() { if (!InputBlocked) Rig.FocusShelter(); }

        public void OpenGuide() => OpenWindow(WindowId.Guide);

        /// <summary>Şu anki vardiyanın numarası (kurtuluşta gün + 1: ilk vardiya 001).</summary>
        public int Shift => (Sim?.S.Day ?? 0) + 1;

        // ---------- Komutlar: düğmeler ve sığınak nesneleri aynı yolu kullanır; sonuç teleksten gerekçesiyle basılır ----------

        public ActResult UpgradeRoom(string roomId)
        {
            if (InputBlocked || Sim?.Shelter == null) return null;
            string why = TelexReasons.Upgrade(Sim, roomId);
            return Report(game.ShelterUpgrade(roomId), why);
        }

        public ActResult AssignRoom(string roomId, int workers)
        {
            if (InputBlocked || Sim?.Shelter == null) return null;
            string why = TelexReasons.Assign(Sim, roomId, workers);
            return Report(game.ShelterAssign(roomId, workers), why);
        }

        public ActResult OpenFront(string regionId)
        {
            if (InputBlocked || Sim?.LiberationOn != true) return null;
            string why = TelexReasons.Front(Sim, regionId);
            string done = Loc.F("Yeni cephe: {0}", RegionName(regionId));
            return Report(game.SelectLiberationRegion(regionId), why, done);
        }

        public ActResult LaunchSector(string sectorId)
        {
            if (InputBlocked || Sim?.LiberationOn != true) return null;
            string why = TelexReasons.Raid(Sim, sectorId);
            var sector = Sim.LiberationSector(sectorId);
            string done = sector == null ? null : Loc.F("Baskın emri: {0} · {1} · {2} fon", RegionName(sector.Region), sector.Name,
                DeskFormat.Signed(-Sim.B.liberationRaidCost, 0));
            return Report(game.StartLiberationRaid(sectorId), why, done);
        }

        /// <summary>Vardiyayı bitirir (şalterin kolu ya da eski düğme). Yapılamazsa false.</summary>
        public bool AdvanceShift()
        {
            if (InputBlocked || Sim?.LiberationOn != true || Sim.S.Blocked || Sim.S.Ending != null || Sim.S.Raid != null) return false;
            game.AdvanceLiberationDay();
            Refresh();
            return true;
        }

        void UpdateBriefingPlacement()
        {
            if (briefingRoot == null || briefingCanvas == null) return;
            bool show = !NewMap && briefingOpen && !InputBlocked && Rig.IsFocused && Sim?.LiberationOn == true;
            briefingRoot.gameObject.SetActive(show);
            if (briefingLink != null) briefingLink.gameObject.SetActive(show);
            if (!show) return;
            var bounds = briefingCanvas.rect;
            float scale = Mathf.Min(1, (bounds.width - 40) / briefingRoot.rect.width,
                (bounds.height - 220) / briefingRoot.rect.height);
            briefingRoot.localScale = Vector3.one * scale;
            float halfW = briefingRoot.rect.width * scale * .5f, halfH = briefingRoot.rect.height * scale * .5f;
            Vector2 pin = DeskCanvasProjection.LocalPoint(map, map.RegionWorld(selectedRegion), briefingCanvas);
            bool right = pin.x < bounds.center.x;
            var center = new Vector2(pin.x + (right ? 1 : -1) * (halfW + 42), pin.y + 20);
            center.x = Mathf.Clamp(center.x, bounds.xMin + halfW + 24, bounds.xMax - halfW - 24);
            center.y = Mathf.Clamp(center.y, bounds.yMin + halfH + 142, bounds.yMax - halfH - 82);
            briefingRoot.anchoredPosition = center;
            if (briefingLink != null)
            {
                linkPoints[0] = pin;
                linkPoints[1] = new Vector2(center.x + (right ? -halfW : halfW), Mathf.Clamp(pin.y, center.y - halfH, center.y + halfH));
                briefingLink.SetPoints(linkPoints);
            }
        }

        void NewGame()
        {
            if (Sim?.LiberationOn != true) return;
            selectedRegion = Sim.S.Liberation.CurrentRegion;
            storyOpen = true;
            game.TutorialHold = true;
            if (desk != null) desk.ExternalBlocked = true;
            // Yeni görünümde açılış dizisi ve emir belgesi (K7); BAŞLA / KAYITTAN DEVAM buraya döner (BeginCampaign, Loaded)
            if (NewBriefing) siginakBriefing.OpenOpening();
            else storyRoot.SetActive(true);
            source.State.IntroOpen = false;
            Refresh();
        }

        /// <summary>Emir belgesinde BAŞLA (yeni görünüm): hikâye kapanır, harekât başlar.</summary>
        public void BeginCampaign() => CloseStory();

        void Loaded()
        {
            selectedRegion = Sim.S.Liberation.CurrentRegion;
            CloseStory();
        }

        void CloseStory()
        {
            storyOpen = false;
            storyRoot.SetActive(false);
            game.TutorialHold = false;
            if (desk != null) desk.ExternalBlocked = false;
            source.State.IntroOpen = false;
            briefingOpen = true;
            source.State.Selected = selectedRegion;
            Rig.FocusMap();
            source.MarkDirty();
            Refresh();
        }

        public void BindGame(DeskViewModel model, IDeskCommands cmd)
        {
            commands = cmd;
            if (model?.Sade?.View?.Map != null) map.Bind(model.Sade.View.Map);
            Refresh();
        }

        static string RegionName(string id) => string.IsNullOrEmpty(id) ? "—" : Loc.T(GameData.Region(id).Name);

        void Refresh()
        {
            if (Sim?.LiberationOn != true) return;
            lastBlocked = InputBlocked;
            var lib = Sim.S.Liberation;
            selectedRegion ??= lib.CurrentRegion;
            var area = Sim.LiberationRegion(selectedRegion);
            var shelter = Sim.Shelter;
            var b = Sim.B;
            DeskUi.SetText(summary, Loc.F("VARDİYA {0:000}  ·  FON {1:0}  ·  ERZAK {2:0}  ·  MALZEME {3:0}  ·  ÖZGÜR BÖLGE {4}/{5}",
                Sim.S.Day, Sim.S.Fon, shelter.Supplies, shelter.Materials, lib.Regions.Count(r => r.Completed), lib.Regions.Count));
            DeskUi.SetText(missionTitle, Loc.F("HAREKÂT / {0}\nSEKTÖRLER {1}/3", RegionName(selectedRegion), area?.CapturedCount ?? 0));
            for (int i = 0; i < sectorButtons.Length; i++)
            {
                var sector = area != null && i < area.Sectors.Count ? area.Sectors[i] : null;
                string check = sector != null ? Sim.LiberationRaidCheck(sector.Id) : "—";
                string status = sector?.Captured == true ? Loc.T("GERİ ALINDI") : check == null ? Loc.T("BASKINA HAZIR") : Loc.T("KİLİTLİ");
                DeskUi.SetText(sectorLabels[i], sector == null ? "—" : Loc.F("{0:00}  {1}\n{2} · {3}", i + 1, sector.Name,
                    sector.MissionKind == "loot" ? Loc.T("YAĞMA") : Loc.T("SUNUCU YIKIMI"), status));
                sectorButtons[i].interactable = !InputBlocked && check == null;
            }
            var next = Sim.NextLiberationSector(selectedRegion);
            string reason = next != null ? Sim.LiberationRaidCheck(next.Id) : null;
            string detail = area?.Completed == true
                ? Loc.T("Bölge özgür. Haritadan sınırdaki bir bölgeyi seç ve yeni cepheyi aç.")
                : next?.MissionKind == "loot"
                    ? Loc.T("Teknisyeni ikmal kasasının yanına götür. Q ile yeteneği seç ve kasaya tıklayarak malzemeleri al.")
                    : Loc.T("Sunucu çekirdeğini yık. Son sektör alınınca bu bölge özgürleşir.");
            DeskUi.SetText(missionDetail, reason == null ? detail : Loc.F("{0}\n\n{1}", detail, reason));
            chooseRegion.interactable = !InputBlocked && Sim.SelectLiberationRegionCheck(selectedRegion) == null;
            DeskUi.SetText(shelterSummary, Loc.F("SON SIĞINAK\nTOPLULUK {0}/{1}  ·  BOŞTA {2}\nENERJİ {3:0}/{4:0}  ·  ERZAK {5:0}/{6:0}\nSON VARDİYA: ERZAK {7:+0.0;-0.0;0}  ·  MALZEME {8:+0.0;-0.0;0}",
                shelter.Population, ShelterLogic.Capacity(shelter, b), ShelterLogic.AvailableWorkers(shelter),
                ShelterLogic.EnergyDemand(shelter, b), ShelterLogic.EnergyCapacity(shelter, b), shelter.Supplies,
                ShelterLogic.SuppliesCapacity(shelter, b), shelter.LastSuppliesChange, shelter.LastMaterialsChange));
            for (int i = 0; i < roomLabels.Length; i++)
            {
                var r = shelter.Room(roomIds[i]);
                DeskUi.SetText(roomLabels[i], Loc.F("{0}\n{1} · PERSONEL {2}/{3}", ShelterLogic.RoomName(r.Id),
                    r.Level == 0 ? Loc.T("HASARLI") : Loc.F("SEVİYE {0}", r.Level), r.Workers, ShelterLogic.WorkerCapacity(r, b)));
                roomButtons[i].interactable = !InputBlocked;
                var colors = roomButtons[i].colors;
                colors.normalColor = r.Id == selectedRoom ? new Color(.65f, .92f, .75f, .8f) : new Color(.9f, .87f, .76f, .65f);
                roomButtons[i].colors = colors;
            }
            var room = shelter.Room(selectedRoom);
            var cost = ShelterLogic.UpgradeCost(shelter, selectedRoom, b);
            string why = Sim.ShelterUpgradeCheck(selectedRoom);
            DeskUi.SetText(roomDetail, Loc.F("{0}\n{1}\nGeliştirme: {2:0} fon + {3:0} malzeme\n{4}", ShelterLogic.RoomName(selectedRoom),
                ShelterLogic.RoomDescription(selectedRoom), cost.Fon, cost.Materials, why ?? Loc.T("Geliştirmeye hazır.")));
            upgradeRoom.interactable = !InputBlocked && why == null;
            workerMinus.interactable = !InputBlocked && Sim.ShelterAssignCheck(selectedRoom, room.Workers - 1) == null;
            workerPlus.interactable = !InputBlocked && Sim.ShelterAssignCheck(selectedRoom, room.Workers + 1) == null;
            advanceDay.interactable = !InputBlocked && !Sim.S.Blocked && Sim.S.Ending == null;
            continueStory.interactable = game.SavedGameSummary() != null;
            DeskUi.SetText(monitorStatus, Sim.S.Raid != null
                ? Loc.T("BAĞLANTI KURULUYOR\n\nSAHA YAYINI AÇILIYOR")
                : Loc.T("SAHA YAYINI\n\nBAĞLANTI BEKLENİYOR\n\nBaskın emrini harita üzerindeki görev fişinden ver."));
            if (desk != null) desk.ExternalBlocked = storyOpen || launcher != null && launcher.IsTransitioning;
            UpdateBriefingPlacement();
        }

        ActResult Report(ActResult result, string failDetail = null, string doneDetail = null)
        {
            DeskUi.SetText(resultNote, result?.Msg ?? (result?.Ok == true ? Loc.T("Emir uygulandı.") : Loc.T("Yapılamadı.")));
            // Başarılı komutun iletisi boşsa (baskın emri, cephe) teleks satırı neyin uygulandığını yazar
            if (telex != null && result != null)
                telex.Post(Shift, result.Ok ? Loc.T("EMİR UYGULANDI") : Loc.T("YAPILAMADI"),
                    result.Ok ? (string.IsNullOrEmpty(result.Msg) ? doneDetail ?? "" : result.Msg) : failDetail ?? result.Msg,
                    result.Ok ? TelexKind.Done : TelexKind.Failed);
            source.MarkDirty();
            Refresh();
            return result;
        }

        void Assign(int delta)
        {
            if (!InputBlocked && Sim?.Shelter != null) AssignRoom(selectedRoom, Sim.Shelter.Room(selectedRoom).Workers + delta);
        }

        void Launch(int slot)
        {
            if (InputBlocked || Sim?.LiberationOn != true) return;
            var area = Sim.LiberationRegion(selectedRegion);
            if (area == null || slot >= area.Sectors.Count) return;
            LaunchSector(area.Sectors[slot].Id);
        }

        public void SelectRegion(string region)
        {
            // Yeni görünümde bölge tıklamasını kurtuluş haritası işler (görev fişi / aday)
            if (InputBlocked || string.IsNullOrEmpty(region) || NewMap) return;
            selectedRegion = region;
            commands?.SelectRegion(region);
            briefingOpen = true;
            Refresh();
            Rig.FocusMap();
        }

        public void OpenWindow(WindowId window)
        {
            if (InputBlocked) return;
            if (window == WindowId.Guide && NewBriefing) { siginakBriefing.OpenReading(); return; }
            if (window == WindowId.Guide)
            {
                storyOpen = true; storyRoot.SetActive(true); game.TutorialHold = true; desk.ExternalBlocked = true;
            }
            else if (window == WindowId.Research || window == WindowId.Teams) Rig.FocusShelter();
            else ShowPlanning();
        }
        public void ToggleWindow(WindowId window) => OpenWindow(window);
        public void RegionAction(int index) { if (index == 0 || index == 2) ShowPlanning(); }
        public void FieldOperation() => ShowPlanning();
        public void Escape()
        {
            if (storyOpen) { CloseStory(); return; }
            if (siginak != null && siginak.isActiveAndEnabled && siginak.HandleEscape()) return;
            if (!InputBlocked) SettingsView.Instance?.Open();
        }
        public void StepSpeed(int direction) { }
    }
}
