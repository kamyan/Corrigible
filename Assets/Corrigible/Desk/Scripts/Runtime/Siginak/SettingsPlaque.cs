using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sağ üstteki tek sabit ekran öğesi (K1 notu): bakalit plaka “Esc · AYARLAR”; kilitli dizilerde “Boşluk · ATLA”.
    /// Kenar satırı açıkken gizlenir (kenar satırında aynı düğme vardır).
    /// </summary>
    public sealed class SettingsPlaque : MonoBehaviour
    {
        [SerializeField] GameObject plaque;
        [SerializeField] Button button;
        [SerializeField] TMP_Text key, label;
        [SerializeField] string settingsKey = "Esc", settingsLabel, skipKey = "Boşluk", skipLabel;

        public event Action SettingsClicked, SkipClicked;
        public bool SkipMode { get; private set; }

        public void Configure(GameObject root, Button plaqueButton, TMP_Text keyText, TMP_Text labelText, string settingsSource, string skipKeySource, string skipSource)
        {
            plaque = root; button = plaqueButton; key = keyText; label = labelText;
            settingsLabel = settingsSource; skipKey = skipKeySource; skipLabel = skipSource;
        }

        void Awake() => button.onClick.AddListener(() => { if (SkipMode) SkipClicked?.Invoke(); else SettingsClicked?.Invoke(); });

        public void SetMode(bool skip, bool hidden)
        {
            if (plaque.activeSelf == hidden) plaque.SetActive(!hidden);
            SkipMode = skip;
            string k = skip ? Corrigible.Core.Loc.T(skipKey) : settingsKey, l = Corrigible.Core.Loc.T(skip ? skipLabel : settingsLabel);
            if (key.text != k) key.text = k;
            if (label.text != l) label.text = l;
        }
    }
}
