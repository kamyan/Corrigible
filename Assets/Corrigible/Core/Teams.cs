using System.Linq;

namespace Corrigible.Core
{
    /// <summary>Saha ekibi: sığınaktan baskına çıkar, yaralanırsa toparlanana kadar bekler.</summary>
    public sealed partial class Simulation
    {
        /// <summary>Son sığınağın bölgesi: harekât buradan başlar.</summary>
        public const string HQ = "weu";
        public static readonly string[] TeamNames = { Loc.N("Alfa"), Loc.N("Bravo"), Loc.N("Charlie"), Loc.N("Delta") };

        static TeamState NewTeam(int i) => new TeamState { Id = TeamNames[i].ToLowerInvariant(), Name = Loc.T(TeamNames[i]), At = HQ };

        public TeamState Team(string id) => S.Teams.FirstOrDefault(t => t.Id == id);
        public static bool TeamReady(TeamState t) => t.Busy <= 0 && t.Down <= 0;

        void StepTeams()
        {
            foreach (var t in S.Teams)
            {
                if (t.Down > 0) { t.Down--; if (t.Down == 0) t.DownWhy = null; }
                if (t.Busy > 0) { t.Busy--; if (t.Busy == 0) t.Task = null; }
            }
        }

        /// <summary>Taktik haritanın sonucu: sektör geri alınır ya da ekip geri döner.</summary>
        public ActResult ResolveRaid(RaidResult result) => RunCommand(() => result == null ? ActResult.Fail(Loc.T("Baskın sonucu eksik.")) : ResolveLiberationRaidCore(result), "raidres", result?.Encode());
    }
}
