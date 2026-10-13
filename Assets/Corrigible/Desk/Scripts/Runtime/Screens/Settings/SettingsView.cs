using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Ayarlar (F8.3, taslak <c>drafts/ayarlar</c>): dizin kartı penceresi. Dört ses kanalı (kaydırıcı + yüzde), ses açık kutusu,
    /// dil seçimi, kapat. Değerler <see cref="IDeskSettings"/>'ten okunur ve ona yazılır; açıkken oyun zamanı durur.
    /// Esc ya da kapat düğmesi kapatır. Modal canvas'ında en üstte (giriş dosyasının da üstünde) açılır.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour
    {
        [Serializable]
        public struct ChannelRow
        {
            public TMP_Text label;
            public FaderView fader;
            public TMP_Text value;
        }

        [SerializeField] GameObject root;
        [SerializeField] ChannelRow[] channels = Array.Empty<ChannelRow>();
        [Tooltip("Ses açık: kural satırı (X kutusu, kalın etiket, açıklama).")]
        [SerializeField] RuleRowView mute;
        [SerializeField] RadioCheckView[] languages = Array.Empty<RadioCheckView>();
        [SerializeField] DeskButton close, closeX;
        [Tooltip("Bölüm başlıkları ve çizgileri (SES, DİL): çizgi çevrilen başlığın sonundan başlar.")]
        [SerializeField] TMP_Text[] sectionCaps = Array.Empty<TMP_Text>();
        [SerializeField] RectTransform[] sectionRules = Array.Empty<RectTransform>();
        [SerializeField] float sectionWidth = 474;
        [Tooltip("Metne göre dizilen satırlar (başlık, dil, alt şerit): açılınca yerleşir.")]
        [SerializeField] DeskRow[] rows = Array.Empty<DeskRow>();

        static readonly string MuteLabel = Corrigible.Core.Loc.N("Ses açık"), MuteHint = Corrigible.Core.Loc.N("Kapatınca bütün kanallar susar; ayarlar korunur.");
        readonly RuleVM muteVm = new RuleVM();

        IDeskSettings settings;
        public bool IsOpen => root != null && root.activeSelf;
        public static SettingsView Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < channels.Length; i++)
            {
                int k = i;
                channels[i].fader.Changed += v => { settings?.Set(k, v); DeskUi.SetText(channels[k].value, DeskFormat.Percent(v * 100)); settings?.Preview(k); };
                channels[i].fader.Released += () => settings?.Save();
            }
            if (mute != null) mute.Clicked += _ => { if (settings == null) return; settings.Muted = !settings.Muted; Refresh(); };
            DeskLoc.Instance.Changed += OnLanguage;
            for (int i = 0; i < languages.Length; i++)
            {
                var lang = i == 0 ? DeskLang.Tr : DeskLang.En;
                languages[i].Clicked += _ => { settings?.SetLanguage(lang); Refresh(); };
            }
            if (close != null) close.onClick.AddListener(Close);
            if (closeX != null) closeX.onClick.AddListener(Close);
            if (root != null) root.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; DeskLoc.Instance.Changed -= OnLanguage; }

        void OnLanguage() { if (IsOpen) Refresh(); }

        public void Init(IDeskSettings s) => settings = s;

        public void Open()
        {
            if (IsOpen || settings == null) return;
            root.SetActive(true);
            settings.SetOpen(true);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            root.SetActive(false);
            settings?.Save();
            settings?.SetOpen(false);
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        void Refresh()
        {
            if (settings == null) return;
            for (int i = 0; i < channels.Length; i++)
            {
                float v = settings.Get(i);
                channels[i].fader.Set(v);
                DeskUi.SetText(channels[i].value, DeskFormat.Percent(v * 100));
            }
            if (mute != null)
            {
                muteVm.Label = MuteLabel; muteVm.Hint = MuteHint; muteVm.On = !settings.Muted;
                mute.Bind(muteVm);
            }
            for (int i = 0; i < languages.Length; i++) languages[i].SetOn((i == 0 ? DeskLang.Tr : DeskLang.En) == settings.Language);
            for (int i = 0; i < sectionCaps.Length && i < sectionRules.Length; i++) DeskUi.CapRule(sectionCaps[i], sectionRules[i], null, sectionWidth);
            foreach (var r in rows) if (r != null) r.Layout();
        }
    }
}
