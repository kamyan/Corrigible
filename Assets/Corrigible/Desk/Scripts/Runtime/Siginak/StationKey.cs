using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Anahtar rayındaki tek tuş (K1 notu › anahtar): normal · üzerinde (4 mm kalkar, plaka parlar) · etkin (6 mm içeride,
    /// amber lamba) · devre dışı (lamba sönük, plaka soluk). Tıklama <see cref="DeskTarget"/>'tan gelir.
    /// </summary>
    public sealed class StationKey : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Transform cap;
        [SerializeField] Renderer jewel, plate;
        [SerializeField] Material jewelOn, jewelOff;
        [SerializeField] DeskTarget target;

        Vector3 capRest;
        float offset, goal;
        bool active, disabled;
        Color plateColor;
        MaterialPropertyBlock block;
        Material plateInstance;

        public DeskTarget Target => target;
        public bool Active => active;

        public void Configure(Transform keyCap, Renderer jewelRenderer, Renderer plateRenderer, Material on, Material off, DeskTarget clickTarget)
        {
            cap = keyCap; jewel = jewelRenderer; plate = plateRenderer; jewelOn = on; jewelOff = off; target = clickTarget;
        }

        void Awake()
        {
            capRest = cap.localPosition;
            plateColor = plate != null && plate.sharedMaterial != null ? plate.sharedMaterial.GetColor(ColorId) : Color.white;
            if (target != null) target.Hovered += _ => Refresh();
        }

        void OnDestroy()
        {
            if (plateInstance != null) Destroy(plateInstance);
        }

        public void SetState(bool isActive, bool isDisabled)
        {
            if (active == isActive && disabled == isDisabled) return;
            active = isActive;
            disabled = isDisabled;
            if (target != null) target.Interactable = !disabled;
            Refresh();
        }

        void Refresh()
        {
            var t = SiginakTheme.Active;
            bool hovered = target != null && target.IsHovered && !disabled;
            goal = active && !disabled ? -t.Number("motion.keyPressMm", 6) / 1000f : hovered ? t.Number("motion.hoverLiftMm", 4) / 1000f : 0;
            if (jewel != null) jewel.sharedMaterial = active && !disabled ? jewelOn : jewelOff;
            if (plate != null)
            {
                float k = disabled ? 0.6f : hovered ? 1.15f : 1;
                var c = new Color(plateColor.r * k, plateColor.g * k, plateColor.b * k, plateColor.a);
                // Play'de plakanın malzeme kopyası (SRP Batcher uyumlu), editörde property block
                if (Application.isPlaying)
                {
                    plateInstance ??= plate.material;
                    plateInstance.SetColor(ColorId, c);
                }
                else
                {
                    block ??= new MaterialPropertyBlock();
                    plate.GetPropertyBlock(block);
                    block.SetColor(ColorId, c);
                    plate.SetPropertyBlock(block);
                }
            }
        }

        void Update()
        {
            if (Mathf.Approximately(offset, goal)) return;
            float speed = 0.006f / Mathf.Max(0.01f, SiginakTheme.Active.Number("motion.keyClickS", 0.06f));
            offset = Mathf.MoveTowards(offset, goal, speed * Time.unscaledDeltaTime);
            cap.localPosition = capRest + Vector3.up * offset;
        }
    }
}
