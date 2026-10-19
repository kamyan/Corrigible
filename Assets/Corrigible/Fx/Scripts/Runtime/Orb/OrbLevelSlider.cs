using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class OrbLevelSlider : Slider
    {
        public override void OnMove(AxisEventData data)
        {
            if(IsActive()&&IsInteractable()&&(data.moveDir==MoveDirection.Left||data.moveDir==MoveDirection.Right))
            {
                float step=Keyboard.current!=null&&Keyboard.current.shiftKey.isPressed?10:1;
                value+=(data.moveDir==MoveDirection.Left?-step:step);data.Use();return;
            }
            base.OnMove(data);
        }
    }
}
