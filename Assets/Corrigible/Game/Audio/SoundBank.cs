using System;
using UnityEngine;

namespace Corrigible.Game.Audio
{
    /// <summary>
    /// Oyunun sesleri ve karışım ayarları. Şimdilik "Corrigible > Ses > Yer tutucu sesleri üret" ile kodla üretilmiş
    /// klipler bağlı; gerçek seslerle değiştirmek için klipleri buradan değiştirmek yeterli (müzik katmanları aynı uzunlukta,
    /// döngüye uygun olmalı).
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Sound Bank", fileName = "SoundBank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Header("Müzik katmanları (aynı uzunlukta döngüler)")]
        [Tooltip("Her zaman çalar.")] public AudioClip drone;
        [Tooltip("Gösterilen Tekillik %25'i geçince girer.")] public AudioClip pulse;
        [Tooltip("Tekillik %50'yi geçince girer.")] public AudioClip arpeggio;
        [Tooltip("Tekillik %75'i geçince girer.")] public AudioClip tension;
        [Tooltip("Bağımlılık yükseldikçe girer: rahatlatıcı, tehlikeli huzur.")] public AudioClip warm;
        [Tooltip("Gerçek dünyanın uğultusu ve paraziti. İllüzyon sırasında susar (ses de yalan söyler).")] public AudioClip grit;

        [Header("YZ motifi")]
        [Tooltip("Rakip tipine göre tını; klip 440 Hz (A4) üzerinde, perde değiştirilerek çalınır.")]
        public MotifVoice[] voices = Array.Empty<MotifVoice>();
        [Tooltip("Motifin notaları (MIDI). Her yetenek bir nota ekler.")]
        public int[] motif = { 74, 81, 77, 76, 80, 81, 86 };

        [Header("Haber bandı")]
        [Tooltip("GameData.Agencies sırasıyla ajans jingle'ları.")] public AudioClip[] agencyJingles = Array.Empty<AudioClip>();
        [Tooltip("GameData.FakeAgencies sırasıyla: aynı jingle, bir notası yanlış.")] public AudioClip[] fakeJingles = Array.Empty<AudioClip>();
        public AudioClip teleprinter;
        [Tooltip("Alarm haberi (gerçek).")] public AudioClip alarm;
        [Tooltip("İllüzyon sırasında alarmın yerine çalan yumuşak bildirim.")] public AudioClip softChime;

        [Header("Kararlar")]
        public AudioClip killSwitch;
        [Tooltip("Küresel Son Şalter'in ilk basışı: koruma kapağı.")] public AudioClip globalArm;
        [Tooltip("Perde yırtıldı: bastırılan gerçek ses geri gelir.")] public AudioClip veilTear;
        [Tooltip("Karanlık Çağ: her şey sustuktan sonra.")] public AudioClip wind;

        [Header("Ses olayları")]
        [Tooltip("Tek seferlik ses olayları (Cue) ve döngüler (LoopCue).")]
        public CueClip[] cues = Array.Empty<CueClip>();
        public LoopClip[] loops = Array.Empty<LoopClip>();

        [Header("Karışım")]
        [Range(0, 1)] public float musicVolume = 0.55f;
        [Range(0, 1)] public float sfxVolume = 0.8f;
        [Range(0, 1)] public float gritVolume = 0.35f;
        [Tooltip("Baskın sahnesi açıkken müziğin çarpanı.")]
        [Range(0, 1)] public float raidDuck = 0.3f;
        [Tooltip("Oyun duraklatılınca müziğe uygulanan alçak geçiren filtre (Hz).")]
        public float pausedCutoff = 900f;
        [Tooltip("Ard arda iki jingle arasındaki en kısa süre (saniye); arada teleks sesi çalar.")]
        public float jingleCooldown = 2.5f;

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

        public AudioClip Voice(string typeId)
        {
            foreach (var v in voices) if (v.typeId == typeId) return v.note;
            return voices.Length > 0 ? voices[0].note : null;
        }
    }

    [Serializable]
    public struct MotifVoice
    {
        [Tooltip("GameData.AiTypes kimliği.")] public string typeId;
        public AudioClip note;
    }
}

namespace Corrigible.Game.Audio
{
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
}
