using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Arayüz renkleri. Sahnedeki görünümler bu asset'e bağlıdır; Inspector'dan değiştirilen renkler oyunda
    /// durum renklerine (tehdit, uyarı, doğrulama…) uygulanır. Paneller ve yazıların sabit renkleri prefab/sahnede durur.
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/UI Theme", fileName = "UiTheme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Header("Zemin")]
        public Color bg = Palette.Bg;
        public Color panel = Palette.Panel;
        public Color raise = Palette.Raise;
        public Color line = Palette.Line;
        public Color line2 = Palette.Line2;

        [Header("Yazı")]
        public Color fg = Palette.Fg;
        public Color muted = Palette.Muted;
        public Color dim = Palette.Dim;

        [Header("Anlam")]
        public Color threat = Palette.Threat;
        public Color verify = Palette.Verify;
        public Color warn = Palette.Warn;
        public Color good = Palette.Good;
        public Color inkOnThreat = Palette.InkOnThreat;

        [Header("Harita")]
        public Color land = Palette.Land;
        public Color unknown = Palette.Unknown;
        [Tooltip("Sızma 0 iken düğüm rengi; 100'e doğru tehdit rengine kayar.")]
        public Color nodeBase = Palette.NodeBase;

        [Header("Seçim")]
        public Color selectedCardBg = Palette.TypeOnBg;

        public Color Infiltration(double v) => Color.Lerp(nodeBase, threat, Mathf.Pow(Mathf.Clamp01((float)v / 100f), 0.8f));
    }
}
