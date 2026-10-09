using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Bölge panelindeki küçük durum etiketi (NORMAL, KARANTİNA, ÇEKİRDEK…).</summary>
    public sealed class ChipView : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] Graphic border;

        public void Set(string text, Color color, Color borderColor)
        {
            label.text = text;
            label.color = color;
            border.color = borderColor;
        }

        public void SetText(string text) => label.text = text;
        public void Show(bool on) { if (gameObject.activeSelf != on) gameObject.SetActive(on); }
    }
}
