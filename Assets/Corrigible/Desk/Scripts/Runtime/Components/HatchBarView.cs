using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Araştırma satırı ilerleme çubuğu (C1 .pbar): 48×10, 1 px ink2 kenar, içi “/” tarama.</summary>
    [ExecuteAlways]
    public sealed class HatchBarView : MonoBehaviour
    {
        [Range(0, 1)] [SerializeField] float value = 0.62f;
        [SerializeField] Image frame;
        [SerializeField] Image hatch;

        void OnEnable() { DeskTheme.Changed += Apply; Apply(); }
        void OnDisable() => DeskTheme.Changed -= Apply;
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            if (Mathf.Approximately(value, fraction)) return;
            value = fraction; Apply();
        }

        public void Apply()
        {
            if (frame == null || hatch == null) return;
            DeskSprites.Apply(frame, "sh_frame_r0_1");
            DeskUi.Tint(frame, "ink.ink2");
            DeskSprites.Apply(hatch, "pt_hatch");
            DeskUi.Tint(hatch, "ink.graphite");
            var size = ((RectTransform)transform).rect.size;
            var h = hatch.rectTransform;
            h.anchorMin = new Vector2(0, 0); h.anchorMax = new Vector2(0, 1); h.pivot = new Vector2(0, 0.5f);
            h.offsetMin = new Vector2(1, 1); h.offsetMax = new Vector2(1 + (size.x - 2) * value, -1);
            DeskUi.SetActive(hatch, value > 0.001f);
        }
    }
}
