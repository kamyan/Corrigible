using System.Collections.Generic;
using System.IO;
using Corrigible.Core;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// İngilizce tablo: <c>strings.txt</c> + Aşama 6'nın tabloya geç giren metinleri (PLAN §4; aynı değerlerle). Tablodaki değer önce gelir.
    /// Rev3AdmissionSnapshotTests silinince (aşama 7) kendi dosyasına taşındı; eski tek adaylı kabulün metinleri sözlükten çıkarıldı.
    /// </summary>
    internal static class Rev3AdmissionEnglish
    {
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";

        internal static readonly Dictionary<string, string> Stage6 = new Dictionary<string, string>
        {
            { "KABUL TEZGÂHI", "ADMISSION DESK" }, { "KABUL ·\nTAM ERİŞİM", "ADMIT ·\nFULL ACCESS" },
            { "CAN {0}/{1} · HAR {2} · GÜÇ {3}", "HP {0}/{1} · MOVE {2} · POWER {3}" }, { "İZLER", "TRACES" },
            { "KARAR V.{0:000} · {1}", "DECISION S.{0:000} · {1}" }, { "KISITLI · {0} V", "LIMIT · {0} S" },
        };

        /// <summary>İngilizceye geçer: gerçek tablo, eksikse Aşama 6 sözlüğü.</summary>
        internal static void Use()
        {
            var en = new Dictionary<string, string>();
            if (File.Exists(TablePath))
            {
                var table = Loc.ParseTable(File.ReadAllText(TablePath));
                if (table.TryGetValue(Loc.English, out var t)) foreach (var kv in t) en[kv.Key] = kv.Value;
            }
            foreach (var kv in Stage6) if (!en.ContainsKey(kv.Key) || string.IsNullOrEmpty(en[kv.Key])) en[kv.Key] = kv.Value;
            Loc.SetLanguage(Loc.English, en);
        }
    }
}
