using System;
using Corrigible.Game.Raid;
using UnityEngine;
using UnityEngine.Rendering;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Eski komuta odası ile yeni sığınak odası arasında geçiş (Docs/UI_Siginak PROMPT §2 “Güvenli geçiş”).
    /// Açılışta <see cref="SiginakSettings.newPresentation"/>'ı uygular; editörde menüden de çevrilebilir.
    /// Yeni görünümde: eski oda nesneleri kapanır, tek dünya haritası yeni masaya taşınır, kamera K1 fiziksel pozuna
    /// geçer, ortam ışığı üç renkli olur. Eski görünüme dönüşte hepsi kurulumda saklanan değerlere döner.
    /// Bağlantıların hepsi sahnede saklanır (oda kurucusu yazar).
    /// </summary>
    public sealed class SiginakPresentationSwitch : MonoBehaviour
    {
        [Serializable] public struct CameraState
        {
            public Vector3 position; public Quaternion rotation; public float fieldOfView, nearClip; public bool physical;
        }

        [Serializable] public struct AmbientState
        {
            public AmbientMode mode; public Color sky, equator, ground; public float intensity, reflection;
        }

        [SerializeField] SiginakSettings settings;
        [SerializeField] GameObject siginakRoot;
        [Tooltip("Yeni görünümde kapanan eski oda nesneleri (CommandPost3D altındaki fiziksel düzen).")]
        [SerializeField] GameObject[] legacyObjects = Array.Empty<GameObject>();
        [Tooltip("Sığınak kökünün dışında olup yalnız yeni görünümde açılan nesneler (harita canvas'ındaki kurtuluş katmanları).")]
        [SerializeField] GameObject[] siginakObjects = Array.Empty<GameObject>();

        [Header("Dünya haritası")]
        [SerializeField] RectTransform mapCanvas;
        [SerializeField] Transform legacyMapParent;
        [SerializeField] Vector3 legacyMapPosition;
        [SerializeField] Quaternion legacyMapRotation = Quaternion.identity;
        [SerializeField] Vector3 legacyMapScale = Vector3.one;
        [SerializeField] Transform siginakMapMount;

        [Header("Kamera")]
        [SerializeField] Camera mainCamera;
        [Tooltip("Eski kamera denetimi; yeni görünümde kapanır.")]
        [SerializeField] Behaviour legacyCameraRig;
        [Tooltip("Sığınak kamerası; yalnız yeni görünümde etkin.")]
        [SerializeField] SiginakCameraRig cameraRig;
        [SerializeField] CameraState legacyCamera;

        [Header("Baskın geçişi (monitöre yaklaşma)")]
        [SerializeField] RaidLauncher launcher;
        [SerializeField] Transform crtDivePose;
        [SerializeField] float crtDiveFieldOfView = 40;
        [SerializeField] Transform legacyMonitorPose;
        [SerializeField] float legacyMonitorFieldOfView = 35;
        [Tooltip("Yeni görünümün baskın geçişi (CRT dizisi; IRaidTransition). Eski görünümde başlatıcının kancası boştur.")]
        [SerializeField] MonoBehaviour raidTransition;

        [Header("Girdi")]
        [Tooltip("Odadaki tıklanır nesneler için; eski görünümde kapalı (eski odanın çarpıştırıcıları dünya canvas'larını örtmesin).")]
        [SerializeField] Behaviour physicsRaycaster;

        [Header("Ortam ışığı")]
        [SerializeField] AmbientState legacyAmbient;

        public bool IsNew => siginakRoot != null && siginakRoot.activeSelf;
        public Camera MainCamera => mainCamera;
        public SiginakCameraRig CameraRig => cameraRig;
        public GameObject Root => siginakRoot;

        void Awake()
        {
            if (settings != null) Apply(settings.newPresentation);
        }

        // Baskın sahnesi ek yüklüyken odanın ışıkları taktik tahtayı da aydınlatmasın (Aşama 7'de CrtTransition devralır).
        void Update()
        {
            if (!IsNewRequested || siginakRoot == null) return;
            bool show = !RaidLauncher.IsRaidLoaded;
            if (siginakRoot.activeSelf != show) siginakRoot.SetActive(show);
        }

        bool IsNewRequested => requested;
        bool requested;

        /// <summary>Görünümü uygular. Editörde sahne nesnelerini değiştirir (kaydedilirse kalıcı olur).</summary>
        public void Apply(bool on)
        {
            requested = on;
            if (on)
            {
                MoveMap(siginakMapMount, Vector3.up * 0.0004f, Quaternion.Euler(90, 0, 0), Vector3.one * MapScale);
                foreach (var go in legacyObjects) if (go != null) go.SetActive(false);
                if (siginakRoot != null) siginakRoot.SetActive(true);
                foreach (var go in siginakObjects) if (go != null) go.SetActive(true);
                if (legacyCameraRig != null) legacyCameraRig.enabled = false;
                if (cameraRig != null) { cameraRig.enabled = true; cameraRig.Snap(SiginakPose.Desk); }
                if (physicsRaycaster != null) physicsRaycaster.enabled = true;
                if (launcher != null && crtDivePose != null) launcher.SetMonitorTransition(crtDivePose, crtDiveFieldOfView);
                // K2: “EMİR VERİLDİ” damgası iner, 0,3 s sonra kamera CRT'ye döner
                if (launcher != null)
                {
                    launcher.TransitionDelaySeconds = SiginakTheme.Active.NumberOf("motion.raidOrderDelayS", 0.3f);
                    launcher.Transition = raidTransition as IRaidTransition;
                }
                ApplyAmbient();
            }
            else
            {
                foreach (var go in legacyObjects) if (go != null) go.SetActive(true);
                MoveMap(legacyMapParent, legacyMapPosition, legacyMapRotation, legacyMapScale);
                if (siginakRoot != null) siginakRoot.SetActive(false);
                foreach (var go in siginakObjects) if (go != null) go.SetActive(false);
                if (launcher != null) { launcher.TransitionDelaySeconds = 0; launcher.Transition = null; }
                if (cameraRig != null) cameraRig.enabled = false;
                if (physicsRaycaster != null) physicsRaycaster.enabled = false;
                if (legacyCameraRig != null) legacyCameraRig.enabled = true;
                if (launcher != null && legacyMonitorPose != null) launcher.SetMonitorTransition(legacyMonitorPose, legacyMonitorFieldOfView);
                RestoreCamera();
                RestoreAmbient();
            }
        }

        float MapScale => (settings != null ? settings.mapWidthMeters : 1.2f) / MapProjection.DesignWidth;

        void MoveMap(Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (mapCanvas == null || parent == null) return;
            mapCanvas.SetParent(parent, false);
            mapCanvas.localPosition = position;
            mapCanvas.localRotation = rotation;
            mapCanvas.localScale = scale;
        }

        void RestoreCamera()
        {
            if (mainCamera == null) return;
            mainCamera.usePhysicalProperties = legacyCamera.physical;
            mainCamera.lensShift = Vector2.zero;
            mainCamera.transform.SetPositionAndRotation(legacyCamera.position, legacyCamera.rotation);
            mainCamera.fieldOfView = legacyCamera.fieldOfView;
            mainCamera.nearClipPlane = legacyCamera.nearClip;
        }

        /// <summary>three.js yarım küre ışığı (gök → yer) → URP üç renkli ortam; ekvator iki rengin ortası.</summary>
        void ApplyAmbient()
        {
            var t = SiginakTheme.Active;
            float k = settings != null ? settings.ambientScale : 0.32f;
            Color Scale(Color c) => (c.linear * k).gamma;
            var sky = t.ColorOf("lights.ambient.sky");
            var ground = t.ColorOf("lights.ambient.ground");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Scale(sky);
            RenderSettings.ambientEquatorColor = Scale(Color.Lerp(sky, ground, 0.5f));
            RenderSettings.ambientGroundColor = Scale(ground);
            RenderSettings.reflectionIntensity = settings != null ? settings.reflectionIntensity : 0.15f;
        }

        void RestoreAmbient()
        {
            RenderSettings.ambientMode = legacyAmbient.mode;
            RenderSettings.ambientSkyColor = legacyAmbient.sky;
            RenderSettings.ambientEquatorColor = legacyAmbient.equator;
            RenderSettings.ambientGroundColor = legacyAmbient.ground;
            RenderSettings.ambientIntensity = legacyAmbient.intensity;
            RenderSettings.reflectionIntensity = legacyAmbient.reflection;
        }

        /// <summary>
        /// Editör oda kurucusu serileştirilmiş bağlantıları yazar. Eski görünüm etkinken çağrılır: haritanın eski yeri,
        /// kamera ve ortam ışığı o anki değerlerinden saklanır. Çalışma anında hiyerarşi yaratmaz.
        /// </summary>
        public void Configure(SiginakSettings settingsAsset, GameObject root, GameObject[] legacy, RectTransform map,
            Transform mapMount, Camera camera, Behaviour legacyRig, SiginakCameraRig rig, RaidLauncher raidLauncher,
            Transform crtDive, float crtDiveFov, Behaviour physics, GameObject[] siginakOnly = null, MonoBehaviour transition = null)
        {
            raidTransition = transition;
            settings = settingsAsset;
            siginakObjects = siginakOnly ?? Array.Empty<GameObject>();
            siginakRoot = root;
            legacyObjects = legacy;
            mapCanvas = map;
            if (map != null)
            {
                legacyMapParent = map.parent;
                legacyMapPosition = map.localPosition;
                legacyMapRotation = map.localRotation;
                legacyMapScale = map.localScale;
            }
            siginakMapMount = mapMount;
            mainCamera = camera;
            legacyCameraRig = legacyRig;
            cameraRig = rig;
            launcher = raidLauncher;
            crtDivePose = crtDive;
            crtDiveFieldOfView = crtDiveFov;
            physicsRaycaster = physics;
            if (raidLauncher != null)
            {
                legacyMonitorPose = raidLauncher.MonitorFocusPose;
                legacyMonitorFieldOfView = raidLauncher.MonitorFieldOfView;
            }
            if (camera != null) legacyCamera = Capture(camera);
            legacyAmbient = CaptureAmbient();
        }

        static CameraState Capture(Camera c) => new CameraState
        {
            position = c.transform.position, rotation = c.transform.rotation, fieldOfView = c.fieldOfView,
            nearClip = c.nearClipPlane, physical = c.usePhysicalProperties,
        };

        static AmbientState CaptureAmbient() => new AmbientState
        {
            mode = RenderSettings.ambientMode, sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor,
            ground = RenderSettings.ambientGroundColor, intensity = RenderSettings.ambientIntensity,
            reflection = RenderSettings.reflectionIntensity,
        };
    }
}
