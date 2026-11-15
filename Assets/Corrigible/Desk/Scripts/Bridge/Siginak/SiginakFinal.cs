using Corrigible.Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Harekât sonu raporunun oda bağlantısı (Revizyon 2 · S8): rapor açılınca kamera harita pozuna geçer (0,6 s) ve kilitlenir,
    /// damgada ses çalar (kazanınca uzun zil, kaybedince boğuk “tak”); rapor kapanınca (yeniden başla) kilit kalkar. İçeriği
    /// <see cref="DeskPresenter"/> bağlar (<see cref="FinalReportBuilder"/>).
    /// </summary>
    public sealed class SiginakFinal : MonoBehaviour
    {
        [SerializeField] FinalReportView report;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField, Min(0.01f)] float cameraSeconds = 0.6f;

        void OnEnable()
        {
            report.Opened += Opened;
            report.Closed += Closed;
            report.Stamped += Stamped;
        }

        void OnDisable()
        {
            report.Opened -= Opened;
            report.Closed -= Closed;
            report.Stamped -= Stamped;
        }

        void Opened()
        {
            rig.Locked = true;
            rig.Focus(SiginakPose.Map, cameraSeconds);
        }

        void Closed() => rig.Locked = false;

        /// <summary>Damga sesi: kazanınca uzun zil, kayıpta boğuk "tak", düzenli çekilmede daha hafif "tak" (KOS aşama 6).</summary>
        static void Stamped(FinalOutcome outcome)
        {
            if (outcome == FinalOutcome.Won) Sfx.Play(Cue.FinalBell, 0.9f);
            else Sfx.Play(Cue.StampHeavy, outcome == FinalOutcome.Withdrawn ? 0.7f : 0.9f);
        }

        /// <summary>Tab ya da ←/→: alt yarıda HAREKÂT ↔ SİCİL (KOS aşama 6).</summary>
        void Update()
        {
            if (report == null || !report.IsOpen || !report.HasRecord) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.tabKey.wasPressedThisFrame) report.TogglePage();
            else if (kb.leftArrowKey.wasPressedThisFrame) report.ShowPage(0);
            else if (kb.rightArrowKey.wasPressedThisFrame) report.ShowPage(1);
        }
    }
}
