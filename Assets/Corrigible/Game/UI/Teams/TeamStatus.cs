using Corrigible.Core;

namespace Corrigible.Game.UI
{
    public enum TeamMood { Ready, Busy, Down }

    /// <summary>Bir saha ekibinin oyuncuya gösterilen durumu (hazır, yolda, görevde, yaralı…).</summary>
    public static class TeamStatus
    {
        static string TaskName(string task) => task switch
        {
            Actions.Monitor => Loc.T("izleme ağı kuruyor"),
            Actions.Audit => Loc.T("denetimde"),
            "baskin" => Loc.T("baskında"),
            _ => Loc.T("görevde"),
        };

        public static (TeamMood mood, string text) Of(TeamState t)
        {
            if (t.Down > 0) return (TeamMood.Down, Loc.F("{0} · {1} g", t.DownWhy ?? Loc.T("iyileşiyor"), t.Down));
            if (t.Dest != null) return (TeamMood.Busy, Loc.F("→ {0} · {1} g", GameData.Region(t.Dest).Name, t.Eta));
            if (t.Busy > 0) return (TeamMood.Busy, Loc.F("{0} · {1} g", TaskName(t.Task), t.Busy));
            return (TeamMood.Ready, Loc.T("hazır"));
        }

        public static UnityEngine.Color Color(TeamMood m, UiTheme theme) => m == TeamMood.Down ? theme.dim : m == TeamMood.Busy ? theme.warn : theme.verify;
    }
}
