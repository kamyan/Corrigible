using UnityEngine;

namespace Corrigible.Game.Audio
{
    /// <summary>
    /// Tek seferlik ses olayları. Klipleri <see cref="SoundBank.cues"/> bağlar; asset değeri sayı olarak sakladığı için her
    /// üyenin sayısı sabittir (silinen sesler numaralarını boş bırakır).
    /// </summary>
    public enum Cue
    {
        // Arayüz
        Click = 0, TeamRadio = 22,
        // Son
        EndErase = 25,
        // Baskın
        RaidStep = 31, RaidEmpBlast = 34, RaidDroneShot = 36, RaidMelee = 37, RaidHit = 38, RaidHitObject = 39,
        RaidArmor = 40, RaidRepair = 41, RaidPush = 42, RaidUnitDown = 43, RaidRobotDown = 44, RaidCivilianBreak = 45, RaidExplosion = 46,
        RaidCoreBreak = 47, RaidBreak = 48, RaidSpawn = 49,
        // Sığınağın kapısı
        DoorKnock = 50, DoorOpen = 51, DoorShot = 52,
        // Telsiz: sahayla tek bağ (hat açılır, emir iletilir, gözcü bildirir)
        RadioTune = 53, RadioSend = 54, RadioReport = 55, RadioVoice = 56,
    }

    /// <summary>Sürekli çalan döngüler (sesi duruma göre açılıp kısılır).</summary>
    public enum LoopCue { RaidAmbience = 2, RadioHiss = 3 }

    /// <summary>
    /// Ses çalmanın tek giriş noktası (görünümler buradan çağırır). Sesi ana sahnedeki <see cref="AudioDirector"/> çalar;
    /// o yoksa (ör. Baskin sahnesi tek başına açıldı) sessizce hiçbir şey yapmaz.
    /// </summary>
    public static class Sfx
    {
        /// <param name="pan">-1 sol, 1 sağ.</param>
        /// <param name="pitch">1 = özgün perde.</param>
        public static void Play(Cue cue, float volume = 1f, float pan = 0f, float pitch = 1f) => AudioDirector.Instance?.PlayCue(cue, volume, pan, pitch);

        /// <summary>Dünya konumunu ekrandaki yatay konuma göre sağ-sol dengesine çevirir.</summary>
        public static float PanOf(Camera cam, Vector3 world)
        {
            if (cam == null) return 0;
            return Mathf.Clamp((cam.WorldToViewportPoint(world).x - 0.5f) * 1.4f, -0.8f, 0.8f);
        }
    }
}
