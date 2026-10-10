using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Game
{
    /// <summary>
    /// Dil seçimi ve çeviri tablosunun yüklenmesi. Tablo: <c>Resources/Localization/strings.txt</c> (biçimi
    /// <see cref="Loc.ParseTable"/>'da). Dil sahne yüklenmeden önce ayarlanır; değişince sahne yeniden yüklenir, böylece
    /// her metin (sahnedeki sabitler, simülasyonun ürettikleri) baştan yeni dilde kurulur.
    /// </summary>
    public static class Localization
    {
        public const string TablePath = "Localization/strings";
        const string PrefKey = "corrigible.language";

        static Dictionary<string, Dictionary<string, string>> tables;
        static readonly HashSet<string> warned = new HashSet<string>();

        public static string Current => Loc.Lang;

        /// <summary>Dilin kendi dilindeki adı (dil düğmesinde).</summary>
        public static string DisplayName(string lang) => lang == Loc.English ? "English" : "Türkçe";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Loc.Missing -= OnMissing;
            Loc.Missing += OnMissing;
            Apply(PlayerPrefs.GetString(PrefKey, Application.systemLanguage == SystemLanguage.Turkish ? Loc.Turkish : Loc.English));
        }

        static void Apply(string lang)
        {
            if (tables == null)
            {
                var asset = Resources.Load<TextAsset>(TablePath);
                tables = asset != null ? Loc.ParseTable(asset.text) : new Dictionary<string, Dictionary<string, string>>();
            }
            tables.TryGetValue(lang, out var table);
            Loc.SetLanguage(lang, table);
            warned.Clear();
        }

        /// <summary>Dili değiştirir, kaydeder ve sahneyi yeniden yükler.</summary>
        public static void SetLanguage(string lang)
        {
            if (lang == Loc.Lang) return;
            PlayerPrefs.SetString(PrefKey, lang);
            PlayerPrefs.Save();
            Apply(lang);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public static void Next()
        {
            int i = System.Array.IndexOf(Loc.Languages, Loc.Lang);
            SetLanguage(Loc.Languages[(i + 1) % Loc.Languages.Length]);
        }

        static void OnMissing(string source)
        {
            if (Debug.isDebugBuild && warned.Add(source))
                Debug.LogWarning($"[Lokalizasyon] '{Loc.Lang}' çevirisi yok: {source}");
        }
    }
}
