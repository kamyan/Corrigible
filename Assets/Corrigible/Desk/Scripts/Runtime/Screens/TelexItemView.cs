using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Teleks öğesi: gün (CP 13 ink3), kaynak (CP 700 13), metin (CP 13), güven işareti ya da damga; aralık 9.</summary>
    public sealed class TelexItemView : MonoBehaviour
    {
        [SerializeField] DeskRow row;
        [SerializeField] TMP_Text day, source, text;
        [SerializeField] Image strike;
        [SerializeField] TrustMarkView mark;
        [SerializeField] StampView stamp;

        public float Width => ((RectTransform)transform).sizeDelta.x;

        public void Bind(TelexItemVM i)
        {
            var T = DeskLoc.Instance;
            DeskUi.SetText(day, i.Day);
            DeskUi.SetText(source, T.T(i.Source));
            DeskUi.SetText(text, T.T(i.Text));
            DeskUi.Tint(text, i.Fake ? "ink.ink3" : "ink.ink");

            var trust = DeskUi.ParseTrust(i.Trust);
            bool useStamp = i.Fake || trust == TrustLevel.Verified;
            DeskUi.SetActive(stamp, useStamp);
            DeskUi.SetActive(mark, !useStamp);
            if (i.Fake && i.Stamp != null) stamp.Set(T.T(i.Stamp.Text), StampView.ParseInk(i.Stamp.Color), StampSize.Small, i.Stamp.Rot, "telex." + i.Text);
            else if (useStamp) stamp.Set(T.T("ONAYLANDI"), StampInk.Blue, StampSize.Small, i.StampRot, "telex." + i.Text);
            else mark.Set(trust);
            row.Layout();

            DeskUi.SetActive(strike, i.Fake);
            if (i.Fake)
            {
                // Metin genişliğinde 2 px kırmızı çizgi (TMP <s> rengi ve kalınlığı ayarlanamaz)
                var tr = text.rectTransform; var sr = strike.rectTransform;
                sr.anchorMin = sr.anchorMax = tr.anchorMin; sr.pivot = tr.pivot;
                sr.anchoredPosition = tr.anchoredPosition + new Vector2(0, 0.5f);
                sr.sizeDelta = new Vector2(tr.sizeDelta.x, 2);
            }
        }
    }
}
