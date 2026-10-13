using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// C4 KPI kartı (CSS .kpi 202×152, dizin kâğıdı): başlık (CP 11), değer (CP 700 40 + 18 px birim), alta yaslı not (LF 12).
    /// TEMİZ BÖLGE kartında üstü çizili damga (yeşil TEMİZ + kırmızı çarpı, −8°).
    /// </summary>
    public sealed class KpiCardView : MonoBehaviour
    {
        [SerializeField] TMP_Text cap, value, note;
        [SerializeField] RectTransform struck;
        [SerializeField] StampView struckStamp;

        public void Bind(KpiVM k)
        {
            var T = DeskLoc.Instance;
            DeskUi.SetText(cap, T.T(k.Cap));
            DeskUi.SetText(value, string.IsNullOrEmpty(k.Unit) ? k.Value : k.Value + "<space=4px><size=18>" + T.T(k.Unit) + "</size>");
            DeskUi.SetText(note, T.T(k.Note));
            DeskUi.SetActive(struck, k.StruckStamp != null);
            if (k.StruckStamp == null) return;
            struckStamp.Set(T.T(k.StruckStamp.Text), StampView.ParseInk(k.StruckStamp.Color), StampSize.Normal, 0, "kpi.struck");
            struck.localEulerAngles = new Vector3(0, 0, -k.StruckStamp.RotDeg);
        }
    }
}
