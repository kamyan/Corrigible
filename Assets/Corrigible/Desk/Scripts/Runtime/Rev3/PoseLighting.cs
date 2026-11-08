using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk
{
    /// <summary>
    /// Poz ışığı (Revizyon 3 · R14c): kamera pozu değişince <see cref="PoseLightRecipe"/>'deki ışıkları kameranın geçiş süresi
    /// boyunca yumuşakça ayarlar. Yalnız ışığın açık/kapalı hâlini, gölgesini ve taban şiddetini yazar: ampul ve dolgusu
    /// <see cref="BulbFlicker"/> üzerinden (kırpışma ve vardiya karartması üstüne çarpar), diğerleri doğrudan. Gölge geçişin
    /// başında açılır, sonunda kapanır (yolda gölge sıçraması yok); yeni pozda sönecek ışık geçiş bitince kapanır. Şiddeti iki pozda
    /// aynı kalan ışığa yazılmaz (başka sahibi, ör. vardiya dizisinin masa lambası karartması, ezilmesin). Ortam ışığı: sahnenin üç
    /// renkli ortamı bir kez saklanır, poz çarpanı ile <see cref="ShiftSequence.AmbientScale"/>'in küçüğüyle yazılır (ikisi de K6a
    /// karartmasını taşır, bir kez sayılır; yalnız bu sahne etkinken, baskın eklemeli yüklenince onun ayarına dokunulmaz).
    /// </summary>
    public sealed class PoseLighting : MonoBehaviour
    {
        [Serializable]
        public struct Binding
        {
            public string id;
            public Light light;
        }

        [SerializeField] SiginakCameraRig rig;
        [SerializeField] PoseLightRecipe recipe;
        [SerializeField] Binding[] bindings = Array.Empty<Binding>();
        [Tooltip("Ampul spotunun ve dolgusunun taban şiddeti bu bileşen üzerinden yazılır.")]
        [SerializeField] BulbFlicker bulb;
        [SerializeField] string bulbId = "bulbSpot", fillId = "bulbFill";
        [Tooltip("Kamerayı izleyen gölgesiz ortam dolgusu: three.js referansının her malzemedeki ortam haritasının (envMap 0,22) " +
                 "karşılığı; kameraya bakan yüzeyler (teleks kâğıdı, plakalar) onunla okunur. Şiddeti tarifte (envFill). Odanın içinde " +
                 "durur, oda gizlenince (baskın) söner.")]
        [SerializeField] Light cameraFill;
        [SerializeField] Vector3 cameraFillOffset = new Vector3(0, 0.05f, -0.1f);

        float[] from = Array.Empty<float>(), to = Array.Empty<float>();
        bool[] targetOn = Array.Empty<bool>(), targetShadow = Array.Empty<bool>();
        float ambientFrom = 1, ambientTo = 1, ambient = 1, written = -1;
        float blendStart, blendSeconds;
        bool blending, started, saved;
        Color sky, equator, ground;

        public PoseLightRecipe Recipe => recipe;
        public int BindingCount => bindings.Length;
        public Binding BindingAt(int i) => bindings[i];

        void OnEnable()
        {
            if (rig != null) rig.PoseChanged += OnPose;
            if (started) Apply(rig != null ? rig.Current : SiginakPose.Desk, 0);
        }

        void OnDisable()
        {
            if (rig != null) rig.PoseChanged -= OnPose;
        }

        // Start: BulbFlicker'ın OnEnable'da aldığı tabanın üstüne yazılır
        void Start()
        {
            started = true;
            Apply(rig != null ? rig.Current : SiginakPose.Desk, 0);
        }

        void OnPose(SiginakPose pose) => Apply(pose, rig != null ? rig.TransitionRemaining : 0);

        void Apply(SiginakPose pose, float seconds)
        {
            if (recipe == null || !recipe.TryGet(pose, out var entry)) return;
            int n = bindings.Length;
            if (from.Length != n) { from = new float[n]; to = new float[n]; targetOn = new bool[n]; targetShadow = new bool[n]; }
            for (int i = 0; i < n; i++)
            {
                var l = bindings[i].light;
                if (l == null) continue;
                from[i] = l.enabled ? GetBase(i) : 0;
                to[i] = from[i]; targetOn[i] = l.enabled; targetShadow[i] = l.shadows != LightShadows.None;
                if (entry.lights == null) continue;
                foreach (var s in entry.lights)
                {
                    if (s.id != bindings[i].id) continue;
                    to[i] = s.on ? s.intensity : 0;
                    targetOn[i] = s.on;
                    targetShadow[i] = s.shadows;
                }
                // Yanacak ışık şimdi açılır, gölgesi gerekiyorsa şimdi açılır; kapanacaklar geçiş sonunda
                if (targetOn[i] && !l.enabled) { SetBase(i, 0); l.enabled = true; }
                if (targetShadow[i]) l.shadows = LightShadows.Soft;
            }
            ambientFrom = ambient;
            ambientTo = entry.ambientScale;
            blendStart = Time.unscaledTime;
            blendSeconds = seconds;
            blending = true;
            Step();
        }

        void Update()
        {
            if (blending) Step();
            WriteAmbient();
        }

        void LateUpdate()
        {
            if (cameraFill == null || rig == null) return;
            var t = rig.transform;
            cameraFill.transform.SetPositionAndRotation(t.TransformPoint(cameraFillOffset), t.rotation);
        }

        void Step()
        {
            float t = blendSeconds <= 0 ? 1 : Mathf.Clamp01((Time.unscaledTime - blendStart) / blendSeconds);
            float k = t * t * (3 - 2 * t);
            for (int i = 0; i < bindings.Length; i++)
            {
                var l = bindings[i].light;
                if (l == null || i >= from.Length) continue;
                if (!Mathf.Approximately(from[i], to[i])) SetBase(i, Mathf.Lerp(from[i], to[i], k));
                if (t < 1) continue;
                if (!targetShadow[i]) l.shadows = LightShadows.None;
                if (!targetOn[i]) l.enabled = false;
            }
            ambient = Mathf.Lerp(ambientFrom, ambientTo, k);
            if (t >= 1) blending = false;
            WriteAmbient();
        }

        float GetBase(int i)
        {
            string id = bindings[i].id;
            if (bulb != null && id == bulbId) return bulb.BaseIntensity;
            if (bulb != null && id == fillId) return bulb.FillBase(0);
            return bindings[i].light.intensity;
        }

        void SetBase(int i, float v)
        {
            string id = bindings[i].id;
            if (bulb != null && bulb.isActiveAndEnabled && id == bulbId) { bulb.BaseIntensity = v; return; }
            if (bulb != null && bulb.isActiveAndEnabled && id == fillId) { bulb.SetFillBase(0, v); return; }
            bindings[i].light.intensity = v;
        }

        void WriteAmbient()
        {
            if (gameObject.scene != SceneManager.GetActiveScene() || RenderSettings.ambientMode != UnityEngine.Rendering.AmbientMode.Trilight) return;
            if (!saved)
            {
                sky = RenderSettings.ambientSkyColor; equator = RenderSettings.ambientEquatorColor; ground = RenderSettings.ambientGroundColor;
                saved = true;
            }
            float k = Mathf.Min(ambient, ShiftSequence.AmbientScale);
            if (Mathf.Approximately(k, written)) return;
            written = k;
            RenderSettings.ambientSkyColor = sky * k;
            RenderSettings.ambientEquatorColor = equator * k;
            RenderSettings.ambientGroundColor = ground * k;
        }

        public void Configure(SiginakCameraRig cameraRig, PoseLightRecipe lights, Binding[] links, BulbFlicker flicker, Light fill = null)
        {
            rig = cameraRig; recipe = lights; bindings = links ?? Array.Empty<Binding>(); bulb = flicker; cameraFill = fill;
        }
    }
}
