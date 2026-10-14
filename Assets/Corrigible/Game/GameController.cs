using System;
using System.Collections.Generic;
using System.IO;
using Corrigible.Core;
using Corrigible.Game.Campaign;
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

        // Aşamalı öğrenme ve olay kartları (Unity'ye özgü kurallar; kapalıyken temel kurallar = JS paritesi)
        const string OnboardingOffKey = "corrigible.onboarding.off", EventsKey = "corrigible.rules.events";
        /// <summary>Oyuncu aşamalı öğrenmeyi kapattı: bütün kurallar ilk oyundan açık.</summary>
        public bool OnboardingOff { get; private set; }
        public bool EventsEnabled { get; private set; } = true;
        /// <summary>Yeni oyunun öğrenme aşaması (1–4): oynanan oyun sayısıyla açılır.</summary>
        public int OnboardingStage => OnboardingOff ? 4 : Mathf.Clamp((Profile?.runs ?? 0) + 1, 1, 4);

        /// <summary>Süren kampanya (yoksa null).</summary>
        public CampaignState Campaign { get; private set; }

        RuleOptions RulesForNewGame()
        {
            if (Campaign != null) return Campaign.Rules();
            int stage = OnboardingStage;
            // Bütün kurallar açık ve olay yoksa temel kurallar (kayıt ve parite için null)
            // Olay kartları aşamalı öğrenmede 2. oyundan itibaren
            bool events = EventsEnabled && stage >= 2;
            return stage >= 4 && !events ? null : new RuleOptions { Stage = stage, Events = events };
        }

        /// <summary>Bir pencere açıkken zamanı durdurmak için sunum katmanı ayarlar.</summary>
        public bool Hold { get; set; }

        /// <summary>Eğitim bir açıklama gösterirken zamanı durdurur (pencerelerin Hold'undan bağımsız).</summary>
        public bool TutorialHold { get; set; }

        /// <summary>Günün ilerleyişi (0–1); zaman durunca donar. Analist masasının saati için.</summary>
        public float DayProgress
        {
            get
            {
                if (Sim == null) return 0;
                float spd = secondsPerDay[Speed >= 4 ? 2 : Mathf.Max(0, Speed - 1)] * (Sim.S.Exp != null ? expDayFactor : 1);
                return spd > 0 ? Mathf.Clamp01(acc / spd) : 0;
            }
        }

        /// <summary>1× hızda bir oyun gününün gerçek süresi (saniye); masanın saati bu hızla ilerler.</summary>
        public float SecondsPerDay(int speed) =>
            secondsPerDay[speed >= 4 ? 2 : Mathf.Max(0, speed - 1)] * (Sim?.S.Exp != null ? expDayFactor : 1);

        public event Action<List<SimEvent>> EventsRaised;
        public event Action Changed;
        public event Action NewGameStarted;
        public event Action GameLoaded;

        static string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

        float acc;

        void Awake()
        {
            ReadSettings();
            if (Campaign != null && Campaign.Finished) { CampaignState.Delete(); Campaign = null; }
            NewGame(Campaign != null ? Campaign.CurrentType : TypeId);
        }

        /// <summary>Oyuncu tercihleri, profil ve kampanya (yalnız okur).</summary>
        void ReadSettings()
        {
            Profile = ProfileStore.Load();
            ExpTeams = PlayerPrefs.GetInt(ExpTeamsKey, 1) == 1;
            ExpTimeScale = PlayerPrefs.GetFloat(ExpScaleKey, 1.5f);
            OnboardingOff = PlayerPrefs.GetInt(OnboardingOffKey, 0) == 1;
            EventsEnabled = PlayerPrefs.GetInt(EventsKey, 1) == 1;
            Campaign = CampaignState.Load();
            // İlk açılışta en kolay rakip (Solucan) seçili gelir; eğitim onunla tasarlandı
            var saved = PlayerPrefs.GetString(TypePrefKey, "solucan");
            TypeId = GameData.HasType(saved) ? saved : TypeId;
        }

        /// <summary>
        /// Edit mod önizlemesi (analist masası): açılışta kurulacak oyunun aynısını sabit tohumla kurar. Tercih, kayıt, profil ve
        /// kampanya dosyasına yazmaz, olay yayınlamaz; yalnız bu bileşenin (kaydedilmeyen) ayar alanlarını okunan değerlerle
        /// doldurur. Play'de o anki oyunu döndürür.
        /// </summary>
        public Simulation PreviewOpeningGame(uint seed = 1)
        {
            if (Application.isPlaying) return Sim;
            ReadSettings();
            if (Campaign != null && Campaign.Finished) Campaign = null;
            if (Campaign != null) TypeId = Campaign.CurrentType;
            TypeId = GameData.HasType(TypeId) ? TypeId : "manipulator";
            return CreateSim(seed);
        }

        public void NewGame(string typeId) => NewGame(typeId, null);

        /// <summary>Yeni oyun; <paramref name="seed"/> verilirse aynı tohumla (analist masası: “Aynı tohumla dene”).</summary>
        public void NewGame(string typeId, uint? seed)
        {
            if (Campaign != null) typeId = Campaign.CurrentType;
            TypeId = GameData.HasType(typeId) ? typeId : "manipulator";
            if (Campaign == null) PlayerPrefs.SetString(TypePrefKey, TypeId);
            Sim = CreateSim(seed);
            Speed = 0;
            acc = 0;
            NewGameStarted?.Invoke();
            Changed?.Invoke();
        }

        Simulation CreateSim(uint? seed)
        {
            var balance = balanceConfig != null ? balanceConfig.CreateRuntimeCopy() : new Balance();
            var exp = ExpForNewGame;
            Campaign?.Apply(balance, exp);
            return Simulation.NewGame(Profile, TypeId, seed, balance, exp, RulesForNewGame());
        }

        /// <summary>Deneysel ayarları değiştirir ve aynı rakiple yeni oyun kurar (giriş penceresinden).</summary>
        public void SetExperimental(bool teams, double timeScale)
        {
            ExpTeams = teams; ExpTimeScale = timeScale;
            PlayerPrefs.SetInt(ExpTeamsKey, teams ? 1 : 0);
            PlayerPrefs.SetFloat(ExpScaleKey, (float)timeScale);
            NewGame(TypeId);
        }

        public void SetOnboardingOff(bool off)
        {
            OnboardingOff = off;
            PlayerPrefs.SetInt(OnboardingOffKey, off ? 1 : 0);
            NewGame(TypeId);
        }

        public void SetEventsEnabled(bool on)
        {
            EventsEnabled = on;
            PlayerPrefs.SetInt(EventsKey, on ? 1 : 0);
            NewGame(TypeId);
        }

        // ---------- Kampanya ----------
        public void StartCampaign()
        {
            DeleteSave();
            Campaign = CampaignState.New();
            Campaign.Save();
            NewGame(Campaign.CurrentType);
        }

        /// <summary>Krizi bitince seçilen yükseltmeyle sonraki krize geçer; kampanya bittiyse kapatır.</summary>
        public void NextCrisis(string upgradeId)
        {
            if (Campaign == null) return;
            Campaign.Advance(upgradeId);
            if (Campaign.Finished) { EndCampaign(); return; }
            Campaign.Save();
            NewGame(Campaign.CurrentType);
        }

        public void EndCampaign()
        {
            CampaignState.Delete();
            Campaign = null;
            DeleteSave();
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
            if (s.Ending != null || s.Blocked) return;
            float spd = secondsPerDay[Speed >= 4 ? 2 : Speed - 1] * (s.Exp != null ? expDayFactor : 1);
            acc += Time.unscaledDeltaTime;
            int n = 0;
            while (acc >= spd && n < 3)
            {
                acc -= spd; n++;
                Sim.Step();
                if (s.Day % autosaveDays == 0) Save();
                Flush();
                if (s.Blocked || s.Ending != null || Hold) { acc = 0; break; }
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
                if (Campaign != null) { Campaign.Record(Sim.S.Ending, Sim.S.Day); Campaign.Save(); }
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
        public ActResult BuyOperation(string id) => Run(() => Sim.BuyOperation(id));
        public ActResult SetResearchPaused(bool paused) => Run(() => Sim.SetResearchPaused(paused));
        public ActResult StartRecovery(string id) => Run(() => Sim.StartRecovery(id));
        public ActResult DiplomaticTalk(string id) => Run(() => Sim.DiplomaticTalk(id));
        public ActResult ResolveGift(string choice) => Run(() => Sim.ResolveGift(choice));
        public ActResult Verify(string headlineId) => Run(() => Sim.Verify(headlineId));
        public ActResult GlobalKillSwitch() => Run(() => Sim.GlobalKillSwitch());
        public ActResult SetResearchReserve(double reserve) => Run(() => Sim.SetResearchReserve(reserve));
        public ActResult MoveTeam(string teamId, string regionId) => Run(() => Sim.MoveTeam(teamId, regionId));
        public ActResult HireTeam() => Run(() => Sim.HireTeam());
        public ActResult StartRaid(string regionId) => Run(() => Sim.StartRaid(regionId));
        public ActResult ResolveRaid(RaidResult result) => Run(() => Sim.ResolveRaid(result));
        public ActResult ResolveEvent(int choice) => Run(() => Sim.ResolveEvent(choice));

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
            return rec == null ? null : Loc.F("{0}, {1}. gün", GameData.AiType(rec.type).Name, rec.day);
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
