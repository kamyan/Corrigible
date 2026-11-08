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
                Platform.Current.UnlockAchievement(Achievements.ForEnding(Sim.S.Ending));
                SaveStore.Delete();
            }
            EventsRaised?.Invoke(events);
        }

        // ---------- Komutlar ----------
        public ActResult StartLiberationRaid(string sectorId, string pick = null) => Run(() => Sim.StartLiberationRaid(sectorId, pick));
        public ActResult ResolveRaid(RaidResult result) => Run(() => Sim.ResolveRaid(result));
        public ActResult SelectLiberationRegion(string regionId) => Run(() => Sim.SelectLiberationRegion(regionId));
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
            GameLoaded?.Invoke();
            Changed?.Invoke();
            return true;
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() => Save();
    }
}
