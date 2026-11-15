using System.Collections.Generic;
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

    /// <summary>
    /// Steamworks'te tanımlanacak başarım kimlikleri. <see cref="Eradication"/> yayında tanımlı; KOS aşama 6'nın yenileri
    /// (<see cref="EradicationClean"/>, <see cref="Withdrawal"/>, <see cref="Evacuation"/>, <see cref="ShelterFallen"/>,
    /// <see cref="ShelterAbandoned"/>) yalnız kod eşlemesidir: Steamworks'te tanımlanana dek <c>SetAchievement</c> false döner ve
    /// sessizce düşer (Docs/KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md §11 soru 7).
    /// </summary>
    public static class Achievements
    {
        public const string Eradication = "ACH_ERADICATION";
        /// <summary>Silme + temiz ton.</summary>
        public const string EradicationClean = "ACH_ERADICATION_CLEAN";
        /// <summary>Son şalter + düzenli çekilme (yenilgi değil).</summary>
        public const string Withdrawal = "ACH_WITHDRAWAL";
        /// <summary>Son şalter + tahliye.</summary>
        public const string Evacuation = "ACH_EVACUATION";
        /// <summary>Sığınak düştü (dusus).</summary>
        public const string ShelterFallen = "ACH_SHELTER_FALLEN";
        /// <summary>Sığınak boşaldı (terk).</summary>
        public const string ShelterAbandoned = "ACH_SHELTER_ABANDONED";

        static readonly string[] None = new string[0];

        /// <summary>
        /// Sonun açtığı başarımlar. <paramref name="switchKind"/> ("cekilme" | "tahliye") ve <paramref name="tone"/> ("temiz" …) son
        /// raporundan (<c>EndReport</c>); eski oda yolunda null (yalnız kimliğe göre). topluluk, dagilma, vuruldu, sizma: boş.
        /// </summary>
        public static IReadOnlyList<string> ForEnding(string ending, string switchKind = null, string tone = null)
        {
            switch (ending)
            {
                case "silme": return tone == "temiz" ? new[] { Eradication, EradicationClean } : new[] { Eradication };
                case "salter": return switchKind == "tahliye" ? new[] { Evacuation } : new[] { Withdrawal };
                case "dusus": return new[] { ShelterFallen };
                case "terk": return new[] { ShelterAbandoned };
                default: return None;
            }
        }
    }
}
