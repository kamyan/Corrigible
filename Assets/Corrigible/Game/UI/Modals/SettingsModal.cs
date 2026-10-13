using System;
using Corrigible.Core;
using Corrigible.Game.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Ayarlar penceresi: ses düzeyleri (genel, müzik, efektler, ortam), sesi kapatma ve dil. Diğer pencerelerin üstünde
    /// açılır (giriş penceresinden de); açıkken oyun zamanı durur. Esc açar/kapatır.
    /// </summary>
    public sealed class SettingsModal : MonoBehaviour
    {
        [Serializable]
        public struct VolumeRow
        {
            public AudioPrefs.Channel channel;
            public Slider slider;
            public TMP_Text value;
        }

        [SerializeField] GameController game;
        [SerializeField] UiTheme theme;
        [Tooltip("Karartma ve pencere; edit modda kapalı.")]
        [SerializeField] GameObject root;
        [SerializeField] VolumeRow[] rows = Array.Empty<VolumeRow>();
        [SerializeField] StyledButton mute;
        [SerializeField] StyledButton language;
        [SerializeField] StyledButton close;
        [Tooltip("Pencereyi açan düğmeler (üst çubuk, giriş penceresi).")]
        [SerializeField] StyledButton[] openers = Array.Empty<StyledButton>();

        bool heldBefore;
        float lastPreview;

        public bool IsOpen => root.activeSelf;

        void Awake()
        {
            root.SetActive(false);
            foreach (var r in rows)
            {
                var row = r;
                row.slider.minValue = 0; row.slider.maxValue = 1;
                row.slider.SetValueWithoutNotify(AudioPrefs.Get(row.channel));
                row.slider.onValueChanged.AddListener(v => OnSlider(row, v));
            }
            UiUtil.OnClick(mute.Button, () => { AudioPrefs.Muted = !AudioPrefs.Muted; Refresh(); });
            UiUtil.OnClick(language.Button, Localization.Next);
            UiUtil.OnClick(close.Button, Close);
            foreach (var o in openers) if (o != null) UiUtil.OnClick(o.Button, Open);
            Refresh();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (IsOpen) Close();
            else if (DeskWindows.Instance != null && DeskWindows.Instance.AnyOpen) DeskWindows.Instance.CloseAll();   // önce açık pencereler kapanır
            else if (game.Sim == null || game.Sim.S.Raid == null) Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            heldBefore = game.Hold;
            game.Hold = true;
            foreach (var r in rows) r.slider.SetValueWithoutNotify(AudioPrefs.Get(r.channel));
            Refresh();
            root.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            root.SetActive(false);
            game.Hold = heldBefore;
            AudioPrefs.Save();
        }

        void OnSlider(VolumeRow row, float v)
        {
            AudioPrefs.Set(row.channel, v);
            row.value.text = Percent(v);
            // Efekt düzeyini değiştirirken nasıl duyulduğunu dinlet (sık tetiklenmesin)
            if (row.channel != AudioPrefs.Channel.Music && Time.unscaledTime - lastPreview > 0.12f)
            {
                lastPreview = Time.unscaledTime;
                Sfx.Play(row.channel == AudioPrefs.Channel.Ambience ? Cue.TeamRadio : Cue.Click);
            }
        }

        void Refresh()
        {
            foreach (var r in rows) r.value.text = Percent(AudioPrefs.Get(r.channel));
            mute.SetText(AudioPrefs.Muted ? Loc.T("Ses: KAPALI") : Loc.T("Ses: AÇIK"));
            mute.SetColors(text: AudioPrefs.Muted ? theme.warn : theme.fg);
            language.SetText(Loc.T("Dil: Türkçe · Language: English"));
        }

        static string Percent(float v) => Loc.F("%{0}", Mathf.RoundToInt(v * 100));
    }
}
