using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sağ üstteki tek sabit ekran öğesi (K1 notu): bakalit plaka “AYARLAR”; kilitli dizilerde “ATLA” (tıklanır; Esc ve Boşluk
    /// kısayolu yazılmaz). Kenar satırı açıkken gizlenir (kenar satırında aynı düğme vardır).
    /// </summary>
    public sealed class SettingsPlaque : MonoBehaviour
    {
        [SerializeField] GameObject plaque;
        [SerializeField] Button button;
        [SerializeField] TMP_Text label;
        [SerializeField] string settingsLabel, skipLabel;

        public event Action SettingsClicked, SkipClicked;
        public bool SkipMode { get; private set; }

        public void Configure(GameObject root, Button plaqueButton, TMP_Text labelText, string settingsSource, string skipSource)
        {
            plaque = root; button = plaqueButton; label = labelText;
            settingsLabel = settingsSource; skipLabel = skipSource;
        }

        void Awake() => button.onClick.AddListener(() => { if (SkipMode) SkipClicked?.Invoke(); else SettingsClicked?.Invoke(); });

        public void SetMode(bool skip, bool hidden)
        {
            if (plaque.activeSelf == hidden) plaque.SetActive(!hidden);
            SkipMode = skip;
            string l = Corrigible.Core.Loc.T(skip ? skipLabel : settingsLabel);
            if (label.text != l) label.text = l;
        }
    }
}
