using Corrigible.Core;

namespace Corrigible.Desk
{
    /// <summary>Bölge kartlarının sunum kimlikleri ve paket dışındaki kısa kaynak metinleri.</summary>
    public static class SadeCardText
    {
        public static string Command(string id) => id switch
        {
            "monitorNet" => "izleme", "audit" => "denetim", "quarantine" => "karantina", "killSwitch" => "kilit", _ => id,
        };

        public static string ActionId(string command) => command switch
        {
            "izleme" => "monitorNet", "denetim" => "audit", "karantina" => "quarantine", "kilit" => "killSwitch", _ => command,
        };

        public static string Icon(string id) => id switch
        {
            "monitorNet" => "antenna", "audit" => "search", "quarantine" => "quar", "killSwitch" => "kill", _ => id,
        };

        public static string Literal(string source) => Loc.T(source switch
        {
            "Telekom raporu" => Loc.N("Telekom raporu"), "Ekip B · saha ölçümü" => Loc.N("Ekip B · saha ölçümü"),
            "PANORAMA ajansı" => Loc.N("PANORAMA ajansı"), "haber" => Loc.N("haber"), "Seul" => Loc.N("Seul"),
            "1,64 mlr" => Loc.N("1,64 mlr"), _ => source,
        });

        public static string Cost(string source) => source switch
        {
            "8 fon · 5 gün" => Loc.F("{0} fon · {1} gün", 8, 5),
            "4 fon · 2 SG · 2 gün" => Loc.F("{0} fon · {1} SG · {2} gün", 4, 2, 2),
            "15 SG · güven −3" => Loc.F("{0} SG · güven −{1}", 15, 3),
            "25 SG · ekonomi −40" => Loc.F("{0} SG · ekonomi −{1}", 25, 40),
            _ => Literal(source),
        };

        public static string TeamName(string id) => Loc.F("Ekip {0}", id);
        public static string TeamSub(string city, string status) => Loc.F("{0} · {1}", Literal(city), status);
    }
}
