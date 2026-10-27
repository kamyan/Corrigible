using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Son Şalter simülasyon çekirdeği: YZ işgali altındaki dünyayı son sığınaktan sektör sektör geri alma harekâtı.
    /// UnityEngine'e bağımlı değildir; aynı tohum ve aynı komutlar her zaman aynı sonucu verir (kayıt bunun üstüne kurulu).
    /// Zaman yalnız oyuncunun vardiyayı bitirmesiyle (<see cref="Step"/>) ilerler; durumu yalnız komutlar değiştirir.
    /// </summary>
    public sealed partial class Simulation
    {
        public readonly WorldState S;
        public readonly Balance B;

        Simulation(WorldState s, Balance b) { S = s; B = b; }

        /// <summary>Kayıt: başlangıç koşulları + başarılı komutlar. Kaydetme/devam ettirme bunun üzerinden yapılır.</summary>
        public GameRecord Record;

        ActResult Rec(ActResult r, string kind, string a = null, string b = null)
        {
            if (r.Ok && Record != null) Record.commands.Add(new SavedCommand { day = S.Day, kind = kind, a = a, b = b });
            return r;
        }

        // ---------- Kurulum ----------
        public static Simulation NewGame(uint? seed = null, Balance balance = null)
        {
            var b = balance ?? new Balance();
            uint sd = seed ?? (uint)new Random().Next(0, int.MaxValue);
            var s = new WorldState { Seed = sd, Rng = new Rng(sd), Fon = b.startFon };
            var sim = new Simulation(s, b);
            sim.SetupLiberation();
            sim.Record = new GameRecord { seed = sd, balance = b.Clone() };
            s.Events.Clear();
            return sim;
        }

        static RegionDef Def(string id) => GameData.Region(id);

        /// <summary>İşgal oranı (%): geri alınmamış sektörlerin payı.</summary>
        public double Occupation
        {
            get
            {
                int total = 0, open = 0;
                foreach (var r in S.Liberation.Regions) { total += r.Sectors.Count; open += r.Sectors.Count - r.CapturedCount; }
                return total == 0 ? 0 : 100.0 * open / total;
            }
        }

        void Moment(string kind, string text) => S.Moments.Add(new Moment { Day = S.Day, Kind = kind, Text = text });

        void End(string key)
        {
            if (S.Ending != null) return;
            S.Ending = key;
            S.Events.Add(new SimEvent { Type = SimEventType.End, Key = key });
        }

        // ---------- Bir vardiya ----------
        /// <summary>Vardiyayı bitirir: gelir, ekiplerin toparlanması, sığınağın üretimi ve tüketimi.</summary>
        public void Step()
        {
            if (S.Ending != null || S.Blocked) return;
            S.Day++;
            S.LastIncome = IncomeBreakdown;
            S.LastIncomeDay = S.Day;
            S.Fon += S.LastIncome.Total;
            AddTransaction("income", null, S.LastIncome.Total);
            StepTeams();
            ShelterLogic.Advance(S, B);
            S.Hist.Add(new HistPoint { Day = S.Day, Real = Js.Round(Occupation * 10) / 10 });
        }

        public List<SimEvent> DrainEvents()
        {
            var e = new List<SimEvent>(S.Events);
            S.Events.Clear();
            return e;
        }
    }
}
