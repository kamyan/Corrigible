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

        bool storyOpen;
        Simulation Sim => game != null ? game.Sim : null;
        /// <summary>Baskın sonucu henüz okunmadı: operasyon raporu modalı açılacak ya da açık (K3 dizisi onu bekler).</summary>
        public bool RaidReportPending => Sim != null && source != null && Sim.S.Raid == null && Sim.S.RaidLog.Count > source.State.RaidLogSeen;
        public bool InputBlocked => storyOpen || launcher != null && launcher.IsTransitioning || RaidLauncher.IsRaidLoaded ||
            SettingsView.Instance != null && SettingsView.Instance.IsOpen || GameOverView.Instance != null && GameOverView.Instance.IsOpen ||
            EventModalView.Instance != null && EventModalView.Instance.IsOpen ||
            siginak != null && siginak.isActiveAndEnabled && siginak.ShiftBusy || siginakBriefing != null && siginakBriefing.Busy;

        void Start()
        {
            game.NewGameStarted += NewGame;
            game.GameLoaded += Loaded;
            NewGame();
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

        public void ShowShelter() { if (!InputBlocked) siginakRig.FocusShelter(); }

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

        public ActResult LaunchSector(string sectorId)
        {
            if (InputBlocked) return null;
            string why = TelexReasons.Raid(Sim, sectorId);
            var sector = Sim.LiberationSector(sectorId);
            string done = sector == null ? null : Loc.F("Baskın emri: {0} · {1} · {2} fon", RegionName(sector.Region), sector.Name,
                DeskFormat.Signed(-Sim.B.liberationRaidCost, 0));
            return Report(game.StartLiberationRaid(sectorId), why, done);
        }

        /// <summary>Vardiyayı bitirir (şalterin kolu). Yapılamazsa false.</summary>
        public bool AdvanceShift()
        {
            if (InputBlocked || Sim.S.Blocked || Sim.S.Ending != null) return false;
            game.AdvanceShift();
            return true;
        }

        // ---------- Açılış ve hikâye ----------

        void NewGame()
        {
            storyOpen = true;
            game.StoryHold = true;
            if (desk != null) desk.ExternalBlocked = true;
            // Açılış dizisi ve emir belgesi (K7); BAŞLA / KAYITTAN DEVAM buraya döner (BeginCampaign, Loaded)
            siginakBriefing.OpenOpening();
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

        static string RegionName(string id) => string.IsNullOrEmpty(id) ? "—" : Loc.T(GameData.Region(id).Name);

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

        public void Escape()
        {
            if (siginak != null && siginak.isActiveAndEnabled && siginak.HandleEscape()) return;
            if (!InputBlocked) SettingsView.Instance?.Open();
        }
    }
}
