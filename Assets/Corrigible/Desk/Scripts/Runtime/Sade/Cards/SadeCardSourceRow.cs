using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Tam dosyada kaynak satırı; değiştirilmiş kayıt uyarısı VM bayrağıyla görünür.</summary>
    public sealed class SadeCardSourceRow : MonoBehaviour
    {
        [SerializeField] TMP_Text nameLabel, sub, value, day, flagText, question, slipText;
        [SerializeField] Image verified, suspectRing, slipFill, slipFrame, flagIcon;
        [SerializeField] Graphic divider;

        public float Height { get; private set; }

        public void Bind(JObject data, bool last)
        {
            if (data == null) return;
            bool flagged = (string)data["flag"] == "aiLogTampered";
            Height = flagged ? 40.3f : 34;
            ((RectTransform)transform).sizeDelta = new Vector2(((RectTransform)transform).sizeDelta.x, Height);
            SadeUi.Type(nameLabel, "sourceName");
            DeskUi.SetText(nameLabel, SadeCardText.Literal((string)data["name"]));
            DeskUi.SetText(sub, SadeCardText.Literal((string)data["sub"]));
            float nw = SadeUi.Width(nameLabel, nameLabel.text);
            SadeUi.Place(nameLabel, 0, flagged ? 3 : 10, Mathf.Min(nw, 266), 17);
            SadeUi.Place(sub, Mathf.Min(nw + 8, 230), flagged ? 5 : 12, 80, 14);
            SadeUi.Type(value, "sourceValue");
            bool unknown = data["value"] == null || data["value"].Type == JTokenType.Null;
            DeskUi.SetText(value, unknown ? "?" : DeskFormat.Percent((float)data["value"], 0));
            DeskUi.SetText(day, (string)data["day"]);
            var trust = DeskUi.ParseTrust((string)data["trust"]);
            DeskUi.SetActive(verified, trust == TrustLevel.Verified);
            DeskUi.SetActive(suspectRing, trust == TrustLevel.Suspicious);
            DeskUi.SetActive(question, trust == TrustLevel.Suspicious || trust == TrustLevel.Unknown);
            DeskUi.SetActive(slipFill, trust == TrustLevel.Report); DeskUi.SetActive(slipFrame, trust == TrustLevel.Report); DeskUi.SetActive(slipText, trust == TrustLevel.Report);
            DeskUi.SetActive(flagText, flagged); DeskUi.SetActive(flagIcon, flagged);
            if (flagged) DeskUi.SetText(flagText, SadePanelText.T("dossier.tampered"));
            DeskUi.SetActive(divider, !last);
            float mid = Height / 2;
            SadeUi.Place(value, 266, mid - 10, 52, 20);
            SadeUi.Place(day, 318, mid - 8, 64, 18);
            SadeUi.Place(verified, 402, mid - 12, 24, 24);
            SadeUi.Place(suspectRing, 397, mid - 13, 34, 26);
            SadeUi.Place(question, 407, mid - 12, 14, 24);
            SadeUi.Place(slipFill, 402, mid - 11, 24, 22); SadeUi.Place(slipFrame, 402, mid - 11, 24, 22); SadeUi.Place(slipText, 402, mid - 11, 24, 22);
            if (divider != null) SadeUi.Place(divider, 0, Height - 1, 428, 1);
        }
    }
}
