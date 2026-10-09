using System;
using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Game
{
    /// <summary>
    /// TickClock + komut kapısı: gerçek zamanı oyun gününe çevirir, simülasyonu ilerletir ve olayları sunuma iletir.
    /// Sunum katmanı simülasyonu yalnızca buradaki metotlarla değiştirir.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        const string TypePrefKey = "corrigible.aiType";

        [SerializeField] BalanceConfig balanceConfig;
        [Tooltip("1×, 2× ve 4× hızlarda bir oyun gününün süresi (saniye).")]
        [SerializeField] float[] secondsPerDay = { 0.9f, 0.45f, 0.22f };

        public Simulation Sim { get; private set; }
        public PlayerProfile Profile { get; private set; }
        public string TypeId { get; private set; } = "manipulator";
        public int Speed { get; private set; }

        /// <summary>Bir pencere açıkken zamanı durdurmak için sunum katmanı ayarlar.</summary>
        public bool Hold { get; set; }

        public event Action<List<SimEvent>> EventsRaised;
        public event Action Changed;
        public event Action NewGameStarted;

        float acc;

        void Awake()
        {
            Profile = ProfileStore.Load();
            var saved = PlayerPrefs.GetString(TypePrefKey, TypeId);
            TypeId = GameData.HasType(saved) ? saved : TypeId;
            NewGame(TypeId);
        }

        public void NewGame(string typeId)
        {
            TypeId = GameData.HasType(typeId) ? typeId : "manipulator";
            PlayerPrefs.SetString(TypePrefKey, TypeId);
            var balance = balanceConfig != null ? balanceConfig.CreateRuntimeCopy() : new Balance();
            Sim = Simulation.NewGame(Profile, TypeId, null, balance);
            Speed = 0;
            acc = 0;
            NewGameStarted?.Invoke();
            Changed?.Invoke();
        }

        public void SetSpeed(int speed)
        {
            Speed = Mathf.Clamp(speed, 0, 4);
            Changed?.Invoke();
        }

        void Update()
        {
            if (Sim == null || Speed == 0 || Hold) return;
            var s = Sim.S;
            if (s.Ending != null || s.PendingGift != null) return;
            float spd = secondsPerDay[Speed >= 4 ? 2 : Speed - 1];
            acc += Time.unscaledDeltaTime;
            int n = 0;
            while (acc >= spd && n < 3)
            {
                acc -= spd; n++;
                Sim.Step();
                Flush();
                if (s.PendingGift != null || s.Ending != null || Hold) { acc = 0; break; }
            }
            if (n > 0) Changed?.Invoke();
        }

        /// <summary>Simülasyonun biriktirdiği olayları sunuma iletir; oyun bittiyse profili günceller.</summary>
        public void Flush()
        {
            var events = Sim.DrainEvents();
            if (events.Count == 0) return;
            foreach (var e in events)
            {
                if (e.Type != SimEventType.End) continue;
                Profile = Profiling.UpdateProfile(Profile, Sim.S);
                ProfileStore.Save(Profile);
            }
            EventsRaised?.Invoke(events);
        }

        // ---------- Komutlar ----------
        public ActResult Act(string action, string regionId) => Run(() => Sim.Act(action, regionId));
        public ActResult StartResearch(string id) => Run(() => Sim.StartResearch(id));
        public ActResult ResolveGift(string choice) => Run(() => Sim.ResolveGift(choice));
        public ActResult Verify(string headlineId) => Run(() => Sim.Verify(headlineId));
        public ActResult GlobalKillSwitch() => Run(() => Sim.GlobalKillSwitch());

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
            Flush();
            Changed?.Invoke();
            return r;
        }
    }
}
