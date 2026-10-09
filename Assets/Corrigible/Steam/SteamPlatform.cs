using Steamworks;
using UnityEngine;

namespace Corrigible.Steam
{
    /// <summary>
    /// Steam sürümü: başarımları Steamworks'e yazar. Bu assembly yalnızca Steamworks.NET paketi kuruluyken derlenir
    /// (STEAMWORKS_NET). Oyuncu profili ve kayıt dosyası persistentDataPath altındadır; Steam Cloud için bu klasör
    /// Steamworks'te Auto-Cloud'a eklenir, kod gerekmez.
    /// </summary>
    public sealed class SteamPlatform : Game.IPlatformServices
    {
        public string Name => "Steam";

        public void UnlockAchievement(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (SteamUserStats.SetAchievement(id)) SteamUserStats.StoreStats();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (!SteamAPI.Init())
            {
                Debug.LogWarning("Steam başlatılamadı; başarımlar yerel olarak günlüğe yazılacak.");
                return;
            }
            Game.Platform.Current = new SteamPlatform();
            var go = new GameObject("SteamCallbacks") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(go);
            go.AddComponent<SteamCallbacks>();
        }

        sealed class SteamCallbacks : MonoBehaviour
        {
            void Update() => SteamAPI.RunCallbacks();
            void OnApplicationQuit() => SteamAPI.Shutdown();
        }
    }
}
