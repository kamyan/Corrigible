using UnityEngine;
using UnityEngine.EventSystems;
namespace Corrigible.Desk
{
    public sealed class RaidAbilityChipView : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        [SerializeField] RaidHudView hud;
        public void OnPointerEnter(PointerEventData e)=>hud.ShowAbilityTip();
        public void OnPointerExit(PointerEventData e)=>hud.HideAbilityTip();
    }
}
