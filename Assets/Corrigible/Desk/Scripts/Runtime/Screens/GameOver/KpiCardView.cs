using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Son rapor KPI kartı (CSS .kpi 202×152, dizin kâğıdı): başlık (CP 11), değer (CP 700 40 + 18 px birim), alta yaslı not (LF 12).
    /// </summary>
    public sealed class KpiCardView : MonoBehaviour
    {
        [SerializeField] TMP_Text cap, value, note;

        public void Bind(KpiVM k)
        {
            var T = DeskLoc.Instance;
            DeskUi.SetText(cap, T.T(k.Cap));
            DeskUi.SetText(value, string.IsNullOrEmpty(k.Unit) ? k.Value : k.Value + "<space=4px><size=18>" + T.T(k.Unit) + "</size>");
            DeskUi.SetText(note, T.T(k.Note));
        }
    }
}
