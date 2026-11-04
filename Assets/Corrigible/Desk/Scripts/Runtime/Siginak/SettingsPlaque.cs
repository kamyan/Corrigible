using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sağ üstteki bakalit plaka (K1 notu): yalnız kilitli, atlanabilir dizilerde (açılış, vardiya dizisi) “ATLA” olarak görünür
    /// ve tıklanır (Boşluk kısayolu yazılmaz). Ayarlar ekranda düğme değil, odadaki dolaptır (Docs/AYARLAR-DOLABI-VE-MENU.md);
    /// plaka AYARLAR göstermez. Sınıf adı sahnedeki bağlar için korunur.
    /// </summary>
    public sealed class SettingsPlaque : MonoBehaviour
    {
        [SerializeField] GameObject plaque;
        [SerializeField] Button button;
        [SerializeField] TMP_Text label;

        static readonly string SkipSource = Corrigible.Core.Loc.N("ATLA");

        public event Action SkipClicked;
        public bool SkipMode { get; private set; }

        public void Configure(GameObject root, Button plaqueButton, TMP_Text labelText)
        {
            plaque = root; button = plaqueButton; label = labelText;
        }

        void Awake() => button.onClick.AddListener(() => { if (SkipMode) SkipClicked?.Invoke(); });

        /// <summary>Atlanabilir bir dizi oynuyor: plaka “ATLA” olarak görünür; yoksa gizli.</summary>
        public void SetSkip(bool skip)
        {
            SkipMode = skip;
            if (plaque.activeSelf != skip) plaque.SetActive(skip);
            if (!skip) return;
            string l = Corrigible.Core.Loc.T(SkipSource);
            if (label.text != l) label.text = l;
        }
    }
}
