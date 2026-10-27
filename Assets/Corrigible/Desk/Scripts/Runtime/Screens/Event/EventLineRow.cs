using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Sonuç satırı: değer bedelde kırmızı, kazançta yeşil; metin değer (“GERİ ALINDI”) LF 12 kalın.</summary>
    public sealed class EventLineRow : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] CarbonText value;
        [SerializeField] Image divider;
        [Tooltip("Değerin sağ kenarı satırın sağından.")]
        [SerializeField] float valueRightInset = 27;

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
            value.Set(T.T(l.Value), false, gain ? "ink.green" : "ink.red");
            var vr = (RectTransform)value.transform;
            vr.anchorMin = vr.anchorMax = new Vector2(0, 0.5f); vr.pivot = new Vector2(1, 0.5f);
            vr.anchoredPosition = new Vector2(width - valueRightInset, vr.anchoredPosition.y);

            DeskUi.SetActive(divider, !last);
            // Noktalı çizgi satır kutusu boyunca (CSS alt kenarlığı); satır belge kipinde 654 px genişler
            var dr = divider.rectTransform;
            float rowW = ((RectTransform)transform).sizeDelta.x;
            if (Mathf.Approximately(dr.anchorMin.x, dr.anchorMax.x) && !Mathf.Approximately(dr.sizeDelta.x, rowW)) dr.sizeDelta = new Vector2(rowW, dr.sizeDelta.y);
        }
    }
}
