using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Ayarlar kartının oyun tarafı (F8.3): ses kanalları ve sessiz mod <see cref="AudioPrefs"/>'te (eski SettingsModal'la
    /// aynı tercih anahtarları), dil <see cref="DeskLoc"/> ile sahneyi yeniden yüklemeden. Kart açıkken zaman durur.
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
        public void SetLanguage(DeskLang lang) => ApplyLanguage(lang, source != null ? source.Game : null);

        public void SetOpen(bool open)
        {
            if (source == null) return;
            source.State.SettingsOpen = open;
            source.MarkDirty();
        }

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
            if (game?.Sim != null && game.Sim.S.Day == 0 && game.Sim.Record != null && game.Sim.Record.commands.Count == 0)
                game.NewGame(game.TypeId);
        }
    }
}
