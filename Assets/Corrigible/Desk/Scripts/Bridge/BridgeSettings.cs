using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Ayarlar dolabının oyun tarafı (<see cref="SettingsCabinet"/>): ses kanalları ve sessiz mod <see cref="AudioPrefs"/>'te,
    /// dil <see cref="DeskLoc"/> ile sahneyi yeniden yüklemeden. Menü sahnesinde de aynı bileşen çalışır (veri kaynağı yok).
    /// </summary>
    public sealed class BridgeSettings : MonoBehaviour, IDeskSettings
    {
        const string LanguageKey = "corrigible.language";

        [SerializeField] SimDeskDataSource source;

        // Kart sırası: ana ses, müzik, ortam, efektler
        static readonly AudioPrefs.Channel[] Channels = { AudioPrefs.Channel.Master, AudioPrefs.Channel.Music, AudioPrefs.Channel.Ambience, AudioPrefs.Channel.Effects };
        float lastPreview;

        public float Get(int channel) => AudioPrefs.Get(Channels[Mathf.Clamp(channel, 0, 3)]);
        public void Set(int channel, float value) => AudioPrefs.Set(Channels[Mathf.Clamp(channel, 0, 3)], value);

        public bool Muted { get => AudioPrefs.Muted; set => AudioPrefs.Muted = value; }

        public DeskLang Language => DeskLoc.Instance.Lang;

        /// <summary>
        /// Dolap açıkken oyun yeniden kurulmaz (açılış dizisi dolabın önünü karartırdı); dolap kapanınca
        /// <see cref="RebuildIfPending"/> kurar.
        /// </summary>
        public void SetLanguage(DeskLang lang)
        {
            var game = source != null ? source.Game : null;
            bool open = SettingsCabinet.Instance != null && SettingsCabinet.Instance.IsOpen;
            if (open && lang != DeskLoc.Instance.Lang && Fresh(game)) rebuildPending = true;
            ApplyLanguage(lang, open ? null : game);
        }

        bool rebuildPending;

        /// <summary>Dolapta dil değiştiyse ve oyun hâlâ oynanmamışsa yeni dilde yeniden kurar.</summary>
        public void RebuildIfPending()
        {
            if (!rebuildPending) return;
            rebuildPending = false;
            var game = source != null ? source.Game : null;
            if (Fresh(game)) game.NewGame();
        }

        static bool Fresh(GameController game) => game?.Sim != null && game.Sim.S.Day == 0 && game.Sim.Record != null && game.Sim.Record.commands.Count == 0;

        /// <summary>Kaydırırken nasıl duyulduğunu dinlet (müzik hariç; sık tetiklenmesin).</summary>
        public void Preview(int channel)
        {
            if (channel == 1 || Time.unscaledTime - lastPreview < 0.12f) return;
            lastPreview = Time.unscaledTime;
            Sfx.Play(channel == 2 ? Cue.TeamRadio : Cue.Click);
        }

        public void Save() => AudioPrefs.Save();

        /// <summary>
        /// Dili değiştirir ve tercihe yazar (oyunun Localization'ı açılışta aynı anahtardan okur). Giriş dosyası açıksa (gün 0)
        /// oyun yeniden kurulur ki YZ'nin karşı satırları ve ekip adları da yeni dilde üretilsin; oyun sürerken üretilmiş
        /// haberler üretildikleri dilde kalır.
        /// </summary>
        public static void ApplyLanguage(DeskLang lang, GameController game)
        {
            if (lang == DeskLoc.Instance.Lang) return;
            DeskLoc.Instance.SetLanguage(lang);
            PlayerPrefs.SetString(LanguageKey, lang == DeskLang.En ? Loc.English : Loc.Turkish);
            PlayerPrefs.Save();
            if (Fresh(game)) game.NewGame();
        }
    }
}
