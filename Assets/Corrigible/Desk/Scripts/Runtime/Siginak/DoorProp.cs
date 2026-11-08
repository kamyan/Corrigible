using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sığınağın çelik kapısı (sağ duvar, Revizyon 2 · S4). Biri kapıyı çalınca tehlike bandının üstündeki kafesli vuruş
    /// lambası amber yanar, her vuruşta bir an parlar; harekâtı bitiren kapıda kırmızıdır. Açılırken önce el çarkı döner
    /// (0,5 s), sonra kanat menteşesinde koridora doğru açılır (0,8 s); merdiven boşluğunun ışıkları 0,3 s'de yanar ve ziyaretçinin
    /// silueti görünür. Kapanırken kanat 0,6 s'de kapanır, sonra çark geri döner. Gözetleme deliğinin kapağı 0,15 s'de kayar;
    /// interkomdan cevap gelirken amber noktası yanar. Yalnız çizer: ne zaman ne olacağını kapının bağlayıcısı söyler.
    /// Çalışırken hiyerarşi yaratmaz.
    /// <para>Revizyon 3 · Aşama 6 (R9): bütün zaman <see cref="Advance"/> üzerinden akar (<c>Update</c> yalnız onu çağırır; vuruşun
    /// parlamaları da iç saatle, <c>Time.unscaledTime</c> okunmaz), böylece Edit modda süreler ölçülür. Evreler artan süreyi bir
    /// sonrakine devreder (çark bitince kalan zaman kanadı sürer); uçlar tam değere oturur (±1e-6 s). Kabul dizisi kanadı
    /// <c>admission.motion.admitWheelS/admitLeafS</c> süreleriyle açar (<see cref="SetOpen(bool, IReadOnlyList{string}, float, bool, float, float)"/>);
    /// kartlı oyunda koridor boştur (siluet yok) ve kırmızı lamba hiç yanmaz.</para>
    /// </summary>
    public sealed class DoorProp : MonoBehaviour
    {
        [Tooltip("Kanadın menteşe pivotu (yerel y ekseninde döner); açı tokens › layout3d.door.leafOpenDeg (koridora).")]
        [SerializeField] Transform leaf;
        [SerializeField] float openAngle = 95;
        [Tooltip("Kanattaki çark (yerel z ekseninde döner).")]
        [SerializeField] Transform wheel;
        [SerializeField] float wheelTurns = 1.25f;
        [Tooltip("Kafesli vuruş lambasının camı: kapalı, amber (biri çalıyor), kırmızı (ölümcül).")]
        [SerializeField] Renderer lamp;
        [SerializeField] Material lampOn, lampOff, lampFatal;
        [SerializeField] Light lampLight;
        [SerializeField] float lampIntensity = 1.2f;
        [Tooltip("Vuruşta ışığın parlama çarpanı ve ölümcülde ışığın çarpanı (three.js referansı: 1,2 → 2,2).")]
        [SerializeField] float flashBoost = 2.2f, fatalBoost = 1.8f;
        [Tooltip("Gözetleme deliğinin kapağı; açıkken yerel konumuna eklenen kayma.")]
        [SerializeField] Transform peepCover;
        [SerializeField] Vector3 peepOffset = new Vector3(0, 0.05f, 0);
        [Tooltip("İnterkomun amber noktası (cevap gelirken yanar).")]
        [SerializeField] Renderer intercomLed;
        [SerializeField] Material ledOn, ledOff;
        [Tooltip("Merdiven boşluğunun ışıkları (kapı açılınca yanar; sahnedeki şiddetleri tam değer).")]
        [SerializeField] Light[] stairLights = System.Array.Empty<Light>();
        [Tooltip("Ziyaretçinin silueti (merdiven boşluğunda).")]
        [SerializeField] VisitorSilhouette silhouette;

        Quaternion leafRest, wheelRest;
        Vector3 peepRest;
        float[] stairBase = System.Array.Empty<float>();
        float spin, swing, glow, peep, peepTarget;
        /// <summary>Vuruşun başından beri geçen süre (iç saat, <see cref="Advance"/> sürer); −1 = vuruş yok.</summary>
        float knockT = -1;
        /// <summary>Süreli açılışın çark ve kanat süreleri (≤ 0 = token <c>motion.doorWheelS/doorLeafS</c>).</summary>
        float wheelOverride = -1, leafOverride = -1;
        int openTarget;
        bool waiting, fatal, rested, led, ledSet;
        string[] kinds = System.Array.Empty<string>();
        float height = 1;
        bool mirror;

        static SiginakTheme T => SiginakTheme.Active;
        static float WheelS => Mathf.Max(0.05f, T.NumberOf("motion.doorWheelS", 0.5f));
        static float LeafS => Mathf.Max(0.05f, T.NumberOf("motion.doorLeafS", 0.8f));
        static float StairS => Mathf.Max(0.05f, T.NumberOf("motion.stairLightS", 0.3f));
        const float CloseS = 0.6f, PeepS = 0.15f;
        /// <summary>Evrenin bitişini yakalama payı (s): ardışık küçük adımların float toplamı ucu kaçırmasın.</summary>
        const float EndSlack = 1e-6f;

        float WheelDur => wheelOverride > 0 ? Mathf.Max(0.05f, wheelOverride) : WheelS;
        float LeafDur => leafOverride > 0 ? Mathf.Max(0.05f, leafOverride) : LeafS;

        /// <summary>Kanat açılıyor ya da kapanıyor.</summary>
        public bool Moving => spin != openTarget || swing != openTarget;
        public bool IsOpen => swing >= 1;
        /// <summary>Kapıda biri bekliyor (vuruş lambası amber; <see cref="SetKnocking"/> ya da <see cref="Knock"/>).</summary>
        public bool Knocking => waiting;
        /// <summary>Lamba şu an bir vuruşun parlamasında (motion.doorLampFlashS; kırmızıda yok).</summary>
        public bool LampFlashing => !fatal && knockT >= 0 && Flashing(knockT);
        /// <summary>Üç vuruş sürüyor (<see cref="Knock"/>'tan sonra motion.doorKnockS boyunca).</summary>
        public bool KnockPlaying => knockT >= 0;
        /// <summary>Çarkın dönüşü (derece, kolaylaştırılmış; açıkken 360 × wheelTurns = 450).</summary>
        public float WheelDegrees => Ease(spin) * 360 * wheelTurns;
        /// <summary>Kanadın açısı (derece, kolaylaştırılmış; açıkken openAngle = 95).</summary>
        public float LeafDegrees => openAngle * Ease(swing);
        /// <summary>Merdiven ışıklarının oranı (0 sönük … 1 sahnedeki şiddet).</summary>
        public float StairGlow => glow;
        public Transform Leaf => leaf;
        public VisitorSilhouette Silhouette => silhouette;

        void Awake() => Rest();

        void Rest()
        {
            if (rested) return;
            rested = true;
            if (leaf != null) leafRest = leaf.localRotation;
            if (wheel != null) wheelRest = wheel.localRotation;
            if (peepCover != null) peepRest = peepCover.localPosition;
            stairBase = new float[stairLights.Length];
            for (int i = 0; i < stairLights.Length; i++) stairBase[i] = stairLights[i] != null ? stairLights[i].intensity : 0;
        }

        void OnEnable() { Rest(); Apply(); Lamp(); Led(false); }

        /// <summary>Kapıda biri bekliyor: lamba amber yanar (vuruşlar <see cref="Knock"/>).</summary>
        public void SetKnocking(bool on)
        {
            if (waiting == on) return;
            waiting = on;
            Lamp();
        }

        /// <summary>
        /// Üç vuruş (motion.doorKnockS 0,9 s): lamba amber yanar ve 0 / 0,3 / 0,6 s'de motion.doorLampFlashS (0,08 s) kadar parlar,
        /// sonra amber kalır. Saat <see cref="Advance"/> ile ilerler.
        /// </summary>
        public void Knock()
        {
            waiting = true;
            knockT = 0;
            Lamp();
        }

        /// <summary>Harekâtı bitiren kapı: lamba kırmızı ve sürekli (kâğıt kapanana kadar).</summary>
        public void SetFatal(bool on)
        {
            if (fatal == on) return;
            fatal = on;
            Lamp();
        }

        /// <summary>
        /// Kapıyı açar ya da kapatır. <paramref name="visitors"/> koridorda duranlar (boşsa kimse yok: zarf, paket);
        /// <paramref name="scale"/> boy çarpanı, <paramref name="flip"/> aksesuarın tarafı.
        /// </summary>
        public void SetOpen(bool on, IReadOnlyList<string> visitors = null, float scale = 1, bool flip = false)
        {
            // Eski imza bugünkü süreleri kullanır (token); kapanış, açan çağrının sürelerini korur
            if (on) wheelOverride = leafOverride = -1;
            Open(on, visitors, scale, flip);
        }

        /// <summary>
        /// Süreleri veren açılış (Aşama 6 kabul dizisi: <c>admission.motion.admitWheelS/admitLeafS</c>, bugün 0,5 / 0,8 s): önce çark
        /// <paramref name="wheelS"/>, sonra kanat <paramref name="leafS"/>; merdiven ışığı kanat kımıldayınca motion.stairLightS'de yanar.
        /// Kapanış kanatta 0,6 s, çarkta <paramref name="wheelS"/> × 0,6. <paramref name="visitors"/> boşsa koridor boş (siluet yok).
        /// </summary>
        public void SetOpen(bool on, IReadOnlyList<string> visitors, float scale, bool flip, float wheelS, float leafS)
        {
            wheelOverride = wheelS;
            leafOverride = leafS;
            Open(on, visitors, scale, flip);
        }

        void Open(bool on, IReadOnlyList<string> visitors, float scale, bool flip)
        {
            Rest();
            openTarget = on ? 1 : 0;
            if (!on) return;
            kinds = visitors == null ? System.Array.Empty<string>() : new List<string>(visitors).ToArray();
            height = scale;
            mirror = flip;
            if (swing > 0 && silhouette != null) Figures();
        }

        /// <summary>Gözetleme deliğinin kapağı.</summary>
        public void SetPeek(bool on) => peepTarget = on ? 1 : 0;

        /// <summary>İnterkomun amber noktası.</summary>
        public void SetIntercom(bool on) => Led(on);

        /// <summary>Animasyonsuz kapalı hâle döner (yeni oyun, kayıttan devam).</summary>
        public void ResetClosed()
        {
            Rest();
            spin = swing = glow = peep = peepTarget = 0;
            openTarget = 0;
            waiting = fatal = false;
            knockT = -1;
            wheelOverride = leafOverride = -1;
            Apply();
            Lamp();
            Led(false);
        }

        void Update() => Advance(Time.unscaledDeltaTime);

        /// <summary>
        /// Vuruş lambasını, kanadı, çarkı, merdiven ışığını ve gözetleme kapağını <paramref name="dt"/> kadar ilerletir (Edit modda
        /// testler sürer). Uçlar tam değere oturur; karşılaştırmalar tam (yaklaşık eşitlik son adımı yutmasın).
        /// </summary>
        public void Advance(float dt)
        {
            if (dt < 0) dt = 0;
            if (knockT >= 0)
            {
                knockT += dt;
                Lamp();
                if (knockT > T.NumberOf("motion.doorKnockS", 0.9f)) { knockT = -1; Lamp(); }
            }
            bool still = spin == openTarget && swing == openTarget && peep == peepTarget && glow == GlowTarget();
            if (still) return;
            if (openTarget == 1)
            {
                // Önce çark (sürgüler çekilir), sonra kanat; çarktan artan süre kanadı ve merdiven ışığını sürer
                float rest = Toward(ref spin, 1, dt, WheelDur);
                if (spin >= 1)
                {
                    Toward(ref swing, 1, rest, LeafDur);
                    // Merdiven ışığı kanat kımıldayınca yanar (motion.stairLightS)
                    if (swing > 0) Toward(ref glow, 1, rest, StairS);
                }
            }
            else
            {
                // Kanat kapanır (ışık onunla söner), sonra çark geri döner
                float rest = Toward(ref swing, 0, dt, CloseS);
                glow = Mathf.MoveTowards(glow, Mathf.Min(glow, swing), dt / StairS);
                if (swing <= 0) Toward(ref spin, 0, rest, WheelDur * 0.6f);
            }
            Toward(ref peep, peepTarget, dt, PeepS);
            Apply();
        }

        /// <summary>
        /// <paramref name="value"/>'yu <paramref name="target"/>'a <paramref name="seconds"/>'lık tam yol hızıyla yaklaştırır; varırsa
        /// tam değere oturur ve artan süreyi döndürür (bir sonraki evre için), varmazsa 0.
        /// </summary>
        static float Toward(ref float value, float target, float dt, float seconds)
        {
            if (value == target) return dt;
            float need = Mathf.Abs(target - value) * seconds;
            if (dt >= need - EndSlack)
            {
                value = target;
                return Mathf.Max(0, dt - need);
            }
            value = Mathf.MoveTowards(value, target, dt / seconds);
            return 0;
        }

        /// <summary>Merdiven ışığı kanat kımıldayınca yanar (0,3 s); kapanırken kanatla söner.</summary>
        float GlowTarget() => openTarget == 1 ? (swing > 0 ? 1 : 0) : Mathf.Min(glow, swing);

        void Apply()
        {
            if (wheel != null) wheel.localRotation = wheelRest * Quaternion.Euler(0, 0, Ease(spin) * 360 * wheelTurns);
            if (leaf != null) leaf.localRotation = leafRest * Quaternion.Euler(0, openAngle * Ease(swing), 0);
            for (int i = 0; i < stairLights.Length; i++)
            {
                var l = stairLights[i];
                if (l == null) continue;
                l.enabled = glow > 0.001f;
                l.intensity = stairBase[i] * glow;
            }
            // Siluet yalnız kanat aralıkken (kapı kapalıyken yok; ipucu gözetleme deliğinin metninde)
            if (silhouette != null)
            {
                bool show = swing > 0 && kinds.Length > 0;
                if (show && !silhouette.Visible) Figures();
                else if (!show && silhouette.Visible) silhouette.Hide();
            }
            if (peepCover != null) peepCover.localPosition = peepRest + peepOffset * Ease(peep);
        }

        void Figures() => silhouette.Show(kinds, height, mirror);

        void Lamp()
        {
            bool on = waiting || fatal;
            bool flash = LampFlashing;
            var mat = fatal ? lampFatal : on || flash ? lampOn : lampOff;
            if (lamp != null && mat != null && lamp.sharedMaterial != mat) lamp.sharedMaterial = mat;
            if (lampLight == null) return;
            bool lit = on || flash;
            if (lampLight.enabled != lit) lampLight.enabled = lit;
            if (!lit) return;
            lampLight.color = T.ColorOf(fatal ? "door.knockLampFatal" : "door.knockLamp");
            lampLight.intensity = lampIntensity * (fatal ? fatalBoost : flash ? flashBoost : 1);
        }

        /// <summary>Üç eşit aralıklı vuruşun her birinde lamba motion.doorLampFlashS kadar parlar.</summary>
        static bool Flashing(float t)
        {
            float knock = T.NumberOf("motion.doorKnockS", 0.9f) / 3, flash = T.NumberOf("motion.doorLampFlashS", 0.08f);
            return t >= 0 && t < knock * 3 && t % knock < flash;
        }

        void Led(bool on)
        {
            if (ledSet && led == on) return;
            ledSet = true;
            led = on;
            if (intercomLed != null && ledOn != null && ledOff != null) intercomLed.sharedMaterial = on ? ledOn : ledOff;
        }

        static float Ease(float t) => t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
    }
}
