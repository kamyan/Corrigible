using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Seçenek kartında BEDEL / KAZANÇ listesi (CSS .ol): 18 px başlık (eksi/artı daire, harf aralıklı başlık, %50 çizgi) + 22 px
    /// satırlar (etiket · sağa yaslı değer CP 14 · 20 px güven sütunu), satır arası noktalı çizgi.
    /// </summary>
    public sealed class EventLineList : MonoBehaviour
    {
        [SerializeField] TMP_Text header;
        [SerializeField] RectTransform line;
        [SerializeField] EventLineRow rowTemplate;
        [SerializeField] bool gain;
        [SerializeField] float headHeight = 18, rowHeight = 22;

        DeskPool<EventLineRow> pool;

        /// <summary>Listeyi kurar ve yüksekliğini döner.</summary>
        public float Bind(string title, List<EventLineVM> items, float width)
        {
            DeskUi.SetText(header, DeskLoc.Instance.T(title));
            float hx = header.rectTransform.anchoredPosition.x + Mathf.Round(DeskUi.TextWidth(header, header.text)) + 6;
            line.anchoredPosition = new Vector2(hx, line.anchoredPosition.y);
            line.sizeDelta = new Vector2(width - hx, line.sizeDelta.y);
            var rt = (RectTransform)transform;
            pool ??= new DeskPool<EventLineRow>(rowTemplate, rt);
            int n = items?.Count ?? 0;
            for (int i = 0; i < n; i++)
            {
                var r = pool.Get(i);
                var rr = (RectTransform)r.transform;
                rr.anchorMin = rr.anchorMax = new Vector2(0, 1); rr.pivot = new Vector2(0, 1);
                rr.anchoredPosition = new Vector2(0, -(headHeight + rowHeight * i));
                rr.sizeDelta = new Vector2(width, rowHeight);
                r.Bind(items[i], gain, width, i == n - 1);
            }
            pool.Trim(n);
            float h = headHeight + rowHeight * n;
            rt.sizeDelta = new Vector2(width, h);
            return h;
        }
    }
}
