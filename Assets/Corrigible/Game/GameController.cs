using System;
using System.Collections.Generic;
using System.IO;
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

        string SavePath => Path.Combine(Application.persistentDataPath, "liberation_savegame.json");

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
                DeleteSave();
            }
            EventsRaised?.Invoke(events);
        }

        // ---------- Komutlar ----------
        public ActResult StartLiberationRaid(string sectorId) => Run(() => Sim.StartLiberationRaid(sectorId));
        public ActResult ResolveRaid(RaidResult result) => Run(() => Sim.ResolveRaid(result));
        public ActResult SelectLiberationRegion(string regionId) => Run(() => Sim.SelectLiberationRegion(regionId));
        public ActResult ShelterUpgrade(string roomId) => Run(() => Sim.ShelterUpgrade(roomId));
        public ActResult ShelterAssign(string roomId, int workers) => Run(() => Sim.ShelterAssign(roomId, workers));
        public ActResult AnswerDoor(string option) => Run(() => Sim.AnswerDoor(option));

        /// <summary>Vardiyayı bitirir (baskın sürerken, harekât bitmişken ya da hikâye açıkken olmaz).</summary>
        public void AdvanceShift()
        {
            if (Sim == null || Sim.S.Blocked || Sim.S.Ending != null || StoryHold) return;
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
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(Sim.Record)); }
            catch (Exception e) { Debug.LogWarning($"Oyun kaydedilemedi: {e.Message}"); }
        }

        GameRecord LoadRecord()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var rec = JsonUtility.FromJson<GameRecord>(File.ReadAllText(SavePath));
                // Eski biçimdeki (kriz oyunu dönemi) kayıtlar yeniden oynatılamaz
                return rec != null && rec.version == GameRecord.CurrentVersion && (rec.day > 0 || rec.commands.Count > 0) ? rec : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Kayıt okunamadı: {e.Message}");
                return null;
            }
        }

        void DeleteSave()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); }
            catch (Exception e) { Debug.LogWarning($"Kayıt silinemedi: {e.Message}"); }
        }

        /// <summary>Kayıttaki oyunun kaydı (geri yüklemeden; özet ve sunum dosyası eşlemesi için), yoksa null.</summary>
        public GameRecord SavedRecord() => LoadRecord();

        public bool ContinueSaved()
        {
            var rec = LoadRecord();
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
