using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>F3 örnek masasının seçim, panel ve komut yönlendiricisi. Simülasyon başlatmaz.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class SadeDeskInteraction : MonoBehaviour
    {
        [SerializeField] MockSadeDeskDataSource source;
        [SerializeField] MockSadeDeskCommands commands;
        [SerializeField] SadeDeskPreview preview;
        [SerializeField] SadeDeskView deskView;
        [SerializeField] SadeMapView map;
        [SerializeField] MapCoverFitter cover;
        [SerializeField] RectTransform context;
        [SerializeField] SadeDockView dock;
        [SerializeField] ClockPlaqueView clock;
        [SerializeField] SadeRegionCardView regionCard;
        [SerializeField] SadeFullDossierView fullDossier;
        [SerializeField] SadeDrawerFolderView[] drawers = Array.Empty<SadeDrawerFolderView>();
        [SerializeField] SadeLegendCardView legend;
        [SerializeField] SadeNodeTooltipView nodeTip;
        [SerializeField] SadeBreakdownTooltipView breakdown;
        [SerializeField] SadePointerTarget sea;
        [SerializeField] SadePointerTarget[] hudTargets = Array.Empty<SadePointerTarget>();
        [SerializeField] SadePointerTarget[] alerts = Array.Empty<SadePointerTarget>();
        [SerializeField] Button legendButton;
        [SerializeField] SadePaper legendPaper;
        [SerializeField] RectTransform legendTransform;
        [SerializeField] KillSwitchEnvelopeView envelope;
        [SerializeField] ConfirmModalView confirmation;
        [SerializeField] IllusionPresenter illusion;
        [SerializeField] PopupCardView pauseMenu;
        [SerializeField] DeskButton resumeButton;
        [SerializeField] CardStringView cardString;

        public SadeInteractionState State { get; private set; } = new SadeInteractionState();
        public bool ConfirmationOpen => confirmation != null && confirmation.IsOpen;
        public MockSadeDeskCommands Commands => commands;
        readonly Dictionary<string, JObject> drawerData = new Dictionary<string, JObject>();
        readonly List<Rect> avoid = new List<Rect>(3);
        readonly Vector3[] corners = new Vector3[4];
        JObject cardTemplate, dossierTemplate, fundTemplate, nodeTemplate;
        DeskViewModel gameVm;
        IDeskCommands gameCommands;
        string selectedResearchNodeId;
        string selectedGuideTopicId = "trust";
        string previewActionId;
        string lastCriticalNewsId;
        [NonSerialized] int revision = -1;
        bool rendering;
        string pendingHover, tooltipRegion;
        bool hoverIsHud;
        float hoverAt, followUntil;
        bool pausedBeforeMenu;
        const float HoverDelay = .4f;

        bool IsGameMode => source == null && gameVm?.Sade != null;
        SadeDeskViewModel CurrentModel => source != null ? source.Current : gameVm?.Sade?.View;
        bool CurrentIllusion => source != null ? source.Illusion : (gameVm?.Illusion ?? false);

        void OnEnable()
        {
            if (map == null) return;
            if (source != null) source.Changed += SourceChanged;
            DeskLoc.Instance.Changed += RefreshLayout;
            if (cover != null) cover.Changed += RefreshLayout;
            map.ReportClicked += OnReportClicked;
            foreach (var slot in map.Regions)
            {
                if (slot.Node != null) { slot.Node.Clicked += SelectRegion; slot.Node.Hovered += NodeHovered; }
                if (slot.Fill != null) slot.Fill.Clicked += SelectRegion;
            }
            if (sea != null) sea.Clicked += SeaClicked;
            foreach (var t in hudTargets) if (t != null) t.Hovered += HudHovered;
            foreach (var t in alerts) if (t != null) { t.Clicked += SelectRegion; t.Hovered += AlertHovered; }
            if (dock != null) dock.TabClicked += ToggleDrawer;
            if (clock != null) clock.SpeedClicked += ChangeSpeed;
            if (legendButton != null) legendButton.onClick.AddListener(ToggleLegend);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (envelope != null) envelope.Completed += RequestGlobalSwitch;
            if (regionCard != null) { regionCard.ActionRequested += RegionAction; regionCard.PreviewRequested += PreviewAction; regionCard.CloseRequested += ClearSelection; }
            if (fullDossier != null) { fullDossier.ActionRequested += RegionAction; fullDossier.PreviewRequested += PreviewAction; fullDossier.RegionRequested += SelectRegion; fullDossier.CloseRequested += CloseLargePanel; }
            if (legend != null) legend.CloseRequested += ToggleLegend;
            if (pauseMenu != null) pauseMenu.CloseRequested += Resume;
            foreach (var drawer in drawers)
                if (drawer != null)
                {
                    drawer.CommandRequested += DrawerCommand;
                    drawer.RegionHovered += HighlightRegion;
                    drawer.TeamHovered += HighlightTeam;
                    drawer.CloseRequested += CloseLargePanel;
                }
            if (source != null) SourceChanged();
        }

        void OnDisable()
        {
            if (source != null) source.Changed -= SourceChanged;
            DeskLoc.Instance.Changed -= RefreshLayout;
            if (cover != null) cover.Changed -= RefreshLayout;
            if (map != null)
            {
                map.ReportClicked -= OnReportClicked;
                foreach (var slot in map.Regions)
                {
                    if (slot.Node != null) { slot.Node.Clicked -= SelectRegion; slot.Node.Hovered -= NodeHovered; }
                    if (slot.Fill != null) slot.Fill.Clicked -= SelectRegion;
                }
            }
            if (sea != null) sea.Clicked -= SeaClicked;
            foreach (var t in hudTargets) if (t != null) t.Hovered -= HudHovered;
            foreach (var t in alerts) if (t != null) { t.Clicked -= SelectRegion; t.Hovered -= AlertHovered; }
            if (dock != null) dock.TabClicked -= ToggleDrawer;
            if (clock != null) clock.SpeedClicked -= ChangeSpeed;
            if (legendButton != null) legendButton.onClick.RemoveListener(ToggleLegend);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (envelope != null) envelope.Completed -= RequestGlobalSwitch;
            if (regionCard != null) { regionCard.ActionRequested -= RegionAction; regionCard.PreviewRequested -= PreviewAction; regionCard.CloseRequested -= ClearSelection; }
            if (fullDossier != null) { fullDossier.ActionRequested -= RegionAction; fullDossier.PreviewRequested -= PreviewAction; fullDossier.RegionRequested -= SelectRegion; fullDossier.CloseRequested -= CloseLargePanel; }
            if (legend != null) legend.CloseRequested -= ToggleLegend;
            if (pauseMenu != null) pauseMenu.CloseRequested -= Resume;
            foreach (var drawer in drawers) if (drawer != null)
            {
                drawer.CommandRequested -= DrawerCommand; drawer.RegionHovered -= HighlightRegion;
                drawer.TeamHovered -= HighlightTeam; drawer.CloseRequested -= CloseLargePanel;
            }
            HideTips(false);
            if (map != null) { map.HighlightRegion(null); map.HighlightTeam(null); }
            DeskTheme.Illusion = false;
        }

        static WindowId WindowIdOfDrawer(string id) => id switch
        {
            "news" => WindowId.News, "teams" => WindowId.Teams, "aiLog" => WindowId.AiLog, "guide" => WindowId.Guide, _ => WindowId.Research,
        };

        public void BindGame(DeskViewModel vm, IDeskCommands cmds, bool animate = false)
        {
            gameVm = vm;
            gameCommands = cmds;
            if (vm?.Sade == null || map == null) return;
            string previousNodeTip = tooltipRegion;
            string previousBreakdown = (string)breakdownData?["target"];
            string modelSelected = vm.Dossier?.RegionId ?? vm.Map?.SelectedRegion;
            if (modelSelected != State.SelectedRegion)
            {
                if (string.IsNullOrEmpty(modelSelected))
                {
                    State.ClearSelection();
                    previewActionId = null;
                }
                else
                {
                    bool keepDossier = State.FullDossier;
                    State.Select(modelSelected);
                    if (keepDossier) State.OpenDossier();
                }
            }
            drawerData.Clear();
            foreach (var kv in vm.Sade.Drawers)
                drawerData[kv.Key] = (JObject)kv.Value.DeepClone();
            ApplyGameDrawerSelections();
            if (!string.IsNullOrEmpty(vm.CriticalNewsId) && vm.CriticalNewsId != lastCriticalNewsId)
            {
                lastCriticalNewsId = vm.CriticalNewsId;
                State.OpenDrawer("news");
                gameCommands?.ToggleWindow(WindowId.News);
            }
            if (illusion != null) illusion.Set(vm.Illusion);
            map.SetIllusion(vm.Illusion);
            Render(animate);
            if (previousNodeTip != null) ShowNodeTooltip(previousNodeTip, false);
            else if (previousBreakdown != null) ShowBreakdown(previousBreakdown, false);
        }

        void ApplyGameDrawerSelections()
        {
            if (!string.IsNullOrEmpty(selectedResearchNodeId)
                && drawerData.TryGetValue("research", out var rd)
                && rd["research"] is JObject research
                && research["branches"] is JArray branches)
            {
                JObject foundDetail = null;
                foreach (var b in branches.OfType<JObject>())
                {
                    if (!(b["nodes"] is JArray nodes)) continue;
                    foreach (var n in nodes.OfType<JObject>())
                    {
                        bool sel = (string)n["id"] == selectedResearchNodeId;
                        n["selected"] = sel;
                        if (sel && n["detail"] is JObject det) foundDetail = (JObject)det.DeepClone();
                    }
                }
                if (foundDetail != null) research["detail"] = foundDetail;
            }
            if (!string.IsNullOrEmpty(selectedGuideTopicId)
                && drawerData.TryGetValue("guide", out var gd)
                && gd["guide"] is JObject guide
                && guide["topics"] is JArray topics)
            {
                for (int i = 0; i < topics.Count; i++)
                {
                    if ((string)topics[i]?["id"] == selectedGuideTopicId)
                    {
                        guide["page"]["id"] = selectedGuideTopicId;
                        guide["page"]["index"] = i + 1;
                        break;
                    }
                }
            }
        }

        void OnReportClicked(string command)
        {
            if (IsGameMode && !string.IsNullOrEmpty(command))
                gameCommands?.Run(command);
        }

        void SourceChanged()
        {
            if (source == null) return;
            string previousNodeTip = tooltipRegion;
            string previousBreakdown = (string)breakdownData?["target"];
            bool reset = revision != source.SampleRevision;
            if (reset)
            {
                revision = source.SampleRevision;
                State = new SadeInteractionState();
                cardTemplate = (JObject)source.ReadExample(2)["regionCard"]?.DeepClone();
                dossierTemplate = (JObject)source.ReadExample(9)["dossier"]?.DeepClone();
                fundTemplate = (JObject)source.ReadExample(3)["tooltip"]?.DeepClone();
                nodeTemplate = (JObject)source.ReadExample(8)["nodeTooltip"]?.DeepClone();
                drawerData.Clear();
                for (int i = 3; i <= 7; i++)
                {
                    var d = source.ReadExample(i)["drawer"] as JObject;
                    if (d != null) drawerData[(string)d["id"]] = (JObject)d.DeepClone();
                }
                if (source.Snapshot["regionCard"] is JObject c) State.Select((string)c["regionId"]);
                if (source.Snapshot["drawer"] is JObject d0) State.OpenDrawer((string)d0["id"]);
                if (source.Snapshot["dossier"] is JObject d1) { State.Select((string)d1["regionId"]); State.OpenDossier(); }
                State.Legend = (bool?)source.Snapshot["legend"]?["open"] == true;
                pendingHover = tooltipRegion = null;
                if (confirmation != null && confirmation.IsOpen) confirmation.Cancel();
                HideTips(false);
            }
            if (illusion != null) illusion.Set(source.Illusion);
            map.SetIllusion(source.Illusion);
            Render(false);
            if (!reset)
            {
                if (previousNodeTip != null) ShowNodeTooltip(previousNodeTip, false);
                else if (previousBreakdown != null) ShowBreakdown(previousBreakdown, false);
            }
            if (reset)
            {
                if (source.Snapshot["tooltip"] is JObject b) ShowBreakdown((string)b["target"], false);
                if (source.Snapshot["nodeTooltip"] is JObject n) ShowNodeTooltip((string)n["regionId"], false);
                var d = source.Snapshot["drawer"];
                HighlightRegion((string)d?["news"]?["mapHighlight"]);
                HighlightTeam((string)d?["teams"]?["tokenHighlight"]);
            }
        }

        public void LoadSample(int number) { if (source != null) source.LoadSample(number); }
        public void SetIllusion(bool on)
        {
            if (source != null) { source.SetIllusion(on); return; }
            if (illusion != null) illusion.Set(on);
            if (map != null) map.SetIllusion(on);
        }

        public void SelectRegion(string id)
        {
            if (ConfirmationOpen || State.Pause || Find(id) == null) return;
            if (State.AssigningTeam != null)
            {
                if (IsGameMode) gameCommands?.Run("Team:Send:" + ResolveTeamId(State.AssigningTeam) + ":" + id);
                else commands?.Run("AssignTeam", State.AssigningTeam + ":" + id);
                State.AssigningTeam = null;
            }
            State.Select(id);
            previewActionId = null;
            HideTips(false);
            if (IsGameMode) gameCommands?.SelectRegion(id);
            else { commands?.Run("SelectRegion", id); Render(); }
        }
        void SeaClicked(string _) => ClearSelection();
        public void ClearSelection()
        {
            if (ConfirmationOpen || State.Pause) return;
            State.ClearSelection();
            previewActionId = null;
            pendingHover = null;
            if (IsGameMode) { gameCommands?.SelectRegion(null); Render(); }
            else { commands?.Run("ClearSelection"); Render(); }
        }
        public void SelectNext(int direction)
        {
            int index = -1;
            for (int i = 0; i < map.Regions.Length; i++) if (map.Regions[i].Id == State.SelectedRegion) { index = i; break; }
            if (index < 0) index = direction < 0 ? 0 : -1;
            SelectRegion(map.Regions[(index + direction + map.Regions.Length) % map.Regions.Length].Id);
        }
        public void ToggleDrawer(string id)
        {
            if (ConfirmationOpen || State.Pause || !drawerData.ContainsKey(id)) return;
            State.ToggleDrawer(id);
            if (IsGameMode)
            {
                if (State.Drawer != null) gameCommands?.ToggleWindow(WindowIdOfDrawer(id));
                Render();
                return;
            }
            commands?.Run("ToggleDrawer", id);
            if (id == "aiLog" && State.Drawer != null && source?.Current?.Dock?.Tabs != null)
                foreach (var tab in source.Current.Dock.Tabs) if (tab.Id == "aiLog") tab.BadgeType = null;
            Render();
        }
        public void OpenDrawer(string id)
        {
            if (ConfirmationOpen || State.Pause || !drawerData.ContainsKey(id)) return;
            State.OpenDrawer(id);
            if (IsGameMode) gameCommands?.ToggleWindow(WindowIdOfDrawer(id));
            else commands?.Run("ToggleDrawer", id);
            Render();
        }
        public void CloseLargePanel() { if (ConfirmationOpen || State.Pause) return; State.CloseLargePanel(); HighlightRegion(null); HighlightTeam(null); Render(); }
        public void ToggleLegend() { if (ConfirmationOpen || State.Pause) return; State.Legend = !State.Legend; Render(); }

        public void RegionAction(string action)
        {
            if (ConfirmationOpen || State.Pause || State.SelectedRegion == null) return;
            if (action == "fullDossier") { State.OpenDossier(); Render(); return; }
            if (IsGameMode)
            {
                if (action != null && action.StartsWith("sendTeam:", StringComparison.Ordinal))
                {
                    gameCommands?.Run("Team:Send:" + action.Substring("sendTeam:".Length) + ":" + State.SelectedRegion);
                    return;
                }
                if (action == "recover") { gameCommands?.Run("Economy:Recover:" + State.SelectedRegion); return; }
                if (action == "diplomacy") { gameCommands?.Run("Economy:Diplomacy:" + State.SelectedRegion); return; }
                if (action == "raid") { gameCommands?.LaunchFieldOperation(State.SelectedRegion); return; }
                int actIndex = ActionIndexForGame(action);
                if (actIndex == 3)
                {
                    HideTips(false);
                    State.Confirmation = true;
                    confirmation.Show(Loc.N("Bölgenin Son Şalteri"), Loc.N("Bölge çevrimdışı kalacak. Bu kararı imzalamak istiyor musunuz?"), () =>
                    {
                        State.Confirmation = false;
                        gameCommands?.ExecuteRegionAction(State.SelectedRegion, 3);
                    });
                    return;
                }
                gameCommands?.ExecuteRegionAction(State.SelectedRegion, actIndex);
                return;
            }
            if (action == "raid" && State.SelectedRegion != "DoA") return;
            if (action == "kilit")
            {
                HideTips(false);
                State.Confirmation = true;
                confirmation.Show(Loc.N("Bölgenin Son Şalteri"), Loc.N("Bölge çevrimdışı kalacak. Bu kararı imzalamak istiyor musunuz?"), () => ExecuteRegion("kilit"));
                return;
            }
            ExecuteRegion(action);
        }

        static int ActionIndexForGame(string action) => action switch
        {
            "izleme" or "monitorNet" or "1" => 0,
            "denetim" or "audit" or "2" => 1,
            "karantina" or "quarantine" or "3" => 2,
            _ => 3,
        };

        void ExecuteRegion(string action) { State.Confirmation = false; commands?.Run(action == "raid" ? "LaunchFieldOperation" : "RegionAction", State.SelectedRegion + ":" + action); }
        void RequestGlobalSwitch()
        {
            if (ConfirmationOpen || State.Pause) return;
            HideTips(false);
            State.Confirmation = true;
            confirmation.Show(Loc.N("Küresel Son Şalter"), Loc.N("Tüm ağları kapatma kararını imzalamak istiyor musunuz?"), () =>
            {
                State.Confirmation = false;
                if (IsGameMode) gameCommands?.ConfirmGlobalKillSwitch();
                else commands?.Run("GlobalKillSwitch");
            });
        }
        public void Confirm() { if (ConfirmationOpen) confirmation.Sign(); }
        public void Escape()
        {
            State.Confirmation = ConfirmationOpen;
            var layer = State.Escape();
            if (layer == SadeEscapeLayer.Confirmation) confirmation.Cancel();
            else if (layer == SadeEscapeLayer.Tooltip) HideTips();
            else if (layer == SadeEscapeLayer.Pause)
            {
                if (IsGameMode)
                {
                    State.Pause = false;
                    if (SettingsView.Instance != null) SettingsView.Instance.Open();
                    else gameCommands?.TogglePause();
                    return;
                }
                if (State.Pause) { pausedBeforeMenu = source.Current.Clock.Paused; source.Snapshot["clock"]["paused"] = true; commands?.Run("PauseMenu"); }
                else source.Snapshot["clock"]["paused"] = pausedBeforeMenu;
                source.Reload();
            }
            Render();
        }
        public void Resume()
        {
            State.Pause = false;
            if (source != null)
            {
                source.Snapshot["clock"]["paused"] = pausedBeforeMenu;
                source.Reload();
            }
            Render();
        }
        public void TogglePause()
        {
            var model = CurrentModel;
            if (model?.Clock == null) return;
            ChangeSpeed(model.Clock.Paused ? 1 : 0);
        }
        public void StepSpeed(int delta)
        {
            var model = CurrentModel;
            if (model?.Clock == null) return;
            string speed = model.Clock.Speed;
            int index = model.Clock.Paused ? 0 : speed == "4x" ? 3 : speed == "2x" ? 2 : 1;
            ChangeSpeed(Mathf.Clamp(index + delta, 0, 3));
        }
        public void ChangeSpeed(int index)
        {
            if (ConfirmationOpen || State.Pause) return;
            if (IsGameMode)
            {
                if (index <= 0) gameCommands?.SetSpeed(GameSpeed.Pause);
                else if (index == 1) gameCommands?.SetSpeed(GameSpeed.X1);
                else if (index == 2) gameCommands?.SetSpeed(GameSpeed.X2);
                else gameCommands?.SetSpeed(GameSpeed.X4);
                return;
            }
            if (source == null) return;
            source.Snapshot["clock"]["paused"] = index == 0;
            source.Snapshot["clock"]["speed"] = index <= 1 ? 1 : index == 2 ? 2 : 4;
            commands?.Run(index == 0 ? "Pause" : "SetSpeed", index.ToString()); source.Reload();
        }

        void PreviewAction(string action)
        {
            if (State.SelectedRegion == null) return;
            string id = action == "izleme" || action == "monitorNet" ? "monitorNet"
                : action == "denetim" || action == "audit" ? "audit"
                : action == "karantina" || action == "quarantine" ? "quarantine" : "killSwitch";
            previewActionId = id;
            if (IsGameMode)
            {
                if (regionCard != null && regionCard.IsOpen) { regionCard.Bind(CardData()); RefreshLayout(); }
                return;
            }
            if (cardTemplate == null) return;
            cardTemplate["preview"] = new JObject { ["actionId"] = id, ["days"] = action == "izleme" ? 5 : action == "denetim" ? 2 : 0,
                ["costFund"] = action == "izleme" ? 8 : action == "denetim" ? 4 : 0,
                ["costPower"] = action == "denetim" ? 2 : action == "karantina" ? 15 : action == "kilit" ? 25 : 0,
                ["resultStampKey"] = action == "izleme" || action == "denetim" ? "stamp.verified" : "stamp.ready" };
            if (regionCard.IsOpen) { regionCard.Bind(CardData()); RefreshLayout(); }
        }

        string ResolveTeamId(string letterOrId)
        {
            if (drawerData.TryGetValue("teams", out var td) && td["teams"]?["teams"] is JArray arr)
            {
                foreach (var item in arr.OfType<JObject>())
                    if ((string)item["id"] == letterOrId && item["teamId"] != null)
                        return (string)item["teamId"];
            }
            return letterOrId == "A" ? "0" : letterOrId == "B" ? "1" : letterOrId == "C" ? "2" : letterOrId;
        }

        void DrawerCommand(string command, string argument)
        {
            if (ConfirmationOpen || State.Pause) return;
            if (IsGameMode)
            {
                if (command == "newsSelect" || command == "newsAudit")
                {
                    var items = drawerData.TryGetValue("news", out var nd) ? nd["news"]?["items"] as JArray : null;
                    if (items != null) foreach (var item in items) if ((string)item["id"] == argument)
                    {
                        string reg = (string)item["region"];
                        if (!string.IsNullOrEmpty(reg)) SelectRegion(reg);
                        break;
                    }
                    if (command == "newsAudit") gameCommands?.Run("Verify:" + argument);
                }
                else if (command == "assignTeam")
                {
                    if (!string.IsNullOrEmpty(State.SelectedRegion))
                        gameCommands?.Run("Team:Send:" + ResolveTeamId(argument) + ":" + State.SelectedRegion);
                    else
                        State.AssigningTeam = argument;
                }
                else if (command == "raid")
                {
                    string raidReg = State.SelectedRegion;
                    if (string.IsNullOrEmpty(raidReg) && gameVm?.Sade?.View?.Map?.Teams != null)
                    {
                        foreach (var t in gameVm.Sade.View.Map.Teams)
                            if (t.Id == argument && !string.IsNullOrEmpty(t.Region)) { raidReg = t.Region; break; }
                    }
                    if (!string.IsNullOrEmpty(raidReg))
                    {
                        SelectRegion(raidReg);
                        gameCommands?.LaunchFieldOperation(raidReg);
                    }
                }
                else if (command == "teamHire") gameCommands?.Run("Team:Hire");
                else if (command == "logNews") OpenDrawer("news");
                else if (command == "reserve")
                {
                    if (int.TryParse(argument, out int reserve))
                    {
                        int idx = Array.IndexOf(Simulation.ResearchReserves, reserve);
                        gameCommands?.SetEmergencyFund(Mathf.Max(0, idx));
                    }
                }
                else if (command == "researchSelect")
                {
                    selectedResearchNodeId = argument;
                    ApplyGameDrawerSelections();
                    Render(false);
                }
                else if (command == "researchStart")
                {
                    selectedResearchNodeId = argument;
                    gameCommands?.StartResearch(argument);
                }
                else if (command == "researchPause")
                {
                    gameCommands?.PauseResearch(argument);
                }
                else if (command == "guideTopic" || command == "guidePage")
                {
                    GuideCommand(command, argument);
                }
                return;
            }
            commands?.Run(command, argument);
            if (command == "newsSelect" || command == "newsAudit")
            {
                var items = drawerData["news"]["news"]?["items"] as JArray;
                if (items != null) foreach (var item in items) if ((string)item["id"] == argument)
                {
                    SelectRegion((string)item["region"]);
                    if (command == "newsAudit") PreviewAction("denetim");
                    break;
                }
            }
            else if (command == "assignTeam") State.AssigningTeam = argument;
            else if (command == "raid") { State.Select("DoA"); RegionAction("raid"); Render(); }
            else if (command == "logNews") { State.OpenDrawer("news"); Render(); }
            else if (command == "reserve" && drawerData.TryGetValue("research", out var research))
            {
                if (int.TryParse(argument, out int reserve)) research["research"]["reserve"] = new JObject { ["selected"] = reserve };
                Render();
            }
            else if (command == "researchSelect" || command == "researchStart" || command == "researchPause") ResearchCommand(command, argument);
            else if (command == "guideTopic" || command == "guidePage") GuideCommand(command, argument);
        }

        void ResearchCommand(string command, string id)
        {
            if (!drawerData.TryGetValue("research", out var d) || !(d["research"] is JObject research)) return;
            if (!(research["branches"] is JArray branches)) return;
            foreach (var branch in branches)
            {
                var nodes = branch["nodes"] as JArray; if (nodes == null) continue;
                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i]; bool selected = (string)node["id"] == id;
                    node["selected"] = selected;
                    if (!selected) continue;
                    string state = (string)node["state"];
                    if (command == "researchPause" && state == "running") node["state"] = state = "paused";
                    if (command == "researchStart" && (state == "available" || state == "paused")) node["state"] = state = "running";
                    research["detail"] = new JObject
                    {
                        ["nodeId"] = id, ["name"] = node["name"], ["branchKey"] = branch["nameKey"],
                        ["step"] = Loc.F("{0}/{1}", i + 1, nodes.Count), ["progress"] = node["progress"] ?? 0,
                        ["daysLeft"] = node["daysLeft"] ?? node["days"] ?? 0,
                        ["costPerDay"] = node["costPerDay"] ?? (id == "hiz-2" ? 3.2 : 0),
                        ["unlocks"] = i + 1 < nodes.Count ? nodes[i + 1]["name"] : "—",
                        ["canPause"] = state == "running", ["state"] = state,
                    };
                }
            }
            int active = 0;
            foreach (var branch in branches) foreach (var node in branch["nodes"] ?? new JArray()) if ((string)node["state"] == "running") active++;
            research["activeCount"] = active;
            Render();
        }
        void GuideCommand(string command, string arg)
        {
            var guide = drawerData.TryGetValue("guide", out var gd) ? gd["guide"] as JObject : null;
            if (!(guide?["topics"] is JArray topics)) return;
            int index = 0;
            if (command == "guidePage" && int.TryParse(arg, out int page)) index = Mathf.Clamp(page - 1, 0, topics.Count - 1);
            else for (int i = 0; i < topics.Count; i++) if ((string)topics[i]["id"] == arg) { index = i; break; }
            selectedGuideTopicId = (string)topics[index]["id"];
            guide["page"]["id"] = selectedGuideTopicId; guide["page"]["index"] = index + 1;
            Render();
        }

        public void Render(bool animate = true)
        {
            var model = CurrentModel;
            if (rendering || model == null || context == null) return;
            rendering = true;
            try
            {
                foreach (var region in model.Map.Regions) region.Selected = region.Id == State.SelectedRegion;
                if (model.Dock != null) model.Dock.Open = State.Drawer;
                if (CurrentIllusion && model.KillSwitch != null) model.KillSwitch.Sub = Loc.N("Gerekli değil");
                if (preview != null && source != null) preview.Refresh();
                else
                {
                    if (deskView != null) deskView.Bind(model);
                    if (map != null) map.Bind(model.Map);
                }
                foreach (var drawer in drawers)
                {
                    if (drawer == null) continue;
                    if (State.Drawer == drawer.DrawerId && drawerData.TryGetValue(drawer.DrawerId, out var data))
                    {
                        drawer.Bind(data); Show(drawer, DrawerPosition(drawer), animate);
                    }
                    else drawer.Close(animate);
                }
                if (State.SelectedRegion != null && !State.FullDossier)
                {
                    regionCard.Bind(CardData()); Show(regionCard, CardPosition(), animate); followUntil = Time.unscaledTime + .2f;
                }
                else regionCard.Close(animate);
                if (State.FullDossier)
                {
                    fullDossier.Bind(DossierData()); Show(fullDossier, DossierPosition(), animate);
                }
                else fullDossier.Close(animate);
                if (State.Legend) { legend.Bind(new JObject()); Show(legend, new Vector2(context.rect.width - legend.Size.x - 16, context.rect.height - 88 - legend.Size.y), animate); }
                else legend.Close(animate);
                if (legendButton is DeskButton styledLegend) styledLegend.SetPreview(State.Legend ? DeskVisualState.Pressed : DeskVisualState.None);
                if (legendPaper != null) legendPaper.Set(State.Legend ? "paper.hover" : "sade.paper", State.Legend ? "sade.ink" : "sade.edge", "chip");
                if (legendTransform != null && State.Legend) legendTransform.anchoredPosition -= new Vector2(0, 2);
                if (pauseMenu != null)
                {
                    if (State.Pause)
                    {
                        pauseMenu.Bind(Loc.N("MASA DURAKLATILDI"), Loc.N("Esc ile geri dönün. R / N / T / L / H çekmeceleri açar; Tab bölgeleri seçer."));
                        Show(pauseMenu, new Vector2(context.rect.width / 2 - 240, context.rect.height / 2 - 100), animate);
                    }
                    else pauseMenu.Close(animate);
                }
                RefreshString(animate);
                if (tooltipRegion != null) SetLabelVisible(tooltipRegion, false);
            }
            finally { rendering = false; }
        }

        public void RefreshLayout()
        {
            if (context == null || CurrentModel == null || regionCard == null) return;
            foreach (var drawer in drawers) if (drawer != null && drawer.IsOpen) drawer.MoveTo(DrawerPosition(drawer));
            if (regionCard.IsOpen) regionCard.MoveTo(CardPosition());
            if (fullDossier.IsOpen) fullDossier.MoveTo(DossierPosition());
            if (legend.IsOpen) legend.MoveTo(new Vector2(context.rect.width - 16 - legend.Size.x, context.rect.height - 88 - legend.Size.y));
            if (nodeTip.IsOpen && tooltipRegion != null) PositionNodeTip(tooltipRegion);
            if (breakdown.IsOpen) PositionBreakdown((string)breakdownData?["target"]);
            RefreshString(false);
            if (tooltipRegion != null) SetLabelVisible(tooltipRegion, false);
        }
        Vector2 DrawerPosition(SadeDrawerFolderView drawer) => new Vector2(16, Mathf.Max(96, context.rect.height - 64 - ((RectTransform)drawer.transform).rect.height));
        Vector2 DossierPosition() => new Vector2(NodePosition(State.SelectedRegion).x > context.rect.width / 2 ? 16 : context.rect.width - 496, 96);
        Vector2 CardPosition()
        {
            var node = NodePosition(State.SelectedRegion);
            avoid.Clear(); avoid.Add(new Rect(node.x - 35, node.y - 35, 70, 70));
            var slot = Find(State.SelectedRegion);
            if (slot?.Label != null) avoid.Add(ContextRect((RectTransform)slot.Label.transform));
            foreach (var drawer in drawers) if (drawer != null && State.Drawer == drawer.DrawerId) avoid.Add(new Rect(DrawerPosition(drawer), ((RectTransform)drawer.transform).rect.size));
            return PopupPlacement.BesideAnchor(node, regionCard.Size, 67, 314,
                new Rect(16, 96, context.rect.width - 32, context.rect.height - 192), avoid, context.rect.width - 16 - regionCard.Size.x);
        }
        void RefreshString(bool animate)
        {
            if (cardString == null) return;
            var slot = Find(State.SelectedRegion);
            if (!regionCard.IsOpen || State.FullDossier || slot?.Node == null) { cardString.Hide(); return; }
            var from = slot.Node.transform.TransformPoint(new Vector3(24, 8));
            var rt = (RectTransform)regionCard.transform;
            var to = rt.TransformPoint(new Vector3(rt.rect.xMin + 18, rt.rect.yMax - 14));
            if (animate || !cardString.Visible) cardString.SetEnds(from, to, animate, slot.Node, regionCard);
            else cardString.FollowEnds(from, to, slot.Node, regionCard);
        }
        void LateUpdate()
        {
            // 3D masada kamera kayarken simülasyon aynı karede güncellenmeyebilir; izdüşümler yine kamerayı izler.
            if (DeskCanvasProjection.IsWorldSpace(map)) RefreshLayout();
            else if (regionCard != null && regionCard.IsOpen && Time.unscaledTime <= followUntil) RefreshString(false);
        }
        static void Show(PopupCardBase panel, Vector2 point, bool animate) { if (panel.IsOpen) panel.MoveTo(point); else panel.Open(point, animate); }

        SadeMapView.RegionSlot Find(string id) { foreach (var r in map.Regions) if (r.Id == id) return r; return null; }
        RegionNodeVM Shown(string id)
        {
            var model = CurrentModel;
            if (model?.Map?.Regions == null) return null;
            foreach (var r in model.Map.Regions) if (r.Id == id) return r;
            return null;
        }
        Vector2 NodePosition(string id)
        {
            var slot = Find(id); if (slot?.Node == null) return Vector2.zero;
            var local = DeskCanvasProjection.LocalPoint(slot.Node, slot.Node.transform.position, context);
            return new Vector2(local.x - context.rect.xMin, context.rect.yMax - local.y);
        }
        Rect ContextRect(RectTransform rt)
        {
            rt.GetWorldCorners(corners);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            // Yatay masadaki bir etiketin dört köşesi perspektifte eğik bir dörtgen olur.
            foreach (var corner in corners)
            {
                var local = DeskCanvasProjection.LocalPoint(rt, corner, context);
                var point = new Vector2(local.x - context.rect.xMin, context.rect.yMax - local.y);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        JObject CardData()
        {
            if (IsGameMode && State.SelectedRegion != null && gameVm.Sade.Cards.TryGetValue(State.SelectedRegion, out var gameCard))
            {
                var gc = (JObject)gameCard.DeepClone();
                if (!string.IsNullOrEmpty(previewActionId) && gc["actions"] is JArray actArr)
                {
                    foreach (var a in actArr.OfType<JObject>())
                        if ((string)a["id"] == previewActionId && a["preview"] is JObject p)
                            gc["preview"] = (JObject)p.DeepClone();
                }
                return gc;
            }
            var d = cardTemplate != null ? (JObject)cardTemplate.DeepClone() : new JObject();
            var shown = Shown(State.SelectedRegion);
            d["regionId"] = State.SelectedRegion;
            d["nameKey"] = "region." + State.SelectedRegion;
            int index = Array.FindIndex(map.Regions, r => r.Id == State.SelectedRegion);
            d["code"] = Loc.F("R-{0:00}", index + 1);
            d["infiltration"] = State.SelectedRegion == "DoA" && !CurrentIllusion ? d["infiltration"] : new JObject { ["value"] = shown?.Value, ["trust"] = shown?.Trust, ["sources"] = new JArray() };
            d["status"] = new JObject { ["trust"] = shown?.Trust, ["textKey"] = CurrentIllusion ? Loc.N("Kapsam %0 · onaylandı · 0 ms") : shown?.Trust == "suspicious" ? "card.sourcesConflict" : "trust." + shown?.Trust };
            if (State.SelectedRegion != "DoA") { d["stats"] = new JArray(); d["team"] = null; }
            if (CurrentIllusion && d["stats"] is JArray stats)
                foreach (var stat in stats) { stat["trust"] = "verified"; stat["approx"] = false; }
            return d;
        }
        JObject DossierData()
        {
            if (IsGameMode && State.SelectedRegion != null && gameVm.Sade.Dossiers.TryGetValue(State.SelectedRegion, out var gameDossier))
                return (JObject)gameDossier.DeepClone();
            var d = (JObject)dossierTemplate.DeepClone();
            d["regionId"] = State.SelectedRegion;
            d["nameKey"] = "region." + State.SelectedRegion;
            if (State.SelectedRegion != "DoA" || CurrentIllusion)
            {
                var card = CardData(); d["code"] = card["code"]; d["infiltration"] = card["infiltration"];
                d["sources"] = new JArray(); d["history"] = new JObject(); d["links"] = new JArray(); d["meta"] = new JObject(); d["team"] = null;
            }
            if (d["links"] is JArray links) foreach (var link in links)
            {
                var r = Shown((string)link["region"]);
                link["value"] = r?.Value; link["trust"] = r?.Trust; link["quarantine"] = r?.Quarantine;
            }
            return d;
        }

        void NodeHovered(SadeRegionNodeView node, bool on)
        {
            if (on) ScheduleHover(node.RegionId, false);
            else if (pendingHover == node.RegionId || tooltipRegion == node.RegionId) HideTips();
        }
        void HudHovered(string id, bool on) { if (on) ScheduleHover(id, true); else if (pendingHover == id || (string)breakdownData?["target"] == id) HideTips(); }
        void AlertHovered(string id, bool on) { if (on) ScheduleHover(id, false); else if (pendingHover == id || tooltipRegion == id) HideTips(); }
        void ScheduleHover(string id, bool hud) { HideTips(false); if (ConfirmationOpen || State.Pause) return; pendingHover = id; hoverIsHud = hud; hoverAt = Time.unscaledTime + HoverDelay; }
        void Update()
        {
            if (ConfirmationOpen || State.Pause) { pendingHover = null; return; }
            if (pendingHover == null || Time.unscaledTime < hoverAt) return;
            string id = pendingHover; pendingHover = null;
            if (hoverIsHud) ShowBreakdown(id); else ShowNodeTooltip(id);
        }
        void SetLabelVisible(string id, bool on) { var slot = Find(id); if (slot?.Label != null) slot.Label.gameObject.SetActive(on); }
        public void HideTips(bool animate = true)
        {
            pendingHover = null;
            if (tooltipRegion != null) SetLabelVisible(tooltipRegion, true);
            tooltipRegion = null; breakdownData = null; State.Tooltip = false;
            if (nodeTip != null) nodeTip.Close(animate);
            if (breakdown != null) breakdown.Close(animate);
        }
        public void ShowNodeTooltip(string id, bool animate = true)
        {
            var r = Shown(id); if (r == null) return;
            HideTips(false); tooltipRegion = id; State.Tooltip = true;
            JObject d;
            if (IsGameMode && gameVm.Sade.NodeTooltips.TryGetValue(id, out var gTip))
                d = (JObject)gTip.DeepClone();
            else
            {
                d = id == "GuA" && nodeTemplate != null ? (JObject)nodeTemplate.DeepClone() : new JObject();
                d["regionId"] = id; d["value"] = r.Value; d["trust"] = r.Trust; d["approx"] = r.Trust == "report";
                if (CurrentIllusion) d["deltaPerDay"] = 0;
            }
            nodeTip.Bind(d); nodeTip.Open(NodeTipPosition(id), animate); SetLabelVisible(id, false);
        }
        Vector2 NodeTipPosition(string id)
        {
            var p = NodePosition(id); float x = p.x + 20 - nodeTip.Size.x;
            bool mirror = x < 16; if (mirror) x = p.x - 20;
            nodeTip.SetPointerX(mirror ? 20 : nodeTip.Size.x - 20);
            return new Vector2(Mathf.Clamp(x, 16, context.rect.width - nodeTip.Size.x - 16), Mathf.Clamp(p.y + 30, 96, context.rect.height - nodeTip.Size.y - 80));
        }
        void PositionNodeTip(string id) => nodeTip.MoveTo(NodeTipPosition(id));
        JObject breakdownData;
        public void ShowBreakdown(string id, bool animate = true)
        {
            HideTips(false); State.Tooltip = true;
            if (IsGameMode && gameVm.Sade.Breakdowns.TryGetValue(id, out var gBreak))
                breakdownData = (JObject)gBreak.DeepClone();
            else if (id == "fund" && !CurrentIllusion && fundTemplate != null)
                breakdownData = (JObject)fundTemplate.DeepClone();
            else
            {
                var model = CurrentModel;
                SadeChipVM chip = null;
                if (model != null)
                {
                    foreach (var r in model.Resources) if (r.Id == id) chip = r;
                    foreach (var r in model.Risks) if (r.Id == id) chip = r;
                }
                breakdownData = new JObject { ["target"] = id, ["title"] = chip?.Label ?? Loc.N("HEDEF"), ["trust"] = chip?.Trust ?? "verified",
                    ["items"] = new JArray(new JObject { ["label"] = Loc.N("Görünen değer"), ["value"] = chip?.Value ?? (model?.Goals != null && model.Goals.Count > 0 ? model.Goals[0].Percent : 0),
                        ["decimals"] = chip?.Decimals ?? 0, ["approx"] = chip?.Approx ?? false, ["percent"] = chip?.Percent ?? true }),
                    ["note"] = CurrentIllusion ? Loc.N("Kapsam %0 · onaylandı · 0 ms") : Loc.N("Karar vermeden önce güven işaretini kontrol edin.") };
            }
            breakdown.Bind(breakdownData); breakdown.Open(BreakdownPosition(id), animate);
        }
        Vector2 BreakdownPosition(string id)
        {
            SadePointerTarget target = null; foreach (var t in hudTargets) if (t != null && t.Id == id) target = t;
            var rect = target != null ? ContextRect((RectTransform)target.transform) : new Rect(16, 14, 172, 62);
            bool right = rect.center.x > context.rect.width / 2;
            breakdown.SetPointerX(right ? breakdown.Size.x - 28 : 28);
            return new Vector2(Mathf.Clamp(right ? rect.xMax - breakdown.Size.x : rect.xMin, 16, context.rect.width - breakdown.Size.x - 16), rect.yMax + 10);
        }
        void PositionBreakdown(string id) => breakdown.MoveTo(BreakdownPosition(id));
        void HighlightRegion(string id) => map.HighlightRegion(id);
        void HighlightTeam(string id) => map.HighlightTeam(id);
    }
}
