using System;
using System.IO;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Game
{
    /// <summary>
    /// Kayıt dosyası (<c>persistentDataPath/liberation_savegame.json</c>): yazma, okuma, silme. <see cref="GameController"/>
    /// ve menü sahnesi (vardiya kartının özeti) aynı dosyayı buradan okur. Bu sürümün biçiminde olmayan ya da hiç oynanmamış
    /// kayıt yok sayılır.
    /// </summary>
    public static class SaveStore
    {
        /// <summary>Editör doğrulaması ayrı bir klasör kullanır; oyuncunun gerçek kaydı testte değiştirilmez.</summary>
        public static string StorageDirectory
        {
            get
            {
#if UNITY_EDITOR
                string test = UnityEditor.SessionState.GetString("Corrigible.TestStorage", "");
                if (!string.IsNullOrEmpty(test)) return test;
#endif
                return Application.persistentDataPath;
            }
        }
        public static string SavePath => Path.Combine(StorageDirectory, "liberation_savegame.json");

        public static void Write(GameRecord record)
        {
            try
            {
                Directory.CreateDirectory(StorageDirectory);
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    GameRecord previous = null;
                    // Bozuk JSON yeni oyunun kaydını engellemez; özgün dosya yine de arşivlenir.
                    try { previous = JsonUtility.FromJson<GameRecord>(json); }
                    catch (ArgumentException) { }
                    if (previous == null || previous.version <= 0)
                        ArchivePrevious(json, ".unreadable.backup");
                    else if (previous.version != record.version)
                        ArchivePrevious(json, ".v" + previous.version + ".backup");
                }
                string pending = SavePath + ".writing";
                File.WriteAllText(pending, JsonUtility.ToJson(record));
                if (File.Exists(SavePath)) File.Replace(pending, SavePath, null);
                else File.Move(pending, SavePath);
            }
            catch (Exception e) { Debug.LogWarning($"Oyun kaydedilemedi: {e.Message}"); }
        }

        static void ArchivePrevious(string json, string suffix)
        {
            string archive = SavePath + suffix;
            int number = 1;
            // Önceki bir arşivin üstüne yazılmaz; farklı eski kayıtlar ayrı korunur.
            while (File.Exists(archive))
            {
                if (File.ReadAllText(archive) == json) return;
                archive = SavePath + suffix + "." + number++;
            }
            File.Copy(SavePath, archive);
        }

        /// <summary>Kayıttaki oyun (geri yüklemeden), yoksa ya da okunamıyorsa null.</summary>
        public static GameRecord Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var rec = JsonUtility.FromJson<GameRecord>(File.ReadAllText(SavePath));
                // Eski biçimdeki (kriz oyunu dönemi) kayıtlar yeniden oynatılamaz
                if (rec == null || rec.balance == null || rec.commands == null || rec.version != GameRecord.CurrentVersion) return null;
                // JsonUtility null listeyi boş listeye dönüştürebilir. Kartlı oyunda devirsiz geçmiş gün geçerli değildir.
                if (rec.balance.planningBoardEnabled && rec.day > 0 && rec.commands.Count == 0) return null;
                return rec.day > 0 || rec.commands.Count > 0 ? rec : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Kayıt okunamadı: {e.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); }
            catch (Exception e) { Debug.LogWarning($"Kayıt silinemedi: {e.Message}"); }
        }
    }
}
