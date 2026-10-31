using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masa kamerasının girdi kapısı: açık bir ekran (ayarlar, baskın raporu, oyun sonu) ya da sunumun kilidi
    /// (<see cref="ExternalBlocked"/>: hikâye, baskına yaklaşma) varken kamera girdisi kapalıdır; tekerlek ve sağ tuş Overlay
    /// bir yüzeyin üstündeyse kameraya gitmez. Sığınak kamerası (<see cref="SiginakCameraRig"/>) okur.
    /// </summary>
    public sealed class CommandPostDesk : MonoBehaviour
    {
        [SerializeField] DeskPresenter presenter;
        [SerializeField] Camera view;
        PointerEventData pointer;
        EventSystem pointerEvents;
        readonly List<RaycastResult> hits = new List<RaycastResult>(24);

        public bool ExternalBlocked { get; set; }
        public bool InteractionBlocked => ExternalBlocked || !isActiveAndEnabled || view == null || !view.enabled ||
            presenter == null || presenter.Commands == null ||
            (SettingsView.Instance != null && SettingsView.Instance.IsOpen) ||
            (FinalReportView.Instance != null && FinalReportView.Instance.IsOpen);

        /// <summary>İmleç ekranda en üstteki tıklanabilir yüzey olarak bir Overlay canvas'ın üstünde mi.</summary>
        public bool PointerOverOverlay
        {
            get
            {
                var events = EventSystem.current;
                if (events == null || Mouse.current == null) return false;
                if (pointer == null || pointerEvents != events) { pointerEvents = events; pointer = new PointerEventData(events); }
                pointer.Reset();
                pointer.position = Mouse.current.position.ReadValue();
                hits.Clear();
                events.RaycastAll(pointer, hits);
                // EventSystem sırası ekranda en üstteki tıklanabilir yüzeyi verir.
                foreach (var hit in hits)
                {
                    if (!(hit.module is GraphicRaycaster raycaster)) continue;
                    var canvas = raycaster.GetComponent<Canvas>();
                    return canvas != null && canvas.rootCanvas.renderMode != RenderMode.WorldSpace;
                }
                return false;
            }
        }
    }
}
