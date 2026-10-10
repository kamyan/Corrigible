using UnityEngine;

namespace Corrigible.Game.Audio
{
    /// <summary>Tek seferlik ses olayları. Klipleri <see cref="SoundBank.cues"/> bağlar.</summary>
    public enum Cue
    {
        // Arayüz
        Click, Hover, Deny, Confirm, RegionSelect, RegionSelectRadio,
        // Kriz masası
        Monitor, Audit, Quarantine, QuarantineLift, Veto, ResearchStart, ResearchDone, Sabotage,
        VerifyTrue, VerifyFake, VerifyUnknown, Gift, GiftAccept, GiftReject, GiftControlled, Advisor, TeamRadio, LogBlip,
        // Sonlar
        EndAlign, EndErase, EndTakeover, EndDissolve, FakeFanfare, EndSilent,
        // Baskın
        RaidSelect, RaidStep, RaidCutter, RaidEmpCharge, RaidEmpBlast, RaidHookThrow, RaidDroneShot, RaidMelee, RaidHit, RaidHitObject,
        RaidArmor, RaidRepair, RaidPush, RaidUnitDown, RaidRobotDown, RaidCivilianBreak, RaidExplosion, RaidCoreBreak, RaidBreak, RaidSpawn,
        IntentLock, IntentLockFuzzy, RaidEndTurn, RaidBreach, RaidRadio, RaidWin, RaidFail,
    }

    /// <summary>Sürekli çalan döngüler (sesi duruma göre açılıp kısılır).</summary>
    public enum LoopCue { Heartbeat, Clock, RaidAmbience, RaidClock }

    /// <summary>
    /// Ses çalmanın tek giriş noktası (görünümler buradan çağırır). Sesi ana sahnedeki <see cref="AudioDirector"/> çalar;
    /// o yoksa (ör. Baskin sahnesi tek başına açıldı) sessizce hiçbir şey yapmaz.
    /// </summary>
    public static class Sfx
    {
        /// <param name="pan">-1 sol, 1 sağ.</param>
        /// <param name="pitch">1 = özgün perde.</param>
        public static void Play(Cue cue, float volume = 1f, float pan = 0f, float pitch = 1f) => AudioDirector.Instance?.PlayCue(cue, volume, pan, pitch);

        public static void SetLoop(LoopCue loop, bool on) => AudioDirector.Instance?.SetLoop(loop, on);

        /// <summary>Dünya konumunu ekrandaki yatay konuma göre sağ-sol dengesine çevirir.</summary>
        public static float PanOf(Camera cam, Vector3 world)
        {
            if (cam == null) return 0;
            return Mathf.Clamp((cam.WorldToViewportPoint(world).x - 0.5f) * 1.4f, -0.8f, 0.8f);
        }
    }
}
