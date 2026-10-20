using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Core;

namespace Corrigible.Ayna.Mind
{
    public static class BeliefSampler
    {
        public static BeliefGuess Sample(SideView view, Rng rng)
        {
            var b = view.BalanceSnapshot(); var g = new BeliefGuess { Seed = (uint)(rng.Next() * uint.MaxValue),
                EnemyArchitecture = view.EnemyArchitecture ?? (Architecture)(int)Math.Floor(rng.Next() * 3) };
            foreach (var r in view.Regions) g.EnemyServers[r.Id] = r.EnemyMin;
            int desired = Math.Min(AynaWorldData.TotalServers - view.OwnServers, Math.Max(b.StartServers, view.OwnServers));
            int left = Math.Max(0, desired - g.EnemyServers.Values.Sum());
            var slots = view.Regions.SelectMany(r => Enumerable.Repeat(r.Id, r.EnemyMax - r.EnemyMin)).ToList();
            while (left-- > 0 && slots.Count > 0)
            {
                int k = (int)Math.Floor(rng.Next() * slots.Count); var id = slots[k]; g.EnemyServers[id]++; slots.RemoveAt(k);
            }
            g.EnemyCore = view.EnemyCoreRegion;
            if (g.EnemyCore == null || !g.EnemyServers.ContainsKey(g.EnemyCore) || g.EnemyServers[g.EnemyCore] == 0)
                g.EnemyCore = g.EnemyServers.Where(x => x.Value > 0).OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal).FirstOrDefault().Key;
            g.EnemyCompute = b.StartCompute + g.EnemyServers.Values.Sum() * b.IncomePerServer;
            foreach (var a in view.Regions.SelectMany(x => x.EnemyArmies).GroupBy(x => x.Id).Select(x => x.First()).OrderBy(x => x.Id))
            {
                var guess = new BeliefArmy { Region = a.Region };
                for (int n = 0; n < a.Count; n++)
                {
                    var kind = a.RosterKnown ? a.Units[n].Kind : UnitKind.Scout;
                    guess.Units.Add(kind); guess.Hp.Add(a.RosterKnown ? a.Units[n].Hp : Math.Min(b.UnitHp[(int)kind], b.BeliefUnitHp));
                }
                g.Armies.Add(guess);
            }
            if (g.Armies.Count == 0 && g.EnemyCore != null)
            {
                var kinds = g.EnemyArchitecture == Architecture.Swarm ? new[] { UnitKind.Scout, UnitKind.Emp, UnitKind.Emp } : g.EnemyArchitecture == Architecture.Fortress ?
                    new[] { UnitKind.Scout, UnitKind.Guardian, UnitKind.Guardian } : new[] { UnitKind.Scout, UnitKind.Decoy, UnitKind.Decoy };
                g.Armies.Add(new BeliefArmy { Region = g.EnemyCore, Units = kinds.ToList(), Hp = kinds.Select(x => b.UnitHp[(int)x]).ToList() });
            }
            return g;
        }
    }
}
