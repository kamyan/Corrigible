using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    public enum HandRingVariant { Value, Gauge }

    /// <summary>
    /// Şüpheli değerin etrafındaki turuncu yağlı kalem halkası (dosya “%38”: 124×56, sayaç “58,4”: 96×40).
    /// Halka değerin üstünden geçmez; değerin sol-üstüne göre prefab'da yerleşir. İllüzyonda gizlenir.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public sealed class HandRingView : MonoBehaviour
    {
        [SerializeField] HandRingVariant variant;

        void OnEnable() { DeskTheme.Changed += Apply; Apply(); }
        void OnDisable() => DeskTheme.Changed -= Apply;
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Apply()
        {
            var img = GetComponent<Image>();
            DeskSprites.Apply(img, variant == HandRingVariant.Value ? "mark_hand_ring_value" : "mark_hand_ring_gauge");
            DeskUi.Tint(img, "trust.suspicious.line");
            var size = variant == HandRingVariant.Value ? new Vector2(124, 56) : new Vector2(96, 40);
            var rt = (RectTransform)transform;
            if (rt.sizeDelta != size) rt.sizeDelta = size;
            img.raycastTarget = false;
        }
    }
}
