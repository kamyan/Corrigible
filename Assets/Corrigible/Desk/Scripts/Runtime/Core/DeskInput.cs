using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masa klavye haritası: üstteki kâğıt tuşları alır (ayarlar: Esc; oyun sonu: Enter), yoksa oda kısayolları (1 ve 3 görev
    /// fişi, H emir belgesi, Esc). Operasyon raporu ve vardiya raporu kendi tuşlarını okur (masa o sırada kilitli). Kamera
    /// pozlarını <see cref="SiginakCameraRig"/> kendisi okur. <c>Keyboard.current</c> okunur; her karede bellek ayırmaz.
    /// </summary>
    public sealed class DeskInput : MonoBehaviour
    {
        [SerializeField] DeskPresenter presenter;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || presenter.Commands == null) return;
            bool enter = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;

            var st = SettingsView.Instance;
            if (st != null && st.IsOpen) { if (kb.escapeKey.wasPressedThisFrame) st.Close(); return; }
            var final = FinalReportView.Instance;
            if (final != null && final.IsOpen) { if (enter) final.PressRestart(); return; }

            var surface = presenter.Surface;
            if (surface == null || surface.InputBlocked) return;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame || kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                surface.ShowPlanning();
            if (kb.hKey.wasPressedThisFrame) surface.OpenGuide();
            if (kb.escapeKey.wasPressedThisFrame) surface.Escape();
        }
    }
}
