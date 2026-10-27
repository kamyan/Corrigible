using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Saha ekibi: üç uzman (Teknisyen, EMP uzmanı, Analist). Baskında alınan hasar yara olarak kalır; düşen uzman yatar ve
    /// yerine sığınaktaki yedek uzman (acemi) çıkar. Baskına çıkmayan vardiya uzmanlar dinlenir.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>Son sığınağın bölgesi: harekât buradan başlar.</summary>
        public const string HQ = "weu";
        public static readonly string[] TeamNames = { Loc.N("Alfa"), Loc.N("Bravo"), Loc.N("Charlie"), Loc.N("Delta") };
        /// <summary>Uzman rolleri (Baskin birim türleri), kadro ve giriş sırası.</summary>
        public static readonly string[] Roles = { "tek", "emp", "ana" };

        static TeamState NewTeam(int i)
        {
            var team = new TeamState { Id = TeamNames[i].ToLowerInvariant(), Name = Loc.T(TeamNames[i]), At = HQ };
            foreach (var role in Roles) team.Members.Add(new SpecialistState { Role = role });
            return team;
        }

        public TeamState Team(string id) => S.Teams.FirstOrDefault(t => t.Id == id);
        /// <summary>Ekip bu vardiya baskına çıkmadı (kadronun yeterli olup olmadığına bakmaz; bkz. <see cref="Squad"/>).</summary>
        public static bool TeamReady(TeamState t) => t.Busy <= 0;

        public static int MaxHp(string role) => Baskin.UnitTypes[role].Hp;

        /// <summary>
        /// Baskına çıkacak kadro, rol sırasıyla: ayaktaki uzman yarasıyla; yatan uzmanın yerine boştaki ilk yedek, rol canından
        /// eksik; yedek de yoksa rol boş kalır. Zırh plakaları baskın emrinde takılır.
        /// </summary>
        public List<SquadSlot> Squad(TeamState team)
        {
            var list = new List<SquadSlot>();
            if (team == null) return list;
            var used = new HashSet<int>();
            var reserves = Shelter?.Reserves ?? new List<SpecialistState>();
            foreach (var m in team.Members)
            {
                int max = MaxHp(m.Role);
                if (m.Down <= 0) { list.Add(new SquadSlot { Type = m.Role, Hp = Math.Max(1, max - m.Wounds) }); continue; }
                int k = -1;
                for (int i = 0; i < reserves.Count && k < 0; i++) if (reserves[i].Down <= 0 && !used.Contains(i)) k = i;
                if (k < 0) continue;
                used.Add(k);
                list.Add(new SquadSlot { Type = m.Role, Reserve = k, Hp = Math.Max(1, max - B.liberationRookieHpPenalty) });
            }
            return list;
        }

        /// <summary>Kadroda Teknisyen var mı (sabotaj ve ikmal onsuz yapılamaz).</summary>
        public bool SquadCanRaid(TeamState team) => Squad(team).Any(x => x.Type == "tek");

        void StepTeams()
        {
            var reserves = Shelter?.Reserves ?? new List<SpecialistState>();
            foreach (var t in S.Teams)
            {
                // Baskına çıkmayan vardiya uzmanlar dinlenir; yatan uzman bir vardiya daha iyileşir
                bool rested = t.Busy <= 0;
                foreach (var m in t.Members)
                {
                    if (m.Down > 0) m.Down--;
                    else if (rested) m.Wounds = Math.Max(0, m.Wounds - B.liberationRestHeal);
                }
                if (t.Busy > 0) { t.Busy--; if (t.Busy == 0) t.Task = null; }
            }
            foreach (var m in reserves) if (m.Down > 0) m.Down--;
        }

        /// <summary>Taktik haritanın sonucu: sektör geri alınır ya da ekip geri döner.</summary>
        public ActResult ResolveRaid(RaidResult result) => RunCommand(() => result == null ? ActResult.Fail(Loc.T("Baskın sonucu eksik.")) : ResolveLiberationRaidCore(result), "raidres", result?.Encode());
    }
}
