using Corrigible.Core;

namespace Corrigible.Game.UI
{
    public enum TeamMood { Ready, Busy, Down }

    /// <summary>Bir saha ekibinin oyuncuya gösterilen durumu (hazır, yolda, görevde, yaralı…).</summary>
    public static class TeamStatus
    {
        static string TaskName(string task) => task switch
        {
            Actions.Monitor => "izleme ağı kuruyor",
            Actions.Audit => "denetimde",
            "baskin" => "baskında",
            _ => "görevde",
        };

        public static (TeamMood mood, string text) Of(TeamState t)
        {
            if (t.Down > 0) return (TeamMood.Down, $"{t.DownWhy ?? "iyileşiyor"} · {t.Down} g");
            if (t.Dest != null) return (TeamMood.Busy, $"→ {GameData.Region(t.Dest).Name} · {t.Eta} g");
            if (t.Busy > 0) return (TeamMood.Busy, $"{TaskName(t.Task)} · {t.Busy} g");
            return (TeamMood.Ready, "hazır");
        }

        public static UnityEngine.Color Color(TeamMood m, UiTheme theme) => m == TeamMood.Down ? theme.dim : m == TeamMood.Busy ? theme.warn : theme.verify;
    }
}
