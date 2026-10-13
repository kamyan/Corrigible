using System.Collections;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Teleks günlüğünde bir satır: olay, tur ayıracı veya ele geçirilen YZ şeridi.</summary>
    public sealed class RaidLogLineView : MonoBehaviour
    {
        [SerializeField] TMP_Text body;
        [SerializeField] TMP_Text header;
        [SerializeField] Image background;
        [SerializeField] Image rule;
        [SerializeField] LayoutElement layout;
        [SerializeField, Min(1)] float charactersPerSecond = 26f;

        RectTransform rowRect;
        bool hasLine, intercepted, separator, measuring;
        float measuredWidth = -1f;

        void OnEnable() { if (hasLine) RefreshHeight(true); }

        void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled && hasLine && !measuring) RefreshHeight(false);
        }

        public void Set(Baskin.LogLine line, UiTheme theme, bool animate)
        {
            StopAllCoroutines();
            intercepted = line.Kind == "ai"; separator = line.Kind == "turn";
            var color = intercepted || line.Kind == "bad" ? theme.threat : line.Kind == "good" ? theme.good : separator ? theme.fg : theme.muted;
            if (header != null)
            {
                header.gameObject.SetActive(intercepted);
                header.text = Loc.T("YZ · ELE GEÇİRİLEN SATIR");
                header.color = theme.threat;
            }
            if (background != null)
            {
                var tint = theme.threat; tint.a = intercepted ? 0.12f : 0f;
                background.color = tint;
            }
            if (rule != null)
            {
                rule.gameObject.SetActive(separator || intercepted);
                rule.color = intercepted ? theme.threat : theme.line2;
            }
            string symbol = intercepted ? "> " : line.Kind == "good" ? "✓ " : line.Kind == "bad" ? "× " : separator ? "" : "→ ";
            body.text = Loc.F("{0}{1}", symbol, line.Text);
            body.color = color;
            body.fontStyle = intercepted || separator ? FontStyles.Bold : FontStyles.Normal;
            body.alignment = separator ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Overflow;
            body.maxVisibleCharacters = int.MaxValue;
            // Bir ayıraç 24 px, uzun teleks satırı daha yüksek olabilir; metin sabit y/height ile taşmaz.
            var rect = body.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 4f);
            rect.offsetMax = new Vector2(-8f, intercepted ? -24f : -4f);
            hasLine = true;
            RefreshHeight(true);
            body.ForceMeshUpdate();
            if (animate && !separator) StartCoroutine(TypeLine());
        }

        void RefreshHeight(bool force)
        {
            if (!hasLine || measuring || body == null || layout == null) return;
            if (rowRect == null) rowRect = transform as RectTransform;
            if (rowRect == null) return;
            float rowWidth = rowRect.rect.width;
            if (rowWidth <= 0f && rowRect.parent is RectTransform parentRect) rowWidth = parentRect.rect.width;
            float width = Mathf.Max(1f, rowWidth - 16f);
            // Dikey layout, ilk ölçümde henüz satır yüksekliğini vermemiş olabilir.
            // Tercih edilen yükseklik tam metin ve bilinen genişlikle hesaplanır; Ellipsis'e bağlı değildir.
            if (!force && Mathf.Abs(width - measuredWidth) < 0.5f) return;
            measuring = true;
            try
            {
                measuredWidth = width;
                float textHeight = body.GetPreferredValues(body.text, width, float.PositiveInfinity).y;
                float padding = intercepted ? 28f : 8f;
                float height = Mathf.Max(separator ? 24f : 26f, Mathf.Ceil(textHeight) + padding);
                if (!Mathf.Approximately(layout.preferredHeight, height)) layout.preferredHeight = height;
            }
            finally { measuring = false; }
        }

        IEnumerator TypeLine()
        {
            body.ForceMeshUpdate();
            int count = body.textInfo.characterCount;
            float visible = 0;
            body.maxVisibleCharacters = 0;
            while (visible < count)
            {
                visible += Time.unscaledDeltaTime * charactersPerSecond;
                body.maxVisibleCharacters = Mathf.Min(count, Mathf.CeilToInt(visible));
                yield return null;
            }
            body.maxVisibleCharacters = int.MaxValue;
        }
    }
}
