using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Corrigible.Fx
{
    public sealed class FxMapClickTarget : MonoBehaviour,IPointerClickHandler
    {
        [SerializeField] public string regionId;
        public event Action<string> Clicked;
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Clicked?.Invoke(regionId);}
    }
}
