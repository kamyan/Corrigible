using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Örnek haritanın ekip jetonu ve yerelleştirilmiş varış fişi.</summary>
    public sealed class SadeTeamTokenView : MonoBehaviour
    {
        [SerializeField] string teamId;
        [SerializeField] TMP_Text letter;
        [SerializeField] GameObject eta;
        [SerializeField] TMP_Text etaText;
        [SerializeField] GameObject highlight;
        public string TeamId => teamId;
        public void SetHighlighted(bool on) => DeskUi.SetActive(highlight, on);

        public void Bind(SadeMapTeamVM vm)
        {
            DeskUi.SetText(letter, vm.Id);
            DeskUi.SetActive(eta, vm.Status == "travelling");
            if (etaText != null)
            {
                string text = Loc.F("{0} gün", DeskFormat.Number(vm.EtaDays, 0));
                DeskUi.SetText(etaText, text);
                float width = Mathf.Ceil(SadeUi.Width(etaText, text));
                DeskUi.SetWidth(etaText.rectTransform, Mathf.Max(40, width));
                if (eta != null) DeskUi.SetWidth((RectTransform)eta.transform, Mathf.Max(66, width + 28));
            }
        }
    }
}
