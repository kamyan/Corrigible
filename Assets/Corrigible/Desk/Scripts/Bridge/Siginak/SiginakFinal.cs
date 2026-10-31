using Corrigible.Game.Audio;
using UnityEngine;

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

        static void Stamped(bool won) => Sfx.Play(won ? Cue.FinalBell : Cue.StampHeavy, 0.9f);
    }
}
