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

        TelexItemVM lastBound;
        DeskLang lastLanguage;
        DeskTheme lastTheme;
        bool lastIllusion;

        public float Width => ((RectTransform)transform).sizeDelta.x;

        public void Bind(TelexItemVM i)
        {
            if (i == null) return;
            var T = DeskLoc.Instance;
            if (SameContent(lastBound, i) && lastLanguage == T.Lang && lastTheme == DeskTheme.Active && lastIllusion == DeskTheme.Illusion) return;
            // VM'ler her Flush'ta yeniden üretilir; ayrıca aynı VM yerinde değiştirilebilir.
            lastBound = CopyContent(i);
            lastLanguage = T.Lang;
            lastTheme = DeskTheme.Active;
            lastIllusion = DeskTheme.Illusion;
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

        internal static bool SameContent(TelexItemVM a, TelexItemVM b)
        {
            if (a == null || b == null) return a == b;
            if (a.Id != b.Id || a.Day != b.Day || a.Source != b.Source || a.Text != b.Text || a.Trust != b.Trust || a.Fake != b.Fake || a.StampRot != b.StampRot) return false;
            if (a.Stamp == null || b.Stamp == null) return a.Stamp == b.Stamp;
            return a.Stamp.Text == b.Stamp.Text && a.Stamp.Color == b.Stamp.Color && a.Stamp.FontSize == b.Stamp.FontSize && a.Stamp.Rot == b.Stamp.Rot;
        }

        internal static TelexItemVM CopyContent(TelexItemVM i) => i == null ? null : new TelexItemVM
        {
            Id = i.Id, Day = i.Day, Source = i.Source, Text = i.Text, Trust = i.Trust, Fake = i.Fake, StampRot = i.StampRot,
            Stamp = i.Stamp == null ? null : new StampVM { Text = i.Stamp.Text, Color = i.Stamp.Color, FontSize = i.Stamp.FontSize, Rot = i.Stamp.Rot }
        };
    }
}
