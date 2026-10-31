using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Anahtar rayındaki tek tuş (K1 notu › anahtar): normal · üzerinde (4 mm kalkar, plaka parlar) · etkin (6 mm içeride,
    /// amber lamba) · devre dışı (lamba sönük, plaka soluk). Tıklama <see cref="DeskTarget"/>'tan gelir. Revizyon 2: tuş
    /// dikkat isteyebilir (4 · KAPI, ziyaretçi bekliyor): lamba 1 Hz yanıp söner, halka ve el yazısı fiş görünür.
    /// </summary>
    public sealed class StationKey : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Transform cap;
        [SerializeField] Renderer jewel, plate;
        [SerializeField] Material jewelOn, jewelOff;
        [SerializeField] DeskTarget target;
        [Tooltip("Dikkat lambasının malzemesi (yoksa tuşun dikkat durumu yoktur).")]
        [SerializeField] Material jewelAttention;
        [Tooltip("Dikkatte görünen parçalar (lamba halkası, el yazısı fiş).")]
        [SerializeField] GameObject[] attentionMarks = Array.Empty<GameObject>();
        [Tooltip("Dikkat başlayınca sallanan fiş.")]
        [SerializeField] Transform attentionTag;

        Vector3 capRest;
        float offset, goal, swingAt = -100;
        bool active, disabled, attention;
        Color plateColor;
        Quaternion tagRest;
        MaterialPropertyBlock block;
        Material plateInstance;

        public DeskTarget Target => target;
        public bool Active => active;
        public bool Attention => attention;

        public void Configure(Transform keyCap, Renderer jewelRenderer, Renderer plateRenderer, Material on, Material off, DeskTarget clickTarget)
        {
            cap = keyCap; jewel = jewelRenderer; plate = plateRenderer; jewelOn = on; jewelOff = off; target = clickTarget;
        }

        void Awake()
        {
            capRest = cap.localPosition;
            plateColor = plate != null && plate.sharedMaterial != null ? plate.sharedMaterial.GetColor(ColorId) : Color.white;
            if (attentionTag != null) tagRest = attentionTag.localRotation;
            foreach (var m in attentionMarks) if (m != null) m.SetActive(false);
            if (target != null) target.Hovered += _ => Refresh();
        }

        /// <summary>Dikkat (ör. kapıda ziyaretçi): lamba 1 Hz yanıp söner, halka ve fiş görünür; açılırken fiş sallanır.</summary>
        public void SetAttention(bool on)
        {
            if (jewelAttention == null || attention == on) return;
            attention = on;
            foreach (var m in attentionMarks) if (m != null) m.SetActive(on);
            if (on) Nudge();
            Refresh();
        }

        /// <summary>Fiş bir kez sallanır (masaya dönüldüğünde dikkat hâlâ sürüyorsa).</summary>
        public void Nudge() => swingAt = Time.unscaledTime;

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
            if (attention && jewel != null && !(active && !disabled))
                jewel.sharedMaterial = Time.unscaledTime % 1f < 0.5f ? jewelAttention : jewelOff;
            if (attentionTag != null)
            {
                // Fiş 0,2 s sallanır: tek sönümlü salınım
                float age = Time.unscaledTime - swingAt;
                attentionTag.localRotation = age < 0.2f ? tagRest * Quaternion.Euler(0, 0, Mathf.Sin(age / 0.2f * Mathf.PI * 2) * 8 * (1 - age / 0.2f)) : tagRest;
            }
            if (Mathf.Approximately(offset, goal)) return;
            float speed = 0.006f / Mathf.Max(0.01f, SiginakTheme.Active.Number("motion.keyClickS", 0.06f));
            offset = Mathf.MoveTowards(offset, goal, speed * Time.unscaledDeltaTime);
            cap.localPosition = capRest + Vector3.up * offset;
        }
    }
}
