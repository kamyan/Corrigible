using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    public enum DeltaDir { Up, Down, Flat }

    /// <summary>
    /// Değişim (C5 “değişim”): ok yönü = değerin yönü, renk = iyi/kötü (durum kanalı). Courier 700 12, ikon 12, aralık 2.
    /// İllüzyonda düz çizgi + 0,0.
    /// </summary>
    [ExecuteAlways]
    public sealed class DeltaView : MonoBehaviour
    {
        [SerializeField] DeltaDir dir;
        [SerializeField] StatusKind status = StatusKind.Threat;
        [SerializeField] string text = "1,2/gün";
        [SerializeField] Image icon;
        [SerializeField] TMP_Text label;

        void OnEnable() { DeskTheme.Changed += Apply; Apply(); }
        void OnDisable() => DeskTheme.Changed -= Apply;
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(DeltaDir d, string t, StatusKind s)
        {
            if (dir == d && text == t && status == s) return;
            dir = d; text = t; status = s; Apply();
        }

        public static DeltaDir ParseDir(string s) => s == "up" ? DeltaDir.Up : s == "down" ? DeltaDir.Down : DeltaDir.Flat;

        public void Apply()
        {
            var theme = DeskTheme.Active;
            if (theme == null || label == null) return;
            DeskSprites.Apply(icon, dir switch { DeltaDir.Up => "ic_arrow_up_12b", DeltaDir.Down => "ic_arrow_down_12b", _ => "ic_flat_12b" });
            string ink = status == StatusKind.Neutral ? "ink.ink2" : DeskUi.StatusKey(status);
            DeskUi.Tint(icon, ink);
            DeskUi.Tint(label, ink);
            if (label.font != theme.cpBold) label.font = theme.cpBold;
            DeskUi.SetText(label, text);
            float tw = Mathf.Round(DeskUi.TextWidth(label, text));
            var lr = label.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0, 0.5f); lr.pivot = new Vector2(0, 0.5f);
            lr.anchoredPosition = new Vector2(12 + 2, 0); lr.sizeDelta = new Vector2(tw, 12);
            var rt = (RectTransform)transform;
            if (rt.sizeDelta != new Vector2(14 + tw, 12)) rt.sizeDelta = new Vector2(14 + tw, 12);
        }
    }
}
