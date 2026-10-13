using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Çift çerçeveli damga (PROMPT §4.5, C5 “çip ve rozetler”): Franklin 800, harf aralığı .12em, opaklık .92.
    /// Açı kimlikten kararlı (−5°…+5°) ya da VM'den; İllüzyonda 0° ve opaklık .95. Genişlik metne göre kendiliğinden ayarlanır.
    /// </summary>
    [ExecuteAlways]
    public sealed class StampView : MonoBehaviour
    {
        [SerializeField] Image frame;
        [SerializeField] TMP_Text label;
        [SerializeField] string text = "ONAYLANDI";
        [SerializeField] StampInk ink = StampInk.Blue;
        [SerializeField] StampSize size = StampSize.Normal;
        [Tooltip("Açı kaynağı; boşsa metin kullanılır.")]
        [SerializeField] string stampId;
        [Tooltip("VM açısı (CSS derecesi, saat yönü pozitif). Kapalıysa kimlikten üretilir.")]
        [SerializeField] bool useAngle;
        [SerializeField] float angle;
        [Tooltip("Kapalıysa genişlik elle verilir (ör. sabit sütun).")]
        [SerializeField] bool autoWidth = true;
        [Tooltip("0 = boyun varsayılanı (11 / 12 / 30); dosya kapağındaki damga 13.")]
        [SerializeField] float fontSizeOverride;
        [Tooltip("0 = boyun varsayılanı; C4 büyük damgası (yatay 16, dikey 9).")]
        [SerializeField] Vector2 paddingOverride;

        void OnEnable() { DeskTheme.Changed += Apply; Apply(); }
        void OnDisable() => DeskTheme.Changed -= Apply;
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public static StampInk ParseInk(string color) => color == "red" ? StampInk.Red : color == "green" ? StampInk.Green : StampInk.Blue;

        /// <param name="vmAngle">CSS derecesi (saat yönü); null ise kimlikten.</param>
        public void Set(string stampText, StampInk stampInk, StampSize stampSize = StampSize.Normal, float? vmAngle = null, string id = null, float fontSize = 0)
        {
            text = stampText; ink = stampInk; size = stampSize; stampId = id; fontSizeOverride = fontSize;
            useAngle = vmAngle.HasValue; angle = vmAngle ?? 0;
            Apply();
        }

        (string sprite, float font, float spacing, float padX, float padY, float border) Metrics() => size switch
        {
            StampSize.Small => ("stamp_frame_sm", 11, 12, 6, 3, 1),   // CSS 1,5 px kenar Chrome'da 1 px çizilir (yükseklik 19)
            StampSize.Large => ("stamp_frame_lg", 30, 14, 14, 4, 3),
            _ => ("stamp_frame", 12, 12, 8, 3, 2),
        };

        public void Apply()
        {
            var theme = DeskTheme.Active;
            if (theme == null || frame == null || label == null) return;
            var (sprite, font, spacing, padX, padY, border) = Metrics();
            if (fontSizeOverride > 0) font = fontSizeOverride;
            if (paddingOverride.x > 0) padX = paddingOverride.x;
            if (paddingOverride.y > 0) padY = paddingOverride.y;
            float opacity = DeskTheme.Illusion ? theme.stampOpacityIllusion : theme.stampOpacity;

            DeskSprites.Apply(frame, sprite);
            DeskUi.Tint(frame, DeskUi.StampKey(ink), opacity);
            DeskUi.Tint(label, DeskUi.StampKey(ink), opacity);
            if (label.font != theme.lfExtraBold) label.font = theme.lfExtraBold;
            if (!Mathf.Approximately(label.fontSize, font)) label.fontSize = font;
            if (!Mathf.Approximately(label.characterSpacing, spacing)) label.characterSpacing = spacing;
            DeskUi.SetText(label, text);
            // CSS ortalarken son harften sonraki harf aralığını da içeriğe sayar; TMP saymaz → metni yarısı kadar sola al
            float trail = spacing / 100f * font;
            var lr = label.rectTransform;
            if (!Mathf.Approximately(lr.anchoredPosition.x, -trail / 2)) lr.anchoredPosition = new Vector2(-trail / 2, lr.anchoredPosition.y);

            var rt = (RectTransform)transform;
            float h = font + padY * 2 + border * 2;
            float w = autoWidth ? Mathf.Round(DeskUi.TextWidth(label, text)) + padX * 2 + border * 2 : rt.sizeDelta.x;
            if (rt.sizeDelta != new Vector2(w, h)) rt.sizeDelta = new Vector2(w, h);

            float z = -theme.StampAngle(string.IsNullOrEmpty(stampId) ? text : stampId, useAngle ? angle : (float?)null);
            if (!Mathf.Approximately(rt.localEulerAngles.z, z)) rt.localEulerAngles = new Vector3(0, 0, z);
        }
    }
}
