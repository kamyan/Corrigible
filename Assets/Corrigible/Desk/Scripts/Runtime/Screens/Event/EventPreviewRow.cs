using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Seçili seçeneğin ÖNİZLEME notundaki satır: etiket (LF 12) · değer (CP 13 kalın; RAPOR'da karbon) · onaylıysa mühür · sağda not.</summary>
    public sealed class EventPreviewRow : MonoBehaviour
    {
        [SerializeField] DeskRow row;
        [SerializeField] TMP_Text label;
        [SerializeField] CarbonText value;
        [SerializeField] TrustMarkView mark;
        [SerializeField] TMP_Text right;

        public void Bind(EventPreviewRowVM r)
        {
            var T = DeskLoc.Instance;
            DeskUi.SetText(label, T.T(r.Label));
            var trust = string.IsNullOrEmpty(r.Trust) ? TrustLevel.Unknown : DeskUi.ParseTrust(r.Trust);
            value.Set(r.Value, trust == TrustLevel.Report);
            DeskUi.SetActive(mark, trust == TrustLevel.Verified);
            if (trust == TrustLevel.Verified) mark.Set(TrustLevel.Verified, false);
            DeskUi.SetText(right, T.T(r.Right));
            row.Layout();
        }
    }
}
