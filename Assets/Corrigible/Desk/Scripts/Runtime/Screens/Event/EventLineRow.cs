using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Bedel/kazanç satırı: değer bedelde kırmızı, kazançta yeşil, RAPOR'da karbon; metin değer (“bulunabilir”) LF 12 kalın.</summary>
    public sealed class EventLineRow : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] CarbonText value;
        [SerializeField] TrustMarkView mark;
        [SerializeField] Image divider;
        [Tooltip("Değerin sağ kenarı satırın sağından (20 px güven sütunu + 7 aralık).")]
        [SerializeField] float valueRightInset = 27, markColumn = 20;

        public void Bind(EventLineVM l, bool gain, float width, bool last)
        {
            var T = DeskLoc.Instance;
            DeskUi.SetText(label, T.T(l.Label));
            var theme = DeskTheme.Active;
            var main = value.Main;
            var font = l.ValueIsText ? theme.lfBold : theme.cpBold;
            if (main.font != font) main.font = font;
            float size = l.ValueIsText ? 12 : 14;
            if (!Mathf.Approximately(main.fontSize, size)) main.fontSize = size;
            var trust = string.IsNullOrEmpty(l.Trust) ? (TrustLevel?)null : DeskUi.ParseTrust(l.Trust);
            value.Set(T.T(l.Value), trust == TrustLevel.Report, gain ? "ink.green" : "ink.red");
            var vr = (RectTransform)value.transform;
            vr.anchorMin = vr.anchorMax = new Vector2(0, 0.5f); vr.pivot = new Vector2(1, 0.5f);
            vr.anchoredPosition = new Vector2(width - valueRightInset, vr.anchoredPosition.y);

            DeskUi.SetActive(mark, trust.HasValue);
            if (trust.HasValue)
            {
                mark.Set(trust.Value, false, trust == TrustLevel.Report ? "~" : null);
                var mr = (RectTransform)mark.transform;
                mr.anchorMin = mr.anchorMax = new Vector2(0, 0.5f); mr.pivot = new Vector2(0.5f, 0.5f);
                mr.anchoredPosition = new Vector2(width - markColumn / 2, 0);
            }
            DeskUi.SetActive(divider, !last);
            // Noktalı çizgi satır kutusu boyunca (CSS alt kenarlığı); satır belge kipinde 654 px genişler
            var dr = divider.rectTransform;
            float rowW = ((RectTransform)transform).sizeDelta.x;
            if (Mathf.Approximately(dr.anchorMin.x, dr.anchorMax.x) && !Mathf.Approximately(dr.sizeDelta.x, rowW)) dr.sizeDelta = new Vector2(rowW, dr.sizeDelta.y);
        }
    }
}
