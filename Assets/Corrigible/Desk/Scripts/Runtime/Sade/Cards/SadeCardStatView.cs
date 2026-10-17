using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Bölge kartının yerel güven, bağımlılık ya da ekonomi sütunu.</summary>
    public sealed class SadeCardStatView : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text value, label;
        [SerializeField] ChipTrustMarkView trust;

        public void Bind(JObject data)
        {
            if (data == null) return;
            string id = (string)data["id"];
            SadeUi.Sprite(icon, "ic_" + (string)data["icon"] + "_l", "sade.ink2");
            SadeUi.Type(value, "statValue");
            DeskUi.SetText(value, SadeUi.Value((float?)data["value"] ?? 0, 0, false, (bool?)data["approx"] ?? false));
            SadeUi.Type(label, "statLabel");
            DeskUi.SetText(label, SadePanelText.T("card." + id));
            trust.Set(DeskUi.ParseTrust((string)data["trust"]));
            float block = Mathf.Max(SadeUi.Width(label, label.text), SadeUi.Width(value, value.text) + trust.Width + 5);
            float start = (((RectTransform)transform).rect.width - 20 - 8 - block) / 2;
            SadeUi.Place(icon, start, 13, 20, 20);
            SadeUi.Place(value, start + 28, 6.5f, SadeUi.Width(value, value.text), 22);
            SadeUi.Place(trust, start + 28 + SadeUi.Width(value, value.text) + 5, 9, trust.Width, 14);
            SadeUi.Place(label, start + 28, 28.5f, block, 14);
        }
    }
}
