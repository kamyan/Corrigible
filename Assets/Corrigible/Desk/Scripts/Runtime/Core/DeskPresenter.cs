using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masanın kök bileşeni (PROMPT §4.2): veri kaynağını alır, görünüm modelini alt görünümlere <c>Bind</c> ile dağıtır,
    /// kaynak değişince yeniden bağlar ve görünümlerin olaylarını niyet (<see cref="IDeskCommands"/>) olarak yayınlar.
    /// Pencerelerin açık/kapalı durumu görünüme aittir (<see cref="WindowManager"/>); kuralları oyun uygular.
    /// </summary>
    public sealed class DeskPresenter : MonoBehaviour
    {
        [Tooltip("IDeskDataSource uygulayan bileşen (sahte kaynak ya da oyun köprüsü).")]
        [SerializeField] MonoBehaviour dataSource;
        [Tooltip("IDeskCommands uygulayan bileşen.")]
        [SerializeField] MonoBehaviour commands;

        [SerializeField] TopRailView rail;
        [SerializeField] GaugeCellView[] gauges = System.Array.Empty<GaugeCellView>();
        [SerializeField] AdvisorBandView advisor;
        [SerializeField] GoalCardView goal;
        [SerializeField] ResearchFolderView research;
        [SerializeField] MapView map;
        [SerializeField] RegionDossierView dossier;
        [SerializeField] TelexStripView telex;
        [SerializeField] DockView dock;
        [SerializeField] KillSwitchEnvelopeView killSwitch;
        [SerializeField] WindowManager windows;
        [SerializeField] ContentWindowView[] contentWindows = System.Array.Empty<ContentWindowView>();
        [SerializeField] ConfirmModalView confirm;
        [SerializeField] IllusionPresenter illusion;
        [SerializeField] ClueOverlay clues;
        [SerializeField] EventModalView eventModal;
        [SerializeField] GameOverView gameOver;

        [Header("F8.3 ekranları")]
        [SerializeField] IntroView intro;
        [SerializeField] CampaignView campaign;
        [SerializeField] FakeWinView fakeWin;
        [SerializeField] ToastStackView toasts;
        [SerializeField] SettingsView settingsView;
        [Tooltip("Eğitim katmanı (F8.4).")]
        [SerializeField] DeskTutorialView tutorial;
        [Tooltip("IDeskSettings uygulayan bileşen (oyunda ses tercihleri, sahte kaynakta bellek).")]
        [SerializeField] MonoBehaviour settingsSource;

        IDeskDataSource source;
        DeskViewModel vm;
        bool syncing;
        string focusedCriticalNews;
        string pendingCriticalNews;

        public IDeskDataSource Source => source;
        public IDeskCommands Commands => commands as IDeskCommands;
        public TelexStripView Telex => telex;
        public WindowManager Windows => windows;

        void Start()
        {
            source = dataSource as IDeskDataSource;
            if (source == null) { Debug.LogError("[Desk] Veri kaynağı IDeskDataSource değil", this); return; }
            source.Changed += Bind;
            if (map.Selection != null) map.Selection.SetTarget(dossier.Window.Tab);
            Wire();
            Bind(source.Current);
        }

        void OnDestroy()
        {
            if (source != null) source.Changed -= Bind;
        }

        // ------------------------------------------------------------------ görünüm olayları → niyet
        void Wire()
        {
            var cmd = Commands;
            var sp = rail.Speeds;
            if (sp.Length >= 4)
            {
                sp[0].onClick.AddListener(() => cmd.TogglePause());
                sp[1].onClick.AddListener(() => cmd.SetSpeed(GameSpeed.X1));
                sp[2].onClick.AddListener(() => cmd.SetSpeed(GameSpeed.X2));
                sp[3].onClick.AddListener(() => cmd.SetSpeed(GameSpeed.X4));
            }
            advisor.ActionInvoked += c =>
            {
                if (c == "DismissAdvisor") cmd.DismissAdvisor();
                // Pencere açmak görünümün işi; niyet yine yayınlanır (okundu işaretleri için)
                else if (c != null && c.StartsWith("OpenWindow:") && System.Enum.TryParse(c.Substring("OpenWindow:".Length), out WindowId w)) OpenWindow(w);
                else cmd.RunAdvisorAction(c);
            };
            goal.NextLink.onClick.AddListener(() => OpenWindow(WindowId.Research));
            research.ResearchAction += (id, start) => { if (start) cmd.StartResearch(id); else cmd.PauseResearch(id); };
            research.FundSelected += cmd.SetEmergencyFund;
            map.NodeClicked += id => cmd.SelectRegion(id);
            for (int i = 0; i < dossier.Actions.Count; i++)
            {
                var card = dossier.Actions[i];
                card.Clicked += c => RegionAction(c.Index);
                card.Hovered += (c, on) => dossier.ShowPreviewFor(on ? c.Index : -1);
            }
            dossier.Raid.onClick.AddListener(FieldOperation);
            dossier.SendInvoked += c => { if (windows.IsOpen(WindowId.Dossier)) Run(c); };
            telex.ItemClicked += () => OpenWindow(WindowId.News);
            foreach (var tab in dock.Tabs) tab.Clicked += t => ToggleWindow(IdOf(t.TabId));
            killSwitch.Completed += () => confirm.Show(Loc.N("Küresel ağları kapat"),
                Loc.N("Bütün bölgelerin ağları aynı anda karartılır. Geri dönüşü yok."), () => cmd.ConfirmGlobalKillSwitch());
            if (eventModal != null)
            {
                eventModal.Signed += i => cmd.SignEventOption(i);
                eventModal.AdvisorLink += c => cmd.RunAdvisorAction(c);
            }
            if (gameOver != null)
                gameOver.ActionInvoked += c =>
                {
                    switch (c)
                    {
                        case "MainMenu": cmd.ReturnToMainMenu(); break;
                        case "ReviewLogs": cmd.ReviewLogs(); break;
                        case "RetrySameSeed": cmd.RetrySameSeed(); break;
                        case "Restart": case null: case "": cmd.Restart(); break;
                        default: cmd.Run(c); break;   // ör. kampanya: sonraki kriz
                    }
                };
            // F8.3: yeni ekranlar komut dizisini yayınlar; ayarları açmak görünümün işi
            if (settingsView != null) settingsView.Init(settingsSource as IDeskSettings);
            if (rail.Settings != null && settingsView != null) rail.Settings.onClick.AddListener(settingsView.Toggle);
            if (intro != null) intro.Invoked += Run;
            if (campaign != null) campaign.Invoked += Run;
            if (eventModal != null) eventModal.DocInvoked += Run;
            if (fakeWin != null)
            {
                fakeWin.Glitched += () => cmd.Run("FakeWin:Glitch");
                fakeWin.Finished += () => cmd.Run("FakeWin:Done");
            }
            if (toasts != null) toasts.Dismissed += id => cmd.Run("Toast:Dismiss:" + id);
            if (tutorial != null) tutorial.Invoked += Run;
            foreach (var w in contentWindows) w.Invoked += Run;
            windows.Changed += SyncDock;
            windows.Closed += id =>
            {
                // Açık karar 9: dosya kapanınca seçim kalkar, iplik kaybolur
                if (id == WindowId.Dossier && !syncing && vm?.Dossier != null) cmd.SelectRegion(null);
            };
        }

        void Run(string c)
        {
            if (string.IsNullOrEmpty(c)) return;
            if (c == "Settings:Open") { if (settingsView != null) settingsView.Open(); return; }
            Commands.Run(c);
        }

        // ------------------------------------------------------------------ bağlama
        /// <summary>
        /// Edit mod önizlemesi (editördeki DeskEditPreview): Start çalışmadan pencereler ve seçim ipliği kurulur, VM bağlanır.
        /// Komut bağlantıları kurulmaz; yalnızca görünüm.
        /// </summary>
        public void Preview(DeskViewModel model)
        {
            if (Application.isPlaying || model == null) return;
            windows.EnsureInit();
            if (map.Selection != null) map.Selection.SetTarget(dossier.Window.Tab);
            Bind(model);
        }

        public void Bind(DeskViewModel model)
        {
            if (model == null) return;
            vm = model;
            // İllüzyon sunumu önce (palet, damgalar, analist katmanı); görünümler yeni VM'yi bu bayrakla çizer
            if (illusion != null) illusion.Set(model.Illusion); else DeskTheme.Illusion = model.Illusion;
            rail.Bind(model.Rail, model.Clock);
            for (int i = 0; i < gauges.Length; i++)
            {
                bool on = model.Gauges != null && i < model.Gauges.Count;
                DeskUi.SetActive(gauges[i], on);
                if (on) gauges[i].Bind(model.Gauges[i]);
            }
            advisor.Bind(model.Advisor);
            goal.Bind(model.Goal);
            research.Bind(model.Research);
            map.Bind(model.Map);
            dossier.Bind(model.Dossier);
            telex.Bind(model.Telex);
            dock.Bind(model.Dock);
            killSwitch.Bind(model.KillSwitch);
            foreach (var w in contentWindows) w.Bind(model);
            if (clues != null) clues.Bind(model.Clues);
            if (eventModal != null) eventModal.Bind(model.Event);
            if (gameOver != null) gameOver.Bind(model.GameOver);
            if (intro != null) intro.Bind(model.Intro);
            if (campaign != null) campaign.Bind(model.Campaign);
            if (fakeWin != null) fakeWin.Bind(model.FakeWin);
            if (toasts != null) toasts.Bind(model.Toasts);
            if (tutorial != null) tutorial.Bind(model.Tutorial);

            // Seçim varsa dosya açılır, yoksa kapanır
            syncing = true;
            if (model.Dossier != null) windows.Open(WindowId.Dossier);
            else windows.Close(WindowId.Dossier);
            syncing = false;
            SyncDock();
            if (model.CriticalNewsId == null) { focusedCriticalNews = null; pendingCriticalNews = null; }
            else if (focusedCriticalNews != model.CriticalNewsId)
                pendingCriticalNews = model.CriticalNewsId;
            TryPresentCriticalNews();
        }

        // Karar onayının kapanması yeni simülasyon verisi gerektirmez; bekleyen haber o karede sunulur.
        void LateUpdate() => TryPresentCriticalNews();

        void TryPresentCriticalNews()
        {
            if (pendingCriticalNews == null || !Application.isPlaying || Commands == null) return;
            if (confirm != null && confirm.IsOpen) return;
            if (SettingsView.Instance != null && SettingsView.Instance.IsOpen) return;
            focusedCriticalNews = pendingCriticalNews;
            pendingCriticalNews = null;
            OpenWindow(WindowId.News);
            foreach (var w in contentWindows) w.FocusNews(focusedCriticalNews);
        }

        void SyncDock()
        {
            foreach (var tab in dock.Tabs) tab.SetOpen(windows.IsOpen(IdOf(tab.TabId)));
            if (map.Selection != null) map.Selection.SetSuppressed(!windows.IsFront(WindowId.Dossier));
        }

        static WindowId IdOf(string tabId) => tabId switch
        {
            "news" => WindowId.News, "teams" => WindowId.Teams, "aiLog" => WindowId.AiLog, "guide" => WindowId.Guide, _ => WindowId.Research,
        };

        // ------------------------------------------------------------------ klavye ve kısayollar
        public void ToggleWindow(WindowId id) { windows.Toggle(id); Commands.ToggleWindow(id); }

        public void OpenWindow(WindowId id) { windows.Open(id); Commands.ToggleWindow(id); }

        /// <summary>1–4: dosyadaki bölge eylemi (tıklamayla aynı); tehlikeli eylem (Son Şalter) onay ister.</summary>
        public void RegionAction(int index)
        {
            var d = vm?.Dossier;
            if (d == null || !windows.IsOpen(WindowId.Dossier) || index < 0 || index >= d.Actions.Count) return;
            var a = d.Actions[index];
            if (!string.IsNullOrEmpty(a.DisabledReason)) return;
            if (a.Danger)
                confirm.Show(DeskLoc.Instance.T(a.Title) + ": " + DeskLoc.Instance.T(d.Cover?.Name),
                    Loc.N("Bölge karartılır; sızma sıfırlanır ama ekonomi çöker. Geri dönüşü yok."),
                    () => Commands.ExecuteRegionAction(d.RegionId, index));
            else Commands.ExecuteRegionAction(d.RegionId, index);
        }

        /// <summary>B: dosyadaki saha komutu (baskın ya da ekip gönder; VM tuş verdiyse açık).</summary>
        public void FieldOperation()
        {
            var d = vm?.Dossier;
            if (string.IsNullOrEmpty(d?.Field?.Raid?.Key) || !windows.IsOpen(WindowId.Dossier)) return;
            Commands.LaunchFieldOperation(d.RegionId);
        }

        /// <summary>Esc: en üstteki pencereyi kapatır; pencere yoksa ayarlar (eski oyundaki gibi; ayarlar yoksa duraklat).</summary>
        public void Escape()
        {
            if (confirm != null && confirm.IsOpen) return;
            var top = windows.Top;
            if (top != null) windows.Close(top.Id);
            else if (settingsView != null && settingsSource is IDeskSettings) settingsView.Open();
            else Commands.TogglePause();
        }

        /// <summary>+/−: hız 1× ↔ 2× ↔ 4× (açık karar 1).</summary>
        public void StepSpeed(int dir)
        {
            var c = vm?.Clock;
            if (c == null) return;
            string[] order = { "1x", "2x", "4x" };
            int i = Mathf.Clamp(System.Array.IndexOf(order, c.Speed) + dir, 0, 2);
            Commands.SetSpeed(i == 0 ? GameSpeed.X1 : i == 1 ? GameSpeed.X2 : GameSpeed.X4);
        }
    }
}
