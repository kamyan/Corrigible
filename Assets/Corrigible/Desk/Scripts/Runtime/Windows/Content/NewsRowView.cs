using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Haber satırı: teleks kâğıdı şerit; gün + kaynak + güven işareti, altında metin (kaydırılır). Sahte haber soluk + çizili + SAHTE damgası.</summary>
    public sealed class NewsRowView : MonoBehaviour
    {
        [SerializeField] DeskRow head;
        [SerializeField] TMP_Text day, source, text;
        [SerializeField] TrustMarkView mark;
        [SerializeField] StampView stamp;

        public float Height { get; private set; }

        public void Bind(TelexItemVM i, float width)
        {
            var T = DeskLoc.Instance;
            DeskUi.SetText(day, i.Day);
            DeskUi.SetText(source, i.Critical && !i.Fake ? Corrigible.Core.Loc.F("KRİTİK · {0}", T.T(i.Source)) : T.T(i.Source));
            DeskUi.Tint(source, i.Critical && !i.Fake ? "ink.red" : "ink.ink2");
            var trust = DeskUi.ParseTrust(i.Trust);
            bool useStamp = i.Fake || trust == TrustLevel.Verified;
            DeskUi.SetActive(stamp, useStamp);
            DeskUi.SetActive(mark, !useStamp);
            if (i.Fake && i.Stamp != null) stamp.Set(T.T(i.Stamp.Text), StampView.ParseInk(i.Stamp.Color), StampSize.Small, i.Stamp.Rot, "news." + i.Text);
            else if (useStamp) stamp.Set(T.T("ONAYLANDI"), StampInk.Blue, StampSize.Small, i.StampRot, "news." + i.Text);
            else mark.Set(trust);
            head.Layout();
            // TMP <s> çizgisinin rengi ve kalınlığı ayarlanamaz; çok satırlı metinde kabul edilen yaklaşık
            DeskUi.SetText(text, i.Fake ? "<s>" + T.T(i.Text) + "</s>" : T.T(i.Text));
            DeskUi.Tint(text, i.Fake ? "ink.ink3" : "ink.ink");
            float th = text.GetPreferredValues(text.text, width - 20, 1000).y;
            Height = Mathf.Ceil(32 + th + 8);
        }
    }
}
