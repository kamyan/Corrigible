using System;
using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Game
{
    /// <summary>
    /// Komut kapısı: harekâtı kurar, oyuncunun komutlarını ve vardiya sonunu simülasyona iletir, olayları sunuma yayınlar,
    /// oyunu kaydeder. Sunum katmanı simülasyonu yalnızca buradan değiştirir. Zaman kendiliğinden akmaz: her vardiya
    /// oyuncunun şalteriyle (<see cref="AdvanceShift"/>) biter.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] BalanceConfig balanceConfig;

        public Simulation Sim { get; private set; }

        /// <summary>Açılış ve emir belgesi (hikâye) açıkken vardiya bitirilemez.</summary>
        public bool StoryHold { get; set; }

        public event Action<List<SimEvent>> EventsRaised;
        public event Action Changed;
        public event Action NewGameStarted;
        public event Action GameLoaded;

        void Awake() => NewGame();

        /// <summary>Yeni harekât; <paramref name="seed"/> verilirse aynı tohumla.</summary>
        public void NewGame(uint? seed = null)
        {
            var balance = balanceConfig != null ? balanceConfig.CreateRuntimeCopy() : new Balance();
            Sim = Simulation.NewGame(seed, balance);
            NewGameStarted?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>Simülasyonun biriktirdiği olayları sunuma iletir; harekât bitince başarımı açar ve kaydı siler.</summary>
        public void Flush()
        {
            var events = Sim.DrainEvents();
            if (events.Count == 0) return;
            foreach (var e in events)
            {
                if (e.Type != SimEventType.End) continue;
                // KOS aşama 6: çeşit ve ton son raporundan (eski oda yolunda null: yalnız kimlik)
                var report = Sim.EndReport;
                foreach (var id in Achievements.ForEnding(Sim.S.Ending, report?.SwitchKind, report?.Tone))
                    Platform.Current.UnlockAchievement(id);
                SaveStore.Delete();
            }
            EventsRaised?.Invoke(events);
        }

        // ---------- Komutlar ----------
        public ActResult StartLiberationRaid(string sectorId, string pick = null) => Run(() => Sim.StartLiberationRaid(sectorId, pick));
        /// <summary>Baskını kapatır: sonuç oda raporlarından türetilir (verilen sonuçtan yalnız <c>Reason</c> okunur); son oda raporlanmadıysa reddedilir.</summary>
        public ActResult ResolveRaid(RaidResult result) => Run(() => Sim.ResolveRaid(result));
        /// <summary>
        /// Tesis baskınında biten odanın raporu (<c>raid_room</c>; Docs/TESIS-BASKINLARI-TASARIMI.md §3.6): kaydedilir, kayıt
        /// dosyası yazılır. Yalnız tesis baskını oturumu çağırır.
        /// </summary>
        public ActResult AdvanceRaidRoom(RoomReport report) => Run(() => Sim.AdvanceRaidRoom(report));
        public ActResult SelectLiberationRegion(string regionId) => Run(() => Sim.SelectLiberationRegion(regionId));
        /// <summary>Tesis aşama 11: bekleyen karşı saldırıda kanadı savun (<c>siege_defend</c>) ya da bırak (<c>siege_yield</c>).</summary>
        public ActResult DefendSector() => Run(() => Sim.DefendSector());
        public ActResult YieldSector() => Run(() => Sim.YieldSector());
        public ActResult ShelterUpgrade(string roomId) => Run(() => Sim.ShelterUpgrade(roomId));
        public ActResult ShelterAssign(string roomId, int workers) => Run(() => Sim.ShelterAssign(roomId, workers));
        public ActResult AnswerDoor(string option) => Run(() => Sim.AnswerDoor(option));

        /// <summary>Planlama panosunun bütün oyuncu komutları aynı kayıt/olay kapısından geçer.</summary>
        public ActResult ExecuteBoardCommand(Func<Simulation, ActResult> command)
        {
            if (Sim == null || command == null || StoryHold) return ActResult.Fail(Loc.T("Önce açılış belgesini kapat."));
            return Run(() => command(Sim));
        }

        public ActResult PrintBoardShift() => ExecuteBoardCommand(s => s.PrintShift());

        /// <summary>Fişi kutuya koymak Core içinde tam bir vardiyayı bir kez ilerletir.</summary>
        public ActResult FileBoardShift(string receiptId) => ExecuteBoardCommand(s => s.FileShift(receiptId));

        /// <summary>Vardiyayı bitirir (baskın sürerken, harekât bitmişken ya da hikâye açıkken olmaz).</summary>
        public void AdvanceShift()
        {
            if (Sim == null || Sim.S.Blocked || Sim.S.Ending != null || StoryHold) return;
            if (Sim.Board != null) return; // Yeni düzende zaman yalnız kimliği doğrulanmış devir fişiyle ilerler.
            Sim.Step(); Flush(); Save(); Changed?.Invoke();
        }

        ActResult Run(Func<ActResult> f)
        {
            var r = f();
            if (r.Ok) Save();
            Flush();
            Changed?.Invoke();
            return r;
        }

        // ---------- Kayıt ----------
        /// <summary>Kayıt = tohum + denge değerleri + komut geçmişi. Deterministik simülasyon tekrar oynatılarak geri yüklenir.</summary>
        public void Save()
        {
            if (Sim?.Record == null || Sim.S.Ending != null) return;
            if (Sim.S.Day == 0 && Sim.Record.commands.Count == 0) return; // Henüz oynanmamış oyun eski kaydın üstüne yazılmasın
            Sim.Record.day = Sim.S.Day;
            SaveStore.Write(Sim.Record);
        }

        /// <summary>Kayıttaki oyunun kaydı (geri yüklemeden; özet ve sunum dosyası eşlemesi için), yoksa null.</summary>
        public GameRecord SavedRecord() => SaveStore.Load();

        public bool ContinueSaved()
        {
            var rec = SaveStore.Load();
            if (rec == null) return false;
            try { Sim = Replay.Restore(rec); }
            catch (Exception e)
            {
                Debug.LogWarning($"Kayıt geri yüklenemedi: {e.Message}");
                return false;
            }
            // Yeniden oynatmada başarısız komut: oyun kayıttakinden farklı bir yere dönmüş olabilir (geliştirici uyarısı)
            if (Sim.ReplayFailures > 0) Debug.LogWarning($"Kayıttan devam: {Sim.ReplayFailures} komut yeniden oynatılamadı.");
            GameLoaded?.Invoke();
            Changed?.Invoke();
            return true;
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() => Save();
    }
}
