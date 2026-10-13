using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Kampanya dosyasında bir kriz: sıra, rakip · sonuç, açıklama, gün, KAZANILDI/KAYBEDİLDİ damgası, noktalı ayraç.</summary>
    public sealed class CampaignResultRow : MonoBehaviour
    {
        [SerializeField] TMP_Text number, title, text, day;
        [SerializeField] StampView stamp;
        [SerializeField] Image separator;

        public void Bind(int n, EventSourceVM vm, bool last)
        {
            var T = DeskLoc.Instance;
            DeskUi.SetText(number, n + ".");
            DeskUi.SetText(title, T.T(vm.Title));
            DeskUi.SetText(text, T.T(vm.Text));
            DeskUi.SetText(day, vm.Day);
            DeskUi.SetActive(stamp, vm.Stamp != null);
            if (vm.Stamp != null) stamp.Set(T.T(vm.Stamp.Text), StampView.ParseInk(vm.Stamp.Color), StampSize.Small, vm.Stamp.Rot, "campaign.row." + n);
            DeskUi.SetActive(separator, !last);
        }
    }
}
