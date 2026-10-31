using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Telsiz kayıt defterinin satırları (Revizyon 2 · S1, tex2.js logBook): solda kod (CP), sağda kayıt (Kalam, mürekkep
    /// mavisi). Satırları bağlayıcı yazar (<c>RadioLogBuilder</c>); eksik satır gizlenir.
    /// </summary>
    public sealed class RadioLogBook : MonoBehaviour
    {
        [SerializeField] TMP_Text[] codes = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text[] lines = Array.Empty<TMP_Text>();

        public int Capacity => Mathf.Min(codes.Length, lines.Length);

        public void Show(IReadOnlyList<(string code, string text)> entries)
        {
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
