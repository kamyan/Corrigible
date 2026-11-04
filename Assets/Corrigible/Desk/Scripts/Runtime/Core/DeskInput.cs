using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masa klavye haritası: üstteki kâğıt ya da dolap tuşları alır (ayarlar dolabı: Esc; oyun sonu: Enter), yoksa oda kısayolları (3 görev fişi,
    /// Esc). Kısayollar ekranda yazmaz; her biri bir tıklamanın karşılığıdır. Operasyon raporu ve vardiya raporu kendi tuşlarını
    /// okur (masa o sırada kilitli). Kamera pozlarını <see cref="SiginakCameraRig"/> kendisi okur. <c>Keyboard.current</c>
    /// okunur; her karede bellek ayırmaz.
    /// </summary>
    public sealed class DeskInput : MonoBehaviour
    {
        [SerializeField] DeskPresenter presenter;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || presenter.Commands == null) return;
            bool enter = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;

            var cabinet = SettingsCabinet.Instance;
            if (cabinet != null && cabinet.IsOpen) { if (kb.escapeKey.wasPressedThisFrame) cabinet.Close(); return; }
            var final = FinalReportView.Instance;
            if (final != null && final.IsOpen) { if (enter) final.PressRestart(); return; }

            var surface = presenter.Surface;
            if (surface == null || surface.InputBlocked) return;
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) surface.ShowPlanning();
            if (kb.escapeKey.wasPressedThisFrame) surface.Escape();
        }
    }
}
