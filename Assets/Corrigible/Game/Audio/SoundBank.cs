using System;
using UnityEngine;

namespace Corrigible.Game.Audio
{
    /// <summary>
    /// Oyunun sesleri ve karışım ayarları. Şimdilik kodla üretilmiş yer tutucu klipler bağlı; gerçek seslerle değiştirmek
    /// için klipleri buradan değiştirmek yeterli (müzik katmanları aynı uzunlukta, döngüye uygun olmalı).
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Sound Bank", fileName = "SoundBank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Header("Müzik katmanları (aynı uzunlukta döngüler)")]
        [Tooltip("Her zaman çalar.")] public AudioClip drone;
        [Tooltip("İşgal altındaki sektörler çoğaldıkça girer: tehlikeli huzur.")] public AudioClip warm;
        [Tooltip("Dünyanın uğultusu ve paraziti.")] public AudioClip grit;

        [Header("Harekât")]
        [Tooltip("Bir bölge tamamen geri alındı.")] public AudioClip alarm;

        [Header("Ses olayları")]
        [Tooltip("Tek seferlik ses olayları (Cue) ve döngüler (LoopCue).")]
        public CueClip[] cues = Array.Empty<CueClip>();
        public LoopClip[] loops = Array.Empty<LoopClip>();

        [Header("Baskın yerleri")]
        [Tooltip("Bölgeye göre bina sesi (bölge kimliğiyle eşlenir). Bölgenin yatağı yoksa baskında LoopCue.RaidAmbience çalar.")]
        public RaidSite[] sites = Array.Empty<RaidSite>();

        [Header("Karışım")]
        [Range(0, 1)] public float musicVolume = 0.55f;
        [Range(0, 1)] public float sfxVolume = 0.8f;
        [Range(0, 1)] public float gritVolume = 0.35f;
        [Tooltip("Baskın sahnesi açıkken müziğin çarpanı.")]
        [Range(0, 1)] public float raidDuck = 0.3f;
        [Tooltip("Müziğe uygulanan alçak geçiren filtre (Hz): sığınağın duvarlarının ardından duyulur.")]
        public float pausedCutoff = 900f;

        [Header("Telsiz")]
        [Tooltip("Baskında saha sesleri (adım, atış, patlama, bölge olayları, binanın sesi) telsizin dar bandından duyulur.")]
        public bool fieldThroughRadio = true;
        [Tooltip("Telsiz bandının alt sınırı (Hz).")] public float radioLowCut = 450f;
        [Tooltip("Telsiz bandının üst sınırı (Hz).")] public float radioHighCut = 3000f;

        public CueClip Cue(Cue cue)
        {
            foreach (var c in cues) if (c.cue == cue) return c;
            return default;
        }

        public LoopClip Loop(LoopCue loop)
        {
            foreach (var c in loops) if (c.loop == loop) return c;
            return default;
        }

        /// <summary>Bölgenin baskın yeri; kaydı yoksa boş (yatak null).</summary>
        public RaidSite Site(string region)
        {
            if (string.IsNullOrEmpty(region) || sites == null) return default;
            foreach (var s in sites) if (s.region == region) return s;
            return default;
        }
    }

    [Serializable]
    public struct CueClip
    {
        public Cue cue;
        public AudioClip clip;
        [Range(0, 1)] public float volume;
        [Tooltip("Perde rastgele bu kadar oynar (tekrar eden seslerde makine tekdüzeliğini kırar).")]
        [Range(0, 0.2f)] public float pitchJitter;
    }

    [Serializable]
    public struct LoopClip
    {
        public LoopCue loop;
        public AudioClip clip;
        [Range(0, 1)] public float volume;
    }

    /// <summary>
    /// Bir bölgenin baskın yeri (Docs/BASKIN-MEKANLARI.md Katman 7): binanın sürekli ortam yatağı ve seyrek ayrıntıları.
    /// Yatak ve ayrıntılar telsiz bandından geçer; bant sonrası seviye bugünkü uğultuya eşit (RMS ≈0,078, aynı ses düzeyiyle).
    /// </summary>
    [Serializable]
    public struct RaidSite
    {
        [Tooltip("Bölge kimliği (GameData.Regions: weu, eeu…).")] public string region;
        [Tooltip("Sürekli çalan döngü (20–24 sn, dikişsiz).")] public AudioClip bed;
        [Range(0, 1)] public float bedVolume;
        [Tooltip("Seyrek ayrıntılar: aralarından rastgele biri çalar (boşsa yalnız yatak).")] public AudioClip[] details;
        [Range(0, 1)] public float detailVolume;
        [Tooltip("İki ayrıntı arasındaki süre (sn): en az, en çok.")] public Vector2 detailGap;
    }
}
