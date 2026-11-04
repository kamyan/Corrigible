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
        public static string SavePath => Path.Combine(Application.persistentDataPath, "liberation_savegame.json");

        public static void Write(GameRecord record)
        {
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(record)); }
            catch (Exception e) { Debug.LogWarning($"Oyun kaydedilemedi: {e.Message}"); }
        }

        /// <summary>Kayıttaki oyun (geri yüklemeden), yoksa ya da okunamıyorsa null.</summary>
        public static GameRecord Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var rec = JsonUtility.FromJson<GameRecord>(File.ReadAllText(SavePath));
                // Eski biçimdeki (kriz oyunu dönemi) kayıtlar yeniden oynatılamaz
                return rec != null && rec.version == GameRecord.CurrentVersion && (rec.day > 0 || rec.commands.Count > 0) ? rec : null;
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
