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
            foreach (var c in chips) c.interactable = !locked;
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
