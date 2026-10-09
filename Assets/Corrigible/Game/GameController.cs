using System;
using System.Collections.Generic;
using System.IO;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Game
{
    /// <summary>
    /// TickClock + komut kapısı: gerçek zamanı oyun gününe çevirir, simülasyonu ilerletir, olayları sunuma iletir,
    /// oyunu kaydeder ve oyun sonunda profili/başarımları günceller. Sunum katmanı simülasyonu yalnızca buradan değiştirir.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        const string TypePrefKey = "corrigible.aiType";

        [SerializeField] BalanceConfig balanceConfig;
        [Tooltip("1×, 2× ve 4× hızlarda bir oyun gününün süresi (saniye).")]
        [SerializeField] float[] secondsPerDay = { 0.9f, 0.45f, 0.22f };
        [Tooltip("Deneysel modda (saha ekipleri / zaman ölçeği) bir günün ekranda kaç kat uzun süreceği.")]
        [SerializeField, Min(1)] float expDayFactor = 1.22f;
        [Tooltip("Kaç günde bir otomatik kayıt alınır. Komutlardan sonra ve uygulama kapanırken de kaydedilir.")]
        [SerializeField, Min(1)] int autosaveDays = 5;

        public Simulation Sim { get; private set; }
        public PlayerProfile Profile { get; private set; }
        public string TypeId { get; private set; } = "manipulator";
        public int Speed { get; private set; }

        // Deneysel mod ayarları: yeni oyunlarda kullanılır, oyuncu tercihinde saklanır
        const string ExpTeamsKey = "corrigible.exp.teams", ExpScaleKey = "corrigible.exp.timeScale";
        public bool ExpTeams { get; private set; } = true;
        public double ExpTimeScale { get; private set; } = 1.5;
        ExpOptions ExpForNewGame => ExpTeams || ExpTimeScale != 1 ? new ExpOptions { Teams = ExpTeams, TimeScale = ExpTimeScale } : null;

        /// <summary>Bir pencere açıkken zamanı durdurmak için sunum katmanı ayarlar.</summary>
        public bool Hold { get; set; }

        /// <summary>Eğitim bir açıklama gösterirken zamanı durdurur (pencerelerin Hold'undan bağımsız).</summary>
        public bool TutorialHold { get; set; }

        public event Action<List<SimEvent>> EventsRaised;
        public event Action Changed;
        public event Action NewGameStarted;
        public event Action GameLoaded;

        static string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

        float acc;

        void Awake()
        {
            Profile = ProfileStore.Load();
            ExpTeams = PlayerPrefs.GetInt(ExpTeamsKey, 1) == 1;
            ExpTimeScale = PlayerPrefs.GetFloat(ExpScaleKey, 1.5f);
            // İlk açılışta en kolay rakip (Solucan) seçili gelir; eğitim onunla tasarlandı
            var saved = PlayerPrefs.GetString(TypePrefKey, "solucan");
            TypeId = GameData.HasType(saved) ? saved : TypeId;
            NewGame(TypeId);
        }

        public void NewGame(string typeId)
        {
            TypeId = GameData.HasType(typeId) ? typeId : "manipulator";
            PlayerPrefs.SetString(TypePrefKey, TypeId);
            var balance = balanceConfig != null ? balanceConfig.CreateRuntimeCopy() : new Balance();
            Sim = Simulation.NewGame(Profile, TypeId, null, balance, ExpForNewGame);
            Speed = 0;
            acc = 0;
            NewGameStarted?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>Deneysel ayarları değiştirir ve aynı rakiple yeni oyun kurar (giriş penceresinden).</summary>
        public void SetExperimental(bool teams, double timeScale)
        {
            ExpTeams = teams; ExpTimeScale = timeScale;
            PlayerPrefs.SetInt(ExpTeamsKey, teams ? 1 : 0);
            PlayerPrefs.SetFloat(ExpScaleKey, (float)timeScale);
            NewGame(TypeId);
        }

        public void SetSpeed(int speed)
        {
            Speed = Mathf.Clamp(speed, 0, 4);
            Changed?.Invoke();
        }

        void Update()
        {
            if (Sim == null || Speed == 0 || Hold || TutorialHold) return;
            var s = Sim.S;
            if (s.Ending != null || s.PendingGift != null || s.Raid != null) return;
            float spd = secondsPerDay[Speed >= 4 ? 2 : Speed - 1] * (s.Exp != null ? expDayFactor : 1);
            acc += Time.unscaledDeltaTime;
            int n = 0;
            while (acc >= spd && n < 3)
            {
                acc -= spd; n++;
                Sim.Step();
                if (s.Day % autosaveDays == 0) Save();
                Flush();
                if (s.PendingGift != null || s.Ending != null || s.Raid != null || Hold) { acc = 0; break; }
            }
            if (n > 0) Changed?.Invoke();
        }

        /// <summary>Simülasyonun biriktirdiği olayları sunuma iletir; oyun sonunda profili, başarımları ve kaydı günceller.</summary>
        public void Flush()
        {
            var events = Sim.DrainEvents();
            if (events.Count == 0) return;
            foreach (var e in events)
            {
                if (e.Type == SimEventType.Veil) Platform.Current.UnlockAchievement(Achievements.Veil);
                if (e.Type != SimEventType.End) continue;
                Profile = Profiling.UpdateProfile(Profile, Sim.S);
                ProfileStore.Save(Profile);
                Platform.Current.UnlockAchievement(Achievements.ForEnding(Sim.S.Ending));
                if (Sim.S.Predictions.Exists(p => p.State == "miss")) Platform.Current.UnlockAchievement(Achievements.Misread);
                DeleteSave();
            }
            EventsRaised?.Invoke(events);
        }

        // ---------- Komutlar ----------
        public ActResult Act(string action, string regionId) => Run(() => Sim.Act(action, regionId));
        public ActResult StartResearch(string id) => Run(() => Sim.StartResearch(id));
        public ActResult ResolveGift(string choice) => Run(() => Sim.ResolveGift(choice));
        public ActResult Verify(string headlineId) => Run(() => Sim.Verify(headlineId));
        public ActResult GlobalKillSwitch() => Run(() => Sim.GlobalKillSwitch());
        public ActResult SetResearchReserve(double reserve) => Run(() => Sim.SetResearchReserve(reserve));
        public ActResult MoveTeam(string teamId, string regionId) => Run(() => Sim.MoveTeam(teamId, regionId));
        public ActResult HireTeam() => Run(() => Sim.HireTeam());
        public ActResult StartRaid(string regionId) => Run(() => Sim.StartRaid(regionId));
        public ActResult ResolveRaid(RaidResult result) => Run(() => Sim.ResolveRaid(result));

        public void DismissAdvice()
        {
            Sim.DismissAdvice();
            Changed?.Invoke();
        }

        public void ClearProfile()
        {
            ProfileStore.Clear();
            Profile = null;
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
        /// <summary>Kayıt = tohum + başlangıç koşulları + komut geçmişi. Deterministik simülasyon tekrar oynatılarak geri yüklenir.</summary>
        public void Save()
        {
            if (Sim?.Record == null || Sim.S.Ending != null) return;
            if (Sim.S.Day == 0 && Sim.Record.commands.Count == 0) return; // Henüz oynanmamış oyun eski kaydın üstüne yazılmasın
            Sim.Record.day = Sim.S.Day;
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(Sim.Record)); }
            catch (Exception e) { Debug.LogWarning($"Oyun kaydedilemedi: {e.Message}"); }
        }

        static GameRecord LoadRecord()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var rec = JsonUtility.FromJson<GameRecord>(File.ReadAllText(SavePath));
                return rec != null && GameData.HasType(rec.type) && rec.day > 0 ? rec : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Kayıt okunamadı: {e.Message}");
                return null;
            }
        }

        static void DeleteSave()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); }
            catch (Exception e) { Debug.LogWarning($"Kayıt silinemedi: {e.Message}"); }
        }

        /// <summary>Kayıtlı bir oyun varsa kısa açıklaması (giriş penceresi için), yoksa null.</summary>
        public string SavedGameSummary()
        {
            var rec = LoadRecord();
            return rec == null ? null : $"{GameData.AiType(rec.type).Name}, {rec.day}. gün";
        }

        public bool ContinueSaved()
        {
            var rec = LoadRecord();
            if (rec == null) return false;
            try
            {
                Sim = Replay.Restore(rec);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Kayıt geri yüklenemedi: {e.Message}");
                return false;
            }
            TypeId = rec.type;
            Speed = 0;
            acc = 0;
            GameLoaded?.Invoke();
            Changed?.Invoke();
            return true;
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() => Save();
    }
}
