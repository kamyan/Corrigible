using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Tuş fişi (C5): dizin kâğıdı, 1 px kenar + 2 px alt kenar, Courier 700 11. En az 18×18; metin <see cref="DeskKeys"/>'ten
    /// yerelleştirilir (Space → “Boşluk”).
    /// </summary>
    [ExecuteAlways]
    public sealed class KeyChipView : MonoBehaviour
    {
        [SerializeField] string key = "R";
        [SerializeField] Image fill;
        [SerializeField] Image frame;
        [SerializeField] TMP_Text label;
        [SerializeField] bool autoWidth = true;
        [Header("Boy ve renk (varsayılan: 18 px, CP 11, dizin kâğıdı)")]
        [Tooltip("Olay seçeneği başlığında 22 px / CP 13.")]
        [SerializeField] float height = 18;
        [SerializeField] float fontSize = 11;
        [Tooltip("Olay alt şeridinde krem zemin (modal.footerText), #9A9284 kenar, ink yazı.")]
        [SerializeField] string fillKey = "paper.index", frameKey = "ink.ink3", textKey = "ink.ink2";

        void OnEnable() { DeskTheme.Changed += Apply; DeskLoc.Instance.Changed += Apply; Apply(); }
        void OnDisable() { DeskTheme.Changed -= Apply; DeskLoc.Instance.Changed -= Apply; }
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(string keyId)
        {
            if (key == keyId) return;
            key = keyId; Apply();
        }

        /// <summary>Tuşu uygular ve fişin genişliğini döner (düğmeler içeriği hizalarken kullanır).</summary>
        public float Measure(string keyId)
        {
            key = keyId; Apply();
            return ((RectTransform)transform).sizeDelta.x;
        }

        public void Apply()
        {
            var theme = DeskTheme.Active;
            if (theme == null || label == null) return;
            DeskSprites.Apply(fill, "sh_fill_r3");
            DeskUi.Tint(fill, fillKey);
            DeskSprites.Apply(frame, "sh_frame_key");
            DeskUi.Tint(frame, frameKey);
            if (label.font != theme.cpBold) label.font = theme.cpBold;
            if (!Mathf.Approximately(label.fontSize, fontSize)) label.fontSize = fontSize;
            DeskUi.Tint(label, textKey);
            var t = DeskKeys.Label(key);
            DeskUi.SetText(label, t);
            if (!autoWidth) return;
            var rt = (RectTransform)transform;
            float w = Mathf.Max(height, Mathf.Round(DeskUi.TextWidth(label, t)) + 8 + 2);
            if (rt.sizeDelta != new Vector2(w, height)) rt.sizeDelta = new Vector2(w, height);
        }
    }
}
