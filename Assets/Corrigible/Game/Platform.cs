using UnityEngine;

namespace Corrigible.Game
{
    /// <summary>Platforma özgü servisler (başarımlar). Steam sürümünde Corrigible.Steam assembly'si kendi uygulamasını kaydeder.</summary>
    public interface IPlatformServices
    {
        string Name { get; }
        void UnlockAchievement(string id);
    }

    public static class Platform
    {
        public static IPlatformServices Current { get; set; } = new LocalPlatform();
    }

    /// <summary>Steam yokken kullanılır: başarımları yalnızca günlüğe yazar.</summary>
    public sealed class LocalPlatform : IPlatformServices
    {
        public string Name => "Yerel";
        public void UnlockAchievement(string id)
        {
            if (!string.IsNullOrEmpty(id)) Debug.Log($"[Başarım] {id}");
        }
    }

    /// <summary>Steamworks'te tanımlanacak başarım kimlikleri.</summary>
    public static class Achievements
    {
        public const string Alignment = "ACH_ALIGNMENT";
        public const string Eradication = "ACH_ERADICATION";
        public const string DarkAge = "ACH_DARK_AGE";
        public const string SilentLoss = "ACH_SILENT_VICTORY";
        public const string OpenTakeover = "ACH_OPEN_TAKEOVER";
        public const string Dissolved = "ACH_AGENCY_DISSOLVED";
        public const string Veil = "ACH_TEAR_THE_VEIL";
        public const string Misread = "ACH_MISREAD";

        public static string ForEnding(string ending) => ending switch
        {
            "hizalama" => Alignment,
            "silme" => Eradication,
            "karanlik" => DarkAge,
            "karanlikIllusion" => DarkAge,
            "sessiz" => SilentLoss,
            "acik" => OpenTakeover,
            "dagilma" => Dissolved,
            _ => null,
        };
    }
}
