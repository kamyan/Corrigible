using UnityEngine;
using UnityEngine.EventSystems;
namespace Corrigible.Fx
{
    public sealed class FxRaidPointer : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        [SerializeField] public FxRaidHud owner;
        [SerializeField] public string key;
        public void OnPointerEnter(PointerEventData e)=>owner.Hover(key,true);
        public void OnPointerExit(PointerEventData e)=>owner.Hover(key,false);
    }
}
