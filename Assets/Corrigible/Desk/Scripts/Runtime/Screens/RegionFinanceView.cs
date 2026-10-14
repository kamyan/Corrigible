using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Bölgenin güncel fon katkısı; hesaplama görünüm modelinde gelir.</summary>
    public sealed class RegionFinanceView : MonoBehaviour
    {
        [SerializeField] TMP_Text title, contribution, note;

        public void Bind(RegionFinanceVM vm)
        {
            gameObject.SetActive(vm != null);
            if (vm == null) return;
            DeskUi.SetText(title, vm.Title);
            DeskUi.SetText(contribution, vm.Contribution);
            DeskUi.SetText(note, vm.Note);
        }
    }
}
