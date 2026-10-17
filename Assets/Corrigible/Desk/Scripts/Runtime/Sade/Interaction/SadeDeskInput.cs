using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>F3 kısayollarının tek sahibi; onay/pause açıkken masa eylemleri geçmez.</summary>
    public sealed class SadeDeskInput : MonoBehaviour
    {
        [SerializeField] SadeDeskInteraction interaction;
        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || interaction == null) return;
            bool enter = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
            if (interaction.ConfirmationOpen)
            {
                if (enter) interaction.Confirm();
                else if (kb.escapeKey.wasPressedThisFrame) interaction.Escape();
                return;
            }
            if (kb.escapeKey.wasPressedThisFrame) { interaction.Escape(); return; }
            if (interaction.State.Pause) return;
            if (kb.rKey.wasPressedThisFrame) interaction.ToggleDrawer("research");
            if (kb.nKey.wasPressedThisFrame) interaction.ToggleDrawer("news");
            if (kb.tKey.wasPressedThisFrame) interaction.ToggleDrawer("teams");
            if (kb.lKey.wasPressedThisFrame) interaction.ToggleDrawer("aiLog");
            if (kb.hKey.wasPressedThisFrame) interaction.ToggleDrawer("guide");
            if (kb.tabKey.wasPressedThisFrame) interaction.SelectNext(kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed ? -1 : 1);
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) interaction.RegionAction("izleme");
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) interaction.RegionAction("denetim");
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) interaction.RegionAction("karantina");
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) interaction.RegionAction("kilit");
            if (kb.bKey.wasPressedThisFrame) interaction.RegionAction("raid");
            if (kb.spaceKey.wasPressedThisFrame) interaction.TogglePause();
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) interaction.StepSpeed(1);
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) interaction.StepSpeed(-1);
        }
    }
}
