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
        float spin, swing, glow, peep, peepTarget, flashAt = -1;
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

        /// <summary>Kanat açılıyor ya da kapanıyor.</summary>
        public bool Moving => spin != openTarget || swing != openTarget;
        public bool IsOpen => swing >= 1;
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

        /// <summary>Üç vuruş (motion.doorKnockS): lamba her vuruşta motion.doorLampFlashS kadar parlar.</summary>
        public void Knock()
        {
            waiting = true;
            flashAt = Time.unscaledTime;
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
            flashAt = -1;
            Apply();
            Lamp();
            Led(false);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (flashAt >= 0)
            {
                Lamp();
                if (Time.unscaledTime - flashAt > T.NumberOf("motion.doorKnockS", 0.9f)) { flashAt = -1; Lamp(); }
            }
            bool still = spin == openTarget && swing == openTarget && Mathf.Approximately(peep, peepTarget) &&
                         Mathf.Approximately(glow, GlowTarget());
            if (still) return;
            if (openTarget == 1)
            {
                // Önce çark (sürgüler çekilir), sonra kanat
                spin = Mathf.MoveTowards(spin, 1, dt / WheelS);
                if (spin >= 1) swing = Mathf.MoveTowards(swing, 1, dt / LeafS);
            }
            else
            {
                // Kanat kapanır, sonra çark geri döner
                swing = Mathf.MoveTowards(swing, 0, dt / CloseS);
                if (swing <= 0) spin = Mathf.MoveTowards(spin, 0, dt / (WheelS * 0.6f));
            }
            glow = Mathf.MoveTowards(glow, GlowTarget(), dt / StairS);
            peep = Mathf.MoveTowards(peep, peepTarget, dt / PeepS);
            Apply();
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
            bool flash = !fatal && flashAt >= 0 && Flashing(Time.unscaledTime - flashAt);
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
