using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Ekranın üst kenarındaki 40 px bakalit şerit (K4 notu): sayaç rafı kadraj dışındayken aynı değerleri ve istasyon
    /// tuşlarını gösterir. Ayar: Otomatik (varsayılan) / Her zaman / Kapalı (oyuncu tercihi). Tuşlara tıklanabilir.
    /// </summary>
    public sealed class EdgeLine : MonoBehaviour
    {
        public enum Mode { Auto = 0, Always = 1, Off = 2 }
        public const string PrefKey = "corrigible.siginak.edgeLine";

        [SerializeField] GameObject bar;
        [SerializeField] TMP_Text values;
        [Tooltip("İstasyon tuşları (anahtar rayı sırası) ve en sonda AYARLAR.")]
        [SerializeField] Button[] chips = Array.Empty<Button>();
        [SerializeField] Image[] chipBacks = Array.Empty<Image>();
        [SerializeField] TMP_Text[] chipLabels = Array.Empty<TMP_Text>();
        [Tooltip("Çiplerin tuş kutuları ve çiplerin grubu: harekât bitince kutular kesik, çipler soluk (Revizyon 2 · S4c).")]
        [SerializeField] Image[] chipKeys = Array.Empty<Image>();
        [SerializeField] CanvasGroup chipGroup;
        [SerializeField] Sprite keyDashed;
        [SerializeField, Range(0, 1)] float endedAlpha = 0.45f;

        Sprite[] keySolid = Array.Empty<Sprite>();
        bool ended, locked;

        public event Action<int> StationClicked;
        public event Action SettingsClicked;
        public bool Visible => bar != null && bar.activeSelf;

        public static Mode Preference
        {
            get { try { return (Mode)PlayerPrefs.GetInt(PrefKey, 0); } catch { return Mode.Auto; } }
            set => PlayerPrefs.SetInt(PrefKey, (int)value);
        }

        public void Configure(GameObject barRoot, TMP_Text valueText, Button[] keyChips, Image[] keyBacks, TMP_Text[] keyLabels)
        {
            bar = barRoot; values = valueText; chips = keyChips; chipBacks = keyBacks; chipLabels = keyLabels;
        }

        void Awake()
        {
            for (int i = 0; i < chips.Length; i++)
            {
                int index = i;
                chips[i].onClick.AddListener(() => { if (index == chips.Length - 1) SettingsClicked?.Invoke(); else StationClicked?.Invoke(index); });
            }
        }

        /// <summary>Görünürlük: otomatikte yalnız sayaç rafı kadraj dışındayken.</summary>
        public void SetVisible(bool counterBankInView, bool locked)
        {
            var mode = Preference;
            bool show = mode == Mode.Always || mode == Mode.Auto && !counterBankInView;
            if (bar.activeSelf != show) bar.SetActive(show);
            this.locked = locked;
            foreach (var c in chips) c.interactable = !locked && !ended;
        }

        /// <summary>Harekât bitti (ölümcül kapı, son rapor): tuşlar kesikli ve soluk, tıklanmaz; yalnız Enter etkin.</summary>
        public void SetEnded(bool on)
        {
            if (ended == on) return;
            ended = on;
            if (keySolid.Length != chipKeys.Length)
            {
                keySolid = new Sprite[chipKeys.Length];
                for (int i = 0; i < chipKeys.Length; i++) keySolid[i] = chipKeys[i] != null ? chipKeys[i].sprite : null;
            }
            for (int i = 0; i < chipKeys.Length; i++)
            {
                var key = chipKeys[i];
                if (key == null) continue;
                key.sprite = on && keyDashed != null ? keyDashed : keySolid[i];
                key.type = on && keyDashed != null ? Image.Type.Tiled : Image.Type.Sliced;
            }
            if (chipGroup != null) chipGroup.alpha = on ? endedAlpha : 1;
            foreach (var c in chips) c.interactable = !locked && !on;
        }

        public void SetValues(string text) { if (values.text != text) values.text = text; }

        public void SetActiveStation(int index)
        {
            var t = SiginakTheme.Active;
            for (int i = 0; i < chipBacks.Length; i++)
            {
                if (chipBacks[i] != null) chipBacks[i].color = i == index ? t.ColorOf("ui.edgeLineActive") : Color.clear;
                // Etkin çip pirinç zeminde koyu yazı (K4-TR)
                if (i < chipLabels.Length && chipLabels[i] != null) chipLabels[i].color = t.ColorOf(i == index ? "ui.edgeLine" : "ui.edgeLineActive");
            }
        }
    }
}
