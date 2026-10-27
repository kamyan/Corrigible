using Corrigible.Core;
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
        public string Name => Loc.T("Yerel");
        public void UnlockAchievement(string id)
        {
            if (!string.IsNullOrEmpty(id)) Debug.Log($"[Başarım] {id}");
        }
    }

    /// <summary>Steamworks'te tanımlanacak başarım kimlikleri.</summary>
    public static class Achievements
    {
        public const string Eradication = "ACH_ERADICATION";

        public static string ForEnding(string ending) => ending == "silme" ? Eradication : null;
    }
}
