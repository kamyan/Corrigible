using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Raid;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Sığınak odasını harekât komutlarına bağlar (<see cref="IDeskSurface"/>). Komutlar buradan geçer ve sonuçları
    /// gerekçesiyle teleksten basılır; ekran ve dünya nesneleri Siginak bağlayıcılarındadır (masa, harita, sığınak, CRT,
    /// vardiya, brifing). Yeni oyun açılış dizisi ve emir belgesiyle başlar; BAŞLA ya da kayıttan devam hikâyeyi kapatır.
    /// </summary>
    public sealed class LiberationDeskView : MonoBehaviour, IDeskSurface
    {
        [SerializeField] GameController game;
        [SerializeField] SimDeskDataSource source;
        [SerializeField] SiginakCameraRig siginakRig;
        [SerializeField] CommandPostDesk desk;
        [SerializeField] RaidLauncher launcher;
        [Tooltip("Emir sonuçları ve gerekçeleri (tek geri bildirim kapısı).")]
        [SerializeField] TelexFeed telex;
        [SerializeField] SiginakDesk siginak;
        [Tooltip("Kurtuluş haritası (görev fişi, aday, ekip figürü).")]
        [SerializeField] SiginakMap siginakMap;
        [Tooltip("Açılış dizisi ve emir belgesi (K7); H onu okuma kipinde açar.")]
        [SerializeField] SiginakBriefing siginakBriefing;
        [Tooltip("Sığınağın kapısı: kâğıdı açıkken masa kilitli.")]
        [SerializeField] SiginakDoor siginakDoor;
        [Tooltip("Operasyon raporu: okunana kadar masa kilitli.")]
        [SerializeField] SiginakOperation siginakOperation;
        [Tooltip("Ayarlar dolabı: açıkken ve menüye dönerken masa kilitli.")]
        [SerializeField] SiginakCabinet siginakCabinet;
        [Tooltip("Kişi, teçhizat ve vardiya planlama panosu.")]
        [SerializeField] PlanningBoardPresenter planningBoard;
        [Tooltip("Kartlı oyunun kapısı ve kabul istasyonu (Revizyon 3 · Aşama 6): Esc önce ona gider; dizi sürerken masa kilitli.")]
        [SerializeField] SiginakAdmission admission;

        bool storyOpen;
        Simulation Sim => game != null ? game.Sim : null;
        /// <summary>Baskın sonucu henüz okunmadı: operasyon raporu açılacak ya da açık (K3 dizisi onu bekler).</summary>
        public bool RaidReportPending => Sim != null && source != null && Sim.S.Raid == null && Sim.S.RaidLog.Count > source.State.RaidLogSeen;
        public bool InputBlocked => BlockedExceptDoor || DoorBusy;
        /// <summary>Dolabın kendi odağı hariç, menüye dönüşü engelleyen kâğıt ve işlem kilitleri.</summary>
        public bool MenuExitBlocked => SceneBlocked || ShiftBusy || DoorBusy ||
            siginakMap != null && (siginakMap.SlipOpen || siginakMap.FrontSlipOpen || siginakMap.SquadReportOpen);
        /// <summary>Kapının kendisi dışındaki kilitler (kapı ancak bunlar yokken çalar ve açılır).</summary>
        public bool BlockedExceptDoor => OtherBlocked || ShiftBusy;
        /// <summary>
        /// Vardiya kapanışının kendisi dışındaki kilitler (Revizyon 3 · R19): fiş eldeyken ya da sürüklenirken masa kilitlidir ama
        /// fişin kendisi tıklanır ve EVRAK'a bırakılabilir kalır. <see cref="InputBlocked"/>'ın alt kümesidir.
        /// </summary>
        public bool BlockedExceptShift => OtherBlocked || DoorBusy;

        bool DoorBusy => siginakDoor != null && siginakDoor.isActiveAndEnabled && siginakDoor.Busy ||
            admission != null && admission.isActiveAndEnabled && admission.Busy;
        bool ShiftBusy => siginak != null && siginak.isActiveAndEnabled && siginak.ShiftBusy;
        bool OtherBlocked => SceneBlocked || SettingsCabinet.Instance != null && SettingsCabinet.Instance.IsOpen ||
            siginakCabinet != null && siginakCabinet.Leaving;
        bool SceneBlocked => storyOpen || launcher != null && launcher.IsTransitioning || RaidLauncher.IsRaidLoaded ||
            FinalReportView.Instance != null && FinalReportView.Instance.IsOpen ||
            siginakOperation != null && siginakOperation.Busy ||
            siginakBriefing != null && siginakBriefing.Busy;

        /// <summary>Operasyon raporu dosyalandı: rapor yeniden açılmaz, oda ve kamera kilidi açılır.</summary>
        public void MarkRaidReportSeen()
        {
            if (Sim == null || source == null) return;
            source.State.RaidLogSeen = Sim.S.RaidLog.Count;
            source.MarkDirty();
        }

        void Start()
        {
            game.NewGameStarted += NewGame;
            game.GameLoaded += Loaded;
            // Menüden gelindiyse (kapının dışı) karar orada verildi: devamda emir belgesi yok, yenide belgede sekme yok
            var launch = GameLaunch.Take();
            if (launch == GameLaunch.Mode.Continue)
            {
                if (game.ContinueSaved()) { siginakBriefing.OpenReturn(); return; }
                telex.Post(Shift, Loc.T("YAPILAMADI"), Loc.T("Kayıt açılamadı."), TelexKind.Failed);
            }
            OpenStory(launch != GameLaunch.Mode.None);
        }

        void OnDestroy()
        {
            if (game != null) { game.NewGameStarted -= NewGame; game.GameLoaded -= Loaded; }
        }

        // Kamera girdisi hikâye ve baskına yaklaşma boyunca kilitli
        void LateUpdate()
        {
            if (desk != null) desk.ExternalBlocked = storyOpen || launcher != null && launcher.IsTransitioning;
        }

        /// <summary>Harita pozu ve görev fişi (3 GÖREVLER).</summary>
        public void ShowPlanning()
        {
            if (!InputBlocked) siginakMap.ShowMissions();
        }

        public void ShowShelter()
        {
            if (InputBlocked) return;
            if (Sim?.Board != null && planningBoard != null) planningBoard.Show();
            else siginakRig.FocusShelter();
        }

        /// <summary>H: emir belgesi okuma kipinde.</summary>
        public void OpenGuide() { if (!InputBlocked) siginakBriefing.OpenReading(); }

        /// <summary>Şu anki vardiyanın numarası (kurtuluşta gün + 1: ilk vardiya 001).</summary>
        public int Shift => (Sim?.S.Day ?? 0) + 1;

        // ---------- Komutlar: sığınak nesneleri ve fişler aynı yolu kullanır; sonuç teleksten gerekçesiyle basılır ----------

        public ActResult UpgradeRoom(string roomId)
        {
            if (InputBlocked) return null;
            string why = TelexReasons.Upgrade(Sim, roomId);
            return Report(game.ShelterUpgrade(roomId), why);
        }

        public ActResult AssignRoom(string roomId, int workers)
        {
            if (InputBlocked) return null;
            string why = TelexReasons.Assign(Sim, roomId, workers);
            return Report(game.ShelterAssign(roomId, workers), why);
        }

        public ActResult OpenFront(string regionId)
        {
            if (InputBlocked) return null;
            string why = TelexReasons.Front(Sim, regionId);
            string done = Loc.F("Yeni cephe: {0}", RegionName(regionId));
            return Report(game.SelectLiberationRegion(regionId), why, done);
        }

        /// <param name="pick">Teknisyen'in yanına seçilen roller ("emp,maden"); boşsa varsayılan seçim.</param>
        public ActResult LaunchSector(string sectorId, string pick = null)
        {
            if (InputBlocked) return null;
            string why = TelexReasons.Raid(Sim, sectorId);
            var sector = Sim.LiberationSector(sectorId);
            string done = sector == null ? null : Loc.F("Baskın emri: {0} · {1} · {2} fon", RegionName(sector.Region), sector.Name,
                DeskFormat.Signed(-Sim.B.liberationRaidCost, 0));
            return Report(game.StartLiberationRaid(sectorId, pick), why, done);
        }

        /// <summary>
        /// Kapıdaki ziyaretçiye cevap (kapının kâğıdından; kâğıt açıkken masa kilitli olduğu için yalnız öteki kilitlere bakar).
        /// Yapılamazsa gerekçe teleksten basılır; yapılanı kapının bağlayıcısı basar.
        /// </summary>
        public ActResult AnswerDoor(string option)
        {
            if (BlockedExceptDoor) return null;
            string why = Sim.DoorAnswerCheck(option);
            var result = game.AnswerDoor(option);
            if (!result.Ok && telex != null) telex.Post(Shift, Loc.T("YAPILAMADI"), why ?? result.Msg, TelexKind.Failed);
            source.MarkDirty();
            return result;
        }

        /// <summary>Vardiyayı bitirir (şalterin kolu). Yapılamazsa false.</summary>
        public bool AdvanceShift()
        {
            if (InputBlocked || Sim.S.Blocked || Sim.S.Ending != null) return false;
            if (Sim.Board != null) return false;
            game.AdvanceShift();
            return true;
        }

        // ---------- Açılış ve hikâye ----------

        void NewGame() => OpenStory(false);

        void OpenStory(bool fromEntrance)
        {
            storyOpen = true;
            game.StoryHold = true;
            if (desk != null) desk.ExternalBlocked = true;
            // Açılış dizisi ve emir belgesi (K7); BAŞLA / KAYITTAN DEVAM buraya döner (BeginCampaign, Loaded)
            siginakBriefing.OpenOpening(fromEntrance);
        }

        /// <summary>Emir belgesinde BAŞLA: hikâye kapanır, harekât başlar.</summary>
        public void BeginCampaign() => CloseStory();

        void Loaded() => CloseStory();

        void CloseStory()
        {
            storyOpen = false;
            game.StoryHold = false;
            if (desk != null) desk.ExternalBlocked = false;
            siginakRig.FocusMap();
            source.MarkDirty();
        }

        static string RegionName(string id) => string.IsNullOrEmpty(id) ? "—" : GameData.Region(id).Name;

        ActResult Report(ActResult result, string failDetail = null, string doneDetail = null)
        {
            // Başarılı komutun iletisi boşsa (baskın emri, cephe) teleks satırı neyin uygulandığını yazar
            if (telex != null && result != null)
                telex.Post(Shift, result.Ok ? Loc.T("EMİR UYGULANDI") : Loc.T("YAPILAMADI"),
                    result.Ok ? (string.IsNullOrEmpty(result.Msg) ? doneDetail ?? "" : result.Msg) : failDetail ?? result.Msg,
                    result.Ok ? TelexKind.Done : TelexKind.Failed);
            source.MarkDirty();
            return result;
        }

        // ---------- IDeskSurface ----------

        /// <summary>
        /// Esc'in tek girişi: kartlı oyunda önce kapı ve kabul (K8 ailesinde bir seviye geri; Revizyon 3 · Aşama 6), sonra pano, sonra
        /// masanın açık kâğıtları; hiçbiri işlemezse ayarlar dolabı.
        /// </summary>
        public void Escape()
        {
            if (Sim?.Board != null && admission != null && admission.isActiveAndEnabled && admission.HandleEscape()) return;
            if (Sim?.Board != null && planningBoard != null && planningBoard.HandleEscape()) return;
            if (siginak != null && siginak.isActiveAndEnabled && siginak.HandleEscape()) return;
            if (!InputBlocked) SettingsCabinet.Instance?.RequestOpen();
        }
    }
}
