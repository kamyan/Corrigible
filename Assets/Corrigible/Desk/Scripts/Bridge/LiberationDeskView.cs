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
        readonly string[] roomIds = { ShelterLogic.Living, ShelterLogic.Generator, ShelterLogic.Workshop, ShelterLogic.Infirmary };
        IDeskCommands commands;
        string selectedRegion, selectedRoom = ShelterLogic.Living;
        bool storyOpen, lastBlocked, briefingOpen = true;
        readonly Vector2[] linkPoints = new Vector2[2];
        Simulation Sim => game != null ? game.Sim : null;
        public bool InputBlocked => storyOpen || launcher != null && launcher.IsTransitioning || RaidLauncher.IsRaidLoaded ||
            SettingsView.Instance != null && SettingsView.Instance.IsOpen || GameOverView.Instance != null && GameOverView.Instance.IsOpen ||
            EventModalView.Instance != null && EventModalView.Instance.IsOpen || ConfirmModalView.Instance != null && ConfirmModalView.Instance.IsOpen;

        void Start()
        {
            for (int i = 0; i < sectorButtons.Length; i++) { int slot = i; sectorButtons[i].onClick.AddListener(() => Launch(slot)); }
            for (int i = 0; i < roomButtons.Length; i++) { int slot = i; roomButtons[i].onClick.AddListener(() => { if (InputBlocked) return; selectedRoom = roomIds[slot]; Refresh(); }); }
            foreach (var r in map.Regions)
            {
                if (r.Node != null) r.Node.Clicked += SelectRegion;
                if (r.Fill != null) r.Fill.Clicked += SelectRegion;
            }
            chooseRegion.onClick.AddListener(() => { if (!InputBlocked) Report(game.SelectLiberationRegion(selectedRegion)); });
            upgradeRoom.onClick.AddListener(() => { if (!InputBlocked) Report(game.ShelterUpgrade(selectedRoom)); });
            workerMinus.onClick.AddListener(() => Assign(-1));
            workerPlus.onClick.AddListener(() => Assign(1));
            advanceDay.onClick.AddListener(() => { if (!InputBlocked) { game.AdvanceLiberationDay(); Refresh(); } });
            beginStory.onClick.AddListener(CloseStory);
            continueStory.onClick.AddListener(() => { if (game.ContinueSaved()) CloseStory(); });
            commandStation.onClick.AddListener(ShowPlanning);
            shelterStation.onClick.AddListener(() => { if (!InputBlocked) cameraRig.FocusShelter(); });
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

        void ShowPlanning()
        {
            if (InputBlocked) return;
            briefingOpen = true;
            cameraRig.FocusMap();
            Refresh();
        }

        void UpdateBriefingPlacement()
        {
            if (briefingRoot == null || briefingCanvas == null) return;
            bool show = briefingOpen && !InputBlocked && cameraRig.IsFocused && Sim?.LiberationOn == true;
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
            storyRoot.SetActive(true);
            source.State.IntroOpen = false;
            Refresh();
        }

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
            cameraRig.FocusMap();
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
                    sector.MissionKind == "loot" ? Loc.T("LOOT") : Loc.T("SUNUCU YIKIMI"), status));
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

        void Report(ActResult result)
        {
            DeskUi.SetText(resultNote, result?.Msg ?? (result?.Ok == true ? Loc.T("Emir uygulandı.") : Loc.T("Yapılamadı.")));
            source.MarkDirty();
            Refresh();
        }

        void Assign(int delta)
        {
            if (!InputBlocked && Sim?.Shelter != null) Report(game.ShelterAssign(selectedRoom, Sim.Shelter.Room(selectedRoom).Workers + delta));
        }

        void Launch(int slot)
        {
            if (InputBlocked || Sim?.LiberationOn != true) return;
            var area = Sim.LiberationRegion(selectedRegion);
            if (area == null || slot >= area.Sectors.Count) return;
            Report(game.StartLiberationRaid(area.Sectors[slot].Id));
        }

        public void SelectRegion(string region)
        {
            if (InputBlocked || string.IsNullOrEmpty(region)) return;
            selectedRegion = region;
            commands?.SelectRegion(region);
            briefingOpen = true;
            Refresh();
            cameraRig.FocusMap();
        }

        public void OpenWindow(WindowId window)
        {
            if (InputBlocked) return;
            if (window == WindowId.Guide)
            {
                storyOpen = true; storyRoot.SetActive(true); game.TutorialHold = true; desk.ExternalBlocked = true;
            }
            else if (window == WindowId.Research || window == WindowId.Teams) cameraRig.FocusShelter();
            else ShowPlanning();
        }
        public void ToggleWindow(WindowId window) => OpenWindow(window);
        public void RegionAction(int index) { if (index == 0 || index == 2) ShowPlanning(); }
        public void FieldOperation() => ShowPlanning();
        public void Escape() { if (storyOpen) CloseStory(); else if (!InputBlocked) SettingsView.Instance?.Open(); }
        public void StepSpeed(int direction) { }
    }
}
