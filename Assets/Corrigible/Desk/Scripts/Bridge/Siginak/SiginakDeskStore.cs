using System;
using System.Collections.Generic;
using System.IO;
using Corrigible.Core;
using Corrigible.Game;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kayda girmeyen sunum durumu için yan dosya (IMPLEMENTATION_MAP › Soru 4/b): <c>liberation_desk.json</c>, kaydın
    /// yanında. Kayıt biçimi değişmez; dosya kayıttaki tohumla eşleşmezse yok sayılır. Teleks satırları ve
    /// EVRAK tepsisindeki son 5 vardiya raporunun girdisi (metin değil: dil değişince yeniden kurulur).
    /// Revizyon 3 · R19 (Karar D7-A): kartlı oyunda EVRAK'a dosyalanan son 5 devir fişinin verisi (<see cref="HandoverSlipFacts"/>,
    /// dilden bağımsız). Bu alanı taşımayan eski dosya boş arşivle açılır. KOS aşama 7: eski kabul istasyonunun görülen durumu
    /// (<c>admissionSeen</c>) söküldü; o alanı taşıyan eski dosya sorunsuz okunur (JsonUtility bilinmeyen alanı yok sayar).
    /// </summary>
    public static class SiginakDeskStore
    {
        [Serializable]
        sealed class Notes
        {
            public uint seed;
            public List<TelexLine> telex = new List<TelexLine>();
            public List<ShiftReportInput> reports = new List<ShiftReportInput>();
            /// <summary>Kamera pozu (KAYITTAN DEVAM bu poza döner).</summary>
            public string pose;
            /// <summary>Dosyalanan devir fişleri, eskiden yeniye (en çok <see cref="SlipKeep"/>).</summary>
            public List<HandoverSlipFacts> slips = new List<HandoverSlipFacts>();
        }

        public const string FileName = "liberation_desk.json";
        /// <summary>EVRAK arşivinde tutulan devir fişi sayısı (tepsideki rapor sayısıyla aynı kural: son 5).</summary>
        public const int SlipKeep = 5;
        static string FilePath => Path.Combine(SaveStore.StorageDirectory, FileName);

        public static void Save(GameRecord record, IReadOnlyList<TelexLine> telex, IReadOnlyList<ShiftReportInput> reports, string pose = null,
            IReadOnlyList<HandoverSlipFacts> slips = null)
        {
            if (record == null) return;
            var notes = new Notes { seed = record.seed, pose = pose };
            if (telex != null) notes.telex.AddRange(telex);
            if (reports != null) notes.reports.AddRange(reports);
            if (slips != null)
            {
                // Boş girdiler fiş sayılmaz: önce atılır, sonra son SlipKeep fiş kalır
                foreach (var f in slips) if (f != null) notes.slips.Add(f);
                if (notes.slips.Count > SlipKeep) notes.slips.RemoveRange(0, notes.slips.Count - SlipKeep);
            }
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(notes)); }
            catch (Exception e) { Debug.LogWarning("Sığınak notları yazılamadı: " + e.Message); }
        }

        /// <summary>Kayda ait teleks satırları; dosya yoksa ya da başka bir oyuna aitse null.</summary>
        public static List<TelexLine> LoadTelex(GameRecord record) => Load(record)?.telex;

        /// <summary>Kayıttaki kamera pozu; yoksa null.</summary>
        public static string LoadPose(GameRecord record) => Load(record)?.pose;

        /// <summary>Kayda ait vardiya raporları (eskiden yeniye); yoksa null.</summary>
        public static List<ShiftReportInput> LoadReports(GameRecord record) => Load(record)?.reports;

        /// <summary>
        /// Kayda ait devir fişi arşivi (eskiden yeniye, en çok <see cref="SlipKeep"/>); dosya yoksa ya da başka bir oyuna aitse null,
        /// arşivsiz eski dosyada boş liste.
        /// </summary>
        public static List<HandoverSlipFacts> LoadSlips(GameRecord record)
        {
            var notes = Load(record);
            if (notes == null) return null;
            var slips = notes.slips ?? new List<HandoverSlipFacts>();
            slips.RemoveAll(f => f == null);
            if (slips.Count > SlipKeep) slips.RemoveRange(0, slips.Count - SlipKeep);
            return slips;
        }

        static Notes Load(GameRecord record)
        {
            if (record == null) return null;
            try
            {
                if (!File.Exists(FilePath)) return null;
                var notes = JsonUtility.FromJson<Notes>(File.ReadAllText(FilePath));
                return notes != null && notes.seed == record.seed ? notes : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Sığınak notları okunamadı: " + e.Message);
                return null;
            }
        }
    }
}
