using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Telsiz kayıt defterinin satırları (Revizyon 2 · S1, tex2.js logBook): solda kod (CP), sağda kayıt (Kalam, mürekkep
    /// mavisi). Satırları bağlayıcı yazar (<c>RadioLogBuilder</c>); eksik satır gizlenir.
    /// Revizyon 3 (<see cref="page"/> doluysa): defter modelinin sayfa yuvası; kayıtlar alt alta tek metin, kod CP Bold.
    /// </summary>
    public sealed class RadioLogBook : MonoBehaviour
    {
        [SerializeField] TMP_Text[] codes = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text[] lines = Array.Empty<TMP_Text>();
        [Tooltip("Revizyon 3 sayfa yuvası (LogBook TXT_EntriesL; Kalam, mürekkep mavisi). Doluysa satır dizileri kullanılmaz.")]
        [SerializeField] TMP_Text page;
        [Tooltip("Kod sütununun fontu (TMP Resources/Fonts & Materials altındaki ad) ve rengi.")]
        [SerializeField] string codeFont = "CP-Bold SDF";
        [SerializeField] Color codeColor = new Color(0.275f, 0.247f, 0.204f);
        [SerializeField, Min(1)] int pageEntries = 4;
        [Tooltip("Kayıt metninin sol girintisi (sayfa genişliğinin yüzdesi); sarılan satır kodun altına değil metin sütununa döner.")]
        [SerializeField, Range(0, 60)] float textIndentPercent = 24;

        public int Capacity => page != null ? pageEntries : Mathf.Min(codes.Length, lines.Length);
        public string LastLine { get; private set; }

        public void Show(IReadOnlyList<(string code, string text)> entries)
        {
            LastLine = entries != null && entries.Count > 0 ? Corrigible.Core.Loc.F("{0} · {1}", entries[entries.Count - 1].code, entries[entries.Count - 1].text) : null;
            if (page != null)
            {
                var sb = new System.Text.StringBuilder();
                string color = ColorUtility.ToHtmlStringRGB(codeColor);
                int count = entries == null ? 0 : Mathf.Min(entries.Count, pageEntries);
                for (int i = 0; i < count; i++)
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append("<font=\"").Append(codeFont).Append("\"><color=#").Append(color).Append(">").Append(entries[i].code)
                        .Append("</color></font><indent=").Append(textIndentPercent.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture))
                        .Append("%>").Append(entries[i].text).Append("</indent>");
                }
                DeskUi.SetText(page, sb.ToString());
                return;
            }
            for (int i = 0; i < Capacity; i++)
            {
                bool on = entries != null && i < entries.Count;
                DeskUi.SetActive(codes[i], on);
                DeskUi.SetActive(lines[i], on);
                if (!on) continue;
                DeskUi.SetText(codes[i], entries[i].code);
                DeskUi.SetText(lines[i], entries[i].text);
            }
        }
    }
}
