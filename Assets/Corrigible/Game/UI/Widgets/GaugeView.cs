using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Üst şeritteki gösterge: değer, not ve dolum çubuğu.</summary>
    public sealed class GaugeView : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text value;
        [SerializeField] TMP_Text note;
        [SerializeField] RectTransform fill;
        [SerializeField] Graphic fillGraphic;

        /// <summary>Sabit başlık ve edit modda görünen önizleme değerleri (editörde çağrılır).</summary>
        public void Setup(string titleText, string noteText, string previewValue)
        {
            title.text = titleText;
            note.text = noteText;
            value.text = previewValue;
        }

        public void Set(string text, double pct, Color color, string noteText = null)
        {
            value.text = text;
            fill.anchorMax = new Vector2(Mathf.Clamp01((float)pct / 100f), 1);
            fillGraphic.color = color;
            if (noteText != null) note.text = noteText;
        }
    }
}
