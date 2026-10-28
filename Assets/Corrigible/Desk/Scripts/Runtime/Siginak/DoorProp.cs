using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sığınağın çelik kapısı (sağ duvar): biri kapıyı çalınca üstündeki lamba yanıp söner; açılırken önce çark döner, sonra
    /// kanat menteşesinde odaya doğru açılır ve koridorun ışığında ziyaretçinin silueti görünür; gözetleme deliğinin kapağı
    /// kayar. Yalnız çizer: ne zaman ne olacağını kapının bağlayıcısı söyler. Çalışırken hiyerarşi yaratmaz.
    /// </summary>
    public sealed class DoorProp : MonoBehaviour
    {
        [Tooltip("Kanadın menteşe pivotu (yerel y ekseninde döner).")]
        [SerializeField] Transform leaf;
        [SerializeField] float openAngle = -80;
        [Tooltip("Kanattaki çark (yerel z ekseninde döner).")]
        [SerializeField] Transform wheel;
        [SerializeField] float wheelTurns = 1.25f;
        [SerializeField] Renderer lamp;
        [SerializeField] Material lampOn, lampOff;
        [SerializeField] Light lampLight;
        [SerializeField] float lampIntensity = 1.2f;
        [Tooltip("Gözetleme deliğinin kapağı; açıkken yerel konumuna eklenen kayma.")]
        [SerializeField] Transform peepCover;
        [SerializeField] Vector3 peepOffset = new Vector3(0, 0.045f, 0);
        [Tooltip("Koridorun ışığı (kapı açılınca yanar).")]
        [SerializeField] Light corridorLight;
        [SerializeField] float corridorIntensity = 1.6f;
        [Tooltip("Ziyaretçinin silueti (koridorda).")]
        [SerializeField] GameObject silhouette;
        [SerializeField, Min(0.1f)] float openSeconds = 1.2f;
        [SerializeField, Min(0.1f)] float blinkHz = 1.6f;

        Quaternion leafRest, wheelRest;
        Vector3 peepRest;
        float open, openTarget, peep, peepTarget;
        bool knocking, rested, lit;

        /// <summary>Kanat açılıyor ya da kapanıyor.</summary>
        public bool Moving => !Mathf.Approximately(open, openTarget);
        public bool IsOpen => open >= 1;
        public Transform Leaf => leaf;

        public void Configure(Transform leafPivot, Transform wheelPart, Renderer lampRenderer, Material on, Material off, Light lampGlow,
            Transform peepCoverPart, Light corridor, GameObject figure)
        {
            leaf = leafPivot; wheel = wheelPart; lamp = lampRenderer; lampOn = on; lampOff = off; lampLight = lampGlow;
            peepCover = peepCoverPart; corridorLight = corridor; silhouette = figure;
        }

        void Awake() => Rest();

        void Rest()
        {
            if (rested) return;
            rested = true;
            if (leaf != null) leafRest = leaf.localRotation;
            if (wheel != null) wheelRest = wheel.localRotation;
            if (peepCover != null) peepRest = peepCover.localPosition;
        }

        void OnEnable() { Rest(); Apply(); Lamp(false); }

        /// <summary>Biri kapıyı çalıyor: lamba yanıp söner.</summary>
        public void SetKnocking(bool on)
        {
            knocking = on;
            if (!on) Lamp(false);
        }

        /// <summary>Kapıyı açar ya da kapatır; <paramref name="figure"/> koridorda biri duruyor mu (zarf ya da paket bırakılmışsa yok).</summary>
        public void SetOpen(bool on, bool figure = true)
        {
            Rest();
            openTarget = on ? 1 : 0;
            if (on && silhouette != null) silhouette.SetActive(figure);
        }

        /// <summary>Gözetleme deliğinin kapağı.</summary>
        public void SetPeek(bool on) => peepTarget = on ? 1 : 0;

        /// <summary>Animasyonsuz kapalı hâle döner (yeni oyun, kayıttan devam).</summary>
        public void ResetClosed()
        {
            Rest();
            open = openTarget = peep = peepTarget = 0;
            knocking = false;
            Apply();
            Lamp(false);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (knocking) Lamp(Mathf.Repeat(Time.unscaledTime * blinkHz, 1) < 0.5f);
            if (Mathf.Approximately(open, openTarget) && Mathf.Approximately(peep, peepTarget)) return;
            open = Mathf.MoveTowards(open, openTarget, dt / openSeconds);
            peep = Mathf.MoveTowards(peep, peepTarget, dt / 0.25f);
            Apply();
        }

        void Apply()
        {
            // Önce çark döner (sürgüler çekilir), sonra kanat açılır; kapanırken tersi
            float spin = Mathf.Clamp01(open / 0.4f), swing = Ease(Mathf.Clamp01((open - 0.35f) / 0.65f));
            if (wheel != null) wheel.localRotation = wheelRest * Quaternion.Euler(0, 0, spin * 360 * wheelTurns);
            if (leaf != null) leaf.localRotation = leafRest * Quaternion.Euler(0, openAngle * swing, 0);
            if (corridorLight != null)
            {
                corridorLight.enabled = swing > 0.01f;
                corridorLight.intensity = corridorIntensity * swing;
            }
            if (silhouette != null && open <= 0) silhouette.SetActive(false);
            if (peepCover != null) peepCover.localPosition = peepRest + peepOffset * peep;
        }

        void Lamp(bool on)
        {
            if (lit == on && lamp != null && lamp.sharedMaterial == (on ? lampOn : lampOff)) return;
            lit = on;
            if (lamp != null && lampOn != null && lampOff != null) lamp.sharedMaterial = on ? lampOn : lampOff;
            if (lampLight != null)
            {
                lampLight.enabled = on;
                lampLight.intensity = lampIntensity;
            }
        }

        static float Ease(float t) => t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
    }
}
