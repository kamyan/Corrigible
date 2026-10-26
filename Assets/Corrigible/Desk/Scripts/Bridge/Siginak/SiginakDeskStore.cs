using System;
using System.Collections.Generic;
using System.IO;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kayda girmeyen sunum durumu için yan dosya (IMPLEMENTATION_MAP › Soru 4/b): <c>liberation_desk.json</c>, kaydın
    /// yanında. Kayıt biçimi değişmez; dosya kayıttaki tohum ve rakiple eşleşmezse yok sayılır. Teleks satırları ve
    /// EVRAK tepsisindeki son 5 vardiya raporunun girdisi (metin değil: dil değişince yeniden kurulur).
    /// </summary>
    public static class SiginakDeskStore
    {
        [Serializable]
        sealed class Notes
        {
            public uint seed;
            public string type;
            public List<TelexLine> telex = new List<TelexLine>();
            public List<ShiftReportInput> reports = new List<ShiftReportInput>();
            /// <summary>Kamera pozu (KAYITTAN DEVAM bu poza döner).</summary>
            public string pose;
        }

        public const string FileName = "liberation_desk.json";
        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Save(GameRecord record, IReadOnlyList<TelexLine> telex, IReadOnlyList<ShiftReportInput> reports, string pose = null)
        {
            if (record == null) return;
            var notes = new Notes { seed = record.seed, type = record.type, pose = pose };
            if (telex != null) notes.telex.AddRange(telex);
            if (reports != null) notes.reports.AddRange(reports);
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(notes)); }
            catch (Exception e) { Debug.LogWarning("Sığınak notları yazılamadı: " + e.Message); }
        }

        /// <summary>Kayda ait teleks satırları; dosya yoksa ya da başka bir oyuna aitse null.</summary>
        public static List<TelexLine> LoadTelex(GameRecord record) => Load(record)?.telex;

        /// <summary>Kayıttaki kamera pozu; yoksa null.</summary>
        public static string LoadPose(GameRecord record) => Load(record)?.pose;

        /// <summary>Kayda ait vardiya raporları (eskiden yeniye); yoksa null.</summary>
        public static List<ShiftReportInput> LoadReports(GameRecord record) => Load(record)?.reports;

        static Notes Load(GameRecord record)
        {
            if (record == null) return null;
            try
            {
                if (!File.Exists(FilePath)) return null;
                var notes = JsonUtility.FromJson<Notes>(File.ReadAllText(FilePath));
                return notes != null && notes.seed == record.seed && notes.type == record.type ? notes : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Sığınak notları okunamadı: " + e.Message);
                return null;
            }
        }
    }
}
