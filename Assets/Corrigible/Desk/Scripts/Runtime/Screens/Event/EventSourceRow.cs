using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bildirimdeki kaynak satırı (CSS .ev-r: 120 px | esnek | 52 px, aralık 10): güven (onaylandı damgası −3° · RAPOR fişi ·
    /// şüpheli · BİLİNMİYOR), kalın başlık + açıklama (RAPOR'da karbon), sağda gün. Son satırda alt noktalı çizgi yok.
    /// </summary>
    public sealed class EventSourceRow : MonoBehaviour
    {
        [SerializeField] TrustMarkView mark;
        [SerializeField] StampView stamp;
        [SerializeField] CarbonText title, text;
        [SerializeField] TMP_Text day;
        [SerializeField] Image divider;

        public void Bind(EventSourceVM s, int index, bool last)
        {
            var T = DeskLoc.Instance;
            var t = DeskUi.ParseTrust(s.Trust);
            bool verified = t == TrustLevel.Verified;
            DeskUi.SetActive(stamp, verified);
            DeskUi.SetActive(mark, !verified);
            if (verified) stamp.Set(T.T("ONAYLANDI"), StampInk.Blue, StampSize.Small, s.StampRot, "event.source." + index);
            else mark.Set(t);
            bool carbon = s.Carbon || t == TrustLevel.Report;
            title.Set(T.T(s.Title), carbon);
            text.Set(T.T(s.Text), carbon, "ink.ink2");
            DeskUi.SetText(day, s.Day);
            DeskUi.SetActive(divider, !last);
        }
    }
}
