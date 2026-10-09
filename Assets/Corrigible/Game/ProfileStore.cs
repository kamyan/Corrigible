using System;
using System.IO;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Game
{
    /// <summary>Seni Tanıyan Rakip profilini persistentDataPath altında JSON olarak saklar. Steam sürümünde bu dosya Steam Cloud'a eklenir.</summary>
    public static class ProfileStore
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "player_profile.json");

        public static PlayerProfile Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var p = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(FilePath));
                if (p != null && string.IsNullOrEmpty(p.lastBranch)) p.lastBranch = null;
                return p;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Oyuncu profili okunamadı, yeni profil kullanılacak: {e.Message}");
                return null;
            }
        }

        public static void Save(PlayerProfile p)
        {
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(p, true)); }
            catch (Exception e) { Debug.LogWarning($"Oyuncu profili kaydedilemedi: {e.Message}"); }
        }

        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch (Exception e) { Debug.LogWarning($"Oyuncu profili silinemedi: {e.Message}"); }
        }
    }
}
